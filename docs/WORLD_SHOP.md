# Current-world shop safety — 0.12.0 / phase 3

Development branch: `feature/persistent-meta-progression`. Minecraft 1.21.1, Java 21; pack runtime NeoForge 21.1.259. This phase proves the identity and transaction boundary. It does not approve a valuable product, item delivery or a release.

## Active test product

| Field | Value |
| --- | --- |
| ID | `forever_skyblock:phase3_test_entitlement` |
| Visibility | Singleplayer owner or server operator only |
| Scope | Current world |
| Requirement | Permanent Tier I — Conqueror |
| Price | 5 SP |
| Repeatability | Once per player and stable world identity |
| Delivery | None; no item, block, command or gameplay benefit |
| Refund | None |

The card appears in K → Shop → Current World. Its server-authoritative state is locked, available or claimed. Buying requires a confirmation dialog. The client sends only the stable product ID; the server owns price, tier, actor, world identity and duplicate validation.

## Stable world identity

Each save receives a random UUID stored under its world root in `data/forever_skyblock_world_identity.json`, with an independent `.bak` copy. The UUID is shared by all dimensions and is unrelated to the display name, folder, seed or player. Normal UI shows only a friendly world name and whether world purchases are available.

- Restarting or renaming the save folder retains the UUID.
- Copying a save intentionally copies the UUID. The copy therefore sees the same existing claim and cannot rebuy it.
- A genuinely new save receives a new UUID and has an independent current-world claim.
- If one identity copy is missing or corrupt, the valid copy repairs it.
- Two valid but conflicting UUIDs, or no recoverable registered identity, disable world purchases. The mod never guesses between conflicting histories.

Deleting both identity files is unsupported tampering and may cause a fresh identity to be created when no surviving registration can prove the earlier one. Back up the complete world save, not selected files.

## Profile schema 4

Schema 4 preserves every earlier balance, claim, skill, research stage, paid-perk investment, Core receipt, challenge evidence, tier and accounting field. It adds:

- `world_claims`: bounded world UUID → bounded product-ID set.
- `purchase_receipts`: bounded receipt UUID → immutable world/product/currency record.

Limits are 1,024 worlds, 128 products per world and 4,096 total claims/receipts. Every receipt must correspond to its claim. Unknown valid IDs remain round-trippable; malformed, oversized or inconsistent state fails validation.

Valid schema-1/2/3 profiles migrate in memory and are written as schema 4 only by the next successful change. Older mod versions cannot read schema 4. Back up `forever_skyblock/progression/` before testing or downgrading.

## Atomic purchase and crash recovery

The purchase transition validates the catalog product, operator/owner test access, stable world identity, permanent tier, SP balance and absence of a claim. One immutable next profile contains the exact 5-SP debit, accounting increment, world claim and receipt.

`ProfileStore` writes a per-player transaction journal under `forever_skyblock/progression/<authority>/transactions/` after preserving the prior validated state and before replacing the primary profile. The journal is the commit point:

- Crash before the journal: the previous profile remains authoritative; nothing was bought.
- Crash after the journal: recovery finishes the exact committed next profile.
- Repeated, concurrent or replayed requests find the existing claim and cannot debit again.
- A failed write before commitment leaves memory and disk unchanged.

A valid committed journal can repair a stale or corrupt primary. Without one, an arbitrary old backup is not accepted as current economic truth because it could resurrect spent SP or remove a claim. Cross-process locking and arbitrary manual rollback remain unsupported; never run two instances against the same progression directory.

## Deferred boundary

No actual package contents, prices, inventory grant, ME insertion, dropped item, block placement or recipient mailbox is implemented. Those require a separately approved and tested delivery protocol that reconciles the profile receipt with player/world inventory at every crash cut. Contracts and Tier II–IV rules also remain inactive.

## Automated verification

Source `50683be3bf4fd3d1e2261aa15e953c82ae1be3f4` passed CI run `38851560072`: the Java 21 build, pure tests and all 48 required GameTests in both the baseline and NeoForge 21.1.259 pack-integration runs. Coverage includes identity rename/restart/copy/new-save behavior, copy repair/conflict refusal, schema migration, exact accounting and receipts, 64 concurrent requests, every journal crash stage, stale/corrupt primary recovery, packet bounds and an end-to-end service purchase.

## Manual acceptance

1. Back up the progression directory and test world. Install 0.12.0 on both client and server.
2. Load an existing profile and verify SP, PP, skills, research, challenge progress, Tier I and Core receipts before changing anything.
3. If needed, use the owner/operator `TEST: +10 SP` control. Open Shop → Current World and check the friendly world name, Tier-I requirement and 5-SP price.
4. Confirm the purchase. Verify exactly 5 SP is removed, the card becomes claimed and Profile accounting/claim count updates. Confirm no item or gameplay effect is delivered.
5. Reopen the menu and restart the game/server. The same world remains claimed and no second debit is possible.
6. Copy or rename the world save. It must remain claimed. Create a genuinely new world in the same authority; its test entitlement must be independently available.
7. Check EN/DE text and GUI scales 2/3/4. Recheck Challenges, Research, Perks, the retained condenser/Ascension GUIs and existing JEI behavior.

No manual client acceptance is claimed until TeKatz reports the result.
