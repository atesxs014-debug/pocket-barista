"""Pocket Barista model definitions. Units: meters, Y up, pivot at bottom center, front faces +Z."""
import math
from lowpoly import Mesh, chamfer_rect, circle


# ---------------- Furniture ----------------

def table_round():
    m = Mesh("table_round")
    m.prism("wood_dark", circle(0.42, 12), 0.70, 0.03)
    m.prism("wood", circle(0.40, 12), 0.73, 0.03, top_scale=0.97)
    m.cyl("black", 0, 0.06, 0, 0.04, 0.04, 0.64, seg=8)
    m.prism("black", chamfer_rect(0.50, 0.08, 0.02), 0, 0.06)
    m.prism("black", chamfer_rect(0.08, 0.50, 0.02), 0, 0.06)
    return m


def table_square():
    m = Mesh("table_square")
    m.prism("wood", chamfer_rect(0.86, 0.86, 0.05), 0.70, 0.05, top_scale=0.98)
    m.box("wood_dark", 0, 0.62, 0, 0.74, 0.08, 0.74)
    for sx in (-1, 1):
        for sz in (-1, 1):
            m.prism("wood_dark", chamfer_rect(0.07, 0.07, 0.015), 0, 0.70, top_scale=1.25, cx=sx * 0.34, cz=sz * 0.34)
    return m


def chair_wood():
    m = Mesh("chair_wood")
    m.prism("wood", chamfer_rect(0.44, 0.42, 0.04), 0.42, 0.05, top_scale=0.97)
    for sx in (-1, 1):
        for sz in (-1, 1):
            m.box("wood_dark", sx * 0.18, 0, sz * 0.17, 0.045, 0.42, 0.045)
    for sx in (-1, 1):
        m.box("wood_dark", sx * 0.18, 0.47, -0.19, 0.045, 0.48, 0.045)
    m.box("wood", 0, 0.82, -0.19, 0.42, 0.10, 0.035)
    for x in (-0.09, 0, 0.09):
        m.box("wood_light", x, 0.52, -0.19, 0.04, 0.30, 0.025)
    m.box("wood_dark", 0, 0.12, 0, 0.36, 0.03, 0.03)
    return m


def armchair():
    m = Mesh("armchair")
    m.prism("velvet_dark", chamfer_rect(0.86, 0.80, 0.06), 0.08, 0.30)
    m.prism("velvet", chamfer_rect(0.62, 0.62, 0.06), 0.38, 0.12, top_scale=0.95, cz=0.06)
    m.prism("velvet", chamfer_rect(0.80, 0.20, 0.06), 0.38, 0.52, top_scale=0.92, cz=-0.30)
    for sx in (-1, 1):
        m.prism("velvet", chamfer_rect(0.14, 0.72, 0.05), 0.38, 0.24, top_scale=0.9, cx=sx * 0.37, cz=0.02)
    for sx in (-1, 1):
        for sz in (-1, 1):
            m.cyl("wood_dark", sx * 0.34, 0, sz * 0.30, 0.025, 0.035, 0.08, seg=6)
    return m


def bar_stool():
    m = Mesh("bar_stool")
    m.prism("velvet", circle(0.19, 10), 0.72, 0.07, top_scale=0.9)
    m.cyl("steel", 0, 0.05, 0, 0.03, 0.03, 0.67, seg=8)
    m.cyl("steel", 0, 0.28, 0, 0.17, 0.17, 0.025, seg=10)
    m.cyl("steel_dark", 0, 0, 0, 0.22, 0.20, 0.05, seg=10)
    return m


def counter():
    m = Mesh("counter")
    m.box("wood_dark", 0, 0, 0, 1.90, 0.92, 0.78)
    for i in range(10):
        x = -0.855 + i * 0.19
        m.box("wood", x, 0.08, 0.395, 0.15, 0.78, 0.02)
    m.box("amber", 0, 0.03, 0.40, 1.88, 0.03, 0.02)
    m.prism("marble", chamfer_rect(2.00, 0.88, 0.04), 0.92, 0.06, top_scale=0.99)
    m.box("black", 0, 0, 0, 1.92, 0.05, 0.80)
    return m


