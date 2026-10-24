# 시계공 메뉴 초상 128칸 (그래픽 가독성 개편 2단계 묶음 3 — 도적 규칙 README.md 그대로)
#   설계도 = Resources/Characters/clockmaker.png 첫 그림 (32칸 · 몸 13×19) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 13~17 위가 평평한 금발 바가지 머리 (옆머리가 얼굴보다 1칸씩 넓음) · 17~21 얼굴 — 왼눈(x14) · 오른눈 자리에 놋쇠 테 외알 확대경(x17~19)
#     22~25 갈색 조끼 + 소매 · 가운데 크림 셔츠 V (y24 금빛 회중시계) · 25 양손 · 오른손 바깥에 쇠 공구(x19 y23~24 회색) · 26 허리띠 + 놋쇠 버클
#     27~29 어두운 바지 (오른 다리 바깥 빛) · 30~31 어두운 장화
#   명도 계층: 눈(어두운 눈동자 + 반짝) · 외알 렌즈 > 얼굴 > 셔츠 > 머리(바탕을 낮추고 위 · 오른쪽 림만 밝게) > 조끼 > 바지
#   광원 = 설계도를 이어받아 위 · 살짝 오른쪽 (설계도 머리 오른쪽 e Y196 > 왼쪽 d Y187)
import numpy as np
from scipy import ndimage

from .portrait import Canvas, Map, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, n_cyl_v, despeckle

SIZE = 128
K = 4
M = Map(K, 0, -3)
DESIGN = 'Assets/Resources/Characters/clockmaker.png'     # 시트 첫 칸 (32×32) 이 설계도

# 재질: 어두움 → 밝음 · 재질당 ≤ 5단 · 전체 ≤ 32색 (놋쇠는 머리 단계 색을 같이 씀 — 색 수 절약)
MATERIALS = {
    # 금발: 바탕 #C8984C (Y 약 158) · 윗면 #E0B864 (Y 187) · 림 #EDC874 (Y 201) — 가장 밝은 단계도 얼굴 빛(Y 218)보다 어둡게 (판정 1회차: 머리 Y214 > 얼굴)
    'hair':  dict(ramp=[hx('6E4426'), hx('A8763A'), hx('C8984C'), hx('E0B864'), hx('EDC874')], cuts=[0.40, 0.66, 1.00, 9.0]),
    # 놋쇠 = 머리 단계 색을 같이 씀 (색 수 절약 · 가장 밝은 단계 Y 187)
    'brass': dict(ramp=[hx('6E4426'), hx('A8763A'), hx('E0B864')], cuts=[0.55, 0.95], warm=True),
    # 살: 얼굴 빛 받는 면만 4단째 #FAD2B0 (Y 218 — 그림 전체에서 눈 반짝 다음으로 밝음 · 흑백 계층 1위). 손은 3단까지
    'skin':  dict(ramp=[hx('8A4E52'), hx('BE8478'), hx('F2BC96'), hx('FAD2B0')], cuts=[0.62, 0.95, 1.22], warm=True),
    # 조끼 · 소매 가죽: 바탕 #7C4E2C (설계도 p) · 빛 #A17848 (설계도 o) · 그늘 #572714 (설계도 r)
    'vest':  dict(ramp=[hx('3C1D1F'), hx('572714'), hx('7C4E2C'), hx('A17848'), hx('C89C66')], cuts=[0.34, 0.58, 0.92, 9.0], warm=True),
    # 크림 셔츠: 가장 밝은 단계 Y 약 207 (설계도 q #FFFAE9 · 판정 1회차 가슴 Y233 이 그림 최고 → 얼굴 빛 아래로)
    'shirt': dict(ramp=[hx('A8977A'), hx('C9BA98'), hx('DDD0B2')], cuts=[0.55, 1.08]),
    # 쇠 · 유리 (공구 · 렌즈 · 버클 고리): 차가운 회청 — 가장 밝은 단계는 공구 날 1줄 · 렌즈 바탕
    'metal': dict(ramp=[hx('4A5262'), hx('7E8C9C'), hx('B8CCD8')], cuts=[0.55, 0.95]),
    # 바지: 설계도 y #484264 · A #302C4A · B #5A728F 그대로 (허리띠를 뺀 만큼 몸 평균 Y 를 설계도 쪽으로 — lint 13)
    'pants': dict(ramp=[hx('24203C'), hx('302C4A'), hx('484264'), hx('5A728F')], cuts=[0.36, 0.62, 0.98]),
    'boots': dict(ramp=[hx('24162A'), hx('3A2230'), hx('563432'), hx('7A5040')], cuts=[0.40, 0.70, 1.0], warm=True),
}
OUT = hx('1A112C')          # 눈동자 = 바깥 외곽 색 (색 수 절약 · 설계도 h #120B20 과 거의 같음)
GLINT = hx('E2F6FF')        # 렌즈 · 눈 반짝 (설계도 i)
LIGHT = (0.2, -0.8, 0.56)
RIM_DIR = (0.4, -0.92)


