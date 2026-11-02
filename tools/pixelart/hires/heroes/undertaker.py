# 장의사 메뉴 초상 128칸 (새 영웅 · 도적 규칙(README.md) 그대로)
#   설계도 = Resources/Characters/undertaker.png 첫 그림(32칸 · 몸 14×30 — 9영웅 중 키 가장 큼) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 2~7 높은 실크햇(옆 곧은 통 · 은빛 띠 1줄) · 8~9 넓은 챙 · 11~14 창백한 회색 얼굴(어두운 눈 둘) · 15~16 하얀 셔츠 깃
#     15~27 청회색 긴 상복(가운데 단추 둘 · 옷깃 · 양옆 세로 주름) · 28~31 짙은 바지 · 신발
#   메뉴 초상 묘사(영웅 문서 13절): 실크햇이 위 테두리에 닿을 듯 · 삽 자루를 어깨에 · 왼손에 흙 묻은 장갑 · 뒤에 흐린 묘비 · 창백한 얼굴에 옅은 미소
#   광원 = 위 · 살짝 오른쪽 (모자 윗면 · 어깨 · 삽날이 밝음)
#   명도 계층: 눈 · 모자 띠 · 삽날 · 셔츠 깃 > 얼굴 > 상복 > 모자 > 바지 · 묘비
import numpy as np
from scipy import ndimage

from .portrait import Canvas, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, n_ellipsoid, n_cyl_v, n_cyl_h, despeckle

SIZE = 128
K = 4
DESIGN = 'Assets/Resources/Characters/undertaker.png'     # 시트 첫 칸 (32×32) 이 설계도

MATERIALS = {
    # 상복: 바탕 #545878 (설계도) · 그늘 #363850 · 빛 #8890B4
    'coat': dict(ramp=[hx('363A56'), hx('454A6A'), hx('5A6084'), hx('7078A0'), hx('929CC0')], cuts=[0.76, 0.90, 1.0, 1.055]),
    'hat': dict(ramp=[hx('3A3E5E'), hx('4C5276'), hx('6870A0'), hx('8E98C6')], cuts=[0.84, 0.97, 1.05]),
    'silver': dict(ramp=[hx('8A94B4'), hx('CCD4EC'), hx('FFFFFF')], cuts=[0.7, 1.0]),
    'skin': dict(ramp=[hx('7E869C'), hx('A4ACC0'), hx('CCD2E2'), hx('E8ECF8')], cuts=[0.5, 0.8, 1.06]),
    'shirt': dict(ramp=[hx('A4ACC8'), hx('E4E8F6'), hx('FFFFFF')], cuts=[0.6, 1.0]),
    'wood': dict(ramp=[hx('6A4224'), hx('9A6232'), hx('D09A50')], cuts=[0.55, 1.0], warm=True),
    'glove': dict(ramp=[hx('3C3A52'), hx('5C5A78')], cuts=[0.8]),
    'soil': dict(ramp=[hx('6E4A2C')], cuts=[], warm=True),
    'pants': dict(ramp=[hx('2E2E48'), hx('3E4060')], cuts=[0.9]),
    'shoe': dict(ramp=[hx('302E4A'), hx('484668')], cuts=[0.8]),
    'stone': dict(ramp=[hx('3E4260'), hx('505878')], cuts=[0.9]),
}
EYE = hx('120E22')
EYE_HOT = hx('F4F0FF')
LIGHT = (0.04, -0.8, 0.6)
RIM_DIR = (0.4, -0.92)


