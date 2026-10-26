using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 14 generated-item texture editing and Minecraft-style 3D display-transform preview.
/// All changes stay in memory until the later Review / Validate / Apply phase.
/// </summary>
public partial class MainWindow
{
    private bool _studioItemTextureStudioInitialized;
    private MinecraftTextureUvEditor? _studioItemTextureEditor;
    private TextBlock? _studioItemTextureStateText;
    private TextBox? _studioItemReplaceSourceBox;
    private TextBox? _studioItemAlphaBox;
    private readonly Dictionary<string, BitmapSource> _studioItemTextureOverrides = new(StringComparer.OrdinalIgnoreCase);

    public void EnableForeverItemTextureStudio()
    {
        if (_studioItemTextureStudioInitialized || _studioItemCombo is null || _studioItemCombo.Parent is not Panel inspector || _studioItemBlockViewport is null)
            return;
        _studioItemTextureStudioInitialized = true;

        foreach (var hint in FindStudioVisualChildren<TextBlock>(_studioItemEditorSurface ?? inspector)
                     .Where(text => text.Text.StartsWith("BlockItems:", StringComparison.OrdinalIgnoreCase)))
        {
            hint.Text = "3D item preview · full X/Y/Z rotation, translation and scale · right/middle drag pans · wheel zooms";
        }

        _studioItemCombo.SelectionChanged += (_, _) => QueueEnhancedItemRefresh();
        if (_studioItemDisplayModeCombo is not null)
            _studioItemDisplayModeCombo.SelectionChanged += (_, _) => QueueEnhancedItemRefresh();
        foreach (var box in _studioItemRotationBoxes.Concat(_studioItemTranslationBoxes).Concat(_studioItemScaleBoxes))
        {
            if (box is null)
                continue;
            box.LostFocus += (_, _) => QueueEnhancedItemRefresh();
            box.PreviewKeyUp += (_, e) =>
            {
                if (e.Key == Key.Enter)
                    QueueEnhancedItemRefresh();
            };
        }

        foreach (var button in FindStudioVisualChildren<Button>(_studioItemEditorSurface ?? inspector))
        {
            var label = button.Content?.ToString();
            if (string.Equals(label, "Reset transform", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(label, "Reset item", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(label, "Reset view", StringComparison.OrdinalIgnoreCase))
            {
                button.Click += (_, _) => QueueEnhancedItemRefresh();
            }
        }

        inspector.Children.Add(BlockViewerSeparator());
        inspector.Children.Add(StudioSectionLabel("ITEM TEXTURE EDITOR"));
        inspector.Children.Add(new TextBlock
        {
            Text = "Generated items edit layer0 directly in memory. The same pixel workflow as the block texture editor is reused; no PNG is written before the later Review / Apply phase.",
            Foreground = Brush("#91A3AC"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 12)
        });

        _studioItemTextureEditor = new MinecraftTextureUvEditor
        {
            Height = 300,
            UvSnap = 0.25,
            Visibility = Visibility.Collapsed
        };
        _studioItemTextureEditor.TextureChanged += bitmap =>
        {
            if (_studioLoadedItem?.Layer0TexturePath is not { Length: > 0 } path)
                return;
            _studioItemTextureOverrides[path] = bitmap;
            UpdateStudioItemTextureState(path);
            QueueEnhancedItemRefresh(reloadTextureEditor: false);
        };
        _studioItemTextureEditor.ColorPicked += color =>
        {
            if (_studioItemReplaceSourceBox is not null)
                _studioItemReplaceSourceBox.Text = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        };
        inspector.Children.Add(_studioItemTextureEditor);

        var itemTools = new WrapPanel { Margin = new Thickness(0, 8, 0, 8) };
        AddTextureToolButton(itemTools, _studioItemTextureEditor, "Pencil", MinecraftTextureEditTool.Pencil, 68);
        AddTextureToolButton(itemTools, _studioItemTextureEditor, "Eraser", MinecraftTextureEditTool.Eraser, 68);
        AddTextureToolButton(itemTools, _studioItemTextureEditor, "Fill", MinecraftTextureEditTool.Fill, 52);
        AddTextureToolButton(itemTools, _studioItemTextureEditor, "Picker", MinecraftTextureEditTool.Eyedropper, 62);
        AddTextureToolButton(itemTools, _studioItemTextureEditor, "Line", MinecraftTextureEditTool.Line, 52);
        AddTextureToolButton(itemTools, _studioItemTextureEditor, "Rect", MinecraftTextureEditTool.Rectangle, 52);
        AddTextureToolButton(itemTools, _studioItemTextureEditor, "Select", MinecraftTextureEditTool.Selection, 62);
        inspector.Children.Add(itemTools);
        inspector.Children.Add(BuildStudioTextureSelectionCommands(_studioItemTextureEditor));

        _studioItemReplaceSourceBox = BuildStudioColorBox("#FFFFFFFF");
        _studioItemAlphaBox = StudioModelNumberBox();
        _studioItemAlphaBox.Text = "255";
        inspector.Children.Add(BuildStudioReplaceAlphaPanel(
            _studioItemTextureEditor,
            _studioItemReplaceSourceBox,
            _studioItemAlphaBox,
            () => $"#{_studioItemTextureEditor.PaintColor.A:X2}{_studioItemTextureEditor.PaintColor.R:X2}{_studioItemTextureEditor.PaintColor.G:X2}{_studioItemTextureEditor.PaintColor.B:X2}",
            SetStudioItemStatus));
        inspector.Children.Add(BuildStudioMachinePalette(_studioItemTextureEditor, null));

        var resetTexture = StudioButton("Reset item texture", 145);
        resetTexture.HorizontalAlignment = HorizontalAlignment.Left;
        resetTexture.Margin = new Thickness(0, 4, 0, 0);
        resetTexture.Click += (_, _) => ResetStudioItemTexture();
        inspector.Children.Add(resetTexture);

        _studioItemTextureStateText = new TextBlock
        {
            Text = "SOURCE ITEM TEXTURE",
            Foreground = Brush("#74C99A"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 10, 0, 0)
        };
        inspector.Children.Add(_studioItemTextureStateText);

        QueueEnhancedItemRefresh();
    }

    private void QueueEnhancedItemRefresh(bool reloadTextureEditor = true)
        => Dispatcher.BeginInvoke(() => RefreshEnhancedItemPreview(reloadTextureEditor), DispatcherPriority.Background);

    private void RefreshEnhancedItemPreview(bool reloadTextureEditor)
    {
        if (_studioLoadedItem is null || _studioItemBlockViewport is null || string.IsNullOrWhiteSpace(_repoRoot))
            return;

        _studioItemBlockViewport.RenderTransform = Transform.Identity;
        _studioItemBlockViewport.SetTextureOverrides(_studioItemTextureOverrides);

        if (_studioLoadedItem.IsGeneratedItem &&
            _studioLoadedItem.Layer0TextureReference is { Length: > 0 } layer0 &&
            _studioLoadedItem.Layer0TexturePath is { Length: > 0 } texturePath)
        {
            if (_studioItemFlatPreviewHost is not null)
                _studioItemFlatPreviewHost.Visibility = Visibility.Collapsed;
            _studioItemBlockViewport.Visibility = Visibility.Visible;
            _studioItemBlockViewport.SetModel(_repoRoot, BuildStudioGeneratedItemPreviewModel(_studioLoadedItem, layer0), 0, 0);

            if (_studioItemTextureEditor is not null)
            {
                _studioItemTextureEditor.Visibility = Visibility.Visible;
                if (reloadTextureEditor)
                {
                    _studioItemTextureOverrides.TryGetValue(texturePath, out var overrideBitmap);
                    _studioItemTextureEditor.LoadTexture(texturePath, overrideBitmap, [0, 0, 16, 16]);
                }
            }
            UpdateStudioItemTextureState(texturePath);
        }
        else
        {
            if (_studioItemTextureEditor is not null)
                _studioItemTextureEditor.Visibility = Visibility.Collapsed;
            if (_studioItemTextureStateText is not null)
            {
                _studioItemTextureStateText.Text = "BLOCKITEM USES INVENTORY MODEL TEXTURES";
                _studioItemTextureStateText.Foreground = Brush("#81939C");
            }
        }

        ApplyEnhancedItemDisplayTransform();
    }

    private void ApplyEnhancedItemDisplayTransform()
    {
        if (_studioLoadedItem is null || _studioItemBlockViewport is null)
            return;
        var mode = GetSelectedStudioItemDisplayModeKey();
        var transform = _studioWorkingItemTransforms.TryGetValue(mode, out var value)
            ? value
            : IdentityStudioItemTransform();
        _studioItemBlockViewport.RenderTransform = Transform.Identity;
        _studioItemBlockViewport.SetDisplayTransform(transform);
    }

    private void ResetStudioItemTexture()
    {
        if (_studioLoadedItem?.Layer0TexturePath is not { Length: > 0 } texturePath || _studioItemTextureEditor is null)
            return;
        _studioItemTextureOverrides.Remove(texturePath);
        _studioItemTextureEditor.LoadTexture(texturePath, null, [0, 0, 16, 16]);
        UpdateStudioItemTextureState(texturePath);
        QueueEnhancedItemRefresh(reloadTextureEditor: false);
        SetStudioItemStatus("Item texture reset to the repository PNG.", "#74C99A");
    }

    private void UpdateStudioItemTextureState(string texturePath)
    {
        if (_studioItemTextureStateText is null)
            return;
        var modified = _studioItemTextureOverrides.ContainsKey(texturePath);
        _studioItemTextureStateText.Text = modified ? "ITEM TEXTURE MODIFIED · NOT SAVED" : "SOURCE ITEM TEXTURE";
        _studioItemTextureStateText.Foreground = Brush(modified ? "#D8B26A" : "#74C99A");
    }

    private static MinecraftResolvedBlockModel BuildStudioGeneratedItemPreviewModel(MinecraftResolvedItemModel item, string layer0)
    {
        var faces = new Dictionary<string, MinecraftModelFace>(StringComparer.OrdinalIgnoreCase)
        {
            ["north"] = new MinecraftModelFace { Texture = "#layer0", Uv = [0, 0, 16, 16] },
            ["south"] = new MinecraftModelFace { Texture = "#layer0", Uv = [0, 0, 16, 16] }
        };
        return new MinecraftResolvedBlockModel
        {
            ModelReference = $"generated-item:{item.Id}",
            SourcePath = item.SourcePath,
            ParentReference = item.ParentReference,
            AmbientOcclusion = false,
            Textures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["layer0"] = layer0 },
            Elements =
            [
                new MinecraftModelElement
                {
                    From = [0, 0, 7.94],
                    To = [16, 16, 8.06],
                    Faces = faces
                }
            ]
        };
    }
}
