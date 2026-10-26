using System.IO;
using System.Text.Json;

namespace ForeverGuiDesigner.Services;

public sealed class MinecraftBlockAssetDescriptor
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string BlockstatePath { get; init; } = string.Empty;
    public List<MinecraftBlockVariantDescriptor> Variants { get; init; } = [];
}

public sealed class MinecraftBlockVariantDescriptor
{
    public string Key { get; init; } = string.Empty;
    public Dictionary<string, string> Properties { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string ModelReference { get; init; } = string.Empty;
    public int XRotation { get; init; }
    public int YRotation { get; init; }
    public bool UvLock { get; init; }

    public string StateLabel
    {
        get
        {
            var values = Properties
                .Where(pair => !pair.Key.Equals("facing", StringComparison.OrdinalIgnoreCase))
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(pair => $"{pair.Key}={pair.Value}")
                .ToArray();
            return values.Length == 0 ? "default" : string.Join(", ", values);
        }
    }

    public string Facing => Properties.TryGetValue("facing", out var value) ? value : "default";
}

public sealed class MinecraftResolvedBlockModel
{
    public string ModelReference { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string? ParentReference { get; init; }
    public bool AmbientOcclusion { get; init; } = true;
    public Dictionary<string, string> Textures { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<MinecraftModelElement> Elements { get; init; } = [];
}

public sealed class MinecraftModelElement
{
    public double[] From { get; init; } = [0, 0, 0];
    public double[] To { get; init; } = [16, 16, 16];
    public MinecraftModelElementRotation? Rotation { get; init; }
    public Dictionary<string, MinecraftModelFace> Faces { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class MinecraftModelElementRotation
{
    public double[] Origin { get; init; } = [8, 8, 8];
    public string Axis { get; init; } = "y";
    public double Angle { get; init; }
    public bool Rescale { get; init; }
}

public sealed class MinecraftModelFace
{
    public string Texture { get; init; } = string.Empty;
    public double[]? Uv { get; init; }
    public int Rotation { get; init; }
    public string? CullFace { get; init; }
    public int? TintIndex { get; init; }
}

public static class MinecraftBlockModelPreviewService
{
    public static IReadOnlyList<MinecraftBlockAssetDescriptor> ScanBlockAssets(string repoRoot)
    {
        var assetRoot = GetAssetRoot(repoRoot);
        var blockstateRoot = Path.Combine(assetRoot, "blockstates");
        if (!Directory.Exists(blockstateRoot))
            return [];

        var result = new List<MinecraftBlockAssetDescriptor>();
        foreach (var path in Directory.EnumerateFiles(blockstateRoot, "*.json", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var variants = ParseVariants(document.RootElement);
                var id = Path.GetFileNameWithoutExtension(path);

                if (variants.Count == 0)
                {
                    var fallbackModel = $"forever_skyblock:block/{id}";
                    if (File.Exists(ResolveModelPath(repoRoot, fallbackModel)))
                    {
                        variants.Add(new MinecraftBlockVariantDescriptor
                        {
                            Key = "default",
                            ModelReference = fallbackModel
                        });
                    }
                }

                if (variants.Count == 0)
                    continue;

                result.Add(new MinecraftBlockAssetDescriptor
                {
                    Id = id,
                    DisplayName = HumanizeId(id),
                    BlockstatePath = Path.GetRelativePath(repoRoot, path).Replace('\\', '/'),
                    Variants = variants
                });
            }
            catch (JsonException)
            {
                // Invalid blockstates are skipped by the read-only viewer; validation is a later phase.
            }
            catch (IOException)
            {
                // Keep the rest of the project browsable when one file is temporarily unavailable.
            }
        }

        return result;
    }

    public static MinecraftResolvedBlockModel LoadResolvedModel(string repoRoot, string modelReference)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return LoadResolvedModelCore(repoRoot, NormalizeModelReference(modelReference), visited);
    }

    public static string? ResolveTexturePath(string repoRoot, MinecraftResolvedBlockModel model, string faceTexture)
    {
        var resource = ResolveTextureResource(model.Textures, faceTexture);
        if (string.IsNullOrWhiteSpace(resource))
            return null;

        var colon = resource.IndexOf(':');
        var resourceNamespace = colon >= 0 ? resource[..colon] : "forever_skyblock";
        var resourcePath = colon >= 0 ? resource[(colon + 1)..] : resource;
        if (!resourceNamespace.Equals("forever_skyblock", StringComparison.OrdinalIgnoreCase))
            return null;

        var assetRoot = GetAssetRoot(repoRoot);
        var texturePath = Path.Combine(
            assetRoot,
            "textures",
            resourcePath.Replace('/', Path.DirectorySeparatorChar) + ".png");
        return File.Exists(texturePath) ? texturePath : null;
    }

    public static string? ResolveTextureResource(IReadOnlyDictionary<string, string> textures, string textureReference)
    {
        var current = textureReference;
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < 32 && !string.IsNullOrWhiteSpace(current); i++)
        {
            if (!current.StartsWith('#'))
                return current;

            var key = current[1..];
            if (!visited.Add(key) || !textures.TryGetValue(key, out current))
                return null;
        }

        return null;
    }

    private static MinecraftResolvedBlockModel LoadResolvedModelCore(
        string repoRoot,
        string modelReference,
        HashSet<string> visited)
    {
        if (!visited.Add(modelReference))
            throw new InvalidDataException($"Circular Minecraft model parent chain detected at '{modelReference}'.");

        var modelPath = ResolveModelPath(repoRoot, modelReference);
        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"Minecraft model '{modelReference}' was not found.", modelPath);

        using var document = JsonDocument.Parse(File.ReadAllText(modelPath));
        var root = document.RootElement;
        var parentReference = root.TryGetProperty("parent", out var parentProperty)
            ? parentProperty.GetString()
            : null;

        MinecraftResolvedBlockModel? parent = null;
        if (!string.IsNullOrWhiteSpace(parentReference) &&
            !parentReference.StartsWith("minecraft:", StringComparison.OrdinalIgnoreCase))
        {
            var normalizedParent = NormalizeModelReference(parentReference);
            var parentPath = ResolveModelPath(repoRoot, normalizedParent);
            if (File.Exists(parentPath))
                parent = LoadResolvedModelCore(repoRoot, normalizedParent, visited);
        }

        var textures = parent is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(parent.Textures, StringComparer.OrdinalIgnoreCase);

        if (root.TryGetProperty("textures", out var texturesProperty) && texturesProperty.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in texturesProperty.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String && property.Value.GetString() is { } value)
                    textures[property.Name] = value;
            }
        }

