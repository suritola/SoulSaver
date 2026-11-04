# 스킨 물체 겉모습 (스킨 시청각 개편 마무리 · 기획 1-2 D1 「설명 약속」 · 4-12 ~ 4-14 「물체」 칸)
#   판정 물체(더미 · 묘비)는 같은 칸 크기 · 같은 피벗 · 같은 바깥 상자 — 그림만 바꿈 (L11). .meta 는 기본 그림 것을 복사 (자르기 · 피벗 그대로)
#   더미 (Weapons/weapon_dummy__<캐릭터 스킨>.png 128x32 · 4칸): 종이 인형 · 묘역 인형사(해골 인형) · 개발자의 손(손가락 커서)
#     → SkinData.HeldSprite 가 무기 스킨이 없을 때 「weapon_<무기>__<캐릭터 스킨>」 을 찾음 (무기 스킨이 먼저)
#   묘비 (FX/prop_grave[_big|_temp]__<캐릭터 스킨>.png): 석공(작은 조각상) · 지옥 장의사(불꽃 표식) · 미완성 묘비(TODO 회색 상자)
#     → SkinFx.ObjectSheet 가 「<원래 이름>__<스킨>」 을 찾음 (내 묘비만 — 적 묘비는 그대로)
#   손 그림 (Weapons/weapon_camera__photographer__f12.png): 카메라 = F12 키캡 (같은 크기 · 같은 잡는 자리)
# 실행: py tools/pixelart/make_skin_objects.py  (다시 돌려도 guid 유지 · 끝에 상자 검사)
import os, sys
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
from make_skins2 import hx, write_png, sheet_meta, WEAP
from make_skins3 import _outline

FX = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'FX')
O = hx('1A112C')
AL = hx('9FB8FF')


def load(path):
    return np.asarray(Image.open(path).convert('RGBA')).copy()


def px(c):
    return np.array(list(c[:3]) + [c[3] if len(c) > 3 else 255], np.uint8)


def grid(rows, pal):
    h, w = len(rows), max(len(r) for r in rows)
    a = np.zeros((h, w, 4), np.uint8)
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch in pal:
                a[y, x] = px(pal[ch])
    return a


def paste(dst, src, x0, y0):
    h, w = src.shape[:2]
    for y in range(h):
        for x in range(w):
            if src[y, x, 3] and 0 <= y0 + y < dst.shape[0] and 0 <= x0 + x < dst.shape[1]:
                dst[y0 + y, x0 + x] = src[y, x]


def bbox(a):
    ys, xs = np.nonzero(a[:, :, 3] >= 128)
    return (xs.min(), ys.min(), xs.max(), ys.max()) if len(xs) else None


