# Forever GUI Designer changelog

## Development

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
