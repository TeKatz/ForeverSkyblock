# Ascension and Perk Points — 0.8.0

TeKatz approved a second, separate profile currency for exceptional endgame unlocks. This branch builds on the unmerged permanent-tool update from PR #9; it does not merge either feature into `main`.

## Currencies and first perk

| Currency | Source | First use |
| --- | --- | --- |
| Skill Points (SP) | Existing once-per-profile milestones; temporary owner/operator +10 SP button | Regular skills and all four research stages |
| Perk Points (PP) | Consume one crafted Forever Core in an Ascension Chamber | Creative Jetpack research additionally requires 1 PP |

The ordered Jetpack stages remain Beginner (5 SP), MidGame (10 SP), EndGame (20 SP), Creative (**40 SP + 1 PP**). EndGame is still required before Creative. Points, claims and purchases persist across singleplayer worlds in this instance. Dedicated servers maintain their own UUID profiles. There is no currency exchange or online account synchronization.

The K menu now shows both balances and has a Perks tab explaining acquisition and listing PP-gated purchases. Creative also remains visible in the Jetpacks tab with its dual cost. The existing confirmation-based reset refunds SP and actually invested PP separately, removes purchased effects/research and preserves milestone and Core receipts. Repeated reset cannot mint either currency. The temporary +10 button grants SP only. A green, clickable +1 PP message opens the skill menu after a successful new credit.

## Forever Core

Registered item: `forever_skyblock:forever_core`. It has an original 16×16 pixel texture, Epic rarity, fire resistance and a maximum stack size of one.

Initial shaped recipe (one Core):

```text
MNM
ASA
MNM
```

| Symbol | Ingredient | Quantity |
| --- | --- | --- |
| M | Mekanism Antimatter Pellet (`mekanism:pellet_antimatter`) | 4 |
| N | Powah Nitro Crystal (`powah:crystal_nitro`) | 2 |
| A | AE2 Singularity (`ae2:singularity`) | 2 |
| S | Nether Star | 1 |

These quantities are an initial balance choice, not a complete endgame progression or playtime claim. The three modded item identifiers were checked against upstream registries and are checked again by the pack-integration test. The recipe is conditionally loaded only if all three items exist; the mod itself still runs without those mods. No production dependency is added for them.

The recipe is standard `minecraft:crafting_shaped` JSON, bundled in the mod and mirrored byte-for-byte in the Recipe Editor generated pack. Editing its ingredients does not require a custom recipe serializer. Scoped shaped/shapeless assembly hooks give every produced Core a fresh UUID in custom data while preserving its other output components. Recipe display results have no redeemable identity; a raw `/give` or JEI-cheated preview must not generate PP. For testing, craft the Core from cheated ingredients, or use the identified Core entry in the Ingredients creative tab. Copies of that same creative entry share its identity and can only pay once.

All ingredient chains can be automated. The Vanilla Crafter is the tested route for automatically producing distinct Cores; send its output to the Chamber with hoppers/pipes. Starting the point conversion remains personal. **AE2 automatic crafting patterns for the final Core are deliberately blocked**: AE2 caches an exact output, including its UUID, and would otherwise repeatedly produce the same claimed identity. This also avoids silently consuming ingredients for unusable duplicate Cores. The ordinary manual ME Crafting Terminal is not blocked; full mouse-flow testing remains a client check. AE2 can continue supplying all ingredients. Other autocrafters are not certified by these tests.

## Ascension Chamber

Registered block/entity/menu: `forever_skyblock:ascension_chamber`.

Initial recipe: `ROR / OEO / ROR`, using four Reinforced Netherite Ingots, four Obsidian and one Ender Chest. Requires a diamond-tier pickaxe for the block drop. The fixed starter island is unchanged.

1. Insert one crafted Core. Hoppers and compatible pipes can insert/extract an idle input from any side; simulated operations do not change it.
2. The menu shows **1 Core → 1 PP**, the initial **200 ticks / 10 seconds** processing time and the recipient.
3. Personally press Start. The server checks the active menu, distance and current inventory, then binds the initiating player's UUID. Another viewer cannot redirect the operation.
4. The input locks against normal clicks, shift-click and automation during processing. The front pulses and the block emits light; the GUI shows progress.
5. At completion the machine commits the claim/credit before removing the Core. A new credit gives precisely one PP to the initiating profile, and no SP.

The initial version uses no FE or fuel. Processing requires a loaded chunk and an online recipient; logout pauses it. It does not force-load chunks or provide offline work. Inventory, recipient, Core identity and progress survive a chunk/server reload. Breaking the Chamber drops the same real Core once and discards partial work. A storage failure leaves the Core in place, reports failure and stops automatic retries; press Start after repairing storage.

The housing uses **one full cuboid**, with no overlays, intersecting posts or coplanar duplicate faces. All cull tags are on the six actual outer boundaries. Its full-block occlusion is intentional; only genuinely shared hidden faces disappear. Inactive and active models share the exact geometry. A separate animated front texture provides activity without extra geometry. English and German UI text are included, and the screens use the inherited background render once.

JEI shows the two crafting recipes and an Ascension category with the Core input, conversion, duration and personal-start rule. The Chamber is its catalyst, and the GUI progress bar opens that category. Existing Condenser JEI transfers and Jetpack visibility updates remain in place.

## Persistence, crash replay and migration

Profiles migrate from schema 1 to **schema 2**, adding `perk_points`, actual `perk_investments` and redeemed Core UUIDs. Existing SP, ordinary purchases and completed milestones remain. A legacy SP-only Creative purchase is removed and **40 SP per rank refunded**, without awarding PP; EndGame and earlier stages remain. It must then be purchased under the new dual cost. Successful subsequent writes persist the migrated profile; reloading an unchanged legacy file calculates the same migrated balance, not an accumulating refund.

Profiles remain under `forever_skyblock/progression/local/` or `server/`, outside world saves. `core_claims/<core-uuid>.claim` in that authority contains the first beneficiary UUID. A flushed temporary claim is published through an exclusive hard link, so another claimant cannot replace it. The profile then adds an idempotent receipt and one PP through the existing atomic-save path. If the process stops after claiming but before credit, the same recipient can finish it. If it stops after credit but before the world records consumption, replay removes the already-credited Core without a second award. A different player's copied claim cannot pay them.

The filesystem must support ordinary hard links (standard NTFS and common Linux filesystems do). Unsupported storage fails closed, retaining the Core. Preserve **both profiles and Core claims** when moving or backing up progression. Claims are never cleared by reset. Per-profile receipts are bounded at 16,384 and existing point/file-size limits apply; reaching a limit fails without consuming the input.

This protects an already-identified Core against duplicate requests, repeated processing and restoring world state containing that same Core. It is not an anti-cheat boundary against editing/deleting progression data or restoring materials from before crafting and crafting a genuinely new UUID. Whole-system power-loss guarantees depend on the filesystem; no distributed transaction across arbitrary world/profile backups is claimed.

## Verification

Automated checks cover dual balances, purchase/refund limits, legacy migration, duplicate/concurrent redemption, durable replay, different-player claims, storage failure and recovery, recipe/network codecs, actual Vanilla Crafter production, locked automation and menus, distance, recipient binding, saved progress, copied/restored Cores, active-block drops, offline pause and model geometry. The existing Jetpack test now proves that 40 SP alone cannot unlock Creative before redeeming a Core. See [VALIDATION.md](VALIDATION.md) for completed run IDs and the final artifact.

Client visual checks, actual JEI click/hover behavior, manual ME terminal mouse gestures, balance and the complete exported pack remain TeKatz's in-game tests. No manual Minecraft session is claimed here.
