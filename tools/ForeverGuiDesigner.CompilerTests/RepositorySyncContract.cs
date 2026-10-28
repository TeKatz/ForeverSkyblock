using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using ForeverGuiDesigner.Services;

internal static class RepositorySyncContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        Check(ForeverStudioRepositorySyncService.IsStudioManagedPath(
                "mod/src/main/resources/data/forever_skyblock/forever_studio/machines/redstone.json", "redstone"),
            "Machine Project source must be recognized as Studio-managed.");
        Check(ForeverStudioRepositorySyncService.IsStudioManagedPath(
                "mod/src/main/resources/assets/forever_skyblock/blockstates/redstone.json", "redstone"),
            "Generated blockstate must be recognized as Studio-managed.");
        Check(!ForeverStudioRepositorySyncService.IsStudioManagedPath(
                "mod/src/main/java/de/tekatz/foreverskyblock/some/ManualThing.java", "redstone"),
            "Unrelated Java source must never be classified as Studio-managed.");

        if (!GitAvailable())
            return;

        var root = Path.Combine(Path.GetTempPath(), "forever studio repository sync " + Guid.NewGuid().ToString("N"));
        var remote = Path.Combine(root, "origin.git");
        var seed = Path.Combine(root, "seed repo");
        var work = Path.Combine(root, "working repo");
        try
        {
            Directory.CreateDirectory(root);
            RunGit(root, "init", "--bare", remote);
            Directory.CreateDirectory(seed);
            RunGit(seed, "init", "-b", "main");
            RunGit(seed, "config", "user.email", "studio-contract@example.invalid");
            RunGit(seed, "config", "user.name", "Forever Studio Contract");

            Write(seed, "mod/src/main/java/de/tekatz/foreverskyblock/ForeverSkyblock.java", "old main\n");
            Write(seed, "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json", """
{
  "item.forever_skyblock.fixture": "Fixture",
  "item.forever_skyblock.local_override": "Original",
  "tooltip.forever_skyblock.formatting": "§aReady"
}
""");
            RunGit(seed, "add", ".");
            RunGit(seed, "commit", "-m", "old main");
            RunGit(seed, "remote", "add", "origin", remote);
            RunGit(seed, "push", "-u", "origin", "main");

            RunGit(seed, "switch", "-c", ForeverStudioRepositorySyncService.DevelopmentBranch);
            Write(seed, ForeverStudioRuntimeRepositoryContract.RelativeMarkerPath, """
package de.tekatz.foreverskyblock.studio.runtime;
public final class ForeverStudioRuntimeContract {
    public static final int COMPILER_CONTRACT_VERSION = 2;
}
""");
            Write(seed, "mod/src/main/java/de/tekatz/foreverskyblock/ForeverSkyblock.java", "feature runtime\n");
            Write(seed, "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json", """
{
  "item.forever_skyblock.fixture": "Feature Fixture",
  "item.forever_skyblock.local_override": "Original",
  "tooltip.forever_skyblock.formatting": "§aReady",
  "item.forever_skyblock.remote_only": "Runtime Feature"
}
""");
            RunGit(seed, "add", ".");
            RunGit(seed, "commit", "-m", "runtime feature");
            RunGit(seed, "push", "-u", "origin", ForeverStudioRepositorySyncService.DevelopmentBranch);

            RunGit(root, "clone", "--branch", "main", remote, work);
            Write(work,
                "mod/src/main/resources/data/forever_skyblock/forever_studio/machines/redstone.json",
                "{\"assetId\":\"redstone\",\"local\":true}\n");
            File.AppendAllText(Path.Combine(work, "mod", "src", "main", "java", "de", "tekatz", "foreverskyblock", "ForeverSkyblock.java"),
                "        de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines.register(modBus); // Forever Studio generated machines\n");
            Write(work, "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json", """
{
  "item.forever_skyblock.fixture": "Fixture",
  "item.forever_skyblock.local_override": "Local Override",
  "tooltip.forever_skyblock.formatting": "\u00A7aReady",
  "block.forever_skyblock.redstone": "Redstone"
}
""");
            Write(work, "mod/src/main/resources/assets/forever_skyblock/blockstates/redstone.json", "{\"generated\":true}\n");

            var inspection = ForeverStudioRepositorySyncService.InspectAsync(
                    work, "redstone", ForeverStudioRepositorySyncService.DevelopmentBranch)
                .GetAwaiter().GetResult();
            Check(inspection.CanUpdate, "Studio output plus mergeable language overrides must allow repository sync: " + inspection.Error);
            Check(inspection.NeedsUpdate, "Old main checkout must require an update to the runtime feature branch.");
            Check(inspection.ManualChanges.Count == 0,
                "Mergeable language overrides and Studio-owned generated files must not be reported as manual changes.");

            var result = ForeverStudioRepositorySyncService.SynchronizeAsync(
                    work, "redstone", ForeverStudioRepositorySyncService.DevelopmentBranch)
                .GetAwaiter().GetResult();
            Check(result.Succeeded && result.Updated, "Safe repository sync must complete successfully: " + result.Error);
            Check(RunGit(work, "branch", "--show-current").Trim() == ForeverStudioRepositorySyncService.DevelopmentBranch,
                "Repository sync must switch to the required runtime branch.");
            Check(File.ReadAllText(Path.Combine(work,
                    "mod", "src", "main", "resources", "data", "forever_skyblock", "forever_studio", "machines", "redstone.json"))
                    .Contains("\"local\":true", StringComparison.Ordinal),
                "Local Machine Project source must survive repository sync.");
            Check(!File.Exists(Path.Combine(work,
                    "mod", "src", "main", "resources", "assets", "forever_skyblock", "blockstates", "redstone.json")),
                "Stale generated output must be discarded so it can be regenerated against the new runtime.");
            var runtimeSource = File.ReadAllText(Path.Combine(work,
                    "mod", "src", "main", "java", "de", "tekatz", "foreverskyblock", "ForeverSkyblock.java"))
                .Replace("\r\n", "\n", StringComparison.Ordinal);
            Check(runtimeSource == "feature runtime\n",
                "Remote runtime source must win over stale generated bridge edits.");

            var languagePath = Path.Combine(work,
                "mod", "src", "main", "resources", "assets", "forever_skyblock", "lang", "en_us.json");
            using (var language = JsonDocument.Parse(File.ReadAllText(languagePath)))
            {
                var rootElement = language.RootElement;
                Check(!rootElement.TryGetProperty("block.forever_skyblock.redstone", out _),
                    "Stale Studio-owned language output must be discarded before regeneration.");
                Check(rootElement.GetProperty("item.forever_skyblock.local_override").GetString() == "Local Override",
                    "Local non-Studio language override must survive repository sync.");
                Check(rootElement.GetProperty("item.forever_skyblock.fixture").GetString() == "Feature Fixture",
                    "Unmodified local keys must receive the target branch value.");
                Check(rootElement.GetProperty("item.forever_skyblock.remote_only").GetString() == "Runtime Feature",
                    "Target-only language keys must survive local override replay.");
                Check(rootElement.GetProperty("tooltip.forever_skyblock.formatting").GetString() == "§aReady",
                    "Semantically identical Unicode escaping differences must not create a language override delta.");
            }

            Check(ForeverStudioRuntimeRepositoryContract.Validate(work) is null,
                "Updated checkout must satisfy the runtime repository contract.");

            Write(work, "manual-user-file.txt", "do not touch\n");
            var blocked = ForeverStudioRepositorySyncService.InspectAsync(
                    work, "redstone", ForeverStudioRepositorySyncService.DevelopmentBranch)
                .GetAwaiter().GetResult();
            Check(blocked.ManualChanges.Any(change => change.Path == "manual-user-file.txt"),
                "Unknown local files must still be classified as manual changes.");
            File.Delete(Path.Combine(work, "manual-user-file.txt"));

            Write(work, "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json", "{ not-json\n");
            var malformedLanguage = ForeverStudioRepositorySyncService.InspectAsync(
                    work, "redstone", ForeverStudioRepositorySyncService.DevelopmentBranch)
                .GetAwaiter().GetResult();
            Check(malformedLanguage.ManualChanges.Any(change =>
                    change.Path == "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json"),
                "Malformed language JSON must remain a hard sync blocker.");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static bool GitAvailable()
    {
        try
        {
            var start = new ProcessStartInfo("git")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            start.ArgumentList.Add("--version");
            using var process = Process.Start(start);
            if (process is null) return false;
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string RunGit(string workingDirectory, params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start git.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed ({process.ExitCode}): {stderr}");
        return stdout;
    }

    private static void Write(string root, string relative, string content)
    {
        var path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
