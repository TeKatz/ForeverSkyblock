using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Direct manipulation layer for Phase 17. The animation timeline remains independent from the
/// legacy GUI animation system, while this module turns the animation viewport into an authoring
/// surface instead of a read-only preview.
/// </summary>
public partial class MainWindow
{
    private enum StudioAnimationManipulationMode
    {
        Select,
        Move,
        Rotate,
        Scale
    }

    private bool _studioAnimationInteractionInitialized;
    private StudioAnimationManipulationMode _studioAnimationManipulationMode = StudioAnimationManipulationMode.Select;
    private string _studioAnimationManipulationAxis = "x";
    private TextBlock? _studioAnimationSelectionOverlayText;
    private TextBlock? _studioAnimationInteractionModeText;
    private readonly List<Button> _studioAnimationManipulationButtons = [];
    private bool _studioAnimationPointerDragging;
    private Point _studioAnimationPointerStart;
    private StudioBlockAnimationKeyframe? _studioAnimationPointerBaseFrame;
    private double _studioAnimationPointerLastAmount = double.NaN;
    private DispatcherTimer? _studioAnimationIdentifyTimer;

    public void EnableForeverAnimationStudioInteraction()
    {
        if (_studioAnimationInteractionInitialized || _studioAnimationViewport is null ||
            _studioAnimationViewport.Parent is not Grid viewportGrid)
            return;

        _studioAnimationInteractionInitialized = true;

        _studioAnimationViewport.ElementSelected += StudioAnimationInteraction_ElementSelected;
        _studioAnimationViewport.PreviewMouseLeftButtonDown += StudioAnimationInteraction_MouseLeftButtonDown;
        _studioAnimationViewport.PreviewMouseMove += StudioAnimationInteraction_MouseMove;
        _studioAnimationViewport.PreviewMouseLeftButtonUp += StudioAnimationInteraction_MouseLeftButtonUp;
        _studioAnimationViewport.LostMouseCapture += (_, _) => CancelStudioAnimationPointerDrag();

        if (_studioAnimationElementCombo is not null)
        {
            _studioAnimationElementCombo.SelectionChanged += (_, _) =>
            {
                if (_studioAnimationUiUpdating)
                    return;
                SyncStudioAnimationViewportSelection(flashSelection: true);
            };
        }

        var toolbar = new Border
        {
            Background = Brush("#E012181C"),
            BorderBrush = Brush("#3D535C"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(14, 52, 14, 14),
            Padding = new Thickness(8),
            Child = BuildStudioAnimationInteractionToolbar()
        };
        Panel.SetZIndex(toolbar, 20);
        viewportGrid.Children.Add(toolbar);

        var selectionBadge = new Border
        {
            Background = Brush("#E00D1519"),
            BorderBrush = Brush("#3E6E73"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(14, 14, 14, 54),
            Padding = new Thickness(10, 6, 10, 6),
            IsHitTestVisible = false
        };
        _studioAnimationSelectionOverlayText = new TextBlock
        {
            Text = "SELECTED · none — use Select and click a model part",
            Foreground = Brush("#D7E7EB"),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold
        };
        selectionBadge.Child = _studioAnimationSelectionOverlayText;
        Panel.SetZIndex(selectionBadge, 20);
        viewportGrid.Children.Add(selectionBadge);

        foreach (var hint in FindStudioVisualChildren<TextBlock>(viewportGrid)
                     .Where(text => text.Text.StartsWith("Scrub timeline", StringComparison.OrdinalIgnoreCase)))
        {
            hint.Text = "Select: click model part · Move/Rotate/Scale: drag in viewport · transforms auto-key at current timeline time";
        }

        SetStudioAnimationManipulationMode(StudioAnimationManipulationMode.Select, "x");
        SyncStudioAnimationViewportSelection(flashSelection: false);
    }

    private FrameworkElement BuildStudioAnimationInteractionToolbar()
    {
        var root = new StackPanel { MaxWidth = 760 };
        root.Children.Add(new TextBlock
        {
            Text = "ANIMATION TRANSFORM",
            Foreground = Brush("#7ED9DB"),
            FontSize = 9,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(2, 0, 0, 6)
        });

        var modes = new WrapPanel();
        AddStudioAnimationManipulationButton(modes, "Select", StudioAnimationManipulationMode.Select, "x", 58);
        AddStudioAnimationManipulationButton(modes, "Move X", StudioAnimationManipulationMode.Move, "x", 64);
        AddStudioAnimationManipulationButton(modes, "Move Y", StudioAnimationManipulationMode.Move, "y", 64);
        AddStudioAnimationManipulationButton(modes, "Move Z", StudioAnimationManipulationMode.Move, "z", 64);
        AddStudioAnimationManipulationButton(modes, "Rot X", StudioAnimationManipulationMode.Rotate, "x", 58);
        AddStudioAnimationManipulationButton(modes, "Rot Y", StudioAnimationManipulationMode.Rotate, "y", 58);
        AddStudioAnimationManipulationButton(modes, "Rot Z", StudioAnimationManipulationMode.Rotate, "z", 58);
        AddStudioAnimationManipulationButton(modes, "Scale X", StudioAnimationManipulationMode.Scale, "x", 68);
        AddStudioAnimationManipulationButton(modes, "Scale Y", StudioAnimationManipulationMode.Scale, "y", 68);
        AddStudioAnimationManipulationButton(modes, "Scale Z", StudioAnimationManipulationMode.Scale, "z", 68);
        root.Children.Add(modes);

        var commands = new WrapPanel { Margin = new Thickness(0, 3, 0, 0) };
        var minus = StudioButton("− Step", 66);
        minus.Height = 27;
        minus.Margin = new Thickness(0, 0, 5, 4);
        minus.ToolTip = "Nudge the selected transform backwards and create/update a keyframe at the current time";
        minus.Click += (_, _) => NudgeStudioAnimationSelection(-1);
        commands.Children.Add(minus);

        var plus = StudioButton("+ Step", 66);
        plus.Height = 27;
        plus.Margin = new Thickness(0, 0, 7, 4);
        plus.ToolTip = "Nudge the selected transform forwards and create/update a keyframe at the current time";
        plus.Click += (_, _) => NudgeStudioAnimationSelection(1);
        commands.Children.Add(plus);

        var identify = StudioButton("Identify", 72);
        identify.Height = 27;
        identify.Margin = new Thickness(0, 0, 7, 4);
        identify.ToolTip = "Briefly isolate the selected model element so it is obvious which part is being animated";
        identify.Click += (_, _) => IdentifyStudioAnimationSelection(700);
        commands.Children.Add(identify);

        var resetPose = StudioButton("Neutral @ time", 106);
        resetPose.Height = 27;
        resetPose.Margin = new Thickness(0, 0, 7, 4);
        resetPose.ToolTip = "Create/update a keyframe at the current time with neutral position, rotation and scale";
        resetPose.Click += (_, _) => ResetStudioAnimationTransformAtCurrentTime();
        commands.Children.Add(resetPose);

        _studioAnimationInteractionModeText = new TextBlock
        {
            Text = "SELECT · click a model part",
            Foreground = Brush("#9EB0B8"),
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(3, 0, 0, 4)
        };
        commands.Children.Add(_studioAnimationInteractionModeText);
        root.Children.Add(commands);
        return root;
    }

    private void AddStudioAnimationManipulationButton(
        Panel row,
        string label,
        StudioAnimationManipulationMode mode,
        string axis,
        double width)
    {
        var button = StudioButton(label, width);
        button.Height = 27;
        button.Margin = new Thickness(0, 0, 5, 4);
        button.FontSize = 10;
        button.Tag = $"{mode}:{axis}";
        button.Click += (_, _) => SetStudioAnimationManipulationMode(mode, axis);
        _studioAnimationManipulationButtons.Add(button);
        row.Children.Add(button);
    }

    private void SetStudioAnimationManipulationMode(StudioAnimationManipulationMode mode, string axis)
    {
        CancelStudioAnimationPointerDrag();
        _studioAnimationManipulationMode = mode;
        _studioAnimationManipulationAxis = axis is "y" or "z" ? axis : "x";

        foreach (var button in _studioAnimationManipulationButtons)
        {
            var active = string.Equals(button.Tag?.ToString(), $"{mode}:{_studioAnimationManipulationAxis}", StringComparison.Ordinal);
            button.Background = Brush(active ? "#286F78" : "#243139");
            button.BorderBrush = Brush(active ? "#58B9C0" : "#465760");
        }

        if (_studioAnimationViewport is not null)
        {
            _studioAnimationViewport.Cursor = mode switch
            {
                StudioAnimationManipulationMode.Move => Cursors.SizeAll,
                StudioAnimationManipulationMode.Rotate => Cursors.ScrollWE,
                StudioAnimationManipulationMode.Scale => Cursors.SizeNWSE,
                _ => Cursors.Arrow
            };
        }

        if (_studioAnimationInteractionModeText is not null)
        {
            _studioAnimationInteractionModeText.Text = mode == StudioAnimationManipulationMode.Select
                ? "SELECT · click a model part"
                : $"{mode.ToString().ToUpperInvariant()} {_studioAnimationManipulationAxis.ToUpperInvariant()} · drag in viewport · auto-key on";
        }
        SetStudioAnimationStatus(
            mode == StudioAnimationManipulationMode.Select
                ? "Selection mode. Click a visible model part to select its element."
                : $"{mode} {_studioAnimationManipulationAxis.ToUpperInvariant()} mode. Drag in the viewport to transform the selected element at the current timeline time.",
            "#74C99A");
    }

    private void StudioAnimationInteraction_ElementSelected(int index)
    {
        if (_studioAnimationElementCombo is null || index < 0 || index >= _studioAnimationElementCombo.Items.Count)
            return;

        _studioAnimationUiUpdating = true;
        try
        {
            _studioAnimationElementCombo.SelectedIndex = index;
        }
        finally
        {
            _studioAnimationUiUpdating = false;
        }

        RefreshStudioAnimationTrackUi();
        UpdateStudioAnimationSelectionOverlay();
        IdentifyStudioAnimationSelection(320);
        SetStudioAnimationStatus($"Selected element {index + 1}. Choose Move/Rotate/Scale and drag to author a keyframe.", "#74C99A");
    }

    private void SyncStudioAnimationViewportSelection(bool flashSelection)
    {
        var index = _studioAnimationElementCombo?.SelectedIndex ?? -1;
        _studioAnimationViewport?.SelectElement(index);
        UpdateStudioAnimationSelectionOverlay();
        if (flashSelection && index >= 0)
            IdentifyStudioAnimationSelection(420);
    }

    private void UpdateStudioAnimationSelectionOverlay()
    {
        if (_studioAnimationSelectionOverlayText is null)
            return;
        var index = _studioAnimationElementCombo?.SelectedIndex ?? -1;
        if (_studioEditableBlockModel is null || index < 0 || index >= _studioEditableBlockModel.Elements.Count)
        {
            _studioAnimationSelectionOverlayText.Text = "SELECTED · none — use Select and click a model part";
            return;
        }

        var element = _studioEditableBlockModel.Elements[index];
        static string V(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        var faces = element.Faces.Count == 0
            ? "no faces"
            : string.Join("/", element.Faces.Keys.Select(face => face[..1].ToUpperInvariant()));
        _studioAnimationSelectionOverlayText.Text =
            $"SELECTED {index + 1:000} · [{V(element.From[0])},{V(element.From[1])},{V(element.From[2])}] → [{V(element.To[0])},{V(element.To[1])},{V(element.To[2])}] · {faces}";
    }

    private void StudioAnimationInteraction_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_studioAnimationManipulationMode == StudioAnimationManipulationMode.Select || _studioAnimationViewport is null)
            return;

        var index = _studioAnimationElementCombo?.SelectedIndex ?? -1;
        if (_studioEditableBlockModel is null || index < 0 || index >= _studioEditableBlockModel.Elements.Count)
        {
            SetStudioAnimationStatus("Select an element first. Switch to Select and click the model part you want to animate.", "#D8B26A");
            return;
        }

        var time = _studioAnimationTimeline?.Value ?? 0;
        _studioAnimationPointerBaseFrame = EvaluateStudioAnimationFrame(GetStudioAnimationTrack(create: false), time);
        _studioAnimationPointerBaseFrame.TimeSeconds = time;
        _studioAnimationPointerStart = e.GetPosition(_studioAnimationViewport);
        _studioAnimationPointerLastAmount = double.NaN;
        _studioAnimationPointerDragging = true;
        _studioAnimationViewport.CaptureMouse();
        e.Handled = true;
    }

    private void StudioAnimationInteraction_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_studioAnimationPointerDragging || _studioAnimationViewport is null ||
            _studioAnimationPointerBaseFrame is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var current = e.GetPosition(_studioAnimationViewport);
        var delta = current - _studioAnimationPointerStart;
        var pixels = _studioAnimationManipulationMode switch
        {
            StudioAnimationManipulationMode.Rotate => delta.X - delta.Y * 0.35,
            _ when _studioAnimationManipulationAxis == "y" => -delta.Y,
            _ => delta.X
        };

        var amount = _studioAnimationManipulationMode switch
        {
            StudioAnimationManipulationMode.Move => SnapStudioAnimationInteraction(pixels / 24.0, 0.25),
            StudioAnimationManipulationMode.Rotate => SnapStudioAnimationInteraction(pixels * 0.5, 5),
            StudioAnimationManipulationMode.Scale => SnapStudioAnimationInteraction(pixels / 80.0, 0.1),
            _ => 0
        };
        if (double.IsFinite(_studioAnimationPointerLastAmount) && Math.Abs(amount - _studioAnimationPointerLastAmount) < 0.000001)
            return;
        _studioAnimationPointerLastAmount = amount;

        var frame = _studioAnimationPointerBaseFrame.Clone();
        ApplyStudioAnimationInteractionDelta(frame, amount);
        StoreStudioAnimationInteractionFrame(frame, refreshTrackList: false);
        e.Handled = true;
    }

