# K menu Design 2 overhaul — 0.13.4

Development branch: `feature/k-menu-design2-premium-pass`, based on `feature/k-menu-design2-visual-pass`. Minecraft 1.21.1, Java 21; pack runtime NeoForge 21.1.259. No main merge or release is implied.

## Goal

This client-only navigation overhaul follows the approved Design 2 mockup as closely as practical while keeping all existing server-authoritative progression data and actions intact. It changes presentation and navigation, not the SP/PP economy, challenge triggers, profile schema, shop transactions or research rules.

## 0.13.4 revision 3 alignment pass

Revision 3 is based on direct 1920x1080 / GUI-scale-3 screenshots from the real pack. It fixes layout and icon problems that remained after the first premium passes:

- the status header is rebuilt around fixed icon and text lanes instead of heavily compressed generic scaling;
- Skill Points, Perk Points, Tier Status, Playtime and Island Level now use consistent baselines, larger readable values and better-aligned icon areas;
- the Tier Status placeholder item is replaced by a dedicated bronze tier badge drawn by the UI;
- the challenge card renderer no longer draws a large secondary item/artwork block over the right side of the text lane;
- challenge cards use the actual relevant integration item when the optional mod is installed: AE2 storage/crafting items, Mekanism Metallurgic Infuser and Digital Miner, Powah Energizing Orb and Mystical Agriculture Supremium Essence;
- the challenge category rail resolves representative real items from AE2, Mekanism, Powah and Mystical Agriculture instead of Vanilla stand-ins;
- optional integration icons are looked up from the runtime item registry with safe fallbacks, so there is no new hard class dependency on those mods;
- wide content text now has a minimum readable render scale, fixing the tiny Shop, Research, Perks and Profile text seen at GUI scale 3 while leaving compact toolbar/filter controls small enough to fit;
- challenge card title, objective, category, progress and reward areas have explicit non-overlapping lanes.

The progression rules and networking are unchanged.

## Previous premium visual pass

The premium pass keeps `PremiumSkillScreen` as the owner of navigation, filtering, sorting, progression requests and data. Client-only mixins add the final presentation layer so the visual work stays isolated from progression behaviour.

The visual system includes:

- a deep navy shell with drop shadows, inner rims and cyan highlights;
- prominent Forever Skyblock branding and a status-header hierarchy;
- Skill Point, Perk Point and Tier Status cards with colour separation and glow;
- a compact Playtime / Island Level presentation area matching the concept layout; these two values remain placeholders until authoritative data sources exist;
- highlighted navigation and selected states;
- challenge-category rows with category colours and live counts;
- challenge cards with category-coloured rails, objective text, reward badges, progress bars and completion styling;
- consistent panel depth and shadows across the screen.

The responsive layout remains controlled by `PremiumMenuLayout`. In particular, 640x360 logical GUI space — corresponding to 1920x1080 at GUI scale 3 — keeps the challenge sidebar and three-column grid visible.

## Main structure

The K menu uses one persistent visual hierarchy across every page:

1. A top status header with the Forever Skyblock identity plus real profile values for SP, PP and permanent Shop Tier.
2. A full-width horizontal navigation row for **Overview**, **Challenges**, **Shop**, **Research**, **Perks** and **Profile**.
3. One large content workspace below the navigation. Existing purchase, reset, research, profile and detail actions continue to call the same handlers as before the visual overhaul.

## Challenge browser

Challenges follows the approved mockup structure:

- fixed **Filters** sidebar when enough logical width is available;
- search field;
- **All / Open / Completed** state controls;
- category list with live counts for **All**, **Forever**, **Applied Energistics 2**, **Mekanism**, **Powah**, **Mystical Agriculture**, **Combat & Dimensions** and **Contracts**;
- a large right-side challenge area;
- Progress / SP / A-Z sorting;
- grid/list view toggle;
- three challenge columns at the primary target size;
- category-coloured cards with the relevant item icon, title, objective text, category, authoritative progress, progress bar and SP reward badge;
- completed milestones use a green completion treatment;
- selecting a challenge retains the existing detail view rather than putting long text into a hover tooltip.

Contracts remains an explanatory inactive page. The redesign does not fabricate repeatable rewards before the Contracts phase is implemented.

## Responsive behavior

The mockup is the primary large-screen target. Smaller windows retain readable native-size text instead of shrinking the whole interface:

- below the large-layout threshold, the permanent challenge sidebar collapses into compact category/filter controls;
- the challenge grid automatically changes from 3 columns to 2 and then 1;
- very small supported viewports use a compact header and reduced controls while retaining a scrollable content area;
- the six primary sections remain available at every supported size.

`PremiumMenuLayoutTest` covers the Design-2 geometry, including the 640x360 logical target.

## Preserved behavior

The overhaul intentionally does not change:

- the active challenge catalog and SP rewards;
- challenge evidence or attribution;
- profile persistence and networking;
- Shop Tier rules;
- Extended Reach purchase;
- world-scoped shop transaction safety;
- Iron Jetpacks research and reset/refund behavior;
- PP / Ascension behavior;
- machine GUIs, recipes, textures or the fixed starting-island NBT.

No new profile data is synthesized for visual parity with the concept image. The Playtime and Island Level slots are deliberately shown as unavailable until those systems expose authoritative values.

## Validation

The 0.13.4 revision-3 source compiled successfully in GitHub Actions and passed the existing dedicated Condenser game-test step plus the Phase 4 technology/integration game-test step before documentation was updated. Headless CI validates compilation and existing server behaviour, but it cannot certify rendered client pixels.

Before merge, test the 0.13.4 JAR in the actual pack and check:

1. Open K at 1920x1080 / GUI scale 3 and compare the top status row directly with the approved reference.
2. Verify Digital Extraction renders the Mekanism Digital Miner, Mekanized renders the Metallurgic Infuser and the AE2/Powah/Mystical challenges use their real integration items.
3. Verify the category rail shows real integration icons and that labels/counts never overlap them.
4. Check Challenge card title, description, category, progress and reward lanes for overlap across every row.
5. Open Shop, Research, Perks, Profile and Overview and verify their body text is comfortably readable at GUI scale 3.
6. Click every primary navigation section and return to Challenges.
7. Test every Challenge category, including empty/inactive Contracts.
8. Search by title and description; click elsewhere to defocus and right-click the search box to clear it.
9. Toggle All/Open/Completed, cycle sorting and switch grid/list mode.
10. Verify real SP, PP, Tier, progress and category counts update after a qualifying action without reconnecting.
11. Test Shop purchases, Research, Perks, Profile reset confirmation and current-world shop actions to ensure the pass changed presentation only.
12. Inspect English and German labels, JEI overlap and mouse hitboxes. Headless CI cannot approve these visual checks.

Do not merge this branch into `main` until the redesigned screen has been accepted in-game.
