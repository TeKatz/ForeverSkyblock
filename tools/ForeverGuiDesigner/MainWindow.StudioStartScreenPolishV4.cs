using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ForeverGuiDesigner;

/// <summary>
/// Final updater-control polish for the Studio entry screens. Removes the last native
/// ComboBox selection chrome, groups the updater controls more tightly and only uses
/// the strong accent when an actual update action is available.
/// </summary>
public partial class MainWindow
{
    private bool _studioStartScreenPolishV4Initialized;
    private readonly List<StudioV4UpdateActionSurface> _studioV4UpdateActions = [];
    private DispatcherTimer? _studioV4UpdateVisualTimer;

    public void EnableForeverStudioStartScreenPolishV4()
    {
        if (_studioStartScreenPolishV4Initialized)
            return;
        _studioStartScreenPolishV4Initialized = true;

        foreach (var surface in _studioStartUpdateSurfaces)
        {
            ApplyStudioV4ChannelTemplate(surface.Channel);
            TightenStudioV4UpdateLayout(surface.Channel);
            ReplaceStudioV4UpdateActionSurface(surface.Action);
        }

        RefreshStudioV4UpdateActionVisuals();
        _studioV4UpdateVisualTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _studioV4UpdateVisualTimer.Tick += (_, _) => RefreshStudioV4UpdateActionVisuals();
        _studioV4UpdateVisualTimer.Start();
        Closed += (_, _) =>
        {
            _studioV4UpdateVisualTimer?.Stop();
            _studioV4UpdateVisualTimer = null;
        };
    }

    private static void ApplyStudioV4ChannelTemplate(ComboBox combo)
    {
        combo.Width = 154;
        combo.Height = 42;
        combo.Background = Brush("#10191E");
        combo.Foreground = Brush("#E6EFF1");
        combo.BorderBrush = Brush("#334950");
        combo.BorderThickness = new Thickness(1);
        combo.Padding = new Thickness(0);
        combo.FontFamily = new FontFamily("Segoe UI");
        combo.FontSize = 11.5;
        combo.FocusVisualStyle = null;
        combo.Cursor = Cursors.Hand;
        combo.SnapsToDevicePixels = true;
        combo.UseLayoutRounding = true;

        var itemStyle = CreateStudioV4ComboItemStyle();
        combo.ItemContainerStyle = itemStyle;
        foreach (var item in combo.Items.OfType<ComboBoxItem>())
        {
            item.Style = itemStyle;
            item.Foreground = Brush("#DCE8EB");
            item.Background = Brushes.Transparent;
        }

        combo.Template = CreateStudioV4ComboTemplate();
        combo.ApplyTemplate();
    }

