# Current progression

Updated October 22, 2026. This is the live sequential gameplay progression for the current **0.26.4** `main` baseline after PR #29 was folded into PR #28 and PR #28 was merged into `main`.

For exact integrated repository status use [CURRENT_STATE.md](CURRENT_STATE.md). For detailed machine/resource numbers introduced by the latest integration use [RESOURCE_REFINING.md](RESOURCE_REFINING.md).

## Current route at a glance

`Oak / Wood`
`→ Stone Fragments`
`→ Cobblestone / Stone Meal`
`→ Dirt`
`→ Ice / Water`
`→ Farming`
`→ Mud / Packed Mud / Mud Bricks`
`→ Washing Basin`
`→ Raw Iron / Iron`
`→ Condenser Mark II + Garden Chamber`
`→ renewable wood / leaves / crops`
`→ Crucible`
`→ Lava / Obsidian`
`→ Gravel / Sand / renewable Netherrack`
`→ Nether Brick Dust`
`→ Nether Concentrate + Quartz + Gold chance`
`→ Mineral Blend`
`→ Redstone`
`→ Refining Condenser`
`→ Lapis / Copper / Osmium / Emerald / Diamond`
`→ broader technology progression`

This is not a claim that the entire pack is finished. It is the implemented resource spine currently available before later technology, dimension, boss and endgame expansion.

## 1. Foundations — wood into stone resources

The opening economy begins with the island Oak tree and Condenser Mark I.

Current primitive conversions include:

- **1 Log -> 4 Stone Fragments** in **120 ticks / 6 seconds**;
- **1 Plank -> 1 Stone Fragment**;
- **4 Stone Fragments -> 1 Cobblestone** through crafting;
- Cobblestone can be smelted normally into Stone.

Logs are the throughput-efficient route: converting one Log directly produces the same fragment amount as converting its four Planks, but needs one Condenser operation instead of four.

The Condenser remains fuel- and FE-free. Blocked output pauses production without consuming input or discarding partial progress.

## 2. Stone Meal and Mixing Chamber

The reusable Mortar provides the first manual Stone Meal route. Stone Meal then unlocks the Mixing Chamber and the first renewable terrain loop.

Important Mixing recipes:

- **Stone Meal + Oak Sapling -> Dirt**;
- **4 Dirt + 1 Ice -> 4 Mud** in **160 ticks / 8 seconds**;
- **1 Mud + 1 Wheat -> 1 Packed Mud** in **160 ticks / 8 seconds**.

Packed Mud can continue through the established Mud Brick route. See [CONDENSER_MIXING.md](CONDENSER_MIXING.md).

## 3. Freezing, water and farming

The Freezing Chamber supplies the early Ice route. Ice establishes renewable water and remains useful afterward as an input for Mud production.

Water is therefore no longer an unresolved progression question: it is an implemented bridge into farming and into the Washing Basin.

Farming establishes renewable Wheat and Wheat Seeds. These resources later feed Packed Mud, Oak Sapling automation and the Garden Chamber crop loop.

## 4. First metal — Washing Basin and Iron

The Washing Basin is the deterministic first-metal route. It requires a directly adjacent **source-water block**; the water is infrastructure and is not consumed.

Primary early recipe:

- **4 Stone Meal -> 1 Raw Iron**;
- **160 ticks / 8 seconds**;
- no FE or fuel;
- adjacent source water required.

Raw Iron is then smelted normally into Iron Ingots.

This resolves the older design question of how first Iron is obtained without direct passive Iron generation from Condenser Mark I. See [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md).

## 5. Condenser Mark II — primitive automation closes

Condenser Mark II is the first Iron-gated automation tier.

Crafting cost:

- 1 Condenser Mark I;
- 4 Iron Ingots;
- 4 Mud Bricks.

It retains Mark-I processing and adjacent Mixing/Freezing support and additionally automates key manual bottlenecks:

- **4 Stone Fragments -> 1 Stone Meal**;
- **4 Wheat Seeds + 1 Bone Meal -> 2 Oak Saplings** with a Mixing Chamber.

Mark II has three upgrade-capacity points and supports Processing Speed, Garden Accelerator, Buffer Expansion and Batch Processing cards. Its outputs support independent **Block / Void Excess / Void All** policies.

## 6. Garden Chamber — renewable wood and crops

Garden Chamber is exclusive to Condenser Mark II.

Oak recipe:

- retained specimen: **1 Oak Sapling**;
- specimen is not consumed;
- **4 Oak Logs + 8 Oak Leaves**;
- **320 ticks / 16 seconds**.

Wheat recipe:

- retained specimen: **1 Wheat Seeds**;
- **4 Wheat + 2 Wheat Seeds**;
- **320 ticks / 16 seconds**.

Together with Leaves -> Bone Meal, Logs -> Stone Fragments and Mark-II Sapling production, this closes the mature early wood/material loop so permanent manual tree chopping is no longer required.

## 7. Crucible — Lava and Obsidian

The Crucible owns a **4,000 mB** internal Lava buffer.

Current Lava inputs are deliberately different in value:

- **8 Cobblestone -> 1,000 mB Lava**;
- **40 Stone Fragments -> 100 mB Lava**.

Heat progression is:

1. Torch;
2. Campfire;
3. Lava source;
4. Mekanism Superheating Element.

An adjacent Mekanism Fluid Tank enables Lava routing. An adjacent Freezing Chamber enables timed Obsidian production:

