# 스킨 도트 3 (2.1.0~): 새 영웅 둘(씨름꾼 · 공사 반장)의 캐릭터 스킨 8 · 무기 스킨 4
#   캐릭터: Assets/Resources/Characters/<영웅>__<스킨>.png — 기본 시트(192x32 = 6프레임)를 읽어 색 바꾸기 + 덧그림
#   무기:   Assets/Resources/Weapons/weapon_<무기>__<스킨>.png — 기본 무기와 같은 크기 · 같은 잡는 자리(메타 복사)
#   덧그림 좌표: x = 칸 안 가로 자리 · y = 「그 프레임 머리 꼭대기 줄」 기준 (rel) → 몸이 위아래로 흔들려도 같이 따라감
#   make_skins2.py 의 도구(F · hx · sheet_meta …)를 그대로 씀. 실행: make_skins.py 가 부름 · 이 파일만 돌려도 됨
import math, os, re, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from make_skins2 import (F, hx, luma, retry, write_png, old_guid, sheet_meta, frames_of, ascii_img, recolor_img,
                         CHARS, WEAP)

OUTL = (0x1A, 0x11, 0x2C)
SKIN_W = {(0xCE, 0x84, 0x5E), (0x96, 0x54, 0x48), (0xF0, 0xB0, 0x80), (0xAA, 0x42, 0x46)}       # 씨름꾼 살빛 (바탕 · 그늘 · 빛 · 입)


def rgb(p):
    return tuple(int(v) for v in p[:3])


def col_top(f, x, from_rel=-4):
    """x 줄에서 from_rel 아래로 처음 나오는 불투명 화소의 rel (없으면 None)"""
    for rel in range(from_rel, 22):
        if f.px(x, rel)[3]:
            return rel
    return None


def torso_skin(f, fn, x0=11, x1=20, r0=7, r1=12, skip=()):
    """씨름꾼 몸통 칸(살빛 화소)마다 fn(x, rel, 원래 색) → 새 색 (없으면 그대로)"""
    for rel in range(r0, r1 + 1):
        for x in range(x0, x1 + 1):
            if (x, rel) in skip:
                continue
            p = f.px(x, rel)
            if p[3] and rgb(p) in SKIN_W:
                c = fn(x, rel, rgb(p))
                if c is not None:
                    f.put(x, rel, c, mark=False)


# ------------------------------------------------------------------ 씨름꾼 (머리 꼭대기 y11~12)
#   머리카락 rel 0~2 · 얼굴 rel 3~6 · 어깨 rel 7 · 가슴 rel 8~12 · 샅바 rel 13~15 · 다리 rel 16~19
#   살 a CE845E c 965448 d F0B080 · 머리 g 342648 j 604C7C · 샅바 e BE2836 f 5A6BFF m F2504E n A0AEFF · 눈 k 120B20 · 눈썹 i 462628
def wr_local(f):               # 동네 장사 (일반): 흰 러닝셔츠 · 운동회 머리띠(흰 + 빨강)
    scoop = {(14, 8), (15, 8), (16, 8), (17, 8), (15, 9), (16, 9)}

    def shirt(x, rel, c):
        if (x, rel) in scoop or rel == 7 and 14 <= x <= 17:
            return None
        if rel == 12:
            return 'D03C50'                                    # 단 (빨강 줄)
        if c == (0x96, 0x54, 0x48):
            return 'B4B8D0'
        if c == (0xF0, 0xB0, 0x80):
            return 'FFFFFF'
        return 'EEF0F8'
    torso_skin(f, shirt)
    for rel in (8, 9):                                         # 목 둘레 파란 줄
        for x in (13, 18):
            if f.px(x, rel)[3] and rgb(f.px(x, rel)) not in ((0x1A, 0x11, 0x2C),):
                f.put(x, rel, '4A64D8', mark=False)
    for x in range(11, 21):                                    # 머리띠
        p = f.px(x, 2)
        if p[3] and rgb(p) != OUTL:
            f.put(x, 2, 'FFFFFF' if x % 4 else 'D03C50', mark=False)
        p = f.px(x, 1)
    f.put(11, 2, 'D03C50', mark=False); f.put(20, 2, 'D03C50', mark=False)
    f.art(21, 1, ['W.', 'RW', '.R', 'R.'], {'W': hx('FFFFFF'), 'R': hx('D03C50')})     # 오른쪽으로 날리는 띠 끝
    f.outline()


