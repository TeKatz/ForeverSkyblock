using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ForeverGuiDesigner;

/// <summary>
/// Replaces the visible update-channel ComboBox with a purpose-built two-state Studio
/// segmented control. The original ComboBox remains collapsed as the synchronization
/// anchor for the existing updater logic, so behavior stays identical while presentation
/// is fully controlled by the Studio UI layer.
/// </summary>
public partial class MainWindow
{
    private bool _studioStartScreenPolishV5Initialized;
    private readonly List<StudioChannelSegmentSurface> _studioChannelSegmentSurfaces = [];

    public void EnableForeverStudioStartScreenPolishV5()
    {
        if (_studioStartScreenPolishV5Initialized)
            return;
        _studioStartScreenPolishV5Initialized = true;

        foreach (var surface in _studioStartUpdateSurfaces)
            ReplaceStudioUpdateChannelWithSegments(surface.Channel, surface.Action);
    }

    private void ReplaceStudioUpdateChannelWithSegments(ComboBox source, Button updateAction)
    {
        if (VisualTreeHelper.GetParent(source) is not StackPanel channelBlock ||
            VisualTreeHelper.GetParent(channelBlock) is not Grid hostGrid)
            return;

        source.Visibility = Visibility.Collapsed;

        var segment = CreateStudioChannelSegment(source);
        channelBlock.Children.Add(segment.Root);

        var actions = hostGrid.Children
            .OfType<StackPanel>()
            .FirstOrDefault(panel => Grid.GetColumn(panel) == 3);

        channelBlock.HorizontalAlignment = HorizontalAlignment.Right;
        channelBlock.VerticalAlignment = VerticalAlignment.Center;
        channelBlock.Margin = new Thickness(0);

        if (channelBlock.Children.OfType<TextBlock>().FirstOrDefault() is { } label)
        {
            label.FontFamily = new FontFamily("Segoe UI");
            label.FontSize = 9;
            label.FontWeight = FontWeights.SemiBold;
            label.Foreground = Brush("#657B84");
            label.Margin = new Thickness(2, 0, 0, 6);
        }

        if (actions is not null)
        {
            actions.VerticalAlignment = VerticalAlignment.Bottom;
            actions.HorizontalAlignment = HorizontalAlignment.Right;
            actions.Margin = new Thickness(12, 0, 0, 0);

            var buttons = actions.Children.OfType<Button>().ToArray();
            if (buttons.Length > 0)
            {
                buttons[0].Width = 70;
                buttons[0].Height = 40;
            }
            if (buttons.Length > 1)
            {
                buttons[1].Width = 126;
                buttons[1].Height = 40;
            }
        }

        updateAction.Width = 126;
        updateAction.Height = 40;

        source.SelectionChanged += (_, _) => RefreshStudioChannelSegment(segment);
        RefreshStudioChannelSegment(segment);
        _studioChannelSegmentSurfaces.Add(segment);
    }

