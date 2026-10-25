using System.IO;
using System.Text.Json;

namespace ForeverGuiDesigner.Services;

public sealed class MinecraftItemAssetDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
}

public sealed class MinecraftItemDisplayTransform
{
    public double[] Rotation { get; init; } = [0, 0, 0];
    public double[] Translation { get; init; } = [0, 0, 0];
    public double[] Scale { get; init; } = [1, 1, 1];
}

public sealed class MinecraftResolvedItemModel
{
    public string Id { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string? ParentReference { get; init; }
    public string? TerminalParentReference { get; init; }
    public string? BlockModelReference { get; init; }
    public string? Layer0TextureReference { get; init; }
    public string? Layer0TexturePath { get; init; }
    public Dictionary<string, string> Textures { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, MinecraftItemDisplayTransform> DisplayTransforms { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsGeneratedItem => BlockModelReference is null;
}

public static class MinecraftItemModelService
{
    private const string Namespace = "forever_skyblock";

    public static IReadOnlyList<MinecraftItemAssetDescriptor> ScanItemAssets(string repoRoot)
    {
        var root = Path.Combine(GetAssetRoot(repoRoot), "models", "item");
        if (!Directory.Exists(root))
            return [];

        return Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
            .Select(path => new MinecraftItemAssetDescriptor
            {
                Id = Path.GetRelativePath(root, path)
                    .Replace('\\', '/')
                    .Replace(".json", string.Empty, StringComparison.OrdinalIgnoreCase),
                DisplayName = Humanize(Path.GetFileNameWithoutExtension(path)),
                SourcePath = Path.GetRelativePath(repoRoot, path).Replace('\\', '/')
            })
            .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static MinecraftResolvedItemModel LoadResolvedItem(string repoRoot, string id)
    {
        var reference = $"{Namespace}:item/{id}";
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resolved = ResolveModelChain(repoRoot, reference, visited);
        var ownPath = ResolveModelPath(repoRoot, reference)
            ?? throw new FileNotFoundException($"Item model '{reference}' was not found.");

        using var ownDocument = JsonDocument.Parse(File.ReadAllText(ownPath));
        var ownParent = ownDocument.RootElement.TryGetProperty("parent", out var parentProperty) && parentProperty.ValueKind == JsonValueKind.String
            ? parentProperty.GetString()
            : null;

        resolved.Textures.TryGetValue("layer0", out var layer0);
        var layer0Path = ResolveTexturePath(repoRoot, layer0);

        return new MinecraftResolvedItemModel
        {
            Id = id,
            SourcePath = Path.GetRelativePath(repoRoot, ownPath).Replace('\\', '/'),
            ParentReference = ownParent,
            TerminalParentReference = resolved.TerminalParent,
            BlockModelReference = resolved.BlockModelReference,
            Layer0TextureReference = layer0,
            Layer0TexturePath = layer0Path,
            Textures = resolved.Textures,
            DisplayTransforms = resolved.DisplayTransforms
        };
    }

    private static ResolvedChain ResolveModelChain(string repoRoot, string reference, HashSet<string> visited)
    {
        if (!visited.Add(reference))
            throw new InvalidDataException($"Circular item model parent chain detected at '{reference}'.");

        if (reference.StartsWith("minecraft:", StringComparison.OrdinalIgnoreCase))
        {
            return new ResolvedChain
            {
                TerminalParent = reference
            };
        }

        var path = ResolveModelPath(repoRoot, reference);
        if (path is null || !File.Exists(path))
        {
            return new ResolvedChain
            {
                TerminalParent = reference,
                BlockModelReference = reference.Contains(":block/", StringComparison.OrdinalIgnoreCase) ? reference : null
            };
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var parentReference = root.TryGetProperty("parent", out var parentProperty) && parentProperty.ValueKind == JsonValueKind.String
            ? parentProperty.GetString()
            : null;

        ResolvedChain inherited;
        if (!string.IsNullOrWhiteSpace(parentReference))
            inherited = ResolveModelChain(repoRoot, NormalizeReference(parentReference!), visited);
        else
            inherited = new ResolvedChain { TerminalParent = reference };

        var textures = new Dictionary<string, string>(inherited.Textures, StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("textures", out var texturesProperty) && texturesProperty.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in texturesProperty.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() is { } value)
                    textures[property.Name] = value;
            }
        }

        var display = inherited.DisplayTransforms.ToDictionary(
            pair => pair.Key,
            pair => CloneTransform(pair.Value),
            StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("display", out var displayProperty) && displayProperty.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in displayProperty.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object)
                    display[property.Name] = ParseTransform(property.Value);
            }
        }

        var normalizedCurrent = NormalizeReference(reference);
        var blockReference = normalizedCurrent.Contains(":block/", StringComparison.OrdinalIgnoreCase)
            ? normalizedCurrent
            : inherited.BlockModelReference;

        return new ResolvedChain
        {
            TerminalParent = inherited.TerminalParent ?? parentReference ?? reference,
            BlockModelReference = blockReference,
            Textures = textures,
            DisplayTransforms = display
        };
    }

    private static MinecraftItemDisplayTransform ParseTransform(JsonElement element)
        => new()
        {
            Rotation = ParseVector(element, "rotation", [0, 0, 0]),
            Translation = ParseVector(element, "translation", [0, 0, 0]),
            Scale = ParseVector(element, "scale", [1, 1, 1])
        };

    private static double[] ParseVector(JsonElement owner, string name, double[] fallback)
    {
        if (!owner.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.Array)
            return [.. fallback];
        var values = property.EnumerateArray()
            .Take(3)
            .Select(value => value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) ? number : 0)
            .ToList();
        while (values.Count < 3)
            values.Add(fallback[values.Count]);
        return [values[0], values[1], values[2]];
    }

