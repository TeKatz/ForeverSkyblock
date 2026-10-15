# Development mod snapshot — October 15, 2026

The current source is TeKatz's supplied **`Modliste.txt`**, containing **80 enabled artifact labels** and no disabled entries. This is an **experimental development snapshot, subject to change**. Inclusion does not assign a permanent progression role or skill restriction.

The list supplies artifact/version labels, not runtime mod IDs or metadata from inside JARs. A fresh loaded-ID count and embedded dependency list cannot be inferred from it. Minecraft **1.21.1**, NeoForge **21.1.259** and Java **21** remain the previously confirmed runtime baseline; TeKatz has not reported a loader change.

[Machine-readable inventory](../pack/mods.inventory.json) · [Exact supplied text](../pack/snapshots/2026-10-15_modlist.txt) · [Previous runtime snapshot](../pack/snapshots/2026-10-15_080057.runtime.json)

## Changes relative to the previous log

Forever Skyblock is now listed as **0.6.1**, replacing **0.2.1**. Forever Recipe Editor remains **1.4.3-skyblock.1** and Production Monitor remains **5.0.1**. The explicit Iron Jetpacks **8.0.11** label matches the tested research integration baseline; Iron Furnaces is now specified as **4.3.2**.

The following names were absent from the previous runtime snapshot and are now listed. Version labels are read from the supplied artifact names, not from a new runtime log:

| Mod | Supplied version label |
| --- | --- |
| Clean Swing | `1.10` |
| Extreme Sound Muffler | `3.57` |
| FallingTree | `1.21.1.11` |
| Iron Furnaces | `4.3.2` |
| Iron Jetpacks | `8.0.11` |
| Magnum Torch | `21.1.1` |
| ME Requester | `1.5.0` |
| Pick Up Notifier | `21.1.1` |
| Puzzles Lib | `21.1.64` |
| Time in a Bottle | `6.5.4` |
| Twerk Crop Growth | `3.0.0` |

Sodium **0.8.13** and MezzConfig **0.6.8** were already present in the old loaded-ID table. They now also have explicit artifact labels in the supplied list. This is not recorded as a new mod installation or version upgrade. Other unchanged filename labels retain full build identifiers, such as Mekanism `10.7.19.85` and Sophisticated Backpacks `3.26.12.2208`; shorter old runtime versions do not imply an upgrade.

The old log reported **79 loaded IDs** and **67 mods-folder JARs**. Those are historical counts with a different observation method, not current counts to compare directly with the 80 enabled list entries. Previously embedded modules are not marked removed simply because the new text does not enumerate them. DeepCore is not listed; its inclusion remains undecided.

## Complete enabled list

Artifact labels below are preserved exactly as supplied. No `.jar` suffix, normalized mod ID or download identifier is invented.

