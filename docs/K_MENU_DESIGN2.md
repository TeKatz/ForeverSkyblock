# K-menu overhaul — integrated 0.24.0

Integrated into `main` through [PR #16](https://github.com/TeKatz/ForeverSkyblock/pull/16) on October 19, 2026, together with the preceding 0.21–0.23 stack. Developed on `feature/k-menu-overhaul` from validated 0.23.0 head `c08c9a3d65825d6025ba7981e28b2d41578579fd`, this update implements TeKatz's screenshots and requested redesign. No release is published by this integration.

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

The schema-2 world-progress file requires this update's reader. To roll back to a 0.23.x build, restore the pre-update world-progress file with the earlier JAR; its schema-1 reader cannot load the new world state.

Player profile schema remains dynamic 4/5/6. The added authoritative statistics extend the network snapshot to **skills-9**, requiring matching 0.24.0 client/server builds. A bounded read-only refresh occurs every five seconds while the menu/detail view is open; it does not retry purchases or contract recovery and does not recreate the UI on each statistics update.

## Validation and acceptance

Automated checks cover bounded motion/circular hit areas, shared evidence validation, persistence/migration/backup recovery, unchanged progression rewards and statistics codec round trips. The opt-in GameTest-source client fixture renders the actual screen in English/German at full-HD GUI scale 3 and smaller logical viewports, including detail windows and lower scroll regions. It also routes real screen clicks through Challenge/Contract cards, rejects a round node's square corner, opens its center and selects a scrolled category. Its synthetic data and reflection setup are excluded from the delivered mod JAR. These images validate real Minecraft rendering of a fixture, not purchases in a running user's world or a live skin download.

Source **`227c81611f8c8cda719c59106c80d6350a2034a1`** passed Actions run **`39178417187`**: Java 21 build, **78 unit tests**, client startup, actual-client rendering/input, and **90 required GameTests on each server runtime** (default 21.1.249 and pack 21.1.259). All four native input checks passed: challenge card, circle corner/center, contract card and scrolled category selection. Seventeen actual Minecraft fixture captures were produced; representative captures were inspected across English/German and the 640×360 / 480×270 logical GUI sizes. A real modal item-depth bug found in the captures was fixed before final validation.

The fresh `ForeverSkyblock-1.21.1-NeoForge-0.24.0.jar` is **5,262,116 bytes**, from artifact **`12155336788`**, SHA-256 **`06694e5ead79a1d59e010c114663a9bc1a1bf767b960233255e3e0c2ea8098bb`**. All **158 resource files** match the checkout byte-for-byte; the menu/network/world-progress classes are Java 21, `skills-9` is present, and no GameTests or visual-fixture classes are included. The fixed island retains Git blob `4c0e3ada63bb61ef1d109ad110b66b6d359d473f`. The pre-merge evidence and integration-normalization commits change documentation only.

Remaining real-world acceptance: skin/hat rendering for the actual player; English/German readability at the chosen scale; search/category scrolling with the full modpack; click/drag and keyboard navigation; live reward/contract purchases/package delivery and respec with existing profiles; two players sharing world milestones; and world reload/new-world separation. The fixed starter-island NBT and prior machine/recipe behavior remain unchanged.
