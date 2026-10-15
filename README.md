# Forever Skyblock

A standalone **Expert Skyblock modpack for Minecraft 1.21.1 / NeoForge** by TeKatz.

Players start on a small island with a tree, an operational AE2 network and the custom **Condenser Mark I**. TeKatz builds the quest book manually using FTB Quests. This repository contains the mod source, available pack files and authoritative project documentation.

## Current status

- Development mod **0.6.1**: corrected chamber geometry/neighbor visibility, server-validated JEI recipe transfer with disabled unavailable recipes and suppressed Vanilla introduction hints. Includes the 0.6.0 features: reusable Mortar, Stone Meal, adjacent Mixing Chamber Mark I, two-input dirt processing and persistent ghost filters for inventory/JEI/automation. Preserves the 0.5.0 features: four ordered Iron Jetpacks research stages, optional runtime JEI hiding, server crafting/flight restrictions and a temporary +10 SP test button. Includes the previous features: persistent cross-world skill points and purchases, a remappable K shortcut, a card-based Skills/Tasks menu with confirmed skill refunds, a clickable green chat reward and a +1 block reach test skill. The first fragment-to-cobblestone craft awards one point per profile. Also includes the condenser features from 0.2.1: Condenser Mark I turns logs into four Stone Fragments or planks into one, without power. Includes an animated GUI, hopper/pipe support, a custom fragment texture, a 2×2 cobblestone recipe and optional JEI integration.
- TeKatz confirmed 0.4.0 works in game and the revised Skills GUI is clearer. The earlier milestone, purchase and active skill were also tested successfully across two worlds. See [validation status](docs/VALIDATION.md) for the scope of these reports.
- TeKatz successfully tested the starting world, spawn and placed machines in game using mod 0.1.0.
- The island has a tree, wooden equipment platform and open workspace. The latest ME inventory contains bread and one seed; spare saplings and dirt were removed. The original chest is no longer placed.
- The fixed starting template is `forever_me_start.nbt`, with spawn `[8, 4, 5]` facing east. The actual NBT is included; its 1k cell stores eight bread and one wheat seed.
- Actual Skyblock Builder and Default World Type configuration files have been imported from the instance. The world-type restriction selects only Skyblock.
- Forever Recipe Editor **1.4.3-skyblock.1** is maintained in its own repository on branch `skyblock` and was built against **JEI 19.64.0.454**.

The fixed island and actual world configuration are now included. The current runtime is **NeoForge 21.1.259**; [the observed mod list](docs/MODLIST_SNAPSHOT.md) is an experimental snapshot that TeKatz will update periodically. A complete distributable export still needs a launcher manifest and any future recipes, quests, scripts and other required overrides. See [remaining files](docs/FILES_TO_IMPORT.md).

## Repository layout

| Path | Contents |
| --- | --- |
| `mod/` | Complete standalone Gradle project for the Forever Skyblock mod |
| `pack/` | Files to copy into the modpack instance directory |
| `docs/SKILLS.md` | Persistent skills, scope, controls, storage and test steps |
| `docs/DESIGN.md` | Agreed direction and Expert pacing |
| `docs/PROGRESSION.md` | Unconfirmed proposals for early material progression |
| `docs/START_ISLAND.md` | Starting island, tested snapshot and planned changes |
| `docs/MODS.md` | Runtime/build versions, separate repositories and snapshot policy |
| `docs/MODLIST_SNAPSHOT.md` | Current enabled artifact list and historical runtime snapshot |
| `docs/VALIDATION.md` | Completed checks and reproducibility limits |
| `docs/CHANGELOG.md` | Project changes |

## Build the mod

Install Java 21, then run:

```sh
cd mod
./gradlew build
```

On Windows, use `gradlew.bat build`. The JAR is written to `mod/build/libs/`. A GitHub Actions workflow also builds the mod with Java 21 and provides the JAR as a build artifact.

## Use the pack files

Copy the **contents** of `pack/` into the instance directory with the dependencies matching the current snapshot. Consult [FILES_TO_IMPORT.md](docs/FILES_TO_IMPORT.md) before preparing a complete distribution. `mods.inventory.json` is an inventory, not a launcher manifest. Third-party mods will be included through the launcher and distribution platform selected later.

The initial import into `main` did not introduce new gameplay. Further features and balancing changes use working branches; see [AGENTS.md](AGENTS.md).

The current source includes the 0.5.0 Iron Jetpacks research system, 0.6.0 condenser mixing and the 0.6.1 chamber-rendering, JEI-transfer and Vanilla-tutorial updates; see [JETPACK_RESEARCH.md](docs/JETPACK_RESEARCH.md) and [CONDENSER_MIXING.md](docs/CONDENSER_MIXING.md). The current provisional inventory comes from TeKatz's 80-entry `Modliste.txt`, including Forever Skyblock 0.6.1; the earlier runtime log snapshot is archived separately.
