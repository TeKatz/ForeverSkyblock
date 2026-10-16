# K menu Design 2 overhaul — 0.13.2

Development branch: `feature/k-menu-design2-premium-pass`, based on `feature/k-menu-design2-visual-pass`. Minecraft 1.21.1, Java 21; pack runtime NeoForge 21.1.259. No main merge or release is implied.

## Goal

This client-only navigation overhaul follows the approved Design 2 mockup as closely as practical while keeping all existing server-authoritative progression data and actions intact. It changes presentation and navigation, not the SP/PP economy, challenge triggers, profile schema, shop transactions or research rules.

## 0.13.2 premium visual pass

The premium pass keeps `PremiumSkillScreen` as the owner of navigation, filtering, sorting, progression requests and data. Client-only mixins add the final presentation layer so the visual work stays isolated from progression behaviour.

Compared with 0.13.1, the pass adds:

- a deeper navy shell with stronger drop shadows, inner rims and cyan highlights;
- larger Forever Skyblock branding and a more prominent header hierarchy;
- richer Skill Point, Perk Point and Tier Status cards with stronger colour separation and glow;
- a compact Playtime / Island Level presentation area matching the concept layout; these two values remain placeholders until authoritative data sources exist;
- stronger navigation chrome and selected-state lighting;
- cleaner challenge-category rows with subdued separators, category colours and a clearer selected state;
- redesigned challenge cards with category-coloured rails, icon artwork, objective text, reward badges, stronger progress bars, completion styling and hover glow;
- subtle blocky scenery and depth accents behind the challenge grid;
- more consistent panel depth, highlights and shadows across the rest of the screen.

The responsive layout remains controlled by `PremiumMenuLayout`. In particular, 640×360 logical GUI space — corresponding to 1920×1080 at GUI scale 3 — keeps the challenge sidebar and three-column grid visible.

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
- Progress / SP / A–Z sorting;
- grid/list view toggle;
- three challenge columns at the primary target size;
- category-coloured cards with item icon, title, objective text, category, authoritative progress, progress bar and SP reward badge;
- completed milestones use a green completion treatment;
- selecting a challenge retains the existing detail view rather than putting long text into a hover tooltip.

Contracts remains an explanatory inactive page. The redesign does not fabricate repeatable rewards before the Contracts phase is implemented.

## Responsive behavior

The mockup is the primary large-screen target. Smaller windows retain readable native-size text instead of shrinking the whole interface:

- below the large-layout threshold, the permanent challenge sidebar collapses into compact category/filter controls;
- the challenge grid automatically changes from 3 columns to 2 and then 1;
- very small supported viewports use a compact header and reduced controls while retaining a scrollable content area;
- the six primary sections remain available at every supported size.

`PremiumMenuLayoutTest` covers the Design-2 geometry, including the 640×360 logical target.

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

The 0.13.2 source must pass the normal Gradle build plus the existing dedicated condenser and Phase 4 integration game-test steps before its JAR is handed out. CI validates compilation and existing server behaviour, but it cannot certify rendered client pixels.

Before merge, test the 0.13.2 JAR in the actual pack and check:

1. Open K at the GUI scale and resolution used for the approved reference, especially 1920×1080 at GUI scale 3.
2. Compare the Challenge page visually with the Design 2 reference: branding, status header, navigation, filter rail, challenge cards, shadows, glow and spacing.
3. Click every primary navigation section and return to Challenges.
4. Test every Challenge category, including empty/inactive Contracts.
5. Search by title and description; click elsewhere to defocus and right-click the search box to clear it.
6. Toggle All/Open/Completed, cycle sorting and switch grid/list mode.
7. Scroll a long challenge list and open/close details without losing unrelated progression state.
8. Verify real SP, PP, Tier, progress and category counts update after a qualifying action without reconnecting.
9. Test Shop purchases, Research, Perks, Profile reset confirmation and current-world shop actions to ensure the pass changed presentation only.
10. Inspect English and German labels, JEI overlap and mouse hitboxes. Headless CI cannot approve these visual checks.

Do not merge this branch into `main` until the redesigned screen has been accepted in-game.
