using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase 17D.4 parity preview. Source renderer geometry is composed first; Studio-authored transforms
/// are then applied as an outer WPF transform around the exact Phase-17D.3 Java binding pivot. This
/// preserves source animation, source rotations and mesh topology while allowing all authored JSON
/// and runtime targets to play together.
/// </summary>
public partial class MainWindow
{
    private sealed record StudioRuntimeTargetPreviewBinding(
        string TargetId,
        int StartElementIndex,
        int ElementCount,
        Point3D Pivot);

    private bool _studioRuntimeTargetPreviewInitialized;
    private bool _studioRuntimeTargetPreviewQueued;
    private double _studioRuntimeTargetPreviewTime;
    private int _studioRuntimeTargetPreviewBaseElementCount;
    private readonly List<StudioRuntimeTargetPreviewBinding> _studioRuntimeTargetPreviewBindings = [];

    public void EnableForeverAnimationRuntimeTargetPreview()
    {
        if (_studioRuntimeTargetPreviewInitialized || !_studioRuntimeTargetAuthoringInitialized ||
            _studioAnimationViewport is null)
            return;

        _studioRuntimeTargetPreviewInitialized = true;

        // Phase 17D.4 makes material evaluation global by stable JSON target instead of tying it to
        // whichever target happens to be selected. Runtime components deliberately keep source
        // materials; Java 17D.3 consumes transform/visibility only for runtime TargetIds.
        _studioAnimationViewport.FaceTextureResolver = ResolveStudioAnimationFaceTexturePhase17D4;
        QueueStudioRuntimeTargetPreview();
    }

    private string ResolveStudioAnimationFaceTexturePhase17D4(int elementIndex, string direction, string sourceTexture)
    {
        if (!_studioAnimationMaterialEffectsInitialized || _studioEditableBlockModel is null ||
            elementIndex < 0 || elementIndex >= _studioEditableBlockModel.Elements.Count)
            return sourceTexture;

        var target = _studioAnimationTargetRegistry
            .JsonTargetsForModel(_studioEditableBlockModel.SourcePath)
            .FirstOrDefault(candidate => candidate.ElementIndex == elementIndex);
        if (target is null)
            return sourceTexture;

        var frame = EvaluateStudioAnimationFrame(
            FindStudioAnimationTrackForTarget(target),
            _studioAnimationTimeline?.Value ?? 0);
        var keyedTexture = frame.TextureState?.Trim() ?? string.Empty;
        return IsValidStudioMaterialTextureReference(_studioEditableBlockModel, keyedTexture)
            ? keyedTexture
            : sourceTexture;
    }

    private void QueueStudioRuntimeTargetPreview()
    {
        if (!_studioRuntimeTargetPreviewInitialized && !_studioRuntimeTargetAuthoringInitialized)
            return;

        _studioRuntimeTargetPreviewTime = _studioAnimationTimeline?.Value ?? 0;
        if (_studioRuntimeTargetPreviewQueued)
            return;
        _studioRuntimeTargetPreviewQueued = true;
        _ = Dispatcher.BeginInvoke(() =>
        {
            _studioRuntimeTargetPreviewQueued = false;
            if (_studioAnimationSurface?.Visibility == Visibility.Visible)
                RenderStudioRuntimeTargetPreviewNow(_studioRuntimeTargetPreviewTime);
        }, DispatcherPriority.Render);
    }

