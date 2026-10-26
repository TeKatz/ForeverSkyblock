using System.Windows;
using System.Windows.Controls;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17Q density correction after testing the workstation shell at 1920x1080.
/// Keeps the established controls and handlers intact, but moves secondary commands to the
/// already available context menu/shortcuts and gives viewport + inspector the useful space.
/// </summary>
public partial class MainWindow
{
    private bool _studioAnimationPhase17QDensityFixInitialized;

    public void EnableForeverAnimationPhase17QDensityFix()
    {
        if (_studioAnimationPhase17QDensityFixInitialized ||
            !_studioAnimationPhase17QInitialized ||
            _studioAnimationSurface is not Grid root ||
            root.RowDefinitions.Count < 4 ||
            root.ColumnDefinitions.Count < 3)
            return;

        _studioAnimationPhase17QDensityFixInitialized = true;

        // The viewport is the primary editor. The timeline is deliberately a thin support strip.
        root.RowDefinitions[1].MinHeight = 390;
        root.RowDefinitions[2].Height = new GridLength(4);
        root.RowDefinitions[3].Height = new GridLength(82);
        root.RowDefinitions[3].MinHeight = 68;
        root.RowDefinitions[3].MaxHeight = 112;

        // A useful inspector is more valuable than a few extra viewport pixels. It still remains
        // user-resizable through the existing splitter.
        root.ColumnDefinitions[0].MinWidth = 650;
        root.ColumnDefinitions[1].Width = new GridLength(4);
        root.ColumnDefinitions[2].Width = new GridLength(600);
        root.ColumnDefinitions[2].MinWidth = 520;
        root.ColumnDefinitions[2].MaxWidth = 820;

        CompactStudioPhase17QPrimaryBar(root);
        CompactStudioPhase17QTransformBar();
        CompactStudioPhase17QTimelineStrip(root);
    }

    private void CompactStudioPhase17QPrimaryBar(Grid root)
    {
        var commandCard = root.Children
            .OfType<Border>()
            .FirstOrDefault(border => Grid.GetRow(border) == 0 && Grid.GetColumnSpan(border) >= 3);
        if (commandCard?.Child is not StackPanel shell)
            return;

        commandCard.Padding = new Thickness(7, 4, 7, 4);
        commandCard.Margin = new Thickness(0, 0, 0, 5);

        var commandRow = shell.Children.OfType<WrapPanel>().FirstOrDefault();
        if (commandRow is null)
            return;

        // Secondary keyframe/view actions stay available through the context menu and shortcuts.
        // Hiding them here removes duplication; no handler or command is removed.
        foreach (var button in commandRow.Children
            .OfType<StackPanel>()
            .SelectMany(cell => cell.Children.OfType<WrapPanel>())
            .SelectMany(panel => panel.Children.OfType<Button>()))
        {
            var text = button.Content?.ToString();
            if (text is "◀ Key" or "Key ▶" or "Copy" or "Paste @ time" or "Move to time" or "Reset view" or "Identify")
                button.Visibility = Visibility.Collapsed;
            else
                button.Height = 25;
        }

        foreach (var cell in commandRow.Children.OfType<StackPanel>())
        {
            cell.Margin = new Thickness(0, 0, 7, 0);
            foreach (var label in cell.Children.OfType<TextBlock>())
                label.Visibility = Visibility.Collapsed;
        }

        // Target remains wide enough to identify parts, but not wide enough to dominate the bar.
        if (_studioAnimationElementCombo?.Parent is StackPanel targetCell)
            targetCell.Width = 300;

        if (_studioPhase17QTargetSummary is not null)
            _studioPhase17QTargetSummary.Visibility = Visibility.Collapsed;
        if (_studioPhase17QInterpolationHint is not null)
            _studioPhase17QInterpolationHint.Visibility = Visibility.Collapsed;

        foreach (var hint in shell.Children.OfType<TextBlock>())
            hint.Visibility = Visibility.Collapsed;
    }

