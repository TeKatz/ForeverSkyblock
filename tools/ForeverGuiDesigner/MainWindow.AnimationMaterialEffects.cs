using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17C.3 material and special-effect authoring.
///
/// Material state is target-local and may only reference texture slots that already exist in the
/// resolved source model. Special effects are machine-wide source-backed renderer effects; Studio
/// may keep Source behavior or suppress an effect, but never force an effect past its source predicate.
/// Both channel types use deterministic Step/Hold semantics and remain preview-only.
/// </summary>
public partial class MainWindow
{
    private sealed record StudioMaterialOption(string TextureReference, string DisplayName);

    private sealed class StudioRuntimeEffectKeyframe
    {
        public double TimeSeconds { get; set; }
        public bool Suppressed { get; set; }
    }

    private bool _studioAnimationMaterialEffectsInitialized;
    private ComboBox? _studioAnimationMaterialCombo;
    private TextBlock? _studioAnimationMaterialEffectiveText;
    private ComboBox? _studioRuntimeEffectCombo;
    private ComboBox? _studioRuntimeEffectModeCombo;
    private ListBox? _studioRuntimeEffectKeyframeList;
    private TextBlock? _studioRuntimeEffectEffectiveText;
    private string? _studioRuntimeSelectedEffectId;
    private readonly Dictionary<string, List<StudioRuntimeEffectKeyframe>> _studioRuntimeEffectTracks =
        new(StringComparer.OrdinalIgnoreCase);

    public void EnableForeverAnimationMaterialEffects()
    {
        if (_studioAnimationMaterialEffectsInitialized || !_studioAnimationDiscreteChannelsInitialized)
            return;

        _studioAnimationMaterialEffectsInitialized = true;
        InstallStudioMaterialTimelineUi();
        InstallStudioRuntimeEffectTimelineUi();

        if (_studioAnimationTextureStateBox is not null)
        {
            _studioAnimationTextureStateBox.IsReadOnly = true;
            _studioAnimationTextureStateBox.IsEnabled = false;
            _studioAnimationTextureStateBox.ToolTip =
                "Legacy preview metadata. Use the Phase 17C.3 Material Timeline, which only offers texture slots from the loaded source model.";
        }
        if (_studioAnimationEffectBox is not null)
        {
            _studioAnimationEffectBox.IsReadOnly = true;
            _studioAnimationEffectBox.IsEnabled = false;
            _studioAnimationEffectBox.ToolTip =
                "Legacy preview metadata. Use the source-backed Phase 17C.3 Special Effect Timeline.";
        }

        if (_studioAnimationElementCombo is not null)
            _studioAnimationElementCombo.SelectionChanged += (_, _) => QueueStudioMaterialInspectorRefresh();
        if (_studioBlockMachineCombo is not null)
            _studioBlockMachineCombo.SelectionChanged += (_, _) => QueueStudioMaterialEffectInspectorRefresh();

        if (_studioAnimationTimeline is not null)
        {
            _studioAnimationTimeline.ValueChanged += (_, _) =>
            {
                RefreshStudioMaterialEffectiveReadout(selectEffectiveValue: true);
                RefreshStudioRuntimeEffectTimelineReadout();
                RefreshStudioRuntimeEffectKeyframeList();
            };
        }

        if (_studioAnimationSurface is not null)
        {
            _studioAnimationSurface.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                    QueueStudioMaterialEffectInspectorRefresh();
            };
        }

