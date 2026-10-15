# Condenser mixing and input filters — 0.6.0

This development branch builds on the unmerged `feature/iron-jetpacks-research` branch. It preserves its skill profiles, four Jetpack research stages, restrictions, JEI visibility and temporary +10 SP test button. It does not merge or release either feature.

## Recipes and initial balancing

| Operation | Inputs | Result |
| --- | --- | --- |
| Mortar crafting | 2 Stone Fragments, 2 cobblestone, 1 stick | 1 reusable Mortar |
| Manual grinding | Mortar and 4 Stone Fragments, shapeless | 1 Stone Meal; Mortar returned |
| Mixing Chamber crafting | 4 planks, 4 Stone Fragments, 1 Stone Meal | 1 Mixing Chamber Mark I |
| Condenser with chamber | 1 Stone Meal and any 1 sapling, either input order | 1 dirt after 160 ticks |
| Existing condenser | 1 log or 1 plank | 4 or 1 Stone Fragments after 160 ticks |

No electricity or fuel is used. Recipe shapes and ingredients are visible in JEI. The reusable mortar has no durability. Quantities and processing time are provisional balancing values stored in recipe JSON. Recipe Editor handles crafting changes; machine recipes use the custom serializer and datapacks.

## Module and live GUI

Place `forever_skyblock:mixing_chamber_mark_i` against any of the condenser's six faces, including above or below. Diagonal or distant modules do not count. One or more adjacent chambers expose exactly one extra input. A chamber is a passive extension with no inventory; it may serve multiple adjacent condensers.

The existing open menu updates through normal container synchronization; it does not need reopening. Slots stay at fixed network indices: input 0, output 1, secondary input 2. The second slot and its contents are hidden and inaccessible while the module is absent. Its real items and filter remain saved. Reattaching reveals them again. Removing a module stops a mixing recipe without consuming either input and discards that recipe's partial work. Single-input wood recipes remain available without a module.

A blocked output pauses the cycle without consuming inputs. Both mixing ingredients are consumed together only when the full result fits. Breaking the condenser drops all real stored items, including a hidden secondary stack, once; ghost filters never drop.

## Editing filters

1. Open the condenser and click **Edit filters**.
2. Pick up an inventory stack and click/drag it onto an active input, or drag an item from JEI onto that input. The item is copied as a ghost; the carried stack remains unchanged.
3. Right-click an active input to clear its filter.
4. Click **Done** to resume normal inventory interaction. Filters remain active.

A filtered input accepts only that exact registered item (for example oak saplings rather than every sapling). Counts and item components are ignored for filter matching. Filters are not ingredients and cannot produce output without real inputs. Filtering does not consume items or stop ongoing production. A filter conflicting with an existing real stack is rejected. Empty inputs display a muted ghost and a gold filter marker; occupied filtered inputs retain the gold marker. Inventory editing works with JEI absent. Filters are saved on the block entity and synchronized to all viewers; edit mode belongs to each open menu and is not saved.

## Automation and compatibility

Standard NeoForge item-handler capabilities support real hoppers and compatible pipes. Top and horizontal sides insert into either enabled input, observing filters. Bottom extracts output only. Unsided access combines these policies. Simulation never mutates inventory. Without filters, automation fills the first compatible empty input; set one filter per ingredient for reliable two-input routing.

Old two-slot NBT keeps input at index 0 and output at index 1; loading expands it with an empty index 2. Existing recipe IDs and the `ingredient` / `fragment_count` / `processing_time` format remain supported. New machine recipes may specify `secondary_ingredient` and an item-stack `result`:

```json
{
  "type": "forever_skyblock:condensing",
  "ingredient": { "item": "forever_skyblock:stone_meal" },
  "secondary_ingredient": { "tag": "minecraft:saplings" },
  "result": { "id": "minecraft:dirt", "count": 1 },
  "processing_time": 160
}
```

JEI 19.64.0.454 shows both inputs, interchangeable ordering and the required adjacent module as a non-consumed catalyst. Clicking condenser progress opens its recipe category. New crafting recipes use the standard crafting category. The fixed starter island NBT, ME inventory and runtime mod snapshot are unchanged.

## Validation

Build and dedicated-server integration verification are pending for the initial implementation commit. Added tests cover adjacency/live menu state, ingredient swapping and module removal, real filtered hoppers, ghost safety and server validation, legacy NBT and saved filters/hidden drops, blocked outputs, single-input fallback, mortar remainders and recipe network serialization. Existing skills and Jetpack tests remain part of the same CI runs. Client visuals and actual JEI drag interaction require an in-game test by TeKatz.
