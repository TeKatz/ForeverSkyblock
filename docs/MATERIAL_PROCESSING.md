# Material processing and automation foundation — 0.19.0 development

Development branch: `feature/material-processing-0.19`, based on the integrated 0.18.1 baseline. This document describes development work and does **not** imply a merge or release. The fixed starter-island NBT is unchanged.

## Progression goal

0.19.0 turns the first renewable water source into the bridge from primitive survival to the first metal and then uses that metal to remove two early manual bottlenecks. The intended order is:

`Ice / Water -> Farm -> Mud -> Packed Mud -> Mud Bricks -> Washing Basin -> Raw Iron -> Iron -> Condenser Mark II`

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
- **8 matching Leaves + 1 Bone Meal -> 1 matching Sapling/Propagule** through an adjacent Mixing Chamber.

Matching conversion is defined for Oak, Spruce, Birch, Jungle, Acacia, Dark Oak, Cherry and Mangrove. The recipe produces the corresponding tree starter rather than converting species.

Condenser recipes now carry an optional `minimum_tier` field, defaulting to Mark I. When more than one recipe matches, two-input recipes are preferred before single-input recipes. This is important for Leaves: with the matching Leaves and Bone Meal in a Mark II + Mixing Chamber, the Sapling recipe wins over the existing 8 Leaves -> Bone Meal recipe. Mark I cannot execute Mark-II-only recipes.

Persistent input locks continue to govern manual and automated insertion. This keeps separate automated Mark II units deterministic without turning the universal Freezing input into a catch-all routing sink.

## JEI

JEI receives:

- a dedicated Washing category with input count, Raw Iron output, duration, no-power state, adjacent-water requirement and explicit note that water is not consumed;
- Washing Basin as its recipe catalyst and a Recipes button in the Basin GUI;
- Condenser Mark II as a Condensing and Freezing catalyst;
- Mark-II-only recipe indication inside the Condensing category;
- transfer rejection when a Mark-II-only Condensing recipe is requested from a Mark I.

## Automation boundary

0.19.0 deliberately pays two existing automation debts: Stone Meal and Saplings can now be generated without a manual Mortar/tree-drop dependency once Iron unlocks Mark II. Washing itself is automation-friendly from the start.

**Wood production is still a required later automation step.** The early economy depends heavily on Logs and Leaves, so a future tree-growing/harvesting facility must close that loop. 0.19.0 does not silently pretend that automated Stone Meal/Saplings alone make the primitive resource chain fully autonomous.

## Compatibility and validation

No player-profile schema, progression-network protocol or starter-island change is required. Existing Mark I saves retain their stable inventory/filter indices; Mark II extends recipe eligibility rather than replacing the original machine.

Required branch validation:

1. Java 21 build and unit tests;
2. client mixin smoke test;
3. dedicated regression GameTests, including Mud, water-gated Washing, pause/resume, automation, Mark-I tier rejection, Mark-II Stone Meal and Sapling priority;
4. full NeoForge 21.1.259 pack-runtime integration suite;
5. client visual/JEI acceptance before any main merge.
