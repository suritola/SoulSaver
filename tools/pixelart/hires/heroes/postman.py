# 우체부 메뉴 초상 128칸 (그래픽 가독성 개편 2단계 묶음 3 — 도적 규칙(README.md) 그대로 영웅 8 중 하나)
#   설계도 = Resources/Characters/postman.png 첫 그림(32칸 · 몸 15×18) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 14~16 붉은 우체부 모자(위가 납작한 사각 · 가운데 금 휘장 x15~16) · 17 짙은 챙(오른쪽으로 나옴 x21~22)
#     18~21 얼굴(눈 x14 · x17) · 21 짙은 남색 깃 · 22~25 남색 제복(파란 앞섶 x14 + 금 단추 · 왼팔 x10~11 · 오른팔 x18~19)
#     23~25 갈색 어깨끈(오른 어깨 → 왼 허리) · 25 왼손 · 오른손에 크림색 편지(x18~19) · 26 허리띠 + 주황 버클
#     26~28 왼쪽 아래 갈색 우편 가방 · 27~29 짙은 바지 · 30~31 어두운 장화
#   광원 = 위 · 살짝 오른쪽 (모자 오른 가장자리 · 챙 오른끝 · 오른팔이 밝음 — 설계도 e · i · u)
#   명도 계층: 얼굴(눈) > 편지 > 금 휘장 · 단추 > 모자 > 제복 > 바지 · 장화 (편지 가장 밝은 단계는 윗단 1줄만)
import numpy as np
from scipy import ndimage

from .portrait import Canvas, Map, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, despeckle

SIZE = 128
K = 4
M = Map(K, 0, -3)
DESIGN = 'Assets/Resources/Characters/postman.png'     # 시트 첫 칸 (32×32) 이 설계도

# 재질: 어두움 → 밝음 · 전체 ≤ 32색 (재질 8 + 눈 2 + 외곽 2)
MATERIALS = {
    # 모자: 바탕 #C83440 (설계도 d #BE2836) · 빛 #F0645A (설계도 c #F2504E) — 가장 밝은 단계는 림만
    'hat':     dict(ramp=[hx('5A1230'), hx('9A2036'), hx('C83440'), hx('F0645A')], cuts=[0.34, 0.62, 9.0]),
    # 짙은 남보라: 챙 · 바지 (설계도 h #261C3C · i #546886 · 바지 A #484264 · D #5A728F)
    'dark':    dict(ramp=[hx('161028'), hx('261C3C'), hx('3A3456'), hx('4E5476'), hx('6A7E9E')], cuts=[0.30, 0.55, 0.92, 9.0]),
    # 제복: 바탕 #2C428C (설계도 r) · 빛 #4A6EB5 (설계도 q) · 림 #7C9ED8 (설계도 u #5780B8 보다 한 단계 밝게)
    'uniform': dict(ramp=[hx('121640'), hx('1E2C72'), hx('2C428C'), hx('4A6EB5'), hx('7C9ED8')], cuts=[0.30, 0.55, 0.92, 9.0]),
    # 가죽: 가방 · 어깨끈 · 허리띠 · 장화 (설계도 w #5E2C17 · t #865632)
    'leather': dict(ramp=[hx('2E1A1E'), hx('5E2C17'), hx('865632'), hx('B07C4C')], cuts=[0.40, 0.70, 1.0], warm=True),
    'gold':    dict(ramp=[hx('B46826'), hx('EAAA3A'), hx('FEDE6C')], cuts=[0.55, 0.95], warm=True),
    # 피부 4단 (판정 1회차 권장: 얼굴 큰 평면 → 바탕 #E2A27E 중간 단계 + 위 · 오른쪽 빛 #F2BC96 + 턱 그늘)
    'skin':    dict(ramp=[hx('8A4E52'), hx('CA7E70'), hx('E2A27E'), hx('F2BC96')], cuts=[0.50, 0.80, 1.10], warm=True),
    # 편지: 크림 바탕 Y 약 167 (얼굴보다 한 단계 어둡게 · 눈보다 덜 튀게) · 가장 밝은 단계는 윗단 1줄
    'letter':  dict(ramp=[hx('847862'), hx('B4A688'), hx('D0C4A8')], cuts=[0.45, 9.0]),     # 판정 1회차: 얼굴(평균 Y 164)보다 어둡게
}
EYE = hx('120B20')
EYE_HOT = hx('F4F0FF')
LIGHT = (0.12, -0.8, 0.6)
RIM_DIR = (0.4, -0.92)


