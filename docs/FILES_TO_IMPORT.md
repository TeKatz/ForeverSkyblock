# Local files still to import

The initial import includes all relevant source and project files currently available here. A complete pack still requires:

| File or information | Destination | Purpose |
| --- | --- | --- |
| Actual exported `forever.nbt` | `pack/config/skyblockbuilder/templates/islands/forever.nbt` | Contains the island, machines and saved inventories; the main current blocker |
| Remaining Skyblock Builder configuration files actually in use | `pack/config/skyblockbuilder/` | Includes world generation, dimensions, spawn, structures and optional starting inventory |
| Local Default World Type configuration | Compare with `pack/config/defaultworldtype/client-config.toml` | The tracked file records agreed values but was not copied from the instance |
| Full Skyblock mod list or launcher manifest | Pack inventory and eventual export format | Record all actual versions, dependencies and sources |
| Actual saved Recipe Editor recipes, once created | `pack/config/forever_recipe_editor/generated_pack/` | Custom recipes applied across the pack |
| FTB quest files, once created | Appropriate paths under `pack/config/ftbquests/` | Quest book manually authored by TeKatz |
| Additional resources and configuration actually in use | Corresponding paths under `pack/` | Import after inspection |

Do not create an empty or invented `forever.nbt` placeholder. The complete `config/skyblockbuilder/` directory and the instance's Default World Type file are especially useful for the next import.

The Creative build world remains TeKatz's working copy and is not required for pack distribution. Do not commit world saves or logs.
