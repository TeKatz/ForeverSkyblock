# Forever Skyblock

**Forever Skyblock** is an Expert Skyblock modpack for **Minecraft 1.21.1 / NeoForge** built around deliberate progression, renewable resource loops and long-term automation.

The current integrated `main` baseline is **0.26.4**. On October 22, 2026, the renewable resource-refining stack from PR #28 and its PR #29 hotfix set were integrated into `main`. The implemented material spine now reaches renewable Redstone and FE-powered refining for Lapis, Copper, Osmium, Emerald and Diamond.

See [docs/CURRENT_STATE.md](docs/CURRENT_STATE.md) for the exact integrated state and [docs/PROGRESSION.md](docs/PROGRESSION.md) for the current sequential progression map.

## Current pack baseline

- **Minecraft:** `1.21.1`
- **Current installed pack runtime:** **NeoForge `21.1.262`**
- **Java:** `21`
- **Current supplied mod-list snapshot:** **87 enabled artifact labels**
- **JEI:** `19.69.0.457`
- **Mekanism / Mekanism Generators:** `10.7.19.85`
- **Forever Skyblock mod version:** `0.26.4`

On October 21, 2026, TeKatz reported testing that supplied pack state on NeoForge 21.1.262 with **no errors observed**. The October 22 resource-refining integration has separate automated validation; the earlier pack report is not presented as post-merge gameplay acceptance.

## Project direction

Forever Skyblock is designed as a connected progression world rather than a collection of unrelated mod recipes. Manual work introduces a resource or mechanic; later infrastructure automates the same loop instead of replacing it with an unrelated shortcut.

The pack deliberately avoids a traditional sieve-centered economy. Difficulty should come from production chains, dependencies, resource allocation and automation design rather than random waiting or permanently repeating solved chores.

The broad direction is:

**Foundations → renewable basic resources → water/farming → first metals → automation → Lava/Obsidian → renewable Nether materials → Redstone → powered refining → major technology mods → dimensions/bosses → late-game industry → long-term meta progression**

## Current progression

The implemented resource spine is now:

`Wood → Stone Fragments → Stone Meal / Dirt → Ice / Water → Farm → Mud / Packed Mud → Raw Iron / Iron → Condenser Mark II + Garden → Lava / Obsidian → Gravel / Sand / renewable Netherrack → Nether Concentrate → Mineral Blend → Redstone → Refining Condenser → Lapis / Copper / Osmium / Emerald / Diamond`

Important current systems:

- **Condenser Mark I / II** — primitive material processing and later automation.
- **Mixing Chamber** — two-input recipes such as Dirt, Mud, Packed Mud and Mineral Blend.
- **Freezing Chamber** — early Ice plus Crucible cooling for Obsidian.
- **Washing Basin** — first Raw Iron, Nether Concentrate and renewable Redstone processing.
- **Garden Chamber** — retained Oak/Wheat specimens for automated wood, leaves, Wheat and Seeds.
- **Crucible** — Lava, Obsidian and renewable Netherrack.
- **Refining Condenser** — FE-powered material refinement into Lapis, Copper, Osmium, Emerald and Diamond.

For the exact stage-by-stage route, use [docs/PROGRESSION.md](docs/PROGRESSION.md).

## Crucible

The Crucible owns a **4,000 mB** internal Lava buffer. Current Lava yields are deliberately asymmetric:

- **8 Cobblestone → 1,000 mB Lava**
- **40 Stone Fragments → 100 mB Lava**

Heat progression is **Torch → Campfire → Lava → Mekanism Superheating Element**.

An adjacent Fluid Tank enables Lava routing. An adjacent Freezing Chamber enables timed Obsidian production. The October 22 integration also adds:

- **1 Gravel + 250 mB stored Lava → 1 Netherrack / 4 seconds**
- automatic Crucible item-output pushing into adjacent compatible inventories
- real Lava-fluid JEI amounts instead of Lava Bucket placeholders
- a dedicated filled/heated exterior state

The Mekanism Basic Fluid Tank remains intentionally redstone-free so this infrastructure can exist before renewable Redstone is unlocked.

## Renewable Nether materials and Redstone

The new long-term production chain is:

- **1 Cobblestone → 1 Gravel / 6 s**
- **1 Gravel → 1 Sand / 6 s**
- Gravel + Crucible Lava → renewable Netherrack
- Nether Brick material → Nether Brick Dust on Condenser Mark II
- Washing: Nether Brick Dust → Nether Concentrate + 2 Quartz + 20% Gold Nugget
- Mark II + Mixing: **Nether Concentrate + Stone Meal → 2 Mineral Blend / 8 s**
- Washing: **1 Mineral Blend → 8 Redstone / 6 s**

The Nether Fortress remains useful as an exploration/bootstrap source, but the mature chain is renewable. See [docs/NETHER_PROGRESSION.md](docs/NETHER_PROGRESSION.md).

## Refining Condenser

The player-facing **Refining Condenser** uses the stable internal ID `forever_skyblock:refining_chamber` for compatibility.

