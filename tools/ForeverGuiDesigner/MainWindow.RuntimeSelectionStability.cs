using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Selection in source-faithful machine previews is editor chrome only.
/// Runtime controls must never receive the generic editor Border selection styling.
/// This coordinator intercepts selection input before the legacy SetSelection path can
/// call UpdateSelectionVisuals, then updates only dedicated adorners/resize handles.
/// </summary>
public partial class MainWindow
{
    private bool _runtimeSelectionStabilityInitialized;
    private bool _runtimeSelectionGuardActive;
    private bool _runtimeSelectionPreviousWashingApplying;
    private bool _runtimeSelectionPreviousMachineApplying;
    private int _runtimeSelectionGuardToken;

    public void EnableRuntimeSelectionStabilityFix()
    {
        if (_runtimeSelectionStabilityInitialized)
            return;
        _runtimeSelectionStabilityInitialized = true;

        // Tunnelling input runs before element/list/tree selection handlers. For runtime
        // previews we establish selection here so the later generic SetSelection path is
        // never entered for ordinary clicks.
        PreviewCanvas.AddHandler(UIElement.PreviewMouseDownEvent,
            new MouseButtonEventHandler(RuntimeSelectionPreviewMouseDown), true);
        ElementsList.AddHandler(UIElement.PreviewMouseDownEvent,
            new MouseButtonEventHandler(RuntimeSelectionPreviewMouseDown), true);
        if (_phase8Hierarchy is not null)
        {
            _phase8Hierarchy.AddHandler(UIElement.PreviewMouseDownEvent,
                new MouseButtonEventHandler(RuntimeSelectionPreviewMouseDown), true);
        }

        PreviewKeyDown += RuntimeSelectionPreviewKeyDown;
    }

    private bool RuntimeSelectionPreviewActive
        => (_washingExclusiveModeActive && IsWashingPhase6Active) ||
           (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive);

