using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using ForeverGuiDesigner.Controls;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Future-proof bridge for blocks added after the current Studio release.
///
/// Ordinary blockstate/model JSON remains auto-discovered by MinecraftBlockModelPreviewService and
/// requires no metadata at all. This bridge is only for geometry/state that exists outside those JSON
/// models (normally BlockEntityRenderer output). Project metadata describes that runtime layer using
/// safe local model references and bounded numeric curves; existing hard-coded source-parity renderers
/// remain the fallback for current machines that do not publish metadata yet.
/// </summary>
public partial class MainWindow
{
    private sealed record FutureProofRuntimeBinding(
        string ComponentKey,
        int StartElementIndex,
        int ElementCount,
        Point3D Pivot);

    private bool _futureProofRuntimeMetadataInitialized;
    private bool _futureProofBlockPreviewQueued;
    private bool _futureProofAnimationPreviewQueued;
    private double _futureProofAnimationPreviewTime;

    public void EnableForeverFutureProofRuntimeMetadata()
    {
        if (_futureProofRuntimeMetadataInitialized)
            return;
        _futureProofRuntimeMetadataInitialized = true;

        if (_studioBlockWorkspace is FrameworkElement blockWorkspace)
        {
            blockWorkspace.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                    RefreshFutureProofRuntimeMetadata(forceReload: true);
            };
        }

        if (_studioBlockMachineCombo is not null)
        {
            _studioBlockMachineCombo.SelectionChanged += (_, _) =>
            {
                RefreshFutureProofRuntimeMetadata(forceReload: false, refreshSelectors: false);
                QueueFutureProofBlockPreview();
                QueueFutureProofAnimationPreview();
            };
        }

        // This value changes after every source-model rebuild/edit and gives the future runtime overlay
        // one central post-render hook without coupling to every individual block editing command.
        if (_studioBlockFaceCountText is not null)
        {
            DependencyPropertyDescriptor
                .FromProperty(TextBlock.TextProperty, typeof(TextBlock))?
                .AddValueChanged(_studioBlockFaceCountText, (_, _) => QueueFutureProofBlockPreview());
        }

