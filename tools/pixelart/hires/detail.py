"""덧그림 (2-2 단계 3): 손 도트 지도(handmaps/*.txt)와 붓.

손 도트 지도 형식 (UTF-8):
  # 주석
  @legend
  k = eye:0          글자 = 재질:단계 (재질별 색 단계는 그리는 쪽 팔레트가 정함 — 같은 지도를 다른 색 벌로 다시 씀)
  @grid 이름
  ..kkk..            '.' · ' ' = 그대로 둠 (아래 그림이 보임)
"""
import os

import numpy as np
from PIL import Image, ImageDraw

HANDMAPS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "handmaps")


def load_handmaps(name):
    """handmaps/<name>.txt → (legend {글자: (재질, 단계)}, grids {이름: [행 문자열]})"""
    path = os.path.join(HANDMAPS, name + ".txt")
    legend, grids, cur, mode = {}, {}, None, None
    with open(path, encoding="utf-8") as f:
        for raw in f:
            line = raw.rstrip("\n")
            if line.startswith("#") and mode != "grid":
                continue
            if line.startswith("@legend"):
                mode = "legend"
                continue
            if line.startswith("@grid"):
                mode, cur = "grid", line.split(None, 1)[1].strip()
                grids[cur] = []
                continue
            if mode == "legend" and "=" in line:
                ch, _, val = line.partition("=")
                mat, _, tone = val.strip().partition(":")
                legend[ch.strip()] = (mat.strip(), int(tone or 0))
            elif mode == "grid" and line.strip() and not line.startswith("#"):
                grids[cur].append(line)
    for g, rows in grids.items():
        w = max(len(r) for r in rows)
        grids[g] = [r.ljust(w, ".") for r in rows]
    return legend, grids


def grid_array(rows, rot=0, flip=False):
    a = np.array([list(r) for r in rows])
    if flip:
        a = a[:, ::-1]
    return np.rot90(a, k=-rot) if rot else a


def stamp(mat, tone, rows, legend, x, y, rot=0, flip=False, clip=None):
    """mat · tone 지도(정수 · 재질 번호는 그리는 쪽이 정함 → legend 의 재질 이름을 번호로 바꾼 사전)에 찍기.
    clip: 이 칸 안에서만 (몸 밖으로 안 나가게). 찍은 칸 지도 반환"""
    g = grid_array(rows, rot, flip)
    H, W = mat.shape
    hit = np.zeros((H, W), bool)
    for (gy, gx), ch in np.ndenumerate(g):
        if ch in ". " or ch not in legend:
            continue
        yy, xx = y + gy, x + gx
        if 0 <= yy < H and 0 <= xx < W and (clip is None or clip[yy, xx]):
            mat[yy, xx], tone[yy, xx] = legend[ch]
            hit[yy, xx] = True
    return hit


def poly_mask(shape, pts):
    """다각형 채우기 (화소 중심 기준)"""
    H, W = shape
    im = Image.new("1", (W, H), 0)
    ImageDraw.Draw(im).polygon([(float(x), float(y)) for x, y in pts], fill=1, outline=1)
    return np.array(im, bool)


def disc_mask(shape, cx, cy, r):
    H, W = shape
    ys, xs = np.mgrid[0:H, 0:W]
    return (xs + 0.5 - cx) ** 2 + (ys + 0.5 - cy) ** 2 <= r * r
