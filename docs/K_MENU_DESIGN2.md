# K menu Design 2 overhaul — 0.13.1

Development branch: `feature/k-menu-design2-overhaul`, based on `feature/tech-challenges-phase4`. Minecraft 1.21.1, Java 21; pack runtime NeoForge 21.1.259. No main merge or release is implied.

## Goal

This client-only navigation overhaul follows the approved Design 2 mockup as closely as practical while keeping all existing server-authoritative progression data and actions intact. It changes presentation and navigation, not the SP/PP economy, challenge triggers, profile schema, shop transactions or research rules.

## Main structure

The K menu now uses one persistent visual hierarchy across every page:

1. A top status header with the Forever Skyblock identity plus real profile values for SP, PP and permanent Shop Tier. On the full layout it also shows the verified current world name instead of inventing unavailable playtime or island-level data.
2. A full-width horizontal navigation row for **Overview**, **Challenges**, **Shop**, **Research**, **Perks** and **Profile**. The active section uses a cyan highlight and every section has a Minecraft item icon.
3. One large content workspace below the navigation. Existing purchase, reset, research, profile and detail actions continue to call the same handlers as 0.13.0.

## Challenge browser

At the 960×540 / GUI-scale-2 design target, Challenges follows the approved mockup directly:

- fixed **Filters** sidebar on the left;
- search field;
- **All / Open / Completed** state controls;
- expandable category list with live counts for **All**, **Forever**, **Applied Energistics 2**, **Mekanism**, **Powah**, **Mystical Agriculture**, **Combat & Dimensions** and **Contracts**;
- a large right-side challenge area;
- Progress / SP / A–Z sorting;
- grid/list view toggle;
- three challenge columns at the target size;
- category-colored cards with item icon, title, category, concise objective text, authoritative progress, progress bar and SP reward badge;
- completed milestones use a green completion treatment;
- selecting a challenge retains the existing detail view rather than putting long text into a hover tooltip.

Contracts remains an explanatory inactive page. The redesign does not fabricate repeatable rewards before the Contracts phase is implemented.

## Responsive behavior

The mockup is the primary large-screen target. Smaller windows retain native-size text instead of shrinking the whole interface:

- below the large-layout threshold, the permanent challenge sidebar collapses into compact category/filter controls;
- the challenge grid automatically changes from 3 columns to 2 and then 1;
- very small supported viewports use a compact header and reduced controls while retaining a scrollable content area;
- the six primary sections remain available at every supported size.

`ProgressionMenuLayoutTest` covers the layout invariants for 320×180 through 1920×1080 and specifically requires the full sidebar plus three-column grid at the 960×540 target.

## Preserved behavior

The overhaul intentionally does not change:

- the 20 active Phase-4 challenges / 58 SP catalog;
- challenge evidence or attribution;
- schema 4 profile persistence;
- `skills-6` networking;
- Shop Tier rules;
- Extended Reach purchase;
- world-scoped Phase-3 test entitlement and transaction safety;
- Iron Jetpacks research and reset/refund behavior;
- PP / Ascension behavior;
- machine GUIs, recipes, textures or the fixed starting-island NBT.

No new profile data is synthesized for visual parity with the concept image. Values shown in the header come from the existing authoritative snapshot.

## Manual client acceptance

Automated tests can validate compilation, responsive geometry and unchanged server behavior, but they do not certify rendered pixels. Before merge, test the 0.13.1 JAR in the actual pack and check:

1. Open K at GUI scales 2, 3 and 4 and at 1920×1080 plus a smaller window.
2. Compare the Challenge page visually with the approved Design 2 reference: top status header, horizontal navigation, left category sidebar and three-column card grid at the target size.
3. Click every primary navigation section and return to Challenges.
4. Test every Challenge category, including empty/inactive Contracts.
5. Search by title and description; click elsewhere to defocus and right-click the search box to clear it.
6. Toggle All/Open/Completed, cycle sorting and switch grid/list mode.
7. Scroll a long challenge list and open/close details without losing unrelated progression state.
8. Verify real SP, PP, Tier, progress and category counts update after a qualifying action without reconnecting.
9. Test Shop purchases, Research, Perks, Profile reset confirmation and the Phase-3 current-world test entitlement to ensure the redesign changed only navigation/presentation.
10. Inspect English and German labels, JEI overlap and mouse hitboxes. Headless CI cannot approve these visual checks.

Do not merge this branch into `main` until the redesigned screen has been accepted in-game.