    private void RenderStudioRuntimeTargetPreviewNow(double time)
    {
        if (_studioAnimationViewport is null || _studioEditableBlockModel is null ||
            string.IsNullOrWhiteSpace(_repoRoot))
            return;

        RebuildStudioAnimationTargetRegistry();
        var assetId = ResolveCurrentStudioRuntimeAssetId();
        var phase = Math.Clamp(time / Math.Max(.0001, _studioAnimationDurationSeconds), 0, 1);
        var preview = CloneStudioBlockModel(_studioEditableBlockModel);
        AddRuntimeTextureSlots(preview);
        _studioRuntimeTargetPreviewBaseElementCount = preview.Elements.Count;

        var statusRestore = OverrideStudioRuntimeStatusesAtTime(assetId, time);
        try
        {
            ComposeStudioRuntimeState(preview, assetId, phase);
            BuildStudioRuntimeTargetPreviewBindings(preview, assetId, phase, _studioRuntimeTargetPreviewBaseElementCount);
        }
        finally
        {
            RestoreStudioRuntimeStatusSelections(statusRestore);
        }

        _studioAnimationViewport.SetTextureOverrides(_studioTextureOverrides);
        if (!_studioAnimationViewport.TryUpdateAnimatedModel(
                _repoRoot,
                preview,
                _studioEditableBlockstateXRotation,
                _studioEditableBlockstateYRotation))
        {
            _studioAnimationViewport.SetModel(
                _repoRoot,
                preview,
                _studioEditableBlockstateXRotation,
                _studioEditableBlockstateYRotation,
                forceRebuild: true);
        }

        ApplyStudioAuthoredOuterTransforms(time);
        SyncStudioRuntimeTargetViewportSelection(time);
        UpdateStudioRuntimeTargetSelectionOverlay();
    }

    private Dictionary<string, (bool HadValue, string Value)> OverrideStudioRuntimeStatusesAtTime(string assetId, double time)
    {
        var restore = new Dictionary<string, (bool HadValue, string Value)>(StringComparer.OrdinalIgnoreCase);
        var profile = ForeverRuntimeStateCatalog.ForAsset(assetId);
        foreach (var axis in profile.Axes.Where(IsStudioRuntimeStatusAxis))
        {
            var key = RuntimeStateSelectionKey(profile.AssetId, axis.Id);
            var hadValue = _studioRuntimeStateSelections.TryGetValue(key, out var previous);
            restore[key] = (hadValue, previous ?? string.Empty);
            _studioRuntimeStateSelections[key] = ResolveStudioRuntimeStatusEffectiveOptionId(profile.AssetId, axis, time);
        }
        return restore;
    }

    private void RestoreStudioRuntimeStatusSelections(Dictionary<string, (bool HadValue, string Value)> restore)
    {
        foreach (var pair in restore)
        {
            if (pair.Value.HadValue)
                _studioRuntimeStateSelections[pair.Key] = pair.Value.Value;
            else
                _studioRuntimeStateSelections.Remove(pair.Key);
        }
    }

    private void BuildStudioRuntimeTargetPreviewBindings(
        MinecraftResolvedBlockModel preview,
        string assetId,
        double phase,
        int baseElementCount)
    {
        _studioRuntimeTargetPreviewBindings.Clear();
        var cursor = baseElementCount;

        switch (assetId)
        {
            case "condenser_mark_i":
                BuildStudioCondenserRuntimeBindings(assetId, phase, tier: 1, ref cursor);
                break;
            case "condenser_mark_ii":
                BuildStudioCondenserRuntimeBindings(assetId, phase, tier: 2, ref cursor);
                break;
            case "ascension_chamber":
                BuildStudioAscensionRuntimeBindings(phase, ref cursor);
                break;
            case "refining_chamber":
                BuildStudioRefiningRuntimeBindings(ref cursor);
                break;
            case "garden_chamber_mark_i":
                BuildStudioGardenRuntimeBindings(phase, ref cursor);
                break;
            case "washing_basin":
                BuildStudioWashingRuntimeBindings(phase, ref cursor);
                break;
            case "crucible":
                BuildStudioCrucibleRuntimeBindings(phase, ref cursor);
                break;
        }

        if (cursor == preview.Elements.Count)
            return;

        // Fail closed. A source-composer topology change must never make an authored TargetId jump to
        // a neighboring runtime primitive. The geometry still renders; only D.4 outer transforms are
        // withheld until the explicit binding map is brought back into parity.
        _studioRuntimeTargetPreviewBindings.Clear();
        if (TryGetSelectedStudioRuntimeAuthoringTarget(out _))
        {
            SetStudioAnimationStatus(
                $"Runtime preview binding mismatch · mapped {cursor - baseElementCount} of {preview.Elements.Count - baseElementCount} appended element(s). Source geometry remains visible; authored runtime transforms were not applied.",
                "#E18B8B");
        }
    }