# ------------------------------------------------------------------ 더미 (기본 weapon_dummy.png 의 알파 · 아군 테두리를 따라 칠함)
def paper_cell(base, fr):
    """종이 인형: 아코디언처럼 접힌 크림색 종이 — 4칸마다 접힌 선 · 면마다 밝고 어두움 번갈아 · 빨간 리본 목"""
    a = base[:, fr * 32:(fr + 1) * 32].copy()
    light, mid, crease, red, ink = hx('FFF6DC'), hx('E6D6AC'), hx('A88C4C'), hx('D8403C'), hx('5A4A30')
    out = np.zeros_like(a)
    for y in range(32):
        for x in range(32):
            p = a[y, x]
            if not p[3]:
                continue
            if tuple(p[:3]) == AL[:3] or y <= 4:
                out[y, x] = p
                continue
            panel = (x // 4) % 2
            col = light if panel == 0 else mid
            if x % 4 == 0:
                col = crease
            if y == 10 and 13 <= x <= 18:
                col = red                                   # 목 리본
            if fr == 0 and y == 7 and x in (14, 17):
                col = ink                                   # 눈 두 점
            out[y, x] = px(col)
    return out


def skeleton_cell(base, fr):
    """해골 인형: 뼛빛 몸 · 갈비뼈 줄 · 눈구멍에 녹색 영혼불 (묘역 인형사)"""
    a = base[:, fr * 32:(fr + 1) * 32].copy()
    bone, bone2, gap, soul = hx('ECE6D0'), hx('C8BE9E'), hx('3A3E36'), hx('58FFA8')
    out = np.zeros_like(a)
    for y in range(32):
        for x in range(32):
            p = a[y, x]
            if not p[3]:
                continue
            if tuple(p[:3]) == AL[:3] or y <= 4:
                out[y, x] = p
                continue
            col = bone
            if 5 <= y <= 10:                                # 머리 (해골)
                col = bone if y <= 8 else bone2
                if fr == 0 and y in (7, 8) and x in (13, 14, 17, 18):
                    col = soul if y == 7 else gap
                if fr == 0 and y == 9 and x in (15, 16):
                    col = gap
                if fr == 0 and y == 10 and x % 2 == 0:
                    col = gap
                if fr in (1, 3) and y in (7, 8) and x >= 16:
                    col = soul if y == 7 else gap
            elif 11 <= y <= 15:                             # 팔뼈: 가운데 줄만 밝게 · 마디
                col = bone if y == 13 else bone2
                if x in (8, 23) or (fr in (1, 3) and x in (8, 9)):
                    col = gap
            elif 16 <= y <= 24:                             # 갈비뼈 · 등뼈
                col = bone if y % 2 == 0 else gap
                if 15 <= x <= 16 or (fr in (1, 3) and x == 15):
                    col = bone2
            elif y >= 29:
                col = bone2
            else:                                           # 골반 · 다리뼈
                col = bone if y in (25, 26) else bone2
                if y >= 27 and x in (15, 16):
                    col = gap
            out[y, x] = px(col)
    return out


HAND = [
    '.......aa..........',
    '......awwa.........',
    '......awwa.........',
    '......awwa.........',
    '......awwa.........',
    '......awwaaa.......',
    '......awwawsaaa....',
    '......awwawsawsa...',
    '...aa.awwawsawsaa..',
    '..awsaawwawsawsasa.',
    '..awwsawwwwwwwwasa.',
    '...awwwwwwwwwwwwsa.',
    '...awwwwwwwwwwwwsa.',
    '....awwwwwwwwwwwsa.',
    '....awwwwwwwwwwsa..',
    '.....awwwwwwwwwsa..',
    '.....awwwwwwwwwsa..',
    '......aaaaaaaaaa...',
    '......acccccccca...',
    '......addddddda....',
    '......aaaaaaaaa....',
]


def hand_cell(fr):
    """개발자의 손: 손가락 커서 (개발자 보스의 화살표 커서 fx_devcursor 와 다른 모양) · 휘두르는 칸 = 누름 + 클릭 빛살"""
    pal = {'a': O, 'w': hx('F4FAFF'), 's': hx('B8C8DC'), 'c': hx('40E6FF'), 'd': hx('1F8FA8')}
    out = np.zeros((32, 32, 4), np.uint8)
    press = (0, 1, 2, 1)[fr]
    paste(out, grid(HAND, pal), 7, 7 + press)
    if fr:
        ray = px(hx('40E6FF'))
        tip_x, tip_y = 14, 6 + press
        n = (0, 2, 3, 2)[fr]
        for k in range(1, n + 1):                         # 손가락 끝 둘레 클릭 빛살 셋 (위 · 왼위 · 오른위)
            for (dx, dy) in ((0, -1), (-1, -1), (1, -1)):
                x, y = tip_x + dx * (k + 1), tip_y + dy * (k + 1)
                if 0 <= x < 32 and 0 <= y < 32 and not out[y, x, 3]:
                    out[y, x] = ray
    _outline(out, '9FB8FF')
    return out


def dummy_sheets():
    base = load(os.path.join(WEAP, 'weapon_dummy.png'))
    jobs = (('puppeteer__paper', lambda fr: paper_cell(base, fr)),
            ('puppeteer__crypt', lambda fr: skeleton_cell(base, fr)),
            ('puppeteer__devhand', hand_cell))
    for sid, fn in jobs:
        sheet = np.concatenate([fn(fr) for fr in range(4)], axis=1)
        name = 'weapon_dummy__' + sid
        out = os.path.join(WEAP, name + '.png')
        sheet_meta(os.path.join(WEAP, 'weapon_dummy.png.meta'), 'weapon_dummy', name, out + '.meta')
        write_png(sheet, out)
        print('wrote', name)


# ------------------------------------------------------------------ 묘비 (16x20 · 28x28 · 임시 16x20) — 아래 흙 줄은 원래 그대로
STATUE = [                       # 작은 조각상 (16 폭 · 위 12줄): 두건 쓴 석상이 두 손을 모음
    '................',
    '......aaaa......',
    '.....abbbca.....',
    '.....abbcca.....',
    '.....acccda.....',
    '......abca......',
    '....aabbccaa....',
    '...abbbbccdda...',
    '..abbabbccadda..',
    '..abaabbccaada..',
    '..aa.abbccda.aa.',
    '.....abbccda....',
]
PLINTH = [                       # 받침돌 (16 폭 · 4줄)
    '.aaaaaaaaaaaaaa.',
    '.abbbbbbbbbbcda.',
    '.acccceeeccccda.',
    '.adddddddddddda.',
]
STONE = {'a': O, 'b': hx('D8D2C2'), 'c': hx('A8A090'), 'd': hx('6E685E'), 'e': hx('8A8274')}


def mason(base, kind):
    h, w = base.shape[:2]
    out = np.zeros_like(base)
    ground = 4 if kind != 'big' else 6
    out[h - ground:] = base[h - ground:]
    if kind == 'big':
        # 큰 묘비: 넓은 받침 + 비문 판 + 위에 같은 석상
        body = ['.' * 3 + 'a' * 22 + '.' * 3] + ['...a' + 'b' * 18 + 'cda...'] * 2 + ['...a' + 'cc' + 'e' * 14 + 'cc' + 'cda...'] + \
               ['...a' + 'c' * 18 + 'cda...'] + ['...a' + 'cc' + 'e' * 14 + 'cc' + 'cda...'] + ['...a' + 'c' * 18 + 'cda...'] + \
               ['...a' + 'd' * 20 + 'a...']
        paste(out, grid(body, STONE), 0, h - ground - len(body))
        paste(out, grid(STATUE, STONE), 6, h - ground - len(body) - len(STATUE) + 1)
    else:
        paste(out, grid(PLINTH, STONE), 0, h - ground - len(PLINTH))
        paste(out, grid(STATUE, STONE), 0, h - ground - len(PLINTH) - len(STATUE))
    return out


FLAME = ['..o...', '..oo..', '.oyo..', '.oyyo.', 'oyyyyo', 'oyyyyo', '.rooo.']
FLAME_BIG = ['...o....', '...oo...', '..oyo...', '..oyyo..', '.oyyyo..', '.oyyyyo.', 'oyywyyyo', 'oyywwyyo', 'oyyyyyyo', '.rooooo.']
FIRE = {'o': hx('FF8A28'), 'y': hx('FFD860'), 'w': hx('FFF6D0'), 'r': hx('D83A20')}


def hell(base, kind):
    """원래 묘비를 그을린 돌로 + 비문 자리에 불꽃 표식 (테 · 크기 그대로)"""
    out = base.copy()
    remap = {(208, 218, 234): hx('9C8E8A'), (150, 162, 184): hx('6E625E'), (90, 100, 128): hx('463A3A'), (185, 203, 228): hx('B09A8A')}
    h, w = out.shape[:2]
    ground = 4 if kind != 'big' else 6
    for y in range(h - ground):
        for x in range(w):
            c = tuple(int(v) for v in out[y, x, :3])
            if out[y, x, 3] and c in remap:
                out[y, x, :3] = remap[c][:3]
    # 원래 비문 새김선은 지움 (불꽃 표식이 그 자리)
    stone_b = hx('9C8E8A')[:3]
    y_lo, y_hi, x_lo, x_hi = (12, 19, 5, w - 8) if kind == 'big' else (7, h - ground - 3, 2, w - 5)
    for y in range(y_lo, y_hi + 1):
        for x in range(x_lo, x_hi + 1):
            if out[y, x, 3] and tuple(int(v) for v in base[y, x, :3]) in ((150, 162, 184), (90, 100, 128), (56, 40, 44), (185, 203, 228)):
                out[y, x, :3] = stone_b
    f = grid(FLAME_BIG if kind == 'big' else FLAME, FIRE)
    if kind == 'big':
        paste(out, f, 10, 9)
    else:
        paste(out, f, 5, 7)
    return out


FONT = {
    'T': ['###', '.#.', '.#.', '.#.', '.#.'],
    'O': ['###', '#.#', '#.#', '#.#', '###'],
    'D': ['##.', '#.#', '#.#', '#.#', '##.'],
}


def text(out, word, x0, y0, col):
    for i, ch in enumerate(word):
        for y, r in enumerate(FONT[ch]):
            for x, c in enumerate(r):
                if c == '#':
                    out[y0 + y, x0 + i * 4 + x] = px(col)


def todo(base, kind):
    """미완성 묘비: 회색 자리표 상자 + 「TODO」 비문 (상자 = 원래 묘비 바깥 상자)"""
    h, w = base.shape[:2]
    out = np.zeros_like(base)
    ground = 4 if kind != 'big' else 6
    out[h - ground:] = base[h - ground:]
    x0, y0, x1, y1 = bbox(base[:h - ground])
    box, edge, shade, ink = hx('CDCDD4'), hx('9A9AA4'), hx('B0B0BA'), hx('4A4A58')
    for y in range(y0, h - ground):
        for x in range(x0, x1 + 1):
            border = x in (x0, x1) or y == y0 or y == h - ground - 1
            col = O if border else (edge if (x == x1 - 1 or y == h - ground - 2) else box)
            out[y, x] = px(col)
    # 자리표 체크무늬 한 귀퉁이 (텍스처 없음 느낌)
    for y in range(y0 + 1, y0 + 4):
        for x in range(x0 + 1, x0 + 4):
            if (x + y) % 2 == 0:
                out[y, x] = px(shade)
    if kind == 'big':
        text(out, 'TODO', (w - 15) // 2, y0 + 7, ink)
        for x in range((w - 15) // 2, (w - 15) // 2 + 15, 2):
            out[y0 + 14, x] = px(edge)                    # 아직 안 쓴 비문 줄
        for x in range((w - 11) // 2, (w - 11) // 2 + 11, 2):
            out[y0 + 17, x] = px(edge)
    else:
        text(out, 'TO', (w - 7) // 2, y0 + 3, ink)
        text(out, 'DO', (w - 7) // 2, y0 + 9, ink)
    return out


def ghostify(src, temp_base):
    """임시(장례) 묘비 꼴: 원래 임시 묘비처럼 반투명 · 푸른 잿빛 · 점선 테"""
    out = np.zeros_like(src)
    h, w = src.shape[:2]
    lit = px((185, 203, 228, 200))
    for y in range(h):
        for x in range(w):
            p = src[y, x]
            if not p[3]:
                continue
            c = tuple(int(v) for v in p[:3])
            if c == O[:3]:
                if (x + y) % 3 != 2:
                    out[y, x] = lit
                continue
            L = 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]
            out[y, x] = px((int(L * 0.55 + 20), int(L * 0.58 + 22), int(L * 0.62 + 30), 110))
    # 흙 줄은 원래 임시 묘비 그대로
    out[h - 4:] = temp_base[h - 4:]
    return out


def grave_sheets():
    names = {'grave': 'prop_grave', 'big': 'prop_grave_big', 'temp': 'prop_grave_temp'}
    bases = {k: load(os.path.join(FX, v + '.png')) for k, v in names.items()}
    makers = {'undertaker__mason': mason, 'undertaker__hell': hell, 'undertaker__todo': todo}
    bad = 0
    for sid, fn in makers.items():
        for kind, base_name in names.items():
            if kind == 'temp':
                img = ghostify(fn(bases['grave'], 'grave'), bases['temp'])
            else:
                img = fn(bases[kind], kind)
            name = base_name + '__' + sid
            out = os.path.join(FX, name + '.png')
            sheet_meta(os.path.join(FX, base_name + '.png.meta'), base_name, name, out + '.meta')
            write_png(img, out)
            # 검사 (L11): 칸 크기 같음 · 불투명 바깥 상자가 원래 상자 안 (묘비 Wall 상자보다 크게 보이지 않게)
            b0, b1 = bbox(bases[kind] if kind != 'temp' else bases['grave']), bbox(img if kind != 'temp' else fn(bases['grave'], 'grave'))
            if img.shape != bases[kind].shape or b1[0] < b0[0] - 0 or b1[2] > b0[2] or b1[3] > b0[3]:
                print('상자 넘침:', name, b0, b1)
                bad += 1
            else:
                print('wrote', name)
    return bad


# ------------------------------------------------------------------ F12 키캡 (weapon_camera 13x8 와 같은 크기 · 잡는 자리)
KEY = [
    'aaaaaaaaaaaaa',
    'akkkkkkkkkkka',
    'akkkkkkkkkkka',
    'akkkkkkkkkkka',
    'akkkkkkkkkkka',
    'akkkkkkkkkkka',
    'assssssssssda',
    'aaaaaaaaaaaaa',
]


def f12_key():
    pal = {'a': O, 'k': hx('2C3442'), 's': hx('1C222C'), 'd': hx('141820')}
    a = grid(KEY, pal)
    cyan = hx('40F2F2')
    F = ['###', '#..', '##.', '#..', '#..']
    ONE = ['.#.', '##.', '.#.', '.#.', '###']
    TWO = ['##.', '..#', '.#.', '#..', '###']
    for i, g in enumerate((F, ONE, TWO)):
        for y, r in enumerate(g):
            for x, c in enumerate(r):
                if c == '#':
                    a[1 + y, 1 + i * 4 + x] = px(cyan)
    name = 'weapon_camera__photographer__f12'
    out = os.path.join(WEAP, name + '.png')
    sheet_meta(os.path.join(WEAP, 'weapon_camera.png.meta'), 'weapon_camera', name, out + '.meta')
    write_png(a, out)
    print('wrote', name)


if __name__ == '__main__':
    dummy_sheets()
    f12_key()
    if grave_sheets():
        sys.exit(1)
