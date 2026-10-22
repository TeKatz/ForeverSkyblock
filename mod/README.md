# Forever Skyblock mod — 0.26.4 integrated baseline

Minecraft 1.21.1 / NeoForge 21.1.x. Java 21. Maintained by TeKatz.

The current `main` baseline is **0.26.4**. On October 22, 2026, PR #29 was merged into the resource-refining branch and PR #28 was then merged into `main`. The mod now contains the renewable Nether/Redstone bridge and the FE-powered Refining Condenser in addition to the existing Condenser, Washing, Garden, Crucible and K-menu systems.

Authoritative summaries:

- [Current integrated state](../docs/CURRENT_STATE.md)
- [Current progression](../docs/PROGRESSION.md)
- [Resource refining](../docs/RESOURCE_REFINING.md)

The current installed pack runtime is **NeoForge 21.1.262**. The final pre-integration hotfix source was validated by Actions run #563 with the full automated pack-runtime suite on **NeoForge 21.1.259**. These are separate evidence boundaries.

## Early material progression

The primitive chain remains:

`Wood -> Stone Fragments -> Stone Meal / Dirt -> Ice / Water -> Farm -> Mud / Packed Mud -> Washing Basin -> Raw Iron -> Iron -> Condenser Mark II + Garden`

Key numbers:

- **1 Log -> 4 Stone Fragments / 120 ticks (6 s)**;
- **4 Stone Meal -> 1 Raw Iron / 160 ticks (8 s)** with adjacent source water;
- Mark II automates Stone Meal and Oak Sapling production;
- Garden Oak: retained Sapling -> **4 Logs + 8 Leaves / 320 ticks (16 s)**;
- Garden Wheat: retained Wheat Seeds -> **4 Wheat + 2 Seeds / 320 ticks (16 s)**.

## Condenser Mark II automation

Mark II has **3 upgrade capacity** and three physical upgrade slots. Current cards are Processing Speed, Garden Accelerator, Buffer Expansion and Batch Processing.

Outputs support independent **Block / Void Excess / Void All** policies. Buffer Expansion synchronizes the displayed output amount with visible + hidden buffered storage.

The shared machine-container GUI now uses only native `AbstractContainerScreen` coordinates; the earlier custom fit-to-window scale/mouse remapping layer has been removed to prevent item/slot drift after resizing or changing GUI scale.

## Crucible

The Crucible has a **4,000 mB** internal Lava buffer.

Current Lava production:

- **8 Cobblestone -> 1,000 mB Lava**;
- **40 Stone Fragments -> 100 mB Lava**.

Heat tiers: **Torch -> Campfire -> Lava -> Mekanism Superheating Element**.

An adjacent Fluid Tank enables Lava routing. An adjacent Freezing Chamber enables **1,000 mB Lava -> 1 Obsidian** through timed freezing.

New renewable-Netherrack route:

- **1 Gravel + 250 mB stored Lava -> 1 Netherrack**;
- **80 ticks / 4 seconds**.

Produced Crucible output items can auto-push into adjacent item-handler inventories. JEI shows real fluid quantities rather than Lava Bucket placeholders.

## Granular and Nether processing

Both Condenser tiers:

- **1 Cobblestone -> 1 Gravel / 120 ticks**;
- **1 Gravel -> 1 Sand / 120 ticks**.

Condenser Mark II:

- Nether Bricks / Nether Brick Stairs / Nether Brick Fences -> **1 Nether Brick Dust / 160 ticks**.

Washing Basin:

- **1 Nether Brick Dust -> 1 Nether Concentrate + 2 Quartz + 20% Gold Nugget / 160 ticks**.

Mark II + Mixing:

- **1 Nether Concentrate + 1 Stone Meal -> 2 Mineral Blend / 160 ticks**.

Washing Basin:

- **1 Mineral Blend -> 8 Redstone / 120 ticks**.

## Refining Condenser

Player-facing name: **Refining Condenser**. Stable internal registry ID: `forever_skyblock:refining_chamber`.

Crafting:

- 4 Iron Blocks;
- 2 Redstone;
- 2 Obsidian;
- 1 Blast Furnace.

Machine properties:

- primary input, catalyst and output;
- standard item capability automation;
- output extraction from below;
- **100,000 FE** storage;
- maximum **4,000 FE/t** receive;
- no energy export.

Recipes:

| Result | Inputs | Duration | FE/t |
| --- | --- | ---: | ---: |
| 6 Lapis Lazuli | 2 Mineral Blend + 1 Redstone | 160 ticks | 60 |
| 4 Copper Ingots | 2 Mineral Blend + 1 Quartz | 200 ticks | 80 |
| 2 Osmium Ingots | 3 Mineral Blend + 1 Charcoal | 240 ticks | 100 |
| 1 Emerald | 4 Mineral Blend + 1 Gold Ingot | 300 ticks | 120 |
| 1 Diamond | 2 Carbon Matrix + 8 Mineral Blend | 900 ticks | 240 |

Osmium is conditional on Mekanism. Carbon Matrix is **8 Charcoal + 1 Mineral Blend -> 1 Carbon Matrix**.

The Diamond recipe consumes **216,000 FE** over one full operation, so continuous external power is required despite the 100,000-FE internal buffer.

JEI includes a dedicated Refining category/catalyst and Recipes button. The Refining Condenser and Mineral Blend use dedicated item textures.

## Progression-aligned external recipes

- Mekanism Basic Logistical Transporter: **2 Iron Ingots + 1 Redstone -> 8**.
- Iron Furnaces Iron Furnace: **4 Iron Blocks + 4 Iron Ingots + 1 Furnace -> 1**.
- Mekanism Basic Fluid Tank remains redstone-free to avoid a bootstrap loop before renewable Redstone.

## Compatibility

The resource-refining integration does not change the player profile schema, world-progress schema or `skills-9` progression protocol. The fixed starter-island NBT is unchanged.

The internal Refining Condenser ID intentionally remains `refining_chamber` for compatibility with existing development/test worlds.

## Build

```sh
./gradlew build
```

Current integrated output version: `build/libs/ForeverSkyblock-1.21.1-NeoForge-0.26.4.jar`.

## Validation

Final pre-integration source `38a730e77c066ada5ade7395568baa04c1c635ff` passed Actions run **#563**:

- Java build + unit tests;
- client smoke test;
- visual fixtures;
- dedicated-server GameTests;
- full pack-runtime integration on NeoForge **21.1.259**.

See [../docs/RESOURCE_REFINING.md](../docs/RESOURCE_REFINING.md) and [../docs/VALIDATION.md](../docs/VALIDATION.md) for documentation and validation boundaries.
