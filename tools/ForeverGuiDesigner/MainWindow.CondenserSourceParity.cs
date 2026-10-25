using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Media;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Source recipe bridge for the Condenser preview. The Minecraft screen derives its
/// visible CondenserJob from the recipe resolved for the current inputs, not from the
/// attached module or machine tier. The desktop editor has no live ContainerMenu, so
/// it deterministically selects a real repository recipe compatible with the selected
/// tier/mode and drives the source-parity scene from that recipe's data.
/// </summary>
public partial class MainWindow
{
    private string _condenserSourceJob = "crushing";
    private string? _condenserSourceInput;
    private string? _condenserSourceSecondary;
    private string? _condenserSourceResult;
    private string _condenserSourceRecipeSummary = "Wood → Stone Fragments";
    private double _condenserSourceRecipeDurationSeconds = 5;
    private string? _condenserSourceAssetRoot;
    private readonly Dictionary<string, ImageSource> _condenserSourceItemCache = new(StringComparer.OrdinalIgnoreCase);

    private void RefreshCondenserSourceRecipePreview()
    {
        if (ActiveRuntimeScreen != "CondenserScreen")
            return;

        _condenserSourceJob = CurrentMode == "mixing" ? "mixing" : "crushing";
        _condenserSourceInput = null;
        _condenserSourceSecondary = null;
        _condenserSourceResult = null;
        _condenserSourceRecipeSummary = _machineRuntimeProfile?.Recipe ?? "Wood → Stone Fragments";
        _condenserSourceRecipeDurationSeconds = Math.Max(.05, _machineRuntimeProfile?.DurationSeconds ?? 5);

        if (!string.Equals(_condenserSourceAssetRoot, _repoRoot, StringComparison.OrdinalIgnoreCase))
        {
            _condenserSourceAssetRoot = _repoRoot;
            _condenserSourceItemCache.Clear();
        }

        if (CurrentMode == "garden" || string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot))
            return;

        var directory = Path.Combine(_repoRoot, "mod", "src", "main", "resources", "data", "forever_skyblock", "recipe", "condensing");
        if (!Directory.Exists(directory))
            return;

        var tier = CurrentTier == "mk2" ? 2 : 1;
        var needsSecondary = CurrentMode == "mixing";
        var langPath = Path.Combine(_repoRoot, "mod", "src", "main", "resources", "assets", "forever_skyblock", "lang", "en_us.json");
        var language = ReadLanguage(langPath);

