# Changelog

## 0.27.0 — machine visual overhaul (development branch, not merged)

- Redesign all nine custom machine surfaces and five machine container screens as a consistent graphite/copper/steel series with task-specific accents.
- Add new 32px material/tool/upgrade sprites, clear greenhouse glazing and independent Mark-II/Ascension material atlases.
- Give Refining a recessed 3D block/item model and synchronized reactor, Washing a real wash-surface renderer and sieve cutaway, and stored Lava slow convection.
- Smooth outlet/mote transitions and use actual recipe progress for Ascension rings. Retain the approved Garden GUI tree.
- Preserve recipe/processing/energy/automation/save behavior and native slot/JEI coordinates. Add transient display-only Washing/Refining snapshots and opt-in real-client machine fixtures.

See [MACHINE_VISUAL_OVERHAUL.md](MACHINE_VISUAL_OVERHAUL.md) for scope and validation.


## 0.26.4 — Resource refining progression integrated — October 22, 2026

- Merge PR #29 (`hotfix/pr28-14-fixes`) into PR #28's `feature/resource-refining-progression` branch, then merge PR #28 into `main`.
- Add **Cobblestone -> Gravel -> Sand** Condenser processing at 120 ticks / six seconds per step.
- Correct the Crucible Stone-Fragment route to **40 Stone Fragments -> 100 mB Lava** while retaining **8 Cobblestone -> 1,000 mB Lava**.
- Add **1 Gravel + 250 mB stored Lava -> 1 Netherrack / 80 ticks** and automatic Crucible item-output pushing into adjacent compatible inventories.
- Extend the renewable Nether loop through Nether Brick Dust and Washing: **1 Dust -> 1 Nether Concentrate + 2 Quartz + 20% Gold Nugget**.
- Add Mark-II Mixing **1 Nether Concentrate + 1 Stone Meal -> 2 Mineral Blend / 160 ticks**.
- Add Washing **1 Mineral Blend -> 8 Redstone / 120 ticks** as the renewable Redstone bridge.
- Add the FE-powered **Refining Condenser** (`forever_skyblock:refining_chamber`) with 100,000-FE internal capacity, 4,000 FE/t receive and item/energy automation.
- Add Refining recipes for Lapis, Copper, Osmium, Emerald and Diamond. The final Diamond cost is **2 Carbon Matrix + 8 Mineral Blend**, 900 ticks and 240 FE/t.
- Add renewable Carbon Matrix from **8 Charcoal + 1 Mineral Blend**.
- Align the Mekanism Basic Logistical Transporter recipe to **2 Iron Ingots + 1 Redstone -> 8** and the Iron Furnaces Iron Furnace to **4 Iron Blocks + 4 Iron Ingots + 1 Furnace**.
- Rework Refining JEI/presentation, add machine and Mineral Blend item textures, correct Crucible JEI to use real fluid amounts and add the filled/heated Crucible exterior state.
- Synchronize Condenser buffered-output counts so the visible output amount includes hidden Buffer Expansion storage.
- Remove the shared machine-GUI fit-to-window `PoseStack` scale/mouse-remapping layer. Machine screens now keep Vanilla `AbstractContainerScreen` coordinates for slots, ItemStacks, hit testing and JEI areas.
- Final pre-integration source `38a730e77c066ada5ade7395568baa04c1c635ff` passed Actions run **#563**: build/unit tests, client smoke, visual fixtures, dedicated-server regression suite and full pack-runtime integration on NeoForge **21.1.259**.
- Add/refresh the live documentation in `PROGRESSION.md`, `RESOURCE_REFINING.md`, `CURRENT_STATE.md`, `NETHER_PROGRESSION.md`, root/mod READMEs and the documentation index.

## Pack runtime snapshot — October 21, 2026

- Refresh the authoritative supplied mod-list snapshot to `Modliste(5).txt` with **87 enabled artifact labels**.
- Change the current installed pack runtime from **NeoForge 21.1.259** to **NeoForge 21.1.262**.
- Record TeKatz's successful modpack test on this October 21 state: **no errors observed**.
- Add `MekanismGenerators-1.21.1-10.7.19.85` to the supplied list.
- Update listed artifacts for Forever Recipe Editor, Forever Skyblock, FTB XMod Compat, JEI, Sophisticated Backpacks and Sophisticated Core. JEI is now **19.69.0.457**.
- Preserve older NeoForge 21.1.259 CI/runtime entries as historical run-specific evidence; this documentation update does not claim those automated runs used 21.1.262.
- No gameplay/code version bump is implied by this pack snapshot documentation update.

## 0.26.4 — Crucible and Condenser MK II automation integrated

