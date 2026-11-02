# 스킨 메뉴 초상 128칸 — clockmaker__apprentice 「견습」 (설계도: 회색 가죽 앞치마 · 오른쪽 허리께 쇠 톱니 x21~24 · y23~26)
import numpy as np

from .skinshape import Variant2 as Variant
from .skinshape import ell


def shape(c, mod):
    ell(c, 'cog', 22.0, 25.5, 3.0, 2.6, 'cogm', behind=False)
    body = np.isin(c.mat, [c.mi('vest'), c.mi('shirt')])

    def cells(x0, x1, y0, y1):
        return [(x, y) for y in range(y0, y1) for x in range(x0, x1) if body[y, x]]
    c.set_lvl(cells(48, 80, 85, 108), 2, mat='apron')                    # 앞치마 바탕
    c.set_lvl(cells(48, 80, 85, 89), 0, mat='apron')                     # 허리끈
    for (x0, x1, y0, y1) in ((52, 76, 93, 96), (52, 56, 93, 105), (72, 76, 93, 105), (52, 76, 101, 105)):      # 주머니 테두리
        c.set_lvl(cells(x0, x1, y0, y1), 1, mat='apron')
    for x0 in (56, 70):                                                 # 허리끈 아래 단추
        c.set_lvl(cells(x0, x0 + 3, 89, 92), 1, mat='apron')
    return []


V = Variant('clockmaker', 'clockmaker__apprentice', add={
    'apron': dict(ramp=['3A3C4A', '6E7488', 'A4AABC'], cuts=[0.4, 0.8]),
    'cogm': dict(ramp=['4A5262', '7E8C9C', 'B8CCD8'], cuts=[0.55, 0.95], warm=True),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
