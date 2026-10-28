using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

public partial class MainWindow
{
    private bool _studioBlockViewerInitialized;
    private string? _studioBlockCatalogRepo;
    private IReadOnlyList<MinecraftBlockAssetDescriptor> _studioBlockAssets = [];
    private ComboBox? _studioBlockStateCombo;
    private ComboBox? _studioBlockFacingCombo;
    private TextBlock? _studioBlockModelText;
    private TextBlock? _studioBlockSourceText;
    private TextBlock? _studioBlockParentText;
    private TextBlock? _studioBlockElementCountText;
    private TextBlock? _studioBlockFaceCountText;
    private TextBlock? _studioBlockModelTextureCountText;
    private TextBlock? _studioBlockVariantText;
    private TextBlock? _studioBlockViewerStatusText;
    private MinecraftBlockModelViewport? _studioBlockViewport;
    private bool _studioBlockSelectorUpdate;

    public void EnableForeverBlockModelViewer()
    {
        if (_studioBlockViewerInitialized || _studioBlockWorkspace is not DockPanel blockWorkspace)
            return;
        _studioBlockViewerInitialized = true;

        var page = blockWorkspace.Children.OfType<Grid>().FirstOrDefault(grid => grid.RowDefinitions.Count == 3);
        if (page is null)
            return;

        foreach (var child in page.Children.Cast<UIElement>().Where(child => Grid.GetRow(child) == 2).ToList())
            page.Children.Remove(child);

        var viewer = BuildStudioBlockViewerSurface();
        Grid.SetRow(viewer, 2);
        page.Children.Add(viewer);

        if (_studioBlockMachineCombo is not null)
            _studioBlockMachineCombo.SelectionChanged += StudioBlockMachine_SelectionChanged;
        if (_studioBlockWorkspace is FrameworkElement workspace)
            workspace.IsVisibleChanged += StudioBlockWorkspace_IsVisibleChanged;

        RefreshStudioBlockCatalog(force: true);
    }

