# 지도 제작자 메뉴 초상 128칸 (그래픽 가독성 개편 2단계 묶음 3 — 도적 시안 규칙 README.md 그대로)
#   설계도 = Resources/Characters/cartographer.png 첫 그림 (32칸 · 몸 16×19) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 13~15 갈색 모자 꼭대기 · 16 붉은 띠 · 17 좌우로 넓은 챙 (x9~20) · 18 챙 밑 그늘(머리카락)
#     19 파란 렌즈 안경 둘 (x13~15 · x17~19) · 20 아래 얼굴 + 짙은 깃 · 21~25 짙은 남색 코트 (금 어깨 · 짙은 앞섶 · 가운데 크림 두루마리 지도)
#     21~22 오른 어깨 뒤로 비스듬히 멘 망원경 (회색 쇠 + 크림 고리 + 갈색 가죽 · x20~22) · 25 양손 · 26 금빛 허리띠 · 27~29 어두운 바지 · 30~31 장화
#   광원 = 설계도를 이어받아 위 · 살짝 오른쪽 (코트 오른 가장자리가 밝은 파랑 E)
#   명도 계층: 렌즈 반짝 · 안경 안 눈 · 얼굴 > 망원경 쇠 1줄 · 두루마리 > 모자 띠 > 모자 · 코트 > 바지 > 안쪽 그늘
#   판정 1회차 → 망원경을 오른손에 쥐고 오른쪽 위로 기울임(설계도 x20~23 · y18~22) · 두루마리 · 망원경 마개 · 쇠를 얼굴보다 어둡게
#     · 왼 어깨 검은 덩어리 한 단계 올림 · 넓은 금 허리띠 + 가운데 버클(4명 반복 무늬) → 짙은 가죽 띠 + 오른쪽 지도 주머니(금 없음)
import numpy as np
from scipy import ndimage

from .portrait import Canvas, Map, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, despeckle

SIZE = 128
K = 4
M = Map(K, 0, -3)
DESIGN = 'Assets/Resources/Characters/cartographer.png'     # 시트 첫 칸 (32×32) 이 설계도

# 재질: 어두움 → 밝음 · 재질당 ≤ 5단 · 색 수 ≤ 32 를 지키려고 모자 · 가죽 · 종이는 같은 갈색 줄을 나눠 씀
BROWN = [hx('2E1C26'), hx('4A2C30'), hx('6A4034'), hx('8E5A42'), hx('B07C4C')]
MATERIALS = {
    # 모자: 바탕 #6A4034 ~ #8E5A42 (설계도 e #865436 · c #5C342A) · 빛 #B07C4C (설계도 d)
    'hat':     dict(ramp=BROWN, cuts=[0.28, 0.50, 0.82, 9.0], warm=True),
    # 모자 띠: 설계도 h #941E34 · i #D03C3A · j #F46E56
    'band':    dict(ramp=[hx('7A1830'), hx('C0343A'), hx('F06A56')], cuts=[0.55, 0.95]),
    'skin':    dict(ramp=[hx('8A4E52'), hx('CA7E70'), hx('F2BC96')], cuts=[0.62, 0.95], warm=True),
    # 렌즈: 설계도 o #709ED8 · p #B2E2FC (반짝 q #E2F6FF 는 의도한 점)
    'lens':    dict(ramp=[hx('709ED8'), hx('B2E2FC')], cuts=[0.8]),
    # 코트: 바탕 #2C3E96 (설계도 y #304CA8 · D #1E2A74 · u #1C1F54) · 빛 #3E5CBA · 림 #6A90DA (설계도 E #5A86C9 · z #5278D6 보다 한 단계)
    'coat':    dict(ramp=[hx('161838'), hx('222A6C'), hx('2C3E96'), hx('3E5CBA'), hx('6A90DA')], cuts=[0.30, 0.52, 0.88, 9.0]),
    # 바지: 설계도 J #484264 · K #26203C · M #5A728F
    'pants':   dict(ramp=[hx('2A2642'), hx('484264'), hx('5A728F')], cuts=[0.45, 0.88]),
    'leather': dict(ramp=BROWN[:4], cuts=[0.40, 0.70, 1.0], warm=True),
    # 종이(두루마리 지도): 설계도 F #BA8E64 · B #E8D09C · w #FAEEC6
    #   (판정 1회차: 두루마리 · 망원경 마개가 얼굴보다 밝았음 → 가장 밝은 단계를 Y 약 197 로 낮춤)
    'paper':   dict(ramp=[hx('A47A50'), hx('C8AC7E'), hx('D8C49A')], cuts=[0.55, 0.92], warm=True),
    # 금: 설계도 H #B46826 · x #EAAA3A · I #FEDE6C (가장 밝은 단계는 버클 1줄만)
    'gold':    dict(ramp=[hx('B46826'), hx('EAAA3A')], cuts=[0.55], warm=True),      # 어깨 장식 · 단추만 (허리띠 금 없앰)
    # 쇠(망원경 · 안경테): 설계도 m #76767A · C #AFAF9F · t #CCD8C2
    'steel':   dict(ramp=[hx('4E5462'), hx('7E848E'), hx('B4B8BE')], cuts=[0.55, 9.0]),     # 가장 밝은 단계 Y 약 183 (얼굴 아래)
}
GLINT = hx('E2F6FF')
LIGHT = (0.22, -0.9, 0.42)
RIM_DIR = (0.4, -0.92)


