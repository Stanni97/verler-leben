# Laute von Kuh, Kalb und Bulle

`make_sounds.py` erzeugt die WAV-Dateien in `Assets/` (cow_*, calf_*, bull_*) rein rechnerisch: ein Stimmton, dessen Obertöne von
wandernden Vokal-Formanten geformt werden (nasales "m" öffnet sich zu "oo"/"o"), dazu Tonhöhenverlauf, Vibrato, Rauheit und
Atemrauschen. Es werden keine Aufnahmen verwendet.

```bash
python3 tools/make_sounds.py Assets --spectrograms /tmp/spec   # numpy, für die Bilder Pillow
```

## Real recordings (replace the synthesised sounds)

`Assets/cow_*.wav`, `calf_*.wav` and `bull_*.wav` are made by `tools/make_real_sounds.sh` from two recordings on Wikimedia Commons:

- ["Single Cow Moo.ogg"](https://commons.wikimedia.org/wiki/File:Single_Cow_Moo.ogg) by MichaeltheFox8621, [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/)
- ["Mudchute cow 1.ogg"](https://commons.wikimedia.org/wiki/File:Mudchute_cow_1.ogg) by Secretlondon, [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/)

They are cut, faded, normalised and pitch-shifted (calf higher, Momo lower). The resulting sound files are shared under the same licences (CC BY-SA). The old synthesised versions are in `tools/make_sounds.py`.
