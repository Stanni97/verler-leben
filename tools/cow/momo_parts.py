"""Accessories of Momo the bull, in the coordinates of the Quaternius cow (Blender: x right, -y forward, z up).

Every part has vertices, normals, UVs, triangles and bone weights (cow bone names). The UVs point at a swatch of the
right half of the atlas (cow_momo.png, 1024 x 512): the left half holds the coat, the right half flat colours and the
name tag. build_momo.py adds the parts to the cow, paint_momo.py draws the atlas.
"""
import math
import numpy as np

ATLAS_W, ATLAS_H = 1024, 512
CELL = 64

# swatch name -> (cell x, cell y, colour). Cells are CELL pixels wide, counted from x = 512.
SWATCHES = {
    "leather": (0, 0, (112, 62, 30)),
    "gold": (1, 0, (232, 176, 44)),
    "horn": (2, 0, (236, 224, 192)),
    "petal": (3, 0, (255, 128, 176)),
    "center": (4, 0, (255, 214, 64)),
    "bow": (5, 0, (245, 112, 168)),
    "white": (6, 0, (236, 236, 230)),
}
TAG_RECT = (512, 128, 256, 128)   # x, y (from the top), width, height of the name plate


def swatch_uv(name):
    cx, cy, _ = SWATCHES[name]
    x = 512 + cx * CELL + CELL / 2
    y = cy * CELL + CELL / 2
    return (x / ATLAS_W, 1.0 - y / ATLAS_H)


def tag_uv(u, v):
    """u, v in 0..1 over the plate (v up)."""
    x, y, w, h = TAG_RECT
    return ((x + u * w) / ATLAS_W, 1.0 - (y + (1.0 - v) * h) / ATLAS_H)


class Part:
    def __init__(self, weights):
        self.v, self.n, self.uv, self.tris = [], [], [], []
        self.weights = weights

    def add(self, p, n, uv):
        self.v.append(np.asarray(p, float))
        self.n.append(np.asarray(n, float))
        self.uv.append(uv)
        return len(self.v) - 1

    def tri(self, a, b, c):
        self.tris.append((a, b, c))


def _unit(v):
    v = np.asarray(v, float)
    return v / (np.linalg.norm(v) or 1.0)


def _frame(direction):
    d = _unit(direction)
    helper = np.array([0.0, 0.0, 1.0]) if abs(d[2]) < 0.9 else np.array([1.0, 0.0, 0.0])
    a = _unit(np.cross(d, helper))
    b = np.cross(d, a)
    return a, b


def tube(weights, swatch, path, radii, sides=10, cap_end=True):
    """A tapered tube along a path; outward faces."""
    part = Part(weights)
    uv = swatch_uv(swatch)
    rings = []
    for i, p in enumerate(path):
        p = np.asarray(p, float)
        if i < len(path) - 1:
            d = np.asarray(path[i + 1], float) - p
        else:
            d = p - np.asarray(path[i - 1], float)
        a, b = _frame(d)
        ring = []
        for k in range(sides):
            t = 2 * math.pi * k / sides
            n = math.cos(t) * a + math.sin(t) * b
            ring.append(part.add(p + n * radii[i], n, uv))
        rings.append(ring)
    for i in range(len(rings) - 1):
        for k in range(sides):
            k2 = (k + 1) % sides
            a0, a1, b0, b1 = rings[i][k], rings[i][k2], rings[i + 1][k], rings[i + 1][k2]
            part.tri(a0, b0, a1)
            part.tri(a1, b0, b1)
    if cap_end:
        end = np.asarray(path[-1], float)
        d = _unit(end - np.asarray(path[-2], float))
        tip = part.add(end + d * radii[-1], d, uv)
        for k in range(sides):
            part.tri(rings[-1][k], tip, rings[-1][(k + 1) % sides])
    _orient(part, np.mean(path, axis=0))
    return part


