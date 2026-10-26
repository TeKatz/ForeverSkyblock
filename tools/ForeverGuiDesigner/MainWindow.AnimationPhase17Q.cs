using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17Q turns the accumulated Phase-17 animation authoring controls into a workstation-style
/// editor without replacing their underlying behavior. The existing target selector, transport,
/// timeline slider, transform toolbar and keyframe list are re-parented into a command/timeline
/// shell so every handler installed by 17B-17D keeps operating on the same control instances.
///
/// The phase deliberately does not change persistence, target identity, renderer composition or
/// animation evaluation. It only improves workflow, adds target-aware context commands and provides
/// a shared visual timeline surface that later semantic state tracks can extend.
/// </summary>
public partial class MainWindow
{
    private bool _studioAnimationPhase17QInitialized;
    private bool _studioPhase17QTimelineRefreshQueued;
    private string _studioPhase17QTimelineSignature = string.Empty;
    private StackPanel? _studioPhase17QTimelineLanes;
    private TextBlock? _studioPhase17QTargetSummary;
    private FrameworkElement? _studioPhase17QTimelineHitSurface;

    public void EnableForeverAnimationPhase17Q()
    {
        if (_studioAnimationPhase17QInitialized ||
            _studioAnimationSurface is not Grid root ||
            _studioAnimationViewport?.Parent is not Grid viewportGrid ||
            viewportGrid.Parent is not Border viewportCard ||
            _studioAnimationStatusText?.Parent is not StackPanel inspector ||
            inspector.Parent is not ScrollViewer inspectorScroll ||
            inspectorScroll.Parent is not Border inspectorCard ||
            _studioAnimationElementCombo is null ||
            _studioAnimationTimeline is null ||
            _studioAnimationTimeText is null ||
            _studioAnimationDurationBox is null ||
            _studioAnimationPlayButton is null ||
            _studioAnimationLoopCheck is null ||
            _studioAnimationSpeedCombo is null ||
            _studioAnimationKeyframeList is null)
            return;

        _studioAnimationPhase17QInitialized = true;

        var transportParent = _studioAnimationPlayButton.Parent as Panel;
        var stopButton = transportParent?.Children
            .OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "Stop", StringComparison.Ordinal));
        var durationParent = _studioAnimationDurationBox.Parent as StackPanel;
        var transformToolbar = FindStudioPhase17QViewportToolbar(viewportGrid);
        var resetViewButton = viewportGrid.Children
            .OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "Reset view", StringComparison.Ordinal));

        PrepareStudioPhase17QInspector(inspector, durationParent);
        UpdateStudioPhase17QViewportBadge(viewportGrid);

        DetachStudioPhase17QControl(_studioAnimationElementCombo);
        DetachStudioPhase17QControl(_studioAnimationPlayButton);
        if (stopButton is not null)
            DetachStudioPhase17QControl(stopButton);
        DetachStudioPhase17QControl(_studioAnimationLoopCheck);
        DetachStudioPhase17QControl(_studioAnimationSpeedCombo);
        DetachStudioPhase17QControl(_studioAnimationDurationBox);
        DetachStudioPhase17QControl(_studioAnimationTimeline);
        DetachStudioPhase17QControl(_studioAnimationTimeText);
        DetachStudioPhase17QControl(_studioAnimationKeyframeList);
        if (transformToolbar is not null)
            DetachStudioPhase17QControl(transformToolbar);
        if (resetViewButton is not null)
            DetachStudioPhase17QControl(resetViewButton);

        if (transportParent is FrameworkElement oldTransport)
            oldTransport.Visibility = Visibility.Collapsed;
        if (durationParent is not null)
            durationParent.Visibility = Visibility.Collapsed;

        ReflowStudioPhase17QRuntimeCurveInspector();

        var commandCard = BuildStudioPhase17QCommandCard(
            _studioAnimationElementCombo,
            _studioAnimationPlayButton,
            stopButton,
            _studioAnimationLoopCheck,
            _studioAnimationSpeedCombo,
            _studioAnimationDurationBox,
            resetViewButton,
            transformToolbar);
        var timelineCard = BuildStudioPhase17QTimelineCard(
            _studioAnimationTimeline,
            _studioAnimationTimeText,
            _studioAnimationKeyframeList);

        root.Children.Clear();
        root.RowDefinitions.Clear();
        root.ColumnDefinitions.Clear();

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 260 });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(6) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(190), MinHeight = 120 });

        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 420 });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(410), MinWidth = 330, MaxWidth = 700 });

        Grid.SetRow(commandCard, 0);
        Grid.SetColumn(commandCard, 0);
        Grid.SetColumnSpan(commandCard, 3);
        commandCard.Margin = new Thickness(0, 0, 0, 8);
        root.Children.Add(commandCard);

        viewportCard.Margin = new Thickness(0, 0, 8, 0);
        Grid.SetRow(viewportCard, 1);
        Grid.SetColumn(viewportCard, 0);
        root.Children.Add(viewportCard);

        var verticalSplitter = new GridSplitter
        {
            Width = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Brush("#1E2C32"),
            ResizeDirection = GridResizeDirection.Columns,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
            ToolTip = "Drag to resize the animation inspector"
        };
        Grid.SetRow(verticalSplitter, 1);
        Grid.SetColumn(verticalSplitter, 1);
        root.Children.Add(verticalSplitter);

        inspectorCard.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetRow(inspectorCard, 1);
        Grid.SetColumn(inspectorCard, 2);
        root.Children.Add(inspectorCard);

        var horizontalSplitter = new GridSplitter
        {
            Height = 6,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Brush("#1E2C32"),
            ResizeDirection = GridResizeDirection.Rows,
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
            ToolTip = "Drag to resize the animation timeline"
        };
        Grid.SetRow(horizontalSplitter, 2);
        Grid.SetColumn(horizontalSplitter, 0);
        Grid.SetColumnSpan(horizontalSplitter, 3);
        root.Children.Add(horizontalSplitter);

        Grid.SetRow(timelineCard, 3);
        Grid.SetColumn(timelineCard, 0);
        Grid.SetColumnSpan(timelineCard, 3);
        timelineCard.Margin = new Thickness(0, 8, 0, 0);
        root.Children.Add(timelineCard);

        InstallStudioPhase17QContextMenus();
        InstallStudioPhase17QShortcuts();
        InstallStudioPhase17QRefreshHooks();
        RefreshStudioPhase17QTargetSummary();
        QueueStudioPhase17QTimelineRefresh(force: true);
    }

    private Border BuildStudioPhase17QCommandCard(
        ComboBox targetCombo,
        Button playButton,
        Button? stopButton,
        CheckBox loopCheck,
        ComboBox speedCombo,
        TextBox durationBox,
        Button? resetViewButton,
        Border? transformToolbar)
    {
        var card = StudioCard();
        card.Padding = new Thickness(12, 9, 12, 8);

        var root = new StackPanel();
        var commands = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };

        var targetCell = new StackPanel
        {
            Width = 350,
            Margin = new Thickness(0, 0, 12, 5)
        };
        targetCell.Children.Add(new TextBlock
        {
            Text = "TARGET",
            Foreground = Brush("#7ED9DB"),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(1, 0, 0, 3)
        });
        targetCombo.Width = double.NaN;
        targetCombo.HorizontalAlignment = HorizontalAlignment.Stretch;
        targetCombo.Margin = new Thickness(0);
        targetCell.Children.Add(targetCombo);
        _studioPhase17QTargetSummary = new TextBlock
        {
            Foreground = Brush("#71848D"),
            FontSize = 9,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(1, 3, 0, 0)
        };
        targetCell.Children.Add(_studioPhase17QTargetSummary);
        commands.Children.Add(targetCell);

        var transportCell = new StackPanel { Margin = new Thickness(0, 0, 12, 5) };
        transportCell.Children.Add(StudioPhase17QMiniLabel("TRANSPORT"));
        var transport = new WrapPanel();
        playButton.Height = 30;
        playButton.Margin = new Thickness(0, 0, 5, 0);
        transport.Children.Add(playButton);
        if (stopButton is not null)
        {
            stopButton.Height = 30;
            stopButton.Margin = new Thickness(0, 0, 8, 0);
            transport.Children.Add(stopButton);
        }
        loopCheck.Margin = new Thickness(0, 0, 9, 0);
        loopCheck.VerticalAlignment = VerticalAlignment.Center;
        transport.Children.Add(loopCheck);
        speedCombo.Width = 82;
        speedCombo.Margin = new Thickness(0);
        transport.Children.Add(speedCombo);
        transportCell.Children.Add(transport);
        commands.Children.Add(transportCell);

        var durationCell = new StackPanel { Width = 78, Margin = new Thickness(0, 0, 12, 5) };
        durationCell.Children.Add(StudioPhase17QMiniLabel("DURATION"));
        durationBox.Width = double.NaN;
        durationBox.Margin = new Thickness(0);
        durationCell.Children.Add(durationBox);
        commands.Children.Add(durationCell);

        var keyCell = new StackPanel { Margin = new Thickness(0, 0, 12, 5) };
        keyCell.Children.Add(StudioPhase17QMiniLabel("KEYFRAMES"));
        var keyCommands = new WrapPanel();
        var previous = StudioButton("◀ Key", 70);
        previous.Height = 30;
        previous.Margin = new Thickness(0, 0, 5, 0);
        previous.ToolTip = "Select the previous keyframe for the current target (Ctrl+Left)";
        previous.Click += (_, _) => StudioPhase17QSelectAdjacentTargetKeyframe(-1);
        keyCommands.Children.Add(previous);

        var next = StudioButton("Key ▶", 70);
        next.Height = 30;
        next.Margin = new Thickness(0, 0, 5, 0);
        next.ToolTip = "Select the next keyframe for the current target (Ctrl+Right)";
        next.Click += (_, _) => StudioPhase17QSelectAdjacentTargetKeyframe(1);
        keyCommands.Children.Add(next);

        var key = StudioPrimaryButton("◆ KEY @ TIME", 112);
        key.Height = 30;
        key.Margin = new Thickness(0, 0, 5, 0);
        key.ToolTip = "Add or update the current target keyframe at the playhead (K)";
        key.Click += (_, _) => StudioPhase17QUpsertCurrentTarget();
        keyCommands.Children.Add(key);

        var copy = StudioButton("Copy", 64);
        copy.Height = 30;
        copy.Margin = new Thickness(0, 0, 5, 0);
        copy.ToolTip = "Copy the selected target keyframe (Ctrl+C)";
        copy.Click += (_, _) => StudioPhase17QCopyCurrentTargetKeyframe();
        keyCommands.Children.Add(copy);

        var paste = StudioButton("Paste @ time", 102);
        paste.Height = 30;
        paste.Margin = new Thickness(0);
        paste.ToolTip = "Paste the copied target keyframe at the playhead without overwriting existing data (Ctrl+V)";
        paste.Click += (_, _) => StudioPhase17QPasteCurrentTargetKeyframe();
        keyCommands.Children.Add(paste);
        keyCell.Children.Add(keyCommands);
        commands.Children.Add(keyCell);

        var viewCell = new StackPanel { Margin = new Thickness(0, 0, 0, 5) };
        viewCell.Children.Add(StudioPhase17QMiniLabel("VIEW"));
        var viewCommands = new WrapPanel();
        if (resetViewButton is not null)
        {
            resetViewButton.HorizontalAlignment = HorizontalAlignment.Left;
            resetViewButton.VerticalAlignment = VerticalAlignment.Center;
            resetViewButton.Margin = new Thickness(0, 0, 5, 0);
            resetViewButton.Height = 30;
            viewCommands.Children.Add(resetViewButton);
        }
        var identify = StudioButton("Identify", 78);
        identify.Height = 30;
        identify.Margin = new Thickness(0);
        identify.ToolTip = "Briefly isolate/highlight the current target (I)";
        identify.Click += (_, _) => StudioPhase17QIdentifyCurrentTarget();
        viewCommands.Children.Add(identify);
        viewCell.Children.Add(viewCommands);
        commands.Children.Add(viewCell);

        root.Children.Add(commands);

        if (transformToolbar is not null)
        {
            transformToolbar.HorizontalAlignment = HorizontalAlignment.Stretch;
            transformToolbar.VerticalAlignment = VerticalAlignment.Top;
            transformToolbar.Margin = new Thickness(0, 2, 0, 0);
            transformToolbar.Padding = new Thickness(8, 6, 8, 5);
            root.Children.Add(transformToolbar);
        }

        var hint = new TextBlock
        {
            Text = "Right-click the viewport or timeline for target, visibility, status and effect actions.",
            Foreground = Brush("#71848D"),
            FontSize = 9,
            Margin = new Thickness(2, 3, 0, 0)
        };
        root.Children.Add(hint);

        card.Child = root;
        return card;
    }

    private Border BuildStudioPhase17QTimelineCard(
        Slider timeline,
        TextBlock timeText,
        ListBox targetKeyframeList)
    {
        var card = StudioCard();
        card.Padding = new Thickness(12, 9, 12, 9);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 360 });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300), MinWidth = 230 });

        var left = new StackPanel();
        _studioPhase17QTimelineHitSurface = left;
        var header = new DockPanel { LastChildFill = true, Margin = new Thickness(0, 0, 0, 5) };
        var label = StudioSectionLabel("ANIMATION TIMELINE · PHASE 17Q");
        DockPanel.SetDock(label, Dock.Left);
        header.Children.Add(label);

        timeText.Margin = new Thickness(12, 0, 2, 0);
        timeText.VerticalAlignment = VerticalAlignment.Center;
        timeText.HorizontalAlignment = HorizontalAlignment.Right;
        DockPanel.SetDock(timeText, Dock.Right);
        header.Children.Add(timeText);
        left.Children.Add(header);

        timeline.Margin = new Thickness(112, 0, 4, 6);
        timeline.HorizontalAlignment = HorizontalAlignment.Stretch;
        left.Children.Add(timeline);

        _studioPhase17QTimelineLanes = new StackPanel();
        left.Children.Add(_studioPhase17QTimelineLanes);
        grid.Children.Add(left);

        var divider = new Border
        {
            Width = 1,
            Background = Brush("#35464F"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 2, 0, 2)
        };
        Grid.SetColumn(divider, 1);
        grid.Children.Add(divider);

        var right = new Grid();
        right.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        right.Children.Add(new TextBlock
        {
            Text = "TARGET KEYFRAMES",
            Foreground = Brush("#7ED9DB"),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(1, 0, 0, 5)
        });
        targetKeyframeList.Height = double.NaN;
        targetKeyframeList.MinHeight = 92;
        targetKeyframeList.Margin = new Thickness(0);
        targetKeyframeList.VerticalAlignment = VerticalAlignment.Stretch;
        Grid.SetRow(targetKeyframeList, 1);
        right.Children.Add(targetKeyframeList);
        Grid.SetColumn(right, 2);
        grid.Children.Add(right);

        card.Child = grid;
        return card;
    }

    private void PrepareStudioPhase17QInspector(StackPanel inspector, StackPanel? durationParent)
    {
        var directTexts = inspector.Children.OfType<TextBlock>().ToList();
        foreach (var text in directTexts)
        {
            if (string.Equals(text.Text, "BLOCK ANIMATION STUDIO", StringComparison.Ordinal))
            {
                text.Text = "INSPECTOR · SOURCE / MACHINE CHANNELS";
                continue;
            }

            if (string.Equals(text.Text, "Animated target", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(text.Text, "TIMELINE", StringComparison.Ordinal) ||
                string.Equals(text.Text, "KEYFRAMES", StringComparison.Ordinal) ||
                text.Text.StartsWith("Phase 17B stabilizes", StringComparison.Ordinal))
            {
                CollapseStudioPhase17QInspectorElementWithSeparator(inspector, text);
            }
        }

        if (_studioAnimationTimeBox?.Parent is Panel keyEditorParent)
        {
            foreach (var panel in keyEditorParent.Children.OfType<WrapPanel>())
            {
                if (panel.Children.OfType<Button>().Any(button =>
                        string.Equals(button.Content?.ToString(), "◀ Key", StringComparison.Ordinal)))
                    panel.Visibility = Visibility.Collapsed;
            }
        }

        if (durationParent is not null)
            durationParent.Visibility = Visibility.Collapsed;
    }

    private static void CollapseStudioPhase17QInspectorElementWithSeparator(Panel parent, UIElement element)
    {
        var index = parent.Children.IndexOf(element);
        element.Visibility = Visibility.Collapsed;
        if (index > 0 && parent.Children[index - 1] is Border separator)
            separator.Visibility = Visibility.Collapsed;
    }

    private void ReflowStudioPhase17QRuntimeCurveInspector()
    {
        if (_studioAnimationRuntimeCurveCombo?.Parent is not StackPanel runtimePanel)
            return;
        runtimePanel.Margin = new Thickness(0);
        Grid.SetColumn(runtimePanel, 0);
        Grid.SetColumnSpan(runtimePanel, 2);
    }

    private Border? FindStudioPhase17QViewportToolbar(Grid viewportGrid)
    {
        var firstButton = _studioAnimationManipulationButtons.FirstOrDefault();
        DependencyObject? current = firstButton;
        while (current is FrameworkElement element)
        {
            if (element is Border border && ReferenceEquals(border.Parent, viewportGrid))
                return border;
            current = element.Parent;
        }
        return null;
    }

    private static void DetachStudioPhase17QControl(FrameworkElement element)
    {
        switch (element.Parent)
        {
            case Panel panel:
                panel.Children.Remove(element);
                break;
            case Decorator decorator when ReferenceEquals(decorator.Child, element):
                decorator.Child = null;
                break;
            case ContentControl contentControl when ReferenceEquals(contentControl.Content, element):
                contentControl.Content = null;
                break;
        }
    }

    private static TextBlock StudioPhase17QMiniLabel(string text)
        => new()
        {
            Text = text,
            Foreground = Brush("#71848D"),
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(1, 0, 0, 3)
        };

    private static void UpdateStudioPhase17QViewportBadge(Grid viewportGrid)
    {
        foreach (var border in viewportGrid.Children.OfType<Border>())
        {
            if (border.Child is TextBlock text &&
                text.Text.StartsWith("BLOCK ANIMATION", StringComparison.OrdinalIgnoreCase))
            {
                text.Text = "BLOCK ANIMATION · PHASE 17Q";
                return;
            }
        }
    }

    private void InstallStudioPhase17QContextMenus()
    {
        if (_studioAnimationViewport is not null)
            _studioAnimationViewport.ContextMenu = CreateStudioPhase17QContextMenu();

        if (_studioPhase17QTimelineHitSurface is not null)
        {
            _studioPhase17QTimelineHitSurface.ContextMenu = CreateStudioPhase17QContextMenu();
            _studioPhase17QTimelineHitSurface.PreviewMouseRightButtonDown += StudioPhase17QTimelineRightClick;
        }
    }

    private ContextMenu CreateStudioPhase17QContextMenu()
    {
        var menu = new ContextMenu
        {
            Background = Brush("#20252B"),
            Foreground = Brush("#F2F5F7"),
            BorderBrush = Brush("#46515B"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            HasDropShadow = false
        };
        InstallDarkContextMenuTheme(menu);
        menu.Opened += (_, _) => RebuildStudioPhase17QContextMenu(menu);
        return menu;
    }

    private void RebuildStudioPhase17QContextMenu(ContextMenu menu)
    {
        menu.Items.Clear();
        var target = GetSelectedStudioAnimationTarget();
        var track = target is null ? null : FindStudioAnimationTrackForTarget(target);
        var hasTarget = target is not null;
        var hasKeyframes = track?.Keyframes.Count > 0;
        var runtimeTarget = TryGetSelectedStudioRuntimeAuthoringTarget(out _);
        var hasSelectedKeyframe = runtimeTarget
            ? TryGetSelectedStudioRuntimeTargetKeyframe(out _, out _, out _)
            : TryGetSelectedStudioAnimationKeyframe(out _, out _, out _);

        var targetLabel = target is null ? "No animation target selected" : DescribeStudioAnimationTarget(target);
        menu.Items.Add(ContextItem($"Target · {targetLabel}", (_, _) => { }, enabled: false));
        menu.Items.Add(new Separator());
        menu.Items.Add(ContextItem("Add / update keyframe @ playhead", (_, _) => StudioPhase17QUpsertCurrentTarget(), hasTarget, "K"));
        menu.Items.Add(ContextItem("Previous keyframe", (_, _) => StudioPhase17QSelectAdjacentTargetKeyframe(-1), hasKeyframes, "Ctrl+Left"));
        menu.Items.Add(ContextItem("Next keyframe", (_, _) => StudioPhase17QSelectAdjacentTargetKeyframe(1), hasKeyframes, "Ctrl+Right"));
        menu.Items.Add(new Separator());
        menu.Items.Add(ContextItem("Copy selected keyframe", (_, _) => StudioPhase17QCopyCurrentTargetKeyframe(), hasSelectedKeyframe, "Ctrl+C"));
        menu.Items.Add(ContextItem("Paste keyframe @ playhead", (_, _) => StudioPhase17QPasteCurrentTargetKeyframe(), hasTarget && _studioAnimationClipboardKeyframe is not null, "Ctrl+V"));
        menu.Items.Add(ContextItem("Key visibility @ playhead", (_, _) => StudioPhase17QKeyCurrentVisibility(), hasTarget));
        menu.Items.Add(new Separator());
        menu.Items.Add(ContextItem("Neutral transform @ playhead", (_, _) => StudioPhase17QNeutralCurrentTarget(), hasTarget));
        menu.Items.Add(ContextItem("Identify target", (_, _) => StudioPhase17QIdentifyCurrentTarget(), hasTarget, "I"));
        menu.Items.Add(ContextItem("Delete selected keyframe", (_, _) => StudioPhase17QDeleteCurrentTargetKeyframe(), hasSelectedKeyframe, "Del"));
        menu.Items.Add(ContextItem("Clear target track", (_, _) => StudioPhase17QClearCurrentTargetTrack(), hasKeyframes));

        var statusMenu = ContextSubmenu("Status channel");
        var statusAvailable = _studioRuntimeStatusAxisCombo?.SelectedItem is ForeverRuntimeStateAxis;
        statusMenu.IsEnabled = statusAvailable;
        statusMenu.Items.Add(ContextItem("Key current status @ playhead", (_, _) => UpsertStudioRuntimeStatusKeyframe(), statusAvailable));
        statusMenu.Items.Add(ContextItem(
            "Delete selected status keyframe",
            (_, _) => DeleteSelectedStudioRuntimeStatusKeyframe(),
            statusAvailable && (_studioRuntimeStatusKeyframeList?.SelectedIndex ?? -1) >= 0));
        statusMenu.Items.Add(ContextItem(
            "Clear status channel",
            (_, _) => ClearStudioRuntimeStatusTrack(),
            StudioPhase17QSelectedStatusTrackHasKeys()));

        var effectMenu = ContextSubmenu("Special effect channel");
        var effectAvailable = _studioRuntimeEffectCombo?.SelectedItem is ForeverRuntimeEffectDescriptor;
        effectMenu.IsEnabled = effectAvailable;
        effectMenu.Items.Add(ContextItem("Key current effect @ playhead", (_, _) => UpsertStudioRuntimeEffectKeyframe(), effectAvailable));
        effectMenu.Items.Add(ContextItem(
            "Delete selected effect keyframe",
            (_, _) => DeleteSelectedStudioRuntimeEffectKeyframe(),
            effectAvailable && (_studioRuntimeEffectKeyframeList?.SelectedIndex ?? -1) >= 0));
        effectMenu.Items.Add(ContextItem(
            "Clear effect channel",
            (_, _) => ClearStudioRuntimeEffectTrack(),
            StudioPhase17QSelectedEffectTrackHasKeys()));

        menu.Items.Add(new Separator());
        menu.Items.Add(statusMenu);
        menu.Items.Add(effectMenu);
    }

    private void StudioPhase17QTimelineRightClick(object sender, MouseButtonEventArgs e)
    {
        if (_studioAnimationTimeline is null || _studioAnimationTimeline.ActualWidth <= 1)
            return;

        var point = e.GetPosition(_studioAnimationTimeline);
        if (point.X < 0 || point.X > _studioAnimationTimeline.ActualWidth)
            return;

        var ratio = Math.Clamp(point.X / _studioAnimationTimeline.ActualWidth, 0, 1);
        SetStudioAnimationTimelineTime(ratio * _studioAnimationDurationSeconds);
    }

    private void InstallStudioPhase17QShortcuts()
    {
        PreviewKeyDown += StudioPhase17QPreviewKeyDown;
    }

    private void StudioPhase17QPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_studioAnimationSurface?.Visibility != Visibility.Visible ||
            Keyboard.FocusedElement is TextBoxBase or ComboBox or ButtonBase or ListBox or Slider)
            return;

        var control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        if (e.Key == Key.Space)
        {
            ToggleStudioAnimationPlayback();
            e.Handled = true;
        }
        else if (!control && e.Key == Key.K)
        {
            StudioPhase17QUpsertCurrentTarget();
            e.Handled = true;
        }
        else if (!control && e.Key == Key.I)
        {
            StudioPhase17QIdentifyCurrentTarget();
            e.Handled = true;
        }
        else if (control && e.Key == Key.Left)
        {
            StudioPhase17QSelectAdjacentTargetKeyframe(-1);
            e.Handled = true;
        }
        else if (control && e.Key == Key.Right)
        {
            StudioPhase17QSelectAdjacentTargetKeyframe(1);
            e.Handled = true;
        }
        else if (control && e.Key == Key.C)
        {
            StudioPhase17QCopyCurrentTargetKeyframe();
            e.Handled = true;
        }
        else if (control && e.Key == Key.V)
        {
            StudioPhase17QPasteCurrentTargetKeyframe();
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            StudioPhase17QDeleteCurrentTargetKeyframe();
            e.Handled = true;
        }
    }

    private void InstallStudioPhase17QRefreshHooks()
    {
        if (_studioAnimationElementCombo is not null)
        {
            _studioAnimationElementCombo.SelectionChanged += (_, _) =>
            {
                RefreshStudioPhase17QTargetSummary();
                QueueStudioPhase17QTimelineRefresh();
            };
        }

        if (_studioRuntimeStatusAxisCombo is not null)
            _studioRuntimeStatusAxisCombo.SelectionChanged += (_, _) => QueueStudioPhase17QTimelineRefresh();
        if (_studioRuntimeEffectCombo is not null)
            _studioRuntimeEffectCombo.SelectionChanged += (_, _) => QueueStudioPhase17QTimelineRefresh();
        if (_studioAnimationDurationBox is not null)
            _studioAnimationDurationBox.LostFocus += (_, _) => QueueStudioPhase17QTimelineRefresh(force: true);

        HookStudioPhase17QItemsSourceRefresh(_studioAnimationKeyframeList);
        HookStudioPhase17QItemsSourceRefresh(_studioRuntimeStatusKeyframeList);
        HookStudioPhase17QItemsSourceRefresh(_studioRuntimeEffectKeyframeList);

        if (_studioAnimationSurface is not null)
        {
            _studioAnimationSurface.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                {
                    RefreshStudioPhase17QTargetSummary();
                    QueueStudioPhase17QTimelineRefresh(force: true);
                }
            };
        }
    }

    private void HookStudioPhase17QItemsSourceRefresh(ListBox? list)
    {
        if (list is null)
            return;
        DependencyPropertyDescriptor
            .FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox))?
            .AddValueChanged(list, (_, _) => QueueStudioPhase17QTimelineRefresh());
    }

    private void QueueStudioPhase17QTimelineRefresh(bool force = false)
    {
        if (!_studioAnimationPhase17QInitialized)
            return;
        if (force)
            _studioPhase17QTimelineSignature = string.Empty;
        if (_studioPhase17QTimelineRefreshQueued)
            return;

        _studioPhase17QTimelineRefreshQueued = true;
        _ = Dispatcher.BeginInvoke(() =>
        {
            _studioPhase17QTimelineRefreshQueued = false;
            RefreshStudioPhase17QTimelineLanes();
        }, DispatcherPriority.Background);
    }

    private void RefreshStudioPhase17QTimelineLanes()
    {
        if (_studioPhase17QTimelineLanes is null)
            return;

        var target = GetSelectedStudioAnimationTarget();
        var targetTrack = target is null ? null : FindStudioAnimationTrackForTarget(target);
        var targetTimes = targetTrack?.Keyframes
                              .OrderBy(keyframe => keyframe.TimeSeconds)
                              .Select(keyframe => keyframe.TimeSeconds)
                              .ToList() ?? [];

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        ForeverRuntimeStateAxis? statusAxis = _studioRuntimeStatusAxisCombo?.SelectedItem as ForeverRuntimeStateAxis;
        var statusTimes = statusAxis is null
            ? []
            : GetStudioRuntimeStatusTrack(assetId, statusAxis.Id, create: false)?
                  .OrderBy(keyframe => keyframe.TimeSeconds)
                  .Select(keyframe => keyframe.TimeSeconds)
                  .ToList() ?? [];

        ForeverRuntimeEffectDescriptor? effect = _studioRuntimeEffectCombo?.SelectedItem as ForeverRuntimeEffectDescriptor;
        var effectTimes = effect is null
            ? []
            : GetStudioRuntimeEffectTrack(assetId, effect.Id, create: false)?
                  .OrderBy(keyframe => keyframe.TimeSeconds)
                  .Select(keyframe => keyframe.TimeSeconds)
                  .ToList() ?? [];

        var targetId = target?.TargetId.Value ?? "none";
        var signature = string.Join("|",
            _studioAnimationDurationSeconds.ToString("R", CultureInfo.InvariantCulture),
            targetId,
            string.Join(",", targetTimes.Select(time => time.ToString("R", CultureInfo.InvariantCulture))),
            statusAxis?.Id ?? "none",
            string.Join(",", statusTimes.Select(time => time.ToString("R", CultureInfo.InvariantCulture))),
            effect?.Id ?? "none",
            string.Join(",", effectTimes.Select(time => time.ToString("R", CultureInfo.InvariantCulture))));
        if (string.Equals(signature, _studioPhase17QTimelineSignature, StringComparison.Ordinal))
            return;
        _studioPhase17QTimelineSignature = signature;

        _studioPhase17QTimelineLanes.Children.Clear();
        _studioPhase17QTimelineLanes.Children.Add(BuildStudioPhase17QTimelineLane(
            "TARGET",
            target is null ? "No target" : DescribeStudioAnimationTarget(target),
            targetTimes,
            "#58B9C0",
            time =>
            {
                SetStudioAnimationTimelineTime(time);
                if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
                    RefreshStudioRuntimeTargetTrackUi();
                else
                    SelectStudioAnimationKeyframeAtTime(time);
            }));

        if (statusAxis is not null)
        {
            _studioPhase17QTimelineLanes.Children.Add(BuildStudioPhase17QTimelineLane(
                "STATUS",
                statusAxis.DisplayName,
                statusTimes,
                "#D8B26A",
                SetStudioAnimationTimelineTime));
        }

        if (effect is not null)
        {
            _studioPhase17QTimelineLanes.Children.Add(BuildStudioPhase17QTimelineLane(
                "EFFECT",
                effect.DisplayName,
                effectTimes,
                "#B995D6",
                SetStudioAnimationTimelineTime));
        }
    }

    private FrameworkElement BuildStudioPhase17QTimelineLane(
        string laneName,
        string description,
        IReadOnlyList<double> keyTimes,
        string markerColor,
        Action<double> seek)
    {
        var row = new Grid { Height = 32, Margin = new Thickness(0, 1, 0, 1) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(108) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var label = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        label.Children.Add(new TextBlock
        {
            Text = laneName,
            Foreground = Brush("#AFC0C7"),
            FontSize = 9,
            FontWeight = FontWeights.Bold
        });
        label.Children.Add(new TextBlock
        {
            Text = description,
            Foreground = Brush("#61747D"),
            FontSize = 8,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 100
        });
        row.Children.Add(label);

        var canvas = new Canvas
        {
            Height = 24,
            VerticalAlignment = VerticalAlignment.Center,
            Background = Brush("#0C1317"),
            ClipToBounds = true,
            ToolTip = keyTimes.Count == 0 ? "No keyframes on this lane" : $"{keyTimes.Count} keyframe(s)"
        };
        Grid.SetColumn(canvas, 1);
        row.Children.Add(canvas);

        void Redraw()
        {
            canvas.Children.Clear();
            var width = canvas.ActualWidth;
            if (width <= 1)
                return;

            canvas.Children.Add(new Border
            {
                Height = 1,
                Width = width,
                Background = Brush("#2A3940"),
                VerticalAlignment = VerticalAlignment.Center
            });
            Canvas.SetTop(canvas.Children[^1], 11.5);

            var duration = Math.Max(0.0001, _studioAnimationDurationSeconds);
            foreach (var time in keyTimes)
            {
                var size = 9d;
                var marker = new Border
                {
                    Width = size,
                    Height = size,
                    Background = Brush(markerColor),
                    BorderBrush = Brush("#D8EEF0"),
                    BorderThickness = new Thickness(0.7),
                    CornerRadius = new CornerRadius(1),
                    RenderTransform = new RotateTransform(45),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    Cursor = Cursors.Hand,
                    ToolTip = $"{laneName} keyframe · {time:0.###}s"
                };
                marker.MouseLeftButtonDown += (_, e) =>
                {
                    seek(time);
                    e.Handled = true;
                };
                canvas.Children.Add(marker);
                var x = Math.Clamp(time / duration, 0, 1) * Math.Max(0, width - size);
                Canvas.SetLeft(marker, x);
                Canvas.SetTop(marker, 7.5);
            }
        }

        canvas.Loaded += (_, _) => Redraw();
        canvas.SizeChanged += (_, _) => Redraw();
        return row;
    }

    private void RefreshStudioPhase17QTargetSummary()
    {
        if (_studioPhase17QTargetSummary is null)
            return;
        var target = GetSelectedStudioAnimationTarget();
        if (target is null)
        {
            _studioPhase17QTargetSummary.Text = "No target selected";
            return;
        }

        var kind = target.Kind == AnimationTargetKind.RuntimeRendererComponent
            ? "Runtime component"
            : "JSON model element";
        _studioPhase17QTargetSummary.Text = $"{kind} · stable TargetId";
    }

    private bool StudioPhase17QSelectedStatusTrackHasKeys()
    {
        if (_studioRuntimeStatusAxisCombo?.SelectedItem is not ForeverRuntimeStateAxis axis)
            return false;
        return GetStudioRuntimeStatusTrack(ResolveCurrentStudioRuntimeAssetId(), axis.Id, create: false)?.Count > 0;
    }

    private bool StudioPhase17QSelectedEffectTrackHasKeys()
    {
        if (_studioRuntimeEffectCombo?.SelectedItem is not ForeverRuntimeEffectDescriptor effect)
            return false;
        return GetStudioRuntimeEffectTrack(ResolveCurrentStudioRuntimeAssetId(), effect.Id, create: false)?.Count > 0;
    }

    private void StudioPhase17QUpsertCurrentTarget()
    {
        if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            UpsertStudioRuntimeTargetKeyframe();
        else
            UpsertStudioAnimationKeyframe();
        QueueStudioPhase17QTimelineRefresh(force: true);
    }

    private void StudioPhase17QSelectAdjacentTargetKeyframe(int direction)
    {
        if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            SelectAdjacentStudioRuntimeTargetKeyframe(direction);
        else
            SelectAdjacentStudioAnimationKeyframe(direction);
    }

    private void StudioPhase17QCopyCurrentTargetKeyframe()
    {
        if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            CopySelectedStudioRuntimeTargetKeyframe();
        else
            CopySelectedStudioAnimationKeyframe();
    }

    private void StudioPhase17QPasteCurrentTargetKeyframe()
    {
        if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            PasteStudioRuntimeTargetKeyframeAtTimeline();
        else
            PasteStudioAnimationKeyframeAtTimeline();
        QueueStudioPhase17QTimelineRefresh(force: true);
    }

    private void StudioPhase17QKeyCurrentVisibility()
    {
        if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            KeyStudioRuntimeTargetVisibilityAtCurrentTime();
        else
            KeyStudioAnimationVisibilityAtCurrentTime();
        QueueStudioPhase17QTimelineRefresh(force: true);
    }

    private void StudioPhase17QNeutralCurrentTarget()
    {
        if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            ResetStudioRuntimeTargetAtCurrentTime();
        else
            ResetStudioAnimationTransformAtCurrentTime();
        QueueStudioPhase17QTimelineRefresh(force: true);
    }

    private void StudioPhase17QIdentifyCurrentTarget()
    {
        if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            IdentifyStudioRuntimeTargetSelection(700);
        else
            IdentifyStudioAnimationSelection(700);
    }

    private void StudioPhase17QDeleteCurrentTargetKeyframe()
    {
        if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            DeleteSelectedStudioRuntimeTargetKeyframe();
        else
            DeleteSelectedStudioAnimationKeyframeStable();
        QueueStudioPhase17QTimelineRefresh(force: true);
    }

    private void StudioPhase17QClearCurrentTargetTrack()
    {
        if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            ClearStudioRuntimeTargetTrack();
        else
            ClearStudioAnimationTrack();
        QueueStudioPhase17QTimelineRefresh(force: true);
    }
}