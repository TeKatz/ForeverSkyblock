using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17B transform-timeline stability layer.
///
/// This layer deliberately stays on Position / Rotation / Scale and timeline operations. The
/// non-transform fields that already exist on the in-memory keyframe model are preserved, but no
/// Phase 17C authoring behavior is added here.
/// </summary>
public partial class MainWindow
{
    private const double StudioAnimationTimeEpsilon = 0.0001;

    private bool _studioAnimationTimelineStabilityInitialized;
    private StudioBlockAnimationKeyframe? _studioAnimationClipboardKeyframe;
    private string? _studioAnimationSelectedKeyframeTargetId;
    private double? _studioAnimationSelectedKeyframeTime;

    public void EnableForeverAnimationTimelineStability()
    {
        if (_studioAnimationTimelineStabilityInitialized || _studioAnimationKeyframeList is null)
            return;

        _studioAnimationTimelineStabilityInitialized = true;
        _studioAnimationKeyframeList.SelectionChanged += (_, _) => RememberStudioAnimationKeyframeSelectionFromUi();
        _studioAnimationKeyframeList.PreviewKeyDown += StudioAnimationKeyframeList_PreviewKeyDown;

        if (_studioAnimationTimeBox?.Parent is Panel parent)
        {
            var index = parent.Children.IndexOf(_studioAnimationTimeBox);
            if (index >= 0)
                parent.Children.Insert(index + 1, BuildStudioAnimationTimelineCommands());
        }
    }

    private FrameworkElement BuildStudioAnimationTimelineCommands()
    {
        var panel = new WrapPanel { Margin = new Thickness(0, 7, 0, 2) };

        var previous = StudioButton("◀ Key", 70);
        previous.Height = 28;
        previous.Margin = new Thickness(0, 0, 5, 5);
        previous.ToolTip = "Select the previous keyframe on this target";
        previous.Click += (_, _) => SelectAdjacentStudioAnimationKeyframe(-1);
        panel.Children.Add(previous);

        var next = StudioButton("Key ▶", 70);
        next.Height = 28;
        next.Margin = new Thickness(0, 0, 8, 5);
        next.ToolTip = "Select the next keyframe on this target";
        next.Click += (_, _) => SelectAdjacentStudioAnimationKeyframe(1);
        panel.Children.Add(next);

        var copy = StudioButton("Copy", 66);
        copy.Height = 28;
        copy.Margin = new Thickness(0, 0, 5, 5);
        copy.ToolTip = "Copy the selected keyframe (Ctrl+C)";
        copy.Click += (_, _) => CopySelectedStudioAnimationKeyframe();
        panel.Children.Add(copy);

        var paste = StudioButton("Paste @ time", 104);
        paste.Height = 28;
        paste.Margin = new Thickness(0, 0, 5, 5);
        paste.ToolTip = "Paste the copied keyframe at the current timeline time (Ctrl+V)";
        paste.Click += (_, _) => PasteStudioAnimationKeyframeAtTimeline();
        panel.Children.Add(paste);

        var move = StudioButton("Move to time", 104);
        move.Height = 28;
        move.Margin = new Thickness(0, 0, 0, 5);
        move.ToolTip = "Move the selected keyframe to the time shown above. Existing keyframes are never overwritten silently.";
        move.Click += (_, _) => MoveSelectedStudioAnimationKeyframeToEditorTime();
        panel.Children.Add(move);

        return panel;
    }