    private void AddStudioRuntimeTargetPreviewBinding(
        string assetId,
        string componentKey,
        ref int cursor,
        int count,
        Point3D pivot)
    {
        if (count <= 0)
            return;

        var target = _studioAnimationTargetRegistry.RuntimeTargets().FirstOrDefault(candidate =>
            candidate.RuntimeTarget is ForeverRuntimeAnimationProfile profile &&
            profile.AssetId.Equals(assetId, StringComparison.OrdinalIgnoreCase) &&
            candidate.Scope.Equals(profile.AssetId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.RuntimeComponentKey, componentKey, StringComparison.OrdinalIgnoreCase));
        if (target is not null)
        {
            _studioRuntimeTargetPreviewBindings.Add(new StudioRuntimeTargetPreviewBinding(
                target.TargetId.Value,
                cursor,
                count,
                pivot));
        }
        cursor += count;
    }

    private static void SkipStudioRuntimePreviewElements(ref int cursor, int count)
        => cursor += Math.Max(0, count);

    private void BuildStudioCondenserRuntimeBindings(string assetId, double phase, int tier, ref int cursor)
    {
        var status = GetStudioRuntimeState(assetId, "status", "running");
        var job = GetStudioRuntimeState(assetId, "job", "crushing");
        var attachment = GetStudioRuntimeState(assetId, "attachment", "none");
        var specimen = GetStudioRuntimeState(assetId, "specimen", "oak");
        var running = status == "running";
        var hasGarden = attachment is "garden" or "garden+freezing";
        var hasFreezing = attachment is "freezing" or "garden+freezing";

        if (hasGarden)
        {
            // Controller attachment housing + planter are renderer-owned but have no published target.
            SkipStudioRuntimePreviewElements(ref cursor, 2);
            if (specimen != "none")
                SkipStudioRuntimePreviewElements(ref cursor, 1);

            if (running && IsStudioRuntimeEffectEnabled(assetId, "garden-scan", sourcePredicate: true))
            {
                var scan = 5 + 5.6 * RuntimeSmooth(.04, .84, phase);
                AddStudioRuntimeTargetPreviewBinding(
                    assetId, "condenser_garden_scan", ref cursor, 1,
                    new Point3D(scan, 11, 2.5));
            }
            if (running && IsStudioRuntimeEffectEnabled(assetId, "garden-harvest", sourcePredicate: true))
            {
                var harvest = RuntimeSmooth(.86, 1, phase);
                var harvestY = 5.9 + harvest * 4.5;
                AddStudioRuntimeTargetPreviewBinding(
                    assetId, "condenser_garden_harvest", ref cursor, 1,
                    new Point3D(8, harvestY + .075, 1.2));
            }
        }
        else
        {
            var hasJob = job != "idle";
            var mixing = job is "mixing" or "cultivating";
            var organic = job == "composting";

            if (mixing || organic)
            {
                SkipStudioRuntimePreviewElements(ref cursor, 1); // mixer housing
                AddStudioRuntimeTargetPreviewBinding(
                    assetId, "condenser_mixer", ref cursor, 2,
                    new Point3D(8, 7.1, 2.5));

                var angle = phase * (organic ? 720 : 1080);
                var orbit = angle * Math.PI / 180d;
                var dx = Math.Cos(orbit) * 1.7 * RuntimeSmooth(.16, .32, phase);
                var feedY = 11.8 - 4.4 * RuntimeSmooth(.02, .30, phase);
                AddStudioRuntimeTargetPreviewBinding(
                    assetId, "condenser_input", ref cursor, 1,
                    new Point3D(8 - dx, feedY, 1.5));

                if (!organic)
                {
                    var delayed = phase - .035;
                    var secondaryY = 11.8 - 4.4 * RuntimeSmooth(.02, .30, delayed);
                    AddStudioRuntimeTargetPreviewBinding(
                        assetId, "condenser_secondary", ref cursor, 1,
                        new Point3D(8 + dx, secondaryY, 1.5));
                }

                var resultPivot = ResolveStudioCondenserResultPivot(phase, job == "cultivating");
                AddStudioRuntimeTargetPreviewBinding(
                    assetId, "condenser_result", ref cursor, 1, resultPivot);
            }
            else if (tier >= 2)
            {
                AddStudioRuntimeTargetPreviewBinding(
                    assetId, "condenser_roller_left", ref cursor, 1,
                    new Point3D(6.1, 8.2, 2.5));
                AddStudioRuntimeTargetPreviewBinding(
                    assetId, "condenser_roller_right", ref cursor, 1,
                    new Point3D(9.9, 8.2, 2.5));
                var inputY = hasJob ? 11.8 - 3.1 * RuntimeSmooth(.02, .30, phase) : 10.6;
                AddStudioRuntimeTargetPreviewBinding(
                    assetId, "condenser_input", ref cursor, 1,
                    new Point3D(8, inputY, 2.3));
                if (hasJob)
                {
                    AddStudioRuntimeTargetPreviewBinding(
                        assetId, "condenser_result", ref cursor, 1,
                        ResolveStudioCondenserResultPivot(phase, cultivating: false));
                }
            }
            else
            {
                AddStudioRuntimeTargetPreviewBinding(
                    assetId, "condenser_press", ref cursor, 2,
                    new Point3D(8, 8, 2.5));
                var inputX = hasJob ? 3.8 + 4.2 * RuntimeSmooth(.02, .30, phase) : 8;
                AddStudioRuntimeTargetPreviewBinding(
                    assetId, "condenser_input", ref cursor, 1,
                    new Point3D(inputX, 6.6, 1.5));
                if (hasJob)
                {
                    AddStudioRuntimeTargetPreviewBinding(
                        assetId, "condenser_result", ref cursor, 1,
                        ResolveStudioCondenserResultPivot(phase, cultivating: false));
                }
            }

            if (IsStudioRuntimeEffectEnabled(assetId, "process-dust", running))
            {
                for (var i = 0; i < 4; i++)
                {
                    var fall = ((phase * 5 + i * .23) % 1 + 1) % 1;
                    var strength = RuntimeSmooth(.38, .46, phase) *
                                   (1 - RuntimeSmooth(.76, .84, phase)) *
                                   Math.Sin(Math.PI * fall);
                    var size = Math.Max(0, .18 * strength);
                    var x = 5.8 + i * 1.4;
                    var y = 7.2 - 1.7 * fall;
                    if (i < 3)
                    {
                        AddStudioRuntimeTargetPreviewBinding(
                            assetId, $"condenser_dust_{i}", ref cursor, 1,
                            new Point3D(x + size / 2, y + size / 2, 1.4 + size / 2));
                    }
                    else
                    {
                        // Source has particle four but 17A intentionally has no dust_3 target.
                        SkipStudioRuntimePreviewElements(ref cursor, 1);
                    }
                }
            }
        }

        AddStudioRuntimeTargetPreviewBinding(
            assetId, "condenser_status", ref cursor, 6,
            new Point3D(8, 12.35, 1.325));

        if (!hasFreezing)
            return;
        SkipStudioRuntimePreviewElements(ref cursor, 1); // freezing backing plate
        AddStudioRuntimeTargetPreviewBinding(
            assetId, "condenser_freezing_fill", ref cursor, 1,
            new Point3D(1.05, 8.15, 1.05));
    }

