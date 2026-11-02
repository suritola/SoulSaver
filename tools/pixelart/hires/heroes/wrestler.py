# 씨름꾼 메뉴 초상 128칸 (새 영웅 · 도적 규칙(README.md) 그대로)
#   설계도 = Resources/Characters/wrestler.png 첫 그림(32칸 · 몸 20×21) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 11~13 짧은 머리(짙은 보라) · 15 굵은 눈썹(x13~14 · x17~18) · 16 눈(x14 · x17) · 17 웃는 입(x15~16)
#     18~19 어깨가 두 배로 넓은 역사다리꼴(살빛 · 윗면 밝음) · 20~23 맨 가슴(아래 가슴선) · 24~26 샅바 띠(빨강 · 파랑 꼰 띠)
#     27~30 무릎 굽혀 벌린 다리 · 31 발 · 팔은 양옆으로 내려 24~25 에서 주먹이 띠 끝을 쥠
#   광원 = 위 · 살짝 오른쪽 (어깨 윗면 · 오른 가슴 · 오른 다리가 밝음)
#   명도 계층: 눈 · 땀 반짝 > 살 윗면 > 샅바 > 머리 > 살 그늘
import numpy as np
from scipy import ndimage

from .portrait import Canvas, Map, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, n_ellipsoid, n_cyl_v, n_cyl_h, despeckle

SIZE = 128
K = 4
M = Map(K, 0, -3)
DESIGN = 'Assets/Resources/Characters/wrestler.png'     # 시트 첫 칸 (32×32) 이 설계도

MATERIALS = {
    # 살: 바탕 #CE845E (설계도 a) · 그늘 #965448 (설계도 c) · 빛 #F0B080 (설계도 d)
    'skin':  dict(ramp=[hx('84443E'), hx('A85C4C'), hx('CE845E'), hx('E8A474'), hx('F6C496')], cuts=[0.40, 0.66, 0.92, 1.10], warm=True),
    'hair':  dict(ramp=[hx('32243F'), hx('453658'), hx('64528A')], cuts=[0.45, 0.85]),
    'red':   dict(ramp=[hx('6A1030'), hx('A81C34'), hx('D83A48'), hx('F26A5E')], cuts=[0.40, 0.70, 1.00], warm=True),
    'blue':  dict(ramp=[hx('2A2E80'), hx('3C48C8'), hx('5A6BFF'), hx('A0AEFF')], cuts=[0.40, 0.70, 1.00]),
    'brow':  dict(ramp=[hx('3E1C26'), hx('5A3030')], cuts=[0.5], warm=True),
}
EYE = hx('120B20')
EYE_HOT = hx('F4F0FF')
SWEAT = hx('E8F4FF')
LIGHT = (0.12, -0.8, 0.6)
RIM_DIR = (0.4, -0.92)


def n_belt(cy, r):
    return n_cyl_h(cy, r, 0.1)


