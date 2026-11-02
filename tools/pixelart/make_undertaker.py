# 새 영웅 장의사 「석관을 묻은 자」 그림 일체 (docs/plans/2026-11-02-big-update/hero-undertaker.md 13절)
#   Characters/undertaker.png          32칸 몸 시트 (192×32 · idle 2 + run 4 · 실크햇이 9영웅 중 가장 큼)
#   Weapons/weapon_shovel.png          손에 든 삽 (긴 나무 자루 + 은빛 삽날 · 손잡이가 왼쪽 · 코드가 어깨 위 대각선으로 듦)
#   Weapons/weapon_kit_161~163.png     전용 무기 까마귀 · 상여 종 · 석관 (기획서 이름 weapon_crow · bell · sarcophagus 도 같은 그림으로)
#                                      두 컷이 필요한 까마귀(날개) · 종(흔들림)은 weapon_crow_2 · weapon_bell_2 가 두 번째 컷
#   FX/fx_*.png · FX/prop_grave*.png   구멍 · 흙 · 매장 · 묘비 · 흙담 · 땅굴 · 유령 손 · 관 · 종 파동 · 도랑 · 함정 · 종 매달림 · 까마귀
#                                      (묘비는 기존 무한 지형 묘비 Abyss/prop_grave 와 폴더로 구분 — 그쪽은 보라 돌 + 십자, 이쪽은 청회색 은 돌 + 비문 줄)
#   Icons/ability_<번호>.png           특수 561~567 · 전용 카드 770~775 · 고유 스킬 870~879 · 스킬 진화 970~976 · 강화 621~623
#   범주 틀은 frame_icons.py 의 틀을 그대로 가져옴 (사각 무기 · 원 스킬 · 마름모 패시브 · 육각 진화)
#   .meta 는 이미 있으면 그대로 둠 (다시 돌려도 guid 유지) · 새로 만들 때 guid 는 이름에서 정해짐
# 실행: py -3.14 -X utf8 tools/pixelart/make_undertaker.py [미리보기 폴더]
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
    return uuid.uuid5(uuid.NAMESPACE_URL, 'soulsaver/undertaker/' + key).hex


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


# ------------------------------------------------------------------ 색
SIL, SILD, SILL = (185, 203, 228), (118, 138, 172), (238, 244, 252)       # 영웅색 은 · 어두움 · 밝음
STN, STND, STNL = (150, 162, 184), (90, 100, 128), (208, 218, 234)        # 묘비 돌
SOIL, SOILD, SOILL, SOILX = (140, 104, 76), (92, 66, 54), (188, 148, 106), (56, 40, 44)
WOOD, WOODD, WOODL = (160, 108, 66), (100, 66, 46), (206, 156, 100)
GOLD, GOLD_D, WHITE = (254, 222, 108), (200, 140, 40), (255, 250, 232)
BRZ, BRZD, BRZL = (206, 150, 66), (140, 92, 44), (250, 214, 130)          # 청동 종
COAL, COALD, COALL = (84, 88, 120), (54, 56, 80), (136, 144, 180)          # 검정 옷 (밝은 쪽)
FACE, FACED = (204, 212, 222), (146, 156, 178)
GRN, GRND = (140, 220, 140), (62, 140, 92)
EYE = (32, 22, 50)
ALT = (52, 30, 56)
PETAL, PETALD = (248, 232, 238), (206, 170, 190)
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def fade(cv, k):
    for (x, y) in list(cv.p):
        if BAYER[y % 4][x % 4] / 16.0 < k:
            del cv.p[(x, y)]


# ================================================================== 1. 몸 시트
BODY_PAL = {
    'C': COAL, 'D': COALL, 'c': COALD, 'T': (64, 66, 94), 'Q': (34, 30, 52), 'q': (110, 116, 148),
    'f': FACE, 'F': FACED, 'e': EYE, 'w': (248, 250, 255), 'W': (186, 196, 216),
    'b': SIL, 'B': SILL, 'g': FACE, 'd': (136, 98, 74),
}
HAT = [
    (0, 4, 'DDDDDc'), (1, 4, 'DCCCCc'), (2, 4, 'DCCCCc'), (3, 4, 'DCCCCc'),
    (4, 4, 'BbbbbB'),                                   # 모자 띠 은빛 1줄
    (5, 4, 'DCCCCc'),
    (6, 2, 'DDDDDDDDDc'),                               # 챙
    (7, 3, 'cccccccc'),
]
FACE_ROWS = [(8, 4, 'ffffff'), (9, 4, 'feffeF'), (10, 4, 'fffFFF'), (11, 5, 'FFFF')]
TORSO = [
    (12, 4, 'cCwwCc'),
    (13, 2, 'DDDwwwwDDc'),
    (14, 2, 'DDCCwcCCcc'),
]


def body_cells(head_dy=0, legs='stand', arm_l='down', arm_r='down', fl=0, dust=()):
    c = {}

    def put(rows, dy=0):
        for y, x0, text in rows:
            for i, ch in enumerate(text):
                if ch != '.':
                    c[(x0 + i, y + dy)] = BODY_PAL.get(ch, ch)

    def fill(y, x0, x1, left='C', mid='C', right='c'):
        for x in range(x0, x1 + 1):
            c[(x, y)] = BODY_PAL[left if x == x0 else right if x == x1 else mid]

    # 몸통 r15~19 : 소매 · 앞섶(단추 한 줄)
    for y in range(15, 20):
        for i, ch in enumerate('DCcCcCCcCc'):
            c[(2 + i, y)] = BODY_PAL[ch]
        c[(6, y)] = BODY_PAL['c']
        if y in (16, 18):
            c[(7, y)] = BODY_PAL['b']
    # 치마(상복 자락) r20~23 : 무릎 아래까지, 뛸 때 뒤로 갈라짐
    for y, (x0, x1) in zip(range(20, 24), ((3, 10), (2, 11), (2 - fl, 11 + (fl > 0)), (1 - fl, 12 + (fl > 0)))):
        fill(y, x0, x1, 'D', 'C', 'c')
    for x in range(1 - fl, 14):
        if (x, 23) in c:
            c[(x, 23)] = BODY_PAL['c']
    if fl:                                   # 뒤로 날리는 자락의 밝은 가장자리
        c[(2 - fl, 22)] = BODY_PAL['D']
        c[(1 - fl, 23)] = BODY_PAL['C']
    if legs in ('a', 'c'):                   # 자락이 갈라져 다리가 보임
        for y in (22, 23):
            for x in (6, 7):
                c.pop((x, y), None)
        c[(6, 21)] = BODY_PAL['c']; c[(7, 21)] = BODY_PAL['c']
    # 다리 r24~27
    LEGS = {
        'stand': [(24, 4, 'TT..TT'), (25, 4, 'TT..TT'), (26, 4, 'TT..TT'), (27, 3, 'QQq.qQQ')],
        'a': [(24, 4, 'TT..TT'), (25, 3, 'TT'), (26, 2, 'QQq'), (25, 9, 'TT'), (26, 9, 'TT'), (27, 9, 'qQQ')],
        'b': [(24, 5, 'TT.TT'), (25, 5, 'TT.TT'), (26, 5, 'TT.TT'), (27, 4, 'QQq.qQQ')],
        'c': [(24, 4, 'TT..TT'), (25, 9, 'TT'), (26, 10, 'qQQ'), (25, 3, 'TT'), (26, 3, 'TT'), (27, 2, 'QQq')],
    }
    put(LEGS[legs])
    # 팔 : 아래 = 옆구리 · 앞 = 팔을 들어 올림
    for side, mode in ((2, arm_l), (10, arm_r)):
        sx = (2, 3) if side == 2 else (10, 11)
        if mode == 'up':
            for y in range(18, 22):
                for x in sx:
                    c.pop((x, y), None)
            for x in sx:
                c[(x, 17)] = BODY_PAL['C']
                c[(x, 18)] = BODY_PAL['g']
                c[(x, 19)] = BODY_PAL['F']
        else:
            for x in sx:
                c[(x, 20)] = BODY_PAL['C'] if side == 2 else BODY_PAL['c']
                c[(x, 21)] = BODY_PAL['g']
                c[(x, 22)] = BODY_PAL['F']
    # 어깨 · 깃
    put(TORSO)
    put(HAT, head_dy)
    put(FACE_ROWS, head_dy)
    return c, dust


def body_frame(cells, dust):
    ox, oy = 9, 3
    cv = Cv()
    for (x, y), col in cells.items():
        cv.p[(x + ox, y + oy)] = col
    SK = {BODY_PAL['D'], BODY_PAL['f'], BODY_PAL['w'], BODY_PAL['B'], BODY_PAL['g']}
    cv.outline(lambda x, y: ALT if cv.p.get((x, y)) in SK and (x + y) % 2 == 0 else K1)
    for (x, y) in dust:
        cv.p[(x + ox, y + oy)] = (176, 140, 104)
    return cv.image(32, 32)


