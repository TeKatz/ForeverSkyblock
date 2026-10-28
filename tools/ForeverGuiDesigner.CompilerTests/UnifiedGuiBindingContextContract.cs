using System.Runtime.CompilerServices;
using ForeverGuiDesigner.Services;

internal static class UnifiedGuiBindingContextContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "forever studio gui binding context " + Guid.NewGuid().ToString("N"));
        try
        {
            var texturePath = Path.Combine(root, "mod", "src", "main", "resources", "assets", "forever_skyblock", "textures", "block", "ascension_materials.png");
            Directory.CreateDirectory(Path.GetDirectoryName(texturePath)!);
            File.WriteAllBytes(texturePath, [0]);

            var load = ForeverStudioLibraryCatalog.Load(root);
            Check(load.Warnings.Count == 0, "Built-in Studio library must load for GUI binding context: " + string.Join(" | ", load.Warnings));

            var machine = ForeverStudioMachineProjectService.CreateBlank("binding_context", "Binding Context");
            machine.Behavior.Nodes.AddRange(
            [
                Node("input", "function.item-input", 20, 20),
                Node("output", "function.item-storage", 40, 20),
                Node("processor", "function.powered-processor", 60, 20),
                Node("energy", "function.energy-storage", 80, 20),
                Node("tank", "function.fluid-tank", 100, 20)
            ]);

            var context = ForeverMachineGuiBindingContext.Build(machine, load.Catalog);
            Check(context.Sources.Count > 0, "Machine binding context must expose non-flow Function outputs.");
            Check(context.Sources.Any(source => source.NodeId == "input" && source.Category == "Inputs" &&
                                               source.SuggestedWidgetKinds.Contains("item-slot", StringComparer.OrdinalIgnoreCase)),
                "Item input outputs must be presented as friendly Inputs and suggest an item slot.");
            Check(context.Sources.Any(source => source.NodeId == "output" && source.Category is "Outputs" or "Storage" &&
                                               source.SuggestedWidgetKinds.Contains("output-slot", StringComparer.OrdinalIgnoreCase)),
                "Item storage/output data must suggest an output slot.");
            Check(context.Sources.Any(source => source.NodeId == "processor" && source.Category == "Processor" &&
                                               source.SuggestedWidgetKinds.Contains("progress-bar", StringComparer.OrdinalIgnoreCase)),
                "Processor progress must be exposed as a human-facing Processor source.");
            Check(context.Sources.Any(source => source.NodeId == "energy" && source.Category == "Energy" &&
                                               source.SuggestedWidgetKinds.Contains("energy-bar", StringComparer.OrdinalIgnoreCase)),
                "Energy outputs must suggest an energy bar.");
            Check(context.Sources.Any(source => source.NodeId == "tank" && source.Category == "Fluids" &&
                                               source.SuggestedWidgetKinds.Contains("fluid-gauge", StringComparer.OrdinalIgnoreCase)),
                "Fluid outputs must suggest a fluid gauge.");

            var first = context.Sources.First();
            Check(context.Find(first.NodeId, first.PortId)?.Key == first.Key,
                "Binding context must resolve persisted raw node/port ids back to the friendly source.");
            Check(!first.ToString().Contains("::", StringComparison.Ordinal),
                "Human-facing binding labels must not expose the internal compound key.");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static ForeverStudioMachineBehaviorNode Node(string id, string libraryItemId, double x, double y)
        => new()
        {
            Id = id,
            LibraryItemId = libraryItemId,
            LibraryItemVersion = 1,
            X = x,
            Y = y
        };

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