    private void StudioAnimationInteraction_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_studioAnimationPointerDragging)
            return;
        if (_studioAnimationViewport?.IsMouseCaptured == true)
            _studioAnimationViewport.ReleaseMouseCapture();
        _studioAnimationPointerDragging = false;
        _studioAnimationPointerBaseFrame = null;
        _studioAnimationPointerLastAmount = double.NaN;
        RefreshStudioAnimationTrackUi();
        UpdateStudioAnimationSelectionOverlay();
        SetStudioAnimationStatus(
            $"Auto-keyed element {(_studioAnimationElementCombo?.SelectedIndex ?? 0) + 1} at {(_studioAnimationTimeline?.Value ?? 0):0.###}s.",
            "#74C99A");
        e.Handled = true;
    }

    private void CancelStudioAnimationPointerDrag()
    {
        _studioAnimationPointerDragging = false;
        _studioAnimationPointerBaseFrame = null;
        _studioAnimationPointerLastAmount = double.NaN;
    }

    private void NudgeStudioAnimationSelection(int direction)
    {
        if (_studioAnimationManipulationMode == StudioAnimationManipulationMode.Select)
        {
            SetStudioAnimationStatus("Choose Move, Rotate or Scale before using the step buttons.", "#D8B26A");
            return;
        }
        if (_studioAnimationElementCombo?.SelectedIndex < 0)
        {
            SetStudioAnimationStatus("Select an element first.", "#D8B26A");
            return;
        }

        var time = _studioAnimationTimeline?.Value ?? 0;
        var frame = EvaluateStudioAnimationFrame(GetStudioAnimationTrack(create: false), time);
        frame.TimeSeconds = time;
        var step = _studioAnimationManipulationMode switch
        {
            StudioAnimationManipulationMode.Move => 0.25,
            StudioAnimationManipulationMode.Rotate => 5,
            StudioAnimationManipulationMode.Scale => 0.1,
            _ => 0
        };
        ApplyStudioAnimationInteractionDelta(frame, step * Math.Sign(direction));
        StoreStudioAnimationInteractionFrame(frame, refreshTrackList: true);
    }

    private void ApplyStudioAnimationInteractionDelta(StudioBlockAnimationKeyframe frame, double amount)
    {
        var axis = _studioAnimationManipulationAxis switch { "y" => 1, "z" => 2, _ => 0 };
        switch (_studioAnimationManipulationMode)
        {
            case StudioAnimationManipulationMode.Move:
                frame.Position[axis] = Math.Clamp(frame.Position[axis] + amount, -64, 64);
                break;
            case StudioAnimationManipulationMode.Rotate:
                frame.RotationAxis = _studioAnimationManipulationAxis;
                frame.RotationDegrees = Math.Clamp(frame.RotationDegrees + amount, -3600, 3600);
                break;
            case StudioAnimationManipulationMode.Scale:
                frame.Scale[axis] = Math.Clamp(frame.Scale[axis] + amount, 0.05, 8);
                break;
        }
    }

    private void StoreStudioAnimationInteractionFrame(StudioBlockAnimationKeyframe frame, bool refreshTrackList)
    {
        var track = GetStudioAnimationTrack(create: true);
        if (track is null)
            return;

        var time = Math.Clamp(_studioAnimationTimeline?.Value ?? frame.TimeSeconds, 0, _studioAnimationDurationSeconds);
        frame.TimeSeconds = time;
        var existing = track.Keyframes.FirstOrDefault(candidate => Math.Abs(candidate.TimeSeconds - time) < 0.0001);
        if (existing is null)
            track.Keyframes.Add(frame);
        else
            track.Keyframes[track.Keyframes.IndexOf(existing)] = frame;
        track.Keyframes.Sort((a, b) => a.TimeSeconds.CompareTo(b.TimeSeconds));

        PopulateStudioAnimationFields(frame);
        RenderStudioAnimationFrame(time);
        if (refreshTrackList)
        {
            RefreshStudioAnimationTrackUi();
            PopulateStudioAnimationFields(frame);
        }
        UpdateStudioAnimationSelectionOverlay();
    }

    private void ResetStudioAnimationTransformAtCurrentTime()
    {
        if (_studioAnimationElementCombo?.SelectedIndex < 0)
        {
            SetStudioAnimationStatus("Select an element first.", "#D8B26A");
            return;
        }
        var time = _studioAnimationTimeline?.Value ?? 0;
        var frame = EvaluateStudioAnimationFrame(GetStudioAnimationTrack(create: false), time);
        frame.TimeSeconds = time;
        frame.Position = [0, 0, 0];
        frame.RotationDegrees = 0;
        frame.Scale = [1, 1, 1];
        StoreStudioAnimationInteractionFrame(frame, refreshTrackList: true);
        SetStudioAnimationStatus($"Neutral transform keyed at {time:0.###}s.", "#74C99A");
    }

    private void IdentifyStudioAnimationSelection(int milliseconds)
    {
        if (_studioAnimationViewport is null || _studioEditableBlockModel is null || string.IsNullOrWhiteSpace(_repoRoot))
            return;
        var index = _studioAnimationElementCombo?.SelectedIndex ?? -1;
        if (index < 0 || index >= _studioEditableBlockModel.Elements.Count)
            return;

        _studioAnimationIdentifyTimer?.Stop();
        var time = _studioAnimationTimeline?.Value ?? 0;
        var frame = EvaluateStudioAnimationFrame(GetStudioAnimationTrack(create: false), time);
        var preview = CloneStudioBlockModel(_studioEditableBlockModel);
        var selected = CloneStudioBlockElement(preview.Elements[index]);
        preview.Elements.Clear();
        preview.Elements.Add(selected);
        ApplyStudioAnimationInteractionFrameToElement(preview, 0, frame);

        _studioAnimationViewport.SetTextureOverrides(_studioTextureOverrides);
        _studioAnimationViewport.SetModel(_repoRoot, preview, _studioEditableBlockstateXRotation, _studioEditableBlockstateYRotation);
        _studioAnimationViewport.SelectElement(0);

        _studioAnimationIdentifyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(Math.Clamp(milliseconds, 150, 1500)) };
        _studioAnimationIdentifyTimer.Tick += (_, _) =>
        {
            _studioAnimationIdentifyTimer?.Stop();
            RenderStudioAnimationFrame(_studioAnimationTimeline?.Value ?? time);
            SyncStudioAnimationViewportSelection(flashSelection: false);
        };
        _studioAnimationIdentifyTimer.Start();
    }

    private static void ApplyStudioAnimationInteractionFrameToElement(
        MinecraftResolvedBlockModel preview,
        int elementIndex,
        StudioBlockAnimationKeyframe frame)
    {
        if (!frame.Visible || elementIndex < 0 || elementIndex >= preview.Elements.Count)
        {
            if (!frame.Visible && elementIndex >= 0 && elementIndex < preview.Elements.Count)
                preview.Elements.RemoveAt(elementIndex);
            return;
        }

        var source = preview.Elements[elementIndex];
        var center = new[]
        {
            (source.From[0] + source.To[0]) / 2.0,
            (source.From[1] + source.To[1]) / 2.0,
            (source.From[2] + source.To[2]) / 2.0
        };
        var from = new double[3];
        var to = new double[3];
        for (var i = 0; i < 3; i++)
        {
            from[i] = center[i] + (source.From[i] - center[i]) * frame.Scale[i] + frame.Position[i];
            to[i] = center[i] + (source.To[i] - center[i]) * frame.Scale[i] + frame.Position[i];
        }

        MinecraftModelElementRotation? rotation;
        if (Math.Abs(frame.RotationDegrees) < 0.0001)
        {
            rotation = source.Rotation is null ? null : CloneStudioBlockRotation(source.Rotation);
        }
        else if (source.Rotation is not null && source.Rotation.Axis.Equals(frame.RotationAxis, StringComparison.OrdinalIgnoreCase))
        {
            rotation = new MinecraftModelElementRotation
            {
                Origin =
                [
                    source.Rotation.Origin[0] + frame.Position[0],
                    source.Rotation.Origin[1] + frame.Position[1],
                    source.Rotation.Origin[2] + frame.Position[2]
                ],
                Axis = source.Rotation.Axis,
                Angle = source.Rotation.Angle + frame.RotationDegrees,
                Rescale = source.Rotation.Rescale
            };
        }
        else
        {
            rotation = new MinecraftModelElementRotation
            {
                Origin = [center[0] + frame.Position[0], center[1] + frame.Position[1], center[2] + frame.Position[2]],
                Axis = frame.RotationAxis,
                Angle = frame.RotationDegrees,
                Rescale = false
            };
        }
        preview.Elements[elementIndex] = CloneStudioBlockElement(source, from, to, rotation, explicitlyReplaceRotation: true);
    }

    private static double SnapStudioAnimationInteraction(double value, double snap)
        => Math.Round(value / snap, MidpointRounding.AwayFromZero) * snap;
}
