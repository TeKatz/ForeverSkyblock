# Material processing and automation foundation — integrated 0.24.0

The 0.19–0.20 foundation was integrated through PR #15; the 0.21–0.24 follow-ups are integrated through [PR #16](https://github.com/TeKatz/ForeverSkyblock/pull/16) on October 19, 2026. This document describes the live early material-processing path in `main`. The fixed starter-island NBT is unchanged; no release is implied.

Integrated **0.22.0** adds retained Wheat Seeds -> **4 Wheat + 2 Seeds / 16 seconds**, synchronized crop presentation and the Iron-priced Wooden Hopper. See [the crop template and validation](MACHINE_ANIMATIONS.md#0220-garden-crops-and-pack-fixes).

Integrated **0.22.1** separates harvest: a hopper under MK II takes the primary output, while one under Garden Chamber takes the secondary. The controller GUI explains both routes; all inventory remains in that controller. See [routing behavior and validation](MACHINE_ANIMATIONS.md#0221-separate-garden-harvest-ports).

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

Oak is currently the only supported tree because it is the tree species guaranteed by the current opening progression. Spruce, Birch, Jungle, Acacia, Dark Oak, Cherry and Mangrove recipes remain deferred until the pack provides a real first-acquisition path for those species. JEI must not advertise dead-end recipes that cannot be bootstrapped.

Condenser recipes carry an optional `minimum_tier` field, defaulting to Mark I. Mark I cannot execute Mark-II-only recipes. Persistent input locks continue to govern normal Condenser insertion.

## Garden Chamber

The **Garden Chamber** is the dedicated tree/crop-production upgrade and is intentionally exclusive to **Condenser Mark II**.

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

Oak Garden recipe:

- specimen: **1 Oak Sapling**;
- specimen is **not consumed**;
- 320 ticks / **16 seconds**;
- output: **4 Oak Logs + 8 Oak Leaves**.

Wheat retains **1 Wheat Seeds** and produces **4 Wheat + 2 Seeds every 16 seconds**, using the shared crop presentation profile.

The specimen slot is limited to one item and functions as a machine setting rather than a consumed ingredient. Garden recipes are data-driven so later progression can add other trees or crops only after the corresponding plant has a real first-acquisition path.

Automation inserts a supported specimen through the Condenser capability. It extracts only the primary harvest through MK II and only the secondary harvest through the adjacent Garden Chamber's bottom or unsided port. Both slots remain manually accessible in the Condenser GUI. Hidden normal inputs stay inaccessible during Garden mode; either full output pauses production. The Chamber has no standalone storage or GUI.

## Garden presentation

The presentation introduced in 0.20.1 and polished through 0.22.x keeps the Garden system visually distinct from normal condensing without changing the gameplay numbers:

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

On October 19, 2026, TeKatz tested the 0.22.1 setup and reported relatively fast accumulation of Iron after building the complete automated chain, with Bone Meal produced by its parallel Leaf-processing branch. Use that established-factory availability when balancing future content. The first-Iron milestone and mass production after automation are different pacing stages; larger Iron quantities alone should not be assumed to keep later content difficult. See [the agreed balancing baseline](DESIGN.md#automated-iron-as-a-balancing-baseline--october-19-2026). The feedback itself changed no recipes; the requested 0.23.0 trial below subsequently adjusted processing speed and Iron costs.

## Compatibility and validation

The material-processing changes require no player-profile schema, progression-network protocol or starter-island change. Existing Mark I saves retain their stable original inventory/filter indices; Garden storage is appended after the existing Condenser and Freezing slots for save compatibility. The separate 0.24.0 K-menu update uses protocol `skills-9` and world-progress schema 2; see [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md#island-level-explicitly-chosen-world-progress).

Historical 0.20.1 pre-merge head `aec270d7bc0caea5952af034ed8505ce0fd3d68f` passed GitHub Actions run **#259**: Java 21 build/unit tests, headless client mixin smoke, dedicated regression GameTests and the full NeoForge 21.1.259 pack-runtime integration suite. Current 0.24.0 validation is recorded in [VALIDATION.md](VALIDATION.md#0240-integration--pr-16).

Regression coverage includes Mud consumption, water-gated Washing, pause/resume, Washing automation, Mark-I tier rejection, Mark-II Stone Meal, the Wheat-Seed Sapling recipe, Garden exclusivity, retained specimen, dual-output harvest and Garden automation. TeKatz separately performed in-game testing throughout the 0.19.x/0.20.x visual iterations.


## 0.23.0 balancing and Packed Mud

Developed on `feature/iron-balance-packed-mud` from validated 0.22.1 head `06d01d6c03f063ed68fc4ce27cac4f25588d4df6`, this requested balancing trial is integrated into `main` through PR #16. It remains subject to later gameplay tuning.

### Log throughput

The shared `condensing/logs` recipe uses **120 ticks / six seconds**, retaining one Log -> four Stone Fragments and availability on MK I and II. Other existing cycle durations are unchanged. GUI/world interpolation and JEI read the actual recipe duration. A Garden produces four Logs per 16 seconds (15/minute); one new six-second processor consumes 10/minute. This reduces the previous backlog growth but still permits an eventual 64-Log input stack in a single-processor line. The requested value is preserved as a trial, not presented as exact throughput matching.

### Iron-priced normal Hopper and tools

Normal Hopper uses `I I` / `BCB` / ` I `: I = Iron Ingot, B = Iron Block, C = Chest. Total is two Blocks, three Ingots and one Chest (21 Iron Ingots equivalent, excluding Chest). `forever_skyblock:hopper` is authoritative; an explicit condition removes the original `minecraft:hopper`, and the existing recipe-replacement filter now suppresses competing static normal-Hopper outputs as well as the existing Wooden Hopper alternatives. A recipe-book advancement unlocks the replacement. Wooden Hopper still costs two Ingots, three Planks and a wooden Chest.

The four custom Iron tool heads each replace one Ingot with one Block. Exact costs and retained prior-tier tools/Chains are documented in [TOOL_PROGRESSION.md](TOOL_PROGRESSION.md#0230-iron-cost-adjustment). The personal Iron Tools challenge validates the new one-Block ingredient proof, rejects the previous all-Ingot proof, and retains the existing rewards and earned claims. Other tool tiers and durability policy are unchanged. The distributed Recipe Editor snapshots mirror the recipes/removal/advancement definitions byte-for-byte; the mod does not rewrite a player's local Recipe Editor configuration.

For an existing instance with Recipe Editor overrides, also apply `ForeverSkyblock-0.23.0-RecipeEditor-Update.zip` at the instance root. It contains only the nine matching data files under `config/forever_recipe_editor/generated_pack/data/`; these refresh the four Iron-tool recipes, Logs, Packed Mud, the normal Hopper/removal and its recipe-book advancement. The test JAR itself also bundles those definitions for standalone installation.

### Packed Mud

The new `condensing/packed_mud` recipe takes **one Mud + one Wheat -> one Packed Mud**, **160 ticks / eight seconds**, no power, minimum tier one. It needs the existing Mixing Chamber to expose the second input and supports either ingredient order on MK I and MK II. A single shared recipe correctly serves both tiers. Existing Garden-exclusive mode and output-capacity rules remain unchanged. The Vanilla crafting route is still available.

The JEI plugin already registers all loaded Condensing recipes and both controller catalysts; its category displays the actual inputs/output, duration and required Mixing Chamber, with its existing transfer handler. Packed Mud therefore receives a native Condensing entry rather than a fabricated separate category. Recipe synchronization uses the existing serializer/stream codec and the same recipe drives the live GUI's mixing scene.

### Garden badge alignment

Each badge now takes the corresponding real output slot centre as its anchor. The 24-pixel background and tooltip rectangle match the slot's skin bounds; the block icon and arrow are arranged around the centered number. Neither output slots nor the approved plant animations move. English/German software previews of the actual draw routines were inspected; final appearance and mouse behavior remain a real-client test.

### 0.23.0 validation and artifacts

Source `1fd28930ed79bd2bdccace44ab2ec19c95faa966` passed [GitHub Actions run 39173558477](https://github.com/TeKatz/ForeverSkyblock/actions/runs/39173558477): Java 21 build, all **74 unit tests**, client startup/mixin/model checks, **89 required GameTests** on NeoForge 21.1.249 and **89 required GameTests** on the full NeoForge 21.1.259 integration runtime, including the real Wooden Hopper.

Added/updated checks cover six-second completion on both tiers with unchanged Plank timing, retained output quantities, every new Iron-tool cost and rejection of old patterns, priced Hopper/reload filtering, Packed Mud module requirements/ingredient order/actual production/JEI recipe source/network round-trip, and byte-identical distributed Recipe Editor resources. The first full regression caught the old hardcoded Iron-tool challenge proof; the final source updates that strict proof to the new recipe, rejects all-Ingot evidence and passes the existing real crafting/smithing result-slot test with its unchanged 15-SP total. Already-earned claims and other challenge rewards are preserved.

The fresh `ForeverSkyblock-1.21.1-NeoForge-0.23.0.jar` is **5,275,826 bytes**, from artifact `12153273554` of that exact run. Artifact digest and ZIP/JAR CRCs passed. Mod version is 0.23.0, updated classes target Java 21, the Iron-Block challenge proof is present, every packaged resource matches the checkout and GameTest classes are excluded. SHA-256: `1a80b6f264fd23669b09f52e4794030c9aa5f200f36bb316a5c7ccd796f8059a`.

`ForeverSkyblock-0.23.0-RecipeEditor-Update.zip` contains the nine matching Recipe Editor data files, **3,863 bytes**. Each ZIP entry was compared against the corresponding packaged JAR resource. SHA-256: `904b13867db08a78c9af5859463b4549fb2007d3462e85875b4d9558510bb687`. Apply it at an existing instance's root when Recipe Editor overrides are installed, so local old custom Iron-tool costs cannot shadow the new JAR recipes.

The fixed island NBT remains unchanged. The centered badge routine was inspected in English/German software previews using approximate fonts and placeholder item swatches. Headless client startup and data-source tests do not certify the real JEI mouse flow or final GUI pixels; check those in the delivered client build. The final documentation-only commit changes no validated code/resources.
