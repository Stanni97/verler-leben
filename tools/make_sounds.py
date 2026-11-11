"""Synthesises the cow, calf and bull voices (Assets/cow_*.wav, calf_*.wav, bull_*.wav): a glottal tone whose harmonics are
shaped by moving vowel formants (a nasal "m" that opens to "oo"/"o"), with pitch glide, vibrato, roughness and breath noise.
No recordings are used, so there is nothing to license.

Usage: python3 make_sounds.py OUT_DIR [--spectrograms DIR]   (needs numpy; Pillow for the spectrogram images)
"""
import sys
import wave

import numpy as np

SR = 22050


def curve(points, n):
    t = np.linspace(0, 1, n)
    xs = [p[0] for p in points]
    return np.stack([np.interp(t, xs, [p[1][i] if isinstance(p[1], (tuple, list)) else p[1] for p in points])
                     for i in range(len(points[0][1]) if isinstance(points[0][1], (tuple, list)) else 1)])


def voice(dur, f0, formants, amp, nasal=0.0, nasal_time=0.25, rough=0.0, rough_hz=32.0, noise=0.0, seed=1, bw=(110, 150, 250)):
    rng = np.random.default_rng(seed)
    n = int(dur * SR)
    t = np.arange(n) / SR
    pitch = curve(f0, n)[0]
    # vibrato and a slow random wobble make it breathe
    wobble = np.cumsum(rng.normal(0, 1, n)); wobble = np.convolve(wobble, np.ones(2000) / 2000, "same")
    wobble = wobble / (np.abs(wobble).max() + 1e-9)
    pitch = pitch * (1 + 0.012 * np.sin(2 * np.pi * 5.2 * t) + 0.02 * wobble)
    phase = 2 * np.pi * np.cumsum(pitch) / SR
    F = curve(formants, n)
    sig = np.zeros(n)
    nasal_amount = np.clip(1 - t / max(nasal_time * dur, 1e-6), 0, 1) * nasal
    for h in range(1, 60):
        fh = h * pitch
        if fh.max() < 40:
            continue
        gain = np.full(n, 0.03)
        for k in range(3):
            gain += (1.0 if k == 0 else 0.55 if k == 1 else 0.2) * np.exp(-0.5 * ((fh - F[k]) / bw[k]) ** 2)
        gain *= h ** -1.05
        gain *= np.where(fh < 0.45 * SR, 1.0, 0.0)
        # "m": the nose keeps only the lowest harmonics
        gain *= 1 - nasal_amount * np.clip((fh - 350) / 600, 0, 1) * 0.92
        sig += gain * np.sin(h * phase + rng.uniform(0, 6.28))
    env = curve(amp, n)[0]
    if rough > 0:
        sig *= 1 - rough * (0.5 + 0.5 * np.sin(2 * np.pi * rough_hz * t + 0.8 * np.sin(2 * np.pi * 3.1 * t)))
    if noise > 0:
        breath = rng.normal(0, 1, n)
        breath = np.convolve(breath, np.ones(6) / 6, "same")          # a little low-passed
        sig += noise * breath * np.abs(sig).max() * 1.2
    sig *= env
    sig = np.tanh(1.6 * sig / (np.abs(sig).max() + 1e-9)) / np.tanh(1.6)
    fade = np.minimum(1, np.minimum(t / 0.01, (dur - t) / 0.04))
    return 0.9 * sig * fade


O_NASAL = (300, 900, 2200)
OO = (360, 760, 2300)
O = (480, 900, 2350)
AH = (640, 1150, 2500)
EH = (650, 1650, 2700)
UU = (330, 650, 2100)

