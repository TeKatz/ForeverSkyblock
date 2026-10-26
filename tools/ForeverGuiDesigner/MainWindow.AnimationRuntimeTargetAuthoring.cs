using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17D.4 closes the Studio-side authoring gap for the stable runtime targets introduced in
/// Phase 17A and consumed by the Java renderer binder in Phase 17D.3. Runtime components reuse the
/// existing transform/visibility keyframe model and persistence format; no second timeline or target
/// resolver is introduced.
/// </summary>
public partial class MainWindow
{
    private bool _studioRuntimeTargetAuthoringInitialized;
    private bool _studioRuntimeTargetSelectorRefreshQueued;
    private bool _studioRuntimeTargetPointerDragging;
    private Point _studioRuntimeTargetPointerStart;
    private double _studioRuntimeTargetPointerLastAmount = double.NaN;
    private StudioBlockAnimationKeyframe? _studioRuntimeTargetPointerBaseFrame;

    public void EnableForeverAnimationRuntimeTargetAuthoring()
    {
        if (_studioRuntimeTargetAuthoringInitialized || !_studioAnimationStudioInitialized ||
            _studioAnimationElementCombo is null || _studioAnimationViewport is null)
            return;

        _studioRuntimeTargetAuthoringInitialized = true;

        _studioAnimationElementCombo.SelectionChanged += (_, _) =>
        {
            if (_studioAnimationUiUpdating)
                return;
            if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
                RefreshStudioRuntimeTargetTrackUi();
            UpdateStudioRuntimeTargetSelectionOverlay();
            QueueStudioRuntimeTargetPreview();
        };

        if (_studioBlockMachineCombo is not null)
            _studioBlockMachineCombo.SelectionChanged += (_, _) => QueueStudioRuntimeTargetSelectorRefresh();

        if (_studioAnimationSurface is not null)
        {
            _studioAnimationSurface.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                    QueueStudioRuntimeTargetSelectorRefresh();
            };
        }

        if (_studioAnimationTimeline is not null)
        {
            _studioAnimationTimeline.ValueChanged += (_, _) =>
            {
                if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
                    RefreshStudioRuntimeTargetTrackUi(updateFieldsFromSelection: false);
                QueueStudioRuntimeTargetPreview();
            };
        }

        if (_studioAnimationTimer is not null)
            _studioAnimationTimer.Tick += (_, _) => QueueStudioRuntimeTargetPreview();

        if (_studioRuntimeStateSummaryText is not null)
        {
            DependencyPropertyDescriptor
                .FromProperty(TextBlock.TextProperty, typeof(TextBlock))?
                .AddValueChanged(_studioRuntimeStateSummaryText, (_, _) => QueueStudioRuntimeTargetPreview());
        }

        _studioAnimationViewport.ElementSelected += StudioRuntimeTargetAuthoring_ElementSelected;
        _studioAnimationViewport.AddHandler(
            UIElement.PreviewMouseLeftButtonDownEvent,
            new MouseButtonEventHandler(StudioRuntimeTargetAuthoring_MouseLeftButtonDown),
            handledEventsToo: true);
        _studioAnimationViewport.AddHandler(
            UIElement.PreviewMouseMoveEvent,
            new MouseEventHandler(StudioRuntimeTargetAuthoring_MouseMove),
            handledEventsToo: true);
        _studioAnimationViewport.AddHandler(
            UIElement.PreviewMouseLeftButtonUpEvent,
            new MouseButtonEventHandler(StudioRuntimeTargetAuthoring_MouseLeftButtonUp),
            handledEventsToo: true);

        if (_studioAnimationKeyframeList is not null)
        {
            _studioAnimationKeyframeList.SelectionChanged += (_, _) => LoadSelectedStudioRuntimeTargetKeyframe();
            _studioAnimationKeyframeList.AddHandler(
                Keyboard.PreviewKeyDownEvent,
                new KeyEventHandler(StudioRuntimeTargetAuthoring_KeyDown),
                handledEventsToo: true);
        }