- Integrate the Crucible Lava/Obsidian progression through PR #21 and the configurable Condenser Mark II automation layer through PR #22.
- Crucible: 4,000 mB internal Lava buffer, visible fill progression, Torch/Campfire/Lava/Superheating heat tiers, adjacent Fluid Tank / Freezing Chamber detection, Lava-vs-Obsidian routing, timed freezing, JEI, Jade and hopper/cable support.
- Current Crucible material rates are **8 Cobblestone -> 1,000 mB Lava** or **40 Stone Fragments -> 100 mB Lava** after the October 22 balance correction.
- Keep the Mekanism Basic Fluid Tank recipe redstone-free so Redstone can be introduced later as an independent progression step.
- Add 3-point Condenser Mark II upgrade capacity with Processing Speed, Garden Accelerator, Buffer Expansion and Batch Processing cards.
- Add independent Mark II output policies: **Block / Void Excess / Void All**, including separate Garden primary/secondary policies.
- Give all four upgrade cards individual textures and keep effective upgraded timing synchronized with GUI/world animation timing.
- Polish Crucible GUI naming/layout and anchor melt/freeze animation to the central Lava meter and progress bars; replace the fast single-pixel sweep with smoother interpolated progress/highlight motion.
- Final pre-merge head `87f66cd7b3a41ff97bd9053b5392760bc61acc5e` passed Java build, client smoke, visual fixtures, dedicated-server regression tests and full NeoForge 21.1.259 pack-runtime integration.

## 0.25.3 — K-menu Studio integrated

- Integrate the K-menu Studio/editor stack into `main` through PR #19.
- Preserve native layout/quest/node authoring, server-validated publication, drafts, conflict reconciliation, backups and repository/ZIP export.
- Retain the 0.25.x usability work: compact command chrome, collapsible inspector sections, Focus preview, stable Select-mode previews, independent header sub-elements, searchable icon replacement and visual ARGB editing.
- The former `feature/kmenu-admin-editor` and `feature/kmenu-editor-polish` branches are historical development origins, not outstanding integrations.

## CI — Actions usage optimization

- Integrate PR #23: feature branches validate through pull requests rather than duplicate push + PR runs.
- Cancel stale runs, skip the heavyweight workflow for documentation-only changes, cap job runtime and reduce artifact retention.
- Keep the same validation chain: Java build, client smoke, K-menu visual fixtures, dedicated-server regression suite and full NeoForge 21.1.259 pack-runtime integration.

## 0.25.2 — K-menu Studio usability pass (historical development stage)

- Narrow the Studio element rail and inspector so the live K-menu preview remains the dominant workspace; retain one-click Focus mode.
- Shorten state/control labels, clamp overflowing button text and replace opaque layout IDs with friendly names.
- Expose SP, PP, Tier, Playtime and Island-Level header icons/labels/live values as individually selectable editor elements.
- Add localized header-label editing plus searchable registered-item icon replacement while preserving the approved built-in artwork as the default.
- Add a visual ARGB picker with channel sliders, palette swatches, live preview and Default/Apply/Cancel actions for text/background/border/decoration colors.
- Remove the K-menu panel birth-offset animation that could replay on Challenges a few seconds after opening when fresh server data rebuilt the category/body panels.
- Extend the native client fixture to prove a header label can be selected independently before parent-group drag and undo/redo.
- Validated source `f6ad7233f04ea80e8d8d2b6873fdf2a829725dcc` in Actions run `39209084530`; build, client smoke, native visual/input fixture and both server suites passed. Production JAR: `ForeverSkyblock-1.21.1-NeoForge-0.25.2.jar`, SHA-256 `df750cea1e972817749bf5b889a97eef9a333b8d076fb02791c1200be919d1f0`.

## 0.25.1 — K-menu Studio usability polish (historical development stage)

- Reduce the Studio's permanently visible command chrome with Files/More/Help workflows and visibly mark the active workspace.
- Add grouped/collapsible inspector sections, contextual footer guidance and a full-width Focus preview mode.
- Stabilize Select-mode native previews so normal editor rebuilds do not restart preview reveal animation; retain real animation/input in Try mode.
- Refresh the viewport control together with the preview when switching logical preview sizes.

## 0.25.0 — K-menu Studio (historical development stage)

- Add native admin layout, quest/chapter, node, Shop-package and Contract authoring for the K-menu.
- Add private draft history, live native previews, safe publication/revision leases, conflict reconciliation and backup restoration.
- Add server-owned multi-objective quests with persistent SP/PP rewards and bounded per-tick evidence batching.
- Reuse existing world-bound package delivery/resource-contract recovery; preserve published IDs and package contents.
- Export one validated ZIP or a local repository configuration; bundle repository exports as defaults.
- Keep existing recipes, machines, world identity, profile schemas, Tier rules and fixed island unchanged.
- Validation and editor behavior: [K_MENU_EDITOR.md](K_MENU_EDITOR.md).

The complete 0.21–0.24 stack was integrated into `main` through PR #16 and the 0.24.1 artwork through PR #17. Entries below retain their development-origin detail.

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