| # | Enabled artifact label |
| --- | --- |
| 1 | `ae2wtlib-19.5.1` |
| 2 | `Apotheosis-1.21.1-8.9.0` |
| 3 | `ApothicAttributes-1.21.1-2.11.0` |
| 4 | `ApothicEnchanting-1.21.1-1.6.4` |
| 5 | `ApothicSpawners-1.21.1-1.4.0` |
| 6 | `appleskin-neoforge-mc1.21-3.0.9` |
| 7 | `appliedenergistics2-19.2.18` |
| 8 | `Applied-Mekanistics-1.6.3` |
| 9 | `architectury-13.0.11-neoforge` |
| 10 | `BadOptimizations-2.4.1-1.21.1` |
| 11 | `balm-neoforge-1.21.1-21.0.66` |
| 12 | `better-advanced-tooltips-2101.1.0-build.5` |
| 13 | `bookshelf-neoforge-1.21.1-21.1.81` |
| 14 | `cleanswing-1.10-1.21` |
| 15 | `cloth-config-15.0.140-neoforge` |
| 16 | `Clumps-neoforge-1.21.1-19.0.0.1` |
| 17 | `configured-neoforge-1.21.1-2.6.3` |
| 18 | `Controlling-neoforge-1.21.1-19.0.5` |
| 19 | `Cucumber-1.21.1-8.0.16` |
| 20 | `curios-neoforge-9.5.1+1.21.1` |
| 21 | `DefaultWorldType-1.21-5.0.4` |
| 22 | `enchdesc-neoforge-1.21.1-21.1.11` |
| 23 | `entityculling-neoforge-1.11.3-mc1.21.1` |
| 24 | `EuphoriaPatcher-1.10.5-r5.9.3-neoforge` |
| 25 | `ExtendedAE-1.21-2.2.35-neoforge` |
| 26 | `ExtremeSoundMuffler-3.57_NeoForge-1.21` |
| 27 | `FallingTree-1.21.1-1.21.1.11` |
| 28 | `ferritecore-7.0.3-neoforge` |
| 29 | `ForeverProductionMonitor-1.21.1-NeoForge-5.0.1` |
| 30 | `ForeverRecipeEditor-1.21.1-NeoForge-1.4.3-skyblock.1` |
| 31 | `ForeverSkyblock-1.21.1-NeoForge-0.6.1` |
| 32 | `ftb-backups-3-21.1.5` |
| 33 | `ftb-chunks-neoforge-2101.1.22` |
| 34 | `ftb-essentials-neoforge-2101.1.10` |
| 35 | `ftb-library-neoforge-2101.1.37` |
| 36 | `ftb-quests-neoforge-2101.1.36` |
| 37 | `ftb-teams-neoforge-2101.1.11` |
| 38 | `ftb-ultimine-neoforge-2101.1.15` |
| 39 | `functionalstorage-1.21.1-1.5.7` |
| 40 | `Glodium-1.21-2.2-neoforge` |
| 41 | `guideme-21.1.19` |
| 42 | `ImmediatelyFast-NeoForge-1.6.15+1.21.1` |
| 43 | `industrialforegoing-1.21-3.6.39` |
| 44 | `iris-neoforge-1.8.14-beta.1+mc1.21.1` |
| 45 | `ironfurnaces-neoforge-1.21.1-4.3.2` |
| 46 | `IronJetpacks-1.21.1-8.0.11` |
| 47 | `Jade-1.21.1-NeoForge-15.10.6` |
| 48 | `jei-1.21.1-neoforge-19.64.0.454` |
| 49 | `kubejs_curios_neoforge_1.21.1-1.0.4` |
| 50 | `kubejs-mekanism-neoforge-2101.1.7-build.18` |
| 51 | `kubejs-neoforge-2101.7.2-build.377` |
| 52 | `LibX-1.21.1-6.0.15` |
| 53 | `lithium-neoforge-0.15.4+mc1.21.1` |
| 54 | `MagnumTorch-v21.1.1-1.21.1-NeoForge` |
| 55 | `Mekanism-1.21.1-10.7.19.85` |
| 56 | `merequester-neoforge-1.21.1-1.5.0` |
| 57 | `mezz_config-1.21.1-neoforge-0.6.8-standalone` |
| 58 | `mob_grinding_utils-1.1.10+mc1.21.1` |
| 59 | `modernfix-neoforge-5.27.26+mc1.21.1` |
| 60 | `MouseTweaks-neoforge-mc1.21-2.26.1` |
| 61 | `MysticalAgriculture-1.21.1-8.0.28` |
| 62 | `Patchouli-1.21.1-93-NEOFORGE` |
| 63 | `PickUpNotifier-v21.1.1-1.21.1-NeoForge` |
| 64 | `Placebo-1.21.1-9.9.4` |
| 65 | `polymorph-neoforge-1.2.0+1.21.1` |
| 66 | `Powah-6.2.10` |
| 67 | `prickle-neoforge-1.21.1-21.1.11` |
| 68 | `puzzleslib-v21.1.64-mc1.21.1+neoforge` |
| 69 | `rhino-2101.2.8-build.91` |
| 70 | `Searchables-neoforge-1.21.1-1.0.2` |
| 71 | `SkyblockBuilder-21.1.37` |
| 72 | `sodium-neoforge-0.8.13+mc1.21.1` |
| 73 | `sophisticatedbackpacks-1.21.1-3.26.12.2208` |
| 74 | `sophisticatedcore-1.21.1-1.5.12.2400` |
| 75 | `sophisticatedstorage-1.21.1-1.6.6.2181` |
| 76 | `spark-1.10.124-neoforge` |
| 77 | `tiab-neoforge-6.5.4` |
| 78 | `titanium-1.21-4.0.51` |
| 79 | `TwerkCropGrowth-neoforge-1.21.1-3.0.0` |
| 80 | `waystones-neoforge-1.21.1-21.1.46` |

Source-text SHA-256: `64b716a2b4359ebc6dbe5076e45a55d6900a847964fa4ef24af0fbfaece5c646`. The original log-derived JSON is retained unchanged in the previous-runtime snapshot. Future updates should use newly supplied evidence. Neither snapshot is a launcher manifest or a compatibility audit.