    private static MinecraftItemDisplayTransform CloneTransform(MinecraftItemDisplayTransform source)
        => new()
        {
            Rotation = [.. source.Rotation],
            Translation = [.. source.Translation],
            Scale = [.. source.Scale]
        };

    private static string? ResolveModelPath(string repoRoot, string reference)
    {
        var normalized = NormalizeReference(reference);
        var colon = normalized.IndexOf(':');
        var ns = colon >= 0 ? normalized[..colon] : Namespace;
        var resource = colon >= 0 ? normalized[(colon + 1)..] : normalized;
        if (!ns.Equals(Namespace, StringComparison.OrdinalIgnoreCase))
            return null;
        return Path.Combine(GetAssetRoot(repoRoot), "models", resource.Replace('/', Path.DirectorySeparatorChar) + ".json");
    }

    private static string? ResolveTexturePath(string repoRoot, string? textureReference)
    {
        if (string.IsNullOrWhiteSpace(textureReference) || textureReference.StartsWith('#'))
            return null;
        var colon = textureReference.IndexOf(':');
        var ns = colon >= 0 ? textureReference[..colon] : Namespace;
        var resource = colon >= 0 ? textureReference[(colon + 1)..] : textureReference;
        if (!ns.Equals(Namespace, StringComparison.OrdinalIgnoreCase))
            return null;
        var path = Path.Combine(GetAssetRoot(repoRoot), "textures", resource.Replace('/', Path.DirectorySeparatorChar) + ".png");
        return File.Exists(path) ? path : null;
    }

    private static string NormalizeReference(string reference)
    {
        if (reference.Contains(':'))
            return reference;
        if (reference.StartsWith("block/", StringComparison.OrdinalIgnoreCase) ||
            reference.StartsWith("item/", StringComparison.OrdinalIgnoreCase))
            return $"{Namespace}:{reference}";
        return $"{Namespace}:{reference}";
    }

    private static string GetAssetRoot(string repoRoot)
        => Path.Combine(repoRoot, "mod", "src", "main", "resources", "assets", Namespace);

    private static string Humanize(string id)
        => string.Join(' ', id.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));

    private sealed class ResolvedChain
    {
        public string? TerminalParent { get; init; }
        public string? BlockModelReference { get; init; }
        public Dictionary<string, string> Textures { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, MinecraftItemDisplayTransform> DisplayTransforms { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
