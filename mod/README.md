# Forever Skyblock mod — 0.26.4 integrated baseline

Minecraft 1.21.1 / NeoForge 21.1.x. Java 21. Maintained by TeKatz.

The integrated `main` baseline is **0.26.4**. It includes the 0.21–0.24 machine animation/balancing/K-menu stack, the integrated K-menu Studio, Crucible Lava/Obsidian progression, Condenser Mark II upgrade/overflow automation and the current Nether Fortress material-processing work present on `main`. See [CURRENT_STATE.md](../docs/CURRENT_STATE.md) for the authoritative integrated-state summary.

The current installed pack runtime is **NeoForge 21.1.262**. TeKatz reported testing the October 21, 2026 pack state with the current supplied 87-artifact mod list on that loader and finding **no errors observed**. The standalone mod project's default development NeoForge dependency remains **21.1.249** unless changed deliberately. Historical automated validation entries below that name NeoForge 21.1.259 remain tied to those earlier runs.

The current supplied pack list uses JEI **19.69.0.457**, Mekanism **10.7.19.85** and Mekanism Generators **10.7.19.85**. Exact pack artifact versions are tracked in [MODLIST_SNAPSHOT.md](../docs/MODLIST_SNAPSHOT.md) and [MODS.md](../docs/MODS.md).

## Early material progression

The Condenser Mark I, Mixing Chamber and Freezing Chamber remain the primitive foundation. The 0.19.x-0.20.1 stack introduced the deterministic route from Water/Farming to Iron. Iron unlocks the higher automation tier that closes the wood loop.

### Mud

Condenser + Mixing Chamber:

- **4 Dirt + 1 Ice -> 4 Mud**;
- 160 ticks / 8 seconds;
- no power.

Both Condenser tiers with Mixing Chamber automate **1 Wheat + 1 Mud -> 1 Packed Mud / 8 seconds**. Vanilla crafting remains available; Packed Mud -> Mud Bricks continues the route.

### Washing Basin

The Washing Basin is crafted from 4 Mud Bricks, 2 Cobblestone and 2 Planks. It has one input/output pair and standard item-handler automation.

It only processes while a directly adjacent block on one of its six faces is a **source-water block**. The water is not consumed. Removing water pauses the operation without losing input or partial progress.

Initial washing recipe:

- **4 Stone Meal -> 1 Raw Iron**;
- 160 ticks / 8 seconds;
- no power.

Raw Iron can then be smelted normally.

## Condenser Mark I crafting

The starter Condenser becomes reproducible before Iron after the player has established Water/Farming/Packed Mud:

- 4 Mud Bricks;
- 2 Cobblestone;
- 2 Stone Meal;
- 1 Furnace.

## Condenser Mark II

Mark II is the first Iron-gated automation tier. Crafting uses:

- 1 Condenser Mark I;
- 4 Iron Ingots;
- 4 Mud Bricks.

It retains Mark-I recipes, locks, automation and Mixing/Freezing compatibility, and adds:

- **4 Stone Fragments -> 1 Stone Meal**;
- **4 Wheat Seeds + 1 Bone Meal -> 2 Oak Saplings** with a Mixing Chamber.

Oak is currently the only supported tree because the opening progression does not yet provide first-acquisition paths for other tree species. Wheat is also available as a Garden crop.

A single Bone Meal use on a normal placed Vanilla Sapling attempts full tree growth immediately, removing the random early-game wait while keeping normal space/tree-generation constraints.

### Mark II upgrade system

Mark II has **3 upgrade capacity** and three physical upgrade slots. Current cards are:

- Processing Speed Card;
- Garden Accelerator Card;
- Buffer Expansion Card;
- Batch Processing Card.

Each output has an independent **Block / Void Excess / Void All** policy. Garden primary and secondary outputs can be configured separately. See [CURRENT_STATE.md](../docs/CURRENT_STATE.md) for the authoritative integrated behavior.

## Garden Chamber

The player-facing block is named **Garden Chamber**. Its internal registry ID remains `forever_skyblock:garden_chamber_mark_i` for compatibility with development worlds created before the visible rename.

Crafting uses:

- 4 Mud Bricks;
- 2 Iron Ingots;
- 2 Planks;
- 1 Oak Sapling.

The block is a Mark-II-only exclusive upgrade. While directly adjacent to Condenser Mark II, Garden mode overrides normal Condensing, Mixing and Freezing, but preserves all hidden items, filters and partial progress for resume after detaching the chamber.

Garden recipes:

- retained specimen: **1 Oak Sapling**;
- 320 ticks / **16 seconds**;
- **4 Oak Logs + 8 Oak Leaves**;
- the specimen is never consumed.

