"""
Gerador procedural do personagem "dev" (fundador) sentado, low-poly, para o
escritório de The Game Like. Pose sentada, pronta para ficar na cadeira
(OfficeChair, assento ~0.47m) operando o computador.

    blender --background --python Tools/Blender/gen_character.py

Exporta Assets/Art/Characters/Dev/Dev.fbx (mesh única "Dev", pivô na base,
frente = -Y -> -Z no Unity, flat shading). Submesh tintável = camisa, para
variação de cor por material. Renderiza um preview em <scratchpad>/dev_preview.png.

Revisão 2026-11-04: trocado o corpo todo-de-caixas por primitivas arredondadas
(cabeca esferica, tronco afunilado oval, membros cilindricos facetados) para
sair do visual "cubico". Continua low-poly (flat shading, poucas faces).

Animacoes (digitar / ocioso / cochilo) e as bolinhas de ponto saindo da cabeca
serao feitas depois no Unity (procedural), sobre este mesh.
"""

import math
import os
import bpy
from mathutils import Vector

OUT_DIR = r"C:\Users\david\OneDrive\Documentos\Unity\TheGameLike\My project\Assets\Art\Characters\Dev"
PREVIEW = r"C:\Users\david\AppData\Local\Temp\claude\16b0273e3f71106e\aa7ddef6-5ee0-4014-9684-043325e028e6\scratchpad\dev_preview.png"


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def mat(name, rgb, rough=0.6):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    if b:
        b.inputs["Base Color"].default_value = (rgb[0], rgb[1], rgb[2], 1)
        if "Roughness" in b.inputs:
            b.inputs["Roughness"].default_value = max(0.15, rough * 0.7)
    m.diffuse_color = (rgb[0], rgb[1], rgb[2], 1)
    return m


def _finish(o, name, rot, scale, material, smooth):
    o.name = name
    o.rotation_euler = (math.radians(rot[0]), math.radians(rot[1]), math.radians(rot[2]))
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    o.data.materials.append(material)
    for p in o.data.polygons:
        p.use_smooth = smooth
    return o


def box(name, size, loc, material, rot=(0, 0, 0), smooth=False):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    return _finish(bpy.context.active_object, name, rot,
                   (size[0], size[1], size[2]), material, smooth)


def cyl(name, radius, depth, loc, material, rot=(0, 0, 0), scale=(1, 1, 1),
        verts=12, smooth=False):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=radius,
                                         depth=depth, location=loc)
    return _finish(bpy.context.active_object, name, rot, scale, material, smooth)


def cone(name, r1, r2, depth, loc, material, rot=(0, 0, 0), scale=(1, 1, 1),
         verts=14, smooth=False):
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=r1, radius2=r2,
                                    depth=depth, location=loc)
    return _finish(bpy.context.active_object, name, rot, scale, material, smooth)


def sphere(name, radius, loc, material, scale=(1, 1, 1), segs=16, rings=9,
           smooth=True):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segs, ring_count=rings,
                                         radius=radius, location=loc)
    return _finish(bpy.context.active_object, name, (0, 0, 0), scale, material, smooth)


def join(objs, name):
    for o in bpy.context.selected_objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = name
    return o


def origin_base(o):
    corners = [o.matrix_world @ Vector(c) for c in o.bound_box]
    min_z = min(c.z for c in corners)
    cx = sum(c.x for c in corners) / 8.0
    cy = sum(c.y for c in corners) / 8.0
    bpy.context.scene.cursor.location = (cx, cy, min_z)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    o.location = (0, 0, 0)


