# Changelog

This file is the concise version history. Detailed test evidence belongs in [VALIDATION.md](VALIDATION.md); current integrated repository status belongs in [CURRENT_STATE.md](CURRENT_STATE.md). Entries above 0.13.4 below are development-branch versions until explicitly merged.

## 0.16.0 — General polish / phase 7 development

- Keep pack progression balance intentionally unchanged while the wider modpack structure is still being designed.
- Harden progression request validation without changing the `skills-8` packet shape or schema 4/5/6 persistence model.
- Correct the real World Shop purchase confirmation so it describes the actual package instead of the obsolete Phase-3 test entitlement.
- Keep READY package claims actionable from the World Shop instead of disabling the visible Claim Package button with new-purchase rules.
- Allow a consumed Supply Cache to contribute its own main-inventory slot to package delivery capacity while correctly excluding offhand capacity.
- Extend GameTests for claim requests, malformed progression packets and Supply Cache slot reuse.
- Rename CI stages around the current regression/full-pack integration boundary and document deferred balance/K-menu work explicitly.

## 0.15.0 — Real current-world shop delivery / phase 6 development

- Activate the first bounded real product, `shop/advanced_ae2_starter`, at Tier I for 12 SP / 0 PP once per player/world identity.
- Deliver a deliberately challenge-safe AE2 autocrafting infrastructure package through a unique owner/world/receipt-bound Supply Cache.
- Add durable READY/DISPATCHING/DELIVERED/OPENING/OPENED delivery reconciliation around forced player-data checkpoints.
- Add dynamic schema 6 only once delivery state exists; contract-only profiles remain schema 5 and untouched profiles remain schema 4.
- Move the matching client/server protocol to `skills-8` with bounded authoritative product state.
- Keep Tier II-IV, additional real products and arbitrary-backup rollback recovery outside this phase.

## 0.14.0 — Repeatable contracts / phase 5 development

- Add Dragon Reborn and Wither Hunter repeatable boss payouts after their one-time milestones.
- Add explicit Singularity, Nitro and Antimatter resource turn-ins with server-side consumption.
- Add replay guards, durable resource intents and crash-recovery checks around player inventory checkpoints.
- Introduce dynamic schema 5 for contract state and protocol `skills-7` while preserving schema-4 profiles until needed.

## 0.13.4 — Design-2 revision 3 and main integration

- Rework the 1920x1080 / GUI-scale-3 status header into stable icon/text lanes with readable SP, PP, Tier and placeholder status fields.
- Replace placeholder challenge/category art with real optional-integration items where available, using registry lookup and safe fallbacks.
- Separate challenge title/objective/category/progress/reward lanes to prevent overlap and improve wide-page text readability.
- Preserve server-authoritative progression, schema 4, `skills-6`, shop transactions, challenge triggers and research rules.
- GitHub Actions run #151 passed for feature head `f5a7bdadfab0d4480c45898b0b0f288bc7dbbc46`.
- Integrate the complete stacked development line into `main` through PR #11 on October 16, 2026. No release was published by the merge.
- Normalize repository documentation so current files no longer describe 0.9-0.13 work as unmerged.

## 0.13.1-0.13.3 — Design-2 K-menu overhaul

- Add the premium full-screen progression shell, persistent top status area and six-section navigation.
- Add responsive challenge sidebar/grid behavior, search, open/completed filters, sorting and grid/list presentation.
- Add premium visual mixins while keeping progression actions and networking owned by the existing screen/service code.
- Refine visual hierarchy, challenge cards, category counts and GUI-scale behavior through screenshot-driven passes.

## 0.13.0 — Technology challenges / phase 4

- Expand the live catalog to **20 personal challenges / 58 SP**.
- Add bounded AE2, Mekanism, Powah and Mystical Agriculture challenge adapters with fail-closed runtime compatibility checks.
- Keep unreliable shared-machine objectives inactive instead of rewarding inventory possession or nearby activity.
- Preserve schema 4, Tier 0/I and the existing `skills-6` payload.

## 0.12.0 — Current-world shop safety / phase 3

- Add stable per-save identity with independent primary/backup validation.
- Extend profiles to schema 4 world claims and immutable purchase receipts.
- Add write-ahead transaction journals and idempotent recovery across tested crash cuts.
- Add the owner/operator Phase 3 Test Entitlement: Tier I, 5 SP, once per world identity, no delivery or gameplay benefit.

## 0.11.0 — Personal challenge progression / phase 2

- Activate 11 personal challenges / 34 SP, persistent objective evidence and permanent Tier 0/I.
- Add explicit accounting for newly tracked earned/spent/refunded/admin currency.
- Add personal crafting/smithing, combat and dimension evidence with FakePlayer/nearby-player exclusions.

## 0.10.0 — Progression navigation foundation / phase 1

- Replace the earlier flat tabs with Overview, Challenges, Shop, Research, Perks and Profile.
- Add live overview data, challenge search/filter/detail presentation and separated Shop sections.
- Preserve existing purchases, Jetpack research, Perks and confirmed refunds while keeping future systems inactive.

## 0.9.0-0.9.1 — Machine visual overhaul

- Redesign Condenser and Ascension models, animated internals and matching machine GUIs.
- Correct item rendering scale, labels, core-slot border, inventory spacing and button hover/focus feedback.

## 0.8.0 — Ascension and Perk Points

- Add Forever Core, Ascension Chamber and persistent Perk Points.
- Require 40 SP + 1 PP for Creative Jetpack research and add dual-currency refunds.
- Add replay-safe Core identity claims, recipes and tested automation boundaries.

## 0.7.0 — Permanent tool progression

- Replace the approved 24 Vanilla mining/farming tool recipes with permanent tiered progression.
- Add Reinforced Netherite Ingot and component-preserving Netherite smithing.
- Mirror the authoritative recipe/removal set into the Recipe Editor generated pack.

## 0.6.1 — Chamber rendering and JEI transfer

- Fix Mixing Chamber occlusion/z-fighting issues.
- Add server-validated JEI Condensing recipe transfer and unavailable-recipe reasons.
- Suppress the Vanilla introductory movement/tree/inventory/crafting tutorial sequence.

## 0.6.0 — Condenser mixing

- Add reusable Mortar, Stone Meal and Mixing Chamber Mark I.
- Add two-input dirt processing and persistent ghost filters for manual/automated input.

## 0.5.0 — Iron Jetpacks research

- Add four sequential research stages at 5/10/20/40 SP; Creative later gains the 1 PP requirement in 0.8.0.
- Gate supported crafting, JEI visibility and flight paths on server-authoritative research state.

## 0.4.0 — Skill cards and confirmed refunds

- Replace the earlier detail layout with clearer full-width cards.
- Add confirmed skill reset/refund and a clickable point-reward shortcut.

## 0.3.0-0.3.1 — Persistent skills

- Add cross-world UUID-based progression profiles, First Foundations and Extended Reach.
- Add atomic storage/backups, K-menu access and the readability fix for custom screen rendering.

## 0.2.0-0.2.1 — Condenser Mark I

- Add log/plank processing into Stone Fragments, persistent progress, hopper/pipe support and JEI integration.
- Add the 2x2 cobblestone recipe and revised Stone Fragment sprite.

## 0.1.0-0.1.1 — Initial project import and identity cleanup

- Import the original mod source, starter pack files and documentation foundation.
- Standardize English project documentation, TeKatz identity and `de.tekatz.foreverskyblock` package/group while preserving registered IDs.
