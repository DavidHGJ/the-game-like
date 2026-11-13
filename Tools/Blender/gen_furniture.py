"""
Gerador procedural de mobiliario low-poly para The Game Like.

Roda headless no Blender:
    blender --background --python Tools/Blender/gen_furniture.py

Exporta um .fbx por peca em Assets/Art/Furniture/<Peca>/, seguindo ART_STYLE.md:
  - pivo no ponto de contato com o chao (base, centro em XY, Z=0);
  - mesh separado em slots de material: *_Fixed e *_Tintable (estofado), para
    troca de cor por material;
  - orientacao canonica (frente = -Y no Blender -> -Z no Unity), 4 direcoes via
    rotacao Y;
  - flat shading low-poly com chanfro generoso (acabamento macio estilo ithappy);
  - escala em metros (1 unidade Unity = 1 m).

Revisao 2026-11-04: paleta clara/saturada e geometria arredondada (estilo
"ithappy interior"), e muitas pecas novas (sofa, estante, luminaria, etc.).
Renderiza um catalogo em Tools/Blender/furniture_preview.png com luz clara.
"""

import math
import os

import bpy
from mathutils import Vector

OUT_DIR = r"C:\Users\david\OneDrive\Documentos\Unity\TheGameLike\My project\Assets\Art\Furniture"
PREVIEW = r"C:\Users\david\OneDrive\Documentos\Unity\TheGameLike\My project\Tools\Blender\furniture_preview.png"

# --------------------------------------------------------------------------
# Paleta (estilo ithappy: clara, levemente saturada, nunca neon). sRGB 0..1.
# --------------------------------------------------------------------------
C = {
    "wood":      (0.70, 0.50, 0.30),
    "wood_lt":   (0.82, 0.64, 0.42),
    "wood_dk":   (0.45, 0.30, 0.18),
    "metal":     (0.26, 0.28, 0.32),
    "metal_lt":  (0.62, 0.65, 0.70),
    "white":     (0.92, 0.92, 0.90),
    "cream":     (0.93, 0.89, 0.80),
    "teal":      (0.20, 0.56, 0.60),
    "teal_dk":   (0.14, 0.42, 0.47),
    "coral":     (0.91, 0.47, 0.42),
    "mustard":   (0.92, 0.72, 0.30),
    "sage":      (0.56, 0.69, 0.50),
    "blue":      (0.34, 0.56, 0.74),
    "pink":      (0.93, 0.74, 0.76),
    "screen":    (0.40, 0.62, 0.72),
    "leaf":      (0.32, 0.60, 0.34),
    "leaf_dk":   (0.22, 0.47, 0.27),
    "terra":     (0.80, 0.46, 0.34),
    "black":     (0.11, 0.12, 0.14),
    "dark":      (0.17, 0.18, 0.21),
}


# --------------------------------------------------------------------------
# Infra
# --------------------------------------------------------------------------

def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def ensure_fbx_exporter():
    try:
        bpy.ops.preferences.addon_enable(module="io_scene_fbx")
    except Exception:
        pass


def make_material(name, rgb, metallic=0.0, roughness=0.55):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf is not None:
        bsdf.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1.0)
        if "Metallic" in bsdf.inputs:
            bsdf.inputs["Metallic"].default_value = metallic
        if "Roughness" in bsdf.inputs:
            bsdf.inputs["Roughness"].default_value = roughness
    mat.diffuse_color = (rgb[0], rgb[1], rgb[2], 1.0)
    return mat


def box(name, size, location, material, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = (math.radians(rot[0]), math.radians(rot[1]), math.radians(rot[2]))
    obj.scale = (size[0], size[1], size[2])
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.data.materials.append(material)
    for p in obj.data.polygons:
        p.use_smooth = False
    return obj


def cylinder(name, radius, depth, location, material, verts=16, rot=(0, 0, 0), scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius, depth=depth, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = (math.radians(rot[0]), math.radians(rot[1]), math.radians(rot[2]))
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.data.materials.append(material)
    for p in obj.data.polygons:
        p.use_smooth = False
    return obj


def cone(name, r1, r2, depth, location, material, verts=16, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2, depth=depth, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.rotation_euler = (math.radians(rot[0]), math.radians(rot[1]), math.radians(rot[2]))
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)
    obj.data.materials.append(material)
    for p in obj.data.polygons:
        p.use_smooth = False
    return obj


def sphere(name, radius, location, material, scale=(1, 1, 1), segs=16, rings=9, smooth=False):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=rings, radius=radius, location=location)
    obj = bpy.context.active_object
    obj.name = name
    obj.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(material)
    for p in obj.data.polygons:
        p.use_smooth = smooth
    return obj


def join(objects, final_name):
    for o in bpy.context.selected_objects:
        o.select_set(False)
    objects = [o for o in objects if o is not None]
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = final_name
    return obj


def set_origin_to_base(obj):
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    min_z = min(c.z for c in corners)
    cx = sum(c.x for c in corners) / 8.0
    cy = sum(c.y for c in corners) / 8.0
    bpy.context.scene.cursor.location = (cx, cy, min_z)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    obj.location = (0.0, 0.0, 0.0)


def add_bevel(obj, width=0.02, segments=3):
    bevel = obj.modifiers.new(name="Bevel", type='BEVEL')
    bevel.width = width
    bevel.segments = segments
    bevel.limit_method = 'ANGLE'
    bevel.angle_limit = math.radians(40)
    bevel.harden_normals = False


def export(obj, piece_name):
    folder = os.path.join(OUT_DIR, piece_name)
    os.makedirs(folder, exist_ok=True)
    path = os.path.join(folder, piece_name + ".fbx")
    for o in bpy.context.selected_objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
        object_types={'MESH'}, mesh_smooth_type='FACE', use_mesh_modifiers=True,
        add_leaf_bones=False, axis_forward='-Z', axis_up='Y', path_mode='COPY')
    print("EXPORTED", path)
    return path


