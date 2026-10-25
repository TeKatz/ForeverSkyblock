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
/// Forever Skyblock's current renderers/block entities; it does not create a universal state system.
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
            "CondenserBlockEntity visualStatus; controls running-only effects and the six process status segments.",
            O("idle", "Idle", "Neutral status lights; timeline represents held visual progress."),
            O("running", "Running", "Timeline phase advances the active recipe presentation."),
            O("blocked", "Blocked", "Blocked status lights; timeline represents held visual progress."));

        ForeverRuntimeStateAxis condenserJob = A(
            "job", "Resolved job", "crushing",
            "CondenserJob from the resolved recipe. This selects the renderer branch instead of abusing Runtime curve as a branch selector.",
            O("idle", "Idle", "No resolved recipe job."),
            O("crushing", "Crushing", "Standard press/roller processing path."),
            O("milling", "Milling", "Mark II rollers use the 1440° milling path."),
            O("composting", "Composting", "Organic rotor path at 720° per cycle."),
            O("mixing", "Mixing", "Dual-input mixer path at 1080° per cycle."),
            O("cultivating", "Cultivating", "Dual-input mixer path with cultivating-specific product timing."));

        ForeverRuntimeStateAxis condenserAttachment = A(
            "attachment", "Attachment state", "none",
            "Renderer predicates from hasGardenChamber()/hasFreezingChamber(). Garden replaces the process branch; Freezing adds its independent gauge.",
            O("none", "None", "Normal process renderer."),
            O("garden", "Garden Chamber", "Use the Garden attachment renderer branch."),
            O("freezing", "Freezing Chamber", "Normal process branch plus freezing-progress column."),
            O("garden+freezing", "Garden + Freezing", "Garden renderer branch plus freezing-progress column."));

        ForeverRuntimeStateAxis condenserSpecimen = A(
            "specimen", "Garden specimen", "oak",
            "Presence of the Condenser GARDEN_SPECIMEN slot. The state is source-backed; the controller miniature remains a simplified Studio proxy in 17C.1.",
            O("none", "Empty", "Garden attachment contains no specimen."),
            O("oak", "Oak specimen", "Represent a present oak specimen in the Garden attachment."));

        ForeverRuntimeStateAxis freezingStatus = A(
            "freezing-status", "Freezing status", "running",
            "CondenserBlockEntity visualFreezingStatus. IDLE and RUNNING intentionally share the same frost material in CondenserRenderer; BLOCKED uses the warning material.",
            O("idle", "Idle", "Source status IDLE; same frost material as RUNNING."),
            O("running", "Running", "Source status RUNNING; same frost material as IDLE."),
            O("blocked", "Blocked", "Source status BLOCKED; warning frost material."));

        ForeverRuntimeStateAxis freezingProgress = A(
            "freezing-progress", "Freezing progress", "16",
            "visualFreezingProgress out of FREEZING_REQUIRED_ITEMS = 32; independent from recipe phase.",
            O("0", "0 / 32", "Empty freezing column."),
            O("8", "8 / 32", "Quarter progress sample."),
            O("16", "16 / 32", "Half progress sample."),
            O("24", "24 / 32", "Three-quarter progress sample."),
            O("32", "32 / 32", "Required item count reached."));

        var condenserAxes = new[]
        {
            condenserStatus,
            condenserJob,
            condenserAttachment,
            condenserSpecimen,
            freezingStatus,
            freezingProgress
        };

        result["condenser_mark_i"] = new(
            "condenser_mark_i",
            "Condenser Mark I",
            condenserAxes,
            "Source: CondenserRenderer.java + CondenserJob.java + CondenserBlockEntity.java. State controls map to renderer predicates and visual-state fields.");

        result["condenser_mark_ii"] = new(
            "condenser_mark_ii",
            "Condenser Mark II",
            condenserAxes,
            "Source: CondenserRenderer.java + CondenserJob.java + CondenserBlockEntity.java. Milling, mixing, cultivating and composting use their actual source branches.");

        result["ascension_chamber"] = new(
            "ascension_chamber",
            "Ascension Chamber",
            [
                A("activity", "Activity", "active",
                    "AscensionChamberBlock.ACTIVE; inactive forces renderer phase to zero and uses inactive accents.",
                    O("idle", "Idle", "Static rings and inactive accents."),
                    O("active", "Active", "Rings, core and particles follow timeline phase.")),
                A("core", "Inserted core", "present",
                    "Renderer checks inventory slot 0 before drawing the inserted core.",
                    O("empty", "Empty", "No inserted runtime core."),
                    O("present", "Present", "Draw inserted core and its active motion."))
            ],
            "Source: AscensionRenderer.java.");

        result["refining_chamber"] = new(
            "refining_chamber",
            "Refining Condenser",
            [
                A("status", "Machine status", "running",
                    "RefiningChamberBlockEntity defines IDLE/RUNNING/BLOCKED/NO_POWER. RefiningRenderer reads status separately from phase; BLOCKED and NO_POWER share warning glow.",
                    O("idle", "Idle", "Neutral glow; timeline represents held phase."),
                    O("running", "Running", "Active glow; timeline phase represents processing progress."),
                    O("blocked", "Blocked", "Blocked-output warning glow; timeline represents held phase."),
                    O("no-power", "No Power", "Power-loss warning glow; timeline represents held phase."))
            ],
            "Source: RefiningRenderer.java + RefiningChamberBlockEntity.java + MachineVisualState. Status and phase are intentionally separate controls.");

        result["garden_chamber_mark_i"] = new(
            "garden_chamber_mark_i",
            "Garden Chamber Mark I",
            [
                A("link", "Linked Condenser", "linked",
                    "GardenChamberRenderer returns immediately when linkedCondenser() is null.",
                    O("unlinked", "Unlinked", "No BlockEntityRenderer overlay."),
                    O("linked", "Linked", "Use linked Condenser presentation state.")),
                A("status", "Linked status", "running",
                    "Linked Condenser visualStatus. Running-only irrigation/harvest additionally require a non-empty specimen; lamp/plant retain the selected phase when idle or blocked.",
                    O("idle", "Idle", "Neutral status strips; timeline represents held growth progress."),
                    O("running", "Running", "Irrigation and harvest are enabled and timeline phase advances."),
                    O("blocked", "Blocked", "Blocked status strips; timeline represents held growth progress.")),
                A("specimen", "Specimen", "oak",
                    "Garden specimen slot. Current Studio parity has an exact source-shaped oak tree and an explicit empty state.",
                    O("none", "Empty", "No specimen geometry; running-only effects are disabled."),
                    O("oak", "Oak tree", "Render GardenTreeShape with source growth factors."))
            ],
            "Source: GardenChamberRenderer.java + GardenPlantRenderer.java. Exact crop/datapack item-specific specimen rendering remains a later state/effect extension.");

        result["washing_basin"] = new(
            "washing_basin",
            "Washing Basin",
            [
                A("amount", "Stored wash amount", "present",
                    "WashingRenderer only branches on visual.amount()==0 versus non-zero; it does not vary the rendered surface by amount.",
                    O("dry", "0 · Dry", "No runtime water surface or rings."),
                    O("present", "> 0 · Present", "Render the source water surface and circulation rings.")),
                A("status", "Wash status", "running",
                    "WashingBasinBlockEntity defines IDLE/RUNNING/BLOCKED/NO_WATER. WashingRenderer animates only RUNNING; all other statuses use the fixed idle rings.",
                    O("idle", "Idle", "Three static circulation rings."),
                    O("running", "Running", "Circulation rings animate with timeline phase."),
                    O("blocked", "Blocked", "Blocked-output status; renderer uses static circulation rings."),
                    O("no-water", "No Water", "Missing-water status; renderer uses static circulation rings."))
            ],
            "Source: WashingRenderer.java + WashingBasinBlockEntity.java. Amount is intentionally represented only as zero/non-zero because that is the renderer's complete visual branch.");

        result["crucible"] = new(
            "crucible",
            "Crucible",
            [
                A("fill", "Lava fill", "4",
                    "CrucibleBlock.FILL is IntegerProperty 0..8; convection exists whenever fill is above zero and is independent of recipe activity.",
                    O("0", "0 · Empty", "No runtime lava overlay."),
                    O("1", "Fill 1", "CrucibleBlock.FILL = 1."),
                    O("2", "Fill 2", "CrucibleBlock.FILL = 2."),
                    O("3", "Fill 3", "CrucibleBlock.FILL = 3."),
                    O("4", "Fill 4", "CrucibleBlock.FILL = 4."),
                    O("5", "Fill 5", "CrucibleBlock.FILL = 5."),
                    O("6", "Fill 6", "CrucibleBlock.FILL = 6."),
                    O("7", "Fill 7", "CrucibleBlock.FILL = 7."),
                    O("8", "Fill 8", "CrucibleBlock.FILL = 8."))
            ],
            "Source: CrucibleBlock.java + CrucibleRenderer.java.");

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
