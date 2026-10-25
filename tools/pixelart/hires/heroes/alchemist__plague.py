# 스킨 메뉴 초상 128칸 — alchemist__plague 「역병 의사」 (모양이 다른 스킨 — 설계도 = alchemist__plague 32칸 첫 칸)
#   설계도 읽기 (줄 = 설계도 y): 13~15 위가 납작한 검은 모자 (x12~19) · 16 넓은 챙 (x9~22) · 17 챙 밑 짙은 그늘
#     18~20 상아 부리 가면 (F4EAD4 · DAC8B0) + 파란 유리 눈 둘 (x14 · x18 — 4280E8 · 반짝 8CC4FF) · 21~23 가슴까지 내려온 부리 (x14~17)
#     22~26 짙은 검회 외투 (201829 · 2E283A · 453B51 · 오른 테 596F85 · 50667A) · 25 양손 · 26 허리띠 · 27~31 바지 · 장화 = 기본과 같음
#   → 기본 연금술사 초상(alchemist.py)의 몸 · 손 · 허리띠 · 바지 · 장화는 그대로 쓰고 (외투는 색만), 머리(흰 머리 · 고글 · 얼굴 · 깃)를 지우고
#     모자 · 챙 · 가면 · 부리 · 유리 눈을 새로 그림 (외곽을 정하기 전 — 실루엣이 모자 모양으로 바뀜)
#   흑백 계층: 가면 (Y 약 205 — 이 스킨의 「얼굴」) > 유리 눈 반짝 > 부리 그늘 > 외투 테 > 모자 > 외투
#   (물약은 기본의 초록 물약 그대로 — 설계도에 물약 칸이 외투에 가려 없음 → 외투 색으로)
import numpy as np
from scipy import ndimage

from .portrait import path, poly_mask, ellipse_mask, n_plane, n_ellipsoid, n_cyl_v, line_cells
from .skin import Variant


