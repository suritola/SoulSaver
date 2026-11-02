# 스킨 메뉴 초상 128칸 — postman__courier 「택배 기사」 (설계도: 갈색 모자 · 형광 주황 조끼 · 왼쪽 종이 상자 x3~10 · y22~28 · 오른손 단말기 x22~25)
from .skinshape import Variant2 as Variant
from .skinshape import rect, tune


def shape(c, mod):
    pb = rect(c, 'box', 9, 23, 20, 29, 'card', behind=False)
    c.set_lvl([(x, y) for y in range(86, 118) for x in range(34, 82) if c.pid[y, x] == pb], 1, part=pb)
    c.set_lvl([(x, y) for y in range(86, 118) for x in range(52, 60) if c.pid[y, x] == pb], 2, part=pb)         # 테이프
    for (x0, x1) in ((34, 52), (60, 81)):                                                  # 송장 · 바코드 줄
        for y0 in (96, 104):
            c.set_lvl([(x, y) for y in range(y0, y0 + 2) for x in range(x0, x1) if c.pid[y, x] == pb], 0, part=pb)
    ps = rect(c, 'scanner', 20, 26, 22.6, 30.4, 'card', behind=False)
    c.set_lvl([(x, y) for y in range(101, 119) for x in range(80, 92) if c.pid[y, x] == ps], 0, part=ps)
    return []


V = Variant('postman', 'postman__courier', warm=(), ramps={
    'uniform': ['6E2E04', 'A84E10', 'E07818', 'F89A38', 'F89A38'],
}, add={
    'card': dict(ramp=['7A5A30', 'C8A468', 'E8D8A8'], cuts=[0.4, 0.9], warm=True),
}, shape=shape)
tune(V, hat=1.25)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
