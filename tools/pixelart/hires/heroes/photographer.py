# 사진사 메뉴 초상 128칸 (새 영웅 · 도적 규칙(README.md) 그대로)
#   설계도 = Resources/Characters/photographer.png 첫 그림(32칸 · 몸 18×23 — 어깨 뒤로 접힌 삼각대가 오른쪽 위로 솟음) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 9~22 오른쪽 어깨 뒤 대각선 회색 막대 셋(삼각대) · 11~13 갈색 머리 · 14~17 얼굴 앞 검정 네모 카메라(렌즈 청백 한 점)
#     18~25 흰 재킷(어깨 패드 없음 · 단추 검정 둘 · 팔을 올려 카메라를 받침) · 26~30 짙은 바지 · 31 갈색 신발
#   메뉴 초상 묘사(영웅 문서 13절): 카메라를 눈에서 막 뗀 모습(가슴 앞) · 한쪽 눈 찡그림 · 재킷 주머니에 필름 통 둘 · 등 뒤 삼각대 다리 · 렌즈에 작은 적 무리가 비침
#   광원 = 위 · 살짝 오른쪽 (머리 윗면 · 어깨 · 렌즈 반짝이 밝음)
#   명도 계층: 렌즈 반짝 · 눈 > 재킷 흰 > 얼굴 > 카메라 · 삼각대 > 바지 · 머리
import numpy as np
from scipy import ndimage

from .portrait import Canvas, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, n_ellipsoid, n_cyl_v, n_cyl_h, despeckle

SIZE = 128
K = 4
DESIGN = 'Assets/Resources/Characters/photographer.png'     # 시트 첫 칸 (32×32) 이 설계도

