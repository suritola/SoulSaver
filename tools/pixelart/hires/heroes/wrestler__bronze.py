# 스킨 메뉴 초상 128칸 — wrestler__bronze 「청동 샅바」 (설계도: 청동 샅바 · 팔 토시 · 어깨 청동 장식 · 청록 녹)
from .skinops import conv, box, cells
from .skinshape import Variant2 as Variant


def shape(c, mod):
    S = c.S
    conv(c, box(c, 0, 84, S, 99), 'bronze', parts=['arm_l', 'arm_r'], lmap=[0, 1, 2, 3, 3])          # 팔 토시
    conv(c, box(c, 0, 0, S, 83), 'bronze', parts=['delt_l', 'delt_r'], lmap=[0, 1, 2, 3, 3])         # 어깨 청동 장식
    c.set_lvl(cells(26, 88, 30, 92) + cells(98, 89, 102, 93), 0, mat='verd')                           # 녹
    c.set_lvl(cells(26, 92, 29, 94) + cells(99, 93, 102, 95), 1, mat='verd')
    c.set_lvl(cells(35, 74, 40, 78), 1, mat='verd')
    return []


V = Variant('wrestler', 'wrestler__bronze', warm=None, auto=False, ramps={
    'red': ['5A3010', '8A5224', 'C8843C', 'F0C070'],
    'blue': ['6A3A18', 'A8682C', 'D8963E', 'F8D488'],
}, add={
    'bronze': dict(ramp=['5A3010', '8A5224', 'C8843C', 'F0C070'], cuts=[0.5, 0.8, 1.0], warm=True),
    'verd': dict(ramp=['2E6E5E', '4FA08A'], cuts=[0.5]),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
