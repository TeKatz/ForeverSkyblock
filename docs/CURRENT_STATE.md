# Current integrated state

Updated October 16, 2026 after the authorized integration of the 0.13.4 development stack into `main` through PR #11.

## Baseline

| Item | Current value |
| --- | --- |
| Mod version | **0.13.4** |
| Minecraft | **1.21.1** |
| Java | **21** |
| Pack runtime | **NeoForge 21.1.259** |
| Default build baseline | NeoForge 21.1.249 |
| Profile schema | **4** |
| Progression protocol | **skills-6** |
| Main integration commit | `3890636889ccd69839c3e7262a0784190e2d3483` |
| Last pre-merge feature head | `f5a7bdadfab0d4480c45898b0b0f288bc7dbbc46` |
| Verified pre-merge CI | GitHub Actions **#151**, success |
| Release | Not published by this merge |

## Integrated gameplay and UI

`main` now contains the complete stacked development line from 0.8.0 through 0.13.4:

- **0.9.x machine presentation:** redesigned Condenser/Ascension models, animated internals and corrected machine GUIs, including item scale, labels, inventory spacing and hover feedback.
- **0.10.0 navigation foundation:** Overview, Challenges, Shop, Research, Perks and Profile in one persistent K-menu hierarchy.
- **0.11.0 personal challenges:** persistent evidence, accounting and permanent Tier 0/I progression.
- **0.12.0 world-shop safety:** schema 4, stable world identities, claims/receipts and write-ahead transaction recovery; only the harmless owner/operator test entitlement is active.
- **0.13.0 technology challenges:** active AE2, Mekanism, Powah and Mystical Agriculture objectives with fail-closed attribution adapters.
- **0.13.1-0.13.4 Design-2 UI work:** premium K-menu shell, challenge filtering/sorting, real integration icons and the revision-3 alignment/readability pass.

The earlier Condenser, Mixing Chamber, permanent-tool, Jetpack research and Ascension/Perk systems remain integrated.

## Active progression boundary

- **20 challenges / 58 SP** are active.
- Permanent progression currently reaches **Tier 0 — Foundations** and **Tier I — Conqueror**.
- Iron Jetpacks research remains 5 / 10 / 20 / 40 SP, with Creative also requiring 1 PP.
- Profile schema 4 preserves SP, PP, accounting, challenge evidence, skill/research purchases, permanent tier, world claims/receipts and Core redemption receipts.
- Current-world purchases use a stable hidden world UUID and fail closed on ambiguous identity state.
- The only active current-world product is the development-only **Phase 3 Test Entitlement**: Tier I, 5 SP, once per player/world identity, no item or gameplay reward.

## Explicitly not active

Do not infer implementation from planning documents for any of the following:

- repeatable contracts;
- Tier II-IV;
- valuable world-shop packages or item delivery;
- Industrial Foregoing challenge rewards;
- deferred shared-machine/multiblock objectives whose personal attribution is unresolved;
- unresolved first-iron, water/lava or pre-Diamond Obsidian shortcuts;
- fabricated Playtime or Island Level values in the K-menu header.

## Pack state

The fixed starting island remains `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` with spawn `[8, 4, 5]` facing east and the verified eight-bread/one-seed 1k cell. It is not part of the 0.13.4 cleanup and must not change without explicit approval.

The mod inventory/list files are provisional evidence. The supplied 80-entry list still records an older Forever Skyblock artifact; that does **not** define the current repository mod version.

## Validation boundary

The 0.13.4 feature head passed GitHub Actions run #151 before merge. The workflow builds with Java 21, runs the dedicated Condenser suite and runs the NeoForge 21.1.259 integration suite with Ascension, Jetpacks, Ultimine and Phase 4 technology adapters.

Headless CI does not prove rendered client pixels. The 0.13.4 K-menu alignment was developed from real 1920x1080 / GUI-scale-3 screenshots, but any final visual acceptance must still be recorded as an explicit TeKatz in-game result rather than inferred from CI.

See [VALIDATION.md](VALIDATION.md) for detailed historical checks and [K_MENU_DESIGN2.md](K_MENU_DESIGN2.md) for the current manual UI checklist.
