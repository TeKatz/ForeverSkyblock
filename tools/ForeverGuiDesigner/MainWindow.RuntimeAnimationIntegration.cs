using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Bridges the animations that already exist in the Minecraft runtime into the Phase-17 Studio.
/// Runtime tracks are source-backed/read-only; Studio keyframes remain an explicit override layer.
/// </summary>
public partial class MainWindow
{
    private bool _studioRuntimeAnimationIntegrationInitialized;
    private bool _studioRuntimeAnimationPreviewQueued;
    private double _studioRuntimeAnimationPreviewTime;
    private ForeverRuntimeAnimationProfile? _studioRuntimeAnimationProfile;

    public void EnableForeverRuntimeAnimationIntegration()
    {
        if (_studioRuntimeAnimationIntegrationInitialized || _studioAnimationRuntimeCurveCombo is null)
            return;
        _studioRuntimeAnimationIntegrationInitialized = true;

        _studioAnimationRuntimeCurveCombo.SelectionChanged += (_, _) =>
        {
            UpdateStudioRuntimeAnimationReadout();
            QueueStudioRuntimeAnimationPreview();
        };
        if (_studioAnimationTimeline is not null)
            _studioAnimationTimeline.ValueChanged += (_, _) =>
            {
                UpdateStudioRuntimeAnimationReadout();
                QueueStudioRuntimeAnimationPreview();
            };
        if (_studioBlockMachineCombo is not null)
            _studioBlockMachineCombo.SelectionChanged += (_, _) => QueueStudioRuntimeAnimationProfileRefresh();
        if (_studioBlockStateCombo is not null)
            _studioBlockStateCombo.SelectionChanged += (_, _) => QueueStudioRuntimeAnimationPreview();
        if (_studioBlockFacingCombo is not null)
            _studioBlockFacingCombo.SelectionChanged += (_, _) => QueueStudioRuntimeAnimationPreview();
        if (_studioAnimationSurface is not null)
            _studioAnimationSurface.IsVisibleChanged += (_, _) => QueueStudioRuntimeAnimationProfileRefresh();

        QueueStudioRuntimeAnimationProfileRefresh();
    }

    private void QueueStudioRuntimeAnimationProfileRefresh()
        => Dispatcher.BeginInvoke(() => RefreshStudioRuntimeAnimationProfile(), DispatcherPriority.ContextIdle);

    private void RefreshStudioRuntimeAnimationProfile()
    {
        var assetId = (_studioBlockMachineCombo?.SelectedItem as MinecraftBlockAssetDescriptor)?.Id ?? string.Empty;
        _studioRuntimeAnimationProfile = ForeverRuntimeAnimationCatalog.ForAsset(assetId);

        if (_studioAnimationRuntimeCurveCombo is not null)
        {
            _studioAnimationUiUpdating = true;
            try
            {
                _studioAnimationRuntimeCurveCombo.ItemsSource = null;
                _studioAnimationRuntimeCurveCombo.DisplayMemberPath = nameof(ForeverRuntimeAnimationTrack.DisplayName);
                _studioAnimationRuntimeCurveCombo.ItemsSource = _studioRuntimeAnimationProfile.Tracks;
                _studioAnimationRuntimeCurveCombo.SelectedIndex = _studioRuntimeAnimationProfile.Tracks.Count > 0 ? 0 : -1;
            }
            finally
            {
                _studioAnimationUiUpdating = false;
            }
        }

        RebuildStudioAnimationTargetRegistry();
        MergeStudioRuntimeTargetsIntoTargetSelector();
        UpdateStudioRuntimeAnimationReadout();
        QueueStudioRuntimeAnimationPreview();
    }

    private void MergeStudioRuntimeTargetsIntoTargetSelector()
    {
        if (_studioEditableBlockModel is null || _studioAnimationElementCombo is null)
            return;

        var selectedId = (_studioAnimationElementCombo.SelectedItem as AnimationTargetDescriptor)?.TargetId.Value;
        var entries = _studioAnimationTargetRegistry.JsonTargetsForModel(_studioEditableBlockModel.SourcePath)
            .Concat(_studioAnimationTargetRegistry.RuntimeTargets())
            .ToList();
        var selected = string.IsNullOrWhiteSpace(selectedId)
            ? -1
            : entries.FindIndex(item => string.Equals(item.TargetId.Value, selectedId, StringComparison.Ordinal));

        _studioAnimationUiUpdating = true;
        try
        {
            _studioAnimationElementCombo.ItemsSource = entries;
            _studioAnimationElementCombo.SelectedIndex = entries.Count == 0 ? -1 : selected >= 0 ? selected : 0;
        }
        finally
        {
            _studioAnimationUiUpdating = false;
        }
    }

