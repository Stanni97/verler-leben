# Changelog

## 1.6.0

- **Momo als Herdenvater:** Kälber, die nicht weiter als 20 m von Momo entfernt sind, wachsen um 50 % schneller heran (`[Bull] CalfGrowthBonusPercent`, `CalfGrowthRangeMetres`).
- **Kuh-Pflege:** Streichelst du eine zahme Kuh mit E, ist sie 15 Minuten zufrieden und gibt beim Melken 1 Milch mehr (`[Cow] PetMilkBonus`, `PetMemorySeconds`). Auch eine Kuh, die vor kurzem gefressen hat (in der ersten Hälfte der Sattzeit), gibt 1 Milch mehr (`WellFedMilkBonus`, `WellFedPercent`).
- **Butter, Butterbrot und Milchbrei:** Neue Gerichte aus Milch. Butter (3 Milch, ergibt 2) ist eine Zutat; Butterbrot (Brot und Butter) und Milchbrei (Gerstenmehl, Milch, Butter, Honig) sind Essen.
- **Gereifter Käse:** Käse im Fermenter reift (so lange wie Met) zu 2 gereiftem Käse mit besseren Werten (`[Kaese] RipenedPerCheese`).
- **Hauptmenü:** Auf dem Hauptmenü steht ein Bild von Momo (nur im eigenen Spiel, abschaltbar mit `[Screens] MenuBackground`).

## 1.5.1

- **Echte Kuh-Laute:** Die selbst erzeugten Klänge von 1.5.0 sind ersetzt durch echte Kuh-Aufnahmen von Wikimedia Commons ("Single Cow Moo" von MichaeltheFox8621, CC BY-SA 4.0, und "Mudchute cow 1" von Secretlondon, CC BY-SA 3.0), geschnitten und in der Tonhöhe verändert: höher fürs Kalb, tiefer für Momo. Die Quellen stehen im README unter "Credits".

## 1.5.0

- **Momo, der Zuchtbulle:** Ein Teil der Kälber (`[Bull] BullChancePercent`, Standard 25) wächst zu einem Bullen heran: Hörner, Blume, Halsband mit Glocke und Namensschild "MOMO" und rosa gestreifte Strümpfe. Er ist mindestens so stark wie ein Lox (1200 Leben, 1,5-facher Schaden), von Geburt an zahm, beschützt die Herde gegen Monster und greift keinen Spieler an, außer er wird zuerst geschlagen. Er lässt sich nicht melken, gibt aber 4 bis 6 Rindfleisch. Leben, Größe, Schaden und Wachreichweite stehen in der Config.
- **Kühe haben mehr Leben:** 250 statt 150. Steht der Wert noch auf 150, wird er einmal auf den neuen Standard gesetzt.
- **Füttern:** Gezähmte Kühe liefen nicht zum Futter auf dem Boden, wenn der Weg dorthin für einen Lox (2 m breit, 7 m hoch) zu eng war, zum Beispiel im Stall oder am Zaun. Sie suchen jetzt mit dem Wegenetz eines Pferdes. Nur wenn man das Futter direkt vor sie warf, fraßen sie es.
- **Muhen statt Lox-Grunzen:** Kuh, Kalb und Momo haben eigene Laute für Rufen, Aufmerksamkeit, Getroffenwerden, Streicheln und Sterben: Kühe muhen, das Kalb ruft hell, Momo brüllt tief. Die Klänge sind selbst erzeugt (`tools/make_sounds.py`), keine Aufnahmen. Die Laute sind für alle immer an, es gibt keine Einstellung dafür. Sie kommen vom Mod im jeweiligen Spiel, wer die Mod hat, hört sie.
- **Kein Lox-Fell mehr beim Treffer und Tod:** Beim Treffen und Töten spritzten die braunen Haarflocken des Lox ab (Partikel "lox_hair_particle" in den Treffer- und Todeseffekten). Kuh, Kalb und Momo bekommen jetzt Kopien dieser Effekte ohne die Haarflocken, Blut und Ton bleiben.
- **"Erworben durch Cheaten" bei Gegenständen dieser Mod:** Das Spiel gibt diese Markierung weiter: Alles, was aus markierten Zutaten oder an einer so gebauten Station entsteht, ist selbst markiert, und ein einziger markierter Stapel im Rucksack (zum Beispiel Stein oder ein Hammer von einem Konsolen-Test) reicht dafür. Die Markierung wanderte so in Dönerfleisch, Käse, Feste und alles Weitere, auch auf dem Server. Gegenstände dieser Mod verlieren die Markierung jetzt, sobald sie in ein Inventar oder eine Kiste kommen; sie geben sie dadurch auch nicht mehr weiter. Gegenstände des Spiels sind davon nicht betroffen.

