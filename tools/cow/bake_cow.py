"""Smooths the Quaternius cow, unwraps it and bakes realistic coat textures (Cycles, CPU).

Usage: blender -b --disable-autoexec Cow.blend --python bake_cow.py -- OUT_JSON OUT_DIR [SIZE]

Writes OUT_JSON (smoothed mesh: positions, corner normals, UVs, bone weights) and one texture per coat in OUT_DIR:
cow_schwarzbunt.png, cow_rotbunt.png, cow_braun.png. The body gets irregular patches from 3D noise, the head keeps
the pattern of the model, hooves and muzzle keep their colours. Fur grain and baked ambient occlusion add depth.
"""
import json
import math
import os
import sys

import bpy
import bmesh

args = sys.argv[sys.argv.index("--") + 1:]
out_json, out_dir = args[0], args[1]
size = int(args[2]) if len(args) > 2 else 512

COATS = {
    # name: (base coat, patches)
    # A neutral, slightly cool white: the sun is warm, and AO and the belly shading already darken it.
    "schwarzbunt": ((0.58, 0.58, 0.56), (0.035, 0.032, 0.032)),
    "rotbunt": ((0.58, 0.58, 0.56), (0.30, 0.11, 0.05)),
    "braun": ((0.40, 0.27, 0.17), (0.30, 0.19, 0.11)),
}
PINK = (0.48, 0.28, 0.30)
HOOF = (0.09, 0.08, 0.075)

arm = bpy.data.objects["Armature"]
arm.data.pose_position = "REST"
cow = bpy.data.objects["Cow"]
for ob in bpy.context.view_layer.objects:
    ob.select_set(False)
bpy.context.view_layer.objects.active = cow
cow.select_set(True)

# 1. smooth: one subdivision level, applied before the armature so the vertex groups are interpolated
sub = cow.modifiers.new("Smooth", "SUBSURF")
sub.levels = 1
sub.render_levels = 1
bpy.ops.object.modifier_move_to_index(modifier="Smooth", index=0)
bpy.ops.object.modifier_apply(modifier="Smooth")
me = cow.data
for poly in me.polygons:
    poly.use_smooth = True

# 2. what each face shows: the original colour (white / black / pink), hooves, and whether the body pattern is used
mats = [m.name for m in me.materials]
orig = me.color_attributes.new("orig", "FLOAT_COLOR", "CORNER")
body = me.attributes.new("body", "FLOAT", "FACE")
for poly in me.polygons:
    c = poly.center
    name = mats[poly.material_index]
    is_head = c.y < -3.0
    is_tail_tip = c.y > 3.3 and c.z < 3.2
    if name == "Pink":
        colour, use_body = PINK, 0.0
    elif c.z < 0.55:
        colour, use_body = HOOF, 0.0
    elif is_head or is_tail_tip:
        # markers: white -> coat colour, black -> patch colour
        colour, use_body = ((1.0, 1.0, 1.0) if name == "White" else (0.0, 0.0, 0.0)), 0.0
    else:
        colour, use_body = (1.0, 1.0, 1.0), 1.0
    body.data[poly.index].value = use_body
    for li in poly.loop_indices:
        orig.data[li].color = (*colour, 1.0)

# 3. UVs
bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.02)
bpy.ops.object.mode_set(mode="OBJECT")

# 4. one material for everything, built from nodes
mat = bpy.data.materials.new("Coat")
mat.use_nodes = True
nt = mat.node_tree
nodes, links = nt.nodes, nt.links
nodes.clear()
out = nodes.new("ShaderNodeOutputMaterial")
bsdf = nodes.new("ShaderNodeBsdfPrincipled")
links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])

coord = nodes.new("ShaderNodeTexCoord")
coat_rgb = nodes.new("ShaderNodeRGB")
patch_rgb = nodes.new("ShaderNodeRGB")

patches = nodes.new("ShaderNodeTexNoise")
patches.inputs["Scale"].default_value = 0.30
patches.inputs["Detail"].default_value = 2.0
patches.inputs["Roughness"].default_value = 0.55
patches.inputs["Distortion"].default_value = 0.7
links.new(coord.outputs["Object"], patches.inputs["Vector"])
ramp = nodes.new("ShaderNodeValToRGB")
ramp.color_ramp.elements[0].position = 0.505
ramp.color_ramp.elements[1].position = 0.53
links.new(patches.outputs["Fac"], ramp.inputs["Fac"])
body_mix = nodes.new("ShaderNodeMix")
body_mix.data_type = "RGBA"
links.new(ramp.outputs["Color"], body_mix.inputs["Factor"])
links.new(coat_rgb.outputs["Color"], body_mix.inputs["A"])
links.new(patch_rgb.outputs["Color"], body_mix.inputs["B"])