def pastry_display():
    m = Mesh("pastry_display")
    m.box("wood_dark", 0, 0, 0, 0.70, 0.06, 0.42)
    m.box("steel", 0, 0.06, 0, 0.66, 0.015, 0.38)
    for x in (-0.20, 0.0, 0.20):
        m.add(croissant(), x, 0.075, 0.07, rot_y_deg=20)
        m.add(cake_slice(), x, 0.075, -0.09, rot_y_deg=-15)
    glass = Mesh("g")
    glass.box("glass", 0, 0.06, 0, 0.68, 0.32, 0.40)
    m.faces += glass.faces
    m.box("wood_dark", 0, 0.38, 0, 0.70, 0.03, 0.42)
    return m


def espresso_machine():
    m = Mesh("espresso_machine")
    m.prism("steel", chamfer_rect(0.70, 0.46, 0.04), 0, 0.06)
    m.box("steel", 0, 0.06, -0.08, 0.70, 0.40, 0.30)
    m.prism("black", chamfer_rect(0.74, 0.36, 0.03), 0.46, 0.04, cz=-0.08)
    m.box("amber", 0, 0.38, 0.075, 0.50, 0.03, 0.01)
    m.box("steel_dark", 0, 0.06, 0.13, 0.62, 0.025, 0.18)
    for x in (-0.17, 0.17):
        m.cyl("steel", x, 0.27, 0.11, 0.05, 0.05, 0.05, seg=8)
        m.cyl("black", x, 0.23, 0.13, 0.045, 0.045, 0.04, seg=8)
        m.box("black", x, 0.235, 0.22, 0.025, 0.025, 0.12)
        m.add(coffee_cup(small=True), x, 0.085, 0.13)
    m.cyl("steel", 0.31, 0.12, 0.10, 0.012, 0.012, 0.22, seg=5)
    m.cyl("white", 0, 0.30, 0.074, 0.05, 0.05, 0.01, seg=10)
    for i, x in enumerate((-0.2, 0.0, 0.2)):
        m.add(coffee_cup(small=True, plain=True), x, 0.50, -0.08)
    return m


def coffee_grinder():
    m = Mesh("coffee_grinder")
    m.prism("black", chamfer_rect(0.20, 0.24, 0.03), 0, 0.30)
    m.box("steel_dark", 0, 0.05, 0.10, 0.10, 0.04, 0.06)
    m.cyl("steel", 0, 0.30, 0, 0.06, 0.06, 0.05, seg=8)
    m.cyl("glass", 0, 0.35, 0, 0.05, 0.11, 0.18, seg=8, cap_top=False)
    m.cyl("bean", 0, 0.36, 0, 0.055, 0.09, 0.10, seg=8)
    m.cyl("black", 0, 0.53, 0, 0.115, 0.10, 0.03, seg=8)
    return m


def cash_register():
    m = Mesh("cash_register")
    m.prism("black", chamfer_rect(0.36, 0.34, 0.03), 0, 0.08)
    m.prism("steel_dark", chamfer_rect(0.32, 0.22, 0.02), 0.08, 0.06, top_scale=0.9, cz=0.04)
    m.box("black", 0, 0.08, -0.10, 0.08, 0.16, 0.04)
    m.box("black", 0, 0.24, -0.10, 0.26, 0.16, 0.02)
    m.box("amber", 0, 0.26, -0.088, 0.22, 0.12, 0.004)
    return m


def coffee_cup(small=False, plain=False):
    m = Mesh("coffee_cup")
    s = 0.7 if small else 1.0
    if not plain:
        m.cyl("white", 0, 0, 0, 0.085 * s, 0.09 * s, 0.012 * s, seg=12)
    y = 0 if plain else 0.012 * s
    m.cyl("white", 0, y, 0, 0.034 * s, 0.046 * s, 0.065 * s, seg=12, top="crema")
    m.box("white", 0.056 * s, y + 0.02 * s, 0, 0.02 * s, 0.035 * s, 0.012 * s)
    return m


