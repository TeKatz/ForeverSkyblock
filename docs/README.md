# Forever Skyblock documentation

This directory contains the live integrated-state documentation plus historical/design records. Treat [CURRENT_STATE.md](CURRENT_STATE.md) as the authoritative snapshot of what is currently in `main`.

## Current baseline

The integrated mod baseline is **0.26.4** on Minecraft **1.21.1** and Java **21**. The current installed pack runtime is **NeoForge 21.1.262**.

The current supplied `Modliste(5).txt` contains **87 enabled artifact labels**. TeKatz reported testing this exact current modpack state on NeoForge 21.1.262 with **no errors observed**. This is user-reported pack validation; historical CI/runtime entries that name NeoForge 21.1.259 remain valid for those earlier runs.

The exact current list and version changes are documented in [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md) and [MODS.md](MODS.md).

The current stack includes:

- the 0.21–0.24 machine animation, Garden, balancing and K-menu work;
- the integrated **K-menu Studio** from the 0.25.x development series;
- the **Crucible Lava/Obsidian progression**;
- **Condenser Mark II automation upgrades** and per-output overflow policies;
- the current Nether Fortress material-processing work present on `main`;
- GitHub Actions usage optimization that removes duplicate feature-branch push/PR runs while retaining the established validation chain.

The fixed starter-island NBT remains protected and unchanged by these integrations.

## Start here

1. [CURRENT_STATE.md](CURRENT_STATE.md) — authoritative current `main` behavior, version and pack-runtime baseline.
2. [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md) — exact current supplied artifact labels and differences from the previous list.
3. [MODS.md](MODS.md) — runtime/build version policy and mod-list evidence boundary.
4. [K_MENU_EDITOR.md](K_MENU_EDITOR.md) — K-menu Studio authoring model and safety rules. Older wording about development branches is historical context; the Studio is now integrated.
5. [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md) — early material-processing and Garden automation history/details.
6. [DESIGN.md](DESIGN.md) — approved project direction and Expert pacing.
7. [FREEZING_CHAMBER.md](FREEZING_CHAMBER.md) — original Freezing Chamber / Ice behavior and Condenser integration.
8. [VALIDATION.md](VALIDATION.md) — automated and real-player validation history.
9. [CHANGELOG.md](CHANGELOG.md) — version-level history; older “not merged” labels describe the state at the time those entries were written.

## Integrated progression highlights

### Early material chain

`Wood → Stone Fragments → Stone Meal / Dirt → Ice / Water → Farm → Mud / Packed Mud → Washing Basin → Raw Iron → Iron → Condenser Mark II + Garden Chamber → Crucible → Lava / Obsidian`

### Crucible

The integrated Crucible has a **4,000 mB** Lava buffer and heat progression through **Torch, Campfire, Lava and Mekanism Superheating Element**.

Current rates:

- **8 Cobblestone → 1,000 mB Lava**
- **40 Stone Fragments → 1,000 mB Lava**

It detects adjacent Fluid Tanks and Freezing Chambers, can route Lava or produce Obsidian, exposes hopper/cable automation, has JEI entries, and reports heat tier through Jade. The Mekanism Basic Fluid Tank recipe is intentionally redstone-free.

### Condenser Mark II upgrades

Mark II has **3 upgrade capacity** and supports Processing Speed, Garden Accelerator, Buffer Expansion and Batch Processing cards. Outputs support independent **Block / Void Excess / Void All** policies, including separate Garden primary/secondary policies.

### K-menu Studio

The Studio is integrated into `main`. Authorized users can edit supported layouts, quests/chapters, Shop nodes/packages and Contracts; keep drafts; preview changes; validate/publish server-owned documents; reconcile conflicts; restore backups; and export configuration.

## Feature documentation

| Document | Scope |
| --- | --- |
| [CURRENT_STATE.md](CURRENT_STATE.md) | Current integrated version, feature boundary and validation summary |
| [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md) | Current supplied mod artifacts and version changes |
| [MODS.md](MODS.md) | Runtime/build version policy and mod-list evidence |
| [K_MENU_EDITOR.md](K_MENU_EDITOR.md) | K-menu Studio editor workflow, publication safety and compatibility |
| [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md) | K-menu presentation, Shop graph, profile and statistics foundation |
| [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md) | Mud, Washing Basin, first Iron, Condensers and Garden automation |
| [MACHINE_ANIMATIONS.md](MACHINE_ANIMATIONS.md) | Condenser/Garden animation architecture and historical polish |
| [CONDENSER_MIXING.md](CONDENSER_MIXING.md) | Mortar, Stone Meal, Mixing Chamber, locks and JEI transfer |
| [FREEZING_CHAMBER.md](FREEZING_CHAMBER.md) | Freezing Chamber Ice route and Condenser integration |
| [EARLY_GAME_QOL.md](EARLY_GAME_QOL.md) | Bone Meal loop, Wandering Trader gate and world upgrades |
| [CHALLENGES.md](CHALLENGES.md) | Challenge catalog and attribution rules |
| [CONTRACTS.md](CONTRACTS.md) | Repeatable contracts and transaction rules |
| [WORLD_SHOP.md](WORLD_SHOP.md) | Stable world identity, package delivery and world-wide upgrades |
| [SKILLS.md](SKILLS.md) | Persistent skills/progression storage and refunds |
| [JETPACK_RESEARCH.md](JETPACK_RESEARCH.md) | Iron Jetpacks research gates |
| [ASCENSION.md](ASCENSION.md) | Forever Core, Ascension Chamber and Perk Points |
| [TOOL_PROGRESSION.md](TOOL_PROGRESSION.md) | Permanent Vanilla tool progression |
| [START_ISLAND.md](START_ISLAND.md) | Protected starting island and starter contents |

## Historical / proposal records

Some documents preserve the state or intent of earlier phases. They are useful context, but do not override the current integrated snapshot.

| Document | Interpretation |
| --- | --- |
| [POLISH_PHASE7.md](POLISH_PHASE7.md) | Historical stabilization phase |
| [META_PROGRESSION.md](META_PROGRESSION.md) | Architecture/design history; older baseline paragraphs may be stale |
| [PROGRESSION.md](PROGRESSION.md) | Proposals and planning; verify implementation against CURRENT_STATE |
| [TOOL_RECIPE_HANDOFF.md](TOOL_RECIPE_HANDOFF.md) | Historical implementation handoff |
| [FILES_TO_IMPORT.md](FILES_TO_IMPORT.md) | Remaining external/export inputs |

## Documentation rules

- `CURRENT_STATE.md` is the source of truth for integrated status.
- The current pack runtime is **NeoForge 21.1.262**; older 21.1.259 references inside historical validation entries belong to those specific runs.
- Historical version/branch statements describe the state at the time they were written and must not override current `main`.
- `PROGRESSION.md` may contain unimplemented ideas.
- Validation claims must distinguish automated CI/server checks from TeKatz's real-player or pack-instance testing.
- The fixed-island and repository working rules in `AGENTS.md` remain authoritative.
