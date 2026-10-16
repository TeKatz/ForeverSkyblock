# Forever Skyblock mod — 0.13.4

Minecraft 1.21.1 / NeoForge 21.1.x. Java 21. Maintained by TeKatz. Java package and Gradle group: `de.tekatz.foreverskyblock`.

This directory is the standalone Gradle project for the custom Forever Skyblock mod. The repository-level current status is documented in [../docs/CURRENT_STATE.md](../docs/CURRENT_STATE.md).

## Current progression model

- Profile schema: **4**
- Network protocol: **skills-6**
- Active personal challenges: **20 / 58 SP**
- Permanent tiers: **Tier 0 — Foundations** and **Tier I — Conqueror**
- Jetpack research: 5 / 10 / 20 / 40 SP; Creative also costs 1 PP
- Current-world shop: only the owner/operator Phase 3 Test Entitlement is active; it costs 5 SP, requires Tier I, is one-time per stable world identity and deliberately delivers nothing

Profiles preserve SP, PP, challenge evidence, accounting, skills/research, permanent tier, world claims/receipts and redeemed Core identities. Matching client/server mod versions are required for the current progression protocol.

## K menu

Press **K** or use `/foreverskills` to open the integrated Design-2 progression menu:

- Overview
- Challenges
- Shop
- Research
- Perks
- Profile

The 0.13.4 revision uses the screenshot-driven status/header alignment, real optional-integration challenge icons and explicit non-overlapping card lanes. Playtime and Island Level remain unavailable placeholders until authoritative sources exist. See [K_MENU_DESIGN2.md](../docs/K_MENU_DESIGN2.md).

## Challenges

Active challenge integrations are deliberately limited to actions with a trustworthy player/owner boundary:

- Forever crafting/smithing and permanent tools
- AE2 personal storage/crafting actions and player-requested autocrafting completion
- Mekanism personal Infuser crafting and owner-bound successful Digital Miner extraction
- Powah player-taken Energizing Orb output
- Mystical Agriculture personal Supremium crafting
- Nether/End entry, Wither/Dragon credit and persistent Monster kills

Shared-machine objectives without an approved attribution rule remain disabled. See [CHALLENGES.md](../docs/CHALLENGES.md).

## Condenser and Mixing Chamber

`forever_skyblock:condenser_mark_i` processes:

| Input | Output | Default time |
| --- | --- | --- |
| 1 item in `minecraft:logs` | 4 Stone Fragments | 160 ticks |
| 1 item in `minecraft:planks` | 1 Stone Fragment | 160 ticks |

An adjacent Mixing Chamber enables the Stone Meal + sapling dirt recipe and the second filtered input. Processing pauses safely on full output; inventory/progress persist; supported sided item capabilities allow hopper/pipe automation. JEI provides recipe display and server-validated transfer. See [CONDENSER_MIXING.md](../docs/CONDENSER_MIXING.md).

## Permanent tools

The approved 24 Vanilla pickaxes, axes, shovels and hoes use the tiered permanent-tool recipe path. Gold branches from Stone; Diamond requires the Iron-tier path plus Obsidian; Netherite uses Reinforced Netherite Ingot in smithing. See [TOOL_PROGRESSION.md](../docs/TOOL_PROGRESSION.md).

## Ascension and Perks

A crafted Forever Core can be personally redeemed in the Ascension Chamber for one persistent Perk Point. Creative Jetpack research requires 40 SP + 1 PP after End-Game research. Core identities, profile receipts and chamber handling prevent duplicate payout. See [ASCENSION.md](../docs/ASCENSION.md).

## Build

From this directory with Java 21:

```sh
./gradlew build
```

Windows:

```bat
gradlew.bat build
```

Output: `build/libs/ForeverSkyblock-1.21.1-NeoForge-0.13.4.jar`.

The default NeoForge build baseline is 21.1.249. Use the confirmed pack loader for integration work with:

```sh
./gradlew <task> -PpackRuntime=21.1.259
```

## Test suites

Baseline dedicated Condenser suite:

```sh
./gradlew runGameTestServer -PcondenserTests --no-daemon
```

Current pack-integration suite:

```sh
./gradlew runGameTestServer -PcondenserTests -PjetpackTests -PtoolIntegrationTests -PascensionTests -PtechChallengeTests -PpackRuntime=21.1.259 --no-daemon
```

GitHub Actions run #151 passed for the final 0.13.4 feature head before it was merged into `main`. See [VALIDATION.md](../docs/VALIDATION.md) for detailed historical evidence and the remaining client/manual checks.

## Deliberately deferred

The current mod does not activate repeatable contracts, Tier II-IV, valuable world-shop delivery, Industrial Foregoing rewards or unresolved shared-machine objectives. Do not implement those from old planning text without the corresponding approval.
