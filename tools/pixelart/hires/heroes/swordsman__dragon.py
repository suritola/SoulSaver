# 스킨 메뉴 초상 128칸 — swordsman__dragon 「용 기사」 (모양이 다른 스킨 — 설계도 = swordsman__dragon 32칸 첫 칸)
#   설계도 읽기: 투구 · 갑옷 · 망토 자리는 기본 검사와 같음 → 기본 검사 초상 모듈(swordsman.py)을 바탕으로
#     다른 점 ① 투구 양옆 위로 뻗은 상아 뿔 둘 (설계도 왼 x8~11 · 오른 x20~23, y13~16 — 밑동 짙은 52424F · 끝 F4EAD4)
#     ② 은 투구 · 어깨받이 → 붉은 용 비늘 (460F24 · 98282C · C45148 · FB9C88) ③ 붉은 깃털 → 주황 불꽃 볏 (F8842A · FFC856)
#     ④ 파란 갑옷 → 붉은 갑옷 (541330 · C4283E · EE5C56) ⑤ 망토 → 짙은 검붉음 (370D1E · 701C1E · 933C34) ⑥ 바지 → 검붉은 (310F23 · 602028)
#   흑백 계층: 얼굴 (Y 199) > 뿔 끝 (Y ≤ 197) > 볏 > 갑옷 빛 > 투구 > 망토 — 뿔 · 볏 최고 단계를 얼굴 아래로 누름
import numpy as np

from .portrait import path, poly_mask, n_plane, line_cells
from .skin import Variant


def shape(c, mod):
    S = c.S
    empty = c.mat < 0
    # 뿔: 투구 위 옆에서 바깥 · 위로 휘어 뻗음 (밑동 굵기 6칸 → 끝 1~2칸) — 빈칸에만 (투구 뒤에서 나옴)
    horn_l = poly_mask(path((41, 65), [(37, 61), (33.5, 56), (31, 51)], (29.5, 46.5), (31.5, 47),
                            [(35, 51.5), (39, 55.5), (44, 59)]), S)     # 깃털 장식 왼쪽 밖으로 (설계도 x8~11)
    horn_r = poly_mask(path((77, 58), [(82, 55), (86.5, 51), (89.5, 47)], (91, 45), (91.5, 47.5),
                            [(90, 52), (86.5, 57.5), (82, 62)]), S)
    pl = c.part('horn_l', horn_l & empty, 'horn', n_plane(0.2, -0.8, 0.55), bias=-0.02)
    pr = c.part('horn_r', horn_r & empty, 'horn', n_plane(0.5, -0.7, 0.5), bias=0.08)
    # 뿔 명암 (판정 2회차: 한 색 판 → 단계 3~4): 뿔 축을 따라 밑동 u 0 → 끝 u 1, 축에 수직인 s = 윗면(+) · 아랫면(−)
    #   밑동 (u < 0.09) = 짙은 뿌리 (설계도 h 52424F) · 아랫면 · 바깥 = 그늘 · 가운데 = 바탕 · 윗 모서리 + 끝 (u > 0.8) = 상아 빛 (설계도 i)
    for p, base, tip, up in ((pl, (41.5, 63.5), (30, 46.5), (17, -11)), (pr, (79.5, 60), (91.2, 45.8), (-14.5, -11.5))):
        ys, xs = np.nonzero(c.pid == p)
        bx, by = base; dx, dy = tip[0] - bx, tip[1] - by
        L2 = dx * dx + dy * dy
        nx, ny = up[0] / np.hypot(*up), up[1] / np.hypot(*up)
        for x, y in zip(xs, ys):
            px, py = x + 0.5 - bx, y + 0.5 - by
            u = (px * dx + py * dy) / L2
            s = px * nx + py * ny
            half = 3.2 * (1 - u) + 0.6                     # 축에서 가장자리까지 대략 반폭 (끝으로 갈수록 가늘게)
            t = s / half
            lv = 0 if u < 0.09 else (3 if (u > 0.8 or t > 0.45) else (1 if t < -0.3 else 2))
            c.set_lvl([(int(x), int(y))], lv)
    c.absorb(np.isin(c.pid, (pl, pr)), min_size=3)
    return []


V = Variant('swordsman', 'swordsman__dragon', auto=False, ramps={
    'red':   ['300818', '370D1E', '4E0913', '701C1E', '933C34'],
    'steel': ['300818', '982A4A', 'B6364A', 'D05462', 'E27E88'],     # 투구 = 갑옷과 같은 진홍 색상 (살갗 주황과 갈라 위 광원이 읽히게)
    'armor': ['300818', '942838', 'A82A38', 'B6364A', 'D05462'],     # 판정 2회차: 투구보다 한 단계 어둡게 (빛 두 단계는 투구 색을 나눠 씀 · 색 수)
    'cloth': ['370D1E', '4E0913', '701C1E', '933C34'],     # 바지 = 망토와 같은 검붉은 단계 (색 수 절약)
}, parts={
    'plume': ['701C1E', 'A83A1C', 'DE6A24', 'F8922E', 'FCBE52'],
}, add={
    'horn': dict(ramp=['52424F', '8A7C78', 'B8AA96', 'D2C6B0'], cuts=[0.35, 0.62, 0.98], warm=True),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
