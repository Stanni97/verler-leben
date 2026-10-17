"""Draws the item icons for Verler Leben.

Usage: python3 make_icons.py OUT_DIR [SIZE]
  Writes doener.png, kaese.png, teller.png, tellerraw.png, pizza.png and pizzaraw.png.
  SIZE defaults to 128 (item icons).

Everything is drawn from simple shapes. The baked dishes follow photos of the real thing:
the Spexarder Pizza is round with a golden crust, thick melted cheese and dark chunks of
Doener meat; the Kaunitzer Teller is an aluminium tray with overbaked cheese and burnt edges.
"""
import math
import os
import random
import sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter

OUT_DIR = sys.argv[1]
SIZE = int(sys.argv[2]) if len(sys.argv) > 2 else 128
W = H = 512                # drawn at 512 and downscaled for anti-aliasing


def new_layer():
    return Image.new("RGBA", (W, H), (0, 0, 0, 0))


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(len(a)))


def blob(d, cx, cy, rx, ry, fill, rng, bumps=9, jitter=0.28, rot=None):
    """Irregular rounded shape, used for cheese patches and meat chunks."""
    rot = rng.uniform(0, math.tau) if rot is None else rot
    phases = [rng.uniform(0, math.tau) for _ in range(3)]
    pts = []
    for i in range(36):
        a = i / 36 * math.tau
        wob = 1 + jitter * (math.sin(a * bumps / 3 + phases[0]) * 0.5 + math.sin(a * 2 + phases[1]) * 0.35
                            + math.sin(a * 5 + phases[2]) * 0.15)
        x, y = math.cos(a) * rx * wob, math.sin(a) * ry * wob
        pts.append((cx + x * math.cos(rot) - y * math.sin(rot), cy + x * math.sin(rot) + y * math.cos(rot)))
    d.polygon(pts, fill=fill)


def clip(layer, mask):
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    out.paste(layer, (0, 0), ImageChops.multiply(mask, layer.getchannel("A")))
    return out


def ellipse_mask(box):
    m = Image.new("L", (W, H), 0)
    ImageDraw.Draw(m).ellipse(box, fill=255)
    return m


def shade(layer, strength, light=(0.35, 0.3)):
    """Soft darkening towards the lower right so the flat drawing gets some volume."""
    px = Image.new("L", (W, H), 255)
    p = px.load()
    for y in range(H):
        for x in range(W):
            t = ((x / W - light[0]) + (y / H - light[1])) / 1.1
            p[x, y] = int(255 * (1 - strength * max(0.0, t) ** 1.5))
    rgb = ImageChops.multiply(layer.convert("RGB"), Image.merge("RGB", (px, px, px)))
    return Image.merge("RGBA", (*rgb.split(), layer.getchannel("A")))


