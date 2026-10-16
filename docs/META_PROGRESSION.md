# Persistent challenges, shop and profile progression

Architecture proposal and bounded implementation plan, October 16, 2026.
Owner: TeKatz. Source: the complete 38-section Shop/Challenges handoff supplied by TeKatz.

## Baseline and immediate boundary

Inspected remote main `d80e535` (0.8.0) and visual-overhaul `e2781ef` (0.9.1), AGENTS.md, the profile/store/service, events, milestone and skill catalogs, Jetpack gates, K screen, network snapshot, CoreRedemptions, Chamber completion, tool/progression/Ascension documentation and the provisional pack inventory.

Work lives on `feature/persistent-meta-progression`, branched from **0.9.1** to retain the machine GUI corrections. Neither this branch nor the visual overhaul is merged into main. Minecraft **1.21.1**, Java 21, pack NeoForge **21.1.259** remain authoritative; older 1.21.4 references are not this repository's target.

Phase 1 is a client navigation foundation, not the new economy. It reorganizes existing working functionality, adds honest explanatory shells for later features, and defines the architecture here. It does not activate new challenges, tiers, contracts, world purchases, or valuable starter contents. It deliberately leaves profile schema 2 and the existing network/purchase handlers unchanged. This makes the first phase independently reviewable and avoids writing unused persistence fields before their semantics and recovery tests exist.

## Findings in the existing implementation

- `ProgressionProfile` is immutable: SP, completed milestone IDs, skill ranks, PP, actual PP investments and redeemed Core UUIDs.
- `ProfileStore` writes schema 2 JSON outside saves, partitioned into local and dedicated-server authorities. Cache replacement happens after writing a validated backup and the new primary file. It loads schema 1 with the established Creative refund/re-lock behavior. Unknown milestone/skill IDs are retained; arbitrary unknown JSON fields are not.
- `MilestoneCatalog` has only `forever_skyblock:first_foundations`. Personal fragment crafting is filtered on the server and rejects FakePlayer. There is no existing 135-SP challenge catalog.
- `SkillCatalog` has Extended Reach and four ordered Jetpack stages costing 5/10/20/40 SP; Creative also costs 1 PP. Current reset refunds known purchases and preserves milestone/Core receipts.
- `SkillNetwork` uses protocol `skills-4`, a bounded snapshot and request actions 0–3. It sends only known milestone/skill IDs. It has no world UUID, counters, tier or delivery state.
- `CoreRedemptions` publishes an exclusive authority-wide Core identity claim, then credits a profile receipt. Chamber replay consumes the same identified Core without paying twice. This relies on unique Core identities; it is not a general transaction for anonymous item stacks.
- The current GUI has four flat tabs. Its compact Jetpack cards rely on large hover descriptions. The first phase replaces that presentation with hierarchy and wrapped inline information while retaining server operations.
- First iron, water/lava and pre-Diamond obsidian production are still unresolved. The island already provides working AE2. Neither fact may be hidden by new milestone shortcuts.

## Economy and stable identifiers

Use a hybrid catalog: validated immutable metadata definitions plus typed Java trigger adapters. Start with a small code catalog; adopt server-loaded JSON metadata only when catalog iteration warrants it. Clients never define rewards, prices, scopes, eligibility or triggers. Missing optional integrations produce an explicit unavailable reason, not an item-possession fallback.

Definitions contain stable namespaced ID, revision, category, translation keys, icon ID, prerequisites, reward, objective type/target, availability and handler key. Resource IDs resolve only after registries load. Unknown persisted IDs survive removal of a mod/catalog entry; retired definitions remain in a tombstone registry for claims and refunds. A changed objective with materially different meaning gets a new versioned ID; changing a title never changes identity.

Keep `forever_skyblock:first_foundations`, `forever_skyblock:extended_reach` and all `jetpacks_*` IDs exactly. Do not pay a second First Foundations reward during catalog migration. Reserve the following new challenge suffixes under `forever_skyblock:`; these are design entries, not active rewards:

| Category | ID suffix and suggested SP | Total |
| --- | --- | ---: |
| Forever | first_foundations 1; stoneworker 1; from_nothing_soil 2; permanent_tools_i 2; first_metal 2; iron_age 3; obsidian_gate 2; diamond_standard 3; forever_forged 5 | 21 |
| AE2 | beyond_starter_cell 1; autocrafting_online 3; crafting_brain 3; wireless_operator 2; quantum_link 5; storage_architect 4 | 18 |
| Mekanism | mekanized 1; factory_floor 3; digital_extraction 5; ultimate_industry 4; fission_online 7; fusion_online 8; matter_transcended 10 | 38 |
| Powah | energized 1; reactor_engineer 3; nitro_age 6 | 10 |
| Industrial Foregoing | industrialized 1; automated_agriculture 3; laser_extraction 5; industrial_mastery 6 | 15 |
| Mystical Agriculture | essence_farmer 1; supremium 4; master_grower 5 | 10 |
| Combat/dimensions | into_nether 2; withered_away 5; final_dimension 3; dragon_slayer 6; mythic_power 4; veteran 3 | 23 |

