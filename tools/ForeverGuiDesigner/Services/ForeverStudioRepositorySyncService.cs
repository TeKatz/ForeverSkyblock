using System.Diagnostics;
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
    public bool NeedsUpdate => Error is null &&
                               (!CurrentBranch.Equals(TargetBranch, StringComparison.OrdinalIgnoreCase) ||
                                !HeadCommit.Equals(TargetCommit, StringComparison.OrdinalIgnoreCase));
    public bool CanUpdate => Error is null && ManualChanges.Count == 0 && LocalTargetAhead == 0;
}

public sealed record ForeverStudioRepositorySyncResult(
    bool Succeeded,
    bool Updated,
    string TargetBranch,
    IReadOnlyList<string> PreservedMachineProjects,
    string? Error);

/// <summary>
/// Safely aligns the selected local checkout with the repository revision family used by the running
/// Forever Studio build. Machine Project source JSON is treated as authoritative and backed up before
/// Git touches the working tree; generated Studio output is deliberately regenerated after the sync.
/// Unknown/manual working-tree changes fail closed instead of being stashed or overwritten.
/// </summary>
public static partial class ForeverStudioRepositorySyncService
{
    public const string StableBranch = "main";
    public const string DevelopmentBranch = "feature/forever-studio-runtime-production";

    private const string MainModPath = "mod/src/main/java/de/tekatz/foreverskyblock/ForeverSkyblock.java";
    private const string GeneratedJavaPrefix = "mod/src/main/java/de/tekatz/foreverskyblock/generated/";
    private const string LanguagePath = "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json";
    private const string StudioDataPrefix = "mod/src/main/resources/data/forever_skyblock/forever_studio/";
    private const string StudioRecipePrefix = "mod/src/main/resources/data/forever_skyblock/recipe/forever_studio/";
    private const string StudioAssetPrefix = "mod/src/main/resources/assets/forever_skyblock/forever_studio/";
    private const string StudioComponentModelPrefix = "mod/src/main/resources/assets/forever_skyblock/models/block/forever_studio/";

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
            var changes = ParseStatus(status.StdOut, assetId);

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
        if (!inspection.CanUpdate)
        {
            var manual = inspection.ManualChanges.Take(5).Select(change => change.Path).ToArray();
            var detail = manual.Length == 0 ? inspection.Error :
                $"{inspection.Error} Files: {string.Join(", ", manual)}{(inspection.ManualChanges.Count > manual.Length ? ", …" : string.Empty)}";
            return new ForeverStudioRepositorySyncResult(false, false, inspection.TargetBranch, [], detail ?? "Repository sync is blocked.");
        }

        if (!inspection.NeedsUpdate)
        {
            progress?.Report($"> Repository already matches '{inspection.TargetBranch}'.");
            return new ForeverStudioRepositorySyncResult(true, false, inspection.TargetBranch, [], null);
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

    private static IReadOnlyList<ForeverStudioRepositoryChange> ParseStatus(string output, string assetId)
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
                // Porcelain -z reports the second rename/copy path as the following token. Treat
                // renames conservatively; asset ids are intentionally stable in Machine Builder.
                if (index + 1 < entries.Length)
                    path += " -> " + NormalizePath(entries[++index]);
                changes.Add(new ForeverStudioRepositoryChange(status, path, false));
                continue;
            }
            changes.Add(new ForeverStudioRepositoryChange(status, path, IsStudioManagedPath(path, assetId)));
        }
        return changes;
    }

    public static bool IsStudioManagedPath(string path, string assetId)
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

        if (string.IsNullOrWhiteSpace(assetId))
            return false;
        var safe = ForeverStudioMachineProjectService.SanitizeAssetId(assetId);
        return path.Equals($"mod/src/main/resources/assets/forever_skyblock/blockstates/{safe}.json", StringComparison.OrdinalIgnoreCase) ||
               path.Equals($"mod/src/main/resources/assets/forever_skyblock/models/block/{safe}.json", StringComparison.OrdinalIgnoreCase) ||
               path.Equals($"mod/src/main/resources/assets/forever_skyblock/models/item/{safe}.json", StringComparison.OrdinalIgnoreCase) ||
               path.Equals($"mod/src/main/resources/data/forever_skyblock/loot_table/blocks/{safe}.json", StringComparison.OrdinalIgnoreCase);
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
