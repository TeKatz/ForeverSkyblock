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

The asset browser scans:

`mod/src/main/resources/assets/forever_skyblock/textures`

It shows decoded PNG previews with nearest-neighbor scaling. Double-clicking a texture adds it to the canvas. Texture references are stored as repository-relative asset paths such as `textures/item/forever_core.png`.

## Forever project integration

The command bar contains a dedicated **FOREVER SKYBLOCK** workflow.

1. Select the local ForeverSkyblock repository with **Repo…**.
2. Click **Scan project**.
3. Select one of the detected machine screens.
4. Click **Load machine** to create an editable layout from the actual Java sources.
5. Edit the result visually and use **Export Java** to generate GUI-local constants for the mod.

The scanner intentionally reads the project instead of shipping a fixed machine list. It detects classes extending `MachineScreen<...>`, resolves their `MachineLayout` constant, pairs them with their Menu class, and imports the static geometry it can prove from source.

### Project-specific geometry

Forever machine screens do not use the vanilla 176 x 166 assumption. The scanner reads the actual `MachineLayout.java` dimensions and inventory/hotbar coordinates.

It also mirrors `MachineSkin.slot` geometry:

- machine slot frames: 24 x 24, padded 4 px around vanilla `Slot.x` / `Slot.y`
- player inventory/hotbar frames: 20 x 20, padded 2 px around vanilla slot coordinates

Player inventory and hotbar slots are imported individually for these project templates so their real Forever spacing remains editable.

### Visual machine preview

The project scan reconstructs a non-interactive machine backdrop from the actual Forever rendering style. The preview mirrors `MachineSkin.frame`, statically resolvable `MachineSkin.panel(...)` regions, the shared player inventory panel and matching repository block/item textures. Imported slots, buttons and editor widgets remain editable above this backdrop.

The **Visual preview** checkbox can hide this reconstructed background without changing layout data or Java export.

### Condenser state editor

The Condenser has a state-aware preview workflow:

- tier: **MK I** / **MK II**
- mode: **Base** / **Mixing** / **Freezing** / **Garden**
- progress preview: **0 / 25 / 50 / 75 / 100%**

Changing state does not rewrite layout coordinates. The editor switches preview and visibility of imported elements while keeping separate state anchors for state-specific surfaces.

### Detailed Washing scene

Washing now has a richer preview based directly on `WashingScreen` and `MachineProcessEffects.washing`:

- runtime states: **Running**, **Idle**, **Blocked**, **No water**
- connected-water / water-required presentation
- animated washing ripples
- animated process particles
- moving progress-meter highlight
- shared progress percentage selector
- animation play/pause control
- editable status text and indicator
- editable water panel and water-state label
- editable input/output labels
- editable washing process surface and animated process-scene anchor
- editable processing-time label
- editable recipe-summary anchor
- editable hint panel and hint labels

The animation is editor presentation state only. It does not rewrite layout coordinates or add temporary textures to the mod.

### Forever widgets

The Forever Skyblock command bar provides direct widget insertion for:

- **Panel** — MachineSkin-style panel surface
- **Meter** — previewable progress meter
- **Lock** — pixel-style Forever lock indicator
- **Status** — machine status indicator

These are ordinary editable layout elements and can be moved, resized, aligned, copied, saved and included in Java coordinate export.

### Theme-safe command controls

ComboBox controls in the command bars receive explicit high-contrast text, surface and border colors at runtime. This prevents Windows themes from producing unreadable light-on-light or dark-on-dark selected values.

### Java export

**Export Java** creates a small Java layout class containing `GUI_WIDTH`, `GUI_HEIGHT`, and constants for editable machine slots/buttons/visual elements.

For imported machine slots, exported X/Y values are converted back from the 24 x 24 visual frame to the vanilla `Slot.x` / `Slot.y` coordinates expected by the Menu. Player inventory coordinates remain derived from `MachineLayout`.

The export includes comments identifying the imported Screen, Menu and `MachineLayout` source. It is intended as a reviewed handoff rather than silently rewriting Java source files.

## Build

```powershell
dotnet build .\tools\ForeverGuiDesigner\ForeverGuiDesigner.csproj -c Release
```

Run during development with:

```powershell
dotnet run --project .\tools\ForeverGuiDesigner\ForeverGuiDesigner.csproj
```

The GitHub Actions workflow publishes a self-contained `win-x64` build so the application can be tested without installing the .NET SDK.

## Layout format

A layout contains its logical Minecraft GUI size and a list of elements:

```json
{
  "name": "Condenser",
  "width": 248,
  "height": 352,
  "elements": [
    {
      "id": "machine_slot_1",
      "type": "slot",
      "x": 26,
      "y": 57,
      "width": 24,
      "height": 24,
      "content": "30,61",
      "texturePath": null,
      "visible": true
    }
  ]
}
```

## Next steps

- Extend rich process-scene previews to Refining, Crucible and Ascension.
- Resolve more machine labels directly from language files.
- Add visible guides/rulers and a project-aware snap grid.
- Add Minecraft font rendering instead of desktop fallback fonts.
- Add a reviewed apply-back workflow for selected Java constants.
- Add hover/pressed states and more runtime-state previews.
