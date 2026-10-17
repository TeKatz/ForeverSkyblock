# Forever Skyblock documentation

This directory contains **integrated-state documentation**, live feature documents and clearly marked historical/development records. Use the map below instead of inferring repository state from an old phase file.

## Start here

1. [CURRENT_STATE.md](CURRENT_STATE.md) — truth for the version currently integrated into `main`.
2. [DESIGN.md](DESIGN.md) — approved project direction and Expert pacing.
3. [FREEZING_CHAMBER.md](FREEZING_CHAMBER.md) — integrated 0.18.1 Freezing Chamber, dynamic Condenser UI and Crafting Table gate.
4. [EARLY_GAME_QOL.md](EARLY_GAME_QOL.md) — integrated early-game Bone Meal loop, Trader unlock and world upgrades.
5. [VALIDATION.md](VALIDATION.md) — detailed automated checks and user-reported in-game validation history.
6. [CHANGELOG.md](CHANGELOG.md) — concise version-level history.

The current integrated baseline is **0.18.1**, merged through PR #13 on October 17, 2026. No release was published by that merge.

## Feature documentation

| Document | Scope |
| --- | --- |
| [CHALLENGES.md](CHALLENGES.md) | Unique Challenge catalog, personal attribution and deferred objectives |
| [CONTRACTS.md](CONTRACTS.md) | Integrated repeatable contracts, rewards and transaction rules |
| [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md) | Integrated Design-2 layout; larger redesign work remains deferred |
| [WORLD_SHOP.md](WORLD_SHOP.md) | Stable world identity, real package delivery and world-wide upgrades |
| [SKILLS.md](SKILLS.md) | Persistent progression storage, skills and refunds |
| [JETPACK_RESEARCH.md](JETPACK_RESEARCH.md) | Iron Jetpacks research gates and automation behavior |
| [ASCENSION.md](ASCENSION.md) | Forever Core, Ascension Chamber and Perk Points |
| [CONDENSER_MIXING.md](CONDENSER_MIXING.md) | Mortar, Stone Meal, Mixing Chamber, locks and JEI transfer |
| [FREEZING_CHAMBER.md](FREEZING_CHAMBER.md) | Freezing Chamber, 32-item Ice process, four Condenser layouts and Crafting Table gate |
| [EARLY_GAME_QOL.md](EARLY_GAME_QOL.md) | Leaves -> Bone Meal, Wandering Trader gate and permanent world upgrades |
| [TOOL_PROGRESSION.md](TOOL_PROGRESSION.md) | Permanent Vanilla tools and Reinforced Netherite progression |
| [START_ISLAND.md](START_ISLAND.md) | Protected starting island and verified starter contents |
| [MODS.md](MODS.md) | Runtime/build version policy and separate Forever repositories |

## Historical / phase records

| Document | Interpretation |
| --- | --- |
| [POLISH_PHASE7.md](POLISH_PHASE7.md) | Historical 0.16.0 stabilization phase, now integrated into the 0.18.1 baseline. |
| [META_PROGRESSION.md](META_PROGRESSION.md) | Architecture/design record; older baseline paragraphs are historical. |
| [PROGRESSION.md](PROGRESSION.md) | Unconfirmed resource/progression proposals. Do not treat proposals as implemented unless CURRENT_STATE says so. |
| [TOOL_RECIPE_HANDOFF.md](TOOL_RECIPE_HANDOFF.md) | Historical implementation handoff; `TOOL_PROGRESSION.md` is the live behavior document. |
| [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md) | Supplied runtime/list evidence, not a final launcher manifest. |
| [FILES_TO_IMPORT.md](FILES_TO_IMPORT.md) | Remaining pack/export inputs that are not yet present or final. |

## Documentation rules

- Integrated status belongs in `CURRENT_STATE.md`.
- Live feature behavior belongs in the corresponding feature document.
- Historical branch/phase notes must be read as historical context and must not override the current integrated baseline.
- `PROGRESSION.md` may contain ideas that are not implemented; verify against `CURRENT_STATE.md` before acting on them.
- Validation claims must distinguish CI/server checks from TeKatz's real client/in-game tests.
- The fixed island and pack runtime rules in `AGENTS.md` remain authoritative.
