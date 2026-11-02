# 스킨 메뉴 초상 128칸 — foreman__night 「야간 작업」 (설계도: 흰 안전모 + 이마 전등 · 연두 형광 조끼 + 은색 반사 줄 · 바짓단 반사띠)
from .skinops import conv, box, cells
from .skinshape import Variant2 as Variant
from .skinshape import rect


def shape(c, mod):
    rect(c, 'lamp', 13.0, 13.2, 19.0, 15.4, 'sleeve', behind=False)
    c.set_lvl(cells(56, 52, 72, 56), 0, mat='lamplight')
    c.set_lvl(cells(60, 53, 68, 55), 1, mat='lamplight')
    conv(c, box(c, 0, 108, c.S, 113), 'stripe', parts=['legs'], lmap=[2, 2, 2, 2])       # 바짓단 반사띠
    # 소매 · 모자 반사띠 (은색 줄 — 큰 덩어리)
    conv(c, box(c, 28, 76, 46, 80), 'stripe', parts=['upper_l'], lmap=[1, 2, 2, 2])
    conv(c, box(c, 28, 69, 44, 72), 'stripe', parts=['fore_l'], lmap=[1, 2, 2, 2])
    conv(c, box(c, 86, 84, 98, 88), 'stripe', parts=['arm_r'], lmap=[1, 2, 2, 2])
    conv(c, box(c, 36, 58, 92, 60), 'stripe', parts=['brim'], lmap=[1, 2, 2, 2])
    return []


V = Variant('foreman', 'foreman__night', warm=(), auto=False, ramps={
    'hat': ['848CA8', 'B4BAD0', 'ECEFF6', 'FFFFFF'],
    'vest': ['78A810', 'B8F020', 'E8FF80'],
    'stripe': ['6E7488', 'A0A8B8', 'C4CAD8'],
}, add={
    'lamplight': dict(ramp=['FFF0A8', 'FFFFFF'], cuts=[0.5]),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
