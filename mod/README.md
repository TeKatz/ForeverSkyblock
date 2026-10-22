# Forever Skyblock mod — 0.27.0 integrated baseline

Minecraft **1.21.1** / NeoForge **21.1.264 development target** / Java **21**. Maintained by TeKatz.

The current `main` mod baseline is **0.27.0**. It includes the renewable resource-refining stack and the integrated machine visual overhaul. The current supplied pack snapshot contains **94 enabled artifact labels** and reports NeoForge **21.1.264**.

Authoritative summaries:

- [Current integrated state](../docs/CURRENT_STATE.md)
- [Current progression](../docs/PROGRESSION.md)
- [Current mod list](../docs/MODLIST_SNAPSHOT.md)
- [Resource refining](../docs/RESOURCE_REFINING.md)
- [Machine visual overhaul](../docs/MACHINE_VISUAL_OVERHAUL.md)

## Early material progression

`Wood -> Stone Fragments -> Stone Meal / Dirt -> Ice / Water -> Farm -> Mud / Packed Mud -> Washing Basin -> Raw Iron -> Iron -> Condenser Mark II + Garden`

Key numbers:

- **1 Log -> 4 Stone Fragments / 120 ticks (6 s)**
- **4 Stone Meal -> 1 Raw Iron / 160 ticks (8 s)** with adjacent source water
- Garden Oak: retained Sapling -> **4 Logs + 8 Leaves / 320 ticks (16 s)**
- Garden Wheat: retained Wheat Seeds -> **4 Wheat + 2 Seeds / 320 ticks (16 s)**

## Condenser Mark II automation

Mark II has **3 upgrade capacity** and three physical upgrade slots. Current cards are Processing Speed, Garden Accelerator, Buffer Expansion and Batch Processing. Outputs support independent **Block / Void Excess / Void All** policies.

The shared machine-container GUI uses native `AbstractContainerScreen` coordinates. The earlier fit-to-window scaling/mouse-remapping layer is not used.

## Crucible

- **4,000 mB** internal Lava buffer
- **8 Cobblestone -> 1,000 mB Lava**
- **40 Stone Fragments -> 100 mB Lava**
- **1 Gravel + 250 mB stored Lava -> 1 Netherrack / 80 ticks**
- adjacent Fluid Tank routing
- adjacent Freezing Chamber Obsidian production
- automatic item-output pushing
- real Lava-fluid JEI quantities

Heat tiers: **Torch -> Campfire -> Lava -> Mekanism Superheating Element**.

## Renewable Nether / Redstone chain

- Cobblestone -> Gravel -> Sand
- Nether brick material -> Nether Brick Dust
- Washing: Dust -> Nether Concentrate + 2 Quartz + 20% Gold Nugget
- Mark II + Mixing: **Nether Concentrate + Stone Meal -> 2 Mineral Blend / 160 ticks**
- Washing: **1 Mineral Blend -> 8 Redstone / 120 ticks**

## Refining Condenser

Player-facing name: **Refining Condenser**. Stable internal registry ID: `forever_skyblock:refining_chamber`.

- **100,000 FE** storage
- maximum **4,000 FE/t** receive
- primary input, catalyst and output
- standard item/energy capability automation

| Result | Inputs | Duration | FE/t |
| --- | --- | ---: | ---: |
| 6 Lapis Lazuli | 2 Mineral Blend + 1 Redstone | 160 ticks | 60 |
| 4 Copper Ingots | 2 Mineral Blend + 1 Quartz | 200 ticks | 80 |
| 2 Osmium Ingots | 3 Mineral Blend + 1 Charcoal | 240 ticks | 100 |
| 1 Emerald | 4 Mineral Blend + 1 Gold Ingot | 300 ticks | 120 |
| 1 Diamond | 2 Carbon Matrix + 8 Mineral Blend | 900 ticks | 240 |

Carbon Matrix is **8 Charcoal + 1 Mineral Blend -> 1 Carbon Matrix**.

## 0.27.0 machine visual overhaul

The visual overhaul is integrated, not an outstanding feature branch. It refreshes all nine custom machine exteriors and five machine GUIs while preserving production, recipe, energy, automation and save behavior. Final Washing Basin presentation fixes and the Crucible heat-overlay Z-fighting correction are part of the integrated source.

## Current dependency/runtime alignment

- Gradle default NeoForge runtime: **21.1.264**
- Full pack-runtime CI target: **21.1.264**
- JEI development/test dependency: **19.69.0.457**
- NeoForge mod metadata range remains `[21.1.249,21.2)` as a compatibility range, not a current-runtime pin.

The current supplied launcher artifact label is `ForeverSkyblock-main-latest`; the repository mod version remains **0.27.0**.

## Build

```sh
./gradlew build
```

Current integrated output: `build/libs/ForeverSkyblock-1.21.1-NeoForge-0.27.0.jar`.

Historical validation records retain the NeoForge version actually used by each run. See [../docs/VALIDATION.md](../docs/VALIDATION.md) for run-specific evidence.
