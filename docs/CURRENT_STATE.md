# Current integrated state

Updated October 21, 2026. `main` is **0.26.4** after the integration of the K-menu Studio stack, the Crucible Lava/Obsidian system, Condenser Mark II automation upgrades, GitHub Actions usage cleanup and the Nether Fortress material-processing work now present on `main`.

## Baseline

| Item | Current value |
| --- | --- |
| Mod version | **0.26.4** |
| Minecraft | **1.21.1** |
| Java | **21** |
| Current pack runtime | **NeoForge 21.1.262** |
| Default development NeoForge | **21.1.249** |
| Current supplied mod-list snapshot | **87 enabled artifact labels** (`Modliste(5).txt`, October 21, 2026) |
| Player profile schema | **dynamic 4 / 5 / 6** |
| World-progress schema | **2** |
| Progression protocol | **skills-9** plus integrated K-menu editor channel |
| Current main merge | `fccb38b76cbfd6519ce39a6f380f0639eec02162` |
| Release | Active development; repository integration is not a public pack release |

TeKatz reported testing the current listed modpack state on **NeoForge 21.1.262** with **no errors observed**. This is user-reported pack acceptance. No fresh runtime log accompanied the list, so the repository does not infer a new loaded-mod-ID count from that test.

The fixed starter island remains protected and unchanged by the 0.25/0.26 integrations.

## Integrated feature stack

### 0.24.x — K-menu presentation

The rebuilt K-menu remains integrated with scrolling categories, readable Challenge/Contract details, round Shop nodes, compact player profile, real player statistics, persistent world/tier rules and the existing server-owned transaction model.

### 0.25.x — K-menu Studio

The K-menu Studio is no longer a development-only branch. It was integrated into `main` through PR #19.

Authorized users can edit native layouts, quests/chapters, Shop nodes/packages and supported Contracts. The editor supports drafts, undo/redo, native previews, validation, server-owned publication, conflict reconciliation, backups and JSON/ZIP/local-repository export. Existing progression IDs, purchase recovery and published-content safety remain authoritative.

See [K_MENU_EDITOR.md](K_MENU_EDITOR.md) for the editor workflow and compatibility rules. Historical branch/version language in that document should be read as development history, not current repository state.

### 0.26.x — Crucible Lava/Obsidian progression

The **Crucible** is integrated.

- Internal Lava capacity: **4,000 mB** with visible fill progression.
- Input rates: **8 Cobblestone** or **40 Stone Fragments** produce **1,000 mB Lava**.
- Heat tiers: **Torch → Campfire → Lava → Mekanism Superheating Element**.
- The Torch is intentionally extremely slow and exists as the earliest usable heat source.
- The Crucible scans adjacent blocks for a Fluid Tank and Freezing Chamber.
- A Fluid Tank enables Lava routing; a Freezing Chamber enables Obsidian production.
- If both are present, the GUI exposes a route selector.
- Obsidian uses a real timed freezing phase and progress bar rather than instant conversion.
- Hopper/item capability input/output and drain-only fluid capability support automation with hoppers and pipes/cables.
- JEI contains the Crucible processing entries.
- Jade reports the active heat/melting tier.
- The Mekanism **Basic Fluid Tank** recipe is overridden to be redstone-free so Redstone can be introduced later as its own progression step.

The Crucible GUI includes animated Lava level/surface, heat/melt activity and freeze effects. The final 0.26.4 polish keeps those effects anchored to the central Lava meter and progress bars instead of unrelated slots.

### 0.26.x — Condenser Mark II automation upgrades

Condenser Mark II has **3 upgrade capacity** and three physical upgrade slots.

| Upgrade | Effect | Limit |
| --- | --- | --- |
| Processing Speed Card | 20% / 35% / 50% shorter normal processing duration | 3 cards, 1 capacity each |
| Garden Accelerator Card | +25% Garden speed per card | 2 cards, 1 capacity each |
| Buffer Expansion Card | hidden standard-stack input/output queues | 2 cards, 1 capacity each |
| Batch Processing Card | two normal recipe runs per cycle when capacity/input allow | 1 card, 2 capacity |

The four cards have individual player-facing textures and are exposed through normal crafting/JEI paths.

Every Mark II output has an independent policy:

- **Block** — stop when the output and its buffer are full.
- **Void Excess** — keep everything that fits and delete only overflow.
- **Void All** — delete all produced items for that output.

Garden primary and secondary outputs can be configured independently. Hopper/cable extraction works with the internal buffers, and effective processing/Garden timing is reflected in both world and GUI animations.

## Active early material route

`Wood → Stone Fragments → Stone Meal / Dirt → Ice / Water → Farm → Mud → Packed Mud → Mud Bricks → Washing Basin → Raw Iron → Iron → Condenser Mark II → Garden Chamber → Crucible → Lava / Obsidian`

Current key behavior:

- **Condenser:** 1 Log → 4 Stone Fragments in 6 seconds. Existing Plank and Leaves recipes remain active.
- **Mixing:** Stone Meal + Sapling → Dirt; Dirt + Ice → Mud; Mud + Wheat → Packed Mud. Mark II can also automate the renewable Oak Sapling route.
- **Freezing Chamber:** retains the existing 32-item Ice counter and also acts as the Crucible's adjacent Obsidian-cooling module.
- **Washing Basin:** 4 Stone Meal → 1 Raw Iron in 8 seconds with adjacent source water.
- **Garden Chamber:** retained Oak Sapling → Logs + Leaves; retained Wheat Seeds → Wheat + Seeds. Garden remains Mark-II-only.
- **Mark II overflow:** full outputs no longer have to halt a long-running setup when the player explicitly selects Void Excess or Void All.

## Current modpack snapshot

The authoritative current artifact-label snapshot is [MODLIST_SNAPSHOT.md](MODLIST_SNAPSHOT.md). Important current values include:

- **JEI 19.69.0.457**;
- **Mekanism 10.7.19.85** plus **Mekanism Generators 10.7.19.85**;
- **Forever Production Monitor 5.0.1**;
- supplied **Forever Recipe Editor 1.4.2 (2)** artifact;
- supplied **Forever Skyblock 0.26.4-Mekanism-Automation-Test** artifact;
- **FTB XMod Compat 21.1.13**;
- **Sophisticated Backpacks 3.26.14.2222** and **Sophisticated Core 1.5.16.2422**.

The list is a launcher artifact-label snapshot, not a final distribution manifest.

## Meta progression boundary

The existing challenge/shop/research/perk/world-purchase rules remain in place. The editor can author supported content but does not turn arbitrary scripts or unsupported machine attribution into safe server-owned objectives.

Tier II–IV, final late-game balance, additional packages/contracts, deeper technology progression and first-acquisition routes for further resources remain active design areas.

## CI and validation

The historical automated validation chain for the integrated 0.26.4 work used the repository's configured runtimes, including full pack-runtime integration on **NeoForge 21.1.259**. Those historical CI claims remain tied to the runs that produced them and are not rewritten retroactively.

The current installed pack baseline is now **NeoForge 21.1.262**. On October 21, 2026, TeKatz reported testing the modpack on the current 87-label mod set with that loader and finding **no errors**.

The fixed island `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` remains protected pack state.
