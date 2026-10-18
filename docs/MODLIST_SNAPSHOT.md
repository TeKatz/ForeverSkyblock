# Development mod snapshot — October 18, 2026

The current source is TeKatz's supplied **`Modliste(4).txt`**, containing **86 enabled artifact labels** and no disabled entries. This is an **experimental development snapshot, subject to change**. Inclusion does not assign a permanent progression role or skill restriction.

The list supplies artifact/version labels, not runtime mod IDs or metadata from inside JARs. A fresh loaded-ID count and embedded dependency list cannot be inferred from it. Minecraft **1.21.1**, NeoForge **21.1.259** and Java **21** remain the previously confirmed runtime baseline; the supplied text does not report a loader change.

[Machine-readable inventory](../pack/mods.inventory.json) · [Exact supplied text](../pack/snapshots/2026-10-18_modlist.txt) · [Previous October 15 mod list](../pack/snapshots/2026-10-15_modlist.txt) · [Previous runtime snapshot](../pack/snapshots/2026-10-15_080057.runtime.json)

## Changes since the October 15 mod-list snapshot

The list grew from **80 to 86 enabled artifact labels**.

### Added labels

| Mod / artifact | Supplied label |
| --- | --- |
| AE2 JEI Integration | `ae2jeiintegration-1.2.1` |
| Cherished Worlds | `cherishedworlds-neoforge-10.1.1+1.21.1` |
| FTB Filter System | `ftb-filter-system-neoforge-21.1.4` |
| FTB Quests Optimizer | `FTBQuestsOptimizer-neoforge-3.2.0-1.21.1` |
| FTB XMod Compat | `ftb-xmod-compat-neoforge-21.1.12` |
| Wooden Hopper | `woodenhopper-neoforge-1.21.1-3.2.1.0` |
| Wooden Shears | `woodenshears-neoforge-1.21-3.2.3.0` |

### Version replacements

| Component | October 15 | October 18 |
| --- | --- | --- |
| Forever Skyblock | `0.6.1` | `0.19.1` |
| Sophisticated Core | `1.5.12.2400` | `1.5.13.2404` |
| Sophisticated Storage | `1.6.6.2181` | `1.6.7.2184` |

`TwerkCropGrowth-neoforge-1.21.1-3.0.0` is no longer listed. The supplied list now explicitly contains Wooden Shears and Wooden Hopper. These are list-level observations only: a launcher artifact label is not a runtime mod ID, and this snapshot does not prove load success or embedded-module state.

The previous runtime log reported **79 loaded IDs** and **67 mods-folder JARs**. Those remain historical counts gathered by a different observation method and should not be compared directly with the current 86 artifact labels. DeepCore is still not listed; its inclusion remains undecided.

## Complete enabled list

Artifact labels below are preserved exactly as supplied. No `.jar` suffix, normalized mod ID or download identifier is invented.

