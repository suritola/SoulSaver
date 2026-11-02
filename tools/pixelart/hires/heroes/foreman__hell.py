# 스킨 메뉴 초상 128칸 — foreman__hell 「지옥 공사」 (설계도: 그을린 안전모에 용암 금 · 뿔 둘 · 용암빛 조끼 · 이글거리는 눈 · 달궈진 발바닥)
from .portrait import line_cells
from .skinops import conv, box, cells
from .skinshape import Variant2 as Variant
from .skinshape import poly


def shape(c, mod):
    S = c.S
    # 용암 금: 안전모에 큰 갈라진 줄 둘 (2칸 굵기)
    for pts in ([(55, 44), (57, 50), (61, 55)], [(73, 46), (71, 52)]):
        cl = line_cells(pts)
        c.set_lvl(cl + [(x + 1, y) for x, y in cl], 1, part=None, mat='lava')
    c.set_lvl(cells(60, 54, 62, 56), 2, mat='lava')
    # 뿔 둘: 안전모 옆에서 위로 휘어짐
    poly(c, 'hornL', [(9.6, 15.4), (8.0, 12.2), (7.4, 8.4), (9.8, 11.8), (11.6, 14.4)], 'horn', behind=False)
    poly(c, 'hornR', [(22.4, 15.4), (24.0, 12.2), (24.6, 8.4), (22.2, 11.8), (20.4, 14.4)], 'horn', behind=False)
    # 안전화 발바닥 용암빛
    conv(c, box(c, 0, 121, S, 125), 'lava', parts=['boot_l', 'boot_r'], lmap=[0, 0, 0, 0])
    # 용암 줄: 소매 · 바지 · 허리띠에 갈라진 금
    for pts in ([(30, 80), (33, 86), (31, 91)], [(88, 82), (91, 88)], [(50, 105), (52, 108)]):
        cl = line_cells(pts)
        c.set_lvl(cl + [(x + 1, y) for x, y in cl], 1, mat='lava')
    c.set_lvl(cells(36, 62, 42, 66), 1, mat='lava')
    return []


V = Variant('foreman', 'foreman__hell', warm=('hornL', 'hornR'), auto=False, ramps={
    'hat': ['5A4C58', '7C6A76', 'A8929E', 'DCC8D0'],
    'skin': ['6A4030', '8A5238', 'C88460', 'E8A078'],
    'stache': ['2A1812', '4A2A20'],
    'vest': ['B02808', 'FF5A18', 'FFC040'],
    'stripe': ['140C0A', '1E100C', '3A1810'],
    'sleeve': ['342A34', '4A3C46', '6A5A66'],
    'pants': ['241412', '3A2420', '4A2C28'],
    'boot': ['1E100C', '3A2018', '5A3424', '7A4A34'],
}, add={
    'lava': dict(ramp=['B02808', 'FF5A18', 'FFC040'], cuts=[0.5, 0.9], warm=True),
    'horn': dict(ramp=['B02808', 'FF5A18', 'FFC040'], cuts=[0.45, 0.85], warm=True),
}, glob={'EYE': 'FFE060'}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
