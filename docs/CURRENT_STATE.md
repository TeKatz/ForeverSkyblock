# Current integrated state

Updated October 20, 2026. `main` is now **0.26.4** after the integration of the K-menu Studio stack, the Crucible Lava/Obsidian system, Condenser Mark II automation upgrades and the GitHub Actions usage cleanup.

## Baseline

| Item | Current value |
| --- | --- |
| Mod version | **0.26.4** |
| Minecraft | **1.21.1** |
| Java | **21** |
| Pack runtime validation | **NeoForge 21.1.259** |
| Default development NeoForge | **21.1.249** |
| Player profile schema | **dynamic 4 / 5 / 6** |
| World-progress schema | **2** |
| Progression protocol | **skills-9** plus integrated K-menu editor channel |
| Current main merge | `ca49a20864ea08bad1e8972fb77248a52b25bb9e` |
| Release | Active development; repository integration is not a public pack release |

The fixed starter island remains protected and unchanged by the 0.25/0.26 integrations.

## Integrated feature stack

### 0.24.x — K-menu presentation

The rebuilt K-menu remains integrated with scrolling categories, readable Challenge/Contract details, round Shop nodes, compact player profile, real player statistics, persistent world/tier rules and the existing server-owned transaction model.

### 0.25.x — K-menu Studio

The K-menu Studio is no longer a development-only branch. It was integrated into `main` through PR #19.

Authorized users can edit native layouts, quests/chapters, Shop nodes/packages and supported Contracts. The editor supports drafts, undo/redo, native previews, validation, server-owned publication, conflict reconciliation, backups and JSON/ZIP/local-repository export. Existing progression IDs, purchase recovery and published-content safety remain authoritative.

See [K_MENU_EDITOR.md](K_MENU_EDITOR.md) for the editor workflow and compatibility rules. Historical branch/version language in that document should be read as development history, not current repository state.

### 0.26.x — Crucible Lava/Obsidian progression

The **Crucible** is now integrated.

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

Condenser Mark II now has **3 upgrade capacity** and three physical upgrade slots.

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

## Meta progression boundary

The existing challenge/shop/research/perk/world-purchase rules remain in place. The editor can author supported content but does not turn arbitrary scripts or unsupported machine attribution into safe server-owned objectives.

Tier II–IV, final late-game balance, additional packages/contracts, deeper technology progression and first-acquisition routes for further resources remain active design areas.

## CI and validation

PR #23 optimized GitHub Actions without reducing the validation chain:

- feature branches are validated through pull requests instead of duplicate push + PR workflows;
- stale runs are cancelled when a newer revision is pushed;
- documentation-only changes skip the heavyweight mod workflow;
- jobs have an explicit timeout;
- CI artifacts use reduced retention.

The integrated 0.26.4 head was validated before merge with:

1. Java build/tests
2. headless NeoForge client smoke test
3. K-menu visual fixtures
4. dedicated-server regression GameTests
5. full pack-runtime integration on **NeoForge 21.1.259**

The fixed island `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` remains protected pack state.
