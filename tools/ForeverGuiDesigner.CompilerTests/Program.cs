using System.Text.Json;
using ForeverGuiDesigner.Services;

static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);
static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static ForeverStudioMachineBehaviorNode Node(string id, string kind, double x, double y) => new()
{
    Id = id,
    LibraryItemId = kind,
    LibraryItemVersion = 1,
    X = x,
    Y = y
};

static void ConnectEntry(ForeverStudioMachineDocument document, string processorId)
{
    document.Behavior.Connections.Add(new ForeverStudioMachineBehaviorConnection
    {
        Id = "entry_" + processorId,
        FromNodeId = "machine_entry",
        FromPortId = "flow",
        ToNodeId = processorId,
        ToPortId = "flow-in"
    });
}

var root = Path.Combine(Path.GetTempPath(), "forever-studio-compiler-e2e-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    var mainPath = Path.Combine(root, "mod", "src", "main", "java", "de", "tekatz", "foreverskyblock", "ForeverSkyblock.java");
    Directory.CreateDirectory(Path.GetDirectoryName(mainPath)!);
    File.WriteAllText(mainPath, """
package de.tekatz.foreverskyblock;
public final class ForeverSkyblock {
    void register() {
        RECIPE_SERIALIZERS.register(modBus);
    }
}
""");

    var texturePath = Path.Combine(root, "mod", "src", "main", "resources", "assets", "forever_skyblock", "textures", "block", "ascension_materials.png");
    Directory.CreateDirectory(Path.GetDirectoryName(texturePath)!);
    File.WriteAllBytes(texturePath, [0]);

    var libraryLoad = ForeverStudioLibraryCatalog.Load(root);
    Check(libraryLoad.Warnings.Count == 0, "Built-in Studio library must load without warnings: " + string.Join(" | ", libraryLoad.Warnings));
    var catalog = libraryLoad.Catalog;
    var housing = catalog.Find("asset.housing", 1) ?? throw new InvalidOperationException("Missing built-in asset.housing.");

    // Scenario 1: the production contract must preserve deterministic two-input/two-output
    // processing, graph state, split component geometry and animation metadata through Review & Build.
    var multi = ForeverStudioMachineProjectService.CreateBlank("studio_compiler_e2e", "Studio Compiler E2E");
    var multiHousing = ForeverStudioVisualAssemblyService.CreateComponent(housing, multi.Components);
    multi.Components.Add(multiHousing);
    var processor = Node("processor", "function.powered-processor", 360, 120);
    var inputA = Node("input_a", "function.item-input", 100, 210);
    var inputB = Node("input_b", "function.item-input", 100, 370);
    var outputA = Node("output_a", "function.item-storage", 650, 210);
    var outputB = Node("output_b", "function.item-storage", 650, 370);
    var energy = Node("energy", "function.energy-storage", 360, 430);
    energy.Properties["capacity"] = Json(120_000);
    energy.Properties["maxReceive"] = Json(5_000);
    multi.Behavior.Nodes.AddRange([processor, inputA, inputB, outputA, outputB, energy]);
    ConnectEntry(multi, processor.Id);
    multi.Recipes.Add(new ForeverStudioMachineRecipeBinding
    {
        Id = "dual_process",
        RecipeTypeId = "forever_skyblock:powered_processing",
        Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["processorNodeId"] = Json(processor.Id),
            ["inputs"] = Json(new[]
            {
                new { item = "minecraft:cobblestone", count = 1 },
                new { item = "minecraft:flint", count = 2 }
            }),
            ["outputs"] = Json(new[]
            {
                new { item = "minecraft:gravel", count = 1 },
                new { item = "minecraft:sand", count = 1 }
            }),
            ["processingTimeTicks"] = Json(100),
            ["energy"] = Json(new { fePerTick = 20, totalFe = 2000 })
        }
    });
    multi.Animations.Add(new ForeverStudioMachineAnimationBinding
    {
        Id = "progress_motion",
        LibraryItemId = "animation.follow-progress",
        LibraryItemVersion = 1,
        TargetComponentId = multiHousing.Id,
        Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["sourceNodeId"] = Json(processor.Id),
            ["sourcePortId"] = Json("progress"),
            ["sourceType"] = Json("number"),
            ["axis"] = Json("y"),
            ["scale"] = Json(1.0)
        }
    });

    _ = ForeverStudioMachineProjectService.Serialize(multi);
    var plan = ForeverStudioMachineCompilerService.BuildPlan(root, multi, catalog);
    Check(plan.ProductionKind == ForeverStudioMachineCompilerService.ProductionKindRuntimeMachine,
        "Compiler must choose runtime-machine production for executable behavior.");
    Check(plan.CanApply, "Multi-I/O compiler plan must be applyable: " + string.Join(" | ", plan.Issues.Select(issue => issue.Message)));
    _ = ForeverStudioMachineCompilerService.Apply(plan);

    var runtimePath = Path.Combine(root, "mod", "src", "main", "resources", "data", "forever_skyblock", "forever_studio", "runtime", "studio_compiler_e2e.json");
    var recipePath = Path.Combine(root, "mod", "src", "main", "resources", "data", "forever_skyblock", "recipe", "forever_studio", "studio_compiler_e2e.json");
    var registryPath = Path.Combine(root, "mod", "src", "main", "java", "de", "tekatz", "foreverskyblock", "generated", "ForeverStudioGeneratedMachines.java");
    var manifestPath = Path.Combine(root, "mod", "src", "main", "resources", "data", "forever_skyblock", "forever_studio", "generated", "studio_compiler_e2e.json");
    var componentManifestPath = Path.Combine(root, "mod", "src", "main", "resources", "assets", "forever_skyblock", "forever_studio", "components", "studio_compiler_e2e.json");

    using (var runtime = JsonDocument.Parse(File.ReadAllText(runtimePath)))
    {
        var json = runtime.RootElement;
        Check(json.GetProperty("schemaVersion").GetInt32() == 2, "Runtime definition schema must be 2.");
        Check(json.GetProperty("machine").GetString() == "forever_skyblock:studio_compiler_e2e", "Runtime machine id must match.");
        Check(json.GetProperty("processor").GetProperty("kind").GetString() == "powered", "Powered processor kind must survive compilation.");
        var slots = json.GetProperty("inventory");
        Check(slots.GetArrayLength() == 4, "Multi-I/O compiler must emit two inputs and two outputs.");
        Check(slots[0].GetProperty("role").GetString() == "input" && slots[1].GetProperty("role").GetString() == "input", "First two slots must be inputs.");
        Check(slots[2].GetProperty("role").GetString() == "output" && slots[3].GetProperty("role").GetString() == "output", "Last two slots must be outputs.");
        Check(json.GetProperty("graph").GetProperty("nodes").GetArrayLength() >= 7, "Typed Function Graph must be emitted into the runtime definition.");
        Check(json.GetProperty("animations").GetArrayLength() == 1, "Animation binding must be emitted into the runtime definition.");
        Check(json.GetProperty("energy").GetProperty("capacity").GetInt32() == 120_000, "Authored FE capacity must be preserved.");
    }

    using (var recipeBook = JsonDocument.Parse(File.ReadAllText(recipePath)))
    {
        var recipe = recipeBook.RootElement.GetProperty("recipes")[0];
        Check(recipe.GetProperty("inputs").GetArrayLength() == 2, "Recipe book must contain both item inputs.");
        Check(recipe.GetProperty("results").GetArrayLength() == 2, "Recipe book must contain both item outputs.");
        Check(recipe.GetProperty("inputs")[1].GetProperty("ingredient").GetProperty("item").GetString() == "minecraft:flint", "Second input must be preserved.");
        Check(recipe.GetProperty("results")[1].GetProperty("id").GetString() == "minecraft:sand", "Second output must be preserved.");
        Check(recipe.GetProperty("energy_per_tick").GetInt32() == 20, "FE/t must be preserved.");
    }

    using (var components = JsonDocument.Parse(File.ReadAllText(componentManifestPath)))
    {
        var entries = components.RootElement.GetProperty("components");
        Check(entries.GetArrayLength() == 1, "Runtime compiler must emit one independently baked model entry for the authored housing component.");
        var component = entries[0];
        Check(component.GetProperty("id").GetString() == multiHousing.Id, "Component manifest must preserve the exact authored component id.");
        var pivot = component.GetProperty("pivot");
        Check(pivot.GetArrayLength() == 3 && Math.Abs(pivot[0].GetDouble() - 0.5) < 0.0001
              && Math.Abs(pivot[1].GetDouble() - 0.5) < 0.0001 && Math.Abs(pivot[2].GetDouble() - 0.5) < 0.0001,
            "Component manifest must preserve the authored model pivot in block coordinates.");
        var modelId = component.GetProperty("model").GetString() ?? throw new InvalidOperationException("Component model id missing.");
        Check(modelId.StartsWith("forever_skyblock:block/forever_studio/components/studio_compiler_e2e/", StringComparison.Ordinal),
            "Component model id must live under the deterministic Studio component namespace.");
        var modelPath = modelId[(modelId.IndexOf(':') + 1)..];
        var componentModelPath = Path.Combine(root, "mod", "src", "main", "resources", "assets", "forever_skyblock", "models",
            modelPath.Replace('/', Path.DirectorySeparatorChar) + ".json");
        Check(File.Exists(componentModelPath), "Component manifest must reference a generated standalone baked-model JSON file.");
        using var componentModel = JsonDocument.Parse(File.ReadAllText(componentModelPath));
        Check(componentModel.RootElement.GetProperty("elements").GetArrayLength() > 0,
            "Split component model must retain the component's actual authored geometry.");
    }

    var registry = File.ReadAllText(registryPath);
    Check(registry.Contains("new ForeverStudioMachineBlock", StringComparison.Ordinal), "Generated registry must instantiate the generic runtime block.");
    Check(registry.Contains("Capabilities.FluidHandler.BLOCK", StringComparison.Ordinal), "Generated registry must expose the fluid capability boundary.");
    using (var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath)))
    {
        Check(manifest.RootElement.GetProperty("schemaVersion").GetInt32() == 3 &&
              manifest.RootElement.GetProperty("productionKind").GetString() == "runtime-machine",
            "Manifest must record schema-3 runtime-machine production.");
        var owned = manifest.RootElement.GetProperty("generatedFiles").EnumerateArray()
            .Select(value => value.GetString() ?? string.Empty).ToArray();
        Check(owned.Any(path => path.EndsWith("assets/forever_skyblock/forever_studio/components/studio_compiler_e2e.json", StringComparison.Ordinal)),
            "Ownership manifest must include the component-model manifest.");
        Check(owned.Any(path => path.Contains("models/block/forever_studio/components/studio_compiler_e2e/", StringComparison.Ordinal)),
            "Ownership manifest must include every split component model.");
    }

    // Scenario 2: item + fluid processing must compile to an explicit tank contract and recipe requirement.
    var fluidMachine = ForeverStudioMachineProjectService.CreateBlank("studio_fluid_e2e", "Studio Fluid E2E");
    fluidMachine.Components.Add(ForeverStudioVisualAssemblyService.CreateComponent(housing, fluidMachine.Components));
    var fluidProcessor = Node("fluid_processor", "function.fluid-processor", 360, 140);
    var fluidInput = Node("item_input", "function.item-input", 100, 240);
    var tank = Node("tank", "function.fluid-tank", 100, 400);
    tank.Properties["capacity"] = Json(8_000);
    tank.Properties["maxFill"] = Json(1_000);
    tank.Properties["maxDrain"] = Json(1_000);
    var fluidOutput = Node("item_output", "function.item-storage", 650, 240);
    fluidMachine.Behavior.Nodes.AddRange([fluidProcessor, fluidInput, tank, fluidOutput]);
    ConnectEntry(fluidMachine, fluidProcessor.Id);
    fluidMachine.Recipes.Add(new ForeverStudioMachineRecipeBinding
    {
        Id = "wash_gravel",
        RecipeTypeId = "forever_skyblock:fluid_processing",
        Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["processorNodeId"] = Json(fluidProcessor.Id),
            ["inputs"] = Json(new[] { new { item = "minecraft:gravel", count = 1 } }),
            ["outputs"] = Json(new[] { new { item = "minecraft:flint", count = 1 } }),
            ["fluidInputs"] = Json(new[] { new { tankNodeId = tank.Id, fluid = "minecraft:water", amount = 250 } }),
            ["processingTimeTicks"] = Json(40),
            ["energy"] = Json(new { fePerTick = 0, totalFe = 0 })
        }
    });
    _ = ForeverStudioMachineProjectService.Serialize(fluidMachine);
    var fluidPlan = ForeverStudioMachineCompilerService.BuildPlan(root, fluidMachine, catalog);
    Check(fluidPlan.CanApply, "Fluid compiler plan must be applyable: " + string.Join(" | ", fluidPlan.Issues.Select(issue => issue.Message)));
    _ = ForeverStudioMachineCompilerService.Apply(fluidPlan);

    var fluidRuntimePath = Path.Combine(root, "mod", "src", "main", "resources", "data", "forever_skyblock", "forever_studio", "runtime", "studio_fluid_e2e.json");
    var fluidRecipePath = Path.Combine(root, "mod", "src", "main", "resources", "data", "forever_skyblock", "recipe", "forever_studio", "studio_fluid_e2e.json");
    using (var runtime = JsonDocument.Parse(File.ReadAllText(fluidRuntimePath)))
    {
        var fluids = runtime.RootElement.GetProperty("fluids");
        Check(fluids.GetArrayLength() == 1, "Fluid processor must emit exactly one authored tank.");
        Check(fluids[0].GetProperty("capacity").GetInt32() == 8_000, "Tank capacity must be preserved.");
    }
    using (var recipes = JsonDocument.Parse(File.ReadAllText(fluidRecipePath)))
    {
        var fluidInputs = recipes.RootElement.GetProperty("recipes")[0].GetProperty("fluid_inputs");
        Check(fluidInputs.GetArrayLength() == 1 && fluidInputs[0].GetProperty("fluid").GetString() == "minecraft:water",
            "Fluid recipe requirement must be preserved.");
        Check(fluidInputs[0].GetProperty("amount").GetInt32() == 250, "Fluid recipe amount must be preserved.");
    }

    Console.WriteLine("FOREVER_STUDIO_COMPILER_E2E_OK");
}
finally
{
    try { Directory.Delete(root, recursive: true); } catch { }
}
