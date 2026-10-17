# Freezing Chamber Mark I and workbench gate — integrated 0.18.1 behavior

The Freezing Chamber Mark I and Crafting Table progression gate are integrated into the current `main` baseline through PR #13. The fixed starter-island NBT is unchanged and no release was published by the merge.

Historical development branch: `feature/freezing-chamber-workbench`.

## Freezing Chamber Mark I

The Freezing Chamber Mark I is a passive Condenser module. It has no menu or inventory of its own and is detected only when it shares one of the Condenser's six faces. Its block model follows the approved third design concept: a dark machine-family housing with oak/copper accents, an open frosted top basin, cyan cold indicators and a distinct freezing panel.

Recipe:

- 1 × Mixing Chamber Mark I;
- 4 × Cobblestone;
- 2 × Stone Meal;
- 2 × Bone Meal.

All ingredients are available at the current leaves/Bone Meal progression boundary.

Connecting the module exposes two stable Condenser inventory slots:

- slot 3: Freezing input;
- slot 4: Freezing output.

The input accepts any registered item unless its input lock restricts it. One real item is consumed per processing step and advances an independent material counter. Every 32 consumed items create exactly one `minecraft:ice`. The final item is not consumed while the Ice output is blocked.

Freezing runs independently and in parallel with the normal Condenser recipe. Removing the module hides and disables its slots but preserves real input, output, partial progress and its lock. Reattaching resumes from the saved state. Breaking the Condenser still drops every real stored stack exactly once.

The 0.18.1 visual cleanup raises the four decorative top bolts above the frame plane. They no longer share a coplanar top surface with the surrounding rim, removing the observed top-side z-fighting while retaining Design 3's recessed basin silhouette.

## Input locks and automation

The existing **Edit input locks** mode covers all active Condenser inputs:

- main Condenser input;
- Mixing Chamber input, while connected;
- Freezing Chamber input, while connected.

The Freezing input accepts every item only while unlocked. A lock is enforced by manual insertion, drag/ghost editing, JEI ghost input and NeoForge item-handler automation. Disconnected module slots are not exposed to automation. The bottom capability extracts either real output when its corresponding process is available.

Because an unlocked Freezing input accepts every item, it is deliberately excluded from generic shift-click routing and normal Condensing JEI transfers. Automated installations should lock it when deterministic routing matters.

Lock editing no longer renders a second per-slot help tooltip on top of the normal item tooltip. Lock instructions remain on the lock-edit button while the normal item tooltip remains readable.

## Dynamic Condenser menu

The container keeps five stable machine slot indices but presents four live module states without reopening:

1. base Condenser;
2. Condenser + Mixing Chamber;
3. Condenser + Freezing Chamber;
4. Condenser + both modules.

The Mixing input and Freezing input/output activate only for their own adjacent module. The screen renders a separate 0/32 Freezing progress section when applicable, while the original timed Condensing progress remains independent. Placing or removing either module while the menu is open changes the state immediately through normal container synchronization.

For 0.18.1 the Condenser screen is taller and the timed Condensing section, Freezing section, action buttons, module-state hint and player inventory are vertically separated. The logical slot indices and automation semantics are unchanged. The former hidden click target across the upper progress area has been removed; JEI now opens only from the explicit **Recipes** button or normal JEI interactions.

## JEI

Freezing has its own JEI category in the same visual language as Condensing. It shows a wildcard input, 32-item material progress, one Ice output, no power requirement and the adjacent Freezing Chamber requirement. The wildcard is deliberately not registered as every item in the pack, preventing the Freezing recipe from polluting every item's JEI Uses page. Ice and the Freezing Chamber still lead to the entry normally, and the Condenser **Recipes** button opens both Condensing and Freezing categories.

## Crafting Table progression gate

The normal `minecraft:crafting_table` recipe is replaced with a 3×3 plus-shaped recipe:

- four Planks around the center;
- one Stone Meal in the center.

This intentionally keeps the starter ME Crafting Terminal as the first 3×3 crafting workstation. Once Stone Meal is established, the player can craft normal workbenches and become independent of the starter terminal for ordinary crafting.

The override is bundled in the mod and mirrored into the Forever Recipe Editor generated pack. No starter-island inventory or structure changes are required.

## Validation

The 0.18.1 implementation is covered by Java 21 build/unit tests, a headless NeoForge client mixin smoke test, dedicated regression GameTests and the full NeoForge 21.1.259 integration suite. Tests cover all four live menu states, any-item freezing, exact 32-item output, blocked-output safety, persisted progress/locks, automation filtering, module disconnect/reconnect, parallel Mixing + Freezing operation, transfer isolation, the chamber recipe, the Stone Meal workbench gate, the expanded Condenser layout and the non-coplanar Freezing Chamber top details.

The final feature head `97715f06391339141032c5f0c859b5925f99539d` passed GitHub Actions run #195 before PR #13 merged it into `main`.

TeKatz tested the implementation in-game. The 0.18.0 test identified top-side z-fighting, cramped GUI spacing, a missing Ice JEI entry, overlapping lock tooltips and an old hidden recipe-list click target. After those issues were corrected in 0.18.1, TeKatz confirmed the result as a good baseline to continue from. This is user-reported client acceptance separate from CI.
