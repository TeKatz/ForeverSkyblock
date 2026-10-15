# Noch zu übernehmende lokale Dateien

Der initiale Import enthält alle aktuell hier vorliegenden, relevanten Quellen und Projektdateien. Für einen vollständigen Pack-Stand fehlen noch:

| Datei oder Stand | Ziel | Bedeutung |
| --- | --- | --- |
| Tatsächliche exportierte `forever.nbt` | `pack/config/skyblockbuilder/templates/islands/forever.nbt` | Enthält Insel, Maschinen und gespeicherte Inventare; derzeitiger Hauptblocker |
| Weitere tatsächlich verwendete Skyblock-Builder-Configs | `pack/config/skyblockbuilder/` | Unter anderem Worldgen, Dimensionen, Spawn, Strukturen und gegebenenfalls Startinventar |
| Lokale Default-World-Type-Konfiguration | Abgleich mit `pack/config/defaultworldtype/client-config.toml` | Der eingecheckte Stand enthält die besprochenen Werte, ist aber nicht aus der Instanz kopiert |
| Vollständige Skyblock-Modliste oder Launcher-Manifest | Pack-Bestandsaufnahme bzw. späteres Exportformat | Alle tatsächlichen Versionen, Abhängigkeiten und Bezugsquellen festhalten |
| Tatsächlich gespeicherte Recipe-Editor-Rezepte, sobald angelegt | `pack/config/forever_recipe_editor/generated_pack/` | Packweite eigene Rezeptänderungen |
| FTB-Questdateien, sobald angelegt | Passende Pfade unter `pack/config/ftbquests/` | Timos manuell aufgebautes Questbuch |
| Zusätzliche tatsächlich eingesetzte Ressourcen/Configs | Entsprechende Pfade unter `pack/` | Erst nach Sichtung übernehmen |

Keine leere oder erfundene `forever.nbt` als Platzhalter erzeugen. Der vollständige Ordner `config/skyblockbuilder/` und die Default-World-Type-Datei aus der Instanz sind für die nächste Übernahme besonders hilfreich.

Die Creative-Bauwelt bleibt Timos Arbeitskopie und wird nicht für die Pack-Verteilung benötigt. Weltstände und Logs werden nicht ins Repo eingecheckt.
