"""Gerçekçi oranlı, yumuşak hatlı low-poly karakterler. ~1.72 m, pivot ayak altı, yüz +Z."""
from lowpoly import Mesh, chamfer_rect
from models import coffee_cup

HEAD_Y = 1.585   # Kafa merkezi
HEAD_R = 0.115
CHEST_Z = 0.125  # Göğüs ön yüzü (aksesuarlar için)


def person(name, skin, hair, top, legs, shoe="shoe", hair_style="short", hat=None, female=False):
    m = Mesh(name)
    hip = 0.17 if female else 0.15
    # Ayakkabı
    for sx in (-1, 1):
        m.loft(shoe, [(chamfer_rect(0.10, 0.22, 0.035), 0.0, sx * 0.09, 0.03),
                      (chamfer_rect(0.105, 0.23, 0.04), 0.035, sx * 0.09, 0.03),
                      (chamfer_rect(0.09, 0.16, 0.035), 0.08, sx * 0.09, 0.0)])
        # Baldır → diz → uyluk (konik loft)
        m.loft(legs, [(chamfer_rect(0.085, 0.09, 0.03), 0.08, sx * 0.09, 0),
                      (chamfer_rect(0.105, 0.11, 0.035), 0.30, sx * 0.09, 0.005),
                      (chamfer_rect(0.10, 0.11, 0.035), 0.46, sx * 0.09, 0.01),
                      (chamfer_rect(0.135, 0.15, 0.045), 0.70, sx * 0.085, 0.0),
                      (chamfer_rect(0.15, 0.17, 0.05), 0.84, sx * 0.08, 0.0)])
    # Kalça + gövde (bel incelir, göğüs/omuz genişler)
    m.loft(top, [(chamfer_rect(hip * 2 + 0.05, 0.22, 0.07), 0.80, 0, 0),
                 (chamfer_rect(0.32, 0.20, 0.07), 1.00, 0, 0),
                 (chamfer_rect(0.38 if not female else 0.34, 0.23, 0.08), 1.22, 0, 0.005),
                 (chamfer_rect(0.40 if not female else 0.35, 0.21, 0.08), 1.36, 0, 0),
                 (chamfer_rect(0.22, 0.15, 0.06), 1.43, 0, 0)])
    # Omuzlar, kollar (üst kol + dirsek + ön kol), eller
    sh = 0.21 if not female else 0.185
    for sx in (-1, 1):
        m.sphere(top, sx * sh, 1.34, 0, 0.055, seg=10, rings=6)
        m.cyl(top, sx * (sh + 0.02), 1.08, 0, 0.048, 0.06, 0.28, seg=8)
        m.cyl(top, sx * (sh + 0.03), 0.84, 0.01, 0.038, 0.047, 0.25, seg=8)
        m.loft(skin, [(chamfer_rect(0.06, 0.035, 0.012), 0.74, sx * (sh + 0.03), 0.01),
                      (chamfer_rect(0.075, 0.045, 0.015), 0.80, sx * (sh + 0.03), 0.01),
                      (chamfer_rect(0.06, 0.04, 0.012), 0.845, sx * (sh + 0.03), 0.01)])
    # Boyun + kafa (hafif yumurta), kulaklar, burun, gözler, kaşlar
    m.cyl(skin, 0, 1.42, 0, 0.05, 0.048, 0.08, seg=8)
    m.sphere(skin, 0, HEAD_Y, 0, HEAD_R, HEAD_R * 1.12, HEAD_R * 1.02, seg=14, rings=9)
    m.sphere(skin, 0, HEAD_Y - 0.06, -0.005, HEAD_R * 0.82, HEAD_R * 0.6, HEAD_R * 0.9, seg=12, rings=6)  # Çene
    for sx in (-1, 1):
        m.sphere(skin, sx * HEAD_R * 0.98, HEAD_Y - 0.005, -0.005, 0.02, 0.035, 0.025, seg=6, rings=4)
        m.sphere("eye_white", sx * 0.042, HEAD_Y + 0.008, HEAD_R * 0.93, 0.02, 0.014, 0.01, seg=8, rings=3)
        m.sphere("eye", sx * 0.042, HEAD_Y + 0.008, HEAD_R * 0.97, 0.011, 0.012, 0.006, seg=6, rings=3)
        m.box(hair, sx * 0.045, HEAD_Y + 0.04, HEAD_R * 0.93, 0.045, 0.01, 0.012)
    m.loft(skin, [(chamfer_rect(0.03, 0.03, 0.01), HEAD_Y - 0.04, 0, HEAD_R * 1.0),
                  (chamfer_rect(0.012, 0.012, 0.004), HEAD_Y + 0.01, 0, HEAD_R * 0.95)])
    m.box("mouth", 0, HEAD_Y - 0.06, HEAD_R * 0.9, 0.035, 0.007, 0.012)
    # Saç
    top_y = HEAD_Y + 0.03
    if hair_style in ("short", "long", "bun"):
        m.sphere(hair, 0, top_y, -0.012, HEAD_R * 1.08, HEAD_R * 0.98, HEAD_R * 1.08, seg=14, rings=8)
        m.rbox(hair, 0, HEAD_Y + 0.06, HEAD_R * 0.6, 0.19, 0.045, 0.07, b=0.015)   # Alın saçı
        for sx in (-1, 1):
            m.rbox(hair, sx * HEAD_R * 0.95, HEAD_Y - 0.03, -0.02, 0.035, 0.10, 0.16, b=0.01)  # Favoriler
    if hair_style == "long":
        m.loft(hair, [(chamfer_rect(0.26, 0.10, 0.04), HEAD_Y - 0.26, 0, -0.07),
                      (chamfer_rect(0.27, 0.13, 0.05), HEAD_Y - 0.05, 0, -0.06),
                      (chamfer_rect(0.25, 0.14, 0.05), HEAD_Y + 0.06, 0, -0.04)])
    elif hair_style == "bun":
        m.sphere(hair, 0, HEAD_Y + 0.10, -0.10, 0.06, seg=10, rings=6)
    elif hair_style == "bald_sides":
        m.sphere(hair, 0, HEAD_Y - 0.015, -0.04, HEAD_R * 1.1, HEAD_R * 0.8, HEAD_R * 1.02, seg=14, rings=8)
    # Şapkalar
    if hat == "beanie":
        m.cyl("beanie", 0, HEAD_Y + 0.04, 0, HEAD_R * 1.12, HEAD_R * 0.55, 0.13, seg=14)
        m.cyl("beanie", 0, HEAD_Y + 0.02, 0, HEAD_R * 1.15, HEAD_R * 1.15, 0.04, seg=14)
    elif hat == "cap":
        m.sphere("apron", 0, HEAD_Y + 0.05, 0, HEAD_R * 1.1, HEAD_R * 0.8, HEAD_R * 1.1, seg=14, rings=8)
        m.rbox("apron", 0, HEAD_Y + 0.05, HEAD_R + 0.04, 0.16, 0.016, 0.10, b=0.006)
    elif hat == "sunhat":
        m.cyl("paper", 0, HEAD_Y + 0.06, 0, 0.24, 0.24, 0.015, seg=18)
        m.cyl("paper", 0, HEAD_Y + 0.06, 0, 0.13, 0.11, 0.10, seg=14)
        m.cyl("tie", 0, HEAD_Y + 0.075, 0, 0.133, 0.13, 0.025, seg=14)
    return m


