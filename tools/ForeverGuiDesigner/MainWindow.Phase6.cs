using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private const string Phase6WashingOverlayTag = "__forever_phase6_washing_overlay";
    private const string Phase6ComboTag = "__forever_phase6_combo";

    private ComboBox? _phase6WashingStatusCombo;
    private CheckBox? _phase6PlayAnimations;
    private DispatcherTimer? _phase6Timer;
    private double _phase6AnimationClock;
    private bool _phase6Initialized;
    private bool _phase6RefreshQueued;
    private bool _phase6Refreshing;
    private bool _phase6LoadRequested;

    public void EnableForeverPhase6Tools()
    {
        if (_phase6Initialized)
            return;
        _phase6Initialized = true;

        ApplyThemeSafeComboBoxes();
        SkyblockBarHost.LayoutUpdated += (_, _) => ApplyThemeSafeComboBoxes();

        if (SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content as string, "Load machine", StringComparison.Ordinal)) is { } loadButton)
        {
            loadButton.PreviewMouseLeftButtonDown += (_, _) => _phase6LoadRequested = true;
        }

        SkyblockBarHost.Children.Add(SeparatorBar());
        SkyblockBarHost.Children.Add(SectionLabel("SCENE"));

        _phase6WashingStatusCombo = new ComboBox
        {
            Width = 104,
            ToolTip = "Preview the current Washing Basin runtime state."
        };
        _phase6WashingStatusCombo.Items.Add(ComboItem("Running", "running"));
        _phase6WashingStatusCombo.Items.Add(ComboItem("Idle", "idle"));
        _phase6WashingStatusCombo.Items.Add(ComboItem("Blocked", "blocked"));
        _phase6WashingStatusCombo.Items.Add(ComboItem("No water", "no_water"));
        _phase6WashingStatusCombo.SelectedIndex = 0;
        _phase6WashingStatusCombo.SelectionChanged += (_, _) => QueuePhase6Refresh();
        SkyblockBarHost.Children.Add(_phase6WashingStatusCombo);

        _phase6PlayAnimations = new CheckBox
        {
            Content = "Animate",
            IsChecked = true,
            Foreground = Brush("#DCE7EC"),
            ToolTip = "Animate decorative process effects and the progress-meter highlight."
        };
        _phase6PlayAnimations.Checked += (_, _) => QueuePhase6Refresh();
        _phase6PlayAnimations.Unchecked += (_, _) => QueuePhase6Refresh();
        SkyblockBarHost.Children.Add(_phase6PlayAnimations);

        _phase6Timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _phase6Timer.Tick += (_, _) =>
        {
            if (!IsWashingPhase6Active || _phase6PlayAnimations?.IsChecked != true || CurrentWashingStatus != "running")
                return;
            _phase6AnimationClock += .05;
            QueuePhase6Refresh();
        };
        _phase6Timer.Start();

        if (_foreverTemplateCombo is not null)
            _foreverTemplateCombo.SelectionChanged += (_, _) => QueuePhase6Refresh();

        PreviewCanvas.LayoutUpdated += (_, _) =>
        {
            if (_phase6LoadRequested || (IsWashingPhase6Active && !HasWashingOverlay()))
                QueuePhase6Refresh();
        };

        QueuePhase6Refresh();
    }

    private bool IsWashingPhase6Active
        => _activeForeverTemplate is { ScreenClass: "WashingScreen" } template &&
           string.Equals(_layout.Name, template.DisplayName, StringComparison.Ordinal) &&
           _layout.Width == template.Layout.Width &&
           _layout.Height == template.Layout.Height;

    private string CurrentWashingStatus
        => _phase6WashingStatusCombo?.SelectedItem is ComboBoxItem { Tag: string tag } ? tag : "running";

    private bool WashingHasWater => CurrentWashingStatus != "no_water";
    private bool WashingRunning => CurrentWashingStatus == "running";
    private bool WashingBlocked => CurrentWashingStatus == "blocked";

    private void QueuePhase6Refresh()
    {
        if (_phase6RefreshQueued || _phase6Refreshing)
            return;
        _phase6RefreshQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(RefreshPhase6Presentation));
    }

    private void RefreshPhase6Presentation()
    {
        _phase6RefreshQueued = false;
        if (_phase6Refreshing)
            return;

        _phase6Refreshing = true;
        try
        {
            ApplyThemeSafeComboBoxes();
            UpdatePhase6ControlAvailability();
            _phase6LoadRequested = false;

            if (IsWashingPhase6Active && EnsureWashingPhase6Elements())
            {
                RefreshElementList();
                RenderLayout();

                // RenderLayout clears every generated PreviewCanvas child. Reinsert the
                // project backdrop immediately so the first Washing load reaches the
                // exact same presentation state as a later Visual preview toggle.
                EnsureForeverVisualPreview();
            }

            if (!IsWashingPhase6Active)
            {
                RemoveWashingOverlay();
                return;
            }

            // Finish the complete presentation in the same refresh that created the
            // Phase 6 elements. Previously this depended on a LayoutUpdated callback,
            // but that callback fires while _phase6Refreshing is still true and its
            // queued refresh is therefore discarded on the initial machine load.
            DecorateWashingElements();
            EnsureWashingOverlay();
            EnsureStableWashingAnimationVisuals();
            UpdateStableWashingAnimationVisuals();
            UpdateSelectionVisuals();
        }
        finally
        {
            _phase6Refreshing = false;
        }
    }

    private void UpdatePhase6ControlAvailability()
    {
        var washing = IsWashingPhase6Active;
        if (_phase6WashingStatusCombo is not null)
        {
            _phase6WashingStatusCombo.IsEnabled = washing;
            _phase6WashingStatusCombo.Visibility = washing ? Visibility.Visible : Visibility.Collapsed;
        }
        if (_phase6PlayAnimations is not null)
        {
            _phase6PlayAnimations.IsEnabled = washing;
            _phase6PlayAnimations.Visibility = washing ? Visibility.Visible : Visibility.Collapsed;
        }

        var condenser = IsCondenserPhase5Active;
        if (_foreverTierCombo is not null)
            _foreverTierCombo.Visibility = condenser ? Visibility.Visible : Visibility.Collapsed;
        if (_foreverModeCombo is not null)
            _foreverModeCombo.Visibility = condenser ? Visibility.Visible : Visibility.Collapsed;

        if (_foreverProgressCombo is not null)
        {
            _foreverProgressCombo.IsEnabled = condenser || washing || _layout.Elements.Any(IsForeverMeter);
            _foreverProgressCombo.Visibility = condenser || washing || _layout.Elements.Any(IsForeverMeter)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        foreach (var text in SkyblockBarHost.Children.OfType<TextBlock>())
        {
            if (string.Equals(text.Text, "Progress", StringComparison.Ordinal))
                text.Visibility = condenser || washing ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private bool EnsureWashingPhase6Elements()
    {
        if (!IsWashingPhase6Active || _layout.Elements.Any(e => e.Id.StartsWith("forever_phase6_washing_", StringComparison.OrdinalIgnoreCase)))
            return false;

        void Add(string id, GuiElementType type, int x, int y, int width, int height, string content)
            => _layout.Elements.Add(new GuiElementModel
            {
                Id = id,
                Type = type,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Content = content
            });

        Add("forever_phase6_washing_status", GuiElementType.Texture, 177, 12, 4, 4, "Status");
        Add("forever_phase6_washing_status_text", GuiElementType.Text, 186, 10, 43, 11, "WASHING");
        Add("forever_phase6_washing_panel_water", GuiElementType.Texture, 18, 35, 212, 18, "Water connection");
        Add("forever_phase6_washing_water_text", GuiElementType.Text, 22, 39, 204, 10, "WATER CONNECTED");
        Add("forever_phase6_washing_panel_process", GuiElementType.Texture, 18, 58, 212, 74, "Washing process");
        Add("forever_phase6_washing_label_input", GuiElementType.Text, 23, 63, 54, 10, "INPUT");
        Add("forever_phase6_washing_label_output", GuiElementType.Text, 160, 63, 70, 10, "OUTPUT");
        Add("forever_phase6_washing_process_scene", GuiElementType.Texture, 83, 74, 82, 27, "Washing animation");
        Add("forever_phase6_washing_time", GuiElementType.Text, 79, 102, 90, 9, "2.5 / 5.0 s");
        Add("forever_phase6_washing_meter_progress", GuiElementType.Texture, 83, 111, 82, 3, "Washing progress");
        Add("forever_phase6_washing_recipe_summary", GuiElementType.Text, 26, 120, 196, 10, "INPUT → OUTPUT + SECONDARY");
        Add("forever_phase6_washing_panel_hints", GuiElementType.Texture, 18, 138, 138, 34, "Washing hints");
        Add("forever_phase6_washing_hint_source", GuiElementType.Text, 25, 144, 124, 10, "Wash valid materials here");
        Add("forever_phase6_washing_hint_water", GuiElementType.Text, 25, 156, 124, 10, "Requires connected water");
        return true;
    }

    private void DecorateWashingElements()
    {
        if (!IsWashingPhase6Active)
            return;

        var accent = WashingHasWater ? "#9DDDF0" : "#FFBF76";
        var status = CurrentWashingStatus switch
        {
            "blocked" => "OUTPUT FULL",
            "no_water" => "NO WATER",
            "idle" => "IDLE",
            _ => "WASHING"
        };

        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            if (!model.Id.StartsWith("forever_phase6_washing_", StringComparison.OrdinalIgnoreCase) || pair.Value is not Border border)
                continue;

            border.Cursor = System.Windows.Input.Cursors.SizeAll;
            border.ClipToBounds = true;

            if (model.Id == "forever_phase6_washing_status")
            {
                border.Background = Brush(WashingBlocked || !WashingHasWater ? "#FFBF76" : WashingRunning ? "#76D8D2" : "#A0B4BE");
                border.BorderBrush = Brushes.Transparent;
                border.BorderThickness = new Thickness(0);
                border.Child = null;
            }
            else if (model.Id == "forever_phase6_washing_status_text")
            {
                SetWashingText(border, status, WashingBlocked || !WashingHasWater ? "#FFBF76" : WashingRunning ? "#76D8D2" : "#A0B4BE", 6.4, HorizontalAlignment.Left);
            }
            else if (model.Id == "forever_phase6_washing_panel_water")
            {
                SetWashingPanel(border, WashingHasWater ? "#355B66" : "#5C4939");
            }
            else if (model.Id == "forever_phase6_washing_water_text")
            {
                SetWashingText(border, WashingHasWater ? "WATER CONNECTED" : "WATER REQUIRED", accent, 6.6, HorizontalAlignment.Center);
            }
            else if (model.Id == "forever_phase6_washing_panel_process")
            {
                SetWashingPanel(border, "#54616D");
            }
            else if (model.Id == "forever_phase6_washing_process_scene")
            {
                border.Background = Brushes.Transparent;
                border.BorderThickness = new Thickness(0);
                border.Child = CreateWashingProcessScene(model.Width, model.Height);
            }
            else if (model.Id == "forever_phase6_washing_meter_progress")
            {
                border.Background = Brushes.Transparent;
                border.BorderThickness = new Thickness(0);
                border.Child = CreateWashingMeter(model.Width, model.Height);
            }
            else if (model.Id == "forever_phase6_washing_time")
            {
                SetWashingText(border, $"{CurrentProgress * 5.0:0.0} / 5.0 s", "#A0B4BE", 5.8, HorizontalAlignment.Center);
            }
            else if (model.Id == "forever_phase6_washing_recipe_summary")
            {
                SetWashingText(border, model.Content, "#E6EEE9", 5.9, HorizontalAlignment.Center);
            }
            else if (model.Id == "forever_phase6_washing_panel_hints")
            {
                SetWashingPanel(border, "#465A68");
            }
            else if (model.Type == GuiElementType.Text)
            {
                var color = model.Id.Contains("label_", StringComparison.OrdinalIgnoreCase) ? "#A0B4BE" : "#A0B4BE";
                SetWashingText(border, model.Content, color, 6.1,
                    model.Id.Contains("hint_", StringComparison.OrdinalIgnoreCase) ? HorizontalAlignment.Left : HorizontalAlignment.Center);
            }
        }
    }

    private FrameworkElement CreateWashingProcessScene(int width, int height)
    {
        var canvas = new Canvas { Width = width, Height = height, ClipToBounds = true };
        var bg = new Rectangle
        {
            Width = width,
            Height = Math.Max(1, height - 1),
            Fill = new LinearGradientBrush(Brush("#102431").Color, Brush("#224754").Color, 90)
        };
        canvas.Children.Add(bg);

        AddPhase6Rect(canvas, 0, Math.Max(0, height - 2), width, 2, "#708E98");
        AddPhase6Rect(canvas, 5, 16, Math.Max(1, width - 10), 2, "#839FA3");
        for (var i = 7; i < width - 6; i += 6)
            AddPhase6Rect(canvas, i, 17, 3, 4, "#294652");

        var phase = CurrentProgress;
        if (WashingRunning && _phase6PlayAnimations?.IsChecked == true)
            phase = (phase + _phase6AnimationClock * .14) % 1.0;

        var effectColor = WashingBlocked ? "#E4B47C" : "#9DDDF0";
        var rgb = Brush(effectColor).Color;
        for (var i = 0; i < 4; i++)
        {
            var t = (phase * 8 + i * .25) % 1.0;
            var alpha = (byte)Math.Clamp(Math.Round(Math.Sin(Math.PI * t) * 150), 0, 255);
            var radius = 3 + (int)Math.Round(t * (width / 2.0 - 6));
            var line = new Rectangle
            {
                Width = Math.Max(1, radius * 2),
                Height = 1,
                Fill = new SolidColorBrush(Color.FromArgb(alpha, rgb.R, rgb.G, rgb.B))
            };
            Canvas.SetLeft(line, width / 2.0 - radius);
            Canvas.SetTop(line, 6 + i * 4);
            canvas.Children.Add(line);
        }

        if (WashingRunning)
        {
            for (var i = 0; i < 5; i++)
            {
                var t = (phase * 7 + i * .2) % 1.0;
                var strength = Math.Sin(Math.PI * t);
                var xx = 9 + i * Math.Max(1, width - 18) / 5.0;
                var yy = 2 + t * 21;
                var particle = new Rectangle
                {
                    Width = 2,
                    Height = 2,
                    Fill = new SolidColorBrush(Color.FromArgb((byte)Math.Clamp(Math.Round(strength * 230), 0, 255), 0xE1, 0xD3, 0xAD))
                };
                Canvas.SetLeft(particle, xx);
                Canvas.SetTop(particle, yy);
                canvas.Children.Add(particle);
            }
        }

        AddPhase6Rect(canvas, 0, 0, 2, height, "#527787");
        AddPhase6Rect(canvas, Math.Max(0, width - 2), 0, 2, height, "#527787");
        return canvas;
    }

    private FrameworkElement CreateWashingMeter(int width, int height)
    {
        var canvas = new Canvas { Width = width, Height = Math.Max(3, height), ClipToBounds = true };
        AddPhase6Rect(canvas, 0, 0, width, Math.Max(3, height), "#07141E");
        var filled = (int)Math.Round(Math.Clamp(CurrentProgress, 0, 1) * width);
        if (filled <= 0)
            return canvas;

        AddPhase6Rect(canvas, 0, 0, filled, Math.Max(3, height), WashingBlocked ? "#E4B47C" : "#9DDDF0");
        AddPhase6Rect(canvas, 0, Math.Max(0, height - 1), filled, 1, "#552A3B44");

        if (WashingRunning && _phase6PlayAnimations?.IsChecked == true)
        {
            var center = (int)Math.Round((_phase6AnimationClock % 2.0) / 2.0 * (filled + 10)) - 5;
            for (var i = -3; i <= 3; i++)
            {
                var xx = center + i;
                if (xx < 0 || xx >= filled)
                    continue;
                var alpha = (byte)((4 - Math.Abs(i)) * 24);
                var shine = new Rectangle
                {
                    Width = 1,
                    Height = Math.Max(3, height),
                    Fill = new SolidColorBrush(Color.FromArgb(alpha, 0xE8, 0xFF, 0xF3))
                };
                Canvas.SetLeft(shine, xx);
                canvas.Children.Add(shine);
            }
        }
        return canvas;
    }

    private void SetWashingPanel(Border border, string edge)
    {
        border.Background = Brush("#0A151F");
        border.BorderBrush = Brush(edge);
        border.BorderThickness = new Thickness(1);
        border.Child = new Border
        {
            Margin = new Thickness(2),
            Background = new LinearGradientBrush(Brush("#1B2C39").Color, Brush("#12212D").Color, 90),
            BorderBrush = Brush("#637884"),
            BorderThickness = new Thickness(0, 1, 0, 0)
        };
    }

    private static void SetWashingText(Border border, string text, string color, double size, HorizontalAlignment alignment)
    {
        border.Background = Brushes.Transparent;
        border.BorderBrush = Brushes.Transparent;
        border.BorderThickness = new Thickness(0);
        border.Child = new TextBlock
        {
            Text = text,
            Foreground = Brush(color),
            FontFamily = new FontFamily("Consolas"),
            FontSize = size,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
    }

    private bool HasWashingOverlay()
        => PreviewCanvas.Children.OfType<FrameworkElement>()
            .Any(child => string.Equals(child.Tag as string, Phase6WashingOverlayTag, StringComparison.Ordinal));

    private void EnsureWashingOverlay()
    {
        RemoveWashingOverlay();
        if (!IsWashingPhase6Active || _foreverPreviewToggle?.IsChecked == false)
            return;

        var overlay = new Canvas
        {
            Tag = Phase6WashingOverlayTag,
            Width = _layout.Width,
            Height = _layout.Height,
            IsHitTestVisible = false,
            Background = Brushes.Transparent
        };
        Panel.SetZIndex(overlay, -850);

        // Small static accents that are easy to miss in a geometry-only import.
        AddPhase6Rect(overlay, 14, 27, 52, 1, WashingHasWater ? "#9DDDF0" : "#FFBF76");
        AddPhase6Rect(overlay, 10, 33, 1, Math.Max(1, _layout.Height - 45), "#344A58");
        AddPhase6Rect(overlay, _layout.Width - 11, 33, 1, Math.Max(1, _layout.Height - 45), "#344A58");
        AddPhase6Rect(overlay, _layout.Width / 2 - 14, _layout.Height - 4, 28, 1, WashingHasWater ? "#9DDDF0" : "#FFBF76");

        PreviewCanvas.Children.Insert(0, overlay);
    }

    private void RemoveWashingOverlay()
    {
        var overlays = PreviewCanvas.Children.OfType<FrameworkElement>()
            .Where(child => string.Equals(child.Tag as string, Phase6WashingOverlayTag, StringComparison.Ordinal))
            .ToList();
        foreach (var overlay in overlays)
            PreviewCanvas.Children.Remove(overlay);
    }

    private static void AddPhase6Rect(Canvas canvas, double x, double y, double width, double height, string color)
    {
        if (width <= 0 || height <= 0)
            return;
        var rect = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = Brush(color),
            IsHitTestVisible = false
        };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        canvas.Children.Add(rect);
    }

    private void ApplyThemeSafeComboBoxes()
    {
        foreach (var combo in FindVisualChildren<ComboBox>(this))
        {
            if (string.Equals(combo.Tag as string, Phase6ComboTag, StringComparison.Ordinal))
                continue;

            combo.Tag = Phase6ComboTag;
            combo.Foreground = Brush("#111820");
            combo.Background = Brush("#F2F5F7");
            combo.BorderBrush = Brush("#87939D");
            combo.FontWeight = FontWeights.SemiBold;
            combo.Opacity = 1;
            combo.Padding = new Thickness(5, 2, 4, 2);

            void StyleItems()
            {
                foreach (var item in combo.Items.OfType<ComboBoxItem>())
                {
                    item.Foreground = Brush("#111820");
                    item.Background = Brush("#F2F5F7");
                    item.FontWeight = FontWeights.SemiBold;
                    item.Opacity = 1;
                }
            }

            combo.Loaded += (_, _) => StyleItems();
            combo.DropDownOpened += (_, _) => StyleItems();
            StyleItems();
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is null)
            yield break;

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                yield return match;
            foreach (var nested in FindVisualChildren<T>(child))
                yield return nested;
        }
    }
}