    private static Point3D ResolveStudioCondenserResultPivot(double phase, bool cultivating)
    {
        var product = cultivating
            ? RuntimeSmooth(.44, .82, phase)
            : RuntimeSmooth(.64, .82, phase);
        var exit = RuntimeSmooth(.84, 1, phase);
        product *= 1 - RuntimeSmooth(.92, 1, phase);
        var startY = cultivating ? 7.4 + product * 1.2 : 5.8;
        var resultY = startY + (1.85 - startY) * exit;
        var resultZ = 1.35 + 1.35 * exit;
        return new Point3D(8, resultY, resultZ);
    }

    private void BuildStudioAscensionRuntimeBindings(double phase, ref int cursor)
    {
        const string assetId = "ascension_chamber";
        var active = GetStudioRuntimeState(assetId, "activity", "active") == "active";
        var corePresent = GetStudioRuntimeState(assetId, "core", "present") == "present";
        var sourcePhase = active ? phase : 0;

        AddStudioRuntimeTargetPreviewBinding(
            assetId, "ascension_outer_ring", ref cursor, 8,
            new Point3D(8, 8, 2.8));
        AddStudioRuntimeTargetPreviewBinding(
            assetId, "ascension_inner_ring", ref cursor, 8,
            new Point3D(8, 8, 1.6));

        if (corePresent)
        {
            var bob = active ? Math.Sin(sourcePhase * Math.PI * 4) * .18 : 0;
            AddStudioRuntimeTargetPreviewBinding(
                assetId, "ascension_core", ref cursor, 1,
                new Point3D(8, 8 + bob, 2.5));
        }

        if (IsStudioRuntimeEffectEnabled(assetId, "orbit-particles", active))
        {
            AddStudioRuntimeTargetPreviewBinding(
                assetId, "ascension_particles", ref cursor, 6,
                new Point3D(8, 8, 2.5));
        }
    }

