using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Adds the static/default BlockEntityRenderer geometry to the Studio block preview without
/// polluting the editable Minecraft model. The project JSON remains the authoritative edit model;
/// these elements exist only in the viewport, matching what Minecraft composes at runtime.
/// </summary>
public partial class MainWindow
{
    private bool _studioMachineBlockEntityParityInitialized;
    private bool _studioMachineBlockEntityPreviewQueued;

    public void EnableForeverMachineBlockEntityPreviewParity()
    {
        if (_studioMachineBlockEntityParityInitialized || _studioBlockViewport is null)
            return;
        _studioMachineBlockEntityParityInitialized = true;

        NormalizeStudioMachineDisplayNames();

        if (_studioBlockMachineCombo is not null)
        {
            _studioBlockMachineCombo.DropDownOpened += (_, _) => NormalizeStudioMachineDisplayNames();
            _studioBlockMachineCombo.SelectionChanged += (_, _) => QueueStudioMachineBlockEntityPreview();
        }
        if (_studioBlockStateCombo is not null)
            _studioBlockStateCombo.SelectionChanged += (_, _) => QueueStudioMachineBlockEntityPreview();
        if (_studioBlockFacingCombo is not null)
            _studioBlockFacingCombo.SelectionChanged += (_, _) => QueueStudioMachineBlockEntityPreview();
        if (_studioBlockWorkspace is FrameworkElement workspace)
            workspace.IsVisibleChanged += (_, e) =>
            {
                if (e.NewValue is true)
                {
                    NormalizeStudioMachineDisplayNames();
                    QueueStudioMachineBlockEntityPreview();
                }
            };

        // Every source-model render updates this text immediately after SetModel. Listening to the
        // dependency property gives us one central, low-cost post-render hook for numeric edits,
        // UV/face edits, reset/duplicate/delete and direct viewport nudges.
        if (_studioBlockFaceCountText is not null)
        {
            DependencyPropertyDescriptor
                .FromProperty(TextBlock.TextProperty, typeof(TextBlock))?
                .AddValueChanged(_studioBlockFaceCountText, (_, _) => QueueStudioMachineBlockEntityPreview());
        }

        QueueStudioMachineBlockEntityPreview();
    }

