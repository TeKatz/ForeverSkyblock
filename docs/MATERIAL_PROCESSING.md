# Material processing and automation foundation — 0.20.1 development

Development branch: `feature/material-processing-0.19`, based on the integrated 0.18.1 baseline and the tested 0.19.1 material-processing work. This document describes development work and does **not** imply a merge or release. The fixed starter-island NBT is unchanged.

## Progression goal

The material-processing arc turns the first renewable water source into the bridge from primitive survival to the first metal, then uses Iron to remove early manual bottlenecks and finally closes the wood loop with the Garden Chamber.

`Ice / Water -> Farm -> Mud -> Packed Mud -> Mud Bricks -> Washing Basin -> Raw Iron -> Iron -> Condenser Mark II -> Garden Chamber`

Vanilla `Mud + Wheat -> Packed Mud` and Packed Mud -> Mud Bricks remain meaningful links rather than being replaced by a custom shortcut.

## Mud through the Mixing Chamber

The Condenser + Mixing Chamber gains:

- **4 Dirt + 1 Ice -> 4 Mud**;
- 160 ticks / 8 seconds;
- no power.

Both ingredients are consumed. Ice therefore remains a useful production material after the player's first permanent water source exists.

## Washing Basin

The Washing Basin is the first independent primitive material-processing machine after the Condenser family.

Crafting recipe:

- 4 Mud Bricks;
- 2 Cobblestone;
- 2 Planks.

The Basin has one input and one output, accepts normal item-handler automation, and requires a **directly adjacent water source block** on one of its six faces. Flowing water does not qualify. The source is infrastructure and is never consumed.

If the water source is removed during an operation, processing pauses and preserves its exact partial progress and input. Restoring a source resumes the same operation. A blocked output also pauses without item loss.

Initial recipe:

- **4 Stone Meal -> 1 Raw Iron**;
- 160 ticks / 8 seconds;
- no power;
- adjacent source water required and not consumed.

This makes the first Iron deterministic rather than a sieve-style random drop. Raw Iron then uses normal furnace smelting.

## Condenser Mark I crafting

The starter Condenser remains an intentional early advantage, but it becomes reproducible after water and farming are established rather than waiting until Iron.

Recipe:

- 4 Mud Bricks;
- 2 Cobblestone;
- 2 Stone Meal;
- 1 Furnace.

This allows parallel primitive processing before the first metal without trivializing the opening steps.

## Condenser Mark II

The Mark II is the first Iron-gated automation tier. It uses the same Condenser inventory, lock system, adjacent Mixing/Freezing Chamber detection, sided automation and dynamic GUI states as Mark I.

Recipe:

- 1 Condenser Mark I;
- 4 Iron Ingots;
- 4 Mud Bricks.

Mark II retains all Mark I recipes and additionally unlocks:

- **4 Stone Fragments -> 1 Stone Meal** without the manual Mortar step;
- **4 Wheat Seeds + 1 Bone Meal -> 2 Oak Saplings** through an adjacent Mixing Chamber.

The Sapling recipe deliberately no longer consumes Leaves. Harvesting Leaves with Wooden Shears suppresses natural Sapling drops, so tying both Bone Meal and Sapling production to the same limited Leaf stock created an avoidable softlock risk. Wheat Seeds are renewable as soon as farming is established and give that existing by-product an additional progression role.

Only Oak is exposed at this stage because it is the tree species guaranteed by the current opening progression. Spruce, Birch, Jungle, Acacia, Dark Oak, Cherry and Mangrove recipes remain deferred until the pack provides a real first-acquisition path for those species. JEI must not advertise dead-end recipes that cannot be bootstrapped.

Condenser recipes carry an optional `minimum_tier` field, defaulting to Mark I. Mark I cannot execute Mark-II-only recipes. Persistent input locks continue to govern normal Condenser insertion.

## Garden Chamber

The player-facing name is simply **Garden Chamber**. The internal registry id remains `forever_skyblock:garden_chamber_mark_i` so development/test worlds that already contain the block do not lose it when updating from 0.20.0.

The Garden Chamber is the first dedicated wood-production upgrade and is intentionally exclusive to **Condenser Mark II**.

Crafting recipe:

- 4 Mud Bricks;
- 2 Iron Ingots;
- 2 Planks;
- 1 Oak Sapling.

A Garden Chamber placed directly against a Condenser Mark II changes the machine into **Garden mode**. A Condenser Mark I ignores the Garden Chamber completely.

Garden mode has strict priority over the other adjacent upgrades:

- Mixing Chamber is paused and its slot disappears;
- Freezing Chamber is paused and its slots disappear;
- normal Condensing is paused;
- all existing normal inputs, filters and partial progress remain stored and resume unchanged when the Garden Chamber is removed.

