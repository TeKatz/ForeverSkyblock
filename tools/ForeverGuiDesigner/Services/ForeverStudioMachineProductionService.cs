using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ForeverGuiDesigner.Services;

public enum ForeverStudioMachineProductionChangeKind
{
    JavaBridge,
    GeneratedRegistry,
    BlockModel,
    ItemModel,
    Blockstate,
    LootTable,
    Language,
    BuildManifest
}

public sealed class ForeverStudioMachineProductionChange
{
    public required ForeverStudioMachineProductionChangeKind Kind { get; init; }
    public required string RelativePath { get; init; }
    public required string Description { get; init; }
    public required string NewText { get; init; }
    public required IReadOnlyList<string> PreviewLines { get; init; }
    public required string FullPath { get; init; }
    public required string OriginalHash { get; init; }
    public required bool ExistedBefore { get; init; }
}

public sealed record ForeverStudioMachineProductionPlan(
    string AssetId,
    string DisplayName,
    string ProductionKind,
    IReadOnlyList<ForeverStudioMachineProductionChange> Changes,
    IReadOnlyList<StudioValidationIssue> Issues)
{
    public bool CanApply => Changes.Count > 0 && !Issues.Any(issue => issue.Severity == StudioValidationSeverity.Error);
    public int ErrorCount => Issues.Count(issue => issue.Severity == StudioValidationSeverity.Error);
    public int WarningCount => Issues.Count(issue => issue.Severity == StudioValidationSeverity.Warning);
}

public sealed record ForeverStudioMachineProductionApplyResult(
    string AssetId,
    IReadOnlyList<string> WrittenPaths);

/// <summary>
/// Phase-6 production compiler. The first backend intentionally supports only a real static visual
/// block. Runtime Function Graph, recipe, GUI and animation authoring fail closed until dedicated
/// compilers exist. Review snapshots every target hash and Apply is rollback-capable across all files.
/// </summary>
public static class ForeverStudioMachineProductionService
{
    public const int ManifestSchemaVersion = 1;
    public const string ProductionKindStaticBlock = "static-block";

    private const string MissingHash = "<missing>";
    private const string GeneratedDirectory =
        "mod/src/main/resources/data/forever_skyblock/forever_studio/generated";
    private const string GeneratedRegistryPath =
        "mod/src/main/java/de/tekatz/foreverskyblock/generated/ForeverStudioGeneratedMachines.java";
    private const string MainModPath =
        "mod/src/main/java/de/tekatz/foreverskyblock/ForeverSkyblock.java";
    private const string BridgeAnchor = "        RECIPE_SERIALIZERS.register(modBus);";
    private const string BridgeLine =
        "        de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines.register(modBus); // Forever Studio generated machines";

    private static readonly JsonSerializerOptions JsonIndented = new() { WriteIndented = true };

