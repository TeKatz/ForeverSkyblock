# Forever Skyblock

Eigenständiges **Expert-Skyblock-Modpack für Minecraft 1.21.1 / NeoForge** von TeKatz.

Der Einstieg beginnt auf einer kleinen Insel mit Baum, betriebsbereitem AE2-Netz und dem eigenen **Kondensator Mark I**. Das Questbuch wird von Timo manuell mit FTB Quests aufgebaut. Dieses Repository enthält Mod-Quellcode, verfügbare Pack-Dateien und die verbindliche Projektdokumentation.

## Aktueller Stand

- Eigene Mod **0.1.0**: ein platzierbarer Kondensator mit Texturen, Item, Übersetzungen und Loot-Tabelle. Noch keine Ressourcenproduktion oder GUI.
- Startwelt, Spawn und aufgestellte Maschinen wurden von Timo erfolgreich ingame getestet.
- Die Insel wurde vergrößert; die Startkiste wurde entfernt und ihr Inhalt ins ME-System gelegt.
- `templates.json5` verwendet ausschließlich `forever.nbt`, Spawn `[8, 3, 4]`, Blickrichtung Osten.
- Die Welttyp-Sperre ist mit den besprochenen Werten für Default World Type hinterlegt.
- Forever Recipe Editor **1.4.3-skyblock.1** liegt in seinem eigenen Repository auf Branch `skyblock` und wurde gegen **JEI 19.64.0.454** gebaut.

**Der vollständige Pack-Export ist noch nicht reproduzierbar:** Die echte `forever.nbt`, weitere lokale Skyblock-Builder-Konfigurationen und die vollständige installierte Modliste wurden noch nicht übergeben. Die Datei für Default World Type ist aus unseren besprochenen Einstellungen rekonstruiert und noch nicht mit Timos lokaler Datei abgeglichen. Details: [Fehlende Dateien](docs/FILES_TO_IMPORT.md).

## Aufbau

| Pfad | Inhalt |
| --- | --- |
| `mod/` | Vollständiges eigenständiges Gradle-Projekt der Forever-Skyblock-Mod |
| `pack/` | Dateien, die später in den Instanzordner des Modpacks kopiert werden |
| `docs/DESIGN.md` | Festgelegte Ausrichtung und Expert-Pacing |
| `docs/PROGRESSION.md` | Offene Entwürfe für die erste Materialprogression |
| `docs/START_ISLAND.md` | Startinsel, bestätigter Snapshot und geplante Änderungen |
| `docs/MODS.md` | Bestätigte Versionen, separate Repositories und Mod-Kandidaten |
| `docs/VALIDATION.md` | Bisherige Prüfungen und Grenzen der Reproduzierbarkeit |
| `docs/CHANGELOG.md` | Änderungen am Projekt |

## Mod bauen

Java 21 installieren, dann:

```sh
cd mod
./gradlew build
```

Windows: `gradlew.bat build`. Die JAR entsteht in `mod/build/libs/`. Ein GitHub-Actions-Workflow baut die Mod ebenfalls mit Java 21 und stellt die JAR als Build-Artefakt bereit.

## Pack-Dateien verwenden

Den **Inhalt** von `pack/` in den Instanzordner übernehmen, sobald die unter [FILES_TO_IMPORT.md](docs/FILES_TO_IMPORT.md) genannten Dateien ergänzt sind. `mods.inventory.json` ist unsere Bestandsaufnahme und kein Launcher-Manifest. Fremdmods werden über den später festzulegenden Launcher-/Distributionsweg eingebunden.

Für die Anfangsübernahme auf `main` wurde kein neues Gameplay implementiert. Weitere Feature- und Balancing-Arbeiten erfolgen auf Arbeitsbranches; siehe [AGENTS.md](AGENTS.md).
