# 스킨 도트 4 (2.2.0~): 새 영웅 셋(인형사 · 장의사 · 사진사)의 캐릭터 스킨 12 · 무기 스킨 6
#   캐릭터: Assets/Resources/Characters/<영웅>__<스킨>.png — 기본 시트(192x32 = 6프레임)를 읽어 색 바꾸기 + 덧그림
#   무기:   Assets/Resources/Weapons/weapon_<무기>__<스킨>.png — 기본 무기와 같은 크기 · 같은 잡는 자리(메타 복사)
#           인형사 무기 스킨(곰 인형 · 목각 인형)은 손에 든 그림이 아니라 더미 그림(weapon_dummy.png 128x32 = 4칸)을 바꿈
#   덧그림 좌표: x = 칸 안 가로 자리 · y = 「그 프레임 머리 꼭대기 줄」 기준 (rel) → 몸이 위아래로 흔들려도 같이 따라감
#   make_skins2.py 의 도구(F · hx · sheet_meta …)를 그대로 씀. 실행: 이 파일만 돌려도 됨 (다시 돌려도 guid 유지)
import math, os, re, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from make_skins2 import (F, hx, luma, retry, write_png, old_guid, sheet_meta, frames_of, CHARS, WEAP)
from make_skins3 import _outline

OUTL = '1A112C'


def rgb(p):
    return tuple(int(v) for v in p[:3])


def box(f, x0, rel0, w, h, c, behind=False):
    for j in range(h):
        for i in range(w):
            f.put(x0 + i, rel0 + j, c, behind)


# ------------------------------------------------------------------ 인형사 (조종대 꼭대기 y0 · 머리카락 rel 8~ · 얼굴 rel 9~15 · 망토 rel 16~30)
#   a 1A112C 외곽 · b E2AC70 c 704830 d B07648 (조종대 나무) · e FFFFFF 실 · g ECDAD8 h B89CB0 (얼굴) · f 3E1C30 · j 382C58 m 7060A4 (머리카락)
#   i D4B2FF k AC7CF0 l 7A50BA p 56368C r 3C2466 s 2E1C50 t 3E2E5E (망토) · q F0BE48 (걸쇠) · n 201632 (눈) · o 966278 (입) · u 544476 v 32264C (신발)
def pu_shadow(f):              # 그림자극 (일반): 검은 종이 실루엣 — 몸 전체가 납작한 짙은 보라 · 눈만 하얀 점
    f.recolor({'D4B2FF': '6A5E92', 'AC7CF0': '54487A', '7A50BA': '3A3158', '56368C': '2E2748', '3C2466': '241E3C',
               '2E1C50': '1E1934', '3E2E5E': '1E1934', '544476': '3A3158', '32264C': '1E1934', 'F0BE48': '6A5E92',
               'ECDAD8': '2E2748', 'B89CB0': '241E3C', '966278': '2E2748', '3E1C30': '1A1530',
               '382C58': '1A1530', '7060A4': '2C2646', '201632': 'F4F0FF'}, 0, 99)
    f.recolor({'E2AC70': 'B88A52', 'B07648': '8A5C32', '704830': '5A3A20'}, 0, 99)         # 대나무 조종대는 갈색 그대로 (그림자극 막대)
    f.outline()


def pu_paper(f):               # 종이 인형 (희귀): 접은 종이 옷 — 크림색 종이 · 접힌 선 · 붉은 리본 · 종이끈 줄
    f.recolor({'D4B2FF': 'FFF8E0', 'AC7CF0': 'F2E4BE', '7A50BA': 'E8D6A2', '56368C': 'D2BA84', '3C2466': 'BCA46C',
               '2E1C50': 'A08A56', '3E2E5E': 'BCA46C', '544476': '8A7648', '32264C': '6E5C34', 'F0BE48': 'D8403C',
               'FFFFFF': 'D0AE74'}, 0, 99)
    for (x, rel) in ((11, 20), (11, 22), (12, 24), (12, 26), (15, 19), (15, 21), (16, 24), (16, 27), (13, 18)):           # 접힌 선
        if f.px(x, rel)[3] and rgb(f.px(x, rel)) != hx(OUTL)[:3]:
            f.put(x, rel, 'A88C4C', mark=False)
    for x in (13, 14, 15):                                         # 빨간 리본 (목 걸쇠 위)
        f.put(x, 15, 'D8403C', mark=False)
    f.put(12, 14, 'D8403C', mark=False); f.put(16, 14, 'D8403C', mark=False)
    f.outline()


