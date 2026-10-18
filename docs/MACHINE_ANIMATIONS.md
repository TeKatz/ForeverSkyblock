# Machine animation update — 0.21.0

Development branch: [`feature/machine-animation-overhaul`](https://github.com/TeKatz/ForeverSkyblock/tree/feature/machine-animation-overhaul), based on integrated `main` commit `c1f38cb6f9187826af806f7ab4a11ebe0d3f491a` (0.20.1). This document does not imply a merge or release.

## Presentation

| Machine/job | Block exterior | Condenser GUI |
| --- | --- | --- |
| Mark I, wood/ordinary crushing | Feed, compression hold, release, falling recipe result | Matching miniature press cutaway and progress strip |
| Mark II, crushing | Counter-rotating toothed rollers | Twin rotating gears and visible ingredient/result flow |
| Mark II, Stone Meal milling | Faster opposed rollers, fine debris | Faster milling motion and pale progress color |
| Either tier, Leaves -> Bone Meal | Agitation pan and organic debris | Composting paddle, actual Leaves and Bone Meal preview |
| Either tier, two-input mixing | Mixing pan with both real ingredients | Combining ingredients and mixing paddle |
| Mark II, Seeds + Bone Meal -> Saplings | Cultivation pan with emerging result | Cultivation scene with Sapling result preview |
| Mark II + Garden Chamber | Rooted miniature tree, grow-light scanner, harvest sweep | Greenhouse scene with synchronized rooted growth, lamp, growth motes and recipe-based harvest previews |
| Adjacent Garden Chamber | Rooted tree grows in the soil, lamp follows its rail, harvest sweep closes the cycle | Uses the controller's exclusive Garden screen; no new standalone GUI |

The selected recipe, rather than mere module presence, determines the normal process scene. Existing recipes and custom two-input recipes retain their original matching, quantities and durations. Arbitrary non-Oak Garden specimens retain their item identity in the greenhouse; the currently implemented Oak recipe receives the modeled rooted tree.

The new Garden Chamber exterior has a dark inset base, steel posts, copper trim, clear cutout glazing, soil bed, lamp rails, rear housings and a compact roof vent. Its inventory model includes a static planted specimen; the placed model has no permanent decorative crop. The internal ID remains `garden_chamber_mark_i`.

## State and compatibility

- The existing server processing methods, recipes, automation handlers, slot positions, filters, recipe priority, exclusive Garden rule and save fields are retained.
- The existing viewer update packet carries a transient `Visual` compound with separate normal/Garden/Freezing statuses, real durations, resolved job and result preview. It is not written into persistent machine saves and does not change the menu protocol or progression protocol.
- Status/mode changes trigger the existing viewer sync path immediately. Running jobs retain its bounded one-second heartbeat. No new block ticker, background task or custom network channel is added.
- World interpolation clamps at completion and extrapolates at most one heartbeat; it cannot invent repeated production cycles from a stale snapshot.
- Main and Garden moving effects stop when their own job pauses or blocks, even if parallel Freezing still keeps the shared block `ACTIVE` flag set. The retained tree remains at its real growth stage. Empty/unlinked greenhouses do not grow a fake crop.
- Multiple adjacent controllers are selected deterministically, preferring a running Garden controller; unloaded neighboring chunks are not queried.
- GUI growth and process motion use game progress and partial ticks, replacing wall-clock-driven cloud/shimmer movement. The independent Freezing indication follows its own running status.
- No starter-island, pack configuration, research, production, recipe or inventory migration changes are part of this branch.

## Validation

The branch adds unit coverage for bounded press travel, monotonic rooted growth, harvest staging, completed-phase clamping and clear greenhouse mesh geometry without coplanar overlapping faces. Dedicated GameTests cover recipe/job classification, freezing-only snapshots, output-blocked snapshots, real duration/result round-trips, transient save boundaries, Garden blocking and controller removal. The existing full regression/integration workflow remains enabled.

Final source commit `643f3ebd64452b461276a85134c1e12d204b8d48` passed [GitHub Actions run 39117087166](https://github.com/TeKatz/ForeverSkyblock/actions/runs/39117087166):

- Java 21 build and **69 unit tests**;
- headless NeoForge client mixin/startup smoke test;
- **78 required GameTests** on NeoForge 21.1.249;
- **78 required GameTests** with the full optional integrations on pack runtime NeoForge 21.1.259.

The resulting `ForeverSkyblock-1.21.1-NeoForge-0.21.0.jar` was downloaded from that exact run, CRC-checked and inspected for 0.21.0 mod metadata, Java 21 class files, all new renderers and matching Garden models. SHA-256: `8d6b08c68fe422560e69505b5033a233ce552817052950aec46b16bae6cb1984`. GameTest classes are excluded from the delivered mod JAR. This final documentation update changes no built code or resources.

A direct comparison against the base verified that the production methods, Condenser menu/slots/handlers and fixed starter-island bytes are unchanged. Both Garden meshes were checked for coplanar overlaps; compact GUI process/growth routines were inspected using a software raster adapter with placeholder item swatches. These checks and the headless launch do not prove final in-world rendered pixels. Final appearance, motion smoothness and resource-pack/shader combinations still require testing the delivered JAR in the real client.

Suggested client checks: Mark I/II Logs and Leaves; Mark-II Stone Meal; Dirt/Mud mixing; Sapling cultivation; Garden growth through a full 16-second cycle; either full harvest slot; empty specimen; module removal/reattachment; freezing-only work; all four Condenser orientations; close/reopen GUI; chunk reload; small and large GUI scale.
