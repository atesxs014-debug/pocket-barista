"""Generates every OBJ model, a catalog sheet and a cafe scene preview.
Usage: python3 build_all.py <output_dir>"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image, ImageDraw, ImageFont
import models as M
import models_extra as X
import characters as C
import models_venues as VM
import venue_previews as VP
for _f in C.CHARACTERS:
    setattr(M, _f.__name__, _f)
    setattr(X, _f.__name__, _f)
from lowpoly import Mesh, write_obj
from render import render, fit_ortho, Light


def font(size):
    for path in ("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", "/usr/share/fonts/dejavu/DejaVuSans.ttf"):
        if os.path.exists(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def catalog(meshes, path, cols=6, cell=260):
    rows = (len(meshes) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cell, rows * (cell + 30) + 70), (24, 22, 22))
    d = ImageDraw.Draw(sheet)
    d.text((20, 18), "Pocket Barista  -  low-poly modeller", fill=(240, 200, 140), font=font(28))
    cam = (1.0, 0.75, 1.3)
    for i, m in enumerate(meshes):
        center, size = fit_ortho([m], cam)
        img = render([m], cell, cell, cam_dir=cam, target=center, ortho_height=size,
                     bg_top=(0.15, 0.14, 0.14), bg_bottom=(0.10, 0.095, 0.10), ambient=0.42)
        x, y = (i % cols) * cell, 70 + (i // cols) * (cell + 30)
        sheet.paste(img, (x, y))
        d.text((x + 12, y + cell + 4), m.name, fill=(225, 215, 200), font=font(16))
    sheet.save(path)


def scene():
    s = Mesh("scene")
    for ix in range(8):
        for iz in range(7):
            s.add(M.floor_tile(), -3.5 + ix, 0, -3.0 + iz)
    for ix in range(8):
        piece = M.wall_window() if ix in (5, 6) else M.wall_segment()
        s.add(piece, -3.5 + ix, 0, -3.56)
    for iz in range(7):
        piece = M.wall_window() if iz in (3, 4) else M.wall_segment()
        s.add(piece, -4.06, 0, -3.0 + iz, rot_y_deg=90)

    # Counter area
    cx, cz = 0.6, -2.45
    s.add(M.counter(), cx, 0, cz)
    s.add(M.pastry_display(), cx - 0.6, 0.98, cz + 0.05)
    s.add(M.cash_register(), cx + 0.05, 0.98, cz + 0.12, rot_y_deg=10)
    s.add(M.espresso_machine(), cx + 0.55, 0.98, cz - 0.05)
    s.add(M.coffee_grinder(), cx + 1.05, 0.98, cz - 0.05)
    s.add(M.barista(), cx + 0.5, 0, cz - 0.72)
    s.add(M.menu_board(), 2.45, 0, -1.9, rot_y_deg=-35)
    s.add(M.bookshelf(), -3.82, 0, -2.2, rot_y_deg=90)
    s.add(M.plant_tall(), 3.3, 0, -3.1)

    # Dining tables
    for (tx, tz, kind) in ((-1.5, -0.6, "round"), (1.4, 0.4, "round"), (-0.2, 1.7, "square")):
        s.add(M.table_round() if kind == "round" else M.table_square(), tx, 0, tz)
        s.add(M.chair_wood(), tx - 0.62, 0, tz, rot_y_deg=90)
        s.add(M.chair_wood(), tx + 0.62, 0, tz, rot_y_deg=-90)
        s.add(M.coffee_cup(), tx - 0.15, 0.76, tz + 0.05)
        s.add(M.coffee_cup(), tx + 0.18, 0.76, tz - 0.08, rot_y_deg=180)
        s.add(M.plant_small(), tx, 0.76, tz - 0.12)

    # Lounge
    s.add(M.rug(), -2.6, 0, 2.0)
    s.add(M.armchair(), -3.25, 0, 1.6, rot_y_deg=70)
    s.add(M.armchair(), -2.2, 0, 2.9, rot_y_deg=170)
    s.add(M.plant_tall(), -3.55, 0, 2.95)
    s.add(M.table_round(), -2.55, 0, 1.95, scale=0.7)

    # New stations & decor
    s.add(X.tea_station(), cx - 1.15, 0.98, cz - 0.02)
    s.add(X.tip_jar(), cx + 0.3, 0.98, cz + 0.2)
    s.add(X.neon_open_sign(), 1.9, 1.4, -3.48)
    s.add(X.wall_art(), -4.0, 1.2, -1.0, rot_y_deg=90)
    s.add(X.wall_clock(), -1.5, 1.8, -3.48)
    s.add(X.coffee_bag_shelf(), -2.0, 1.1, -3.4)
    s.add(X.sofa_booth(), 2.9, 0, 2.6, rot_y_deg=180)
    s.add(X.table_long(), 2.9, 0, 1.9)
    s.add(X.smoothie_cup(), 2.6, 0.75, 1.9)
    s.add(X.donut(), 3.15, 0.75, 1.85)
    s.add(X.money_pile(), 1.4, 0.76, 0.6)
    s.add(X.cupcake(), -1.35, 0.76, -0.75)
    s.add(X.waiter(), 0.6, 0, 0.9, rot_y_deg=150)
    s.add(X.cashier(), cx - 0.2, 0, cz - 0.72)
    s.add(X.customer_tourist(), 1.0, 0, 2.6, rot_y_deg=200)
    s.add(X.customer_elder(), -2.6, 0, 1.2, rot_y_deg=220)

    # Customers
    s.add(M.customer_local(), cx - 0.1, 0, -1.25, rot_y_deg=180)
    s.add(M.customer_student(), 0.35, 0, -0.35, rot_y_deg=200)
    s.add(M.customer_business(), 2.05, 0, 0.55, rot_y_deg=-90)
    s.add(M.customer_student(), -2.4, 0, -0.2, rot_y_deg=60)

    # Lamps
    lamps = [(-1.5, -0.6), (1.4, 0.4), (-0.2, 1.7), (cx - 0.3, cz), (cx + 0.7, cz), (-2.6, 2.0)]
    for (lx, lz) in lamps:
        s.add(M.pendant_lamp(), lx, 2.15, lz)
    lights = [Light((lx, 2.25, lz), (1.0, 0.68, 0.36), 1.25, 1.1) for (lx, lz) in lamps]
    lights.append(Light((2.0, 1.9, -3.4), (1.0, 0.3, 0.6), 0.5, 1.2))   # neon window glow
    return s, lights


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "Models"
    os.makedirs(out, exist_ok=True)
    _chars = {f.__name__ for f in C.CHARACTERS}
    meshes = [fn() for fn in M.MODELS + X.EXTRA_MODELS + VM.VENUE_MODELS if fn.__name__ not in _chars] + [f() for f in C.CHARACTERS]
    total = 0
    for m in meshes:
        tris = write_obj(m, out)
        total += tris
        print(f"{m.name:20s} {tris:5d} tris")
    print(f"total {total} tris")
    catalog(meshes, os.path.join(out, "catalog.png"))
    print("catalog.png")
    s, lights = scene()
    img = render([s], 1400, 1050, cam_dir=(1.0, 0.95, 1.05), target=(-0.2, 0.4, -0.1), ortho_height=8.2,
                 ambient=0.22, key_strength=0.35, key_color=(0.75, 0.80, 1.0), point_lights=lights)
    img.save(os.path.join(out, "scene_preview.png"))
    print("scene_preview.png")
    VP.build(os.path.join(out, "venues_preview.png"), font)
    print("venues_preview.png")