This is **38 unique challenges / 135 SP**, the lower end of the requested 135–150 range, before any optional meta bonuses. Existing 1 SP is included, not added on top. Jetpacks consume 75 SP + 1 PP. Do not inflate the total with shopping cashback, starter AE2, trivial QoL crafts or optional completion bonuses. Unimplemented/undecided milestones are excluded from the live obtainable total.

Contract IDs use `contract/dragon_reborn`, `contract/wither_hunter`, `contract/industrial`, `contract/singularity`, `contract/nitro`, `contract/antimatter`; PP redemption remains the separate Core path. Maximum initial selection: 5–8 types. Dragon +1, three extra Withers +1, four Singularities +1, Nitro 16/32/64 candidates and four Antimatter Pellets +3 are proposals requiring the relevant balancing gate. Industrial inputs remain unspecified. No timed login rewards, clock cooldowns, low-value bulk exchanges or PP/SP exchange.

Product definitions contain stable ID, category, PERMANENT or CURRENT_WORLD scope, SP/PP prices, independent tier requirement, delivery adapter, confirmation and refund policy. Existing skills/research stay in their existing catalog/storage during transition. Reserve `shop/test_entitlement` for a harmless test in phase 3; `shop/advanced_ae2_starter` is only a design identifier. Its example 5-SP price and contents are not activated.

## Event and credit architecture

A server `ChallengeService` receives normalized evidence from adapters, validates actor/prerequisites and updates a profile atomically. Completion, counter changes, SP and any tier increase commit together. Only after success are balance snapshots and chat feedback sent. No new every-tick inventory/world scans. Events are deduplicated with stable evidence IDs when replayable; partial progress and counters are bounded. FakePlayer, null-player and client reports cannot award personal milestones.

| Trigger family | Generic part | Adapter / unresolved requirement |
| --- | --- | --- |
| Personal craft | Server craft evidence; required recipe/ingredients; distinct tool set | Verify real crafting and smithing event semantics; preserve fragment check; AE terminal path tests. MA seed tier metadata needs MA-specific adapter, not all item IDs containing seed. |
| Dimensions | Server dimension event per player | Nether/End definition; no replay reward after claim. |
| Bosses / hostile kills | Server death, credited real player, entity evidence ID | Dragon attribution can differ from ordinary last-hit mobs. Define participation policy before implementation; no nearby-player blanket award. Veteran persists across worlds. |
| Forever dirt | Successful committed Condenser operation | Existing Condenser lacks an ownership contract. Bind a qualifying initiating interaction; no points just for picking up traded dirt. Multiplayer policy remains open. |
| AE2 | Personal cell/storage crafts can use generic craft | Completed requesting-player job, wireless access and actual cross-dimension bridge use need version-specific AE2/terminal adapters. Starter components never pay. No ownership inference from shared-grid proximity. |
| Mekanism | Infuser craft can use generic craft | Successful Factory/miner work, running fission/fusion and SPS output need explicit operation evidence and attribution. Building a block alone is insufficient. |
| Powah | Shared machine evidence envelope | Orb completion and operating reactor tier need Powah-specific hooks and player binding. |
| IF | Shared machine evidence envelope | Crop-cycle, laser output and endgame action need IF hooks; installed JAR/API and final progression must be checked. |
| MA / Apotheosis | Craft/action envelope | Essence and Tier-5 resources require real recipe/tag/tier verification; first MA step and Apotheosis capstone remain open. |
| Resource turn-in | Explicit server command from Contracts UI | Typed inventory/escrow transaction; never possession detection. No ME auto-pull in first implementation. |

Optional adapters must be isolated behind mod-presence checks without loading unavailable classes. Test each supported installed version and a no-optional-mod server. Current CI only covers AE2/Iron Jetpacks/Ultimine/Mekanism/Powah for existing features; it does not prove the future adapters or IF/MA/Apotheosis integrations.

## Expanded profile schema proposal

Schema 3 (phase 2) keeps all six schema-2 fields and adds a nested, validated `meta` record:

- `shop_tier`: monotonic integer, **-1** meaning not yet earned; 0–4 for Foundations through Ascended. -1 avoids giving Tier 0 before First Foundations.
- `challenge_progress`: ID → bounded typed counter/set state, with objective revision.
- `contract_progress` and `contract_completions`: ID → current count and lifetime successful payouts.
- `permanent_purchases`: product ID → paid receipt/refund policy, separate from old skill ranks.
- `world_claims`: world UUID → product ID → receipt ID/status, each player already separated by profile UUID.
- `transactions`: bounded pending recovery references; immutable completed journal records stored separately by authority.
- `accounting`: SP/PP earned, spent, refunded, admin-granted **since tracking began**, plus a legacy-history-unknown flag. Do not fabricate lifetime earned/spent from current balance.

Monotonic tier calculation uses completed persisted challenges, then stores `max(oldTier, newlyQualifiedTier)` in the same update. Tier 0 from First Foundations, Tier I from personal Dragon Slayer. Higher-tier rules remain disabled proposals until confirmed: Engineer 3/4 Quantum/Nitro/Fission/IF, Mastery multiple capstones, Ascended Core + capstones. Ordinary respec cannot clear tier, progress, purchases without refund policy, world claims, receipts or counters. Admin SP has no tier effect.

New transaction-capable persistence needs revision/checksum and a durable journal checkpoint. The current previous-state backup may be stale; blindly accepting it would resurrect spent points/claims. Recover economic state by replaying validated journal records, or mark it read-only if recovery is ambiguous. Validate unknown IDs without dropping them. Keep file/entry bounds; paginate/partition world history rather than silently deleting receipts. Check cross-process profile ownership before enabling financial operations in simultaneous instances.

## World identity

Create one server-authoritative random UUID per save, shared across its dimensions, unrelated to name, folder, seed or player. Store it durably under the world data directory before enabling CURRENT_WORLD operations; expose only a friendly world name in normal UI. Pair UUID with an authority registration record/checksum so failed initial persistence cannot create a fresh spending identity on retry.

Renames and restarts retain the UUID. A normal new world gets a new UUID. A copied world intentionally retains the original identity and claims; copying a save is not a claim reset. Imported old worlds may receive an identity once only when there is no evidence of an existing financial history. A corrupt/missing previously registered identity disables purchases and requests recovery; never silently mint a replacement. Need an explicit administrative repair path and tests for lost data/backup restoration. Dedicated-server scope remains its existing server authority; there is no cross-server account service.

## Purchases and resource contracts: recovery boundary

A sequence of profile-save, inventory-grant and world-save is **not** an atomic transaction. In particular, recording delivered before or after inserting anonymous items alone cannot guarantee both no duplication and no loss. Existing Core identity protection cannot fix this by being copied wholesale.

Proposed phase-3 approach: a durable, owner/world-bound entitlement mailbox in the progression authority. Purchase under server-thread serialization:

1. Verify definition/revision, world identity, tier, balances, no prior world claim and request limits. Ignore client-supplied costs/items/player IDs.
2. Allocate a receipt keyed by `(authority, player, world, product)`; repeated requests resolve the same receipt.
3. Commit debit + world claim + immutable delivery entitlement together in the profile/journal transaction. On write failure, change nothing and send an error.
4. UI shows the durable entitlement as ready/pending. Phase 3 tests a harmless non-item entitlement, not valuable inventory delivery.
5. Enable real inventory withdrawal only after a separately tested recipient-bound delivery protocol can reconcile persisted player inventory and receipts at every crash cut. Until then expose no withdraw action or high-value product. ME insertion, loose dropped items and new world blocks are not in scope.

If a future real-item transfer is ambiguous, keep its recoverable transaction/entitlement locked for reconciliation; never auto-grant or auto-refund on uncertainty. Supported ordinary crash/restart semantics and the limits of independently restoring backups must be documented. Do not claim arbitrary-rollback protection for fungible items.

Contracts require a corresponding explicit escrow protocol: validate resources/identity/count and persist intent before making inventory changes; credit and reset the contract counter only after the escrow debit is durably reconciled. Profile-write failure must leave resources available. Crash between inventory debit and ledger commit must retain a recoverable intent, and replay must not take resources or grant SP twice. No resource contracts go live until fault-injection tests prove these boundaries. Clock time is never the eligibility mechanism.

Boss contracts share the event ledger, not resource escrow. First personal Dragon kill pays the unique challenge only. Later qualifying **respawned** Dragon kills may pay repeatables only once the approved mastery gate is met. New world's original dragon is not a substitute for respawn evidence. First Wither credit does not enter the additional-three counter. Contract payouts, counter reset and completion count commit together. Retroactive pre-unlock counters are not silently accrued.

