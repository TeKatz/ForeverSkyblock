# Forever GUI Designer

A Windows-first visual editor for Forever Skyblock GUI layouts.

The editor stores positions in Minecraft GUI-local coordinates rather than desktop/window coordinates. This keeps layouts useful for Java Screen/Menu integration and independent of the player's resolution.

## Current feature set

### Layout editing

- Create, open and save JSON GUI layouts.
- Drag elements directly on the Minecraft-sized preview canvas.
- Pixel snapping at 1, 2, 4 or 8 px.
- Preview zoom from 200% to 600%.
- Property inspector for id, position, size and text content.
- Keyboard nudging: arrow keys = 1 px, Shift + arrow keys = 5 px.
- Delete selected elements.
- Copy/paste with Ctrl+C / Ctrl+V.
- Undo/redo with Ctrl+Z / Ctrl+Y.
- Ctrl+A selects all elements.

### Multi-selection and layout tools

- Extended selection in the element list.
- Ctrl-click selection directly on the preview.
- Drag multiple selected elements as one group.
- Align left, horizontal center, right, top, vertical center or bottom.
- Bring selected elements to the front or send them behind other elements.

### Minecraft-oriented components

- Slot
- Text
- Button
- Texture
- Player Inventory
- Hotbar

### Forever Skyblock asset browser

Use **Repo…** to select the local ForeverSkyblock repository. When the application is launched from inside the repository it attempts to detect it automatically.

The asset browser scans `mod/src/main/resources/assets/forever_skyblock/textures`, shows decoded PNG previews with nearest-neighbor scaling, and stores repository-relative texture paths.

### Block Builder

The Block Builder works directly against the selected Forever Skyblock repository and keeps edits staged in memory until the explicit review/apply step.

- Scans blockstates and resolves their block-model parent chains and texture references.
- Provides a 3D model viewport with element selection plus direct move/scale gizmos.
- Supports element geometry, rotation, face, UV and texture editing with immediate preview updates.
- Keeps edits for multiple block models staged while switching between block assets instead of discarding the previous model when another block is opened.
- Supports visual editing of simple `variants` blockstates.
- Weighted variant arrays and `multipart` blockstates are deliberately read-only. Multipart assets remain browsable through a representative model preview; their conditions, apply lists and weighted alternatives are never rewritten by the simple variant editor.
- Review collects all staged block models, blockstates and PNG edits for the active repository in one change set.
- Validation checks model geometry, legal model/blockstate rotations, model and texture targets, UVs, cull faces, tint indices, duplicate variants and generated JSON roundtrips.
- Apply stages every payload, rechecks reviewed source hashes, atomically replaces existing files with rollback backups, verifies the written SHA-256 values and then reloads the Block Builder from disk.

No Block Builder source file is written by direct viewport, model, UV, texture or blockstate editing. Repository writes only occur after the explicit **Review changes → Apply** confirmation.

## Forever project integration

1. Select the local ForeverSkyblock repository with **Repo…**.
2. Click **Scan project**.
3. Select one of the detected machine screens.
4. Click **Load machine**.
5. Edit visually, run **Validate**, then use **Review Java…** to inspect and apply safe source-backed geometry changes.
6. **Export Java** remains available for standalone GUI-local constant snippets.

The scanner detects `MachineScreen<...>` classes, resolves `MachineLayout`, pairs Screen and Menu sources and imports statically provable geometry.

### Visual machine preview

The project scan reconstructs the common `MachineSkin.frame`, statically resolvable panels, the player inventory surface and matching repository textures. Imported slots/buttons remain editable above that preview.

Washing preview initialization is normalized in the same refresh that inserts Phase 6 elements, so loading the machine no longer depends on toggling **Visual preview** off/on to restore the generated backdrop and decorated controls.

Generated/imported panel `Content` values are editor metadata only and are suppressed in the WYSIWYG preview. Helper labels such as `Washing process`, `Water connection`, `Washing hints`, `Condensing` or `Automation / upgrades` no longer leak into the actual GUI rendering.

### Machine attachment previews

The attachment selector models physical machine neighbors independently from runtime state where the source GUI supports those combinations.

- **Condenser Mark I:** None / Mixing / Freezing / Mixing + Freezing.
- **Condenser Mark II:** keeps the established Base / Mixing / Freezing / Garden state selector unchanged; Garden remains its separate special presentation path.
- **Crucible:** None / Freezing / Tank / Tank + Freezing. The established combined Tank + Freezing presentation remains the default so existing previews do not silently change.
- Removing the Crucible Freezing attachment disables the impossible `Freezing` runtime state until the attachment is restored.

The combined Condenser Mark I preview reuses the real Mixing recipe/process path and adds the Freezing route, meter and slots on top instead of inventing a second disconnected machine renderer.

### Condenser state editor

- MK I / MK II tier preview.
- Mark I attachment combinations are selected through **ATTACHMENTS**.
- Mark II retains Base / Mixing / Freezing / Garden.
- 0 / 25 / 50 / 75 / 100% progress preview.
- **Active Processing** continuously loops progress from 0 → 100% without rebuilding the Canvas.
- State-specific editable anchors without rewriting coordinates.

### Detailed Washing scene

Washing mirrors the real `WashingScreen` / `MachineScreen` / `MachineProcessEffects.washing` presentation with runtime states, water status, animated water/process effects, timing, meters, recipe summary and hints.

