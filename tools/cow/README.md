# Kuh-Modell

`Assets/cow_mesh.bin` und die Fell-Texturen `Assets/cow_schwarzbunt.png`, `cow_rotbunt.png` und `cow_braun.png`
sind die Kuh aus dem **Farm Animals Pack von Quaternius** (CC0 1.0,
<https://opengameart.org/content/lowpoly-animated-farm-animal-pack>), geglättet, mit gebackenen Fell-Texturen und
ausgerichtet auf das Skelett des Lox aus Valheim.

`cow_smooth.json` (geglättete Kuh mit Normalen, UVs und Knochengewichten) und `loxskel.json` (Knochen des Lox) liegen
schon hier. Für eine neue Ausrichtung reicht deshalb Schritt 3.

1. Geglättete Kuh, UVs und Fell-Texturen aus `Cow.blend` des Pakets erzeugen (Blender, Cycles auf der CPU):
   `blender -b --disable-autoexec Cow.blend --python bake_cow.py -- cow_smooth.json ../../Assets 512`
2. Lox-Skelett aus den Spieldaten auslesen (braucht UnityPy und numpy):
   `python3 export_lox_skeleton.py ~/.local/share/Steam/steamapps/common/Valheim loxskel.json`
3. Ausrichten und schreiben:
   `python3 build_cow.py cow_smooth.json loxskel.json ../../Assets/cow_mesh.bin preview.json`
4. Vorschau rendern: `blender -b --factory-startup --python preview.py -- preview.json ../../Assets/cow_schwarzbunt.png prev`

**bake_cow.py** glättet die Kuh (eine Unterteilungsstufe), legt UVs an und backt drei Felle: Der Körper bekommt
unregelmäßige Flecken aus 3D-Rauschen, der Kopf behält das Muster des Modells, Hufe und Schnauze ihre Farbe. Dazu
kommen Fellstruktur, ein etwas dunklerer Bauch und gebackene Umgebungsverdeckung (AO).

**build_cow.py** skaliert die Kuh auf den Beinabstand des Lox, spiegelt X wie Unitys FBX-Import (und dreht dafür die
Dreiecke um) und ordnet ihre Knochen den Lox-Knochen zu. Weil der Lox breiter steht und die Beine tiefer beugt, wird
die Kuh etwas breiter (`WIDTH`), die Beine rücken auseinander (`SPREAD`), und die Beine werden nach der Höhe der
Lox-Gelenke gewichtet. Im Spiel richtet `CowLegs` die Beine nach jeder Animation gerade aus.