def wr_bronze(f):              # 청동 샅바 (희귀): 청동 샅바 · 팔 토시 · 어깨 청동 장식
    f.recolor({'BE2836': '8A4E22', 'F2504E': 'D8963E', '5A6BFF': 'D8963E', 'A0AEFF': 'F8D488', 'AA4246': 'AA4246'}, 12, 16)
    f.recolor({'3842C8': '6A3A18'}, 12, 16)
    for rel in range(9, 14):                                   # 팔 토시 (살이 보이는 팔 칸 중 바깥 3칸)
        for x in list(range(6, 11)) + list(range(21, 26)):
            p = f.px(x, rel)
            if p[3] and rgb(p) in SKIN_W:
                c = rgb(p)
                f.put(x, rel, {(0xCE, 0x84, 0x5E): 'C8843C', (0x96, 0x54, 0x48): '7A4818', (0xF0, 0xB0, 0x80): 'F0C070'}.get(c, 'C8843C'), mark=False)
    for rel in (9, 13):                                        # 토시 테두리 청동 띠
        for x in list(range(7, 11)) + list(range(21, 25)):
            p = f.px(x, rel)
            if p[3] and rgb(p) != OUTL:
                f.put(x, rel, 'F0C070' if rel == 9 else '5A3010', mark=False)
    for x in (9, 10, 21, 22):                                  # 어깨 갑옷 (청동 + 청록 녹)
        t = col_top(f, x, 4)
        if t is not None:
            f.put(x, t, 'D8963E'); f.put(x, t + 1, 'F0C070' if x in (10, 21) else 'B8742C')
    f.put(9, 10, '4FA08A', mark=False); f.put(22, 11, '4FA08A', mark=False)
    f.outline()


def wr_crypt(f):               # 묘역의 장사 (영웅): 잿빛 살 · 흰 머리 · 해골 샅바 · 어깨 뼈 가시 · 초록 눈빛
    f.recolor({'CE845E': '9C92A8', '965448': '6A6082', 'F0B080': 'C8C0DA', 'AA4246': '5A4A72',
               '342648': 'DAD6E8', '604C7C': 'FFFFFF', '462628': '7A748C', '3E1C26': '3A2E4A', '120B20': '7CFFB0'}, 0, 99)
    f.recolor({'BE2836': 'E4DCC4', 'F2504E': 'FFFAE6', '5A6BFF': '5A3A82', 'A0AEFF': '9A72C8', '3842C8': '3E2660'}, 12, 16)
    f.art(14, 13, ['SSSSSS', 'SKSSKS', '.STTS.'], {'S': hx('F4EEDA'), 'K': hx('2A1E3C'), 'T': hx('9A907A')})
    for x, side in ((9, -1), (22, 1)):                         # 어깨 뼈 가시: 어깨 위에서 바깥으로 기운 굵은 뿔 둘
        t = col_top(f, x, 3)
        if t is None:
            continue
        x0 = x if side > 0 else x - 1
        f.art(x0 - (1 if side < 0 else 0), t - 4, ['.B' if side > 0 else 'B.', 'BB', 'BB', 'bb'][0:4], {'B': hx('F4EEDA'), 'b': hx('C8BEA4')})
        f.put(x + side * 2, t - 3, 'F4EEDA')
    f.outline()


