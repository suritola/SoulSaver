# 궁수 메뉴 초상 128칸 (그래픽 가독성 개편 2단계 묶음 3 — 도적 규칙(README.md) 그대로)
#   설계도 = Resources/Characters/archer.png 첫 그림 (32칸 · 몸 15×20) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 12~14 꼭지 왼쪽 위로 뻗은 흰 깃털 · 13~18 꼭지가 뾰족한 초록 두건(오른쪽이 빛)
#     19~21 얼굴 (눈 x14 · x17) + 두건 옆자락 · 목 둘레 깃 · 18~23 오른 어깨 뒤 회색 화살통 (위로 흰 깃 · 화살 끝)
#     22~25 갈색 가죽 조끼 (가운데 밝은 앞섶) · 양 소매 · 손 · 26 허리띠 + 놋쇠 버클 · 27~29 어두운 갈색 바지 · 30~31 장화
#   광원 = 설계도를 이어받아 위 · 오른쪽 (두건 오른 테 · 오른 다리 · 장화 오른쪽이 밝음)
#   도적 판정 규칙: 정체성 실루엣(뾰족한 두건 · 화살통 · 조끼) 유지 · 둥근 종 금지 · 1~2칸 조각 흡수 · 림 라이트로만 분리 · 손 2톤
#   영웅 8명 판정 1회차 → 화살통 = 원통 + 위 · 아래 테 + 흰 깃 끝 셋 (회색 바위 덩어리 걷기) · 오른 허리 화살(정체불명 촉) 지움
#     하체 명도 분리 (조끼 아랫단 밑 그늘 · 장화 한 단계 어둡게) · 허리띠 = 가는 가죽 끈 + 매듭 (금 버클 없음 — 다른 영웅과 다르게)
#     흑백 계층: 깃털 · 화살 깃 가장 밝은 단계 < 얼굴 빛 (눈 반짝만 더 밝음)
#   판정 2회차 → 화살통 몸통 = 갈색 가죽 원통 (오른쪽 림 · 왼쪽 그늘 — 회색 돌덩이 걷기) · 허리 매듭 = 고리 둘 + 늘어진 끈 한 가닥
#     얼굴 밝은 면 한 색 217칸 → 오른뺨 · 턱 아래 그늘 1단 (한 색 덩어리 200칸 이하)
import numpy as np
from scipy import ndimage

from .portrait import Canvas, Map, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, n_cyl_v, despeckle

SIZE = 128
K = 4
M = Map(K, 0, -3)
DESIGN = 'Assets/Resources/Characters/archer.png'     # 시트 첫 칸 (32×32) 이 설계도

