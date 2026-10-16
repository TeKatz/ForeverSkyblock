# Personal challenge progression — 0.13.0 / phase 4

Development branch: `feature/tech-challenges-phase4`, based on the Phase 3 meta-progression branch and retaining the unmerged 0.9.1 machine fixes. Minecraft 1.21.1, Java 21; pack runtime NeoForge 21.1.259. No main merge or release is implied.

## Active objectives

All rewards are once per player profile, across worlds within the existing authority. Singleplayer and dedicated-server storage remain separate. No inventory-possession or retrospective inventory scan grants a reward.

| Category | Challenge | Personal action | SP |
| --- | --- | --- | ---: |
| Forever | First Foundations | Craft Cobblestone from four Stone Fragments; existing ID/claim retained | 1 |
| Forever | Stoneworker | Craft Stone Meal using the Mortar and four fragments | 1 |
| Forever | Permanent Tools I | Craft each of the four Forever Stone tools | 2 |
| Forever | The Iron Age | Craft each of the four Forever Iron tools | 3 |
| Forever | Diamond Standard | Craft the expert Diamond Pickaxe | 3 |
| Forever | Forever Forged | Smith the Netherite Pickaxe with Reinforced Netherite Ingot | 5 |
| AE2 | Beyond the Starter Cell | Personally craft a 4k-or-higher AE2 item storage cell | 1 |
| AE2 | Autocrafting Online | Finish an AE2 crafting-CPU job requested by the connected player | 3 |
| AE2 | Crafting Brain | Personally craft `ae2:64k_crafting_storage` | 3 |
| AE2 | Wireless Operator | Open an AE2 Wireless Crafting Terminal | 2 |
| AE2 | Storage Architect | Personally craft `ae2:item_storage_cell_256k` | 4 |
| Mekanism | Mekanized | Personally craft `mekanism:metallurgic_infuser` | 1 |
| Mekanism | Digital Extraction | A Digital Miner owned by the connected player successfully mines a block | 5 |
| Powah | Energized | Personally take a completed output from a Powah Energizing Orb | 1 |
| Mystical Agriculture | Supremium | Personally craft `mysticalagriculture:supremium_essence` | 4 |
| Combat & Dimensions | Into the Nether | Enter the Nether | 2 |
| Combat & Dimensions | Withered Away | Minecraft credits the player with a Wither kill | 5 |
| Combat & Dimensions | The Final Dimension | Enter the End | 3 |
| Combat & Dimensions | Dragon Slayer | Minecraft credits the player with an Ender Dragon kill | 6 |
| Combat & Dimensions | Veteran | 1,000 distinct personally credited Monster-category kills | 3 |

The live catalog now contains **20 challenges / 58 SP**, including the existing First Foundations point. The complete proposed 38-challenge / 135-SP catalog is still a balancing target, not an active reward list. Existing partial challenge evidence, completed claims, balances and tiers survive this update unchanged.

## Phase 4 attribution rules

Phase 4 activates only technology milestones with an authoritative player/action boundary on the pinned pack versions. Optional integrations fail closed: if the expected class/API is missing or changes, the affected challenge gives no reward and logs a compatibility warning instead of guessing.

### Applied Energistics 2 — 19.2.18

- Storage-cell and 64k Crafting Storage objectives reuse the server's personal `RECIPE_CRAFTED` evidence. The exact registered output ID is checked; merely receiving a cell does not qualify.
- A 256k item cell can complete **Beyond the Starter Cell** and **Storage Architect** together if both were still open. Each reward remains once per profile.
- **Autocrafting Online** hooks AE2's successful crafting-CPU completion. The persisted job requester ID is resolved through AE2's player registry; only the connected requesting player receives credit. Machine-only jobs and an offline requester do not pay.
- **Wireless Operator** requires opening the actual `WirelessCraftingTermMenu`. Merely crafting, holding or receiving the terminal does not qualify.
- Existing AE2 manual Crafting Terminal personal crafts continue through the same Vanilla recipe trigger used by the Forever challenges.

### Mekanism — 10.7.19.85

- **Mekanized** is a personal craft of the Metallurgic Infuser.
- **Digital Extraction** is emitted only after `TileEntityDigitalMiner` reports a successful block replacement/removal. Credit is resolved from the machine's security owner UUID and requires that owner to be connected. Crafting or placing a Digital Miner does not qualify.

### Powah — 6.2.10

- **Energized** requires a real player interaction with an Energizing Orb whose output-only slot contains a completed recipe result. The player receives credit when taking that finished output. Owning the Orb or its output item is not enough.

### Mystical Agriculture — 8.0.28

- **Supremium** uses the personal craft result path and the exact registered Supremium Essence output. Inventory possession and player-to-player handoff do not qualify.

## Deliberately deferred objectives

The following proposed rewards remain inactive rather than using an unreliable ownership shortcut:

