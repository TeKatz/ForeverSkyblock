using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17C.2 discrete animation channels.
///
/// Visibility stays target-local and is authored on the existing stable JSON TargetId track.
/// Light/status state is machine-wide, so it deliberately uses a separate source-backed runtime
/// status timeline instead of the legacy free-text LightStatus field on an arbitrary JSON element.
/// Status channels use deterministic Step/Hold semantics: before the first state keyframe the
/// Phase-17C.1 base selection applies; from a keyframe onward its value is held until the next one.
/// </summary>
public partial class MainWindow
{
    private sealed class StudioRuntimeStatusKeyframe
    {
        public double TimeSeconds { get; set; }
        public required string OptionId { get; set; }
    }

    private bool _studioAnimationDiscreteChannelsInitialized;
    private ComboBox? _studioRuntimeStatusAxisCombo;
    private ComboBox? _studioRuntimeStatusValueCombo;
    private ListBox? _studioRuntimeStatusKeyframeList;
    private TextBlock? _studioRuntimeStatusEffectiveText;
    private string? _studioRuntimeStatusSelectedAxisId;
    private readonly Dictionary<string, List<StudioRuntimeStatusKeyframe>> _studioRuntimeStatusTracks =
        new(StringComparer.OrdinalIgnoreCase);

    public void EnableForeverAnimationDiscreteChannels()
    {
        if (_studioAnimationDiscreteChannelsInitialized || !_studioRuntimeStateStudioInitialized)
            return;

        _studioAnimationDiscreteChannelsInitialized = true;
        InstallStudioVisibilityChannelUi();
        InstallStudioRuntimeStatusTimelineUi();

        // The generic MachineState/LightStatus keyframe fields pre-date the source-backed runtime
        // state system. Preserve their data for compatibility but stop presenting them as active
        // authoring controls; 17C.2 owns actual renderer status through the semantic timeline below.
        if (_studioAnimationMachineStateCombo is not null)
        {
            _studioAnimationMachineStateCombo.IsEnabled = false;
            _studioAnimationMachineStateCombo.ToolTip =
                "Legacy preview metadata. Use the source-backed Light / Status Timeline in Phase 17C.2.";
        }
        if (_studioAnimationLightStatusBox is not null)
        {
            _studioAnimationLightStatusBox.IsReadOnly = true;
            _studioAnimationLightStatusBox.IsEnabled = false;
            _studioAnimationLightStatusBox.ToolTip =
                "Legacy preview metadata. Runtime light/status is authored through the source-backed Phase 17C.2 timeline.";
        }

        if (_studioBlockMachineCombo is not null)
            _studioBlockMachineCombo.SelectionChanged += (_, _) => QueueStudioRuntimeStatusTimelineInspectorRefresh();

        if (_studioAnimationTimeline is not null)
        {
            _studioAnimationTimeline.ValueChanged += (_, _) =>
            {
                RefreshStudioRuntimeStatusTimelineReadout();
                QueueStudioVisibilitySelectionSync();

                if (_studioAnimationPlaying)
                    return; // Playback is finalized by the timer handler after the 17C.1 queue.

                if (_studioAnimationUiUpdating)
                {
                    QueueExplicitStudioRuntimeStatusPreview();
                    return;
                }

                PrepareStudioRuntimeStatusForQueuedPreview();
            };
        }

        if (_studioAnimationTimer is not null)
        {
            _studioAnimationTimer.Tick += (_, _) =>
            {
                if (!_studioAnimationPlaying)
                    return;
                PrepareStudioRuntimeStatusForQueuedPreview();
                QueueStudioVisibilitySelectionSync();
            };
        }

        if (_studioAnimationSurface is not null)
        {
            _studioAnimationSurface.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                {
                    QueueStudioRuntimeStatusTimelineInspectorRefresh();
                    QueueStudioVisibilitySelectionSync();
                }
            };
        }

