using System.IO;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Lightweight project-level index used by the Forever Studio launcher/home shell.
/// The existing ForeverProjectScanner remains authoritative for Java machine screens;
/// this index adds the asset categories needed by future block/item workspaces.
/// </summary>
public sealed class ForeverStudioProjectIndex
{
    public string RepositoryRoot { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public int MachineScreenCount { get; init; }
    public int BlockModelCount { get; init; }
    public int ItemModelCount { get; init; }
    public int BlockstateCount { get; init; }
    public int BlockTextureCount { get; init; }
    public int ItemTextureCount { get; init; }
    public int WarningCount { get; init; }

    public int TotalModelCount => BlockModelCount + ItemModelCount;
    public int TotalTextureCount => BlockTextureCount + ItemTextureCount;
}

public static class ForeverStudioProjectIndexer
{
    public static ForeverStudioProjectIndex Build(string repoRoot, ForeverScanResult machineScan)
    {
        var assetRoot = Path.Combine(repoRoot, "mod", "src", "main", "resources", "assets", "forever_skyblock");
        return new ForeverStudioProjectIndex
        {
            RepositoryRoot = Path.GetFullPath(repoRoot),
            ProjectName = new DirectoryInfo(Path.GetFullPath(repoRoot)).Name,
            MachineScreenCount = machineScan.Templates.Count,
            BlockModelCount = CountFiles(Path.Combine(assetRoot, "models", "block"), "*.json"),
            ItemModelCount = CountFiles(Path.Combine(assetRoot, "models", "item"), "*.json"),
            BlockstateCount = CountFiles(Path.Combine(assetRoot, "blockstates"), "*.json"),
            BlockTextureCount = CountFiles(Path.Combine(assetRoot, "textures", "block"), "*.png"),
            ItemTextureCount = CountFiles(Path.Combine(assetRoot, "textures", "item"), "*.png"),
            WarningCount = machineScan.Warnings.Count
        };
    }

    private static int CountFiles(string directory, string pattern)
    {
        if (!Directory.Exists(directory))
            return 0;

        try
        {
            return Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories).Count();
        }
        catch (IOException)
        {
            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }
}
