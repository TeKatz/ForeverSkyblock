# Phase 7 — general polish and stability

Phase 7 is intentionally **not a progression-balancing phase**. The wider Forever Skyblock pack is still early enough that final Challenge rewards, Contract throughput, Shop prices and higher-tier requirements would be premature. This branch therefore creates a cleaner technical baseline before work moves to the progression of the pack itself.

Development branch: `chore/general-polish-phase7`, based on the 0.15.0 Phase 6 head. Target test version: **0.16.0**. No merge or release is implied.

## Included

### Networking and authority

`SkillNetwork.Request` now validates its action range and payload shape at construction/decoding time. Actions that require a stable progression ID must carry one; actions that do not use an ID reject unexpected data. Snapshot messages and accounting references also receive explicit bounds/null validation. The packet shape is unchanged, so the protocol remains `skills-8`.

### Current-world package delivery

The real-product confirmation path is now owned by the Phase 6 shop integration instead of falling through to the old Phase-3 test-entitlement dialog. The dialog describes the actual package, SP cost, current world and non-refundable world scope.

Opening a Supply Cache may reuse the main-inventory slot occupied by that cache. A six-stack package therefore needs five additional empty main slots when the cache itself is in the main inventory, rather than incorrectly requiring six extra slots. Offhand/other slots do not create phantom capacity.

The purchase receipt, owner/world binding, READY → DISPATCHING → DELIVERED → OPENING → OPENED state machine and crash-recovery checkpoints are otherwise unchanged.

### Regression coverage

The dedicated GameTest suite now covers the Phase 6 claim request through the network codec and verifies rejection of unknown actions, malformed product IDs, missing IDs and unexpected payload data. Existing schema/journal/contract/delivery tests remain in place.

### Repository polish

The test build is versioned as 0.16.0, CI step names describe the current regression/integration scope instead of older individual phases, and this document records what Phase 7 does and does not claim.

## Explicitly not included

- no final Challenge/SP balancing;
- no repeatable Contract quantity or payout tuning;
- no final World Shop price/package catalog;
- no Tier II-IV activation or requirement design;
- no large K-menu redesign;
- no new machines, dimensions or major progression mechanics;
- no starter-island changes.

Those decisions should follow the next stage: designing and implementing the overall modpack progression. Once more of that structure exists, the K menu and meta-progression can be revisited with real pacing information instead of guesses.

## Compatibility boundary

Phase 7 does not introduce a new profile schema. Existing schema-4, schema-5 and schema-6 behavior remains compatible with the 0.15.0 development line. `skills-8` remains the client/server protocol. As with Phase 6, older builds should not be used on a profile after schema-6 delivery state has been written without restoring a matching backup.

## Automated acceptance

The branch must pass all of the following on Java 21 before a test JAR is handed off:

1. `./gradlew build --no-daemon` including pure unit tests;
2. dedicated regression GameTests;
3. full NeoForge 21.1.259 pack-runtime GameTests with Jetpacks, tool, Ascension and technology integrations;
4. artifact generation from the exact final branch head.

Headless CI does not prove client rendering or real pack behavior. Those remain manual tests.

## Manual acceptance

1. Back up progression and the test world before using the JAR.
2. Confirm an existing Phase 5/6 profile loads with unchanged SP, PP, skills, milestones, contracts, Shop Tier and package receipts.
3. Check K → Shop → Current World and verify the real product confirmation describes the actual AE2 package rather than the old test entitlement.
4. Buy/claim only in a disposable test world. Confirm no second debit or second claim becomes possible.
5. Put the Supply Cache in the main inventory with exactly five other empty main slots and open it. The six configured package stacks should fit by reusing the consumed cache slot.
6. Repeat with the cache in offhand and only five empty main slots. Opening should remain blocked because the offhand slot cannot receive a package stack.
7. Restart after delivery and after opening; no package or SP duplication should occur.
8. Recheck Contracts, Challenges, Research, Perks, Condenser, Ascension, permanent tools and JEI for regressions.
9. Check the K menu at the GUI scales you normally use, but treat visual redesign/balance feedback as input for the later progression/K-menu phase rather than expanding Phase 7.

No client acceptance is claimed until TeKatz reports the result.
