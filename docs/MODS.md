# Mods and versions

## Current development snapshot

The actual pack runtime baseline remains **Minecraft 1.21.1 / NeoForge 21.1.259**, confirmed previously by TeKatz and the supplied October 15, 2026 runtime log. Java 21 is the mod's build/runtime baseline. The mod project compiles against NeoForge 21.1.249 and tests both that baseline and the actual 21.1.259 runtime; the build dependency is tracked separately from the installed pack version.

[Current enabled artifact list](MODLIST_SNAPSHOT.md) · [Machine-readable inventory](../pack/mods.inventory.json) · [Exact October 18 supplied list](../pack/snapshots/2026-10-18_modlist.txt) · [Previous runtime log snapshot](../pack/snapshots/2026-10-15_080057.runtime.json)

The current supplied `Modliste(4).txt` contains **86 enabled artifact labels**. The previous runtime log reported 79 loaded IDs, including Minecraft, NeoForge and embedded modules, and 67 mods-folder JARs; it is archived separately. These counts use different observation methods. The new text does not revalidate runtime mod IDs or embedded modules. **This is an evolving development snapshot, subject to change.** Installed/listed status does not approve a permanent progression role. Keep NeoForge at 21.1.259 until TeKatz reports a change.

| Component | Version from current artifact label |
| --- | --- |
| Forever Skyblock | 0.19.1 |
| Forever Recipe Editor | 1.4.3-skyblock.1 |
| Forever Production Monitor | 5.0.1 |
| Applied Energistics 2 | 19.2.18 |
| AE2 JEI Integration | 1.2.1 |
| ExtendedAE | 2.2.35 |
| JEI | 19.64.0.454 |
| Skyblock Builder | 21.1.37 |
| Default World Type | 5.0.4 |
| FTB Quests | 2101.1.36 |
| FTB Filter System | 21.1.4 |
| FTB Quests Optimizer | 3.2.0 |
| FTB XMod Compat | 21.1.12 |
| Mekanism | 10.7.19.85 |
| Industrial Foregoing | 3.6.39 |
| Mystical Agriculture | 8.0.28 |
| KubeJS | 2101.7.2-build.377 |
| Iron Jetpacks | 8.0.11 |
| Iron Furnaces | 4.3.2 |
| ME Requester | 1.5.0 |
| Sophisticated Core | 1.5.13.2404 |
| Sophisticated Storage | 1.6.7.2184 |
| Time in a Bottle | 6.5.4 |
| Wooden Hopper | 3.2.1.0 |
| Wooden Shears | 3.2.3.0 |

Relative to the October 15 text snapshot, the current list adds AE2 JEI Integration, Cherished Worlds, FTB Filter System, FTB Quests Optimizer, FTB XMod Compat, Wooden Hopper and Wooden Shears. Forever Skyblock is now listed as 0.19.1, Sophisticated Core as 1.5.13.2404 and Sophisticated Storage as 1.6.7.2184. `TwerkCropGrowth-neoforge-1.21.1-3.0.0` is no longer listed. Full labels and the exact comparison are recorded in [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md).

Other installed/listed mods, libraries, rendering and utility components are listed in the full snapshot. DeepCore is not listed; inclusion and void-world behavior remain undecided. Mystical Agriculture and KubeJS are observed list entries, while their final progression roles remain open.

## Separate Forever repositories

- [Forever Recipe Editor](https://github.com/TeKatz/ForeverRecipeEditor): branch [`skyblock`](https://github.com/TeKatz/ForeverRecipeEditor/tree/skyblock), known build commit `3dbaa50e8f0a59b900f8b5e5ac68f3cac73ea32f`. The Skyblock build requires JEI 19.64.0.454. Crafting changes are stored in `config/forever_recipe_editor/generated_pack`.
- [Forever Production Monitor](https://github.com/TeKatz/ForeverProductionMonitor): listed version 5.0.1 in the current snapshot; do not substitute a different repository build without a requested pack update.
- [ForeverDeepCore](https://github.com/TeKatz/ForeverDeepCore): separate repository; not listed in the current supplied export.

Do not duplicate these mods' sources here. Create and recipe-sieve progression remain excluded from the agreed pack direction.

## Distribution

Do not commit third-party mod JARs. A real launcher/export manifest still needs project/file download identifiers and the selected distribution format; a text list or runtime log does not supply these. Before publication, follow selected mods' licenses/distribution rules and test a fresh installation from the export.

## Evidence boundary

The October 18 list is the current artifact-label snapshot, not a fresh runtime log. It confirms what the supplied launcher list names, including Wooden Shears and Wooden Hopper, but it does not independently prove successful loading, embedded modules or compatibility. The October 15 runtime snapshot remains the latest supplied runtime evidence until a newer log is provided.