def wr_referee(f):             # 심판 (전설): 줄무늬 심판복 · 검은 챙 모자 · 호루라기 · 검은 반바지 샅바
    def stripes(x, rel, c):
        dark = c == (0x96, 0x54, 0x48)
        if x % 2 == 0:
            return '23202E'
        return 'B8B8C8' if dark else 'F4F4F8'
    scoop = {(15, 7), (16, 7), (15, 8), (16, 8)}
    torso_skin(f, stripes, 8, 23, 7, 12, skip=scoop)           # 짧은 소매까지 (팔 윗부분)
    f.recolor({'BE2836': '23202E', 'F2504E': '3A3646', '5A6BFF': 'F4F4F8', 'A0AEFF': 'FFFFFF', '3842C8': 'B8B8C8'}, 12, 16)
    for x in (13, 14, 17, 18):                                 # 흰 깃
        f.put(x, 7, 'F4F4F8')
    # 검은 챙 모자 (머리카락 위) + 흰 띠 + 금 휘장
    cap = {'K': hx('23202E'), 'k': hx('3A3646'), 'W': hx('F4F4F8'), 'G': hx('F0C040')}
    f.art(14, -3, ['KKKK'], cap)
    f.art(12, -2, ['KKkkkkKK'], cap)
    f.art(11, -1, ['KKkkkkKKKK'], cap)
    f.art(11, 0, ['KKKKKKKKKK'], cap)
    f.art(11, 1, ['WWWWWWWWWW'], cap)
    f.art(11, 2, ['KKKKKKKKKKK'], cap)
    f.art(15, -1, ['GG'], cap)
    f.art(20, 2, ['KK'], cap)
    # 입에 문 호루라기 + 가슴 끈
    f.art(15, 6, ['SSY'], {'S': hx('D8DCE8'), 'Y': hx('F0C040')})
    f.put(14, 8, '23202E'); f.put(14, 9, '23202E'); f.put(15, 10, '23202E')
    f.art(15, 11, ['YY'], {'Y': hx('F0C040')})
    f.outline()


# ------------------------------------------------------------------ 공사 반장 (머리 꼭대기 y11~12)
#   안전모 rel 0~5 · 얼굴 rel 6~8 · 조끼 rel 9~14 · 바지 rel 15~17 · 안전화 rel 18~19
#   안전모 c F2E454 i D4B228 k FFF796 p AA841E j 342208 · 조끼 f F88C2C s C45C1E u FFB858 b 1E1826(검정 줄) n 462E36 · 소매 l 606A86 m 3E4664 · 바지 d 3C466A r 262C4A
#   콧수염 q 583428 · 눈 v 181024 · 안전화 o 925C36 h 5C3A26 · 살 e E8AC80 g B26E58
def fm_night(f):               # 야간 작업 (일반): 흰 안전모 + 이마 전등 · 연두 형광 조끼 + 은색 반사 줄 · 바짓단 반사띠
    f.recolor({'F2E454': 'ECEFF6', 'D4B228': 'B4BAD0', 'FFF796': 'FFFFFF', 'AA841E': '848CA8'}, 0, 5)
    f.recolor({'F88C2C': 'B8F020', 'C45C1E': '78A810', 'FFB858': 'E8FF80', '1E1826': 'C4CAD8', '462E36': '58602C'}, 9, 14)
    f.art(13, 2, ['KKKKKK', 'KwwwwK', 'KKKKKK'], {'K': hx('4A5068'), 'w': hx('FFF6B0')})       # 이마 전등 (안전모 앞)
    f.put(12, 3, 'FFF6B0'); f.put(19, 3, 'FFF6B0')
    for x in list(range(11, 15)) + list(range(17, 21)):        # 바짓단 반사띠
        p = f.px(x, 17)
        if p[3] and rgb(p) != OUTL:
            f.put(x, 17, 'C4CAD8', mark=False)
    f.outline()


def fm_snow(f):                # 제설차 (희귀): 눈 쌓인 안전모 · 빨간 목도리 · 얼음빛 조끼 · 서리 낀 콧수염
    f.recolor({'F88C2C': '5A9AD8', 'C45C1E': '3C70B0', 'FFB858': 'A8D8F8', '1E1826': '1A2644', '462E36': '2A4670'}, 9, 14)
    f.recolor({'583428': 'D8E4F4'}, 6, 8)
    f.recolor({'3C466A': '2E3E70', '262C4A': '1C264C', '3E4664': '4A5A80'}, 15, 17)
    for rel in (0, 1):                                         # 안전모 윗부분이 눈에 덮임
        for x in range(10, 22):
            p = f.px(x, rel)
            if p[3] and rgb(p) not in (OUTL, (0x34, 0x22, 0x08)):
                f.put(x, rel, 'F6FAFF' if (x + rel) % 3 else 'D0DEF4', mark=False)
    f.art(12, -1, ['FFFFFFFF'], {'F': hx('F6FAFF')})
    f.art(13, -2, ['FFFF'], {'F': hx('D0DEF4')})
    for x in range(9, 23):                                     # 빨간 목도리 (턱 아래 한 줄 + 늘어진 끝)
        p = f.px(x, 8)
        if p[3] and rgb(p) != OUTL:
            f.put(x, 8, 'E0402E' if x % 3 else 'A82820', mark=False)
    f.art(10, 9, ['RR', 'Rr', 'RR'], {'R': hx('E0402E'), 'r': hx('A82820')})
    for (x, rel) in ((9, 9), (10, 9), (21, 9), (22, 9)):       # 어깨에 쌓인 눈
        if f.px(x, rel)[3]:
            f.put(x, rel, 'F6FAFF', mark=False)
    f.outline()


