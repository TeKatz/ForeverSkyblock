using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Splits the visual assembly into independently baked component models for the runtime renderer.
/// The normal block model remains the inventory/static fallback, while the BlockEntity renderer can
/// transform each authored component without moving unrelated machine geometry.
/// </summary>
internal static class ForeverStudioRuntimeComponentModelFixup
{
    private const string MissingHash = "<missing>";
    private const string DeleteDescriptionPrefix = "[DELETE] ";
    private static readonly JsonSerializerOptions JsonIndented = new() { WriteIndented = true };

    internal static ForeverStudioMachineProductionPlan Normalize(
        string repoRoot,
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        ForeverStudioMachineProductionPlan plan)
    {
        if (!plan.ProductionKind.Equals(ForeverStudioRuntimeCompiler.ProductionKindRuntimeMachine, StringComparison.Ordinal))
            return plan;

        var root = Path.GetFullPath(repoRoot.Trim());
        var model = ForeverStudioVisualAssemblyService.BuildPreviewModel(document, catalog, out var componentIds);
        if (model.Elements.Count != componentIds.Count)
            throw new InvalidDataException("Visual assembly component mapping does not match the generated model element count.");

        var changes = plan.Changes.ToList();
        var intended = ReadIntendedFiles(root, document.AssetId, changes);
        var entries = new List<Dictionary<string, object?>>();
        var currentComponentPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in componentIds.Select((componentId, index) => (componentId, index))
                     .GroupBy(value => value.componentId, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var modelKey = ComponentModelKey(group.Key);
            var relativePath = $"mod/src/main/resources/assets/forever_skyblock/models/block/forever_studio/components/{document.AssetId}/{modelKey}.json";
            currentComponentPaths.Add(relativePath);
            intended.Add(relativePath);
            AddChange(changes, root, ForeverStudioMachineProductionChangeKind.BlockModel, relativePath,
                $"Compile Studio component '{group.Key}' into an independently renderable baked model.",
                SerializeBlockModel(model, group.Select(value => value.index)));

            var component = document.Components.FirstOrDefault(candidate => candidate.Id.Equals(group.Key, StringComparison.OrdinalIgnoreCase));
            var pivot = component is null
                ? new[] { 8d, 8d, 8d }
                : ForeverStudioVisualAssemblyService.ReadVector(component, "pivot", [8d, 8d, 8d]);
            entries.Add(new Dictionary<string, object?>
            {
                ["id"] = group.Key,
                ["model"] = $"forever_skyblock:block/forever_studio/components/{document.AssetId}/{modelKey}",
                ["pivot"] = pivot.Select(value => value / 16.0).ToArray()
            });
        }

        var componentManifestPath = $"mod/src/main/resources/assets/forever_skyblock/forever_studio/components/{document.AssetId}.json";
        intended.Add(componentManifestPath);
        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.BuildManifest, componentManifestPath,
            "Map authored component ids and pivots to independently baked runtime models.",
            Json(new Dictionary<string, object?>
            {
                ["schemaVersion"] = 1,
                ["machine"] = $"forever_skyblock:{document.AssetId}",
                ["components"] = entries
            }));

