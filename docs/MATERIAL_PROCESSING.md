# Material processing and automation foundation — integrated 0.20.1

Integrated through PR #15 on October 18, 2026. This document describes the live early material-processing path in `main`. The fixed starter-island NBT is unchanged and no release is implied by the merge.

Development follow-up **0.22.0** on `feature/garden-wheat-polish` adds retained Wheat Seeds -> **4 Wheat + 2 Seeds / 16 seconds**, synchronized crop presentation and the Iron-priced Wooden Hopper. These changes are separate from the integrated baseline described below. See [the crop template and validation](MACHINE_ANIMATIONS.md#0220-garden-crops-and-pack-fixes).

Development follow-up **0.22.1** on `feature/garden-harvest-routing` separates the harvest: a hopper under MK II takes the primary output, while one under Garden Chamber takes the secondary output. The existing controller GUI explains both routes; all inventory remains in that controller. See [routing behavior and validation](MACHINE_ANIMATIONS.md#0221-separate-garden-harvest-ports). This is not yet integrated into main.

## Progression goal

The material-processing arc turns the first renewable water source into the bridge from primitive survival to the first metal, then uses Iron to remove early manual bottlenecks and closes the wood loop with the Garden Chamber.

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

This is the implemented deterministic first-Iron route. Raw Iron then uses normal furnace smelting.

## Condenser Mark I crafting

The starter Condenser remains an intentional early advantage, but it becomes reproducible after water and farming are established rather than waiting until Iron.

Recipe:

- 4 Mud Bricks;
- 2 Cobblestone;
- 2 Stone Meal;
- 1 Furnace.

This allows parallel primitive processing before the first metal without trivializing the opening steps.

## Condenser Mark II

Mark II is the first Iron-gated automation tier. It uses the same core Condenser inventory, lock system, sided automation and adjacent Mixing/Freezing Chamber behavior as Mark I.

Recipe:

- 1 Condenser Mark I;
- 4 Iron Ingots;
- 4 Mud Bricks.

Mark II retains all Mark I recipes and additionally unlocks:

- **4 Stone Fragments -> 1 Stone Meal** without the manual Mortar step;
- **4 Wheat Seeds + 1 Bone Meal -> 2 Oak Saplings** through an adjacent Mixing Chamber.

The Sapling recipe deliberately does not consume Leaves. Harvesting Leaves with Wooden Shears suppresses natural Sapling drops, so tying both Bone Meal and Sapling production to the same limited Leaf stock created an avoidable softlock risk. Wheat Seeds are renewable as soon as farming is established and give that existing by-product an additional progression role.

Only Oak is exposed at this stage because it is the tree species guaranteed by the current opening progression. Spruce, Birch, Jungle, Acacia, Dark Oak, Cherry and Mangrove recipes remain deferred until the pack provides a real first-acquisition path for those species. JEI must not advertise dead-end recipes that cannot be bootstrapped.

Condenser recipes carry an optional `minimum_tier` field, defaulting to Mark I. Mark I cannot execute Mark-II-only recipes. Persistent input locks continue to govern normal Condenser insertion.

## Garden Chamber

The **Garden Chamber** is the dedicated wood-production upgrade and is intentionally exclusive to **Condenser Mark II**.

The player-facing name is simply `Garden Chamber`. The internal registry ID remains `forever_skyblock:garden_chamber_mark_i` to preserve compatibility with the already-tested development worlds/items created before the visible rename.

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

The Garden interface replaces the normal Condenser process cards with a green landscape, a persistent specimen slot, a growth bar, two harvest outputs and a dedicated Garden recipe button.

Initial Garden recipe:

- specimen: **1 Oak Sapling**;
- specimen is **not consumed**;
- 320 ticks / **16 seconds**;
- output: **4 Oak Logs + 8 Oak Leaves**.

The specimen slot is limited to one item and functions as a machine setting rather than a consumed ingredient. Garden recipes are data-driven so later progression can add other trees or crops only after the corresponding plant has a real first-acquisition path.

Garden automation is exposed through the existing Condenser item capability. Automation can insert a valid specimen and extract both harvest outputs. Hidden normal Condenser slots are not exposed while Garden mode is active.

## Garden presentation

The 0.20.1 presentation pass keeps the Garden system visually distinct from normal condensing without changing the gameplay numbers:

- the Condenser Garden GUI uses a larger layered landscape with sky, hills, soil, clouds and a more detailed staged tree-growth animation;
- the Garden JEI page is intentionally static and compact: specimen, retained-state indicator, duration and two outputs, with no decorative tree-growth animation in the recipe overview;
- the Garden Chamber block uses a small greenhouse-style model rather than the earlier flat moss/copper placeholder;
- while a connected Mark-II Condenser is actively growing, the greenhouse interior gains subtle plant/light motion;
- the Condenser exterior switches from the normal crusher presentation to green Garden-specific indicators/scanner motion while Garden mode is active.

These exterior animations are presentation-only. Garden output timing and state remain server authoritative.

## Reliable early tree growth

Bone Meal on a placed Vanilla Sapling no longer has a random-success wait. A single Bone Meal use advances the Sapling through both Vanilla growth stages and attempts tree generation immediately. Normal tree placement constraints still apply: if the tree cannot physically generate, the game does not force blocks through obstructions.

This removes idle waiting from the wood-heavy early economy while preserving Bone Meal as an actual resource cost.

## JEI

JEI includes:

- a dedicated Washing category with input count, Raw Iron output, duration, no-power state, adjacent-water requirement and explicit note that water is not consumed;
- a dedicated Garden category showing the retained specimen, both harvest outputs, duration, Condenser Mark II requirement and exclusive Garden-mode behavior;
- Washing Basin as its recipe catalyst and a Recipes button in the Basin GUI;
- Condenser Mark II and Garden Chamber as Garden recipe catalysts;
- Condenser Mark II as a Condensing and Freezing catalyst;
- Mark-II-only recipe indication inside the Condensing category;
- transfer rejection when a Mark-II-only Condensing recipe is requested from a Mark I.

## Automation closure

The primitive resource chain now has a concrete automation closure:

- Stone Fragments -> Stone Meal is automated by Condenser Mark II;
- Wheat Seeds + Bone Meal -> Oak Saplings is automated by Condenser Mark II + Mixing Chamber;
- Oak Logs + Oak Leaves are automated by Condenser Mark II + Garden Chamber;
- Leaves continue feeding Bone Meal production;
- Logs continue feeding Stone Fragment production;
- Stone Meal + Sapling can therefore feed automated Dirt production.

The pack no longer requires permanent manual tree chopping after the player reaches the Iron / Mark-II tier. This follows the project rule that a progression stage may begin manually, but its core repeating work should become automatable once that stage has been mastered.

## Balancing feedback from the automated Iron line

On October 19, 2026, TeKatz tested the 0.22.1 setup and reported relatively fast accumulation of Iron after building the complete automated chain, with Bone Meal produced by its parallel Leaf-processing branch. Use that established-factory availability when balancing future content. The first-Iron milestone and mass production after automation are different pacing stages; larger Iron quantities alone should not be assumed to keep later content difficult. See [the agreed balancing baseline](DESIGN.md#automated-iron-as-a-balancing-baseline--october-19-2026). Existing yields, durations and recipes are unchanged by this feedback.

## Compatibility and validation

No player-profile schema, progression-network protocol or starter-island change is required. Existing Mark I saves retain their stable original inventory/filter indices; Garden storage is appended after the existing Condenser and Freezing slots for save compatibility.

Final pre-merge head `aec270d7bc0caea5952af034ed8505ce0fd3d68f` passed GitHub Actions run **#259**: Java 21 build/unit tests, headless client mixin smoke, dedicated regression GameTests and the full NeoForge 21.1.259 pack-runtime integration suite.

Regression coverage includes Mud consumption, water-gated Washing, pause/resume, Washing automation, Mark-I tier rejection, Mark-II Stone Meal, the Wheat-Seed Sapling recipe, Garden exclusivity, retained specimen, dual-output harvest and Garden automation. TeKatz separately performed in-game testing throughout the 0.19.x/0.20.x visual iterations.
