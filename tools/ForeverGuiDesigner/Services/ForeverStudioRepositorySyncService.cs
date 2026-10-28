using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ForeverGuiDesigner.Services;

public sealed record ForeverStudioRepositoryChange(string Status, string Path, bool StudioManaged);

public sealed record ForeverStudioRepositorySyncInspection(
    string CurrentBranch,
    string TargetBranch,
    string HeadCommit,
    string TargetCommit,
    IReadOnlyList<ForeverStudioRepositoryChange> Changes,
    int LocalTargetAhead,
    string? Error)
{
    public IReadOnlyList<ForeverStudioRepositoryChange> ManualChanges => Changes.Where(change => !change.StudioManaged).ToArray();
    public bool NeedsUpdate =>
        !CurrentBranch.Equals(TargetBranch, StringComparison.OrdinalIgnoreCase) ||
        !HeadCommit.Equals(TargetCommit, StringComparison.OrdinalIgnoreCase);
    public bool CanUpdate => Error is null && LocalTargetAhead == 0 && ManualChanges.Count == 0;
}

public sealed record ForeverStudioRepositorySyncResult(
    bool Succeeded,
    bool Updated,
    string TargetBranch,
    IReadOnlyList<string> PreservedMachineProjects,
    string? Error);

/// <summary>
/// Safely aligns the selected local checkout with the repository branch used by the running Forever
/// Studio build. Machine Project source JSON is authoritative and backed up before Git touches the
/// working tree; generated Studio output is deliberately regenerated afterwards. Unknown/manual
/// changes fail closed instead of being stashed, overwritten or hidden from the user.
/// </summary>
public static partial class ForeverStudioRepositorySyncService
{
    public const string StableBranch = "main";
    public const string DevelopmentBranch = "feature/forever-studio-runtime-production";

    private const string MainModPath = "mod/src/main/java/de/tekatz/foreverskyblock/ForeverSkyblock.java";
    private const string GeneratedJavaPrefix = "mod/src/main/java/de/tekatz/foreverskyblock/generated/";
    private const string LanguagePath = "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json";
    private const string StudioDataPrefix = "mod/src/main/resources/data/forever_skyblock/forever_studio/";
    private const string StudioGeneratedPrefix = StudioDataPrefix + "generated/";
    private const string StudioRuntimePrefix = StudioDataPrefix + "runtime/";
    private const string StudioRecipePrefix = "mod/src/main/resources/data/forever_skyblock/recipe/forever_studio/";
    private const string StudioAssetPrefix = "mod/src/main/resources/assets/forever_skyblock/forever_studio/";
    private const string StudioComponentModelPrefix = "mod/src/main/resources/assets/forever_skyblock/models/block/forever_studio/";
    private const string GeneratedBridgeMarker =
        "de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines.register(modBus); // Forever Studio generated machines";

