# Forever Skyblock mod — 0.19.0 material-processing development

Minecraft 1.21.1 / NeoForge 21.1.x. Java 21. Maintained by TeKatz. Java package and Gradle group: `de.tekatz.foreverskyblock`.

Branch: `feature/material-processing-0.19`, based on the integrated 0.18.1 `main` baseline. It is not merged and no release is implied.

## Early material progression

The existing Condenser Mark I, Mixing Chamber and Freezing Chamber remain the primitive foundation. Version 0.19.0 adds the first deterministic route from Water/Farming to Iron and then uses Iron to unlock a higher automation tier.

### Mud

Condenser + Mixing Chamber:

- **4 Dirt + 1 Ice -> 4 Mud**;
- 160 ticks / 8 seconds;
- no power.

The intended continuation uses Vanilla Wheat + Mud -> Packed Mud -> Mud Bricks.

### Washing Basin

The new Washing Basin is crafted from 4 Mud Bricks, 2 Cobblestone and 2 Planks. It has one input/output pair and standard item-handler automation.

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

It shares the Mark I inventory/lock architecture and supports the same adjacent Mixing and Freezing Chambers. It retains all Mark-I recipes and additionally provides:

- **4 Stone Fragments -> 1 Stone Meal**;
- **8 matching Leaves + 1 Bone Meal -> 1 matching Sapling/Propagule** with a Mixing Chamber.

Oak, Spruce, Birch, Jungle, Acacia, Dark Oak, Cherry and Mangrove are covered. Mark-II-only recipes use a recipe-tier field and cannot run in Mark I. When both Leaves -> Bone Meal and a matching two-input Sapling recipe are possible, the explicit two-input recipe has deterministic priority.

## JEI and GUI

JEI includes a Washing category, Washing Basin catalyst, water-source requirement, non-consumed-water note, duration/output information and a Recipes button in the Basin GUI. Condenser Mark II is registered for Condensing/Freezing and Mark-II-only recipes identify their tier requirement. Condenser recipe transfer refuses Mark-II-only recipes when the open machine is Mark I.

## Automation boundary

0.19.0 removes the permanent manual dependency on the Mortar for Stone Meal and on random tree drops for Saplings once Mark II is reached. The Washing Basin is also automation-capable.

Automatic **Log/Leaf production remains intentionally unresolved in this update and is the next automation obligation**: the pack's primitive resource chain is based on wood, so a later tree-growing/harvesting system must close that loop rather than leaving permanent manual tree farming.

## Compatibility and fixed pack state

No progression-profile schema or `skills-8` protocol change is required. The fixed starter-island NBT is unchanged.

Default development baseline: NeoForge 21.1.249. Pack-runtime integration target: NeoForge 21.1.259.

See [../docs/MATERIAL_PROCESSING.md](../docs/MATERIAL_PROCESSING.md) for the full 0.19.0 behavior and validation boundary.

## Build

```sh
./gradlew build
```

Output: `build/libs/ForeverSkyblock-1.21.1-NeoForge-0.19.0.jar`.