# 재질: 어두움 → 밝음 (어두운 쪽은 차갑게, 밝은 쪽은 따뜻하게) · 재질당 ≤ 5단 · 바탕 = 설계도 색 근처
MATERIALS = {
    # 두건: 설계도 e #1A5A3D · h #348054 · f #51A66C · 림 = 설계도 i #5CA695 보다 한 단계 밝게
    'hood':  dict(ramp=[hx('0C2E28'), hx('1A5A3D'), hx('348054'), hx('51A66C'), hx('86C88E')], cuts=[0.30, 0.55, 0.92, 9.0]),
    # 깃털 · 화살통 · 화살 깃: 회갈 → 흰 (설계도 n #938C81 · b #F4EAD4)
    'pale':  dict(ramp=[hx('3C3440'), hx('645C60'), hx('938C81'), hx('C2B8A4'), hx('F4EAD4')], cuts=[0.35, 0.62, 0.95, 9.0]),
    'skin':  dict(ramp=[hx('8A4E52'), hx('CA7E70'), hx('F2BC96')], cuts=[0.58, 0.92], warm=True),
    # 조끼 가죽: 설계도 s #452322 · u #69311A · r #966038 · q #C19159
    'vest':  dict(ramp=[hx('3A1C1E'), hx('69311A'), hx('966038'), hx('C19159')], cuts=[0.38, 0.72, 1.05], warm=True),
    # 바지: 설계도 D #371B1F · E #4E2315 · C #70462C
    'pants': dict(ramp=[hx('2A1620'), hx('4E2315'), hx('70462C'), hx('8C6448')], cuts=[0.40, 0.72, 1.02], warm=True),
    # 허리띠 · 장화 · 끈: 설계도 y #3C2028 · z #56322E · G #563432 · J #5C697C
    #   가장 밝은 단계 = 조끼 · 화살통 가죽 #966038 과 같은 색 (판정 2회차 뒤: 회청 #6E6A80 이라 어깨끈이 회색 띠로 보였음 · 색 수도 아낌)
    'dark':  dict(ramp=[hx('1E1426'), hx('3C2028'), hx('563432'), hx('966038')], cuts=[0.42, 0.78, 1.08], warm=True),
    # 장화: 바지보다 한 단계 어둡게 (설계도 G #563432 · H #341E2C) — 하체가 한 갈색으로 뭉치지 않게
    'boot':  dict(ramp=[hx('2A1828'), hx('3E2430'), hx('5A3A3C'), hx('7A5A54')], cuts=[0.50, 0.85, 1.15], warm=True),
    'brass': dict(ramp=[hx('B46826'), hx('EAAA3A')], cuts=[0.7]),
    # 화살촉: 가장 밝은 단계(Y 약 195)는 촉 날 1줄만 — 얼굴보다 밝지 않게
    'steel': dict(ramp=[hx('4E5A6E'), hx('8A96AA'), hx('BCC8D6')], cuts=[0.55, 9.0]),
}
EYE = hx('120B20')
EYE_HOT = hx('FFFCF0')
LIGHT = (0.3, -0.8, 0.52)
RIM_DIR = (0.5, -0.86)           # 림 라이트가 드는 바깥 방향 (위 · 오른쪽)


# ───────────── 형태 모델 (법선 = x 오른쪽 · y 아래 · z 보는 쪽)
def n_cone(tx, ty, bx, by, half):
    """뾰족한 두건: 꼭지 (tx, ty) 에서 밑동 가운데 (bx, by) · 밑동 반폭 half 로 벌어지는 원뿔 — 비탈이 위를 봄 (둥근 종 아님)"""
    def f(xx, yy):
        t = np.clip((yy + 0.5 - ty) / (by - ty), 0.05, 1.0)
        cx = tx + (bx - tx) * t
        u = np.clip((xx + 0.5 - cx) / (half * t), -0.97, 0.97)
        v = -0.75 + 0.7 * t                       # 꼭지 쪽 비탈이 위를 봄 → 밑동(얼굴 테 · 목 깃)으로 갈수록 그늘
        return u * 0.7, v, np.sqrt(np.clip(1 - u * u * 0.6, 0.05, 1))
    return f