    private void BuildStudioRefiningRuntimeBindings(ref int cursor)
    {
        const string assetId = "refining_chamber";
        AddStudioRuntimeTargetPreviewBinding(
            assetId, "refining_outer_front", ref cursor, 8,
            new Point3D(8, 8, 2.15));
        AddStudioRuntimeTargetPreviewBinding(
            assetId, "refining_core_front", ref cursor, 1,
            new Point3D(8, 8, 2.15));
        AddStudioRuntimeTargetPreviewBinding(
            assetId, "refining_outer_back", ref cursor, 8,
            new Point3D(8, 8, 13.85));
        AddStudioRuntimeTargetPreviewBinding(
            assetId, "refining_core_back", ref cursor, 1,
            new Point3D(8, 8, 13.85));
    }

    private void BuildStudioGardenRuntimeBindings(double phase, ref int cursor)
    {
        const string assetId = "garden_chamber_mark_i";
        var linked = GetStudioRuntimeState(assetId, "link", "linked") == "linked";
        if (!linked)
            return;

        var status = GetStudioRuntimeState(assetId, "status", "running");
        var specimen = GetStudioRuntimeState(assetId, "specimen", "oak");
        var specimenPresent = specimen != "none";
        var running = status == "running" && specimenPresent;
        var growth = RuntimeSmooth(.04, .84, phase);
        var z = 3.1 + 9.4 * growth;

        AddStudioRuntimeTargetPreviewBinding(
            assetId, "garden_lamp", ref cursor, 2,
            new Point3D(8, 13.55, z + .2));

        if (specimenPresent)
        {
            AddStudioRuntimeTargetPreviewBinding(
                assetId, "garden_plant", ref cursor, StudioGardenTreeParts.Count,
                new Point3D(8, 3.25, 8));
        }

        SkipStudioRuntimePreviewElements(ref cursor, 4); // status strips are source presentation

        if (running && IsStudioRuntimeEffectEnabled(assetId, "irrigation-drops", sourcePredicate: true))
        {
            AddStudioRuntimeTargetPreviewBinding(
                assetId, "garden_drops", ref cursor, 4,
                new Point3D(8, 8, 8));
        }

        if (running && IsStudioRuntimeEffectEnabled(assetId, "harvest-sweep", sourcePredicate: true))
        {
            var harvest = RuntimeSmooth(.86, 1, phase);
            var sweep = 3.4 + harvest * 8.8;
            AddStudioRuntimeTargetPreviewBinding(
                assetId, "garden_harvest", ref cursor, 2,
                new Point3D(8, sweep, 8));
        }
    }

    private void BuildStudioWashingRuntimeBindings(double phase, ref int cursor)
    {
        const string assetId = "washing_basin";
        var amount = GetStudioRuntimeState(assetId, "amount", "present");
        if (amount == "dry")
            return;

        AddStudioRuntimeTargetPreviewBinding(
            assetId, "washing_surface", ref cursor, 1,
            new Point3D(8, 9.15, 8));
        if (!IsStudioRuntimeEffectEnabled(assetId, "circulation-rings", sourcePredicate: true))
            return;

        for (var i = 0; i < 3; i++)
        {
            AddStudioRuntimeTargetPreviewBinding(
                assetId, $"washing_ring_{i}", ref cursor, 4,
                new Point3D(8, 9.23, 8));
        }
    }

