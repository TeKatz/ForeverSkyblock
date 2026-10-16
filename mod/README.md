# Forever Skyblock mod — 0.14.0 Phase 5 test

Minecraft 1.21.1 / NeoForge 21.1.x. Java 21. Maintained by TeKatz. Java package and Gradle group: `de.tekatz.foreverskyblock`.

This branch adds the first repeatable-contract implementation on top of the integrated 0.13.4 baseline. It is a **test branch**, not `main` and not a published release. See [CONTRACTS.md](../docs/CONTRACTS.md).

## Phase 5 contracts

K → Challenges → Contracts now contains five live repeatables:

- Dragon Reborn: later respawned Dragons, +1 SP each;
- Wither Hunter: 3 additional Withers, +1 SP;
- 4 AE2 Singularities, +1 SP;
- 32 Powah Nitro Crystals, +1 SP — provisional test quantity;
- 4 Mekanism Antimatter Pellets, +3 SP.

All require permanent Tier I. The original Dragon/Wither kills remain one-time challenge rewards and never double-pay the repeatable on that same kill. Resource contracts use explicit server-authoritative turn-in buttons and consume the configured items.

The resource transaction uses a durable pending intent plus forced pre/post player-data checkpoints around inventory removal. Recovery compares exact before/after item counts and fails closed on an ambiguous state. Request UUID replay guards prevent a lost response from turning one click into multiple payouts.

## Progression model

- Profile schema: **4 until the first contract state is written, then 5**
- Network protocol: **skills-7**
- One-time challenge catalog remains **20 / 58 SP**
- Permanent tiers remain Tier 0 / Tier I; Tier II-IV are still inactive
- Jetpack research and PP/Ascension behavior are unchanged
- Existing current-world test entitlement and schema-4 world claims/receipts remain intact

## Existing systems retained

The branch retains the 0.13.4 Design-2 K menu, Condenser/Mixing Chamber, permanent tools, Jetpack research, Ascension/Perk Points, Phase 4 technology challenges and crash-safe current-world purchase infrastructure. Shared-machine challenges without an approved attribution rule remain disabled.

## Build

With Java 21:

```sh
./gradlew build
```

Windows:

```bat
gradlew.bat build
```

Output: `build/libs/ForeverSkyblock-1.21.1-NeoForge-0.14.0.jar`.

The default build baseline is NeoForge 21.1.249. Pack integration uses `-PpackRuntime=21.1.259`.

## Test suites

```sh
./gradlew runGameTestServer -PcondenserTests --no-daemon
./gradlew runGameTestServer -PcondenserTests -PjetpackTests -PtoolIntegrationTests -PascensionTests -PtechChallengeTests -PpackRuntime=21.1.259 --no-daemon
```

`./gradlew build` also runs the pure Phase 5 contract/profile tests. Do not merge this branch into `main` until the 0.14.0 JAR has been accepted in the real pack.
