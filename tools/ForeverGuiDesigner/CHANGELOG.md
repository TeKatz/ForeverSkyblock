# Forever GUI Designer changelog

## Development

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
- Added eight canvas resize handles with pixel snap; Shift preserves ratio, Alt scales from center.
- Added smart center/edge guides plus horizontal/vertical distribution.
- Added a persistent animation timeline with Play / Pause / Stop / Loop / Scrub and 0.25x / 0.5x / 1x / 2x speed.
- Timeline animation mutates persistent visuals rather than rebuilding the canvas.
- Added reconstructed editable scenes for Crucible, Ascension Chamber and Refining Condenser based on their real Screen/Menu/MachineLayout coordinates.
- Ascension's real `forever_skyblock:forever_core` input is resolved through the repository item model and texture path.
- Bumped the editor to 0.8.0.

### Earlier development

- Replaced the updater's `await` return path with an explicit ThreadPool continuation plus `Dispatcher.BeginInvoke`, so successful network results are posted back to WPF deterministically instead of relying on a captured synchronization context.
- Moved all updater network resolution off the WPF UI thread and added a visible Cancel action plus `%LOCALAPPDATA%\ForeverGuiDesigner\updater.log` diagnostics with a one-click Log button.
- Added an outer update-check watchdog so the UI always leaves `Checking…` after at most 18 seconds even if Windows networking does not honor an inner HTTP cancellation promptly.
- Fixed self-update verification on Windows by disposing the staged executable download stream before reopening the file for SHA-256 verification.
- Added a GitHub release-manifest fallback for update checks.
- Added Active Processing preview and suppressed internal Forever panel metadata labels in machine previews.
