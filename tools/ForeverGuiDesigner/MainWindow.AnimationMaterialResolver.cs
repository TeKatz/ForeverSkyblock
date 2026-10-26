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
        if (target?.Kind != Services.AnimationTargetKind.JsonModelElement ||
            target.ElementIndex is not int targetIndex || targetIndex != elementIndex)
            return sourceTexture;

        var keyedTexture = ResolveStudioMaterialTextureState(_studioAnimationTimeline?.Value ?? 0);
        return IsValidStudioMaterialTextureReference(_studioEditableBlockModel, keyedTexture)
            ? keyedTexture
            : sourceTexture;
    }
}
