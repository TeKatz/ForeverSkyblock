# Arbeiten an Forever Skyblock

## Zuerst den tatsächlichen Stand prüfen

- Vor Änderungen Branch, HEAD, Arbeitsverzeichnis, vorhandene Regeln und relevante Quellen prüfen. Bei anderen Forever-Mods direkt deren Repository lesen; ältere Erinnerungen ersetzen keinen aktuellen Quellcode.
- `docs/DESIGN.md` enthält beschlossene Vorgaben. `docs/PROGRESSION.md` kennzeichnet unbestätigte Vorschläge. Vorschläge erst nach Zustimmung implementieren, sofern der Benutzer die konkrete Umsetzung nicht bereits angefordert hat.
- Vor der Arbeit am Mod `mod/README.md`, Registrierungen, Ressourcen und Abhängigkeiten prüfen.
- Die Vorlage `forever.nbt` und die aktuelle installierte Modliste nicht erfinden oder aus Screenshots rekonstruieren. Fehlende Dateien unter `docs/FILES_TO_IMPORT.md` nachhalten.

## Branches und externe Änderungen

- Neue Features, Rezeptbalance und Fehlerbehebungen auf eigenen Arbeitsbranches umsetzen.
- Der initiale Projektimport auf `main` ist durch Timos Auftrag zur Übernahme autorisiert. Spätere Merges nach `main`, Releases und Veröffentlichungen benötigen einen entsprechenden Benutzerauftrag.
- Bestehende Benutzeränderungen erhalten; keine fremden Arbeitsstände überschreiben oder Versionskontrollen umgehen.
- Recipe Editor, Production Monitor und DeepCore behalten ihre eigenen Repositories. Änderungen dort nur im passenden Repo und Branch umsetzen.

## Implementierung und Prüfung

- Minecraft 1.21.1, NeoForge, Java 21 und Mod-Metadaten konsistent halten. Versionen nur nach tatsächlicher Prüfung als kompatibel dokumentieren.
- Kleine, begrenzte Änderungen durchführen. Keine unnötigen Tick-Schleifen, Netzwerknachrichten oder unbegrenzten Produktionspuffer einführen.
- Passende Builds und gezielte Prüfungen ausführen. Ingame-Tests nur als durchgeführt bezeichnen, wenn sie tatsächlich stattgefunden haben; Tests durch Timo separat zuordnen.
- Bei UI-Änderungen echte Screens, Handler und Registrierungen prüfen. Kein alleiniger Schluss aus Klassennamen.
- Keine veröffentlichte JAR durch manuelles Umbenennen ihrer Version als neuen Build ausgeben. Aus dem zugehörigen Commit bauen und die fertige JAR samt Metadaten prüfen.
- Funktionsänderungen, Testergebnisse und offene Punkte in der Dokumentation aktualisieren.

## Pack und Speicherung

- Konfigurationen, eigene Rezept-Datapacks, Questdateien, Ressourcen und die echte Insel-NBT gehören ins Repo.
- Build-Ausgaben, Fremdmod-JARs, Welten, Logs, Caches, Zugangsdaten und lokale Infrastruktur-Hilfen gehören nicht ins Repo. Der Gradle-Wrapper ist eine beabsichtigte Ausnahme für eine Build-Abhängigkeit.
- Fertige JARs über Build-Artefakte bzw. eine ausdrücklich beauftragte Release-Veröffentlichung liefern.
- Vor einer Pack-Veröffentlichung eine frische Instanz aus dem Export testen: Insel, Spawn, ME-Inhalte, Energie, Welttyp-Sperre, Rezepte und Quests.