Crafting cost:

- **4 Iron Blocks**
- **2 Redstone**
- **2 Obsidian**
- **1 Blast Furnace**

Energy:

- **100,000 FE** internal capacity
- up to **4,000 FE/t** receive
- no energy export

Current refining outputs:

| Output | Inputs | Time | FE/t |
| --- | --- | ---: | ---: |
| 6 Lapis Lazuli | 2 Mineral Blend + 1 Redstone | 8 s | 60 |
| 4 Copper Ingots | 2 Mineral Blend + 1 Quartz | 10 s | 80 |
| 2 Osmium Ingots | 3 Mineral Blend + 1 Charcoal | 12 s | 100 |
| 1 Emerald | 4 Mineral Blend + 1 Gold Ingot | 15 s | 120 |
| 1 Diamond | 2 Carbon Matrix + 8 Mineral Blend | 45 s | 240 |

**Carbon Matrix** is crafted from **8 Charcoal + 1 Mineral Blend**. Detailed FE totals, automation rules and integration notes are in [docs/RESOURCE_REFINING.md](docs/RESOURCE_REFINING.md).

## Technology bridge recipes

Selected external recipes are aligned to the current progression:

- **Mekanism Basic Logistical Transporter:** 2 Iron Ingots + 1 Redstone → 8 Transporters.
- **Iron Furnaces Iron Furnace:** 4 Iron Blocks + 4 Iron Ingots + 1 Furnace.

Earlier expensive Hopper and permanent Iron-tool costs remain part of the Iron-sink balancing strategy for the post-automation stage.

## Condenser Mark II automation

Mark II has **3 upgrade capacity** and three physical upgrade slots:

| Upgrade | Effect | Limit / cost |
| --- | --- | --- |
| Processing Speed Card | 20% / 35% / 50% shorter normal processing time | up to 3, 1 capacity each |
| Garden Accelerator Card | +25% Garden speed per card | up to 2, 1 capacity each |
| Buffer Expansion Card | hidden standard-stack buffering | up to 2, 1 capacity each |
| Batch Processing Card | two normal recipe runs per cycle when possible | max 1, costs 2 capacity |

Outputs have independent **Block / Void Excess / Void All** policies. Buffer Expansion now reports the synchronized visible + hidden stored amount rather than showing only the visible-slot stack count.

## Machine GUI coordinate behavior

The shared machine screens now stay in Vanilla's native `AbstractContainerScreen` coordinate system. The earlier custom per-screen fit-to-window scaling/mouse remapping was removed so slot backgrounds, rendered ItemStacks, hover/click behavior and JEI target areas share the same `leftPos`/`topPos` origin after window or GUI-scale changes.

## K-menu and persistent progression

The K-menu remains the central interface for Challenges, Contracts, Shop/Tiers, Research, Perks, world purchases and player statistics.

The integrated **K-menu Studio** lets authorized users edit supported layouts, quests/chapters, Shop nodes/packages and Contracts; preview and validate changes; publish server-owned documents; retain drafts; reconcile conflicts; restore backups and export configuration. See [docs/K_MENU_EDITOR.md](docs/K_MENU_EDITOR.md).

## Validation boundary

Final pre-integration resource-refining/hotfix source: `38a730e77c066ada5ade7395568baa04c1c635ff`.

GitHub Actions run **#563** passed:

1. Java build and unit tests
2. client smoke test
3. visual fixtures
4. dedicated-server regression GameTests
5. full pack-runtime integration on **NeoForge 21.1.259**

The current installed pack runtime is separately **NeoForge 21.1.262**. Historical automated evidence is kept tied to the runtime actually used by each run.

## Repository structure

| Path | Purpose |
| --- | --- |
| `mod/` | Standalone Gradle project for the Forever Skyblock mod |
| `pack/` | Pack-owned configuration, data and fixed island files |
| `docs/` | Design, progression, validation and system documentation |
| `AGENTS.md` | Repository working rules |

The fixed starting island at `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` remains protected pack state unless a deliberate revision is requested.

The quest book is maintained separately from this repository. Repository progression documents describe implemented resource/machine behavior and do not invent FTB quest ordering.

## Build

Install Java 21 and run from `mod/`:

```sh
./gradlew build
```

Windows:

```bat
gradlew.bat build
```

The production JAR is written to `mod/build/libs/`.

## Documentation

Start with:

- [Current integrated state](docs/CURRENT_STATE.md)
- [Current progression map](docs/PROGRESSION.md)
- [Resource refining](docs/RESOURCE_REFINING.md)
- [Nether progression](docs/NETHER_PROGRESSION.md)
- [Material processing and automation](docs/MATERIAL_PROCESSING.md)
- [Documentation map](docs/README.md)
- [Current mod list](docs/MODLIST_SNAPSHOT.md)
- [K-menu Studio](docs/K_MENU_EDITOR.md)
- [Design direction](docs/DESIGN.md)
- [Validation history](docs/VALIDATION.md)
- [Version history](docs/CHANGELOG.md)
