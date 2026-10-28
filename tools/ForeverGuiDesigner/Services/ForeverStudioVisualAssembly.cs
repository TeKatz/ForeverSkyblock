using System.Text.Json;

namespace ForeverGuiDesigner.Services;

public sealed record ForeverStudioVisualCuboid(
    double X,
    double Y,
    double Z,
    double Width,
    double Height,
    double Depth);

/// <summary>
/// Phase-3 bridge between reusable visual-library assets and the canonical Machine Builder document.
/// Component properties are the source of truth; the 3D preview is deterministically rebuilt from
/// those properties instead of keeping a second, preview-only model.
/// </summary>
public static class ForeverStudioVisualAssemblyService
{
    public const string DefaultTexture = "forever_skyblock:block/ascension_materials";

    private static readonly string[] FaceDirections =
        ["down", "up", "north", "south", "west", "east"];

    public static ForeverStudioMachineComponent CreateComponent(
        ForeverStudioLibraryItem asset,
        IEnumerable<ForeverStudioMachineComponent> existing)
    {
        if (asset.Kind != ForeverStudioLibraryItemKind.Asset)
            throw new ArgumentException("Only Asset library items can become visual components.", nameof(asset));

        var baseId = SanitizeComponentId(asset.Id.StartsWith("asset.", StringComparison.OrdinalIgnoreCase)
            ? asset.Id[6..]
            : asset.Id);
        if (string.IsNullOrWhiteSpace(baseId))
            baseId = "component";

        var used = existing.Select(component => component.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var id = baseId;
        for (var suffix = 2; used.Contains(id); suffix++)
            id = $"{baseId}_{suffix}";

        var defaults = DefaultBounds(asset.PreviewKey);
        return new ForeverStudioMachineComponent
        {
            Id = id,
            DisplayName = asset.DisplayName,
            LibraryItemId = asset.Id,
            LibraryItemVersion = asset.Version,
            ParentId = "root",
            Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
            {
                ["position"] = JsonSerializer.SerializeToElement(new[] { defaults.X, defaults.Y, defaults.Z }),
                ["size"] = JsonSerializer.SerializeToElement(new[] { defaults.Width, defaults.Height, defaults.Depth }),
                ["pivot"] = JsonSerializer.SerializeToElement(new[]
                {
                    defaults.X + defaults.Width / 2,
                    defaults.Y + defaults.Height / 2,
                    defaults.Z + defaults.Depth / 2
                }),
                ["rotationAxis"] = JsonSerializer.SerializeToElement("y"),
                ["rotationAngle"] = JsonSerializer.SerializeToElement(0d),
                ["texture"] = JsonSerializer.SerializeToElement(DefaultTexture),
                ["visible"] = JsonSerializer.SerializeToElement(true),
                ["previewKey"] = JsonSerializer.SerializeToElement(asset.PreviewKey)
            }
        };
    }

    public static MinecraftResolvedBlockModel BuildPreviewModel(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        out List<string> elementComponentIds)
    {
        elementComponentIds = [];
        var elements = new List<MinecraftModelElement>();
        var textures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var textureIndex = 0;

        foreach (var component in document.Components)
        {
            if (component.Id.Equals("root", StringComparison.OrdinalIgnoreCase) || !ReadBoolean(component, "visible", true))
                continue;

            var asset = catalog.Find(component.LibraryItemId, component.LibraryItemVersion);
            if (asset is null || asset.Kind != ForeverStudioLibraryItemKind.Asset)
                continue;

            var position = ReadVector(component, "position", [0, 0, 0]);
            var size = ReadVector(component, "size", [16, 16, 16]);
            var pivot = ReadVector(component, "pivot",
                [position[0] + size[0] / 2, position[1] + size[1] / 2, position[2] + size[2] / 2]);
            var axis = ReadString(component, "rotationAxis", "y");
            if (axis is not ("x" or "y" or "z"))
                axis = "y";
            var angle = Clamp(ReadNumber(component, "rotationAngle", 0), -360, 360);
            var texture = ReadString(component, "texture", DefaultTexture);
            if (string.IsNullOrWhiteSpace(texture))
                texture = DefaultTexture;

            var textureKey = $"component_{textureIndex++}";
            textures[textureKey] = texture;
            var previewKey = ReadString(component, "previewKey", asset.PreviewKey);
            foreach (var primitive in Shape(previewKey))
            {
                var from = new[]
                {
                    position[0] + size[0] * primitive.X,
                    position[1] + size[1] * primitive.Y,
                    position[2] + size[2] * primitive.Z
                };
                var to = new[]
                {
                    from[0] + size[0] * primitive.Width,
                    from[1] + size[1] * primitive.Height,
                    from[2] + size[2] * primitive.Depth
                };

                if (to[0] - from[0] <= 0.0001 || to[1] - from[1] <= 0.0001 || to[2] - from[2] <= 0.0001)
                    continue;

                var faces = FaceDirections.ToDictionary(
                    direction => direction,
                    _ => new MinecraftModelFace { Texture = $"#{textureKey}" },
                    StringComparer.OrdinalIgnoreCase);

                elements.Add(new MinecraftModelElement
                {
                    From = from,
                    To = to,
                    Rotation = Math.Abs(angle) < 0.0001
                        ? null
                        : new MinecraftModelElementRotation
                        {
                            Origin = [.. pivot],
                            Axis = axis,
                            Angle = angle,
                            Rescale = false
                        },
                    Faces = faces
                });
                elementComponentIds.Add(component.Id);
            }
        }

        return new MinecraftResolvedBlockModel
        {
            ModelReference = $"forever_studio:machine_builder/{document.AssetId}",
            SourcePath = $"forever_studio/machines/{document.AssetId}.preview",
            AmbientOcclusion = true,
            Textures = textures,
            Elements = elements
        };
    }

    public static IReadOnlyList<string> ScanBlockTextures(string? repoRoot)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
            return [];

        string root;
        try
        {
            root = Path.Combine(
                Path.GetFullPath(repoRoot.Trim()),
                "mod", "src", "main", "resources", "assets", "forever_skyblock", "textures", "block");
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return [];
        }

        if (!Directory.Exists(root))
            return [];

        try
        {
            return Directory.EnumerateFiles(root, "*.png", SearchOption.AllDirectories)
                .Take(1024)
                .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
                .Select(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? path[..^4] : path)
                .Select(path => $"forever_skyblock:block/{path}")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    public static double[] ReadVector(
        ForeverStudioMachineComponent component,
        string key,
        double[] fallback)
    {
        if (!component.Properties.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.Array)
            return [.. fallback];

        var values = value.EnumerateArray()
            .Take(3)
            .Select(item => item.TryGetDouble(out var number) && double.IsFinite(number) ? number : 0)
            .ToList();
        while (values.Count < 3)
            values.Add(fallback[Math.Min(values.Count, fallback.Length - 1)]);
        return values.ToArray();
    }

    public static double ReadNumber(ForeverStudioMachineComponent component, string key, double fallback)
        => component.Properties.TryGetValue(key, out var value) && value.TryGetDouble(out var number) && double.IsFinite(number)
            ? number
            : fallback;

    public static string ReadString(ForeverStudioMachineComponent component, string key, string fallback)
        => component.Properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    public static bool ReadBoolean(ForeverStudioMachineComponent component, string key, bool fallback)
        => component.Properties.TryGetValue(key, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : fallback;

    public static void WriteVector(ForeverStudioMachineComponent component, string key, params double[] values)
        => component.Properties[key] = JsonSerializer.SerializeToElement(values.Take(3).ToArray());

    public static void WriteNumber(ForeverStudioMachineComponent component, string key, double value)
        => component.Properties[key] = JsonSerializer.SerializeToElement(value);

    public static void WriteString(ForeverStudioMachineComponent component, string key, string value)
        => component.Properties[key] = JsonSerializer.SerializeToElement(value ?? string.Empty);

    public static void WriteBoolean(ForeverStudioMachineComponent component, string key, bool value)
        => component.Properties[key] = JsonSerializer.SerializeToElement(value);

    private static (double X, double Y, double Z, double Width, double Height, double Depth) DefaultBounds(string? previewKey)
        => (previewKey ?? string.Empty).ToLowerInvariant() switch
        {
            "panel" => (3, 3, 1, 10, 10, 1),
            "chamber" => (3, 2, 3, 10, 12, 10),
            "tank" => (4, 2, 4, 8, 12, 8),
            "pipe" => (6, 6, 1, 4, 4, 14),
            "conveyor" => (1, 3, 2, 14, 4, 12),
            "tray" => (2, 2, 1, 12, 4, 14),
            "rotor" or "gear" or "fan" => (4, 4, 7, 8, 8, 2),
            "shaft" => (7, 2, 7, 2, 12, 2),
            "mixer" or "drill" => (6, 2, 6, 4, 12, 4),
            "press" => (3, 7, 3, 10, 4, 10),
            "piston" => (6, 3, 6, 4, 10, 4),
            "door" or "screen" or "vent" => (3, 2, 1, 10, 12, 1),
            "valve" => (5, 5, 6, 6, 6, 4),
            "nozzle" => (6, 3, 6, 4, 8, 4),
            "coil" or "core" => (4, 4, 4, 8, 8, 8),
            "lamp" or "port" => (6, 6, 1, 4, 4, 2),
            "platform" => (1, 1, 1, 14, 3, 14),
            "frame" => (1, 1, 1, 14, 14, 14),
            _ => (1, 1, 1, 14, 14, 14)
        };

    private static IReadOnlyList<ForeverStudioVisualCuboid> Shape(string? previewKey)
        => (previewKey ?? string.Empty).ToLowerInvariant() switch
        {
            "frame" => FrameShape(),
            "conveyor" =>
            [
                new(0, 0.10, 0, 1, 0.35, 1),
                new(0, 0.45, 0, 0.10, 0.18, 1),
                new(0.90, 0.45, 0, 0.10, 0.18, 1)
            ],
            "tray" =>
            [
                new(0, 0, 0, 1, 0.22, 1),
                new(0, 0.22, 0, 0.10, 0.35, 1),
                new(0.90, 0.22, 0, 0.10, 0.35, 1),
                new(0.10, 0.22, 0.90, 0.80, 0.35, 0.10)
            ],
            "rotor" or "fan" =>
            [
                new(0.38, 0.38, 0, 0.24, 0.24, 1),
                new(0.05, 0.43, 0.08, 0.90, 0.14, 0.84),
                new(0.43, 0.05, 0.08, 0.14, 0.90, 0.84)
            ],
            "gear" =>
            [
                new(0.22, 0.22, 0, 0.56, 0.56, 1),
                new(0, 0.40, 0.08, 1, 0.20, 0.84),
                new(0.40, 0, 0.08, 0.20, 1, 0.84)
            ],
            "mixer" =>
            [
                new(0.40, 0, 0.40, 0.20, 0.75, 0.20),
                new(0.05, 0.72, 0.40, 0.90, 0.16, 0.20),
                new(0.40, 0.72, 0.05, 0.20, 0.16, 0.90)
            ],
            "drill" =>
            [
                new(0.34, 0, 0.34, 0.32, 0.72, 0.32),
                new(0.25, 0.72, 0.25, 0.50, 0.16, 0.50),
                new(0.38, 0.88, 0.38, 0.24, 0.12, 0.24)
            ],
            "tank" or "chamber" =>
            [
                new(0.08, 0, 0.08, 0.84, 0.10, 0.84),
                new(0.12, 0.10, 0.12, 0.76, 0.80, 0.76),
                new(0.08, 0.90, 0.08, 0.84, 0.10, 0.84)
            ],
            "piston" =>
            [
                new(0.30, 0, 0.30, 0.40, 0.72, 0.40),
                new(0.08, 0.72, 0.08, 0.84, 0.28, 0.84)
            ],
            "valve" =>
            [
                new(0.38, 0.38, 0, 0.24, 0.24, 1),
                new(0.08, 0.43, 0.18, 0.84, 0.14, 0.64)
            ],
            "nozzle" =>
            [
                new(0.20, 0, 0.20, 0.60, 0.40, 0.60),
                new(0.30, 0.40, 0.30, 0.40, 0.35, 0.40),
                new(0.40, 0.75, 0.40, 0.20, 0.25, 0.20)
            ],
            "coil" =>
            [
                new(0.18, 0.18, 0.18, 0.64, 0.64, 0.64),
                new(0, 0.42, 0.42, 1, 0.16, 0.16),
                new(0.42, 0, 0.42, 0.16, 1, 0.16),
                new(0.42, 0.42, 0, 0.16, 0.16, 1)
            ],
            "vent" =>
            [
                new(0, 0, 0, 1, 1, 0.20),
                new(0.10, 0.18, 0, 0.80, 0.08, 0.28),
                new(0.10, 0.46, 0, 0.80, 0.08, 0.28),
                new(0.10, 0.74, 0, 0.80, 0.08, 0.28)
            ],
            _ => [new(0, 0, 0, 1, 1, 1)]
        };

    private static IReadOnlyList<ForeverStudioVisualCuboid> FrameShape()
    {
        const double t = 0.12;
        var result = new List<ForeverStudioVisualCuboid>();
        foreach (var x in new[] { 0d, 1 - t })
        foreach (var z in new[] { 0d, 1 - t })
            result.Add(new(x, 0, z, t, 1, t));
        foreach (var y in new[] { 0d, 1 - t })
        {
            foreach (var z in new[] { 0d, 1 - t })
                result.Add(new(t, y, z, 1 - 2 * t, t, t));
            foreach (var x in new[] { 0d, 1 - t })
                result.Add(new(x, y, t, t, t, 1 - 2 * t));
        }
        return result;
    }

    private static string SanitizeComponentId(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) || character is '_' or '-' ? character : '_')
            .ToArray();
        return new string(chars).Trim('_');
    }

    private static double Clamp(double value, double min, double max)
        => Math.Max(min, Math.Min(max, value));
}