def croissant():
    m = Mesh("croissant")
    m.sphere("pastry", 0, 0.025, 0, 0.055, 0.03, 0.035, seg=8, rings=4)
    for sx in (-1, 1):
        m.sphere("pastry", sx * 0.055, 0.018, 0.012, 0.03, 0.02, 0.022, seg=6, rings=3)
    return m


def cake_slice():
    m = Mesh("cake_slice")
    m.prism("cake", [(0.0, 0.0), (0.06, 0.05), (-0.06, 0.05)], 0, 0.035, top="cream", cz=-0.025)
    m.prism("cake", [(0.0, 0.0), (0.06, 0.05), (-0.06, 0.05)], 0.035, 0.03, top="cream", cz=-0.025)
    m.sphere("berry", 0, 0.072, 0.0, 0.012, seg=6, rings=3)
    return m


def plant_tall():
    m = Mesh("plant_tall")
    m.prism("pot_white", circle(0.18, 10), 0, 0.38, top_scale=1.15, top="bean")
    m.cyl("wood_dark", 0, 0.38, 0, 0.015, 0.01, 0.55, seg=5)
    leaves = [(0.18, 0.62, 0.05, 30), (-0.16, 0.70, 0.08, 150), (0.05, 0.86, -0.15, 260),
              (-0.08, 0.95, 0.12, 100), (0.14, 1.02, -0.05, 330), (0.0, 1.10, 0.0, 200)]
    for i, (x, y, z, ang) in enumerate(leaves):
        leaf = Mesh("leaf")
        leaf.sphere("leaf" if i % 2 else "leaf_dark", 0.12, 0, 0, 0.16, 0.025, 0.10, seg=7, rings=3)
        m.add(leaf, x * 0.4, y, z * 0.4, rot_y_deg=ang)
    return m


def plant_small():
    m = Mesh("plant_small")
    m.prism("terracotta", circle(0.09, 8), 0, 0.12, top_scale=1.2, top="bean")
    m.prism("terracotta", circle(0.115, 8), 0.12, 0.03)
    m.sphere("cactus", 0, 0.24, 0, 0.06, 0.11, 0.06, seg=8, rings=5)
    m.sphere("cactus", 0.07, 0.25, 0, 0.03, 0.05, 0.03, seg=6, rings=4)
    m.sphere("neon_pink", 0, 0.355, 0, 0.022, seg=6, rings=3)
    return m


def pendant_lamp():
    m = Mesh("pendant_lamp")
    m.cyl("black", 0, 0.40, 0, 0.006, 0.006, 0.60, seg=4)
    m.cyl("black", 0, 0.18, 0, 0.20, 0.04, 0.22, seg=12, cap_bottom=False)
    m.cyl("amber", 0, 0.181, 0, 0.19, 0.19, 0.002, seg=12, cap_top=False)
    m.sphere("bulb", 0, 0.16, 0, 0.06, seg=8, rings=5)
    return m


def menu_board():
    m = Mesh("menu_board")
    for sz in (-1, 1):
        board = Mesh("b")
        board.box("wood_dark", 0, 0, 0, 0.60, 0.95, 0.03)
        board.box("chalk", 0, 0.12, 0.016, 0.50, 0.72, 0.005)
        for k, w in enumerate((0.32, 0.22, 0.28, 0.18, 0.30)):
            board.box("white" if k % 2 == 0 else "amber", -0.08 + w / 2 - 0.12, 0.70 - k * 0.11, 0.02, w, 0.025, 0.003)
        rot = 0 if sz > 0 else 180
        m.add(board, 0, 0, sz * 0.12, rot_y_deg=rot)
    return m


