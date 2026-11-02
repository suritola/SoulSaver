# 스킨 메뉴 초상 128칸 — cartographer__drafter 「제도사」 (설계도 = 32칸 첫 칸: 올리브 모자 · 카키 조끼 · 등 뒤 도면통 x8~9 · y19~25)
from .skin import Variant
from .skinshape import rect, tune


def shape(c, mod):
    rect(c, 'tube', 7, 19, 10.4, 26, 'leather')
    return []


V = Variant('cartographer', 'cartographer__drafter', shape=shape)
tune(V, hat=1.3, coat=0.88, pants=0.6, leather=0.7)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
