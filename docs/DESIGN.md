# Agreed design

As of October 15, 2026. TeKatz's explicit decisions are authoritative. Unconfirmed proposals are recorded in [PROGRESSION.md](PROGRESSION.md).

## Confirmed decisions

- **Forever Skyblock** is a separate project from the regular Forever modpack.
- **Minecraft 1.21.1 / NeoForge 21.1.259** is the current pack runtime. TeKatz will report loader changes. The experimental mod list may change independently; Forever Production Monitor 5.0.1 is present in the supplied snapshot.
- **Expert** difficulty. This supersedes the initial recommendation of Normal.
- A custom starting island created with Skyblock Builder, inspired by the opening of Cuboid Outpost.
- A prebuilt, operational AE2 network: ME Controller, ME Drive, one item storage cell, Crystal Resonance Generator, one Smart Cable and an ME Crafting Terminal. The tested snapshot uses a **1k cell**.
- The island was enlarged after the first build. The placed chest was removed and its contents moved into the ME system.
- The supplied **`forever_me_start.nbt` island is final**. Preserve its layout and inventory unless TeKatz explicitly requests a revision. It is the only selected template, with spawn `[8, 4, 5]` facing east. Skyblock is the only type available through the normal selection button.
- **No sieve progression.** Create is excluded in line with TeKatz's modpack preference.
- A custom pack mod with **Condenser Mark I** as its first block. Version 0.2.0 adds wood conversion, a GUI, automation and JEI integration.
- Mark I converts one log into **four Stone Fragments**, or one plank into **one Stone Fragment**, without electricity. Four fragments craft into one cobblestone in a 2×2 recipe.
- Mark I must **not generate iron directly**.
- The fixed NBT stores **eight bread and one wheat seed** in one 1k ME cell. Spare saplings and dirt were removed. An emergency sapling quest is a possible later addition.
- No ice or lava bucket is stored in the fixed starter inventory, preventing an immediate water/lava cobblestone generator.
- TeKatz creates and organizes the **FTB quest line manually**. Technical progression and tutorial content can be developed together.
- Crafting recipes are adjusted in game using **Forever Recipe Editor**. Its Skyblock version is maintained on a separate branch.

## Persistent progression — initial implementation requested

- Add a player-wide skill point/shop system shared across singleplayer worlds in the same instance.
- Version 0.3.0 provides a dedicated GUI, remappable K shortcut, one test milestone and one test skill.
- First Foundations awards one point once per profile when personally crafting cobblestone from four Stone Fragments.
- Extended Reach costs one point and adds one block interaction range. The island NBT is unchanged.
- Dedicated servers own separate profiles; online/cross-PC synchronization and FTB quest integration are not part of this first version.
- Version 0.4.0 reorganizes the menu into skill/task cards, adds confirmed refunds without clearing task claims and links the green chat point reward to the menu.
- Version 0.7.0 implements the approved permanent-tool recipe handoff for the 24 Vanilla pickaxes, axes, shovels and hoes; other equipment remains a separate design pass.

## Pacing direction

Expert difficulty will be developed through connected crafting steps, new processes and production facilities that scale over time. The prebuilt ME network remains the central workstation. Specific time targets, costs, energy requirements and unlocks have not been agreed yet.

## Open questions

- Final balancing of the initial eight-second machine cycle; later condenser tiers remain optional.
- The complete route to water, lava, stone, initial metals and processing machines.
- Periodic updates to the experimental mod snapshot and final progression roles for installed mods.
- Core technology mods, other resource paths, Nether/End progression and the final goal.
- DeepCore's role in a void world. Survey behavior and resource distribution must first be checked against its actual code.
- Integration of Production Monitor, AE2 expansion and possible AE2 add-ons.

A complete Expert recipe progression has not been approved yet.

## Approved condenser mixing — version 0.6.0

TeKatz requested the reusable Mortar, Stone Meal, an adjacent Mixing Chamber that exposes one additional input, two-input dirt processing, and persistent ghost filters editable using inventory items or JEI. Shared block faces count; diagonals do not. Removing the module preserves the hidden input and filter. No changes to the fixed island or skills are authorized by this feature. Details: [CONDENSER_MIXING.md](CONDENSER_MIXING.md).

## Approved follow-up — version 0.6.1

Fix Mixing Chamber z-fighting and disappearing adjacent faces. Add a JEI plus-button transfer from player inventory with unavailable recipes disabled. Disable Vanilla's initial in-world tutorial sequence. Preserve the existing progression, filters, starter island and optional JEI support.

## Approved permanent tools — version 0.7.0

Implement the supplied [tool handoff](TOOL_RECIPE_HANDOFF.md): exact 20 tiered crafting replacements, four reinforced-Netherite smithing replacements and scoped infinite durability. Gold branches from Stone; Diamond requires Iron plus Obsidian. The iron/water/lava/pre-Diamond Obsidian acquisition routes remain open. See [TOOL_PROGRESSION.md](TOOL_PROGRESSION.md).

## Ascension update — 0.8.0

TeKatz approved separate Perk Points, earned by personally consuming an endgame Forever Core in an Ascension Chamber. Creative Jetpack research now requires **40 SP + 1 PP**, retaining its EndGame prerequisite. The menu, confirmed refunds and cross-world persistence support both currencies. Legacy SP-only Creative research is refunded and re-locked. See [ASCENSION.md](ASCENSION.md) for initial recipes, automation, migration, claim replay and test scope. The implementation was developed on `feature/ascension-perk-points` over the permanent-tool work in PR #9. TeKatz authorized integrating PRs #9 and #10 into `main` on October 15, 2026.


## Persistent shop/challenge expansion — phased instruction (October 16, 2026)

TeKatz supplied the complete Shop/Challenges handoff and requested following it. Architecture and a bounded UI foundation are developed first on a separate branch, preserving all existing features and the final island. Unique profile challenges are the primary SP source; a small mastery-gated repeatable set will provide slower renewable SP. Shop tiers must be independent of currency and permanent across worlds/respecs. Future world products need a stable world identity and safe recoverable transactions. No actual valuable package contents, unresolved production routes, contract quantities or higher-tier requirements are finalized by this instruction. See [META_PROGRESSION.md](META_PROGRESSION.md) for source findings, proposed schema, trigger ownership, transaction limits and phased validation.