def body_sheet():
    frames = [
        body_cells(0, 'stand'),
        body_cells(1, 'stand'),
        body_cells(1, 'a', 'up', 'down', fl=1, dust=[(0, 27), (-1, 26)]),
        body_cells(0, 'b', 'down', 'up', fl=0),
        body_cells(1, 'c', 'down', 'up', fl=1, dust=[(13, 27), (14, 26)]),
        body_cells(0, 'b', 'up', 'down', fl=0),
    ]
    sheet = Image.new('RGBA', (192, 32), (0, 0, 0, 0))
    for i, (c, d) in enumerate(frames):
        sheet.paste(body_frame(c, d), (i * 32, 0))
    out = os.path.join(CHARS, 'undertaker.png')
    save_png(sheet, out)
    meta_copy(os.path.join(CHARS, 'cartographer.png.meta'), out, 'Characters/undertaker', sprite_names=('cartographer', 'undertaker'))
    return sheet


# ================================================================== 2. 손에 든 것 · 전용 무기
def weapon_out(name, pic, pivot_cell, template_base='sword', also=()):
    pic.outline()
    im, x0, y0 = pic.image()
    px = (pivot_cell[0] - x0 + 0.5) / im.w
    py = 1.0 - (pivot_cell[1] - y0 + 0.5) / im.h
    for nm in (name,) + tuple(also):
        path = os.path.join(WEAP, nm + '.png')
        KW.png.write(im, path)
        KW.write_meta(path, template_base, (round(px, 3), round(py, 3)))
        _fix_guid(path + '.meta', 'Weapons/' + nm)
    return im.w, im.h


def _fix_guid(meta, key):
    # 새로 만든 meta 의 guid 를 이름에서 정함 (처음 만든 직후에만 · 이미 이름 guid 면 그대로)
    t = open(meta, encoding='utf-8').read()
    g = guid_of(key)
    if g in t:
        return
    marker = meta + '.new'
    # write_meta 가 새로 만든 파일인지 알기 위해 호출 전 존재 여부를 따로 봄 → 여기서는 항상 바꾸면 기존 영웅 것도 바뀌므로 이름이 undertaker 계열만
    t = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + g, t, count=1, flags=re.M)
    open(meta, 'w', encoding='utf-8', newline='\n').write(t)


def pic_from(rows, pal):
    p = KW.Pic()
    n = len(rows[0])
    for y, row in enumerate(rows):
        assert len(row) == n, (row, len(row), n)
        for x, ch in enumerate(row):
            if ch != '.':
                p.dot(x, y, KW.hx(pal[ch]) if isinstance(pal[ch], str) else tuple(pal[ch][:3]) + (255,))
    return p


WP = {
    'W': '#8c5634', 'L': '#b47c4a', 'l': '#d0b278',                     # 나무
    '6': '#fafcff', '5': '#d6e4f6', '4': '#b9cbe4', '3': '#8a9fc0', '2': '#5d6c8c',   # 은빛 삽날 (영웅색)
    'k': '#2a2a40', 'K': '#44465e', 'j': '#6a6e92',                      # 까마귀
    'y': '#fede6c', 'Y': '#fffad2', 'g': '#b46826', 'G': '#eaaa3a',      # 금 · 부리
    'c': '#d0a060', 'C': '#fada82', 'h': '#8c5a2c', 'z': '#6a4a30',      # 청동 종
    's': '#9ea6bc', 'S': '#c8d0e4', 'x': '#5a627c', 'X': '#7a84a0',      # 석관
}


def w_shovel():             # 삽 — 긴 나무 자루(왼쪽에 D 손잡이) + 은빛 삽날 (오른쪽)
    rows = [
        '......................44435.',
        '..W...................444455',
        '.WL.....................6455',
        'WLllllllllllllllllllllll6554',
        'WWWWWWWWWWWWWWWWWWWWWWWW6544',
        '.WL......................444',
        '..W......................33.',
    ]
    # 위 그림은 칸 수 맞추기 어렵게 보여서 직접 칸으로 찍음
    p = KW.Pic()
    H = lambda x, y, ch: p.dot(x, y, KW.hx(WP[ch]))
    for x in range(2, 21):                      # 자루 2칸
        H(x, 3, 'l' if x % 7 else 'L')
        H(x, 4, 'W')
    for y in (2, 3, 4, 5):                      # D 손잡이
        H(0, y, 'L' if y in (3, 4) else 'W')
    H(1, 2, 'W'); H(1, 5, 'W'); H(1, 3, 'l'); H(1, 4, 'L')
    H(21, 3, '2'); H(21, 4, '2')                # 쇠 목
    # 삽날 (오른쪽을 향한 둥근 세모꼴 · 위쪽 밝은 가장자리)
    blade = {
        1: (22, 24), 2: (22, 26), 3: (22, 27), 4: (22, 27), 5: (22, 26), 6: (22, 24),
    }
    for y, (a, b) in blade.items():
        for x in range(a, b + 1):
            ch = '5' if x < b - 1 else '4'
            if y <= 2: ch = '6' if x < b else '5'
            elif y >= 5: ch = '3' if x < b else '2'
            H(x, y, ch)
    for x in range(22, 28):
        H(x, 3, '4' if x < 26 else '3')
    H(27, 3, '4'); H(27, 4, '3')
    return p, (1, 3.5)


def w_crow(wing='up'):     # 까마귀 — 검은 몸 · 노란 부리 (오른쪽) · 날개 2컷
    rows_up = [
        '...kK......',
        '..kKKk.....',
        '.kKKjkkk...',
        'kkkkkkkkkkK',
        '.kkkjkkkkYy',
        '..kkkkkkk..',
        '...k.k.....',
    ]
    rows_dn = [
        '...........',
        '...........',
        '....kkkk...',
        'kkkkkkkkkkK',
        '.kkjjkkkkYy',
        '..kkkkkkk..',
        '..kKKKKk...',
    ]
    p = pic_from(rows_up if wing == 'up' else rows_dn, {**{k: WP[k] for k in WP}})
    return p, (0, 3)


def w_bell(swing=0):        # 상여 종 — 청동 종 (입구 오른쪽) · 흔들림 2컷
    p = KW.Pic()
    H = lambda x, y, ch: p.dot(x, y, KW.hx(WP[ch]))
    hs = [3, 5, 7, 8, 9, 9, 10, 10]
    for i, h in enumerate(hs):
        x = 3 + i
        top = 5 - h // 2 + (swing if i > 4 else 0)
        for j in range(h):
            y = top + j
            ch = 'C' if j == 0 else 'c' if j < h * 0.35 else 'h' if j < h * 0.8 else 'z'
            H(x, y, ch)
    for x in range(0, 3):
        H(x, 5, 'z')
    H(0, 4, 'h'); H(0, 6, 'h')
    H(11, 5 + swing, 'C'); H(11, 6 + swing, 'z')            # 추
    return p, (0, 5)


def w_sarcophagus():        # 석관 — 회색 돌 육각 + 금 뚜껑 줄
    rows = [
        '..SSSSSSSSSS..',
        '.SSssssssssXS.',
        'SSsssGGGGsssXX',
        'SssssGyyGsssxX',
        'SSsssGGGGsssxX',
        '.SsssssssssxX.',
        '..xxxxxxxxxx..',
    ]
    return pic_from(rows, WP), (0, 3)


def weapons():
    out = []
    out.append(('weapon_shovel',) + weapon_out('weapon_shovel', *w_shovel()))
    for kid, nm, fn in ((161, 'crow', lambda: w_crow('up')), (162, 'bell', lambda: w_bell(0)), (163, 'sarcophagus', w_sarcophagus)):
        out.append(('weapon_kit_%d' % kid,) + weapon_out('weapon_kit_%d' % kid, *fn(), also=('weapon_' + nm,)))
    out.append(('weapon_crow_2',) + weapon_out('weapon_crow_2', *w_crow('dn')))
    out.append(('weapon_bell_2',) + weapon_out('weapon_bell_2', *w_bell(1)))
    return out


# ================================================================== 3. 이펙트 (FX)
def fx_out(name, frames, w, h, ppu=16, pivot=None):
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, cv in enumerate(frames):
        sheet.paste(cv.image(w, h), (i * w, 0))
    out = os.path.join(FXD, name + '.png')
    save_png(sheet, out)
    if not os.path.exists(out + '.meta'):
        sheet_meta(name, w, h, len(frames), out + '.meta')
        t = open(out + '.meta', encoding='utf-8').read()
        t = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, t, count=1)
        t = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + guid_of('FX/' + name), t, count=1, flags=re.M)
        if pivot is not None:
            t = t.replace('pivot: {x: 0.5, y: 0.5}\n      border', 'pivot: {x: %.3f, y: %.3f}\n      border' % pivot)
        open(out + '.meta', 'w', encoding='utf-8', newline='\n').write(t)
    return sheet


def soil_dots(cv, cx, cy, R, n, seed, cols=(SOIL, SOILD, SOILL), rr=(0.6, 1.4)):
    r = random.Random(seed)
    for _ in range(n):
        a = r.random() * TAU
        d = R * math.sqrt(r.random())
        cv.disc(cx + math.cos(a) * d, cy + math.sin(a) * d, rr[0] + r.random() * (rr[1] - rr[0]), r.choice(cols))


