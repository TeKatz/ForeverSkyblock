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

## Further uses discussed, not implemented

- Stone tools using fragments in place of cobblestone.
- A mortar for an early manual processing step.
- A wood-and-fragment machine frame once a concrete machine needs it.
- Later condenser tiers.

These remain proposals. The current task does not approve their implementation.

## Remaining progression decisions

1. How do players obtain water and establish sustainable food production?
2. Which process provides the first metals without direct iron output from Mark I?
3. Which machine follows the condenser, and which materials does it require?
4. When do lava and a higher-throughput cobblestone generator unlock?
5. How should a possible emergency sapling quest work?

TeKatz authors the FTB quests manually. Import actual saved crafting changes into `pack/config/forever_recipe_editor/generated_pack/` when available.