def n_dome(cx, cy, rx, ry):
    """머리: 위가 평평한 바가지 — 꼭대기는 위를 보고 옆은 옆을 봄"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.98, 0.98)
        v = np.clip((yy + 0.5 - cy) / ry, -0.98, 0.1)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.04, 1))
    return f


def n_ball(cx, cy, rx, ry):
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.97, 0.97)
        v = np.clip((yy + 0.5 - cy) / ry, -0.97, 0.97)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def n_torso(cx, r, top, drop):
    """몸통: 세로 원기둥 + 어깨에서 위를 보는 둥근 어깨"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        v = -np.clip(1 - (yy + 0.5 - top) / drop, 0, 1) * 0.8
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def draw():
    c = Canvas(SIZE, MATERIALS, light=LIGHT)
    S = SIZE

    # ── 1 바지 (설계도 y27~29 · x12~17 — 두 다리 · 안쪽 그늘)
    p_legl = c.part('leg_l', poly_mask(path((47, 102), (60, 102), (60, 119), (48, 119)), S), 'pants', n_cyl_v(54, 8), bias=0.08)
    p_legr = c.part('leg_r', poly_mask(path((60, 102), (73, 102), (72, 119), (61, 119)), S), 'pants', n_cyl_v(66, 8), bias=0.12)

    # ── 2 장화 (설계도 y30~31 — 발끝이 바깥으로)
    boot_l = poly_mask(path((48, 117), (59, 117), (59, 125), (43, 125), [(43, 122.5), (45, 121), (48, 120.5)]), S)
    boot_r = poly_mask(path((61, 117), (72, 117), [(72.5, 120), (75, 120.5), (77, 122)], (77, 125), (61, 125)), S)
    p_bootl = c.part('boot_l', boot_l, 'boots', n_plane(-0.1, -0.6, 0.8), bias=-0.12)
    p_bootr = c.part('boot_r', boot_r, 'boots', n_plane(0.3, -0.6, 0.75), bias=-0.09)

    # ── 3 몸통 조끼 (설계도 y21~25 — 왼 어깨가 한 줄 높음 · 높은 깃)
    torso = poly_mask(path((41, 84), [(44, 81.5), (49, 81), (54, 82)], (74, 84), [(79, 84), (82, 86), (83, 89)],
                           (83, 102), (68, 103), [(65, 105), (62.5, 107.5), (61, 108.5)], [(59.5, 107.5), (57, 105), (54, 103)], (40, 102), (38, 99),
                           [(37.5, 94), (38, 88), (41, 84)]), S)
    p_torso = c.part('torso', torso, 'vest', n_torso(61, 24, 82, 7), bias=0.13)

    # ── 4 셔츠 V (설계도 y22~24 x13~16 — 위가 넓고 아래로 좁아짐) + 깃 두 장
    shirt = poly_mask(path((51, 84), (69, 84), [(67, 89), (64.5, 94), (62.5, 98)], (57.5, 98), [(55.5, 94), (53, 89), (51, 84)]), S)
    p_shirt = c.part('shirt', shirt, 'shirt', n_plane(0.1, -0.5, 0.85), bias=-0.12)
    p_coll_l = c.part('collar_l', poly_mask(path((50, 82), (58, 84), (55, 89), (50, 86)), S), 'shirt', n_plane(-0.3, -0.7, 0.6), bias=0.06)
    p_coll_r = c.part('collar_r', poly_mask(path((62, 84), (71, 82), (71, 86), (65, 89)), S), 'shirt', n_plane(0.4, -0.7, 0.6), bias=0.1)

    # ── 5 소매 (설계도 x10~11 · x18~19 — 조끼와 같은 가죽 · 바깥 테 위주)
    arm_l = poly_mask(path((41, 84), [(38.5, 86.5), (36.6, 91), (37.6, 97.5)], (40, 99), (47, 99), (48, 92), [(47.5, 88), (45, 85), (41, 84)]), S)
    arm_r = poly_mask(path((77, 84), [(81.5, 85.5), (84.2, 90), (83.3, 97.5)], (80, 99), (73, 99), (72.5, 92), [(73, 88), (74.5, 85.5), (77, 84)]), S)
    p_arml = c.part('arm_l', arm_l, 'vest', n_cyl_v(42, 7, 0.2), bias=-0.06)
    p_armr = c.part('arm_r', arm_r, 'vest', n_cyl_v(78, 7, 0.2), bias=0.0)

    # ── 6 허리띠 없음 (판정 1회차: 검사 · 지도 제작자 · 거너와 같은 「넓은 금 띠 + 가운데 버클」이 묶음 반복 무늬)
    #   → 시계공은 조끼 아랫단이 앞에서 두 갈래로 뾰족한 V (조끼 단 = 3번 몸통 모양) + 회중시계 줄로 허리를 나눔

    # ── 7 회중시계 (V 아래 · 설계도 y24 x14~15 금빛) + 줄은 덧그림
    p_watch = c.part('watch', ellipse_mask(60, 94.5, 4.6, 4.2, S), 'brass', n_ball(61.5, 93, 5, 4.5), bias=0.0)

    # ── 8 손 (설계도 y25 — 왼손 늘어뜨린 손 · 오른손 공구 쥔 주먹) + 오른손 공구 (설계도 x19 y23~24 회색 → 위로 선 드라이버)
    p_handl = c.part('hand_l', poly_mask(path((38.5, 97), (46.5, 97), (47, 101), [(46, 103.5), (42, 104), (39, 102)]), S),
                     'skin', n_ball(44, 99, 5, 4.5), bias=-0.05)
    p_tool = c.part('tool', poly_mask(path((83.5, 84.5), (86.5, 85.5), (82, 98), (79, 97)), S), 'metal', n_plane(0.6, -0.3, 0.75), bias=-0.05)
    p_handr = c.part('hand_r', poly_mask(path((73.5, 96), (82, 96), (82.5, 100), [(81.5, 102.5), (77, 103), (74, 101)]), S),
                     'skin', n_ball(79, 98, 5, 4.5), bias=0.04)

    # ── 9 얼굴 (설계도 y17~21 · 턱은 왼쪽이 그늘)
    face = poly_mask(path((47, 60), (81, 60), (81.5, 77), [(80.5, 81), (78, 84), (74, 85)], (57, 85),
                          [(52, 84), (48.5, 81), (47.5, 77)]), S)
    p_face = c.part('face', face, 'skin', n_ball(68, 60, 40, 60), bias=0.62, smooth=1.0)   # 판정 1회차: 왼쪽 반 넓은 그늘 → 빛 중심을 가운데로 (그늘 폭 반)

    # ── 10 머리 (설계도 y13~17 — 위가 평평한 사각 · 폭 = 설계도 x11~20 → 44~84 (판정 1회차: 40~88 로 부풀어 마스코트 같음)
    #   옆머리는 얼굴보다 3칸씩 넓게 귀를 덮고, 앞머리 아랫선은 거의 곧은 가로 (오른쪽이 1칸 짧게 들림)
    #   옆머리 끝은 안쪽으로 1~2칸 둥글게 말림 (판정: 사각 상자 · 투구처럼 보이지 않게)
    #   판정 2회차: 옆면 세로 직선(x44 · x84) → 위에서 아래로 살짝 부풀었다가 아래 끝이 1~2칸 안으로 말림 · 앞머리 끝 계단 3개 (간격 다르게)
    hair_pts = path((50, 49), (78, 49), [(81.5, 49.3), (83.8, 51), (84.3, 54)], [(84.8, 58), (84.6, 63), (83.4, 67)],
                    [(82.8, 69), (81.8, 70), (80.8, 69.6)], [(80, 67.5), (79, 66.4), (77, 66.2)], (74.5, 65), [(71, 64.4), (68, 64.6), (66, 65.2)],
                    (64.2, 67.2), (62.4, 65.6), [(58, 65.2), (54, 65.4), (52, 65.8)], (50.6, 67.8), (48.6, 66.6),
                    [(47.6, 68), (47, 69.6), (45.8, 69.4)], [(44.6, 68.4), (43.4, 65), (43.4, 61)], [(43.3, 57), (43.6, 54), (44.3, 51)],
                    [(45.5, 49.8), (47.5, 49.1), (50, 49)])
    hair_n = n_dome(64, 62, 22, 16)
    p_hair = c.part('hair', poly_mask(hair_pts, S), 'hair', hair_n, bias=0.08)

    # ── 11 외알 확대경 (설계도 오른눈 자리 · 테 가운데 (74.5, 75.5)) — 놋쇠 테 2칸 + 유리
    ring = ellipse_mask(74.5, 75.5, 6.2, 6.2, S)
    lens = ellipse_mask(74.5, 75.5, 4.1, 4.1, S)
    p_ring = c.part('ring', ring & ~lens, 'brass', n_ball(76, 73.5, 7, 7), bias=0.25)
    p_lens = c.part('lens', lens, 'metal', n_plane(0.2, -0.5, 0.85), bias=0.4, line=False)

    c.tidy(4)
    c.contact_lines(skip={('face', 'hair'), ('torso', 'shirt'), ('shirt', 'collar_l'), ('shirt', 'collar_r'), ('torso', 'collar_l'),
                          ('torso', 'collar_r'), ('belt', 'buckle'), ('ring', 'lens'), ('leg_l', 'boot_l'), ('leg_r', 'boot_r'),
                          ('tool', 'hand_r'), ('shirt', 'watch'), ('torso', 'watch')})

    # ───────────── 덧그림 (큰 형태 위주)
    deep = ndimage.distance_transform_cdt(np.pad(c.mat >= 0, 1), metric='taxicab')[1:-1, 1:-1] >= 4

    def fold(pts, parts, side=-1, dark=-1, ridge=1):
        """긴 주름 · 머리 결: 골 2칸(빛 반대쪽) + 등성이 1칸 — 실루엣 3칸 안은 안 팜"""
        cells = [(x, y) for x, y in line_cells(path(*pts)) if 0 <= y < S and 0 <= x < S and deep[y, x]]
        c.nudge(cells + [(x + side, y) for x, y in cells], dark, parts)
        if ridge:
            c.nudge([(x - side, y) for x, y in cells], ridge, parts)

    # 머리: 앞머리를 긴 뭉치 셋으로 나누는 결 둘 — 간격 · 기울기를 다르게 (판정 1회차: 같은 간격 세로 줄 넷 → 둘)
    fold([[(63, 51), (61.5, 56), (60, 61), (59.5, 64)]], p_hair)
    fold([[(76, 54), (77.5, 59), (78, 64)]], p_hair, side=1, ridge=0)
    # 판정 2회차: 왼 옆머리 = 빛 반대쪽 긴 그늘 덩어리 (위가 좁고 아래로 넓어지는 낫 모양) — 금발이 한 색 판자로 보이지 않게
    c.shift(poly_mask(path((47, 55), [(49.5, 58), (51.5, 62), (52, 66)], (47.5, 69), [(45.5, 66), (45.5, 60), (47, 55)]), S), -1, p_hair)
    # 3회차 대비: 머리 바탕 한 색 233칸 → 앞머리 오른쪽 위에 빛 받는 긴 덩어리 (아래 경계가 휘어 내려가는 곡선 · 결 줄과 간격 다름)
    lobe = poly_mask(path((65, 51.5), (80, 51.5), [(82.5, 55), (82.5, 59), (80.5, 62.5)], [(76, 61.5), (71.5, 58.5), (68, 55.5)]), S)
    c.shift(lobe & (c.lvl == 2), 1, p_hair)
    # 앞머리 아랫단 그늘 (머리 끝 2칸 한 단계 어둡게) · 이마에 드리운 머리 그림자 2칸
    hm = c.pid == p_hair
    below = np.zeros_like(hm)
    below[1:] = hm[:-1]
    shadow_face = below & (c.pid == p_face)
    sh2 = np.zeros_like(hm)
    sh2[1:] = shadow_face[:-1]
    c.shift(shadow_face | (sh2 & (c.pid == p_face)), -1, p_face)
    above = np.zeros_like(hm)
    above[:-1] = (c.pid == p_face)[1:]
    a2 = np.zeros_like(hm)
    a2[:-1] = above[1:]
    c.shift((above | a2) & hm & deep, -1, p_hair)
    # 얼굴: 왼 볼 · 턱 그늘 (빛 반대쪽 큰 덩어리) · 눈썹 · 입
    # 판정 2회차 (불합격 ④): 얼굴 살색 한 색 345칸 → 면 셋을 손으로 정함 — 한 색 덩어리 200칸 이하
    #   빛 (4단째): 이마 오른쪽 위 띠 + 코등을 따라 눈 · 확대경 사이로 내려오는 쐐기 (빛 = 위 · 오른쪽)
    #   그늘 (2단째): 왼뺨 바깥 곡선 띠 → 턱 아래로 이어짐 (폭 3~4칸 · 턱은 2칸) — 예전 턱 왼쪽 갈색 얼룩 대신
    fm = c.pid == p_face
    lit = poly_mask(path((56, 65), (81, 65), (81, 68.5), [(74, 69), (70, 69.5), (68.5, 72)], [(67.5, 75), (67, 78), (66.5, 80)],
                         (62.5, 80), [(61.5, 76.5), (60.5, 73), (58.5, 70.5)], (55.5, 69)), S)
    shade = poly_mask(path((46, 67), (48.6, 67), [(48.8, 72), (49.8, 77), (53.5, 81.2)], [(58, 83), (66, 83.4), (72, 83)], (76, 82.5),
                           (79, 82.4), (79, 86), (46, 86)), S)
    c.set_lvl([(int(x), int(y)) for y, x in zip(*np.nonzero(fm & lit))], 3, p_face, fix=False)
    c.set_lvl([(int(x), int(y)) for y, x in zip(*np.nonzero(fm & shade))], 1, p_face, fix=False)
    c.stamp(['bbbbb', '.bbbb'], 55, 69, {'b': ('hair', 0)}, part=None)       # 왼 눈썹 (앞머리 그림자 아래)
    c.stamp(['bbbbb'], 61, 81, {'b': ('skin', 0)})                           # 입 (작게 · 가장 어두운 살색 — 둘레가 그늘 단계라 한 단계로는 안 보였음)
    # 조끼: 앞섶 가장자리 (V 테 = 왼 자락 그늘 · 오른 자락 빛) · 주머니 · 조끼 단추 아래 큰 주름 둘
    fold([[(50, 86), (54, 92), (57, 99)]], p_torso, side=-1, ridge=0)
    c.nudge(line_cells([(68, 87), (65, 96)]) + line_cells([(69, 87), (66, 96)]), 1, p_torso)
    c.nudge(line_cells([(48, 92), (54, 92)]) + line_cells([(48, 93), (54, 93)]), -1, p_torso)   # 왼 주머니 입구 (2칸 띠)
    fold([[(50, 95), (52.5, 99), (54.5, 102)]], p_torso, side=-1, ridge=1)    # 판정 2회차: 조끼 바탕 한 색 323칸 → 주머니 아래에서 V 끝으로 흐르는 긴 주름 (골 + 빛 등성이)
    fold([[(69, 97), (67.5, 101), (66, 103)]], p_torso, side=1, ridge=1)
    c.nudge(line_cells([(49, 91), (53, 91)]), 1, p_torso)
    # 회중시계 줄 (시계 왼쪽 → 왼 주머니 · 2칸 굵기 놋쇠)
    chain = line_cells(path([(56, 94), (53, 95.5), (50, 94)]))
    c.set_lvl(chain + [(x, y - 1) for x, y in chain], 1, p_torso, mat='brass')
    # 시계 판: 가운데 크림 판 + 바늘 한 줄
    #   (판정 1회차: 4칸 흰 판 Y233 이 그림 최고 → 한 단계 낮춘 판 + 바늘 둘(ㄱ자)로 시계로 읽히게)
    c.stamp(['.ccc.', 'ccdcc', 'ccddc', '.ccc.'], 58, 92, {'c': ('shirt', 1), 'd': ('pants', 1)})
    # 셔츠 V: 가운데 그늘 · 깃 아래 그늘
    c.nudge(line_cells([(54, 88), (60, 88), (66, 88)]), -1, p_shirt)
    # 소매: 팔꿈치 주름 하나씩 · 손목 소맷부리
    c.nudge(line_cells([(39, 96), (47, 96)]), 1, p_arml)
    c.nudge(line_cells([(73, 96), (82, 96)]), 1, p_armr)
    # 조끼 아랫단: 뾰족한 두 끝 위 그늘 1줄 (단이 앞으로 접힘)
    c.nudge(line_cells(path([(55, 102), (58, 104.5), (60.5, 106.5)])), -1, p_torso)      # 판정 2회차: 톱니 두 끝 → 매끈한 V 하나 (왼 자락 단 그늘 1줄만)
    # 바지: 오른 다리 바깥 빛 (설계도 B) · 무릎 아래 주름
    c.nudge(line_cells([(70, 106), (70, 116)]) + line_cells([(69, 106), (69, 116)]), 1, p_legr)
    # 장화: 접힌 목 (위 1줄 밝게) · 오른 발끝 빛
    for pb in (p_bootl, p_bootr):
        c.nudge([(x, 118) for x in range(40, 82)], 1, pb)
    # 왼손: 늘어뜨린 손 (피부 2톤 + 손가락 경계선 1줄)
    #   판정 2회차 묶음 공통: 양손이 같은 틀(가로 경계 주먹) → 왼손은 편 손가락이 아래로 (검지 · 중지 사이 세로 경계 1줄)
    c.stamp(['..cccc.',
             '.bcccc.',
             'bbbbbcc',
             'bbbabbb',
             'bbbabb.',
             '.bbab..'], 39, 97, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 2)})
    # 오른손: 공구를 쥔 주먹 (손가락 골 둘 · 엄지 빛)
    c.stamp(['.ccccc.',
             'bbbcccc',
             'aaaaaaa',
             'bbbbbbb',
             '.bbbbb.'], 74, 97, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 2)})
    # 공구 날 밝은 1줄 (오른쪽 · 위 끝까지) · 위 끝 납작한 날 (넓게)
    c.set_lvl(line_cells([(85, 86), (81, 97)]), 2, p_tool)
    # 확대경 테: 오른 아래 그늘 반 · 렌즈 속 커진 눈 · 렌즈 아래 반 한 단계 어둡게
    c.shift(ellipse_mask(72.5, 77.5, 5, 5, S) & ~ellipse_mask(76, 74, 4, 4, S), -1, p_ring)
    c.shift(ellipse_mask(73, 78, 3.6, 3.0, S), -1, p_lens)

    # 림 라이트: 오른쪽 위 실루엣 안쪽 — 머리 2칸 띠 · 소매 · 조끼 어깨 1~2칸
    c.rim((p_hair,), (0.75, -0.66), width=2, level=-1, thresh=0.5)        # 평평한 윗면 전체가 아니라 오른 위 모서리 · 오른 옆머리만 (모자 챙처럼 보이지 않게)
    c.absorb(c.pid == p_hair, min_size=3)
    c.rim((p_armr, p_torso), RIM_DIR, width=2, level=-1, thresh=0.3)
    c.rim((p_bootr, p_legr), RIM_DIR, width=1, by=1, thresh=0.5)
    for p in (p_torso, p_arml, p_armr, p_legl, p_legr, p_face, p_shirt):
        c.absorb(c.pid == p, min_size=3)

    # 눈: 왼눈 (어두운 눈동자 + 반짝) · 렌즈 속 커진 오른눈 (+ 렌즈 반짝 = 시선점)
    c.stamp(['.aa.', 'aaab', 'aaaa', '.aa.'], 56, 73, {'a': OUT, 'b': GLINT})
    c.stamp(['.aa.', 'aaaa', 'aaaa', '.aa.'], 73, 74, {'a': OUT})
    c.stamp(['gg', 'g.'], 76, 72, {'g': GLINT})
    dots = [(59, 74), (76, 72)]
    c.outline(warm_parts=(p_handl, p_handr, p_bootl, p_bootr, p_face, p_hair, p_torso, p_arml, p_armr))
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
