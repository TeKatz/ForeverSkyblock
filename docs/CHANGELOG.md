# Changelog

## 2026-10-15 — Provisional mod-list refresh

- Record TeKatz's supplied 80 enabled artifact labels and preserve the exact original text.
- Update Forever Skyblock to the listed 0.6.1 and record Iron Jetpacks 8.0.11 / Iron Furnaces 4.3.2.
- Archive the earlier 79-loaded-ID / 67-JAR runtime snapshot unchanged and distinguish artifact labels from loaded mod IDs.
- Keep the selection experimental and NeoForge 21.1.259 unchanged; no progression roles, skill locks, recipes or compatibility guarantees are inferred.

## 2026-10-15 — Iron Jetpacks research test (0.5.0)

- Add four sequential profile-wide flight research stages with costs 5/10/20/40 SP and a dedicated Jetpacks tab.
- Map actual jetpack definitions and typed components to their research stage; keep Iron Jetpacks optional.
- Add crafting, cached AE2 pattern and chest/Curios flight restrictions; update JEI items/recipes at runtime as research changes.
- Add an explicitly temporary +10 SP test button for singleplayer owners/operators and a conditional Creative Jetpack recipe.
- Preserve existing profiles and the confirmed reset, including repeatable refunds/relocking for testing.
- Record Iron Jetpacks and Iron Furnaces as user-reported additions with versions awaiting a new snapshot; do not alter the historical runtime inventory.


## 2026-10-15 — 0.4.0 accepted for merge

- Record TeKatz's successful in-game acceptance of 0.4.0 and the improved Skills GUI.
- Consolidate the tested condenser, fixed-island pack files and persistent skills through 0.4.0 into the main development baseline.
- Preserve the distinction between general user acceptance and individually enumerated manual test steps.

## 2026-10-15 — Clearer skill cards, refunds and reward shortcut (0.4.0)

- Replace the selected-row/detail layout with full-width skill/task cards and inline effect/objective, rank/reward and action.
- Keep available points visible, mark the active tab and scroll long/future card lists.
- Add confirmed skill reset with server-side point refunds, immediate effect removal and persistent task claims.
- Make only the chat point reward green and clickable to open the Skills menu.
- Keep schema 1 and existing profiles compatible; update the skill payload protocol to require matching client/server versions.
- Record TeKatz's successful two-world milestone, purchase and carried-over skill test of 0.3.1.


## 2026-10-15 — Skills menu readability (0.3.1)

- Draw the custom panel in the background phase, before widgets, so the inherited screen render does not blur the finished UI a second time.
- Remove description tooltips from the detail area and catalog rows. Wrap descriptions inline and allow mouse-wheel scrolling with a visible scrollbar when space is limited.
- Preserve profiles, milestone rewards, purchases, packets and condenser behavior.

## 2026-10-15 — Persistent skills (0.3.0)

- Add UUID-based profiles outside world saves, with atomic persistence, validated backups and explicit failure handling.
- Add the once-per-profile First Foundations milestone and one-point Extended Reach skill.
- Apply a stable, non-stacking +1 block interaction modifier on purchase/login/respawn/dimension changes.
- Add server-authoritative open/purchase packets, a custom scrollable Skills/Milestones screen and a remappable K shortcut.
- Add the `/foreverskills` fallback and operator-only point-grant command.
- Add English/German labels, profile-storage tests and dedicated-server effect/recipe/packet tests.
- Preserve the fixed island, existing condenser functionality and observed mod inventory snapshot.


## 2026-10-15 — Fixed island, actual configuration and development inventory

- Import the actual Skyblock Builder settings and approved final `forever_me_start.nbt` without modifying the supplied files.
- Replace the provisional island filename/spawn with the actual template and east-facing spawn `[8, 4, 5]`.
- Verify one 1k storage cell containing eight bread and one wheat seed directly from NBT.
- Replace reconstructed Default World Type settings with the actual supplied file.
- Record NeoForge 21.1.259 as the pack runtime, separately from the mod's 21.1.249 build baseline.
- Extract 79 loaded mod IDs/versions and 67 mods-folder JAR filenames as an explicitly evolving development snapshot.
- Close the received-file gaps and retain future recipes, quests, scripts, additional overrides and launcher export as later work.
- Exclude raw logs and generated Skyblock Builder registry listings.


## 2026-10-15 — Clearer Stone Fragment texture (0.2.1)

- Replace the three-pebble sprite with one angular stone shard with a light fracture face and a dark outline, readable at 16×16 inventory scale.
- Preserve all item IDs, recipes, processing behavior, GUI and automation.
- Record TeKatz's successful 0.2.0 in-game processing and crafting test. The revised texture still needs visual approval in the pack.


## 2026-10-15 — Condenser Mark I processing (0.2.0)

- Process one tagged log into four Stone Fragments, or one tagged plank into one fragment, without power. Default duration: 160 ticks per operation.
- Add persistent input/output inventory, saved progress, full-output pausing and inventory drops when removed.
- Add a compact localized GUI with animated progress and activity indicators, shift-click support and subtle active block light.
- Expose sided item handlers for hopper and pipe input/output.
- Add the custom Stone Fragment sprite and a 2×2 cobblestone recipe.
- Register a datapack-driven Condensing recipe type and an optional JEI category using the actual machine recipes.
- Add isolated dedicated-server tests and run them in CI.
- Record the latest island layout and reduced bread-and-seed starter inventory.


## 2026-10-15 — English documentation and TeKatz identity

- Translate all project documentation and READMEs into English.
- Use TeKatz throughout project files and establish this identity and language policy in `AGENTS.md`.
- Rename the Java package and Gradle group to `de.tekatz.foreverskyblock`.
- Bump the mod to 0.1.1 so the updated compiled package is distinguishable from 0.1.0.
- Preserve registered IDs, gameplay and supported in-game translations.

## 2026-10-15 — Initial project import

- Import the existing 0.1.0 mod source into `mod/`, including the Gradle wrapper, three textures, block/item models, translations and loot table.
- Import the edited `templates.json5` with `forever.nbt` as its only template.
- Record the agreed world-type restriction in `pack/config/defaultworldtype/client-config.toml`, explicitly marked as reconstructed configuration.
- Document Expert difficulty, the starting island, planned starter inventory reductions, open mod choices and separate Forever repositories.
- Record Stone Fragments → cobblestone → furnace → stone as an explicitly unconfirmed proposal; no production implemented.
- Include two screenshots supplied by TeKatz as references.
- Add working instructions, a build workflow and a list of local pack files still missing.
- Preserve the existing root license; exclude built JARs, third-party mods, worlds and local build helpers.
