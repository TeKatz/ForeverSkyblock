# Forever GUI Designer changelog

## Development

### 0.9.0 — Phase 9 source workflow

- Added a GUI Scale simulator with 854×480 through 3840×2160 framebuffer presets, fixed 1x–4x scale choices and an Auto mode that renders the current GUI in the effective logical viewport without changing stored coordinates.
- Added debounced live source watching for Java and `assets/forever_skyblock` files. Clean imported machines reload automatically, while active editor changes are preserved instead of being overwritten by external source saves.
- Added a validation panel for duplicate ids, invalid/out-of-bounds geometry, fixed Forever slot sizes, missing texture references and overlapping interactive controls. Validation errors block apply-back and selecting an issue jumps to its element.
- Added reviewed Java diff/apply-back for statically bound `MachineButton` geometry, Menu-backed machine-slot constants and literal `MachineSkin.slot(..., true)` coordinates.
- Shared source constants are protected against conflicting per-element edits; ambiguous mappings are rejected instead of guessed.
- Java apply-back rechecks reviewed source contents immediately before writing, stages replacements with backups, rolls back on failure and reloads the active machine after success.
- Editor-only buttons are reported during review and are never silently invented in Java.
- Added explicit Phase 9 controls to the existing project toolbar and documented the source-editing workflow.

### 0.8.7 — Runtime-sourced Washing preview

- Fixed the remaining Washing progress split-brain: the exclusive timeline and visible process/meter surfaces now use the same progress value instead of the meter still reading the old Phase 5 progress selector.
- Washing playback duration is sourced from the current runtime recipe/default (`processing_time` / `WashingRecipe.DEFAULT_TIME`) instead of the old hard-coded 5-second editor demo.
- Washing preview text and representative recipe metadata are read from the selected repository (`en_us.json` plus the current washing recipe), so the editor follows the actual runtime source rather than stale Phase 6 strings.
- Current runtime preview therefore tracks `WASHING BASIN`, water-source wording, current hints, the representative `4 × Stone Meal → Raw Iron` recipe and the 8.0-second processing time from the repository.
- Bumped the editor to 0.8.7.

### 0.8.6 — Washing ownership hardening

- Fixed the remaining delayed visual writer: Phase 5 was still refreshing on every `LayoutUpdated` and its broad `_panel_` / `_meter_` / `_status` predicates matched Phase 6 Washing elements, allowing the GUI to mutate again seconds after the user stopped interacting.
- Washing exclusive mode now gates the Phase 5 presentation loop in addition to Phase 6, Visual Fidelity, Machine Presentation and the legacy Washing Source renderer.
- Fixed direct canvas selection bypassing the 0.8.5 selection restore path. Canvas clicks suppress `ElementsList.SelectionChanged`, so Washing now synchronizes source chrome from the bubbling canvas mouse event after the base selection handler has run.
- Hierarchy and element-list selection use the same synchronous selection restoration path, and Phase 8 resize handles are synchronized immediately so stale handles cannot remain after clearing a selection.
- Scene and Animate controls are explicitly restored through `UpdatePhase6ControlAvailability()` when exclusive Washing ownership starts, preventing the Washing animation controls from remaining hidden after a gated Phase 6 refresh.
- Removed Phase 8 as an active Washing progress clock. Its timer is now only a heartbeat; `_phase8Playing` remains false while Washing is active and the exclusive controller advances the 0–5 s timeline itself.
- Play resumes Active Processing, restarting from 0 when playback is already at 5 s; Pause freezes the current value; Stop deterministically resets to 0; scrubbing pauses at the selected value; Loop and speed are honored by the same single clock.
- The exclusive progress setter updates the timeline slider/text and Active Processing tag atomically, preventing Pause/Stop from being overwritten by a second scheduler.
- Bumped the editor to 0.8.6.

### 0.8.5 — Exclusive Washing runtime controller

