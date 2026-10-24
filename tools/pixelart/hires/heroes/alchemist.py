# 연금술사 메뉴 초상 128칸 (그래픽 가독성 개편 2단계 묶음 3 — 도적 규칙(README.md) 그대로)
#   설계도 = Resources/Characters/alchemist.png 첫 그림 (32칸 · 몸 13×20) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 12~16 흰 머리카락 (꼭대기가 두 갈래 뿔처럼 솟음 — 왼 갈래 꼭지 x13 · 오른 갈래 꼭지 x19 · 사이 골 x15~16)
#     17 이마의 갈색 띠 + 청록 고글 알 둘 (x13~14 · x16~17) · 18~21 얼굴 (검은 눈 x14 · x17) · 양옆 머리 자락 x11 · x20 이 y18 까지
#     21~25 보라 로브 (팔 x10~11 · x18~19, 몸통 x13~16, 그 사이 짙은 골 x12 · x17) · 25 양손 · 26 허리띠 + 초록 물약병 (x13~14) + 쇠 고리 (x19)
#     27~29 짙은 남색 바지 (가운데 골 · 오른쪽 밝음) · 30~31 작은 장화
#   명도 계층: 흰 머리 바탕은 설계도(Y 230~250)보다 낮춰(Y 약 190) 얼굴(Y 199)과 눈이 먼저 보이게 — 가장 밝은 단계는 위 · 오른쪽 림 1~2칸만
#   광원 = 위 · 살짝 오른쪽 (도적과 같은 메뉴 조명)
import numpy as np
from scipy import ndimage

from .portrait import Canvas, Map, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, n_cyl_h, despeckle

SIZE = 128
K = 4
M = Map(K, 0, -3)
DESIGN = 'Assets/Resources/Characters/alchemist.png'     # 시트 첫 칸 (32×32) 이 설계도

# 재질: 어두움 → 밝음 (어두운 쪽은 차갑게, 밝은 쪽은 따뜻하게) · 재질당 ≤ 5단 · 전체 ≤ 32색
MATERIALS = {
    # 흰 머리: 그늘 #4E4866 · 골 #8C88AA · 바탕 #BCBCD6 (Y 191 — 얼굴보다 어둡게) · 빛 #CCCCE2 · 림 #D8D8EA (Y 216)
    #   (판정 1회차: 림 Y 247 이 얼굴 최고 Y 222 보다 밝아 시선을 뺏음 → 머리 가장 밝은 단계도 얼굴 최고보다 어둡게)
    'hair':  dict(ramp=[hx('4E4866'), hx('8C88AA'), hx('BCBCD6'), hx('CCCCE2'), hx('D8D8EA')], cuts=[0.34, 0.56, 1.02, 9.0]),
    # 로브: 바탕 #7444A4 (설계도 p #7C46AA) · 빛 #9C62CC (설계도 o) · 림 #C490EC
    'robe':  dict(ramp=[hx('26123C'), hx('4A2274'), hx('7444A4'), hx('9C62CC'), hx('C490EC')], cuts=[0.34, 0.58, 0.98, 9.0]),
    'skin':  dict(ramp=[hx('8A4E52'), hx('CA7E70'), hx('F2BC96'), hx('FCD8B4')], cuts=[0.40, 0.70, 1.10], warm=True),
    # 바지: 설계도 y #484264 · z #26203C · B #5A728F
    'pants': dict(ramp=[hx('1C1830'), hx('2E2A4A'), hx('464062'), hx('62789A')], cuts=[0.32, 0.56, 1.02]),
    # 가죽 (장화 · 고글 띠 — 띠는 밝은 두 단계 · 설계도 f #A06428) · 로브 끈도 이 갈색 (설계도 y26 의 t · u)
    'leather': dict(ramp=[hx('2E1C26'), hx('4A2C30'), hx('7A4A2E'), hx('A8692C')], cuts=[0.40, 0.70, 1.0], warm=True),
    # 고글 알 (청록 유리 · 가장 밝은 단계는 반사 한 줄만)
    'lens':  dict(ramp=[hx('2A7E98'), hx('50D6EA'), hx('9EE8F0')], cuts=[0.62, 9.0]),     # 반사 Y 211 (얼굴 최고보다 어둡게)
    # 물약 (초록 유리 · 설계도 v #30B676 · w #82ECAA)
    'potion': dict(ramp=[hx('1A6A48'), hx('30B676'), hx('A8F0C4')], cuts=[0.62, 9.0]),
}
EYE = hx('120B20')
FACE_C = (68.5, 70, 17.5, 12.0)     # 얼굴 그늘 초승달의 안쪽 타원 (가운데 x · y · 반지름 x · y)
LIGHT = (0.25, -0.8, 0.55)
RIM_DIR = (0.4, -0.92)           # 림 라이트가 드는 바깥 방향 (위 · 조금 오른쪽)


