# Validation status

## Omnidirectional machine cables — integrated October 23, 2026

Source `ab743dbd90ba607a0a0751f6c25f05b34f7bf647` on `fix/omnidirectional-machine-cables` passed [Actions run 39910924225](https://github.com/TeKatz/ForeverSkyblock/actions/runs/39910924225). TeKatz subsequently authorized merging [PR #34](https://github.com/TeKatz/ForeverSkyblock/pull/34), now integrated into `main` as `88414546678f53ee4983f56d89b7ff7e9adbdd31`. The merge preserves the tested source and resources exactly. The mod version remains 0.27.0; no public release was created. [Post-merge Actions run 39917867033](https://github.com/TeKatz/ForeverSkyblock/actions/runs/39917867033) also passed the build, all 100 JUnit tests, client startup/visual checks and all 122 required GameTests in each server run on NeoForge 21.1.264.

The original branch's [failed run 39846106532](https://github.com/TeKatz/ForeverSkyblock/actions/runs/39846106532) completed compilation and client checks but failed two of 120 GameTests. `sidedAutomationAndSimulation` still forbade bottom insertion and top extraction; `portsExposeOnlyTheirHarvestAndSimulationDoesNotConsume` still expected non-bottom Garden capabilities to be absent. Those expectations contradicted the branch's intended capability contract. The registered Condenser, Garden Chamber, Washing Basin and Refining Condenser capabilities already selected their machine-owned combined handlers correctly. The follow-up corrects and expands the tests without changing production handlers, recipes, buffers, filters or output policies.

Coverage queries the actual registered `Capabilities.ItemHandler.BLOCK` providers, rather than bypassing registration through the internal `automation(side)` accessors. All six faces are checked for valid insertion, exact extraction and non-mutating simulation. Additional checks preserve Garden specimen insertion and retention, the two separate harvest routes, hidden module/upgrade slots, Mark-II exact-item filters and bounded input/output buffers, both Washing outputs and Refining ingredient/output and energy restrictions. Unsided Condenser, Garden, Mark-II and Refining access remains covered. The Crucible regression now also queries its registered item/fluid capabilities and verifies its existing directional contract. Existing real Vanilla/Wooden Hopper production tests remain in the suite.

Validation passed: Java 21 build and GameTest compilation; **100 JUnit tests**; client mixin/model startup check; machine GUI/block and K-menu visual/input fixtures; **122 required GameTests in each of the two dedicated-server runs**, both on NeoForge **21.1.264**, with the second run enabling the configured optional pack integrations including Mekanism. This does not claim a manual player-instance test or a physical Mekanism transporter-network transfer test.

Downloaded and inspected CI artifact **12482433396** (`ForeverSkyblock-Mod`): archive integrity and artifact ZIP SHA-256 `094069b47c15ab32d0001e5997a2e3ee58ad11b239281f5b3f9a869c19aa8166` match. Its production JAR is `ForeverSkyblock-1.21.1-NeoForge-0.27.0.jar`, **1,107,408 bytes**, SHA-256 `11c3b4a217b262df1a7f52cbdc85585081469eeb22d855b487cf13e96c671b4e`. Java 21 bytecode, loader metadata, expected machine/adapter classes and all **250 source resource files** were verified; test/preview and bundled Minecraft/Mekanism classes are absent. The fixed island Git blob remains `4c0e3ada63bb61ef1d109ad110b66b6d359d473f`. A local wrapper attempt could not download Gradle in this environment; the successful build/runtime evidence above comes from the linked CI run.

Post-merge artifact **12486605857** was downloaded from the successful Main run and inspected separately. Its ZIP SHA-256 is `52f013a7c3da79b80efba8edfc144885e76f0d3e8ad95f9cd16059690d104540`; the contained JAR has the same verified filename, byte size and SHA-256 as the pre-merge artifact above. All 250 source resources, Java 21/loader metadata, mixin class references and the absence of test/preview or bundled Minecraft/Mekanism classes were checked. The integration documentation follow-up changes Markdown only.

## 0.24.0 integration — PR #16

The complete 0.21–0.24 stack is integrated through [PR #16](https://github.com/TeKatz/ForeverSkyblock/pull/16). Source `227c81611f8c8cda719c59106c80d6350a2034a1` passed [Actions run 39178417187](https://github.com/TeKatz/ForeverSkyblock/actions/runs/39178417187): Java 21 build, **78 unit tests**, client startup/model checks, actual-client rendering/input and **90 required GameTests on each runtime** (NeoForge 21.1.249 and pack 21.1.259). The integration documentation changes no validated code/resources.

The native client fixture produced **17 EN/DE captures**, including 640×360 and 480×270 logical layouts. Representative views were inspected. Four actual screen-input checks passed: Challenge card, circular Shop node corner/center, Contract card and scrolled category selection. Synthetic fixture data and reflection setup are excluded from the mod JAR; these checks do not certify a live player skin, full-instance gameplay or every transaction in a player's save.

The fresh `ForeverSkyblock-1.21.1-NeoForge-0.24.0.jar` is **5,262,116 bytes**, artifact `12155336788`, SHA-256 `06694e5ead79a1d59e010c114663a9bc1a1bf767b960233255e3e0c2ea8098bb`. ZIP/JAR integrity and Java 21 metadata passed; all **158 resource files** match source, `skills-9` is present and test/preview classes are absent. Fixed island Git blob remains `4c0e3ada63bb61ef1d109ad110b66b6d359d473f`.

Current persistence, rollback instructions and remaining player-instance acceptance are in [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md#validation-and-acceptance). Earlier evidence below is version-specific history, not the current mod/protocol baseline.

## Current-world shop safety — 0.12.0 / phase 3

Tested source `50683be3bf4fd3d1e2261aa15e953c82ae1be3f4` passed [CI run 38851560072](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38851560072), job `116813669907`: the Java 21 build and pure world-identity/profile tests passed, followed by **all 48 required GameTests in both server runs**. The second run used the pack runtime NeoForge 21.1.259 with the existing AE2, Iron Jetpacks, Mekanism, Powah and Ultimine integration set.

Automated coverage includes restart/rename/copy/new-save identity semantics; missing/corrupt identity-copy repair and conflict refusal; exact tier/cost/scope/accounting/receipt checks; schema-3→4 migration; 64 concurrent duplicate requests; pre-commit rollback and deterministic recovery after every write-ahead-journal crash cut; stale/corrupt primary recovery from a committed journal; bounded snapshot round trips; and a real `ProgressionService` operator purchase after Dragon Tier I. The service test verifies exactly one 5-SP debit, claim and receipt and an idempotent repeat request.

The verified production JAR is `ForeverSkyblock-1.21.1-NeoForge-0.12.0.jar`, **4,983,088 bytes**, SHA-256 **`63ac04a1c3d260525521b2b2569a9bc034bbdb68f9ccb8161d6d29b63c91acad`**. CI artifact ID: `12025138559`; artifact ZIP SHA-256: `209990b9929ebe2ea75f30bde636875fdc867fd7d84301495034760825e3b89e`. ZIP/JAR integrity, version 0.12.0, Java 21 bytecode and absence of test classes passed. Comparison with 0.11.0 found no removed entries and no changed condenser, Ascension, recipe, texture or tool-policy entries.

Manual client acceptance remains pending for an existing-profile migration, confirmation and claimed states, restart/copy/new-world behavior, friendly world name, EN/DE text and GUI scales. Back up both `forever_skyblock/progression/` and the world save before testing. Once written as schema 4, older JARs cannot read that profile. Deleting both world-identity copies is unsupported and may create a new identity; one missing or corrupt copy repairs from the other.

## Personal challenge progression — 0.11.0 / phase 2

Tested source `9349caec4b134fbd383fecb3e50bca8ce6f50dfa` passed [CI run 38848917148](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38848917148), job `116805567107`: the Java 21 build and pure progression/profile tests passed, followed by **all 46 required GameTests in both server runs**. The baseline exercised the mod without optional integrations. The pack-loader run used NeoForge 21.1.259 with AE2 19.2.18, Iron Jetpacks 8.0.11, Cucumber 8.0.16, GuideME 21.1.19, FTB Ultimine 2101.1.15, FTB Library 2101.1.37, Architectury 13.0.11, Mekanism 10.7.19.85, Powah 6.2.10 and Cloth Config 15.0.140.

The progression tests cover schema-2 migration, First Foundations identity without a second reward, accounting, unknown IDs/revisions, distinct tools across reloads, a 630+220+150 Veteran sequence, duplicate/concurrent evidence, failed persistence and monotonic Tier 0/I. GameTests use real Vanilla crafting and smithing result slots, actual hostile/Wither/Dragon lethal damage, fake/passive exclusions, dimension transitions, AE2's actual manual crafting slot and snapshot round trips. Both loader runs reported `All 46 required tests passed :)`.

The verified production JAR is `ForeverSkyblock-1.21.1-NeoForge-0.11.0.jar`, **4,965,610 bytes**, SHA-256 **`fe5c841ad8aa20fff0aab705be2e2042c261fe94e4b7481363f36202a8bbff7b`**. CI artifact ID: `12023818884`; artifact ZIP SHA-256: `88c3a99e55bb4d749a6b01ef65ccc085caa152e81a5157b6ab96036fc6adbadf`. The ZIP and JAR integrity checks passed. Inspection confirmed version 0.11.0, both personal-action mixins, the new progression classes and EN/DE resources, with no removed JAR entries. No condenser, Ascension, recipe or texture entry changed compared with the delivered 0.10.0 JAR.

Manual client acceptance remains pending for migration on a copied profile, K-menu rendering at the preferred GUI scales, normal/Shift ME crafting, multiplayer boss credit, restart/new-world persistence and retained machine visuals. Back up the progression directory before testing: once a changed profile is written as schema 3, older mod versions cannot read it.

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

## Ascension and Perk Points 0.8.0 — October 15, 2026

Tested source `d974f092cae2ed0a5bd31bd2806340d2c453b89f` passed [CI run 38798641171](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38798641171), job `116641574280`: Java 21 build, all **22 JUnit tests**, and all **42 required GameTests in each loader run**. The baseline used NeoForge 21.1.249 without optional integrations. The pack-loader run used NeoForge 21.1.259 with AE2 19.2.18, Iron Jetpacks 8.0.11, Cucumber 8.0.16, GuideME 21.1.19, FTB Ultimine 2101.1.15, FTB Library 2101.1.37, Architectury 13.0.11, **Mekanism 10.7.19.85, Powah 6.2.10 and Cloth Config 15.0.140**. Modrinth version IDs pin those last three development-only dependencies; no third-party classes are bundled.

New coverage includes six pure redemption/profile tests and a Chamber model geometry regression test. Eight new GameTests exercise Core identity and recipe codecs/conditions, two real Vanilla Crafter operations with distinct outputs and consumed ingredients, rejection of cached AE2 Core outputs, initiator binding, menu UUID data, hopper simulation/locks, duplicate requests, saved progress and world rollback replay, invalid/copied/other-player claims, offline pause, remote-menu rejection, active-block drops and deliberate profile-storage failure followed by a successful retry. The existing real-Iron-Jetpacks test now proves SP alone cannot buy Creative; redeeming a Core enables the dual-cost purchase. All previous tool, Ultimine, condenser, transfer, milestone, reset and optional-integration tests remain included.

The controlled storage-failure test intentionally logs its caught I/O error; it then verifies the same Core is retained and a repair/retry pays once. An earlier test run exposed a test-fixture path mistake: Minecraft's GameTestServer reports a local authority, so the fixture must select local/server exactly as the service does. That fixture was corrected before the passing final run. Earlier build/test artifacts were not delivered.

The verified production artifact is `ForeverSkyblock-1.21.1-NeoForge-0.8.0.jar`, **212,756 bytes**, SHA-256 **`664dbd74d990533c264754cbe4bba5022ae0e9ff07b24f142711136d30945edc`**. Artifact ID: `12003167078`; artifact ZIP SHA-256: `26a2f70cf9761c18fd913c13b66c0546b428f417c76f384bb762782d3806acc1`. Inspection checked the artifact digest, metadata/TeKatz author, Java 21 classes, every source resource against the JAR, every Recipe Editor mirror, texture dimensions, nine mixin declarations and absence of test or bundled third-party classes. Existing condenser block/entity/menu/transfer/network/screen and permanent-tool policy mixin class bytes match the delivered 0.7.0 JAR.

The active/inactive Chamber models share one identical full-cube geometry, without intersecting solids or coplanar face overlaps. English/German machine labels were checked against Minecraft's bitmap glyph widths and shortened to fit the interface. This is static asset/layout verification, not an actual client render. The fixed island SHA-256 remains `1ae2e4b779b1c6b56320aede0af2a711d4bd247a976771c95f633e05c1beb0c1`; all pre-existing recipe resources and the supplied mod inventory are unchanged. Documentation-only follow-ups do not change this tested mod source.

Remaining manual checks: actual client graphics at preferred GUI scales and beside adjacent blocks, JEI recipe/category clicks and live visibility, normal ME-terminal mouse crafting, multiplayer player-name display, full pack export and endgame balance. The Vanilla Crafter is the certified automatic Core producer; AE2 automatic Core patterns intentionally fail safely. See [ASCENSION.md](ASCENSION.md) for initial recipes, profile migration, filesystem/backup limits and exact behavior. TeKatz authorized integrating PRs #9 and #10 into `main` on October 15, 2026. This integration publishes no release and changes no tested mod code or resources.
