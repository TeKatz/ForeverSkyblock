# Resource refining progression

Updated October 22, 2026 for the integration of PR #29 into PR #28 and PR #28 into `main`.

This document describes the live renewable-resource and Refining Condenser stage that follows the established Iron, Garden and Crucible progression. The player-facing machine name is **Refining Condenser**; its internal registry ID remains `forever_skyblock:refining_chamber` so existing development worlds keep the same block identity.

## Progression position

The current bridge into early technology is:

`Cobblestone -> Gravel -> Sand -> stored Lava -> Netherrack -> Nether Bricks -> Nether Brick Dust -> Nether Concentrate -> Mineral Blend -> Redstone -> Refining Condenser -> Lapis / Copper / Osmium / Emerald / Diamond`

This extends, rather than replaces, the earlier route documented in [PROGRESSION.md](PROGRESSION.md) and [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md).

## Renewable basic materials

Both Condenser tiers can process:

- **1 Cobblestone -> 1 Gravel** in **120 ticks / 6 seconds**;
- **1 Gravel -> 1 Sand** in **120 ticks / 6 seconds**.

The Crucible now has two distinct Lava yields:

- **8 Cobblestone -> 1,000 mB Lava**;
- **40 Stone Fragments -> 100 mB Lava**.

The smaller Stone-Fragment route is intentionally no longer equivalent to a full Cobblestone batch.

With stored Lava available, the Crucible also converts:

- **1 Gravel + 250 mB stored Lava -> 1 Netherrack**;
- **80 ticks / 4 seconds**.

Crucible output items are actively pushed into adjacent item-handler inventories when possible. The normal Lava routing and Freezing Chamber Obsidian route remain available.

## Renewable Nether material loop

Netherrack can be smelted through the normal Vanilla path into Nether Brick items and crafted into Nether Bricks. Condenser Mark II accepts Nether Bricks, Nether Brick Stairs and Nether Brick Fences through the shared `forever_skyblock:nether_brick_materials` tag:

- **1 Nether brick material -> 1 Nether Brick Dust**;
- **160 ticks / 8 seconds**;
- **Mark II only**.

Washing one Nether Brick Dust with an adjacent source-water block produces:

- **1 Nether Concentrate**;
- **2 Nether Quartz guaranteed**;
- **20% chance for 1 Gold Nugget**;
- **160 ticks / 8 seconds**.

The Nether Fortress remains an exploration/bootstrap source, but the material chain is no longer permanently dependent on fortress blocks once the renewable loop is established. See [NETHER_PROGRESSION.md](NETHER_PROGRESSION.md).

## Mineral Blend and Redstone

Condenser Mark II with its Mixing Chamber combines:

- **1 Nether Concentrate + 1 Stone Meal -> 2 Mineral Blend**;
- **160 ticks / 8 seconds**;
- **Mark II only**.

The Washing Basin converts:

- **1 Mineral Blend -> 8 Redstone**;
- **120 ticks / 6 seconds**;
- adjacent source water is still required and is not consumed.

This is the intended renewable Redstone bridge. The Mekanism Basic Fluid Tank remains deliberately redstone-free so the Crucible infrastructure can exist before this step.

## Refining Condenser

Crafting pattern:

`IRI`
`OFO`
`IRI`

where:

- `I` = Iron Block;
- `R` = Redstone;
- `O` = Obsidian;
- `F` = Blast Furnace.

Total cost: **4 Iron Blocks + 2 Redstone + 2 Obsidian + 1 Blast Furnace**.

The machine has a primary input, catalyst input and output. It exposes standard item automation, with output extraction from the bottom, and accepts FE through its energy capability.

Energy behavior:

- internal capacity: **100,000 FE**;
- maximum receive: **4,000 FE/t**;
- the machine receives but does not export energy;
- processing pauses on insufficient FE and on a blocked output without consuming the recipe inputs.

## Refining recipes

| Output | Primary input | Catalyst | Time | FE/t | Total FE |
| --- | --- | --- | ---: | ---: | ---: |
| 6 Lapis Lazuli | 2 Mineral Blend | 1 Redstone | 160 ticks / 8 s | 60 | 9,600 |
| 4 Copper Ingots | 2 Mineral Blend | 1 Quartz | 200 ticks / 10 s | 80 | 16,000 |
| 2 Osmium Ingots | 3 Mineral Blend | 1 Charcoal | 240 ticks / 12 s | 100 | 24,000 |
| 1 Emerald | 4 Mineral Blend | 1 Gold Ingot | 300 ticks / 15 s | 120 | 36,000 |
| 1 Diamond | 2 Carbon Matrix | 8 Mineral Blend | 900 ticks / 45 s | 240 | 216,000 |

The Osmium recipe is only loaded when Mekanism is present. The Diamond recipe deliberately exceeds the machine's 100,000-FE internal capacity over a full cycle, so it requires continuing external energy supply rather than a single precharged buffer.

## Carbon Matrix

Carbon Matrix is renewable from the established wood/charcoal chain:

- shaped recipe `CCC / CMC / CCC`;
- **8 Charcoal + 1 Mineral Blend -> 1 Carbon Matrix**.

Two Carbon Matrices plus eight Mineral Blend are consumed for one Diamond.

## Technology bridge recipe overrides

The integrated progression also overrides selected external recipes so they unlock after the new Redstone/Iron stage instead of bypassing it.

### Mekanism Basic Logistical Transporter

- pattern: `IRI`;
- **2 Iron Ingots + 1 Redstone -> 8 Basic Logistical Transporters**.

### Iron Furnaces Iron Furnace

- pattern: `BIB / IFI / BIB`;
- `B` = Iron Block, `I` = Iron Ingot, `F` = Furnace;
- total: **4 Iron Blocks + 4 Iron Ingots + 1 Furnace**.

These costs are intentional Iron sinks for the post-automation stage.

## JEI, GUI and presentation

The Refining Condenser has its own JEI category and catalyst, a Recipes button, redesigned recipe cards and dedicated block/item presentation. Mineral Blend and the Refining Condenser use dedicated item textures.

The Crucible JEI display uses real Lava fluid amounts instead of showing Lava Buckets, including the different 1,000 mB Cobblestone and 100 mB Stone-Fragment yields. The filled Crucible also has a dedicated heated/Lava exterior state.

Machine container screens now use the native `AbstractContainerScreen` coordinate system. The earlier custom fit-to-window `PoseStack` scale/remapping layer was removed so Vanilla slots, rendered ItemStacks, custom slot frames and pointer interaction share the same `leftPos`/`topPos` coordinate space.

## Integration and validation

- PR #29 (`hotfix/pr28-14-fixes`) was merged into PR #28's `feature/resource-refining-progression` branch.
- PR #28 was then merged into `main` on October 22, 2026.
- Final hotfix source before integration: `38a730e77c066ada5ade7395568baa04c1c635ff`.
- GitHub Actions run **#563** passed the Java build/unit tests, client smoke test, visual fixtures, dedicated-server regression suite and full pack-runtime integration on **NeoForge 21.1.259**.

The current installed pack runtime remains **NeoForge 21.1.262** based on the separate October 21 pack snapshot. The automated #563 validation used 21.1.259 and should not be rewritten as 21.1.262. The final native-coordinate GUI change is merged by explicit project authorization; no additional post-merge real-player GUI acceptance is claimed here.
