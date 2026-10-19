# Changelog

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

## 0.20.1 — Garden presentation cleanup and main integration

- Rename the player-facing **Garden Chamber Mark I** to **Garden Chamber** while retaining the internal registry ID `forever_skyblock:garden_chamber_mark_i` for development-world compatibility.
- Replace the placeholder Garden Chamber exterior with a small greenhouse-style model and add subtle active interior animation while the attached Condenser is growing.
- Add a Garden-specific exterior state to the Condenser so active Garden mode visibly differs from normal crusher operation.
- Rework the live Garden Condenser GUI with a larger layered green landscape and a more detailed staged tree-growth presentation.
- Simplify the JEI Garden page: remove the decorative tree-growth animation and use a compact static specimen -> harvest layout with duration and requirement information.
- Preserve Garden gameplay values from 0.20.0: retained Oak Sapling specimen, 16-second cycle, 4 Oak Logs + 8 Oak Leaves, Garden priority over Mixing/Freezing and dedicated automation slots.
- Final pre-merge feature head `aec270d7bc0caea5952af034ed8505ce0fd3d68f` passed GitHub Actions run #259: build/unit tests, headless client mixin smoke, dedicated regression GameTests, full NeoForge 21.1.259 integration tests and artifact upload.
- Integrate the material-processing/Garden stack into `main` through PR #15 on October 18, 2026. No release is published and the fixed starter-island NBT is unchanged.

## 0.20.0 — Garden Chamber and wood-loop automation

- Add the exclusive **Garden Chamber** operating mode for Condenser Mark II.
- Garden mode takes priority over normal Condensing, Mixing and Freezing while preserving their stored items, filters and partial progress for exact resume when the Garden Chamber is removed.
- Add a retained one-item specimen slot and two harvest output slots.
- Add the first Garden recipe: **1 retained Oak Sapling -> 4 Oak Logs + 8 Oak Leaves in 320 ticks / 16 seconds**.
- Add Garden item-handler automation and a dedicated JEI Garden category.
- Change the Mark-II Sapling recipe to **4 Wheat Seeds + 1 Bone Meal -> 2 Oak Saplings**, removing the Leaf-consumption softlock pressure.
- Make one Bone Meal use on a Vanilla Sapling immediately attempt full growth while normal placement constraints still apply.
- Keep Garden recipes data-driven so additional species/crops can be added only after their first-acquisition paths exist.

## 0.19.1 — Material-processing presentation and balance cleanup

- Give Condenser Mark II a detailed machine exterior instead of the placeholder-like flat block look.
- Increase Washing Basin GUI spacing and clean up Condensing/Washing JEI requirement layout.
- Remove the Freezing Chamber top-rim texture seam.
- Restrict the automated Sapling path to Oak until other tree species have real first-acquisition routes.
- Add reliable one-use Bone Meal Sapling growth behavior to avoid passive waiting in the wood-heavy early game.

## 0.19.0 — Material processing and first Iron

- Add the Mixing Chamber recipe **4 Dirt + 1 Ice -> 4 Mud**, connecting the Dirt/Freezing loop to Vanilla Packed Mud and Mud Bricks.
- Add the Washing Basin, crafted from Mud Bricks, Cobblestone and Planks. It requires a directly adjacent source-water block, does not consume the water and pauses without losing partial progress when water is removed.
- Add deterministic first-metal processing: **4 Stone Meal -> 1 Raw Iron in 160 ticks / 8 seconds** with no power.
- Make Condenser Mark I reproducible before Iron using Mud Bricks, Cobblestone, Stone Meal and a Furnace.
- Add the Iron-gated Condenser Mark II using one Mark I, four Iron Ingots and four Mud Bricks while retaining the existing Mixing/Freezing modules, locks, automation and dynamic Condenser UI.
- Add Mark-II-only **4 Stone Fragments -> 1 Stone Meal** automation.
- Introduce recipe-tier metadata, Mark-II JEI requirements and transfer rejection when a Mark-II-only recipe is requested from a Mark I.
- Add dedicated regression coverage for Mud consumption, source-water gating, Washing pause/resume and automation, Mark-I tier refusal and Mark-II processing.

