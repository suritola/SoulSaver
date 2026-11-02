# 스킨 메뉴 초상 128칸 — foreman__beta 「베타 테스터」 (설계도: 창틀 안전모(파란 제목줄 · 빨간 X · BETA 띠) · 회색 조끼 · 가슴 B 딱지 · 양옆 깨진 화소)
import numpy as np

from .portrait import line_cells
from .skinops import conv, box, cells, block_glyph
from .skinshape import Variant2 as Variant
from .skinshape import rect


def shape(c, mod):
    S = c.S
    yy, xx = c.yy, c.xx
    # 안전모 창틀: 제목줄(파랑) + 오른쪽 닫기 버튼(빨강 · 흰 X)
    title = box(c, 0, 44, S, 53)
    conv(c, title, 'titlebar', parts=['dome'], lmap=[0, 0, 1, 1])
    close = box(c, 70, 46, 78, 52)
    conv(c, close, 'redx', parts=['dome'], lmap=[0])
    for (x, y) in line_cells([(72, 47), (76, 51)]) + line_cells([(76, 47), (72, 51)]):
        if 0 <= y < S and 0 <= x < S and c.mat[y, x] >= 0:
            c.mat[y, x] = c.mi('hat'); c.lvl[y, x] = 3; c.fixed[y, x] = True
    # 챙의 BETA 띠: 빨강 바탕에 흰 글자 덩어리
    band = box(c, 38, 58, 90, 62)
    conv(c, band, 'redx', parts=['brim'], lmap=[0])
    for x0 in range(44, 84, 8):
        c.set_lvl(cells(x0, 59, x0 + 4, 62), 3, mat='hat')
    for x0 in (54, 62):                                                   # 창 조절 버튼 둘 (최소화 · 최대화)
        c.set_lvl(cells(x0, 47, x0 + 6, 52), 3, mat='hat')
        c.set_lvl(cells(x0 + 2, 49, x0 + 4, 51), 0, mat='titlebar')
    conv(c, box(c, 28, 76, 46, 80), 'titlebar', parts=['upper_l'], lmap=[0, 1, 1])
    conv(c, box(c, 86, 82, 98, 86), 'titlebar', parts=['arm_r'], lmap=[0, 1, 1])
    # 가슴 딱지: 어두운 바탕 + 흰 B (4칸 블록 글자)
    rect(c, 'patch', 13.4, 19.6, 18.6, 25.6, 'stripe', behind=False)
    c.set_lvl(block_glyph(['##.', '#.#', '##.', '#.#', '##.'], 58, 79), 3, mat='hat')
    # 깨진 화소: 양옆 번짐 (시안 · 자홍)
    rect(c, 'shardL', 5.2, 22.4, 7.6, 24.0, 'cyan', behind=False)
    rect(c, 'shardR', 24.4, 21.6, 26.8, 23.2, 'magenta', behind=False)
    return []


V = Variant('foreman', 'foreman__beta', warm=('shardL', 'shardR'), auto=False, ramps={
    'hat': ['7C8298', 'A0A6B6', 'C8CCD6', 'F4F6FA'],
    'vest': ['8A98B8', '8A98B8', 'B8C4DC'],
    'stripe': ['11142A', '11142A', '344070'],
    'sleeve': ['344070', '4A5A90', '4A5A90'],
    'pants': ['2E3858', '3C466A', '3C466A'],
    'boot': ['3A2410', '5C3A26', '5C3A26', '925C36'],
}, add={
    'titlebar': dict(ramp=['2A5ED8', '4A80F0'], cuts=[0.7]),
    'redx': dict(ramp=['E02830'], cuts=[]),
    'cyan': dict(ramp=['40F0FF'], cuts=[]),
    'magenta': dict(ramp=['FF40D0'], cuts=[]),
}, shape=shape)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
