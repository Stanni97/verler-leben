"""Draws the item icons for Verler Leben.

Usage: python3 make_icons.py OUT_DIR [SIZE]
  Writes doener.png, kaese.png, teller.png, tellerraw.png, pizza.png, pizzaraw.png, milch.png,
  rindfleisch.png, boonekamp.png, manta.png, mantaraw.png and entenbox.png.
  SIZE defaults to 128 (item icons).

Everything is drawn from simple shapes. The baked dishes follow photos of the real thing:
the Spexarder Pizza is round with a golden crust, thick melted cheese and dark chunks of
Doener meat; the Kaunitzer Teller is an aluminium tray with overbaked cheese and burnt edges.
"""
import math
import os
import random
import sys
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

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


def draw_milch():
    """Glass milk bottle with a blue cap and a cow-patch label."""
    rng = random.Random(41)
    layer = new_layer()
    d = ImageDraw.Draw(layer)
    d.rounded_rectangle([170, 170, 342, 472], radius=44, fill=(246, 249, 252, 255), outline=(150, 168, 190, 255), width=8)
    d.rectangle([214, 92, 298, 196], fill=(238, 243, 249, 255), outline=(150, 168, 190, 255), width=6)
    d.rounded_rectangle([200, 52, 312, 106], radius=14, fill=(58, 110, 196, 255), outline=(30, 60, 120, 255), width=5)
    body = new_layer()
    bd = ImageDraw.Draw(body)
    for cx, cy, rx, ry in ((228, 250, 34, 26), (300, 330, 30, 38), (214, 410, 28, 24)):
        blob(bd, cx, cy, rx, ry, (36, 38, 46, 255), rng, jitter=0.35)
    mask = Image.new("L", (W, H), 0)
    ImageDraw.Draw(mask).rounded_rectangle([178, 178, 334, 464], radius=38, fill=255)
    layer.alpha_composite(clip(body, mask))
    d.rectangle([178, 330, 334, 372], fill=(70, 124, 206, 255))
    d.line([196, 190, 196, 300], fill=(255, 255, 255, 200), width=10)      # glass highlight
    return shade(layer, 0.18)


def draw_rindfleisch():
    """Raw beef: a red cut with fat marbling and a white fat rim."""
    rng = random.Random(51)
    layer = new_layer()
    d = ImageDraw.Draw(layer)
    blob(d, 256, 262, 196, 150, (232, 214, 196, 255), rng, jitter=0.18, rot=0.2)      # fat rim
    blob(d, 252, 258, 178, 132, (176, 42, 54, 255), rng, jitter=0.16, rot=0.2)        # meat
    blob(d, 232, 232, 120, 80, (200, 66, 74, 255), rng, jitter=0.2, rot=0.3)          # lighter middle
    for _ in range(14):                                                                # marbling
        x, y = rng.uniform(150, 360), rng.uniform(190, 330)
        a = rng.uniform(-0.8, 0.8)
        d.line([x, y, x + math.cos(a) * rng.uniform(30, 70), y + math.sin(a) * rng.uniform(14, 34)],
               fill=(246, 228, 218, 255), width=rng.randint(4, 8))
    blob(d, 330, 220, 26, 20, (250, 244, 236, 255), rng, jitter=0.2)                   # bone end
    return shade(layer, 0.25)