## 0.18.1 — Freezing Chamber cleanup and main integration

- Fix top-side z-fighting on the Freezing Chamber while retaining the approved recessed-basin Design 3 silhouette.
- Increase Condenser screen height and vertical spacing so Condensing, Freezing, actions, module state and player inventory no longer feel compressed.
- Add a dedicated JEI Freezing category in the same visual language as Condensing: wildcard input, 32-item progress, Ice output, no power and adjacent Freezing Chamber requirement.
- Remove the overlapping per-slot lock help tooltip; lock instructions remain on the lock-edit button while normal item tooltips remain readable.
- Remove the old hidden recipe-list click target between the main input/output slots.
- Final feature head `97715f06391339141032c5f0c859b5925f99539d` passed GitHub Actions run #195.
- Integrate the full 0.14.0-0.18.1 development stack into `main` through PR #13 on October 17, 2026. No release was published and the fixed starter-island NBT was not changed.

## 0.18.0 — Freezing Chamber Mark I and Crafting Table gate

- Add the passive Freezing Chamber Mark I as a direct Condenser attachment with no standalone GUI or inventory.
- Add stable Freezing input/output slots inside the Condenser and an independent 0/32 material counter: every 32 consumed items create 1 Ice.
- Allow every registered item as Freezing input unless its persistent input lock restricts it.
- Preserve Freezing input/output, partial progress and lock while the Chamber is detached; disconnected slots remain inaccessible to automation.
- Extend the Condenser to four live GUI states: base, Mixing only, Freezing only and Mixing + Freezing.
- Keep Mixing and Freezing processes independent and able to run in parallel.
- Exclude the universal Freezing input from generic shift-click routing so valuable items are not silently consumed.
- Add the Freezing Chamber recipe from one Mixing Chamber plus current-progression Cobblestone, Stone Meal and Bone Meal.
- Replace the normal Crafting Table recipe with four Planks around one Stone Meal, intentionally keeping the starter ME Crafting Terminal as the first 3x3 crafting workstation.

## 0.17.1 — Emergency client hotfix

- Correct the premium World Shop mixin injection target from the obsolete `cards` name to the actual `generalCards` method.
- Rename colliding private helper methods across premium-screen mixins.
- Add a regression test for the target method and a headless NeoForge client smoke stage.
- Restore successful client startup after the 0.17.0 regression.

## 0.17.0 — Early-game pacing and world QoL

- Add counted Condenser inputs and **8 Leaves -> 1 Bone Meal in 8 seconds**.
- Add a save-wide Wandering Trader gate unlocked by the first real-player Emerald pickup.
- Add world-wide Tier-0 Current World upgrades: No Phantoms (1 SP), No More Rain (2 SP) and Permanent Day (3 SP).
- Persist world QoL state against the stable world UUID while keeping profile schemas 4/5/6 and protocol `skills-8` unchanged.

## 0.16.0 — General polish / phase 7

- Harden progression request validation without changing the `skills-8` packet shape or schema 4/5/6 persistence model.
- Correct real World Shop confirmation/claim behavior and Supply Cache capacity handling.
- Extend GameTests and document deferred balance/K-menu work explicitly.

## 0.15.0 — Real current-world shop delivery / phase 6

- Activate the Tier-I Advanced AE2 Starter Package for 12 SP, once per player/stable-world identity.
- Deliver it through a unique owner/world/receipt-bound Supply Cache with durable recovery states.
- Add dynamic schema 6 only when delivery state exists and move the protocol to `skills-8`.

## 0.14.0 — Repeatable contracts / phase 5

