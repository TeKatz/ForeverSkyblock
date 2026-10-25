using System.Windows;
using ForeverGuiDesigner.Services;

namespace ForeverGuiDesigner;

/// <summary>
/// Garden-specific runtime parity that mirrors the fixed GardenTreeShape geometry and keeps the
/// runtime pass on top of the legacy Phase-17B base pass without introducing another rebuild loop.
/// </summary>
public partial class MainWindow
{
    private const int GardenTreeAllFaces = 1 | 2 | 4 | 8 | 16 | 32;
    private static readonly IReadOnlyList<StudioGardenTreePart> StudioGardenTreeParts = BuildStudioGardenTreeParts();
    private bool _studioGardenAnimationParityInitialized;

    public void EnableStudioGardenAnimationParityFix()
    {
        if (_studioGardenAnimationParityInitialized)
            return;
        _studioGardenAnimationParityInitialized = true;

        // Manual scrubbing: the legacy JSON preview runs first. Render Garden runtime immediately
        // afterwards so the glass-hidden base frame is never the last frame submitted to WPF.
        if (_studioAnimationTimeline is not null)
        {
            _studioAnimationTimeline.ValueChanged += (_, _) =>
            {
                if (!_studioAnimationUiUpdating)
                    RenderStudioGardenRuntimeImmediately();
            };
        }

        // Playback uses SetStudioAnimationTimelineTime(), which performs a legacy base render after
        // the slider ValueChanged event. This trailing Tick handler therefore has to be registered
        // after the original transport handler and restores the Garden runtime frame synchronously.
        if (_studioAnimationTimer is not null)
        {
            _studioAnimationTimer.Tick += (_, _) =>
            {
                if (_studioAnimationPlaying)
                    RenderStudioGardenRuntimeImmediately();
            };
        }
    }

    private void RenderStudioGardenRuntimeImmediately()
    {
        if (_studioAnimationSurface?.Visibility != Visibility.Visible ||
            _studioRuntimeAnimationProfile?.AssetId is not "garden_chamber_mark_i")
            return;

        RenderStudioRuntimeAnimationPreview(_studioAnimationTimeline?.Value ?? 0);
    }

    /// <summary>
    /// Intercepts only the two deliberately simplified Garden primitives from the original Studio
    /// bridge: the one-box tree proxy and the fixed-size irrigation drops. Other machines and Garden
    /// lamp/harvest geometry continue through the normal AddRuntimeBox path unchanged.
    /// </summary>
    private static bool TryExpandStudioGardenRuntimeGeometry(
        MinecraftResolvedBlockModel model,
        int material,
        double x0, double y0, double z0,
        double x1, double y1, double z1,
        double rotationAngle,
        double[] rotationOrigin)
    {
        if (!IsStudioGardenRuntimeModel(model) || Math.Abs(rotationAngle) > 0.0001)
            return false;

        // Previous bridge proxy: one 0.5×N×0.5 column at the specimen root. Replace it with the
        // actual fixed GardenTreeShape: 2 trunk pieces, 2 branches and 27 foliage cells.
        if (material == 5 &&
            Near(x0, 7.75) && Near(y0, 3.25) && Near(z0, 7.75) &&
            Near(x1, 8.25) && Near(z1, 8.25))
        {
            var proxyHeight = Math.Max(0, y1 - y0);
            var growth = Math.Clamp((proxyHeight - 0.6) / 5.5, 0, 1);
            var phase = ResolveStudioGardenPhaseFromGrowth(growth);
            AddStudioGardenTreeShape(model, phase);
            return true;
        }

        // Previous bridge used four fixed 0.12×0.30×0.12 drops. The Java renderer scales each drop
        // with sin(PI*fall), so preserve four stable elements but use the real per-frame dimensions.
        if (material == 7 &&
            Near(Math.Abs(x1 - x0), 0.12) &&
            Near(Math.Abs(y1 - y0), 0.30) &&
            Near(Math.Abs(z1 - z0), 0.12) &&
            (Near(x0, 3.5) || Near(x0, 12.2)) &&
            (Near(z0, 3.5) || Near(z0, 12.2)))
        {
            var fall = Math.Clamp((12.7 - Math.Min(y0, y1)) / 8.6, 0, 1);
            var size = 0.12 * Math.Sin(Math.PI * fall);
            var sourceX = x0 < 8 ? 3.2 : 12.4;
            var sourceZ = z0 < 8 ? 3.2 : 12.4;
            var sourceY = 12.7 - fall * 8.6;
            AddStudioGardenMaskedBox(
                model, 7,
                sourceX, sourceY, sourceZ,
                sourceX + size, sourceY + size * 2.5, sourceZ + size,
                GardenTreeAllFaces);
            return true;
        }

        return false;
    }

