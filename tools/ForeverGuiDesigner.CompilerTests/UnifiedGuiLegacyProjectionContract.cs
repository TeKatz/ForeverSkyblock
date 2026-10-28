using System.Runtime.CompilerServices;
using System.Text.Json;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

internal static class UnifiedGuiLegacyProjectionContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var shared = new ForeverGuiDocument
        {
            DocumentId = "projection_screen",
            Name = "Projection",
            Canvas = new ForeverGuiCanvas { Width = 176, Height = 166 },
            Bindings =
            [
                Binding("input", "input_node", "item", "item"),
                Binding("progress", "processor", "progress", "number"),
                Binding("fluid", "tank", "ratio", "number"),
                Binding("future", "processor", "future", "any")
            ],
            Elements =
            [
                Element("input", "item-slot", 10, 20, 18, 18),
                Element("progress", "progress-bar", 40.5, 20, 50, 8),
                Element("fluid", "fluid-gauge", 100, 20.5, 12, 50.5),
                Element("future", "provider-future-widget", 120, 20, 20, 20)
            ]
        };

        var projected = ForeverGuiLegacyEditorProjection.Create(shared);
        Check(projected.Elements.Single(element => element.Id == "input").Kind == nameof(GuiElementType.Slot),
            "Item-slot must project to the established Slot editor primitive.");
        Check(projected.Elements.Single(element => element.Id == "progress").Kind == nameof(GuiElementType.Texture),
            "Progress bar must project to an editable Texture primitive.");
        Check(projected.Elements.Single(element => element.Id == "future").Properties.TryGetValue(
                  ForeverGuiLegacyEditorProjection.OriginalKindProperty, out var futureKind) &&
              futureKind.GetString() == "provider-future-widget",
            "Future provider widget kinds must be preserved before legacy projection.");
        Check(projected.Elements.Single(element => element.Id == "progress").X == 41 &&
              projected.Elements.Single(element => element.Id == "progress").Properties.ContainsKey(ForeverGuiLegacyEditorProjection.OriginalXProperty),
            "Fractional Machine geometry must be projected to integer editor coordinates with an exact-value marker.");

        var legacy = ForeverGuiDocumentService.ToLegacyLayout(projected);
        // Leave progress X untouched so its original 40.5 value must be restored.
        legacy.Elements.Single(element => element.Id == "fluid").Y = 23;
        legacy.Elements.Single(element => element.Id == "future").Width = 24;
        var editedProjection = ForeverGuiDocumentService.FromLegacyLayout(legacy);
        var restored = ForeverGuiLegacyEditorProjection.Restore(editedProjection);

        Check(restored.Elements.Single(element => element.Id == "input").Kind == "item-slot",
            "Item-slot semantic kind must be restored after legacy editing.");
        Check(restored.Elements.Single(element => element.Id == "progress").Kind == "progress-bar" &&
              Math.Abs(restored.Elements.Single(element => element.Id == "progress").X - 40.5) < 0.0001,
            "Untouched fractional Machine geometry must be restored exactly after legacy editing.");
        Check(restored.Elements.Single(element => element.Id == "fluid").Kind == "fluid-gauge" &&
              restored.Elements.Single(element => element.Id == "fluid").Y == 23 &&
              Math.Abs(restored.Elements.Single(element => element.Id == "fluid").Height - 50.5) < 0.0001,
            "Edited projected coordinates must win while untouched fractional dimensions remain exact.");
        Check(restored.Elements.Single(element => element.Id == "future").Kind == "provider-future-widget" &&
              restored.Elements.Single(element => element.Id == "future").Width == 24,
            "Unknown future provider kinds must survive projection while accepting visual edits.");
        Check(restored.Elements.All(element =>
                !element.Properties.ContainsKey(ForeverGuiLegacyEditorProjection.OriginalKindProperty) &&
                !element.Properties.ContainsKey(ForeverGuiLegacyEditorProjection.OriginalXProperty) &&
                !element.Properties.ContainsKey(ForeverGuiLegacyEditorProjection.OriginalYProperty) &&
                !element.Properties.ContainsKey(ForeverGuiLegacyEditorProjection.OriginalWidthProperty) &&
                !element.Properties.ContainsKey(ForeverGuiLegacyEditorProjection.OriginalHeightProperty)),
            "Projection-only semantic and geometry markers must be removed before context commit.");
    }

    private static ForeverGuiBinding Binding(string id, string source, string port, string type)
        => new()
        {
            Id = id,
            Provider = "machine",
            SourceId = source,
            SourcePortId = port,
            SourceType = type,
            LibraryItemId = "asset.screen",
            LibraryItemVersion = 1
        };

    private static ForeverGuiElement Element(string id, string kind, double x, double y, double width, double height)
        => new()
        {
            Id = id,
            Kind = kind,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            BindingId = id,
            AssetId = "asset.screen"
        };

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
