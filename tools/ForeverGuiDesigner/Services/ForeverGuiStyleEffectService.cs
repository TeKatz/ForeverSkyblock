using System.Text.Json;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner.Services;

public sealed record ForeverGuiElementStyleSettings(
    string Tint,
    double Opacity,
    bool HoverEnabled,
    double HoverScale,
    bool GlowEnabled,
    string GlowColor,
    bool ShadowEnabled,
    bool PulseEnabled,
    double PulseMinOpacity,
    int PulseDurationMs,
    bool PressedOffsetEnabled,
    double PressedOffsetX,
    double PressedOffsetY,
    bool DisabledAppearanceEnabled,
    double DisabledOpacity,
    bool StateSwitchEnabled,
    string StateSwitchStateId);

/// <summary>
/// Canonical shared style/effect vocabulary. These objects live in ForeverGuiDocument and are
/// intentionally independent from Machine bindings so standalone and Machine GUIs use identical
/// authoring semantics.
/// </summary>
public static class ForeverGuiStyleEffectService
{
    public const string HoverKind = "hover";
    public const string GlowKind = "glow";
    public const string ShadowKind = "shadow";
    public const string PulseKind = "pulse";
    public const string PressedOffsetKind = "pressed-offset";
    public const string DisabledAppearanceKind = "disabled-appearance";
    public const string StateSwitchKind = "state-switch";

    public static ForeverGuiElementStyleSettings Read(ForeverGuiDocument document, ForeverGuiElement element)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(element);

        var style = string.IsNullOrWhiteSpace(element.StyleId)
            ? null
            : document.Styles.FirstOrDefault(candidate => candidate.Id.Equals(element.StyleId, StringComparison.OrdinalIgnoreCase));
        var effects = document.Effects
            .Where(effect => element.EffectIds.Contains(effect.Id, StringComparer.OrdinalIgnoreCase))
            .ToDictionary(effect => effect.Kind, StringComparer.OrdinalIgnoreCase);

