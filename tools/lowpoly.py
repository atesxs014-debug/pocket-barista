"""Small low-poly mesh toolkit: primitives, transforms, OBJ/MTL export."""
import math
import os

# name: (r, g, b, emissive?, alpha)
PALETTE = {
    "wood":        (0.62, 0.40, 0.22, False, 1.0),
    "wood_light":  (0.78, 0.56, 0.33, False, 1.0),
    "wood_dark":   (0.30, 0.18, 0.11, False, 1.0),
    "plank_a":     (0.36, 0.24, 0.16, False, 1.0),
    "plank_b":     (0.31, 0.21, 0.14, False, 1.0),
    "marble":      (0.90, 0.88, 0.84, False, 1.0),
    "cream":       (0.94, 0.88, 0.76, False, 1.0),
    "steel":       (0.70, 0.71, 0.74, False, 1.0),
    "steel_dark":  (0.40, 0.41, 0.44, False, 1.0),
    "black":       (0.09, 0.09, 0.10, False, 1.0),
    "chalk":       (0.13, 0.16, 0.15, False, 1.0),
    "amber":       (1.00, 0.63, 0.22, True, 1.0),
    "neon_pink":   (1.00, 0.33, 0.62, True, 1.0),
    "bulb":        (1.00, 0.86, 0.55, True, 1.0),
    "glass":       (0.70, 0.85, 0.95, False, 0.35),
    "white":       (0.96, 0.96, 0.94, False, 1.0),
    "coffee":      (0.32, 0.18, 0.09, False, 1.0),
    "crema":       (0.80, 0.60, 0.38, False, 1.0),
    "bean":        (0.24, 0.13, 0.07, False, 1.0),
    "pastry":      (0.88, 0.62, 0.28, False, 1.0),
    "cake":        (0.45, 0.22, 0.14, False, 1.0),
    "berry":       (0.75, 0.12, 0.22, False, 1.0),
    "leaf":        (0.27, 0.56, 0.30, False, 1.0),
    "leaf_dark":   (0.17, 0.40, 0.22, False, 1.0),
    "cactus":      (0.36, 0.60, 0.36, False, 1.0),
    "terracotta":  (0.78, 0.42, 0.28, False, 1.0),
    "pot_white":   (0.88, 0.86, 0.82, False, 1.0),
    "velvet":      (0.62, 0.20, 0.22, False, 1.0),
    "velvet_dark": (0.45, 0.13, 0.16, False, 1.0),
    "rug":         (0.55, 0.30, 0.20, False, 1.0),
    "rug_pattern": (0.85, 0.65, 0.35, False, 1.0),
    "wall":        (0.20, 0.19, 0.20, False, 1.0),
    "wall_trim":   (0.13, 0.12, 0.13, False, 1.0),
    "brick":       (0.42, 0.22, 0.17, False, 1.0),
    "book_red":    (0.70, 0.20, 0.18, False, 1.0),
    "book_blue":   (0.20, 0.35, 0.60, False, 1.0),
    "book_green":  (0.25, 0.50, 0.35, False, 1.0),
    "book_yellow": (0.90, 0.72, 0.25, False, 1.0),
    "skin_a":      (0.96, 0.78, 0.64, False, 1.0),
    "skin_b":      (0.78, 0.55, 0.40, False, 1.0),
    "skin_c":      (0.52, 0.34, 0.24, False, 1.0),
    "hair_dark":   (0.15, 0.10, 0.07, False, 1.0),
    "hair_brown":  (0.42, 0.25, 0.13, False, 1.0),
    "hair_blond":  (0.88, 0.72, 0.40, False, 1.0),
    "hair_grey":   (0.70, 0.70, 0.70, False, 1.0),
    "hoodie":      (0.38, 0.58, 0.48, False, 1.0),
    "suit":        (0.17, 0.19, 0.25, False, 1.0),
    "shirt_white": (0.92, 0.92, 0.94, False, 1.0),
    "tie":         (0.72, 0.15, 0.20, False, 1.0),
    "sweater":     (0.80, 0.52, 0.30, False, 1.0),
    "beanie":      (0.55, 0.18, 0.22, False, 1.0),
    "jeans":       (0.22, 0.30, 0.45, False, 1.0),
    "pants_dark":  (0.15, 0.15, 0.18, False, 1.0),
    "apron":       (0.20, 0.25, 0.22, False, 1.0),
    "tshirt":      (0.92, 0.90, 0.86, False, 1.0),
    "backpack":    (0.85, 0.40, 0.20, False, 1.0),
    "leather":     (0.38, 0.22, 0.12, False, 1.0),
    "shoe":        (0.12, 0.11, 0.11, False, 1.0),
    "eye":         (0.06, 0.06, 0.07, False, 1.0),
    "eye_white":   (1.00, 1.00, 1.00, False, 1.0),
    "blush":       (0.95, 0.55, 0.55, False, 1.0),
    "mouth":       (0.55, 0.20, 0.20, False, 1.0),
    "money":       (0.42, 0.68, 0.40, False, 1.0),
    "money_dark":  (0.28, 0.50, 0.30, False, 1.0),
    "gold":        (1.00, 0.80, 0.25, False, 1.0),
    "frosting":    (0.98, 0.62, 0.74, False, 1.0),
    "chocolate":   (0.33, 0.19, 0.12, False, 1.0),
    "sprinkle":    (0.40, 0.75, 0.95, False, 1.0),
    "bread":       (0.90, 0.74, 0.48, False, 1.0),
    "lettuce":     (0.50, 0.78, 0.30, False, 1.0),
    "tomato":      (0.88, 0.25, 0.20, False, 1.0),
    "cheese":      (0.98, 0.82, 0.30, False, 1.0),
    "smoothie":    (0.95, 0.45, 0.55, False, 1.0),
    "tea":         (0.70, 0.35, 0.12, False, 1.0),
    "paper":       (0.93, 0.90, 0.84, False, 1.0),
    "oven_glow":   (1.00, 0.45, 0.15, True, 1.0),
    "neon_blue":   (0.30, 0.75, 1.00, True, 1.0),
    "hawaii":      (0.20, 0.65, 0.70, False, 1.0),
    "hawaii_dot":  (0.98, 0.85, 0.30, False, 1.0),
    "shorts":      (0.80, 0.70, 0.50, False, 1.0),
    "cardigan":    (0.48, 0.35, 0.55, False, 1.0),
    "skirt":       (0.30, 0.28, 0.40, False, 1.0),
    "vest":        (0.12, 0.12, 0.14, False, 1.0),
    "red_shirt":   (0.70, 0.18, 0.18, False, 1.0),
    "art_a":       (0.95, 0.55, 0.30, False, 1.0),
    "art_b":       (0.25, 0.40, 0.60, False, 1.0),
    "sofa":        (0.28, 0.36, 0.34, False, 1.0),
    "sofa_dark":   (0.19, 0.25, 0.24, False, 1.0),
    "sand":        (0.90, 0.80, 0.58, False, 1.0),
    "sand_dark":   (0.82, 0.71, 0.50, False, 1.0),
    "deck":        (0.66, 0.50, 0.34, False, 1.0),
    "deck_dark":   (0.55, 0.41, 0.28, False, 1.0),
    "sea":         (0.25, 0.62, 0.75, False, 1.0),
    "stripe_red":  (0.88, 0.30, 0.28, False, 1.0),
    "palm":        (0.55, 0.42, 0.28, False, 1.0),
    "concrete":    (0.50, 0.50, 0.52, False, 1.0),
    "concrete_dk": (0.38, 0.38, 0.40, False, 1.0),
    "tile_white":  (0.88, 0.88, 0.86, False, 1.0),
    "tile_grey":   (0.72, 0.73, 0.74, False, 1.0),
    "glass_dark":  (0.25, 0.32, 0.42, False, 0.6),
    "skyline":     (0.95, 0.80, 0.45, True, 1.0),
    "green_board": (0.18, 0.32, 0.24, False, 1.0),
    "cork":        (0.75, 0.58, 0.38, False, 1.0),
    "beanbag":     (0.95, 0.55, 0.25, False, 1.0),
    "neon_green":  (0.35, 1.00, 0.55, True, 1.0),
    "neon_purple": (0.70, 0.35, 1.00, True, 1.0),
    "fire":        (1.00, 0.50, 0.12, True, 1.0),
    "rope":        (0.78, 0.15, 0.20, False, 1.0),
    "laptop":      (0.62, 0.64, 0.68, False, 1.0),
    "screen":      (0.45, 0.70, 0.95, True, 1.0),
}


