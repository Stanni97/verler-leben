"""Reads the Lox and calf body from Valheim's asset bundle: bone names and bind positions (mesh space).
Usage: python3 export_lox_skeleton.py VALHEIM_DIR OUT_JSON   (needs UnityPy and numpy)"""
import json, os, sys
import numpy as np
import UnityPy

bundle = os.path.join(sys.argv[1], "valheim_Data/StreamingAssets/SoftRef/Bundles/c4210710")
env = UnityPy.load(bundle)
objs = {o.path_id: o for o in env.objects}


def components(go):
    for c in go.m_Components:
        ref = c.component if hasattr(c, "component") else c
        if ref.m_PathID in objs:
            yield objs[ref.m_PathID]


def body_renderer(transform):
    t = transform.read()
    go = t.m_GameObject.read()
    for c in components(go):
        if c.type.name == "SkinnedMeshRenderer" and go.m_Name == "Body":
            return c
    for child in t.m_Children:
        found = body_renderer(objs[child.m_PathID])
        if found:
            return found
    return None


result = {}
for o in env.objects:
    if o.type.name != "GameObject":
        continue
    go = o.read()
    if go.m_Name not in ("Lox", "Lox_Calf") or go.m_Name in result:
        continue
    transform = next((c for c in components(go) if c.type.name == "Transform"), None)
    renderer = body_renderer(transform) if transform else None
    if renderer is None:
        continue
    r = renderer.read()
    mesh = r.m_Mesh.read()
    bones = [objs[b.m_PathID].read().m_GameObject.read().m_Name for b in r.m_Bones]
    positions = []
    for m in mesh.m_BindPose:
        matrix = np.array([[m.e00, m.e01, m.e02, m.e03], [m.e10, m.e11, m.e12, m.e13],
                           [m.e20, m.e21, m.e22, m.e23], [m.e30, m.e31, m.e32, m.e33]])
        positions.append((np.linalg.inv(matrix) @ np.array([0, 0, 0, 1.0]))[:3].tolist())
    result[go.m_Name] = {"Body": {"bones": bones, "bonePos": positions}}
    print(go.m_Name, len(bones), "bones")

json.dump(result, open(sys.argv[2], "w"), indent=1)
