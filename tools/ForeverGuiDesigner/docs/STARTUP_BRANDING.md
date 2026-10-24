# Startup and branding presentation

The Forever GUI Designer startup/branding layer is intentionally isolated from machine rendering and editor runtime ownership.

## Startup behavior

- The main window is maximized before it is first shown.
- A fresh launch replaces the constructor's internal example layout with a new empty `GuiLayoutModel` before the first `Show()` call, so no example or machine is visible at startup.
- No machine is auto-loaded.

## Empty workspace presentation

`MainWindow.StartupPresentation.cs` owns a display-only kitten overlay above the workspace column.

- The overlay is not a `GuiElementModel` and never enters `_layout.Elements`.
- It is not selectable and has `IsHitTestVisible = false`.
- It is not part of export, validation, Java apply-back, or undo/redo state.
- It is shown only when the editable layout has no elements and there is no active Forever machine layout.
- A loaded machine suppresses the overlay even if its editable element list is temporarily empty.
- New/Open/Load Machine and element-list changes update the presentation explicitly; there is no `LayoutUpdated` polling or repeating timer.

## Branding assets

- `Assets/empty-state-kitten.png` is the transparent empty-workspace artwork.
- `Assets/ForeverGuiDesigner.ico` is a separate Windows application icon with 16, 24, 32, 48, 64, 128, and 256 pixel images.
- The icon is embedded as both a WPF resource for the window and the project `ApplicationIcon` for the executable/taskbar identity.

## Regression boundary

This layer does not own or mutate machine presentation, runtime selection, simulator rendering, or the established preview renderer. Changes to this feature should remain inside the startup/presentation layer unless a verified root cause requires otherwise.
