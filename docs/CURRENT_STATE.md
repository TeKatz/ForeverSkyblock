# Current integrated state

Updated October 22, 2026. `main` remains **0.26.4** and now includes the renewable resource-refining stack from PR #28 together with its validated PR #29 hotfix set. PR #29 was merged into `feature/resource-refining-progression`; PR #28 was then merged into `main` as `d97e69781eabb8471ecc7388cc1a164b74eeb94a`.

## Baseline

| Item | Current value |
| --- | --- |
| Mod version | **0.26.4** |
| Minecraft | **1.21.1** |
| Java | **21** |
| Current pack runtime | **NeoForge 21.1.262** |
| Default development NeoForge | **21.1.249** |
| Current supplied mod-list snapshot | **87 enabled artifact labels** (`Modliste(5).txt`, October 21, 2026) |
| Player profile schema | **dynamic 4 / 5 / 6** |
| World-progress schema | **2** |
| Progression protocol | **skills-9** plus integrated K-menu editor channel |
| Current gameplay baseline merge | `d97e69781eabb8471ecc7388cc1a164b74eeb94a` |
| Release | Active development; repository integration is not a public pack release |

TeKatz reported testing the October 21 supplied modpack state on **NeoForge 21.1.262** with **no errors observed**. That pack acceptance predates the October 22 resource-refining merge and is not presented as post-merge gameplay validation.

The fixed starter island remains protected and unchanged.

## Integrated feature stack

### K-menu and persistent progression

The rebuilt K-menu and K-menu Studio remain integrated. Challenges, Contracts, Shop/Tiers, Research, Perks, player/world statistics and server-owned publication rules are unchanged by the resource-refining merge. See [K_MENU_EDITOR.md](K_MENU_EDITOR.md) and [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md).

### Early material automation

The existing material route remains active:

`Wood -> Stone Fragments -> Stone Meal / Dirt -> Ice / Water -> Farm -> Mud / Packed Mud -> Washing Basin -> Raw Iron -> Iron -> Condenser Mark II + Garden Chamber`

Key behavior:

- **1 Log -> 4 Stone Fragments / 120 ticks (6 seconds)** on both Condenser tiers.
- Mixing supports Dirt, Mud and Packed Mud production.
- Washing Basin provides the deterministic first-Iron route: **4 Stone Meal -> 1 Raw Iron / 160 ticks (8 seconds)** with adjacent source water.
- Condenser Mark II automates Stone Meal and Oak Sapling production.
- Garden Chamber retains Oak Saplings or Wheat Seeds and automates Logs/Leaves or Wheat/Seeds.
- Mark II retains three upgrade-capacity points and independent **Block / Void Excess / Void All** output policies.

See [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md).

### Crucible — Lava, Obsidian and renewable Netherrack

The Crucible has a **4,000 mB** internal Lava buffer.

Current Lava production rates:

- **8 Cobblestone -> 1,000 mB Lava**;
- **40 Stone Fragments -> 100 mB Lava**.

Heat tiers remain **Torch -> Campfire -> Lava -> Mekanism Superheating Element**. An adjacent Fluid Tank enables Lava routing and an adjacent Freezing Chamber enables **1,000 mB Lava -> 1 Obsidian** through the timed freezing route.

The October 22 integration adds the renewable Nether bridge:

- **1 Gravel + 250 mB stored Lava -> 1 Netherrack**;
- **80 ticks / 4 seconds**;
- produced items can auto-push into adjacent item-handler inventories.

The filled Crucible has a dedicated heated/Lava presentation. JEI now shows actual Lava fluid amounts instead of representing those operations with Lava Buckets.

### Renewable granular and Nether-material chain

Both Condenser tiers process:

- **1 Cobblestone -> 1 Gravel / 120 ticks (6 seconds)**;
- **1 Gravel -> 1 Sand / 120 ticks (6 seconds)**.

After Netherrack enters the normal Nether Brick path, Condenser Mark II processes Nether Bricks, Nether Brick Stairs or Nether Brick Fences:

- **1 Nether brick material -> 1 Nether Brick Dust / 160 ticks (8 seconds)**.

Washing Basin then produces:

- **1 Nether Brick Dust -> 1 Nether Concentrate**;
- **+2 Quartz guaranteed**;
- **+20% chance for 1 Gold Nugget**;
- **160 ticks / 8 seconds**.

Fortress exploration remains a bootstrap source, but the long-term Nether material loop is renewable. See [NETHER_PROGRESSION.md](NETHER_PROGRESSION.md).

### Mineral Blend and renewable Redstone

Condenser Mark II + Mixing Chamber:

- **1 Nether Concentrate + 1 Stone Meal -> 2 Mineral Blend / 160 ticks (8 seconds)**.

Washing Basin:

- **1 Mineral Blend -> 8 Redstone / 120 ticks (6 seconds)**.

