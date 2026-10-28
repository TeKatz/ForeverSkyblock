using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using ForeverGuiDesigner.Models;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>Shared Style & Effects inspector used by standalone and Machine GUI contexts.</summary>
public partial class MainWindow
{
    private Border? _unifiedStyleEffectsInspector;
    private TextBox? _unifiedStyleTintBox;
    private TextBox? _unifiedStyleOpacityBox;
    private CheckBox? _unifiedHoverCheck;
    private TextBox? _unifiedHoverScaleBox;
    private CheckBox? _unifiedGlowCheck;
    private TextBox? _unifiedGlowColorBox;
    private CheckBox? _unifiedShadowCheck;
    private CheckBox? _unifiedPulseCheck;
    private TextBox? _unifiedPulseOpacityBox;
    private TextBox? _unifiedPulseDurationBox;
    private CheckBox? _unifiedPressedCheck;
    private TextBox? _unifiedPressedXBox;
    private TextBox? _unifiedPressedYBox;
    private CheckBox? _unifiedDisabledCheck;
    private TextBox? _unifiedDisabledOpacityBox;
    private CheckBox? _unifiedStateSwitchCheck;
    private ComboBox? _unifiedStateSwitchCombo;
    private bool _unifiedStyleEffectsRefreshing;
    private bool _unifiedStylePreviewInitialized;
    private bool _unifiedStylePreviewQueued;
    private FrameworkElement? _unifiedStylePreviewAnchor;
    private int _unifiedStylePreviewVisualCount = -1;

    public void EnableForeverUnifiedGuiStyleEffects()
    {
        EnsureUnifiedStyleEffectsInspector();
        ElementsList.SelectionChanged += (_, _) => RefreshUnifiedStyleEffectsInspector();
        if (!_unifiedStylePreviewInitialized)
        {
            _unifiedStylePreviewInitialized = true;
            CaptureUnifiedStylePreviewGeneration();
            PreviewCanvas.LayoutUpdated += UnifiedStylePreviewLayoutUpdated;
        }
        RefreshUnifiedStyleEffectsInspector();
    }

