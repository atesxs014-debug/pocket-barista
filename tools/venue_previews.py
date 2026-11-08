"""5 mekanın küçük önizleme sahneleri → venues_preview.png"""
import os
from PIL import Image, ImageDraw
from lowpoly import Mesh
from render import render, Light
import models as M
import models_extra as X
import models_venues as V
import characters as C

W, H = 700, 520


def room(floor, walls=True, size=(6, 5)):
    s = Mesh("room")
    nx, nz = size
    for ix in range(nx):
        for iz in range(nz):
            s.add(floor(), -nx / 2 + 0.5 + ix, 0, -nz / 2 + 0.5 + iz)
    if walls:
        for ix in range(nx):
            s.add(M.wall_window() if ix in (2, 3) else M.wall_segment(), -nx / 2 + 0.5 + ix, 0, -nz / 2 - 0.06)
        for iz in range(nz):
            s.add(M.wall_segment(), -nx / 2 - 0.06, 0, -nz / 2 + 0.5 + iz, rot_y_deg=90)
    return s


def counter_block(s, x, z):
    s.add(M.counter(), x, 0, z)
    s.add(M.espresso_machine(), x + 0.5, 0.98, z - 0.05)
    s.add(M.cash_register(), x - 0.3, 0.98, z + 0.1)
    s.add(C.barista(), x + 0.4, 0, z - 0.7)


def table_set(s, x, z, table=None, chair=None):
    s.add((table or M.table_round)(), x, 0, z)
    for (dx, r) in ((-0.62, 90), (0.62, -90)):
        s.add((chair or M.chair_wood)(), x + dx, 0, z, rot_y_deg=r)
    s.add(M.coffee_cup(), x - 0.12, 0.76 if table is None else 1.06 if table == V.high_table else 0.76, z)


def locked_area(s, x, z):
    for i in range(3):
        s.add(V.wood_fence(), x - 1 + i, 0, z)
    s.add(V.sale_sign(), x, 0, z + 0.4)


def lamps(points, color=(1.0, 0.68, 0.36), strength=1.2):
    return [Light((x, 2.2, z), color, strength, 1.1) for (x, z) in points]


def corner():
    s = room(M.floor_tile)
    counter_block(s, 0.8, -1.9)
    table_set(s, -1.3, -0.5); table_set(s, 0.6, 0.6)
    s.add(M.plant_tall(), -2.6, 0, -2.1); s.add(M.bookshelf(), -2.85, 0, 0.5, rot_y_deg=90)
    s.add(C.customer_local(), -0.4, 0, -0.9, rot_y_deg=150); s.add(C.customer_elder(), 1.5, 0, 0.9, rot_y_deg=-120)
    locked_area(s, 1.5, 1.9)
    return s, lamps([(-1.3, -0.5), (0.6, 0.6), (0.8, -1.9)])


def campus():
    s = room(M.floor_tile)
    counter_block(s, 0.8, -1.9)
    s.add(V.study_desk(), -1.5, 0, -0.4); s.add(M.chair_wood(), -1.5, 0, 0.15, rot_y_deg=180)
    s.add(V.study_desk(), 0.6, 0, 0.5); s.add(M.chair_wood(), 0.6, 0, 1.05, rot_y_deg=180)
    s.add(V.chalkboard_stand(), -2.4, 0, -1.9, rot_y_deg=30); s.add(V.bulletin_board(), -2.9, 1.0, 1.2, rot_y_deg=90)
    s.add(V.beanbag(), -2.2, 0, 1.6); s.add(V.beanbag(), -1.3, 0, 1.9, rot_y_deg=40)
    s.add(C.customer_student(), -1.0, 0, 0.0, rot_y_deg=200); s.add(C.customer_student(), 1.4, 0, -0.6, rot_y_deg=-140)
    return s, lamps([(-1.5, -0.4), (0.6, 0.5), (0.8, -1.9)])


