# 공사 반장 메뉴 초상 128칸 (새 영웅 · 도적 규칙(README.md) 그대로)
#   설계도 = Resources/Characters/foreman.png 첫 그림(32칸 · 몸 18×21) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 11~15 둥근 노랑 안전모(가운데 줄 x15~16 · 앞 챙 y16) · 17~19 얼굴(눈 x14 · x17) · 18~19 콧수염
#     20 턱 · 20~25 주황 · 검정 줄무늬 조끼(가운데 지퍼 · 소매는 회청) · 24~25 손 · 26 허리띠 + 금 버클
#     26~28 짙은 작업 바지(두 다리) · 29~30 갈색 안전화 · 31 바닥
#   메뉴 초상 묘사(영웅 문서 13절): 왼손은 안전모를 고쳐 쓰는 중 · 오른손은 삽날 자루를 어깨에 걸침 · 한쪽 눈썹 올림 · 「아 지금 하잖아요」 반쯤 열린 입
#   광원 = 위 · 살짝 오른쪽 (안전모 오른쪽 윗면 · 오른 어깨가 밝음)
#   명도 계층: 눈 > 안전모 > 조끼 주황 > 얼굴 > 소매 · 바지 > 검정 줄무늬 · 장화
import numpy as np
from scipy import ndimage

from .portrait import Canvas, Map, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, n_ellipsoid, n_cyl_v, n_cyl_h, despeckle

SIZE = 128
K = 4
M = Map(K, 0, -3)
DESIGN = 'Assets/Resources/Characters/foreman.png'      # 시트 첫 칸 (32×32) 이 설계도

MATERIALS = {
    # 안전모: 바탕 #F2E454 (설계도 c) · 그늘 #D4B228 (i) · 챙 #AA841E (p) · 빛 #FFF796 (k — 림에만)
    'hat':    dict(ramp=[hx('AA841E'), hx('D4B228'), hx('F2E454'), hx('FFF796')], cuts=[0.55, 0.92, 1.10], warm=True),
    'skin':   dict(ramp=[hx('8A5440'), hx('B26E58'), hx('E8AC80'), hx('F8CCA8')], cuts=[0.50, 0.80, 1.10], warm=True),
    'stache': dict(ramp=[hx('583428'), hx('7A4A38')], cuts=[0.9], warm=True),
    # 조끼: 주황 #F88C2C (설계도 f) · 그늘 #C45C1E (s) · 빛 #FFB858 (u)
    'vest':   dict(ramp=[hx('C45C1E'), hx('F88C2C'), hx('FFB858')], cuts=[0.62, 1.05], warm=True),
    'stripe': dict(ramp=[hx('2A2638'), hx('3C364C'), hx('524C66')], cuts=[0.55, 1.0]),
    'sleeve': dict(ramp=[hx('3E4664'), hx('606A86'), hx('7C86A6')], cuts=[0.55, 1.0]),
    'pants':  dict(ramp=[hx('2E3858'), hx('3C466A'), hx('5A6A94')], cuts=[0.55, 1.0]),
    'boot':   dict(ramp=[hx('3A2410'), hx('5C3A26'), hx('925C36'), hx('B8844E')], cuts=[0.45, 0.78, 1.10], warm=True),
}
EYE = hx('181024')
EYE_HOT = hx('F4F0FF')
LIGHT = (0.12, -0.8, 0.6)
RIM_DIR = (0.4, -0.92)


