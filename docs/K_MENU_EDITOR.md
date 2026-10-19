# K-menu Studio — 0.25.0 development branch

Implemented on `feature/kmenu-admin-editor`, based on integrated **0.24.1** commit `c7246d00456c9c4c23905f496316a72bbcd473e8`. This document describes the branch, not an integration into `main` or a public release. Machines, recipes, the fixed starter island and existing skill effects remain outside the editor.

## Open and work

Open the K-menu and use the pencil button beside Close, or run `/forevereditor`. The server checks permission level 2 or integrated-server ownership for every editor operation. Ordinary players receive the published presentation and quest state, not permission to publish it.

The workspace contains a searchable element/content list, a native Minecraft preview or graph canvas, and a property inspector. Scroll the inspector to reach lower properties. **Apply properties / Enter** validates and commits a field edit to the draft. A failed edit remains visible for correction. **Undo / Redo** operates on draft changes; one completed drag is one history entry. **Ctrl+S** saves the draft locally. Closing a changed draft offers to save it.

Nothing in a draft changes another player's rules. **Check** validates it; **Publish** presents a confirmation, uploads bounded chunks, rechecks authorization, references, registries and the expected server revision, then atomically replaces the server document. All connected players receive the new catalog without a restart. Failed validation/publication preserves the active document.

## Layout

- Select header groups, navigation tabs, the search field, category rail, content panels, card templates and their title/description/icon/reward areas, profile sections and detail-window sections.
- Drag to move; switch **Move / Resize** to resize. Grid snapping uses the configured grid; hold Alt to move freely. Numeric properties allow exact changes.
- Width/height **0** retains the original calculated size. START/CENTER/END anchors decide how replacement dimensions sit inside the original rectangle.
- Colors use **AARRGGBB**; blank retains the original color. Font/item scale and visibility are available where the selected renderer supports them.
- Switch between 640×360, 480×270 and 944×522 logical preview viewports. **All / Size** selects a common override or a small/compact/wide override. Minecraft's existing responsive layout still provides defaults.
- **Try** routes input through the preview's native widgets. Purchase, respec, test-point and detail actions cannot modify gameplay from this preview.
- **Detail** previews challenge, package and contract windows. **State** switches between live, locked, available/partial and completed sample states for quest/node presentation.
- Add decorative text, panels, registered item icons or progress bars. Text can show SP, PP, Tier, playtime, Island Level, deaths, kills or completed challenges. These are display bindings, not executable scripts.
- Settings control grid/snap, entry/reveal duration, scroll speed and the live quest graph/card view.

Repeated cards use one shared template. This is deliberate: changing a reward label's alignment should affect every generated card. Some responsive layout relationships still come from Java; this is not a Java source-code editor or a universal editor for other mods' screens.

## Quests and chapters

The initial document mirrors all **20 existing challenges / 58 SP**. Their IDs, original event adapters, original Tier 0/I milestones and saved completions remain intact. Titles, descriptions, icons, chapter, enabled state, rewards and prerequisites can be authored. Legacy evidence targets can be reduced, but not increased beyond their original evidence capacity. Built-in trigger types remain protected; duplicate a built-in challenge to create a custom task structure.

New quest IDs use `forever_skyblock:quest/…`. A quest supports up to 16 individually identified objectives:

| Objective | Authoritative source |
| --- | --- |
| CRAFT | Player crafting result event |
| SMELT | Player collection of furnace/smelting output |
| PICKUP | Items actually transferred from an item entity into player inventory |
| KILL | Vanilla personal kill attribution; exact entity ID or `minecraft:any_hostile` |
| DIMENSION | Player dimension transition |
| BREAK / PLACE | Uncancelled block event, checked after the operation at the end of the server tick |
| ADVANCEMENT | Earned advancement ID |

Automated output is not automatically credited as personal crafting/smelting. PICKUP counts pickup actions and can intentionally be repeated by dropping/picking up items; use another objective if unique acquisition is required. BREAK/PLACE similarly measure those actions, not unique blocks. New observation mechanisms, machine-operation objectives, custom abilities and arbitrary scripts require code.

Objectives and prerequisite quests independently support **ALL / ANY**. Required Tier and SP/PP reward values are server-owned. Add chapters and order them; connect quests in the graph to express unlocks. A cycle, missing dependency, duplicate ID or unknown custom registry target prevents publication.

Quest evidence is reduced in original event order and saved once per affected player per server tick. Pending evidence is flushed on logout, server stopping and before activating another document. Per-player event queues, objective counters, identity guards, document size and graph sizes are bounded.

Permanent completions remain permanent: changing a title, reward, target or prerequisite never pays an already completed quest again. Reduced targets for incomplete custom quests are evaluated on the next qualifying action. Existing progress is retained by stable quest/objective IDs. Published task identities/types/targets are protected; use a new quest ID when changing a task's meaning. Draft tasks can be added/removed before publication. Published entries are disabled rather than deleted. The editor's progress shares the existing permanent player-profile storage and SP/PP accounting; it does not award extra Island Level or replace the separate FTB Quests book.