def build_and_export(piece_name, build_fn):
    reset_scene()
    obj = build_fn()
    set_origin_to_base(obj)
    add_bevel(obj)
    return export(obj, piece_name)


# --------------------------------------------------------------------------
# Pecas (metros; frente canonica = -Y no Blender). Base no chao (Z=0).
# --------------------------------------------------------------------------

def m(key, **kw):
    return make_material(key.capitalize() + "_Fixed" if "name" not in kw else kw["name"], C[key], **{x: v for x, v in kw.items() if x != "name"})


def build_desk():
    wood = make_material("Wood_Fixed", C["wood_lt"], roughness=0.5)
    # Tampo cobre quase as 2 células (1.9 x 0.9 m) para o que for apoiado caber inteiro.
    top = box("DeskTop", (1.9, 0.9, 0.07), (0, 0, 0.73), wood)
    legs = [box("Leg", (0.09, 0.09, 0.72), (sx * 0.87, sy * 0.38, 0.36), wood)
            for sx in (-1, 1) for sy in (-1, 1)]
    return join([top] + legs, "Desk")


def build_office_chair():
    fabric = make_material("Fabric_Tintable", C["teal"], roughness=0.7)
    metal = make_material("Metal_Fixed", C["metal"], metallic=0.6, roughness=0.4)
    parts = [
        box("Seat", (0.50, 0.50, 0.12), (0, 0, 0.47), fabric),
        box("Back", (0.48, 0.12, 0.56), (0, 0.21, 0.80), fabric),
        box("ArmL", (0.06, 0.34, 0.06), (-0.27, 0.02, 0.60), metal),
        box("ArmR", (0.06, 0.34, 0.06), (0.27, 0.02, 0.60), metal),
        cylinder("Post", 0.045, 0.34, (0, 0, 0.24), metal, verts=10),
    ]
    for i in range(5):
        a = math.radians(i * 72)
        parts.append(box("Spoke", (0.07, 0.30, 0.05), (0.13 * math.sin(a), 0.13 * math.cos(a), 0.06),
                         metal, rot=(0, 0, math.degrees(a))))
    return join(parts, "OfficeChair")


def build_gaming_chair():
    body = make_material("GChair_Tintable", C["coral"], roughness=0.6)
    black = make_material("GChair_Fixed", C["black"], roughness=0.5)
    metal = make_material("GChairMetal_Fixed", C["metal"], metallic=0.6, roughness=0.4)
    parts = [
        box("Seat", (0.52, 0.54, 0.14), (0, 0, 0.47), black),
        box("SeatPad", (0.40, 0.46, 0.06), (0, 0, 0.55), body),
        box("Back", (0.52, 0.14, 0.72), (0, 0.22, 0.92), black),
        box("BackPad", (0.38, 0.08, 0.62), (0, 0.16, 0.90), body),
        box("WingL", (0.07, 0.12, 0.60), (-0.235, 0.18, 0.92), body),
        box("WingR", (0.07, 0.12, 0.60), (0.235, 0.18, 0.92), body),
        box("HeadRest", (0.26, 0.10, 0.14), (0, 0.17, 1.30), body),
        cylinder("Post", 0.05, 0.34, (0, 0, 0.24), metal, verts=10),
    ]
    for i in range(5):
        a = math.radians(i * 72)
        parts.append(box("Spoke", (0.08, 0.32, 0.05), (0.14 * math.sin(a), 0.14 * math.cos(a), 0.06),
                         metal, rot=(0, 0, math.degrees(a))))
    return join(parts, "GamingChair")


def build_monitor():
    dark = make_material("Plastic_Dark", C["dark"], roughness=0.45)
    screen = make_material("Screen_Tintable", C["screen"], roughness=0.2)
    parts = [
        box("Bezel", (0.60, 0.05, 0.37), (0, 0, 0.64), dark),
        box("Panel", (0.55, 0.02, 0.32), (0, -0.035, 0.64), screen),
        box("Neck", (0.05, 0.05, 0.18), (0, 0.02, 0.38), dark),
        box("Foot", (0.28, 0.18, 0.03), (0, 0.02, 0.28), dark),
    ]
    return join(parts, "Monitor")


