using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private bool _compactContextMenusInitialized;
    private ContextMenu? _workspaceContextMenu;

    public void EnableCompactContextMenus()
    {
        if (_compactContextMenusInitialized)
            return;
        _compactContextMenusInitialized = true;

        RemoveMovedToolbarCommands();

        _workspaceContextMenu = new ContextMenu
        {
            Background = Brush("#20252B"),
            Foreground = Brush("#F2F5F7"),
            BorderBrush = Brush("#46515B"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            HasDropShadow = false
        };
        InstallDarkContextMenuTheme(_workspaceContextMenu);
        _workspaceContextMenu.Opened += (_, _) => RebuildWorkspaceContextMenu();

        PreviewCanvas.ContextMenu = _workspaceContextMenu;
        ElementsList.ContextMenu = _workspaceContextMenu;
        PreviewCanvas.PreviewMouseRightButtonDown += WorkspacePreviewMouseRightButtonDown;
        ElementsList.PreviewMouseRightButtonDown += ElementsListPreviewMouseRightButtonDown;
        PreviewKeyDown += CompactContextMenuPreviewKeyDown;
    }

    private void RemoveMovedToolbarCommands()
    {
        var mainBar = ZoomCombo.Parent as Panel;
        if (mainBar is not null)
        {
            RemoveToolbarSection(mainBar, "ADD", child =>
                child is Button { Tag: string tag } && tag is "Slot" or "Text" or "Button" or "Texture" or "PlayerInventory" or "Hotbar");
            RemoveToolbarSection(mainBar, "EDIT", child => ReferenceEquals(child, UndoButton) || ReferenceEquals(child, RedoButton));
        }

        RemoveSkyblockText("Project-aware machine preview");

        RemoveSkyblockSection("WIDGETS", child =>
            child is Button { Tag: string tag } && tag is "panel" or "meter" or "lock" or "status");
        RemoveSkyblockSection("POSITION", child =>
            child is Button button && button.Content?.ToString() is "Distribute H" or "Distribute V");

        RemoveSkyblockButton("Export Java", removePrecedingSeparator: true);
        RemoveSkyblockButton("Reload source", removePrecedingSeparator: false);
        RemoveSkyblockButton("Validate", removePrecedingSeparator: false);
        RemoveSkyblockButton("Review Java…", removePrecedingSeparator: false);

        if (_phase9LiveReloadToggle?.Parent is Panel liveParent)
            liveParent.Children.Remove(_phase9LiveReloadToggle);

        RemoveSkyblockLabel("PHASE 9", removePrecedingSeparator: true);

        if (_machineRuntimeSourceText?.Parent is Panel sourceParent)
            sourceParent.Children.Remove(_machineRuntimeSourceText);

        RelocateTimelineToMainToolbar(mainBar);
    }

    private void RelocateTimelineToMainToolbar(Panel? mainBar)
    {
        if (mainBar is null)
            return;

        var timelineLabel = SkyblockBarHost.Children.OfType<TextBlock>()
            .FirstOrDefault(text => string.Equals(text.Text, "TIMELINE", StringComparison.Ordinal));
        if (timelineLabel is null)
            return;

        var separator = PreviousBorder(SkyblockBarHost, timelineLabel);
        var play = SkyblockBarHost.Children.OfType<Button>().FirstOrDefault(button => Equals(button.Content, "▶"));
        var pause = SkyblockBarHost.Children.OfType<Button>().FirstOrDefault(button => Equals(button.Content, "Ⅱ"));
        var stop = SkyblockBarHost.Children.OfType<Button>().FirstOrDefault(button => Equals(button.Content, "■"));

        var timelineControls = new UIElement?[]
        {
            timelineLabel,
            play,
            pause,
            stop,
            _phase8LoopBox,
            _phase8TimelineSlider,
            _phase8TimelineText,
            _phase8SpeedBox
        }
        .Where(control => control is not null)
        .Cast<UIElement>()
        .ToList();

        if (separator is not null)
            SkyblockBarHost.Children.Remove(separator);
        foreach (var control in timelineControls)
        {
            if (control is FrameworkElement { Parent: Panel parent })
                parent.Children.Remove(control);
        }

        mainBar.Children.Add(SeparatorBar());
        foreach (var control in timelineControls)
            mainBar.Children.Add(control);
    }

    private static void InstallDarkContextMenuTheme(ContextMenu menu)
    {
        InstallDarkMenuResources(menu.Resources);

        var itemStyle = new Style(typeof(MenuItem));
        itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#F2F5F7")));
        itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brush("#20252B")));
        itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 4, 8, 4)));
        itemStyle.Setters.Add(new Setter(Control.FontSizeProperty, 12.0));
        var disabled = new Trigger { Property = UIElement.IsEnabledProperty, Value = false };
        disabled.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#75818A")));
        itemStyle.Triggers.Add(disabled);
        menu.Resources[typeof(MenuItem)] = itemStyle;
    }

    private static void InstallDarkMenuResources(ResourceDictionary resources)
    {
        resources[SystemColors.MenuBrushKey] = Brush("#20252B");
        resources[SystemColors.MenuTextBrushKey] = Brush("#F2F5F7");
        resources[SystemColors.HighlightBrushKey] = Brush("#35576A");
        resources[SystemColors.HighlightTextBrushKey] = Brush("#FFFFFF");
        resources[SystemColors.GrayTextBrushKey] = Brush("#75818A");
        resources[SystemColors.ControlBrushKey] = Brush("#20252B");
        resources[SystemColors.ControlTextBrushKey] = Brush("#F2F5F7");
        resources[SystemColors.WindowBrushKey] = Brush("#20252B");
        resources[SystemColors.WindowTextBrushKey] = Brush("#F2F5F7");
    }

    private static void RemoveToolbarSection(Panel host, string label, Func<UIElement, bool> belongsToSection)
    {
        var sectionLabel = host.Children.OfType<TextBlock>()
            .FirstOrDefault(text => string.Equals(text.Text, label, StringComparison.Ordinal));
        if (sectionLabel is null)
            return;

        var separator = PreviousBorder(host, sectionLabel);
        var toRemove = host.Children.Cast<UIElement>().Where(belongsToSection).ToList();
        foreach (var child in toRemove)
            host.Children.Remove(child);
        host.Children.Remove(sectionLabel);
        if (separator is not null)
            host.Children.Remove(separator);
    }

    private void RemoveSkyblockSection(string label, Func<UIElement, bool> belongsToSection)
    {
        var sectionLabel = SkyblockBarHost.Children.OfType<TextBlock>()
            .FirstOrDefault(text => string.Equals(text.Text, label, StringComparison.Ordinal));
        if (sectionLabel is null)
            return;

        var separator = PreviousBorder(SkyblockBarHost, sectionLabel);
        var toRemove = SkyblockBarHost.Children.Cast<UIElement>().Where(belongsToSection).ToList();
        foreach (var child in toRemove)
            SkyblockBarHost.Children.Remove(child);
        SkyblockBarHost.Children.Remove(sectionLabel);
        if (separator is not null)
            SkyblockBarHost.Children.Remove(separator);
    }

    private void RemoveSkyblockText(string text)
    {
        var label = SkyblockBarHost.Children.OfType<TextBlock>()
            .FirstOrDefault(candidate => string.Equals(candidate.Text, text, StringComparison.Ordinal));
        if (label is not null)
            SkyblockBarHost.Children.Remove(label);
    }

    private void RemoveSkyblockLabel(string text, bool removePrecedingSeparator)
    {
        var label = SkyblockBarHost.Children.OfType<TextBlock>()
            .FirstOrDefault(candidate => string.Equals(candidate.Text, text, StringComparison.Ordinal));
        if (label is null)
            return;

        var separator = removePrecedingSeparator ? PreviousBorder(SkyblockBarHost, label) : null;
        SkyblockBarHost.Children.Remove(label);
        if (separator is not null)
            SkyblockBarHost.Children.Remove(separator);
    }

    private void RemoveSkyblockButton(string content, bool removePrecedingSeparator)
    {
        var button = SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(candidate => string.Equals(candidate.Content?.ToString(), content, StringComparison.Ordinal));
        if (button is null)
            return;

        Border? separator = null;
        if (removePrecedingSeparator)
            separator = PreviousBorder(SkyblockBarHost, button);

        SkyblockBarHost.Children.Remove(button);
        if (separator is not null)
            SkyblockBarHost.Children.Remove(separator);
    }

    private static Border? PreviousBorder(Panel host, UIElement child)
    {
        var index = host.Children.IndexOf(child);
        return index > 0 && host.Children[index - 1] is Border border ? border : null;
    }

    private void WorkspacePreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var element = FindGuiElementFromVisual(e.OriginalSource as DependencyObject);
        if (element is not null && !_selection.Contains(element))
            SetSelection([element]);
    }

    private void ElementsListPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source)
            return;

        var container = ItemsControl.ContainerFromElement(ElementsList, source) as ListBoxItem;
        if (container?.DataContext is GuiElementModel element && !_selection.Contains(element))
            SetSelection([element]);
    }

    private GuiElementModel? FindGuiElementFromVisual(DependencyObject? source)
    {
        var current = source;
        while (current is not null && !ReferenceEquals(current, PreviewCanvas))
        {
            if (current is FrameworkElement { Tag: GuiElementModel model } && _layout.Elements.Contains(model))
                return model;

            current = current switch
            {
                Visual or Visual3D => VisualTreeHelper.GetParent(current),
                FrameworkContentElement content => content.Parent,
                _ => null
            };
        }
        return null;
    }

    private void RebuildWorkspaceContextMenu()
    {
        if (_workspaceContextMenu is null)
            return;

        _workspaceContextMenu.Items.Clear();
        var hasSelection = _selection.Count > 0;
        var singleText = _selection.Count == 1 && _selection.First().Type == GuiElementType.Text;

        if (singleText)
        {
            _workspaceContextMenu.Items.Add(ContextItem("Edit Text…", (_, _) => EditSelectedText(), true, "F2"));
            _workspaceContextMenu.Items.Add(new Separator());
        }

        _workspaceContextMenu.Items.Add(ContextItem("Cut", (_, _) => CutSelection(), hasSelection, "Ctrl+X"));
        _workspaceContextMenu.Items.Add(ContextItem("Copy", (_, _) => CopySelection(), hasSelection, "Ctrl+C"));
        _workspaceContextMenu.Items.Add(ContextItem("Paste", (_, _) => PasteSelection(), _clipboardElements.Count > 0, "Ctrl+V"));
        _workspaceContextMenu.Items.Add(ContextItem("Delete", (_, _) => DeleteSelection(), hasSelection, "Del"));

        if (hasSelection)
        {
            _workspaceContextMenu.Items.Add(new Separator());
            _workspaceContextMenu.Items.Add(ContextItem("Bring to front", BringFront_Click));
            _workspaceContextMenu.Items.Add(ContextItem("Send to back", SendBack_Click));
        }

        _workspaceContextMenu.Items.Add(new Separator());
        _workspaceContextMenu.Items.Add(BuildAddMenu());
        _workspaceContextMenu.Items.Add(BuildEditMenu());
        _workspaceContextMenu.Items.Add(BuildPositionMenu());
        _workspaceContextMenu.Items.Add(BuildSourceMenu());
    }

    private MenuItem BuildAddMenu()
    {
        var add = ContextSubmenu("Add");
        add.Items.Add(AddElementMenuItem("Slot", GuiElementType.Slot));
        add.Items.Add(AddElementMenuItem("Text", GuiElementType.Text));
        add.Items.Add(AddElementMenuItem("Button", GuiElementType.Button));
        add.Items.Add(AddElementMenuItem("Texture", GuiElementType.Texture));
        add.Items.Add(AddElementMenuItem("Inventory", GuiElementType.PlayerInventory));
        add.Items.Add(AddElementMenuItem("Hotbar", GuiElementType.Hotbar));
        add.Items.Add(new Separator());

        var widgets = ContextSubmenu("Widgets");
        widgets.Items.Add(AddWidgetMenuItem("Panel", "panel"));
        widgets.Items.Add(AddWidgetMenuItem("Meter", "meter"));
        widgets.Items.Add(AddWidgetMenuItem("Lock", "lock"));
        widgets.Items.Add(AddWidgetMenuItem("Status", "status"));
        add.Items.Add(widgets);
        return add;
    }

    private MenuItem BuildEditMenu()
    {
        var edit = ContextSubmenu("Edit");
        edit.Items.Add(ContextItem("Undo", (_, _) => Undo(), _undo.Count > 0, "Ctrl+Z"));
        edit.Items.Add(ContextItem("Redo", (_, _) => Redo(), _redo.Count > 0, "Ctrl+Y"));
        edit.Items.Add(new Separator());
        edit.Items.Add(ContextItem("Select all", (_, _) => SetSelection(_layout.Elements), _layout.Elements.Count > 0, "Ctrl+A"));
        return edit;
    }

    private MenuItem BuildPositionMenu()
    {
        var position = ContextSubmenu("Position");
        var canDistribute = _selection.Count >= 3;
        position.Items.Add(ContextItem("Distribute horizontally", (_, _) => DistributePhase8Selection(horizontal: true), canDistribute));
        position.Items.Add(ContextItem("Distribute vertically", (_, _) => DistributePhase8Selection(horizontal: false), canDistribute));
        return position;
    }

    private MenuItem BuildSourceMenu()
    {
        var source = ContextSubmenu("Source / Java");
        source.Items.Add(ContextItem("Export Java…", JavaExport_Click));
        source.Items.Add(new Separator());

        var live = ContextSubmenu("Live Source");
        live.IsCheckable = true;
        live.IsChecked = _phase9LiveReloadToggle?.IsChecked == true;
        live.IsEnabled = _phase9LiveReloadToggle is not null;
        live.Click += (_, _) =>
        {
            if (_phase9LiveReloadToggle is not null)
                _phase9LiveReloadToggle.IsChecked = live.IsChecked;
        };
        source.Items.Add(live);
        source.Items.Add(ContextItem("Reload Source", (_, _) => ManualPhase9SourceReload(), !string.IsNullOrWhiteSpace(_repoRoot)));
        source.Items.Add(ContextItem("Validate", (_, _) => RunPhase9Validation(force: true), true));
        source.Items.Add(ContextItem(
            "Review Java…",
            ReviewPhase9Java_Click,
            _phase9ReviewButton?.IsEnabled == true));
        return source;
    }

    private MenuItem AddElementMenuItem(string header, GuiElementType type)
    {
        var item = ContextItem(header, AddElement_Click);
        item.Tag = type.ToString();
        return item;
    }

    private MenuItem AddWidgetMenuItem(string header, string kind)
    {
        var item = ContextItem(header, AddForeverWidget_Click);
        item.Tag = kind;
        return item;
    }

    private static MenuItem ContextSubmenu(string header)
    {
        var item = new MenuItem
        {
            Header = header,
            Background = Brush("#20252B"),
            Foreground = Brush("#F2F5F7")
        };
        InstallDarkMenuResources(item.Resources);
        return item;
    }

    private static MenuItem ContextItem(
        string header,
        RoutedEventHandler handler,
        bool enabled = true,
        string? gesture = null)
    {
        var item = new MenuItem
        {
            Header = header,
            IsEnabled = enabled,
            InputGestureText = gesture ?? string.Empty,
            Background = Brush("#20252B"),
            Foreground = enabled ? Brush("#F2F5F7") : Brush("#75818A")
        };
        InstallDarkMenuResources(item.Resources);
        item.Click += handler;
        return item;
    }

    private void CutSelection()
    {
        if (_selection.Count == 0)
            return;

        CopySelection();
        DeleteSelection();
    }

    private void CompactContextMenuPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement is TextBox)
            return;

        var control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        if (control && e.Key == Key.X)
        {
            CutSelection();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F2 && _selection.Count == 1 && _selection.First().Type == GuiElementType.Text)
        {
            EditSelectedText();
            e.Handled = true;
        }
    }

    private void EditSelectedText()
    {
        if (_selection.Count != 1 || _selection.First() is not { Type: GuiElementType.Text } element)
            return;

        var editor = new TextBox
        {
            Text = element.Content ?? string.Empty,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MinHeight = 90,
            Margin = new Thickness(0, 0, 0, 12)
        };

        var apply = new Button { Content = "Apply", Width = 90, IsDefault = true };
        var cancel = new Button { Content = "Cancel", Width = 90, IsCancel = true };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(apply);

        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(new TextBlock
        {
            Text = element.Id,
            Foreground = Brush("#8DB9E8"),
            Margin = new Thickness(0, 0, 0, 8)
        });
        panel.Children.Add(editor);
        panel.Children.Add(buttons);

        var dialog = new Window
        {
            Owner = this,
            Title = "Edit Text",
            Width = 460,
            Height = 230,
            MinWidth = 360,
            MinHeight = 190,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Brush("#202327"),
            Foreground = Brush("#E8E8E8"),
            Content = panel,
            ShowInTaskbar = false
        };

        apply.Click += (_, _) => dialog.DialogResult = true;
        dialog.Loaded += (_, _) =>
        {
            editor.Focus();
            editor.SelectAll();
        };

        if (dialog.ShowDialog() != true || string.Equals(element.Content, editor.Text, StringComparison.Ordinal))
            return;

        PushUndo();
        element.Content = editor.Text;
        RenderLayout();
        UpdateInspector();
        MarkDirty();
    }
}
