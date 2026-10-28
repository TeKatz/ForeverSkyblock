using System.Diagnostics;

namespace ForeverGuiDesigner.Services;

public enum ForeverStudioGradleOperation
{
    BuildModJar,
    LaunchTestClient
}

public sealed record ForeverStudioGradleResult(
    ForeverStudioGradleOperation Operation,
    int ExitCode,
    bool Cancelled,
    IReadOnlyList<string> BuiltJars,
    string? PrimaryJarPath,
    TimeSpan Duration)
{
    public bool Succeeded => !Cancelled && ExitCode == 0;
}

/// <summary>
/// Runs the repository-owned Gradle wrapper for Studio production tasks. The service deliberately
/// owns no Machine Builder state: callers prepare/apply the Studio transaction first, then invoke
/// Gradle against the selected local repository. Output is streamed line-by-line and cancellation
/// terminates the complete wrapper process tree.
/// </summary>
public static class ForeverStudioGradleService
{
    public const string ModDirectoryName = "mod";
    public const string BuildTask = "build";
    public const string RunClientTask = "runClient";

    private static readonly string[] NonDistributionJarSuffixes =
    [
        "-sources.jar",
        "-javadoc.jar",
        "-dev.jar",
        "-tests.jar",
        "-test.jar"
    ];

    public static string TaskFor(ForeverStudioGradleOperation operation)
        => operation switch
        {
            ForeverStudioGradleOperation.BuildModJar => BuildTask,
            ForeverStudioGradleOperation.LaunchTestClient => RunClientTask,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };

    public static string ResolveModDirectory(string repoRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);
        return Path.Combine(Path.GetFullPath(repoRoot.Trim()), ModDirectoryName);
    }

    public static string ResolveGradleWrapperPath(string repoRoot)
        => Path.Combine(ResolveModDirectory(repoRoot), OperatingSystem.IsWindows() ? "gradlew.bat" : "gradlew");

    public static string? ValidateRepository(string repoRoot)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
            return "No repository is selected.";

        string root;
        try
        {
            root = Path.GetFullPath(repoRoot.Trim());
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return $"The repository path is invalid: {ex.Message}";
        }

        if (!Directory.Exists(root))
            return $"Repository directory does not exist: {root}";

        var modDirectory = Path.Combine(root, ModDirectoryName);
        if (!Directory.Exists(modDirectory))
            return $"The repository has no '{ModDirectoryName}' directory.";

        if (!File.Exists(Path.Combine(modDirectory, "build.gradle")) &&
            !File.Exists(Path.Combine(modDirectory, "build.gradle.kts")))
            return "The mod directory has no Gradle build file.";

        var wrapper = ResolveGradleWrapperPath(root);
        if (!File.Exists(wrapper))
            return $"Gradle wrapper is missing: {wrapper}";

        return null;
    }

    public static ProcessStartInfo CreateStartInfo(string repoRoot, ForeverStudioGradleOperation operation)
    {
        var validation = ValidateRepository(repoRoot);
        if (validation is not null)
            throw new InvalidOperationException(validation);

        var modDirectory = ResolveModDirectory(repoRoot);
        var wrapper = ResolveGradleWrapperPath(repoRoot);
        var task = TaskFor(operation);
        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = modDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            startInfo.ArgumentList.Add("/d");
            startInfo.ArgumentList.Add("/s");
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add($"call \"{wrapper}\" {task} --console=plain");
        }
        else
        {
            startInfo.FileName = wrapper;
            startInfo.ArgumentList.Add(task);
            startInfo.ArgumentList.Add("--console=plain");
        }

        return startInfo;
    }

    public static IReadOnlyList<string> FindBuiltJars(string repoRoot)
    {
        var libs = Path.Combine(ResolveModDirectory(repoRoot), "build", "libs");
        if (!Directory.Exists(libs))
            return [];

        return Directory.EnumerateFiles(libs, "*.jar", SearchOption.TopDirectoryOnly)
            .Where(path => !NonDistributionJarSuffixes.Any(suffix =>
                Path.GetFileName(path).EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ThenBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static async Task<ForeverStudioGradleResult> RunAsync(
        string repoRoot,
        ForeverStudioGradleOperation operation,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.StartNew();
        var startInfo = CreateStartInfo(repoRoot, operation);
        progress?.Report($"> {TaskFor(operation)}");
        progress?.Report($"> Working directory: {startInfo.WorkingDirectory}");

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
                throw new InvalidOperationException("Gradle process could not be started.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new InvalidOperationException($"Could not start the Gradle wrapper: {ex.Message}", ex);
        }

        var stdoutTask = PumpAsync(process.StandardOutput, progress, prefix: null);
        var stderrTask = PumpAsync(process.StandardError, progress, prefix: "! ");
        var cancelled = 0;
        using var registration = cancellationToken.Register(() =>
        {
            Interlocked.Exchange(ref cancelled, 1);
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch
            {
                // A racing normal exit wins over cancellation; the final result below is authoritative.
            }
        });

        await process.WaitForExitAsync().ConfigureAwait(false);
        await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
        started.Stop();

        var wasCancelled = Volatile.Read(ref cancelled) != 0 || cancellationToken.IsCancellationRequested;
        var jars = operation == ForeverStudioGradleOperation.BuildModJar && process.ExitCode == 0 && !wasCancelled
            ? FindBuiltJars(repoRoot)
            : [];
        return new ForeverStudioGradleResult(
            operation,
            process.ExitCode,
            wasCancelled,
            jars,
            jars.FirstOrDefault(),
            started.Elapsed);
    }

    private static async Task PumpAsync(StreamReader reader, IProgress<string>? progress, string? prefix)
    {
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            if (line.Length == 0)
                continue;
            progress?.Report(prefix is null ? line : prefix + line);
        }
    }
}