def build_workstation():
    wood = make_material("Wood_Fixed", C["wood_lt"], roughness=0.5)
    dark = make_material("Plastic_Dark", C["dark"], roughness=0.45)
    screen = make_material("Screen_Tintable", C["screen"], roughness=0.2)
    parts = [box("DeskTop", (1.9, 0.9, 0.07), (0, 0, 0.73), wood)]
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(box("Leg", (0.09, 0.09, 0.72), (sx * 0.87, sy * 0.38, 0.36), wood))
    for dx in (-0.37, 0.37):
        parts.append(box("MonBezel", (0.56, 0.05, 0.34), (dx, -0.28, 1.02), dark, rot=(0, 0, -8 if dx < 0 else 8)))
        parts.append(box("MonPanel", (0.50, 0.02, 0.29), (dx, -0.30, 1.02), screen, rot=(0, 0, -8 if dx < 0 else 8)))
        parts.append(box("MonNeck", (0.05, 0.05, 0.14), (dx, -0.26, 0.80), dark))
        parts.append(box("MonFoot", (0.24, 0.15, 0.03), (dx, -0.26, 0.76), dark))
    parts.append(box("Keyboard", (0.46, 0.16, 0.03), (0, 0.16, 0.77), dark))
    parts.append(box("Mouse", (0.07, 0.11, 0.03), (0.32, 0.16, 0.77), dark))
    return join(parts, "Workstation")


def build_sofa():
    body = make_material("Sofa_Tintable", C["teal"], roughness=0.75)
    foot = make_material("SofaFoot_Fixed", C["wood_dk"], roughness=0.5)
    parts = [
        box("Base", (1.9, 0.85, 0.28), (0, 0, 0.30), body),
        box("Back", (1.9, 0.20, 0.55), (0, 0.33, 0.62), body),
        box("ArmL", (0.22, 0.85, 0.48), (-0.84, 0, 0.52), body),
        box("ArmR", (0.22, 0.85, 0.48), (0.84, 0, 0.52), body),
        box("CushL", (0.80, 0.62, 0.16), (-0.42, -0.05, 0.50), body),
        box("CushR", (0.80, 0.62, 0.16), (0.42, -0.05, 0.50), body),
    ]
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(cylinder("Foot", 0.05, 0.12, (sx * 0.78, sy * 0.32, 0.06), foot, verts=8))
    return join(parts, "Sofa")


def build_armchair():
    body = make_material("Arm_Tintable", C["coral"], roughness=0.75)
    foot = make_material("ArmFoot_Fixed", C["wood_dk"], roughness=0.5)
    parts = [
        box("Base", (0.85, 0.80, 0.30), (0, 0, 0.32), body),
        box("Back", (0.85, 0.18, 0.52), (0, 0.31, 0.62), body),
        box("ArmL", (0.16, 0.80, 0.42), (-0.35, 0, 0.52), body),
        box("ArmR", (0.16, 0.80, 0.42), (0.35, 0, 0.52), body),
        box("Cush", (0.62, 0.58, 0.14), (0, -0.04, 0.52), body),
    ]
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(cylinder("Foot", 0.045, 0.12, (sx * 0.34, sy * 0.30, 0.06), foot, verts=8))
    return join(parts, "Armchair")


def build_coffee_table():
    wood = make_material("Wood_Fixed", C["wood"], roughness=0.5)
    top = box("Top", (0.94, 0.78, 0.06), (0, 0, 0.40), wood)
    legs = [box("Leg", (0.07, 0.07, 0.38), (sx * 0.40, sy * 0.32, 0.19), wood)
            for sx in (-1, 1) for sy in (-1, 1)]
    return join([top] + legs, "CoffeeTable")


def build_bookshelf():
    wood = make_material("Shelf_Fixed", C["wood"], roughness=0.5)
    cols = [C["coral"], C["teal"], C["mustard"], C["sage"], C["blue"], C["pink"]]
    parts = [
        box("SideL", (0.06, 0.32, 1.80), (-0.52, 0, 0.90), wood),
        box("SideR", (0.06, 0.32, 1.80), (0.52, 0, 0.90), wood),
        box("Topp", (1.10, 0.32, 0.05), (0, 0, 1.78), wood),
    ]
    shelf_z = [0.03, 0.45, 0.90, 1.35]
    for z in shelf_z:
        parts.append(box("Shelf", (1.02, 0.32, 0.04), (0, 0, z), wood))
    # livros coloridos em pe em algumas prateleiras
    for si, z in enumerate(shelf_z[:3]):
        x = -0.44
        bi = 0
        while x < 0.40:
            h = 0.30 + 0.06 * ((bi + si) % 3)
            w = 0.05 + 0.02 * (bi % 2)
            col = make_material(f"Book_{si}_{bi}_Fixed", cols[(bi + si) % len(cols)], roughness=0.6)
            parts.append(box("Book", (w, 0.22, h), (x + w / 2, 0, z + 0.04 + h / 2), col))
            x += w + 0.012
            bi += 1
    return join(parts, "Bookshelf")


