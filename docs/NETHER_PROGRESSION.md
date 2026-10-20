# Nether Fortress processing

The first Nether progression step deliberately treats the Fortress itself as the raw-material source instead of opening the full vanilla Nether resource tree at once.

## Fortress chest loot

`minecraft:chests/nether_bridge` is overridden by Forever Skyblock. Diamonds, Gold Ingots, Golden Chestplates, Saddles and all Horse Armor variants are removed. Nether Quartz, Coal and Sugar Cane replace their combined loot weight. The remaining vanilla entries and Rib Armor Trim roll stay available.

## Condenser Mark II

Nether Bricks, Nether Brick Stairs and Nether Brick Fences can be crushed in Condenser Mark II:

- 1 Nether brick material -> 1 Nether Brick Dust
- 8 seconds
- Mark II only

The recipe is a normal `forever_skyblock:condensing` datapack recipe and therefore appears in JEI automatically.

## Washing Basin

Nether Brick Dust can be washed with an adjacent source-water block:

- 1 Nether Brick Dust -> 1 Nether Concentrate
- + 2 Nether Quartz guaranteed
- + 20% chance for 1 Gold Nugget
- 8 seconds

The Washing Basin exposes two visible output slots. Primary output uses the first slot. Guaranteed secondary output uses the second slot. Chance-based bonus items are stored in a persistent internal queue and are released through the second output channel as soon as it is free, so automation cannot lose a successful bonus roll.

The Washing JEI category displays primary, secondary and chance output separately and labels the bonus probability.