    private static ControlTemplate CreateStudioV4ComboTemplate()
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
        chrome.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));

        var contentGrid = new FrameworkElementFactory(typeof(Grid));
        var selectedText = new FrameworkElementFactory(typeof(TextBlock));
        selectedText.SetBinding(TextBlock.TextProperty, new Binding("SelectedItem.Content")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        selectedText.SetValue(TextBlock.ForegroundProperty, Brush("#E6EFF1"));
        selectedText.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Segoe UI"));
        selectedText.SetValue(TextBlock.FontSizeProperty, 11.5);
        selectedText.SetValue(TextBlock.FontWeightProperty, FontWeights.Medium);
        selectedText.SetValue(TextBlock.MarginProperty, new Thickness(12, 0, 34, 0));
        selectedText.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        selectedText.SetValue(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis);
        contentGrid.AppendChild(selectedText);

        var arrow = new FrameworkElementFactory(typeof(TextBlock));
        arrow.Name = "Arrow";
        arrow.SetValue(TextBlock.TextProperty, "⌄");
        arrow.SetValue(TextBlock.ForegroundProperty, Brush("#76909A"));
        arrow.SetValue(TextBlock.FontSizeProperty, 14.0);
        arrow.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        arrow.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 12, 3));
        arrow.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right);
        arrow.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        contentGrid.AppendChild(arrow);

        chrome.AppendChild(contentGrid);
        root.AppendChild(chrome);

        var toggle = new FrameworkElementFactory(typeof(ToggleButton));
        toggle.SetValue(Control.BackgroundProperty, Brushes.Transparent);
        toggle.SetValue(Control.BorderThicknessProperty, new Thickness(0));
        toggle.SetValue(UIElement.FocusableProperty, false);
        toggle.SetValue(Control.CursorProperty, Cursors.Hand);
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
        popupBorder.SetValue(Border.BackgroundProperty, Brush("#0F181D"));
        popupBorder.SetValue(Border.BorderBrushProperty, Brush("#334950"));
        popupBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        popupBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(7));
        popupBorder.SetValue(Border.PaddingProperty, new Thickness(4));
        popupBorder.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 5, 0, 0));
        popupBorder.SetBinding(FrameworkElement.WidthProperty, new Binding("ActualWidth")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });

        var scroll = new FrameworkElementFactory(typeof(ScrollViewer));
        scroll.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);
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
                new Setter(Border.BackgroundProperty, Brush("#15242A"), "Chrome"),
                new Setter(Border.BorderBrushProperty, Brush("#496873"), "Chrome"),
                new Setter(TextBlock.ForegroundProperty, Brush("#91AFB8"), "Arrow")
            }
        });
        template.Triggers.Add(new Trigger
        {
            Property = ComboBox.IsDropDownOpenProperty,
            Value = true,
            Setters =
            {
                new Setter(Border.BackgroundProperty, Brush("#17272D"), "Chrome"),
                new Setter(Border.BorderBrushProperty, Brush("#68C8CB"), "Chrome"),
                new Setter(TextBlock.ForegroundProperty, Brush("#76D6D5"), "Arrow")
            }
        });
        return template;
    }

    private static Style CreateStudioV4ComboItemStyle()
    {
        var style = new Style(typeof(ComboBoxItem));
        style.Setters.Add(new Setter(Control.ForegroundProperty, Brush("#DCE8EB")));
        style.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        style.Setters.Add(new Setter(Control.FocusVisualStyleProperty, null));

        var template = new ControlTemplate(typeof(ComboBoxItem));
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "ItemBorder";
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
        border.SetValue(Border.PaddingProperty, new Thickness(11, 8, 11, 8));

        var text = new FrameworkElementFactory(typeof(TextBlock));
        text.Name = "ItemText";
        text.SetBinding(TextBlock.TextProperty, new Binding("Content")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent)
        });
        text.SetValue(TextBlock.ForegroundProperty, Brush("#DCE8EB"));
        text.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Segoe UI"));
        text.SetValue(TextBlock.FontSizeProperty, 11.5);
        text.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(text);
        template.VisualTree = border;

        template.Triggers.Add(new Trigger
        {
            Property = ComboBoxItem.IsHighlightedProperty,
            Value = true,
            Setters =
            {
                new Setter(Border.BackgroundProperty, Brush("#1A2D34"), "ItemBorder"),
                new Setter(TextBlock.ForegroundProperty, Brush("#F0F7F8"), "ItemText")
            }
        });
        template.Triggers.Add(new Trigger
        {
            Property = ComboBoxItem.IsSelectedProperty,
            Value = true,
            Setters =
            {
                new Setter(Border.BackgroundProperty, Brush("#18363C"), "ItemBorder"),
                new Setter(TextBlock.ForegroundProperty, Brush("#72D7D5"), "ItemText")
            }
        });
        style.Setters.Add(new Setter(Control.TemplateProperty, template));
        return style;
    }

    private static void TightenStudioV4UpdateLayout(ComboBox channel)
    {
        if (VisualTreeHelper.GetParent(channel) is not StackPanel channelBlock ||
            VisualTreeHelper.GetParent(channelBlock) is not Grid grid)
            return;

        var actions = grid.Children
            .OfType<StackPanel>()
            .FirstOrDefault(panel => Grid.GetColumn(panel) == 3);

        if (grid.ColumnDefinitions.Count >= 4)
            grid.ColumnDefinitions[1].Width = new GridLength(56);

        channelBlock.HorizontalAlignment = HorizontalAlignment.Right;
        channelBlock.Margin = new Thickness(0, 0, 0, 0);

        if (channelBlock.Children.OfType<TextBlock>().FirstOrDefault() is { } label)
        {
            label.Foreground = Brush("#60747D");
            label.FontSize = 9;
            label.Margin = new Thickness(2, 0, 0, 6);
        }

        if (actions is not null)
        {
            actions.Margin = new Thickness(12, 0, 0, 0);
            actions.HorizontalAlignment = HorizontalAlignment.Right;
        }
    }

    private void ReplaceStudioV4UpdateActionSurface(Button button)
    {
        if (button.Content is not Border { Child: TextBlock oldLabel })
            return;

        var surface = new Border
        {
            Background = Brush("#1B282E"),
            BorderBrush = Brush("#3B515A"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14, 8, 14, 8)
        };
        var label = new TextBlock
        {
            Text = oldLabel.Text,
            Foreground = Brush("#DDE7EA"),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        surface.Child = label;
        button.Content = surface;

        var state = new StudioV4UpdateActionSurface(button, surface, label);
        _studioV4UpdateActions.Add(state);

        button.MouseEnter += (_, _) => ApplyStudioV4ActionVisual(state, hovered: true);
        button.MouseLeave += (_, _) => ApplyStudioV4ActionVisual(state, hovered: false);
        button.GotKeyboardFocus += (_, _) => ApplyStudioV4ActionVisual(state, hovered: true);
        button.LostKeyboardFocus += (_, _) => ApplyStudioV4ActionVisual(state, hovered: false);
    }

    private void RefreshStudioV4UpdateActionVisuals()
    {
        foreach (var state in _studioV4UpdateActions)
        {
            if (!state.Button.IsMouseOver && !state.Button.IsKeyboardFocusWithin)
                ApplyStudioV4ActionVisual(state, hovered: false);
        }
    }

    private static void ApplyStudioV4ActionVisual(StudioV4UpdateActionSurface state, bool hovered)
    {
        var actionText = state.Label.Text;
        var accent = actionText.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
                     actionText.Contains("Restart", StringComparison.OrdinalIgnoreCase) ||
                     actionText.Contains("Cancel", StringComparison.OrdinalIgnoreCase);

        if (accent)
        {
            state.Surface.Background = Brush(hovered ? "#338A91" : "#28777F");
            state.Surface.BorderBrush = Brush(hovered ? "#82DDDC" : "#56BEC2");
            state.Label.Foreground = Brushes.White;
        }
        else
        {
            state.Surface.Background = Brush(hovered ? "#263840" : "#1B282E");
            state.Surface.BorderBrush = Brush(hovered ? "#58727C" : "#3B515A");
            state.Label.Foreground = Brush(hovered ? "#F0F6F7" : "#DDE7EA");
        }
    }

    private sealed record StudioV4UpdateActionSurface(Button Button, Border Surface, TextBlock Label);
}
