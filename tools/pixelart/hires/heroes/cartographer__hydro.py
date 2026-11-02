# 스킨 메뉴 초상 128칸 — cartographer__hydro 「해도사」 (설계도: 남색 외투 · 흰 띠 선장 모자 · 왼쪽에 놋쇠 육분의 x5~8 · y22~25)
from .skin import Variant
from .skinshape import poly


def shape(c, mod):
    p = poly(c, 'sextant', [(4, 27.5), (6, 24), (8, 22.5), (10.5, 22.5), (10.5, 27.5)], 'brass2', behind=False)
    c.set_lvl([(x, y) for y in range(98, 105) for x in range(16, 42) if c.pid[y, x] == p], 0, part=p)           # 아래 틀 (호의 받침)
    c.set_lvl([(x, y) for y in range(92, 97) for x in range(28, 40) if c.pid[y, x] == p], 2, part=p)            # 유리 눈금판
    return []


V = Variant('cartographer', 'cartographer__hydro', add={
    'brass2': dict(ramp=['6A4818', 'B8862E', 'E0B04A'], cuts=[0.45, 0.9], warm=True),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