    private void CompactStudioPhase17QTransformBar()
    {
        var first = _studioAnimationManipulationButtons.FirstOrDefault();
        DependencyObject? current = first;
        Border? toolbar = null;
        while (current is FrameworkElement element)
        {
            if (element is Border border)
            {
                toolbar = border;
                break;
            }
            current = element.Parent;
        }

        if (toolbar?.Child is not StackPanel transformRoot)
            return;

        toolbar.Padding = new Thickness(0);
        toolbar.Margin = new Thickness(0, 3, 0, 0);
        toolbar.BorderThickness = new Thickness(0);
        toolbar.Background = Brush("#00000000");

        foreach (var title in transformRoot.Children.OfType<TextBlock>())
            title.Visibility = Visibility.Collapsed;

        var rows = transformRoot.Children.OfType<WrapPanel>().ToList();
        if (rows.Count == 0)
            return;

        var primary = rows[0];
        primary.Margin = new Thickness(0);

        if (rows.Count > 1)
        {
            var secondary = rows[1];
            foreach (var button in secondary.Children.OfType<Button>().ToList())
            {
                var text = button.Content?.ToString();
                if (text is "− Step" or "+ Step" or "Neutral @ time")
                {
                    secondary.Children.Remove(button);
                    primary.Children.Add(button);
                }
                else
                {
                    button.Visibility = Visibility.Collapsed;
                }
            }
            secondary.Visibility = Visibility.Collapsed;
        }

        if (_studioAnimationInteractionModeText is not null)
            _studioAnimationInteractionModeText.Visibility = Visibility.Collapsed;

        foreach (var button in primary.Children.OfType<Button>())
        {
            button.Height = 23;
            button.MinWidth = 0;
            button.Padding = new Thickness(7, 2, 7, 2);
            button.Margin = new Thickness(0, 0, 3, 0);
        }
    }

    private void CompactStudioPhase17QTimelineStrip(Grid root)
    {
        var timelineCard = root.Children
            .OfType<Border>()
            .FirstOrDefault(border => Grid.GetRow(border) == 3);
        if (timelineCard?.Child is not Grid timelineGrid)
            return;

        timelineCard.Padding = new Thickness(7, 3, 7, 3);
        timelineCard.Margin = new Thickness(0, 4, 7, 0);

        // The dedicated keyframe list duplicated information now visible as markers. Keep the list
        // alive for selection logic, but remove its permanent width from the compact timeline.
        if (timelineGrid.ColumnDefinitions.Count >= 3)
        {
            timelineGrid.ColumnDefinitions[1].Width = new GridLength(0);
            timelineGrid.ColumnDefinitions[2].Width = new GridLength(0);
            timelineGrid.ColumnDefinitions[2].MinWidth = 0;
        }

        if (_studioAnimationKeyframeList is not null)
            _studioAnimationKeyframeList.Visibility = Visibility.Collapsed;
        if (_studioAnimationTimeline is not null)
            _studioAnimationTimeline.Margin = new Thickness(66, 0, 2, 1);

        foreach (var label in timelineGrid.Children
            .OfType<Panel>()
            .SelectMany(panel => panel.Children.OfType<TextBlock>()))
        {
            if (label.Text is "ANIMATION TIMELINE · PHASE 17Q" or "TARGET KEYFRAMES")
                label.Visibility = Visibility.Collapsed;
        }

        if (_studioPhase17QTimelineLanes is null)
            return;

        foreach (var row in _studioPhase17QTimelineLanes.Children.OfType<Grid>())
        {
            row.Height = 17;
            row.Margin = new Thickness(0);
            if (row.ColumnDefinitions.Count > 0)
                row.ColumnDefinitions[0].Width = new GridLength(64);

            if (row.Children.OfType<StackPanel>().FirstOrDefault() is { } labelStack)
            {
                var labels = labelStack.Children.OfType<TextBlock>().ToList();
                if (labels.Count > 0)
                    labels[0].FontSize = 7;
                if (labels.Count > 1)
                    labels[1].Visibility = Visibility.Collapsed;
            }

            if (row.Children.OfType<Canvas>().FirstOrDefault() is { } canvas)
                canvas.Height = 15;
        }
    }
}