## GUI architecture and phase-1 behavior

Six main sections, kept as one navigation hierarchy, with bounded scrollable cards:

| Main section | Subnavigation | Phase 1 |
| --- | --- | --- |
| Overview | Quick links to Challenges, Shop, Contracts, Research | Live SP/PP, known challenge completion count, research stage and explicit foundation status; no fictional world/tier values. |
| Challenges | Category selector + status filter + search; Contracts | Existing First Foundations in All/Forever; completed/open filters; Contracts explains later gate and consumption. Only populated challenge categories appear. |
| Shop | Permanent / Current World / Tier Progress | Existing Extended Reach purchase remains functional; other two sections clearly state not activated. No dummy purchasable packages. |
| Research | Iron Jetpacks | Existing four research stages, prerequisites, exact costs, optional-mod reason, inline variant descriptions. No fake future categories. |
| Perks | Ascension and dual-cost unlocks | Existing explanation, PP balance and Creative research with its prerequisite. |
| Profile | Balances, known purchases, scope, confirmed respec | Real snapshot data only; no invented lifetime totals; owner/operator test button only for permitted users. |

Keep six sections in a compact sidebar at normal sizes; use a compact section selector at narrow sizes to preserve card width and legible 1x text. Subpage selection and scroll position survive a detail view and confirmation return. Search accepts all normal text including the K hotkey; Escape exits search focus before closing. Right-click clears search. Inline text wraps, actions use their own row, clipping applies to drawing and clicks, and no large hover wall is needed. Status always includes words, not just color. World continues running as before.

A future detail screen extends the same card model with full objectives/prerequisites. Server snapshots must eventually carry authoritative status/lock reasons, current tier, world identity validity and receipt summaries; no client-derived economic eligibility. Paginate large catalogs/history with bounded packets; do not just remove the current 128-entry packet limit. Future payload changes require a new protocol version and client/server version match.

## Migration and staged gates

1. **Phase 1 / 0.10.0:** architecture + hierarchical K screen, Overview, category/search/filter shell, existing purchases/research/perks and Profile. Schema/network untouched. Compare production JAR server/profile/recipe classes against 0.9.1; run existing build and both dedicated-server suites. Manual EN/DE, GUI scales, search, respec and JEI test checklist required.
2. **Phase 2:** schema 3 + generalized personal evidence, core crafts/counters, dimension/combat events and Tier 0/I. Migrate First Foundations by identity, not re-award. Defer dirt ownership until policy is settled. Test v1→v2→v3 and v2→v3 without minted points (except the established v1 Creative refund), restarts, fake players, duplicates, failure-before-commit and monotonic tiers. Unknown IDs preserved.
3. **Phase 3:** durable world identity, journal/recovery, one harmless test entitlement, claim-per-world and exact tier/cost checks. Test renames/copies/new saves, concurrency, spam, corrupt identity, stale backups and every transaction crash cut. Item-delivery choice remains a separate gate.
4. **Phase 4:** each tech adapter as its own bounded change, verify real installed APIs and optional absence. No awarding for possession where real action evidence is required.
5. **Phase 5:** mastery-gated boss contracts first; resource contracts only after escrow safety and measured production rates. Select Tier I vs II and quantities through balancing; no arbitrary SP/hour claim.
6. **Phase 6:** separately design real product contents/prices/scopes/PP requirements and delivery. Never activate the example AE2 package from this handoff alone.
7. **Phase 7:** full UI, translations, detail views, profile accounting, accessible input and exported-pack verification. Remove temporary test-SP UI before public release.

## Remaining decisions (intentionally unresolved)

First iron; water/lava; pre-Diamond obsidian; IF endgame and Apotheosis objective; first MA objective; repeatable quantities/SP per hour; Tier II–IV conditions; Contracts Tier I vs II; machine/team/network attribution; boss participation; product contents/prices/PP use; progressive category visibility; real delivery method and reconciliation. No Create/sieve shortcut, no added Contract Board block, no island edit, no main merge or release.

## Manual acceptance for phase 1

Open K at GUI scales 2/3/4, 1920×1080 and a small window; EN and DE. Visit all six sections and every subpage. Search by title/description, type K inside search, clear by right click, switch filters and return from detail/confirmation. Scroll long Jetpack variants, click partially clipped cards and ensure neighboring actions do not fire. Check SP/PP before/after existing buys and refunds, completed First Foundations, chat reward shortcut, optional Jetpacks missing, normal-player absence of admin button and reconnect state. Inspect JEI overlay with the installed client. Automated server tests do not certify rendered client layout.