        AddStaleComponentDeletes(changes, root, document.AssetId, currentComponentPaths);
        ReplaceOwnershipManifest(changes, root, document, intended);
        return plan with { Changes = changes };
    }

    private static HashSet<string> ReadIntendedFiles(string root, string assetId,
        IReadOnlyList<ForeverStudioMachineProductionChange> changes)
    {
        var manifestPath = $"mod/src/main/resources/data/forever_skyblock/forever_studio/generated/{assetId}.json";
        var change = changes.LastOrDefault(candidate => candidate.RelativePath.Equals(manifestPath, StringComparison.OrdinalIgnoreCase));
        var text = change?.NewText;
        if (string.IsNullOrWhiteSpace(text))
        {
            var full = ResolveRepoPath(root, manifestPath);
            if (File.Exists(full)) text = File.ReadAllText(full);
        }

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(text))
        {
            try
            {
                using var json = JsonDocument.Parse(text);
                if (json.RootElement.TryGetProperty("generatedFiles", out var files) && files.ValueKind == JsonValueKind.Array)
                    foreach (var item in files.EnumerateArray())
                        if (item.ValueKind == JsonValueKind.String && item.GetString() is { } path)
                            result.Add(path);
            }
            catch (JsonException) { }
        }

        if (result.Count == 0)
        {
            result.UnionWith(new[]
            {
                $"mod/src/main/resources/assets/forever_skyblock/models/block/{assetId}.json",
                $"mod/src/main/resources/assets/forever_skyblock/models/item/{assetId}.json",
                $"mod/src/main/resources/assets/forever_skyblock/blockstates/{assetId}.json",
                $"mod/src/main/resources/data/forever_skyblock/loot_table/blocks/{assetId}.json",
                "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json",
                "mod/src/main/java/de/tekatz/foreverskyblock/generated/ForeverStudioGeneratedMachines.java",
                $"mod/src/main/resources/data/forever_skyblock/forever_studio/runtime/{assetId}.json",
                $"mod/src/main/resources/data/forever_skyblock/recipe/forever_studio/{assetId}.json",
                manifestPath
            });
        }
        result.Add(manifestPath);
        return result;
    }

    private static void ReplaceOwnershipManifest(List<ForeverStudioMachineProductionChange> changes, string root,
        ForeverStudioMachineDocument document, HashSet<string> intended)
    {
        var manifestPath = $"mod/src/main/resources/data/forever_skyblock/forever_studio/generated/{document.AssetId}.json";
        changes.RemoveAll(change => change.RelativePath.Equals(manifestPath, StringComparison.OrdinalIgnoreCase));
        var manifest = Json(new Dictionary<string, object?>
        {
            ["schemaVersion"] = ForeverStudioRuntimeCompiler.ManifestSchemaVersion,
            ["assetId"] = document.AssetId,
            ["displayName"] = document.DisplayName,
            ["productionKind"] = ForeverStudioRuntimeCompiler.ProductionKindRuntimeMachine,
            ["machineProject"] = ForeverStudioMachineProjectService.RelativePathForAsset(document.AssetId),
            ["generatedFiles"] = intended.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray()
        });
        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.BuildManifest, manifestPath,
            "Record runtime, visual and split component model ownership for deterministic rebuilds.", manifest);
    }

    private static void AddStaleComponentDeletes(List<ForeverStudioMachineProductionChange> changes, string root,
        string assetId, HashSet<string> currentComponentPaths)
    {
        var ownershipPath = $"mod/src/main/resources/data/forever_skyblock/forever_studio/generated/{assetId}.json";
        var fullOwnership = ResolveRepoPath(root, ownershipPath);
        if (!File.Exists(fullOwnership)) return;
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(fullOwnership));
            if (!json.RootElement.TryGetProperty("generatedFiles", out var files) || files.ValueKind != JsonValueKind.Array) return;
            var prefix = $"mod/src/main/resources/assets/forever_skyblock/models/block/forever_studio/components/{assetId}/";
            foreach (var item in files.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String || item.GetString() is not { } path) continue;
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !currentComponentPaths.Contains(path))
                    AddDelete(changes, root, path, "Remove stale Studio-owned component model output.");
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            throw new InvalidDataException($"Existing build manifest '{ownershipPath}' cannot be read safely.", ex);
        }
    }

    private static string SerializeBlockModel(MinecraftResolvedBlockModel model, IEnumerable<int> indexes)
    {
        var selected = indexes.Select(index => model.Elements[index]).Select(element =>
        {
            var result = new Dictionary<string, object?>
            {
                ["from"] = element.From,
                ["to"] = element.To,
                ["faces"] = element.Faces.ToDictionary(
                    pair => pair.Key,
                    pair => (object?)SerializeFace(pair.Value),
                    StringComparer.OrdinalIgnoreCase)
            };
            if (element.Rotation is { } rotation)
                result["rotation"] = new Dictionary<string, object?>
                {
                    ["origin"] = rotation.Origin,
                    ["axis"] = rotation.Axis,
                    ["angle"] = rotation.Angle,
                    ["rescale"] = rotation.Rescale
                };
            return result;
        }).ToArray();

        return Json(new Dictionary<string, object?>
        {
            ["parent"] = "minecraft:block/block",
            ["ambientocclusion"] = model.AmbientOcclusion,
            ["textures"] = model.Textures,
            ["elements"] = selected
        });
    }

    private static Dictionary<string, object?> SerializeFace(MinecraftModelFace face)
    {
        var result = new Dictionary<string, object?> { ["texture"] = face.Texture };
        if (face.Uv is { Length: 4 }) result["uv"] = face.Uv;
        if (face.Rotation != 0) result["rotation"] = face.Rotation;
        if (!string.IsNullOrWhiteSpace(face.CullFace)) result["cullface"] = face.CullFace;
        if (face.TintIndex is { } tint) result["tintindex"] = tint;
        return result;
    }

    private static string ComponentModelKey(string componentId)
    {
        var normalized = new string(componentId.Trim().ToLowerInvariant().Select(character =>
            char.IsLetterOrDigit(character) || character is '_' or '-' or '.' ? character : '_').ToArray()).Trim('_');
        if (string.IsNullOrWhiteSpace(normalized)) normalized = "component";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(componentId)))[..8].ToLowerInvariant();
        return $"{normalized}_{hash}";
    }

    private static void AddChange(List<ForeverStudioMachineProductionChange> changes, string root,
        ForeverStudioMachineProductionChangeKind kind, string relativePath, string description, string newText)
    {
        var fullPath = ResolveRepoPath(root, relativePath);
        var normalized = NormalizeNewlines(newText);
        var existed = File.Exists(fullPath);
        if (existed && NormalizeNewlines(File.ReadAllText(fullPath)).Equals(normalized, StringComparison.Ordinal)) return;
        changes.RemoveAll(change => change.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
        changes.Add(new ForeverStudioMachineProductionChange
        {
            Kind = kind,
            RelativePath = relativePath.Replace('\\', '/'),
            Description = description,
            NewText = normalized,
            PreviewLines = normalized.Split('\n').Take(120).ToList(),
            FullPath = fullPath,
            OriginalHash = existed ? HashFile(fullPath) : MissingHash,
            ExistedBefore = existed
        });
    }

    private static void AddDelete(List<ForeverStudioMachineProductionChange> changes, string root,
        string relativePath, string description)
    {
        var fullPath = ResolveRepoPath(root, relativePath);
        if (!File.Exists(fullPath)) return;
        changes.RemoveAll(change => change.RelativePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase));
        changes.Add(new ForeverStudioMachineProductionChange
        {
            Kind = ForeverStudioMachineProductionChangeKind.BuildManifest,
            RelativePath = relativePath.Replace('\\', '/'),
            Description = DeleteDescriptionPrefix + description,
            NewText = string.Empty,
            PreviewLines = ["<delete>"],
            FullPath = fullPath,
            OriginalHash = HashFile(fullPath),
            ExistedBefore = true
        });
    }

    private static string ResolveRepoPath(string root, string relativePath)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production output escaped the selected repository.");
        return full;
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string Json(object value)
        => JsonSerializer.Serialize(value, JsonIndented) + Environment.NewLine;

    private static string NormalizeNewlines(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
