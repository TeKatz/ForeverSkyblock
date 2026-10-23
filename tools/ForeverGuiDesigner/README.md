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

It shows PNG previews with nearest-neighbor scaling. Double-clicking a texture adds it to the canvas. Texture references are stored as repository-relative asset paths such as `textures/item/forever_core.png`.

## Forever project integration

The toolbar now contains a dedicated **Skyblock** workflow.

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

### Java export

**Export Java** creates a small Java layout class containing `GUI_WIDTH`, `GUI_HEIGHT`, and constants for editable machine slots/buttons/visual elements.

For imported machine slots, exported X/Y values are converted back from the 24 x 24 visual frame to the vanilla `Slot.x` / `Slot.y` coordinates expected by the Menu. Player inventory coordinates are deliberately not duplicated because those remain derived from `MachineLayout` in the mod.

The export includes comments identifying the imported Screen, Menu and `MachineLayout` source where available. It is intended as a safe handoff/snippet for integration rather than silently rewriting Java source files.

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

- Import more dynamically calculated panels, labels and progress meters from Forever machine screens.
- Add first-class Forever widgets for progress meters, panels, locks and machine status indicators.
- Add visible guides/rulers and a project-aware snap grid.
- Add Minecraft font rendering.
- Add a reviewed apply-back workflow for updating selected Java constants rather than only exporting snippets.
- Add hover/pressed states and lightweight animation previews.
