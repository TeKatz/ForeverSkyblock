# Machine visual overhaul

Work on `feature/machine-visual-overhaul`, based on `d6fc9a6` (integrated 0.26.4). This is a presentation-only development update, not a merge or release.

## Scope and visual language

All nine custom machines are covered: Condenser Mark I/II, Mixing Chamber, Freezing Chamber, Garden Chamber, Washing Basin, Crucible, Refining Condenser and Ascension Chamber. All five container screens use a new native, pixel-aligned instrument-panel skin with graphite housings, beveled slots, inset inventory trays, live status, clear keyboard-focus/hover feedback and machine-specific accents. Existing slot coordinates, buttons, ghost targets, JEI integration and Vanilla ItemStack rendering retain the native coordinate contract.

| Machine | Presentation |
| --- | --- |
| Condenser I | Warm copper/titanium, recessed press and centered shrinking workpieces |
| Condenser II | Dedicated six-cell steel atlas instead of sampling a single Freezing tile, counter-rotating rollers |
| Mixing | Dedicated wooden supports, lined basin and live two-ingredient agitation |
| Freezing | Brushed cold metal, recessed ice, frost facets and copper fasteners |
| Garden | Dedicated greenhouse materials and clear cutout glazing, rooted crops, smooth shrinking nutrient motes |
| Washing | Ceramic lining, timber rim, shallow water and circulating ripples; GUI sieve/particle separation |
| Crucible | Refractory housing and metal supports, stored-lava convection and independent melt/freeze meters |
| Refining | Real recessed 3D housing/inventory model, opposed synchronized reactor insets and GUI reaction chamber |
| Ascension | Independent pale/violet material atlas, recipe-driven rings and crystal core |

The approved Garden GUI tree/crop drawing is retained. No harvest items fly into outputs. Product copies shrink into their outlets; motes enter/leave through a bounded envelope. Idle/blocked/dry/unpowered states do not animate fake processing. Stored hot lava retains slow thermal motion independently of production.

All registered custom material items, the Forever Core, upgrade cards, Mortar and Supply Cache have a consistent 32px vector-authored sprite set. The generated machine panel atlas is sampled in its existing four quadrants. Native SVG sources and `tools/build_visual_assets.py` reproduce the other assets with CairoSVG. No image-generation service is used at runtime. The obsolete screenshot GUI atlases and flat Refining inventory sprite are retired.

## Functional preservation

Recipes, yields, durations, FE costs, capacities, input/output policies, capabilities, filters, menus, packet actions, save fields, registration IDs, progression and pack/island files retain their existing implementations. The only server-side addition is a transient presentation observer after Washing/Refining processing. It uses existing block-entity update packets, sends on state/cycle changes and once per running second, and is never consulted by production. Snapshot handling cannot deserialize or replace gameplay inventory or energy. Stale motion clamps after one heartbeat and at completion. No new ticker, custom channel or persistent field is introduced.

## Validation

The existing model/layout, unit, server and pack-integration regressions remain enabled. Added resource checks verify the six-cell material contract, actual greenhouse transparency and packaged texture references. Added GameTests verify paused/unpowered poses, bounded interpolation and the separation between presentation packets and persisted state.

`./gradlew runClient -PcondenserTests -PmachineVisualTests` creates a disposable integrated world, captures all five real GUI implementations in representative states and multiple GUI scales, and renders a nine-machine block gallery. The fixture is excluded from the distributable JAR. These are presentation fixtures, not substitutes for player-operated automation tests. Final shader/resource-pack appearance still requires a real-pack check.

Final source `c34b0d1bb0f18ba4fb5df47f5055cf62b238c07e` passed [Actions run #567](https://github.com/TeKatz/ForeverSkyblock/actions/runs/39758768145):

- Java 21 build, **99 unit tests**, and all GameTest classes compiled;
- client startup/mixin/model/texture smoke check;
- **18 real-client machine screenshots**, covering all five screens and a nine-machine block gallery; reviewed after correcting Ascension label/ring clearance;
- existing K-menu visual/input fixtures;
- **120 required GameTests** on default NeoForge **21.1.249**;
- **120 required GameTests** with full optional integrations on the current pack runtime, NeoForge **21.1.262**.

The final JAR from CI artifact `12417432843` is **1,099,649 bytes**. Its SHA-256 is `e3cd724c4c08fe9eaec0d832218d48dcfabda1b2c1197dcd128c4fcfabfc7abe`. The artifact ZIP and JAR CRCs passed; metadata targets 0.27.0 / Minecraft 1.21.1 / NeoForge, new classes target Java 21, every packaged source resource matches the checkout and test fixtures are excluded. The independently built local JAR is byte-identical. The saved user download was materialized again and its exact size/hash verified.

A final baseline comparison confirms unchanged production, automation, energy and persistence method bodies, unchanged menu/recipe/action-protocol files, and unchanged pack files. The protected island SHA-256 remains `1ae2e4b779b1c6b56320aede0af2a711d4bd247a976771c95f633e05c1beb0c1`. Regenerating all native SVG motifs produced byte-identical PNGs.

The existing native Condenser layout is 248 × 352 logical pixels. At 1920 × 1080 it fits GUI scale 3; scale 4 preserves item/frame alignment but exceeds the available logical height, as documented by the fixture. No fit-to-window transform was reintroduced. Final player-operated appearance/automation and shader/resource-pack combinations still require TeKatz's real-pack check. This final documentation update changes none of the validated runtime source or resources.
