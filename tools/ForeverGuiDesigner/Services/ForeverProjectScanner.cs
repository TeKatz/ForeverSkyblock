using System.IO;
using System.Text.RegularExpressions;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner.Services;

public sealed class ForeverMachineTemplate
{
    public string DisplayName { get; init; } = string.Empty;
    public string ScreenClass { get; init; } = string.Empty;
    public string MenuClass { get; init; } = string.Empty;
    public string LayoutKey { get; init; } = string.Empty;
    public string ScreenPath { get; init; } = string.Empty;
    public string? MenuPath { get; init; }
    public string? PreviewImagePath { get; init; }
    public GuiLayoutModel Layout { get; init; } = new();
    public int MachineSlotCount { get; init; }
    public int ButtonCount { get; init; }

    public bool HasVisualPreview => !string.IsNullOrWhiteSpace(PreviewImagePath) && File.Exists(PreviewImagePath);
    public string Summary => $"{Layout.Width}×{Layout.Height} · {MachineSlotCount} machine slots · {ButtonCount} buttons · {(HasVisualPreview ? "visual preview" : "layout only")}";
}

public sealed class ForeverScanResult
{
    public List<ForeverMachineTemplate> Templates { get; } = [];
    public List<string> Warnings { get; } = [];
}

public static class ForeverProjectScanner
{
    private sealed record MachineLayoutSpec(int Width, int Height, int InventoryY, int HotbarY);

