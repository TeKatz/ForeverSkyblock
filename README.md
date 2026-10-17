# Forever Skyblock

A standalone **Expert Skyblock modpack for Minecraft 1.21.1 / NeoForge** by TeKatz.

This repository contains the Forever Skyblock mod, the pack-owned configuration and data that are already available, the fixed starting island, and the authoritative project documentation. TeKatz authors the FTB Quests book separately.

## Current baseline

- **Main mod version:** `0.18.1`
- **Minecraft:** 1.21.1
- **Java:** 21
- **Pack runtime:** NeoForge 21.1.259
- **Default development/build baseline:** NeoForge 21.1.249 unless `-PpackRuntime=21.1.259` is supplied
- **Profile schemas:** dynamic 4 / 5 / 6, depending on used progression features
- **Progression network protocol:** `skills-8`
- **Latest integrated development stack:** PR #13, merged into `main` on October 17, 2026
- **Feature integration commit:** `b895e26da9087393d883ec8efa2e083e1606f580`
- **Verified pre-merge feature head:** `97715f06391339141032c5f0c859b5925f99539d`; GitHub Actions run #195 passed
- **Release status:** no release publication is implied by the merge

Start with [docs/CURRENT_STATE.md](docs/CURRENT_STATE.md) for the live implementation boundary and [docs/README.md](docs/README.md) for the documentation map.

## Active systems

The current `main` source includes:

- Condenser Mark I, Stone Fragments, Mortar, Stone Meal and the adjacent Mixing Chamber with persistent input locks and JEI recipe transfer;
- **Freezing Chamber Mark I**, attached directly to the Condenser, with its own lock-aware any-item input, parallel 0/32 material progress and Ice output;
- four live Condenser layouts: base, Mixing only, Freezing only, and Mixing + Freezing;
- 8 Leaves -> 1 Bone Meal Condenser processing and a Stone Meal-gated Vanilla Crafting Table recipe;
- the permanent Vanilla tool progression and Reinforced Netherite smithing path;
- Forever Core, Ascension Chamber and persistent Perk Points;
- persistent cross-world progression profiles with Skill Points, purchases, research, refunds and accounting;
- the six-section K menu: Overview, Challenges, Shop, Research, Perks and Profile;
- 20 active personal challenges worth 58 SP across Forever, AE2, Mekanism, Powah, Mystical Agriculture and combat/dimensions;
- five repeatable contracts for renewable SP after Tier I;
- permanent Tier 0/I progression;
- stable current-world identity, claims, purchase receipts and write-ahead transaction recovery;
- the Tier-I **Advanced AE2 Starter Package** with receipt-bound Supply Cache delivery;
- three Tier-0 world-wide upgrades: No Phantoms, No More Rain and Permanent Day;
- a save-wide Wandering Trader gate unlocked by the first real-player Emerald pickup;
- the Design-2 K-menu visual/navigation stack and the 0.16.0 stability/polish pass;
- the 0.18.1 Condenser/Freezing UI, JEI and model cleanup accepted in-game as the next development baseline.

Still deliberately inactive or unresolved: Tier II-IV, final Challenge/Contract/Shop balancing, additional valuable current-world packages, the deferred Industrial Contract, unresolved shared-machine challenge attribution, the first-Iron route, Lava progression and broader late-game pack progression.

## Fixed pack state

The final starting template is `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt`. Its layout, spawn and ME contents are protected by the repository rules unless TeKatz explicitly requests a revision. The selected spawn is `[8, 4, 5]` facing east; the 1k cell contains eight bread and one wheat seed.

The 0.18.1 integration did **not** modify the starter-island NBT.

`pack/mods.inventory.json` and the mod-list documentation are development snapshots, not a launcher lockfile. Older recorded Forever Skyblock artifacts are historical evidence rather than the repository's current mod version.

## Repository layout

| Path | Purpose |
| --- | --- |
| `mod/` | Standalone Gradle project for the Forever Skyblock mod |
| `pack/` | Pack-owned configuration, data and fixed island files |
| `docs/README.md` | Documentation index and source-of-truth rules |
| `docs/CURRENT_STATE.md` | Current integrated implementation boundary |
| `docs/DESIGN.md` | Agreed project direction and Expert pacing |
| `docs/CHALLENGES.md` | Active challenge catalog and attribution rules |
| `docs/CONTRACTS.md` | Integrated repeatable contracts and transaction rules |
| `docs/WORLD_SHOP.md` | Current-world products, delivery and world upgrades |
| `docs/EARLY_GAME_QOL.md` | Leaves/Bone Meal, Trader unlock and world QoL behavior |
| `docs/FREEZING_CHAMBER.md` | Freezing Chamber, dynamic Condenser UI and Crafting Table gate |
| `docs/K_MENU_DESIGN2.md` | Current K-menu visual/navigation design |
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

The production JAR is written to `mod/build/libs/`. The GitHub Actions workflow also performs a headless NeoForge client mixin smoke test, the dedicated regression GameTests and the full NeoForge 21.1.259 integration suite with the current optional integrations.

## Pack completeness

The fixed island and current world configuration are present. A complete distributable pack still needs the final launcher/export manifest plus any future authored quests, scripts and intentional configuration overrides. See [docs/FILES_TO_IMPORT.md](docs/FILES_TO_IMPORT.md).

Feature work should happen on a bounded working branch. Merging into `main`, publishing releases and changing the protected starting island require TeKatz's explicit request; see [AGENTS.md](AGENTS.md).