def build_wall_shelf():
    wood = make_material("WallShelf_Fixed", C["wood"], roughness=0.5)
    cols = [C["coral"], C["teal"], C["mustard"], C["sage"]]
    parts = [box("Plank", (0.9, 0.22, 0.05), (0, 0, 1.30), wood)]
    x = -0.34
    for bi in range(6):
        h = 0.26 + 0.05 * (bi % 3)
        w = 0.05 + 0.015 * (bi % 2)
        col = make_material(f"WShelfBook_{bi}_Fixed", cols[bi % len(cols)], roughness=0.6)
        parts.append(box("Book", (w, 0.16, h), (x + w / 2, 0, 1.325 + h / 2), col))
        x += w + 0.012
    return join(parts, "WallShelf")


def build_floor_lamp():
    metal = make_material("LampMetal_Fixed", C["metal"], metallic=0.5, roughness=0.4)
    shade = make_material("LampShade_Tintable", C["cream"], roughness=0.6)
    parts = [
        cylinder("Base", 0.18, 0.04, (0, 0, 0.02), metal, verts=16),
        cylinder("Pole", 0.025, 1.45, (0, 0, 0.74), metal, verts=8),
        cone("Shade", 0.22, 0.14, 0.26, (0, 0, 1.56), shade, verts=16),
    ]
    return join(parts, "FloorLamp")


def build_plant():
    pot = make_material("Pot_Fixed", C["terra"], roughness=0.6)
    leaf = make_material("Leaf_Fixed", C["leaf"], roughness=0.7)
    leaf2 = make_material("LeafDk_Fixed", C["leaf_dk"], roughness=0.7)
    parts = [
        cone("Pot", 0.17, 0.21, 0.30, (0, 0, 0.15), pot, verts=12),
        cylinder("Trunk", 0.035, 0.25, (0, 0, 0.42), make_material("Trunk_Fixed", C["wood_dk"]), verts=6),
        sphere("Foliage", 0.30, (0, 0, 0.72), leaf, scale=(1, 1, 0.9), segs=10, rings=6),
        sphere("FoliageTop", 0.20, (0.06, 0.04, 0.95), leaf2, segs=8, rings=5),
    ]
    return join(parts, "Plant")


def build_plant_small():
    pot = make_material("PotS_Fixed", C["white"], roughness=0.5)
    leaf = make_material("LeafS_Fixed", C["sage"], roughness=0.7)
    parts = [
        cylinder("Pot", 0.12, 0.16, (0, 0, 0.08), pot, verts=12),
        sphere("Bush", 0.17, (0, 0, 0.26), leaf, scale=(1, 1, 0.85), segs=8, rings=5),
        sphere("Bush2", 0.12, (0.05, -0.03, 0.34), leaf, segs=8, rings=5),
    ]
    return join(parts, "PlantSmall")


def build_rug():
    rug = make_material("Rug_Tintable", C["mustard"], roughness=0.9)
    trim = make_material("RugTrim_Fixed", C["teal_dk"], roughness=0.9)
    base = box("Rug", (2.6, 1.8, 0.02), (0, 0, 0.012), rug)
    inner = box("RugInner", (2.2, 1.4, 0.021), (0, 0, 0.014), trim)
    return join([base, inner], "Rug")


def build_rug_round():
    rug = make_material("RugR_Tintable", C["sage"], roughness=0.9)
    trim = make_material("RugRTrim_Fixed", C["cream"], roughness=0.9)
    base = cylinder("RugR", 1.1, 0.02, (0, 0, 0.011), rug, verts=28)
    inner = cylinder("RugRInner", 0.8, 0.021, (0, 0, 0.013), trim, verts=28)
    return join([base, inner], "RugRound")


def build_whiteboard():
    frame = make_material("WBFrame_Fixed", C["metal_lt"], metallic=0.4, roughness=0.4)
    board = make_material("WBBoard_Fixed", C["white"], roughness=0.3)
    parts = [
        box("Frame", (1.5, 0.05, 1.0), (0, 0, 1.30), frame),
        box("Board", (1.42, 0.02, 0.92), (0, -0.03, 1.30), board),
        box("LegL", (0.05, 0.05, 0.85), (-0.6, 0.1, 0.42), frame),
        box("LegR", (0.05, 0.05, 0.85), (0.6, 0.1, 0.42), frame),
        box("Foot", (1.3, 0.5, 0.04), (0, 0.1, 0.02), frame),
    ]
    return join(parts, "Whiteboard")


def build_server_rack():
    cab = make_material("Rack_Fixed", C["dark"], roughness=0.5)
    parts = [box("Cabinet", (0.6, 0.7, 1.7), (0, 0, 0.85), cab)]
    leds = [C["sage"], C["coral"], C["mustard"], C["teal"]]
    for i in range(7):
        z = 0.25 + i * 0.20
        parts.append(box("Slot", (0.5, 0.02, 0.14), (0, -0.35, z), make_material("RackSlot_Fixed", C["black"], roughness=0.4)))
        parts.append(box("Led", (0.03, 0.02, 0.03), (0.18, -0.37, z), make_material(f"Led_{i}_Fixed", leds[i % 4], roughness=0.3)))
    return join(parts, "ServerRack")


