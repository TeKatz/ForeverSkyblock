# Working on Forever Skyblock

## Language and project identity

- Write all READMEs, project documentation, code comments and new contributor instructions in English.
- Use **TeKatz** for the project owner's name in documentation, credits, author metadata, package names and other project files. Do not introduce a personal name or personal email address.
- Use `de.tekatz.foreverskyblock` for this mod's Java package and Gradle group. Preserve registered identifiers such as `forever_skyblock:condenser_mark_i` unless a migration is explicitly requested.
- Keep supported in-game translations, including `de_de.json`; the English documentation requirement does not remove localization.

## Inspect the actual state first

- Before editing, inspect the branch, HEAD, working directory, existing rules and relevant source files. For other Forever mods, read their own repositories; older recollections do not replace current source code.
- Read `docs/CURRENT_STATE.md` and `docs/README.md` before relying on older phase documents. Historical branch/baseline paragraphs inside design records describe the state when they were written and do not override the current integrated baseline.
- `docs/DESIGN.md` contains agreed requirements. `docs/PROGRESSION.md` identifies unconfirmed proposals. Implement proposals only after approval, unless the user has already requested that specific implementation.
- Before working on the mod, inspect `mod/README.md`, registrations, resources and dependencies.
- The supplied `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` is the final starting island. Preserve its exact bytes, layout, spawn and ME contents unless TeKatz explicitly requests a revision. It supersedes the earlier `forever.nbt` placeholder name.
- The current pack/runtime and repository development target is NeoForge **21.1.264**, reported by the latest supplied `Modliste(6).txt` on October 22, 2026. Historical CI/runtime evidence may still name 21.1.259 or 21.1.262; do not rewrite those run-specific claims. `neoforge.mods.toml` keeps its compatibility minimum separately from the current runtime target.
- `pack/mods.inventory.json` is an experimental supplied-artifact snapshot, not a final selection or launcher lockfile. Refresh it from actual supplied logs/files when requested; do not freeze the mod selection or silently substitute versions.
- Do not infer installed versions from screenshots or unrelated Forever instances. Track remaining files in `docs/FILES_TO_IMPORT.md`.

## Branches and repository hygiene

- Implement new features, recipe balancing and fixes on separate bounded working branches.
- The initial project import into `main` was authorized by TeKatz's import request. Later merges into `main`, releases and publishing require a corresponding user request.
- After an authorized merge, normalize current-state documentation so it no longer says the merged work is still isolated from `main`. Keep historical phase notes only when they are clearly identified as historical context.
- Keep `README.md`, `mod/README.md`, `docs/CURRENT_STATE.md`, `docs/README.md` and `docs/CHANGELOG.md` aligned with the current integrated version.
- Do not create multiple overlapping handoff/status files for the same live feature. Prefer the existing feature document and the central current-state index.
- Preserve existing user changes. Do not overwrite unrelated work or bypass version control.
- Recipe Editor, Production Monitor and DeepCore retain their own repositories. Make changes there only in the appropriate repository and branch.

## Documentation boundaries

- `docs/CURRENT_STATE.md` is the concise statement of what is currently integrated and active.
- `META_PROGRESSION.md`, `TOOL_RECIPE_HANDOFF.md` and snapshot documents may retain historical design/baseline context. Do not silently reinterpret historical statements as current repository status.
- When documenting tested development work before its merge, state which source and pack files are actually available on the current branch. Link to the implementation branch/commit for files not yet present; do not imply a documentation update merges code.

## Implementation and validation

- Keep Minecraft 1.21.1, NeoForge, Java 21 and mod metadata consistent. Document version compatibility only after checking it.
- Make small, bounded changes. Avoid unnecessary tick loops, network messages and unbounded production buffers.
- Run appropriate builds and focused checks. Claim in-game tests only when they actually occurred; attribute tests performed by TeKatz separately.
- For UI changes, inspect actual screens, handlers and registrations. Do not infer behavior from class names alone.
- Do not present an existing JAR as a new build by renaming its version. Build from the corresponding commit and inspect the resulting JAR and metadata.
- Update documentation with behavior changes, validation results and remaining work.

## Pack files and storage

- Track configuration, custom recipe datapacks, quest files, resources and the actual island NBT in this repository.
- Do not commit build outputs, third-party mod JARs, worlds, logs, caches, credentials or local infrastructure helpers. The Gradle wrapper is an intentional build-dependency exception.
- Deliver built JARs through build artifacts or an explicitly requested release.
- Before publishing the pack, test a fresh instance from the export: island, spawn, ME contents, power, world-type restriction, recipes and quests.
