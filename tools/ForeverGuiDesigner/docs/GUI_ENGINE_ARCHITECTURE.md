# Forever Studio shared GUI engine

This document records the Phase-1 audit and migration boundary for the **04 Recipes & GUI** milestone.
It describes the current implementation on `feature/forever-studio-runtime-production`; it does not
replace the repository-wide integrated-state documentation for `main`.

## Non-negotiable architecture rule

Forever Studio has one GUI engine and one GUI document contract. Machine Builder, standalone GUI
editing and future Menu/Overlay/HUD contexts are contextual entry points into that engine, not
separate canvas or style systems.

The migration must preserve this round trip:

`Machine Builder -> GUI Editor -> Machine Builder -> Build Mod`

Unknown document, element, style/effect and context metadata must survive a round trip whenever the
current build can safely preserve it.

## Phase-1 audit

| Concern | Standalone GUI editor | Machine Builder 04 | Production runtime | Migration consequence |
| --- | --- | --- | --- | --- |
| Document | `GuiLayoutModel` | `ForeverStudioMachineGuiDefinition` + bindings | compiled runtime JSON | Centralize on `GuiDocument`; keep compatibility adapters only |
| Canvas | `PreviewCanvas` in `MainWindow.xaml.cs` plus Phase 8/9 extensions | private `_guiDesignerCanvas` in `MainWindow.MachineBuilderGuiDesignerPhase4.cs` | native Minecraft coordinates | Machine Builder canvas must be removed after reusable editor surface exists |
| Geometry | element `X/Y/Width/Height` | binding property bag `x/y/width/height` | widget + slot coordinates | One geometry owner in `GuiDocument` |
| Element types | Slot/Text/Button/Texture/Inventory/Hotbar plus later presentation extensions | item/output slot, progress, energy, fluid, status, value presets | widget type strings | Machine widgets become normal GUI elements + bindings |
| Assets/textures | repository PNG browser + texture paths | library asset IDs / simple generated visuals | compiled resource/model output | Shared asset references and one asset browser |
| Inspector | existing standalone inspector + later Phase 8 metadata | separate widget inspector | n/a | Reusable inspector with context-provided binding picker |
| Layering | list order, groups, locks, visibility, Phase 8 hierarchy | numeric `order` property only | render order | Explicit shared layers/order; legacy ordering remains an adapter concern |
| Bindings | no canonical runtime binding model before this milestone | `SourceId` + `sourcePortId/sourceType` property bag | source node/port in runtime widget | Shared `GuiBindingModel`; context is data, not widget type |
| Styles/effects | presentation logic spread across editor phases | none as a reusable contract | hard-coded widget rendering | Shared styles/effects must precede Machine-only hover/glow/shadow work |
| Visual states | source preview/runtime-specific code | preview values + machine state | graph state/status | Shared visual-state contract; runtime binding added later |
| Serialization | `System.Text.Json` directly on `GuiLayoutModel` | Machine Project serializer | compiler-owned runtime JSON | Shared document must preserve forward-compatible fields |
| Recipes | not part of standalone GUI layout | legacy quick form + multi-resource text fields | production Multi-I/O/Fluid/FE runtime | Keep runtime; replace only authoring UX after GUI engine convergence |

## Shared contract introduced in Phase 1

`Models/GuiDocument.cs` is the canonical authoring contract. It currently contains:

- canvas geometry plus canvas settings;
- GUI elements;
- layers;
- asset references;
- generic context bindings;
- styles;
- reusable effects;
- visual states;
- extension-data bags on every forward-evolving contract object.

`GuiLayoutModel` now derives from `GuiDocument`. The legacy class name remains so the large existing
editor surface can move incrementally without a flag-day rewrite. Existing layout JSON continues to
use the same `name`, `width`, `height` and `elements` fields; the new sections are additive.

`GuiElementModel` also carries shared `LayerId`, `AssetId`, `StyleId`, `EffectIds`, generic properties
and JSON extension data. This lets an older/editor-specific surface move a known element without
silently deleting metadata it does not actively edit.

## Compatibility boundary

Phase 1 intentionally does **not** replace `MainWindow.MachineBuilderGuiDesignerPhase4.cs` yet.
Removing that canvas before the reusable standalone surface can be embedded would regress a working
Machine Builder. The duplicate canvas is now explicitly considered migration debt, not a second
supported GUI architecture.

Likewise, Java runtime widget rendering remains unchanged in Phase 1. Runtime evolution follows only
after the authoring contract and reusable editor surface are stable.

## Next refactoring steps

1. Add a Machine GUI context adapter that exposes Function Graph outputs as `GuiBindingModel`
   choices without exposing raw node/port IDs in normal UI workflows.
2. Extract the standalone canvas, selection, layer/hierarchy and inspector behavior behind reusable
   editor-surface components.
3. Embed that exact surface in Machine Builder 04 and retire `_guiDesignerCanvas`.
4. Move current Machine GUI binding geometry into `GuiDocument` elements and compile runtime widgets
   from the shared document.
5. Centralize GUI assets, styles and effects on the same surface.
6. Only then replace the recipe text fields with the visual Item/Fluid browser workflow.
7. Run explicit Machine -> standalone editor -> Machine round-trip tests before expanding runtime
   presentation.

## Safety rules

- Do not delete or reinterpret unknown extension data during normalization.
- Do not create a Machine-only style/effect implementation.
- Do not make the runtime JSON the authoring source of truth.
- Keep current production compiler behavior fail-closed where runtime semantics are unsupported.
- Preserve legacy layout and Machine Project loading throughout the migration.
