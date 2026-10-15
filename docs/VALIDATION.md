# Validation status

## Iron Jetpacks research — 0.5.0

[Java 21 build, fourteen profile/research tests and fourteen dedicated-server GameTests passed](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38711112494) for source commit `cfafa994f2e676de7a3828fe1830f8aa7dc91358`. Baseline GameTests run without optional mods on NeoForge 21.1.249. A second required integration run loads Iron Jetpacks 8.0.11, Cucumber 8.0.16, AE2 19.2.18 and GuideME 21.1.19 on the actual pack loader, NeoForge 21.1.259.

- Verified material/creative classification, sequential stages, persistent purchases/refunds and prerequisite refusal.
- With Iron Jetpacks present, checked actual generated shaped recipes at each of the five access states, including component/upgrade recipes and the Creative recipe. Each stage checks at least twenty registered, satisfiable recipes.
- Verified a locked equipped chest jetpack is excluded from flight, an unlocked one is usable, another player's research does not grant personal flight, and reset revokes eligibility.
- Loaded/transformed the AE2 manual crafting-slot hook and verified an actual encoded AE2 crafting pattern works after unlock but produces no output after reset, without consuming its supplied inputs.
- All previous condenser/profile tests pass. Optional-target mixins skip absent mods; fresh game-test directories emit the vanilla missing-server.properties initialization message.
- Inspected the delivered JAR: 0.5.0/TeKatz metadata, optional Iron Jetpacks dependency, all six declared mixins, new classes/recipe, EN/DE key parity, no bundled third-party classes or test/profile data. Thirty condenser recipe/texture/implementation entries match 0.4.0 byte-for-byte. JSON/TOML, relative links, four-card layout bounds and the unchanged fixed-island hash passed.
- JAR SHA-256: `99d1ab1cc8be3beb1267a360da9e66538298f9b79f7f2bb478126c3ff8fcac4f`.

At the time of the 0.5.0 integration test, installed Iron Jetpacks/Iron Furnaces versions had not been supplied. The later `Modliste.txt` update lists Iron Jetpacks 8.0.11 and Iron Furnaces 4.3.2; this is separate user-supplied evidence.

TeKatz reported successful in-game testing of 0.5.0 on October 15, 2026: unlocked jetpacks appear in JEI only after purchase, resetting points hides them again, and jetpack functionality is disabled without the required research. A specifically tested Creative Jetpack works only after buying the final Creative stage. TeKatz stated that the system behaves exactly as intended. This is user-reported client validation, separate from the automated tests above.

Remaining individually unconfirmed checks: multiple GUI scales, temporary test points with a non-cheat singleplayer owner, separate coverage of every typed component, manual ME-terminal crafting, Curios-specific equipment behavior and 0.5.0 research/reset persistence after rejoining, changing worlds and restarting. Shared playerless automation semantics are documented in [JETPACK_RESEARCH.md](JETPACK_RESEARCH.md); arbitrary modded crafting paths are not claimed as validated.


## Skill cards, refunds and reward shortcut — 0.4.0

Implementation originated on `feature/skills-menu-reset`, based on `feature/persistent-skills`. [Java 21 build, all eleven profile-storage tests and all eleven dedicated-server GameTests passed](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38700037140) for source commit `08c2de1a85a4358a8c27937d3ebe0a7ac686d68b`. Added coverage verifies persistent and idempotent refunds, preserved unspent points/task claims/unknown skill IDs, separate player profiles, balance-limit refusal, failed-write rollback, immediate reach removal, reset payload round trips and chat reward styling/command. All six condenser GameTests remain passing.

The delivered 0.4.0 JAR was checked for its archive digest, version/author metadata, new client/common classes, EN/DE labels and exclusion of test/profile data. Thirty-three condenser recipe/texture/implementation entries match 0.3.1 byte-for-byte. JSON/TOML, translation parity, relative Markdown links and the fixed-island hash passed. JAR SHA-256: `f8d238093b0bd05a3f052380c278c1045126cbad74cd4aa84550b071bd7d7327`.