def build_mini_fridge():
    body = make_material("Fridge_Fixed", C["white"], roughness=0.4)
    handle = make_material("FridgeHandle_Fixed", C["metal_lt"], metallic=0.5, roughness=0.4)
    parts = [
        box("Body", (0.6, 0.6, 0.85), (0, 0, 0.43), body),
        box("DoorLine", (0.61, 0.61, 0.02), (0, 0, 0.55), make_material("FridgeLine_Fixed", C["metal_lt"], roughness=0.4)),
        box("Handle", (0.04, 0.05, 0.22), (0.24, -0.30, 0.72), handle),
    ]
    return join(parts, "MiniFridge")


def build_beanbag():
    body = make_material("Bean_Tintable", C["mustard"], roughness=0.85)
    base = sphere("Bean", 0.45, (0, 0, 0.30), body, scale=(1.0, 1.0, 0.62), segs=14, rings=8)
    dent = sphere("BeanDent", 0.26, (0, -0.04, 0.46), body, scale=(1, 1, 0.5), segs=10, rings=6)
    return join([base, dent], "Beanbag")


def build_cardboard_box():
    card = make_material("Cardboard_Fixed", C["wood_lt"], roughness=0.85)
    tape = make_material("Tape_Fixed", C["cream"], roughness=0.7)
    body = box("BoxBody", (0.5, 0.5, 0.45), (0, 0, 0.225), card)
    flap1 = box("Flap1", (0.5, 0.23, 0.03), (0, -0.135, 0.47), card)
    flap2 = box("Flap2", (0.5, 0.23, 0.03), (0, 0.135, 0.47), card)
    seam = box("Seam", (0.06, 0.5, 0.005), (0, 0, 0.485), tape)
    return join([body, flap1, flap2, seam], "CardboardBox")


def build_window():
    # Janela de parede: moldura + vidro aceso (material 'Window' -> glow branco).
    # Fina em Y (encosta na parede), larga em X, alta em Z. Pivô na base.
    frame = make_material("WindowFrame_Fixed", C["white"], roughness=0.5)
    pane = make_material("WindowPane_Window", (0.96, 0.98, 1.0), roughness=0.1)
    parts = [
        box("Frame", (1.3, 0.07, 1.3), (0, 0, 0.65), frame),
        box("Pane", (1.16, 0.03, 1.16), (0, -0.025, 0.65), pane),
        box("MullionV", (0.04, 0.05, 1.16), (0, -0.04, 0.65), frame),
        box("MullionH", (1.16, 0.05, 0.04), (0, -0.04, 0.65), frame),
    ]
    return join(parts, "Window")


def build_poster():
    frame = make_material("PosterFrame_Fixed", C["dark"], roughness=0.5)
    art = make_material("PosterArt_Tintable", C["blue"], roughness=0.5)
    backing = box("PosterFrame", (0.7, 0.03, 1.0), (0, 0, 0.5), frame)
    sheet = box("PosterArt", (0.62, 0.02, 0.9), (0, -0.02, 0.5), art)
    return join([backing, sheet], "Poster")


def build_clock():
    frame = make_material("ClockFrame_Fixed", C["dark"], roughness=0.5)
    face = make_material("ClockFace_Fixed", C["white"], roughness=0.4)
    hand = make_material("ClockHand_Fixed", C["coral"], roughness=0.4)
    parts = [
        cylinder("Rim", 0.24, 0.05, (0, 0, 0), frame, verts=24, rot=(90, 0, 0)),
        cylinder("Face", 0.21, 0.051, (0, -0.005, 0), face, verts=24, rot=(90, 0, 0)),
        box("HandH", (0.03, 0.02, 0.13), (0, -0.03, 0.05), hand),
        box("HandM", (0.02, 0.02, 0.18), (0.06, -0.03, 0.0), hand, rot=(0, 30, 0)),
    ]
    return join(parts, "Clock")


def build_water_cooler():
    body = make_material("Cooler_Fixed", C["white"], roughness=0.4)
    water = make_material("CoolerWater_Tintable", C["blue"], roughness=0.2)
    parts = [
        box("Body", (0.38, 0.38, 0.95), (0, 0, 0.48), body),
        box("Tray", (0.30, 0.10, 0.04), (0, -0.20, 0.55), make_material("CoolerTray_Fixed", C["metal_lt"], roughness=0.4)),
        cylinder("Bottle", 0.17, 0.42, (0, 0, 1.18), water, verts=16),
        cone("BottleNeck", 0.17, 0.06, 0.12, (0, 0, 0.96), water, verts=16),
    ]
    return join(parts, "WaterCooler")


