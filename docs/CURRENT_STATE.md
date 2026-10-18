# Current integrated state

Updated October 17, 2026 after the authorized integration of the 0.14.0-0.18.1 development stack into `main` through PR #13.

## Baseline

| Item | Current value |
| --- | --- |
| Mod version | **0.18.1** |
| Minecraft | **1.21.1** |
| Java | **21** |
| Pack runtime | **NeoForge 21.1.259** |
| Default build baseline | NeoForge 21.1.249 |
| Profile schema | **dynamic 4 / 5 / 6** |
| Progression protocol | **skills-8** |
| Main feature integration commit | `b895e26da9087393d883ec8efa2e083e1606f580` |
| Last pre-merge feature head | `97715f06391339141032c5f0c859b5925f99539d` |
| Verified pre-merge CI | GitHub Actions **#195**, success |
| Release | Not published by this merge |

## Integrated gameplay and UI

`main` now contains the complete stacked development line through 0.18.1:

- **0.14.0 repeatable contracts:** Dragon Reborn, Wither Hunter, Singularity, Nitro and Antimatter renewable SP paths with replay protection and durable resource turn-in intents.
- **0.15.0 real current-world delivery:** Tier-I Advanced AE2 Starter Package for 12 SP, delivered through a receipt-bound Forever Supply Cache with READY/DISPATCHING/DELIVERED/OPENING/OPENED recovery.
- **0.16.0 stability/polish:** stricter request validation, corrected real-product confirmation/claim behavior, Supply Cache capacity fixes and expanded regression coverage.
- **0.17.x early-game/world QoL:** 8 Leaves -> 1 Bone Meal, save-wide Wandering Trader unlock after first real-player Emerald pickup, and permanent world-wide No Phantoms / No More Rain / Permanent Day upgrades.
- **0.17.1 emergency hotfix:** corrected the premium shop mixin target and helper-name collisions; CI now includes a headless NeoForge client mixin smoke test.
- **0.18.0-0.18.1 Freezing Chamber / workbench gate:** passive Freezing Chamber Mark I, lock-aware any-item freezing, four dynamic Condenser layouts, dedicated JEI Freezing category, Stone Meal-gated Crafting Table recipe and the final GUI/model/tooltip/click-target cleanup.

The earlier Condenser, Mixing Chamber, permanent-tool, Jetpack research, Ascension/Perk, challenge, K-menu and stable world-identity systems remain integrated.

## Active progression boundary

- **20 unique challenges / 58 SP** are active.
- Permanent progression currently reaches **Tier 0 — Foundations** and **Tier I — Conqueror**.
- Five repeatable contracts are active after Tier I:
  - Dragon Reborn: one qualifying respawned Dragon -> 1 SP;
  - Wither Hunter: three additional Withers -> 1 SP;
  - 4 AE2 Singularities -> 1 SP;
  - 32 Powah Nitro Crystals -> 1 SP;
  - 4 Mekanism Antimatter Pellets -> 3 SP.
- Iron Jetpacks research remains 5 / 10 / 20 / 40 SP, with Creative also requiring 1 PP.
- Player-profile persistence remains dynamic: untouched/base profiles can remain schema 4, contract state raises the profile to schema 5, and real delivery state raises it to schema 6.
- Current-world purchases use a stable hidden world UUID and fail closed on ambiguous identity state.
- The active real current-world package is **Advanced AE2 Starter Package**: Tier I, 12 SP, once per player/stable-world identity, delivered through a unique Supply Cache.
- Tier-0 world upgrades are active at 1 / 2 / 3 SP: No Phantoms, No More Rain and Permanent Day. Once activated, their effects are world-wide and do not charge other players again.
- The Condenser can process **8 Leaves -> 1 Bone Meal** without a Mixing Chamber.
- The **Freezing Chamber Mark I** attaches directly to the Condenser and consumes any unlocked/allowed item toward an independent 32-item counter; each completed counter creates 1 Ice.
- The normal Vanilla Crafting Table now requires four Planks plus one Stone Meal in a plus-shaped 3x3 recipe, keeping the starter ME Crafting Terminal as the first 3x3 crafting workstation.

## Freezing Chamber and Condenser UI

The Condenser keeps stable machine slot IDs and supports four live module states:

1. base Condenser;
2. + Mixing Chamber;
3. + Freezing Chamber;
4. + Mixing Chamber and Freezing Chamber.

Main, Mixing and Freezing inputs all use the same persistent lock system. Inactive module slots are hidden/disabled and are not exposed to automation. Removing the Freezing Chamber preserves real input/output, partial 0-31 progress and its lock.

The 0.18.1 screen pass increases vertical spacing, separates Condensing and Freezing process areas, avoids lock-tooltip overlap and removes the old hidden recipe-list click target between the main input/output slots. JEI now has a dedicated Freezing category using the same presentation language as Condensing.

## Explicitly not active / not finalized

Do not infer implementation from planning documents for any of the following:

- Tier II-IV;
- final Challenge/SP balancing;
- final repeatable-contract quantities/rewards beyond the currently integrated test values;
- additional valuable current-world shop packages;
- the deferred Industrial Contract;
- deferred shared-machine/multiblock objectives whose personal attribution is unresolved;
- a finalized first-Iron route;
- a finalized Lava progression route;
- final late-game pack progression or final K-menu/meta-progression balance;
- fabricated Playtime or Island Level values in the K-menu header.

## Pack state

The fixed starting island remains `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` with spawn `[8, 4, 5]` facing east and the verified eight-bread/one-seed 1k cell. PR #13 did not modify the starter-island NBT.

The current supplied October 18 development list explicitly includes Wooden Shears **3.2.3.0** and Wooden Hopper **3.2.1.0**. These remain third-party pack dependencies; their sources and JARs are not committed here. Pack mod-inventory/list files remain provisional evidence rather than the final launcher/export lockfile.

## Validation boundary

The final 0.18.1 feature head `97715f06391339141032c5f0c859b5925f99539d` passed GitHub Actions run #195 before merge. The workflow builds with Java 21, performs a headless NeoForge client mixin smoke test, runs the dedicated regression GameTests and runs the full NeoForge 21.1.259 integration suite with the existing optional integrations.

Headless CI does not prove rendered client pixels. TeKatz tested the Freezing/Condenser work in-game, reported the 0.18.0 z-fighting, cramped layout, missing Ice JEI entry, overlapping lock tooltips and hidden recipe-click issue, then confirmed the corrected 0.18.1 build as a good baseline to continue from. That is user-reported client acceptance separate from CI.

See [VALIDATION.md](VALIDATION.md) for detailed historical checks, [FREEZING_CHAMBER.md](FREEZING_CHAMBER.md) for the current machine behavior and [EARLY_GAME_QOL.md](EARLY_GAME_QOL.md) for the active early-game/world QoL systems.
