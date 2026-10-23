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
            Visible = Visible
        };
    }

    public override string ToString() => $"{Id}  [{Type}]";
}
