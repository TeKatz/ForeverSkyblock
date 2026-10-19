# Current integrated state

Main now includes **0.24.1 header artwork through PR #17** on top of the 0.24.0 behavior below. The unmerged **0.25.0 K-menu Studio** work is isolated on `feature/kmenu-admin-editor`; see [K_MENU_EDITOR.md](K_MENU_EDITOR.md). Its editor behavior is not yet an integrated-main feature.

Updated October 19, 2026 for the authorized integration of the 0.21–0.24 animation, automation, balancing and K-menu stack through [PR #16](https://github.com/TeKatz/ForeverSkyblock/pull/16).

## Baseline

| Item | Current value |
| --- | --- |
| Mod version | **0.24.1** |
| Minecraft | **1.21.1** |
| Java | **21** |
| Pack runtime | **NeoForge 21.1.259** |
| Default build baseline | NeoForge 21.1.249 |
| Player profile schema | **dynamic 4 / 5 / 6** |
| World-progress schema | **2**, migrated from 1 with backup |
| Progression protocol | **skills-9**, matching client/server required |
| Integration PR | **#17**, following #16 |
| Historical 0.24.0 validated source | `227c81611f8c8cda719c59106c80d6350a2034a1` |
| Historical 0.24.0 pre-merge validation | [Actions 39178417187](https://github.com/TeKatz/ForeverSkyblock/actions/runs/39178417187), success |
| Release | Not published by this merge |

The integrated 0.24.1 baseline commit is `c7246d00456c9c4c23905f496316a72bbcd473e8`. Header artwork is the only 0.24.1 presentation change; the 0.24.0 gameplay evidence below remains historical.

## Integrated gameplay and presentation

The existing progression/meta foundation and 0.19–0.20 material-processing stack remain active. PR #16 includes all intervening feature branches:

- **0.21.1:** job-aware Condenser Mark I/II block and GUI animation, continuous ingredient/result routes and the rebuilt greenhouse. The approved Oak GUI tree is preserved without decorative harvest-item flights; the block tree has a fuller crown.
- **0.22.0:** retained Wheat Seeds produce **4 Wheat + 2 Seeds every 16 seconds**, with shared crop profiles for GUI and both blocks. Garden labels and press-item scaling are centered. Wooden Hopper costs two Iron Ingots, three Planks and a wooden Chest. The Emerald gate blocks the whole Vanilla Trader caravan attempt and fresh untamed Trader llamas, preserving loaded/tamed and ordinary llamas.
- **0.22.1:** separate Garden harvest ports: a hopper below MK II extracts the primary output; one below Garden Chamber extracts the secondary. Numbered GUI badges and a permanent legend explain placement. Both outputs remain in the controller and manually accessible; the Chamber has no independent inventory or GUI.
- **0.23.0:** Logs become four Stone Fragments in **6 seconds** on both Condenser tiers; normal Hopper costs **2 Iron Blocks + 3 Ingots + 1 Chest**. Each permanent Iron tool requires one Iron Block plus its remaining Ingots, matching Stone tool and Chain. Both Condenser tiers with Mixing Chamber process **1 Mud + 1 Wheat -> 1 Packed Mud in 8 seconds**, with JEI support. Garden output badges are centered.
- **0.24.0:** the K-menu owns its layout/input directly, with scrolling categories, readable animated Challenge/Contract details, round Shop nodes, compact profile with player-skin face, custom SP/PP/Tier icons and subtle motion. Purchases, rewards, recovery and respec retain their existing server rules.

See [MACHINE_ANIMATIONS.md](MACHINE_ANIMATIONS.md), [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md) and [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md) for behavior and version-specific evidence. Branch origins in historical validation sections are development history, not outstanding integrations.

## Active early material route

`Wood -> Stone Fragments -> Stone Meal / Dirt -> Ice / Water -> Farm -> Mud -> Packed Mud -> Mud Bricks -> Washing Basin -> Raw Iron -> Iron -> Condenser Mark II -> Garden Chamber`

- Condenser: **1 Log -> 4 Stone Fragments / 6 seconds**; 1 Plank -> 1 Stone Fragment and 8 Leaves -> 1 Bone Meal retain their eight-second cycles.
- Mixing: Stone Meal + Sapling -> Dirt; 4 Dirt + 1 Ice -> 4 Mud; 1 Mud + 1 Wheat -> 1 Packed Mud. Mark II additionally makes 2 Oak Saplings from 4 Wheat Seeds + 1 Bone Meal.
- Freezing: the independent counter consumes 32 accepted items for 1 Ice. The Crafting Table remains Stone Meal-gated.
- Washing Basin: **4 Stone Meal -> 1 Raw Iron / 8 seconds**, directly adjacent source water required and never consumed.
- Mark II: 4 Stone Fragments -> 1 Stone Meal without the Mortar.
- Garden + Mark II: retained Oak Sapling -> **4 Logs + 8 Leaves / 16 seconds**; retained Wheat Seeds -> **4 Wheat + 2 Seeds / 16 seconds**.

Garden mode is exclusive to Mark II and takes priority over normal Condensing, Mixing and Freezing. Stored items, filters and partial progress resume after detaching the Garden Chamber. Hidden inactive slots are not exposed to automation. Either full harvest output pauses production; no overflow is deleted. The compatibility-preserved Garden registry ID remains `forever_skyblock:garden_chamber_mark_i`.

Existing Recipe Editor overrides must be refreshed with the nine matching 0.23.0 files described in [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md#iron-priced-normal-hopper-and-tools); stale local tool recipes can shadow the JAR's new prices.

## Statistics and persistence

K-menu playtime, deaths and mob kills come from the player's real Vanilla server statistics in the current save. Island Level is shared save-wide progress: one level per distinct completed world milestone, currently at most 20. Tracking begins with this update, including partial tool sets and bounded kill evidence. Permanent profile history is not imported and the display awards no extra SP or gameplay bonus. This is not a separate team-island ranking.

Player profiles remain schema 4/5/6. World-progress schema 2 preserves stable world identity, the Trader unlock and world upgrades while adding milestone evidence. A rollback to 0.23.x requires restoring the pre-update world-progress file together with the earlier JAR. See [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md#island-level-explicitly-chosen-world-progress).

## Meta-progression boundary

- **20 unique challenges / 58 SP**, permanent **Tier 0 — Foundations** and **Tier I — Conqueror**.
- Five repeatable contracts remain active after Tier I, with explicit resource turn-in in their detail view.
- Jetpack research remains **5 / 10 / 20 / 40 SP**; Creative also requires **1 PP**.
- The Tier-I Advanced AE2 Starter Package remains **12 SP** for the current world.
- Tier-0 world upgrades remain No Phantoms (1 SP), No More Rain (2 SP) and Permanent Day (3 SP).

Tier II–IV, Industrial Contract, additional packages, final SP/contract/late-game balance, unresolved shared-machine attribution, Lava/Obsidian progression and first-acquisition routes for non-Oak trees remain unfinished. First Iron is implemented through the Washing Basin; Wheat is an active Garden crop.

## Balancing baseline

TeKatz's automated Iron/Bone Meal line demonstrated rapid bulk Iron after setup. Future costs must account for automated throughput and parallel scaling. The six-second Log trial reduces backlog growth but does not balance a single processor: one Garden supplies **15 Logs/minute**, while one Condenser consumes **10/minute** before pauses. See [the agreed Iron baseline](DESIGN.md#automated-iron-as-a-balancing-baseline--october-19-2026).

## Pack and validation boundary

The fixed island `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` retains its exact bytes, spawn `[8, 4, 5]` facing east and eight-bread/one-seed 1k cell. Git blob: `4c0e3ada63bb61ef1d109ad110b66b6d359d473f`; SHA-256: `1ae2e4b779b1c6b56320aede0af2a711d4bd247a976771c95f633e05c1beb0c1`.

The October 18 mod-list inventory remains experimental runtime evidence, not a launcher lockfile or final mod selection.

Historical 0.24.0 source `227c81611f8c8cda719c59106c80d6350a2034a1` passed the Java 21 build, **78 unit tests**, client startup, **17 actual-client EN/DE fixture captures** with four input checks, and **90 GameTests on each runtime** (21.1.249 and pack 21.1.259). The fresh 0.24.0 JAR was inspected against all 158 resources and excludes test fixtures. Integration documentation changes no validated code/resources.

Native fixture rendering is distinct from a full player-instance test. Actual player skin/hat, full-pack scrolling/scale, live purchases and contract/respec flow, multiplayer milestones and reload separation still need acceptance in TeKatz's instance. Earlier user-reported machine tests remain attributed in the feature records. See [VALIDATION.md](VALIDATION.md) for evidence and [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md#validation-and-acceptance) for the historical 0.24.0 artifact hash.
