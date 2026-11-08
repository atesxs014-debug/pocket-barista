"""Mekan temalı modeller: Kampüs, Sahil, Şehir Merkezi, Neon Çatı Katı + alan açma (kilitli bölge) parçaları."""
import math
from lowpoly import Mesh, chamfer_rect, circle

# ---------- Ortak: zeminler ----------

def floor_tile_named(name, a, b, checker=False):
    m = Mesh(name)
    if checker:
        for ix in range(2):
            for iz in range(2):
                m.box(a if (ix + iz) % 2 == 0 else b, -0.25 + ix * 0.5, -0.02, -0.25 + iz * 0.5, 0.5, 0.02, 0.5)
    else:
        for i in range(4):
            m.box(a if i % 2 == 0 else b, 0, -0.02, -0.375 + i * 0.25, 1.0, 0.02, 0.245)
    return m

def sand_tile(): return floor_tile_named("sand_tile", "sand", "sand_dark")
def deck_tile(): return floor_tile_named("deck_tile", "deck", "deck_dark")
def concrete_tile(): return floor_tile_named("concrete_tile", "concrete", "concrete_dk", checker=True)
def marble_tile(): return floor_tile_named("marble_tile", "tile_white", "tile_grey", checker=True)

# ---------- Kampüs ----------

def study_desk():
    m = Mesh("study_desk")
    m.box("wood_light", 0, 0.70, 0, 1.2, 0.04, 0.60)
    for sx in (-1, 1):
        m.box("steel_dark", sx * 0.55, 0, 0, 0.05, 0.70, 0.50)
    m.box("steel_dark", 0, 0.35, -0.22, 1.05, 0.05, 0.03)
    # Laptop
    m.box("laptop", -0.25, 0.74, 0.05, 0.32, 0.015, 0.22)
    lid = Mesh("lid"); lid.box("laptop", 0, 0, 0, 0.32, 0.20, 0.012); lid.box("screen", 0, 0.015, 0.008, 0.28, 0.16, 0.004)
    m.add(lid, -0.25, 0.755, -0.06)
    # Kitap yığını
    for i, mat in enumerate(("book_red", "book_blue", "book_green")):
        m.box(mat, 0.30, 0.74 + i * 0.035, 0.0, 0.22, 0.032, 0.16)
    m.cyl("white", 0.10, 0.74, 0.12, 0.035, 0.04, 0.09, seg=10, top="coffee")
    return m

def chalkboard_stand():
    m = Mesh("chalkboard_stand")
    for sx in (-1, 1):
        m.box("wood_dark", sx * 0.36, 0, 0, 0.05, 1.5, 0.05)
    m.box("wood_dark", 0, 0.55, 0, 0.78, 0.86, 0.04)
    m.box("green_board", 0, 0.59, 0.022, 0.70, 0.78, 0.01)
    for i, w in enumerate((0.40, 0.52, 0.30, 0.46)):
        m.box("white", -0.30 + w / 2, 1.20 - i * 0.14, 0.03, w, 0.02, 0.004)
    return m

def bulletin_board():
    m = Mesh("bulletin_board")
    m.box("wood_dark", 0, 0, 0, 0.90, 0.62, 0.04)
    m.box("cork", 0, 0.04, 0.02, 0.82, 0.54, 0.01)
    for (x, y, mat) in ((-0.25, 0.35, "paper"), (0.05, 0.40, "hawaii_dot"), (0.28, 0.25, "paper"), (-0.15, 0.12, "sprinkle"), (0.18, 0.08, "frosting")):
        m.box(mat, x, y, 0.028, 0.18, 0.14, 0.004)
        m.sphere("stripe_red", x, y + 0.12, 0.035, 0.012, seg=6, rings=3)
    return m

def beanbag():
    m = Mesh("beanbag")
    m.sphere("beanbag", 0, 0.20, 0, 0.42, 0.22, 0.42, seg=14, rings=8)
    m.sphere("beanbag", 0, 0.42, -0.18, 0.30, 0.22, 0.18, seg=12, rings=6)
    return m

def bike_rack():
    m = Mesh("bike_rack")
    for i in range(4):
        x = -0.45 + i * 0.30
        m.cyl("steel", x, 0, 0, 0.018, 0.018, 0.6, seg=6)
        m.cyl("steel", x, 0, 0.25, 0.018, 0.018, 0.6, seg=6)
        m.box("steel", x, 0.58, 0.125, 0.04, 0.04, 0.29)
    return m

# ---------- Sahil ----------

