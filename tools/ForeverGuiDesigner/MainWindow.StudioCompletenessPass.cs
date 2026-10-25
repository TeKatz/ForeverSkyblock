using System.Globalization;
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
/// Consolidation pass for phases 10-14. This fills the intentionally deferred interaction/fidelity gaps
/// without introducing repository writes; Phase 16 remains the owner of validation/review/apply.
/// </summary>
public partial class MainWindow
{
    private bool _studioCompletenessPassInitialized;
    private MinecraftTextureUvEditor? _studioItemTextureEditor;
    private TextBlock? _studioItemTextureStateText;
    private TextBox? _studioTextureReplaceSourceBox;
    private TextBox? _studioTextureAlphaBox;
    private TextBox? _studioItemReplaceSourceBox;
    private TextBox? _studioItemAlphaBox;
    private readonly Dictionary<string, BitmapSource> _studioItemTextureOverrides = new(StringComparer.OrdinalIgnoreCase);

    public void EnableForeverStudioCompletenessPass()
    {
        if (_studioCompletenessPassInitialized)
            return;
        _studioCompletenessPassInitialized = true;

        EnableStudioDirectModelManipulation();
        EnableStudioAdvancedTextureTools();
        EnableStudioItemCompleteness();
    }

    private void EnableStudioDirectModelManipulation()
    {
        if (_studioBlockViewport is null || _studioElementList is null)
            return;

        _studioBlockViewport.ElementSelected += index =>
        {
            if (_studioElementList is null || index < 0 || index >= _studioElementList.Items.Count)
                return;
            _studioElementList.SelectedIndex = index;
            _studioElementList.ScrollIntoView(_studioElementList.SelectedItem);
        };
        _studioElementList.SelectionChanged += (_, _) =>
        {
            if (_studioElementList.SelectedIndex >= 0)
                _studioBlockViewport?.SelectElement(_studioElementList.SelectedIndex);
        };
        _studioBlockViewport.ElementDragDelta += ApplyStudioViewportElementDrag;

        if (_studioBlockViewport.Parent is not Grid viewportGrid)
            return;

        var toolbar = new Border
        {
            Background = Brush("#D012181C"),
            BorderBrush = Brush("#33464F"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(14, 52, 14, 14),
            Padding = new Thickness(7),
            Child = BuildStudioModelGizmoToolbar()
        };
        Panel.SetZIndex(toolbar, 12);
        viewportGrid.Children.Add(toolbar);

        foreach (var hint in FindStudioVisualChildren<TextBlock>(viewportGrid)
                     .Where(text => text.Text.StartsWith("Left drag:", StringComparison.OrdinalIgnoreCase)))
        {
            hint.Text = "Left drag: orbit · Right/middle drag: pan · wheel: zoom · choose Move/Scale axis for direct element drag";
        }
    }

    private FrameworkElement BuildStudioModelGizmoToolbar()
    {
        var root = new StackPanel();
        root.Children.Add(new TextBlock
        {
            Text = "DIRECT MODEL GIZMO",
            Foreground = Brush("#7ED9DB"),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(2, 0, 0, 6)
        });
        var row = new WrapPanel();
        AddViewportModeButton(row, "Orbit", MinecraftViewportEditMode.None, "x", 58);
        AddViewportModeButton(row, "Move X", MinecraftViewportEditMode.Move, "x", 64);
        AddViewportModeButton(row, "Move Y", MinecraftViewportEditMode.Move, "y", 64);
        AddViewportModeButton(row, "Move Z", MinecraftViewportEditMode.Move, "z", 64);
        AddViewportModeButton(row, "Scale X", MinecraftViewportEditMode.Scale, "x", 68);
        AddViewportModeButton(row, "Scale Y", MinecraftViewportEditMode.Scale, "y", 68);
        AddViewportModeButton(row, "Scale Z", MinecraftViewportEditMode.Scale, "z", 68);
        root.Children.Add(row);
        return root;
    }

    private void AddViewportModeButton(Panel row, string label, MinecraftViewportEditMode mode, string axis, double width)
    {
        var button = StudioButton(label, width);
        button.Height = 28;
        button.Margin = new Thickness(0, 0, 5, 4);
        button.FontSize = 10;
        button.Click += (_, _) =>
        {
            if (_studioBlockViewport is null)
                return;
            _studioBlockViewport.EditMode = mode;
            _studioBlockViewport.EditAxis = axis;
            SetStudioEditorStatus(
                mode == MinecraftViewportEditMode.None
                    ? "Viewport orbit mode. Right/middle drag pans the camera."
                    : $"Direct {mode.ToString().ToLowerInvariant()} mode on {axis.ToUpperInvariant()} axis. Drag an element in the viewport.",
                "#74C99A");
        };
        row.Children.Add(button);
    }

    private void ApplyStudioViewportElementDrag(int index, string axis, double delta, MinecraftViewportEditMode mode)
    {
        if (_studioEditableBlockModel is null || index < 0 || index >= _studioEditableBlockModel.Elements.Count)
            return;
        var axisIndex = axis.ToLowerInvariant() switch { "y" => 1, "z" => 2, _ => 0 };
        var snap = GetStudioElementSnap();
        var amount = SnapStudioModelNumber(delta, snap);
        if (Math.Abs(amount) < 0.000001)
            return;

        _studioSelectedElementIndex = index;
        var source = _studioEditableBlockModel.Elements[index];
        double[] from = [.. source.From];
        double[] to = [.. source.To];
        if (mode == MinecraftViewportEditMode.Move)
        {
            from[axisIndex] = SnapStudioModelNumber(from[axisIndex] + amount, snap);
            to[axisIndex] = SnapStudioModelNumber(to[axisIndex] + amount, snap);
        }
        else if (mode == MinecraftViewportEditMode.Scale)
        {
            from[axisIndex] = SnapStudioModelNumber(from[axisIndex] - amount / 2.0, snap);
            to[axisIndex] = SnapStudioModelNumber(to[axisIndex] + amount / 2.0, snap);
            if (Math.Abs(to[axisIndex] - from[axisIndex]) < snap)
                return;
        }
        else
        {
            return;
        }

        _studioEditableBlockModel.Elements[index] = CloneStudioBlockElement(source, from, to);
        _studioBlockModelModified = true;
        RefreshStudioElementList(index);
        PopulateStudioElementFields();
        RenderStudioEditableBlockModel();
        UpdateStudioModelEditState();
        _studioBlockViewport?.SelectElement(index);
        SetStudioEditorStatus($"Element {index + 1} {mode.ToString().ToLowerInvariant()}d on {axis.ToUpperInvariant()} · in-memory preview only.", "#74C99A");
    }

    private void EnableStudioAdvancedTextureTools()
    {
        if (_studioTextureUvEditor is null || _studioTextureUvEditor.Parent is not Panel inspector)
            return;

        if (_studioTextureZoomCombo is not null)
        {
            var current = _studioTextureZoomCombo.SelectedItem?.ToString() ?? "1×";
            _studioTextureZoomCombo.ItemsSource = new[] { "1×", "2×", "4×", "8×", "16×" };
            _studioTextureZoomCombo.SelectedItem = current;
        }

        _studioTextureUvEditor.TextureChanged += _ => RefreshStudioLiveTexturePreview();
        foreach (var reset in FindStudioVisualChildren<Button>(inspector)
                     .Where(button => string.Equals(button.Content?.ToString(), "Reset texture", StringComparison.OrdinalIgnoreCase)))
        {
            reset.Click += (_, _) => Dispatcher.BeginInvoke(RefreshStudioLiveTexturePreview, DispatcherPriority.Background);
        }

        inspector.Children.Add(BlockViewerSeparator());
        inspector.Children.Add(StudioSectionLabel("ADVANCED PIXEL TOOLS"));
        inspector.Children.Add(new TextBlock
        {
            Text = "Line/rectangle drawing, selection movement, clipboard-style transforms, color replacement and alpha editing. Texture edits now feed the 3D preview immediately.",
            Foreground = Brush("#91A3AC"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 12)
        });

        var tools = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        AddTextureToolButton(tools, _studioTextureUvEditor, "Line", MinecraftTextureEditTool.Line, 58);
        AddTextureToolButton(tools, _studioTextureUvEditor, "Rect", MinecraftTextureEditTool.Rectangle, 58);
        AddTextureToolButton(tools, _studioTextureUvEditor, "Select", MinecraftTextureEditTool.Selection, 64);
        inspector.Children.Add(tools);
        inspector.Children.Add(BuildStudioTextureSelectionCommands(_studioTextureUvEditor));

        _studioTextureReplaceSourceBox = BuildStudioColorBox("#FFFFFFFF");
        _studioTextureAlphaBox = StudioModelNumberBox();
        _studioTextureAlphaBox.Text = "255";
        inspector.Children.Add(BuildStudioReplaceAlphaPanel(
            _studioTextureUvEditor,
            _studioTextureReplaceSourceBox,
            _studioTextureAlphaBox,
            () => _studioTextureColorBox?.Text ?? "#FFFFFFFF"));
        inspector.Children.Add(BuildStudioMachinePalette(_studioTextureUvEditor, _studioTextureColorBox));
    }

    private void RefreshStudioLiveTexturePreview()
    {
        if (_studioBlockViewport is null)
            return;
        _studioBlockViewport.SetTextureOverrides(_studioTextureOverrides);
        RenderStudioEditableBlockModel();
    }

    private void EnableStudioItemCompleteness()
    {
        if (_studioItemCombo is null || _studioItemCombo.Parent is not Panel inspector || _studioItemBlockViewport is null)
            return;

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

        if (_studioItemBlockViewport.Parent is Grid previewGrid)
        {
            foreach (var reset in FindStudioVisualChildren<Button>(previewGrid)
                         .Where(button => string.Equals(button.Content?.ToString(), "Reset view", StringComparison.OrdinalIgnoreCase)))
            {
                reset.Click += (_, _) => QueueEnhancedItemRefresh();
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
            if (_studioItemTextureStateText is not null)
            {
                _studioItemTextureStateText.Text = "ITEM TEXTURE MODIFIED · NOT SAVED";
                _studioItemTextureStateText.Foreground = Brush("#D8B26A");
            }
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
            () => $"#{_studioItemTextureEditor.PaintColor.A:X2}{_studioItemTextureEditor.PaintColor.R:X2}{_studioItemTextureEditor.PaintColor.G:X2}{_studioItemTextureEditor.PaintColor.B:X2}"));
        inspector.Children.Add(BuildStudioMachinePalette(_studioItemTextureEditor, null));

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
            if (_studioItemTextureStateText is not null)
            {
                var modified = _studioItemTextureOverrides.ContainsKey(texturePath);
                _studioItemTextureStateText.Text = modified ? "ITEM TEXTURE MODIFIED · NOT SAVED" : "SOURCE ITEM TEXTURE";
                _studioItemTextureStateText.Foreground = Brush(modified ? "#D8B26A" : "#74C99A");
            }
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

    private static void AddTextureToolButton(Panel row, MinecraftTextureUvEditor editor, string label, MinecraftTextureEditTool tool, double width)
    {
        var button = StudioButton(label, width);
        button.Height = 30;
        button.Margin = new Thickness(0, 0, 6, 5);
        button.FontSize = 10;
        button.Click += (_, _) => editor.SetTool(tool);
        row.Children.Add(button);
    }

    private FrameworkElement BuildStudioTextureSelectionCommands(MinecraftTextureUvEditor editor)
    {
        var row = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        AddTextureCommand(row, "All", 46, editor.SelectAll);
        AddTextureCommand(row, "Copy", 54, () => editor.CopySelection());
        AddTextureCommand(row, "Paste", 54, () => editor.PasteSelection());
        AddTextureCommand(row, "Flip H", 58, () => editor.FlipSelectionHorizontal());
        AddTextureCommand(row, "Flip V", 58, () => editor.FlipSelectionVertical());
        AddTextureCommand(row, "Rotate 90°", 78, () => editor.RotateSelection90());
        return row;
    }

    private static void AddTextureCommand(Panel row, string label, double width, Action action)
    {
        var button = StudioButton(label, width);
        button.Height = 28;
        button.Margin = new Thickness(0, 0, 5, 4);
        button.FontSize = 10;
        button.Click += (_, _) => action();
        row.Children.Add(button);
    }

    private FrameworkElement BuildStudioReplaceAlphaPanel(
        MinecraftTextureUvEditor editor,
        TextBox sourceBox,
        TextBox alphaBox,
        Func<string> replacementText)
    {
        var root = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        var replaceGrid = new Grid();
        replaceGrid.ColumnDefinitions.Add(new ColumnDefinition());
        replaceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var sourcePanel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        sourcePanel.Children.Add(new TextBlock { Text = "Replace color", Foreground = Brush("#71848D"), FontSize = 9, Margin = new Thickness(1, 0, 0, 3) });
        sourcePanel.Children.Add(sourceBox);
        replaceGrid.Children.Add(sourcePanel);
        var replace = StudioButton("Replace", 75);
        replace.VerticalAlignment = VerticalAlignment.Bottom;
        replace.Click += (_, _) =>
        {
            if (!TryParseStudioTextureColor(sourceBox.Text, out var source) ||
                !TryParseStudioTextureColor(replacementText(), out var replacement))
            {
                SetStudioEditorStatus("Invalid replace color. Use #RRGGBB or #AARRGGBB.", "#E18B8B");
                return;
            }
            var count = editor.ReplaceColor(source, replacement, selectionOnly: !editor.Selection.IsEmpty);
            SetStudioEditorStatus($"Replaced {count} matching pixel(s) in memory.", "#74C99A");
        };
        Grid.SetColumn(replace, 1);
        replaceGrid.Children.Add(replace);
        root.Children.Add(replaceGrid);

        var alphaGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        alphaGrid.ColumnDefinitions.Add(new ColumnDefinition());
        alphaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var alphaPanel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        alphaPanel.Children.Add(new TextBlock { Text = "Alpha (0-255)", Foreground = Brush("#71848D"), FontSize = 9, Margin = new Thickness(1, 0, 0, 3) });
        alphaPanel.Children.Add(alphaBox);
        alphaGrid.Children.Add(alphaPanel);
        var applyAlpha = StudioButton("Apply alpha", 90);
        applyAlpha.VerticalAlignment = VerticalAlignment.Bottom;
        applyAlpha.Click += (_, _) =>
        {
            if (!byte.TryParse(alphaBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var alpha))
            {
                SetStudioEditorStatus("Alpha must be between 0 and 255.", "#E18B8B");
                return;
            }
            editor.SetSelectionAlpha(alpha);
            SetStudioEditorStatus("Alpha updated in memory.", "#74C99A");
        };
        Grid.SetColumn(applyAlpha, 1);
        alphaGrid.Children.Add(applyAlpha);
        root.Children.Add(alphaGrid);
        return root;
    }

    private static TextBox BuildStudioColorBox(string value)
    {
        var box = StudioModelNumberBox();
        box.Text = value;
        return box;
    }

    private FrameworkElement BuildStudioMachinePalette(MinecraftTextureUvEditor editor, TextBox? paintBox)
    {
        var root = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        root.Children.Add(new TextBlock
        {
            Text = "Forever Machine Palette",
            Foreground = Brush("#71848D"),
            FontSize = 9,
            Margin = new Thickness(1, 0, 0, 5)
        });
        var row = new WrapPanel();
        foreach (var (name, hex) in StudioMachinePalette)
        {
            if (!TryParseStudioTextureColor(hex, out var color))
                continue;
            var button = new Button
            {
                Width = 25,
                Height = 25,
                Margin = new Thickness(0, 0, 5, 5),
                Background = new SolidColorBrush(color),
                BorderBrush = Brush("#53636B"),
                BorderThickness = new Thickness(1),
                ToolTip = $"{name} · {hex}",
                Cursor = Cursors.Hand
            };
            button.Click += (_, _) =>
            {
                editor.SetPaintColor(color);
                if (paintBox is not null)
                    paintBox.Text = hex;
            };
            row.Children.Add(button);
        }
        root.Children.Add(row);
        return root;
    }

    private static readonly (string Name, string Hex)[] StudioMachinePalette =
    [
        ("Graphite", "#FF20282D"),
        ("Steel Dark", "#FF39464D"),
        ("Steel Mid", "#FF60717A"),
        ("Steel Highlight", "#FF9AAAB2"),
        ("Copper", "#FFA96845"),
        ("Copper Highlight", "#FFD28B62"),
        ("Status Green", "#FF54C47A"),
        ("Warning Orange", "#FFE8A44A"),
        ("Freezing Cyan", "#FF62D7E6"),
        ("Ascension Purple", "#FF9D76E8")
    ];
}