def n_ball(cx, cy, rx, ry):
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.97, 0.97)
        v = np.clip((yy + 0.5 - cy) / ry, -0.97, 0.97)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def n_torso(cx, r, top, drop):
    """몸통: 세로 원기둥 + 어깨에서 위를 보는 둥근 어깨 (top 에서 drop 칸 동안)"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        v = -np.clip(1 - (yy + 0.5 - top) / drop, 0, 1) * 0.8
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def draw():
    c = Canvas(SIZE, MATERIALS, light=LIGHT)
    S = SIZE

    # ── 1 깃털 (두건 꼭지 왼쪽에서 왼쪽 위로 · 설계도 x11~13 · y12~14) — 끝이 살짝 휜 긴 잎 모양
    feather = poly_mask(path((64, 57), [(59, 51.5), (52, 47.5), (45.5, 45)], [(46, 50.5), (52, 55.5), (61, 61)]), S)
    p_feather = c.part('feather', feather, 'pale', n_plane(0.3, -0.6, 0.75), bias=0.32)

    # ── 2 화살통 (오른 어깨 뒤 · 설계도 x21~22 · y18~23) + 위로 나온 화살 깃 둘 (모양 · 기울기 다르게)
    #   (판정 1회차: 회색 바위 덩어리 → 깃 끝 셋을 길이 · 기울기 다르게 띄우고 사이를 어둡게, 원통 위 · 아래에 짙은 테)
    fl1 = poly_mask(path((83.5, 72), (83.5, 64), (86, 58.5), (88.6, 63), (88.6, 72)), S)
    fl2 = poly_mask(path((88.6, 72), (88.6, 61), (91.5, 55.5), (93.6, 61), (93.6, 72)), S)
    fl3 = poly_mask(path((93.6, 72), (93.6, 65), (96, 61.5), (96.6, 66), (96.6, 72)), S)
    p_fl1 = c.part('fletch1', fl1, 'pale', n_plane(-0.2, -0.6, 0.8), bias=0.0)
    p_fl2 = c.part('fletch2', fl2, 'pale', n_plane(0.2, -0.6, 0.8), bias=0.0)
    p_fl3 = c.part('fletch3', fl3, 'pale', n_plane(0.6, -0.4, 0.7), bias=0.0)
    quiver = poly_mask(path((83.5, 71.5), (97, 71.5), (96.5, 89), [(96, 91.5), (93, 92.5), (90, 92.5)], (84, 92.5)), S)
    p_quiv = c.part('quiver', quiver, 'vest', n_cyl_v(89, 7.5), bias=-0.02)      # 갈색 가죽 원통 (판정 2회차: 회색 돌 → 가죽)

    # ── 3 바지 (설계도 y27~29 · 두 다리 사이 짙은 그늘)
    pants = poly_mask(path((47.5, 104), (75.5, 104), (75.5, 107), (73, 108), (72.5, 120), (47.5, 120)), S)
    p_pants = c.part('pants', pants, 'pants', n_cyl_v(60, 16), bias=0.16)

    # ── 4 장화 (설계도 y30~31 — 작고 어두움 · 발끝이 바깥으로)
    boot_l = poly_mask(path((47.5, 117), (56.5, 117), (56.5, 124.5), (44, 124.5), [(44, 122.5), (45, 121.5), (47.5, 121)]), S)
    boot_r = poly_mask(path((63.5, 117), (72.5, 117), [(73, 120), (74.5, 121), (76, 122)], (76, 124.5), (63.5, 124.5)), S)
    p_bootl = c.part('boot_l', boot_l, 'boot', n_plane(-0.1, -0.6, 0.8), bias=-0.3)
    p_bootr = c.part('boot_r', boot_r, 'boot', n_plane(0.5, -0.5, 0.7), bias=-0.22)

    # ── 5 조끼 몸통 (설계도 x12~17 · y22~25) — 어깨에서 허리까지
    torso = poly_mask(path((46, 85), (74, 85), (73, 101), (47, 101)), S)
    p_torso = c.part('torso', torso, 'vest', n_torso(61, 16, 85, 6), bias=-0.02)
    # 앞섶 (가운데 밝은 속옷 V — 설계도 q x14~15 · y22~23)
    shirt = poly_mask(path((55, 85), (64.5, 85), (60, 94.5)), S)
    p_shirt = c.part('shirt', shirt, 'vest', n_plane(0.1, -0.2, 0.95), bias=0.12, line=False)

    # ── 6 소매 (설계도 x10~11 · x18~19 · y22~24) — 어깨가 둥글게 위를 봄
    sl_l = poly_mask(path((46.5, 85), [(41, 85), (37, 87.5), (36.5, 92)], (37, 97.5), (47.5, 97.5)), S)
    sl_r = poly_mask(path((73.5, 85), [(78, 85), (82.5, 87.5), (83.5, 92)], (83.5, 97.5), (72.5, 97.5)), S)
    p_sll = c.part('sleeve_l', sl_l, 'vest', n_torso(42, 7, 85, 5), bias=-0.06)
    p_slr = c.part('sleeve_r', sl_r, 'vest', n_torso(78, 7, 85, 5), bias=0.0)

    # ── 7 어깨끈 (오른 어깨 → 왼 허리 · 화살통을 멘 끈)
    strap = poly_mask(path((75, 85), (80, 85), (55, 101.5), (50, 101.5)), S)
    p_strap = c.part('strap', strap, 'dark', n_plane(0.3, -0.5, 0.8), bias=0.06)

    # ── 8 허리띠 = 가는 가죽 끈 (설계도 y26 · 2~3칸 굵기 · 살짝 처짐) + 왼쪽 매듭에서 늘어진 끈 두 가닥 (금 버클 없음)
    #   (판정 1회차: 검사 · 지도 제작자 · 거너 · 시계공과 같은 「넓은 금 띠 + 가운데 버클」 → 궁수는 끈 매듭)
    belt = poly_mask(path((44, 100.5), [(56, 101.5), (68, 101.5), (79.5, 100.5)], (79.5, 103), [(68, 104), (56, 104), (44, 103)]), S)
    p_belt = c.part('belt', belt, 'dark', n_plane(0.2, -0.4, 0.9), bias=0.0)
    #   (판정 2회차: 어두운 덩어리 → 매듭 양옆 고리 둘(가운데가 뚫려 바지가 보임) + 매듭에서 늘어진 끈 한 가닥)
    loop_l = ellipse_mask(49.5, 104.5, 4.2, 3.2, S) & ~ellipse_mask(49.8, 104.8, 2.2, 1.4, S)
    loop_r = ellipse_mask(59.8, 104.0, 3.6, 2.8, S) & ~ellipse_mask(60.0, 104.3, 1.8, 1.2, S)
    tails = poly_mask(path((53.5, 104), (57, 104), [(57.5, 108), (57, 111), (58.5, 114)], (56, 114.5), [(54.5, 111), (54.5, 108)]), S)
    p_tails = c.part('tails', tails | loop_l | loop_r, 'vest', n_plane(0.3, -0.5, 0.8), bias=-0.05)   # 끈 가죽 (회색 금속처럼 안 보이게)
    p_knot = c.part('knot', ellipse_mask(55.2, 102.6, 2.6, 2.4, S), 'dark', n_ball(56, 101.5, 3, 3), bias=0.4)

    # ── 10 손 (설계도 y25 — 좌우 다르게: 왼손 느슨한 주먹 · 오른손 화살을 쥠)
    p_handl = c.part('hand_l', poly_mask(path((37.5, 96.5), (46.5, 96.5), (47, 100), (45.5, 102.5), (39.5, 102.5), (37.5, 100)), S),
                     'skin', n_ball(43, 98, 5, 4.5), bias=-0.08)
    p_handr = c.part('hand_r', poly_mask(path((73, 96.5), (82.5, 96.5), (83, 99.5), (81.5, 102.5), (74.5, 102.5), (72.5, 100)), S),
                     'skin', n_ball(79, 97.5, 5, 4.5), bias=0.0)

    # ── 11 두건 (꼭지 = 설계도 x17 · y13 — 뾰족 · 오른쪽으로 살짝 기움, 밑동 x10~21 · 옆자락 · 목 깃)
    hood_pts = path((69.5, 47.5), [(78.5, 60.5), (88.5, 71)],
                    [(88.5, 77), (88, 82), (86.5, 86)], [(80, 87), (70, 86.5), (60, 86.5)],
                    (53, 87), (52.5, 90), (48.5, 90), (47.5, 87.5), (43, 87),
                    [(40.5, 84), (39.5, 79), (39.5, 72)], [(55.5, 62), (69.5, 47.5)])
    hood_n = n_cone(69.5, 48.5, 64, 74, 25)
    p_hood = c.part('hood', poly_mask(hood_pts, S), 'hood', hood_n, bias=0.05, smooth=1.6)

    # ── 12 얼굴 (두건 구멍 · 설계도 x12~19 · y19~21 — 턱으로 좁아짐)
    face = poly_mask(path((48, 72.5), (80, 72.5), (80, 78), [(79, 82), (75, 85.5), (69, 86)], (59, 86), [(53, 85.5), (49, 82), (48, 78)]), S)
    p_face = c.part('face', face, 'skin', n_plane(0.2, -0.3, 0.93), bias=0.3, smooth=0)

    c.tidy(4)
    c.contact_lines(skip={('hood', 'face'), ('torso', 'shirt'), ('pants', 'boot_l'), ('pants', 'boot_r'),
                          ('torso', 'strap'), ('feather', 'hood'), ('belt', 'knot'), ('tails', 'knot')})

    # ───────────── 덧그림 (큰 형태 위주 — 잔점 · 같은 간격 줄 없음)
    deep = ndimage.distance_transform_cdt(np.pad(c.mat >= 0, 1), metric='taxicab')[1:-1, 1:-1] >= 4

    def fold(pts, parts, side=-1, dark=-1, ridge=1):
        """긴 천 주름: 골 2칸(빛 반대쪽) + 등성이 1칸(빛 쪽) — 실루엣 3칸 안은 골을 안 파서 외곽이 두꺼워 보이지 않게"""
        cells = [(x, y) for x, y in line_cells(path(*pts)) if 0 <= y < S and 0 <= x < S and deep[y, x]]
        c.nudge(cells + [(x + side, y) for x, y in cells], dark, parts)
        if ridge:
            c.nudge([(x - side, y) for x, y in cells], ridge, parts)

    # 두건: 꼭지에서 흘러내리는 긴 주름 둘 (왼쪽 그늘 골 · 오른쪽 빛 받는 등성이) · 얼굴 둘레 접힌 테
    fold([[(66, 54), (60, 60), (54, 66), (50, 71)]], p_hood)
    fold([[(72, 55), (76, 61), (80, 68)]], p_hood, side=1, ridge=0)
    rim_face = line_cells(path([(47, 79), (47, 74), (52, 71.5)], [(60, 71), (70, 71), (81, 72)], (81, 79)))
    c.nudge([(x, y) for x, y in rim_face if c.pid[y, x] == p_hood], 1, p_hood)
    c.nudge([(x, y - 1) for x, y in rim_face if y - 1 >= 0 and c.pid[y - 1, x] == p_hood], 1, p_hood)
    c.nudge([(x, y - 3) for x, y in rim_face[2:-2]], -1, p_hood)
    c.nudge([(x, y) for y in range(64, 74) for x in range(40, 60) if x < 40 + (y - 62) * 1.3 + 6], -1, p_hood)
    # 옆자락 안쪽 그늘 (얼굴 양옆 — 설계도 j)
    c.nudge(line_cells([(46, 75), (46, 84)]) + line_cells([(45, 76), (45, 84)]), -1, p_hood)
    c.nudge(line_cells([(82, 75), (82, 84)]), -1, p_hood)
    # 목 깃 아랫단 그늘 · 왼쪽 처진 깃 끝 골
    c.nudge(line_cells([(55, 85), (85, 85)]), -1, p_hood)
    c.nudge(line_cells([(50, 86), (50, 89)]), -1, p_hood)
    # 깃털: 가운데 깃대 (밝은 쪽 위로) · 끝 어둡게 (설계도 a)
    c.nudge(line_cells([(61, 58), (47, 47)]), -1, p_feather)
    c.set_lvl([(x, y) for y in range(45, 49) for x in range(45, 50)], 1, p_feather)
    # 화살통: 아가리 테 (짙게) · 아래쪽 바닥 테 · 몸 가운데 가죽 띠 하나 (같은 간격 줄 아님)
    #   아가리 테 (짙은 가죽 3칸) · 바닥 테 (4칸) — 원통으로 읽히게, 가운데는 회색 원기둥 명암 그대로
    c.set_lvl([(x, y) for y in (72, 73, 74) for x in range(83, 98)], 1, p_quiv, mat='dark')
    c.set_lvl([(x, 72) for x in range(85, 97)], 2, p_quiv, mat='dark')
    c.set_lvl([(x, y) for y in (89, 90, 91, 92) for x in range(83, 98)], 1, p_quiv, mat='dark')
    # 깃 끝 셋 = 흰 깃 (가장 밝은 단계 3 = Y 약 183 — 얼굴 빛 Y 199 보다 어둡게) · 깃대 쪽 반은 한 단계 그늘
    for pf, mk in ((p_fl1, fl1), (p_fl2, fl2), (p_fl3, fl3)):
        c.set_lvl([(int(x), int(y)) for y, x in zip(*np.nonzero(mk & (c.pid == pf)))], 3, pf, fix=False)
    c.nudge(line_cells([(85, 64), (85, 71)]) + line_cells([(86, 62), (86, 71)]), -1, p_fl1)
    c.nudge(line_cells([(90, 61), (90, 71)]) + line_cells([(91, 59), (91, 71)]), -1, p_fl2)
    c.nudge(line_cells([(95, 67), (95, 71)]), -1, p_fl3)
    # 조끼: 앞섶 양쪽 테(왼쪽 빛 · 오른쪽 그늘) · 옆구리 솔기 그늘 · 아랫단
    c.nudge(line_cells([(54, 86), (59, 95)]), -1, p_torso)
    c.nudge(line_cells([(66, 86), (61, 95)]), -1, p_torso)
    fold([[(51, 88), (50, 94), (50, 99)]], p_torso, ridge=0)
    fold([[(69, 89), (70, 94), (70, 99)]], p_torso, side=1, ridge=1)
    c.nudge(line_cells([(48, 99), (72, 99)]), -1, p_torso)
    c.nudge(line_cells([(57, 88), (60, 93)]), -1, p_shirt)                  # 속옷 목 그늘
    # 소매: 팔꿈치로 모인 주름 하나씩 · 소맷부리 그늘
    fold([[(40, 89), (41, 94)]], p_sll, ridge=0)
    fold([[(80, 89), (79, 94)]], p_slr, side=1)
    c.nudge(line_cells([(37, 96), (47, 96)]), -1, p_sll)
    c.nudge(line_cells([(73, 96), (83, 96)]), -1, p_slr)
    # 어깨끈: 빛 받는 위 가장자리 1줄 · 끈 고리 (놋쇠)
    c.nudge(line_cells([(78, 85), (53, 101)]), 1, p_strap)
    c.stamp(['bbb', 'b.b', 'bbb'], 66, 91, {'b': ('brass', 0)})
    # 허리띠 끈: 윗단 빛 1줄 (끊지 않음) · 매듭 가운데 묶인 골 · 늘어진 끈 그늘 쪽
    c.nudge(line_cells(path([(44, 101), (56, 102), (68, 102), (79, 101)])), 1, p_belt)
    c.nudge(line_cells([(54, 104), (56, 101)]), -1, p_knot)                # 묶인 골 (사선 1줄)
    c.nudge(line_cells([(46, 106), (52, 107.5)]) + line_cells([(57, 106), (62, 106)]), -1, p_tails)   # 고리 아랫면 그늘
    c.nudge(line_cells([(55, 105), (55, 112)]), -1, p_tails)              # 늘어진 끈 왼쪽 그늘 (빛 반대)
    # 바지: 두 다리 사이 짙은 골 (설계도 D) · 오른 다리 빛 (설계도 F) · 무릎 아래 그늘
    gap = [(x, y) for y in range(108, 121) for x in range(57, 63)]
    c.set_lvl(gap, 0, p_pants)
    c.nudge(line_cells([(70, 106), (70, 116)]) + line_cells([(69, 108), (69, 116)]), 1, p_pants)
    c.nudge(line_cells([(48, 106), (56, 106)]) + line_cells([(64, 106), (73, 106)]), -1, p_pants)
    # 허리 아래 그늘 (조끼 아랫단 · 끈이 드리운 그림자 — 하체가 웃옷과 한 갈색으로 뭉치지 않게)
    c.set_lvl([(x, y) for y in (104, 105) for x in range(46, 78)], 0, p_pants)
    # 장화: 접힌 목 (위 1줄 밝게)
    for pb in (p_bootl, p_bootr):
        c.nudge([(x, 118) for x in range(40, 82)], 1, pb)
    # 왼손: 느슨한 주먹 (피부 2톤 b 바탕 · c 빛 + 손가락 경계선 a 1줄만)
    c.stamp(['..cccccc.',
             '.bbbbcccc',
             'bbbbbbbcc',
             '.aaaaaaa.',
             'bbbbbbbbb',
             '.bbbbbbb.'], 38, 97, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 2)})
    # 오른손: 편 손 (엄지가 앞 · 왼손 주먹과 다르게 — 손가락 경계선 1줄은 세로)
    c.stamp(['..cccccc.',
             '.bbcccccc',
             'bbbbccccc',
             'bbbabbbcc',
             'bbbabbbbb',
             '.bbabbbb.'], 74, 97, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 2)})
    # 얼굴: 두건 챙 아래 그늘 2줄 · 왼뺨 그늘 (빛 반대쪽) · 코 그늘
    c.set_lvl([(x, y) for y in (73, 74) for x in range(48, 81)], 1, p_face)
    c.set_lvl([(x, y) for y in range(75, 86) for x in range(48, 52)], 1, p_face)
    c.set_lvl([(63, 78), (63, 79), (63, 80), (64, 80)], 1, p_face)
    # 턱 그늘: 왼 아래로 넓어지는 곡선 (빛 반대쪽 · 목 깃 위) — 얼굴 한 색 덩어리를 둘로
    c.set_lvl([(x, y) for y in range(80, 87) for x in range(48, int(53 + (y - 79) ** 1.6))], 1, p_face)
    # 턱 아래 그늘 1단 (판정 2회차: 밝은 면 한 색 217칸 → 오른뺨 · 이마만 밝게 — 빛이 위 · 오른쪽이라 그늘은 왼쪽 · 아래)
    c.set_lvl([(x, y) for y in range(83, 87) for x in range(48, 81)], 1, p_face)

    # 림 라이트: 오른쪽 위 실루엣 안쪽 — 두건 2칸 · 깃털 · 화살통 · 소매 · 장화 1칸 (몸 전체는 설계도 밝기 그대로)
    c.rim((p_hood,), RIM_DIR, width=2, level=-1, thresh=0.3)
    c.absorb(c.pid == p_hood, min_size=3)
    c.rim((p_quiv, p_slr, p_bootr, p_feather), RIM_DIR, width=1, by=1, thresh=0.45)
    allp = np.isin(c.pid, [p_torso, p_sll, p_slr, p_pants, p_quiv, p_feather, p_belt, p_strap, p_bootl, p_bootr, p_shirt,
                           p_fl1, p_fl2, p_fl3, p_tails, p_knot])
    c.absorb(allp, min_size=3)
    # 흑백 계층: 깃털 · 화살 깃 · 화살통의 가장 밝은 단계(흰 #F4EAD4 · Y 234)는 쓰지 않음 → 얼굴 빛(Y 199)이 1위 (판정 1회차)
    pale = c.mat == c.mi('pale')
    c.lvl[pale] = np.minimum(c.lvl[pale], 3)
    # 깃 끝 사이 골 (세로 1줄 · 가장 어두운 단계) — 셋이 한 덩어리로 붙어 보이지 않게
    c.set_lvl([(89, y) for y in range(62, 72)], 0, p_fl2)
    c.set_lvl([(94, y) for y in range(66, 72)], 0, p_fl3)

    # 눈 (설계도 x14 · x17 → 가운데 57.5 · 69.5) — 짙은 눈동자 + 빛 쪽 위 반짝 1칸
    eye = ['ab', 'aa', 'aa']
    for ex in (56, 68):
        c.stamp(eye, ex, 75, {'a': EYE, 'b': EYE_HOT})
    dots = [(57, 75), (69, 75)]
    c.outline(warm_parts=(p_handl, p_handr))          # 장화는 어두워져(판정 1회차) 기본 외곽 — 가장자리 한 색 몫 75% 이상
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
