using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForeverGuiDesigner.Models;
using Microsoft.Win32;

namespace ForeverGuiDesigner;

public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private GuiLayoutModel _layout = GuiLayoutModel.CreateExample();
    private GuiElementModel? _selectedElement;
    private FrameworkElement? _dragVisual;
    private Point _dragStart;
    private Point _dragElementStart;
    private bool _isDragging;
    private bool _dirty;
    private string? _currentPath;
    private int _snap = 1;

    public MainWindow()
    {
        InitializeComponent();
        LoadLayoutIntoEditor(_layout, null);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private void LoadLayoutIntoEditor(GuiLayoutModel layout, string? path)
    {
        _layout = layout;
        _currentPath = path;
        _selectedElement = null;
        _dirty = false;

        PreviewCanvas.Width = Math.Max(1, _layout.Width);
        PreviewCanvas.Height = Math.Max(1, _layout.Height);

        RefreshElementList();
        RenderLayout();
        UpdateInspector();
        UpdateStatus();
    }

    private void RefreshElementList()
    {
        ElementsList.ItemsSource = null;
        ElementsList.ItemsSource = _layout.Elements;
        ElementsList.SelectedItem = _selectedElement;
    }

    private void RenderLayout()
    {
        PreviewCanvas.Children.Clear();
        PreviewCanvas.Width = Math.Max(1, _layout.Width);
        PreviewCanvas.Height = Math.Max(1, _layout.Height);

        foreach (var element in _layout.Elements.Where(element => element.Visible))
        {
            var visual = CreateElementVisual(element);
            visual.Tag = element;
            visual.MouseLeftButtonDown += Element_MouseLeftButtonDown;
            visual.MouseMove += Element_MouseMove;
            visual.MouseLeftButtonUp += Element_MouseLeftButtonUp;

            Canvas.SetLeft(visual, element.X);
            Canvas.SetTop(visual, element.Y);
            PreviewCanvas.Children.Add(visual);
        }

        UpdateSelectionVisuals();
    }

    private FrameworkElement CreateElementVisual(GuiElementModel element)
    {
        var border = new Border
        {
            Width = Math.Max(1, element.Width),
            Height = Math.Max(1, element.Height),
            SnapsToDevicePixels = true,
            Cursor = Cursors.SizeAll,
            BorderThickness = new Thickness(1)
        };

        switch (element.Type)
        {
            case GuiElementType.Slot:
                border.Background = ColorBrush("#8B8B8B");
                border.BorderBrush = ColorBrush("#373737");
                border.Child = new Border
                {
                    Margin = new Thickness(2),
                    Background = ColorBrush("#B7B7B7"),
                    BorderBrush = ColorBrush("#FFFFFF"),
                    BorderThickness = new Thickness(1, 1, 0, 0)
                };
                break;

            case GuiElementType.Text:
                border.Background = Brushes.Transparent;
                border.BorderBrush = Brushes.Transparent;
                border.Child = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(element.Content) ? element.Id : element.Content,
                    FontSize = 7,
                    Foreground = ColorBrush("#3F3F3F"),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                break;

            case GuiElementType.Button:
                border.Background = ColorBrush("#777777");
                border.BorderBrush = ColorBrush("#2C2C2C");
                border.Child = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(element.Content) ? element.Id : element.Content,
                    FontSize = 7,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                break;

            case GuiElementType.Texture:
                border.Background = ColorBrush("#676B70");
                border.BorderBrush = ColorBrush("#34373A");
                border.Child = new TextBlock
                {
                    Text = "texture",
                    FontSize = 6,
                    Foreground = ColorBrush("#E4E4E4"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                break;
        }

        return border;
    }

    private static SolidColorBrush ColorBrush(string color)
    {
        return (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
    }

    private void SelectElement(GuiElementModel? element)
    {
        _selectedElement = element;

        if (!ReferenceEquals(ElementsList.SelectedItem, element))
        {
            ElementsList.SelectedItem = element;
        }

        UpdateInspector();
        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        foreach (var child in PreviewCanvas.Children.OfType<Border>())
        {
            if (child.Tag is not GuiElementModel model)
            {
                continue;
            }

            var selected = ReferenceEquals(model, _selectedElement);
            if (selected)
            {
                child.BorderBrush = ColorBrush("#2E9BFF");
                child.BorderThickness = new Thickness(1.5);
            }
            else
            {
                child.BorderThickness = new Thickness(1);
                child.BorderBrush = model.Type switch
                {
                    GuiElementType.Text => Brushes.Transparent,
                    GuiElementType.Slot => ColorBrush("#373737"),
                    GuiElementType.Button => ColorBrush("#2C2C2C"),
                    _ => ColorBrush("#34373A")
                };
            }
        }
    }

    private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement visual || visual.Tag is not GuiElementModel element)
        {
            return;
        }

        e.Handled = true;
        SelectElement(element);

        _isDragging = true;
        _dragVisual = visual;
        _dragStart = e.GetPosition(PreviewCanvas);
        _dragElementStart = new Point(element.X, element.Y);
        visual.CaptureMouse();
    }

    private void Element_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || e.LeftButton != MouseButtonState.Pressed || !ReferenceEquals(sender, _dragVisual))
        {
            return;
        }

        if (_selectedElement is null || _dragVisual is null)
        {
            return;
        }

        var position = e.GetPosition(PreviewCanvas);
        var x = Snap(_dragElementStart.X + position.X - _dragStart.X);
        var y = Snap(_dragElementStart.Y + position.Y - _dragStart.Y);

        _selectedElement.X = Clamp((int)x, 0, Math.Max(0, _layout.Width - _selectedElement.Width));
        _selectedElement.Y = Clamp((int)y, 0, Math.Max(0, _layout.Height - _selectedElement.Height));

        Canvas.SetLeft(_dragVisual, _selectedElement.X);
        Canvas.SetTop(_dragVisual, _selectedElement.Y);
        UpdateInspector();
        MarkDirty();
    }

    private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        if (_dragVisual is not null)
        {
            _dragVisual.ReleaseMouseCapture();
        }
        _dragVisual = null;
        e.Handled = true;
    }

    private double Snap(double value)
    {
        return Math.Round(value / _snap, MidpointRounding.AwayFromZero) * _snap;
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }

    private void ElementsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ElementsList.SelectedItem is GuiElementModel element)
        {
            SelectElement(element);
        }
        else if (ElementsList.SelectedItem is null && !_isDragging)
        {
            SelectElement(null);
        }
    }

    private void PreviewCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, PreviewCanvas))
        {
            SelectElement(null);
        }
    }

    private void AddElement_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string typeName } ||
            !Enum.TryParse<GuiElementType>(typeName, out var type))
        {
            return;
        }

        var (width, height, content) = type switch
        {
            GuiElementType.Slot => (18, 18, string.Empty),
            GuiElementType.Text => (60, 10, "Text"),
            GuiElementType.Button => (60, 16, "Button"),
            GuiElementType.Texture => (32, 32, string.Empty),
            _ => (18, 18, string.Empty)
        };

        var element = new GuiElementModel
        {
            Id = CreateUniqueId(type.ToString().ToLowerInvariant()),
            Type = type,
            X = SnapToInt(Math.Max(0, (_layout.Width - width) / 2)),
            Y = SnapToInt(Math.Max(0, (_layout.Height - height) / 2)),
            Width = width,
            Height = height,
            Content = content
        };

        _layout.Elements.Add(element);
        _selectedElement = element;
        RefreshElementList();
        RenderLayout();
        UpdateInspector();
        MarkDirty();
    }

    private int SnapToInt(int value) => (int)Snap(value);

    private string CreateUniqueId(string prefix)
    {
        var index = 1;
        var candidate = prefix;
        var ids = _layout.Elements.Select(element => element.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        while (ids.Contains(candidate))
        {
            candidate = $"{prefix}{++index}";
        }

        return candidate;
    }

    private void UpdateInspector()
    {
        var hasSelection = _selectedElement is not null;
        TypeText.Text = hasSelection ? _selectedElement!.Type.ToString() : "No selection";
        IdBox.IsEnabled = hasSelection;
        XBox.IsEnabled = hasSelection;
        YBox.IsEnabled = hasSelection;
        WidthBox.IsEnabled = hasSelection;
        HeightBox.IsEnabled = hasSelection;
        ContentBox.IsEnabled = hasSelection;

        if (!hasSelection)
        {
            IdBox.Text = string.Empty;
            XBox.Text = string.Empty;
            YBox.Text = string.Empty;
            WidthBox.Text = string.Empty;
            HeightBox.Text = string.Empty;
            ContentBox.Text = string.Empty;
            return;
        }

        IdBox.Text = _selectedElement!.Id;
        XBox.Text = _selectedElement.X.ToString();
        YBox.Text = _selectedElement.Y.ToString();
        WidthBox.Text = _selectedElement.Width.ToString();
        HeightBox.Text = _selectedElement.Height.ToString();
        ContentBox.Text = _selectedElement.Content;
    }

    private void ApplyInspector_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedElement is null)
        {
            return;
        }

        if (!int.TryParse(XBox.Text, out var x) ||
            !int.TryParse(YBox.Text, out var y) ||
            !int.TryParse(WidthBox.Text, out var width) ||
            !int.TryParse(HeightBox.Text, out var height))
        {
            MessageBox.Show(this, "X, Y, Width and Height must be whole numbers.", "Invalid value", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        width = Math.Max(1, width);
        height = Math.Max(1, height);
        _selectedElement.Id = string.IsNullOrWhiteSpace(IdBox.Text) ? _selectedElement.Id : IdBox.Text.Trim();
        _selectedElement.Width = Math.Min(width, _layout.Width);
        _selectedElement.Height = Math.Min(height, _layout.Height);
        _selectedElement.X = Clamp(x, 0, Math.Max(0, _layout.Width - _selectedElement.Width));
        _selectedElement.Y = Clamp(y, 0, Math.Max(0, _layout.Height - _selectedElement.Height));
        _selectedElement.Content = ContentBox.Text;

        RefreshElementList();
        RenderLayout();
        UpdateInspector();
        MarkDirty();
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        LoadLayoutIntoEditor(new GuiLayoutModel(), null);
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Forever GUI layout (*.json)|*.json|All files (*.*)|*.*",
            Title = "Open GUI layout"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(dialog.FileName);
            var layout = JsonSerializer.Deserialize<GuiLayoutModel>(json, JsonOptions)
                         ?? throw new InvalidDataException("The layout file was empty.");
            layout.Elements ??= [];
            LoadLayoutIntoEditor(layout, dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open layout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPath is null)
        {
            SaveAs_Click(sender, e);
            return;
        }

        SaveTo(_currentPath);
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Forever GUI layout (*.json)|*.json",
            Title = "Save GUI layout",
            FileName = string.IsNullOrWhiteSpace(_layout.Name) ? "layout.json" : SanitizeFileName(_layout.Name) + ".json"
        };

        if (dialog.ShowDialog(this) == true)
        {
            SaveTo(dialog.FileName);
        }
    }

    private void SaveTo(string path)
    {
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(_layout, JsonOptions));
            _currentPath = path;
            _dirty = false;
            UpdateStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not save layout", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    }

    private void ZoomCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PreviewScale is null || ZoomCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string value || !double.TryParse(value, out var zoom))
        {
            return;
        }

        PreviewScale.ScaleX = zoom;
        PreviewScale.ScaleY = zoom;
    }

    private void SnapCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SnapCombo.SelectedItem is ComboBoxItem item && item.Tag is string value && int.TryParse(value, out var snap))
        {
            _snap = Math.Max(1, snap);
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.S)
        {
            Save_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (_selectedElement is null)
        {
            return;
        }

        if (e.Key == Key.Delete)
        {
            _layout.Elements.Remove(_selectedElement);
            _selectedElement = null;
            RefreshElementList();
            RenderLayout();
            UpdateInspector();
            MarkDirty();
            e.Handled = true;
            return;
        }

        var step = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? 5 : 1;
        var dx = 0;
        var dy = 0;

        switch (e.Key)
        {
            case Key.Left:
                dx = -step;
                break;
            case Key.Right:
                dx = step;
                break;
            case Key.Up:
                dy = -step;
                break;
            case Key.Down:
                dy = step;
                break;
            default:
                return;
        }

        _selectedElement.X = Clamp(_selectedElement.X + dx, 0, Math.Max(0, _layout.Width - _selectedElement.Width));
        _selectedElement.Y = Clamp(_selectedElement.Y + dy, 0, Math.Max(0, _layout.Height - _selectedElement.Height));
        RenderLayout();
        UpdateInspector();
        MarkDirty();
        e.Handled = true;
    }

    private void MarkDirty()
    {
        _dirty = true;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var file = _currentPath is null ? "unsaved" : Path.GetFileName(_currentPath);
        var dirty = _dirty ? "  • modified" : string.Empty;
        StatusText.Text = $"{_layout.Name}  |  {_layout.Width}×{_layout.Height}  |  {_layout.Elements.Count} elements  |  {file}{dirty}";
        Title = $"Forever GUI Designer — {_layout.Name}{(_dirty ? " *" : string.Empty)}";
    }
}