def pu_crypt(f):               # 묘역 인형사 (영웅): 안개 낀 이끼 망토 · 뼈 조종대 · 하얀 머리 · 초록 눈빛 · 떠도는 영혼불
    f.recolor({'D4B2FF': 'B8E0D0', 'AC7CF0': '88C0B0', '7A50BA': '5C8C86', '56368C': '3E6862', '3C2466': '2C4A48',
               '2E1C50': '22383A', '3E2E5E': '2C4A48', '544476': '3A4A50', '32264C': '26343A', 'F0BE48': '9AE8C8',
               'ECDAD8': 'E4EEE4', 'B89CB0': 'A8BCB4', '966278': '7A8C88', '3E1C30': '2A3A3A',
               '382C58': 'D0D8D4', '7060A4': 'FFFFFF', '201632': '58FFA8',
               'E2AC70': 'F6F0DA', 'B07648': 'D4CCB0', '704830': '9C9478', 'FFFFFF': '9AA8A4'}, 0, 99)
    for (x, rel, c) in ((8, 12, 'A8FFD0'), (8, 13, '58E8A8'), (23, 13, 'A8FFD0'), (23, 14, '58E8A8')):      # 영혼불
        f.put(x, rel, c)
    f.outline()


def pu_devhand(f):             # 개발자의 손 (전설): 마우스 커서를 든 손 · 작업 창 같은 망토 + 파란 제목줄 · 작업 표시줄 옷자락 · 화면빛 눈
    f.recolor({'D4B2FF': '8FA4C8', 'AC7CF0': '6B82AE', '7A50BA': '445878', '56368C': '34445E', '3C2466': '28344C',
               '2E1C50': 'CDD2DE', '3E2E5E': 'A8AEC0', '544476': '3A4660', '32264C': '262F44', 'F0BE48': '40F0FF',
               '382C58': '1E2A44', '7060A4': '3A5078', '201632': '40F0FF'}, 0, 99)
    for rel in range(0, 8):                                        # 조종대 · 실 지움 (커서로 바꿈)
        for x in range(14, 25):
            if f.px(x, rel)[3] and not (rel >= 6 and 17 <= x <= 20):
                f.clear(x, rel)
    cur = {'X': hx('1A112C'), 'W': hx('FFFFFF'), 'G': hx('B8C4DC')}
    f.art(16, 0, ['X.....', 'XX....', 'XWX...', 'XWWX..', 'XWWWX.', 'XWWXXX', 'XXWWX.'][:7], cur)
    f.put(23, 2, '4A80F0'); f.put(22, 2, '2A5ED8')
    f.put(19, 6, 'FFFFFF', mark=False); f.put(20, 6, '1A112C', mark=False)
    for x in range(9, 18):                                         # 창 제목줄 (어깨 아래 파란 띠) + 닫기 단추
        p = f.px(x, 17)
        if p[3] and rgb(p) != hx(OUTL)[:3]:
            f.put(x, 17, '2A5ED8' if x < 16 else 'E0403A', mark=False)
    f.put(10, 18, '4A80F0', mark=False); f.put(11, 18, '4A80F0', mark=False)
    for (x, rel, c) in ((9, 29, '58C060'), (10, 29, 'F0A030'), (12, 29, '4A80F0')):         # 작업 표시줄 아이콘 칸
        if f.px(x, rel)[3] and rgb(f.px(x, rel)) != hx(OUTL)[:3]:
            f.put(x, rel, c, mark=False)
    f.outline()


# ------------------------------------------------------------------ 장의사 (모자 꼭대기 y2)
#   모자 rel 0~8 (통 rel 0~5 · 띠 rel 5 · 챙 rel 6~8) · 얼굴 rel 9~12 · 깃 rel 13~14 · 상복 rel 13~25 · 아랫단 rel 24 · 바지 rel 25~29
#   c 8890B4 빛 e 545878 바탕 d 363850 그늘 · f EEF4FC g B9CBE4 띠 · h CCD4DE j 929CB2 얼굴 · i 201632 눈 · k F8FAFF l 40425E
def clear_hat(f):
    """모자 줄(rel 0~8)을 지움 — 얼굴 윗부분(rel 9~)은 그대로"""
    for rel in range(0, 9):
        for x in range(8, 24):
            f.clear(x, rel)


