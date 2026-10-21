# Mods and versions

## Current development snapshot

The current pack baseline is **Minecraft 1.21.1 / NeoForge 21.1.262 / Java 21**. NeoForge 21.1.262 was explicitly reported by TeKatz as the installed pack runtime on October 21, 2026, replacing the previous 21.1.259 baseline.

TeKatz also reported testing the modpack on the current supplied mod set and NeoForge 21.1.262 with **no errors observed**. This is user-reported pack validation, not a fresh runtime-log-derived mod-ID inventory. The standalone Forever Skyblock Gradle project continues to track its own development NeoForge dependency separately.

[Current enabled artifact list](MODLIST_SNAPSHOT.md) · [Machine-readable inventory](../pack/mods.inventory.json) · [Exact October 21 supplied list](../pack/snapshots/2026-10-21_modlist.txt) · [Previous October 18 list](../pack/snapshots/2026-10-18_modlist.txt) · [Previous runtime log snapshot](../pack/snapshots/2026-10-15_080057.runtime.json)

The supplied `Modliste(5).txt` contains **87 enabled artifact labels** and no disabled entries. The previous October 18 list contained 86 labels. The older October 15 runtime log reported 79 loaded IDs and 67 mods-folder JARs on NeoForge 21.1.259; those counts use a different observation method and remain historical evidence only.

## Selected current versions

| Component | Current supplied artifact/version |
| --- | --- |
| Minecraft | 1.21.1 |
| NeoForge pack runtime | **21.1.262** |
| Java | 21 |
| Forever Skyblock | `0.26.4-Mekanism-Automation-Test` |
| Forever Recipe Editor | `1.4.2 (2)` |
| Forever Production Monitor | 5.0.1 |
| Applied Energistics 2 | 19.2.18 |
| AE2 JEI Integration | 1.2.1 |
| ExtendedAE | 2.2.35 |
| JEI | **19.69.0.457** |
| Skyblock Builder | 21.1.37 |
| FTB Quests | 2101.1.36 |
| FTB Filter System | 21.1.4 |
| FTB Quests Optimizer | 3.2.0 |
| FTB XMod Compat | **21.1.13** |
| Mekanism | 10.7.19.85 |
| Mekanism Generators | **10.7.19.85** |
| Industrial Foregoing | 3.6.39 |
| Mystical Agriculture | 8.0.28 |
| KubeJS | 2101.7.2-build.377 |
| Iron Jetpacks | 8.0.11 |
| Iron Furnaces | 4.3.2 |
| ME Requester | 1.5.0 |
| Sophisticated Backpacks | **3.26.14.2222** |
| Sophisticated Core | **1.5.16.2422** |
| Sophisticated Storage | 1.6.7.2184 |
| Time in a Bottle | 6.5.4 |
| Wooden Hopper | 3.2.1.0 |
| Wooden Shears | 3.2.3.0 |

## Changes since October 18

The October 21 list adds **Mekanism Generators** and replaces the listed artifacts/versions for Forever Recipe Editor, Forever Skyblock, FTB XMod Compat, JEI, Sophisticated Backpacks and Sophisticated Core. No other October 18 artifact label is absent from the October 21 list. The exact before/after values are recorded in [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md).

The important loader change is independent of the artifact-label count: the installed pack runtime is now **NeoForge 21.1.262**, not 21.1.259.

## Separate Forever repositories

- [Forever Recipe Editor](https://github.com/TeKatz/ForeverRecipeEditor): separate repository. The current supplied pack artifact is `ForeverRecipeEditor-1.21.1-NeoForge-1.4.2 (2)` and the supplied JEI version is 19.69.0.457.
- [Forever Production Monitor](https://github.com/TeKatz/ForeverProductionMonitor): current supplied version 5.0.1.
- [ForeverDeepCore](https://github.com/TeKatz/ForeverDeepCore): separate repository and still not listed in the current supplied export.

Do not duplicate those mods' sources in this repository.

## Distribution

Do not commit third-party mod JARs. A launcher/export manifest still needs project/file download identifiers and the selected distribution format; a text list does not provide those identifiers. Before publication, follow selected mods' licenses/distribution rules and test a fresh installation from the final export.

## Evidence boundary

The October 21 list is the current artifact-label snapshot. TeKatz additionally reported successful testing of this modpack state on NeoForge 21.1.262 with no errors observed. Because no fresh runtime log was supplied with the list, the repository does **not** claim a new loaded-mod-ID count, embedded-module count or JAR hash inventory.

Historical CI and runtime entries that name NeoForge 21.1.259 remain valid for those specific earlier runs. They should not be rewritten as if those automated runs used 21.1.262.
