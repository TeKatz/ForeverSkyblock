namespace ForeverGuiDesigner.Services;

public sealed record ForeverRuntimeEffectDescriptor(
    string Id,
    string DisplayName,
    string SourceSymbol,
    string SourcePath,
    string Description);

public sealed record ForeverRuntimeEffectProfile(
    string AssetId,
    IReadOnlyList<ForeverRuntimeEffectDescriptor> Effects,
    string SourceNote)
{
    public bool HasEffects => Effects.Count > 0;
}

/// <summary>
/// Source-backed catalog of renderer effects that Phase 17C.3 may suppress in preview.
/// The catalog never invents a new runtime effect and never bypasses the renderer predicate that
/// normally enables an effect; the authoring layer can only keep Source behavior or suppress it.
/// </summary>
public static class ForeverRuntimeEffectCatalog
{
    private static readonly IReadOnlyDictionary<string, ForeverRuntimeEffectProfile> Profiles = BuildProfiles();

    public static ForeverRuntimeEffectProfile ForAsset(string? assetId)
    {
        var key = (assetId ?? string.Empty).Trim().ToLowerInvariant();
        return Profiles.TryGetValue(key, out var profile)
            ? profile
            : new ForeverRuntimeEffectProfile(key, [], "No source-backed special renderer effects are cataloged for this asset.");
    }

    private static IReadOnlyDictionary<string, ForeverRuntimeEffectProfile> BuildProfiles()
    {
        static ForeverRuntimeEffectDescriptor E(string id, string name, string symbol, string path, string description)
            => new(id, name, symbol, path, description);

        var condenserEffects = new[]
        {
            E(
                "process-dust",
                "Process dust",
                "CondenserRenderer.process / MachineAnimation.dust",
                "mod/src/main/java/de/tekatz/foreverskyblock/client/CondenserRenderer.java",
                "Four bounded dust particles. Source predicate: process branch + RUNNING; material follows composting/cultivating versus normal jobs."),
            E(
                "garden-scan",
                "Garden scan bar",
                "CondenserRenderer.garden / MachineAnimation.growth",
                "mod/src/main/java/de/tekatz/foreverskyblock/client/CondenserRenderer.java",
                "Travelling scan bar. Source predicate: Garden attachment + RUNNING."),
            E(
                "garden-harvest",
                "Garden harvest sweep",
                "CondenserRenderer.garden / MachineAnimation.harvest",
                "mod/src/main/java/de/tekatz/foreverskyblock/client/CondenserRenderer.java",
                "Garden harvest sweep. Source predicate: Garden attachment + RUNNING and harvest curve above zero.")
        };

        return new Dictionary<string, ForeverRuntimeEffectProfile>(StringComparer.OrdinalIgnoreCase)
        {
            ["condenser_mark_i"] = new(
                "condenser_mark_i",
                condenserEffects,
                "Source: CondenserRenderer.java. Effect authoring is suppression-only and keeps all source predicates authoritative."),
            ["condenser_mark_ii"] = new(
                "condenser_mark_ii",
                condenserEffects,
                "Source: CondenserRenderer.java. Effect authoring is suppression-only and keeps all source predicates authoritative."),
            ["ascension_chamber"] = new(
                "ascension_chamber",
                [
                    E(
                        "orbit-particles",
                        "Orbit particles",
                        "AscensionRenderer.render particle loop",
                        "mod/src/main/java/de/tekatz/foreverskyblock/client/AscensionRenderer.java",
                        "Six orbiting glow particles. Source predicate: AscensionChamberBlock.ACTIVE.")
                ],
                "Source: AscensionRenderer.java."),
            ["garden_chamber_mark_i"] = new(
                "garden_chamber_mark_i",
                [
                    E(
                        "irrigation-drops",
                        "Irrigation drops",
                        "GardenChamberRenderer.render drop loop",
                        "mod/src/main/java/de/tekatz/foreverskyblock/client/GardenChamberRenderer.java",
                        "Four falling irrigation drops. Source predicate: linked Condenser RUNNING + non-empty specimen."),
                    E(
                        "harvest-sweep",
                        "Harvest sweep",
                        "GardenChamberRenderer.render / MachineAnimation.harvest",
                        "mod/src/main/java/de/tekatz/foreverskyblock/client/GardenChamberRenderer.java",
                        "Two harvest sweep bars. Source predicate: linked Condenser RUNNING + non-empty specimen + harvest curve above zero.")
                ],
                "Source: GardenChamberRenderer.java."),
            ["washing_basin"] = new(
                "washing_basin",
                [
                    E(
                        "circulation-rings",
                        "Circulation rings",
                        "WashingRenderer.render ring loop",
                        "mod/src/main/java/de/tekatz/foreverskyblock/client/WashingRenderer.java",
                        "Three water circulation rings. Source predicate: stored wash amount > 0; RUNNING changes motion/material but not ring existence.")
                ],
                "Source: WashingRenderer.java."),
            ["crucible"] = new(
                "crucible",
                [
                    E(
                        "thermal-convection",
                        "Thermal convection",
                        "CrucibleRenderer.render convection loop",
                        "mod/src/main/java/de/tekatz/foreverskyblock/client/CrucibleRenderer.java",
                        "Four drifting hot-lava highlights. Source predicate: CrucibleBlock.FILL > 0; independent of recipe activity.")
                ],
                "Source: CrucibleRenderer.java."),
            ["refining_chamber"] = new(
                "refining_chamber",
                [],
                "RefiningRenderer has moving reactor geometry and status materials, but no separate optional special-effect loop to expose here."),
            ["mixing_chamber_mark_i"] = new(
                "mixing_chamber_mark_i",
                [],
                "No dedicated BlockEntityRenderer special effects in current source."),
            ["freezing_chamber_mark_i"] = new(
                "freezing_chamber_mark_i",
                [],
                "Freezing fill is rendered as linked Condenser state, not as a standalone optional effect.")
        };
    }
}