- Add Dragon Reborn and Wither Hunter repeatable boss payouts after their one-time milestones.
- Add Singularity, Nitro and Antimatter resource turn-ins with server-side consumption.
- Add replay guards, durable resource intents and dynamic schema 5 for contract state.

## 0.13.4 — Design-2 revision 3 and first main integration

- Rework the status header and challenge/card readability for the Design-2 K menu.
- Preserve server-authoritative progression, schema 4 and `skills-6`.
- Integrate the complete stacked development line into `main` through PR #11 on October 16, 2026.

## 0.13.1-0.13.3 — Design-2 K-menu overhaul

- Add the premium full-screen progression shell, persistent top status area and six-section navigation.
- Add challenge search/filter/sorting and responsive grid/list presentation.
- Refine visual hierarchy while preserving existing progression actions/networking.

## 0.13.0 — Technology challenges / phase 4

- Expand the live catalog to **20 personal challenges / 58 SP**.
- Add bounded AE2, Mekanism, Powah and Mystical Agriculture challenge adapters.
- Keep unreliable shared-machine objectives inactive instead of rewarding ambiguous nearby activity.

## 0.12.0 — Current-world shop safety / phase 3

- Add stable per-save identity, schema-4 world claims, immutable purchase receipts and write-ahead recovery.
- Add the owner/operator Phase-3 test entitlement used to validate the transaction path.

## 0.11.0 — Personal challenge progression / phase 2

- Activate 11 personal challenges / 34 SP, persistent objective evidence and permanent Tier 0/I.
- Add explicit earned/spent/refunded/admin currency accounting and personal action attribution.

## 0.10.0 — Progression navigation foundation / phase 1

- Replace the earlier flat tabs with Overview, Challenges, Shop, Research, Perks and Profile.
- Add live overview data, challenge search/filter/detail presentation and separated Shop sections.

## 0.9.0-0.9.1 — Machine visual overhaul

- Redesign Condenser and Ascension models, animated internals and matching machine GUIs.
- Correct item rendering scale, labels, core-slot border, inventory spacing and button hover/focus feedback.

## 0.8.0 — Ascension and Perk Points

- Add Forever Core, Ascension Chamber and persistent Perk Points.
- Require 40 SP + 1 PP for Creative Jetpack research and add dual-currency refunds.

## 0.7.0 — Permanent tool progression

- Replace the approved 24 Vanilla mining/farming tool recipes with permanent tiered progression.
- Add Reinforced Netherite Ingot and component-preserving Netherite smithing.

## 0.6.1 — Chamber rendering and JEI transfer

- Fix Mixing Chamber occlusion/z-fighting issues.
- Add server-validated JEI Condensing recipe transfer and suppress the Vanilla introductory tutorial sequence.

## 0.6.0 — Condenser mixing

- Add reusable Mortar, Stone Meal and Mixing Chamber Mark I.
- Add two-input Dirt processing and persistent ghost filters for manual/automated input.

## 0.5.0 — Iron Jetpacks research

- Add four sequential research stages at 5/10/20/40 SP and gate supported crafting/JEI/flight paths on research state.

## 0.4.0 — Skill cards and confirmed refunds

- Add clearer full-width skill cards, confirmed reset/refund and a clickable point-reward shortcut.

## 0.3.0-0.3.1 — Persistent skills

- Add cross-world UUID-based progression profiles, First Foundations and Extended Reach.
- Add atomic storage/backups, K-menu access and the readability fix for custom screen rendering.

## 0.2.0-0.2.1 — Condenser Mark I

- Add log/plank processing into Stone Fragments, persistent progress, hopper/pipe support and JEI integration.
- Add the 2x2 cobblestone recipe and revised Stone Fragment sprite.

## 0.1.0-0.1.1 — Initial project import and identity cleanup

- Import the original mod source, starter pack files and documentation foundation.
- Standardize English project documentation, TeKatz identity and `de.tekatz.foreverskyblock` package/group while preserving registered IDs.
