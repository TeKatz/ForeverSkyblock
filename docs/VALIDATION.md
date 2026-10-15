# Validation status

## Forever Skyblock mod 0.1.1

- The Java package and Gradle group now use `de.tekatz.foreverskyblock`.
- Author metadata and project licenses identify TeKatz.
- Registered mod, block and item IDs remain unchanged.
- Build and compiled-JAR checks for this revision are pending until recorded below.
- No new in-game test is claimed for 0.1.1.

## Previous Forever Skyblock mod 0.1.0 checks

- Java 21 Gradle build succeeded.
- JAR contents, Minecraft/NeoForge metadata and JSON resources were checked.
- A dedicated development server started successfully with Minecraft 1.21.1 and NeoForge 21.1.249.
- The block was placed through a server command and verified with `execute if block`.
- The loot table returned one `Condenser Mark I` when queried with a diamond pickaxe.
- The server shut down cleanly and saved its world.
- TeKatz's later screenshots show the texture in game; TeKatz confirmed working world generation, spawn and placed machines.

The local server checks used development mode. The complete imported pack export has not been tested in a fresh instance. Initial container-specific build-tool adjustments are not part of the mod source and were not imported.

## Forever Recipe Editor 1.4.3-skyblock.1

- Separate branch `skyblock`, commit `3dbaa50e8f0a59b900f8b5e5ac68f3cac73ea32f`.
- [Java 21 build succeeded](https://github.com/TeKatz/ForeverRecipeEditor/actions/runs/38678418057).
- The nine public API interfaces checked for JEI integration are unchanged between 19.53.0.425 and 19.64.0.454.
- The downloaded build artifact was verified using SHA-256; the JAR was checked for version, classes and the exact JEI requirement `[19.64.0.454]`.
- Interactive testing of opening, hover, ghost dragging, save/reload, rename and delete in TeKatz's current pack remains pending.

## Before publishing the pack

1. Import the actual missing files listed in [FILES_TO_IMPORT.md](FILES_TO_IMPORT.md).
2. Revise the Expert starter inventory in the island template.
3. Record the full mod list and versions.
4. Create a fresh instance from the eventual pack export.
5. Check the island, spawn, power, ME contents and world-type restriction.
6. Play through all implemented resource paths, recipes and quests.

A successful mod build alone does not validate a complete pack export.
