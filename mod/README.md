# Forever Skyblock mod — 0.17.0 early-game / world QoL test

Minecraft 1.21.1 / NeoForge 21.1.x. Java 21. Maintained by TeKatz. Java package and Gradle group: `de.tekatz.foreverskyblock`.

This development branch builds on the 0.16.0 general-polish head and targets concrete pacing issues found while actually playing the early game. It does not finalize the wider pack economy or K-menu design.

Branch: `feature/earlygame-world-qol`. It is not `main` and it is not a published release.

## Condenser pacing recipe

The Condenser Mark I now supports counted primary inputs. A new recipe converts **8 items in `minecraft:leaves` into 1 Bone Meal in 160 ticks (8 seconds)**. This is intended to pair with manually harvested leaves, including leaves obtained with a separate shears mod, without making that external mod a required dependency of Forever Skyblock.

JEI and recipe transfer use the same counted-input definition as the server machine. Existing log, plank and Mixing Chamber recipes remain compatible because their omitted `ingredient_count` defaults to one.

## Wandering Trader world gate

Wandering Traders are blocked until the current save reaches its first Emerald pickup. When a real player first picks up an Emerald, the world stores the unlock and sends **“The Wandering Trader can now spawn”** (localized EN/DE). The unlock only permits normal/random spawning; it does not force a Trader to appear immediately.

The unlock is world-wide and persists with the save rather than a player profile.

## World QoL upgrades

K → Shop → Current World now also exposes permanent world-wide upgrades:

- **No Phantoms — 1 SP**: disables insomnia Phantom spawning and blocks new Phantom entity joins.
- **No More Rain — 2 SP**: clears rain/thunder and disables the weather cycle.
- **Permanent Day — 3 SP**: sets daytime to 1000 and disables the daylight cycle.

These upgrades currently use **Tier 0** as their minimum Shop Tier. A purchase is charged once for the stable world identity and becomes active for every player in that world. Other players mirror the active world claim without paying again.

World state is stored with primary/backup copies under `data/forever_skyblock_world_progress.json(.bak)` and is bound to the existing stable world UUID. A paid player-profile claim is also the recovery source if the server stops between the SP debit and activation of the world-wide effect.

## Compatibility

No new player-profile schema is introduced. Existing schema 4/5/6 behavior remains unchanged and the client/server protocol remains `skills-8`. The new persistent file belongs to the world save.

The fixed starter island NBT is unchanged. No third-party Wooden Shears JAR is bundled or committed by this branch.

## Build

```sh
./gradlew build
```

Output: `build/libs/ForeverSkyblock-1.21.1-NeoForge-0.17.0.jar`.

Default development baseline: NeoForge 21.1.249. Pack-runtime integration: NeoForge 21.1.259.

See [EARLY_GAME_QOL.md](../docs/EARLY_GAME_QOL.md) for persistence and manual acceptance details.
