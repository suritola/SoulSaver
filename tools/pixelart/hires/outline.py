"""외곽 (2-2 단계 5): 바깥 1칸(4방향 빈칸 옆 칸)을 외곽색으로. 두께 1칸 (안쪽 칸은 건드리지 않음)."""
import numpy as np

OL = (0x1A, 0x11, 0x2C)        # 기본 외곽
OL_WARM = (0x2D, 0x19, 0x2A)   # 따뜻한 재질(금 · 살 · 나무) 옆 — make_kit_weapons.py 의 'O'


def edge_cells(opaque):
    P = np.pad(opaque, 1)
    return opaque & (~P[:-2, 1:-1] | ~P[2:, 1:-1] | ~P[1:-1, :-2] | ~P[1:-1, 2:])


def outward(opaque):
    """가장자리 칸의 바깥쪽 방향 (dx 오른쪽 + · dy 아래 +) — 빈 이웃 쪽 합"""
    P = np.pad(opaque, 1)
    dx = (~P[1:-1, 2:]).astype(int) - (~P[1:-1, :-2]).astype(int)
    dy = (~P[2:, 1:-1]).astype(int) - (~P[:-2, 1:-1]).astype(int)
    return dx, dy


def inner_neighbor(mat, opaque, edge):
    """가장자리 칸마다 가장 가까운 안쪽 칸의 재질 (4방향 → 대각선 순)"""
    H, W = mat.shape
    out = mat.copy()
    inside = opaque & ~edge
    for y, x in np.argwhere(edge):
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
            yy, xx = y + dy, x + dx
            if 0 <= yy < H and 0 <= xx < W and inside[yy, xx]:
                out[y, x] = mat[yy, xx]
                break
    return out


def no_lonely(sel):
    """보조 외곽색 칸 중 같은 칸이 8-이웃에 없는 외톨이는 뺌 (1칸 섬 방지)"""
    from scipy import ndimage
    n = ndimage.convolve(sel.astype(int), np.ones((3, 3), int), mode="constant") - sel
    return sel & (n > 0)
