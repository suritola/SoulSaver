# 스킨 메뉴 초상 128칸 — wrestler__crypt 「묘역의 장사」 (설계도: 잿빛 살 · 흰 머리 · 해골 샅바 · 어깨 뼈 뿔 둘 · 초록 눈빛)
from .skinops import conv, box, cells
from .skinshape import Variant2 as Variant
from .skinshape import poly, ell


def shape(c, mod):
    # 어깨 뼈 뿔 (바깥 위로 휘어짐) — 어깨 위에 닿음
    poly(c, 'hornL', [(8.4, 19.4), (10.6, 19.4), (9.6, 17.4), (8.8, 16.0), (7.2, 14.6), (7.6, 17.0)], 'bone', behind=False)
    poly(c, 'hornR', [(21.4, 19.4), (23.6, 19.4), (24.6, 17.0), (24.8, 14.6), (23.2, 16.0), (22.4, 17.4)], 'bone', behind=False)
    # 해골 샅바 장식 (띠 가운데) — 눈구멍 · 코 · 이빨
    ell(c, 'skull', 16.0, 24.5, 2.5, 2.2, 'bone', behind=False)
    c.set_lvl(cells(60, 92, 63, 95) + cells(66, 92, 69, 95), 0, mat='bonedark')
    c.set_lvl(cells(64, 95, 65, 97), 0, mat='bonedark')
    c.set_lvl(cells(61, 98, 68, 99), 1, mat='bonedark')
    return []


V = Variant('wrestler', 'wrestler__crypt', warm=None, auto=False, ramps={
    'skin': ['4A4260', '6A6082', '9C92A8', 'C8C0DA', 'E8E4F4'],
    'hair': ['9A96B0', 'DAD6E8', 'FFFFFF'],
    'red': ['8A8068', 'C8BEA4', 'F4EEDA', 'FFFAE6'],
    'blue': ['2A1A44', '3E2660', '5A3A82', '9A72C8'],
    'brow': ['3A2E4A', '5A4A72'],
}, add={
    'bone': dict(ramp=['8A8068', 'C8BEA4', 'F4EEDA'], cuts=[0.5, 0.9], warm=True),
    'bonedark': dict(ramp=['2A1E3C', '5A4A72'], cuts=[0.5]),
}, glob={'EYE': '7CFFB0'}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