- AE2 **Quantum Link**: a shared cross-dimensional grid needs an explicit personal-use/ownership rule.
- Mekanism **Factory Floor**, **Ultimate Industry**, **Fission Online**, **Fusion Online** and **Matter Transcended**: shared machines and multiblocks need an approved attribution policy.
- Powah **Reactor Engineer** and **Nitro Age**: reactor ownership/use needs an approved personal rule.
- Industrial Foregoing technology challenges: operation ownership and the final **Industrial Mastery** target are not yet approved.
- Mystical Agriculture **Essence Farmer** and **Master Grower**: the first progression target and altar/seed attribution need an explicit rule.
- Apotheosis **Mythic Power**: the capstone itself remains an open design decision.
- First Iron, Dirt ownership and Water/Lava/Obsidian objectives remain tied to unresolved pack-progression routes.
- Repeatable contracts, Tier II–IV rules and valuable world-shop product delivery remain later phases.

These deferred challenges do not appear as obtainable rewards and do not count toward the live 58-SP total.

## Existing personal-action rules retained

`ChallengeRules` remains a pure server-owned catalog/reducer. `ChallengeEvents` and `TechChallengeEvents` normalize trusted evidence; `ProgressionService.evidence` commits progress, completion, SP, accounting and any Tier increase atomically through `ProfileStore`. Chat/snapshots are sent only after persistence succeeds.

- **Crafting and smithing:** use successful personal recipe-result paths; recipe previews and inventory pickup do not qualify. FakePlayer is rejected.
- **Combat:** use Minecraft's credited player from `PLAYER_KILLED_ENTITY`, including attributed projectiles/recent-player kill credit; nearby players never receive blanket credit.
- **Dimensions:** use `PlayerChangedDimensionEvent`; login while already in a dimension is not retroactive evidence.
- **Optional integrations:** no per-tick inventory/world scans are introduced. Absent optional classes are not a reason to weaken an objective into possession detection.

## Tiers and menu

Tier -1 means no tier yet. First Foundations unlocks **Tier 0 — Foundations**; Dragon Slayer unlocks **Tier I — Conqueror**. Tier increases remain monotonic and independent of balances, admin/test SP and skill respecs. Phase 4 technology challenges do **not** activate Tier II–IV.

K → Challenges now offers:

- All
- Forever
- Applied Energistics 2
- Mekanism
- Powah
- Mystical Agriculture
- Combat & Dimensions
- Contracts

Contracts remains an explanatory inactive page. Existing search, open/completed filters, detail cards, Overview, Shop, Research, Perks and Profile remain in the same hierarchy. The network payload shape and protocol remain **`skills-6`** because Phase 4 only expands the server catalog and known IDs; no new client-authoritative economic fields are added.

## Persistence and migration

Profile schema remains **4**. The update adds no destructive migration and preserves:

- SP / PP balances and accounting
- completed milestone IDs
- partial objective evidence
- skill and Jetpack research ranks
- permanent Shop Tier
- world claims and purchase receipts
- paid PP investments
- redeemed Core UUIDs
- unknown valid IDs/revisions for forward compatibility

Existing profiles do not receive retroactive technology rewards from inventory, advancements or machine contents. The first new qualifying action after installing 0.13.0 starts/completes the relevant objective. Ordinary skill reset does not clear challenge claims/progress, tiers, world claims or Core receipts.

Back up `forever_skyblock/progression/` and the test world before changing versions. Older mod versions cannot be assumed to read profiles written by a newer schema-compatible implementation if their catalog/protocol expectations differ.

## Automated verification

The Phase 4 CI matrix includes the normal Java 21 build/unit tests, baseline dedicated GameTests without optional technology mods, and a NeoForge 21.1.259 integration run with the pinned pack integrations. The integration run explicitly checks the runtime IDs/API boundaries used by the AE2, Mekanism, Powah and Mystical Agriculture adapters so an upstream/signature mismatch fails the build rather than silently shipping a dead objective.

The final verified commit/run/artifact are recorded in [VALIDATION.md](VALIDATION.md).

## Manual acceptance

Use a copy of the pack instance and profile:

1. Confirm existing SP, PP, Tier, completed challenges, world claim and Jetpack research remain unchanged after replacing 0.12.0.
2. Open K → Challenges and inspect All plus AE2/Mekanism/Powah/Mystical Agriculture categories in English and German at GUI scales 2/3/4.
3. AE2: personally craft 4k and 256k item cells and a 64k Crafting Storage; run a real player-requested autocraft; open the Wireless Crafting Terminal. Verify each reward pays once and restart/new-world cannot replay it.
4. Mekanism: personally craft the Metallurgic Infuser. Let a Digital Miner owned by this player actually mine a block while the owner is online; merely craft/place it first and verify no reward occurs.
5. Powah: complete an Energizing recipe and personally take the output. Verify possession of the same item through another route does not pay.
6. Mystical Agriculture: personally craft Supremium Essence and verify a handed/given stack alone does not pay.
7. Recheck existing Forever tool challenges, Veteran/boss/dimension progress, current-world Phase 3 test purchase, skill reset, Jetpack JEI restrictions, Condenser/Ascension GUIs and machine visuals.
8. Confirm no Tier II–IV unlock occurs from technology SP alone.

Headless tests do not certify rendered client pixels or every multiplayer/team workflow; those remain TeKatz's in-game acceptance checks.
