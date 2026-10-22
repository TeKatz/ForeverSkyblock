# Mods and versions

## Current development snapshot

The current pack baseline is **Minecraft 1.21.1 / NeoForge 21.1.264 / Java 21**. The supplied `Modliste(6).txt` component header reports NeoForge 21.1.264, replacing the previous 21.1.262 pack baseline.

[Current enabled artifact list](MODLIST_SNAPSHOT.md) · [Machine-readable inventory](../pack/mods.inventory.json) · [Exact October 22 supplied list](../pack/snapshots/2026-10-22_modlist.txt) · [Previous October 21 list](../pack/snapshots/2026-10-21_modlist.txt) · [Previous runtime log snapshot](../pack/snapshots/2026-10-15_080057.runtime.json)

The supplied `Modliste(6).txt` contains **94 enabled artifact labels** and no disabled entries, up from 87 in the October 21 list. No fresh runtime log accompanied the list, so loaded-mod-ID and embedded-module counts remain unknown for this snapshot.

## Selected current versions

| Component | Current supplied artifact/version |
| --- | --- |
| Minecraft | 1.21.1 |
| NeoForge pack/runtime + development target | **21.1.264** |
| Java | 21 |
| Forever Skyblock | `main-latest` (repository mod version **0.27.0**) |
| Forever Recipe Editor | `1.4.2 (2)` |
| Forever Production Monitor | 5.0.1 |
| Applied Energistics 2 | 19.2.18 |
| AdvancedAE | **1.6.12** |
| AE2 Pattern Restocker | **1.1.0** |
| AE2 JEI Integration | 1.2.1 |
| ExtendedAE | 2.2.35 |
| JEI | **19.69.0.457** |
| GeckoLib | **4.9.3** |
| CobbleGenGalore | **0.2.9** |
| Not Enough Recipe Book | **0.4.3** |
| OctoLib | **0.6.2** |
| ServerCore | **1.5.19** |
| Puzzles Lib | **21.1.65** |
| Skyblock Builder | 21.1.37 |
| FTB Quests | 2101.1.36 |
| FTB Filter System | 21.1.4 |
| FTB Quests Optimizer | 3.2.0 |
| FTB XMod Compat | 21.1.13 |
| Mekanism | 10.7.19.85 |
| Mekanism Generators | 10.7.19.85 |
| Industrial Foregoing | 3.6.39 |
| Mystical Agriculture | 8.0.28 |
| KubeJS | 2101.7.2-build.377 |
| Iron Jetpacks | 8.0.11 |
| Iron Furnaces | 4.3.2 |
| ME Requester | 1.5.0 |
| Sophisticated Backpacks | 3.26.14.2222 |
| Sophisticated Core | **1.5.17.2426** |
| Sophisticated Storage | **1.6.8.2196** |
| Time in a Bottle | 6.5.4 |
| Wooden Hopper | 3.2.1.0 |
| Wooden Shears | 3.2.3.0 |

## Changes since October 21

Seven new artifact labels were added: **AdvancedAE**, **AE2 Pattern Restocker**, **CobbleGenGalore**, **GeckoLib**, **Not Enough Recipe Book**, **OctoLib** and **ServerCore**.

Four labels were replaced by current versions/builds:

- `ForeverSkyblock-0.26.4-Mekanism-Automation-Test` -> `ForeverSkyblock-main-latest`
- `puzzleslib-v21.1.64-mc1.21.1+neoforge` -> `puzzleslib-v21.1.65-mc1.21.1+neoforge`
- `sophisticatedcore-1.21.1-1.5.16.2422` -> `sophisticatedcore-1.21.1-1.5.17.2426`
- `sophisticatedstorage-1.21.1-1.6.7.2184` -> `sophisticatedstorage-1.21.1-1.6.8.2196`

The pack/runtime target moves from **NeoForge 21.1.262** to **21.1.264**. The exact before/after list is recorded in [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md).

## Separate Forever repositories

- [Forever Recipe Editor](https://github.com/TeKatz/ForeverRecipeEditor): separate repository. Current supplied artifact: `ForeverRecipeEditor-1.21.1-NeoForge-1.4.2 (2)`.
- [Forever Production Monitor](https://github.com/TeKatz/ForeverProductionMonitor): current supplied version 5.0.1.
- [ForeverDeepCore](https://github.com/TeKatz/ForeverDeepCore): separate repository and still not listed in the current supplied export.

Do not duplicate those mods' sources in this repository.

## Distribution

Do not commit third-party mod JARs. A launcher/export manifest still needs project/file download identifiers and the selected distribution format; this text list does not provide those identifiers. Before publication, follow selected mods' licenses/distribution rules and test a fresh installation from the final export.

## Evidence boundary

The October 22 list is the current artifact-label snapshot. It reports the launcher component/runtime state but is **not** a loaded-mod-ID export and is **not** itself a new gameplay-test result. Historical CI/runtime entries retain the exact NeoForge versions used by those runs.

The repository build and full pack-runtime CI target are aligned to **NeoForge 21.1.264** by this update. The NeoForge dependency range in `neoforge.mods.toml` remains a compatibility range rather than a statement of the currently installed pack runtime.