def build_desk_lamp():
    # Abajur de mesa: haste sobe, braço vai pra frente (-Y) e a cúpula fica na
    # ponta apontando PRA BAIXO (abertura voltada ao tampo). A luz sai da cúpula.
    metal = make_material("DLampMetal_Fixed", C["metal"], metallic=0.5, roughness=0.4)
    shade = make_material("DLampShade_Tintable", C["mustard"], roughness=0.5)
    parts = [
        cylinder("Base", 0.11, 0.03, (0, 0, 0.015), metal, verts=16),
        cylinder("Pole", 0.018, 0.44, (0, 0, 0.23), metal, verts=8),
        cylinder("Arm", 0.015, 0.28, (0, -0.12, 0.44), metal, verts=6, rot=(90, 0, 0)),
        # cone: extremidade larga embaixo (abertura pra baixo) — sem rotação.
        cone("Shade", 0.10, 0.035, 0.13, (0, -0.25, 0.40), shade, verts=16),
    ]
    return join(parts, "DeskLamp")


def build_light_point():
    """Ponto de luz avulso: uma lâmpada decorativa que o dev planta no editor.
    O brilho real vem da point light embutida na definição; o orbe só marca o
    lugar e acende (material 'Bulb' recebe emissão no OfficeBuilder)."""
    bulb = make_material("Bulb_Fixed", C["mustard"], roughness=0.3)
    metal = make_material("BulbCap_Fixed", C["metal"], metallic=0.6, roughness=0.4)
    parts = [
        sphere("Bulb", 0.11, (0, 0, 0.14), bulb, scale=(1, 1, 1.1), segs=14, rings=8, smooth=True),
        cylinder("Cap", 0.05, 0.05, (0, 0, 0.26), metal, verts=12),
    ]
    return join(parts, "LightPoint")


def build_arcade():
    body = make_material("Arcade_Fixed", C["teal_dk"], roughness=0.5)
    screen = make_material("ArcadeScreen_Tintable", C["screen"], roughness=0.2)
    accent = make_material("ArcadeAccent_Fixed", C["coral"], roughness=0.5)
    parts = [
        box("Cabinet", (0.6, 0.7, 1.7), (0, 0, 0.85), body),
        box("Screen", (0.5, 0.02, 0.45), (0, -0.33, 1.30), screen),
        box("Marquee", (0.6, 0.1, 0.22), (0, -0.30, 1.62), accent),
        box("Panel", (0.6, 0.35, 0.08), (0, -0.18, 0.95), accent, rot=(20, 0, 0)),
        cylinder("Joy", 0.03, 0.12, (-0.12, -0.30, 1.02), make_material("ArcadeJoy_Fixed", C["black"]), verts=8),
        sphere("JoyBall", 0.04, (-0.12, -0.30, 1.09), accent, segs=8, rings=5),
        sphere("BtnA", 0.035, (0.08, -0.28, 1.0), make_material("BtnA_Fixed", C["mustard"]), segs=8, rings=5),
        sphere("BtnB", 0.035, (0.17, -0.28, 1.0), make_material("BtnB_Fixed", C["sage"]), segs=8, rings=5),
    ]
    return join(parts, "Arcade")


# --------------------------------------------------------------------------
# Pecas da GARAGEM (cenario inicial): carrinho, armario+caixas, mesinha,
# PC antigo (CRT), cadeira antiga.
# --------------------------------------------------------------------------

def build_car():
    body = make_material("CarBody_Tintable", C["coral"], roughness=0.35)
    glass = make_material("CarGlass_Fixed", C["dark"], roughness=0.15)
    tire = make_material("CarTire_Fixed", C["black"], roughness=0.85)
    rim = make_material("CarRim_Fixed", C["metal_lt"], metallic=0.6, roughness=0.35)
    chrome = make_material("CarChrome_Fixed", C["metal_lt"], metallic=0.7, roughness=0.3)
    head = make_material("CarHead_Fixed", C["cream"], roughness=0.3)
    tail = make_material("CarTail_Fixed", C["terra"], roughness=0.3)
    L, W = 3.6, 1.6
    parts = [
        # corpo inferior (capô + porta-malas), cantos macios via bevel
        box("Lower", (L, W, 0.36), (0, 0, 0.53), body),
        # cabine elevada, mais curta e levemente estreita
        box("Cabin", (L * 0.48, W * 0.88, 0.34), (0, 0, 0.86), body),
        box("Roof", (L * 0.40, W * 0.80, 0.07), (0, 0, 1.02), body),
        # faixa de vidros em volta da cabine
        box("WinF", (0.04, W * 0.74, 0.24), (L * 0.24, 0, 0.85), glass),
        box("WinB", (0.04, W * 0.74, 0.24), (-L * 0.24, 0, 0.85), glass),
        box("WinL", (L * 0.44, 0.04, 0.24), (0, -W * 0.44, 0.85), glass),
        box("WinR", (L * 0.44, 0.04, 0.24), (0, W * 0.44, 0.85), glass),
        # para-choques cromados
        box("BumperF", (0.14, W * 0.92, 0.16), (L * 0.5, 0, 0.42), chrome),
        box("BumperB", (0.14, W * 0.92, 0.16), (-L * 0.5, 0, 0.42), chrome),
        # faróis e lanternas
        box("HeadL", (0.05, 0.26, 0.12), (L * 0.5, -0.46, 0.58), head),
        box("HeadR", (0.05, 0.26, 0.12), (L * 0.5, 0.46, 0.58), head),
        box("TailL", (0.05, 0.22, 0.10), (-L * 0.5, -0.46, 0.58), tail),
        box("TailR", (0.05, 0.22, 0.10), (-L * 0.5, 0.46, 0.58), tail),
    ]
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(cylinder("Wheel", 0.34, 0.22, (sx * L * 0.32, sy * W * 0.52, 0.34), tire, verts=20, rot=(90, 0, 0)))
            parts.append(cylinder("Rim", 0.16, 0.24, (sx * L * 0.32, sy * W * 0.52, 0.34), rim, verts=16, rot=(90, 0, 0)))
    return join(parts, "Car")


