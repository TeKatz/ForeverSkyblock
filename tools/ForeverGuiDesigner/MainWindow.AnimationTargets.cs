using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17A bridge between the block-animation authoring surface and the canonical target registry.
/// This file intentionally owns target resolution so Timeline/selection/preview do not grow separate
/// lookup rules.
/// </summary>
public partial class MainWindow
{
    private readonly AnimationTargetRegistry _studioAnimationTargetRegistry = new();

    private AnimationTargetDescriptor? GetSelectedStudioAnimationTarget()
    {
        if (_studioAnimationElementCombo?.SelectedItem is AnimationTargetDescriptor descriptor)
            return _studioAnimationTargetRegistry.FindById(descriptor.TargetId.Value);

        if (_studioEditableBlockModel is null || _studioAnimationElementCombo is null || _studioAnimationElementCombo.SelectedIndex < 0)
            return null;

        return _studioAnimationTargetRegistry.FindJsonTarget(
            _studioEditableBlockModel.SourcePath,
            _studioAnimationElementCombo.SelectedIndex);
    }

    private AnimationTargetResolution ResolveStudioAnimationTrack(StudioBlockAnimationTrack track)
        => _studioAnimationTargetRegistry.Resolve(track.TargetId, track.ModelKey, track.ElementIndex);

    private AnimationTargetResolution ResolveSelectedStudioAnimationTarget()
    {
        var selected = GetSelectedStudioAnimationTarget();
        if (selected is null)
        {
            return new AnimationTargetResolution(
                AnimationTargetResolutionState.Unresolved,
                null,
                "No animation target is selected.");
        }

        return _studioAnimationTargetRegistry.Resolve(selected.TargetId.Value, selected.ModelKey, selected.ElementIndex ?? -1);
    }

    private StudioBlockAnimationTrack? FindStudioAnimationTrackForTarget(AnimationTargetDescriptor target)
    {
        if (_studioAnimationTracks.TryGetValue(target.TargetId.Value, out var stableTrack))
            return stableTrack;

        // Legacy tracks may have been keyed by SourcePath|element:N. Resolve them without mutating
        // either the dictionary key or their stored compatibility fields. Migration stays deferred
        // until a future explicit Review/Apply write path.
        foreach (var track in _studioAnimationTracks.Values.Distinct())
        {
            var resolution = ResolveStudioAnimationTrack(track);
            if (resolution.Target is not null &&
                string.Equals(resolution.Target.TargetId.Value, target.TargetId.Value, StringComparison.Ordinal))
                return track;
        }

        return null;
    }

    private void RebuildStudioAnimationTargetRegistry()
    {
        if (_studioEditableBlockModel is null)
            return;

        _studioAnimationTargetRegistry.RebuildJsonModelTargets(_studioEditableBlockModel);
        SyncStudioAnimationRuntimeTargets();
    }

    private void SyncStudioAnimationRuntimeTargets()
    {
        _studioAnimationTargetRegistry.ClearRuntimeTargets();
        if (_nonWashingStableAnimationParts.Count == 0)
            return;

        var scope = string.IsNullOrWhiteSpace(ActiveRuntimeScreen) ? "machine-runtime" : ActiveRuntimeScreen;
        foreach (var pair in _nonWashingStableAnimationParts.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            _studioAnimationTargetRegistry.RegisterRuntimeComponent(
                scope,
                pair.Key,
                pair.Value,
                $"Runtime · {scope} · {pair.Key}");
        }
    }

    private string DescribeStudioAnimationTargetHealth()
    {
        if (_studioAnimationTracks.Count == 0)
            return string.Empty;

        var unresolved = new List<string>();
        var migratable = new List<string>();
        foreach (var track in _studioAnimationTracks.Values.Distinct())
        {
            var resolution = ResolveStudioAnimationTrack(track);
            var label = !string.IsNullOrWhiteSpace(track.TargetId)
                ? track.TargetId!
                : $"{track.ModelKey} @ element {track.ElementIndex}";

            switch (resolution.State)
            {
                case AnimationTargetResolutionState.LegacyMigratable:
                    migratable.Add(label);
                    break;
                case AnimationTargetResolutionState.Unresolved:
                    unresolved.Add(label);
                    break;
            }
        }

        if (unresolved.Count > 0)
        {
            var examples = string.Join(", ", unresolved.Take(2));
            return $" · TARGET WARNING: {unresolved.Count} unresolved ({examples}). Tracks/keyframes were preserved.";
        }

        if (migratable.Count > 0)
            return $" · {migratable.Count} legacy target(s) resolved and remain unmigrated until an explicit write operation.";

        return string.Empty;
    }

    private string DescribeStudioAnimationTarget(AnimationTargetDescriptor? target)
    {
        if (target is null)
            return "unresolved target";

        return target.Kind switch
        {
            AnimationTargetKind.JsonModelElement => $"{target.DisplayName} · {target.TargetId.Value}",
            AnimationTargetKind.RuntimeRendererComponent => $"{target.DisplayName} · {target.TargetId.Value}",
            _ => target.TargetId.Value
        };
    }
}
