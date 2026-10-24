using System.Windows;
using System.Windows.Controls;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private bool _contextMenuVisualPolishInitialized;
    private Style? _designerContextMenuItemStyle;
    private Style? _designerContextMenuSeparatorStyle;

    public void EnableContextMenuVisualPolish()
    {
        if (_contextMenuVisualPolishInitialized || _workspaceContextMenu is null)
            return;

        _contextMenuVisualPolishInitialized = true;

        if (Application.Current.TryFindResource("DesignerContextMenuStyle") is Style menuStyle)
            _workspaceContextMenu.Style = menuStyle;
        _designerContextMenuItemStyle = Application.Current.TryFindResource("DesignerContextMenuItemStyle") as Style;
        _designerContextMenuSeparatorStyle = Application.Current.TryFindResource("DesignerContextMenuSeparatorStyle") as Style;

        // RebuildWorkspaceContextMenu is registered before this handler. Styling after
        // every rebuild ensures dynamically-created leaf and submenu items all use the
        // same custom chrome instead of Windows' native icon gutter/template.
        _workspaceContextMenu.Opened += (_, _) => ApplyDesignerContextMenuStyles();
        ApplyDesignerContextMenuStyles();
    }

    private void ApplyDesignerContextMenuStyles()
    {
        if (_workspaceContextMenu is null)
            return;

        foreach (var item in _workspaceContextMenu.Items)
            ApplyDesignerContextMenuStyleRecursive(item);
    }

    private void ApplyDesignerContextMenuStyleRecursive(object? item)
    {
        switch (item)
        {
            case MenuItem menuItem:
                if (_designerContextMenuItemStyle is not null)
                    menuItem.Style = _designerContextMenuItemStyle;
                foreach (var child in menuItem.Items)
                    ApplyDesignerContextMenuStyleRecursive(child);
                break;

            case Separator separator:
                if (_designerContextMenuSeparatorStyle is not null)
                    separator.Style = _designerContextMenuSeparatorStyle;
                break;
        }
    }
}