The **SCENE** controls are shown only for Washing Basin; all other machine previews use the runtime owner without a redundant scene selector. The source header is normalized to `Washing Basin` beside the machine icon.

### Phase 8 — True WYSIWYG

- Deterministic Minecraft-style pixel text previews with shadow, alignment, scale controls and clipping warnings.
- Project-aware item previews resolved from real item model JSON to repository textures, including stack count, durability, ghost and glint metadata.
- Hierarchy/layer editing with groups, visibility, locks and drag/drop ordering.
- Eight resize handles, pixel snapping, Shift aspect-ratio preservation and Alt center-resize.
- Resize-handle mouse-down is isolated from the Washing/non-Washing runtime selection synchronizers. The same `Thumb` keeps capture for the complete drag and the runtime presentation is restored only after resize completion.
- Smart center/edge guides and equal horizontal/vertical distribution.
- Unified Play/Pause/Stop/Loop/Scrub timeline with 0.25x / 0.5x / 1x / 2x speed.
- Editable reconstructed Crucible, Ascension Chamber and Refining Condenser scenes based on their real Screen/Menu/MachineLayout coordinates.

#### Phase 8 performance guard (0.8.1+)

The first 0.8.0 implementation attached hierarchy rebuilding, WYSIWYG decoration and resize-handle recreation directly to WPF `PreviewCanvas.LayoutUpdated`. Because those operations invalidate layout themselves, navigation and animation could trigger a self-amplifying UI-thread refresh storm.

0.8.1 permanently gates that broad callback after initialization. A lightweight visual-generation detector now coalesces refresh work and performs a full Phase 8 refresh only when `RenderLayout()` actually replaces the editable visual generation. Timeline frames, guide movement and normal WPF layout passes therefore no longer rebuild the hierarchy or overlays. Resize handles follow drag operations through coordinate-only updates. The current interaction guard additionally prevents source-runtime selection callbacks from replacing the active resize `Thumb` on mouse-down.

### Phase 9 — Source workflow

#### GUI Scale simulator

- Simulates the GUI centered in a Minecraft-style logical viewport instead of changing stored GUI-local coordinates.
- Presets cover 854×480 through 3840×2160 framebuffer resolutions and GUI scales 1x–4x plus Auto.
- The simulator reports framebuffer size, effective logical viewport and the GUI's physical framebuffer footprint.

#### Java/asset live reload

- Watches `mod/src/main/java` and `assets/forever_skyblock` recursively.
- Debounces IDE/file-system write bursts before rescanning.
- Clean imported layouts reload automatically.
- If the current imported layout has editor changes, source and visual assets refresh without discarding those local edits.
- Manual **Reload source** asks before replacing edited geometry.

#### Validation

- Detects duplicate/empty ids, invalid dimensions and out-of-bounds geometry.
- Checks fixed Forever machine-slot geometry and player-slot frame sizing.
- Reports missing texture references and overlapping interactive slots/buttons.
- Validation issues are selectable and jump back to the affected element.
- Errors block Java apply-back; warnings remain reviewable.

#### Reviewed Java diff/apply-back

- **Review Java…** creates a read-only before/after diff before any source file is touched.
- Source-backed `MachineButton` x/y/width/height geometry can be applied back to the matching Screen.
- Machine-slot movement applies to resolved Menu constants or literal `MachineSkin.slot(..., true)` coordinates.
- Shared Java constants are changed only when every linked slot requests the same value; conflicts are blocked instead of guessed.
- Editor-only buttons are explicitly reported and are not silently invented in Java.
- Apply rechecks that source files still match the reviewed originals, stages all writes, uses replacement backups and reloads the machine after success.

### Forever widgets

- Panel
- Meter
- Lock
- Status

These remain ordinary editable layout elements.

### Theme-safe command controls

Top ComboBoxes receive explicit high-contrast text/surface/border colors at runtime, avoiding unreadable selected values caused by Windows theme chrome.

### Self updater

The editor supports **Development** and **Stable** channels. It checks the GitHub Releases API for the channel release, extracts the embedded build commit and GitHub-provided SHA-256/size for `ForeverGuiDesigner.exe`, and compares that commit with the running build.

- Startup checks are informational only; installation always requires **Update & Restart**.
- Version checks use a short hard timeout and can never inherit the long EXE-download timeout.
- If the GitHub Releases API is unavailable on the local network, the editor automatically falls back to the channel's tiny `update-manifest.json` release asset with its own short timeout.
- The update executable is verified by size and SHA-256 before replacement.
- A temporary helper process waits for the editor to exit, replaces the executable, restarts it and cleans itself up.
- Development follows `feature/forever-gui-designer-mvp`; Stable follows `main`.

### Java export

Export converts imported machine-slot frames back to the vanilla `Slot.x` / `Slot.y` coordinates and identifies source Screen/Menu/Layout in comments.

## Build

```powershell
dotnet build .\tools\ForeverGuiDesigner\ForeverGuiDesigner.csproj -c Release
```

```powershell
dotnet run --project .\tools\ForeverGuiDesigner\ForeverGuiDesigner.csproj
```

The workflow publishes a self-contained `win-x64` build and refreshes the selected fixed self-update channel on successful push builds.

## Next steps

- Expand apply-back bindings beyond statically resolved machine slots and `MachineButton` geometry.
- Richer tooltip/button runtime-state previews.
- Optional validation autofixes for safe, mechanical issues.