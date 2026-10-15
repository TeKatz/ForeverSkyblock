# Persistent skills — menu and research test (0.5.0)

## Scope

Skill points, milestone claims and purchased skills belong to a player profile identified by UUID. In singleplayer, the same profile is shared by all worlds in the **same Minecraft instance**, including a new world created after deleting an old save. This does not synchronize to another installation, PC or unrelated server.

A dedicated server stores its own player profiles outside its world directory, so replacing that server's world retains its progression. It never imports a client's locally earned balance. LAN guests use the hosting instance's profiles. All multiplayer participants need the updated mod.

The fixed island and its stored inventory are unchanged. Players gain profile effects after joining; no edits are applied to the island NBT.

## Open the menu

Press **K** in game. Remap **Open Skills** under **Options → Controls → Key Binds → Forever Skyblock**. A conflicting binding can be changed there. The same key or Escape closes the menu. `/foreverskills` opens it as a command fallback.

The menu adds a **Jetpacks** research tab in 0.5.0; see [JETPACK_RESEARCH.md](JETPACK_RESEARCH.md). The original two tabs remain: **Skills** for purchases and **Tasks** for earning points. Each full-width card combines its effect/objective, rank/reward and action. The available point balance stays at the top; the selected tab has a cyan underline. Scroll the card list to view later entries or long text at larger GUI scales. Descriptions stay inline rather than appearing as large hover tooltips. The world continues running while the menu is open.

Completing a task sends the usual chat message with a green, underlined **+1 SP** reward. Open chat and click that reward to open the Skills menu through `/foreverskills`. Hovering it explains the shortcut; the link does not claim points or buy a skill.

## Reset purchased skills

**Reset skills** opens an explicit confirmation dialog showing the number of points to refund and the cross-world scope. Cancel or Escape leaves the profile unchanged. Confirming sends one reset request; purchases and further resets wait for the server response.

The server refunds all ranks of known skills, clears those purchases, saves the profile and immediately removes their effects. Unspent points remain. Completed tasks remain claimed and cannot award points again, including in another world. Reset can be repeated safely without duplicating refunds, and skills can be bought again with the refunded points. The control is disabled when there are no known purchases, a request is pending or storage is unavailable.

Version 0.4.0 keeps schema 1 and reads existing profiles without migration. Refunds use the server catalog's cost per rank; the existing skill still costs one point. A future change to skill prices needs an explicit refund/migration policy. Unknown stored skill IDs are preserved and not refunded. A refund exceeding the balance limit is refused without changing the profile. Failed storage writes retain the existing purchase and effect.

## Earn and spend the first point

| Entry | Behavior |
| --- | --- |
| Milestone | **First Foundations** (`forever_skyblock:first_foundations`) |
| Trigger | Personally craft cobblestone using four Stone Fragments |
| Reward | **1 skill point**, once per player profile |
| Test skill | **Extended Reach** (`forever_skyblock:extended_reach`) |
| Cost/ranks | 1 point; one rank |
| Effect | **+1 block interaction range**; entity/combat reach is unchanged |

The recipe is checked through the server crafting event, including the four fragment input slots. Picking up cobblestone does not qualify. Fake-player/autocrafter events do not earn the personal milestone. AE2's manual crafting-terminal path uses the standard crafting-event hook; the exact pack interaction still requires client testing.

Repeat crafts, shift crafts, a new world or a restarted game cannot claim First Foundations again for the same profile. Purchases validate the known skill, rank and balance on the server, save the new profile first, then apply the effect and return the authoritative state to the GUI.

The reach modifier is transient and uses a stable ID. It is reapplied on login, respawn and dimension changes, removing the previous modifier before adding it. It does not multiply on repeated refreshes.

## Storage and recovery

Relative to the instance/server working directory:

- Singleplayer/LAN host: `forever_skyblock/progression/local/<player-uuid>.json`.
- Dedicated server: `forever_skyblock/progression/server/<player-uuid>.json`.
- Last validated state: the corresponding `.json.bak` file.

Current profiles use schema version 2 with separate SP/PP balances, paid-perk investments and Core receipts. Schema-1 profiles migrate as documented in [ASCENSION.md](ASCENSION.md). Writes use a flushed temporary file and atomic replacement where supported. A purchase/reward is committed to memory only after a successful save. The last validated profile is backed up before replacement. A corrupted primary file can recover from a valid backup; if neither is readable, purchases/rewards stop and an error is shown instead of silently resetting progression. Unsupported schemas are never overwritten.

Back up the progression directory separately from world saves. Deleting the entire Minecraft instance also deletes these local profiles. Files are loaded on demand, cached during a session and evicted when players leave; they are not polled from disk every tick. No account backend, cross-PC synchronization or cryptographic anti-cheat is included.

## Testing/admin command

Operators with permission level 2 can use:

```mcfunction
/foreverskills grant <player> <points>
```

This administrative command adds 1–1000 points per invocation, up to the profile balance limit. It is available for testing and administration; the normal milestone remains once per profile. Arbitrary client packets cannot grant points.

## Extend later

- Add skill definitions to `SkillCatalog.SKILLS`: ID, point cost, maximum rank, icon and attribute bonus per rank. The existing list/detail/purchase UI picks up those entries automatically.
- Add milestone definitions to `MilestoneCatalog.MILESTONES` and connect a server-side trigger. Deduplication uses the stable milestone ID.
- Add English and German title/description translations.
- New effect kinds can extend the catalog/effect application. Advanced prerequisites, respecs, FTB quest rewards, data-driven definitions and online synchronization are future work.
- Preserve IDs and serialized profile data when adding entries. Unknown stored IDs survive loading even when they have no effect in the current catalog.

## Manual acceptance test

1. Open the menu with K; check the Controls entry and rebind it. With background blur enabled, check readable cards, both tabs, cost/status/action placement and scrolling at multiple GUI scales. Hover the cards: no description tooltip should cover the screen.
2. Process wood and craft cobblestone from four fragments, including a test through the ME Crafting Terminal.
3. Verify one point is awarded; repeat the craft and confirm no second reward. Open chat and click the green +1 SP to open the Skills menu.
4. Buy Extended Reach and confirm the point is spent and a block can be interacted with one block farther away.
5. Close/reopen the menu, die/respawn and change dimensions; confirm the bonus remains exactly +1.
6. Exit to the menu, create a second world in the same instance and check that the purchased skill remains unlocked and the milestone cannot pay again.
7. Restart the game and verify persistence once more.
8. Click Reset skills, then Cancel or Escape: verify no change. Confirm a reset: verify the point returns and reach is removed immediately. Close/reopen, switch worlds and restart; the reset must persist. The task must stay completed. Buy the skill again and confirm the bonus remains exactly +1.

Automated coverage and actual completed tests are recorded in [VALIDATION.md](VALIDATION.md). No manual client test is claimed before it has occurred.

## Ascension update — 0.8.0

TeKatz approved separate Perk Points, earned by personally consuming an endgame Forever Core in an Ascension Chamber. Creative Jetpack research now requires **40 SP + 1 PP**, retaining its EndGame prerequisite. The menu, confirmed refunds and cross-world persistence support both currencies. Legacy SP-only Creative research is refunded and re-locked. See [ASCENSION.md](ASCENSION.md) for initial recipes, automation, migration, claim replay and test scope. The implementation was developed on `feature/ascension-perk-points` over the permanent-tool work in PR #9. TeKatz authorized integrating PRs #9 and #10 into `main` on October 15, 2026.