def ut_keeper(f):              # 묘지기 (일반): 모자 대신 짙은 이끼색 두건 · 올리브 외투 · 왼쪽에 랜턴 (따뜻한 불빛)
    clear_hat(f)
    f.recolor({'8890B4': '8C9C78', '363850': '343C2C', '545878': '4C5A40', '40425E': '3A4430', '6E7494': '6C7A5C',
               'CCD4DE': 'E8DCC0', '929CB2': 'C8B898', 'EEF4FC': 'F0E8D0', 'B9CBE4': 'C8C0A0', 'F8FAFF': 'FFF4D8'}, 0, 99)
    hood = {'H': hx('3E4A34'), 'h': hx('566648'), 'D': hx('2C3424')}
    for rel, (x0, x1) in {1: (14, 17), 2: (13, 18), 3: (13, 18), 4: (12, 19), 5: (12, 19), 6: (11, 20), 7: (11, 20), 8: (11, 20)}.items():
        for x in range(x0, x1 + 1):
            f.put(x, rel, 'D' if x == x1 else 'h' if x == x0 else 'H', mark=True) if False else f.put(x, rel, '2C3424' if x == x1 else '566648' if x == x0 else '3E4A34')
    for rel in range(9, 13):
        f.put(11, rel, '566648'); f.put(20, rel, '2C3424')
    f.art(10, 13, ['hhHHHHHHHHDD'], hood)                         # 어깨를 덮는 두건 자락
    lan = {'X': hx(OUTL), 'F': hx('2C2C3C'), 'Y': hx('FFE070'), 'W': hx('FFF8C0'), 'G': hx('C8A838')}
    f.art(9, 19, ['XFX', 'XYX', 'XWX', 'XGX'], lan)
    f.put(10, 18, '1A112C')
    f.outline()


def ut_mason(f):               # 석공 (희귀): 돌가루 앉은 잿빛 중절모(통 높이 그대로) · 갈색 외투 위 아마 앞치마 · 허리에 끌
    f.recolor({'8890B4': 'B0AEB8', '363850': '5A5864', '545878': '8A8896', '40425E': '54525E', 'EEF4FC': 'FFFFFF', 'B9CBE4': 'E4DCC8', 'F8FAFF': 'FFFFFF'}, 0, 8)
    f.recolor({'8890B4': 'A08A70', '363850': '4A3A2E', '545878': '6E5A48', '40425E': '54443A', '6E7494': '8A7662',
               'CCD4DE': 'E4D0B8', '929CB2': 'C0A488', 'EEF4FC': 'F4E8D8', 'B9CBE4': 'D8C8B0', 'F8FAFF': 'FFF6E8'}, 9, 99)
    ap = {'A': hx('E0D4B8'), 'a': hx('BCAE8C'), 'B': hx('8A7C5C'), 'S': hx('C8D0E8'), 's': hx('808AA8')}
    for rel in range(14, 25):                                      # 앞치마 (몸통 가운데 8칸)
        for x in range(12, 20):
            p = f.px(x, rel)
            if p[3] and rgb(p) != hx(OUTL)[:3]:
                f.put(x, rel, 'E0D4B8' if (x < 16) else 'BCAE8C', mark=False)
    for x in range(12, 20):
        if f.px(x, 14)[3]: f.put(x, 14, '8A7C5C', mark=False)     # 목끈
    for rel in (20, 21):
        for x in range(13, 19):
            if f.px(x, rel)[3]: f.put(x, rel, '8A7C5C' if rel == 20 else 'BCAE8C', mark=False)   # 주머니 윗단
    f.art(19, 17, ['S', 'S', 's'], ap)                              # 허리에 꽂은 끌
    f.art(19, 20, ['B', 'B'], ap)
    for (x, rel) in ((9, 16), (22, 18), (10, 22), (21, 23)):       # 돌가루
        if f.px(x, rel)[3]: f.put(x, rel, 'FFFFFF', mark=False)
    f.outline()


def ut_hell(f):                # 지옥 장의사 (영웅): 검붉은 상복 · 불꽃 표식을 단 모자 · 모자 위 불꽃 · 이글거리는 눈 · 아랫단 용암빛
    f.recolor({'8890B4': '8A3030', '363850': '2A1418', '545878': '4A1E24', '40425E': '30161C', '6E7494': '7A3A3A',
               'CCD4DE': 'D8B8A0', '929CB2': 'A88070', '201632': 'FF9020', 'EEF4FC': 'FFD060', 'B9CBE4': 'FF9A38',
               'F8FAFF': 'FFE890'}, 0, 99)
    f.recolor({'8A3030': '5A2428', '4A1E24': '2E1218', '2A1418': '1E0C10'}, 0, 8)                 # 모자는 더 어둡게
    for x in range(12, 20):                                        # 띠 = 불꽃 띠
        if f.px(x, 5)[3] and rgb(f.px(x, 5)) != hx(OUTL)[:3]:
            f.put(x, 5, 'FF9A38', mark=False)
    f.art(14, 1, ['.O.', 'OYO', 'OYO', 'OOO'][0:4], {'O': hx('FF7A28'), 'Y': hx('FFD060')})        # 불꽃 표식 (모자 앞)
    f.put(12, 1, 'FF7A28'); f.put(13, 0, 'FF9A38'); f.put(18, 0, 'FF9A38'); f.put(19, 1, 'FF7A28')
    for x in range(11, 21):                                        # 아랫단 용암 안감
        p = f.px(x, 24)
        if p[3] and rgb(p) != hx(OUTL)[:3]:
            f.put(x, 24, 'FF7A28' if x % 2 == 0 else 'FFC040', mark=False)
    for (x, rel) in ((9, 20), (22, 14), (10, 11)):
        f.put(x, rel, 'FFB040')
    f.outline()


