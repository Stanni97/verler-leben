# Changelog

## 1.3.0

- Neues Feast **Mantaplatte** (Currywurst und Pommes mit Mayo): 85 Leben, 65 Ausdauer, 65 Minuten. Roh am Zubereitungstisch aus 6 Würsten, 4 Karotten, 3 Himbeeren, 2 Eiern und 1 Berg-Gewürz, dann im Steinofen backen. Zählt auch für den Feast-Bonus des Boonekamp.
- Neuer Snack **Entenbox** von der Kirmes (frittierte Teigbällchen mit Zimtzucker und Erdbeersoße) am Kessel aus 1 Brotteig, 1 Honig und 2 Himbeeren. Gibt 45 Eitr, 15 Leben und 20 Ausdauer für 15 Minuten.

## 1.2.2

- Die Kühe leben jetzt in den **Ebenen** statt in den Wiesen, dort wachsen auch Gerste und Flachs, ihr Futter. In der Config unter `[Cow]` stellt `SpawnBiome` das Biom ein (Plains, Meadows, BlackForest, Swamp, Mountain, Mistlands). Schon vorhandene Kühe in den Wiesen bleiben, wo sie sind.
- Zahme Kühe folgen dir jetzt: **E** wechselt zwischen Folgen und Warten, wie beim Wolf. Dafür ist das **Melken** auf eine eigene Taste gewandert, standardmäßig **H** (Config: `MilkKey` unter `[Cow]`). Der Hovertext zeigt beide Tasten.
- Behoben: Beim Töten verwandelte sich die Kuh in einen Lox, weil die Todesanimation noch den Lox-Körper benutzte. Kuh und Kalb haben jetzt eigene Ragdolls mit dem Kuhmodell und demselben Fell.

## 1.2.1

- Dönerfleisch wird aus 1 Rindfleisch, 1 gebratenem Loxfleisch und 1 Berg-Gewürz gemacht (statt 2 gebratenem Wolfsfleisch).
- Grund: Mit 2 gebratenem Wolfsfleisch war das Dönerfleisch sonst ein bisschen zu stark (zu OP).
- Bestehende Configs behalten den alten Wert. Zum Übernehmen in `BepInEx/config/felix.verlerleben.cfg` unter `[Doener]` die Zeile `Ingredients` anpassen oder die Datei löschen.

## 1.2.0

- **Kuh** in den Wiesen mit eigenem Kuhmodell (Quaternius, CC0, geglättet, mit Fell-Texturen in Schwarzbunt, Rotbunt und Braun) auf dem Lox-Skelett, harmlos (kein Angriff, kein Zerstören von Zäunen). Zähmen mit Gerste oder Flachs, Züchten über Kälber, **Melken** durch Streicheln einmal pro Tag. Neue Items: Milch und Rindfleisch.
- Käse wird jetzt aus 2 Milch gemacht (ergibt 3), Dönerfleisch aus 1 Rindfleisch, 2 gebratenem Wolfsfleisch und 1 Berg-Gewürz (ergibt 3). Teller und Pizza brauchen wieder die größeren Mengen mit Zwiebeln, Jotunn- und Magecap-Pilzen, Wolkenbeeren und Berg-Gewürz.
- **Schroeders Boonekamp** am Kessel: Verdauungsschnaps mit +25 % Lebens- und +15 % Ausdauer-Regeneration. Zusatzboni, die sich addieren: mit Teller oder Pizza als aktivem Essen +15 % / +10 %, mit irgendeinem Met +10 % / +8 %, mit gebrautem Gerstenwein (dem Bier des Spiels) +15 % / +10 %. Höchstens +65 % / +43 %.
- Neue Abhängigkeit im Code: Harmony (liegt im BepInExPack).
- Bestehende Configs behalten ihre alten Werte. Zum Übernehmen `BepInEx/config/felix.verlerleben.cfg` löschen.

## 1.1.1

- Einfacher zu farmen, fast alle Zutaten lassen sich züchten oder anbauen:
  - Dönerfleisch: 1 gebratenes Loxfleisch, 1 gebratenes Wolfsfleisch, 1 Zwiebel (ergibt 3), ohne Gewürz.
  - Käse: 2 Gerste (ergibt 2), Gerstenkäse statt Loxfleisch.
  - Kaunitzer Teller: 4 Dönerfleisch, 3 Käse, 4 Karotten, 1 Jotunn-Pilz, 1 Berg-Gewürz.
  - Spexarder Pizza: 3 Brotteig, 6 Dönerfleisch, 4 Käse, 2 Zwiebeln, 1 Magecap-Pilz, 1 Berg-Gewürz.
- Das Ebenen-Gewürz und das Nebelwelt-Gewürz werden nicht mehr gebraucht, nur noch 1 Berg-Gewürz pro Feast.
- Bestehende Configs behalten ihre alten Werte. Zum Übernehmen `BepInEx/config/felix.verlerleben.cfg` löschen.

## 1.1.0

- Stärkere Feasts: Kaunitzer Teller 100 Leben / 50 Ausdauer (60 Min.), Spexarder Pizza 80 / 80 (80 Min.).
- Aufwendigere Rezepte mit Zutaten aus Ebenen, Bergen und Nebelwelt: Dönerfleisch braucht jetzt auch gebratenes Wolfsfleisch und Nebelwelt-Gewürz (ergibt 3). Der Teller braucht zusätzlich Zwiebeln und Jotunn-Pilze, die Pizza Wolkenbeeren statt Himbeeren, Magecap-Pilze und Zwiebeln.
- Bestehende Configs behalten ihre alten Werte. Zum Übernehmen die Datei `BepInEx/config/felix.verlerleben.cfg` löschen, sie wird neu erzeugt.

## 1.0.0

- Erste Version: Spexarder Pizza und Kaunitzer Teller als Feasts für den Servierteller,
  dazu die Zutaten Dönerfleisch und Käse. Die Gerichte werden roh zubereitet und im Steinofen gebacken.
