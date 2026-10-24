using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ForeverGuiDesigner.Models;
using Microsoft.Win32;

namespace ForeverGuiDesigner;

public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private GuiLayoutModel _layout = GuiLayoutModel.CreateExample();
    private readonly HashSet<GuiElementModel> _selection = [];
    private readonly Dictionary<GuiElementModel, FrameworkElement> _visuals = [];
    private readonly Dictionary<GuiElementModel, Point> _dragOrigins = [];
    private readonly List<GuiElementModel> _clipboardElements = [];
    private readonly Stack<string> _undo = [];
    private readonly Stack<string> _redo = [];
    private readonly List<AssetItemModel> _allAssets = [];

    private FrameworkElement? _dragVisual;
    private Point _dragStart;
    private string? _dragUndoSnapshot;
    private bool _dragHistoryCaptured;
    private bool _isDragging;
    private bool _dirty;
    private bool _suppressSelectionChange;
    private string? _currentPath;
    private string? _repoRoot;
    private string? _assetRoot;
    private int _snap = 1;

    public MainWindow()
    {
        InitializeComponent();
        LoadLayoutIntoEditor(_layout, null, clearHistory: true);
        TryAutoDetectRepository();
        UpdateHistoryButtons();
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

    private void LoadLayoutIntoEditor(GuiLayoutModel layout, string? path, bool clearHistory)
    {
        _layout = layout;
        _layout.Elements ??= [];
        _currentPath = path;
        _selection.Clear();
        _dirty = false;

        if (clearHistory)
        {
            _undo.Clear();
            _redo.Clear();
        }

        RefreshElementList();
        RenderLayout();
        UpdateInspector();
        UpdateStatus();
        UpdateHistoryButtons();
    }

    private void RefreshElementList()
    {
        _suppressSelectionChange = true;
        try
        {
            ElementsList.ItemsSource = null;
            ElementsList.ItemsSource = _layout.Elements;
            ElementsList.SelectedItems.Clear();
            foreach (var element in _selection.Where(_layout.Elements.Contains))
            {
                ElementsList.SelectedItems.Add(element);
            }
        }
        finally
        {
            _suppressSelectionChange = false;
        }
    }

    private void RenderLayout()
    {
        PreviewCanvas.Children.Clear();
        _visuals.Clear();
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
            _visuals[element] = visual;
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
            BorderThickness = new Thickness(1),
            ClipToBounds = true
        };

        switch (element.Type)
        {
            case GuiElementType.Slot:
                border.Background = Brush("#8B8B8B");
                border.BorderBrush = Brush("#373737");
                border.Child = CreateSlotBody();
                break;

            case GuiElementType.Text:
                border.Background = Brushes.Transparent;
                border.BorderBrush = Brushes.Transparent;
                border.Child = Label(element, "#3F3F3F", 7, HorizontalAlignment.Left);
                break;

            case GuiElementType.Button:
                border.Background = Brush("#777777");
                border.BorderBrush = Brush("#2C2C2C");
                border.Child = Label(element, "#FFFFFF", 7, HorizontalAlignment.Center);
                break;

            case GuiElementType.Texture:
                border.Background = Brush("#676B70");
                border.BorderBrush = Brush("#34373A");
                border.Child = CreateTextureBody(element);
                break;

            case GuiElementType.PlayerInventory:
                border.Background = Brushes.Transparent;
                border.BorderBrush = Brushes.Transparent;
                border.Child = CreateSlotGrid(3, 9);
                break;

            case GuiElementType.Hotbar:
                border.Background = Brushes.Transparent;
                border.BorderBrush = Brushes.Transparent;
                border.Child = CreateSlotGrid(1, 9);
                break;
        }

        return border;
    }

    private static FrameworkElement CreateSlotBody()
    {
        return new Border
        {
            Margin = new Thickness(2),
            Background = Brush("#B7B7B7"),
            BorderBrush = Brushes.White,
            BorderThickness = new Thickness(1, 1, 0, 0)
        };
    }

    private static UniformGrid CreateSlotGrid(int rows, int columns)
    {
        var grid = new UniformGrid
        {
            Rows = rows,
            Columns = columns
        };

        for (var i = 0; i < rows * columns; i++)
        {
            grid.Children.Add(new Border
            {
                Width = 18,
                Height = 18,
                Background = Brush("#8B8B8B"),
                BorderBrush = Brush("#373737"),
                BorderThickness = new Thickness(1),
                Child = CreateSlotBody()
            });
        }

        return grid;
    }

    private FrameworkElement CreateTextureBody(GuiElementModel element)
    {
        var fullPath = ResolveTexturePath(element.TexturePath);
        if (fullPath is not null && File.Exists(fullPath))
        {
            try
            {
                var image = new Image
                {
                    Source = LoadBitmap(fullPath),
                    Stretch = Stretch.Fill,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
                return image;
            }
            catch
            {
            }
        }

        return new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(element.TexturePath) ? "texture" : "missing",
            FontSize = 6,
            Foreground = Brush("#E4E4E4"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static BitmapImage LoadBitmap(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private static TextBlock Label(GuiElementModel element, string color, double fontSize, HorizontalAlignment alignment)
    {
        return new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(element.Content) ? element.Id : element.Content,
            FontSize = fontSize,
            Foreground = Brush(color),
            HorizontalAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
    }

    private static SolidColorBrush Brush(string color)
        => (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;

    private void SetSelection(IEnumerable<GuiElementModel> elements, bool syncList = true)
    {
        _selection.Clear();
        foreach (var element in elements.Where(_layout.Elements.Contains))
        {
            _selection.Add(element);
        }

        if (syncList)
        {
            _suppressSelectionChange = true;
            try
            {
                ElementsList.SelectedItems.Clear();
                foreach (var element in _selection)
                {
                    ElementsList.SelectedItems.Add(element);
                }
            }
            finally
            {
                _suppressSelectionChange = false;
            }
        }

        UpdateInspector();
        UpdateSelectionVisuals();
        UpdateStatus();
    }

    private void ToggleSelection(GuiElementModel element)
    {
        var updated = _selection.ToList();
        if (_selection.Contains(element))
        {
            updated.Remove(element);
        }
        else
        {
            updated.Add(element);
        }
        SetSelection(updated);
    }

    private void UpdateSelectionVisuals()
    {
        foreach (var pair in _visuals)
        {
            if (pair.Value is not Border border)
            {
                continue;
            }

            var model = pair.Key;
            if (_selection.Contains(model))
            {
                border.BorderBrush = Brush("#2E9BFF");
                border.BorderThickness = new Thickness(1.5);
                continue;
            }

            border.BorderThickness = new Thickness(1);
            border.BorderBrush = model.Type switch
            {
                GuiElementType.Text or GuiElementType.PlayerInventory or GuiElementType.Hotbar => Brushes.Transparent,
                GuiElementType.Slot => Brush("#373737"),
                GuiElementType.Button => Brush("#2C2C2C"),
                _ => Brush("#34373A")
            };
        }
    }

    private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement visual || visual.Tag is not GuiElementModel element)
        {
            return;
        }

        e.Handled = true;
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            ToggleSelection(element);
            if (!_selection.Contains(element))
            {
                return;
            }
        }
        else if (!_selection.Contains(element))
        {
            SetSelection([element]);
        }

        _isDragging = true;
        _dragVisual = visual;
        _dragStart = e.GetPosition(PreviewCanvas);
        _dragOrigins.Clear();
        foreach (var selected in _selection)
        {
            _dragOrigins[selected] = new Point(selected.X, selected.Y);
        }
        _dragUndoSnapshot = SerializeLayout();
        _dragHistoryCaptured = false;
        visual.CaptureMouse();
    }

    private void Element_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || e.LeftButton != MouseButtonState.Pressed || !ReferenceEquals(sender, _dragVisual) || _selection.Count == 0)
        {
            return;
        }

        var position = e.GetPosition(PreviewCanvas);
        var dx = (int)Snap(position.X - _dragStart.X);
        var dy = (int)Snap(position.Y - _dragStart.Y);

        var minX = _dragOrigins.Min(pair => pair.Value.X);
        var minY = _dragOrigins.Min(pair => pair.Value.Y);
        var maxRight = _dragOrigins.Max(pair => pair.Value.X + pair.Key.Width);
        var maxBottom = _dragOrigins.Max(pair => pair.Value.Y + pair.Key.Height);
        dx = Clamp(dx, (int)-minX, (int)(_layout.Width - maxRight));
        dy = Clamp(dy, (int)-minY, (int)(_layout.Height - maxBottom));

        if (dx == 0 && dy == 0)
        {
            return;
        }

        if (!_dragHistoryCaptured && _dragUndoSnapshot is not null)
        {
            PushUndoSnapshot(_dragUndoSnapshot);
            _dragHistoryCaptured = true;
        }

        foreach (var pair in _dragOrigins)
        {
            pair.Key.X = (int)pair.Value.X + dx;
            pair.Key.Y = (int)pair.Value.Y + dy;
            if (_visuals.TryGetValue(pair.Key, out var visual))
            {
                Canvas.SetLeft(visual, pair.Key.X);
                Canvas.SetTop(visual, pair.Key.Y);
            }
        }

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
        _dragVisual?.ReleaseMouseCapture();
        _dragVisual = null;
        _dragOrigins.Clear();
        _dragUndoSnapshot = null;
        e.Handled = true;
    }

    private double Snap(double value)
        => Math.Round(value / _snap, MidpointRounding.AwayFromZero) * _snap;

    private static int Clamp(int value, int min, int max)
        => Math.Min(Math.Max(value, min), max);

    private void ElementsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChange)
        {
            return;
        }

        SetSelection(ElementsList.SelectedItems.Cast<GuiElementModel>(), syncList: false);
    }

    private void PreviewCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, PreviewCanvas))
        {
            SetSelection([]);
        }
    }

    private void AddElement_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string typeName } || !Enum.TryParse<GuiElementType>(typeName, out var type))
        {
            return;
        }

        PushUndo();
        var (width, height, content) = GetDefaultElementSize(type);
        var element = new GuiElementModel
        {
            Id = CreateUniqueId(type.ToString().ToLowerInvariant()),
            Type = type,
            X = (int)Snap(Math.Max(0, (_layout.Width - width) / 2)),
            Y = (int)Snap(Math.Max(0, (_layout.Height - height) / 2)),
            Width = Math.Min(width, _layout.Width),
            Height = Math.Min(height, _layout.Height),
            Content = content
        };

        _layout.Elements.Add(element);
        SetSelection([element]);
        RefreshElementList();
        RenderLayout();
        MarkDirty();
    }

    private static (int Width, int Height, string Content) GetDefaultElementSize(GuiElementType type)
    {
        return type switch
        {
            GuiElementType.Slot => (18, 18, string.Empty),
            GuiElementType.Text => (60, 10, "Text"),
            GuiElementType.Button => (60, 16, "Button"),
            GuiElementType.Texture => (32, 32, string.Empty),
            GuiElementType.PlayerInventory => (162, 54, string.Empty),
            GuiElementType.Hotbar => (162, 18, string.Empty),
            _ => (18, 18, string.Empty)
        };
    }

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
        SelectionText.Text = _selection.Count switch
        {
            0 => "Nothing selected",
            1 => "1 element selected",
            _ => $"{_selection.Count} elements selected"
        };

        var selected = _selection.Count == 1 ? _selection.First() : null;
        var enabled = selected is not null;
        TypeText.Text = selected?.Type.ToString() ?? (_selection.Count > 1 ? "Multiple" : "No selection");
        IdBox.IsEnabled = XBox.IsEnabled = YBox.IsEnabled = WidthBox.IsEnabled = HeightBox.IsEnabled = ContentBox.IsEnabled = ApplyButton.IsEnabled = enabled;

        if (!enabled)
        {
            IdBox.Text = XBox.Text = YBox.Text = WidthBox.Text = HeightBox.Text = ContentBox.Text = TexturePathBox.Text = string.Empty;
            return;
        }

        IdBox.Text = selected!.Id;
        XBox.Text = selected.X.ToString();
        YBox.Text = selected.Y.ToString();
        WidthBox.Text = selected.Width.ToString();
        HeightBox.Text = selected.Height.ToString();
        ContentBox.Text = selected.Content;
        TexturePathBox.Text = selected.TexturePath ?? string.Empty;
    }

    private void ApplyInspector_Click(object sender, RoutedEventArgs e)
    {
        if (_selection.Count != 1)
        {
            return;
        }

        var selected = _selection.First();
        if (!int.TryParse(XBox.Text, out var x) || !int.TryParse(YBox.Text, out var y) ||
            !int.TryParse(WidthBox.Text, out var width) || !int.TryParse(HeightBox.Text, out var height))
        {
            MessageBox.Show(this, "X, Y, Width and Height must be whole numbers.", "Invalid value", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        PushUndo();
        width = Math.Max(1, width);
        height = Math.Max(1, height);
        selected.Id = string.IsNullOrWhiteSpace(IdBox.Text) ? selected.Id : IdBox.Text.Trim();
        selected.Width = Math.Min(width, _layout.Width);
        selected.Height = Math.Min(height, _layout.Height);
        selected.X = Clamp(x, 0, Math.Max(0, _layout.Width - selected.Width));
        selected.Y = Clamp(y, 0, Math.Max(0, _layout.Height - selected.Height));
        selected.Content = ContentBox.Text;

        RefreshElementList();
        RenderLayout();
        UpdateInspector();
        MarkDirty();
    }

    private void Align_Click(object sender, RoutedEventArgs e)
    {
        if (_selection.Count < 2 || sender is not FrameworkElement { Tag: string mode })
        {
            return;
        }

        PushUndo();
        var minX = _selection.Min(item => item.X);
        var minY = _selection.Min(item => item.Y);
        var maxRight = _selection.Max(item => item.X + item.Width);
        var maxBottom = _selection.Max(item => item.Y + item.Height);
        var centerX = (minX + maxRight) / 2.0;
        var centerY = (minY + maxBottom) / 2.0;

        foreach (var item in _selection)
        {
            switch (mode)
            {
                case "Left": item.X = minX; break;
                case "Right": item.X = maxRight - item.Width; break;
                case "HCenter": item.X = Clamp((int)Math.Round(centerX - item.Width / 2.0), 0, _layout.Width - item.Width); break;
                case "Top": item.Y = minY; break;
                case "Bottom": item.Y = maxBottom - item.Height; break;
                case "VCenter": item.Y = Clamp((int)Math.Round(centerY - item.Height / 2.0), 0, _layout.Height - item.Height); break;
            }
        }

        RenderLayout();
        UpdateInspector();
        MarkDirty();
    }

    private void BringFront_Click(object sender, RoutedEventArgs e)
    {
        if (_selection.Count == 0)
        {
            return;
        }

        PushUndo();
        var selectedInOrder = _layout.Elements.Where(_selection.Contains).ToList();
        _layout.Elements.RemoveAll(_selection.Contains);
        _layout.Elements.AddRange(selectedInOrder);
        RefreshElementList();
        RenderLayout();
        MarkDirty();
    }

    private void SendBack_Click(object sender, RoutedEventArgs e)
    {
        if (_selection.Count == 0)
        {
            return;
        }

        PushUndo();
        var selectedInOrder = _layout.Elements.Where(_selection.Contains).ToList();
        _layout.Elements.RemoveAll(_selection.Contains);
        _layout.Elements.InsertRange(0, selectedInOrder);
        RefreshElementList();
        RenderLayout();
        MarkDirty();
    }

    private void New_Click(object sender, RoutedEventArgs e)
        => LoadLayoutIntoEditor(new GuiLayoutModel(), null, clearHistory: true);

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
            var layout = JsonSerializer.Deserialize<GuiLayoutModel>(File.ReadAllText(dialog.FileName), JsonOptions)
                         ?? throw new InvalidDataException("The layout file was empty.");
            layout.Elements ??= [];
            LoadLayoutIntoEditor(layout, dialog.FileName, clearHistory: true);
            TrySetRepository(FindRepoRoot(dialog.FileName) ?? _repoRoot, showError: false);
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
            FileName = (string.IsNullOrWhiteSpace(_layout.Name) ? "layout" : SanitizeFileName(_layout.Name)) + ".json"
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
            File.WriteAllText(path, SerializeLayout());
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

    private void ChooseRepo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose the ForeverSkyblock repository",
            InitialDirectory = _repoRoot ?? Environment.CurrentDirectory
        };

        if (dialog.ShowDialog(this) == true)
        {
            TrySetRepository(dialog.FolderName, showError: true);
        }
    }

    private void TryAutoDetectRepository()
    {
        var candidates = new[]
        {
            AppContext.BaseDirectory,
            Environment.CurrentDirectory,
            _currentPath
        };

        foreach (var candidate in candidates)
        {
            var root = FindRepoRoot(candidate);
            if (root is not null && TrySetRepository(root, showError: false))
            {
                return;
            }
        }

        RepoText.Text = "Repo: not selected";
    }

    private static string? FindRepoRoot(string? startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
        {
            return null;
        }

        var path = File.Exists(startPath) ? Path.GetDirectoryName(startPath) : startPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var directory = new DirectoryInfo(path);
        for (var i = 0; i < 12 && directory is not null; i++, directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "mod", "src", "main", "resources", "assets", "forever_skyblock")))
            {
                return directory.FullName;
            }
        }

        return null;
    }

    private bool TrySetRepository(string? root, bool showError)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return false;
        }

        var detected = FindRepoRoot(root) ?? root;
        var assetRoot = Path.Combine(detected, "mod", "src", "main", "resources", "assets", "forever_skyblock");
        if (!Directory.Exists(assetRoot))
        {
            if (showError)
            {
                MessageBox.Show(this,
                    "That folder does not look like the ForeverSkyblock repository. Expected mod/src/main/resources/assets/forever_skyblock.",
                    "Repository not found", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            return false;
        }

        _repoRoot = Path.GetFullPath(detected);
        _assetRoot = assetRoot;
        RepoText.Text = $"Repo: {Path.GetFileName(_repoRoot)}";
        RefreshAssets();
        RenderLayout();
        UpdateStatus();
        return true;
    }

    private void RefreshAssets()
    {
        _allAssets.Clear();
        if (_assetRoot is null)
        {
            ApplyAssetFilter();
            return;
        }

        var texturesRoot = Path.Combine(_assetRoot, "textures");
        if (!Directory.Exists(texturesRoot))
        {
            ApplyAssetFilter();
            return;
        }

        try
        {
            foreach (var path in Directory.EnumerateFiles(texturesRoot, "*.png", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                var relative = Path.GetRelativePath(_assetRoot, path).Replace('\\', '/');
                var underTextures = Path.GetRelativePath(texturesRoot, path).Replace('\\', '/');
                var category = underTextures.Split('/').FirstOrDefault() ?? "textures";
                _allAssets.Add(new AssetItemModel
                {
                    FullPath = path,
                    RelativePath = relative,
                    DisplayName = Path.GetFileNameWithoutExtension(path),
                    Category = category
                });
            }
        }
        catch (Exception ex)
        {
            AssetHintText.Text = $"Could not scan assets: {ex.Message}";
            return;
        }

        ApplyAssetFilter();
    }

    private void AssetFilterBox_TextChanged(object sender, TextChangedEventArgs e)
        => ApplyAssetFilter();

    private void ApplyAssetFilter()
    {
        if (AssetList is null || AssetHintText is null || AssetFilterBox is null)
        {
            return;
        }

        var filter = AssetFilterBox.Text.Trim();
        var visible = string.IsNullOrEmpty(filter)
            ? _allAssets
            : _allAssets.Where(asset =>
                asset.RelativePath.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                asset.DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                asset.Category.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        AssetList.ItemsSource = visible.ToList();
        AssetHintText.Text = _assetRoot is null
            ? "Choose the ForeverSkyblock repository to browse PNG assets."
            : $"{visible.Count()} of {_allAssets.Count} PNG textures";
    }

    private void AssetList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => UseSelectedAsset();

    private void UseAsset_Click(object sender, RoutedEventArgs e)
        => UseSelectedAsset();

    private void UseSelectedAsset()
    {
        if (AssetList.SelectedItem is not AssetItemModel asset)
        {
            return;
        }

        PushUndo();
        GuiElementModel target;
        if (_selection.Count == 1 && _selection.First().Type == GuiElementType.Texture)
        {
            target = _selection.First();
        }
        else
        {
            var (width, height) = GetImageSize(asset.FullPath);
            target = new GuiElementModel
            {
                Id = CreateUniqueId("texture"),
                Type = GuiElementType.Texture,
                Width = Math.Min(Math.Max(1, width), _layout.Width),
                Height = Math.Min(Math.Max(1, height), _layout.Height)
            };
            target.X = Math.Max(0, (_layout.Width - target.Width) / 2);
            target.Y = Math.Max(0, (_layout.Height - target.Height) / 2);
            _layout.Elements.Add(target);
        }

        target.TexturePath = asset.RelativePath;
        SetSelection([target]);
        RefreshElementList();
        RenderLayout();
        UpdateInspector();
        MarkDirty();
    }

    private static (int Width, int Height) GetImageSize(string path)
    {
        try
        {
            var bitmap = BitmapFrame.Create(new Uri(path, UriKind.Absolute), BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            return (bitmap.PixelWidth, bitmap.PixelHeight);
        }
        catch
        {
            return (32, 32);
        }
    }

    private string? ResolveTexturePath(string? texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return null;
        }
        if (Path.IsPathRooted(texturePath))
        {
            return texturePath;
        }
        return _assetRoot is null ? null : Path.Combine(_assetRoot, texturePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private void ZoomCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PreviewScale is null || ZoomCombo.SelectedItem is not ComboBoxItem { Tag: string value } || !double.TryParse(value, out var zoom))
        {
            return;
        }
        PreviewScale.ScaleX = PreviewScale.ScaleY = zoom;
    }

    private void SnapCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SnapCombo.SelectedItem is ComboBoxItem { Tag: string value } && int.TryParse(value, out var snap))
        {
            _snap = Math.Max(1, snap);
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        var control = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
        if (control && e.Key == Key.S)
        {
            Save_Click(sender, e);
            e.Handled = true;
            return;
        }

        if (Keyboard.FocusedElement is TextBox)
        {
            return;
        }

        if (control && e.Key == Key.Z)
        {
            Undo();
            e.Handled = true;
            return;
        }
        if (control && e.Key == Key.Y)
        {
            Redo();
            e.Handled = true;
            return;
        }
        if (control && e.Key == Key.C)
        {
            CopySelection();
            e.Handled = true;
            return;
        }
        if (control && e.Key == Key.V)
        {
            PasteSelection();
            e.Handled = true;
            return;
        }
        if (control && e.Key == Key.A)
        {
            SetSelection(_layout.Elements);
            e.Handled = true;
            return;
        }

        if (_selection.Count == 0)
        {
            return;
        }

        if (e.Key == Key.Delete)
        {
            DeleteSelection();
            e.Handled = true;
            return;
        }

        var step = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? 5 : 1;
        var (dx, dy) = e.Key switch
        {
            Key.Left => (-step, 0),
            Key.Right => (step, 0),
            Key.Up => (0, -step),
            Key.Down => (0, step),
            _ => (0, 0)
        };

        if (dx == 0 && dy == 0)
        {
            return;
        }

        MoveSelection(dx, dy);
        e.Handled = true;
    }

    private void MoveSelection(int dx, int dy)
    {
        var minX = _selection.Min(item => item.X);
        var minY = _selection.Min(item => item.Y);
        var maxRight = _selection.Max(item => item.X + item.Width);
        var maxBottom = _selection.Max(item => item.Y + item.Height);
        dx = Clamp(dx, -minX, _layout.Width - maxRight);
        dy = Clamp(dy, -minY, _layout.Height - maxBottom);
        if (dx == 0 && dy == 0)
        {
            return;
        }

        PushUndo();
        foreach (var item in _selection)
        {
            item.X += dx;
            item.Y += dy;
        }
        RenderLayout();
        UpdateInspector();
        MarkDirty();
    }

    private void DeleteSelection()
    {
        if (_selection.Count == 0)
        {
            return;
        }

        PushUndo();
        _layout.Elements.RemoveAll(_selection.Contains);
        _selection.Clear();
        RefreshElementList();
        RenderLayout();
        UpdateInspector();
        MarkDirty();
    }

    private void CopySelection()
    {
        _clipboardElements.Clear();
        foreach (var element in _layout.Elements.Where(_selection.Contains))
        {
            _clipboardElements.Add(element.Clone());
        }
    }

    private void PasteSelection()
    {
        if (_clipboardElements.Count == 0)
        {
            return;
        }

        PushUndo();
        var pasted = new List<GuiElementModel>();
        foreach (var source in _clipboardElements)
        {
            var copy = source.Clone();
            copy.Id = CreateUniqueId(source.Id + "_copy");
            copy.X = Clamp(source.X + 4, 0, Math.Max(0, _layout.Width - copy.Width));
            copy.Y = Clamp(source.Y + 4, 0, Math.Max(0, _layout.Height - copy.Height));
            _layout.Elements.Add(copy);
            pasted.Add(copy);
        }

        SetSelection(pasted);
        RefreshElementList();
        RenderLayout();
        MarkDirty();
    }

    private void Undo_Click(object sender, RoutedEventArgs e) => Undo();
    private void Redo_Click(object sender, RoutedEventArgs e) => Redo();

    private void Undo()
    {
        if (_undo.Count == 0)
        {
            return;
        }

        _redo.Push(SerializeLayout());
        RestoreLayoutSnapshot(_undo.Pop());
    }

    private void Redo()
    {
        if (_redo.Count == 0)
        {
            return;
        }

        _undo.Push(SerializeLayout());
        RestoreLayoutSnapshot(_redo.Pop());
    }

    private void RestoreLayoutSnapshot(string snapshot)
    {
        var restored = JsonSerializer.Deserialize<GuiLayoutModel>(snapshot, JsonOptions);
        if (restored is null)
        {
            return;
        }

        _layout = restored;
        _layout.Elements ??= [];
        _selection.Clear();
        _dirty = true;
        RefreshElementList();
        RenderLayout();
        UpdateInspector();
        UpdateStatus();
        UpdateHistoryButtons();
    }

    private string SerializeLayout()
        => JsonSerializer.Serialize(_layout, JsonOptions);

    private void PushUndo()
        => PushUndoSnapshot(SerializeLayout());

    private void PushUndoSnapshot(string snapshot)
    {
        _undo.Push(snapshot);
        if (_undo.Count > 50)
        {
            var retained = _undo.Take(50).Reverse().ToArray();
            _undo.Clear();
            foreach (var item in retained)
            {
                _undo.Push(item);
            }
        }
        _redo.Clear();
        UpdateHistoryButtons();
    }

    private void UpdateHistoryButtons()
    {
        if (UndoButton is null || RedoButton is null)
        {
            return;
        }
        UndoButton.IsEnabled = _undo.Count > 0;
        RedoButton.IsEnabled = _redo.Count > 0;
    }

    private void MarkDirty()
    {
        _dirty = true;
        UpdateStatus();
        UpdateHistoryButtons();
    }

    private void UpdateStatus()
    {
        if (StatusText is null)
        {
            return;
        }

        var file = _currentPath is null ? "unsaved" : Path.GetFileName(_currentPath);
        var dirty = _dirty ? "  • modified" : string.Empty;
        var selection = _selection.Count > 0 ? $"  |  {_selection.Count} selected" : string.Empty;
        var repo = _repoRoot is null ? "  |  no repo" : "  |  repo connected";
        StatusText.Text = $"{_layout.Name}  |  {_layout.Width}×{_layout.Height}  |  {_layout.Elements.Count} elements{selection}  |  {file}{dirty}{repo}";
        Title = $"Forever GUI Designer — {_layout.Name}{(_dirty ? " *" : string.Empty)}";
    }
}
