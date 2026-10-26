using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17R turns the Phase-17Q single-window shell into a real workstation layout.
/// Existing viewport, inspector and timeline controls are moved between dock hosts and tool windows;
/// they are never cloned, so all Phase-17B..17Q event handlers, dirty tracking and persistence keep
/// operating on the same control instances. The inspector is also partitioned into focused pages to
/// remove the accumulated vertical scroll wall without deleting any established authoring function.
/// </summary>
public partial class MainWindow
{
    private enum StudioPhase17RPane
    {
        Viewport,
        Inspector,
        Timeline
    }

    private bool _studioAnimationPhase17RInitialized;
    private bool _studioPhase17RRestoringLayout;
    private bool _studioPhase17RShuttingDown;
    private bool _studioPhase17RViewportFullscreen;
    private WindowState _studioPhase17RViewportStateBeforeFullscreen = WindowState.Normal;

    private Grid? _studioPhase17RRoot;
    private GridSplitter? _studioPhase17RVerticalSplitter;
    private GridSplitter? _studioPhase17RHorizontalSplitter;
    private TabControl? _studioPhase17RInspectorTabs;
    private Button? _studioPhase17RWorkspaceButton;
    private FrameworkElement? _studioPhase17RViewportWindowHeader;

    private double _studioPhase17RDockedInspectorWidth = 600;
    private double _studioPhase17RDockedTimelineHeight = 82;
    private ForeverStudioWorkspaceLayout _studioPhase17RLayout = new();

    private readonly Dictionary<StudioPhase17RPane, Border> _studioPhase17RCards = [];
    private readonly Dictionary<StudioPhase17RPane, Window> _studioPhase17RWindows = [];
    private readonly Dictionary<StudioPhase17RPane, ContentControl> _studioPhase17RWindowHosts = [];
    private readonly HashSet<StudioPhase17RPane> _studioPhase17RDetachedPanes = [];
    private readonly HashSet<StudioPhase17RPane> _studioPhase17RInitialWindowStateApplied = [];

    public void EnableForeverAnimationPhase17R()
    {
        if (_studioAnimationPhase17RInitialized ||
            !_studioAnimationPhase17QInitialized ||
            _studioAnimationSurface is not Grid root ||
            root.RowDefinitions.Count < 4 ||
            root.ColumnDefinitions.Count < 3 ||
            _studioAnimationViewport?.Parent is not Grid viewportGrid ||
            viewportGrid.Parent is not Border viewportCard ||
            _studioAnimationStatusText?.Parent is not StackPanel inspector ||
            inspector.Parent is not ScrollViewer inspectorScroll ||
            inspectorScroll.Parent is not Border inspectorCard)
            return;

        var timelineCard = root.Children
            .OfType<Border>()
            .FirstOrDefault(border => Grid.GetRow(border) == 3);
        if (timelineCard is null)
            return;

        _studioAnimationPhase17RInitialized = true;
        _studioPhase17RRoot = root;
        _studioPhase17RCards[StudioPhase17RPane.Viewport] = viewportCard;
        _studioPhase17RCards[StudioPhase17RPane.Inspector] = inspectorCard;
        _studioPhase17RCards[StudioPhase17RPane.Timeline] = timelineCard;
        _studioPhase17RVerticalSplitter = root.Children
            .OfType<GridSplitter>()
            .FirstOrDefault(splitter => Grid.GetColumn(splitter) == 1);
        _studioPhase17RHorizontalSplitter = root.Children
            .OfType<GridSplitter>()
            .FirstOrDefault(splitter => Grid.GetRow(splitter) == 2);

        _studioPhase17RLayout = ForeverStudioWorkspaceLayoutStore.Load();
        _studioPhase17RDockedInspectorWidth = _studioPhase17RLayout.InspectorWidth;
        _studioPhase17RDockedTimelineHeight = _studioPhase17RLayout.TimelineHeight;

        BuildStudioPhase17RInspector(inspectorCard, inspectorScroll, inspector);
        InstallStudioPhase17RWorkspaceCommand(root);
        UpdateStudioPhase17RViewportBadge(viewportGrid);
        ApplyStudioPhase17RDockedGeometry();

        if (_studioPhase17RInspectorTabs is not null)
            _studioPhase17RInspectorTabs.SelectedIndex = Math.Clamp(_studioPhase17RLayout.InspectorTabIndex, 0, 3);

        Loaded += StudioPhase17RLoaded;
        Closing += StudioPhase17RMainWindowClosing;
        if (_studioAnimationSurface is not null)
            _studioAnimationSurface.IsVisibleChanged += (_, _) => UpdateStudioPhase17RToolWindowVisibility();
    }