def draw_boonekamp():
    """Dark herbal bitter bottle with a cream label and a green band."""
    layer = new_layer()
    d = ImageDraw.Draw(layer)
    d.rounded_rectangle([156, 196, 356, 476], radius=34, fill=(52, 34, 22, 255), outline=(20, 12, 8, 255), width=6)
    d.polygon([(192, 196), (320, 196), (292, 128), (220, 128)], fill=(52, 34, 22, 255))
    d.rectangle([222, 66, 290, 140], fill=(60, 40, 26, 255), outline=(20, 12, 8, 255), width=5)
    d.rounded_rectangle([210, 44, 302, 82], radius=10, fill=(206, 166, 60, 255), outline=(120, 90, 20, 255), width=4)
    d.rectangle([166, 276, 346, 430], fill=(240, 228, 196, 255), outline=(90, 70, 40, 255), width=4)
    d.rectangle([166, 276, 346, 312], fill=(44, 98, 60, 255))
    d.rectangle([166, 394, 346, 430], fill=(44, 98, 60, 255))
    try:
        font = ImageFont.truetype("/usr/share/fonts/TTF/DejaVuSerif-Bold.ttf", 38)
    except OSError:
        font = ImageFont.load_default()
    for text, y in (("BOONE", 316), ("KAMP", 354)):
        box = d.textbbox((0, 0), text, font=font)
        d.text((256 - (box[0] + box[2]) / 2, y), text, font=font, fill=(44, 98, 60, 255))
    d.ellipse([234, 236, 278, 280], fill=(206, 166, 60, 255), outline=(120, 90, 20, 255), width=3)
    d.line([212, 214, 212, 330], fill=(255, 255, 255, 70), width=10)                   # glass highlight
    return shade(layer, 0.2)


def rot_rect(d, cx, cy, w, h, ang, fill, outline=None):
    """Filled rectangle turned by ang (radians) around its centre."""
    c, sn = math.cos(ang), math.sin(ang)
    pts = [(cx + x * c - y * sn, cy + x * sn + y * c) for x, y in ((-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2))]
    d.polygon(pts, fill=fill, outline=outline)
    return pts


def convex_hull(points):
    pts = sorted(set(points))
    if len(pts) < 3:
        return pts
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lower, upper = [], []
    for p in pts:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    for p in reversed(pts):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    return lower[:-1] + upper[:-1]


def with_shadow(base, part, offset=(6, 10), blur=7, alpha=110):
    """Composites part onto base with a soft drop shadow underneath."""
    mask = part.getchannel("A").point(lambda v: alpha if v > 0 else 0)
    shadow = Image.new("RGBA", (W, H), (30, 16, 6, 0))
    shadow.putalpha(mask)
    shadow = shadow.filter(ImageFilter.GaussianBlur(blur))
    moved = new_layer()
    moved.alpha_composite(shadow, offset)
    base.alpha_composite(moved)
    base.alpha_composite(part)


def gloss(layer, spots, blur=4):
    """Soft white highlights (x, y, rx, ry, alpha) on top of a layer, clipped to it."""
    g = Image.new("RGBA", (W, H), (255, 252, 244, 0))   # colour everywhere, so blurring only softens the alpha
    gd = ImageDraw.Draw(g)
    for x, y, rx, ry, a in spots:
        gd.ellipse([x - rx, y - ry, x + rx, y + ry], fill=(255, 252, 244, a))
    g = g.filter(ImageFilter.GaussianBlur(blur))
    g.putalpha(ImageChops.multiply(g.getchannel("A"), layer.getchannel("A")))   # clip without darkening the edges
    layer.alpha_composite(g)