def _orient(part, inside):
    """Flips triangles whose face normal points to the inside; only for roughly convex shapes."""
    pts = np.array(part.v)
    flipped = []
    for a, b, c in part.tris:
        n = np.cross(pts[b] - pts[a], pts[c] - pts[a])
        centre = (pts[a] + pts[b] + pts[c]) / 3
        out = np.dot(n, part.n[a] + part.n[b] + part.n[c])   # compare with the vertex normals, which are outward
        flipped.append((a, c, b) if out < 0 else (a, b, c))
    part.tris = flipped


def sphere(weights, swatch, centre, radius, rings=8, sides=12, squash=(1, 1, 1)):
    part = Part(weights)
    uv = swatch_uv(swatch)
    centre = np.asarray(centre, float)
    index = []
    for i in range(rings + 1):
        phi = math.pi * i / rings
        row = []
        for k in range(sides):
            t = 2 * math.pi * k / sides
            n = np.array([math.sin(phi) * math.cos(t), math.sin(phi) * math.sin(t), math.cos(phi)])
            row.append(part.add(centre + n * radius * np.array(squash), _unit(n / np.array(squash)), uv))
        index.append(row)
    for i in range(rings):
        for k in range(sides):
            k2 = (k + 1) % sides
            part.tri(index[i][k], index[i + 1][k], index[i][k2])
            part.tri(index[i][k2], index[i + 1][k], index[i + 1][k2])
    _orient(part, centre)
    return part


def torus_ellipse(weights, swatch, centre, a, b, tube_radius, plane="xz", steps=36, sides=8, outline=None):
    """A ring around the neck. The ring lies in the x-z plane (the neck points along y); outline(theta) -> (a, b)
    scales it so it follows the neck."""
    part = Part(weights)
    uv = swatch_uv(swatch)
    centre = np.asarray(centre, float)
    rings = []
    for i in range(steps):
        th = 2 * math.pi * i / steps
        ra, rb = outline(th) if outline else (a, b)
        p = centre + np.array([ra * math.cos(th), 0.0, rb * math.sin(th)])
        tangent = _unit(np.array([-ra * math.sin(th), 0.0, rb * math.cos(th)]))
        radial = _unit(np.array([rb * math.cos(th), 0.0, ra * math.sin(th)]))   # outward in the plane
        side = np.array([0.0, 1.0, 0.0])
        ring = []
        for k in range(sides):
            t = 2 * math.pi * k / sides
            n = math.cos(t) * radial + math.sin(t) * side
            ring.append(part.add(p + n * tube_radius, n, uv))
        rings.append(ring)
    for i in range(steps):
        i2 = (i + 1) % steps
        for k in range(sides):
            k2 = (k + 1) % sides
            part.tri(rings[i][k], rings[i2][k], rings[i][k2])
            part.tri(rings[i][k2], rings[i2][k], rings[i2][k2])
    _orient(part, centre)
    return part


