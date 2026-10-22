# Forever Skyblock documentation

This directory contains the live integrated-state documentation plus historical/design records. Treat [CURRENT_STATE.md](CURRENT_STATE.md) as the authoritative snapshot of what is currently in `main`, and [PROGRESSION.md](PROGRESSION.md) as the live sequential resource/progression map.

## Current baseline

The integrated mod baseline is **0.26.4** on Minecraft **1.21.1** and Java **21**. The current installed pack runtime is **NeoForge 21.1.262**.

The current supplied `Modliste(5).txt` contains **87 enabled artifact labels**. TeKatz reported testing that October 21 modpack state on NeoForge 21.1.262 with **no errors observed**. This predates the October 22 resource-refining merge and is not presented as post-merge gameplay acceptance. Historical CI/runtime entries that name NeoForge 21.1.259 remain valid for the runs that actually used it.

The exact supplied list and version changes are documented in [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md) and [MODS.md](MODS.md).

The current stack includes:

- the machine animation, Garden, balancing and rebuilt K-menu work;
- the integrated **K-menu Studio**;
- **Condenser Mark II automation upgrades** and per-output overflow policies;
- the **Crucible Lava/Obsidian system**, including renewable Netherrack;
- renewable **Cobblestone -> Gravel -> Sand** processing;
- Nether Brick Dust -> Nether Concentrate processing;
- **Mineral Blend -> renewable Redstone**;
- the FE-powered **Refining Condenser** with Lapis, Copper, Osmium, Emerald and Diamond refining;
- progression-aligned Mekanism Basic Logistical Transporter and Iron Furnace recipes.

PR #29 was folded into PR #28 and PR #28 was merged to `main` on October 22, 2026. The fixed starter-island NBT remains protected and unchanged.

## Start here

1. [CURRENT_STATE.md](CURRENT_STATE.md) — authoritative current `main` behavior, version and validation boundary.
2. [PROGRESSION.md](PROGRESSION.md) — live sequential material/progression route from Wood through powered refining.
3. [RESOURCE_REFINING.md](RESOURCE_REFINING.md) — current renewable Nether/Redstone/Refining Condenser recipes, FE costs and integration notes.
4. [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md) — early material-processing and Garden automation foundation.
5. [NETHER_PROGRESSION.md](NETHER_PROGRESSION.md) — Fortress bootstrap plus renewable Nether material loop.
6. [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md) — exact current supplied artifact labels and differences from the previous list.
7. [MODS.md](MODS.md) — runtime/build version policy and mod-list evidence boundary.
8. [K_MENU_EDITOR.md](K_MENU_EDITOR.md) — K-menu Studio authoring model and safety rules.
9. [VALIDATION.md](VALIDATION.md) — automated and real-player validation history.
10. [CHANGELOG.md](CHANGELOG.md) — version/integration history.

## Integrated progression highlights

### Current material spine

`Wood -> Stone Fragments -> Stone Meal / Dirt -> Ice / Water -> Farm -> Mud / Packed Mud -> Raw Iron / Iron -> Condenser Mark II + Garden -> Lava / Obsidian -> Gravel / Sand / renewable Netherrack -> Nether Concentrate -> Mineral Blend -> Redstone -> Refining Condenser -> Lapis / Copper / Osmium / Emerald / Diamond`

### Crucible

The Crucible has a **4,000 mB** Lava buffer and heat progression through **Torch, Campfire, Lava and Mekanism Superheating Element**.

Current Lava rates:

- **8 Cobblestone -> 1,000 mB Lava**;
- **40 Stone Fragments -> 100 mB Lava**.

Additional route:

- **1 Gravel + 250 mB stored Lava -> 1 Netherrack / 4 seconds**.

It detects adjacent Fluid Tanks and Freezing Chambers, can route Lava or produce Obsidian, auto-pushes produced output items into adjacent compatible inventories and exposes real fluid amounts in JEI.

### Mineral Blend and Redstone

- Condenser Mark II + Mixing: **1 Nether Concentrate + 1 Stone Meal -> 2 Mineral Blend / 8 seconds**.
- Washing Basin: **1 Mineral Blend -> 8 Redstone / 6 seconds**.

### Refining Condenser

Crafting requires **4 Iron Blocks + 2 Redstone + 2 Obsidian + 1 Blast Furnace**. The machine stores **100,000 FE**, receives up to **4,000 FE/t** and refines the new renewable material chain into Lapis, Copper, Osmium, Emerald and Diamond. Detailed input, duration and FE values are in [RESOURCE_REFINING.md](RESOURCE_REFINING.md).

