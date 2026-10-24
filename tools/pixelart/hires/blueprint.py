"""설계도 (2-2 단계 1 · 2): 32칸 그림을 읽어 칸 좌표 → 새 화소 좌표로 옮기고, 부드러운 부위를 계단 없이 키운다."""
import os

import numpy as np
from PIL import Image
from scipy import ndimage

EIGHT = np.ones((3, 3), dtype=bool)


def guard(path):
    """AI 화소 차단: gemini 가 들어간 경로는 읽지 않음 (2-2)"""
    if "gemini" in os.path.normcase(os.path.abspath(path)):
        raise SystemExit(f"설계도 경로에 gemini 가 있음 — 이미지 생성 모델 화소는 쓰지 않음: {path}")
    return path


def load(path):
    return np.asarray(Image.open(guard(path)).convert("RGBA")).copy()


def cells(sheet, size=32):
    """시트를 가로로 먼저 · 위에서부터 칸 목록으로"""
    h, w = sheet.shape[:2]
    return [sheet[r * size:(r + 1) * size, c * size:(c + 1) * size] for r in range(h // size) for c in range(w // size)]


def hexrgb(s):
    s = s.lstrip("#")
    return tuple(int(s[i:i + 2], 16) for i in (0, 2, 4))


def bbox(mask):
    ys, xs = np.nonzero(mask)
    return int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1


def color_mask(im, colors):
    """설계도에서 그 색들인 칸 (불투명만)"""
    a = im[..., 3] > 127
    m = np.zeros(a.shape, bool)
    for c in colors:
        r, g, b = hexrgb(c) if isinstance(c, str) else c
        m |= (im[..., 0] == r) & (im[..., 1] == g) & (im[..., 2] == b)
    return m & a


class Grid:
    """설계도 1칸 = k 화소. 칸 좌표(실수) → 새 그림 화소(반올림)"""

    def __init__(self, k):
        self.k = float(k)

    def px(self, v):
        return int(round(v * self.k))

    def box(self, b):
        return tuple(self.px(v) for v in b)


def soft_upscale(mask, k, size, sigma=0.3, thr=0.5, keep_extremes=True):
    """부드러운 부위 키우기: 칸 지도 × k → 가우시안(σ = sigma·k) → 문턱 (계단 없는 곡선).
    keep_extremes: 덩어리마다 경계 상자 끝 칸은 키운 칸을 그대로 덮어 상자가 설계도 × k 와 맞게 (검사 14)"""
    H, W = size
    ys, xs = np.mgrid[0:H, 0:W]
    src = np.zeros((H, W), float)
    cy = np.clip((ys + 0.5) / k, 0, mask.shape[0] - 1e-6).astype(int)
    cx = np.clip((xs + 0.5) / k, 0, mask.shape[1] - 1e-6).astype(int)
    src = mask[cy, cx].astype(float)
    out = ndimage.gaussian_filter(src, sigma * k) > thr
    if keep_extremes:
        lab, n = ndimage.label(mask, structure=EIGHT)
        for i in range(1, n + 1):
            comp = lab == i
            x0, y0, x1, y1 = bbox(comp)
            for (yy, xx) in np.argwhere(comp):
                if xx in (x0, x1 - 1) or yy in (y0, y1 - 1):
                    out |= (cx == xx) & (cy == yy)
    return out


def components(mask):
    """8-연결 덩어리 목록 [(칸 지도, 경계 상자)]"""
    lab, n = ndimage.label(mask, structure=EIGHT)
    return [((lab == i), bbox(lab == i)) for i in range(1, n + 1)]
