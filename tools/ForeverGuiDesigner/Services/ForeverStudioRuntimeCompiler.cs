using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Production backend for executable Forever Studio machines. The authoring document remains the
/// source of truth; this compiler emits a deterministic data-driven NeoForge runtime contract.
/// </summary>
internal static class ForeverStudioRuntimeCompiler
{
    internal const int ManifestSchemaVersion = 3;
    internal const string ProductionKindRuntimeMachine = "runtime-machine";

    private const string MissingHash = "<missing>";
    private const string DeleteDescriptionPrefix = "[DELETE] ";
    private const string GeneratedDirectory = "mod/src/main/resources/data/forever_skyblock/forever_studio/generated";
    private const string RuntimeDirectory = "mod/src/main/resources/data/forever_skyblock/forever_studio/runtime";
    private const string RuntimeRecipeDirectory = "mod/src/main/resources/data/forever_skyblock/recipe/forever_studio";
    private const string GeneratedRegistryPath = "mod/src/main/java/de/tekatz/foreverskyblock/generated/ForeverStudioGeneratedMachines.java";
    private static readonly JsonSerializerOptions JsonIndented = new() { WriteIndented = true };

    private sealed record StackData(string Item, int Count);
    private sealed record FluidData(int Tank, string Fluid, int Amount);
    private sealed record RuntimeRecipeData(
        string Id,
        IReadOnlyList<StackData> Inputs,
        IReadOnlyList<StackData> Outputs,
        IReadOnlyList<FluidData> FluidInputs,
        IReadOnlyList<FluidData> FluidOutputs,
        int ProcessingTime,
        int EnergyPerTick,
        int EnergyOutputPerTick);
    private sealed record SlotData(string NodeId, string Role, int Slot, int X, int Y);
    private sealed record TankData(string NodeId, int Tank, int Capacity, int MaxFill, int MaxDrain);
    private sealed record WidgetData(string Id, string Type, string SourceNode, string SourcePort, string Label,
        int X, int Y, int Width, int Height, int Slot, int Tank);
    private sealed record GraphNodeData(string Id, string Kind, IReadOnlyDictionary<string, JsonElement> Properties);
    private sealed record GraphConnectionData(string Id, string FromNode, string FromPort, string ToNode, string ToPort);
    private sealed record StateData(string Id, bool Initial);
    private sealed record AnimationData(string Id, string Preset, string TargetComponent, string SourceNode,
        string SourcePort, string Axis, double Scale);
    private sealed record RuntimeContract(
        ForeverStudioMachineBehaviorNode Processor,
        string ProcessorKind,
        IReadOnlyList<SlotData> Slots,
        IReadOnlyList<TankData> Tanks,
        ForeverStudioMachineBehaviorNode? Energy,
        bool Powered,
        int EnergyCapacity,
        int MaxReceive,
        IReadOnlyList<RuntimeRecipeData> Recipes,
        IReadOnlyList<WidgetData> Widgets,
        IReadOnlyList<GraphNodeData> GraphNodes,
        IReadOnlyList<GraphConnectionData> GraphConnections,
        IReadOnlyList<StateData> States,
        IReadOnlyList<AnimationData> Animations);
    private sealed record GeneratedManifestEntry(string AssetId, string ProductionKind);

    internal static ForeverStudioMachineProductionPlan BuildPlan(
        string repoRoot,
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog)
    {
        var root = Path.GetFullPath(repoRoot.Trim());
        var issues = ForeverStudioMachineProjectService.Validate(document, catalog)
            .Where(issue => issue.Severity != StudioValidationSeverity.Info)
            .ToList();

        var runtime = CompileContract(document, catalog, issues);
        if (HasErrors(issues) || runtime is null)
            return Plan(document, [], issues);

        // Reuse the established static backend for the visual model, blockstate, loot and language
        // output. The clone intentionally strips runtime authoring so that backend can do one job.
        var visualDocument = BuildVisualOnlyClone(document);
        _ = ForeverStudioMachineProjectService.Serialize(visualDocument);
        var visualPlan = ForeverStudioMachineProductionService.BuildPlan(root, visualDocument, catalog);
        issues.AddRange(visualPlan.Issues.Where(issue => issue.Severity != StudioValidationSeverity.Info));
        if (HasErrors(issues))
            return Plan(document, [], issues);

        var manifestPath = $"{GeneratedDirectory}/{document.AssetId}.json";
        var runtimePath = $"{RuntimeDirectory}/{document.AssetId}.json";
        var recipePath = $"{RuntimeRecipeDirectory}/{document.AssetId}.json";
        var changes = visualPlan.Changes
            .Where(change => change.Kind != ForeverStudioMachineProductionChangeKind.GeneratedRegistry &&
                             !change.RelativePath.Equals(manifestPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.BuildManifest, runtimePath,
            "Compile Function Graph, inventory, resources, GUI and animation bindings into the runtime contract.",
            SerializeRuntimeDefinition(document, runtime));
        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.BuildManifest, recipePath,
            "Compile all authored processing recipes into the runtime recipe book.",
            SerializeRuntimeRecipeBook(document, runtime));

        var generated = ScanGeneratedManifests(root);
        generated[document.AssetId] = new GeneratedManifestEntry(document.AssetId, ProductionKindRuntimeMachine);
        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.GeneratedRegistry, GeneratedRegistryPath,
            "Regenerate the Studio-owned block/runtime registry from built machine manifests.",
            BuildGeneratedRegistry(generated.Values));

