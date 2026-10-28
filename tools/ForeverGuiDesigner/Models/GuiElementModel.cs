using System.Text.Json;
using System.Text.Json.Serialization;

namespace ForeverGuiDesigner.Models;

public enum GuiElementType
{
    Slot,
    Text,
    Button,
    Texture,
    PlayerInventory,
    Hotbar
}

public sealed class GuiElementModel
{
    public string Id { get; set; } = "element";
    public GuiElementType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 18;
    public int Height { get; set; } = 18;
    public string Content { get; set; } = string.Empty;
    public string? TexturePath { get; set; }
    public bool Visible { get; set; } = true;

    // Phase 8 editor metadata. Defaults keep older layout JSON fully compatible.
    public bool Locked { get; set; }
    public string? Group { get; set; }
    public bool TextShadow { get; set; } = true;
    public double TextScale { get; set; } = 1.0;
    public string TextAlignment { get; set; } = "left";
    public string? ItemId { get; set; }
    public int StackCount { get; set; } = 1;
    public double Durability { get; set; } = -1;
    public bool GhostPreview { get; set; }
    public bool GlintPreview { get; set; }

    // Explicit editor overrides. Source/runtime presentation remains the default owner;
    // these values are only honored after a user invokes the matching editing command.
    public bool EditorSuppressed { get; set; }
    public string? EditorTextOverride { get; set; }
    public int? EditorZIndexOverride { get; set; }
    public bool EditorCustomTextureOverride { get; set; }

    // Preserve fields introduced by newer shared-contract/editor revisions. This makes a
    // legacy-layout open/save cycle non-destructive even before every new field has UI support.
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    // Runtime-only classification used to distinguish source-backed controls from
    // ordinary editor-created elements. Reconstructed after load/undo as needed.
    [JsonIgnore]
    public bool EditorSourceOwned { get; set; }

    public GuiElementModel Clone()
    {
        return new GuiElementModel
        {
            Id = Id,
            Type = Type,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Content = Content,
            TexturePath = TexturePath,
            Visible = Visible,
            Locked = Locked,
            Group = Group,
            TextShadow = TextShadow,
            TextScale = TextScale,
            TextAlignment = TextAlignment,
            ItemId = ItemId,
            StackCount = StackCount,
            Durability = Durability,
            GhostPreview = GhostPreview,
            GlintPreview = GlintPreview,
            EditorSuppressed = EditorSuppressed,
            EditorTextOverride = EditorTextOverride,
            EditorZIndexOverride = EditorZIndexOverride,
            EditorCustomTextureOverride = EditorCustomTextureOverride,
            ExtensionData = ExtensionData?.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Clone(),
                StringComparer.Ordinal),
            EditorSourceOwned = EditorSourceOwned
        };
    }

    public override string ToString()
    {
        var state = Locked ? " 🔒" : string.Empty;
        var group = string.IsNullOrWhiteSpace(Group) ? string.Empty : $" · {Group}";
        return $"{Id}  [{Type}]{group}{state}";
    }
}