        foreach (var path in Directory.EnumerateFiles(directory, "*.json").OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var root = document.RootElement;
                var minimumTier = root.TryGetProperty("minimum_tier", out var tierElement) && tierElement.TryGetInt32(out var parsedTier)
                    ? Math.Max(1, parsedTier)
                    : 1;
                if (minimumTier > tier)
                    continue;

                var input = ReadCondenserResourceId(root, "ingredient");
                var secondary = ReadCondenserResourceId(root, "secondary_ingredient");
                // CondenserRecipe.Serializer uses ItemStack.EMPTY when result is omitted;
                // CondenserRecipe.result() then produces Stone Fragments. Mirror that here.
                var result = ReadCondenserResourceId(root, "result") ?? "forever_skyblock:stone_fragment";
                var hasSecondary = !string.IsNullOrWhiteSpace(secondary);
                if (hasSecondary != needsSecondary)
                    continue;

                // The recipe codec's DEFAULT_TIME is 160 ticks when processing_time is omitted.
                var ticks = root.TryGetProperty("processing_time", out var timeElement) && timeElement.TryGetInt32(out var parsedTicks)
                    ? Math.Max(1, parsedTicks)
                    : 160;

                _condenserSourceInput = input;
                _condenserSourceSecondary = secondary;
                _condenserSourceResult = result;
                _condenserSourceJob = ClassifyCondenserSourceJob(result, hasSecondary);
                _condenserSourceRecipeDurationSeconds = ticks / 20.0;

                var inputName = SourceResourceName(input, language, "Input");
                var outputName = SourceResourceName(result, language, "Output");
                _condenserSourceRecipeSummary = hasSecondary
                    ? $"{inputName} + {SourceResourceName(secondary, language, "Input")} → {outputName}"
                    : $"{inputName} → {outputName}";

                // MachineRuntimeDurationSeconds and SynchronizeMachineRuntimeText already
                // consume this profile. Updating the record here keeps playback speed,
                // timeline text and recipe text on the same source recipe.
                if (_machineRuntimeProfile is not null)
                    _machineRuntimeProfile = _machineRuntimeProfile with
                    {
                        Recipe = _condenserSourceRecipeSummary,
                        DurationSeconds = _condenserSourceRecipeDurationSeconds
                    };
                return;
            }
            catch
            {
                // A malformed optional recipe must not make the designer unusable.
                // Continue to the next real source recipe and retain the safe fallback.
            }
        }
    }

    private static string? ReadCondenserResourceId(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var element))
            return null;
        if (element.ValueKind == JsonValueKind.String)
            return element.GetString();
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        if (element.TryGetProperty("item", out var item) && item.ValueKind == JsonValueKind.String)
            return item.GetString();
        if (element.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
            return id.GetString();
        if (element.TryGetProperty("tag", out var tag) && tag.ValueKind == JsonValueKind.String)
            return "#" + tag.GetString();
        return null;
    }

    private static string ClassifyCondenserSourceJob(string resultId, bool hasSecondary)
    {
        if (hasSecondary)
        {
            var path = resultId[(resultId.IndexOf(':') + 1)..];
            return path.EndsWith("_sapling", StringComparison.OrdinalIgnoreCase) ? "cultivating" : "mixing";
        }
        if (string.Equals(resultId, "forever_skyblock:stone_meal", StringComparison.OrdinalIgnoreCase))
            return "milling";
        if (string.Equals(resultId, "minecraft:bone_meal", StringComparison.OrdinalIgnoreCase))
            return "composting";
        return "crushing";
    }

    private static string SourceResourceName(string? resourceId, IReadOnlyDictionary<string, string> language, string fallback)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            return fallback;
        var id = resourceId.TrimStart('#');
        var split = id.Split(':', 2);
        if (split.Length != 2)
            return id;
        foreach (var prefix in new[] { "item", "block" })
        {
            if (language.TryGetValue($"{prefix}.{split[0]}.{split[1]}", out var translated) && !string.IsNullOrWhiteSpace(translated))
                return translated;
        }
        return string.Join(' ', split[1].Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
    }

    private string CondenserSourceStatusText()
    {
        if (CurrentMachineRuntimeState == "blocked")
            return CurrentMode == "garden" ? "HARVEST FULL" : "OUTPUT FULL";
        if (CurrentMachineRuntimeState != "running")
            return CurrentMode == "garden" ? "GARDEN READY" : "IDLE";
        if (CurrentMode == "garden")
            return "GROWING";
        return _condenserSourceJob.ToUpperInvariant();
    }

    private void UpdateCondenserSourceDynamicText()
    {
        if (ActiveRuntimeScreen != "CondenserScreen" || !IsExclusiveNonWashingMachineRuntimeActive)
            return;

        SetAndRenderRuntimeText(MachineRuntimePrefix + "status_text", CondenserSourceStatusText());
        if (CurrentMode != "garden")
        {
            SetAndRenderRuntimeText(MachineRuntimePrefix + "recipe_summary", _condenserSourceRecipeSummary);
            SetAndRenderRuntimeText(MachineRuntimePrefix + "time_text", RuntimeTimeText());
        }
        else
        {
            SetAndRenderRuntimeText(MachineRuntimePrefix + "garden_time", RuntimeTimeText());
        }
    }

    private void RebuildNonWashingSourceParityOverlayWithRecipes()
    {
        RemoveMachineRuntimePolishOverlay();
        if (!IsExclusiveNonWashingMachineRuntimeActive)
            return;

        UpdateNonWashingSourceParityDynamicText();
        UpdateCondenserSourceDynamicText();

        var canvas = new Canvas
        {
            Tag = MachineRuntimePolishTag,
            Width = _layout.Width,
            Height = _layout.Height,
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        RenderOptions.SetEdgeMode(canvas, EdgeMode.Aliased);
        Panel.SetZIndex(canvas, -940);

        DrawSourceFrame(canvas, SourceFrameAccent());
        DrawHeaderIcon(canvas);
        switch (ActiveRuntimeScreen)
        {
            case "CrucibleScreen": DrawCrucibleParity(canvas); break;
            case "RefiningScreen": DrawRefiningParity(canvas); break;
            case "AscensionScreen": DrawAscensionParity(canvas); break;
            case "CondenserScreen": DrawCondenserRecipeParity(canvas); break;
        }
        DrawSourceInventory(canvas);
        PreviewCanvas.Children.Insert(0, canvas);
    }

    private void DrawCondenserRecipeParity(Canvas c)
    {
        DrawStatusDot(c, StatusColor());
        if (CurrentMode == "garden")
        {
            DrawSourcePanel(c, 42, 31, 164, 12, "#315B45");
            DrawGardenParity(c);
            DrawSourcePanel(c, 18, 183, 212, 16, "#466252");
            AddOverlayText(c, "GARDEN OUTPUT ROUTES", 22, 185, 204, 7, .60, "center", "#D0E7D4");
            AddOverlayText(c, "Outputs stay separated by route", 22, 192, 204, 7, .60, "center", "#B4D1BC");
            DrawCondenserGardenRouteIcon(c, 38, 172, "forever_skyblock:condenser_mark_ii");
            DrawCondenserGardenRouteIcon(c, 210, 172, "forever_skyblock:garden_chamber_mark_i");
        }
        else
        {
            DrawSourcePanel(c, 42, 31, 164, 12, "#365A66");
            DrawSourcePanel(c, 14, 44, 220, 64, "#54616D");
            DrawCondenserRecipeProcess(c);
            DrawSourcePanel(c, 14, 113, 220, 43, "#465A68");
            if (CurrentMode == "freezing")
            {
                DrawSourcePanel(c, 62, 132, 124, 8, "#4E6879");
                DrawProgressBar(c, 64, 134, 120, 4, _phase8TimelineProgress, "#76D8D2", "#4E6879");
                if (CurrentMachineRuntimeState == "running")
                {
                    var travel = (_phase8TimelineProgress * 4 % 1) * 115;
                    AddPolishRect(c, 65 + travel, 134, 3, 4, "#E1F7FF");
                }
            }
            DrawSourcePanel(c, 18, 187, 212, 12, "#5D6875");
        }

        DrawSourcePanel(c, 18, 203, 212, 51, CurrentTier == "mk2" ? "#4B5E6E" : "#3E454C");
        if (CurrentTier != "mk2")
            AddOverlayText(c, "MK II automation upgrades unavailable", 27, 222, 194, 8, .72, "center", "#A0B4BE");
        else
        {
            AddOverlayText(c, CurrentMode == "garden" ? "Primary" : "Main", 126, 216, 49, 7, .58, "center", "#A0B4BE");
            if (CurrentMode is "garden" or "freezing")
                AddOverlayText(c, CurrentMode == "garden" ? "Secondary" : "Freezing", 179, 216, 49, 7, .58, "center", "#A0B4BE");
            AddOverlayText(c, "S0  G0  B0", 22, 247, 96, 7, .62, "center", "#B7C8D4");
        }
    }

    private void DrawCondenserRecipeProcess(Canvas c)
    {
        var mixingLayout = CurrentMode == "mixing";
        var x = mixingLayout ? 111.0 : 83.0;
        var w = mixingLayout ? 70.0 : 98.0;
        var cx = x + w / 2;
        var phase = Math.Clamp(_phase8TimelineProgress, 0, 1);
        var running = CurrentMachineRuntimeState == "running";
        var blocked = CurrentMachineRuntimeState == "blocked";
        var sceneJob = CurrentMachineRuntimeState == "idle" ? "idle" : _condenserSourceJob;
        var combining = sceneJob is "mixing" or "composting" or "cultivating";
        var tint = blocked ? "#E9AF68" : sceneJob switch
        {
            "composting" or "cultivating" => "#8BD57C",
            "mixing" => "#D0AD79",
            "milling" => "#DDE5E9",
            _ => "#70D9E8"
        };

        AddPolishRect(c, x, 56, w, 22, "#10212D");
        AddPolishRect(c, x + 1, 77, w - 2, 1, "#576673");
        AddPolishRect(c, x + 2, 58, 1, 17, "#40515E");
        AddPolishRect(c, x + w - 3, 58, 1, 17, "#40515E");

        var motion = sceneJob == "idle" ? 0 : phase;
        if (combining)
        {
            AddPolishRect(c, cx - 11, 72, 23, 3, "#657580");
            AddPolishRect(c, cx - 9, 69, 19, 3, "#394D49");
            var arm = Math.Cos(motion * Math.PI * 6) * 8;
            AddPolishRect(c, cx, 59, 2, 13, "#BAC6C9");
            AddPolishRect(c, Math.Min(cx, cx + arm), 68, Math.Max(2, Math.Abs(arm) + 2), 2, tint);
        }
        else if (CurrentTier == "mk2")
        {
            var turns = sceneJob == "milling" ? 1440 : 720;
            AddGearParity(c, cx - 8, 67, motion * turns, tint);
            AddGearParity(c, cx + 8, 67, -motion * turns, tint);
        }
        else
        {
            var stroke = sceneJob == "idle" ? 0 : SmoothParity(.16, .48, phase) * (1 - SmoothParity(.64, .83, phase)) * 6;
            AddPolishRect(c, cx - 12, 74, 25, 2, "#778A98");
            AddPolishRect(c, cx - 1, 57, 3, 5 + stroke, "#9FB2B9");
            AddPolishRect(c, cx - 11, 61 + stroke, 23, 3, "#CED9D5");
            AddPolishRect(c, cx - 10, 64 + stroke, 21, 1, tint);
        }

        // Mirror CondenserProcessScene's continuous ingredient/product paths. These
        // images are loaded from the same repository assets and remain non-interactive.
        if (sceneJob == "idle")
        {
            AddCondenserSourceItem(c, _condenserSourceInput, cx, 70, .44);
        }
        else
        {
            var feed = SmoothParity(.02, .30, phase);
            var ingredientScale = .46 * SmoothParity(0, .06, phase) * (1 - SmoothParity(.38, .62, phase));
            var orbit = combining ? Math.Sin(motion * Math.PI * 6) * 1.4 * SmoothParity(.18, .32, phase) : 0;
            var target = cx - (combining ? 5 : 0) + orbit;
            AddCondenserSourceItem(c, _condenserSourceInput, x - 9 + (target - x + 9) * feed, 69, ingredientScale);

            if (sceneJob is "mixing" or "cultivating")
            {
                var secondPhase = phase - .035;
                var secondFeed = SmoothParity(.02, .30, secondPhase);
                var secondScale = .44 * SmoothParity(0, .06, secondPhase) * (1 - SmoothParity(.38, .62, secondPhase));
                AddCondenserSourceItem(c, _condenserSourceSecondary, x - 9 + (cx + 4 - orbit - x + 9) * secondFeed, 68, secondScale);
            }

            var product = sceneJob == "cultivating" ? SmoothParity(.44, .82, phase) : SmoothParity(.64, .82, phase);
            var exit = SmoothParity(.84, 1, phase);
            var resultX = cx + (w + 8 - w / 2) * exit;
            var resultY = 56 + (sceneJob == "cultivating" ? 7 + 6 * exit : 14);
            var resultScale = .46 * product * (1 - SmoothParity(.94, 1, phase));
            AddCondenserSourceItem(c, _condenserSourceResult, resultX, resultY, resultScale);
        }

        if (running)
        {
            for (var i = 0; i < 3; i++)
            {
                var fall = (phase * 5 + i * .23) % 1;
                var dust = SmoothParity(.38, .46, phase) * (1 - SmoothParity(.76, .84, phase)) * Math.Sin(Math.PI * fall);
                var alpha = (int)Math.Round(210 * Math.Max(0, dust));
                if (alpha > 0)
                    AddPolishRect(c, cx - 7 + i * 6, 70 + Math.Round(fall * 5), 1, 1, $"#{alpha:X2}{tint[1..]}");
            }
        }

        // Port lips are intentionally drawn after item images, matching the raised
        // source layer that hides route endpoints in CondenserProcessScene.
        AddPolishRect(c, x, 61, 5, 14, "#536976");
        AddPolishRect(c, x + w - 5, 61, 5, 14, "#536976");
        AddPolishRect(c, x + 1, 64, 2, 8, "#9DB2BC");
        AddPolishRect(c, x + w - 3, 64, 2, 8, tint);
        if (blocked)
        {
            AddPolishRect(c, x + w - 8, 59, 2, 7, tint);
            AddPolishRect(c, x + w - 8, 68, 2, 2, tint);
        }

        DrawProgressBar(c, x + 1, 88, w - 2, 3, phase, tint, "#263844");
    }

    private void AddCondenserSourceItem(Canvas c, string? itemId, double centerX, double centerY, double scale)
    {
        if (scale <= .001 || string.IsNullOrWhiteSpace(itemId) || itemId.StartsWith('#') || string.IsNullOrWhiteSpace(_repoRoot))
            return;
        if (!_condenserSourceItemCache.TryGetValue(itemId, out var source))
        {
            var bitmap = new MinecraftAssetResolver(_repoRoot).LoadItemBitmap(itemId);
            if (bitmap is null)
                return;
            source = bitmap;
            _condenserSourceItemCache[itemId] = source;
        }

        var size = Math.Max(.1, 16 * scale);
        var image = new Image
        {
            Source = source,
            Width = size,
            Height = size,
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        Canvas.SetLeft(image, centerX - size / 2);
        Canvas.SetTop(image, centerY - size / 2);
        c.Children.Add(image);
    }

    private void DrawCondenserGardenRouteIcon(Canvas c, double centerX, double y, string itemId)
    {
        if (string.IsNullOrWhiteSpace(_repoRoot))
            return;
        if (!_condenserSourceItemCache.TryGetValue(itemId, out var source))
        {
            var bitmap = new MinecraftAssetResolver(_repoRoot).LoadItemBitmap(itemId);
            if (bitmap is null)
                return;
            source = bitmap;
            _condenserSourceItemCache[itemId] = source;
        }
        var image = new Image { Source = source, Width = 8, Height = 8, Stretch = Stretch.Uniform, SnapsToDevicePixels = true, IsHitTestVisible = false };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        Canvas.SetLeft(image, centerX - 11);
        Canvas.SetTop(image, y);
        c.Children.Add(image);
    }
}
