using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

/// <summary>
/// Right-click selection is special in the designer: opening the context menu may
/// select the element under the pointer before the menu is shown. The base SetSelection
/// path calls UpdateSelectionVisuals synchronously, which repaints the real element
/// borders with generic editor chrome. Source-faithful machine previews must never be
/// modified just because their context menu opens.
///
/// Replace only the context-menu selection handlers. Normal editor layouts keep using
/// SetSelection; exclusive machine previews update the model/list selection plus their
/// dedicated selection adorners without touching any machine visual.
/// </summary>
public partial class MainWindow
{
    private bool _runtimeContextSelectionFixInitialized;

    public void EnableRuntimeContextSelectionFix()
    {
        if (_runtimeContextSelectionFixInitialized)
            return;
        _runtimeContextSelectionFixInitialized = true;

        // EnableCompactContextMenus registered the original handlers immediately before
        // this initializer runs. Remove exactly those handlers and replace them with the
        // runtime-safe variants below.
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

        _selection.Clear();
        if (_layout.Elements.Contains(element))
            _selection.Add(element);

        // Mirror SetSelection's list synchronization, but deliberately skip
        // UpdateSelectionVisuals(). That method writes generic BorderThickness and
        // BorderBrush values into the real runtime controls and caused the persistent
        // visual jump when the context menu was opened.
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

        UpdateInspector();
        UpdateStatus();

        if (_washingExclusiveModeActive && IsWashingPhase6Active)
            UpdateWashingSelectionAdorners();
        else if (_machineRuntimeExclusiveActive && IsNonWashingRuntimeMachineActive)
            UpdateMachineRuntimeSelectionAdorners();

        if (_phase8Initialized)
            EnsurePhase8OverlayVisuals();
    }
}
