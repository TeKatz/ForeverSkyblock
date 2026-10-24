using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Final coordinator for machine presentation. Older editor phases intentionally own
/// separate features, but they must converge on one deterministic render order:
/// base layout -> runtime machine decoration -> stable animation surfaces -> fidelity.
/// </summary>
public partial class MainWindow
{
    private bool _machinePresentationLifecycleInitialized;
    private bool _machinePresentationQueued;
    private bool _machinePresentationApplying;
    private FrameworkElement? _machinePresentationVisualAnchor;
    private int _machinePresentationVisualCount = -1;

    public void EnableMachinePresentationLifecycleFix()
    {
        if (_machinePresentationLifecycleInitialized)
            return;
        _machinePresentationLifecycleInitialized = true;

        CaptureMachinePresentationGeneration();

        // Explicitly finish machine presentation after operations that are known to
        // call RenderLayout(). Relying on LayoutUpdated alone allowed Apply properties
        // and the first machine load to remain in the generic grey renderer.
        ApplyButton.Click += (_, _) => QueueMachinePresentation();

        if (SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content as string, "Load machine", StringComparison.Ordinal)) is { } loadButton)
        {
            loadButton.Click += (_, _) => QueueMachinePresentation();
        }

        if (_foreverTemplateCombo is not null)
            _foreverTemplateCombo.SelectionChanged += (_, _) =>
            {
                SyncMachineAnimationSchedulers();
                QueueMachinePresentation();
            };

        if (_phase6WashingStatusCombo is not null)
            _phase6WashingStatusCombo.SelectionChanged += (_, _) =>
            {
                SyncMachineAnimationSchedulers();
                QueueMachinePresentation();
            };

        if (_phase6PlayAnimations is not null)
        {
            _phase6PlayAnimations.Checked += (_, _) =>
            {
                SyncMachineAnimationSchedulers();
                QueueMachinePresentation();
            };
            _phase6PlayAnimations.Unchecked += (_, _) =>
            {
                SyncMachineAnimationSchedulers();
                QueueMachinePresentation();
            };
        }

        if (_foreverProgressCombo is not null)
            _foreverProgressCombo.SelectionChanged += (_, _) =>
            {
                SyncMachineAnimationSchedulers();
                QueueMachinePresentation();
            };

        // Active Processing updates CurrentProgress in its own timer. Refresh only the
        // tiny time label after that tick; never rebuild/decorate the full canvas per frame.
        if (_phase7ProcessingTimer is not null)
            _phase7ProcessingTimer.Tick += (_, _) =>
            {
                if (WashingMotionEnabled)
                    UpdateWashingDynamicFidelityText();
            };

        // The Phase 8 timeline is a second valid progress source. It obeys the same
        // Washing state/Animate gate and updates the same small dynamic text surface.
        if (_phase8TimelineTimer is not null)
            _phase8TimelineTimer.Tick += (_, _) =>
            {
                if (WashingMotionEnabled && _phase8Playing)
                    UpdateWashingDynamicFidelityText();
            };

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (_machinePresentationApplying)
                return;
            if (MachinePresentationGenerationChanged())
                QueueMachinePresentation();
        };

        SyncMachineAnimationSchedulers();
        QueueMachinePresentation();
    }

    private bool HasActiveForeverMachineLayout
        => _activeForeverTemplate is { } template &&
           string.Equals(_layout.Name, template.DisplayName, StringComparison.Ordinal) &&
           _layout.Width == template.Layout.Width &&
           _layout.Height == template.Layout.Height;

    private bool WashingMotionEnabled
        => IsWashingPhase6Active &&
           WashingRunning &&
           _phase6PlayAnimations?.IsChecked == true;

    private void QueueMachinePresentation()
    {
        if (_machinePresentationQueued || _machinePresentationApplying)
            return;

        _machinePresentationQueued = true;

        // ApplicationIdle is deliberately below the Background/ContextIdle work used
        // by Phase 5-8. This pass therefore always wins after every older decorator.
        Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(ApplyMachinePresentation));
    }

    private void ApplyMachinePresentation()
    {
        _machinePresentationQueued = false;
        if (_machinePresentationApplying)
            return;

        _machinePresentationApplying = true;
        try
        {
            SyncMachineAnimationSchedulers();

            if (!HasActiveForeverMachineLayout)
            {
                CaptureMachinePresentationGeneration();
                return;
            }

            // RenderLayout() clears generated PreviewCanvas children. Restore the real
            // source-derived machine backdrop explicitly instead of waiting for a later
            // incidental LayoutUpdated event.
            EnsureForeverVisualPreview();

            if (IsWashingPhase6Active)
            {
                // Re-apply the stateful runtime surfaces first. The fidelity pass below
                // is the final owner of slot/button/text chrome.
                DecorateWashingElements();
                EnsureWashingOverlay();
                EnsureStableWashingAnimationVisuals();
                UpdateStableWashingAnimationVisuals();
                ApplyPausedWashingMotionVisibility();
            }

            HideGeneratedPanelMetadata();
            ApplyMachineSemanticLayering();

            // Phase 8 and state decorators are allowed to run before this point. The
            // source-of-truth fidelity renderer always runs last.
            ApplyFidelityPreview();
            ApplyMachineSemanticLayering();

            CaptureFidelityVisualGeneration();
            CaptureMachinePresentationGeneration();
        }
        finally
        {
            _machinePresentationApplying = false;
        }
    }

    private void SyncMachineAnimationSchedulers()
    {
        // Phase 6's original rebuilding timer must never be a live animation source.
        _phase6Timer?.Stop();

        if (!IsWashingPhase6Active)
        {
            _phase6StableAnimationTimer?.Stop();

            // Outside Washing, Active Processing keeps its original behaviour.
            if (IsActiveProcessingSelected)
                _phase7ProcessingTimer?.Start();
            return;
        }

        if (WashingMotionEnabled)
        {
            _phase6StableAnimationTimer?.Start();
            if (IsActiveProcessingSelected)
                _phase7ProcessingTimer?.Start();
            return;
        }

        // IDLE/BLOCKED/NO WATER and Animate=false are hard animation stops. Previously
        // Active Processing kept mutating CurrentProgress behind the disabled scene,
        // which made the process scene loop forever despite the UI state.
        _phase6StableAnimationTimer?.Stop();
        _phase7ProcessingTimer?.Stop();
        _phase8Playing = false;
        _phase6AnimationClock = 0;

        ApplyPausedWashingMotionVisibility();
        UpdateWashingDynamicFidelityText();
    }

    private void ApplyPausedWashingMotionVisibility()
    {
        if (!IsWashingPhase6Active || WashingMotionEnabled)
            return;

        foreach (var pair in _visuals)
        {
            if (pair.Value is not Border border)
                continue;

            if (string.Equals(pair.Key.Id, "forever_phase6_washing_process_scene", StringComparison.OrdinalIgnoreCase) &&
                border.Child is Canvas { Tag: StableWashingSceneTag } scene)
            {
                foreach (var particle in scene.Children.OfType<Rectangle>()
                             .Where(rect => rect.Tag is string tag && tag.StartsWith("particle:", StringComparison.Ordinal)))
                    particle.Visibility = Visibility.Collapsed;
            }
            else if (string.Equals(pair.Key.Id, "forever_phase6_washing_meter_progress", StringComparison.OrdinalIgnoreCase) &&
                     border.Child is Canvas { Tag: StableWashingMeterTag } meter)
            {
                foreach (var shine in meter.Children.OfType<Rectangle>()
                             .Where(rect => rect.Tag is string tag && tag.StartsWith("meter:shine:", StringComparison.Ordinal)))
                    shine.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void UpdateWashingDynamicFidelityText()
    {
        if (!IsWashingPhase6Active)
            return;

        foreach (var pair in _visuals)
        {
            if (!string.Equals(pair.Key.Id, "forever_phase6_washing_time", StringComparison.OrdinalIgnoreCase) ||
                pair.Value is not Border border)
                continue;

            ApplyMachineTextFidelity(pair.Key, border, washing: true);
            Panel.SetZIndex(border, 260);
            break;
        }
    }

    private void ApplyMachineSemanticLayering()
    {
        if (!HasActiveForeverMachineLayout)
            return;

        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            var id = model.Id;

            var z = model.Type switch
            {
                GuiElementType.Slot => 200,
                GuiElementType.Text => 260,
                GuiElementType.Button => 300,
                GuiElementType.Texture when IsMachineBackgroundPanel(id) => -250,
                GuiElementType.Texture when id.Contains("process_scene", StringComparison.OrdinalIgnoreCase) => 80,
                GuiElementType.Texture when id.Contains("meter", StringComparison.OrdinalIgnoreCase) => 120,
                GuiElementType.Texture when id.Contains("ring", StringComparison.OrdinalIgnoreCase) => 120,
                GuiElementType.Texture when id.Contains("status", StringComparison.OrdinalIgnoreCase) => 240,
                GuiElementType.Texture when id.Contains("lock", StringComparison.OrdinalIgnoreCase) => 240,
                GuiElementType.Texture => 40,
                _ => 0
            };

            Panel.SetZIndex(pair.Value, z);
        }
    }

    private static bool IsMachineBackgroundPanel(string id)
        => id.Contains("panel_", StringComparison.OrdinalIgnoreCase) ||
           id.Contains("_panel", StringComparison.OrdinalIgnoreCase);

    private bool MachinePresentationGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _machinePresentationVisualCount ||
               !ReferenceEquals(anchor, _machinePresentationVisualAnchor);
    }

    private void CaptureMachinePresentationGeneration()
    {
        _machinePresentationVisualCount = _visuals.Count;
        _machinePresentationVisualAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }
}
