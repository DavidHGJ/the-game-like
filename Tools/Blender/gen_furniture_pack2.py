"""
Pacote 2 de mobiliario low-poly para The Game Like (2026-11-12).

Roda headless no Blender:
    blender --background --python Tools/Blender/gen_furniture_pack2.py

Reaproveita a infraestrutura, a paleta e as convencoes de gen_furniture.py (pivo na base,
frente = -Y, materiais *_Fixed / *_Tintable, metros). Exporta um .fbx por peca em
Assets/Art/Furniture/<Peca>/ e renderiza o catalogo em Tools/Blender/furniture_pack2_preview.png.

Tres familias:
  - pecas pequenas que vao EM CIMA de mesas/armarios (laptop, livros, caneca, cacto...);
  - moveis de chao (arquivo, mesa redonda, bancada de garagem, bicicleta, pingue-pongue...);
  - pecas de parede (mural de cortica, TV, neon, ar-condicionado).

Nomes de material com "Screen" acendem como tela; com "Led" ficam acesos na propria cor
(ver OfficeBuilder.Polished). Evitar essas silabas em materiais que nao brilham.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_furniture as g  # noqa: E402

box, cylinder, cone, sphere, join = g.box, g.cylinder, g.cone, g.sphere, g.join
mat, C = g.make_material, g.C

PREVIEW = os.path.join(os.path.dirname(os.path.abspath(__file__)), "furniture_pack2_preview.png")


# --------------------------------------------------------------------------
# Em cima de moveis
# --------------------------------------------------------------------------

def build_laptop():
    shell = mat("LaptopShell_Fixed", C["metal_lt"], metallic=0.4, roughness=0.4)
    keys = mat("LaptopKeys_Fixed", C["dark"], roughness=0.5)
    screen = mat("LaptopScreen_Tintable", C["screen"], roughness=0.2)
    parts = [
        box("Base", (0.34, 0.24, 0.02), (0, 0, 0.01), shell),
        box("Keys", (0.28, 0.13, 0.004), (0, -0.02, 0.022), keys),
        box("Lid", (0.34, 0.02, 0.23), (0, 0.135, 0.125), shell, rot=(-12, 0, 0)),
        box("Display", (0.30, 0.008, 0.19), (0, 0.122, 0.127), screen, rot=(-12, 0, 0)),
    ]
    return join(parts, "Laptop")


def build_books():
    parts = [
        box("BookA", (0.25, 0.18, 0.045), (0, 0, 0.0225), mat("BookA_Tintable", C["coral"], roughness=0.6)),
        box("BookB", (0.23, 0.17, 0.04), (0.01, 0, 0.065), mat("BookB_Fixed", C["teal"], roughness=0.6), rot=(0, 0, 12)),
        box("BookC", (0.20, 0.15, 0.035), (-0.01, 0.005, 0.1025), mat("BookC_Fixed", C["mustard"], roughness=0.6), rot=(0, 0, -9)),
        box("Pages", (0.21, 0.155, 0.03), (0.012, 0, 0.065), mat("BookPages_Fixed", C["white"], roughness=0.7), rot=(0, 0, 12)),
    ]
    return join(parts, "Books")


def build_mug():
    body = mat("Mug_Tintable", C["white"], roughness=0.4)
    parts = [
        cylinder("Cup", 0.045, 0.09, (0, 0, 0.045), body, verts=14),
        box("Handle", (0.022, 0.05, 0.05), (0.058, 0, 0.047), body),
        cylinder("Coffee", 0.037, 0.006, (0, 0, 0.09), mat("MugCoffee_Fixed", C["wood_dk"], roughness=0.3), verts=14),
    ]
    return join(parts, "Mug")


def build_cactus():
    green = mat("Cactus_Fixed", C["leaf"], roughness=0.6)
    parts = [
        cone("Pot", 0.065, 0.09, 0.10, (0, 0, 0.05), mat("CactusPot_Tintable", C["terra"], roughness=0.7), verts=12),
        cylinder("Body", 0.05, 0.17, (0, 0, 0.185), green, verts=10),
        sphere("Top", 0.05, (0, 0, 0.27), green, segs=10, rings=6),
        cylinder("Arm", 0.024, 0.07, (0.07, 0, 0.19), green, verts=8, rot=(0, 90, 0)),
        cylinder("ArmUp", 0.024, 0.08, (0.10, 0, 0.225), green, verts=8),
        sphere("Flower", 0.022, (0, 0, 0.325), mat("CactusFlower_Fixed", C["pink"], roughness=0.5), segs=8, rings=5),
    ]
    return join(parts, "Cactus")


def build_lava_lamp():
    metal = mat("LavaMetal_Fixed", C["metal_lt"], metallic=0.6, roughness=0.35)
    parts = [
        cone("Base", 0.065, 0.04, 0.09, (0, 0, 0.045), metal, verts=14),
        cylinder("Glass", 0.045, 0.20, (0, 0, 0.19), mat("LavaLed_Tintable", C["coral"], roughness=0.2), verts=14),
        cone("Cap", 0.045, 0.025, 0.06, (0, 0, 0.32), metal, verts=14),
    ]
    return join(parts, "LavaLamp")


def build_trophy():
    gold = mat("Trophy_Fixed", C["mustard"], metallic=0.7, roughness=0.3)
    parts = [
        box("Base", (0.11, 0.11, 0.035), (0, 0, 0.0175), mat("TrophyBase_Fixed", C["wood_dk"], roughness=0.6)),
        cylinder("Stem", 0.016, 0.07, (0, 0, 0.07), gold, verts=8),
        cone("Cup", 0.03, 0.065, 0.10, (0, 0, 0.155), gold, verts=14),
        box("HandleL", (0.02, 0.02, 0.06), (-0.075, 0, 0.165), gold),
        box("HandleR", (0.02, 0.02, 0.06), (0.075, 0, 0.165), gold),
    ]
    return join(parts, "Trophy")


def build_printer():
    white = mat("Printer_Fixed", C["white"], roughness=0.5)
    dark = mat("PrinterDark_Fixed", C["dark"], roughness=0.5)
    parts = [
        box("Body", (0.46, 0.38, 0.20), (0, 0, 0.10), white),
        box("Lid", (0.42, 0.30, 0.03), (0, 0.02, 0.215), dark),
        box("FeedTray", (0.32, 0.03, 0.16), (0, 0.17, 0.27), dark, rot=(20, 0, 0)),
        box("OutTray", (0.30, 0.16, 0.015), (0, -0.25, 0.055), dark),
        box("Paper", (0.22, 0.14, 0.006), (0, -0.24, 0.066), white),
        box("Status", (0.09, 0.012, 0.04), (-0.13, -0.191, 0.15), mat("PrinterScreen_Fixed", C["screen"], roughness=0.2)),
    ]
    return join(parts, "Printer")


def build_desk_fan():
    metal = mat("Fan_Fixed", C["metal_lt"], metallic=0.5, roughness=0.4)
    dark = mat("FanDark_Fixed", C["dark"], roughness=0.5)
    blade = mat("FanBlade_Tintable", C["teal"], roughness=0.4)
    parts = [
        cylinder("Base", 0.085, 0.025, (0, 0, 0.0125), dark, verts=16),
        cylinder("Pole", 0.014, 0.15, (0, 0, 0.10), metal, verts=8),
        cylinder("Cage", 0.12, 0.05, (0, 0, 0.27), metal, verts=18, rot=(90, 0, 0)),
        cylinder("Blades", 0.10, 0.055, (0, 0, 0.27), blade, verts=6, rot=(90, 0, 0)),
        cylinder("Hub", 0.03, 0.07, (0, 0, 0.27), dark, verts=10, rot=(90, 0, 0)),
    ]
    return join(parts, "DeskFan")


def build_game_console():
    white = mat("Console_Fixed", C["white"], roughness=0.4)
    dark = mat("ConsoleDark_Fixed", C["dark"], roughness=0.5)
    parts = [
        box("Body", (0.32, 0.24, 0.055), (0, 0.04, 0.0275), white),
        box("Stripe", (0.32, 0.03, 0.058), (0, 0.04, 0.029), dark),
        box("Power", (0.03, 0.012, 0.012), (-0.12, -0.081, 0.03), mat("ConsoleLed_Fixed", C["teal"], roughness=0.3)),
        box("Pad", (0.15, 0.07, 0.03), (0.04, -0.19, 0.015), dark),
        box("GripL", (0.045, 0.09, 0.035), (-0.03, -0.21, 0.0175), dark, rot=(0, 0, 14)),
        box("GripR", (0.045, 0.09, 0.035), (0.11, -0.21, 0.0175), dark, rot=(0, 0, -14)),
    ]
    return join(parts, "GameConsole")


def build_speaker():
    dark = mat("Speaker_Fixed", C["dark"], roughness=0.5)
    cone_m = mat("SpeakerCone_Fixed", C["metal_lt"], metallic=0.4, roughness=0.4)
    parts = [
        box("Box", (0.18, 0.18, 0.28), (0, 0, 0.14), dark),
        cylinder("Woofer", 0.06, 0.012, (0, -0.092, 0.10), cone_m, verts=16, rot=(90, 0, 0)),
        cylinder("Tweeter", 0.03, 0.012, (0, -0.092, 0.21), cone_m, verts=12, rot=(90, 0, 0)),
    ]
    return join(parts, "Speaker")


# --------------------------------------------------------------------------
# Chao
# --------------------------------------------------------------------------

def build_filing_cabinet():
    body = mat("Filing_Tintable", C["metal_lt"], metallic=0.3, roughness=0.5)
    dark = mat("FilingHandle_Fixed", C["metal"], metallic=0.5, roughness=0.4)
    parts = [box("Body", (0.50, 0.60, 1.00), (0, 0, 0.50), body)]
    for z in (0.18, 0.50, 0.82):
        parts.append(box("Drawer", (0.44, 0.02, 0.28), (0, -0.305, z), body))
        parts.append(box("Handle", (0.16, 0.03, 0.025), (0, -0.325, z + 0.06), dark))
    return join(parts, "FilingCabinet")


def build_trash_bin():
    parts = [
        cone("Bin", 0.12, 0.16, 0.32, (0, 0, 0.16), mat("Bin_Fixed", C["metal"], metallic=0.4, roughness=0.5), verts=14),
        sphere("PaperA", 0.06, (0.03, 0.02, 0.31), mat("BinPaper_Fixed", C["white"], roughness=0.8), segs=7, rings=5),
        sphere("PaperB", 0.05, (-0.05, -0.03, 0.30), mat("BinPaper_Fixed", C["white"], roughness=0.8), segs=7, rings=5),
    ]
    return join(parts, "TrashBin")


def build_round_table():
    metal = mat("RoundTableMetal_Fixed", C["metal"], metallic=0.5, roughness=0.4)
    parts = [
        cylinder("Top", 0.82, 0.06, (0, 0, 0.73), mat("RoundTableTop_Fixed", C["wood_lt"], roughness=0.5), verts=28),
        cylinder("Pedestal", 0.09, 0.68, (0, 0, 0.36), metal, verts=12),
        cylinder("Foot", 0.42, 0.04, (0, 0, 0.02), metal, verts=20),
    ]
    return join(parts, "RoundTable")


def build_standing_desk():
    metal = mat("StandMetal_Fixed", C["metal"], metallic=0.5, roughness=0.4)
    parts = [box("Top", (1.90, 0.90, 0.05), (0, 0, 1.05), mat("StandTop_Fixed", C["white"], roughness=0.5))]
    for sx in (-1, 1):
        parts.append(box("Leg", (0.09, 0.12, 1.00), (sx * 0.80, 0, 0.52), metal))
        parts.append(box("Foot", (0.09, 0.80, 0.04), (sx * 0.80, 0, 0.02), metal))
    parts.append(box("Beam", (1.60, 0.06, 0.06), (0, 0, 0.96), metal))
    return join(parts, "StandingDesk")


def build_partition():
    metal = mat("PartitionMetal_Fixed", C["metal_lt"], metallic=0.4, roughness=0.4)
    parts = [
        box("Board", (0.98, 0.06, 1.36), (0, 0, 0.74), mat("Partition_Tintable", C["sage"], roughness=0.8)),
        box("Rail", (1.00, 0.08, 0.04), (0, 0, 1.44), metal),
        box("FootL", (0.06, 0.36, 0.05), (-0.42, 0, 0.025), metal),
        box("FootR", (0.06, 0.36, 0.05), (0.42, 0, 0.025), metal),
    ]
    return join(parts, "Partition")


def build_vending_machine():
    dark = mat("VendingDark_Fixed", C["dark"], roughness=0.5)
    parts = [
        box("Body", (0.86, 0.76, 1.86), (0, 0, 0.93), mat("Vending_Tintable", C["coral"], roughness=0.5)),
        box("Glass", (0.50, 0.02, 1.04), (-0.13, -0.385, 1.18), mat("VendingScreen_Fixed", C["screen"], roughness=0.2)),
        box("Keypad", (0.16, 0.02, 0.46), (0.29, -0.385, 1.25), dark),
        box("Slot", (0.50, 0.02, 0.16), (-0.13, -0.385, 0.36), dark),
        box("Coin", (0.06, 0.025, 0.10), (0.29, -0.39, 0.86), mat("VendingMetal_Fixed", C["metal_lt"], metallic=0.5, roughness=0.4)),
    ]
    return join(parts, "VendingMachine")


def build_tv_stand():
    dark = mat("TvDark_Fixed", C["dark"], roughness=0.45)
    wood = mat("TvStandWood_Fixed", C["wood_dk"], roughness=0.6)
    parts = [
        box("Cabinet", (1.60, 0.42, 0.44), (0, 0, 0.22), wood),
        box("DoorL", (0.74, 0.02, 0.34), (-0.39, -0.215, 0.22), mat("TvStandDoor_Tintable", C["wood"], roughness=0.6)),
        box("DoorR", (0.74, 0.02, 0.34), (0.39, -0.215, 0.22), mat("TvStandDoor_Tintable", C["wood"], roughness=0.6)),
        box("Foot", (0.44, 0.20, 0.03), (0, 0.02, 0.455), dark),
        box("Neck", (0.08, 0.05, 0.12), (0, 0.03, 0.52), dark),
        box("Bezel", (1.30, 0.05, 0.76), (0, 0.03, 0.95), dark),
        box("Display", (1.22, 0.02, 0.68), (0, 0.0, 0.95), mat("TvScreen_Fixed", C["screen"], roughness=0.2)),
    ]
    return join(parts, "TvStand")


def build_workbench():
    wood = mat("BenchWood_Fixed", C["wood"], roughness=0.7)
    metal = mat("BenchMetal_Fixed", C["metal"], metallic=0.5, roughness=0.5)
    red = mat("BenchTool_Tintable", C["coral"], roughness=0.5)
    parts = [
        box("Top", (1.90, 0.86, 0.08), (0, 0, 0.88), wood),
        box("Shelf", (1.70, 0.70, 0.04), (0, 0, 0.26), mat("BenchShelf_Fixed", C["wood_dk"], roughness=0.7)),
        box("Peg", (1.90, 0.04, 0.80), (0, 0.41, 1.32), mat("BenchPeg_Fixed", C["wood_lt"], roughness=0.8)),
        box("HammerGrip", (0.035, 0.03, 0.30), (-0.45, 0.38, 1.30), wood),
        box("HammerHead", (0.14, 0.04, 0.06), (-0.45, 0.38, 1.46), metal),
        box("Wrench", (0.05, 0.02, 0.34), (-0.20, 0.38, 1.32), metal, rot=(0, 18, 0)),
        box("Saw", (0.34, 0.015, 0.12), (0.30, 0.38, 1.38), metal),
        box("SawGrip", (0.10, 0.03, 0.14), (0.50, 0.38, 1.38), red),
            ]
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(box("Leg", (0.08, 0.08, 0.84), (sx * 0.88, sy * 0.36, 0.42), metal))
    return join(parts, "Workbench")


def build_bicycle():
    tire = mat("BikeTire_Fixed", C["black"], roughness=0.7)
    rim = mat("BikeRim_Fixed", C["metal_lt"], metallic=0.5, roughness=0.4)
    frame = mat("Bike_Tintable", C["coral"], roughness=0.4)
    dark = mat("BikeDark_Fixed", C["dark"], roughness=0.5)
    parts = []
    for sx in (-0.52, 0.52):
        parts.append(cylinder("Tire", 0.34, 0.045, (sx, 0, 0.34), tire, verts=20, rot=(90, 0, 0)))
        parts.append(cylinder("Rim", 0.27, 0.05, (sx, 0, 0.34), rim, verts=20, rot=(90, 0, 0)))
        parts.append(cylinder("Hub", 0.05, 0.08, (sx, 0, 0.34), dark, verts=10, rot=(90, 0, 0)))
    parts += [
        box("SeatTube", (0.04, 0.04, 0.50), (-0.14, 0, 0.60), frame, rot=(0, -10, 0)),
        box("TopTube", (0.56, 0.04, 0.04), (0.12, 0, 0.80), frame),
        box("DownTube", (0.60, 0.04, 0.04), (0.14, 0, 0.58), frame, rot=(0, 38, 0)),
        box("ChainStay", (0.42, 0.035, 0.035), (-0.32, 0, 0.35), frame),
        box("SeatStay", (0.50, 0.035, 0.035), (-0.33, 0, 0.58), frame, rot=(0, -48, 0)),
        box("Fork", (0.04, 0.04, 0.60), (0.46, 0, 0.62), frame, rot=(0, 12, 0)),
        box("Saddle", (0.22, 0.10, 0.04), (-0.19, 0, 0.88), dark),
        box("Bar", (0.04, 0.44, 0.04), (0.40, 0, 0.94), dark),
        cylinder("Crank", 0.07, 0.03, (-0.08, 0, 0.36), rim, verts=12, rot=(90, 0, 0)),
        box("Stand", (0.03, 0.22, 0.03), (-0.20, 0.10, 0.14), dark, rot=(35, 0, 0)),
    ]
    return join(parts, "Bicycle")


def build_coffee_station():
    dark = mat("CoffeeMachine_Fixed", C["dark"], roughness=0.4)
    metal = mat("CoffeeMetal_Fixed", C["metal_lt"], metallic=0.6, roughness=0.3)
    white = mat("CoffeeCup_Fixed", C["white"], roughness=0.4)
    parts = [
        box("Counter", (0.90, 0.55, 0.84), (0, 0, 0.42), mat("CoffeeCounter_Tintable", C["wood_lt"], roughness=0.6)),
        box("Worktop", (0.94, 0.59, 0.04), (0, 0, 0.86), mat("CoffeeTop_Fixed", C["white"], roughness=0.4)),
        box("DoorL", (0.40, 0.02, 0.66), (-0.22, -0.28, 0.42), mat("CoffeeDoor_Fixed", C["wood"], roughness=0.6)),
        box("DoorR", (0.40, 0.02, 0.66), (0.22, -0.28, 0.42), mat("CoffeeDoor_Fixed", C["wood"], roughness=0.6)),
        box("Machine", (0.30, 0.32, 0.36), (-0.22, 0.06, 1.06), dark),
        box("Spout", (0.10, 0.10, 0.05), (-0.22, -0.13, 1.07), metal),
        box("Drip", (0.24, 0.16, 0.02), (-0.22, -0.16, 0.89), metal),
        cylinder("Cup", 0.035, 0.07, (-0.22, -0.16, 0.935), white, verts=12),
        cylinder("MugA", 0.04, 0.08, (0.20, 0.05, 0.92), white, verts=12),
        cylinder("MugB", 0.04, 0.08, (0.31, -0.06, 0.92), mat("CoffeeMug_Fixed", C["teal"], roughness=0.4), verts=12),
        cylinder("Jar", 0.06, 0.16, (0.26, 0.17, 0.96), mat("CoffeeJar_Fixed", C["wood_dk"], roughness=0.4), verts=12),
    ]
    return join(parts, "CoffeeStation")


def build_metal_shelf():
    metal = mat("ShelfMetal_Fixed", C["metal_lt"], metallic=0.5, roughness=0.45)
    parts = []
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(box("Post", (0.04, 0.04, 1.80), (sx * 0.43, sy * 0.19, 0.90), metal))
    for z in (0.15, 0.70, 1.25, 1.78):
        parts.append(box("Board", (0.90, 0.42, 0.03), (0, 0, z), metal))
    parts += [
        cylinder("CanA", 0.08, 0.18, (-0.28, 0, 0.255), mat("ShelfCanA_Fixed", C["coral"], roughness=0.5), verts=12),
        cylinder("CanB", 0.08, 0.18, (-0.08, 0.02, 0.255), mat("ShelfCanB_Fixed", C["blue"], roughness=0.5), verts=12),
        cylinder("CanC", 0.07, 0.14, (0.26, 0, 0.785), mat("ShelfCanC_Fixed", C["mustard"], roughness=0.5), verts=12),
        box("Crate", (0.34, 0.30, 0.22), (-0.20, 0, 0.825), mat("ShelfCrate_Fixed", C["wood"], roughness=0.8)),
        box("Carton", (0.36, 0.32, 0.26), (0.20, 0, 1.395), mat("ShelfCarton_Fixed", C["wood_lt"], roughness=0.8)),
        box("Case", (0.30, 0.20, 0.14), (-0.24, 0, 1.335), mat("ShelfCase_Tintable", C["teal"], roughness=0.5)),
    ]
    return join(parts, "MetalShelf")


def build_ping_pong():
    metal = mat("PongMetal_Fixed", C["metal"], metallic=0.5, roughness=0.4)
    white = mat("PongLine_Fixed", C["white"], roughness=0.5)
    parts = [
        box("Top", (2.70, 1.50, 0.05), (0, 0, 0.76), mat("Pong_Tintable", C["teal_dk"], roughness=0.5)),
        box("Line", (2.70, 0.025, 0.052), (0, 0, 0.76), white),
        box("Net", (0.02, 1.60, 0.15), (0, 0, 0.86), white),
        box("PaddleFace", (0.15, 0.16, 0.012), (0.70, -0.30, 0.792), mat("PongPaddle_Fixed", C["coral"], roughness=0.5)),
        box("PaddleGrip", (0.10, 0.03, 0.02), (0.82, -0.30, 0.795), mat("PongGrip_Fixed", C["wood"], roughness=0.6)),
        sphere("Ball", 0.02, (-0.50, 0.25, 0.805), white, segs=8, rings=5),
    ]
    for sx in (-1, 1):
        for sy in (-1, 1):
            parts.append(box("Leg", (0.06, 0.06, 0.74), (sx * 1.15, sy * 0.60, 0.37), metal))
    return join(parts, "PingPong")


def build_stool():
    metal = mat("StoolMetal_Fixed", C["metal"], metallic=0.5, roughness=0.4)
    parts = [
        cylinder("Seat", 0.18, 0.06, (0, 0, 0.62), mat("Stool_Tintable", C["mustard"], roughness=0.6), verts=16),
        cylinder("Pole", 0.028, 0.57, (0, 0, 0.305), metal, verts=8),
        cylinder("Ring", 0.14, 0.02, (0, 0, 0.26), metal, verts=14),
        cylinder("Foot", 0.21, 0.03, (0, 0, 0.015), metal, verts=16),
    ]
    return join(parts, "Stool")


# --------------------------------------------------------------------------
# Parede (fina em Y, frente = -Y, pivo na base)
# --------------------------------------------------------------------------

def build_corkboard():
    parts = [
        box("Frame", (1.00, 0.04, 0.70), (0, 0, 0.35), mat("CorkFrame_Fixed", C["wood_dk"], roughness=0.6)),
        box("Cork", (0.92, 0.02, 0.62), (0, -0.02, 0.35), mat("Cork_Fixed", C["wood_lt"], roughness=0.9)),
    ]
    notes = [(-0.28, 0.48, "mustard", 6), (0.05, 0.50, "pink", -8), (0.30, 0.42, "white", 4),
             (-0.20, 0.20, "blue", -5), (0.18, 0.18, "sage", 9)]
    for i, (x, z, color, tilt) in enumerate(notes):
        parts.append(box("Note", (0.15, 0.012, 0.15), (x, -0.036, z),
                         mat("CorkNote%d_Fixed" % i, C[color], roughness=0.7), rot=(0, tilt, 0)))
    return join(parts, "Corkboard")


def build_wall_tv():
    parts = [
        box("Bezel", (1.40, 0.05, 0.80), (0, 0, 0.40), mat("WallTvDark_Fixed", C["dark"], roughness=0.45)),
        box("Display", (1.32, 0.02, 0.72), (0, -0.03, 0.40), mat("WallTvScreen_Fixed", C["screen"], roughness=0.2)),
    ]
    return join(parts, "WallTv")


def build_neon_sign():
    pink = mat("NeonLedA_Tintable", C["coral"], roughness=0.2)
    teal = mat("NeonLedB_Fixed", C["teal"], roughness=0.2)
    parts = [
        box("Back", (1.20, 0.03, 0.56), (0, 0, 0.28), mat("NeonBack_Fixed", C["black"], roughness=0.6)),
        # raio (3 traços) + moldura
        box("BoltA", (0.05, 0.03, 0.26), (-0.04, -0.03, 0.39), pink, rot=(0, -28, 0)),
        box("BoltB", (0.22, 0.03, 0.05), (0.0, -0.03, 0.28), pink),
        box("BoltC", (0.05, 0.03, 0.26), (0.04, -0.03, 0.17), pink, rot=(0, -28, 0)),
        box("Top", (1.04, 0.03, 0.04), (0, -0.03, 0.50), teal),
        box("Bottom", (1.04, 0.03, 0.04), (0, -0.03, 0.06), teal),
        box("Left", (0.04, 0.03, 0.44), (-0.52, -0.03, 0.28), teal),
        box("Right", (0.04, 0.03, 0.44), (0.52, -0.03, 0.28), teal),
    ]
    return join(parts, "NeonSign")


def build_ac_unit():
    parts = [
        box("Body", (0.95, 0.24, 0.30), (0, 0, 0.15), mat("Ac_Fixed", C["white"], roughness=0.4)),
        box("Vent", (0.85, 0.02, 0.06), (0, -0.121, 0.06), mat("AcVent_Fixed", C["dark"], roughness=0.5)),
        box("Grille", (0.85, 0.02, 0.02), (0, -0.121, 0.20), mat("AcGrille_Fixed", C["metal_lt"], roughness=0.5)),
        box("Light", (0.03, 0.012, 0.012), (0.40, -0.122, 0.25), mat("AcLed_Fixed", C["leaf"], roughness=0.3)),
    ]
    return join(parts, "AcUnit")


PIECES = {
    "Laptop": build_laptop, "Books": build_books, "Mug": build_mug, "Cactus": build_cactus,
    "LavaLamp": build_lava_lamp, "Trophy": build_trophy, "Printer": build_printer,
    "DeskFan": build_desk_fan, "GameConsole": build_game_console, "Speaker": build_speaker,
    "FilingCabinet": build_filing_cabinet, "TrashBin": build_trash_bin, "RoundTable": build_round_table,
    "StandingDesk": build_standing_desk, "Partition": build_partition, "VendingMachine": build_vending_machine,
    "TvStand": build_tv_stand, "Workbench": build_workbench, "Bicycle": build_bicycle,
    "CoffeeStation": build_coffee_station, "MetalShelf": build_metal_shelf, "PingPong": build_ping_pong,
    "Stool": build_stool,
    "Corkboard": build_corkboard, "WallTv": build_wall_tv, "NeonSign": build_neon_sign, "AcUnit": build_ac_unit,
}


def main():
    g.ensure_fbx_exporter()
    os.makedirs(g.OUT_DIR, exist_ok=True)
    results = []
    for name, fn in PIECES.items():
        try:
            g.build_and_export(name, fn)
            results.append((name, True))
        except Exception as exc:  # noqa: BLE001
            print("FAILED", name, repr(exc))
            results.append((name, False))
    g.PIECES = PIECES
    g.PREVIEW = PREVIEW
    g.render_preview()
    print("=== SUMMARY ===")
    for name, ok in results:
        print(("OK  " if ok else "ERR ") + name)


if __name__ == "__main__":
    main()
