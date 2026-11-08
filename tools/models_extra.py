"""Extra Pocket Barista models: new stations, products, money, customers, staff, decor."""
import math
from lowpoly import Mesh, chamfer_rect, circle
from models import _humanoid, coffee_cup


# ---------------- Stations (sit on counter top, front faces +Z) ----------------

def oven():
    m = Mesh("oven")
    m.prism("steel_dark", chamfer_rect(0.60, 0.48, 0.03), 0, 0.46)
    m.box("black", 0, 0.08, 0.235, 0.46, 0.28, 0.02)
    m.box("oven_glow", 0, 0.11, 0.247, 0.38, 0.20, 0.004)
    m.box("steel", 0, 0.38, 0.25, 0.40, 0.025, 0.03)
    for x in (-0.20, -0.08, 0.04):
        m.cyl("black", x, 0.40, 0.24, 0.022, 0.022, 0.03, seg=6)
    m.box("amber", 0.2, 0.41, 0.245, 0.06, 0.03, 0.004)
    m.add(croissant(), -0.12, 0.46, 0.0, scale=0.9)
    m.add(croissant(), 0.12, 0.46, 0.02, rot_y_deg=40, scale=0.9)
    return m


def tea_station():
    m = Mesh("tea_station")
    m.prism("wood_dark", chamfer_rect(0.62, 0.40, 0.03), 0, 0.05)
    # Kettle
    m.cyl("steel", -0.15, 0.05, 0, 0.10, 0.08, 0.20, seg=10)
    m.cyl("steel", -0.15, 0.25, 0, 0.08, 0.03, 0.05, seg=10)
    m.cyl("black", -0.15, 0.30, 0, 0.02, 0.02, 0.03, seg=6)
    m.box("steel", -0.03, 0.14, 0, 0.12, 0.025, 0.025)
    # Teapot + cups
    m.sphere("white", 0.14, 0.13, -0.05, 0.08, 0.07, 0.08, seg=10, rings=5)
    m.box("white", 0.24, 0.13, -0.05, 0.08, 0.02, 0.02)
    m.cyl("tea", 0.14, 0.19, -0.05, 0.02, 0.02, 0.02, seg=6)
    m.add(tea_cup(), 0.10, 0.05, 0.12)
    m.add(tea_cup(), 0.24, 0.05, 0.10)
    # Tea tins
    for i, mat in enumerate(("book_red", "book_green", "book_yellow")):
        m.cyl(mat, -0.25 + i * 0.07, 0.05, -0.14, 0.03, 0.03, 0.10, seg=8)
    return m


def blender_station():
    m = Mesh("blender_station")
    m.prism("white", chamfer_rect(0.50, 0.36, 0.03), 0, 0.05)
    for x in (-0.12, 0.12):
        m.prism("black", chamfer_rect(0.14, 0.14, 0.02), 0.05, 0.10, cx=x)
        m.cyl("glass", x, 0.15, 0, 0.05, 0.065, 0.22, seg=8, cap_top=False)
        m.cyl("smoothie" if x < 0 else "lettuce", x, 0.15, 0, 0.05, 0.06, 0.12, seg=8)
        m.cyl("black", x, 0.37, 0, 0.068, 0.068, 0.03, seg=8)
        m.box("amber", x, 0.10, 0.071, 0.04, 0.02, 0.004)
    m.add(smoothie_cup(), 0.0, 0.05, 0.12, scale=0.8)
    return m


# ---------------- Products (served on tables) ----------------

def tea_cup():
    m = Mesh("tea_cup")
    m.cyl("white", 0, 0, 0, 0.075, 0.08, 0.01, seg=12)
    m.cyl("glass", 0, 0.01, 0, 0.03, 0.04, 0.07, seg=10, top="tea")
    return m


def donut():
    m = Mesh("donut")
    m.cyl("white", 0, 0, 0, 0.10, 0.10, 0.01, seg=12)
    seg = 12
    for i in range(seg):
        a = 2 * math.pi * i / seg
        m.sphere("pastry", 0.055 * math.cos(a), 0.035, 0.055 * math.sin(a), 0.032, 0.025, 0.032, seg=6, rings=3)
        m.sphere("frosting", 0.055 * math.cos(a), 0.045, 0.055 * math.sin(a), 0.030, 0.02, 0.030, seg=6, rings=3)
    for i in range(6):
        a = 2 * math.pi * i / 6 + 0.3
        m.box("sprinkle" if i % 2 else "hawaii_dot", 0.055 * math.cos(a), 0.064, 0.055 * math.sin(a), 0.012, 0.005, 0.005)
    return m


