# K-menu Studio — 0.25.2 development polish

The K-menu Studio was introduced on `feature/kmenu-admin-editor` as the 0.25.0 development build. `feature/kmenu-editor-polish` now carries the 0.25.2 usability pass based on real-player editor feedback. This remains a development branch and is not merged into `main`.

The editor data model, publication safety, server-owned progression rules, machines, recipes and fixed starter island remain unchanged except for the additive style fields used for editable header labels/icons.

## Open and work

Open the K-menu and use the pencil button beside Close, or run `/forevereditor`. The server checks permission level 2 or integrated-server ownership for every authoritative editor operation.

The workspace still uses a content rail, native Minecraft preview/graph canvas and property inspector, but 0.25.2 deliberately makes the preview the dominant surface:

- the element rail is capped at 108/118 logical pixels instead of occupying the old wide column;
- the inspector is capped at 184/196 logical pixels instead of expanding to roughly one third of the whole screen;
- **Focus** remains available as a one-click full-canvas mode and never appears twice;
- controls use short state labels such as **Challenges**, **640×360**, **Select**, **Move**, **All**, **Menu** and **Live** rather than repeating long prefixes;
- overflowing button labels are clipped safely instead of drawing over neighboring controls;
- German Undo/Redo stay recognizable as the standard editor terms rather than becoming unreadable abbreviations;
- internal layout IDs are presented as friendly names such as `Header / SP`, `SP / Icon`, `SP / Text` and `SP / Wert`.

The top toolbar keeps **Undo**, **Redo**, **Files…**, **Validate**, **Publish** and **More…** as the main workflow. **Files…** contains draft save/load plus import/export. **More…** contains reset and reconciliation. **Help** or **F1** opens quick-start guidance. The footer explains the current workspace and selection.

`Ctrl+S` saves the local draft. `Ctrl+Z` and `Ctrl+Shift+Z` perform undo/redo. Enter applies pending inspector fields. Closing a dirty editor offers to save it.

Nothing in a draft changes another player's rules. **Validate** checks the draft. **Publish** confirms, uploads bounded chunks, rechecks authorization/references/registries/server revision, then atomically replaces the active server document. Failed validation/publication preserves the active document.

## Layout workspace

The layout editor can select header groups, navigation tabs, search/category regions, content panels, repeated card templates, profile sections, detail-window regions and custom decorative elements.

The main layout controls are:

- page selector for the live K-menu page;
- logical viewport selector for 640×360, 480×270 and 944×522;
- **Select / Try** interaction mode;
- **Move / Resize** direct manipulation;
- common or current-size responsive override;
- detail surface selector;
- sample state selector;
- **Focus** for a full-width preview.

Dragging moves or resizes the selected element. Grid snapping uses the configured grid; hold Alt for free movement. Wheel input zooms the layout canvas, empty-space dragging pans it and middle-click resets the layout view. Numeric properties remain available for exact editing.

Width/height `0` retains the original calculated size. START/CENTER/END anchors decide how replacement dimensions sit inside the original rectangle. Supported renderers expose font scale, item scale and visibility.

### Header sub-elements

Existing header groups are no longer opaque single blocks. The native header renderer now exposes separate selectable hit boxes for:

- SP icon, label and live value;
- PP icon, label and live value;
- Shop Tier icon, label and live value;
- Playtime icon, label and live value;
- Island Level icon, label and live value.

Clicking one of these directly in the preview selects that exact leaf. The left rail exposes the same leaves with friendly names. Parent groups still move/resize as one unit from their uncovered panel area.

Header labels support individual English/German text overrides through **Edit text…**. Leaving an override blank restores the built-in localized label. Dynamic values such as the current SP amount remain live values rather than becoming hard-coded editor text. Header pictograms support **Choose icon…**, which opens a searchable registered-item picker; choosing **Default** restores the original approved header artwork.

Published/imported 0.25.0/0.25.1 documents remain decodable: absent header override fields normalize to blank/default values. Overrides are bounded to 120 characters per language.

### Visual color picker

Text, background, border and decoration color properties still retain their exact AARRGGBB representation for portable documents, but users no longer need to type that representation manually. A `#` button beside each supported color property opens a native picker with:

- Alpha, Red, Green and Blue sliders;
- common palette swatches;
- live color preview;
- optional exact AARRGGBB editing;
- **Apply**, **Default** and **Cancel** actions.

**Default** clears the override and returns to the renderer's normal color.

### Stable preview and K-menu motion fix

Select mode keeps the editor preview stable rather than replaying its preview reveal whenever the editor rebuilds. Try mode still uses the real configured K-menu animation timing and native widget input.

A separate K-menu bug was identified in the live menu: `MenuPanel` instances carried a small birth-time vertical entry offset. When a fresh server snapshot rebuilt the Challenges/category panels a few seconds after the user had already opened the page, the newly-created panels replayed that movement, making an idle menu visibly shift again. 0.25.2 removes that per-panel re-entry offset globally. Scrolling, normal page navigation, the page reveal line and detail behavior remain separate mechanisms.

## Inspector organization

The right inspector groups related fields into collapsible sections. Depending on the workspace these include:

- **Position & size**, **Anchors**, **Appearance**, **Alignment** for layout elements;
- **Content & status** for authored entries;
- **Progression & rewards**, **Objectives**, **Dependencies & node** for quests;
- **Pricing & requirements**, **Package contents**, **Node** for Shop products;
- **Contract rules** for contracts;
- **Chapter order** for chapters;
- **Server & recovery** and **Editor behavior** for Settings.

