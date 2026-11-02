# 스킨 메뉴 초상 128칸 — ghost__deleted 「삭제된 장면」 (설계도: 왼쪽 반은 그려지고 오른쪽 반은 마젠타 와이어프레임 · 가로 취소선 x4~27 · y22)
import numpy as np
from scipy import ndimage

from .skinshape import Variant2 as Variant
from .skinshape import rect


def shape(c, mod):
    S = c.S
    body = c.mat >= 0
    dist = ndimage.distance_transform_edt(body)
    eye = np.isin(c.pid, [i for i, p in enumerate(c.parts) if p['name'] in ('eye_r', 'tear_r', 'mouth')])
    right = np.zeros((S, S), bool)
    right[:, 64:] = True
    blk = ((c.xx // 10).astype(int) + (c.yy // 10).astype(int)) % 2 == 0
    mesh = (((c.xx + c.yy) % 8) < 2) | (((c.xx - c.yy) % 8) < 2)
    rb = body & right & ~eye
    mi = c.mi('wire')
    fill, lines = rb, rb & mesh & (dist > 2.2)
    ring = rb & (dist <= 2.2) & (((c.xx // 12).astype(int) + (c.yy // 12).astype(int)) % 4 != 0)
    top = np.zeros((S, S), bool)
    top[:66] = True
    for m, lv in ((fill & ~top, 0), (fill & top, 1), (lines & ~top, 1), (lines & top, 2), (ring, 2)):
        c.mat[m] = mi
        c.lvl[m] = lv
        c.fixed[m] = True
    left = body & ~right & (c.mat == c.mi('sheet'))
    for y in range(0, 128):                                         # 위에서 아래로 줄무늬 단계 (그리다 만 면 — 넓은 한 색 면 나눔)
        base = 6 if y < 62 else 5 if y < 82 else 3 if y < 96 else 2
        lv = base - ((y // 7) % 2)
        m = left & (np.arange(128)[:, None] == y)
        c.lvl[m] = lv
        c.fixed[m] = True
    edge_l = left & (dist <= 4.5)
    c.lvl[edge_l] = np.maximum(c.lvl[edge_l] - 2, 0)
    pw = rect(c, 'strike', 4, 22, 28, 23, 'wire', behind=False)
    c.set_lvl([(x, y) for y in (86, 87) for x in range(16, 112)], 1, part=pw)
    return []


V = Variant('ghost', 'ghost__deleted', warm=(), add={
    'wire': dict(ramp=['F058DC', 'FF98EE', 'FFD0F8'], cuts=[0.4, 0.8]),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
