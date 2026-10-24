using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Keeps editor selection out of source-faithful machine chrome.
///
/// The exclusive Washing/non-Washing owners remain the only render owners. This helper
/// only establishes selection before the legacy SetSelection path can paint generic
/// Border chrome. It never schedules presentation work and never keeps a render-owner
/// re-entrancy flag across routed events or dispatcher turns.
/// </summary>
public partial class MainWindow
{
    private bool _runtimeSelectionStabilityInitialized;
    private int _runtimeSelectionWriterSuppressionDepth;
    private bool _runtimeSelectionPreviousWashingApplying;
    private bool _runtimeSelectionPreviousMachineApplying;

    public void EnableRuntimeSelectionStabilityFix()
    {
        if (_runtimeSelectionStabilityInitialized)
            return;
        _runtimeSelectionStabilityInitialized = true;

        // Tunnelling left-click selection runs before the base element/list/tree
        // handlers. Runtime-safe selection is completed synchronously here. No matching
        // bubbling callback is required, so no state can leak into a later edit command.
        PreviewCanvas.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(RuntimeSelectionPreviewMouseLeftButtonDown), true);
        ElementsList.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(RuntimeSelectionPreviewMouseLeftButtonDown), true);
        if (_phase8Hierarchy is not null)
        {
            _phase8Hierarchy.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(RuntimeSelectionPreviewMouseLeftButtonDown), true);
        }

        PreviewKeyDown += RuntimeSelectionPreviewKeyDown;
    }

    private bool RuntimeSelectionPreviewActive
        => (_washingExclusiveModeActive && IsWashingPhase6Active) ||
           (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive);

    private void RuntimeSelectionPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!RuntimeSelectionPreviewActive)
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
                // Ctrl-click is completely owned here. Letting the base element handler
                // continue would toggle the same element a second time.
                ToggleRuntimeSafeSelection(element);
                e.Handled = true;
            }
            else if (!_selection.Contains(element))
            {
                // Preselect without touching runtime chrome. Keep the event unhandled so
                // the established base drag-start logic still captures the mouse. It sees
                // the element already selected and therefore skips SetSelection().
                SetRuntimeSafeSelection([element]);
            }
            return;
        }

        if (ReferenceEquals(e.OriginalSource, PreviewCanvas))
        {
            SetRuntimeSafeSelection([]);
            // Prevent PreviewCanvas_MouseLeftButtonDown from entering SetSelection([]).
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

        // We synchronized SelectedItems ourselves; suppress the ListBox's normal
        // mutation so ElementsList_SelectionChanged cannot call SetSelection().
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

        // Prevent TreeView.SelectedItemChanged from calling SetSelection([model]).
        e.Handled = true;
    }

    private void RuntimeSelectionPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!RuntimeSelectionPreviewActive || Keyboard.FocusedElement is TextBox)
            return;

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.A)
        {
            SetRuntimeSafeSelection(_layout.Elements);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Suppresses only the legacy selection synchronizers while SelectedItems is being
    /// changed by SetRuntimeSafeSelection. The scope is reference-counted and strictly
    /// synchronous; callers must pair it in try/finally and it must never span an input
    /// event, context-menu command or dispatcher callback.
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
}
