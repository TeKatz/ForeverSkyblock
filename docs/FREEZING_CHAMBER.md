# Freezing Chamber Mark I and workbench gate — 0.18.1 development

Development branch: `feature/freezing-chamber-workbench`, based on the validated 0.17.1 emergency-hotfix head. No merge or release is implied. The fixed starter-island NBT is unchanged.

## Freezing Chamber Mark I

The Freezing Chamber Mark I is a passive condenser module. It has no menu or inventory of its own and is detected only when it shares one of the condenser's six faces. Its block model follows the approved third design concept: a dark machine-family housing with oak/copper accents, an open frosted top basin, cyan cold indicators and a distinct freezing panel.

Recipe:

- 1 × Mixing Chamber Mark I;
- 4 × Cobblestone;
- 2 × Stone Meal;
- 2 × Bone Meal.

All ingredients are already available at the current leaves/Bone Meal progression boundary.

Connecting the module exposes two stable condenser inventory slots:

- slot 3: Freezing input;
- slot 4: Freezing output.

The input accepts any registered item unless its input lock restricts it. One real item is consumed per processing step and advances an independent material counter. Every 32 consumed items create exactly one `minecraft:ice`. The final item is not consumed while the Ice output is blocked.

Freezing runs independently and in parallel with the normal condenser recipe. Removing the module hides and disables its slots but preserves real input, output, partial progress and its lock. Reattaching resumes from the saved state. Breaking the condenser still drops every real stored stack exactly once.

The 0.18.1 visual cleanup raises the four decorative top bolts above the frame plane. They no longer share a coplanar top surface with the surrounding rim, removing the observed top-side z-fighting while retaining Design 3's recessed basin silhouette.

## Input locks and automation

The existing **Edit input locks** mode covers all active condenser inputs:

- main condenser input;
- Mixing Chamber input, while connected;
- Freezing Chamber input, while connected.

The Freezing input accepts every item only while unlocked. A lock is enforced by manual insertion, drag/ghost editing, JEI ghost input and NeoForge item-handler automation. Disconnected module slots are not exposed to automation. The bottom capability extracts either real output when its corresponding process is available.

Because an unlocked Freezing input accepts every item, it is deliberately excluded from generic shift-click routing and normal condensing JEI transfers. Automated installations should lock it when deterministic routing matters.

Lock editing no longer renders a second per-slot help tooltip on top of the normal item tooltip. Lock instructions remain on the lock-edit button while the normal item tooltip remains readable.

## Dynamic condenser menu

The container keeps five stable machine slot indices but presents four live module states without reopening:

1. base condenser;
2. condenser + Mixing Chamber;
3. condenser + Freezing Chamber;
4. condenser + both modules.

The Mixing input and Freezing input/output activate only for their own adjacent module. The screen renders a separate 0/32 Freezing progress section when applicable, while the original timed condensing progress remains independent. Placing or removing either module while the menu is open changes the state immediately through normal container synchronization.

For 0.18.1 the condenser screen is taller and the timed condensing section, Freezing section, action buttons, module-state hint and player inventory are vertically separated. The logical slot indices and automation semantics are unchanged. The former hidden click target across the upper progress area has been removed; JEI now opens only from the explicit **Recipes** button or normal JEI interactions.

## JEI

Freezing has its own JEI category in the same visual language as Condensing. It shows a wildcard input, 32-item material progress, one Ice output, no power requirement and the adjacent Freezing Chamber requirement. The wildcard is deliberately not registered as every item in the pack, preventing the Freezing recipe from polluting every item's JEI Uses page. Ice and the Freezing Chamber still lead to the entry normally, and the condenser **Recipes** button opens both Condensing and Freezing categories.

## Crafting Table progression gate

The normal `minecraft:crafting_table` recipe is replaced with a 3×3 plus-shaped recipe:

- four planks around the center;
- one Stone Meal in the center.

This intentionally keeps the starter ME Crafting Terminal as the first 3×3 crafting workstation. Once Stone Meal is established, the player can craft normal workbenches and become independent of the starter terminal for ordinary crafting.

The override is bundled in the mod and mirrored into the Forever Recipe Editor generated pack. No starter-island inventory or structure changes are required.

## Validation target

The 0.18.1 branch must retain the existing Java 21 build, client mixin smoke test, dedicated condenser regression GameTests and full NeoForge 21.1.259 integration suite. Tests cover all four live menu states, any-item freezing, exact 32-item output, blocked output safety, persisted progress/locks, automation filtering, module disconnect/reconnect, parallel Mixing + Freezing operation, transfer isolation, the chamber recipe, the Stone Meal workbench gate, the expanded condenser layout and the non-coplanar Freezing Chamber top details.