    private void UpdateStudioRuntimeAnimationReadout()
    {
        if (_studioAnimationRuntimeValueText is null || _studioRuntimeAnimationProfile is null)
            return;

        if (!_studioRuntimeAnimationProfile.HasRuntimeAnimation)
        {
            _studioAnimationRuntimeValueText.Text =
                $"Existing runtime animation · none · {_studioRuntimeAnimationProfile.CoverageNote}";
            return;
        }

        var track = _studioAnimationRuntimeCurveCombo?.SelectedItem as ForeverRuntimeAnimationTrack
                    ?? _studioRuntimeAnimationProfile.Tracks.FirstOrDefault();
        if (track is null)
            return;

        var phase = Math.Clamp((_studioAnimationTimeline?.Value ?? 0) /
                               Math.Max(.0001, _studioAnimationDurationSeconds), 0, 1);
        var value = track.Evaluate(phase);
        _studioAnimationRuntimeValueText.Text =
            $"RUNTIME SOURCE · {track.SourceSymbol} · phase {phase:0.000} = {value:0.###} · {track.Kind} · {track.SourcePath}";
    }

    private void QueueStudioRuntimeAnimationPreview()
    {
        if (!_studioRuntimeAnimationIntegrationInitialized)
            return;

        // Keep only the newest requested timeline time. Previously every slider/playback tick queued a
        // separate full model rebuild, allowing the Dispatcher backlog to grow faster than WPF could
        // render it. That caused the entire application to stall and old/new frames to alternate.
        _studioRuntimeAnimationPreviewTime = _studioAnimationTimeline?.Value ?? 0;
        if (_studioRuntimeAnimationPreviewQueued)
            return;

        _studioRuntimeAnimationPreviewQueued = true;
        _ = Dispatcher.BeginInvoke(() =>
        {
            _studioRuntimeAnimationPreviewQueued = false;
            if (_studioAnimationSurface?.Visibility == Visibility.Visible)
                RenderStudioRuntimeAnimationPreview(_studioRuntimeAnimationPreviewTime);
        }, DispatcherPriority.Render);
    }

    private void RenderStudioRuntimeAnimationPreview(double time)
    {
        if (_studioRuntimeAnimationProfile is null || _studioAnimationViewport is null ||
            _studioEditableBlockModel is null || string.IsNullOrWhiteSpace(_repoRoot))
            return;

        var phase = Math.Clamp(time / Math.Max(.0001, _studioAnimationDurationSeconds), 0, 1);
        var preview = CloneStudioBlockModel(_studioEditableBlockModel);

        // Preserve the Studio transform override while composing the source-backed runtime layer.
        var selectedTarget = GetSelectedStudioAnimationTarget();
        if (selectedTarget?.Kind == AnimationTargetKind.JsonModelElement && selectedTarget.ElementIndex is int elementIndex)
        {
            var authoredTrack = FindStudioAnimationTrackForTarget(selectedTarget);
            var frame = EvaluateStudioAnimationFrame(authoredTrack, time);
            ApplyStudioAnimationInteractionFrameToElement(preview, elementIndex, frame);
        }

        if (_studioRuntimeAnimationProfile.HasRuntimeAnimation)
        {
            // Runtime boxes use the same atlas resources as the actual BlockEntityRenderer geometry.
            // These slots were previously only injected by the static parity preview, which left
            // Garden/runtime overlays without resolvable materials in the animation viewport.
            AddRuntimeTextureSlots(preview);

            var selectedRuntimeTrack = _studioAnimationRuntimeCurveCombo?.SelectedItem as ForeverRuntimeAnimationTrack;
            switch (_studioRuntimeAnimationProfile.AssetId)
            {
                case "condenser_mark_i":
                    if (!AddStudioRuntimeCondenserVariant(preview, phase, selectedRuntimeTrack?.Id))
                        AddStudioRuntimeCondenserMarkI(preview, phase);
                    break;
                case "condenser_mark_ii":
                    if (!AddStudioRuntimeCondenserVariant(preview, phase, selectedRuntimeTrack?.Id))
                        AddStudioRuntimeCondenserMarkII(preview, phase);
                    break;
                case "ascension_chamber": AddStudioRuntimeAscension(preview, phase); break;
                case "refining_chamber": AddStudioRuntimeRefining(preview, phase); break;
                case "garden_chamber_mark_i": AddStudioRuntimeGarden(preview, phase); break;
                case "washing_basin": AddStudioRuntimeWashing(preview, phase); break;
                case "crucible": AddStudioRuntimeCrucible(preview, phase); break;
            }
        }

        // Animation playback now keeps the existing scene/material graph alive. A full rebuild is
        // performed only when topology actually changes (machine/track/state switch); all following
        // frames update only mesh coordinates and transforms.
        if (!_studioAnimationViewport.TryUpdateAnimatedModel(
                _repoRoot,
                preview,
                _studioEditableBlockstateXRotation,
                _studioEditableBlockstateYRotation))
        {
            _studioAnimationViewport.SetTextureOverrides(_studioTextureOverrides);
            _studioAnimationViewport.SetModel(
                _repoRoot,
                preview,
                _studioEditableBlockstateXRotation,
                _studioEditableBlockstateYRotation,
                forceRebuild: true);
        }

        if (selectedTarget?.Kind == AnimationTargetKind.JsonModelElement && selectedTarget.ElementIndex is int selectedIndex)
            _studioAnimationViewport.SelectElement(selectedIndex);
    }

