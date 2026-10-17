# Forever Skyblock mod — 0.18.1 integrated baseline

Minecraft 1.21.1 / NeoForge 21.1.x. Java 21. Maintained by TeKatz. Java package and Gradle group: `de.tekatz.foreverskyblock`.

Version **0.18.1** is integrated into `main` through PR #13. It combines the 0.14.0-0.18.1 development stack: repeatable contracts, real current-world package delivery, stability/polish work, early-game/world QoL, the Freezing Chamber Mark I and the Stone Meal Crafting Table progression gate. No release was published by the merge.

## Condenser family

The Condenser Mark I supports counted primary ingredients and persistent input locks. Existing log/plank, Mixing Chamber and JEI transfer behavior remains active.

### Bone Meal loop

A Condenser recipe converts **8 items in `minecraft:leaves` into 1 Bone Meal in 160 ticks / 8 seconds**. No Mixing Chamber and no power are required. JEI and recipe transfer use the same counted-input definition.

### Mixing Chamber Mark I

The passive adjacent Mixing Chamber exposes the second Condenser recipe input. Dirt remains the early two-input recipe using Stone Meal plus a Sapling.

### Freezing Chamber Mark I

The passive adjacent Freezing Chamber exposes a dedicated input/output pair inside the Condenser. The Chamber itself has no GUI or inventory.

- every registered item is valid as Freezing input unless the input lock restricts it;
- one consumed item advances independent material progress;
- **32 consumed items produce 1 `minecraft:ice`**;
- Freezing runs in parallel with normal Condenser/Mixing processing;
- removing the Chamber hides its slots while preserving input, output, partial progress and lock;
- disconnected module slots are not exposed to automation;
- the Freezing input is deliberately excluded from generic shift-click routing because an unlocked slot accepts everything.

The Condenser screen has four live layouts without reopening: base, Mixing only, Freezing only, and Mixing + Freezing. The 0.18.1 pass increased vertical spacing, separated Condensing and Freezing presentation, removed overlapping lock help tooltips and removed the old hidden JEI click target between the main input/output slots.

JEI includes a dedicated Freezing category showing the 32-item wildcard process, Ice output, no-power requirement and adjacent Freezing Chamber requirement.

## Crafting Table gate

The normal `minecraft:crafting_table` recipe is replaced by a 3x3 plus-shaped recipe using four Planks around one Stone Meal. The starter ME Crafting Terminal therefore remains the first 3x3 crafting workstation until Stone Meal is reached.

The recipe override is shipped by the mod and mirrored into the Forever Recipe Editor generated pack.

## Progression systems

The integrated progression protocol is `skills-8`. Player profiles serialize dynamically as schema 4, 5 or 6 depending on which systems have created state.

Current integrated systems include:

- 20 unique personal challenges worth 58 SP;
- permanent Shop Tier 0 and Tier I;
- five Tier-I repeatable contracts;
- the Advanced AE2 Starter Package at Tier I for 12 SP with receipt-bound Supply Cache delivery;
- world-wide No Phantoms, No More Rain and Permanent Day upgrades;
- save-wide Wandering Trader unlock after the first real-player Emerald pickup;
- Jetpack research, permanent skills/refunds, Ascension and Perk Points.

See `../docs/CURRENT_STATE.md` for the complete active/deferred boundary.

## Compatibility and fixed pack state

The fixed starter island NBT is unchanged. No third-party Wooden Shears JAR is bundled or committed by Forever Skyblock.

Default development baseline: NeoForge 21.1.249. Pack-runtime integration target: NeoForge 21.1.259.

## Build

```sh
./gradlew build
```

Output: `build/libs/ForeverSkyblock-1.21.1-NeoForge-0.18.1.jar`.

The final pre-merge feature head `97715f06391339141032c5f0c859b5925f99539d` passed GitHub Actions run #195: Java 21 build/unit tests, headless client mixin smoke, dedicated regression GameTests, full NeoForge 21.1.259 integration GameTests and artifact upload.

TeKatz subsequently confirmed the 0.18.1 in-game cleanup as a good baseline to continue from. This user-reported client acceptance is separate from the automated CI checks.
