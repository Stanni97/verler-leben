"""Renders the built cow with one coat texture: side, front and three-quarter view.
Usage: blender -b --factory-startup --python preview.py -- PREVIEW_JSON TEXTURE OUT_PREFIX"""
import bpy, json, sys
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:]
data = json.load(open(args[0]))
texture, out = args[1], args[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
verts = [(-v[0], v[1], v[2]) for v in data["verts"]]          # back from Lox mesh space to Blender
faces = [tuple(t) for t in data["tris"]]
me = bpy.data.meshes.new("cow")
me.from_pydata(verts, [], faces)
me.update()
uv = me.uv_layers.new(name="UV")
for poly in me.polygons:
    poly.use_smooth = True
    for li in poly.loop_indices:
        uv.data[li].uv = data["uvs"][me.loops[li].vertex_index]
ob = bpy.data.objects.new("cow", me)
scene.collection.objects.link(ob)
mat = bpy.data.materials.new("coat")
mat.use_nodes = True
tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
tex.image = bpy.data.images.load(texture)
bsdf = mat.node_tree.nodes["Principled BSDF"]
mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
me.materials.append(mat)

scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "TEXTURE"
scene.render.resolution_x, scene.render.resolution_y = 560, 420
scene.world = bpy.data.worlds.new("w")
scene.world.color = (0.32, 0.42, 0.3)
cam_data = bpy.data.cameras.new("cam")
cam_data.type = "ORTHO"
cam_data.ortho_scale = 0.11
cam = bpy.data.objects.new("cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
center = Vector((0, -0.01, 0.024))
for name, pos in {"side": Vector((0.3, 0, 0.024)), "front": Vector((0, -0.3, 0.03)),
                  "threequarter": Vector((0.2, -0.2, 0.09))}.items():
    cam.location = pos
    cam.rotation_euler = (center - pos).normalized().to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = out + "_" + name + ".png"
    bpy.ops.render.render(write_still=True)
