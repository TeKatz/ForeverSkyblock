# Forever Skyblock — Infinite-Durability Tool Recipe Overhaul
## Work Handoff / Implementation Plan

**Project:** Forever Skyblock  
**Repository:** `TeKatz/ForeverSkyblock`  
**Target runtime:** Minecraft 1.21.1 / NeoForge 21.1.259  
**Pack style:** Skyblock Tech Expert  
**Status:** Design proposal for implementation; do not merge to `main` unless explicitly requested.

---

## 1. Goal

Replace the existing vanilla crafting recipes for Minecraft tools with a new progression-oriented recipe system designed around the following pack rule:

> **Tools have infinite durability.**

Because tools are no longer consumables, their recipes should represent a **permanent progression investment** instead of cheap disposable equipment.

The new system should:

- Remove the vanilla crafting recipes for the relevant tool set.
- Add new, more expensive recipes.
- Use the existing Forever Skyblock progression instead of arbitrary expensive ingredients.
- Make each tool tier feel like an unlock.
- Encourage upgrading the same tool through the tiers.
- Avoid meaningless grind such as requiring full material blocks for every tool.
- Avoid tying fundamental vanilla tools too tightly to optional tech mods whose presence may still change.
- Preserve a clear visual crafting-language across tiers.
- Support JEI correctly.
- Fit the existing early-game Condenser / Stone Fragment / Stone Meal progression.

---

## 2. Confirmed repository context

The current Forever Skyblock progression already establishes:

- Custom Skyblock start with a prebuilt AE2 network.
- Condenser Mark I as the first progression machine.
- Logs convert into Stone Fragments.
- Planks convert into Stone Fragments.
- 4 Stone Fragments craft into Cobblestone.
- Mortar exists as a reusable item.
- Stone Meal exists and is made from Stone Fragments using the Mortar.
- Mixing Chamber / Stone Meal are part of the early resource progression.
- No sieve progression.
- Create is excluded.
- Direct early iron generation from Condenser Mark I is intentionally not part of the progression.
- The complete metal/water/lava progression is still under development.
- Recipe Editor is the intended system for custom crafting recipe changes.
- The current mod list is experimental and subject to change.

Important design consequence:

**Base vanilla tools should primarily depend on Forever Skyblock + Vanilla resources, not on Mekanism/Powah/Industrial Foregoing components.**

This keeps the fundamental tool chain stable even if the later technology stack changes.

---

## 3. Core design principle

Since tools have infinite durability, every tool is effectively a **permanent unlock**.

The crafting cost should therefore be higher than vanilla, but should still feel intentional rather than grindy.

Recommended design:

- More head material than vanilla.
- Previous-tier tool is consumed as the "core" of the next tier.
- Each tier adds one thematic reinforcement/gating item.
- Gold remains an optional sidegrade rather than a mandatory progression tier.
- Netherite remains a Smithing Table upgrade so item data, enchantments, names, etc. continue to transfer naturally.

---

## 4. Shared recipe language

Use the following generalized patterns.

### Pickaxe

```text
X X X
X T X
. C .
```

### Axe

```text
X X .
X T X
. C .
```

### Shovel

```text
. X .
X T X
. C .
```

### Hoe

```text
X X .
. T X
. C .
```

Where:

- `X` = main tier material
- `T` = previous-tier tool or initial handle/core
- `C` = reinforcement / progression-gating ingredient
- `.` = empty slot

Material usage per recipe:

| Tool | Main material X |
|---|---:|
| Pickaxe | 5 |
| Axe | 4 |
| Shovel | 3 |
| Hoe | 3 |

A complete 4-tool set therefore requires:

**15 units of the primary tier material**

This is intentionally higher than vanilla's 9 units for the same set, which is justified by permanent durability.

---

# 5. Tier 1 — Wooden Tools

## Intent

The first real tool should already require interaction with the Condenser progression.

Instead of crafting fully functional permanent tools directly from wood, require Stone Fragments as a reinforcement.

This makes the first tool unlock part of the existing:

**Tree → Condenser → Stone Fragment → Tool**

loop.

## Ingredients

- `P` = any plank (`#minecraft:planks`)
- `S` = Stick
- `F` = Forever Skyblock Stone Fragment

