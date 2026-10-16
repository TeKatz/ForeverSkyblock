# Current-world shop and real package delivery — Phase 6 / 0.15.0 development

The stable world identity, claim and purchase-receipt foundation was introduced in 0.12.0 / Phase 3 and is integrated in `main` through the 0.13.4 baseline. This document also describes the **0.15.0 Phase 6 development branch** `feature/real-world-shop-phase6`, which is based on the 0.14.0 Phase 5 contract head. Phase 6 is not yet merged or released.

Minecraft 1.21.1, Java 21; pack runtime NeoForge 21.1.259.

## Active Phase 6 product

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

The old `forever_skyblock:phase3_test_entitlement` remains in the server catalog for regression coverage and operator/development use, but the Phase 6 Current World page hides it in favor of the real package.

## Stable world identity

Each save receives a random UUID stored under its world root in `data/forever_skyblock_world_identity.json`, with an independent `.bak` copy. Restart/rename retains it; a copied save intentionally retains it and therefore the same claims; a genuinely new save gets a different UUID. One missing/corrupt copy can be repaired from the other, while conflicting valid UUIDs fail closed.

Normal UI shows the friendly world name rather than exposing the identity. The identity itself remains server-authoritative and is used when validating the purchase, delivery entitlement and supply cache.

## Purchase transaction

The client sends only a product ID. The server owns scope, tier, price, PP cost, contents, player and current world.

A successful real purchase atomically persists:

1. 12-SP debit and accounting increment;
2. current-world product claim;
3. immutable purchase receipt;
4. a delivery receipt in **READY** state.

The profile write-ahead journal remains the economic commit point. A failed profile write does not partially debit the account, and replayed purchase requests cannot create a second world claim.

## Recipient-bound delivery protocol

Phase 6 avoids directly granting a set of anonymous valuable stacks during purchase. The committed entitlement moves through a bounded state machine:

`READY → DISPATCHING → DELIVERED → OPENING → OPENED`

### Claiming the package

- **READY:** Shop shows **Claim Package**.
- Server commits **DISPATCHING** before touching inventory.
- It inserts one `forever_skyblock:world_supply_cache` into an empty main-inventory slot. The cache is non-stackable and contains the server-created receipt UUID, owner UUID, world UUID and product ID.
- Player data is forced to disk before the profile moves to **DELIVERED**.
- On restart while DISPATCHING, recovery searches the player's inventory for that exact receipt. If present it finalizes DELIVERED; if absent and an empty slot exists it issues that same receipt cache and then finalizes. No second purchase/debit occurs.

### Opening the package

- Only the matching owner in the matching world can use the cache.
- The server verifies the delivery receipt and catalog again; item data never supplies package contents.
- Enough empty main-inventory slots are required before the server commits **OPENING**.
- The cache is consumed and the exact catalog stacks are inserted, then player data is forced to disk and the profile moves to **OPENED**.
- On restart while OPENING: if the cache is still present, the item mutation was not durably completed and recovery repeats the bounded opening path; if the cache is absent after the checkpoint, recovery finalizes OPENED instead of issuing contents again.
- Copies/replays of the same cache cannot open after its receipt reaches OPENED.

This protocol is designed for ordinary process/server crashes around the explicit checkpoints. It does not claim protection against arbitrary external rollback of only one persistence authority, manual NBT editing, inventory restoration from mismatched backups or two processes writing the same progression/player data concurrently.

## Schema and network boundary

- Existing untouched profiles continue to serialize as schema **4**.
- A profile with Phase 5 contract state but no delivery stays schema **5**.
- The first real delivery entitlement upgrades it to schema **6**.
- Schema 6 adds delivery receipts/stages and records PP cost in purchase receipts.
- Network protocol is **`skills-8`** and sends bounded server-owned product availability/delivery state to the matching client.

Older 0.14.0 and 0.13.4 builds cannot be assumed to read profiles after a schema-6 delivery purchase. Back up both `forever_skyblock/progression/` and the test world/player data before testing.

## Automated validation boundary

Pure tests cover one-time debit/claim/delivery creation, strict delivery-stage transitions, respec preservation and schema-5 → schema-6 persistence. Dedicated GameTests cover the unique supply-cache marker and the expanded network payload. The NeoForge 21.1.259 integration suite checks all six AE2 registry IDs used by the package along with the existing Phase 4 integrations.

Headless tests cannot prove rendered Shop layout or a real OS-level kill at every exact inventory checkpoint. Those remain manual acceptance items.

## Manual acceptance

1. Back up the progression directory **and** the test world/player data. Use matching 0.15.0 client/server JARs.
2. Confirm the existing Phase 5 profile, contracts, SP/PP, challenges, research and Tier I are unchanged on load.
3. Open K → Shop → Current World. Verify the real AE2 package, 12-SP price, Tier-I gate, world name and no visible development entitlement.
4. With fewer than 12 SP, verify purchase is disabled. With 12+ SP, confirm once and verify exactly 12 SP is deducted.
5. Reopen the page. Verify the product is not purchasable again and shows **Claim Package**.
6. Claim it with one empty inventory slot. Verify exactly one Forever Supply Cache appears and restart before opening it; the receipt remains delivered without duplication.
7. Try the cache with another player and in another world; it must not deliver contents.
8. Return to the purchasing world/owner, ensure at least six empty ordinary inventory slots, then open the cache. Verify the exact six configured stacks and no storage-cell/challenge reward appears merely from delivery.
9. Reopen/replay a copied cache with the same receipt; it must not pay again. Restart and verify the package remains OPENED/claimed.
10. Check EN/DE Shop presentation and GUI scales 2/3/4. Recheck Contracts, Challenges, Research, Perks, Condenser/Ascension GUIs and JEI.

No manual client acceptance is claimed until TeKatz reports it.
