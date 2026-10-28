using System.IO;
using System.Text;
using System.Windows;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Records otherwise unhandled Forever Studio exceptions without changing normal crash semantics.
/// Pure UI-thread hangs cannot be captured here, but exception-driven failures leave actionable data
/// for the next investigation instead of disappearing without context.
/// </summary>
public static class StudioCrashDiagnostics
{
    private static readonly object Gate = new();
    private static bool _installed;

    public static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ForeverGuiDesigner",
        "studio-crash.log");

    public static void Install(Application application)
    {
        if (_installed)
            return;
        _installed = true;

        application.DispatcherUnhandledException += (_, eventArgs) =>
            Write("DispatcherUnhandledException", eventArgs.Exception);

        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            var exception = eventArgs.ExceptionObject as Exception;
            Write(
                eventArgs.IsTerminating ? "AppDomain.UnhandledException terminating" : "AppDomain.UnhandledException",
                exception,
                eventArgs.ExceptionObject?.ToString());
        };

        TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
            Write("TaskScheduler.UnobservedTaskException", eventArgs.Exception);
    }

    private static void Write(string source, Exception? exception, string? fallback = null)
    {
        try
        {
            lock (Gate)
            {
                var path = LogPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                var builder = new StringBuilder();
                builder.AppendLine("================================================================================");
                builder.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] {source}");
                builder.AppendLine($"Process: {Environment.ProcessId} · Thread: {Environment.CurrentManagedThreadId}");
                builder.AppendLine($"Version: {SelfUpdateService.ShortCommit(SelfUpdateService.CurrentCommit)}");
                if (exception is not null)
                    builder.AppendLine(exception.ToString());
                else if (!string.IsNullOrWhiteSpace(fallback))
                    builder.AppendLine(fallback);
                else
                    builder.AppendLine("No exception object was supplied.");

                File.AppendAllText(path, builder.ToString());
            }
        }
        catch
        {
            // Diagnostics must never interfere with Studio shutdown or exception propagation.
        }
    }
}
