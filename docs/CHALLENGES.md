# Personal challenge progression — 0.11.0 / phase 2

Development branch: `feature/persistent-meta-progression`, based on 0.10.0 and retaining the unmerged 0.9.1 machine fixes. Minecraft 1.21.1, Java 21; pack runtime NeoForge 21.1.259. No main merge or release is implied.

## Active objectives

All rewards are once per player profile, across worlds within the existing authority. Singleplayer and dedicated-server storage remain separate. No inventory-possession or retrospective inventory scan grants a reward.

| Challenge | Personal action | SP |
| --- | --- | ---: |
| First Foundations | Craft Cobblestone from four Stone Fragments; existing ID/claim retained | 1 |
| Stoneworker | Craft Stone Meal using the Mortar and four fragments | 1 |
| Permanent Tools I | Craft each of the four Forever Stone tools | 2 |
| The Iron Age | Craft each of the four Forever Iron tools | 3 |
| Diamond Standard | Craft the expert Diamond Pickaxe | 3 |
| Forever Forged | Smith the Netherite Pickaxe with Reinforced Netherite Ingot | 5 |
| Into the Nether | Enter the Nether | 2 |
| Withered Away | Minecraft credits the player with a Wither kill | 5 |
| The Final Dimension | Enter the End | 3 |
| Dragon Slayer | Minecraft credits the player with an Ender Dragon kill | 6 |
| Veteran | 1,000 distinct personally credited Monster-category kills | 3 |

The live catalog contains **11 challenges / 34 SP**, including the existing First Foundations point. The full proposed 38-challenge / 135-SP catalog is not yet active. Distinct Stone/Iron tool sets and Veteran evidence survive restarts, new worlds and ordinary skill resets. Veteran stops accumulating after completion; its bounded entity UUID evidence prevents the same victim from advancing the counter twice. Earlier kills/crafts are not inferred from inventory, statistics or advancements. The first new qualifying action starts tracking.

Dirt ownership, first iron and water/lava/Obsidian routes remain unresolved. No challenge or recipe is invented for them. Optional technology adapters, repeatable contracts, product contents/prices and world purchases remain later phases.

## Authoritative action adapters

`ChallengeRules` is a pure immutable catalog/reducer; `ChallengeEvents` normalizes trusted server action evidence; `ProgressionService.evidence` commits a batch of progress, completion, reward, accounting and tier together through `ProfileStore`. Chat and snapshots follow successful persistence only. There is no client award request and no per-tick inventory scan.

- **Crafting and smithing:** hook only vanilla `CriteriaTriggers.RECIPE_CRAFTED`, which successful menu `onTake` paths call via `RecipeCraftingHolder`. Check the exact Forever recipe ID, registered result and consumed ingredient slots. Ignore the separate `CRAFTER_RECIPE_CRAFTED` trigger. Standard `PlayerEvent.ItemCraftedEvent` preserves the existing fragment check; duplicate notification is idempotent. Recipe previews and item pickup do not trigger this path. AE2 19.2.18's manual `AppEngCraftingSlot.onTake` calls the same recipe trigger; its automatic crafting jobs are not this adapter.
- **Combat:** hook only vanilla `CriteriaTriggers.PLAYER_KILLED_ENTITY`. Use Minecraft's attributed player, including attributed projectiles and vanilla recent-player kill credit; never grant to all nearby players or all damage participants. This also follows the Dragon's actual lethal-damage/advancement path despite its extended death animation. Unattributed environmental/machine kills and FakePlayers do not pay. Veteran uses the entity type's `MobCategory.MONSTER`, not current aggression: neutral members of that category count; passive-category animals do not.
- **Dimensions:** use server `PlayerChangedDimensionEvent` to Nether/End. Login while already in a dimension is not retroactive entry evidence.
- **Automation and actors:** reject FakePlayer and null actors centrally and at the event boundary. Optional adapters never load absent mod classes. Existing machine recipes and Core redemption are unchanged.

## Tiers and menu

Tier -1 means no tier yet. First Foundations unlocks **Tier 0 — Foundations**; Dragon Slayer unlocks **Tier I — Conqueror**, even if First Foundations has not been completed. Tier increases use `max(previous, qualified)` and are independent of balances, test/admin points and respecs. Higher stored tiers are retained, but new Tier II–IV rules remain disabled.

