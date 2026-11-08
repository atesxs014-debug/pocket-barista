"""Tiny numpy z-buffer rasterizer for low-poly previews (flat shading, point lights, outlines, glass)."""
import math
import numpy as np
from PIL import Image
from lowpoly import PALETTE, normal


def _norm(v):
    v = np.asarray(v, dtype=np.float64)
    return v / (np.linalg.norm(v) or 1.0)


class Light:
    def __init__(self, pos, color, intensity, radius):
        self.pos, self.color, self.intensity, self.radius = np.array(pos, float), np.array(color, float), intensity, radius


def render(meshes, width, height, cam_dir=(1.0, 0.85, 1.0), target=(0, 0.6, 0), ortho_height=4.0,
           bg_top=(0.13, 0.12, 0.12), bg_bottom=(0.07, 0.065, 0.07), key_dir=(0.5, 1.0, 0.3),
           key_color=(1.0, 0.92, 0.82), key_strength=0.75, ambient=0.32, fill_color=(0.55, 0.62, 0.85),
           point_lights=(), ss=2, outline=True):
    W, H = width * ss, height * ss
    fwd = -_norm(cam_dir)
    right = _norm(np.cross(fwd, [0, 1, 0]))
    up = np.cross(right, fwd)
    target = np.array(target, float)
    scale = H / ortho_height
    key = _norm(key_dir)

    # background gradient
    t = np.linspace(0, 1, H)[:, None, None]
    img = (np.array(bg_top)[None, None, :] * (1 - t) + np.array(bg_bottom)[None, None, :] * t) * np.ones((H, W, 3))
    zbuf = np.full((H, W), np.inf)
    nbuf = np.zeros((H, W, 3))

    opaque, glass = [], []
    for mesh in meshes:
        for mat, poly in mesh.faces:
            (opaque if PALETTE[mat][4] >= 0.99 else glass).append((mat, poly))

    def shade(mat, poly):
        r, g, b, emissive, alpha = PALETTE[mat]
        base = np.array([r, g, b])
        if emissive:
            return np.minimum(1.0, base * 1.15)
        n = np.array(normal(poly))
        c = np.mean(np.array(poly), axis=0)
        light = ambient + key_strength * max(0.0, n @ key) * np.array(key_color)
        light = light + 0.18 * max(0.0, n @ _norm([-0.6, 0.3, -0.2])) * np.array(fill_color)
        for L in point_lights:
            d = L.pos - c
            dist = np.linalg.norm(d)
            att = L.intensity / (1.0 + (dist / L.radius) ** 2)
            light = light + att * max(0.0, n @ (d / (dist or 1))) * L.color
        return np.minimum(1.0, base * light)

    def project(poly):
        p = np.array(poly) - target
        return np.stack([W / 2 + (p @ right) * scale, H / 2 - (p @ up) * scale, p @ fwd], axis=1)

    def raster(mat, poly, blend_alpha=None):
        n = np.array(normal(poly))
        if blend_alpha is None and n @ fwd > 0.02:
            return  # back face
        col = shade(mat, poly)
        P = project(poly)
        for k in range(1, len(P) - 1):
            a, b, c = P[0], P[k], P[k + 1]
            x0, x1 = int(max(0, math.floor(min(a[0], b[0], c[0])))), int(min(W - 1, math.ceil(max(a[0], b[0], c[0]))))
            y0, y1 = int(max(0, math.floor(min(a[1], b[1], c[1])))), int(min(H - 1, math.ceil(max(a[1], b[1], c[1]))))
            if x1 < x0 or y1 < y0:
                continue
            area = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])
            if abs(area) < 1e-9:
                continue
            xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
            w0 = ((b[0] - xs) * (c[1] - ys) - (b[1] - ys) * (c[0] - xs)) / area
            w1 = ((c[0] - xs) * (a[1] - ys) - (c[1] - ys) * (a[0] - xs)) / area
            w2 = 1 - w0 - w1
            inside = (w0 >= -1e-6) & (w1 >= -1e-6) & (w2 >= -1e-6)
            if not inside.any():
                continue
            z = w0 * a[2] + w1 * b[2] + w2 * c[2]
            zs = zbuf[y0:y1 + 1, x0:x1 + 1]
            vis = inside & (z < zs - 1e-5)
            if blend_alpha is None:
                zs[vis] = z[vis]
                img[y0:y1 + 1, x0:x1 + 1][vis] = col
                nbuf[y0:y1 + 1, x0:x1 + 1][vis] = n
            else:
                region = img[y0:y1 + 1, x0:x1 + 1]
                region[vis] = region[vis] * (1 - blend_alpha) + col * blend_alpha

    for mat, poly in opaque:
        raster(mat, poly)
    for mat, poly in glass:
        raster(mat, poly, blend_alpha=PALETTE[mat][4])

    if outline:
        edge = np.zeros((H, W), bool)
        zf = np.where(np.isinf(zbuf), 1e6, zbuf)
        for axis in (0, 1):
            dz = np.abs(np.diff(zf, axis=axis))
            dn = 1 - np.sum(np.diff(nbuf, axis=axis) ** 2, axis=2) / 2
            e = (dz > 0.08) | (dn < 0.85)
            if axis == 0:
                edge[1:, :] |= e
            else:
                edge[:, 1:] |= e
        img[edge] *= 0.55

    img = img.reshape(height, ss, width, ss, 3).mean(axis=(1, 3))
    return Image.fromarray((np.clip(img, 0, 1) ** (1 / 1.1) * 255).astype(np.uint8))


def fit_ortho(meshes, cam_dir, margin=1.15):
    pts = np.array([p for m in meshes for _, poly in m.faces for p in poly])
    fwd = -_norm(cam_dir)
    right = _norm(np.cross(fwd, [0, 1, 0]))
    up = np.cross(right, fwd)
    center = (pts.max(0) + pts.min(0)) / 2
    q = pts - center
    h = (q @ up).max() - (q @ up).min()
    w = (q @ right).max() - (q @ right).min()
    return center, max(h, w) * margin
