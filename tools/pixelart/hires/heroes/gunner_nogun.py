# 거너 메뉴 초상 128칸 (그래픽 가독성 개편 2단계 묶음 3 — 도적 시안 규칙(README.md) 그대로)
#   설계도 = Resources/Characters/gunner_nogun.png 첫 그림 (32칸 · 몸 14×20) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 12~15 짙은 보라 모자 꼭대기 (왼쪽 e 빛 · 오른 가장자리 f 청회) · 16 금 띠 · 17 넓은 챙 (양 끝 k 가 위로 말림)
#     18 챙 그늘 + 갈색 앞머리 · 19~20 얼굴 + 검은 눈 둘(x14 · x17) · 21~22 청록 목도리(입을 가림 · 왼쪽 t 흰 빛 · 가운데 매듭 u)
#     22~26 갈색 외투 (왼 소매 w · 어깨 x 황갈 · 오른 소매 z 회갈 · 가운데 짙은 조끼 D · 앞섶 B) · 25 양손 + 금 버클 · 27~29 보라 바지 · 30~31 장화
#   광원 = 설계도를 이어받아 위 · 조금 왼쪽 (모자 e · 목도리 t 가 왼쪽에서 밝음)
#   메뉴의 권총 덧그림(weapon_pistol, 10.3 화소/칸 — 세 번째 밀도)은 128칸 초상이면 생략됨 → 권총을 오른손에 쥔 채 그림 안에 그림
#     (예전 덧그림 자리 = 초상 오른쪽 아래 · 손 높이 · 총구 오른쪽) — 설계도 상자 × 4 안 (lint 14)
import numpy as np
from scipy import ndimage

from .portrait import Canvas, Map, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, n_cyl_v, n_cyl_h, despeckle

SIZE = 128
K = 4
M = Map(K, 0, -3)
DESIGN = 'Assets/Resources/Characters/gunner_nogun.png'     # 시트 첫 칸 (32×32) 이 설계도

# 재질: 어두움 → 밝음 · 재질당 ≤ 5단 · 바탕 = 설계도 색 근처 · 전체 ≤ 32색 (외곽 2 포함)
MATERIALS = {
    # 모자: 바탕 #3C3056 (설계도 d) · 빛 #5A4C78 (설계도 e) · 림 #7C80B0 (설계도 f 청회보다 한 단계 밝게)
    'hat':   dict(ramp=[hx('1C1430'), hx('2C2246'), hx('3C3056'), hx('5A4C78'), hx('7C80B0')], cuts=[0.30, 0.52, 0.86, 9.0]),
    # 금 띠 · 버클 (설계도 h · i · j)
    'gold':  dict(ramp=[hx('8A4A22'), hx('C8822E'), hx('ECB24A')], cuts=[0.55, 0.95], warm=True),
    'skin':  dict(ramp=[hx('8A4E52'), hx('CA7E70'), hx('F2BC96')], cuts=[0.55, 0.92], warm=True),
    # 손: 얼굴과 같은 살색이되 밝은 단계를 한 칸 낮춤 (Y 약 182 < 얼굴 200 — 흑백 1위는 얼굴 · 판정 1회차 공통)
    'hand':  dict(ramp=[hx('8A4E52'), hx('CA7E70'), hx('E8A888')], cuts=[0.55, 0.92], warm=True),
    # 목도리: 바탕 청록 #168298 · 빛 #32C8D0 (설계도 r) · 가장 밝은 단계는 왼쪽 위 접힌 테 1줄만 (얼굴보다 눈에 띄지 않게)
    #   (판정 1회차: 가장 밝은 단계 Y 212 > 얼굴 → Y 약 180 으로 낮춤 — 흑백 1위는 얼굴 · 눈)
    'scarf': dict(ramp=[hx('0E3A4E'), hx('168298'), hx('32B8C4'), hx('6AD2D4')], cuts=[0.42, 0.78, 9.0]),
    # 외투 · 앞머리: 바탕 #56322E (설계도 C) · #68382E (A) · 소매 빛 #965C3A (w) · 어깨 #C08650 (x)
    'coat':  dict(ramp=[hx('2D192A'), hx('46242C'), hx('623630'), hx('8E5838'), hx('BC8450')], cuts=[0.34, 0.60, 0.92, 9.0], warm=True),
    # 바지: 설계도 G #423E62 · H #576F8E
    'pants': dict(ramp=[hx('24203E'), hx('3C3A5C'), hx('56688A')], cuts=[0.50, 0.85]),
    # 장화 · 허리띠 가죽: 설계도 J #563432
    'boot':  dict(ramp=[hx('2E1C26'), hx('46282E'), hx('5E3A34'), hx('7E5848')], cuts=[0.40, 0.70, 1.0], warm=True),
    # 쇠 (권총 · 탄띠 고리): 바탕 Y 약 90 · 가장 밝은 단계는 총신 윗줄 1줄만 — Y 약 165 (판정 1회차: Y 206 이 얼굴보다 밝았음)
    'steel': dict(ramp=[hx('2A2E40'), hx('4E5A6E'), hx('9CA8BA')], cuts=[0.55, 9.0]),
}
EYE_HOT = hx('F4F0E6')            # 눈 반짝 (의도한 점 — 그림에서 가장 밝은 칸은 이것뿐)
LIGHT = (-0.2, -0.8, 0.56)
RIM_DIR = (-0.38, -0.92)          # 림 라이트가 드는 바깥 방향 (위 · 조금 왼쪽)