def ut_todo(f):                # 미완성 묘비 (전설): 자리표시 회색 상자 몸 · 노란 TODO 쪽지 · 모래시계 「저장 중…」 · 회색 상자 모자
    f.recolor({'8890B4': 'C4C4CC', '363850': '5C5C68', '545878': '8A8A96', '40425E': '70707C', '6E7494': '9C9CA8',
               'CCD4DE': 'DCDCE4', '929CB2': 'B4B4C0', 'EEF4FC': 'F4F4F8', 'B9CBE4': 'D0D0D8', 'F8FAFF': 'FFFFFF', '201632': '2A2A34',
               '341E38': '3A3A46'}, 0, 99)
    for rel in range(1, 6):                                        # 모자 앞면 = 평평한 회색 판 (TODO 쪽지 자리)
        for x in range(13, 19):
            if f.px(x, rel)[3] and rgb(f.px(x, rel)) != hx(OUTL)[:3]:
                f.put(x, rel, 'A8A8B4', mark=False)
    note = {'Y': hx('FFE060'), 'y': hx('C8A020'), 'K': hx('5A4A10')}
    f.art(13, 1, ['YYYYYY', 'YKYKKY', 'YKYKYY', 'YyyyyY'][0:4], note)
    hg = {'W': hx('FFFFFF'), 'Y': hx('F4D060'), 'D': hx('B8A040')}
    f.art(14, 17, ['WWW', '.W.', '.Y.', 'YDY'][0:4], hg)
    f.outline()


# ------------------------------------------------------------------ 사진사 (삼각대 끝 y9 · 머리 rel 2~5 · 카메라 rel 5~8 · 재킷 rel 9~17 · 바지 rel 17~21 · 신발 rel 21~22)
#   a 1A112C · b 58628C c 8A96BE (삼각대) · d 4C3C28 e 7E603C 머리카락 · f C8D4FF g FCFDFF 렌즈 빛 · h 2C1C46 i 28263C j 545A76 m 6C748E 카메라 · n 181E4A 렌즈
#   l BE826A o E8B48E 살 · p 7C82A2 r EEF0F8 t BAC0D6 재킷 · q 1E1A30 s 2E2A42 · u 343650 v 24243A 바지 · w 604E52 신발
def ph_press(f):               # 신문 기자 (일반): PRESS 띠를 두른 모자 · 낡은 베이지 코트 · 노란 플래시
    f.recolor({'EEF0F8': 'E8D8AC', 'BAC0D6': 'CDB888', '7C82A2': '9C8A62', '2E2A42': '5A4630', '343650': '3E382E', '24243A': '2E2A22',
               'C8D4FF': 'FFE070', 'FCFDFF': 'FFF8C0', 'BECCF0': 'FFE890', '4C3C28': '6A4A28', '7E603C': '9A6E3C',
               '604E52': '4A3A2C'}, 0, 99)
    for x in range(10, 21):                                        # 모자 챙 (머리 위에서 양옆으로)
        if f.px(x, 4)[3] and x in (10, 20):
            f.put(x, 4, '6A4A28')
    f.art(10, 4, ['bbbbbbbbbbb'][0:1], {'b': hx('6A4A28')}, behind=True)
    f.art(12, 3, ['FFFFFF', 'KKKKKK'], {'F': hx('FFFFFF'), 'K': hx('3A3030')})                   # PRESS 카드 (흰 바탕 + 글자 줄)
    f.art(12, 3, ['FFFFFF'], {'F': hx('FFFFFF')})
    f.art(13, 4, ['KKKK'], {'K': hx('3A3030')})
    f.outline()