    private void RuntimeSelectionPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!RuntimeSelectionPreviewActive)
            return;

        BeginRuntimeSelectionGuard();

        if (e.ChangedButton != MouseButton.Left)
            return;

        if (ReferenceEquals(sender, PreviewCanvas))
        {
            HandleRuntimeCanvasLeftSelection(e);
            return;
        }

        if (ReferenceEquals(sender, ElementsList))
        {
            HandleRuntimeElementsListLeftSelection(e);
            return;
        }

        if (_phase8Hierarchy is not null && ReferenceEquals(sender, _phase8Hierarchy))
            HandleRuntimeHierarchyLeftSelection(e);
    }

    private void HandleRuntimeCanvasLeftSelection(MouseButtonEventArgs e)
    {
        var element = FindGuiElementFromVisual(e.OriginalSource as DependencyObject);
        var control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

        if (element is not null)
        {
            if (control)
            {
                // Legacy Element_MouseLeftButtonDown would call ToggleSelection and then
                // SetSelection. Handle Ctrl-click completely here to avoid that writer.
                ToggleRuntimeSafeSelection(element);
                e.Handled = true;
            }
            else if (!_selection.Contains(element))
            {
                // Preselect without touching runtime chrome. Leave the event unhandled so
                // the existing drag-start logic still runs; it now sees the element already
                // selected and therefore skips SetSelection.
                SetRuntimeSafeSelection([element]);
            }
            return;
        }

        if (ReferenceEquals(e.OriginalSource, PreviewCanvas))
        {
            SetRuntimeSafeSelection([]);
            // Prevent PreviewCanvas_MouseLeftButtonDown from calling SetSelection([]).
            e.Handled = true;
        }
    }

    private void HandleRuntimeElementsListLeftSelection(MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source)
            return;

        var container = ItemsControl.ContainerFromElement(ElementsList, source) as ListBoxItem;
        if (container?.DataContext is not GuiElementModel element)
            return;

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            ToggleRuntimeSafeSelection(element);
        else
            SetRuntimeSafeSelection([element]);

        // We already synchronized SelectedItems ourselves. Suppress the ListBox's normal
        // selection mutation so ElementsList_SelectionChanged cannot call SetSelection.
        e.Handled = true;
    }

    private void HandleRuntimeHierarchyLeftSelection(MouseButtonEventArgs e)
    {
        if (_phase8Hierarchy is null || e.OriginalSource is not DependencyObject source)
            return;

        var container = ItemsControl.ContainerFromElement(_phase8Hierarchy, source) as TreeViewItem;
        if (container?.Tag is not GuiElementModel element)
            return;

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            ToggleRuntimeSafeSelection(element);
        else
            SetRuntimeSafeSelection([element]);

        // Prevent TreeView.SelectedItemChanged from entering SetSelection([model]).
        e.Handled = true;
    }

    private void RuntimeSelectionPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!RuntimeSelectionPreviewActive || Keyboard.FocusedElement is TextBox)
            return;

        var control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        if (control && e.Key == Key.A)
        {
            BeginRuntimeSelectionGuard();
            SetRuntimeSafeSelection(_layout.Elements);
            e.Handled = true;
        }
    }

    private void BeginRuntimeSelectionGuard()
    {
        var token = ++_runtimeSelectionGuardToken;

        if (!_runtimeSelectionGuardActive)
        {
            _runtimeSelectionGuardActive = true;
            _runtimeSelectionPreviousWashingApplying = _washingRenderPipelineFixApplying;
            _runtimeSelectionPreviousMachineApplying = _machineRuntimeApplying;

            if (_washingExclusiveModeActive && IsWashingPhase6Active)
                _washingRenderPipelineFixApplying = true;
            if (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive)
                _machineRuntimeApplying = true;
        }

        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            if (token != _runtimeSelectionGuardToken)
                return;
            EndRuntimeSelectionGuard();
        }));
    }

    private void EndRuntimeSelectionGuard()
    {
        if (!_runtimeSelectionGuardActive)
            return;

        _runtimeSelectionGuardActive = false;
        _washingRenderPipelineFixApplying = _runtimeSelectionPreviousWashingApplying;
        _machineRuntimeApplying = _runtimeSelectionPreviousMachineApplying;

        // Never rebuild children or source chrome here. Only normalize outer properties
        // and refresh dedicated editor overlays.
        if (_washingExclusiveModeActive && IsWashingPhase6Active)
        {
            NormalizeWashingSelectionChrome();
            UpdateWashingSelectionAdorners();
        }
        else if (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive)
        {
            NormalizeMachineRuntimeSelectionChrome();
            UpdateMachineRuntimeSelectionAdorners();
        }

        if (_phase8Initialized)
            EnsurePhase8OverlayVisuals();
    }

    private void NormalizeWashingSelectionChrome()
    {
        var accent = WashingHasWater ? "#9DDDF0" : "#FFBF76";

        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            if (pair.Value is not Border border)
                continue;

            switch (model.Type)
            {
                case GuiElementType.Slot:
                case GuiElementType.Text:
                case GuiElementType.PlayerInventory:
                case GuiElementType.Hotbar:
                    border.BorderBrush = Brushes.Transparent;
                    border.BorderThickness = new Thickness(0);
                    break;

                case GuiElementType.Button:
                    border.BorderBrush = Brush(border.IsMouseOver ? accent : "#6A818C");
                    border.BorderThickness = new Thickness(1);
                    break;

                case GuiElementType.Texture:
                    NormalizeWashingTextureOuterChrome(model, border);
                    break;
            }
        }
    }

    private void NormalizeWashingTextureOuterChrome(GuiElementModel model, Border border)
    {
        switch (model.Id)
        {
            case "forever_phase6_washing_panel_water":
                border.BorderBrush = Brush(WashingHasWater ? "#355B66" : "#5C4939");
                border.BorderThickness = new Thickness(1);
                break;

            case "forever_phase6_washing_panel_process":
                border.BorderBrush = Brush("#54616D");
                border.BorderThickness = new Thickness(1);
                break;

            case "forever_phase6_washing_panel_hints":
                border.BorderBrush = Brush("#465A68");
                border.BorderThickness = new Thickness(1);
                break;

            default:
                border.BorderBrush = Brushes.Transparent;
                border.BorderThickness = new Thickness(0);
                break;
        }
    }

    private void NormalizeMachineRuntimeSelectionChrome()
    {
        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            if (pair.Value is not Border border)
                continue;

            switch (model.Type)
            {
                case GuiElementType.Slot:
                case GuiElementType.Text:
                case GuiElementType.PlayerInventory:
                case GuiElementType.Hotbar:
                    border.BorderBrush = Brushes.Transparent;
                    border.BorderThickness = new Thickness(0);
                    break;

                case GuiElementType.Button:
                    border.BorderBrush = border.IsMouseOver ? Brush(RuntimeAccent()) : Brush("#6A818C");
                    border.BorderThickness = new Thickness(1);
                    break;

                case GuiElementType.Texture:
                    NormalizeMachineRuntimeTextureOuterChrome(model, border);
                    break;
            }
        }
    }

    private void NormalizeMachineRuntimeTextureOuterChrome(GuiElementModel model, Border border)
    {
        var id = model.Id;
        if (id.StartsWith(MachineRuntimePrefix, StringComparison.OrdinalIgnoreCase) &&
            id.Contains("ring_", StringComparison.OrdinalIgnoreCase))
        {
            border.BorderBrush = Brush("#8E4FD2");
            border.BorderThickness = new Thickness(2);
            return;
        }

        if (id.StartsWith(MachineRuntimePrefix, StringComparison.OrdinalIgnoreCase) &&
            id.Contains("_scene", StringComparison.OrdinalIgnoreCase))
        {
            border.BorderBrush = Brush(id.Contains("garden", StringComparison.OrdinalIgnoreCase) ? "#5A8A63" : "#566D7A");
            border.BorderThickness = new Thickness(1);
            return;
        }

        border.BorderBrush = Brushes.Transparent;
        border.BorderThickness = new Thickness(0);
    }
}