def draw_pizza(baked):
    rng = random.Random(7 if baked else 8)
    cx, cy, rx, ry = 256, 262, 214, 170          # slightly tilted view of a round pizza
    layer = new_layer()
    d = ImageDraw.Draw(layer)

    if baked:  # white plate with a soft rim
        d.ellipse([cx - rx - 26, cy - ry - 18, cx + rx + 26, cy + ry + 26], fill=(226, 228, 232, 255))
        d.ellipse([cx - rx - 12, cy - ry - 8, cx + rx + 12, cy + ry + 14], fill=(246, 247, 249, 255))

    crust = (214, 138, 52) if baked else (232, 214, 176)
    crust_dark = (150, 80, 28) if baked else (205, 184, 142)
    d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=crust_dark)
    d.ellipse([cx - rx + 4, cy - ry + 3, cx + rx - 4, cy + ry - 8], fill=crust)

    # tomato sauce, visible where the cheese does not reach
    inner = [cx - rx + 38, cy - ry + 30, cx + rx - 38, cy + ry - 38]
    d.ellipse(inner, fill=(196, 74, 34) if baked else (206, 72, 48))
    mask = ellipse_mask(inner)

    top = new_layer()
    td = ImageDraw.Draw(top)
    if baked:
        # thick, melted, golden cheese
        for _ in range(46):
            a, r = rng.uniform(0, math.tau), rng.uniform(0, 1) ** 0.6
            x, y = cx + math.cos(a) * (rx - 50) * r, cy + math.sin(a) * (ry - 44) * r
            c = lerp((246, 222, 140), (252, 240, 190), rng.random())
            blob(td, x, y, rng.uniform(36, 62), rng.uniform(26, 44), c + (255,), rng, jitter=0.35)
        # browned spots on the cheese
        for _ in range(16):
            a, r = rng.uniform(0, math.tau), rng.uniform(0.1, 1) ** 0.6
            x, y = cx + math.cos(a) * (rx - 55) * r, cy + math.sin(a) * (ry - 50) * r
            blob(td, x, y, rng.uniform(10, 20), rng.uniform(8, 14), (222, 160, 70, 200), rng)
        # dark roasted Doener chunks
        for _ in range(15):
            a, r = rng.uniform(0, math.tau), rng.uniform(0, 1) ** 0.7
            x, y = cx + math.cos(a) * (rx - 62) * r, cy + math.sin(a) * (ry - 55) * r
            blob(td, x, y, rng.uniform(15, 27), rng.uniform(10, 18), (92, 54, 30, 255), rng, jitter=0.4)
            blob(td, x - 3, y - 3, rng.uniform(7, 13), rng.uniform(5, 9), (140, 88, 48, 255), rng, jitter=0.4)
        # a few burnt bits and oregano
        for _ in range(5):
            a, r = rng.uniform(0, math.tau), rng.uniform(0.5, 1)
            td.ellipse([cx + math.cos(a) * (rx - 70) * r - 7, cy + math.sin(a) * (ry - 60) * r - 5,
                        cx + math.cos(a) * (rx - 70) * r + 7, cy + math.sin(a) * (ry - 60) * r + 5],
                       fill=(46, 28, 20, 255))
        for _ in range(60):
            a, r = rng.uniform(0, math.tau), rng.uniform(0, 1) ** 0.6
            x, y = cx + math.cos(a) * (rx - 60) * r, cy + math.sin(a) * (ry - 52) * r
            td.ellipse([x - 3, y - 2, x + 3, y + 2], fill=(110, 130, 50, 255))
    else:
        # raw: pale shredded cheese and pink Doener slices on the sauce
        for _ in range(26):
            a, r = rng.uniform(0, math.tau), rng.uniform(0, 1) ** 0.6
            x, y = cx + math.cos(a) * (rx - 55) * r, cy + math.sin(a) * (ry - 48) * r
            blob(td, x, y, rng.uniform(20, 36), rng.uniform(12, 20), (250, 242, 206, 255), rng, jitter=0.4)
        for _ in range(13):
            a, r = rng.uniform(0, math.tau), rng.uniform(0, 1) ** 0.7
            x, y = cx + math.cos(a) * (rx - 62) * r, cy + math.sin(a) * (ry - 55) * r
            blob(td, x, y, rng.uniform(15, 24), rng.uniform(9, 15), (168, 104, 84, 255), rng, jitter=0.4)
    layer.alpha_composite(clip(top, mask))

    # puffy crust ring: lighter bumps along the edge
    ring = new_layer()
    rd = ImageDraw.Draw(ring)
    for i in range(34):
        a = i / 34 * math.tau
        x, y = cx + math.cos(a) * (rx - 16), cy + math.sin(a) * (ry - 14)
        rd.ellipse([x - 24, y - 17, x + 24, y + 17], fill=lerp(crust, (240, 190, 100) if baked else (246, 232, 204), 0.5) + (150,))
    cut = ellipse_mask(inner)
    ring.putalpha(ImageChops.multiply(ring.getchannel("A"), ImageChops.invert(cut)))
    layer.alpha_composite(ring)
    if baked:
        for a in (2.4, 3.4, 0.5):  # charred crust spots
            d.ellipse([cx + math.cos(a) * (rx - 14) - 15, cy + math.sin(a) * (ry - 12) - 9,
                       cx + math.cos(a) * (rx - 14) + 15, cy + math.sin(a) * (ry - 12) + 9], fill=(92, 50, 24, 230))
    return shade(layer, 0.28)