def fx_hole():                  # 구멍 바닥 (64칸 · 위에서 본 둥근 구멍 · 안쪽 벽이 위가 밝고 아래로 어두워짐)
    cv = Cv()
    r = random.Random(7)
    for y in range(64):
        for x in range(64):
            dx, dy = x + 0.5 - 32, y + 0.5 - 32
            d = math.hypot(dx, dy)
            wob = 1.2 * math.sin(math.atan2(dy, dx) * 5) + 0.8 * math.sin(math.atan2(dy, dx) * 9 + 1)
            if d > 31 + wob:
                continue
            e = (31 + wob - d)
            if e < 1.2:
                cv.p[(x, y)] = SOILX
            elif e < 3.6:
                cv.p[(x, y)] = SOILD if dy > -6 else (84, 62, 56)               # 위쪽 안쪽 벽이 조금 밝게
            else:
                k = d / 31
                lit = -dy / 31 * 0.6 - dx / 31 * 0.25
                t = k * 0.9 + (0.5 - lit) * 0.4
                th = BAYER[y % 4][x % 4] / 16.0
                cv.p[(x, y)] = (46, 34, 46) if t + (th - 0.5) * 0.35 > 0.62 else (26, 18, 34) if t + (th - 0.5) * 0.35 > 0.34 else (14, 10, 22)
    for (x, y) in list(cv.p):
        if cv.p[(x, y)] in ((14, 10, 22),) and r.random() < 0.02:
            cv.p[(x, y)] = (34, 24, 40)
    return fx_out('fx_hole', [cv], 64, 64)


def fx_hole_rim():              # 앞 가장자리 (아래 반쪽 호 · 적 앞에 그려 하반신을 가림)
    cv = Cv()
    for y in range(64):
        for x in range(64):
            dx, dy = x + 0.5 - 32, y + 0.5 - 32
            d = math.hypot(dx, dy)
            a = math.atan2(dy, dx)
            if not (0.12 < a < math.pi - 0.12):
                continue
            wob = 1.2 * math.sin(a * 5) + 0.8 * math.sin(a * 9 + 1)
            lip = 31 + wob
            sin_a = math.sin(a)
            width = 8.5 * min(1.0, (min(a, math.pi - a) - 0.12) / 0.45 + 0.15) * (0.5 + 0.5 * sin_a)
            if lip - width <= d <= lip + 2.2:
                t = (d - (lip - width)) / (width + 2.2)
                cv.p[(x, y)] = SOILL if t < 0.28 else SOIL if t < 0.62 else SOILD if t < 0.88 else SOILX
    soil_dots(cv, 32, 32, 36, 0, 1)
    r = random.Random(3)
    for _ in range(22):                          # 흩어진 흙덩이
        a = 0.2 + r.random() * (math.pi - 0.4)
        d = 31 + r.random() * 3.5
        cv.disc(32 + math.cos(a) * d, 32 + math.sin(a) * d, 0.8 + r.random() * 1.0, r.choice((SOIL, SOILL, SOILD)))
    return fx_out('fx_hole_rim', [cv], 64, 64)


def fx_dirt():                  # 흙 튀김 4컷
    fr = []
    for f in range(4):
        cv = Cv()
        r = random.Random(30 + f)
        n = 12
        for i in range(n):
            a = TAU * i / n + r.random() * 0.3
            sp = 4 + f * 3.2 + r.random() * 3
            x, y = 16 + math.cos(a) * sp, 18 + math.sin(a) * sp * 0.7 - (3 - abs(f - 1.5)) * 2.2 + f * 1.2
            rr = max(0.9, 2.4 - f * 0.4 + r.random() * 0.6)
            cv.disc(x, y, rr, SOILD)
            cv.disc(x - 0.4, y - 0.5, rr * 0.7, SOIL)
            if rr > 1.4:
                cv.set(x - 0.8, y - 0.9, SOILL)
        fade(cv, max(0.0, (f - 1) * 0.2))
        fr.append(cv)
    return fx_out('fx_dirt', fr, 32, 32)


def fx_bury():                  # 흙 쏟아짐 5컷 (구멍 64칸 위에 : 흙줄기 → 쌓임 → 봉분 → 먼지)
    fr = []
    for f in range(5):
        cv = Cv()
        r = random.Random(50 + f)
        R = 30
        if f < 4:                                # 쏟아지는 흙줄기
            for k in range(14 - f * 2):
                x = 32 + (r.random() - 0.5) * R * 1.5
                y0 = 6 + r.random() * 34 + f * 4
                ln = 4 + r.random() * 5
                col = SOIL if r.random() < 0.6 else SOILL
                cv.line(x, y0, x + (r.random() - 0.5), y0 + ln, col, 1)
                cv.set(x, y0 + ln, SOILD)
        # 구멍을 덮는 흙 : 가장자리부터 중앙으로 채워짐
        cover = [0.18, 0.45, 0.72, 0.92, 1.0][f]
        for y in range(64):
            for x in range(64):
                dx, dy = x + 0.5 - 32, (y + 0.5 - 32)
                d = math.hypot(dx, dy)
                if d <= R * cover and f > 0:
                    th = BAYER[y % 4][x % 4] / 16.0
                    base = SOILD if d > R * cover * 0.7 else SOIL
                    if -dy - dx * 0.4 > R * 0.25 * cover and d < R * cover * 0.8:
                        base = SOILL
                    if f < 4 and th > 0.4 + cover * 0.55:
                        continue
                    cv.p[(x, y)] = base
        if f >= 3:                               # 봉분 · 먼지
            for i in range(10):
                a = TAU * i / 10 + 0.3
                cv.disc(32 + math.cos(a) * 33, 32 + math.sin(a) * 33 * 0.9, 1.6, SOILD)
        if f == 4:
            fade(cv, 0.0)
            for i in range(12):
                a = TAU * i / 12
                cv.set(32 + math.cos(a) * 36, 32 + math.sin(a) * 36, (176, 140, 104))
        fr.append(cv)
    return fx_out('fx_bury', fr, 64, 64)