    public static ForeverScanResult Scan(string repoRoot)
    {
        var result = new ForeverScanResult();
        var javaRoot = Path.Combine(repoRoot, "mod", "src", "main", "java");
        if (!Directory.Exists(javaRoot))
        {
            result.Warnings.Add("mod/src/main/java was not found.");
            return result;
        }

        var layoutPath = Directory.EnumerateFiles(javaRoot, "MachineLayout.java", SearchOption.AllDirectories).FirstOrDefault();
        if (layoutPath is null)
        {
            result.Warnings.Add("MachineLayout.java was not found.");
            return result;
        }

        var layouts = ParseLayouts(File.ReadAllText(layoutPath));
        if (layouts.Count == 0)
        {
            result.Warnings.Add("No MachineLayout constants could be parsed.");
            return result;
        }

        var menuFiles = Directory.EnumerateFiles(javaRoot, "*Menu.java", SearchOption.AllDirectories)
            .ToDictionary(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase);

        foreach (var screenPath in Directory.EnumerateFiles(javaRoot, "*Screen.java", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(screenPath);
            var screenClass = Path.GetFileNameWithoutExtension(screenPath);
            var menuMatch = Regex.Match(source, @"extends\s+MachineScreen<(?<menu>[A-Za-z0-9_]+)>");
            if (!menuMatch.Success)
                continue;

            var layoutMatch = Regex.Match(source, @"super\s*\([^;]*?MachineLayout\.(?<layout>[A-Z0-9_]+)", RegexOptions.Singleline);
            if (!layoutMatch.Success)
            {
                result.Warnings.Add($"{Path.GetFileName(screenPath)}: MachineLayout could not be resolved.");
                continue;
            }

            var layoutKey = layoutMatch.Groups["layout"].Value;
            if (!layouts.TryGetValue(layoutKey, out var spec))
            {
                result.Warnings.Add($"{Path.GetFileName(screenPath)}: unknown MachineLayout.{layoutKey}.");
                continue;
            }

            var menuClass = menuMatch.Groups["menu"].Value;
            menuFiles.TryGetValue(menuClass, out var menuPath);
            var menuSource = menuPath is null ? string.Empty : File.ReadAllText(menuPath);
            var constants = ParseIntConstants(menuSource);

            var layout = new GuiLayoutModel
            {
                Name = Humanize(screenClass.Replace("Screen", string.Empty)),
                Width = spec.Width,
                Height = spec.Height,
                Elements = []
            };

            var machineSlots = ParseMachineSlots(source, menuClass, constants);
            var slotIndex = 1;
            foreach (var slot in machineSlots)
            {
                layout.Elements.Add(new GuiElementModel
                {
                    Id = $"machine_slot_{slotIndex++}",
                    Type = GuiElementType.Slot,
                    X = slot.X - 4,
                    Y = slot.Y - 4,
                    Width = 24,
                    Height = 24,
                    Content = $"{slot.X},{slot.Y}"
                });
            }

            // Forever's MachineScreen draws player slot frames with a 2 px pad around vanilla Slot.x/Slot.y.
            for (var row = 0; row < 3; row++)
            {
                for (var col = 0; col < 9; col++)
                {
                    var slotX = 24 + col * 22;
                    var slotY = spec.InventoryY + row * 20;
                    layout.Elements.Add(new GuiElementModel
                    {
                        Id = $"player_inventory_r{row + 1}_c{col + 1}",
                        Type = GuiElementType.Slot,
                        X = slotX - 2,
                        Y = slotY - 2,
                        Width = 20,
                        Height = 20,
                        Content = $"{slotX},{slotY}"
                    });
                }
            }

            for (var col = 0; col < 9; col++)
            {
                var slotX = 24 + col * 22;
                var slotY = spec.HotbarY;
                layout.Elements.Add(new GuiElementModel
                {
                    Id = $"player_hotbar_{col + 1}",
                    Type = GuiElementType.Slot,
                    X = slotX - 2,
                    Y = slotY - 2,
                    Width = 20,
                    Height = 20,
                    Content = $"{slotX},{slotY}"
                });
            }

            var buttons = ParseButtons(source);
            var buttonIndex = 1;
            foreach (var button in buttons)
            {
                layout.Elements.Add(new GuiElementModel
                {
                    Id = string.IsNullOrWhiteSpace(button.Name) ? $"button_{buttonIndex++}" : UniqueId(layout, button.Name),
                    Type = GuiElementType.Button,
                    X = button.X,
                    Y = button.Y,
                    Width = button.Width,
                    Height = button.Height,
                    Content = Humanize(string.IsNullOrWhiteSpace(button.Name) ? "Button" : button.Name)
                });
            }

            // The common machine header is a useful editing anchor even though its runtime text is dynamic.
            layout.Elements.Insert(0, new GuiElementModel
            {
                Id = "screen_title",
                Type = GuiElementType.Text,
                X = 42,
                Y = 10,
                Width = Math.Max(80, Math.Min(150, spec.Width - 84)),
                Height = 12,
                Content = layout.Name
            });

            var previewPath = ForeverMachinePreviewRenderer.CreatePreview(
                repoRoot,
                screenClass,
                source,
                spec.Width,
                spec.Height,
                spec.InventoryY,
                spec.HotbarY);
            if (previewPath is null)
                result.Warnings.Add($"{screenClass}: visual preview could not be generated; layout import still works.");

            result.Templates.Add(new ForeverMachineTemplate
            {
                DisplayName = layout.Name,
                ScreenClass = screenClass,
                MenuClass = menuClass,
                LayoutKey = layoutKey,
                ScreenPath = Path.GetRelativePath(repoRoot, screenPath).Replace('\\', '/'),
                MenuPath = menuPath is null ? null : Path.GetRelativePath(repoRoot, menuPath).Replace('\\', '/'),
                PreviewImagePath = previewPath,
                Layout = layout,
                MachineSlotCount = machineSlots.Count,
                ButtonCount = buttons.Count
            });
        }

        result.Templates.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
        return result;
    }

    private static Dictionary<string, MachineLayoutSpec> ParseLayouts(string source)
    {
        var result = new Dictionary<string, MachineLayoutSpec>(StringComparer.OrdinalIgnoreCase);
        var regex = new Regex(
            @"public\s+static\s+final\s+MachineLayout\s+(?<name>[A-Z0-9_]+)\s*=\s*new\s+MachineLayout\s*\(\s*(?<w>\d+)\s*,\s*(?<h>\d+)\s*,\s*(?<inventory>\d+)\s*,\s*(?<hotbar>\d+)\s*\)",
            RegexOptions.Multiline);

        foreach (Match match in regex.Matches(source))
        {
            result[match.Groups["name"].Value] = new MachineLayoutSpec(
                int.Parse(match.Groups["w"].Value),
                int.Parse(match.Groups["h"].Value),
                int.Parse(match.Groups["inventory"].Value),
                int.Parse(match.Groups["hotbar"].Value));
        }
        return result;
    }

    private static Dictionary<string, int> ParseIntConstants(string source)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (Match declaration in Regex.Matches(source, @"public\s+static\s+final\s+int\s+(?<body>[^;]+);", RegexOptions.Multiline))
        {
            foreach (var part in declaration.Groups["body"].Value.Split(','))
            {
                var assignment = part.Split('=', 2, StringSplitOptions.TrimEntries);
                if (assignment.Length == 2 && int.TryParse(assignment[1], out var value))
                    result[assignment[0].Trim()] = value;
            }
        }
        return result;
    }