# ───────────── 형태 모델 (법선 = x 오른쪽 · y 아래 · z 보는 쪽)
def n_ball(cx, cy, rx, ry):
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -0.97, 0.97)
        v = np.clip((yy + 0.5 - cy) / ry, -0.97, 0.97)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def n_coat(cx, r, top, drop):
    """외투: 세로 원기둥 + 어깨에서 위를 보는 둥근 어깨 (top 에서 drop 칸 동안) + 아래로 갈수록 조금 그늘"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        v = -np.clip(1 - (yy + 0.5 - top) / drop, 0, 1) * 0.85 + np.clip((yy - 96) / 30.0, 0, 0.3)
        return u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.05, 1))
    return f


def n_brim(cx, r):
    """챙: 위를 보는 넓은 판 + 양 끝이 위로 말려 바깥을 봄"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        return u * 0.3, np.full_like(u, -0.75), np.full_like(u, 0.55)
    return f


def draw():
    c = Canvas(SIZE, MATERIALS, light=LIGHT)
    S = SIZE

    # ── 1 다리 (설계도 y27~29 · 두 다리 사이 틈 x14)
    p_legl = c.part('leg_l', poly_mask(path((42, 103), (55, 103), (55, 117), (43, 117)), S), 'pants', n_cyl_v(48, 7), bias=-0.12)
    p_legr = c.part('leg_r', poly_mask(path((61, 103), (74, 103), (73, 117), (61, 117)), S), 'pants', n_cyl_v(67, 7), bias=-0.04)

    # ── 2 장화 (설계도 y30~31 — 발끝이 바깥으로)
    boot_l = poly_mask(path((43, 116), (55, 116), (55, 124.5), (37, 124.5), [(37, 122), (39, 120), (43, 119.5)]), S)
    boot_r = poly_mask(path((61, 116), (73, 116), [(74, 119), (76.5, 119.5), (79, 121)], (79, 124.5), (61, 124.5)), S)
    p_bootl = c.part('boot_l', boot_l, 'boot', n_plane(-0.2, -0.6, 0.8), bias=-0.2)
    p_bootr = c.part('boot_r', boot_r, 'boot', n_plane(0.1, -0.6, 0.8), bias=-0.22)

    # ── 3 외투 몸통 (어깨 ~ 엉덩이 · 설계도 y22~26)
    coat = poly_mask(path((36, 92), [(36.5, 87.5), (40, 85), (47, 84.5)], (77, 84.5), [(83, 85), (85.5, 87.5), (86, 92)],
                          (83, 98), (82, 106), (42, 106), (39, 98)), S)
    p_coat = c.part('coat', coat, 'coat', n_coat(60, 26, 84.5, 8), bias=-0.02)
    # 조끼 (외투가 벌어진 가운데 · 설계도 x13~16 의 짙은 D · C)
    vest = poly_mask(path((53, 86), (68, 86), (69, 106), (52, 106)), S)
    p_vest = c.part('vest', vest, 'coat', n_plane(-0.1, -0.3, 0.9), bias=-0.32)
    # 탄띠 (설계도 y25~26 의 허리띠 자리 — 판정 1회차: 검사 · 지도 제작자 · 시계공과 같은 「넓은 금 가로 띠 + 가운데 버클」 반복 무늬
    #   → 거너는 짙은 가죽 탄띠가 오른 허리에서 왼 엉덩이로 비스듬히 처짐 · 버클은 왼쪽으로 치우친 작은 쇠 고리)
    belt = poly_mask(path((84, 94.5), (84.5, 98.5), (41, 106), (39.5, 102)), S)
    p_belt = c.part('belt', belt, 'boot', n_plane(-0.1, -0.4, 0.9), bias=-0.1)

    # ── 4 팔 (소매 · 설계도 x10 왼 · x19 오른 — 왼쪽은 빛 받는 갈색 w, 오른쪽은 회갈 z)
    #   왼 소매: 판정 1회차 「둥글게 부푼 국소 베개 명암」 → 평면 형태 모델 + 긴 주름 하나 (덧그림)
    p_arml = c.part('arm_l', poly_mask(path((37, 89), [(38, 86), (43, 85.5), (47, 88)], (48, 97), (40.5, 97.5), [(38.5, 97), (37.2, 96), (37, 94)]), S),
                    'coat', n_plane(-0.35, -0.45, 0.82), bias=-0.06)
    p_armr = c.part('arm_r', poly_mask(path((74, 88), [(77, 85.5), (83, 86), (85.5, 89)], (85, 95), (82, 97.5), (74, 97)), S),
                    'coat', n_cyl_v(80, 6, tilt=0.3), bias=-0.06)

    # ── 5 왼손 (빈 주먹 · 설계도 (10~11, 25))
    p_handl = c.part('hand_l', poly_mask(path((40, 96.5), (47, 96.5), (48, 99), (47, 103), (41.5, 103.5), [(39.5, 102.5), (38.5, 101), (38.8, 98.5)]), S),
                     'hand', n_ball(41, 98, 6, 5), bias=-0.06)

    # ── 6 권총 (오른손 · 총구 오른쪽 · 손잡이는 주먹 안을 지나 아래로 끝만 나옴)
    #   판정 1회차: 권총이 손 위에 떠 있었음 → 엄지가 총신 뒤를 덮고, 손잡이 뒷면이 주먹 왼쪽에 · 개머리가 주먹 아래로
    grip = poly_mask(path((68, 93), (75, 93), (74.5, 104.5), (68.5, 104.5)), S)
    p_grip = c.part('grip', grip, 'boot', n_plane(-0.2, -0.4, 0.85), bias=-0.04)
    slide = poly_mask(path((66.5, 88), (70.5, 88), (70.5, 90), (84, 90), (84, 88.5), (86.5, 88.5), (87.5, 90), (87.5, 95), (67, 95)), S)
    p_slide = c.part('slide', slide, 'steel', n_plane(-0.1, -0.5, 0.8), bias=0.0)
    guard = poly_mask(path((77, 95), (82, 95), (82, 98.5), (78, 98.5)), S)
    p_guard = c.part('guard', guard, 'steel', n_plane(0.0, 0.3, 0.9), bias=-0.4)
    p_handr = c.part('hand_r', poly_mask(path((69, 90), (72, 90), (73.5, 92), (76, 94), (77.5, 96.5), (76.5, 99.5), (69, 99.5)), S),
                     'hand', n_ball(71, 94, 6, 5), bias=-0.02)

    # ── 7 얼굴 (챙 아래 · 설계도 y18~20 — 위 1줄은 챙 그늘)
    face = poly_mask(path((47, 68), (81, 68), [(81.5, 74), (80.5, 79), (78, 82)], (50, 82), [(47.5, 79), (46.5, 74), (47, 68)]), S)
    p_face = c.part('face', face, 'skin', n_ball(64, 74, 24, 14), bias=0.42)
    # 앞머리 (설계도 x13~18 갈색 m · n — 챙 바로 아래 · 가운데가 조금 밝음)
    hair = poly_mask(path((49, 68), (79, 68), (79, 72.5), (74, 72.5), (71, 75), (68.5, 72.5), (57, 72.5), (54, 74.5), (51.5, 72.5), (49, 72.5)), S)
    p_hair = c.part('hair', hair, 'coat', n_plane(-0.1, -0.6, 0.8), bias=0.12, line=False, smooth=0)

    # ── 8 목도리 (입을 가리는 청록 천 · 설계도 y21~22 · 가운데 아래로 처진 삼각 끝)
    scarf = poly_mask(path((45, 81), [(54, 79.5), (70, 79.5), (83, 81)], (84, 85), [(77, 86), (71, 87.5), (66, 91)], (61, 91),
                           [(57, 88), (52, 86.5), (45, 85)]), S)
    p_scarf = c.part('scarf', scarf, 'scarf', n_cyl_h(82, 6, tilt=-0.25), bias=0.26)

    # ── 9 모자 (설계도 y12~17 — 꼭대기 폭 = 설계도 6칸 + 외곽 · 위는 살짝 눌린 홈)
    crown = poly_mask(path((46, 61), (46.5, 50), [(47, 47), (52, 45.5), (59, 47)], [(64, 46.5), (69, 45.5), (73, 47.5)], (73.5, 50), (74, 61)), S)
    p_crown = c.part('crown', crown, 'hat', n_cyl_v(59, 15, tilt=0.3), bias=-0.14)
    band = poly_mask(path((45, 60), (75, 60), (75.5, 65.5), (44.5, 65.5)), S)
    p_band = c.part('band', band, 'gold', n_cyl_v(62, 26), bias=0.0)
    brim = poly_mask(path((32, 64), (34.5, 64.5), [(37, 65.5), (41, 66), (46, 66)], (74, 66), [(80, 66), (83, 65.5), (85.5, 64.5)],
                          (88, 64), [(88, 66.5), (85, 69.5), (79, 70.5)], (47, 70.5), [(39, 70.5), (34, 69), (32, 66)]), S)
    p_brim = c.part('brim', brim, 'hat', n_brim(60, 28), bias=-0.2)
    hats = (p_crown, p_brim)

    c.tidy(4)
    c.contact_lines(skip={('coat', 'vest'), ('grip', 'hand_r'), ('slide', 'hand_r'), ('guard', 'hand_r'),
                          ('face', 'hair'), ('crown', 'band'), ('leg_l', 'boot_l'), ('leg_r', 'boot_r')})

    # ───────────── 덧그림 (큰 형태 위주 — 잔점 · 같은 간격 줄 없음)
    deep = ndimage.distance_transform_cdt(np.pad(c.mat >= 0, 1), metric='taxicab')[1:-1, 1:-1] >= 4

    def fold(pts, parts, side=1, dark=-1, ridge=1):
        """긴 천 주름: 골 2칸(빛 반대쪽 = 오른쪽) + 등성이 1칸(빛 쪽) — 실루엣 3칸 안은 안 팜"""
        cells = [(x, y) for x, y in line_cells(path(*pts)) if 0 <= y < S and 0 <= x < S and deep[y, x]]
        c.nudge(cells + [(x + side, y) for x, y in cells], dark, parts)
        if ridge:
            c.nudge([(x - side, y) for x, y in cells], ridge, parts)

    # 모자: 꼭대기 홈 그늘 · 왼쪽 세로 빛 띠 (설계도 e) · 챙 아랫단 그늘
    fold([[(59, 48), (60, 52), (60, 57)]], p_crown, side=1, ridge=0)
    c.nudge(line_cells([(51, 50), (51, 58)]) + line_cells([(52, 50), (52, 58)]), 1, p_crown)
    c.nudge(line_cells(path([(38, 69), (50, 69.5), (70, 69.5), (84, 68)])), -1, p_brim)
    # 금 띠: 아랫단 그늘 · 앞 매듭 고리
    c.nudge(line_cells([(46, 64), (75, 64)]), -1, p_band)
    # 앞쪽 금 윤기 (설계도 j — 눈 · 얼굴보다 어둡게 Y 약 185) · 판정 2회차: 1칸 직선 → 길이가 다른 2토막
    c.set_lvl(line_cells([(49, 61), (56, 61)]), 2, p_band)
    c.set_lvl(line_cells([(59, 61), (62, 61)]), 2, p_band)
    # 얼굴: 챙 그늘 (이마 위 2줄) · 코 그늘 · 오른 볼 그늘
    c.nudge([(x, y) for x in range(46, 83) for y in (68, 69)], -1, p_face)
    c.nudge(line_cells([(65, 75), (65, 78)]) + line_cells([(66, 76), (66, 78)]), -1, p_face)
    # 목도리: 왼쪽 위 접힌 테 (가장 밝은 1줄 · 설계도 t) · 매듭으로 모인 주름 · 아랫단 그늘
    c.set_lvl(line_cells(path([(47, 82), (52, 81), (58, 80.5)])), -1, p_scarf)
    fold([[(48, 84), (56, 85), (62, 88)]], p_scarf, side=0, ridge=0)
    fold([[(80, 82), (73, 84), (66, 88)]], p_scarf, side=0, ridge=0)
    c.nudge(line_cells([(70, 82), (79, 83)]), 1, p_scarf)
    # 외투: 앞섶 가장자리 (왼 자락 빛 받는 테) · 큰 세로 주름 둘 · 어깨 빛 (설계도 x 황갈)
    c.nudge(line_cells([(50, 87), (50, 97)]) + line_cells([(51, 87), (51, 97)]), 1, p_coat)
    fold([[(46, 89), (45, 96)]], p_coat, ridge=0)
    fold([[(73, 89), (75, 96)]], p_coat)
    c.nudge(line_cells([(70, 87), (71, 97)]), -1, p_coat)
    fold([[(48, 103), (47, 105)]], p_coat, ridge=0)
    # 조끼: 가운데 세로 줄 없음 (탄띠가 끊어 1칸 폭 토막이 되풀이됐음 — 판정 1회차 뒤)
    # 소매: 팔꿈치 주름 · 소맷부리 그늘
    c.set_lvl(line_cells([(39, 96), (47, 96)]), 0, p_arml)          # 판정 2회차: 소맷부리 = 손과의 경계 어두운 띠 1줄
    fold([[(78, 90), (82, 92)]], p_armr, side=0, ridge=0)
    # 탄띠: 윗단 빛 1줄 (비스듬히 · 끊지 않음) · 왼쪽으로 치우친 작은 쇠 고리 (테두리만)
    c.nudge(line_cells([(83, 95), (42, 103)]), 1, p_belt)
    c.stamp(['sss', 's.s', 'sss'], 49, 101, {'s': ('steel', 1)})
    # 왼 소매: 어깨에서 소맷부리로 흐르는 긴 주름 하나 (빛 쪽 왼편 등성이)
    fold([[(41, 88), (42.5, 92), (44, 96)]], p_arml, side=1, ridge=1)
    # 바지: 무릎 주름 하나씩 · 다리 사이 그늘
    fold([[(46, 106), (48, 113)]], p_legl, side=0, ridge=0)
    fold([[(70, 106), (68, 113)]], p_legr, side=0, ridge=0)
    # 장화: 접힌 목 (위 2줄 밝게)
    for pb in (p_bootl, p_bootr):
        c.nudge([(x, y) for x in range(36, 80) for y in (117, 118)], 1, pb)
    # 왼손: 힘 뺀 편 손 (손등 c 빛 · b 바탕 + 손가락 사이 경계선 a 세로 1줄) — 판정 2회차 묶음 공통: 양손이 같은 주먹 틀 → 오른손(권총 쥔 주먹)과 다른 모양
    c.stamp(['.cccc..',
             'cccccbb',
             'cccbbbb',
             'ccbbabb',
             'cbbbabb',
             '.bbbab.'], 40, 97, {'a': ('boot', 2), 'b': ('hand', 1), 'c': ('hand', 2)})   # 손가락 경계선은 손 그늘보다 한 단계 짙은 가죽색 (4배 화면에서 읽히게 · 새 색 없음)
    # 오른손: 손잡이를 감싼 주먹 — 엄지(c)가 총신 뒤를 덮고, 손가락 경계선(a) 1줄이 손잡이를 둘러 감음 ·
    #   주먹 왼쪽 g = 손잡이 뒷면 (주먹을 지나는 손잡이) · 검지는 방아쇠울 쪽으로 뻗음
    c.stamp(['.cc.....',
             'cccc....',
             'gcccbb..',
             'gcbbbbbb',
             'gaaaaaaa',
             'gbbbbbbbb',
             'gbbbbbbb.',
             'gbbbbbbb.',
             '.bbbbbb..'], 68, 91, {'a': ('hand', 0), 'b': ('hand', 1), 'c': ('hand', 2), 'g': ('boot', 1)})
    # 권총: 총신 윗줄 가장 밝은 1줄 · 총구 구멍 · 공이치기 · 방아쇠울 안 빈 곳
    c.set_lvl(line_cells([(72, 91), (86, 91)]), 2, p_slide)
    c.nudge(line_cells([(68, 94), (86, 94)]), -1, p_slide)
    c.stamp(['ss', 'ss'], 86, 92, {'s': ('steel', 0)})
    c.stamp(['ss', 'ss'], 79, 96, {'s': ('boot', 0)})

    # 림 라이트: 왼쪽 위 실루엣 안쪽 — 모자 2칸 띠 · 외투 어깨 · 목도리 1칸
    c.rim(hats, RIM_DIR, width=2, level=-1, thresh=0.3)
    c.absorb(np.isin(c.pid, hats + (p_band,)), min_size=3)
    c.rim((p_coat, p_arml), RIM_DIR, width=2, by=1, thresh=0.3)
    c.rim((p_bootl, p_legl), RIM_DIR, width=1, by=1, thresh=0.5)
    c.absorb(np.isin(c.pid, (p_coat, p_arml, p_armr, p_vest, p_scarf, p_face, p_legl, p_legr, p_bootl, p_bootr, p_belt)), min_size=3)
    c.absorb(np.isin(c.pid, (p_slide, p_grip)), min_size=3)        # 엄지 · 총신 뒤 끝에 남은 1~2칸 조각
    c.absorb(c.pid == p_handl, min_size=3)                          # 편 손 손가락 사이에 남은 1~2칸 조각 (경계선 3칸은 남음)

    # 눈 (검은 눈 · 설계도 x14 · x17 → 가운데 58 · 70) — 얼굴이 가장 밝은 판 위의 가장 짙은 점이라 시선이 감
    eye = ['waa', 'aaa', 'aaa']
    for ex in (56, 68):
        c.stamp(eye, ex, 73, {'a': ('hat', 0), 'w': EYE_HOT})
    dots = [(56, 73), (68, 73)]
    c.outline(warm_parts=(p_bootl, p_bootr))
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
