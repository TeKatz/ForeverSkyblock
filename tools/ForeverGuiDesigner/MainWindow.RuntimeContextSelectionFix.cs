using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Runtime/source-faithful machine previews must never repaint their real controls
/// merely because the editor selection changed. Keep selection state in the model,
/// list and dedicated adorners, and leave the machine visual tree untouched.
/// </summary>
public partial class MainWindow
{
    private bool _runtimeContextSelectionFixInitialized;

    public void EnableRuntimeContextSelectionFix()
    {
        if (_runtimeContextSelectionFixInitialized)
            return;
        _runtimeContextSelectionFixInitialized = true;

        // EnableCompactContextMenus registered generic right-click selection handlers.
        // Runtime previews need the safe selection path instead.
        PreviewCanvas.PreviewMouseRightButtonDown -= WorkspacePreviewMouseRightButtonDown;
        ElementsList.PreviewMouseRightButtonDown -= ElementsListPreviewMouseRightButtonDown;

        PreviewCanvas.PreviewMouseRightButtonDown += RuntimeSafeWorkspaceRightButtonDown;
        ElementsList.PreviewMouseRightButtonDown += RuntimeSafeElementsListRightButtonDown;
    }

    private void RuntimeSafeWorkspaceRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var element = FindGuiElementFromVisual(e.OriginalSource as DependencyObject);
        if (element is not null && !_selection.Contains(element))
            SelectForContextMenu(element);
    }

    private void RuntimeSafeElementsListRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source)
            return;

        var container = ItemsControl.ContainerFromElement(ElementsList, source) as ListBoxItem;
        if (container?.DataContext is GuiElementModel element && !_selection.Contains(element))
            SelectForContextMenu(element);
    }

    private void SelectForContextMenu(GuiElementModel element)
    {
        if (!RuntimeSelectionPreviewActive)
        {
            SetSelection([element]);
            return;
        }

        SetRuntimeSafeSelection([element]);
    }

    private void SetRuntimeSafeSelection(IEnumerable<GuiElementModel> elements)
    {
        _selection.Clear();
        foreach (var element in elements.Where(_layout.Elements.Contains))
            _selection.Add(element);

        // ElementsList.SelectedItems raises SelectionChanged synchronously and the
        // exclusive runtime owners listen to that event. Gate only this exact mutation,
        // then restore the owner flags before returning. No suppression survives into a
        // routed-event continuation, dispatcher callback or later context-menu command.
        BeginRuntimeSelectionWriterSuppression();
        try
        {
            _suppressSelectionChange = true;
            try
            {
                ElementsList.SelectedItems.Clear();
                foreach (var selected in _selection)
                    ElementsList.SelectedItems.Add(selected);
            }
            finally
            {
                _suppressSelectionChange = false;
            }
        }
        finally
        {
            EndRuntimeSelectionWriterSuppression();
        }

        UpdateInspector();
        UpdateStatus();
        RefreshRuntimeSelectionOverlaysOnly();
    }

    private void ToggleRuntimeSafeSelection(GuiElementModel element)
    {
        var updated = _selection.ToList();
        if (updated.Contains(element))
            updated.Remove(element);
        else if (_layout.Elements.Contains(element))
            updated.Add(element);
        SetRuntimeSafeSelection(updated);
    }

    private void RefreshRuntimeSelectionOverlaysOnly()
    {
        if (_washingExclusiveModeActive && IsWashingPhase6Active)
            UpdateWashingSelectionAdorners();
        else if (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive)
            UpdateMachineRuntimeSelectionAdorners();

        if (_phase8Initialized)
            EnsurePhase8OverlayVisuals();
    }
}
