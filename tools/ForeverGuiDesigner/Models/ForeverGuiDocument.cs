using System.Text.Json;
using System.Text.Json.Serialization;

namespace ForeverGuiDesigner.Models;

/// <summary>
/// Shared, context-neutral GUI authoring contract used by the standalone GUI editor and by
/// specialized authoring surfaces such as Machine Builder. The document intentionally separates
/// visual elements from assets, runtime bindings, styles, effects and states so those providers can
/// evolve without introducing another canvas format.
/// </summary>
public sealed class ForeverGuiDocument
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; set; } = CurrentFormatVersion;
    public string DocumentId { get; set; } = string.Empty;
    public string Name { get; set; } = "Untitled GUI";
    public ForeverGuiCanvas Canvas { get; set; } = new();
    public List<ForeverGuiElement> Elements { get; set; } = [];
    public List<ForeverGuiLayer> Layers { get; set; } = [];
    public List<ForeverGuiAssetReference> Assets { get; set; } = [];
    public List<ForeverGuiBinding> Bindings { get; set; } = [];
    public List<ForeverGuiStyle> Styles { get; set; } = [];
    public List<ForeverGuiEffect> Effects { get; set; } = [];
    public List<ForeverGuiState> States { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ForeverGuiCanvas
{
    public double Width { get; set; } = 176;
    public double Height { get; set; } = 166;
    public double Snap { get; set; } = 1;
    public string? BackgroundAssetId { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ForeverGuiElement
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = "texture";
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 18;
    public double Height { get; set; } = 18;
    public double Order { get; set; }
    public bool Visible { get; set; } = true;
    public bool Locked { get; set; }
    public string? LayerId { get; set; }
    public string? Group { get; set; }
    public string? AssetId { get; set; }
    public string? BindingId { get; set; }
    public string? StyleId { get; set; }
    public List<string> EffectIds { get; set; } = [];
    public List<string> StateIds { get; set; } = [];

    // Common presentation fields already supported by the existing editor. Keeping them in the
    // shared document avoids hiding established editor behavior in provider-specific dictionaries.
    public string Text { get; set; } = string.Empty;
    public string? TexturePath { get; set; }
    public bool TextShadow { get; set; } = true;
    public double TextScale { get; set; } = 1.0;
    public string TextAlignment { get; set; } = "left";
    public string? ItemId { get; set; }
    public int StackCount { get; set; } = 1;
    public double Durability { get; set; } = -1;
    public bool GhostPreview { get; set; }
    public bool GlintPreview { get; set; }
    public bool EditorSuppressed { get; set; }
    public string? EditorTextOverride { get; set; }
    public int? EditorZIndexOverride { get; set; }
    public bool EditorCustomTextureOverride { get; set; }

    // Provider-owned element metadata that is not yet promoted to a stable shared field.
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    [JsonIgnore]
    public bool EditorSourceOwned { get; set; }
}

public sealed class ForeverGuiLayer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public double Order { get; set; }
    public bool Visible { get; set; } = true;
    public bool Locked { get; set; }
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ForeverGuiAssetReference
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ForeverGuiBinding
{
    public string Id { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string SourcePortId { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string LibraryItemId { get; set; } = string.Empty;
    public int LibraryItemVersion { get; set; } = 1;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ForeverGuiStyle
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ForeverGuiEffect
{
    public string Id { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ForeverGuiState
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsInitial { get; set; }
    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
