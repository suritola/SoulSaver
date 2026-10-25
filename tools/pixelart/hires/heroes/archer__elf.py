# 스킨 메뉴 초상 128칸 — archer__elf 「엘프」 (모양이 다른 스킨 — 설계도 = archer__elf 32칸 첫 칸)
#   설계도 읽기: 두건 · 몸 · 화살통 자리는 기본 궁수와 같음 → 기본 궁수 초상 모듈(archer.py)을 바탕으로
#     다른 점 ① 두건 → 짙은 청록 (0C5746 · 227C5C · 3DA173 · 51A49A) ② 이마에 금 머리띠 (설계도 x14~20 · y18 — B46826 · EAAA3A)
#     ③ 흰 깃털 → 박하 잎 (17393E · 82ECAA) ④ 갈색 조끼 · 바지 → 숲 초록 (192C32 · 2E604A · 467F60 / 194336)
#     ⑤ 화살통은 갈색 그대로 (5B3629 · 5E3228 — 조끼 재질을 쓰던 부위라 따로 칠함)
#   흑백 계층: 얼굴 (Y 199) > 머리띠 빛 (Y 약 175 · 1줄) > 깃털 > 두건 > 조끼 > 바지
import numpy as np

from .portrait import line_cells
from .skin import Variant


def shape(c, mod):
    S = c.S
    hood = [i for i, p in enumerate(c.parts) if p['name'] == 'hood']
    face = [i for i, p in enumerate(c.parts) if p['name'] == 'face']
    hm = np.isin(c.pid, hood)
    # 금 머리띠: 얼굴 테 바로 위 두건 위를 가로지르는 4칸 띠 (살짝 아래로 휨) — 위 1줄 빛 · 가운데 바탕 · 아래 1줄 그늘
    for x in range(47, 83):
        sag = int(round(((x - 65) / 18.0) ** 2 * -1.5))          # 양끝이 1~2칸 위로 (이마 둘레를 감음)
        top = 67 + sag
        for k, lv in ((0, 2), (1, 1), (2, 1), (3, 0)):
            y = top + k
            if 0 <= y < S and (hm[y, x] or (k == 3 and np.isin(c.pid[y, x], face))):
                c.set_lvl([(x, y)], lv, mat='gold')
    # 머리띠 가운데 작은 잎 장식 (3×3 · 박하빛 — 엘프 표시)
    c.stamp(['..bb', '.bab', 'bab.', 'bb..'], 63, 66, {'a': ('gold', 2), 'b': ('gold', 1)})     # 비스듬한 잎 (판정 2회차 권장: 점 → 잎 모양)
    # 깃털 (판정 2회차: 한 색 판 69칸) — 가운데 잎맥 빛 1줄 · 아래 가장자리 그늘 1줄
    fe = [i for i, p in enumerate(c.parts) if p['name'] == 'feather']
    vein = line_cells([(61, 57), (55, 52.5), (48.5, 47.5)])
    c.set_lvl(vein + [(x + 1, y) for x, y in vein], 4, part=fe)                 # 4-연결로 이어진 2칸 굵기 (1칸 계단 조각 방지)
    c.set_lvl([(x, y + 1) for x, y in vein] + [(x + 1, y + 1) for x, y in vein], 2, part=fe)
    c.absorb(np.isin(c.pid, fe), min_size=3)
    return []


V = Variant('archer', 'archer__elf', auto=False, ramps={
    'hood':  ['062A26', '0C5746', '227C5C', '3DA173', '6CC09A'],
    'vest':  ['192C32', '2E604A', '467F60', '62A07C'],
    'pants': ['141E24', '194336', '2E604A', '3E7058'],
}, parts={
    'feather': ['0C5746', '227C5C', '3DA173', '62C690', '96E0B4'],     # 어두운 세 단계는 두건과 같은 색
    'quiver':  ['1E1426', '3C2028', '563432', '966038'],     # 기본 끈 · 허리띠 갈색과 같은 단계 (색 수 절약)
}, add={
    'gold': dict(ramp=['563432', 'B46826', 'EAAA3A'], cuts=[0.4, 0.7], warm=True),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
