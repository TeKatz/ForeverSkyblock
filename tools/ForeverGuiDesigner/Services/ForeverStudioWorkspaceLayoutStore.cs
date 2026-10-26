using System.IO;
using System.Text.Json;

namespace ForeverGuiDesigner.Services;

internal sealed class ForeverStudioWorkspaceWindowPlacement
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsMaximized { get; set; }
    public bool IsFullscreen { get; set; }
}

internal sealed class ForeverStudioWorkspaceLayout
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; set; } = CurrentFormatVersion;
    public bool ViewportDetached { get; set; }
    public bool InspectorDetached { get; set; }
    public bool TimelineDetached { get; set; }
    public double InspectorWidth { get; set; } = 600;
    public double TimelineHeight { get; set; } = 82;
    public int InspectorTabIndex { get; set; }
    public ForeverStudioWorkspaceWindowPlacement? ViewportWindow { get; set; }
    public ForeverStudioWorkspaceWindowPlacement? InspectorWindow { get; set; }
    public ForeverStudioWorkspaceWindowPlacement? TimelineWindow { get; set; }
}

internal static class ForeverStudioWorkspaceLayoutStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private static string LayoutPath
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ForeverStudio",
            "workspace-layout.json");

    public static ForeverStudioWorkspaceLayout Load()
    {
        try
        {
            if (!File.Exists(LayoutPath))
                return new ForeverStudioWorkspaceLayout();

            var json = File.ReadAllText(LayoutPath);
            var layout = JsonSerializer.Deserialize<ForeverStudioWorkspaceLayout>(json, JsonOptions);
            if (layout is null || layout.FormatVersion != ForeverStudioWorkspaceLayout.CurrentFormatVersion)
                return new ForeverStudioWorkspaceLayout();

            layout.InspectorWidth = SanitizeRange(layout.InspectorWidth, 520, 900, 600);
            layout.TimelineHeight = SanitizeRange(layout.TimelineHeight, 68, 220, 82);
            layout.InspectorTabIndex = Math.Clamp(layout.InspectorTabIndex, 0, 3);
            SanitizePlacement(layout.ViewportWindow, 640, 420);
            SanitizePlacement(layout.InspectorWindow, 460, 520);
            SanitizePlacement(layout.TimelineWindow, 720, 180);
            return layout;
        }
        catch
        {
            return new ForeverStudioWorkspaceLayout();
        }
    }

    public static void Save(ForeverStudioWorkspaceLayout layout)
    {
        try
        {
            layout.FormatVersion = ForeverStudioWorkspaceLayout.CurrentFormatVersion;
            var directory = Path.GetDirectoryName(LayoutPath)!;
            Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(layout, JsonOptions);
            var temporaryPath = LayoutPath + ".tmp";
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, LayoutPath, overwrite: true);
        }
        catch
        {
            // Workspace layout is convenience-only. Never make Studio startup/shutdown fail because
            // the local profile is unavailable, read-only or temporarily locked.
        }
    }

    public static void Reset()
    {
        try
        {
            if (File.Exists(LayoutPath))
                File.Delete(LayoutPath);
        }
        catch
        {
            // Same fail-open rule as Save(). The editor must remain usable without local layout I/O.
        }
    }

    private static double SanitizeRange(double value, double min, double max, double fallback)
        => double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;

    private static void SanitizePlacement(
        ForeverStudioWorkspaceWindowPlacement? placement,
        double minimumWidth,
        double minimumHeight)
    {
        if (placement is null)
            return;

        placement.Left = double.IsFinite(placement.Left) ? placement.Left : 0;
        placement.Top = double.IsFinite(placement.Top) ? placement.Top : 0;
        placement.Width = SanitizeRange(placement.Width, minimumWidth, 5000, minimumWidth);
        placement.Height = SanitizeRange(placement.Height, minimumHeight, 5000, minimumHeight);
    }
}
