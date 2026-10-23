"""Turns the Quaternius cow into a mesh for the Lox skeleton.
Usage: build_cow.py COW_JSON LOXSKEL_JSON OUT_BIN OUT_PALETTE OUT_PREVIEW_JSON"""
import json, struct, sys
import numpy as np
from PIL import Image

cow = json.load(open(sys.argv[1]))
skel = json.load(open(sys.argv[2]))
lox = skel["Lox"]["Body"]
calf = skel["Lox_Calf"]["Body"]
print("calf bind poses same as Lox:", np.allclose(np.array(lox["bonePos"]), np.array(calf["bonePos"])), lox["bones"] == calf["bones"])

bones = lox["bones"]
bpos = {b: np.array(p) for b, p in zip(bones, lox["bonePos"])}

# cow bone -> Lox bone. Cow ".L" is +x in Blender, mirrored to -x like the Lox ".l" bones.
MAP = {
    "root": "Root", "Body": "Hip", "Hips": "Hip", "Back": "Hip",
    "Tail1": "Hip", "Tail2": "Hip", "Tail3": "Hip", "Tail4": "Hip",
    "Torso": "spine1", "Shoulders": "Spine2", "Neck": "Neck", "Head": "Head",
}
for side, s in (("L", "l"), ("R", "r")):
    MAP.update({
        f"FrontLeg.{side}": f"Shoulder.{s}", f"FrontUpLeg.{side}": f"Frontupperleg.{s}",
        f"FrontLowLeg.{side}": f"Frontlowerleg.{s}", f"FrontFoot.{side}": f"Frontfoot.{s}",
        f"BackLeg.{side}": f"Hip.{s}", f"BackUpLeg.{side}": f"Backupperleg.{s}",
        f"BackLowLeg.{side}": f"Backlowerleg.{s}", f"BackFoot.{side}": f"Backfoot.{s}",
    })

V = np.array(cow["verts"], dtype=float)                 # Blender: x right, -y forward, z up
raw = np.stack([-V[:, 0], V[:, 1], V[:, 2]], axis=1)    # Lox mesh space: x mirrored like Unity's FBX import

# scale: distance front legs to back legs, like the Lox
cow_front, cow_back = -1.856, 2.196
lox_front = (bpos["Frontupperleg.l"][1] + bpos["Frontupperleg.r"][1]) / 2
lox_back = (bpos["Backupperleg.l"][1] + bpos["Backupperleg.r"][1]) / 2
s = abs(lox_back - lox_front) / abs(cow_back - cow_front) * float(sys.argv[6] if len(sys.argv) > 6 else 1.0)
raw *= s
raw[:, 1] += (lox_front + lox_back) / 2 - s * (cow_front + cow_back) / 2
raw[:, 2] -= raw[:, 2].min()
print("scale", s, "bbox", raw.min(0).round(4), raw.max(0).round(4))

# palette
COLORS = {"White": (236, 233, 226), "Black": (32, 30, 34), "Pink": (226, 146, 156)}
cell = 16
palette = Image.new("RGBA", (cell * len(cow["mats"]), cell), (255, 255, 255, 255))
for i, m in enumerate(cow["mats"]):
    palette.paste(COLORS.get(m, (200, 200, 200)) + (255,), (i * cell, 0, (i + 1) * cell, cell))
palette.save(sys.argv[4])

def weights_of(i):
    w = {}
    for g, val in cow["weights"][i].items():
        b = MAP.get(g)
        if b is None:
            continue
        w[b] = w.get(b, 0) + val
    top = sorted(w.items(), key=lambda kv: -kv[1])[:4]
    total = sum(v for _, v in top) or 1.0
    top = [(bones.index(b), v / total) for b, v in top]
    if not top:
        top = [(bones.index("Hip"), 1.0)]
    while len(top) < 4:
        top.append((0, 0.0))
    return top

# flat shaded: every triangle gets its own vertices
out_v, out_uv, out_w, idx = [], [], [], []
for tri, mat in cow["tris"]:
    u = (mat + 0.5) * cell / palette.width
    for vi in tri:
        idx.append(len(out_v))
        out_v.append(raw[vi]); out_uv.append((u, 0.5)); out_w.append(weights_of(vi))

with open(sys.argv[3], "wb") as f:
    f.write(b"COW1")
    f.write(struct.pack("<i", len(bones)))
    for b in bones:
        data = b.encode("utf-8"); f.write(struct.pack("<i", len(data))); f.write(data)
    f.write(struct.pack("<i", len(out_v)))
    for p, uv, w in zip(out_v, out_uv, out_w):
        f.write(struct.pack("<5f", p[0], p[1], p[2], uv[0], uv[1]))
        f.write(struct.pack("<4B", *[i for i, _ in w]))
        f.write(struct.pack("<4f", *[v for _, v in w]))
    f.write(struct.pack("<i", len(idx)))
    f.write(struct.pack("<%di" % len(idx), *idx))
print("wrote", len(out_v), "vertices", len(idx) // 3, "triangles")

json.dump({"verts": [list(map(float, p)) for p in out_v], "mats": [m for _, m in cow["tris"]],
           "bones": {b: list(map(float, p)) for b, p in bpos.items()}, "colors": COLORS, "matnames": cow["mats"]},
          open(sys.argv[5], "w"))
