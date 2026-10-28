namespace ForeverGuiDesigner.Models;

/// <summary>
/// Backwards-compatible name for the standalone editor document. The canonical contract now lives
/// in GuiDocument; keeping this derived type avoids breaking older editor code and layout JSON.
/// </summary>
public sealed class GuiLayoutModel : GuiDocument
{
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
                    X = 55,
                    Y = 7,
                    Width = 66,
                    Height = 10,
                    Content = "Condenser"
                },
                new GuiElementModel
                {
                    Id = "input",
                    Type = GuiElementType.Slot,
                    X = 39,
                    Y = 30,
                    Width = 18,
                    Height = 18
                },
                new GuiElementModel
                {
                    Id = "output",
                    Type = GuiElementType.Slot,
                    X = 119,
                    Y = 30,
                    Width = 18,
                    Height = 18
                },
                new GuiElementModel
                {
                    Id = "recipes",
                    Type = GuiElementType.Button,
                    X = 104,
                    Y = 55,
                    Width = 48,
                    Height = 16,
                    Content = "Recipes"
                },
                new GuiElementModel
                {
                    Id = "inventory",
                    Type = GuiElementType.PlayerInventory,
                    X = 7,
                    Y = 84,
                    Width = 162,
                    Height = 54
                },
                new GuiElementModel
                {
                    Id = "hotbar",
                    Type = GuiElementType.Hotbar,
                    X = 7,
                    Y = 142,
                    Width = 162,
                    Height = 18
                }
            ]
        };
    }
}