# ───────────── 형태 모델 (법선 = x 오른쪽 · y 아래 · z 보는 쪽)
def n_ball(cx, cy, rx, ry):
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.97, 0.97)
        v = np.clip((yy + 0.5 - cy) / ry, -0.97, 0.97)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def n_dome(cx, cy, rx, ry):
    """머리카락: 위가 둥근 반구 (아래로 갈수록 앞을 봄)"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.98, 0.98)
        v = np.clip((yy + 0.5 - cy) / ry, -0.98, 0.1)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.04, 1))
    return f


def n_body(cx, r, top, drop):
    """로브: 세로 원기둥 + 어깨에서 위를 보는 둥근 어깨 (top 에서 drop 칸 동안)"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        v = -np.clip(1 - (yy + 0.5 - top) / drop, 0, 1) * 0.8
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def draw():
    c = Canvas(SIZE, MATERIALS, light=LIGHT)
    S = SIZE

    # ── 1 바지 (설계도 y27~29 · 가운데 골 · 아래는 두 다리)
    pants = poly_mask(path((47, 103), (76, 103), (76, 119), (64.5, 119), (63.5, 112), (60, 112), (59, 119), (47, 119)), S)
    p_pants = c.part('pants', pants, 'pants', n_body(62, 18, 103, 4), bias=0.42)

    # ── 2 장화 (설계도 y30~31 — 작고 어두움 · 발끝이 바깥으로)
    boot_l = poly_mask(path((47, 117), (59, 117), (59, 125), (43, 125), [(43, 122.5), (44.5, 121), (47, 120.5)]), S)
    boot_r = poly_mask(path((64, 117), (76, 117), [(76.5, 120), (78, 121), (79, 122)], (79, 125), (64, 125)), S)
    p_bootl = c.part('boot_l', boot_l, 'leather', n_plane(-0.1, -0.6, 0.8), bias=-0.26)
    p_bootr = c.part('boot_r', boot_r, 'leather', n_plane(0.3, -0.6, 0.75), bias=-0.30)

    # ── 3 로브 몸통 (설계도 x13~16 · y22~25)
    torso = poly_mask(path((48, 83), (73, 83), (73, 104), (48, 104)), S)
    p_torso = c.part('torso', torso, 'robe', n_body(61, 16, 83, 6), bias=0.27)

    # ── 4 팔 (소매 — 설계도 x10~11 · x18~19, 어깨는 둥글게) — 몸통 뒤에 그려 사이 골(설계도 m)이 경계선이 됨
    arm_l = poly_mask(path((50, 84), [(46, 82.5), (40.5, 83), (37.5, 87.5)], (36.5, 97), (49, 97.5), (50.5, 90)), S)
    arm_r = poly_mask(path((71, 84), [(75, 82.5), (80.5, 83), (83.5, 87.5)], (84, 97), (71.5, 97.5), (70.5, 90)), S)
    p_arml = c.part('arm_l', arm_l, 'robe', n_body(43, 9, 83, 6), bias=0.28)
    p_armr = c.part('arm_r', arm_r, 'robe', n_body(78, 9, 83, 6), bias=0.25)

    # ── 5 로브 끈 (설계도 y26 — 판정 1회차: 4명이 같은 「넓은 금 허리띠 + 가운데 버클」 → 연금술사는 천 끈을 오른쪽에서 묶음 · 쇠 없음)
    #   가운데가 살짝 처진 3~4칸 천 띠 + 오른쪽 매듭 + 바지 위로 늘어진 끈 두 가닥 (길이 · 기울기 다르게)
    belt = poly_mask(path((47, 101), (76, 100.5), (76, 104), [(66, 105.5), (56, 105.5), (47, 104.5)]), S)
    p_belt = c.part('belt', belt, 'leather', n_plane(0.1, -0.4, 0.9), bias=-0.02)

    # ── 6 손 (설계도 y25 — 좌우 다르게: 왼손 주먹 · 오른손 엄지를 앞으로 편 손)
    p_handl = c.part('hand_l', ellipse_mask(43, 99.5, 4.6, 3.6, S), 'skin', n_ball(44, 98, 5, 4.5), bias=-0.10)
    p_handr = c.part('hand_r', ellipse_mask(78.5, 99.5, 4.4, 3.8, S), 'skin', n_ball(80, 98, 5, 4.5), bias=0.0)

    # ── 7 물약병 (허리띠 왼쪽에 매단 둥근 병 · 설계도 x13~14) — 목 + 코르크 + 둥근 몸
    p_cork = c.part('cork', poly_mask(path((53.5, 97), (57.5, 97), (57.5, 101), (53.5, 101)), S), 'leather', n_plane(0.3, -0.6, 0.75), bias=0.05)
    bottle = ellipse_mask(55.5, 106, 4.6, 4.3, S) | poly_mask(path((54, 100), (57, 100), (57, 103), (54, 103)), S)
    p_pot = c.part('potion', bottle, 'potion', n_ball(55.5, 106, 5.0, 4.6), bias=-0.05)
    # 오른 허리 가죽 주머니 (판정 2회차: 늘어진 끈 두 가닥 + 매듭이 흘러내린 얼룩 → 덮개 + 바닥선이 있는 주머니 하나)
    #   몸통 = 아래 모서리가 둥근 상자 · 덮개 = 위 4줄 한 단계 밝게 + 아래 끝이 가운데로 살짝 처진 곡선 · 덮개 밑 그늘 1줄 · 바닥선 그늘 1줄
    pouch = poly_mask(path((65, 102), (75, 102), (75.5, 110), [(75.5, 112), (74, 113), (72, 113)], (68, 113), [(66, 113), (64.5, 112), (64.5, 110)]), S)
    p_tails = c.part('tails', pouch, 'leather', n_plane(0.35, -0.3, 0.88), bias=0.04)
    flap = poly_mask(path((65, 102), (75, 102), (75, 106), [(72, 107.5), (68, 107.5), (65, 106)]), S)
    p_knot = c.part('knot', flap, 'leather', n_plane(0.3, -0.6, 0.75), bias=0.30)   # 덮개 = 몸통보다 한 단계 밝게 (덮개 · 몸통이 갈라 읽히게)

    # ── 8 얼굴 (설계도 y18~21 · 넓은 얼굴 · 턱은 둥글게)
    face = poly_mask(path((45, 67), (82, 67), (83, 75), [(82.5, 80), (79, 84), (73, 85)], (54, 85), [(48, 84), (45, 80), (45, 75)]), S)
    p_face = c.part('face', face, 'skin', n_plane(0.1, -0.3, 0.95), bias=0.0, smooth=0)   # 바탕 한 단계 — 밝은 면 · 그늘은 아래 덧그림

    # ── 9 깃 (설계도 x10~11 · y21 — 왼쪽으로 세운 짙은 보라 깃 · 목둘레 띠)
    collar = poly_mask(path((41, 85), [(41.5, 81.5), (44, 79.5), (47.5, 80)], (51, 84.5), (72, 84.5), (75, 83.5), (76.5, 86.5), (48, 87.5), (43, 88)), S)
    p_collar = c.part('collar', collar, 'robe', n_plane(-0.2, -0.5, 0.8), bias=-0.46)

    # ── 10 흰 머리카락 (두 갈래 뿔처럼 솟은 꼭대기 · 사이 골 · 양옆 뾰족한 자락이 볼까지)
    #   꼭대기 폭은 설계도 폭 안 (갈래 꼭지 x53 · x78 — 설계도 x13 · x19)
    hair_pts = path((44.5, 77), [(42, 73), (40.5, 67), (40.5, 62)], [(40.5, 59.5), (42, 57), (44, 55)],
                    [(45, 54), (49.5, 51), (53, 45)],                 # 왼 갈래 바깥 → 꼭지 (안으로 오목 — 뾰족하게)
                    [(54.5, 50), (57, 53.5), (63, 57)],               # 왼 갈래 안쪽 → 사이 골
                    [(69, 53.5), (75, 51), (79, 45)],                 # 오른 갈래 안쪽 → 꼭지
                    [(80.5, 50), (82.5, 53), (84.5, 55)], [(86.5, 57.5), (87, 60), (87, 62)],
                    [(87.5, 65), (87, 71), (83.5, 77)],                # 오른 자락 끝
                    [(81.5, 73), (80, 70), (79, 69)], (48, 69), [(47, 71), (46, 74), (44.5, 77)])
    hair_n = n_dome(64, 66, 25, 20)
    p_hair = c.part('hair', poly_mask(hair_pts, S), 'hair', hair_n, bias=0.0)

    # ── 11 고글 띠 (이마 · 머리 둘레를 따라 살짝 휨) · 고글 알 둘 (설계도 x13~14 · x16~17)
    band = poly_mask(path((42, 64), [(52, 62.5), (74, 62.5), (86, 64)], (86, 68.5), [(74, 67.5), (52, 67.5), (42, 68.5)]), S)
    p_band = c.part('band', band, 'leather', n_plane(0.1, -0.5, 0.86), bias=0.06)
    p_lensl = c.part('lens_l', ellipse_mask(56, 65.5, 4.8, 4.2, S), 'lens', n_ball(57, 64, 6, 5), bias=-0.08)
    p_lensr = c.part('lens_r', ellipse_mask(69.5, 65.5, 4.8, 4.2, S), 'lens', n_ball(70.5, 64, 6, 5), bias=-0.02)

    c.tidy(4)
    c.contact_lines(skip={('pants', 'boot_l'), ('torso', 'belt'), ('arm_l', 'belt'), ('arm_r', 'belt'), ('face', 'collar'),
                          ('belt', 'cork'), ('cork', 'potion'), ('face', 'hair'), ('hair', 'band'), ('tails', 'knot'), ('belt', 'knot'), ('belt', 'tails')})

    # ───────────── 덧그림 (큰 형태 위주 — 잔점 · 같은 간격 줄 없음)
    deep = ndimage.distance_transform_cdt(np.pad(c.mat >= 0, 1), metric='taxicab')[1:-1, 1:-1] >= 4

    def fold(pts, parts, side=-1, dark=-1, ridge=1):
        """긴 주름: 골 2칸(빛 반대쪽) + 등성이 1칸(빛 쪽) — 실루엣 3칸 안은 안 파서 외곽이 두꺼워 보이지 않게"""
        cells = [(x, y) for x, y in line_cells(path(*pts)) if 0 <= y < S and 0 <= x < S and deep[y, x]]
        c.nudge(cells + [(x + side, y) for x, y in cells], dark, parts)
        if ridge:
            c.nudge([(x - side, y) for x, y in cells], ridge, parts)

    # 머리카락: 갈래마다 꼭지에서 흘러내리는 긴 가닥 골 (2~3개) · 사이 골 깊은 그늘 · 띠 위에 얹힌 앞머리 그늘
    fold([[(53, 48), (51, 53), (48, 58), (46, 63)]], p_hair)
    fold([[(78, 48), (77, 53), (74, 58), (71, 62)]], p_hair, ridge=0)
    fold([[(63, 58), (62.5, 61), (62, 63)]], p_hair, side=0, dark=-2, ridge=0)          # 두 갈래 사이 골
    fold([[(84, 66), (83.5, 70), (82, 74)]], p_hair)                                     # 오른 자락
    c.nudge(line_cells([(46, 62), (80, 62)]), -1, p_hair)                                # 띠 위 앞머리 그늘 (띠가 누름)
    lock = (c.pid == p_hair) & (c.xx <= 47) & (c.yy >= 63) & (c.lvl == 0)               # 왼 자락: 그늘이지만 흰 머리로 읽히게 한 단계 위
    c.nudge([(int(x), int(y)) for y, x in zip(*np.nonzero(lock))], 1, p_hair)
    # 얼굴: 머리 · 띠 밑 그늘 1줄 (설계도 i) · 양 볼 가장자리 그늘
    c.nudge(line_cells([(47, 69), (81, 69)]), -1, p_face)
    # (판정 1회차: 왼 볼 넓은 그늘 면 · 오른 볼 밝은 면이 얼룩 → 턱이 돌아가는 왼 가장자리 2칸 띠만 그늘 · 볼은 붉은 기 덩어리 하나씩)
    # (판정 2회차 · 얼굴 한 색 256칸 → 200칸 이하: 빛 중심을 오른쪽 위로 둔 타원 바깥 = 왼뺨 · 턱 아래로 두꺼워지는 그늘 초승달 1단
    #   — 곡면 경계 하나라 볼 화장 얼룩 · 같은 간격 줄이 생기지 않음)
    turn = face & ~ellipse_mask(FACE_C[0], FACE_C[1], FACE_C[2], FACE_C[3], S) & (c.lvl == 2)
    c.nudge([(int(x), int(y)) for y, x in zip(*np.nonzero(turn))], -1, p_face)
    c.nudge(line_cells([(52, 84), (74, 84)]), -1, p_face)                                # 턱 아래 그늘
    # 빛 받는 면: 이마 ~ 오른 볼 (위 · 오른쪽 광원) — 매끈한 곡선 경계 하나로 얼굴을 두 면으로 (넓은 한 색 평면 · 얼룩 둘 다 피함)
    lit = face & ellipse_mask(71, 69.5, 15, 10, S) & (c.lvl == 2)
    c.nudge([(int(x), int(y)) for y, x in zip(*np.nonzero(lit))], 1, p_face)
    c.nudge(line_cells([(61, 81), (64, 81)]), -1, p_face)                                # 작은 입 (짧은 그늘 1줄)
    # 고글: 알마다 반사 한 줄 (왼 위 사선 2칸) · 띠 아랫단 그늘
    for lx in (53, 66):
        c.set_lvl([(lx, 64), (lx + 1, 64), (lx, 63), (lx + 1, 63), (lx + 2, 63)], 2, (p_lensl, p_lensr))   # 반사 덩어리 (2칸 이상)
    c.nudge(line_cells([(42, 68), (86, 68)]), -1, p_band)
    # 로브: 몸통 가운데 앞섶 (설계도 x14~15 의 밝은 o — 빛 받는 가슴) · 소매 안쪽 그늘 (설계도 q) · 소매 끝단
    chest = ellipse_mask(63.5, 89.5, 7.5, 4.5, S)                                          # 빛 받는 가슴 (설계도 x14~15 의 o)
    c.nudge([(int(x), int(y)) for y, x in zip(*np.nonzero(chest))], 1, p_torso)
    # (판정 2회차: 짧은 어두운 주름 흩뿌림 → 긴 주름 둘 — 어깨 밑에서 허리띠까지 이어 당겨진 천 · 등성이 1칸으로 어둡기만 늘지 않게)
    fold([[(52, 86), (53, 91), (54.5, 96), (56, 99)]], p_torso, ridge=1)
    fold([[(70, 86), (69.5, 91), (68.5, 95), (67, 99)]], p_torso, side=1, ridge=1)
    c.nudge([(x, y) for y in (99, 100) for x in range(48, 74)], -1, p_torso)           # 허리띠 위로 부푼 천 아래 그늘
    fold([[(40, 89), (40, 95)]], p_arml, side=1, dark=1, ridge=0)                        # 왼 소매 바깥 등성이 (설계도 o)
    for pa in (p_arml, p_armr):
        c.nudge([(x, 95) for x in range(36, 86)] + [(x, 96) for x in range(36, 86)], -1, pa)   # 소매 끝 접힌 그늘
    # 주머니: 덮개 밑 그늘 1줄 (덮개 곡선을 따라) · 바닥선 그늘 1줄 · 오른쪽 위 림은 아래 rim 에서
    c.nudge([(x, y + 1) for x, y in line_cells(path([(65, 106), (68, 107.5), (72, 107.5), (75, 106)]))], -1, p_tails)
    c.nudge(line_cells([(66, 112), (74, 112)]), -1, p_tails)
    # 허리띠: 아랫단 그늘
    c.nudge(line_cells([(48, 104), (65, 104.5)]), -1, p_belt)                            # 끈 아랫단 그늘 (매듭 왼쪽까지)
    # 바지: 가운데 골 (설계도 z) · 오른 다리 빛 (설계도 B) · 무릎 아래 접힌 그늘
    fold([[(61.5, 105), (61.5, 112)]], p_pants, side=0, ridge=0)
    # (판정 1회차: 오른 다리 세로 밝은 줄이 정체불명 → 지움 · 오른 다리 빛은 림 1칸만)
    # 물약: 왼 위 유리 반사 한 줄 · 아래 짙은 물 (병 속 액체 면)
    c.set_lvl([(52, 104), (53, 104), (52, 105), (53, 105), (53, 103)], 2, p_pot)          # 반사 덩어리 (2칸 이상)
    c.nudge(line_cells([(52, 109), (59, 109)]) + line_cells([(53, 110), (58, 110)]), -1, p_pot)
    # 왼손: 소매에서 늘어뜨린 손 (판정 2회차 묶음 공통 「양손이 같은 주먹 틀」 → 엄지만 앞으로 · 손가락 끝 둥글게 · 경계선은 엄지 옆 짧게)
    c.stamp(['..cccc..',
             '.cccccb.',
             'bbcccbbb',
             'bbaaabbb',
             '.bbbbbbb',
             '..bbbbb.'], 39, 96, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 2)})
    # 오른손: 손가락을 아래로 편 손 (엄지가 앞 · 손가락 사이 경계 1줄)
    c.stamp(['..cccc..',
             '.cccccbb',
             'ccccbbbb',
             'cccabbbb',
             '.ccabbb.',
             '..bbbb..'], 75, 96, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 2)})
    # 장화: 접힌 목 (위 1줄 밝게)
    for pb in (p_bootl, p_bootr):
        c.nudge([(x, 118) for x in range(40, 82)], 1, pb)

    # 림 라이트: 오른쪽 위 실루엣 안쪽 — 머리 2칸 · 로브 2칸 · 바지 · 장화 1칸
    c.rim((p_hair,), RIM_DIR, width=2, level=-1, thresh=0.3)
    c.absorb(c.pid == p_hair, min_size=3)
    c.rim((p_armr, p_arml, p_torso), RIM_DIR, width=2, level=-1, thresh=0.3)
    c.absorb(np.isin(c.pid, (p_armr, p_arml, p_torso, p_collar)), min_size=3)
    c.rim((p_pants, p_bootr, p_face), RIM_DIR, width=1, by=1, thresh=0.5)
    c.absorb(np.isin(c.pid, (p_pants, p_face, p_band, p_belt, p_tails, p_knot, p_lensl, p_lensr, p_pot, p_bootl, p_bootr, p_handl)), min_size=3)

    # 볼 붉은 기: 눈 아래 바깥쪽에 3×2 덩어리 하나씩 (피부 둘째 단계 · 좌우 같은 자리 — 그늘 단계는 쓰지 않음)
    c.stamp(['rrr', 'rrr'], 51, 79, {'r': ('skin', 1)})
    c.stamp(['rrr', 'rrr'], 74, 79, {'r': ('skin', 1)})
    # 눈 (검은 눈 · 설계도 x14 · x17 → 가운데 57.5 · 69.5) + 반짝 1칸
    eye = ['aa', 'aa', 'aa', 'aa']
    for ex in (56, 68):
        c.stamp(eye, ex, 73, {'a': EYE})
    dots = []
    c.outline(warm_parts=(p_handl, p_handr))
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
