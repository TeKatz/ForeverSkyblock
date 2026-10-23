namespace ForeverGuiDesigner.Models;

public sealed class GuiLayoutModel
{
    public string Name { get; set; } = "Untitled GUI";
    public int Width { get; set; } = 176;
    public int Height { get; set; } = 166;
    public List<GuiElementModel> Elements { get; set; } = [];

    public static GuiLayoutModel CreateExample()
    {
        return new GuiLayoutModel
        {
            Name = "Condenser draft",
            Elements =
            [
                new GuiElementModel
                {
                    Id = "title",
                    Type = GuiElementType.Text,
                    X = 52,
                    Y = 8,
                    Width = 72,
                    Height = 10,
                    Content = "Condenser"
                },
                new GuiElementModel
                {
                    Id = "input",
                    Type = GuiElementType.Slot,
                    X = 39,
                    Y = 34,
                    Width = 18,
                    Height = 18
                },
                new GuiElementModel
                {
                    Id = "output",
                    Type = GuiElementType.Slot,
                    X = 119,
                    Y = 34,
                    Width = 18,
                    Height = 18
                },
                new GuiElementModel
                {
                    Id = "recipes",
                    Type = GuiElementType.Button,
                    X = 104,
                    Y = 61,
                    Width = 48,
                    Height = 16,
                    Content = "Recipes"
                }
            ]
        };
    }
}
