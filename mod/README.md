# Forever Skyblock — 0.10.0 (meta-progression phase 1)

Minecraft 1.21.1 / NeoForge 21.1.249 or newer 21.1.x. Requires Java 21. Maintained by TeKatz; Java package and Gradle group: `de.tekatz.foreverskyblock`.

## Progression navigation foundation

The K menu now uses six main sections: Overview, Challenges, Shop, Research, Perks and Profile. Existing skills are under Shop → Permanent; Jetpacks are under Research; reset and owner/operator test controls are under Profile. Challenges have category, search and completion filters. This phase adds no new rewards, contracts, world purchases or active Shop Tiers. Profiles retain schema 2. See [META_PROGRESSION.md](../docs/META_PROGRESSION.md) for the architecture, implementation stages and manual acceptance checklist.

## Machine visual update

This branch includes the 0.9.0 machine models, animated internals and matching GUIs. The 0.9.1 follow-up corrects item scaling, slot labels and the core-slot border, removes Inventory headings, raises both inventories and strengthens button hover/focus feedback. The meta-progression branch retains these visual changes. Main remains on 0.8.0 until an explicitly requested merge. Client visual acceptance is pending.

## Persistent skills

Press **K** to open the new Skills menu, or remap **Open Skills** in the **Forever Skyblock** keybind category. Craft cobblestone from four Stone Fragments to earn one profile-wide point. Buy **Extended Reach** for one point to gain +1 block interaction range across singleplayer worlds in this instance.

Points, milestone claims and purchases are saved outside world folders. Dedicated servers keep their own profiles; no client profile is imported. The card-based GUI has Skills/Tasks tabs. Each card shows its effect or objective, rank/reward, price and action together. Reset skills opens a confirmation dialog and refunds purchased ranks while keeping completed tasks. The green point reward in chat links directly to the menu. See [SKILLS.md](../docs/SKILLS.md) for scope, storage, commands, extension points and manual test steps.

## Iron Jetpacks research test

The **Jetpacks** tab adds Beginner (5 SP), Mid-Game (10 SP), End-Game (20 SP) and Creative (40 SP + 1 PP), in order. Locked jetpacks and components are hidden from JEI; supported crafting and flight paths enforce the research on the server. The temporary **TEST: +10 SP** button is available to the singleplayer owner or server operators. Existing profiles migrate as described in the Ascension section. See [JETPACK_RESEARCH.md](../docs/JETPACK_RESEARCH.md) for tier mapping, shared automation policy, reset behavior and the conditional Creative crafting recipe.

Integration baseline: Iron Jetpacks 8.0.11, optional at runtime; JEI 19.64.0.454 remains optional. Use matching client/server versions for this test. TeKatz's supplied list specifies Iron Jetpacks 8.0.11 and Iron Furnaces 4.3.2; Iron Furnaces is not modified.

## Condenser Mark I

Right-click `forever_skyblock:condenser_mark_i` to open its compact interface. Insert logs or planks on the left and collect Stone Fragments on the right. An adjacent Mixing Chamber reveals a second input for Stone Meal plus a sapling into dirt. No fuel, water or power is required.

| Input consumed per operation | Output | Default duration |
| --- | --- | --- |
| 1 item in `minecraft:logs` | 4 Stone Fragments | 160 ticks / 8 seconds at 20 TPS |
| 1 item in `minecraft:planks` | 1 Stone Fragment | 160 ticks / 8 seconds at 20 TPS |

The whole result must fit before processing advances. A blocked output pauses progress and consumes no input. Input is consumed only when the operation finishes. Changing the recipe or removing the input resets progress. Inventory and partial progress are saved with the block entity; there is no offline production or chunk loading.

The GUI shows progress, moving cyan indicators and idle/processing/output-full status. The block emits a subtle light while processing. Shift-click transfers supported inputs and collected output between the machine and player inventory. Breaking the block drops its stored input and output; partial work is discarded.

## Automation

The standard NeoForge item-handler block capability is exposed for hoppers and compatible pipes:

- Top and horizontal sides: insert supported, filter-matching inputs into slot 0 and, while a Mixing Chamber is adjacent, slot 2.
- Bottom: extract the produced output from slot 1.
- Unsided access: insert input and extract output.
- Automation cannot extract input or insert output. Simulated operations do not change inventory.

Each input and the output hold at most one stack of 64 items. Tagged wood from other mods works when included in the corresponding Minecraft tags.

## Stone Fragments and crafting

`forever_skyblock:stone_fragment` uses a custom transparent 16×16 pixel texture depicting one angular stone shard and appears in the Ingredients creative tab. Four fragments in a 2×2 crafting recipe produce one cobblestone. Version 0.6.0 added the reusable Mortar, Stone Meal and Mixing Chamber Mark I; see [mixing and filters](../docs/CONDENSER_MIXING.md). Later condenser tiers are not implemented; permanent-tool progression is documented below.

