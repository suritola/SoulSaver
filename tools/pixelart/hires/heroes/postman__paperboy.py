# 스킨 메뉴 초상 128칸 — postman__paperboy 「신문 배달」 (설계도: 파란 야구 모자 · 겨자색 점퍼 · 오른손에 둘둘 만 신문 x21~28 · y22~28)
from .skinshape import Variant2 as Variant, ell, tune


def shape(c, mod):
    p = ell(c, 'news', 23.0, 26.0, 5.0, 2.6, 'letter', behind=False)
    cap = ell(c, 'newscap', 25.4, 26.0, 1.9, 2.3, 'letter', behind=False)
    c.set_lvl([(x, y) for y in range(98, 102) for x in range(80, 98) if c.pid[y, x] == p], 1, part=p)         # 글줄
    c.set_lvl([(x, y) for y in range(104, 107) for x in range(78, 96) if c.pid[y, x] == p], 1, part=p)
    c.set_lvl([(x, y) for y in range(93, 113) for x in range(96, 108) if c.pid[y, x] == cap], 0, part=cap)    # 말린 끝면
    c.set_lvl([(x, y) for y in range(98, 108) for x in range(99, 106) if c.pid[y, x] == cap], 2, part=cap)
    return []


V = Variant('postman', 'postman__paperboy', warm=(), shape=shape, ramps={
    'uniform': ['5A3C10', '8A5E18', 'B8822A', 'D8A648', 'EEC870'],
})
tune(V, _all=1.12, letter=0.8)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
