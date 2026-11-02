# 스킨 메뉴 초상 128칸 — wrestler__referee 「심판」 (설계도: 흑백 세로 줄무늬 심판복(짧은 소매) · 검은 챙 모자 + 흰 띠 · 호루라기 · 검은 · 흰 샅바)
from .portrait import ellipse_mask
from .skinops import conv, box, cells
from .skinshape import Variant2 as Variant
from .skinshape import poly, rect


def shape(c, mod):
    S = c.S
    xx = c.xx
    body = ['torso', 'pec_l', 'pec_r', 'belly', 'neck']
    shirt = box(c, 46, 71, 83, 90) & ~ellipse_mask(64, 70, 7, 8, S)
    sleeve = box(c, 0, 71, S, 92)
    stripe_k = ((xx.astype(int) // 4) % 2 == 0)
    for names, region in ((body, shirt), (['arm_l', 'arm_r', 'delt_l', 'delt_r'], sleeve)):
        conv(c, region & ~stripe_k, 'white', parts=names, lmap=[0, 1, 2, 2, 2])
        conv(c, region & stripe_k, 'black', parts=names, lmap=[0, 1, 2, 2, 2])
    # 검은 챙 모자: 둥근 윗면 + 흰 띠 + 금 휘장 + 오른쪽으로 나온 챙
    poly(c, 'cap', [(11.2, 13.0), (10.8, 10.6), (12.2, 8.4), (14.2, 7.2), (17.8, 7.2), (19.8, 8.4), (21.2, 10.6), (20.8, 13.0)], 'black', behind=False)
    rect(c, 'capband', 11.0, 12.6, 21.0, 13.7, 'white', behind=False)
    poly(c, 'visor', [(11.0, 13.7), (23.4, 13.7), (23.2, 14.8), (11.4, 14.6)], 'black', behind=False)
    rect(c, 'badge', 15.0, 9.4, 17.0, 11.0, 'gold', behind=False)
    # 호루라기: 입에 문 금빛 (가슴 끈은 검은 줄)
    rect(c, 'whistle', 14.8, 16.9, 17.6, 17.7, 'gold', behind=False)
    c.set_lvl(cells(56, 77, 59, 85), 0, mat='black')
    return []


V = Variant('wrestler', 'wrestler__referee', warm=('visor', 'badge', 'whistle'), auto=False, ramps={
    'red': ['14121C', '23202E', '3A3646', '54506A'],
    'blue': ['8E8EA6', 'C0C0D4', 'F4F4F8', 'FFFFFF'],
}, add={
    'white': dict(ramp=['9A9AB4', 'D0D0E0', 'F8F8FC'], cuts=[0.5, 0.9]),
    'black': dict(ramp=['14121C', '23202E', '3E3A52'], cuts=[0.5, 0.9]),
    'gold': dict(ramp=['B8860B', 'F0C040', 'FFE890'], cuts=[0.5, 0.9], warm=True),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
