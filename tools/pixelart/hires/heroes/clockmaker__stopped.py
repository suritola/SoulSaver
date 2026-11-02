# 스킨 메뉴 초상 128칸 — clockmaker__stopped 「멈춘 시계의 주인」 (설계도: 흑백 몸 · 머리 뒤 깨진 시계판 중심 (15.5, 11.5) 반지름 7 · 유리 조각)
import math

from .portrait import line_cells
from .skinshape import tune, Variant2 as Variant
from .skinshape import ell, rect


def shape(c, mod):
    pd = ell(c, 'dial', 15.5, 11.5, 7.2, 7.2, 'shirt')
    rect(c, 'shardA', 24, 7, 25.4, 8.4, 'metal', behind=False)
    rect(c, 'shardB', 25, 10, 27, 11.4, 'metal', behind=False)
    rect(c, 'shardC', 4, 6, 6.9, 7.4, 'metal', behind=False)
    ring = [(x, y) for y in range(8, 80) for x in range(30, 100)
            if 24.5 <= math.hypot(x + 0.5 - 62, y + 0.5 - 43) <= 28.5 and c.pid[y, x] == pd]
    c.set_lvl(ring, 0, part=pd)
    mid = [(x, y) for y in range(8, 80) for x in range(30, 100)
           if 15.5 <= math.hypot(x + 0.5 - 62, y + 0.5 - 43) < 19.5 and c.pid[y, x] == pd]
    c.set_lvl(mid, 1, part=pd)
    for r0, r1, lv in ((0, 4, 0), (4, 9.5, 1), (9.5, 15.5, 2)):
        c.set_lvl([(x, y) for y in range(8, 80) for x in range(30, 100)
                   if r0 <= math.hypot(x + 0.5 - 62, y + 0.5 - 43) < r1 and c.pid[y, x] == pd], lv, part=pd)                                    # 테두리 한 줄 (넓은 한 색 면 나눔)
    # 11:59 — 시침(11)은 짧게 · 분침(59)은 길게 (2칸 굵기), 열두 시각 표시 2×2
    cx, cy = 62, 43
    for ang, ln in ((-30, 15), (-6, 23)):
        a = math.radians(ang)
        tip = (cx + math.sin(a) * ln, cy - math.cos(a) * ln)
        cells = line_cells([(cx, cy), tip])
        c.set_lvl(cells + [(x + 1, y) for x, y in cells], 0, part=pd)
    crack = line_cells([(80, 18), (74, 24), (76, 30), (70, 36)])           # 금
    c.set_lvl(crack, 0, part=pd)
    for k in range(12):
        a = k * math.pi / 6
        x, y = int(round(cx + math.sin(a) * 25)), int(round(cy - math.cos(a) * 25))
        c.set_lvl([(x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)], 0, part=pd)
    return []


V = Variant('clockmaker', 'clockmaker__stopped', shape=shape)
tune(V, shirt=0.9)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
