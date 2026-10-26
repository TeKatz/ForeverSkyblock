using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Keeps the 17D.1 dirty indicator semantic rather than event-based. UI refreshes and selection
/// changes may fire frequently, so each hook compares the canonical current document with the
/// persisted project JSON before deciding whether the asset is actually dirty.
/// </summary>
public partial class MainWindow
{
    private bool _studioAnimationPersistenceDirtyTrackingInitialized;
    private bool _studioAnimationPersistenceDirtyRefreshQueued;
    private readonly HashSet<ComboBox> _studioAnimationPersistenceTrackedStateCombos = [];

    public void EnableForeverAnimationPersistenceDirtyTracking()
    {
        if (_studioAnimationPersistenceDirtyTrackingInitialized || !_studioAnimationPersistenceInitialized)
            return;
        _studioAnimationPersistenceDirtyTrackingInitialized = true;

        if (_studioAnimationKeyframeList is not null)
            _studioAnimationKeyframeList.SelectionChanged += (_, _) => QueueStudioAnimationPersistenceDirtyRecompute();
        if (_studioRuntimeStatusKeyframeList is not null)
            _studioRuntimeStatusKeyframeList.SelectionChanged += (_, _) => QueueStudioAnimationPersistenceDirtyRecompute();
        if (_studioRuntimeEffectKeyframeList is not null)
            _studioRuntimeEffectKeyframeList.SelectionChanged += (_, _) => QueueStudioAnimationPersistenceDirtyRecompute();
        if (_studioAnimationDurationBox is not null)
            _studioAnimationDurationBox.LostFocus += (_, _) => QueueStudioAnimationPersistenceDirtyRecompute();
        if (_studioAnimationPersistenceIdBox is not null)
            _studioAnimationPersistenceIdBox.LostFocus += (_, _) => QueueStudioAnimationPersistenceDirtyRecompute();
        if (_studioAnimationPersistenceNameBox is not null)
            _studioAnimationPersistenceNameBox.LostFocus += (_, _) => QueueStudioAnimationPersistenceDirtyRecompute();
        if (_studioAnimationLoopCheck is not null)
        {
            _studioAnimationLoopCheck.Checked += (_, _) => QueueStudioAnimationPersistenceDirtyRecompute();
            _studioAnimationLoopCheck.Unchecked += (_, _) => QueueStudioAnimationPersistenceDirtyRecompute();
        }
        if (_studioBlockMachineCombo is not null)
        {
            _studioBlockMachineCombo.SelectionChanged += (_, _) =>
                _ = Dispatcher.BeginInvoke(HookStudioAnimationPersistenceRuntimeStateCombos, DispatcherPriority.ContextIdle);
        }

        HookStudioAnimationPersistenceRuntimeStateCombos();
    }

    private void HookStudioAnimationPersistenceRuntimeStateCombos()
    {
        foreach (var combo in _studioRuntimeStateCombos.Values)
        {
            if (!_studioAnimationPersistenceTrackedStateCombos.Add(combo))
                continue;
            combo.SelectionChanged += (_, _) => QueueStudioAnimationPersistenceDirtyRecompute();
        }
    }

    private void QueueStudioAnimationPersistenceDirtyRecompute()
    {
        if (!_studioAnimationPersistenceDirtyTrackingInitialized || _studioAnimationPersistenceLoading ||
            _studioAnimationPersistenceDirtyRefreshQueued)
            return;
        _studioAnimationPersistenceDirtyRefreshQueued = true;
        _ = Dispatcher.BeginInvoke(() =>
        {
            _studioAnimationPersistenceDirtyRefreshQueued = false;
            RecomputeStudioAnimationPersistenceDirtyState();
        }, DispatcherPriority.ContextIdle);
    }

    private void RecomputeStudioAnimationPersistenceDirtyState()
    {
        if (_studioAnimationPersistenceLoading || _studioEditableBlockModel is null || string.IsNullOrWhiteSpace(_repoRoot))
            return;
        var assetId = CurrentStudioAnimationPersistenceAssetId();
        var fullPath = CurrentStudioAnimationPersistenceFullPath();
        if (string.IsNullOrWhiteSpace(assetId) || fullPath is null)
            return;

        var currentDocument = BuildCurrentStudioAnimationPersistenceDocument();
        var currentJson = ForeverStudioAnimationPersistence.Serialize(currentDocument);
        var dirty = true;

        if (File.Exists(fullPath))
        {
            try
            {
                var diskValidation = ForeverStudioAnimationPersistence.ValidateRoundTrip(File.ReadAllText(fullPath));
                dirty = diskValidation.Document is null || diskValidation.HasErrors ||
                        !string.Equals(currentJson, diskValidation.NormalizedJson, StringComparison.Ordinal);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                dirty = true;
            }
        }
        else
        {
            dirty = !IsStudioAnimationPersistenceDefaultDocument(currentDocument);
        }

        if (dirty)
        {
            _studioAnimationPersistenceDirtyAssets.Add(assetId);
            _studioAnimationPersistencePending[assetId] = currentDocument;
        }
        else
        {
            _studioAnimationPersistenceDirtyAssets.Remove(assetId);
            _studioAnimationPersistencePending.Remove(assetId);
        }
        UpdateStudioAnimationPersistenceDirtyReadout();
    }

    private bool IsStudioAnimationPersistenceDefaultDocument(ForeverStudioAnimationDocument document)
    {
        if (document.Animation.Tracks.Any(track => track.Keyframes.Count > 0) ||
            document.Animation.StatusTracks.Any(track => track.Keyframes.Count > 0) ||
            document.Animation.EffectTracks.Any(track => track.Keyframes.Count > 0))
            return false;
        if (Math.Abs(document.Animation.DurationSeconds - 6) > .0001 || !document.Animation.Loop)
            return false;
        if (!document.Animation.Id.Equals("default", StringComparison.OrdinalIgnoreCase))
            return false;
        var expectedName = $"{(_studioBlockMachineCombo?.SelectedItem as MinecraftBlockAssetDescriptor)?.DisplayName ?? document.AssetId} Animation";
        if (!document.Animation.DisplayName.Equals(expectedName, StringComparison.Ordinal))
            return false;

        var profile = ForeverRuntimeStateCatalog.ForAsset(document.AssetId);
        foreach (var pair in document.Animation.RuntimeStateBase)
        {
            var axis = profile.Axes.FirstOrDefault(candidate => candidate.Id.Equals(pair.Key, StringComparison.OrdinalIgnoreCase));
            if (axis is null || !pair.Value.Equals(axis.DefaultOptionId, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }
}
