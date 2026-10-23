import bpy, bmesh, json, sys
out = sys.argv[sys.argv.index("--")+1]
arm = bpy.data.objects["Armature"]
arm.data.pose_position = "REST"
cow = bpy.data.objects["Cow"]
deps = bpy.context.evaluated_depsgraph_get()
ev = cow.evaluated_get(deps)
me = ev.to_mesh()
bm = bmesh.new(); bm.from_mesh(me); bmesh.ops.triangulate(bm, faces=bm.faces[:]); bm.to_mesh(me); bm.free()
groups = [g.name for g in cow.vertex_groups]
# weights from the original (non-evaluated) mesh, same vertex order
src = cow.data
weights = []
for v in src.vertices:
    weights.append({groups[g.group]: round(g.weight, 5) for g in v.groups if g.weight > 0.001})
verts = [list(cow.matrix_world @ v.co) for v in me.vertices]
tris = [[list(p.vertices), p.material_index] for p in me.polygons]
mats = [m.name for m in src.materials]
json.dump({"verts": verts, "tris": tris, "mats": mats, "weights": weights}, open(out, "w"))
print("EXPORTED", len(verts), "verts", len(tris), "tris", mats)