SMOOTH = True  # Büyük kutuları pahlar, büyük silindir/küreleri yumuşatır


class Mesh:
    def __init__(self, name):
        self.name = name
        self.faces = []  # (mat, [(x,y,z),...]) CCW seen from outside

    # ---------- composition ----------
    def add(self, other, tx=0.0, ty=0.0, tz=0.0, rot_y_deg=0.0, scale=1.0):
        c, s = math.cos(math.radians(rot_y_deg)), math.sin(math.radians(rot_y_deg))
        for mat, poly in other.faces:
            out = []
            for (x, y, z) in poly:
                x, y, z = x * scale, y * scale, z * scale
                out.append((x * c + z * s + tx, y + ty, -x * s + z * c + tz))
            self.faces.append((mat, out))
        return self

    def face(self, mat, pts):
        self.faces.append((mat, list(pts)))

    # ---------- primitives ----------
    def loft(self, mat, rings, top=None, cap_top=True, cap_bottom=True):
        """rings: [(outline_xz, y, cx, cz)], all outlines same length, increasing angle."""
        pts = [[(cx + x, y, cz + z) for (x, z) in o] for (o, y, cx, cz) in rings]
        n = len(pts[0])
        for k in range(len(pts) - 1):
            b, t = pts[k], pts[k + 1]
            for i in range(n):
                j = (i + 1) % n
                self.face(mat, [b[j], b[i], t[i], t[j]])
        if cap_top:
            self.face(top or mat, list(reversed(pts[-1])))
        if cap_bottom:
            self.face(mat, pts[0])
        return self

    def rbox(self, mat, cx, y0, cz, sx, sy, sz, b=None, top=None):
        """Kenarları pahlı (yumuşak) kutu."""
        if b is None:
            b = min(0.018, 0.14 * min(sx, sy, sz))
        outer = chamfer_rect(sx, sz, b)
        inner = chamfer_rect(sx - 1.4 * b, sz - 1.4 * b, b * 0.5)
        y1 = y0 + sy
        return self.loft(mat, [(inner, y0, cx, cz), (outer, y0 + b, cx, cz), (outer, y1 - b, cx, cz), (inner, y1, cx, cz)], top=top)

    def box(self, mat, cx, y0, cz, sx, sy, sz, top=None, bottom=None):
        if SMOOTH and bottom is None and min(sx, sy, sz) >= 0.05:
            return self.rbox(mat, cx, y0, cz, sx, sy, sz, top=top)
        x0, x1, z0, z1, y1 = cx - sx / 2, cx + sx / 2, cz - sz / 2, cz + sz / 2, y0 + sy
        self.face(top or mat, [(x0, y1, z0), (x0, y1, z1), (x1, y1, z1), (x1, y1, z0)])
        self.face(bottom or mat, [(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)])
        self.face(mat, [(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)])
        self.face(mat, [(x1, y0, z0), (x0, y0, z0), (x0, y1, z0), (x1, y1, z0)])
        self.face(mat, [(x1, y0, z1), (x1, y0, z0), (x1, y1, z0), (x1, y1, z1)])
        self.face(mat, [(x0, y0, z0), (x0, y0, z1), (x0, y1, z1), (x0, y1, z0)])
        return self

    def prism(self, mat, outline, y0, h, top=None, top_scale=1.0, cap_bottom=True, cx=0.0, cz=0.0):
        """Extrude an XZ outline ordered by increasing angle (+X toward +Z). top_scale tapers/bevels the top."""
        n = len(outline)
        b = [(cx + x, y0, cz + z) for (x, z) in outline]
        t = [(cx + x * top_scale, y0 + h, cz + z * top_scale) for (x, z) in outline]
        for i in range(n):
            j = (i + 1) % n
            self.face(mat, [b[j], b[i], t[i], t[j]])
        self.face(top or mat, list(reversed(t)))
        if cap_bottom:
            self.face(mat, b)
        return self

    def cyl(self, mat, cx, y0, cz, r0, r1, h, seg=10, top=None, cap_top=True, cap_bottom=True):
        if SMOOTH and max(r0, r1) >= 0.06:
            seg = max(seg, 14)
        b = [(cx + r0 * math.cos(2 * math.pi * i / seg), y0, cz + r0 * math.sin(2 * math.pi * i / seg)) for i in range(seg)]
        t = [(cx + r1 * math.cos(2 * math.pi * i / seg), y0 + h, cz + r1 * math.sin(2 * math.pi * i / seg)) for i in range(seg)]
        for i in range(seg):
            j = (i + 1) % seg
            if r1 > 1e-6:
                self.face(mat, [b[j], b[i], t[i], t[j]])
            else:
                self.face(mat, [b[j], b[i], (cx, y0 + h, cz)])
        if cap_top and r1 > 1e-6:
            self.face(top or mat, list(reversed(t)))
        if cap_bottom and r0 > 1e-6:
            self.face(mat, b)
        return self

    def sphere(self, mat, cx, cy, cz, rx, ry=None, rz=None, seg=8, rings=5):
        if SMOOTH and rx >= 0.08:
            seg, rings = max(seg, 14), max(rings, 8)
        ry = rx if ry is None else ry
        rz = rx if rz is None else rz
        rows = []
        for k in range(rings + 1):
            phi = math.pi * k / rings
            rows.append([(cx + rx * math.sin(phi) * math.cos(2 * math.pi * i / seg), cy + ry * math.cos(phi),
                          cz + rz * math.sin(phi) * math.sin(2 * math.pi * i / seg)) for i in range(seg)])
        for k in range(rings):
            for i in range(seg):
                j = (i + 1) % seg
                a, b, c, d = rows[k][i], rows[k][j], rows[k + 1][j], rows[k + 1][i]
                if k == 0:
                    self.face(mat, [a, c, d])
                elif k == rings - 1:
                    self.face(mat, [a, b, c])
                else:
                    self.face(mat, [a, b, c, d])
        return self

    def quad_y(self, mat, x0, z0, x1, z1, y):
        """Flat upward-facing quad (decals, rugs)."""
        self.face(mat, [(x0, y, z0), (x0, y, z1), (x1, y, z1), (x1, y, z0)])
        return self


