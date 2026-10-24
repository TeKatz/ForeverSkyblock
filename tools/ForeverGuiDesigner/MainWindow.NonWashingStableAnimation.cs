using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Stable animation owner for Crucible, Refining, Ascension and Condenser.
///
/// The source-parity canvas remains the static presentation owner. This layer is
/// created only when the active machine/state/variant or editable visual generation
/// changes. During playback a frame mutates only existing WPF properties; it never
/// adds/removes preview children, never calls RenderLayout and never rewrites text,
/// buttons or slots. Washing keeps its dedicated animation owner.
/// </summary>
public partial class MainWindow
{
    private const string NonWashingStableAnimationTag = "__forever_non_washing_stable_animation_v1";

    private bool _nonWashingStableAnimationInitialized;
    private bool _nonWashingStableAnimationQueued;
    private Canvas? _nonWashingStableAnimationLayer;
    private FrameworkElement? _nonWashingStableAnimationVisualAnchor;
    private int _nonWashingStableAnimationVisualCount = -1;
    private string? _nonWashingStableAnimationSignature;
    private DateTime _nonWashingStableAnimationLastFrame = DateTime.MinValue;
    private readonly Dictionary<string, FrameworkElement> _nonWashingStableAnimationParts = new(StringComparer.Ordinal);

    public void EnableNonWashingStableAnimation()
    {
        if (_nonWashingStableAnimationInitialized)
            return;
        _nonWashingStableAnimationInitialized = true;

        CaptureNonWashingStableAnimationGeneration();

        if (_machineRuntimeStateCombo is not null)
            _machineRuntimeStateCombo.SelectionChanged += (_, _) => QueueNonWashingStableAnimationStructure();
        if (_foreverTierCombo is not null)
            _foreverTierCombo.SelectionChanged += (_, _) => QueueNonWashingStableAnimationStructure();
        if (_foreverModeCombo is not null)
            _foreverModeCombo.SelectionChanged += (_, _) => QueueNonWashingStableAnimationStructure();
        if (_foreverTemplateCombo is not null)
            _foreverTemplateCombo.SelectionChanged += (_, _) => QueueNonWashingStableAnimationStructure();
        if (_phase8TimelineSlider is not null)
            _phase8TimelineSlider.ValueChanged += (_, _) =>
            {
                if (!IsExclusiveNonWashingMachineRuntimeActive)
                    return;
                EnsureNonWashingStableAnimationStructure();
                UpdateNonWashingStableAnimationFrame();
            };

        if (SkyblockBarHost.Children.OfType<Button>().FirstOrDefault(button => Equals(button.Content, "Load machine")) is { } load)
            load.Click += (_, _) => QueueNonWashingStableAnimationStructure();
        ApplyButton.Click += (_, _) => QueueNonWashingStableAnimationStructure();

        if (_phase8TimelineTimer is not null)
            _phase8TimelineTimer.Tick += (_, _) => TickNonWashingStableAnimation();

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (!NonWashingStableAnimationGenerationChanged())
                return;
            CaptureNonWashingStableAnimationGeneration();
            QueueNonWashingStableAnimationStructure();
        };

