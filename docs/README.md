# Forever Skyblock documentation

This directory contains the live integrated-state documentation plus historical/design records. Treat [CURRENT_STATE.md](CURRENT_STATE.md) as the authoritative snapshot of what is currently in `main`, and [PROGRESSION.md](PROGRESSION.md) as the live sequential resource/progression map.

## Current baseline

- **Forever Skyblock mod:** 0.27.0
- **Minecraft:** 1.21.1
- **Java:** 21
- **Current pack/runtime + development target:** NeoForge **21.1.264**
- **Current supplied mod list:** `Modliste(6).txt`, **94 enabled artifact labels**
- **Machine visual overhaul:** integrated through PR #30
- **Resource-refining stack:** integrated through PR #28/#29

The October 22 launcher list reports the current component/artifact state but does not provide a fresh loaded-mod-ID export or a new player-test result. Historical validation records keep the NeoForge versions actually used by those runs.

## Start here

1. [CURRENT_STATE.md](CURRENT_STATE.md) — authoritative current `main` behavior, version and validation boundary.
2. [PROGRESSION.md](PROGRESSION.md) — live sequential material/progression route from Wood through powered refining.
3. [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md) — exact current supplied artifact snapshot and changes from October 21.
4. [MODS.md](MODS.md) — current runtime/build version policy and selected mod versions.
5. [MACHINE_VISUAL_OVERHAUL.md](MACHINE_VISUAL_OVERHAUL.md) — integrated 0.27.0 machine presentation overhaul.
6. [RESOURCE_REFINING.md](RESOURCE_REFINING.md) — renewable Nether/Redstone/Refining Condenser recipes and FE costs.
7. [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md) — early material processing and Garden automation.
8. [NETHER_PROGRESSION.md](NETHER_PROGRESSION.md) — Fortress bootstrap plus renewable Nether loop.
9. [K_MENU_EDITOR.md](K_MENU_EDITOR.md) — K-menu Studio authoring model and safety rules.
10. [VALIDATION.md](VALIDATION.md) — automated and real-player validation history.
11. [CHANGELOG.md](CHANGELOG.md) — version/integration history.

## Current progression spine

`Wood -> Stone Fragments -> Stone Meal / Dirt -> Ice / Water -> Farm -> Mud / Packed Mud -> Raw Iron / Iron -> Condenser Mark II + Garden -> Lava / Obsidian -> Gravel / Sand / renewable Netherrack -> Nether Concentrate -> Mineral Blend -> Redstone -> Refining Condenser -> Lapis / Copper / Osmium / Emerald / Diamond`

## Integrated systems

- Condenser Mark I / II, Mixing and Freezing Chambers
- Garden Chamber Oak/Wheat automation
- Washing Basin and first-Iron route
- Crucible Lava/Obsidian/renewable-Netherrack system
- renewable Nether Concentrate, Mineral Blend and Redstone
- FE-powered Refining Condenser
- Condenser Mark II upgrade cards and output policies
- K-menu and K-menu Studio
- 0.27.0 machine exterior/GUI visual overhaul

## Feature documentation

| Document | Scope |
| --- | --- |
| [CURRENT_STATE.md](CURRENT_STATE.md) | Current integrated version, feature boundary and runtime baseline |
| [PROGRESSION.md](PROGRESSION.md) | Live sequential resource/progression map |
| [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md) | Current supplied launcher artifact labels and delta |
| [MODS.md](MODS.md) | Runtime/build version policy and selected versions |
| [MACHINE_VISUAL_OVERHAUL.md](MACHINE_VISUAL_OVERHAUL.md) | Integrated machine visual system and validation history |
| [RESOURCE_REFINING.md](RESOURCE_REFINING.md) | Renewable Nether materials, Redstone and powered refining |
| [NETHER_PROGRESSION.md](NETHER_PROGRESSION.md) | Fortress bootstrap and renewable Nether loop |
| [MATERIAL_PROCESSING.md](MATERIAL_PROCESSING.md) | Mud, Washing Basin, first Iron, Condensers and Garden |
| [MACHINE_ANIMATIONS.md](MACHINE_ANIMATIONS.md) | Condenser/Garden animation architecture and historical polish |
| [CONDENSER_MIXING.md](CONDENSER_MIXING.md) | Mortar, Stone Meal, Mixing Chamber, locks and JEI transfer |
| [FREEZING_CHAMBER.md](FREEZING_CHAMBER.md) | Freezing Chamber Ice route and Condenser integration |
| [K_MENU_EDITOR.md](K_MENU_EDITOR.md) | K-menu Studio editor workflow and publication safety |
| [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md) | K-menu presentation, Shop graph, profile and statistics |
| [EARLY_GAME_QOL.md](EARLY_GAME_QOL.md) | Bone Meal loop, Wandering Trader gate and world upgrades |
| [CHALLENGES.md](CHALLENGES.md) | Challenge catalog and attribution rules |
| [CONTRACTS.md](CONTRACTS.md) | Repeatable contracts and transaction rules |
| [WORLD_SHOP.md](WORLD_SHOP.md) | Stable world identity, packages and world upgrades |
| [SKILLS.md](SKILLS.md) | Persistent skills/progression storage and refunds |
| [JETPACK_RESEARCH.md](JETPACK_RESEARCH.md) | Iron Jetpacks research gates |
| [ASCENSION.md](ASCENSION.md) | Forever Core, Ascension Chamber and Perk Points |
| [TOOL_PROGRESSION.md](TOOL_PROGRESSION.md) | Permanent Vanilla tool progression |
| [START_ISLAND.md](START_ISLAND.md) | Protected starting island and starter contents |

## Historical / implementation records

Historical phase documents remain useful context but do not override the current-state files above. Earlier NeoForge 21.1.259/21.1.262 validation statements remain tied to the runs that actually used those versions.

## Documentation rules

- `CURRENT_STATE.md` is the source of truth for what is integrated into `main`.
- `PROGRESSION.md` is the source of truth for the current sequential resource spine.
- `RESOURCE_REFINING.md` owns detailed refining numbers.
- Current pack/runtime and development target: **NeoForge 21.1.264**.
- Historical run-specific runtime versions must not be retroactively rewritten.
- Validation claims must distinguish automated CI/server checks from TeKatz's real-player or pack-instance testing.
- The fixed-island and repository working rules in `AGENTS.md` remain authoritative.