def chamfer_rect(sx, sz, c):
    hx, hz = sx / 2, sz / 2
    return [(-hx + c, -hz), (hx - c, -hz), (hx, -hz + c), (hx, hz - c),
            (hx - c, hz), (-hx + c, hz), (-hx, hz - c), (-hx, -hz + c)]


def circle(r, seg):
    return [(r * math.cos(2 * math.pi * i / seg), r * math.sin(2 * math.pi * i / seg)) for i in range(seg)]


def normal(poly):
    ax, ay, az = (poly[1][i] - poly[0][i] for i in range(3))
    bx, by, bz = (poly[2][i] - poly[0][i] for i in range(3))
    nx, ny, nz = ay * bz - az * by, az * bx - ax * bz, ax * by - ay * bx
    l = math.sqrt(nx * nx + ny * ny + nz * nz) or 1.0
    return nx / l, ny / l, nz / l


def write_obj(mesh, out_dir):
    mats = sorted({m for m, _ in mesh.faces})
    with open(os.path.join(out_dir, mesh.name + ".mtl"), "w") as f:
        for m in mats:
            r, g, b, emissive, alpha = PALETTE[m]
            f.write(f"newmtl {m}\nKd {r:.3f} {g:.3f} {b:.3f}\nKa 0 0 0\nKs 0 0 0\n")
            if emissive:
                f.write(f"Ke {r:.3f} {g:.3f} {b:.3f}\n")
            f.write(f"d {alpha:.2f}\nillum 1\n\n")
    out = [f"# Pocket Barista low-poly: {mesh.name}", f"mtllib {mesh.name}.mtl", f"o {mesh.name}"]
    vi, ni = 1, 1
    for m in mats:
        out += [f"usemtl {m}", "s off"]
        for fm, poly in mesh.faces:
            if fm != m:
                continue
            n = normal(poly)
            out += [f"v {x:.4f} {y:.4f} {z:.4f}" for (x, y, z) in poly]
            out.append(f"vn {n[0]:.4f} {n[1]:.4f} {n[2]:.4f}")
            out.append("f " + " ".join(f"{vi + k}//{ni}" for k in range(len(poly))))
            vi += len(poly)
            ni += 1
    with open(os.path.join(out_dir, mesh.name + ".obj"), "w") as f:
        f.write("\n".join(out) + "\n")
    return sum(len(p) - 2 for _, p in mesh.faces)
