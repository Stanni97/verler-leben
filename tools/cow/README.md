# Kuh-Modell

`Assets/cow_mesh.bin` und `Assets/cowpalette.png` sind die Kuh aus dem **Farm Animals Pack von Quaternius**
(CC0 1.0, <https://opengameart.org/content/lowpoly-animated-farm-animal-pack>), ausgerichtet auf das Skelett
des Lox aus Valheim.

Neu erzeugen:

1. `Cow.blend` aus dem Paket herunterladen.
2. Mesh, Materialien und Knochengewichte exportieren:
   `blender -b --disable-autoexec Cow.blend --python export_cow.py -- cow_src.json`
3. Lox-Skelett aus den Spieldaten auslesen (`loxskel.json` mit Knochen und Bind-Positionen des Lox-Körpers, z. B. mit UnityPy).
4. Ausrichten und schreiben:
   `python3 build_cow.py cow_src.json loxskel.json ../../Assets/cow_mesh.bin ../../Assets/cowpalette.png preview.json`
5. Vorschau rendern: `blender -b --factory-startup --python preview.py -- preview.json prev 1`

`build_cow.py` skaliert die Kuh auf den Beinabstand des Lox, spiegelt X wie Unitys FBX-Import und ordnet ihre
Knochen den Lox-Knochen zu (Kopf, Hals, Rumpf, Beine). Die Kuh bewegt sich dann mit den Lox-Animationen.