## 1.4.2

- **Zucht einstellbar:** `BreedLimit` (ab so vielen Kühen und Kälbern im Umkreis von 20 m wird nicht mehr gezüchtet, Standard 0 = keine Obergrenze; Lox und Wolf: 4, Wildschwein: 5, Huhn: 10) und `BreedChancePercent` (Standard 50, eine Lox: 33; ein Versuch alle 30 Sekunden) im Abschnitt `[Cow]`.
- **Weniger Kühe in der Wildnis:** `SpawnMax` 2 (vorher 3), `SpawnChancePercent` 25 (vorher 50), `GroupMin` 1 (vorher 2) und `GroupMax` 2 (vorher 3). Werte, die noch auf dem alten Standard stehen, werden beim Start einmal auf die neuen gesetzt.

- **Kühe fressen Futter vom Boden:** Eine hungrige, gezähmte Kuh sucht jetzt im Umkreis von 15 m (vorher 10 m) alle 3 Sekunden (vorher 10) nach Gerste und Flachs. War sie alarmiert oder hatte sie ein Ziel (zum Beispiel ein vorbeigelaufenes Tier), hat das Spiel die Futtersuche übersprungen; das wird jetzt zurückgesetzt, weil sie nicht kämpfen kann. Im Log steht dann eine Zeile "Cow is hungry and ... lies ... m away".

## 1.4.1

- **Essbar:** Dönerfleisch (45 Leben, 25 Ausdauer), Käse (30 Leben, 40 Ausdauer) und Milch (12 Leben, 30 Ausdauer) lassen sich auch einzeln essen. Die Werte stehen in der Config und können dort auf 0 gesetzt werden. Rindfleisch bleibt roh und nicht essbar.
- Die Tooltips dieser Zutaten zeigten Werte von Vanilla-Gegenständen (gekochtes Loxfleisch, Brot), weil sie von Loxfleisch und Teig den Verweis auf das gekochte Gegenstück erbten. Jetzt zeigen sie ihre eigenen Werte, Rindfleisch keine.
- Die rohen und gebackenen Gerichte zeigen jetzt die Werte des fertigen Feasts an (Leben, Ausdauer, Regeneration, Dauer, auch vom Server), statt der Werte der Vanilla-Pastete.
- README: Der Bifrost Updater wird nicht mehr erwähnt. Updates laufen über den Mod-Manager oder die Release-Seite.

## 1.4.0

- **Server-Config:** Auf einem Server mit dem Mod gelten seine Werte (Rezepte, Essen, Boonekamp, Kuh) für alle Spieler. Beim Betreten schickt der Server sie, beim Verlassen gelten wieder die eigenen. Ohne Server zählt wie bisher die eigene Config. Nur die Melk-Taste (`MilkKey`) bleibt immer die eigene.
- Geänderte Werte wirken sofort, ohne Neustart: Rezepte, Werte der Gerichte und des Boonekamp, Backzeit, Portionen und die Kuh (Spawn, Zähmen, Wachsen, Größe neuer Kühe).
- `Enabled = false` entfernt jetzt das Rezept (und das Feast aus dem Servierteller), statt den Gegenstand ganz aus dem Spiel zu nehmen. Bei der Kuh heißt es: Es spawnen keine neuen Kühe.
- Die Config wird beim Update aufgeräumt: Werte, die noch einem alten Standardwert entsprechen (zum Beispiel das Dönerfleisch oder der Käse aus Loxfleisch), werden durch die aktuellen Rezepte und Werte ersetzt. Eigene Änderungen bleiben unverändert. Im Log steht, wie viele Werte ersetzt wurden. Die Config-Version steht unter `[General]` (`ConfigVersion`).

## 1.3.1 (nicht veröffentlicht)

- In 1.4.0 enthalten.
- Die Config wird beim Update aufgeräumt: Werte, die noch einem alten Standardwert entsprechen (zum Beispiel das Dönerfleisch oder der Käse aus Loxfleisch), werden durch die aktuellen Rezepte und Werte ersetzt. Eigene Änderungen bleiben unverändert. Im Log steht, wie viele Werte ersetzt wurden. Die Config-Version steht unter `[General]` (`ConfigVersion`).

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
