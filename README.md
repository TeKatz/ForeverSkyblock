# Forever Skyblock

Development branch `feature/machine-animation-overhaul` adds the **0.21.1 machine animation update** over the integrated 0.20.1 baseline. See [animation behavior and validation](docs/MACHINE_ANIMATIONS.md).

**Forever Skyblock** is an Expert Skyblock modpack for **Minecraft 1.21.1 / NeoForge** built around deliberate progression, meaningful resource loops and long-term automation.

The pack starts with very little, but the goal is not simply to survive or rush toward a familiar endgame. Progression is designed as a connected chain in which each new resource, machine and system creates the tools needed to solve the next problem. Early manual work gradually turns into automation, automation opens access to more advanced technology, and previously important materials continue to have a reason to exist instead of becoming disposable stepping stones.

## The idea behind the pack

Forever Skyblock is intended to feel like a world that is **built up piece by piece** rather than a checklist of unrelated mod recipes.

The early game focuses on creating a stable economy from extremely limited resources. Wood, stone, soil, water, farming and the first metals are not isolated milestones; they form a dependency network that gradually becomes self-sustaining. Custom machines bridge gaps that Vanilla and the included technology mods do not naturally cover in Skyblock, while existing Minecraft mechanics are used whenever they fit the progression cleanly.

The pack deliberately avoids a traditional sieve-based resource loop. Resources should come from processes that make sense within the progression and that can later be automated. Large technology mods are not meant to replace the early game immediately; they become part of the progression once the infrastructure required to support them has been earned.

## Design goals

### Meaningful resources

Items should remain useful for as long as they reasonably can. A material introduced early should ideally participate in several systems, serve as infrastructure for later machines, or gain a new role as progression expands.

This does not mean forcing every Vanilla item into the main progression. The goal is to avoid custom resources and important progression materials becoming dead ends after a single recipe.

### Manual first, automated later

Manual or semi-manual production is acceptable when a resource is first discovered. It gives the resource weight and teaches the process behind it.

Permanent manual chores are not the goal.

Once a progression stage has been mastered, later machinery should be able to automate the work that stage introduced. The long-term target is a factory where fundamental production chains can run continuously without requiring repetitive player interaction.

### Progression instead of recipe spam

New machines should exist because they solve a progression problem, not simply because another processing block can be added.

Each major machine tier should have a clear purpose. Early machines establish basic materials and environmental resources. Later tiers improve throughput, remove manual bottlenecks and introduce increasingly sophisticated automation.

### Connected systems

Farming, resource processing, technology, exploration, combat and meta progression are intended to reinforce one another rather than exist as independent tracks.

The custom Condenser family and its chambers are one example of this approach: the system begins as primitive material processing and gradually develops into a broader automation platform. The Washing Basin introduces the first deterministic metal route, while the Garden Chamber closes the early wood economy once the corresponding automation tier has been reached.

### Expert without unnecessary punishment

Forever Skyblock aims for an Expert-style progression where milestones matter and recipes are intentionally gated, but inconvenience alone is not treated as difficulty.

Waiting for random events, repeating obsolete manual tasks or creating artificial grind solely to extend playtime is avoided where possible. Difficulty should come from building the next production chain, understanding dependencies, allocating resources and expanding infrastructure.

## The intended journey

The overall progression is planned as a gradual expansion from a tiny controlled starting environment into a highly automated technological Skyblock world.

The broad direction is:

**Foundations → renewable basic resources → water and farming → first metals → primitive automation → advanced resource processing → major technology mods → dimensions and bosses → late-game industry → long-term meta progression**

The exact path is intentionally developed and tested in stages rather than locking the entire pack into a rigid plan too early. New systems are expected to build on existing ones, and progression can be rebalanced when actual gameplay exposes weak loops, excessive grind or automation gaps.

## Automation as a core pillar

Automation is not reserved for the endgame.

A central design rule is that important production chains should eventually become fully automatable. If a resource is required indefinitely, the player should not still be gathering or crafting its basic components by hand dozens of hours later.

Automation itself is also progression. Early systems may only automate individual processing steps. Later machines close complete loops, remove dependencies on manual tools and allow several production chains to feed one another.

The long-term vision is a base that feels increasingly alive: machines processing resources, farms sustaining themselves, storage and crafting networks coordinating production, and technology gradually replacing the primitive infrastructure that made it possible.

## Meta progression

Forever Skyblock also contains a persistent progression layer outside normal item acquisition.

Challenges, repeatable contracts, research, Skill Points, Perk Points and world-level upgrades provide longer-term objectives alongside the physical factory progression. These systems are intended to reward milestones and offer meaningful choices without replacing the core survival and automation game.

The K-menu acts as the central interface for this progression layer, covering challenges, research, world purchases, perks and player progression.

## Future vision

The current early game establishes the foundations, first deterministic Iron production and the first major automation tier. The next stages are expected to expand from that foundation rather than abandon it.

Future development is intended to include:

- broader automated farming and renewable resource production;
- Lava and Obsidian progression;
- additional metals and material-processing tiers;
- deeper integration of AE2, Mekanism, Powah, Mystical Agriculture and other selected mods;
- meaningful dimension and boss progression;
- additional machine tiers and specialized automation systems;
- expansion of the Challenge, Contract, Research, Perk and World Shop systems;
- a late game where the different technological and progression systems converge instead of existing as isolated mod branches.

The final goal is a long-form Expert Skyblock experience where the player can look back at the first tree, first machine and first patch of farmland and still see how those systems evolved into the automated world around them.

## Current project status

- **Main mod version:** `0.20.1`
- **Minecraft:** 1.21.1
- **Java:** 21
- **Pack runtime:** NeoForge 21.1.259
- **Progression protocol:** `skills-8`
- **Current integrated baseline:** material processing and Garden automation through 0.20.1
- **Release status:** active development; repository integration does not imply a public release

The current implemented behavior and exact integration boundary are documented in [docs/CURRENT_STATE.md](docs/CURRENT_STATE.md). The full documentation map is available in [docs/README.md](docs/README.md).

## Repository structure

| Path | Purpose |
| --- | --- |
| `mod/` | Standalone Gradle project for the Forever Skyblock mod |
| `pack/` | Pack-owned configuration, data and fixed island files |
| `docs/` | Design, progression, validation and system documentation |
| `AGENTS.md` | Repository working rules |

The fixed starting island is stored at `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` and is treated as protected pack state unless a deliberate revision is made.

The quest book is maintained separately from this repository. Mod-list inventory files are development/runtime snapshots rather than a final launcher lockfile.

## Build and test

Install Java 21, then run from `mod/`:

```sh
./gradlew build
```

Windows:

```bat
gradlew.bat build
```

The production JAR is written to `mod/build/libs/`.

GitHub Actions additionally performs a headless NeoForge client smoke test, dedicated regression GameTests and a full NeoForge 21.1.259 pack-runtime integration test.

## Documentation

Detailed implementation information intentionally lives outside this README. Useful starting points include:

- [Current integrated state](docs/CURRENT_STATE.md)
- [Design direction](docs/DESIGN.md)
- [Material processing and automation](docs/MATERIAL_PROCESSING.md)
- [Progression planning](docs/PROGRESSION.md)
- [Validation history](docs/VALIDATION.md)
- [Version history](docs/CHANGELOG.md)
