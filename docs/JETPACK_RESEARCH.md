# Iron Jetpacks research test — 0.5.0

This is a development test of profile-wide mod restrictions, not final Forever balancing. The implementation originated on `feature/iron-jetpacks-research` and is included in the current 0.6.1 source. Iron Jetpacks is optional; integration is built against 8.0.11 for Minecraft 1.21.1. Automated integration runs also load AE2 19.2.18 and GuideME 21.1.19 on the pack's NeoForge 21.1.259. TeKatz's updated `Modliste.txt` now lists Iron Jetpacks 8.0.11 and Iron Furnaces 4.3.2. The current inventory reflects that supplied list; the earlier runtime log remains archived separately.

## Menu and prices

Open Forever Skills with K and select **Jetpacks**. Four compact ordered cards show stage, cost and unlock/prerequisite state. All four fit in the normal 344-pixel-high panel; smaller screens scroll. Hover a card action button for a bounded, automatically wrapped description with actual registered variants and prerequisite. Buying a stage spends profile points and persists across worlds/restarts.

| Research | Additional cost | Included content |
| --- | ---: | --- |
| Beginner | 5 SP | Non-creative material tiers 0–1; straps and basic coils |
| Mid-Game | 10 SP | Non-creative tiers 2–3; advanced coils |
| End-Game | 20 SP | Non-creative tiers 4 and higher; elite/ultimate coils |
| Creative | 40 SP + 1 PP | Creative variants and their components |

Stages must be bought in order; total cost is 75 SP + 1 PP. Typed cells, thrusters and capacitors follow their jetpack definition. The actual registered tier and creative flag determine the stage, including customized jetpacks. Invalid/missing variants fail closed at Creative. Missing Iron Jetpacks disables research purchases.

## Temporary test points

The clearly marked **TEST: +10 SP** button grants exactly ten points per successful click, saved server-side. It can be used repeatedly. Singleplayer owners can use it even without enabling cheats; dedicated-server operators need permission level 2. Other players see a disabled button. The packet cannot choose another player or supply an amount. This developer feature must be removed before a public production pack release.

For repeatable testing, the existing confirmed reset also refunds and clears jetpack research. It restores JEI restrictions and personal flight restrictions; task claims remain completed. Permanent non-refundable research is a future design decision, not silently introduced here.

## Actual restrictions

- Vanilla shaped and shapeless crafting recipes producing Iron Jetpacks items check research. This includes generated component/upgrade recipes and replacement recipes using these vanilla recipe classes.
- Vanilla crafting-result pickup and AE2's manual crafting action also validate the actual player. Existing/cached output cannot bypass a reset through these paths.
- Cached AE2 crafting patterns check the research again when assembling output.
- Iron Jetpacks' equipped-jetpack lookup is filtered on client and server. This covers its chest and Curios flight paths, including an already-owned or handed-over jetpack. Creative-mode players follow the same research requirement; administrator commands can still create items.
- Standard JEI runtime APIs hide component-specific Iron Jetpacks items and their crafting recipes until their stage is unlocked. The same runtime restores them after buying, and hides them after reset; no restart is intended. JEI is optional and is not the authority for crafting/flight.

Playerless automation has no player profile. For this test it uses the highest research of players currently connected to that server, unless a crafting-player context is supplied. It stops producing locked items when no connected player has the necessary research. Manual crafting and personal flight remain checked against their own player. This shared automation policy is provisional; team/owner research and offline dedicated-server automation need a separate design decision.

This does not dynamically unload Iron Jetpacks, remove inventory items, block operator commands, or promise coverage of arbitrary custom crafting machines/recipe serializers. New integrations must preserve the server-side check before inputs are consumed. Iron Furnaces has no restriction or recipe changes in this update.

## Creative crafting recipe

Iron Jetpacks does not generate a recipe for its default Creative variant. A conditional test recipe is added here: four netherite blocks, two nether stars, two elytra and an Emerald Jetpack produce one Creative Jetpack. It requires the Creative research stage. It is a normal shaped recipe visible in JEI after unlock. It is not final Expert balancing and assumes the default Emerald/Creative definitions remain available.

## Manual acceptance

1. With no jetpack research, search `@ironjetpacks` in JEI: no Iron Jetpacks entries or their crafting recipes should be available. Manually arrange a strap/jetpack recipe; it must not produce a craftable result. Test the ME Crafting Terminal as well.
2. Open K → Jetpacks. Click TEST: +10 SP. Buy Beginner for five points. Only Beginner items/recipes should appear, without reconnecting.
3. Try Mid-Game without enough points, and later stages without their prerequisite. Both must remain unavailable. Grant more test points and buy each stage in order; check the newly visible variants and components.
4. Craft and use a permitted jetpack; try a higher-stage jetpack via an administrator-given item. Test chest and Curios flight.
5. Redeem a crafted Forever Core for one PP, buy Creative, then verify its test crafting recipe and creative variant are visible and usable.
6. Cancel a reset, then confirm it. Check full point refund, no repeated task payout, JEI hiding and an already-equipped jetpack losing flight eligibility.
7. Change worlds and restart the game. The same profile must retain points and research/reset state.
8. On a dedicated server, check operator-only test points, separate personal flight permissions and the documented shared automation policy.

Automated build/test evidence is recorded in [VALIDATION.md](VALIDATION.md). Headless tests do not establish GUI readability or JEI client behavior.

## Ascension update — 0.8.0

TeKatz approved separate Perk Points, earned by personally consuming an endgame Forever Core in an Ascension Chamber. Creative Jetpack research now requires **40 SP + 1 PP**, retaining its EndGame prerequisite. The menu, confirmed refunds and cross-world persistence support both currencies. Legacy SP-only Creative research is refunded and re-locked. See [ASCENSION.md](ASCENSION.md) for initial recipes, automation, migration, claim replay and test scope. This feature is on `feature/ascension-perk-points`, based on the still-open permanent-tool PR #9; no main merge is implied.
