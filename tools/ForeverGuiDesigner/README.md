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

### Condenser state editor

- MK I / MK II
- Base / Mixing / Freezing / Garden
- 0 / 25 / 50 / 75 / 100% progress preview
- state-specific editable anchors without rewriting coordinates

### Detailed Washing scene

Washing now mirrors much more of the real `WashingScreen` / `MachineProcessEffects.washing` presentation:

- Running / Idle / Blocked / No water states
- water connected / required surface
- animated water ripples
- animated process particles
- moving meter highlight
- animation play/pause
- editable status, panels, input/output labels, process scene, time, recipe summary and hints

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

- Extend rich process scenes to Refining, Crucible and Ascension.
- Resolve machine labels from language files.
- Add guides/rulers and project-aware snap grid.
- Add Minecraft font rendering.
- Add reviewed apply-back for selected Java constants.
- Add hover/pressed states and more runtime-state previews.
