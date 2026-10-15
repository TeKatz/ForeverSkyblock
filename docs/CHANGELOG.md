# Changelog

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