def customer_student():
    m = person("customer_student", "skin_a", "hair_brown", "hoodie", "jeans", hair_style="long", female=True)
    m.loft("hoodie", [(chamfer_rect(0.26, 0.10, 0.04), 1.32, 0, -0.13), (chamfer_rect(0.22, 0.08, 0.03), 1.44, 0, -0.14)])
    m.rbox("backpack", 0, 0.98, -0.19, 0.28, 0.36, 0.13)
    m.rbox("leather", 0, 1.06, -0.26, 0.15, 0.10, 0.03, b=0.008)
    for sx in (-1, 1):
        m.box("leather", sx * 0.10, 1.00, CHEST_Z, 0.03, 0.36, 0.01)
    return m


def customer_business():
    m = person("customer_business", "skin_b", "hair_dark", "suit", "pants_dark", hair_style="bald_sides")
    m.box("shirt_white", 0, 1.18, CHEST_Z, 0.10, 0.22, 0.012)
    m.box("tie", 0, 1.05, CHEST_Z + 0.008, 0.045, 0.32, 0.01)
    m.rbox("leather", 0.30, 0.45, 0.0, 0.07, 0.28, 0.36)
    m.box("black", 0.30, 0.73, 0.0, 0.02, 0.05, 0.10)
    return m


def customer_local():
    m = person("customer_local", "skin_c", "hair_grey", "sweater", "jeans", hair_style="short", hat="beanie")
    m.loft("sweater", [(chamfer_rect(0.24, 0.18, 0.06), 1.40, 0, 0), (chamfer_rect(0.20, 0.15, 0.05), 1.46, 0, 0)])
    m.add(coffee_cup(small=True, plain=True), 0.25, 0.85, 0.06)
    return m