For header label/icon leaves, the primary **Edit text…** or **Choose icon…** action is placed at the top of the inspector instead of below the generic style fields. Collapsing a section changes only editor presentation.

## Quests and chapters

The initial document mirrors all 20 existing challenges / 58 SP. Their IDs, original event adapters, Tier 0/I milestones and saved completions remain intact. Titles, descriptions, icons, chapter, enabled state, rewards and prerequisites can be authored. Built-in trigger identities remain protected; duplicate a built-in challenge to create a custom objective structure.

New quest IDs use `forever_skyblock:quest/…`. A quest supports up to 16 individually identified objectives:

| Objective | Authoritative source |
| --- | --- |
| CRAFT | Player crafting-result event |
| SMELT | Player collection of furnace/smelting output |
| PICKUP | Items actually transferred from an item entity into player inventory |
| KILL | Vanilla personal kill attribution; exact entity ID or `minecraft:any_hostile` |
| DIMENSION | Player dimension transition |
| BREAK / PLACE | Uncancelled block event, checked after the operation at the end of the server tick |
| ADVANCEMENT | Earned advancement ID |

Automated machine output is not automatically credited as personal crafting/smelting. PICKUP and BREAK/PLACE measure actions rather than unique resources. New machine-operation objectives, arbitrary scripts and new gameplay effects still require code.

Objectives and prerequisite quests independently support ALL / ANY. Add chapters and order them; connect quests in the graph to express unlocks. A cycle, missing dependency, duplicate ID or unknown custom registry target blocks publication.

Permanent completions remain permanent. Editing a completed quest cannot pay it again. Existing progress is retained by stable quest/objective IDs. Published task identities/types/targets remain protected; use a new quest ID when changing a published task's meaning. Published entries are disabled rather than deleted.

## Graph workspace

Drag nodes, pan empty canvas and use the wheel to zoom. **Arrange** lays out the quest graph and **Center** restores the default view. Node coordinates and radius can also be edited in the inspector.

For quest links, **Link** selects the prerequisite first and the dependent quest second. **Unlink** uses the same order. Quest edges come from prerequisite IDs and preserve ALL/ANY semantics.

The Shop graph includes the existing permanent-skill/Tier roots, existing world upgrades/packages, new packages and quest-prerequisite nodes. Node position changes presentation only; visual repositioning cannot bypass server-owned purchase rules.

## Shop and contracts

Products can edit names/descriptions/icons, SP/PP prices, Tier, enabled state and quest requirements. New package IDs use `forever_skyblock:shop/custom/…` and support up to 16 item stacks through the existing owner/world-bound delivery mechanism.

Published package contents are immutable under an existing ID. Duplicate the product to create a materially different package. Paid receipts retain their original costs and disabled offers remain resolvable for existing claims/recovery.

Contracts can edit text/icon, enabled state, target, reward (1–10 SP), required Tier and the supported resource/Wither/Dragon types. New IDs use `forever_skyblock:contract/custom/…`. Resource contracts retain the existing explicit consumption, write-ahead intent, checkpoints and idempotent payout/recovery rules.

## Files, import, export and reconciliation

The authoritative instance/server file is `config/forever_skyblock/kmenu.json`; the previous valid document is retained as `.json.bak`. Invalid primary/backup states fail closed rather than silently replacing published gameplay data.

Client drafts live under `forever_skyblock/editor/<connection-key>/draft.json`, with exports in its `exports/` directory. Import accepts a JSON document or bounded editor ZIP, including file drag/drop. Export can create:

1. a portable ZIP containing `pack/config/forever_skyblock/kmenu.json` plus revision/SHA-256 manifest; or
2. the same configuration directly inside a selected local ForeverSkyblock Git checkout.

The editor does not commit, push, merge or store GitHub credentials.

**Merge newer server changes** performs the existing field-level three-way reconciliation when another administrator publishes while a draft is open. Independent field edits merge automatically. Same-field conflicts require an explicit draft/server choice. The publication revision lease prevents silent overwrites.

## Compatibility boundary

- Minecraft 1.21.1, Java 21.
- NeoForge development baseline 21.1.249; pack integration target 21.1.259.
- Existing `skills-9` payloads, player schemas 4/5/6 and world-progress schema 2 are retained.
- The additional mandatory `kmenu-editor-1` network channel requires matching editor-capable client/server builds.
- No new challenge, package or balance content is introduced by this editor usability pass.
- The fixed `forever_me_start.nbt` is not part of this branch.

## Validation history

The original 0.25.0 implementation source `910f902ff853b0051e5ecfb96a60bb773f901db1` passed Actions run `39200171912`. The first 0.25.1 polish source `56246f2a57672d0fdf966cf865b596eeeaee0019` passed Actions run `39204247635`.

The 0.25.2 usability source `f6ad7233f04ea80e8d8d2b6873fdf2a829725dcc` passed Actions run `39209084530`: Java 21 build/compileGameTestJava, client startup/model/mixin smoke, the native K-menu render/input fixture, the dedicated regression GameTests and the full NeoForge 21.1.259 pack-runtime integration suite. The native fixture explicitly checks that `header.sp.label` can be selected independently before the parent SP group is dragged and undo/redo is exercised.

The validated production artifact is `ForeverSkyblock-1.21.1-NeoForge-0.25.2.jar`, SHA-256 `df750cea1e972817749bf5b889a97eef9a333b8d076fb02791c1200be919d1f0`. Automated validation does not replace TeKatz's real-player usability acceptance.