# Forever Skyblock — 0.5.0

Minecraft 1.21.1 / NeoForge 21.1.249 or newer 21.1.x. Requires Java 21. Maintained by TeKatz; Java package and Gradle group: `de.tekatz.foreverskyblock`.

## Persistent skills

Press **K** to open the new Skills menu, or remap **Open Skills** in the **Forever Skyblock** keybind category. Craft cobblestone from four Stone Fragments to earn one profile-wide point. Buy **Extended Reach** for one point to gain +1 block interaction range across singleplayer worlds in this instance.

Points, milestone claims and purchases are saved outside world folders. Dedicated servers keep their own profiles; no client profile is imported. The card-based GUI has Skills/Tasks tabs. Each card shows its effect or objective, rank/reward, price and action together. Reset skills opens a confirmation dialog and refunds purchased ranks while keeping completed tasks. The green point reward in chat links directly to the menu. See [SKILLS.md](../docs/SKILLS.md) for scope, storage, commands, extension points and manual test steps.

## Iron Jetpacks research test

The **Jetpacks** tab adds Beginner (5 SP), Mid-Game (10 SP), End-Game (20 SP) and Creative (40 SP), in order. Locked jetpacks and components are hidden from JEI; supported crafting and flight paths enforce the research on the server. The temporary **TEST: +10 SP** button is available to the singleplayer owner or server operators. Existing profiles remain compatible. See [JETPACK_RESEARCH.md](../docs/JETPACK_RESEARCH.md) for tier mapping, shared automation policy, reset behavior and the conditional Creative crafting recipe.

Integration baseline: Iron Jetpacks 8.0.11, optional at runtime; JEI 19.64.0.454 remains optional. Client and server must both use 0.5.0. Iron Furnaces is newly reported installed by TeKatz but is not modified.

## Condenser Mark I

Right-click `forever_skyblock:condenser_mark_i` to open its compact two-slot interface. Insert logs or planks on the left and collect Stone Fragments on the right. No fuel, water or power is required.

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

`forever_skyblock:stone_fragment` uses a custom transparent 16×16 pixel texture depicting one angular stone shard and appears in the Ingredients creative tab. Four fragments in a 2×2 crafting recipe produce one cobblestone. Version 0.6.0 adds the reusable Mortar, Stone Meal and Mixing Chamber Mark I; see [mixing and filters](../docs/CONDENSER_MIXING.md). Later condenser tiers and tool recipes remain unchanged.

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

Replace the previous Forever Skyblock JAR with `ForeverSkyblock-1.21.1-NeoForge-0.6.0.jar` in the instance's `mods` directory. Install on both client and server for multiplayer.

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