def customer_tourist():
    m = person("customer_tourist", "skin_a", "hair_blond", "hawaii", "shorts", hair_style="short", hat="sunhat")
    for (x, y) in ((-0.10, 1.25), (0.09, 1.10), (-0.05, 0.95), (0.11, 1.30), (0.0, 1.18)):
        m.sphere("hawaii_dot", x, y, CHEST_Z, 0.025, 0.025, 0.01, seg=6, rings=3)
    m.rbox("black", 0, 1.06, 0.15, 0.13, 0.09, 0.07)
    m.sphere("steel_dark", 0, 1.105, 0.19, 0.03, 0.03, 0.02, seg=8, rings=3)
    for sx in (-1, 1):
        m.box("leather", sx * 0.07, 1.20, CHEST_Z, 0.02, 0.22, 0.01)
    return m


def customer_elder():
    m = person("customer_elder", "skin_b", "hair_grey", "cardigan", "skirt", hair_style="bun", female=True)
    for y in (1.0, 1.12, 1.24):
        m.sphere("gold", 0, y, CHEST_Z, 0.012, seg=6, rings=3)
    m.cyl("wood_dark", 0.29, 0, 0.10, 0.015, 0.015, 0.80, seg=6)
    m.rbox("wood_dark", 0.26, 0.80, 0.10, 0.08, 0.03, 0.03, b=0.006)
    for sx in (-1, 1):
        m.box("gold", sx * 0.042, HEAD_Y - 0.012, HEAD_R + 0.012, 0.045, 0.006, 0.006)
        m.box("gold", sx * 0.042, HEAD_Y + 0.028, HEAD_R + 0.012, 0.045, 0.006, 0.006)
    m.box("gold", 0, HEAD_Y + 0.008, HEAD_R + 0.014, 0.02, 0.006, 0.006)
    return m


def barista():
    m = person("barista", "skin_a", "hair_dark", "tshirt", "pants_dark", hair_style="short", hat="cap")
    m.box("apron", 0, 0.55, CHEST_Z - 0.005, 0.32, 0.72, 0.015)
    m.box("apron", 0, 1.22, CHEST_Z - 0.01, 0.20, 0.14, 0.012)
    m.box("amber", 0.08, 1.20, CHEST_Z + 0.005, 0.06, 0.04, 0.004)
    return m


def waiter():
    m = person("waiter", "skin_c", "hair_dark", "shirt_white", "pants_dark", hair_style="short")
    m.box("vest", 0, 0.92, CHEST_Z - 0.005, 0.32, 0.42, 0.012)
    m.sphere("tie", 0, 1.40, 0.08, 0.04, 0.02, 0.02, seg=8, rings=3)
    m.cyl("steel", 0.27, 1.06, 0.17, 0.15, 0.15, 0.014, seg=16)
    m.add(coffee_cup(small=True), 0.22, 1.074, 0.14)
    return m


def cashier():
    m = person("cashier", "skin_a", "hair_brown", "red_shirt", "jeans", hair_style="long", female=True)
    m.box("apron", 0, 0.55, CHEST_Z - 0.005, 0.30, 0.70, 0.015)
    m.box("paper", 0.09, 1.20, CHEST_Z, 0.07, 0.035, 0.006)
    m.rbox("black", 0.24, 0.78, 0.08, 0.06, 0.10, 0.025, b=0.006)
    return m


CHARACTERS = [customer_student, customer_business, customer_local, customer_tourist, customer_elder, barista, waiter, cashier]