    private FrameworkElement BuildStudioBlockViewerSurface()
    {
        var content = new Grid();
        content.ColumnDefinitions.Add(new ColumnDefinition());
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(470) });

        var viewportCard = StudioCard();
        viewportCard.Margin = new Thickness(0, 0, 20, 0);
        var viewportGrid = new Grid();
        _studioBlockViewport = new MinecraftBlockModelViewport();
        viewportGrid.Children.Add(_studioBlockViewport);

        var editorBadge = new Border
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
                Text = "IN-MEMORY MODEL EDITOR",
                Foreground = Brush("#7ED9DB"),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            }
        };
        Panel.SetZIndex(editorBadge, 10);
        viewportGrid.Children.Add(editorBadge);

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(14)
        };
        var resetView = StudioButton("Reset view", 100);
        resetView.Click += (_, _) => _studioBlockViewport?.ResetView();
        controls.Children.Add(resetView);
        Panel.SetZIndex(controls, 10);
        viewportGrid.Children.Add(controls);

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
                Text = "Left drag: orbit   ·   Mouse wheel: zoom   ·   Double click: reset",
                Foreground = Brush("#A8B6BD"),
                FontSize = 11
            }
        };
        Panel.SetZIndex(hint, 10);
        viewportGrid.Children.Add(hint);
        viewportCard.Child = viewportGrid;
        Grid.SetColumn(viewportCard, 0);
        content.Children.Add(viewportCard);

        var inspectorCard = StudioCard();
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var inspector = new StackPanel { Margin = new Thickness(22) };
        inspector.Children.Add(StudioSectionLabel("MODEL"));
        inspector.Children.Add(new TextBlock
        {
            Text = "Minecraft blockstate and model JSON are loaded directly from the selected repository. Element edits update only the in-memory preview until the later Review / Apply phase.",
            Foreground = Brush("#8FA1AA"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 20)
        });

        inspector.Children.Add(BlockViewerLabel("State"));
        _studioBlockStateCombo = BlockViewerCombo();
        _studioBlockStateCombo.SelectionChanged += StudioBlockState_SelectionChanged;
        inspector.Children.Add(_studioBlockStateCombo);

        inspector.Children.Add(BlockViewerLabel("Facing", 14));
        _studioBlockFacingCombo = BlockViewerCombo();
        _studioBlockFacingCombo.SelectionChanged += StudioBlockFacing_SelectionChanged;
        inspector.Children.Add(_studioBlockFacingCombo);

        inspector.Children.Add(BlockViewerSeparator());
        _studioBlockVariantText = BlockViewerInfo(inspector, "Variant");
        _studioBlockModelText = BlockViewerInfo(inspector, "Model");
        _studioBlockSourceText = BlockViewerInfo(inspector, "Source");
        _studioBlockParentText = BlockViewerInfo(inspector, "Parent");
        _studioBlockElementCountText = BlockViewerInfo(inspector, "Elements");
        _studioBlockFaceCountText = BlockViewerInfo(inspector, "Faces rendered");
        _studioBlockModelTextureCountText = BlockViewerInfo(inspector, "Texture slots");

        BuildStudioBlockModelEditor(inspector);

        inspector.Children.Add(BlockViewerSeparator());
        _studioBlockViewerStatusText = new TextBlock
        {
            Text = "Select a scanned block asset.",
            Foreground = Brush("#91A3AC"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap
        };
        inspector.Children.Add(_studioBlockViewerStatusText);

        scroll.Content = inspector;
        inspectorCard.Child = scroll;
        Grid.SetColumn(inspectorCard, 1);
        content.Children.Add(inspectorCard);
        return content;
    }

    private void StudioBlockWorkspace_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
            RefreshStudioBlockCatalog(force: false);
    }

    private void RefreshStudioBlockCatalog(bool force)
    {
        if (!_studioBlockViewerInitialized || string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot))
            return;
        if (!force && string.Equals(_studioBlockCatalogRepo, _repoRoot, StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var previousId = (_studioBlockMachineCombo?.SelectedItem as MinecraftBlockAssetDescriptor)?.Id;
            _studioBlockAssets = MinecraftBlockModelPreviewService.ScanBlockAssets(_repoRoot);
            _studioBlockCatalogRepo = _repoRoot;

            if (_studioBlockMachineCombo is null)
                return;

            _studioBlockSelectorUpdate = true;
            try
            {
                _studioBlockMachineCombo.ItemsSource = null;
                _studioBlockMachineCombo.DisplayMemberPath = nameof(MinecraftBlockAssetDescriptor.DisplayName);
                _studioBlockMachineCombo.ItemsSource = _studioBlockAssets;
                var selected = previousId is null
                    ? _studioBlockAssets.FirstOrDefault()
                    : _studioBlockAssets.FirstOrDefault(asset => asset.Id.Equals(previousId, StringComparison.OrdinalIgnoreCase))
                      ?? _studioBlockAssets.FirstOrDefault();
                _studioBlockMachineCombo.SelectedItem = selected;
            }
            finally
            {
                _studioBlockSelectorUpdate = false;
            }

            PopulateStudioBlockSelectors();
            if (_studioBlockViewerStatusText is not null && _studioBlockAssets.Count == 0)
                _studioBlockViewerStatusText.Text = "No blockstate-backed block models were found in this repository.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            if (_studioBlockViewerStatusText is not null)
            {
                _studioBlockViewerStatusText.Text = $"Block asset scan failed: {ex.Message}";
                _studioBlockViewerStatusText.Foreground = Brush("#E18B8B");
            }
        }
    }

    private void StudioBlockMachine_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_studioBlockSelectorUpdate)
            PopulateStudioBlockSelectors();
    }

    private void PopulateStudioBlockSelectors()
    {
        if (_studioBlockMachineCombo?.SelectedItem is not MinecraftBlockAssetDescriptor asset ||
            _studioBlockStateCombo is null || _studioBlockFacingCombo is null)
        {
            _studioBlockViewport?.ClearModel();
            ClearStudioBlockEditorModel();
            return;
        }

        _studioBlockSelectorUpdate = true;
        try
        {
            var states = asset.Variants
                .Select(variant => variant.StateLabel)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToList();
            _studioBlockStateCombo.ItemsSource = states;
            _studioBlockStateCombo.SelectedIndex = states.Count > 0 ? 0 : -1;
            PopulateStudioBlockFacings(asset);
        }
        finally
        {
            _studioBlockSelectorUpdate = false;
        }

        LoadSelectedStudioBlockVariant();
    }

    private void StudioBlockState_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_studioBlockSelectorUpdate || _studioBlockMachineCombo?.SelectedItem is not MinecraftBlockAssetDescriptor asset)
            return;

        _studioBlockSelectorUpdate = true;
        try
        {
            PopulateStudioBlockFacings(asset);
        }
        finally
        {
            _studioBlockSelectorUpdate = false;
        }
        LoadSelectedStudioBlockVariant();
    }

    private void PopulateStudioBlockFacings(MinecraftBlockAssetDescriptor asset)
    {
        if (_studioBlockFacingCombo is null)
            return;

        var state = _studioBlockStateCombo?.SelectedItem?.ToString() ?? "default";
        var facings = asset.Variants
            .Where(variant => variant.StateLabel.Equals(state, StringComparison.OrdinalIgnoreCase))
            .Select(variant => variant.Facing)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(FacingSortKey)
            .ThenBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _studioBlockFacingCombo.ItemsSource = facings;
        _studioBlockFacingCombo.SelectedIndex = facings.Count > 0 ? 0 : -1;
    }

    private void StudioBlockFacing_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_studioBlockSelectorUpdate)
            LoadSelectedStudioBlockVariant();
    }

    private void LoadSelectedStudioBlockVariant()
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) ||
            _studioBlockMachineCombo?.SelectedItem is not MinecraftBlockAssetDescriptor asset)
            return;

        var state = _studioBlockStateCombo?.SelectedItem?.ToString() ?? "default";
        var facing = _studioBlockFacingCombo?.SelectedItem?.ToString() ?? "default";
        var variant = asset.Variants.FirstOrDefault(candidate =>
                          candidate.StateLabel.Equals(state, StringComparison.OrdinalIgnoreCase) &&
                          candidate.Facing.Equals(facing, StringComparison.OrdinalIgnoreCase))
                      ?? asset.Variants.FirstOrDefault(candidate =>
                          candidate.StateLabel.Equals(state, StringComparison.OrdinalIgnoreCase))
                      ?? asset.Variants.FirstOrDefault();
        if (variant is null)
        {
            _studioBlockViewport?.ClearModel();
            ClearStudioBlockEditorModel();
            return;
        }

        try
        {
            var model = MinecraftBlockModelPreviewService.LoadResolvedModel(_repoRoot, variant.ModelReference);
            LoadStudioBlockEditorDocumentModel(model, variant.XRotation, variant.YRotation);

            SetBlockViewerText(_studioBlockVariantText, variant.Key);
            SetBlockViewerText(_studioBlockModelText, model.ModelReference);
            SetBlockViewerText(_studioBlockSourceText, model.SourcePath);
            SetBlockViewerText(_studioBlockParentText, model.ParentReference ?? "—");
            SetBlockViewerText(_studioBlockElementCountText, model.Elements.Count.ToString());
            SetBlockViewerText(_studioBlockFaceCountText, (_studioBlockViewport?.RenderedFaceCount ?? 0).ToString());
            SetBlockViewerText(_studioBlockModelTextureCountText, model.Textures.Count.ToString());

            if (_studioBlockViewerStatusText is not null)
            {
                var missing = _studioBlockViewport?.MissingTextureCount ?? 0;
                var rotation = variant.XRotation == 0 && variant.YRotation == 0
                    ? "native orientation"
                    : $"blockstate rotation X {variant.XRotation}° / Y {variant.YRotation}°";
                _studioBlockViewerStatusText.Text = missing == 0
                    ? $"Loaded {asset.DisplayName} · {rotation}. Select an element to edit its preview geometry."
                    : $"Loaded {asset.DisplayName} · {rotation}. {missing} face texture reference(s) could not be resolved locally and use the fallback material.";
                _studioBlockViewerStatusText.Foreground = missing == 0 ? Brush("#74C99A") : Brush("#D8B26A");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            _studioBlockViewport?.ClearModel();
            ClearStudioBlockEditorModel();
            if (_studioBlockViewerStatusText is not null)
            {
                _studioBlockViewerStatusText.Text = $"Could not load model: {ex.Message}";
                _studioBlockViewerStatusText.Foreground = Brush("#E18B8B");
            }
        }
    }

    private static ComboBox BlockViewerCombo()
        => new()
        {
            Height = 34,
            Background = Brush("#182229"),
            Foreground = Brushes.White,
            BorderBrush = Brush("#43545D")
        };

    private static TextBlock BlockViewerLabel(string text, double topMargin = 0)
        => new()
        {
            Text = text,
            Foreground = Brush("#DCE7EC"),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, topMargin, 0, 6)
        };

    private static Border BlockViewerSeparator()
        => new()
        {
            Height = 1,
            Background = Brush("#2C3940"),
            Margin = new Thickness(0, 20, 0, 18)
        };

    private static TextBlock BlockViewerInfo(Panel panel, string label)
    {
        panel.Children.Add(new TextBlock
        {
            Text = label,
            Foreground = Brush("#71848D"),
            FontSize = 10,
            Margin = new Thickness(0, 0, 0, 2)
        });
        var value = new TextBlock
        {
            Text = "—",
            Foreground = Brush("#D3DEE3"),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        };
        panel.Children.Add(value);
        return value;
    }

    private static void SetBlockViewerText(TextBlock? target, string value)
    {
        if (target is not null)
            target.Text = string.IsNullOrWhiteSpace(value) ? "—" : value;
    }

    private static int FacingSortKey(string value)
        => value.ToLowerInvariant() switch
        {
            "north" => 0,
            "east" => 1,
            "south" => 2,
            "west" => 3,
            "up" => 4,
            "down" => 5,
            "default" => 6,
            _ => 7
        };
}
