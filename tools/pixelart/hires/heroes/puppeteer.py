# 인형사 메뉴 초상 128칸 (새 영웅 · 도적 규칙(README.md) 그대로)
#   설계도 = Resources/Characters/puppeteer.png 첫 그림(32칸 · 키 크고 가는 연출가) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 0~7 머리 위로 든 조종대(십자 · 가로 막대 y2~3 · 끝에서 흰 실 짧게) · 6~16 오른팔(소매)이 조종대를 쥠
#     8~16 머리(짙은 머리카락 · 창백한 얼굴 · 분홍 입) · 16 금 목 걸쇠 · 16~30 보라 망토(발목까지 곧게 · 왼쪽이 밝음) · 22~24 왼손(창백) · 30~31 짙은 신발
#   메뉴 초상 묘사(영웅 문서 13절): 오른손 조종대를 높이 · 왼손이 줄을 쥐고 T 자세 더미가 줄에 매달려 반쯤 들림 · 관객을 봄 · 망토 주름은 세로 · 더미 격자는 정확한 4×4
#   광원 = 위 · 살짝 오른쪽 (조종대 오른쪽 · 소매 · 얼굴 오른쪽이 밝음)
#   명도 계층: 눈 · 조종대 윗면 > 얼굴 · 더미 흰칸 > 망토 밝은 쪽 > 머리카락 · 망토 그늘
import numpy as np
from scipy import ndimage

from .portrait import Canvas, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, n_ellipsoid, n_cyl_v, n_cyl_h, despeckle

SIZE = 128
K = 4
DESIGN = 'Assets/Resources/Characters/puppeteer.png'     # 시트 첫 칸 (32×32) 이 설계도

MATERIALS = {
    'cloak': dict(ramp=[hx('34226A'), hx('452E86'), hx('5A3CA2'), hx('7452BE'), hx('9A74DC')], cuts=[0.80, 0.93, 1.0, 1.055]),
    'lining': dict(ramp=[hx('2E1A58'), hx('44287C')], cuts=[0.7]),
    'hair': dict(ramp=[hx('2C3652'), hx('3C4870'), hx('5A6A98')], cuts=[0.5, 0.95]),
    'skin': dict(ramp=[hx('9C7890'), hx('C8A4AE'), hx('EBD2D6'), hx('FFEEEE')], cuts=[0.45, 0.80, 1.08], warm=True),
    'wood': dict(ramp=[hx('6A4224'), hx('9A6232'), hx('D09A50')], cuts=[0.55, 1.0], warm=True),
    'thread': dict(ramp=[hx('C0C0DA'), hx('FFFFFF')], cuts=[0.7]),
    'gold': dict(ramp=[hx('C08A18'), hx('FFD050')], cuts=[0.7], warm=True),
    'boot': dict(ramp=[hx('302450'), hx('463872')], cuts=[0.7]),
    # 더미: 텍스처 없음 표시 (흰 · 마젠타 격자) + 관절 검정
    'dwhite': dict(ramp=[hx('B4B4D0'), hx('E6E6F6'), hx('FFFFFF')], cuts=[0.5, 0.95]),
    'dmag': dict(ramp=[hx('8A1068'), hx('D424AC'), hx('FF58E0')], cuts=[0.5, 0.95]),
    'dblack': dict(ramp=[hx('34324A')], cuts=[]),
}
EYE = hx('140E24')
EYE_HOT = hx('F4F0FF')
LIGHT = (0.0, -0.8, 0.6)
RIM_DIR = (0.4, -0.92)


