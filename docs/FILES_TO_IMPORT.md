# Remaining files and information

## Already present

The repository already contains:

- the actual Skyblock Builder configuration folder and fixed `forever_me_start.nbt` island template;
- the actual `config/defaultworldtype/client-config.toml`;
- the supplied runtime/list evidence and provisional mod inventory;
- the confirmed pack runtime NeoForge 21.1.259;
- the approved permanent-tool recipe/removal baseline in the Recipe Editor generated pack.

The island is final unless TeKatz explicitly requests a revision. The mod selection remains experimental and may be refreshed from newly supplied lists, logs or files.

## Import when available or relevant

| File or information | Destination | When needed |
| --- | --- | --- |
| Further user-authored Recipe Editor exports | `pack/config/forever_recipe_editor/generated_pack/` | When additional custom crafting changes are authored |
| FTB quest files | `pack/config/ftbquests/` and related pack paths | When TeKatz wants the authored quest book tracked here |
| KubeJS scripts/assets/data, if customized | Corresponding paths under `pack/kubejs/` | Only when actual custom files exist |
| Other intentional configuration overrides | Corresponding `pack/config/` / `pack/defaultconfigs/` paths | When required for a reproducible instance |
| Launcher/export manifest and download identifiers | Chosen pack export format | Before distributing a complete installable pack |
| Updated runtime mod snapshot | `pack/mods.inventory.json` plus snapshot docs | When TeKatz supplies a new authoritative list/log/export |

## Snapshot caveat

Neither the current launcher text list nor the older log-derived snapshot is a launcher manifest or a final mod selection. The supplied 80-entry list records an older Forever Skyblock artifact (`0.6.1`); the repository's current integrated mod version is **0.13.4**. Do not downgrade the repository version to match the snapshot.

Missing future recipes, quests or scripts are planned inputs rather than defects in the fixed island. Raw logs, Creative test worlds and generated registry dumps do not belong in the repository.

Before publishing, verify a fresh instance created from the actual export: fixed island, spawn, ME contents, power, world-type restriction, production, recipes, progression and quests.