    private static bool IsStudioGardenRuntimeModel(MinecraftResolvedBlockModel model)
        => model.ModelReference.Contains("garden_chamber_mark_i", StringComparison.OrdinalIgnoreCase) ||
           model.SourcePath.Contains("garden_chamber_mark_i", StringComparison.OrdinalIgnoreCase);

    private static bool Near(double left, double right)
        => Math.Abs(left - right) <= 0.0001;

    private static double ResolveStudioGardenPhaseFromGrowth(double growth)
    {
        if (growth <= 0)
            return 0;
        if (growth >= 1)
            return 0.84;

        var low = 0.04;
        var high = 0.84;
        for (var i = 0; i < 28; i++)
        {
            var mid = (low + high) / 2;
            if (StudioGardenSmooth(0.04, 0.84, mid) < growth)
                low = mid;
            else
                high = mid;
        }
        return (low + high) / 2;
    }

    private static double StudioGardenSmooth(double from, double to, double phase)
    {
        var t = Math.Clamp((phase - from) / (to - from), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static void AddStudioGardenTreeShape(MinecraftResolvedBlockModel model, double phase)
    {
        var growth = StudioGardenSmooth(0.04, 0.84, phase);
        var crown = StudioGardenSmooth(0.20, 0.78, phase);
        var height = 0.2 + 0.8 * growth;
        var radius = 0.15 + 0.85 * crown;
        var trunk = 0.45 + 0.55 * growth;

        foreach (var part in StudioGardenTreeParts)
        {
            if (part.Faces == 0)
                continue;

            var width = part.Leaves || part.Y0 > 3.7 ? radius : trunk;
            AddStudioGardenMaskedBox(
                model,
                part.Leaves ? 7 : 5,
                8 + part.X0 * width,
                3.25 + part.Y0 * height,
                8 + part.Z0 * width,
                8 + part.X1 * width,
                3.25 + part.Y1 * height,
                8 + part.Z1 * width,
                part.Faces);
        }
    }

    private static void AddStudioGardenStatusStrips(MinecraftResolvedBlockModel model)
    {
        // Legacy helper retained for compatibility with older preview paths. Phase 17C.1 now owns
        // semantic status material selection and therefore does not call this fixed-running helper.
        AddRuntimeBox(model, 7, 5.8, 1.05, 0.03, 10.2, 1.40, 0.13, 0, [8, 1.2, 0.08]);
        AddRuntimeBox(model, 7, 5.8, 1.05, 15.87, 10.2, 1.40, 15.97, 0, [8, 1.2, 15.92]);
        AddRuntimeBox(model, 7, 0.03, 1.05, 5.8, 0.13, 1.40, 10.2, 0, [0.08, 1.2, 8]);
        AddRuntimeBox(model, 7, 15.87, 1.05, 5.8, 15.97, 1.40, 10.2, 0, [15.92, 1.2, 8]);
    }

    private static void AddStudioGardenMaskedBox(
        MinecraftResolvedBlockModel model,
        int material,
        double x0, double y0, double z0,
        double x1, double y1, double z1,
        int faceMask)
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
        AddStudioGardenFace(faces, faceMask, 1, "north", texture, uv);
        AddStudioGardenFace(faces, faceMask, 2, "south", texture, uv);
        AddStudioGardenFace(faces, faceMask, 4, "west", texture, uv);
        AddStudioGardenFace(faces, faceMask, 8, "east", texture, uv);
        AddStudioGardenFace(faces, faceMask, 16, "up", texture, uv);
        AddStudioGardenFace(faces, faceMask, 32, "down", texture, uv);

        model.Elements.Add(new MinecraftModelElement
        {
            From = [Math.Min(x0, x1), Math.Min(y0, y1), Math.Min(z0, z1)],
            To = [Math.Max(x0, x1), Math.Max(y0, y1), Math.Max(z0, z1)],
            Faces = faces
        });
    }

    private static void AddStudioGardenFace(
        Dictionary<string, MinecraftModelFace> faces,
        int faceMask,
        int bit,
        string direction,
        string texture,
        double[] uv)
    {
        if ((faceMask & bit) == 0)
            return;
        faces[direction] = new MinecraftModelFace
        {
            Texture = texture,
            Uv = [.. uv]
        };
    }

    private static IReadOnlyList<StudioGardenTreePart> BuildStudioGardenTreeParts()
    {
        var parts = new List<StudioGardenTreePart>
        {
            new(-0.6, 0, -0.6, 0.6, 3.6, 0.6, false, GardenTreeAllFaces),
            new(-0.4, 3.6, -0.4, 0.4, 6.3, 0.4, false, GardenTreeAllFaces),
            new(-2.1, 4.1, -0.25, -0.4, 4.55, 0.25, false, GardenTreeAllFaces),
            new(0.4, 5.25, -0.25, 2.1, 5.7, 0.25, false, GardenTreeAllFaces)
        };

        var foliage = new List<(int X, int Y, int Z)>();
        for (var layer = 0; layer < 2; layer++)
        {
            for (var x = 0; x < 4; x++)
            {
                for (var z = 0; z < 4; z++)
                {
                    var corner = (x is 0 or 3) && (z is 0 or 3);
                    if (corner || (layer == 0 && x == 3 && z == 1))
                        continue;
                    foliage.Add((x, layer, z));
                }
            }
        }
        for (var x = 0; x < 2; x++)
            for (var z = 0; z < 2; z++)
                foliage.Add((x, 2, z));

        var occupied = foliage.ToHashSet();
        foreach (var cell in foliage)
        {
            var faces = 0;
            if (!occupied.Contains((cell.X, cell.Y, cell.Z - 1))) faces |= 1;
            if (!occupied.Contains((cell.X, cell.Y, cell.Z + 1))) faces |= 2;
            if (!occupied.Contains((cell.X - 1, cell.Y, cell.Z))) faces |= 4;
            if (!occupied.Contains((cell.X + 1, cell.Y, cell.Z))) faces |= 8;
            if (!occupied.Contains((cell.X, cell.Y + 1, cell.Z))) faces |= 16;
            if (!occupied.Contains((cell.X, cell.Y - 1, cell.Z))) faces |= 32;

            if (cell.Y < 2)
            {
                var baseY = 3.4 + cell.Y * 2.0;
                var fx = (cell.X - 1) * 1.7 - 1.2;
                var fz = (cell.Z - 1) * 1.7 - 1.2;
                parts.Add(new StudioGardenTreePart(fx, baseY, fz, fx + 1.9, baseY + 2.0, fz + 1.9, true, faces));
            }
            else
            {
                var fx = (cell.X - 0.5) * 1.5 - 1.0;
                var fz = (cell.Z - 0.5) * 1.5 - 1.0;
                parts.Add(new StudioGardenTreePart(fx, 7.4, fz, fx + 2.0, 9.0, fz + 2.0, true, faces));
            }
        }

        return parts;
    }

    private readonly record struct StudioGardenTreePart(
        double X0, double Y0, double Z0,
        double X1, double Y1, double Z1,
        bool Leaves,
        int Faces);
}