Wheat uses a retained **1 Wheat Seeds** specimen and produces **4 Wheat + 2 Wheat Seeds** every **320 ticks / 16 seconds**. Wheat receives synchronized green-to-gold crop animations in the GUI, controller and greenhouse. Both harvest outputs must have room. The existing Oak GUI tree is preserved.

With Wooden Hopper installed, its recipe uses **2 Iron Ingots** in the top corners, **3 Planks** in the remaining positions and **1 wooden Chest**. Before the first real-player Emerald pickup, the entire Vanilla Trader caravan attempt and fresh untamed Trader llamas are blocked. Saved/tamed Trader llamas and ordinary llamas remain intact.

Garden mode exposes a one-item specimen slot and two automation-accessible harvest outputs. A hopper below Condenser Mark II extracts only the primary harvest; a hopper below the Garden Chamber extracts only the secondary harvest. Place the blocks side by side for two accessible bottom ports. The Chamber delegates to the controller's existing secondary slot and has no standalone inventory or GUI. Both outputs remain manually accessible in the Condenser GUI, with block icons, numbered downward arrows, a persistent routing legend and localized tooltips.

## Crucible

The integrated Crucible owns a **4,000 mB** internal Lava buffer and accepts either **8 Cobblestone** or **40 Stone Fragments** for **1,000 mB Lava**. Heat progression is Torch → Campfire → Lava → Mekanism Superheating Element.

An adjacent Fluid Tank enables Lava routing. An adjacent Freezing Chamber enables Obsidian production. If both are present, the GUI exposes route selection. The Basic Fluid Tank recipe is intentionally redstone-free so Redstone can enter progression later as a separate step.

## JEI, GUI and presentation

JEI includes dedicated Washing, Garden and Crucible processing presentation as appropriate to the integrated systems. Garden mode replaces the normal Condenser process cards with a larger green landscape and staged growth presentation. The Garden Chamber block is modeled as a small greenhouse with an animated interior while the connected Condenser is actively growing. The Condenser exterior also switches to a Garden-specific green/scanner animation while that mode is active.

The 0.19.1 cleanup remains included: Washing Basin spacing, Condensing/Washing JEI spacing, detailed Condenser Mark II exterior and the Freezing Chamber top-rim seam fix.

## Automation boundary

The early primitive chain is automatable after the player reaches the Iron / Mark-II tier:

- Garden Chamber -> Oak Logs + Oak Leaves;
- Logs -> Stone Fragments;
- Mark II -> Stone Meal;
- Leaves -> Bone Meal;
- Mark II + Mixing -> Oak Saplings;
- Stone Meal + Sapling -> Dirt.

This intentionally removes permanent manual tree chopping from the mature early-game loop.

## Compatibility and fixed pack state

The machine changes required no profile/protocol change. The 0.24.0 K-menu statistics use `skills-9` while preserving player profile schema 4/5/6. The fixed starter-island NBT is unchanged.

Default development baseline: NeoForge **21.1.249**. Current installed pack runtime: NeoForge **21.1.262**.

Historical automated runs used the then-current pack runtime NeoForge 21.1.259 and remain valid run-specific evidence. The October 21 pack state was separately user-tested by TeKatz on 21.1.262 with no errors observed.

See [../docs/MATERIAL_PROCESSING.md](../docs/MATERIAL_PROCESSING.md) for the full behavior and validation boundary.

## Build

```sh
./gradlew build
```

Current integrated output version: `build/libs/ForeverSkyblock-1.21.1-NeoForge-0.26.4.jar`.

## Historical validation notes

The complete 0.21–0.24 stack was validated with Java builds, client startup/model checks, native K-menu visual/input fixtures and dedicated-server GameTests. Historical pack-loader suites used NeoForge 21.1.259. Exact source/artifact evidence remains recorded in the detailed feature documents and [VALIDATION.md](../docs/VALIDATION.md).

The 0.23.0 balancing work established:

- Both Condenser tiers: **1 Log -> 4 Stone Fragments / 120 ticks (6 seconds)**.
- Both tiers with Mixing Chamber: **1 Mud + 1 Wheat -> 1 Packed Mud / 160 ticks (8 seconds)**.
- Normal Hopper: **2 Iron Blocks + 3 Iron Ingots + 1 Chest**.
- Iron Pickaxe/Axe/Shovel/Hoe: **one Iron Block** plus **4/3/2/2 Ingots**, respectively, the matching Stone tool and one Chain.
- Garden output badges and tooltip hit areas centered on the actual output slot centres.

One Garden produces 15 Logs/minute while one six-second Condenser consumes 10/minute, before pauses. This requested trial reduces backlog growth but does not make that single-processor line balanced.

Existing Recipe Editor installations should keep the repository's matching generated-pack overrides synchronized with the installed Recipe Editor version and the current JEI baseline.
