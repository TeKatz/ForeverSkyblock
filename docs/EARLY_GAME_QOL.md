# Early-game pacing and world QoL — integrated 0.17.x behavior

The 0.17.x early-game/world-QoL work is integrated into the current 0.18.1 `main` baseline through PR #13. It addresses concrete friction found during real Skyblock play without attempting to finalize the full pack progression.

Historical development branch: `feature/earlygame-world-qol`. No release was published from that branch.

## Condenser: leaves to Bone Meal

The Condenser recipe model supports a counted primary ingredient through `ingredient_count` (default `1`, preserving older recipes).

Integrated recipe:

- input: **8 × `#minecraft:leaves`**;
- output: **1 × `minecraft:bone_meal`**;
- duration: **160 ticks / 8 seconds**;
- power: none;
- Mixing Chamber: not required.

The machine, JEI display and JEI transfer planner all use the same quantity. Seven leaves do not start the recipe; completing a cycle consumes exactly eight. This creates the active tree -> leaves -> Bone Meal growth loop.

Forever Skyblock does not add or bundle Wooden Shears; a separate shears mod may provide that harvesting tool independently.

## Wandering Trader unlock

Before the world unlock is recorded, Wandering Trader entity joins are canceled. The first successful pickup of an Emerald by a real server player sets the save-wide flag `wandering_trader_unlocked` and sends the localized message **“The Wandering Trader can now spawn”**.

The event does not create a trader. It only stops blocking future spawn attempts. The flag belongs to the save, not to the player who picked up the Emerald.

Development follow-up **0.22.0** on `feature/garden-wheat-polish` also blocks the entire Vanilla caravan attempt and fresh untamed Trader llamas until the same unlock. Saved/tamed Trader llamas and ordinary llamas are preserved. This fix is not integrated into `main`; see [the implementation and validation](MACHINE_ANIMATIONS.md#requested-pack-fixes).

## Permanent world upgrades

The Current World Shop distinguishes item Supply Packages from non-delivery World Upgrades.

| Upgrade | Cost | Tier | Effect |
| --- | ---: | --- | --- |
| No Phantoms | 1 SP | Tier 0 | disables the insomnia gamerule and blocks new Phantom entity joins |
| No More Rain | 2 SP | Tier 0 | clears current Overworld rain/thunder and disables weather cycling |
| Permanent Day | 3 SP | Tier 0 | sets Overworld daytime to 1000 and disables daylight cycling |

A World Upgrade is paid only once per stable world identity. Its effect is world-wide: after activation, other players receive the matching world claim for UI/idempotency purposes without a second SP charge. Supply-cache delivery remains limited to actual item packages.

## Persistence and recovery

World-wide state is stored in:

- `data/forever_skyblock_world_progress.json`
- `data/forever_skyblock_world_progress.json.bak`

The file stores schema 1, the stable world UUID, the Wandering Trader gate and the set of active world upgrades. It is validated against `forever_skyblock_world_identity.json` so world state cannot silently attach to a different save identity.

World-upgrade purchase order keeps the existing player-profile claim/receipt as the durable economic commit. If the SP debit succeeds but world-state activation is interrupted, login or opening the progression menu reconciles that paid claim into the world state. Once the world upgrade is active, active upgrade IDs are mirrored into current players' world claims so they cannot purchase the same global effect again.

This adds no new player-profile schema: dynamic schema 4/5/6 and protocol `skills-8` remain in place.

## Relation to 0.18.x

The later Freezing Chamber work builds directly on this early-game boundary. Once Bone Meal, Stone Meal and the Mixing Chamber are available, the Freezing Chamber can be crafted and can convert any allowed item into 0/32 material progress toward Ice. The normal Crafting Table is also gated behind Stone Meal. See [FREEZING_CHAMBER.md](FREEZING_CHAMBER.md).

## Validation boundary

The dedicated regression suite covers the exact 8-leaves -> 1-Bone-Meal machine cycle, world-state persistence/recovery and purchase behavior. The complete 0.18.1 stack subsequently passed GitHub Actions run #195 before PR #13 merged it into `main`.

The starter-island NBT is intentionally untouched. Wooden Shears remains an external pack choice rather than a hard mod dependency. First-Iron and Lava progression are still not finalized.
