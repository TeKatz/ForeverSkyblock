using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Production compiler used by Machine Builder Review & Build.
///
/// Static projects remain simple registered blocks. Runtime projects compile the supported semantic
/// processor graph into a generic NeoForge runtime contract, one generated recipe book and the same
/// deterministic block assets. Unsupported graph contracts fail closed instead of being approximated.
/// </summary>
public static class ForeverStudioMachineCompilerService
{
    public const int ManifestSchemaVersion = 2;
    public const string ProductionKindStaticBlock = "static-block";
    public const string ProductionKindRuntimeMachine = "runtime-machine";

    private const string MissingHash = "<missing>";
    private const string DeleteDescriptionPrefix = "[DELETE] ";
    private const string GeneratedDirectory =
        "mod/src/main/resources/data/forever_skyblock/forever_studio/generated";
    private const string RuntimeDirectory =
        "mod/src/main/resources/data/forever_skyblock/forever_studio/runtime";
    private const string RuntimeRecipeDirectory =
        "mod/src/main/resources/data/forever_skyblock/recipe/forever_studio";
    private const string GeneratedRegistryPath =
        "mod/src/main/java/de/tekatz/foreverskyblock/generated/ForeverStudioGeneratedMachines.java";
    private const string MainModPath =
        "mod/src/main/java/de/tekatz/foreverskyblock/ForeverSkyblock.java";
    private const string BridgeAnchor = "        RECIPE_SERIALIZERS.register(modBus);";
    private const string BridgeLine =
        "        de.tekatz.foreverskyblock.generated.ForeverStudioGeneratedMachines.register(modBus); // Forever Studio generated machines";

    private const double GuiWidth = 176;
    private const double GuiHeight = 166;

    private static readonly JsonSerializerOptions JsonIndented = new() { WriteIndented = true };

    private sealed record RuntimeRecipeData(
        string Id,
        string InputItem,
        int InputCount,
        string OutputItem,
        int OutputCount,
        int ProcessingTime,
        int EnergyPerTick);

    private sealed record RuntimeWidgetData(
        string Id,
        string Type,
        string SourceNode,
        string SourcePort,
        string Label,
        int X,
        int Y,
        int Width,
        int Height,
        int Slot);

    private sealed record RuntimeContract(
        ForeverStudioMachineBehaviorNode Processor,
        ForeverStudioMachineBehaviorNode Input,
        ForeverStudioMachineBehaviorNode Output,
        ForeverStudioMachineBehaviorNode? Energy,
        bool Powered,
        int EnergyCapacity,
        int MaxReceive,
        int InputX,
        int InputY,
        int OutputX,
        int OutputY,
        IReadOnlyList<RuntimeRecipeData> Recipes,
        IReadOnlyList<RuntimeWidgetData> Widgets);

    private sealed record GeneratedManifestEntry(string AssetId, string ProductionKind);

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

        ValidateVisualContract(root, document, catalog, issues);
        var runtimeRequested = HasRuntimeAuthoring(document);
        RuntimeContract? runtime = null;
        if (runtimeRequested)
            runtime = CompileRuntimeContract(document, issues);

        var productionKind = runtimeRequested ? ProductionKindRuntimeMachine : ProductionKindStaticBlock;
        if (HasErrors(issues))
            return Plan(document, productionKind, [], issues);