### Wooden Pickaxe

```text
P P P
P S P
. F .
```

### Wooden Axe

```text
P P .
P S P
. F .
```

### Wooden Shovel

```text
. P .
P S P
. F .
```

### Wooden Hoe

```text
P P .
. S P
. F .
```

## Why this works

- Uses a resource already central to Forever Skyblock.
- Prevents instant permanent tools from only a few planks.
- Forces the player to use the Condenser very early.
- Creates an actual resource decision between:
  - building with wood,
  - processing wood into Stone Fragments,
  - crafting tools.

---

# 6. Tier 2 — Stone Tools

## Intent

Stone tools should not use Cobblestone directly.

Instead, they should use:

- Stone Fragments
- Stone Meal
- previous-tier Wooden Tool

This makes Stone Meal relevant outside dirt generation and integrates the Mortar system into tool progression.

## Ingredients

- `F` = Stone Fragment
- `W` = matching Wooden Tool
- `M` = Stone Meal

### Stone Pickaxe

```text
F F F
F W F
. M .
```

### Stone Axe

```text
F F .
F W F
. M .
```

### Stone Shovel

```text
. F .
F W F
. M .
```

### Stone Hoe

```text
F F .
. W F
. M .
```

## Effective cost

Current Stone Meal production itself consumes Stone Fragments via the reusable Mortar.

Therefore each Stone Tool consumes:

- tool-specific Stone Fragments
- 1 Stone Meal
- matching Wooden Tool

Example:

**Stone Pickaxe = 5 direct Stone Fragments + 1 Stone Meal + Wooden Pickaxe**

If Stone Meal remains at 4 Stone Fragments per unit, the effective fragment cost of one Stone Pickaxe is 9 fragments plus the prior tool.

This is desirable for infinite durability.

---

# 7. Tier 3 — Iron Tools

## Intent

Iron should be the first major metal-tool milestone.

The recipe should feel structurally stronger than Stone and consume the previous tier.

Use a Chain as the reinforcement component.

## Ingredients

- `I` = Iron Ingot
- `S` = matching Stone Tool
- `C` = Chain

### Iron Pickaxe

```text
I I I
I S I
. C .
```

### Iron Axe

```text
I I .
I S I
. C .
```

### Iron Shovel

```text
. I .
I S I
. C .
```

### Iron Hoe

```text
I I .
. S I
. C .
```

## Cost

| Tool | Iron Ingots | Chain | Previous Tool |
|---|---:|---:|---|
| Iron Pickaxe | 5 | 1 | Stone Pickaxe |
| Iron Axe | 4 | 1 | Stone Axe |
| Iron Shovel | 3 | 1 | Stone Shovel |
| Iron Hoe | 3 | 1 | Stone Hoe |

A full Iron set therefore costs:

- 15 Iron Ingots
- 4 Chains
- complete Stone tool set

This makes the first metal tools a meaningful progression milestone without becoming excessive.

---

# 8. Gold Tools — Optional Sidegrade

## Intent

Gold should not be a mandatory step between Iron and Diamond.

With infinite durability, Gold tools become genuinely useful because their high mining speed is no longer offset by terrible durability.

This creates a natural sidegrade:

**Stone → Gold**

rather than:

**Iron → Gold → Diamond**

Gold becomes a specialized speed tool rather than a strict upgrade tier.

## Ingredients

- `G` = Gold Ingot
- `S` = matching Stone Tool
- `R` = Redstone Dust

### Gold Pickaxe

```text
G G G
G S G
. R .
```

### Gold Axe

```text
G G .
G S G
. R .
```

### Gold Shovel

```text
. G .
G S G
. R .
```

### Gold Hoe

```text
G G .
. S G
. R .
```

## Purpose

Gold tools become useful for:

- fast mining of blocks within Gold's harvest capability
- building
- dirt/sand removal
- wood clearing
- large Ultimine operations where harvest tier is irrelevant

Gold should remain optional.

It should not gate Diamond.

---

# 9. Tier 4 — Diamond Tools

## Intent

Diamond tools should be gated not only by Diamonds but also by the player's ability to produce Obsidian.

This ties Diamond progression to the later water/lava infrastructure.

