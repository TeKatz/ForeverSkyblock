# Changelog

## 0.25.1 — K-menu Studio usability polish (development branch, not merged)

- Replace the flat wall of editor actions with a smaller primary toolbar plus **Files…**, **More…** and **Help/F1** workflows.
- Mark the active workspace, add contextual footer guidance and rename terse layout/graph controls to describe their current state.
- Group the property inspector into collapsible position, appearance, progression, objective, dependency, package and recovery sections.
- Add **Focus preview** to temporarily hide both side panels and give the native preview more working space.
- Keep layout **Select** mode animation-stable so ordinary selection/rebuild clicks no longer restart the K-menu reveal animation; **Try** mode retains the real configured animation timing.
- Rebuild editor controls when changing preview viewport so the visible 640×360 / 480×270 / 944×522 label always follows the active logical size.
- Version the polish build as 0.25.1 without changing editor persistence/protocol semantics, gameplay balance, machines, recipes or fixed island.
- Validate source `56246f2a57672d0fdf966cf865b596eeeaee0019` in Actions run `39204247635`: build/compile, client smoke, native K-menu visual fixtures and both server regression runtimes passed. See [K_MENU_EDITOR.md](K_MENU_EDITOR.md).

## 0.25.0 — K-menu Studio (development branch, not merged)

- Add native admin layout, quest/chapter, node, Shop-package and Contract authoring for the K-menu.
- Add private draft history, live native previews, safe publication/revision leases, conflict reconciliation and backup restoration.
- Add server-owned multi-objective quests with persistent SP/PP rewards and bounded per-tick evidence batching.
- Reuse existing world-bound package delivery/resource-contract recovery; preserve published IDs and package contents.
- Export one validated ZIP or a local repository configuration; bundle repository exports as defaults.
- Keep existing recipes, machines, world identity, profile schemas, Tier rules and fixed island unchanged.
- Validation and remaining acceptance: [K_MENU_EDITOR.md](K_MENU_EDITOR.md).

