# Phase 7 — general polish and stability (historical integrated phase)

Phase 7 was intentionally **not a progression-balancing phase**. It created a cleaner technical baseline before work moved into early-game pacing and the Freezing Chamber. The 0.16.0 work is now integrated into the current 0.18.1 `main` baseline through PR #13.

Historical development branch: `chore/general-polish-phase7`. Historical target version: **0.16.0**. No release was published from that phase.

## Included

### Networking and authority

`SkillNetwork.Request` validates its action range and payload shape at construction/decoding time. Actions that require a stable progression ID must carry one; actions that do not use an ID reject unexpected data. Snapshot messages and accounting references also receive explicit bounds/null validation. The packet shape remained unchanged, so the protocol stayed `skills-8`.

### Current-world package delivery

The real-product confirmation path is owned by the Phase 6 shop integration instead of falling through to the old Phase-3 test-entitlement dialog. The dialog describes the actual package, SP cost, current world and non-refundable world scope. A purchased package in READY state remains actionable from the same card, so the visible **Claim Package** action is not accidentally disabled by the new-purchase eligibility check.

Opening a Supply Cache may reuse the main-inventory slot occupied by that cache. A six-stack package therefore needs five additional empty main slots when the cache itself is in the main inventory, rather than incorrectly requiring six extra slots. Offhand/other slots do not create phantom capacity.

The purchase receipt, owner/world binding, READY -> DISPATCHING -> DELIVERED -> OPENING -> OPENED state machine and crash-recovery checkpoints were otherwise unchanged by Phase 7.

### Regression coverage

The dedicated GameTest suite added coverage for the Phase 6 claim request through the network codec and rejection of unknown actions, malformed product IDs, missing IDs and unexpected payload data. It also locks in the Supply Cache capacity boundary: the consumed cache slot counts only when the cache actually occupies the main inventory. Existing schema/journal/contract/delivery tests remain in place.

### Repository polish

The test build was versioned as 0.16.0, CI step names were updated around the current regression/integration scope, and the phase explicitly documented what it did and did not claim.

## Explicitly not included in Phase 7

- no final Challenge/SP balancing;
- no repeatable Contract quantity or payout tuning;
- no final World Shop price/package catalog;
- no Tier II-IV activation or requirement design;
- no large K-menu redesign;
- no new machines, dimensions or major progression mechanics;
- no starter-island changes.

Some later phases added early-game/world QoL and the Freezing Chamber, but the remaining balance/Tier/late-game items above are still not final unless [CURRENT_STATE.md](CURRENT_STATE.md) says otherwise.

## Compatibility boundary

Phase 7 introduced no new profile schema. Existing schema-4, schema-5 and schema-6 behavior remained compatible with the 0.15.0 development line and `skills-8` remained the client/server protocol. That remains true in the current 0.18.1 baseline.

## Validation history

The 0.16.0 branch passed the Java 21 build/unit tests, dedicated regression GameTests and full NeoForge 21.1.259 pack-runtime integrations before later work was stacked on top. The complete 0.18.1 stack subsequently passed GitHub Actions run #195 before PR #13 merged it into `main`.

This file is retained as a historical phase record. Current behavior belongs in [CURRENT_STATE.md](CURRENT_STATE.md), [WORLD_SHOP.md](WORLD_SHOP.md), [EARLY_GAME_QOL.md](EARLY_GAME_QOL.md) and [FREEZING_CHAMBER.md](FREEZING_CHAMBER.md).
