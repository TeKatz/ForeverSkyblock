using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Final editor interaction layer for machine attachment previews and Phase 8 resize
/// handles. It deliberately runs after the older phase/runtime owners so their source-
/// faithful rendering remains intact and only the missing state combinations are added.
/// </summary>
public partial class MainWindow
{
    private const string AttachmentOverrideTag = "__forever_attachment_override_v1";

    private readonly HashSet<Thumb> _attachmentResizeHookedHandles = [];
    private ComboBox? _attachmentModeCombo;
    private TextBlock? _attachmentModeLabel;
    private Border? _attachmentModeSeparator;
    private TextBlock? _sceneSectionLabel;
    private Border? _sceneSectionSeparator;
    private bool _attachmentAndResizeFixInitialized;
    private bool _attachmentFixApplying;
    private bool _attachmentRefreshQueued;
    private bool _attachmentStateApplying;
    private bool _attachmentResizeGuardActive;
    private bool _attachmentResizePreviousMachineApplying;
    private bool _attachmentResizePreviousWashingApplying;
    private bool _attachmentResizePreviousOverlayBusy;
    private Thumb? _attachmentActiveResizeHandle;
    private string _attachmentOptionKey = string.Empty;
    private string _condenserMk1AttachmentMode = "none";
    private string _crucibleAttachmentMode = "tank_freezing";
    private string _savedCondenserMk2Mode = "base";
    private string _lastAttachmentTier = "mk1";