    private void BuildStudioCrucibleRuntimeBindings(double phase, ref int cursor)
    {
        const string assetId = "crucible";
        var fillId = GetStudioRuntimeState(assetId, "fill", "4");
        if (!int.TryParse(fillId, out var fill) || fill <= 0)
            return;

        fill = Math.Clamp(fill, 1, 8);
        var top = 4.35 + fill * 1.05;
        AddStudioRuntimeTargetPreviewBinding(
            assetId, "crucible_lava_surface", ref cursor, 2,
            new Point3D(8, (4.15 + top) / 2, 8));
        if (!IsStudioRuntimeEffectEnabled(assetId, "thermal-convection", sourcePredicate: true))
            return;

        for (var i = 0; i < 4; i++)
        {
            var drift = ((phase + i * .25) % 1 + 1) % 1;
            var x = 4.2 + i * 2.2;
            var z = 6 + Math.Sin(i * 2.4 + drift * Math.PI * 2) * 2;
            AddStudioRuntimeTargetPreviewBinding(
                assetId, $"crucible_convection_{i}", ref cursor, 1,
                new Point3D(x, top + .165, z));
        }
    }

    private void ApplyStudioAuthoredOuterTransforms(double time)
    {
        var content = GetStudioAnimationViewportContentGroup();
        if (content is null)
            return;

        // Apply every resolved JSON track simultaneously. Keeping every base element in the scene and
        // collapsing hidden targets in WPF fixes index shifting and lets the material resolver keep
        // stable source indices across multi-target playback.
        foreach (var track in _studioAnimationTracks.Values.Distinct())
        {
            var resolution = ResolveStudioAnimationTrack(track);
            if (resolution.Target?.Kind != AnimationTargetKind.JsonModelElement ||
                resolution.Target.ElementIndex is not int elementIndex ||
                elementIndex < 0 || elementIndex >= _studioEditableBlockModel!.Elements.Count ||
                elementIndex >= content.Children.Count)
                continue;

            var source = _studioEditableBlockModel.Elements[elementIndex];
            var pivot = new Point3D(
                (source.From[0] + source.To[0]) / 2,
                (source.From[1] + source.To[1]) / 2,
                (source.From[2] + source.To[2]) / 2);
            ApplyStudioAuthoredOuterTransformToElementGroup(
                content,
                elementIndex,
                EvaluateStudioAnimationFrame(track, time),
                pivot);
        }

        foreach (var binding in _studioRuntimeTargetPreviewBindings)
        {
            var track = _studioAnimationTracks.Values.Distinct().FirstOrDefault(candidate =>
                string.Equals(candidate.TargetId, binding.TargetId, StringComparison.Ordinal));
            if (track is null)
                continue;
            var frame = EvaluateStudioAnimationFrame(track, time);
            for (var index = binding.StartElementIndex;
                 index < binding.StartElementIndex + binding.ElementCount && index < content.Children.Count;
                 index++)
            {
                ApplyStudioAuthoredOuterTransformToElementGroup(content, index, frame, binding.Pivot);
            }
        }
    }