def build_cabinet():
    wood = make_material("CabWood_Fixed", C["wood"], roughness=0.6)
    metal = make_material("CabHandle_Fixed", C["metal"], metallic=0.5, roughness=0.4)
    card = make_material("CabBox_Fixed", C["wood_lt"], roughness=0.85)
    tape = make_material("CabTape_Fixed", C["cream"], roughness=0.7)
    W, D, H = 0.9, 0.5, 1.4
    parts = [
        box("Cab", (W, D, H), (0, 0, H / 2), wood),
        box("Seam", (0.015, D * 0.02, H * 0.82), (0, -D * 0.5, H * 0.5), metal),
        box("HandleL", (0.04, 0.06, 0.14), (-0.18, -D * 0.5, H * 0.52), metal),
        box("HandleR", (0.04, 0.06, 0.14), (0.18, -D * 0.5, H * 0.52), metal),
        box("Shelf", (W, D * 0.9, 0.03), (0, 0, H * 0.5), metal),
        box("TopBox1", (0.5, 0.46, 0.4), (-0.14, 0.0, H + 0.2), card),
        box("TopBox1Tape", (0.06, 0.46, 0.005), (-0.14, 0.0, H + 0.4), tape),
        box("TopBox2", (0.38, 0.36, 0.3), (0.22, 0.04, H + 0.15), card),
    ]
    return join(parts, "Cabinet")


def build_old_desk():
    wood = make_material("OldDeskWood_Fixed", C["wood_dk"], roughness=0.72)
    # Ocupa 1 célula: tampo de 0.96 x 0.9 m — cabe o PC antigo inteiro (com teclado e mouse).
    top = box("Top", (0.96, 0.9, 0.06), (0, 0, 0.73), wood)
    legs = [box("Leg", (0.07, 0.07, 0.72), (sx * 0.42, sy * 0.39, 0.36), wood)
            for sx in (-1, 1) for sy in (-1, 1)]
    apron = box("Apron", (0.84, 0.06, 0.1), (0, 0.39, 0.63), wood)
    return join([top, apron] + legs, "OldDesk")


def build_old_pc():
    beige = make_material("PCBeige_Fixed", C["cream"], roughness=0.6)
    scr = make_material("PCScreen_Fixed", C["screen"], roughness=0.2)
    dark = make_material("PCDark_Fixed", C["dark"], roughness=0.5)
    parts = [
        box("CRTfront", (0.46, 0.30, 0.42), (0, -0.05, 0.23), beige),
        box("CRTscreen", (0.34, 0.02, 0.30), (0, -0.205, 0.26), scr),
        # Conjunto compacto (0.75 x 0.72 m): cabe num tampo de 1 célula sem nada flutuar.
        box("CRTback", (0.34, 0.20, 0.34), (0, 0.19, 0.24), beige),
        box("Stand", (0.30, 0.26, 0.04), (0, 0, 0.02), dark),
        box("Keyboard", (0.42, 0.15, 0.03), (-0.06, -0.33, 0.015), beige),
        box("KeyPad", (0.38, 0.11, 0.012), (-0.06, -0.33, 0.032), dark),
        box("Mouse", (0.07, 0.11, 0.03), (0.26, -0.33, 0.015), beige),
    ]
    return join(parts, "OldPC")


def build_old_chair():
    wood = make_material("OldChairWood_Fixed", C["wood"], roughness=0.7)
    parts = [
        box("Seat", (0.42, 0.42, 0.06), (0, 0, 0.46), wood),
        box("BackTop", (0.42, 0.06, 0.08), (0, 0.18, 0.92), wood),
        box("Slat1", (0.07, 0.05, 0.5), (-0.13, 0.18, 0.72), wood),
        box("Slat2", (0.07, 0.05, 0.5), (0.13, 0.18, 0.72), wood),
    ]
    legs = [box("Leg", (0.05, 0.05, 0.46), (sx * 0.18, sy * 0.18, 0.23), wood)
            for sx in (-1, 1) for sy in (-1, 1)]
    return join(parts + legs, "OldChair")


