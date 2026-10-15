# Starting island and inventory

## Tested snapshot

On October 15, 2026, TeKatz confirmed that the world, spawn and placed machines work. The island was slightly enlarged. The prebuilt ME network and condenser are visible in these reference images:

![Starting island](images/starting-island-2026-10-15.png)

![Starting inventory in the ME system](images/starting-inventory-2026-10-15.png)

The visible ME Drive readout shows **one 1k ME Item Storage Cell**. The earlier recommendation of a 4k cell was not adopted in this snapshot.

## Inventory shown in the screenshot

| Item | Quantity |
| --- | ---: |
| Bamboo | 1 |
| Cactus | 1 |
| Chests as items | 2 |
| Ice | 1 |
| Lava bucket | 1 |
| Melon slice | 1 |
| Obsidian | 1 |
| Seeds; exact type cannot be reliably identified without a tooltip | 1 |
| Sand | 1 |
| Sugar cane | 1 |

This is a visual inventory, not item IDs read from the NBT. The two chest items in the network are distinct from the previously placed starter chest that was removed.

## Subsequent decisions

TeKatz subsequently chose Expert difficulty and wants to remove items that enable an immediate cobblestone generator. The screenshot therefore **does not show the final starter inventory**. Remove ice and the lava bucket in the next revision and decide the remaining contents. Removing obsidian was a suggestion, not a confirmed decision.

## Stored template configuration

- File: `pack/config/skyblockbuilder/templates.json5`
- Only template: `forever`
- Structure file: `forever.nbt`
- Spawn: `[8, 3, 4]`, relative to the structure, facing east
- `mainSpawnIsland`: `null`, so the first and only template is used

The structure file itself is still missing. After each revision, export the island again from the build world and replace the actual file at `pack/config/skyblockbuilder/templates/islands/forever.nbt`. Starter item changes must be applied to the saved machine inventories or storage cell in the template; this document does not modify the ME inventory.
