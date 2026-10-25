using System.Windows;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Conditional Condenser renderer branches are real runtime animations too. They share one block
/// entity/renderer, so selecting their source track switches the Studio overlay to that exact branch
/// rather than pretending every Condenser job uses the press/roller mechanism.
/// </summary>
public partial class MainWindow
{
    private bool _studioRuntimeAnimationVariantsInitialized;

    public void EnableForeverRuntimeAnimationVariants()
    {
        if (_studioRuntimeAnimationVariantsInitialized || _studioAnimationRuntimeCurveCombo is null)
            return;
        _studioRuntimeAnimationVariantsInitialized = true;

        _studioAnimationRuntimeCurveCombo.SelectionChanged += (_, _) => QueueStudioRuntimeVariantPreview();
        if (_studioAnimationTimeline is not null)
            _studioAnimationTimeline.ValueChanged += (_, _) => QueueStudioRuntimeVariantPreview();
    }

    private void QueueStudioRuntimeVariantPreview()
        => Dispatcher.BeginInvoke(() =>
        {
            if (_studioAnimationSurface?.Visibility == Visibility.Visible)
                RenderStudioRuntimeVariantPreview(_studioAnimationTimeline?.Value ?? 0);
        }, DispatcherPriority.ContextIdle);

    private void RenderStudioRuntimeVariantPreview(double time)
    {
        if (_studioRuntimeAnimationProfile is null ||
            _studioRuntimeAnimationProfile.AssetId is not ("condenser_mark_i" or "condenser_mark_ii") ||
            _studioAnimationRuntimeCurveCombo?.SelectedItem is not ForeverRuntimeAnimationTrack track ||
            _studioAnimationViewport is null || _studioEditableBlockModel is null || string.IsNullOrWhiteSpace(_repoRoot))
            return;

        var mode = track.Id;
        var mixing = mode is "mixer-rotor" or "compost-rotor" or "mixing-orbit" or "secondary-intake" or "secondary-ingredient";
        var garden = mode is "garden-scan" or "garden-harvest";
        var status = mode == "status";
        if (!mixing && !garden && !status)
            return;

        var phase = Math.Clamp(time / Math.Max(.0001, _studioAnimationDurationSeconds), 0, 1);
        var preview = CloneStudioBlockModel(_studioEditableBlockModel);
        var selectedTarget = GetSelectedStudioAnimationTarget();
        if (selectedTarget?.Kind == AnimationTargetKind.JsonModelElement && selectedTarget.ElementIndex is int elementIndex)
        {
            var authoredTrack = FindStudioAnimationTrackForTarget(selectedTarget);
            ApplyStudioAnimationInteractionFrameToElement(preview, elementIndex, EvaluateStudioAnimationFrame(authoredTrack, time));
        }

        if (mixing)
            AddStudioRuntimeCondenserMixing(preview, phase, composting: mode == "compost-rotor");
        else if (garden)
            AddStudioRuntimeCondenserGardenAttachment(preview, phase);
        else
            AddStudioRuntimeCondenserStatus(preview, phase);

        _studioAnimationViewport.SetTextureOverrides(_studioTextureOverrides);
        _studioAnimationViewport.SetModel(_repoRoot, preview, _studioEditableBlockstateXRotation, _studioEditableBlockstateYRotation);
        if (selectedTarget?.Kind == AnimationTargetKind.JsonModelElement && selectedTarget.ElementIndex is int selectedIndex)
            _studioAnimationViewport.SelectElement(selectedIndex);
    }

    private static void AddStudioRuntimeCondenserMixing(MinecraftResolvedBlockModel model, double phase, bool composting)
    {
        AddRuntimeBox(model, 3, 4.8,5.3,1.3,11.2,5.9,3.8,0,[8,7.1,2.5]);
        var angle = phase * (composting ? 720 : 1080);
        AddRuntimeBox(model, 2, 7.78,6.0,2.28,8.22,10.4,2.72, angle, [8,7.1,2.5]);
        AddRuntimeBox(model, 3, 5.7,6.55,2.22,10.3,7.0,2.78, angle, [8,7.1,2.5]);

        var intake = RuntimeSmooth(.02,.30,phase);
        var ingredient = RuntimeSmooth(0,.06,phase) * (1-RuntimeSmooth(.38,.62,phase));
        var orbit = angle * Math.PI / 180d;
        var dx = Math.Cos(orbit) * 1.7 * RuntimeSmooth(.16,.32,phase);
        var feedY = 11.8 - 4.4 * intake;
        var s = Math.Max(.06, .5 * ingredient);
        AddRuntimeBox(model,5,8-dx-s,feedY-s,1.5-s,8-dx+s,feedY+s,1.5+s,0,[8-dx,feedY,1.5]);

        if (!composting)
        {
            var delayed = phase - .035;
            var intake2 = RuntimeSmooth(.02,.30,delayed);
            var ingredient2 = RuntimeSmooth(0,.06,delayed) * (1-RuntimeSmooth(.38,.62,delayed));
            var y2 = 11.8 - 4.4 * intake2;
            var s2 = Math.Max(.06, .5 * ingredient2);
            AddRuntimeBox(model,5,8+dx-s2,y2-s2,1.5-s2,8+dx+s2,y2+s2,1.5+s2,0,[8+dx,y2,1.5]);
        }

        AddStudioRuntimeCondenserProduct(model, phase);
    }

    private static void AddStudioRuntimeCondenserGardenAttachment(MinecraftResolvedBlockModel model, double phase)
    {
        AddRuntimeBox(model,3,4.7,11.3,1.3,11.3,12,3.7,0,[8,11.6,2.5]);
        AddRuntimeBox(model,5,5.2,5.3,1.3,10.8,5.8,3.7,0,[8,5.5,2.5]);
        var growth = RuntimeSmooth(.04,.84,phase);
        var scan = 5 + 5.6 * growth;
        AddRuntimeBox(model,7,scan,10.7,1.35,scan+.35,11.25,3.65,0,[scan,11,2.5]);
        var harvest = RuntimeSmooth(.86,1,phase);
        if (harvest > 0)
            AddRuntimeBox(model,7,5.4,5.9+harvest*4.5,1.1,10.6,6.05+harvest*4.5,1.3,0,[8,6+harvest*4.5,1.2]);
    }

    private static void AddStudioRuntimeCondenserStatus(MinecraftResolvedBlockModel model, double phase)
    {
        for (var i = 0; i < 6; i++)
        {
            var material = phase >= (i + 1) / 6d ? 7 : 4;
            AddRuntimeBox(model,material,4.7+i*1.1,12.2,1.2,5.4+i*1.1,12.5,1.45,0,[8,12.35,1.3]);
        }
    }
}
