# Mods and versions

## Current development snapshot

The actual pack runtime is **Minecraft 1.21.1 / NeoForge 21.1.259**, confirmed by TeKatz and the supplied October 15, 2026 log. Java 21 is the mod's build/runtime baseline. The mod project still compiles and runs its CI server tests against NeoForge 21.1.249; this is a separate build baseline, not the installed pack version.

[Full observed mod table](MODLIST_SNAPSHOT.md) · [Machine-readable inventory and JAR filenames](../pack/mods.inventory.json)

The log lists 79 loaded IDs, including Minecraft, NeoForge and embedded modules, and 67 JARs discovered through the mods-folder locator. **This is an evolving development snapshot.** TeKatz experiments with mods and will periodically provide an updated log. Installed status does not approve a permanent progression role. Keep the runtime version at 21.1.259 until TeKatz reports a change; do not infer it from the mod project's build dependency.

| Component | Observed version |
| --- | --- |
| Forever Skyblock | 0.2.1 |
| Forever Recipe Editor | 1.4.3-skyblock.1 |
| Forever Production Monitor | 5.0.1 |
| Applied Energistics 2 | 19.2.18 |
| JEI | 19.64.0.454 |
| Skyblock Builder | 21.1.37 |
| Default World Type | 1.21-5.0.4 |
| FTB Quests | 2101.1.36 |
| Mekanism | 10.7.19 |
| Industrial Foregoing | 1.21-3.6.39 |
| Mystical Agriculture | 8.0.28 |
| KubeJS | 2101.7.2-build.377 |

Other installed mods, libraries, rendering and utility components are listed in the full snapshot. DeepCore is not present in this log; inclusion and void-world behavior remain undecided. Mystical Agriculture and KubeJS are observed installations, while their final progression roles remain open.

## Separate Forever repositories

- [Forever Recipe Editor](https://github.com/TeKatz/ForeverRecipeEditor): branch [`skyblock`](https://github.com/TeKatz/ForeverRecipeEditor/tree/skyblock), known build commit `3dbaa50e8f0a59b900f8b5e5ac68f3cac73ea32f`. The Skyblock build requires JEI 19.64.0.454. Crafting changes are stored in `config/forever_recipe_editor/generated_pack`.
- [Forever Production Monitor](https://github.com/TeKatz/ForeverProductionMonitor): installed version 5.0.1 in this snapshot; do not substitute a different repository build without a requested pack update.
- [ForeverDeepCore](https://github.com/TeKatz/ForeverDeepCore): separate repository; not installed in this snapshot.

Do not duplicate these mods' sources here. Create and recipe-sieve progression remain excluded from the agreed pack direction.

## Distribution

Do not commit third-party mod JARs. A real launcher/export manifest still needs project/file download identifiers and the selected distribution format; a log does not supply these. Before publication, follow selected mods' licenses/distribution rules and test a fresh installation from the export.

## User-reported additions after the log snapshot

TeKatz added **Iron Jetpacks** and **Iron Furnaces**. Their installed versions await a new log or supplied JAR metadata. The 0.5.0 research integration is tested separately against Iron Jetpacks 8.0.11; this does not assert that exact version is installed.