MATERIALS = {
    # 재킷: 바탕 #BAC0D6 (설계도) · 그늘 #7C82A2 · 빛 #EEF0F8
    'jacket': dict(ramp=[hx('7C82A2'), hx('A0A8C6'), hx('C4CADE'), hx('EEF0F8')], cuts=[0.84, 0.97, 1.05]),
    'hair': dict(ramp=[hx('4C3C28'), hx('6A5232'), hx('8E6E3C')], cuts=[0.7, 1.02]),
    'skin': dict(ramp=[hx('A8704C'), hx('D49C72'), hx('F4C09A')], cuts=[0.6, 1.0], warm=True),
    'cam': dict(ramp=[hx('1E1C34'), hx('2C2C48'), hx('3E4262')], cuts=[0.6, 1.0]),
    'camtop': dict(ramp=[hx('7C84A8'), hx('B4BCDA'), hx('E8ECFA')], cuts=[0.6, 1.0]),
    'glass': dict(ramp=[hx('1C2870'), hx('3A58C0'), hx('78A0F0'), hx('D4E2FC')], cuts=[0.45, 0.8, 1.05]),
    'pod': dict(ramp=[hx('585E80'), hx('8A92B4'), hx('B8C0DC')], cuts=[0.6, 1.0]),
    'pants': dict(ramp=[hx('2E2C46'), hx('3E3E5A')], cuts=[0.9]),
    'shoe': dict(ramp=[hx('50403C'), hx('6C5C54')], cuts=[0.8]),
    'film': dict(ramp=[hx('34344E'), hx('5C5C7C')], cuts=[0.8]),
    'label': dict(ramp=[hx('D8542C')], cuts=[], warm=True),
}
EYE = hx('201418')
EYE_HOT = hx('EEF0F8')
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

    # ── 0 삼각대 (등 뒤 · 어깨 위로 비스듬히 솟음 · 막대 둘 + 머리 마운트)
    pod_a = poly_mask(path((91, 36), (96, 36), (82, 84), (77, 84)), S)
    pod_b = poly_mask(path((97, 40), (102, 40), (90, 88), (85, 88)), S)
    p_poda = c.part('pod_a', pod_a, 'pod', n_cyl_v(86.5, 3.0, 0.1), bias=0.02)
    p_podb = c.part('pod_b', pod_b, 'pod', n_cyl_v(93.5, 3.0, 0.1), bias=0.05)
    mount = poly_mask(path((90, 33), (103, 33), (103, 40), (90, 40)), S)
    p_mount = c.part('mount', mount, 'cam', n_plane(0.2, -0.6, 0.7), bias=0.1)
    knob = ellipse_mask(86, 58, 3.2, 3.2, S)
    p_knob = c.part('knob', knob, 'camtop', n_ellipsoid(86, 58, 3.2, 3.2), bias=-0.02)

    # ── 1 신발 · 바지
    shoe_l = poly_mask(path((44, 119), (59, 119), (59, 124.5), (40, 124.5), (40, 122)), S)
    shoe_r = poly_mask(path((66, 119), (81, 119), (84, 122), (84, 124.5), (66, 124.5)), S)
    p_shl = c.part('shoe_l', shoe_l, 'shoe', n_plane(-0.1, -0.5, 0.85), bias=-0.1)
    p_shr = c.part('shoe_r', shoe_r, 'shoe', n_plane(0.3, -0.5, 0.8), bias=0.0)
    pants = poly_mask(path((46, 108), (78, 108), (78, 120), (63, 120), (62, 112), (62, 112), (61, 120), (46, 120)), S)
    p_pants = c.part('pants', pants, 'pants', n_cyl_v(62, 17, 0.1), bias=-0.12)

    # ── 2 재킷 (흰 · 허리까지 · 단추 검정 둘)
    jacket = poly_mask(path((46, 78), (78, 78), [(84, 80), (86, 86)], (87, 110), (37, 110), (38, 86), [(40, 80), (46, 78)]), S)
    p_jacket = c.part('jacket', jacket, 'jacket', n_folds(62, 24, 10, 0.4, 78, 112), bias=0.0)
    # 팔: 팔꿈치가 바깥으로 · 카메라를 받침
    sl_l = poly_mask(path((46, 80), (40, 82), [(34, 88), (32, 96)], (36, 102), (44, 104), (48, 96), (49, 86)), S)
    sl_r = poly_mask(path((78, 80), (84, 82), [(90, 88), (92, 96)], (88, 102), (80, 104), (76, 96), (75, 86)), S)
    p_sll = c.part('sl_l', sl_l, 'jacket', n_cyl_v(41, 8), bias=-0.12)
    p_slr = c.part('sl_r', sl_r, 'jacket', n_cyl_v(84, 8), bias=0.02)
    # 주머니 (아랫배 둘 · 뚜껑) + 필름 통
    film_l = poly_mask(path((46, 100), (53, 100), (53, 110), (46, 110)), S)
    film_r = poly_mask(path((70, 100), (77, 100), (77, 110), (70, 110)), S)
    p_fl = c.part('film_l', film_l, 'film', n_cyl_v(49.5, 3.6), bias=0.0)
    p_fr = c.part('film_r', film_r, 'film', n_cyl_v(73.5, 3.6), bias=0.05)
    pocket_l = poly_mask(path((42, 105), (58, 105), (58, 112), (42, 112)), S)
    pocket_r = poly_mask(path((66, 105), (82, 105), (82, 112), (66, 112)), S)
    p_pkl = c.part('pocket_l', pocket_l, 'jacket', n_plane(0.0, -0.3, 0.9), bias=-0.2)
    p_pkr = c.part('pocket_r', pocket_r, 'jacket', n_plane(0.0, -0.3, 0.9), bias=-0.2)

    # ── 3 머리: 목 · 얼굴 · 갈색 머리
    neck = poly_mask(path((55, 72), (69, 72), (70, 80), (54, 80)), S)
    p_neck = c.part('neck', neck, 'skin', n_plane(0.0, -0.1, 0.9), bias=-0.3)
    head = ellipse_mask(62, 59, 16.5, 16.5, S) | poly_mask(path((48, 62), (76, 62), (73, 71), (68, 75), (56, 75), (51, 71)), S)
    p_face = c.part('face', head, 'skin', n_plane(0.05, -0.4, 0.9), bias=0.0)
    c.lvl[head] = 2
    hair = ellipse_mask(62, 54, 18.2, 15, S) & (yy < 56)
    hair |= poly_mask(path((44, 50), (50, 50), (50, 64), (46, 64)), S) | poly_mask(path((74, 50), (80, 50), (79, 64), (74, 64)), S)
    hair &= ellipse_mask(62, 56, 19, 19, S)
    p_hair = c.part('hair', hair, 'hair', n_ellipsoid(62, 56, 19, 17), bias=0.0)

    # ── 4 카메라 (가슴 앞 · 막 눈에서 뗌): 몸통 + 윗판 + 렌즈 + 플래시
    body = poly_mask(path((43, 82), (81, 82), (81, 105), (43, 105)), S)
    p_cam = c.part('cam', body, 'cam', n_plane(0.1, -0.5, 0.8), bias=0.0)
    top = poly_mask(path((43, 82), (81, 82), (81, 87), (43, 87)), S)
    p_ctop = c.part('camtop', top, 'camtop', n_plane(0.0, -0.9, 0.5), bias=0.1)
    flash = poly_mask(path((68, 76), (75, 76), (75, 82), (68, 82)), S)
    p_flash = c.part('flash', flash, 'camtop', n_cyl_v(71.5, 3.6), bias=0.1)
    finder = poly_mask(path((52, 78), (59, 78), (59, 82), (52, 82)), S)
    p_finder = c.part('finder', finder, 'cam', n_plane(0.0, -0.5, 0.8), bias=0.0)
    ring = ellipse_mask(62, 95, 10.5, 10.5, S)
    p_ring = c.part('ring', ring, 'cam', n_ellipsoid(62, 95, 10.5, 10.5), bias=-0.3)
    silver = ellipse_mask(62, 95, 9.4, 9.4, S) & ~ellipse_mask(62, 95, 7.6, 7.6, S)
    p_silver = c.part('silver', silver, 'camtop', n_ellipsoid(62, 95, 9.4, 9.4), bias=-0.1, line=False)
    lens = ellipse_mask(62, 95, 7.6, 7.6, S)
    p_lens = c.part('lens', lens, 'glass', n_ellipsoid(62, 95, 7.6, 7.6), bias=-0.05)

    # ── 5 손 (카메라 양옆을 받침)
    hand_l = ellipse_mask(43, 97, 5.0, 5.4, S)
    hand_r = ellipse_mask(81, 97, 5.0, 5.4, S)
    p_hl = c.part('hand_l', hand_l, 'skin', n_ellipsoid(43, 97, 5, 5.4), bias=-0.4)
    p_hr = c.part('hand_r', hand_r, 'skin', n_ellipsoid(81, 97, 5, 5.4), bias=-0.3)

    # 카메라 끈: 카메라 윗모서리에서 목 뒤로 (검은 띠)
    strap_l = poly_mask(path((49, 82), (54, 82), (58, 74), (55, 73)), S)
    strap_r = poly_mask(path((70, 82), (75, 82), (69, 73), (66, 74)), S)
    p_strl = c.part('strap_l', strap_l, 'cam', n_plane(0.0, -0.5, 0.8), bias=-0.1)
    p_strr = c.part('strap_r', strap_r, 'cam', n_plane(0.0, -0.5, 0.8), bias=-0.1)
    c.tidy(4)
    c.contact_lines(skip={('jacket', 'sl_l'), ('jacket', 'sl_r'), ('jacket', 'pocket_l'), ('jacket', 'pocket_r'), ('jacket', 'film_l'), ('jacket', 'film_r'),
                          ('film_l', 'pocket_l'), ('film_r', 'pocket_r'), ('neck', 'face'), ('face', 'hair'), ('jacket', 'neck'), ('jacket', 'face'),
                          ('pants', 'jacket'), ('pants', 'pocket_l'), ('pants', 'pocket_r'), ('pants', 'shoe_l'), ('pants', 'shoe_r'),
                          ('cam', 'camtop'), ('cam', 'ring'), ('ring', 'lens'), ('ring', 'silver'), ('silver', 'lens'), ('cam', 'finder'), ('cam', 'flash'), ('camtop', 'flash'), ('camtop', 'finder'),
                          ('sl_l', 'hand_l'), ('sl_r', 'hand_r'), ('cam', 'hand_l'), ('cam', 'hand_r'), ('pod_a', 'pod_b'), ('pod_a', 'mount'), ('pod_b', 'mount'),
                          ('pod_a', 'jacket'), ('pod_b', 'jacket'), ('pod_a', 'sl_r'), ('pod_b', 'sl_r'), ('pod_a', 'hair'), ('pod_a', 'face'), ('knob', 'pod_a'),
                          ('pod_a', 'knob'), ('pod_b', 'knob'), ('knob', 'pod_b'), ('knob', 'hair'), ('knob', 'face'), ('sl_l', 'cam'), ('sl_r', 'cam'),
                          ('jacket', 'cam'), ('jacket', 'camtop'), ('jacket', 'hand_l'), ('jacket', 'hand_r'), ('flash', 'face'), ('finder', 'face'),
                          ('jacket', 'flash'), ('jacket', 'finder'), ('jacket', 'ring'), ('sl_l', 'ring'), ('sl_r', 'ring'), ('sl_l', 'camtop'), ('sl_r', 'camtop'),
                          ('sl_l', 'jacket'), ('sl_r', 'jacket'), ('pocket_l', 'film_l'), ('pocket_r', 'film_r'), ('neck', 'cam'), ('neck', 'finder'), ('neck', 'flash'),
                          ('jacket', 'pod_a'), ('jacket', 'pod_b'), ('neck', 'strap_l'), ('neck', 'strap_r'), ('jacket', 'strap_l'), ('jacket', 'strap_r'), ('strap_l', 'cam'), ('strap_r', 'cam'), ('cam', 'strap_l'), ('cam', 'strap_r'), ('strap_l', 'camtop'), ('strap_r', 'camtop')})

    # ───────────── 덧그림
    # 재킷: 가운데 앞 여밈 · 단추 검정 둘 · 옷깃 · 주름 둘
    c.nudge(line_cells([(62, 106), (62, 110)]), -1, p_jacket)
    c.nudge(line_cells([(54, 88), (52, 108)]) + line_cells([(55, 88), (53, 108)]), -1, p_jacket)
    c.nudge(line_cells([(72, 90), (74, 108)]) + line_cells([(71, 90), (73, 108)]), -1, p_jacket)
    c.nudge(line_cells([(41, 84), (38, 106)]), 1, p_jacket)
    c.nudge(line_cells([(37, 90), (35, 98)]) + line_cells([(38, 90), (36, 98)]), 1, p_sll)
    c.nudge(line_cells([(44, 98), (45, 103)]) + line_cells([(45, 98), (46, 103)]), -1, p_sll)
    c.nudge(line_cells([(80, 98), (79, 103)]) + line_cells([(81, 98), (80, 103)]), -1, p_slr)
    c.nudge([(x, y) for y in (108, 109) for x in range(38, 87)], -1, p_jacket)
    # 필름 통: 노란 라벨 (둘레 한 줄) · 뚜껑
    c.set_lvl([(x, y) for y in range(103, 107) for x in range(47, 53)], 0, mat='label')
    c.set_lvl([(x, y) for y in range(103, 107) for x in range(71, 77)], 0, mat='label')
    c.nudge([(x, y) for y in (100, 101) for x in list(range(46, 53)) + list(range(70, 77))], 1, (p_fl, p_fr))
    # 얼굴: 턱 · 윗면 빛 · 한쪽 눈 찡그림 쪽 볼 그늘
    fm = c.pid == p_face
    jaw = fm & ~ndimage.binary_erosion(fm, iterations=2) & (yy > 66)
    c.nudge([(int(x), int(y)) for y, x in zip(*np.nonzero(jaw))], -1, p_face)
    c.lvl[fm & ellipse_mask(68, 61, 6.5, 5.5, S) & (c.lvl == 2)] = 3
    c.nudge(line_cells([(48, 60), (50, 68), (54, 72)]) + line_cells([(49, 60), (51, 68), (55, 72)]), -1, p_face)
    # 머리: 윗면 빛 한 덩어리 · 가르마 쪽 그늘
    c.set_lvl([(x, y) for y in range(38, 46) for x in range(58, 80) if c.pid[y, x] == p_hair and (x - 68) ** 2 / 70 + (y - 38) ** 2 / 16 < 1], 2, p_hair)
    c.nudge(line_cells([(52, 46), (50, 54), (50, 62)]) + line_cells([(53, 46), (51, 54), (51, 62)]), -1, p_hair)
    # 카메라: 윗판 위쪽 빛 · 몸통 아래 그늘 · 렌즈 반짝 (큰 한 덩어리) + 렌즈 속 작은 적 무리 · 렌즈 바깥 고리 홈
    c.nudge([(x, y) for y in (101, 102, 103) for x in range(47, 77)], -1, p_cam)
    c.nudge(line_cells([(48, 83), (76, 83)]), 1, p_ctop)
    gl = c.pid == p_lens
    c.lvl[gl & ellipse_mask(58, 91, 4.4, 3.6, S)] = 3
    c.lvl[gl & ellipse_mask(66, 99, 5.5, 4.2, S) & (c.lvl < 2)] = 0
    # 렌즈에 비친 작은 적 무리: 셋 (둥근 몸 · 보라색 눈 둘) — 3칸 넘는 덩어리
    for (ex, ey) in ((59, 96), (65, 93), (66, 99)):
        c.set_lvl([(x, y) for y in range(ey, ey + 3) for x in range(ex, ex + 4)], 1, p_lens)
    c.nudge(line_cells([(56, 79), (58, 79)]), 0, p_finder)
    c.nudge(line_cells([(70, 77), (73, 77)]), 1, p_flash)
    # 삼각대 다리 잠금쇠 둘 (밝은 띠) · 앞머리 그늘 덩어리 · 소매 단
    c.set_lvl([(x, y) for y in range(50, 55) for x in range(86, 93) if c.pid[y, x] == p_poda], 1, mat='camtop')
    c.set_lvl([(x, y) for y in range(60, 65) for x in range(92, 99) if c.pid[y, x] == p_podb], 1, mat='camtop')
    c.set_lvl([(x, y) for y in range(49, 55) for x in range(54, 66) if c.pid[y, x] == p_hair], 0, p_hair)
    c.set_lvl([(x, y) for y in range(92, 98) for x in range(33, 38) if c.pid[y, x] == p_sll], 3, p_sll)
    c.set_lvl([(x, y) for y in range(92, 98) for x in range(87, 92) if c.pid[y, x] == p_slr], 0, p_slr)
    # 작은 덩어리들 (3칸 이상): 카메라 나사 넷 · 뷰파인더 창 · 셔터 단추 · 재킷 단추 · 귀 둘 · 볼 홍조 둘 · 코 그늘
    for (sx, sy) in ((45, 85), (77, 85), (45, 101), (77, 101)):
        c.set_lvl([(x, y) for y in range(sy, sy + 3) for x in range(sx, sx + 3) if c.pid[y, x] == p_cam or c.pid[y, x] == p_ctop], 2, mat='camtop')
    c.set_lvl([(x, y) for y in range(79, 82) for x in range(53, 58)], 2, p_finder)
    c.set_lvl([(x, y) for y in range(78, 82) for x in range(62, 66) if c.pid[y, x] in (p_cam, p_ctop, -1) and False], 2)
    c.set_lvl([(x, y) for y in range(105, 109) for x in range(59, 64)], 0, p_jacket)
    c.set_lvl([(x, y) for y in range(60, 68) for x in range(44, 48) if c.pid[y, x] == p_face], 1, p_face)
    c.set_lvl([(x, y) for y in range(60, 68) for x in range(77, 81) if c.pid[y, x] == p_face], 3, p_face)
    c.set_lvl([(x, y) for y in range(64, 68) for x in range(51, 56) if c.pid[y, x] == p_face], 3, p_face)
    c.set_lvl([(x, y) for y in range(64, 68) for x in range(69, 74) if c.pid[y, x] == p_face], 3, p_face)
    c.set_lvl([(x, y) for y in range(63, 67) for x in range(61, 64) if c.pid[y, x] == p_face], 1, p_face)
    c.set_lvl([(x, y) for y in range(76, 80) for x in range(44, 52) if c.pid[y, x] == p_jacket], 0, p_jacket)
    c.set_lvl([(x, y) for y in range(76, 80) for x in range(72, 80) if c.pid[y, x] == p_jacket], 0, p_jacket)
    c.set_lvl([(x, y) for y in range(110, 118) for x in range(48, 52) if c.pid[y, x] == p_pants], 1, p_pants)
    c.set_lvl([(x, y) for y in range(110, 118) for x in range(72, 76) if c.pid[y, x] == p_pants], 1, p_pants)
    c.set_lvl([(x, y) for y in range(121, 124) for x in range(44, 52) if c.pid[y, x] == p_shl], 1, p_shl)
    c.set_lvl([(x, y) for y in range(121, 124) for x in range(70, 78) if c.pid[y, x] == p_shr], 1, p_shr)
    # 긴 줄 몇 개: 바지 안쪽 솔기 · 무릎 주름 · 머리 결 · 소매 주름 · 재킷 아랫단 줄
    c.nudge(line_cells([(54, 111), (54, 119)]) + line_cells([(55, 111), (55, 119)]), -1, p_pants)
    c.nudge(line_cells([(70, 111), (70, 119)]) + line_cells([(71, 111), (71, 119)]), 1, p_pants)
    c.nudge(line_cells([(60, 46), (56, 54)]) + line_cells([(61, 46), (57, 54)]), 1, p_hair)
    c.nudge(line_cells([(70, 44), (74, 52)]) + line_cells([(69, 44), (73, 52)]), -1, p_hair)
    c.nudge(line_cells([(33, 90), (36, 100)]), -1, p_sll)
    c.nudge(line_cells([(91, 90), (88, 100)]), -1, p_slr)
    c.nudge([(x, 112) for x in range(38, 87)], 1, p_jacket)
    c.nudge(line_cells([(48, 78), (44, 90)]) + line_cells([(49, 78), (45, 90)]), 1, p_jacket)
    c.nudge(line_cells([(75, 78), (80, 90)]) + line_cells([(74, 78), (79, 90)]), -1, p_jacket)
    c.nudge(line_cells([(52, 119), (52, 123)]) + line_cells([(53, 119), (53, 123)]), -1, (p_shl, p_shr))
    c.nudge(line_cells([(70, 119), (70, 123)]) + line_cells([(71, 119), (71, 123)]), -1, (p_shl, p_shr))
    c.set_lvl([(x, y) for y in range(100, 103) for x in range(66, 70) if c.pid[y, x] == p_lens], 3, p_lens)
    c.set_lvl([(x, y) for y in range(87, 89) for x in range(54, 60) if c.pid[y, x] == p_ring], 2, p_ring)
    c.set_lvl([(x, y) for y in range(100, 104) for x in range(55, 60) if c.pid[y, x] == p_ring], 2, p_ring)
    c.set_lvl([(x, y) for y in range(86, 90) for x in range(66, 72) if c.pid[y, x] == p_ring], 1, p_ring)
    c.nudge(line_cells([(57, 74), (60, 80)]) + line_cells([(58, 74), (61, 80)]), 1, p_strl)
    c.nudge(line_cells([(67, 74), (71, 80)]) + line_cells([(66, 74), (70, 80)]), 1, p_strr)
    c.nudge(line_cells([(40, 82), (46, 80)]) + line_cells([(40, 83), (46, 81)]), 1, p_sll)
    c.nudge(line_cells([(82, 80), (88, 84)]) + line_cells([(82, 81), (88, 85)]), 1, p_slr)
    c.set_lvl([(x, y) for y in range(70, 74) for x in range(56, 68) if c.pid[y, x] == p_neck], 0, p_neck)
    c.set_lvl([(x, y) for y in range(100, 104) for x in range(84, 88) if c.pid[y, x] == p_slr], 3, p_slr)
    c.set_lvl([(x, y) for y in range(112, 118) for x in range(40, 44) if c.pid[y, x] == p_pants], 1, p_pants)
    c.set_lvl([(x, y) for y in range(90, 94) for x in range(46, 50) if c.pid[y, x] == p_cam], 2, p_cam)
    c.set_lvl([(x, y) for y in range(90, 94) for x in range(75, 79) if c.pid[y, x] == p_cam], 0, p_cam)
    c.set_lvl([(x, y) for y in range(45, 49) for x in range(46, 52) if c.pid[y, x] == p_hair], 2, p_hair)
    c.set_lvl([(x, y) for y in range(118, 121) for x in range(46, 52) if c.pid[y, x] == p_pants], 0, p_pants)
    c.set_lvl([(x, y) for y in range(118, 121) for x in range(72, 78) if c.pid[y, x] == p_pants], 0, p_pants)
    c.set_lvl([(x, y) for y in range(108, 112) for x in range(44, 50) if c.pid[y, x] == p_pkl], 3, p_pkl)
    c.set_lvl([(x, y) for y in range(108, 112) for x in range(74, 80) if c.pid[y, x] == p_pkr], 0, p_pkr)
    # 삼각대: 막대 이음 · 빛
    c.nudge(line_cells([(94, 40), (84, 80)]), 1, p_poda)
    c.nudge(line_cells([(100, 44), (89, 84)]), -1, p_podb)
    # 눈썹 · 입 (한쪽 눈 찡그림 = 왼쪽이 얇은 곡선 · 웃는 입)
    BR, MO = hx('4C3C28'), hx('9A4A44')
    c.stamp(['aaaaaa', '.aaaaa'], 49, 55, {'a': BR}, part=p_face)
    c.stamp(['aaaaaa', 'aaaaa.'], 65, 54, {'a': BR}, part=p_face)
    c.stamp(['a.........a', '.aaaaaaaaa.'], 57, 68, {'a': MO}, part=p_face)

    # 림 라이트: 머리 · 삼각대 · 오른 어깨
    c.rim((p_hair, p_poda, p_podb, p_slr, p_flash), RIM_DIR, width=1, level=-1, thresh=0.35)
    c.absorb((c.pid >= 0), min_size=3)

    # 눈: 오른쪽 크게 뜸 · 왼쪽은 찡그려 얇게 (가는 선 · 눈썹 아래)
    c.stamp(['aab', 'aaa', 'aaa', '.a.'], 66, 60, {'a': EYE, 'b': EYE_HOT})
    c.stamp(['.aaaa.', 'aaaaaa'], 49, 62, {'a': EYE})
    dots = [(68, 60)]
    c.outline(warm_parts=(p_hair,))
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