def box(weights, front_swatch, side_swatch, centre, size, front_dir=(0, -1, 0), tag=False):
    """A flat plate. front_dir is the face that shows the name; with tag=True it carries the name tag UVs."""
    part = Part(weights)
    centre = np.asarray(centre, float)
    f = _unit(front_dir)
    up = np.array([0.0, 0.0, 1.0])
    right = _unit(np.cross(up, f))      # seen from the front, to the left
    hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2   # width, thickness, height
    faces = [
        (f, [(-1, 1), (1, 1), (1, -1), (-1, -1)], True),
        (-f, [(1, 1), (-1, 1), (-1, -1), (1, -1)], False),
    ]
    def corner(sx, sz, sy):
        return centre + right * sx * hx + up * sz * hz + f * sy * hy
    # front and back
    for normal, quad, is_front in faces:
        sy = 1 if is_front else -1
        pts = [corner(sx, sz, sy) for sx, sz in quad]
        uvs = []
        for sx, sz in quad:
            if is_front and tag:
                # seen from the front, +right is the viewer's right
                uvs.append(tag_uv(0.5 + sx * 0.5, 0.5 + sz * 0.5))
            else:
                uvs.append(swatch_uv(front_swatch))
        ids = [part.add(p, normal, u) for p, u in zip(pts, uvs)]
        # winding: quad listed top-left, top-right, bottom-right, bottom-left as seen from outside
        part.tri(ids[0], ids[2], ids[1])
        part.tri(ids[0], ids[3], ids[2])
    # rim: four thin quads
    uv = swatch_uv(side_swatch)
    for (sx0, sz0), (sx1, sz1) in [((-1, 1), (1, 1)), ((1, 1), (1, -1)), ((1, -1), (-1, -1)), ((-1, -1), (-1, 1))]:
        p0f, p1f = corner(sx0, sz0, 1), corner(sx1, sz1, 1)
        p0b, p1b = corner(sx0, sz0, -1), corner(sx1, sz1, -1)
        n = _unit(np.cross(p1f - p0f, p0b - p0f))
        mid = (p0f + p1f + p0b + p1b) / 4
        if np.dot(n, mid - centre) < 0:
            n = -n
        ids = [part.add(p, n, uv) for p in (p0f, p1f, p1b, p0b)]
        part.tri(ids[0], ids[1], ids[2])
        part.tri(ids[0], ids[2], ids[3])
        part.tri(ids[0], ids[2], ids[1])   # both sides, so winding does not matter for such a thin rim
        part.tri(ids[0], ids[3], ids[2])
    return part


def disc(weights, swatch, centre, normal, radius, sides=16, offset=0.0):
    """A flat round disc, both sides."""
    part = Part(weights)
    uv = swatch_uv(swatch)
    n = _unit(normal)
    a, b = _frame(n)
    centre = np.asarray(centre, float) + n * offset
    hub = part.add(centre, n, uv)
    hub_back = part.add(centre, -n, uv)
    ring, ring_back = [], []
    for k in range(sides):
        t = 2 * math.pi * k / sides
        p = centre + (math.cos(t) * a + math.sin(t) * b) * radius
        ring.append(part.add(p, n, uv))
        ring_back.append(part.add(p, -n, uv))
    for k in range(sides):
        k2 = (k + 1) % sides
        part.tri(hub, ring[k], ring[k2])
        part.tri(hub_back, ring_back[k2], ring_back[k])
    return part


def petal(weights, swatch, base, direction, normal, length, width):
    """A flat petal: a kite from the base outwards, both sides."""
    part = Part(weights)
    uv = swatch_uv(swatch)
    d, n = _unit(direction), _unit(normal)
    side = _unit(np.cross(d, n))
    base = np.asarray(base, float)
    tip = base + d * length
    mid = base + d * length * 0.55
    pts = [base, mid + side * width / 2, tip, mid - side * width / 2]
    front = [part.add(p, n, uv) for p in pts]
    back = [part.add(p, -n, uv) for p in pts]
    part.tri(front[0], front[1], front[2])
    part.tri(front[0], front[2], front[3])
    part.tri(back[0], back[2], back[1])
    part.tri(back[0], back[3], back[2])
    return part


def bow(weights, swatch, centre, outward, axis, size):
    """Two triangles (the loops) and a small knot, lying on the leg; outward = away from the leg."""
    part = Part(weights)
    out = _unit(outward)
    ax = _unit(axis)       # along the loops
    c = np.asarray(centre, float)
    up = _unit(np.cross(out, ax))
    uv = swatch_uv(swatch)
    parts = []
    for sign in (-1, 1):
        tip = c + ax * sign * size
        a, b = c + up * size * 0.55 + out * 0.01, c - up * size * 0.55 + out * 0.01
        ids_f = [part.add(p, out, uv) for p in (c, a, tip, b)]
        ids_b = [part.add(p, -out, uv) for p in (c, a, tip, b)]
        part.tri(ids_f[0], ids_f[2], ids_f[1])
        part.tri(ids_f[0], ids_f[3], ids_f[2])
        part.tri(ids_b[0], ids_b[1], ids_b[2])
        part.tri(ids_b[0], ids_b[2], ids_b[3])
    return part
