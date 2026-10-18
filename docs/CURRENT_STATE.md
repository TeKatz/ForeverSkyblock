# Current integrated state

Updated October 18, 2026 for the authorized integration of the material-processing and Garden-automation stack through **0.20.1** via PR #15.

## Baseline

| Item | Current value |
| --- | --- |
| Mod version | **0.20.1** |
| Minecraft | **1.21.1** |
| Java | **21** |
| Pack runtime | **NeoForge 21.1.259** |
| Default build baseline | NeoForge 21.1.249 |
| Profile schema | **dynamic 4 / 5 / 6** |
| Progression protocol | **skills-8** |
| Integration PR | **#15** |
| Final pre-merge feature head | `aec270d7bc0caea5952af034ed8505ce0fd3d68f` |
| Verified pre-merge CI | GitHub Actions **#259**, success |
| Release | Not published by this merge |

## Development branch: machine animations (0.21.1)

`feature/machine-animation-overhaul` starts from `main` commit `c1f38cb6f9187826af806f7ab4a11ebe0d3f491a` and contains the job-aware block/GUI animation update, rebuilt Garden Chamber and 0.21.1 polish following TeKatz's screenshot feedback. This remains development work; the baseline table above describes integrated 0.20.1.

The 0.21.1 follow-up adds a fuller block-tree crown, preserves the approved Garden GUI tree without harvest-item flights, and gives other Condenser jobs continuous workpiece transitions. Source commit `6ad7bb9e235684b56802eb80439a51276c97f857` passed build, 72 unit tests, client startup/model checks and both 78-test server suites in Actions run `39119398626`; its fresh 0.21.1 test JAR was inspected. See [MACHINE_ANIMATIONS.md](MACHINE_ANIMATIONS.md) for behavior, artifact hash and the remaining in-client visual test boundary.

## Development follow-up: Garden crops and polish (0.22.0)

`feature/garden-wheat-polish` branches from animation head `65922b4f4305c99067d57b97517fe8d68345d3ae`; both development branches descend from the unchanged main baseline above. It adds retained Wheat Seeds -> **4 Wheat + 2 Seeds / 16 seconds**, shared crop animation profiles, raised/centered Garden labels and centered workpiece scaling. The approved Oak GUI tree is unchanged. It also prices Wooden Hopper's two top corners at one Iron Ingot each, removes cheaper alternate Hopper recipes, and gates fresh Trader llamas plus the complete Vanilla caravan attempt before Emerald unlock. Saved/tamed Trader llamas are preserved. These changes are not integrated into `main`. See [MACHINE_ANIMATIONS.md](MACHINE_ANIMATIONS.md) for implementation and validation.

## Integrated gameplay and UI

`main` contains the complete progression/meta stack through 0.18.1 plus the 0.19.x-0.20.1 early material-processing expansion:

- repeatable contracts, real current-world package delivery, world QoL upgrades and the existing Tier 0/I meta-progression;
- Condenser Mark I, Mixing Chamber, Freezing Chamber, persistent input locks and JEI integration;
- Stone Meal-gated Crafting Table and 8 Leaves -> 1 Bone Meal early loop;
- permanent Vanilla tool progression, Jetpack research, Ascension/Perk Points, challenges and the Design-2 K menu;
- **4 Dirt + 1 Ice -> 4 Mud** through Condenser + Mixing Chamber, bridging Water/Farming into Vanilla Packed Mud and Mud Bricks;
- a standalone **Washing Basin** that requires directly adjacent source water and processes **4 Stone Meal -> 1 Raw Iron** in 8 seconds without consuming the water;
- a reproducible pre-Iron **Condenser Mark I** recipe using Mud Bricks, Cobblestone, Stone Meal and a Furnace;
- Iron-gated **Condenser Mark II**, retaining the Mark-I/Mixing/Freezing behavior while adding **4 Stone Fragments -> 1 Stone Meal** automation;
- the Mark-II Mixing recipe **4 Wheat Seeds + 1 Bone Meal -> 2 Oak Saplings**, avoiding the earlier Leaf-consumption softlock risk;
- reliable Sapling growth: one Bone Meal use immediately advances a Vanilla Sapling through its growth attempt while normal tree-placement constraints still apply;
- the exclusive **Garden Chamber** operating mode for Condenser Mark II: a retained Oak Sapling specimen grows for 16 seconds and produces **4 Oak Logs + 8 Oak Leaves** without consuming the specimen;
- Garden mode pauses normal Condensing, Mixing and Freezing while preserving their stored items, filters and partial progress for exact resume after detaching the Garden Chamber;
- dedicated Garden JEI presentation, Garden automation slots, a greenhouse-style Garden Chamber exterior with active interior animation, and a distinct green Condenser exterior/GUI state while Garden growth is running.

