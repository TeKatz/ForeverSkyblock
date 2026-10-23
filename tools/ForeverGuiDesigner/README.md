# Forever GUI Designer

Small Windows-first visual editor for Forever Skyblock GUI layouts.

This is the first MVP. It deliberately stores positions in Minecraft GUI-local pixels rather than desktop/window coordinates, so the same layout data can later be consumed by the mod.

## Current MVP

- WPF / .NET 8 Windows desktop application
- 176×166 Minecraft-style preview canvas by default
- Add Slot, Text, Button and Texture placeholder elements
- Select and drag elements on the canvas
- Configurable 1/2/4/8 px snapping
- 200/300/400/600% preview zoom
- Property inspector for id, x, y, width, height and content
- Arrow-key movement (1 px, or 5 px with Shift)
- Delete selected element
- JSON open/save
- Example Condenser draft layout

## Build and run

Install the .NET 8 SDK on Windows, then from the repository root:

```powershell
dotnet build .\tools\ForeverGuiDesigner\ForeverGuiDesigner.csproj
dotnet run --project .\tools\ForeverGuiDesigner\ForeverGuiDesigner.csproj
```

The build output is intentionally ignored by the repository.

## Layout format

```json
{
  "name": "Example",
  "width": 176,
  "height": 166,
  "elements": [
    {
      "id": "input",
      "type": "slot",
      "x": 39,
      "y": 34,
      "width": 18,
      "height": 18,
      "content": "",
      "texturePath": null,
      "visible": true
    }
  ]
}
```

Coordinates are local to the Minecraft GUI. They are not screen-resolution coordinates.

## Next implementation steps

1. Repository auto-detection and direct asset browser for `mod/src/main/resources/assets/forever_skyblock`.
2. Load PNG textures with nearest-neighbor rendering instead of placeholder rectangles.
3. Minecraft font preview and dedicated player-inventory/hotbar components.
4. Multi-selection, alignment tools, copy/paste and undo/redo.
5. Forever-specific widgets such as machine slots, recipe buttons, progress arrows and energy/fluid bars.
6. Export/import bridge to the actual Java Screen/Menu layout definitions.

The editor is intentionally independent from the mod build for now; no Python, Node.js or Electron runtime is required.
