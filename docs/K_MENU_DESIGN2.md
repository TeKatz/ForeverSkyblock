# K menu Design 2 overhaul — 0.13.4

**Status:** integrated into `main` through PR #11 on October 16, 2026. Minecraft 1.21.1, Java 21; pack runtime NeoForge 21.1.259. Integration does not imply a published release.

## Goal

This client-side navigation overhaul follows the approved Design 2 mockup as closely as practical while keeping all existing server-authoritative progression data and actions intact. It changes presentation and navigation, not the SP/PP economy, challenge triggers, profile schema, shop transactions or research rules.

## 0.13.4 revision 3 alignment pass

Revision 3 is based on direct 1920x1080 / GUI-scale-3 screenshots from the real pack. It fixes layout and icon problems that remained after the first premium passes:

- the status header is rebuilt around fixed icon and text lanes instead of heavily compressed generic scaling;
- Skill Points, Perk Points, Tier Status, Playtime and Island Level use consistent baselines, larger readable values and better-aligned icon areas;
- the Tier Status placeholder item is replaced by a dedicated bronze tier badge drawn by the UI;
- the challenge card renderer no longer draws a large secondary artwork block over the text lane;
- challenge cards use the relevant integration item when the optional mod is installed: AE2 storage/crafting items, Mekanism Metallurgic Infuser and Digital Miner, Powah Energizing Orb and Mystical Agriculture Supremium Essence;
- the challenge category rail resolves representative real items from AE2, Mekanism, Powah and Mystical Agriculture instead of Vanilla stand-ins;
- optional integration icons are looked up from the runtime item registry with safe fallbacks, so there is no new hard class dependency on those mods;
- wide content text has a minimum readable render scale while compact toolbar/filter controls remain small enough to fit;
- challenge-card title, objective, category, progress and reward areas have explicit non-overlapping lanes.

The progression rules and networking are unchanged.

## Visual system

`PremiumSkillScreen` owns navigation, filtering, sorting, progression requests and data. Client-only visual mixins provide the presentation layer so the visual work remains isolated from progression behavior.

The integrated visual system includes:

- a deep navy shell with drop shadows, inner rims and cyan highlights;
- prominent Forever Skyblock branding and a status-header hierarchy;
- Skill Point, Perk Point and Tier Status cards with clear visual separation;
- a compact Playtime / Island Level area; these values remain unavailable until authoritative data sources exist;
- highlighted navigation and selected states;
- challenge-category rows with category colors and live counts;
- challenge cards with relevant item icons, objective text, reward badges, progress bars and completion styling;
- consistent panel depth and shadows across the screen.

The responsive layout remains controlled by `PremiumMenuLayout`. At 640x360 logical GUI space — corresponding to 1920x1080 at GUI scale 3 — the challenge sidebar and three-column grid remain available.

## Main structure

The K menu uses one visual hierarchy across every page:

1. Top status header with the Forever Skyblock identity plus real profile values for SP, PP and permanent Shop Tier.
2. Full-width navigation for **Overview**, **Challenges**, **Shop**, **Research**, **Perks** and **Profile**.
3. One content workspace below the navigation. Existing purchase, reset, research, profile and detail actions call the same server-authoritative handlers as before the visual overhaul.

## Challenge browser

Challenges provides:

- fixed **Filters** sidebar when enough logical width is available;
- search field;
- **All / Open / Completed** state controls;
- category list with live counts for **All**, **Forever**, **Applied Energistics 2**, **Mekanism**, **Powah**, **Mystical Agriculture**, **Combat & Dimensions** and **Contracts**;
- Progress / SP / A-Z sorting;
- grid/list view toggle;
- three columns at the primary target size;
- category-colored cards with relevant item icon, title, objective, category, authoritative progress, progress bar and SP reward;
- green completed-state treatment;
- challenge details without relying on long hover-only text.

Contracts remains explanatory and inactive. The redesign does not fabricate repeatable rewards before that phase is implemented.

## Responsive behavior

- Below the large-layout threshold, the permanent challenge sidebar collapses into compact controls.
- The challenge grid changes from 3 columns to 2 and then 1.
- Very small supported viewports use a compact header and reduced controls while retaining scrollable content.
- All six primary sections remain available at every supported size.

`PremiumMenuLayoutTest` covers the Design-2 geometry, including the 640x360 logical target.

## Preserved behavior

The overhaul intentionally does not change:

- active challenge definitions and SP rewards;
- challenge evidence or attribution;
- profile persistence and networking;
- Shop Tier rules;
- Extended Reach;
- world-scoped shop transaction safety;
- Iron Jetpacks research and reset/refund behavior;
- PP / Ascension behavior;
- machine GUIs, recipes, textures or the fixed starting-island NBT.

No profile data is synthesized for visual parity with the concept image. Playtime and Island Level deliberately remain unavailable until those systems expose authoritative values.

## Validation

The 0.13.4 revision-3 feature head `f5a7bdadfab0d4480c45898b0b0f288bc7dbbc46` passed GitHub Actions run #151 before integration. The workflow compiled the mod and passed the dedicated Condenser and NeoForge 21.1.259 Phase 4 integration suites. Headless CI validates compilation and server behavior; it cannot certify rendered client pixels.

For final client acceptance/release readiness, check:

1. Open K at 1920x1080 / GUI scale 3 and compare the top status row with the approved reference.
2. Verify Digital Extraction renders the Mekanism Digital Miner, Mekanized renders the Metallurgic Infuser and AE2/Powah/Mystical challenges use real integration items.
3. Verify category-rail icons do not overlap labels/counts.
4. Check title, description, category, progress and reward lanes across challenge rows.
5. Open Shop, Research, Perks, Profile and Overview and verify body text remains comfortably readable.
6. Click every primary section and return to Challenges.
7. Test every Challenge category, including inactive Contracts.
8. Search by title/description; click elsewhere to defocus and right-click the search box to clear it.
9. Toggle All/Open/Completed, cycle sorting and switch grid/list mode.
10. Verify live SP, PP, Tier, progress and counts update after qualifying actions without reconnecting.
11. Recheck Shop purchases, Research, Perks, Profile reset confirmation and current-world shop actions.
12. Inspect English/German labels, JEI overlap and mouse hitboxes.

Any real client acceptance result should be recorded separately in [VALIDATION.md](VALIDATION.md).