K → Challenges now offers All, Forever, Combat & Dimensions and Contracts. Existing title/description search and open/completed filters cover the active catalog. Cards and details show current progress; unsupported future objective revisions show an explicit unavailable status. Overview and Shop → Tier Progress show authoritative permanent tiers and their unlock conditions. Profile shows earned, spent, refunded and admin-granted currency since tracking began. Rewards retain the green clickable chat link; a new tier adds an explicit unlock message. Counter-only snapshots do not rebuild JEI research visibility.

## Schema 3 and migration

All six schema-2 fields remain: SP, milestone IDs, skill ranks, PP, paid-perk investments, redeemed Core UUIDs. New `meta` fields are:

- `shop_tier`: -1 through 4, monotonic through normal operations.
- `challenge_progress`: namespaced ID → `{revision, evidence}`; revision 1 uses approved tool IDs, a unique-completion token or victim UUIDs. Limits: 4,096 objective entries, 1,000 tokens per objective, 8,192 tokens overall and 1 MiB encoded profile size. Unknown IDs/revisions survive round trips; unknown objective semantics never award points.
- `accounting`: nonnegative 64-bit SP/PP earned/spent/refunded totals, SP admin grants and `legacyHistoryUnknown`. Overflow fails the transaction. Legacy history is marked unknown, not guessed from balance or owned skills. New transactions are tracked separately. Known v1 Creative migration refunds are recorded as refunds, not earnings.

World claims, contract records and product receipts are deliberately not allocated before their phase-3+ semantics exist. Schema 3 is the bounded phase-2 implementation of the expanded architecture, not a claim that the entire proposed economy is complete.

Valid v2 profiles retain balances, research, purchases, unknown IDs and Core receipts. Existing First Foundations derives Tier 0 with **no second point**. V1 profiles still apply the established removal/refund of legacy SP-only Creative research exactly once after the next successful write. Loading migrates in memory; the first successful changed transaction writes schema 3. No routine respec clears challenge claims, partial objectives, tiers or Core receipts. Older mod versions cannot load schema-3 profiles: back up the progression directory before testing a downgrade or restoring an older mod.

**Recovery change:** a missing/corrupt primary with an existing backup now makes progression unavailable instead of silently loading a previous-state backup. That backup may predate rewards, debits or claims and cannot safely establish the current balance. Both files are preserved for recovery. The phase-3 journal/recovery protocol is still required before world financial transactions go live. This does not provide cross-process locking or arbitrary backup rollback protection; do not run simultaneous instances against one profile directory.

Snapshots use protocol **`skills-5`**, carrying tier, bounded known objective counts and accounting; evidence UUIDs are not sent to clients. Install 0.11.0 on both client and server.

## Verification and manual acceptance

Automated coverage includes legacy migration, one-time rewards, distinct tool evidence, 630+220+150 kills across reloads, duplicate events, concurrent claims, failure-before-commit, monotonic tiers, unknown IDs/revisions, accounting and corrupt-primary refusal. Dedicated GameTests exercise real Vanilla crafting/smithing menus, actual hostile/boss lethal damage, fake/automated exclusions, dimension dispatch, actual optional AE2 manual slot dispatch and snapshot round trips. Exact completed CI evidence is recorded in VALIDATION.md before artifact delivery.

Manual acceptance on a copy of the test instance:

1. Load an existing profile. Confirm SP/PP, all research and Core receipts; existing Foundations should show completed and Tier 0 without a new point.
2. Craft Stone Meal, then three Stone tools, restart or change worlds and craft the fourth. Verify progress and exactly 2 SP for the set; repeat crafts and respec must not re-award.
3. Test normal and shift crafting through the pack's ME terminal, the Diamond recipe and Netherite smithing. Check completion feedback and unchanged recipe behavior.
4. Kill monsters, reopen K and inspect Veteran. Enter Nether/End. Kill Wither/Dragon with melee/projectiles in multiplayer; only the vanilla-credited player should receive the personal reward and Dragon Tier I. Confirm Tier I after a new world and skill reset.
5. Inspect EN/DE categories, search, counters, details, tier cards and Profile accounting at GUI scales 2/3/4. Verify the retained machine GUI fixes, JEI behavior, existing purchases/research and refunds. Server tests do not certify rendered client layout.