def fm_hell(f):                # 지옥 공사 (영웅): 그을린 안전모 + 용암 금 · 뿔 · 용암빛 조끼 · 이글거리는 눈 · 발바닥 불
    f.recolor({'F2E454': '3E343C', 'D4B228': '2A222A', 'FFF796': '6A5A62', 'AA841E': '1E181E'}, 0, 5)
    for (x, rel) in ((14, 1), (14, 2), (15, 3), (15, 4), (18, 2), (18, 3)):
        if f.px(x, rel)[3]:
            f.put(x, rel, 'FF6A20', mark=False)
    horn = {'H': hx('A02818'), 'h': hx('E85A2A'), 'T': hx('FFC040')}
    f.art(8, -2, ['T.', 'hH', 'hH', '.H'], horn)               # 왼쪽 뿔 (모자 옆에서 위로 휘어)
    f.art(22, -2, ['.T', 'Hh', 'Hh', 'H.'], horn)
    f.recolor({'F88C2C': 'FF5A18', 'C45C1E': 'B02808', 'FFB858': 'FFC040', '1E1826': '1E100C', '462E36': '3A1810'}, 9, 14)
    f.recolor({'E8AC80': 'C88460', 'B26E58': '8A5238', '583428': '2A1812', '181024': 'FFE060'}, 6, 8)
    f.recolor({'3C466A': '3A2420', '262C4A': '241412', '3E4664': '4A2C28'}, 15, 17)
    f.recolor({'606A86': '4A3C46', '3E4664': '342A34'}, 9, 14)
    f.recolor({'925C36': '4A2A20', '5C3A26': 'FF7A28'}, 18, 19)       # 안전화 · 발바닥이 달궈짐
    f.outline()


def fm_beta(f):                # 베타 테스터 (전설): 창틀 안전모(파란 제목줄 · 빨간 X) · 베타 딱지 · 회색 조끼 · 깨진 화소 번짐
    f.recolor({'F2E454': 'C8CCD6', 'D4B228': 'A0A6B6', 'FFF796': 'F4F6FA', 'AA841E': '7C8298', '342208': '2A2E44'}, 0, 5)
    for rel in (1, 2):                                         # 제목줄
        for x in range(9, 23):
            p = f.px(x, rel)
            if p[3] and rgb(p) not in (OUTL, (0x2A, 0x2E, 0x44)):
                f.put(x, rel, '2A5ED8' if rel == 1 else '4A80F0', mark=False)
    for (x, rel) in ((19, 1), (20, 1), (19, 2), (20, 2)):
        if f.px(x, rel)[3]:
            f.put(x, rel, 'E02830', mark=False)
    f.put(20, 1, 'FFFFFF', mark=False)
    for x in range(12, 20):                                    # 이마의 빨간 BETA 띠 (글자는 흰 점으로)
        p = f.px(x, 4)
        if p[3] and rgb(p) != OUTL:
            f.put(x, 4, 'E02830' if x % 2 == 0 else 'FFFFFF', mark=False)
    f.recolor({'F88C2C': '8A98B8', 'C45C1E': '5C6888', 'FFB858': 'B8C4DC', '1E1826': '23283C', '462E36': '23283C'}, 9, 14)
    f.recolor({'606A86': '4A5A90', '3E4664': '344070'}, 9, 14)
    f.art(13, 9, ['KKKKK'] * 6, {'K': hx('11142A')})
    f.art(14, 9, ['WW.', 'W.W', 'WW.', 'W.W', 'WW.'], {'W': hx('F4F6FA')})   # 가슴 베타 딱지 (B)
    for (x, rel, c) in ((7, 12, '40F0FF'), (6, 13, 'FF40D0'), (25, 11, 'FF40D0'), (24, 12, '40F0FF'), (8, 16, '40F0FF'), (23, 17, 'FF40D0')):
        f.put(x, rel, c)
    f.outline()


