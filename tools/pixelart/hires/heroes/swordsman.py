# 검사 메뉴 초상 128칸 (그래픽 가독성 개편 2단계 묶음 3 — 도적 시안 규칙(README.md) 그대로)
#   설계도 = Resources/Characters/swordsman.png 첫 그림(32칸 · 몸 17×20) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 12~16 붉은 깃털 장식(꼭지 x13 · 왼쪽으로 흘러내림) · 16~18 은빛 투구 · 19 짙은 투구 테 + 금 징 둘(x11 · x18)
#     20~21 얼굴 (왼쪽 갈색 머리 한 줌 · 검은 눈 둘 x14 · x17) · 22 은빛 어깨받이 둘 · 22~26 파란 갑옷 (가운데 밝은 등줄 · 가슴 금 장식)
#     23~27 양옆 붉은 망토 (왼쪽 그늘 · 오른쪽 밝음 · 아랫단 회색 안감) · 25 양손 · 26 금 허리띠 · 27 짧은 갑옷 치마 · 28~29 바지 · 30~31 갈색 장화
#   판정 1회차 → 투구 테 얇게 + 왼 머리가 테 밑으로 · 어깨받이 · 손 · 금 징 좌우 다르게 (오른손 = 허리 칼자루) · 금 허리띠 + 버클 → 얇은 파란 판금 테 + 짙은 가죽 띠
#     (넓은 금 띠 + 가운데 버클은 다른 영웅과 묶음 반복 무늬) · 쇠 · 금 가장 밝은 단계를 얼굴(Y 199)보다 어둡게
#   광원 = 설계도를 이어받아 위 · 살짝 오른쪽 (망토 오른쪽이 밝음 · 투구 빛이 가운데 오른쪽)
#   흑백 계층: 얼굴 · 눈 (Y 199 가 그림 최고 · 눈 반짝 제외) > 투구 빛 1줄 > 깃털 > 투구 · 어깨받이 > 갑옷 > 망토 > 바지 · 장화 — 쇠 가장 밝은 단계는 투구 위 테 1줄만
import numpy as np
from scipy import ndimage

from .portrait import Canvas, Map, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, n_cyl_v, despeckle, OUT1

SIZE = 128
K = 4
M = Map(K, 0, -3)
DESIGN = 'Assets/Resources/Characters/swordsman.png'     # 시트 첫 칸 (32×32) 이 설계도

# 재질: 어두움 → 밝음 (어두운 쪽은 차갑게, 밝은 쪽은 따뜻하게) · 재질당 ≤ 5단 · 바탕 = 설계도 색 근처 · 전체 ≤ 32색
MATERIALS = {
    # 붉은 천 (깃털 장식 · 망토 같은 천): 설계도 e #560E36 · B #9E243C · c #E03840 · F #CE4448 · d #FC785C
    'red':   dict(ramp=[hx('3A0C2A'), hx('6E1838'), hx('A4283E'), hx('D8424A'), hx('FA7C5E')], cuts=[0.30, 0.52, 0.84, 9.0]),
    # 투구 · 어깨받이 · 칼자루 쇠: 바탕 Y 약 143 · 빛 Y 약 173 · 가장 밝은 단계(Y 약 196, 위 테 1줄)도 얼굴(Y 199)보다 어둡게
    'steel': dict(ramp=[hx('383A60'), hx('5E6488'), hx('868EB2'), hx('A4ACCA'), hx('BCC4DE')], cuts=[0.34, 0.56, 0.86, 9.0]),
    # 금 장식 (투구 징 · 가슴 장식만 — 허리띠는 금 아님): 설계도 m #B46826 · D #EAAA3A (밝은 #FEDE6C 는 Y 215 라 얼굴보다 밝아 뺌)
    'gold':  dict(ramp=[hx('7A4018'), hx('B46826'), hx('E6A63A')], cuts=[0.40, 0.70], warm=True),
    # 파란 갑옷: 설계도 u #222565 · C #283694 · v #3E5ECA · w #6A96F2 · z #B4D6FF
    'armor': dict(ramp=[hx('181A4C'), hx('26307E'), hx('3A58C2'), hx('5A86E4'), hx('9CC2F8')], cuts=[0.32, 0.56, 0.88, 9.0]),
    'skin':  dict(ramp=[hx('8A4E52'), hx('CA7E70'), hx('F2BC96')], cuts=[0.55, 0.80], warm=True),
    # 갈색 가죽 (머리카락 · 장화): 설계도 o #6F4129 · M #563432 · N #341E2C
    'leather': dict(ramp=[hx('2E1C26'), hx('4A2C30'), hx('6C4030'), hx('94603E')], cuts=[0.40, 0.68, 1.0], warm=True),
    # 회청 천 (치마 · 바지 · 망토 안감): 설계도 I #2E2B4A · K #3E4062 · J #5E6486 · H #5F5E7C · L #627E9E
    'cloth': dict(ramp=[hx('221E3A'), hx('343452'), hx('4E5476'), hx('707A9C')], cuts=[0.36, 0.62, 0.95]),
}
EYE = OUT1                       # 검은 눈 (설계도 q #120B20 → 외곽색과 같은 색 · 색 수 아낌)
EYE_HOT = hx('BCC4DE')           # 눈 반짝 (쇠 가장 밝은 단계와 같은 색)
LIGHT = (0.25, -0.8, 0.55)
RIM_DIR = (0.4, -0.92)           # 림 라이트가 드는 바깥 방향 (위 · 조금 오른쪽)


