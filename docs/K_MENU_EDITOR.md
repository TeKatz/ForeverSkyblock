# K-menu Studio — 0.25.1 development polish

The K-menu Studio was introduced on `feature/kmenu-admin-editor` as the 0.25.0 development build. The follow-up branch `feature/kmenu-editor-polish` keeps the same editor data model, publication rules and gameplay boundary while addressing the first real-player usability feedback before integration into `main`.

This document describes development branches, not a public release. The integrated `main` baseline remains 0.24.1 until a separate merge is approved. Machines, recipes, the fixed starter island and existing skill effects remain outside the editor.

## Open and work

Open the K-menu and use the pencil button beside Close, or run `/forevereditor`. The server checks permission level 2 or integrated-server ownership for every authoritative editor operation.

The editor remains a three-part workspace: content selection, a native Minecraft preview/graph canvas and a typed property inspector. The 0.25.1 polish reduces the amount of permanently visible chrome and adds explicit guidance:

- the top toolbar now keeps **Undo**, **Redo**, **Files…**, **Validate**, **Publish** and **More…** as the primary workflow;
- **Files…** contains save/load draft plus import/export;
- **More…** contains the destructive reset and server-revision reconciliation actions;
- the active workspace tab is visibly marked;
- **Help** or **F1** opens a quick-start screen with mode-specific instructions and keyboard shortcuts;
- the footer explains the current mode and selected element instead of showing only an opaque internal ID;
- inspector properties are grouped into collapsible sections instead of one long unstructured list.

`Ctrl+S` saves the local draft. `Ctrl+Z` and `Ctrl+Shift+Z` perform undo/redo. Enter applies pending inspector fields. Closing a dirty editor still offers to save the draft.

Nothing in a draft changes another player's rules. **Validate** checks the draft. **Publish** presents a confirmation, uploads bounded chunks, rechecks authorization, references, registries and the expected server revision, then atomically replaces the server document. Failed validation/publication preserves the active document.

## Layout workspace

The layout editor can select header groups, navigation tabs, search/category regions, content panels, repeated card templates, profile sections, detail-window regions and custom decorative elements.

The 0.25.1 toolbar uses descriptive state labels rather than terse developer labels:

- **Page: …** chooses the live K-menu page being previewed;
- **Viewport: 640×360 / 480×270 / 944×522** switches the logical preview size;
- **Interaction: Select / Try** separates authoring from real widget interaction;
- **Edit: Move / Resize** controls drag behavior;
- **All sizes / This size** selects a common style override or the active responsive-size override;
- **Detail: Menu / Quest detail / Shop detail / Contract detail** selects the preview surface;
- **State: Live / Locked / Available / Completed** chooses the sample presentation state;
- **Focus preview** temporarily hides both side panels to maximize the canvas.

Dragging moves or resizes the selected element. Grid snapping uses the configured grid; hold Alt for free movement. Wheel input zooms the layout canvas, empty-space dragging pans it and middle-click resets the layout view. Exact numeric values remain available in the inspector.

Width/height `0` retains the original calculated size. START/CENTER/END anchors decide how replacement dimensions sit inside the original rectangle. Colors use AARRGGBB; blank values keep the original color. Supported renderers expose font scale, item scale and visibility.

### Stable editor preview

The first 0.25.0 player test found that ordinary selection clicks recreated the native preview and therefore restarted its reveal animation every time. 0.25.1 deliberately renders **Select** mode with an editor-only preview copy whose animation duration is zero. Selection, property editing and viewport changes therefore remain visually stable.

**Try** mode still uses the actual configured animation duration and routes input through the native K-menu widgets, so animations and interactions can be tested intentionally. Preview purchases, respecs, test-point actions and detail actions remain non-destructive.

Changing the logical viewport now rebuilds the editor controls as well as the preview, so the viewport button text always reflects the active size.

## Inspector organization

The right inspector now groups related fields into collapsible sections. Depending on the workspace these include:

- **Position & size**, **Anchors**, **Appearance**, **Alignment** for layout elements;
- **Content & status** for authored entries;
- **Progression & rewards**, **Objectives**, **Dependencies & node** for quests;
- **Pricing & requirements**, **Package contents**, **Node** for Shop products;
- **Contract rules** for contracts;
- **Chapter order** for chapters;
- **Server & recovery** and **Editor behavior** for Settings.

Collapsing a section changes only editor presentation. It does not change the document being authored.

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

Drag nodes, pan empty canvas and use the wheel to zoom. **Auto arrange** lays out the quest graph and **Center view** restores the default view. Node coordinates and radius can also be edited in the inspector.

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
- No new challenge, package or balance content is introduced by the editor polish.
- The fixed `forever_me_start.nbt` is not part of this branch.

## Validation history

The original 0.25.0 implementation source `910f902ff853b0051e5ecfb96a60bb773f901db1` passed Actions run `39200171912`: 96 unit tests, client startup/model/mixin smoke, 95 required GameTests on each NeoForge runtime, 32 native-client fixture stages and seven native input checks. Its test artifact was `ForeverSkyblock-1.21.1-NeoForge-0.25.0.jar`.

The 0.25.1 code and metadata source `56246f2a57672d0fdf966cf865b596eeeaee0019` passed Actions run `39204247635`: Java 21 build/compileGameTestJava, client startup/model/mixin smoke, all native K-menu visual fixtures, the dedicated-server regression suite and the full NeoForge **21.1.259** integration suite. The visual fixture set was inspected after the run; it confirms the reorganized toolbar/inspector presentation and that switching from the 640×360 preview to 480×270 updates the visible viewport label to `Viewport: 480×270`.

The production artifact `ForeverSkyblock-Mod` is artifact `12165525325`. Its GitHub artifact ZIP digest is `sha256:90986733c297b1ff06e4c6ce22447d34a136e7ee574df2a19847b61c0167f369`. The contained production JAR is `ForeverSkyblock-1.21.1-NeoForge-0.25.1.jar`, 5,466,801 bytes, SHA-256 `e530f8ae16c1131893073a7ba529b87521528005a78559a9a45bf9dbfdcb8b6c`; its packaged `neoforge.mods.toml` reports version `0.25.1`.

Real-player usability acceptance remains separate: the automated fixture verifies rendering/input paths and the two reported bugs were addressed in code, but TeKatz's follow-up in-game use is authoritative for whether the polish feels good enough. This branch remains unmerged and publishes no release.
