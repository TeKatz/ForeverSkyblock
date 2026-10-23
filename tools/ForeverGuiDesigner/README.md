# Forever GUI Designer

A small Windows-first visual editor for Forever Skyblock GUI layouts.

The editor intentionally stores positions in Minecraft GUI-local coordinates rather than desktop/window coordinates. This keeps layouts useful for later Java Screen/Menu integration and independent of the player's resolution.

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
- Player Inventory (9 x 3 slots as one movable element)
- Hotbar (9 slots as one movable element)

### Forever Skyblock asset browser

Use **Repo…** to select the local ForeverSkyblock repository. When the application is launched from inside the repository it attempts to detect it automatically.

The asset browser scans:

`mod/src/main/resources/assets/forever_skyblock/textures`

It shows PNG previews with nearest-neighbor scaling. Double-clicking a texture adds it to the canvas. If exactly one Texture element is selected, the selected repository asset is assigned to that element instead.

Texture references are stored as repository-relative asset paths such as:

`textures/item/forever_core.png`

This avoids putting machine-specific absolute Windows paths into layout JSON files.

## Build

```powershell
dotnet build .\tools\ForeverGuiDesigner\ForeverGuiDesigner.csproj -c Release
```

Run during development with:

```powershell
dotnet run --project .\tools\ForeverGuiDesigner\ForeverGuiDesigner.csproj
```

The GitHub Actions workflow also publishes a self-contained `win-x64` build so the application can be tested without installing the .NET SDK.

## Layout format

A layout contains its logical Minecraft GUI size and a list of elements:

```json
{
  "name": "Condenser",
  "width": 176,
  "height": 166,
  "elements": [
    {
      "id": "input",
      "type": "slot",
      "x": 39,
      "y": 30,
      "width": 18,
      "height": 18,
      "content": "",
      "texturePath": null,
      "visible": true
    }
  ]
}
```

## Next steps

- Minecraft font rendering.
- Guides, rulers and visible snap grid.
- More Forever-specific machine widgets (input/output slots, progress arrows, energy/fluid indicators, lock/recipe buttons).
- Import existing Java Screen/Menu coordinates.
- Export generated layout constants/data for the mod.
- Machine GUI templates based on the actual Forever Skyblock screens.
- Hover/pressed states and lightweight animation previews.
