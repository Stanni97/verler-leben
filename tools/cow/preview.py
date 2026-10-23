import bpy, json, sys, math
from mathutils import Vector
args = sys.argv[sys.argv.index("--")+1:]
data = json.load(open(args[0])); out = args[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
# mesh
verts = [(-v[0], v[1], v[2]) for v in data["verts"]]
faces = [(i, i+1, i+2) for i in range(0, len(verts), 3)]
me = bpy.data.meshes.new("cow"); me.from_pydata(verts, [], faces); me.update()
ob = bpy.data.objects.new("cow", me); scene.collection.objects.link(ob)
for name in data["matnames"]:
    m = bpy.data.materials.new(name); c = data["colors"][name]
    m.diffuse_color = (c[0]/255, c[1]/255, c[2]/255, 1); me.materials.append(m)
for p, mi in zip(me.polygons, data["mats"]): p.material_index = mi
# bones as small red spheres
for b, p in data["bones"].items():
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.0012, location=(-p[0], p[1], p[2]))
    s = bpy.context.active_object; mat = bpy.data.materials.new("bone"); mat.diffuse_color = (1, 0.1, 0.1, 1); s.data.materials.append(mat)
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"; scene.display.shading.color_type = "MATERIAL"
scene.display.shading.show_xray = bool(int(args[2])) if len(args) > 2 else False
scene.render.resolution_x, scene.render.resolution_y = 640, 480
scene.world = bpy.data.worlds.new("w"); scene.world.color = (0.25, 0.3, 0.25)
cam_data = bpy.data.cameras.new("cam"); cam_data.type = "ORTHO"; cam_data.ortho_scale = 0.11
cam = bpy.data.objects.new("cam", cam_data); scene.collection.objects.link(cam); scene.camera = cam
center = Vector((0, -0.01, 0.024))
views = {"side": (Vector((0.3, 0, 0.024)),), "front": (Vector((0, -0.3, 0.03)),), "threequarter": (Vector((0.2, -0.2, 0.09)),)}
for name, (pos,) in views.items():
    cam.location = pos
    d = (center - pos).normalized()
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = out + "_" + name + ".png"
    bpy.ops.render.render(write_still=True)
