# Changelog

## Omnidirectional machine cables integrated — October 23, 2026

- Merge PR #34 into `main` as `88414546678f53ee4983f56d89b7ff7e9adbdd31`; the mod version remains 0.27.0.
- Expose permitted item insertion/extraction on all six faces of both Condenser tiers, Garden Chamber, Washing Basin and Refining Condenser.
- Preserve input validation, filters, hidden slots, buffers, output policies, separate Garden harvest routes and existing bottom-hopper flow.
- Retain the Crucible's sided item/fluid contract and Ascension's existing omnidirectional access.
- Replace obsolete sided-capability expectations with all-face regression coverage. Pre-merge validation passed 100 JUnit tests, client checks and 122 required GameTests in each server run on NeoForge 21.1.264; see [VALIDATION.md](VALIDATION.md).

## Item texture polish — October 22, 2026

- Replace the Stone Meal, Nether Brick Dust, Nether Concentrate and Mineral Blend inventory textures with the approved pixel-art redesigns.
- Add a subtle animated glow cycle to Nether Concentrate through a four-frame vertical texture strip and `nether_concentrate.png.mcmeta`.
- Keep item IDs, recipes, progression and gameplay behavior unchanged.

## Pack runtime snapshot — October 22, 2026

- Refresh the authoritative supplied launcher snapshot to `Modliste(6).txt` with **94 enabled artifact labels**, up from 87.
- Move the current pack/runtime and repository development target from **NeoForge 21.1.262** to **21.1.264**.
- Align the standalone Gradle default and full optional-integration CI target to NeoForge 21.1.264.
- Align JEI development/test dependencies to the supplied **19.69.0.457** version.
- Add AdvancedAE, AE2 Pattern Restocker, CobbleGenGalore, GeckoLib, Not Enough Recipe Book, OctoLib and ServerCore to the supplied artifact snapshot.
- Update the supplied labels for Forever Skyblock (`main-latest`), Puzzles Lib (21.1.65), Sophisticated Core (1.5.17.2426) and Sophisticated Storage (1.6.8.2196).
- Preserve the exact October 21 list and older runtime snapshots as historical evidence. No fresh loaded-mod-ID count or gameplay-validation claim is inferred from the new launcher list.

## 0.27.0 — Machine visual overhaul integrated — October 22, 2026

- Merge PR #30 into `main` as `5d8ab39b5c8820f4b8f1441715ebbbd9ec5b372d`.
- Redesign all nine custom machine surfaces and five machine container screens as a consistent graphite/copper/steel series with task-specific accents.
- Add refreshed material/tool/upgrade sprites, clear greenhouse glazing and independent Mark-II/Ascension material atlases.
- Give Refining a recessed 3D block/item model and synchronized reactor, Washing a wash-surface/sieve presentation and stored Lava slow convection.
- Preserve recipe/processing/energy/automation/save behavior and native slot/JEI coordinates.
- Keep the approved Garden GUI tree and native `AbstractContainerScreen` coordinate contract.
- Incorporate final player-feedback corrections for Washing Basin presentation and Crucible heat-overlay Z-fighting before merge.
- Final branch validation included Actions run #576; post-merge `main` Actions run #577 passed on the merge commit.

See [MACHINE_VISUAL_OVERHAUL.md](MACHINE_VISUAL_OVERHAUL.md) for scope and validation history.

## 0.26.4 — Resource refining progression integrated — October 22, 2026

- Merge PR #29 (`hotfix/pr28-14-fixes`) into PR #28's `feature/resource-refining-progression` branch, then merge PR #28 into `main`.
- Add **Cobblestone -> Gravel -> Sand** Condenser processing at 120 ticks / six seconds per step.
- Correct the Crucible Stone-Fragment route to **40 Stone Fragments -> 100 mB Lava** while retaining **8 Cobblestone -> 1,000 mB Lava**.
- Add **1 Gravel + 250 mB stored Lava -> 1 Netherrack / 80 ticks** and automatic Crucible item-output pushing.
- Extend the renewable Nether loop through Nether Brick Dust, Nether Concentrate, Mineral Blend and **renewable Redstone**.
- Add the FE-powered **Refining Condenser** with Lapis, Copper, Osmium, Emerald and Diamond recipes.
- Set final Diamond cost to **2 Carbon Matrix + 8 Mineral Blend**, 900 ticks and 240 FE/t.
- Align Mekanism Basic Logistical Transporter and Iron Furnaces Iron Furnace recipes with progression.
- Rework Refining/Crucible JEI presentation and synchronize buffered Condenser output counts.
- Remove custom machine-GUI fit-to-window scaling/mouse-remapping so Vanilla slot, ItemStack, click and JEI coordinates share one origin.
- Final pre-integration source passed Actions run #563; that historical full-pack suite used NeoForge **21.1.259**.

