# Fixed starting island

## Authoritative baseline

On October 15, 2026, TeKatz declared the supplied starting island **final**. Preserve its layout, stored inventories and exact NBT bytes. Do not redesign, resize, regenerate or change starter items unless TeKatz explicitly requests a revision.

The actual exported file is **`forever_me_start.nbt`**, superseding the earlier provisional `forever.nbt` name.

| Setting | Verified value |
| --- | --- |
| Structure | `pack/config/skyblockbuilder/templates/islands/forever_me_start.nbt` |
| Selected template | `forever_me_start` |
| Spawn | `[8, 4, 5]`, relative to the structure, facing east |
| Structure size | 14 × 11 × 10 blocks (X × Y × Z, including tree and underside) |
| Stored blocks | 396 |
| Stored entities | 0 |
| `mainSpawnIsland` | `null` |
| Spawn cell | Air, with a grass block immediately beneath |
| NBT DataVersion | 3955 |

The archive's `default.nbt` is preserved as supplied but is not selected: `templateList` contains only `forever_me_start`. Old named spawn references in the configuration are not additional selectable templates.

## Layout and stored equipment

The island has a tree on the left, a compact oak-plank equipment platform on the right, torches on fence posts and open workspace. The parsed NBT contains:

- 206 dirt blocks, 90 grass blocks and 19 oak planks.
- Six oak logs, 58 oak leaves, six oak fences and six torches.
- One Condenser Mark I, one ME Controller, one ME Drive, one Crystal Resonance Generator and one AE2 cable bus.
- The cable bus stores one Fluix Smart Cable and a west-facing ME Crafting Terminal.

The condenser is stored without a block-entity inventory tag in this export. Preserve the supplied NBT; the functional mod provides its block entity when placed/initialized. A fresh exported-pack instance remains the final integration check.

## Exact ME inventory

The ME Drive contains **one 1k ME Item Storage Cell**. Its saved storage component contains exactly:

| Item ID | Quantity |
| --- | ---: |
| `minecraft:bread` | 8 |
| `minecraft:wheat_seeds` | 1 |

These values were read from NBT, not inferred from a screenshot. No spare dirt, saplings, ice, lava bucket or other starter items are stored in the cell. `starter_inventory.json5` also contains an empty `items` list; items granted by other mods are not disabled by `clearInitialInventory`, which is false in the supplied settings.

An emergency sapling quest remains an optional future design task. It does not change the fixed island.

## Integrity and provenance

- Island SHA-256: `1ae2e4b779b1c6b56320aede0af2a711d4bd247a976771c95f633e05c1beb0c1`.
- Supplied archive SHA-256: `7e02bbc7ea184e41d51cfcd14a073f0abe43357f7de7012c72f4c2d21ea52b92`.
- All operational Skyblock Builder JSON5 files and supplied structure files were imported byte-for-byte. Generated registry listings under `skyblockbuilder/data/` are excluded.

The screenshot references in `docs/images/starting-*-2026-10-15.png` show an earlier revision and are retained only as historical references. They do not override this fixed NBT baseline.