    private void BuildStudioPhase17RInspector(
        Border inspectorCard,
        ScrollViewer legacyScroll,
        StackPanel legacyInspector)
    {
        var originalChildren = legacyInspector.Children.Cast<UIElement>().ToList();
        var sourceHost = StudioPhase17RInspectorHost();
        var targetHost = StudioPhase17RInspectorHost();
        var channelsHost = StudioPhase17RInspectorHost();
        var projectHost = StudioPhase17RInspectorHost();

        var runtimeGrid = FindStudioPhase17RDirectChild(legacyInspector, _studioAnimationRuntimeCurveCombo);
        var runtimeStateRoot = FindStudioPhase17RDirectChild(legacyInspector, _studioRuntimeStateSummaryText) as StackPanel;
        var targetStart = FindStudioPhase17RDirectChild(legacyInspector, _studioAnimationTimeBox);
        var projectStart = FindStudioPhase17RDirectChild(legacyInspector, _studioAnimationStatusText);
        var targetStartIndex = targetStart is null ? -1 : originalChildren.IndexOf(targetStart);
        var projectStartIndex = projectStart is null ? -1 : originalChildren.IndexOf(projectStart);

        if (runtimeStateRoot is not null)
        {
            foreach (var channelSection in runtimeStateRoot.Children
                         .OfType<StackPanel>()
                         .Where(IsStudioPhase17RChannelSection)
                         .ToList())
            {
                runtimeStateRoot.Children.Remove(channelSection);
                channelsHost.Children.Add(channelSection);
            }
        }

        MoveStudioPhase17RInspectorElement(runtimeGrid, sourceHost);
        MoveStudioPhase17RInspectorElement(_studioAnimationRuntimeValueText, sourceHost);
        MoveStudioPhase17RInspectorElement(runtimeStateRoot, sourceHost);

        if (targetStartIndex >= 0 && projectStartIndex > targetStartIndex)
        {
            foreach (var element in originalChildren.Skip(targetStartIndex).Take(projectStartIndex - targetStartIndex))
            {
                if (ReferenceEquals((element as FrameworkElement)?.Parent, legacyInspector))
                    MoveStudioPhase17RInspectorElement(element, targetHost);
            }
        }

        if (projectStartIndex >= 0)
        {
            foreach (var element in originalChildren.Skip(projectStartIndex))
            {
                if (ReferenceEquals((element as FrameworkElement)?.Parent, legacyInspector))
                    MoveStudioPhase17RInspectorElement(element, projectHost);
            }
        }

        CompactStudioPhase17RTargetInspector(targetHost);
        CompactStudioPhase17RSourceInspector(sourceHost);
        CompactStudioPhase17RChannelsInspector(channelsHost);
        CompactStudioPhase17RProjectInspector(projectHost);

        _studioPhase17RInspectorTabs = new TabControl
        {
            Background = Brush("#101619"),
            Foreground = Brush("#E4ECEF"),
            BorderBrush = Brush("#35464F"),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };
        _studioPhase17RInspectorTabs.Items.Add(StudioPhase17RInspectorTab("MACHINE", sourceHost));
        _studioPhase17RInspectorTabs.Items.Add(StudioPhase17RInspectorTab("TARGET", targetHost));
        _studioPhase17RInspectorTabs.Items.Add(StudioPhase17RInspectorTab("CHANNELS", channelsHost));
        _studioPhase17RInspectorTabs.Items.Add(StudioPhase17RInspectorTab("PROJECT", projectHost));

        inspectorCard.Child = _studioPhase17RInspectorTabs;
        inspectorCard.Padding = new Thickness(0);
        legacyScroll.Content = legacyInspector;
    }

    private static StackPanel StudioPhase17RInspectorHost()
        => new()
        {
            Margin = new Thickness(14, 12, 14, 14)
        };