    private StudioChannelSegmentSurface CreateStudioChannelSegment(ComboBox source)
    {
        var root = new Border
        {
            Width = 208,
            Height = 40,
            Background = Brush("#10191E"),
            BorderBrush = Brush("#354B54"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
            ClipToBounds = true
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var development = CreateStudioChannelSegmentButton("Development", leftSide: true);
        development.Click += (_, _) => source.SelectedIndex = 0;
        Grid.SetColumn(development.Button, 0);
        grid.Children.Add(development.Button);

        var divider = new Border
        {
            Background = Brush("#354B54"),
            Width = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false
        };
        Grid.SetColumn(divider, 1);
        grid.Children.Add(divider);

        var stable = CreateStudioChannelSegmentButton("Stable", leftSide: false);
        stable.Click += (_, _) => source.SelectedIndex = 1;
        Grid.SetColumn(stable.Button, 2);
        grid.Children.Add(stable.Button);

        root.Child = grid;

        var surface = new StudioChannelSegmentSurface(
            source,
            root,
            development.Button,
            development.Surface,
            development.Label,
            stable.Button,
            stable.Surface,
            stable.Label);

        development.Button.MouseEnter += (_, _) => ApplyStudioChannelSegmentVisual(surface, development: true, hovered: true);
        development.Button.MouseLeave += (_, _) => ApplyStudioChannelSegmentVisual(surface, development: true, hovered: false);
        development.Button.GotKeyboardFocus += (_, _) => ApplyStudioChannelSegmentVisual(surface, development: true, hovered: true);
        development.Button.LostKeyboardFocus += (_, _) => ApplyStudioChannelSegmentVisual(surface, development: true, hovered: false);

        stable.Button.MouseEnter += (_, _) => ApplyStudioChannelSegmentVisual(surface, development: false, hovered: true);
        stable.Button.MouseLeave += (_, _) => ApplyStudioChannelSegmentVisual(surface, development: false, hovered: false);
        stable.Button.GotKeyboardFocus += (_, _) => ApplyStudioChannelSegmentVisual(surface, development: false, hovered: true);
        stable.Button.LostKeyboardFocus += (_, _) => ApplyStudioChannelSegmentVisual(surface, development: false, hovered: false);

        return surface;
    }

    private static StudioChannelSegmentButton CreateStudioChannelSegmentButton(string text, bool leftSide)
    {
        var button = new Button
        {
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Cursor = Cursors.Hand,
            FocusVisualStyle = null,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };
        ApplyStudioContentOnlyButtonTemplate(button);

        var surface = new Border
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            CornerRadius = leftSide
                ? new CornerRadius(6, 0, 0, 6)
                : new CornerRadius(0, 6, 6, 0),
            Padding = new Thickness(10, 0, 10, 0)
        };

        var label = new TextBlock
        {
            Text = text,
            Foreground = Brush("#A8BAC0"),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11,
            FontWeight = FontWeights.Medium,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        surface.Child = label;
        button.Content = surface;
        return new StudioChannelSegmentButton(button, surface, label);
    }

    private static void RefreshStudioChannelSegment(StudioChannelSegmentSurface surface)
    {
        ApplyStudioChannelSegmentVisual(surface, development: true, hovered: false);
        ApplyStudioChannelSegmentVisual(surface, development: false, hovered: false);
    }

    private static void ApplyStudioChannelSegmentVisual(
        StudioChannelSegmentSurface surface,
        bool development,
        bool hovered)
    {
        var selectedIndex = surface.Source.SelectedIndex < 0 ? 0 : surface.Source.SelectedIndex;
        var selected = development ? selectedIndex == 0 : selectedIndex == 1;

        var targetSurface = development ? surface.DevelopmentSurface : surface.StableSurface;
        var targetLabel = development ? surface.DevelopmentLabel : surface.StableLabel;

        if (selected)
        {
            targetSurface.Background = Brush(hovered ? "#245057" : "#1D4248");
            targetLabel.Foreground = Brush(hovered ? "#8DE3E0" : "#77D5D3");
            targetLabel.FontWeight = FontWeights.SemiBold;
            surface.Root.BorderBrush = Brush(hovered ? "#55737D" : "#405B64");
        }
        else
        {
            targetSurface.Background = Brush(hovered ? "#18272D" : "#10191E");
            targetLabel.Foreground = Brush(hovered ? "#D7E4E7" : "#849AA2");
            targetLabel.FontWeight = FontWeights.Medium;
            if (!surface.DevelopmentButton.IsMouseOver && !surface.StableButton.IsMouseOver)
                surface.Root.BorderBrush = Brush("#354B54");
        }
    }

    private sealed record StudioChannelSegmentButton(Button Button, Border Surface, TextBlock Label);

    private sealed record StudioChannelSegmentSurface(
        ComboBox Source,
        Border Root,
        Button DevelopmentButton,
        Border DevelopmentSurface,
        TextBlock DevelopmentLabel,
        Button StableButton,
        Border StableSurface,
        TextBlock StableLabel);
}
