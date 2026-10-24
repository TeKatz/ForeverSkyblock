# Context command repairs

This note documents the October 24, 2026 repair pass for editor commands that behaved correctly in the base layout model but were neutralized by the later source-faithful machine presentation owners.

## Root causes

### Cut / Delete

The base command removed selected models correctly. In a loaded machine preview, the source/runtime owner subsequently interpreted a missing source-backed control as damaged preview structure and reconstructed it. The visible result was a no-op even though the command itself had executed.

Source-backed controls now use an editor tombstone when cut/deleted: the model remains hidden so the runtime/source invariant is satisfied, while the element disappears from the canvas, Elements list and Hierarchy. Ordinary editor-created elements are still removed normally. Paste always creates an ordinary detached editor copy.

This is intentionally an editor-layout operation. Java apply-back continues to support the source-backed geometry described in the Phase 9 workflow; it does not silently delete Java controls.

### Bring to front / Send to back

The legacy commands reordered `_layout.Elements`, but the Washing source renderer, exclusive machine runtime and machine-presentation lifecycle later assigned fixed semantic `Panel.ZIndex` values. Those final writers won over the list order.

Explicit front/back commands persist a narrow editor Z override (`+900` / `-900`). The normal semantic layer values remain untouched until the user invokes one of these commands. Selection adorners remain above the override range and machine backdrops remain below it.

The first implementation restored this Z override through a deferred `ContextIdle -> SystemIdle` callback. Runtime testing showed that this was not a reliable final-writer boundary while the editor's render-priority timeline dispatcher remained active: the override was applied, then a later machine presentation pass could restore its semantic Z value before the idle callback ran. The correction is now synchronous and deterministic: a final `LayoutUpdated` handler is registered after all machine presentation owners and reapplies only already-declared editor overrides when the visual owner has actually changed them.

### Edit Text

The dialog already updated `GuiElementModel.Content`. Runtime synchronization and Washing fidelity then replaced source-owned titles/status/time text with current runtime values, making the edit appear to fail.

An explicit text edit stores an editor text override. The normal runtime text remains authoritative for untouched elements; only the deliberately edited element is restored after the final presentation owner has run. Undo/redo and layout JSON preserve the override.

As with Z-order, the original idle-dispatch restoration was not a reliable final writer under the active render dispatcher. Text override visuals carry a signature and are now restored synchronously by the last registered layout-finalization handler. If the correct override visual is already present, the pass is a no-op; this prevents the old LayoutUpdated/render-storm failure mode from returning.

## Custom textures

Right-clicking a single Texture element now offers **Choose custom texture…**. It opens the Windows file picker and accepts PNG, JPEG, BMP, GIF and TIFF files supported by WPF. The selected absolute path is stored in `TexturePath` and marked as an explicit custom-texture override so runtime scene decorators do not immediately replace the chosen image.

The existing repository asset browser is unchanged. Custom external paths are local-machine paths and validation continues to warn if such a file later becomes unavailable.

## Interaction consistency

The repaired semantics are shared by the context menu and keyboard commands:

- `Ctrl+X` — source-aware Cut
- `Ctrl+V` — detached Paste
- `Delete` — source-aware Delete
- `F2` — explicit text override
- `Ctrl+A` — selects visible/non-suppressed editor elements

The persistent **Bring front** and **Send back** buttons are also rebound to the repaired layer commands.

No polling timer and no replacement machine renderer were introduced. Explicit editor overrides are finalized synchronously only when such an override exists; text and texture visuals are signature-guarded and Z changes are value-guarded, so unchanged presentation is not rebuilt.
