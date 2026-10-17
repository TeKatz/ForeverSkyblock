# Early-game pacing and world QoL — 0.17.0 development

Development branch: `feature/earlygame-world-qol`, based on the accepted 0.16.0 general-polish development head. No merge or release is implied.

This block addresses concrete friction found during real Skyblock play without attempting to finalize the full pack progression.

## Condenser: leaves to Bone Meal

The Condenser recipe model now supports a counted primary ingredient through `ingredient_count` (default `1`, preserving all old recipes).

New recipe:

- input: **8 × `#minecraft:leaves`**;
- output: **1 × `minecraft:bone_meal`**;
- duration: **160 ticks / 8 seconds**;
- power: none;
- Mixing Chamber: not required.

The machine, JEI display and JEI transfer planner all use the same quantity. Seven leaves do not start the recipe; completing a cycle consumes exactly eight. This creates an active tree → leaves → Bone Meal → tree-growth loop. Forever Skyblock does not add or bundle Wooden Shears; a shears mod may provide the harvesting tool independently.

## Wandering Trader unlock

Before the world unlock is recorded, Wandering Trader entity joins are canceled. The first successful pickup of an Emerald by a real server player sets the save-wide flag `wandering_trader_unlocked` and sends the localized message **“The Wandering Trader can now spawn”**.

The event does not create a trader. It only stops blocking future normal/random spawn attempts. The flag belongs to the save, not to the player who picked up the Emerald.

## Permanent world upgrades

The existing Current World Shop transaction system now distinguishes item Supply Packages from non-delivery World Upgrades.

| Upgrade | Cost | Tier | Effect |
| --- | ---: | --- | --- |
| No Phantoms | 1 SP | Tier 0 | disables the insomnia gamerule and blocks new Phantom entity joins |
| No More Rain | 2 SP | Tier 0 | clears current rain/thunder and disables weather cycling |
| Permanent Day | 3 SP | Tier 0 | sets Overworld daytime to 1000 and disables daylight cycling |

A World Upgrade is paid only once per stable world identity. Its effect is world-wide: after activation, other players receive the matching world claim for UI/idempotency purposes without a second SP charge. Supply-cache delivery remains limited to actual item packages and is unchanged.

## Persistence and recovery

World-wide state is stored in:

- `data/forever_skyblock_world_progress.json`
- `data/forever_skyblock_world_progress.json.bak`

The file stores schema 1, the stable world UUID, the Wandering Trader gate and the set of active world upgrades. It is validated against `forever_skyblock_world_identity.json` so world state cannot silently attach to a different save identity.

World-upgrade purchase order intentionally keeps the existing player-profile claim/receipt as the durable economic commit. If the SP debit succeeds but world-state activation is interrupted, login or opening the progression menu reconciles that paid claim into the world state. Once the world upgrade is active, active upgrade IDs are mirrored into current players' world claims so they cannot purchase the same global effect again.

This does not add a new player-profile schema: schema 4/5/6 and protocol `skills-8` remain in place.

## Automated validation

Required before handing off the test JAR:

1. Java 21 `./gradlew build --no-daemon`, including world-state and purchase unit tests;
2. dedicated regression GameTests, including the exact 8-leaves → 1-Bone-Meal machine cycle;
3. the full NeoForge 21.1.259 pack-runtime integration suite;
4. artifact generation from the exact final branch head.

## Manual acceptance

1. Back up `forever_skyblock/progression/` and the test world.
2. Verify 7 leaves do nothing in the Condenser; add an eighth and verify exactly 1 Bone Meal after 8 seconds and all 8 leaves are consumed. Check JEI quantity and transfer.
3. In a fresh/unlocked test world, verify a Wandering Trader cannot appear before an Emerald pickup. Pick up the first Emerald and verify the one-time message; do not expect an immediate Trader spawn.
4. Buy No Phantoms for 1 SP and confirm another player sees it as active without paying. Verify new Phantoms no longer spawn.
5. Buy No More Rain for 2 SP while raining if possible; weather should clear and stay clear through a restart.
6. Buy Permanent Day for 3 SP; time should move/set to daytime 1000 and remain there through a restart.
7. Reopen/reconnect with another player and confirm all active world upgrades stay marked active and cannot be purchased twice.
8. Recheck the AE2 Supply Package delivery, Contracts, Research, Condenser/Mixing Chamber, Ascension, JEI and permanent tools for regressions.

The starter-island NBT is intentionally untouched. Wooden Shears remains an external pack choice rather than a hard mod dependency.
