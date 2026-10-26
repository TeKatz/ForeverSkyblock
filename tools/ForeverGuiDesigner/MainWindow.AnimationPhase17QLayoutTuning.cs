using System.Windows;
using System.Windows.Controls;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17Q layout tuning based on real 1920x1080 workstation use. The first workstation shell
/// proved the interaction model, but spent too much vertical space on chrome and made the inspector
/// too short. This pass keeps every Phase-17Q control and handler intact while rebalancing the
/// workspace around the 3D viewport: a taller inspector, a viewport-local compact timeline and a
/// single-row transform toolbar.
/// </summary>
public partial class MainWindow
{
    private bool _studioAnimationPhase17QLayoutTuningInitialized;

    public void EnableForeverAnimationPhase17QLayoutTuning()
    {
        if (_studioAnimationPhase17QLayoutTuningInitialized ||
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

        _studioAnimationPhase17QLayoutTuningInitialized = true;

        RebalanceStudioPhase17QWorkspace(root, viewportCard, inspectorCard);
        CompactStudioPhase17QCommandArea(root);
        CompactStudioPhase17QTransformToolbar();
        CompactStudioPhase17QTimeline(root);

        if (_studioPhase17QTimelineLanes is not null)
        {
            _studioPhase17QTimelineLanes.LayoutUpdated += (_, _) =>
            {
                if (_studioPhase17QTimelineLanes.Children
                    .OfType<Grid>()
                    .Any(row => row.Height > 24.1))
                {
                    CompactStudioPhase17QTimelineLanes();
                }
            };
        }

        CompactStudioPhase17QTimelineLanes();
    }

    private void RebalanceStudioPhase17QWorkspace(
        Grid root,
        Border viewportCard,
        Border inspectorCard)
    {
        // Keep the top command area auto-sized, give the viewport the remaining height and make the
        // timeline a compact strip below the viewport only. The inspector now spans both rows, so it
        // gains the timeline height instead of becoming the smallest part of the editor.
        root.RowDefinitions[1].MinHeight = 320;
        root.RowDefinitions[2].Height = new GridLength(5);
        root.RowDefinitions[3].Height = new GridLength(118);
        root.RowDefinitions[3].MinHeight = 92;
        root.RowDefinitions[3].MaxHeight = 150;

        root.ColumnDefinitions[0].MinWidth = 620;
        root.ColumnDefinitions[1].Width = new GridLength(5);
        root.ColumnDefinitions[2].Width = new GridLength(520);
        root.ColumnDefinitions[2].MinWidth = 460;
        root.ColumnDefinitions[2].MaxWidth = 760;

        viewportCard.Margin = new Thickness(0, 0, 7, 0);
        inspectorCard.Margin = new Thickness(7, 0, 0, 0);
        Grid.SetRow(inspectorCard, 1);
        Grid.SetRowSpan(inspectorCard, 3);

        var verticalSplitter = root.Children
            .OfType<GridSplitter>()
            .FirstOrDefault(splitter => Grid.GetColumn(splitter) == 1);
        if (verticalSplitter is not null)
        {
            verticalSplitter.Width = 5;
            Grid.SetRow(verticalSplitter, 1);
            Grid.SetRowSpan(verticalSplitter, 3);
        }

        var horizontalSplitter = root.Children
            .OfType<GridSplitter>()
            .FirstOrDefault(splitter => Grid.GetRow(splitter) == 2);
        if (horizontalSplitter is not null)
        {
            horizontalSplitter.Height = 5;
            Grid.SetColumn(horizontalSplitter, 0);
            Grid.SetColumnSpan(horizontalSplitter, 1);
        }

        var timelineCard = root.Children
            .OfType<Border>()
            .FirstOrDefault(border => Grid.GetRow(border) == 3);
        if (timelineCard is not null)
        {
            Grid.SetColumn(timelineCard, 0);
            Grid.SetColumnSpan(timelineCard, 1);
            timelineCard.Margin = new Thickness(0, 6, 7, 0);
        }
    }

    private void CompactStudioPhase17QCommandArea(Grid root)
    {
        var commandCard = root.Children
            .OfType<Border>()
            .FirstOrDefault(border => Grid.GetRow(border) == 0 && Grid.GetColumnSpan(border) >= 3);
        if (commandCard?.Child is not StackPanel shell)
            return;

        commandCard.Padding = new Thickness(9, 5, 9, 5);
        commandCard.Margin = new Thickness(0, 0, 0, 6);

        var commandRow = shell.Children.OfType<WrapPanel>().FirstOrDefault();
        if (commandRow is not null)
        {
            foreach (var cell in commandRow.Children.OfType<StackPanel>())
            {
                cell.Margin = new Thickness(0, 0, 9, 2);

                // The controls are self-explanatory in this dense workstation bar. Keep the labels
                // in the tree for semantics/tooling, but stop paying a full text row for each group.
                foreach (var label in cell.Children.OfType<TextBlock>())
                    label.Visibility = Visibility.Collapsed;

                foreach (var button in cell.Children.OfType<WrapPanel>().SelectMany(panel => panel.Children.OfType<Button>()))
                    button.Height = 27;
            }
        }

        if (_studioPhase17QTargetSummary is not null)
            _studioPhase17QTargetSummary.Visibility = Visibility.Collapsed;
        if (_studioPhase17QInterpolationHint is not null)
            _studioPhase17QInterpolationHint.Visibility = Visibility.Collapsed;

        foreach (var hint in shell.Children.OfType<TextBlock>())
        {
            if (hint.Text.StartsWith("Right-click the viewport", StringComparison.OrdinalIgnoreCase))
                hint.Visibility = Visibility.Collapsed;
        }
    }

    private void CompactStudioPhase17QTransformToolbar()
    {
        var firstManipulationButton = _studioAnimationManipulationButtons.FirstOrDefault();
        DependencyObject? current = firstManipulationButton;
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

        toolbar.Background = Brush("#00000000");
        toolbar.BorderThickness = new Thickness(0);
        toolbar.Padding = new Thickness(0);
        toolbar.Margin = new Thickness(0, 2, 0, 0);
        transformRoot.MaxWidth = double.PositiveInfinity;

        foreach (var title in transformRoot.Children.OfType<TextBlock>())
        {
            if (title.Text.Equals("ANIMATION TRANSFORM", StringComparison.OrdinalIgnoreCase))
                title.Visibility = Visibility.Collapsed;
        }

        var rows = transformRoot.Children.OfType<WrapPanel>().ToList();
        if (rows.Count == 0)
            return;

        var modes = rows[0];
        modes.Margin = new Thickness(0);

        if (rows.Count > 1)
        {
            var commands = rows[1];
            var keep = commands.Children
                .OfType<Button>()
                .Where(button => button.Content?.ToString() is "− Step" or "+ Step" or "Neutral @ time")
                .ToList();
            foreach (var button in keep)
            {
                commands.Children.Remove(button);
                button.Height = 25;
                button.Margin = new Thickness(0, 0, 4, 0);
                modes.Children.Add(button);
            }

            foreach (var duplicateIdentify in commands.Children
                .OfType<Button>()
                .Where(button => string.Equals(button.Content?.ToString(), "Identify", StringComparison.Ordinal))
                .ToList())
            {
                duplicateIdentify.Visibility = Visibility.Collapsed;
            }

            if (_studioAnimationInteractionModeText is not null)
                _studioAnimationInteractionModeText.Visibility = Visibility.Collapsed;
            commands.Visibility = Visibility.Collapsed;
        }

        foreach (var button in modes.Children.OfType<Button>())
        {
            button.Height = 25;
            button.Margin = new Thickness(0, 0, 4, 0);
        }
    }

    private void CompactStudioPhase17QTimeline(Grid root)
    {
        var timelineCard = root.Children
            .OfType<Border>()
            .FirstOrDefault(border => Grid.GetRow(border) == 3);
        if (timelineCard?.Child is not Grid timelineGrid)
            return;

        timelineCard.Padding = new Thickness(8, 5, 8, 5);
        if (timelineGrid.ColumnDefinitions.Count >= 3)
        {
            timelineGrid.ColumnDefinitions[1].Width = new GridLength(8);
            timelineGrid.ColumnDefinitions[2].Width = new GridLength(220);
            timelineGrid.ColumnDefinitions[2].MinWidth = 180;
        }

        if (_studioAnimationTimeline is not null)
            _studioAnimationTimeline.Margin = new Thickness(82, 0, 2, 3);
        if (_studioAnimationKeyframeList is not null)
            _studioAnimationKeyframeList.MinHeight = 54;

        foreach (var label in timelineGrid
            .Children
            .OfType<Panel>()
            .SelectMany(panel => panel.Children.OfType<TextBlock>()))
        {
            if (label.Text is "ANIMATION TIMELINE · PHASE 17Q" or "TARGET KEYFRAMES")
                label.FontSize = 8;
        }
    }

    private void CompactStudioPhase17QTimelineLanes()
    {
        if (_studioPhase17QTimelineLanes is null)
            return;

        foreach (var row in _studioPhase17QTimelineLanes.Children.OfType<Grid>())
        {
            row.Height = 24;
            row.Margin = new Thickness(0);

            if (row.Children.OfType<StackPanel>().FirstOrDefault() is { } labelStack)
            {
                var labels = labelStack.Children.OfType<TextBlock>().ToList();
                if (labels.Count > 0)
                    labels[0].FontSize = 8;
                if (labels.Count > 1)
                    labels[1].Visibility = Visibility.Collapsed;
            }
        }
    }
}
