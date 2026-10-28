using System.Text.Json;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner.Services;

/// <summary>
/// Lossless projection from the shared GUI contract into the element kinds understood by the
/// established GUI editor. Machine/runtime widgets keep their exact semantic kind in provider
/// metadata while the legacy canvas receives the nearest editable visual primitive. Restoring the
/// projection reinstates the original kind before the document is committed to its context.
/// </summary>
public static class ForeverGuiLegacyEditorProjection
{
    public const string OriginalKindProperty = "foreverStudioOriginalKind";
    public const string OriginalXProperty = "foreverStudioOriginalX";
    public const string OriginalYProperty = "foreverStudioOriginalY";
    public const string OriginalWidthProperty = "foreverStudioOriginalWidth";
    public const string OriginalHeightProperty = "foreverStudioOriginalHeight";

    public static ForeverGuiDocument Create(ForeverGuiDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var projected = Clone(source);
        foreach (var element in projected.Elements)
        {
            element.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            if (!Enum.TryParse<GuiElementType>(element.Kind, ignoreCase: true, out _))
            {
                if (!element.Properties.ContainsKey(OriginalKindProperty))
                    element.Properties[OriginalKindProperty] = JsonSerializer.SerializeToElement(element.Kind);
                element.Kind = ProjectKind(element.Kind).ToString();
            }

            ProjectGeometry(element, OriginalXProperty, element.X, value => element.X = value);
            ProjectGeometry(element, OriginalYProperty, element.Y, value => element.Y = value);
            ProjectGeometry(element, OriginalWidthProperty, element.Width, value => element.Width = Math.Max(1, value));
            ProjectGeometry(element, OriginalHeightProperty, element.Height, value => element.Height = Math.Max(1, value));
        }
        return projected;
    }

    public static ForeverGuiDocument Restore(ForeverGuiDocument projected)
    {
        ArgumentNullException.ThrowIfNull(projected);
        var restored = Clone(projected);
        foreach (var element in restored.Elements)
        {
            element.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            if (element.Properties.TryGetValue(OriginalKindProperty, out var original) &&
                original.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(original.GetString()))
            {
                element.Kind = original.GetString()!;
                element.Properties.Remove(OriginalKindProperty);
            }

            RestoreGeometry(element, OriginalXProperty, element.X, value => element.X = value);
            RestoreGeometry(element, OriginalYProperty, element.Y, value => element.Y = value);
            RestoreGeometry(element, OriginalWidthProperty, element.Width, value => element.Width = value);
            RestoreGeometry(element, OriginalHeightProperty, element.Height, value => element.Height = value);
        }
        return restored;
    }

    public static GuiLayoutModel ToLegacyLayout(ForeverGuiDocument source)
        => ForeverGuiDocumentService.ToLegacyLayout(Create(source));

    public static ForeverGuiDocument FromLegacyLayout(GuiLayoutModel layout)
        => Restore(ForeverGuiDocumentService.FromLegacyLayout(layout));

    private static void ProjectGeometry(
        ForeverGuiElement element,
        string property,
        double exactValue,
        Action<double> setProjected)
    {
        var projected = Math.Round(exactValue, MidpointRounding.AwayFromZero);
        if (Math.Abs(exactValue - projected) <= 0.0001)
            return;

        if (!element.Properties.ContainsKey(property))
            element.Properties[property] = JsonSerializer.SerializeToElement(exactValue);
        setProjected(projected);
    }

    private static void RestoreGeometry(
        ForeverGuiElement element,
        string property,
        double editedValue,
        Action<double> restore)
    {
        if (!element.Properties.TryGetValue(property, out var exactJson) || !exactJson.TryGetDouble(out var exactValue))
            return;

        var originallyProjected = Math.Round(exactValue, MidpointRounding.AwayFromZero);
        if (Math.Abs(editedValue - originallyProjected) <= 0.0001)
            restore(exactValue);
        element.Properties.Remove(property);
    }

    private static GuiElementType ProjectKind(string kind)
        => kind.ToLowerInvariant() switch
        {
            "item-slot" or "output-slot" => GuiElementType.Slot,
            "value" => GuiElementType.Text,
            "progress-bar" or "energy-bar" or "fluid-gauge" or "status" => GuiElementType.Texture,
            // Future provider-specific visual kinds are intentionally projected as Texture instead
            // of rejected. OriginalKindProperty guarantees their semantic identity survives.
            _ => GuiElementType.Texture
        };

    private static ForeverGuiDocument Clone(ForeverGuiDocument source)
        => ForeverGuiDocumentService.Deserialize(ForeverGuiDocumentService.Serialize(source));
}