def parasol():
    m = Mesh("parasol")
    m.cyl("white", 0, 0, 0, 0.18, 0.15, 0.06, seg=12)
    m.cyl("wood_light", 0, 0.06, 0, 0.025, 0.025, 2.1, seg=8)
    seg = 12
    for i in range(seg):
        a0, a1 = 2 * math.pi * i / seg, 2 * math.pi * (i + 1) / seg
        r, y0, y1 = 1.1, 1.85, 2.25
        p0 = (r * math.cos(a0), y0, r * math.sin(a0)); p1 = (r * math.cos(a1), y0, r * math.sin(a1))
        mat = "stripe_red" if i % 2 == 0 else "white"
        m.face(mat, [p1, p0, (0, y1, 0)])
        m.face(mat, [p0, p1, (0, y1 - 0.02, 0)])
    m.sphere("wood_light", 0, 2.27, 0, 0.04, seg=6, rings=4)
    return m

def lounger():
    m = Mesh("lounger")
    m.box("white", 0, 0.25, 0.15, 0.60, 0.06, 1.20)
    back = Mesh("back"); back.box("white", 0, 0, 0, 0.60, 0.06, 0.60)
    # Eğimli sırt: noktaları elle döndür
    ang = math.radians(50)
    for mat, poly in back.faces:
        pts = [(x, 0.28 + y * math.cos(ang) + (z + 0.30) * math.sin(ang), -0.45 - (z + 0.30) * math.cos(ang) + y * math.sin(ang) + 0.0) for (x, y, z) in poly]
        m.face(mat, pts)
    for sx in (-1, 1):
        for z in (-0.35, 0.65):
            m.box("steel", sx * 0.26, 0, z, 0.04, 0.25, 0.04)
    m.box("stripe_red", 0, 0.31, 0.15, 0.50, 0.03, 1.10)
    return m

def palm_tree():
    m = Mesh("palm_tree")
    y, x = 0.0, 0.0
    for i in range(7):
        r = 0.13 - i * 0.008
        m.cyl("palm", x, y, 0, r, r - 0.01, 0.42, seg=8)
        y += 0.40; x += 0.035
    m.sphere("wood_dark", x, y + 0.02, 0, 0.12, seg=8, rings=5)
    for k in range(7):
        a = 2 * math.pi * k / 7
        dx, dz = math.cos(a), math.sin(a)
        leaf = [(x, y + 0.08, 0),
                (x + dx * 0.55 - dz * 0.14, y + 0.12, dz * 0.55 + dx * 0.14),
                (x + dx * 1.1, y - 0.25, dz * 1.1),
                (x + dx * 0.55 + dz * 0.14, y + 0.12, dz * 0.55 - dx * 0.14)]
        m.face("leaf" if k % 2 else "leaf_dark", leaf)
        m.face("leaf" if k % 2 else "leaf_dark", list(reversed(leaf)))
    for k in range(3):
        a = 2 * math.pi * k / 3
        m.sphere("leaf_dark", x + 0.09 * math.cos(a), y - 0.05, 0.09 * math.sin(a), 0.06, seg=6, rings=4)
    return m

def surfboard():
    """Duvara/kuma dik dayalı sörf tahtası: XY düzleminde dış hat, Z'de kalınlık."""
    m = Mesh("surfboard")
    n = 20
    pts = []
    for i in range(n):
        a = 2 * math.pi * i / n
        x = 0.26 * math.cos(a) * (0.875 - 0.125 * math.sin(a))  # Uç tarafı daha dar
        y = 0.95 + 0.95 * math.sin(a)
        pts.append((x, y))
    z0, z1 = -0.03, 0.03
    front = [(x, y, z1) for (x, y) in pts]
    back = [(x, y, z0) for (x, y) in reversed(pts)]
    m.face("white", front)
    m.face("sea", back)
    for i in range(n):
        j = (i + 1) % n
        (xa, ya), (xb, yb) = pts[i], pts[j]
        m.face("sea", [(xa, ya, z0), (xb, yb, z0), (xb, yb, z1), (xa, ya, z1)])
    m.box("stripe_red", 0, 0.15, 0.031, 0.035, 1.6, 0.004)
    return m


def railing_white():
    m = Mesh("railing_white")
    for i in range(5):
        m.box("white", -0.5 + i * 0.25, 0, 0, 0.04, 0.9, 0.04)
    m.box("white", 0, 0.9, 0, 1.06, 0.05, 0.07)
    m.box("white", 0, 0.45, 0, 1.0, 0.03, 0.03)
    return m

# ---------- Şehir Merkezi ----------

