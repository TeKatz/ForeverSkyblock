using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 12 block model editor. All edits remain in memory until the later
/// Review/Validation/Apply phase introduces a controlled write path.
/// </summary>
public partial class MainWindow
{
    private MinecraftResolvedBlockModel? _studioSourceBlockModel;
    private MinecraftResolvedBlockModel? _studioEditableBlockModel;
    private int _studioEditableBlockstateXRotation;
    private int _studioEditableBlockstateYRotation;
    private int _studioSelectedElementIndex = -1;
    private bool _studioElementEditorUpdating;
    private bool _studioBlockModelModified;

    private ListBox? _studioElementList;
    private readonly TextBox?[] _studioElementFromBoxes = new TextBox?[3];
    private readonly TextBox?[] _studioElementToBoxes = new TextBox?[3];
    private readonly TextBox?[] _studioElementOriginBoxes = new TextBox?[3];
    private ComboBox? _studioElementSnapCombo;
    private ComboBox? _studioElementRotationAxisCombo;
    private TextBox? _studioElementRotationAngleBox;
    private CheckBox? _studioElementRescaleCheck;
    private TextBlock? _studioElementFacesText;
    private TextBlock? _studioModelEditStateText;

    private void BuildStudioBlockModelEditor(Panel inspector)
    {
        inspector.Children.Add(BlockViewerSeparator());
        inspector.Children.Add(StudioSectionLabel("ELEMENT EDITOR"));
        inspector.Children.Add(new TextBlock
        {
            Text = "Changes update the 3D preview immediately but remain in memory. Source JSON is not written in this phase.",
            Foreground = Brush("#91A3AC"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 14)
        });

        _studioModelEditStateText = new TextBlock
        {
            Text = "SOURCE STATE",
            Foreground = Brush("#74C99A"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 9)
        };
        inspector.Children.Add(_studioModelEditStateText);

        _studioElementList = new ListBox
        {
            Height = 190,
            Background = Brush("#0D1418"),
            Foreground = Brush("#D9E4E9"),
            BorderBrush = Brush("#35464F"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(3)
        };
        _studioElementList.SelectionChanged += StudioElementList_SelectionChanged;
        inspector.Children.Add(_studioElementList);

        var commandRow = new Grid { Margin = new Thickness(0, 9, 0, 14) };
        commandRow.ColumnDefinitions.Add(new ColumnDefinition());
        commandRow.ColumnDefinitions.Add(new ColumnDefinition());
        commandRow.ColumnDefinitions.Add(new ColumnDefinition());

        var duplicate = StudioButton("Duplicate", 0);
        duplicate.Margin = new Thickness(0, 0, 5, 0);
        duplicate.Click += (_, _) => DuplicateStudioBlockElement();
        Grid.SetColumn(duplicate, 0);
        commandRow.Children.Add(duplicate);

        var delete = StudioButton("Delete", 0);
        delete.Margin = new Thickness(5, 0, 5, 0);
        delete.Click += (_, _) => DeleteStudioBlockElement();
        Grid.SetColumn(delete, 1);
        commandRow.Children.Add(delete);

        var reset = StudioButton("Reset model", 0);
        reset.Margin = new Thickness(5, 0, 0, 0);
        reset.Click += (_, _) => ResetStudioBlockModelEdits();
        Grid.SetColumn(reset, 2);
        commandRow.Children.Add(reset);
        inspector.Children.Add(commandRow);

        inspector.Children.Add(BlockViewerLabel("Snap"));
        _studioElementSnapCombo = BlockViewerCombo();
        _studioElementSnapCombo.ItemsSource = new[] { "0.25", "0.5", "1" };
        _studioElementSnapCombo.SelectedItem = "0.25";
        inspector.Children.Add(_studioElementSnapCombo);

        inspector.Children.Add(BlockViewerLabel("From", 14));
        inspector.Children.Add(BuildStudioVectorEditor(_studioElementFromBoxes));
        inspector.Children.Add(BlockViewerLabel("To", 12));
        inspector.Children.Add(BuildStudioVectorEditor(_studioElementToBoxes));

        inspector.Children.Add(BlockViewerLabel("Rotation", 16));
        var rotationGrid = new Grid();
        rotationGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        rotationGrid.ColumnDefinitions.Add(new ColumnDefinition());

        _studioElementRotationAxisCombo = BlockViewerCombo();
        _studioElementRotationAxisCombo.ItemsSource = new[] { "x", "y", "z" };
        _studioElementRotationAxisCombo.SelectedItem = "y";
        _studioElementRotationAxisCombo.SelectionChanged += (_, _) => CommitStudioElementFields();
        Grid.SetColumn(_studioElementRotationAxisCombo, 0);
        rotationGrid.Children.Add(_studioElementRotationAxisCombo);

        _studioElementRotationAngleBox = StudioModelNumberBox();
        _studioElementRotationAngleBox.Margin = new Thickness(8, 0, 0, 0);
        HookStudioModelNumberBox(_studioElementRotationAngleBox);
        Grid.SetColumn(_studioElementRotationAngleBox, 1);
        rotationGrid.Children.Add(_studioElementRotationAngleBox);
        inspector.Children.Add(rotationGrid);

        inspector.Children.Add(BlockViewerLabel("Rotation origin", 12));
        inspector.Children.Add(BuildStudioVectorEditor(_studioElementOriginBoxes));

        _studioElementRescaleCheck = new CheckBox
        {
            Content = "Rescale rotated element",
            Foreground = Brush("#C7D4D9"),
            Margin = new Thickness(0, 10, 0, 0)
        };
        _studioElementRescaleCheck.Click += (_, _) => CommitStudioElementFields();
        inspector.Children.Add(_studioElementRescaleCheck);

        var clearRotation = StudioButton("Clear rotation", 130);
        clearRotation.HorizontalAlignment = HorizontalAlignment.Left;
        clearRotation.Margin = new Thickness(0, 10, 0, 0);
        clearRotation.Click += (_, _) => ClearStudioElementRotation();
        inspector.Children.Add(clearRotation);

        inspector.Children.Add(BlockViewerLabel("Faces", 16));
        _studioElementFacesText = new TextBlock
        {
            Text = "—",
            Foreground = Brush("#AFC0C7"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap
        };
        inspector.Children.Add(_studioElementFacesText);
    }

    private FrameworkElement BuildStudioVectorEditor(TextBox?[] targets)
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
            HookStudioModelNumberBox(box);
            targets[i] = box;
            panel.Children.Add(box);
            Grid.SetColumn(panel, i);
            grid.Children.Add(panel);
        }
        return grid;
    }

