# Current integrated state

Updated October 23, 2026. `main` is **0.27.0** and includes the renewable resource-refining stack from PR #28/#29, the machine visual overhaul from PR #30 and omnidirectional machine item capabilities from PR #34.

## Baseline

| Item | Current value |
| --- | --- |
| Mod version | **0.27.0** |
| Minecraft | **1.21.1** |
| Java | **21** |
| Current pack/runtime + development target | **NeoForge 21.1.264** |
| NeoForge compatibility minimum in mod metadata | **21.1.249** |
| Current supplied mod-list snapshot | **94 enabled artifact labels** (`Modliste(6).txt`, October 22, 2026) |
| Player profile schema | **dynamic 4 / 5 / 6** |
| World-progress schema | **2** |
| Progression protocol | **skills-9** plus integrated K-menu editor channel |
| Current visual-overhaul merge | `5d8ab39b5c8820f4b8f1441715ebbbd9ec5b372d` |
| Release | Active development; repository integration is not a public pack release |

The supplied launcher list reports NeoForge 21.1.264. No fresh runtime log or explicit gameplay-validation statement accompanied `Modliste(6).txt`, so the repository does not invent a new loaded-mod-ID count or player-test result from the list alone.

The fixed starter island remains protected and unchanged.

## Integrated feature stack

### K-menu and persistent progression

The rebuilt K-menu and K-menu Studio are integrated. Challenges, Contracts, Shop/Tiers, Research, Perks, player/world statistics and server-owned publication rules remain active. See [K_MENU_EDITOR.md](K_MENU_EDITOR.md) and [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md).

### Early material automation

Current early route:

`Wood -> Stone Fragments -> Stone Meal / Dirt -> Ice / Water -> Farm -> Mud / Packed Mud -> Washing Basin -> Raw Iron -> Iron -> Condenser Mark II + Garden Chamber`

Key behavior:

- **1 Log -> 4 Stone Fragments / 120 ticks (6 seconds)** on both Condenser tiers.
- Mixing supports Dirt, Mud and Packed Mud production.
- Washing Basin provides the first-Iron route: **4 Stone Meal -> 1 Raw Iron / 160 ticks (8 seconds)** with adjacent source water.
- Condenser Mark II automates Stone Meal and Oak Sapling production.
- Garden Chamber retains Oak Saplings or Wheat Seeds and automates Logs/Leaves or Wheat/Seeds.
- Mark II has three upgrade-capacity points and independent **Block / Void Excess / Void All** output policies.

See [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md).

### Machine item cable access

PR #34 is integrated through merge `88414546678f53ee4983f56d89b7ff7e9adbdd31`. Condenser Mark I/II, Garden Chamber, Washing Basin and Refining Condenser item capabilities expose their permitted operations on all six faces. Slot validation, filters, output restrictions, buffers and policies remain active. Garden primary and secondary harvest still use the Condenser and Chamber respectively; bottom hoppers continue to work. The Crucible keeps its existing sided item/fluid behavior, and Ascension remains omnidirectional. Automated validation and the remaining manual transporter-network check are recorded in [VALIDATION.md](VALIDATION.md).

### Crucible — Lava, Obsidian and renewable Netherrack

The Crucible has a **4,000 mB** internal Lava buffer.

- **8 Cobblestone -> 1,000 mB Lava**
- **40 Stone Fragments -> 100 mB Lava**
- **1 Gravel + 250 mB stored Lava -> 1 Netherrack / 80 ticks**

Heat tiers remain **Torch -> Campfire -> Lava -> Mekanism Superheating Element**. Adjacent Fluid Tanks enable Lava routing; an adjacent Freezing Chamber enables **1,000 mB Lava -> 1 Obsidian**. Produced items can auto-push into adjacent compatible inventories. JEI shows real fluid amounts.

### Renewable Nether materials and Redstone

Both Condenser tiers process:

- **1 Cobblestone -> 1 Gravel / 120 ticks**
- **1 Gravel -> 1 Sand / 120 ticks**

Condenser Mark II processes Nether brick material into Nether Brick Dust. Washing then produces **1 Nether Concentrate + 2 Quartz + 20% Gold Nugget** from one Dust. Mark II + Mixing converts **1 Nether Concentrate + 1 Stone Meal -> 2 Mineral Blend / 160 ticks**, and Washing converts **1 Mineral Blend -> 8 Redstone / 120 ticks**.

See [NETHER_PROGRESSION.md](NETHER_PROGRESSION.md) and [RESOURCE_REFINING.md](RESOURCE_REFINING.md).

### Refining Condenser

The player-facing **Refining Condenser** uses the stable internal registry ID `forever_skyblock:refining_chamber`.

Machine properties:

- primary input + catalyst + output;
- item input/output capability access on every face;
- **100,000 FE** internal energy capacity;
- maximum **4,000 FE/t** receive;
- no energy export.

| Result | Inputs | Time | FE/t |
| --- | --- | ---: | ---: |
| 6 Lapis Lazuli | 2 Mineral Blend + 1 Redstone | 8 s | 60 |
| 4 Copper Ingots | 2 Mineral Blend + 1 Quartz | 10 s | 80 |
| 2 Osmium Ingots | 3 Mineral Blend + 1 Charcoal | 12 s | 100 |
| 1 Emerald | 4 Mineral Blend + 1 Gold Ingot | 15 s | 120 |
| 1 Diamond | 2 Carbon Matrix + 8 Mineral Blend | 45 s | 240 |

**Carbon Matrix** is crafted from **8 Charcoal + 1 Mineral Blend**.

### Machine visual overhaul — integrated 0.27.0

PR #30 is integrated into `main`. All nine custom machine exteriors and five machine container screens use the 0.27.0 graphite/copper/steel visual system with machine-specific accents, refreshed sprites/models and process presentation.

The final Washing Basin fixes and Crucible heat-overlay Z-fighting correction are included in the merged source. Native `AbstractContainerScreen` coordinates remain the GUI contract; no custom fit-to-window scaling layer has been reintroduced.

See [MACHINE_VISUAL_OVERHAUL.md](MACHINE_VISUAL_OVERHAUL.md).

## Current progression spine

`Wood -> Stone Fragments -> Stone Meal / Dirt -> Ice / Water -> Farm -> Mud / Packed Mud -> Raw Iron / Iron -> Mark II / Garden -> Lava / Obsidian -> Gravel / Sand / Netherrack -> Nether Concentrate -> Mineral Blend -> Redstone -> Refining Condenser -> Lapis / Copper / Osmium / Emerald / Diamond`

[PROGRESSION.md](PROGRESSION.md) is the live sequential map.

## Current modpack snapshot

The authoritative supplied artifact-label snapshot is [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md):

- **94 enabled artifact labels** from `Modliste(6).txt`;
- NeoForge **21.1.264**;
- JEI **19.69.0.457**;
- Mekanism / Mekanism Generators **10.7.19.85**;
- Forever Production Monitor **5.0.1**;
- Forever Recipe Editor **1.4.2 (2)** artifact;
- Forever Skyblock launcher label **`ForeverSkyblock-main-latest`**;
- Puzzles Lib **21.1.65**;
- Sophisticated Core **1.5.17.2426**;
- Sophisticated Storage **1.6.8.2196**.

The list is a launcher artifact-label snapshot, not a final distribution manifest or a loaded-mod-ID inventory.

## Validation boundary

Historical test records retain the exact runtime used by each run. For example, PR #30's visual-overhaul validation used the then-current NeoForge 21.1.262 full-pack target; older resource-refining runs used 21.1.259. Those historical claims are not rewritten.

The repository development default and full integration CI target are now aligned to **NeoForge 21.1.264**. The fixed island `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` remains protected pack state.