The Garden Chamber's player-facing name is intentionally **Garden Chamber**. Its internal registered ID remains `forever_skyblock:garden_chamber_mark_i` for compatibility with the already-tested 0.20.0 development worlds and items.

## Active early progression boundary

The currently implemented early material route is:

`Wood -> Stone Fragments -> Stone Meal / Dirt -> Ice / Water -> Farm -> Mud -> Packed Mud -> Mud Bricks -> Washing Basin -> Raw Iron -> Iron -> Condenser Mark II -> Garden Chamber`

Important active recipes and machine rules:

- Condenser: 1 Log -> 4 Stone Fragments; 1 Plank -> 1 Stone Fragment; 8 Leaves -> 1 Bone Meal.
- Mixing Chamber: Stone Meal + Sapling -> Dirt; 4 Dirt + 1 Ice -> 4 Mud; Mark II additionally uses 4 Wheat Seeds + 1 Bone Meal -> 2 Oak Saplings.
- Freezing Chamber: any accepted item advances the independent 32-item counter; 32 consumed items create 1 Ice.
- Washing Basin: 4 Stone Meal -> 1 Raw Iron, 8 seconds, adjacent source water required and never consumed.
- Condenser Mark II: 4 Stone Fragments -> 1 Stone Meal without the Mortar.
- Garden Chamber + Mark II: retained Oak Sapling specimen -> 4 Oak Logs + 8 Oak Leaves every 16 seconds.

This closes the permanent manual wood dependency at the first-Iron/Mark-II tier: Garden Logs feed Stone Fragments, Garden Leaves feed Bone Meal, Mark II automates Stone Meal, and the Mixing Chamber can feed renewable Oak Saplings and Dirt.

## Condenser module rules

Normal Condenser mode supports the existing live module states for Mixing and Freezing. Main, Mixing and Freezing inputs use persistent filters and hidden inactive slots are not exposed to automation.

Garden Chamber is different: it is a **Mark-II-only exclusive mode** and takes priority over Mixing and Freezing whenever directly adjacent. The Garden interface replaces the normal process cards with the specimen/harvest layout and green landscape. Its specimen slot is limited to one supported item and is not consumed; the two harvest outputs are independently automation-accessible.

## Meta-progression boundary

- **20 unique challenges / 58 SP** are active.
- Permanent progression currently reaches **Tier 0 — Foundations** and **Tier I — Conqueror**.
- Five repeatable contracts remain active after Tier I.
- Iron Jetpacks research remains 5 / 10 / 20 / 40 SP, with Creative also requiring 1 PP.
- Player-profile persistence remains dynamic schema 4/5/6 and protocol `skills-8`.
- The active real current-world package remains the Tier-I **Advanced AE2 Starter Package** for 12 SP.
- Tier-0 world upgrades remain No Phantoms, No More Rain and Permanent Day.

## Explicitly not active / not finalized

Do not infer implementation from planning documents for any of the following:

- Tier II-IV;
- final Challenge/SP balancing;
- final repeatable-contract quantities/rewards beyond the currently integrated values;
- additional valuable current-world shop packages;
- the deferred Industrial Contract;
- deferred shared-machine/multiblock objectives whose personal attribution is unresolved;
- a finalized Lava progression route;
- acquisition routes for non-Oak tree species and corresponding Garden recipes;
- final late-game pack progression or final K-menu/meta-progression balance;
- fabricated Playtime or Island Level values in the K-menu header.

The first-Iron route is **no longer open**: Washing Basin -> Raw Iron is now the implemented deterministic path.

## Pack state

The fixed starting island remains `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` with spawn `[8, 4, 5]` facing east and the verified eight-bread/one-seed 1k cell. PR #15 does not modify the starter-island NBT.

The supplied October 18 mod-list snapshot is documented separately. `pack/mods.inventory.json` and the mod-list documents remain development/runtime evidence rather than a final launcher/export lockfile.

## Validation boundary

Final feature head `aec270d7bc0caea5952af034ed8505ce0fd3d68f` passed GitHub Actions run **#259** before merge. The workflow completed the Java 21 build/unit tests, headless NeoForge client mixin smoke test, dedicated regression GameTests and full NeoForge 21.1.259 pack-runtime integration suite.

TeKatz also tested the 0.19.x/0.20.x progression in-game. Reported client issues included the initial Condenser Mark II exterior, Washing/JEI spacing, Freezing top seam, Garden JEI layout and Garden presentation; the branch was iterated through 0.20.1 before merge. Headless CI still does not prove final rendered pixels beyond those user-reported client checks.

See [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md) for the early material/automation details, [FREEZING_CHAMBER.md](FREEZING_CHAMBER.md) for Freezing behavior, [EARLY_GAME_QOL.md](EARLY_GAME_QOL.md) for world QoL, and [VALIDATION.md](VALIDATION.md) for historical validation evidence.