        QueueNonWashingStableAnimationStructure();
    }

    private void TickNonWashingStableAnimation()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive)
        {
            if (_nonWashingStableAnimationLayer is not null)
                QueueNonWashingStableAnimationStructure();
            return;
        }

        EnsureNonWashingStableAnimationStructure();
        if (!_machineRuntimePlaybackRequested || !RuntimeStateRuns)
            return;

        var now = DateTime.UtcNow;
        if ((now - _nonWashingStableAnimationLastFrame).TotalMilliseconds < 45)
            return;
        _nonWashingStableAnimationLastFrame = now;
        UpdateNonWashingStableAnimationFrame();
    }

    private void QueueNonWashingStableAnimationStructure()
    {
        if (_nonWashingStableAnimationQueued)
            return;
        _nonWashingStableAnimationQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            _nonWashingStableAnimationQueued = false;
            EnsureNonWashingStableAnimationStructure();
            UpdateNonWashingStableAnimationFrame();
        }));
    }

    private void EnsureNonWashingStableAnimationStructure()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive)
        {
            RemoveNonWashingStableAnimationLayer();
            return;
        }

        var signature = string.Join('|',
            ActiveRuntimeScreen,
            CurrentTier,
            CurrentMode,
            CurrentMachineRuntimeState,
            _layout.Width,
            _layout.Height,
            _visuals.Count,
            RuntimeStateRuns);

        if (_nonWashingStableAnimationLayer is not null &&
            string.Equals(signature, _nonWashingStableAnimationSignature, StringComparison.Ordinal) &&
            PreviewCanvas.Children.Contains(_nonWashingStableAnimationLayer))
            return;

        RemoveNonWashingStableAnimationLayer();
        _nonWashingStableAnimationSignature = signature;
        _nonWashingStableAnimationParts.Clear();

        if (ActiveRuntimeScreen == "CondenserScreen")
            RefreshCondenserSourceRecipePreview();

        var layer = new Canvas
        {
            Tag = NonWashingStableAnimationTag,
            Width = _layout.Width,
            Height = _layout.Height,
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true,
            ClipToBounds = true
        };
        RenderOptions.SetEdgeMode(layer, EdgeMode.Aliased);
        Panel.SetZIndex(layer, -930);
        _nonWashingStableAnimationLayer = layer;

        switch (ActiveRuntimeScreen)
        {
            case "CrucibleScreen": BuildStableCrucibleAnimation(layer); break;
            case "RefiningScreen": BuildStableRefiningAnimation(layer); break;
            case "AscensionScreen": BuildStableAscensionAnimation(layer); break;
            case "CondenserScreen": BuildStableCondenserAnimation(layer); break;
        }

        PreviewCanvas.Children.Add(layer);
        CaptureNonWashingStableAnimationGeneration();
    }

    private void RemoveNonWashingStableAnimationLayer()
    {
        if (_nonWashingStableAnimationLayer is not null)
            PreviewCanvas.Children.Remove(_nonWashingStableAnimationLayer);
        foreach (var child in PreviewCanvas.Children.OfType<FrameworkElement>()
                     .Where(element => Equals(element.Tag, NonWashingStableAnimationTag)).ToList())
            PreviewCanvas.Children.Remove(child);
        _nonWashingStableAnimationLayer = null;
        _nonWashingStableAnimationSignature = null;
        _nonWashingStableAnimationParts.Clear();
    }

    private bool NonWashingStableAnimationGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _nonWashingStableAnimationVisualCount ||
               !ReferenceEquals(anchor, _nonWashingStableAnimationVisualAnchor);
    }

    private void CaptureNonWashingStableAnimationGeneration()
    {
        _nonWashingStableAnimationVisualCount = _visuals.Count;
        _nonWashingStableAnimationVisualAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }

    private double StableAnimationWorldTicks
        => Math.Max(0, _phase8TimelineProgress * MachineRuntimeDurationSeconds * 20.0);

    private void BuildStableCrucibleAnimation(Canvas layer)
    {
        var melting = CurrentMachineRuntimeState.StartsWith("melting_", StringComparison.Ordinal);
        var freezing = CurrentMachineRuntimeState == "freezing";

        // Cover only the process cutaway. Editable slots/text remain above this layer.
        DrawSourcePanel(layer, 18, 81, 212, 70, "#54616D");
        DrawSourcePanel(layer, 111, 95, 26, 32, "#3D454B");
        StableAnimRect(layer, "crucible_melt_bg", 46, 141, 64, 4, "#637888");
        StableAnimRect(layer, "crucible_freeze_bg", 138, 141, 64, 4, "#637888");
        StableAnimRect(layer, "crucible_melt_fill", 46, 141, 0, 4, "#FF6A20");
        StableAnimRect(layer, "crucible_freeze_fill", 138, 141, 0, 4, "#76D8D2");

        // Header glow is isolated from the rest of the frame and therefore safe to
        // cover independently. Redraw the status dot above it.
        StableAnimRect(layer, "crucible_glow_base", 175, 10, 8, 8, "#101D29");
        StableAnimRect(layer, "crucible_glow", 175, 10, 8, 8, "#004DEBFF");
        StableAnimRect(layer, "crucible_status_dot", 177, 12, 4, 4, StatusColor());

        StableAnimRect(layer, "crucible_lava", 113, 125, 22, 0, "#FF6A20");
        StableAnimRect(layer, "crucible_lava_top", 114, 125, 19, 1, "#FFB34D");
        for (var i = 0; i < 3; i++)
        {
            StableAnimRect(layer, $"crucible_bubble_{i}", 116 + i * 6, 123, 1, 1, i == 1 ? "#FFE1A8" : "#FFC26A");
            StableAnimRect(layer, $"crucible_heat_{i}", 116 + i * 7, 93, 1, 2, i == 1 ? "#66FFB04A" : "#44FF7A2A");
        }
        StableAnimRect(layer, "crucible_frost_left", 113, 125, 3, 1, "#99DFF9FF");
        StableAnimRect(layer, "crucible_frost_right", 132, 126, 3, 1, "#99DFF9FF");
        for (var i = 0; i < 4; i++)
            StableAnimRect(layer, $"crucible_frost_particle_{i}", 109, 98, 1, 1, i % 2 == 0 ? "#E1F7FF" : "#76D8D2");

        BuildStableSweep(layer, "crucible_melt_sweep", "#33FFD29A", "#66FFD08A", "#FFE0AE");
        BuildStableSweep(layer, "crucible_freeze_sweep", "#334DEBFF", "#668DEEFF", "#E1F7FF");

        if (!melting && !freezing)
            StableAnimPart<Rectangle>("crucible_glow")!.Visibility = Visibility.Collapsed;
    }

    private void UpdateStableCrucibleAnimation(double phase)
    {
        var melting = CurrentMachineRuntimeState.StartsWith("melting_", StringComparison.Ordinal);
        var freezing = CurrentMachineRuntimeState == "freezing";
        var time = StableAnimationWorldTicks;

        var glow = StableAnimPart<Rectangle>("crucible_glow");
        if (glow is not null)
        {
            glow.Visibility = melting || freezing ? Visibility.Visible : Visibility.Collapsed;
            if (glow.Fill is SolidColorBrush glowBrush)
            {
                var alpha = 0x22 + (int)Math.Round((.5 + .5 * Math.Sin(time * .18)) * 0x33);
                glowBrush.Color = Color.FromArgb((byte)Math.Clamp(alpha, 0, 255), 0x4D, 0xEB, 0xFF);
            }
        }

        var lavaMb = PreviewCrucibleLavaMb();
        var lavaHeight = Math.Min(28, 28 * lavaMb / 4000);
        var lavaTop = 125 - lavaHeight;
        SetStableAnimRect(StableAnimPart<Rectangle>("crucible_lava"), 113, lavaTop, 22, lavaHeight, lavaHeight > 0);
        var drift = PositiveModuloStable((int)Math.Floor(time / 5.0), 3);
        SetStableAnimRect(StableAnimPart<Rectangle>("crucible_lava_top"), 114 + drift, lavaTop, Math.Max(1, 19 - drift), 1, lavaHeight > 0);

        for (var i = 0; i < 3; i++)
        {
            var bubble = StableAnimPart<Rectangle>($"crucible_bubble_{i}");
            var travel = PositiveModuloStable((int)Math.Floor(time * 1.6) + i * 9, Math.Max(2, lavaHeight + 5));
            var by = 123 - travel;
            SetStableAnimRect(bubble, 116 + i * 6, by, 1, 1, melting && lavaHeight > 0 && by >= lavaTop + 1 && by < 124);

            var heat = StableAnimPart<Rectangle>($"crucible_heat_{i}");
            var rise = PositiveModuloStable((int)Math.Floor(time * 1.2) + i * 4, 6);
            SetStableAnimRect(heat, 116 + i * 7, 93 - rise, 1, 2, melting);
        }

        SetStableAnimRect(StableAnimPart<Rectangle>("crucible_frost_left"), 113, lavaTop, 3, 1, freezing && lavaHeight > 0);
        SetStableAnimRect(StableAnimPart<Rectangle>("crucible_frost_right"), 132, lavaTop + 1, 3, 1, freezing && lavaHeight > 0);
        for (var i = 0; i < 4; i++)
        {
            var px = 109 + PositiveModuloStable((int)Math.Floor(time * .8) + i * 8, 30);
            var py = 98 + PositiveModuloStable((int)Math.Floor(time * .6) + i * 5, 25);
            SetStableAnimRect(StableAnimPart<Rectangle>($"crucible_frost_particle_{i}"), px, py, 1, 1,
                freezing && (px < 113 || px >= 135));
        }

        var meltWidth = melting ? 64 * phase : 0;
        var freezeWidth = freezing ? 64 * phase : 0;
        SetStableAnimRect(StableAnimPart<Rectangle>("crucible_melt_fill"), 46, 141, meltWidth, 4, meltWidth > 0);
        SetStableAnimRect(StableAnimPart<Rectangle>("crucible_freeze_fill"), 138, 141, freezeWidth, 4, freezeWidth > 0);
        UpdateStableSweep("crucible_melt_sweep", 46, 141, meltWidth, PositiveModuloStable(time, 48) / 48.0, melting);
        UpdateStableSweep("crucible_freeze_sweep", 138, 141, freezeWidth, PositiveModuloStable(time + 12, 48) / 48.0, freezing);
    }

    private void BuildStableRefiningAnimation(Canvas layer)
    {
        AddPolishGradient(layer, 111, 75, 50, 27, "#121E30", "#273047");
        StableAnimRing(layer, "refining_outer", 136, 88, 9, 1, "#6E80A2");
        StableAnimRing(layer, "refining_inner", 136, 88, 5, 1,
            CurrentMachineRuntimeState is "blocked" or "no_power" ? "#E4B47C" : "#BCA4F3");
        StableAnimRect(layer, "refining_center", 134, 86, 4, 4,
            CurrentMachineRuntimeState == "running" ? "#E6DCFF" : "#7F8CA7");
        for (var i = 0; i < 3; i++)
            StableAnimRect(layer, $"refining_spark_{i}", 113, 99, 1, 1,
                CurrentMachineRuntimeState is "blocked" or "no_power" ? "#E4B47C" : "#BCA4F3");
        StableAnimRect(layer, "refining_progress_bg", 111, 115, 50, 3, "#07141E");
        StableAnimRect(layer, "refining_progress", 111, 115, 0, 3, "#BCA4F3");
    }

    private void UpdateStableRefiningAnimation(double phase)
    {
        SetStableAnimAngle("refining_outer", phase * 360 + 45);
        SetStableAnimAngle("refining_inner", -phase * 360 + 45);
        SetStableAnimRect(StableAnimPart<Rectangle>("refining_progress"), 111, 115, 50 * phase, 3, phase > 0);

        var running = CurrentMachineRuntimeState == "running";
        for (var i = 0; i < 3; i++)
        {
            var t = PositiveModuloStable(phase * 8 + i / 3.0, 1);
            var spark = StableAnimPart<Rectangle>($"refining_spark_{i}");
            SetStableAnimRect(spark, 113 + Math.Round(t * 46), 99, 1, 1, running);
            if (spark is not null)
                spark.Opacity = running ? Math.Max(0, Math.Sin(Math.PI * t)) : 0;
        }
    }

    private void BuildStableAscensionAnimation(Canvas layer)
    {
        // The rings live directly on the machine frame, so cover just their footprint
        // once. This patch never moves and the editable core slot stays above it.
        AddPolishGradient(layer, 42, 46, 40, 40, "#253746", "#15232F");
        StableAnimRing(layer, "ascension_outer", 62, 66, 18, 2, "#6F7C8D");
        StableAnimRing(layer, "ascension_inner", 62, 66, 13, 2, "#6E449A");
        if (CurrentMachineRuntimeState == "processing")
        {
            StableAnimRect(layer, "ascension_lock_a", 68, 72, 7, 2, "#07141E");
            StableAnimRect(layer, "ascension_lock_b", 72, 70, 2, 6, "#07141E");
        }
        DrawSourcePanel(layer, 23, 106, 146, 8, "#697487");
        StableAnimRect(layer, "ascension_progress", 25, 108, 0, 4, "#BCA4F3");
    }

    private void UpdateStableAscensionAnimation(double phase)
    {
        var busy = CurrentMachineRuntimeState == "processing";
        var p = busy ? phase : 0;
        SetStableAnimAngle("ascension_outer", p * 360);
        SetStableAnimAngle("ascension_inner", -p * 360);
        SetStableAnimRect(StableAnimPart<Rectangle>("ascension_progress"), 25, 108, 142 * p, 4, p > 0);
    }

    private void BuildStableCondenserAnimation(Canvas layer)
    {
        if (CurrentMode == "garden")
        {
            BuildStableGardenAnimation(layer);
            return;
        }

        var mixingLayout = CurrentMode == "mixing";
        var x = mixingLayout ? 111.0 : 83.0;
        var w = mixingLayout ? 70.0 : 98.0;
        var cx = x + w / 2;
        var blocked = CurrentMachineRuntimeState == "blocked";
        var sceneJob = CurrentMachineRuntimeState == "idle" ? "idle" : _condenserSourceJob;
        var combining = sceneJob is "mixing" or "composting" or "cultivating";
        var tint = blocked ? "#E9AF68" : StableCondenserTint(sceneJob);

        StableAnimRect(layer, "condenser_scene_bg", x, 56, w, 22, "#10212D");
        StableAnimRect(layer, "condenser_scene_floor", x + 1, 77, w - 2, 1, "#576673");
        StableAnimRect(layer, "condenser_scene_side_l", x + 2, 58, 1, 17, "#40515E");
        StableAnimRect(layer, "condenser_scene_side_r", x + w - 3, 58, 1, 17, "#40515E");

        if (combining)
        {
            StableAnimRect(layer, "condenser_basin_a", cx - 11, 72, 23, 3, "#657580");
            StableAnimRect(layer, "condenser_basin_b", cx - 9, 69, 19, 3, "#394D49");
            StableAnimRect(layer, "condenser_rod", cx, 59, 2, 13, "#BAC6C9");
            StableAnimRect(layer, "condenser_arm", cx, 68, 2, 2, tint);
        }
        else if (CurrentTier == "mk2")
        {
            StableAnimGear(layer, "condenser_gear_l", cx - 8, 67, tint);
            StableAnimGear(layer, "condenser_gear_r", cx + 8, 67, tint);
        }
        else
        {
            StableAnimRect(layer, "condenser_press_base", cx - 12, 74, 25, 2, "#778A98");
            StableAnimRect(layer, "condenser_press_rod", cx - 1, 57, 3, 5, "#9FB2B9");
            StableAnimRect(layer, "condenser_press_plate", cx - 11, 61, 23, 3, "#CED9D5");
            StableAnimRect(layer, "condenser_press_tint", cx - 10, 64, 21, 1, tint);
        }

        StableAnimImage(layer, "condenser_input", ResolveStableCondenserSource(_condenserSourceInput));
        StableAnimImage(layer, "condenser_secondary", ResolveStableCondenserSource(_condenserSourceSecondary));
        StableAnimImage(layer, "condenser_result", ResolveStableCondenserSource(_condenserSourceResult));
        for (var i = 0; i < 3; i++)
            StableAnimRect(layer, $"condenser_dust_{i}", cx - 7 + i * 6, 70, 1, 1, tint);

        StableAnimRect(layer, "condenser_port_l", x, 61, 5, 14, "#536976");
        StableAnimRect(layer, "condenser_port_r", x + w - 5, 61, 5, 14, "#536976");
        StableAnimRect(layer, "condenser_port_lip_l", x + 1, 64, 2, 8, "#9DB2BC");
        StableAnimRect(layer, "condenser_port_lip_r", x + w - 3, 64, 2, 8, tint);
        if (blocked)
        {
            StableAnimRect(layer, "condenser_blocked_a", x + w - 8, 59, 2, 7, tint);
            StableAnimRect(layer, "condenser_blocked_b", x + w - 8, 68, 2, 2, tint);
        }
        StableAnimRect(layer, "condenser_progress_bg", x + 1, 88, w - 2, 3, "#263844");
        StableAnimRect(layer, "condenser_progress", x + 1, 88, 0, 3, tint);

        if (CurrentMode == "freezing")
        {
            DrawSourcePanel(layer, 62, 132, 124, 8, "#4E6879");
            StableAnimRect(layer, "condenser_freeze_fill", 64, 134, 0, 4, "#76D8D2");
            StableAnimRect(layer, "condenser_freeze_scan", 65, 134, 3, 4, "#E1F7FF");
        }
    }

    private void UpdateStableCondenserAnimation(double phase)
    {
        if (CurrentMode == "garden")
        {
            UpdateStableGardenAnimation(phase);
            return;
        }

        var mixingLayout = CurrentMode == "mixing";
        var x = mixingLayout ? 111.0 : 83.0;
        var w = mixingLayout ? 70.0 : 98.0;
        var cx = x + w / 2;
        var sceneJob = CurrentMachineRuntimeState == "idle" ? "idle" : _condenserSourceJob;
        var combining = sceneJob is "mixing" or "composting" or "cultivating";
        var motion = sceneJob == "idle" ? 0 : phase;

        if (combining)
        {
            var arm = Math.Cos(motion * Math.PI * 6) * 8;
            SetStableAnimRect(StableAnimPart<Rectangle>("condenser_arm"), Math.Min(cx, cx + arm), 68,
                Math.Max(2, Math.Abs(arm) + 2), 2, true);
        }
        else if (CurrentTier == "mk2")
        {
            var turns = sceneJob == "milling" ? 1440 : 720;
            SetStableAnimAngle("condenser_gear_l", motion * turns);
            SetStableAnimAngle("condenser_gear_r", -motion * turns);
        }
        else
        {
            var stroke = sceneJob == "idle" ? 0 : SmoothParity(.16, .48, phase) * (1 - SmoothParity(.64, .83, phase)) * 6;
            SetStableAnimRect(StableAnimPart<Rectangle>("condenser_press_rod"), cx - 1, 57, 3, 5 + stroke, true);
            SetStableAnimRect(StableAnimPart<Rectangle>("condenser_press_plate"), cx - 11, 61 + stroke, 23, 3, true);
            SetStableAnimRect(StableAnimPart<Rectangle>("condenser_press_tint"), cx - 10, 64 + stroke, 21, 1, true);
        }

        if (sceneJob == "idle")
        {
            SetStableAnimImage("condenser_input", cx, 70, .44);
            SetStableAnimImage("condenser_secondary", 0, 0, 0);
            SetStableAnimImage("condenser_result", 0, 0, 0);
        }
        else
        {
            var feed = SmoothParity(.02, .30, phase);
            var ingredientScale = .46 * SmoothParity(0, .06, phase) * (1 - SmoothParity(.38, .62, phase));
            var orbit = combining ? Math.Sin(motion * Math.PI * 6) * 1.4 * SmoothParity(.18, .32, phase) : 0;
            var target = cx - (combining ? 5 : 0) + orbit;
            SetStableAnimImage("condenser_input", x - 9 + (target - x + 9) * feed, 69, ingredientScale);

            if (sceneJob is "mixing" or "cultivating")
            {
                var secondPhase = phase - .035;
                var secondFeed = SmoothParity(.02, .30, secondPhase);
                var secondScale = .44 * SmoothParity(0, .06, secondPhase) * (1 - SmoothParity(.38, .62, secondPhase));
                SetStableAnimImage("condenser_secondary", x - 9 + (cx + 4 - orbit - x + 9) * secondFeed, 68, secondScale);
            }
            else
            {
                SetStableAnimImage("condenser_secondary", 0, 0, 0);
            }

            var product = sceneJob == "cultivating" ? SmoothParity(.44, .82, phase) : SmoothParity(.64, .82, phase);
            var exit = SmoothParity(.84, 1, phase);
            var resultX = cx + (w + 8 - w / 2) * exit;
            var resultY = 56 + (sceneJob == "cultivating" ? 7 + 6 * exit : 14);
            var resultScale = .46 * product * (1 - SmoothParity(.94, 1, phase));
            SetStableAnimImage("condenser_result", resultX, resultY, resultScale);
        }

        var running = CurrentMachineRuntimeState == "running";
        for (var i = 0; i < 3; i++)
        {
            var fall = PositiveModuloStable(phase * 5 + i * .23, 1);
            var dust = SmoothParity(.38, .46, phase) * (1 - SmoothParity(.76, .84, phase)) * Math.Sin(Math.PI * fall);
            var alpha = Math.Max(0, dust);
            var particle = StableAnimPart<Rectangle>($"condenser_dust_{i}");
            SetStableAnimRect(particle, cx - 7 + i * 6, 70 + Math.Round(fall * 5), 1, 1, running && alpha > .001);
            if (particle is not null)
                particle.Opacity = Math.Clamp(alpha, 0, 1);
        }

        SetStableAnimRect(StableAnimPart<Rectangle>("condenser_progress"), x + 1, 88, (w - 2) * phase, 3, phase > 0);

        if (CurrentMode == "freezing")
        {
            SetStableAnimRect(StableAnimPart<Rectangle>("condenser_freeze_fill"), 64, 134, 120 * phase, 4, phase > 0);
            var time = PositiveModuloStable(StableAnimationWorldTicks, 32);
            var travel = (int)(time * 115 / 32.0);
            SetStableAnimRect(StableAnimPart<Rectangle>("condenser_freeze_scan"), 65 + travel, 134, 3, 4, running);
        }
    }

    private void BuildStableGardenAnimation(Canvas layer)
    {
        const double left = 20, right = 228, top = 50, ground = 112;
        DrawSourcePanel(layer, 14, 44, 220, 138, "#335544");
        StableAnimRect(layer, "garden_sky_a", left, top, right - left, 18, "#182F3B");
        StableAnimRect(layer, "garden_sky_b", left, 68, right - left, 20, "#234753");
        StableAnimRect(layer, "garden_sky_c", left, 88, right - left, 12, "#305849");
        StableAnimRect(layer, "garden_hill_a", left, 96, 52, 9, "#355F48");
        StableAnimRect(layer, "garden_hill_b", 58, 91, 66, 14, "#3B684C");
        StableAnimRect(layer, "garden_hill_c", 112, 94, right - 112, 11, "#315B45");
        StableAnimRect(layer, "garden_grass", left, 103, right - left, 9, "#47763D");
        StableAnimRect(layer, "garden_ground", left, ground, right - left, 5, "#4B4033");
        StableAnimRect(layer, "garden_grass_edge", left, 103, right - left, 3, "#6B9A4D");
        StableAnimRect(layer, "garden_sun_glow", 199, 56, 16, 16, "#556F9B72");
        StableAnimRect(layer, "garden_sun_a", 202, 59, 10, 10, "#E8CF72");
        StableAnimRect(layer, "garden_sun_b", 204, 61, 6, 6, "#FFE29A");
        StableAnimRect(layer, "garden_frame_top", left, top, right - left, 3, "#B1CFC2");
        StableAnimRect(layer, "garden_frame_l", left, top, 3, ground - top, "#688D86");
        StableAnimRect(layer, "garden_frame_r", right - 3, top, 3, ground - top, "#688D86");
        StableAnimRect(layer, "garden_frame_inner", left + 3, top + 3, right - left - 6, 1, "#B5C9BA");
        StableAnimRect(layer, "garden_post_l", 56, top + 4, 1, ground - top - 8, "#445F9C98");
        StableAnimRect(layer, "garden_post_r", 190, top + 4, 1, ground - top - 8, "#445F9C98");

        StableAnimRect(layer, "garden_lamp", left + 8, 54, 17, 2, "#98AAA0");
        StableAnimRect(layer, "garden_lamp_light", left + 10, 56, 13, 1,
            CurrentMachineRuntimeState == "running" ? "#D8F5A4" : "#60776B");
        StableAnimRect(layer, "garden_trunk", 123, 101, 3, 3, "#68452B");
        StableAnimRect(layer, "garden_seed_l", 121, 100, 3, 3, "#5D9A4B");
        StableAnimRect(layer, "garden_seed_r", 125, 98, 3, 3, "#83BF64");
        StableAnimRect(layer, "garden_leaf_a", 124, 80, 1, 1, "#275E36");
        StableAnimRect(layer, "garden_leaf_b", 124, 80, 1, 1, "#347744");
        StableAnimRect(layer, "garden_leaf_c", 124, 80, 1, 1, "#4C9250");
        StableAnimRect(layer, "garden_leaf_d", 124, 80, 1, 1, "#65A95A");
        for (var i = 0; i < 4; i++)
            StableAnimRect(layer, $"garden_drop_{i}", left + 13 + i * 58, top + 10, 1, 3, "#96D7C4");
        StableAnimRect(layer, "garden_scan", 94, ground - 3, 61, 1, "#CAF1A0");
        DrawSourcePanel(layer, 66, 122, 116, 9, "#456250");
        StableAnimRect(layer, "garden_progress", 68, 124, 0, 5,
            CurrentMachineRuntimeState == "blocked" ? "#FFBF76" : "#62C46B");
        DrawGardenRouteParity(layer, 38, 172, 1);
        DrawGardenRouteParity(layer, 210, 172, 2);
        DrawCondenserGardenRouteIcon(layer, 38, 172, "forever_skyblock:condenser_mark_ii");
        DrawCondenserGardenRouteIcon(layer, 210, 172, "forever_skyblock:garden_chamber_mark_i");
    }

    private void UpdateStableGardenAnimation(double phase)
    {
        const double left = 20, right = 228, top = 50, ground = 112;
        var running = CurrentMachineRuntimeState == "running";
        var growth = SmoothParity(.04, .84, phase);
        var lamp = left + 8 + Math.Round((right - left - 33) * growth);
        SetStableAnimRect(StableAnimPart<Rectangle>("garden_lamp"), lamp, 54, 17, 2, true);
        SetStableAnimRect(StableAnimPart<Rectangle>("garden_lamp_light"), lamp + 2, 56, 13, 1, true);

        var height = 3 + Math.Round(31 * growth);
        var topY = 104 - height;
        SetStableAnimRect(StableAnimPart<Rectangle>("garden_trunk"), 123, topY, 3, height, true);

        var seed = growth < .10;
        SetStableAnimRect(StableAnimPart<Rectangle>("garden_seed_l"), 121, 103 - height, 3, 3, seed);
        SetStableAnimRect(StableAnimPart<Rectangle>("garden_seed_r"), 125, 101 - height, 3, 3, seed);

        var r = 3 + 13 * growth;
        var cy = 106 - height;
        SetStableAnimRect(StableAnimPart<Rectangle>("garden_leaf_a"), 124 - r, cy - r / 2, r * 2, Math.Max(3, r), !seed);
        SetStableAnimRect(StableAnimPart<Rectangle>("garden_leaf_b"), 127 - r, cy + 1, Math.Max(3, r * 2 - 5), Math.Max(2, r * .7), !seed);
        SetStableAnimRect(StableAnimPart<Rectangle>("garden_leaf_c"), 126 - r, cy - r / 2 - 2, Math.Max(3, r * 2 - 6), Math.Max(2, r * .55), !seed);
        SetStableAnimRect(StableAnimPart<Rectangle>("garden_leaf_d"), 124 - r / 2, cy - r / 2 - 4, r + 2, 4, !seed);

        for (var i = 0; i < 4; i++)
        {
            var fall = PositiveModuloStable(phase * 12 + i * .25, 1);
            SetStableAnimRect(StableAnimPart<Rectangle>($"garden_drop_{i}"), left + 13 + i * 58,
                top + 10 + Math.Round(fall * 36), 1, 3, running);
        }

        var harvest = SmoothParity(.86, 1, phase);
        SetStableAnimRect(StableAnimPart<Rectangle>("garden_scan"), 94, ground - 3 - Math.Round(harvest * 39), 61, 1,
            running && harvest > 0);
        SetStableAnimRect(StableAnimPart<Rectangle>("garden_progress"), 68, 124, 112 * phase, 5, phase > 0);
    }

    private void UpdateNonWashingStableAnimationFrame()
    {
        if (!IsExclusiveNonWashingMachineRuntimeActive || _nonWashingStableAnimationLayer is null)
            return;
        var phase = Math.Clamp(_phase8TimelineProgress, 0, 1);
        switch (ActiveRuntimeScreen)
        {
            case "CrucibleScreen": UpdateStableCrucibleAnimation(phase); break;
            case "RefiningScreen": UpdateStableRefiningAnimation(phase); break;
            case "AscensionScreen": UpdateStableAscensionAnimation(phase); break;
            case "CondenserScreen": UpdateStableCondenserAnimation(phase); break;
        }
    }

    private Rectangle StableAnimRect(Canvas canvas, string key, double x, double y, double width, double height, string color)
    {
        var rectangle = new Rectangle
        {
            Width = Math.Max(0, width),
            Height = Math.Max(0, height),
            Fill = StableAnimBrush(color),
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        Canvas.SetLeft(rectangle, x);
        Canvas.SetTop(rectangle, y);
        canvas.Children.Add(rectangle);
        _nonWashingStableAnimationParts[key] = rectangle;
        return rectangle;
    }

    private Canvas StableAnimRing(Canvas canvas, string key, double cx, double cy, double radius, double thickness, string color)
    {
        var size = radius * 2;
        var ring = new Canvas
        {
            Width = size,
            Height = size,
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new RotateTransform(0)
        };
        var brush = StableAnimBrush(color);
        void Edge(double x, double y, double w, double h)
        {
            var rect = new Rectangle { Width = w, Height = h, Fill = brush, IsHitTestVisible = false, SnapsToDevicePixels = true };
            Canvas.SetLeft(rect, x); Canvas.SetTop(rect, y); ring.Children.Add(rect);
        }
        Edge(0, 0, size, thickness);
        Edge(0, size - thickness, size, thickness);
        Edge(0, thickness, thickness, Math.Max(0, size - thickness * 2));
        Edge(size - thickness, thickness, thickness, Math.Max(0, size - thickness * 2));
        Canvas.SetLeft(ring, cx - radius);
        Canvas.SetTop(ring, cy - radius);
        canvas.Children.Add(ring);
        _nonWashingStableAnimationParts[key] = ring;
        return ring;
    }

    private Canvas StableAnimGear(Canvas canvas, string key, double cx, double cy, string tint)
    {
        var gear = new Canvas
        {
            Width = 14,
            Height = 14,
            IsHitTestVisible = false,
            RenderTransformOrigin = new Point(.5, .5),
            RenderTransform = new RotateTransform(0)
        };
        for (var i = 0; i < 4; i++)
        {
            var tooth = new Rectangle
            {
                Width = 3,
                Height = 12,
                Fill = StableAnimBrush("#99B0B7"),
                IsHitTestVisible = false,
                RenderTransformOrigin = new Point(.5, .5),
                RenderTransform = new RotateTransform(i * 45)
            };
            Canvas.SetLeft(tooth, 5.5); Canvas.SetTop(tooth, 1); gear.Children.Add(tooth);
        }
        var body = new Rectangle { Width = 8, Height = 8, Fill = StableAnimBrush("#597787"), IsHitTestVisible = false };
        Canvas.SetLeft(body, 3); Canvas.SetTop(body, 3); gear.Children.Add(body);
        var inner = new Rectangle { Width = 6, Height = 6, Fill = StableAnimBrush(tint), IsHitTestVisible = false };
        Canvas.SetLeft(inner, 4); Canvas.SetTop(inner, 4); gear.Children.Add(inner);
        var hub = new Rectangle { Width = 2, Height = 2, Fill = StableAnimBrush("#142734"), IsHitTestVisible = false };
        Canvas.SetLeft(hub, 6); Canvas.SetTop(hub, 6); gear.Children.Add(hub);
        Canvas.SetLeft(gear, cx - 7); Canvas.SetTop(gear, cy - 7); canvas.Children.Add(gear);
        _nonWashingStableAnimationParts[key] = gear;
        return gear;
    }

    private Image StableAnimImage(Canvas canvas, string key, ImageSource? source)
    {
        var image = new Image
        {
            Source = source,
            Width = 1,
            Height = 1,
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        canvas.Children.Add(image);
        _nonWashingStableAnimationParts[key] = image;
        return image;
    }

    private ImageSource? ResolveStableCondenserSource(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || itemId.StartsWith('#') || string.IsNullOrWhiteSpace(_repoRoot))
            return null;
        if (_condenserSourceItemCache.TryGetValue(itemId, out var source))
            return source;
        var bitmap = new MinecraftAssetResolver(_repoRoot).LoadItemBitmap(itemId);
        if (bitmap is null)
            return null;
        _condenserSourceItemCache[itemId] = bitmap;
        return bitmap;
    }

    private void SetStableAnimImage(string key, double centerX, double centerY, double scale)
    {
        var image = StableAnimPart<Image>(key);
        if (image is null || image.Source is null || scale <= .001)
        {
            if (image is not null) image.Visibility = Visibility.Collapsed;
            return;
        }
        var size = Math.Max(.1, 16 * scale);
        image.Visibility = Visibility.Visible;
        image.Width = size;
        image.Height = size;
        Canvas.SetLeft(image, centerX - size / 2);
        Canvas.SetTop(image, centerY - size / 2);
    }

    private void BuildStableSweep(Canvas canvas, string prefix, string outer, string middle, string core)
    {
        StableAnimRect(canvas, prefix + "_outer", 0, 0, 0, 4, outer);
        StableAnimRect(canvas, prefix + "_middle", 0, 0, 0, 4, middle);
        StableAnimRect(canvas, prefix + "_core", 0, 0, 0, 4, core);
    }

    private void UpdateStableSweep(string prefix, double x, double y, double width, double phase, bool visible)
    {
        var center = x - 7 + phase * (width + 14);
        UpdateStableSweepBand(prefix + "_outer", Math.Max(x, center - 7), Math.Min(x + width, center + 8), y, visible && width > 0);
        UpdateStableSweepBand(prefix + "_middle", Math.Max(x, center - 4), Math.Min(x + width, center + 5), y, visible && width > 0);
        UpdateStableSweepBand(prefix + "_core", Math.Max(x, center - 1), Math.Min(x + width, center + 2), y, visible && width > 0);
    }

    private void UpdateStableSweepBand(string key, double left, double right, double y, bool visible)
        => SetStableAnimRect(StableAnimPart<Rectangle>(key), left, y, Math.Max(0, right - left), 4, visible && right > left);

    private T? StableAnimPart<T>(string key) where T : FrameworkElement
        => _nonWashingStableAnimationParts.TryGetValue(key, out var element) ? element as T : null;

    private static void SetStableAnimRect(Rectangle? rectangle, double x, double y, double width, double height, bool visible)
    {
        if (rectangle is null)
            return;
        rectangle.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible)
            return;
        rectangle.Width = Math.Max(0, width);
        rectangle.Height = Math.Max(0, height);
        Canvas.SetLeft(rectangle, x);
        Canvas.SetTop(rectangle, y);
    }

    private void SetStableAnimAngle(string key, double angle)
    {
        if (StableAnimPart<FrameworkElement>(key) is not { RenderTransform: RotateTransform rotate })
            return;
        rotate.Angle = angle;
    }

    private static SolidColorBrush StableAnimBrush(string color)
        => new((Color)ColorConverter.ConvertFromString(color)!);

    private static string StableCondenserTint(string job)
        => job switch
        {
            "composting" or "cultivating" => "#8BD57C",
            "mixing" => "#D0AD79",
            "milling" => "#DDE5E9",
            _ => "#70D9E8"
        };

    private static int PositiveModuloStable(int value, int modulus)
    {
        if (modulus <= 0) return 0;
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }

    private static double PositiveModuloStable(double value, double modulus)
    {
        if (modulus <= 0) return 0;
        var result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
