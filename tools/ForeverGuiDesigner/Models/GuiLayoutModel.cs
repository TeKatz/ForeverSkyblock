using System.Text.Json;
using System.Text.Json.Serialization;

namespace ForeverGuiDesigner.Models;

public sealed class GuiLayoutModel
{
    public string Name { get; set; } = "Untitled GUI";
    public int Width { get; set; } = 176;
    public int Height { get; set; } = 166;
    public List<GuiElementModel> Elements { get; set; } = [];

    // Preserve forward-compatible fields when older editor surfaces open and resave a layout.
    // The unified GUI contract adapter carries these fields through unchanged.
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

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
