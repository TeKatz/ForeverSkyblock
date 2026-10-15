# Forever Skyblock — 0.1.0

Minimal standalone mod for Minecraft 1.21.1 / NeoForge 21.1.249 or newer 21.1.x. Requires Java 21.

## Content

Exactly one block and its block item: `forever_skyblock:condenser_mark_i`.

- English: Condenser Mark I; German: Kondensator Mark I.
- Custom 32 × 32 pixel machine textures and matching inventory model.
- Available in the Functional Blocks creative tab and creative search.
- Drops itself when mined with a pickaxe; normal full-cube collision and occlusion.
- No resource generation, GUI, inventory, energy system, recipe or dependencies on other mods in this initial version.

## Install

Copy `ForeverSkyblock-1.21.1-NeoForge-0.1.0.jar` into the instance's `mods` folder. Install on both client and server for multiplayer.

Give command:

```mcfunction
/give @s forever_skyblock:condenser_mark_i
```

## Build

With a Java 21 JDK installed:

```sh
./gradlew build
```

On Windows, use `gradlew.bat build`. Output is in `build/libs`.

For development: `./gradlew runClient` or `./gradlew runServer`. The dedicated server requires accepting the Minecraft EULA in its run directory before launching.

The mod uses standard block models and no client-only Java references. This first version intentionally has no block entity or ticking code.