def n_ball(cx, cy, rx, ry):
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.97, 0.97)
        v = np.clip((yy + 0.5 - cy) / ry, -0.97, 0.97)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def n_dome(cx, cy, rx, ry):
    """모자 꼭대기: 위가 둥근 반구 (아래로 갈수록 앞을 봄)"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.98, 0.98)
        v = np.clip((yy + 0.5 - cy) / ry, -0.98, 0.05)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.04, 1))
    return f


def n_torso(cx, r, top, drop):
    """코트 몸통: 세로 원기둥 + 어깨에서 위를 보는 둥근 어깨"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        v = -np.clip(1 - (yy + 0.5 - top) / drop, 0, 1) * 0.85
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def n_cyl_v(cx, r):
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        return u, np.full_like(u, -0.25), np.sqrt(1 - u * u)
    return f


def n_cyl_axis(p0, p1, r):
    """기울어진 원기둥 (망원경) — 축 p0 → p1 에 수직인 거리로 둥근 면"""
    ax, ay = p1[0] - p0[0], p1[1] - p0[1]
    ln = (ax * ax + ay * ay) ** 0.5
    px, py = -ay / ln, ax / ln
    def f(xx, yy):
        d = np.clip(((xx + 0.5 - p0[0]) * px + (yy + 0.5 - p0[1]) * py) / r, -0.97, 0.97)
        return d * px, d * py, np.sqrt(1 - d * d)
    return f


def bar(p0, p1, w0, w1=None):
    """p0 → p1 축의 띠 다각형 (폭 w0 → w1)"""
    w1 = w0 if w1 is None else w1
    ax, ay = p1[0] - p0[0], p1[1] - p0[1]
    ln = (ax * ax + ay * ay) ** 0.5
    px, py = -ay / ln, ax / ln
    return [(p0[0] + px * w0 / 2, p0[1] + py * w0 / 2), (p1[0] + px * w1 / 2, p1[1] + py * w1 / 2),
            (p1[0] - px * w1 / 2, p1[1] - py * w1 / 2), (p0[0] - px * w0 / 2, p0[1] - py * w0 / 2)]


def lerp(p0, p1, t):
    return (p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t)


TUBE0, TUBE1 = (80.5, 97.0), (92.0, 78.0)      # (2회차 판정: 끝이 챙 오른끝에 닿음 → 조금 눕혀 챙과 2칸 이상 띄움)
#      # 망원경 축: 오른손 아래 대안 끝 → 오른쪽 위 대물 끝 (설계도 x20~23 · y18~22)


