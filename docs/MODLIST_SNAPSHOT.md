# Development mod snapshot — October 22, 2026

The current source is TeKatz's supplied **`Modliste(6).txt`**, containing **94 enabled artifact labels** and no disabled entries. This remains an experimental development snapshot and does not by itself assign progression roles or guarantee final pack inclusion.

The supplied component header reports Minecraft **1.21.1**, NeoForge **21.1.264** and LWJGL **3.3.3**. Java remains **21** for the repository build. No fresh runtime log or loaded-mod-ID export accompanied this list, so no new loaded-ID or embedded-module count is inferred.

[Machine-readable inventory](../pack/mods.inventory.json) · [Exact October 22 supplied text](../pack/snapshots/2026-10-22_modlist.txt) · [Previous October 21 list](../pack/snapshots/2026-10-21_modlist.txt) · [Previous runtime snapshot](../pack/snapshots/2026-10-15_080057.runtime.json)

## Changes since October 21

The launcher artifact list grew from **87 to 94 enabled entries**.

### Added labels

| Mod / artifact | Supplied label |
| --- | --- |
| AdvancedAE | `AdvancedAE-1.6.12-1.21.1` |
| AE2 Pattern Restocker | `ae2_pattern_restocker-neoforge-21.1-1.1.0` |
| CobbleGenGalore | `cobblegengalore-1.21.1-0.2.9` |
| GeckoLib | `geckolib-neoforge-1.21.1-4.9.3` |
| Not Enough Recipe Book | `Not Enough Recipe Book-NEOFORGE-0.4.3+1.21` |
| OctoLib | `OctoLib-NEOFORGE-0.6.2+1.21` |
| ServerCore | `servercore-neoforge-1.5.19+1.21.1` |

### Version / artifact replacements

| Component | October 21 | October 22 |
| --- | --- | --- |
| Forever Skyblock | `ForeverSkyblock-0.26.4-Mekanism-Automation-Test` | `ForeverSkyblock-main-latest` |
| Puzzles Lib | `puzzleslib-v21.1.64-mc1.21.1+neoforge` | `puzzleslib-v21.1.65-mc1.21.1+neoforge` |
| Sophisticated Core | `sophisticatedcore-1.21.1-1.5.16.2422` | `sophisticatedcore-1.21.1-1.5.17.2426` |
| Sophisticated Storage | `sophisticatedstorage-1.21.1-1.6.7.2184` | `sophisticatedstorage-1.21.1-1.6.8.2196` |

No October 21 artifact label is otherwise absent from the supplied October 22 list. `ForeverDeepCore` is still not listed.

## Loader/runtime update

The current pack runtime is **NeoForge 21.1.264**, replacing the previous current baseline **21.1.262**. The standalone Gradle default and full pack-runtime CI target are aligned to 21.1.264 in the same update.

This does **not** rewrite historical evidence. Earlier CI/runtime records continue to name the exact NeoForge version used by those runs, including 21.1.259 and 21.1.262.

## Exact current list

The authoritative exact text is stored at [`pack/snapshots/2026-10-22_modlist.txt`](../pack/snapshots/2026-10-22_modlist.txt). The machine-readable copy is [`pack/mods.inventory.json`](../pack/mods.inventory.json). Those files preserve every supplied artifact label rather than normalizing names into inferred mod IDs.

Source-text SHA-256: `10632f4585ea371377321e3aaf62a802de00dd32e90497ca483c53d51ab0ae88`.

The October 15 runtime log remains the last machine-derived runtime-ID snapshot (**79 loaded IDs**, **67 mods-folder JARs**, NeoForge **21.1.259**). Those historical counts use a different observation method and must not be compared directly with the current 94 launcher artifact labels.
