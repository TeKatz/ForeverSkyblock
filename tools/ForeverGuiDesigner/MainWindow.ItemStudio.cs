using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 14 Item Studio. Item model resolution and display-transform editing stay in memory.
/// Controlled persistence is intentionally deferred to the Review / Validate / Apply phase.
/// </summary>
public partial class MainWindow
{
    private static readonly (string Key, string Label)[] StudioItemDisplayModes =
    [
        ("gui", "GUI"),
        ("ground", "Ground"),
        ("fixed", "Fixed"),
        ("firstperson_righthand", "First Person Right"),
        ("firstperson_lefthand", "First Person Left"),
        ("thirdperson_righthand", "Third Person Right"),
        ("thirdperson_lefthand", "Third Person Left"),
        ("head", "Head")
    ];

    private bool _studioItemStudioInitialized;
    private FrameworkElement? _studioBlockEditorSurface;
    private FrameworkElement? _studioItemEditorSurface;
    private FrameworkElement? _studioBlockMachineRow;
    private Button? _studioBlockAssetTab;
    private Button? _studioItemAssetTab;
    private ComboBox? _studioItemCombo;
    private ComboBox? _studioItemDisplayModeCombo;
    private TextBlock? _studioItemSourceText;
    private TextBlock? _studioItemParentText;
    private TextBlock? _studioItemResolvedParentText;
    private TextBlock? _studioItemKindText;
    private TextBlock? _studioItemResolvedAssetText;
    private TextBlock? _studioItemEditStateText;
    private TextBlock? _studioItemStatusText;
    private MinecraftBlockModelViewport? _studioItemBlockViewport;
    private Grid? _studioItemFlatPreviewHost;
    private Image? _studioItemFlatImage;
    private readonly TextBox?[] _studioItemRotationBoxes = new TextBox?[3];
    private readonly TextBox?[] _studioItemTranslationBoxes = new TextBox?[3];
    private readonly TextBox?[] _studioItemScaleBoxes = new TextBox?[3];
    private IReadOnlyList<MinecraftItemAssetDescriptor> _studioItemAssets = [];
    private string? _studioItemCatalogRepo;
    private MinecraftResolvedItemModel? _studioLoadedItem;
    private Dictionary<string, MinecraftItemDisplayTransform> _studioWorkingItemTransforms = new(StringComparer.OrdinalIgnoreCase);
    private bool _studioItemEditorUpdating;
    private bool _studioItemModelModified;