def draw_fry(d, rng, cx, cy, length, angle, width, raw):
    """A thick, square-cut chip in a slight top view: darker side face, golden top with a light ridge,
    flat cut ends with a slightly browner rim, a few fried specks."""
    top = (250, 216, 98) if not raw else (242, 232, 190)
    ridge = (255, 236, 156) if not raw else (250, 244, 218)
    side = (222, 168, 62) if not raw else (226, 210, 164)
    end = (236, 186, 74) if not raw else (234, 222, 178)
    top = lerp(top, (246, 200, 70) if not raw else top, rng.uniform(0, 0.5))
    c, s = math.cos(angle), math.sin(angle)
    hx, hy = c * length / 2, s * length / 2
    px, py = -s * width / 2, c * width / 2
    corners = [(cx - hx - px, cy - hy - py), (cx + hx - px, cy + hy - py), (cx + hx + px, cy + hy + py), (cx - hx + px, cy - hy + py)]
    thick = width * 0.5
    lowered = [(x, y + thick) for x, y in corners]
    d.polygon(convex_hull(corners + lowered), fill=side + (255,))
    # the cut ends, a bit lighter than the side
    for i, j in ((0, 3), (1, 2)):
        d.polygon([corners[i], corners[j], lowered[j], lowered[i]], fill=end + (255,))
    d.polygon(corners, fill=top + (255,))
    q = 0.32
    d.polygon([(cx - hx * 0.96 - px, cy - hy * 0.96 - py), (cx + hx * 0.96 - px, cy + hy * 0.96 - py),
               (cx + hx * 0.96 - px * (1 - 2 * q), cy + hy * 0.96 - py * (1 - 2 * q)),
               (cx - hx * 0.96 - px * (1 - 2 * q), cy - hy * 0.96 - py * (1 - 2 * q))], fill=ridge + (255,))
    # slightly browner where it was cut
    for sign in (-1, 1):
        ex, ey = cx + sign * hx * 0.93, cy + sign * hy * 0.93
        d.line([ex - px, ey - py, ex + px, ey + py], fill=lerp(top, (200, 130, 40), 0.35) + (255,), width=4)
    for _ in range(int(length / 14)):
        t, u = rng.uniform(-0.42, 0.42), rng.uniform(-0.35, 0.35)
        x, y = cx + c * length * t - s * width * u, cy + s * length * t + c * width * u
        col = lerp(top, (200, 140, 40), 0.3) if not raw else lerp(top, (214, 196, 150), 0.3)
        d.ellipse([x - 1.6, y - 1.2, x + 1.8, y + 1.6], fill=col + (255,))


