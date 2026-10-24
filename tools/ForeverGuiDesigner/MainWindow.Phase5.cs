using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using ForeverGuiDesigner.Models;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private const string Phase5OverlayTag = "__forever_phase5_state_overlay";

    private ComboBox? _foreverTierCombo;
    private ComboBox? _foreverModeCombo;
    private ComboBox? _foreverProgressCombo;
    private bool _phase5ToolsInitialized;
    private bool _phase5RefreshQueued;
    private bool _phase5Refreshing;
    private bool _phase5AutoAugmentRequested;

    public void EnableForeverPhase5Tools()
    {
        if (_phase5ToolsInitialized)
            return;

        _phase5ToolsInitialized = true;

        if (SkyblockBarHost.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content as string, "Load machine", StringComparison.Ordinal)) is { } loadButton)
        {
            loadButton.PreviewMouseLeftButtonDown += (_, _) => _phase5AutoAugmentRequested = true;
        }

        SkyblockBarHost.Children.Add(SeparatorBar());
        SkyblockBarHost.Children.Add(SectionLabel("STATE"));

        _foreverTierCombo = new ComboBox
        {
            Width = 78,
            ToolTip = "Preview the Condenser as Mark I or Mark II."
        };
        _foreverTierCombo.Items.Add(ComboItem("MK I", "mk1"));
        _foreverTierCombo.Items.Add(ComboItem("MK II", "mk2"));
        _foreverTierCombo.SelectedIndex = 0;
        _foreverTierCombo.SelectionChanged += Phase5StateChanged;
        SkyblockBarHost.Children.Add(_foreverTierCombo);

        _foreverModeCombo = new ComboBox
        {
            Width = 92,
            ToolTip = "Preview the active Condenser module/chamber state."
        };
        _foreverModeCombo.Items.Add(ComboItem("Base", "base"));
        _foreverModeCombo.Items.Add(ComboItem("Mixing", "mixing"));
        _foreverModeCombo.Items.Add(ComboItem("Freezing", "freezing"));
        _foreverModeCombo.Items.Add(ComboItem("Garden", "garden"));
        _foreverModeCombo.SelectedIndex = 0;
        _foreverModeCombo.SelectionChanged += Phase5StateChanged;
        SkyblockBarHost.Children.Add(_foreverModeCombo);

        SkyblockBarHost.Children.Add(new TextBlock
        {
            Text = "Progress",
            Foreground = Brush("#AEBBC4"),
            Margin = new Thickness(8, 0, 3, 0),
            VerticalAlignment = VerticalAlignment.Center
        });

        _foreverProgressCombo = new ComboBox
        {
            Width = 72,
            ToolTip = "Preview fill amount for Forever progress meters."
        };
        foreach (var value in new[] { 0, 25, 50, 75, 100 })
            _foreverProgressCombo.Items.Add(ComboItem($"{value}%", value.ToString()));
        _foreverProgressCombo.SelectedIndex = 2;
        _foreverProgressCombo.SelectionChanged += Phase5StateChanged;
        SkyblockBarHost.Children.Add(_foreverProgressCombo);

        SkyblockBarHost.Children.Add(SeparatorBar());
        SkyblockBarHost.Children.Add(SectionLabel("WIDGETS"));
        SkyblockBarHost.Children.Add(WidgetButton("Panel", "panel", "Add an editable Forever MachineSkin-style panel."));
        SkyblockBarHost.Children.Add(WidgetButton("Meter", "meter", "Add an editable Forever progress meter."));
        SkyblockBarHost.Children.Add(WidgetButton("Lock", "lock", "Add an editable Forever lock indicator."));
        SkyblockBarHost.Children.Add(WidgetButton("Status", "status", "Add an editable Forever status indicator."));

        if (_foreverTemplateCombo is not null)
            _foreverTemplateCombo.SelectionChanged += (_, _) => QueuePhase5Refresh();

        PreviewCanvas.LayoutUpdated += (_, _) => QueuePhase5Refresh();
        QueuePhase5Refresh();
    }

    private static Border SeparatorBar()
        => new()
        {
            Width = 1,
            Height = 24,
            Background = Brush("#3B454E"),
            Margin = new Thickness(7, 0, 7, 0)
        };

    private static TextBlock SectionLabel(string text)
        => new()
        {
            Text = text,
            Foreground = Brush("#7F929F"),
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

    private static ComboBoxItem ComboItem(string text, string tag)
        => new() { Content = text, Tag = tag };

    private Button WidgetButton(string text, string kind, string tooltip)
    {
        var button = new Button { Content = text, Tag = kind, ToolTip = tooltip };
        button.Click += AddForeverWidget_Click;
        return button;
    }

    private void Phase5StateChanged(object sender, SelectionChangedEventArgs e)
        => QueuePhase5Refresh();

    private string CurrentTier
        => _foreverTierCombo?.SelectedItem is ComboBoxItem { Tag: string tag } ? tag : "mk1";

    private string CurrentMode
        => _foreverModeCombo?.SelectedItem is ComboBoxItem { Tag: string tag } ? tag : "base";

    private double CurrentProgress
    {
        get
        {
            if (_foreverProgressCombo?.SelectedItem is ComboBoxItem { Tag: string tag } &&
                double.TryParse(tag, out var percent))
                return Math.Clamp(percent / 100.0, 0, 1);
            return .5;
        }
    }

    private bool IsCondenserPhase5Active
        => _activeForeverTemplate is { ScreenClass: "CondenserScreen" } template &&
           string.Equals(_layout.Name, template.DisplayName, StringComparison.Ordinal) &&
           _layout.Width == template.Layout.Width &&
           _layout.Height == template.Layout.Height;

    private void QueuePhase5Refresh()
    {
        if (_phase5RefreshQueued || _phase5Refreshing)
            return;

        _phase5RefreshQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(RefreshPhase5Presentation));
    }

    private void RefreshPhase5Presentation()
    {
        _phase5RefreshQueued = false;
        if (_phase5Refreshing)
            return;

        _phase5Refreshing = true;
        try
        {
            UpdatePhase5ControlAvailability();

            if (_phase5AutoAugmentRequested)
            {
                _phase5AutoAugmentRequested = false;
                if (IsCondenserPhase5Active && EnsureCondenserPhase5Elements())
                {
                    RefreshElementList();
                    RenderLayout();
                    return;
                }
            }

            DecorateForeverWidgets();
            ApplyCondenserStateVisibility();
            EnsureCondenserStateOverlay();
        }
        finally
        {
            _phase5Refreshing = false;
        }
    }

    private void UpdatePhase5ControlAvailability()
    {
        var enabled = IsCondenserPhase5Active;
        if (_foreverTierCombo is not null) _foreverTierCombo.IsEnabled = enabled;
        if (_foreverModeCombo is not null) _foreverModeCombo.IsEnabled = enabled;
        if (_foreverProgressCombo is not null) _foreverProgressCombo.IsEnabled = enabled || _layout.Elements.Any(IsForeverMeter);
    }

    private void AddForeverWidget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string kind })
            return;

        var (width, height, content) = kind switch
        {
            "panel" => (120, 30, "Forever panel"),
            "meter" => (96, 7, "Progress"),
            "lock" => (8, 10, "Lock"),
            "status" => (6, 6, "Status"),
            _ => (32, 16, "Forever widget")
        };

        PushUndo();
        var element = new GuiElementModel
        {
            Id = CreateUniqueId($"forever_widget_{kind}"),
            Type = GuiElementType.Texture,
            X = Math.Max(0, (_layout.Width - width) / 2),
            Y = Math.Max(0, (_layout.Height - height) / 2),
            Width = Math.Min(width, _layout.Width),
            Height = Math.Min(height, _layout.Height),
            Content = content
        };
        _layout.Elements.Add(element);
        SetSelection([element]);
        RefreshElementList();
        RenderLayout();
        MarkDirty();
        QueuePhase5Refresh();
    }

    private bool EnsureCondenserPhase5Elements()
    {
        if (_layout.Elements.Any(element => element.Id.StartsWith("forever_phase5_", StringComparison.OrdinalIgnoreCase)))
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

        Add("forever_phase5_common_status", GuiElementType.Texture, 177, 12, 4, 4, "Machine status");
        Add("forever_phase5_common_panel_automation", GuiElementType.Texture, 18, 203, 212, 51, "Automation / upgrades");

        Add("forever_phase5_non_garden_panel_condensing", GuiElementType.Texture, 14, 44, 220, 64, "Condensing");
        Add("forever_phase5_non_garden_panel_freezing_area", GuiElementType.Texture, 14, 113, 220, 43, "Freezing chamber");
        Add("forever_phase5_non_garden_panel_module_hint", GuiElementType.Texture, 18, 187, 212, 12, "Module hint");
        Add("forever_phase5_non_garden_label_input", GuiElementType.Text, 18, 48, 43, 9, "Input");
        Add("forever_phase5_non_garden_label_output", GuiElementType.Text, 189, 48, 41, 9, "Output");
        Add("forever_phase5_non_garden_lock_main", GuiElementType.Texture, 42, 52, 8, 10, "Input lock");

        Add("forever_phase5_base_meter_process", GuiElementType.Texture, 84, 88, 96, 3, "Process");

        Add("forever_phase5_mixing_label_module", GuiElementType.Text, 64, 48, 49, 9, "Module input");
        Add("forever_phase5_mixing_meter_process", GuiElementType.Texture, 112, 88, 68, 3, "Process");
        Add("forever_phase5_mixing_lock_module", GuiElementType.Texture, 92, 52, 8, 10, "Module lock");

        Add("forever_phase5_freezing_label_freezing", GuiElementType.Text, 29, 116, 190, 9, "Freezing");
        Add("forever_phase5_freezing_meter_process", GuiElementType.Texture, 84, 88, 96, 3, "Process");
        Add("forever_phase5_freezing_meter_freezing", GuiElementType.Texture, 62, 132, 124, 8, "Freezing progress");
        Add("forever_phase5_freezing_lock_freezing", GuiElementType.Texture, 42, 119, 8, 10, "Freezing lock");

        Add("forever_phase5_garden_panel_scene", GuiElementType.Texture, 14, 44, 220, 138, "Garden scene");
        Add("forever_phase5_garden_panel_routes", GuiElementType.Texture, 18, 183, 212, 16, "Garden routes");
        Add("forever_phase5_garden_meter_growth", GuiElementType.Texture, 66, 122, 116, 9, "Growth progress");
        Add("forever_phase5_garden_label_specimen", GuiElementType.Text, 7, 139, 62, 9, "Specimen");
        Add("forever_phase5_garden_label_harvest", GuiElementType.Text, 158, 139, 75, 9, "Harvest");

        Add("forever_phase5_mk2_label_upgrades", GuiElementType.Text, 22, 207, 96, 9, "UPGRADES 0 / 3");
        Add("forever_phase5_mk2_label_policies", GuiElementType.Text, 128, 207, 98, 9, "OUTPUT POLICIES");
        Add("forever_phase5_mk2_non_garden_policy_main", GuiElementType.Button, 126, 224, 49, 18, "Block");
        Add("forever_phase5_mk2_freezing_policy_freezing", GuiElementType.Button, 179, 224, 49, 18, "Block");
        Add("forever_phase5_mk2_garden_policy_primary", GuiElementType.Button, 126, 224, 49, 18, "Block");
        Add("forever_phase5_mk2_garden_policy_secondary", GuiElementType.Button, 179, 224, 49, 18, "Block");

        return true;
    }

    private void DecorateForeverWidgets()
    {
        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            if (pair.Value is not Border border)
                continue;

            var id = model.Id;
            if (IsForeverPanel(model))
            {
                border.Background = Brush(id.Contains("garden", StringComparison.OrdinalIgnoreCase) ? "#213E31" : "#172A37");
                border.BorderBrush = Brush(id.Contains("garden", StringComparison.OrdinalIgnoreCase) ? "#547A61" : "#566D7A");
                border.BorderThickness = new Thickness(1);
                border.Child = new Grid
                {
                    Children =
                    {
                        new Border { Margin = new Thickness(2), BorderBrush = Brush("#263D49"), BorderThickness = new Thickness(1) },
                        new TextBlock
                        {
                            Text = model.Content,
                            Foreground = Brush("#9FB3BE"),
                            FontSize = 6,
                            Margin = new Thickness(4, 2, 2, 2),
                            VerticalAlignment = VerticalAlignment.Top,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        }
                    }
                };
            }
            else if (IsForeverMeter(model))
            {
                border.Background = Brush("#07141E");
                border.BorderBrush = Brush("#4E6879");
                border.BorderThickness = new Thickness(1);
                var grid = new Grid();
                var fill = new Border
                {
                    Background = Brush(CurrentMode == "garden" ? "#62C46B" : CurrentMode == "freezing" ? "#9DDDF0" : "#76D8D2"),
                    Width = Math.Max(0, (model.Width - 2) * CurrentProgress),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(1)
                };
                grid.Children.Add(fill);
                border.Child = grid;
            }
            else if (IsForeverLock(model))
            {
                border.Background = Brushes.Transparent;
                border.BorderBrush = Brushes.Transparent;
                border.Child = CreateLockVisual();
            }
            else if (IsForeverStatus(model))
            {
                border.Background = Brush(CurrentMode == "garden" ? "#76D982" : CurrentTier == "mk2" ? "#76D8D2" : "#E4B47C");
                border.BorderBrush = Brush("#DDE9EE");
                border.BorderThickness = new Thickness(.5);
                border.Child = null;
            }
            else if (id.StartsWith("forever_phase5_", StringComparison.OrdinalIgnoreCase) && model.Type == GuiElementType.Text)
            {
                border.Background = Brushes.Transparent;
                border.BorderBrush = Brushes.Transparent;
                border.Child = new TextBlock
                {
                    Text = model.Content,
                    Foreground = Brush(id.Contains("garden", StringComparison.OrdinalIgnoreCase) ? "#BFD6C4" : "#A0B4BE"),
                    FontSize = 6,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
            }
            else if (id.StartsWith("forever_phase5_", StringComparison.OrdinalIgnoreCase) && model.Type == GuiElementType.Button)
            {
                border.Background = Brush("#263B4B");
                border.BorderBrush = Brush("#6A818C");
                border.BorderThickness = new Thickness(1);
                border.Child = new TextBlock
                {
                    Text = model.Content,
                    Foreground = Brush("#E6EEE9"),
                    FontSize = 6,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }
            else if (id is "filters" or "recipes")
            {
                border.Background = Brush(id == "filters" ? "#2D4756" : "#263B4B");
                border.BorderBrush = Brush("#6A818C");
                if (border.Child is TextBlock text)
                    text.Foreground = Brush("#E6EEE9");
            }
            else if (id == "screen_title" && border.Child is TextBlock title)
            {
                title.Text = CurrentTier == "mk2" && IsCondenserPhase5Active ? "Condenser Mark II" : model.Content;
                title.Foreground = Brush(CurrentTier == "mk2" ? "#E6EEE9" : "#E4B47C");
            }
        }
    }

    private static bool IsForeverPanel(GuiElementModel model)
        => model.Id.Contains("_panel_", StringComparison.OrdinalIgnoreCase) ||
           model.Id.StartsWith("forever_widget_panel", StringComparison.OrdinalIgnoreCase);

    private static bool IsForeverMeter(GuiElementModel model)
        => model.Id.Contains("_meter_", StringComparison.OrdinalIgnoreCase) ||
           model.Id.StartsWith("forever_widget_meter", StringComparison.OrdinalIgnoreCase);

    private static bool IsForeverLock(GuiElementModel model)
        => model.Id.Contains("_lock_", StringComparison.OrdinalIgnoreCase) ||
           model.Id.StartsWith("forever_widget_lock", StringComparison.OrdinalIgnoreCase);

    private static bool IsForeverStatus(GuiElementModel model)
        => model.Id.Contains("_status", StringComparison.OrdinalIgnoreCase) ||
           model.Id.StartsWith("forever_widget_status", StringComparison.OrdinalIgnoreCase);

    private static FrameworkElement CreateLockVisual()
    {
        var canvas = new Canvas { Width = 8, Height = 10 };
        AddRect(canvas, 2, 0, 4, 1, "#E6EEE9");
        AddRect(canvas, 1, 1, 1, 3, "#E6EEE9");
        AddRect(canvas, 6, 1, 1, 3, "#E6EEE9");
        AddRect(canvas, 0, 4, 8, 6, "#76D8D2");
        AddRect(canvas, 3, 6, 2, 3, "#152331");
        return new Viewbox { Stretch = Stretch.Fill, Child = canvas };
    }

    private void ApplyCondenserStateVisibility()
    {
        if (!IsCondenserPhase5Active)
            return;

        foreach (var pair in _visuals)
        {
            var model = pair.Key;
            var visual = pair.Value;
            var visible = true;

            if (model.Id.StartsWith("forever_phase5_", StringComparison.OrdinalIgnoreCase))
                visible = Phase5ElementVisible(model.Id);
            else if (model.Id.StartsWith("machine_slot_", StringComparison.OrdinalIgnoreCase))
                visible = MachineSlotVisible(model.Content);
            else if (model.Id == "filters")
                visible = CurrentMode != "garden";

            visual.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            if (model.Id == "recipes" && visible)
            {
                Canvas.SetLeft(visual, CurrentMode == "garden" ? 94 : 168);
                Canvas.SetTop(visual, CurrentMode == "garden" ? 151 : 162);
            }
        }
    }

    private bool Phase5ElementVisible(string id)
    {
        if (id.Contains("_mk2_", StringComparison.OrdinalIgnoreCase) && CurrentTier != "mk2")
            return false;
        if (id.Contains("_non_garden_", StringComparison.OrdinalIgnoreCase) && CurrentMode == "garden")
            return false;
        if (id.Contains("_garden_", StringComparison.OrdinalIgnoreCase) && CurrentMode != "garden")
            return false;
        if (id.Contains("_base_", StringComparison.OrdinalIgnoreCase) && CurrentMode != "base")
            return false;
        if (id.Contains("_mixing_", StringComparison.OrdinalIgnoreCase) && CurrentMode != "mixing")
            return false;
        if (id.Contains("_freezing_", StringComparison.OrdinalIgnoreCase) && CurrentMode != "freezing")
            return false;
        return true;
    }

    private bool MachineSlotVisible(string content)
    {
        var parts = content.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var x) || !int.TryParse(parts[1], out var y))
            return true;

        return (x, y) switch
        {
            (30, 61) or (202, 61) => CurrentMode != "garden",
            (80, 61) => CurrentMode == "mixing",
            (30, 128) or (202, 128) => CurrentMode == "freezing",
            (30, 151) or (174, 151) or (202, 151) => CurrentMode == "garden",
            (30, 224) or (58, 224) or (86, 224) => CurrentTier == "mk2",
            _ => true
        };
    }

    private void EnsureCondenserStateOverlay()
    {
        RemovePhase5Overlay();
        if (!IsCondenserPhase5Active || _foreverPreviewToggle?.IsChecked == false)
            return;

        var canvas = new Canvas
        {
            Tag = Phase5OverlayTag,
            Width = _layout.Width,
            Height = _layout.Height,
            IsHitTestVisible = false,
            Background = Brush("#15232F")
        };
        Panel.SetZIndex(canvas, -900);

        var accent = CurrentMode == "garden" ? "#9BD4A6" : CurrentTier == "mk2" ? "#76D8D2" : "#E4B47C";
        DrawFrame(canvas, accent);

        AddPanel(canvas, 18, 254, 212, 92, "#405866");
        AddRect(canvas, 24, 318, 200, 1, "#344C5B");

        if (CurrentMode == "garden")
        {
            AddPanel(canvas, 42, 31, 164, 12, "#315B45");
            AddPanel(canvas, 14, 44, 220, 138, "#335544");
            AddPanel(canvas, 18, 183, 212, 16, "#466252");
            AddPanel(canvas, 18, 203, 212, 51, CurrentTier == "mk2" ? "#4B5E6E" : "#3E454C");
            AddOverlayText(canvas, "GARDEN CONNECTED", 42, 33, 164, "#8FE59B", 6.5);
            AddOverlayText(canvas, "Garden chamber", 22, 72, 204, "#BFD6C4", 8);
            AddOverlayText(canvas, "routes / harvest", 22, 187, 204, "#B4D1BC", 6);
        }
        else
        {
            AddPanel(canvas, 42, 31, 164, 12, "#365A66");
            AddPanel(canvas, 14, 44, 220, 64, "#54616D");
            AddPanel(canvas, 14, 113, 220, 43, "#465A68");
            AddPanel(canvas, 18, 187, 212, 12, "#5D6875");
            AddPanel(canvas, 18, 203, 212, 51, CurrentTier == "mk2" ? "#4B5E6E" : "#3E454C");
            AddOverlayText(canvas, CurrentMode switch
            {
                "mixing" => "MIXING CHAMBER",
                "freezing" => "FREEZING CHAMBER",
                _ => "BASE CONDENSER"
            }, 42, 33, 164, "#76D8D2", 6.5);
        }

        AddOverlayText(canvas, CurrentTier == "mk2" ? "CONDENSER MARK II" : "CONDENSER", 42, 10, 126, "#E6EEE9", 8);
        AddOverlayText(canvas, CurrentMode == "garden" ? "READY" : "IDLE", 184, 10, 48, accent, 6);
        TryAddCondenserIcon(canvas);

        PreviewCanvas.Children.Insert(0, canvas);
    }

    private void RemovePhase5Overlay()
    {
        var existing = PreviewCanvas.Children.OfType<FrameworkElement>()
            .FirstOrDefault(child => string.Equals(child.Tag as string, Phase5OverlayTag, StringComparison.Ordinal));
        if (existing is not null)
            PreviewCanvas.Children.Remove(existing);
    }

    private void DrawFrame(Canvas canvas, string accent)
    {
        AddRect(canvas, 0, 0, _layout.Width, _layout.Height, "#0C151F");
        AddRect(canvas, 3, 4, _layout.Width - 6, _layout.Height - 8, "#253746");
        AddRect(canvas, 8, 7, _layout.Width - 16, 20, "#101D29");
        AddRect(canvas, 14, 27, _layout.Width - 28, 1, "#526979");
        AddRect(canvas, 14, 27, 52, 1, accent);
        AddRect(canvas, 10, 33, 1, _layout.Height - 45, "#344A58");
        AddRect(canvas, _layout.Width - 11, 33, 1, _layout.Height - 45, "#344A58");
        AddRect(canvas, _layout.Width / 2 - 14, _layout.Height - 4, 28, 1, accent);
    }

    private static void AddPanel(Canvas canvas, int x, int y, int width, int height, string edge)
    {
        AddRect(canvas, x, y, width, height, "#0A151F");
        AddRect(canvas, x + 1, y + 1, width - 2, height - 2, edge);
        AddRect(canvas, x + 2, y + 2, width - 4, height - 4, "#182A37");
        AddRect(canvas, x + 3, y + 1, width - 6, 1, "#637884");
    }

    private static void AddRect(Canvas canvas, double x, double y, double width, double height, string color)
    {
        if (width <= 0 || height <= 0)
            return;
        var rect = new Rectangle { Width = width, Height = height, Fill = Brush(color), SnapsToDevicePixels = true };
        Canvas.SetLeft(rect, x);
        Canvas.SetTop(rect, y);
        canvas.Children.Add(rect);
    }

    private static void AddOverlayText(Canvas canvas, string text, double x, double y, double width, string color, double fontSize)
    {
        var label = new TextBlock
        {
            Text = text,
            Width = width,
            Foreground = Brush(color),
            FontSize = fontSize,
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Canvas.SetLeft(label, x);
        Canvas.SetTop(label, y);
        canvas.Children.Add(label);
    }

    private void TryAddCondenserIcon(Canvas canvas)
    {
        var preferred = CurrentTier == "mk2" ? "condenser_mark_ii" : "condenser_mark_i";
        var asset = _allAssets.FirstOrDefault(item => string.Equals(item.DisplayName, preferred, StringComparison.OrdinalIgnoreCase))
                    ?? _allAssets.FirstOrDefault(item => item.DisplayName.Contains("condenser", StringComparison.OrdinalIgnoreCase));
        if (asset is null)
            return;

        try
        {
            var image = new Image
            {
                Source = LoadBitmap(asset.FullPath),
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            Canvas.SetLeft(image, 18);
            Canvas.SetTop(image, 10);
            canvas.Children.Add(image);
        }
        catch
        {
        }
    }
}
