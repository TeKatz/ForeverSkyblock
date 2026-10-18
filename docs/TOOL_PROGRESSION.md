# Permanent mining and farming tools — 0.7.0

Implements TeKatz's supplied [complete handoff](TOOL_RECIPE_HANDOFF.md) on `feature/infinite-tools-recipe-overhaul`, based on main `16d96019274766e1e22c4748ea0d5b16f10b28c7`. TeKatz authorized its integration into `main` on October 15, 2026 alongside the Ascension update. This remains a test baseline, not a completed resource progression; no release is published by the integration.

## 0.23.0 Iron cost adjustment (development)

`feature/iron-balance-packed-mud` updates the four existing custom Iron tools following TeKatz's automated-Iron balancing feedback. The integrated/historical patterns below retain their earlier meaning; only Iron is changed in this development revision.

| Tool | Iron Block | Other Iron Ingots | Consumed core | Reinforcement |
| --- | ---: | ---: | --- | --- |
| Pickaxe | 1 | 4 | Stone Pickaxe | 1 Chain |
| Axe | 1 | 3 | Stone Axe | 1 Chain |
| Shovel | 1 | 2 | Stone Shovel | 1 Chain |
| Hoe | 1 | 2 | Stone Hoe | 1 Chain |

Exactly one original head-material position becomes an Iron Block; tool shape, matching Stone core and Chain position are retained. Full set: four Iron Blocks, eleven other Ingots, four Chains and four Stone tools. The existing personal Iron Tools challenge now verifies the new one-Block ingredient proof; old all-Ingot proof is rejected. Its reward and previously earned claims are unchanged. Recipe IDs and permanent durability are unchanged. Both bundled recipes and Recipe Editor snapshots match. Old all-ingot custom patterns no longer match; the existing authoritative tool filter still removes Vanilla/modded alternatives. Swords, armor and other excluded tools are outside this request. See [validation](MATERIAL_PROCESSING.md#0230-balancing-and-packed-mud-development).

## Exact scope and patterns

The 24 Vanilla pickaxes, axes, shovels and hoes across Wood, Stone, Iron, Gold, Diamond and Netherite are permanent. Swords, utility tools, armor and modded tools retain their existing recipes/durability for the separate design pass identified in the handoff.

| Tool | Rows (spaces are empty) | Primary material |
| --- | --- | ---: |
| Pickaxe | `XXX` / `XTX` / ` C ` | 5 |
| Axe | `XX ` / `XTX` / ` C ` | 4 |
| Shovel | ` X ` / `XTX` / ` C ` | 3 |
| Hoe | `XX ` / ` TX` / ` C ` | 3 |

| Tier | X | T | C |
| --- | --- | --- | --- |
| Wood | Any plank (`minecraft:planks`) | Stick | Stone Fragment |
| Stone | Stone Fragment | Matching Wooden tool | Stone Meal |
| Iron | Iron ingot | Matching Stone tool | Chain |
| Gold | Gold ingot | Matching Stone tool | Redstone dust |
| Diamond | Diamond | Matching Iron tool | Obsidian |

One set uses 15 primary materials and four reinforcements. The exact prior-tier tool is consumed, without a crafting remainder. Shaped upgrades use normal Vanilla output semantics and do not copy names/enchantments from the consumed core. Gold is an optional branch from Stone. The concrete recipes and final table in the handoff are authoritative over the schematic overview's Iron-to-Gold arrow.

The Reinforced Netherite Ingot recipe is `ONO` / `GDG` / `ONO`: N = netherite ingot, O = obsidian, G = gold ingot, D = diamond. One output costs two netherite ingots, four obsidian, two gold ingots and one diamond. Its registered ID is `forever_skyblock:reinforced_netherite_ingot`, with EN/DE translations and an original provisional 16-pixel sprite. Final art remains an open design decision.

Four standard `minecraft:smithing_transform` recipes use a Netherite Upgrade Smithing Template, the matching Diamond tool and the reinforced ingot. They retain Vanilla smithing component/name/enchantment transfer and consume the template normally. Netherite armor and swords retain their ordinary smithing recipes.

## Recipe Editor and recipe authority

All 25 recipe definitions and 24 explicit removal overrides are stored in `pack/config/forever_recipe_editor/generated_pack` using the actual 1.4.3-skyblock.1 directory structure and standard serializers. The same files are bundled byte-for-byte in the JAR so the test requires only replacing the mod JAR, without manual installation of another datapack. The mod never writes over an existing local Recipe Editor configuration. The current Recipe Editor GUI edits crafting recipes; smithing definitions remain standard JSON authored in the same data-pack tree.

The original 20 crafting and four tool-smithing IDs receive Recipe Editor-compatible `neoforge:false` condition overrides. The replacements use `forever_skyblock:tools/<tool_id>` (smithing adds `_smithing`). Own IDs avoid NeoForge's higher-priority Vanilla crafting replacements. The runtime recipe filter also removes every original/alternative static tool-output ID, so resource-pack priority cannot restore a cheap path. The new reinforced ingot has its own recipe ID. A bounded recipe-load/replacement filter removes other static-output recipes for these 24 tools before the authoritative recipe list is synchronized to clients/JEI. The new Forever tool IDs remain editable in Recipe Editor; keep those canonical IDs when balancing. Renamed/extra tool-output recipes are deliberately filtered out. No per-tick recipe scanning is used. Vanilla's special crafting repair recipe also rejects these permanent tools.

The current supplied 80-entry list has no newly inferred tool recipe dependencies. Optional tech components are not used. Other mods' arbitrary dynamic-output serializers or direct item-generating systems cannot be exhaustively certified from a filename list; the supplied pack should still be checked in JEI after installation. Villager trades, loot, commands and existing inventories are not crafting recipes and are not redesigned by this pass.

## Infinite durability

Durability policy is separate from recipe data. Only the 24 approved Vanilla item IDs receive default `minecraft:unbreakable` with the normal tooltip. ItemStack hooks also report no damageability/wear and normalize damage setter calls to zero for these IDs, protecting older stacks and mod integrations that use standard damage APIs even if a stack's default component was patched away. Existing stored wear is ignored without tick scans or inventory rewrites.

Normal mining, tool combat, hoeing, path creation and log stripping use the same protected stack behavior. Unbreaking and Mending may remain as enchantments but have no durability to conserve/repair; smithing preserves them. Their enchantment availability has not been redesigned. An integration that deliberately consumes an entire stack is not a durability API; crafting must still be able to consume the previous tool as specified.

FTB Ultimine 2101.1.15's actual source accepts TieredItem tools independently of damageability, and its normal block-break dispatch delegates to the server mining path. A conditional integration test loads the actual mod, checks eligibility and invokes its block-break dispatcher repeatedly. The complete client key/selection/raytrace flow still needs manual playtesting.

## Progression boundaries and testing

Wood is reachable from the fixed island's tree and powered condenser: punch logs, make planks/sticks, process a log for fragments, then craft through the existing ME terminal. Cobblestone from fragments allows the existing reusable mortar and Stone Meal, enabling Stone tools. No added recipe requires an inaccessible new machine before Wood/Stone.

First iron, water and lava acquisition remain intentionally open. Diamond recipes implement the requested Obsidian gate, but a complete pre-Diamond Obsidian production route has **not** been supplied. Vanilla mining of placed obsidian with a Diamond pickaxe cannot serve as its own prerequisite. The pack is therefore not claimed to have a playable metal-to-Diamond route yet. Validate/tune that dependency when the resource route is designed. No new iron output, water/lava recipe or acquisition shortcut was invented.

The final island NBT and ME contents are unchanged. The mod inventory remains the supplied 0.6.1 installation snapshot until TeKatz reports the test JAR installed. Existing condenser functions, filters, skill profiles, Jetpack research and JEI visibility remain included.

Tested source `b5109aa6dd6fd4f239a972c846bf5639fb834a63` passed [GitHub Actions run 38774235720](https://github.com/TeKatz/ForeverSkyblock/actions/runs/38774235720): Java 21 build, all 15 JUnit tests and all 34 dedicated-server GameTests in each run. The baseline used NeoForge 21.1.249 without optional integrations. The pack-loader run used NeoForge 21.1.259 with AE2 19.2.18, Iron Jetpacks 8.0.11, Cucumber 8.0.16, GuideME 21.1.19, FTB Ultimine 2101.1.15, FTB Library 2101.1.37 and Architectury 13.0.11. The Ultimine test asserts the real mod is loaded, checks its actual tool predicate and executes its private block-break dispatcher for 64 stone blocks; it does not simulate the client key/selection/raytrace flow.

Seven new GameTests cover all 20 exact shaped patterns and material counts, consumed cores and removal of legacy IDs/cheap patterns, plank tag variants, Stone-based Gold and Iron-plus-Obsidian Diamond gates, the reinforced-ingot recipe, all four smithing upgrades with preserved name/enchantments/custom data/repair cost, damage immunity for all 24 tools including component-patched and saved stacks, normal wear for excluded equipment, actual server mining/combat/hoe/path/axe actions, alternative crafting-output and smithing removal, the 24-recipe client sync, special crafting repair rejection and the real Ultimine dispatcher. Existing condenser/filter/JEI-transfer/skills/Jetpack/AE2 tests remain included.

The production JAR is `ForeverSkyblock-1.21.1-NeoForge-0.7.0.jar`, 175,018 bytes; SHA-256 `700aba24336f634a185bb8bfad238c3b0865883b2f018b7bb959fdb83246364c`. CI artifact ID: `11993071517`. Inspection confirmed 0.7.0/TeKatz metadata, Java 21 classes, all nine mixin declarations, the new item model/texture, all 49 recipe/removal resources matching the Recipe Editor files, no bundled third-party classes or test classes, and unchanged prior progression/condenser entity bytecode and condensing recipes compared with 0.6.1. The fixed island hash remains `1ae2e4b779b1c6b56320aede0af2a711d4bd247a976771c95f633e05c1beb0c1`; the supplied mod inventory is unchanged.

The initial in-place Vanilla crafting-ID approach was caught by integration tests because NeoForge supplied higher-priority old patterns. The final own-ID recipes plus explicit legacy-ID removals and authoritative filtering solve that issue; no failed build was delivered. Documentation-only follow-up edits do not change this verified mod source. Client JEI presentation, Recipe Editor interaction, actual starter-island playthrough, mining/Ultimine mouse behavior, timing/balance and a full multiplayer pack export remain manual tests. Automated checks must be listed separately from user testing.