The Garden interface replaces the normal process cards with a dedicated landscaped growth scene, a persistent specimen slot, a growth bar, a central recipe button and two harvest outputs.

Initial Garden recipe:

- specimen: **1 Oak Sapling**;
- specimen is **not consumed**;
- 320 ticks / **16 seconds**;
- output: **4 Oak Logs + 8 Oak Leaves**.

The specimen slot is limited to one item and functions as a machine setting rather than a consumed ingredient. Garden recipes are data-driven so later progression can add other trees or crops only after the corresponding plant has a real first-acquisition path.

Garden automation is exposed through the existing Condenser item capability. Automation can insert a valid specimen and extract both harvest outputs. Hidden normal Condenser slots are not exposed while Garden mode is active.

## Reliable early tree growth

Bone Meal on a placed Vanilla Sapling no longer has a random-success wait. A single Bone Meal use advances the Sapling through both Vanilla growth stages and attempts tree generation immediately. Normal tree placement constraints still apply: if the tree cannot physically generate, the game does not force blocks through obstructions.

This removes idle waiting from the wood-heavy early economy while preserving Bone Meal as an actual resource cost.

## Presentation cleanup

### 0.19.1

- Condenser Mark II uses the detailed machine panel atlas again, with a dark metal Mark-II frame instead of the flat placeholder-like vanilla block textures.
- Washing Basin receives a taller layout with separated labels, slots, process bar, recipe summary, hint text and player inventory.
- Condensing and Washing JEI categories gain additional width/height and shorter requirement labels so tier/module/water text no longer overlaps.
- The Freezing Chamber's raised top bolts render only their top plates, removing the stray rim stripe while retaining the approved recessed-basin silhouette.

### 0.20.1 Garden polish

- Garden JEI is now a compact schematic view: specimen on the left, both outputs on the right, a clean process arrow, duration and two catalyst rows. The decorative tree-growth scene was intentionally removed from JEI.
- Garden-mode Condenser GUI gets a larger landscaped viewport with layered sky, hills, grass/soil, moving clouds, sun, staged tree growth and a small late-growth shimmer. The process controls are spaced below the scene instead of competing with it.
- The Garden Chamber block is rebuilt visually as a small copper-framed greenhouse with green glass, a soil/moss bed and visible interior space.
- A lightweight Garden Chamber block-entity renderer animates the greenhouse interior only while an adjacent Condenser Mark II is actively growing: the retained specimen gently moves, leaf motes orbit inside and a grow-light scanner sweeps the chamber.
- The Condenser renderer changes externally in Garden mode as well: the crusher carriage retracts into a grow-light assembly, botanical markers appear on the front and an active scanner/sapling indicator replaces the normal crushing animation.

## JEI

JEI includes:

- a dedicated Washing category with input count, Raw Iron output, duration, no-power state, adjacent-water requirement and explicit note that water is not consumed;
- a dedicated Garden category showing the retained specimen, both harvest outputs, 16-second duration and the required Condenser Mark II + Garden Chamber catalysts without decorative animation or overflowing explanatory text;
- Washing Basin as its recipe catalyst and a Recipes button in the Basin GUI;
- Condenser Mark II and Garden Chamber as Garden recipe catalysts;
- Condenser Mark II as a Condensing and Freezing catalyst;
- Mark-II-only recipe indication inside the Condensing category;
- transfer rejection when a Mark-II-only Condensing recipe is requested from a Mark I.

## Automation boundary

The primitive resource chain now has a concrete automation closure:

- Stone Fragments -> Stone Meal is automated by Condenser Mark II;
- Wheat Seeds + Bone Meal -> Oak Saplings is automated by Condenser Mark II + Mixing Chamber;
- Oak Logs + Oak Leaves are automated by Condenser Mark II + Garden Chamber;
- Leaves continue feeding Bone Meal production;
- Logs continue feeding Stone Fragment production;
- Stone Meal + Sapling can therefore feed automated Dirt production.

This means the wood-heavy opening economy no longer requires permanent manual tree chopping once the player has reached the Iron / Mark-II tier.

## Compatibility and validation

No player-profile schema, progression-network protocol or starter-island change is required. Existing Mark I saves retain their stable original inventory/filter indices; Garden storage is appended after the existing Condenser and Freezing slots for save compatibility. The Garden Chamber keeps its original development registry id while dropping `Mark I` from all player-facing naming.

Required branch validation:

1. Java 21 build and unit tests;
2. client mixin smoke test, including Garden renderer registration;
3. dedicated regression GameTests, including Mud, water-gated Washing, pause/resume, Mark-I tier rejection, Mark-II Stone Meal, the Wheat-Seed Sapling recipe, Garden exclusivity, retained specimen, dual-output harvest and Garden automation;
4. full NeoForge 21.1.259 pack-runtime integration suite;
5. client visual/JEI acceptance before any main merge.