## Node editor

Drag nodes, pan empty canvas, use the wheel to zoom, and use **Arrange / Center**. Node coordinates and radius can also be edited in the inspector. Geometry is shared with the live menu, including circular hit areas. Quest edges come from prerequisite IDs and show ALL/ANY semantics. **Link** selects a prerequisite, then its dependent quest; **Unlink** removes that relationship.

The Shop includes its existing permanent-skill/Tier roots, existing world upgrades/packages, new packages and quest-prerequisite nodes. Root positions can be changed without changing their gameplay meaning. **Requires / Unlink** selects a quest prerequisite for the selected product. Existing product branches retain their real Tier rules; visual repositioning cannot invent purchases or replace server checks.

## Shop and contracts

- Edit product names/descriptions/icons, prices, Tier, enabled state and quest requirements. New products use `forever_skyblock:shop/custom/…` and support up to 16 item stacks through the existing owner/world-bound package-delivery mechanism.
- Package contents become immutable under a published ID. Existing package contents are protected immediately. Duplicate a package to create a different offer; price changes affect future purchases. Paid receipts retain their original costs, and disabled offers remain resolvable for existing claims and recovery.
- World-upgrade effects and research/skill effects remain the existing Java implementations. The editor does not create arbitrary new gameplay effects.
- Edit contracts' text/icon, enabled state, target, reward (1–10 SP), required Tier and supported resource/boss type. New IDs use `forever_skyblock:contract/custom/…`.
- Resource contracts use the existing explicit consumption, write-ahead intent, forced player-data checkpoints and idempotent payout/recovery. A published contract's type/item is immutable; an in-flight intent keeps its original item count and reward.

## Files, import and repository export

The authoritative instance/server file is **`config/forever_skyblock/kmenu.json`**. The previous valid document is kept as `.json.bak`. Invalid or missing-primary-with-backup files are reported rather than silently overwritten; edited challenge/contract rewards and new purchases fail closed until the operator reloads a repaired file or restores a valid backup. Existing machines and ordinary skill effects remain available. Restoring a backup retains newer published IDs as disabled entries.

Client drafts are under `forever_skyblock/editor/<connection-key>/draft.json`, with exports in its `exports/` directory. Import accepts a JSON document or an editor ZIP, including file drag/drop. Export offers:

1. One ZIP containing `pack/config/forever_skyblock/kmenu.json` and a revision/SHA-256 manifest. Imports read only those two bounded entries and never extract supplied paths.
2. Direct export to a selected local ForeverSkyblock Git checkout. The editor validates the repository root and destination and writes the same pack-owned configuration path. It does not commit, push, merge or store GitHub credentials.

The Gradle build includes an exported pack configuration as a bundled default. A deployed instance/server config takes precedence. Existing installations therefore need the exported config distributed with the updated pack as well as the JAR when replacing an existing override.

**Reconcile** performs a field-level three-way merge when another administrator publishes while a draft is open. Independent changes merge automatically; conflicting paths are shown with a choice between draft/server values. A revision lease on publication prevents silent overwrites. Exported files can be committed and reviewed using ordinary Git; sending one ZIP also remains supported.

## Compatibility and validation

- Minecraft 1.21.1, Java 21, NeoForge default 21.1.249 and pack runtime 21.1.259.
- Existing `skills-9` payloads/profile schemas 4/5/6 and world-progress schema 2 are retained. The mandatory additional `kmenu-editor-1` channel requires matching new client/server builds.
- Custom aggregate evidence uses revision 2 within the existing extensible challenge-progress map. No existing milestone, contract receipt, delivery, world identity or core-redemption data is cleared.
- No new challenges, packages or balance changes are shipped as default gameplay content.
- The fixed `forever_me_start.nbt` must retain SHA-256 `1ae2e4b779b1c6b56320aede0af2a711d4bd247a976771c95f633e05c1beb0c1`.

Focused tests cover document bounds/references/cycles, original default parity, multi-objective reducers, prerequisites, replay guards, reward/accounting persistence, respec retention, immutable published identities/packages, target/reward edits, history, merge conflicts, rollback, atomic backups and ZIP integrity/path rejection. Dedicated GameTests cover real packet codecs/size bounds and default gameplay parity. Opt-in native client fixtures extend the existing K-menu rendering/input checks with editor pages, alternate viewport/detail/language previews and mouse-drag undo/redo. Test fixtures remain outside the production JAR.

Final CI source/artifact hashes and any remaining acceptance limits are recorded here after validation. A fixture run is not a substitute for TeKatz testing the editor in the full player instance, including its actual skin/resources, multiplayer publication conflicts and server restart with exported configuration.