        var changes = new List<ForeverStudioMachineProductionChange>();
        var model = ForeverStudioVisualAssemblyService.BuildPreviewModel(document, catalog, out _);
        var blockModelPath = $"mod/src/main/resources/assets/forever_skyblock/models/block/{document.AssetId}.json";
        var itemModelPath = $"mod/src/main/resources/assets/forever_skyblock/models/item/{document.AssetId}.json";
        var blockstatePath = $"mod/src/main/resources/assets/forever_skyblock/blockstates/{document.AssetId}.json";
        var lootPath = $"mod/src/main/resources/data/forever_skyblock/loot_table/blocks/{document.AssetId}.json";
        var langPath = "mod/src/main/resources/assets/forever_skyblock/lang/en_us.json";
        var manifestPath = $"{GeneratedDirectory}/{document.AssetId}.json";
        var runtimePath = $"{RuntimeDirectory}/{document.AssetId}.json";
        var recipePath = $"{RuntimeRecipeDirectory}/{document.AssetId}.json";

        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.BlockModel, blockModelPath,
            "Compile the visual assembly into the Minecraft block model.", SerializeBlockModel(model));
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
            return Plan(document, productionKind, [], issues);
        }

        if (runtime is not null)
        {
            AddChange(changes, root, ForeverStudioMachineProductionChangeKind.BuildManifest, runtimePath,
                "Compile Function, inventory, energy and GUI authoring into the generic NeoForge runtime contract.",
                SerializeRuntimeDefinition(document, runtime));
            AddChange(changes, root, ForeverStudioMachineProductionChangeKind.BuildManifest, recipePath,
                "Compile all Phase-04 recipes into the machine's runtime recipe book.",
                SerializeRuntimeRecipeBook(document, runtime));
        }

        var existingManifest = File.Exists(ResolveRepoPath(root, manifestPath));
        var mainPath = ResolveRepoPath(root, MainModPath);
        if (!File.Exists(mainPath))
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Main mod source '{MainModPath}' is missing."));
            return Plan(document, productionKind, [], issues);
        }

        var mainSource = File.ReadAllText(mainPath);
        if (!existingManifest && LooksManuallyRegistered(mainSource, document.AssetId))
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Block id '{document.AssetId}' already appears to be registered by hand in ForeverSkyblock.java."));
            return Plan(document, productionKind, [], issues);
        }

        if (!mainSource.Contains(BridgeLine, StringComparison.Ordinal))
        {
            if (!mainSource.Contains(BridgeAnchor, StringComparison.Ordinal))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    "ForeverSkyblock.java no longer contains the safe generated-registry bridge anchor."));
                return Plan(document, productionKind, [], issues);
            }
            AddChange(changes, root, ForeverStudioMachineProductionChangeKind.JavaBridge, MainModPath,
                "Install the one-time Forever Studio generated-machine registry bridge.",
                mainSource.Replace(BridgeAnchor, BridgeAnchor + Environment.NewLine + BridgeLine, StringComparison.Ordinal));
        }

        var generated = ScanGeneratedManifests(root);
        generated[document.AssetId] = new GeneratedManifestEntry(document.AssetId, productionKind);
        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.GeneratedRegistry, GeneratedRegistryPath,
            "Regenerate the Studio-owned block/runtime registry from built machine manifests.",
            BuildGeneratedRegistry(generated.Values));

        var ownedPaths = new List<string>
        {
            blockModelPath, itemModelPath, blockstatePath, lootPath, langPath,
            GeneratedRegistryPath, manifestPath
        };
        if (runtime is not null)
        {
            ownedPaths.Add(runtimePath);
            ownedPaths.Add(recipePath);
        }

        AddStaleOwnedDeletes(changes, root, manifestPath, ownedPaths, document.AssetId);
        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.BuildManifest, manifestPath,
            "Record the production backend and generated files for deterministic rebuilds.",
            SerializeManifest(document, productionKind, ownedPaths));

        issues.Add(new StudioValidationIssue(StudioValidationSeverity.Info,
            changes.Count == 0
                ? $"{productionKind} production output is already up to date."
                : $"{productionKind} compiler is ready: {changes.Count} file change(s) are planned."));
        if (runtime is not null)
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Info,
                $"Runtime contract: {runtime.Recipes.Count} recipe(s), {runtime.Widgets.Count} GUI widget(s), " +
                (runtime.Powered ? "powered processor." : "standard processor.")));
        }
        return Plan(document, productionKind, changes, issues);
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
        var staged = new List<(ForeverStudioMachineProductionChange Change, string? Temp, string? Backup)>();
        var committed = new List<(ForeverStudioMachineProductionChange Change, string? Backup)>();
        try
        {
            foreach (var change in plan.Changes)
            {
                var directory = Path.GetDirectoryName(change.FullPath)
                    ?? throw new IOException($"Cannot resolve output directory for '{change.RelativePath}'.");
                Directory.CreateDirectory(directory);
                string? backup = null;
                if (change.ExistedBefore)
                {
                    backup = Path.Combine(directory, $".{Path.GetFileName(change.FullPath)}.foreverstudio-{token}.bak");
                    File.Copy(change.FullPath, backup, overwrite: true);
                }

                string? temp = null;
                if (!IsDelete(change))
                {
                    temp = Path.Combine(directory, $".{Path.GetFileName(change.FullPath)}.foreverstudio-{token}.tmp");
                    File.WriteAllText(temp, NormalizeNewlines(change.NewText), new UTF8Encoding(false));
                }
                staged.Add((change, temp, backup));
            }

            foreach (var item in staged)
            {
                if (IsDelete(item.Change))
                    File.Delete(item.Change.FullPath);
                else
                    File.Move(item.Temp!, item.Change.FullPath, overwrite: true);
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
                try { if (item.Temp is not null && File.Exists(item.Temp)) File.Delete(item.Temp); } catch { }
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
        string productionKind,
        IReadOnlyList<ForeverStudioMachineProductionChange> changes,
        IReadOnlyList<StudioValidationIssue> issues)
        => new(document.AssetId, document.DisplayName, productionKind, changes, issues);

    private static bool HasErrors(IEnumerable<StudioValidationIssue> issues)
        => issues.Any(issue => issue.Severity == StudioValidationSeverity.Error);

    private static bool HasRuntimeAuthoring(ForeverStudioMachineDocument document)
    {
        var executable = document.Behavior.Nodes.Any(node =>
            !(node.Id.Equals("machine_entry", StringComparison.OrdinalIgnoreCase) &&
              node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase)));
        return executable || document.Behavior.Connections.Count > 0 || document.Recipes.Count > 0 ||
               document.Gui.Bindings.Count > 0 || !string.IsNullOrWhiteSpace(document.Gui.ScreenId) ||
               document.Animations.Count > 0;
    }

    private static void ValidateVisualContract(
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
                "The current compiler supports exactly one semantic machine state. State-machine runtime compilation is not implemented yet."));

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

    private static RuntimeContract? CompileRuntimeContract(
        ForeverStudioMachineDocument document,
        List<StudioValidationIssue> issues)
    {
        if (document.Animations.Count > 0)
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "Runtime animation bindings are not part of the first production compiler. Remove them or build after the animation backend is implemented."));
        }

        var core = document.Behavior.Nodes.Where(node =>
            node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase)).ToList();
        var processors = document.Behavior.Nodes.Where(node =>
            node.LibraryItemId is "function.standard-processor" or "function.powered-processor").ToList();
        var inputs = document.Behavior.Nodes.Where(node =>
            node.LibraryItemId.Equals("function.item-input", StringComparison.OrdinalIgnoreCase)).ToList();
        var outputs = document.Behavior.Nodes.Where(node =>
            node.LibraryItemId.Equals("function.item-storage", StringComparison.OrdinalIgnoreCase)).ToList();
        var energyNodes = document.Behavior.Nodes.Where(node =>
            node.LibraryItemId.Equals("function.energy-storage", StringComparison.OrdinalIgnoreCase)).ToList();
        var supportedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "core.machine-entry", "function.item-input", "function.item-storage",
            "function.standard-processor", "function.powered-processor", "function.energy-storage"
        };
        var unsupported = document.Behavior.Nodes
            .Where(node => !supportedIds.Contains(node.LibraryItemId))
            .Select(node => $"{node.Id} ({node.LibraryItemId})")
            .ToList();
        if (unsupported.Count > 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "Runtime compiler does not yet support these Function nodes: " + string.Join(", ", unsupported) + "."));

        if (core.Count != 1)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Runtime processor requires exactly one Machine Entry node."));
        if (processors.Count != 1)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "Runtime processor requires exactly one Standard Processor or Powered Processor node."));
        if (inputs.Count != 1)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Runtime processor requires exactly one Item Input node."));
        if (outputs.Count != 1)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Runtime processor requires exactly one Item Storage output node."));
        if (HasErrors(issues)) return null;

        var processor = processors[0];
        var input = inputs[0];
        var output = outputs[0];
        var powered = processor.LibraryItemId.Equals("function.powered-processor", StringComparison.OrdinalIgnoreCase);
        if (powered && energyNodes.Count != 1)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "Powered Processor requires exactly one Energy Storage node."));
        if (!powered && energyNodes.Count > 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "Energy Storage is currently compiled only together with Powered Processor."));

        var allowedConnection = document.Behavior.Connections.Where(connection =>
            core.Any(entry => entry.Id.Equals(connection.FromNodeId, StringComparison.OrdinalIgnoreCase)) &&
            connection.FromPortId.Equals("flow", StringComparison.OrdinalIgnoreCase) &&
            connection.ToNodeId.Equals(processor.Id, StringComparison.OrdinalIgnoreCase) &&
            connection.ToPortId.Equals("flow-in", StringComparison.OrdinalIgnoreCase)).ToList();
        if (allowedConnection.Count != 1 || document.Behavior.Connections.Count != 1)
        {
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "The first runtime compiler expects exactly Machine Entry.flow → Processor.flow-in. " +
                "Additional graph topology remains fail-closed until the generic graph executor is implemented."));
        }

        if (document.Recipes.Count == 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "A compiled processing machine needs at least one Phase-04 recipe."));

        var recipes = new List<RuntimeRecipeData>();
        var seenInputs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var recipe in document.Recipes)
        {
            var expectedType = powered ? "forever_skyblock:powered_processing" : "forever_skyblock:machine_processing";
            if (!recipe.RecipeTypeId.Equals(expectedType, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' uses '{recipe.RecipeTypeId}', but this processor requires '{expectedType}'."));
                continue;
            }
            var processNodeId = ReadString(recipe.Properties, "processorNodeId", string.Empty);
            if (!processNodeId.Equals(processor.Id, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' is not bound to processor '{processor.Id}'."));
                continue;
            }
            if (!TryReadRecipeStack(recipe.Properties, "input", out var inputItem, out var inputCount) ||
                !TryReadRecipeStack(recipe.Properties, "output", out var outputItem, out var outputCount) ||
                !TryReadInt(recipe.Properties, "processingTimeTicks", 1, 1_000_000, out var duration))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' has invalid input, output or processing-time data."));
                continue;
            }
            var fePerTick = 0;
            if (recipe.Properties.TryGetValue("energy", out var energy) && energy.ValueKind == JsonValueKind.Object &&
                energy.TryGetProperty("fePerTick", out var fe) && fe.TryGetInt32(out var parsedFe))
                fePerTick = parsedFe;
            if (fePerTick is < 0 or > 1_000_000 || (long)duration * fePerTick > int.MaxValue)
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' has an unsupported FE/t or total energy value."));
                continue;
            }
            if (!powered && fePerTick != 0)
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' consumes FE but the selected processor is not powered."));
                continue;
            }
            if (!seenInputs.Add(inputItem))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Multiple generated recipes use exact input '{inputItem}'. The current runtime requires one unambiguous recipe per input item."));
                continue;
            }
            recipes.Add(new RuntimeRecipeData(recipe.Id, inputItem, inputCount, outputItem, outputCount, duration, fePerTick));
        }

        var widgets = new List<RuntimeWidgetData>();
        var slottedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inputX = 35;
        var inputY = 52;
        var outputX = 123;
        var outputY = 52;
        foreach (var binding in document.Gui.Bindings.OrderBy(binding => ReadNumber(binding.Properties, "order", 0)))
        {
            var type = ReadString(binding.Properties, "widget", string.Empty);
            var sourcePort = ReadString(binding.Properties, "sourcePortId", string.Empty);
            var label = ReadString(binding.Properties, "label", type);
            if (!TryReadGuiGeometry(binding.Properties, out var x, out var y, out var width, out var height))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"GUI binding '{binding.Id}' has invalid or out-of-bounds geometry."));
                continue;
            }

            var slot = -1;
            var valid = type switch
            {
                "item-slot" => TryMapItemSlot(binding.SourceId, sourcePort, input, output, out slot),
                "output-slot" => binding.SourceId.Equals(output.Id, StringComparison.OrdinalIgnoreCase) &&
                                 sourcePort.Equals("stored", StringComparison.OrdinalIgnoreCase) && AssignSlot(1, out slot),
                "progress-bar" => binding.SourceId.Equals(processor.Id, StringComparison.OrdinalIgnoreCase) &&
                                  sourcePort.Equals("progress", StringComparison.OrdinalIgnoreCase),
                "status" => binding.SourceId.Equals(processor.Id, StringComparison.OrdinalIgnoreCase) &&
                            sourcePort.Equals("running", StringComparison.OrdinalIgnoreCase),
                "energy-bar" => powered && energyNodes.Count == 1 &&
                                binding.SourceId.Equals(energyNodes[0].Id, StringComparison.OrdinalIgnoreCase) &&
                                sourcePort.Equals("ratio", StringComparison.OrdinalIgnoreCase),
                "value" => IsSupportedValueSource(binding.SourceId, sourcePort, processor, energyNodes.FirstOrDefault(), powered),
                _ => false
            };
            if (!valid)
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"GUI binding '{binding.Id}' ({type}) cannot be compiled from source '{binding.SourceId}.{sourcePort}'."));
                continue;
            }
            if (slot >= 0 && !slottedSources.Add(binding.SourceId))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"GUI binding '{binding.Id}' duplicates the interactive slot for Function node '{binding.SourceId}'."));
                continue;
            }
            if (slot == 0) { inputX = x; inputY = y; }
            if (slot == 1) { outputX = x; outputY = y; }
            widgets.Add(new RuntimeWidgetData(binding.Id, type, binding.SourceId, sourcePort, label,
                x, y, width, height, slot));
        }

        if (HasErrors(issues)) return null;
        var energyNode = powered ? energyNodes.Single() : null;
        var capacity = powered ? ReadPositiveNodeInt(energyNode!, "capacity", 100_000, issues) : 0;
        var maxReceive = powered ? ReadPositiveNodeInt(energyNode!, "maxReceive", 4_000, issues) : 0;
        if (HasErrors(issues)) return null;

        return new RuntimeContract(processor, input, output, energyNode, powered, capacity, maxReceive,
            inputX, inputY, outputX, outputY, recipes, widgets);
    }

    private static bool TryMapItemSlot(
        string sourceId,
        string port,
        ForeverStudioMachineBehaviorNode input,
        ForeverStudioMachineBehaviorNode output,
        out int slot)
    {
        slot = -1;
        if (sourceId.Equals(input.Id, StringComparison.OrdinalIgnoreCase) && port.Equals("item", StringComparison.OrdinalIgnoreCase))
            return AssignSlot(0, out slot);
        if (sourceId.Equals(output.Id, StringComparison.OrdinalIgnoreCase) && port.Equals("stored", StringComparison.OrdinalIgnoreCase))
            return AssignSlot(1, out slot);
        return false;
    }

    private static bool AssignSlot(int value, out int slot)
    {
        slot = value;
        return true;
    }

    private static bool IsSupportedValueSource(
        string sourceId,
        string port,
        ForeverStudioMachineBehaviorNode processor,
        ForeverStudioMachineBehaviorNode? energy,
        bool powered)
    {
        if (sourceId.Equals(processor.Id, StringComparison.OrdinalIgnoreCase) &&
            port is "progress" or "running") return true;
        return powered && energy is not null && sourceId.Equals(energy.Id, StringComparison.OrdinalIgnoreCase) &&
               port is "energy" or "ratio";
    }

    private static int ReadPositiveNodeInt(
        ForeverStudioMachineBehaviorNode node,
        string key,
        int fallback,
        List<StudioValidationIssue> issues)
    {
        if (!node.Properties.TryGetValue(key, out var value)) return fallback;
        if (value.TryGetInt32(out var parsed) && parsed > 0) return parsed;
        issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
            $"Function node '{node.Id}' property '{key}' must be a positive integer."));
        return fallback;
    }

    private static bool TryReadGuiGeometry(
        Dictionary<string, JsonElement> properties,
        out int x,
        out int y,
        out int width,
        out int height)
    {
        x = (int)Math.Round(ReadNumber(properties, "x", 0));
        y = (int)Math.Round(ReadNumber(properties, "y", 0));
        width = (int)Math.Round(ReadNumber(properties, "width", 0));
        height = (int)Math.Round(ReadNumber(properties, "height", 0));
        return x >= 0 && y >= 0 && width >= 1 && height >= 1 &&
               x + width <= GuiWidth && y + height <= GuiHeight;
    }

    private static bool TryReadRecipeStack(
        Dictionary<string, JsonElement> properties,
        string key,
        out string item,
        out int count)
    {
        item = string.Empty;
        count = 0;
        if (!properties.TryGetValue(key, out var value) || value.ValueKind != JsonValueKind.Object ||
            !value.TryGetProperty("item", out var itemJson) || itemJson.ValueKind != JsonValueKind.String ||
            !value.TryGetProperty("count", out var countJson) || !countJson.TryGetInt32(out count))
            return false;
        item = itemJson.GetString()?.Trim().ToLowerInvariant() ?? string.Empty;
        return IsResourceLocation(item) && count is >= 1 and <= 64;
    }

    private static bool TryReadInt(
        Dictionary<string, JsonElement> properties,
        string key,
        int min,
        int max,
        out int result)
    {
        result = 0;
        return properties.TryGetValue(key, out var value) && value.TryGetInt32(out result) && result >= min && result <= max;
    }

    private static string SerializeRuntimeDefinition(ForeverStudioMachineDocument document, RuntimeContract runtime)
    {
        var widgets = runtime.Widgets.Select(widget => new Dictionary<string, object?>
        {
            ["id"] = widget.Id,
            ["type"] = widget.Type,
            ["sourceNode"] = widget.SourceNode,
            ["sourcePort"] = widget.SourcePort,
            ["label"] = widget.Label,
            ["x"] = widget.X,
            ["y"] = widget.Y,
            ["width"] = widget.Width,
            ["height"] = widget.Height,
            ["slot"] = widget.Slot
        }).ToArray();
        return Json(new Dictionary<string, object?>
        {
            ["schemaVersion"] = 1,
            ["machine"] = $"forever_skyblock:{document.AssetId}",
            ["displayName"] = document.DisplayName,
            ["processor"] = new Dictionary<string, object?>
            {
                ["nodeId"] = runtime.Processor.Id,
                ["kind"] = runtime.Powered ? "powered" : "standard",
                ["powered"] = runtime.Powered
            },
            ["inventory"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["nodeId"] = runtime.Input.Id,
                    ["role"] = "input",
                    ["slot"] = 0,
                    ["x"] = runtime.InputX,
                    ["y"] = runtime.InputY
                },
                new Dictionary<string, object?>
                {
                    ["nodeId"] = runtime.Output.Id,
                    ["role"] = "output",
                    ["slot"] = 1,
                    ["x"] = runtime.OutputX,
                    ["y"] = runtime.OutputY
                }
            },
            ["energy"] = new Dictionary<string, object?>
            {
                ["enabled"] = runtime.Powered,
                ["capacity"] = runtime.EnergyCapacity,
                ["maxReceive"] = runtime.MaxReceive
            },
            ["gui"] = new Dictionary<string, object?>
            {
                ["width"] = (int)GuiWidth,
                ["height"] = (int)GuiHeight,
                ["widgets"] = widgets
            }
        });
    }

    private static string SerializeRuntimeRecipeBook(ForeverStudioMachineDocument document, RuntimeContract runtime)
        => Json(new Dictionary<string, object?>
        {
            ["type"] = "forever_skyblock:forever_studio_machine",
            ["machine"] = $"forever_skyblock:{document.AssetId}",
            ["recipes"] = runtime.Recipes.Select(recipe => new Dictionary<string, object?>
            {
                ["input"] = new Dictionary<string, object?> { ["item"] = recipe.InputItem },
                ["input_count"] = recipe.InputCount,
                ["result"] = new Dictionary<string, object?> { ["id"] = recipe.OutputItem, ["count"] = recipe.OutputCount },
                ["processing_time"] = recipe.ProcessingTime,
                ["energy_per_tick"] = recipe.EnergyPerTick
            }).ToArray()
        });

    private static string SerializeManifest(
        ForeverStudioMachineDocument document,
        string productionKind,
        IReadOnlyList<string> ownedPaths)
        => Json(new Dictionary<string, object?>
        {
            ["schemaVersion"] = ManifestSchemaVersion,
            ["assetId"] = document.AssetId,
            ["displayName"] = document.DisplayName,
            ["productionKind"] = productionKind,
            ["machineProject"] = ForeverStudioMachineProjectService.RelativePathForAsset(document.AssetId),
            ["generatedFiles"] = ownedPaths.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray()
        });

    private static Dictionary<string, GeneratedManifestEntry> ScanGeneratedManifests(string root)
    {
        var result = new Dictionary<string, GeneratedManifestEntry>(StringComparer.OrdinalIgnoreCase);
        var directory = ResolveRepoPath(root, GeneratedDirectory);
        if (!Directory.Exists(directory)) return result;
        foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly).Take(512))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                var json = document.RootElement;
                if (!json.TryGetProperty("schemaVersion", out var schema) || !schema.TryGetInt32(out var schemaVersion) ||
                    schemaVersion is < 1 or > ManifestSchemaVersion ||
                    !json.TryGetProperty("productionKind", out var kindJson) || kindJson.GetString() is not { } kind ||
                    !json.TryGetProperty("assetId", out var asset) || asset.GetString() is not { } id || !IsSafeAssetId(id))
                    continue;
                if (kind is not (ProductionKindStaticBlock or ProductionKindRuntimeMachine)) continue;
                result[id] = new GeneratedManifestEntry(id, kind);
            }
            catch (Exception ex) when (ex is JsonException or IOException) { }
        }
        return result;
    }

    private static string BuildGeneratedRegistry(IEnumerable<GeneratedManifestEntry> entries)
    {
        var machines = entries
            .Where(entry => IsSafeAssetId(entry.AssetId))
            .GroupBy(entry => entry.AssetId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .OrderBy(entry => entry.AssetId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var runtime = machines.Where(entry => entry.ProductionKind == ProductionKindRuntimeMachine).ToList();

        var b = new StringBuilder();
        b.AppendLine("package de.tekatz.foreverskyblock.generated;");
        b.AppendLine();
        b.AppendLine("import de.tekatz.foreverskyblock.ForeverSkyblock;");
        b.AppendLine("import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineBlock;");
        b.AppendLine("import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineBlockEntity;");
        b.AppendLine("import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineMenu;");
        b.AppendLine("import de.tekatz.foreverskyblock.studio.runtime.ForeverStudioMachineRecipe;");
        b.AppendLine("import net.minecraft.core.registries.Registries;");
        b.AppendLine("import net.minecraft.resources.ResourceLocation;");
        b.AppendLine("import net.minecraft.world.inventory.MenuType;");
        b.AppendLine("import net.minecraft.world.item.BlockItem;");
        b.AppendLine("import net.minecraft.world.item.CreativeModeTabs;");
        b.AppendLine("import net.minecraft.world.item.crafting.RecipeSerializer;");
        b.AppendLine("import net.minecraft.world.item.crafting.RecipeType;");
        b.AppendLine("import net.minecraft.world.level.block.Block;");
        b.AppendLine("import net.minecraft.world.level.block.SoundType;");
        b.AppendLine("import net.minecraft.world.level.block.entity.BlockEntityType;");
        b.AppendLine("import net.minecraft.world.level.block.state.BlockBehaviour;");
        b.AppendLine("import net.minecraft.world.level.material.MapColor;");
        b.AppendLine("import net.neoforged.bus.api.IEventBus;");
        b.AppendLine("import net.neoforged.neoforge.capabilities.Capabilities;");
        b.AppendLine("import net.neoforged.neoforge.capabilities.RegisterCapabilitiesEvent;");
        b.AppendLine("import net.neoforged.neoforge.common.extensions.IMenuTypeExtension;");
        b.AppendLine("import net.neoforged.neoforge.event.BuildCreativeModeTabContentsEvent;");
        b.AppendLine("import net.neoforged.neoforge.registries.DeferredBlock;");
        b.AppendLine("import net.neoforged.neoforge.registries.DeferredHolder;");
        b.AppendLine("import net.neoforged.neoforge.registries.DeferredItem;");
        b.AppendLine("import net.neoforged.neoforge.registries.DeferredRegister;");
        b.AppendLine();
        b.AppendLine("/** Generated by Forever Studio. Do not hand-edit; rebuild via Machine Builder. */");
        b.AppendLine("public final class ForeverStudioGeneratedMachines {");
        b.AppendLine("    private static final DeferredRegister.Blocks BLOCKS = DeferredRegister.createBlocks(ForeverSkyblock.MOD_ID);");
        b.AppendLine("    private static final DeferredRegister.Items ITEMS = DeferredRegister.createItems(ForeverSkyblock.MOD_ID);");
        b.AppendLine("    private static final DeferredRegister<BlockEntityType<?>> BLOCK_ENTITIES = DeferredRegister.create(Registries.BLOCK_ENTITY_TYPE, ForeverSkyblock.MOD_ID);");
        b.AppendLine("    private static final DeferredRegister<MenuType<?>> MENUS = DeferredRegister.create(Registries.MENU, ForeverSkyblock.MOD_ID);");
        b.AppendLine("    private static final DeferredRegister<RecipeType<?>> RECIPE_TYPES = DeferredRegister.create(Registries.RECIPE_TYPE, ForeverSkyblock.MOD_ID);");
        b.AppendLine("    private static final DeferredRegister<RecipeSerializer<?>> RECIPE_SERIALIZERS = DeferredRegister.create(Registries.RECIPE_SERIALIZER, ForeverSkyblock.MOD_ID);");
        b.AppendLine();

        foreach (var machine in machines)
        {
            var field = JavaFieldName(machine.AssetId);
            if (machine.ProductionKind == ProductionKindRuntimeMachine)
            {
                b.AppendLine($"    public static final DeferredBlock<ForeverStudioMachineBlock> {field} = BLOCKS.register(\"{machine.AssetId}\",");
                b.AppendLine($"            () -> new ForeverStudioMachineBlock(ResourceLocation.tryParse(ForeverSkyblock.MOD_ID + \":{machine.AssetId}\"),");
                b.AppendLine("                    BlockBehaviour.Properties.of().mapColor(MapColor.METAL).strength(2.0F, 6.0F).sound(SoundType.METAL).noOcclusion()));");
            }
            else
            {
                b.AppendLine($"    public static final DeferredBlock<Block> {field} = BLOCKS.registerSimpleBlock(\"{machine.AssetId}\",");
                b.AppendLine("            BlockBehaviour.Properties.of().mapColor(MapColor.METAL).strength(2.0F, 6.0F).sound(SoundType.METAL).noOcclusion());");
            }
            b.AppendLine($"    public static final DeferredItem<BlockItem> {field}_ITEM = ITEMS.registerSimpleBlockItem({field});");
        }
        if (machines.Count > 0) b.AppendLine();

        var validBlocks = runtime.Count == 0
            ? string.Empty
            : ", " + string.Join(", ", runtime.Select(machine => JavaFieldName(machine.AssetId) + ".get()"));
        b.AppendLine("    public static final DeferredHolder<BlockEntityType<?>, BlockEntityType<ForeverStudioMachineBlockEntity>> MACHINE_BLOCK_ENTITY =");
        b.AppendLine($"            BLOCK_ENTITIES.register(\"forever_studio_machine\", () -> BlockEntityType.Builder.of(ForeverStudioMachineBlockEntity::new{validBlocks}).build(null));");
        b.AppendLine("    public static final DeferredHolder<MenuType<?>, MenuType<ForeverStudioMachineMenu>> MACHINE_MENU =");
        b.AppendLine("            MENUS.register(\"forever_studio_machine\", () -> IMenuTypeExtension.create(ForeverStudioMachineMenu::new));");
        b.AppendLine("    public static final DeferredHolder<RecipeType<?>, RecipeType<ForeverStudioMachineRecipe>> MACHINE_RECIPE_TYPE =");
        b.AppendLine("            RECIPE_TYPES.register(\"forever_studio_machine\", () -> new RecipeType<>() {");
        b.AppendLine("                @Override public String toString() { return ForeverSkyblock.MOD_ID + \":forever_studio_machine\"; }");
        b.AppendLine("            });");
        b.AppendLine("    public static final DeferredHolder<RecipeSerializer<?>, RecipeSerializer<ForeverStudioMachineRecipe>> MACHINE_RECIPE_SERIALIZER =");
        b.AppendLine("            RECIPE_SERIALIZERS.register(\"forever_studio_machine\", ForeverStudioMachineRecipe.Serializer::new);");
        b.AppendLine();
        b.AppendLine("    private ForeverStudioGeneratedMachines() { }");
        b.AppendLine();
        b.AppendLine("    public static void register(IEventBus modBus) {");
        b.AppendLine("        BLOCKS.register(modBus);");
        b.AppendLine("        ITEMS.register(modBus);");
        b.AppendLine("        BLOCK_ENTITIES.register(modBus);");
        b.AppendLine("        MENUS.register(modBus);");
        b.AppendLine("        RECIPE_TYPES.register(modBus);");
        b.AppendLine("        RECIPE_SERIALIZERS.register(modBus);");
        b.AppendLine("        modBus.addListener(ForeverStudioGeneratedMachines::registerCapabilities);");
        b.AppendLine("        modBus.addListener(ForeverStudioGeneratedMachines::addCreativeContents);");
        b.AppendLine("    }");
        b.AppendLine();
        b.AppendLine("    private static void registerCapabilities(RegisterCapabilitiesEvent event) {");
        b.AppendLine("        event.registerBlockEntity(Capabilities.ItemHandler.BLOCK, MACHINE_BLOCK_ENTITY.get(), ForeverStudioMachineBlockEntity::itemAutomation);");
        b.AppendLine("        event.registerBlockEntity(Capabilities.EnergyStorage.BLOCK, MACHINE_BLOCK_ENTITY.get(), ForeverStudioMachineBlockEntity::energyAutomation);");
        b.AppendLine("    }");
        b.AppendLine();
        b.AppendLine("    private static void addCreativeContents(BuildCreativeModeTabContentsEvent event) {");
        b.AppendLine("        if (event.getTabKey() != CreativeModeTabs.FUNCTIONAL_BLOCKS) return;");
        if (machines.Count == 0)
            b.AppendLine("        // Machine Builder-generated block items are inserted here by Forever Studio.");
        else
            foreach (var machine in machines) b.AppendLine($"        event.accept({JavaFieldName(machine.AssetId)}_ITEM.get());");
        b.AppendLine("    }");
        b.AppendLine("}");
        return b.ToString();
    }

    private static void AddStaleOwnedDeletes(
        List<ForeverStudioMachineProductionChange> changes,
        string root,
        string manifestPath,
        IReadOnlyCollection<string> currentOwnedPaths,
        string assetId)
    {
        var fullManifest = ResolveRepoPath(root, manifestPath);
        if (!File.Exists(fullManifest)) return;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(fullManifest));
            if (!document.RootElement.TryGetProperty("generatedFiles", out var files) || files.ValueKind != JsonValueKind.Array)
                return;
            var current = currentOwnedPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var runtimeDefinition = $"{RuntimeDirectory}/{assetId}.json";
            var recipePrefix = $"{RuntimeRecipeDirectory}/{assetId}";
            foreach (var item in files.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String || item.GetString() is not { } path || current.Contains(path))
                    continue;
                var safeRuntimePath = path.Equals(runtimeDefinition, StringComparison.OrdinalIgnoreCase) ||
                                      path.Equals(recipePrefix + ".json", StringComparison.OrdinalIgnoreCase) ||
                                      path.StartsWith(recipePrefix + "/", StringComparison.OrdinalIgnoreCase);
                if (safeRuntimePath)
                    AddDelete(changes, root, path, "Remove stale Studio-owned runtime output no longer present in the Machine Project.");
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            throw new InvalidDataException($"Existing build manifest '{manifestPath}' cannot be read safely.", ex);
        }
    }

    private static void AddDelete(
        List<ForeverStudioMachineProductionChange> changes,
        string root,
        string relativePath,
        string description)
    {
        var fullPath = ResolveRepoPath(root, relativePath);
        if (!File.Exists(fullPath)) return;
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

    private static bool IsDelete(ForeverStudioMachineProductionChange change)
        => change.Description.StartsWith(DeleteDescriptionPrefix, StringComparison.Ordinal);

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

    private static string ReadString(Dictionary<string, JsonElement> properties, string key, string fallback)
        => properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static double ReadNumber(Dictionary<string, JsonElement> properties, string key, double fallback)
        => properties.TryGetValue(key, out var value) && value.TryGetDouble(out var result) && double.IsFinite(result)
            ? result
            : fallback;

    private static bool IsResourceLocation(string value)
    {
        var split = value.Split(':', 2);
        if (split.Length != 2 || split[0].Length == 0 || split[1].Length == 0) return false;
        return split[0].All(character => char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.') &&
               split[1].All(character => char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.' or '/');
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