def cupcake():
    m = Mesh("cupcake")
    m.cyl("paper", 0, 0, 0, 0.035, 0.05, 0.05, seg=10)
    m.cyl("frosting", 0, 0.05, 0, 0.055, 0.04, 0.03, seg=10)
    m.cyl("frosting", 0, 0.08, 0, 0.04, 0.0, 0.05, seg=10)
    m.sphere("berry", 0, 0.135, 0, 0.015, seg=6, rings=4)
    return m


def sandwich():
    m = Mesh("sandwich")
    m.cyl("white", 0, 0, 0, 0.12, 0.12, 0.01, seg=12)
    tri = [(-0.08, -0.06), (0.08, -0.06), (0.0, 0.08)]
    m.prism("bread", tri, 0.01, 0.02)
    m.prism("lettuce", [(x * 1.08, z * 1.08) for x, z in tri], 0.03, 0.01)
    m.prism("tomato", tri, 0.04, 0.008)
    m.prism("cheese", [(x * 1.04, z * 1.04) for x, z in tri], 0.048, 0.006)
    m.prism("bread", tri, 0.054, 0.02, top_scale=0.95)
    return m


def smoothie_cup():
    m = Mesh("smoothie_cup")
    m.cyl("glass", 0, 0, 0, 0.035, 0.05, 0.15, seg=10, cap_top=False)
    m.cyl("smoothie", 0, 0.005, 0, 0.033, 0.046, 0.13, seg=10)
    m.cyl("white", 0, 0.15, 0, 0.052, 0.052, 0.008, seg=10)
    m.box("neon_pink", 0.015, 0.15, 0, 0.008, 0.10, 0.008)
    return m


def croissant():
    m = Mesh("croissant_s")
    for i, (x, s) in enumerate(((-0.07, 0.6), (-0.035, 0.85), (0, 1.0), (0.035, 0.85), (0.07, 0.6))):
        m.sphere("pastry", x, 0.03 * s, abs(x) * 0.5, 0.03 * s, 0.03 * s, 0.035 * s, seg=6, rings=4)
    return m


# ---------------- Money ----------------

def money_pile():
    m = Mesh("money_pile")
    for i, (x, z, r) in enumerate(((0, 0, 0), (0.02, 0.01, 18), (-0.01, -0.02, -12))):
        sub = Mesh("bill")
        sub.box("money", 0, 0, 0, 0.16, 0.012, 0.08, top="money")
        sub.box("money_dark", 0, 0.012, 0, 0.05, 0.001, 0.05)
        m.add(sub, x, i * 0.013, z, rot_y_deg=r)
    for (x, z, n) in ((0.11, 0.03, 4), (0.09, -0.06, 2), (-0.11, 0.05, 3)):
        for k in range(n):
            m.cyl("gold", x, k * 0.008, z, 0.022, 0.022, 0.007, seg=8)
    return m


def tip_jar():
    m = Mesh("tip_jar")
    m.cyl("glass", 0, 0, 0, 0.07, 0.07, 0.16, seg=10, cap_top=False)
    for k in range(3):
        m.cyl("gold", 0.01 * k, 0.005 + k * 0.012, 0.0, 0.022, 0.022, 0.008, seg=8)
    m.box("money", 0, 0.03, 0, 0.09, 0.06, 0.012)
    m.box("paper", 0, 0.07, 0.072, 0.09, 0.05, 0.004)
    m.box("chalk", 0, 0.085, 0.075, 0.06, 0.012, 0.002)
    return m


# ---------------- Characters ----------------