        if (_studioAnimationSurface is not null)
        {
            _studioAnimationSurface.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                {
                    RefreshFutureProofRuntimeMetadata(forceReload: false);
                    QueueFutureProofAnimationPreview();
                }
            };
        }

        if (_studioAnimationTimeline is not null)
            _studioAnimationTimeline.ValueChanged += (_, _) => QueueFutureProofAnimationPreview();
        if (_studioAnimationRuntimeCurveCombo is not null)
            _studioAnimationRuntimeCurveCombo.SelectionChanged += (_, _) => QueueFutureProofAnimationPreview();
        if (_studioAnimationTimer is not null)
            _studioAnimationTimer.Tick += (_, _) => QueueFutureProofAnimationPreview();

        // RuntimeStateStudio updates its summary synchronously before its legacy source renderer is
        // composed. Queueing from the summary guarantees metadata-backed state changes render last.
        if (_studioRuntimeStateSummaryText is not null)
        {
            DependencyPropertyDescriptor
                .FromProperty(TextBlock.TextProperty, typeof(TextBlock))?
                .AddValueChanged(_studioRuntimeStateSummaryText, (_, _) => QueueFutureProofAnimationPreview());
        }

        RefreshFutureProofRuntimeMetadata(forceReload: true);
    }

    private void RefreshFutureProofRuntimeMetadata(bool forceReload, bool refreshSelectors = true)
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) || !Directory.Exists(_repoRoot))
            return;

        ForeverStudioRuntimeMetadataCatalog.Configure(_repoRoot, forceReload);
        if (refreshSelectors)
            ApplyFutureProofRuntimeDisplayNames();

        // The existing inspectors/registries deliberately stay authoritative. Their catalogs now
        // resolve project metadata first, so refreshing them makes every existing persistence,
        // validation and stable TargetId path work for future blocks as well.
        if (_studioAnimationRuntimeCurveCombo is not null)
            RefreshStudioRuntimeAnimationProfile();
        if (_studioRuntimeStateControlsPanel is not null)
            RefreshStudioRuntimeStateInspector();

        if (forceReload)
            ReportFutureProofRuntimeMetadataHealth();

        QueueFutureProofBlockPreview();
        QueueFutureProofAnimationPreview();
    }

    private void ApplyFutureProofRuntimeDisplayNames()
    {
        if (_studioBlockMachineCombo is null || _studioBlockAssets.Count == 0)
            return;

        var selectedId = (_studioBlockMachineCombo.SelectedItem as MinecraftBlockAssetDescriptor)?.Id;
        var changed = false;
        var renamed = _studioBlockAssets.Select(asset =>
        {
            if (!ForeverStudioRuntimeMetadataCatalog.TryGetBlock(asset.Id, out var metadata) ||
                string.IsNullOrWhiteSpace(metadata.DisplayName) ||
                metadata.DisplayName.Equals(asset.DisplayName, StringComparison.Ordinal))
                return asset;

            changed = true;
            return new MinecraftBlockAssetDescriptor
            {
                Id = asset.Id,
                DisplayName = metadata.DisplayName,
                BlockstatePath = asset.BlockstatePath,
                Variants = asset.Variants.ToList()
            };
        }).ToList();

        if (!changed)
            return;

        _studioBlockAssets = renamed;
        _studioBlockSelectorUpdate = true;
        try
        {
            _studioBlockMachineCombo.ItemsSource = null;
            _studioBlockMachineCombo.DisplayMemberPath = nameof(MinecraftBlockAssetDescriptor.DisplayName);
            _studioBlockMachineCombo.ItemsSource = _studioBlockAssets;
            _studioBlockMachineCombo.SelectedItem = selectedId is null
                ? _studioBlockAssets.FirstOrDefault()
                : _studioBlockAssets.FirstOrDefault(asset => asset.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase))
                  ?? _studioBlockAssets.FirstOrDefault();
        }
        finally
        {
            _studioBlockSelectorUpdate = false;
        }

        PopulateStudioBlockSelectors();
    }

    private void ReportFutureProofRuntimeMetadataHealth()
    {
        var descriptors = ForeverStudioRuntimeMetadataCatalog.AllBlocks.Count();
        var warnings = ForeverStudioRuntimeMetadataCatalog.Warnings;
        if (warnings.Count > 0)
        {
            StatusText.Text =
                $"Runtime metadata: {descriptors} valid descriptor(s), {warnings.Count} warning(s). Invalid runtime metadata was isolated; normal block/model editing remains available. First warning: {warnings[0]}";
            return;
        }

        if (descriptors > 0)
            StatusText.Text = $"Runtime metadata v{ForeverStudioRuntimeMetadataCatalog.CurrentSchemaVersion}: {descriptors} descriptor(s) loaded and validated.";
    }

    private void QueueFutureProofBlockPreview()
    {
        if (!_futureProofRuntimeMetadataInitialized || _futureProofBlockPreviewQueued)
            return;
        _futureProofBlockPreviewQueued = true;
        _ = Dispatcher.BeginInvoke(() =>
        {
            _futureProofBlockPreviewQueued = false;
            if (_studioBlockWorkspace?.Visibility == Visibility.Visible)
                RenderFutureProofBlockPreview();
        }, DispatcherPriority.ContextIdle);
    }

    private void RenderFutureProofBlockPreview()
    {
        if (_studioBlockViewport is null || _studioEditableBlockModel is null ||
            string.IsNullOrWhiteSpace(_repoRoot) ||
            _studioBlockMachineCombo?.SelectedItem is not MinecraftBlockAssetDescriptor asset ||
            !ForeverStudioRuntimeMetadataCatalog.TryGetBlock(asset.Id, out var metadata))
            return;

        var preview = CloneStudioBlockModel(_studioEditableBlockModel);
        var bindings = AppendFutureProofRuntimeComponents(preview, metadata, out var errors);
        if (bindings.Count == 0)
        {
            ReportFutureProofPreviewErrors(errors);
            return;
        }

        _studioBlockViewport.SetTextureOverrides(_studioTextureOverrides);
        _studioBlockViewport.SetModel(
            _repoRoot,
            preview,
            _studioEditableBlockstateXRotation,
            _studioEditableBlockstateYRotation,
            forceRebuild: true);
        ApplyFutureProofSourceTransforms(_studioBlockViewport, metadata, bindings, phase: 0);
        _studioBlockViewport.SelectElement(_studioSelectedElementIndex);
        ReportFutureProofPreviewErrors(errors);
    }

    private void QueueFutureProofAnimationPreview()
    {
        if (!_futureProofRuntimeMetadataInitialized)
            return;

        _futureProofAnimationPreviewTime = _studioAnimationTimeline?.Value ?? 0;
        if (_futureProofAnimationPreviewQueued)
            return;
        _futureProofAnimationPreviewQueued = true;
        _ = Dispatcher.BeginInvoke(() =>
        {
            _futureProofAnimationPreviewQueued = false;
            if (_studioAnimationSurface?.Visibility == Visibility.Visible)
                RenderFutureProofAnimationPreview(_futureProofAnimationPreviewTime);
        }, DispatcherPriority.ContextIdle);
    }

    private void RenderFutureProofAnimationPreview(double time)
    {
        if (_studioAnimationViewport is null || _studioEditableBlockModel is null ||
            string.IsNullOrWhiteSpace(_repoRoot) ||
            _studioBlockMachineCombo?.SelectedItem is not MinecraftBlockAssetDescriptor asset ||
            !ForeverStudioRuntimeMetadataCatalog.TryGetBlock(asset.Id, out var metadata))
            return;

        RebuildStudioAnimationTargetRegistry();
        var phase = Math.Clamp(time / Math.Max(.0001, _studioAnimationDurationSeconds), 0, 1);
        var preview = CloneStudioBlockModel(_studioEditableBlockModel);
        var baseElementCount = preview.Elements.Count;
        var bindings = AppendFutureProofRuntimeComponents(preview, metadata, out var errors);

        _studioRuntimeTargetPreviewBaseElementCount = baseElementCount;
        _studioRuntimeTargetPreviewBindings.Clear();
        var cursor = baseElementCount;
        foreach (var binding in bindings)
        {
            if (binding.StartElementIndex > cursor)
                cursor = binding.StartElementIndex;
            AddStudioRuntimeTargetPreviewBinding(
                metadata.AssetId,
                binding.ComponentKey,
                ref cursor,
                binding.ElementCount,
                binding.Pivot);
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

        ApplyFutureProofSourceTransforms(_studioAnimationViewport, metadata, bindings, phase);
        ApplyStudioAuthoredOuterTransforms(time);
        SyncStudioRuntimeTargetViewportSelection(time);
        UpdateStudioRuntimeTargetSelectionOverlay();
        ReportFutureProofPreviewErrors(errors);
    }

    private List<FutureProofRuntimeBinding> AppendFutureProofRuntimeComponents(
        MinecraftResolvedBlockModel preview,
        ForeverStudioRuntimeBlockMetadata metadata,
        out List<string> errors)
    {
        errors = [];
        var bindings = new List<FutureProofRuntimeBinding>();
        foreach (var component in metadata.Components)
        {
            if (string.IsNullOrWhiteSpace(component.PreviewModel))
                continue;

            try
            {
                var componentModel = MinecraftBlockModelPreviewService.LoadResolvedModel(_repoRoot!, component.PreviewModel);
                var start = preview.Elements.Count;
                var texturePrefix = $"studio_runtime_{SanitizeFutureProofToken(metadata.AssetId)}_{SanitizeFutureProofToken(component.Key)}_";

                foreach (var texture in componentModel.Textures)
                {
                    var value = texture.Value.StartsWith('#')
                        ? $"#{texturePrefix}{texture.Value[1..]}"
                        : texture.Value;
                    preview.Textures[texturePrefix + texture.Key] = value;
                }

                var visible = ForeverStudioRuntimeMetadataCatalog.IsComponentVisible(
                    component,
                    stateId => ResolveFutureProofRuntimeState(metadata, stateId));

                foreach (var source in componentModel.Elements)
                {
                    var clone = CloneFutureProofRuntimeElement(source, texturePrefix);
                    if (!visible)
                        CollapseFutureProofRuntimeElement(clone);
                    preview.Elements.Add(clone);
                }

                var count = preview.Elements.Count - start;
                if (count > 0)
                {
                    bindings.Add(new FutureProofRuntimeBinding(
                        component.Key,
                        start,
                        count,
                        new Point3D(component.Pivot[0], component.Pivot[1], component.Pivot[2])));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                errors.Add($"{component.Key}: {ex.Message}");
            }
        }
        return bindings;
    }

    private string ResolveFutureProofRuntimeState(ForeverStudioRuntimeBlockMetadata metadata, string stateId)
    {
        var state = metadata.States.FirstOrDefault(candidate => candidate.Id.Equals(stateId, StringComparison.OrdinalIgnoreCase));
        return state is null
            ? string.Empty
            : GetStudioRuntimeState(metadata.AssetId, state.Id, state.DefaultOptionId);
    }

    private static MinecraftModelElement CloneFutureProofRuntimeElement(MinecraftModelElement source, string texturePrefix)
        => new()
        {
            From = [.. source.From],
            To = [.. source.To],
            Rotation = source.Rotation is null
                ? null
                : new MinecraftModelElementRotation
                {
                    Origin = [.. source.Rotation.Origin],
                    Axis = source.Rotation.Axis,
                    Angle = source.Rotation.Angle,
                    Rescale = source.Rotation.Rescale
                },
            Faces = source.Faces.ToDictionary(
                pair => pair.Key,
                pair => new MinecraftModelFace
                {
                    Texture = pair.Value.Texture.StartsWith('#')
                        ? $"#{texturePrefix}{pair.Value.Texture[1..]}"
                        : pair.Value.Texture,
                    Uv = pair.Value.Uv is null ? null : [.. pair.Value.Uv],
                    Rotation = pair.Value.Rotation,
                    CullFace = pair.Value.CullFace,
                    TintIndex = pair.Value.TintIndex
                },
                StringComparer.OrdinalIgnoreCase)
        };

    private static void CollapseFutureProofRuntimeElement(MinecraftModelElement element)
    {
        var x = (element.From[0] + element.To[0]) / 2;
        var y = (element.From[1] + element.To[1]) / 2;
        var z = (element.From[2] + element.To[2]) / 2;
        element.From[0] = element.To[0] = x;
        element.From[1] = element.To[1] = y;
        element.From[2] = element.To[2] = z;
    }

    private void ApplyFutureProofSourceTransforms(
        MinecraftBlockModelViewport viewport,
        ForeverStudioRuntimeBlockMetadata metadata,
        IReadOnlyList<FutureProofRuntimeBinding> bindings,
        double phase)
    {
        var content = GetFutureProofViewportContentGroup(viewport);
        if (content is null)
            return;

        foreach (var binding in bindings)
        {
            var tracks = metadata.SourceTracks
                .Where(track => track.Component.Equals(binding.ComponentKey, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (tracks.Count == 0)
                continue;

            var tx = 0d;
            var ty = 0d;
            var tz = 0d;
            var rx = 0d;
            var ry = 0d;
            var rz = 0d;
            var sx = 1d;
            var sy = 1d;
            var sz = 1d;
            var visible = true;

            foreach (var track in tracks)
            {
                var value = ForeverStudioRuntimeMetadataCatalog.EvaluateTrack(track, phase);
                switch (track.Channel.ToLowerInvariant())
                {
                    case "translatex": tx += value; break;
                    case "translatey": ty += value; break;
                    case "translatez": tz += value; break;
                    case "rotatex": rx += value; break;
                    case "rotatey": ry += value; break;
                    case "rotatez": rz += value; break;
                    case "scalex": sx *= value; break;
                    case "scaley": sy *= value; break;
                    case "scalez": sz *= value; break;
                    case "uniformscale": sx *= value; sy *= value; sz *= value; break;
                    case "visibility": visible &= value >= .5; break;
                }
            }

            for (var index = binding.StartElementIndex;
                 index < binding.StartElementIndex + binding.ElementCount && index < content.Children.Count;
                 index++)
            {
                if (content.Children[index] is Model3DGroup group)
                {
                    ApplyFutureProofSourceTransform(
                        group,
                        binding.Pivot,
                        tx, ty, tz,
                        rx, ry, rz,
                        sx, sy, sz,
                        visible);
                }
            }
        }
    }

    private static void ApplyFutureProofSourceTransform(
        Model3DGroup group,
        Point3D pivot,
        double tx, double ty, double tz,
        double rx, double ry, double rz,
        double sx, double sy, double sz,
        bool visible)
    {
        var sourceTransform = group.Transform ?? Transform3D.Identity;
        var combined = new Transform3DGroup();
        if (!sourceTransform.Value.IsIdentity)
            combined.Children.Add(sourceTransform);

        if (!visible)
        {
            combined.Children.Add(new ScaleTransform3D(0, 0, 0, pivot.X, pivot.Y, pivot.Z));
            group.Transform = combined;
            return;
        }

        var identity = Math.Abs(tx) < .0001 && Math.Abs(ty) < .0001 && Math.Abs(tz) < .0001 &&
                       Math.Abs(rx) < .0001 && Math.Abs(ry) < .0001 && Math.Abs(rz) < .0001 &&
                       Math.Abs(sx - 1) < .0001 && Math.Abs(sy - 1) < .0001 && Math.Abs(sz - 1) < .0001;
        if (identity)
            return;

        var runtime = new Transform3DGroup();
        runtime.Children.Add(new TranslateTransform3D(-pivot.X, -pivot.Y, -pivot.Z));
        runtime.Children.Add(new ScaleTransform3D(sx, sy, sz));
        if (Math.Abs(rx) >= .0001)
            runtime.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), rx)));
        if (Math.Abs(ry) >= .0001)
            runtime.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), ry)));
        if (Math.Abs(rz) >= .0001)
            runtime.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), rz)));
        runtime.Children.Add(new TranslateTransform3D(pivot.X + tx, pivot.Y + ty, pivot.Z + tz));
        combined.Children.Add(runtime);
        group.Transform = combined;
    }

    private static Model3DGroup? GetFutureProofViewportContentGroup(MinecraftBlockModelViewport viewport)
    {
        var viewport3D = viewport.Children.OfType<Viewport3D>().FirstOrDefault();
        var visual = viewport3D?.Children.OfType<ModelVisual3D>().FirstOrDefault();
        if (visual?.Content is not Model3DGroup scene)
            return null;
        return scene.Children.OfType<Model3DGroup>().LastOrDefault();
    }

    private void ReportFutureProofPreviewErrors(IReadOnlyList<string> errors)
    {
        if (errors.Count == 0 || _studioBlockViewerStatusText is null)
            return;
        _studioBlockViewerStatusText.Text =
            $"Runtime metadata preview isolated {errors.Count} component error(s). Base model remains editable. First: {errors[0]}";
        _studioBlockViewerStatusText.Foreground = Brush("#E18B8B");
    }

    private static string SanitizeFutureProofToken(string value)
        => new(value.ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) || character == '_' ? character : '_')
            .ToArray());
}
