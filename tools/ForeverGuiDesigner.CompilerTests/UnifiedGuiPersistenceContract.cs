using System.Runtime.CompilerServices;
using System.Text.Json;
using ForeverGuiDesigner.Services;

internal static class UnifiedGuiPersistenceContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var machine = ForeverStudioMachineProjectService.CreateBlank("gui_persistence_contract", "GUI Persistence Contract");
        machine.Gui.ScreenId = "gui_persistence_contract_screen";
        machine.Gui.Bindings.Add(new ForeverStudioMachineGuiBinding
        {
            Id = "status_widget",
            LibraryItemId = "asset.status-lamp",
            LibraryItemVersion = 1,
            SourceId = "machine_entry",
            Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
            {
                ["widget"] = JsonSerializer.SerializeToElement("status"),
                ["sourcePortId"] = JsonSerializer.SerializeToElement("flow"),
                ["sourceType"] = JsonSerializer.SerializeToElement("boolean"),
                ["label"] = JsonSerializer.SerializeToElement("Status"),
                ["x"] = JsonSerializer.SerializeToElement(12),
                ["y"] = JsonSerializer.SerializeToElement(12),
                ["width"] = JsonSerializer.SerializeToElement(12),
                ["height"] = JsonSerializer.SerializeToElement(12),
                ["order"] = JsonSerializer.SerializeToElement(0)
            }
        });

        var shared = ForeverMachineGuiDocumentBridge.FromMachineDocument(machine);
        shared.ExtensionData = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["futureSharedRoot"] = JsonSerializer.SerializeToElement(new { keep = true })
        };
        ForeverMachineGuiDocumentBridge.ApplyToMachineDocument(machine, shared);

        var json = ForeverStudioMachineProjectService.Serialize(machine);
        using var parsed = JsonDocument.Parse(json);
        var root = parsed.RootElement;
        Check(root.TryGetProperty(ForeverMachineGuiDocumentBridge.ExtensionKey, out var persisted) &&
              persisted.ValueKind == JsonValueKind.Object,
            "Serialized Machine Project must contain the complete shared GUI document extension payload.");
        Check(persisted.TryGetProperty("extensionData", out _) == false,
            "JsonExtensionData must be flattened into the shared GUI object rather than serialized as an extensionData wrapper.");
        Check(persisted.TryGetProperty("futureSharedRoot", out var future) &&
              future.GetProperty("keep").GetBoolean(),
            "Unknown shared GUI root fields must survive Machine Project serialization.");
        Check(root.GetProperty("gui").GetProperty("bindings").GetArrayLength() == 1,
            "Machine Project must keep the compact runtime GUI binding projection beside the shared document.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
