# 스킨 메뉴 초상 128칸 — postman__pigeon 「비둘기 조련사」 (설계도: 올리브 베레 · 잿빛 외투 · 모자 위 비둘기 y9~14)
from .skinshape import Variant2 as Variant
from .skinshape import poly, ell, tune


def shape(c, mod):
    ell(c, 'pbody', 16.0, 12.0, 3.4, 2.2, 'dark', behind=False)
    ell(c, 'phead', 19.2, 10.4, 1.9, 1.6, 'dark', behind=False)
    poly(c, 'ptail', [(11.8, 12.8), (13.5, 11), (13.5, 13.6)], 'dark', behind=False)
    poly(c, 'pbeak', [(20.6, 10), (22, 10.8), (20.6, 11.5)], 'leather', behind=False)
    return []


V = Variant('postman', 'postman__pigeon', warm=(), add={
    'bird': dict(ramp=['6A7488', 'B4BED0'], cuts=[0.5]),
}, shape=shape)
tune(V, dark=1.25)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