        return new ForeverGuiElementStyleSettings(
            ReadString(style?.Properties, "tint", string.Empty),
            ReadNumber(style?.Properties, "opacity", 1),
            effects.TryGetValue(HoverKind, out var hover),
            ReadNumber(hover?.Properties, "scale", 1.05),
            effects.TryGetValue(GlowKind, out var glow),
            ReadString(glow?.Properties, "color", "#4EB8BC"),
            effects.ContainsKey(ShadowKind),
            effects.TryGetValue(PulseKind, out var pulse),
            ReadNumber(pulse?.Properties, "minOpacity", 0.65),
            ReadInt(pulse?.Properties, "durationMs", 900),
            effects.TryGetValue(PressedOffsetKind, out var pressed),
            ReadNumber(pressed?.Properties, "x", 1),
            ReadNumber(pressed?.Properties, "y", 1),
            effects.TryGetValue(DisabledAppearanceKind, out var disabled),
            ReadNumber(disabled?.Properties, "opacity", 0.45),
            effects.TryGetValue(StateSwitchKind, out var stateSwitch),
            ReadString(stateSwitch?.Properties, "stateId", string.Empty));
    }

    public static void Apply(
        ForeverGuiDocument document,
        ForeverGuiElement element,
        ForeverGuiElementStyleSettings settings)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(element);
        ValidateSettings(document, settings);

        ApplyStyle(document, element, settings);
        ApplyEffect(document, element, HoverKind, settings.HoverEnabled, new Dictionary<string, JsonElement>
        {
            ["scale"] = JsonSerializer.SerializeToElement(settings.HoverScale)
        });
        ApplyEffect(document, element, GlowKind, settings.GlowEnabled, new Dictionary<string, JsonElement>
        {
            ["color"] = JsonSerializer.SerializeToElement(settings.GlowColor),
            ["blurRadius"] = JsonSerializer.SerializeToElement(12.0),
            ["opacity"] = JsonSerializer.SerializeToElement(0.8)
        });
        ApplyEffect(document, element, ShadowKind, settings.ShadowEnabled, new Dictionary<string, JsonElement>
        {
            ["blurRadius"] = JsonSerializer.SerializeToElement(8.0),
            ["depth"] = JsonSerializer.SerializeToElement(2.0),
            ["opacity"] = JsonSerializer.SerializeToElement(0.55)
        });
        ApplyEffect(document, element, PulseKind, settings.PulseEnabled, new Dictionary<string, JsonElement>
        {
            ["minOpacity"] = JsonSerializer.SerializeToElement(settings.PulseMinOpacity),
            ["durationMs"] = JsonSerializer.SerializeToElement(settings.PulseDurationMs)
        });
        ApplyEffect(document, element, PressedOffsetKind, settings.PressedOffsetEnabled, new Dictionary<string, JsonElement>
        {
            ["x"] = JsonSerializer.SerializeToElement(settings.PressedOffsetX),
            ["y"] = JsonSerializer.SerializeToElement(settings.PressedOffsetY)
        });
        ApplyEffect(document, element, DisabledAppearanceKind, settings.DisabledAppearanceEnabled, new Dictionary<string, JsonElement>
        {
            ["opacity"] = JsonSerializer.SerializeToElement(settings.DisabledOpacity)
        });
        ApplyEffect(document, element, StateSwitchKind, settings.StateSwitchEnabled, new Dictionary<string, JsonElement>
        {
            ["stateId"] = JsonSerializer.SerializeToElement(settings.StateSwitchStateId)
        });
    }

    public static void Clear(ForeverGuiDocument document, ForeverGuiElement element)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(element);
        if (!string.IsNullOrWhiteSpace(element.StyleId))
        {
            var id = element.StyleId;
            document.Styles.RemoveAll(style => style.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            element.StyleId = null;
        }
        var owned = element.EffectIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        document.Effects.RemoveAll(effect => owned.Contains(effect.Id));
        element.EffectIds.Clear();
    }

    private static void ApplyStyle(ForeverGuiDocument document, ForeverGuiElement element, ForeverGuiElementStyleSettings settings)
    {
        var hasStyle = !string.IsNullOrWhiteSpace(settings.Tint) || Math.Abs(settings.Opacity - 1) > 0.0001;
        if (!hasStyle)
        {
            if (!string.IsNullOrWhiteSpace(element.StyleId))
            {
                var oldId = element.StyleId;
                document.Styles.RemoveAll(style => style.Id.Equals(oldId, StringComparison.OrdinalIgnoreCase));
                element.StyleId = null;
            }
            return;
        }

        var id = string.IsNullOrWhiteSpace(element.StyleId) ? UniqueId(document.Styles.Select(style => style.Id), $"style_{element.Id}") : element.StyleId;
        var style = document.Styles.FirstOrDefault(candidate => candidate.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (style is null)
        {
            style = new ForeverGuiStyle { Id = id, Name = $"{element.Id} Style" };
            document.Styles.Add(style);
        }
        style.Properties["tint"] = JsonSerializer.SerializeToElement(settings.Tint);
        style.Properties["opacity"] = JsonSerializer.SerializeToElement(settings.Opacity);
        element.StyleId = id;
    }

    private static void ApplyEffect(
        ForeverGuiDocument document,
        ForeverGuiElement element,
        string kind,
        bool enabled,
        Dictionary<string, JsonElement> properties)
    {
        var existing = document.Effects.FirstOrDefault(effect =>
            element.EffectIds.Contains(effect.Id, StringComparer.OrdinalIgnoreCase) &&
            effect.Kind.Equals(kind, StringComparison.OrdinalIgnoreCase));
        if (!enabled)
        {
            if (existing is not null)
            {
                element.EffectIds.RemoveAll(id => id.Equals(existing.Id, StringComparison.OrdinalIgnoreCase));
                document.Effects.Remove(existing);
            }
            return;
        }

        if (existing is null)
        {
            var id = UniqueId(document.Effects.Select(effect => effect.Id), $"effect_{element.Id}_{kind.Replace('-', '_')}");
            existing = new ForeverGuiEffect { Id = id, Kind = kind };
            document.Effects.Add(existing);
            element.EffectIds.Add(id);
        }
        foreach (var property in properties)
            existing.Properties[property.Key] = property.Value.Clone();
    }

    private static void ValidateSettings(ForeverGuiDocument document, ForeverGuiElementStyleSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.Tint) && !IsHexColor(settings.Tint))
            throw new InvalidDataException("Tint must use #RRGGBB or #AARRGGBB.");
        if (!double.IsFinite(settings.Opacity) || settings.Opacity is < 0 or > 1)
            throw new InvalidDataException("Opacity must be between 0 and 1.");
        if (!double.IsFinite(settings.HoverScale) || settings.HoverScale is < 0.25 or > 4)
            throw new InvalidDataException("Hover scale must be between 0.25 and 4.");
        if (!string.IsNullOrWhiteSpace(settings.GlowColor) && !IsHexColor(settings.GlowColor))
            throw new InvalidDataException("Glow color must use #RRGGBB or #AARRGGBB.");
        if (!double.IsFinite(settings.PulseMinOpacity) || settings.PulseMinOpacity is < 0 or > 1)
            throw new InvalidDataException("Pulse minimum opacity must be between 0 and 1.");
        if (settings.PulseDurationMs is < 100 or > 60_000)
            throw new InvalidDataException("Pulse duration must be 100..60000 ms.");
        if (!double.IsFinite(settings.PressedOffsetX) || !double.IsFinite(settings.PressedOffsetY) ||
            Math.Abs(settings.PressedOffsetX) > 64 || Math.Abs(settings.PressedOffsetY) > 64)
            throw new InvalidDataException("Pressed offset must stay within -64..64 pixels.");
        if (!double.IsFinite(settings.DisabledOpacity) || settings.DisabledOpacity is < 0 or > 1)
            throw new InvalidDataException("Disabled opacity must be between 0 and 1.");
        if (settings.StateSwitchEnabled)
        {
            if (string.IsNullOrWhiteSpace(settings.StateSwitchStateId))
                throw new InvalidDataException("State Switch requires a GUI state id.");
            if (!document.States.Any(state => state.Id.Equals(settings.StateSwitchStateId, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException($"State Switch references missing GUI state '{settings.StateSwitchStateId}'.");
        }
    }

    private static bool IsHexColor(string value)
    {
        if (value.Length is not (7 or 9) || value[0] != '#') return false;
        return value[1..].All(character => Uri.IsHexDigit(character));
    }

    private static string UniqueId(IEnumerable<string> existing, string candidate)
    {
        var used = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var safe = new string(candidate.Select(character => char.IsLetterOrDigit(character) || character == '_' ? character : '_').ToArray());
        var id = safe;
        for (var suffix = 2; used.Contains(id); suffix++) id = $"{safe}_{suffix}";
        return id;
    }

    private static string ReadString(Dictionary<string, JsonElement>? properties, string key, string fallback)
        => properties is not null && properties.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static double ReadNumber(Dictionary<string, JsonElement>? properties, string key, double fallback)
        => properties is not null && properties.TryGetValue(key, out var value) && value.TryGetDouble(out var result) && double.IsFinite(result)
            ? result
            : fallback;

    private static int ReadInt(Dictionary<string, JsonElement>? properties, string key, int fallback)
        => properties is not null && properties.TryGetValue(key, out var value) && value.TryGetInt32(out var result)
            ? result
            : fallback;
}
