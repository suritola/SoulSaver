# 스킨 메뉴 초상 128칸 — clockmaker__hourglass 「모래시계 수도사」 (설계도: 모래빛 후드 · 로브 · 어깨 위로 흘러내린 후드 자락 x10~11 · x20~21)
from .skinshape import Variant2 as Variant
from .skinshape import poly


def shape(c, mod):
    poly(c, 'hoodR', [(20, 21), (23, 21), (23, 23), (20, 23)], 'vest')
    poly(c, 'hoodL', [(9, 21), (11, 21), (11, 23), (9, 23)], 'vest')
    return []


V = Variant('clockmaker', 'clockmaker__hourglass', shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