def draw_tray(baked):
    """Aluminium tray in a three-quarter view, filled with Doener, fries and cheese."""
    rng = random.Random(11 if baked else 12)
    layer = new_layer()
    d = ImageDraw.Draw(layer)

    # parallelogram-ish tray: back edge higher and shifted right
    bl, br, fr, fl = (92, 214), (394, 168), (440, 340), (120, 400)   # back-left, back-right, front-right, front-left
    foil = (196, 202, 212)
    d.polygon([bl, br, fr, fl], fill=(120, 126, 138))                                  # tray wall
    # front wall height
    d.polygon([fl, fr, (fr[0], fr[1] + 38), (fl[0], fl[1] + 40)], fill=(170, 176, 188))
    d.polygon([fl, (fl[0], fl[1] + 40), (fl[0] - 10, fl[1] + 28), (bl[0] - 8, bl[1] + 22)], fill=(150, 156, 168))
    for i in range(1, 12):                                                             # crinkled foil rim
        t = i / 12
        x, y = fl[0] + (fr[0] - fl[0]) * t, fl[1] + (fr[1] - fl[1]) * t
        d.line([x, y + 2, x, y + 38], fill=(188, 194, 206) if i % 2 else (150, 156, 168), width=5)

    # contents: slightly inset quadrilateral
    inset = 22
    cb = [(bl[0] + inset, bl[1] + inset), (br[0] - inset, br[1] + inset), (fr[0] - inset, fr[1] - inset), (fl[0] + inset, fl[1] - inset)]
    base = (226, 188, 96) if baked else (238, 226, 188)
    d.polygon(cb, fill=base)
    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).polygon(cb, fill=255)

    top = new_layer()
    td = ImageDraw.Draw(top)

    def pt(u, v):
        a = (cb[0][0] + (cb[1][0] - cb[0][0]) * u, cb[0][1] + (cb[1][1] - cb[0][1]) * u)
        b = (cb[3][0] + (cb[2][0] - cb[3][0]) * u, cb[3][1] + (cb[2][1] - cb[3][1]) * u)
        return a[0] + (b[0] - a[0]) * v, a[1] + (b[1] - a[1]) * v

    if baked:
        for _ in range(34):                                    # melted cheese
            x, y = pt(rng.uniform(0, 1), rng.uniform(0, 1))
            c = lerp((244, 222, 140), (252, 242, 196), rng.random())
            blob(td, x, y, rng.uniform(30, 52), rng.uniform(20, 34), c + (255,), rng, jitter=0.35)
        for _ in range(12):                                    # Doener chunks peeking out
            x, y = pt(rng.uniform(0.05, 0.95), rng.uniform(0.05, 0.95))
            blob(td, x, y, rng.uniform(13, 22), rng.uniform(9, 14), (112, 70, 44, 255), rng, jitter=0.4)
            blob(td, x - 3, y - 3, rng.uniform(6, 11), rng.uniform(4, 8), (160, 110, 72, 255), rng, jitter=0.4)
        for _ in range(14):                                    # brown baked spots
            x, y = pt(rng.uniform(0, 1), rng.uniform(0, 1))
            blob(td, x, y, rng.uniform(9, 18), rng.uniform(7, 12), (214, 150, 66, 210), rng)
        for _ in range(7):                                     # burnt bits, mostly at the edges
            u, v = rng.choice([(rng.uniform(0, 0.08), rng.random()), (rng.uniform(0.92, 1), rng.random()),
                               (rng.random(), rng.uniform(0.9, 1)), (rng.random(), rng.uniform(0, 0.08))])
            x, y = pt(u, v)
            blob(td, x, y, rng.uniform(14, 24), rng.uniform(9, 15), (60, 34, 20, 255), rng)
    else:
        for _ in range(16):                                    # raw fries
            x, y = pt(rng.uniform(0.05, 0.95), rng.uniform(0.05, 0.95))
            a = rng.uniform(-0.4, 0.4)
            td.line([x - 24 * math.cos(a), y - 24 * math.sin(a), x + 24 * math.cos(a), y + 24 * math.sin(a)],
                    fill=(240, 214, 120, 255), width=11)
        for _ in range(14):                                    # raw Doener slices
            x, y = pt(rng.uniform(0.05, 0.95), rng.uniform(0.05, 0.95))
            blob(td, x, y, rng.uniform(18, 28), rng.uniform(11, 16), (170, 108, 88, 255), rng, jitter=0.4)
        for _ in range(22):                                    # shredded cheese
            x, y = pt(rng.uniform(0, 1), rng.uniform(0, 1))
            blob(td, x, y, rng.uniform(16, 28), rng.uniform(9, 14), (252, 244, 206, 255), rng, jitter=0.4)
    layer.alpha_composite(clip(top, mask))

    if baked:  # the fries sticking out at the front
        for i in range(5):
            x, y = pt(0.12 + i * 0.19, 0.99)
            d.line([x, y + 4, x - 4, y + 30], fill=(228, 176, 66, 255), width=13)
    # tray rim highlight along the back edge
    d.line([bl, br], fill=(222, 226, 234, 255), width=7)
    d.line([bl, fl], fill=(210, 214, 224, 255), width=6)
    return shade(layer, 0.22)


