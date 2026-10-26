using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace ForeverGuiDesigner;

/// <summary>
/// Third visual pass for the Studio entry screens. Fixes the remaining native WPF
/// chrome leaks on workspace cards, improves repository-path text rendering and gives
/// the updater controls a darker, more integrated Studio treatment.
/// </summary>
public partial class MainWindow
{
    private bool _studioStartScreenPolishV3Initialized;

    public void EnableForeverStudioStartScreenPolishV3()
    {
        if (_studioStartScreenPolishV3Initialized)
            return;
        _studioStartScreenPolishV3Initialized = true;

        RefineStudioRepositoryPathField();
        RefineStudioWorkspaceCardChrome();
        RefineStudioStartUpdateSurfaces();
    }

    private void RefineStudioRepositoryPathField()
    {
        if (_studioRepoBox is null)
            return;

        _studioRepoBox.FontFamily = new FontFamily("Segoe UI");
        _studioRepoBox.FontSize = 12;
        _studioRepoBox.FontWeight = FontWeights.Normal;
        _studioRepoBox.Padding = new Thickness(11, 0, 11, 0);
        _studioRepoBox.VerticalContentAlignment = VerticalAlignment.Center;
        _studioRepoBox.HorizontalContentAlignment = HorizontalAlignment.Left;
        _studioRepoBox.TextWrapping = TextWrapping.NoWrap;
        _studioRepoBox.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        _studioRepoBox.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        _studioRepoBox.SnapsToDevicePixels = true;
        _studioRepoBox.UseLayoutRounding = true;

        TextOptions.SetTextFormattingMode(_studioRepoBox, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(_studioRepoBox, TextRenderingMode.ClearType);
        TextOptions.SetTextHintingMode(_studioRepoBox, TextHintingMode.Fixed);
    }

    private void RefineStudioWorkspaceCardChrome()
    {
        if (_studioProjectHome is null)
            return;

        foreach (var button in FindStudioVisualChildren<Button>(_studioProjectHome)
                     .Where(ContainsStudioWorkspaceTitle))
        {
            ApplyStudioContentOnlyButtonTemplate(button);
            button.Background = Brushes.Transparent;
            button.BorderBrush = Brushes.Transparent;
            button.BorderThickness = new Thickness(0);
            button.Padding = new Thickness(0);
            button.FocusVisualStyle = null;
            button.SnapsToDevicePixels = true;
            button.UseLayoutRounding = true;
        }
    }

    private void RefineStudioStartUpdateSurfaces()
    {
        foreach (var root in new[] { _studioLauncher, _studioProjectHome }.Where(root => root is not null))
        {
            foreach (var combo in FindStudioVisualChildren<ComboBox>(root!))
            {
                if (!IsStudioUpdateChannelCombo(combo))
                    continue;

                ApplyStudioDarkComboBox(combo);
                ReflowStudioUpdatePanel(combo);
            }
        }
    }

    private static bool IsStudioUpdateChannelCombo(ComboBox combo)
    {
        if (combo.Items.Count != 2)
            return false;

        var tags = combo.Items
            .OfType<ComboBoxItem>()
            .Select(item => item.Tag?.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return tags.Contains("development") && tags.Contains("stable");
    }

    private static void ReflowStudioUpdatePanel(ComboBox channel)
    {
        if (VisualTreeHelper.GetParent(channel) is not StackPanel channelBlock ||
            VisualTreeHelper.GetParent(channelBlock) is not Grid grid)
            return;

        var actions = grid.Children
            .OfType<StackPanel>()
            .FirstOrDefault(panel => Grid.GetColumn(panel) == 3);
        var copy = grid.Children
            .OfType<StackPanel>()
            .FirstOrDefault(panel => Grid.GetColumn(panel) == 0);

        grid.ColumnDefinitions.Clear();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        if (copy is not null)
        {
            Grid.SetColumn(copy, 0);
            copy.Margin = new Thickness(0, 0, 8, 0);
        }

        Grid.SetColumn(channelBlock, 2);
        channelBlock.HorizontalAlignment = HorizontalAlignment.Right;
        channelBlock.Margin = new Thickness(0);

        if (actions is not null)
        {
            Grid.SetColumn(actions, 3);
            actions.Margin = new Thickness(20, 0, 0, 0);
            actions.HorizontalAlignment = HorizontalAlignment.Right;
        }
    }

    private static void ApplyStudioDarkComboBox(ComboBox combo)
    {
        combo.Background = Brush("#111A1F");
        combo.Foreground = Brush("#E3ECEF");
        combo.BorderBrush = Brush("#354A53");
        combo.BorderThickness = new Thickness(1);
        combo.Padding = new Thickness(0);
        combo.FontFamily = new FontFamily("Segoe UI");
        combo.FontSize = 11.5;
        combo.SnapsToDevicePixels = true;
        combo.UseLayoutRounding = true;
        combo.FocusVisualStyle = null;
        combo.ItemContainerStyle = CreateStudioDarkComboBoxItemStyle();
        combo.Template = CreateStudioDarkComboBoxTemplate();
        combo.ApplyTemplate();
    }

    private static ControlTemplate CreateStudioDarkComboBoxTemplate()
    {
        var template = new ControlTemplate(typeof(ComboBox));
        var root = new FrameworkElementFactory(typeof(Grid));

        var chrome = new FrameworkElementFactory(typeof(Border));
        chrome.Name = "Chrome";
        chrome.SetBinding(Border.BackgroundProperty, new Binding(nameof(Control.Background))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        chrome.SetBinding(Border.BorderBrushProperty, new Binding(nameof(Control.BorderBrush))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        chrome.SetBinding(Border.BorderThicknessProperty, new Binding(nameof(Control.BorderThickness))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

        var chromeGrid = new FrameworkElementFactory(typeof(Grid));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetBinding(ContentPresenter.ContentProperty, new Binding("SelectionBoxItem")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        presenter.SetBinding(ContentPresenter.ContentTemplateProperty, new Binding("SelectionBoxItemTemplate")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        presenter.SetValue(ContentPresenter.MarginProperty, new Thickness(11, 0, 32, 0));
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        chromeGrid.AppendChild(presenter);

        var arrow = new FrameworkElementFactory(typeof(TextBlock));
        arrow.SetValue(TextBlock.TextProperty, "▾");
        arrow.SetValue(TextBlock.ForegroundProperty, Brush("#7F959E"));
        arrow.SetValue(TextBlock.FontSizeProperty, 11.0);
        arrow.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 11, 1));
        arrow.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
        arrow.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        chromeGrid.AppendChild(arrow);
        chrome.AppendChild(chromeGrid);
        root.AppendChild(chrome);

        var toggle = new FrameworkElementFactory(typeof(ToggleButton));
        toggle.SetValue(Control.BackgroundProperty, Brushes.Transparent);
        toggle.SetValue(Control.BorderThicknessProperty, new Thickness(0));
        toggle.SetValue(UIElement.FocusableProperty, false);
        toggle.SetValue(Control.TemplateProperty, CreateStudioTransparentToggleTemplate());
        toggle.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(ComboBox.IsDropDownOpen))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
            Mode = BindingMode.TwoWay
        });
        root.AppendChild(toggle);

        var popup = new FrameworkElementFactory(typeof(Popup));
        popup.Name = "PART_Popup";
        popup.SetValue(Popup.PlacementProperty, PlacementMode.Bottom);
        popup.SetValue(Popup.AllowsTransparencyProperty, true);
        popup.SetValue(Popup.PopupAnimationProperty, PopupAnimation.Fade);
        popup.SetBinding(Popup.IsOpenProperty, new Binding(nameof(ComboBox.IsDropDownOpen))
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });

        var popupBorder = new FrameworkElementFactory(typeof(Border));
        popupBorder.SetValue(Border.BackgroundProperty, Brush("#111A1F"));
        popupBorder.SetValue(Border.BorderBrushProperty, Brush("#354A53"));
        popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        popupBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
        popupBorder.SetValue(Border.PaddingProperty, new Thickness(3));
        popupBorder.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 4, 0, 0));
        popupBorder.SetBinding(FrameworkElement.WidthProperty, new Binding("ActualWidth")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });

        var scroll = new FrameworkElementFactory(typeof(ScrollViewer));
        scroll.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
        scroll.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
        var items = new FrameworkElementFactory(typeof(ItemsPresenter));
        scroll.AppendChild(items);
        popupBorder.AppendChild(scroll);
        popup.AppendChild(popupBorder);
        root.AppendChild(popup);

        template.VisualTree = root;
        template.Triggers.Add(new Trigger
        {
            Property = UIElement.IsMouseOverProperty,
            Value = true,
            Setters =
            {
                new Setter(Border.BorderBrushProperty, Brush("#5B7A83"), "Chrome")
            }
        });
        template.Triggers.Add(new Trigger
        {
            Property = ComboBox.IsDropDownOpenProperty,
            Value = true,
            Setters =
            {
                new Setter(Border.BorderBrushProperty, Brush("#69C9CC"), "Chrome")
            }
        });
        return template;
    }

    private static ControlTemplate CreateStudioTransparentToggleTemplate()
    {
        var template = new ControlTemplate(typeof(ToggleButton));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        template.VisualTree = border;
        return template;
    }

    private static Style CreateStudioDarkComboBoxItemStyle()
    {
        var style = new Style(typeof(ComboBoxItem));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#DDE8EB")));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 7, 10, 7)));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));

        var template = new ControlTemplate(typeof(ComboBoxItem));
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "ItemBorder";
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        border.SetValue(Border.PaddingProperty, new Thickness(9, 6, 9, 6));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        border.AppendChild(presenter);
        template.VisualTree = border;
        template.Triggers.Add(new Trigger
        {
            Property = ComboBoxItem.IsHighlightedProperty,
            Value = true,
            Setters =
            {
                new Setter(Border.BackgroundProperty, Brush("#203139"), "ItemBorder")
            }
        });
        template.Triggers.Add(new Trigger
        {
            Property = ComboBoxItem.IsSelectedProperty,
            Value = true,
            Setters =
            {
                new Setter(Border.BackgroundProperty, Brush("#19343A"), "ItemBorder"),
                new Setter(Control.ForegroundProperty, Brush("#7CDBD8"))
            }
        });
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }

    private static void ApplyStudioContentOnlyButtonTemplate(Button button)
    {
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.ContentSourceProperty, "Content");
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Stretch);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Stretch);
        presenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);

        button.Template = new ControlTemplate(typeof(Button))
        {
            VisualTree = presenter
        };
        button.ApplyTemplate();
    }
}
