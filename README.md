# Forever Skyblock

An **Expert Skyblock modpack for Minecraft 1.21.1 / NeoForge**, created by TeKatz.

Start on a fixed island with an oak tree, an operational compact AE2 network and Condenser Mark I. TeKatz authors the FTB Quests manually. Resource progression begins with wood conversion rather than sieves.

## Project status

This documentation records the current project state across branches as of October 15, 2026.

| Component | Current state |
| --- | --- |
| Source currently on `main` | Mod **0.1.1**, the original placeable condenser block |
| Latest delivered development mod | **0.2.1**, functional condenser, Stone Fragments, GUI, automation and JEI integration |
| Implementation and actual pack files | [PR #2](https://github.com/TeKatz/ForeverSkyblock/pull/2), awaiting merge |
| Pack runtime | **Minecraft 1.21.1 / NeoForge 21.1.259** |
| Fixed starting island | `forever_me_start.nbt`, spawn `[8, 4, 5]` facing east |
| Saved ME contents | One 1k item cell containing **8 bread and 1 wheat seed** |
| Installed mods | [Experimental runtime snapshot](docs/MODLIST_SNAPSHOT.md), updated periodically |

The documentation update does not merge the implementation or pack files. The actual supplied island, complete world configuration, current inventory and 0.2.1 source are available in the [development snapshot](https://github.com/TeKatz/ForeverSkyblock/tree/8d9f4136f6427893aeac0fc8f47857d19646e0a9). Building the current `main` source still produces 0.1.1.

## Latest development behavior

Condenser Mark I processes one tagged log into **four Stone Fragments**, or one tagged plank into **one fragment**, without power. Each operation initially takes 160 ticks, or eight seconds at 20 TPS. Four fragments craft into one cobblestone in a 2×2 recipe.

The machine has a compact GUI with animated progress, persistent inventory/progress, safe output blocking and hopper/pipe access: top/side input and bottom output. The optional JEI category reads the actual machine recipes. Version 0.2.1 uses a clearer single-shard item texture.

Java 21 builds and all six dedicated-server tests passed. TeKatz confirmed in-game log/plank processing and fragment crafting. Manual approval of the revised texture and a complete exported-pack installation test remain pending.

## Fixed island and changing mod selection

The supplied island is final. Preserve its layout, spawn and saved contents unless TeKatz explicitly requests a revision. Its exact NBT and imported settings are documented in [START_ISLAND.md](docs/START_ISLAND.md).

NeoForge remains at **21.1.259** until TeKatz reports a change. The installed mod list is intentionally experimental; updates are captured from newly supplied logs. An observed installation does not determine a mod's final progression role.

The island and actual world settings have been received. Remaining work includes future recipes, quests, customized scripts/overrides where relevant and a real launcher/export manifest. See [FILES_TO_IMPORT.md](docs/FILES_TO_IMPORT.md).

## Documentation

| Path | Contents |
| --- | --- |
| [DESIGN.md](docs/DESIGN.md) | Agreed requirements and open design decisions |
| [PROGRESSION.md](docs/PROGRESSION.md) | Implemented development progression and further proposals |
| [START_ISLAND.md](docs/START_ISLAND.md) | Fixed template, verified spawn and exact ME contents |
| [MODS.md](docs/MODS.md) | Runtime/build versions and separate Forever repositories |
| [MODLIST_SNAPSHOT.md](docs/MODLIST_SNAPSHOT.md) | Full observed mod-ID/version table |
| [VALIDATION.md](docs/VALIDATION.md) | Automated checks, user tests and remaining validation |
| [CHANGELOG.md](docs/CHANGELOG.md) | Project history across branches |
| [AGENTS.md](AGENTS.md) | Contributor instructions |

## Build the source on this branch

Install Java 21, then run:

```sh
cd mod
./gradlew build
```

On Windows, use `gradlew.bat build`. Output is written to `mod/build/libs/`. See [mod/README.md](mod/README.md) for the source version present on this branch. GitHub Actions provides build artifacts.

The `pack/` directory on `main` remains the earlier partial import until PR #2 is merged. For the actual supplied configuration and template, use the [development pack files](https://github.com/TeKatz/ForeverSkyblock/tree/8d9f4136f6427893aeac0fc8f47857d19646e0a9/pack). The inventory is documentation, not an installable launcher manifest.