def ph_instant(f):             # 즉석 카메라 (희귀): 무지개 줄무늬 재킷 · 분홍 즉석 카메라 · 흰 테 인화 사진
    rows = {10: ('E8483E', 'B8302A'), 11: ('F08A30', 'C06818'), 12: ('F4D850', 'C8A828'), 13: ('58C060', '389040'), 14: ('4A88E0', '2C60B0'), 15: ('8A5AD0', '6038A0')}
    for rel, (lt, dk) in rows.items():
        for x in range(7, 26):
            p = f.px(x, rel)
            if not p[3]:
                continue
            c = rgb(p)
            if c in ((0xEE, 0xF0, 0xF8), (0xBA, 0xC0, 0xD6)):
                f.put(x, rel, lt, mark=False)
            elif c == (0x7C, 0x82, 0xA2):
                f.put(x, rel, dk, mark=False)
    f.recolor({'545A76': 'E86AA0', '6C748E': 'FFB0D0', '28263C': 'B04870', '2C1C46': '7A2C50', '181E4A': '3A2A88',
               'FCFDFF': 'FFFFFF', 'C8D4FF': 'BCE0FF'}, 4, 8)
    f.art(12, 9, ['FFFFFF', 'FPPPFF'][0:1], {'F': hx('FFFFFF'), 'P': hx('F4A0C0')})
    f.art(12, 9, ['FFFF', 'FPPF', 'FFFF'], {'F': hx('FFFFFF'), 'P': hx('F4A0C0')})
    f.outline()


def ph_ghost(f):               # 유령 사진가 (영웅): 창백한 연보라 몸 · 보라 플래시 · 바지가 안개로 흩어짐 · 떠도는 영혼
    f.recolor({'EEF0F8': 'E8E0FF', 'BAC0D6': 'C4B8F0', '7C82A2': '8E80C8', '2E2A42': '4A3C80', '343650': '5A4C98', '24243A': '4A3C80',
               'C8D4FF': 'C890FF', 'FCFDFF': 'F0D8FF', 'BECCF0': 'D8B0FF', '4C3C28': '9A88D0', '7E603C': 'C0B0F0',
               '545A76': '6A5CA8', '6C748E': '8E80D0', '28263C': '3A2E70', '181E4A': '8040FF', '2C1C46': '3A2E70', '1E1A30': '3A2E70',
               'BE826A': 'B8A8E8', 'E8B48E': 'E8E0FF', '604E52': '7A6CB8', '8A96BE': 'A89CE0', '58628C': '7A6CB8'}, 0, 99)
    for rel in (19, 20):                                           # 아래가 안개로 흩어짐: 가로 한 칸씩 걸러 지움
        for x in range(10, 22):
            if (x + rel) % 2 == 0 and f.px(x, rel)[3] and rgb(f.px(x, rel)) != hx(OUTL)[:3]:
                f.put(x, rel, 'A89CE0', mark=False)
    for x in range(10, 22):
        if x not in (12, 15, 18):
            f.clear(x, 22)
        if (x % 3) != 0:
            f.clear(x, 21)
    for (x, rel, c) in ((8, 11, 'D8C8FF'), (8, 12, 'A890F0'), (24, 16, 'D8C8FF'), (24, 17, 'A890F0')):
        f.put(x, rel, c)
    f.outline()


def ph_f12(f):                 # 스크린샷 F12 (전설): 가슴에 F12 키캡 · 청록 캡처 틀 모서리 · 청록 카메라 · 청록빛 몸
    f.recolor({'EEF0F8': 'E4F8F8', 'BAC0D6': 'B8E0E0', '7C82A2': '7CB0B4', '2E2A42': '2A5058', '343650': '2C4850', '24243A': '1E3840',
               '545A76': '1F9EB0', '6C748E': '5CE0F0', '28263C': '0E5560', '181E4A': '103840', '2C1C46': '0E5560', '1E1A30': '0E4850',
               'C8D4FF': 'A0FFF0', 'FCFDFF': 'FFFFFF', 'BECCF0': '80F8F0'}, 0, 99)
    key = {'K': hx('F0F4F8'), 'k': hx('B8C4D4'), 'T': hx('22303C')}
    for j in range(7):                                             # 키캡 14x7 (rel 10~16)
        for i in range(14):
            edge = j in (0, 6) or i in (0, 13)
            f.put(9 + i, 10 + j, 'B8C4D4' if edge else 'F0F4F8', mark=False)
    glyph = ['TTT.T.TTT', 'T..TT...T', 'TT..T.TTT', 'T...T.T..', 'T...T.TTT']
    f.art(11, 11, glyph, key)
    for (x, rel) in ((8, 3), (9, 3), (8, 4), (25, 3), (24, 3), (25, 4), (8, 20), (9, 20), (8, 19), (25, 20), (24, 20), (25, 19)):     # 캡처 틀 모서리
        f.put(x, rel, '40F0FF')
    f.outline()