def draw_doener():
    """A cone of Doener meat: stacked slices with a skewer through the middle."""
    rng = random.Random(21)
    layer = new_layer()
    d = ImageDraw.Draw(layer)
    d.line([256, 36, 256, 480], fill=(170, 176, 188, 255), width=12)                    # skewer
    d.ellipse([240, 20, 272, 48], outline=(170, 176, 188, 255), width=8)
    top, bottom = 92, 440
    for i in range(0, 24):
        t = i / 23
        y = top + (bottom - top) * t
        half = 52 + 92 * math.sin(min(1.0, t * 1.15) * math.pi * 0.62) ** 0.8 * (1 - 0.35 * t) + 30 * t
        c = lerp((206, 120, 62), (120, 64, 36), rng.random() * 0.8 + 0.1)
        d.ellipse([256 - half, y - 20, 256 + half, y + 22], fill=lerp(c, (60, 30, 16), 0.35) + (255,))
        d.ellipse([256 - half + 4, y - 22, 256 + half - 4, y + 15], fill=c + (255,))
        for _ in range(3):  # crispy edge bits
            x = 256 + rng.uniform(-half, half) * 0.85
            d.ellipse([x - 9, y - 12, x + 9, y + 6], fill=lerp(c, (240, 190, 110), 0.4) + (255,))
    d.ellipse([228, 70, 284, 102], fill=(150, 82, 44, 255))
    return shade(layer, 0.25)


def draw_kaese():
    """Wedge of cheese with holes."""
    rng = random.Random(31)
    layer = new_layer()
    d = ImageDraw.Draw(layer)
    top = [(70, 250), (380, 150), (448, 214), (150, 330)]
    d.polygon(top, fill=(250, 214, 96, 255))
    d.polygon([(70, 250), (150, 330), (150, 420), (70, 340)], fill=(226, 178, 58, 255))      # left side
    d.polygon([(150, 330), (448, 214), (448, 304), (150, 420)], fill=(240, 198, 78, 255))    # front side
    for cx, cy, r in ((250, 238, 22), (330, 190, 14), (196, 290, 12), (370, 245, 18), (260, 345, 20), (330, 310, 12)):
        if cy < 300:
            d.ellipse([cx - r, cy - r * 0.6, cx + r, cy + r * 0.6], fill=(212, 160, 44, 255))
        else:
            d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(198, 146, 36, 255))
    d.line([(70, 250), (380, 150), (448, 214)], fill=(255, 236, 150, 255), width=6)
    return shade(layer, 0.2)


def finish(layer):
    """Dark outline and soft drop shadow, like vanilla Valheim icons."""
    alpha = layer.getchannel("A")
    outline = alpha.filter(ImageFilter.MaxFilter(13))
    shadow = alpha.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(10))
    out = new_layer()
    out.paste(Image.new("RGBA", (W, H), (0, 0, 0, 150)), (10, 12), shadow)
    out.paste(Image.new("RGBA", (W, H), (12, 10, 8, 255)), (0, 0), outline)
    out.alpha_composite(layer)
    return out.resize((SIZE, SIZE), Image.LANCZOS)


os.makedirs(OUT_DIR, exist_ok=True)
for name, layer in (("doener", draw_doener()), ("kaese", draw_kaese()),
                    ("teller", draw_tray(True)), ("tellerraw", draw_tray(False)),
                    ("pizza", draw_pizza(True)), ("pizzaraw", draw_pizza(False))):
    path = os.path.join(OUT_DIR, f"{name}.png")
    finish(layer).save(path)
    print("wrote", path)
