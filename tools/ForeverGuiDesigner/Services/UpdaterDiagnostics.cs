using System.Diagnostics;
using System.IO;

namespace ForeverGuiDesigner.Services;

public static class UpdaterDiagnostics
{
    private static readonly object Gate = new();

    public static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ForeverGuiDesigner",
        "updater.log");

    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                var path = LogPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(
                    path,
                    $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [T{Environment.CurrentManagedThreadId}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostics must never interfere with the editor itself.
        }
    }

    public static void OpenLog()
    {
        try
        {
            var path = LogPath;
            if (!File.Exists(path))
                Write("Updater log opened before any updater activity was recorded.");

            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }
}