    private void StudioAnimationKeyframeList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.C)
        {
            CopySelectedStudioAnimationKeyframe();
            e.Handled = true;
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.V)
        {
            PasteStudioAnimationKeyframeAtTimeline();
            e.Handled = true;
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.Left)
        {
            SelectAdjacentStudioAnimationKeyframe(-1);
            e.Handled = true;
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.Right)
        {
            SelectAdjacentStudioAnimationKeyframe(1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete)
        {
            DeleteSelectedStudioAnimationKeyframeStable();
            e.Handled = true;
        }
    }

    private void RefreshStudioAnimationTrackUiStable()
    {
        var track = GetStudioAnimationTrack(create: false);
        var keyframes = track?.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).ToList() ?? [];
        var selectedTargetId = GetSelectedStudioAnimationTarget()?.TargetId.Value;
        var currentTime = _studioAnimationTimeline?.Value ?? 0;

        var selectedIndex = -1;
        if (track is not null && keyframes.Count > 0)
        {
            // Operations performed at the playhead should keep/select that exact keyframe. This is
            // especially important for viewport auto-keying, which refreshes the list afterwards.
            selectedIndex = keyframes.FindIndex(keyframe =>
                Math.Abs(keyframe.TimeSeconds - currentTime) < StudioAnimationTimeEpsilon);

            if (selectedIndex < 0 &&
                !string.IsNullOrWhiteSpace(selectedTargetId) &&
                string.Equals(selectedTargetId, _studioAnimationSelectedKeyframeTargetId, StringComparison.Ordinal) &&
                _studioAnimationSelectedKeyframeTime is double rememberedTime)
            {
                selectedIndex = keyframes.FindIndex(keyframe =>
                    Math.Abs(keyframe.TimeSeconds - rememberedTime) < StudioAnimationTimeEpsilon);
            }
        }

        if (_studioAnimationKeyframeList is not null)
        {
            _studioAnimationUiUpdating = true;
            try
            {
                _studioAnimationKeyframeList.ItemsSource = null;
                _studioAnimationKeyframeList.ItemsSource = keyframes.Select(DescribeStudioAnimationKeyframe).ToList();
                _studioAnimationKeyframeList.SelectedIndex = selectedIndex;
            }
            finally
            {
                _studioAnimationUiUpdating = false;
            }
        }

        if (selectedIndex >= 0 && selectedIndex < keyframes.Count && !string.IsNullOrWhiteSpace(selectedTargetId))
        {
            _studioAnimationSelectedKeyframeTargetId = selectedTargetId;
            _studioAnimationSelectedKeyframeTime = keyframes[selectedIndex].TimeSeconds;
        }
        else if (!string.Equals(selectedTargetId, _studioAnimationSelectedKeyframeTargetId, StringComparison.Ordinal))
        {
            ForgetStudioAnimationKeyframeSelection();
        }

        RenderStudioAnimationFrame(currentTime);
    }

    private void RememberStudioAnimationKeyframeSelectionFromUi()
    {
        if (_studioAnimationUiUpdating ||
            !TryGetSelectedStudioAnimationKeyframe(out _, out var keyframe, out _))
            return;

        RememberStudioAnimationKeyframeSelection(keyframe);
    }

    private void RememberStudioAnimationKeyframeSelection(StudioBlockAnimationKeyframe keyframe)
    {
        _studioAnimationSelectedKeyframeTargetId = GetSelectedStudioAnimationTarget()?.TargetId.Value;
        _studioAnimationSelectedKeyframeTime = keyframe.TimeSeconds;
    }

    private void ForgetStudioAnimationKeyframeSelection()
    {
        _studioAnimationSelectedKeyframeTargetId = null;
        _studioAnimationSelectedKeyframeTime = null;
    }

    private bool TryGetSelectedStudioAnimationKeyframe(
        out StudioBlockAnimationTrack? track,
        out StudioBlockAnimationKeyframe keyframe,
        out int orderedIndex)
    {
        track = GetStudioAnimationTrack(create: false);
        keyframe = null!;
        orderedIndex = -1;
        if (track is null || _studioAnimationKeyframeList is null || _studioAnimationKeyframeList.SelectedIndex < 0)
            return false;

        var ordered = track.Keyframes.OrderBy(candidate => candidate.TimeSeconds).ToList();
        orderedIndex = _studioAnimationKeyframeList.SelectedIndex;
        if (orderedIndex < 0 || orderedIndex >= ordered.Count)
            return false;

        keyframe = ordered[orderedIndex];
        return true;
    }

    private void SelectStudioAnimationKeyframeAtTime(double time, bool movePlayhead = false)
    {
        var track = GetStudioAnimationTrack(create: false);
        if (track is null || _studioAnimationKeyframeList is null)
            return;

        var ordered = track.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).ToList();
        var index = ordered.FindIndex(keyframe => Math.Abs(keyframe.TimeSeconds - time) < StudioAnimationTimeEpsilon);
        if (index < 0)
            return;

        RememberStudioAnimationKeyframeSelection(ordered[index]);
        _studioAnimationUiUpdating = true;
        try
        {
            _studioAnimationKeyframeList.SelectedIndex = index;
        }
        finally
        {
            _studioAnimationUiUpdating = false;
        }

        PopulateStudioAnimationFields(ordered[index]);
        if (movePlayhead)
            SetStudioAnimationTimelineTime(ordered[index].TimeSeconds);
    }

    private void SelectAdjacentStudioAnimationKeyframe(int direction)
    {
        var track = GetStudioAnimationTrack(create: false);
        if (track is null || track.Keyframes.Count == 0)
        {
            SetStudioAnimationStatus("The selected target has no keyframes.", "#D8B26A");
            return;
        }

        var ordered = track.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).ToList();
        var current = _studioAnimationKeyframeList?.SelectedIndex ?? -1;
        var next = current < 0
            ? direction < 0 ? ordered.Count - 1 : 0
            : Math.Clamp(current + Math.Sign(direction), 0, ordered.Count - 1);

        SelectStudioAnimationKeyframeAtTime(ordered[next].TimeSeconds, movePlayhead: true);
        SetStudioAnimationStatus($"Selected keyframe at {ordered[next].TimeSeconds:0.###}s.", "#74C99A");
    }

    private void CopySelectedStudioAnimationKeyframe()
    {
        if (!TryGetSelectedStudioAnimationKeyframe(out _, out var keyframe, out _))
        {
            SetStudioAnimationStatus("Select a keyframe before copying it.", "#D8B26A");
            return;
        }

        _studioAnimationClipboardKeyframe = keyframe.Clone();
        SetStudioAnimationStatus($"Copied keyframe at {keyframe.TimeSeconds:0.###}s.", "#74C99A");
    }

    private void PasteStudioAnimationKeyframeAtTimeline()
    {
        if (_studioAnimationClipboardKeyframe is null)
        {
            SetStudioAnimationStatus("No copied keyframe is available.", "#D8B26A");
            return;
        }

        var track = GetStudioAnimationTrack(create: true);
        if (track is null)
        {
            SetStudioAnimationStatus("Select an animation target before pasting a keyframe.", "#D8B26A");
            return;
        }

        var time = Math.Clamp(_studioAnimationTimeline?.Value ?? 0, 0, _studioAnimationDurationSeconds);
        if (track.Keyframes.Any(candidate => Math.Abs(candidate.TimeSeconds - time) < StudioAnimationTimeEpsilon))
        {
            SetStudioAnimationStatus($"A keyframe already exists at {time:0.###}s. Paste did not overwrite it.", "#D8B26A");
            return;
        }

        var pasted = _studioAnimationClipboardKeyframe.Clone();
        pasted.TimeSeconds = time;
        track.Keyframes.Add(pasted);
        track.Keyframes.Sort((left, right) => left.TimeSeconds.CompareTo(right.TimeSeconds));
        RememberStudioAnimationKeyframeSelection(pasted);
        RefreshStudioAnimationTrackUiStable();
        SelectStudioAnimationKeyframeAtTime(time);
        PopulateStudioAnimationFields(pasted);
        SetStudioAnimationStatus($"Pasted keyframe at {time:0.###}s without overwriting existing data.", "#74C99A");
    }

    private void MoveSelectedStudioAnimationKeyframeToEditorTime()
    {
        if (!TryGetSelectedStudioAnimationKeyframe(out var track, out var keyframe, out _ ) || track is null)
        {
            SetStudioAnimationStatus("Select a keyframe before moving it.", "#D8B26A");
            return;
        }

        if (_studioAnimationTimeBox is null ||
            !TryParseStudioModelNumber(_studioAnimationTimeBox.Text, out var requestedTime) ||
            !double.IsFinite(requestedTime))
        {
            SetStudioAnimationStatus("Keyframe time must be a finite number.", "#E18B8B");
            return;
        }

        var time = Math.Max(0, requestedTime);
        if (Math.Abs(keyframe.TimeSeconds - time) < StudioAnimationTimeEpsilon)
        {
            SetStudioAnimationStatus("The keyframe is already at that time.", "#91A3AC");
            return;
        }

        if (track.Keyframes.Any(candidate =>
                !ReferenceEquals(candidate, keyframe) &&
                Math.Abs(candidate.TimeSeconds - time) < StudioAnimationTimeEpsilon))
        {
            SetStudioAnimationStatus($"A keyframe already exists at {time:0.###}s. Move did not overwrite it.", "#D8B26A");
            return;
        }

        keyframe.TimeSeconds = time;
        track.Keyframes.Sort((left, right) => left.TimeSeconds.CompareTo(right.TimeSeconds));
        if (time > _studioAnimationDurationSeconds)
            SetStudioAnimationDuration(time);

        RememberStudioAnimationKeyframeSelection(keyframe);
        SetStudioAnimationTimelineTime(time);
        RefreshStudioAnimationTrackUiStable();
        SelectStudioAnimationKeyframeAtTime(time);
        PopulateStudioAnimationFields(keyframe);
        SetStudioAnimationStatus($"Moved keyframe to {time:0.###}s.", "#74C99A");
    }

    private void DeleteSelectedStudioAnimationKeyframeStable()
    {
        if (!TryGetSelectedStudioAnimationKeyframe(out var track, out var keyframe, out var orderedIndex) || track is null)
            return;

        var ordered = track.Keyframes.OrderBy(candidate => candidate.TimeSeconds).ToList();
        track.Keyframes.Remove(keyframe);
        ordered.Remove(keyframe);

        if (ordered.Count == 0)
        {
            ForgetStudioAnimationKeyframeSelection();
            RefreshStudioAnimationTrackUiStable();
            SetStudioAnimationStatus("Keyframe removed. The target is back on its base transform.", "#74C99A");
            return;
        }

        var nextIndex = Math.Clamp(orderedIndex, 0, ordered.Count - 1);
        var next = ordered[nextIndex];
        RememberStudioAnimationKeyframeSelection(next);
        SetStudioAnimationTimelineTime(next.TimeSeconds);
        RefreshStudioAnimationTrackUiStable();
        SelectStudioAnimationKeyframeAtTime(next.TimeSeconds);
        SetStudioAnimationStatus($"Keyframe removed. Selected {next.TimeSeconds:0.###}s.", "#74C99A");
    }

    private static StudioBlockAnimationKeyframe EvaluateStudioAnimationTransformTimeline(
        StudioBlockAnimationTrack? track,
        double time)
    {
        var currentTime = Math.Max(0, time);
        if (track is null || track.Keyframes.Count == 0)
            return CreateStudioAnimationBaseFrame(currentTime);

        var ordered = track.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).ToList();
        var exact = ordered.FirstOrDefault(keyframe =>
            Math.Abs(keyframe.TimeSeconds - currentTime) < StudioAnimationTimeEpsilon);
        if (exact is not null)
        {
            var result = exact.Clone();
            result.TimeSeconds = currentTime;
            return result;
        }

        if (currentTime < ordered[0].TimeSeconds)
        {
            var right = ordered[0];
            if (right.TimeSeconds <= StudioAnimationTimeEpsilon)
                return right.Clone();

            var left = CreateStudioAnimationBaseFrame(0);
            var raw = Math.Clamp(currentTime / right.TimeSeconds, 0, 1);
            var amount = ApplyStudioAnimationInterpolation(right.Interpolation, raw);
            return InterpolateStudioAnimationTransform(left, right, amount, currentTime);
        }

        if (currentTime > ordered[^1].TimeSeconds)
        {
            var result = ordered[^1].Clone();
            result.TimeSeconds = currentTime;
            return result;
        }

        var rightIndex = ordered.FindIndex(keyframe => keyframe.TimeSeconds > currentTime);
        if (rightIndex <= 0)
            return CreateStudioAnimationBaseFrame(currentTime);

        var leftFrame = ordered[rightIndex - 1];
        var rightFrame = ordered[rightIndex];
        var span = Math.Max(StudioAnimationTimeEpsilon, rightFrame.TimeSeconds - leftFrame.TimeSeconds);
        var rawAmount = Math.Clamp((currentTime - leftFrame.TimeSeconds) / span, 0, 1);
        var interpolatedAmount = ApplyStudioAnimationInterpolation(leftFrame.Interpolation, rawAmount);
        return InterpolateStudioAnimationTransform(leftFrame, rightFrame, interpolatedAmount, currentTime);
    }

    private static StudioBlockAnimationKeyframe CreateStudioAnimationBaseFrame(double time)
        => new()
        {
            TimeSeconds = time,
            Position = [0d, 0d, 0d],
            RotationAxis = "y",
            RotationDegrees = 0,
            Scale = [1d, 1d, 1d]
        };

    private static double ApplyStudioAnimationInterpolation(StudioAnimationInterpolation interpolation, double raw)
        => interpolation switch
        {
            StudioAnimationInterpolation.Step => 0,
            StudioAnimationInterpolation.Linear => raw,
            _ => raw * raw * (3 - 2 * raw)
        };

    private static StudioBlockAnimationKeyframe InterpolateStudioAnimationTransform(
        StudioBlockAnimationKeyframe left,
        StudioBlockAnimationKeyframe right,
        double amount,
        double time)
    {
        var result = left.Clone();
        result.TimeSeconds = time;
        for (var axis = 0; axis < 3; axis++)
        {
            result.Position[axis] = Lerp(left.Position[axis], right.Position[axis], amount);
            result.Scale[axis] = Lerp(left.Scale[axis], right.Scale[axis], amount);
        }

        if (string.Equals(left.RotationAxis, right.RotationAxis, StringComparison.OrdinalIgnoreCase))
        {
            result.RotationAxis = left.RotationAxis;
            result.RotationDegrees = Lerp(left.RotationDegrees, right.RotationDegrees, amount);
        }
        else
        {
            // A single axis+angle representation cannot continuously blend different axes without
            // introducing quaternions into the data model. Hold the left axis until the exact right
            // keyframe rather than producing a mathematically misleading intermediate transform.
            result.RotationAxis = left.RotationAxis;
            result.RotationDegrees = left.RotationDegrees;
        }

        return result;
    }

    private void RestoreStudioAnimationBasePose()
    {
        if (_studioAnimationViewport is null || _studioEditableBlockModel is null || string.IsNullOrWhiteSpace(_repoRoot))
            return;

        _studioAnimationViewport.SetTextureOverrides(_studioTextureOverrides);
        _studioAnimationViewport.SetModel(
            _repoRoot,
            _studioEditableBlockModel,
            _studioEditableBlockstateXRotation,
            _studioEditableBlockstateYRotation);

        if (GetSelectedStudioAnimationTarget()?.ElementIndex is int elementIndex &&
            elementIndex >= 0 && elementIndex < _studioEditableBlockModel.Elements.Count)
            _studioAnimationViewport.SelectElement(elementIndex);

        SetStudioAnimationStatus("Stopped · source-model base pose restored.", "#74C99A");
    }
}