def draw_manta(baked):
    """Mantaplatte like at the snack bar (after a photo): a white paper chip tray with a pleated rim, a big pile of
    thick chips on the left with a large dollop of mayo, Currywurst chunks drowned in dark, glossy curry ketchup with
    heaps of curry powder on the right.
    Raw: pale chips and pink sausage, no sauce, no mayo."""
    rng = random.Random(91 if baked else 92)
    raw = not baked
    out = new_layer()

    # --- paper tray, seen from the front left; the right side shows the pleated rim
    tray = new_layer()
    td = ImageDraw.Draw(tray)
    outer = [(96, 150), (250, 128), (420, 132), (470, 150), (502, 196), (508, 300), (496, 368), (452, 392),
             (300, 420), (120, 452), (40, 440), (22, 360), (30, 250), (54, 190)]
    td.polygon(outer, fill=(250, 249, 244, 255))
    # pleats on the rim
    cx0, cy0 = 280, 290
    for i in range(90):
        a = i / 90 * math.tau
        x1, y1 = cx0 + math.cos(a) * 250, cy0 + math.sin(a) * 170
        td.line([cx0 + math.cos(a) * 190, cy0 + math.sin(a) * 120, x1, y1], fill=(222, 220, 214, 255), width=2)
    rim = new_layer()
    ImageDraw.Draw(rim).polygon(outer, fill=(255, 255, 255, 255))
    tray = clip(tray, rim.getchannel("A"))
    td = ImageDraw.Draw(tray)
    inner = [(110, 172), (255, 152), (410, 156), (452, 172), (476, 210), (480, 300), (468, 350), (432, 368),
             (300, 394), (130, 424), (64, 414), (48, 350), (54, 260), (74, 206)]
    td.polygon(inner, fill=(240, 230, 204, 255))
    td.line(inner + [inner[0]], fill=(214, 204, 178, 255), width=3)
    with_shadow(out, tray, offset=(4, 14), blur=12, alpha=120)

    # --- Currywurst in sauce on the right
    wurst = new_layer()
    wd = ImageDraw.Draw(wurst)
    meat_face = (234, 214, 184) if baked else (232, 168, 158)
    meat_skin = (196, 120, 76) if baked else (210, 128, 118)
    if baked:
        blob(wd, 372, 268, 128, 108, (122, 26, 12, 255), rng, jitter=0.12, rot=0.2)        # the pool of sauce
        blob(wd, 380, 262, 112, 92, (150, 36, 16, 255), rng, jitter=0.15, rot=0.2)
    chunks = [(300, 168, 0.3, 30, 20), (352, 214, 0.0, 34, 22), (414, 246, 0.4, 32, 22), (300, 268, -0.3, 34, 24),
              (430, 326, 0.2, 30, 24), (356, 320, 0.5, 34, 22), (418, 196, -0.2, 30, 20), (300, 340, 0.1, 30, 20)]
    for x, y, a, rx, ry in chunks:
        blob(wd, x, y + 6, rx, ry, lerp(meat_skin, (60, 20, 10), 0.35) + (255,), rng, jitter=0.12, rot=a)
        blob(wd, x, y, rx, ry, meat_skin + (255,), rng, jitter=0.12, rot=a)
        blob(wd, x + rx * 0.3, y + ry * 0.25, rx * 0.5, ry * 0.45, meat_face + (255,), rng, jitter=0.1, rot=a)
        if baked:
            # sauce running over the chunk, leaving only an edge of the cut face visible
            blob(wd, x - rx * 0.25, y - ry * 0.2, rx * 0.95, ry * 0.85, (170, 42, 18, 240), rng, jitter=0.3, rot=a)
    if baked:
        for _ in range(9):                                                                   # runny sauce
            blob(wd, rng.uniform(270, 470), rng.uniform(170, 360), rng.uniform(16, 30), rng.uniform(8, 14),
                 (rng.randint(150, 196), rng.randint(30, 50), 16, 230), rng, jitter=0.3)
        for cx, cy, r in ((364, 256, 50), (300, 214, 26), (418, 290, 28), (330, 316, 20)):  # curry powder heaps
            blob(wd, cx, cy, r, r * 0.6, (190, 120, 34, 245), rng, jitter=0.35)
            for _ in range(int(r * 6)):
                x, y = cx + rng.gauss(0, r * 0.45), cy + rng.gauss(0, r * 0.28)
                col = lerp((214, 150, 40), (240, 196, 80), rng.random()) if rng.random() < 0.7 else (150, 86, 24)
                wd.ellipse([x - 1.6, y - 1.6, x + 1.6, y + 1.6], fill=col + (255,))
        for _ in range(140):                                                                 # sprinkled powder
            x, y = rng.uniform(270, 470), rng.uniform(170, 360)
            wd.ellipse([x - 1.3, y - 1.3, x + 1.3, y + 1.3], fill=(216, 154, 46, 220))
        gloss(wurst, [(330, 190, 20, 7, 150), (440, 230, 16, 6, 140), (390, 336, 22, 7, 130), (300, 300, 14, 5, 140),
                      (448, 300, 10, 5, 150), (272, 240, 10, 4, 120)], blur=3)
    with_shadow(out, wurst, offset=(3, 6), blur=6, alpha=90)

    # --- chips: the back of the pile, then the mayo, then the chips in front
    back = new_layer()
    bd = ImageDraw.Draw(back)
    for cx, cy, ang, length in ((96, 186, -0.30, 170), (170, 170, -0.15, 150), (232, 186, 0.10, 130), (54, 236, -0.55, 150),
                                (130, 222, 0.05, 170), (196, 230, -0.35, 150), (60, 292, -0.05, 150), (250, 214, 0.85, 168),
                                (120, 300, 0.10, 160), (210, 296, -0.15, 140)):
        draw_fry(bd, rng, cx, cy, length, ang, 34, raw)
    with_shadow(out, back, offset=(5, 9), blur=6, alpha=90)

    if baked:
        mayo = new_layer()
        md = ImageDraw.Draw(mayo)
        cream = (246, 238, 206)
        blob(md, 130, 256, 96, 68, (222, 210, 172, 255), rng, jitter=0.1, rot=0.1)
        blob(md, 126, 246, 94, 66, cream + (255,), rng, jitter=0.1, rot=0.1)
        blob(md, 150, 206, 60, 44, cream + (255,), rng, jitter=0.12, rot=-0.2)
        blob(md, 162, 182, 30, 22, (250, 245, 222, 255), rng, jitter=0.12)
        blob(md, 92, 300, 16, 30, cream + (255,), rng, jitter=0.1)                          # a drip
        blob(md, 96, 330, 12, 10, cream + (255,), rng, jitter=0.1)
        md.arc([70, 222, 196, 300], 30, 140, fill=(234, 224, 192, 255), width=3)           # soft folds
        md.arc([110, 190, 196, 250], 210, 310, fill=(238, 228, 198, 255), width=2)
        gloss(mayo, [(116, 214, 30, 14, 230), (156, 192, 16, 8, 240), (88, 262, 10, 6, 200)], blur=5)
        with_shadow(out, mayo, offset=(4, 8), blur=7, alpha=100)

    front = new_layer()
    fd = ImageDraw.Draw(front)
    for cx, cy, ang, length in ((40, 372, -1.30, 170), (160, 372, -0.66, 236), (206, 360, -0.70, 170),
                                (276, 392, 0.46, 150), (110, 410, -0.20, 150), (238, 270, 0.88, 150)):
        draw_fry(fd, rng, cx, cy, length, ang, 36, raw)
    with_shadow(out, front, offset=(5, 10), blur=6, alpha=100)
    return shade(out, 0.15)


