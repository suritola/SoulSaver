# 새 영웅 씨름꾼 「천하장사」 그림 일체 (docs/plans/2026-11-02-big-update/hero-wrestler.md 13절)
#   Characters/wrestler.png            32칸 몸 시트 (192×32 · idle 2 + run 4)
#   Weapons/weapon_satba.png           손에 든 샅바 매듭
#   Weapons/weapon_kit_140~142.png     전용 무기 들돌 · 워낭 · 밧줄 샅바 (기획서 13절 이름 weapon_liftstone · cowbell · rope 도 같은 그림으로)
#   FX/fx_*.png                        손잡이 팔 · 잡기 고리 · 던짐 꼬리 · 모래 튐 · 새끼줄 테 · 충격파 · 들썩 그림자 · 스트라이크 · 장외 · 황소 떼 · 들돌 · 밧줄 · 워낭
#   Icons/ability_<번호>.png           특수 540~546 · 전용 카드 740~745 · 고유 스킬 840~849 · 스킬 진화 940~946 · 강화 612~614
#   범주 틀은 frame_icons.py 의 틀을 그대로 가져옴 (사각 무기 · 원 스킬 · 마름모 패시브 · 육각 진화)
#   .meta 는 이미 있으면 그대로 둠 (다시 돌려도 guid 유지) · 새로 만들 때 guid 는 이름에서 정해짐
# 실행: py -3.14 -X utf8 tools/pixelart/make_wrestler.py [미리보기 폴더]
import math, os, random, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from PIL import Image
import frame_icons as FI
import make_kit_weapons as KW
from make_fx import sheet_meta

ROOT = FI.ROOT
RES = os.path.join(ROOT, 'Assets', 'Resources')
CHARS, WEAP, FXD, ICON = [os.path.join(RES, d) for d in ('Characters', 'Weapons', 'FX', 'Icons')]
K1 = (26, 17, 44)
TAU = math.tau


def guid_of(key):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'soulsaver/wrestler/' + key).hex