- Replaced the stacked Washing hotfix queues with one exclusive synchronous presentation owner while Washing is active. The older Phase 6 refresh, Visual Fidelity, Machine Presentation and legacy Washing Source queues are gated instead of racing to rewrite the same WPF Borders.
- Selection no longer relies on changing native machine Border thickness. Washing restores its source presentation in the same input event and draws a separate selection adorner, eliminating the one-pixel content shifts that made the whole GUI appear to jump when an element was clicked.
- Apply properties and machine load now finish the complete Washing presentation synchronously in the same dispatcher turn, so the generic `RenderLayout()` body is not exposed as an intermediate frame.
- Phase 8 compatibility is explicit: Washing text remains `MinecraftPixelText`, machine slots are wrapped as already-decorated Phase 8 item surfaces, and machine buttons use stable Minecraft-pixel button chrome that Phase 8 will not replace with frameless text.
- The Recipes button therefore keeps its chrome across selection changes and has deterministic hover/selection styling without waiting for a later idle-priority fidelity pass.
- Active Processing now has one live clock: the Phase 8 timeline. The old Phase 6 animation timer and Phase 7 processing timer remain stopped while Washing owns the preview.
- Play selects/resumes Active Processing, Pause freezes both processing and decorative motion, Stop resets to 0.0 s and remains restartable, slider scrubbing pauses at the selected value, and non-looping playback stops cleanly at 5.0 s.
- Running/Idle/Blocked/No water and Animate now gate the same requested playback state rather than independently stopping/restarting competing timers.
- Other machine editors retain their existing Phase 5–8 presentation paths; exclusive ownership is released when Washing is no longer the active layout.
- Bumped the editor to 0.8.5.

### 0.8.4 — Deterministic Washing render pipeline

- Fixed the real reason `Apply properties` could still leave the Washing preview in the generic grey `RenderLayout()` state: the 0.8.3 final compositor was scheduled at `DispatcherPriority.SystemIdle`, which can be starved indefinitely by the editor's continuously ticking Render-priority animation timers.
- Added a deterministic Washing coordinator that runs at `DispatcherPriority.Background` after the older Phase 5/6 background decorators, so it executes reliably while animations are active instead of waiting for the dispatcher to become fully idle.
- The coordinator is triggered by machine load, Apply properties, element selection, Washing state/animation/progress changes and actual preview visual-generation replacements.
- Strengthened the Washing slot invariant: all three runtime source slots are checked independently instead of treating any non-empty `machine_slot_*` set as complete.
- Partial/broken scans now recreate only the missing source slots at the runtime frame coordinates 38/74, 170/74 and 198/74.
- If a visible machine-slot model exists without a generated WPF visual, the preview is rebuilt once and the source compositor is applied again to the new visual generation.
- Final Washing composition explicitly restores every visible machine slot and forces its visual above the process panel after older decorators have finished.
- Bumped the editor to 0.8.4.

### 0.8.3 — Washing source compositor hotfix

- Replaced the generated Washing preview image as the final visual authority with a deterministic WPF compositor derived directly from `WashingScreen`, `MachineScreen`, `MachineLayout` and `MachineSkin` geometry.
- The final Washing compositor runs at `DispatcherPriority.SystemIdle`, after the older Phase 5–8 decorators and the 0.8.2 presentation coordinator, so a later generic decorator cannot win after `Apply properties`.
- Rebuilds the complete MachineSkin frame and the shared 212×92 player-inventory panel directly in the canvas instead of depending on a temporary preview PNG surviving `RenderLayout()`.
- Re-applies MachineSkin slot chrome to every imported player/hotbar slot and gives the three Washing machine slots a dedicated foreground layer above process panels.
- Added a defensive source-slot fallback for old/broken scans: if no machine slots exist, Washing recreates the runtime frames at 38/74, 170/74 and 198/74 (corresponding to vanilla slot coordinates 42/78, 174/78 and 202/78).
- Re-applies Washing panels, state marker, text, Recipes button and stable process/meter surfaces after every visual-generation replacement.
- Keeps the corrected 0.8.2 animation gating unchanged.
- Bumped the editor to 0.8.3.