def build():
    skin = mat("Dev_Skin", (0.86, 0.66, 0.52))
    hair = mat("Dev_Hair", (0.22, 0.15, 0.11))
    shirt = mat("Dev_Shirt_Tintable", (0.24, 0.50, 0.62))
    pants = mat("Dev_Pants", (0.26, 0.29, 0.35))
    shoe = mat("Dev_Shoe", (0.13, 0.13, 0.16))

    parts = []

    # Quadril: cilindro achatado e oval (mais largo que fundo).
    parts.append(cyl("Hips", 0.16, 0.20, (0, 0, 0.50), pants,
                     scale=(1.05, 0.80, 1.0), verts=12))

    # Pernas: coxas horizontais (-Y), canelas verticais, pes arredondados.
    for sx in (-1, 1):
        x = sx * 0.09
        parts.append(cyl("Thigh", 0.075, 0.34, (x, -0.16, 0.49), pants,
                         rot=(90, 0, 0), verts=12))
        parts.append(cyl("Shin", 0.062, 0.42, (x, -0.32, 0.23), pants, verts=12))
        parts.append(box("Foot", (0.12, 0.22, 0.07), (x, -0.40, 0.04), shoe))

    # Tronco: cone afunilado (ombro mais largo que cintura), secao oval.
    parts.append(cone("Torso", 0.15, 0.185, 0.38, (0, -0.02, 0.77), shirt,
                      scale=(1.08, 0.72, 1.0), verts=16))

    # Pescoco + cabeca esferica + cabelo (calota + nuca).
    parts.append(cyl("Neck", 0.052, 0.10, (0, -0.02, 0.975), skin, verts=10))
    parts.append(sphere("Head", 0.125, (0, -0.045, 1.12), skin,
                        scale=(1.0, 1.04, 1.10)))
    # Cabelo: calota puxada para tras/cima para deixar a testa a mostra.
    parts.append(sphere("Hair", 0.133, (0, 0.03, 1.195), hair,
                        scale=(1.06, 1.08, 0.70)))
    parts.append(sphere("HairBack", 0.118, (0, 0.085, 1.12), hair,
                        scale=(1.00, 0.52, 0.95)))
    # Nariz pequeno so para dar frente ao rosto (meio do rosto).
    parts.append(box("Nose", (0.03, 0.05, 0.035), (0, -0.165, 1.075), skin))

    # Bracos: ombro->antebraco a frente (maos no teclado ~0.72).
    for sx in (-1, 1):
        x = sx * 0.20
        parts.append(cyl("UpperArm", 0.052, 0.26, (x, -0.02, 0.80), shirt,
                         rot=(18, 0, 0), verts=10))
        parts.append(cyl("Forearm", 0.045, 0.27, (sx * 0.165, -0.22, 0.735),
                         skin, rot=(90, 0, 0), verts=10))
        parts.append(sphere("Hand", 0.055, (sx * 0.145, -0.37, 0.72), skin,
                            scale=(1.0, 1.1, 0.7)))

    dev = join(parts, "Dev")
    # Bevel leve suaviza as caixas remanescentes (pes, nariz) e quinas.
    bev = dev.modifiers.new("Bevel", 'BEVEL')
    bev.width = 0.008
    bev.segments = 2
    bev.limit_method = 'ANGLE'
    bev.angle_limit = math.radians(40)
    origin_base(dev)
    return dev


def export(dev):
    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, "Dev.fbx")
    for o in bpy.context.selected_objects:
        o.select_set(False)
    dev.select_set(True)
    bpy.context.view_layer.objects.active = dev
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_ALL', bake_space_transform=True,
        object_types={'MESH'}, mesh_smooth_type='FACE', use_mesh_modifiers=True,
        add_leaf_bones=False, axis_forward='-Z', axis_up='Y', path_mode='COPY')
    print("EXPORTED", path)


def render():
    scene = bpy.context.scene
    if scene.world is None:
        scene.world = bpy.data.worlds.new("W")
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes.get("Background")
    if bg:
        bg.inputs[0].default_value = (0.11, 0.13, 0.16, 1)
    tgt = bpy.data.objects.new("T", None)
    tgt.location = (0, -0.15, 0.80)
    scene.collection.objects.link(tgt)
    cam_d = bpy.data.cameras.new("C")
    cam = bpy.data.objects.new("C", cam_d)
    cam.location = (0.85, -1.30, 1.05)
    scene.collection.objects.link(cam)
    con = cam.constraints.new('TRACK_TO')
    con.target = tgt
    con.track_axis = 'TRACK_NEGATIVE_Z'
    con.up_axis = 'UP_Y'
    scene.camera = cam
    sun_d = bpy.data.lights.new("S", 'SUN')
    sun_d.energy = 3.5
    sun = bpy.data.objects.new("S", sun_d)
    sun.rotation_euler = (math.radians(55), math.radians(10), math.radians(-40))
    scene.collection.objects.link(sun)
    for eng in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE', 'CYCLES'):
        try:
            scene.render.engine = eng
            break
        except Exception:
            continue
    scene.render.resolution_x = 800
    scene.render.resolution_y = 950
    scene.render.filepath = PREVIEW
    try:
        bpy.ops.render.render(write_still=True)
        print("RENDERED", PREVIEW)
    except Exception as ex:
        print("RENDER_FAILED", repr(ex))


def main():
    try:
        bpy.ops.preferences.addon_enable(module="io_scene_fbx")
    except Exception:
        pass
    reset()
    dev = build()
    export(dev)
    render()
    print("=== DONE ===")


if __name__ == "__main__":
    main()
