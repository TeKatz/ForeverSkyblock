using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Selection in source-faithful machine previews is editor chrome only.
/// Runtime controls must never receive generic editor selection styling and selection
/// suppression must never outlive the input event that required it.
/// </summary>
public partial class MainWindow
{
    private bool _runtimeSelectionStabilityInitialized;
    private int _runtimeSelectionWriterSuppressionDepth;
    private bool _runtimeSelectionPreviousWashingApplying;
    private bool _runtimeSelectionPreviousMachineApplying;
    private int _runtimeSelectionNormalizeToken;

    public void EnableRuntimeSelectionStabilityFix()
    {
        if (_runtimeSelectionStabilityInitialized)
            return;
        _runtimeSelectionStabilityInitialized = true;

        // Tunnelling input establishes runtime-safe selection before legacy element/list
        // handlers execute. The suppression is released synchronously by the matching
        // bubbling MouseLeftButtonDown event; it never survives into a context-menu action.
        PreviewCanvas.AddHandler(UIElement.PreviewMouseDownEvent,
            new MouseButtonEventHandler(RuntimeSelectionPreviewMouseDown), true);
        PreviewCanvas.AddHandler(UIElement.MouseLeftButtonDownEvent,
            new MouseButtonEventHandler(RuntimeSelectionMouseDownCompleted), true);

        ElementsList.AddHandler(UIElement.PreviewMouseDownEvent,
            new MouseButtonEventHandler(RuntimeSelectionPreviewMouseDown), true);
        ElementsList.AddHandler(UIElement.MouseLeftButtonDownEvent,
            new MouseButtonEventHandler(RuntimeSelectionMouseDownCompleted), true);

        if (_phase8Hierarchy is not null)
        {
            _phase8Hierarchy.AddHandler(UIElement.PreviewMouseDownEvent,
                new MouseButtonEventHandler(RuntimeSelectionPreviewMouseDown), true);
            _phase8Hierarchy.AddHandler(UIElement.MouseLeftButtonDownEvent,
                new MouseButtonEventHandler(RuntimeSelectionMouseDownCompleted), true);
            _phase8Hierarchy.SelectedItemChanged += (_, _) => QueueRuntimeSelectionOuterNormalization();
        }

        // This also catches programmatic SetSelection callers that still synchronize the
        // ListBox directly (validation navigation, legacy helpers, etc.). Their generic
        // border paint is repaired after the synchronous SetSelection call returns, but
        // no child visual or source surface is rebuilt.
        ElementsList.SelectionChanged += (_, _) => QueueRuntimeSelectionOuterNormalization();
        PreviewKeyDown += RuntimeSelectionPreviewKeyDown;
    }

    private bool RuntimeSelectionPreviewActive
        => (_washingExclusiveModeActive && IsWashingPhase6Active) ||
           (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive);

    private void RuntimeSelectionPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!RuntimeSelectionPreviewActive || e.ChangedButton != MouseButton.Left)
            return;

        BeginRuntimeSelectionWriterSuppression();

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

    private void RuntimeSelectionMouseDownCompleted(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || _runtimeSelectionWriterSuppressionDepth == 0)
            return;

        EndRuntimeSelectionWriterSuppression();
        RefreshRuntimeSelectionOverlaysOnly();
    }

    private void HandleRuntimeCanvasLeftSelection(MouseButtonEventArgs e)
    {
        var element = FindGuiElementFromVisual(e.OriginalSource as DependencyObject);
        var control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;

        if (element is not null)
        {
            if (control)
            {
                ToggleRuntimeSafeSelection(element);
                e.Handled = true;
            }
            else if (!_selection.Contains(element))
            {
                // Leave the event unhandled so the established drag-start logic still runs.
                // It sees the element already selected and therefore skips SetSelection().
                SetRuntimeSafeSelection([element]);
            }
            return;
        }

        if (ReferenceEquals(e.OriginalSource, PreviewCanvas))
        {
            SetRuntimeSafeSelection([]);
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

        e.Handled = true;
    }

    private void RuntimeSelectionPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!RuntimeSelectionPreviewActive || Keyboard.FocusedElement is TextBox)
            return;

        var control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        if (control && e.Key == Key.A)
        {
            SetRuntimeSafeSelection(_layout.Elements);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Temporarily gates only the legacy selection synchronizers. This scope is strictly
    /// synchronous and reference-counted so nested list synchronization is safe. It must
    /// never span a dispatcher turn or a context-menu command.
    /// </summary>
    private void BeginRuntimeSelectionWriterSuppression()
    {
        if (_runtimeSelectionWriterSuppressionDepth++ != 0)
            return;

        _runtimeSelectionPreviousWashingApplying = _washingRenderPipelineFixApplying;
        _runtimeSelectionPreviousMachineApplying = _machineRuntimeApplying;

        if (_washingExclusiveModeActive && IsWashingPhase6Active)
            _washingRenderPipelineFixApplying = true;
        if (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive)
            _machineRuntimeApplying = true;
    }

    private void EndRuntimeSelectionWriterSuppression()
    {
        if (_runtimeSelectionWriterSuppressionDepth <= 0)
            return;

        if (--_runtimeSelectionWriterSuppressionDepth != 0)
            return;

        _washingRenderPipelineFixApplying = _runtimeSelectionPreviousWashingApplying;
        _machineRuntimeApplying = _runtimeSelectionPreviousMachineApplying;
    }

    private void QueueRuntimeSelectionOuterNormalization()
    {
        if (!RuntimeSelectionPreviewActive)
            return;

        var token = ++_runtimeSelectionNormalizeToken;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (token != _runtimeSelectionNormalizeToken || !RuntimeSelectionPreviewActive)
                return;

            NormalizeRuntimeSelectionOuterChrome();
            RefreshRuntimeSelectionOverlaysOnly();
        }));
    }

    private void NormalizeRuntimeSelectionOuterChrome()
    {
        if (_washingExclusiveModeActive && IsWashingPhase6Active)
            NormalizeWashingSelectionChrome();
        else if (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive)
            NormalizeMachineRuntimeSelectionChrome();
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
