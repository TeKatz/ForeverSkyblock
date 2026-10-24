using System.IO;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner.Services;

public enum GuiValidationSeverity
{
    Info,
    Warning,
    Error
}

public sealed record GuiValidationIssue(
    GuiValidationSeverity Severity,
    string Message,
    string? ElementId = null)
{
    public override string ToString()
    {
        var prefix = Severity switch
        {
            GuiValidationSeverity.Error => "ERROR",
            GuiValidationSeverity.Warning => "WARN",
            _ => "INFO"
        };
        return string.IsNullOrWhiteSpace(ElementId)
            ? $"{prefix} · {Message}"
            : $"{prefix} · {ElementId} · {Message}";
    }
}

public static class GuiValidationService
{
    public static IReadOnlyList<GuiValidationIssue> Validate(GuiLayoutModel layout, string? repoRoot = null)
    {
        var issues = new List<GuiValidationIssue>();
        if (layout.Width <= 0 || layout.Height <= 0)
        {
            issues.Add(new GuiValidationIssue(
                GuiValidationSeverity.Error,
                $"GUI dimensions must be positive, current size is {layout.Width}×{layout.Height}."));
            return issues;
        }

        var elements = layout.Elements ?? [];
        foreach (var duplicate in elements
                     .Where(element => !string.IsNullOrWhiteSpace(element.Id))
                     .GroupBy(element => element.Id, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            issues.Add(new GuiValidationIssue(
                GuiValidationSeverity.Error,
                $"Element id is used {duplicate.Count()} times.",
                duplicate.Key));
        }

        foreach (var element in elements)
        {
            if (string.IsNullOrWhiteSpace(element.Id))
            {
                issues.Add(new GuiValidationIssue(GuiValidationSeverity.Error, "Element id is empty."));
            }

            if (element.Width <= 0 || element.Height <= 0)
            {
                issues.Add(new GuiValidationIssue(
                    GuiValidationSeverity.Error,
                    $"Size must be positive, current size is {element.Width}×{element.Height}.",
                    element.Id));
                continue;
            }

            if (element.X < 0 || element.Y < 0 ||
                element.X + element.Width > layout.Width ||
                element.Y + element.Height > layout.Height)
            {
                issues.Add(new GuiValidationIssue(
                    GuiValidationSeverity.Error,
                    $"Bounds ({element.X},{element.Y},{element.Width},{element.Height}) leave the {layout.Width}×{layout.Height} GUI.",
                    element.Id));
            }

            if (element.Type == GuiElementType.Text && (element.TextScale <= 0 || element.TextScale > 4))
            {
                issues.Add(new GuiValidationIssue(
                    GuiValidationSeverity.Warning,
                    $"Text scale {element.TextScale:0.##} is outside the recommended 0.25–4 range.",
                    element.Id));
            }

            if (element.Type == GuiElementType.Slot &&
                element.Id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase) &&
                (element.Width != 24 || element.Height != 24))
            {
                issues.Add(new GuiValidationIssue(
                    GuiValidationSeverity.Error,
                    "Forever machine slots use a fixed 24×24 MachineSkin frame around the native 16×16 item slot.",
                    element.Id));
            }

            if (element.Type == GuiElementType.Slot &&
                (element.Id.StartsWith("player_inventory_", StringComparison.OrdinalIgnoreCase) ||
                 element.Id.StartsWith("player_hotbar_", StringComparison.OrdinalIgnoreCase)) &&
                (element.Width != 20 || element.Height != 20))
            {
                issues.Add(new GuiValidationIssue(
                    GuiValidationSeverity.Warning,
                    "Player slots normally use the 20×20 MachineSkin frame.",
                    element.Id));
            }

            if (element.Type == GuiElementType.Texture &&
                !string.IsNullOrWhiteSpace(element.TexturePath) &&
                !string.IsNullOrWhiteSpace(repoRoot))
            {
                var texturePath = element.TexturePath!;
                var fullPath = Path.IsPathRooted(texturePath)
                    ? texturePath
                    : Path.Combine(
                        repoRoot!,
                        "mod", "src", "main", "resources", "assets", "forever_skyblock",
                        texturePath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(fullPath))
                {
                    issues.Add(new GuiValidationIssue(
                        GuiValidationSeverity.Warning,
                        $"Texture does not exist: {element.TexturePath}",
                        element.Id));
                }
            }
        }

        var interactive = elements
            .Where(element => element.Visible && element.Width > 0 && element.Height > 0)
            .Where(element => element.Type is GuiElementType.Slot or GuiElementType.Button)
            .ToList();
        for (var i = 0; i < interactive.Count; i++)
        {
            for (var j = i + 1; j < interactive.Count; j++)
            {
                var left = interactive[i];
                var right = interactive[j];
                if (!Overlaps(left, right))
                    continue;

                issues.Add(new GuiValidationIssue(
                    GuiValidationSeverity.Warning,
                    $"Overlaps interactive element '{right.Id}'.",
                    left.Id));
            }
        }

        return issues;
    }

    private static bool Overlaps(GuiElementModel left, GuiElementModel right)
        => left.X < right.X + right.Width &&
           left.X + left.Width > right.X &&
           left.Y < right.Y + right.Height &&
           left.Y + left.Height > right.Y;
}
