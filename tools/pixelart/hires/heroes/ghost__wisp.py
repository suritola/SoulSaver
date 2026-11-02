# 스킨 메뉴 초상 128칸 — ghost__wisp 「도깨비불」 (설계도: 청록 몸 · 머리 위 불꽃 y7~12 · 곁의 작은 불 x6~7 · x24~26)
import numpy as np

from .skinshape import Variant2 as Variant
from .skinshape import poly, ell


def shape(c, mod):
    poly(c, 'flame', [(14, 12), (14, 9.5), (15, 8), (15.5, 7), (16, 8.5), (17.5, 10), (17.5, 12)], 'fire')
    for nm, cx, cy, rx, ry, tip in (('L', 5.5, 20.0, 2.5, 2.5, [(4.5, 19.5), (5.5, 17), (6.5, 19.5)]),
                                    ('R', 26.5, 28.0, 2.5, 2.8, [(25.5, 27.5), (26.5, 25), (27.5, 27.5)])):
        pf = ell(c, 'wisp' + nm, cx, cy, rx, ry, 'fire', behind=False)
        poly(c, 'tip' + nm, tip, 'fire', behind=False)
        ys, xs = np.nonzero(c.pid == pf)
        c.set_lvl([(x, y) for x, y in zip(xs, ys) if abs(x - xs.mean()) < 3 and y > ys.mean()], 1, part=pf)
    return []


V = Variant('ghost', 'ghost__wisp', warm=(), add={
    'fire': dict(ramp=['1E8A78', '6FE8C8', 'E8FFF8'], cuts=[0.45, 0.9]),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