| # | Enabled artifact label |
| --- | --- |
| 1 | `ae2jeiintegration-1.2.1` |
| 2 | `ae2wtlib-19.5.1` |
| 3 | `Apotheosis-1.21.1-8.9.0` |
| 4 | `ApothicAttributes-1.21.1-2.11.0` |
| 5 | `ApothicEnchanting-1.21.1-1.6.4` |
| 6 | `ApothicSpawners-1.21.1-1.4.0` |
| 7 | `appleskin-neoforge-mc1.21-3.0.9` |
| 8 | `appliedenergistics2-19.2.18` |
| 9 | `Applied-Mekanistics-1.6.3` |
| 10 | `architectury-13.0.11-neoforge` |
| 11 | `BadOptimizations-2.4.1-1.21.1` |
| 12 | `balm-neoforge-1.21.1-21.0.66` |
| 13 | `better-advanced-tooltips-2101.1.0-build.5` |
| 14 | `bookshelf-neoforge-1.21.1-21.1.81` |
| 15 | `cherishedworlds-neoforge-10.1.1+1.21.1` |
| 16 | `cleanswing-1.10-1.21` |
| 17 | `cloth-config-15.0.140-neoforge` |
| 18 | `Clumps-neoforge-1.21.1-19.0.0.1` |
| 19 | `configured-neoforge-1.21.1-2.6.3` |
| 20 | `Controlling-neoforge-1.21.1-19.0.5` |
| 21 | `Cucumber-1.21.1-8.0.16` |
| 22 | `curios-neoforge-9.5.1+1.21.1` |
| 23 | `DefaultWorldType-1.21-5.0.4` |
| 24 | `enchdesc-neoforge-1.21.1-21.1.11` |
| 25 | `entityculling-neoforge-1.11.3-mc1.21.1` |
| 26 | `EuphoriaPatcher-1.10.5-r5.9.3-neoforge` |
| 27 | `ExtendedAE-1.21-2.2.35-neoforge` |
| 28 | `ExtremeSoundMuffler-3.57_NeoForge-1.21` |
| 29 | `FallingTree-1.21.1-1.21.1.11` |
| 30 | `ferritecore-7.0.3-neoforge` |
| 31 | `ForeverProductionMonitor-1.21.1-NeoForge-5.0.1` |
| 32 | `ForeverRecipeEditor-1.21.1-NeoForge-1.4.3-skyblock.1` |
| 33 | `ForeverSkyblock-1.21.1-NeoForge-0.19.1` |
| 34 | `ftb-backups-3-21.1.5` |
| 35 | `ftb-chunks-neoforge-2101.1.22` |
| 36 | `ftb-essentials-neoforge-2101.1.10` |
| 37 | `ftb-filter-system-neoforge-21.1.4` |
| 38 | `ftb-library-neoforge-2101.1.37` |
| 39 | `ftb-quests-neoforge-2101.1.36` |
| 40 | `FTBQuestsOptimizer-neoforge-3.2.0-1.21.1` |
| 41 | `ftb-teams-neoforge-2101.1.11` |
| 42 | `ftb-ultimine-neoforge-2101.1.15` |
| 43 | `ftb-xmod-compat-neoforge-21.1.12` |
| 44 | `functionalstorage-1.21.1-1.5.7` |
| 45 | `Glodium-1.21-2.2-neoforge` |
| 46 | `guideme-21.1.19` |
| 47 | `ImmediatelyFast-NeoForge-1.6.15+1.21.1` |
| 48 | `industrialforegoing-1.21-3.6.39` |
| 49 | `iris-neoforge-1.8.14-beta.1+mc1.21.1` |
| 50 | `ironfurnaces-neoforge-1.21.1-4.3.2` |
| 51 | `IronJetpacks-1.21.1-8.0.11` |
| 52 | `Jade-1.21.1-NeoForge-15.10.6` |
| 53 | `jei-1.21.1-neoforge-19.64.0.454` |
| 54 | `kubejs_curios_neoforge_1.21.1-1.0.4` |
| 55 | `kubejs-mekanism-neoforge-2101.1.7-build.18` |
| 56 | `kubejs-neoforge-2101.7.2-build.377` |
| 57 | `LibX-1.21.1-6.0.15` |
| 58 | `lithium-neoforge-0.15.4+mc1.21.1` |
| 59 | `MagnumTorch-v21.1.1-1.21.1-NeoForge` |
| 60 | `Mekanism-1.21.1-10.7.19.85` |
| 61 | `merequester-neoforge-1.21.1-1.5.0` |
| 62 | `mezz_config-1.21.1-neoforge-0.6.8-standalone` |
| 63 | `mob_grinding_utils-1.1.10+mc1.21.1` |
| 64 | `modernfix-neoforge-5.27.26+mc1.21.1` |
| 65 | `MouseTweaks-neoforge-mc1.21-2.26.1` |
| 66 | `MysticalAgriculture-1.21.1-8.0.28` |
| 67 | `Patchouli-1.21.1-93-NEOFORGE` |
| 68 | `PickUpNotifier-v21.1.1-1.21.1-NeoForge` |
| 69 | `Placebo-1.21.1-9.9.4` |
| 70 | `polymorph-neoforge-1.2.0+1.21.1` |
| 71 | `Powah-6.2.10` |
| 72 | `prickle-neoforge-1.21.1-21.1.11` |
| 73 | `puzzleslib-v21.1.64-mc1.21.1+neoforge` |
| 74 | `rhino-2101.2.8-build.91` |
| 75 | `Searchables-neoforge-1.21.1-1.0.2` |
| 76 | `SkyblockBuilder-21.1.37` |
| 77 | `sodium-neoforge-0.8.13+mc1.21.1` |
| 78 | `sophisticatedbackpacks-1.21.1-3.26.12.2208` |
| 79 | `sophisticatedcore-1.21.1-1.5.13.2404` |
| 80 | `sophisticatedstorage-1.21.1-1.6.7.2184` |
| 81 | `spark-1.10.124-neoforge` |
| 82 | `tiab-neoforge-6.5.4` |
| 83 | `titanium-1.21-4.0.51` |
| 84 | `waystones-neoforge-1.21.1-21.1.46` |
| 85 | `woodenhopper-neoforge-1.21.1-3.2.1.0` |
| 86 | `woodenshears-neoforge-1.21-3.2.3.0` |

Source-text SHA-256: `3e703cf5e6ce26a00afbbf83cb288bc3b1f75fa86b6e06dce21176aab8be57df`. The October 15 text list and runtime-derived JSON remain archived unchanged. Future updates should use newly supplied evidence. None of these snapshots is a launcher manifest or a compatibility audit.
