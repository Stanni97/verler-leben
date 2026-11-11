"""Places Momo's accessories on the cow and adds them to the cow data of build_momo.py."""
import math
import numpy as np
import momo_parts as mp

# the cow's left is +x in Blender, the front is -y
HORN_BASE = np.array([0.34, -4.22, 4.93])
NECK_CENTRE = np.array([0.0, -3.32, 3.98])


def horn_path(sign, steps=9):
    base = HORN_BASE * np.array([sign, 1, 1])
    pts = []
    for i in range(steps):
        t = i / (steps - 1)
        a = t * 1.25
        pts.append(base + np.array([sign * 0.95 * math.sin(a), -0.22 * t, 0.78 * (1 - math.cos(a)) + 0.07 * t]))
    return pts


def make_parts():
    parts = []
    # horns, cream with a darker tip would need another swatch; plain cream is enough at this size
    for sign in (1, -1):
        path = horn_path(sign)
        radii = [0.135 * (1 - i / (len(path) - 1)) + 0.03 for i in range(len(path))]
        parts.append(mp.tube({"Head": 1.0}, "horn", path, radii, sides=10))

    # flower on the left horn, a little out from its base
    anchor = horn_path(1)[2] + np.array([0.0, -0.02, 0.17])
    facing = np.array([0.45, -0.75, 0.5])
    a, b = mp._frame(facing)
    for k in range(8):
        t = 2 * math.pi * k / 8
        d = math.cos(t) * a + math.sin(t) * b
        parts.append(mp.petal({"Head": 1.0}, "petal", anchor + mp._unit(facing) * 0.02, d, facing, 0.42, 0.27))
    parts.append(mp.disc({"Head": 1.0}, "center", anchor, facing, 0.15, offset=0.06))

    # collar around the neck, with bell and name tag at the throat
    def outline(theta):
        return 0.54, 0.99
    parts.append(mp.torus_ellipse({"Neck": 1.0}, "leather", NECK_CENTRE, 0.54, 0.99, 0.075, outline=outline))
    throat = NECK_CENTRE + np.array([0.0, 0.0, -0.99])
    parts.append(mp.sphere({"Neck": 1.0}, "gold", throat + np.array([0.0, -0.10, -0.12]), 0.17, squash=(1, 1, 1.1)))
    parts.append(mp.box({"Neck": 1.0}, "gold", "gold", throat + np.array([0.0, -0.14, -0.62]), (0.62, 0.05, 0.40),
                        front_dir=(0, -1, 0), tag=True))

    parts.append(mp.tube({"Neck": 1.0}, "gold", [throat + np.array([0.0, -0.10, -0.22]), throat + np.array([0.0, -0.13, -0.42])],
                         [0.025, 0.025], sides=6, cap_end=False))

    # bows on the lower legs, where the socks end
    leg_spots = {
        "FrontLowLeg.L": (0.97, -1.96), "FrontLowLeg.R": (-0.97, -1.96),
        "BackLowLeg.L": (0.92, 2.73), "BackLowLeg.R": (-0.92, 2.73),
    }
    for group, (x, y) in leg_spots.items():
        out = np.array([math.copysign(1.0, x), 0.0, 0.0])
        parts.append(mp.bow({group: 1.0}, "bow", np.array([x, y, 1.42]), out, np.array([0.0, 1.0, 0.0]), 0.27))
        parts.append(mp.sphere({group: 1.0}, "bow", np.array([x + out[0] * 0.015, y, 1.42]), 0.06))
    return parts


def flatten_udder(cow):
    """A bull has no udder: the pink bulge under the belly is pushed into the belly."""
    import os
    from PIL import Image
    path = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "cow_schwarzbunt.png")
    im = np.array(Image.open(path).convert("RGB")).astype(float)
    size = im.shape[0]
    pink = set()
    for vi, n, uv in cow["corners"]:
        x, y = min(size - 1, int(uv[0] * size)), min(size - 1, int((1 - uv[1]) * size))
        r, g, b = im[y, x]
        if r > g * 1.22 and r > 80:
            pink.add(vi)
    moved = 0
    for vi in pink:
        v = cow["verts"][vi]
        if v[1] > 0.6 and v[2] < 2.2 and abs(v[0]) < 0.9 and abs(v[0]) < 0.65:
            v[2] = 2.05 + (v[2] - 1.64) * 0.12
            moved += 1
    print("udder vertices flattened:", moved)


def add_to(cow):
    flatten_udder(cow)
    # the cow coat lives in the left half of the atlas
    for c in cow["corners"]:
        c[2] = [c[2][0] * 0.5, c[2][1]]
    for part in make_parts():
        # triangles flipped to match the vertex normals (outward)
        pts = np.array(part.v)
        base_v = len(cow["verts"])
        base_c = len(cow["corners"])
        for p, n, uv in zip(part.v, part.n, part.uv):
            cow["verts"].append([float(x) for x in p])
            cow["weights"].append(dict(part.weights))
            cow["corners"].append([len(cow["verts"]) - 1, [float(x) for x in n], [float(uv[0]), float(uv[1])]])
        for a, b, c in part.tris:
            n = np.cross(pts[b] - pts[a], pts[c] - pts[a])
            if np.dot(n, part.n[a] + part.n[b] + part.n[c]) < 0:
                b, c = c, b
            cow["tris"].append([base_c + a, base_c + b, base_c + c])