CLIPS = {
    # cow
    "cow_idle1": dict(dur=1.7, f0=[(0, 112), (0.18, 132), (0.55, 150), (0.85, 128), (1, 100)],
                      formants=[(0, O_NASAL), (0.25, OO), (0.6, O), (1, UU)], amp=[(0, 0), (0.1, 0.8), (0.5, 1), (0.85, 0.8), (1, 0)],
                      nasal=1.0, rough=0.06, noise=0.02, seed=3),
    "cow_idle2": dict(dur=1.15, f0=[(0, 128), (0.3, 158), (1, 118)],
                      formants=[(0, O_NASAL), (0.3, O), (1, OO)], amp=[(0, 0), (0.12, 0.9), (0.6, 1), (1, 0)],
                      nasal=0.9, rough=0.05, noise=0.02, seed=5),
    "cow_alert": dict(dur=0.6, f0=[(0, 150), (0.35, 196), (1, 150)],
                      formants=[(0, O), (0.4, AH), (1, O)], amp=[(0, 0), (0.08, 1), (0.6, 0.9), (1, 0)],
                      nasal=0.3, rough=0.1, noise=0.04, seed=7),
    "cow_hit1": dict(dur=0.5, f0=[(0, 215), (1, 150)], formants=[(0, AH), (1, O)], amp=[(0, 0), (0.05, 1), (0.5, 0.8), (1, 0)],
                     rough=0.2, noise=0.12, seed=9),
    "cow_hit2": dict(dur=0.4, f0=[(0, 185), (1, 135)], formants=[(0, EH), (1, O)], amp=[(0, 0), (0.05, 1), (1, 0)],
                     rough=0.2, noise=0.1, seed=11),
    "cow_death": dict(dur=2.3, f0=[(0, 165), (0.25, 178), (0.6, 120), (1, 62)],
                      formants=[(0, AH), (0.4, O), (1, UU)], amp=[(0, 0), (0.06, 1), (0.6, 0.85), (1, 0)],
                      rough=0.28, rough_hz=26, noise=0.05, seed=13),
    # calf
    "calf_idle1": dict(dur=0.85, f0=[(0, 255), (0.4, 335), (1, 250)], formants=[(0, O), (0.4, EH), (1, O)],
                       amp=[(0, 0), (0.1, 1), (0.7, 0.9), (1, 0)], nasal=0.5, noise=0.03, seed=15, bw=(130, 180, 280)),
    "calf_idle2": dict(dur=0.6, f0=[(0, 290), (0.5, 360), (1, 280)], formants=[(0, EH), (1, AH)],
                       amp=[(0, 0), (0.1, 1), (1, 0)], noise=0.03, seed=17, bw=(130, 180, 280)),
    "calf_hit": dict(dur=0.32, f0=[(0, 400), (1, 300)], formants=[(0, EH), (1, AH)], amp=[(0, 0), (0.05, 1), (1, 0)],
                     rough=0.15, noise=0.08, seed=19, bw=(130, 180, 280)),
    "calf_death": dict(dur=1.1, f0=[(0, 330), (0.5, 280), (1, 190)], formants=[(0, EH), (1, O)],
                       amp=[(0, 0), (0.06, 1), (0.7, 0.8), (1, 0)], rough=0.18, noise=0.05, seed=21, bw=(130, 180, 280)),
    # bull
    "bull_idle1": dict(dur=2.0, f0=[(0, 68), (0.3, 92), (0.7, 80), (1, 58)], formants=[(0, (250, 600, 1900)), (0.3, UU), (1, (300, 620, 2000))],
                       amp=[(0, 0), (0.12, 0.9), (0.55, 1), (1, 0)], nasal=0.8, rough=0.3, rough_hz=30, noise=0.06, seed=23),
    "bull_idle2": dict(dur=1.4, f0=[(0, 78), (0.4, 100), (1, 66)], formants=[(0, UU), (1, OO)],
                       amp=[(0, 0), (0.1, 1), (1, 0)], rough=0.32, rough_hz=34, noise=0.06, seed=25),
    "bull_alert": dict(dur=1.0, f0=[(0, 86), (0.3, 118), (1, 72)], formants=[(0, O), (0.3, AH), (1, O)],
                       amp=[(0, 0), (0.06, 1), (0.7, 0.9), (1, 0)], rough=0.4, rough_hz=38, noise=0.12, seed=27),
    "bull_hit": dict(dur=0.55, f0=[(0, 100), (1, 60)], formants=[(0, AH), (1, O)], amp=[(0, 0), (0.04, 1), (1, 0)],
                     rough=0.45, rough_hz=40, noise=0.2, seed=29),
    "bull_death": dict(dur=2.6, f0=[(0, 104), (0.2, 118), (0.6, 80), (1, 44)], formants=[(0, AH), (0.5, O), (1, UU)],
                       amp=[(0, 0), (0.05, 1), (0.6, 0.85), (1, 0)], rough=0.42, rough_hz=28, noise=0.1, seed=31),
}


def save(path, data):
    pcm = (np.clip(data, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


def spectrogram(path, data):
    from PIL import Image
    win, hop = 1024, 256
    frames = [np.abs(np.fft.rfft(data[i:i + win] * np.hanning(win))) for i in range(0, len(data) - win, hop)]
    s = np.log1p(np.array(frames).T[:120] * 30)
    s = (255 * s / s.max()).astype("uint8")[::-1]
    Image.fromarray(s).resize((s.shape[1], 240)).save(path)


if __name__ == "__main__":
    out = sys.argv[1]
    shots = sys.argv[sys.argv.index("--spectrograms") + 1] if "--spectrograms" in sys.argv else None
    for name, params in CLIPS.items():
        data = voice(**params)
        save(f"{out}/{name}.wav", data)
        if shots:
            spectrogram(f"{shots}/{name}.png", data)
        print(name, round(len(data) / SR, 2), "s")