def glass_wall():
    m = Mesh("glass_wall")
    m.box("black", 0, 0, 0, 1.0, 0.08, 0.12)
    m.box("black", 0, 2.42, 0, 1.0, 0.08, 0.12)
    for sx in (-1, 1):
        m.box("black", sx * 0.48, 0.08, 0, 0.04, 2.34, 0.12)
    m.box("glass_dark", 0, 0.08, 0, 0.92, 2.34, 0.02)
    # Uzak gökdelen ışıkları
    for (x, h) in ((-0.3, 1.1), (-0.05, 1.7), (0.22, 1.35)):
        m.box("concrete_dk", x, 0.3, -0.25, 0.18, h, 0.05)
        for k in range(int(h / 0.2)):
            m.box("skyline", x, 0.42 + k * 0.2, -0.22, 0.12, 0.05, 0.01)
    return m

def high_table():
    m = Mesh("high_table")
    m.cyl("marble", 0, 1.02, 0, 0.36, 0.36, 0.04, seg=16)
    m.cyl("black", 0, 0.06, 0, 0.035, 0.035, 0.96, seg=8)
    m.cyl("black", 0, 0, 0, 0.26, 0.24, 0.06, seg=16)
    return m

def modern_chair():
    m = Mesh("modern_chair")
    m.box("tile_white", 0, 0.42, 0, 0.44, 0.06, 0.42)
    m.box("tile_white", 0, 0.48, -0.19, 0.44, 0.40, 0.05)
    for sx in (-1, 1):
        m.cyl("wood_light", sx * 0.17, 0, 0.15, 0.02, 0.025, 0.42, seg=6)
        m.cyl("wood_light", sx * 0.17, 0, -0.15, 0.02, 0.025, 0.42, seg=6)
    return m

def planter_box():
    m = Mesh("planter_box")
    m.box("concrete_dk", 0, 0, 0, 1.0, 0.45, 0.35)
    m.box("bean", 0, 0.40, 0, 0.92, 0.04, 0.28)
    for i in range(7):
        x = -0.40 + i * 0.133
        m.sphere("leaf" if i % 2 else "leaf_dark", x, 0.52, (i % 3 - 1) * 0.05, 0.10, 0.14, 0.10, seg=8, rings=5)
    return m

def coat_rack():
    m = Mesh("coat_rack")
    m.cyl("black", 0, 0, 0, 0.18, 0.16, 0.03, seg=12)
    m.cyl("black", 0, 0.03, 0, 0.02, 0.02, 1.65, seg=6)
    for k in range(4):
        a = 2 * math.pi * k / 4
        m.box("black", 0.07 * math.cos(a), 1.60, 0.07 * math.sin(a), 0.03, 0.03, 0.03)
    m.box("suit", 0.08, 1.05, 0.0, 0.10, 0.55, 0.28)
    m.cyl("beanie", -0.07, 1.62, 0, 0.09, 0.07, 0.07, seg=10)
    return m

# ---------- Neon Çatı Katı ----------

def neon_bar():
    m = Mesh("neon_bar")
    m.box("black", 0, 0, 0, 2.0, 1.0, 0.6, top="black")
    m.box("concrete_dk", 0, 1.0, 0.0, 2.1, 0.05, 0.7)
    m.box("neon_purple", 0, 0.06, 0.305, 1.96, 0.025, 0.01)
    m.box("neon_purple", 0, 0.94, 0.305, 1.96, 0.025, 0.01)
    for i in range(5):
        m.box("neon_blue", -0.8 + i * 0.4, 0.15, 0.305, 0.02, 0.72, 0.01)
    for i, mat in enumerate(("glass", "glass", "glass")):
        m.cyl(mat, -0.6 + i * 0.25, 1.05, 0.05, 0.035, 0.04, 0.14, seg=8, top="smoothie")
    return m

def string_lights():
    m = Mesh("string_lights")
    for sx in (-1, 1):
        m.cyl("black", sx * 1.5, 0, 0, 0.03, 0.03, 2.5, seg=6)
    n = 12
    prev = None
    for i in range(n + 1):
        t = i / n
        x = -1.5 + 3.0 * t
        y = 2.45 - 0.45 * math.sin(math.pi * t)
        if prev:
            m.box("black", (x + prev[0]) / 2, min(y, prev[1]) - 0.003, 0, abs(x - prev[0]) + 0.01, abs(y - prev[1]) + 0.01, 0.008)
        if 0 < i < n:
            m.sphere("bulb" if i % 3 else "neon_pink", x, y - 0.05, 0, 0.035, 0.045, 0.035, seg=6, rings=4)
        prev = (x, y)
    return m

