using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Exclusive Washing presentation/playback controller.
///
/// Washing is intentionally treated as a single-owner runtime preview. Older phases
/// still provide helper methods, but while Washing is active none of their asynchronous
/// presentation loops are allowed to mutate the same WPF visuals behind this controller.
/// </summary>
public partial class MainWindow
{
    private const string WashingSelectionAdornerTag = "__forever_washing_selection_adorner_v2";

    private readonly HashSet<Border> _washingExclusiveButtonHooks = [];
    private bool _washingRenderPipelineFixInitialized;
    private bool _washingRenderPipelineFixApplying;
    private bool _washingExclusiveModeActive;
    private bool _washingPlaybackRequested;
    private FrameworkElement? _washingRenderPipelineVisualAnchor;
    private int _washingRenderPipelineVisualCount = -1;
    private DateTime _washingUnifiedLastTick = DateTime.UtcNow;

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

        // Direct canvas selection suppresses ElementsList.SelectionChanged in the base
        // editor. Listen to the bubbling mouse event as well, after the element's own
        // selection handler has executed, so source chrome is restored before WPF paints.
        PreviewCanvas.AddHandler(UIElement.MouseLeftButtonDownEvent,
            new MouseButtonEventHandler((_, _) => SynchronizeExclusiveWashingSelection()), true);

        ElementsList.SelectionChanged += (_, _) => SynchronizeExclusiveWashingSelection();
        if (_phase8Hierarchy is not null)
            _phase8Hierarchy.SelectedItemChanged += (_, _) => SynchronizeExclusiveWashingSelection();

        if (_phase6WashingStatusCombo is not null)
            _phase6WashingStatusCombo.SelectionChanged += (_, _) => HandleWashingGateChanged();

        if (_phase6PlayAnimations is not null)
        {
            _phase6PlayAnimations.Checked += (_, _) => HandleWashingGateChanged();
            _phase6PlayAnimations.Unchecked += (_, _) => HandleWashingGateChanged();
        }

        if (_foreverProgressCombo is not null)
            _foreverProgressCombo.SelectionChanged += (_, _) => HandleWashingProgressSelectionChanged();

        // Existing Phase 8 controls remain the UI. Their original handlers run first;
        // these handlers normalize the final Washing state in the same routed event.
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

        // Reuse the Phase 8 timer only as a heartbeat. _phase8Playing stays false while
        // Washing owns the preview, so Phase8TimelineTick itself cannot be a second clock.
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

    private void SynchronizeExclusiveWashingSelection()
    {
        if (!IsWashingPhase6Active || _washingRenderPipelineFixApplying)
            return;

        EnterExclusiveWashingMode();
        RestoreExclusiveWashingElementChrome();
        UpdateWashingSelectionAdorners();

        // Phase 8 handles are editor chrome, not machine chrome. Keep them in sync
        // immediately so clearing a selection cannot leave stale resize handles behind.
        if (_phase8Initialized)
            EnsurePhase8OverlayVisuals();
    }

    private void EnterExclusiveWashingMode()
    {
        var firstEntry = !_washingExclusiveModeActive;
        _washingExclusiveModeActive = true;

        // Phase 5 was the remaining delayed writer in 0.8.5. Its broad widget
        // predicates classify phase6 Washing ids containing _panel_, _meter_ and
        // _status as generic Phase 5 widgets. Keep its refresh loop gated too.
        _phase5Refreshing = true;
        _phase6Refreshing = true;
        _visualFidelityApplying = true;
        _machinePresentationApplying = true;
        _washingSourceRendererApplying = true;

        _phase6Timer?.Stop();
        _phase6StableAnimationTimer?.Stop();
        _phase7ProcessingTimer?.Stop();
        _phase8Playing = false;

        if (firstEntry)
        {
            UpdatePhase6ControlAvailability();
            _washingPlaybackRequested = IsActiveProcessingSelected && CurrentProgress < 1.0;
            SetExclusiveWashingProgress(CurrentProgress, updateVisuals: false);
            _washingUnifiedLastTick = DateTime.UtcNow;
        }
    }

