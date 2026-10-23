# Kuh-Modell

`Assets/cow_mesh.bin` und `Assets/cowpalette.png` sind die Kuh aus dem **Farm Animals Pack von Quaternius**
(CC0 1.0, <https://opengameart.org/content/lowpoly-animated-farm-animal-pack>), ausgerichtet auf das Skelett
des Lox aus Valheim.

Neu erzeugen:

1. `Cow.blend` aus dem Paket herunterladen.
2. Mesh, Materialien und Knochengewichte exportieren:
   `blender -b --disable-autoexec Cow.blend --python export_cow.py -- cow_src.json`
3. Lox-Skelett aus den Spieldaten auslesen (braucht UnityPy und numpy):
   `python3 export_lox_skeleton.py ~/.local/share/Steam/steamapps/common/Valheim loxskel.json`

`cow_src.json` und `loxskel.json` liegen schon hier, die Schritte 1 bis 3 sind nur nötig, wenn sich das Modell oder der Lox ändert.
4. Ausrichten und schreiben:
   `python3 build_cow.py cow_src.json loxskel.json ../../Assets/cow_mesh.bin ../../Assets/cowpalette.png preview.json`
5. Vorschau rendern: `blender -b --factory-startup --python preview.py -- preview.json prev 1`

`build_cow.py` skaliert die Kuh auf den Beinabstand des Lox, spiegelt X wie Unitys FBX-Import und ordnet ihre
Knochen den Lox-Knochen zu (Kopf, Hals, Rumpf, Beine). Weil der Lox breiter steht und die Beine tiefer beugt, wird
die Kuh etwas breiter gemacht (`WIDTH`), die Beine rücken auseinander (`SPREAD`), und die Beine werden nach der Höhe
der Lox-Gelenke gewichtet. Die Kuh bewegt sich dann mit den Lox-Animationen.