### 0.8.2 — Machine presentation lifecycle hotfix

- Added a final machine-presentation coordinator that runs after all Phase 5–8 background/context-idle decorators, so machine visuals no longer fall back to the generic grey renderer after load or `Apply properties`.
- Restores the source-derived MachineSkin backdrop explicitly after every canvas rebuild instead of relying on an incidental `LayoutUpdated` callback.
- Added semantic Z-layers for machine panels, process surfaces, slots, text and buttons; Washing input/output slots can no longer be hidden behind the generated process panel.
- Unified Washing animation gating: motion only advances while the Washing scene is `Running` and `Animate` is enabled.
- `Idle`, `Blocked`, `No water` and `Animate=false` now stop the stable Washing timer, Active Processing and any running Phase 8 timeline instead of allowing hidden progress loops to keep driving the scene.
- Paused Washing scenes collapse transient particles and meter shine while keeping the selected progress value as a static preview.
- Dynamic Washing time text is updated without rebuilding the canvas.
- Bumped the editor to 0.8.2.

### 0.8.1 — Phase 8 performance hotfix

- Fixed a severe WPF UI-thread regression where `PreviewCanvas.LayoutUpdated` rebuilt the Phase 8 hierarchy, redecorated every editor visual and removed/recreated all resize handles on virtually every layout pass.
- The old broad `LayoutUpdated` refresh path is now permanently gated after Phase 8 initialization.
- Added a lightweight visual-generation detector that coalesces refresh work and only performs a full Phase 8 refresh when `RenderLayout()` actually replaces the editor visuals.
- Resize handles now follow dragged elements through coordinate-only updates instead of being recreated during movement.
- Timeline and smart-guide layout invalidations no longer cascade into hierarchy/overlay rebuild storms.
- Bumped the editor to 0.8.1.

### 0.8.0 — Phase 8: True WYSIWYG

- Added deterministic Minecraft-style pixel text previews with one-pixel shadow, left/center/right alignment, scale controls and overflow warnings.
- Added project-aware item previews resolving real `models/item/*.json` texture references, with stack/durability/ghost/glint metadata.
- Added a hierarchy view with groups, visibility, locks, collapse/expand, layer controls and drag/drop ordering.
- Added eight canvas resize handles with pixel snap; Shift preserves ratio and Alt resizes from the center.
- Added smart center/edge guides and equal horizontal/vertical distribution tools.
- Added a persistent animation timeline with play, pause, stop, looping, scrubbing and 0.25x / 0.5x / 1x / 2x speed.
- Timeline animation mutates persistent visuals only; it does not rebuild the canvas per frame.
- Added editable reconstructed scenes for Crucible, Ascension Chamber and Refining Condenser based on their real Screen/Menu/MachineLayout coordinates.
- Ascension's real `forever_skyblock:forever_core` input is resolved through the repository item model and texture path.
- Bumped the editor to 0.8.0.

### Earlier development

- Replaced the updater's `await` return path with an explicit ThreadPool continuation plus `Dispatcher.BeginInvoke`, so successful network results are posted back to WPF deterministically instead of relying on a captured synchronization context.
- Moved all updater network resolution off the WPF UI thread and added a visible Cancel action plus `%LOCALAPPDATA%\ForeverGuiDesigner\updater.log` diagnostics with a one-click Log button.
- Added an outer update-check watchdog so the UI always leaves `Checking…` after at most 18 seconds even if Windows networking does not honor an inner HTTP cancellation promptly.
- Fixed self-update verification on Windows by disposing the staged executable download stream before reopening the file for SHA-256 verification.
- Added a GitHub release-manifest fallback for update checks.
- Added Active Processing preview and suppressed internal Forever panel metadata labels in machine previews.
