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
- Smart center/edge guides while dragging.
- Equal horizontal/vertical distribution.
- Eight resize handles directly on the canvas; Shift keeps aspect ratio, Alt resizes from center.

### Layers / hierarchy

Phase 8 adds a project-editor style hierarchy above the flat element list:

- grouped / ungrouped tree
- hide/show
- lock/unlock (locked layers cannot be dragged on the canvas)
- automatic groups and ungroup
- layer up/down
- drag-and-drop layer order and group reassignment
- collapse/expand through the tree

### Minecraft-oriented components

- Slot
- Text
- Button
- Texture
- Player Inventory
- Hotbar

### True WYSIWYG text

Phase 8 replaces generic WPF text in editable Text/Button previews with deterministic pixel rendering and Minecraft-like behaviour:

- one-pixel shadow
- left / center / right alignment
- 0.25x–4x scale
- native-pixel placement
- visible overflow warning when the measured text no longer fits the element bounds

The renderer uses the editor's compact deterministic bitmap fallback where a vanilla font atlas is not part of the selected project assets, so the canvas stays pixel-stable and self-contained.

### Real item / block previews

Slots can carry project-aware preview metadata. For mod items the resolver reads the real `models/item/*.json`, follows its texture reference and loads the referenced PNG from the selected ForeverSkyblock repository instead of guessing a filename.

Preview controls include stack count, durability bar, ghost opacity and glint indication. Ascension's imported core slot is seeded from the actual `forever_skyblock:forever_core` item declared by `AscensionMenu`.

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

Washing mirrors the real `WashingScreen` / `MachineProcessEffects.washing` presentation with Running / Idle / Blocked / No water states, water surface, animated ripples/particles, moving meter highlight, timing, recipe summary, hints and persistent per-frame visuals.

### Phase 8 machine scenes

Crucible, Ascension Chamber and Refining Condenser now receive editable WYSIWYG scene layers derived from the current Java Screen/Menu coordinates rather than hand-written generic mockups.

- **Crucible:** heat/tank/freezer/process panels, input/output labels, melt/freeze meters and the real Washing-layout footprint (248×286).
- **Ascension Chamber:** title/core area, counter-rotating conversion rings, reward panel, conversion meter, recipient panel and real Forever Core input; uses the Ascension footprint (248×270).
- **Refining Condenser:** energy panel/meter, material/catalyst/output scene, refining meter and information panel on the real 248×286 layout.

### Animation timeline

The Phase 8 timeline unifies fixed snapshots and live previewing:

- Play / Pause / Stop
- Loop
- scrub slider
- elapsed / duration display
- 0.25x / 0.5x / 1x / 2x speed
- existing 0/25/50/75/100% snapshots remain available

Timeline ticks only mutate persistent meter/ring/Washing visuals. They never call `RenderLayout()` per frame.

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

## Next steps — Phase 9

- GUI Scale 1 / 2 / 3 / 4 / Auto simulator with selectable target resolution.
- Source live reload for Java/assets.
- Validation for overlaps, out-of-bounds elements, text clipping, invalid assets and inconsistent slot positions.
- Reviewed Java diff/apply-back with selectable unambiguous coordinate changes, backup and undo.
- Extend runtime tooltip/button state previews where useful.
