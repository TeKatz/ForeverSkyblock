# Forever Studio Changelog

## Development

### Machine Builder production runtime

- Added the production runtime compiler and deterministic generated-machine registry for executable Forever Studio machines.
- Added runtime schema v2 for item slots, storage slots, processor kinds, Energy, Fluids, Function Graph metadata, GUI widgets, states, and animation bindings.
- Added ordered multi-input / multi-output item recipes, fluid inputs/outputs, FE consumption, and generator FE/t output.
- Added atomic processing semantics so blocked outputs never partially consume item or fluid inputs.
- Added dynamic authored machine menus/screens with synchronized progress, state, Energy, tanks, and animation phase data.
- Added compact S2C runtime-state synchronization for in-world machine rendering outside an open GUI.
- Added persistent machine runtime state across save/reload for inventory, fluids, Energy, active recipe, progress, and authored state.
- Expanded production-safe Function Graph runtime evaluation with flow, branching, resource predicates, recipe discovery/output-space checks, redstone input/control, state transitions, progress values, and event edges.
- Added edge-triggered graph signals for redstone changes, neighbor changes, recipe start/completion, and state-enter transitions.
- Added explicit fail-closed compiler policy for mutation/action nodes whose exactly-once runtime semantics are not implemented yet.
- Added split component models and per-machine component manifests so animations can target authored machine components instead of transforming the whole model.
- Added authored component pivots for rotation/scale animations while preserving `root` whole-machine animation behavior.
- Added one-click `BUILD MOD JAR` and `LAUNCH TEST CLIENT` actions in Review & Build, backed by the repository Gradle wrapper.
- Added repository/runtime contract validation before build/runClient.
- Added automatic local repository alignment to the Studio build channel, preserving Machine Project source and rebuilding stale Studio-generated output against the expected runtime branch.
- Added explicit `UPDATE LOCAL REPOSITORY` in Review & Build.
- Hardened repository sync so unknown working-tree changes and local-only commits are never silently overwritten or stashed.
- Added content-aware handling for shared generated files such as `ForeverSkyblock.java` and `en_us.json`.
- `en_us.json` synchronization now preserves valid local non-Studio translation edits as a key-level delta, updates the repository branch, then conflict-checks and replays those local overrides onto the target language file. Studio-owned machine translation keys are regenerated instead of replayed.
- Same-key language conflicts fail safely and restore the original local language file; malformed language JSON remains a hard sync blocker.
- Language comparison is semantic, so harmless serializer changes such as literal Unicode versus `\uXXXX` escapes do not create false sync blockers.
- Added Windows integration coverage for stale local repositories, paths containing spaces, Machine Project preservation, generated-output cleanup, local language-override replay, target-only language changes, Unicode escaping equivalence, malformed JSON blocking, and Gradle wrapper invocation.

### Verification

- Forever Studio compiler/runtime integration tests cover Multi-I/O, Energy, Fluids, Function Graph metadata/runtime queries, animations, split component geometry/pivots, generated registry capabilities, repository sync, and local language override preservation.
- NeoForge GameTests cover processing, blocked outputs, missing inputs, fluid consumption, persistence/reload, menu sync, animation phases, recipe discovery, output-space separation, and connected fluid queries.
- Client, dedicated-server, visual fixture, and full pack-runtime CI remain part of the production branch verification.
