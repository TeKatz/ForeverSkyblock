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

## Forever project integration

1. Select the local ForeverSkyblock repository with **Repo…**.
2. Click **Scan project**.
3. Select one of the detected machine screens.
4. Click **Load machine**.
5. Edit visually and use **Export Java** for GUI-local constants.

The scanner detects `MachineScreen<...>` classes, resolves `MachineLayout`, pairs Screen and Menu sources and imports statically provable geometry.

### Visual machine preview

The project scan reconstructs the common `MachineSkin.frame`, statically resolvable panels, the player inventory surface and matching repository textures. Imported slots/buttons remain editable above that preview.

Washing preview initialization is normalized in the same refresh that inserts Phase 6 elements, so loading the machine no longer depends on toggling **Visual preview** off/on to restore the generated backdrop and decorated controls.

Generated/imported panel `Content` values are editor metadata only and are suppressed in the WYSIWYG preview. Helper labels such as `Washing process`, `Water connection`, `Washing hints`, `Condensing` or `Automation / upgrades` no longer leak into the actual GUI rendering.

### Condenser state editor

- MK I / MK II
- Base / Mixing / Freezing / Garden
- 0 / 25 / 50 / 75 / 100% progress preview
- **Active Processing** continuously loops progress from 0 → 100% without rebuilding the Canvas
- state-specific editable anchors without rewriting coordinates

### Detailed Washing scene

Washing mirrors the real `WashingScreen` / `MachineProcessEffects.washing` presentation with runtime states, water status, animated water/process effects, timing, meters, recipe summary and hints.

### Phase 8 — True WYSIWYG

- Deterministic Minecraft-style pixel text previews with shadow, alignment, scale controls and clipping warnings.
- Project-aware item previews resolved from real item model JSON to repository textures, including stack count, durability, ghost and glint metadata.
- Hierarchy/layer editing with groups, visibility, locks and drag/drop ordering.
- Eight resize handles, pixel snapping, Shift aspect-ratio preservation and Alt center-resize.
- Smart center/edge guides and equal horizontal/vertical distribution.
- Unified Play/Pause/Stop/Loop/Scrub timeline with 0.25x / 0.5x / 1x / 2x speed.
- Editable reconstructed Crucible, Ascension Chamber and Refining Condenser scenes based on their real Screen/Menu/MachineLayout coordinates.

#### Phase 8 performance guard (0.8.1)

The first 0.8.0 implementation attached hierarchy rebuilding, WYSIWYG decoration and resize-handle recreation directly to WPF `PreviewCanvas.LayoutUpdated`. Because those operations invalidate layout themselves, navigation and animation could trigger a self-amplifying UI-thread refresh storm.

0.8.1 permanently gates that broad callback after initialization. A lightweight visual-generation detector now coalesces refresh work and performs a full Phase 8 refresh only when `RenderLayout()` actually replaces the editable visual generation. Timeline frames, guide movement and normal WPF layout passes therefore no longer rebuild the hierarchy or overlays. Resize handles follow drag operations through coordinate-only updates.

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

- GUI Scale simulator.
- Java/asset source live reload.
- Validation for overlaps, out-of-bounds geometry, text clipping, invalid assets and inconsistent slots.
- Reviewed Java diff/apply-back with selectable safe changes and backup/undo.
- Richer tooltip/button runtime-state previews.