orig_node = nodes.new("ShaderNodeVertexColor")
orig_node.layer_name = "orig"
sep = nodes.new("ShaderNodeSeparateColor")
links.new(orig_node.outputs["Color"], sep.inputs["Color"])
is_white = nodes.new("ShaderNodeMath")
is_white.operation = "GREATER_THAN"
is_white.inputs[1].default_value = 0.99
links.new(sep.outputs["Red"], is_white.inputs[0])
is_black = nodes.new("ShaderNodeMath")
is_black.operation = "LESS_THAN"
is_black.inputs[1].default_value = 0.02
links.new(sep.outputs["Red"], is_black.inputs[0])
head_white = nodes.new("ShaderNodeMix")
head_white.data_type = "RGBA"
links.new(is_white.outputs[0], head_white.inputs["Factor"])
links.new(orig_node.outputs["Color"], head_white.inputs["A"])
links.new(coat_rgb.outputs["Color"], head_white.inputs["B"])
head_black = nodes.new("ShaderNodeMix")
head_black.data_type = "RGBA"
links.new(is_black.outputs[0], head_black.inputs["Factor"])
links.new(head_white.outputs["Result"], head_black.inputs["A"])
links.new(patch_rgb.outputs["Color"], head_black.inputs["B"])

body_attr = nodes.new("ShaderNodeAttribute")
body_attr.attribute_name = "body"
body_attr.attribute_type = "GEOMETRY"
choose = nodes.new("ShaderNodeMix")
choose.data_type = "RGBA"
links.new(body_attr.outputs["Fac"], choose.inputs["Factor"])
links.new(head_black.outputs["Result"], choose.inputs["A"])
links.new(body_mix.outputs["Result"], choose.inputs["B"])

# fur grain
fur = nodes.new("ShaderNodeTexNoise")
fur.inputs["Scale"].default_value = 38.0
fur.inputs["Detail"].default_value = 8.0
links.new(coord.outputs["Object"], fur.inputs["Vector"])
fur_map = nodes.new("ShaderNodeMapRange")
fur_map.inputs["From Min"].default_value = 0.3
fur_map.inputs["From Max"].default_value = 0.7
fur_map.inputs["To Min"].default_value = 0.86
fur_map.inputs["To Max"].default_value = 1.06
links.new(fur.outputs["Fac"], fur_map.inputs["Value"])
# belly and legs a little darker than the back
xyz = nodes.new("ShaderNodeSeparateXYZ")
links.new(coord.outputs["Object"], xyz.inputs["Vector"])
height = nodes.new("ShaderNodeMapRange")
height.inputs["From Min"].default_value = 0.8
height.inputs["From Max"].default_value = 4.5
height.inputs["To Min"].default_value = 0.9
height.inputs["To Max"].default_value = 1.0
links.new(xyz.outputs["Z"], height.inputs["Value"])
shade = nodes.new("ShaderNodeMath")
shade.operation = "MULTIPLY"
links.new(fur_map.outputs["Result"], shade.inputs[0])
links.new(height.outputs["Result"], shade.inputs[1])
final = nodes.new("ShaderNodeMix")
final.data_type = "RGBA"
final.blend_type = "MULTIPLY"
final.inputs["Factor"].default_value = 1.0
links.new(choose.outputs["Result"], final.inputs["A"])
links.new(shade.outputs["Value"], final.inputs["B"])
links.new(final.outputs["Result"], bsdf.inputs["Base Color"])

image_node = nodes.new("ShaderNodeTexImage")
nodes.active = image_node
me.materials.clear()
me.materials.append(mat)

scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = 16


def bake(kind, name, colour_space="sRGB"):
    img = bpy.data.images.new(name, size, size, alpha=False)
    img.colorspace_settings.name = colour_space
    image_node.image = img
    nodes.active = image_node
    if kind == "DIFFUSE":
        bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, margin=8)
    else:
        bpy.ops.object.bake(type=kind, margin=8)
    return img


scene.cycles.samples = 64
ao = bake("AO", "ao", "Non-Color")
ao_px = list(ao.pixels)
scene.cycles.samples = 1
os.makedirs(out_dir, exist_ok=True)
for name, (coat, patch) in COATS.items():
    coat_rgb.outputs["Color"].default_value = (*coat, 1.0)
    patch_rgb.outputs["Color"].default_value = (*patch, 1.0)
    img = bake("DIFFUSE", "coat_" + name)
    px = list(img.pixels)
    for i in range(0, len(px), 4):
        occlusion = 0.68 + 0.32 * ao_px[i]
        px[i] *= occlusion
        px[i + 1] *= occlusion
        px[i + 2] *= occlusion
    img.pixels = px
    img.filepath_raw = os.path.join(out_dir, "cow_%s.png" % name)
    img.file_format = "PNG"
    img.save()
    print("BAKED", name)

# 5. export: triangulated, per corner normal and UV, weights per vertex
bm = bmesh.new()
bm.from_mesh(me)
bmesh.ops.triangulate(bm, faces=bm.faces[:])
bm.to_mesh(me)
bm.free()
groups = [g.name for g in cow.vertex_groups]
uv = me.uv_layers.active.data
corners, tris = [], []
for poly in me.polygons:
    tri = []
    for li in poly.loop_indices:
        loop = me.loops[li]
        n = me.corner_normals[li].vector
        tri.append(len(corners))
        corners.append([loop.vertex_index, [n.x, n.y, n.z], [uv[li].uv.x, uv[li].uv.y]])
    tris.append(tri)
weights = [{groups[g.group]: round(g.weight, 5) for g in v.groups if g.weight > 0.001} for v in me.vertices]
verts = [list(cow.matrix_world @ v.co) for v in me.vertices]
json.dump({"verts": verts, "corners": corners, "tris": tris, "weights": weights}, open(out_json, "w"))
print("EXPORTED", len(verts), "verts", len(corners), "corners", len(tris), "tris")
