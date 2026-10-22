# Nether progression

Updated October 22, 2026. The Nether stage now has two complementary roles: Fortress exploration provides the initial/bootstrap materials, while the integrated Crucible/Condenser/Washing chain provides a renewable long-term route into Nether Concentrate, Quartz, Gold by-product and the later Mineral Blend/Refining stage.

## Fortress bootstrap

`minecraft:chests/nether_bridge` is overridden by Forever Skyblock. Diamonds, Gold Ingots, Golden Chestplates, Saddles and all Horse Armor variants are removed. Nether Quartz, Coal and Sugar Cane replace their combined loot weight. The remaining vanilla entries and Rib Armor Trim roll stay available.

The Fortress is therefore useful for first access without simply handing out the later refined resources directly.

## Renewable Netherrack

Once the player has the Crucible and stored Lava, the Nether material chain can be reproduced without permanently mining Fortress blocks.

Supporting Condenser recipes on both tiers:

- **1 Cobblestone -> 1 Gravel**;
- **120 ticks / 6 seconds**;
- **1 Gravel -> 1 Sand**;
- **120 ticks / 6 seconds**.

Crucible conversion:

- **1 Gravel + 250 mB stored Lava -> 1 Netherrack**;
- **80 ticks / 4 seconds**.

The Crucible can push its produced output item into adjacent compatible inventories. This Netherrack conversion consumes stored Lava and is distinct from the normal heat-driven Lava-production input route.

For reference, current Crucible Lava yields are:

- **8 Cobblestone -> 1,000 mB Lava**;
- **40 Stone Fragments -> 100 mB Lava**.

## Nether Brick Dust — Condenser Mark II

Netherrack can continue through the normal Vanilla smelting/crafting path into Nether Brick materials. Condenser Mark II accepts the shared `forever_skyblock:nether_brick_materials` tag:

- Nether Bricks;
- Nether Brick Stairs;
- Nether Brick Fences.

Processing:

- **1 Nether brick material -> 1 Nether Brick Dust**;
- **160 ticks / 8 seconds**;
- **Mark II only**.

The recipe is a normal `forever_skyblock:condensing` datapack recipe and is exposed through the existing Condensing JEI integration.

## Washing Basin — Nether Concentrate

Nether Brick Dust can be washed with an adjacent source-water block:

- **1 Nether Brick Dust -> 1 Nether Concentrate**;
- **+2 Nether Quartz guaranteed**;
- **+20% chance for 1 Gold Nugget**;
- **160 ticks / 8 seconds**.

The source-water requirement is infrastructure and the water is not consumed.

The Washing Basin uses its primary and secondary output paths for the guaranteed results. Chance-based bonus output is retained in the persistent bonus queue until it can be emitted, so a successful Gold Nugget roll is not silently lost to a temporarily blocked slot.

JEI displays primary, secondary and chance output separately and labels the bonus probability.

## Nether Concentrate into Mineral Blend

Nether Concentrate is not an endpoint. Condenser Mark II with a Mixing Chamber processes:

- **1 Nether Concentrate + 1 Stone Meal -> 2 Mineral Blend**;
- **160 ticks / 8 seconds**;
- **Mark II only**.

Mineral Blend then has two major roles:

1. Washing Basin: **1 Mineral Blend -> 8 Redstone / 120 ticks (6 seconds)**.
2. Refining Condenser: primary material/catalyst chain for Lapis, Copper, Osmium, Emerald and Diamond progression.

See [RESOURCE_REFINING.md](RESOURCE_REFINING.md) for the full refining table and FE requirements.

## Current loop summary

`Cobblestone -> Gravel -> Crucible + Lava -> Netherrack -> Nether Brick path -> Nether Brick Dust -> Washing -> Nether Concentrate + Quartz + Gold chance -> Mineral Blend -> Redstone / Refining`

The Fortress remains meaningful for exploration and bootstrap loot, but the mature production chain is renewable and automation-friendly.