# ───────────── 형태 모델 (법선 = x 오른쪽 · y 아래 · z 보는 쪽)
def n_helm(cx, cy, rx, ry):
    """투구: 위가 납작한 주발 — 옆은 거의 곧게 내려오고(원기둥) 위 테두리만 둥글게 위를 봄"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.97, 0.97)
        v = np.clip((yy + 0.5 - cy) / ry, -0.97, 0.15)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.04, 1))
    return f


def n_ball(cx, cy, rx, ry):
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.97, 0.97)
        v = np.clip((yy + 0.5 - cy) / ry, -0.97, 0.97)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def n_plume(cx, cy, rx, ry):
    """깃털 장식: 왼쪽으로 휘어 내려오는 굵은 다발 — 둥근 단면, 위쪽이 빛을 받음"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.97, 0.97)
        v = np.clip((yy + 0.5 - cy) / ry, -0.97, 0.6) * 0.8
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def draw():
    c = Canvas(SIZE, MATERIALS, light=LIGHT)
    S = SIZE

    # ── 1 망토 (몸 뒤 · 어깨에서 양옆으로 벌어져 무릎 위까지 — 설계도 x8~9 · x21~22, y23~27)
    cape = poly_mask(path((37, 85), (87, 85), [(91, 88), (93.5, 96), (94.6, 110.6)], [(91, 111), (88, 109.5), (85, 108)],
                          (38, 108), [(35, 109.5), (32, 111), (28.4, 110.6)], [(29.5, 96), (32, 88), (37, 85)]), S)
    p_cape = c.part('cape', cape, 'red', n_cyl_v(62, 34, tilt=0.1), bias=0.03)
    # 망토 아랫단 안감 (회청 · 접혀 올라온 자락이 사선 — 설계도 H #5F5E7C)
    yy, xx = c.yy, c.xx
    hem = cape & (((xx < 42) & (yy >= 103.5 + (xx - 29) * 0.25)) | ((xx > 82) & (yy >= 106.5 - (xx - 83) * 0.25)))
    p_hem = c.part('hem', hem, 'cloth', n_plane(0.2, -0.6, 0.8), bias=0.02)

    # ── 2 바지 (설계도 y28~29 · 두 다리 사이 그늘) · 갑옷 치마 (y27)
    legs = poly_mask(path((46.5, 104), (74.5, 104), (74, 118), (46.5, 118)), S)
    p_legs = c.part('legs', legs, 'cloth', n_cyl_v(60, 16), bias=-0.04)
    skirt = poly_mask(path((44, 103), (79, 103), [(80, 106), (79.5, 109.5)], [(72, 111.2), (60, 111.4), (48, 111)], [(44, 109.5), (43.5, 105)]), S)
    p_skirt = c.part('skirt', skirt, 'cloth', n_cyl_v(61, 19, tilt=0.15), bias=0.0)

    # ── 3 장화 (설계도 y30~31 — 갈색 · 발끝이 바깥으로)
    boot_l = poly_mask(path((47, 117), (57, 117), (57, 125), (43.5, 125), [(44, 122.5), (45, 121), (47, 120.5)]), S)
    boot_r = poly_mask(path((63.5, 117), (73.5, 117), [(74, 120), (76, 121), (77.5, 122)], (77.5, 125), (63.5, 125)), S)
    p_bootl = c.part('boot_l', boot_l, 'leather', n_plane(-0.1, -0.6, 0.8), bias=-0.12)
    p_bootr = c.part('boot_r', boot_r, 'leather', n_plane(0.3, -0.6, 0.75), bias=-0.06)

    # ── 4 갑옷 몸통 (설계도 x12~18 · y22~26 — 가운데 앞이 솟은 가슴판)
    torso = poly_mask(path((46.5, 84.5), (77.5, 84.5), [(78, 92), (77, 99)], (77, 104), (47, 104), [(47, 99), (46, 92)]), S)
    p_torso = c.part('torso', torso, 'armor', n_cyl_v(62.5, 17, tilt=0.15), bias=0.08)

    # ── 5 팔 (파란 소매 · 설계도 x10~11 · x19~20) — 망토와 몸 사이
    arm_l = poly_mask(path((39.5, 87), (48, 87), (48.5, 97), [(46, 98.5), (42, 98.5), (39.5, 97)]), S)
    arm_r = poly_mask(path((76, 87), (84.5, 87), (84.5, 97), [(82, 98.5), (78, 98.5), (75.5, 97)]), S)
    p_arml = c.part('arm_l', arm_l, 'armor', n_cyl_v(44, 6), bias=0.04)
    p_armr = c.part('arm_r', arm_r, 'armor', n_cyl_v(80.5, 6), bias=0.25)

    # ── 6 갑옷 아랫단 (설계도 y26 금 띠 자리 — 판정: 넓은 금 띠 + 버클이 다른 영웅과 반복 → 얇은 파란 판금 테 3칸 + 짙은 가죽 띠 2칸 · 버클 없음)
    belt = poly_mask(path((46.5, 100.5), [(62, 101.4), (77.5, 100.5)], (77.5, 103.5), [(62, 104.2), (46.5, 103.5)]), S)
    p_belt = c.part('belt', belt, 'armor', n_cyl_v(66, 17, tilt=0.1), bias=-0.05)    # (판정 2회차: 밝은 가로 직선 → 중간 단계 · 오른쪽만 한 단계 빛)
    strap = poly_mask(path((46, 103.5), [(62, 104.2), (78, 103.5)], (78, 105.5), [(62, 106.3), (46, 105.5)]), S)
    p_strap = c.part('strap', strap, 'leather', n_plane(0.1, -0.2, 0.95), bias=-0.3, smooth=0)

    # (판정 2회차: 허리 칼자루가 작아 안 읽힘 → 지움 — 설계도 첫 칸에 칼 없음)

    # ── 7 손 (설계도 y25 — 좌우 다르게: 왼손 쥔 주먹 · 오른손 허리 판금 테에 걸친 편 손)
    p_handl = c.part('hand_l', poly_mask(path((39.5, 96), (46.5, 96), (47.5, 98), (47, 102), [(45, 103), (41.5, 103), (39.5, 101)]), S),
                     'skin', n_ball(44, 98, 5, 4.5), bias=-0.1)
    p_handr = c.part('hand_r', poly_mask(path((77, 95.5), (84, 95.5), (85, 98), (84.5, 104), [(83, 105.5), (80, 105.5)], (77.5, 103), (76.5, 99)), S),
                     'skin', n_ball(81.5, 98, 5, 4.5), bias=0.0)

    # ── 8 어깨받이 (은빛 둥근 판 둘 · 설계도 y22)
    #   (판정: 기계적 좌우 대칭 → 왼쪽 = 빛 반대라 작고 처진 판 한 장 · 오른쪽 = 빛 받는 겹판 두 장, 크기 · 기울기 다르게)
    #   (판정 2회차: 명암만 다르고 모양이 같음 → 왼쪽 = 둥글고 처진 한 장 · 오른쪽 = 바깥으로 벌어진 각진 판 + 아래 겹판 한 장이 삐져나옴)
    pad_l = ellipse_mask(44.2, 89.6, 6.0, 4.6, S) & (yy <= 93)
    pad_r = poly_mask(path((73.5, 86.5), [(78, 83.6), (84, 83.4), (88.5, 86.5)], (89.5, 90.5), (86, 92.5), (84.5, 95), (78, 94.5), (75, 92), (73.5, 89.5)), S)
    p_padl = c.part('pad_l', pad_l, 'steel', n_ball(46, 93, 9, 8), bias=-0.1)       # 둥근 판의 아래 끝이 중심 — 판 전체가 위를 봄
    p_padr = c.part('pad_r', pad_r, 'steel', n_ball(80, 92, 9, 8), bias=0.04)

    # ── 9 얼굴 (설계도 y20~21 — 투구 테 아래 · 턱은 갑옷 목깃에 묻힘)
    face = poly_mask(path((40, 75), (81, 75), (81, 82), [(80.5, 84.5), (77, 85.6), (72, 85.6)], (50, 85.6), [(44, 85.6), (40.5, 84.5), (40, 82)]), S)
    p_face = c.part('face', face, 'skin', n_cyl_v(66, 30), bias=0.2, smooth=1.2)
    # 왼쪽 머리카락 한 줌 (설계도 o x11 y20 · 투구 밑으로 삐져나옴)
    #   (판정: 테 밑에서 끊겨 챙 모자처럼 → 테 바로 밑부터 귀 옆으로 흘러 내려 어깨받이까지 이어짐)
    hair = poly_mask(path((39.5, 75), (51, 75), [(50, 77.5), (48, 80), (46.5, 83)], [(46, 85), (44.5, 86.5), (42, 86.5)], (39.5, 84)), S)
    p_hair = c.part('hair', hair, 'leather', n_plane(0.1, -0.7, 0.7), bias=-0.06)

    # ── 10 깃털 장식 (투구 꼭대기에서 솟아 왼쪽으로 흘러내림 · 설계도 x11~15 y12~16) — 투구 뒤
    plume = poly_mask(path((51.5, 44.5), (55.5, 44.5), [(58.5, 47), (62, 53), (65.5, 59)], (63, 62), (49, 62),
                           [(47, 66), (45, 67.5), (42, 66)], [(40.5, 61), (42, 53), (49, 46)]), S)
    p_plume = c.part('plume', plume, 'red', n_plume(56, 54, 13, 12), bias=0.3)

    # ── 11 투구 (설계도 y15~19 — 위가 납작한 주발 · 옆은 곧게 · 꼭대기 폭 설계도 안)
    helm = poly_mask(path((47, 60), [(53, 57.2), (61, 56.6), (68, 57)], [(76, 58), (80.5, 61.5), (82, 66)], [(82.6, 70), (83.6, 74)], (83.6, 76.5),
                          (37.4, 76.5), [(37.4, 73), (38.4, 69)], [(39, 64), (42, 61.5), (47, 60)]), S)
    helm_n = n_helm(62, 76, 25, 19)
    p_helm = c.part('helm', helm, 'steel', helm_n, bias=-0.17)
    # 투구 테 (짙은 쇠 띠 — 설계도 l #34305E) · 얼굴 위 그늘을 만듦
    #   (판정: 5칸 굵은 테가 얼굴을 눌러 챙 모자처럼 → 3칸으로 얇게)
    band = poly_mask(path((37.4, 73.5), [(50, 74.3), (72, 74.3), (83.6, 73.5)], (83.6, 77), (37.4, 77)), S)
    p_band = c.part('band', band, 'steel', n_plane(0.1, 0.2, 0.95), bias=-0.4, smooth=0)
    helms = (p_helm, p_band)

    c.tidy(4)
    c.contact_lines(skip={('cape', 'hem'), ('legs', 'skirt'), ('helm', 'band'), ('face', 'hair'), ('torso', 'belt'),
                          ('arm_l', 'hand_l'), ('arm_r', 'hand_r'), ('belt', 'hand_r'), ('belt', 'strap'), ('strap', 'hand_r')})

    # ───────────── 덧그림 (큰 형태 위주 — 잔점 · 같은 간격 줄 없음)
    deep = ndimage.distance_transform_cdt(np.pad(c.mat >= 0, 1), metric='taxicab')[1:-1, 1:-1] >= 4

    def fold(pts, parts, side=-1, dark=-1, ridge=1):
        """긴 주름: 골 2칸(빛 반대쪽) + 등성이 1칸(빛 쪽) — 실루엣 3칸 안은 골을 안 파서 외곽이 두꺼워 보이지 않게"""
        cells = [(x, y) for x, y in line_cells(path(*pts)) if 0 <= y < S and 0 <= x < S and deep[y, x]]
        c.nudge(cells + [(x + side, y) for x, y in cells], dark, parts)
        if ridge:
            c.nudge([(x - side, y) for x, y in cells], ridge, parts)

    # 깃털: 다발 사이 긴 골 둘 (꼭지에서 왼쪽 아래로 휘어짐) · 오른 등성이 빛
    fold([[(54, 47), (51, 52), (47, 58), (44, 64)]], p_plume)
    fold([[(58, 50), (57, 55), (54, 59), (51, 62)]], p_plume, ridge=0)
    # 투구: 가운데 솟은 등줄 (꼭대기 → 테 · 빛 쪽 1칸 밝게 · 그늘 쪽 골) · 테 바로 위 그늘 1줄
    c.nudge(line_cells([(66, 58), (66, 70)]), 1, p_helm)
    c.nudge(line_cells([(65, 59), (65, 70)]), -1, p_helm)
    c.nudge(line_cells([(39, 70), (82, 70)]), -1, p_helm)
    # 투구 테: 아래 끝 그늘 (얼굴 위로 드리운 그림자는 얼굴 쪽) · 금 징 둘 (설계도 m x11 · x18)
    c.nudge(line_cells([(38, 76), (83, 76)]), -1, p_band)
    #   (판정 2회차: 좌우 징이 거울 대칭 → 왼 징은 지우고 빛 받는 오른쪽 고정쇠 하나만 · 테 가운데에서 조금 오른쪽)
    c.stamp(['GGg', 'Ggg'], 72, 74, {'g': ('gold', 1), 'G': ('gold', 2)})
    # 얼굴: 투구 테 그림자 (위 2줄) · 왼뺨 그늘 (설계도 s · t)
    c.nudge(line_cells([(41, 77), (80, 77)]), -1, p_face)
    #   왼뺨 그늘은 머리카락이 덮으므로 머리 경계를 따라 생기지 않게 뺌 (판정 2회 대비 — 머리 옆 사선 띠) · 머리카락 긴 결 하나
    c.nudge(line_cells(path([(49, 76), (47, 80), (44, 85)])) + line_cells(path([(48, 76), (46, 80), (43, 85)])), -1, p_hair)
    # 갑옷: 가슴판 가운데 등줄 (밝은 1칸 + 그늘 1칸 · 설계도 w · z) · 양옆 판 경계 골 (설계도 u x12 · x18)
    #   (판정 2회차: 가운데 밝은 세로 직선 + 아래 가로 직선 = 역T자 → 지우고 오른 가슴에서 허리로 휘는 긴 주름 하나)
    fold([[(69, 87), (66, 92), (63, 96), (57, 99)]], p_torso, side=-1, ridge=1)
    fold([[(51, 88), (51, 95), (50, 100)]], p_torso, side=-1, ridge=1)
    fold([[(73, 88), (73, 95), (74, 100)]], p_torso, side=1, ridge=0)
    # 가슴 금 장식 — (판정 2회차: 가슴 한가운데 작은 금 장식이 묶음 반복 → 왼 가슴 위 목깃 옆으로 옮긴 작은 금 문장 · 위 테만 빛)
    c.stamp(['GGGGg',
             'GGggd',
             'Gggdd',
             '.gdd.',
             '..d..'], 53, 89, {'g': ('gold', 1), 'G': ('gold', 2), 'd': ('gold', 0)}, part=p_torso)
    # 아래 가슴판 이음매 (가슴판이 배 판을 덮는 곡선 — 가운데가 처짐 · 아래 1줄 그늘, 위 1줄 빛은 오른쪽 반만)
    seam = line_cells(path([(48, 95), (56, 98.5), (68, 98.5), (76, 95)]))
    c.nudge([(x, y + 1) for x, y in seam], -1, p_torso)
    c.nudge([(x, y) for x, y in seam if x >= 60], 1, p_torso)
    # 팔: 어깨받이 밑 그늘 · 팔꿈치 접힘 (설계도 C #283694)
    fold([[(43, 94), (43, 99)]], p_arml, ridge=0)
    fold([[(79, 94), (80, 99)]], p_armr, side=0, ridge=0)
    # 갑옷 아랫단 판금 테: 가죽 띠는 무늬 없이 (판정 2회차: 테 위 밝은 가로 직선 지움)
    # 오른 어깨받이 겹판 경계 골 (왼쪽엔 없음 — 아래 겹판이 한 단 내려앉음)
    c.nudge(line_cells(path([(75, 91), (80, 91.5), (87, 90.5)])), -1, p_padr)
    c.nudge(line_cells(path([(76, 92), (80, 92.5), (86, 91.5)])), -1, p_padr)
    # 망토: 긴 세로 주름 (왼쪽 둘 · 오른쪽 둘) · 팔 뒤 안쪽 그늘 (설계도 y #451432)
    fold([[(33, 92), (32, 100), (31, 107)]], p_cape)
    fold([[(37, 96), (36.5, 104)]], p_cape, ridge=0)
    fold([[(89, 92), (90, 100), (91, 106)]], p_cape, side=1, ridge=1)
    fold([[(86, 96), (86.5, 104)]], p_cape, side=1, ridge=0)
    # 안감: 접힌 윗단 빛 1줄
    c.nudge([(x, int(round(103.5 + (x - 29) * 0.25)) + 1) for x in range(30, 42)], 1, p_hem)
    c.nudge([(x, int(round(106.5 - (x - 83) * 0.25)) + 1) for x in range(83, 94)], 1, p_hem)
    # 치마: 판 사이 골 둘 (가운데 · 짧게) · 아랫단 그늘
    c.nudge(line_cells([(60, 105), (60, 109)]), -1, p_skirt)
    c.nudge(line_cells([(51, 106), (50, 109)]) + line_cells([(71, 105), (72, 108)]), -1, p_skirt)   # 옆 판 틈 (길이 · 기울기 다르게)
    c.nudge(line_cells([(46, 109), (77, 109)]), -1, p_skirt)
    # 바지: 두 다리 사이 골 (설계도 I x14~15) · 오른 무릎 빛 (설계도 L)
    c.nudge(line_cells([(60, 110), (60, 117)]) + line_cells([(59, 111), (59, 117)]), -1, p_legs)
    c.nudge(line_cells([(70, 111), (70, 116)]) + line_cells([(71, 111), (71, 116)]), 1, p_legs)
    # 장화: 접힌 목 (위 2줄 밝게) · 오른 발끝 빛
    for pb in (p_bootl, p_bootr):
        c.nudge([(x, 118) for x in range(40, 82)] + [(x, 119) for x in range(40, 82)], 1, pb)
    # 왼손: 쥔 주먹 (피부 2톤 b 바탕 · c 빛 + 손가락 경계선 a 1줄만)
    c.stamp(['..cccc.',
             '.bbbccc',
             'bbbbbbc',
             '.aaaaa.',
             'bbbbbbb',
             '.bbbbb.'], 40, 97, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 2)})
    # 오른손: 허리 판금 테에 걸친 편 손 (손등 빛 · 손가락이 아래로 · 세로 손가락 경계 1줄 — 왼 주먹과 모양 · 크기 다름)
    c.stamp(['.cccccc.',
             'ccccccbb',
             'cccccbbb',
             'bbbbabbb',
             'bbbbabbb',
             '.bbbabb.',
             '..bbbb..'], 77, 97, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 2)})

    # 림 라이트: 위 · 오른쪽 실루엣 안쪽 — 깃털 · 망토 2칸 · 투구 위 테 1줄 (쇠 가장 밝은 단계는 이것뿐)
    c.rim((p_plume,), RIM_DIR, width=1, level=-1, thresh=0.3)
    c.rim((p_helm,), RIM_DIR, width=1, level=-1, thresh=0.45)
    c.rim((p_cape,), RIM_DIR, width=2, level=-2, thresh=0.25)
    c.rim((p_padr, p_armr, p_bootr), RIM_DIR, width=1, by=1, thresh=0.5)
    # 1~2칸 조각(4-연결) 흡수 — 림 · 주름 끝이 하나로 이어진 띠가 되게
    c.absorb(np.isin(c.pid, (p_plume, p_cape, p_helm, p_torso, p_legs, p_skirt, p_hem, p_face, p_padl, p_padr, p_arml, p_belt)), min_size=3)

    # 눈 (검은 눈동자 · 반짝 1칸 · 설계도 x14 · x17 → 가운데 57 · 69)
    eye = ['aab', 'aaa', 'aaa', '.a.']
    for ex in (56, 68):
        c.stamp(eye, ex, 78, {'a': EYE, 'b': EYE_HOT})
    dots = [(58, 78), (70, 78)]
    c.outline(warm_parts=(p_handl, p_handr, p_bootl, p_bootr))
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
