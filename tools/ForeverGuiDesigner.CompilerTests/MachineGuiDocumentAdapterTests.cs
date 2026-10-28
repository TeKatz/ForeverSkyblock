using System.Runtime.CompilerServices;
using System.Text.Json;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

internal static class MachineGuiDocumentAdapterTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        var machine = ForeverStudioMachineProjectService.CreateBlank("machine_gui_adapter", "Machine GUI Adapter");
        machine.Gui.ScreenId = "machine_gui_adapter_screen";
        machine.Gui.Bindings.Add(new ForeverStudioMachineGuiBinding
        {
            Id = "progress_widget",
            LibraryItemId = "asset.screen",
            LibraryItemVersion = 1,
            SourceId = "processor",
            Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
            {
                ["widget"] = JsonSerializer.SerializeToElement("progress-bar"),
                ["sourcePortId"] = JsonSerializer.SerializeToElement("progress"),
                ["sourceType"] = JsonSerializer.SerializeToElement("number"),
                ["label"] = JsonSerializer.SerializeToElement("Progress"),
                ["x"] = JsonSerializer.SerializeToElement(30),
                ["y"] = JsonSerializer.SerializeToElement(40),
                ["width"] = JsonSerializer.SerializeToElement(50),
                ["height"] = JsonSerializer.SerializeToElement(8),
                ["previewValue"] = JsonSerializer.SerializeToElement("0.5"),
                ["order"] = JsonSerializer.SerializeToElement(0),
                ["futureMachineProperty"] = JsonSerializer.SerializeToElement(new { keep = true })
            }
        });

        var document = ForeverStudioMachineGuiDocumentAdapter.LoadAuthoringDocument(machine);
        Check(document.Elements.Count == 1 && document.Bindings.Count == 1,
            "Legacy Machine GUI binding must project into one shared element and one shared binding.");
        Check(document.Bindings[0].Context == "machine" && document.Bindings[0].SourcePort == "progress",
            "Machine source semantics must move into the shared binding contract.");
        Check(document.Elements[0].Properties.ContainsKey("futureMachineProperty"),
            "Unknown legacy Machine GUI properties must survive capture.");

        document.Elements[0].X = 72;
        document.Elements[0].EffectIds.Add("hover_glow");
        document.Effects.Add(new GuiEffectModel
        {
            Id = "hover_glow",
            Kind = "glow",
            Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
            {
                ["strength"] = JsonSerializer.SerializeToElement(0.8)
            }
        });

        ForeverStudioMachineGuiDocumentAdapter.PersistAuthoringDocument(machine, document);
        Check(machine.ExtensionData?.ContainsKey(ForeverStudioMachineGuiDocumentAdapter.ExtensionKey) == true,
            "Machine Project must persist the shared GUI authoring document.");

        var projected = machine.Gui.Bindings.Single();
        Check(projected.Properties["x"].GetInt32() == 72,
            "Shared GUI geometry changes must project back to the legacy runtime binding.");
        Check(projected.Properties.TryGetValue("futureMachineProperty", out var future) &&
              future.GetProperty("keep").GetBoolean(),
            "Unknown Machine GUI binding properties must survive shared-document projection.");
        Check(projected.SourceId == "processor" &&
              projected.Properties["sourcePortId"].GetString() == "progress",
            "Machine binding identity must remain stable through the adapter.");

        // Simulate a complete Machine Project JSON round trip. The authoring document, including an
        // effect the legacy compiler projection does not understand, must remain authoritative.
        var machineJson = ForeverStudioMachineProjectService.Serialize(machine);
        using var machineTree = JsonDocument.Parse(machineJson);
        var restoredMachine = JsonSerializer.Deserialize<ForeverStudioMachineDocument>(machineTree.RootElement.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Machine Project round trip failed.");
        var restored = ForeverStudioMachineGuiDocumentAdapter.LoadAuthoringDocument(restoredMachine);
        Check(restored.Elements[0].X == 72, "Persisted shared GUI geometry must survive Machine Project JSON.");
        Check(restored.Effects.Single().Id == "hover_glow" && restored.Elements[0].EffectIds.Contains("hover_glow"),
            "Shared effects must survive Machine Project round trip even before the legacy runtime projection supports them.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
