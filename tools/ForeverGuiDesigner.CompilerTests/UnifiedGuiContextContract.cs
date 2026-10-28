using System.Runtime.CompilerServices;
using System.Text.Json;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

internal static class UnifiedGuiContextContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        LegacyContextSession();
        MachineContextSession();
    }

    private static void LegacyContextSession()
    {
        var backing = new GuiLayoutModel
        {
            Name = "Standalone",
            Width = 176,
            Height = 166,
            Elements =
            [
                new GuiElementModel
                {
                    Id = "title",
                    Type = GuiElementType.Text,
                    X = 8,
                    Y = 8,
                    Width = 60,
                    Height = 10,
                    Content = "Before"
                }
            ]
        };

        var provider = new ForeverLegacyGuiContextProvider(
            "standalone:test",
            "Standalone test",
            () => backing,
            layout => backing = layout);
        var session = new ForeverGuiEditorSession(provider);
        Check(session.Context.ContextId == "standalone:test", "Standalone context id must be stable.");
        session.Document.Elements.Single().Text = "After";
        session.MarkDirty();
        Check(session.IsDirty, "Session must track dirty state independently from the host model.");
        session.Commit();
        Check(!session.IsDirty && backing.Elements.Single().Content == "After",
            "Standalone context commit must project shared edits back to GuiLayoutModel.");

        backing.Elements.Single().Content = "Reloaded";
        session.Reload();
        Check(session.Document.Elements.Single().Text == "Reloaded" && !session.IsDirty,
            "Standalone context reload must replace the shared document from the provider.");
    }

    private static void MachineContextSession()
    {
        var machine = ForeverStudioMachineProjectService.CreateBlank("context_machine", "Context Machine");
        machine.Gui.ScreenId = "context_machine_screen";
        machine.Gui.Bindings.Add(new ForeverStudioMachineGuiBinding
        {
            Id = "progress",
            LibraryItemId = "asset.screen",
            LibraryItemVersion = 1,
            SourceId = "machine_entry",
            Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
            {
                ["widget"] = JsonSerializer.SerializeToElement("progress-bar"),
                ["sourcePortId"] = JsonSerializer.SerializeToElement("progress"),
                ["sourceType"] = JsonSerializer.SerializeToElement("number"),
                ["label"] = JsonSerializer.SerializeToElement("Progress"),
                ["x"] = JsonSerializer.SerializeToElement(20),
                ["y"] = JsonSerializer.SerializeToElement(20),
                ["width"] = JsonSerializer.SerializeToElement(40),
                ["height"] = JsonSerializer.SerializeToElement(8),
                ["order"] = JsonSerializer.SerializeToElement(0)
            }
        });

        var provider = new ForeverMachineGuiContextProvider(machine);
        var session = new ForeverGuiEditorSession(provider);
        var element = session.Document.Elements.Single();
        element.X = 28;
        element.StyleId = "progress_style";
        session.Document.Styles.Add(new ForeverGuiStyle { Id = "progress_style", Name = "Progress" });
        session.MarkDirty();
        session.Commit();

        Check(machine.Gui.Bindings.Single().Properties["x"].GetDouble() == 28,
            "Machine context commit must update the compact runtime GUI projection.");
        Check(machine.ExtensionData?.ContainsKey(ForeverMachineGuiDocumentBridge.ExtensionKey) == true,
            "Machine context commit must persist the full shared GUI document.");

        var reopened = new ForeverGuiEditorSession(new ForeverMachineGuiContextProvider(machine));
        Check(reopened.Document.Elements.Single().StyleId == "progress_style" && reopened.Document.Styles.Count == 1,
            "Machine context reload must restore shared-only style metadata from the bridge.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