# ───────────── 형태 모델 (법선 = x 오른쪽 · y 아래 · z 보는 쪽)
def n_ball(cx, cy, rx, ry):
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.97, 0.97)
        v = np.clip((yy + 0.5 - cy) / ry, -0.97, 0.97)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def n_body(cx, r, top, drop, sink=100, sink_k=0.3):
    """몸통 · 팔: 세로 원기둥 + 어깨에서 위를 보는 둥근 어깨 (top 에서 drop 칸 동안) + 아래로 갈수록 조금 그늘"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        v = -np.clip(1 - (yy + 0.5 - top) / drop, 0, 1) * 0.85 + np.clip((yy - sink) / 40.0, 0, sink_k)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def n_cap(cx, r, top):
    """모자 둘레: 세로 원기둥 (가운데가 앞) — 위 2칸은 위를 봄 (납작한 윗면이 꺾이는 모서리)"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        v = np.where(yy < top + 2, -0.8, -0.15)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def draw():
    c = Canvas(SIZE, MATERIALS, light=LIGHT)
    S = SIZE

    # ── 1 장화 (설계도 y30~31 — 작고 어두움 · 발끝이 바깥으로)
    boot_l = poly_mask(path((46, 117), (57, 117), (57, 125), (41, 125), [(41, 122.5), (42.5, 121), (46, 120.5)]), S)
    boot_r = poly_mask(path((63, 117), (74, 117), [(74.5, 120), (77, 121), (79, 122.5)], (79, 125), (63, 125)), S)
    p_bootl = c.part('boot_l', boot_l, 'leather', n_plane(-0.1, -0.6, 0.8), bias=-0.42)
    p_bootr = c.part('boot_r', boot_r, 'leather', n_plane(0.3, -0.6, 0.75), bias=-0.34)

    # ── 2 바지 (설계도 y27~29 — 짙은 남보라 · 두 다리 사이 골)
    legs = poly_mask(path((47, 103), (73, 103), (73.5, 118), (63, 118), (60.5, 109), (58, 118), (46.5, 118)), S)
    p_legs = c.part('legs', legs, 'dark', n_body(60, 14, 103, 4, sink=104, sink_k=0.2), bias=0.0)

    # ── 3 제복 몸통 (설계도 x12~17 · y21~26 — 허리띠 아래로 짧은 자락)
    torso = poly_mask(path((46, 83), (74, 83), (74.5, 107), (45.5, 107)), S)
    p_torso = c.part('torso', torso, 'uniform', n_body(60, 20, 83, 7), bias=-0.04)

    # ── 4 팔 (설계도 왼팔 x9~11 · 오른팔 x18~20 — 어깨가 둥글고 몸통과 짙은 골로 나뉨)
    arm_l = poly_mask(path((47, 84), (42, 84.5), [(38.5, 86), (36.5, 89), (36.5, 93)], (37, 100), (47.5, 100)), S)
    arm_r = poly_mask(path((73, 84), (78, 84.5), [(81.5, 86), (83.5, 89), (83.5, 93)], (83, 98), (72.5, 98)), S)
    p_arml = c.part('arm_l', arm_l, 'uniform', n_body(43, 9, 84, 6), bias=-0.08)
    p_armr = c.part('arm_r', arm_r, 'uniform', n_body(77, 9, 84, 6), bias=-0.08)

    # ── 5 허리띠 (설계도 y26) · 어깨끈 (오른 어깨 → 왼 허리 · 설계도 (16,23)→(14,25))
    belt = poly_mask(path((45, 101), (75, 101), (75, 105), (45, 105)), S)
    p_belt = c.part('belt', belt, 'leather', n_plane(0.1, -0.4, 0.9), bias=-0.02)
    strap = poly_mask(path((72, 83), (77, 83), (52, 108), (47, 108)), S)
    p_strap = c.part('strap', strap, 'leather', n_plane(0.1, -0.3, 0.9), bias=-0.12)

    # ── 6 우편 가방 (설계도 x8~11 · y26~28 — 왼 허리 · 덮개 + 쇠 고리)
    bag = poly_mask(path((33, 102), (48.5, 101), (49, 111), [(48.5, 114), (46, 114.5), (36, 114.5)], [(33.5, 114.5), (32.5, 112), (32.5, 109)]), S)
    p_bag = c.part('bag', bag, 'leather', n_ball(44, 104, 14, 12), bias=0.12)
    flap = poly_mask(path((32.5, 101.5), (48.8, 100.5), (49, 107), [(45, 108.5), (37, 108.5), (32.5, 107.5)]), S)
    p_flap = c.part('flap', flap, 'leather', n_plane(0.15, -0.6, 0.8), bias=0.02)

    # ── 7 손: 왼손 = 가방 위 주먹 · 오른손 = 편지를 쥠 (좌우 다르게)
    p_handl = c.part('hand_l', ellipse_mask(42.5, 99.5, 4.6, 3.6, S), 'skin', n_ball(44, 98, 5, 4.5), bias=-0.06)
    letter = poly_mask(path((70, 95.5), (84, 93.5), (85.5, 101.5), (71.5, 103.5)), S)
    p_letter = c.part('letter', letter, 'letter', n_plane(0.2, -0.3, 0.9), bias=-0.02)
    p_handr = c.part('hand_r', ellipse_mask(74, 100.5, 3.8, 3.4, S), 'skin', n_ball(76, 99, 5, 4.5), bias=0.0)

    # ── 8 깃 (설계도 y21 짙은 남색 — 목을 감싼 선 깃)
    collar = poly_mask(path((46, 81), (74, 81), (75, 86), [(66, 87.5), (54, 87.5), (45, 86)]), S)
    p_collar = c.part('collar', collar, 'uniform', n_plane(0.1, -0.2, 0.95), bias=-0.38)

    # ── 9 얼굴 (설계도 x12~19 · y18~21 — 챙 아래 · 턱이 깃 위로)
    face = poly_mask(path((47.5, 66), (80.5, 66), (80.5, 77), [(80, 81), (77, 84), (72, 84.5)], (56, 84.5),
                          [(51, 84), (48, 81), (47.5, 77)]), S)
    p_face = c.part('face', face, 'skin', n_plane(0.0, -0.5, 0.85), bias=0.0)
    c.lvl[face] = 2                         # 얼굴 바탕 = 중간 단계 (빛은 아래 덧그림에서 위 · 오른쪽 한 덩어리로)

    # ── 10 모자 (설계도 y14~16 — 위가 납작한 사각 · 꼭대기 폭 = 설계도 10칸)
    #   (판정 2회차 권장: 윗선 긴 직선 → 가운데 휘장 위 1칸 솟음)
    crown = poly_mask(path((45, 54), (57, 54), (61, 53), (68, 53), (72, 54), (80, 54), [(84.5, 54), (87.5, 55.5), (88, 59)], (88, 66), (40, 66), (40, 58), (42, 55.5)), S)
    p_crown = c.part('crown', crown, 'hat', n_cap(64, 30, 53), bias=0.0)
    # 챙 (설계도 y17 — 오른쪽으로 나온 짙은 챙)
    brim = poly_mask(path((40, 65), (86, 65), [(89, 65.2), (91.5, 66.5), (91.5, 68.5)], (87, 70), (44, 70), (40, 69)), S)
    p_brim = c.part('brim', brim, 'dark', n_plane(0.1, -0.2, 0.95), bias=-0.3)

    c.tidy(4)
    c.contact_lines(skip={('torso', 'belt'), ('legs', 'belt'), ('torso', 'strap'), ('belt', 'strap'), ('bag', 'flap'),
                          ('crown', 'brim'), ('letter', 'hand_r'), ('torso', 'collar'), ('arm_l', 'collar'), ('arm_r', 'collar')})

    # ───────────── 덧그림 (큰 형태 위주 — 잔점 · 같은 간격 줄 없음)
    deep = ndimage.distance_transform_cdt(np.pad(c.mat >= 0, 1), metric='taxicab')[1:-1, 1:-1] >= 4

    def fold(pts, parts, side=-1, dark=-1, ridge=1):
        """긴 천 주름: 골 2칸(빛 반대쪽) + 등성이 1칸(빛 쪽) — 실루엣 3칸 안은 안 팜"""
        cells = [(x, y) for x, y in line_cells(path(*pts)) if 0 <= y < S and 0 <= x < S and deep[y, x]]
        c.nudge(cells + [(x + side, y) for x, y in cells], dark, parts)
        if ridge:
            c.nudge([(x - side, y) for x, y in cells], ridge, parts)

    # 모자: 아래 띠 (휘장 높이 · 한 단계 어둡게) · 윗면 모서리 빛 · 금 휘장 (봉투 모양 · 가운데)
    c.nudge([(x, y) for y in (63, 64) for x in range(41, 88)], -1, p_crown)
    c.stamp(['..BBBB..',
             '.BCCBBB.',
             'BBCCBBBA',
             'BBBBBBBA',
             '.AABBBAA',
             '..AAAA..'], 60, 57, {'A': ('gold', 0), 'B': ('gold', 1), 'C': ('gold', 2)})
    # 크라운 왼쪽 그늘 면 (빛 반대 · 곡선 경계 하나 — 판정 2회차: 바탕 한 색 274칸 → 200칸 이하)
    shade = (c.pid == p_crown) & (c.mat == c.mi('hat')) & (c.lvl == 2) & ellipse_mask(41, 63, 15, 11, S)
    c.lvl[shade] = 1
    # 챙: 윗단 빛 1줄 (오른끝까지) · 챙 그늘이 얼굴 위 2줄에 드리움
    c.nudge(line_cells([(46, 66), (89, 66)]), 1, p_brim)
    c.nudge(line_cells([(43, 69), (86, 69)]), -1, p_brim)           # 챙 아랫단 그늘 1줄 (판정 2회차 권장: 231칸 한 색 띠 나누기)
    c.nudge([(x, y) for y in (70, 71) for x in range(48, 81)], -1, p_face)
    # 얼굴: 왼 턱선 그늘 · 코 그늘 · 입 · 턱 아래 그늘
    fm = c.pid == p_face
    fin = ndimage.binary_erosion(fm, iterations=2)
    jaw = fm & ~fin & (((c.xx < 57) & (c.yy > 73)) | (c.yy > 80))      # 왼 턱선 · 턱 아래 2칸 (빛 반대쪽)
    c.nudge([(int(x), int(y)) for y, x in zip(*np.nonzero(jaw))], -1, p_face)
    c.set_lvl([(64, 78), (65, 78), (64, 79), (65, 79)], 1, p_face)
    c.set_lvl([(61, 81), (62, 81), (63, 81), (64, 81), (65, 81)], 1, p_face)
    c.set_lvl(line_cells([(53, 83), (75, 83)]), 1, p_face)
    # 얼굴 빛: 위 · 오른쪽 볼과 이마 오른쪽을 한 덩어리로 가장 밝은 단계 (판정 1회차 권장 — 살색 큰 평면 나누기)
    lit = fm & ellipse_mask(74, 75.5, 8.5, 6.0, S) & (c.lvl == 2)
    c.lvl[lit] = 3
    # 깃: 윗단 빛 · 앞 여밈 골
    c.nudge(line_cells([(48, 82), (72, 82)]), 1, p_collar)
    # 제복: 파란 앞섶 (설계도 q x14 — 몸통 가운데 세로 띠) · 금 단추 둘 · 팔과 몸통 사이 짙은 골 (설계도 n x12 · x17)
    plk = [(x, y) for y in range(88, 101) for x in range(56, 61)]
    c.set_lvl(plk, 3, p_torso)
    c.nudge([(61, y) for y in range(88, 101)], -1, p_torso)
    c.stamp(['CCC', 'BBB', 'BBB'], 57, 90, {'B': ('gold', 1), 'C': ('gold', 2)})
    c.stamp(['BBB', 'AAA'], 57, 96, {'A': ('gold', 0), 'B': ('gold', 1)})
    fold([[(49, 88), (49, 100)]], p_torso, side=0, ridge=0)
    fold([[(70, 89), (71, 100)]], p_torso, side=1, ridge=0)
    # 팔: 팔꿈치로 모인 긴 주름 하나씩 · 소매 끝 접힘 (밝은 띠)
    fold([[(41, 89), (42, 97)]], p_arml)
    fold([[(79, 89), (78, 95)]], p_armr)
    c.nudge(line_cells([(38, 98), (46, 98)]), 1, p_arml)
    c.nudge(line_cells([(74, 96), (82, 96)]), 1, p_armr)
    # 제복 아랫자락 (허리띠 아래) 그늘
    c.nudge([(x, 106) for x in range(46, 75)], -1, p_torso)
    # 허리띠: 띠 아래 그늘 (판정 2회차 권장: 가운데 금 버클 → 어깨끈 위쪽 작은 고리로 옮김 · 묶음 공통 「가슴 한가운데 금 장식」 반복 피하기)
    c.nudge(line_cells([(45, 104), (75, 104)]), -1, p_belt)
    # 어깨끈: 빛 쪽 가장자리 1줄 (끊지 않음)
    sl = line_cells([(76, 84), (51, 107)])
    c.nudge(sl + [(x + 1, y) for x, y in sl], 1, p_strap)       # 2칸 폭 — 대각선 1칸 계단은 잔점으로 보임
    c.stamp(['AAA', 'ABA', 'AAA'], 68, 88, {'A': ('gold', 0), 'B': ('gold', 1)}, part=p_strap)     # 끈 조임 고리 (오른 가슴 위 · 작게)
    # 가방: 덮개 아랫단 그늘 · 금 걸쇠 · 가방 아래 그늘 · 덮개 윗단 빛
    c.nudge(line_cells([(34, 109), (48, 109)]), -1, p_bag)
    c.stamp(['BBB', 'BCC', 'BCC'], 39, 105, {'B': ('gold', 1), 'C': ('gold', 2)})
    c.nudge([(x, 113) for x in range(35, 47)] + [(x, 112) for x in range(37, 45)], -1, p_bag)
    c.nudge(line_cells([(35, 102), (47, 101)]), 1, p_flap)
    # 바지: 두 다리 사이 골 · 오른 다리 빛 (설계도 D) · 무릎 접힘
    c.nudge(line_cells([(60, 105), (60, 110)]), -1, p_legs)
    c.nudge(line_cells([(69, 106), (70, 116)]) + line_cells([(70, 106), (71, 116)]), 1, p_legs)
    fold([[(52, 107), (51, 116)]], p_legs, ridge=0)
    # 장화: 접힌 목 (위 1줄 밝게) · 발끝 그늘
    for pb in (p_bootl, p_bootr):
        c.nudge([(x, 118) for x in range(40, 82)], 1, pb)
    # 왼손: 가방 덮개를 누른 편 손 — 손가락이 아래로 (오른손 쥔 손과 모양이 다르게 · 판정 2회차 묶음 공통 권장)
    c.stamp(['..ccc...',
             '.bcccc..',
             'bbbcccb.',
             'bbabbbb.',
             'bbabbbb.',
             '.bab.bb.'], 38, 96, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 3)})
    # 오른손: 편지를 쥔 손가락 (엄지 위 · 손가락 골 하나)
    c.stamp(['.ccc..',
             'bcccc.',
             'bbbbbb',
             'aaaaa.',
             'bbbbbb',
             '.bbbb.'], 71, 97, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 3)})
    # 편지: 봉투 접힌 V (바탕보다 한 단계 어둡게 · 2칸 굵기) · 윗단 밝은 1줄 · 붉은 봉인
    vcells = line_cells([(77, 95), (79, 99), (84, 94)])
    c.set_lvl(vcells + [(x, y + 1) for x, y in vcells], 0, p_letter)
    c.set_lvl(line_cells([(76, 95), (83, 94)]), 2, p_letter)
    c.stamp(['RR', 'RR'], 78, 99, {'R': ('hat', 1)}, part=p_letter)

    # 림 라이트: 오른쪽 위 실루엣 안쪽 — 모자 2칸 · 어깨 2칸 · 챙 · 가방 1칸
    c.rim((p_crown,), RIM_DIR, width=2, level=-1, thresh=0.3)
    # 모자 윗면 림을 휘장 왼쪽에서 한 번 끊음 (판정 1회차 권장 — 위 긴 직선이 자로 그은 듯)
    c.set_lvl([(x, y) for y in (54, 55) for x in range(53, 62)], 2, p_crown)
    c.rim((p_arml, p_armr, p_torso), RIM_DIR, width=2, level=-1, thresh=0.3)
    c.rim((p_brim, p_flap, p_legs), RIM_DIR, width=1, by=1, thresh=0.5)
    # 1~2칸 조각(4-연결) 흡수 — 림 · 주름 끝이 이어진 띠가 되게
    keep = (c.mat == c.mi('gold')) | np.isin(c.pid, (p_handl, p_handr))     # 손 도트 지도 · 금붙이 도장은 그대로
    c.absorb((c.pid >= 0) & ~keep, min_size=3)

    # 눈 (짙은 눈동자 · 오른 위 반짝 1칸 — 설계도 x14 · x17 → 가운데 58 · 70)
    eye = ['aab', 'aaa', 'aaa', '.a.']
    for ex in (56, 68):
        c.stamp(eye, ex, 73, {'a': EYE, 'b': EYE_HOT})
    dots = [(58, 73), (70, 73)]
    c.outline(warm_parts=(p_handl, p_handr, p_bootl, p_bootr))
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