    private TabItem StudioPhase17RInspectorTab(string title, StackPanel host)
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
            Content = host,
            Background = Brush("#101619")
        };
        return new TabItem
        {
            Header = title,
            Foreground = Brush("#DDE8EC"),
            Background = Brush("#1A252B"),
            BorderBrush = Brush("#35464F"),
            Padding = new Thickness(11, 6, 11, 6),
            Content = scroll
        };
    }

    private static UIElement? FindStudioPhase17RDirectChild(Panel parent, DependencyObject? descendant)
    {
        DependencyObject? current = descendant;
        while (current is FrameworkElement element)
        {
            if (ReferenceEquals(element.Parent, parent))
                return element;
            current = element.Parent;
        }
        return null;
    }

    private static void MoveStudioPhase17RInspectorElement(UIElement? element, Panel destination)
    {
        if (element is null)
            return;
        if (element is FrameworkElement { Parent: Panel parent })
            parent.Children.Remove(element);
        destination.Children.Add(element);
    }

    private static bool IsStudioPhase17RChannelSection(StackPanel panel)
        => panel.Children.OfType<TextBlock>().Any(text =>
            text.Text.StartsWith("LIGHT / STATUS TIMELINE", StringComparison.OrdinalIgnoreCase) ||
            text.Text.StartsWith("SPECIAL EFFECT TIMELINE", StringComparison.OrdinalIgnoreCase));

    private void CompactStudioPhase17RSourceInspector(StackPanel sourceHost)
    {
        CollapseStudioPhase17RExplanatoryCopy(sourceHost);
        if (_studioRuntimeStateControlsPanel is not null)
        {
            foreach (var row in _studioRuntimeStateControlsPanel.Children.OfType<StackPanel>())
                row.Margin = new Thickness(0, 0, 0, 5);
        }
        if (_studioAnimationRuntimeValueText is not null)
        {
            _studioAnimationRuntimeValueText.FontSize = 9;
            _studioAnimationRuntimeValueText.Margin = new Thickness(0, 5, 0, 2);
        }
    }

    private void CompactStudioPhase17RTargetInspector(StackPanel targetHost)
    {
        CollapseStudioPhase17RLegacyField(targetHost, _studioAnimationTextureStateBox);
        CollapseStudioPhase17RLegacyField(targetHost, _studioAnimationLightStatusBox);
        CollapseStudioPhase17RLegacyField(targetHost, _studioAnimationEffectBox);

        if (_studioAnimationMachineStateCombo?.Parent is StackPanel machineStatePanel)
        {
            machineStatePanel.Visibility = Visibility.Collapsed;
            if (machineStatePanel.Parent is Grid stateGrid && stateGrid.ColumnDefinitions.Count >= 2)
            {
                stateGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                stateGrid.ColumnDefinitions[1].Width = new GridLength(0);
            }
        }

        foreach (var commandRow in targetHost.Children.OfType<WrapPanel>())
        {
            if (commandRow.Children.OfType<Button>().Any(button =>
                    string.Equals(button.Content?.ToString(), "ADD / UPDATE", StringComparison.OrdinalIgnoreCase)))
                commandRow.Visibility = Visibility.Collapsed;
        }

        foreach (var text in targetHost.Children.OfType<TextBlock>())
        {
            if (text.Text.Equals("Interpolation", StringComparison.OrdinalIgnoreCase))
                text.Visibility = Visibility.Collapsed;
        }

        CollapseStudioPhase17RExplanatoryCopy(targetHost);
    }

    private void CompactStudioPhase17RChannelsInspector(StackPanel channelsHost)
    {
        CollapseStudioPhase17RExplanatoryCopy(channelsHost);
        if (_studioRuntimeStatusKeyframeList is not null)
            _studioRuntimeStatusKeyframeList.Height = 70;
        if (_studioRuntimeEffectKeyframeList is not null)
            _studioRuntimeEffectKeyframeList.Height = 70;
    }

    private void CompactStudioPhase17RProjectInspector(StackPanel projectHost)
    {
        CollapseStudioPhase17RExplanatoryCopy(projectHost);
        if (_studioAnimationStatusText is not null)
        {
            _studioAnimationStatusText.FontSize = 10;
            _studioAnimationStatusText.Margin = new Thickness(0, 2, 0, 8);
        }
    }

    private static void CollapseStudioPhase17RLegacyField(StackPanel host, FrameworkElement? control)
    {
        if (control is null || !ReferenceEquals(control.Parent, host))
            return;
        var index = host.Children.IndexOf(control);
        control.Visibility = Visibility.Collapsed;
        if (index > 0 && host.Children[index - 1] is TextBlock label)
            label.Visibility = Visibility.Collapsed;
    }

    private static void CollapseStudioPhase17RExplanatoryCopy(StackPanel root)
    {
        CollapseStudioPhase17RExplanatoryCopyInPanel(root);
        foreach (var child in root.Children.OfType<StackPanel>())
            CollapseStudioPhase17RExplanatoryCopy(child);
    }

    private static void CollapseStudioPhase17RExplanatoryCopyInPanel(StackPanel panel)
    {
        var children = panel.Children.Cast<UIElement>().ToList();
        for (var i = 0; i < children.Count; i++)
        {
            if (children[i] is not TextBlock heading || !IsStudioPhase17RSectionHeading(heading.Text))
                continue;

            for (var j = i + 1; j < children.Count && j <= i + 3; j++)
            {
                if (children[j] is not TextBlock copy ||
                    copy.TextWrapping != TextWrapping.Wrap ||
                    copy.FontSize < 10.5 ||
                    copy.Text.Length < 70)
                    continue;

                heading.ToolTip = copy.Text;
                copy.Visibility = Visibility.Collapsed;
                break;
            }
        }
    }

    private static bool IsStudioPhase17RSectionHeading(string text)
        => text.StartsWith("RUNTIME STATE", StringComparison.OrdinalIgnoreCase) ||
           text.StartsWith("LIGHT / STATUS TIMELINE", StringComparison.OrdinalIgnoreCase) ||
           text.StartsWith("SPECIAL EFFECT TIMELINE", StringComparison.OrdinalIgnoreCase) ||
           text.StartsWith("MATERIAL TIMELINE", StringComparison.OrdinalIgnoreCase) ||
           text.StartsWith("PROJECT ANIMATION DATA", StringComparison.OrdinalIgnoreCase);

    private void InstallStudioPhase17RWorkspaceCommand(Grid root)
    {
        var commandCard = root.Children
            .OfType<Border>()
            .FirstOrDefault(border => Grid.GetRow(border) == 0 && Grid.GetColumnSpan(border) >= 3);
        if (commandCard?.Child is not StackPanel shell)
            return;
        var commandRow = shell.Children.OfType<WrapPanel>().FirstOrDefault();
        if (commandRow is null)
            return;

        _studioPhase17RWorkspaceButton = StudioButton("WORKSPACE ▾", 112);
        _studioPhase17RWorkspaceButton.Height = 25;
        _studioPhase17RWorkspaceButton.Margin = new Thickness(4, 0, 0, 0);
        _studioPhase17RWorkspaceButton.ToolTip =
            "Dock or detach the viewport, inspector and timeline. Detached windows keep the same live editor controls.";
        _studioPhase17RWorkspaceButton.Click += (_, _) => OpenStudioPhase17RWorkspaceMenu();
        commandRow.Children.Add(_studioPhase17RWorkspaceButton);
    }

    private void OpenStudioPhase17RWorkspaceMenu()
    {
        if (_studioPhase17RWorkspaceButton is null)
            return;

        var menu = new ContextMenu
        {
            Background = Brush("#20252B"),
            Foreground = Brush("#F2F5F7"),
            BorderBrush = Brush("#46515B"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            HasDropShadow = false,
            PlacementTarget = _studioPhase17RWorkspaceButton
        };
        InstallDarkContextMenuTheme(menu);

        menu.Items.Add(StudioPhase17RMenuItem(
            "Two-screen · viewport window + controls",
            (_, _) => ApplyStudioPhase17RTwoScreenPreset()));
        menu.Items.Add(StudioPhase17RMenuItem(
            "Three-screen · viewport + controls + timeline",
            (_, _) => ApplyStudioPhase17RThreeScreenPreset()));
        menu.Items.Add(new Separator());
        menu.Items.Add(StudioPhase17RMenuItem(
            _studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Viewport) ? "Dock viewport" : "Detach viewport",
            (_, _) => ToggleStudioPhase17RPane(StudioPhase17RPane.Viewport)));
        menu.Items.Add(StudioPhase17RMenuItem(
            _studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Inspector) ? "Dock inspector" : "Detach inspector",
            (_, _) => ToggleStudioPhase17RPane(StudioPhase17RPane.Inspector)));
        menu.Items.Add(StudioPhase17RMenuItem(
            _studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Timeline) ? "Dock timeline" : "Detach timeline",
            (_, _) => ToggleStudioPhase17RPane(StudioPhase17RPane.Timeline)));

        if (_studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Viewport))
            menu.Items.Add(StudioPhase17RMenuItem("Viewport fullscreen · F11", (_, _) => ToggleStudioPhase17RViewportFullscreen()));

        menu.Items.Add(new Separator());
        menu.Items.Add(StudioPhase17RMenuItem("Dock all", (_, _) => DockAllStudioPhase17RPanes()));
        menu.Items.Add(StudioPhase17RMenuItem("Reset saved workspace", (_, _) => ResetStudioPhase17RWorkspace()));

        _studioPhase17RWorkspaceButton.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private static MenuItem StudioPhase17RMenuItem(string header, RoutedEventHandler handler)
    {
        var item = new MenuItem { Header = header };
        item.Click += handler;
        return item;
    }

    private void StudioPhase17RLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= StudioPhase17RLoaded;
        RestoreStudioPhase17RWorkspace();
    }

    private void RestoreStudioPhase17RWorkspace()
    {
        _studioPhase17RRestoringLayout = true;
        try
        {
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Viewport, _studioPhase17RLayout.ViewportDetached, activate: false);
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Inspector, _studioPhase17RLayout.InspectorDetached, activate: false);
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Timeline, _studioPhase17RLayout.TimelineDetached, activate: false);
        }
        finally
        {
            _studioPhase17RRestoringLayout = false;
        }
        ApplyStudioPhase17RDockedGeometry();
        UpdateStudioPhase17RToolWindowVisibility();
    }

    private void ToggleStudioPhase17RPane(StudioPhase17RPane pane)
        => SetStudioPhase17RPaneDetached(pane, !_studioPhase17RDetachedPanes.Contains(pane), activate: true);

    private void ApplyStudioPhase17RTwoScreenPreset()
    {
        _studioPhase17RRestoringLayout = true;
        try
        {
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Inspector, false, activate: false);
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Timeline, false, activate: false);
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Viewport, true, activate: true);
        }
        finally
        {
            _studioPhase17RRestoringLayout = false;
        }
        ApplyStudioPhase17RDockedGeometry();
        SaveStudioPhase17RWorkspace();
    }

    private void ApplyStudioPhase17RThreeScreenPreset()
    {
        // Monitor 1: detached viewport. Monitor 2: main window becomes a full-height controls/inspector
        // workspace. Monitor 3: detached timeline. This deliberately avoids creating a fourth required
        // window just to hold the command bar.
        _studioPhase17RRestoringLayout = true;
        try
        {
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Inspector, false, activate: false);
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Viewport, true, activate: false);
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Timeline, true, activate: false);
        }
        finally
        {
            _studioPhase17RRestoringLayout = false;
        }
        ApplyStudioPhase17RDockedGeometry();
        UpdateStudioPhase17RToolWindowVisibility(activateDetached: true);
        SaveStudioPhase17RWorkspace();
    }

    private void DockAllStudioPhase17RPanes()
    {
        _studioPhase17RRestoringLayout = true;
        try
        {
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Viewport, false, activate: false);
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Inspector, false, activate: false);
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Timeline, false, activate: false);
        }
        finally
        {
            _studioPhase17RRestoringLayout = false;
        }
        ApplyStudioPhase17RDockedGeometry();
        SaveStudioPhase17RWorkspace();
    }

    private void ResetStudioPhase17RWorkspace()
    {
        _studioPhase17RRestoringLayout = true;
        try
        {
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Viewport, false, activate: false);
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Inspector, false, activate: false);
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Timeline, false, activate: false);
            _studioPhase17RDockedInspectorWidth = 600;
            _studioPhase17RDockedTimelineHeight = 82;
            _studioPhase17RLayout = new ForeverStudioWorkspaceLayout();
            _studioPhase17RInitialWindowStateApplied.Clear();
            if (_studioPhase17RInspectorTabs is not null)
                _studioPhase17RInspectorTabs.SelectedIndex = 0;
        }
        finally
        {
            _studioPhase17RRestoringLayout = false;
        }
        ApplyStudioPhase17RDockedGeometry();
        ForeverStudioWorkspaceLayoutStore.Reset();
    }

    private void SetStudioPhase17RPaneDetached(StudioPhase17RPane pane, bool detached, bool activate)
    {
        if (_studioPhase17RRoot is null || !_studioPhase17RCards.TryGetValue(pane, out var card))
            return;

        var currentlyDetached = _studioPhase17RDetachedPanes.Contains(pane);
        if (currentlyDetached == detached)
        {
            if (detached && activate)
                ShowStudioPhase17RPaneWindow(pane, activate: true);
            return;
        }

        CaptureStudioPhase17RDockedDimensions();
        if (detached)
        {
            _studioPhase17RRoot.Children.Remove(card);
            var window = EnsureStudioPhase17RPaneWindow(pane);
            var host = _studioPhase17RWindowHosts[pane];
            host.Content = card;
            card.Margin = new Thickness(0);
            _studioPhase17RDetachedPanes.Add(pane);
            ApplyStudioPhase17RDockedGeometry();
            if (_studioAnimationSurface?.Visibility == Visibility.Visible)
                ShowStudioPhase17RPaneWindow(pane, activate);
            else
                window.Hide();
        }
        else
        {
            if (pane == StudioPhase17RPane.Viewport && _studioPhase17RViewportFullscreen)
                ExitStudioPhase17RViewportFullscreen();
            if (_studioPhase17RWindowHosts.TryGetValue(pane, out var host))
                host.Content = null;
            if (_studioPhase17RWindows.TryGetValue(pane, out var window))
                window.Hide();
            _studioPhase17RDetachedPanes.Remove(pane);
            _studioPhase17RRoot.Children.Add(card);
            ApplyStudioPhase17RDockedGeometry();
        }

        if (!_studioPhase17RRestoringLayout)
            SaveStudioPhase17RWorkspace();
    }

    private Window EnsureStudioPhase17RPaneWindow(StudioPhase17RPane pane)
    {
        if (_studioPhase17RWindows.TryGetValue(pane, out var existing))
            return existing;

        var (title, width, height, minWidth, minHeight) = pane switch
        {
            StudioPhase17RPane.Viewport => ("Forever Studio — 3D Viewport", 1280d, 820d, 640d, 420d),
            StudioPhase17RPane.Inspector => ("Forever Studio — Inspector", 650d, 900d, 460d, 520d),
            _ => ("Forever Studio — Animation Timeline", 1240d, 260d, 720d, 180d)
        };

        var window = new Window
        {
            Title = title,
            Width = width,
            Height = height,
            MinWidth = minWidth,
            MinHeight = minHeight,
            Background = Brush("#101619"),
            Foreground = Brush("#E4ECEF"),
            WindowStartupLocation = WindowStartupLocation.Manual,
            ResizeMode = ResizeMode.CanResize,
            ShowInTaskbar = true,
            Owner = this
        };

        var shell = new Grid();
        shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = BuildStudioPhase17RToolWindowHeader(pane);
        shell.Children.Add(header);
        if (pane == StudioPhase17RPane.Viewport)
            _studioPhase17RViewportWindowHeader = header;

        var host = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(6, 0, 6, 6)
        };
        Grid.SetRow(host, 1);
        shell.Children.Add(host);
        window.Content = shell;

        window.Closing += (_, e) => StudioPhase17RToolWindowClosing(pane, e);
        if (pane == StudioPhase17RPane.Viewport)
        {
            window.KeyDown += (_, e) =>
            {
                if (e.Key == Key.F11)
                {
                    ToggleStudioPhase17RViewportFullscreen();
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape && _studioPhase17RViewportFullscreen)
                {
                    ExitStudioPhase17RViewportFullscreen();
                    e.Handled = true;
                }
            };
        }

        ApplyStudioPhase17RWindowPlacement(window, pane, width, height);
        _studioPhase17RWindows[pane] = window;
        _studioPhase17RWindowHosts[pane] = host;
        return window;
    }

    private Border BuildStudioPhase17RToolWindowHeader(StudioPhase17RPane pane)
    {
        var bar = new DockPanel { LastChildFill = true };
        var dock = StudioButton("Dock", 66);
        dock.Height = 25;
        dock.Margin = new Thickness(4, 4, 4, 4);
        dock.Click += (_, _) => SetStudioPhase17RPaneDetached(pane, false, activate: false);
        DockPanel.SetDock(dock, Dock.Right);
        bar.Children.Add(dock);

        if (pane == StudioPhase17RPane.Viewport)
        {
            var fullscreen = StudioButton("Fullscreen · F11", 112);
            fullscreen.Height = 25;
            fullscreen.Margin = new Thickness(4, 4, 0, 4);
            fullscreen.Click += (_, _) => ToggleStudioPhase17RViewportFullscreen();
            DockPanel.SetDock(fullscreen, Dock.Right);
            bar.Children.Add(fullscreen);
        }

        bar.Children.Add(new TextBlock
        {
            Text = pane switch
            {
                StudioPhase17RPane.Viewport => "3D EDITOR",
                StudioPhase17RPane.Inspector => "INSPECTOR",
                _ => "ANIMATION TIMELINE"
            },
            Foreground = Brush("#7ED9DB"),
            FontWeight = FontWeights.SemiBold,
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 8, 0)
        });

        return new Border
        {
            Background = Brush("#162126"),
            BorderBrush = Brush("#35464F"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = bar
        };
    }

    private void StudioPhase17RToolWindowClosing(StudioPhase17RPane pane, CancelEventArgs e)
    {
        if (_studioPhase17RShuttingDown)
            return;
        e.Cancel = true;
        SetStudioPhase17RPaneDetached(pane, false, activate: false);
    }

    private void ApplyStudioPhase17RDockedGeometry()
    {
        if (_studioPhase17RRoot is null)
            return;

        var viewportDocked = !_studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Viewport);
        var inspectorDocked = !_studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Inspector);
        var timelineDocked = !_studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Timeline);
        var hasMainPane = viewportDocked || inspectorDocked;

        _studioPhase17RRoot.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
        _studioPhase17RRoot.ColumnDefinitions[0].MinWidth = hasMainPane ? 520 : 0;
        _studioPhase17RRoot.ColumnDefinitions[1].Width = viewportDocked && inspectorDocked
            ? new GridLength(4)
            : new GridLength(0);
        _studioPhase17RRoot.ColumnDefinitions[2].Width = viewportDocked && inspectorDocked
            ? new GridLength(_studioPhase17RDockedInspectorWidth)
            : new GridLength(0);
        _studioPhase17RRoot.ColumnDefinitions[2].MinWidth = viewportDocked && inspectorDocked ? 440 : 0;
        _studioPhase17RRoot.ColumnDefinitions[2].MaxWidth = viewportDocked && inspectorDocked ? 900 : double.PositiveInfinity;

        if (_studioPhase17RVerticalSplitter is not null)
            _studioPhase17RVerticalSplitter.Visibility = viewportDocked && inspectorDocked
                ? Visibility.Visible
                : Visibility.Collapsed;

        if (viewportDocked && _studioPhase17RCards.TryGetValue(StudioPhase17RPane.Viewport, out var viewportCard))
        {
            Grid.SetRow(viewportCard, 1);
            Grid.SetRowSpan(viewportCard, 1);
            Grid.SetColumn(viewportCard, 0);
            Grid.SetColumnSpan(viewportCard, 1);
            viewportCard.Margin = new Thickness(0, 0, viewportDocked && inspectorDocked ? 7 : 0, 0);
        }

        if (inspectorDocked && _studioPhase17RCards.TryGetValue(StudioPhase17RPane.Inspector, out var inspectorCard))
        {
            Grid.SetRow(inspectorCard, 1);
            Grid.SetColumn(inspectorCard, viewportDocked ? 2 : 0);
            Grid.SetColumnSpan(inspectorCard, viewportDocked ? 1 : 3);
            Grid.SetRowSpan(inspectorCard, viewportDocked ? 3 : timelineDocked ? 1 : 3);
            inspectorCard.Margin = viewportDocked ? new Thickness(7, 0, 0, 0) : new Thickness(0);
        }

        if (!timelineDocked)
        {
            _studioPhase17RRoot.RowDefinitions[2].Height = new GridLength(0);
            _studioPhase17RRoot.RowDefinitions[3].Height = new GridLength(0);
            if (_studioPhase17RHorizontalSplitter is not null)
                _studioPhase17RHorizontalSplitter.Visibility = Visibility.Collapsed;
            return;
        }

        if (!hasMainPane && _studioPhase17RCards.TryGetValue(StudioPhase17RPane.Timeline, out var onlyTimeline))
        {
            _studioPhase17RRoot.RowDefinitions[2].Height = new GridLength(0);
            _studioPhase17RRoot.RowDefinitions[3].Height = new GridLength(0);
            Grid.SetRow(onlyTimeline, 1);
            Grid.SetRowSpan(onlyTimeline, 3);
            Grid.SetColumn(onlyTimeline, 0);
            Grid.SetColumnSpan(onlyTimeline, 3);
            onlyTimeline.Margin = new Thickness(0);
            if (_studioPhase17RHorizontalSplitter is not null)
                _studioPhase17RHorizontalSplitter.Visibility = Visibility.Collapsed;
            return;
        }

        _studioPhase17RRoot.RowDefinitions[2].Height = new GridLength(4);
        _studioPhase17RRoot.RowDefinitions[3].Height = new GridLength(_studioPhase17RDockedTimelineHeight);
        _studioPhase17RRoot.RowDefinitions[3].MinHeight = 68;
        _studioPhase17RRoot.RowDefinitions[3].MaxHeight = 220;
        if (_studioPhase17RHorizontalSplitter is not null)
        {
            _studioPhase17RHorizontalSplitter.Visibility = Visibility.Visible;
            Grid.SetColumn(_studioPhase17RHorizontalSplitter, 0);
            Grid.SetColumnSpan(_studioPhase17RHorizontalSplitter, viewportDocked && inspectorDocked ? 1 : 3);
        }

        if (_studioPhase17RCards.TryGetValue(StudioPhase17RPane.Timeline, out var timelineCard))
        {
            Grid.SetRow(timelineCard, 3);
            Grid.SetRowSpan(timelineCard, 1);
            Grid.SetColumn(timelineCard, 0);
            Grid.SetColumnSpan(timelineCard, viewportDocked && inspectorDocked ? 1 : 3);
            timelineCard.Margin = new Thickness(0, 4, viewportDocked && inspectorDocked ? 7 : 0, 0);
        }
    }

    private void CaptureStudioPhase17RDockedDimensions()
    {
        if (_studioPhase17RRoot is null)
            return;
        var viewportDocked = !_studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Viewport);
        var inspectorDocked = !_studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Inspector);
        var timelineDocked = !_studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Timeline);

        if (viewportDocked && inspectorDocked && _studioPhase17RRoot.ColumnDefinitions[2].ActualWidth > 100)
            _studioPhase17RDockedInspectorWidth = Math.Clamp(_studioPhase17RRoot.ColumnDefinitions[2].ActualWidth, 440, 900);
        if (timelineDocked && _studioPhase17RRoot.RowDefinitions[3].ActualHeight > 30)
            _studioPhase17RDockedTimelineHeight = Math.Clamp(_studioPhase17RRoot.RowDefinitions[3].ActualHeight, 68, 220);
    }

    private void UpdateStudioPhase17RToolWindowVisibility(bool activateDetached = false)
    {
        var shouldShow = _studioAnimationSurface?.Visibility == Visibility.Visible && IsVisible;
        foreach (var pane in _studioPhase17RDetachedPanes.ToList())
        {
            if (!_studioPhase17RWindows.TryGetValue(pane, out var window))
                continue;
            if (!shouldShow)
            {
                window.Hide();
                continue;
            }
            ShowStudioPhase17RPaneWindow(pane, activateDetached);
        }
    }

    private void ShowStudioPhase17RPaneWindow(StudioPhase17RPane pane, bool activate)
    {
        if (!_studioPhase17RDetachedPanes.Contains(pane))
            return;
        var window = EnsureStudioPhase17RPaneWindow(pane);
        if (!window.IsVisible)
            window.Show();

        if (_studioPhase17RInitialWindowStateApplied.Add(pane))
        {
            var placement = StudioPhase17RPlacementForPane(pane);
            if (pane == StudioPhase17RPane.Viewport && placement?.IsFullscreen == true)
                EnterStudioPhase17RViewportFullscreen();
            else if (placement?.IsMaximized == true)
                window.WindowState = WindowState.Maximized;
        }

        if (activate)
            window.Activate();
    }

    private void ApplyStudioPhase17RWindowPlacement(
        Window window,
        StudioPhase17RPane pane,
        double defaultWidth,
        double defaultHeight)
    {
        var placement = StudioPhase17RPlacementForPane(pane);
        var width = placement?.Width ?? defaultWidth;
        var height = placement?.Height ?? defaultHeight;
        width = Math.Clamp(width, window.MinWidth, Math.Max(window.MinWidth, SystemParameters.VirtualScreenWidth));
        height = Math.Clamp(height, window.MinHeight, Math.Max(window.MinHeight, SystemParameters.VirtualScreenHeight));

        var defaultLeft = double.IsNaN(Left) ? SystemParameters.VirtualScreenLeft + 40 : Left + 45;
        var defaultTop = double.IsNaN(Top) ? SystemParameters.VirtualScreenTop + 40 : Top + 45;
        var left = placement?.Left ?? defaultLeft;
        var top = placement?.Top ?? defaultTop;

        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
        var virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;
        if (!double.IsFinite(left) || left + 80 < virtualLeft || left > virtualRight - 80)
            left = defaultLeft;
        if (!double.IsFinite(top) || top + 60 < virtualTop || top > virtualBottom - 60)
            top = defaultTop;

        window.Width = width;
        window.Height = height;
        window.Left = Math.Clamp(left, virtualLeft, Math.Max(virtualLeft, virtualRight - 120));
        window.Top = Math.Clamp(top, virtualTop, Math.Max(virtualTop, virtualBottom - 80));
    }

    private ForeverStudioWorkspaceWindowPlacement? StudioPhase17RPlacementForPane(StudioPhase17RPane pane)
        => pane switch
        {
            StudioPhase17RPane.Viewport => _studioPhase17RLayout.ViewportWindow,
            StudioPhase17RPane.Inspector => _studioPhase17RLayout.InspectorWindow,
            _ => _studioPhase17RLayout.TimelineWindow
        };

    private static ForeverStudioWorkspaceWindowPlacement CaptureStudioPhase17RWindowPlacement(
        Window window,
        bool fullscreen)
    {
        var bounds = window.WindowState == WindowState.Normal ?
            new Rect(window.Left, window.Top, window.Width, window.Height) :
            window.RestoreBounds;
        return new ForeverStudioWorkspaceWindowPlacement
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height,
            IsMaximized = !fullscreen && window.WindowState == WindowState.Maximized,
            IsFullscreen = fullscreen
        };
    }

    private void ToggleStudioPhase17RViewportFullscreen()
    {
        if (!_studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Viewport))
        {
            SetStudioPhase17RPaneDetached(StudioPhase17RPane.Viewport, true, activate: true);
            EnterStudioPhase17RViewportFullscreen();
            return;
        }

        if (_studioPhase17RViewportFullscreen)
            ExitStudioPhase17RViewportFullscreen();
        else
            EnterStudioPhase17RViewportFullscreen();
        SaveStudioPhase17RWorkspace();
    }

    private void EnterStudioPhase17RViewportFullscreen()
    {
        if (_studioPhase17RViewportFullscreen ||
            !_studioPhase17RWindows.TryGetValue(StudioPhase17RPane.Viewport, out var window))
            return;

        _studioPhase17RViewportStateBeforeFullscreen = window.WindowState;
        window.WindowState = WindowState.Normal;
        window.WindowStyle = WindowStyle.None;
        window.ResizeMode = ResizeMode.NoResize;
        if (_studioPhase17RViewportWindowHeader is not null)
            _studioPhase17RViewportWindowHeader.Visibility = Visibility.Collapsed;
        window.WindowState = WindowState.Maximized;
        _studioPhase17RViewportFullscreen = true;
    }

    private void ExitStudioPhase17RViewportFullscreen()
    {
        if (!_studioPhase17RViewportFullscreen ||
            !_studioPhase17RWindows.TryGetValue(StudioPhase17RPane.Viewport, out var window))
            return;

        window.WindowState = WindowState.Normal;
        window.WindowStyle = WindowStyle.SingleBorderWindow;
        window.ResizeMode = ResizeMode.CanResize;
        if (_studioPhase17RViewportWindowHeader is not null)
            _studioPhase17RViewportWindowHeader.Visibility = Visibility.Visible;
        window.WindowState = _studioPhase17RViewportStateBeforeFullscreen;
        _studioPhase17RViewportFullscreen = false;
    }

    private void SaveStudioPhase17RWorkspace()
    {
        if (_studioPhase17RRestoringLayout)
            return;

        CaptureStudioPhase17RDockedDimensions();
        _studioPhase17RLayout.ViewportDetached = _studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Viewport);
        _studioPhase17RLayout.InspectorDetached = _studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Inspector);
        _studioPhase17RLayout.TimelineDetached = _studioPhase17RDetachedPanes.Contains(StudioPhase17RPane.Timeline);
        _studioPhase17RLayout.InspectorWidth = _studioPhase17RDockedInspectorWidth;
        _studioPhase17RLayout.TimelineHeight = _studioPhase17RDockedTimelineHeight;
        _studioPhase17RLayout.InspectorTabIndex = _studioPhase17RInspectorTabs?.SelectedIndex ?? 0;

        if (_studioPhase17RWindows.TryGetValue(StudioPhase17RPane.Viewport, out var viewportWindow))
            _studioPhase17RLayout.ViewportWindow = CaptureStudioPhase17RWindowPlacement(viewportWindow, _studioPhase17RViewportFullscreen);
        if (_studioPhase17RWindows.TryGetValue(StudioPhase17RPane.Inspector, out var inspectorWindow))
            _studioPhase17RLayout.InspectorWindow = CaptureStudioPhase17RWindowPlacement(inspectorWindow, fullscreen: false);
        if (_studioPhase17RWindows.TryGetValue(StudioPhase17RPane.Timeline, out var timelineWindow))
            _studioPhase17RLayout.TimelineWindow = CaptureStudioPhase17RWindowPlacement(timelineWindow, fullscreen: false);

        ForeverStudioWorkspaceLayoutStore.Save(_studioPhase17RLayout);
    }

    private void StudioPhase17RMainWindowClosing(object? sender, CancelEventArgs e)
    {
        SaveStudioPhase17RWorkspace();
        _studioPhase17RShuttingDown = true;
        foreach (var window in _studioPhase17RWindows.Values.ToList())
        {
            if (window.IsVisible)
                window.Close();
        }
    }

    private static void UpdateStudioPhase17RViewportBadge(Grid viewportGrid)
    {
        foreach (var border in viewportGrid.Children.OfType<Border>())
        {
            if (border.Child is TextBlock text &&
                text.Text.StartsWith("BLOCK ANIMATION", StringComparison.OrdinalIgnoreCase))
            {
                text.Text = "BLOCK ANIMATION · PHASE 17R";
                break;
            }
        }
    }
}