    public void EnableAttachmentAndResizeFix()
    {
        if (_attachmentAndResizeFixInitialized)
            return;
        _attachmentAndResizeFixInitialized = true;

        CaptureSceneSectionControls();

        _attachmentModeSeparator = SeparatorBar();
        _attachmentModeLabel = SectionLabel("ATTACHMENTS");
        _attachmentModeCombo = new ComboBox
        {
            Width = 148,
            ToolTip = "Preview physically attached machine modules without changing source geometry."
        };
        _attachmentModeCombo.SelectionChanged += AttachmentModeChanged;
        SkyblockBarHost.Children.Add(_attachmentModeSeparator);
        SkyblockBarHost.Children.Add(_attachmentModeLabel);
        SkyblockBarHost.Children.Add(_attachmentModeCombo);

        _lastAttachmentTier = CurrentTier;
        if (IsCondenserPhase5Active && CurrentTier == "mk2")
            _savedCondenserMk2Mode = CurrentMode;

        if (SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content as string, "Load machine", StringComparison.Ordinal)) is { } loadButton)
        {
            loadButton.Click += (_, _) => QueueAttachmentFixRefresh();
        }

        if (_foreverTemplateCombo is not null)
            _foreverTemplateCombo.SelectionChanged += (_, _) => QueueAttachmentFixRefresh();
        if (_foreverTierCombo is not null)
            _foreverTierCombo.SelectionChanged += AttachmentTierChanged;
        if (_foreverModeCombo is not null)
        {
            _foreverModeCombo.SelectionChanged += LegacyCondenserModeChanged;
            _foreverModeCombo.IsVisibleChanged += (_, _) => QueueAttachmentFixRefresh();
        }
        if (_machineRuntimeStateCombo is not null)
            _machineRuntimeStateCombo.SelectionChanged += (_, _) => QueueAttachmentFixRefresh();
        if (_phase6WashingStatusCombo is not null)
            _phase6WashingStatusCombo.SelectionChanged += (_, _) => QueueAttachmentFixRefresh();
        if (_phase8TimelineTimer is not null)
            _phase8TimelineTimer.Tick += (_, _) => UpdateAttachmentDynamicVisuals();

        PreviewCanvas.LayoutUpdated += AttachmentFixLayoutUpdated;

        EnsureAttachmentResizeHooks();
        ApplyThemeSafeComboBoxes();
        QueueAttachmentFixRefresh();
    }

    private void CaptureSceneSectionControls()
    {
        _sceneSectionLabel = SkyblockBarHost.Children
            .OfType<TextBlock>()
            .FirstOrDefault(text => string.Equals(text.Text, "SCENE", StringComparison.Ordinal));
        if (_sceneSectionLabel is null)
            return;

        var index = SkyblockBarHost.Children.IndexOf(_sceneSectionLabel);
        if (index > 0 && SkyblockBarHost.Children[index - 1] is Border separator)
            _sceneSectionSeparator = separator;
    }

    private void AttachmentFixLayoutUpdated(object? sender, EventArgs e)
    {
        EnsureAttachmentResizeHooks();

        // Washing's source compositor intentionally reapplies text fidelity after many
        // operations. Correct the title in-place instead of scheduling another render.
        if (IsWashingPhase6Active)
            ApplyExactWashingHeader();
    }

    private void QueueAttachmentFixRefresh()
    {
        if (_attachmentRefreshQueued || _attachmentFixApplying)
            return;

        _attachmentRefreshQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(ApplyAttachmentFix));
    }

    private void ApplyAttachmentFix()
    {
        _attachmentRefreshQueued = false;
        if (_attachmentFixApplying)
            return;

        _attachmentFixApplying = true;
        try
        {
            EnsureAttachmentResizeHooks();
            UpdateSceneSectionVisibility();
            ConfigureAttachmentControls();
            UpdateCrucibleRuntimeStateAvailability();
            ApplyCondenserAttachmentOverride();
            ApplyCrucibleAttachmentOverride();
            ApplyExactWashingHeader();
            ApplyThemeSafeComboBoxes();
        }
        finally
        {
            _attachmentFixApplying = false;
        }
    }

    private void UpdateSceneSectionVisibility()
    {
        var washing = IsWashingPhase6Active;
        var visibility = washing ? Visibility.Visible : Visibility.Collapsed;

        if (_sceneSectionLabel is not null)
            _sceneSectionLabel.Visibility = visibility;
        if (_sceneSectionSeparator is not null)
            _sceneSectionSeparator.Visibility = visibility;
        if (_phase6WashingStatusCombo is not null)
            _phase6WashingStatusCombo.Visibility = visibility;
        if (_phase6PlayAnimations is not null)
            _phase6PlayAnimations.Visibility = visibility;
    }

    private void ConfigureAttachmentControls()
    {
        if (_attachmentModeCombo is null || _attachmentModeLabel is null || _attachmentModeSeparator is null)
            return;

        var condenserMk1 = IsCondenserPhase5Active && CurrentTier == "mk1";
        var crucible = IsNonWashingRuntimeMachineActive && ActiveRuntimeScreen == "CrucibleScreen";
        var show = condenserMk1 || crucible;
        var visibility = show ? Visibility.Visible : Visibility.Collapsed;
        _attachmentModeCombo.Visibility = visibility;
        _attachmentModeLabel.Visibility = visibility;
        _attachmentModeSeparator.Visibility = visibility;

        if (_foreverModeCombo is not null && IsCondenserPhase5Active)
            _foreverModeCombo.Visibility = condenserMk1 ? Visibility.Collapsed : Visibility.Visible;

        if (!show)
            return;

        if (condenserMk1)
        {
            SetAttachmentOptions(
                "condenser_mk1",
                [
                    ("None", "none"),
                    ("Mixing", "mixing"),
                    ("Freezing", "freezing"),
                    ("Mixing + Freezing", "mixing_freezing")
                ],
                _condenserMk1AttachmentMode);
            SynchronizeLegacyCondenserMode();
        }
        else
        {
            SetAttachmentOptions(
                "crucible",
                [
                    ("None", "none"),
                    ("Freezing", "freezing"),
                    ("Tank", "tank"),
                    ("Tank + Freezing", "tank_freezing")
                ],
                _crucibleAttachmentMode);
        }
    }

    private void SetAttachmentOptions(string key, (string Text, string Tag)[] options, string selectedTag)
    {
        if (_attachmentModeCombo is null)
            return;

        _attachmentStateApplying = true;
        try
        {
            if (!string.Equals(_attachmentOptionKey, key, StringComparison.Ordinal))
            {
                _attachmentOptionKey = key;
                _attachmentModeCombo.Items.Clear();
                foreach (var option in options)
                    _attachmentModeCombo.Items.Add(ComboItem(option.Text, option.Tag));
            }

            var selected = _attachmentModeCombo.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => string.Equals(item.Tag as string, selectedTag, StringComparison.Ordinal));
            if (selected is not null && !ReferenceEquals(_attachmentModeCombo.SelectedItem, selected))
                _attachmentModeCombo.SelectedItem = selected;
        }
        finally
        {
            _attachmentStateApplying = false;
        }
    }

    private void AttachmentModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_attachmentStateApplying || _attachmentModeCombo?.SelectedItem is not ComboBoxItem { Tag: string tag })
            return;

        if (IsCondenserPhase5Active && CurrentTier == "mk1")
        {
            _condenserMk1AttachmentMode = tag;
            SynchronizeLegacyCondenserMode();
        }
        else if (IsNonWashingRuntimeMachineActive && ActiveRuntimeScreen == "CrucibleScreen")
        {
            _crucibleAttachmentMode = tag;
            UpdateCrucibleRuntimeStateAvailability();
        }

        QueueAttachmentFixRefresh();
    }

    private void AttachmentTierChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsCondenserPhase5Active)
        {
            _lastAttachmentTier = CurrentTier;
            QueueAttachmentFixRefresh();
            return;
        }

        var nextTier = CurrentTier;
        if (string.Equals(_lastAttachmentTier, "mk2", StringComparison.Ordinal) &&
            string.Equals(nextTier, "mk1", StringComparison.Ordinal))
        {
            _savedCondenserMk2Mode = CurrentMode;
        }
        else if (string.Equals(_lastAttachmentTier, "mk1", StringComparison.Ordinal) &&
                 string.Equals(nextTier, "mk2", StringComparison.Ordinal))
        {
            SelectLegacyCondenserMode(_savedCondenserMk2Mode);
        }

        _lastAttachmentTier = nextTier;
        if (nextTier == "mk1")
            SynchronizeLegacyCondenserMode();

        QueueAttachmentFixRefresh();
    }

    private void LegacyCondenserModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_attachmentStateApplying || !IsCondenserPhase5Active)
            return;

        if (CurrentTier == "mk2")
            _savedCondenserMk2Mode = CurrentMode;

        QueueAttachmentFixRefresh();
    }

    private void SynchronizeLegacyCondenserMode()
    {
        if (!IsCondenserPhase5Active || CurrentTier != "mk1")
            return;

        var legacy = _condenserMk1AttachmentMode switch
        {
            "mixing" or "mixing_freezing" => "mixing",
            "freezing" => "freezing",
            _ => "base"
        };
        SelectLegacyCondenserMode(legacy);
    }

    private void SelectLegacyCondenserMode(string tag)
    {
        if (_foreverModeCombo is null)
            return;

        var item = _foreverModeCombo.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(candidate => string.Equals(candidate.Tag as string, tag, StringComparison.Ordinal));
        if (item is null || ReferenceEquals(_foreverModeCombo.SelectedItem, item))
            return;

        _attachmentStateApplying = true;
        try
        {
            _foreverModeCombo.SelectedItem = item;
        }
        finally
        {
            _attachmentStateApplying = false;
        }
    }

    private void ApplyCondenserAttachmentOverride()
    {
        if (!IsCondenserPhase5Active || CurrentTier != "mk1")
            return;

        var mixing = _condenserMk1AttachmentMode is "mixing" or "mixing_freezing";
        var freezing = _condenserMk1AttachmentMode is "freezing" or "mixing_freezing";

        var stateText = _condenserMk1AttachmentMode switch
        {
            "mixing" => "MIXING CHAMBER CONNECTED",
            "freezing" => "FREEZING CHAMBER CONNECTED",
            "mixing_freezing" => "MIXING + FREEZING CONNECTED",
            _ => "BASE CONDENSER"
        };
        var hintText = _condenserMk1AttachmentMode switch
        {
            "mixing" => "Second input enabled by Mixing Chamber.",
            "freezing" => "Freezing route enabled.",
            "mixing_freezing" => "Mixing input and freezing route enabled.",
            _ => "No attachment connected."
        };

        SetAndRenderRuntimeText(MachineRuntimePrefix + "module_state", stateText);
        SetAndRenderRuntimeText(MachineRuntimePrefix + "module_hint", hintText);

        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            if (!model.Visible)
                continue;

            if (model.Type == GuiElementType.Slot && model.Id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase) &&
                TryReadAttachmentSlot(model.Content, out var x, out var y))
            {
                if ((x, y) == (80, 61))
                    pair.Value.Visibility = mixing ? Visibility.Visible : Visibility.Collapsed;
                else if ((x, y) is (30, 128) or (202, 128))
                    pair.Value.Visibility = freezing ? Visibility.Visible : Visibility.Collapsed;
                continue;
            }

            if (!model.Id.StartsWith(MachineRuntimePrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            if (model.Id.Contains("module_input", StringComparison.OrdinalIgnoreCase))
                pair.Value.Visibility = mixing ? Visibility.Visible : Visibility.Collapsed;
            else if (model.Id.Contains("freezing_", StringComparison.OrdinalIgnoreCase))
                pair.Value.Visibility = freezing ? Visibility.Visible : Visibility.Collapsed;
        }

        if (freezing)
            RenderCombinedCondenserFreezingProgress();
    }

    private static bool TryReadAttachmentSlot(string content, out int x, out int y)
    {
        x = y = 0;
        var parts = content.Split(',', StringSplitOptions.TrimEntries);
        return parts.Length == 2 && int.TryParse(parts[0], out x) && int.TryParse(parts[1], out y);
    }

    private void RenderCombinedCondenserFreezingProgress()
    {
        var items = Math.Clamp((int)Math.Round(_phase8TimelineProgress * 40), 0, 40);
        SetAndRenderRuntimeText(MachineRuntimePrefix + "freezing_text", $"{items} / 40");

        var model = _layout.Elements.FirstOrDefault(element =>
            string.Equals(element.Id, MachineRuntimePrefix + "freezing_meter", StringComparison.OrdinalIgnoreCase));
        if (model is null || !_visuals.TryGetValue(model, out var visual) || visual is not Border border)
            return;

        var canvas = new Canvas
        {
            Width = model.Width,
            Height = model.Height,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        AddMachineRuntimeRect(canvas, 0, 0, model.Width, model.Height, "#263844");
        AddMachineRuntimeRect(canvas, 0, 0,
            model.Width * Math.Clamp(_phase8TimelineProgress, 0, 1), model.Height, "#76D8D2");
        border.Background = Brushes.Transparent;
        border.BorderBrush = Brushes.Transparent;
        border.BorderThickness = new Thickness(0);
        border.Child = canvas;
        border.Visibility = Visibility.Visible;
    }

    private bool CrucibleTankAttached
        => _crucibleAttachmentMode is "tank" or "tank_freezing";

    private bool CrucibleFreezingAttached
        => _crucibleAttachmentMode is "freezing" or "tank_freezing";

    private void UpdateCrucibleRuntimeStateAvailability()
    {
        if (_machineRuntimeStateCombo is null || ActiveRuntimeScreen != "CrucibleScreen")
            return;

        var freezingItem = _machineRuntimeStateCombo.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag as string, "freezing", StringComparison.Ordinal));
        if (freezingItem is not null)
            freezingItem.IsEnabled = CrucibleFreezingAttached;

        if (!CrucibleFreezingAttached && CurrentMachineRuntimeState == "freezing")
        {
            var idle = _machineRuntimeStateCombo.Items
                .OfType<ComboBoxItem>()
                .FirstOrDefault(item => string.Equals(item.Tag as string, "idle", StringComparison.Ordinal));
            if (idle is not null)
            {
                _attachmentStateApplying = true;
                try
                {
                    _machineRuntimeStateCombo.SelectedItem = idle;
                }
                finally
                {
                    _attachmentStateApplying = false;
                }
            }
        }
    }

    private void ApplyCrucibleAttachmentOverride()
    {
        RemoveAttachmentOverrideOverlay();
        if (!IsNonWashingRuntimeMachineActive || ActiveRuntimeScreen != "CrucibleScreen")
            return;

        SetAndRenderRuntimeText(MachineRuntimePrefix + "tank_text",
            CrucibleTankAttached ? "Fluid Tank detected" : "No Fluid Tank");
        SetAndRenderRuntimeText(MachineRuntimePrefix + "freezer_text",
            CrucibleFreezingAttached ? "Freezing detected" : "No Freezing Chamber");

        if (CrucibleTankAttached && CrucibleFreezingAttached)
            return;

        var canvas = new Canvas
        {
            Tag = AttachmentOverrideTag,
            Width = _layout.Width,
            Height = _layout.Height,
            Background = Brushes.Transparent,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };
        RenderOptions.SetEdgeMode(canvas, EdgeMode.Aliased);

        if (!CrucibleTankAttached)
            AddAttachmentPanelMask(canvas, 18, 58, 103, 18);
        if (!CrucibleFreezingAttached)
            AddAttachmentPanelMask(canvas, 127, 58, 103, 18);

        Panel.SetZIndex(canvas, 570);
        PreviewCanvas.Children.Add(canvas);
    }

    private static void AddAttachmentPanelMask(Canvas canvas, double x, double y, double width, double height)
    {
        AddMachineRuntimeRect(canvas, x, y, width, height, "#09131C");
        AddMachineRuntimeRect(canvas, x + 1, y + 1, width - 2, height - 2, "#37434B");
        AddMachineRuntimeRect(canvas, x + 2, y + 2, width - 4, height - 4, "#14212A");
        AddMachineRuntimeRect(canvas, x + 3, y + 2, width - 6, 1, "#56636B");
    }

    private void RemoveAttachmentOverrideOverlay()
    {
        foreach (var child in PreviewCanvas.Children
                     .OfType<FrameworkElement>()
                     .Where(child => string.Equals(child.Tag as string, AttachmentOverrideTag, StringComparison.Ordinal))
                     .ToList())
        {
            PreviewCanvas.Children.Remove(child);
        }
    }

    private void ApplyExactWashingHeader()
    {
        if (!IsWashingPhase6Active)
            return;

        var model = _layout.Elements.FirstOrDefault(element =>
            string.Equals(element.Id, "screen_title", StringComparison.OrdinalIgnoreCase));
        if (model is null)
            return;

        model.Content = "Washing";
        if (!_visuals.TryGetValue(model, out var visual) || visual is not Border border)
            return;

        border.Background = Brushes.Transparent;
        border.BorderBrush = Brushes.Transparent;
        border.BorderThickness = new Thickness(0);
        if (border.Child is MinecraftPixelText pixelText)
        {
            pixelText.Text = "Washing";
        }
        else
        {
            border.Child = new MinecraftPixelText("Washing", Brush("#E6EEE9"), false, 1.05, "left");
        }
    }

    private void UpdateAttachmentDynamicVisuals()
    {
        if (_attachmentResizeGuardActive)
            return;

        if (IsCondenserPhase5Active && CurrentTier == "mk1" &&
            _condenserMk1AttachmentMode is "freezing" or "mixing_freezing")
        {
            RenderCombinedCondenserFreezingProgress();
        }
    }

    private void EnsureAttachmentResizeHooks()
    {
        _attachmentResizeHookedHandles.RemoveWhere(handle => !_phase8ResizeHandles.Contains(handle));
        foreach (var handle in _phase8ResizeHandles)
        {
            if (!_attachmentResizeHookedHandles.Add(handle))
                continue;

            handle.PreviewMouseLeftButtonDown += AttachmentResizeHandleMouseDown;
            handle.DragStarted += AttachmentResizeHandleDragStarted;
            handle.DragCompleted += AttachmentResizeHandleDragCompleted;
        }
    }

    private void AttachmentResizeHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Thumb thumb)
            BeginAttachmentResizeGuard(thumb);
    }

    private void AttachmentResizeHandleDragStarted(object sender, DragStartedEventArgs e)
    {
        if (sender is not Thumb thumb)
            return;

        BeginAttachmentResizeGuard(thumb);
        if (!thumb.IsMouseCaptured)
            thumb.CaptureMouse();
    }

    private void AttachmentResizeHandleDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (sender is Thumb thumb && thumb.IsMouseCaptured)
            thumb.ReleaseMouseCapture();
        EndAttachmentResizeGuard();
    }

    private void BeginAttachmentResizeGuard(Thumb thumb)
    {
        if (_attachmentResizeGuardActive)
            return;

        _attachmentResizeGuardActive = true;
        _attachmentActiveResizeHandle = thumb;
        _attachmentResizePreviousMachineApplying = _machineRuntimeApplying;
        _attachmentResizePreviousWashingApplying = _washingRenderPipelineFixApplying;
        _attachmentResizePreviousOverlayBusy = _phase8OverlayBusy;

        // Runtime owners listen to handled bubbling mouse events and normally refresh
        // selection adorners there. A resize Thumb must remain the same captured control
        // for the complete drag, so suppress those refreshes until DragCompleted.
        _machineRuntimeApplying = true;
        _washingRenderPipelineFixApplying = true;
        _phase8OverlayBusy = true;
    }

    private void EndAttachmentResizeGuard()
    {
        if (!_attachmentResizeGuardActive)
            return;

        _attachmentResizeGuardActive = false;
        _attachmentActiveResizeHandle = null;
        _machineRuntimeApplying = _attachmentResizePreviousMachineApplying;
        _washingRenderPipelineFixApplying = _attachmentResizePreviousWashingApplying;
        _phase8OverlayBusy = _attachmentResizePreviousOverlayBusy;

        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (IsWashingPhase6Active)
                SynchronizeExclusiveWashingAfterOperation(ensureStructure: false, rebuildBackdrop: true);
            else if (IsNonWashingRuntimeMachineActive)
                SynchronizeExclusiveMachineRuntime(ensureStructure: false, rebuildBackdrop: true);

            EnsureAttachmentResizeHooks();
            QueueAttachmentFixRefresh();
        }));
    }
}
