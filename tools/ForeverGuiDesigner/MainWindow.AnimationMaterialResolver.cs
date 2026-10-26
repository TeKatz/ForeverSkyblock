using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Binds the Phase 17C.3 material timeline into the viewport's effective face-texture resolver.
/// The source/resolved model keeps its original face texture references, so the 17B base pass and
/// 17C runtime pass no longer fight over material objects on every playback frame.
/// </summary>
public partial class MainWindow
{
    private bool _studioAnimationMaterialResolverInitialized;

    public void EnableForeverAnimationMaterialResolver()
    {
        if (_studioAnimationMaterialResolverInitialized || _studioAnimationViewport is null)
            return;

        _studioAnimationMaterialResolverInitialized = true;
        _studioAnimationViewport.FaceTextureResolver = ResolveStudioAnimationFaceTexture;
    }

    private string ResolveStudioAnimationFaceTexture(int elementIndex, string direction, string sourceTexture)
    {
        if (!_studioAnimationMaterialEffectsInitialized || _studioEditableBlockModel is null)
            return sourceTexture;

        var target = GetSelectedStudioAnimationTarget();
        if (target?.Kind != AnimationTargetKind.JsonModelElement ||
            target.ElementIndex is not int targetIndex || targetIndex != elementIndex)
            return sourceTexture;

        // A hidden target is physically removed from the preview list. Never apply its material to
        // the next element that shifts into the same numeric index after that removal.
        var time = _studioAnimationTimeline?.Value ?? 0;
        var track = FindStudioAnimationTrackForTarget(target);
        var frame = EvaluateStudioAnimationFrame(track, time);
        if (!frame.Visible)
            return sourceTexture;

        var keyedTexture = frame.TextureState?.Trim() ?? string.Empty;
        return IsValidStudioMaterialTextureReference(_studioEditableBlockModel, keyedTexture)
            ? keyedTexture
            : sourceTexture;
    }
}
