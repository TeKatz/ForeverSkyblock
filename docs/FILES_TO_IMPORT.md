# Remaining files and information

## Already present

The repository already contains:

- the actual Skyblock Builder configuration folder and fixed `forever_me_start.nbt` island template;
- the actual `config/defaultworldtype/client-config.toml`;
- the supplied runtime/list evidence and provisional mod inventory;
- the current supplied October 22, 2026 mod-list snapshot with **94 enabled artifact labels**;
- the current supplied pack runtime **NeoForge 21.1.264**;
- the previous October 21 snapshot and older runtime evidence as historical records;
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
| Fresh runtime log / loaded-mod-ID export | `pack/mods.inventory.json` plus runtime snapshot | When a new machine-derived loaded-ID count is needed |

## Snapshot caveat

The current `Modliste(6).txt` snapshot contains **94 enabled artifact labels** and reports Minecraft 1.21.1, LWJGL 3.3.3 and NeoForge **21.1.264**. It is the authoritative current text-list snapshot for documentation purposes. The exact text is stored at `pack/snapshots/2026-10-22_modlist.txt`.

No fresh runtime log or explicit gameplay-validation statement accompanied this list. The repository therefore does not invent a new loaded-mod-ID count or embedded-module inventory. The October 15 runtime JSON remains the last machine-derived runtime-ID snapshot and is historical evidence for NeoForge 21.1.259; the October 21 `Modliste(5).txt` remains the previous launcher snapshot on NeoForge 21.1.262.

Missing future recipes, quests or scripts are planned inputs rather than defects in the fixed island. Raw logs, Creative test worlds and generated registry dumps do not belong in the repository unless a deliberate sanitized snapshot is requested.

Before publishing, verify a fresh instance created from the actual export: fixed island, spawn, ME contents, power, world-type restriction, production, recipes, progression and quests.