SKINS = {
    ('puppeteer', 'shadow'): pu_shadow, ('puppeteer', 'paper'): pu_paper,
    ('puppeteer', 'crypt'): pu_crypt, ('puppeteer', 'devhand'): pu_devhand,
    ('undertaker', 'keeper'): ut_keeper, ('undertaker', 'mason'): ut_mason,
    ('undertaker', 'hell'): ut_hell, ('undertaker', 'todo'): ut_todo,
    ('photographer', 'press'): ph_press, ('photographer', 'instant'): ph_instant,
    ('photographer', 'ghost'): ph_ghost, ('photographer', 'f12'): ph_f12,
}


def bbox0(a):
    ys, xs = np.nonzero(a[:, :, 3] > 0)
    return int(xs.min()), int(xs.max()), int(ys.min()), int(ys.max())


def char_skins():
    for (base, name), fn in SKINS.items():
        sid = base + '__' + name
        frames = frames_of(base)
        x0, x1, y0, y1 = bbox0(frames[0].a)
        for f in frames:
            fn(f)
        a0 = frames[0].a                                          # 첫 칸(설계도)은 기본과 같은 상자 안으로 (메뉴 초상 상자 규칙)
        a0[:, :x0] = 0; a0[:, x1 + 1:] = 0; a0[:y0] = 0; a0[y1 + 1:] = 0
        sheet = np.concatenate([f.a for f in frames], axis=1)
        out = os.path.join(CHARS, sid + '.png')
        sheet_meta(os.path.join(CHARS, base + '.png.meta'), base, sid, out + '.meta')
        write_png(sheet, out)
        print('wrote', sid)


# ------------------------------------------------------------------ 인형사 무기 스킨 = 더미 그림 (128x32 · 4칸: 앞 · 옆(¼) · 뒤 · 옆(¾)) — 기본과 같은 틀 · 같은 아군색 테두리
AL = hx('9FB8FF')


def dummy_base():
    return np.asarray(Image.open(os.path.join(WEAP, 'weapon_dummy.png')).convert('RGBA')).copy()


def bear_cell(base, fr):
    a = base[:, fr * 32:(fr + 1) * 32].copy()
    fur, furl, furd, cream, nose, patch, st = hx('A8743E'), hx('C8905A'), hx('7E5028'), hx('F0D8A8'), hx('3A2418'), hx('7A98C8'), hx('5A3418')
    out = np.zeros_like(a)
    for y in range(32):
        for x in range(32):
            p = a[y, x]
            if not p[3]:
                continue
            c = tuple(int(v) for v in p[:3])
            if c == AL[:3] or y <= 4:
                out[y, x] = p
                continue
            col = fur
            if 5 <= y <= 10:                                       # 머리
                col = furl if (y <= 7 and x <= 16) else fur
                if fr in (0,) and y >= 8 and 14 <= x <= 17:
                    col = cream
                if fr == 0 and y == 8 and x in (15, 16):
                    col = nose
                if fr == 0 and y == 7 and x in (13, 18):
                    col = nose
                if fr in (1, 3) and y >= 8 and x >= 17:
                    col = cream
            elif 11 <= y <= 15 and (x <= 10 or x >= 21 or fr in (1, 3)):          # 팔
                col = furl if y <= 12 else fur
                if x <= 5 or x >= 26:
                    col = cream
                if y == 13 and (x % 2 == 0) and 6 <= x <= 9:
                    col = st
            elif y >= 29:                                          # 발
                col = furd
                if fr == 0 and y == 29 and x in (12, 13, 18, 19):
                    col = cream
            else:                                                  # 몸통 · 다리
                col = furl if (x <= 13 and y <= 22) else fur
                if y >= 26:
                    col = furd
                if fr == 0 and 17 <= y <= 24 and 14 <= x <= 17:
                    col = cream
                if fr == 2 and 17 <= y <= 19 and 17 <= x <= 19:
                    col = patch
                if fr == 2 and y == 18 and x in (13, 14):
                    col = st
            out[y, x] = np.array(list(col[:3]) + [255], np.uint8)
    # 귀 둘 (머리 위 양옆) — 둥근 3x3
    for (ex, ey) in ((10, 4), (19, 4)):
        for j in range(3):
            for i in range(3):
                if (i, j) in ((0, 0), (2, 0)) and False:
                    continue
                if not out[ey + j, ex + i, 3]:
                    out[ey + j, ex + i] = np.array(list((cream if (i, j) == (1, 1) else furl)[:3]) + [255], np.uint8)
    _outline(out, '9FB8FF')
    return out