def customer_tourist():
    m = _humanoid("customer_tourist", "skin_a", "hair_blond", "hawaii", "shorts", hair_style="short")
    for (x, y) in ((-0.10, 1.15), (0.08, 1.0), (-0.05, 0.9), (0.12, 1.25)):
        m.box("hawaii_dot", x, y, 0.125, 0.05, 0.05, 0.01)
    m.box("black", 0, 1.02, 0.14, 0.12, 0.08, 0.06)            # camera
    m.cyl("steel_dark", 0, 1.04, 0.20, 0.025, 0.025, 0.04, seg=6)
    m.box("leather", -0.05, 1.10, 0.13, 0.02, 0.30, 0.01)       # camera strap
    m.cyl("paper", 0, 1.72, 0, 0.22, 0.22, 0.015, seg=12)       # sun hat brim
    m.cyl("paper", 0, 1.72, 0, 0.13, 0.12, 0.08, seg=10)
    return m


def customer_elder():
    m = _humanoid("customer_elder", "skin_b", "hair_grey", "cardigan", "skirt", hair_style="long")
    m.box("shirt_white", 0, 1.15, 0.125, 0.10, 0.22, 0.01)
    for y in (1.0, 1.1, 1.2):
        m.cyl("gold", 0.0, y, 0.13, 0.01, 0.01, 0.005, seg=6)
    m.cyl("wood_dark", 0.32, 0, 0.10, 0.015, 0.015, 0.86, seg=6)   # cane
    m.box("wood_dark", 0.29, 0.86, 0.10, 0.08, 0.03, 0.03)
    for sx in (-1, 1):
        m.box("black", sx * 0.05, 1.61, 0.13, 0.06, 0.04, 0.008)   # glasses
    return m


def waiter():
    m = _humanoid("waiter", "skin_c", "hair_dark", "shirt_white", "pants_dark", hair_style="short")
    m.box("vest", 0, 0.85, 0.125, 0.36, 0.45, 0.012)
    m.box("tie", 0, 1.30, 0.13, 0.08, 0.04, 0.01)                    # bow tie
    m.cyl("steel", 0.30, 1.10, 0.18, 0.15, 0.15, 0.012, seg=12)      # tray
    m.add(coffee_cup(small=True), 0.30, 1.112, 0.15)
    m.add(cupcake(), 0.36, 1.112, 0.24, scale=0.8)
    return m


def cashier():
    m = _humanoid("cashier", "skin_a", "hair_brown", "red_shirt", "jeans", hair_style="long")
    m.box("apron", 0, 0.60, 0.125, 0.34, 0.60, 0.015)
    m.box("paper", 0.12, 1.15, 0.13, 0.08, 0.04, 0.006)               # name badge
    m.box("black", 0.30, 0.86, 0.10, 0.06, 0.10, 0.02)                # card reader
    return m


# ---------------- Furniture & decor ----------------

def sofa_booth():
    m = Mesh("sofa_booth")
    m.prism("wood_dark", chamfer_rect(1.40, 0.60, 0.03), 0, 0.12)
    m.prism("sofa", chamfer_rect(1.36, 0.50, 0.05), 0.12, 0.30, top_scale=0.97, cz=0.03)
    m.prism("sofa_dark", chamfer_rect(1.40, 0.18, 0.05), 0.12, 0.85, top_scale=0.93, cz=-0.22)
    for x in (-0.45, 0, 0.45):
        m.prism("sofa", chamfer_rect(0.40, 0.08, 0.03), 0.48, 0.42, top_scale=0.9, cx=x, cz=-0.10)
    return m


def table_long():
    m = Mesh("table_long")
    m.prism("wood", chamfer_rect(1.40, 0.70, 0.05), 0.70, 0.05, top_scale=0.99)
    for sx in (-1, 1):
        m.box("black", sx * 0.58, 0, 0, 0.06, 0.70, 0.55)
    return m


