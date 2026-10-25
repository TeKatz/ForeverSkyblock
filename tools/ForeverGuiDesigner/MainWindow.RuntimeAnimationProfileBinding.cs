using System.IO;
using System.Windows;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Keeps the Animation Studio runtime profile bound to the block model that is actually selected.
/// The original integration refreshed the profile asynchronously; on simple/default-only assets such
/// as the Garden Chamber that allowed the animation preview to render with a stale previous profile.
/// </summary>
public partial class MainWindow
{
    private bool _studioRuntimeAnimationProfileBindingInitialized;

    public void EnableStudioRuntimeAnimationProfileBindingFix()
    {
        if (_studioRuntimeAnimationProfileBindingInitialized)
            return;
        _studioRuntimeAnimationProfileBindingInitialized = true;

        if (_studioBlockMachineCombo is not null)
            _studioBlockMachineCombo.SelectionChanged += (_, _) => SyncStudioRuntimeAnimationProfileToCurrentSource(queuePreview: true);

        // Registered after the original RuntimeAnimationIntegration handler. The old handler may queue
        // a frame first, but this synchronous binding runs before DispatcherPriority.Render executes it.
        if (_studioAnimationTimeline is not null)
            _studioAnimationTimeline.ValueChanged += (_, _) => SyncStudioRuntimeAnimationProfileToCurrentSource(queuePreview: false);

        if (_studioAnimationSurface is not null)
            _studioAnimationSurface.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                    SyncStudioRuntimeAnimationProfileToCurrentSource(queuePreview: true);
            };

        SyncStudioRuntimeAnimationProfileToCurrentSource(queuePreview: false);
    }

    private void SyncStudioRuntimeAnimationProfileToCurrentSource(bool queuePreview)
    {
        var assetId = ResolveCurrentStudioRuntimeAssetId();
        if (string.IsNullOrWhiteSpace(assetId))
            return;

        if (_studioRuntimeAnimationProfile is not null &&
            string.Equals(_studioRuntimeAnimationProfile.AssetId, assetId, StringComparison.OrdinalIgnoreCase))
            return;

        var selectedTrackId = (_studioAnimationRuntimeCurveCombo?.SelectedItem as ForeverRuntimeAnimationTrack)?.Id;
        var profile = ForeverRuntimeAnimationCatalog.ForAsset(assetId);
        _studioRuntimeAnimationProfile = profile;

        if (_studioAnimationRuntimeCurveCombo is not null)
        {
            _studioAnimationUiUpdating = true;
            try
            {
                _studioAnimationRuntimeCurveCombo.ItemsSource = null;
                _studioAnimationRuntimeCurveCombo.DisplayMemberPath = nameof(ForeverRuntimeAnimationTrack.DisplayName);
                _studioAnimationRuntimeCurveCombo.ItemsSource = profile.Tracks;
                _studioAnimationRuntimeCurveCombo.SelectedItem = string.IsNullOrWhiteSpace(selectedTrackId)
                    ? profile.Tracks.FirstOrDefault()
                    : profile.Tracks.FirstOrDefault(track => string.Equals(track.Id, selectedTrackId, StringComparison.OrdinalIgnoreCase))
                      ?? profile.Tracks.FirstOrDefault();
            }
            finally
            {
                _studioAnimationUiUpdating = false;
            }
        }

        UpdateStudioRuntimeAnimationReadout();
        if (queuePreview)
            QueueStudioRuntimeAnimationPreview();
    }

    private string ResolveCurrentStudioRuntimeAssetId()
    {
        if (_studioBlockMachineCombo?.SelectedItem is MinecraftBlockAssetDescriptor asset &&
            !string.IsNullOrWhiteSpace(asset.Id))
            return asset.Id.Trim().ToLowerInvariant();

        if (_studioEditableBlockModel is null)
            return string.Empty;

        var fromReference = NormalizeRuntimeAssetId(_studioEditableBlockModel.ModelReference);
        if (!string.IsNullOrWhiteSpace(fromReference))
            return fromReference;

        return NormalizeRuntimeAssetId(Path.GetFileNameWithoutExtension(_studioEditableBlockModel.SourcePath));
    }

    private static string NormalizeRuntimeAssetId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().Replace('\\', '/');
        var colon = normalized.LastIndexOf(':');
        if (colon >= 0 && colon + 1 < normalized.Length)
            normalized = normalized[(colon + 1)..];
        var slash = normalized.LastIndexOf('/');
        if (slash >= 0 && slash + 1 < normalized.Length)
            normalized = normalized[(slash + 1)..];
        if (normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^5];
        if (normalized.EndsWith("_active", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^7];
        return normalized.ToLowerInvariant();
    }
}
