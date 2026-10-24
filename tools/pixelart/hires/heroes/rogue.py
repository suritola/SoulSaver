# 도적 메뉴 초상 128칸 (그래픽 가독성 개편 2단계 묶음 3 · 시안 2 — 독립 판정 1회차 결함 반영판)
#   설계도 = Resources/Characters/rogue.png 첫 그림(rogue_0, 32칸 · 몸 15×19) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)  (발끝을 3칸 올려 아래 여백 ≥ 2칸 — 설계도는 발이 칸 바닥에 닿음)
#   설계도 읽기 (줄 = 설계도 y): 13~18 짙은 보라 두건 · 19 얼굴 그늘 + 노란 눈 둘(x14 · x17) · 20~21 얇은 붉은 목도리 + 왼쪽 꼬리(x7~9)
#     22~29 짙은 남색 망토 (오른 가장자리가 청회색으로 밝음 — 설계도 x19~20 의 u · t · C) · 25 양손 · 26 허리띠 · 30~31 작고 어두운 장화
#   1회차 판정 결함 → 이번 판: ⑧ 정체성 (밝은 셔츠 · 큰 장화 · 굵은 목도리) → 어깨~발목 망토 · 바탕 명도 설계도 수준(Y 50~65) · 다리는 망토 밑 그늘
#     밝기는 몸 전체가 아니라 위 · 오른쪽 림 라이트(가장자리 1~2칸)로 배경과 분리 · ③ 잔점 · 1칸 계단 솔기 · 버클 X 무늬 제거 → 큰 주름 몇 개로 형태
#     ⑤ 같은 간격 감개 줄 제거 · 손 = 왼손 주먹(손가락 마디) / 오른손 단검 역수 쥠 · 명도 계층: 눈 > 칼날 > 목도리 > 두건 > 망토 > 안쪽 그늘
#   광원 = 설계도를 이어받아 위 · 살짝 오른쪽 (설계도 위 +0.36 · 왼 −0.15)
#   2회차 판정 → 두건 림 2칸 이어진 띠 + 1~2칸 조각 흡수 · 왼손 피부 2톤 · 목도리 밝게(두건 + 15 이상) · 칼날 회청 + 밝은 1줄 · 두건 꼭대기 좁게 · 꼬리 한 갈래 (규칙 정리: README.md)
import numpy as np

from .portrait import Canvas, Map, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, despeckle

SIZE = 128
K = 4
M = Map(K, 0, -3)
DESIGN = 'Assets/Resources/Characters/rogue.png'     # 시트 첫 칸 (32×32) 이 설계도

# 재질: 어두움 → 밝음 (어두운 쪽은 차갑게, 밝은 쪽은 따뜻하게) · 재질당 ≤ 5단 · 바탕 = 설계도 색 근처
MATERIALS = {
    # 두건: 바탕 #4E3278 (설계도 b #4A2E68) · 빛 #6A4A9A · 림 #8E70C0
    'hood':  dict(ramp=[hx('26183F'), hx('3E2866'), hx('503480'), hx('6C50A4'), hx('9478CC')], cuts=[0.30, 0.50, 0.86, 9.0]),
    # 망토: 바탕 #3A3A60 (설계도 c #383252) · 빛 #50608A (설계도 t #4E5D7F) · 림 #7C98BC (설계도 u #5F7694 보다 한 단계 밝게)
    'cloak': dict(ramp=[hx('1C1830'), hx('2A2846'), hx('36365A'), hx('4C5C86'), hx('7A96BA')], cuts=[0.28, 0.52, 0.90, 9.0]),
    # 목도리: 바탕 Y 약 106 (두건보다 15 이상 밝게 — 흑백 계층 · 2회차 판정)
    'scarf': dict(ramp=[hx('5A1430'), hx('9E2038'), hx('DA3A40'), hx('F86E5C')], cuts=[0.40, 0.70, 1.02]),
    'skin':  dict(ramp=[hx('8A4E52'), hx('CA7E70'), hx('F2BC96')], cuts=[0.62, 0.95], warm=True),
    'leather': dict(ramp=[hx('2E1C26'), hx('4A2C30'), hx('6A4034'), hx('8E5A42')], cuts=[0.40, 0.70, 1.0], warm=True),
    'void':  dict(ramp=[hx('160826'), hx('24183C')], cuts=[0.75]),
    # 쇠: 칼날 바탕 회청 Y 약 122 · 가장 밝은 단계(Y 약 205)는 칼날 등 1줄만 — 눈(Y 약 220)보다 어둡게 (시선은 눈으로)
    'steel': dict(ramp=[hx('4E5A6E'), hx('6E7E96'), hx('C4D0DE')], cuts=[0.55, 9.0]),
}
EYE = hx('FFDE5A')
EYE_HOT = hx('FFF8CC')
LIGHT = (0.25, -0.8, 0.55)
RIM_DIR = (0.4, -0.92)           # 림 라이트가 드는 바깥 방향 (위 · 조금 오른쪽)