        RefreshStudioMaterialInspector();
        RefreshStudioRuntimeEffectInspector();
    }

    private void InstallStudioMaterialTimelineUi()
    {
        if (_studioAnimationTextureStateBox?.Parent is not StackPanel inspector)
            return;

        var section = new StackPanel { Margin = new Thickness(0, 7, 0, 0) };
        section.Children.Add(StudioSectionLabel("MATERIAL TIMELINE · PHASE 17C.3"));
        section.Children.Add(new TextBlock
        {
            Text = "Target-local material override. Only texture slots already resolved from the loaded model are selectable. UVs and geometry stay unchanged; values switch discretely and hold.",
            Foreground = Brush("#8FA1AA"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 8)
        });

        _studioAnimationMaterialCombo = BlockViewerCombo();
        _studioAnimationMaterialCombo.DisplayMemberPath = nameof(StudioMaterialOption.DisplayName);
        section.Children.Add(_studioAnimationMaterialCombo);

        _studioAnimationMaterialEffectiveText = new TextBlock
        {
            Foreground = Brush("#71848D"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 6)
        };
        section.Children.Add(_studioAnimationMaterialEffectiveText);

        var key = StudioPrimaryButton("KEY MATERIAL @ TIME", 142);
        key.Height = 29;
        key.Margin = new Thickness(0, 0, 0, 4);
        key.ToolTip = "Create/update the current target keyframe while preserving transform and visibility. Material uses Step/Hold semantics.";
        key.Click += (_, _) => KeyStudioAnimationMaterialAtCurrentTime();
        section.Children.Add(key);

        var insertAt = inspector.Children.IndexOf(_studioAnimationTextureStateBox) + 1;
        inspector.Children.Insert(Math.Clamp(insertAt, 0, inspector.Children.Count), section);
    }

    private void QueueStudioMaterialInspectorRefresh()
    {
        if (!_studioAnimationMaterialEffectsInitialized)
            return;

        _ = Dispatcher.BeginInvoke(RefreshStudioMaterialInspector, DispatcherPriority.ContextIdle);
    }

    private void RefreshStudioMaterialInspector()
    {
        if (_studioAnimationMaterialCombo is null)
            return;

        var options = BuildStudioMaterialOptions();
        _studioAnimationMaterialCombo.ItemsSource = null;
        _studioAnimationMaterialCombo.ItemsSource = options;
        _studioAnimationMaterialCombo.IsEnabled =
            GetSelectedStudioAnimationTarget()?.Kind == AnimationTargetKind.JsonModelElement && options.Count > 1;
        RefreshStudioMaterialEffectiveReadout(selectEffectiveValue: true);
    }

    private List<StudioMaterialOption> BuildStudioMaterialOptions()
    {
        var result = new List<StudioMaterialOption>
        {
            new(string.Empty, "Source faces")
        };
        if (_studioEditableBlockModel is null)
            return result;

        result.AddRange(_studioEditableBlockModel.Textures
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new StudioMaterialOption($"#{pair.Key}", $"#{pair.Key}  →  {pair.Value}")));
        return result;
    }

    private string ResolveStudioMaterialTextureState(double time)
    {
        var target = GetSelectedStudioAnimationTarget();
        if (target?.Kind != AnimationTargetKind.JsonModelElement || _studioEditableBlockModel is null)
            return string.Empty;

        var track = FindStudioAnimationTrackForTarget(target);
        var state = EvaluateStudioAnimationFrame(track, time).TextureState?.Trim() ?? string.Empty;
        return IsValidStudioMaterialTextureReference(_studioEditableBlockModel, state) ? state : string.Empty;
    }

    private void RefreshStudioMaterialEffectiveReadout(bool selectEffectiveValue)
    {
        if (_studioAnimationMaterialEffectiveText is null || _studioAnimationMaterialCombo is null)
            return;

        var time = _studioAnimationTimeline?.Value ?? 0;
        var target = GetSelectedStudioAnimationTarget();
        if (target?.Kind != AnimationTargetKind.JsonModelElement)
        {
            _studioAnimationMaterialEffectiveText.Text = "Select a JSON model element to author material state.";
            _studioAnimationMaterialCombo.SelectedIndex = -1;
            return;
        }

        var state = ResolveStudioMaterialTextureState(time);
        var options = _studioAnimationMaterialCombo.ItemsSource?.Cast<StudioMaterialOption>().ToList() ?? [];
        var option = options.FirstOrDefault(candidate =>
                         candidate.TextureReference.Equals(state, StringComparison.OrdinalIgnoreCase))
                     ?? options.FirstOrDefault();
        if (selectEffectiveValue && option is not null)
            _studioAnimationMaterialCombo.SelectedItem = option;

        var label = string.IsNullOrWhiteSpace(state) ? "Source faces" : state;
        _studioAnimationMaterialEffectiveText.Text = $"Effective @ {time:0.###}s · {label} · Step/Hold";
    }

    private void KeyStudioAnimationMaterialAtCurrentTime()
    {
        var target = GetSelectedStudioAnimationTarget();
        if (target?.Kind != AnimationTargetKind.JsonModelElement ||
            _studioAnimationMaterialCombo?.SelectedItem is not StudioMaterialOption option)
        {
            SetStudioAnimationStatus("Select a JSON model element and a material slot first.", "#D8B26A");
            return;
        }

        var track = GetStudioAnimationTrack(create: true);
        if (track is null)
            return;

        var time = Math.Clamp(_studioAnimationTimeline?.Value ?? 0, 0, _studioAnimationDurationSeconds);
        var frame = EvaluateStudioAnimationFrame(track, time);
        frame.TimeSeconds = time;
        frame.TextureState = option.TextureReference;
        StoreStudioAnimationInteractionFrame(frame, refreshTrackList: true);
        SelectStudioAnimationKeyframeAtTime(time);
        RefreshStudioMaterialEffectiveReadout(selectEffectiveValue: true);
        QueueStudioRuntimeStatePreview();
        SetStudioAnimationStatus(
            $"Material keyed to {option.DisplayName} at {time:0.###}s · Step/Hold.",
            "#74C99A");
    }

    private static bool IsValidStudioMaterialTextureReference(MinecraftResolvedBlockModel model, string? textureReference)
    {
        if (string.IsNullOrWhiteSpace(textureReference))
            return false;
        var trimmed = textureReference.Trim();
        return trimmed.StartsWith('#') && trimmed.Length > 1 && model.Textures.ContainsKey(trimmed[1..]);
    }

    private static void ApplyStudioAnimationMaterialStateToElement(
        MinecraftResolvedBlockModel model,
        int elementIndex,
        string? textureReference)
    {
        if (elementIndex < 0 || elementIndex >= model.Elements.Count ||
            !IsValidStudioMaterialTextureReference(model, textureReference))
            return;

        var texture = textureReference!.Trim();
        var element = model.Elements[elementIndex];
        foreach (var direction in element.Faces.Keys.ToList())
        {
            var face = element.Faces[direction];
            element.Faces[direction] = new MinecraftModelFace
            {
                Texture = texture,
                Uv = face.Uv is null ? null : [.. face.Uv],
                Rotation = face.Rotation,
                CullFace = face.CullFace,
                TintIndex = face.TintIndex
            };
        }
    }

    private void InstallStudioRuntimeEffectTimelineUi()
    {
        if (_studioRuntimeStateSummaryText?.Parent is not StackPanel runtimeRoot)
            return;

        var section = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
        section.Children.Add(BlockViewerSeparator());
        section.Children.Add(StudioSectionLabel("SPECIAL EFFECT TIMELINE · PHASE 17C.3"));
        section.Children.Add(new TextBlock
        {
            Text = "Only effects that exist in the current renderer source are listed. 'Source' keeps the original predicate; 'Suppressed' hides that effect without forcing it to appear in otherwise invalid machine states.",
            Foreground = Brush("#8FA1AA"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 10)
        });

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

        var effectPanel = new StackPanel { Margin = new Thickness(0, 0, 6, 0) };
        effectPanel.Children.Add(new TextBlock
        {
            Text = "Source effect",
            Foreground = Brush("#71848D"),
            FontSize = 9,
            Margin = new Thickness(1, 0, 0, 3)
        });
        _studioRuntimeEffectCombo = BlockViewerCombo();
        _studioRuntimeEffectCombo.DisplayMemberPath = nameof(ForeverRuntimeEffectDescriptor.DisplayName);
        _studioRuntimeEffectCombo.SelectionChanged += (_, _) =>
        {
            if (_studioRuntimeEffectCombo.SelectedItem is ForeverRuntimeEffectDescriptor effect)
                _studioRuntimeSelectedEffectId = effect.Id;
            RefreshStudioRuntimeEffectAxisUi(selectEffectiveMode: true);
        };
        effectPanel.Children.Add(_studioRuntimeEffectCombo);
        grid.Children.Add(effectPanel);

        var modePanel = new StackPanel { Margin = new Thickness(6, 0, 0, 0) };
        modePanel.Children.Add(new TextBlock
        {
            Text = "Mode",
            Foreground = Brush("#71848D"),
            FontSize = 9,
            Margin = new Thickness(1, 0, 0, 3)
        });
        _studioRuntimeEffectModeCombo = BlockViewerCombo();
        _studioRuntimeEffectModeCombo.ItemsSource = new[] { "Source", "Suppressed" };
        _studioRuntimeEffectModeCombo.SelectedItem = "Source";
        modePanel.Children.Add(_studioRuntimeEffectModeCombo);
        Grid.SetColumn(modePanel, 1);
        grid.Children.Add(modePanel);
        section.Children.Add(grid);

        _studioRuntimeEffectEffectiveText = new TextBlock
        {
            Foreground = Brush("#71848D"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 6)
        };
        section.Children.Add(_studioRuntimeEffectEffectiveText);

        _studioRuntimeEffectKeyframeList = new ListBox
        {
            Height = 88,
            Background = Brush("#0D1418"),
            Foreground = Brush("#D9E4E9"),
            BorderBrush = Brush("#35464F"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 4, 0, 8)
        };
        _studioRuntimeEffectKeyframeList.SelectionChanged += (_, _) => LoadSelectedStudioRuntimeEffectKeyframe();
        section.Children.Add(_studioRuntimeEffectKeyframeList);

        var commands = new WrapPanel();
        var key = StudioPrimaryButton("KEY EFFECT @ TIME", 132);
        key.Height = 29;
        key.Margin = new Thickness(0, 0, 6, 5);
        key.Click += (_, _) => UpsertStudioRuntimeEffectKeyframe();
        commands.Children.Add(key);

        var delete = StudioButton("Delete", 72);
        delete.Height = 29;
        delete.Margin = new Thickness(0, 0, 6, 5);
        delete.Click += (_, _) => DeleteSelectedStudioRuntimeEffectKeyframe();
        commands.Children.Add(delete);

        var clear = StudioButton("Clear channel", 102);
        clear.Height = 29;
        clear.Margin = new Thickness(0, 0, 0, 5);
        clear.Click += (_, _) => ClearStudioRuntimeEffectTrack();
        commands.Children.Add(clear);
        section.Children.Add(commands);

        runtimeRoot.Children.Add(section);
    }

    private static string StudioRuntimeEffectTrackKey(string assetId, string effectId)
        => $"{assetId.Trim().ToLowerInvariant()}|{effectId.Trim().ToLowerInvariant()}";

    private List<StudioRuntimeEffectKeyframe>? GetStudioRuntimeEffectTrack(
        string assetId,
        string effectId,
        bool create)
    {
        var key = StudioRuntimeEffectTrackKey(assetId, effectId);
        if (_studioRuntimeEffectTracks.TryGetValue(key, out var track))
            return track;
        if (!create)
            return null;
        track = [];
        _studioRuntimeEffectTracks[key] = track;
        return track;
    }

    private void QueueStudioMaterialEffectInspectorRefresh()
    {
        if (!_studioAnimationMaterialEffectsInitialized)
            return;

        _ = Dispatcher.BeginInvoke(() =>
        {
            RefreshStudioMaterialInspector();
            RefreshStudioRuntimeEffectInspector();
        }, DispatcherPriority.ContextIdle);
    }

    private void RefreshStudioRuntimeEffectInspector()
    {
        if (_studioRuntimeEffectCombo is null)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var profile = ForeverRuntimeEffectCatalog.ForAsset(assetId);
        _studioRuntimeEffectCombo.ItemsSource = null;
        _studioRuntimeEffectCombo.ItemsSource = profile.Effects;
        _studioRuntimeEffectCombo.IsEnabled = profile.HasEffects;
        if (_studioRuntimeEffectModeCombo is not null)
            _studioRuntimeEffectModeCombo.IsEnabled = profile.HasEffects;

        if (!profile.HasEffects)
        {
            _studioRuntimeEffectCombo.SelectedIndex = -1;
            if (_studioRuntimeEffectKeyframeList is not null)
                _studioRuntimeEffectKeyframeList.ItemsSource = null;
            if (_studioRuntimeEffectEffectiveText is not null)
                _studioRuntimeEffectEffectiveText.Text = profile.SourceNote;
            return;
        }

        var selected = profile.Effects.FirstOrDefault(effect =>
                           effect.Id.Equals(_studioRuntimeSelectedEffectId, StringComparison.OrdinalIgnoreCase))
                       ?? profile.Effects[0];
        _studioRuntimeEffectCombo.SelectedItem = selected;
        _studioRuntimeSelectedEffectId = selected.Id;
        RefreshStudioRuntimeEffectAxisUi(selectEffectiveMode: true);
    }

    private void RefreshStudioRuntimeEffectAxisUi(bool selectEffectiveMode)
    {
        if (_studioRuntimeEffectCombo?.SelectedItem is not ForeverRuntimeEffectDescriptor effect)
            return;

        if (selectEffectiveMode && _studioRuntimeEffectModeCombo is not null)
        {
            var suppressed = IsStudioRuntimeEffectSuppressed(
                ResolveCurrentStudioRuntimeAssetId(),
                effect.Id,
                _studioAnimationTimeline?.Value ?? 0);
            _studioRuntimeEffectModeCombo.SelectedItem = suppressed ? "Suppressed" : "Source";
        }

        RefreshStudioRuntimeEffectKeyframeList();
        RefreshStudioRuntimeEffectTimelineReadout();
    }

    private void RefreshStudioRuntimeEffectKeyframeList()
    {
        if (_studioRuntimeEffectKeyframeList is null ||
            _studioRuntimeEffectCombo?.SelectedItem is not ForeverRuntimeEffectDescriptor effect)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var ordered = GetStudioRuntimeEffectTrack(assetId, effect.Id, create: false)?
                          .OrderBy(keyframe => keyframe.TimeSeconds)
                          .ToList() ?? [];
        var currentTime = _studioAnimationTimeline?.Value ?? 0;
        var selectedIndex = ordered.FindIndex(keyframe =>
            Math.Abs(keyframe.TimeSeconds - currentTime) < StudioAnimationTimeEpsilon);
        _studioRuntimeEffectKeyframeList.ItemsSource = ordered
            .Select(keyframe => $"{keyframe.TimeSeconds:0.###}s · {(keyframe.Suppressed ? "Suppressed" : "Source")}")
            .ToList();
        _studioRuntimeEffectKeyframeList.SelectedIndex = selectedIndex;
    }

    private void RefreshStudioRuntimeEffectTimelineReadout()
    {
        if (_studioRuntimeEffectEffectiveText is null ||
            _studioRuntimeEffectCombo?.SelectedItem is not ForeverRuntimeEffectDescriptor effect)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var time = _studioAnimationTimeline?.Value ?? 0;
        var keyed = TryResolveStudioRuntimeEffectKeyframe(assetId, effect.Id, time, out var keyframe);
        var suppressed = keyed && keyframe.Suppressed;
        var source = keyed ? $"keyframed since {keyframe.TimeSeconds:0.###}s" : "source behavior";
        _studioRuntimeEffectEffectiveText.Text =
            $"Effective @ {time:0.###}s · {(suppressed ? "Suppressed" : "Source")} · {source} · {effect.SourceSymbol}\n{effect.Description}";

        if (_studioRuntimeEffectModeCombo is not null && !_studioRuntimeEffectModeCombo.IsKeyboardFocusWithin)
            _studioRuntimeEffectModeCombo.SelectedItem = suppressed ? "Suppressed" : "Source";
    }

    private bool TryResolveStudioRuntimeEffectKeyframe(
        string assetId,
        string effectId,
        double time,
        out StudioRuntimeEffectKeyframe keyframe)
    {
        keyframe = null!;
        var track = GetStudioRuntimeEffectTrack(assetId, effectId, create: false);
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

    private bool IsStudioRuntimeEffectSuppressed(string assetId, string effectId, double time)
        => _studioAnimationMaterialEffectsInitialized &&
           TryResolveStudioRuntimeEffectKeyframe(assetId, effectId, time, out var keyframe) &&
           keyframe.Suppressed;

    private bool IsStudioRuntimeEffectEnabled(string assetId, string effectId, bool sourcePredicate)
        => sourcePredicate && !IsStudioRuntimeEffectSuppressed(
            assetId,
            effectId,
            _studioAnimationTimeline?.Value ?? 0);

    private void UpsertStudioRuntimeEffectKeyframe()
    {
        if (_studioRuntimeEffectCombo?.SelectedItem is not ForeverRuntimeEffectDescriptor effect ||
            _studioRuntimeEffectModeCombo?.SelectedItem is not string mode)
        {
            SetStudioAnimationStatus("This machine has no source-backed special effect selected.", "#D8B26A");
            return;
        }

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var time = Math.Clamp(_studioAnimationTimeline?.Value ?? 0, 0, _studioAnimationDurationSeconds);
        var track = GetStudioRuntimeEffectTrack(assetId, effect.Id, create: true)!;
        var existing = track.FirstOrDefault(candidate =>
            Math.Abs(candidate.TimeSeconds - time) < StudioAnimationTimeEpsilon);
        var suppressed = mode.Equals("Suppressed", StringComparison.OrdinalIgnoreCase);
        if (existing is null)
            track.Add(new StudioRuntimeEffectKeyframe { TimeSeconds = time, Suppressed = suppressed });
        else
            existing.Suppressed = suppressed;
        track.Sort((left, right) => left.TimeSeconds.CompareTo(right.TimeSeconds));

        RefreshStudioRuntimeEffectAxisUi(selectEffectiveMode: true);
        RenderStudioRuntimeStatePreview(time);
        SetStudioAnimationStatus(
            $"{effect.DisplayName} keyed to {(suppressed ? "Suppressed" : "Source")} at {time:0.###}s · Step/Hold.",
            "#74C99A");
    }

    private void DeleteSelectedStudioRuntimeEffectKeyframe()
    {
        if (_studioRuntimeEffectCombo?.SelectedItem is not ForeverRuntimeEffectDescriptor effect ||
            _studioRuntimeEffectKeyframeList is null ||
            _studioRuntimeEffectKeyframeList.SelectedIndex < 0)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var track = GetStudioRuntimeEffectTrack(assetId, effect.Id, create: false);
        if (track is null)
            return;
        var ordered = track.OrderBy(keyframe => keyframe.TimeSeconds).ToList();
        var index = _studioRuntimeEffectKeyframeList.SelectedIndex;
        if (index < 0 || index >= ordered.Count)
            return;

        track.Remove(ordered[index]);
        RefreshStudioRuntimeEffectAxisUi(selectEffectiveMode: true);
        RenderStudioRuntimeStatePreview(_studioAnimationTimeline?.Value ?? 0);
        SetStudioAnimationStatus("Effect keyframe removed. Previous/source behavior restored.", "#74C99A");
    }

    private void ClearStudioRuntimeEffectTrack()
    {
        if (_studioRuntimeEffectCombo?.SelectedItem is not ForeverRuntimeEffectDescriptor effect)
            return;
        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var track = GetStudioRuntimeEffectTrack(assetId, effect.Id, create: false);
        if (track is null || track.Count == 0)
            return;

        track.Clear();
        RefreshStudioRuntimeEffectAxisUi(selectEffectiveMode: true);
        RenderStudioRuntimeStatePreview(_studioAnimationTimeline?.Value ?? 0);
        SetStudioAnimationStatus("Effect channel cleared. Source behavior is active again.", "#74C99A");
    }

    private void LoadSelectedStudioRuntimeEffectKeyframe()
    {
        if (_studioRuntimeEffectCombo?.SelectedItem is not ForeverRuntimeEffectDescriptor effect ||
            _studioRuntimeEffectKeyframeList is null ||
            _studioRuntimeEffectKeyframeList.SelectedIndex < 0)
            return;

        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var ordered = GetStudioRuntimeEffectTrack(assetId, effect.Id, create: false)?
                          .OrderBy(keyframe => keyframe.TimeSeconds)
                          .ToList() ?? [];
        var index = _studioRuntimeEffectKeyframeList.SelectedIndex;
        if (index < 0 || index >= ordered.Count)
            return;

        var keyframe = ordered[index];
        if (_studioRuntimeEffectModeCombo is not null)
            _studioRuntimeEffectModeCombo.SelectedItem = keyframe.Suppressed ? "Suppressed" : "Source";
        SetStudioAnimationTimelineTime(keyframe.TimeSeconds);
        QueueStudioRuntimeStatePreview();
    }
}