def neon_open_sign():
    m = Mesh("neon_open_sign")
    m.box("black", 0, 0, 0, 0.80, 0.34, 0.03)
    # "OPEN" block letters as neon bars
    def bar(x, y, w, h, mat="neon_pink"):
        m.box(mat, x, y, 0.025, w, h, 0.02)
    xs = (-0.27, -0.09, 0.09, 0.27)
    # O
    bar(xs[0], 0.06, 0.12, 0.02); bar(xs[0], 0.26, 0.12, 0.02)
    bar(xs[0] - 0.05, 0.06, 0.02, 0.22); bar(xs[0] + 0.05, 0.06, 0.02, 0.22)
    # P
    bar(xs[1] - 0.05, 0.06, 0.02, 0.22); bar(xs[1], 0.26, 0.12, 0.02); bar(xs[1], 0.16, 0.12, 0.02)
    bar(xs[1] + 0.05, 0.16, 0.02, 0.12)
    # E
    bar(xs[2] - 0.05, 0.06, 0.02, 0.22)
    for y in (0.06, 0.16, 0.26): bar(xs[2], y, 0.12, 0.02)
    # N
    bar(xs[3] - 0.05, 0.06, 0.02, 0.22); bar(xs[3] + 0.05, 0.06, 0.02, 0.22)
    for k in range(5):  # Çapraz çizgi (basamaklı)
        bar(xs[3] - 0.035 + k * 0.0175, 0.22 - k * 0.04, 0.02, 0.05)
    m.box("neon_blue", 0, 0.0, 0.025, 0.76, 0.012, 0.02)
    return m


def door():
    m = Mesh("door")
    m.box("wall_trim", 0, 0, 0, 1.10, 2.30, 0.12)
    m.box("wood_dark", 0, 0, 0.065, 0.90, 2.15, 0.04)
    m.box("glass", 0, 0.95, 0.09, 0.66, 1.0, 0.01)
    m.box("steel", 0.36, 1.0, 0.11, 0.04, 0.30, 0.04)
    m.box("amber", 0, 2.17, 0.07, 0.6, 0.06, 0.01)
    m.prism("rug_pattern", chamfer_rect(0.9, 0.5, 0.03), 0, 0.01, cz=0.45)
    return m


def wall_art():
    m = Mesh("wall_art")
    m.box("wood_dark", 0, 0, 0, 0.60, 0.80, 0.04)
    m.box("cream", 0, 0.05, 0.02, 0.50, 0.70, 0.01)
    m.cyl("art_a", 0, 0.45, 0.03, 0.12, 0.12, 0.004, seg=12)
    m.box("art_b", 0, 0.12, 0.028, 0.44, 0.18, 0.006)
    m.box("coffee", -0.08, 0.30, 0.029, 0.08, 0.12, 0.006)
    return m


def wall_clock():
    m = Mesh("wall_clock")
    # Duvara asılır, +Z'ye bakar: çerçeve kutu halkası ile yaklaşık daire
    for i in range(12):
        a = 2 * math.pi * i / 12
        m.box("wood_dark", 0.19 * math.cos(a), 0.2 + 0.19 * math.sin(a) - 0.03, 0, 0.07, 0.07, 0.04)
        m.box("black", 0.15 * math.cos(a), 0.2 + 0.15 * math.sin(a) - 0.008, 0.03, 0.016, 0.016, 0.005)
    m.box("cream", 0, 0.03, 0.01, 0.30, 0.34, 0.02)
    m.box("black", 0, 0.2, 0.025, 0.012, 0.11, 0.005)
    m.box("black", 0.04, 0.195, 0.025, 0.08, 0.012, 0.005)
    return m


def coffee_bag_shelf():
    m = Mesh("coffee_bag_shelf")
    m.box("wood_dark", 0, 0, 0, 0.9, 0.04, 0.30)
    m.box("wood_dark", 0, 0.45, 0, 0.9, 0.04, 0.30)
    for sx in (-1, 1):
        m.box("wood_dark", sx * 0.43, 0, 0, 0.04, 0.9, 0.30)
    for row in (0.04, 0.49):
        for i, mat in enumerate(("paper", "leather", "paper", "chalk")):
            x = -0.3 + i * 0.2
            m.prism(mat, chamfer_rect(0.14, 0.10, 0.02), row, 0.26, top_scale=0.7, cx=x)
            m.box("amber", x, row + 0.10, 0.051, 0.07, 0.06, 0.004)
    return m


EXTRA_MODELS = [
    oven, tea_station, blender_station,
    tea_cup, donut, cupcake, sandwich, smoothie_cup,
    money_pile, tip_jar,
    customer_tourist, customer_elder, waiter, cashier,
    sofa_booth, table_long, neon_open_sign, door, wall_art, wall_clock, coffee_bag_shelf,
]