        List<MinecraftModelElement> elements;
        if (root.TryGetProperty("elements", out var elementsProperty) && elementsProperty.ValueKind == JsonValueKind.Array)
            elements = elementsProperty.EnumerateArray().Select(ParseElement).ToList();
        else
            elements = parent?.Elements.Select(CloneElement).ToList() ?? [];

        var ambientOcclusion = root.TryGetProperty("ambientocclusion", out var aoProperty) &&
                               aoProperty.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? aoProperty.GetBoolean()
            : parent?.AmbientOcclusion ?? true;

        return new MinecraftResolvedBlockModel
        {
            ModelReference = modelReference,
            SourcePath = Path.GetRelativePath(repoRoot, modelPath).Replace('\\', '/'),
            ParentReference = parentReference,
            AmbientOcclusion = ambientOcclusion,
            Textures = textures,
            Elements = elements
        };
    }

    private static List<MinecraftBlockVariantDescriptor> ParseVariants(JsonElement root)
    {
        var result = new List<MinecraftBlockVariantDescriptor>();
        if (!root.TryGetProperty("variants", out var variants) || variants.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var property in variants.EnumerateObject())
        {
            var candidate = property.Value;
            if (candidate.ValueKind == JsonValueKind.Array)
            {
                candidate = candidate.EnumerateArray().FirstOrDefault();
                if (candidate.ValueKind == JsonValueKind.Undefined)
                    continue;
            }

            if (candidate.ValueKind != JsonValueKind.Object ||
                !candidate.TryGetProperty("model", out var modelProperty) ||
                modelProperty.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(modelProperty.GetString()))
                continue;

            result.Add(new MinecraftBlockVariantDescriptor
            {
                Key = string.IsNullOrWhiteSpace(property.Name) ? "default" : property.Name,
                Properties = ParseVariantProperties(property.Name),
                ModelReference = NormalizeModelReference(modelProperty.GetString()!),
                XRotation = ReadInt(candidate, "x"),
                YRotation = ReadInt(candidate, "y"),
                UvLock = candidate.TryGetProperty("uvlock", out var uvLock) && uvLock.ValueKind == JsonValueKind.True
            });
        }

        return result;
    }