    public static ForeverStudioMachineProductionPlan BuildPlan(
        string repoRoot,
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(catalog);

        var root = Path.GetFullPath(repoRoot.Trim());
        var issues = ForeverStudioMachineProjectService.Validate(document, catalog)
            .Where(issue => issue.Severity != StudioValidationSeverity.Info)
            .ToList();
        ValidateStaticContract(root, document, catalog, issues);
        if (HasErrors(issues))
            return Plan(document, [], issues);

        var changes = new List<ForeverStudioMachineProductionChange>();
        var model = ForeverStudioVisualAssemblyService.BuildPreviewModel(document, catalog, out _);
        var blockModelPath = $"mod/src/main/resources/assets/forever_skyblock/models/block/{document.AssetId}.json";
        var itemModelPath = $"mod/src/main/resources/assets/forever_skyblock/models/item/{document.AssetId}.json";
        var blockstatePath = $"mod/src/main/resources/assets/forever_skyblock/blockstates/{document.AssetId}.json";
        var lootPath = $"mod/src/main/resources/data/forever_skyblock/loot_table/blocks/{document.AssetId}.json";
        var langPath = "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json";
        var manifestPath = $"{GeneratedDirectory}/{document.AssetId}.json";

        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.BlockModel, blockModelPath,
            "Compile the Phase-3 visual assembly into the Minecraft block model.", SerializeBlockModel(model));
        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.ItemModel, itemModelPath,
            "Create the inventory item model for the generated block.", SerializeItemModel(document.AssetId));
        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.Blockstate, blockstatePath,
            "Create the default blockstate pointing at the generated model.", SerializeBlockstate(document.AssetId));
        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.LootTable, lootPath,
            "Make the generated block drop itself when broken.", SerializeLootTable(document.AssetId));

        try
        {
            AddChange(changes, root, ForeverStudioMachineProductionChangeKind.Language, langPath,
                "Add/update the English display name without discarding existing translations.",
                MergeLanguage(root, langPath, document.AssetId, document.DisplayName));
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidDataException)
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Language file cannot be safely merged: {ex.Message}"));
            return Plan(document, [], issues);
        }

        var existingManifest = File.Exists(ResolveRepoPath(root, manifestPath));
        var mainPath = ResolveRepoPath(root, MainModPath);
        if (!File.Exists(mainPath))
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Main mod source '{MainModPath}' is missing."));
            return Plan(document, [], issues);
        }

        var mainSource = File.ReadAllText(mainPath);
        if (!existingManifest && LooksManuallyRegistered(mainSource, document.AssetId))
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Block id '{document.AssetId}' already appears to be registered by hand in ForeverSkyblock.java."));
            return Plan(document, [], issues);
        }

        if (!mainSource.Contains(BridgeLine, StringComparison.Ordinal))
        {
            if (!mainSource.Contains(BridgeAnchor, StringComparison.Ordinal))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    "ForeverSkyblock.java no longer contains the safe generated-registry bridge anchor."));
                return Plan(document, [], issues);
            }
            AddChange(changes, root, ForeverStudioMachineProductionChangeKind.JavaBridge, MainModPath,
                "Install the one-time Forever Studio generated-machine registry bridge.",
                mainSource.Replace(BridgeAnchor, BridgeAnchor + Environment.NewLine + BridgeLine, StringComparison.Ordinal));
        }

        var builtIds = ScanGeneratedAssetIds(root);
        builtIds.Add(document.AssetId);
        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.GeneratedRegistry, GeneratedRegistryPath,
            "Regenerate the Studio-owned static block registry from built machine manifests.",
            BuildGeneratedRegistry(builtIds));

        var ownedPaths = new[]
        {
            blockModelPath, itemModelPath, blockstatePath, lootPath, langPath,
            GeneratedRegistryPath, manifestPath
        };
        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.BuildManifest, manifestPath,
            "Record the production backend and generated files for deterministic rebuilds.",
            SerializeManifest(document, ownedPaths));

        issues.Add(new StudioValidationIssue(StudioValidationSeverity.Info,
            changes.Count == 0
                ? "Static production output is already up to date."
                : $"Static production backend is ready: {changes.Count} file change(s) are planned."));
        return Plan(document, changes, issues);
    }

    public static ForeverStudioMachineProductionApplyResult Apply(ForeverStudioMachineProductionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (!plan.CanApply)
            throw new InvalidOperationException("Production plan is not applyable.");

        foreach (var change in plan.Changes)
        {
            var currentHash = File.Exists(change.FullPath) ? HashFile(change.FullPath) : MissingHash;
            if (!currentHash.Equals(change.OriginalHash, StringComparison.OrdinalIgnoreCase))
                throw new IOException($"'{change.RelativePath}' changed after Review. Refresh Review before applying.");
        }

        var token = Guid.NewGuid().ToString("N");
        var staged = new List<(ForeverStudioMachineProductionChange Change, string Temp, string? Backup)>();
        var committed = new List<(ForeverStudioMachineProductionChange Change, string? Backup)>();
        try
        {
            foreach (var change in plan.Changes)
            {
                var directory = Path.GetDirectoryName(change.FullPath)
                    ?? throw new IOException($"Cannot resolve output directory for '{change.RelativePath}'.");
                Directory.CreateDirectory(directory);
                var temp = Path.Combine(directory, $".{Path.GetFileName(change.FullPath)}.foreverstudio-{token}.tmp");
                File.WriteAllText(temp, NormalizeNewlines(change.NewText), new UTF8Encoding(false));
                string? backup = null;
                if (change.ExistedBefore)
                {
                    backup = Path.Combine(directory, $".{Path.GetFileName(change.FullPath)}.foreverstudio-{token}.bak");
                    File.Copy(change.FullPath, backup, overwrite: true);
                }
                staged.Add((change, temp, backup));
            }

            foreach (var item in staged)
            {
                File.Move(item.Temp, item.Change.FullPath, overwrite: true);
                committed.Add((item.Change, item.Backup));
            }
            foreach (var item in staged)
                if (item.Backup is not null && File.Exists(item.Backup)) File.Delete(item.Backup);

            return new ForeverStudioMachineProductionApplyResult(
                plan.AssetId,
                plan.Changes.Select(change => change.RelativePath).ToList());
        }
        catch
        {
            foreach (var item in staged)
                try { if (File.Exists(item.Temp)) File.Delete(item.Temp); } catch { }
            foreach (var item in committed.AsEnumerable().Reverse())
            {
                try
                {
                    if (item.Backup is not null && File.Exists(item.Backup))
                    {
                        File.Copy(item.Backup, item.Change.FullPath, overwrite: true);
                        File.Delete(item.Backup);
                    }
                    else if (!item.Change.ExistedBefore && File.Exists(item.Change.FullPath))
                    {
                        File.Delete(item.Change.FullPath);
                    }
                }
                catch { }
            }
            foreach (var item in staged)
            {
                try
                {
                    if (item.Backup is not null && File.Exists(item.Backup))
                    {
                        if (!File.Exists(item.Change.FullPath))
                            File.Copy(item.Backup, item.Change.FullPath, overwrite: true);
                        File.Delete(item.Backup);
                    }
                }
                catch { }
            }
            throw;
        }
    }

    private static ForeverStudioMachineProductionPlan Plan(
        ForeverStudioMachineDocument document,
        IReadOnlyList<ForeverStudioMachineProductionChange> changes,
        IReadOnlyList<StudioValidationIssue> issues)
        => new(document.AssetId, document.DisplayName, ProductionKindStaticBlock, changes, issues);

    private static bool HasErrors(IEnumerable<StudioValidationIssue> issues)
        => issues.Any(issue => issue.Severity == StudioValidationSeverity.Error);

    private static void ValidateStaticContract(
        string root,
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        List<StudioValidationIssue> issues)
    {
        var visual = document.Components.Where(component =>
            !component.Id.Equals("root", StringComparison.OrdinalIgnoreCase)).ToList();
        if (visual.Count == 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "Production requires at least one placed visual component."));
        if (document.States.Count != 1)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "The static-block backend supports exactly one semantic machine state."));

        var executableNodes = document.Behavior.Nodes.Where(node =>
            !(node.Id.Equals("machine_entry", StringComparison.OrdinalIgnoreCase) &&
              node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase))).ToList();
        if (executableNodes.Count > 0 || document.Behavior.Connections.Count > 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Function Graph contains executable behavior ({executableNodes.Count} non-core node(s), " +
                $"{document.Behavior.Connections.Count} connection(s)). A NeoForge behavior compiler is required."));
        if (document.Recipes.Count > 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"{document.Recipes.Count} recipe binding(s) require the recipe/runtime compiler."));
        if (document.Gui.Bindings.Count > 0 || !string.IsNullOrWhiteSpace(document.Gui.ScreenId))
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "GUI authoring requires menu/screen/network generation and cannot be emitted by the static-block backend."));
        if (document.Animations.Count > 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"{document.Animations.Count} animation binding(s) require a runtime renderer/block-entity backend."));

        foreach (var component in visual)
        {
            if (catalog.Find(component.LibraryItemId, component.LibraryItemVersion) is not { Kind: ForeverStudioLibraryItemKind.Asset })
                continue;
            var angle = ForeverStudioVisualAssemblyService.ReadNumber(component, "rotationAngle", 0);
            if (!new[] { -45d, -22.5d, 0d, 22.5d, 45d }.Any(candidate => Math.Abs(candidate - angle) < 0.0001))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Component '{component.DisplayName}' uses element rotation {angle.ToString("0.###", CultureInfo.InvariantCulture)}°. " +
                    "Minecraft elements support -45, -22.5, 0, 22.5 or 45 degrees."));

            var texture = ForeverStudioVisualAssemblyService.ReadString(
                component, "texture", ForeverStudioVisualAssemblyService.DefaultTexture);
            if (!TryResolveTexture(root, texture, out var texturePath, out var textureIssue))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Component '{component.DisplayName}' texture is invalid: {textureIssue}"));
            else if (!File.Exists(texturePath))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Component '{component.DisplayName}' references missing texture '{texture}'."));
        }

        if (visual.Count == 0) return;
        try
        {
            var model = ForeverStudioVisualAssemblyService.BuildPreviewModel(document, catalog, out _);
            if (model.Elements.Count == 0)
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Visual assembly produced no model elements."));
            if (model.Elements.Count > 512)
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Visual assembly exceeds the 512-element safety limit."));
            if (model.Elements.Any(element => element.From.Concat(element.To)
                    .Any(value => !double.IsFinite(value) || value < -16 || value > 32)))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    "Visual assembly contains model coordinates outside the safe Minecraft range -16..32."));
        }
        catch (Exception ex)
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Visual assembly cannot be compiled: {ex.Message}"));
        }
    }

    private static bool TryResolveTexture(
        string root,
        string resource,
        out string fullPath,
        out string issue)
    {
        fullPath = string.Empty;
        issue = string.Empty;
        if (string.IsNullOrWhiteSpace(resource) || resource.Contains("..", StringComparison.Ordinal) || resource.Any(char.IsControl))
        {
            issue = "resource path is empty or unsafe.";
            return false;
        }
        var split = resource.Split(':', 2);
        var ns = split.Length == 2 ? split[0] : "forever_skyblock";
        var path = split.Length == 2 ? split[1] : split[0];
        if (!ns.Equals("forever_skyblock", StringComparison.OrdinalIgnoreCase) ||
            !path.StartsWith("block/", StringComparison.OrdinalIgnoreCase) ||
            path.Any(character => !(char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.' or '/')))
        {
            issue = "only local forever_skyblock:block/... resources are supported.";
            return false;
        }
        fullPath = ResolveRepoPath(root, $"mod/src/main/resources/assets/forever_skyblock/textures/{path}.png");
        return true;
    }

    private static string SerializeBlockModel(MinecraftResolvedBlockModel model)
    {
        var elements = model.Elements.Select(element =>
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
        }).ToList();
        return Json(new Dictionary<string, object?>
        {
            ["parent"] = "minecraft:block/block",
            ["ambientocclusion"] = model.AmbientOcclusion,
            ["textures"] = model.Textures,
            ["elements"] = elements
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

    private static string SerializeItemModel(string id)
        => Json(new Dictionary<string, object?> { ["parent"] = $"forever_skyblock:block/{id}" });

    private static string SerializeBlockstate(string id)
        => Json(new Dictionary<string, object?>
        {
            ["variants"] = new Dictionary<string, object?>
            {
                [""] = new Dictionary<string, object?> { ["model"] = $"forever_skyblock:block/{id}" }
            }
        });

    private static string SerializeLootTable(string id)
        => Json(new Dictionary<string, object?>
        {
            ["type"] = "minecraft:block",
            ["pools"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["rolls"] = 1,
                    ["entries"] = new object[]
                    {
                        new Dictionary<string, object?>
                        {
                            ["type"] = "minecraft:item",
                            ["name"] = $"forever_skyblock:{id}"
                        }
                    },
                    ["conditions"] = new object[]
                    {
                        new Dictionary<string, object?> { ["condition"] = "minecraft:survives_explosion" }
                    }
                }
            }
        });

    private static string MergeLanguage(string root, string relativePath, string id, string displayName)
    {
        var fullPath = ResolveRepoPath(root, relativePath);
        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (File.Exists(fullPath))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(fullPath));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("en_us.json root must be an object.");
            foreach (var property in document.RootElement.EnumerateObject())
                values[property.Name] = property.Value.Clone();
        }
        values[$"block.forever_skyblock.{id}"] = JsonSerializer.SerializeToElement(displayName);
        return Json(values);
    }

    private static string SerializeManifest(ForeverStudioMachineDocument document, IReadOnlyList<string> ownedPaths)
        => Json(new Dictionary<string, object?>
        {
            ["schemaVersion"] = ManifestSchemaVersion,
            ["assetId"] = document.AssetId,
            ["displayName"] = document.DisplayName,
            ["productionKind"] = ProductionKindStaticBlock,
            ["machineProject"] = ForeverStudioMachineProjectService.RelativePathForAsset(document.AssetId),
            ["generatedFiles"] = ownedPaths.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray()
        });

    private static SortedSet<string> ScanGeneratedAssetIds(string root)
    {
        var result = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var directory = ResolveRepoPath(root, GeneratedDirectory);
        if (!Directory.Exists(directory)) return result;
        foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly).Take(512))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                var json = document.RootElement;
                if (json.TryGetProperty("schemaVersion", out var schema) && schema.GetInt32() == ManifestSchemaVersion &&
                    json.TryGetProperty("productionKind", out var kind) && kind.GetString() == ProductionKindStaticBlock &&
                    json.TryGetProperty("assetId", out var asset) && asset.GetString() is { } id && IsSafeAssetId(id))
                    result.Add(id);
            }
            catch (Exception ex) when (ex is JsonException or IOException) { }
        }
        return result;
    }

    private static string BuildGeneratedRegistry(IEnumerable<string> assetIds)
    {
        var ids = assetIds.Where(IsSafeAssetId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var b = new StringBuilder();
        b.AppendLine("package de.tekatz.foreverskyblock.generated;");
        b.AppendLine();
        b.AppendLine("import de.tekatz.foreverskyblock.ForeverSkyblock;");
        if (ids.Count > 0)
        {
            b.AppendLine("import net.minecraft.world.item.BlockItem;");
            b.AppendLine("import net.minecraft.world.level.block.Block;");
            b.AppendLine("import net.minecraft.world.level.block.SoundType;");
            b.AppendLine("import net.minecraft.world.level.block.state.BlockBehaviour;");
            b.AppendLine("import net.minecraft.world.level.material.MapColor;");
            b.AppendLine("import net.neoforged.neoforge.registries.DeferredBlock;");
            b.AppendLine("import net.neoforged.neoforge.registries.DeferredItem;");
        }
        b.AppendLine("import net.minecraft.world.item.CreativeModeTabs;");
        b.AppendLine("import net.neoforged.bus.api.IEventBus;");
        b.AppendLine("import net.neoforged.neoforge.event.BuildCreativeModeTabContentsEvent;");
        b.AppendLine("import net.neoforged.neoforge.registries.DeferredRegister;");
        b.AppendLine();
        b.AppendLine("/** Generated by Forever Studio. Do not hand-edit; rebuild via Machine Builder. */");
        b.AppendLine("public final class ForeverStudioGeneratedMachines {");
        b.AppendLine("    private static final DeferredRegister.Blocks BLOCKS = DeferredRegister.createBlocks(ForeverSkyblock.MOD_ID);");
        b.AppendLine("    private static final DeferredRegister.Items ITEMS = DeferredRegister.createItems(ForeverSkyblock.MOD_ID);");
        b.AppendLine();
        foreach (var id in ids)
        {
            var field = JavaFieldName(id);
            b.AppendLine($"    public static final DeferredBlock<Block> {field} = BLOCKS.registerSimpleBlock(\"{id}\",");
            b.AppendLine("            BlockBehaviour.Properties.of().mapColor(MapColor.METAL).strength(2.0F, 6.0F).sound(SoundType.METAL).noOcclusion());");
            b.AppendLine($"    public static final DeferredItem<BlockItem> {field}_ITEM = ITEMS.registerSimpleBlockItem({field});");
        }
        b.AppendLine();
        b.AppendLine("    private ForeverStudioGeneratedMachines() {}");
        b.AppendLine();
        b.AppendLine("    public static void register(IEventBus modBus) {");
        b.AppendLine("        BLOCKS.register(modBus);");
        b.AppendLine("        ITEMS.register(modBus);");
        b.AppendLine("        modBus.addListener(ForeverStudioGeneratedMachines::addCreativeContents);");
        b.AppendLine("    }");
        b.AppendLine();
        b.AppendLine("    private static void addCreativeContents(BuildCreativeModeTabContentsEvent event) {");
        b.AppendLine("        if (event.getTabKey() != CreativeModeTabs.FUNCTIONAL_BLOCKS) return;");
        if (ids.Count == 0)
            b.AppendLine("        // Machine Builder-generated block items are inserted here by Forever Studio.");
        else
            foreach (var id in ids) b.AppendLine($"        event.accept({JavaFieldName(id)}_ITEM.get());");
        b.AppendLine("    }");
        b.AppendLine("}");
        return b.ToString();
    }

    private static void AddChange(
        List<ForeverStudioMachineProductionChange> changes,
        string root,
        ForeverStudioMachineProductionChangeKind kind,
        string relativePath,
        string description,
        string newText)
    {
        var fullPath = ResolveRepoPath(root, relativePath);
        var normalized = NormalizeNewlines(newText);
        var existed = File.Exists(fullPath);
        if (existed && NormalizeNewlines(File.ReadAllText(fullPath)).Equals(normalized, StringComparison.Ordinal))
            return;
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

    private static bool LooksManuallyRegistered(string source, string id)
        => source.Contains($"register(\"{id}\"", StringComparison.Ordinal) ||
           source.Contains($"registerSimpleBlock(\"{id}\"", StringComparison.Ordinal);

    private static bool IsSafeAssetId(string value)
        => value.Length is > 0 and <= 128 && value.All(character =>
            char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.');

    private static string JavaFieldName(string id)
    {
        var safe = new string(id.Select(character => char.IsLetterOrDigit(character)
            ? char.ToUpperInvariant(character)
            : '_').ToArray());
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id)))[..6];
        return $"FS_{safe}_{hash}";
    }
}