TeKatz reported successful testing of the preceding 0.3.1 feature across two worlds: the configured task completes, the skill can be purchased, and its effect remains active in the next save as intended. This is user-reported gameplay validation. It does not validate the new card layout, reset flow or clickable chat reward introduced in 0.4.0.

TeKatz subsequently reported that 0.4.0 works and the revised GUI looks better, and authorized merging the tested changes. This is user-reported in-game acceptance of the update; individual reset and chat-click test steps were not enumerated.

Remaining detailed acceptance checks, where not covered by that report: both tabs and scrolling at multiple GUI scales, reset Cancel/Escape/Confirm, immediate effect removal, reset persistence across worlds/restarts, no repeated milestone payout and clicking the green chat reward.


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

## Condenser mixing 0.6.0 — October 15, 2026

- Tested source: `11c602b5a0df0daa9106c1797643db3e8042613c`, on `feature/condenser-mixing-chamber`, based on the unmerged Iron Jetpacks feature branch.
- [CI run 38718605502](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38718605502) passed the build, 14 JUnit tests and 21 GameTests in each loader run: NeoForge 21.1.249 without optional mods; NeoForge 21.1.259 with Iron Jetpacks, Cucumber, AE2 and GuideME.
- Seven new integration tests exercise every face/diagonal adjacency, the same open menu becoming active/inactive, swapped ingredients, module removal and reattachment, persistent exact-item filters, simulated automation and real hopper routing into dirt, ghost safety/rejection, legacy two-slot NBT, hidden-item drops, blocked dirt output, single-input fallback, mortar remainders and recipe network codecs. Existing skill/Jetpack tests remain included.
- Inspected the built JAR metadata, Java 21 classes, new assets/recipes, absence of test classes and identical existing progression/mixin bytecode compared with the delivered 0.5.0 JAR. Full pack files and fixed island bytes are unchanged.
- Client visual review, actual JEI drag/drop and inventory mouse gestures are pending TeKatz's in-game test. No new manual client test is claimed.
- JAR: `ForeverSkyblock-1.21.1-NeoForge-0.6.0.jar`, 132,387 bytes; SHA-256 `19b32bf4c9855d46262f6cca4ca3bc8bd0969d4bee109d1748d8232f56a7065b`; artifact `11963717159`.

## Chamber model, JEI transfer and tutorial update 0.6.1 — October 15, 2026

Source commit `1301e71854eefbad5f15ab61ba8aaacf14c828f5` passed [CI run 38724723640](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38724723640): build, all 15 JUnit tests and all 27 GameTests on each of NeoForge 21.1.249 without optional mods and NeoForge 21.1.259 with Iron Jetpacks 8.0.11, Cucumber 8.0.16, AE2 19.2.18 and GuideME 21.1.19. Six new GameTests cover read-only preview and one-set transfers, filtered/reversed maximum transfers, missing/stale/invalid requests, full-inventory rollback and replacement, component and hidden-input preservation, and all six adjacent log faces. The new geometry JUnit test rejects overlapping solids, coplanar face overlaps and incorrect inset cull tags.

The production JAR is `ForeverSkyblock-1.21.1-NeoForge-0.6.1.jar`, 152,138 bytes; SHA-256 `8cfdfbc8291c6fca539610409f284dacab8230ce0b4002742709ab5a4a1a9ff5`. Artifact ID: `11967371878`. Its metadata, Java 21 classes, new handlers/model and absence of test classes were inspected. Prior progression/mixin class bytes and texture PNG bytes match the delivered 0.6.0 JAR. Pack files, including the fixed island, remain unchanged.

The Vanilla `Tutorial.setStep` implementation was inspected in the SHA-1-verified official Minecraft 1.21.1 client bytecode using Mojang's mappings: it updates the tutorial option, saves Options and clears/replaces an active step. This is source/bytecode verification, not a manual client test. Documentation-only follow-ups do not alter the tested mod source.

Client visual verification beside the condenser/log/fence, actual JEI plus/Shift-plus clicks and fresh-instance tutorial behavior remain for TeKatz's in-game test. No manual client test is claimed.