def draw_entenbox():
    """The yellow duck-face takeaway box with fried dough balls, cinnamon sugar and strawberry sauce."""
    rng = random.Random(61)
    layer = new_layer()
    d = ImageDraw.Draw(layer)
    d.polygon([(126, 168), (386, 168), (366, 262), (146, 262)], fill=(238, 228, 202, 255), outline=(120, 100, 60, 255))
    d.rounded_rectangle([200, 140, 312, 182], radius=18, fill=(238, 228, 202, 255), outline=(120, 100, 60, 255), width=5)
    d.ellipse([236, 150, 276, 172], fill=(60, 60, 60, 120))                       # handle slot
    for _ in range(12):                                                           # dough balls
        x, y, r = rng.uniform(172, 340), rng.uniform(190, 252), rng.uniform(22, 32)
        d.ellipse([x - r, y - r, x + r, y + r], fill=(196, 130, 48, 255))
        d.ellipse([x - r + 3, y - r + 3, x + r - 5, y + r - 6], fill=(228, 166, 64, 255))
        for _ in range(7):
            sx, sy = x + rng.uniform(-r * 0.7, r * 0.7), y + rng.uniform(-r * 0.7, r * 0.5)
            d.ellipse([sx - 2, sy - 2, sx + 2, sy + 2], fill=(246, 226, 190, 255))   # cinnamon sugar
        if rng.random() < 0.6:
            blob(d, x + rng.uniform(-8, 8), y + rng.uniform(-6, 6), r * 0.55, r * 0.3, (214, 52, 60, 255), rng, jitter=0.3)
    for x0, x1 in ((206, 224), (284, 296)):                                       # wooden skewers
        d.line([x0, 96, x1, 236], fill=(204, 168, 108, 255), width=8)
    d.polygon([(148, 256), (364, 256), (346, 474), (166, 474)], fill=(252, 216, 40, 255), outline=(120, 90, 10, 255))
    for ex in (212, 300):                                                         # eyes
        d.ellipse([ex - 34, 312, ex + 34, 386], fill=(255, 255, 255, 255), outline=(40, 30, 10, 255), width=5)
        d.ellipse([ex - 16, 330, ex + 14, 372], fill=(20, 20, 24, 255))
        d.ellipse([ex - 8, 336, ex + 2, 348], fill=(255, 255, 255, 255))
    d.arc([176, 296, 250, 330], 200, 340, fill=(40, 30, 10, 255), width=7)       # brows
    d.arc([262, 296, 336, 330], 200, 340, fill=(40, 30, 10, 255), width=7)
    d.ellipse([222, 396, 290, 446], fill=(255, 150, 30, 255), outline=(150, 70, 10, 255), width=5)   # beak
    d.ellipse([240, 418, 272, 438], fill=(206, 60, 70, 255))
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
                    ("pizza", draw_pizza(True)), ("pizzaraw", draw_pizza(False)),
                    ("milch", draw_milch()), ("rindfleisch", draw_rindfleisch()), ("boonekamp", draw_boonekamp()),
                    ("manta", draw_manta(True)), ("mantaraw", draw_manta(False)), ("entenbox", draw_entenbox())):
    path = os.path.join(OUT_DIR, f"{name}.png")
    finish(layer).save(path)
    print("wrote", path)