PIECES = {
    "Car": build_car,
    "Cabinet": build_cabinet,
    "OldDesk": build_old_desk,
    "OldPC": build_old_pc,
    "OldChair": build_old_chair,
    "Workstation": build_workstation,
    "Desk": build_desk,
    "OfficeChair": build_office_chair,
    "GamingChair": build_gaming_chair,
    "Monitor": build_monitor,
    "Sofa": build_sofa,
    "Armchair": build_armchair,
    "CoffeeTable": build_coffee_table,
    "Bookshelf": build_bookshelf,
    "WallShelf": build_wall_shelf,
    "FloorLamp": build_floor_lamp,
    "DeskLamp": build_desk_lamp,
    "LightPoint": build_light_point,
    "Plant": build_plant,
    "PlantSmall": build_plant_small,
    "Rug": build_rug,
    "RugRound": build_rug_round,
    "Whiteboard": build_whiteboard,
    "Window": build_window,
    "ServerRack": build_server_rack,
    "MiniFridge": build_mini_fridge,
    "Beanbag": build_beanbag,
    "CardboardBox": build_cardboard_box,
    "Poster": build_poster,
    "Clock": build_clock,
    "WaterCooler": build_water_cooler,
    "Arcade": build_arcade,
}


# --------------------------------------------------------------------------
# Preview: catalogo claro, camera ortografica iso.
# --------------------------------------------------------------------------

def render_preview():
    reset_scene()
    ensure_fbx_exporter()
    scene = bpy.context.scene

    floor_mat = make_material("Preview_Floor", (0.90, 0.87, 0.80), roughness=0.9)
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0, 0, -0.001))
    floor = bpy.context.active_object
    floor.scale = (60, 26, 1)
    bpy.ops.object.transform_apply(scale=True)
    floor.data.materials.append(floor_mat)

    names = list(PIECES.keys())
    cols = 6
    sx, sy = 3.4, 3.6
    rows = (len(names) + cols - 1) // cols
    x0 = -(cols - 1) * sx / 2.0
    y0 = (rows - 1) * sy / 2.0
    for i, nm in enumerate(names):
        obj = PIECES[nm]()
        set_origin_to_base(obj)
        add_bevel(obj)
        cx = x0 + (i % cols) * sx
        cy = y0 - (i // cols) * sy
        obj.location = (cx, cy, 0)

    # View transform Standard: o AgX/Filmic padrao do Blender desatura muito e
    # nao representa a cor real do asset (no Unity/URP as cores saem saturadas).
    try:
        scene.view_settings.view_transform = 'Standard'
        scene.view_settings.look = 'None'
    except Exception:
        pass

    if scene.world is None:
        scene.world = bpy.data.worlds.new("W")
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.82, 0.85, 0.88, 1)
        bg.inputs[1].default_value = 0.55

    tgt = bpy.data.objects.new("T", None)
    tgt.location = (0, 0, 0.5)
    scene.collection.objects.link(tgt)
    cam_d = bpy.data.cameras.new("C")
    cam_d.type = 'ORTHO'
    cam_d.ortho_scale = 22
    cam = bpy.data.objects.new("C", cam_d)
    cam.location = (16, -18, 15)
    scene.collection.objects.link(cam)
    con = cam.constraints.new('TRACK_TO')
    con.target = tgt
    con.track_axis = 'TRACK_NEGATIVE_Z'
    con.up_axis = 'UP_Y'
    scene.camera = cam

    sun_d = bpy.data.lights.new("S", 'SUN')
    sun_d.energy = 3.0
    try:
        sun_d.angle = math.radians(5)
    except Exception:
        pass
    sun = bpy.data.objects.new("S", sun_d)
    sun.rotation_euler = (math.radians(52), math.radians(8), math.radians(-35))
    scene.collection.objects.link(sun)

    fill_d = bpy.data.lights.new("F", 'AREA')
    fill_d.energy = 160
    fill_d.size = 20
    fill = bpy.data.objects.new("F", fill_d)
    fill.location = (-14, -16, 16)
    fc = fill.constraints.new('TRACK_TO')
    fc.target = tgt
    fc.track_axis = 'TRACK_NEGATIVE_Z'
    fc.up_axis = 'UP_Y'
    scene.collection.objects.link(fill)

    for eng in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE', 'CYCLES'):
        try:
            scene.render.engine = eng
            break
        except Exception:
            continue
    scene.render.resolution_x = 1500
    scene.render.resolution_y = 850
    scene.render.filepath = PREVIEW
    try:
        bpy.ops.render.render(write_still=True)
        print("RENDERED", PREVIEW)
    except Exception as ex:
        print("RENDER_FAILED", repr(ex))


def main():
    ensure_fbx_exporter()
    os.makedirs(OUT_DIR, exist_ok=True)
    # GL_ONLY="Desk,OldPC" reexporta só essas peças (e pula o catálogo).
    only = [n for n in os.environ.get("GL_ONLY", "").split(",") if n]
    results = []
    for name, fn in PIECES.items():
        if only and name not in only:
            continue
        try:
            path = build_and_export(name, fn)
            results.append((name, True))
        except Exception as exc:  # noqa: BLE001
            print("FAILED", name, repr(exc))
            results.append((name, False))
    if not only:
        render_preview()
    print("=== SUMMARY ===")
    for name, ok in results:
        print(("OK  " if ok else "ERR ") + name)


if __name__ == "__main__":
    main()