def wood_cell(base, fr):
    a = base[:, fr * 32:(fr + 1) * 32].copy()
    w1, w2, w3, jt, jd = hx('E8BE80'), hx('C69255'), hx('8E6038'), hx('DCA862'), hx('5E3C20')
    out = np.zeros_like(a)
    for y in range(32):
        for x in range(32):
            p = a[y, x]
            if not p[3]:
                continue
            c = tuple(int(v) for v in p[:3])
            if c == AL[:3] or y <= 4:
                out[y, x] = p
                continue
            col = w2
            if 5 <= y <= 10:                                       # 머리 (달걀 · 얼굴 없음)
                col = w1 if (x <= 16 and y <= 8) else w2
                if y == 10:
                    col = w3
            elif 11 <= y <= 15 and (x <= 10 or x >= 21 or fr in (1, 3)):          # 팔: 나뭇결 + 팔꿈치 마디
                col = w1 if y == 12 else w2
                if y == 14:
                    col = w3
                if x in (6, 7, 24, 25):
                    col = jt if y != 13 else jd
            elif y >= 29:
                col = w3
            else:
                col = w1 if (x <= 13 and y <= 21) else w2
                if 16 <= y <= 18:
                    col = w1 if x < 16 else w2                    # 가슴판
                if y in (19,):
                    col = jd                                       # 골반 마디
                if 22 <= y <= 24:
                    col = jt if y != 23 else jd                    # 무릎 마디
                if y >= 27:
                    col = w3
                if 20 <= y <= 21 and x in (13, 14):
                    col = w3
            out[y, x] = np.array(list(col[:3]) + [255], np.uint8)
    # 어깨 마디 · 목 마디: 몸 위쪽 두 점
    for (sx, sy) in ((10, 11), (20, 11)):
        for j in range(3):
            for i in range(2):
                if out[sy + j, sx + i, 3]:
                    out[sy + j, sx + i] = np.array(list(jt[:3]) + [255], np.uint8)
    return out


def dummy_skins():
    base = dummy_base()
    for sid, fn in (('dummy__bear', bear_cell), ('dummy__wood', wood_cell)):
        sheet = np.concatenate([fn(base, fr) for fr in range(4)], axis=1)
        out = os.path.join(WEAP, 'weapon_%s.png' % sid)
        sheet_meta(os.path.join(WEAP, 'weapon_dummy.png.meta'), 'weapon_dummy', 'weapon_' + sid, out + '.meta')
        write_png(sheet, out)
        print('wrote weapon', sid)