The complete 0.21–0.24 stack is integrated into `main` through [PR #16](https://github.com/TeKatz/ForeverSkyblock/pull/16), authorized October 19, 2026. Version entries and branch names below record development origins. This integration publishes no release and leaves the fixed starter-island NBT unchanged.

## 0.24.0 — K-menu overhaul

- Rebuild premium menu presentation directly; remove the six accumulated client visual/shop/contract patch mixins.
- Center header/reward/filter/search labels, scroll the mod category rail and add readable animated Challenge/Contract details.
- Replace Shop strips with round-node branches representing existing skill/tier/product prerequisites; preserve all original server transactions.
- Add compact player/stat/accounting profile and actual skin-face tab; custom SP/PP/Tier marks and subtle bounded motion.
- Display authoritative Vanilla world playtime/deaths/kills. Add shared world-milestone Island Level, explicitly chosen by TeKatz, with no additional rewards or imported permanent history.
- Extend protocol to `skills-9`; preserve player schema 4/5/6 and migrate the backed-up world-progress file to schema 2 without losing Trader/world-upgrade state.
- Add persistence/evidence/motion/codec regressions and opt-in actual-client visual fixtures, excluded from the test JAR.
- Validated final source with 78 unit tests, 17 native-client fixture views/input checks and both 90-test server suites; fresh artifact inspected.

## 0.23.0 — Iron balancing, Packed Mud and Garden alignment

Branch `feature/iron-balance-packed-mud`, extending tested 0.22.1:

- Reduce only Log -> four Stone Fragments to 120 ticks / six seconds on both tiers.
- Price the normal Hopper at two Iron Blocks, three Ingots and one Chest; remove old/alternative static Hopper recipes on load/replacement.
- Require one Iron Block in each permanent Iron Pickaxe/Axe/Shovel/Hoe recipe, retaining 4/3/2/2 other Ingots, matching Stone tool and Chain.
- Add Mud + Wheat -> one Packed Mud in eight seconds with Mixing Chamber on MK I/II, including the existing JEI presentation/transfer path.
- Center Garden routing badges and their tooltip areas under the real output slots.
- Synchronize the matching Recipe Editor pack resources and add timing/crafting/production/network regressions.

## 0.22.1 — Separate Garden harvest ports

Branch `feature/garden-harvest-routing`, extending the tested 0.22.0 work:

- Export only the primary Garden harvest through Condenser automation and only the secondary harvest through the Chamber's bottom port.
- Keep all items and production state in the Condenser; preserve both manual GUI output slots and existing saves.
- Show numbered block icons/down arrows below the two harvest slots, plus a permanent routing legend and English/German tooltips.
- Add extraction/simulation, stale-capability/removal and real Vanilla/Wooden Hopper regressions; preserve normal Condensing/Freezing automation.

## 0.22.0 — Garden Wheat and screenshot polish

Branch `feature/garden-wheat-polish`, extending the 0.21.1 animation work:

- Raise Specimen/Harvest labels and center Harvest over both output slots.
- Scale process ingredients toward the centre of the press/rollers.
- Add a retained Wheat Seed Garden recipe: 4 Wheat + 2 Seeds every 16 seconds, with shared GUI/controller/greenhouse crop profiles.
- Preserve the approved Oak GUI tree and its removed harvest-item flights.
- Replace Wooden Hopper's top-corner Planks with two Iron Ingots; suppress the cheaper original/alternate recipes, including on reload.
- Gate the entire Vanilla Trader caravan and fresh untamed Trader llamas until Emerald pickup; preserve saved/tamed Trader llamas and ordinary llamas.
- Add crop geometry/staging, real Wheat harvest/blocking, optional Hopper crafting/reload and llama-gate regressions.

## 0.21.1 — animation polish after client feedback

- Keep the approved Garden GUI tree growth exactly as in 0.21.0; remove only the decorative Logs/Leaves flight into the output area.
- Replace the flat block-tree crown with deep foliage lobes, a tapered slender trunk and branches inside the canopy. Keep its rooted growth and lamp clearance; update the inventory preview to match.
- Give other Condenser workpieces a single continuous intake/processing/discharge path, with smooth consumption and result emergence rather than hard timed disappearance or duplicated ingredient icons.
- Hide GUI route endpoints beneath inlet/outlet covers, and route the world result behind the physical outlet backplate. Retain paused workpiece poses.
- Fade debris in/out across the processing phase; remove covered faces from the fuller canopy mesh.
- Production, timings, recipes, inventories, menu protocol and Garden GUI tree drawing are unchanged. Validation follows in [MACHINE_ANIMATIONS.md](MACHINE_ANIMATIONS.md).

## 0.21.0 — machine animation update

- Branch: `feature/machine-animation-overhaul`, based on `main` at `c1f38cb6f9187826af806f7ab4a11ebe0d3f491a`; subsequently integrated through PR #16, without a release.
- Mark I now has staged feed/press/release/discharge motion; Mark II uses counter-rotating toothed rollers and a distinct milling cycle.
- Actual resolved recipes select crushing, milling, composting, mixing or cultivation scenes, including live ingredients and recipe result previews in the GUI and front recess.
- Garden mode uses rooted growth, travelling grow lights and a final harvest sweep in the controller, GUI and adjacent greenhouse.
- Rebuilt Garden Chamber frame, clear glazing, soil bed, copper rails, roof vent and planted item preview.
- Viewer snapshots include separate process states, actual recipe durations and output previews; freezing-only work no longer drives the main press. Persistent processing rules, slots, recipes, IDs and production timing are unchanged.
- Validation and client test boundary: [MACHINE_ANIMATIONS.md](MACHINE_ANIMATIONS.md).

This file is the concise version history. Detailed test evidence belongs in [VALIDATION.md](VALIDATION.md); current integrated repository status belongs in [CURRENT_STATE.md](CURRENT_STATE.md).
