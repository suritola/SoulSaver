# 스킨 메뉴 초상 128칸 — cartographer__starmap 「별지도꾼」 (설계도: 밤하늘 뾰족 모자(꼭대기 y8) · 별 · 어깨 뒤로 흐르는 망토 x8~9 · x22~23)
from .skin import Variant
from .skinshape import poly


def shape(c, mod):
    poly(c, 'peak', [(13, 13.6), (17, 13.6), (16, 8.6), (15, 7.2), (14, 8.6)], 'hat', behind=True)
    poly(c, 'capeL', [(7, 21), (10, 21), (10, 30), (7, 29)], 'coat')
    poly(c, 'capeR', [(22, 21), (25, 21), (25, 29), (22, 30)], 'coat')
    return []


V = Variant('cartographer', 'cartographer__starmap', shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
