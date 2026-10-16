# Forever Skyblock documentation

This directory contains both **live feature documentation** and **historical design/validation records**. Use the map below instead of inferring the current repository state from an old phase document.

## Start here

1. [CURRENT_STATE.md](CURRENT_STATE.md) — current integrated version, active systems, deferred systems and the validation boundary.
2. [DESIGN.md](DESIGN.md) — approved project direction and Expert pacing.
3. [VALIDATION.md](VALIDATION.md) — detailed automated checks and user-reported in-game validation history.
4. [CHANGELOG.md](CHANGELOG.md) — concise version-level history.

If a historical branch statement conflicts with `CURRENT_STATE.md`, the current-state document wins.

## Active feature documentation

| Document | Scope |
| --- | --- |
| [CHALLENGES.md](CHALLENGES.md) | Active 20-challenge / 58-SP catalog, personal attribution and deferred objectives |
| [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md) | Integrated 0.13.4 K-menu layout and visual rules |
| [WORLD_SHOP.md](WORLD_SHOP.md) | Stable world identity, schema-4 claims/receipts and crash-safe test purchase |
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
| [META_PROGRESSION.md](META_PROGRESSION.md) | Architecture/design record. Its baseline paragraphs intentionally describe the repository before phases 1-4 were integrated. |
| [PROGRESSION.md](PROGRESSION.md) | Unconfirmed resource/progression proposals. Do not treat them as implemented. |
| [TOOL_RECIPE_HANDOFF.md](TOOL_RECIPE_HANDOFF.md) | Historical implementation handoff retained for traceability; `TOOL_PROGRESSION.md` is the live behavior document. |
| [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md) | Supplied runtime/list evidence, not a final launcher manifest and not the current Forever Skyblock mod version. |
| [FILES_TO_IMPORT.md](FILES_TO_IMPORT.md) | Remaining pack/export inputs that are not yet present or final. |

## Documentation rules

- Current status belongs in `CURRENT_STATE.md`; do not create another competing status/handoff file.
- Feature behavior belongs in the corresponding feature document.
- Validation claims must distinguish CI/server checks from TeKatz's real client/in-game tests.
- Old development-branch context may remain in architecture/history documents, but active feature docs must be normalized after merge.
- The fixed island and pack runtime rules in `AGENTS.md` remain authoritative.
