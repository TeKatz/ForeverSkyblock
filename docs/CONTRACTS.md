# Repeatable contracts — 0.14.0 / phase 5 test branch

Development branch: `feature/repeatable-contracts-phase5`. This document describes the Phase 5 test implementation and does **not** imply a merge into `main` or a release.

## Purpose

Unique challenges remain the best-value SP source. Contracts provide a deliberately slower renewable SP path for established profiles so repeated worlds cannot permanently exhaust the finite one-time SP pool. Contracts require permanent **Tier I — Conqueror** in this first test implementation; a later balance pass may move advanced contracts to Tier II once Tier II exists.

## Active test contracts

| Contract | Trigger / turn-in | Reward | Notes |
| --- | --- | ---: | --- |
| Dragon Reborn | Defeat a respawned Dragon after the one-time Dragon Slayer kill | 1 SP | First Dragon never double-pays |
| Wither Hunter | Defeat 3 additional Withers | 1 SP | Counter resets only after payout |
| Singularity Contract | Consume 4 `ae2:singularity` | 1 SP | Explicit K-menu turn-in |
| Nitro Contract | Consume 32 `powah:crystal_nitro` | 1 SP | **Provisional test quantity** from the original 16/32/64 candidates |
| Antimatter Contract | Consume 4 `mekanism:pellet_antimatter` | 3 SP | Endgame resource sink |

The unspecified Industrial Contract is still deferred because its exact resource and production rate were never approved. Contract quantities are balance inputs, not immutable promises.

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

Contract state introduces profile **schema 5** only when a contract counter, completion or pending intent first exists. Profiles that have never used Phase 5 continue to serialize as schema 4, preserving downgrade/test convenience. Once schema-5 contract state has been written, 0.13.4 must not be used to modify that profile.

Schema 5 adds bounded:

- per-contract progress;
- lifetime completion counts;
- recent boss-evidence replay guards;
- recent resource-request replay guards;
- at most one pending resource turn-in intent.

Skill respec does not clear contract progress/completions, Shop Tier, world claims or Core receipts.

## Network and UI

Phase 5 uses progression protocol **`skills-7`**. Matching client/server versions are required. K → Challenges → Contracts now shows five live contract cards with current progress, lifetime completions, SP reward, Tier state and explicit resource turn-in buttons. Missing optional-mod items make the corresponding resource contract unavailable rather than substituting another item.

## Manual test checklist

Use a backup of `forever_skyblock/progression/` and a test world.

1. Existing 0.13.4 profile loads with identical SP, PP, challenges, Tier, research and world claims.
2. First Dragon still awards only Dragon Slayer +6 SP / Tier I. A later respawned Dragon gives exactly +1 SP per qualifying kill.
3. First Wither still awards only Withered Away. Three later Withers award exactly +1 SP and reset the repeat counter.
4. Open Contracts before Tier I: all contracts are locked even if resources are present.
5. With Tier I, hold exactly 4 AE2 Singularities, turn them in, verify all four disappear and exactly +1 SP/completion is recorded.
6. Repeat the Singularity turn-in with another batch; verify the lifetime count increments and no packet double-click can duplicate payout.
7. Test 32 Nitro Crystals and 4 Antimatter Pellets; verify exact consumption/rewards.
8. Restart after normal turn-ins and verify state persists across worlds.
9. Test EN/DE client selection, 1920x1080 / GUI scale 3 first, then other practical GUI scales.
10. Recheck Challenges, Shop, Research, Perks, Condenser/Ascension GUIs and Phase 4 technology objectives.

Crash-cut recovery is covered structurally by the durable intent design and profile-journal tests, but a real forced-process-stop test around a resource turn-in remains valuable before main integration.