    private static double RuntimeSmooth(double from, double to, double phase)
    {
        var t = Math.Clamp((phase - from) / (to - from), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static double RuntimePress(double phase)
        => RuntimeSmooth(.16, .48, phase) * (1 - RuntimeSmooth(.64, .83, phase));

    private static void AddStudioRuntimeCondenserMarkI(MinecraftResolvedBlockModel model, double phase)
    {
        var stroke = RuntimePress(phase);
        var jaw = 10.4 - 2.6 * stroke;
        AddRuntimeBox(model, 3, 4.6, jaw, 1.25, 11.4, jaw + 1.1, 3.8, 0, [8,8,2.5]);
        AddRuntimeBox(model, 2, 7.3, jaw + 1.1, 1.8, 8.7, 12.5, 3.2, 0, [8,8,2.5]);

        var intake = RuntimeSmooth(.02, .30, phase);
        var ingredient = RuntimeSmooth(0, .06, phase) * (1 - RuntimeSmooth(.38, .62, phase));
        var inputX = 3.8 + 4.2 * intake;
        var inputSize = Math.Max(.08, .55 * ingredient * (1 - .31 * stroke));
        AddRuntimeBox(model, 5, inputX-inputSize, 6.6-inputSize, 1.5-inputSize, inputX+inputSize, 6.6+inputSize, 1.5+inputSize, 0, [inputX,6.6,1.5]);
        AddStudioRuntimeCondenserProduct(model, phase);
    }

    private static void AddStudioRuntimeCondenserMarkII(MinecraftResolvedBlockModel model, double phase)
    {
        var angle = phase * 720;
        AddRuntimeBox(model, 3, 5.05, 7.15, 1.35, 7.15, 9.25, 3.65, angle, [6.1,8.2,2.5]);
        AddRuntimeBox(model, 3, 8.85, 7.15, 1.35, 10.95, 9.25, 3.65, -angle, [9.9,8.2,2.5]);
        var intake = RuntimeSmooth(.02, .30, phase);
        var ingredient = RuntimeSmooth(0, .06, phase) * (1 - RuntimeSmooth(.38, .62, phase));
        var y = 11.8 - 3.1 * intake;
        var s = Math.Max(.08, .65 * ingredient);
        AddRuntimeBox(model, 5, 8-s,y-s,2.3-s,8+s,y+s,2.3+s,0,[8,y,2.3]);
        AddStudioRuntimeCondenserProduct(model, phase);
    }

    private static void AddStudioRuntimeCondenserProduct(MinecraftResolvedBlockModel model, double phase)
    {
        var product = RuntimeSmooth(.64, .82, phase) * (1 - RuntimeSmooth(.92, 1, phase));
        var exit = RuntimeSmooth(.84, 1, phase);
        var y = 5.8 + (1.85 - 5.8) * exit;
        var z = 1.35 + 1.35 * exit;
        var s = .55 * product;

        // Keep topology stable across the whole cycle. At zero product the box collapses to one point
        // instead of being removed, allowing the viewport to keep the same GeometryModel3D objects.
        AddRuntimeBox(model, 7, 8-s,y-s,z-s,8+s,y+s,z+s,0,[8,y,z]);
    }

    private static void AddStudioRuntimeAscension(MinecraftResolvedBlockModel model, double phase)
    {
        AddRuntimeRing(model, 4.5, 2.8, phase * 180, 7);
        AddRuntimeRing(model, 3.15, 1.6, -phase * 360, 7);
        var bob = Math.Sin(phase * Math.PI * 4) * .18;
        var tilt = Math.Sin(phase * Math.PI * 2) * 9;
        AddRuntimeBox(model, 32, 6.6,6.6+bob,1.5,9.4,9.4+bob,3.4,tilt,[8,8+bob,2.5]);
        for (var i = 0; i < 6; i++)
        {
            var angle = phase * Math.PI * 4 + i * Math.PI / 3;
            var x = 8 + Math.Cos(angle) * 2.3;
            var y = 8 + Math.Sin(angle) * 2.3;
            AddRuntimeBox(model, 7, x,y,2.1,x+.13,y+.13,2.25,0,[x,y,2.1]);
        }
    }

    private static void AddStudioRuntimeRefining(MinecraftResolvedBlockModel model, double phase)
    {
        AddStudioRuntimeRefiningSide(model, 2.15, false, phase);
        AddStudioRuntimeRefiningSide(model, 13.85, true, phase);
    }

    private static void AddStudioRuntimeRefiningSide(MinecraftResolvedBlockModel model, double z, bool back, double phase)
    {
        var outer = phase * 720 * (back ? -1 : 1);
        for (var i = 0; i < 4; i++)
            AddRuntimeBox(model, 34,5,5,z-.3,11,5.3,z+.3,outer+i*90,[8,8,z]);
        AddRuntimeBox(model,7,7.1,7.1,z-.5,8.9,8.9,z+.5,(back?-45:45) - phase*360,[8,8,z]);
    }

    private static void AddStudioRuntimeGarden(MinecraftResolvedBlockModel model, double phase)
    {
        PrepareStudioRuntimeGardenVisibility(model);

        var growth = RuntimeSmooth(.04, .84, phase);
        var z = 3.1 + 9.4 * growth;
        AddRuntimeBox(model,3,2.1,13.4,z,13.9,13.95,z+.4,0,[8,13.6,z]);
        AddRuntimeBox(model,7,3.1,13.15,z+.05,12.9,13.4,z+.35,0,[8,13.3,z]);

        // Source-derived generic rooted specimen proxy. Actual crop/tree meshes depend on the linked ItemStack.
        var height = .6 + 5.5 * growth;
        AddRuntimeBox(model,5,7.75,3.25,7.75,8.25,3.25+height,8.25,0,[8,3.25,8]);
        for (var i = 0; i < 4; i++)
        {
            var fall = ((phase * 12 + i * .25) % 1 + 1) % 1;
            var x = i % 2 == 0 ? 3.5 : 12.2;
            var zz = i < 2 ? 3.5 : 12.2;
            var y = 12.7 - fall * 8.6;
            AddRuntimeBox(model,7,x,y,zz,x+.12,y+.3,zz+.12,0,[x,y,zz]);
        }

        var harvest = RuntimeSmooth(.86,1,phase);
        var sweep = 3.4 + harvest * 8.8;
        if (harvest > 0)
        {
            AddRuntimeBox(model,7,2.7,sweep,2.7,13.3,sweep+.12,2.85,0,[8,sweep,2.8]);
            AddRuntimeBox(model,7,2.7,sweep,13.15,13.3,sweep+.12,13.3,0,[8,sweep,13.2]);
        }
        else
        {
            // Preserve both harvest elements before the source condition becomes active. A fully
            // collapsed box is invisible but keeps the per-frame mesh topology constant.
            AddRuntimeBox(model,7,8,sweep,2.775,8,sweep,2.775,0,[8,sweep,2.775]);
            AddRuntimeBox(model,7,8,sweep,13.225,8,sweep,13.225,0,[8,sweep,13.225]);
        }
    }

    private static void PrepareStudioRuntimeGardenVisibility(MinecraftResolvedBlockModel model)
    {
        // WPF 3D writes transparent materials into the depth buffer. The Garden Chamber is the only
        // runtime-animated machine whose moving renderer parts live almost completely behind a closed
        // glass shell, so drawing the JSON glass first can hide every subsequently appended runtime
        // element. Keep the source model untouched and collapse only glass-only elements in this
        // cloned Animation-Studio preview. Element/face counts remain stable, so the incremental
        // animation path keeps its no-rebuild performance characteristics.
        foreach (var element in model.Elements)
        {
            if (element.Faces.Count == 0 ||
                !element.Faces.Values.All(face => IsStudioGardenGlassReference(model, face.Texture)))
                continue;

            var centerX = (element.From[0] + element.To[0]) / 2.0;
            var centerY = (element.From[1] + element.To[1]) / 2.0;
            var centerZ = (element.From[2] + element.To[2]) / 2.0;
            element.From[0] = centerX;
            element.From[1] = centerY;
            element.From[2] = centerZ;
            element.To[0] = centerX;
            element.To[1] = centerY;
            element.To[2] = centerZ;
        }
    }

    private static bool IsStudioGardenGlassReference(MinecraftResolvedBlockModel model, string textureReference)
    {
        var resolved = textureReference;
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (resolved.StartsWith('#'))
        {
            var key = resolved[1..];
            if (!visited.Add(key) || !model.Textures.TryGetValue(key, out var next) || string.IsNullOrWhiteSpace(next))
                break;
            resolved = next;
        }

        return resolved.Contains("garden_glass", StringComparison.OrdinalIgnoreCase) ||
               resolved.EndsWith("/glass", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddStudioRuntimeWashing(MinecraftResolvedBlockModel model, double phase)
    {
        AddRuntimeBox(model,4,3.05,9.1,3.05,12.95,9.2,12.95,0,[8,9.15,8]);
        for (var i = 0; i < 3; i++)
        {
            var wave = ((phase * 9 + i / 3d) % 1 + 1) % 1;
            var r = 1 + 3.5 * wave;
            var t = .04 + .09 * Math.Sin(wave * Math.PI);
            AddRuntimeBox(model,7,8-r,9.22,8-r,8+r,9.25,8-r+t,0,[8,9.23,8]);
            AddRuntimeBox(model,7,8-r,9.22,8+r-t,8+r,9.25,8+r,0,[8,9.23,8]);
            AddRuntimeBox(model,7,8-r,9.22,8-r,8-r+t,9.25,8+r,0,[8,9.23,8]);
            AddRuntimeBox(model,7,8+r-t,9.22,8-r,8+r,9.25,8+r,0,[8,9.23,8]);
        }
    }

    private void AddStudioRuntimeCrucible(MinecraftResolvedBlockModel model, double phase)
    {
        var state = _studioBlockStateCombo?.SelectedItem?.ToString() ?? string.Empty;
        var fill = ParseRuntimeStateInt(state, "fill", 1);
        if (fill <= 0)
            return;
        var top = 4.35 + fill * 1.05;
        AddRuntimeBox(model,6,3.1,4.15,3.1,12.9,top,12.9,0,[8,top,8]);
        AddRuntimeBox(model,7,2.95,top,2.95,13.05,top+.14,13.05,0,[8,top,8]);
        for (var i = 0; i < 4; i++)
        {
            var drift = ((phase + i * .25) % 1 + 1) % 1;
            var size = .12 + .28 * Math.Sin(Math.PI * drift);
            var x = 4.2 + i * 2.2;
            var z = 6 + Math.Sin(i * 2.4 + drift * Math.PI * 2) * 2;
            AddRuntimeBox(model,7,x-size,top+.15,z-size,x+size,top+.18,z+size,0,[x,top,z]);
        }
    }

    private static int ParseRuntimeStateInt(string state, string property, int fallback)
    {
        foreach (var token in state.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = token.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && parts[0].Equals(property, StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                return value;
        }
        return fallback;
    }
}
