# Forever Skyblock

**Forever Skyblock** is an Expert Skyblock modpack for **Minecraft 1.21.1 / NeoForge** built around deliberate progression, renewable resource loops and long-term automation.

The current integrated `main` mod baseline is **0.27.0**. The renewable resource-refining stack from PR #28/#29 and the machine visual overhaul from PR #30 are integrated. See [docs/CURRENT_STATE.md](docs/CURRENT_STATE.md) for the exact current state and [docs/PROGRESSION.md](docs/PROGRESSION.md) for the sequential progression map.

## Current pack baseline

- **Minecraft:** `1.21.1`
- **Current pack/runtime + development target:** **NeoForge `21.1.264`**
- **Java:** `21`
- **Current supplied mod-list snapshot:** **94 enabled artifact labels** (`Modliste(6).txt`, October 22, 2026)
- **JEI:** `19.69.0.457`
- **Mekanism / Mekanism Generators:** `10.7.19.85`
- **Forever Skyblock mod version:** `0.27.0`
- **Current supplied Forever Skyblock artifact label:** `ForeverSkyblock-main-latest`

The exact supplied launcher list is stored at [`pack/snapshots/2026-10-22_modlist.txt`](pack/snapshots/2026-10-22_modlist.txt), with a machine-readable inventory at [`pack/mods.inventory.json`](pack/mods.inventory.json). The list reports NeoForge 21.1.264 but is not itself a loaded-mod-ID export or a new gameplay-test result.

## Project direction

Forever Skyblock is designed as a connected progression world rather than a collection of unrelated mod recipes. Manual work introduces a resource or mechanic; later infrastructure automates the same loop instead of replacing it with an unrelated shortcut.

The broad direction is:

**Foundations → renewable basic resources → water/farming → first metals → automation → Lava/Obsidian → renewable Nether materials → Redstone → powered refining → major technology mods → dimensions/bosses → late-game industry → long-term meta progression**

## Current progression

The implemented resource spine is:

`Wood → Stone Fragments → Stone Meal / Dirt → Ice / Water → Farm → Mud / Packed Mud → Raw Iron / Iron → Condenser Mark II + Garden → Lava / Obsidian → Gravel / Sand / renewable Netherrack → Nether Concentrate → Mineral Blend → Redstone → Refining Condenser → Lapis / Copper / Osmium / Emerald / Diamond`

Important current systems:

- **Condenser Mark I / II** — primitive material processing and later automation.
- **Mixing Chamber** — two-input recipes such as Dirt, Mud, Packed Mud and Mineral Blend.
- **Freezing Chamber** — early Ice plus Crucible cooling for Obsidian.
- **Washing Basin** — first Raw Iron, Nether Concentrate and renewable Redstone processing.
- **Garden Chamber** — retained Oak/Wheat specimens for automated wood, leaves, Wheat and Seeds.
- **Crucible** — Lava, Obsidian and renewable Netherrack.
- **Refining Condenser** — FE-powered material refinement into Lapis, Copper, Osmium, Emerald and Diamond.
- **K-menu / K-menu Studio** — persistent Challenges, Contracts, Shop/Tiers, Research, Perks and authoring tools.

## Resource-refining highlights

The Crucible has a **4,000 mB** internal Lava buffer. Current Lava yields are:

- **8 Cobblestone → 1,000 mB Lava**
- **40 Stone Fragments → 100 mB Lava**

Renewable Nether/Redstone chain:

- **1 Cobblestone → 1 Gravel / 6 s**
- **1 Gravel → 1 Sand / 6 s**
- **1 Gravel + 250 mB Lava → 1 Netherrack / 4 s**
- Nether brick material → Nether Brick Dust
- Washing: Nether Brick Dust → Nether Concentrate + 2 Quartz + 20% Gold Nugget
- Mark II + Mixing: **Nether Concentrate + Stone Meal → 2 Mineral Blend / 8 s**
- Washing: **1 Mineral Blend → 8 Redstone / 6 s**

The FE-powered Refining Condenser then produces Lapis, Copper, Osmium, Emerald and Diamond. See [docs/RESOURCE_REFINING.md](docs/RESOURCE_REFINING.md) for exact costs and FE totals.

## Machine visual overhaul

Version **0.27.0** integrates the machine visual overhaul through PR #30. The custom machines now share a graphite/copper/steel visual language with machine-specific accents, refreshed block/item models, process effects and native pixel-aligned GUIs. The approved Garden GUI tree remains intact.

The shared machine screens remain in Vanilla's native `AbstractContainerScreen` coordinate system; no fit-to-window `PoseStack` scaling is used. This preserves slot/ItemStack/click/JEI alignment across window and GUI-scale changes.

See [docs/MACHINE_VISUAL_OVERHAUL.md](docs/MACHINE_VISUAL_OVERHAUL.md).

## Current mod-list update

The October 22 snapshot moves the pack from **87 to 94 enabled artifact labels** and from NeoForge **21.1.262 to 21.1.264**. Newly listed artifacts are AdvancedAE, AE2 Pattern Restocker, CobbleGenGalore, GeckoLib, Not Enough Recipe Book, OctoLib and ServerCore. Puzzles Lib, Sophisticated Core and Sophisticated Storage also move to newer supplied versions, and the Forever Skyblock launcher artifact now points at `main-latest`.

See [docs/MODLIST_SNAPSHOT.md](docs/MODLIST_SNAPSHOT.md) and [docs/MODS.md](docs/MODS.md).

## Repository structure

| Path | Purpose |
| --- | --- |
| `mod/` | Standalone Gradle project for the Forever Skyblock mod |
| `pack/` | Pack-owned configuration, data, snapshots and fixed island files |
| `docs/` | Design, progression, validation and system documentation |
| `AGENTS.md` | Repository working rules |

The fixed starting island at `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` remains protected pack state unless a deliberate revision is requested.

## Build

Install Java 21 and run from `mod/`:

```sh
./gradlew build
```

The development NeoForge default is **21.1.264**. `neoforge.mods.toml` keeps its lower compatibility range separately; that minimum is not the current pack-runtime pin.

## Documentation

Start with:

- [Current integrated state](docs/CURRENT_STATE.md)
- [Current progression map](docs/PROGRESSION.md)
- [Current mod list](docs/MODLIST_SNAPSHOT.md)
- [Mods and versions](docs/MODS.md)
- [Machine visual overhaul](docs/MACHINE_VISUAL_OVERHAUL.md)
- [Resource refining](docs/RESOURCE_REFINING.md)
- [Nether progression](docs/NETHER_PROGRESSION.md)
- [Material processing and automation](docs/MATERIAL_PROCESSING.md)
- [K-menu Studio](docs/K_MENU_EDITOR.md)
- [Validation history](docs/VALIDATION.md)
- [Version history](docs/CHANGELOG.md)