The condenser remains in Functional Blocks. Its registered ID and original cube textures are preserved. It drops itself with a pickaxe; stored inventory is dropped separately when the block is removed. Existing placed condensers acquire their new block entity when loaded; test a copy of the starter template before replacing a distributed pack.

## JEI and recipe data

JEI is optional. The integration is compiled against **19.64.0.454**, with a declared client compatibility range of `[19.64.0.454,20)`. The mod also loads without JEI and on a dedicated server.

The Condensing category lists the actual machine recipes, cycles tagged inputs and shows output quantities, duration and zero power requirement. The condenser is the recipe catalyst; clicking the progress area in its GUI opens this category. The mortar, meal, module and cobblestone recipes appear in JEI's standard crafting category.

Recipes use the custom serializer `forever_skyblock:condensing`:

```json
{
  "type": "forever_skyblock:condensing",
  "ingredient": { "tag": "minecraft:logs" },
  "fragment_count": 4,
  "processing_time": 160
}
```

`fragment_count` accepts 1–64; `processing_time` accepts 1–32767 ticks and defaults to 160. Legacy recipes without `result` produce Stone Fragments. An explicit `result` permits other outputs, and an optional `secondary_ingredient` requires an adjacent Mixing Chamber with both real inputs. Bundled recipes live under `data/forever_skyblock/recipe/condensing/`; datapacks can override them. Changes take effect through the server recipe manager after `/reload`; reopening JEI or rejoining may be necessary to refresh its recipe display. Recipe Editor currently edits crafting recipes, not this machine recipe type.

## Install and build

Replace the previous Forever Skyblock JAR with `ForeverSkyblock-1.21.1-NeoForge-0.8.0.jar` in the instance's `mods` directory. Install on both client and server for multiplayer.

With Java 21 installed:

```sh
./gradlew build
```

Windows: `gradlew.bat build`. Output: `build/libs/`.

For development: `./gradlew runClient` or `./gradlew runServer`. Development client runs include JEI 19.64.0.454. The dedicated server requires acceptance of the Minecraft EULA in its run directory.

Run the isolated dedicated-server integration tests with:

```sh
./gradlew runGameTestServer -PcondenserTests
```

Test classes and their empty structure are in `src/gameTest/` and are excluded from the production JAR. See [validation status](../docs/VALIDATION.md) for completed checks and remaining manual tests.

## Condenser mixing in 0.6.0

See [CONDENSER_MIXING.md](../docs/CONDENSER_MIXING.md) for recipes, the live second input, persistent filters, automation and migration. Filter editing copies items without consuming them; right-click clears an input filter. JEI is optional.

## JEI transfer and Vanilla tutorial hints in 0.6.1

Open the Condensing category through the condenser progress area (JEI's **Show all recipes**), then press the recipe **+** to load one set from the player inventory. Shift-click loads the largest complete set count, bounded by stack sizes. Already loaded matching inputs can count toward the recipe. The plus remains visible but disabled if ingredients, the required Mixing Chamber or return space are missing, if filters conflict, while editing ghosts, or while holding a cursor item. Hover for the reason. The server rechecks the recipe, module, filters, distance and current items before changing either inventory; a failed transfer moves nothing. Displaced real inputs are returned to the inventory without dropping items. Hidden secondary inputs, filters and output are retained. The JEI completion callback waits for the server result. JEI remains optional.

Vanilla's initial movement, tree, inventory and crafting tutorial sequence is set to `none` on client login using Minecraft's Tutorial API, which persists that option. This applies to new worlds, existing worlds and multiplayer clients with this mod. Quest notifications, advancement toasts and other mods' tutorials are unaffected. No complete `options.txt` is shipped or overwritten.

The Mixing Chamber model now uses disjoint solid partitions with only exposed faces, cull tags only on actual block boundaries and a non-occluding block state, preventing overlapping wood/stone surfaces and missing neighboring log faces.

## Permanent tools in 0.7.0

The 24 Vanilla pickaxes, axes, shovels and hoes have infinite durability and replacement progression recipes. Gold branches from Stone; Diamond needs Iron and Obsidian; Netherite uses the smithing table with the new Reinforced Netherite Ingot. Standard recipe JSON is bundled in this JAR and mirrored in the pack Recipe Editor directory. See [TOOL_PROGRESSION.md](../docs/TOOL_PROGRESSION.md) for exact patterns, scope, recipe replacement rules, open resource dependencies and validation. No separate datapack installation is needed for the JAR test.

## Ascension in 0.8.0

Craft a Forever Core and personally redeem it in the Ascension Chamber for one Perk Point. Creative Jetpack research now costs 40 SP + 1 PP after EndGame research. The K menu shows both balances and a Perks tab. Confirmed reset refunds each currency separately. Legacy Creative purchases are removed and their 40 SP refunded without minting PP. See [ASCENSION.md](../docs/ASCENSION.md) for exact recipes, the Vanilla Crafter automation route, intentionally blocked AE2 automatic Core patterns, claim replay and storage requirements. Both client and server need 0.8.0; no extra datapack installation is needed for the test JAR.
