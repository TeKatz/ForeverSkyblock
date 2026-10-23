# Forever GUI Designer changelog

## Development

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
- Added project-aware item preview resolution from item model JSON to the real repository PNG, plus stack count, durability, ghost and glint preview metadata.
- Added a hierarchy view with groups, visibility, locks, layer movement and drag-and-drop reordering.
- Added eight canvas resize handles with Minecraft-pixel snapping; Shift preserves aspect ratio and Alt resizes from the center.
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
