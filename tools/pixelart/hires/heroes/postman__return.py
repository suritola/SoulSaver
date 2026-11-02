# 스킨 메뉴 초상 128칸 — postman__return 「반송 우체부」 (설계도: 검은 제복 · 모자 밀랍 봉인 · 가슴 빨간 RETURN 도장 x11~19 · y23~28)
from .skinshape import Variant2 as Variant
from .skinshape import rect, tune


def shape(c, mod):
    p = rect(c, 'stamp', 12, 24, 19, 28.5, 'stampred', behind=False)
    c.set_lvl([(x, y) for y in range(93, 111) for x in range(48, 76)], 1, part=p)
    c.set_lvl([(x, y) for y in range(93, 111) for x in (61, 62)] + [(x, y) for y in (101, 102) for x in range(48, 76)], 0, part=p)
    for (x0, x1, y0, y1) in ((52, 59, 97, 101), (62, 71, 97, 101), (54, 70, 104, 107)):    # 도장 글자 덩어리
        c.set_lvl([(x, y) for y in range(y0, y1) for x in range(x0, x1)], 0, part=p)
    return []


V = Variant('postman', 'postman__return', warm=(), ramps={
    'uniform': ['2A2836', '363446', '444058', '5A5672', '5A5672'],
    'leather': ['1C1420', '34283A', '4C3C50', '685468'],
}, add={
    'stampred': dict(ramp=['6A1020', 'B03040'], cuts=[0.6], warm=True),
}, shape=shape)
tune(V, hat=1.25)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
