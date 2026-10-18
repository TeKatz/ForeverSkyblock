# K-menu overhaul — 0.24.0 development

`feature/k-menu-overhaul` starts from validated 0.23.0 branch head `c08c9a3d65825d6025ba7981e28b2d41578579fd`. The integrated main baseline remains 0.20.1. This update implements TeKatz's October 19 screenshots and requested redesign; it does not imply a merge or release.

## Presentation and navigation

`PremiumSkillScreen` now owns its rendering, controls and geometry directly. The six accumulated premium visual/contract/shop mixins and their reflection-based client adapters are removed. The existing server transaction and world-upgrade reconciliation code remains in place.

- Code-drawn crystal, star and crest identify SP, PP and Shop Tier. Header labels/values and rewards are centered using actual text width.
- The right header shows authoritative Vanilla playtime and the new world-local Island Level.
- Profile uses the actual client's skin face, including the hat layer, both in its tab and in its identity section.
- The mod category rail has independent clipped scrolling. Its search placeholder and filter buttons are centered; search text remains normally editable, with right-click to clear and Escape to defocus.
- Challenge titles and descriptions have separate wrapping lanes. Rewards and progress never share the same text rectangle. Full descriptions and required tool objectives are available in readable detail windows.
- Contracts use the same clipped card browser, without the stray Book and Quill header image. Clicking any card opens its description, progress, repeat count and resource-consumption/automatic-payout explanation. Resource turn-in happens only through the explicit detail action and still uses the original server request.
- All scroll regions ease toward bounded targets with frame-rate independent motion. Body entry is a five-pixel, 200 ms ease; tabs and detail windows use a short accent reveal. Controls and hitboxes use the same animated coordinates. Scrollbars remain draggable.
- English and German labels are included. Small layouts collapse the category rail to compact category/filter controls. Each content view and detail prose remains scrollable.

## Shop graph

The Shop is a connected graph of round, clickable nodes, replacing the previous product strips. Node click areas are circular. Selected/owned states, prices and full descriptions are accessible through detail windows.

The permanent-skill branch contains Extended Reach with its original SP-only prerequisite. It is deliberately separate from the Tier-0 product branch: the overhaul does not invent a new Tier requirement for that skill. Tier 0 branches to No Phantoms, No More Rain and Permanent Day as siblings; purchasing one is not a prerequisite for another. Tier I is reached through Dragon Slayer and connects to the Advanced AE2 Starter Package. Only implemented Tier 0/I and actual products are shown. No speculative Tier II-IV purchases are introduced.

Product details explain world-wide effects or show exact package contents. Original world identity, current-world scope, confirmation, SP/PP/tier checks, operator restrictions, claim/recovery/delivered/opened states and non-refundable purchases are preserved. The operator-only transaction probe is available below the graph.

Research still offers all four existing Jetpack stages, prerequisites, runtime variants and missing-integration status. Creative's 40 SP + 1 PP requirement is unchanged. Perks retains the Creative research action and explains where PP comes from.

## Compact profile

The identity block and stat grid show player name, scope, current world, SP, PP, completed challenges, playtime, Island Level, unlocked skills, deaths, mob kills and world claims. The lower section retains earned/spent/refunded/admin accounting and its legacy-history caveat, confirmed respec and operator test points.

Playtime/deaths/mob kills refer to the current player's Vanilla statistics in the current save, not cross-world lifetime totals. They are read on the server. Playtime is displayed in hours/minutes; the server remains the source rather than a client timer.

## Island Level: explicitly chosen world progress

TeKatz selected **world milestones**, rather than weighted placed blocks, as the level foundation. Each distinct completed world milestone adds one level; the current maximum is 20. World evidence is shared by players in the save, including partial tool sets and bounded, deduplicated hostile-kill UUIDs. It consumes only existing authoritative challenge events. Fake players, client requests, reopening menus, purchases and refunded skills cannot manufacture levels.

Tracking starts with this update. Existing permanent player-profile completion sets are not imported as if they had happened in the current world. Repeating a qualifying action in a new world can advance its world evidence even when that player's permanent challenge is already complete; the original personal SP reward is never replayed. No currency or gameplay bonus is awarded by the level display.

The existing world-progress file and backup migrate from schema 1 to schema 2, preserving the world UUID, Trader unlock and active world upgrades. World evidence survives save/restart/rename along with the existing world identity. It does not follow a player into another save. This is save-wide world progress, not a separate Skyblock Builder team-island ranking.

Player profile schema remains dynamic 4/5/6. The added authoritative statistics extend the network snapshot to **skills-9**, requiring matching 0.24.0 client/server builds. A bounded read-only refresh occurs every five seconds while the menu/detail view is open; it does not retry purchases or contract recovery and does not recreate the UI on each statistics update.

## Validation and acceptance

Automated checks cover bounded motion/circular hit areas, shared evidence validation, persistence/migration/backup recovery, unchanged progression rewards and statistics codec round trips. The opt-in GameTest-source client fixture renders the actual screen at full-HD GUI scale 3 and smaller logical viewports, including detail windows. Its synthetic data and reflection setup are excluded from the delivered mod JAR. These images validate real Minecraft rendering of a fixture, not purchases in a running user's world or a live skin download.

Final source/build/artifact evidence will be recorded after validation. Remaining real-world acceptance: skin/hat rendering for the actual player; English/German readability at the chosen scale; search/category scrolling with the full modpack; click/drag and keyboard navigation; live reward/contract purchases/package delivery and respec with existing profiles; two players sharing world milestones; and world reload/new-world separation. The fixed starter-island NBT and prior machine/recipe behavior remain unchanged.
