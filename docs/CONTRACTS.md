# Repeatable contracts — integrated 0.14.0 behavior

Repeatable contracts were introduced during Phase 5 / 0.14.0 and are integrated into the current 0.18.1 `main` baseline through PR #13. They remain intentionally conservative renewable-SP sinks rather than the primary progression source. No release publication is implied by that merge.

## Purpose

Unique challenges remain the best-value SP source. Contracts provide a slower renewable SP path for established profiles so repeated worlds cannot permanently exhaust the finite one-time SP pool. The current implementation requires permanent **Tier I — Conqueror**. A later balance pass may move advanced contracts to Tier II once Tier II exists.

## Active contracts

| Contract | Trigger / turn-in | Reward | Notes |
| --- | --- | ---: | --- |
| Dragon Reborn | Defeat a respawned Dragon after the one-time Dragon Slayer kill | 1 SP | First Dragon never double-pays |
| Wither Hunter | Defeat 3 additional Withers | 1 SP | Counter resets only after payout |
| Singularity Contract | Consume 4 `ae2:singularity` | 1 SP | Explicit K-menu turn-in |
| Nitro Contract | Consume 32 `powah:crystal_nitro` | 1 SP | Current provisional quantity; still subject to later pack balance |
| Antimatter Contract | Consume 4 `mekanism:pellet_antimatter` | 3 SP | Endgame resource sink |

The unspecified Industrial Contract remains deferred because its exact resource and production rate have not been approved. Current quantities/rewards are integrated behavior but remain balance inputs rather than final pack-economy promises.

## Boss rules

Boss contract credit is reduced atomically with the existing unique challenge update. For Dragon Slayer and Withered Away, the first qualifying boss kill only pays the existing one-time challenge. A repeatable counter can start only on a later qualifying kill and only after the profile has reached the required Shop Tier. Recent entity UUIDs are retained in a bounded replay guard so duplicate event delivery cannot immediately pay/count twice.

## Resource turn-in transaction safety

Resource turn-ins do not award SP for merely owning an item. The client submits an explicit contract ID plus a unique request UUID; the server owns the item ID, quantity, tier and reward.

The transaction deliberately bridges profile storage and Minecraft player-data with two durable checkpoints:

1. Verify Tier, contract definition and exact resource count on the server.
2. Force-save player data so the pre-consumption inventory count is durable.
3. Commit one pending contract intent to the existing crash-safe profile journal. The intent stores the exact before-count, required item/count and reward.
4. Remove the resources from the player's real inventory.
5. Force-save player data again so the post-consumption count is durable.
6. Finalize the profile transaction: increment lifetime completions, record the recent request UUID, award SP and remove the pending intent.

If the process stops after step 3, login/open recovery compares the live item count against the exact persisted before/after counts. A pre-consumption count consumes once and continues; a post-consumption count awards once and continues. Any other count is ambiguous and fails closed instead of guessing, duplicating SP or consuming another batch.

Only one resource contract intent may be pending per profile. Profile write failures remain recoverable through the existing write-ahead journal. The request replay guard prevents a lost client response from replaying the same completed request UUID.

## Persistence and migration

Contract state introduces profile **schema 5** only when a contract counter, completion or pending intent first exists. Profiles that have never used contract state can remain schema 4. Once schema-5 contract state has been written, older builds that predate schema 5 must not be used to modify that profile.

Schema 5 adds bounded:

- per-contract progress;
- lifetime completion counts;
- recent boss-evidence replay guards;
- recent resource-request replay guards;
- at most one pending resource turn-in intent.

Skill respec does not clear contract progress/completions, Shop Tier, world claims or Core receipts.

The current integrated network protocol is **`skills-8`** because later world-shop delivery work extended the protocol after contracts were introduced. Matching client/server versions remain required.

## UI

K -> Challenges -> Contracts shows the five live contract cards with current progress, lifetime completions, SP reward, Tier state and explicit resource turn-in buttons. Missing optional-mod items make the corresponding resource contract unavailable rather than substituting another item.

## Validation boundary

The original Phase-5 source and later stacked regressions are covered by pure transaction tests, dedicated GameTests and the current full-pack integration suite. The final integrated 0.18.1 feature head `97715f06391339141032c5f0c859b5925f99539d` passed GitHub Actions run #195 before PR #13 merged it into `main`.

The crash-safe intent design covers tested persistence/recovery boundaries, but a real forced-process-stop at every possible resource-turn-in checkpoint is not claimed as manually validated. Current quantities/rewards also remain candidates for later pack-balance tuning.