    private static TextBox StudioModelNumberBox()
        => new()
        {
            Height = 32,
            Padding = new Thickness(7, 5, 7, 5),
            Background = Brush("#0D1418"),
            Foreground = Brushes.White,
            BorderBrush = Brush("#35464F"),
            BorderThickness = new Thickness(1),
            VerticalContentAlignment = VerticalAlignment.Center
        };

    private void HookStudioModelNumberBox(TextBox box)
    {
        box.LostFocus += (_, _) => CommitStudioElementFields();
        box.PreviewKeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
                return;
            CommitStudioElementFields();
            e.Handled = true;
        };
    }

    private void LoadStudioBlockEditorModel(MinecraftResolvedBlockModel model, int blockstateXRotation, int blockstateYRotation)
    {
        _studioSourceBlockModel = CloneStudioBlockModel(model);
        _studioEditableBlockModel = CloneStudioBlockModel(model);
        _studioEditableBlockstateXRotation = blockstateXRotation;
        _studioEditableBlockstateYRotation = blockstateYRotation;
        _studioBlockModelModified = false;
        _studioSelectedElementIndex = _studioEditableBlockModel.Elements.Count > 0 ? 0 : -1;
        RefreshStudioElementList();
        PopulateStudioElementFields();
        RenderStudioEditableBlockModel();
        UpdateStudioModelEditState();
    }

    private void ClearStudioBlockEditorModel()
    {
        _studioSourceBlockModel = null;
        _studioEditableBlockModel = null;
        _studioSelectedElementIndex = -1;
        _studioBlockModelModified = false;
        if (_studioElementList is not null)
            _studioElementList.ItemsSource = null;
        PopulateStudioElementFields();
        UpdateStudioModelEditState();
    }

    private void StudioElementList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_studioElementEditorUpdating || _studioElementList is null)
            return;
        _studioSelectedElementIndex = _studioElementList.SelectedIndex;
        PopulateStudioElementFields();
        RenderStudioEditableBlockModel();
    }

    private void RefreshStudioElementList(int? requestedSelection = null)
    {
        if (_studioElementList is null)
            return;

        var model = _studioEditableBlockModel;
        var selected = requestedSelection ?? _studioSelectedElementIndex;
        var entries = model?.Elements.Select((element, index) => DescribeStudioBlockElement(element, index)).ToList() ?? [];

        _studioElementEditorUpdating = true;
        try
        {
            _studioElementList.ItemsSource = entries;
            if (entries.Count == 0)
                _studioElementList.SelectedIndex = -1;
            else
                _studioElementList.SelectedIndex = Math.Clamp(selected, 0, entries.Count - 1);
            _studioSelectedElementIndex = _studioElementList.SelectedIndex;
        }
        finally
        {
            _studioElementEditorUpdating = false;
        }
    }

    private static string DescribeStudioBlockElement(MinecraftModelElement element, int index)
    {
        static string V(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
        var faces = element.Faces.Count == 0 ? "no faces" : string.Join(",", element.Faces.Keys.Select(key => key[..1].ToUpperInvariant()));
        return $"{index + 1:000}  [{V(element.From[0])}, {V(element.From[1])}, {V(element.From[2])}] → [{V(element.To[0])}, {V(element.To[1])}, {V(element.To[2])}]  · {faces}";
    }

    private void PopulateStudioElementFields()
    {
        _studioElementEditorUpdating = true;
        try
        {
            var element = GetSelectedStudioBlockElement();
            var enabled = element is not null;
            foreach (var box in _studioElementFromBoxes.Concat(_studioElementToBoxes).Concat(_studioElementOriginBoxes))
            {
                if (box is not null)
                    box.IsEnabled = enabled;
            }
            if (_studioElementRotationAngleBox is not null)
                _studioElementRotationAngleBox.IsEnabled = enabled;
            if (_studioElementRotationAxisCombo is not null)
                _studioElementRotationAxisCombo.IsEnabled = enabled;
            if (_studioElementRescaleCheck is not null)
                _studioElementRescaleCheck.IsEnabled = enabled;

            if (element is null)
            {
                SetStudioVectorBoxes(_studioElementFromBoxes, null);
                SetStudioVectorBoxes(_studioElementToBoxes, null);
                SetStudioVectorBoxes(_studioElementOriginBoxes, null);
                if (_studioElementRotationAngleBox is not null)
                    _studioElementRotationAngleBox.Text = string.Empty;
                if (_studioElementFacesText is not null)
                    _studioElementFacesText.Text = "—";
                return;
            }

            SetStudioVectorBoxes(_studioElementFromBoxes, element.From);
            SetStudioVectorBoxes(_studioElementToBoxes, element.To);
            var rotation = element.Rotation;
            SetStudioVectorBoxes(_studioElementOriginBoxes, rotation?.Origin ?? new[] { 8d, 8d, 8d });
            if (_studioElementRotationAxisCombo is not null)
                _studioElementRotationAxisCombo.SelectedItem = rotation?.Axis?.ToLowerInvariant() ?? "y";
            if (_studioElementRotationAngleBox is not null)
                _studioElementRotationAngleBox.Text = FormatStudioModelNumber(rotation?.Angle ?? 0);
            if (_studioElementRescaleCheck is not null)
                _studioElementRescaleCheck.IsChecked = rotation?.Rescale ?? false;
            if (_studioElementFacesText is not null)
                _studioElementFacesText.Text = element.Faces.Count == 0
                    ? "No rendered faces"
                    : string.Join(" · ", element.Faces.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase));
        }
        finally
        {
            _studioElementEditorUpdating = false;
        }
    }

    private void CommitStudioElementFields()
    {
        if (_studioElementEditorUpdating || _studioEditableBlockModel is null ||
            _studioSelectedElementIndex < 0 || _studioSelectedElementIndex >= _studioEditableBlockModel.Elements.Count)
            return;

        if (!TryReadStudioVector(_studioElementFromBoxes, out var from) ||
            !TryReadStudioVector(_studioElementToBoxes, out var to) ||
            !TryReadStudioVector(_studioElementOriginBoxes, out var origin) ||
            _studioElementRotationAngleBox is null ||
            !TryParseStudioModelNumber(_studioElementRotationAngleBox.Text, out var angle))
        {
            SetStudioEditorStatus("Invalid numeric value. Changes were not applied.", "#E18B8B");
            return;
        }

        var snap = GetStudioElementSnap();
        for (var i = 0; i < 3; i++)
        {
            from[i] = SnapStudioModelNumber(from[i], snap);
            to[i] = SnapStudioModelNumber(to[i], snap);
            origin[i] = SnapStudioModelNumber(origin[i], snap);
        }

        var previous = _studioEditableBlockModel.Elements[_studioSelectedElementIndex];
        var axis = _studioElementRotationAxisCombo?.SelectedItem?.ToString()?.ToLowerInvariant() ?? "y";
        var rotation = Math.Abs(angle) < 0.0000001 && previous.Rotation is null
            ? null
            : new MinecraftModelElementRotation
            {
                Origin = origin,
                Axis = axis is "x" or "z" ? axis : "y",
                Angle = angle,
                Rescale = _studioElementRescaleCheck?.IsChecked == true
            };

        _studioEditableBlockModel.Elements[_studioSelectedElementIndex] = CloneStudioBlockElement(previous, from, to, rotation);
        _studioBlockModelModified = true;
        RefreshStudioElementList(_studioSelectedElementIndex);
        PopulateStudioElementFields();
        RenderStudioEditableBlockModel();
        UpdateStudioModelEditState();
        SetStudioEditorStatus("Preview updated. Model JSON is still unchanged on disk.", "#74C99A");
    }

    private void DuplicateStudioBlockElement()
    {
        if (_studioEditableBlockModel is null || GetSelectedStudioBlockElement() is not { } element)
            return;
        var insertAt = _studioSelectedElementIndex + 1;
        _studioEditableBlockModel.Elements.Insert(insertAt, CloneStudioBlockElement(element));
        _studioSelectedElementIndex = insertAt;
        _studioBlockModelModified = true;
        RefreshStudioElementList(insertAt);
        PopulateStudioElementFields();
        RenderStudioEditableBlockModel();
        UpdateStudioModelEditState();
    }

    private void DeleteStudioBlockElement()
    {
        if (_studioEditableBlockModel is null || _studioSelectedElementIndex < 0 ||
            _studioSelectedElementIndex >= _studioEditableBlockModel.Elements.Count)
            return;
        _studioEditableBlockModel.Elements.RemoveAt(_studioSelectedElementIndex);
        _studioSelectedElementIndex = Math.Min(_studioSelectedElementIndex, _studioEditableBlockModel.Elements.Count - 1);
        _studioBlockModelModified = true;
        RefreshStudioElementList(_studioSelectedElementIndex);
        PopulateStudioElementFields();
        RenderStudioEditableBlockModel();
        UpdateStudioModelEditState();
    }

    private void ClearStudioElementRotation()
    {
        if (_studioEditableBlockModel is null || GetSelectedStudioBlockElement() is not { } element)
            return;
        _studioEditableBlockModel.Elements[_studioSelectedElementIndex] = CloneStudioBlockElement(element, rotation: null, explicitlyReplaceRotation: true);
        _studioBlockModelModified = true;
        RefreshStudioElementList(_studioSelectedElementIndex);
        PopulateStudioElementFields();
        RenderStudioEditableBlockModel();
        UpdateStudioModelEditState();
    }

    private void ResetStudioBlockModelEdits()
    {
        if (_studioSourceBlockModel is null)
            return;
        _studioEditableBlockModel = CloneStudioBlockModel(_studioSourceBlockModel);
        _studioBlockModelModified = false;
        _studioSelectedElementIndex = _studioEditableBlockModel.Elements.Count > 0 ? 0 : -1;
        RefreshStudioElementList();
        PopulateStudioElementFields();
        RenderStudioEditableBlockModel();
        UpdateStudioModelEditState();
        SetStudioEditorStatus("Model reset to the repository source.", "#74C99A");
    }

    private void RenderStudioEditableBlockModel()
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) || _studioEditableBlockModel is null)
        {
            _studioBlockViewport?.ClearModel();
            return;
        }

        _studioBlockViewport?.SetModel(
            _repoRoot,
            _studioEditableBlockModel,
            _studioEditableBlockstateXRotation,
            _studioEditableBlockstateYRotation);
        SetBlockViewerText(_studioBlockElementCountText, _studioEditableBlockModel.Elements.Count.ToString(CultureInfo.InvariantCulture));
        SetBlockViewerText(_studioBlockFaceCountText, (_studioBlockViewport?.RenderedFaceCount ?? 0).ToString(CultureInfo.InvariantCulture));
    }

    private MinecraftModelElement? GetSelectedStudioBlockElement()
    {
        if (_studioEditableBlockModel is null || _studioSelectedElementIndex < 0 ||
            _studioSelectedElementIndex >= _studioEditableBlockModel.Elements.Count)
            return null;
        return _studioEditableBlockModel.Elements[_studioSelectedElementIndex];
    }

    private void UpdateStudioModelEditState()
    {
        if (_studioModelEditStateText is null)
            return;
        _studioModelEditStateText.Text = _studioBlockModelModified ? "PREVIEW MODIFIED · NOT SAVED" : "SOURCE STATE";
        _studioModelEditStateText.Foreground = Brush(_studioBlockModelModified ? "#D8B26A" : "#74C99A");
    }

    private void SetStudioEditorStatus(string message, string color)
    {
        if (_studioBlockViewerStatusText is null)
            return;
        _studioBlockViewerStatusText.Text = message;
        _studioBlockViewerStatusText.Foreground = Brush(color);
    }

    private bool TryReadStudioVector(TextBox?[] boxes, out double[] values)
    {
        values = new double[3];
        for (var i = 0; i < 3; i++)
        {
            if (boxes[i] is null || !TryParseStudioModelNumber(boxes[i]!.Text, out values[i]))
                return false;
        }
        return true;
    }

    private static bool TryParseStudioModelNumber(string value, out double result)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ||
           double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result);

    private double GetStudioElementSnap()
    {
        var value = _studioElementSnapCombo?.SelectedItem?.ToString();
        return value is not null && TryParseStudioModelNumber(value, out var snap) && snap > 0 ? snap : 0.25;
    }

    private static double SnapStudioModelNumber(double value, double snap)
        => Math.Round(value / snap, MidpointRounding.AwayFromZero) * snap;

    private static void SetStudioVectorBoxes(TextBox?[] boxes, double[]? values)
    {
        for (var i = 0; i < 3; i++)
        {
            if (boxes[i] is not null)
                boxes[i]!.Text = values is null ? string.Empty : FormatStudioModelNumber(values[i]);
        }
    }

    private static string FormatStudioModelNumber(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static MinecraftResolvedBlockModel CloneStudioBlockModel(MinecraftResolvedBlockModel source)
        => new()
        {
            ModelReference = source.ModelReference,
            SourcePath = source.SourcePath,
            ParentReference = source.ParentReference,
            AmbientOcclusion = source.AmbientOcclusion,
            Textures = new Dictionary<string, string>(source.Textures, StringComparer.OrdinalIgnoreCase),
            Elements = source.Elements.Select(CloneStudioBlockElement).ToList()
        };

    private static MinecraftModelElement CloneStudioBlockElement(
        MinecraftModelElement source,
        double[]? from = null,
        double[]? to = null,
        MinecraftModelElementRotation? rotation = null,
        bool explicitlyReplaceRotation = false)
        => new()
        {
            From = from is null ? [.. source.From] : [.. from],
            To = to is null ? [.. source.To] : [.. to],
            Rotation = explicitlyReplaceRotation
                ? rotation
                : rotation ?? (source.Rotation is null ? null : CloneStudioBlockRotation(source.Rotation)),
            Faces = source.Faces.ToDictionary(
                pair => pair.Key,
                pair => new MinecraftModelFace
                {
                    Texture = pair.Value.Texture,
                    Uv = pair.Value.Uv is null ? null : [.. pair.Value.Uv],
                    Rotation = pair.Value.Rotation,
                    CullFace = pair.Value.CullFace,
                    TintIndex = pair.Value.TintIndex
                },
                StringComparer.OrdinalIgnoreCase)
        };

    private static MinecraftModelElementRotation CloneStudioBlockRotation(MinecraftModelElementRotation source)
        => new()
        {
            Origin = [.. source.Origin],
            Axis = source.Axis,
            Angle = source.Angle,
            Rescale = source.Rescale
        };
}
