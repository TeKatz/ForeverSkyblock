namespace ForeverGuiDesigner.Services;

internal enum ForeverRuntimeAnimationKind
{
    Static,
    Rotation,
    Translation,
    Scale,
    Procedural,
    Effect
}

internal sealed record ForeverRuntimeAnimationTrack(
    string Id,
    string DisplayName,
    string ComponentKey,
    string SourceSymbol,
    string SourcePath,
    ForeverRuntimeAnimationKind Kind,
    Func<double, double> Evaluate,
    string Description)
{
    public override string ToString() => DisplayName;
}

internal sealed record ForeverRuntimeAnimationProfile(
    string AssetId,
    string DisplayName,
    string RuntimeOwner,
    IReadOnlyList<string> ComponentKeys,
    IReadOnlyList<ForeverRuntimeAnimationTrack> Tracks,
    string CoverageNote)
{
    public bool HasRuntimeAnimation => Tracks.Count > 0;
}

/// <summary>
/// Source-derived catalog of the runtime animations that already exist in the mod.  The catalog is
/// deliberately read-only: it describes Java renderer behavior without converting procedural curves
/// into fake Studio keyframes.  Studio-authored animation remains a separate override layer.
/// </summary>
internal static class ForeverRuntimeAnimationCatalog
{
    private const string Client = "mod/src/main/java/de/tekatz/foreverskyblock/client/";

    private static double Smooth(double from, double to, double phase)
    {
        var t = Math.Clamp((phase - from) / (to - from), 0d, 1d);
        return t * t * (3d - 2d * t);
    }

    private static double Press(double phase)
        => Smooth(.16, .48, phase) * (1d - Smooth(.64, .83, phase));

    private static double Dust(double phase, int index)
    {
        var fall = PositiveModulo(phase * 5d + index * .23d, 1d);
        return Smooth(.38, .46, phase) * (1d - Smooth(.76, .84, phase)) * Math.Sin(Math.PI * fall);
    }

    private static double PositiveModulo(double value, double modulus)
        => ((value % modulus) + modulus) % modulus;

    private static ForeverRuntimeAnimationTrack T(
        string id, string name, string component, string symbol, string file,
        ForeverRuntimeAnimationKind kind, Func<double, double> evaluate, string description)
        => new(id, name, component, symbol, Client + file, kind, evaluate, description);

    private static readonly IReadOnlyDictionary<string, ForeverRuntimeAnimationProfile> Profiles = BuildProfiles();

    public static ForeverRuntimeAnimationProfile ForAsset(string? assetId)
    {
        var key = (assetId ?? string.Empty).Trim().ToLowerInvariant();
        return Profiles.TryGetValue(key, out var profile)
            ? profile
            : new ForeverRuntimeAnimationProfile(key, string.IsNullOrWhiteSpace(key) ? "Unknown" : key,
                "No registered runtime renderer", [], [], "No existing Forever runtime animation was found for this asset.");
    }

    public static IReadOnlyCollection<ForeverRuntimeAnimationProfile> All => Profiles.Values;