    private void ExitExclusiveWashingMode()
    {
        if (!_washingExclusiveModeActive)
            return;

        _washingExclusiveModeActive = false;
        _phase5Refreshing = false;
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

        if (IsActiveProcessingSelected)
            _phase7ProcessingTimer?.Start();

        QueuePhase5Refresh();
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
            UpdatePhase6ControlAvailability();

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
            RestoreExclusiveWashingElementChrome();

            UpdateStableWashingAnimationVisuals();
            if (!ExclusiveWashingMotionEnabled)
                HideExclusiveWashingTransientMotion();

            UpdateWashingSelectionAdorners();
            if (_phase8Initialized)
                EnsurePhase8OverlayVisuals();

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

    private void RestoreExclusiveWashingElementChrome()
    {
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
    }

    private void HandleWashingGateChanged()
    {
        if (!IsWashingPhase6Active)
            return;

        EnterExclusiveWashingMode();
        UpdatePhase6ControlAvailability();
        ApplyExclusiveWashingPresentation(ensureStructure: false, rebuildBackdrop: true);
    }

    private void HandleWashingProgressSelectionChanged()
    {
        if (!IsWashingPhase6Active)
            return;

        EnterExclusiveWashingMode();
        _phase7ProcessingTimer?.Stop();
        _phase8Playing = false;

        if (IsActiveProcessingSelected)
        {
            SetExclusiveWashingProgress(CurrentProgress, updateVisuals: false);
            _washingPlaybackRequested = true;
        }
        else
        {
            _washingPlaybackRequested = false;
            SetExclusiveWashingProgress(CurrentProgress, updateVisuals: false);
        }

        ApplyExclusiveWashingPresentation(ensureStructure: false, rebuildBackdrop: false);
    }

    private void StartExclusiveWashingPlayback()
    {
        if (!IsWashingPhase6Active)
            return;

        EnterExclusiveWashingMode();

        if (_phase7ActiveProcessingItem is not null && !IsActiveProcessingSelected)
            _foreverProgressCombo!.SelectedItem = _phase7ActiveProcessingItem;

        if (_phase8TimelineProgress >= .999999)
            SetExclusiveWashingProgress(0, updateVisuals: true);

        _washingPlaybackRequested = true;
        _phase8Playing = false;
        _washingUnifiedLastTick = DateTime.UtcNow;
        UpdateExclusiveWashingRuntimeVisuals();
    }

    private void PauseExclusiveWashingPlayback()
    {
        if (!IsWashingPhase6Active)
            return;

        EnterExclusiveWashingMode();
        _washingPlaybackRequested = false;
        _phase8Playing = false;
        _phase7ProcessingTimer?.Stop();
        UpdateExclusiveWashingRuntimeVisuals();
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

        SetExclusiveWashingProgress(0, updateVisuals: true);
        _washingUnifiedLastTick = DateTime.UtcNow;
    }

    private void HandleExclusiveWashingTimelineScrub()
    {
        if (!IsWashingPhase6Active || _phase8UpdatingSlider)
            return;

        EnterExclusiveWashingMode();
        _washingPlaybackRequested = false;
        _phase8Playing = false;
        _phase7ProcessingTimer?.Stop();
        SetExclusiveWashingProgress((_phase8TimelineSlider?.Value ?? 0) / 100.0, updateVisuals: true);
        _washingUnifiedLastTick = DateTime.UtcNow;
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

        // Never let the original Phase 8 tick become a second Washing clock.
        _phase8Playing = false;
        _phase6Timer?.Stop();
        _phase6StableAnimationTimer?.Stop();
        _phase7ProcessingTimer?.Stop();

        var shouldAdvance = IsActiveProcessingSelected &&
                            _washingPlaybackRequested &&
                            WashingRunning &&
                            _phase6PlayAnimations?.IsChecked == true;

        if (shouldAdvance)
        {
            var speed = _phase8SpeedBox?.SelectedItem is ComboBoxItem { Tag: double value } ? value : 1.0;
            var next = _phase8TimelineProgress + dt * speed / 5.0;
            if (next >= 1.0)
            {
                if (_phase8LoopBox?.IsChecked == true)
                {
                    next %= 1.0;
                }
                else
                {
                    next = 1.0;
                    _washingPlaybackRequested = false;
                }
            }

            SetExclusiveWashingProgress(next, updateVisuals: false);
        }

        if (ExclusiveWashingMotionEnabled)
            _phase6AnimationClock += dt;

        UpdateExclusiveWashingRuntimeVisuals();
    }

    private bool ExclusiveWashingMotionEnabled
        => IsWashingPhase6Active &&
           WashingRunning &&
           _phase6PlayAnimations?.IsChecked == true &&
           (!IsActiveProcessingSelected || _washingPlaybackRequested);

    private void SetExclusiveWashingProgress(double progress, bool updateVisuals)
    {
        progress = Math.Clamp(progress, 0, 1);
        _phase8TimelineProgress = progress;
        _phase8Playing = false;

        if (_phase8TimelineSlider is not null)
        {
            _phase8UpdatingSlider = true;
            _phase8TimelineSlider.Value = progress * 100.0;
            _phase8UpdatingSlider = false;
        }

        if (_phase8TimelineText is not null)
            _phase8TimelineText.Text = $"{progress * 5.0:0.0}/5.0s";

        if (_phase7ActiveProcessingItem is not null && IsActiveProcessingSelected)
        {
            _phase7ActiveProcessingItem.Tag =
                (progress * 100.0).ToString("0.###", CultureInfo.InvariantCulture);
        }

        if (updateVisuals)
            UpdateExclusiveWashingRuntimeVisuals();
    }

    private void UpdateExclusiveWashingRuntimeVisuals()
    {
        if (!IsWashingPhase6Active)
            return;

        EnsureStableWashingAnimationVisuals();
        UpdateStableWashingAnimationVisuals();
        if (!ExclusiveWashingMotionEnabled)
            HideExclusiveWashingTransientMotion();
        UpdateExclusiveWashingTimeText();
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
                    particle.Visibility = Visibility.Collapsed;
            }
            else if (string.Equals(pair.Key.Id, "forever_phase6_washing_meter_progress", StringComparison.OrdinalIgnoreCase) &&
                     border.Child is Canvas { Tag: StableWashingMeterTag } meter)
            {
                foreach (var shine in meter.Children.OfType<System.Windows.Shapes.Rectangle>()
                             .Where(rect => rect.Tag is string tag && tag.StartsWith("meter:shine:", StringComparison.Ordinal)))
                    shine.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static void NormalizeExclusiveWashingNativeOuterBorder(Border border)
    {
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

        border.Background = new LinearGradientBrush(
            Brush(hover ? "#37515E" : "#263B4B").Color,
            Brush("#172936").Color,
            90);
        border.BorderBrush = Brush(hover ? accent : "#6A818C");
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
