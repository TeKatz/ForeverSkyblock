# Mods and versions

This is an inventory, **not a complete or resolvable modpack lockfile**. Do not copy unknown versions from the regular Forever pack or the separate 1.21.4 test pack.

## Known state

| Component | Known value | Status |
| --- | --- | --- |
| Minecraft | 1.21.1 | Confirmed |
| Loader | NeoForge | Confirmed; installed version still needs to be recorded |
| Java | 21 | Build and runtime baseline |
| Forever Skyblock mod | 0.2.1 | Source under `mod/`; targets NeoForge 21.1.249 |
| JEI | 19.64.0.454 | Reported by TeKatz as the current Skyblock version |
| Forever Recipe Editor | 1.4.3-skyblock.1 | Skyblock build requires exactly JEI 19.64.0.454 and NeoForge 21.1.249+ |
| Applied Energistics 2 | To be recorded | Present; starting network tested by TeKatz |
| Skyblock Builder | To be recorded | Present; starting world tested by TeKatz |
| Default World Type | To be recorded | Intended for world-type selection and restriction; compare actual local file |
| FTB Quests | To be recorded | Quest line planned; no quest files supplied yet |
| Forever Production Monitor | To be selected/recorded | Intended for integration; no verified Skyblock installation yet |
| ForeverDeepCore | Open | Candidate for later industry; check void-world suitability first |

The custom mod now includes an optional JEI category compiled against 19.64.0.454. Its development client runtime includes that version; dedicated servers do not require JEI.

## Separate Forever repositories

- [Forever Recipe Editor](https://github.com/TeKatz/ForeverRecipeEditor): branch [`skyblock`](https://github.com/TeKatz/ForeverRecipeEditor/tree/skyblock), known build commit `3dbaa50e8f0a59b900f8b5e5ac68f3cac73ea32f`. `main` remains the regular version with a different JEI requirement.
- [Forever Production Monitor](https://github.com/TeKatz/ForeverProductionMonitor): separate repository; select the specific pack version later.
- [ForeverDeepCore](https://github.com/TeKatz/ForeverDeepCore): separate repository; inclusion and version remain undecided.

Do not duplicate these mods' sources here. Record the selected versions and commits in the pack inventory. Editor recipe changes are stored in `config/forever_recipe_editor/generated_pack` and loaded as a datapack by the editor mod.

## Candidates discussed so far

Mekanism, Industrial Foregoing, Powah, ExtendedAE, Applied Mekanistics, Functional Storage and Sophisticated Storage were proposed. Modern Industrialization was discussed as an option for a stronger industrial focus. Mystical Agriculture remains a balancing decision. **None of these mentions confirms inclusion or an installed version.**

Ex Deorum was proposed for the sieve opening that has since been rejected. KubeJS was suggested as a pack customization tool; whether it is needed alongside the custom Recipe Editor remains undecided.

## Distribution

Do not commit third-party mod JARs. Before publication, generate an actual launcher manifest and overrides, follow the selected mods' licenses and distribution rules, and test a fresh installation. Create the manifest once the platform and complete mod inventory are known.
