using System.Text.Json;
using System.Text.Json.Serialization;

namespace ForeverGuiDesigner.Models;

/// <summary>
/// Canonical Forever Studio GUI document shared by standalone GUI editing and contextual editors
/// such as Machine Builder. The existing GuiLayoutModel derives from this contract so legacy layout
/// JSON remains directly editable while newer document sections can round-trip without a conversion.
/// </summary>
public class GuiDocument
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; set; } = CurrentFormatVersion;
    public string Name { get; set; } = "Untitled GUI";
    public int Width { get; set; } = 176;
    public int Height { get; set; } = 166;
    public GuiCanvasSettings Canvas { get; set; } = new();
    public List<GuiElementModel> Elements { get; set; } = [];
    public List<GuiLayerModel> Layers { get; set; } = [];
    public List<GuiAssetReferenceModel> Assets { get; set; } = [];
    public List<GuiBindingModel> Bindings { get; set; } = [];
    public List<GuiStyleModel> Styles { get; set; } = [];
    public List<GuiEffectModel> Effects { get; set; } = [];
    public List<GuiVisualStateModel> States { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class GuiCanvasSettings
{
    public int Snap { get; set; } = 1;
    public bool GridVisible { get; set; } = true;
    public string? BackgroundAssetId { get; set; }
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class GuiLayerModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool Visible { get; set; } = true;
    public bool Locked { get; set; }
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class GuiAssetReferenceModel
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = "texture";
    public string Resource { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class GuiBindingModel
{
    public string Id { get; set; } = string.Empty;
    public string ElementId { get; set; } = string.Empty;
    public string TargetProperty { get; set; } = "value";
    public string Context { get; set; } = "standalone";
    public string SourceId { get; set; } = string.Empty;
    public string SourcePort { get; set; } = string.Empty;
    public string DataType { get; set; } = "any";
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class GuiStyleModel
{
    public string Id { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class GuiEffectModel
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<GuiBindingModel> Bindings { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class GuiVisualStateModel
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public static class GuiDocumentContract
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize(GuiDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Normalize(document);
        return JsonSerializer.Serialize(document, Options);
    }

    public static GuiDocument Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<GuiDocument>(json, Options)
                       ?? throw new InvalidDataException("The GUI document was empty.");
        Normalize(document);
        return document;
    }

    public static GuiDocument Clone(GuiDocument document)
        => Deserialize(Serialize(document));

    public static void Normalize(GuiDocument document)
    {
        if (document.FormatVersion <= 0)
            document.FormatVersion = CurrentFormatVersion;
        if (document.FormatVersion > CurrentFormatVersion)
            throw new InvalidDataException($"Unsupported GUI document formatVersion {document.FormatVersion}; this Studio supports {CurrentFormatVersion}.");

        document.Name ??= "Untitled GUI";
        document.Width = Math.Max(1, document.Width);
        document.Height = Math.Max(1, document.Height);
        document.Canvas ??= new GuiCanvasSettings();
        document.Canvas.Snap = Math.Max(1, document.Canvas.Snap);
        document.Canvas.Properties ??= new(StringComparer.OrdinalIgnoreCase);
        document.Elements ??= [];
        document.Layers ??= [];
        document.Assets ??= [];
        document.Bindings ??= [];
        document.Styles ??= [];
        document.Effects ??= [];
        document.States ??= [];

        foreach (var element in document.Elements)
        {
            element.EffectIds ??= [];
            element.Properties ??= new(StringComparer.OrdinalIgnoreCase);
        }
        foreach (var layer in document.Layers)
            layer.Properties ??= new(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in document.Assets)
            asset.Properties ??= new(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in document.Bindings)
            binding.Properties ??= new(StringComparer.OrdinalIgnoreCase);
        foreach (var style in document.Styles)
            style.Properties ??= new(StringComparer.OrdinalIgnoreCase);
        foreach (var effect in document.Effects)
        {
            effect.Properties ??= new(StringComparer.OrdinalIgnoreCase);
            effect.Bindings ??= [];
            foreach (var binding in effect.Bindings)
                binding.Properties ??= new(StringComparer.OrdinalIgnoreCase);
        }
        foreach (var state in document.States)
            state.Properties ??= new(StringComparer.OrdinalIgnoreCase);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
