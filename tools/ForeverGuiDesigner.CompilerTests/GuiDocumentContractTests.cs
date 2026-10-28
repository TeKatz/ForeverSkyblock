using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using ForeverGuiDesigner.Models;

internal static class GuiDocumentContractTests
{
    [ModuleInitializer]
    internal static void Run()
    {
        SharedContractRoundTrip();
        StandaloneEditorForwardCompatibility();
    }

    private static void SharedContractRoundTrip()
    {
        var document = new GuiDocument
        {
            Name = "Machine GUI",
            Width = 220,
            Height = 180,
            Canvas = new GuiCanvasSettings { Snap = 4, GridVisible = true },
            Layers =
            [
                new GuiLayerModel { Id = "base", Name = "Base", Order = 0 },
                new GuiLayerModel { Id = "effects", Name = "Effects", Order = 1 }
            ],
            Assets =
            [
                new GuiAssetReferenceModel
                {
                    Id = "panel",
                    Kind = "texture",
                    Resource = "textures/gui/panel.png"
                }
            ],
            Elements =
            [
                new GuiElementModel
                {
                    Id = "progress",
                    Type = GuiElementType.Texture,
                    X = 30,
                    Y = 44,
                    Width = 60,
                    Height = 8,
                    LayerId = "base",
                    AssetId = "panel",
                    StyleId = "machine-meter",
                    EffectIds = ["hover-glow"]
                }
            ],
            Bindings =
            [
                new GuiBindingModel
                {
                    Id = "progress-value",
                    ElementId = "progress",
                    TargetProperty = "value",
                    Context = "machine",
                    SourceId = "processor",
                    SourcePort = "progress",
                    DataType = "number"
                }
            ],
            Styles =
            [
                new GuiStyleModel
                {
                    Id = "machine-meter",
                    Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["fillDirection"] = JsonSerializer.SerializeToElement("left-to-right")
                    }
                }
            ],
            Effects =
            [
                new GuiEffectModel
                {
                    Id = "hover-glow",
                    Kind = "glow",
                    Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["strength"] = JsonSerializer.SerializeToElement(0.7)
                    }
                }
            ],
            States =
            [
                new GuiVisualStateModel { Id = "idle", DisplayName = "Idle" },
                new GuiVisualStateModel { Id = "running", DisplayName = "Running" }
            ]
        };

        var json = GuiDocumentContract.Serialize(document);
        var restored = GuiDocumentContract.Deserialize(json);
        Check(restored.Width == 220 && restored.Height == 180, "GUI canvas geometry must round-trip.");
        Check(restored.Canvas.Snap == 4, "GUI canvas settings must round-trip.");
        Check(restored.Layers.Count == 2 && restored.Assets.Count == 1, "Layers/assets must round-trip.");
        Check(restored.Bindings.Single().SourcePort == "progress", "Machine-neutral bindings must round-trip.");
        Check(restored.Styles.Single().Properties.ContainsKey("fillDirection"), "Styles must round-trip.");
        Check(restored.Effects.Single().Properties.ContainsKey("strength"), "Effects must round-trip.");
        Check(restored.States.Count == 2, "Visual states must round-trip.");
    }

    private static void StandaloneEditorForwardCompatibility()
    {
        const string json = """
        {
          "formatVersion": 1,
          "name": "Forward compatible layout",
          "width": 176,
          "height": 166,
          "canvas": { "snap": 2, "gridVisible": true, "futureCanvasMode": "pixel-perfect" },
          "elements": [
            {
              "id": "slot",
              "type": "slot",
              "x": 12,
              "y": 18,
              "width": 18,
              "height": 18,
              "content": "",
              "visible": true,
              "futureElementProperty": { "keep": true }
            }
          ],
          "layers": [{ "id": "main", "name": "Main", "order": 0, "visible": true, "locked": false }],
          "assets": [],
          "bindings": [{
            "id": "slot-item",
            "elementId": "slot",
            "targetProperty": "item",
            "context": "machine",
            "sourceId": "input",
            "sourcePort": "item",
            "dataType": "item"
          }],
          "styles": [],
          "effects": [{ "id": "glow", "kind": "glow", "properties": { "radius": 3 } }],
          "states": [],
          "futureDocumentFlag": { "version": 99 }
        }
        """;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        var layout = JsonSerializer.Deserialize<GuiLayoutModel>(json, options)
                     ?? throw new InvalidOperationException("Forward-compatible GUI layout did not deserialize.");
        Check(layout.ExtensionData?.ContainsKey("futureDocumentFlag") == true,
            "Standalone layout model must retain unknown document fields.");
        Check(layout.Elements.Single().ExtensionData?.ContainsKey("futureElementProperty") == true,
            "Standalone element model must retain unknown element fields.");
        Check(layout.Bindings.Single().SourceId == "input", "Standalone layout must retain shared machine bindings.");
        Check(layout.Effects.Single().Kind == "glow", "Standalone layout must retain shared effects.");

        layout.Elements[0].X = 24;
        var saved = JsonSerializer.Serialize(layout, options);
        using var parsed = JsonDocument.Parse(saved);
        Check(parsed.RootElement.TryGetProperty("futureDocumentFlag", out _),
            "Unknown document fields must survive standalone-editor save.");
        var savedElement = parsed.RootElement.GetProperty("elements")[0];
        Check(savedElement.TryGetProperty("futureElementProperty", out _),
            "Unknown element fields must survive standalone-editor save.");
        Check(savedElement.GetProperty("x").GetInt32() == 24,
            "Known standalone edits must coexist with forward-compatible fields.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
