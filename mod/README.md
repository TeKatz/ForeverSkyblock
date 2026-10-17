# Forever Skyblock mod — 0.16.0 Phase 7 polish test

Minecraft 1.21.1 / NeoForge 21.1.x. Java 21. Maintained by TeKatz. Java package and Gradle group: `de.tekatz.foreverskyblock`.

This branch is a **general stability and polish pass** on top of the 0.15.0 Phase 6 development head. It deliberately does not finalize the pack economy, Challenge/Contract rewards, Shop pricing, Tier II-IV progression or the K-menu design. Those decisions are postponed until the wider modpack progression has a stronger structure.

Branch: `chore/general-polish-phase7`. It is not `main` and it is not a published release.

## Phase 7 scope

- Harden progression network requests so unknown actions, malformed IDs and unexpected payload data are rejected before reaching economic handlers.
- Tighten snapshot validation without changing the `skills-8` wire format.
- Correct the real World Shop confirmation flow so it no longer falls through to the obsolete Phase-3 test-entitlement confirmation.
- Let a Supply Cache reuse its own main-inventory slot while opening, avoiding an unnecessary extra free-slot requirement.
- Keep Phase 5 contracts, Phase 6 package delivery, schema 4/5/6 migration and existing challenge attribution behavior unchanged.
- Expand regression coverage for the World Shop claim request and malformed progression requests.
- Keep the starter island and pack gameplay balance untouched.

## Deliberately deferred

The current values remain **development values**, not final pack balance. Phase 7 does not retune the 20 unique challenges, repeatable-contract quantities/rewards, Jetpack costs, the 12-SP AE2 package or permanent Shop tiers. Tier II-IV remain inactive. A larger K-menu design pass is also deferred until the pack progression has been developed further.

## Persistence and compatibility

The profile format is unchanged from 0.15.0: untouched profiles can remain schema 4, contract state uses schema 5, and real package delivery state uses schema 6. The network protocol remains `skills-8` because Phase 7 changes validation rather than packet shape.

Back up `forever_skyblock/progression/` and the test world's player data before testing a profile that already uses schema 6.

## Build

```sh
./gradlew build
```

Output: `build/libs/ForeverSkyblock-1.21.1-NeoForge-0.16.0.jar`.

Default development baseline: NeoForge 21.1.249. Pack-runtime integration: NeoForge 21.1.259.

## Test suites

```sh
./gradlew runGameTestServer -PcondenserTests --no-daemon
./gradlew runGameTestServer -PcondenserTests -PjetpackTests -PtoolIntegrationTests -PascensionTests -PtechChallengeTests -PpackRuntime=21.1.259 --no-daemon
```

See [POLISH_PHASE7.md](../docs/POLISH_PHASE7.md) for the exact boundary and manual acceptance checklist. Do not merge this branch into `main` until the 0.16.0 JAR has been accepted in the real pack.
