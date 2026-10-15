# Prüfstand

## Forever Skyblock Mod 0.1.0

- Java-21-Gradle-Build erfolgreich.
- JAR-Inhalt, Minecraft-/NeoForge-Metadaten sowie JSON-Ressourcen geprüft.
- Dedizierter Entwicklungsserver mit Minecraft 1.21.1 und NeoForge 21.1.249 erfolgreich gestartet.
- Block per Serverkommando platziert und anschließend per `execute if block` geprüft.
- Loot-Tabelle mit Diamantspitzhacke abgefragt: ein `Condenser Mark I` ausgegeben.
- Server ordnungsgemäß beendet und Welt gespeichert.
- Timos spätere Screenshots zeigen die Textur ingame; er bestätigte funktionierende Welt, Spawn und platzierte Maschinen.

Die lokalen Serverprüfungen fanden im Entwicklungsmodus statt. Der vollständige importierte Pack-Export wurde noch nicht in einer frischen Instanz getestet. Die anfänglichen Container-Anpassungen des Buildwerkzeugs sind nicht Teil des Mod-Quellcodes und wurden nicht übernommen.

## Forever Recipe Editor 1.4.3-skyblock.1

- Eigener Branch `skyblock`, Commit `3dbaa50e8f0a59b900f8b5e5ac68f3cac73ea32f`.
- [Java-21-Build erfolgreich](https://github.com/TeKatz/ForeverRecipeEditor/actions/runs/38678418057).
- Die neun für die JEI-Integration geprüften öffentlichen API-Interfaces sind zwischen 19.53.0.425 und 19.64.0.454 unverändert.
- Heruntergeladenes Build-Artefakt per SHA-256 geprüft; JAR geprüft: Version, Klassen und genaue JEI-Ladebedingung `[19.64.0.454]`.
- Interaktive Prüfung von Öffnen, Hover, Ghost-Drag, Speichern/Reload, Rename und Delete in Timos aktuellem Pack steht noch aus.

## Vor der Pack-Veröffentlichung

1. Fehlende echte Dateien aus [FILES_TO_IMPORT.md](FILES_TO_IMPORT.md) übernehmen.
2. Expert-Startvorrat in der Inselvorlage überarbeiten.
3. Vollständige Modliste und Versionen erfassen.
4. Frische Instanz aus dem späteren Pack-Export erstellen.
5. Insel, Spawn, Energie, ME-Inhalt und Welttyp-Sperre prüfen.
6. Tatsächlich implementierte Ressourcenwege, Rezepte und Quests vollständig durchspielen.

Ein erfolgreicher Mod-Build allein bestätigt keinen vollständigen Pack-Export.
