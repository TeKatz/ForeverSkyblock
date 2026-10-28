using System.Runtime.CompilerServices;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

internal static class UnifiedGuiStyleEffectContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var document = new ForeverGuiDocument
        {
            DocumentId = "style_contract",
            Name = "Style Contract",
            States = [new ForeverGuiState { Id = "active", Name = "Active", IsInitial = true }],
            Elements =
            [
                new ForeverGuiElement
                {
                    Id = "button",
                    Kind = "Button",
                    X = 10,
                    Y = 10,
                    Width = 60,
                    Height = 16,
                    Text = "Styled"
                }
            ]
        };
        var element = document.Elements.Single();
        var settings = new ForeverGuiElementStyleSettings(
            Tint: "#80FFFFFF",
            Opacity: 0.9,
            HoverEnabled: true,
            HoverScale: 1.08,
            GlowEnabled: true,
            GlowColor: "#4EB8BC",
            ShadowEnabled: true,
            PulseEnabled: true,
            PulseMinOpacity: 0.6,
            PulseDurationMs: 750,
            PressedOffsetEnabled: true,
            PressedOffsetX: 1,
            PressedOffsetY: 2,
            DisabledAppearanceEnabled: true,
            DisabledOpacity: 0.4,
            StateSwitchEnabled: true,
            StateSwitchStateId: "active");

        ForeverGuiStyleEffectService.Apply(document, element, settings);
        Check(document.Styles.Count == 1 && !string.IsNullOrWhiteSpace(element.StyleId),
            "Applying shared style settings must create one referenced style object.");
        Check(document.Effects.Count == 7 && element.EffectIds.Count == 7,
            "All supported shared effects must persist as referenced effect objects.");

        var read = ForeverGuiStyleEffectService.Read(document, element);
        Check(read == settings, "Shared style/effect settings must round-trip through canonical objects.");

        var json = ForeverGuiDocumentService.Serialize(document);
        var restored = ForeverGuiDocumentService.Deserialize(json);
        var restoredElement = restored.Elements.Single();
        Check(ForeverGuiStyleEffectService.Read(restored, restoredElement) == settings,
            "Shared style/effect objects must survive GUI document serialization.");

        var legacy = ForeverGuiDocumentService.ToLegacyLayout(restored);
        var recovered = ForeverGuiDocumentService.FromLegacyLayout(legacy);
        Check(ForeverGuiStyleEffectService.Read(recovered, recovered.Elements.Single()) == settings,
            "Shared style/effect objects must survive the legacy editor bridge.");

        var invalidState = settings with { StateSwitchStateId = "missing" };
        CheckThrows(() => ForeverGuiStyleEffectService.Apply(recovered, recovered.Elements.Single(), invalidState),
            "State Switch must fail closed when it references a missing GUI state.");

        ForeverGuiStyleEffectService.Clear(recovered, recovered.Elements.Single());
        Check(recovered.Styles.Count == 0 && recovered.Effects.Count == 0 &&
              recovered.Elements.Single().StyleId is null && recovered.Elements.Single().EffectIds.Count == 0,
            "Clearing shared styles/effects must remove owned objects and references.");
    }

    private static void CheckThrows(Action action, string message)
    {
        try
        {
            action();
        }
        catch (InvalidDataException)
        {
            return;
        }
        throw new InvalidOperationException(message);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
