using System.Runtime.CompilerServices;
using System.Text.Json;
using ForeverGuiDesigner.Services;

internal static class UnifiedGuiRuntimePresentationContract
{
    [ModuleInitializer]
    internal static void Run()
    {
        var root = Path.Combine(Path.GetTempPath(), "forever studio gui runtime " + Guid.NewGuid().ToString("N"));
        try
        {
            var mainPath = Path.Combine(root, "mod", "src", "main", "java", "de", "tekatz", "foreverskyblock", "ForeverSkyblock.java");
            Directory.CreateDirectory(Path.GetDirectoryName(mainPath)!);
            File.WriteAllText(mainPath, """
package de.tekatz.foreverskyblock;
public final class ForeverSkyblock {
    void register() {
        RECIPE_SERIALIZERS.register(modBus);
    }
}
""");
            var texturePath = Path.Combine(root, "mod", "src", "main", "resources", "assets", "forever_skyblock", "textures", "block", "ascension_materials.png");
            Directory.CreateDirectory(Path.GetDirectoryName(texturePath)!);
            File.WriteAllBytes(texturePath, [0]);

            var load = ForeverStudioLibraryCatalog.Load(root);
            Check(load.Warnings.Count == 0, "Built-in Studio library must load: " + string.Join(" | ", load.Warnings));
            var housing = load.Catalog.Find("asset.housing", 1) ?? throw new InvalidOperationException("Missing asset.housing.");
            var machine = ForeverStudioMachineProjectService.CreateBlank("gui_runtime_contract", "GUI Runtime Contract");
            machine.Components.Add(ForeverStudioVisualAssemblyService.CreateComponent(housing, machine.Components));
            machine.Behavior.Nodes.AddRange([
                Node("input", "function.item-input", 100, 180),
                Node("processor", "function.standard-processor", 360, 180),
                Node("output", "function.item-storage", 620, 180)
            ]);
            machine.Behavior.Connections.Add(new ForeverStudioMachineBehaviorConnection
            {
                Id = "entry_processor",
                FromNodeId = "machine_entry",
                FromPortId = "flow",
                ToNodeId = "processor",
                ToPortId = "flow-in"
            });
            machine.Recipes.Add(new ForeverStudioMachineRecipeBinding
            {
                Id = "process",
                RecipeTypeId = "forever_skyblock:machine_processing",
                Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                {
                    ["processorNodeId"] = Json("processor"),
                    ["inputs"] = Json(new[] { new { item = "minecraft:iron_ingot", count = 1 } }),
                    ["outputs"] = Json(new[] { new { item = "minecraft:gold_ingot", count = 1 } }),
                    ["processingTimeTicks"] = Json(40),
                    ["energy"] = Json(new { fePerTick = 0, totalFe = 0 })
                }
            });
            machine.Gui.ScreenId = "gui_runtime_contract_screen";
            machine.Gui.Bindings.Add(new ForeverStudioMachineGuiBinding
            {
                Id = "progress_widget",
                LibraryItemId = "asset.screen",
                LibraryItemVersion = 1,
                SourceId = "processor",
                Properties = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase)
                {
                    ["widget"] = Json("progress-bar"),
                    ["sourcePortId"] = Json("progress"),
                    ["sourceType"] = Json("number"),
                    ["label"] = Json("Progress"),
                    ["x"] = Json(30), ["y"] = Json(35), ["width"] = Json(60), ["height"] = Json(8), ["order"] = Json(0)
                }
            });

            var shared = ForeverMachineGuiDocumentBridge.FromMachineDocument(machine);
            var element = shared.Elements.Single();
            ForeverGuiStyleEffectService.Apply(shared, element, new ForeverGuiElementStyleSettings(
                Tint: "#80FFFFFF", Opacity: 0.9,
                HoverEnabled: true, HoverScale: 1.08,
                GlowEnabled: true, GlowColor: "#4EB8BC",
                ShadowEnabled: true,
                PulseEnabled: true, PulseMinOpacity: 0.6, PulseDurationMs: 750,
                PressedOffsetEnabled: true, PressedOffsetX: 1, PressedOffsetY: 1,
                DisabledAppearanceEnabled: false, DisabledOpacity: 0.45,
                StateSwitchEnabled: false, StateSwitchStateId: string.Empty));
            ForeverMachineGuiDocumentBridge.ApplyToMachineDocument(machine, shared);
            _ = ForeverStudioMachineProjectService.Serialize(machine);

            var plan = ForeverStudioMachineCompilerService.BuildPlan(root, machine, load.Catalog);
            Check(plan.CanApply, "Supported shared GUI presentation must compile: " + string.Join(" | ", plan.Issues.Select(issue => issue.Message)));
            var sidecarPath = "mod/src/main/resources/data/forever_skyblock/forever_studio/gui/gui_runtime_contract.json";
            var sidecar = plan.Changes.Single(change => change.RelativePath.Equals(sidecarPath, StringComparison.OrdinalIgnoreCase));
            using (var json = JsonDocument.Parse(sidecar.NewText))
            {
                var presentation = json.RootElement.GetProperty("elements").GetProperty("progress_widget");
                Check(presentation.GetProperty("style").GetProperty("opacity").GetDouble() == 0.9,
                    "Runtime GUI sidecar must preserve shared style opacity.");
                var effects = presentation.GetProperty("effects").EnumerateArray().Select(effect => effect.GetProperty("kind").GetString()).ToArray();
                Check(effects.Contains(ForeverGuiStyleEffectService.HoverKind) &&
                      effects.Contains(ForeverGuiStyleEffectService.GlowKind) &&
                      effects.Contains(ForeverGuiStyleEffectService.ShadowKind) &&
                      effects.Contains(ForeverGuiStyleEffectService.PulseKind) &&
                      effects.Contains(ForeverGuiStyleEffectService.PressedOffsetKind),
                    "Runtime GUI sidecar must preserve every supported shared effect kind.");
            }
            var manifest = plan.Changes.Single(change => change.RelativePath.EndsWith("forever_studio/generated/gui_runtime_contract.json", StringComparison.Ordinal));
            Check(manifest.NewText.Contains(sidecarPath, StringComparison.Ordinal),
                "Runtime ownership manifest must own the GUI presentation sidecar.");

            var blocked = ForeverMachineGuiDocumentBridge.FromMachineDocument(machine);
            var blockedElement = blocked.Elements.Single();
            var current = ForeverGuiStyleEffectService.Read(blocked, blockedElement);
            ForeverGuiStyleEffectService.Apply(blocked, blockedElement, current with { DisabledAppearanceEnabled = true });
            ForeverMachineGuiDocumentBridge.ApplyToMachineDocument(machine, blocked);
            var blockedPlan = ForeverStudioMachineCompilerService.BuildPlan(root, machine, load.Catalog);
            Check(!blockedPlan.CanApply && blockedPlan.Issues.Any(issue =>
                    issue.Severity == StudioValidationSeverity.Error && issue.Message.Contains("Disabled Appearance", StringComparison.Ordinal)),
                "Ambiguous Disabled Appearance runtime semantics must fail closed instead of being ignored.");
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    private static ForeverStudioMachineBehaviorNode Node(string id, string libraryItemId, double x, double y)
        => new() { Id = id, LibraryItemId = libraryItemId, LibraryItemVersion = 1, X = x, Y = y };

    private static JsonElement Json(object value) => JsonSerializer.SerializeToElement(value);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
