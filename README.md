# Forever Skyblock

**Forever Skyblock** is an Expert Skyblock modpack for **Minecraft 1.21.1 / NeoForge** built around deliberate progression, meaningful resource loops and long-term automation.

The current integrated `main` baseline is **0.26.4**. It includes the rebuilt K-menu and K-menu Studio, the early Lava/Obsidian progression through the Crucible, and the first configurable Condenser Mark II automation upgrades. The current implementation boundary is documented in [docs/CURRENT_STATE.md](docs/CURRENT_STATE.md).

## Project direction

Forever Skyblock is designed as a world that is built up piece by piece rather than a checklist of unrelated mod recipes. The pack starts with very little. Early manual processes teach the resource loop; later machines automate those same loops instead of replacing them with unrelated systems.

The pack deliberately avoids a traditional sieve-centered economy. Resources should come from processes that fit the progression, remain useful for more than one step, and eventually become automatable.

The broad direction is:

**Foundations → renewable basic resources → water and farming → first metals → Lava/Obsidian → configurable automation → major technology mods → dimensions and bosses → late-game industry → long-term meta progression**

## Current early-game progression

The implemented material route now reaches renewable wood/crops, deterministic Iron and the first Lava/Obsidian loop:

`Wood → Stone Fragments → Stone Meal / Dirt → Ice / Water → Farm → Mud / Packed Mud → Washing Basin → Raw Iron → Iron → Condenser Mark II + Garden Chamber → Crucible → Lava / Obsidian`

Important current systems include:

- **Condenser Mark I / II** for early material processing, with job-aware block and GUI animations.
- **Mixing Chamber** for Dirt, Mud, Packed Mud and other two-input processes.
- **Freezing Chamber** for Ice and as the adjacent cooling module used by the Crucible for Obsidian.
- **Washing Basin** as the deterministic first Raw Iron route.
- **Garden Chamber** for retained Oak Sapling and Wheat Seed production with separate primary/secondary harvest ports.
- **Crucible** with a 4,000 mB internal Lava buffer, visible fill level, JEI recipes, Jade heat-tier information and hopper/cable support.

### Crucible heat progression

The Crucible detects its heat source below it. The current heat tiers are:

1. **Torch** — intentionally very slow early access.
2. **Campfire** — practical early upgrade.
3. **Lava** — stronger mid-step heat.
4. **Mekanism Superheating Element** — high-end heat source.

The current material rates are:

- **8 Cobblestone → 1,000 mB Lava**
- **40 Stone Fragments → 1,000 mB Lava**

An adjacent Fluid Tank enables Lava routing. An adjacent Freezing Chamber enables Obsidian production. If both are detected, the Crucible GUI lets the player choose the active route. The Mekanism Basic Fluid Tank recipe is intentionally redstone-free so Redstone can be introduced later as its own progression step.

## Condenser Mark II automation

Mark II is now the first configurable production machine rather than only a faster Mark I.

It has **3 upgrade capacity** and three physical upgrade slots. The currently integrated cards are:

| Upgrade | Effect | Limit / cost |
| --- | --- | --- |
| Processing Speed Card | 20% / 35% / 50% shorter normal processing time | up to 3, 1 capacity each |
| Garden Accelerator Card | +25% Garden speed per card | up to 2, 1 capacity each |
| Buffer Expansion Card | adds hidden standard-stack input/output buffering | up to 2, 1 capacity each |
| Batch Processing Card | processes two normal recipe runs per cycle when possible | max 1, costs 2 capacity |

Each Mark II output also has an independent policy:

- **Block** — stop when the output and its buffer are full.
- **Void Excess** — keep everything that fits and destroy only overflow.
- **Void All** — destroy all production for that output.

Garden primary and secondary harvest outputs can be configured independently. Upgrade timing is reflected in the machine and GUI animations.

## K-menu and persistent progression

The K-menu is the central interface for the persistent progression layer: Challenges, Contracts, Shop/Tiers, Research, Perks, world purchases and player statistics.

The **K-menu Studio is integrated into `main`**. Authorized users can edit layouts, quests/chapters, Shop nodes/packages and supported Contracts, preview changes, validate/publish server-owned documents, keep drafts, reconcile conflicts and export configuration. See [docs/K_MENU_EDITOR.md](docs/K_MENU_EDITOR.md).

The existing progression boundary remains intentionally conservative: completed objectives, purchases and recovery are server-owned, while unsupported arbitrary scripts or new gameplay effects still require code.

## Design goals

### Meaningful resources

Important materials should remain useful as progression expands instead of becoming dead ends after one recipe.

### Manual first, automated later

Early manual work is acceptable as teaching and progression. Permanent repetitive chores are not. Once a stage is mastered, later infrastructure should be able to automate it.

### Expert without unnecessary punishment

Difficulty should come from building production chains, understanding dependencies and allocating resources—not from random waiting or keeping obsolete manual chores alive solely to add playtime.

### Connected systems

Farming, resource processing, technology, exploration, combat and meta progression should reinforce one another rather than form isolated mod branches.

## Current project status

- **Main mod version:** `0.26.4`
- **Minecraft:** `1.21.1`
- **Java:** `21`
- **Default development NeoForge:** `21.1.249`
- **Full pack-runtime validation:** `NeoForge 21.1.259`
- **Progression protocol:** `skills-9` plus the integrated K-menu editor channel
- **Persistence:** existing player schema 4/5/6 and world-progress schema 2 remain compatible
- **Repository:** public, active development
- **Release status:** repository integration does not by itself imply a published pack release

The 0.26.4 gameplay head was validated with the Java build, client smoke test, K-menu visual fixtures, dedicated-server regression suite and the full NeoForge 21.1.259 pack-runtime integration suite before merge.

## CI behavior

GitHub Actions uses the full validation chain for `main` and pull requests:

1. Java build and tests
2. headless NeoForge client smoke test
3. K-menu visual fixtures
4. dedicated-server regression GameTests
5. full pack-runtime integration on NeoForge 21.1.259

Feature branches are validated through pull requests rather than duplicate push + PR runs. Stale runs are cancelled, documentation-only changes skip the heavyweight mod workflow, jobs have a timeout and CI artifacts use reduced retention.

## Repository structure

| Path | Purpose |
| --- | --- |
| `mod/` | Standalone Gradle project for the Forever Skyblock mod |
| `pack/` | Pack-owned configuration, data and fixed island files |
| `docs/` | Design, progression, validation and system documentation |
| `AGENTS.md` | Repository working rules |

The fixed starting island is stored at `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` and remains protected pack state unless a deliberate revision is made.

The quest book is maintained separately from this repository. Mod-list inventory files are development/runtime snapshots rather than a final launcher lockfile.

## Build

Install Java 21 and run from `mod/`:

```sh
./gradlew build
```

Windows:

```bat
gradlew.bat build
```

The production JAR is written to `mod/build/libs/`.

## Documentation

Start with:

- [Current integrated state](docs/CURRENT_STATE.md)
- [Documentation map](docs/README.md)
- [K-menu Studio](docs/K_MENU_EDITOR.md)
- [Material processing and automation](docs/MATERIAL_PROCESSING.md)
- [Design direction](docs/DESIGN.md)
- [Validation history](docs/VALIDATION.md)
- [Version history](docs/CHANGELOG.md)