        RefreshStudioRuntimeStatusTimelineInspector();
        QueueStudioVisibilitySelectionSync();
    }

    private void InstallStudioVisibilityChannelUi()
    {
        if (_studioAnimationVisibleCheck?.Parent is not StackPanel visiblePanel)
            return;

        var keyVisibility = StudioButton("Key visibility @ time", 154);
        keyVisibility.Height = 27;
        keyVisibility.Margin = new Thickness(0, 7, 0, 0);
        keyVisibility.FontSize = 10;
        keyVisibility.ToolTip =
            "Create/update a keyframe at the current playhead while preserving the evaluated transform. Visibility uses Step/Hold semantics.";
        keyVisibility.Click += (_, _) => KeyStudioAnimationVisibilityAtCurrentTime();
        visiblePanel.Children.Add(keyVisibility);

        visiblePanel.Children.Add(new TextBlock
        {
            Text = "Discrete · Step/Hold",
            Foreground = Brush("#71848D"),
            FontSize = 9,
            Margin = new Thickness(1, 4, 0, 0)
        });
    }

    private void KeyStudioAnimationVisibilityAtCurrentTime()
    {
        var target = GetSelectedStudioAnimationTarget();
        if (target?.Kind != AnimationTargetKind.JsonModelElement)
        {
            SetStudioAnimationStatus("Select a JSON model element before keying visibility.", "#D8B26A");
            return;
        }

        var track = GetStudioAnimationTrack(create: true);
        if (track is null)
            return;

        var time = Math.Clamp(_studioAnimationTimeline?.Value ?? 0, 0, _studioAnimationDurationSeconds);
        var frame = EvaluateStudioAnimationFrame(track, time);
        frame.TimeSeconds = time;
        frame.Visible = _studioAnimationVisibleCheck?.IsChecked != false;
        StoreStudioAnimationInteractionFrame(frame, refreshTrackList: true);
        SelectStudioAnimationKeyframeAtTime(time);
        QueueStudioVisibilitySelectionSync();
        QueueStudioRuntimeStatePreview();
        SetStudioAnimationStatus(
            $"Visibility keyed {((frame.Visible) ? "Visible" : "Hidden")} at {time:0.###}s · Step/Hold.",
            "#74C99A");
    }

    private void InstallStudioRuntimeStatusTimelineUi()
    {
        if (_studioRuntimeStateSummaryText?.Parent is not StackPanel runtimeRoot)
            return;

        var section = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        section.Children.Add(BlockViewerSeparator());
        section.Children.Add(StudioSectionLabel("LIGHT / STATUS TIMELINE · PHASE 17C.2"));
        section.Children.Add(new TextBlock
        {
            Text = "Machine-wide source status is keyed separately from JSON transforms. Only real renderer status/activity axes are offered. Values switch discretely and hold until the next state keyframe.",
            Foreground = Brush("#8FA1AA"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 10)
        });

        var selectorGrid = new Grid();
        selectorGrid.ColumnDefinitions.Add(new ColumnDefinition());
        selectorGrid.ColumnDefinitions.Add(new ColumnDefinition());

        var axisPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        axisPanel.Children.Add(new TextBlock
        {
            Text = "Status channel",
            Foreground = Brush("#71848D"),
            FontSize = 9,
            Margin = new Thickness(1, 0, 0, 3)
        });
        _studioRuntimeStatusAxisCombo = BlockViewerCombo();
        _studioRuntimeStatusAxisCombo.DisplayMemberPath = nameof(ForeverRuntimeStateAxis.DisplayName);
        _studioRuntimeStatusAxisCombo.SelectionChanged += (_, _) =>
        {
            if (_studioRuntimeStatusAxisCombo.SelectedItem is ForeverRuntimeStateAxis axis)
                _studioRuntimeStatusSelectedAxisId = axis.Id;
            RefreshStudioRuntimeStatusAxisUi(selectEffectiveValue: true);
        };
        axisPanel.Children.Add(_studioRuntimeStatusAxisCombo);
        selectorGrid.Children.Add(axisPanel);

        var valuePanel = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        valuePanel.Children.Add(new TextBlock
        {
            Text = "Keyed value",
            Foreground = Brush("#71848D"),
            FontSize = 9,
            Margin = new Thickness(1, 0, 0, 3)
        });
        _studioRuntimeStatusValueCombo = BlockViewerCombo();
        _studioRuntimeStatusValueCombo.DisplayMemberPath = nameof(ForeverRuntimeStateOption.DisplayName);
        valuePanel.Children.Add(_studioRuntimeStatusValueCombo);
        Grid.SetColumn(valuePanel, 1);
        selectorGrid.Children.Add(valuePanel);
        section.Children.Add(selectorGrid);

        _studioRuntimeStatusEffectiveText = new TextBlock
        {
            Foreground = Brush("#71848D"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 6)
        };
        section.Children.Add(_studioRuntimeStatusEffectiveText);

        _studioRuntimeStatusKeyframeList = new ListBox
        {
            Height = 92,
            Background = Brush("#0D1418"),
            Foreground = Brush("#D9E4E9"),
            BorderBrush = Brush("#35464F"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 4, 0, 8)
        };
        _studioRuntimeStatusKeyframeList.SelectionChanged += (_, _) => LoadSelectedStudioRuntimeStatusKeyframe();
        section.Children.Add(_studioRuntimeStatusKeyframeList);

        var commands = new WrapPanel();
        var upsert = StudioPrimaryButton("KEY STATUS @ TIME", 132);
        upsert.Height = 29;
        upsert.Margin = new Thickness(0, 0, 6, 5);
        upsert.Click += (_, _) => UpsertStudioRuntimeStatusKeyframe();
        commands.Children.Add(upsert);

        var delete = StudioButton("Delete", 72);
        delete.Height = 29;
        delete.Margin = new Thickness(0, 0, 6, 5);
        delete.Click += (_, _) => DeleteSelectedStudioRuntimeStatusKeyframe();
        commands.Children.Add(delete);

        var clear = StudioButton("Clear channel", 102);
        clear.Height = 29;
        clear.Margin = new Thickness(0, 0, 0, 5);
        clear.Click += (_, _) => ClearStudioRuntimeStatusTrack();
        commands.Children.Add(clear);
        section.Children.Add(commands);

        var insertAt = runtimeRoot.Children.IndexOf(_studioRuntimeStateSummaryText) + 1;
        runtimeRoot.Children.Insert(Math.Clamp(insertAt, 0, runtimeRoot.Children.Count), section);
    }

    private static bool IsStudioRuntimeStatusAxis(ForeverRuntimeStateAxis axis)
        => axis.Id.Equals("status", StringComparison.OrdinalIgnoreCase) ||
           axis.Id.Equals("activity", StringComparison.OrdinalIgnoreCase) ||
           axis.Id.Equals("freezing-status", StringComparison.OrdinalIgnoreCase);

    private static string StudioRuntimeStatusTrackKey(string assetId, string axisId)
        => $"{assetId.Trim().ToLowerInvariant()}|{axisId.Trim().ToLowerInvariant()}";

    private List<StudioRuntimeStatusKeyframe>? GetStudioRuntimeStatusTrack(
        string assetId,
        string axisId,
        bool create)
    {
        var key = StudioRuntimeStatusTrackKey(assetId, axisId);
        if (_studioRuntimeStatusTracks.TryGetValue(key, out var track))
            return track;
        if (!create)
            return null;
        track = [];
        _studioRuntimeStatusTracks[key] = track;
        return track;
    }

    private void QueueStudioRuntimeStatusTimelineInspectorRefresh()
    {
        if (!_studioAnimationDiscreteChannelsInitialized)
            return;

        _ = Dispatcher.BeginInvoke(() =>
        {
            RefreshStudioRuntimeStatusTimelineInspector();
            QueueExplicitStudioRuntimeStatusPreview();
        }, DispatcherPriority.ContextIdle);
    }

    private void RefreshStudioRuntimeStatusTimelineInspector()
    {
        if (_studioRuntimeStatusAxisCombo is null)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var profile = ForeverRuntimeStateCatalog.ForAsset(assetId);
        var axes = profile.Axes.Where(IsStudioRuntimeStatusAxis).ToList();

        _studioRuntimeStatusAxisCombo.ItemsSource = null;
        _studioRuntimeStatusAxisCombo.ItemsSource = axes;
        _studioRuntimeStatusAxisCombo.IsEnabled = axes.Count > 0;

        if (axes.Count == 0)
        {
            _studioRuntimeStatusAxisCombo.SelectedIndex = -1;
            if (_studioRuntimeStatusValueCombo is not null)
            {
                _studioRuntimeStatusValueCombo.ItemsSource = null;
                _studioRuntimeStatusValueCombo.IsEnabled = false;
            }
            if (_studioRuntimeStatusKeyframeList is not null)
                _studioRuntimeStatusKeyframeList.ItemsSource = null;
            if (_studioRuntimeStatusEffectiveText is not null)
                _studioRuntimeStatusEffectiveText.Text = "This machine has no source-backed light/status axis to key.";
            return;
        }

        var selected = axes.FirstOrDefault(axis =>
                           axis.Id.Equals(_studioRuntimeStatusSelectedAxisId, StringComparison.OrdinalIgnoreCase))
                       ?? axes[0];
        _studioRuntimeStatusAxisCombo.SelectedItem = selected;
        _studioRuntimeStatusSelectedAxisId = selected.Id;
        HookStudioRuntimeStatusBaseCombos(profile);
        RefreshStudioRuntimeStatusAxisUi(selectEffectiveValue: true);
    }

    private void HookStudioRuntimeStatusBaseCombos(ForeverRuntimeStateProfile profile)
    {
        foreach (var axis in profile.Axes.Where(IsStudioRuntimeStatusAxis))
        {
            if (!_studioRuntimeStateCombos.TryGetValue(axis.Id, out var combo))
                continue;

            combo.SelectionChanged += (_, _) =>
            {
                RefreshStudioRuntimeStatusTimelineReadout();
                if (TryResolveStudioRuntimeStatusKeyframe(
                        profile.AssetId,
                        axis.Id,
                        _studioAnimationTimeline?.Value ?? 0,
                        out _))
                {
                    RenderStudioRuntimeStatusTimelineFrameNow();
                }
            };
        }
    }

    private void RefreshStudioRuntimeStatusAxisUi(bool selectEffectiveValue)
    {
        if (_studioRuntimeStatusAxisCombo?.SelectedItem is not ForeverRuntimeStateAxis axis ||
            _studioRuntimeStatusValueCombo is null)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        _studioRuntimeStatusValueCombo.ItemsSource = axis.Options;
        _studioRuntimeStatusValueCombo.IsEnabled = true;

        if (selectEffectiveValue)
        {
            var optionId = ResolveStudioRuntimeStatusEffectiveOptionId(assetId, axis, _studioAnimationTimeline?.Value ?? 0);
            _studioRuntimeStatusValueCombo.SelectedItem =
                axis.Options.FirstOrDefault(option => option.Id.Equals(optionId, StringComparison.OrdinalIgnoreCase))
                ?? axis.DefaultOption;
        }

        RefreshStudioRuntimeStatusKeyframeList();
        RefreshStudioRuntimeStatusTimelineReadout();
    }

    private void RefreshStudioRuntimeStatusKeyframeList()
    {
        if (_studioRuntimeStatusKeyframeList is null ||
            _studioRuntimeStatusAxisCombo?.SelectedItem is not ForeverRuntimeStateAxis axis)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var ordered = GetStudioRuntimeStatusTrack(assetId, axis.Id, create: false)?
                          .OrderBy(keyframe => keyframe.TimeSeconds)
                          .ToList() ?? [];
        var currentTime = _studioAnimationTimeline?.Value ?? 0;
        var selectedIndex = ordered.FindIndex(keyframe =>
            Math.Abs(keyframe.TimeSeconds - currentTime) < StudioAnimationTimeEpsilon);

        _studioRuntimeStatusKeyframeList.ItemsSource = ordered.Select(keyframe =>
        {
            var option = axis.Options.FirstOrDefault(candidate =>
                candidate.Id.Equals(keyframe.OptionId, StringComparison.OrdinalIgnoreCase));
            return $"{keyframe.TimeSeconds:0.###}s · {option?.DisplayName ?? keyframe.OptionId}";
        }).ToList();
        _studioRuntimeStatusKeyframeList.SelectedIndex = selectedIndex;
    }

    private void RefreshStudioRuntimeStatusTimelineReadout()
    {
        if (_studioRuntimeStatusEffectiveText is null ||
            _studioRuntimeStatusAxisCombo?.SelectedItem is not ForeverRuntimeStateAxis axis)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var profile = ForeverRuntimeStateCatalog.ForAsset(assetId);
        var time = _studioAnimationTimeline?.Value ?? 0;
        var optionId = ResolveStudioRuntimeStatusEffectiveOptionId(assetId, axis, time);
        var option = axis.Options.FirstOrDefault(candidate =>
                         candidate.Id.Equals(optionId, StringComparison.OrdinalIgnoreCase))
                     ?? axis.DefaultOption;
        var keyed = TryResolveStudioRuntimeStatusKeyframe(assetId, axis.Id, time, out var sourceKeyframe);
        var source = keyed
            ? $"keyframed since {sourceKeyframe.TimeSeconds:0.###}s"
            : "Phase 17C.1 base state";
        _studioRuntimeStatusEffectiveText.Text =
            $"Effective @ {time:0.###}s · {axis.DisplayName}: {option.DisplayName} · {source} · Step/Hold";

        if (_studioRuntimeStatusValueCombo is not null && !_studioRuntimeStatusValueCombo.IsKeyboardFocusWithin)
            _studioRuntimeStatusValueCombo.SelectedItem = option;
    }

    private string ResolveStudioRuntimeStatusEffectiveOptionId(
        string assetId,
        ForeverRuntimeStateAxis axis,
        double time)
    {
        if (TryResolveStudioRuntimeStatusKeyframe(assetId, axis.Id, time, out var keyframe) &&
            axis.Options.Any(option => option.Id.Equals(keyframe.OptionId, StringComparison.OrdinalIgnoreCase)))
            return keyframe.OptionId;

        var profile = ForeverRuntimeStateCatalog.ForAsset(assetId);
        return GetStudioRuntimeStateOption(profile, axis).Id;
    }

    private bool TryResolveStudioRuntimeStatusKeyframe(
        string assetId,
        string axisId,
        double time,
        out StudioRuntimeStatusKeyframe keyframe)
    {
        keyframe = null!;
        var track = GetStudioRuntimeStatusTrack(assetId, axisId, create: false);
        if (track is null || track.Count == 0)
            return false;

        var resolved = track
            .Where(candidate => candidate.TimeSeconds <= time + StudioAnimationTimeEpsilon)
            .OrderBy(candidate => candidate.TimeSeconds)
            .LastOrDefault();
        if (resolved is null)
            return false;

        keyframe = resolved;
        return true;
    }

    private void UpsertStudioRuntimeStatusKeyframe()
    {
        if (_studioRuntimeStatusAxisCombo?.SelectedItem is not ForeverRuntimeStateAxis axis ||
            _studioRuntimeStatusValueCombo?.SelectedItem is not ForeverRuntimeStateOption option)
        {
            SetStudioAnimationStatus("This machine has no source-backed status channel selected.", "#D8B26A");
            return;
        }

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var time = Math.Clamp(_studioAnimationTimeline?.Value ?? 0, 0, _studioAnimationDurationSeconds);
        var track = GetStudioRuntimeStatusTrack(assetId, axis.Id, create: true)!;
        var existing = track.FirstOrDefault(candidate =>
            Math.Abs(candidate.TimeSeconds - time) < StudioAnimationTimeEpsilon);
        if (existing is null)
            track.Add(new StudioRuntimeStatusKeyframe { TimeSeconds = time, OptionId = option.Id });
        else
            existing.OptionId = option.Id;
        track.Sort((left, right) => left.TimeSeconds.CompareTo(right.TimeSeconds));

        RefreshStudioRuntimeStatusKeyframeList();
        RefreshStudioRuntimeStatusTimelineReadout();
        RenderStudioRuntimeStatusTimelineFrameNow();
        SetStudioAnimationStatus(
            $"{axis.DisplayName} keyed to {option.DisplayName} at {time:0.###}s · Step/Hold.",
            "#74C99A");
    }

    private void DeleteSelectedStudioRuntimeStatusKeyframe()
    {
        if (_studioRuntimeStatusAxisCombo?.SelectedItem is not ForeverRuntimeStateAxis axis ||
            _studioRuntimeStatusKeyframeList is null ||
            _studioRuntimeStatusKeyframeList.SelectedIndex < 0)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var track = GetStudioRuntimeStatusTrack(assetId, axis.Id, create: false);
        if (track is null)
            return;
        var ordered = track.OrderBy(keyframe => keyframe.TimeSeconds).ToList();
        var index = _studioRuntimeStatusKeyframeList.SelectedIndex;
        if (index < 0 || index >= ordered.Count)
            return;

        track.Remove(ordered[index]);
        RefreshStudioRuntimeStatusAxisUi(selectEffectiveValue: true);
        RenderStudioRuntimeStatusTimelineFrameNow();
        SetStudioAnimationStatus("Status keyframe removed. Base/previous Step state restored.", "#74C99A");
    }

    private void ClearStudioRuntimeStatusTrack()
    {
        if (_studioRuntimeStatusAxisCombo?.SelectedItem is not ForeverRuntimeStateAxis axis)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var track = GetStudioRuntimeStatusTrack(assetId, axis.Id, create: false);
        if (track is null || track.Count == 0)
            return;
        track.Clear();
        RefreshStudioRuntimeStatusAxisUi(selectEffectiveValue: true);
        RenderStudioRuntimeStatePreview(_studioAnimationTimeline?.Value ?? 0);
        SetStudioAnimationStatus("Status channel cleared. Phase 17C.1 base state is active again.", "#74C99A");
    }

    private void LoadSelectedStudioRuntimeStatusKeyframe()
    {
        if (_studioRuntimeStatusAxisCombo?.SelectedItem is not ForeverRuntimeStateAxis axis ||
            _studioRuntimeStatusKeyframeList is null ||
            _studioRuntimeStatusKeyframeList.SelectedIndex < 0)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var ordered = GetStudioRuntimeStatusTrack(assetId, axis.Id, create: false)?
                          .OrderBy(keyframe => keyframe.TimeSeconds)
                          .ToList() ?? [];
        var index = _studioRuntimeStatusKeyframeList.SelectedIndex;
        if (index < 0 || index >= ordered.Count)
            return;

        var keyframe = ordered[index];
        if (_studioRuntimeStatusValueCombo is not null)
            _studioRuntimeStatusValueCombo.SelectedItem = axis.Options.FirstOrDefault(option =>
                option.Id.Equals(keyframe.OptionId, StringComparison.OrdinalIgnoreCase));
        SetStudioAnimationTimelineTime(keyframe.TimeSeconds);
        QueueExplicitStudioRuntimeStatusPreview();
    }

    private Dictionary<string, (bool HadValue, string Value)> ApplyStudioRuntimeStatusTimelineSelections(double time)
    {
        var snapshot = new Dictionary<string, (bool HadValue, string Value)>(StringComparer.OrdinalIgnoreCase);
        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var profile = ForeverRuntimeStateCatalog.ForAsset(assetId);
        foreach (var axis in profile.Axes.Where(IsStudioRuntimeStatusAxis))
        {
            if (!TryResolveStudioRuntimeStatusKeyframe(assetId, axis.Id, time, out var keyframe) ||
                !axis.Options.Any(option => option.Id.Equals(keyframe.OptionId, StringComparison.OrdinalIgnoreCase)))
                continue;

            var key = RuntimeStateSelectionKey(assetId, axis.Id);
            snapshot[key] = _studioRuntimeStateSelections.TryGetValue(key, out var old)
                ? (true, old)
                : (false, string.Empty);
            _studioRuntimeStateSelections[key] = keyframe.OptionId;
        }
        return snapshot;
    }

    private void RestoreStudioRuntimeStatusTimelineSelections(
        Dictionary<string, (bool HadValue, string Value)> snapshot)
    {
        foreach (var pair in snapshot)
        {
            if (pair.Value.HadValue)
                _studioRuntimeStateSelections[pair.Key] = pair.Value.Value;
            else
                _studioRuntimeStateSelections.Remove(pair.Key);
        }
    }

    private void PrepareStudioRuntimeStatusForQueuedPreview()
    {
        if (!_studioAnimationDiscreteChannelsInitialized)
            return;

        var time = _studioAnimationTimeline?.Value ?? 0;
        var snapshot = ApplyStudioRuntimeStatusTimelineSelections(time);
        if (snapshot.Count == 0)
            return;

        if (ResolveCurrentStudioRuntimeAssetId() == "garden_chamber_mark_i")
        {
            // Garden's 17C.1 finalizer is deliberately synchronous to beat WPF transparent-depth
            // artifacts. Re-render once with the keyed state before returning from the same UI tick.
            RenderStudioRuntimeStatePreview(time);
            RestoreStudioRuntimeStatusTimelineSelections(snapshot);
            return;
        }

        // The 17C.1 preview is already queued at DispatcherPriority.Render. Keep the temporary keyed
        // selection alive through that render, then restore the user's base-state combo afterwards.
        _ = Dispatcher.BeginInvoke(() =>
        {
            RestoreStudioRuntimeStatusTimelineSelections(snapshot);
            RefreshStudioRuntimeStatusTimelineReadout();
        }, DispatcherPriority.ContextIdle);
    }

    private void QueueExplicitStudioRuntimeStatusPreview()
    {
        if (!_studioAnimationDiscreteChannelsInitialized)
            return;

        _ = Dispatcher.BeginInvoke(() => RenderStudioRuntimeStatusTimelineFrameNow(), DispatcherPriority.Render);
    }

    private void RenderStudioRuntimeStatusTimelineFrameNow()
    {
        var time = _studioAnimationTimeline?.Value ?? 0;
        var snapshot = ApplyStudioRuntimeStatusTimelineSelections(time);
        try
        {
            RenderStudioRuntimeStatePreview(time);
        }
        finally
        {
            RestoreStudioRuntimeStatusTimelineSelections(snapshot);
        }
        RefreshStudioRuntimeStatusTimelineReadout();
        QueueStudioVisibilitySelectionSync();
    }

    private void QueueStudioVisibilitySelectionSync()
    {
        if (!_studioAnimationDiscreteChannelsInitialized)
            return;

        _ = Dispatcher.BeginInvoke(SyncStudioVisibilitySelectionToEvaluatedFrame, DispatcherPriority.ContextIdle);
    }

    private void SyncStudioVisibilitySelectionToEvaluatedFrame()
    {
        var target = GetSelectedStudioAnimationTarget();
        if (target?.Kind != AnimationTargetKind.JsonModelElement || target.ElementIndex is not int index)
            return;

        var track = FindStudioAnimationTrackForTarget(target);
        var frame = EvaluateStudioAnimationFrame(track, _studioAnimationTimeline?.Value ?? 0);
        _studioAnimationViewport?.SelectElement(frame.Visible ? index : -1);
    }
}