    private static List<(int X, int Y)> ParseMachineSlots(string source, string menuClass, IReadOnlyDictionary<string, int> constants)
    {
        var result = new List<(int X, int Y)>();
        var escapedMenu = Regex.Escape(menuClass);
        var regex = new Regex(
            $@"skin\.slot\s*\(\s*g\s*,\s*x\s*\+\s*{escapedMenu}\.(?<x>[A-Z0-9_]+)\s*,\s*y\s*\+\s*{escapedMenu}\.(?<y>[A-Z0-9_]+)\s*,\s*true\s*\)",
            RegexOptions.Multiline);

        foreach (Match match in regex.Matches(source))
        {
            if (!constants.TryGetValue(match.Groups["x"].Value, out var x) ||
                !constants.TryGetValue(match.Groups["y"].Value, out var y))
                continue;
            if (!result.Contains((x, y)))
                result.Add((x, y));
        }

        // Some screens use literal slot coordinates rather than Menu constants.
        foreach (Match match in Regex.Matches(source,
                     @"skin\.slot\s*\(\s*g\s*,\s*x\s*\+\s*(?<x>\d+)\s*,\s*y\s*\+\s*(?<y>\d+)\s*,\s*true\s*\)",
                     RegexOptions.Multiline))
        {
            var slot = (int.Parse(match.Groups["x"].Value), int.Parse(match.Groups["y"].Value));
            if (!result.Contains(slot))
                result.Add(slot);
        }

        return result;
    }

    private static List<(string Name, int X, int Y, int Width, int Height)> ParseButtons(string source)
    {
        var result = new List<(string, int, int, int, int)>();
        var regex = new Regex(
            @"(?:(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*)?addRenderableWidget\s*\(\s*new\s+MachineButton\s*\(\s*leftPos\s*\+\s*(?<x>\d+)\s*,\s*topPos\s*\+\s*(?<y>\d+)\s*,\s*(?<w>\d+)\s*,\s*(?<h>\d+)",
            RegexOptions.Singleline);

        foreach (Match match in regex.Matches(source))
        {
            result.Add((
                match.Groups["name"].Value,
                int.Parse(match.Groups["x"].Value),
                int.Parse(match.Groups["y"].Value),
                int.Parse(match.Groups["w"].Value),
                int.Parse(match.Groups["h"].Value)));
        }
        return result;
    }

    private static string UniqueId(GuiLayoutModel layout, string preferred)
    {
        var id = preferred;
        var index = 2;
        while (layout.Elements.Any(element => string.Equals(element.Id, id, StringComparison.OrdinalIgnoreCase)))
            id = preferred + "_" + index++;
        return id;
    }

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;
        value = Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2");
        value = value.Replace('_', ' ');
        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}
