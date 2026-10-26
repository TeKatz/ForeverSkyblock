using System.IO;
using System.Text;

namespace ForeverGuiDesigner.Services;

public sealed class ForeverStudioMachineAssociation
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public ForeverMachineTemplate? Gui { get; init; }
    public MinecraftBlockAssetDescriptor? Block { get; init; }
    public MinecraftItemAssetDescriptor? Item { get; init; }
    public IReadOnlyList<string> BlockModelReferences { get; init; } = [];

    public bool HasGui => Gui is not null;
    public bool HasBlock => Block is not null;
    public bool HasItem => Item is not null;
}

/// <summary>
/// Project-level source of truth used by Forever Studio. It keeps the lightweight summary counts used by
/// Project Home, but also retains the discovered assets and logical machine associations so later workspaces
/// no longer need to invent a separate project model.
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
    public IReadOnlyList<ForeverMachineTemplate> Screens { get; init; } = [];
    public IReadOnlyList<MinecraftBlockAssetDescriptor> Blocks { get; init; } = [];
    public IReadOnlyList<MinecraftItemAssetDescriptor> Items { get; init; } = [];
    public IReadOnlyList<string> BlockModels { get; init; } = [];
    public IReadOnlyList<string> ItemModels { get; init; } = [];
    public IReadOnlyList<string> Blockstates { get; init; } = [];
    public IReadOnlyList<string> BlockTextures { get; init; } = [];
    public IReadOnlyList<string> ItemTextures { get; init; } = [];
    public IReadOnlyList<ForeverStudioMachineAssociation> MachineAssociations { get; init; } = [];

    public int TotalModelCount => BlockModelCount + ItemModelCount;
    public int TotalTextureCount => BlockTextureCount + ItemTextureCount;
}

public static class ForeverStudioProjectIndexer
{
    public static ForeverStudioProjectIndex Build(string repoRoot, ForeverScanResult machineScan)
    {
        var assetRoot = Path.Combine(repoRoot, "mod", "src", "main", "resources", "assets", "forever_skyblock");
        var blockModels = EnumerateRelative(repoRoot, Path.Combine(assetRoot, "models", "block"), "*.json");
        var itemModels = EnumerateRelative(repoRoot, Path.Combine(assetRoot, "models", "item"), "*.json");
        var blockstates = EnumerateRelative(repoRoot, Path.Combine(assetRoot, "blockstates"), "*.json");
        var blockTextures = EnumerateRelative(repoRoot, Path.Combine(assetRoot, "textures", "block"), "*.png");
        var itemTextures = EnumerateRelative(repoRoot, Path.Combine(assetRoot, "textures", "item"), "*.png");
        var blocks = MinecraftBlockModelPreviewService.ScanBlockAssets(repoRoot);
        var items = MinecraftItemModelService.ScanItemAssets(repoRoot);
        var screens = machineScan.Templates.ToList();

        return new ForeverStudioProjectIndex
        {
            RepositoryRoot = Path.GetFullPath(repoRoot),
            ProjectName = new DirectoryInfo(Path.GetFullPath(repoRoot)).Name,
            MachineScreenCount = screens.Count,
            BlockModelCount = blockModels.Count,
            ItemModelCount = itemModels.Count,
            BlockstateCount = blockstates.Count,
            BlockTextureCount = blockTextures.Count,
            ItemTextureCount = itemTextures.Count,
            WarningCount = machineScan.Warnings.Count,
            Screens = screens,
            Blocks = blocks,
            Items = items,
            BlockModels = blockModels,
            ItemModels = itemModels,
            Blockstates = blockstates,
            BlockTextures = blockTextures,
            ItemTextures = itemTextures,
            MachineAssociations = BuildMachineAssociations(screens, blocks, items)
        };
    }

    private static IReadOnlyList<ForeverStudioMachineAssociation> BuildMachineAssociations(
        IReadOnlyList<ForeverMachineTemplate> screens,
        IReadOnlyList<MinecraftBlockAssetDescriptor> blocks,
        IReadOnlyList<MinecraftItemAssetDescriptor> items)
    {
        var screenMap = screens
            .GroupBy(screen => NormalizeId(screen.ScreenClass.Replace("Screen", string.Empty, StringComparison.OrdinalIgnoreCase)), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var blockMap = blocks
            .GroupBy(block => NormalizeId(block.Id), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var itemMap = items
            .GroupBy(item => NormalizeId(item.Id), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var ids = screenMap.Keys
            .Concat(blockMap.Keys)
            .Concat(itemMap.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new List<ForeverStudioMachineAssociation>();
        foreach (var id in ids)
        {
            screenMap.TryGetValue(id, out var screen);
            blockMap.TryGetValue(id, out var block);
            itemMap.TryGetValue(id, out var item);

            // A single GUI can represent multiple physical tiers (for example Condenser -> condenser_mark_i/_ii).
            // Prefer an exact match, then associate the most specific screen prefix with the asset id.
            if (screen is null && block is not null)
            {
                screen = screenMap
                    .Where(pair => id.StartsWith(pair.Key + "_", StringComparison.OrdinalIgnoreCase) ||
                                   pair.Key.StartsWith(id + "_", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(pair => pair.Key.Length)
                    .Select(pair => pair.Value)
                    .FirstOrDefault();
            }

            // Only call something a machine association when at least a block exists or a GUI has been found.
            // Standalone material/upgrade items remain in Items without polluting the machine list.
            if (block is null && screen is null)
                continue;

            result.Add(new ForeverStudioMachineAssociation
            {
                Id = id,
                DisplayName = screen?.DisplayName ?? block?.DisplayName ?? item?.DisplayName ?? Humanize(id),
                Gui = screen,
                Block = block,
                Item = item,
                BlockModelReferences = block?.Variants
                    .Select(variant => variant.ModelReference)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? []
            });
        }
        return result;
    }

    private static List<string> EnumerateRelative(string repoRoot, string directory, string pattern)
    {
        if (!Directory.Exists(directory))
            return [];
        try
        {
            return Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static string NormalizeId(string value)
    {
        var source = Path.GetFileNameWithoutExtension(value).Replace('-', '_').Replace(' ', '_');
        var builder = new StringBuilder(source.Length + 8);
        for (var i = 0; i < source.Length; i++)
        {
            var current = source[i];
            if (char.IsUpper(current) && i > 0 && source[i - 1] != '_' && !char.IsUpper(source[i - 1]))
                builder.Append('_');
            builder.Append(char.ToLowerInvariant(current));
        }
        return string.Join('_', builder.ToString().Split('_', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Humanize(string id)
        => string.Join(' ', id.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));
}
