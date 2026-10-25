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
/// Phase 13 UV + texture editing surface. Model-face and texture changes stay in memory;
/// repository writes remain reserved for the later Review / Validate / Apply phase.
/// </summary>
public partial class MainWindow
{
    private bool _studioUvTextureEditorInitialized;
    private bool _studioFaceEditorUpdating;
    private ComboBox? _studioFaceCombo;
    private ComboBox? _studioFaceTextureCombo;
    private ComboBox? _studioFaceRotationCombo;
    private readonly TextBox?[] _studioFaceUvBoxes = new TextBox?[4];
    private TextBlock? _studioFaceUvModeText;
    private TextBlock? _studioResolvedTextureText;
    private TextBlock? _studioTextureEditStateText;
    private TextBox? _studioTextureColorBox;
    private ComboBox? _studioTextureZoomCombo;
    private MinecraftTextureUvEditor? _studioTextureUvEditor;
    private readonly Dictionary<string, BitmapSource> _studioTextureOverrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _studioTextureModifiedPaths = new(StringComparer.OrdinalIgnoreCase);

    public void EnableForeverUvTextureEditor()
    {
        if (_studioUvTextureEditorInitialized || _studioElementList?.Parent is not Panel inspector)
            return;
        _studioUvTextureEditorInitialized = true;

        if (_studioBlockViewerStatusText is UIElement status && inspector.Children.Contains(status))
            inspector.Children.Remove(status);

        BuildStudioUvTextureEditor(inspector);

        inspector.Children.Add(BlockViewerSeparator());
        if (_studioBlockViewerStatusText is UIElement restoredStatus)
            inspector.Children.Add(restoredStatus);

        _studioElementList.SelectionChanged += (_, _) => QueueStudioFaceEditorRefresh();
        RefreshStudioFaceEditor();
    }

    private void BuildStudioUvTextureEditor(Panel inspector)
    {
        inspector.Children.Add(BlockViewerSeparator());
        inspector.Children.Add(StudioSectionLabel("UV & TEXTURE EDITOR"));
        inspector.Children.Add(new TextBlock
        {
            Text = "Edit the selected model face, move or resize its UV rectangle, and paint the resolved texture. All changes remain in memory.",
            Foreground = Brush("#91A3AC"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 14)
        });

        inspector.Children.Add(BlockViewerLabel("Face"));
        _studioFaceCombo = BlockViewerCombo();
        _studioFaceCombo.SelectionChanged += (_, _) =>
        {
            if (!_studioFaceEditorUpdating)
                LoadStudioSelectedFace();
        };
        inspector.Children.Add(_studioFaceCombo);

        inspector.Children.Add(BlockViewerLabel("Texture slot", 12));
        _studioFaceTextureCombo = BlockViewerCombo();
        _studioFaceTextureCombo.SelectionChanged += (_, _) =>
        {
            if (!_studioFaceEditorUpdating)
                CommitStudioFaceTextureReference();
        };
        inspector.Children.Add(_studioFaceTextureCombo);

        _studioResolvedTextureText = new TextBlock
        {
            Text = "—",
            Foreground = Brush("#71848D"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        };
        inspector.Children.Add(_studioResolvedTextureText);

        inspector.Children.Add(BlockViewerLabel("UV", 14));
        inspector.Children.Add(BuildStudioUvBoxes());

        var uvCommandRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 9, 0, 0)
        };
        var defaultUv = StudioButton("Use default UV", 125);
        defaultUv.Margin = new Thickness(0, 0, 8, 0);
        defaultUv.Click += (_, _) => UseStudioDefaultFaceUv();
        uvCommandRow.Children.Add(defaultUv);

        _studioFaceUvModeText = new TextBlock
        {
            Text = "—",
            Foreground = Brush("#81939C"),
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center
        };
        uvCommandRow.Children.Add(_studioFaceUvModeText);
        inspector.Children.Add(uvCommandRow);