The repository currently leaves water/lava progression open, so Obsidian is a useful future gate that can naturally slot into that progression once finalized.

## Ingredients

- `D` = Diamond
- `I` = matching Iron Tool
- `O` = Obsidian

### Diamond Pickaxe

```text
D D D
D I D
. O .
```

### Diamond Axe

```text
D D .
D I D
. O .
```

### Diamond Shovel

```text
. D .
D I D
. O .
```

### Diamond Hoe

```text
D D .
. I D
. O .
```

## Cost

| Tool | Diamonds | Obsidian | Previous Tool |
|---|---:|---:|---|
| Diamond Pickaxe | 5 | 1 | Iron Pickaxe |
| Diamond Axe | 4 | 1 | Iron Axe |
| Diamond Shovel | 3 | 1 | Iron Shovel |
| Diamond Hoe | 3 | 1 | Iron Hoe |

A complete Diamond tool set requires:

- 15 Diamonds
- 4 Obsidian
- complete Iron tool set

This is a substantial but reasonable permanent-tool investment.

---

# 10. Tier 5 — Netherite Tools

## Intent

Netherite should remain a Smithing Table upgrade.

Reasons:

- preserves names
- preserves enchantments
- preserves components/data
- keeps vanilla Smithing progression relevant
- feels appropriate for the final permanent upgrade tier

Avoid requiring Netherite Blocks per tool. That would create grind without adding meaningful progression.

## New proposed item

### Reinforced Netherite Ingot

This is the only new custom crafting ingredient proposed for the base vanilla tool progression.

### Recipe

```text
O N O
G D G
O N O
```

Where:

- `N` = Netherite Ingot
- `O` = Obsidian
- `G` = Gold Ingot
- `D` = Diamond

Result:

**1 Reinforced Netherite Ingot**

Cost per Reinforced Netherite Ingot:

- 2 Netherite Ingots
- 4 Obsidian
- 2 Gold Ingots
- 1 Diamond

## Smithing recipe

Use:

- Netherite Upgrade Smithing Template
- matching Diamond Tool
- Reinforced Netherite Ingot

Result:

- matching Netherite Tool

## Full-set cost

For all 4 tools:

- 8 Netherite Ingots
- 16 Obsidian
- 8 Gold Ingots
- 4 Diamonds
- 4 Netherite Upgrade Smithing Templates
- complete Diamond tool set

This feels appropriately expensive for permanent endgame tools while avoiding absurd resource requirements.

---

# 11. Resulting progression chain

```text
Tree
 ↓
Planks
 ↓
Condenser Mark I
 ↓
Stone Fragment
 ↓
Wooden Tools
 ↓
Mortar
 ↓
Stone Meal
 ↓
Stone Tools
 ↓
First Metal Production
 ↓
Iron Tools ───────────────→ Gold Tools
 ↓                          optional speed sidegrade
Water + Lava
 ↓
Obsidian
 ↓
Diamond Tools
 ↓
Nether access / Netherite progression
 ↓
Reinforced Netherite Ingot
 ↓
Smithing Table
 ↓
Netherite Tools
```

---

# 12. Tier identity summary

| Tier | Role |
|---|---|
| Wood | first permanent basic tool |
| Stone | first real resource-processing tool |
| Iron | first major metal progression unlock |
| Gold | optional high-speed specialization |
| Diamond | high-tier tool gated by Diamond + Obsidian |
| Netherite | final permanent endgame upgrade |

---

# 13. Items included in this first recipe overhaul

Initial scope should cover at minimum:

- Wooden Pickaxe
- Wooden Axe
- Wooden Shovel
- Wooden Hoe

- Stone Pickaxe
- Stone Axe
- Stone Shovel
- Stone Hoe

- Iron Pickaxe
- Iron Axe
- Iron Shovel
- Iron Hoe

- Golden Pickaxe
- Golden Axe
- Golden Shovel
- Golden Hoe

- Diamond Pickaxe
- Diamond Axe
- Diamond Shovel
- Diamond Hoe

- Netherite Pickaxe
- Netherite Axe
- Netherite Shovel
- Netherite Hoe

Sword recipes should be handled as a separate design pass rather than copied blindly from this pattern.

