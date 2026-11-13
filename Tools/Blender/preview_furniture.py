"""
Renderiza uma previa das pecas de mobiliario geradas, numa camera ortografica
isometrica (mesma direcao de ART_STYLE.md), para inspecao rapida sem abrir o Unity.

    blender --background --python Tools/Blender/preview_furniture.py
"""

import math
import os

import bpy
from mathutils import Vector

ROOT = r"C:\Users\david\OneDrive\Documentos\Unity\TheGameLike\My project"
FURN = os.path.join(ROOT, "Assets", "Art", "Furniture")
OUT_IMG = os.path.join(ROOT, "Tools", "Blender", "furniture_preview.png")

PIECES = ["Desk", "OfficeChair", "Monitor", "Plant", "CardboardBox", "Poster", "Rug"]


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_fbx(name, location):
    path = os.path.join(FURN, name, name + ".fbx")
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new = [o for o in bpy.data.objects if o not in before and o.type == 'MESH']
    for o in new:
        o.location = o.location + Vector(location)
    return new


def main():
    try:
        bpy.ops.preferences.addon_enable(module="io_scene_fbx")
    except Exception:
        pass
    reset()

    # Chao
    bpy.ops.mesh.primitive_plane_add(size=12, location=(0, 0, 0))
    floor = bpy.context.active_object
    fmat = bpy.data.materials.new("Floor")
    fmat.use_nodes = True
    fmat.node_tree.nodes.get("Principled BSDF").inputs["Base Color"].default_value = (0.45, 0.52, 0.49, 1)
    floor.data.materials.append(fmat)

    spots = [(-5.4, 0, 0), (-3.6, 0, 0), (-1.8, 0, 0), (0.0, 0, 0),
             (1.9, 0, 0), (3.7, 0, 0), (5.6, 0, 0)]
    for name, loc in zip(PIECES, spots):
        import_fbx(name, loc)

    # Luz direcional
    light_data = bpy.data.lights.new("Sun", type='SUN')
    light_data.energy = 3.0
    light = bpy.data.objects.new("Sun", light_data)
    bpy.context.collection.objects.link(light)
    light.rotation_euler = (math.radians(50), 0, math.radians(-40))

    # Camera ortografica isometrica
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = 'ORTHO'
    cam_data.ortho_scale = 15.0
    cam = bpy.data.objects.new("Cam", cam_data)
    bpy.context.collection.objects.link(cam)
    cam.location = (0.0, -6.0, 5.0)
    cam.rotation_euler = (math.radians(55), 0, 0)
    bpy.context.scene.camera = cam

    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 640
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new("W")
    scene.world.use_nodes = True
    scene.world.node_tree.nodes["Background"].inputs[0].default_value = (0.12, 0.13, 0.17, 1)
    scene.render.filepath = OUT_IMG
    bpy.ops.render.render(write_still=True)
    print("RENDERED", OUT_IMG)


if __name__ == "__main__":
    main()