        inspector.Children.Add(BlockViewerLabel("Face rotation", 14));
        _studioFaceRotationCombo = BlockViewerCombo();
        _studioFaceRotationCombo.ItemsSource = new[] { "0°", "90°", "180°", "270°" };
        _studioFaceRotationCombo.SelectionChanged += (_, _) =>
        {
            if (!_studioFaceEditorUpdating)
                CommitStudioFaceRotation();
        };
        inspector.Children.Add(_studioFaceRotationCombo);

        inspector.Children.Add(BlockViewerLabel("Texture canvas", 16));
        _studioTextureUvEditor = new MinecraftTextureUvEditor
        {
            Margin = new Thickness(0, 0, 0, 10),
            UvSnap = 0.25
        };
        _studioTextureUvEditor.UvChanged += StudioTextureUvEditor_UvChanged;
        _studioTextureUvEditor.TextureChanged += StudioTextureUvEditor_TextureChanged;
        _studioTextureUvEditor.ColorPicked += StudioTextureUvEditor_ColorPicked;
        inspector.Children.Add(_studioTextureUvEditor);

        var toolRow = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        AddStudioTextureToolButton(toolRow, "UV", MinecraftTextureEditTool.Uv, 55);
        AddStudioTextureToolButton(toolRow, "Pencil", MinecraftTextureEditTool.Pencil, 72);
        AddStudioTextureToolButton(toolRow, "Eraser", MinecraftTextureEditTool.Eraser, 72);
        AddStudioTextureToolButton(toolRow, "Fill", MinecraftTextureEditTool.Fill, 55);
        AddStudioTextureToolButton(toolRow, "Picker", MinecraftTextureEditTool.Eyedropper, 68);
        inspector.Children.Add(toolRow);

