# Changelog

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
