# 스킨 메뉴 초상 128칸 — ghost__deepsea 「심해 유령」 (설계도: 짙은 청남 몸 · 머리 위 낚싯대 + 발광 구슬 y5~11 · 양옆 지느러미 x7~9 · x22~25)
from .skinshape import Variant2 as Variant
from .skinshape import poly, ell


def shape(c, mod):
    poly(c, 'stalk', [(15, 12), (15, 9), (16, 8), (17, 7.5), (17, 6), (18, 6), (18, 8.5), (17, 10), (16.5, 12)], 'finm')
    ell(c, 'lure', 18.5, 6.5, 1.7, 1.7, 'glowm', behind=False)
    ell(c, 'finL', 9.0, 20.5, 2.0, 2.7, 'finm')
    ell(c, 'finR', 23.4, 21.5, 2.5, 2.7, 'finm')
    return []


V = Variant('ghost', 'ghost__deepsea', warm=(), add={
    'finm': dict(ramp=['0E3048', '1E5A78'], cuts=[0.5]),
    'glowm': dict(ramp=['40B8A0', 'F8F0A0'], cuts=[0.5]),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