        var textureSettings = new Grid();
        textureSettings.ColumnDefinitions.Add(new ColumnDefinition());
        textureSettings.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        var colorPanel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        colorPanel.Children.Add(new TextBlock
        {
            Text = "Paint color",
            Foreground = Brush("#71848D"),
            FontSize = 9,
            Margin = new Thickness(1, 0, 0, 3)
        });
        _studioTextureColorBox = StudioModelNumberBox();
        _studioTextureColorBox.Text = "#FFFFFFFF";
        _studioTextureColorBox.LostFocus += (_, _) => ApplyStudioTexturePaintColor();
        _studioTextureColorBox.PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;
            ApplyStudioTexturePaintColor();
            e.Handled = true;
        };
        colorPanel.Children.Add(_studioTextureColorBox);
        Grid.SetColumn(colorPanel, 0);
        textureSettings.Children.Add(colorPanel);

        var zoomPanel = new StackPanel();
        zoomPanel.Children.Add(new TextBlock
        {
            Text = "Zoom",
            Foreground = Brush("#71848D"),
            FontSize = 9,
            Margin = new Thickness(1, 0, 0, 3)
        });
        _studioTextureZoomCombo = BlockViewerCombo();
        _studioTextureZoomCombo.ItemsSource = new[] { "1×", "2×", "4×", "8×" };
        _studioTextureZoomCombo.SelectedItem = "1×";
        _studioTextureZoomCombo.SelectionChanged += (_, _) => ApplyStudioTextureZoom();
        zoomPanel.Children.Add(_studioTextureZoomCombo);
        Grid.SetColumn(zoomPanel, 1);
        textureSettings.Children.Add(zoomPanel);
        inspector.Children.Add(textureSettings);

        var textureCommandRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 10, 0, 0)
        };
        var resetTexture = StudioButton("Reset texture", 120);
        resetTexture.Margin = new Thickness(0, 0, 10, 0);
        resetTexture.Click += (_, _) => ResetStudioTextureEdits();
        textureCommandRow.Children.Add(resetTexture);

        _studioTextureEditStateText = new TextBlock
        {
            Text = "SOURCE TEXTURE",
            Foreground = Brush("#74C99A"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        textureCommandRow.Children.Add(_studioTextureEditStateText);
        inspector.Children.Add(textureCommandRow);
    }

    private FrameworkElement BuildStudioUvBoxes()
    {
        var grid = new Grid();
        for (var i = 0; i < 4; i++)
            grid.ColumnDefinitions.Add(new ColumnDefinition());

        var labels = new[] { "U1", "V1", "U2", "V2" };
        for (var i = 0; i < 4; i++)
        {
            var panel = new StackPanel { Margin = new Thickness(i == 0 ? 0 : 4, 0, i == 3 ? 0 : 4, 0) };
            panel.Children.Add(new TextBlock
            {
                Text = labels[i],
                Foreground = Brush("#71848D"),
                FontSize = 9,
                Margin = new Thickness(1, 0, 0, 3)
            });
            var box = StudioModelNumberBox();
            box.LostFocus += (_, _) => CommitStudioFaceUvFromFields();
            box.PreviewKeyDown += (_, e) =>
            {
                if (e.Key != Key.Enter)
                    return;
                CommitStudioFaceUvFromFields();
                e.Handled = true;
            };
            _studioFaceUvBoxes[i] = box;
            panel.Children.Add(box);
            Grid.SetColumn(panel, i);
            grid.Children.Add(panel);
        }
        return grid;
    }

    private void AddStudioTextureToolButton(Panel panel, string label, MinecraftTextureEditTool tool, double width)
    {
        var button = StudioButton(label, width);
        button.Margin = new Thickness(0, 0, 7, 0);
        button.Click += (_, _) =>
        {
            _studioTextureUvEditor?.SetTool(tool);
            SetStudioEditorStatus($"Texture tool: {label}.", "#74C99A");
        };
        panel.Children.Add(button);
    }

    private void QueueStudioFaceEditorRefresh()
        => Dispatcher.BeginInvoke(RefreshStudioFaceEditor, DispatcherPriority.Background);

    private void RefreshStudioFaceEditor()
    {
        if (!_studioUvTextureEditorInitialized || _studioFaceCombo is null)
            return;

        var element = GetSelectedStudioBlockElement();
        var previous = _studioFaceCombo.SelectedItem?.ToString();
        var faces = element?.Faces.Keys
            .OrderBy(StudioFaceSortKey)
            .ThenBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        _studioFaceEditorUpdating = true;
        try
        {
            _studioFaceCombo.ItemsSource = faces;
            _studioFaceCombo.SelectedItem = previous is not null && faces.Contains(previous, StringComparer.OrdinalIgnoreCase)
                ? faces.First(value => value.Equals(previous, StringComparison.OrdinalIgnoreCase))
                : faces.FirstOrDefault();
        }
        finally
        {
            _studioFaceEditorUpdating = false;
        }

        LoadStudioSelectedFace();
    }

    private void LoadStudioSelectedFace()
    {
        if (_studioFaceCombo is null || _studioFaceTextureCombo is null || _studioFaceRotationCombo is null)
            return;

        var element = GetSelectedStudioBlockElement();
        var direction = _studioFaceCombo.SelectedItem?.ToString();
        if (element is null || string.IsNullOrWhiteSpace(direction) || !element.Faces.TryGetValue(direction, out var face))
        {
            ClearStudioFaceEditor();
            return;
        }

        var uv = face.Uv is { Length: >= 4 } ? [.. face.Uv] : GetStudioDefaultFaceUv(direction, element);
        var textureOptions = (_studioEditableBlockModel?.Textures.Keys.Select(key => "#" + key) ?? [])
            .Append(face.Texture)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _studioFaceEditorUpdating = true;
        try
        {
            _studioFaceTextureCombo.ItemsSource = textureOptions;
            _studioFaceTextureCombo.SelectedItem = textureOptions.FirstOrDefault(value => value.Equals(face.Texture, StringComparison.OrdinalIgnoreCase));
            SetStudioUvBoxes(uv);
            if (_studioFaceUvModeText is not null)
            {
                _studioFaceUvModeText.Text = face.Uv is null ? "DEFAULT UV" : "EXPLICIT UV";
                _studioFaceUvModeText.Foreground = Brush(face.Uv is null ? "#81939C" : "#D8B26A");
            }
            _studioFaceRotationCombo.SelectedItem = $"{NormalizeStudioFaceRotation(face.Rotation)}°";
        }
        finally
        {
            _studioFaceEditorUpdating = false;
        }

        LoadStudioResolvedTexture(face.Texture, uv);
    }

    private void ClearStudioFaceEditor()
    {
        _studioFaceEditorUpdating = true;
        try
        {
            if (_studioFaceTextureCombo is not null)
                _studioFaceTextureCombo.ItemsSource = null;
            SetStudioUvBoxes(null);
            if (_studioFaceRotationCombo is not null)
                _studioFaceRotationCombo.SelectedIndex = -1;
            if (_studioFaceUvModeText is not null)
                _studioFaceUvModeText.Text = "—";
            if (_studioResolvedTextureText is not null)
                _studioResolvedTextureText.Text = "—";
        }
        finally
        {
            _studioFaceEditorUpdating = false;
        }
        _studioTextureUvEditor?.LoadTexture(null, null, [0, 0, 16, 16]);
        UpdateStudioTextureEditState(null);
    }

    private void LoadStudioResolvedTexture(string textureReference, double[] uv)
    {
        if (_studioTextureUvEditor is null || string.IsNullOrWhiteSpace(_repoRoot) || _studioEditableBlockModel is null)
            return;
        var path = MinecraftBlockModelPreviewService.ResolveTexturePath(_repoRoot, _studioEditableBlockModel, textureReference);
        BitmapSource? inMemory = null;
        if (path is not null)
            _studioTextureOverrides.TryGetValue(path, out inMemory);

        _studioTextureUvEditor.LoadTexture(path, inMemory, uv);
        if (_studioResolvedTextureText is not null)
        {
            _studioResolvedTextureText.Text = path is null
                ? "Texture could not be resolved inside the selected repository."
                : Path.GetRelativePath(_repoRoot, path).Replace('\\', '/');
            _studioResolvedTextureText.Foreground = Brush(path is null ? "#D8B26A" : "#71848D");
        }
        UpdateStudioTextureEditState(path);
    }

    private void CommitStudioFaceUvFromFields()
    {
        if (_studioFaceEditorUpdating || !TryReadStudioFaceUv(out var uv))
            return;
        for (var i = 0; i < 4; i++)
            uv[i] = Math.Clamp(SnapStudioModelNumber(uv[i], 0.25), 0, 16);
        CommitStudioFaceUv(uv);
    }

    private void StudioTextureUvEditor_UvChanged(double[] uv)
    {
        if (_studioFaceEditorUpdating)
            return;
        _studioFaceEditorUpdating = true;
        try
        {
            SetStudioUvBoxes(uv);
        }
        finally
        {
            _studioFaceEditorUpdating = false;
        }
        CommitStudioFaceUv(uv);
    }

    private void CommitStudioFaceUv(double[] uv)
    {
        if (!TryGetStudioSelectedFace(out var element, out var direction, out var face))
            return;
        element.Faces[direction] = CloneStudioFace(face, uv: [.. uv], replaceUv: true);
        MarkStudioFaceModelChanged();
        if (_studioFaceUvModeText is not null)
        {
            _studioFaceUvModeText.Text = "EXPLICIT UV";
            _studioFaceUvModeText.Foreground = Brush("#D8B26A");
        }
        _studioTextureUvEditor?.SetUv(uv);
    }

    private void UseStudioDefaultFaceUv()
    {
        if (!TryGetStudioSelectedFace(out var element, out var direction, out var face))
            return;
        element.Faces[direction] = CloneStudioFace(face, uv: null, replaceUv: true);
        var uv = GetStudioDefaultFaceUv(direction, element);
        _studioFaceEditorUpdating = true;
        try
        {
            SetStudioUvBoxes(uv);
        }
        finally
        {
            _studioFaceEditorUpdating = false;
        }
        _studioTextureUvEditor?.SetUv(uv);
        if (_studioFaceUvModeText is not null)
        {
            _studioFaceUvModeText.Text = "DEFAULT UV";
            _studioFaceUvModeText.Foreground = Brush("#81939C");
        }
        MarkStudioFaceModelChanged();
    }

    private void CommitStudioFaceTextureReference()
    {
        if (!TryGetStudioSelectedFace(out var element, out var direction, out var face))
            return;
        var texture = _studioFaceTextureCombo?.SelectedItem?.ToString();
        if (string.IsNullOrWhiteSpace(texture) || texture.Equals(face.Texture, StringComparison.Ordinal))
            return;
        element.Faces[direction] = CloneStudioFace(face, texture: texture);
        MarkStudioFaceModelChanged();
        LoadStudioSelectedFace();
    }

    private void CommitStudioFaceRotation()
    {
        if (!TryGetStudioSelectedFace(out var element, out var direction, out var face))
            return;
        var text = _studioFaceRotationCombo?.SelectedItem?.ToString()?.TrimEnd('°');
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rotation))
            return;
        rotation = NormalizeStudioFaceRotation(rotation);
        if (rotation == face.Rotation)
            return;
        element.Faces[direction] = CloneStudioFace(face, rotation: rotation);
        MarkStudioFaceModelChanged();
    }

    private void MarkStudioFaceModelChanged()
    {
        _studioBlockModelModified = true;
        RenderStudioEditableBlockModel();
        UpdateStudioModelEditState();
        SetStudioEditorStatus("UV/model-face preview updated. Repository JSON is unchanged.", "#74C99A");
    }

    private void StudioTextureUvEditor_TextureChanged(BitmapSource bitmap)
    {
        var path = _studioTextureUvEditor?.TexturePath;
        if (string.IsNullOrWhiteSpace(path))
            return;
        _studioTextureOverrides[path] = bitmap;
        _studioTextureModifiedPaths.Add(path);
        UpdateStudioTextureEditState(path);
        SetStudioEditorStatus("Texture pixels modified in memory. Source PNG is unchanged.", "#D8B26A");
    }

    private void StudioTextureUvEditor_ColorPicked(Color color)
    {
        if (_studioTextureColorBox is null)
            return;
        _studioTextureColorBox.Text = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        _studioTextureUvEditor?.SetPaintColor(color);
    }

    private void ApplyStudioTexturePaintColor()
    {
        if (_studioTextureColorBox is null || !TryParseStudioTextureColor(_studioTextureColorBox.Text, out var color))
        {
            SetStudioEditorStatus("Invalid paint color. Use #RRGGBB or #AARRGGBB.", "#E18B8B");
            return;
        }
        _studioTextureColorBox.Text = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        _studioTextureUvEditor?.SetPaintColor(color);
    }

    private void ApplyStudioTextureZoom()
    {
        var text = _studioTextureZoomCombo?.SelectedItem?.ToString()?.TrimEnd('×');
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var zoom))
            _studioTextureUvEditor?.SetZoom(zoom);
    }

    private void ResetStudioTextureEdits()
    {
        var path = _studioTextureUvEditor?.TexturePath;
        if (string.IsNullOrWhiteSpace(path))
            return;
        _studioTextureOverrides.Remove(path);
        _studioTextureModifiedPaths.Remove(path);
        _studioTextureUvEditor?.ResetTextureFromDisk();
        UpdateStudioTextureEditState(path);
        SetStudioEditorStatus("Texture reset to the repository PNG.", "#74C99A");
    }

    private void UpdateStudioTextureEditState(string? path)
    {
        if (_studioTextureEditStateText is null)
            return;
        var modified = path is not null && _studioTextureModifiedPaths.Contains(path);
        _studioTextureEditStateText.Text = modified ? "TEXTURE MODIFIED · NOT SAVED" : "SOURCE TEXTURE";
        _studioTextureEditStateText.Foreground = Brush(modified ? "#D8B26A" : "#74C99A");
    }

    private bool TryGetStudioSelectedFace(
        out MinecraftModelElement element,
        out string direction,
        out MinecraftModelFace face)
    {
        element = null!;
        direction = string.Empty;
        face = null!;
        var selectedElement = GetSelectedStudioBlockElement();
        var selectedDirection = _studioFaceCombo?.SelectedItem?.ToString();
        if (selectedElement is null || string.IsNullOrWhiteSpace(selectedDirection) ||
            !selectedElement.Faces.TryGetValue(selectedDirection, out var selectedFace))
            return false;
        element = selectedElement;
        direction = selectedDirection;
        face = selectedFace;
        return true;
    }

    private bool TryReadStudioFaceUv(out double[] uv)
    {
        uv = new double[4];
        for (var i = 0; i < 4; i++)
        {
            if (_studioFaceUvBoxes[i] is null || !TryParseStudioModelNumber(_studioFaceUvBoxes[i]!.Text, out uv[i]))
            {
                SetStudioEditorStatus("Invalid UV value. Changes were not applied.", "#E18B8B");
                return false;
            }
        }
        return true;
    }

    private void SetStudioUvBoxes(double[]? uv)
    {
        for (var i = 0; i < 4; i++)
        {
            if (_studioFaceUvBoxes[i] is not null)
                _studioFaceUvBoxes[i]!.Text = uv is null ? string.Empty : FormatStudioModelNumber(uv[i]);
        }
    }

    private static MinecraftModelFace CloneStudioFace(
        MinecraftModelFace source,
        string? texture = null,
        double[]? uv = null,
        bool replaceUv = false,
        int? rotation = null)
        => new()
        {
            Texture = texture ?? source.Texture,
            Uv = replaceUv ? uv : source.Uv is null ? null : [.. source.Uv],
            Rotation = rotation ?? source.Rotation,
            CullFace = source.CullFace,
            TintIndex = source.TintIndex
        };

    private static double[] GetStudioDefaultFaceUv(string direction, MinecraftModelElement element)
    {
        var x1 = Math.Min(element.From[0], element.To[0]);
        var y1 = Math.Min(element.From[1], element.To[1]);
        var z1 = Math.Min(element.From[2], element.To[2]);
        var x2 = Math.Max(element.From[0], element.To[0]);
        var y2 = Math.Max(element.From[1], element.To[1]);
        var z2 = Math.Max(element.From[2], element.To[2]);
        return direction.ToLowerInvariant() switch
        {
            "down" => [x1, 16 - z2, x2, 16 - z1],
            "up" => [x1, z1, x2, z2],
            "north" => [16 - x2, 16 - y2, 16 - x1, 16 - y1],
            "south" => [x1, 16 - y2, x2, 16 - y1],
            "west" => [z1, 16 - y2, z2, 16 - y1],
            "east" => [16 - z2, 16 - y2, 16 - z1, 16 - y1],
            _ => [0, 0, 16, 16]
        };
    }

    private static int NormalizeStudioFaceRotation(int rotation)
    {
        var normalized = ((rotation % 360) + 360) % 360;
        return normalized switch
        {
            < 45 => 0,
            < 135 => 90,
            < 225 => 180,
            < 315 => 270,
            _ => 0
        };
    }

    private static int StudioFaceSortKey(string direction)
        => direction.ToLowerInvariant() switch
        {
            "north" => 0,
            "east" => 1,
            "south" => 2,
            "west" => 3,
            "up" => 4,
            "down" => 5,
            _ => 6
        };

    private static bool TryParseStudioTextureColor(string value, out Color color)
    {
        color = Colors.White;
        var text = value.Trim().TrimStart('#');
        if (text.Length == 6 && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            color = Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
            return true;
        }
        if (text.Length == 8 && uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var argb))
        {
            color = Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
            return true;
        }
        return false;
    }
}
