namespace ForeverGuiDesigner.Services;

internal sealed record ForeverRuntimeStateOption(
    string Id,
    string DisplayName,
    string Description);

internal sealed record ForeverRuntimeStateAxis(
    string Id,
    string DisplayName,
    string DefaultOptionId,
    IReadOnlyList<ForeverRuntimeStateOption> Options,
    string Description)
{
    public ForeverRuntimeStateOption DefaultOption
        => Options.First(option => option.Id.Equals(DefaultOptionId, StringComparison.OrdinalIgnoreCase));
}

internal sealed record ForeverRuntimeStateProfile(
    string AssetId,
    string DisplayName,
    IReadOnlyList<ForeverRuntimeStateAxis> Axes,
    string SourceNote)
{
    public bool HasStateControls => Axes.Count > 0;
}

/// <summary>
/// Phase 17C.1 semantic runtime-state catalog. This intentionally models only states that exist in
/// Forever Skyblock's current renderers; it does not create a universal state system for every asset.
/// The catalog is read-only and drives Studio preview composition only.
/// </summary>
internal static class ForeverRuntimeStateCatalog
{
    private static ForeverRuntimeStateOption O(string id, string name, string description)
        => new(id, name, description);

    private static ForeverRuntimeStateAxis A(
        string id,
        string name,
        string defaultOptionId,
        string description,
        params ForeverRuntimeStateOption[] options)
        => new(id, name, defaultOptionId, options, description);

    private static readonly IReadOnlyDictionary<string, ForeverRuntimeStateProfile> Profiles = BuildProfiles();

    public static ForeverRuntimeStateProfile ForAsset(string? assetId)
    {
        var key = (assetId ?? string.Empty).Trim().ToLowerInvariant();
        return Profiles.TryGetValue(key, out var profile)
            ? profile
            : new ForeverRuntimeStateProfile(
                key,
                string.IsNullOrWhiteSpace(key) ? "Unknown" : key,
                [],
                "No source-backed runtime-state controls are required for this asset.");
    }

    public static IEnumerable<ForeverRuntimeStateProfile> All => Profiles.Values;

