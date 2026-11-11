# Laute von Kuh, Kalb und Bulle

`make_sounds.py` erzeugt die WAV-Dateien in `Assets/` (cow_*, calf_*, bull_*) rein rechnerisch: ein Stimmton, dessen Obertöne von
wandernden Vokal-Formanten geformt werden (nasales "m" öffnet sich zu "oo"/"o"), dazu Tonhöhenverlauf, Vibrato, Rauheit und
Atemrauschen. Es werden keine Aufnahmen verwendet.

```bash
python3 tools/make_sounds.py Assets --spectrograms /tmp/spec   # numpy, für die Bilder Pillow
```