def beach():
    s = room(V.sand_tile, walls=False, size=(7, 5))
    for ix in range(3):
        for iz in range(2):
            s.add(V.deck_tile(), 0.5 + ix, 0.005, -1.5 + iz)
    s.add(M.counter(), 1.6, 0, -1.6); s.add(M.espresso_machine(), 2.1, 0.98, -1.65); s.add(X.blender_station(), 1.2, 0.98, -1.6)
    s.add(C.barista(), 2.0, 0, -2.3)
    s.add(V.parasol(), -1.5, 0, -0.6); s.add(V.lounger(), -2.3, 0, -0.4, rot_y_deg=20)
    s.add(V.parasol(), -0.3, 0, 1.2); s.add(M.table_round(), -0.3, 0, 1.2); s.add(X.smoothie_cup(), -0.4, 0.76, 1.1)
    s.add(V.palm_tree(), -3.0, 0, -2.0); s.add(V.palm_tree(), 3.0, 0, 1.6, rot_y_deg=120)
    s.add(V.surfboard(), 3.2, 0, -1.7, rot_y_deg=-30)
    for i in range(3):
        s.add(V.railing_white(), 0.5 + i, 0, -0.5)
    s.add(C.customer_tourist(), 0.4, 0, 1.6, rot_y_deg=200); s.add(C.customer_tourist(), -1.6, 0, 0.6, rot_y_deg=60)
    return s, []


def downtown():
    s = room(V.marble_tile, walls=False)
    for ix in range(6):
        s.add(V.glass_wall(), -2.5 + ix, 0, -2.56)
    for iz in range(5):
        s.add(V.glass_wall(), -3.06, 0, -2.0 + iz, rot_y_deg=90)
    counter_block(s, 0.8, -1.8)
    table_set(s, -1.4, -0.4, V.high_table, V.modern_chair); table_set(s, 0.5, 0.7, V.high_table, V.modern_chair)
    s.add(V.planter_box(), -2.4, 0, 1.8, rot_y_deg=90); s.add(V.coat_rack(), -2.6, 0, -1.9)
    s.add(C.customer_business(), -0.6, 0, -0.9, rot_y_deg=160); s.add(C.customer_business(), 1.4, 0, 1.0, rot_y_deg=-110)
    locked_area(s, 1.5, 1.95)
    return s, lamps([(-1.4, -0.4), (0.5, 0.7), (0.8, -1.8)], color=(1.0, 0.85, 0.65))


def rooftop():
    s = room(V.concrete_tile, walls=False, size=(7, 5))
    for ix in range(7):
        s.add(V.rooftop_railing(), -3 + ix, 0, -2.6)
    s.add(V.neon_bar(), 0.8, 0, -1.7); s.add(C.waiter(), 1.2, 0, -2.3)
    s.add(V.neon_cup_sign(), -1.4, 0.8, -2.5)
    s.add(V.string_lights(), 0, 0, 0.0)
    s.add(V.fire_pit(), -1.8, 0, 0.8); s.add(X.sofa_booth(), -1.8, 0, 1.9, rot_y_deg=180)
    table_set(s, 1.2, 0.8, V.high_table, M.bar_stool)
    s.add(C.customer_business(), 0.2, 0, 0.3, rot_y_deg=170); s.add(C.customer_student(), -0.9, 0, 0.9, rot_y_deg=-90)
    lights = [Light((-1.8, 0.6, 0.8), (1.0, 0.5, 0.15), 1.6, 1.4), Light((0.8, 1.0, -1.2), (0.7, 0.35, 1.0), 1.2, 1.3),
              Light((0, 2.2, 0), (1.0, 0.8, 0.5), 0.9, 1.6)]
    return s, lights


VENUES = [("1. Mahalle Köşesi", corner, 0.24), ("2. Kampüs Kafesi", campus, 0.26), ("3. Sahil Kafesi", beach, 0.42),
          ("4. Şehir Merkezi", downtown, 0.34), ("5. Neon Çatı Katı", rooftop, 0.12)]


def build(path, font):
    sheet = Image.new("RGB", (W * 3, (H + 40) * 2), (22, 20, 20))
    d = ImageDraw.Draw(sheet)
    for i, (title, fn, amb) in enumerate(VENUES):
        s, lights = fn()
        open_air = fn in (beach, rooftop)
        img = render([s], W, H, cam_dir=(1.0, 0.95, 1.05), target=(0, 0.7 if fn is beach else 0.4, 0),
                     ortho_height=7.4 if fn is beach else 6.2, ambient=amb,
                     key_strength=0.55 if fn is beach else 0.4, point_lights=lights)
        x, y = (i % 3) * W, (i // 3) * (H + 40)
        sheet.paste(img, (x, y + 40))
        d.text((x + 14, y + 8), title, fill=(240, 200, 140), font=font(24))
    d.text((2 * W + 30, H + 40 + 200), "Yeniden doğuşla sırayla açılır.\nYıldızlar mekanlar arasında taşınır.\nAhşap çit + tabela = parayla\naçılan kilitli bölge.", fill=(210, 200, 190), font=font(24))
    sheet.save(path)
