# Mods und Versionen

Dies ist eine Bestandsaufnahme, **keine vollständige oder auflösbare Modpack-Lockdatei**. Unbekannte Versionen werden nicht aus dem normalen Forever-Pack oder dem separaten 1.21.4-Testpack übernommen.

## Bekannter Stand

| Bestandteil | Bekannter Wert | Status |
| --- | --- | --- |
| Minecraft | 1.21.1 | Festgelegt |
| Loader | NeoForge | Festgelegt; tatsächlich installierte Version noch erfassen |
| Java | 21 | Build-/Laufzeitbasis |
| Forever Skyblock Mod | 0.1.0 | Im Repo unter `mod/`; gegen NeoForge 21.1.249 gebaut |
| JEI | 19.64.0.454 | Von Timo als aktuelle Skyblock-Version genannt |
| Forever Recipe Editor | 1.4.3-skyblock.1 | Skyblock-Build verlangt exakt JEI 19.64.0.454 und NeoForge 21.1.249+ |
| Applied Energistics 2 | Noch erfassen | Vorhanden, Startnetz von Timo getestet |
| Skyblock Builder | Noch erfassen | Vorhanden, Startwelt von Timo getestet |
| Default World Type | Noch erfassen | Für Vorauswahl und Sperre des Welttyps vorgesehen; konkrete lokale Datei noch abgleichen |
| FTB Quests | Noch erfassen | Questreihe vorgesehen; noch keine Questdateien übergeben |
| Forever Production Monitor | Noch festlegen/erfassen | Soll integriert werden; kein Skyblock-Installationsstand belegt |
| ForeverDeepCore | Noch offen | Kandidat für spätere Industrie; Void-Eignung zuvor prüfen |

## Separate Forever-Repositories

- [Forever Recipe Editor](https://github.com/TeKatz/ForeverRecipeEditor): Branch [`skyblock`](https://github.com/TeKatz/ForeverRecipeEditor/tree/skyblock), bekannter Build-Commit `3dbaa50e8f0a59b900f8b5e5ac68f3cac73ea32f`. `main` bleibt die reguläre Version mit anderer JEI-Bindung.
- [Forever Production Monitor](https://github.com/TeKatz/ForeverProductionMonitor): eigenes Repository, konkrete Pack-Version noch auswählen.
- [ForeverDeepCore](https://github.com/TeKatz/ForeverDeepCore): eigenes Repository, Aufnahme und Version noch offen.

Die Quellen dieser Mods werden hier nicht dupliziert. Die Pack-Bestandsaufnahme hält die verwendeten Versionen und Commits fest. Rezeptänderungen des Editors liegen unter `config/forever_recipe_editor/generated_pack` und werden von der Editor-Mod als Datapack eingebunden.

## Bisher diskutierte Kandidaten

Mekanism, Industrial Foregoing, Powah, ExtendedAE, Applied Mekanistics, Functional Storage und Sophisticated Storage wurden vorgeschlagen. Modern Industrialization wurde als mögliche stärkere industrielle Ausrichtung diskutiert. Mystical Agriculture ist eine offene Balanceentscheidung. **Keine dieser Nennungen bestätigt die Aufnahme oder eine konkrete installierte Version.**

Ex Deorum war ein Vorschlag für den inzwischen verworfenen Sieb-Einstieg. KubeJS war eine mögliche Packgestaltungshilfe; ein Bedarf zusätzlich zum eigenen Recipe Editor ist noch nicht beschlossen.

## Distribution

Fremdmod-JARs nicht ins Git-Repo einchecken. Für die spätere Veröffentlichung ein echtes Launcher-Manifest und Overrides erzeugen, Lizenzen und Distributionsregeln der tatsächlich gewählten Mods beachten und eine frische Installation testen. Ein Manifest wird erst angelegt, wenn Plattform und vollständiger Modbestand feststehen.
