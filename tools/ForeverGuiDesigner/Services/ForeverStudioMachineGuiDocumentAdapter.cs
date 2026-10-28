using System.Globalization;
using System.Text.Json;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Transitional bridge between the historical Machine Builder binding list and the shared GUI
/// document. The shared document is the authoring shape; legacy bindings remain a compiler-facing
/// projection until the runtime compiler consumes GuiDocument directly.
/// </summary>
public static class ForeverStudioMachineGuiDocumentAdapter
{
    private const string MainLayerId = "machine-main";

    public static GuiDocument Capture(ForeverStudioMachineDocument machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        var document = new GuiDocument
        {
            Name = string.IsNullOrWhiteSpace(machine.DisplayName) ? machine.AssetId : machine.DisplayName,
            Width = 176,
            Height = 166,
            Layers =
            [
                new GuiLayerModel
                {
                    Id = MainLayerId,
                    Name = "Machine GUI",
                    Order = 0
                }
            ]
        };

        foreach (var source in machine.Gui.Bindings
                     .OrderBy(binding => ReadNumber(binding.Properties, "order", 0))
                     .ThenBy(binding => binding.Id, StringComparer.OrdinalIgnoreCase))
        {
            var widget = ReadString(source.Properties, "widget", "value");
            var element = new GuiElementModel
            {
                Id = source.Id,
                Type = LegacyElementType(widget),
                X = Round(ReadNumber(source.Properties, "x", 0)),
                Y = Round(ReadNumber(source.Properties, "y", 0)),
                Width = Math.Max(1, Round(ReadNumber(source.Properties, "width", 18))),
                Height = Math.Max(1, Round(ReadNumber(source.Properties, "height", 18))),
                Content = ReadString(source.Properties, "label", widget),
                LayerId = MainLayerId,
                AssetId = source.LibraryItemId,
                Properties = CloneProperties(source.Properties)
            };
            element.Properties["machineWidget"] = JsonSerializer.SerializeToElement(widget);
            element.Properties["legacyLibraryItemVersion"] = JsonSerializer.SerializeToElement(source.LibraryItemVersion);
            document.Elements.Add(element);

            document.Bindings.Add(new GuiBindingModel
            {
                Id = source.Id + ".value",
                ElementId = source.Id,
                TargetProperty = "value",
                Context = "machine",
                SourceId = source.SourceId,
                SourcePort = ReadString(source.Properties, "sourcePortId", string.Empty),
                DataType = ReadString(source.Properties, "sourceType", "any"),
                Properties = CloneProperties(source.Properties)
            });
        }

        GuiDocumentContract.Normalize(document);
        return document;
    }

    public static void Apply(ForeverStudioMachineDocument machine, GuiDocument document)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(document);
        GuiDocumentContract.Normalize(document);

        if (string.IsNullOrWhiteSpace(machine.Gui.ScreenId))
            machine.Gui.ScreenId = $"{machine.AssetId}_screen";

        var elements = document.Elements.ToDictionary(element => element.Id, StringComparer.OrdinalIgnoreCase);
        var bindings = document.Bindings
            .Where(binding => binding.Context.Equals("machine", StringComparison.OrdinalIgnoreCase))
            .OrderBy(binding => ElementOrder(document, binding.ElementId))
            .ThenBy(binding => binding.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var projected = new List<ForeverStudioMachineGuiBinding>();
        foreach (var binding in bindings)
        {
            if (!elements.TryGetValue(binding.ElementId, out var element))
                continue;

            var properties = CloneProperties(binding.Properties);
            foreach (var pair in element.Properties)
                properties[pair.Key] = pair.Value.Clone();

            var widget = ReadString(element.Properties, "machineWidget",
                ReadString(properties, "widget", WidgetForElement(element.Type)));
            properties["widget"] = JsonSerializer.SerializeToElement(widget);
            properties["sourcePortId"] = JsonSerializer.SerializeToElement(binding.SourcePort);
            properties["sourceType"] = JsonSerializer.SerializeToElement(binding.DataType);
            properties["label"] = JsonSerializer.SerializeToElement(element.Content ?? string.Empty);
            properties["x"] = JsonSerializer.SerializeToElement(element.X);
            properties["y"] = JsonSerializer.SerializeToElement(element.Y);
            properties["width"] = JsonSerializer.SerializeToElement(element.Width);
            properties["height"] = JsonSerializer.SerializeToElement(element.Height);
            properties["order"] = JsonSerializer.SerializeToElement(ElementOrder(document, element.Id));

            var libraryItemId = !string.IsNullOrWhiteSpace(element.AssetId)
                ? element.AssetId!
                : DefaultAssetForWidget(widget);
            var libraryItemVersion = ReadInt(element.Properties, "legacyLibraryItemVersion", 1);

            projected.Add(new ForeverStudioMachineGuiBinding
            {
                Id = element.Id,
                LibraryItemId = libraryItemId,
                LibraryItemVersion = Math.Max(1, libraryItemVersion),
                SourceId = binding.SourceId,
                Properties = properties
            });
        }

        machine.Gui.Bindings = projected;
    }

    private static int ElementOrder(GuiDocument document, string elementId)
    {
        for (var index = 0; index < document.Elements.Count; index++)
            if (document.Elements[index].Id.Equals(elementId, StringComparison.OrdinalIgnoreCase))
                return index;
        return int.MaxValue;
    }

    private static GuiElementType LegacyElementType(string widget)
        => widget switch
        {
            "item-slot" or "output-slot" => GuiElementType.Slot,
            "value" => GuiElementType.Text,
            _ => GuiElementType.Texture
        };

    private static string WidgetForElement(GuiElementType type)
        => type switch
        {
            GuiElementType.Slot => "item-slot",
            GuiElementType.Text => "value",
            _ => "value"
        };

    private static string DefaultAssetForWidget(string widget)
        => widget switch
        {
            "item-slot" => "asset.input-tray",
            "output-slot" => "asset.output-tray",
            "fluid-gauge" => "asset.fluid-window",
            "status" => "asset.status-lamp",
            _ => "asset.screen"
        };

    private static Dictionary<string, JsonElement> CloneProperties(Dictionary<string, JsonElement>? properties)
        => properties?.ToDictionary(
               pair => pair.Key,
               pair => pair.Value.Clone(),
               StringComparer.OrdinalIgnoreCase)
           ?? new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

    private static string ReadString(Dictionary<string, JsonElement> properties, string key, string fallback)
        => properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static double ReadNumber(Dictionary<string, JsonElement> properties, string key, double fallback)
        => properties.TryGetValue(key, out var value) && value.TryGetDouble(out var result) && double.IsFinite(result)
            ? result
            : fallback;

    private static int ReadInt(Dictionary<string, JsonElement> properties, string key, int fallback)
        => properties.TryGetValue(key, out var value) && value.TryGetInt32(out var result) ? result : fallback;

    private static int Round(double value)
        => (int)Math.Round(value, MidpointRounding.AwayFromZero);
}