---

# 14. Follow-up tool pass

After the core mining/farming tool set is stable, design replacement recipes for:

- Swords
- Shears
- Flint and Steel
- Fishing Rod
- Brush

Potentially also review:

- Bow
- Crossbow
- Shield
- Trident acquisition/repair behavior
- Mace progression
- utility tools added by mods

Important:

If these items also inherit infinite durability, their vanilla acquisition cost becomes too low and should be rebalanced accordingly.

---

# 15. Recipe-removal requirements

Implementation should explicitly remove the existing recipes for the replaced tools before adding the new ones.

Do not leave vanilla alternatives accessible through JEI or crafting.

Check:

- normal crafting-table recipes
- special recipes
- recipe conflicts
- alternative mod-provided recipes
- recipe duplication from tags
- smithing recipes for Netherite
- recipe visibility in JEI

The goal is one authoritative progression path per tool tier.

Gold is intentionally a sidegrade and therefore may coexist with the main Iron → Diamond chain.

---

# 16. Recipe Editor / implementation strategy

Forever Recipe Editor is already intended for pack-level crafting changes.

Preferred implementation path:

1. Use Recipe Editor-generated data/recipes where it cleanly supports the required recipe replacement.
2. Store final custom recipe assets in the repository under the existing pack recipe configuration structure.
3. Keep custom mod code limited to behavior that cannot be expressed cleanly through recipe data.
4. The Reinforced Netherite Ingot requires a new registered Forever Skyblock item unless an existing generic component is deliberately chosen instead.
5. If the new item is added:
   - register item
   - add English and German translations
   - add item model/texture
   - add JEI visibility naturally through the recipe
   - document progression purpose
6. Do not tie these recipes to optional tech-mod components unless explicitly approved later.

---

# 17. Infinite durability interaction

The recipe overhaul assumes tools are globally configured or implemented to have effectively infinite durability.

Implementation of infinite durability should remain separate from recipe balancing where possible.

Validate that:

- using tools does not reduce durability
- Ultimine does not reduce durability
- enchantments do not accidentally re-enable durability consumption
- mod integrations that damage tools do not bypass the infinite-durability rule
- Smithing upgrades preserve item state correctly
- no repair loop becomes relevant or exploitable

If tools use a no-damage implementation rather than enormous durability, verify behavior across vanilla and modded tool-damage hooks.

---

# 18. Progression balance philosophy

Avoid "Expert Pack" balancing based purely on multiplying ingredient counts.

The intended difficulty should come from:

- connected crafting steps
- production dependencies
- prior-tier investment
- infrastructure unlocks
- resource-processing milestones
- optional specialization

Not from:

- requiring stacks of ingots for one item
- arbitrary compressed blocks
- dozens of intermediate crafting components with no later purpose
- forcing unrelated tech mods into fundamental vanilla progression

A recipe should answer:

> "Why is this ingredient here?"

Every ingredient in this proposal has a progression role.

---

# 19. Important balancing notes

## Wood

Wooden tools should be obtainable early enough that the start does not become tedious.

Do not increase their cost beyond the proposed recipe unless real playtesting shows they are still too cheap.

## Stone

Stone is intentionally the first significant resource sink.

Because Stone Meal itself costs Stone Fragments, this tier already scales naturally with Condenser throughput.

## Iron

Do not finalize iron recipe costs before the first-metal production route is designed.

The proposed values are a target, but actual iron availability may require tuning.

## Gold

Gold's infinite durability makes it much stronger than in vanilla.

This is intentional.

If actual gameplay proves it too dominant, adjust mining-speed access or recipe cost rather than forcing it into the main progression chain.

## Diamond

Obsidian is the intended secondary gate.

Do not implement this blindly before checking that the final water/lava route makes Obsidian available at the correct stage.

## Netherite

Do not require Netherite Blocks.

The Reinforced Netherite Ingot is intended as a meaningful endgame material sink without turning the tier into pure grind.

---

# 20. Validation checklist

## Recipe validation