    private void EnsureUnifiedStyleEffectsInspector()
    {
        if (_unifiedStyleEffectsInspector is not null || SelectionText.Parent is not StackPanel inspector)
            return;

        var card = new Border
        {
            Background = Brush("#171D21"), BorderBrush = Brush("#3A454B"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 12),
            Visibility = Visibility.Collapsed
        };
        var panel = new StackPanel();
        panel.Children.Add(SectionText("STYLE & EFFECTS", "Shared by normal and Machine GUIs."));

        var styleGrid = PairGrid();
        _unifiedStyleTintBox = SmallBox("#80FFFFFF");
        _unifiedStyleOpacityBox = SmallBox("1");
        AddPair(styleGrid, "Tint", _unifiedStyleTintBox, "Opacity", _unifiedStyleOpacityBox);
        panel.Children.Add(styleGrid);

        (_unifiedHoverCheck, _unifiedHoverScaleBox) = AddToggleValue(panel, "Hover", "Scale", "1.05");
        (_unifiedGlowCheck, _unifiedGlowColorBox) = AddToggleValue(panel, "Glow", "Color", "#4EB8BC");
        _unifiedShadowCheck = AddToggle(panel, "Shadow");

        var pulseGrid = TripleGrid();
        _unifiedPulseCheck = new CheckBox { Content = "Pulse", VerticalAlignment = VerticalAlignment.Center };
        _unifiedPulseOpacityBox = SmallBox("0.65");
        _unifiedPulseDurationBox = SmallBox("900");
        pulseGrid.Children.Add(_unifiedPulseCheck);
        Grid.SetColumn(_unifiedPulseOpacityBox, 2); pulseGrid.Children.Add(_unifiedPulseOpacityBox);
        Grid.SetColumn(_unifiedPulseDurationBox, 4); pulseGrid.Children.Add(_unifiedPulseDurationBox);
        panel.Children.Add(LabelRow("", "Min opacity", "Duration ms"));
        panel.Children.Add(pulseGrid);

        var pressedGrid = TripleGrid();
        _unifiedPressedCheck = new CheckBox { Content = "Pressed Offset", VerticalAlignment = VerticalAlignment.Center };
        _unifiedPressedXBox = SmallBox("1");
        _unifiedPressedYBox = SmallBox("1");
        pressedGrid.Children.Add(_unifiedPressedCheck);
        Grid.SetColumn(_unifiedPressedXBox, 2); pressedGrid.Children.Add(_unifiedPressedXBox);
        Grid.SetColumn(_unifiedPressedYBox, 4); pressedGrid.Children.Add(_unifiedPressedYBox);
        panel.Children.Add(LabelRow("", "X", "Y"));
        panel.Children.Add(pressedGrid);

        (_unifiedDisabledCheck, _unifiedDisabledOpacityBox) = AddToggleValue(panel, "Disabled Appearance", "Opacity", "0.45");
        _unifiedStateSwitchCheck = AddToggle(panel, "State Switch");
        _unifiedStateSwitchCombo = new ComboBox { Height = 28, Margin = new Thickness(0, 3, 0, 8) };
        panel.Children.Add(_unifiedStateSwitchCombo);

        var actions = new Grid { Margin = new Thickness(0, 5, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        actions.ColumnDefinitions.Add(new ColumnDefinition());
        var apply = CreateStudioChromeButton("APPLY STYLE", double.NaN, true);
        apply.Height = 32; apply.Click += (_, _) => ApplyUnifiedStyleEffects(); actions.Children.Add(apply);
        var clear = CreateStudioChromeButton("CLEAR", double.NaN, false);
        clear.Height = 32; clear.Click += (_, _) => ClearUnifiedStyleEffects(); Grid.SetColumn(clear, 2); actions.Children.Add(clear);
        panel.Children.Add(actions);
        card.Child = panel;
        _unifiedStyleEffectsInspector = card;

        var applyIndex = inspector.Children.IndexOf(ApplyButton);
        inspector.Children.Insert(applyIndex >= 0 ? applyIndex + 1 : Math.Min(8, inspector.Children.Count), card);
    }

    private void RefreshUnifiedStyleEffectsInspector()
    {
        EnsureUnifiedStyleEffectsInspector();
        if (_unifiedStyleEffectsInspector is null)
            return;
        _unifiedStyleEffectsInspector.Visibility = _selection.Count == 1 ? Visibility.Visible : Visibility.Collapsed;
        if (_selection.Count != 1)
            return;

        try
        {
            var document = ReadUnifiedGuiDocumentFromEditor();
            var element = document.Elements.FirstOrDefault(candidate => candidate.Id.Equals(_selection.Single().Id, StringComparison.OrdinalIgnoreCase));
            if (element is null) return;
            var settings = ForeverGuiStyleEffectService.Read(document, element);
            _unifiedStyleEffectsRefreshing = true;
            _unifiedStyleTintBox!.Text = settings.Tint;
            _unifiedStyleOpacityBox!.Text = F(settings.Opacity);
            _unifiedHoverCheck!.IsChecked = settings.HoverEnabled;
            _unifiedHoverScaleBox!.Text = F(settings.HoverScale);
            _unifiedGlowCheck!.IsChecked = settings.GlowEnabled;
            _unifiedGlowColorBox!.Text = settings.GlowColor;
            _unifiedShadowCheck!.IsChecked = settings.ShadowEnabled;
            _unifiedPulseCheck!.IsChecked = settings.PulseEnabled;
            _unifiedPulseOpacityBox!.Text = F(settings.PulseMinOpacity);
            _unifiedPulseDurationBox!.Text = settings.PulseDurationMs.ToString(CultureInfo.InvariantCulture);
            _unifiedPressedCheck!.IsChecked = settings.PressedOffsetEnabled;
            _unifiedPressedXBox!.Text = F(settings.PressedOffsetX);
            _unifiedPressedYBox!.Text = F(settings.PressedOffsetY);
            _unifiedDisabledCheck!.IsChecked = settings.DisabledAppearanceEnabled;
            _unifiedDisabledOpacityBox!.Text = F(settings.DisabledOpacity);
            _unifiedStateSwitchCheck!.IsChecked = settings.StateSwitchEnabled;
            _unifiedStateSwitchCombo!.ItemsSource = document.States.Select(state => state.Id).ToList();
            _unifiedStateSwitchCombo.SelectedItem = settings.StateSwitchStateId;
            _unifiedStateSwitchCombo.IsEnabled = document.States.Count > 0;
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException)
        {
            StatusText.Text = "Style/effect metadata cannot be read safely: " + ex.Message;
        }
        finally { _unifiedStyleEffectsRefreshing = false; }
    }

    private void ApplyUnifiedStyleEffects()
    {
        if (_unifiedStyleEffectsRefreshing || _selection.Count != 1) return;
        try
        {
            var document = ReadUnifiedGuiDocumentFromEditor();
            var selectedId = _selection.Single().Id;
            var element = document.Elements.First(candidate => candidate.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase));
            var settings = new ForeverGuiElementStyleSettings(
                _unifiedStyleTintBox!.Text.Trim(), D(_unifiedStyleOpacityBox.Text, "Opacity"),
                _unifiedHoverCheck!.IsChecked == true, D(_unifiedHoverScaleBox!.Text, "Hover scale"),
                _unifiedGlowCheck!.IsChecked == true, _unifiedGlowColorBox!.Text.Trim(),
                _unifiedShadowCheck!.IsChecked == true,
                _unifiedPulseCheck!.IsChecked == true, D(_unifiedPulseOpacityBox!.Text, "Pulse opacity"), I(_unifiedPulseDurationBox!.Text, "Pulse duration"),
                _unifiedPressedCheck!.IsChecked == true, D(_unifiedPressedXBox!.Text, "Pressed X"), D(_unifiedPressedYBox!.Text, "Pressed Y"),
                _unifiedDisabledCheck!.IsChecked == true, D(_unifiedDisabledOpacityBox!.Text, "Disabled opacity"),
                _unifiedStateSwitchCheck!.IsChecked == true, _unifiedStateSwitchCombo!.SelectedItem?.ToString() ?? string.Empty);
            ForeverGuiStyleEffectService.Apply(document, element, settings);
            ReplaceUnifiedEditorDocument(document, selectedId);
            StatusText.Text = $"Applied shared style/effects to '{selectedId}'.";
        }
        catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or JsonException)
        { MessageBox.Show(this, ex.Message, "Style/effect validation failed", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ClearUnifiedStyleEffects()
    {
        if (_selection.Count != 1) return;
        try
        {
            var document = ReadUnifiedGuiDocumentFromEditor();
            var selectedId = _selection.Single().Id;
            var element = document.Elements.First(candidate => candidate.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase));
            ForeverGuiStyleEffectService.Clear(document, element);
            ReplaceUnifiedEditorDocument(document, selectedId);
            StatusText.Text = $"Cleared shared style/effects from '{selectedId}'.";
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException)
        { MessageBox.Show(this, ex.Message, "Style/effect clear failed", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void ReplaceUnifiedEditorDocument(ForeverGuiDocument document, string selectedId)
    {
        PushUndo();
        LoadLayoutIntoEditor(ProjectUnifiedGuiDocumentToEditor(document), _currentPath, clearHistory: false);
        var selected = _layout.Elements.FirstOrDefault(candidate => candidate.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase));
        if (selected is not null) SetSelection([selected]);
        MarkDirty();
        RefreshUnifiedStyleEffectsInspector();
    }

    private void UnifiedStylePreviewLayoutUpdated(object? sender, EventArgs e)
    {
        var anchor = _visuals.Count == 0 ? null : _visuals.Values.First();
        if (_visuals.Count == _unifiedStylePreviewVisualCount && ReferenceEquals(anchor, _unifiedStylePreviewAnchor)) return;
        CaptureUnifiedStylePreviewGeneration();
        if (_unifiedStylePreviewQueued) return;
        _unifiedStylePreviewQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            _unifiedStylePreviewQueued = false;
            if (!IsLoaded) return;
            RefreshUnifiedStyleEffectVisuals();
            CaptureUnifiedStylePreviewGeneration();
        }));
    }

    private void CaptureUnifiedStylePreviewGeneration()
    {
        _unifiedStylePreviewVisualCount = _visuals.Count;
        _unifiedStylePreviewAnchor = _visuals.Count == 0 ? null : _visuals.Values.First();
    }

    private void RefreshUnifiedStyleEffectVisuals()
    {
        if (_visuals.Count == 0) return;
        ForeverGuiDocument document;
        try { document = ReadUnifiedGuiDocumentFromEditor(); }
        catch (Exception ex) when (ex is InvalidDataException or JsonException) { return; }
        var elements = document.Elements.ToDictionary(element => element.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in _visuals)
        {
            if (pair.Value is not Border border || !elements.TryGetValue(pair.Key.Id, out var element)) continue;
            var settings = ForeverGuiStyleEffectService.Read(document, element);
            border.Opacity = settings.Opacity;
            if (!string.IsNullOrWhiteSpace(settings.Tint) && border.Child is { } original)
            {
                var grid = new Grid();
                border.Child = null;
                grid.Children.Add(original);
                grid.Children.Add(new Border { Background = Brush(settings.Tint), Opacity = 0.25, IsHitTestVisible = false });
                border.Child = grid;
            }
            if (settings.GlowEnabled || settings.ShadowEnabled)
            {
                border.Effect = new DropShadowEffect
                {
                    Color = ParseColor(settings.GlowEnabled ? settings.GlowColor : "#000000"),
                    BlurRadius = settings.GlowEnabled ? 12 : 8,
                    ShadowDepth = settings.GlowEnabled ? 0 : 2,
                    Opacity = settings.GlowEnabled ? 0.8 : 0.55
                };
            }
            if (settings.HoverEnabled)
            {
                var scale = new ScaleTransform(1, 1);
                border.RenderTransformOrigin = new Point(0.5, 0.5);
                border.RenderTransform = scale;
                border.MouseEnter += (_, _) => scale.ScaleX = scale.ScaleY = settings.HoverScale;
                border.MouseLeave += (_, _) => scale.ScaleX = scale.ScaleY = 1;
            }
            if (settings.PulseEnabled)
            {
                border.BeginAnimation(OpacityProperty, new DoubleAnimation
                {
                    From = settings.Opacity,
                    To = Math.Min(settings.Opacity, settings.PulseMinOpacity),
                    Duration = TimeSpan.FromMilliseconds(settings.PulseDurationMs),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                });
            }
        }
    }

    private static TextBlock SectionText(string title, string subtitle) => new()
    { Text = title + "\n" + subtitle, Foreground = Brush("#72D5D3"), FontSize = 9.5, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
    private static TextBox SmallBox(string value) => new() { Text = value, Height = 28, Margin = new Thickness(0, 2, 0, 6) };
    private static Grid PairGrid() { var g = new Grid(); g.ColumnDefinitions.Add(new ColumnDefinition()); g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) }); g.ColumnDefinitions.Add(new ColumnDefinition()); return g; }
    private static Grid TripleGrid() { var g = new Grid(); g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) }); g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) }); g.ColumnDefinitions.Add(new ColumnDefinition()); g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) }); g.ColumnDefinitions.Add(new ColumnDefinition()); return g; }
    private static Grid LabelRow(string a, string b, string c) { var g = TripleGrid(); var x = new TextBlock { Text = a, Foreground = Brush("#89969C"), FontSize = 8 }; var y = new TextBlock { Text = b, Foreground = Brush("#89969C"), FontSize = 8 }; var z = new TextBlock { Text = c, Foreground = Brush("#89969C"), FontSize = 8 }; g.Children.Add(x); Grid.SetColumn(y, 2); g.Children.Add(y); Grid.SetColumn(z, 4); g.Children.Add(z); return g; }
    private static void AddPair(Grid grid, string l1, TextBox a, string l2, TextBox b) { var p1 = new StackPanel(); p1.Children.Add(new TextBlock { Text = l1, Foreground = Brush("#AEB4BA"), FontSize = 9 }); p1.Children.Add(a); grid.Children.Add(p1); var p2 = new StackPanel(); p2.Children.Add(new TextBlock { Text = l2, Foreground = Brush("#AEB4BA"), FontSize = 9 }); p2.Children.Add(b); Grid.SetColumn(p2, 2); grid.Children.Add(p2); }
    private static (CheckBox Toggle, TextBox Value) AddToggleValue(StackPanel panel, string title, string label, string value) { var g = PairGrid(); var left = new StackPanel(); var cb = new CheckBox { Content = title }; left.Children.Add(cb); g.Children.Add(left); var right = new StackPanel(); right.Children.Add(new TextBlock { Text = label, Foreground = Brush("#89969C"), FontSize = 8 }); var box = SmallBox(value); right.Children.Add(box); Grid.SetColumn(right, 2); g.Children.Add(right); panel.Children.Add(g); return (cb, box); }
    private static CheckBox AddToggle(StackPanel panel, string title) { var cb = new CheckBox { Content = title, Margin = new Thickness(0, 2, 0, 6) }; panel.Children.Add(cb); return cb; }
    private static double D(string text, string name) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) ? value : throw new InvalidDataException(name + " must be a finite number.");
    private static int I(string text, string name) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : throw new InvalidDataException(name + " must be an integer.");
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    private static Color ParseColor(string value) { try { return (Color)ColorConverter.ConvertFromString(value)!; } catch { return Colors.Black; } }
}
