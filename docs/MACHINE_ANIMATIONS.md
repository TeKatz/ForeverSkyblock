# Machine animation update — 0.21.1

Development branch: [`feature/machine-animation-overhaul`](https://github.com/TeKatz/ForeverSkyblock/tree/feature/machine-animation-overhaul), based on integrated `main` commit `c1f38cb6f9187826af806f7ab4a11ebe0d3f491a` (0.20.1). This document does not imply a merge or release.

## 0.21.1 polish from TeKatz's client feedback

The supplied October 18 screenshot showed the mature block tree as a thick trunk beneath shallow, stretched foliage sheets. The block tree now uses a narrower tapered trunk and a deep crown assembled from compact foliage lobes, with branch ends inside the crown. Covered leaf faces are omitted, and the inventory preview uses the same mature shape. The tree remains inside the existing greenhouse and controller recess at every growth stage.

TeKatz explicitly approved the **Garden GUI tree animation**. Its drawing routine, progress and scenery are preserved; only the decorative Logs/Leaves flight at harvest has been removed. The actual output slots and harvest operation are unchanged.

Other Condenser scenes retain their press, rollers, agitation and cultivation ideas. Each ingredient now follows one route into the mechanism, shrinks smoothly during processing, and yields a result that emerges continuously. The GUI route starts/ends behind physical port covers; the world result goes behind the recessed outlet backplate. No moving ingredient copy is simply switched off at an arbitrary mid-flight threshold. Blocked processes retain their workpiece pose. Debris has a bounded envelope instead of appearing at full size or opacity on a phase boundary.

Source commit `6ad7bb9e235684b56802eb80439a51276c97f857` passed [GitHub Actions run 39119398626](https://github.com/TeKatz/ForeverSkyblock/actions/runs/39119398626): Java 21 build, **72 unit tests**, client startup/model loading, **78 required GameTests** on NeoForge 21.1.249, and **78 required GameTests** with the full integrations on NeoForge 21.1.259. The client smoke check now explicitly rejects broken Forever Skyblock model/texture resources, including invalid empty-face model elements.

The fresh `ForeverSkyblock-1.21.1-NeoForge-0.21.1.jar` was CRC-checked and inspected for matching 0.21.1 metadata, Java 21 classes and the corrected nonempty-face Garden inventory model. SHA-256: `60eb1636e338d4fb988c8ff7f16c8582b0dacd788368d02af51ab4a3792f530e`. The Garden GUI tree routine was compared byte-for-byte against the approved 0.21.0 version. Machine block entities, slots, recipes and pack files are unchanged in this follow-up.

Software raster previews of the actual GUI draw routine and the exact block-tree geometry were inspected; these are not full client screenshots. Final visual confirmation with TeKatz's real pack/shader setup remains a client test. This final documentation-only update does not change the source or resources used by the validated JAR.

## Presentation

| Machine/job | Block exterior | Condenser GUI |
| --- | --- | --- |
| Mark I, wood/ordinary crushing | Feed, compression hold, release, falling recipe result | Matching miniature press cutaway and progress strip |
| Mark II, crushing | Counter-rotating toothed rollers | Twin rotating gears and visible ingredient/result flow |
| Mark II, Stone Meal milling | Faster opposed rollers, fine debris | Faster milling motion and pale progress color |
| Either tier, Leaves -> Bone Meal | Agitation pan and organic debris | Composting paddle, actual Leaves and Bone Meal preview |
| Either tier, two-input mixing | Mixing pan with both real ingredients | Combining ingredients and mixing paddle |
| Mark II, Seeds + Bone Meal -> Saplings | Cultivation pan with emerging result | Cultivation scene with Sapling result preview |
| Mark II + Garden Chamber | Rooted miniature tree, grow-light scanner, harvest sweep | Approved greenhouse growth scene with lamp and growth motes; no flying harvest items |
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

## 0.21.0 baseline validation

The branch adds unit coverage for bounded press travel, monotonic rooted growth, harvest staging, completed-phase clamping and clear greenhouse mesh geometry without coplanar overlapping faces. Dedicated GameTests cover recipe/job classification, freezing-only snapshots, output-blocked snapshots, real duration/result round-trips, transient save boundaries, Garden blocking and controller removal. The existing full regression/integration workflow remains enabled.

The prior 0.21.0 source commit `643f3ebd64452b461276a85134c1e12d204b8d48` passed [GitHub Actions run 39117087166](https://github.com/TeKatz/ForeverSkyblock/actions/runs/39117087166):

- Java 21 build and **69 unit tests**;
- headless NeoForge client mixin/startup smoke test;
- **78 required GameTests** on NeoForge 21.1.249;
- **78 required GameTests** with the full optional integrations on pack runtime NeoForge 21.1.259.

The resulting `ForeverSkyblock-1.21.1-NeoForge-0.21.0.jar` was downloaded from that exact run, CRC-checked and inspected for 0.21.0 mod metadata, Java 21 class files, all new renderers and matching Garden models. SHA-256: `8d6b08c68fe422560e69505b5033a233ce552817052950aec46b16bae6cb1984`. GameTest classes are excluded from the delivered mod JAR. This final documentation update changes no built code or resources.

A direct comparison against the base verified that the production methods, Condenser menu/slots/handlers and fixed starter-island bytes are unchanged. Both Garden meshes were checked for coplanar overlaps; compact GUI process/growth routines were inspected using a software raster adapter with placeholder item swatches. These checks and the headless launch do not prove final in-world rendered pixels. Final appearance, motion smoothness and resource-pack/shader combinations still require testing the delivered JAR in the real client.

Suggested client checks: Mark I/II Logs and Leaves; Mark-II Stone Meal; Dirt/Mud mixing; Sapling cultivation; Garden growth through a full 16-second cycle; either full harvest slot; empty specimen; module removal/reattachment; freezing-only work; all four Condenser orientations; close/reopen GUI; chunk reload; small and large GUI scale.


## 0.22.0 Garden crops and pack fixes

This bounded follow-up uses `feature/garden-wheat-polish`, based on animation commit `65922b4f4305c99067d57b97517fe8d68345d3ae`. It does not merge either development branch into main.

Garden labels move up four logical pixels; Harvest is centered at the midpoint of the two real harvest slots. Small process items now render about their centre (`-8,-8` in their scaled item pose), with input targets at the press/roller centre. Intake and outlet covers still hide route endpoints. The approved `renderGardenTree` routine is unchanged.

The new data recipe `garden/wheat.json` retains **one Wheat Seeds** and produces **4 Wheat + 2 Wheat Seeds every 320 ticks**. It uses the existing Garden recipe type, one-item specimen slot, exclusive Mark-II mode, automation, two-output capacity checks, pause/resume and JEI category. No persistent fields, menu slots or production algorithms change.

### Small crop template

`GardenCropProfile` is a presentation-only template, separate from production recipes. Its immutable stalk layout, height, delayed growth, ear emergence, ripening palette and bounded sway are shared by `GardenCropScene` and `GardenPlantRenderer`. Wheat has eight rooted stalks, paired leaves and tapered heads; it matures before the existing harvest sweep. The greenhouse uses full-size geometry; the Mark-II controller uses the same geometry at its existing miniature scale. No new ticker or packets are needed. Paused jobs stop their motion. No decorative harvest items fly into outputs.

For a future crop:

1. Add a normal Garden recipe with retained specimen, two outputs and processing time.
2. Define a `GardenCropProfile` with the crop's stalk layout, height and palette; preserve the 13.15-unit greenhouse lamp clearance including the root height and grain heads.
3. Associate its specimen with the profile in the presentation resolver and reuse the GUI/block crop renderers. Crops with different structures may supply a distinct renderer while keeping recipe/production state independent.
4. Check first sprout, leaf growth, maturation, harvest, blocked outputs and reload. Add a real production regression for the new recipe. Do not copy processing logic into rendering code.

The crop profile is the initial small template, not a new configurable gameplay system. Further plants and their balances are not added implicitly.

### Requested pack fixes

The conditional `forever_skyblock:wooden_hopper` recipe copies Wooden Hopper 1.21.1's three-row layout and wooden-Chest tag, replacing only the top corners with Iron. `PackRecipes` retains the existing authoritative tool filtering and removes competing Hopper-output recipes whenever the replacement exists, both on initial load and `replaceRecipes`/reload. It leaves other recipes intact and does not require Wooden Hopper in standalone installs. The optional CI runtime now loads the pack's exact Wooden Hopper **3.2.1.0**, CurseForge project 440985 / file 6753722.

A `WanderingTraderSpawner.spawn` head injection blocks the complete Vanilla caravan attempt before it creates either Trader llama. The existing entity-join guard additionally rejects fresh untamed Trader llamas (including those introduced by other spawn paths). It preserves llamas loaded from disk, tamed Trader llamas and all ordinary llamas. Real-player Emerald pickup still uses the same persistent world unlock and notification. No new per-tick polling is added.

Validation is pending for this new source revision. Software raster previews of actual GUI drawing code were inspected, but are not in-game client screenshots. Final visual confirmation remains a real-client test.