    private void NormalizeStudioMachineDisplayNames()
    {
        if (_studioBlockMachineCombo is null || _studioBlockAssets.Count == 0)
            return;
        if (!_studioBlockAssets.Any(asset =>
                asset.Id.Equals("refining_chamber", StringComparison.OrdinalIgnoreCase) &&
                !asset.DisplayName.Equals("Refining Condenser", StringComparison.Ordinal)))
            return;

        var selectedId = (_studioBlockMachineCombo.SelectedItem as MinecraftBlockAssetDescriptor)?.Id;
        _studioBlockAssets = _studioBlockAssets.Select(asset =>
        {
            if (!asset.Id.Equals("refining_chamber", StringComparison.OrdinalIgnoreCase))
                return asset;
            return new MinecraftBlockAssetDescriptor
            {
                Id = asset.Id,
                DisplayName = "Refining Condenser",
                BlockstatePath = asset.BlockstatePath,
                Variants = asset.Variants.ToList()
            };
        }).ToList();

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

    private void QueueStudioMachineBlockEntityPreview()
    {
        if (_studioMachineBlockEntityPreviewQueued)
            return;
        _studioMachineBlockEntityPreviewQueued = true;
        _ = Dispatcher.BeginInvoke(() =>
        {
            _studioMachineBlockEntityPreviewQueued = false;
            ApplyStudioMachineBlockEntityPreview();
        }, DispatcherPriority.Background);
    }

    private void ApplyStudioMachineBlockEntityPreview()
    {
        if (string.IsNullOrWhiteSpace(_repoRoot) || _studioBlockViewport is null ||
            _studioEditableBlockModel is null ||
            _studioBlockMachineCombo?.SelectedItem is not MinecraftBlockAssetDescriptor asset)
            return;

        var preview = asset.Id.ToLowerInvariant() switch
        {
            "ascension_chamber" => BuildAscensionRuntimePreview(_studioEditableBlockModel),
            "refining_chamber" => BuildRefiningRuntimePreview(_studioEditableBlockModel),
            _ => null
        };
        if (preview is null)
            return;

        _studioBlockViewport.SetTextureOverrides(_studioTextureOverrides);
        _studioBlockViewport.SetModel(
            _repoRoot,
            preview,
            _studioEditableBlockstateXRotation,
            _studioEditableBlockstateYRotation);
        _studioBlockViewport.SelectElement(_studioSelectedElementIndex);
    }

    private MinecraftResolvedBlockModel BuildAscensionRuntimePreview(MinecraftResolvedBlockModel source)
    {
        var preview = CloneStudioBlockModel(source);
        AddRuntimeTextureSlots(preview);
        var active = string.Equals(_studioBlockStateCombo?.SelectedItem?.ToString(), "active=true", StringComparison.OrdinalIgnoreCase);
        var accent = active ? 7 : 4;
        AddRuntimeRing(preview, 4.5, 2.8, 0, accent);
        AddRuntimeRing(preview, 3.15, 1.6, 0, accent);
        return preview;
    }

    private static MinecraftResolvedBlockModel BuildRefiningRuntimePreview(MinecraftResolvedBlockModel source)
    {
        var preview = CloneStudioBlockModel(source);
        AddRuntimeTextureSlots(preview);
        AddRefiningInset(preview, 2.15, false);
        AddRefiningInset(preview, 13.85, true);
        return preview;
    }

    private static void AddRuntimeTextureSlots(MinecraftResolvedBlockModel model)
    {
        model.Textures["runtime_machine_materials"] = "forever_skyblock:block/machine_materials";
        model.Textures["runtime_machine_materials_ii"] = "forever_skyblock:block/machine_materials_ii";
        model.Textures["runtime_ascension_materials"] = "forever_skyblock:block/ascension_materials";
        model.Textures["runtime_machine_lights"] = "forever_skyblock:block/machine_lights";
    }

    private static void AddRuntimeRing(MinecraftResolvedBlockModel model, double radius, double z, double angle, int accentMaterial)
    {
        for (var i = 0; i < 4; i++)
        {
            var rotation = angle + i * 90;
            AddRuntimeBox(
                model,
                34,
                8 - radius + 0.5, 8 - radius, z,
                8 + radius, 8 - radius + 0.5, z + 0.35,
                rotation,
                [8, 8, z]);
            AddRuntimeBox(
                model,
                accentMaterial,
                8 - radius * 0.55, 8 - radius + 0.12, z - 0.055,
                8 + radius * 0.55, 8 - radius + 0.36, z - 0.025,
                rotation,
                [8, 8, z]);
        }
    }

    private static void AddRefiningInset(MinecraftResolvedBlockModel model, double centerZ, bool back)
    {
        // Idle/default renderer state (phase 0, status 0), matching the block as seen in-world when
        // no processing animation is active. The back face receives the mirrored runtime inset too.
        var mirror = back ? -1 : 1;
        for (var i = 0; i < 4; i++)
        {
            var rotation = mirror * i * 90;
            AddRuntimeBox(
                model,
                34,
                5, 5, centerZ - 0.3,
                11, 5.3, centerZ + 0.3,
                rotation,
                [8, 8, centerZ]);
            AddRuntimeBox(
                model,
                4,
                6.2, 4.94, centerZ - 0.36,
                9.8, 5.04, centerZ - 0.31,
                rotation,
                [8, 8, centerZ]);
        }

        AddRuntimeBox(
            model,
            4,
            7.1, 7.1, centerZ - 0.5,
            8.9, 8.9, centerZ + 0.5,
            back ? -45 : 45,
            [8, 8, centerZ]);
    }

    private static void AddRuntimeBox(
        MinecraftResolvedBlockModel model,
        int material,
        double x0, double y0, double z0,
        double x1, double y1, double z1,
        double rotationAngle,
        double[] rotationOrigin)
    {
        var palette = material / 16;
        var slot = material % 16;
        var glow = slot >= 6;
        var textureKey = glow
            ? "runtime_machine_lights"
            : palette == 2
                ? "runtime_ascension_materials"
                : palette == 1
                    ? "runtime_machine_materials_ii"
                    : "runtime_machine_materials";

        var u0 = glow ? (slot - 6) / 4.0 : (slot % 3) / 3.0;
        var v0 = glow ? 0 : (slot / 3) / 2.0;
        var du = glow ? 0.249 : Math.Max(0.002, Math.Abs(x1 - x0) / 48.0);
        var dv = glow ? 0.999 : Math.Max(0.002, Math.Abs(y1 - y0) / 32.0);
        double[] uv = [u0 * 16, v0 * 16, (u0 + du) * 16, (v0 + dv) * 16];
        var texture = $"#{textureKey}";

        var faces = new Dictionary<string, MinecraftModelFace>(StringComparer.OrdinalIgnoreCase);
        foreach (var direction in new[] { "north", "south", "west", "east", "up", "down" })
        {
            faces[direction] = new MinecraftModelFace
            {
                Texture = texture,
                Uv = [.. uv]
            };
        }

        model.Elements.Add(new MinecraftModelElement
        {
            From = [Math.Min(x0, x1), Math.Min(y0, y1), Math.Min(z0, z1)],
            To = [Math.Max(x0, x1), Math.Max(y0, y1), Math.Max(z0, z1)],
            Rotation = Math.Abs(rotationAngle) < 0.0001
                ? null
                : new MinecraftModelElementRotation
                {
                    Origin = [.. rotationOrigin],
                    Axis = "z",
                    Angle = rotationAngle,
                    Rescale = false
                },
            Faces = faces
        });
    }
}
