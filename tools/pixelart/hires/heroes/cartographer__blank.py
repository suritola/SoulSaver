# 스킨 메뉴 초상 128칸 — cartographer__blank 「백지도의 저자」 (설계도: 종이색 몸 · 빨간 교정 펜 오른쪽 x22~26 · 모자 오른 모서리의 불꽃 y10~12)
from .skin import Variant
from .skinshape import poly, tune


def shape(c, mod):
    poly(c, 'pen', [(22, 21), (24, 19), (27, 16), (27.8, 17), (24.8, 20.5), (23, 22)], 'redpen')
    poly(c, 'flame', [(17, 14.6), (20.5, 14.6), (20.5, 11), (19.5, 10), (18.5, 11.5), (17.5, 11)], 'flame2')
    return []


V = Variant('cartographer', 'cartographer__blank', add={
    'redpen': dict(ramp=['6A1822', 'B03038', 'E04850'], cuts=[0.45, 0.9]),
    'flame2': dict(ramp=['A8301C', 'E8782A', 'FFD060'], cuts=[0.45, 0.9], warm=True),
}, shape=shape)
tune(V, _all=1.14)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