## Pack runtime snapshot — October 21, 2026

- Refresh the supplied list to `Modliste(5).txt` with **87 enabled artifact labels**.
- Move the then-current installed pack runtime from NeoForge 21.1.259 to **21.1.262**.
- Add Mekanism Generators and update the then-supplied Forever Recipe Editor, Forever Skyblock, FTB XMod Compat, JEI, Sophisticated Backpacks and Sophisticated Core labels.
- Preserve older NeoForge 21.1.259 CI/runtime entries as run-specific historical evidence.

## 0.26.4 — Crucible and Condenser Mark II automation integrated

- Integrate the Crucible Lava/Obsidian progression and configurable Condenser Mark II automation layer.
- Add 4,000 mB Crucible Lava storage, heat tiers, Fluid Tank/Freezing Chamber detection, timed freezing, JEI/Jade and automation.
- Add three-point Mark II upgrade capacity with Processing Speed, Garden Accelerator, Buffer Expansion and Batch Processing cards.
- Add independent **Block / Void Excess / Void All** output policies.
- Keep timing/visual state synchronized with effective upgraded processing.

## 0.25.3 — K-menu Studio integrated

- Integrate the K-menu Studio/editor stack into `main` through PR #19.
- Preserve native layout/quest/node authoring, server-validated publication, drafts, conflict reconciliation, backups and repository/ZIP export.
- Retain compact command chrome, collapsible inspector sections, Focus preview, searchable icon replacement and visual ARGB editing.

## CI — Actions usage optimization

- Integrate PR #23: feature branches validate through pull requests rather than duplicate push + PR runs.
- Cancel stale runs, skip heavyweight work for documentation-only changes, cap job runtime and reduce artifact retention.

## 0.25.2 — K-menu Studio usability pass

- Narrow Studio side rails and keep live K-menu preview dominant.
- Expose header sub-elements and searchable icon replacement.
- Add visual ARGB editing and stabilize preview animation behavior.

## 0.25.1 — K-menu Studio usability polish

- Reduce permanently visible command chrome with Files/More/Help workflows.
- Add grouped/collapsible inspector sections and Focus preview mode.
- Stabilize Select-mode previews and preview-size switching.

## 0.25.0 — K-menu Studio

- Add native admin layout, quest/chapter, node, Shop-package and Contract authoring.
- Add drafts, native previews, safe publication/revision leases, conflict reconciliation and backup restoration.
- Add persistent SP/PP reward authoring and validated configuration export.

## 0.24.0 — K-menu overhaul

- Rebuild K-menu presentation, centered header/reward/filter/search labels and scrollable categories.
- Replace Shop strips with round-node branches while preserving transactions.
- Add compact profile/stat/accounting views and shared Island Level.
- Extend protocol to `skills-9` while preserving player schema 4/5/6 and world-progress schema 2.

## 0.23.0 — Iron balancing, Packed Mud and Garden alignment

- Reduce Log -> Stone Fragments to six seconds on both tiers.
- Increase Hopper and permanent Iron-tool costs.
- Add Mud + Wheat -> Packed Mud and align Garden routing badges.

## 0.22.1 — Separate Garden harvest ports

- Route primary and secondary Garden harvests through separate automation ports while preserving GUI outputs and saves.
- Add routing legends/tooltips and extraction regressions.

## 0.22.0 — Garden Wheat and screenshot polish

- Add retained Wheat Seed Garden recipe and shared crop presentation.
- Raise/center Garden labels and remove decorative harvest-item flights.
- Increase Wooden Hopper cost and gate the Wandering Trader caravan until Emerald pickup.

## 0.21.1 — Animation polish

- Keep the approved Garden GUI tree while removing final decorative output flights.
- Improve block-tree crown/trunk geometry and smooth processing intake/discharge transitions.

## 0.21.0 — Machine animation update

- Add staged Condenser Mark I/II processing, recipe-driven process scenes and Garden growth/harvest animation.
- Rebuild the Garden Chamber frame/glazing/soil presentation while preserving production rules.

This file is the concise version history. Detailed test evidence belongs in [VALIDATION.md](VALIDATION.md); current integrated repository status belongs in [CURRENT_STATE.md](CURRENT_STATE.md).