    private static IReadOnlyDictionary<string, ForeverRuntimeStateProfile> BuildProfiles()
    {
        var result = new Dictionary<string, ForeverRuntimeStateProfile>(StringComparer.OrdinalIgnoreCase);

        ForeverRuntimeStateAxis condenserStatus = A(
            "status", "Machine status", "running",
            "CondenserBlockEntity visualStatus; controls running effects and status-light material.",
            O("idle", "Idle", "No running particles; neutral status lights."),
            O("running", "Running", "Timeline phase drives the active recipe presentation."),
            O("blocked", "Blocked", "Recipe presentation is held while status lights show the blocked material."));

        ForeverRuntimeStateAxis condenserJob = A(
            "job", "Resolved job", "crushing",
            "CondenserJob from the resolved recipe. This selects the real renderer branch instead of using Runtime curve as a branch selector.",
            O("idle", "Idle", "No resolved recipe job."),
            O("crushing", "Crushing", "Standard press/roller processing path."),
            O("milling", "Milling", "Mark II rollers use the 1440° milling path."),
            O("composting", "Composting", "Organic rotor path at 720° per cycle."),
            O("mixing", "Mixing", "Dual-input mixer path at 1080° per cycle."),
            O("cultivating", "Cultivating", "Dual-input mixer path with cultivating result timing."));

        ForeverRuntimeStateAxis condenserAttachment = A(
            "attachment", "Attachment state", "none",
            "Renderer attachment predicates from CondenserBlockEntity.hasGardenChamber()/hasFreezingChamber().",
            O("none", "None", "Normal process renderer."),
            O("garden", "Garden Chamber", "Use the Garden attachment renderer branch."),
            O("freezing", "Freezing Chamber", "Normal process branch plus freezing-progress column."),
            O("garden+freezing", "Garden + Freezing", "Garden renderer branch plus freezing-progress column."));

        ForeverRuntimeStateAxis condenserSpecimen = A(
            "specimen", "Garden specimen", "oak",
            "Presence of the Condenser GARDEN_SPECIMEN slot. Studio uses the source-shaped oak tree preview for the supported tree specimen.",
            O("none", "Empty", "Garden attachment contains no specimen."),
            O("oak", "Oak tree", "Render the source-shaped GardenTreeShape specimen."));

        ForeverRuntimeStateAxis freezingProgress = A(
            "freezing-progress", "Freezing progress", "50",
            "visualFreezingProgress / FREEZING_REQUIRED_ITEMS. This is independent from recipe phase.",
            O("0", "0%", "Empty freezing column."),
            O("25", "25%", "Quarter-filled freezing column."),
            O("50", "50%", "Half-filled freezing column."),
            O("75", "75%", "Three-quarter-filled freezing column."),
            O("100", "100%", "Full freezing column."));

        var condenserAxes = new[]
        {
            condenserStatus,
            condenserJob,
            condenserAttachment,
            condenserSpecimen,
            freezingProgress
        };

        result["condenser_mark_i"] = new(
            "condenser_mark_i",
            "Condenser Mark I",
            condenserAxes,
            "Source: CondenserRenderer.java + CondenserJob.java. State controls map directly to renderer branch predicates and visual status.");

        result["condenser_mark_ii"] = new(
            "condenser_mark_ii",
            "Condenser Mark II",
            condenserAxes,
            "Source: CondenserRenderer.java + CondenserJob.java. Milling, mixing, cultivating and composting use their actual source branches.");

        result["ascension_chamber"] = new(
            "ascension_chamber",
            "Ascension Chamber",
            [
                A("activity", "Activity", "active",
                    "AscensionChamberBlock.ACTIVE; inactive holds runtime phase at zero and uses inactive accents.",
                    O("idle", "Idle", "Static rings and inactive accents."),
                    O("active", "Active", "Rings, core and particles follow timeline phase.")),
                A("core", "Inserted core", "present",
                    "Renderer checks inventory slot 0 before drawing the inserted core.",
                    O("empty", "Empty", "No inserted runtime core."),
                    O("present", "Present", "Draw inserted core and active motion."))
            ],
            "Source: AscensionRenderer.java.");

        result["refining_chamber"] = new(
            "refining_chamber",
            "Refining Condenser",
            [
                A("activity", "Processing", "running",
                    "MachineVisualState phase advances while processing and holds when processing stops.",
                    O("idle", "Idle", "Preview the source renderer at phase zero."),
                    O("running", "Running", "Timeline phase drives both mirrored reactor insets."))
            ],
            "Source: RefiningRenderer.java + MachineVisualState semantics.");

        result["garden_chamber_mark_i"] = new(
            "garden_chamber_mark_i",
            "Garden Chamber Mark I",
            [
                A("link", "Linked Condenser", "linked",
                    "GardenChamberRenderer returns immediately when linkedCondenser() is null.",
                    O("unlinked", "Unlinked", "No BlockEntityRenderer overlay."),
                    O("linked", "Linked", "Use linked Condenser presentation state.")),
                A("status", "Linked status", "running",
                    "Linked Condenser visualStatus. Running additionally requires a non-empty specimen.",
                    O("idle", "Idle", "Static specimen/lamp with neutral status strips."),
                    O("running", "Running", "Growth, irrigation and harvest follow timeline phase."),
                    O("blocked", "Blocked", "Static runtime state with blocked status strips.")),
                A("specimen", "Specimen", "oak",
                    "Garden specimen slot. Current Studio parity has an exact source-shaped oak tree and an explicit empty state.",
                    O("none", "Empty", "No specimen geometry; running is effectively disabled."),
                    O("oak", "Oak tree", "Render GardenTreeShape with source growth factors."))
            ],
            "Source: GardenChamberRenderer.java + GardenPlantRenderer.java. Datapack/crop item-specific rendering remains a later state/effect extension.");

        result["washing_basin"] = new(
            "washing_basin",
            "Washing Basin",
            [
                A("amount", "Stored wash amount", "full",
                    "WashingRenderer returns when visual.amount()==0.",
                    O("dry", "Dry", "No runtime water surface or rings."),
                    O("quarter", "Quarter", "Wet basin; amount is non-zero."),
                    O("half", "Half", "Wet basin; amount is non-zero."),
                    O("full", "Full", "Wet basin; amount is non-zero.")),
                A("status", "Wash status", "running",
                    "WashingRenderer uses running phase rings or three fixed idle rings.",
                    O("idle", "Idle", "Three static circulation rings."),
                    O("running", "Running", "Circulation rings animate with timeline phase."))
            ],
            "Source: WashingRenderer.java.");

        result["crucible"] = new(
            "crucible",
            "Crucible",
            [
                A("fill", "Lava fill", "2",
                    "CrucibleBlock.FILL; convection exists whenever fill is above zero and is independent of recipe activity.",
                    O("0", "Empty", "No runtime lava overlay."),
                    O("1", "Fill 1", "Lowest stored lava level."),
                    O("2", "Fill 2", "Second stored lava level."),
                    O("3", "Fill 3", "Third stored lava level."),
                    O("4", "Fill 4", "Highest stored lava level."))
            ],
            "Source: CrucibleRenderer.java.");

        result["mixing_chamber_mark_i"] = new(
            "mixing_chamber_mark_i",
            "Mixing Chamber Mark I",
            [],
            "Current source registers a simple block with no dedicated BlockEntityRenderer state.");

        result["freezing_chamber_mark_i"] = new(
            "freezing_chamber_mark_i",
            "Freezing Chamber Mark I",
            [],
            "Current source renders freezing progress through the linked Condenser, not through a standalone renderer.");

        return result;
    }
}
