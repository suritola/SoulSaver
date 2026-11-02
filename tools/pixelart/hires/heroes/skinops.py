# 새 영웅 둘(씨름꾼 · 공사 반장) 스킨 메뉴 초상 도우미 — 이미 그려진 부위를 다른 재질로 옮기는 연산 (128칸 좌표)
import numpy as np


def conv(c, mask, mat, parts=None, lmap=None):
    """mask 칸(+ 부위 이름 목록 parts)을 새 재질로 — 단계는 lmap(옛 단계 → 새 단계, 기본 비례)로 옮겨 명암(형태)은 그대로"""
    mi = c.mi(mat)
    n_new = len(c.ramps[mi])
    sel = mask & (c.mat >= 0)
    if parts is not None:
        pids = [i for i, p in enumerate(c.parts) if p['name'] in parts]
        sel &= np.isin(c.pid, pids)
    ys, xs = np.nonzero(sel)
    for y, x in zip(ys, xs):
        old = int(c.lvl[y, x])
        n_old = len(c.ramps[c.mat[y, x]])
        lv = lmap[min(old, len(lmap) - 1)] if lmap is not None else int(round(old * (n_new - 1) / max(1, n_old - 1)))
        c.mat[y, x] = mi
        c.lvl[y, x] = min(lv, n_new - 1)
        c.fixed[y, x] = True
    return sel


def box(c, x0, y0, x1, y1):
    """128칸 좌표 사각 마스크 (x1 · y1 미포함)"""
    m = np.zeros((c.S, c.S), bool)
    m[max(0, y0):max(0, y1), max(0, x0):max(0, x1)] = True
    return m


def cells(x0, y0, x1, y1):
    return [(x, y) for y in range(y0, y1) for x in range(x0, x1)]


def block_glyph(rows, x0, y0, k=4):
    """글자 격자(# = 칸)를 k 칸 정사각으로 키운 칸 목록"""
    out = []
    for j, r in enumerate(rows):
        for i, ch in enumerate(r):
            if ch == '#':
                out += cells(x0 + i * k, y0 + j * k, x0 + (i + 1) * k, y0 + (j + 1) * k)
    return out