# ------------------------------------------------------------------ 작은 붓 (칸 → 색)
class Cv:
    def __init__(s):
        s.p = {}

    def set(s, x, y, c):
        s.p[(int(round(x)), int(round(y)))] = c

    def disc(s, cx, cy, r, c):
        for y in range(int(cy - r - 1), int(cy + r + 2)):
            for x in range(int(cx - r - 1), int(cx + r + 2)):
                if (x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2 <= r * r:
                    s.p[(x, y)] = c

    def ell(s, cx, cy, rx, ry, c):
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1:
                    s.p[(x, y)] = c

    def rect(s, x, y, w, h, c):
        for j in range(y, y + h):
            for i in range(x, x + w):
                s.p[(i, j)] = c

    def line(s, x0, y0, x1, y1, c, w=1):
        n = int(max(abs(x1 - x0), abs(y1 - y0))) * 2 + 1
        for i in range(n + 1):
            t = i / n
            x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            if w == 1:
                s.set(x, y, c)
            else:
                for dj in range(w):
                    for di in range(w):
                        s.set(math.floor(x) + di - (w - 1) // 2, math.floor(y) + dj - (w - 1) // 2, c)

    def poly(s, pts, c):
        ys = [p[1] for p in pts]
        for y in range(int(min(ys)), int(max(ys)) + 2):
            yc = y + 0.5
            xs = []
            for i in range(len(pts)):
                (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % len(pts)]
                if (y0 <= yc < y1) or (y1 <= yc < y0):
                    xs.append(x0 + (yc - y0) * (x1 - x0) / (y1 - y0))
            xs.sort()
            for a, b in zip(xs[::2], xs[1::2]):
                for x in range(int(math.floor(a + 0.5)), int(math.floor(b + 0.5))):
                    s.p[(x, y)] = c

    def ring(s, cx, cy, r0, r1, c, rx=1.0, ry=1.0):
        for y in range(int(cy - r1 * ry - 1), int(cy + r1 * ry + 2)):
            for x in range(int(cx - r1 * rx - 1), int(cx + r1 * rx + 2)):
                d = math.hypot((x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry)
                if r0 <= d <= r1:
                    s.p[(x, y)] = c

    def rows(s, x, y, rows, pal):
        for j, row in enumerate(rows):
            for i, ch in enumerate(row):
                if ch != '.':
                    s.p[(x + i, y + j)] = pal[ch]

    def outline(s, c, skip=()):
        add = {}
        for (x, y) in s.p:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                q = (x + dx, y + dy)
                if q not in s.p and q not in skip:
                    add[q] = c(x, y) if callable(c) else c
        s.p.update(add)

    def image(s, w, h):
        im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
        for (x, y), c in s.p.items():
            if 0 <= x < w and 0 <= y < h:
                im.putpixel((x, y), tuple(c[:3]) + ((c[3] if len(c) == 4 else 255),))
        return im


def save_png(im, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.save(path)


# ------------------------------------------------------------------ .meta 만들기 (있으면 그대로)
def meta_copy(template, out, key, pivot=None, ppu=None, sprite_names=None):
    m = out + '.meta'
    if os.path.exists(m):
        return
    t = open(template, encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + guid_of(key), t, count=1, flags=re.M)
    # 스프라이트 번호 새로
    r = random.Random(key)
    ids = {}
    for old in set(re.findall(r'internalID: (-?\d+)', t)):
        ids[old] = str(r.randint(-2 ** 31, 2 ** 31 - 1)) if old != '0' else '0'
    t = re.sub(r'(?<![\w-])-?\d{3,}(?![\w])', lambda mo: ids.get(mo.group(0), mo.group(0)), t)
    t = re.sub(r'spriteID: [0-9a-f]{32}', lambda mo: 'spriteID: ' + uuid.UUID(int=r.getrandbits(128)).hex, t)
    if sprite_names:
        a, b = sprite_names
        t = t.replace(a, b)
    if pivot is not None:
        t = re.sub(r'spritePivot: \{x: [-0-9.]+, y: [-0-9.]+\}', 'spritePivot: {x: %.3f, y: %.3f}' % pivot, t, count=1)
    if ppu:
        t = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, t, count=1)
    with open(m, 'w', encoding='utf-8', newline='\n') as f:
        f.write(t)


# ================================================================== 1. 몸 시트
BODY_PAL = {
    'h': (52, 38, 72), 'H': (96, 76, 124), 's': (206, 132, 94), 'S': (150, 84, 72), 'L': (240, 176, 128), 'b': (70, 38, 40), 'k': (18, 11, 32),
    'm': (170, 66, 70), 'r': (190, 40, 54), 'R': (242, 80, 78), 'u': (90, 107, 255), 'U': (160, 174, 255), 'w': (255, 250, 235),
}
ALT = (62, 28, 38)                 # 살색 그림자 쪽 외곽 (K1 보다 살짝 붉은 어두움 · Y < 41)
DUST = (214, 176, 112)

HEAD = [(0, 6, 'hhhhhh'), (1, 5, 'hHHhhhhh'), (2, 5, 'hhsssshh'), (3, 5, 'sbbssbbs'), (4, 5, 'sskssksS'), (5, 6, 'ssmmsS')]
TORSO = [
    (6, 3, 'LLLLLLsssssS'),
    (7, 1, 'LLLLLLLLssssssss'),
    (8, 1, 'LLLSssssssssSsss'),
    (9, 1, 'sssSSSSssSSSSsss'),
    (10, 1, 'sssSssssssssSsss'),
    (11, 1, 'sssSsssSSsssSsss'),
    (12, 1, 'ss' + 'rrruuurrruuu' + 'ss'),
    (13, 0, 'ss' + 'S' + 'uuurrruuurrr' + 'S' + 'ss'),
    (14, 3, 'Rrrrr' + 'ru' + 'Uuuuu'),
]
LEGS = {
    'stand': [(15, 3, 'sssSS..ssSSS'), (16, 2, 'ssssS....ssssS'), (17, 2, 'ssssS....ssssS'), (18, 1, 'ssssS......ssssS')],
    'a': [(15, 3, 'sssSS..ssSSS'), (16, 2, 'ssssS....ssssS'), (17, 4, 'sssS'), (17, 11, 'ssssS'), (18, 12, 'ssssSS')],   # 왼발 들림 · 오른발 뒤로
    'b': [(15, 4, 'ssSS..ssSS'), (16, 4, 'ssSS..ssSS'), (17, 4, 'ssSS..ssSS'), (18, 3, 'sssSS..ssSSS')],                  # 스침
    'c': [(15, 3, 'sssSS..ssSSS'), (16, 2, 'ssssS....ssssS'), (17, 2, 'ssssS'), (17, 10, 'sssS'), (18, 0, 'ssssSS')],     # 오른발 들림 · 왼발 뒤로
}
# 달릴 때 팔: 위 = 주먹이 가슴 높이 바깥, 아래 = 옆구리 (idle)
ARMS_UP_L = [(9, 0, 'sss'), (10, 0, 'ssS')]
ARMS_UP_R = [(9, 15, 'Sss'), (10, 15, 'Sss')]


def body_cells(head_dy=0, legs='stand', arm_l='down', arm_r='down', dust=()):
    c = {}

    def put(rows, dy=0):
        for y, x0, text in rows:
            for i, ch in enumerate(text):
                if ch != '.':
                    c[(x0 + i, y + dy)] = BODY_PAL.get(ch, ch)
    # 몸통 · 다리 먼저, 머리는 위에 덮음 (머리가 1칸 내려가면 어깨가 올라온 모양)
    put(TORSO)
    put(LEGS[legs])
    if arm_l == 'up':
        for y in range(9, 14):
            for x in range(0, 3):
                c.pop((x, y), None)
        put(ARMS_UP_L)
    if arm_r == 'up':
        for y in range(9, 14):
            for x in range(15, 18):
                c.pop((x, y), None)
        put(ARMS_UP_R)
    put(HEAD, head_dy)
    return c, dust


def body_frame(cells, dust):
    ox, oy = 7, 12
    cv = Cv()
    for (x, y), col in cells.items():
        cv.p[(x + ox, y + oy)] = col
    S = BODY_PAL['S']

    def edge_col(x, y):                 # 그림자 살색에 붙은 외곽만 살짝 붉은 어둠 (색 단조로움 줄임)
        return ALT if cv.p.get((x, y)) == S else K1
    cv.outline(edge_col)
    for (x, y) in dust:
        cv.p[(x + ox, y + oy)] = DUST
    return cv.image(32, 32)


def body_sheet():
    frames = [
        body_cells(0, 'stand'),
        body_cells(1, 'stand'),
        body_cells(1, 'a', 'up', 'down', dust=[(18, 18)]),
        body_cells(0, 'b', 'up', 'up'),
        body_cells(1, 'c', 'down', 'up', dust=[(-1, 18)]),
        body_cells(0, 'b', 'down', 'down'),
    ]
    sheet = Image.new('RGBA', (192, 32), (0, 0, 0, 0))
    for i, (c, d) in enumerate(frames):
        sheet.paste(body_frame(c, d), (i * 32, 0))
    out = os.path.join(CHARS, 'wrestler.png')
    save_png(sheet, out)
    names = None
    meta_copy(os.path.join(CHARS, 'cartographer.png.meta'), out, 'Characters/wrestler', sprite_names=('cartographer', 'wrestler'))
    return sheet


# ================================================================== 2. 손에 든 것 · 전용 무기
for _k, _v in {'P': '#5a6bff', 'p': '#a0aeff', 'C': '#3842c8', 's': '#f2bc96', 'S': '#ca7e70', 't': '#e8c27a', 'T': '#b08850', 'r': '#c8a064', 'q': '#e6c88c'}.items():
    KW.PAL[_k] = KW.hx(_v)


def weapon_out(name, pic, pivot_cell, template_base='sword', also=()):
    pic.outline()
    im, x0, y0 = pic.image()
    px = (pivot_cell[0] - x0 + 0.5) / im.w
    py = 1.0 - (pivot_cell[1] - y0 + 0.5) / im.h
    for nm in (name,) + tuple(also):
        path = os.path.join(WEAP, nm + '.png')
        KW.png.write(im, path)
        KW.write_meta(path, template_base, (round(px, 3), round(py, 3)))
        # 새 meta 의 guid 를 이름에서 정해 줌 (이미 있으면 그대로 · write_meta 가 처음에만 새로 만듦)
    return im.w, im.h


def w_satba():          # 샅바 끝 매듭 — 빨강 · 파랑 꼰 띠가 손에서 뻗고, 끝에 굵은 매듭과 늘어진 두 가닥
    return KW.Pic().rows([
        '.......RRkk.....',
        'ppPPCCPPppRkKRR.',
        'RkKKrrKKkkRRRkKR',
        'PPpp..PPCCPKRRR.',
        '.....CCPPC.RR...',
        '..........CP....',
    ]), (0, 2.5)


def w_liftstone():      # 들돌 — 둥근 회색 돌 · 윗면에 손잡이 홈
    p = KW.Pic()
    for y in range(10):
        for x in range(10):
            d = math.hypot(x + 0.5 - 5, y + 0.5 - 5)
            if d <= 5:
                hl = math.hypot(x + 0.5 - 3.4, y + 0.5 - 3.2)
                ch = '6' if hl < 1.5 else '5' if hl < 3.2 else '4' if d < 3.2 else '3' if d < 4.2 else '2'
                p.dot(x, y, ch)
    for x in range(3, 8):
        p.dot(x, 1, '1')
    for x in range(4, 7):
        p.dot(x, 2, '2')
    p.dot(7, 6, '1'); p.dot(2, 6, '3')
    return p, (0, 5)


def w_cowbell():        # 워낭 — 놋쇠 방울 (입구가 오른쪽) · 왼쪽에 붉은 끈 고리
    p = KW.Pic()
    hs = [3, 5, 6, 7, 8, 9, 9, 10, 10]
    for i, h in enumerate(hs):
        x = 3 + i
        top = 5 - h // 2
        for j in range(h):
            y = top + j
            ch = 'Y' if (j == 0 and i < 6) else 'y' if j < h * 0.3 else 'G' if j < h * 0.65 else 'g'
            if i >= 7:
                ch = 'g' if j in (0, h - 1) else ch
            p.dot(x, y, ch)
    p.dot(12, 5, 'q'); p.dot(12, 4, 'q'); p.dot(13, 5, 'Q')
    for x in range(0, 4):
        p.dot(x, 5, 'R')
    p.dot(0, 4, 'k'); p.dot(0, 6, 'K'); p.dot(1, 4, 'R'); p.dot(1, 6, 'R')
    return p, (0, 5)


def w_rope():           # 밧줄 샅바 — 굵은 누런 밧줄 고리 · 풀린 끝
    p = KW.Pic()
    for y in range(11):
        for x in range(11):
            d = math.hypot(x + 0.5 - 5.5, y + 0.5 - 5.5)
            if 2.6 <= d <= 5.4:
                ang = math.atan2(y + 0.5 - 5.5, x + 0.5 - 5.5)
                k = int(ang / TAU * 18 + d * 1.2) % 2
                p.dot(x, y, 'l' if k == 0 else 'r')
                if d > 4.6 and k == 0:
                    p.dot(x, y, 'L')
    for i in range(5):
        p.dot(9 + i, 8 + (i // 2), 'l' if i % 2 == 0 else 'r')
    p.dot(14, 10, 'W'); p.dot(15, 10, 'w')
    return p, (0, 5)


def weapons():
    out = []
    out.append(('weapon_satba',) + weapon_out('weapon_satba', *w_satba()))
    for kid, nm, fn in ((140, 'liftstone', w_liftstone), (141, 'cowbell', w_cowbell), (142, 'rope', w_rope)):
        out.append(('weapon_kit_%d' % kid,) + weapon_out('weapon_kit_%d' % kid, *fn(), also=('weapon_' + nm,)))
    return out


# ================================================================== 3. 이펙트 (FX)
SAND_L, SAND, SAND_D, SAND_X = (250, 232, 178), (232, 194, 122), (176, 134, 80), (122, 88, 56)
HERO, HERO_L = (90, 107, 255), (170, 182, 255)
GOLD, GOLD_D, WHITE = (254, 222, 108), (200, 140, 40), (255, 250, 232)
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def fade(cv, k):
    # 디더로 화소를 뺌 (k 0 = 그대로 · 1 = 다 사라짐) — 그라데이션 없이 사라지는 방식
    for (x, y) in list(cv.p):
        if BAYER[y % 4][x % 4] / 16.0 < k:
            del cv.p[(x, y)]


def fx_out(name, frames, w, h, ppu=16):
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, cv in enumerate(frames):
        sheet.paste(cv.image(w, h), (i * w, 0))
    out = os.path.join(FXD, name + '.png')
    save_png(sheet, out)
    if not os.path.exists(out + '.meta'):
        sheet_meta(name, w, h, len(frames), out + '.meta')
        t = open(out + '.meta', encoding='utf-8').read()
        t = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, t, count=1)
        open(out + '.meta', 'w', encoding='utf-8', newline='\n').write(t)
    return sheet


def puff(cv, cx, cy, R, n, rr, seed, light=True):
    r = random.Random(seed)
    for i in range(n):
        a = TAU * i / n + r.random() * 0.4
        d = R * (0.55 + r.random() * 0.45)
        x, y = cx + math.cos(a) * d, cy + math.sin(a) * d * 0.8
        cv.disc(x, y, rr * (0.8 + r.random() * 0.4), SAND_D)
    for i in range(n):
        a = TAU * i / n + 0.3 * math.sin(i)
        d = R * 0.5
        x, y = cx + math.cos(a) * d, cy + math.sin(a) * d * 0.8
        cv.disc(x - 0.5, y - 0.7, rr * 0.9, SAND)
        if light:
            cv.disc(x - 1, y - 1.3, rr * 0.45, SAND_L)


def fx_wrestler_arms():          # 머리 위로 뻗은 두 팔 (몸 시트와 같은 32칸 · ppu 8)
    fr = []
    for f in range(2):
        cv = Cv()
        for side in (0, 1):
            def X(x):
                return x if side == 0 else 31 - x
            wob = f
            for y in range(12 - wob, 22):
                for x in (7, 8, 9, 10):
                    cv.p[(X(x), y)] = BODY_PAL['S'] if x == 10 else BODY_PAL['s']
            # 주먹 (손끝이 모음 · 팔보다 한 칸 굵게)
            for y in range(7 - wob, 12 - wob):
                for x in range(6 + f, 12):
                    cv.p[(X(x), y)] = BODY_PAL['s'] if x < 10 else BODY_PAL['S']
            cv.p[(X(8 + f), 8 - wob)] = BODY_PAL['S']
            # 샅바 팔목 띠
            for x in (7, 8, 9, 10):
                cv.p[(X(x), 16)] = BODY_PAL['r'] if side == 0 else BODY_PAL['u']
        cv.outline(K1)
        fr.append(cv)
    return fx_out('fx_wrestler_arms', fr, 32, 32, ppu=8)


def fx_grabmark():               # 잡을 후보 발밑 고리 (영웅색 끊긴 마디 · 매듭 네 점)
    fr = []
    for f in range(2):
        cv = Cv()
        rot = f * 0.26
        for i in range(12):
            a0 = rot + TAU * i / 12 + 0.06
            for t in range(6):
                a = a0 + t * 0.045
                for rad, col in ((13.2, HERO), (12.2, HERO), (14.2, HERO_L)):
                    cv.set(16 + math.cos(a) * rad - 0.5, 16 + math.sin(a) * rad - 0.5, col)
        for k in range(4):
            a = rot * 0.5 + TAU * k / 4
            x, y = 16 + math.cos(a) * 13.2 - 0.5, 16 + math.sin(a) * 13.2 - 0.5
            cv.disc(x, y, 2.0 + f * 0.5, GOLD)
            cv.set(x, y, WHITE)
        fr.append(cv)
    return fx_out('fx_grabmark', fr, 32, 32)


def fx_throwtrail():             # 볼 뒤로 끌리는 모래 꼬리 (오른쪽 끝 = 볼 쪽)
    fr = []
    for f in range(3):
        cv = Cv()
        for i in range(7):
            x = 29 - i * 4.2 - f * 0.9
            r = max(1.0, 3.6 - i * 0.45)
            y = 8 + math.sin(i * 1.3 + f * 1.7) * 1.4
            cv.disc(x, y, r, SAND_D)
            cv.disc(x - 0.4, y - 0.6, r * 0.7, SAND)
            if i < 3:
                cv.disc(x - 0.8, y - 1.0, r * 0.35, SAND_L)
        fade(cv, 0.0)
        # 오른쪽은 진하고 왼쪽 끝은 디더로 흩어짐
        for (x, y) in list(cv.p):
            if x < 12 and BAYER[y % 4][x % 4] / 16.0 < (12 - x) / 14.0:
                del cv.p[(x, y)]
        fr.append(cv)
    return fx_out('fx_throwtrail', fr, 32, 16)


def fx_sandpuff():               # 착지 모래 튐 6프레임
    fr = []
    for f in range(6):
        cv = Cv()
        R = 3 + f * 2.4
        puff(cv, 16, 17, R, 8, max(1.5, 4.2 - f * 0.45), 11 + f)
        r = random.Random(40 + f)
        for _ in range(6 + f):
            a, d = r.random() * TAU, R + 2 + r.random() * 4
            cv.set(16 + math.cos(a) * d, 17 + math.sin(a) * d * 0.7 - f * 0.6, SAND_D if r.random() < 0.6 else SAND)
        fade(cv, max(0.0, (f - 2) * 0.22))
        fr.append(cv)
    return fx_out('fx_sandpuff', fr, 32, 32)


def rope_tones():
    return (214, 170, 100), (150, 104, 56), (74, 48, 36)


def fx_ropering():               # 모래판 새끼줄 테 (64칸 · 꼬임 무늬 · 둘레에 매듭)
    cv = Cv()
    lt, dk, ed = rope_tones()
    for y in range(64):
        for x in range(64):
            d = math.hypot(x + 0.5 - 32, y + 0.5 - 32)
            if 26.0 <= d <= 31.4:
                rr = d - 26.0
                a = math.atan2(y + 0.5 - 32, x + 0.5 - 32)
                if rr < 1.0 or rr > 4.4:
                    cv.p[(x, y)] = ed
                else:
                    k = (int(a / TAU * 60) + int(rr)) // 1
                    cv.p[(x, y)] = lt if (k // 2) % 2 == 0 else dk
    for k in range(4):                                  # 네 방향 매듭 (빨강 · 파랑 띠)
        a = TAU * k / 4
        cx, cy = 32 + math.cos(a) * 28.7, 32 + math.sin(a) * 28.7
        cv.disc(cx, cy, 3.0, ed)
        cv.disc(cx, cy, 2.0, (190, 40, 54) if k % 2 == 0 else HERO)
        cv.set(cx - 1, cy - 1, (242, 120, 110) if k % 2 == 0 else HERO_L)
    return fx_out('fx_ropering', [cv], 64, 64)


def fx_sandwave():               # 천하장사 충격파 고리 8프레임 (끊긴 마디 · 바깥 모래알)
    fr = []
    for f in range(8):
        cv = Cv()
        R = 7 + f * 3.4
        th = 4.4 - f * 0.42
        r = random.Random(70 + f)
        for y in range(64):
            for x in range(64):
                d = math.hypot(x + 0.5 - 32, y + 0.5 - 32)
                a = math.atan2(y + 0.5 - 32, x + 0.5 - 32)
                if R - th <= d <= R:
                    seg = int((a + math.pi) / TAU * 22 + f) % 22
                    if seg % 5 == 4:
                        continue
                    t = (d - (R - th)) / th
                    lit = -math.cos(a - 3.9)                    # 위 왼쪽이 밝게
                    cv.p[(x, y)] = SAND_L if t > 0.7 and lit > 0 else SAND if t > 0.35 else SAND_D
        for _ in range(10 + f * 2):
            a = r.random() * TAU
            d = R + 1.5 + r.random() * (2 + f * 0.6)
            cv.set(32 + math.cos(a) * d, 32 + math.sin(a) * d, SAND if r.random() < 0.5 else SAND_D)
        fade(cv, max(0.0, (f - 4) * 0.24))
        fr.append(cv)
    return fx_out('fx_sandwave', fr, 64, 64)


def fx_liftshadow():             # 보스 들썩 그림자 (검정 45% 납작 타원 · 가장자리 디더)
    cv = Cv()
    cv.ell(32, 16, 30, 11, (0, 0, 0, 115))
    for (x, y) in list(cv.p):
        e = ((x + 0.5 - 32) / 30) ** 2 + ((y + 0.5 - 16) / 11) ** 2
        if e > 0.8 and BAYER[y % 4][x % 4] / 16.0 < (e - 0.8) * 5:
            del cv.p[(x, y)]
    return fx_out('fx_liftshadow', [cv], 64, 32)


def star_burst(cv, cx, cy, n, long_, short_, w, c1, c2):
    for i in range(n):
        a = TAU * i / n + TAU / (2 * n) * 0
        ln = long_ if i % 2 == 0 else short_
        for t in range(int(ln * 2)):
            d = t / 2.0
            x, y = cx + math.cos(a) * d, cy + math.sin(a) * d
            ww = max(1, int(round(w * (1 - d / (ln + 1)))))
            for j in range(ww):
                cv.set(x + math.cos(a + 1.57) * (j - (ww - 1) / 2) - 0.5, y + math.sin(a + 1.57) * (j - (ww - 1) / 2) - 0.5, c1 if d < ln * 0.55 else c2)


def fx_strike():                 # 볼링 스트라이크 별 폭발 6프레임
    fr = []
    for f in range(6):
        cv = Cv()
        L = 5 + f * 3.0 if f < 4 else 14 - (f - 4) * 1
        star_burst(cv, 24, 24, 8, L, L * 0.55, 3, GOLD, GOLD_D)
        if f < 5:
            cv.disc(24, 24, max(2, 6 - f), WHITE)
            cv.disc(24, 24, max(1, 3.5 - f * 0.5), GOLD)
        # 바깥 모래 테
        for i in range(14):
            a = TAU * i / 14 + f * 0.3
            d = L + 3 + f * 0.8
            cv.set(24 + math.cos(a) * d, 24 + math.sin(a) * d, SAND if i % 2 else SAND_D)
        fade(cv, max(0.0, (f - 3) * 0.3))
        fr.append(cv)
    return fx_out('fx_strike', fr, 48, 48)


def fx_ringout():                # 장외: 먼지 구름 + 가운데 반짝 별
    fr = []
    for f in range(6):
        cv = Cv()
        puff(cv, 24, 26, 4 + f * 2.8, 9, max(1.8, 5 - f * 0.5), 90 + f)
        if f < 5:
            s = [3, 6, 7, 5, 3][f]
            star_burst(cv, 24, 19, 4, s, s * 0.5, 2, WHITE, GOLD)
            cv.disc(24, 19, 1.4, WHITE)
        fade(cv, max(0.0, (f - 2) * 0.24))
        fr.append(cv)
    return fx_out('fx_ringout', fr, 48, 48)


def fx_bullrun():                # 황소 달리기 4프레임 (오른쪽 향함)
    BR, BRL, BRD = (150, 100, 66), (190, 138, 90), (96, 60, 42)
    fr = []
    for f in range(4):
        cv = Cv()
        bob = (0, -1, 0, -1)[f]
        # 다리 (뒤 둘 · 앞 둘): 달리기 주기
        sw = [(-4, 3), (-1, 1), (3, -3), (1, -1)][f]
        for lx, k in ((9, sw[0]), (12, sw[1]), (23, sw[1]), (26, sw[0])):
            cv.line(lx, 15 + bob, lx + k * 0.8, 21, BRD, 2)
            cv.rect(int(lx + k * 0.8) - 1, 21, 3, 1, (60, 38, 30))
        cv.ell(18, 11 + bob, 11, 6.2, BR)
        cv.ell(17, 9 + bob, 9, 3.4, BRL)
        cv.ell(18, 15.2 + bob, 9.5, 1.8, BRD)
        cv.ell(29.5, 9 + bob, 4.6, 4.2, BR)
        cv.ell(33.5, 11 + bob, 2.8, 2.4, (228, 196, 150))
        cv.set(34, 11 + bob, (60, 38, 30)); cv.set(33, 11 + bob, (60, 38, 30))
        cv.set(30, 8 + bob, WHITE); cv.set(31, 8 + bob, K1)
        cv.line(28, 5 + bob, 27, 2 + bob, GOLD, 1); cv.line(31, 5 + bob, 33, 2 + bob, GOLD, 1); cv.set(34, 1 + bob, SAND_L)
        cv.line(7, 8 + bob, 3, 6 - (f % 2) * 2 + bob, BRD, 1)                    # 꼬리
        cv.rect(24, 6 + bob, 3, 2, (232, 120, 150))                              # 목에 꽃
        cv.outline(K1)
        for i in range(3):
            cv.set(1 - i + (f % 2), 19 + (i % 2) + bob * 0, SAND_D)             # 뒤로 튀는 모래
        fr.append(cv)
    return fx_out('fx_bullrun', fr, 40, 24)


def fx_liftstone():              # 떨어지는 들돌 4프레임: 높이 · 중간 · 쾅 · 먼지와 금
    ST = [(226, 230, 240), (168, 174, 190), (104, 110, 134), (58, 62, 84)]
    fr = []
    for f in range(4):
        cv = Cv()
        sh = [3, 6, 9, 11][f]
        cv.ell(16, 25, sh, sh * 0.38, (0, 0, 0, 115))
        if f < 3:
            y = [7, 14, 19][f]
            if f == 1:
                for k in range(3):
                    cv.line(10 + k * 6, y - 9, 10 + k * 6, y - 6, (200, 206, 224), 1)
            cv.disc(16, y, 6.5, ST[2])
            cv.disc(15.3, y - 0.6, 5.4, ST[1])
            cv.disc(13.6, y - 2.2, 2.4, ST[0])
            cv.rect(14, int(y - 6), 5, 1, ST[3])
            cv.outline(K1)
        if f >= 2:
            puff(cv, 16, 23, 4 + (f - 2) * 5, 8, 3.4 - (f - 2) * 0.7, 120 + f)
            if f == 2:
                for a in (0.5, 1.2, 1.9, 2.6):
                    cv.line(16, 24, 16 + math.cos(a) * 11, 24 + math.sin(a) * 4 + 2, K1, 1)
        fade(cv, 0.35 if f == 3 else 0)
        fr.append(cv)
    return fx_out('fx_liftstone', fr, 32, 32)


def fx_ropepull():               # 줄다리기 밧줄 (가로 32칸 마디 · 꼬임이 움직임)
    lt, dk, ed = rope_tones()
    fr = []
    for f in range(2):
        cv = Cv()
        for x in range(32):
            for y in range(1, 7):
                k = ((x + (y // 2) * 2 + f * 2) // 2) % 2
                cv.p[(x, y)] = lt if k == 0 else dk
        for x in range(32):
            cv.p[(x, 0)] = ed
            cv.p[(x, 7)] = ed
        fr.append(cv)
    return fx_out('fx_ropepull', fr, 32, 8)


def fx_cowbellwave():            # 워낭 소리 고리 4프레임 (끊긴 마디 + 작은 음표 점)
    fr = []
    for f in range(4):
        cv = Cv()
        for ring_i, R in enumerate((7 + f * 4.2, 3 + f * 3.4)):
            if R < 3 or (ring_i == 1 and f == 0):
                continue
            for i in range(60):
                a = TAU * i / 60
                if (i // 5 + ring_i + f) % 3 == 0:
                    continue
                for t in range(2 if ring_i == 0 else 1):
                    d = R - t
                    cv.set(24 + math.cos(a) * d - 0.5, 24 + math.sin(a) * d - 0.5, GOLD if t == 0 else GOLD_D)
        for k in range(4):
            a = TAU * k / 4 + 0.78
            d = 9 + f * 4.2
            x, y = 24 + math.cos(a) * d, 24 + math.sin(a) * d
            cv.disc(x, y, 1.5, WHITE)
        fade(cv, max(0.0, (f - 1) * 0.28))
        fr.append(cv)
    return fx_out('fx_cowbellwave', fr, 48, 48)


def fxs():
    return [fx_wrestler_arms(), fx_grabmark(), fx_throwtrail(), fx_sandpuff(), fx_ropering(), fx_sandwave(), fx_liftshadow(),
            fx_strike(), fx_ringout(), fx_bullrun(), fx_liftstone(), fx_ropepull(), fx_cowbellwave()]


# ================================================================== 4. 아이콘 (범주 틀 + 그림문자)
SK, SKS = (242, 188, 150), (202, 126, 112)
RED, REDD, REDL = (222, 60, 70), (150, 30, 50), (255, 130, 120)
BLU, BLUD, BLUL = (110, 128, 255), (58, 70, 200), (180, 192, 255)
STN, STND, STNL = (168, 174, 190), (98, 104, 130), (232, 236, 246)
BRN, BRND = (170, 112, 66), (104, 66, 44)
GRN, GRND = (140, 220, 140), (62, 140, 92)
BONE, BONED = (250, 244, 226), (206, 190, 156)
ROPE_L, ROPE_D = (222, 178, 104), (158, 110, 60)
EYE = (32, 22, 50)


def foe(cv, cx, cy, r, tilt=0):            # 적 하나 (둥근 몸 · 눈 둘)
    cv.disc(cx, cy, r, GRND)
    cv.disc(cx - 0.3, cy - 0.4, r - 0.9, GRN)
    if r >= 3:
        cv.set(cx - r * 0.45, cy - r * 0.15 + tilt, EYE); cv.set(cx + r * 0.45 - 1, cy - r * 0.15 + tilt, EYE)


def arrow_head(cv, x, y, dx, dy, c, n=3):
    px, py = -dy, dx
    for i in range(n):
        for s in range(-(n - 1 - i), n - i):
            cv.set(x - dx * i + px * s, y - dy * i + py * s, c)


def arc(cv, cx, cy, r, a0, a1, c, w=2):
    n = int(abs(a1 - a0) * r * 2) + 2
    for i in range(n + 1):
        a = a0 + (a1 - a0) * i / n
        for j in range(w):
            rr = r - j + (w - 1) / 2
            cv.set(cx + math.cos(a) * rr - 0.5, cy + math.sin(a) * rr - 0.5, c)


def pin(cv, x, y, ang, scale=1.0):         # 볼링 핀 (막대 · 목에 빨간 띠)
    L = 4 * scale
    dx, dy = math.cos(ang), math.sin(ang)
    cv.line(x - dx * L, y - dy * L, x + dx * L, y + dy * L, WHITE, 2)
    cv.set(x + dx * L * 0.35, y + dy * L * 0.35, RED)
    cv.set(x + dx * L * 0.35 + 1, y + dy * L * 0.35, RED)


def bull(cv, cx, cy, gold=False, k=1.0):
    body, bl, horn = ((236, 178, 60), (254, 222, 108), (255, 246, 214)) if gold else ((150, 98, 64), (196, 142, 92), (248, 232, 190))
    dk = (176, 112, 36) if gold else (100, 62, 44)
    for sgn in (-1, 1):                     # 뿔
        pts = [(0, -1), (3, -2), (5, -5), (6, -8)]
        for (a, b), (c, d) in zip(pts, pts[1:]):
            cv.line(cx + sgn * a * k - (1 if sgn < 0 else 0), cy + b * k, cx + sgn * c * k - (1 if sgn < 0 else 0), cy + d * k, horn, 2)
    cv.ell(cx, cy + 1, 6.6 * k, 6.0 * k, body)
    cv.ell(cx - 1, cy - 1, 4.6 * k, 3.4 * k, bl)
    for sgn in (-1, 1):                     # 귀
        cv.ell(cx + sgn * 7.4 * k, cy - 0.5, 2.0 * k, 1.3 * k, dk)
    cv.ell(cx, cy + 4.6 * k, 4.2 * k, 3.0 * k, (236, 206, 164))
    cv.set(cx - 2, cy + 4.6 * k, dk); cv.set(cx + 1, cy + 4.6 * k, dk)
    cv.set(cx - 4, cy - 0.5, EYE); cv.set(cx + 3, cy - 0.5, EYE)
    cv.set(cx - 4, cy - 1.5, WHITE); cv.set(cx + 3, cy - 1.5, WHITE)


def ringl(cv, cx, cy, rx, ry, w=2):        # 땅 위 새끼줄 테 (타원)
    for y in range(int(cy - ry - 3), int(cy + ry + 4)):
        for x in range(int(cx - rx - 3), int(cx + rx + 4)):
            d = math.hypot((x + 0.5 - cx) / rx, (y + 0.5 - cy) / ry)
            if 1 - w / (2.0 * min(rx, ry)) * 1.0 <= d <= 1 + w / (2.0 * min(rx, ry)) * 1.0:
                cv.p[(x, y)] = ROPE_L if ((x + y) // 2) % 2 == 0 else ROPE_D


def sand_fill(cv, cx, cy, rx, ry):
    cv.ell(cx, cy, rx, ry, SAND)
    for (x, y) in list(cv.p):
        pass


# ---- 특수 능력 540~546
def g540(cv):     # 들돌
    cv.line(23, 12, 27, 12, STNL); cv.line(24, 15, 28, 15, STNL)
    cv.disc(14.5, 17, 8.5, STND)
    cv.disc(14, 16.5, 7.4, STN)
    cv.disc(11.5, 13.5, 3.4, STNL)
    cv.rect(11, 8, 7, 2, STND)                   # 손잡이 홈
    cv.rect(12, 8, 5, 1, (60, 64, 86))
    cv.ell(15, 26, 8, 1.6, SAND_D)
    for x in (6, 22): cv.set(x, 25, SAND)


def g541(cv):     # 워낭
    for y in range(9, 22):
        half = 3 + (y - 9) * 0.55
        for x in range(int(16 - half), int(16 + half)):
            lit = x < 14 - (y - 9) * 0.1
            cv.p[(x, y)] = (254, 236, 150) if (lit and y < 15 and x > 12) else GOLD if x < 18 else GOLD_D
    cv.rect(8, 21, 16, 2, GOLD_D)
    cv.rect(9, 21, 14, 1, GOLD)
    cv.disc(16, 24.5, 2.2, BRND)
    cv.ring(16, 7, 1.8, 3.4, RED)
    cv.rect(15, 8, 2, 2, REDD)
    for sgn, x in ((-1, 5), (1, 26)):
        cv.line(x, 12, x, 15, WHITE); cv.line(x + sgn * 2, 10, x + sgn * 2, 17, WHITE)


def g542(cv):     # 밧줄 샅바 (감아 놓은 고리 + 풀린 끝)
    for y in range(32):
        for x in range(32):
            d = math.hypot(x + 0.5 - 14, y + 0.5 - 15)
            if 4.6 <= d <= 10.2:
                a = math.atan2(y + 0.5 - 15, x + 0.5 - 14)
                k = int(a / TAU * 22 + d * 0.9) % 2
                cv.p[(x, y)] = ROPE_L if k == 0 else ROPE_D
    for i in range(7):
        cv.rect(21 + i, 21 + i // 1, 3, 2, ROPE_L if i % 2 == 0 else ROPE_D)
    cv.rect(27, 28, 2, 1, ROPE_D); cv.set(28, 29, ROPE_L)
    cv.rect(11, 11, 2, 2, RED); cv.rect(15, 18, 2, 2, BLU)


def g543(cv):     # 통뼈
    cv.line(13, 19, 19, 13, BONE, 3)
    cv.line(14, 20, 20, 14, BONED, 1)
    for (x, y) in ((12, 20), (20, 12)):
        for sx, sy in ((-1.8, 1.8), (1.8, -1.8)):
            cv.disc(x + sx, y + sy, 2.3, BONE)
            cv.set(x + sx + 1, y + sy + 1, BONED); cv.set(x + sx + 1, y + sy, BONED)


def g544(cv):     # 모래 근성 (모래 더미 위로 솟는 쐐기)
    cv.poly([(10, 22), (16, 15), (22, 22)], SAND)
    cv.poly([(16, 15), (22, 22), (17, 22)], SAND_D)
    cv.line(10, 22, 22, 22, SAND_D)
    for y0 in (9, 12):
        for i in range(4):
            cv.set(16 - 1 - i, y0 + i, GOLD); cv.set(16 + i, y0 + i, GOLD)
        for i in range(4):
            cv.set(16 - 1 - i, y0 + i + 1, GOLD_D); cv.set(16 + i, y0 + i + 1, GOLD_D)


def g545(cv):     # 장사 밥상 (밥그릇 · 김)
    cv.ell(16, 16, 5.2, 4.0, BONE)
    cv.rect(11, 16, 11, 1, BONE)
    cv.ell(15.2, 15, 2.4, 1.6, (255, 255, 250))
    for y in range(17, 22):
        half = 6 - (y - 17) * 0.8
        for x in range(int(16 - half), int(16 + half)):
            cv.p[(x, y)] = RED if x < 18 else REDD
    cv.rect(11, 17, 11, 1, REDL)
    cv.rect(13, 21, 6, 1, REDD)
    for x in (13, 16, 19):
        cv.set(x, 9, WHITE); cv.set(x + (1 if x == 16 else -1), 11, WHITE); cv.set(x, 13, WHITE) if x != 16 else None


def g546(cv):     # 관중의 환호 (치켜든 두 팔)
    for sgn in (-1, 1):
        cv.line(16 + sgn * 1, 22, 16 + sgn * 4, 12, SK, 3)
        cv.line(16 + sgn * 2, 22, 16 + sgn * 5, 12, SKS, 1)
        cv.disc(16 + sgn * 4.5, 10.5, 3.2, SK)
        cv.disc(16 + sgn * 5, 11.2, 1.6, SKS)
        cv.rect(16 + sgn * 4 - (1 if sgn < 0 else 0), 18, 2, 2, RED if sgn < 0 else BLU)
    cv.set(16, 7, GOLD); cv.set(15, 7, GOLD); cv.rect(15, 5, 2, 2, GOLD)
    cv.set(9, 13, GOLD); cv.set(22, 13, GOLD)


# ---- 전용 카드 740~745
def g740(cv):     # 도미노 (공이 핀을 쓰러뜨림)
    cv.disc(8, 20.5, 5.0, STND); cv.disc(7.5, 20, 4.0, STN); cv.rect(5, 17, 2, 2, STNL)
    for bx, ang in ((14, 0.15), (19.5, 0.65), (24.5, 1.25)):
        top = (bx + math.sin(ang) * 11, 23 - math.cos(ang) * 11)
        base = (bx, 23)
        w = 2.4
        px, py = math.cos(ang) * w, math.sin(ang) * w
        cv.poly([(base[0] - px, base[1] - py), (base[0] + px, base[1] + py), (top[0] + px, top[1] + py), (top[0] - px, top[1] - py)], BLUL)
        cv.line(base[0] + px, base[1] + py, top[0] + px, top[1] + py, BLUD, 1)
    cv.line(10, 14, 13, 17, GOLD); cv.line(11, 13, 11, 17, GOLD)


def g741(cv):     # 한 아름 (두 적을 한 팔로)
    foe(cv, 11, 11, 5); foe(cv, 21, 11, 5)
    arc(cv, 16, 13, 10, 0.1, math.pi - 0.1, SK, 3)
    arc(cv, 16, 13, 8, 0.2, math.pi - 0.2, SKS, 1)
    cv.rect(5, 12, 3, 3, SK); cv.rect(24, 12, 3, 3, SK)
    cv.line(9, 24, 23, 24, RED, 2)


def g742(cv):     # 되치기 (맞은 자리에서 곧바로 잡아 돌림)
    arc(cv, 16, 16, 9, -0.4, 4.4, BLU, 3)
    arrow_head(cv, 16 + math.cos(4.4) * 9 - 1, 16 + math.sin(4.4) * 9 - 1, -1, 0, BLUL, 4)
    foe(cv, 16, 16, 4)
    cv.rect(10, 14, 3, 4, SK); cv.rect(19, 14, 3, 4, SK)
    star = [(16, 4), (16, 5), (15, 5), (17, 5)]
    for x, y in star: cv.set(x, y, GOLD)


def g743(cv):     # 모래 둑 (모래에 발이 빠진 적)
    foe(cv, 16, 12, 5.5)
    cv.poly([(4, 26), (4, 19), (9, 16), (23, 16), (28, 19), (28, 26)], SAND)
    cv.rect(4, 22, 24, 4, SAND_D)
    cv.line(7, 16, 25, 16, SAND_L, 1)
    for x in range(5, 28, 4):
        cv.rect(x, 18, 2, 1, ROPE_D)
    cv.line(9, 21, 23, 21, SAND_D, 1)


def g744(cv):     # 번쩍 들기 (무거운 추를 들어 올림)
    cv.ring(16, 9.5, 2.6, 4.6, STNL)
    cv.poly([(7, 24), (9, 13), (23, 13), (25, 24)], STND)
    cv.poly([(9, 13), (23, 13), (21, 16), (11, 16)], STN)
    cv.rect(12, 18, 8, 3, STNL)
    cv.rect(13, 19, 6, 1, STND)
    for x in (4, 27):
        for i in range(3):
            cv.set(x - i, 9 + i, GOLD); cv.set(x + 1 + i, 9 + i, GOLD)
        cv.rect(x, 12, 2, 4, GOLD_D)


def g745(cv):     # 발 걸기 (다리에 걸려 넘어지는 적)
    foe(cv, 20, 12, 5.5, 1)
    cv.line(23, 6, 27, 4, SAND_L); cv.line(24, 9, 28, 8, SAND_L)
    cv.line(5, 21, 22, 21, SK, 3)
    cv.line(5, 23, 22, 23, SKS, 1)
    cv.ell(24, 21.5, 3.2, 2.6, SK)
    cv.rect(10, 20, 3, 3, RED)
    cv.set(9, 9, GOLD); cv.set(8, 10, GOLD); cv.set(10, 10, GOLD); cv.set(9, 11, GOLD)


# ---- 고유 스킬 840~849
def g840(cv):     # 샅바 낚기 (올가미)
    ringl(cv, 14, 13, 8, 8, 3)
    foe(cv, 14, 13, 3.2)
    cv.line(19, 19, 25, 25, ROPE_L, 2)
    cv.line(20, 19, 26, 25, ROPE_D, 1)
    cv.rect(24, 24, 4, 4, SK); cv.rect(24, 24, 4, 1, BLU)
    cv.rect(19, 16, 3, 3, RED)


def g841(cv):     # 장외 패대기 (테 밖으로 날아가 모래가 터짐)
    ringl(cv, 13, 21, 10, 5.5, 2)
    foe(cv, 22, 10, 4.2, 1)
    cv.line(15, 14, 18, 11, SAND_L); cv.line(13, 11, 17, 8, SAND_L)
    star_burst(cv, 24, 21, 8, 5, 3, 2, SAND_L, SAND)
    cv.disc(24, 21, 1.5, WHITE)


def g842(cv):     # 배지기 (배로 받쳐 쿵)
    cv.disc(16, 18, 8.2, SKS)
    cv.disc(15.4, 17.5, 7.2, SK)
    cv.ell(13, 14.5, 3.2, 2.2, (255, 224, 196))
    cv.set(16, 19, SKS); cv.set(17, 19, SKS)
    cv.rect(8, 23, 16, 3, RED); cv.rect(8, 25, 16, 1, REDD); cv.rect(14, 23, 4, 3, BLU)
    for sgn in (-1, 0, 1):
        x = 16 + sgn * 9
        cv.line(x, 8 - (0 if sgn == 0 else -2), x + sgn * 2, 4 - (0 if sgn == 0 else -2), GOLD, 2)


def g843(cv):     # 황소 돌진
    bull(cv, 16, 16)
    for x, y in ((5, 25), (8, 27), (24, 27), (27, 25)):
        cv.rect(x, y, 2, 1, SAND_D)
    cv.line(4, 22, 7, 25, SAND); cv.line(28, 22, 25, 25, SAND)


def g844(cv):     # 모래판 다지기 (판이 더 오래)
    cv.ell(16, 20, 10, 5.4, SAND)
    cv.ell(16, 21.5, 8, 3.4, SAND_D)
    ringl(cv, 16, 20, 10.5, 5.8, 2)
    cv.rect(14, 6, 4, 12, GOLD); cv.rect(10, 10, 12, 4, GOLD)
    cv.rect(15, 7, 2, 10, WHITE); cv.rect(11, 11, 10, 2, WHITE)


def g845(cv):     # 끈기 (겹쳐진 방패)
    shield = lambda ox, oy, a, b: (cv.poly([(ox - 7, oy - 8), (ox + 7, oy - 8), (ox + 7, oy + 2), (ox, oy + 9), (ox - 7, oy + 2)], a),
                                   cv.poly([(ox - 5, oy - 6), (ox + 5, oy - 6), (ox + 5, oy + 1), (ox, oy + 6), (ox - 5, oy + 1)], b))
    shield(19, 12, BLUD, (82, 94, 226))
    shield(14, 17, BLU, BLUD)
    cv.rect(10, 12, 9, 2, BLUL); cv.rect(13, 12, 3, 10, BLUL)
    cv.rect(13, 14, 3, 1, RED)


def g846(cv):     # 기술 연결 (이어지는 두 고리)
    cv.ring(19, 19, 3.2, 6.6, GOLD); cv.ring(19, 19, 5.2, 6.6, GOLD_D)
    cv.ring(12.5, 12.5, 3.2, 6.6, BLU)
    cv.ring(12.5, 12.5, 5.2, 6.6, BLUD)
    for (x, y) in list(cv.p):
        pass
    cv.set(9, 8, BLUL); cv.set(10, 7, BLUL); cv.set(21, 15, (255, 240, 170))
    cv.rect(15, 15, 2, 2, GOLD)


def g847(cv):     # 거인 메치기 (큰 적이 거꾸로 뒤집혀 쿵)
    cv.line(4, 25, 28, 25, K1, 1)
    for a in (-0.5, 0.2, 0.7):
        cv.line(16, 26, 16 + math.cos(2.6 - a * 2) * 10, 28 + math.sin(a) * 3, STND, 1)
    cv.disc(16, 15, 8, GRND); cv.disc(15.4, 14.4, 7, GRN)
    cv.set(12, 18, EYE); cv.set(19, 18, EYE); cv.rect(14, 21, 5, 1, EYE)
    cv.line(10, 8, 8, 4, GRND, 2); cv.line(22, 8, 24, 4, GRND, 2)
    for x, y in ((4, 22), (27, 22), (6, 19), (25, 19)):
        cv.set(x, y, GOLD)


def g848(cv):     # 눈덩이 (핀을 쓰러뜨릴수록 커지는 공)
    for (x, y, r) in ((7, 22, 3.2), (13, 19, 5.0)):
        cv.disc(x, y, r, BLUL); cv.disc(x - 0.5, y - 0.6, r - 1, WHITE)
    cv.disc(21, 14, 8.2, BLUL)
    cv.disc(20.4, 13.4, 7.2, WHITE)
    cv.disc(17.6, 10.6, 2.6, (255, 255, 255))
    cv.set(23, 17, BLUL); cv.set(24, 15, BLUL); cv.set(20, 18, BLUL)
    for i in range(4):
        cv.set(3 + i * 2, 26 - i % 2, BLUL)


def g849(cv):     # 꽃가마
    cv.rect(3, 22, 26, 2, BRND); cv.rect(3, 22, 26, 1, BRN)
    cv.rect(9, 14, 14, 8, RED); cv.rect(9, 14, 14, 1, REDL); cv.rect(9, 21, 14, 1, REDD)
    cv.rect(13, 16, 6, 4, (255, 232, 170)); cv.rect(15, 16, 2, 4, REDD)
    cv.poly([(6, 13), (26, 13), (22, 8), (10, 8)], GOLD); cv.rect(6, 13, 20, 1, GOLD_D)
    cv.rect(5, 12, 2, 1, GOLD); cv.rect(25, 12, 2, 1, GOLD)
    cv.rect(14, 5, 4, 3, (255, 150, 190)); cv.set(15, 4, (255, 190, 214))
    cv.rect(5, 25, 2, 3, SK); cv.rect(25, 25, 2, 3, SK)


# ---- 스킬 진화 940~946 (육각)
def g940(cv):     # 스트라이크 장사
    star_burst(cv, 16, 16, 8, 11, 6.5, 3, GOLD, GOLD_D)
    cv.disc(16, 16, 5.2, STND); cv.disc(15.6, 15.6, 4.4, STN); cv.disc(14, 14, 1.8, STNL)
    for a, d in ((0.5, 10), (2.1, 10), (3.7, 10), (5.3, 10), (1.3, 7), (4.5, 7)):
        pin(cv, 16 + math.cos(a) * d, 16 + math.sin(a) * d, a, 0.8)


def g941(cv):     # 천하 도효
    cv.ell(16, 19, 11, 6.2, SAND)
    cv.ell(16, 21, 9, 3.8, SAND_D)
    ringl(cv, 16, 19, 11.5, 6.6, 2)
    for x, y in ((5, 19), (27, 19), (16, 13), (16, 25)):
        cv.rect(x - 1, y - 1, 2, 2, BLU)
    cv.poly([(16, 5), (18, 9), (22, 10), (18, 11), (16, 15), (14, 11), (10, 10), (14, 9)], GOLD)
    cv.set(16, 9, WHITE); cv.set(15, 9, WHITE)


def g942(cv):     # 철벽 샅바
    for r in range(4):
        y = 7 + r * 5
        cv.rect(5, y, 22, 4, STN); cv.rect(5, y + 3, 22, 1, STND); cv.rect(5, y, 22, 1, STNL)
        off = 0 if r % 2 == 0 else 3
        for x in range(5 + off, 28, 6):
            cv.rect(x, y, 1, 4, (60, 64, 88))
    cv.rect(3, 14, 26, 5, RED); cv.rect(3, 16, 26, 3, BLU)
    for x in range(3, 29, 4):
        cv.rect(x, 14, 2, 2, REDL); cv.rect(x + 2, 17, 2, 2, BLUL)
    cv.rect(3, 14, 26, 1, REDL); cv.rect(3, 18, 26, 1, BLUD)


def g943(cv):     # 천지 메치기 (둘이 엇갈려 메쳐짐)
    cv.line(8, 9, 24, 23, BLU, 3); cv.line(24, 9, 8, 23, GOLD, 3)
    arrow_head(cv, 25, 24, 1, 1, BLUL, 4); arrow_head(cv, 7, 24, -1, 1, (255, 240, 170), 4)
    foe(cv, 7, 7, 3.4); foe(cv, 25, 7, 3.4)
    cv.line(11, 27, 21, 27, SAND, 1); cv.set(8, 26, SAND_D); cv.set(24, 26, SAND_D)


def g944(cv):     # 황금 황소
    bull(cv, 16, 17, gold=True)
    cv.set(16, 4, WHITE); cv.rect(15, 5, 2, 2, GOLD); cv.set(10, 6, GOLD); cv.set(22, 6, GOLD)


def g945(cv):     # 장외 스트라이크
    cv.ell(11, 19, 9, 6, SAND); cv.ell(11, 20.5, 7, 3.6, SAND_D)
    ringl(cv, 11, 19, 9.5, 6.4, 2)
    star_burst(cv, 20, 14, 8, 6, 3.5, 2, GOLD, GOLD_D)
    for (x, y, a) in ((26, 8, 0.7), (27, 15, 1.2), (24, 23, 2.0)):
        pin(cv, x, y, a, 0.9)
    cv.disc(20, 14, 2, WHITE)


def g946(cv):     # 황소 장사 (방패 위의 금빛 황소)
    cv.poly([(6, 12), (26, 12), (26, 21), (16, 28), (6, 21)], BLUD)
    cv.poly([(8, 14), (24, 14), (24, 20), (16, 25), (8, 20)], BLU)
    cv.rect(7, 22, 18, 1, BLUL) if False else None
    bull(cv, 16, 12, gold=True, k=0.85)
    cv.rect(13, 22, 6, 2, RED)


# ---- 무기 강화 612~614
def g612(cv):     # 안다리 (한 다리가 안쪽을 걸어 올림)
    cv.line(13, 6, 13, 21, SK, 4); cv.line(15, 6, 15, 21, SKS, 1)
    cv.ell(12.5, 23, 4.8, 2.2, SK)
    cv.rect(10, 12, 6, 2, RED)
    cv.line(25, 7, 21, 15, SKS, 4)
    cv.line(21, 15, 11, 19, SKS, 3)
    cv.ell(9.5, 19, 2.6, 2.2, SKS)
    cv.rect(21, 11, 4, 2, BLU)
    cv.set(26, 18, GOLD); cv.set(25, 19, GOLD); cv.set(27, 19, GOLD); cv.set(26, 20, GOLD)


def g613(cv):     # 뒤집기 (둥글게 뒤집어 메침)
    arc(cv, 16, 16, 9.5, 0.3, math.pi - 0.3, BLU, 3)
    arc(cv, 16, 16, 9.5, math.pi + 0.3, TAU - 0.3, BLU, 3)
    arrow_head(cv, 16 + math.cos(math.pi - 0.3) * 9.5 - 1, 16 + math.sin(math.pi - 0.3) * 9.5, -0.3, -1, BLUL, 4)
    arrow_head(cv, 16 + math.cos(TAU - 0.3) * 9.5, 16 + math.sin(TAU - 0.3) * 9.5 - 1, 0.3, 1, BLUL, 4)
    foe(cv, 16, 16, 4.6, 1)


def g614(cv):     # 모래 바람 (한 줄기로 쓸고 지나감)
    for k, (y0, ph) in enumerate(((10, 0.0), (16, 1.6), (22, 3.1))):
        for x in range(4, 26):
            y = y0 + math.sin(x * 0.45 + ph) * 1.8
            cv.set(x, y, SAND_L); cv.set(x, y + 1, SAND); cv.set(x, y + 2, SAND_D)
        a0 = 0
        for t in range(10):
            a = -1.2 + t * 0.55
            cv.set(25 + math.cos(a) * 2.6 + 0.5, y0 + 1 + math.sin(a) * 2.6 + 2.5, SAND if t % 2 else SAND_L)
    for x, y in ((8, 6), (14, 4), (20, 7), (24, 27), (10, 27), (5, 14)):
        cv.set(x, y, SAND_D)


ICONS = [
    (540, 'square', 'weapon', g540), (541, 'square', 'weapon', g541), (542, 'square', 'weapon', g542),
    (543, 'diamond', 'survival', g543), (544, 'diamond', 'survival', g544), (545, 'diamond', 'survival', g545), (546, 'diamond', 'survival', g546),
    (740, 'square', 'weapon', g740), (741, 'square', 'weapon', g741), (742, 'square', 'weapon', g742), (743, 'square', 'weapon', g743),
    (744, 'circle', 'skill', g744), (745, 'circle', 'skill', g745),
    (840, 'circle', 'skill', g840), (841, 'circle', 'skill', g841), (842, 'circle', 'skill', g842), (843, 'circle', 'skill', g843),
    (844, 'circle', 'skill', g844), (845, 'circle', 'skill', g845), (846, 'circle', 'skill', g846), (847, 'circle', 'skill', g847),
    (848, 'circle', 'skill', g848), (849, 'circle', 'skill', g849),
    (940, 'hex', 'soul', g940), (941, 'hex', 'soul', g941), (942, 'hex', 'soul', g942), (943, 'hex', 'soul', g943),
    (944, 'hex', 'soul', g944), (945, 'hex', 'soul', g945), (946, 'hex', 'soul', g946),
    (612, 'square', 'weapon', g612), (613, 'square', 'weapon', g613), (614, 'square', 'weapon', g614),
]


def icon_image(shape, cat, glyph):
    px = FI.frame(shape, cat)
    cv = Cv()
    glyph(cv)
    g = {p: c[:3] for p, c in cv.p.items() if p in px}
    band = 3.6 if shape == 'diamond' else 3.0
    for (x, y) in list(g):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            q = (x + dx, y + dy)
            if q not in g and q in px and FI.depth(shape, *q) > band:
                px[q] = K1
    px.update(g)
    im = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    for (x, y), c in px.items():
        im.putpixel((x, y), tuple(c[:3]) + (255,))
    return im


def icons():
    out = []
    tmpl = os.path.join(ICON, 'ability_100.png.meta')
    for num, shape, cat, fn in ICONS:
        im = icon_image(shape, cat, fn)
        path = os.path.join(ICON, 'ability_%d.png' % num)
        save_png(im, path)
        meta_copy(tmpl, path, 'Icons/ability_%d' % num)
        out.append((num, im))
    return out


# ================================================================== 미리보기
def preview(folder, body, weps, icns):
    os.makedirs(folder, exist_ok=True)
    bg = (60, 58, 76, 255)
    # 몸 + 무기 + FX
    sc = 6
    S = Image.new('RGBA', (192 * sc, 32 * sc + 40), bg)
    b = Image.new('RGBA', (192, 32), bg); b.alpha_composite(body)
    S.paste(b.resize((192 * sc, 32 * sc), Image.NEAREST), (0, 0))
    S.save(os.path.join(folder, 'body.png'))
    # 아이콘
    cols = 11
    I = Image.new('RGBA', (cols * 100, ((len(icns) + cols - 1) // cols) * 100), (30, 24, 44, 255))
    for k, (n, im) in enumerate(icns):
        I.alpha_composite(im.resize((96, 96), Image.NEAREST), ((k % cols) * 100 + 2, (k // cols) * 100 + 2))
    I.save(os.path.join(folder, 'icons.png'))
    # 무기 · FX
    items = []
    for nm in ('weapon_satba', 'weapon_kit_140', 'weapon_kit_141', 'weapon_kit_142'):
        items.append((Image.open(os.path.join(WEAP, nm + '.png')).convert('RGBA'), 10))
    fxn = ['fx_wrestler_arms', 'fx_grabmark', 'fx_throwtrail', 'fx_sandpuff', 'fx_ropering', 'fx_sandwave', 'fx_liftshadow', 'fx_strike',
           'fx_ringout', 'fx_bullrun', 'fx_liftstone', 'fx_ropepull', 'fx_cowbellwave']
    for nm in fxn:
        items.append((Image.open(os.path.join(FXD, nm + '.png')).convert('RGBA'), 3 if nm in ('fx_sandwave', 'fx_ropering') else 4))
    W = 1500
    x = y = 0; rowh = 0
    pos = []
    for im, s in items:
        w, h = im.width * s, im.height * s
        if x + w > W:
            x, y, rowh = 0, y + rowh + 8, 0
        pos.append((im, s, x, y)); x += w + 8; rowh = max(rowh, h)
    F = Image.new('RGBA', (W, y + rowh + 8), (70, 66, 86, 255))
    for im, s, x, y in pos:
        F.alpha_composite(im.resize((im.width * s, im.height * s), Image.NEAREST), (x, y))
    F.save(os.path.join(folder, 'fx.png'))


def main():
    sheet = body_sheet()
    weps = weapons()
    for w in weps:
        print('%-18s %2d×%-2d' % w)
    fxs()
    icns = icons()
    print('아이콘', len(icns), '장')
    if len(sys.argv) > 1:
        preview(sys.argv[1], sheet, weps, icns)


if __name__ == '__main__':
    main()
