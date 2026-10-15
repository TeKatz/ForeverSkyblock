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

Source commit `11c602b5a0df0daa9106c1797643db3e8042613c` passed [GitHub Actions run 38718605502](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38718605502): build, all 14 JUnit tests, and all 21 dedicated-server GameTests on both NeoForge 21.1.249 without optional mods and NeoForge 21.1.259 with Iron Jetpacks 8.0.11 / Cucumber 8.0.16 / AE2 19.2.18 / GuideME 21.1.19. Seven added GameTests cover adjacency/live menu state, ingredient swapping and module removal, real filtered hoppers, ghost safety and server validation, legacy NBT and saved filters/hidden drops, blocked outputs, single-input fallback, mortar remainders and recipe network serialization. Existing skills and Jetpack tests remain part of the same CI runs. Client visuals and actual JEI drag interaction require an in-game test by TeKatz.

The verified production JAR is `ForeverSkyblock-1.21.1-NeoForge-0.6.0.jar`, 132,387 bytes, SHA-256 `19b32bf4c9855d46262f6cca4ca3bc8bd0969d4bee109d1748d8232f56a7065b`. Artifact ID: `11963717159`. Metadata, Java 21 class version, new assets/registrations and exclusion of test classes were inspected. Existing progression/mixin class bytes match the delivered 0.5.0 JAR. The island NBT SHA-256 remains `1ae2e4b779b1c6b56320aede0af2a711d4bd247a976771c95f633e05c1beb0c1`. Documentation-only follow-up changes do not alter the tested mod source.

## Model and JEI transfer update — 0.6.1

The wood housing and stone posts no longer occupy overlapping solid volumes or emit overlapping coplanar faces. Inset top/basin surfaces have no boundary cull tag. The chamber is non-occluding so neighboring blocks retain faces visible through its open upper region. Geometry and all six neighboring log-face visibility checks are covered by focused tests.

JEI now registers an advanced transfer handler for the condenser. The normal plus loads one complete recipe set; Shift-plus fills as many complete sets as fit. Matching real inputs are retained and can count toward the recipe. Either dirt-input ordering is supported and existing filters determine valid arrangements. Missing ingredients, conflicting filters, a missing chamber, active filter editing, a carried item or insufficient space to return old inputs disable the visible plus and provide a tooltip. No items are moved during JEI availability checks. The server receives a recipe ID and request parameters, resolves the current recipe and independently plans from the current inventories. It applies only a complete successful plan, preserving components, filters, output and any hidden secondary stack. No new skill or island changes are included.

Vanilla introductory tutorial hints are disabled on client login through `Tutorial.setStep(TutorialSteps.NONE)`. This persists the Vanilla tutorial setting rather than distributing an entire options file. Other notifications and quest tutorials remain available.

Validation of the 0.6.1 build and new tests is pending the initial implementation CI run. Actual client visuals, JEI mouse transfer and fresh-instance tutorial behavior still require an in-game test.