    public void EnableForeverItemStudio()
    {
        if (_studioItemStudioInitialized || _studioBlockWorkspace is not DockPanel blockWorkspace)
            return;

        var page = blockWorkspace.Children.OfType<Grid>().FirstOrDefault(grid => grid.RowDefinitions.Count == 3);
        if (page is null)
            return;

        var tabs = page.Children.OfType<StackPanel>().FirstOrDefault(panel => Grid.GetRow(panel) == 1);
        if (tabs is null)
            return;

        _studioBlockAssetTab = tabs.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "BLOCK", StringComparison.OrdinalIgnoreCase));
        _studioItemAssetTab = tabs.Children.OfType<Button>()
            .FirstOrDefault(button => string.Equals(button.Content?.ToString(), "ITEM", StringComparison.OrdinalIgnoreCase));
        if (_studioBlockAssetTab is null || _studioItemAssetTab is null)
            return;

        _studioItemStudioInitialized = true;
        _studioBlockMachineRow = page.Children.Cast<UIElement>()
            .OfType<FrameworkElement>()
            .FirstOrDefault(element => Grid.GetRow(element) == 0);
        _studioBlockEditorSurface = page.Children.Cast<UIElement>()
            .OfType<FrameworkElement>()
            .FirstOrDefault(element => Grid.GetRow(element) == 2);

        _studioItemEditorSurface = BuildStudioItemSurface();
        _studioItemEditorSurface.Visibility = Visibility.Collapsed;
        Grid.SetRow(_studioItemEditorSurface, 2);
        page.Children.Add(_studioItemEditorSurface);

        _studioBlockAssetTab.IsEnabled = true;
        _studioItemAssetTab.IsEnabled = true;
        _studioBlockAssetTab.Click += (_, _) => ShowStudioAssetEditor(itemMode: false);
        _studioItemAssetTab.Click += (_, _) => ShowStudioAssetEditor(itemMode: true);
        SetStudioAssetTabVisuals(itemMode: false);
    }

    private FrameworkElement BuildStudioItemSurface()
    {
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition());
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(470) });

        var previewCard = StudioCard();
        previewCard.Margin = new Thickness(0, 0, 20, 0);
        var previewGrid = new Grid();

        _studioItemBlockViewport = new MinecraftBlockModelViewport
        {
            Visibility = Visibility.Collapsed
        };
        previewGrid.Children.Add(_studioItemBlockViewport);

        _studioItemFlatPreviewHost = new Grid
        {
            Background = Brush("#0C1013"),
            Visibility = Visibility.Collapsed
        };
        var flatFrame = new Border
        {
            Width = 360,
            Height = 360,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Background = Brush("#0A0E11"),
            BorderBrush = Brush("#2E3C43"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4)
        };
        _studioItemFlatImage = new Image
        {
            Width = 256,
            Height = 256,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true,
            RenderTransformOrigin = new Point(0.5, 0.5)
        };
        RenderOptions.SetBitmapScalingMode(_studioItemFlatImage, BitmapScalingMode.NearestNeighbor);
        flatFrame.Child = _studioItemFlatImage;
        _studioItemFlatPreviewHost.Children.Add(flatFrame);
        previewGrid.Children.Add(_studioItemFlatPreviewHost);

        var badge = new Border
        {
            Background = Brush("#16262B"),
            BorderBrush = Brush("#3E6E73"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(14),
            Padding = new Thickness(10, 5, 10, 5),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = "ITEM PREVIEW",
                Foreground = Brush("#7ED9DB"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            }
        };
        Panel.SetZIndex(badge, 10);
        previewGrid.Children.Add(badge);

        var resetView = StudioButton("Reset view", 100);
        resetView.HorizontalAlignment = HorizontalAlignment.Right;
        resetView.VerticalAlignment = VerticalAlignment.Top;
        resetView.Margin = new Thickness(14);
        resetView.Click += (_, _) =>
        {
            _studioItemBlockViewport?.ResetView();
            ApplyStudioItemPreviewTransform();
        };
        Panel.SetZIndex(resetView, 10);
        previewGrid.Children.Add(resetView);

        var hint = new Border
        {
            Background = Brush("#C0101417"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(16),
            Padding = new Thickness(12, 7, 12, 7),
            CornerRadius = new CornerRadius(4),
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = "BlockItems: orbit with left drag · Generated items: pixel preview · Display values remain in memory",
                Foreground = Brush("#A8B6BD"),
                FontSize = 11
            }
        };
        Panel.SetZIndex(hint, 10);
        previewGrid.Children.Add(hint);
        previewCard.Child = previewGrid;
        Grid.SetColumn(previewCard, 0);
        content.Children.Add(previewCard);

        var inspectorCard = StudioCard();
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var inspector = new StackPanel { Margin = new Thickness(22) };
        inspector.Children.Add(StudioSectionLabel("ITEM STUDIO"));
        inspector.Children.Add(new TextBlock
        {
            Text = "Inspect machine BlockItems and normal generated items, then edit Minecraft display transforms per preview context. Changes stay in memory.",
            Foreground = Brush("#8FA1AA"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 18)
        });

        inspector.Children.Add(BlockViewerLabel("Item"));
        _studioItemCombo = BlockViewerCombo();
        _studioItemCombo.DisplayMemberPath = nameof(MinecraftItemAssetDescriptor.DisplayName);
        _studioItemCombo.SelectionChanged += (_, _) =>
        {
            if (!_studioItemEditorUpdating)
                LoadSelectedStudioItem();
        };
        inspector.Children.Add(_studioItemCombo);

        inspector.Children.Add(BlockViewerSeparator());
        _studioItemSourceText = BlockViewerInfo(inspector, "Source");
        _studioItemParentText = BlockViewerInfo(inspector, "Parent");
        _studioItemResolvedParentText = BlockViewerInfo(inspector, "Terminal parent");
        _studioItemKindText = BlockViewerInfo(inspector, "Kind");
        _studioItemResolvedAssetText = BlockViewerInfo(inspector, "Preview asset");

        inspector.Children.Add(BlockViewerSeparator());
        inspector.Children.Add(StudioSectionLabel("DISPLAY TRANSFORM"));
        _studioItemEditStateText = new TextBlock
        {
            Text = "SOURCE STATE",
            Foreground = Brush("#74C99A"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 8, 0, 12)
        };
        inspector.Children.Add(_studioItemEditStateText);

        inspector.Children.Add(BlockViewerLabel("Preview context"));
        _studioItemDisplayModeCombo = BlockViewerCombo();
        _studioItemDisplayModeCombo.ItemsSource = StudioItemDisplayModes.Select(mode => mode.Label).ToList();
        _studioItemDisplayModeCombo.SelectedIndex = 0;
        _studioItemDisplayModeCombo.SelectionChanged += (_, _) =>
        {
            if (!_studioItemEditorUpdating)
                PopulateStudioItemTransformFields();
        };
        inspector.Children.Add(_studioItemDisplayModeCombo);

        inspector.Children.Add(BlockViewerLabel("Rotation", 15));
        inspector.Children.Add(BuildStudioItemVectorEditor(_studioItemRotationBoxes));
        inspector.Children.Add(BlockViewerLabel("Translation", 12));
        inspector.Children.Add(BuildStudioItemVectorEditor(_studioItemTranslationBoxes));
        inspector.Children.Add(BlockViewerLabel("Scale", 12));
        inspector.Children.Add(BuildStudioItemVectorEditor(_studioItemScaleBoxes));

        var commandRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 14, 0, 0)
        };
        var resetTransform = StudioButton("Reset transform", 135);
        resetTransform.Margin = new Thickness(0, 0, 8, 0);
        resetTransform.Click += (_, _) => ResetStudioItemTransform();
        commandRow.Children.Add(resetTransform);
        var resetItem = StudioButton("Reset item", 110);
        resetItem.Click += (_, _) => LoadSelectedStudioItem();
        commandRow.Children.Add(resetItem);
        inspector.Children.Add(commandRow);

        inspector.Children.Add(BlockViewerSeparator());
        _studioItemStatusText = new TextBlock
        {
            Text = "Open the ITEM tab to scan item models.",
            Foreground = Brush("#91A3AC"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        inspector.Children.Add(_studioItemStatusText);

        scroll.Content = inspector;
        inspectorCard.Child = scroll;
        Grid.SetColumn(inspectorCard, 1);
        content.Children.Add(inspectorCard);
        return content;
    }

    private FrameworkElement BuildStudioItemVectorEditor(TextBox?[] targets)
    {
        var grid = new Grid();
        for (var i = 0; i < 3; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition());

        var labels = new[] { "X", "Y", "Z" };
        for (var i = 0; i < 3; i++)
        {
            var panel = new StackPanel { Margin = new Thickness(i == 0 ? 0 : 5, 0, i == 2 ? 0 : 5, 0) };
            panel.Children.Add(new TextBlock
            {
                Text = labels[i],
                Foreground = Brush("#71848D"),
                FontSize = 9,
                Margin = new Thickness(1, 0, 0, 3)
            });
            var box = StudioModelNumberBox();
            box.LostFocus += (_, _) => CommitStudioItemTransform();
            box.PreviewKeyDown += (_, e) =>
            {
                if (e.Key != Key.Enter)
                    return;
                CommitStudioItemTransform();
                e.Handled = true;
            };
            targets[i] = box;
            panel.Children.Add(box);
            Grid.SetColumn(panel, i);
            grid.Children.Add(panel);
        }
        return grid;
    }

    private void ShowStudioAssetEditor(bool itemMode)
    {
        if (_studioBlockEditorSurface is not null)
            _studioBlockEditorSurface.Visibility = itemMode ? Visibility.Collapsed : Visibility.Visible;
        if (_studioItemEditorSurface is not null)
            _studioItemEditorSurface.Visibility = itemMode ? Visibility.Visible : Visibility.Collapsed;
        if (_studioBlockMachineRow is not null)
            _studioBlockMachineRow.Visibility = itemMode ? Visibility.Collapsed : Visibility.Visible;
        SetStudioAssetTabVisuals(itemMode);

        if (itemMode)
            RefreshStudioItemCatalog(force: !string.Equals(_studioItemCatalogRepo, _repoRoot, StringComparison.OrdinalIgnoreCase));
    }

    private void SetStudioAssetTabVisuals(bool itemMode)
    {
        if (_studioBlockAssetTab is not null)
        {
            _studioBlockAssetTab.Background = Brush(itemMode ? "#1A252B" : "#286F78");
            _studioBlockAssetTab.BorderBrush = Brush(itemMode ? "#35464F" : "#58B9C0");
        }
        if (_studioItemAssetTab is not null)
        {
            _studioItemAssetTab.Background = Brush(itemMode ? "#286F78" : "#1A252B");
            _studioItemAssetTab.BorderBrush = Brush(itemMode ? "#58B9C0" : "#35464F");
            _studioItemAssetTab.Foreground = Brushes.White;
        }
    }

    private void RefreshStudioItemCatalog(bool force)
    {
        if (!_studioItemStudioInitialized || _studioItemCombo is null || string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot))
            return;
        if (!force && string.Equals(_studioItemCatalogRepo, _repoRoot, StringComparison.OrdinalIgnoreCase) && _studioItemAssets.Count > 0)
            return;

        try
        {
            var previous = (_studioItemCombo.SelectedItem as MinecraftItemAssetDescriptor)?.Id;
            _studioItemAssets = MinecraftItemModelService.ScanItemAssets(_repoRoot);
            _studioItemCatalogRepo = _repoRoot;

            _studioItemEditorUpdating = true;
            try
            {
                _studioItemCombo.ItemsSource = null;
                _studioItemCombo.ItemsSource = _studioItemAssets;
                _studioItemCombo.SelectedItem = previous is null
                    ? _studioItemAssets.FirstOrDefault()
                    : _studioItemAssets.FirstOrDefault(item => item.Id.Equals(previous, StringComparison.OrdinalIgnoreCase))
                      ?? _studioItemAssets.FirstOrDefault();
            }
            finally
            {
                _studioItemEditorUpdating = false;
            }

            LoadSelectedStudioItem();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            SetStudioItemStatus($"Item scan failed: {ex.Message}", "#E18B8B");
        }
    }

    private void LoadSelectedStudioItem()
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) || _studioItemCombo?.SelectedItem is not MinecraftItemAssetDescriptor item)
            return;

        try
        {
            _studioLoadedItem = MinecraftItemModelService.LoadResolvedItem(_repoRoot, item.Id);
            _studioWorkingItemTransforms = _studioLoadedItem.DisplayTransforms.ToDictionary(
                pair => pair.Key,
                pair => CloneStudioItemTransform(pair.Value),
                StringComparer.OrdinalIgnoreCase);
            _studioItemModelModified = false;

            SetBlockViewerText(_studioItemSourceText, _studioLoadedItem.SourcePath);
            SetBlockViewerText(_studioItemParentText, _studioLoadedItem.ParentReference ?? "—");
            SetBlockViewerText(_studioItemResolvedParentText, _studioLoadedItem.TerminalParentReference ?? "—");
            SetBlockViewerText(_studioItemKindText, _studioLoadedItem.IsGeneratedItem ? "Generated / 2D item" : "BlockItem / inventory model");
            SetBlockViewerText(
                _studioItemResolvedAssetText,
                _studioLoadedItem.BlockModelReference ?? _studioLoadedItem.Layer0TextureReference ?? "—");

            PopulateStudioItemTransformFields();
            RenderStudioItemPreview();
            UpdateStudioItemEditState();
            SetStudioItemStatus(
                _studioLoadedItem.IsGeneratedItem
                    ? $"Loaded {item.DisplayName}. Pixel preview uses layer0; display transforms are editable per context."
                    : $"Loaded {item.DisplayName}. Inventory block model resolved for 3D preview.",
                "#74C99A");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            _studioLoadedItem = null;
            _studioWorkingItemTransforms.Clear();
            _studioItemBlockViewport?.ClearModel();
            if (_studioItemFlatImage is not null)
                _studioItemFlatImage.Source = null;
            SetStudioItemStatus($"Could not load item model: {ex.Message}", "#E18B8B");
        }
    }

    private void PopulateStudioItemTransformFields()
    {
        if (_studioLoadedItem is null)
            return;

        var mode = GetSelectedStudioItemDisplayModeKey();
        var transform = _studioWorkingItemTransforms.TryGetValue(mode, out var existing)
            ? existing
            : IdentityStudioItemTransform();

        _studioItemEditorUpdating = true;
        try
        {
            SetStudioItemVectorBoxes(_studioItemRotationBoxes, transform.Rotation);
            SetStudioItemVectorBoxes(_studioItemTranslationBoxes, transform.Translation);
            SetStudioItemVectorBoxes(_studioItemScaleBoxes, transform.Scale);
        }
        finally
        {
            _studioItemEditorUpdating = false;
        }
        ApplyStudioItemPreviewTransform();
    }

    private void CommitStudioItemTransform()
    {
        if (_studioItemEditorUpdating || _studioLoadedItem is null)
            return;
        if (!TryReadStudioItemVector(_studioItemRotationBoxes, out var rotation) ||
            !TryReadStudioItemVector(_studioItemTranslationBoxes, out var translation) ||
            !TryReadStudioItemVector(_studioItemScaleBoxes, out var scale))
        {
            SetStudioItemStatus("Invalid transform value. Changes were not applied.", "#E18B8B");
            return;
        }

        var mode = GetSelectedStudioItemDisplayModeKey();
        _studioWorkingItemTransforms[mode] = new MinecraftItemDisplayTransform
        {
            Rotation = rotation,
            Translation = translation,
            Scale = scale
        };
        _studioItemModelModified = true;
        UpdateStudioItemEditState();
        ApplyStudioItemPreviewTransform();
        SetStudioItemStatus($"{GetSelectedStudioItemDisplayModeLabel()} transform updated in memory. Item JSON is unchanged on disk.", "#74C99A");
    }

    private void ResetStudioItemTransform()
    {
        if (_studioLoadedItem is null)
            return;
        var mode = GetSelectedStudioItemDisplayModeKey();
        if (_studioLoadedItem.DisplayTransforms.TryGetValue(mode, out var source))
            _studioWorkingItemTransforms[mode] = CloneStudioItemTransform(source);
        else
            _studioWorkingItemTransforms.Remove(mode);
        _studioItemModelModified = !StudioItemTransformsEqual(_studioWorkingItemTransforms, _studioLoadedItem.DisplayTransforms);
        PopulateStudioItemTransformFields();
        UpdateStudioItemEditState();
        SetStudioItemStatus($"{GetSelectedStudioItemDisplayModeLabel()} reset to resolved source state.", "#74C99A");
    }

    private void RenderStudioItemPreview()
    {
        if (_studioLoadedItem is null || string.IsNullOrWhiteSpace(_repoRoot))
            return;

        if (_studioLoadedItem.BlockModelReference is { Length: > 0 } blockReference)
        {
            _studioItemFlatPreviewHost?.SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
            if (_studioItemBlockViewport is not null)
            {
                _studioItemBlockViewport.Visibility = Visibility.Visible;
                try
                {
                    var blockModel = MinecraftBlockModelPreviewService.LoadResolvedModel(_repoRoot, blockReference);
                    _studioItemBlockViewport.SetModel(_repoRoot, blockModel, 0, 0);
                }
                catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
                {
                    _studioItemBlockViewport.ClearModel();
                    SetStudioItemStatus($"Inventory model preview failed: {ex.Message}", "#D8B26A");
                }
            }
        }
        else
        {
            if (_studioItemBlockViewport is not null)
                _studioItemBlockViewport.Visibility = Visibility.Collapsed;
            if (_studioItemFlatPreviewHost is not null)
                _studioItemFlatPreviewHost.Visibility = Visibility.Visible;
            if (_studioItemFlatImage is not null)
            {
                _studioItemFlatImage.Source = LoadStudioItemBitmap(_studioLoadedItem.Layer0TexturePath);
                RenderOptions.SetBitmapScalingMode(_studioItemFlatImage, BitmapScalingMode.NearestNeighbor);
            }
        }

        ApplyStudioItemPreviewTransform();
    }

    private void ApplyStudioItemPreviewTransform()
    {
        if (_studioLoadedItem is null)
            return;
        var mode = GetSelectedStudioItemDisplayModeKey();
        var transform = _studioWorkingItemTransforms.TryGetValue(mode, out var current)
            ? current
            : IdentityStudioItemTransform();

        // WPF UI transforms provide immediate framing feedback without mutating the shared 3D renderer.
        // X/Y item rotations remain represented numerically; full Minecraft 3D display-space parity can be
        // layered onto the renderer later without coupling item state into the block-model editor.
        var group = new TransformGroup();
        var sx = Math.Clamp(transform.Scale.ElementAtOrDefault(0), -8, 8);
        var sy = Math.Clamp(transform.Scale.ElementAtOrDefault(1), -8, 8);
        if (Math.Abs(sx) < 0.0001) sx = 0.0001;
        if (Math.Abs(sy) < 0.0001) sy = 0.0001;
        group.Children.Add(new ScaleTransform(sx, sy));
        group.Children.Add(new RotateTransform(transform.Rotation.ElementAtOrDefault(2)));
        group.Children.Add(new TranslateTransform(
            transform.Translation.ElementAtOrDefault(0) * 3,
            -transform.Translation.ElementAtOrDefault(1) * 3));

        if (_studioLoadedItem.IsGeneratedItem && _studioItemFlatImage is not null)
        {
            _studioItemFlatImage.RenderTransformOrigin = new Point(0.5, 0.5);
            _studioItemFlatImage.RenderTransform = group;
        }
        else if (_studioItemBlockViewport is not null)
        {
            _studioItemBlockViewport.RenderTransformOrigin = new Point(0.5, 0.5);
            _studioItemBlockViewport.RenderTransform = group;
        }
    }

    private static BitmapSource? LoadStudioItemBitmap(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch (IOException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private string GetSelectedStudioItemDisplayModeKey()
    {
        var index = _studioItemDisplayModeCombo?.SelectedIndex ?? 0;
        return StudioItemDisplayModes[Math.Clamp(index, 0, StudioItemDisplayModes.Length - 1)].Key;
    }

    private string GetSelectedStudioItemDisplayModeLabel()
    {
        var index = _studioItemDisplayModeCombo?.SelectedIndex ?? 0;
        return StudioItemDisplayModes[Math.Clamp(index, 0, StudioItemDisplayModes.Length - 1)].Label;
    }

    private static MinecraftItemDisplayTransform IdentityStudioItemTransform()
        => new();

    private static MinecraftItemDisplayTransform CloneStudioItemTransform(MinecraftItemDisplayTransform source)
        => new()
        {
            Rotation = [.. source.Rotation],
            Translation = [.. source.Translation],
            Scale = [.. source.Scale]
        };

    private bool TryReadStudioItemVector(TextBox?[] boxes, out double[] values)
    {
        values = new double[3];
        for (var i = 0; i < 3; i++)
        {
            if (boxes[i] is null || !TryParseStudioModelNumber(boxes[i]!.Text, out values[i]))
                return false;
        }
        return true;
    }

    private static void SetStudioItemVectorBoxes(TextBox?[] boxes, double[] values)
    {
        for (var i = 0; i < 3; i++)
        {
            if (boxes[i] is not null)
                boxes[i]!.Text = values.ElementAtOrDefault(i).ToString("0.###", CultureInfo.InvariantCulture);
        }
    }

    private static bool StudioItemTransformsEqual(
        IReadOnlyDictionary<string, MinecraftItemDisplayTransform> left,
        IReadOnlyDictionary<string, MinecraftItemDisplayTransform> right)
    {
        if (left.Count != right.Count)
            return false;
        foreach (var (key, value) in left)
        {
            if (!right.TryGetValue(key, out var other) ||
                !value.Rotation.SequenceEqual(other.Rotation) ||
                !value.Translation.SequenceEqual(other.Translation) ||
                !value.Scale.SequenceEqual(other.Scale))
                return false;
        }
        return true;
    }

    private void UpdateStudioItemEditState()
    {
        if (_studioItemEditStateText is null)
            return;
        _studioItemEditStateText.Text = _studioItemModelModified ? "PREVIEW MODIFIED · NOT SAVED" : "SOURCE STATE";
        _studioItemEditStateText.Foreground = Brush(_studioItemModelModified ? "#D8B26A" : "#74C99A");
    }

    private void SetStudioItemStatus(string message, string color)
    {
        if (_studioItemStatusText is null)
            return;
        _studioItemStatusText.Text = message;
        _studioItemStatusText.Foreground = Brush(color);
    }
}
