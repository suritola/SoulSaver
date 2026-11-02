# 스킨 메뉴 초상 128칸 — foreman__snow 「제설차」 (설계도: 눈 쌓인 안전모 · 빨간 목도리(늘어진 끝) · 얼음빛 조끼 · 서리 낀 콧수염 · 어깨에 쌓인 눈)
import numpy as np

from .skinops import conv, box, cells
from .skinshape import Variant2 as Variant
from .skinshape import poly, ell


def shape(c, mod):
    S = c.S
    yy, xx = c.yy, c.xx
    edge = 52 + 3.0 * np.sin(xx * 0.28)                          # 눈 덮개 아랫선 (물결)
    conv(c, (yy < edge) & (yy < 60), 'snow', parts=['dome'], lmap=[0, 0, 1, 1])
    poly(c, 'pile', [(12.8, 11.6), (13.2, 10.0), (14.2, 8.8), (15.6, 8.1), (16.6, 8.1), (17.8, 8.8), (18.8, 10.0), (19.2, 11.6)], 'snow', behind=False)
    # 빨간 목도리: 턱 아래 한 줄 + 왼쪽 가슴으로 늘어진 끝
    poly(c, 'scarf', [(11.4, 19.4), (20.6, 19.4), (21.0, 20.8), (11.0, 20.8)], 'scarfred', behind=False)
    poly(c, 'scarftail', [(11.2, 20.8), (13.8, 20.8), (14.0, 23.8), (12.4, 24.4), (11.2, 23.6)], 'scarfred', behind=False)
    c.set_lvl(cells(48, 88, 52, 91), 0, mat='scarfred')
    ell(c, 'snowL', 10.2, 19.6, 1.8, 0.9, 'snow', behind=False)
    ell(c, 'snowR', 21.8, 19.6, 1.8, 0.9, 'snow', behind=False)
    conv(c, box(c, 28, 69, 44, 73), 'snow', parts=['fore_l'], lmap=[0, 0, 1, 1])
    conv(c, box(c, 86, 90, 98, 94), 'snow', parts=['arm_r'], lmap=[0, 0, 1, 1])
    for x0 in range(46, 84, 8):                                           # 목도리 줄무늬 (큰 마디)
        c.set_lvl(cells(x0, 77, x0 + 4, 83), 0, mat='scarfred')
    c.set_lvl(cells(48, 94, 52, 99), 0, mat='scarfred')
    conv(c, box(c, 40, 113, 52, 117), 'snow', parts=['boot_l'], lmap=[0, 0, 1, 1])
    conv(c, box(c, 72, 113, 84, 117), 'snow', parts=['boot_r'], lmap=[0, 0, 1, 1])
    conv(c, box(c, 44, 108, 54, 111), 'snow', parts=['legs'], lmap=[0, 0, 1, 1])
    for (x0, y0, nm) in ((30, 76, 'upper_l'), (31, 82, 'upper_l'), (90, 86, 'arm_r'), (43, 78, 'vest'), (80, 78, 'vest'), (34, 70, 'fore_l')):
        conv(c, box(c, x0, y0, x0 + 5, y0 + 3), 'snow', parts=[nm], lmap=[1, 1, 1, 1])
    c.nudge(cells(47, 118, 49, 123) + cells(77, 118, 79, 123) + cells(57, 112, 59, 116) + cells(69, 112, 71, 116), -1, part=None)
    c.nudge(cells(33, 74, 35, 82) + cells(92, 82, 94, 90), -1, part=None)
    return []


V = Variant('foreman', 'foreman__snow', warm=('scarf', 'scarftail', 'pile', 'snowL', 'snowR'), auto=False, ramps={
    'vest': ['2E5C98', '4C86C4', '88BCE8'],
    'stache': ['A8B8D0', 'D8E4F4'],
    'pants': ['1C264C', '2E3E70', '3E5088'],
    'sleeve': ['3E4664', '606A86', '7C86A6'],
    'boot': ['3A2410', '5C3A26', '5C3A26', '925C36'],
    'stripe': ['1A2644', '1A2644', '2A4670'],
}, add={
    'snow': dict(ramp=['9CB0D0', 'DCE8FA'], cuts=[0.6]),
    'scarfred': dict(ramp=['A82820', 'E0402E'], cuts=[0.6], warm=True),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
