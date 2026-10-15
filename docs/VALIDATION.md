# Validation status

## Skills menu readability — 0.3.1

TeKatz's 0.3.0 screenshot showed blurred custom text/panels while vanilla buttons remained sharp, and a single-line description tooltip extending off screen. The screen called `renderBackground` before custom drawing and then `Screen.render`, which calls the background again. The fix draws the panel in the overridden background phase; the inherited render then draws widgets. Long descriptions wrap and scroll inline, with no area-wide or row description tooltip.

[Java 21 build, all eight storage tests and all nine dedicated-server GameTests passed](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38698177437) for source commit `bc4f8df0f14a76fdd5d586b4781b40d579d3a648`. The delivered JAR contains version 0.3.1 and author TeKatz. Compared byte-for-byte with 0.3.0, only `SkillScreen.class` and the mod version metadata differ; all progression/storage code, condenser code, recipes, translations and assets are unchanged.

- JAR SHA-256: `c1fe3176590aab70b15a7693dfc5f0d3c33ff63744be9684af2ca1815b6df792`.
- Verified archive digest, absence of test/profile data, JSON/TOML, translation parity, relative documentation links and the unchanged fixed-island hash.
- Visual acceptance at multiple GUI scales with background blur enabled remains a manual client check; headless server tests do not verify rendered pixels.

## Persistent skills — 0.3.0

Implementation is on `feature/persistent-skills`, based on the current condenser branch. [Java 21 build, all eight storage tests and all nine dedicated-server GameTests passed](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38696790035) for commit `127009ef9aded0f203ea14b295d1e2970a4178a2`.

- Storage tests verified persistence after store recreation/world-directory deletion, once-per-profile rewards, persistent non-duplicated purchases, insufficient funds, independent player UUIDs, backup recovery, corrupt/future schema refusal, failed-write rollback and concurrent duplicate claims.
- Server tests verified exactly +1 block interaction range without stacking, removal/reapplication of the modifier, the fragment-crafting milestone predicate and payload round trips.
- All six existing condenser integration tests also passed. No JEI dependency was required on the dedicated test server.
- JSON/TOML, translation-key parity, relative links, common/client separation and the unchanged fixed-island hash were checked.

Automated coverage includes eight profile-storage tests (restart/world independence, deduplication, purchases, insufficient funds, player separation, backup recovery, corrupt/future schemas, failed writes and concurrent claims) and three additional server GameTests (non-stacking reach application, exact milestone ingredients and packet round trips). Existing six condenser GameTests remain enabled.

Manual client testing remains pending: remappable key, GUI layout and purchase states, crafting through AE2, exact reach behavior, death/respawn, new-world carryover and a full game restart. These are not claimed as completed.


## Fixed island and actual pack configuration — October 15, 2026

- Parsed all 10 supplied Skyblock Builder JSON5 configuration files and the actual Default World Type TOML.
- Verified `templateList` selects only `forever_me_start.nbt`; the referenced structure exists and spawn `[8, 4, 5]` is air over grass within the structure bounds.
- Parsed the original compressed NBT: 14×11×10, 396 blocks, zero entities, expected machines and saved cable/terminal parts.
- Read the storage cell component directly: one 1k cell with exactly eight bread and one wheat seed.
- Imported operational configuration and structures byte-for-byte; preserved the fixed-island SHA-256. Generated registry lists and the raw log are excluded.
- Extracted 79 unique loaded IDs and versions, plus 67 mods-folder JAR filenames. NeoForge 21.1.259 matches TeKatz's stated pack runtime.
- These are source/configuration checks, not a newly performed client installation test. TeKatz previously confirmed the island works and has now declared it final. A fresh complete exported-pack test is still pending.


## Forever Skyblock mod 0.2.1

- Texture-only gameplay update: one angular stone shard replaces the three-pebble image; texture remains transparent and 16×16.
- No Java sources or recipe data changed; build metadata, version inventory and documentation identify 0.2.1.
- Build/artifact checks are performed for the new JAR before delivery; the existing server test suite also runs through CI.
- Manual visual approval of the revised texture remains pending.


## Forever Skyblock mod 0.2.0

- [Java 21 build and five dedicated-server GameTests passed](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38688266212) for commit `26a12bb258787aec7acbb7e4357aff515eb04af3`.
- Verified the 4:1 log ratio and 1:1 plank ratio, delayed output and consumption only at completion.
- Verified full-output blocking and resumption without item loss.
- Verified sided capability access, rejection of invalid input, and non-mutating simulated insertion/extraction.
- Verified actual vanilla hoppers feeding wood and extracting exactly four fragments.
- Verified inventory and partial progress serialization/restoration followed by exact completion.
- [All six dedicated-server GameTests passed](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38688444211) for commit `97ede5cf0727298bff9f78fa32d37ad34a7ddad8`, including shift-click transfers, menu distance checks and exact inventory drops on removal.
- Static JSON/TOML, recipe-data, version and project-identity checks passed. The fragment sprite was visually inspected.

TeKatz reported successful in-game testing of 0.2.0: logs and planks are processed into Stone Fragments, and the fragments can be crafted further. TeKatz stated that the functions work as intended; the remaining requested adjustment was item-texture readability. This is user-reported validation, separate from the automated tests above. Individual GUI/JEI checks were not enumerated, and a fresh exported island test is still pending.


## Forever Skyblock mod 0.1.1

- The Java package and Gradle group now use `de.tekatz.foreverskyblock`.
- Author metadata and project licenses identify TeKatz.
- Registered mod, block and item IDs remain unchanged.
- [Java 21 CI build succeeded](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38681195245) for source commit `038e7cbc4c3a67dda232a46d49ae506c8c27d0ae`.
- Inspected the actual CI JAR: version `0.1.1`, author `TeKatz`, compiled class `de/tekatz/foreverskyblock/ForeverSkyblock.class`, stable registered IDs and retained German localization.
- Checked every repository file path and file's bytes, plus every JAR entry name and its decompressed bytes, for remnants of the previous personal name: no matches.
- Parsed all JSON and TOML files and verified relative Markdown links.
- JAR SHA-256: `be393e77bc3408f494d035cc601d08c859a54ef3fecf5b24c91e9c7548e9f84c`.
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
2. Preserve and verify the fixed island and its eight-bread/one-seed inventory.
3. Record the full mod list and versions.
4. Create a fresh instance from the eventual pack export.
5. Check the island, spawn, power, ME contents and world-type restriction.
6. Play through all implemented resource paths, recipes and quests.

A successful mod build alone does not validate a complete pack export.
