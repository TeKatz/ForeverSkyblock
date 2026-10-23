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
            GlintPreview = GlintPreview
        };
    }

    public override string ToString()
    {
        var state = Locked ? " 🔒" : string.Empty;
        var group = string.IsNullOrWhiteSpace(Group) ? string.Empty : $" · {Group}";
        return $"{Id}  [{Type}]{group}{state}";
    }
}