This is the current renewable Redstone route. The Mekanism Basic Fluid Tank remains intentionally redstone-free so Crucible infrastructure can be established before Redstone itself is unlocked.

### Refining Condenser

The player-facing **Refining Condenser** is integrated. Its internal registry ID remains `forever_skyblock:refining_chamber` for save compatibility.

Crafting cost:

- 4 Iron Blocks;
- 2 Redstone;
- 2 Obsidian;
- 1 Blast Furnace.

Machine behavior:

- primary input + catalyst + output;
- item automation and bottom output extraction;
- **100,000 FE** internal energy capacity;
- maximum **4,000 FE/t** receive;
- no energy export;
- processing pauses on missing power or blocked output.

Current refining recipes:

| Result | Inputs | Time | FE/t |
| --- | --- | ---: | ---: |
| 6 Lapis Lazuli | 2 Mineral Blend + 1 Redstone | 8 s | 60 |
| 4 Copper Ingots | 2 Mineral Blend + 1 Quartz | 10 s | 80 |
| 2 Osmium Ingots | 3 Mineral Blend + 1 Charcoal | 12 s | 100 |
| 1 Emerald | 4 Mineral Blend + 1 Gold Ingot | 15 s | 120 |
| 1 Diamond | 2 Carbon Matrix + 8 Mineral Blend | 45 s | 240 |

**Carbon Matrix** is crafted from **8 Charcoal + 1 Mineral Blend**. The Diamond cycle consumes 216,000 FE in total, deliberately requiring continuing external FE because that exceeds the machine's internal capacity.

JEI contains the dedicated Refining category and catalyst; the GUI has a Recipes button, corrected FE layout and dedicated machine/item presentation. See [RESOURCE_REFINING.md](RESOURCE_REFINING.md).

### Progression-aligned external recipes

The integrated hotfix also aligns selected external recipes with the current economy:

- **Mekanism Basic Logistical Transporter:** 2 Iron Ingots + 1 Redstone -> 8 Transporters.
- **Iron Furnaces Iron Furnace:** 4 Iron Blocks + 4 Iron Ingots + 1 Furnace.

Earlier Iron-expensive Hopper and permanent Iron-tool recipes remain in place.

### Machine GUI coordinate contract

The final PR #29 GUI correction removes the custom per-screen fit-to-window `PoseStack` scale and mouse-coordinate remapping from the shared machine container screen. Machine slot frames, Vanilla ItemStacks, clicks/hovering and JEI areas now use the native `AbstractContainerScreen` `leftPos`/`topPos` coordinate system.

This was merged specifically to eliminate the long-running resize/GUI-scale drift where rendered inventory items could separate from their slot frames. The final source is merged by project authorization; no additional post-merge real-player acceptance is claimed in this document.

## Current progression spine

The implemented material progression now reaches renewable powered refining:

`Wood -> Stone Fragments -> Stone Meal / Dirt -> Ice / Water -> Farm -> Mud / Packed Mud -> Raw Iron / Iron -> Mark II / Garden -> Lava / Obsidian -> Gravel / Sand / Netherrack -> Nether Concentrate -> Mineral Blend -> Redstone -> Refining Condenser -> Lapis / Copper / Osmium / Emerald / Diamond`

[PROGRESSION.md](PROGRESSION.md) is the live sequential map for this route.

## Current modpack snapshot

The authoritative supplied artifact-label snapshot remains [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md):

- JEI **19.69.0.457**;
- Mekanism / Mekanism Generators **10.7.19.85**;
- Forever Production Monitor **5.0.1**;
- supplied Forever Recipe Editor **1.4.2 (2)** artifact;
- supplied Forever Skyblock **0.26.4-Mekanism-Automation-Test** artifact;
- current installed loader **NeoForge 21.1.262**.

The list is a launcher artifact-label snapshot, not a final distribution manifest.

## Meta progression and remaining design boundary

The existing challenge/shop/research/perk/world-purchase rules remain in place. The resource progression now resolves earlier missing routes for Water, first Iron, Lava/Obsidian, renewable Redstone and renewable Diamond.

Still-active design areas include deeper technology-mod gating, further first-acquisition routes, dimensions/bosses, Tier II–IV economy, late-game industry and final pack-wide balance after extended factory testing.

## CI and validation

The final pre-integration hotfix source was `38a730e77c066ada5ade7395568baa04c1c635ff`. GitHub Actions run **#563** passed:

1. Java build and unit tests;
2. client smoke test;
3. visual fixtures;
4. dedicated-server regression GameTests;
5. full pack-runtime integrations on **NeoForge 21.1.259**.

The current installed pack baseline is separately **NeoForge 21.1.262** from the October 21 snapshot. Historical/current automated runs that used 21.1.259 remain described as 21.1.259; they are not retroactively relabeled.

The fixed island `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` remains protected pack state.