- [ ] Vanilla recipes removed.
- [ ] New Wooden recipes work.
- [ ] New Stone recipes work.
- [ ] New Iron recipes work.
- [ ] New Gold recipes work.
- [ ] New Diamond recipes work.
- [ ] Reinforced Netherite Ingot recipe works.
- [ ] Netherite Smithing recipes work.
- [ ] JEI shows only intended recipes.
- [ ] No duplicate or alternate recipes remain.
- [ ] Tags resolve correctly.

## Progression validation

Start from the actual Forever Skyblock starting island and verify:

- [ ] Player can reach Wooden Tool progression without external resources.
- [ ] Stone Tool progression naturally follows Condenser + Mortar + Stone Meal.
- [ ] No circular dependency is introduced.
- [ ] Gold remains optional.
- [ ] Diamond cannot be reached before intended Obsidian progression.
- [ ] Netherite remains an endgame upgrade.
- [ ] Tool costs feel substantial but not grindy.

## Infinite durability validation

- [ ] Mining does not damage tools.
- [ ] Combat use does not damage tools where applicable.
- [ ] Ultimine does not damage tools.
- [ ] Tool actions such as hoeing/path creation do not damage tools.
- [ ] Smithing preserves enchantments/components.
- [ ] Unbreaking/Mending do not create bugs or misleading behavior.

## Multiplayer / persistence

- [ ] Crafting works identically in dedicated server environments.
- [ ] Recipe sync works correctly.
- [ ] No client-only recipe desync.
- [ ] JEI displays server-authoritative recipe state correctly.

---

# 21. Repository workflow constraints

Before editing:

- Inspect actual branch and HEAD.
- Inspect current working tree.
- Read `AGENTS.md`.
- Read `docs/DESIGN.md`.
- Read `docs/PROGRESSION.md`.
- Inspect current registered items and recipes.
- Inspect Recipe Editor generated pack state.

Implementation rules:

- Create a separate working branch.
- Do not overwrite unrelated work.
- Do not merge into `main` unless explicitly requested.
- Update documentation with implemented behavior.
- Do not claim in-game testing unless it was actually performed.
- Build and validate appropriately.
- Keep the final starting island NBT unchanged.

---

# 22. Recommended implementation branch

Suggested branch name:

`feature/infinite-tools-recipe-overhaul`

Alternative:

`feature/expert-tool-progression`

---

# 23. Suggested implementation order

1. Inspect repository state and existing recipe files.
2. Confirm current infinite-durability implementation/status.
3. Implement recipe removals.
4. Implement Wooden Tool recipes.
5. Implement Stone Tool recipes.
6. Implement Iron Tool recipes.
7. Implement Gold Tool recipes.
8. Implement Diamond Tool recipes.
9. Add Reinforced Netherite Ingot item.
10. Add Reinforced Netherite recipe.
11. Replace Netherite Smithing recipes.
12. Verify JEI.
13. Run builds.
14. Perform focused recipe tests.
15. Update documentation.
16. Deliver test artifact / branch for user playtesting.
17. Tune costs based on real progression feel.
18. Only merge after explicit user approval.

---

# 24. Decisions intentionally left open

The following should not be guessed during implementation:

1. Final route to first Iron.
2. Final Water progression.
3. Final Lava progression.
4. Exact point at which Obsidian becomes available.
5. Whether Swords also use the same previous-tier upgrade philosophy.
6. Whether Shears / Fishing Rod / Brush / Flint and Steel receive infinite durability.
7. Whether Gold tools need additional balancing after real playtesting.
8. Final texture/design for Reinforced Netherite Ingot.
9. Whether the new Netherite component should remain a custom item or reuse a future universal high-tier material.

These should be resolved based on actual pack progression rather than invented in isolation.

---

# 25. Design recommendation

The preferred recipe chain is:

| Tier | Main material | Core | Reinforcement |
|---|---|---|---|
| Wood | Planks | Stick | Stone Fragment |
| Stone | Stone Fragment | Wooden Tool | Stone Meal |
| Iron | Iron Ingot | Stone Tool | Chain |
| Gold | Gold Ingot | Stone Tool | Redstone Dust |
| Diamond | Diamond | Iron Tool | Obsidian |
| Netherite | — | Diamond Tool | Reinforced Netherite Ingot + Smithing Template |

This is the recommended baseline for implementation and in-game testing.