def bookshelf():
    m = Mesh("bookshelf")
    m.box("wood_dark", -0.48, 0, 0, 0.04, 1.80, 0.32)
    m.box("wood_dark", 0.48, 0, 0, 0.04, 1.80, 0.32)
    m.box("wood_dark", 0, 0, -0.15, 1.0, 1.80, 0.02)
    colors = ["book_red", "book_blue", "book_green", "book_yellow", "leather", "cream"]
    for shelf in range(4):
        y = shelf * 0.45
        m.box("wood", 0, y, 0, 0.92, 0.04, 0.30)
        x = -0.42
        k = shelf * 3
        while x < 0.36:
            w = 0.035 + 0.02 * ((k * 7) % 3)
            h = 0.22 + 0.04 * ((k * 5) % 4)
            m.box(colors[k % len(colors)], x + w / 2, y + 0.04, 0.0, w, h, 0.20)
            x += w + 0.008
            k += 1
        if shelf == 3:
            m.add(plant_small(), 0.30, y + 0.04, 0.02)
    m.box("wood", 0, 1.78, 0, 1.0, 0.04, 0.32)
    return m


def rug():
    m = Mesh("rug")
    m.prism("rug", chamfer_rect(2.0, 1.4, 0.1), 0, 0.01)
    m.prism("rug_pattern", chamfer_rect(1.6, 1.0, 0.08), 0.01, 0.002, cap_bottom=False)
    m.prism("rug", chamfer_rect(1.3, 0.7, 0.06), 0.012, 0.002, cap_bottom=False)
    return m


def floor_tile():
    m = Mesh("floor_tile")
    for i in range(5):
        z = -0.4 + i * 0.2
        m.box("plank_a" if i % 2 == 0 else "plank_b", 0, -0.03, z, 1.0, 0.03, 0.195)
    return m


def wall_segment():
    m = Mesh("wall_segment")
    m.box("wall", 0, 0, 0, 1.0, 2.6, 0.12)
    m.box("wall_trim", 0, 0, 0.065, 1.0, 0.12, 0.02)
    for row in range(5):
        for col in range(3):
            off = 0.16 if row % 2 else 0.0
            x = -0.33 + col * 0.33 + off
            if x > 0.42:
                continue
            m.box("brick", x, 0.15 + row * 0.09, 0.062, 0.30, 0.07, 0.01)
    return m


def wall_window():
    m = Mesh("wall_window")
    m.box("wall", 0, 0, 0, 1.0, 0.9, 0.12)
    m.box("wall", 0, 2.1, 0, 1.0, 0.5, 0.12)
    m.box("wall", -0.46, 0.9, 0, 0.08, 1.2, 0.12)
    m.box("wall", 0.46, 0.9, 0, 0.08, 1.2, 0.12)
    m.box("wood_dark", 0, 0.88, 0.04, 0.88, 0.05, 0.16)
    m.box("wood_dark", 0, 0.9, 0, 0.04, 1.2, 0.08)
    m.box("wood_dark", 0, 1.48, 0, 0.84, 0.04, 0.08)
    glass = Mesh("g")
    glass.box("glass", 0, 0.9, 0, 0.84, 1.2, 0.02)
    m.faces += glass.faces
    m.box("neon_pink", 0, 1.9, 0.07, 0.6, 0.04, 0.02)
    m.box("wall_trim", 0, 0, 0.065, 1.0, 0.12, 0.02)
    return m


# ---------------- Characters (front faces +Z, ~1.7 m) ----------------

