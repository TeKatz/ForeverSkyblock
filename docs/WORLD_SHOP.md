# Current-world shop and world-state progression — integrated baseline

The stable world identity, claim and purchase-receipt foundation was introduced in 0.12.0. Real package delivery followed in 0.15.0 and world-wide QoL upgrades in 0.17.0. The complete stack is integrated into the current 0.18.1 `main` baseline through PR #13.

Minecraft 1.21.1, Java 21; pack runtime NeoForge 21.1.259.

## Active real package

| Field | Value |
| --- | --- |
| ID | `forever_skyblock:shop/advanced_ae2_starter` |
| Name | Advanced AE2 Starter Package |
| Scope | Current world |
| Requirement | Permanent Tier I — Conqueror |
| Price | **12 SP / 0 PP** |
| Repeatability | Once per player and stable world identity |
| Refund | None |
| Delivery | Unique owner/world-bound Forever Supply Cache |

Package contents are server-owned:

| Item | Count |
| --- | ---: |
| `ae2:drive` | 1 |
| `ae2:pattern_provider` | 2 |
| `ae2:molecular_assembler` | 2 |
| `ae2:blank_pattern` | 16 |
| `ae2:fluix_smart_cable` | 16 |
| `ae2:dense_energy_cell` | 1 |

The package is intentionally automation-oriented. It does **not** contain an AE2 item storage cell, 64k Crafting Storage or Wireless Crafting Terminal, so the package cannot satisfy those existing personal challenges merely by being bought. Required item IDs are resolved from the live server registry; a missing/renamed entry makes the product unavailable instead of weakening or substituting its contents.

The old `forever_skyblock:phase3_test_entitlement` remains in the server catalog for regression/operator development coverage, but the normal Current World page hides it in favor of real products and upgrades.

## Active world-wide upgrades

World Upgrades are non-delivery Current World products. They are paid through the same stable world-identity transaction system but activate persistent world state instead of creating a Supply Cache.

| Upgrade | Price | Tier | Integrated effect |
| --- | ---: | --- | --- |
| No Phantoms | 1 SP | Tier 0 | disables insomnia and blocks new Phantom entity joins |
| No More Rain | 2 SP | Tier 0 | clears Overworld rain/thunder and disables weather cycling |
| Permanent Day | 3 SP | Tier 0 | fixes Overworld daytime at 1000 and disables daylight cycling |

Each upgrade is charged once for the stable world identity. Its effect is world-wide; other players mirror the active claim for UI/idempotency purposes without a second SP debit.

World-wide state is persisted separately in `data/forever_skyblock_world_progress.json` with a `.bak` copy. The file stores schema 1, the stable world UUID, the Wandering Trader unlock and active world upgrades. It fails closed if its stored world identity does not match the save's stable identity.

If an SP debit/profile claim commits but world-state activation is interrupted, login or opening progression reconciles the paid claim into the world state. Once global state is active, current players mirror the claim so the same upgrade cannot be repurchased.

## Stable world identity

Each save receives a random UUID stored under its world root in `data/forever_skyblock_world_identity.json`, with an independent `.bak` copy. Restart/rename retains it; a copied save intentionally retains it and therefore the same claims; a genuinely new save gets a different UUID. One missing/corrupt copy can be repaired from the other, while conflicting valid UUIDs fail closed.

Normal UI shows the friendly world name rather than exposing the identity. The identity itself remains server-authoritative and is used when validating purchases, delivery entitlements, Supply Caches and world-upgrade state.

## Package purchase transaction

The client sends only a product ID. The server owns scope, tier, price, PP cost, contents, player and current world.

A successful real package purchase atomically persists:

1. 12-SP debit and accounting increment;
2. current-world product claim;
3. immutable purchase receipt;
4. a delivery receipt in **READY** state.

The profile write-ahead journal remains the economic commit point. A failed profile write does not partially debit the account, and replayed purchase requests cannot create a second world claim.

## Recipient-bound delivery protocol

The committed package entitlement moves through a bounded state machine:

`READY -> DISPATCHING -> DELIVERED -> OPENING -> OPENED`

### Claiming the package

- **READY:** Shop shows **Claim Package**.
- Server commits **DISPATCHING** before touching inventory.
- It inserts one `forever_skyblock:world_supply_cache` into an empty main-inventory slot. The cache is non-stackable and contains the server-created receipt UUID, owner UUID, world UUID and product ID.
- Player data is forced to disk before the profile moves to **DELIVERED**.
- On restart while DISPATCHING, recovery searches the player's inventory for that exact receipt. If present it finalizes DELIVERED; if absent and an empty slot exists it issues that same receipt cache and then finalizes. No second purchase/debit occurs.

### Opening the package

- Only the matching owner in the matching world can use the cache.
- The server verifies the delivery receipt and catalog again; item data never supplies package contents.
- Enough empty main-inventory capacity is required before the server commits **OPENING**. The consumed cache's own main-inventory slot counts as available capacity; offhand does not.
- The cache is consumed and the exact catalog stacks are inserted, then player data is forced to disk and the profile moves to **OPENED**.
- On restart while OPENING: if the cache is still present, the item mutation was not durably completed and recovery repeats the bounded opening path; if the cache is absent after the checkpoint, recovery finalizes OPENED instead of issuing contents again.
- Copies/replays of the same cache cannot open after its receipt reaches OPENED.

This protocol is designed for ordinary process/server crashes around the explicit checkpoints. It does not claim protection against arbitrary external rollback of only one persistence authority, manual NBT editing, inventory restoration from mismatched backups or two processes writing the same progression/player data concurrently.

## Schema and network boundary

- untouched/base profiles can remain schema **4**;
- a profile with contract state but no delivery can remain schema **5**;
- the first real delivery entitlement raises it to schema **6**;
- schema 6 adds delivery receipts/stages and records PP cost in purchase receipts;
- current client/server protocol is **`skills-8`**.

Older builds cannot be assumed to read profiles after newer schema-5/6 state has been written. Back up both `forever_skyblock/progression/` and the world/player data before downgrade experiments.

## Validation boundary

Unit tests cover one-time debit/claim/delivery creation, delivery-stage transitions, world-state identity/recovery and schema persistence. Dedicated GameTests cover Supply Cache behavior and network boundaries. The NeoForge 21.1.259 integration suite checks the AE2 registry IDs used by the package along with the existing optional integrations.

The final integrated 0.18.1 feature head `97715f06391339141032c5f0c859b5925f99539d` passed GitHub Actions run #195 before PR #13 merged it into `main`. Headless CI does not prove rendered Shop layout or a real OS-level kill at every exact inventory checkpoint.

Tier II-IV, more valuable current-world packages and final pricing/balance remain intentionally deferred.