        var ownedPaths = new List<string>(changes
            .Where(change => !IsDelete(change))
            .Select(change => change.RelativePath)
            .Where(path => !path.Equals(manifestPath, StringComparison.OrdinalIgnoreCase)));
        ownedPaths.Add(runtimePath);
        ownedPaths.Add(recipePath);
        ownedPaths.Add(GeneratedRegistryPath);
        ownedPaths.Add(manifestPath);
        ownedPaths = ownedPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        AddStaleOwnedDeletes(changes, root, manifestPath, ownedPaths, document.AssetId);
        AddChange(changes, root, ForeverStudioMachineProductionChangeKind.BuildManifest, manifestPath,
            "Record the runtime backend and generated files for deterministic rebuilds.",
            SerializeManifest(document, ownedPaths));

        issues.Add(new StudioValidationIssue(StudioValidationSeverity.Info,
            $"Runtime compiler ready: {runtime.Slots.Count} item slot(s), {runtime.Tanks.Count} tank(s), " +
            $"{runtime.Recipes.Count} recipe(s), {runtime.GraphNodes.Count} graph node(s), " +
            $"{runtime.Animations.Count} animation binding(s)."));
        return Plan(document, changes, issues);
    }

    internal static ForeverStudioMachineProductionApplyResult Apply(ForeverStudioMachineProductionPlan plan)
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
                if (IsDelete(item.Change)) File.Delete(item.Change.FullPath);
                else File.Move(item.Temp!, item.Change.FullPath, overwrite: true);
                committed.Add((item.Change, item.Backup));
            }
            foreach (var item in staged)
                if (item.Backup is not null && File.Exists(item.Backup)) File.Delete(item.Backup);
            return new ForeverStudioMachineProductionApplyResult(plan.AssetId,
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
                    else if (!item.Change.ExistedBefore && File.Exists(item.Change.FullPath)) File.Delete(item.Change.FullPath);
                }
                catch { }
            }
            foreach (var item in staged)
                try { if (item.Backup is not null && File.Exists(item.Backup)) File.Delete(item.Backup); } catch { }
            throw;
        }
    }

    private static RuntimeContract? CompileContract(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        List<StudioValidationIssue> issues)
    {
        var core = document.Behavior.Nodes.Where(node =>
            node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase)).ToList();
        var processors = document.Behavior.Nodes.Where(node => IsProcessor(node.LibraryItemId)).ToList();
        if (core.Count != 1)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Runtime machines require exactly one Machine Entry node."));
        if (processors.Count != 1)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                "Runtime machines require exactly one Standard, Powered, Fluid Processor or Generator Core node."));
        if (HasErrors(issues)) return null;

        var processor = processors.Single();
        var processorKind = ProcessorKind(processor.LibraryItemId);
        var itemInputs = document.Behavior.Nodes.Where(node => IsConsumableInput(node.LibraryItemId))
            .OrderBy(node => node.Y).ThenBy(node => node.X).ThenBy(node => node.Id, StringComparer.OrdinalIgnoreCase).ToList();
        var itemOutputs = document.Behavior.Nodes.Where(node => IsOutput(node.LibraryItemId))
            .OrderBy(node => node.Y).ThenBy(node => node.X).ThenBy(node => node.Id, StringComparer.OrdinalIgnoreCase).ToList();
        var storage = document.Behavior.Nodes.Where(node => node.LibraryItemId.Equals("function.upgrade-slot", StringComparison.OrdinalIgnoreCase))
            .OrderBy(node => node.Y).ThenBy(node => node.X).ThenBy(node => node.Id, StringComparer.OrdinalIgnoreCase).ToList();
        var energyNodes = document.Behavior.Nodes.Where(node =>
            node.LibraryItemId.Equals("function.energy-storage", StringComparison.OrdinalIgnoreCase)).ToList();
        var tankNodes = document.Behavior.Nodes.Where(node =>
            node.LibraryItemId.Equals("function.fluid-tank", StringComparison.OrdinalIgnoreCase))
            .OrderBy(node => node.Y).ThenBy(node => node.X).ThenBy(node => node.Id, StringComparer.OrdinalIgnoreCase).ToList();

        if (processorKind != "generator" && itemInputs.Count == 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "A processing runtime needs at least one item input slot."));
        if (processorKind != "generator" && itemOutputs.Count == 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "A processing runtime needs at least one item output slot."));
        var powered = processorKind is "powered" or "generator";
        if (powered && energyNodes.Count != 1)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"{processorKind} runtime requires exactly one Energy Storage node."));
        if (!powered && energyNodes.Count > 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Energy Storage requires a Powered Processor or Generator Core."));
        if (processorKind == "fluid" && tankNodes.Count == 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Fluid Processor requires at least one Fluid Tank node."));
        if (processorKind != "fluid" && tankNodes.Count > 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Warning,
                "Fluid tanks are present on a non-fluid processor; they remain available as storage but are not recipe prerequisites."));

        ValidateGraph(document, catalog, processor, issues);
        if (HasErrors(issues)) return null;

        var slots = new List<SlotData>();
        var slotByNode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var nextSlot = 0;
        foreach (var node in itemInputs)
        {
            var (x, y) = SlotGeometry(document, node.Id, nextSlot, true);
            slotByNode[node.Id] = nextSlot;
            slots.Add(new SlotData(node.Id, "input", nextSlot++, x, y));
        }
        foreach (var node in itemOutputs)
        {
            var (x, y) = SlotGeometry(document, node.Id, nextSlot, false);
            slotByNode[node.Id] = nextSlot;
            slots.Add(new SlotData(node.Id, "output", nextSlot++, x, y));
        }
        foreach (var node in storage)
        {
            var (x, y) = SlotGeometry(document, node.Id, nextSlot, true);
            slotByNode[node.Id] = nextSlot;
            slots.Add(new SlotData(node.Id, "storage", nextSlot++, x, y));
        }
        if (nextSlot > 64)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Runtime inventory exceeds the 64-slot safety limit."));

        var tanks = new List<TankData>();
        var tankByNode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < tankNodes.Count; index++)
        {
            var node = tankNodes[index];
            var capacity = ReadPositiveNodeInt(node, "capacity", 4_000, 1, 2_000_000_000, issues);
            var maxFill = ReadPositiveNodeInt(node, "maxFill", capacity, 1, 2_000_000_000, issues);
            var maxDrain = ReadPositiveNodeInt(node, "maxDrain", capacity, 1, 2_000_000_000, issues);
            tankByNode[node.Id] = index;
            tanks.Add(new TankData(node.Id, index, capacity, maxFill, maxDrain));
        }
        if (tanks.Count > 8)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Runtime fluid storage supports at most 8 tanks per machine."));

        var recipes = CompileRecipes(document, processor, processorKind, itemInputs.Count, itemOutputs.Count, tankByNode, issues);
        var widgets = CompileWidgets(document, processor, slotByNode, tankByNode, powered ? energyNodes.SingleOrDefault() : null, issues);
        var animations = CompileAnimations(document, issues);
        if (HasErrors(issues)) return null;

        var energy = powered ? energyNodes.Single() : null;
        var capacityEnergy = powered ? ReadPositiveNodeInt(energy!, "capacity", 100_000, 1, 2_000_000_000, issues) : 0;
        var maxReceive = powered ? ReadPositiveNodeInt(energy!, "maxReceive", 4_000, 1, 2_000_000_000, issues) : 0;
        if (HasErrors(issues)) return null;

        var graphNodes = document.Behavior.Nodes.Select(node => new GraphNodeData(
            node.Id, node.LibraryItemId,
            node.Properties.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.OrdinalIgnoreCase))).ToList();
        var graphConnections = document.Behavior.Connections.Select(connection => new GraphConnectionData(
            connection.Id, connection.FromNodeId, connection.FromPortId, connection.ToNodeId, connection.ToPortId)).ToList();
        var states = document.States.Select(state => new StateData(state.Id, state.IsInitial)).ToList();

        return new RuntimeContract(processor, processorKind, slots, tanks, energy, powered,
            capacityEnergy, maxReceive, recipes, widgets, graphNodes, graphConnections, states, animations);
    }

    private static List<RuntimeRecipeData> CompileRecipes(
        ForeverStudioMachineDocument document,
        ForeverStudioMachineBehaviorNode processor,
        string processorKind,
        int inputSlots,
        int outputSlots,
        IReadOnlyDictionary<string, int> tankByNode,
        List<StudioValidationIssue> issues)
    {
        if (document.Recipes.Count == 0)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, "Runtime processing requires at least one authored recipe."));
        var result = new List<RuntimeRecipeData>();
        var signatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var recipe in document.Recipes)
        {
            var expectedType = processorKind switch
            {
                "powered" => "forever_skyblock:powered_processing",
                "fluid" => "forever_skyblock:fluid_processing",
                "generator" => "forever_skyblock:generator",
                _ => "forever_skyblock:machine_processing"
            };
            if (!recipe.RecipeTypeId.Equals(expectedType, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' uses '{recipe.RecipeTypeId}', but processor '{processor.Id}' requires '{expectedType}'."));
                continue;
            }
            var processNodeId = ReadString(recipe.Properties, "processorNodeId", string.Empty);
            if (!processNodeId.Equals(processor.Id, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' is not bound to processor '{processor.Id}'."));
                continue;
            }
            if (!TryReadStacks(recipe.Properties, "inputs", "input", out var inputs) ||
                !TryReadStacks(recipe.Properties, "outputs", "output", out var outputs) ||
                !TryReadInt(recipe.Properties, "processingTimeTicks", 1, 1_000_000, out var duration))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' has invalid item stacks or processing time."));
                continue;
            }
            if (processorKind == "generator" && outputs.Count == 0) outputs = [];
            if (inputSlots != inputs.Count)
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' declares {inputs.Count} item input(s), but the Function Graph exposes {inputSlots} consumable input slot(s)."));
                continue;
            }
            if (processorKind != "generator" && outputs.Count != outputSlots)
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' declares {outputs.Count} item output(s), but the Function Graph exposes {outputSlots} output slot(s)."));
                continue;
            }
            if (processorKind == "generator" && outputs.Count > outputSlots)
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Generator recipe '{recipe.Id}' declares more outputs than available output slots."));
                continue;
            }

            var fluidInputs = TryReadFluids(recipe.Properties, "fluidInputs", "fluidInput", tankByNode, issues, recipe.Id);
            var fluidOutputs = TryReadFluids(recipe.Properties, "fluidOutputs", "fluidOutput", tankByNode, issues, recipe.Id);
            if (processorKind == "fluid" && fluidInputs.Count == 0)
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Fluid recipe '{recipe.Id}' requires at least one fluid input."));
            if (processorKind != "fluid" && (fluidInputs.Count > 0 || fluidOutputs.Count > 0))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' declares fluids but its processor is not a Fluid Processor."));

            var fePerTick = ReadNestedInt(recipe.Properties, "energy", "fePerTick", 0);
            var energyOutput = ReadNestedInt(recipe.Properties, "energy", "feOutputPerTick", 0);
            if (processorKind == "generator" && energyOutput == 0)
                energyOutput = Math.Max(0, fePerTick);
            if (processorKind != "generator" && energyOutput != 0)
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' generates FE but the selected processor is not a Generator Core."));
            if (processorKind is not ("powered" or "generator") && fePerTick != 0)
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' uses FE/t but the selected processor has no energy storage."));
            if (fePerTick is < 0 or > 1_000_000 || energyOutput is < 0 or > 1_000_000)
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Recipe '{recipe.Id}' has unsupported FE values."));

            var signature = string.Join("|", inputs.Select(stack => stack.Item + "@" + stack.Count)) + ":" +
                            string.Join("|", fluidInputs.Select(fluid => fluid.Tank + "=" + fluid.Fluid + "@" + fluid.Amount));
            if (!signatures.Add(signature))
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipe.Id}' duplicates an existing item/fluid input signature."));

            result.Add(new RuntimeRecipeData(recipe.Id, inputs, outputs, fluidInputs, fluidOutputs,
                duration, processorKind == "generator" ? 0 : fePerTick, energyOutput));
        }
        return result;
    }

    private static List<WidgetData> CompileWidgets(
        ForeverStudioMachineDocument document,
        ForeverStudioMachineBehaviorNode processor,
        IReadOnlyDictionary<string, int> slotByNode,
        IReadOnlyDictionary<string, int> tankByNode,
        ForeverStudioMachineBehaviorNode? energy,
        List<StudioValidationIssue> issues)
    {
        var widgets = new List<WidgetData>();
        var usedSlots = new HashSet<int>();
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
            var slot = slotByNode.TryGetValue(binding.SourceId, out var mappedSlot) ? mappedSlot : -1;
            var tank = tankByNode.TryGetValue(binding.SourceId, out var mappedTank) ? mappedTank : -1;
            var valid = type switch
            {
                "item-slot" or "output-slot" => slot >= 0,
                "progress-bar" or "status" => binding.SourceId.Equals(processor.Id, StringComparison.OrdinalIgnoreCase),
                "energy-bar" => energy is not null && binding.SourceId.Equals(energy.Id, StringComparison.OrdinalIgnoreCase),
                "fluid-gauge" => tank >= 0,
                "value" => IsRuntimeValueSource(binding.SourceId, sourcePort, processor, energy, tankByNode),
                _ => false
            };
            if (!valid)
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"GUI binding '{binding.Id}' ({type}) cannot be synchronized from '{binding.SourceId}.{sourcePort}'."));
                continue;
            }
            if (slot >= 0 && type is "item-slot" or "output-slot" && !usedSlots.Add(slot))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"GUI binding '{binding.Id}' duplicates machine slot {slot}."));
                continue;
            }
            widgets.Add(new WidgetData(binding.Id, type, binding.SourceId, sourcePort, label,
                x, y, width, height, slot, tank));
        }
        return widgets;
    }

    private static List<AnimationData> CompileAnimations(ForeverStudioMachineDocument document, List<StudioValidationIssue> issues)
    {
        var components = document.Components.Select(component => component.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nodeIds = document.Behavior.Nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new List<AnimationData>();
        foreach (var binding in document.Animations)
        {
            if (!binding.LibraryItemId.StartsWith("animation.", StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Animation '{binding.Id}' does not reference an animation preset."));
                continue;
            }
            if (!components.Contains(binding.TargetComponentId))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Animation '{binding.Id}' targets missing component '{binding.TargetComponentId}'."));
                continue;
            }
            var sourceNode = ReadString(binding.Properties, "sourceNodeId", string.Empty);
            var sourcePort = ReadString(binding.Properties, "sourcePortId", string.Empty);
            if (!string.IsNullOrWhiteSpace(sourceNode) && !nodeIds.Contains(sourceNode))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Animation '{binding.Id}' references missing Function node '{sourceNode}'."));
                continue;
            }
            var axis = ReadString(binding.Properties, "axis", "y").ToLowerInvariant();
            if (axis is not ("x" or "y" or "z"))
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Animation '{binding.Id}' has invalid axis '{axis}'."));
                continue;
            }
            var scale = ReadNumber(binding.Properties, "scale", 1);
            if (!double.IsFinite(scale) || Math.Abs(scale) > 10_000)
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error, $"Animation '{binding.Id}' has an unsafe scale."));
                continue;
            }
            result.Add(new AnimationData(binding.Id, binding.LibraryItemId, binding.TargetComponentId,
                sourceNode, sourcePort, axis, scale));
        }
        return result;
    }

    private static void ValidateGraph(
        ForeverStudioMachineDocument document,
        ForeverStudioLibraryCatalog catalog,
        ForeverStudioMachineBehaviorNode processor,
        List<StudioValidationIssue> issues)
    {
        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "core.machine-entry",
            "function.on-tick", "function.on-use", "function.on-redstone-changed", "function.on-neighbor-changed",
            "function.on-inventory-changed", "function.on-recipe-start", "function.on-recipe-complete", "function.on-state-enter",
            "function.item-input", "function.item-output", "function.item-storage", "function.filter-slot", "function.catalyst-slot",
            "function.fuel-slot", "function.upgrade-slot", "function.consume-item", "function.produce-item",
            "function.energy-storage", "function.has-energy", "function.consume-energy", "function.generate-energy", "function.transfer-energy",
            "function.fluid-tank", "function.has-fluid", "function.consume-fluid", "function.produce-fluid", "function.transfer-fluid",
            "function.find-recipe", "function.recipe-available", "function.output-space", "function.start-process",
            "function.process-progress", "function.complete-process", "function.process-duration", "function.chance-output", "function.secondary-output",
            "function.branch", "function.and", "function.or", "function.not", "function.compare-number", "function.compare-state",
            "function.random-chance", "function.number", "function.boolean", "function.timer", "function.counter", "function.clamp",
            "function.map-range", "function.set-state", "function.redstone-input", "function.standard-processor",
            "function.powered-processor", "function.fluid-processor", "function.generator-core", "function.redstone-controller"
        };
        var unsupported = document.Behavior.Nodes.Where(node => !supported.Contains(node.LibraryItemId)).ToList();
        foreach (var node in unsupported)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Function node '{node.Id}' ({node.LibraryItemId}) has no production runtime implementation."));

        // At least one event/entry path must be wired into the selected processor. Additional typed
        // graph topology is serialized and evaluated by the Java graph runtime.
        var hasProcessorFlow = document.Behavior.Connections.Any(connection =>
            connection.ToNodeId.Equals(processor.Id, StringComparison.OrdinalIgnoreCase) &&
            connection.ToPortId.Equals("flow-in", StringComparison.OrdinalIgnoreCase));
        if (!hasProcessorFlow)
            issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                $"Processor '{processor.Id}' has no incoming flow connection."));

        foreach (var node in document.Behavior.Nodes)
        {
            if (catalog.Find(node.LibraryItemId, node.LibraryItemVersion) is null)
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Function node '{node.Id}' references an unavailable library contract."));
        }
    }

    private static ForeverStudioMachineDocument BuildVisualOnlyClone(ForeverStudioMachineDocument source)
    {
        var initial = source.States.FirstOrDefault(state => state.IsInitial) ?? source.States.First();
        var entry = source.Behavior.Nodes.First(node => node.LibraryItemId.Equals("core.machine-entry", StringComparison.OrdinalIgnoreCase));
        return new ForeverStudioMachineDocument
        {
            FormatVersion = source.FormatVersion,
            AssetId = source.AssetId,
            DisplayName = source.DisplayName,
            Description = source.Description,
            Namespace = source.Namespace,
            MachineType = source.MachineType,
            Components = source.Components.Select(component => new ForeverStudioMachineComponent
            {
                Id = component.Id, DisplayName = component.DisplayName, LibraryItemId = component.LibraryItemId,
                LibraryItemVersion = component.LibraryItemVersion, ParentId = component.ParentId,
                Properties = component.Properties.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.OrdinalIgnoreCase)
            }).ToList(),
            States = [new ForeverStudioMachineState
            {
                Id = initial.Id, DisplayName = initial.DisplayName, IsInitial = true,
                Properties = initial.Properties.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.OrdinalIgnoreCase)
            }],
            Behavior = new ForeverStudioMachineBehaviorGraph
            {
                Nodes = [new ForeverStudioMachineBehaviorNode
                {
                    Id = entry.Id, LibraryItemId = entry.LibraryItemId, LibraryItemVersion = entry.LibraryItemVersion,
                    X = entry.X, Y = entry.Y,
                    Properties = entry.Properties.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.OrdinalIgnoreCase)
                }]
            },
            Recipes = [],
            Gui = new ForeverStudioMachineGuiDefinition(),
            Animations = [],
            Library = new ForeverStudioMachineLibraryManifest()
        };
    }

    private static string SerializeRuntimeDefinition(ForeverStudioMachineDocument document, RuntimeContract runtime)
        => Json(new Dictionary<string, object?>
        {
            ["schemaVersion"] = 2,
            ["machine"] = $"forever_skyblock:{document.AssetId}",
            ["displayName"] = document.DisplayName,
            ["processor"] = new Dictionary<string, object?>
            {
                ["nodeId"] = runtime.Processor.Id,
                ["kind"] = runtime.ProcessorKind,
                ["powered"] = runtime.Powered
            },
            ["inventory"] = runtime.Slots.Select(slot => new Dictionary<string, object?>
            {
                ["nodeId"] = slot.NodeId, ["role"] = slot.Role, ["slot"] = slot.Slot, ["x"] = slot.X, ["y"] = slot.Y
            }).ToArray(),
            ["energy"] = new Dictionary<string, object?>
            {
                ["enabled"] = runtime.Powered,
                ["capacity"] = runtime.EnergyCapacity,
                ["maxReceive"] = runtime.MaxReceive
            },
            ["fluids"] = runtime.Tanks.Select(tank => new Dictionary<string, object?>
            {
                ["nodeId"] = tank.NodeId, ["tank"] = tank.Tank, ["capacity"] = tank.Capacity,
                ["maxFill"] = tank.MaxFill, ["maxDrain"] = tank.MaxDrain
            }).ToArray(),
            ["graph"] = new Dictionary<string, object?>
            {
                ["states"] = runtime.States.Select(state => new Dictionary<string, object?>
                {
                    ["id"] = state.Id, ["initial"] = state.Initial
                }).ToArray(),
                ["nodes"] = runtime.GraphNodes.Select(node => new Dictionary<string, object?>
                {
                    ["id"] = node.Id, ["kind"] = node.Kind, ["properties"] = node.Properties
                }).ToArray(),
                ["connections"] = runtime.GraphConnections.Select(connection => new Dictionary<string, object?>
                {
                    ["id"] = connection.Id, ["fromNode"] = connection.FromNode, ["fromPort"] = connection.FromPort,
                    ["toNode"] = connection.ToNode, ["toPort"] = connection.ToPort
                }).ToArray()
            },
            ["animations"] = runtime.Animations.Select(animation => new Dictionary<string, object?>
            {
                ["id"] = animation.Id, ["preset"] = animation.Preset, ["targetComponent"] = animation.TargetComponent,
                ["sourceNode"] = animation.SourceNode, ["sourcePort"] = animation.SourcePort,
                ["axis"] = animation.Axis, ["scale"] = animation.Scale
            }).ToArray(),
            ["gui"] = new Dictionary<string, object?>
            {
                ["width"] = 176, ["height"] = 166,
                ["widgets"] = runtime.Widgets.Select(widget => new Dictionary<string, object?>
                {
                    ["id"] = widget.Id, ["type"] = widget.Type, ["sourceNode"] = widget.SourceNode,
                    ["sourcePort"] = widget.SourcePort, ["label"] = widget.Label,
                    ["x"] = widget.X, ["y"] = widget.Y, ["width"] = widget.Width, ["height"] = widget.Height,
                    ["slot"] = widget.Slot, ["tank"] = widget.Tank
                }).ToArray()
            }
        });

    private static string SerializeRuntimeRecipeBook(ForeverStudioMachineDocument document, RuntimeContract runtime)
        => Json(new Dictionary<string, object?>
        {
            ["type"] = "forever_skyblock:forever_studio_machine",
            ["machine"] = $"forever_skyblock:{document.AssetId}",
            ["recipes"] = runtime.Recipes.Select(recipe => new Dictionary<string, object?>
            {
                ["inputs"] = recipe.Inputs.Select(input => new Dictionary<string, object?>
                {
                    ["ingredient"] = new Dictionary<string, object?> { ["item"] = input.Item }, ["count"] = input.Count
                }).ToArray(),
                ["results"] = recipe.Outputs.Select(output => new Dictionary<string, object?>
                {
                    ["id"] = output.Item, ["count"] = output.Count
                }).ToArray(),
                ["fluid_inputs"] = recipe.FluidInputs.Select(fluid => new Dictionary<string, object?>
                {
                    ["tank"] = fluid.Tank, ["fluid"] = fluid.Fluid, ["amount"] = fluid.Amount
                }).ToArray(),
                ["fluid_results"] = recipe.FluidOutputs.Select(fluid => new Dictionary<string, object?>
                {
                    ["tank"] = fluid.Tank, ["fluid"] = fluid.Fluid, ["amount"] = fluid.Amount
                }).ToArray(),
                ["processing_time"] = recipe.ProcessingTime,
                ["energy_per_tick"] = recipe.EnergyPerTick,
                ["energy_output_per_tick"] = recipe.EnergyOutputPerTick
            }).ToArray()
        });

    private static string SerializeManifest(ForeverStudioMachineDocument document, IReadOnlyList<string> ownedPaths)
        => Json(new Dictionary<string, object?>
        {
            ["schemaVersion"] = ManifestSchemaVersion,
            ["assetId"] = document.AssetId,
            ["displayName"] = document.DisplayName,
            ["productionKind"] = ProductionKindRuntimeMachine,
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
                if (kind is not (ForeverStudioMachineProductionService.ProductionKindStaticBlock or ProductionKindRuntimeMachine)) continue;
                result[id] = new GeneratedManifestEntry(id, kind);
            }
            catch (Exception ex) when (ex is JsonException or IOException) { }
        }
        return result;
    }

    private static string BuildGeneratedRegistry(IEnumerable<GeneratedManifestEntry> entries)
    {
        var machines = entries.Where(entry => IsSafeAssetId(entry.AssetId))
            .GroupBy(entry => entry.AssetId, StringComparer.OrdinalIgnoreCase).Select(group => group.Last())
            .OrderBy(entry => entry.AssetId, StringComparer.OrdinalIgnoreCase).ToList();
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
                b.AppendLine($"            () -> new ForeverStudioMachineBlock(ResourceLocation.fromNamespaceAndPath(ForeverSkyblock.MOD_ID, \"{machine.AssetId}\"),");
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
        var validBlocks = runtime.Count == 0 ? string.Empty : ", " + string.Join(", ", runtime.Select(machine => JavaFieldName(machine.AssetId) + ".get()"));
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
        b.AppendLine("    public static void register(IEventBus modBus) {");
        b.AppendLine("        BLOCKS.register(modBus); ITEMS.register(modBus); BLOCK_ENTITIES.register(modBus); MENUS.register(modBus); RECIPE_TYPES.register(modBus); RECIPE_SERIALIZERS.register(modBus);");
        b.AppendLine("        modBus.addListener(ForeverStudioGeneratedMachines::registerCapabilities);");
        b.AppendLine("        modBus.addListener(ForeverStudioGeneratedMachines::addCreativeContents);");
        b.AppendLine("    }");
        b.AppendLine("    private static void registerCapabilities(RegisterCapabilitiesEvent event) {");
        b.AppendLine("        event.registerBlockEntity(Capabilities.ItemHandler.BLOCK, MACHINE_BLOCK_ENTITY.get(), ForeverStudioMachineBlockEntity::itemAutomation);");
        b.AppendLine("        event.registerBlockEntity(Capabilities.EnergyStorage.BLOCK, MACHINE_BLOCK_ENTITY.get(), ForeverStudioMachineBlockEntity::energyAutomation);");
        b.AppendLine("        event.registerBlockEntity(Capabilities.FluidHandler.BLOCK, MACHINE_BLOCK_ENTITY.get(), ForeverStudioMachineBlockEntity::fluidAutomation);");
        b.AppendLine("    }");
        b.AppendLine("    private static void addCreativeContents(BuildCreativeModeTabContentsEvent event) {");
        b.AppendLine("        if (event.getTabKey() != CreativeModeTabs.FUNCTIONAL_BLOCKS) return;");
        foreach (var machine in machines) b.AppendLine($"        event.accept({JavaFieldName(machine.AssetId)}_ITEM.get());");
        b.AppendLine("    }");
        b.AppendLine("}");
        return b.ToString();
    }

    private static void AddStaleOwnedDeletes(List<ForeverStudioMachineProductionChange> changes, string root,
        string manifestPath, IReadOnlyCollection<string> currentOwnedPaths, string assetId)
    {
        var fullManifest = ResolveRepoPath(root, manifestPath);
        if (!File.Exists(fullManifest)) return;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(fullManifest));
            if (!document.RootElement.TryGetProperty("generatedFiles", out var files) || files.ValueKind != JsonValueKind.Array) return;
            var current = currentOwnedPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var runtimeDefinition = $"{RuntimeDirectory}/{assetId}.json";
            var recipePrefix = $"{RuntimeRecipeDirectory}/{assetId}";
            foreach (var item in files.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String || item.GetString() is not { } path || current.Contains(path)) continue;
                var safe = path.Equals(runtimeDefinition, StringComparison.OrdinalIgnoreCase) ||
                           path.Equals(recipePrefix + ".json", StringComparison.OrdinalIgnoreCase) ||
                           path.StartsWith(recipePrefix + "/", StringComparison.OrdinalIgnoreCase);
                if (safe) AddDelete(changes, root, path, "Remove stale Studio-owned runtime output.");
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            throw new InvalidDataException($"Existing build manifest '{manifestPath}' cannot be read safely.", ex);
        }
    }

    private static bool TryReadStacks(Dictionary<string, JsonElement> properties, string pluralKey, string legacyKey, out List<StackData> stacks)
    {
        stacks = [];
        if (properties.TryGetValue(pluralKey, out var plural) && plural.ValueKind == JsonValueKind.Array)
        {
            foreach (var element in plural.EnumerateArray())
            {
                if (!TryReadStack(element, out var stack)) return false;
                stacks.Add(stack);
            }
            return stacks.Count > 0;
        }
        if (properties.TryGetValue(legacyKey, out var legacy) && TryReadStack(legacy, out var single))
        {
            stacks.Add(single);
            return true;
        }
        // Generators may intentionally have no item outputs.
        return pluralKey.Equals("outputs", StringComparison.OrdinalIgnoreCase) &&
               properties.TryGetValue(pluralKey, out plural) && plural.ValueKind == JsonValueKind.Array && plural.GetArrayLength() == 0;
    }

    private static bool TryReadStack(JsonElement value, out StackData stack)
    {
        stack = new StackData(string.Empty, 0);
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("item", out var itemJson) ||
            itemJson.ValueKind != JsonValueKind.String || !value.TryGetProperty("count", out var countJson) ||
            !countJson.TryGetInt32(out var count)) return false;
        var item = itemJson.GetString()?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!IsResourceLocation(item) || count is < 1 or > 64) return false;
        stack = new StackData(item, count);
        return true;
    }

    private static List<FluidData> TryReadFluids(Dictionary<string, JsonElement> properties, string pluralKey,
        string legacyKey, IReadOnlyDictionary<string, int> tankByNode, List<StudioValidationIssue> issues, string recipeId)
    {
        var values = new List<JsonElement>();
        if (properties.TryGetValue(pluralKey, out var plural) && plural.ValueKind == JsonValueKind.Array)
            values.AddRange(plural.EnumerateArray().Select(value => value.Clone()));
        else if (properties.TryGetValue(legacyKey, out var legacy) && legacy.ValueKind == JsonValueKind.Object)
            values.Add(legacy.Clone());
        var result = new List<FluidData>();
        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            var fluid = value.TryGetProperty("fluid", out var fluidJson) && fluidJson.ValueKind == JsonValueKind.String
                ? fluidJson.GetString()?.Trim().ToLowerInvariant() ?? string.Empty : string.Empty;
            var amount = value.TryGetProperty("amount", out var amountJson) && amountJson.TryGetInt32(out var parsedAmount) ? parsedAmount : 0;
            var nodeId = value.TryGetProperty("tankNodeId", out var tankJson) && tankJson.ValueKind == JsonValueKind.String
                ? tankJson.GetString() ?? string.Empty : string.Empty;
            var tank = !string.IsNullOrWhiteSpace(nodeId) && tankByNode.TryGetValue(nodeId, out var mapped) ? mapped : index;
            if (!IsResourceLocation(fluid) || amount < 1 || amount > 2_000_000_000 || tank < 0 || tank >= tankByNode.Count)
            {
                issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
                    $"Recipe '{recipeId}' has an invalid fluid stack/tank mapping in '{pluralKey}'."));
                continue;
            }
            result.Add(new FluidData(tank, fluid, amount));
        }
        return result;
    }

    private static (int X, int Y) SlotGeometry(ForeverStudioMachineDocument document, string nodeId, int index, bool input)
    {
        var binding = document.Gui.Bindings.FirstOrDefault(candidate => candidate.SourceId.Equals(nodeId, StringComparison.OrdinalIgnoreCase) &&
            ReadString(candidate.Properties, "widget", string.Empty) is "item-slot" or "output-slot");
        if (binding is not null && TryReadGuiGeometry(binding.Properties, out var x, out var y, out _, out _)) return (x, y);
        return input ? (18 + (index % 3) * 20, 48 + (index / 3) * 20) : (118 + (index % 3) * 20, 48 + (index / 3) * 20);
    }

    private static bool IsRuntimeValueSource(string sourceId, string port, ForeverStudioMachineBehaviorNode processor,
        ForeverStudioMachineBehaviorNode? energy, IReadOnlyDictionary<string, int> tanks)
    {
        if (sourceId.Equals(processor.Id, StringComparison.OrdinalIgnoreCase) && port is "progress" or "running") return true;
        if (energy is not null && sourceId.Equals(energy.Id, StringComparison.OrdinalIgnoreCase) && port is "energy" or "ratio") return true;
        return tanks.ContainsKey(sourceId) && port is "ratio" or "fluid";
    }

    private static bool IsProcessor(string id) => id is "function.standard-processor" or "function.powered-processor" or "function.fluid-processor" or "function.generator-core";
    private static string ProcessorKind(string id) => id switch { "function.powered-processor" => "powered", "function.fluid-processor" => "fluid", "function.generator-core" => "generator", _ => "standard" };
    private static bool IsConsumableInput(string id) => id is "function.item-input" or "function.filter-slot" or "function.catalyst-slot" or "function.fuel-slot";
    private static bool IsOutput(string id) => id is "function.item-storage" or "function.item-output" or "function.secondary-output" or "function.chance-output";

    private static int ReadPositiveNodeInt(ForeverStudioMachineBehaviorNode node, string key, int fallback, int min, int max, List<StudioValidationIssue> issues)
    {
        if (!node.Properties.TryGetValue(key, out var value)) return fallback;
        if (value.TryGetInt32(out var parsed) && parsed >= min && parsed <= max) return parsed;
        issues.Add(new StudioValidationIssue(StudioValidationSeverity.Error,
            $"Function node '{node.Id}' property '{key}' must be {min}..{max}."));
        return fallback;
    }

    private static bool TryReadGuiGeometry(Dictionary<string, JsonElement> properties, out int x, out int y, out int width, out int height)
    {
        x = (int)Math.Round(ReadNumber(properties, "x", 0)); y = (int)Math.Round(ReadNumber(properties, "y", 0));
        width = (int)Math.Round(ReadNumber(properties, "width", 0)); height = (int)Math.Round(ReadNumber(properties, "height", 0));
        return x >= 0 && y >= 0 && width >= 1 && height >= 1 && x + width <= 176 && y + height <= 166;
    }

    private static int ReadNestedInt(Dictionary<string, JsonElement> properties, string owner, string child, int fallback)
        => properties.TryGetValue(owner, out var value) && value.ValueKind == JsonValueKind.Object &&
           value.TryGetProperty(child, out var nested) && nested.TryGetInt32(out var parsed) ? parsed : fallback;
    private static bool TryReadInt(Dictionary<string, JsonElement> properties, string key, int min, int max, out int result)
    { result = 0; return properties.TryGetValue(key, out var value) && value.TryGetInt32(out result) && result >= min && result <= max; }
    private static string ReadString(Dictionary<string, JsonElement> properties, string key, string fallback)
        => properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? fallback : fallback;
    private static double ReadNumber(Dictionary<string, JsonElement> properties, string key, double fallback)
        => properties.TryGetValue(key, out var value) && value.TryGetDouble(out var result) && double.IsFinite(result) ? result : fallback;

    private static ForeverStudioMachineProductionPlan Plan(ForeverStudioMachineDocument document,
        IReadOnlyList<ForeverStudioMachineProductionChange> changes, IReadOnlyList<StudioValidationIssue> issues)
        => new(document.AssetId, document.DisplayName, ProductionKindRuntimeMachine, changes, issues);
    private static bool HasErrors(IEnumerable<StudioValidationIssue> issues) => issues.Any(issue => issue.Severity == StudioValidationSeverity.Error);

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
            Kind = kind, RelativePath = relativePath.Replace('\\', '/'), Description = description,
            NewText = normalized, PreviewLines = normalized.Split('\n').Take(120).ToList(), FullPath = fullPath,
            OriginalHash = existed ? HashFile(fullPath) : MissingHash, ExistedBefore = existed
        });
    }

    private static void AddDelete(List<ForeverStudioMachineProductionChange> changes, string root, string relativePath, string description)
    {
        var fullPath = ResolveRepoPath(root, relativePath);
        if (!File.Exists(fullPath)) return;
        changes.Add(new ForeverStudioMachineProductionChange
        {
            Kind = ForeverStudioMachineProductionChangeKind.BuildManifest, RelativePath = relativePath.Replace('\\', '/'),
            Description = DeleteDescriptionPrefix + description, NewText = string.Empty, PreviewLines = ["<delete>"],
            FullPath = fullPath, OriginalHash = HashFile(fullPath), ExistedBefore = true
        });
    }

    private static bool IsDelete(ForeverStudioMachineProductionChange change) => change.Description.StartsWith(DeleteDescriptionPrefix, StringComparison.Ordinal);
    private static bool IsResourceLocation(string value)
    {
        var split = value.Split(':', 2);
        return split.Length == 2 && split[0].Length > 0 && split[1].Length > 0 &&
               split[0].All(character => char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.') &&
               split[1].All(character => char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.' or '/');
    }
    private static bool IsSafeAssetId(string value) => value.Length is > 0 and <= 128 && value.All(character => char.IsLower(character) || char.IsDigit(character) || character is '_' or '-' or '.');
    private static string ResolveRepoPath(string root, string relativePath)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var full = Path.GetFullPath(Path.Combine(normalizedRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Production output escaped the selected repository.");
        return full;
    }
    private static string JavaFieldName(string id)
    {
        var safe = new string(id.Select(character => char.IsLetterOrDigit(character) ? char.ToUpperInvariant(character) : '_').ToArray());
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id)))[..6];
        return $"FS_{safe}_{hash}";
    }
    private static string HashFile(string path) { using var stream = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant(); }
    private static string Json(object value) => JsonSerializer.Serialize(value, JsonIndented) + Environment.NewLine;
    private static string NormalizeNewlines(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
}
