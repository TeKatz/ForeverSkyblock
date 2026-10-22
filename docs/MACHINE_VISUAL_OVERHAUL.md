# Machine visual overhaul

The **0.27.0 machine visual overhaul is integrated into `main` through PR #30**. Its final merge commit is `5d8ab39b5c8820f4b8f1441715ebbbd9ec5b372d`. The original development branch was `feature/machine-visual-overhaul`.

## Scope and visual language

All nine custom machines are covered: Condenser Mark I/II, Mixing Chamber, Freezing Chamber, Garden Chamber, Washing Basin, Crucible, Refining Condenser and Ascension Chamber. All five container screens use a native, pixel-aligned instrument-panel skin with graphite housings, beveled slots, inset inventory trays, live status, clear keyboard-focus/hover feedback and machine-specific accents. Existing slot coordinates, buttons, ghost targets, JEI integration and Vanilla ItemStack rendering retain the native coordinate contract.

| Machine | Presentation |
| --- | --- |
| Condenser I | Warm copper/titanium, recessed press and centered shrinking workpieces |
| Condenser II | Dedicated six-cell steel atlas, counter-rotating rollers |
| Mixing | Dedicated wooden supports, lined basin and live two-ingredient agitation |
| Freezing | Brushed cold metal, recessed ice, frost facets and copper fasteners |
| Garden | Dedicated greenhouse materials and clear cutout glazing, rooted crops, smooth nutrient motes |
| Washing | Ceramic lining, timber rim, shallow water and circulating ripples; GUI sieve/particle separation and final side-panel treatment |
| Crucible | Refractory housing and metal supports, stored-lava convection, independent melt/freeze meters and corrected heat-overlay depth |
| Refining | Recessed 3D housing/inventory model, opposed synchronized reactor insets and GUI reaction chamber |
| Ascension | Independent pale/violet material atlas, recipe-driven rings and crystal core |

The approved Garden GUI tree/crop drawing is retained. No harvest items fly into outputs. Product copies shrink into their outlets; motes enter/leave through a bounded envelope. Idle/blocked/dry/unpowered states do not animate fake processing. Stored hot lava retains slow thermal motion independently of production.

Registered custom material items, the Forever Core, upgrade cards, Mortar and Supply Cache use a consistent sprite family. Dedicated Mark-II/Ascension materials and greenhouse transparency are packaged with the mod.

## Functional preservation

Recipes, yields, durations, FE costs, capacities, input/output policies, capabilities, filters, menus, packet actions, save fields, registration IDs, progression and pack/island files retain their existing implementations. Washing/Refining presentation observers are display-only and are never consulted by production.

The shared GUI contract remains Vanilla `AbstractContainerScreen` coordinates. No custom fit-to-window `PoseStack` scaling/mouse remapping was reintroduced.

## Final polish before merge

Player feedback after the initial 0.27.0 visual build produced several final corrections on the same branch before PR #30 was merged:

- Washing Basin progress time/readout was separated cleanly from the process animation/meter.
- Washing Basin exterior received a clearer wash/water panel treatment.
- Crucible filled-state heat overlays were moved off coplanar exterior faces to eliminate strong Z-fighting.
- The final Crucible correction was committed directly to the branch so the merged source and test JAR use the same assets.

## Validation history

The original overhaul source `c34b0d1bb0f18ba4fb5df47f5055cf62b238c07e` passed Actions run #567 with Java 21 build/unit tests, client startup/model checks, real-client machine fixtures, K-menu fixtures and full GameTests. Its full optional integration suite used the then-current pack runtime **NeoForge 21.1.262**.

Subsequent player-feedback fixes were validated on the same branch through later Actions runs, including the final Crucible-fix run #576. PR #30 was then merged and `main` Actions run #577 passed on merge commit `5d8ab39b5c8820f4b8f1441715ebbbd9ec5b372d`.

Those run-specific NeoForge versions are historical evidence. The repository's current pack/runtime and development target has since moved to **NeoForge 21.1.264**; older validation records are not retroactively relabeled.

The protected island SHA-256 and gameplay production rules were not changed by the visual overhaul.
