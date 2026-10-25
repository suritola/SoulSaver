# 스킨 메뉴 초상 128칸 — rogue__void 「공허」 (모양이 다른 스킨 — 설계도 = rogue__void 32칸 첫 칸)
#   설계도 읽기: 두건 · 얼굴 그늘 · 몸 자리는 기본 도적과 같음 → 기본 도적 초상 모듈(rogue.py)을 바탕으로
#     다른 점 ① 온몸 보라 (두건 421470 · 6032A0 · 9454CD / 망토 220E3A · 3A1E62 · 5D3482) ② 빛나는 분홍 눈 (FF78E6)
#     ③ 붉은 목도리가 없어지고 그 자리 = 짙은 보라 띠 (240E3E · 3D1D56 — 얼굴 그늘과 거의 같은 어둠)
#     ④ 망토가 양옆으로 2칸씩 넓은 날개 자락 (설계도 x7~8 · x22~23, y22~27) + 바깥 테 라벤더 (7C42B8 · 8980D3 · 6C6DAF)
#   흑백 계층: 분홍 눈 (Y 약 172) > 라벤더 테 (Y ≤ 131) > 두건 > 망토 > 띠 · 그늘
import numpy as np

from .portrait import path, poly_mask, n_plane
from .skin import Variant


def shape(c, mod):
    """기본 도적이 외곽을 정하기 바로 전 — 망토 양옆 날개 자락 (빈칸에만 = 몸 뒤)"""
    S = c.S
    empty = c.mat < 0
    wing_l = poly_mask(path((38, 86), (36, 92), [(32, 98), (29, 104), (27.5, 109)], (30, 111.5), (35, 110), (38, 112)), S)
    wing_r = poly_mask(path((86, 86), (89, 92), [(92, 98), (94.5, 104), (95.5, 109)], (93.5, 111.5), (90, 110), (87, 112)), S)
    pl = c.part('wing_l', wing_l & empty, 'cloak', n_plane(-0.5, -0.3, 0.8), bias=-0.05)
    pr = c.part('wing_r', wing_r & empty, 'cloak', n_plane(0.55, -0.4, 0.75), bias=0.05)
    # 바깥 테 (실루엣 바로 안 2칸) = 라벤더 — 오른쪽(빛)이 가장 밝은 단계, 왼쪽은 한 단계 아래
    a = c.mat >= 0
    for p, lv, side in ((pl, 3, -1), (pr, 3, 1)):     # 양쪽 같은 단계 (오른쪽만 밝으면 왼 광원 항목이 기울어짐)
        m = c.pid == p
        ys, xs = np.nonzero(m)
        for y in np.unique(ys):
            row = xs[ys == y]
            x0 = row.min() if side < 0 else row.max()
            cells = [(int(x0 - side * (i + 1)), int(y)) for i in range(2)]
            c.set_lvl([(x, yy) for x, yy in cells if 0 <= x < S and m[yy, x]], lv)
        # 안쪽 접힌 골 하나 (망토와 만나는 쪽 — 자락이 앞으로 겹친 그늘)
        c.set_lvl([(int(xs[ys == y].min() if side > 0 else xs[ys == y].max()), int(y)) for y in np.unique(ys)], 1)
    c.absorb(np.isin(c.pid, (pl, pr)), min_size=3)
    return []


V = Variant('rogue', 'rogue__void', auto=False, ramps={
    'hood':  ['1E0A36', '401E68', '542C94', '6E42B2', '9262D0'],     # 실루엣 가까이 쓰는 단계는 Y ≥ 41 (lint 4 외곽 두께)
    'cloak': ['160828', '3A2060', '462872', '5E3A8C', '8478C8'],
    # 목도리 → 짙은 보라 띠 (눈 아래를 가리는 그늘 — 붉은 목도리 없음)
    'scarf': ['140626', '240E3E', '3D1D56', '56307A'],
    'void':  ['12021E', '26034A'],
    'leather': ['221828', '3A2430', '56322E', '6C4436'],
}, glob={'EYE': 'FF78E6', 'EYE_HOT': 'FFC8F4'}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