def n_folds(cx, r, per, amp, tilt=0.25):
    """세로 주름 천: 가로로 오르내리는 법선 — 폭 per 칸마다 빛 · 그늘 띠가 번갈아 (긴 세로 주름)"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        nx = u * 0.5 + amp * np.sin((xx - cx) / per * 2 * np.pi)
        ny = -0.9 + 0.016 * np.clip(yy - 62, 0, 65) + 0.14 * np.sin(yy / 9.0 + xx / 17.0)
        return nx, ny, np.sqrt(1 - u * u) * 0.35 + 0.2
    return f


def n_grad(y0, y1):
    """위에서 아래로 빛을 덜 받는 판 (더미 · 줄 — 위가 밝음)"""
    def f(xx, yy):
        t = np.clip((yy - y0) / (y1 - y0), 0, 1)
        return np.full_like(t, 0.15), -0.9 + 1.0 * t, np.full_like(t, 0.45)
    return f


def draw():
    c = Canvas(SIZE, MATERIALS, light=LIGHT)
    S = SIZE
    yy, xx = c.yy, c.xx

    # ── 0 조종대 (가장 뒤): 세로 막대 + 가로 막대 + 막대 끝에서 늘어진 실 넷
    post = poly_mask(path((77, 3), (83, 3), (83, 40), (77, 40)), S)
    p_post = c.part('post', post, 'wood', n_cyl_v(80, 3.2), bias=0.02)
    bar = poly_mask(path((62, 8), (93, 8), (93, 14), (62, 14)), S)
    p_bar = c.part('bar', bar, 'wood', n_cyl_h(11, 3.4), bias=0.02)
    caps = []
    for cx in (63.0, 92.5):
        caps.append(c.part('cap%d' % int(cx), ellipse_mask(cx, 11, 2.6, 3.4, S), 'wood', n_ellipsoid(cx, 11, 2.6, 3.4), bias=-0.04))
    thr = []
    for i, tx in enumerate((64, 70, 86, 92)):
        L = (11, 8, 8, 12)[i]
        thr.append(c.part('thr%d' % i, poly_mask(path((tx - 1, 14), (tx + 1, 14), (tx + 1, 14 + L), (tx - 1, 14 + L)), S), 'thread', n_plane(0.3, -0.2, 0.9), bias=0.0, line=False))

    # ── 1 신발 · 망토 (발목까지 곧게 · 아래가 안감으로 갈라짐)
    boot_l = poly_mask(path((44, 116), (56, 116), (57, 125.5), (40, 125.5), (41, 120)), S)
    boot_r = poly_mask(path((62, 116), (74, 116), (78, 120), (79, 125.5), (62, 125.5)), S)
    p_bootl = c.part('boot_l', boot_l, 'boot', n_plane(-0.1, -0.5, 0.85), bias=-0.2)
    p_bootr = c.part('boot_r', boot_r, 'boot', n_plane(0.3, -0.5, 0.8), bias=-0.1)
    cloak = poly_mask(path((46, 62), (72, 62), [(76, 64), (78, 70)], (79, 96), (80, 120), (66, 121), (60, 119), (50, 121), (32, 120), (33, 96), (36, 72), [(38, 66), (46, 62)]), S)
    p_cloak = c.part('cloak', cloak, 'cloak', n_folds(56, 26, 11, 0.55), bias=-0.02)
    # 안감: 옷자락 아래가 열려 진보라가 보이는 곳 (앞쪽 갈라진 틈)
    slit = poly_mask(path((56, 96), (62, 96), (65, 121), (53, 121)), S) & cloak
    p_slit = c.part('slit', slit, 'lining', n_plane(0.0, -0.3, 0.9), bias=-0.15)
    # 어깨 덮개 (망토 위 둥근 어깨)
    sh_l = ellipse_mask(43, 70, 8, 6, S) & cloak
    sh_r = ellipse_mask(71, 70, 8, 6, S) & cloak
    p_shl = c.part('sh_l', sh_l, 'cloak', n_ellipsoid(43, 70, 8, 6), bias=-0.06)
    p_shr = c.part('sh_r', sh_r, 'cloak', n_ellipsoid(71, 70, 8, 6), bias=0.06)

    # ── 2 왼팔 (소매가 몸 옆으로 내려옴) · 줄을 쥔 왼손
    arm_l = poly_mask(path((42, 68), (36, 70), [(33, 76), (32, 82)], (33, 87), (43, 87), (44, 80), (45, 74)), S)
    p_arml = c.part('arm_l', arm_l, 'cloak', n_cyl_v(38, 6), bias=-0.2)
    hand_l = ellipse_mask(39, 89, 5.4, 4.6, S)
    p_handl = c.part('hand_l', hand_l, 'skin', n_ellipsoid(39, 89, 5.4, 4.6), bias=0.0)

    # ── 3 목 · 걸쇠 · 머리
    neck = poly_mask(path((52, 56), (62, 56), (63, 66), (51, 66)), S)
    p_neck = c.part('neck', neck, 'skin', n_plane(0.0, -0.1, 0.9), bias=-0.32)
    clasp = ellipse_mask(57, 63.5, 3.4, 2.4, S)
    p_clasp = c.part('clasp', clasp, 'gold', n_ellipsoid(57, 63.5, 3.4, 2.4), bias=0.0)
    head = ellipse_mask(56, 45, 14.5, 16.5, S) | poly_mask(path((44, 50), (68, 50), (66, 56), (62, 60), (50, 60), (46, 56)), S)
    p_face = c.part('face', head, 'skin', n_plane(0.05, -0.4, 0.9), bias=0.0)
    c.lvl[head] = 2
    hair = head & ((yy < 41) | (np.abs(xx - 56) > 11.5) & (yy < 56))
    hair |= ellipse_mask(56, 41, 17.2, 15.5, S) & (yy < 42)
    hair |= poly_mask(path((38, 42), (45, 42), (45, 58), (40, 60), (38, 50)), S) | poly_mask(path((67, 42), (74, 42), (74, 52), (72, 60), (67, 58)), S)
    p_hair = c.part('hair', hair, 'hair', n_ellipsoid(56, 44, 18, 18), bias=-0.02)

    # ── 4 오른팔 (조종대를 높이 든 팔 · 소매가 팔꿈치로 흘러내림) + 쥔 주먹
    sleeve = poly_mask(path((68, 72), (72, 64), (74, 38), (87, 38), (89, 56), (90, 68), (89, 76), (82, 78), (76, 76)), S)
    p_sleeve = c.part('sleeve', sleeve, 'cloak', n_folds(81, 9, 8, 0.35), bias=0.02)
    cuff = poly_mask(path((73, 37), (87, 37), (88, 42), (73, 42)), S)
    p_cuff = c.part('cuff', cuff, 'lining', n_plane(0.0, -0.3, 0.9), bias=0.2)
    fist = ellipse_mask(80, 31, 6.6, 6.0, S)
    p_fist = c.part('fist', fist, 'skin', n_ellipsoid(80, 31, 6.6, 6.0), bias=0.04)

    # ── 5 더미 (왼손이 쥔 줄에 매달려 반쯤 들림 · T 자세 · 얼굴 없음 · 4×4 격자)
    d_head = ellipse_mask(40, 101, 5.6, 5.6, S)
    d_arms = poly_mask(path((32, 108), (49, 108), (49, 113), (32, 113)), S)
    d_torso = poly_mask(path((36, 108), (45, 108), (45, 119), (36, 119)), S)
    d_legs = poly_mask(path((36, 119), (45, 119), (45, 124.5), (36, 124.5)), S) & ~poly_mask(path((40, 119), (41, 119), (41, 125), (40, 125)), S)
    dum = d_head | d_arms | d_torso | d_legs
    chk = (((xx.astype(int) // 4) + (yy.astype(int) // 4)) % 2) == 0
    p_dw = c.part('dwhite', dum & ~chk, 'dwhite', n_grad(96, 125), bias=0.1, smooth=0)
    p_dm = c.part('dmag', dum & chk, 'dmag', n_grad(96, 125), bias=0.1, smooth=0)
    joints = []
    for (jx, jy) in ((35, 108), (45, 108), (38, 119)):
        joints.append(c.part('dj%d' % jx, ellipse_mask(jx + 1.5, jy + 2.0, 2.6, 2.6, S) & dum, 'dblack', n_plane(0, 0, 1), bias=0.0, line=False, smooth=0))
        # 줄: 왼손에서 더미 머리로
    c.part('hold', poly_mask(path((39, 92), (41, 92), (41, 96), (39, 96)), S), 'thread', n_plane(0.3, -0.2, 0.9), line=False)

    c.tidy(4)
    c.contact_lines(skip={('cloak', 'sh_l'), ('cloak', 'sh_r'), ('cloak', 'arm_l'), ('cloak', 'sleeve'), ('cloak', 'slit'), ('sh_l', 'arm_l'),
                          ('neck', 'face'), ('face', 'hair'), ('cloak', 'face'), ('cloak', 'neck'), ('post', 'bar'), ('bar', 'cap63'), ('bar', 'cap94'),
                          ('sleeve', 'cuff'), ('sh_r', 'sleeve'), ('cloak', 'cuff'), ('sleeve', 'fist'), ('cuff', 'fist'),
                          ('dwhite', 'dmag'), ('dmag', 'dwhite'), ('dwhite', 'dj35'), ('dmag', 'dj35'), ('dwhite', 'dj45'), ('dmag', 'dj45'),
                          ('dwhite', 'dj38'), ('dmag', 'dj38'), ('neck', 'clasp'), ('cloak', 'clasp'), ('bar', 'thr0'), ('bar', 'thr1'), ('bar', 'thr2'), ('bar', 'thr3'),
                          ('cloak', 'hair'), ('sh_l', 'hair'), ('hair', 'neck'), ('arm_l', 'hand_l'),
                          ('cloak', 'boot_l'), ('cloak', 'boot_r'), ('boot_l', 'cloak'), ('boot_r', 'cloak'), ('sh_r', 'neck'), ('sh_l', 'neck')})

    # ───────────── 덧그림
    # 망토 주름: 세로 긴 줄 셋 (골 2칸 + 등성이 1칸) — 앞면
    # 왼쪽 가장자리 밝은 천 (설계도: 왼쪽이 밝음)
    c.nudge(line_cells([(35, 74), (34, 96), (33, 116)]) + line_cells([(36, 76), (35, 96), (34, 116)]), 1, p_cloak)
    # 앞 가운데 접힌 밑단 그늘
    c.nudge([(x, y) for y in (118, 119, 120) for x in range(34, 80)], -1, p_cloak)
    # 소매: 흘러내림 긴 줄 둘
    c.nudge(line_cells([(78, 46), (77, 60), (74, 70)]) + line_cells([(79, 46), (78, 60), (75, 70)]), -1, p_sleeve)
    c.nudge(line_cells([(85, 48), (87, 62), (88, 70)]), 1, p_sleeve)
    # 얼굴: 턱 아래 그늘 · 윗면 빛
    fm = c.pid == p_face
    jaw = fm & ~ndimage.binary_erosion(fm, iterations=2) & (yy > 54)
    c.nudge([(int(x), int(y)) for y, x in zip(*np.nonzero(jaw))], -1, p_face)
    lit = fm & ellipse_mask(61, 48, 6.5, 5.5, S) & (c.lvl == 2)
    c.lvl[lit] = 3
    # 머리카락: 오른쪽 위 빛 한 덩어리 · 앞머리 가르마 그늘
    c.set_lvl([(x, y) for y in range(26, 34) for x in range(54, 72) if c.pid[y, x] == p_hair and (x - 64) ** 2 / 70 + (y - 26) ** 2 / 14 < 1], 2, p_hair)
    c.nudge(line_cells([(52, 30), (50, 38), (49, 46)]) + line_cells([(53, 30), (51, 38), (50, 46)]), -1, p_hair)
    # 조종대: 윗면 빛 · 가로 막대 아래 그늘
    c.nudge([(x, 13) for x in range(62, 99)], -1, p_bar)
    c.nudge([(x, 8) for x in range(63, 98)], 1, p_bar)
    # 눈썹 · 입 (창백한 얼굴 · 분홍 입)
    BR, MO = hx('3A2A48'), hx('B0506E')
    c.stamp(['aaaaa', '.aaaa'], 47, 40, {'a': BR}, part=p_face)
    c.stamp(['aaaaa', 'aaaa.'], 59, 40, {'a': BR}, part=p_face)
    c.stamp(['a.....a', '.aaaaa.'], 53, 54, {'a': MO}, part=p_face)

    # 림 라이트 (위 · 오른쪽)
    c.rim((p_hair, p_shr, p_sleeve, p_fist, p_post, p_bar), RIM_DIR, width=1, level=-1, thresh=0.35)
    c.absorb((c.pid >= 0), min_size=3)

    # 눈: 큰 짙은 눈동자 + 반짝 (관객을 봄)
    for ex in (49, 61):
        c.stamp(['aab', 'aaa', 'aaa', '.a.'], ex, 44, {'a': EYE, 'b': EYE_HOT})
    dots = [(51, 44), (63, 44)]
    c.outline(warm_parts=(p_fist, p_face, p_neck, p_handl, p_clasp, p_post, p_bar) + tuple(caps))
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
