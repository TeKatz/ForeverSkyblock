using System.Windows;
using System.Windows.Controls;
using ForeverGuiDesigner.Controls;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 12 direct viewport editing. Numerical element properties remain the authoritative edit state;
/// this module adds hit-tested move/scale manipulation on top of the same in-memory model.
/// </summary>
public partial class MainWindow
{
    private bool _studioBlockDirectEditingInitialized;

    public void EnableForeverBlockDirectEditing()
    {
        if (_studioBlockDirectEditingInitialized || _studioBlockViewport is null || _studioElementList is null)
            return;
        _studioBlockDirectEditingInitialized = true;

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
}
