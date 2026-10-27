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
    internal required string FullPath { get; init; }
    internal required string OriginalHash { get; init; }
    internal required bool ExistedBefore { get; init; }
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
/// Phase-6 production compiler for Machine Builder projects. The first production backend is deliberately
/// strict: a visual/static machine can be compiled into a real registered NeoForge block; executable
/// behavior, menu/recipe runtime and dynamic animation contracts fail closed until dedicated compilers exist.
/// Every planned write is hash-snapshotted and applied as one rollback-capable transaction.
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
    private const string BridgeLine =
        "        de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines.register(modBus); // Forever Studio generated machines";
    private const string BridgeAnchor = "        RECIPE_SERIALIZERS.register(modBus);";

    private static readonly JsonSerializerOptions JsonIndented = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
        var changes = new List<ForeverStudioMachineProductionChange>();

        ValidateStaticProductionContract(root, document, catalog, issues);
        if (issues.Any(issue => issue.Severity == StudioValidationSeverity.Error))
            return new ForeverStudioMachineProductionPlan(
                document.AssetId,
                document.DisplayName,
                ProductionKindStaticBlock,
                changes,
                issues);

        var model = ForeverStudioVisualAssemblyService.BuildPreviewModel(document, catalog, out _);
        var blockModelPath = $"mod/src/main/resources/assets/forever_skyblock/models/block/{document.AssetId}.json";
        var itemModelPath = $"mod/src/main/resources/assets/forever_skyblock/models/item/{document.AssetId}.json";
        var blockstatePath = $"mod/src/main/resources/assets/forever_skyblock/blockstates/{document.AssetId}.json";
        var lootPath = $"mod/src/main/resources/data/forever_skyblock/loot_table/blocks/{document.AssetId}.json";
        var langPath = "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json";
        var manifestPath = $"{GeneratedDirectory}/{document.AssetId}.json";

        changes.Add(CreateChange(root, ForeverStudioMachineProductionChangeKind.BlockModel, blockModelPath,
            "Compile the Phase-3 visual assembly into the Minecraft block model.", SerializeBlockModel(model)));
        changes.Add(CreateChange(root, ForeverStudioMachineProductionChangeKind.ItemModel, itemModelPath,
            "Create the inventory item model for the generated block.", SerializeItemModel(document.AssetId)));
        changes.Add(CreateChange(root, ForeverStudioMachineProductionChangeKind.Blockstate, blockstatePath,
            "Create the default blockstate pointing at the generated model.", SerializeBlockstate(document.AssetId)));
        changes.Add(CreateChange(root, ForeverStudioMachineProductionChangeKind.LootTable, lootPath,
            "Make the generated block drop itself when broken.", SerializeLootTable(document.AssetId)));

        string language;
        try
        {
            language = MergeLanguage(root, langPath, document.AssetId, document.DisplayName);
        }
        catch (Exception ex) when (ex is JsonException or IOException or InvalidDataException)
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Language file cannot be safely merged: {ex.Message}"));
            return new ForeverStudioMachineProductionPlan(document.AssetId, document.DisplayName,
                ProductionKindStaticBlock, [], issues);
        }
        changes.Add(CreateChange(root, ForeverStudioMachineProductionChangeKind.Language, langPath,
            "Add/update the English display name without discarding existing translations.", language));

        var builtAssetIds = ScanGeneratedAssetIds(root);
        builtAssetIds.Add(document.AssetId);
        var collisions = builtAssetIds
            .GroupBy(JavaFieldName, StringComparer.Ordinal)
            .Where(group => group.Select(value => value).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .ToList();
        if (collisions.Count > 0)
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "Generated Java registry identifiers would collide: " +
                string.Join("; ", collisions.Select(group => string.Join(", ", group))))) ;
            return new ForeverStudioMachineProductionPlan(document.AssetId, document.DisplayName,
                ProductionKindStaticBlock, [], issues);
        }

        var mainFullPath = ResolveRepoPath(root, MainModPath);
        if (!File.Exists(mainFullPath))
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Main mod source '{MainModPath}' is missing."));
            return new ForeverStudioMachineProductionPlan(document.AssetId, document.DisplayName,
                ProductionKindStaticBlock, [], issues);
        }

        var mainSource = File.ReadAllText(mainFullPath);
        if (LooksManuallyRegistered(mainSource, document.AssetId) && !builtAssetIds.Any(id =>
                id.Equals(document.AssetId, StringComparison.OrdinalIgnoreCase) &&
                File.Exists(ResolveRepoPath(root, manifestPath))))
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Block id '{document.AssetId}' already appears to be registered by hand in ForeverSkyblock.java."));
            return new ForeverStudioMachineProductionPlan(document.AssetId, document.DisplayName,
                ProductionKindStaticBlock, [], issues);
        }

        if (!mainSource.Contains(BridgeLine, StringComparison.Ordinal))
        {
            if (!mainSource.Contains(BridgeAnchor, StringComparison.Ordinal))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    "ForeverSkyblock.java no longer contains the safe generated-registry bridge anchor."));
                return new ForeverStudioMachineProductionPlan(document.AssetId, document.DisplayName,
                    ProductionKindStaticBlock, [], issues);
            }
            var patched = mainSource.Replace(
                BridgeAnchor,
                BridgeAnchor + Environment.NewLine + BridgeLine,
                StringComparison.Ordinal);
            changes.Add(CreateChange(root, ForeverStudioMachineProductionChangeKind.JavaBridge, MainModPath,
                "Install the one-time Forever Studio generated-machine registry bridge.", patched));
        }

        changes.Add(CreateChange(root, ForeverStudioMachineProductionChangeKind.GeneratedRegistry, GeneratedRegistryPath,
            "Regenerate the Studio-owned static block registry from built machine manifests.",
            BuildGeneratedRegistry(builtAssetIds)));

        var generatedPaths = changes.Select(change => change.RelativePath)
            .Append(manifestPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        changes.Add(CreateChange(root, ForeverStudioMachineProductionChangeKind.BuildManifest, manifestPath,
            "Record the production backend and generated files for deterministic rebuilds.",
            SerializeManifest(document, generatedPaths)));

        issues.Add(new StudioValidationIssue(StudioValidationSeverity.Info,
            $"Static production backend is ready: {changes.Count} file change(s) are planned."));
        return new ForeverStudioMachineProductionPlan(
            document.AssetId,
            document.DisplayName,
            ProductionKindStaticBlock,
            changes,
            issues);
    }

    public static ForeverStudioMachineProductionApplyResult Apply(
        ForeverStudioMachineProductionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        if (!plan.CanApply)
            throw new InvalidOperationException("Production plan contains blocking validation errors.");

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
            {
                if (item.Backup is not null && File.Exists(item.Backup))
                    File.Delete(item.Backup);
            }

            return new ForeverStudioMachineProductionApplyResult(
                plan.AssetId,
                plan.Changes.Select(change => change.RelativePath).ToList());
        }
        catch
        {
            foreach (var item in staged)
            {
                try
                {
                    if (File.Exists(item.Temp))
                        File.Delete(item.Temp);
                }
                catch { }
            }

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

    private static void ValidateStaticProductionContract(
        string root,
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        List<StudioValidationIssue> issues)
    {
        var visualComponents = document.Components
            .Where(component => !component.Id.Equals("root", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (visualComponents.Count == 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "Production requires at least one placed visual component."));

        if (document.States.Count != 1)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "The static-block production backend supports exactly one semantic machine state."));

        var executableNodes = document.Behavior.Nodes.Where(node =>
            !(node.Id.Equals("machine_entry", StringComparison.OrdinalIgnoreCase) &&
              node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase))).ToList();
        if (executableNodes.Count > 0 || document.Behavior.Connections.Count > 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Function Graph contains executable behavior ({executableNodes.Count} non-core node(s), {document.Behavior.Connections.Count} connection(s)). " +
                "A NeoForge runtime compiler for behavior nodes is required before this machine can be produced."));
        if (document.Recipes.Count > 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"{document.Recipes.Count} recipe binding(s) require the future recipe/runtime compiler and cannot be emitted as a static block."));
        if (document.Gui.Bindings.Count > 0 || !string.IsNullOrWhiteSpace(document.Gui.ScreenId))
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "GUI authoring is present. Menu/screen/network generation is not part of the static-block production backend."));
        if (document.Animations.Count > 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"{document.Animations.Count} animation binding(s) require a runtime renderer/block-entity backend and cannot be emitted as a static block."));

        foreach (var component in visualComponents)
        {
            var asset = catalog.Find(component.LibraryItemId, component.LibraryItemVersion);
            if (asset is not { Kind: ForeverStudioLibraryItemKind.Asset })
                continue;

            var angle = ForeverStudioVisualAssemblyService.ReadNumber(component, "rotationAngle", 0);
            var allowedAngle = new[] { -45d, -22.5d, 0d, 22.5d, 45d }
                .Any(candidate => Math.Abs(candidate - angle) < 0.0001);
            if (!allowedAngle)
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Component '{component.DisplayName}' uses element rotation {angle.ToString("0.###", CultureInfo.InvariantCulture)}°. " +
                    "Minecraft block-model element rotations must be -45, -22.5, 0, 22.5 or 45 degrees."));

            var texture = ForeverStudioVisualAssemblyService.ReadString(
                component, "texture", ForeverStudioVisualAssemblyService.DefaultTexture);
            if (!TryResolveLocalBlockTexture(root, texture, out var texturePath, out var textureIssue))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Component '{component.DisplayName}' texture is invalid: {textureIssue}"));
            }
            else if (!File.Exists(texturePath))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Component '{component.DisplayName}' references missing texture '{texture}'."));
            }
        }

        if (visualComponents.Count > 0)
        {
            try
            {
                var model = ForeverStudioVisualAssemblyService.BuildPreviewModel(document, catalog, out _);
                if (model.Elements.Count == 0)
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Visual assembly produced no model elements."));
                if (model.Elements.Count > 512)
                    issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Visual assembly exceeds the 512-element production safety limit."));
                foreach (var element in model.Elements)
                {
                    if (element.From.Concat(element.To).Any(value => !double.IsFinite(value) || value < -16 || value > 32))
                    {
                        issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                            "Visual assembly contains model coordinates outside the safe Minecraft range -16..32."));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Visual assembly cannot be compiled: {ex.Message}"));
            }
        }
    }

    private static bool TryResolveLocalBlockTexture(
        string root,
        string resource,
        out string fullPath,
        out string issue)
    {
        fullPath = string.Empty;
        issue = string.Empty;
        if (string.IsNullOrWhiteSpace(resource) || resource.Contains("..", StringComparison.Ordinal) ||
            resource.Any(char.IsControl))
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
        fullPath = ResolveRepoPath(root,
            $"mod/src/main/resources/assets/forever_skyblock/textures/{path}.png");
        return true;
    }

    private static string SerializeBlockModel(MinecraftResolvedBlockModel model)
    {
        var root = new Dictionary<string, object?>
        {
            ["parent"] = "minecraft:block/block",
            ["ambientocclusion"] = model.AmbientOcclusion,
            ["textures"] = model.Textures,
            ["elements"] = model.Elements.Select(element =>
            {
                var value = new Dictionary<string, object?>
                {
                    ["from"] = element.From,
                    ["to"] = element.To,
                    ["faces"] = element.Faces.ToDictionary(
                        pair => pair.Key,
                        pair => (object?)BuildFace(pair.Value),
                        StringComparer.OrdinalIgnoreCase)
                };
                if (element.Rotation is { } rotation)
                {
                    value["rotation"] = new Dictionary<string, object?>
                    {
                        ["origin"] = rotation.Origin,
                        ["axis"] = rotation.Axis,
                        ["angle"] = rotation.Angle,
                        ["rescale"] = rotation.Rescale
                    };
                }
                return value;
            }).ToList()
        };
        return JsonSerializer.Serialize(root, JsonIndented) + Environment.NewLine;
    }

    private static Dictionary<string, object?> BuildFace(MinecraftModelFace face)
    {
        var result = new Dictionary<string, object?> { ["texture"] = face.Texture };
        if (face.Uv is { Length: 4 }) result["uv"] = face.Uv;
        if (face.Rotation != 0) result["rotation"] = face.Rotation;
        if (!string.IsNullOrWhiteSpace(face.CullFace)) result["cullface"] = face.CullFace;
        if (face.TintIndex is { } tint) result["tintindex"] = tint;
        return result;
    }

    private static string SerializeItemModel(string assetId)
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["parent"] = $"forever_skyblock:block/{assetId}"
        }, JsonIndented) + Environment.NewLine;

    private static string SerializeBlockstate(string assetId)
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["variants"] = new Dictionary<string, object?>
            {
                [""] = new Dictionary<string, object?> { ["model"] = $"forever_skyblock:block/{assetId}" }
            }
        }, JsonIndented) + Environment.NewLine;

    private static string SerializeLootTable(string assetId)
        => JsonSerializer.Serialize(new Dictionary<string, object?>
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
                            ["name"] = $"forever_skyblock:{assetId}"
                        }
                    },
                    ["conditions"] = new object[]
                    {
                        new Dictionary<string, object?> { ["condition"] = "minecraft:survives_explosion" }
                    }
                }
            }
        }, JsonIndented) + Environment.NewLine;

    private static string MergeLanguage(string root, string relativePath, string assetId, string displayName)
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
        values[$"block.forever_skyblock.{assetId}"] = JsonSerializer.SerializeToElement(displayName);
        return JsonSerializer.Serialize(values, JsonIndented) + Environment.NewLine;
    }

    private static string SerializeManifest(
        ForeverStudioMachineDocument document,
        IReadOnlyList<string> generatedPaths)
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["schemaVersion"] = ManifestSchemaVersion,
            ["assetId"] = document.AssetId,
            ["displayName"] = document.DisplayName,
            ["productionKind"] = ProductionKindStaticBlock,
            ["machineProject"] = ForeverStudioMachineProjectService.RelativePathForAsset(document.AssetId),
            ["generatedFiles"] = generatedPaths
        }, JsonIndented) + Environment.NewLine;

    private static SortedSet<string> ScanGeneratedAssetIds(string root)
    {
        var result = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var directory = ResolveRepoPath(root, GeneratedDirectory);
        if (!Directory.Exists(directory))
            return result;
        foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly).Take(512))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                var json = document.RootElement;
                if (json.TryGetProperty("schemaVersion", out var schema) && schema.GetInt32() == ManifestSchemaVersion &&
                    json.TryGetProperty("productionKind", out var kind) &&
                    kind.GetString() == ProductionKindStaticBlock &&
                    json.TryGetProperty("assetId", out var asset) && asset.GetString() is { } id && IsSafeAssetId(id))
                    result.Add(id);
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                // Invalid stale manifests are ignored here; they are not trusted as registry input.
            }
        }
        return result;
    }

    private static string BuildGeneratedRegistry(IEnumerable<string> assetIds)
    {
        var ids = assetIds.Where(IsSafeAssetId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var builder = new StringBuilder();
        builder.AppendLine("package de.tekatz.foreverskyblock.generated;");
        builder.AppendLine();
        builder.AppendLine("import de.tekatz.foreverskyblock.ForeverSkyblock;");
        builder.AppendLine("import net.minecraft.world.item.BlockItem;");
        builder.AppendLine("import net.minecraft.world.item.CreativeModeTabs;");
        builder.AppendLine("import net.minecraft.world.level.block.Block;");
        builder.AppendLine("import net.minecraft.world.level.block.SoundType;");
        builder.AppendLine("import net.minecraft.world.level.block.state.BlockBehaviour;");
        builder.AppendLine("import net.minecraft.world.level.material.MapColor;");
        builder.AppendLine("import net.neoforged.bus.api.IEventBus;");
        builder.AppendLine("import net.neoforged.neoforge.event.BuildCreativeModeTabContentsEvent;");
        builder.AppendLine("import net.neoforged.neoforge.registries.DeferredBlock;");
        builder.AppendLine("import net.neoforged.neoforge.registries.DeferredItem;");
        builder.AppendLine("import net.neoforged.neoforge.registries.DeferredRegister;");
        builder.AppendLine();
        builder.AppendLine("/** Generated by Forever Studio. Do not hand-edit; rebuild through Machine Builder Review & Build. */");
        builder.AppendLine("public final class ForeverStudioGeneratedMachines {");
        builder.AppendLine("    private static final DeferredRegister.Blocks BLOCKS = DeferredRegister.createBlocks(ForeverSkyblock.MOD_ID);");
        builder.AppendLine("    private static final DeferredRegister.Items ITEMS = DeferredRegister.createItems(ForeverSkyblock.MOD_ID);");
        builder.AppendLine();
        foreach (var id in ids)
        {
            var field = JavaFieldName(id);
            builder.AppendLine($"    public static final DeferredBlock<Block> {field} = BLOCKS.registerSimpleBlock(\"{id}\",");
            builder.AppendLine("            BlockBehaviour.Properties.of().mapColor(MapColor.METAL).strength(2.0F, 6.0F).sound(SoundType.METAL).noOcclusion());");
            builder.AppendLine($"    public static final DeferredItem<BlockItem> {field}_ITEM = ITEMS.registerSimpleBlockItem({field});");
        }
        builder.AppendLine();
        builder.AppendLine("    private ForeverStudioGeneratedMachines() {}");
        builder.AppendLine();
        builder.AppendLine("    public static void register(IEventBus modBus) {");
        builder.AppendLine("        BLOCKS.register(modBus);");
        builder.AppendLine("        ITEMS.register(modBus);");
        builder.AppendLine("        modBus.addListener(ForeverStudioGeneratedMachines::addCreativeContents);");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    private static void addCreativeContents(BuildCreativeModeTabContentsEvent event) {");
        builder.AppendLine("        if (event.getTabKey() != CreativeModeTabs.FUNCTIONAL_BLOCKS) return;");
        foreach (var id in ids)
            builder.AppendLine($"        event.accept({JavaFieldName(id)}_ITEM.get());");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        return builder.ToString();
    }

    private static ForeverStudioMachineProductionChange CreateChange(
        string root,
        ForeverStudioMachineProductionChangeKind kind,
        string relativePath,
        string description,
        string newText)
    {
        var fullPath = ResolveRepoPath(root, relativePath);
        var existed = File.Exists(fullPath);
        var originalHash = existed ? HashFile(fullPath) : MissingHash;
        return new ForeverStudioMachineProductionChange
        {
            Kind = kind,
            RelativePath = relativePath.Replace('\\', '/'),
            Description = description,
            NewText = NormalizeNewlines(newText),
            PreviewLines = NormalizeNewlines(newText).Split('\n').Take(120).ToList(),
            FullPath = fullPath,
            OriginalHash = originalHash,
            ExistedBefore = existed
        };
    }

    private static string ResolveRepoPath(string root, string relativePath)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = normalizedRoot + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production output escaped the selected repository.");
        return full;
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string NormalizeNewlines(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static bool LooksManuallyRegistered(string mainSource, string assetId)
        => mainSource.Contains($"register(\"{assetId}\"", StringComparison.Ordinal) ||
           mainSource.Contains($"registerSimpleBlock(\"{assetId}\"", StringComparison.Ordinal);

    private static bool IsSafeAssetId(string value)
        => value.Length is > 0 and <= 128 && value.All(character =>
            char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.');

    private static string JavaFieldName(string assetId)
    {
        var safe = new string(assetId.Select(character => char.IsLetterOrDigit(character)
            ? char.ToUpperInvariant(character)
            : '_').ToArray());
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(assetId))).ToLowerInvariant()[..6].ToUpperInvariant();
        return $"FS_{safe}_{hash}";
    }
}