    private static IReadOnlyDictionary<string, ForeverRuntimeAnimationProfile> BuildProfiles()
    {
        var result = new Dictionary<string, ForeverRuntimeAnimationProfile>(StringComparer.OrdinalIgnoreCase);

        var condenserCommon = new[]
        {
            T("intake", "Input intake", "condenser_input", "MachineAnimation.intake", "MachineAnimation.java", ForeverRuntimeAnimationKind.Translation,
                p => Smooth(.02,.30,p), "Input travels into the front mechanism."),
            T("ingredient", "Ingredient consume / scale", "condenser_input", "MachineAnimation.ingredient", "MachineAnimation.java", ForeverRuntimeAnimationKind.Scale,
                p => Smooth(0,.06,p) * (1-Smooth(.38,.62,p)), "Input scales out while the recipe consumes it."),
            T("product", "Product emerge", "condenser_result", "MachineAnimation.product", "MachineAnimation.java", ForeverRuntimeAnimationKind.Scale,
                p => Smooth(.64,.82,p), "Result emerges after processing."),
            T("discharge", "Product discharge", "condenser_result", "MachineAnimation.discharge", "MachineAnimation.java", ForeverRuntimeAnimationKind.Translation,
                p => Smooth(.84,1,p), "Result moves into the recessed output."),
            T("dust0", "Process dust 1", "condenser_dust_0", "MachineAnimation.dust(phase,0)", "MachineAnimation.java", ForeverRuntimeAnimationKind.Effect,
                p => Dust(p,0), "First bounded process-particle path."),
            T("dust1", "Process dust 2", "condenser_dust_1", "MachineAnimation.dust(phase,1)", "MachineAnimation.java", ForeverRuntimeAnimationKind.Effect,
                p => Dust(p,1), "Second bounded process-particle path."),
            T("dust2", "Process dust 3", "condenser_dust_2", "MachineAnimation.dust(phase,2)", "MachineAnimation.java", ForeverRuntimeAnimationKind.Effect,
                p => Dust(p,2), "Third bounded process-particle path.")
        };

        result["condenser_mark_i"] = new(
            "condenser_mark_i", "Condenser Mark I", "CondenserRenderer.java",
            ["condenser_press", "condenser_input", "condenser_result", "condenser_dust_0", "condenser_dust_1", "condenser_dust_2"],
            [
                T("press", "Press stroke", "condenser_press", "MachineAnimation.press", "MachineAnimation.java", ForeverRuntimeAnimationKind.Translation,
                    Press, "Mark I ram compression stroke; renderer travel is 2.6 model units."),
                .. condenserCommon
            ],
            "Source: CondenserRenderer process path for tier I plus MachineAnimation shared curves.");

        result["condenser_mark_ii"] = new(
            "condenser_mark_ii", "Condenser Mark II", "CondenserRenderer.java",
            ["condenser_roller_left", "condenser_roller_right", "condenser_input", "condenser_result", "condenser_dust_0", "condenser_dust_1", "condenser_dust_2"],
            [
                T("roller-left", "Left roller", "condenser_roller_left", "phase * 720/1440", "CondenserRenderer.java", ForeverRuntimeAnimationKind.Rotation,
                    p => p * 720, "Normal Mark II cycle rotates 720°; milling uses 1440°."),
                T("roller-right", "Right roller", "condenser_roller_right", "-phase * 720/1440", "CondenserRenderer.java", ForeverRuntimeAnimationKind.Rotation,
                    p => -p * 720, "Counter-rotating roller; milling doubles the angle."),
                .. condenserCommon
            ],
            "Source: CondenserRenderer tier-II roller path plus MachineAnimation shared curves.");

        result["ascension_chamber"] = new(
            "ascension_chamber", "Ascension Chamber", "AscensionRenderer.java",
            ["ascension_outer_ring", "ascension_inner_ring", "ascension_core", "ascension_particles"],
            [
                T("outer-ring", "Outer ring", "ascension_outer_ring", "phase * 180", "AscensionRenderer.java", ForeverRuntimeAnimationKind.Rotation, p => p*180, "Outer runtime ring rotation."),
                T("inner-ring", "Inner ring", "ascension_inner_ring", "-phase * 360", "AscensionRenderer.java", ForeverRuntimeAnimationKind.Rotation, p => -p*360, "Inner runtime ring counter-rotation."),
                T("core-bob", "Core bob", "ascension_core", "sin(phase*PI*4)*.18", "AscensionRenderer.java", ForeverRuntimeAnimationKind.Translation, p => Math.Sin(p*Math.PI*4)*.18, "Inserted core vertical bob."),
                T("core-tilt", "Core tilt", "ascension_core", "sin(phase*PI*2)*9", "AscensionRenderer.java", ForeverRuntimeAnimationKind.Rotation, p => Math.Sin(p*Math.PI*2)*9, "Inserted core Z tilt."),
                T("particles", "Orbit particles", "ascension_particles", "phase*PI*4", "AscensionRenderer.java", ForeverRuntimeAnimationKind.Procedural, p => p*Math.PI*4, "Six particles orbit the active core.")
            ],
            "Source: AscensionRenderer; inactive block holds all runtime motion at phase 0.");

        result["refining_chamber"] = new(
            "refining_chamber", "Refining Condenser", "RefiningRenderer.java",
            ["refining_outer_front", "refining_core_front", "refining_outer_back", "refining_core_back"],
            [
                T("outer-front", "Front outer reactor", "refining_outer_front", "phase * 720", "RefiningRenderer.java", ForeverRuntimeAnimationKind.Rotation, p => p*720, "Front outer inset rotates two turns per visual cycle."),
                T("core-front", "Front core", "refining_core_front", "45 - phase * 360", "RefiningRenderer.java", ForeverRuntimeAnimationKind.Rotation, p => 45-p*360, "Front core counter-rotation."),
                T("outer-back", "Back outer reactor", "refining_outer_back", "phase * 720", "RefiningRenderer.java", ForeverRuntimeAnimationKind.Rotation, p => p*720, "Mirrored back outer inset."),
                T("core-back", "Back core", "refining_core_back", "45 - phase * 360", "RefiningRenderer.java", ForeverRuntimeAnimationKind.Rotation, p => 45-p*360, "Mirrored back core counter-rotation.")
            ],
            "Source: RefiningRenderer; MachineVisualState freezes phase when processing stops.");

        result["garden_chamber_mark_i"] = new(
            "garden_chamber_mark_i", "Garden Chamber Mark I", "GardenChamberRenderer.java",
            ["garden_lamp", "garden_plant", "garden_drops", "garden_harvest"],
            [
                T("lamp", "Travelling grow lamp", "garden_lamp", "MachineAnimation.growth", "MachineAnimation.java", ForeverRuntimeAnimationKind.Translation, p => Smooth(.04,.84,p), "Lamp travels across the chamber."),
                T("plant-growth", "Specimen growth", "garden_plant", "GardenPlantRenderer.plant", "GardenPlantRenderer.java", ForeverRuntimeAnimationKind.Procedural, p => Smooth(.04,.84,p), "Crop/tree geometry grows from the linked specimen."),
                T("irrigation", "Irrigation drops", "garden_drops", "(phase*12+i*.25)%1", "GardenChamberRenderer.java", ForeverRuntimeAnimationKind.Effect, p => PositiveModulo(p*12,1), "Four bounded falling drops while running."),
                T("harvest", "Harvest sweep", "garden_harvest", "MachineAnimation.harvest", "MachineAnimation.java", ForeverRuntimeAnimationKind.Translation, p => Smooth(.86,1,p), "Late-cycle harvest sweep.")
            ],
            "Source: GardenChamberRenderer + GardenPlantRenderer + MachineAnimation.");

        result["washing_basin"] = new(
            "washing_basin", "Washing Basin", "WashingRenderer.java",
            ["washing_surface", "washing_ring_0", "washing_ring_1", "washing_ring_2"],
            [
                T("ring0", "Circulation ring 1", "washing_ring_0", "(phase*9+0/3)%1", "WashingRenderer.java", ForeverRuntimeAnimationKind.Scale, p => PositiveModulo(p*9,1), "First expanding wash ring."),
                T("ring1", "Circulation ring 2", "washing_ring_1", "(phase*9+1/3)%1", "WashingRenderer.java", ForeverRuntimeAnimationKind.Scale, p => PositiveModulo(p*9+1d/3d,1), "Second expanding wash ring."),
                T("ring2", "Circulation ring 3", "washing_ring_2", "(phase*9+2/3)%1", "WashingRenderer.java", ForeverRuntimeAnimationKind.Scale, p => PositiveModulo(p*9+2d/3d,1), "Third expanding wash ring.")
            ],
            "Source: WashingRenderer; dry basin has no runtime surface or ring motion.");

        result["crucible"] = new(
            "crucible", "Crucible", "CrucibleRenderer.java",
            ["crucible_lava_surface", "crucible_convection_0", "crucible_convection_1", "crucible_convection_2", "crucible_convection_3"],
            [
                T("convection0", "Lava convection 1", "crucible_convection_0", "(time/90+0*.25)%1", "CrucibleRenderer.java", ForeverRuntimeAnimationKind.Effect, p => PositiveModulo(p,1), "Stored-lava thermal convection; independent of recipe activity."),
                T("convection1", "Lava convection 2", "crucible_convection_1", "(time/90+1*.25)%1", "CrucibleRenderer.java", ForeverRuntimeAnimationKind.Effect, p => PositiveModulo(p+.25,1), "Second convection patch."),
                T("convection2", "Lava convection 3", "crucible_convection_2", "(time/90+2*.25)%1", "CrucibleRenderer.java", ForeverRuntimeAnimationKind.Effect, p => PositiveModulo(p+.50,1), "Third convection patch."),
                T("convection3", "Lava convection 4", "crucible_convection_3", "(time/90+3*.25)%1", "CrucibleRenderer.java", ForeverRuntimeAnimationKind.Effect, p => PositiveModulo(p+.75,1), "Fourth convection patch.")
            ],
            "Source: CrucibleRenderer; animation exists only while stored lava fill is non-zero.");

        result["mixing_chamber_mark_i"] = new(
            "mixing_chamber_mark_i", "Mixing Chamber Mark I", "Registered simple block",
            [], [], "Registered block asset, but no BlockEntity or BlockEntityRenderer animation exists in the current mod source.");
        result["freezing_chamber_mark_i"] = new(
            "freezing_chamber_mark_i", "Freezing Chamber Mark I", "FreezingChamberBlock.java",
            [], [], "Registered block asset/attachment, but no dedicated BlockEntityRenderer animation exists; freezing progress is rendered by CondenserRenderer when attached to a Condenser.");

        return result;
    }
}
