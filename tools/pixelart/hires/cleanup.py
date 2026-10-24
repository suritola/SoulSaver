"""정리 (2-2 단계 6): 1칸 섬(8-연결 같은 색 외톨이)을 이웃 색으로 · 알파 0/255."""
import collections

import numpy as np


def kill_singles(rgb, opaque, keep=None, protect=None, passes=3):
    """같은 RGB 8-이웃이 없는 칸 → 4-이웃 중 가장 많은 색 (keep = 의도한 점 · protect = 바꾸지 않을 칸, 예: 외곽).
    바뀐 칸 수 반환"""
    H, W = opaque.shape
    key = (rgb[..., 0].astype(np.int64) << 16) | (rgb[..., 1].astype(np.int64) << 8) | rgb[..., 2]
    changed = 0
    for _ in range(passes):
        P = np.pad(np.where(opaque, key, -1), 1, constant_values=-1)
        same = np.zeros((H, W), bool)
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if dy or dx:
                    same |= P[1 + dy:H + 1 + dy, 1 + dx:W + 1 + dx] == key
        lone = opaque & ~same
        if keep is not None:
            lone &= ~keep
        if protect is not None:
            lone &= ~protect
        if not lone.any():
            break
        for y, x in np.argwhere(lone):
            cnt = collections.Counter()
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)):
                yy, xx = y + dy, x + dx
                if 0 <= yy < H and 0 <= xx < W and opaque[yy, xx] and (protect is None or not protect[yy, xx]):
                    cnt[int(key[yy, xx])] += 2 if dy == 0 or dx == 0 else 1
            if cnt:
                v = cnt.most_common(1)[0][0]
                key[y, x] = v
                rgb[y, x] = ((v >> 16) & 255, (v >> 8) & 255, v & 255)
                changed += 1
    return changed


def to_rgba(rgb, opaque):
    out = np.zeros(opaque.shape + (4,), np.uint8)
    out[..., :3] = np.where(opaque[..., None], rgb, 0)
    out[..., 3] = np.where(opaque, 255, 0)
    return out