    private static Dictionary<string, string> ParseVariantProperties(string key)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(key))
            return result;

        foreach (var part in key.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pieces = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (pieces.Length == 2 && !string.IsNullOrWhiteSpace(pieces[0]))
                result[pieces[0]] = pieces[1];
        }
        return result;
    }

    private static MinecraftModelElement ParseElement(JsonElement element)
    {
        var faces = new Dictionary<string, MinecraftModelFace>(StringComparer.OrdinalIgnoreCase);
        if (element.TryGetProperty("faces", out var facesProperty) && facesProperty.ValueKind == JsonValueKind.Object)
        {
            foreach (var faceProperty in facesProperty.EnumerateObject())
            {
                var face = faceProperty.Value;
                faces[faceProperty.Name] = new MinecraftModelFace
                {
                    Texture = face.TryGetProperty("texture", out var texture) && texture.ValueKind == JsonValueKind.String
                        ? texture.GetString() ?? string.Empty
                        : string.Empty,
                    Uv = face.TryGetProperty("uv", out var uv) && uv.ValueKind == JsonValueKind.Array
                        ? ReadVector(uv, 4)
                        : null,
                    Rotation = ReadInt(face, "rotation"),
                    CullFace = face.TryGetProperty("cullface", out var cullFace) && cullFace.ValueKind == JsonValueKind.String
                        ? cullFace.GetString()
                        : null,
                    TintIndex = face.TryGetProperty("tintindex", out var tintIndex) && tintIndex.TryGetInt32(out var tint)
                        ? tint
                        : null
                };
            }
        }

        return new MinecraftModelElement
        {
            From = element.TryGetProperty("from", out var from) && from.ValueKind == JsonValueKind.Array
                ? ReadVector(from, 3)
                : [0, 0, 0],
            To = element.TryGetProperty("to", out var to) && to.ValueKind == JsonValueKind.Array
                ? ReadVector(to, 3)
                : [16, 16, 16],
            Rotation = element.TryGetProperty("rotation", out var rotation) && rotation.ValueKind == JsonValueKind.Object
                ? ParseElementRotation(rotation)
                : null,
            Faces = faces
        };
    }

    private static MinecraftModelElementRotation ParseElementRotation(JsonElement rotation)
        => new()
        {
            Origin = rotation.TryGetProperty("origin", out var origin) && origin.ValueKind == JsonValueKind.Array
                ? ReadVector(origin, 3)
                : [8, 8, 8],
            Axis = rotation.TryGetProperty("axis", out var axis) && axis.ValueKind == JsonValueKind.String
                ? axis.GetString() ?? "y"
                : "y",
            Angle = rotation.TryGetProperty("angle", out var angle) && angle.TryGetDouble(out var value) ? value : 0,
            Rescale = rotation.TryGetProperty("rescale", out var rescale) && rescale.ValueKind == JsonValueKind.True
        };

    private static MinecraftModelElement CloneElement(MinecraftModelElement source)
        => new()
        {
            From = [.. source.From],
            To = [.. source.To],
            Rotation = source.Rotation is null
                ? null
                : new MinecraftModelElementRotation
                {
                    Origin = [.. source.Rotation.Origin],
                    Axis = source.Rotation.Axis,
                    Angle = source.Rotation.Angle,
                    Rescale = source.Rotation.Rescale
                },
            Faces = source.Faces.ToDictionary(
                pair => pair.Key,
                pair => new MinecraftModelFace
                {
                    Texture = pair.Value.Texture,
                    Uv = pair.Value.Uv is null ? null : [.. pair.Value.Uv],
                    Rotation = pair.Value.Rotation,
                    CullFace = pair.Value.CullFace,
                    TintIndex = pair.Value.TintIndex
                },
                StringComparer.OrdinalIgnoreCase)
        };

    private static double[] ReadVector(JsonElement array, int expected)
    {
        var values = array.EnumerateArray()
            .Take(expected)
            .Select(value => value.TryGetDouble(out var number) ? number : 0)
            .ToList();
        while (values.Count < expected)
            values.Add(0);
        return values.ToArray();
    }

    private static int ReadInt(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.TryGetInt32(out var value) ? value : 0;

    private static string ResolveModelPath(string repoRoot, string modelReference)
    {
        var normalized = NormalizeModelReference(modelReference);
        var colon = normalized.IndexOf(':');
        var modelNamespace = colon >= 0 ? normalized[..colon] : "forever_skyblock";
        var modelPath = colon >= 0 ? normalized[(colon + 1)..] : normalized;

        if (!modelNamespace.Equals("forever_skyblock", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(repoRoot, "__external_model__", modelNamespace, modelPath + ".json");

        return Path.Combine(
            GetAssetRoot(repoRoot),
            "models",
            modelPath.Replace('/', Path.DirectorySeparatorChar) + ".json");
    }

    private static string NormalizeModelReference(string modelReference)
    {
        var value = modelReference.Trim();
        if (value.Contains(':'))
            return value;
        return $"forever_skyblock:{value}";
    }

    private static string GetAssetRoot(string repoRoot)
        => Path.Combine(repoRoot, "mod", "src", "main", "resources", "assets", "forever_skyblock");

    private static string HumanizeId(string value)
    {
        var words = value.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Length == 1
                ? word.ToUpperInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..]);
        return string.Join(' ', words);
    }
}