## Acceptance and integration — October 15, 2026

TeKatz accepted the delivered 0.6.1 update and requested merging the completed work. The Iron Jetpacks research and condenser-mixing prerequisites were merged into `main` through PRs #5 and #6. PR #7 combines the chamber-rendering fix, JEI recipe transfer, introductory tutorial suppression and this documentation refresh. Its mod source is unchanged from the verified 0.6.1 test JAR; the final follow-up edits only documentation. Acceptance is user-reported; individual client checks were not enumerated and are not claimed as separately performed automated or manual tests.

## Updated user-supplied mod list — October 15, 2026

Imported `Modliste.txt` as 80 unique enabled artifact labels, with no disabled entries. Verified exact label order/count, source-text SHA-256 and byte-for-byte preservation of the raw text and previous runtime JSON. Confirmed Forever Skyblock 0.6.1, Iron Jetpacks 8.0.11 and Iron Furnaces 4.3.2 directly from the supplied labels. The snapshot is explicitly provisional and does not assert a new loaded-ID count, embedded module removals, launcher download identifiers or full pack compatibility. Schema 3 distinguishes this launcher list from the archived schema-2 runtime-log snapshot. No mod code, recipes, configs or fixed-island bytes changed; no new game build is required for this documentation/data update.

## Permanent tool progression 0.7.0 — October 15, 2026

Tested source `b5109aa6dd6fd4f239a972c846bf5639fb834a63` passed [GitHub Actions run 38774235720](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38774235720): Java 21 build, all 15 JUnit tests and all 34 dedicated-server GameTests in each run. The baseline used NeoForge 21.1.249 without optional integrations. The pack-loader run used NeoForge 21.1.259 with AE2 19.2.18, Iron Jetpacks 8.0.11, Cucumber 8.0.16, GuideME 21.1.19, FTB Ultimine 2101.1.15, FTB Library 2101.1.37 and Architectury 13.0.11. The Ultimine test asserts the real mod is loaded, checks its actual tool predicate and executes its private block-break dispatcher for 64 stone blocks; it does not simulate the client key/selection/raytrace flow.

Seven new GameTests cover all 20 exact shaped patterns and material counts, consumed cores and removal of legacy IDs/cheap patterns, plank tag variants, Stone-based Gold and Iron-plus-Obsidian Diamond gates, the reinforced-ingot recipe, all four smithing upgrades with preserved name/enchantments/custom data/repair cost, damage immunity for all 24 tools including component-patched and saved stacks, normal wear for excluded equipment, actual server mining/combat/hoe/path/axe actions, alternative crafting-output and smithing removal, the 24-recipe client sync, special crafting repair rejection and the real Ultimine dispatcher. Existing condenser/filter/JEI-transfer/skills/Jetpack/AE2 tests remain included.

The production JAR is `ForeverSkyblock-1.21.1-NeoForge-0.7.0.jar`, 175,018 bytes; SHA-256 `700aba24336f634a185bb8bfad238c3b0865883b2f018b7bb959fdb83246364c`. CI artifact ID: `11993071517`. Inspection confirmed 0.7.0/TeKatz metadata, Java 21 classes, all nine mixin declarations, the new item model/texture, all 49 recipe/removal resources matching the Recipe Editor files, no bundled third-party classes or test classes, and unchanged prior progression/condenser entity bytecode and condensing recipes compared with 0.6.1. The fixed island hash remains `1ae2e4b779b1c6b56320aede0af2a711d4bd247a976771c95f633e05c1beb0c1`; the supplied mod inventory is unchanged.

The initial in-place Vanilla crafting-ID approach was caught by integration tests because NeoForge supplied higher-priority old patterns. The final own-ID recipes plus explicit legacy-ID removals and authoritative filtering solve that issue; no failed build was delivered. Documentation-only follow-up edits do not change this verified mod source.

Remaining manual checks: actual client JEI presentation/Recipe Editor editing, a starter-island survival playthrough, the complete Ultimine interaction, full client restart and multiplayer pack export. First metals and pre-Diamond Obsidian production remain design work; no complete survival route through Diamond is claimed.
