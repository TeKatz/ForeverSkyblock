# Forever Skyblock mod — 0.15.0 Phase 6 test

Minecraft 1.21.1 / NeoForge 21.1.x. Java 21. Maintained by TeKatz. Java package and Gradle group: `de.tekatz.foreverskyblock`.

This development branch layers **Phase 6 real current-world shop delivery** on top of the green 0.14.0 Phase 5 contract build. It is not `main` and it is not a published release. The integrated `main` baseline remains 0.13.4 until TeKatz explicitly approves later integration.

## Real current-world product

K → Shop → Current World now exposes one deliberately bounded real package:

**Advanced AE2 Starter Package** — Tier I, **12 SP / 0 PP**, once per player and stable world identity.

Contents:

- 1× AE2 ME Drive
- 2× AE2 Pattern Provider
- 2× AE2 Molecular Assembler
- 16× AE2 Blank Pattern
- 16× AE2 Fluix Smart Cable
- 1× AE2 Dense Energy Cell

The package deliberately contains no storage cell, 64k Crafting Storage or Wireless Crafting Terminal, so buying it does not substitute for those personal challenge actions. The server validates all item IDs against the installed registry; if a required entry is absent, the product fails closed instead of substituting another item.

## Delivery transaction

A purchase no longer inserts valuable anonymous items directly. It commits the 12-SP debit, current-world claim, immutable purchase receipt and a **READY** delivery entitlement together. Claiming that entitlement issues one non-stackable `forever_skyblock:world_supply_cache` bound to the exact receipt, player and world.

Delivery stages are `READY → DISPATCHING → DELIVERED → OPENING → OPENED`. The profile transition is journaled and player data is forced to disk around inventory changes. Login/menu-open recovery reconciles an interrupted DISPATCHING or OPENING stage before another economic action proceeds. Duplicate request/cache copies cannot reopen an already OPENED receipt.

The cache may only be opened by its owning player in its purchasing world. Its contents come from the server catalog, never from client or item display data. Package opening requires enough ordinary inventory slots and fails closed when the state is ambiguous.

## Progression model

- Profile schema stays **4** for untouched pre-contract profiles.
- Contract-only state remains **schema 5**.
- The first real delivery entitlement upgrades that profile to **schema 6**.
- Network protocol: **skills-8**.
- Phase 5 repeatable contracts remain active on this branch.
- Permanent tiers remain Tier 0 / Tier I; Tier II-IV stay inactive.
- Existing 20 unique challenges / 58 SP, Jetpack research and Ascension/PP rules are unchanged.

## Build

With Java 21:

```sh
./gradlew build
```

Windows:

```bat
gradlew.bat build
```

Output: `build/libs/ForeverSkyblock-1.21.1-NeoForge-0.15.0.jar`.

The default build baseline is NeoForge 21.1.249. Pack integration uses `-PpackRuntime=21.1.259`.

## Test suites

```sh
./gradlew runGameTestServer -PcondenserTests --no-daemon
./gradlew runGameTestServer -PcondenserTests -PjetpackTests -PtoolIntegrationTests -PascensionTests -PtechChallengeTests -PpackRuntime=21.1.259 --no-daemon
```

`./gradlew build` runs the pure delivery/schema/contract tests. The pack integration suite also verifies every AE2 item ID used by the real package. Client rendering, confirmation flow and real crash/restart acceptance still require TeKatz's in-game test before this branch should be merged.