def n_folds(cx, r, per, amp, y0=60, y1=125):
    """세로 주름 천: 폭 per 칸마다 빛 · 그늘 띠가 번갈아 + 위에서 아래로 갈수록 어두움"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        nx = u * 0.5 + amp * np.sin((xx - cx) / per * 2 * np.pi)
        ny = -0.9 + 0.016 * np.clip(yy - y0, 0, y1 - y0) + 0.12 * np.sin(yy / 9.0 + xx / 17.0)
        return nx, ny, np.sqrt(1 - u * u) * 0.35 + 0.2
    return f


def draw():
    c = Canvas(SIZE, MATERIALS, light=LIGHT)
    S = SIZE
    yy, xx = c.yy, c.xx

    # ── 0 뒤 배경: 흐린 묘비 (오른쪽 뒤 · 어둡고 낮은 대비)
    tomb = poly_mask(path((36, 66), (36, 52), [(36, 44), (42, 41)], [(48, 44), (48, 52)], (48, 66)), S)
    p_tomb = c.part('tomb', tomb, 'stone', n_plane(0.1, -0.5, 0.8), bias=-0.05, line=True)

    # ── 1 삽 (어깨 뒤로 비스듬히 · 오른쪽 위에 날) — 자루 + 날
    shaft = poly_mask(path((85, 60), (89, 60), (87, 108), (83, 108)), S)
    p_shaft = c.part('shaft', shaft, 'wood', n_cyl_v(87, 2.6), bias=0.02)
    blade = poly_mask(path((88, 37), (92, 45), (92, 56), (84, 56), (84, 45)), S)
    p_blade = c.part('blade', blade, 'silver', n_plane(0.35, -0.5, 0.7), bias=0.05)
    sock = poly_mask(path((85, 55), (91, 55), (90, 62), (86, 62)), S)
    p_sock = c.part('sock', sock, 'silver', n_cyl_v(88, 3), bias=-0.15)

    # ── 2 다리 · 신발 (짙은 바지 · 끝이 바깥)
    leg_l = poly_mask(path((48, 104), (62, 104), (62, 120), (48, 120)), S)
    leg_r = poly_mask(path((66, 104), (80, 104), (80, 120), (66, 120)), S)
    p_legl = c.part('leg_l', leg_l, 'pants', n_cyl_v(55, 7), bias=-0.2)
    p_legr = c.part('leg_r', leg_r, 'pants', n_cyl_v(73, 7), bias=-0.2)
    shoe_l = poly_mask(path((44, 118), (62, 118), (62, 125.5), (42, 125.5), (42, 121)), S)
    shoe_r = poly_mask(path((66, 118), (84, 118), (86, 121), (86, 125.5), (66, 125.5)), S)
    p_shl = c.part('shoe_l', shoe_l, 'shoe', n_plane(-0.1, -0.5, 0.85), bias=-0.1)
    p_shr = c.part('shoe_r', shoe_r, 'shoe', n_plane(0.3, -0.5, 0.8), bias=-0.05)

    # ── 3 상복 (긴 상의 · 무릎 위까지 · 아래가 조금 퍼짐)
    coat = poly_mask(path((46, 62), (82, 62), [(86, 64), (88, 72)], (88, 100), (90, 112), (38, 112), (40, 100), (40, 72), [(41, 64), (46, 62)]), S)
    p_coat = c.part('coat', coat, 'coat', n_folds(64, 26, 12, 0.5, 62, 112), bias=-0.02)
    # 어깨 (둥글게)
    p_shl_ = c.part('sh_l', ellipse_mask(47, 70, 8, 6, S) & coat, 'coat', n_ellipsoid(47, 70, 8, 6), bias=-0.04)
    p_shr_ = c.part('sh_r', ellipse_mask(81, 70, 8, 6, S) & coat, 'coat', n_ellipsoid(81, 70, 8, 6), bias=0.06)
    # 오른쪽(보는 쪽 왼) 팔 소매 · 왼쪽 팔 소매 — 몸 옆 세로 면
    arm_l = poly_mask(path((46, 74), (40, 76), (38, 100), (46, 100), (48, 84)), S)
    arm_r = poly_mask(path((82, 74), (88, 76), (90, 98), (80, 98), (80, 84)), S)
    p_arml = c.part('arm_l', arm_l, 'coat', n_cyl_v(43, 5), bias=-0.12)
    p_armr = c.part('arm_r', arm_r, 'coat', n_cyl_v(85, 5), bias=-0.02)
    # 옷깃 (V) · 셔츠
    shirt = poly_mask(path((55, 61), (73, 61), (70, 80), (64, 86), (58, 80)), S)
    p_shirt = c.part('shirt', shirt, 'shirt', n_plane(0.0, -0.4, 0.9), bias=0.0)
    lapl = poly_mask(path((52, 62), (57, 62), (64, 88), (58, 90), (50, 76)), S)
    lapr = poly_mask(path((76, 62), (71, 62), (64, 88), (70, 90), (78, 76)), S)
    p_lapl = c.part('lap_l', lapl, 'coat', n_plane(-0.3, -0.5, 0.8), bias=0.02)
    p_lapr = c.part('lap_r', lapr, 'coat', n_plane(0.4, -0.5, 0.75), bias=0.12)

    # ── 4 장갑 손: 왼손 (흙 묻음 · 아래로 늘어뜨림) · 오른손 (삽 자루를 쥠)
    glove_l = ellipse_mask(43, 102, 5.8, 5.4, S)
    p_gl = c.part('glove_l', glove_l, 'glove', n_ellipsoid(43, 102, 5.8, 5.4), bias=0.0)
    glove_r = ellipse_mask(86, 92, 5.6, 5.4, S)
    p_gr = c.part('glove_r', glove_r, 'glove', n_ellipsoid(86, 92, 5.6, 5.4), bias=0.08)

    # ── 5 목 · 얼굴 · 모자
    neck = poly_mask(path((57, 56), (71, 56), (72, 64), (56, 64)), S)
    p_neck = c.part('neck', neck, 'skin', n_plane(0.0, -0.1, 0.9), bias=-0.35)
    face = ellipse_mask(64, 47, 14.5, 13, S) | poly_mask(path((52, 50), (76, 50), (74, 56), (70, 59), (58, 59), (54, 56)), S)
    p_face = c.part('face', face, 'skin', n_plane(0.05, -0.4, 0.9), bias=0.0)
    c.lvl[face] = 2
    # 모자: 높은 통 + 위로 살짝 넓어지는 윗면 + 넓은 챙
    crown = poly_mask(path((49, 30), (79, 30), (80, 13), [(80, 9), (76, 6.5)], (52, 6.5), [(48, 9), (48, 13)]), S)
    p_crown = c.part('crown', crown, 'hat', n_folds(64, 16, 20, 0.15, 6, 34), bias=0.0)
    top = poly_mask(path((50, 6.5), (78, 6.5), (80, 10), (48, 10)), S)
    p_top = c.part('top', top, 'hat', n_plane(0.0, -0.9, 0.5), bias=0.35)
    band = poly_mask(path((48.5, 22), (79.5, 22), (79.5, 28), (48.5, 28)), S)
    p_band = c.part('band', band, 'silver', n_cyl_v(64, 16, 0.1), bias=-0.05)
    brim = poly_mask(path((42, 31), (86, 31), (88, 34), [(87, 38), (84, 39)], (44, 39), [(41, 38), (40, 34)]), S)
    p_brim = c.part('brim', brim, 'hat', n_plane(0.1, -0.2, 0.9), bias=-0.12)

    c.tidy(4)
    c.contact_lines(skip={('coat', 'sh_l'), ('coat', 'sh_r'), ('coat', 'arm_l'), ('coat', 'arm_r'), ('sh_l', 'arm_l'), ('sh_r', 'arm_r'),
                          ('coat', 'shirt'), ('coat', 'lap_l'), ('coat', 'lap_r'), ('shirt', 'lap_l'), ('shirt', 'lap_r'), ('crown', 'top'),
                          ('crown', 'band'), ('neck', 'face'), ('coat', 'neck'), ('coat', 'face'), ('neck', 'shirt'), ('sh_l', 'lap_l'), ('sh_r', 'lap_r'),
                          ('leg_l', 'coat'), ('leg_r', 'coat'), ('leg_l', 'shoe_l'), ('leg_r', 'shoe_r'), ('tomb', 'coat'), ('tomb', 'arm_r'),
                          ('tomb', 'sh_r'), ('arm_l', 'glove_l'), ('arm_r', 'glove_r'), ('coat', 'glove_l'), ('coat', 'glove_r'), ('tomb', 'shaft'),
                          ('shaft', 'coat'), ('shaft', 'arm_r'), ('shaft', 'sh_r'), ('shaft', 'glove_r'), ('brim', 'face'), ('face', 'crown'),
                          ('shirt', 'neck'), ('lap_l', 'neck'), ('lap_r', 'neck'), ('arm_l', 'sh_l'), ('arm_r', 'sh_r'), ('leg_l', 'leg_r'),
                          ('tomb', 'leg_r'), ('shaft', 'leg_r'), ('blade', 'sock'), ('sock', 'shaft'), ('shaft', 'sock'), ('tomb', 'brim'), ('tomb', 'face'), ('tomb', 'sh_l'), ('tomb', 'glove_l'), ('tomb', 'arm_l'), ('sock', 'sh_r'), ('sock', 'coat')})

    # ───────────── 덧그림
    # 상복: 단추 둘 (은) · 가운데 여밈선 · 양옆 긴 세로 주름
    for by in (93, 101):
        c.set_lvl([(x, y) for y in range(by, by + 4) for x in range(62, 66)], 1, mat='silver')
    c.nudge(line_cells([(64, 88), (64, 110)]), -1, p_coat)
    c.nudge(line_cells([(52, 90), (50, 110)]) + line_cells([(53, 90), (51, 110)]), -1, p_coat)
    c.nudge(line_cells([(76, 90), (78, 110)]) + line_cells([(77, 90), (79, 110)]), -1, p_coat)
    c.nudge(line_cells([(42, 78), (42, 108)]), 1, p_coat)
    # 주머니 뚜껑 둘 (어두운 큰 덩어리) · 왼 소매 끝 셔츠 단 · 옷깃 바깥 빛
    c.set_lvl([(x, y) for y in range(96, 100) for x in range(46, 58)], 0, p_coat)
    c.set_lvl([(x, y) for y in range(96, 100) for x in range(71, 83)], 0, p_coat)
    c.set_lvl([(x, y) for y in range(93, 97) for x in range(39, 47)], 1, mat='shirt')
    c.nudge(line_cells([(54, 66), (60, 82)]) + line_cells([(55, 66), (61, 82)]) + line_cells([(56, 66), (62, 82)]), 1, p_lapl)
    c.nudge(line_cells([(74, 66), (68, 82)]) + line_cells([(73, 66), (67, 82)]), -1, p_lapr)
    # 아랫단 그늘
    c.nudge([(x, y) for y in (109, 110, 111) for x in range(36, 93)], -1, p_coat)
    # 가슴 주머니 손수건 (은빛 하얀 삼각 — 작은 덩어리)
    c.set_lvl([(x, y) for y in range(80, 85) for x in range(72 + (y - 80) // 2, 78)], 1, mat='shirt')
    # 모자: 윗면 빛 · 오른쪽 통 빛 · 띠 위아래 그늘 줄
    c.nudge(line_cells([(74, 12), (74, 21)]) + line_cells([(75, 12), (75, 21)]), 1, p_crown)
    c.nudge(line_cells([(52, 12), (52, 21)]) + line_cells([(53, 12), (53, 21)]), -1, p_crown)
    c.nudge([(x, 29) for x in range(50, 79)], -1, p_crown)
    c.nudge([(x, y) for y in (37, 38) for x in range(42, 87)], -1, p_brim)
    c.nudge([(x, 31) for x in range(44, 85)], 1, p_brim)
    # 얼굴: 광대 아래 그늘 · 턱 · 이마 빛
    fm = c.pid == p_face
    jaw = fm & ~ndimage.binary_erosion(fm, iterations=2) & (yy > 52)
    c.nudge([(int(x), int(y)) for y, x in zip(*np.nonzero(jaw))], -1, p_face)
    c.nudge(line_cells([(54, 44), (55, 52), (58, 56)]) + line_cells([(55, 44), (56, 52), (59, 56)]), -1, p_face)
    c.lvl[fm & ellipse_mask(68, 44, 6, 5, S) & (c.lvl == 2)] = 3
    # 삽: 날 가장자리 밝은 한 줄 · 날 가운데 그늘 · 자루 이음
    c.nudge(line_cells([(88, 40), (91, 47)]) + line_cells([(89, 40), (91, 46)]), 1, p_blade)
    c.nudge(line_cells([(86, 46), (86, 54)]) + line_cells([(87, 46), (87, 54)]), -1, p_blade)
    c.nudge(line_cells([(86, 70), (85, 100)]), 1, p_shaft)
    # 장갑: 손가락 골 · 왼손 흙 (납작한 갈색 한 덩어리)
    c.nudge(line_cells([(41, 100), (41, 107)]) + line_cells([(45, 100), (45, 107)]), -1, p_gl)
    c.set_lvl([(x, y) for y in range(103, 108) for x in range(38, 46) if c.pid[y, x] == p_gl and (x - 38) + (y - 103) * 0.5 < 7], 0, mat='soil')
    c.nudge(line_cells([(83, 90), (89, 90)]), -1, p_gr)
    c.nudge(line_cells([(83, 87), (89, 86)]), 1, p_gr)
    # 눈썹 · 입 (옅은 미소 · 입꼬리가 조금 올라감)
    BR, MO = hx('3E4260'), hx('5A4658')
    c.stamp(['aaaaa', 'aaaa.'], 52, 40, {'a': BR}, part=p_face)
    c.stamp(['aaaaa', '.aaaa'], 67, 40, {'a': BR}, part=p_face)
    c.stamp(['a.........a', '.aaaaaaaaa.'], 59, 53, {'a': MO}, part=p_face)

    # 림 라이트: 모자 윗면 · 어깨 · 삽날
    c.rim((p_crown, p_top, p_shr_, p_blade, p_shaft, p_armr), RIM_DIR, width=1, level=-1, thresh=0.35)
    c.absorb((c.pid >= 0), min_size=3)
    # 눈: 어두운 눈구멍 (작고 깊게) + 흰 반짝
    for ex in (55, 69):
        c.stamp(['aab', 'aaa', 'aaa', '.a.'], ex, 44, {'a': EYE, 'b': EYE_HOT})
    dots = [(57, 44), (71, 44)]
    c.outline(warm_parts=(p_shaft, p_gl, p_gr))
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