def draw():
    c = Canvas(SIZE, MATERIALS, light=LIGHT)
    S = SIZE

    # ── 2 바지 · 장화 (설계도 y27~31)
    legs = poly_mask(path((45, 103), (78, 103), (77, 117), (65, 117), (63, 113), (60, 113), (58, 117), (46, 117)), S)
    p_legs = c.part('legs', legs, 'pants', n_cyl_v(62, 18), bias=-0.12)
    boot_l = poly_mask(path((46, 116), (57.5, 116), (57.5, 125), (43, 125), [(43, 122.5), (44.5, 120.5), (46.5, 120)]), S)
    boot_r = poly_mask(path((64.5, 116), (76, 116), [(76.5, 119), (78.5, 120.5), (80, 121.5)], (80, 125), (64.5, 125)), S)
    p_bootl = c.part('boot_l', boot_l, 'leather', n_plane(-0.1, -0.6, 0.8), bias=-0.32)
    p_bootr = c.part('boot_r', boot_r, 'leather', n_plane(0.3, -0.6, 0.75), bias=-0.24)

    # ── 3 코트 (어깨 ~ 허리 · 설계도 y21~25, 왼 x9 ~ 오른 x20)
    #   어깨는 넓고 허리(허리띠 x11~19)로 좁아짐 — 아래가 네모 상자처럼 보이지 않게
    coat = poly_mask(path((47, 79.5), (80, 79.5), [(84, 80.5), (86.5, 83), (86.5, 88)], (85.5, 97), (82, 101), (44, 101),
                          (40, 97), (39.5, 88), [(39.5, 83), (42, 80.5), (47, 79.5)]), S)
    p_coat = c.part('coat', coat, 'coat', n_torso(64, 25, 79, 7), bias=0.0)
    # 조끼 (앞섶 사이 · 설계도 x13~16 — 코트보다 한 단계 밝은 파랑)
    vest = poly_mask(path((53, 82), (69, 82), (68.5, 101), (53.5, 101)), S)
    p_vest = c.part('vest', vest, 'coat', n_plane(0.15, -0.3, 0.9), bias=0.28)
    # 셔츠 깃 (설계도 z — 연파랑 · 턱 아래 V)
    collar = poly_mask(path((54, 81), (69, 81), (65.5, 86.5), (61.5, 88), (57.5, 86.5)), S)
    p_collar = c.part('collar', collar, 'coat', n_plane(0.2, -0.6, 0.75), bias=0.45, line=False)
    # 앞섶 (짙은 남색 · 설계도 u x12 · x17 — 위가 넓은 젖힌 깃 → 허리로 좁아짐)
    lap_l = poly_mask(path((46, 78), (53, 80.5), (56, 88), (53.5, 101), (49.5, 101), (50, 89)), S)
    lap_r = poly_mask(path((81, 78), (69, 80.5), (66.5, 88), (68.5, 101), (72.5, 101), (72.5, 89)), S)
    p_lapl = c.part('lapel_l', lap_l, 'coat', n_plane(-0.2, -0.7, 0.7), bias=-0.30)
    p_lapr = c.part('lapel_r', lap_r, 'coat', n_plane(0.4, -0.7, 0.6), bias=-0.30)
    # 두루마리 지도 (조끼 앞에 꽂은 크림 두루마리 — 설계도 x14~15 · y22~24, 위 끝이 말려 있음)
    scroll = poly_mask(path((57, 88), (64, 87), (64.5, 99), (57.5, 99.5)), S)
    p_scroll = c.part('scroll', scroll, 'paper', n_cyl_v(61, 5), bias=0.02)
    # 금 어깨 장식 (설계도 x10 · x19 y21 — 왼쪽은 다 보이고 오른쪽은 망원경 끈에 반쯤 가림)
    ep_l = poly_mask(path((40.5, 82), [(42, 79.5), (46, 79), (49, 79.5)], (48.5, 83), (41, 84.5)), S)
    ep_r = poly_mask(path((75, 79.5), [(79, 79), (83, 79.5), (85.5, 82)], (85, 84), (76, 83)), S)
    p_epl = c.part('ep_l', ep_l, 'gold', n_plane(0.0, -0.8, 0.6), bias=-0.65)
    p_epr = c.part('ep_r', ep_r, 'gold', n_plane(0.3, -0.8, 0.5), bias=-0.65)

    # 왼 어깨 · 소매 위쪽 그늘 한 단계 올림 (판정 1회차: 외곽 안쪽에서 가장 어두운 검은 덩어리)
    c.shift(poly_mask(path((39, 80), (50, 80), (50, 97), (39, 97)), S), 1, part=p_coat, hi=2)

    # ── 4 허리띠 (설계도 y26) — 짙은 가죽 띠 (금 · 가운데 버클 없음 · 판정 1회차: 4명이 같은 금 띠 반복) + 오른쪽 지도 주머니
    belt = poly_mask(path((43.5, 101), (82, 101), (81.5, 104), (44, 104)), S)
    p_belt = c.part('belt', belt, 'leather', n_plane(0.1, -0.6, 0.8), bias=-0.30)
    pouch = poly_mask(path((66, 100), (76.5, 100), [(77, 104), (76.5, 108), (75, 110)], (67.5, 110), [(66, 108), (65.5, 104), (66, 100)]), S)
    p_pouch = c.part('pouch', pouch, 'leather', n_plane(0.2, -0.4, 0.85), bias=0.0)

    # ── 망원경 (오른손에 쥐고 오른쪽 위로 기울임 — 설계도 x20~23 · y18~22 · 코트 앞 · 손 뒤 · 모자 챙 뒤)
    p_tube = c.part('tube', poly_mask(bar(TUBE0, TUBE1, 6.0, 7.0), S), 'steel', n_cyl_axis(TUBE0, TUBE1, 3.6), bias=-0.05)
    p_grip = c.part('grip', poly_mask(bar(lerp(TUBE0, TUBE1, 0.42), lerp(TUBE0, TUBE1, 0.56), 7.0), S), 'leather',
                    n_cyl_axis(TUBE0, TUBE1, 3.8), bias=0.05)
    p_cap = c.part('cap', poly_mask(bar(lerp(TUBE0, TUBE1, 0.84), TUBE1, 8.0, 8.0), S), 'paper',
                   n_cyl_axis(TUBE0, TUBE1, 4.2), bias=-0.10)

    # ── 5 손 (설계도 y25 · 좌우 다르게): 왼손 = 엄지를 세운 느슨한 주먹 · 오른손 = 멜빵 끝을 쥔 주먹
    p_handl = c.part('hand_l', poly_mask(path((40, 92), (47, 91.5), (48, 94), (47.5, 98), (41, 98.5), (39.5, 95.5)), S),
                     'skin', n_ball(44, 93, 5, 4.5), bias=-0.10)
    p_handr = c.part('hand_r', poly_mask(path((76, 91.5), (83, 92), (84.5, 95), (83, 98.5), (77, 98.5), (75.5, 95)), S),
                     'skin', n_ball(82, 93, 5, 4.5), bias=0.02)

    # ── 6 머리 (설계도 y18~20 — 챙 밑 그늘의 머리카락 · 얼굴)
    head = poly_mask(path((47, 68), (81, 68), (81, 78), [(80, 82), (75, 85.5), (64, 85.5)], [(53, 85.5), (48, 82), (47, 78)]), S)
    p_head = c.part('head', head, 'skin', n_ball(67, 70, 26, 24), bias=0.58)
    hair = poly_mask(path((46.5, 68), (81.5, 68), (81.5, 73), (79, 72.5), (73, 71.5), (55, 71.5), (50, 73), (50.5, 79), (47, 80)), S)
    p_hair = c.part('hair', hair, 'hat', n_plane(0.1, -0.2, 0.9), bias=-0.30, line=False)
    # 깃 (설계도 u x11 — 코트 깃이 턱 옆까지 세워짐)
    hi_col = poly_mask(path((44, 77), (49, 80), (52, 84), (47, 84), (43.5, 81)), S)
    p_hicol = c.part('hicol', hi_col, 'coat', n_plane(-0.3, -0.5, 0.8), bias=0.05)

    # ── 7 모자 (설계도 y13~17 — 좁은 꼭대기 + 붉은 띠 + 좌우로 넓은 챙)
    #   꼭대기는 거의 평평 + 가운데 살짝 눌림 (양 끝이 솟으면 귀처럼 보임 — 1회차 자기 검토)
    crown = poly_mask(path((51, 50), [(57, 49.5), (63, 51), (69, 49.5)], (72, 50), [(74.5, 52), (76, 57), (77, 63)],
                           (44, 63), [(45, 57), (46.5, 52), (51, 50)]), S)
    p_crown = c.part('crown', crown, 'hat', n_dome(62, 66, 20, 17), bias=0.0)
    band = poly_mask(path((44.5, 58.5), (76.5, 58.5), (77.5, 64.5), (43.5, 64.5)), S)
    p_band = c.part('band', band, 'band', n_plane(0.15, -0.3, 0.9), bias=-0.04)
    brim = poly_mask(path((32, 69.5), [(39, 65), (52, 63.5), (62, 63.5)], [(73, 63.5), (82, 64.5), (87, 68)],
                          [(86, 71), (78, 72.5), (64, 72.5)], [(50, 72.5), (40, 72.5), (32, 70.5)]), S)
    p_brim = c.part('brim', brim, 'hat', n_plane(0.15, -0.85, 0.5), bias=-0.05)

    # ── 8 안경 (설계도 y19 — 파란 렌즈 둘 + 가운데 다리 · 쇠테)
    p_rim = []
    for cx in (57.5, 72.5):
        p_rim.append(c.part('frame', ellipse_mask(cx, 76.5, 5.6, 4.6, S), 'steel', n_plane(0.2, -0.6, 0.75), bias=-0.35, line=False))
    p_lens = []
    for cx in (57.5, 72.5):
        p_lens.append(c.part('lens', ellipse_mask(cx, 76.5, 4.4, 3.5, S), 'lens', n_ball(cx + 1.5, 75.5, 4.5, 4), bias=0.25,
                             line=False, smooth=0))

    c.tidy(4)
    c.contact_lines(skip={('tube', 'cap'), ('tube', 'grip'), ('coat', 'vest'), ('vest', 'collar'), ('coat', 'lapel_l'),
                          ('coat', 'lapel_r'), ('vest', 'lapel_l'), ('vest', 'lapel_r'), ('head', 'hair'),
                          ('crown', 'band'), ('frame', 'lens'), ('head', 'frame'), ('hair', 'frame'),
                          ('collar', 'lapel_l'), ('collar', 'lapel_r'), ('coat', 'hicol')})

    # ───────────── 덧그림 (큰 형태 위주 — 잔점 · 같은 간격 줄 없음)
    deep = ndimage.distance_transform_cdt(np.pad(c.mat >= 0, 1), metric='taxicab')[1:-1, 1:-1] >= 4

    def fold(pts, parts, side=-1, dark=-1, ridge=1):
        """긴 천 주름: 골 2칸(빛 반대쪽) + 등성이 1칸(빛 쪽) — 실루엣 3칸 안은 안 팜"""
        cells = [(x, y) for x, y in line_cells(path(*pts)) if 0 <= y < S and 0 <= x < S and deep[y, x]]
        c.nudge(cells + [(x + side, y) for x, y in cells], dark, parts)
        if ridge:
            c.nudge([(x - side, y) for x, y in cells], ridge, parts)

    hats = (p_crown, p_brim)
    # 모자: 꼭대기 가운데 눌린 골 (탐험가 모자) · 챙 아랫단 그늘 · 챙 윗면 앞쪽 빛
    fold([[(60, 53), (61, 56), (61, 59)]], p_crown, ridge=1)
    c.nudge(line_cells(path([(37, 70.5), (50, 71.5), (64, 71.5)], [(64, 71.5), (78, 71.5), (85, 69.5)])), -1, p_brim)
    c.nudge(line_cells(path([(44, 66.5), (56, 65.5), (70, 65.5), (80, 66.5)])), 1, p_brim)
    c.nudge(line_cells(path([(35, 69), (42, 68), (50, 68.5)])), 1, p_brim)            # (2회차 판정: 챙 한 색 덩어리 → 왼쪽 앞 빛 1줄)
    # 오른쪽(빛 반대) 챙 그늘 면 — 크라운 그림자가 챙에 드리운 곡선 경계 (2회차 판정 뒤: 챙 한 색 264칸 → 200칸 이하)
    c.shift(poly_mask(path((72, 62), [(67, 65), (65.5, 69), (68, 74)], (90, 74), (90, 62)), S), -1, part=p_brim)
    # 모자 띠: 아랫단 그늘 1줄 · 오른쪽 매듭 겹 (설계도 j)
    c.nudge(line_cells([(45, 63), (76, 63)]), -1, p_band)
    c.stamp(['bbbb', 'cccb', 'bccb'], 67, 59, {'b': ('band', 1), 'c': ('band', 2)}, part=p_band)
    # 얼굴: 챙 밑 그늘(머리카락 아래 1줄 피부 그늘) · 코 옆 그늘 · 입
    c.nudge(line_cells([(51, 72), (80, 72)]), -1, p_head)
    c.set_lvl(line_cells([(62, 83), (67, 83)]), 0, p_head)
    c.nudge(line_cells([(64, 80), (66, 81)]), -1, p_head)
    c.nudge(line_cells(path([(49, 79), (50, 83), (55, 85)])), -1, p_head)
    # 안경 다리 (두 렌즈 사이 쇠 · 설계도 r)
    c.stamp(['ssss'], 63, 76, {'s': ('steel', 0)})
    # 코트: 소매 경계 큰 주름 (설계도 D x11 · x18) · 팔꿈치 겹 · 오른 자락 빛 받는 등성이
    fold([[(48, 85), (47.5, 92), (47, 99)]], p_coat)
    fold([[(76, 86), (76.5, 92), (77, 99)]], p_coat, side=1, ridge=0)
    c.nudge(line_cells([(79, 87), (80, 97)]) + line_cells([(80, 87), (81, 97)]), 1, p_coat)
    fold([[(42, 90), (43.5, 94)]], p_coat, ridge=0)
    # 조끼: 왼쪽(두루마리 그늘) 골 · 금 단추 둘 (위아래 크기 다르게)
    c.nudge(line_cells([(55, 89), (55, 99)]), -1, p_vest)
    # (2회차 판정: 조끼 위 금 단추 조각이 외투 위 주황 점으로 흩어짐 → 지움 · 금은 어깨 장식만)
    # 깃: 가운데 접힌 골
    c.nudge(line_cells([(61, 82), (61, 86)]), -1, p_collar)
    # 앞섶: 젖힌 깃 빛 받는 테 (왼 자락 = 바깥 1줄)
    c.nudge(line_cells([(47, 79), (53, 81), (55.5, 88)]), 1, p_lapl)
    c.nudge(line_cells([(70, 81), (67.5, 88)]), 1, p_lapr)
    # 두루마리: 위 끝 말린 소용돌이 (어두운 골 + 안쪽 빛) · 오른 가장자리 빛 1줄 · 가로 묶은 끈
    c.stamp(['.pppp..',
             'pPPPPpp',
             'pPaaaPp'], 57, 87, {'p': ('paper', 0), 'P': ('paper', 1), 'a': ('leather', 2)}, part=p_scroll)
    c.set_lvl(line_cells([(63, 91), (63, 98)]), 1, p_scroll)      # 가장자리 빛은 중간 단계 (얼굴보다 어둡게)
    c.stamp(['rrrrrrr', 'RRRRRRR'], 57, 94, {'r': ('band', 0), 'R': ('band', 1)}, part=p_scroll)
    c.nudge(line_cells([(58, 99), (64, 99)]), -1, p_scroll)
    # 금 어깨: 아래 술 그늘 1줄
    c.nudge(line_cells([(42, 83), (48, 82)]), -1, p_epl)
    c.set_lvl([(x, y) for x, y in line_cells([(41, 81), (43, 80), (48, 80)]) if c.pid[y, x] == p_epl], 1, p_epl)
    # 허리띠: 위 1줄만 한 단계 밝게 (가죽 윗단) · 지도 주머니 덮개 (밝은 윗단 + 아래 그늘 골) · 덮개 끈
    bm = c.pid == p_belt
    top = {}
    for y, x in zip(*np.nonzero(bm)):
        top[x] = min(top.get(x, 999), y)
    c.set_lvl([(x, y) for x, y in top.items()], 2, p_belt)
    flap = poly_mask(path((66, 100), (76.5, 100), (76, 104.5), (71.5, 106), (66.5, 104.5)), S)
    c.set_lvl([(int(x), int(y)) for y, x in zip(*np.nonzero(flap))], 3, p_pouch)
    c.nudge(line_cells(path([(67, 105), (71.5, 106.5), (76, 105)])), -1, p_pouch)
    c.nudge([(x, 101) for x in range(68, 77)], 1, p_pouch)
    # 바지: 두 다리 사이 골 (설계도 K) · 오른 다리 무릎 빛 (설계도 M)
    c.set_lvl(line_cells([(61, 105), (61, 113)]) + line_cells([(62, 105), (62, 113)]), 0, p_legs)
    c.nudge(line_cells([(71, 106), (72, 115)]) + line_cells([(72, 106), (73, 115)]), 1, p_legs)
    c.nudge(line_cells([(50, 107), (52, 115)]), -1, p_legs)
    # 장화: 접힌 목 위 1줄 빛 · 오른 발끝 쇠
    for pb in (p_bootl, p_bootr):
        c.nudge([(x, 117) for x in range(40, 84)], 1, pb)
    c.stamp(['TT'], 78, 122, {'T': ('steel', 0)})
    # 망원경: 빛 쪽(왼 위) 밝은 1줄 · 마개 경계 그늘 · 감개 아랫단 골
    ax = np.subtract(TUBE1, TUBE0); ax = ax / np.hypot(*ax)
    perp = (ax[1], -ax[0]) if ax[1] < 0 else (-ax[1], ax[0])      # 축에 수직 · 빛 쪽(왼 위)
    if perp[0] > 0: perp = (-perp[0], -perp[1])
    hl0, hl1 = lerp(TUBE0, TUBE1, 0.58), lerp(TUBE0, TUBE1, 0.82)
    c.set_lvl(line_cells([(hl0[0] + perp[0] * 1.6, hl0[1] + perp[1] * 1.6), (hl1[0] + perp[0] * 1.6, hl1[1] + perp[1] * 1.6)]), 2, p_tube)
    e0, e1 = lerp(TUBE0, TUBE1, 0.84), lerp(TUBE0, TUBE1, 0.42)
    q = (-perp[0], -perp[1])
    c.nudge(line_cells([(e0[0] - q[0] * 4, e0[1] - q[1] * 4), (e0[0] + q[0] * 4, e0[1] + q[1] * 4)]), -1, p_cap)
    c.nudge(line_cells([(e1[0] - q[0] * 4, e1[1] - q[1] * 4), (e1[0] + q[0] * 4, e1[1] + q[1] * 4)]), -1, p_grip)

    # 림 라이트: 위 · 오른쪽 실루엣 안쪽 — 모자 2칸 · 코트 2칸 · 나머지 1칸
    c.rim(hats, RIM_DIR, width=2, level=-1, thresh=0.3)
    c.absorb(np.isin(c.pid, hats + (p_band, p_hair)), min_size=3)
    c.rim((p_coat,), RIM_DIR, width=2, by=1, thresh=0.25)
    c.rim((p_tube, p_cap, p_legs, p_bootr, p_handr), RIM_DIR, width=1, by=1, thresh=0.5)
    # 1~2칸 조각 흡수 (코트 · 바지 · 장화 · 얼굴 — 주름 끝 · 림 끝)
    c.absorb(np.isin(c.pid, (p_coat, p_vest, p_lapl, p_lapr, p_legs, p_bootl, p_bootr, p_head, p_belt, p_pouch,
                              p_tube, p_cap, p_grip, p_epl, p_epr)), min_size=3)

    # 손 (손 도트 지도 — 피부 2톤 b 바탕 · c 빛 + 손가락 경계선 a 1줄): 왼손 느슨한 주먹 · 오른손 멜빵 끝 쥔 주먹 (엄지 위)
    c.stamp(['..ccc..',
             '.bbccc.',
             'bbbbbcc',
             'aaaaaab',
             'bbbbbbb',
             '.bbbbb.'], 40, 92, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 2)}, part=p_handl)
    c.stamp(['..ccc..',
             '.bcccc.',
             'bbacbcc',
             'bbabbbc',
             'bbabbb.',
             '.bbbb..'], 76, 92, {'a': ('skin', 0), 'b': ('skin', 1), 'c': ('skin', 2)}, part=p_handr)

    # 렌즈 반짝 (의도한 점 — 얼굴이 시선 1순위) · 렌즈 안 눈동자 (짙은 2칸)
    dots = []
    for cx in (57, 72):
        c.stamp(['ee', 'ee'], cx - 1, 76, {'e': ('coat', 0)})
        c.stamp(['g'], cx + 2, 74, {'g': GLINT})
        dots.append((cx + 2, 74))
    c.outline(warm_parts=(p_bootl, p_bootr))
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
