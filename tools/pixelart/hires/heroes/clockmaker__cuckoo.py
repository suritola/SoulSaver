# 스킨 메뉴 초상 128칸 — clockmaker__cuckoo 「뻐꾸기 시계」 (설계도: 등 뒤 시계 집 x3~8 · y18~27 · 초록 알프스 모자(꼭대기 y11) + 깃털 x20~21 · y10~12)
from .portrait import line_cells
from .skinshape import Variant2 as Variant
from .skinshape import poly, rect


def shape(c, mod):
    pr = poly(c, 'roof', [(4, 21), (6, 18), (7.5, 17), (9, 18), (11.5, 21)], 'roof')
    ph_house = rect(c, 'house', 4, 21, 11.5, 27.5, 'wood', bias=0.5)
    rect(c, 'weightA', 6.5, 27.5, 8, 29.6, 'roof')
    rect(c, 'weightB', 8, 27.5, 9.5, 29.6, 'roof')
    ph = poly(c, 'hat', [(12, 12), (13, 11), (17, 11), (18, 12), (19, 13), (23, 15), (10, 15), (12, 13)], 'felt', behind=True)
    poly(c, 'feather', [(18.5, 13), (20, 9), (22, 9), (21, 13)], 'shirt', behind=False)
    band = [(x, y) for y in range(49, 53) for x in range(40, 90) if c.pid[y, x] == ph]       # 모자 띠
    c.set_lvl(band, 0, part=ph, mat='roof')
    for y0 in (68, 72):                                             # 지붕 널 두 줄
        c.set_lvl([(x, y) for y in range(y0, y0 + 2) for x in range(16, 48) if c.pid[y, x] == pr], 1, part=pr)
    # 집: 시계 문(둥근 판 + 바늘) · 널빤지 · 문 + 어깨 멜빵 둘
    fc = [(x, y) for y in range(80, 100) for x in range(18, 44) if (x - 30) ** 2 + (y - 89) ** 2 <= 30 and c.pid[y, x] == ph_house]
    c.set_lvl(fc, 2, mat='shirt')
    c.set_lvl(line_cells([(30, 89), (30, 84)]) + line_cells([(30, 89), (34, 91)]), 0, part=ph_house)
    c.set_lvl([(x, y) for y in range(97, 109) for x in range(16, 44) if c.pid[y, x] == ph_house and 24 <= x <= 33], 0, part=ph_house)
    for ln in (line_cells([(47, 81), (54, 101)]), line_cells([(79, 81), (72, 101)])):      # 어깨 멜빵 둘
        c.set_lvl(ln + [(x + 1, y) for x, y in ln], 0, mat='wood')
    vm = (c.mat == c.mi('vest'))                                   # 조끼 위 사선 한 줄 (넓은 한 색 면 나눔)
    c.set_lvl([(x, y) for y in range(75, 100) for x in range(40, 90) if vm[y, x] and (abs((x + y) - 138) <= 1 or abs((x + y) - 156) <= 1 or abs((x + y) - 174) <= 1)], 1)
    for (x0, y0) in ((52, 80), (74, 80), (50, 98), (76, 98), (63, 84), (58, 92), (70, 92), (54, 88), (72, 88), (64, 96), (46, 84), (80, 84), (60, 100), (68, 100), (56, 76), (72, 76), (46, 92), (82, 92)):         # 조끼 단추
        cs = [(x, y) for y in range(y0, y0 + 3) for x in range(x0, x0 + 3)]
        if all(vm[y, x] for x, y in cs):
            c.set_lvl(cs, 0)
    for x0 in (47, 75):                                             # 조끼 주머니 둘
        c.set_lvl([(x, y) for y in range(88, 94) for x in range(x0, x0 + 6) if vm[y, x]], 0)
    return []


V = Variant('clockmaker', 'clockmaker__cuckoo', add={
    'felt': dict(ramp=['2A5A34', '4C9A52'], cuts=[0.55]),
    'wood': dict(ramp=['8A6038', 'D8A468'], cuts=[0.4], warm=True),
    'roof': dict(ramp=['3A2010', '6A3A24'], cuts=[0.6], warm=True),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
