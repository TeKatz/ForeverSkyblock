# Forever Skyblock mod — 0.21.0 machine animation update

Minecraft 1.21.1 / NeoForge 21.1.x. Java 21. Maintained by TeKatz. Java package and Gradle group: `de.tekatz.foreverskyblock`.

The integrated `main` baseline remains 0.20.1 after PR #15. This development branch, `feature/machine-animation-overhaul`, builds 0.21.0 with the animation and Garden Chamber presentation changes described in [MACHINE_ANIMATIONS.md](../docs/MACHINE_ANIMATIONS.md). No merge or release is implied.

## Early material progression

The Condenser Mark I, Mixing Chamber and Freezing Chamber remain the primitive foundation. The 0.19.x-0.20.1 stack adds the deterministic route from Water/Farming to Iron, then uses Iron to unlock a higher automation tier and close the wood loop.

### Mud

Condenser + Mixing Chamber:

- **4 Dirt + 1 Ice -> 4 Mud**;
- 160 ticks / 8 seconds;
- no power.

The intended continuation uses Vanilla Wheat + Mud -> Packed Mud -> Mud Bricks.

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

Only Oak is advertised at this stage because the current progression guarantees Oak but does not yet provide first-acquisition paths for the other tree species.

A single Bone Meal use on a normal placed Vanilla Sapling now attempts full tree growth immediately, removing the random early-game wait while keeping normal space/tree-generation constraints.

## Garden Chamber

The player-facing block is named **Garden Chamber**. Its internal registry ID remains `forever_skyblock:garden_chamber_mark_i` for compatibility with the tested development worlds created before the visible rename.

Crafting uses:

- 4 Mud Bricks;
- 2 Iron Ingots;
- 2 Planks;
- 1 Oak Sapling.

The block is a Mark-II-only exclusive upgrade. While directly adjacent to Condenser Mark II, Garden mode overrides normal Condensing, Mixing and Freezing, but preserves all hidden items, filters and partial progress for resume after detaching the chamber.

Initial Garden recipe:

- retained specimen: **1 Oak Sapling**;
- 320 ticks / **16 seconds**;
- **4 Oak Logs + 8 Oak Leaves**;
- the specimen is never consumed.

Garden mode exposes a one-item specimen slot and two automation-accessible harvest outputs. The Garden Chamber itself remains a passive adjacent upgrade with no standalone inventory or GUI.

## JEI, GUI and presentation

JEI includes dedicated Washing and Garden categories, catalysts, duration/output information and Mark-II requirements. The Garden category is intentionally compact and static; the decorative tree-growth animation is confined to the live Condenser GUI.

Garden mode replaces the normal Condenser process cards with a larger green landscape and staged tree-growth presentation. The Garden Chamber block is modeled as a small greenhouse with an animated interior while the connected Condenser is actively growing. The Condenser exterior also switches to a Garden-specific green/scanner animation while that mode is active.

The 0.19.1 cleanup remains included: Washing Basin spacing, Condensing/Washing JEI spacing, detailed Condenser Mark II exterior and the Freezing Chamber top-rim seam fix.

## Automation boundary

The early primitive chain is now automatable after the player reaches the Iron / Mark-II tier:

- Garden Chamber -> Oak Logs + Oak Leaves;
- Logs -> Stone Fragments;
- Mark II -> Stone Meal;
- Leaves -> Bone Meal;
- Mark II + Mixing -> Oak Saplings;
- Stone Meal + Sapling -> Dirt.

This intentionally removes permanent manual tree chopping from the mature early-game loop.

## Compatibility and fixed pack state

No progression-profile schema or `skills-8` protocol change is required. The fixed starter-island NBT is unchanged.

Default development baseline: NeoForge 21.1.249. Pack-runtime integration target: NeoForge 21.1.259.

Final pre-merge head `aec270d7bc0caea5952af034ed8505ce0fd3d68f` passed GitHub Actions run #259.

See [../docs/MATERIAL_PROCESSING.md](../docs/MATERIAL_PROCESSING.md) for the full behavior and validation boundary.

## Build

```sh
./gradlew build
```

Output: `build/libs/ForeverSkyblock-1.21.1-NeoForge-0.21.0.jar`.
