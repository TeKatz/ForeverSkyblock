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

    public static ForeverGuiDocument Create(ForeverGuiDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var projected = Clone(source);
        foreach (var element in projected.Elements)
        {
            if (Enum.TryParse<GuiElementType>(element.Kind, ignoreCase: true, out _))
                continue;

            element.Properties ??= new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            if (!element.Properties.ContainsKey(OriginalKindProperty))
                element.Properties[OriginalKindProperty] = JsonSerializer.SerializeToElement(element.Kind);
            element.Kind = ProjectKind(element.Kind).ToString();
        }
        return projected;
    }

    public static ForeverGuiDocument Restore(ForeverGuiDocument projected)
    {
        ArgumentNullException.ThrowIfNull(projected);
        var restored = Clone(projected);
        foreach (var element in restored.Elements)
        {
            if (element.Properties is null ||
                !element.Properties.TryGetValue(OriginalKindProperty, out var original) ||
                original.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(original.GetString()))
                continue;

            element.Kind = original.GetString()!;
            element.Properties.Remove(OriginalKindProperty);
        }
        return restored;
    }

    public static GuiLayoutModel ToLegacyLayout(ForeverGuiDocument source)
        => ForeverGuiDocumentService.ToLegacyLayout(Create(source));

    public static ForeverGuiDocument FromLegacyLayout(GuiLayoutModel layout)
        => Restore(ForeverGuiDocumentService.FromLegacyLayout(layout));

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