def fire_pit():
    m = Mesh("fire_pit")
    m.cyl("concrete_dk", 0, 0, 0, 0.45, 0.45, 0.35, seg=16, top="black")
    m.cyl("concrete", 0, 0.35, 0, 0.47, 0.47, 0.04, seg=16, cap_top=False)
    for k in range(5):
        a = 2 * math.pi * k / 5
        m.cyl("fire", 0.12 * math.cos(a), 0.35, 0.12 * math.sin(a), 0.07, 0.0, 0.25 + 0.05 * (k % 2), seg=6)
    m.cyl("amber", 0, 0.35, 0, 0.09, 0.0, 0.38, seg=6)
    return m

def neon_cup_sign():
    m = Mesh("neon_cup_sign")
    m.box("black", 0, 0, 0, 0.7, 0.7, 0.03)
    # Fincan (çizgi kutuları)
    m.box("neon_green", 0, 0.12, 0.025, 0.34, 0.025, 0.02)
    for sx in (-1, 1):
        m.box("neon_green", sx * 0.17, 0.12, 0.025, 0.025, 0.28, 0.02)
    m.box("neon_green", 0, 0.38, 0.025, 0.36, 0.025, 0.02)
    m.box("neon_green", 0.22, 0.20, 0.025, 0.08, 0.025, 0.02)
    m.box("neon_green", 0.26, 0.20, 0.025, 0.025, 0.12, 0.02)
    m.box("neon_green", 0.22, 0.31, 0.025, 0.08, 0.025, 0.02)
    for x in (-0.07, 0.07):  # Buhar
        for k in range(3):
            m.box("neon_pink", x + (0.025 if k % 2 else -0.025), 0.44 + k * 0.06, 0.025, 0.02, 0.05, 0.02)
    return m

def rooftop_railing():
    m = Mesh("rooftop_railing")
    m.box("concrete", 0, 0, 0, 1.0, 0.5, 0.2)
    m.box("glass", 0, 0.5, 0, 0.98, 0.55, 0.02)
    m.box("steel", 0, 1.05, 0, 1.0, 0.04, 0.06)
    return m

# ---------- Alan açma (kilitli bölge) ----------

def sale_sign():
    m = Mesh("sale_sign")
    m.cyl("wood_dark", 0, 0, 0, 0.03, 0.03, 1.1, seg=6)
    m.box("wood", 0, 0.75, 0.03, 0.70, 0.40, 0.04)
    m.box("amber", 0, 0.80, 0.055, 0.60, 0.30, 0.005)
    m.box("black", 0, 0.98, 0.06, 0.40, 0.04, 0.004)
    m.box("money", 0, 0.86, 0.06, 0.14, 0.08, 0.004)
    m.sphere("gold", 0, 1.20, 0.03, 0.06, seg=8, rings=5)
    return m

def rope_barrier():
    m = Mesh("rope_barrier")
    for sx in (-1, 1):
        m.cyl("gold", sx * 0.5, 0, 0, 0.14, 0.12, 0.04, seg=12)
        m.cyl("gold", sx * 0.5, 0.04, 0, 0.025, 0.025, 0.86, seg=8)
        m.sphere("gold", sx * 0.5, 0.92, 0, 0.045, seg=8, rings=5)
    for i in range(10):
        t0, t1 = i / 10, (i + 1) / 10
        x0, x1 = -0.5 + t0, -0.5 + t1
        y0, y1 = 0.85 - 0.18 * math.sin(math.pi * t0), 0.85 - 0.18 * math.sin(math.pi * t1)
        m.box("rope", (x0 + x1) / 2, min(y0, y1) - 0.012, 0, 0.105, abs(y1 - y0) + 0.03, 0.03)
    return m

def wood_fence():
    m = Mesh("wood_fence")
    for i in range(6):
        x = -0.42 + i * 0.168
        m.loft("wood_light", [(chamfer_rect(0.12, 0.03, 0.005), 0, x, 0), (chamfer_rect(0.12, 0.03, 0.005), 0.85, x, 0),
                              (chamfer_rect(0.02, 0.02, 0.005), 0.95, x, 0)])
    for y in (0.22, 0.65):
        m.box("wood", 0, y, -0.03, 1.0, 0.08, 0.03)
    return m


VENUE_MODELS = [
    sand_tile, deck_tile, concrete_tile, marble_tile,
    study_desk, chalkboard_stand, bulletin_board, beanbag, bike_rack,
    parasol, lounger, palm_tree, surfboard, railing_white,
    glass_wall, high_table, modern_chair, planter_box, coat_rack,
    neon_bar, string_lights, fire_pit, neon_cup_sign, rooftop_railing,
    sale_sign, rope_barrier, wood_fence,
]
