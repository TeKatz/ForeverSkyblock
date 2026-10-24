using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Source-parity layer for the exclusive non-Washing runtime owner.
/// Static model/text corrections are applied only on state/variant/generation changes;
/// dynamic scene chrome is rebuilt only by the existing one-percent performance buckets.
/// Washing remains owned by its dedicated renderer.
/// </summary>
public partial class MainWindow
{
    private void ApplyNonWashingSourceParityStatic()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive)
            return;

        SynchronizeMachineRuntimeText();

        switch (ActiveRuntimeScreen)
        {
            case "CrucibleScreen":
                SetMachineRuntimeText(MachineRuntimePrefix + "tank_text", "Fluid Tank detected");
                SetMachineRuntimeText(MachineRuntimePrefix + "freezer_text", "Freezing detected");
                SetMachineRuntimeText(MachineRuntimePrefix + "input_label", "INPUT");
                SetMachineRuntimeText(MachineRuntimePrefix + "output_label", "OUTPUT");
                SetMachineRuntimeText(MachineRuntimePrefix + "melt_label", "MELT");
                SetMachineRuntimeText(MachineRuntimePrefix + "freeze_label", "FREEZE");
                break;

            case "RefiningScreen":
                SetMachineRuntimeText(MachineRuntimePrefix + "material_label", "MATERIAL");
                SetMachineRuntimeText(MachineRuntimePrefix + "catalyst_label", "CATALYST");
                SetMachineRuntimeText(MachineRuntimePrefix + "output_label", "OUTPUT");
                SetMachineRuntimeText(MachineRuntimePrefix + "info_primary", "Consumes FE continuously");
                SetMachineRuntimeText(MachineRuntimePrefix + "info_secondary", "Materials & costs in JEI");
                break;

            case "AscensionScreen":
                SetMachineRuntimeText(MachineRuntimePrefix + "core_label", "FOREVER CORE");
                SetMachineRuntimeText(MachineRuntimePrefix + "arrow", "→");
                SetMachineRuntimeText(MachineRuntimePrefix + "reward", "+1 PP");
                SetMachineRuntimeText(MachineRuntimePrefix + "perk_label", "PERK POINT");
                SetMachineRuntimeText(MachineRuntimePrefix + "conversion", "1 Forever Core → 1 Perk Point");
                SetMachineRuntimeText(MachineRuntimePrefix + "power_text", "No power required");
                break;

            case "CondenserScreen":
                SetMachineRuntimeText(MachineRuntimePrefix + "input_label", "INPUT");
                SetMachineRuntimeText(MachineRuntimePrefix + "module_input_label", "MODULE INPUT");
                SetMachineRuntimeText(MachineRuntimePrefix + "output_label", "OUTPUT");
                SetMachineRuntimeText(MachineRuntimePrefix + "freezing_label", "FREEZING");
                SetMachineRuntimeText(MachineRuntimePrefix + "upgrade_label", CurrentTier == "mk2" ? "UPGRADES 0 / 3" : string.Empty);
                SetMachineRuntimeText(MachineRuntimePrefix + "policies_label", CurrentTier == "mk2" ? "OUTPUT POLICIES" : string.Empty);
                SetMachineRuntimeText(MachineRuntimePrefix + "garden_specimen", "SPECIMEN");
                SetMachineRuntimeText(MachineRuntimePrefix + "garden_harvest", "HARVEST");
                SetMachineRuntimeText(MachineRuntimePrefix + "garden_routes", "GARDEN OUTPUT ROUTES");
                break;
        }

        UpdateNonWashingSourceParityDynamicText();
    }

    private void UpdateNonWashingSourceParityDynamicText()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive)
            return;

        switch (ActiveRuntimeScreen)
        {
            case "CrucibleScreen":
            {
                var lava = PreviewCrucibleLavaMb();
                SetAndRenderRuntimeText(MachineRuntimePrefix + "lava_text", $"{lava} mB");
                break;
            }
            case "RefiningScreen":
            {
                var ratio = PreviewRefiningEnergyRatio();
                SetAndRenderRuntimeText(MachineRuntimePrefix + "energy_text", $"{Math.Round(10000 * ratio):0} / 10000 FE");
                break;
            }
            case "CondenserScreen" when CurrentMode == "freezing":
            {
                var items = Math.Clamp((int)Math.Round(_phase8TimelineProgress * 40), 0, 40);
                SetAndRenderRuntimeText(MachineRuntimePrefix + "freezing_text", $"{items} / 40");
                break;
            }
        }
    }

    private void SetAndRenderRuntimeText(string id, string value)
    {
        var model = _layout.Elements.FirstOrDefault(element => string.Equals(element.Id, id, StringComparison.OrdinalIgnoreCase));
        if (model is null)
            return;
        model.Content = value;
        if (_visuals.TryGetValue(model, out var visual) && visual is Border border && model.EditorTextOverride is null)
            ApplyMachineRuntimeText(model, border);
    }

    private int PreviewCrucibleLavaMb()
    {
        var p = Math.Clamp(_phase8TimelineProgress, 0, 1);
        if (CurrentMachineRuntimeState == "lava_full") return 4000;
        if (CurrentMachineRuntimeState == "freezing") return (int)Math.Round(3000 - p * 1000);
        if (CurrentMachineRuntimeState.StartsWith("melting_", StringComparison.Ordinal)) return (int)Math.Round(1000 + p * 1000);
        return 2000;
    }

    private double PreviewRefiningEnergyRatio()
        => CurrentMachineRuntimeState switch
        {
            "no_power" => 0,
            "running" => Math.Clamp(.72 - _phase8TimelineProgress * .24, .42, .72),
            "blocked" => .48,
            _ => .58
        };

    private void RebuildNonWashingSourceParityOverlay()
    {
        RemoveMachineRuntimePolishOverlay();
        if (!IsExclusiveNonWashingMachineRuntimeActive)
            return;

        UpdateNonWashingSourceParityDynamicText();

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
            case "CondenserScreen": DrawCondenserParity(canvas); break;
        }
        DrawSourceInventory(canvas);
        PreviewCanvas.Children.Insert(0, canvas);
    }

    private void DrawCrucibleParity(Canvas c)
    {
        var melting = CurrentMachineRuntimeState.StartsWith("melting_", StringComparison.Ordinal);
        var freezing = CurrentMachineRuntimeState == "freezing";
        var phase = Math.Clamp(_phase8TimelineProgress, 0, 1);

        DrawStatusDot(c, StatusColor());
        DrawSourcePanel(c, 18, 35, 212, 18, CurrentMachineRuntimeState == "no_heat" ? "#4B4F54" : "#5A4536");
        DrawSourcePanel(c, 18, 58, 103, 18, "#355B66");
        DrawSourcePanel(c, 127, 58, 103, 18, "#355B66");
        DrawSourcePanel(c, 18, 81, 212, 70, "#54616D");

        if (melting || freezing)
        {
            var pulse = .35 + .65 * Math.Sin(phase * Math.PI * 8) * Math.Sin(phase * Math.PI * 8);
            AddPolishRect(c, 175, 10, 8, 8, $"#{(int)Math.Round(34 + 51 * pulse):X2}4DEBFF");
        }

        DrawSourcePanel(c, 111, 95, 26, 32, "#3D454B");
        var lavaRatio = PreviewCrucibleLavaMb() / 4000.0;
        var lavaHeight = Math.Clamp(28 * lavaRatio, 0, 28);
        if (lavaHeight > 0)
        {
            var lavaTop = 125 - lavaHeight;
            AddPolishRect(c, 113, lavaTop, 22, lavaHeight, "#FF6A20");
            AddPolishRect(c, 114 + Math.Floor(phase * 15) % 3, lavaTop, 20, 1, "#FFB34D");
            if (melting)
            {
                for (var i = 0; i < 3; i++)
                {
                    var travel = (phase * 44 + i * 9) % Math.Max(2, lavaHeight + 5);
                    var by = 123 - travel;
                    if (by >= lavaTop + 1 && by < 124)
                        AddPolishRect(c, 116 + i * 6, by, 1, 1, i == 1 ? "#FFE1A8" : "#FFC26A");
                }
                for (var i = 0; i < 3; i++)
                {
                    var rise = (phase * 36 + i * 4) % 6;
                    AddPolishRect(c, 116 + i * 7, 93 - rise, 1, 2, i == 1 ? "#66FFB04A" : "#44FF7A2A");
                }
            }
            if (freezing)
            {
                AddPolishRect(c, 113, lavaTop, 3, 1, "#99DFF9FF");
                AddPolishRect(c, 132, lavaTop + 1, 3, 1, "#99DFF9FF");
                for (var i = 0; i < 4; i++)
                {
                    var px = 109 + (phase * 28 + i * 8) % 30;
                    var py = 98 + (phase * 18 + i * 5) % 25;
                    if (px < 113 || px >= 135)
                        AddPolishRect(c, px, py, 1, 1, i % 2 == 0 ? "#E1F7FF" : "#76D8D2");
                }
            }
        }

        DrawProgressBar(c, 46, 141, 64, 4, melting ? phase : 0, "#FF6A20", "#637888");
        DrawProgressBar(c, 138, 141, 64, 4, freezing ? phase : 0, "#76D8D2", "#637888");
        if (melting) DrawSoftSweepParity(c, 46, 141, 64 * phase, phase, "#33FFD29A", "#66FFD08A", "#FFE0AE");
        if (freezing) DrawSoftSweepParity(c, 138, 141, 64 * phase, (phase + .25) % 1, "#334DEBFF", "#668DEEFF", "#E1F7FF");
    }

    private void DrawRefiningParity(Canvas c)
    {
        var phase = Math.Clamp(_phase8TimelineProgress, 0, 1);
        var running = CurrentMachineRuntimeState == "running";
        var blocked = CurrentMachineRuntimeState is "blocked" or "no_power";
        var tint = blocked ? "#E4B47C" : "#BCA4F3";

        DrawStatusDot(c, StatusColor());
        DrawSourcePanel(c, 18, 35, 212, 20, "#465A68");
        DrawProgressBar(c, 23, 49, 202, 3, PreviewRefiningEnergyRatio(), "#BCA4F3", "#2D3843");
        DrawSourcePanel(c, 18, 60, 212, 72, "#54616D");

        AddPolishGradient(c, 111, 75, 50, 27, "#121E30", "#273047");
        AddRotatingRing(c, 136, 88, 18, 18, "#6E80A2", 1, phase * 360 + 45);
        AddRotatingRing(c, 136, 88, 8, 8, tint, 1, -phase * 360 + 45);
        AddPolishRect(c, 134, 86, 4, 4, running ? "#E6DCFF" : "#7F8CA7");
        if (running)
        {
            for (var i = 0; i < 3; i++)
            {
                var t = (phase * 8 + i / 3.0) % 1;
                var alpha = (int)Math.Round(Math.Sin(Math.PI * t) * 220);
                if (alpha > 0) AddPolishRect(c, 113 + Math.Round(t * 46), 99, 1, 1, $"#{alpha:X2}{tint[1..]}");
            }
        }
        DrawProgressBar(c, 111, 115, 50, 3, phase, "#BCA4F3", "#07141E");
        DrawSourcePanel(c, 18, 138, 212, 34, "#465A68");
    }

    private void DrawAscensionParity(Canvas c)
    {
        var busy = CurrentMachineRuntimeState == "processing";
        var phase = busy ? Math.Clamp(_phase8TimelineProgress, 0, 1) : 0;

        DrawSourcePanel(c, 190, 9, 40, 15, "#615372");
        DrawSourcePanel(c, 149, 46, 70, 41, "#8E4FD2");
        DrawSourcePanel(c, 23, 106, 146, 8, "#697487");
        DrawProgressBar(c, 25, 108, 142, 4, phase, "#BCA4F3", "#697487");
        DrawSourcePanel(c, 20, 120, 208, 17, "#576073");

        AddRotatingRing(c, 62, 66, 36, 36, "#6F7C8D", 2, phase * 360);
        AddRotatingRing(c, 62, 66, 26, 26, "#6E449A", 2, -phase * 360);
        if (busy)
        {
            AddPolishRect(c, 68, 72, 7, 2, "#07141E");
            AddPolishRect(c, 72, 70, 2, 6, "#07141E");
        }

        // PlayerFaceRenderer footprint. Keep it below editable recipient/balance text.
        AddPolishRect(c, 24, 123, 11, 11, "#49372D");
        AddPolishRect(c, 25, 124, 9, 4, "#C99B76");
        AddPolishRect(c, 26, 126, 2, 1, "#E8E8E8");
        AddPolishRect(c, 31, 126, 2, 1, "#E8E8E8");
    }

    private void DrawCondenserParity(Canvas c)
    {
        DrawStatusDot(c, StatusColor());
        if (CurrentMode == "garden")
        {
            DrawSourcePanel(c, 42, 31, 164, 12, "#315B45");
            DrawGardenParity(c);
            DrawSourcePanel(c, 18, 183, 212, 16, "#466252");
            AddOverlayText(c, "GARDEN OUTPUT ROUTES", 22, 185, 204, 7, .60, "center", "#D0E7D4");
            AddOverlayText(c, "Outputs stay separated by route", 22, 192, 204, 7, .60, "center", "#B4D1BC");
        }
        else
        {
            DrawSourcePanel(c, 42, 31, 164, 12, "#365A66");
            DrawSourcePanel(c, 14, 44, 220, 64, "#54616D");
            DrawCondenserProcessParity(c);
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

    private void DrawCondenserProcessParity(Canvas c)
    {
        var mixing = CurrentMode == "mixing";
        var x = mixing ? 111.0 : 83.0;
        var w = mixing ? 70.0 : 98.0;
        var cx = x + w / 2;
        var phase = Math.Clamp(_phase8TimelineProgress, 0, 1);
        var running = CurrentMachineRuntimeState == "running";
        var blocked = CurrentMachineRuntimeState == "blocked";
        var job = mixing ? "mixing" : CurrentTier == "mk2" ? "milling" : "crushing";
        var tint = blocked ? "#E9AF68" : job switch { "mixing" => "#D0AD79", "milling" => "#DDE5E9", _ => "#70D9E8" };

        AddPolishRect(c, x, 56, w, 22, "#10212D");
        AddPolishRect(c, x + 1, 77, w - 2, 1, "#576673");
        AddPolishRect(c, x + 2, 58, 1, 17, "#40515E");
        AddPolishRect(c, x + w - 3, 58, 1, 17, "#40515E");

        if (mixing)
        {
            AddPolishRect(c, cx - 11, 72, 23, 3, "#657580");
            AddPolishRect(c, cx - 9, 69, 19, 3, "#394D49");
            var arm = Math.Cos(phase * Math.PI * 6) * 8;
            AddPolishRect(c, cx, 59, 2, 13, "#BAC6C9");
            AddPolishRect(c, Math.Min(cx, cx + arm), 68, Math.Max(2, Math.Abs(arm) + 2), 2, tint);
        }
        else if (CurrentTier == "mk2")
        {
            AddGearParity(c, cx - 8, 67, phase * 1440, tint);
            AddGearParity(c, cx + 8, 67, -phase * 1440, tint);
        }
        else
        {
            var stroke = SmoothParity(.16, .48, phase) * (1 - SmoothParity(.64, .83, phase)) * 6;
            AddPolishRect(c, cx - 12, 74, 25, 2, "#778A98");
            AddPolishRect(c, cx - 1, 57, 3, 5 + stroke, "#9FB2B9");
            AddPolishRect(c, cx - 11, 61 + stroke, 23, 3, "#CED9D5");
            AddPolishRect(c, cx - 10, 64 + stroke, 21, 1, tint);
        }

        if (running)
        {
            for (var i = 0; i < 3; i++)
            {
                var fall = (phase * 5 + i * .23) % 1;
                var dust = SmoothParity(.38, .46, phase) * (1 - SmoothParity(.76, .84, phase)) * Math.Sin(Math.PI * fall);
                var alpha = (int)Math.Round(210 * Math.Max(0, dust));
                if (alpha > 0) AddPolishRect(c, cx - 7 + i * 6, 70 + Math.Round(fall * 5), 1, 1, $"#{alpha:X2}{tint[1..]}");
            }
        }

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

    private void DrawGardenParity(Canvas c)
    {
        var phase = Math.Clamp(_phase8TimelineProgress, 0, 1);
        var running = CurrentMachineRuntimeState == "running";
        const double left = 20, right = 228, top = 50, ground = 112;

        DrawSourcePanel(c, 14, 44, 220, 138, "#335544");
        AddPolishRect(c, left, top, right-left, 18, "#182F3B");
        AddPolishRect(c, left, 68, right-left, 20, "#234753");
        AddPolishRect(c, left, 88, right-left, 12, "#305849");
        AddPolishRect(c, left, 96, 52, 9, "#355F48");
        AddPolishRect(c, 58, 91, 66, 14, "#3B684C");
        AddPolishRect(c, 112, 94, right-112, 11, "#315B45");
        AddPolishRect(c, left, 103, right-left, 9, "#47763D");
        AddPolishRect(c, left, ground, right-left, 5, "#4B4033");
        AddPolishRect(c, left, 103, right-left, 3, "#6B9A4D");
        AddPolishRect(c, 199, 56, 16, 16, "#556F9B72");
        AddPolishRect(c, 202, 59, 10, 10, "#E8CF72");
        AddPolishRect(c, 204, 61, 6, 6, "#FFE29A");

        var lamp = left + 8 + (right-left-33) * phase;
        AddPolishRect(c, lamp, 54, 17, 2, "#98AAA0");
        AddPolishRect(c, lamp + 2, 56, 13, 1, running ? "#D8F5A4" : "#60776B");

        var growth = SmoothParity(.04, .84, phase);
        var height = 3 + 31 * growth;
        AddPolishRect(c, 123, 104-height, 3, height, "#68452B");
        if (growth < .10)
        {
            AddPolishRect(c, 121, 103-height, 3, 3, "#5D9A4B");
            AddPolishRect(c, 125, 101-height, 3, 3, "#83BF64");
        }
        else
        {
            var r = 3 + 13 * growth;
            var cy = 106-height;
            AddPolishRect(c, 124-r, cy-r/2, r*2, Math.Max(3, r), "#275E36");
            AddPolishRect(c, 127-r, cy+1, Math.Max(3, r*2-5), Math.Max(2, r*.7), "#347744");
            AddPolishRect(c, 126-r, cy-r/2-2, Math.Max(3, r*2-6), Math.Max(2, r*.55), "#4C9250");
            AddPolishRect(c, 124-r/2, cy-r/2-4, r+2, 4, "#65A95A");
        }

        var harvest = SmoothParity(.86, 1, phase);
        if (running && harvest > 0)
        {
            var scanY = ground - 3 - harvest * 39;
            AddPolishRect(c, 94, scanY, 61, 1, "#CAF1A0");
        }

        DrawSourcePanel(c, 66, 122, 116, 9, "#456250");
        DrawProgressBar(c, 68, 124, 112, 5, phase, CurrentMachineRuntimeState == "blocked" ? "#FFBF76" : "#62C46B", "#456250");
        DrawGardenRouteParity(c, 38, 172, 1);
        DrawGardenRouteParity(c, 210, 172, 2);
    }

    private void DrawGardenRouteParity(Canvas c, double centerX, double y, int number)
    {
        AddPolishRect(c, centerX-12, y-1, 24, 10, "#294535");
        AddOverlayText(c, number.ToString(), centerX-2.5, y+1, 5, 7, .65, "center", "#D0E7D4");
        AddPolishRect(c, centerX+7, y+1, 1, 6, "#8FE59B");
        AddPolishRect(c, centerX+5, y+4, 5, 1, "#8FE59B");
    }

    private void AddGearParity(Canvas c, double cx, double cy, double angle, string tint)
    {
        var gear = new Canvas
        {
            Width = 14,
            Height = 14,
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new RotateTransform(angle)
        };
        for (var i = 0; i < 4; i++)
        {
            var tooth = new Rectangle { Width = 3, Height = 12, Fill = Brush("#99B0B7"), IsHitTestVisible = false, RenderTransformOrigin = new Point(.5,.5), RenderTransform = new RotateTransform(i*45) };
            Canvas.SetLeft(tooth, 5.5); Canvas.SetTop(tooth, 1); gear.Children.Add(tooth);
        }
        AddPolishRect(gear, 3, 3, 8, 8, "#597787");
        AddPolishRect(gear, 4, 4, 6, 6, tint);
        AddPolishRect(gear, 6, 6, 2, 2, "#142734");
        Canvas.SetLeft(gear, cx-7); Canvas.SetTop(gear, cy-7); c.Children.Add(gear);
    }

    private static double SmoothParity(double from, double to, double phase)
    {
        if (to <= from) return phase >= to ? 1 : 0;
        var t = Math.Clamp((phase-from)/(to-from), 0, 1);
        return t*t*(3-2*t);
    }

    private static void DrawProgressBar(Canvas c, double x, double y, double width, double height, double ratio, string fill, string background)
    {
        AddPolishRect(c, x, y, width, height, background);
        AddPolishRect(c, x, y, width * Math.Clamp(ratio, 0, 1), height, fill);
    }

    private static void DrawSoftSweepParity(Canvas c, double x, double y, double width, double phase, string outer, string middle, string core)
    {
        if (width <= 0) return;
        var center = x - 7 + phase * (width + 14);
        var left = x;
        var right = x + width;
        static void Band(Canvas canvas, double l, double r, double yy, string color)
        {
            if (r > l) AddPolishRect(canvas, l, yy, r-l, 4, color);
        }
        Band(c, Math.Max(left, center-7), Math.Min(right, center+8), y, outer);
        Band(c, Math.Max(left, center-4), Math.Min(right, center+5), y, middle);
        Band(c, Math.Max(left, center-1), Math.Min(right, center+2), y, core);
    }
}
