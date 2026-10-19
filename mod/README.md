# Forever Skyblock mod — 0.25.1 K-menu Studio polish development

Minecraft 1.21.1 / NeoForge 21.1.x. Java 21. Maintained by TeKatz.

`feature/kmenu-editor-polish` builds 0.25.1 on top of the unmerged 0.25.0 K-menu Studio branch and the integrated 0.24.1 header-art baseline. It keeps the native admin layout/quest/node editor, server-owned publication and portable/repository export, while polishing navigation, help, inspector grouping and preview behavior after the first real-player test. Matching client/server builds are required for the `kmenu-editor-1` channel. See [K_MENU_EDITOR.md](../docs/K_MENU_EDITOR.md). Main integration and publication are separate steps. Java package and Gradle group: `de.tekatz.foreverskyblock`.

The integrated `main` baseline is **0.24.1** through [PR #17](https://github.com/TeKatz/ForeverSkyblock/pull/17), on top of [PR #16](https://github.com/TeKatz/ForeverSkyblock/pull/16), including all 0.21–0.23 animation, crop, harvest-routing and balancing updates. The K-menu has round Shop nodes, improved Challenge/Contract browsing and details, a compact profile with actual skin face, custom status icons and subtle motion. Server-supplied playtime/deaths/kills and shared world-milestone Island Level use protocol **skills-9**; matching client/server versions are required. Player profile schema remains 4/5/6; the backed-up world-progress file migrates to schema 2. See [K_MENU_DESIGN2.md](../docs/K_MENU_DESIGN2.md). No release is published by this integration.

The final 0.24.0 source passed 78 unit tests, native-client render/input checks and both 90-test server suites, including NeoForge 21.1.259. The fresh JAR was checked against all 158 source resources and excludes the visual fixture/GameTests. Exact source, run and artifact hash are recorded in [K_MENU_DESIGN2.md](../docs/K_MENU_DESIGN2.md#validation-and-acceptance).

## Early material progression

The Condenser Mark I, Mixing Chamber and Freezing Chamber remain the primitive foundation. The 0.19.x-0.20.1 stack introduced the deterministic route from Water/Farming to Iron. Iron unlocks the higher automation tier that closes the wood loop.

### Mud

Condenser + Mixing Chamber:

- **4 Dirt + 1 Ice -> 4 Mud**;
- 160 ticks / 8 seconds;
- no power.

Both Condenser tiers with Mixing Chamber now automate **1 Wheat + 1 Mud -> 1 Packed Mud / 8 seconds**. Vanilla crafting remains available; Packed Mud -> Mud Bricks continues the route.

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

A single Bone Meal use on a normal placed Vanilla Sapling now attempts full tree growth immediately, removing the random early-game wait while keeping normal space/tree-generation constraints.

## Garden Chamber

The player-facing block is named **Garden Chamber**. Its internal registry ID remains `forever_skyblock:garden_chamber_mark_i` for compatibility with the tested development worlds created before the visible rename.

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

With Wooden Hopper installed, its recipe now uses **2 Iron Ingots** in the top corners, **3 Planks** in the remaining positions and **1 wooden Chest**. The cheaper original is removed at recipe reload; without that optional mod the replacement is not loaded. Before the first real-player Emerald pickup, the entire Vanilla Trader caravan attempt and fresh untamed Trader llamas are blocked. Saved/tamed Trader llamas and ordinary llamas remain intact.

Garden mode exposes a one-item specimen slot and two automation-accessible harvest outputs. A hopper below Condenser Mark II extracts only the primary harvest; a hopper below the Garden Chamber extracts only the secondary harvest. Place the blocks side by side for two accessible bottom ports. The Chamber delegates to the controller's existing secondary slot and has no standalone inventory or GUI. Both outputs remain manually accessible in the Condenser GUI, with block icons, numbered downward arrows, a persistent routing legend and localized tooltips.

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

The machine changes required no profile/protocol change. The subsequent 0.24.0 K-menu statistics use `skills-9` while preserving player profile schema 4/5/6. The fixed starter-island NBT is unchanged.

Default development baseline: NeoForge 21.1.249. Pack-runtime integration target: NeoForge 21.1.259.

Historical 0.20.1 pre-merge head `aec270d7bc0caea5952af034ed8505ce0fd3d68f` passed GitHub Actions run #259. Historical 0.24.0 validation is recorded above and in [K_MENU_DESIGN2.md](../docs/K_MENU_DESIGN2.md#validation-and-acceptance).

See [../docs/MATERIAL_PROCESSING.md](../docs/MATERIAL_PROCESSING.md) for the full behavior and validation boundary.

## Build

```sh
./gradlew build
```

Output on this branch: `build/libs/ForeverSkyblock-1.21.1-NeoForge-0.25.1.jar`.

The validated 0.25.1 editor-polish code/metadata source `56246f2a57672d0fdf966cf865b596eeeaee0019` passed Actions run `39204247635`, including Java build, client smoke, native K-menu visual fixtures and both server regression runtimes. The production JAR SHA-256 is `e530f8ae16c1131893073a7ba529b87521528005a78559a9a45bf9dbfdcb8b6c`; real-player usability acceptance is still separate.

The 0.22.0 source passed 73 unit tests, client startup/model checks and both 81-test GameTest suites, including the real Wooden Hopper on NeoForge 21.1.259. Exact source/artifact evidence and the client visual test boundary are recorded in [MACHINE_ANIMATIONS.md](../docs/MACHINE_ANIMATIONS.md#0220-validation-and-test-artifact).

The 0.22.1 source passed 73 unit tests, client startup/model checks and both 85-test server suites. Real Vanilla hoppers separated Oak harvest; the actual pack Wooden Hoppers separated Wheat harvest. Exact artifact evidence and remaining client checks are in [MACHINE_ANIMATIONS.md](../docs/MACHINE_ANIMATIONS.md#0221-validation-and-test-artifact).

## Integrated 0.23.0 balancing

- Both Condenser tiers: **1 Log -> 4 Stone Fragments / 120 ticks (6 seconds)**; Planks, Leaves and other existing cycles retain their timing.
- Both tiers with Mixing Chamber: **1 Mud + 1 Wheat -> 1 Packed Mud / 160 ticks (8 seconds)**, swappable inputs, no power and the existing Condensing JEI category. The Vanilla crafting alternative is retained.
- Normal Hopper: **2 Iron Blocks + 3 Iron Ingots + 1 Chest**; the old Vanilla/NeoForge recipe and competing static-output recipes are removed. Wooden Hopper pricing is unchanged.
- Iron Pickaxe/Axe/Shovel/Hoe: **one Iron Block** plus **4/3/2/2 Ingots**, respectively, the matching Stone tool and one Chain. Prior tool/durability behavior is unchanged. Matching Recipe Editor snapshots are included in the repository.
- Garden output badges and tooltip hit areas are centered on the actual output slot centres.

One Garden still produces 15 Logs/minute while one six-second Condenser consumes 10/minute, before pauses. This requested trial reduces backlog growth but does not make that single-processor line balanced.

Existing Recipe Editor installations should also apply `ForeverSkyblock-0.23.0-RecipeEditor-Update.zip` at the instance root. Its nine data files refresh the matching overrides under `config/forever_recipe_editor/generated_pack/data/`; an old local Iron-tool override would otherwise keep the old cost and fail the new challenge ingredient proof.

The 0.23.0 source passed 74 unit tests, client startup/model checks and both 89-test server suites. The strict Iron Tools challenge proof was updated with the recipe costs and its existing rewards remain covered. Exact source/artifact evidence is in [MATERIAL_PROCESSING.md](../docs/MATERIAL_PROCESSING.md#0230-validation-and-artifacts).