def stone_tomb(cv, ox, base_y, w, h, hp=1.0, big=False, ghost=False, crack=0):
    """묘비 하나를 cv 에 그림 (ox = 가운데 x · base_y = 발 y · hp = 솟은 높이 비율)"""
    hw = w / 2.0
    top = base_y - h * hp
    pal = (STN, STND, STNL)
    for y in range(int(top), int(base_y)):
        yy = y + 0.5
        rel = yy - top
        if rel < hw:
            dy = hw - rel
            half = math.sqrt(max(0.0, hw * hw - dy * dy))
        else:
            half = hw
        x0, x1 = int(round(ox - half)), int(round(ox + half))
        for x in range(x0, x1):
            col = STN
            if x - x0 < max(1, w // 8):
                col = STNL
            elif x1 - x - 1 < max(1, w // 8):
                col = STND
            if rel < 1.2:
                col = STNL
            if y >= base_y - 1:
                col = STND
            cv.p[(x, y)] = col
    return top


def carve(cv, ox, top, w, h, hp=1.0, big=False):
    # 비문 줄 (십자 아님) : 가로 줄 3개 + 작은 꽃 자리
    if hp < 0.85:
        return
    y = int(top + w * 0.5 + 1)
    for k, ln in enumerate((w - 6, w - 8, w - 5)):
        if ln <= 0:
            continue
        for x in range(int(ox - ln / 2), int(ox + ln / 2)):
            cv.p[(x, y + k * 2)] = STND
            cv.p[(x, y + k * 2 + 1)] = STNL if big and k == 0 else cv.p.get((x, y + k * 2 + 1), STN)
    # 은 띠 : 맨 위 둥근 곳 바로 아래 한 줄 (영웅색)
    yb = int(top + w * 0.5 - 1)
    for x in range(int(ox - w / 2 + 2), int(ox + w / 2 - 2)):
        cv.p[(x, yb)] = SIL


def mound(cv, ox, base_y, w, hgt=2):
    for i in range(hgt + 1):
        for x in range(int(ox - w / 2 + i), int(ox + w / 2 - i)):
            cv.p[(x, base_y - i)] = SOIL if i == 0 else SOILL if i == hgt else SOILD


def prop_img(w, h, ox, tw, th, big=False, temp=False):
    cv = Cv()
    base = h - 3
    mound(cv, ox, base + 2, w - 2, 2)
    top = stone_tomb(cv, ox, base, tw, th, 1.0, big)
    carve(cv, ox, top, tw, th, 1.0, big)
    cv.p[(int(ox + tw / 2 - 3), int(top + th * 0.45))] = SOILX            # 금 한 점
    cv.p[(int(ox + tw / 2 - 4), int(top + th * 0.45) + 1)] = SOILX
    if big:                                                               # 큰 묘비 : 받침 · 시든 꽃
        cv.rect(int(ox - tw / 2 - 2), base - 3, tw + 4, 3, STND)
        cv.rect(int(ox - tw / 2 - 2), base - 3, tw + 4, 1, STN)
        cv.set(ox - tw / 2 - 1, base - 6, PETALD); cv.set(ox - tw / 2 - 1, base - 5, SOILD)
    cv.outline(K1)
    if temp:                                                              # 임시 묘비 : 반투명 테 + 옅은 채움
        out = {}
        inner = {p: c for p, c in cv.p.items() if c != K1}
        for p, c in cv.p.items():
            if c == K1:
                out[p] = SIL + (200,)
            else:
                out[p] = tuple(int(c[i] * 0.35 + SIL[i] * 0.3) for i in range(3)) + (110,)
        for p in list(out):                                                 # 점선 : 테 한 칸 건너 하나씩 비움
            if len(out[p]) == 4 and out[p][3] == 200 and (p[0] + p[1]) % 3 == 0:
                del out[p]
        cv.p = out
    return cv


def fx_props():
    prop_img_out = []
    for name, (w, h, ox, tw, th, big, temp) in {
        'prop_grave': (16, 20, 8, 12, 15, False, False),
        'prop_grave_big': (28, 28, 14, 20, 21, True, False),
        'prop_grave_temp': (16, 20, 8, 12, 15, False, True),
    }.items():
        cv = prop_img(w, h, ox, tw, th, big, temp)
        prop_img_out.append(fx_out(name, [cv], w, h, pivot=(0.5, 0.15)))
    return prop_img_out


def fx_grave_rise():            # 묘비 솟음 3컷 (흙이 갈라짐 → 반쯤 → 다 솟고 흙이 쏟아짐)
    fr = []
    for f in range(3):
        cv = Cv()
        base = 27
        hp = [0.25, 0.65, 1.0][f]
        mound(cv, 16, base + 1, 22 - f * 2, 2)
        top = stone_tomb(cv, 16, base, 12, 17, hp)
        if hp >= 0.9:
            carve(cv, 16, top, 12, 17, hp)
        cv.outline(K1)
        r = random.Random(60 + f)
        for _ in range(8 + f * 2):
            a = r.random() * math.pi
            d = 6 + r.random() * 8
            cv.disc(16 + math.cos(a) * d * (1 if r.random() < 0.5 else -1), base - 2 - math.sin(a) * d * (0.9 - f * 0.2), 0.8 + r.random() * 0.8, r.choice((SOIL, SOILD, SOILL)))
        fr.append(cv)
    return fx_out('fx_grave_rise', fr, 32, 32, pivot=(0.5, 0.15))


def fx_grave_break():           # 묘비 무너짐 4컷 (금 → 갈라짐 → 조각이 흩어짐 → 먼지)
    fr = []
    for f in range(4):
        cv = Cv()
        base = 27
        r = random.Random(80 + f)
        if f < 2:
            top = stone_tomb(cv, 16, base, 12, 17)
            cv.outline(K1)
            ln = 7 + f * 6
            for i in range(ln):
                cv.set(17 + math.sin(i * 0.9) * 1.5, base - 16 + i, K1)
            if f == 1:
                for i in range(6):
                    cv.set(12 + i, base - 7 + (i % 2), K1)
            mound(cv, 16, base + 1, 20, 2)
        else:
            for i in range(7 - f):
                a = TAU * i / (7 - f) + r.random()
                d = 4 + (f - 2) * 5 + r.random() * 3
                x, y = 16 + math.cos(a) * d, base - 6 + math.sin(a) * d * 0.7 + (f - 2) * 3
                s = 2.4 - (f - 2) * 0.6
                cv.rect(int(x), int(y), int(max(2, s)), int(max(2, s)), STN)
                cv.rect(int(x), int(y), int(max(2, s)), 1, STNL)
            mound(cv, 16, base + 1, 20, 2)
            cv.outline(K1, skip=())
            if f == 3:
                for i in range(10):
                    a = TAU * i / 10
                    cv.disc(16 + math.cos(a) * 11, base - 3 + math.sin(a) * 6, 1.3, (176, 140, 104))
        fade(cv, 0.3 if f == 3 else 0)
        fr.append(cv)
    return fx_out('fx_grave_break', fr, 32, 32, pivot=(0.5, 0.15))


def fx_earthwall():             # 흙담 타일 (가로 16칸 · 가로로 이어 붙여 늘림)
    cv = Cv()
    for x in range(16):
        top = 2 + int(1.3 * math.sin(x * TAU / 16 * 2) + 0.5)
        for y in range(top, 12):
            col = SOILL if y == top else SOIL if y < top + 3 else SOILD if y < 10 else SOILX
            cv.p[(x, y)] = col
    for x, y in ((3, 5), (4, 5), (10, 6), (11, 6), (7, 8), (14, 7), (1, 8)):     # 박힌 돌 · 짚
        cv.p[(x, y)] = STN
        cv.p[(x, y + 1)] = STND
    # 위아래 K1 선 (가로 타일이 이어져야 해서 좌우는 열어 둠)
    for x in range(16):
        cv.p[(x, 12)] = K1
        for y in range(0, 12):
            if (x, y) in cv.p:
                if (x, y - 1) not in cv.p:
                    cv.p[(x, y - 1)] = K1
                break
    return fx_out('fx_earthwall', [cv], 16, 14)


def fx_tunnel():                # 땅굴 튀어나옴 4컷
    fr = []
    for f in range(4):
        cv = Cv()
        r = random.Random(95 + f)
        cv.ell(16, 20, 12 - (f == 0) * 4, 6 - (f == 0) * 2, SOILD)
        cv.ell(16, 20.5, 10 - (f == 0) * 4, 4.6 - (f == 0), (22, 14, 30))
        if f >= 1:
            for i in range(12):
                a = math.pi + (TAU / 2) * i / 11 + r.random() * 0.1
                d = 4 + f * 3
                x, y = 16 + math.cos(a) * d * 1.3, 20 + math.sin(a) * d * 1.4
                cv.disc(x, y, 2.2 - f * 0.35 + r.random() * 0.5, SOIL)
                cv.set(x - 0.6, y - 0.8, SOILL)
        if f >= 2:
            cv.ring(16, 20, 12 + f, 13.6 + f, SIL, 1.0, 0.5)
        fade(cv, 0.3 if f == 3 else 0)
        fr.append(cv)
    return fx_out('fx_tunnel', fr, 32, 32)


GHOST, GHOSTD, GHOSTL = (170, 196, 232), (112, 140, 188), (232, 242, 255)


def fx_hand():                  # 유령 손 3컷 (땅에서 뻗음 → 펼침 → 움켜쥠)
    fr = []
    for f in range(3):
        cv = Cv()
        h = [8, 15, 14][f]                       # 팔 높이
        for y in range(23 - h, 23):
            w = 3 if y > 23 - h + 4 else 4
            x0 = 8 - w // 2 + (1 if f == 2 else 0)
            for x in range(x0, x0 + w):
                cv.p[(x, y)] = GHOSTL if x == x0 else GHOST if x < x0 + w - 1 else GHOSTD
        top = 23 - h
        if f == 0:
            cv.rect(6, top - 2, 5, 3, GHOST); cv.rect(6, top - 2, 5, 1, GHOSTL)
            for x in (6, 8, 10):
                cv.rect(x, top - 4, 1, 3, GHOST)
        elif f == 1:
            cv.rect(5, top - 3, 7, 4, GHOST); cv.rect(5, top - 3, 7, 1, GHOSTL); cv.rect(5, top, 7, 1, GHOSTD)
            for x, ln in ((4, 3), (6, 5), (8, 6), (10, 5)):
                cv.rect(x, top - 3 - ln + 1, 1, ln, GHOST)
                cv.set(x, top - 3 - ln + 1, GHOSTL)
            cv.rect(12, top - 1, 2, 1, GHOST)
        else:
            cv.rect(5, top - 4, 7, 5, GHOST); cv.rect(5, top - 4, 7, 1, GHOSTL); cv.rect(5, top, 7, 1, GHOSTD)
            for x in (5, 7, 9, 11):
                cv.rect(x, top - 5, 1, 2, GHOSTL)
        # 팔 끝 : 아래로 갈수록 흩어짐 (유령)
        for (x, y) in list(cv.p):
            if y > 20 and BAYER[y % 4][x % 4] / 16.0 < (y - 20) / 4.0:
                del cv.p[(x, y)]
        cv.outline((64, 80, 132))
        for (x, y) in list(cv.p):
            if y >= 24:
                del cv.p[(x, y)]
        fr.append(cv)
    return fx_out('fx_hand', fr, 16, 24)


def fx_coffin():                # 관 (운구 · 가로 32 × 16 · 오른쪽이 앞)
    cv = Cv()
    pts = [(2, 8), (7, 2), (26, 2), (30, 8), (26, 14), (7, 14)]
    cv.poly(pts, WOODD)
    cv.poly([(4, 8), (8, 3.6), (25, 3.6), (28, 8), (25, 12.4), (8, 12.4)], WOOD)
    cv.rect(9, 4, 16, 1, WOODL)
    for x in (12, 16, 20):
        cv.rect(x, 5, 1, 7, WOODD)
    cv.rect(9, 7, 16, 2, SIL); cv.rect(9, 7, 16, 1, SILL)       # 은 띠 (십자 대신)
    for x in (10, 24):
        cv.set(x, 5, SILL); cv.set(x, 11, SILD)
    cv.outline(K1)
    return fx_out('fx_coffin', [cv], 32, 16)


def fx_bell_ring():             # 종 파동 고리 4컷 (영웅색 은 · 끊긴 마디)
    fr = []
    for f in range(4):
        cv = Cv()
        for ri, R in enumerate((8 + f * 4.4, 3 + f * 3.4)):
            if R < 3 or (ri == 1 and f == 0):
                continue
            for i in range(64):
                a = TAU * i / 64
                if (i // 5 + ri + f) % 3 == 0:
                    continue
                for t in range(2 if ri == 0 else 1):
                    d = R - t
                    cv.set(24 + math.cos(a) * d - 0.5, 24 + math.sin(a) * d - 0.5, SIL if t == 0 else SILD)
        for k in range(4):
            a = TAU * k / 4 + 0.78
            d = 10 + f * 4.0
            cv.disc(24 + math.cos(a) * d, 24 + math.sin(a) * d, 1.5, SILL)
        fade(cv, max(0.0, (f - 1) * 0.28))
        fr.append(cv)
    return fx_out('fx_bell_ring', fr, 48, 48)


def fx_trench():                # 도랑 (가로 32 × 8 · 양 끝이 이어지도록)
    cv = Cv()
    for x in range(32):
        for y in range(8):
            if y == 0:
                col = SOILL
            elif y == 1:
                col = SOIL
            elif y == 7:
                col = SOILD
            elif y == 6:
                col = SOIL
            else:
                col = (22, 14, 30) if y > 2 else (46, 34, 46)
            cv.p[(x, y)] = col
    for x in (4, 13, 21, 28):
        cv.p[(x, 0)] = SOIL; cv.p[(x + 1, 7)] = SOILD
    for x in (7, 18, 25):
        cv.p[(x, 4)] = (34, 24, 40)
    return fx_out('fx_trench', [cv], 32, 8)


def fx_pitfall():               # 빈 무덤 함정 (흙을 덮은 자국 · 32칸)
    cv = Cv()
    r = random.Random(11)
    cv.ell(16, 16, 14, 13, SOILD)
    cv.ell(16, 15.4, 12.5, 11.5, SOIL)
    for (x, y) in list(cv.p):
        if cv.p[(x, y)] == SOIL and BAYER[y % 4][x % 4] / 16.0 < 0.22:
            cv.p[(x, y)] = SOILD
    for _ in range(14):
        a = r.random() * TAU
        d = r.random() * 10
        cv.disc(16 + math.cos(a) * d, 16 + math.sin(a) * d, 0.8 + r.random() * 0.8, SOILL)
    for i in range(8):                           # 판자 흔적 (덮은 판자 틈)
        cv.set(9 + i * 2, 12 + (i % 2), SOILD)
    for k in range(4):                           # 은빛 표식 네 점 (내 함정이라는 영웅색)
        a = TAU * k / 4 + 0.78
        cv.set(16 + math.cos(a) * 11.5, 16 + math.sin(a) * 10.5, SIL)
    return fx_out('fx_pitfall', [cv], 32, 32)


def fx_bell_hang():             # 장례 충전 종 (화면 위 가운데 · 흔들림 2컷 · 32칸)
    fr = []
    for f in range(2):
        cv = Cv()
        sh = -2 if f == 0 else 2
        cv.rect(8, 2, 16, 2, WOODD); cv.rect(8, 2, 16, 1, WOOD)
        cv.line(16, 4, 16 + sh * 0.6, 7, (94, 98, 120), 1)
        pts = [(16 - 3 + sh * 0.6, 7), (16 + 3 + sh * 0.6, 7), (16 + 9 + sh, 22), (16 - 9 + sh, 22)]
        cv.poly(pts, BRZ)
        cv.poly([(13 + sh * 0.6, 8), (15 + sh * 0.6, 8), (13 + sh, 21), (9 + sh, 21)], BRZL)
        cv.poly([(19 + sh * 0.6, 8), (21 + sh * 0.6, 8), (25 + sh, 21), (21 + sh, 21)], BRZD)
        cv.rect(7 + sh, 21, 18, 2, BRZD); cv.rect(7 + sh, 21, 18, 1, BRZ)
        cv.disc(16 + sh * 1.6, 25.5, 2.0, WOODD)
        cv.outline(K1)
        fr.append(cv)
    return fx_out('fx_bell_hang', fr, 32, 32)


def fx_crow():                  # 까마귀 내리꽂힘 4컷 (오른쪽 아래로)
    fr = []
    for f in range(4):
        cv = Cv()
        wing = (-1, 2, -3, 1)[f]
        cv.ell(13, 12, 7, 3.4, (44, 44, 66))
        cv.ell(13, 11.4, 6, 2.2, (84, 88, 120))
        cv.disc(21, 11, 2.8, (44, 44, 66))
        cv.poly([(23, 10.4), (27, 11.8), (23, 12.6)], (250, 210, 90))
        cv.set(21, 10, (255, 250, 232))
        cv.poly([(8, 12), (8, 8 + wing), (14, 7 + wing * 1.4), (18, 11)], (36, 36, 56))
        cv.poly([(7, 11), (0, 12 - wing * 0.5), (3, 14), (8, 14)], (30, 30, 48))
        cv.outline(K1)
        fr.append(cv)
    return fx_out('fx_crow', fr, 32, 24)


def fxs():
    return [fx_hole(), fx_hole_rim(), fx_dirt(), fx_bury()] + fx_props() + [
        fx_grave_rise(), fx_grave_break(), fx_earthwall(), fx_tunnel(), fx_hand(), fx_coffin(), fx_bell_ring(),
        fx_trench(), fx_pitfall(), fx_bell_hang(), fx_crow()]


# ================================================================== 4. 아이콘 (범주 틀 + 그림문자)
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


def foe(cv, cx, cy, r, tilt=0):
    cv.disc(cx, cy, r, GRND)
    cv.disc(cx - 0.3, cy - 0.4, r - 0.9, GRN)
    if r >= 3:
        cv.set(cx - r * 0.45, cy - r * 0.15 + tilt, EYE); cv.set(cx + r * 0.45 - 1, cy - r * 0.15 + tilt, EYE)


def hole(cv, cx, cy, rx, ry):   # 구멍 (위에서 본 흙 테 + 어두운 속)
    cv.ell(cx, cy + 0.6, rx + 1.8, ry + 1.4, SOILD)
    cv.ell(cx, cy, rx + 1.4, ry + 1.0, SOIL)
    cv.ell(cx, cy + 0.4, rx, ry, (26, 18, 34))
    cv.ell(cx, cy - ry * 0.35, rx * 0.8, ry * 0.45, (54, 40, 54))
    cv.ell(cx, cy + ry * 0.2, rx * 0.7, ry * 0.4, (16, 11, 24))
    for x in range(int(cx - rx), int(cx + rx)):
        cv.set(x, cy - ry - 1, SOILL) if (x % 3) else None


def tomb(cv, cx, top, w, h, hl=True, text=True):
    hw = w / 2.0
    for y in range(top, top + h):
        rel = y + 0.5 - top
        half = math.sqrt(max(0.0, hw * hw - (hw - rel) ** 2)) if rel < hw else hw
        x0, x1 = int(round(cx - half)), int(round(cx + half))
        for x in range(x0, x1):
            col = STNL if (x - x0 < 2 and hl) else STND if x1 - x <= 2 else STN
            if rel < 1.2:
                col = STNL
            cv.p[(x, y)] = col
    if text and w >= 10:
        for k in range(3):
            ln = int(w - 6 - k)
            for x in range(int(cx - ln / 2), int(cx - ln / 2) + ln):
                cv.p[(x, int(top + hw + 1 + k * 2))] = STND


def shovel(cv, x0, y0, x1, y1, sc=1.0):         # 손잡이 (x0, y0) → 날 끝 (x1, y1) 대각선 삽
    L = math.hypot(x1 - x0, y1 - y0)
    dx, dy = (x1 - x0) / L, (y1 - y0) / L
    px, py = -dy, dx
    bl = 7 * sc
    bx, by = x1 - dx * bl, y1 - dy * bl
    cv.line(x0, y0, bx, by, WOODD, 2)
    cv.line(x0 - px * 0.6, y0 - py * 0.6, bx - px * 0.6, by - py * 0.6, WOOD, 1)
    cv.line(x0 - px * 2.5, y0 - py * 2.5, x0 + px * 2.5, y0 + py * 2.5, WOODD, 2)
    w = 3.4 * sc
    cv.poly([(bx - px * w, by - py * w), (bx + px * w, by + py * w), (x1 + px * w * 0.5, y1 + py * w * 0.5), (x1 - px * w * 0.5, y1 - py * w * 0.5)], SILD)
    cv.poly([(bx - px * (w - 1), by - py * (w - 1)), (bx + px * (w - 2), by + py * (w - 2)), (x1 + px * w * 0.2, y1 + py * w * 0.2), (x1 - px * w * 0.4, y1 - py * w * 0.4)], SIL)
    cv.line(bx - px * (w - 1), by - py * (w - 1), x1 - px * w * 0.4, y1 - py * w * 0.4, SILL, 1)


def bell(cv, cx, cy, k=1.0, col=(BRZ, BRZD, BRZL)):
    a, d, l = col
    top = cy - 7 * k
    for y in range(int(top), int(cy + 7 * k)):
        t = (y - top) / (14 * k)
        half = 3 + t * 6.5 * k
        for x in range(int(cx - half), int(cx + half)):
            cv.p[(x, y)] = l if (x < cx - half * 0.35 and t < 0.75) else a if x < cx + half * 0.45 else d
    cv.rect(int(cx - 10 * k), int(cy + 6 * k), int(20 * k), 2, d)
    cv.rect(int(cx - 10 * k), int(cy + 6 * k), int(20 * k), 1, a)
    cv.disc(cx, cy + 9.5 * k, 1.8, WOODD)
    cv.ring(cx, top - 1, 1.2, 2.6, (150, 154, 176))


def coffin_icon(cv, cx, cy, w=20, h=12, rot=0.0):
    pts = [(-w / 2, 0), (-w / 2 + 4, -h / 2), (w / 2 - 4, -h / 2), (w / 2, 0), (w / 2 - 4, h / 2), (-w / 2 + 4, h / 2)]
    cs, sn = math.cos(rot), math.sin(rot)
    P = lambda p, sx=1.0: (cx + (p[0] * sx) * cs - p[1] * sx * sn, cy + (p[0] * sx) * sn + p[1] * sx * cs)
    cv.poly([P(p) for p in pts], WOODD)
    cv.poly([P(p, 0.78) for p in pts], WOOD)
    a, b = P((-w / 2 + 5, 0)), P((w / 2 - 5, 0))
    cv.line(a[0], a[1], b[0], b[1], SIL, 2)


def flower(cv, cx, cy, r=3.2):
    for i in range(8):
        a = TAU * i / 8
        cv.disc(cx + math.cos(a) * r * 0.7, cy + math.sin(a) * r * 0.7, 1.2, PETAL)
    cv.disc(cx, cy, 1.3, GOLD)


def hand(cv, x, y, s=1.0, col=(GHOST, GHOSTD, GHOSTL)):
    a, d, l = col
    cv.rect(int(x - 2 * s), int(y), int(4 * s), int(6 * s), a)
    cv.rect(int(x - 3 * s), int(y - 4 * s), int(6 * s), int(5 * s), a)
    for i, fx in enumerate((-3, -1, 1, 3)):
        cv.rect(int(x + fx * s - (1 if fx < 0 else 0)), int(y - 7 * s - (i in (1, 2))), max(1, int(s)), int(4 * s), l if i < 2 else a)
    cv.rect(int(x - 3 * s), int(y), int(6 * s), 1, d)


def burst(cv, cx, cy, n, a, b, c1, c2):
    for i in range(n):
        ang = TAU * i / n
        ln = a if i % 2 == 0 else b
        for t in range(int(ln * 2)):
            d = 2 + t / 2.0
            cv.set(cx + math.cos(ang) * d - 0.5, cy + math.sin(ang) * d - 0.5, c1 if d < ln * 0.6 else c2)


# ---- 특수 능력 561~567
def g561(cv):     # 묘지 까마귀 : 구멍 가장자리에 앉은 까마귀가 부리를 내려 쪼는 모습
    hole(cv, 16, 24, 9, 3.6)
    cv.ell(14, 14, 8, 5.4, COALD); cv.ell(13, 13, 7, 4, COAL); cv.ell(11, 12, 4, 2.2, COALL)
    cv.disc(23, 9, 4, COALD); cv.disc(22.6, 8.6, 3.2, COAL)
    cv.poly([(25, 9), (30, 12), (24, 12.4)], GOLD); cv.set(26, 10, GOLD_D)
    cv.set(22, 7, WHITE); cv.set(22, 8, EYE)
    cv.poly([(7, 14), (1, 18), (2, 21), (8, 18)], COALD)                      # 꼬리
    cv.line(12, 19, 11, 22, GOLD_D, 1); cv.line(16, 19, 16, 22, GOLD_D, 1)    # 발
    cv.line(4, 5, 8, 8, COAL, 2); cv.set(3, 4, COALL)                         # 펼친 날개 끝


def g562(cv):     # 상여 종
    bell(cv, 16, 16)
    arc(cv, 16, 16, 14, -0.45, 0.45, SIL, 2) if False else None
    for sgn in (-1, 1):
        cv.line(16 + sgn * 13, 11, 16 + sgn * 13, 15, SILL); cv.line(16 + sgn * 15, 9, 16 + sgn * 15, 17, SIL)
    cv.rect(8, 26, 16, 1, SILD)


def g563(cv):     # 석관
    pts = [(2, 16), (8, 7), (24, 7), (30, 16), (24, 25), (8, 25)]
    cv.poly(pts, STND)
    cv.poly([(4.5, 16), (9, 9), (23, 9), (27.5, 16), (23, 23), (9, 23)], STN)
    cv.poly([(9, 9), (23, 9), (21, 11), (11, 11)], STNL)
    cv.rect(10, 15, 12, 3, GOLD); cv.rect(10, 15, 12, 1, WHITE); cv.rect(10, 17, 12, 1, GOLD_D)     # 금 뚜껑 줄
    cv.rect(10, 12, 12, 1, GOLD_D); cv.rect(10, 20, 12, 1, GOLD_D)


def g564(cv):     # 묘지기의 장화 (패시브)
    for ox, c in ((0, 1), (9, 0)):
        x = 6 + ox
        cv.rect(x, 6, 7, 14, COAL if c else COALD)
        cv.rect(x, 6, 2, 14, COALL if c else COAL)
        cv.rect(x - 1, 19, 12, 5, COALD); cv.rect(x - 1, 19, 12, 1, COAL)
        cv.rect(x - 1, 24, 13, 2, SOILD)
        cv.rect(x, 7, 7, 1, SIL)
    for xx, yy in ((5, 27), (24, 26), (28, 22)):
        cv.set(xx, yy, SOIL)


def g565(cv):     # 조의금 (검은 띠 봉투 + 코인)
    cv.rect(5, 9, 22, 15, WHITE); cv.rect(5, 9, 22, 1, PETAL)
    cv.poly([(5, 9), (27, 9), (16, 18)], (226, 230, 240))
    cv.line(5, 9, 16, 18, (186, 194, 214), 1); cv.line(27, 9, 16, 18, (186, 194, 214), 1)
    cv.rect(5, 22, 22, 2, (200, 206, 224))
    cv.rect(13, 9, 6, 15, COALD); cv.rect(13, 9, 2, 15, COAL)                 # 검은 띠
    cv.disc(24, 22, 5, GOLD_D); cv.disc(23.6, 21.6, 4, GOLD); cv.rect(22, 19, 3, 3, WHITE)


def g566(cv):     # 밤샘 (촛불)
    cv.rect(11, 14, 10, 12, (240, 236, 224)); cv.rect(11, 14, 3, 12, WHITE); cv.rect(18, 14, 3, 12, (200, 196, 190))
    cv.rect(9, 25, 14, 3, STND); cv.rect(9, 25, 14, 1, STN)
    cv.rect(15, 11, 2, 3, COALD)
    cv.poly([(16, 3), (19, 8), (16, 12), (13, 8)], GOLD_D)
    cv.poly([(16, 5), (18, 8), (16, 11), (14, 8)], GOLD)
    cv.poly([(16, 7), (17, 9), (16, 11), (15, 9)], WHITE)
    cv.set(13, 18, WHITE); cv.set(12, 21, WHITE)
    for sgn in (-1, 1):
        cv.line(16 + sgn * 10, 6, 16 + sgn * 10, 9, SIL)


def g567(cv):     # 부장품 (항아리 + 솟는 구슬)
    cv.ell(16, 21, 8.4, 6.6, WOODD)
    cv.ell(15.4, 20.4, 7.2, 5.6, WOOD)
    cv.ell(13, 18, 3, 2.4, WOODL)
    cv.rect(10, 12, 12, 3, WOODD); cv.rect(11, 11, 10, 2, WOOD)
    cv.rect(10, 17, 12, 2, SIL)
    cv.disc(16, 5.6, 3.4, GRND); cv.disc(15.6, 5.2, 2.6, GRN); cv.set(14, 4, WHITE)
    for x, y in ((9, 6), (23, 7), (6, 11), (26, 12)):
        cv.set(x, y, GOLD)


# ---- 전용 카드 770~775
def g770(cv):     # 깊은 구덩이 (단면 : 층층 흙 + 깊은 어둠 + 갇힌 적)
    cv.rect(2, 6, 28, 22, SOILD)
    for y, col in ((6, SOILL), (7, SOIL), (12, SOIL), (13, SOILD)):
        cv.rect(2, y, 28, 1, col)
    cv.rect(9, 6, 14, 22, (24, 16, 32))
    cv.rect(10, 6, 12, 5, (44, 32, 46))
    foe(cv, 16, 21, 4.4)
    cv.rect(13, 17, 6, 2, (24, 16, 32))
    for x in (4, 25, 7, 27):
        cv.set(x, 17 + x % 3, SOIL); cv.set(x, 22, SOILL)
    cv.line(16, 7, 16, 12, SIL); arrow_head(cv, 16, 13, 0, 1, SILL, 3)


def g771(cv):     # 여러 삽 (구멍 셋 + 삽 하나)
    hole(cv, 8, 24, 4.2, 2.4); hole(cv, 16, 24, 4.2, 2.4); hole(cv, 24, 24, 4.2, 2.4)
    shovel(cv, 24, 2, 16, 18, 0.9)
    cv.line(26, 12, 28, 10, SILL); cv.line(27, 15, 29, 15, SILL)


def g772(cv):     # 무너지는 흙 (구멍 쪽으로 끌려드는 적)
    hole(cv, 16, 17, 7, 4.4)
    foe(cv, 4, 7, 3.2); foe(cv, 28, 25, 3.2)
    cv.line(7, 9, 11, 13, SIL, 2); arrow_head(cv, 12, 14, 1, 1, SILL, 3)
    cv.line(25, 23, 21, 20, SIL, 2); arrow_head(cv, 20, 19, -1, -1, SILL, 3)
    for x, y in ((11, 7), (22, 8), (8, 26), (19, 27), (26, 14)):
        cv.disc(x, y, 1.3, SOIL); cv.set(x, y - 1, SOILL)


def g773(cv):     # 종지기 (묘비 위에서 울리는 종)
    tomb(cv, 16, 17, 14, 12)
    bell(cv, 16, 9, 0.62)
    for sgn in (-1, 1):
        cv.line(16 + sgn * 11, 4, 16 + sgn * 11, 9, SILL); cv.line(16 + sgn * 13, 3, 16 + sgn * 13, 11, SIL)
    cv.rect(6, 28, 20, 2, SOILD)


def g774(cv):     # 비문 (글자 새긴 묘비 · 받는 피해 증가 화살)
    tomb(cv, 14, 4, 16, 24)
    for k in range(4):
        w = (10, 8, 10, 6)[k]
        cv.rect(14 - w // 2, 14 + k * 3, w, 1, SILL)
    cv.rect(6, 28, 16, 2, SOILD)
    cv.line(26, 20, 26, 8, (250, 150, 120), 2); arrow_head(cv, 26, 6, 0, -1, (255, 190, 150), 3)


def g775(cv):     # 되파기 (묘비를 찍어 쪼개는 삽)
    tomb(cv, 11, 8, 14, 18)
    for i in range(9):
        cv.set(12 + math.sin(i * 0.9) * 1.4, 8 + i * 1.3, K1)
    shovel(cv, 29, 2, 15, 17, 0.95)
    for x, y in ((4, 26), (8, 28), (18, 26), (21, 22), (24, 27)):
        cv.rect(x, y, 2, 2, STN); cv.rect(x, y, 2, 1, STNL)
    cv.set(17, 20, GOLD); cv.set(16, 21, GOLD); cv.set(18, 21, GOLD); cv.set(17, 22, GOLD)


# ---- 고유 스킬 870~879
def g870(cv):     # 관 뚜껑 (닫히는 충격)
    coffin_icon(cv, 16, 20, 24, 14)
    cv.rect(4, 8, 24, 3, WOODD); cv.rect(4, 8, 24, 1, WOODL)
    burst(cv, 16, 12, 8, 6, 3, SILL, SIL)
    cv.ring(16, 22, 11, 13, SIL, 1.0, 0.5)


def g871(cv):     # 도랑 파기 (구멍에서 뻗은 도랑 · 느려지는 적)
    hole(cv, 7, 22, 4.4, 2.8)
    for x in range(10, 28):
        cv.rect(x, 21, 1, 1, SOILL); cv.rect(x, 22, 1, 3, (30, 22, 38)); cv.rect(x, 25, 1, 1, SOILD)
    foe(cv, 22, 14, 4.2)
    cv.line(17, 8, 27, 8, SIL, 1); cv.set(26, 7, SIL); cv.set(26, 9, SIL)
    cv.line(17, 6, 22, 6, SILD, 1)


def g872(cv):     # 유령 손 (묘비에서 뻗어 발목을 잡음)
    tomb(cv, 9, 8, 12, 17)
    foe(cv, 24, 13, 4.2)
    cv.rect(21, 17, 7, 2, EYE)
    hand(cv, 21, 24, 1.0)
    cv.line(10, 26, 20, 26, SOILD, 2)
    cv.set(3, 6, GOLD); cv.set(2, 7, GOLD); cv.set(4, 7, GOLD)


def g873(cv):     # 흙담 (묘비를 잇는 담)
    for x in (4, 20):
        tomb(cv, x + 4, 9, 8, 15, text=False)
    cv.rect(12, 18, 12, 8, SOILD); cv.rect(12, 18, 12, 2, SOILL); cv.rect(12, 20, 12, 3, SOIL)
    for x, y in ((14, 22), (19, 23), (16, 20)):
        cv.set(x, y, STN)
    cv.rect(2, 26, 28, 2, SOILD)


def g874(cv):     # 운구 (미끄러지는 관)
    coffin_icon(cv, 15, 15, 22, 12, 0.35)
    for k in range(3):
        cv.line(2, 20 + k * 3, 8, 22 + k * 3, SIL, 1)
    foe(cv, 27, 24, 3.2)
    cv.set(26, 13, GOLD); cv.set(25, 12, GOLD); cv.set(27, 12, GOLD)


def g875(cv):     # 추모 (묘비 앞의 흰 꽃)
    tomb(cv, 16, 4, 14, 18)
    flower(cv, 16, 25, 4.0)
    cv.line(16, 28, 16, 31, GRND, 1)
    cv.rect(8, 26, 3, 3, SIL) if False else None
    for sgn in (-1, 1):
        cv.line(16 + sgn * 11, 10, 16 + sgn * 11, 22, SIL, 1)


def g876(cv):     # 빈 무덤 (덮은 함정)
    cv.ell(16, 20, 13, 7, SOILD); cv.ell(16, 19.3, 11.5, 5.8, SOIL)
    for i in range(7):
        cv.rect(8 + i * 2, 17 + (i % 2), 2, 1, SOILD)
    for x, y in ((11, 20), (21, 21), (16, 22), (14, 17)):
        cv.set(x, y, SOILL)
    cv.disc(16, 8, 4.4, SIL); cv.disc(16, 8, 3.4, COALD)
    cv.rect(15, 6, 2, 2, SIL); cv.rect(15, 9, 2, 2, SIL)
    cv.line(16, 13, 16, 15, SIL)


def g877(cv):     # 부고 (검은 띠 종이 + 게이지)
    cv.rect(8, 4, 16, 24, (248, 244, 232)); cv.rect(8, 4, 16, 1, WHITE)
    cv.rect(8, 4, 16, 24, (248, 244, 232)); cv.rect(8, 27, 16, 1, (206, 196, 176))
    cv.rect(8, 4, 2, 24, COALD); cv.rect(22, 4, 2, 24, COALD); cv.rect(8, 4, 16, 2, COALD); cv.rect(8, 26, 16, 2, COALD)
    for y in (10, 13, 16, 19):
        cv.rect(12, y, 8, 1, COAL)
    cv.rect(15, 21, 2, 2, SIL)
    cv.disc(25, 25, 4, GOLD_D); cv.disc(24.6, 24.6, 3, GOLD); cv.set(24, 23, WHITE)


def g878(cv):     # 명부 (펼친 장부 + 세는 줄)
    cv.poly([(2, 8), (16, 10), (30, 8), (30, 26), (16, 28), (2, 26)], COALD)
    cv.poly([(4, 9), (15, 11), (15, 26), (4, 24.5)], (240, 232, 210))
    cv.poly([(17, 11), (28, 9), (28, 24.5), (17, 26)], (232, 222, 196))
    cv.rect(15, 11, 2, 15, (150, 130, 110))
    for k in range(4):
        cv.line(6, 14 + k * 2.4, 13, 15 + k * 2.4, COALL, 1)
    for k in range(4):
        cv.rect(20 + k * 2, 13, 1, 5, COAL)
    cv.line(19, 17, 28, 14, COAL, 1)
    cv.rect(19, 20, 9, 1, COAL); cv.rect(20, 22, 7, 1, SOILD)


def g879(cv):     # 산 자의 무덤 (구멍에 들어간 장의사 · 실크햇)
    hole(cv, 16, 21, 11, 6)
    cv.rect(11, 8, 10, 8, COAL); cv.rect(11, 8, 2, 8, COALL); cv.rect(10, 15, 12, 2, COAL)
    cv.rect(11, 12, 10, 1, SIL)
    cv.rect(12, 18, 8, 3, FACE); cv.set(14, 19, EYE); cv.set(18, 19, EYE)
    cv.ring(16, 15, 11, 12.6, SIL, 1.0, 1.0)
    for x, y in ((3, 8), (29, 8), (4, 24)):
        cv.set(x, y, SILL)


# ---- 스킬 진화 970~976 (육각)
def g970(cv):     # 집단 매장
    hole(cv, 16, 22, 11, 5)
    for x, y in ((10, 20), (16, 18), (22, 20), (13, 23), (19, 23)):
        foe(cv, x, y, 3.2)
    cv.poly([(5, 25), (11, 18), (21, 18), (27, 25)], SOIL)
    cv.poly([(11, 18), (21, 18), (19, 20), (13, 20)], SOILL)
    burst(cv, 16, 9, 8, 6, 3, SILL, SIL)
    cv.disc(16, 9, 2, WHITE)


def g971(cv):     # 묘지기의 담
    for x, h in ((2, 12), (23, 12)):
        tomb(cv, x + 3.5, 10, 7, h, text=False)
    tomb(cv, 16, 6, 10, 16, text=False)
    cv.rect(9, 20, 3, 6, SOILD); cv.rect(20, 20, 3, 6, SOILD)
    cv.rect(2, 22, 28, 4, SOILD); cv.rect(2, 22, 28, 1, SOILL); cv.rect(2, 23, 28, 1, SOIL)
    for x in (5, 13, 21, 27):
        cv.set(x, 24, STN)
    cv.rect(2, 26, 28, 2, SOILX)
    cv.rect(13, 12, 6, 1, SIL); cv.rect(14, 15, 4, 1, SILD)


def g972(cv):     # 지하 묘도 (단면 : 두 구멍 + 땅속 통로)
    cv.rect(2, 6, 28, 22, SOILD)
    cv.rect(2, 6, 28, 2, SOILL); cv.rect(2, 8, 28, 2, SOIL)
    DK = (24, 16, 32)
    cv.rect(4, 6, 6, 12, DK); cv.rect(22, 6, 6, 12, DK)
    cv.rect(4, 15, 24, 8, DK)
    cv.rect(5, 6, 4, 2, (54, 40, 54)); cv.rect(23, 6, 4, 2, (54, 40, 54))
    cv.rect(4, 15, 24, 1, (44, 32, 46))
    cv.line(10, 19, 20, 19, SIL, 2); arrow_head(cv, 21, 19, 1, 0, SILL, 3)
    cv.rect(2, 26, 28, 2, SOILX)
    for x, y in ((12, 11), (16, 12), (14, 25), (25, 25)):
        cv.set(x, y, SOILL)


def g973(cv):     # 국장 (검은 리본을 맨 큰 종 · 세 번 울림)
    bell(cv, 16, 13, 0.9)
    cv.poly([(16, 21), (11, 30), (14, 28), (16, 31), (18, 28), (21, 30)], COALD)
    cv.poly([(16, 21), (13, 28), (16, 26)], COAL)
    cv.rect(13, 20, 6, 2, COAL)
    for sgn in (-1, 1):
        for k in range(3):
            cv.set(16 + sgn * (12 + k * 1.4), 8 + k * 3, SILL)
            cv.set(16 + sgn * (12 + k * 1.4), 9 + k * 3, SIL)


def g974(cv):     # 무덤 털이 (열린 관과 쏟아지는 코인)
    cv.poly([(3, 20), (8, 12), (24, 12), (29, 20), (24, 28), (8, 28)], WOODD)
    cv.poly([(5.5, 20), (9, 14), (23, 14), (26.5, 20), (23, 26), (9, 26)], (30, 22, 38))
    cv.poly([(1, 10), (9, 4), (27, 6), (28, 11), (10, 12)], WOOD)
    cv.line(2, 10, 27, 7, WOODL, 1)
    for x, y, r in ((13, 21, 3), (19, 23, 3), (16, 18, 2.6), (22, 18, 2.4)):
        cv.disc(x, y, r, GOLD_D); cv.disc(x - 0.4, y - 0.4, r - 1, GOLD)
    cv.set(12, 20, WHITE); cv.set(18, 22, WHITE)
    burst(cv, 25, 6, 6, 4, 2, GOLD, GOLD_D)


def g975(cv):     # 안식처 (큰 묘비 · 달무리 · 두 송이 꽃)
    cv.disc(16, 13, 12.4, (62, 78, 112)); cv.disc(16, 13, 11, (84, 104, 146))
    cv.disc(16, 13, 9.4, (108, 130, 176))
    tomb(cv, 16, 5, 14, 20)
    for k in range(3):
        cv.rect(12 + k % 2, 14 + k * 3, 8 - k % 2 * 2, 1, STND)
    cv.rect(6, 25, 20, 3, SOILD); cv.rect(6, 25, 20, 1, SOIL)
    flower(cv, 8, 24, 3.0); flower(cv, 24, 24, 3.0)
    cv.set(26, 4, SILL); cv.set(27, 3, SILL); cv.set(28, 4, SILL); cv.set(27, 5, SILL)


def g976(cv):     # 개미지옥 (소용돌이 구덩이 · 집게)
    for k, (rx, ry, col) in enumerate(((14, 11.5, SOIL), (11.2, 9.2, SOILD), (8.2, 6.8, (46, 34, 46)), (5.2, 4.2, (26, 18, 34)), (2.6, 2.2, (14, 10, 22)))):
        cv.ell(16, 17, rx, ry, col)
    for i in range(30):
        a = i * 0.46
        d = 12.5 - i * 0.37
        if d > 2:
            cv.set(16 + math.cos(a) * d * 1.0, 17 + math.sin(a) * d * 0.82, SOILL if i % 3 else SOIL)
    for sgn in (-1, 1):                                              # 집게
        cv.line(16 + sgn * 4, 7, 16 + sgn * 1, 14, SIL, 2)
        cv.set(16 + sgn * 1, 14, SILL); cv.set(16 + sgn * 5, 6, SILL)
    foe(cv, 25, 8, 2.8)
    cv.set(5, 26, SOILL); cv.set(27, 25, SOILL)


# ---- 무기 강화 621~623
def g621(cv):     # 넓은 삽날 (넓어진 날)
    cv.line(16, 2, 16, 12, WOODD, 2); cv.line(15, 2, 15, 12, WOOD, 1)
    cv.rect(12, 2, 8, 2, WOODD)
    cv.poly([(5, 12), (27, 12), (25, 24), (16, 30), (7, 24)], SILD)
    cv.poly([(7, 13), (25, 13), (23, 23), (16, 28), (9, 23)], SIL)
    cv.rect(7, 13, 18, 1, SILL); cv.rect(8, 14, 2, 8, SILL)
    for sgn in (-1, 1):
        cv.line(16 + sgn * 11, 18, 16 + sgn * 14, 18, (255, 240, 170), 1)
        cv.set(16 + sgn * 14 + (-1 if sgn < 0 else 1), 17, (255, 240, 170)); cv.set(16 + sgn * 14 + (-1 if sgn < 0 else 1), 19, (255, 240, 170))


def g622(cv):     # 두 번 파기 (구멍 둘 · 이어서)
    hole(cv, 9, 22, 6.2, 3.4)
    hole(cv, 23, 11, 4.2, 2.4)
    shovel(cv, 8, 2, 8, 14, 0.8) if False else None
    cv.line(12, 17, 19, 14, SIL, 2); arrow_head(cv, 20, 13.6, 1, -0.4, SILL, 3)
    cv.rect(5, 6, 5, 1, SILL); cv.rect(4, 8, 7, 1, SIL)
    cv.set(23, 4, GOLD); cv.set(22, 5, GOLD); cv.set(24, 5, GOLD)


def g623(cv):     # 무거운 흙 (큰 흙덩이 + 눌러 내림)
    cv.poly([(5, 26), (7, 13), (13, 7), (21, 7), (26, 13), (28, 26)], SOILD)
    cv.poly([(7, 25), (9, 14), (14, 9), (20, 9), (24, 14)], SOIL)
    cv.poly([(9, 14), (14, 9), (19, 9), (17, 13)], SOILL)
    for x, y in ((13, 17), (20, 20), (11, 22), (22, 14)):
        cv.rect(x, y, 2, 2, SOILD)
    cv.rect(13, 3, 6, 2, SILL)
    cv.line(16, 2, 16, 6, SILL, 2) if False else None
    arrow_head(cv, 16, 7, 0, 1, SILL, 3)
    cv.rect(3, 26, 26, 2, SOILX)


ICONS = [
    (561, 'square', 'weapon', g561), (562, 'square', 'weapon', g562), (563, 'square', 'weapon', g563),
    (564, 'diamond', 'survival', g564), (565, 'diamond', 'survival', g565), (566, 'diamond', 'survival', g566), (567, 'diamond', 'survival', g567),
    (770, 'square', 'weapon', g770), (771, 'square', 'weapon', g771), (772, 'square', 'weapon', g772), (773, 'square', 'weapon', g773),
    (774, 'circle', 'skill', g774), (775, 'circle', 'skill', g775),
    (870, 'circle', 'skill', g870), (871, 'circle', 'skill', g871), (872, 'circle', 'skill', g872), (873, 'circle', 'skill', g873),
    (874, 'circle', 'skill', g874), (875, 'circle', 'skill', g875), (876, 'circle', 'skill', g876), (877, 'circle', 'skill', g877),
    (878, 'circle', 'skill', g878), (879, 'circle', 'skill', g879),
    (970, 'hex', 'soul', g970), (971, 'hex', 'soul', g971), (972, 'hex', 'soul', g972), (973, 'hex', 'soul', g973),
    (974, 'hex', 'soul', g974), (975, 'hex', 'soul', g975), (976, 'hex', 'soul', g976),
    (621, 'square', 'weapon', g621), (622, 'square', 'weapon', g622), (623, 'square', 'weapon', g623),
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
    sc = 8
    S = Image.new('RGBA', (192 * sc, 32 * sc), bg)
    b = Image.new('RGBA', (192, 32), bg); b.alpha_composite(body)
    S.paste(b.resize((192 * sc, 32 * sc), Image.NEAREST), (0, 0))
    S.save(os.path.join(folder, 'body.png'))
    cols = 11
    I = Image.new('RGBA', (cols * 100, ((len(icns) + cols - 1) // cols) * 100), (30, 24, 44, 255))
    for k, (n, im) in enumerate(icns):
        I.alpha_composite(im.resize((96, 96), Image.NEAREST), ((k % cols) * 100 + 2, (k // cols) * 100 + 2))
    I.save(os.path.join(folder, 'icons.png'))
    items = []
    for nm in ('weapon_shovel', 'weapon_kit_161', 'weapon_crow_2', 'weapon_kit_162', 'weapon_bell_2', 'weapon_kit_163'):
        items.append((Image.open(os.path.join(WEAP, nm + '.png')).convert('RGBA'), 10))
    fxn = ['fx_hole', 'fx_hole_rim', 'fx_dirt', 'fx_bury', 'prop_grave', 'prop_grave_big', 'prop_grave_temp', 'fx_grave_rise', 'fx_grave_break',
           'fx_earthwall', 'fx_tunnel', 'fx_hand', 'fx_coffin', 'fx_bell_ring', 'fx_trench', 'fx_pitfall', 'fx_bell_hang', 'fx_crow']
    for nm in fxn:
        s = 3 if nm in ('fx_hole', 'fx_hole_rim', 'fx_bury', 'fx_bell_ring') else 6 if nm.startswith('prop') else 4
        items.append((Image.open(os.path.join(FXD, nm + '.png')).convert('RGBA'), s))
    W = 1700
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
