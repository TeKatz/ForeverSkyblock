using System.Runtime.CompilerServices;
using ForeverGuiDesigner.Services;

internal static class GradleServiceContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        Check(ForeverStudioGradleService.TaskFor(ForeverStudioGradleOperation.BuildModJar) == "build",
            "Build Mod JAR must map to the Gradle build task.");
        Check(ForeverStudioGradleService.TaskFor(ForeverStudioGradleOperation.LaunchTestClient) == "runClient",
            "Launch Test Client must map to the Gradle runClient task.");

        if (!OperatingSystem.IsWindows())
            return;

        // The directory deliberately contains spaces: this is the real-world path shape that broke
        // the first cmd.exe wrapper implementation and must remain covered by the contract test.
        var root = Path.Combine(Path.GetTempPath(), "forever studio gradle contract " + Guid.NewGuid().ToString("N"));
        try
        {
            var mod = Path.Combine(root, "mod");
            Directory.CreateDirectory(mod);
            File.WriteAllText(Path.Combine(mod, "build.gradle"), "// fixture");
            File.WriteAllText(Path.Combine(mod, "gradlew.bat"), """
@echo off
if /I "%1"=="build" (
  echo FAKE_GRADLE_BUILD
  if not exist "build\libs" mkdir "build\libs"
  > "build\libs\ForeverSkyblock-contract-1.0.0.jar" echo runtime
  > "build\libs\ForeverSkyblock-contract-1.0.0-sources.jar" echo sources
  exit /b 0
)
if /I "%1"=="runClient" (
  echo FAKE_GRADLE_CLIENT
  exit /b 0
)
exit /b 7
""");

            Check(ForeverStudioGradleService.ValidateRepository(root) is null,
                "Gradle service fixture repository must validate.");
            var startInfo = ForeverStudioGradleService.CreateStartInfo(root, ForeverStudioGradleOperation.BuildModJar);
            Check(Path.GetFullPath(startInfo.WorkingDirectory) == Path.GetFullPath(mod),
                "Gradle service must execute from the mod directory.");
            Check(!startInfo.UseShellExecute && startInfo.RedirectStandardOutput && startInfo.RedirectStandardError,
                "Gradle service must capture stdout/stderr without shell execution mode.");

            var buildLog = new CaptureProgress();
            var result = ForeverStudioGradleService.RunAsync(root, ForeverStudioGradleOperation.BuildModJar, buildLog)
                .GetAwaiter().GetResult();
            Check(result.Succeeded && result.ExitCode == 0,
                $"Fake Gradle build must complete successfully (exit {result.ExitCode}). Log: {buildLog.Joined}");
            Check(buildLog.Lines.Any(line => line.Contains("FAKE_GRADLE_BUILD", StringComparison.Ordinal)),
                "Gradle stdout must be streamed to the caller.");
            Check(result.BuiltJars.Count == 1,
                "JAR discovery must exclude source artifacts.");
            Check(Path.GetFileName(result.PrimaryJarPath) == "ForeverSkyblock-contract-1.0.0.jar",
                "JAR discovery must select the distributable runtime artifact.");

            var clientLog = new CaptureProgress();
            var client = ForeverStudioGradleService.RunAsync(root, ForeverStudioGradleOperation.LaunchTestClient, clientLog)
                .GetAwaiter().GetResult();
            Check(client.Succeeded && client.PrimaryJarPath is null,
                $"Fake runClient task must complete without claiming a JAR output. Log: {clientLog.Joined}");
            Check(clientLog.Lines.Any(line => line.Contains("FAKE_GRADLE_CLIENT", StringComparison.Ordinal)),
                "runClient stdout must be streamed to the caller.");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private sealed class CaptureProgress : IProgress<string>
    {
        private readonly object _gate = new();
        private readonly List<string> _lines = [];

        public IReadOnlyList<string> Lines
        {
            get
            {
                lock (_gate)
                    return _lines.ToArray();
            }
        }

        public string Joined
        {
            get
            {
                lock (_gate)
                    return string.Join(" | ", _lines);
            }
        }

        public void Report(string value)
        {
            lock (_gate)
                _lines.Add(value);
        }
    }
}