def draw():
    c = Canvas(SIZE, MATERIALS, light=LIGHT)
    S = SIZE

    # ── 1 다리 (설계도 y27~30 — 무릎 굽혀 벌린 자세 · 발끝이 바깥으로)
    leg_l = poly_mask(path((42, 104), (64, 104), (63, 112), [(60, 118), (57, 121)], (56, 124.5), (32, 124.5), (31.5, 120.5), [(33, 116), (36, 111)]), S)
    leg_r = poly_mask(path((64, 104), (86, 104), [(92, 110), (95.5, 117), (96, 121)], (96, 124.5), (72, 124.5), (71, 121), (65, 113)), S)
    p_legl = c.part('leg_l', leg_l, 'skin', n_cyl_v(46, 15, 0.1), bias=-0.12)
    p_legr = c.part('leg_r', leg_r, 'skin', n_cyl_v(80, 15, 0.1), bias=-0.04)

    # ── 2 몸통 (어깨가 넓은 역사다리꼴 — 설계도 y18~26)
    torso = poly_mask(path((50, 69), (78, 69), [(88, 70), (90, 75)], (88, 84), (84, 97), (83, 105), (45, 105), (44, 97), (40, 84), (38, 75), [(38, 70), (50, 69)]), S)
    p_torso = c.part('torso', torso, 'skin', n_cyl_v(64, 24, 0.15), bias=-0.08)
    neck = poly_mask(path((55, 64), (73, 64), (75, 74), (53, 74)), S)
    p_neck = c.part('neck', neck, 'skin', n_plane(0.0, -0.1, 0.9), bias=-0.30)

    # ── 3 팔 (양옆 · 팔꿈치가 바깥 · 주먹이 샅바 끝을 쥠)
    arm_l = poly_mask(path((46, 74), (30, 74), [(26, 76), (25, 84)], [(24, 92), (24.5, 98)], (42, 99), (44, 92), (46, 84)), S)
    arm_r = poly_mask(path((82, 74), (98, 74), [(102, 76), (103, 84)], [(104, 92), (103.5, 98)], (86, 99), (84, 92), (82, 84)), S)
    p_arml = c.part('arm_l', arm_l, 'skin', n_cyl_v(36, 12), bias=-0.12)
    p_armr = c.part('arm_r', arm_r, 'skin', n_cyl_v(92, 12), bias=0.0)
    # 어깨 근육 (둥근 윗면 — 빛을 받는 곳)
    p_deltl = c.part('delt_l', ellipse_mask(37, 78, 11, 9, S), 'skin', n_ellipsoid(37, 78, 11, 9), bias=-0.04)
    p_deltr = c.part('delt_r', ellipse_mask(91, 78, 11, 9, S), 'skin', n_ellipsoid(91, 78, 11, 9), bias=0.04)

    # ── 4 가슴 (설계도 y20~22 — 둘로 갈라진 큰 가슴 · 아래에 가슴선)
    p_pecl = c.part('pec_l', ellipse_mask(54, 79, 12.5, 9.5, S) & torso, 'skin', n_ellipsoid(54, 79, 12.5, 9.5), bias=-0.05)
    p_pecr = c.part('pec_r', ellipse_mask(74, 79, 12.5, 9.5, S) & torso, 'skin', n_ellipsoid(74, 79, 12.5, 9.5), bias=0.03)
    p_belly = c.part('belly', poly_mask(path((48, 87), (80, 87), (82, 97), (46, 97)), S) & torso, 'skin', n_plane(0.1, -0.35, 0.9), bias=-0.12)

    # ── 5 샅바 띠 (설계도 y24~26 — 허리를 감은 빨강 · 파랑 꼰 띠) + 앞 매듭 + 허벅지 감은 자락
    belt = poly_mask(path((38, 91), (90, 91), (92, 99), (88, 105), (40, 105), (36, 99)), S)
    yy, xx = c.yy, c.xx
    seg = (((xx + (yy - 91) * 0.9) // 11).astype(int) % 2) == 0
    p_beltr = c.part('belt_r', belt & seg, 'red', n_belt(98, 8), bias=0.0)
    p_beltb = c.part('belt_b', belt & ~seg, 'blue', n_belt(98, 8), bias=0.0)
    tail_r = poly_mask(path((57, 104), (65, 104), (64, 115), (59, 117)), S)
    tail_b = poly_mask(path((63, 104), (72, 104), (72, 113), (67, 117)), S)
    p_tailr = c.part('tail_r', tail_r, 'red', n_plane(0.1, -0.3, 0.9), bias=-0.08)
    p_tailb = c.part('tail_b', tail_b, 'blue', n_plane(0.1, -0.3, 0.9), bias=-0.08)
    wrap_l = poly_mask(path((33, 108), (63, 108), (62, 114), (34, 114)), S) & leg_l
    wrap_r = poly_mask(path((65, 108), (93, 108), (95, 114), (66, 114)), S) & leg_r
    p_wrapl = c.part('wrap_l', wrap_l, 'red', n_cyl_h(111, 4), bias=-0.04)
    p_wrapr = c.part('wrap_r', wrap_r, 'blue', n_cyl_h(111, 4), bias=-0.04)

    # ── 6 주먹 (설계도 y24~25 — 띠 끝을 쥔 주먹 · 손가락 골)
    p_fistl = c.part('fist_l', ellipse_mask(33, 100, 9.5, 6.5, S), 'skin', n_ellipsoid(33, 100, 10, 7), bias=-0.06)
    p_fistr = c.part('fist_r', ellipse_mask(95, 100, 9.5, 6.5, S), 'skin', n_ellipsoid(95, 100, 10, 7), bias=0.02)

    # ── 7 머리 · 얼굴 (설계도 y11~17 — 머리 폭 40칸 · 짧은 머리 · 턱이 넓음)
    head = ellipse_mask(64, 54.5, 20.5, 14, S) | poly_mask(path((44, 57), (84, 57), (82, 63), (77, 68.5), (51, 68.5), (46, 63)), S)
    p_face = c.part('face', head, 'skin', n_plane(0.0, -0.45, 0.88), bias=0.0)
    c.lvl[head] = 2
    hair = head & (yy < 58) & ((yy < 52.5) | (np.abs(xx - 64) > 12.5) | (yy < 54.5) & (np.abs(xx - 64) > 7))
    hair &= ellipse_mask(64, 54.5, 20.5, 14, S)
    p_hair = c.part('hair', hair, 'hair', n_ellipsoid(64, 56, 20.5, 16), bias=0.0)
    c.tidy(4)
    c.contact_lines(skip={('torso', 'pec_l'), ('torso', 'pec_r'), ('torso', 'belly'), ('pec_l', 'pec_r'), ('torso', 'delt_l'),
                          ('torso', 'delt_r'), ('arm_l', 'delt_l'), ('arm_r', 'delt_r'), ('torso', 'neck'), ('arm_l', 'torso'),
                          ('arm_r', 'torso'), ('belt_r', 'belt_b'), ('leg_l', 'wrap_l'), ('leg_r', 'wrap_r'), ('torso', 'belt_r'),
                          ('torso', 'belt_b'), ('belly', 'belt_r'), ('belly', 'belt_b'), ('face', 'hair'), ('torso', 'face'),
                          ('neck', 'face'), ('leg_l', 'belt_r'), ('leg_l', 'belt_b'), ('leg_r', 'belt_r'), ('leg_r', 'belt_b'),
                          ('leg_l', 'tail_r'), ('leg_r', 'tail_b'), ('leg_l', 'tail_b'), ('leg_r', 'tail_r'),
                          ('belt_r', 'fist_l'), ('belt_r', 'fist_r'), ('belt_b', 'fist_l'), ('belt_b', 'fist_r'),
                          ('arm_l', 'fist_l'), ('arm_r', 'fist_r'), ('arm_l', 'belt_r'), ('arm_l', 'belt_b'),
                          ('arm_r', 'belt_r'), ('arm_r', 'belt_b'), ('tail_r', 'tail_b')})

    # ───────────── 덧그림 (큰 형태 위주)
    def keep_in(cells, part):
        return [(x, y) for x, y in cells if 0 <= x < S and 0 <= y < S and c.pid[y, x] == part]

    # 얼굴: 윗면 빛 · 왼 턱선 · 턱 아래 그늘
    fm = c.pid == p_face
    fin = ndimage.binary_erosion(fm, iterations=2)
    jaw = fm & ~fin & (((xx < 57) & (yy > 62)) | (yy > 66))
    c.nudge([(int(x), int(y)) for y, x in zip(*np.nonzero(jaw))], -1, p_face)
    lit = fm & ellipse_mask(74, 62, 8.5, 6.0, S) & (c.lvl == 2)
    c.lvl[lit] = 3
    # 머리: 위 · 오른쪽 빛 한 덩어리 · 가르마 쪽 그늘
    c.set_lvl([(x, y) for y in range(42, 48) for x in range(60, 80) if c.pid[y, x] == p_hair and (x - 72) ** 2 / 90 + (y - 42) ** 2 / 14 < 1], 2, p_hair)
    # 굵은 눈썹: 짙게 두 줄 (설계도 x13~14 · x17~18)
    for ex in (50, 68):
        c.stamp(['aaaaaaaa', 'aaaaaaaa', '.aaaaaa.'], ex, 55, {'a': ('brow', 0)}, part=p_face)
    # 입: 웃는 입 (설계도 x15~16 · y17) — 입꼬리가 올라감
    MD, TT = hx('5A1A28'), hx('F4F0FF')
    c.stamp(['a...............a', '.aaaaaaaaaaaaaaa.', '..aTTTTTTTTTTTa..'], 56, 64, {'a': MD, 'T': TT}, part=p_face)

    # 몸: 가슴 아래 그늘선 (큰 호 둘 · 설계도 y22 가슴선) · 가슴 가운데 골 · 어깨 윗면 빛
    for cx in (54, 74):
        arc = line_cells([(cx - 11, 84), (cx - 6, 88), (cx, 89), (cx + 6, 88), (cx + 11, 84)])
        c.nudge(arc + [(x, y + 1) for x, y in arc], -1, (p_pecl, p_pecr, p_belly, p_torso))
    c.nudge(line_cells([(64, 72), (64, 90)]) + line_cells([(63, 74), (63, 88)]), -1, (p_pecl, p_pecr, p_torso))
    c.nudge(line_cells([(40, 70), (52, 69)]) + line_cells([(76, 69), (90, 70)]), 1, (p_torso, p_deltl, p_deltr))
    # 배: 가운데 세로 골 + 가로 골 하나 (큰 줄 · 잔주름 아님)
    c.nudge(line_cells([(64, 90), (64, 96)]) + line_cells([(63, 90), (63, 96)]), -1, p_belly)
    c.nudge([(x, 93) for x in range(51, 78)] + [(x, 94) for x in range(51, 78)], -1, p_belly)
    # 팔: 이두 불룩(빛 쪽 긴 줄) · 팔꿈치 그늘 · 안쪽 그늘 줄
    c.nudge(line_cells([(31, 84), (30, 92)]) + line_cells([(32, 84), (31, 92)]), 1, p_arml)
    c.nudge(line_cells([(97, 84), (98, 92)]) + line_cells([(98, 84), (99, 92)]), 1, p_armr)
    c.nudge(line_cells([(43, 80), (42, 94)]) + line_cells([(44, 80), (43, 94)]), -1, p_arml)
    c.nudge(line_cells([(84, 80), (85, 94)]) + line_cells([(85, 80), (86, 94)]), -1, p_armr)
    # 다리: 무릎 둘레 · 두 다리 사이 골 · 오른 다리 빛 (설계도 d) · 종아리 불룩
    c.nudge(line_cells([(61, 114), (59, 122)]) + line_cells([(62, 114), (60, 122)]), -1, p_legl)
    c.nudge(line_cells([(67, 116), (71, 122)]) + line_cells([(68, 116), (72, 122)]), -1, p_legr)
    c.nudge(line_cells([(88, 112), (93, 120)]) + line_cells([(89, 112), (94, 120)]), 1, p_legr)
    c.nudge(line_cells([(37, 114), (35, 120)]) + line_cells([(38, 114), (36, 120)]), 1, p_legl)
    c.nudge([(x, y) for y in (122, 123) for x in range(30, 98)], -1, (p_legl, p_legr))          # 발 그늘
    # 샅바: 띠 아래 그늘 · 띠 위 빛 (둥근 띠)
    c.nudge([(x, 104) for x in range(38, 92)], -1, (p_beltr, p_beltb))
    # 주먹: 손가락 골 하나 · 엄지 · 손 윗면 빛
    for fx_, sgn in ((26, 1), (94, 1)):
        c.nudge(line_cells([(fx_ + 3, 98), (fx_ + 3, 104)]) + line_cells([(fx_ + 8, 98), (fx_ + 8, 104)]), -1, (p_fistl, p_fistr))
        c.nudge(line_cells([(fx_ + 1, 96), (fx_ + 12, 95)]), 1, (p_fistl, p_fistr))

    # 림 라이트: 오른쪽 위
    c.rim((p_hair, p_deltr, p_armr, p_torso), RIM_DIR, width=2, level=-1, thresh=0.3)
    c.rim((p_legr, p_fistr, p_wrapr), RIM_DIR, width=1, by=1, thresh=0.5)
    keep = np.isin(c.pid, (p_fistl, p_fistr))
    c.absorb((c.pid >= 0) & ~keep, min_size=3)

    # 눈 (짙은 눈동자 · 오른 위 반짝 1칸 — 설계도 x14 · x17 → 56 · 68)
    eye = ['aab', 'aaa', 'aaa', '.a.']
    for ex in (56, 68):
        c.stamp(eye, ex, 60, {'a': EYE, 'b': EYE_HOT})
    # 땀 반짝 두 점 (어깨 윗면)
    for (sx, sy) in ((40, 73), (88, 74)):
        c.stamp(['aa', 'aa'], sx, sy, {'a': SWEAT})
    dots = [(58, 60), (70, 60), (40, 73), (88, 74), (41, 73), (89, 74), (40, 74), (88, 75), (41, 74), (89, 75)]
    c.outline(warm_parts=(p_fistl, p_fistr, p_legl, p_legr, p_arml, p_armr, p_torso, p_face, p_neck, p_deltl, p_deltr, p_pecl, p_pecr, p_belly, p_beltr, p_tailr, p_wrapl))
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
