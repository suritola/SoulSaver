# 스킨 메뉴 초상 128칸 — wrestler__local 「동네 장사」 (설계도: 흰 러닝셔츠(빨강 단) · 운동회 머리띠(흰 + 빨강, 오른쪽으로 날리는 끝))
import numpy as np

from .portrait import ellipse_mask
from .skinops import conv, box, cells
from .skinshape import Variant2 as Variant
from .skinshape import poly


def shape(c, mod):
    S = c.S
    yy, xx = c.yy, c.xx
    body = ['torso', 'pec_l', 'pec_r', 'belly', 'neck']
    shirt = box(c, 47, 71, 82, 90) & ~ellipse_mask(64, 70, 10, 9, S)
    shirt |= box(c, 46, 71, 50, 76) | box(c, 78, 71, 82, 76)                     # 어깨끈
    conv(c, shirt, 'shirt', parts=body, lmap=[0, 0, 1, 2, 2])
    conv(c, box(c, 47, 87, 82, 90), 'hem', parts=body, lmap=[0, 1, 1, 2, 2])      # 빨간 단
    # 머리띠: 흰 바탕 + 빨강 마디 둘 · 오른쪽 끝이 날림
    head = np.isin(c.pid, [i for i, p in enumerate(c.parts) if p['name'] in ('hair', 'face')])
    band = head & (yy >= 50) & (yy < 55)
    conv(c, band, 'shirt', lmap=[1, 1, 1, 2, 2])
    conv(c, band & (((xx >= 50) & (xx < 58)) | ((xx >= 70) & (xx < 78))), 'hem', lmap=[1, 1, 1, 2, 2])
    poly(c, 'tailA', [(21, 12.1), (23.4, 11.4), (23.9, 12.6), (22.2, 12.9), (21, 13.2)], 'hem', behind=True)
    poly(c, 'tailB', [(21, 13.0), (22.6, 13.3), (23.2, 14.4), (21.8, 14.2)], 'shirt', behind=True)
    return []


V = Variant('wrestler', 'wrestler__local', warm=None, auto=False, add={
    'shirt': dict(ramp=['A8ACC8', 'DCE0F0', 'FFFFFF'], cuts=[0.5, 0.9]),
    'hem': dict(ramp=['9A1A30', 'D03C50', 'F26A74'], cuts=[0.5, 0.9], warm=True),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
