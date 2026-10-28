using System.Text.Json;
using ForeverGuiDesigner.Services;

static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);
static void Check(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
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

    var document = ForeverStudioMachineProjectService.CreateBlank("studio_compiler_e2e", "Studio Compiler E2E");
    var housing = catalog.Find("asset.housing", 1) ?? throw new InvalidOperationException("Missing built-in asset.housing.");
    document.Components.Add(ForeverStudioVisualAssemblyService.CreateComponent(housing, document.Components));

    var processor = new ForeverStudioMachineBehaviorNode
    {
        Id = "processor",
        LibraryItemId = "function.standard-processor",
        LibraryItemVersion = 1,
        X = 360,
        Y = 120
    };
    var input = new ForeverStudioMachineBehaviorNode
    {
        Id = "input",
        LibraryItemId = "function.item-input",
        LibraryItemVersion = 1,
        X = 140,
        Y = 230
    };
    var output = new ForeverStudioMachineBehaviorNode
    {
        Id = "output",
        LibraryItemId = "function.item-storage",
        LibraryItemVersion = 1,
        X = 580,
        Y = 230
    };
    document.Behavior.Nodes.AddRange([processor, input, output]);
    document.Behavior.Connections.Add(new ForeverStudioMachineBehaviorConnection
    {
        Id = "entry_processor",
        FromNodeId = "machine_entry",
        FromPortId = "flow",
        ToNodeId = processor.Id,
        ToPortId = "flow-in"
    });

    document.Recipes.Add(new ForeverStudioMachineRecipeBinding
    {
        Id = "cobble_to_gravel",
        RecipeTypeId = "forever_skyblock:machine_processing",
        Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
        {
            ["authoring"] = Json("compiler-e2e"),
            ["processorNodeId"] = Json(processor.Id),
            ["input"] = Json(new { item = "minecraft:cobblestone", count = 1 }),
            ["output"] = Json(new { item = "minecraft:gravel", count = 1 }),
            ["processingTimeTicks"] = Json(100),
            ["energy"] = Json(new { fePerTick = 0, totalFe = 0 })
        }
    });

    _ = ForeverStudioMachineProjectService.Serialize(document);
    var plan = ForeverStudioMachineCompilerService.BuildPlan(root, document, catalog);
    Check(plan.ProductionKind == ForeverStudioMachineCompilerService.ProductionKindRuntimeMachine,
        "Compiler must choose runtime-machine production for executable behavior.");
    Check(plan.CanApply,
        "Compiler plan must be applyable: " + string.Join(" | ", plan.Issues.Select(issue => issue.Message)));

    var result = ForeverStudioMachineCompilerService.Apply(plan);
    Check(result.WrittenPaths.Count > 0, "Compiler apply must write production output.");

    var runtimePath = Path.Combine(root, "mod", "src", "main", "resources", "data", "forever_skyblock", "forever_studio", "runtime", "studio_compiler_e2e.json");
    var recipePath = Path.Combine(root, "mod", "src", "main", "resources", "data", "forever_skyblock", "recipe", "forever_studio", "studio_compiler_e2e.json");
    var registryPath = Path.Combine(root, "mod", "src", "main", "java", "de", "tekatz", "foreverskyblock", "generated", "ForeverStudioGeneratedMachines.java");
    var manifestPath = Path.Combine(root, "mod", "src", "main", "resources", "data", "forever_skyblock", "forever_studio", "generated", "studio_compiler_e2e.json");

    Check(File.Exists(runtimePath), "Compiler must emit the runtime definition.");
    Check(File.Exists(recipePath), "Compiler must emit the runtime recipe book.");
    Check(File.Exists(registryPath), "Compiler must emit the generated NeoForge registry boundary.");
    Check(File.Exists(manifestPath), "Compiler must emit the generated build manifest.");

    using (var runtime = JsonDocument.Parse(File.ReadAllText(runtimePath)))
    {
        var json = runtime.RootElement;
        Check(json.GetProperty("schemaVersion").GetInt32() == 1, "Runtime definition schema must be 1.");
        Check(json.GetProperty("machine").GetString() == "forever_skyblock:studio_compiler_e2e", "Runtime machine id must match the authored project.");
        var slots = json.GetProperty("inventory");
        Check(slots.GetArrayLength() == 2, "Baseline compiler contract must emit one input and one output slot.");
        Check(slots[0].GetProperty("role").GetString() == "input" && slots[0].GetProperty("slot").GetInt32() == 0,
            "Baseline compiler input slot contract changed unexpectedly.");
        Check(slots[1].GetProperty("role").GetString() == "output" && slots[1].GetProperty("slot").GetInt32() == 1,
            "Baseline compiler output slot contract changed unexpectedly.");
    }

    using (var recipeBook = JsonDocument.Parse(File.ReadAllText(recipePath)))
    {
        var json = recipeBook.RootElement;
        Check(json.GetProperty("type").GetString() == "forever_skyblock:forever_studio_machine", "Runtime recipe serializer id must stay stable.");
        var recipe = json.GetProperty("recipes")[0];
        Check(recipe.GetProperty("input").GetProperty("item").GetString() == "minecraft:cobblestone", "Compiler must preserve the authored input item.");
        Check(recipe.GetProperty("result").GetProperty("id").GetString() == "minecraft:gravel", "Compiler must preserve the authored output item.");
        Check(recipe.GetProperty("processing_time").GetInt32() == 100, "Compiler must preserve processing duration.");
    }

    var registry = File.ReadAllText(registryPath);
    Check(registry.Contains("new ForeverStudioMachineBlock", StringComparison.Ordinal), "Generated registry must instantiate the generic Studio runtime block.");
    Check(registry.Contains("studio_compiler_e2e", StringComparison.Ordinal), "Generated registry must contain the compiled asset id.");

    using (var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath)))
    {
        Check(manifest.RootElement.GetProperty("productionKind").GetString() == "runtime-machine", "Manifest must record runtime-machine production.");
    }

    Console.WriteLine("FOREVER_STUDIO_COMPILER_E2E_OK");
}
finally
{
    try { Directory.Delete(root, recursive: true); } catch { }
}