def shape(c, mod):
    S = c.S
    # 1 머리 지우기 (흰 머리 · 고글 띠 · 고글 알 · 얼굴 · 깃) — 그 칸의 낱색(눈)도
    gone = [i for i, p in enumerate(c.parts) if p['name'] in ('hair', 'band', 'lens_l', 'lens_r', 'face', 'collar')]
    m = np.isin(c.pid, gone)
    c.mat[m] = -1
    c.pid[m] = -1
    c.fixed[m] = False
    for key in [k for k in c.color if m[k]]:
        del c.color[key]

    # 2 목 그늘 (가면 · 부리 뒤 외투 깃 — 짙은 외투)
    neck = poly_mask(path((44, 80), (84, 80), (84, 90), (44, 90)), S)
    c.part('neck', neck & (c.mat < 0), 'robe', n_plane(0.0, -0.4, 0.9), bias=-0.5, line=False)

    # 3 가면 (설계도 x11~20 · y18~20 — 위가 넓고 아래가 좁은 상아 가면 · 오른쪽이 빛)
    mask = poly_mask(path((44, 66), (84, 66), (84, 74), [(83, 78), (80, 81), (76, 82)], (52, 82), [(48, 81), (45, 78), (44, 74)]), S)
    p_mask = c.part('mask', mask, 'mask', n_cyl_v(66, 24, tilt=0.1), bias=0.26)

    # 4 부리 (가면 아래 가운데에서 가슴까지 · 끝이 아래 · 설계도 x14~17 y21~23)
    beak = poly_mask(path((53, 79), (75, 79), [(74, 84), (71, 89), (66.5, 94)], (64, 96.5), [(61, 94), (57, 89), (54, 84)]), S)
    p_beak = c.part('beak', beak, 'mask', n_ellipsoid(65, 78, 13, 16), bias=0.32)     # 빛 중심을 위로 — 부리 끝으로 갈수록 그늘 (위 광원)
    # 부리 가운데 등성이 (빛 받는 1~2칸 띠) · 왼 아랫면 그늘 · 콧구멍 자리 짙은 줄
    c.nudge(line_cells([(66, 82), (66, 92)]) + line_cells([(67, 82), (67, 90)]), 1, p_beak)
    c.nudge([(x, y) for x, y in line_cells([(56, 82), (60, 89), (62.5, 94)]) if c.lvl[y, x] <= 1], 1, p_beak)   # 그늘 쪽 가장자리 반사광 1단 (외투 빛)
    c.nudge(line_cells([(57, 80), (62, 80)]) + line_cells([(68, 80), (72, 80)]), -1, p_beak)   # 가면과 부리가 꺾이는 골 (끊긴 두 마디 · 옅게)

    # 5 유리 눈 (설계도 x14 · x18 · y18~19 → 가운데 (57.5, 72) · (73.5, 72)) — 짙은 테 + 파란 알 + 반짝
    dots = []
    for cx in (57.5, 73.5):
        e = ellipse_mask(cx, 72, 3.8, 3.5, S)
        ring = ndimage.binary_dilation(e) & ~e & (c.pid == p_mask)
        c.set_lvl([(int(x), int(y)) for y, x in zip(*np.nonzero(ring))], 0, p_mask)
        pe = c.part('eye', e, 'lens', n_ellipsoid(cx + 1.5, 69, 6, 5), bias=0.0, line=False)
        c.set_lvl([(int(cx) + 1, 70), (int(cx) + 2, 70), (int(cx) + 1, 71)], 2, pe)       # 반짝 (3칸 덩어리)
    # 가면 아래 테 그늘 · 눈 사이 콧등 빛
    c.nudge(line_cells([(48, 80), (60, 81)]), -1, p_mask)
    c.nudge(line_cells([(65, 68), (65, 76)]) + line_cells([(66, 68), (66, 76)]), 1, p_mask)

    # 6 모자 (설계도 x12~19 · y13~15 위가 납작) · 챙 (y16 · x9~22) · 챙 밑 그늘 (y17)
    crown = poly_mask(path((47, 49.5), [(55, 48.6), (73, 48.6), (80, 49.5)], (82, 52), (83, 62), (45, 62), (46, 52)), S)
    p_crown = c.part('crown', crown, 'hat', n_cyl_v(66, 20, tilt=0.25), bias=0.0)
    brim = poly_mask(path((38, 61.5), [(47, 59.5), (82, 59.5), (90, 61.5)], (91, 63.5), [(87, 65.5), (76, 66.5), (52, 66.5)], (41, 65.5), (37, 63.5)), S)     # 챙 폭 = 설계도 x9~22 (14칸 × 4)
    p_brim = c.part('brim', brim, 'hat', n_plane(0.15, -0.9, 0.4), bias=0.05)
    shade = poly_mask(path((45, 66), (84, 66), (84, 68.5), (45, 68.5)), S)
    c.shift(shade & (c.pid == p_mask), -1)                                    # 챙이 드리운 그늘 (가면 위 2칸)
    # 모자 띠 (짙은 한 단계 · 모자 밑동) · 모자 정수리 위 테 빛
    c.set_lvl([(x, y) for y in (58, 59) for x in range(46, 83)], 1, p_crown)
    c.nudge(line_cells([(50, 50), (78, 50)]), 1, p_crown)
    # 챙 윗면 앞쪽 테 빛 (오른쪽만 · 끊긴 두 마디)
    c.nudge(line_cells([(70, 61), (86, 61.5)]), 1, p_brim)
    c.nudge(line_cells([(52, 61), (62, 61)]), 1, p_brim)
    # 새 부위 둘레만 경계선 (기본 그림의 경계선 · 건너뛴 쌍은 그대로 — 전체를 다시 그으면 몸통에 선 조각이 생김)
    before = c.lvl.copy()
    lines = c.contact_lines(skip={('crown', 'brim'), ('mask', 'beak'), ('mask', 'eye'), ('neck', 'mask'), ('neck', 'beak'),
                                ('torso', 'beak'), ('arm_l', 'beak'), ('arm_r', 'beak')})     # 부리 끝 둘레는 선 대신 부리 그늘 단계로 (1칸 계단 조각 방지)
    new = np.isin(c.pid, [i for i, p in enumerate(c.parts) if p['name'] in ('neck', 'mask', 'beak', 'eye', 'crown', 'brim')])
    near = ndimage.binary_dilation(new)
    c.lvl[lines & ~near] = before[lines & ~near]

    # 7 외투 림 · 조각 흡수
    c.rim((p_crown, p_brim), (0.4, -0.92), width=1, by=1, thresh=0.4)
    c.absorb(np.isin(c.pid, (p_mask, p_beak, p_crown, p_brim)), min_size=3)
    return dots


V = Variant('alchemist', 'alchemist__plague', auto=False, ramps={
    # 외투: 보라 → 짙은 검회 (32칸 1D152A · 201829 · 2E283A · 453B51 · 오른 테 596F85)
    'robe': ['141020', '1D152A', '2E283A', '453B51', '5E5678'],     # 빛 단계도 같은 검보라 색상 (테를 파랗게 하면 위 빛이 다른 색 묶음으로 갈라짐)
    # 유리 눈: 파랑 (4280E8 · 반짝 8CC4FF)
    'lens': ['1E3C78', '4280E8', '8CC4FF'],
}, parts={
    # 물약: 초록 → 유리 눈과 같은 파랑 (설계도엔 외투에 가려 없음 — 연금술사 물약은 남기되 이 스킨의 색으로 · 색 수 절약)
    'potion': ['141020', '1E3C78', '4280E8'],     # 외투 그늘 속이라 한 단계 어둡게 (빛 1위는 가면 · 유리 눈)
}, add={
    # 가면 · 부리: 상아 (52424F 그늘 테 · DAC8B0 · F4EAD4) — 최고 단계 Y 약 222 (이 스킨에서 가장 밝은 곳 = 얼굴 자리)
    'mask': dict(ramp=['52424F', '8E8072', 'C4B49E', 'DCCDB4', 'EEE2CA'], cuts=[0.30, 0.55, 0.85, 1.05], warm=True),
    # 모자: 무채 검정 (32칸 140E1B · 201829 · 테 50667A — 외투 검보라와 색상을 갈라 모자 · 외투가 한 덩어리로 안 읽히게)
    'hat':  dict(ramp=['1A1A1C', '2A292D', '3A393E', '5C6066'], cuts=[0.40, 0.75, 9.0]),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
