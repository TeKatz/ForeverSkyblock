# Forever Skyblock documentation

This directory contains **integrated-state documentation**, live feature documents and clearly marked historical/development records. Use the map below instead of inferring repository state from an old phase file.

## Start here

1. [CURRENT_STATE.md](CURRENT_STATE.md) — truth for the version currently integrated into `main`.
2. [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md) — integrated 0.24.0 material-processing and Garden-automation path.
3. [DESIGN.md](DESIGN.md) — approved project direction and Expert pacing.
4. [FREEZING_CHAMBER.md](FREEZING_CHAMBER.md) — Freezing Chamber, dynamic Condenser UI and Crafting Table gate.
5. [EARLY_GAME_QOL.md](EARLY_GAME_QOL.md) — early-game Bone Meal loop, Trader unlock and world upgrades.
6. [VALIDATION.md](VALIDATION.md) — detailed automated checks and user-reported in-game validation history.
7. [CHANGELOG.md](CHANGELOG.md) — concise version-level history.

The integrated baseline is **0.24.0**, merged through [PR #16](https://github.com/TeKatz/ForeverSkyblock/pull/16) on October 19, 2026. This includes the complete 0.21–0.24 stack. The fixed starter-island NBT is unchanged; no release is implied by the merge.

## Integrated 0.21–0.24 updates

- [Machine animations — 0.21.1](MACHINE_ANIMATIONS.md): job-specific Condenser motion, synchronized Garden growth, rebuilt greenhouse and polished tree/workpiece transitions.
- [Garden crops and pack fixes — 0.22.0](MACHINE_ANIMATIONS.md#0220-garden-crops-and-pack-fixes): Wheat crop template, centered labels/items, Iron-priced Wooden Hopper and complete Trader-caravan gate.
- [Separate Garden harvest ports — 0.22.1](MACHINE_ANIMATIONS.md#0221-separate-garden-harvest-ports): two unfiltered hoppers separate primary/secondary harvest; the Condenser GUI explains both ports.
- [Iron balancing and Packed Mud — 0.23.0](MATERIAL_PROCESSING.md#0230-balancing-and-packed-mud): six-second Logs, higher Iron costs, Packed Mud mixing and centered Garden badges.
- [K-menu overhaul — 0.24.0](K_MENU_DESIGN2.md): round Shop nodes, readable details, compact profile, subtle motion and real statistics. Island Level uses shared world milestones from this update without extra SP.

## Development branch

[K-menu Studio](K_MENU_EDITOR.md) describes the unmerged `feature/kmenu-admin-editor` 0.25.0 implementation, authoring workflow and validation boundary. The integrated main branch includes the approved 0.24.1 header artwork through PR #17.

## Feature documentation

| Document | Scope |
| --- | --- |
| [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md) | Mud, Washing Basin, first Raw Iron, craftable Mark I, Iron-gated Mark II and Garden Chamber wood automation |
| [CHALLENGES.md](CHALLENGES.md) | Unique Challenge catalog, personal attribution and deferred objectives |
| [CONTRACTS.md](CONTRACTS.md) | Integrated repeatable contracts, rewards and transaction rules |
| [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md) | Integrated 0.24.0 overhaul: Shop graph, details, profile, motion and authoritative statistics |
| [WORLD_SHOP.md](WORLD_SHOP.md) | Stable world identity, real package delivery and world-wide upgrades |
| [SKILLS.md](SKILLS.md) | Persistent progression storage, skills and refunds |
| [JETPACK_RESEARCH.md](JETPACK_RESEARCH.md) | Iron Jetpacks research gates and automation behavior |
| [ASCENSION.md](ASCENSION.md) | Forever Core, Ascension Chamber and Perk Points |
| [CONDENSER_MIXING.md](CONDENSER_MIXING.md) | Mortar, Stone Meal, Mixing Chamber, locks and JEI transfer |
| [FREEZING_CHAMBER.md](FREEZING_CHAMBER.md) | Freezing Chamber, 32-item Ice process, dynamic Condenser layouts and Crafting Table gate |
| [EARLY_GAME_QOL.md](EARLY_GAME_QOL.md) | Leaves -> Bone Meal, Wandering Trader gate and permanent world upgrades |
| [TOOL_PROGRESSION.md](TOOL_PROGRESSION.md) | Permanent Vanilla tools and Reinforced Netherite progression |
| [START_ISLAND.md](START_ISLAND.md) | Protected starting island and verified starter contents |
| [MODS.md](MODS.md) | Runtime/build version policy and current supplied mod-list evidence |

## Current early material arc

The live early-game route now reaches the first metal and closes the primitive wood loop:

`Ice / Water -> Farm -> Mud -> Packed Mud -> Mud Bricks -> Washing Basin -> Raw Iron -> Iron -> Condenser Mark II -> Garden Chamber`

Important integrated outcomes:

- Washing Basin is the deterministic first-Iron route.
- Mark II removes the manual Mortar dependency for Stone Meal.
- Mark II + Mixing uses Wheat Seeds + Bone Meal for renewable Oak Saplings.
- Garden Chamber is an exclusive Mark-II mode that retains its specimen and produces Oak Logs + Leaves or Wheat + Seeds every 16 seconds.
- Separate bottom ports on MK II and Garden Chamber route the primary and secondary harvest.
- Both tiers with Mixing Chamber can automate Mud + Wheat -> Packed Mud.
- Garden mode has priority over Mixing/Freezing but preserves their stored state for resume after detaching it.
- the Garden Chamber is player-facing simply as **Garden Chamber**; its compatibility-preserved internal registry ID remains `garden_chamber_mark_i`.

## Historical / phase records

| Document | Interpretation |
| --- | --- |
| [POLISH_PHASE7.md](POLISH_PHASE7.md) | Historical 0.16.0 stabilization phase, now integrated. |
| [META_PROGRESSION.md](META_PROGRESSION.md) | Architecture/design record; older baseline paragraphs are historical. |
| [PROGRESSION.md](PROGRESSION.md) | Unconfirmed resource/progression proposals. Do not treat proposals as implemented unless CURRENT_STATE says so. |
| [TOOL_RECIPE_HANDOFF.md](TOOL_RECIPE_HANDOFF.md) | Historical implementation handoff; `TOOL_PROGRESSION.md` is the live behavior document. |
| [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md) | Supplied runtime/list evidence, not a final launcher manifest. |
| [FILES_TO_IMPORT.md](FILES_TO_IMPORT.md) | Remaining pack/export inputs that are not yet present or final. |

## Documentation rules

- Integrated status belongs in `CURRENT_STATE.md`.
- Development-only behavior must identify its branch and must not silently redefine the current `main` baseline.
- Live feature behavior belongs in the corresponding feature document.
- Historical branch/phase notes must be read as historical context and must not override the current integrated baseline.
- `PROGRESSION.md` may contain ideas that are not implemented; verify against `CURRENT_STATE.md` before acting on them.
- Validation claims must distinguish CI/server checks from TeKatz's real client/in-game tests.
- The fixed island and pack runtime rules in `AGENTS.md` remain authoritative.