    [GeneratedRegex(@"COMPILER_CONTRACT_VERSION\s*=\s*(\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex ContractVersionRegex();

    public static async Task<ForeverStudioRepositorySyncInspection> InspectAsync(
        string repoRoot,
        string assetId,
        string? targetBranch = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
            return FailedInspection("No repository is selected.");

        var root = Path.GetFullPath(repoRoot.Trim());
        if (!Directory.Exists(root) || !Directory.Exists(Path.Combine(root, ".git")))
            return FailedInspection($"Selected directory is not a Git checkout: {root}");

        try
        {
            progress?.Report("> Checking local Git repository…");
            await RunGitAsync(root, ["--version"], cancellationToken);
            progress?.Report("> Fetching repository metadata from origin…");
            await RunGitAsync(root, ["fetch", "origin", "--prune"], cancellationToken);

            targetBranch ??= await ResolveTargetBranchAsync(root, cancellationToken);
            var remoteRef = $"refs/remotes/origin/{targetBranch}";
            var remote = await RunGitAsync(root, ["rev-parse", "--verify", remoteRef], cancellationToken, throwOnFailure: false);
            if (remote.ExitCode != 0)
                return FailedInspection($"Required remote branch 'origin/{targetBranch}' is unavailable.", targetBranch);

            var currentBranchResult = await RunGitAsync(root, ["branch", "--show-current"], cancellationToken);
            var currentBranch = currentBranchResult.StdOut.Trim();
            if (string.IsNullOrWhiteSpace(currentBranch))
                return FailedInspection("The selected repository is in detached-HEAD state. Switch to a normal branch before automatic sync.", targetBranch);

            var head = (await RunGitAsync(root, ["rev-parse", "HEAD"], cancellationToken)).StdOut.Trim();
            var target = remote.StdOut.Trim();

            var targetContract = await RunGitAsync(
                root,
                ["show", $"origin/{targetBranch}:{ForeverStudioRuntimeRepositoryContract.RelativeMarkerPath}"],
                cancellationToken,
                throwOnFailure: false);
            if (targetContract.ExitCode != 0 || !HasExpectedRuntimeContract(targetContract.StdOut))
            {
                return FailedInspection(
                    $"Remote branch 'origin/{targetBranch}' does not provide the Forever Studio runtime contract v{ForeverStudioRuntimeRepositoryContract.ExpectedCompilerContractVersion} required by this Studio build.",
                    targetBranch,
                    currentBranch,
                    head,
                    target);
            }

            var localTargetAhead = 0;
            var localTarget = await RunGitAsync(root, ["show-ref", "--verify", "--quiet", $"refs/heads/{targetBranch}"], cancellationToken, throwOnFailure: false);
            if (localTarget.ExitCode == 0)
            {
                var counts = await RunGitAsync(
                    root,
                    ["rev-list", "--left-right", "--count", $"refs/heads/{targetBranch}...{remoteRef}"],
                    cancellationToken);
                var parts = counts.StdOut.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 1)
                    _ = int.TryParse(parts[0], out localTargetAhead);
            }

            var status = await RunGitAsync(
                root,
                ["-c", "core.quotepath=false", "status", "--porcelain=v1", "-z", "--untracked-files=all"],
                cancellationToken);
            var knownAssetIds = KnownMachineAssetIds(root, assetId);
            var changes = ParseStatus(status.StdOut, knownAssetIds);
            changes = await VerifySharedStudioChangesAsync(root, changes, knownAssetIds, cancellationToken);

            string? error = null;
            if (localTargetAhead > 0)
                error = $"Local branch '{targetBranch}' contains {localTargetAhead} commit(s) that are not on origin. Automatic sync will not rewrite local commits.";
            else if (changes.Any(change => !change.StudioManaged))
                error = "Manual/non-Studio working-tree changes were detected. Automatic sync will not overwrite or stash them.";

            return new ForeverStudioRepositorySyncInspection(
                currentBranch,
                targetBranch,
                head,
                target,
                changes,
                localTargetAhead,
                error);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FailedInspection(ex.Message, targetBranch ?? string.Empty);
        }
    }

    public static async Task<ForeverStudioRepositorySyncResult> SynchronizeAsync(
        string repoRoot,
        string assetId,
        string? targetBranch = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var inspection = await InspectAsync(repoRoot, assetId, targetBranch, progress, cancellationToken);
        if (!inspection.NeedsUpdate)
        {
            progress?.Report($"> Repository already matches '{inspection.TargetBranch}'.");
            return new ForeverStudioRepositorySyncResult(true, false, inspection.TargetBranch, [], null);
        }

        if (!inspection.CanUpdate)
        {
            var manual = inspection.ManualChanges.Take(5).Select(change => change.Path).ToArray();
            var detail = manual.Length == 0 ? inspection.Error :
                $"{inspection.Error} Files: {string.Join(", ", manual)}{(inspection.ManualChanges.Count > manual.Length ? ", …" : string.Empty)}";
            return new ForeverStudioRepositorySyncResult(false, false, inspection.TargetBranch, [], detail ?? "Repository sync is blocked.");
        }

        var root = Path.GetFullPath(repoRoot.Trim());
        var backupRoot = Path.Combine(Path.GetTempPath(), "ForeverStudio", "repository-sync", Guid.NewGuid().ToString("N"));
        var backupMachines = Path.Combine(backupRoot, "machines");
        var machineRoot = Resolve(root, ForeverStudioMachineProjectService.RelativeDirectory);
        var deletedMachineProjects = inspection.Changes
            .Where(change => change.Path.StartsWith(ForeverStudioMachineProjectService.RelativeDirectory + "/", StringComparison.OrdinalIgnoreCase) &&
                             change.Status.Contains('D'))
            .Select(change => change.Path)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var preservedProjects = new List<string>();

        try
        {
            Directory.CreateDirectory(backupMachines);
            if (Directory.Exists(machineRoot))
            {
                foreach (var file in Directory.EnumerateFiles(machineRoot, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(machineRoot, file);
                    var target = Path.Combine(backupMachines, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(file, target, overwrite: true);
                    preservedProjects.Add(Path.Combine(ForeverStudioMachineProjectService.RelativeDirectory, relative).Replace('\\', '/'));
                }
            }

            progress?.Report($"> Preserved {preservedProjects.Count} Machine Project source file(s).");
            progress?.Report("> Removing stale Studio-generated working-tree output…");
            await RunGitAsync(root, ["reset", "--hard", "HEAD"], cancellationToken);
            await RunGitAsync(root, ["clean", "-fd"], cancellationToken);

            var localTarget = await RunGitAsync(root, ["show-ref", "--verify", "--quiet", $"refs/heads/{inspection.TargetBranch}"], cancellationToken, throwOnFailure: false);
            if (localTarget.ExitCode == 0)
            {
                progress?.Report($"> Switching to '{inspection.TargetBranch}'…");
                await RunGitAsync(root, ["switch", inspection.TargetBranch], cancellationToken);
                progress?.Report($"> Fast-forwarding '{inspection.TargetBranch}' from origin…");
                await RunGitAsync(root, ["merge", "--ff-only", $"origin/{inspection.TargetBranch}"], cancellationToken);
            }
            else
            {
                progress?.Report($"> Creating local tracking branch '{inspection.TargetBranch}'…");
                await RunGitAsync(root, ["switch", "--track", "-c", inspection.TargetBranch, $"origin/{inspection.TargetBranch}"], cancellationToken);
            }

            if (Directory.Exists(backupMachines))
            {
                Directory.CreateDirectory(machineRoot);
                foreach (var file in Directory.EnumerateFiles(backupMachines, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(backupMachines, file);
                    var destination = Path.Combine(machineRoot, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                    File.Copy(file, destination, overwrite: true);
                }
            }

            foreach (var deleted in deletedMachineProjects)
            {
                var fullPath = Resolve(root, deleted);
                if (File.Exists(fullPath))
                    File.Delete(fullPath);
            }

            if (ForeverStudioRuntimeRepositoryContract.Validate(root) is { } contractIssue)
                throw new InvalidOperationException(contractIssue.Message);

            progress?.Report($"> Repository synchronized to '{inspection.TargetBranch}'.");
            progress?.Report("> Machine Projects restored. Generated machine output will now be rebuilt from source.");
            return new ForeverStudioRepositorySyncResult(true, true, inspection.TargetBranch, preservedProjects, null);
        }
        catch (OperationCanceledException)
        {
            TryRestoreMachineProjects(backupMachines, machineRoot);
            throw;
        }
        catch (Exception ex)
        {
            TryRestoreMachineProjects(backupMachines, machineRoot);
            return new ForeverStudioRepositorySyncResult(false, false, inspection.TargetBranch, preservedProjects,
                $"Repository sync failed: {ex.Message}");
        }
        finally
        {
            try { if (Directory.Exists(backupRoot)) Directory.Delete(backupRoot, recursive: true); } catch { }
        }
    }

    private static async Task<string> ResolveTargetBranchAsync(string root, CancellationToken cancellationToken)
    {
        var commit = SelfUpdateService.CurrentCommit;
        if (Regex.IsMatch(commit, "^[0-9a-fA-F]{40}$", RegexOptions.CultureInvariant))
        {
            var containing = await RunGitAsync(
                root,
                ["branch", "-r", "--contains", commit, "--format=%(refname:short)"],
                cancellationToken,
                throwOnFailure: false);
            if (containing.ExitCode == 0)
            {
                var branches = containing.StdOut.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (branches.Contains("origin/main"))
                    return StableBranch;
                if (branches.Contains("origin/" + DevelopmentBranch))
                    return DevelopmentBranch;
            }
        }

        return SelfUpdateService.LoadPreferences().Channel == UpdateChannel.Stable
            ? StableBranch
            : DevelopmentBranch;
    }

    private static HashSet<string> KnownMachineAssetIds(string root, string currentAssetId)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(currentAssetId))
            ids.Add(ForeverStudioMachineProjectService.SanitizeAssetId(currentAssetId));

        AddAssetIdsFromDirectory(root, ForeverStudioMachineProjectService.RelativeDirectory, ids);
        AddAssetIdsFromDirectory(root, StudioGeneratedPrefix.TrimEnd('/'), ids);
        AddAssetIdsFromDirectory(root, StudioRuntimePrefix.TrimEnd('/'), ids);
        return ids;
    }

    private static void AddAssetIdsFromDirectory(string root, string relativeDirectory, HashSet<string> ids)
    {
        var directory = Resolve(root, relativeDirectory);
        if (!Directory.Exists(directory))
            return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var id = Path.GetFileNameWithoutExtension(file);
                var sanitized = ForeverStudioMachineProjectService.SanitizeAssetId(id);
                if (string.Equals(id, sanitized, StringComparison.Ordinal))
                    ids.Add(id);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Classification stays conservative if an owned directory cannot be enumerated.
        }
    }

    private static IReadOnlyList<ForeverStudioRepositoryChange> ParseStatus(string output, IReadOnlySet<string> assetIds)
    {
        var changes = new List<ForeverStudioRepositoryChange>();
        var entries = output.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            if (entry.Length < 4)
                continue;
            var status = entry[..2];
            var path = NormalizePath(entry[3..]);
            if (status.IndexOfAny(['R', 'C']) >= 0)
            {
                if (index + 1 < entries.Length)
                    path += " -> " + NormalizePath(entries[++index]);
                changes.Add(new ForeverStudioRepositoryChange(status, path, false));
                continue;
            }
            changes.Add(new ForeverStudioRepositoryChange(status, path, IsStudioManagedPath(path, assetIds)));
        }
        return changes;
    }

    private static async Task<IReadOnlyList<ForeverStudioRepositoryChange>> VerifySharedStudioChangesAsync(
        string root,
        IReadOnlyList<ForeverStudioRepositoryChange> changes,
        IReadOnlySet<string> assetIds,
        CancellationToken cancellationToken)
    {
        var result = new List<ForeverStudioRepositoryChange>(changes.Count);
        foreach (var change in changes)
        {
            if (!change.StudioManaged ||
                (!change.Path.Equals(MainModPath, StringComparison.OrdinalIgnoreCase) &&
                 !change.Path.Equals(LanguagePath, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(change);
                continue;
            }

            var managed = change.Path.Equals(MainModPath, StringComparison.OrdinalIgnoreCase)
                ? await IsGeneratedBridgeOnlyChangeAsync(root, change.Path, cancellationToken)
                : await IsMachineLanguageOnlyChangeAsync(root, change.Path, assetIds, cancellationToken);
            result.Add(change with { StudioManaged = managed });
        }
        return result;
    }

    private static async Task<bool> IsGeneratedBridgeOnlyChangeAsync(
        string root,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var workingPath = Resolve(root, relativePath);
        if (!File.Exists(workingPath))
            return false;
        var head = await RunGitAsync(root, ["show", $"HEAD:{relativePath}"], cancellationToken, throwOnFailure: false);
        if (head.ExitCode != 0)
            return false;
        string working;
        try { working = File.ReadAllText(workingPath); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
        return string.Equals(StripGeneratedBridge(working), StripGeneratedBridge(head.StdOut), StringComparison.Ordinal);
    }

    private static string StripGeneratedBridge(string source)
    {
        source = source.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return string.Join("\n", source.Split('\n')
            .Where(line => !line.Contains(GeneratedBridgeMarker, StringComparison.Ordinal)));
    }

    private static async Task<bool> IsMachineLanguageOnlyChangeAsync(
        string root,
        string relativePath,
        IReadOnlySet<string> assetIds,
        CancellationToken cancellationToken)
    {
        var workingPath = Resolve(root, relativePath);
        if (!File.Exists(workingPath))
            return false;
        var head = await RunGitAsync(root, ["show", $"HEAD:{relativePath}"], cancellationToken, throwOnFailure: false);
        if (head.ExitCode != 0)
            return false;

        try
        {
            using var beforeDoc = JsonDocument.Parse(head.StdOut);
            using var afterDoc = JsonDocument.Parse(File.ReadAllText(workingPath));
            if (beforeDoc.RootElement.ValueKind != JsonValueKind.Object || afterDoc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            var before = beforeDoc.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
            var after = afterDoc.RootElement.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
            var allowed = assetIds.Select(id => $"block.forever_skyblock.{id}").ToHashSet(StringComparer.Ordinal);
            foreach (var key in before.Keys.Concat(after.Keys).Distinct(StringComparer.Ordinal))
            {
                if (allowed.Contains(key))
                    continue;
                if (!before.TryGetValue(key, out var beforeValue) || !after.TryGetValue(key, out var afterValue) ||
                    !JsonValuesEqual(beforeValue, afterValue))
                    return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    private static bool JsonValuesEqual(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
            return false;

        switch (left.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var leftProperties = left.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
                var rightProperties = right.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.Clone(), StringComparer.Ordinal);
                if (leftProperties.Count != rightProperties.Count)
                    return false;
                foreach (var pair in leftProperties)
                {
                    if (!rightProperties.TryGetValue(pair.Key, out var value) || !JsonValuesEqual(pair.Value, value))
                        return false;
                }
                return true;
            }
            case JsonValueKind.Array:
            {
                var leftItems = left.EnumerateArray().ToArray();
                var rightItems = right.EnumerateArray().ToArray();
                if (leftItems.Length != rightItems.Length)
                    return false;
                for (var index = 0; index < leftItems.Length; index++)
                    if (!JsonValuesEqual(leftItems[index], rightItems[index]))
                        return false;
                return true;
            }
            case JsonValueKind.String:
                return string.Equals(left.GetString(), right.GetString(), StringComparison.Ordinal);
            case JsonValueKind.Number:
                if (left.TryGetDecimal(out var leftDecimal) && right.TryGetDecimal(out var rightDecimal))
                    return leftDecimal == rightDecimal;
                return string.Equals(left.GetRawText(), right.GetRawText(), StringComparison.Ordinal);
            case JsonValueKind.True:
            case JsonValueKind.False:
                return left.GetBoolean() == right.GetBoolean();
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return true;
            default:
                return string.Equals(left.GetRawText(), right.GetRawText(), StringComparison.Ordinal);
        }
    }

    public static bool IsStudioManagedPath(string path, string assetId)
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(assetId))
            ids.Add(ForeverStudioMachineProjectService.SanitizeAssetId(assetId));
        return IsStudioManagedPath(path, ids);
    }

    private static bool IsStudioManagedPath(string path, IReadOnlySet<string> assetIds)
    {
        path = NormalizePath(path);
        var machinePrefix = ForeverStudioMachineProjectService.RelativeDirectory + "/";
        if (path.StartsWith(machinePrefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(StudioDataPrefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(StudioRecipePrefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(StudioAssetPrefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(StudioComponentModelPrefix, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(GeneratedJavaPrefix, StringComparison.OrdinalIgnoreCase) ||
            path.Equals(MainModPath, StringComparison.OrdinalIgnoreCase) ||
            path.Equals(LanguagePath, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (var id in assetIds)
        {
            if (path.Equals($"mod/src/main/resources/assets/forever_skyblock/blockstates/{id}.json", StringComparison.OrdinalIgnoreCase) ||
                path.Equals($"mod/src/main/resources/assets/forever_skyblock/models/block/{id}.json", StringComparison.OrdinalIgnoreCase) ||
                path.Equals($"mod/src/main/resources/assets/forever_skyblock/models/item/{id}.json", StringComparison.OrdinalIgnoreCase) ||
                path.Equals($"mod/src/main/resources/data/forever_skyblock/loot_table/blocks/{id}.json", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool HasExpectedRuntimeContract(string source)
    {
        var match = ContractVersionRegex().Match(source);
        return match.Success && int.TryParse(match.Groups[1].Value, out var version) &&
               version == ForeverStudioRuntimeRepositoryContract.ExpectedCompilerContractVersion;
    }

    private static ForeverStudioRepositorySyncInspection FailedInspection(
        string error,
        string targetBranch = "",
        string currentBranch = "",
        string head = "",
        string target = "")
        => new(currentBranch, targetBranch, head, target, [], 0, error);

    private static string NormalizePath(string path) => path.Replace('\\', '/').Trim();

    private static string Resolve(string root, string relative)
        => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

    private static void TryRestoreMachineProjects(string backupMachines, string machineRoot)
    {
        try
        {
            if (!Directory.Exists(backupMachines))
                return;
            Directory.CreateDirectory(machineRoot);
            foreach (var file in Directory.EnumerateFiles(backupMachines, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(backupMachines, file);
                var destination = Path.Combine(machineRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(file, destination, overwrite: true);
            }
        }
        catch
        {
            // The backup directory remains until finally. Preserve the original sync error.
        }
    }

    private sealed record GitCommandResult(int ExitCode, string StdOut, string StdErr);

    private static async Task<GitCommandResult> RunGitAsync(
        string root,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken,
        bool throwOnFailure = true)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
                throw new InvalidOperationException("Git process could not be started.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException($"Could not start Git. Ensure Git is installed and available on PATH. {ex.Message}", ex);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        });
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);
        var result = new GitCommandResult(process.ExitCode, stdout, stderr);
        if (throwOnFailure && result.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(stderr) ? stdout.Trim() : stderr.Trim();
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed with exit code {result.ExitCode}: {detail}");
        }
        return result;
    }
}