def _humanoid(name, skin, hair, top, legs, shoe="shoe", hair_style="short", hat=None):
    m = Mesh(name)
    for sx in (-1, 1):
        m.box(shoe, sx * 0.09, 0, 0.02, 0.11, 0.07, 0.22)
        m.prism(legs, chamfer_rect(0.12, 0.14, 0.02), 0.07, 0.75, top_scale=1.1, cx=sx * 0.09)
    m.prism(top, chamfer_rect(0.40, 0.24, 0.05), 0.80, 0.58, top_scale=1.08)
    m.prism(top, chamfer_rect(0.30, 0.20, 0.05), 1.38, 0.05, top_scale=0.8)
    for sx in (-1, 1):
        m.prism(top, chamfer_rect(0.10, 0.11, 0.02), 0.90, 0.50, top_scale=1.1, cx=sx * 0.26)
        m.sphere(skin, sx * 0.26, 0.86, 0.0, 0.05, seg=6, rings=4)
    m.cyl(skin, 0, 1.43, 0, 0.05, 0.05, 0.06, seg=6)
    m.sphere(skin, 0, 1.60, 0.0, 0.13, 0.14, 0.13, seg=10, rings=6)
    for sx in (-1, 1):
        m.box("eye", sx * 0.045, 1.60, 0.125, 0.025, 0.03, 0.01)
    if hair_style == "short":
        m.sphere(hair, 0, 1.66, -0.015, 0.14, 0.11, 0.14, seg=10, rings=4)
    elif hair_style == "long":
        m.sphere(hair, 0, 1.65, -0.02, 0.145, 0.12, 0.145, seg=10, rings=4)
        m.box(hair, 0, 1.30, -0.08, 0.28, 0.32, 0.08)
    elif hair_style == "bald_sides":
        m.sphere(hair, 0, 1.62, -0.03, 0.135, 0.09, 0.13, seg=10, rings=4)
    if hat == "beanie":
        m.cyl("beanie", 0, 1.66, 0, 0.145, 0.10, 0.12, seg=10)
        m.cyl("beanie", 0, 1.64, 0, 0.15, 0.15, 0.04, seg=10)
    elif hat == "cap":
        m.cyl("apron", 0, 1.68, 0, 0.14, 0.12, 0.08, seg=10)
        m.box("apron", 0, 1.68, 0.14, 0.18, 0.015, 0.12)
    return m


def customer_student():
    m = _humanoid("customer_student", "skin_a", "hair_brown", "hoodie", "jeans", hair_style="long")
    m.box("hoodie", 0, 1.32, -0.15, 0.30, 0.10, 0.10)  # hood
    m.prism("backpack", chamfer_rect(0.32, 0.16, 0.04), 0.88, 0.42, top_scale=0.9, cz=-0.20)
    m.box("leather", -0.11, 0.92, 0.125, 0.03, 0.45, 0.01)
    m.box("leather", 0.11, 0.92, 0.125, 0.03, 0.45, 0.01)
    return m


def customer_business():
    m = _humanoid("customer_business", "skin_b", "hair_dark", "suit", "pants_dark", hair_style="bald_sides")
    m.box("shirt_white", 0, 1.10, 0.125, 0.12, 0.28, 0.01)
    m.box("tie", 0, 0.98, 0.132, 0.05, 0.38, 0.01)
    m.box("leather", 0.30, 0.42, 0.0, 0.08, 0.30, 0.40)
    m.box("black", 0.30, 0.72, 0.0, 0.02, 0.06, 0.12)
    return m


def customer_local():
    m = _humanoid("customer_local", "skin_c", "hair_grey", "sweater", "jeans", hair_style="short", hat="beanie")
    m.prism("sweater", chamfer_rect(0.30, 0.22, 0.04), 1.33, 0.08, top_scale=0.9)  # collar
    m.add(coffee_cup(small=True, plain=True), 0.26, 0.80, 0.06)
    return m


def barista():
    m = _humanoid("barista", "skin_a", "hair_dark", "tshirt", "pants_dark", hair_style="short", hat="cap")
    m.box("apron", 0, 0.55, 0.125, 0.36, 0.75, 0.015)
    m.box("apron", 0, 1.22, 0.125, 0.22, 0.14, 0.012)
    m.box("amber", 0, 1.03, 0.134, 0.08, 0.06, 0.004)
    return m


MODELS = [
    table_round, table_square, chair_wood, armchair, bar_stool,
    counter, pastry_display, espresso_machine, coffee_grinder, cash_register,
    coffee_cup, croissant, cake_slice,
    plant_tall, plant_small, pendant_lamp, menu_board, bookshelf, rug,
    floor_tile, wall_segment, wall_window,
    customer_student, customer_business, customer_local, barista,
]