    private static void ApplyStudioAuthoredOuterTransformToElementGroup(
        Model3DGroup content,
        int elementIndex,
        StudioBlockAnimationKeyframe frame,
        Point3D pivot)
    {
        if (elementIndex < 0 || elementIndex >= content.Children.Count ||
            content.Children[elementIndex] is not Model3DGroup elementGroup)
            return;

        var sourceTransform = elementGroup.Transform ?? Transform3D.Identity;
        if (!frame.Visible)
        {
            var hidden = new Transform3DGroup();
            if (!sourceTransform.Value.IsIdentity)
                hidden.Children.Add(sourceTransform);
            hidden.Children.Add(new ScaleTransform3D(0, 0, 0, pivot.X, pivot.Y, pivot.Z));
            elementGroup.Transform = hidden;
            return;
        }

        var positionIdentity = frame.Position.All(value => Math.Abs(value) < .0001);
        var scaleIdentity = frame.Scale.All(value => Math.Abs(value - 1) < .0001);
        var rotationIdentity = Math.Abs(frame.RotationDegrees) < .0001;
        if (positionIdentity && scaleIdentity && rotationIdentity)
            return;

        var authored = new Transform3DGroup();
        authored.Children.Add(new TranslateTransform3D(-pivot.X, -pivot.Y, -pivot.Z));
        authored.Children.Add(new ScaleTransform3D(frame.Scale[0], frame.Scale[1], frame.Scale[2]));
        if (!rotationIdentity)
        {
            var axis = frame.RotationAxis.ToLowerInvariant() switch
            {
                "x" => new Vector3D(1, 0, 0),
                "z" => new Vector3D(0, 0, 1),
                _ => new Vector3D(0, 1, 0)
            };
            authored.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(axis, frame.RotationDegrees)));
        }
        authored.Children.Add(new TranslateTransform3D(
            pivot.X + frame.Position[0],
            pivot.Y + frame.Position[1],
            pivot.Z + frame.Position[2]));

        var combined = new Transform3DGroup();
        if (!sourceTransform.Value.IsIdentity)
            combined.Children.Add(sourceTransform);
        combined.Children.Add(authored);
        elementGroup.Transform = combined;
    }

    private Model3DGroup? GetStudioAnimationViewportContentGroup()
    {
        if (_studioAnimationViewport is null)
            return null;
        var viewport = _studioAnimationViewport.Children.OfType<Viewport3D>().FirstOrDefault();
        var visual = viewport?.Children.OfType<ModelVisual3D>().FirstOrDefault();
        if (visual?.Content is not Model3DGroup scene)
            return null;
        return scene.Children.OfType<Model3DGroup>().LastOrDefault();
    }

    private void SyncStudioRuntimeTargetViewportSelection(double time)
    {
        if (_studioAnimationViewport is null)
            return;
        var selected = GetSelectedStudioAnimationTarget();
        if (selected is null)
        {
            _studioAnimationViewport.SelectElement(-1);
            return;
        }

        if (selected.Kind == AnimationTargetKind.JsonModelElement && selected.ElementIndex is int jsonIndex)
        {
            var frame = EvaluateStudioAnimationFrame(FindStudioAnimationTrackForTarget(selected), time);
            _studioAnimationViewport.SelectElement(frame.Visible ? jsonIndex : -1);
            return;
        }

        if (selected.Kind == AnimationTargetKind.RuntimeRendererComponent)
        {
            var binding = _studioRuntimeTargetPreviewBindings.FirstOrDefault(candidate =>
                candidate.TargetId.Equals(selected.TargetId.Value, StringComparison.Ordinal));
            if (binding is null)
            {
                _studioAnimationViewport.SelectElement(-1);
                return;
            }
            var frame = EvaluateStudioAnimationFrame(FindStudioAnimationTrackForTarget(selected), time);
            _studioAnimationViewport.SelectElement(frame.Visible ? binding.StartElementIndex : -1);
        }
    }

    private void IsolateStudioRuntimeTargetPreview(StudioRuntimeTargetPreviewBinding binding, int milliseconds)
    {
        var content = GetStudioAnimationViewportContentGroup();
        if (content is null)
            return;

        for (var index = 0; index < content.Children.Count; index++)
        {
            if (index >= binding.StartElementIndex &&
                index < binding.StartElementIndex + binding.ElementCount)
                continue;
            if (content.Children[index] is Model3DGroup group)
                group.Transform = new ScaleTransform3D(0, 0, 0);
        }
        _studioAnimationViewport?.SelectElement(binding.StartElementIndex);

        _studioAnimationIdentifyTimer?.Stop();
        _studioAnimationIdentifyTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Clamp(milliseconds, 150, 1500))
        };
        _studioAnimationIdentifyTimer.Tick += (_, _) =>
        {
            _studioAnimationIdentifyTimer?.Stop();
            RenderStudioRuntimeTargetPreviewNow(_studioAnimationTimeline?.Value ?? 0);
        };
        _studioAnimationIdentifyTimer.Start();
    }
}
