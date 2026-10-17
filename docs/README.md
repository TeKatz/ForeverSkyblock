# Forever Skyblock documentation

This directory contains **integrated-state documentation**, live feature documents and development/history records. Use the map below instead of inferring repository state from an old phase file.

## Start here

1. [CURRENT_STATE.md](CURRENT_STATE.md) — truth for the version currently integrated into `main`.
2. [POLISH_PHASE7.md](POLISH_PHASE7.md) — current 0.16.0 general-polish development branch and its deliberately limited scope.
3. [DESIGN.md](DESIGN.md) — approved project direction and Expert pacing.
4. [VALIDATION.md](VALIDATION.md) — detailed automated checks and user-reported in-game validation history.
5. [CHANGELOG.md](CHANGELOG.md) — concise version-level history.

`CURRENT_STATE.md` intentionally remains the authority for `main`; a development-branch document must not silently redefine the integrated baseline.

## Feature documentation

| Document | Scope |
| --- | --- |
| [CHALLENGES.md](CHALLENGES.md) | Unique Challenge catalog, personal attribution and deferred objectives |
| [CONTRACTS.md](CONTRACTS.md) | Phase 5 repeatable-contract development behavior and transaction rules |
| [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md) | Integrated Design-2 layout; larger redesign work is currently deferred |
| [WORLD_SHOP.md](WORLD_SHOP.md) | Stable world identity and Phase 6 real-package delivery development behavior |
| [SKILLS.md](SKILLS.md) | Persistent progression storage, skills and refunds |
| [JETPACK_RESEARCH.md](JETPACK_RESEARCH.md) | Iron Jetpacks research gates and automation behavior |
| [ASCENSION.md](ASCENSION.md) | Forever Core, Ascension Chamber and Perk Points |
| [CONDENSER_MIXING.md](CONDENSER_MIXING.md) | Mortar, Stone Meal, Mixing Chamber, filters and JEI transfer |
| [TOOL_PROGRESSION.md](TOOL_PROGRESSION.md) | Permanent Vanilla tools and Reinforced Netherite progression |
| [START_ISLAND.md](START_ISLAND.md) | Protected starting island and verified starter contents |
| [MODS.md](MODS.md) | Runtime/build version policy and separate Forever repositories |

## Planning and historical records

| Document | Interpretation |
| --- | --- |
| [META_PROGRESSION.md](META_PROGRESSION.md) | Architecture/design record; older baseline paragraphs are historical. |
| [PROGRESSION.md](PROGRESSION.md) | Unconfirmed resource/progression proposals. Do not treat them as implemented. |
| [TOOL_RECIPE_HANDOFF.md](TOOL_RECIPE_HANDOFF.md) | Historical implementation handoff; `TOOL_PROGRESSION.md` is the live behavior document. |
| [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md) | Supplied runtime/list evidence, not a final launcher manifest. |
| [FILES_TO_IMPORT.md](FILES_TO_IMPORT.md) | Remaining pack/export inputs that are not yet present or final. |

## Documentation rules

- Integrated status belongs in `CURRENT_STATE.md`.
- Development-only state must identify its branch and must not present itself as merged or released.
- Feature behavior belongs in the corresponding feature document.
- Validation claims must distinguish CI/server checks from TeKatz's real client/in-game tests.
- The fixed island and pack runtime rules in `AGENTS.md` remain authoritative.
