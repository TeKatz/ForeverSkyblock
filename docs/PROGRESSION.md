# Entwürfe für die erste Progression

**Status: Vorschlag, noch nicht von Timo bestätigt und nicht implementiert.**

Die Diskussion wurde nach dem folgenden Vorschlag durch die Einrichtung des Repositories unterbrochen. Ein Auftrag, alle bisherigen Dinge zu übernehmen, bestätigt diese Gameplay-Idee nicht automatisch.

## Letzter konkreter Vorschlag

| Schritt | Entwurf |
| --- | --- |
| Ausgangsstoff | Neues Item **Steinfragmente** |
| Mark I | Ein fester Ausgangsstoff, langsam und ohne externe Energie |
| Testwert | Ein Fragment alle zwei Sekunden |
| Cobblestone | Vier Fragmente im 2×2-Rezept ergeben einen Cobblestone |
| Erster Ofen | Zunächst das normale Rezept aus acht Cobblestone |
| Stone | Cobblestone mit Holz bzw. Holzkohle im Ofen schmelzen |
| Entnahme | Anfangs manuell aus einem kleinen, begrenzten Ausgabepuffer |
| Volles Inventar | Produktion pausiert |
| Späterer Ausbau | Transport, weitere Verarbeitung und ein Wasser-/Lava-Cobblestone-Generator als höherer Durchsatz |

Rechnerisch ergibt der Testwert einen Cobblestone alle acht Sekunden und acht Cobblestone nach 64 Sekunden, ohne Handhabungszeit. Das sind Entwurfswerte und keine gemessenen Laufzeiten.

## Vor der Umsetzung entscheiden

1. Passt dieser Einstieg zum gewünschten Expert-Anspruch?
2. Welches Verfahren führt zu ersten Metallen, ohne direkten Eisen-Output des Mark I?
3. Welche Materialien benötigt die erste neue Verarbeitungsmaschine?
4. Wie wird Wasser früh genug erreichbar, und wann folgt Lava?
5. Wie lässt sich die Grundversorgung nach Verlust oder Abbau einzelner Startgegenstände wiederherstellen?

Neue Materialien und Maschinenfunktionen entstehen in `mod/`. Passende Craftingrezepte kann Timo im Recipe Editor anlegen; deren tatsächlich generierte Dateien werden anschließend unter `pack/config/forever_recipe_editor/generated_pack/` übernommen.