### Condenser Mark II upgrades

Mark II retains **3 upgrade capacity** and supports Processing Speed, Garden Accelerator, Buffer Expansion and Batch Processing cards. Outputs support independent **Block / Void Excess / Void All** policies, including separate Garden primary/secondary policies.

### K-menu Studio

The Studio is integrated into `main`. Authorized users can edit supported layouts, quests/chapters, Shop nodes/packages and Contracts; keep drafts; preview changes; validate/publish server-owned documents; reconcile conflicts; restore backups; and export configuration.

## Feature documentation

| Document | Scope |
| --- | --- |
| [CURRENT_STATE.md](CURRENT_STATE.md) | Current integrated version, feature boundary and validation summary |
| [PROGRESSION.md](PROGRESSION.md) | Live sequential resource/progression map |
| [RESOURCE_REFINING.md](RESOURCE_REFINING.md) | Renewable Nether materials, Mineral Blend, Redstone and Refining Condenser |
| [NETHER_PROGRESSION.md](NETHER_PROGRESSION.md) | Fortress bootstrap and renewable Nether loop |
| [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md) | Mud, Washing Basin, first Iron, Condensers and Garden automation |
| [MACHINE_ANIMATIONS.md](MACHINE_ANIMATIONS.md) | Condenser/Garden animation architecture and historical polish |
| [CONDENSER_MIXING.md](CONDENSER_MIXING.md) | Mortar, Stone Meal, Mixing Chamber, locks and JEI transfer |
| [FREEZING_CHAMBER.md](FREEZING_CHAMBER.md) | Freezing Chamber Ice route and Condenser integration |
| [K_MENU_EDITOR.md](K_MENU_EDITOR.md) | K-menu Studio editor workflow, publication safety and compatibility |
| [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md) | K-menu presentation, Shop graph, profile and statistics foundation |
| [EARLY_GAME_QOL.md](EARLY_GAME_QOL.md) | Bone Meal loop, Wandering Trader gate and world upgrades |
| [CHALLENGES.md](CHALLENGES.md) | Challenge catalog and attribution rules |
| [CONTRACTS.md](CONTRACTS.md) | Repeatable contracts and transaction rules |
| [WORLD_SHOP.md](WORLD_SHOP.md) | Stable world identity, package delivery and world-wide upgrades |
| [SKILLS.md](SKILLS.md) | Persistent skills/progression storage and refunds |
| [JETPACK_RESEARCH.md](JETPACK_RESEARCH.md) | Iron Jetpacks research gates |
| [ASCENSION.md](ASCENSION.md) | Forever Core, Ascension Chamber and Perk Points |
| [TOOL_PROGRESSION.md](TOOL_PROGRESSION.md) | Permanent Vanilla tool progression |
| [START_ISLAND.md](START_ISLAND.md) | Protected starting island and starter contents |
| [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md) | Current supplied mod artifacts and version changes |
| [MODS.md](MODS.md) | Runtime/build version policy and mod-list evidence |

## Historical / implementation records

These documents preserve earlier phases or implementation handoffs. They are useful context, but do not override the live state/progression documents above.

| Document | Interpretation |
| --- | --- |
| [POLISH_PHASE7.md](POLISH_PHASE7.md) | Historical stabilization phase |
| [META_PROGRESSION.md](META_PROGRESSION.md) | Architecture/design history; older baseline paragraphs may be stale |
| [TOOL_RECIPE_HANDOFF.md](TOOL_RECIPE_HANDOFF.md) | Historical implementation handoff |
| [FILES_TO_IMPORT.md](FILES_TO_IMPORT.md) | Remaining external/export inputs |
| `HOTFIX_PR28_*` | October 22 PR #28/#29 validation history; no longer an outstanding branch plan |

## Documentation rules

- `CURRENT_STATE.md` is the source of truth for what is integrated into `main`.
- `PROGRESSION.md` is the source of truth for the current sequential resource spine; unimplemented future ideas must be explicitly marked as future/open.
- `RESOURCE_REFINING.md` owns the detailed current refining numbers.
- The current installed pack runtime is **NeoForge 21.1.262**; older 21.1.259 references inside validation entries belong to those specific automated runs.
- Historical branch/version statements describe the state at the time they were written and must not override current `main`.
- Validation claims must distinguish automated CI/server checks from TeKatz's real-player or pack-instance testing.
- The fixed-island and repository working rules in `AGENTS.md` remain authoritative.
