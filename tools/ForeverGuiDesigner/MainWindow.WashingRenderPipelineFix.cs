using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Exclusive Washing presentation/playback controller.
///
/// Older editor phases were designed as incremental additions and each owns valid
/// functionality in isolation. Washing, however, ended up with several presentation
/// queues and two competing progress clocks touching the same WPF visuals. This
/// controller turns those older passes into passive helpers while Washing is active:
/// one synchronous presentation owner and the Phase 8 timeline as the only live clock.
/// Other machine editors keep their existing Phase 5-8 behaviour unchanged.
/// </summary>
public partial class MainWindow
{
    private const string WashingSelectionAdornerTag = "__forever_washing_selection_adorner_v1";

    private readonly HashSet<Border> _washingExclusiveButtonHooks = [];
    private bool _washingRenderPipelineFixInitialized;
    private bool _washingRenderPipelineFixApplying;
    private bool _washingExclusiveModeActive;
    private bool _washingPlaybackRequested;
    private FrameworkElement? _washingRenderPipelineVisualAnchor;
    private int _washingRenderPipelineVisualCount = -1;
    private DateTime _washingUnifiedLastTick = DateTime.UtcNow;
    private double _washingDynamicTextAccumulator;

    public void EnableWashingRenderPipelineFix()
    {
        if (_washingRenderPipelineFixInitialized)
            return;
        _washingRenderPipelineFixInitialized = true;

        if (SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content as string, "Load machine", StringComparison.Ordinal)) is { } loadButton)
        {
            loadButton.Click += (_, _) => SynchronizeExclusiveWashingAfterOperation(ensureStructure: true, rebuildBackdrop: true);
        }

        ApplyButton.Click += (_, _) => SynchronizeExclusiveWashingAfterOperation(ensureStructure: true, rebuildBackdrop: true);

        // Selection must be restored synchronously. The base editor intentionally
        // changes BorderThickness/BorderBrush for every element; doing the Washing
        // restoration in the same input event prevents a one-frame geometry shift.
        ElementsList.SelectionChanged += (_, _) =>
        {
            if (IsWashingPhase6Active && !_washingRenderPipelineFixApplying)
                ApplyExclusiveWashingPresentation(ensureStructure: false, rebuildBackdrop: false);
        };

        if (_phase6WashingStatusCombo is not null)
            _phase6WashingStatusCombo.SelectionChanged += (_, _) => HandleWashingGateChanged();

        if (_phase6PlayAnimations is not null)
        {
            _phase6PlayAnimations.Checked += (_, _) => HandleWashingGateChanged();
            _phase6PlayAnimations.Unchecked += (_, _) => HandleWashingGateChanged();
        }

        if (_foreverProgressCombo is not null)
            _foreverProgressCombo.SelectionChanged += (_, _) => HandleWashingProgressSelectionChanged();

        // The existing timeline controls remain the UI, but their Washing semantics
        // are unified here so Play/Pause/Stop all control the same progress source.
        foreach (var button in SkyblockBarHost.Children.OfType<Button>())
        {
            switch (button.Content as string)
            {
                case "▶":
                    button.Click += (_, _) => StartExclusiveWashingPlayback();
                    break;
                case "Ⅱ":
                    button.Click += (_, _) => PauseExclusiveWashingPlayback();
                    break;
                case "■":
                    button.Click += (_, _) => StopExclusiveWashingPlayback();
                    break;
            }
        }

        if (_phase8TimelineSlider is not null)
            _phase8TimelineSlider.ValueChanged += (_, _) => HandleExclusiveWashingTimelineScrub();

        // Phase 8's render timer is always alive. While Washing is active it becomes
        // the single clock for both Active Processing and decorative motion. The old
        // Phase 6 stable timer and Phase 7 processing timer stay stopped.
        if (_phase8TimelineTimer is not null)
            _phase8TimelineTimer.Tick += (_, _) => TickExclusiveWashingRuntime();

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (_washingRenderPipelineFixApplying)
                return;

            if (IsWashingPhase6Active)
            {
                EnterExclusiveWashingMode();
                if (DeterministicWashingGenerationChanged() || !HasWashingSourceBackdrop())
                    ApplyExclusiveWashingPresentation(ensureStructure: true, rebuildBackdrop: true);
                return;
            }

            if (_washingExclusiveModeActive)
                ExitExclusiveWashingMode();
        };

        CaptureDeterministicWashingGeneration();
        SynchronizeExclusiveWashingAfterOperation(ensureStructure: true, rebuildBackdrop: true);
    }

    private void SynchronizeExclusiveWashingAfterOperation(bool ensureStructure, bool rebuildBackdrop)
    {
        if (IsWashingPhase6Active)
        {
            EnterExclusiveWashingMode();
            ApplyExclusiveWashingPresentation(ensureStructure, rebuildBackdrop);
        }
        else if (_washingExclusiveModeActive)
        {
            ExitExclusiveWashingMode();
        }
    }

    private void EnterExclusiveWashingMode()
    {
        var firstEntry = !_washingExclusiveModeActive;
        _washingExclusiveModeActive = true;

        // Gate every legacy full-presentation queue. Their helper methods remain
        // available and are called directly below in one deterministic order.
        _phase6Refreshing = true;
        _visualFidelityApplying = true;
        _machinePresentationApplying = true;
        _washingSourceRendererApplying = true;

        _phase6Timer?.Stop();
        _phase6StableAnimationTimer?.Stop();
        _phase7ProcessingTimer?.Stop();

        if (firstEntry)
        {
            _washingPlaybackRequested = IsActiveProcessingSelected;
            SyncTimelineDisplayOnly(CurrentProgress);
            _washingUnifiedLastTick = DateTime.UtcNow;
        }

        SyncExclusiveWashingPlaybackState();
    }

    private void ExitExclusiveWashingMode()
    {
        if (!_washingExclusiveModeActive)
            return;

        _washingExclusiveModeActive = false;
        _phase6Refreshing = false;
        _visualFidelityApplying = false;
        _machinePresentationApplying = false;
        _washingSourceRendererApplying = false;
        _washingPlaybackRequested = false;
        _phase8Playing = false;

        RemoveWashingSelectionAdorners();
        RemoveWashingSourceBackdrop();
        _washingExclusiveButtonHooks.Clear();
        _washingUnifiedLastTick = DateTime.UtcNow;

        // Hand ownership back to the normal editor pipeline for every other machine.
        if (IsActiveProcessingSelected)
            _phase7ProcessingTimer?.Start();
        QueuePhase6Refresh();
        QueueVisualFidelityPass();
        QueueMachinePresentation();
        QueueWashingSourceRender();
    }

    private void ApplyExclusiveWashingPresentation(bool ensureStructure, bool rebuildBackdrop)
    {
        if (!IsWashingPhase6Active || _washingRenderPipelineFixApplying)
            return;

        _washingRenderPipelineFixApplying = true;
        try
        {
            EnterExclusiveWashingMode();

            var changed = false;
            if (ensureStructure)
            {
                changed |= EnsureWashingPhase6Elements();
                changed |= EnsureCompleteWashingSourceSlots();
            }

            if (changed)
                RefreshElementList();

            if (changed || VisibleWashingMachineSlotMissingVisual())
            {
                // This is synchronous on purpose. The source presentation is applied
                // immediately afterwards in the same dispatcher turn, so the user
                // never sees RenderLayout()'s generic grey intermediate frame.
                RenderLayout();
                rebuildBackdrop = true;
            }

            PreviewCanvas.Background = Brush("#111315");
            RemoveWashingOverlay();
            if (rebuildBackdrop || !HasWashingSourceBackdrop())
                RebuildWashingSourceBackdrop();

            DecorateWashingElements();
            EnsureStableWashingAnimationVisuals();
            UpdateStableWashingAnimationVisuals();
            HideGeneratedPanelMetadata();

            _washingExclusiveButtonHooks.RemoveWhere(border => !_visuals.Values.Contains(border));

            foreach (var pair in _visuals)
            {
                var model = pair.Key;
                if (pair.Value is not Border border)
                    continue;

                switch (model.Type)
                {
                    case GuiElementType.Slot:
                        ApplyMachineSlotFidelity(model, border, washing: true);
                        NormalizeExclusiveWashingNativeOuterBorder(border);
                        WrapExclusiveWashingSlotForPhase8(border);
                        break;

                    case GuiElementType.Button:
                        ApplyExclusiveWashingButtonChrome(model, border, border.IsMouseOver);
                        break;

                    case GuiElementType.Text:
                        ApplyMachineTextFidelity(model, border, washing: true);
                        NormalizeExclusiveWashingNativeOuterBorder(border);
                        break;
                }

                ApplyWashingSourceSurface(model, border);
            }

            ApplyWashingSourceLayering();
            SyncExclusiveWashingPlaybackState();
            UpdateStableWashingAnimationVisuals();
            if (!ExclusiveWashingMotionEnabled)
                HideExclusiveWashingTransientMotion();

            UpdateWashingSelectionAdorners();

            CaptureFidelityVisualGeneration();
            CaptureMachinePresentationGeneration();
            CaptureWashingSourceGeneration();
            CaptureDeterministicWashingGeneration();
        }
        finally
        {
            _washingRenderPipelineFixApplying = false;
        }
    }

    private void HandleWashingGateChanged()
    {
        if (!IsWashingPhase6Active)
            return;

        EnterExclusiveWashingMode();
        SyncExclusiveWashingPlaybackState();
        ApplyExclusiveWashingPresentation(ensureStructure: false, rebuildBackdrop: true);
    }

    private void HandleWashingProgressSelectionChanged()
    {
        if (!IsWashingPhase6Active)
            return;

        EnterExclusiveWashingMode();
        _phase7ProcessingTimer?.Stop();

        if (IsActiveProcessingSelected)
        {
            // Phase 7's earlier selection handler has already reset its live item to
            // zero. Mirror that value into the one surviving Phase 8 clock and start.
            SyncTimelineDisplayOnly(CurrentProgress);
            _washingPlaybackRequested = true;
        }
        else
        {
            _washingPlaybackRequested = false;
            SyncTimelineDisplayOnly(CurrentProgress);
        }

        SyncExclusiveWashingPlaybackState();
        ApplyExclusiveWashingPresentation(ensureStructure: false, rebuildBackdrop: false);
    }

    private void StartExclusiveWashingPlayback()
    {
        if (!IsWashingPhase6Active)
            return;

        EnterExclusiveWashingMode();

        if (_phase7ActiveProcessingItem is not null && !IsActiveProcessingSelected)
            _foreverProgressCombo!.SelectedItem = _phase7ActiveProcessingItem;

        _washingPlaybackRequested = true;
        SyncExclusiveWashingPlaybackState();
        ApplyExclusiveWashingPresentation(ensureStructure: false, rebuildBackdrop: false);
    }

    private void PauseExclusiveWashingPlayback()
    {
        if (!IsWashingPhase6Active)
            return;

        EnterExclusiveWashingMode();
        _washingPlaybackRequested = false;
        _phase8Playing = false;
        _phase7ProcessingTimer?.Stop();
        SyncExclusiveWashingPlaybackState();
        ApplyExclusiveWashingPresentation(ensureStructure: false, rebuildBackdrop: false);
    }

    private void StopExclusiveWashingPlayback()
    {
        if (!IsWashingPhase6Active)
            return;

        EnterExclusiveWashingMode();
        _washingPlaybackRequested = false;
        _phase8Playing = false;
        _phase7ProcessingTimer?.Stop();
        _phase6AnimationClock = 0;

        if (_phase7ActiveProcessingItem is not null && !IsActiveProcessingSelected)
            _foreverProgressCombo!.SelectedItem = _phase7ActiveProcessingItem;

        SetPhase8TimelineProgress(0);
        SyncExclusiveWashingPlaybackState();
        ApplyExclusiveWashingPresentation(ensureStructure: false, rebuildBackdrop: false);
    }

    private void HandleExclusiveWashingTimelineScrub()
    {
        if (!IsWashingPhase6Active || _phase8UpdatingSlider)
            return;

        EnterExclusiveWashingMode();
        _washingPlaybackRequested = false;
        _phase8Playing = false;
        _phase7ProcessingTimer?.Stop();
        SyncExclusiveWashingPlaybackState();
        ApplyExclusiveWashingPresentation(ensureStructure: false, rebuildBackdrop: false);
    }

    private void TickExclusiveWashingRuntime()
    {
        var now = DateTime.UtcNow;
        var dt = Math.Clamp((now - _washingUnifiedLastTick).TotalSeconds, 0, .1);
        _washingUnifiedLastTick = now;

        if (!IsWashingPhase6Active)
        {
            if (_washingExclusiveModeActive)
                ExitExclusiveWashingMode();
            return;
        }

        EnterExclusiveWashingMode();

        // A non-looping timeline that reached 100% is a real stop condition.
        if (IsActiveProcessingSelected && _phase8TimelineProgress >= 1 && _phase8LoopBox?.IsChecked != true)
            _washingPlaybackRequested = false;

        SyncExclusiveWashingPlaybackState();

        if (ExclusiveWashingMotionEnabled)
            _phase6AnimationClock += dt;

        EnsureStableWashingAnimationVisuals();
        UpdateStableWashingAnimationVisuals();
        if (!ExclusiveWashingMotionEnabled)
            HideExclusiveWashingTransientMotion();

        _washingDynamicTextAccumulator += dt;
        if (_washingDynamicTextAccumulator >= .05)
        {
            _washingDynamicTextAccumulator = 0;
            UpdateExclusiveWashingTimeText();
        }
    }

    private bool ExclusiveWashingMotionEnabled
    {
        get
        {
            if (!IsWashingPhase6Active || !WashingRunning || _phase6PlayAnimations?.IsChecked != true)
                return false;

            return !IsActiveProcessingSelected || _washingPlaybackRequested;
        }
    }

    private void SyncExclusiveWashingPlaybackState()
    {
        if (!IsWashingPhase6Active)
            return;

        // Phase 8 is the only live Washing clock. These timers are intentionally kept
        // stopped even if an older synchronous state handler tried to restart them.
        _phase6Timer?.Stop();
        _phase6StableAnimationTimer?.Stop();
        _phase7ProcessingTimer?.Stop();

        var shouldPlayTimeline = IsActiveProcessingSelected &&
                                 _washingPlaybackRequested &&
                                 WashingRunning &&
                                 _phase6PlayAnimations?.IsChecked == true;

        if (shouldPlayTimeline && !_phase8Playing)
            _phase8LastTick = DateTime.UtcNow;

        _phase8Playing = shouldPlayTimeline;
    }

    private void SyncTimelineDisplayOnly(double progress)
    {
        _phase8TimelineProgress = Math.Clamp(progress, 0, 1);
        if (_phase8TimelineSlider is not null)
        {
            _phase8UpdatingSlider = true;
            _phase8TimelineSlider.Value = _phase8TimelineProgress * 100;
            _phase8UpdatingSlider = false;
        }

        if (_phase8TimelineText is not null)
            _phase8TimelineText.Text = $"{_phase8TimelineProgress * 5.0:0.0}/5.0s";
    }

    private void UpdateExclusiveWashingTimeText()
    {
        foreach (var pair in _visuals)
        {
            if (!string.Equals(pair.Key.Id, "forever_phase6_washing_time", StringComparison.OrdinalIgnoreCase) ||
                pair.Value is not Border border)
            {
                continue;
            }

            ApplyMachineTextFidelity(pair.Key, border, washing: true);
            NormalizeExclusiveWashingNativeOuterBorder(border);
            Panel.SetZIndex(border, 620);
            break;
        }
    }

    private void HideExclusiveWashingTransientMotion()
    {
        foreach (var pair in _visuals)
        {
            if (pair.Value is not Border border)
                continue;

            if (string.Equals(pair.Key.Id, "forever_phase6_washing_process_scene", StringComparison.OrdinalIgnoreCase) &&
                border.Child is Canvas { Tag: StableWashingSceneTag } scene)
            {
                foreach (var particle in scene.Children.OfType<System.Windows.Shapes.Rectangle>()
                             .Where(rect => rect.Tag is string tag && tag.StartsWith("particle:", StringComparison.Ordinal)))
                {
                    particle.Visibility = Visibility.Collapsed;
                }
            }
            else if (string.Equals(pair.Key.Id, "forever_phase6_washing_meter_progress", StringComparison.OrdinalIgnoreCase) &&
                     border.Child is Canvas { Tag: StableWashingMeterTag } meter)
            {
                foreach (var shine in meter.Children.OfType<System.Windows.Shapes.Rectangle>()
                             .Where(rect => rect.Tag is string tag && tag.StartsWith("meter:shine:", StringComparison.Ordinal)))
                {
                    shine.Visibility = Visibility.Collapsed;
                }
            }
        }
    }

    private void NormalizeExclusiveWashingNativeOuterBorder(Border border)
    {
        // Selection is rendered by a separate overlay. Never change native element
        // border thickness; doing so moves the child by one pixel in WPF.
        border.BorderBrush = Brushes.Transparent;
        border.BorderThickness = new Thickness(0);
    }

    private static void WrapExclusiveWashingSlotForPhase8(Border border)
    {
        if (border.Child is Grid { Tag: string tag } && string.Equals(tag, "phase8-item", StringComparison.Ordinal))
            return;

        if (border.Child is not FrameworkElement child)
            return;

        border.Child = null;
        var wrapper = new Grid
        {
            Tag = "phase8-item",
            ClipToBounds = true,
            IsHitTestVisible = false
        };
        wrapper.Children.Add(child);
        border.Child = wrapper;
    }

    private void ApplyExclusiveWashingButtonChrome(GuiElementModel model, Border border, bool hover)
    {
        var accent = WashingHasWater ? "#9DDDF0" : "#FFBF76";
        var selected = _selection.Contains(model);

        border.Background = new LinearGradientBrush(
            Brush(hover ? "#37515E" : "#263B4B").Color,
            Brush("#172936").Color,
            90);
        border.BorderBrush = Brush(selected ? "#2E9BFF" : hover ? accent : "#6A818C");
        border.BorderThickness = new Thickness(1);
        border.ClipToBounds = true;

        border.Child = new MinecraftPixelText(
            string.IsNullOrWhiteSpace(model.Content) ? model.Id : model.Content,
            Brush("#E6EEE9"),
            false,
            .92,
            "center")
        {
            Width = Math.Max(1, model.Width - 4),
            Height = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };

        // Phase 8 treats MinecraftPixelText as already decorated, so it cannot
        // replace the button body with a frameless generic text control anymore.
        if (_washingExclusiveButtonHooks.Add(border))
        {
            border.MouseEnter += (_, _) =>
            {
                if (IsWashingPhase6Active && border.Tag is GuiElementModel current)
                    ApplyExclusiveWashingButtonChrome(current, border, hover: true);
            };
            border.MouseLeave += (_, _) =>
            {
                if (IsWashingPhase6Active && border.Tag is GuiElementModel current)
                    ApplyExclusiveWashingButtonChrome(current, border, hover: false);
            };
        }
    }

    private void UpdateWashingSelectionAdorners()
    {
        RemoveWashingSelectionAdorners();
        if (!IsWashingPhase6Active)
            return;

        foreach (var model in _selection.Where(model => model.Visible && _visuals.ContainsKey(model)))
        {
            var adorner = new Border
            {
                Tag = WashingSelectionAdornerTag,
                Width = Math.Max(1, model.Width),
                Height = Math.Max(1, model.Height),
                Background = Brushes.Transparent,
                BorderBrush = Brush("#2E9BFF"),
                BorderThickness = new Thickness(1),
                IsHitTestVisible = false,
                SnapsToDevicePixels = true
            };
            Canvas.SetLeft(adorner, model.X);
            Canvas.SetTop(adorner, model.Y);
            Panel.SetZIndex(adorner, 10000);
            PreviewCanvas.Children.Add(adorner);
        }
    }

    private void RemoveWashingSelectionAdorners()
    {
        var adorners = PreviewCanvas.Children.OfType<FrameworkElement>()
            .Where(child => string.Equals(child.Tag as string, WashingSelectionAdornerTag, StringComparison.Ordinal))
            .ToList();
        foreach (var adorner in adorners)
            PreviewCanvas.Children.Remove(adorner);
    }

    private bool EnsureCompleteWashingSourceSlots()
    {
        var changed = false;
        var expected = new[]
        {
            (Id: "machine_slot_1", Content: "42,78", X: 38, Y: 74),
            (Id: "machine_slot_2", Content: "174,78", X: 170, Y: 74),
            (Id: "machine_slot_3", Content: "202,78", X: 198, Y: 74)
        };

        foreach (var slot in expected)
        {
            var existing = _layout.Elements.FirstOrDefault(element =>
                               element.Type == GuiElementType.Slot &&
                               string.Equals(element.Content, slot.Content, StringComparison.OrdinalIgnoreCase))
                           ?? _layout.Elements.FirstOrDefault(element =>
                               element.Type == GuiElementType.Slot &&
                               string.Equals(element.Id, slot.Id, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
                continue;

            _layout.Elements.Add(new GuiElementModel
            {
                Id = CreateUniqueId(slot.Id),
                Type = GuiElementType.Slot,
                X = slot.X,
                Y = slot.Y,
                Width = 24,
                Height = 24,
                Content = slot.Content,
                Visible = true
            });
            changed = true;
        }

        return changed;
    }

    private bool VisibleWashingMachineSlotMissingVisual()
        => _layout.Elements.Any(model =>
            model.Visible &&
            model.Type == GuiElementType.Slot &&
            model.Id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase) &&
            !_visuals.ContainsKey(model));

    private bool DeterministicWashingGenerationChanged()
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        return _visuals.Count != _washingRenderPipelineVisualCount ||
               !ReferenceEquals(anchor, _washingRenderPipelineVisualAnchor);
    }

    private void CaptureDeterministicWashingGeneration()
    {
        _washingRenderPipelineVisualCount = _visuals.Count;
        _washingRenderPipelineVisualAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }
}
