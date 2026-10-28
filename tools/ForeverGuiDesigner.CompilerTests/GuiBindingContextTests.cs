using System.Runtime.CompilerServices;
using ForeverGuiDesigner.Services;

internal static class GuiBindingContextTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        var load = ForeverStudioLibraryCatalog.Load(null);
        Check(load.Warnings.Count == 0, "Built-in GUI binding catalog must load without warnings.");

        var document = ForeverStudioMachineProjectService.CreateBlank("gui_binding_context", "GUI Binding Context");
        document.Behavior.Nodes.Add(new ForeverStudioMachineBehaviorNode
        {
            Id = "iron_input",
            LibraryItemId = "function.item-input",
            LibraryItemVersion = 1,
            X = 100,
            Y = 200
        });
        document.Behavior.Nodes.Add(new ForeverStudioMachineBehaviorNode
        {
            Id = "energy",
            LibraryItemId = "function.energy-storage",
            LibraryItemVersion = 1,
            X = 300,
            Y = 200
        });
        document.Behavior.Nodes.Add(new ForeverStudioMachineBehaviorNode
        {
            Id = "progress",
            LibraryItemId = "function.process-progress",
            LibraryItemVersion = 1,
            X = 500,
            Y = 200
        });

        var context = MachineGuiBindingContext.Create(document, load.Catalog);
        Check(context.ContextId == "machine", "Machine GUI binding context must expose the machine context id.");

        var item = context.Find("iron_input", "item");
        Check(item is not null && item.DataType == "item" && item.Category == "Inputs",
            "Item Input must be exposed as a typed, human-facing input binding source.");

        var ratio = context.Find("energy", "ratio");
        Check(ratio is not null && ratio.DataType == "number" && ratio.Category == "Energy",
            "Energy fill ratio must be exposed as a numeric Energy binding source.");

        var progress = context.Find("progress", "progress");
        Check(progress is not null && progress.DataType == "number",
            "Process progress must be exposed as a numeric GUI binding source.");

        var binding = progress!.CreateBinding("progress_value", "progress_bar", "value");
        Check(binding.Context == "machine", "Bindings created by the Machine context must retain their context id.");
        Check(binding.SourceId == "progress" && binding.SourcePort == "progress",
            "Bindings must retain the typed source without exposing raw editing fields in the UI contract.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
