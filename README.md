# Forever Skyblock

A standalone **Expert Skyblock modpack for Minecraft 1.21.1 / NeoForge** by TeKatz.

This repository contains the Forever Skyblock mod, the pack-owned configuration and data that are already available, the fixed starting island, and the authoritative project documentation. TeKatz authors the FTB Quests book separately.

## Current baseline

- **Main mod version:** `0.13.4`
- **Minecraft:** 1.21.1
- **Java:** 21
- **Pack runtime:** NeoForge 21.1.259
- **Default development/build baseline:** NeoForge 21.1.249 unless `-PpackRuntime=21.1.259` is supplied
- **Profile schema:** 4
- **Progression network protocol:** `skills-6`
- **Latest integrated development stack:** PR #11, merged into `main` on October 16, 2026
- **Last pre-merge verified feature head:** `f5a7bdadfab0d4480c45898b0b0f288bc7dbbc46`; GitHub Actions run #151 passed
- **Release status:** no release publication is implied by the merge

Start with [docs/CURRENT_STATE.md](docs/CURRENT_STATE.md) for the live implementation boundary and [docs/README.md](docs/README.md) for the documentation map.

## Active systems

The current source includes:

- Condenser Mark I, Stone Fragments, Mortar, Stone Meal and the adjacent Mixing Chamber with persistent filters and JEI recipe transfer;
- the permanent Vanilla tool progression and Reinforced Netherite smithing path;
- Forever Core, Ascension Chamber and persistent Perk Points;
- persistent cross-world progression profiles with Skill Points, purchases, research, refunds and accounting;
- the six-section K menu: Overview, Challenges, Shop, Research, Perks and Profile;
- 20 active personal challenges worth 58 SP across Forever, AE2, Mekanism, Powah, Mystical Agriculture and combat/dimensions;
- permanent Tier 0/I progression;
- stable current-world identity, claims, purchase receipts and write-ahead transaction recovery;
- the development-only Phase 3 current-world test entitlement;
- the Design-2 K-menu visual/navigation overhaul through 0.13.4;
- the corrected Condenser and Ascension machine models, animated internals and GUIs.

Still deliberately inactive: repeatable contracts, Tier II-IV, valuable current-world shop packages/delivery, unresolved shared-machine challenge attribution and unapproved early-resource shortcuts.

## Fixed pack state

The final starting template is `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt`. Its layout, spawn and ME contents are protected by the repository rules unless TeKatz explicitly requests a revision. The selected spawn is `[8, 4, 5]` facing east; the 1k cell contains eight bread and one wheat seed.

`pack/mods.inventory.json` and the mod-list documentation are **development snapshots**, not a launcher lockfile. The supplied 80-entry list predates the current Forever Skyblock mod version, so its `0.6.1` Forever Skyblock entry is historical evidence rather than the repository's current mod version.

## Repository layout

| Path | Purpose |
| --- | --- |
| `mod/` | Standalone Gradle project for the Forever Skyblock mod |
| `pack/` | Pack-owned configuration, data and fixed island files |
| `docs/README.md` | Documentation index and source-of-truth rules |
| `docs/CURRENT_STATE.md` | Current integrated implementation boundary |
| `docs/DESIGN.md` | Agreed project direction and Expert pacing |
| `docs/CHALLENGES.md` | Active challenge catalog and attribution rules |
| `docs/K_MENU_DESIGN2.md` | Current K-menu visual/navigation design |
| `docs/WORLD_SHOP.md` | Stable world identity and transaction safety |
| `docs/VALIDATION.md` | Detailed automated and user-reported validation history |
| `docs/CHANGELOG.md` | Version-level change summary |
| `AGENTS.md` | Repository working rules |

## Build and test

Install Java 21, then run from `mod/`:

```sh
./gradlew build
```

Windows:

```bat
gradlew.bat build
```

The production JAR is written to `mod/build/libs/`. The GitHub Actions workflow also runs the dedicated Condenser tests and the NeoForge 21.1.259 integration suite with the current optional integrations.

## Pack completeness

The fixed island and current world configuration are present. A complete distributable pack still needs the final launcher/export manifest plus any future authored quests, scripts and intentional configuration overrides. See [docs/FILES_TO_IMPORT.md](docs/FILES_TO_IMPORT.md).

Feature work should happen on a bounded working branch. Merging into `main`, publishing releases and changing the protected starting island require TeKatz's explicit request; see [AGENTS.md](AGENTS.md).
