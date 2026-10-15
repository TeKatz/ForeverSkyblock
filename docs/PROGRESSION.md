# Early progression

## Confirmed and implemented in mod 0.2.0

TeKatz selected wood conversion as the first resource path and requested the condenser GUI, animation, automation, fragment item and JEI integration.

| Step | Behavior |
| --- | --- |
| Starting resources | Existing AE2 network, island oak tree and Condenser Mark I |
| Logs | One log produces four Stone Fragments |
| Planks | One plank produces one Stone Fragment |
| Power | No electricity or fuel |
| Initial machine rate | One operation every 160 ticks; eight seconds at 20 TPS, chosen as an initial balancing value |
| Cobblestone | Four fragments in a 2×2 crafting recipe produce one cobblestone |
| First furnace | Existing vanilla recipe using eight cobblestone |
| Stone | Smelt cobblestone in the furnace |
| Interaction | Two-slot GUI with animated progress and status |
| Automation | Top/side input, bottom output through standard item handlers |
| Blocked output | Pause without consuming wood; retain partial progress |

A log and its four planks give the same material yield, but processing four planks takes four operations. Logs therefore provide better throughput. At the default rate, eight logs provide the 32 fragments needed for a furnace in 64 seconds of uninterrupted processing, excluding collection and crafting time.

The earlier passive-generation proposal is superseded. Mark I does not produce iron. Its current machine recipes can be overridden with datapacks; Recipe Editor continues to handle crafting recipes.

## Implemented in 0.6.0

The approved next step adds a reusable Mortar (two fragments, two cobblestone and one stick), four fragments plus the mortar into one Stone Meal, and a Mixing Chamber made from four planks, four fragments and one meal. One meal plus one sapling produces one dirt in the condenser in 160 ticks while a chamber shares a face with it. See [CONDENSER_MIXING.md](CONDENSER_MIXING.md). The quantities are initial test balancing values.

## Further uses discussed, not implemented

- A wood-and-fragment machine frame once a concrete machine needs it.
- Later condenser tiers.

These remaining entries are proposals.

## Remaining progression decisions

1. How do players obtain water and establish sustainable food production?
2. Which process provides the first metals without direct iron output from Mark I?
3. Which machine follows the condenser, and which materials does it require?
4. When do lava and a higher-throughput cobblestone generator unlock?
5. How should a possible emergency sapling quest work?

TeKatz authors the FTB quests manually. Import actual saved crafting changes into `pack/config/forever_recipe_editor/generated_pack/` when available.

## Permanent tool progression — 0.7.0

The accepted handoff adds Wood → Stone → Iron → Diamond → Netherite pickaxes/axes/shovels/hoes, with Gold as an optional Stone-based sidegrade. Exact recipes and durability behavior are documented in [TOOL_PROGRESSION.md](TOOL_PROGRESSION.md). First metals and pre-Diamond Obsidian production remain unresolved and must be designed before claiming complete survival progression through Diamond.

## Ascension update — 0.8.0

TeKatz approved separate Perk Points, earned by personally consuming an endgame Forever Core in an Ascension Chamber. Creative Jetpack research now requires **40 SP + 1 PP**, retaining its EndGame prerequisite. The menu, confirmed refunds and cross-world persistence support both currencies. Legacy SP-only Creative research is refunded and re-locked. See [ASCENSION.md](ASCENSION.md) for initial recipes, automation, migration, claim replay and test scope. This feature is on `feature/ascension-perk-points`, based on the still-open permanent-tool PR #9; no main merge is implied.
