# 스킨 메뉴 초상 128칸 — rogue__ninja 「닌자」 (모양이 다른 스킨 — 설계도 = rogue__ninja 32칸 첫 칸)
#   설계도 읽기: 두건 · 망토 자리는 기본 도적과 같음 → 기본 도적 초상 모듈(rogue.py)을 바탕으로
#     다른 점 ① 온몸 남색 (두건 12154D · 28346E · 414690 · 림 5577A5 / 망토 151234 · 222A54 · 373A71 · 림 517195)
#     ② 얼굴 그늘이 아니라 눈 둘레 살갗 띠 1줄 (설계도 y19 F2BC96) + 검은 눈 (120B20) — 빛나는 눈 없음
#     ③ 그 아래 입 가리개 = 남색 복면 (목도리 자리) · 붉은 천은 왼쪽 매듭 · 꼬리만 (설계도 x6~10, y21~22)
#     ④ 가슴에 붉은 띠 (설계도 x13~16 · y23 C4283E)
#   흑백 계층: 살갗 띠 (Y 약 199) > 붉은 매듭 · 띠 > 두건 림 > 두건 > 망토
import numpy as np

from .portrait import line_cells
from .skin import Variant


def shape(c, mod):
    S = c.S
    face = [i for i, p in enumerate(c.parts) if p['name'] == 'face']
    fm = np.isin(c.pid, face) & (c.mat >= 0)
    # 눈 둘레 살갗 띠: 설계도 y19 → 128 y 71~78 (위 1줄은 두건 그늘 = 살 어두운 단계 · 아래 1줄 = 복면 그늘)
    for y in range(68, 80):
        cells = [(int(x), y) for x in np.nonzero(fm[y])[0]]
        if not cells:
            continue
        lv = 0 if y <= 70 else (1 if y in (71, 77) else 2)
        if y >= 78:
            c.set_lvl(cells, 1, mat='hood')
        else:
            c.set_lvl(cells, lv, mat='skin')
    # 가슴 붉은 띠 (설계도 y23 x13~16 → 128 y 89~92 · x 52~67) — 망토 · 안쪽 · 어깨끈 위에
    body = [i for i, p in enumerate(c.parts) if p['name'] in ('inner', 'strap')]     # 망토 앞섶 사이로 보이는 만큼만
    for y, lv in ((89, 3), (90, 2), (91, 2), (92, 1)):
        x0, x1 = 51 + (92 - y) // 2, 68 - (y - 89) // 2
        cells = [(x, y) for x in range(x0, x1 + 1) if c.pid[y, x] in body]
        c.set_lvl(cells, lv, mat='scarf')
    # 띠 양끝: 앞섶 밑으로 2~3칸 이어 들어가며 주름 그늘 (떠 있는 막대가 아니라 몸을 감은 띠로)
    cloak = [i for i, p in enumerate(c.parts) if p['name'] == 'cloak']
    for y in range(89, 93):
        row = [x for x in range(46, 76) if c.pid[y, x] in body and c.mat[y, x] == c.mi('scarf')]
        if not row:
            continue
        for x in (row[0] - 1, row[0] - 2, row[-1] + 1, row[-1] + 2):
            if c.pid[y, x] in cloak:
                c.set_lvl([(x, y)], 0 if y in (89, 92) else 1, mat='scarf')
    return []


V = Variant('rogue', 'rogue__ninja', auto=False, ramps={
    'hood':  ['0E1036', '283268', '2E3C76', '40508E', '5070A0'],     # 실루엣 가까이 쓰는 단계는 Y ≥ 41 (lint 4 외곽 두께)
    'cloak': ['0C0C26', '262E5C', '2C3866', '3E4C78', '4E6A8A'],
    'steel': ['4E5A6E', '6E7E96', 'B8C4D2'],     # 칼날 등 빛 Y 194 (빛나는 눈이 없어 살갗 띠가 1위)
}, parts={
    # 목도리 → 남색 복면 (입을 가림 — 붉은 천은 매듭 · 꼬리 · 가슴 띠만)
    'scarf': ['0E1036', '16184A', '222C5C', '34407A'],
}, glob={'EYE': '120B20', 'EYE_HOT': 'F4F0E6'}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