def draw():
    c = Canvas(SIZE, MATERIALS, light=LIGHT)
    S = SIZE
    yy, xx = c.yy, c.xx

    # ── 0 삽 자루 (오른쪽 어깨 뒤로 · 설계도 영역 안) — 가장 뒤
    pole = poly_mask(path((94, 66), (99, 66), (99, 106), (94, 106)), S)
    p_pole = c.part('pole', pole, 'boot', n_cyl_v(96.5, 3.2), bias=0.0)
    knob = ellipse_mask(96.5, 67, 4.2, 3.2, S)
    p_knob = c.part('knob', knob, 'boot', n_ellipsoid(96.5, 67, 4.2, 3.2), bias=0.08)

    # ── 1 안전화 (설계도 y29~30 — 갈색 · 발끝이 바깥)
    boot_l = poly_mask(path((42, 112), (59, 112), (59, 124.5), (37, 124.5), (37, 120), [(37.5, 117), (41, 115)]), S)
    boot_r = poly_mask(path((68, 112), (85, 112), [(89, 115), (90.5, 117.5)], (90.5, 124.5), (68, 124.5)), S)
    p_bootl = c.part('boot_l', boot_l, 'boot', n_plane(-0.1, -0.6, 0.8), bias=-0.30)
    p_bootr = c.part('boot_r', boot_r, 'boot', n_plane(0.3, -0.6, 0.75), bias=-0.20)

    # ── 2 바지 (설계도 y26~28 — 짙은 작업 바지 · 두 다리 사이 골)
    legs = poly_mask(path((42, 98), (86, 98), (86, 106), (84, 114), (68, 114), (65.5, 106), (62, 106), (59.5, 114), (44, 114), (42, 106)), S)
    p_legs = c.part('legs', legs, 'pants', n_cyl_v(64, 24, 0.1), bias=-0.04)

    # ── 3 조끼 몸통 (설계도 y20~25 — 상자 모양 · 주황)
    vest = poly_mask(path((41, 77), (87, 77), (88, 86), (88, 101), (40, 101), (40, 86)), S)
    p_vest = c.part('vest', vest, 'vest', n_cyl_v(64, 26, 0.2), bias=-0.06)

    # ── 4 팔: 오른팔(소매 + 자루를 쥔 손) · 왼팔(소매가 팔꿈치에서 꺾여 안전모로)
    arm_r = poly_mask(path((82, 77), (91, 78), [(95, 82), (96, 90)], (95, 98), (87, 98), (86, 88), (84, 82)), S)
    p_armr = c.part('arm_r', arm_r, 'sleeve', n_cyl_v(90, 8), bias=-0.02)
    hand_r = ellipse_mask(94.5, 98, 5.6, 5.0, S)
    p_handr = c.part('hand_r', hand_r, 'skin', n_ellipsoid(94.5, 98, 5.6, 5.0), bias=0.0)
    upper_l = poly_mask(path((46, 77), (36, 77), [(30, 80), (28.5, 86)], (29, 92), (38, 93), (42, 86), (46, 82)), S)
    p_upl = c.part('upper_l', upper_l, 'sleeve', n_cyl_v(36, 9), bias=-0.12)
    fore_l = poly_mask(path((28.5, 92), (38, 93), (41.5, 69), (31.5, 66)), S)
    p_forel = c.part('fore_l', fore_l, 'sleeve', n_cyl_v(35, 6), bias=-0.06)

    # ── 5 머리: 얼굴 (설계도 y17~20) → 콧수염 → 입
    face = poly_mask(path((45, 62), (83, 62), (83.5, 68), (82, 73), [(79, 77), (74, 78)], (54, 78), [(49, 77), (46, 73), (44.5, 68)]), S)
    p_face = c.part('face', face, 'skin', n_plane(0.0, -0.5, 0.85), bias=0.0)
    c.lvl[face] = 2
    stache = poly_mask(path((49, 70), (60, 70), (64, 71.5), (68, 70), (79, 70), (80, 75), [(77, 76), (72, 74)], (64, 74), (56, 74), [(51, 76), (48, 75)]), S)
    p_stache = c.part('stache', stache, 'stache', n_plane(0.1, -0.2, 0.95), bias=0.0)
    neckv = poly_mask(path((57, 77), (71, 77), (69, 80), (64, 82), (59, 80)), S)       # 조끼 깃 안쪽 (회청 셔츠)
    p_neck = c.part('neck', neckv, 'sleeve', n_plane(0.0, -0.3, 0.9), bias=-0.35)

    # ── 6 안전모 (설계도 y11~15 — 둥근 노랑 · 꼭대기 폭 설계도 안)
    dome = ellipse_mask(64, 62.5, 30, 21.5, S) & (yy < 62)
    p_dome = c.part('dome', dome, 'hat', n_ellipsoid(64, 66, 30, 25), bias=0.0)
    brim = poly_mask(path((35, 56), (93, 56), [(95.5, 58), (95.5, 61)], (92, 63), (36, 63), [(32.5, 61), (32.5, 58)]), S)
    p_brim = c.part('brim', brim, 'hat', n_plane(0.1, -0.15, 0.95), bias=-0.34)
    visor = poly_mask(path((46, 60), (82, 60), (81, 64), (47, 64)), S)
    p_visor = c.part('visor', visor, 'hat', n_plane(0.0, 0.1, 0.98), bias=-0.30)

    # ── 7 왼손: 안전모 챙을 잡고 고쳐 씀 (손가락이 챙을 감쌈)
    hand_l = ellipse_mask(37.5, 62.5, 7.2, 5.6, S)
    p_handl = c.part('hand_l', hand_l, 'skin', n_ellipsoid(37.5, 62.5, 7.2, 5.6), bias=-0.04)

    # ── 8 허리띠 · 버클 (설계도 y26)
    belt = poly_mask(path((40, 98), (88, 98), (88, 103), (40, 103)), S)
    p_belt = c.part('belt', belt, 'stripe', n_cyl_h(100.5, 3), bias=0.0)
    buckle = poly_mask(path((58, 97.5), (70, 97.5), (70, 103.5), (58, 103.5)), S)
    p_buckle = c.part('buckle', buckle, 'hat', n_plane(0.1, -0.5, 0.85), bias=0.0)

    # ── 9 허리춤 연장: 왼쪽 망치(자루 + 쇠머리) · 오른쪽 줄자(노랑 통 + 검은 구멍) — 허리띠에 걸림
    hammer_h = poly_mask(path((46, 102), (50, 102), (50, 110), (46, 110)), S)
    p_hamh = c.part('hammer_h', hammer_h, 'boot', n_cyl_v(48, 2.2), bias=0.0)
    hammer = poly_mask(path((41, 107), (55, 107), (55, 113), (41, 113)), S)
    p_ham = c.part('hammer', hammer, 'sleeve', n_plane(0.1, -0.4, 0.9), bias=0.0)
    tape = ellipse_mask(80, 109, 6.2, 6.2, S)
    p_tape = c.part('tape', tape, 'hat', n_ellipsoid(80, 109, 6.2, 6.2), bias=0.0)
    tclip = poly_mask(path((77, 101), (83, 101), (83, 104), (77, 104)), S)
    p_tclip = c.part('tclip', tclip, 'sleeve', n_plane(0.1, -0.4, 0.9), bias=-0.1)
    c.tidy(4)
    c.contact_lines(skip={('vest', 'neck'), ('vest', 'belt'), ('legs', 'belt'), ('belt', 'buckle'), ('face', 'stache'),
                          ('dome', 'brim'), ('face', 'visor'), ('dome', 'visor'), ('brim', 'visor'), ('upper_l', 'fore_l'),
                          ('vest', 'arm_r'), ('vest', 'upper_l'), ('face', 'neck'), ('arm_r', 'hand_r'), ('pole', 'hand_r'),
                          ('pole', 'arm_r'), ('boot_l', 'legs'), ('boot_r', 'legs'), ('vest', 'fore_l'), ('stache', 'neck'),
                          ('brim', 'hand_l'), ('dome', 'hand_l'), ('face', 'hand_l'), ('fore_l', 'hand_l'),
                          ('knob', 'pole'), ('legs', 'hammer_h'), ('legs', 'hammer'), ('hammer_h', 'hammer'), ('legs', 'tape'), ('belt', 'tclip'), ('tclip', 'tape'), ('legs', 'tclip'), ('belt', 'hammer_h'), ('legs', 'boot_l'), ('legs', 'boot_r'), ('vest', 'belt')})

    # ───────────── 덧그림 (큰 형태 위주)
    # 안전모: 가운데 줄 (설계도 x15~16 · 한 단계 어둡게 + 한쪽 빛) · 왼쪽 그늘 면 · 오른쪽 윗면 빛 덩어리
    ridge = [(x, y) for y in range(43, 58) for x in range(59, 66)]
    c.nudge(ridge, -1, p_dome)
    c.nudge([(x, y) for y in range(43, 58) for x in range(66, 68)], 1, p_dome)
    shade = (c.pid == p_dome) & ellipse_mask(36, 60, 12, 16, S)
    c.nudge([(int(x), int(y)) for y, x in zip(*np.nonzero(shade))], -1, p_dome)
    # 챙: 윗단 빛 1줄 · 아랫단 그늘
    c.nudge(line_cells([(36, 57), (92, 57)]), 1, p_brim)
    c.nudge(line_cells([(36, 62), (92, 62)]), -1, p_brim)
    # 얼굴: 챙 그늘이 이마에 드리움 · 왼 턱선 그늘 · 오른 볼 빛
    c.nudge([(x, y) for y in range(64, 67) for x in range(46, 82) if c.pid[y, x] == p_face], -1, p_face)
    fm = c.pid == p_face
    fin = ndimage.binary_erosion(fm, iterations=2)
    jaw = fm & ~fin & (((xx < 55) & (yy > 70)) | (yy > 76))
    c.nudge([(int(x), int(y)) for y, x in zip(*np.nonzero(jaw))], -1, p_face)
    lit = fm & ellipse_mask(75, 70, 7, 5, S) & (c.lvl == 2)
    c.lvl[lit] = 3
    # 콧수염: 가운데 위 빛 줄 · 양끝 처짐 (그늘)
    c.nudge(line_cells([(54, 71), (74, 71)]), 1, p_stache)
    c.nudge(line_cells([(49, 75), (56, 75)]) + line_cells([(72, 75), (79, 75)]), -1, p_stache)
    # 입: 반쯤 열린 「아 지금 하잖아요」 (콧수염 아래 · 어두운 입안 + 아랫니 + 혀)
    MOUTH, TEETH, TONGUE = hx('4A1A28'), EYE_HOT, ('skin', 0)
    c.stamp(['.aaaaaaaa.',
             'atttttttta',
             'aaaaaaaaaa',
             '.aaTTTTaa.'], 57, 75, {'a': MOUTH, 't': TEETH, 'T': TONGUE}, part=p_face)
    # 눈썹: 왼쪽 평평 · 오른쪽을 높이 올림 (한쪽 눈썹 올림)
    BR = ('stache', 0)
    c.stamp(['aaaaaaaaa', '.aaaaaaaa'], 51, 63, {'a': BR}, part=p_face)
    c.stamp(['.......aaa', '....aaaaaa', '..aaaa....'], 66, 60, {'a': BR}, part=p_visor)
    # 조끼: 검정 가로 띠 둘(굵게) + 위 주황 띠 · 가운데 지퍼 골 · 가슴 주머니 · 깃
    for (y0, y1) in ((81, 85), (90, 94)):
        c.set_lvl([(x, y) for y in range(y0, y1) for x in range(40, 89) if c.pid[y, x] == p_vest], 1, p_vest, mat='stripe')
    c.set_lvl([(x, y) for y in range(77, 101) for x in (63, 64)], 0, p_vest, mat='stripe')
    c.nudge(line_cells([(44, 79), (60, 79)]), 1, p_vest)
    c.nudge(line_cells([(68, 79), (84, 79)]), 1, p_vest)
    # 조끼 어깨끈 (회청 셔츠 위로) — 윗면 빛 줄
    # 소매: 팔꿈치 접힘 · 주먹 쪽 빛
    c.nudge(line_cells([(33, 80), (30, 88)]) + line_cells([(34, 80), (31, 88)]), 1, p_upl)
    c.nudge(line_cells([(37, 70), (38, 90)]) + line_cells([(38, 70), (39, 90)]), -1, p_forel)
    c.nudge(line_cells([(92, 80), (94, 92)]) + line_cells([(93, 80), (95, 92)]), 1, p_armr)
    c.nudge(line_cells([(85, 82), (87, 94)]) + line_cells([(86, 82), (88, 94)]), -1, p_armr)
    # 손: 손가락 골 (자루를 감쌈 · 안전모 챙을 잡음) — 좌우 다르게
    c.nudge(line_cells([(91, 95), (91, 102)]) + line_cells([(95, 95), (95, 102)]), -1, p_handr)
    c.nudge(line_cells([(32, 61), (33, 66)]) + line_cells([(37, 59), (38, 66)]) + line_cells([(42, 60), (42, 66)]), -1, p_handl)
    c.nudge(line_cells([(32, 58), (43, 57)]), 1, p_handl)
    # 바지: 두 다리 사이 골 · 오른 다리 빛 · 무릎 접힘 · 아랫단 접힘
    c.nudge(line_cells([(63, 106), (63, 113)]) + line_cells([(64, 106), (64, 113)]), -1, p_legs)
    c.nudge(line_cells([(80, 104), (81, 112)]) + line_cells([(81, 104), (82, 112)]), 1, p_legs)
    c.nudge(line_cells([(50, 106), (48, 112)]) + line_cells([(51, 106), (49, 112)]), -1, p_legs)
    c.nudge([(x, 113) for x in range(44, 84)], 1, p_legs)
    # 장화: 윗단 끈 줄 · 발끝 코 그늘
    for pb in (p_bootl, p_bootr):
        c.nudge([(x, 113) for x in range(36, 92)], 1, pb)
        c.nudge([(x, y) for y in (122, 123) for x in range(36, 92)], -1, pb)
    # 자루: 빛 줄 1
    c.nudge(line_cells([(98, 70), (98, 96)]), 1, p_pole)
    # 버클: 금 속 안쪽 사각 홈
    c.set_lvl([(x, y) for y in range(99, 102) for x in range(61, 67)], 0, p_buckle)

    # 안전모 갈빗대 (왼쪽 · 오른쪽 둘씩 — 둥글게 휘어 가운데 줄로 모임) · 조끼 주머니 덮개 · 명찰 · 바지 무릎 · 소매 끝 · 코 덮개
    for pts in ([(52, 59), (50, 51), (55, 44)], [(44, 59), (42, 52), (46, 47)], [(76, 59), (78, 51), (73, 44)], [(84, 59), (86, 52), (82, 47)]):
        cl = [(int(round(x)), int(round(y))) for x, y in path(pts)]
        cl = line_cells(path(pts))
        c.nudge(cl + [(x + 1, y) for x, y in cl], -1, p_dome)
    for (x0, x1) in ((45, 57), (71, 83)):
        c.nudge([(x, y) for y in range(95, 98) for x in range(x0, x1)], -1, p_vest)
    c.stamp(['TTTTTTTT', 'TgggTTTT', 'TgggTTTT', 'TTTTTTTT'], 70, 82, {'T': EYE_HOT, 'g': ('stache', 1)})
    for x0 in (46, 74):
        c.nudge([(x, y) for y in range(106, 111) for x in range(x0, x0 + 8)], -1, p_legs)
    c.nudge([(x, y) for y in range(71, 75) for x in range(30, 41)], -1, p_forel)
    c.nudge([(x, y) for y in range(83, 89) for x in range(29, 36)], -1, p_upl)
    c.nudge([(x, y) for y in range(91, 94) for x in range(86, 96)], -1, p_armr)
    c.nudge([(x, y) for y in range(114, 118) for x in range(38, 47)], -1, p_bootl)
    c.nudge([(x, y) for y in range(114, 118) for x in range(80, 89)], -1, p_bootr)
    # 바지 주머니 · 장화 끈 구멍 · 소매 솔기 · 안전모 앞 표지 (큰 덩어리를 나눔)
    for x0, x1 in ((46, 56), (72, 82)):
        c.nudge(line_cells([(x0, 103), (x0, 110)]) + line_cells([(x0 + 1, 103), (x0 + 1, 110)]), -1, p_legs)
        c.nudge(line_cells([(x0, 110), (x1, 110)]) + line_cells([(x0, 111), (x1, 111)]), -1, p_legs)
    for pb, (xa, xb) in ((p_bootl, (44, 56)), (p_bootr, (70, 82))):
        c.nudge([(x, y) for y in range(118, 121) for x in range(xa, xb)], 1, pb)
    c.nudge(line_cells([(34, 71), (34, 83)]) + line_cells([(35, 71), (35, 83)]), 1, p_forel)
    c.nudge([(x, y) for y in range(77, 80) for x in range(30, 41)], 1, p_upl)
    c.stamp(['..AAAA..', '.ABBBBA.', 'ABBBBBBA', '.AAAAAA.'], 56, 49, {'A': ('hat', 1), 'B': ('hat', 3)}, part=p_dome)
    c.nudge([(x, y) for y in range(78, 81) for x in range(42, 60)], 1, p_vest)
    c.nudge([(x, y) for y in range(78, 81) for x in range(68, 86)], 1, p_vest)
    c.set_lvl([(x, y) for y in range(107, 111) for x in range(78, 82)], 0, p_tape, mat='stripe')
    c.nudge([(x, y) for y in range(108, 111) for x in range(43, 49)], 1, p_ham)
    c.rim((p_dome, p_brim), RIM_DIR, width=2, level=-1, thresh=0.3)
    c.rim((p_vest, p_armr, p_legs), RIM_DIR, width=2, by=1, thresh=0.35)
    keep = np.isin(c.pid, (p_handl, p_handr, p_buckle))
    c.absorb((c.pid >= 0) & ~keep, min_size=3)

    # 눈 (설계도 x14 · x17 → 56 · 68) — 오른쪽 눈썹이 올라가 눈이 조금 커 보임
    eye = ['aab', 'aaa', 'aaa', '.a.']
    for ex in (55, 68):
        c.stamp(eye, ex, 66, {'a': EYE, 'b': EYE_HOT})
    dots = [(57, 66), (70, 66)]
    c.outline(warm_parts=(p_handl, p_handr, p_face, p_bootl, p_bootr, p_stache, p_pole, p_knob, p_vest, p_dome, p_brim, p_buckle, p_legs))
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