- **1,000 mB Lava -> 1 Obsidian**;
- **100 ticks / 5 seconds** of freezing once the route is available.

The Basic Fluid Tank remains intentionally redstone-free, because Redstone is introduced later through Mineral Blend rather than being required to bootstrap its own production chain.

## 8. Gravel, Sand and renewable Netherrack

Both Condenser tiers add the renewable granular-material chain:

- **1 Cobblestone -> 1 Gravel** in **120 ticks / 6 seconds**;
- **1 Gravel -> 1 Sand** in **120 ticks / 6 seconds**.

The Crucible can spend stored Lava to create Netherrack:

- **1 Gravel + 250 mB Lava -> 1 Netherrack**;
- **80 ticks / 4 seconds**.

This provides the renewable input for the Nether material loop after the player has already established Lava infrastructure.

## 9. Nether materials and Nether Concentrate

The Nether Fortress is still an exploration/bootstrap source, with its custom chest loot documented in [NETHER_PROGRESSION.md](NETHER_PROGRESSION.md). The long-term processing route is renewable.

After Netherrack is converted through the normal Nether Brick path, Condenser Mark II processes Nether Bricks, Nether Brick Stairs or Nether Brick Fences:

- **1 Nether brick material -> 1 Nether Brick Dust**;
- **160 ticks / 8 seconds**;
- Mark II only.

Washing one Nether Brick Dust produces:

- **1 Nether Concentrate**;
- **2 Quartz guaranteed**;
- **20% chance for 1 Gold Nugget**;
- **160 ticks / 8 seconds**.

Quartz and Gold are therefore fed back into the later Refining recipes instead of being isolated loot-only materials.

## 10. Mineral Blend and renewable Redstone

Condenser Mark II + Mixing Chamber:

- **1 Nether Concentrate + 1 Stone Meal -> 2 Mineral Blend**;
- **160 ticks / 8 seconds**.

Washing Basin:

- **1 Mineral Blend -> 8 Redstone**;
- **120 ticks / 6 seconds**.

This is the current renewable Redstone unlock and the material bridge into powered refining and broader technology recipes.

## 11. Refining Condenser

The player-facing **Refining Condenser** uses the stable internal registry ID `forever_skyblock:refining_chamber`.

Crafting cost:

- **4 Iron Blocks**;
- **2 Redstone**;
- **2 Obsidian**;
- **1 Blast Furnace**.

The machine has **100,000 FE** internal capacity, receives up to **4,000 FE/t**, accepts primary/catalyst item automation and exposes its output for extraction from below.

Current recipes:

| Result | Inputs | Time | FE/t |
| --- | --- | ---: | ---: |
| 6 Lapis Lazuli | 2 Mineral Blend + 1 Redstone | 8 s | 60 |
| 4 Copper Ingots | 2 Mineral Blend + 1 Quartz | 10 s | 80 |
| 2 Osmium Ingots | 3 Mineral Blend + 1 Charcoal | 12 s | 100 |
| 1 Emerald | 4 Mineral Blend + 1 Gold Ingot | 15 s | 120 |
| 1 Diamond | 2 Carbon Matrix + 8 Mineral Blend | 45 s | 240 |

Osmium exists when Mekanism is loaded.

Carbon Matrix is crafted from **8 Charcoal + 1 Mineral Blend**. The Diamond process consumes two Carbon Matrices and eight Mineral Blend and requires **216,000 FE** across the full cycle, forcing continuous energy input because the machine itself stores only 100,000 FE.

Detailed behavior, FE totals and integration notes are in [RESOURCE_REFINING.md](RESOURCE_REFINING.md).

## 12. First technology recipe bridges

Selected external recipes are aligned with this material stage instead of allowing cheap progression skips.

- **Mekanism Basic Logistical Transporter:** 2 Iron Ingots + 1 Redstone -> 8 Transporters.
- **Iron Furnaces Iron Furnace:** 4 Iron Blocks + 4 Iron Ingots + 1 Furnace -> 1 Iron Furnace.

Earlier balancing also keeps the normal Hopper and permanent Iron tools intentionally Iron-expensive. These sinks assume the player may already have an automated Raw-Iron line by this stage.

## 13. Tools, exploration and meta progression

Permanent Vanilla tool progression remains:

**Wood -> Stone -> Iron -> Diamond -> Netherite**, with Gold as the established sidegrade. Exact recipes and permanent-tool behavior are documented in [TOOL_PROGRESSION.md](TOOL_PROGRESSION.md).

The new Refining Condenser resolves the previous missing renewable Diamond production route; Obsidian is already available through the Crucible.

The K-menu, Challenges, Contracts, Shop/Tiers, Research, Perks, world purchases and Ascension remain a parallel persistent progression layer rather than replacements for the physical resource chain. See [CURRENT_STATE.md](CURRENT_STATE.md), [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md) and [ASCENSION.md](ASCENSION.md).

## Still open by design

The implemented progression now reaches powered refining and renewable Diamond, but later progression is still intentionally under active design. Important future areas include:

- deeper Mekanism and other major technology-mod gating;
- additional first-acquisition routes and renewable materials;
- later dimensions and boss requirements;
- Tier II–IV economy and challenge balance;
- late-game industry and endgame resource sinks;
- final pack-wide balance after long-running automated factories are measured in real play.

TeKatz maintains the FTB quest-book sequence separately. This repository documentation describes the actual machine/resource implementation and should not invent quest ordering that has not been authored in the live quest book.