SKINS = {
    ('wrestler', 'local'): wr_local, ('wrestler', 'bronze'): wr_bronze,
    ('wrestler', 'crypt'): wr_crypt, ('wrestler', 'referee'): wr_referee,
    ('foreman', 'night'): fm_night, ('foreman', 'snow'): fm_snow,
    ('foreman', 'hell'): fm_hell, ('foreman', 'beta'): fm_beta,
}


def char_skins():
    for (base, name), fn in SKINS.items():
        sid = base + '__' + name
        frames = frames_of(base)
        for f in frames:
            fn(f)
        sheet = np.concatenate([f.a for f in frames], axis=1)
        out = os.path.join(CHARS, sid + '.png')
        sheet_meta(os.path.join(CHARS, base + '.png.meta'), base, sid, out + '.meta')
        write_png(sheet, out)
        print('wrote', sid)


# ------------------------------------------------------------------ 무기 (기본과 같은 크기 · 같은 메타)
def weapon_art():
    O = '1A112C'
    W = {}
    # 밧줄 샅바 18x8: 굵은 누런 밧줄 · 사선 꼬임 · 오른쪽 끝에 올 풀린 매듭
    a = np.zeros((8, 18, 4), np.uint8)
    rope = {'Y': hx('F0D898'), 'y': hx('D8B068'), 'd': hx('A8783C'), 'D': hx('7A5228')}
    for y in range(8):
        for x in range(18):
            d = ((x - 7.2) / 6.6) ** 2 + ((y - 3.5) / 3.1) ** 2
            if 0.38 <= d <= 1.12:
                k = (x + y) % 3
                c = 'Y' if k == 0 else 'y' if k == 1 else 'd'
                if y >= 5:
                    c = 'd' if k < 2 else 'D'
                a[y, x] = rope[c]
    for (x, y) in ((13, 1), (14, 1), (13, 2), (14, 2), (15, 2), (13, 3), (14, 3), (15, 3), (13, 4), (14, 4), (15, 4), (13, 5), (14, 5), (15, 5), (14, 6)):
        a[y, x] = rope['d'] if (x + y) % 2 else rope['D']                  # 묶은 매듭
    for (x, y) in ((16, 2), (17, 2), (16, 3), (16, 5), (17, 5), (17, 6)):
        a[y, x] = rope['y'] if (x + y) % 2 else rope['d']                  # 늘어진 끝
    _outline(a, O)
    W['satba__rope'] = ('satba', a)
    # 비단 띠 18x8: 분홍 자주 비단 · 금실 가장자리 · 금 술 · 나부끼는 물결
    a = np.zeros((8, 18, 4), np.uint8)
    for x in range(1, 16):
        cy = 3.4 + 1.5 * math.sin(x * 0.55)
        for y in range(8):
            d = y - cy
            if -1.8 <= d <= 1.9:
                if d < -1.1:
                    c = 'F0C040'                                  # 금실 윗가장자리
                elif d > 1.1:
                    c = 'C89020'                              # 금실 아랫가장자리
                else:
                    c = ('E868A0' if d < 0 else 'C03A68') if x % 4 else 'FFE890'      # 비단 + 금 수 놓은 마디
                a[y, x] = hx(c)
    for (x, y) in ((16, 3), (17, 3), (16, 5), (17, 6), (16, 4)):                       # 금 술
        a[y, x] = hx('F0C040' if x == 16 else 'C89020')
    _outline(a, O)
    W['satba__silk'] = ('satba', a)

    # ---- 삽(plow) 19x19: 손잡이 x0~8 · 날 x9~18 (오른쪽이 날끝) — 같은 실루엣 위에 새 재질
    base = np.asarray(Image.open(os.path.join(WEAP, 'weapon_plow.png')).convert('RGBA')).copy()
    handle_mask = base.copy()
    handle_mask[:, 9:] = 0                                     # 손잡이 부분만
    # 마당 빗자루: 밝은 대나무 손잡이 · 빨간 끈 · 지푸라기 솔(끝이 들쭉날쭉)
    a = np.zeros_like(base)
    a[:, :9] = handle_mask[:, :9]
    for y in range(19):
        for x in range(9):
            if a[y, x, 3] and tuple(a[y, x, :3]) != hx(O)[:3]:
                a[y, x] = {0: hx('E0C07A'), 1: hx('C09850')}.get(0 if y == 9 else 1, hx('C09850'))
    straw = {'Y': hx('F4DC8C'), 'y': hx('D8B050'), 'd': hx('A07C30'), 'D': hx('6E5018')}
    for y in range(19):
        half = 0.5 + abs(y - 9) * 0.0
        for x in range(9, 18):
            t = (x - 9) / 9.0
            top = 9 - (1.8 + 7.4 * t)                         # 부채꼴: 끈에서 좁고 끝에서 넓음
            bot = 9 + (1.8 + 7.4 * t)
            if top - 0.3 <= y <= bot + 0.3:
                if x == 17 and (y * 7) % 3 == 0:
                    continue                                     # 들쭉날쭉한 솔 끝
                k = (y + x // 3) % 3
                c = 'Y' if k == 0 else 'y' if k == 1 else 'd'
                if x >= 16 and (y % 2 == 0):
                    c = 'D' if k == 2 else c
                a[y, x] = straw[c]
    for y in range(6, 13):                                       # 빨간 묶음 끈
        for x in (9, 10):
            a[y, x] = hx('D03C3A' if x == 9 else 'F46E56')
    for y in (6, 12):
        a[y, 9] = hx('8A1C28'); a[y, 10] = hx('8A1C28')
    _outline(a, O)
    W['plow__broom'] = ('plow', a)
    # 쇠스랑: 나무 손잡이 · 쇠 가로대 · 갈퀴 이빨 넷(끝이 뾰족)
    a = np.zeros_like(base)
    a[:, :9] = handle_mask[:, :9]
    iron = {'L': hx('E4EAF8'), 'M': hx('A4ACC0'), 'D': hx('5E6678'), 'K': hx('3A4054')}
    for y in range(3, 16):                                       # 가로대
        for x in (9, 10):
            a[y, x] = iron['L'] if x == 9 else iron['M']
    for y in (3, 15):
        a[y, 9] = iron['M']; a[y, 10] = iron['D']
    for y in range(7, 12):                                       # 손잡이와 가로대를 잇는 쇠 목
        a[y, 9] = iron['M']
    for y0 in (3, 6, 10, 14):                                    # 이빨 (2칸 굵기 · 끝에서 한 칸으로 좁아짐)
        for x in range(11, 18):
            a[y0, x] = iron['L'] if x < 16 else iron['M']
            if x < 16:
                a[y0 + 1, x] = iron['M'] if x < 15 else iron['D']
        a[y0 + 1, 16] = (0, 0, 0, 0)
    _outline(a, O)
    W['plow__pitchfork'] = ('plow', a)

    for sid, (base_name, arr) in W.items():
        out = os.path.join(WEAP, 'weapon_%s.png' % sid)
        src = os.path.join(WEAP, 'weapon_%s.png.meta' % base_name)
        s = open(src, encoding='utf-8').read()
        s = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + old_guid(out + '.meta'), s, count=1)
        retry(lambda: open(out + '.meta', 'w', encoding='utf-8', newline='\n').write(s))
        write_png(arr, out)
        print('wrote weapon', sid)


def _outline(a, O):
    """불투명 화소 둘레(투명한 곳)에 윤곽선 — 가장자리 칸은 잘라내지 않고 이미 있는 윤곽(O 색)은 그대로"""
    h, w = a.shape[:2]
    solid = a[:, :, 3] > 0
    col = np.array(hx(O), np.uint8)
    for y in range(h):
        for x in range(w):
            if solid[y, x]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and solid[ny, nx]:
                    a[y, x] = col
                    break


def run():
    char_skins()
    weapon_art()


if __name__ == '__main__':
    run()