# ------------------------------------------------------------------ 무기 (기본과 같은 크기 · 같은 메타)
def weapon_art():
    O = OUTL
    W = {}
    base = np.asarray(Image.open(os.path.join(WEAP, 'weapon_shovel.png')).convert('RGBA')).copy()       # 30x8: 자루 x0~22 · 날 x23~29
    # 곡괭이: 같은 나무 자루 + 끝에 휜 쇠 곡괭이 머리 (양끝이 뾰족하고 앞으로 굽음)
    a = np.zeros_like(base)
    a[:, :22] = base[:, :22]
    steel = {'L': hx('F4F8FF'), 'M': hx('B4C0DC'), 'D': hx('6A7898'), 'K': hx('3C4864')}
    for y in range(8):
        for x in range(22, 30):
            d = math.hypot(x - 19.5, (y - 3.5) * 0.92)
            if 5.2 <= d <= 7.8 and x >= 23:
                a[y, x] = steel['L'] if d < 6.1 else steel['M'] if d < 7.0 else steel['D']
    for y in (3, 4):                                             # 자루와 머리 이음
        for x in range(21, 26):
            a[y, x] = steel['M'] if y == 3 else steel['D']
    a[0, 25] = steel['K'] if False else a[0, 25]
    _outline(a, O)
    W['shovel__pickaxe'] = ('shovel', a)
    # 은수저: 가는 은 자루 + 끝의 둥근 국자 (속이 오목해 어두움)
    a = np.zeros_like(base)
    sv = {'L': hx('FFFFFF'), 'M': hx('D4DEF2'), 'D': hx('8E9CBC'), 'K': hx('5A6888')}
    for x in range(0, 22):
        t = x / 22.0
        top = 3 if x > 3 else 2
        bot = 4 if x > 3 else 5
        for y in range(top, bot + 1):
            a[y, x] = sv['L'] if y == top else sv['D'] if y == bot else sv['M']
    for y in range(8):
        for x in range(22, 30):
            d = ((x - 25.6) / 4.0) ** 2 + ((y - 3.5) / 3.4) ** 2
            if d <= 1.0:
                inner = d <= 0.5
                a[y, x] = sv['K'] if inner and x > 25 else sv['M'] if inner else sv['L'] if (x + y) % 3 == 0 and d > 0.7 else sv['D'] if y >= 5 else sv['M']
    _outline(a, O)
    W['shovel__spoon'] = ('shovel', a)

    # ---- 카메라 13x8 (기본 weapon_camera 와 같은 크기 · 잡는 자리)
    # 일회용 카메라: 종이 상자 · 빨간 띠 라벨 · 작은 렌즈 구멍 · 뷰파인더 · 태엽 바퀴
    a = np.zeros((8, 13, 4), np.uint8)
    kr = {'L': hx('F0CC84'), 'M': hx('D8A860'), 'D': hx('A87C3C'), 'R': hx('D84030'), 'Y': hx('F8E060'), 'W': hx('F4F4FA'), 'G': hx('8A92AC'),
          'B': hx('3A3A60'), 'C': hx('7CA8F0'), 'H': hx('FFFFFF')}
    for y in range(2, 7):
        for x in range(1, 12):
            a[y, x] = kr['L'] if y == 2 else kr['D'] if y == 6 else kr['M']
    for x in range(2, 7):                                         # 빨간 라벨 + 노란 점
        a[4, x] = kr['R']; a[5, x] = kr['R']
    a[4, 3] = kr['Y']; a[4, 4] = kr['Y']
    for y in range(3, 7):                                         # 렌즈 (오른쪽)
        for x in range(8, 12):
            d = math.hypot(x - 9.6, y - 4.6)
            if d <= 2.1:
                a[y, x] = kr['B'] if d > 1.2 else kr['C']
    a[4, 9] = kr['H']
    a[3, 2] = kr['W']; a[3, 3] = kr['W']                           # 뷰파인더 창
    a[1, 3] = kr['G']; a[1, 4] = kr['W']; a[1, 5] = kr['G']         # 태엽 바퀴
    _outline(a, O)
    W['camera__disposable'] = ('camera', a)
    # 필름 카메라: 가죽 몸통 · 은 윗판 · 주름 상자(왼쪽) · 큰 렌즈 · 윗판에 터지는 플래시 가루
    a = np.zeros((8, 13, 4), np.uint8)
    fl = {'K': hx('2E2A3A'), 'k': hx('484458'), 'S': hx('D8E0F4'), 's': hx('8A94B4'), 'B': hx('C8A050'), 'b': hx('7C5C20'),
          'G': hx('1C2478'), 'C': hx('5C84E8'), 'H': hx('FFFFFF'), 'Y': hx('FFE070'), 'O': hx('FF9A30')}
    for y in range(3, 7):                                         # 주름 상자 (왼쪽 x1~4): 골과 마루가 번갈아
        for x in range(1, 5):
            a[y, x] = fl['k'] if x % 2 == 1 else fl['K']
    for y in range(2, 7):                                         # 가죽 몸통 x5~9
        for x in range(5, 10):
            a[y, x] = fl['k'] if y == 2 or x == 5 else fl['K']
    for x in range(5, 10):                                        # 은 윗판
        a[1, x] = fl['S']
    a[1, 9] = fl['s']
    for y in range(2, 7):                                         # 큰 렌즈 x8~11 (놋쇠 테 + 푸른 유리)
        for x in range(8, 12):
            d = math.hypot(x - 9.6, y - 4.0)
            if d <= 2.5:
                a[y, x] = fl['B'] if d > 1.9 else fl['G'] if d > 0.9 else fl['C']
    a[3, 9] = fl['H']
    a[0, 6] = fl['Y']; a[0, 7] = fl['O']; a[1, 7] = fl['Y']        # 플래시 가루 (윗판 위 작은 불꽃)
    _outline(a, O)
    W['camera__film'] = ('camera', a)

    for sid, (base_name, arr) in W.items():
        out = os.path.join(WEAP, 'weapon_%s.png' % sid)
        src = os.path.join(WEAP, 'weapon_%s.png.meta' % base_name)
        s = open(src, encoding='utf-8').read()
        s = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + old_guid(out + '.meta'), s, count=1)
        retry(lambda: open(out + '.meta', 'w', encoding='utf-8', newline='\n').write(s))
        write_png(arr, out)
        print('wrote weapon', sid)


def run():
    char_skins()
    dummy_skins()
    weapon_art()


if __name__ == '__main__':
    run()
