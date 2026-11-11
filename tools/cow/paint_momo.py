"""Draws Momo's texture atlas (1024 x 512): the Holstein coat of the cow with striped socks on the left half,
flat colours for the accessories and the name plate on the right half.

Usage: python3 paint_momo.py COW_SMOOTH_JSON COAT_PNG OUT_PNG
"""
import json
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

import momo_parts as mp

cow = json.load(open(sys.argv[1]))
coat = np.array(Image.open(sys.argv[2]).convert("RGB"), dtype=float)
size = coat.shape[0]
V = np.array(cow["verts"])

LEG_GROUPS = ("Leg", "Foot")
leg_amount = np.array([sum(v for g, v in w.items() if any(s in g for s in LEG_GROUPS)) for w in cow["weights"]])

# 3D position and leg amount of every texel, by rasterising the UV triangles
pos = np.zeros((size, size, 3))
leg = np.zeros((size, size))
filled = np.zeros((size, size), bool)
corners = cow["corners"]
for tri in cow["tris"]:
    ids = [corners[c][0] for c in tri]
    uv = np.array([corners[c][2] for c in tri]) * size
    x0, x1 = int(max(0, np.floor(uv[:, 0].min()))), int(min(size - 1, np.ceil(uv[:, 0].max())))
    y0, y1 = int(max(0, np.floor(uv[:, 1].min()))), int(min(size - 1, np.ceil(uv[:, 1].max())))
    a, b, c = uv
    den = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
    if abs(den) < 1e-9:
        continue
    for py in range(y0, y1 + 1):
        for px in range(x0, x1 + 1):
            x, y = px + 0.5, py + 0.5
            w0 = ((b[1] - c[1]) * (x - c[0]) + (c[0] - b[0]) * (y - c[1])) / den
            w1 = ((c[1] - a[1]) * (x - c[0]) + (a[0] - c[0]) * (y - c[1])) / den
            w2 = 1 - w0 - w1
            if min(w0, w1, w2) < -0.02:
                continue
            row = size - 1 - py   # image row 0 is the top, v = 0 the bottom
            pos[row, px] = w0 * V[ids[0]] + w1 * V[ids[1]] + w2 * V[ids[2]]
            leg[row, px] = w0 * leg_amount[ids[0]] + w1 * leg_amount[ids[1]] + w2 * leg_amount[ids[2]]
            filled[row, px] = True

lum = coat @ np.array([0.299, 0.587, 0.114])
bright = lum[filled & (lum > 90)]
white_ref = np.median(bright) if len(bright) else 150.0
shade = np.clip(lum / white_ref, 0.62, 1.0)[..., None]

PINK = np.array([238.0, 128.0, 166.0])
SOCK_WHITE = np.array([236.0, 236.0, 230.0])
out = coat.copy()

# striped socks: the lower legs from above the hoof up to the bows
z = pos[..., 2]
sock = filled & (leg > 0.5) & (z > 0.5) & (z < 1.44)
band = np.floor((z - 0.5) / 0.15).astype(int)
stripe = np.where((band % 2 == 0)[..., None], PINK, SOCK_WHITE)
out[sock] = (stripe * (0.55 + 0.45 * shade))[sock]

# no udder on a bull: the pink patch under the belly gets the coat of the belly
pinkish = (coat[..., 0] > coat[..., 1] * 1.22) & (coat[..., 0] > 80) & filled
belly = pinkish & (pos[..., 1] > -0.6) & (pos[..., 2] < 3.2) & (np.abs(pos[..., 0]) < 0.9)
out[belly] = SOCK_WHITE * (0.55 + 0.45 * shade[belly].squeeze(-1))[:, None] * 0.9

atlas = Image.new("RGB", (mp.ATLAS_W, mp.ATLAS_H), (30, 30, 30))
atlas.paste(Image.fromarray(np.clip(out, 0, 255).astype("uint8")), (0, 0))
draw = ImageDraw.Draw(atlas)
for name, (cx, cy, colour) in mp.SWATCHES.items():
    draw.rectangle([512 + cx * mp.CELL, cy * mp.CELL, 512 + (cx + 1) * mp.CELL - 1, (cy + 1) * mp.CELL - 1], fill=colour)

# name plate: gold, dark lettering and a heart, like the tag in the picture
x, y, w, h = mp.TAG_RECT
draw.rectangle([x, y, x + w - 1, y + h - 1], fill=(226, 172, 52))
draw.rectangle([x + 5, y + 5, x + w - 6, y + h - 6], outline=(150, 100, 20), width=4)
font = ImageFont.truetype("/usr/share/fonts/TTF/DejaVuSans-Bold.ttf", 62)
draw.text((x + w / 2, y + h * 0.42), "MOMO", font=font, fill=(70, 36, 10), anchor="mm")
heart = ImageFont.truetype("/usr/share/fonts/TTF/DejaVuSans-Bold.ttf", 40)
draw.text((x + w / 2, y + h * 0.80), "♥", font=heart, fill=(190, 40, 60), anchor="mm")
atlas.save(sys.argv[3])
print("wrote", sys.argv[3], "white reference", round(white_ref, 1))
