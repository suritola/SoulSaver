# 스킨 메뉴 초상 128칸 — ghost__sheet 「이불」 (설계도: 파란 체크 이불 · 머리 위 잠옷 모자 y8~13 + 방울)
from .skinshape import Variant2 as Variant
from .skinshape import poly


def shape(c, mod):
    poly(c, 'cap', [(13, 13), (14, 11), (16, 9), (18, 8), (20, 8), (20.5, 11), (18, 12.5), (18, 13)], 'capblue')
    pb = next(i for i, p in enumerate(c.parts) if p['name'] == 'body')
    for y in range(128):                                           # 큰 칸 체크: 8칸 네모를 16칸 간격으로 (서로 안 닿음) + 배경은 가로 띠 단계
        for x in range(128):
            if c.pid[y, x] != pb:
                continue
            bx, by = x // 8, (y + 3 - 48) // 6
            if bx % 2 == 0 and by % 2 == 0:
                c.set_lvl([(x, y)], 0 if y >= 82 else 1, mat='check')
            else:
                c.set_lvl([(x, y)], max(1, 6 - by // 4 - by % 3))
    return []


V = Variant('ghost', 'ghost__sheet', warm=(), add={
    'check': dict(ramp=['4A78B8', '8CB4EC'], cuts=[0.5]),
    'capblue': dict(ramp=['3A58A8', '6C8CE0', 'E8F0FF'], cuts=[0.45, 0.9]),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
