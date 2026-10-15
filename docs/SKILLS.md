# Persistent skills — first version (0.3.1)

## Scope

Skill points, milestone claims and purchased skills belong to a player profile identified by UUID. In singleplayer, the same profile is shared by all worlds in the **same Minecraft instance**, including a new world created after deleting an old save. This does not synchronize to another installation, PC or unrelated server.

A dedicated server stores its own player profiles outside its world directory, so replacing that server's world retains its progression. It never imports a client's locally earned balance. LAN guests use the hosting instance's profiles. All multiplayer participants need the updated mod.

The fixed island and its stored inventory are unchanged. Players gain profile effects after joining; no edits are applied to the island NBT.

## Open the menu

Press **K** in game. Remap **Open Skills** under **Options → Controls → Key Binds → Forever Skyblock**. A conflicting binding can be changed there. The same key or Escape closes the menu. `/foreverskills` opens it as a command fallback.

The custom screen includes a point balance, scrollable Skills and Milestones lists, selected-entry details, current rank/reward, purchase state and profile-scope label. Descriptions wrap inside the detail panel; use the mouse wheel over that text when a scrollbar appears. Hovering the panel does not create a description tooltip. The world continues running while the menu is open.

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

Profiles use schema version 1. Writes use a flushed temporary file and atomic replacement where supported. A purchase/reward is committed to memory only after a successful save. The last validated profile is backed up before replacement. A corrupted primary file can recover from a valid backup; if neither is readable, purchases/rewards stop and an error is shown instead of silently resetting progression. Unsupported schemas are never overwritten.

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

1. Open the menu with K; check the Controls entry and rebind it. With background blur enabled, confirm the panel, text, icons and buttons stay sharp. Hover the details and rows: no description tooltip should cover the screen. Check both tabs at multiple GUI scales; scroll the details when a scrollbar appears.
2. Process wood and craft cobblestone from four fragments, including a test through the ME Crafting Terminal.
3. Verify one point is awarded; repeat the craft and confirm no second reward.
4. Buy Extended Reach and confirm the point is spent and a block can be interacted with one block farther away.
5. Close/reopen the menu, die/respawn and change dimensions; confirm the bonus remains exactly +1.
6. Exit to the menu, create a second world in the same instance and check that the purchased skill remains unlocked and the milestone cannot pay again.
7. Restart the game and verify persistence once more.

Automated coverage and actual completed tests are recorded in [VALIDATION.md](VALIDATION.md). No manual client test is claimed before it has occurred.
