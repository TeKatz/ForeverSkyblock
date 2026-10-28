using System.Runtime.CompilerServices;
using System.Text.Json;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

internal static class UnifiedGuiContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        LegacyEditorRoundTrip();
        MachineBuilderRoundTrip();
    }

    private static void LegacyEditorRoundTrip()
    {
        var legacy = new GuiLayoutModel
        {
            Name = "Unified Contract",
            Width = 176,
            Height = 166,
            ExtensionData = new Dictionary<string, JsonElement>
            {
                ["futureLayoutField"] = Json(new { preserved = true })
            }
        };
        legacy.Elements.Add(new GuiElementModel
        {
            Id = "status",
            Type = GuiElementType.Button,
            X = 12,
            Y = 18,
            Width = 42,
            Height = 16,
            Content = "Status",
            TexturePath = "forever_skyblock:textures/gui/status.png",
            Visible = true,
            Locked = true,
            Group = "controls",
            TextShadow = false,
            TextScale = 1.5,
            TextAlignment = "center",
            ItemId = "minecraft:iron_ingot",
            StackCount = 3,
            Durability = 0.75,
            GhostPreview = true,
            GlintPreview = true,
            EditorSuppressed = true,
            EditorTextOverride = "Override",
            EditorZIndexOverride = 17,
            EditorCustomTextureOverride = true,
            ExtensionData = new Dictionary<string, JsonElement>
            {
                ["futureElementField"] = Json("keep-me")
            }
        });

        var shared = ForeverGuiDocumentService.FromLegacyLayout(legacy);
        shared.DocumentId = "unified_contract_screen";
        shared.Canvas.Snap = 4;
        shared.Canvas.BackgroundAssetId = "background";
        shared.Canvas.ExtensionData = new Dictionary<string, JsonElement> { ["futureCanvasField"] = Json(99) };
        shared.Layers.Add(new ForeverGuiLayer { Id = "foreground", Name = "Foreground", Order = 10 });
        shared.Assets.Add(new ForeverGuiAssetReference { Id = "button_asset", Kind = "texture", Resource = "forever_skyblock:gui/button" });
        shared.Bindings.Add(new ForeverGuiBinding
        {
            Id = "status_binding",
            Provider = "test",
            SourceId = "status_source",
            SourcePortId = "running",
            SourceType = "boolean"
        });
        shared.Styles.Add(new ForeverGuiStyle
        {
            Id = "status_style",
            Name = "Status Style",
            Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase) { ["hoverColor"] = Json("#ffffff") }
        });
        shared.Effects.Add(new ForeverGuiEffect
        {
            Id = "status_hover",
            Kind = "hover",
            Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase) { ["scale"] = Json(1.05) }
        });
        shared.States.Add(new ForeverGuiState { Id = "idle", Name = "Idle", IsInitial = true });
        shared.ExtensionData = Merge(shared.ExtensionData, "futureContractField", Json(new { version = 2 }));

        var element = shared.Elements.Single();
        element.LayerId = "foreground";
        element.AssetId = "button_asset";
        element.BindingId = "status_binding";
        element.StyleId = "status_style";
        element.EffectIds.Add("status_hover");
        element.StateIds.Add("idle");
        element.Order = 3.5;
        element.Properties["providerOnly"] = Json("still-here");
        element.ExtensionData = Merge(element.ExtensionData, "futureSharedElement", Json(true));

        var serialized = ForeverGuiDocumentService.Serialize(shared);
        var restoredShared = ForeverGuiDocumentService.Deserialize(serialized);
        Check(!ForeverGuiDocumentService.Validate(restoredShared).Any(issue => issue.Severity == ForeverGuiContractSeverity.Error),
            "Unified GUI document must validate after shared serialization round-trip.");

        var bridgedLegacy = ForeverGuiDocumentService.ToLegacyLayout(restoredShared);
        Check(bridgedLegacy.ExtensionData?.ContainsKey("futureLayoutField") == true,
            "Legacy migration must preserve unknown layout fields.");
        Check(bridgedLegacy.Elements[0].ExtensionData?.ContainsKey("futureElementField") == true,
            "Legacy migration must preserve unknown element fields.");

        // Simulate a legacy-editor edit after shared metadata has been embedded.
        bridgedLegacy.Elements[0].X = 33;
        bridgedLegacy.Elements[0].Content = "Edited in legacy editor";
        var recovered = ForeverGuiDocumentService.FromLegacyLayout(bridgedLegacy);
        var recoveredElement = recovered.Elements.Single();
        Check(recovered.DocumentId == "unified_contract_screen" && Math.Abs(recovered.Canvas.Snap - 4) < 0.0001,
            "Shared document identity/canvas metadata must survive a legacy editor round-trip.");
        Check(recovered.Layers.Count == 1 && recovered.Assets.Count == 1 && recovered.Bindings.Count == 1 &&
              recovered.Styles.Count == 1 && recovered.Effects.Count == 1 && recovered.States.Count == 1,
            "All shared GUI collections must survive a legacy editor round-trip.");
        Check(recoveredElement.X == 33 && recoveredElement.Text == "Edited in legacy editor",
            "Legacy visual edits must remain authoritative when the shared document is recovered.");
        Check(recoveredElement.LayerId == "foreground" && recoveredElement.AssetId == "button_asset" &&
              recoveredElement.BindingId == "status_binding" && recoveredElement.StyleId == "status_style" &&
              recoveredElement.EffectIds.SequenceEqual(["status_hover"]) && recoveredElement.StateIds.SequenceEqual(["idle"]),
            "Shared element references must survive the legacy bridge.");
        Check(recoveredElement.Properties.TryGetValue("providerOnly", out var providerOnly) && providerOnly.GetString() == "still-here",
            "Provider-specific shared element metadata must survive the legacy bridge.");
        Check(recovered.ExtensionData?.ContainsKey("futureContractField") == true &&
              recoveredElement.ExtensionData?.ContainsKey("futureSharedElement") == true,
            "Unknown shared-contract extension fields must round-trip without loss.");
    }

    private static void MachineBuilderRoundTrip()
    {
        var machine = ForeverStudioMachineProjectService.CreateBlank("gui_contract_machine", "GUI Contract Machine");
        machine.Gui.ScreenId = "gui_contract_machine_screen";
        machine.Gui.Bindings.Add(new ForeverStudioMachineGuiBinding
        {
            Id = "input_widget",
            LibraryItemId = "asset.input-tray",
            LibraryItemVersion = 1,
            SourceId = "input_node",
            Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
            {
                ["widget"] = Json("item-slot"),
                ["sourcePortId"] = Json("item"),
                ["sourceType"] = Json("item"),
                ["label"] = Json("Input"),
                ["x"] = Json(20),
                ["y"] = Json(30),
                ["width"] = Json(18),
                ["height"] = Json(18),
                ["order"] = Json(2),
                ["futureBindingProperty"] = Json(new { keep = "yes" })
            }
        });

        var shared = ForeverMachineGuiDocumentBridge.FromMachineDocument(machine);
        shared.Layers.Add(new ForeverGuiLayer { Id = "machine_layer", Name = "Machine Layer" });
        shared.Styles.Add(new ForeverGuiStyle { Id = "machine_style", Name = "Machine Style" });
        shared.Effects.Add(new ForeverGuiEffect { Id = "machine_hover", Kind = "hover" });
        shared.States.Add(new ForeverGuiState { Id = "machine_idle", Name = "Idle", IsInitial = true });
        var bound = shared.Elements.Single();
        bound.LayerId = "machine_layer";
        bound.StyleId = "machine_style";
        bound.EffectIds.Add("machine_hover");
        bound.StateIds.Add("machine_idle");
        bound.Properties["editorOnly"] = Json(1234);
        shared.Bindings.Single().Properties["sharedBindingOnly"] = Json("preserve");
        shared.Elements.Add(new ForeverGuiElement
        {
            Id = "static_label",
            Kind = "Text",
            X = 70,
            Y = 12,
            Width = 60,
            Height = 10,
            Order = 50,
            Text = "Shared-only label",
            LayerId = "machine_layer",
            StyleId = "machine_style"
        });

        ForeverMachineGuiDocumentBridge.ApplyToMachineDocument(machine, shared);
        Check(machine.Gui.Bindings.Count == 1, "Shared-only GUI elements must not invent runtime Machine bindings.");
        Check(machine.Gui.Bindings[0].Properties.TryGetValue("futureBindingProperty", out var future) &&
              future.GetProperty("keep").GetString() == "yes",
            "Existing Machine binding properties must survive shared-document projection.");
        Check(machine.ExtensionData?.ContainsKey(ForeverMachineGuiDocumentBridge.ExtensionKey) == true,
            "Machine Project must persist the complete shared GUI document in extension data.");

        // Simulate the existing Machine GUI editor changing runtime-authoritative geometry after the bridge was written.
        machine.Gui.Bindings[0].Properties["x"] = Json(44);
        machine.Gui.Bindings[0].Properties["label"] = Json("Machine editor changed this");
        var recovered = ForeverMachineGuiDocumentBridge.FromMachineDocument(machine);
        var recoveredBound = recovered.Elements.Single(element => element.Id == "input_widget");
        Check(recoveredBound.X == 44 && recoveredBound.Text == "Machine editor changed this",
            "Current Machine binding geometry/presentation must override stale bridge values.");
        Check(recoveredBound.LayerId == "machine_layer" && recoveredBound.StyleId == "machine_style" &&
              recoveredBound.EffectIds.SequenceEqual(["machine_hover"]) && recoveredBound.StateIds.SequenceEqual(["machine_idle"]),
            "Machine Builder round-trip must preserve shared layer/style/effect/state references.");
        Check(recovered.Elements.Any(element => element.Id == "static_label" && element.Text == "Shared-only label"),
            "Shared-only elements must survive a Machine Project round-trip even before runtime compilation supports them.");
        Check(recovered.Bindings.Single(binding => binding.Id == "input_widget").Properties.ContainsKey("sharedBindingOnly"),
            "Shared binding metadata not represented by the compact runtime projection must survive.");
    }

    private static Dictionary<string, JsonElement> Merge(Dictionary<string, JsonElement>? source, string key, JsonElement value)
    {
        var result = source?.ToDictionary(pair => pair.Key, pair => pair.Value.Clone(), StringComparer.Ordinal)
                     ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        result[key] = value;
        return result;
    }

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
