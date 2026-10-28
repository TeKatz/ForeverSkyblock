using System.Diagnostics;
using System.Runtime.CompilerServices;
using ForeverGuiDesigner.Services;

internal static class RepositorySyncLanguageContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        if (!GitAvailable())
            return;

        var root = Path.Combine(Path.GetTempPath(), "forever studio language sync " + Guid.NewGuid().ToString("N"));
        var remote = Path.Combine(root, "origin.git");
        var seed = Path.Combine(root, "seed");
        var work = Path.Combine(root, "work");
        try
        {
            Directory.CreateDirectory(root);
            RunGit(root, "init", "--bare", remote);
            Directory.CreateDirectory(seed);
            RunGit(seed, "init", "-b", "main");
            RunGit(seed, "config", "user.email", "studio-contract@example.invalid");
            RunGit(seed, "config", "user.name", "Forever Studio Contract");

            Write(seed, "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json",
                "{\n  \"tooltip.forever_skyblock.fixture\": \"§aReady\"\n}\n");
            RunGit(seed, "add", ".");
            RunGit(seed, "commit", "-m", "main language");
            RunGit(seed, "remote", "add", "origin", remote);
            RunGit(seed, "push", "-u", "origin", "main");

            RunGit(seed, "switch", "-c", ForeverStudioRepositorySyncService.DevelopmentBranch);
            Write(seed, ForeverStudioRuntimeRepositoryContract.RelativeMarkerPath, """
package de.tekatz.foreverskyblock.studio.runtime;
public final class ForeverStudioRuntimeContract {
    public static final int COMPILER_CONTRACT_VERSION = 2;
}
""");
            RunGit(seed, "add", ".");
            RunGit(seed, "commit", "-m", "runtime contract");
            RunGit(seed, "push", "-u", "origin", ForeverStudioRepositorySyncService.DevelopmentBranch);

            RunGit(root, "clone", "--branch", "main", remote, work);
            Write(work,
                "mod/src/main/resources/data/forever_skyblock/forever_studio/machines/redstone.json",
                "{\"assetId\":\"redstone\"}\n");
            Write(work, "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json",
                "{\n  \"tooltip.forever_skyblock.fixture\": \"\\u00A7aReady\",\n  \"block.forever_skyblock.redstone\": \"Redstone\"\n}\n");

            var inspection = ForeverStudioRepositorySyncService.InspectAsync(
                    work, "redstone", ForeverStudioRepositorySyncService.DevelopmentBranch)
                .GetAwaiter().GetResult();
            Check(inspection.CanUpdate,
                "Equivalent JSON string escaping plus a Studio-owned machine translation must not block sync: " + inspection.Error);
            Check(!inspection.ManualChanges.Any(change =>
                    change.Path == "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json"),
                "Semantically unchanged language values must remain Studio-safe even after serializer escaping changes.");
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