        HookStudioRuntimeTargetAuthoringButtons();
        QueueStudioRuntimeTargetSelectorRefresh();
    }

    private void QueueStudioRuntimeTargetSelectorRefresh()
    {
        if (!_studioRuntimeTargetAuthoringInitialized || _studioRuntimeTargetSelectorRefreshQueued)
            return;

        _studioRuntimeTargetSelectorRefreshQueued = true;
        _ = Dispatcher.BeginInvoke(() =>
        {
            _studioRuntimeTargetSelectorRefreshQueued = false;
            if (_studioAnimationSurface?.Visibility == Visibility.Visible)
                RefreshStudioRuntimeTargetSelector();
        }, DispatcherPriority.ContextIdle);
    }

    private void RefreshStudioRuntimeTargetSelector()
    {
        if (_studioEditableBlockModel is null || _studioAnimationElementCombo is null)
            return;

        var selectedId = (_studioAnimationElementCombo.SelectedItem as AnimationTargetDescriptor)?.TargetId.Value;
        RebuildStudioAnimationTargetRegistry();

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var jsonTargets = _studioAnimationTargetRegistry
            .JsonTargetsForModel(_studioEditableBlockModel.SourcePath)
            .ToList();
        var runtimeTargets = _studioAnimationTargetRegistry.RuntimeTargets()
            .Where(target => IsCurrentStudioRuntimeCatalogTarget(target, assetId))
            .ToList();
        var entries = jsonTargets.Concat(runtimeTargets).ToList();

        var selectedIndex = string.IsNullOrWhiteSpace(selectedId)
            ? -1
            : entries.FindIndex(entry => string.Equals(entry.TargetId.Value, selectedId, StringComparison.Ordinal));
        if (selectedIndex < 0 && _studioSelectedElementIndex >= 0 && _studioSelectedElementIndex < jsonTargets.Count)
            selectedIndex = _studioSelectedElementIndex;
        if (selectedIndex < 0 && entries.Count > 0)
            selectedIndex = 0;

        _studioAnimationUiUpdating = true;
        try
        {
            _studioAnimationElementCombo.ItemsSource = entries;
            _studioAnimationElementCombo.SelectedIndex = selectedIndex;
        }
        finally
        {
            _studioAnimationUiUpdating = false;
        }

        if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            RefreshStudioRuntimeTargetTrackUi();
        else
            RefreshStudioAnimationTrackUi();

        UpdateStudioRuntimeTargetSelectionOverlay();
        QueueStudioRuntimeTargetPreview();

        var health = DescribeStudioAnimationTargetHealth();
        SetStudioAnimationStatus(
            $"Animation target registry ready · {jsonTargets.Count} JSON target(s) · {runtimeTargets.Count} runtime target(s) for {assetId}.{health}",
            health.Contains("TARGET WARNING", StringComparison.Ordinal) ? "#D8B26A" : "#74C99A");
    }

    private static bool IsCurrentStudioRuntimeCatalogTarget(AnimationTargetDescriptor target, string assetId)
        => target.Kind == AnimationTargetKind.RuntimeRendererComponent &&
           target.RuntimeTarget is ForeverRuntimeAnimationProfile profile &&
           profile.AssetId.Equals(assetId, StringComparison.OrdinalIgnoreCase) &&
           target.Scope.Equals(profile.AssetId, StringComparison.OrdinalIgnoreCase);

    private bool TryGetSelectedStudioRuntimeAuthoringTarget(out AnimationTargetDescriptor target)
    {
        target = null!;
        var selected = GetSelectedStudioAnimationTarget();
        if (selected is null ||
            !IsCurrentStudioRuntimeCatalogTarget(selected, ResolveCurrentStudioRuntimeAssetId()))
            return false;
        target = selected;
        return true;
    }

    private StudioBlockAnimationTrack? GetStudioRuntimeTargetTrack(bool create)
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out var target) || _studioEditableBlockModel is null)
            return null;

        var existing = FindStudioAnimationTrackForTarget(target);
        if (existing is not null)
            return existing;
        if (!create)
            return null;

        var created = new StudioBlockAnimationTrack
        {
            TargetId = target.TargetId.Value,
            ModelKey = _studioEditableBlockModel.SourcePath,
            ElementIndex = -1
        };
        _studioAnimationTracks[target.TargetId.Value] = created;
        return created;
    }

    private void RefreshStudioRuntimeTargetTrackUi(bool updateFieldsFromSelection = true)
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out var target) || _studioAnimationKeyframeList is null)
            return;

        var track = GetStudioRuntimeTargetTrack(create: false);
        var keyframes = track?.Keyframes.OrderBy(keyframe => keyframe.TimeSeconds).ToList() ?? [];
        var currentTime = _studioAnimationTimeline?.Value ?? 0;
        var selectedIndex = keyframes.FindIndex(keyframe =>
            Math.Abs(keyframe.TimeSeconds - currentTime) < StudioAnimationTimeEpsilon);

        if (selectedIndex < 0 &&
            string.Equals(_studioAnimationSelectedKeyframeTargetId, target.TargetId.Value, StringComparison.Ordinal) &&
            _studioAnimationSelectedKeyframeTime is double rememberedTime)
        {
            selectedIndex = keyframes.FindIndex(keyframe =>
                Math.Abs(keyframe.TimeSeconds - rememberedTime) < StudioAnimationTimeEpsilon);
        }

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

        if (updateFieldsFromSelection && selectedIndex >= 0 && selectedIndex < keyframes.Count)
        {
            RememberStudioAnimationKeyframeSelection(keyframes[selectedIndex]);
            PopulateStudioAnimationFields(keyframes[selectedIndex]);
        }

        UpdateStudioRuntimeTargetSelectionOverlay();
    }

    private void HookStudioRuntimeTargetAuthoringButtons()
    {
        if (_studioAnimationSurface is null)
            return;

        foreach (var panel in EnumerateStudioRuntimeAuthoringLogicalChildren<WrapPanel>(_studioAnimationSurface))
        {
            var buttons = panel.Children.OfType<Button>().ToList();
            var labels = buttons.Select(button => button.Content?.ToString() ?? string.Empty).ToHashSet(StringComparer.Ordinal);

            if (labels.Contains("ADD / UPDATE"))
            {
                HookButton(buttons, "ADD / UPDATE", UpsertStudioRuntimeTargetKeyframe);
                HookButton(buttons, "Delete", DeleteSelectedStudioRuntimeTargetKeyframe);
                HookButton(buttons, "Clear track", ClearStudioRuntimeTargetTrack);
            }

            if (labels.Contains("◀ Key"))
            {
                HookButton(buttons, "◀ Key", () => SelectAdjacentStudioRuntimeTargetKeyframe(-1));
                HookButton(buttons, "Key ▶", () => SelectAdjacentStudioRuntimeTargetKeyframe(1));
                HookButton(buttons, "Copy", CopySelectedStudioRuntimeTargetKeyframe);
                HookButton(buttons, "Paste @ time", PasteStudioRuntimeTargetKeyframeAtTimeline);
                HookButton(buttons, "Move to time", MoveSelectedStudioRuntimeTargetKeyframeToEditorTime);
            }

            if (labels.Contains("− Step"))
            {
                HookButton(buttons, "− Step", () => NudgeStudioRuntimeTarget(-1));
                HookButton(buttons, "+ Step", () => NudgeStudioRuntimeTarget(1));
                HookButton(buttons, "Neutral @ time", ResetStudioRuntimeTargetAtCurrentTime);
                HookButton(buttons, "Identify", () => IdentifyStudioRuntimeTargetSelection(700));
            }
        }

        foreach (var button in EnumerateStudioRuntimeAuthoringLogicalChildren<Button>(_studioAnimationSurface))
        {
            if (string.Equals(button.Content?.ToString(), "Key visibility @ time", StringComparison.Ordinal))
                button.Click += (_, _) => KeyStudioRuntimeTargetVisibilityAtCurrentTime();
        }
    }

    private static void HookButton(IEnumerable<Button> buttons, string label, Action action)
    {
        var button = buttons.FirstOrDefault(candidate => string.Equals(candidate.Content?.ToString(), label, StringComparison.Ordinal));
        if (button is not null)
            button.Click += (_, _) => action();
    }

    private static IEnumerable<T> EnumerateStudioRuntimeAuthoringLogicalChildren<T>(DependencyObject root)
        where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is not DependencyObject dependencyObject)
                continue;
            if (dependencyObject is T match)
                yield return match;
            foreach (var descendant in EnumerateStudioRuntimeAuthoringLogicalChildren<T>(dependencyObject))
                yield return descendant;
        }
    }

    private void UpsertStudioRuntimeTargetKeyframe()
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out var target))
            return;
        var track = GetStudioRuntimeTargetTrack(create: true);
        if (track is null || !TryReadStudioAnimationFrameFields(out var frame))
            return;

        NormalizeStudioRuntimeTargetFrame(frame);
        var existing = track.Keyframes.FirstOrDefault(candidate =>
            Math.Abs(candidate.TimeSeconds - frame.TimeSeconds) < StudioAnimationTimeEpsilon);
        if (existing is null)
            track.Keyframes.Add(frame);
        else
            track.Keyframes[track.Keyframes.IndexOf(existing)] = frame;
        track.Keyframes.Sort((left, right) => left.TimeSeconds.CompareTo(right.TimeSeconds));

        if (frame.TimeSeconds > _studioAnimationDurationSeconds)
            SetStudioAnimationDuration(frame.TimeSeconds);
        RememberStudioAnimationKeyframeSelection(frame);
        SetStudioAnimationTimelineTime(frame.TimeSeconds);
        RefreshStudioRuntimeTargetTrackUi();
        QueueStudioRuntimeTargetPreview();
        SetStudioAnimationStatus(
            $"Keyframe stored at {frame.TimeSeconds:0.###}s for {DescribeStudioAnimationTarget(target)}.",
            "#74C99A");
    }

    private static void NormalizeStudioRuntimeTargetFrame(StudioBlockAnimationKeyframe frame)
    {
        // Runtime TargetId tracks own only component transform/visibility. Machine-wide state,
        // materials and special effects remain on their dedicated source-backed channels.
        frame.TextureState = string.Empty;
        frame.LightStatus = string.Empty;
        frame.SpecialEffect = string.Empty;
        frame.MachineState = StudioAnimationMachineState.Idle;
    }

    private bool TryGetSelectedStudioRuntimeTargetKeyframe(
        out StudioBlockAnimationTrack? track,
        out StudioBlockAnimationKeyframe frame,
        out int orderedIndex)
    {
        track = GetStudioRuntimeTargetTrack(create: false);
        frame = null!;
        orderedIndex = -1;
        if (track is null || _studioAnimationKeyframeList is null || _studioAnimationKeyframeList.SelectedIndex < 0)
            return false;

        var ordered = track.Keyframes.OrderBy(candidate => candidate.TimeSeconds).ToList();
        orderedIndex = _studioAnimationKeyframeList.SelectedIndex;
        if (orderedIndex < 0 || orderedIndex >= ordered.Count)
            return false;
        frame = ordered[orderedIndex];
        return true;
    }

    private void DeleteSelectedStudioRuntimeTargetKeyframe()
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out _) ||
            !TryGetSelectedStudioRuntimeTargetKeyframe(out var track, out var frame, out var orderedIndex) || track is null)
            return;

        var ordered = track.Keyframes.OrderBy(candidate => candidate.TimeSeconds).ToList();
        track.Keyframes.Remove(frame);
        ordered.Remove(frame);

        if (ordered.Count == 0)
        {
            ForgetStudioAnimationKeyframeSelection();
            RefreshStudioRuntimeTargetTrackUi();
            QueueStudioRuntimeTargetPreview();
            SetStudioAnimationStatus("Keyframe removed. The runtime component is back on its source transform.", "#74C99A");
            return;
        }

        var next = ordered[Math.Clamp(orderedIndex, 0, ordered.Count - 1)];
        RememberStudioAnimationKeyframeSelection(next);
        SetStudioAnimationTimelineTime(next.TimeSeconds);
        RefreshStudioRuntimeTargetTrackUi();
        PopulateStudioAnimationFields(next);
        QueueStudioRuntimeTargetPreview();
        SetStudioAnimationStatus($"Keyframe removed. Selected {next.TimeSeconds:0.###}s.", "#74C99A");
    }

    private void ClearStudioRuntimeTargetTrack()
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            return;
        var track = GetStudioRuntimeTargetTrack(create: false);
        if (track is null)
            return;
        track.Keyframes.Clear();
        ForgetStudioAnimationKeyframeSelection();
        RefreshStudioRuntimeTargetTrackUi();
        QueueStudioRuntimeTargetPreview();
        SetStudioAnimationStatus("Animation track cleared. Source renderer code is unchanged.", "#74C99A");
    }

    private void LoadSelectedStudioRuntimeTargetKeyframe()
    {
        if (_studioAnimationUiUpdating || !TryGetSelectedStudioRuntimeAuthoringTarget(out _) ||
            !TryGetSelectedStudioRuntimeTargetKeyframe(out _, out var frame, out _))
            return;

        RememberStudioAnimationKeyframeSelection(frame);
        PopulateStudioAnimationFields(frame);
        SetStudioAnimationTimelineTime(frame.TimeSeconds);
        QueueStudioRuntimeTargetPreview();
    }

    private void CopySelectedStudioRuntimeTargetKeyframe()
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out _) ||
            !TryGetSelectedStudioRuntimeTargetKeyframe(out _, out var frame, out _))
            return;
        _studioAnimationClipboardKeyframe = frame.Clone();
        SetStudioAnimationStatus($"Copied keyframe at {frame.TimeSeconds:0.###}s.", "#74C99A");
    }

    private void PasteStudioRuntimeTargetKeyframeAtTimeline()
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            return;
        if (_studioAnimationClipboardKeyframe is null)
        {
            SetStudioAnimationStatus("No copied keyframe is available.", "#D8B26A");
            return;
        }

        var track = GetStudioRuntimeTargetTrack(create: true);
        if (track is null)
            return;
        var time = Math.Clamp(_studioAnimationTimeline?.Value ?? 0, 0, _studioAnimationDurationSeconds);
        if (track.Keyframes.Any(candidate => Math.Abs(candidate.TimeSeconds - time) < StudioAnimationTimeEpsilon))
        {
            SetStudioAnimationStatus($"A keyframe already exists at {time:0.###}s. Paste did not overwrite it.", "#D8B26A");
            return;
        }

        var pasted = _studioAnimationClipboardKeyframe.Clone();
        pasted.TimeSeconds = time;
        NormalizeStudioRuntimeTargetFrame(pasted);
        track.Keyframes.Add(pasted);
        track.Keyframes.Sort((left, right) => left.TimeSeconds.CompareTo(right.TimeSeconds));
        RememberStudioAnimationKeyframeSelection(pasted);
        RefreshStudioRuntimeTargetTrackUi();
        PopulateStudioAnimationFields(pasted);
        QueueStudioRuntimeTargetPreview();
        SetStudioAnimationStatus($"Pasted keyframe at {time:0.###}s without overwriting existing data.", "#74C99A");
    }

    private void MoveSelectedStudioRuntimeTargetKeyframeToEditorTime()
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out _) ||
            !TryGetSelectedStudioRuntimeTargetKeyframe(out var track, out var frame, out _) || track is null)
            return;
        if (_studioAnimationTimeBox is null ||
            !TryParseStudioModelNumber(_studioAnimationTimeBox.Text, out var requestedTime) ||
            !double.IsFinite(requestedTime))
        {
            SetStudioAnimationStatus("Keyframe time must be a finite number.", "#E18B8B");
            return;
        }

        var time = Math.Max(0, requestedTime);
        if (track.Keyframes.Any(candidate =>
                !ReferenceEquals(candidate, frame) &&
                Math.Abs(candidate.TimeSeconds - time) < StudioAnimationTimeEpsilon))
        {
            SetStudioAnimationStatus($"A keyframe already exists at {time:0.###}s. Move did not overwrite it.", "#D8B26A");
            return;
        }

        frame.TimeSeconds = time;
        track.Keyframes.Sort((left, right) => left.TimeSeconds.CompareTo(right.TimeSeconds));
        if (time > _studioAnimationDurationSeconds)
            SetStudioAnimationDuration(time);
        RememberStudioAnimationKeyframeSelection(frame);
        SetStudioAnimationTimelineTime(time);
        RefreshStudioRuntimeTargetTrackUi();
        PopulateStudioAnimationFields(frame);
        QueueStudioRuntimeTargetPreview();
        SetStudioAnimationStatus($"Moved keyframe to {time:0.###}s.", "#74C99A");
    }

    private void SelectAdjacentStudioRuntimeTargetKeyframe(int direction)
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            return;
        var track = GetStudioRuntimeTargetTrack(create: false);
        if (track is null || track.Keyframes.Count == 0)
        {
            SetStudioAnimationStatus("The selected runtime target has no keyframes.", "#D8B26A");
            return;
        }

        var ordered = track.Keyframes.OrderBy(frame => frame.TimeSeconds).ToList();
        var current = _studioAnimationKeyframeList?.SelectedIndex ?? -1;
        var index = current < 0
            ? direction < 0 ? ordered.Count - 1 : 0
            : Math.Clamp(current + Math.Sign(direction), 0, ordered.Count - 1);
        var selected = ordered[index];
        RememberStudioAnimationKeyframeSelection(selected);
        SetStudioAnimationTimelineTime(selected.TimeSeconds);
        RefreshStudioRuntimeTargetTrackUi();
        PopulateStudioAnimationFields(selected);
        SetStudioAnimationStatus($"Selected keyframe at {selected.TimeSeconds:0.###}s.", "#74C99A");
    }

    private void KeyStudioRuntimeTargetVisibilityAtCurrentTime()
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            return;
        var track = GetStudioRuntimeTargetTrack(create: true);
        if (track is null)
            return;
        var time = Math.Clamp(_studioAnimationTimeline?.Value ?? 0, 0, _studioAnimationDurationSeconds);
        var frame = EvaluateStudioAnimationFrame(track, time);
        frame.TimeSeconds = time;
        frame.Visible = _studioAnimationVisibleCheck?.IsChecked != false;
        StoreStudioRuntimeTargetInteractionFrame(frame, refreshTrackList: true);
        SetStudioAnimationStatus(
            $"Visibility keyed {(frame.Visible ? "Visible" : "Hidden")} at {time:0.###}s · Step/Hold.",
            "#74C99A");
    }

    private void NudgeStudioRuntimeTarget(int direction)
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            return;
        if (_studioAnimationManipulationMode == StudioAnimationManipulationMode.Select)
        {
            SetStudioAnimationStatus("Choose Move, Rotate or Scale before using the step buttons.", "#D8B26A");
            return;
        }

        var time = _studioAnimationTimeline?.Value ?? 0;
        var frame = EvaluateStudioAnimationFrame(GetStudioRuntimeTargetTrack(create: false), time);
        frame.TimeSeconds = time;
        var step = _studioAnimationManipulationMode switch
        {
            StudioAnimationManipulationMode.Move => .25,
            StudioAnimationManipulationMode.Rotate => 5,
            StudioAnimationManipulationMode.Scale => .1,
            _ => 0
        };
        ApplyStudioAnimationInteractionDelta(frame, step * Math.Sign(direction));
        StoreStudioRuntimeTargetInteractionFrame(frame, refreshTrackList: true);
        SetStudioAnimationStatus(
            $"Auto-keyed runtime target at {time:0.###}s.",
            "#74C99A");
    }

    private void ResetStudioRuntimeTargetAtCurrentTime()
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            return;
        var time = _studioAnimationTimeline?.Value ?? 0;
        var frame = EvaluateStudioAnimationFrame(GetStudioRuntimeTargetTrack(create: false), time);
        frame.TimeSeconds = time;
        frame.Position = [0, 0, 0];
        frame.RotationDegrees = 0;
        frame.Scale = [1, 1, 1];
        StoreStudioRuntimeTargetInteractionFrame(frame, refreshTrackList: true);
        SetStudioAnimationStatus($"Neutral transform keyed at {time:0.###}s.", "#74C99A");
    }

    private void StoreStudioRuntimeTargetInteractionFrame(StudioBlockAnimationKeyframe frame, bool refreshTrackList)
    {
        var track = GetStudioRuntimeTargetTrack(create: true);
        if (track is null)
            return;

        var time = Math.Clamp(_studioAnimationTimeline?.Value ?? frame.TimeSeconds, 0, _studioAnimationDurationSeconds);
        frame.TimeSeconds = time;
        NormalizeStudioRuntimeTargetFrame(frame);
        var existing = track.Keyframes.FirstOrDefault(candidate =>
            Math.Abs(candidate.TimeSeconds - time) < StudioAnimationTimeEpsilon);
        if (existing is null)
            track.Keyframes.Add(frame);
        else
            track.Keyframes[track.Keyframes.IndexOf(existing)] = frame;
        track.Keyframes.Sort((left, right) => left.TimeSeconds.CompareTo(right.TimeSeconds));

        RememberStudioAnimationKeyframeSelection(frame);
        PopulateStudioAnimationFields(frame);
        if (refreshTrackList)
            RefreshStudioRuntimeTargetTrackUi();
        QueueStudioRuntimeTargetPreview();
        UpdateStudioRuntimeTargetSelectionOverlay();
    }

    private void StudioRuntimeTargetAuthoring_KeyDown(object sender, KeyEventArgs e)
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out _))
            return;

        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.C)
            CopySelectedStudioRuntimeTargetKeyframe();
        else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.V)
            PasteStudioRuntimeTargetKeyframeAtTimeline();
        else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.Left)
            SelectAdjacentStudioRuntimeTargetKeyframe(-1);
        else if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.Right)
            SelectAdjacentStudioRuntimeTargetKeyframe(1);
        else if (e.Key == Key.Delete)
            DeleteSelectedStudioRuntimeTargetKeyframe();
        else
            return;

        e.Handled = true;
    }

    private void StudioRuntimeTargetAuthoring_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out _) ||
            _studioAnimationManipulationMode == StudioAnimationManipulationMode.Select ||
            _studioAnimationViewport is null)
            return;

        var time = _studioAnimationTimeline?.Value ?? 0;
        _studioRuntimeTargetPointerBaseFrame = EvaluateStudioAnimationFrame(GetStudioRuntimeTargetTrack(create: false), time);
        _studioRuntimeTargetPointerBaseFrame.TimeSeconds = time;
        _studioRuntimeTargetPointerStart = e.GetPosition(_studioAnimationViewport);
        _studioRuntimeTargetPointerLastAmount = double.NaN;
        _studioRuntimeTargetPointerDragging = true;
        _studioAnimationViewport.CaptureMouse();
        e.Handled = true;
    }

    private void StudioRuntimeTargetAuthoring_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_studioRuntimeTargetPointerDragging || _studioAnimationViewport is null ||
            _studioRuntimeTargetPointerBaseFrame is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var current = e.GetPosition(_studioAnimationViewport);
        var delta = current - _studioRuntimeTargetPointerStart;
        var pixels = _studioAnimationManipulationMode switch
        {
            StudioAnimationManipulationMode.Rotate => delta.X - delta.Y * .35,
            _ when _studioAnimationManipulationAxis == "y" => -delta.Y,
            _ => delta.X
        };
        var amount = _studioAnimationManipulationMode switch
        {
            StudioAnimationManipulationMode.Move => SnapStudioAnimationInteraction(pixels / 24.0, .25),
            StudioAnimationManipulationMode.Rotate => SnapStudioAnimationInteraction(pixels * .5, 5),
            StudioAnimationManipulationMode.Scale => SnapStudioAnimationInteraction(pixels / 80.0, .1),
            _ => 0
        };
        if (double.IsFinite(_studioRuntimeTargetPointerLastAmount) &&
            Math.Abs(amount - _studioRuntimeTargetPointerLastAmount) < .000001)
            return;
        _studioRuntimeTargetPointerLastAmount = amount;

        var frame = _studioRuntimeTargetPointerBaseFrame.Clone();
        ApplyStudioAnimationInteractionDelta(frame, amount);
        StoreStudioRuntimeTargetInteractionFrame(frame, refreshTrackList: false);
        e.Handled = true;
    }

    private void StudioRuntimeTargetAuthoring_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_studioRuntimeTargetPointerDragging)
            return;

        if (_studioAnimationViewport?.IsMouseCaptured == true)
            _studioAnimationViewport.ReleaseMouseCapture();
        _studioRuntimeTargetPointerDragging = false;
        _studioRuntimeTargetPointerBaseFrame = null;
        _studioRuntimeTargetPointerLastAmount = double.NaN;
        RefreshStudioRuntimeTargetTrackUi();
        UpdateStudioRuntimeTargetSelectionOverlay();
        SetStudioAnimationStatus(
            $"Auto-keyed runtime component at {(_studioAnimationTimeline?.Value ?? 0):0.###}s.",
            "#74C99A");
        e.Handled = true;
    }

    private void StudioRuntimeTargetAuthoring_ElementSelected(int elementIndex)
    {
        if (!_studioRuntimeTargetAuthoringInitialized || _studioAnimationElementCombo is null ||
            elementIndex < _studioRuntimeTargetPreviewBaseElementCount)
            return;

        var binding = _studioRuntimeTargetPreviewBindings.FirstOrDefault(candidate =>
            elementIndex >= candidate.StartElementIndex &&
            elementIndex < candidate.StartElementIndex + candidate.ElementCount);
        if (binding is null)
        {
            // Source-only runtime geometry must never select an unrelated stable target merely because
            // its numeric viewport index happens to line up with the target combo.
            _studioAnimationUiUpdating = true;
            try { _studioAnimationElementCombo.SelectedIndex = -1; }
            finally { _studioAnimationUiUpdating = false; }
            _studioAnimationViewport?.SelectElement(-1);
            UpdateStudioRuntimeTargetSelectionOverlay();
            SetStudioAnimationStatus("This runtime primitive is source-only and has no published Stable TargetId.", "#D8B26A");
            return;
        }

        var entries = _studioAnimationElementCombo.Items.Cast<AnimationTargetDescriptor>().ToList();
        var index = entries.FindIndex(candidate =>
            candidate.TargetId.Value.Equals(binding.TargetId, StringComparison.Ordinal));
        if (index < 0)
            return;

        _studioAnimationUiUpdating = true;
        try { _studioAnimationElementCombo.SelectedIndex = index; }
        finally { _studioAnimationUiUpdating = false; }
        RefreshStudioRuntimeTargetTrackUi();
        UpdateStudioRuntimeTargetSelectionOverlay();
        SetStudioAnimationStatus($"Selected runtime target · {binding.TargetId}", "#74C99A");
    }

    private void UpdateStudioRuntimeTargetSelectionOverlay()
    {
        if (_studioAnimationSelectionOverlayText is null || !TryGetSelectedStudioRuntimeAuthoringTarget(out var target))
            return;
        _studioAnimationSelectionOverlayText.Text =
            $"SELECTED · RUNTIME · {target.RuntimeComponentKey ?? target.DisplayName} · {target.TargetId.Value}";
    }

    private void IdentifyStudioRuntimeTargetSelection(int milliseconds)
    {
        if (!TryGetSelectedStudioRuntimeAuthoringTarget(out var target))
            return;
        RenderStudioRuntimeTargetPreviewNow(_studioAnimationTimeline?.Value ?? 0);
        var binding = _studioRuntimeTargetPreviewBindings.FirstOrDefault(candidate =>
            candidate.TargetId.Equals(target.TargetId.Value, StringComparison.Ordinal));
        if (binding is null)
        {
            SetStudioAnimationStatus(
                "The selected runtime target is not emitted by the current source-backed machine state.",
                "#D8B26A");
            return;
        }
        IsolateStudioRuntimeTargetPreview(binding, milliseconds);
    }
}