# ───────────── 형태 모델 (법선 = x 오른쪽 · y 아래 · z 보는 쪽)
def n_dome(cx, cy, rx, ry):
    """두건: 위가 둥근 반구 — 아래로 갈수록 앞을 봄 (천이 얼굴 앞으로 내려옴)"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.98, 0.98)
        v = np.clip((yy + 0.5 - cy) / ry, -0.98, 0.05)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.04, 1))
    return f


def n_cloak(cx, r, top, drop):
    """망토: 세로 원기둥(가운데가 앞) + 어깨에서 위를 보는 둥근 어깨 (top 에서 drop 칸 동안) + 아래로 갈수록 조금 그늘"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        v = -np.clip(1 - (yy + 0.5 - top) / drop, 0, 1) * 0.85 + np.clip((yy - 100) / 40.0, 0, 0.3)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def n_ball(cx, cy, rx, ry):
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.97, 0.97)
        v = np.clip((yy + 0.5 - cy) / ry, -0.97, 0.97)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def draw():
    c = Canvas(SIZE, MATERIALS, light=LIGHT)
    S = SIZE

    # ── 1 목도리 꼬리 (몸 뒤 · 왼쪽 매듭에서 왼쪽으로 두 가닥 — 설계도 x7~9 · y20~21)
    #   (2회차 판정: 가늘고 어두운 두 갈래 → 밝은 빨강 한 갈래 · 폭 3~4칸 · 왼쪽 실루엣 밖까지)
    tail = poly_mask(path((42, 79), [(38, 78.5), (34, 79.5), (29.5, 81.5)], (28.5, 84), (31.5, 84.5), [(35, 83.5), (38.5, 83), (42, 84)]), S)
    p_tail = c.part('tail', tail, 'scarf', n_plane(0.1, -0.5, 0.86), bias=-0.04)

    # ── 2 망토 안쪽 그늘 (겉옷 · 바지 — 망토가 벌어진 사이로만 보임, 설계도 x13~16 의 짙은 c · 다리 k · h)
    inner = poly_mask(path((55, 86), (70, 86), (75, 119), (46, 119)), S)
    p_inner = c.part('inner', inner, 'cloak', n_plane(0.2, -0.3, 0.9), bias=-0.38, line=False)

    # ── 3 장화 (설계도 y30~31 — 작고 어두움 · 발끝이 바깥으로)
    boot_l = poly_mask(path((48, 117), (57, 117), (57, 125), (44, 125), [(44, 123), (45, 121.5), (48, 121)]), S)
    boot_r = poly_mask(path((64, 117), (73, 117), [(73.5, 120), (75.5, 121), (77, 122)], (77, 125), (64, 125)), S)
    p_bootl = c.part('boot_l', boot_l, 'leather', n_plane(-0.1, -0.6, 0.8), bias=-0.38)
    p_bootr = c.part('boot_r', boot_r, 'leather', n_plane(0.3, -0.6, 0.75), bias=-0.32)

    # ── 4 망토 (어깨 ~ 발목 · 앞이 V 로 벌어짐 · 아랫단은 해진 천)
    cloak = poly_mask(path((39, 85), (86, 85), [(88, 90), (88.5, 101), (87.5, 113)], (85.5, 116.5), (82, 115.5), (79, 117.5),
                           (75.5, 116), (46, 116), (43, 117.5), (40, 116), [(37, 114), (36.5, 104), (36.5, 94)], (37.5, 88)), S)
    opening = poly_mask(path((60, 87), (65.5, 87), (73, 117), (49, 117)), S)
    p_cloak = c.part('cloak', cloak & ~opening, 'cloak', n_cloak(63, 27, 85, 8), bias=0.0)

    # ── 5 어깨끈 (왼 어깨 → 허리 가운데 · 설계도 (13,23)→(15,25))
    strap = poly_mask(path((49, 86), (53, 86), (62.5, 101), (58.5, 101)), S)
    p_strap = c.part('strap', strap, 'leather', n_plane(0.2, -0.5, 0.85), bias=-0.04)

    # ── 6 허리띠 (설계도 y26 — 망토 사이로 보이는 만큼)
    belt = poly_mask(path((51, 100), (71, 100), (72, 104), (50, 104)), S)
    p_belt = c.part('belt', belt, 'leather', n_plane(0.1, -0.4, 0.9), bias=-0.06)

    # ── 7 손 (설계도 y25 · 망토 옆 틈에서 나온 손 — 좌우 다르게)
    #   왼손: 망토 자락을 쥔 주먹 (손가락 마디 3) · 오른손: 단검을 거꾸로 쥠 (칼날이 아래로)
    p_handl = c.part('hand_l', poly_mask(path((40, 97), (45.5, 96.5), (47, 98.5), (46.5, 102), (41, 102), (39.5, 100)), S),
                     'skin', n_ball(44, 98, 5, 4.5), bias=-0.12)
    blade = poly_mask(path((79, 101), (81.5, 101), (81.5, 110), (80, 113), (79, 110)), S)
    p_blade = c.part('blade', blade, 'steel', n_plane(0.6, -0.2, 0.75), bias=0.0)
    hilt = poly_mask(path((78.5, 92), (82, 92), (82, 97), (78.5, 97)), S)
    p_hilt = c.part('hilt', hilt, 'leather', n_plane(0.4, -0.4, 0.8), bias=0.0)
    p_handr = c.part('hand_r', ellipse_mask(80.5, 98.5, 3.8, 3.0, S), 'skin', n_ball(82, 97, 5, 4.5), bias=0.02)

    # ── 8 두건 (설계도 y13~21 — 위가 좁고 납작, 옆자락이 어깨까지)
    #   (2회차 판정: 둥근 종 → 꼭대기 폭을 줄이고 꼭지를 왼쪽 위로 기울임 — 설계도 꼭대기 4칸)
    hood_pts = path((55, 49), (60, 49), [(65, 50), (71, 52.5), (76.5, 56.5)], [(81, 60.5), (85, 65.5), (87, 71)],
                    [(88, 75), (88, 80), (87, 84)], (84, 87), (80, 86.5), (46, 86.5), (41, 88),
                    [(37.5, 86), (36, 82), (36.5, 77)], [(38, 72), (41.5, 66), (44, 61)], [(46.5, 56), (50, 52), (53.5, 49.6)],
                    (55, 49))
    hood_n = n_dome(64, 70, 25, 22)
    p_hood = c.part('hood', poly_mask(hood_pts, S), 'hood', hood_n, bias=0.0)

    # ── 9 얼굴 그늘 (두건 안쪽 · 설계도 y19 — 눈 둘만 보임)
    face = poly_mask(path((45, 82), [(45.5, 77), (51, 71.5), (61, 71)], [(71, 70.5), (80, 72), (83, 77)], (83, 82)), S)
    p_face = c.part('face', face, 'void', n_plane(0.0, -1.0, 0.0), bias=0.0, line=False, smooth=0)

    # ── 10 목도리 (얇은 띠 — 얼굴 아래 반을 가림 · 설계도 y20~21 · 가운데 밝은 원통 띠 없음)
    scarf = poly_mask(path((44, 78.5), [(54, 77.5), (66, 77.5), (77, 78)], (84, 79), (84, 83.5),
                           [(76, 84.5), (62, 85), (50, 84.5)], (44, 84)), S)
    p_scarf = c.part('scarf', scarf, 'scarf', n_plane(0.15, -0.35, 0.92), bias=-0.06)
    # 매듭 (왼쪽 · 꼬리가 나오는 곳 — 두건 옆자락 위)
    p_knot = c.part('knot', ellipse_mask(42.5, 81.5, 3.4, 2.8, S), 'scarf', n_plane(0.1, -0.3, 0.9), bias=-0.04)

    # 두건 옆자락 (목도리 오른 끝을 덮음 — 두건과 같은 형태 모델 · 경계선 없음)
    flap_r = poly_mask(path((82, 74), (88, 74), [(88.5, 80), (88, 84), (85, 87.5)], (81.5, 86.5), [(81, 82), (81.5, 78), (82, 74)]), S)
    p_flapr = c.part('flap_r', flap_r, 'hood', hood_n, bias=0.0)
    hoods = (p_hood, p_flapr)

    c.tidy(4)
    c.contact_lines(skip={('hood', 'face'), ('hood', 'flap_r'), ('face', 'scarf'), ('inner', 'cloak'), ('inner', 'belt'),
                          ('inner', 'strap'), ('cloak', 'strap'), ('blade', 'hand_r'), ('hilt', 'hand_r')})

    # ───────────── 덧그림 (큰 형태 위주 — 잔점 · 같은 간격 줄 없음)
    from scipy import ndimage
    deep = ndimage.distance_transform_cdt(np.pad(c.mat >= 0, 1), metric='taxicab')[1:-1, 1:-1] >= 4

    def fold(pts, parts, side=-1, dark=-1, ridge=1):
        """긴 천 주름: 골 2칸(빛 반대쪽) + 등성이 1칸(빛 쪽) — 실루엣 3칸 안은 골을 안 파서 외곽이 두꺼워 보이지 않게"""
        cells = [(x, y) for x, y in line_cells(path(*pts)) if 0 <= y < S and 0 <= x < S and deep[y, x]]
        c.nudge(cells + [(x + side, y) for x, y in cells], dark, parts)
        if ridge:
            c.nudge([(x - side, y) for x, y in cells], ridge, parts)

    # 두건: 얼굴 둘레 접힌 테 (밝은 2칸 띠 + 위 골) · 끝에서 흘러내리는 큰 주름 둘
    rim_face = line_cells(path([(45, 77), (51, 71), (61, 70)], [(71, 69.5), (80, 71), (83, 76)]))
    c.nudge([(x, y - 1) for x, y in rim_face], 1, hoods)
    c.nudge([(x, y - 2) for x, y in rim_face], 1, hoods)
    c.nudge([(x, y - 3) for x, y in rim_face], -1, hoods)
    fold([[(58, 51), (54, 55), (49, 62), (45, 72)]], hoods)
    fold([[(63, 51), (70, 55), (77, 62), (81, 70)]], hoods, ridge=0)
    fold([[(39, 74), (39, 79), (40, 85)]], hoods, ridge=0)                    # 왼 옆자락이 어깨로 접힌 골
    fold([[(85, 75), (85.5, 80), (85, 85)]], hoods)
    # 얼굴 구멍 양옆으로 모인 짧은 천 주름 (테에서 바깥 · 2칸 폭)
    fold([[(48, 72), (45, 67)]], hoods, ridge=0)
    fold([[(79, 70), (82, 66)]], hoods, ridge=0)
    fold([[(55, 63), (60, 65.5), (67, 65.5), (72, 63)]], hoods, side=0, ridge=0)   # 이마 위로 처진 천 (얼굴 테 위 골)
    c.nudge([(x, 64) for x in range(57, 71)] + [(x, 63) for x in range(59, 69)], 1, hoods)   # 그 위 등성이
    c.nudge(line_cells([(31, 83), (40, 83)]), -1, p_tail)                    # 꼬리 아랫면 그늘
    # 얼굴 그늘: 목도리 바로 위 붉은 반사 1줄
    refl = line_cells([(47, 77), (56, 76), (72, 76), (81, 77)])
    c.set_lvl([(x, y) for x, y in refl if c.pid[y, x] == p_face], 0, mat='scarf')
    # 목도리: 매듭으로 당겨진 주름 하나 · 아랫단 그늘 · 매듭 가운데 골
    c.nudge(line_cells(path([(82, 80.5), (70, 80.5), (58, 81), (47, 82)])), -1, p_scarf)
    c.nudge(line_cells([(83, 79), (75, 79)]), 1, p_scarf)                    # 오른 끝 윗단 빛 (짧게)
    c.nudge(line_cells([(66, 83), (74, 82)]), -1, p_scarf)                   # 오른쪽 처진 겹
    c.nudge(line_cells([(46, 84), (60, 84.5), (72, 84.5), (83, 83)]), -1, p_scarf)
    c.nudge(line_cells([(40, 83), (44, 80)]), -1, p_knot)                   # 묶인 골 (사선)
    # 망토: 큰 세로 주름 셋 (아랫단까지) · 앞섶 가장자리 (왼 자락 = 빛 받는 테, 오른 자락 = 그늘 테)
    fold([[(45, 90), (44, 98), (43, 106), (42, 115)]], p_cloak)
    fold([[(54, 96), (53, 104), (51, 115)]], p_cloak)
    fold([[(77, 102), (78, 108), (79, 115)]], p_cloak)
    fold([[(70, 98), (72, 106), (73, 112)]], p_cloak, side=1, ridge=0)       # 오른 앞자락 안쪽 주름
    fold([[(48, 92), (47, 100), (46.5, 108)]], p_cloak, ridge=0)
    fold([[(83, 90), (84, 98), (84, 106)]], p_cloak)                           # 단검 뒤 오른 옆구리
    c.nudge(line_cells([(75, 90), (75, 97)]) + line_cells([(76, 104), (76, 111)]), 1, p_cloak)   # 오른 자락 빛 받는 등성이 (끊긴 두 마디)
    fold([[(41, 88), (46, 91), (52, 92)]], p_cloak, side=0, dark=-1, ridge=0)  # 왼 어깨에서 처지는 주름
    c.nudge(line_cells([(59, 88), (48, 116)]), 1, p_cloak)
    # 아랫단 그늘 (주름 사이마다 · 해진 끝)
    for x0, x1 in ((39, 43), (46, 50), (55, 58), (75, 78), (81, 85)):
        c.nudge([(x, 113) for x in range(x0, x1 + 1)] + [(x, 112) for x in range(x0 + 1, x1)], -1, p_cloak)
    # 목 여밈 쇠 (목도리 바로 아래 · 망토 양 자락을 잇는 고리)
    c.stamp(['.sTs.', 'sT.Ts', '.sss.'], 60, 86, {'s': ('steel', 0), 'T': ('steel', 1)})
    # 허리 주머니 (허리띠 왼쪽 · 덮개 빛)
    c.stamp(['LLLLL', 'lLLLl', 'lllll', 'mmmmm', '.mmm.'], 52, 101, {'L': ('leather', 3), 'l': ('leather', 2), 'm': ('leather', 1)})
    c.nudge(line_cells([(33, 80), (39, 79)]), 1, p_tail)
    # 망토 왼 아래 덧댄 천 (낡은 도적 망토 · 한 단계 밝은 천 + 어두운 테)
    patch = poly_mask(path((40.5, 104), (46.5, 103.5), (47, 109.5), (41, 110)), S)
    pc = [(int(x), int(y)) for y, x in zip(*np.nonzero(patch))]
    c.nudge(pc, 1, p_cloak)
    ring = [(x, y) for x, y in pc if not all(patch[y + dy, x + dx] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))]
    c.nudge(ring, -2, p_cloak)
    # 겉옷 안쪽 주름 둘 (허리띠 위)
    c.nudge(line_cells([(60, 90), (59, 99)]), 1, p_inner)
    c.nudge(line_cells([(64, 92), (65, 99)]), 1, p_inner)
    # 장화 목 쇠 장식 · 칼 날밑
    c.stamp(['ss'], 50, 119, {'s': ('steel', 0)})
    c.stamp(['ss'], 67, 119, {'s': ('steel', 1)})
    c.stamp(['TTTT'], 78, 101, {'T': ('steel', 1)})
    c.nudge(line_cells([(67, 88), (74, 116)]), -1, p_cloak)
    # 안쪽 그늘: 겉옷 아래 두 다리 사이 골 · 오른 다리 무릎 빛 (설계도 C)
    c.nudge(line_cells([(61, 105), (61, 118)]), -1, p_inner)
    c.nudge(line_cells([(65, 106), (66, 112)]) + line_cells([(66, 106), (67, 112)]), 1, p_inner)
    # 허리띠: 작은 쇠 버클 (테두리만) · 띠 아래 그늘
    c.stamp(['TTT', 'T.T', 'TTT'], 61, 101, {'T': ('steel', 1)})
    c.nudge(line_cells([(51, 103), (71, 103)]), -1, p_belt)
    # 어깨끈: 가장자리 빛 1줄 (끊지 않음)
    c.nudge(line_cells([(52, 87), (61, 100)]), 1, p_strap)
    # 왼손: 망토 자락을 쥔 주먹 (손 도트 지도 — 피부 2톤 b 바탕 · c 빛 + 손가락 경계선 a 1줄만 · 2회차 판정: 얼룩 걷기)
    c.stamp(['..cccc.',
             '.bbbccc',
             'bbbbbbc',
             '.aaaaaa',
             'bbbbbbb',
             '.bbbbb.'], 40, 97, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 2)})
    # 오른손: 칼자루를 감싼 손가락 (골 둘) · 단검 날 등 빛 · 칼자루 끝 쇠
    c.nudge([(79, 99), (80, 99), (81, 100), (82, 100)], -1, p_handr)
    c.set_lvl(line_cells([(81, 102), (81, 109)]), 2, p_blade)               # 칼날 등 밝은 1줄
    c.stamp(['TTT'], 79, 91, {'T': ('steel', 1)})
    c.stamp(['sss'], 79, 92, {'s': ('steel', 0)})
    # 장화: 접힌 목 (위 1줄 밝게) · 오른 발끝 쇠 빛 (설계도 q)
    for pb in (p_bootl, p_bootr):
        c.nudge([(x, 118) for x in range(40, 82)], 1, pb)
    c.stamp(['TT'], 76, 122, {'T': ('steel', 0)})

    # 림 라이트: 오른쪽 위 실루엣 안쪽 — 두건 1칸 · 망토 2칸 · 목도리 · 장화 1칸 (몸 전체는 그대로 어둡게)
    c.rim(hoods, RIM_DIR, width=2, level=-1, thresh=0.3)
    # 두건의 1~2칸 조각(4-연결)을 이웃 단계로 흡수 — 림 · 주름 끝이 하나로 이어진 띠가 되게 (2회차 판정: 오른 위 테두리 52조각)
    c.absorb(np.isin(c.pid, hoods), min_size=3)
    c.rim((p_cloak,), RIM_DIR, width=2, level=-1, thresh=0.25)
    c.rim((p_scarf, p_knot, p_tail, p_bootr), RIM_DIR, width=1, by=1, thresh=0.5)

    # 눈 (빛나는 노란 눈 · 설계도 x14 · x17 → 가운데 58 · 70)
    eye = ['.aab', 'aaaa', '.aa.']
    for ex in (55, 67):
        c.stamp(eye, ex, 72, {'a': EYE, 'b': EYE_HOT})
    dots = [(58, 72), (70, 72)]
    c.outline(warm_parts=(p_handl, p_handr, p_bootl, p_bootr))
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
