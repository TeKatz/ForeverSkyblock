# Agreed design

As of October 15, 2026. TeKatz's explicit decisions are authoritative. Unconfirmed proposals are recorded in [PROGRESSION.md](PROGRESSION.md).

## Confirmed decisions

- **Forever Skyblock** is a separate project from the regular Forever modpack.
- **Minecraft 1.21.1 / NeoForge**, with support for integrating Forever Production Monitor.
- **Expert** difficulty. This supersedes the initial recommendation of Normal.
- A custom starting island created with Skyblock Builder, inspired by the opening of Cuboid Outpost.
- A prebuilt, operational AE2 network: ME Controller, ME Drive, one item storage cell, Crystal Resonance Generator, one Smart Cable and an ME Crafting Terminal. The tested snapshot uses a **1k cell**.
- The island was enlarged after the first build. The placed chest was removed and its contents moved into the ME system.
- Use only the custom `forever.nbt` island template. Skyblock should be the only world type available through the normal selection button.
- **No sieve progression.** Create is excluded in line with TeKatz's modpack preference.
- A custom pack mod with **Condenser Mark I** as its first block. Version 0.1.0 introduced only this block and its item; 0.1.1 updates project identity without changing gameplay.
- Mark I must **not generate iron directly**.
- Remove starter items that enable an immediate water/lava cobblestone generator. This decision still needs to be applied to, or verified in, the final island NBT.
- TeKatz creates and organizes the **FTB quest line manually**. Technical progression and tutorial content can be developed together.
- Crafting recipes are adjusted in game using **Forever Recipe Editor**. Its Skyblock version is maintained on a separate branch.

## Pacing direction

Expert difficulty will be developed through connected crafting steps, new processes and production facilities that scale over time. The prebuilt ME network remains the central workstation. Specific time targets, costs, energy requirements and unlocks have not been agreed yet.

## Open questions

- Mark I's output resource, production interval, interaction and buffer size.
- The complete route to water, lava, stone, initial metals and processing machines.
- Final starter inventory, particularly additional plants and obsidian.
- Core technology mods, other resource paths, Nether/End progression and the final goal.
- DeepCore's role in a void world. Survey behavior and resource distribution must first be checked against its actual code.
- Integration of Production Monitor, AE2 expansion and possible AE2 add-ons.

A complete Expert recipe progression has not been approved yet.
