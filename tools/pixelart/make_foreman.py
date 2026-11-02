# 새 영웅 공사 반장 「영원한 공사 중」 그림 일체 (docs/plans/2026-11-02-big-update/hero-foreman.md 13절)
#   Characters/foreman.png             32칸 몸 시트 (192×32 · idle 2 + run 4) — 스프라이트 foreman_0~5
#   Weapons/weapon_plow.png            손에 든 삽날 (held = "plow")
#   Weapons/weapon_kit_147~149.png     전용 무기 굴착기 버킷 · 착암기 · 철거 쇠공 (weapon_bucket · jackhammer · wreckball 도 같은 그림)
#   FX/fx_dust · crunch · rubble · cement · landslide · roller · wreckball · barricade · rebar · tape · bucket
#      (fx_quake 는 이미 있는 공용 그림이라 만들지 않음 — 기존 것을 그대로 씀)
#   Icons/ability_<번호>.png           특수 547~553 · 전용 카드 750~755 · 고유 스킬 850~859 · 스킬 진화 950~956 · 강화 615~617
#   범주 틀은 frame_icons.py 의 틀을 그대로 가져옴 (사각 무기 · 원 스킬 · 마름모 패시브 · 육각 진화)
#   .meta 는 이미 있으면 그대로 둠 (다시 돌려도 guid 유지) · 새로 만들 때 guid 는 이름에서 정해짐
# 실행: py -3.14 -X utf8 tools/pixelart/make_foreman.py [미리보기 폴더]
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
    return uuid.uuid5(uuid.NAMESPACE_URL, 'soulsaver/foreman/' + key).hex


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
    r = random.Random('foreman/' + key)
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
# 영웅색 = 레몬 노랑 + 검정 (안전모 · 조끼 줄무늬). 금색 영웅(거너 · 우체부 · 시계공)과 구분되게 노랑은 차갑게(연두 쪽), 검정 줄을 크게.
BODY_PAL = {
    'Y': (255, 247, 150), 'y': (242, 228, 84), 'g': (212, 178, 40), 'd': (170, 132, 30),          # 안전모 (밝음 · 기본 · 줄/그늘 · 챙 밑)
    's': (232, 172, 128), 'S': (178, 110, 88), 'L': (250, 204, 160), 'm': (88, 52, 40), 'e': (24, 16, 36),   # 살 · 콧수염 · 눈
    'n': (96, 106, 134), 'N': (62, 70, 100),                                                      # 작업복 소매
    'o': (248, 140, 44), 'l': (255, 184, 88), 'O': (196, 92, 30), 'b': (30, 24, 38), 'z': (70, 46, 54),   # 조끼 · 검정 줄 · 지퍼
    'P': (60, 70, 106), 'p': (38, 44, 74), 'B': (146, 92, 54), 'c': (92, 58, 38), 'x': (255, 247, 150),  # 바지 · 안전화 · 버클
}
EDGE_Y = (52, 34, 8)               # 안전모 노랑에 붙은 외곽 (Y < 41)
EDGE_O = (70, 26, 10)              # 조끼 주황에 붙은 외곽
EDGE_S = (62, 28, 38)              # 살색 그늘
DUST = (214, 176, 112)

HEAD = [
    (0, 6, 'Yyggyy'),
    (1, 4, 'YYyyggyyyd'),
    (2, 3, 'Yyyyyggyyygd'),
    (3, 2, 'YYyyyyyyyyyygg'),                      # 앞 챙
    (4, 3, 'ddSSSSSSSSdd'),                        # 챙 그늘 진 이마
    (5, 4, 'sssessessS'),
    (6, 4, 'ssmmmmmmsS'),                          # 콧수염
    (7, 6, 'sssssS'),
]
TORSO = [
    (8, 2, 'nnloosSSsooONN'),
    (9, 1, 'nN' + 'bbbbb' + 'zz' + 'bbbbb' + 'Nn'),
    (10, 1, 'nN' + 'loooo' + 'zz' + 'ooooO' + 'Nn'),
    (11, 1, 'nN' + 'bbbbb' + 'zz' + 'bbbbb' + 'Nn'),
    (12, 1, 'sS' + 'looOo' + 'zz' + 'oooOO' + 'Ss'),
    (13, 1, 'sS' + 'bbbbb' + 'xx' + 'bbbbb' + 'Ss'),
    (14, 3, 'PPPPPppPPPPP'),
]
LEGS = {
    'stand': [(15, 4, 'PPPp..pPPP'), (16, 4, 'PPPp..pPPP'), (17, 3, 'BBBBc..cBBBB'), (18, 3, 'ccccc..ccccc')],
    'a': [(15, 4, 'PPPp..pPPP'), (16, 4, 'PPPp'), (16, 11, 'pPPP'), (17, 4, 'BBBc'), (17, 11, 'BBBBc'), (18, 12, 'cBBBB')],
    'b': [(15, 5, 'PPPppPPP'), (16, 5, 'PPPppPPP'), (17, 5, 'BBBccBBB'), (18, 5, 'cccccccc')],
    'c': [(15, 4, 'PPPp..pPPP'), (16, 4, 'PPPp'), (16, 11, 'pPPP'), (17, 3, 'BBBBc'), (17, 11, 'cBBB'), (18, 2, 'cBBBB')],
}
ARMS_UP_L = [(9, 0, 'sS'), (10, 0, 'nN'), (11, 1, 'nN')]
ARMS_UP_R = [(9, 16, 'Ss'), (10, 16, 'Nn'), (11, 15, 'Nn')]
ARMS_UP_R2 = [(10, 16, 'sS'), (11, 15, 'nN'), (12, 15, 'Nn')]


def body_cells(head_dy=0, legs='stand', arm_l='down', arm_r='down', dust=()):
    c = {}

    def put(rows, dy=0):
        for y, x0, text in rows:
            for i, ch in enumerate(text):
                if ch != '.':
                    c[(x0 + i, y + dy)] = BODY_PAL[ch]
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
    yel = {BODY_PAL[k] for k in 'Ygdy'}
    ora = {BODY_PAL[k] for k in 'oOl'}
    skin = {BODY_PAL[k] for k in 'sSL'}

    def edge_col(x, y):
        k = cv.p.get((x, y))
        return EDGE_S if k == BODY_PAL['S'] else EDGE_Y if k in (BODY_PAL['d'], BODY_PAL['g']) else K1
    cv.outline(edge_col)
    for (x, y) in dust:
        cv.p[(x + ox, y + oy)] = DUST
    return cv.image(32, 32)


def body_sheet():
    frames = [
        body_cells(0, 'stand'),
        body_cells(1, 'stand'),
        body_cells(1, 'a', 'up', 'down', dust=[(18, 18)]),
        body_cells(0, 'b', 'up', 'down'),
        body_cells(1, 'c', 'down', 'up', dust=[(-1, 18)]),
        body_cells(0, 'b', 'down', 'down'),
    ]
    sheet = Image.new('RGBA', (192, 32), (0, 0, 0, 0))
    for i, (c, d) in enumerate(frames):
        sheet.paste(body_frame(c, d), (i * 32, 0))
    out = os.path.join(CHARS, 'foreman.png')
    save_png(sheet, out)
    meta_copy(os.path.join(CHARS, 'cartographer.png.meta'), out, 'Characters/foreman', sprite_names=('cartographer', 'foreman'))
    return sheet


# ================================================================== 2. 손에 든 것 · 전용 무기
def C(r, g, b):
    return (r, g, b, 255)


for _k, _v in {'E': (242, 228, 84), 'F': (255, 247, 150), 'D': (196, 160, 34), 'M': (30, 24, 38), 'T': (214, 164, 108), 't': (112, 76, 50),
               'P': (150, 154, 164), 'p': (104, 108, 124)}.items():
    KW.PAL[_k] = C(*_v)


def weapon_out(name, pic, pivot_cell, template_base='sword', also=()):
    pic.outline()
    im, x0, y0 = pic.image()
    px = (pivot_cell[0] - x0 + 0.5) / im.w
    py = 1.0 - (pivot_cell[1] - y0 + 0.5) / im.h
    for nm in (name,) + tuple(also):
        path = os.path.join(WEAP, nm + '.png')
        KW.png.write(im, path)
        KW.write_meta(path, template_base, (round(px, 3), round(py, 3)))
    return im.w, im.h


def w_plow():           # 사다리꼴 삽날 — 자루가 왼쪽, 오른쪽으로 넓어지는 노랑 판 · 검정 줄무늬 가장자리 · 은색 날끝
    p = KW.Pic()
    for x in range(0, 9):
        p.dot(x, 8, 'L' if x % 3 else 'W')
        p.dot(x, 9, 'w')
    for x in range(8, 17):
        half = 3.2 + (x - 8) * 0.62
        for y in range(0, 18):
            dy = y + 0.5 - 8.5
            if abs(dy) <= half:
                ch = 'E'
                if dy < -half + 1.1:
                    ch = 'F'
                elif dy > half - 1.1:
                    ch = 'D'
                if 12 <= x <= 15:
                    ch = 'M' if ((x + y) // 2) % 2 == 0 else 'E'
                if x == 16:
                    ch = '5' if dy < 0 else '4'
                if x == 8:
                    ch = 'D'
                p.dot(x, y, ch)
    p.dot(10, 8, 'p'); p.dot(10, 9, 'p'); p.dot(10, 7, '2'); p.dot(11, 8, '2')
    return p, (0, 8.5)


def w_bucket():         # 굴착기 버킷 — 노랑 팔 + 이빨 달린 통 (아래쪽 흙)
    p = KW.Pic()
    for x in range(0, 8):
        p.dot(x, 5, 'F' if x % 2 else 'E')
        p.dot(x, 6, 'E')
        p.dot(x, 7, 'D')
    for y in range(3, 12):
        for x in range(8, 17):
            if (x - 8) + (y - 3) * 0.2 <= 8 and not (x > 14 and y < 5):
                ch = 'E'
                if y == 3 or x == 8:
                    ch = 'F'
                if y >= 10:
                    ch = 'D'
                p.dot(x, y, ch)
    for x in range(9, 15):
        p.dot(x, 5, 't'); p.dot(x, 6, 'T') if x % 2 else p.dot(x, 6, 't')
    for x in (9, 11, 13, 15):
        p.dot(x, 12, '5'); p.dot(x, 13, '3'); p.dot(x + 1, 12, '4')
    p.dot(8, 6, 'p'); p.dot(8, 5, 'p')
    return p, (0, 6)


def w_jackhammer():     # 착암기 — 손잡이 둘 + 노랑 몸통 + 은빛 끌 + 떨림 점
    p = KW.Pic()
    p.rows([
        'MM..............',
        'MM..............',
        'MMEEEEEEEE......',
        'MMEFFFFEEED.....',
        'MMEEEEEEEDD54...',
        'MMEDDDDDDDD.5544',
        'MM..pppp........',
        'MM..............',
    ])
    p.dot(15, 3, '6'); p.dot(14, 3, '5'); p.dot(15, 7, '4')
    p.dot(1, 8, 'M'); p.dot(0, 8, 'M')
    return p, (1, 4)


def w_wreckball():      # 철거 쇠공 — 사슬 + 검은 쇠공 (노랑 · 검정 띠)
    p = KW.Pic()
    for i, x in enumerate(range(0, 9)):
        p.dot(x, 7 + (i % 2) * 0, '3' if i % 2 == 0 else '5')
        if i % 2 == 0:
            p.dot(x, 6, '4'); p.dot(x, 8, '2')
    for y in range(0, 14):
        for x in range(8, 22):
            d = math.hypot(x + 0.5 - 15, y + 0.5 - 7.5)
            if d <= 6.4:
                hl = math.hypot(x + 0.5 - 13, y + 0.5 - 5)
                ch = '5' if hl < 1.4 else '3' if hl < 3.2 else '1' if d < 5.0 else 'j'
                if abs((y - 7) + (x - 15) * 0.0) <= 1 and d > 3:
                    ch = 'E' if x % 2 == 0 else 'M'
                p.dot(x, y, ch)
    return p, (0, 7)


def weapons():
    out = []
    out.append(('weapon_plow',) + weapon_out('weapon_plow', *w_plow(), also=()))
    for kid, nm, fn in ((147, 'bucket', w_bucket), (148, 'jackhammer', w_jackhammer), (149, 'wreckball', w_wreckball)):
        out.append(('weapon_kit_%d' % kid,) + weapon_out('weapon_kit_%d' % kid, *fn(), also=('weapon_' + nm,)))
    return out


# ================================================================== 3. 이펙트 (FX)
DRT, DRT_D, DRT_L = (160, 116, 76), (104, 72, 48), (206, 160, 108)
YEL, YEL_D, YEL_L = (242, 228, 84), (196, 160, 34), (255, 247, 150)
BLK = (30, 24, 38)
STL, STL_M, STL_D = (214, 222, 240), (150, 158, 184), (96, 104, 130)
CON, CON_D, CON_L = (160, 164, 174), (104, 108, 124), (214, 218, 228)
WHITE = (255, 250, 232)
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def fade(cv, k):
    for (x, y) in list(cv.p):
        if BAYER[y % 4][x % 4] / 16.0 < k:
            del cv.p[(x, y)]


def fx_out(name, frames, w, h, ppu=16, tiled=False):
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, cv in enumerate(frames):
        sheet.paste(cv.image(w, h), (i * w, 0))
    out = os.path.join(FXD, name + '.png')
    save_png(sheet, out)
    if not os.path.exists(out + '.meta'):
        if tiled:     # 한 장짜리 · FullRect · 반복 (SpriteRenderer Tiled 로 깖)
            t = open(os.path.join(FXD, 'fx_ind_band.png.meta'), encoding='utf-8').read()
            t = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + guid_of('FX/' + name), t, count=1, flags=re.M)
            t = t.replace('fx_ind_band', name)
            t = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, t, count=1)
            r = random.Random('foreman/FX/' + name)
            t = re.sub(r'spriteID: [0-9a-f]{32}', lambda mo: 'spriteID: ' + uuid.UUID(int=r.getrandbits(128)).hex, t)
            open(out + '.meta', 'w', encoding='utf-8', newline='\n').write(t)
        else:
            sheet_meta(name, w, h, len(frames), out + '.meta')
            t = open(out + '.meta', encoding='utf-8').read()
            t = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + guid_of('FX/' + name), t, count=1)
            t = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, t, count=1)
            open(out + '.meta', 'w', encoding='utf-8', newline='\n').write(t)
    return sheet


def dirt_puff(cv, cx, cy, R, n, rr, seed):
    r = random.Random(seed)
    for i in range(n):
        a = TAU * i / n + r.random() * 0.4
        d = R * (0.55 + r.random() * 0.45)
        cv.disc(cx + math.cos(a) * d, cy + math.sin(a) * d * 0.8, rr * (0.8 + r.random() * 0.4), DRT_D)
    for i in range(n):
        a = TAU * i / n + 0.3 * math.sin(i)
        d = R * 0.5
        x, y = cx + math.cos(a) * d, cy + math.sin(a) * d * 0.8
        cv.disc(x - 0.5, y - 0.7, rr * 0.9, DRT)
        cv.disc(x - 1, y - 1.3, rr * 0.45, DRT_L)


def fx_dust():                       # 흙먼지 4프레임
    fr = []
    for f in range(4):
        cv = Cv()
        dirt_puff(cv, 16, 18, 3 + f * 3.0, 8, max(1.6, 4.4 - f * 0.7), 200 + f)
        fade(cv, max(0.0, (f - 1) * 0.3))
        fr.append(cv)
    return fx_out('fx_dust', fr, 32, 32)


def shard(cv, cx, cy, ang, ln, w, c1, c2):
    dx, dy = math.cos(ang), math.sin(ang)
    px, py = -dy, dx
    cv.poly([(cx + px * w, cy + py * w), (cx + dx * ln, cy + dy * ln), (cx - px * w, cy - py * w), (cx - dx * 1.2, cy - dy * 1.2)], c1)
    cv.line(cx, cy, cx + dx * ln * 0.8, cy + dy * ln * 0.8, c2, 1)


def fx_crunch():                     # 빠각 — 별 모양 파편 6프레임
    fr = []
    for f in range(6):
        cv = Cv()
        R = 3 + f * 3.0
        n = 10
        for i in range(n):
            a = TAU * i / n + 0.15 * (i % 2)
            ln = (5.0 if i % 2 == 0 else 3.2) - f * 0.25
            d = R * (1.0 if i % 2 == 0 else 0.75)
            shard(cv, 24 + math.cos(a) * d * 0.5, 24 + math.sin(a) * d * 0.5, a, max(2.0, ln), 1.3 - f * 0.1, STL_M if i % 3 else YEL, STL_D)
        if f < 4:
            cv.disc(24, 24, max(1.5, 7 - f * 1.6), YEL_L)
            cv.disc(24, 24, max(1.0, 4 - f * 1.0), WHITE)
        for i in range(8):
            a = TAU * i / 8 + 0.4 + f * 0.2
            d = R + 4 + f * 0.5
            cv.set(24 + math.cos(a) * d, 24 + math.sin(a) * d, DRT_L if i % 2 else DRT_D)
        fade(cv, max(0.0, (f - 3) * 0.3))
        fr.append(cv)
    return fx_out('fx_crunch', fr, 48, 48)


def fx_rubble():                     # 돌 조각 · 잔해 — 모양 다른 4장 (16×16)
    shapes = [
        [(2, 12), (3, 6), (8, 3), (13, 6), (14, 12), (10, 13)],
        [(1, 11), (5, 4), (11, 4), (14, 9), (12, 13), (4, 13)],
        [(3, 13), (2, 8), (7, 2), (12, 5), (13, 12)],
        [(2, 12), (6, 5), (9, 8), (13, 4), (14, 12)],
    ]
    fr = []
    for k, pts in enumerate(shapes):
        cv = Cv()
        cv.poly(pts, CON_D)
        cv.poly([(x - 0.6, y - 0.8) for x, y in pts], CON)
        cv.poly([(x * 0.55 + 6.5 * 0.45 - 0.5, y * 0.55 + 6 * 0.45 - 0.8) for x, y in pts], CON_L) if k % 2 == 0 else None
        for (x, y) in list(cv.p):
            if cv.p[(x, y)] == CON_L and (x + y) % 3:
                cv.p[(x, y)] = CON
        cv.set(pts[0][0] + 3, pts[0][1] - 4, CON_L)
        if k == 3:                    # 철근 가닥이 삐져나온 조각
            cv.line(8, 5, 11, 1, (170, 96, 52), 1)
        cv.outline(K1)
        fr.append(cv)
    return fx_out('fx_rubble', fr, 16, 16)


def fx_cement():                     # 굳은 적에 얹는 회색 얼룩 2프레임 (24×24)
    fr = []
    for f in range(2):
        cv = Cv()
        r = random.Random(300 + f)
        cv.ell(12, 13, 9, 7, CON_D)
        cv.ell(11.4, 12.4, 8, 6, CON)
        for _ in range(10):
            a = r.random() * TAU
            cv.disc(12 + math.cos(a) * 9.5, 13 + math.sin(a) * 7.5, 1.4, CON)
        for k in range(3):           # 흘러내리는 줄
            x = 6 + k * 6 + f
            cv.rect(x, 18, 2, 3 + (k + f) % 3, CON_D)
        for _ in range(7):
            cv.set(5 + r.random() * 14, 7 + r.random() * 9, CON_L)
        cv.outline(K1)
        fr.append(cv)
    return fx_out('fx_cement', fr, 24, 24)


def fx_landslide():                  # 흙 파도 띠 (한 장 · Tiled · 가로로 이어짐 32×16)
    cv = Cv()
    for x in range(32):
        top = 3.0 + 2.2 * math.sin(x / 32 * TAU * 2 + 0.6) + 1.2 * math.sin(x / 32 * TAU * 4 + 1.1)
        for y in range(16):
            if y >= top:
                depth = y - top
                cv.p[(x, y)] = DRT_L if depth < 1.2 else DRT if depth < 6 else DRT_D
    r = random.Random(77)
    for _ in range(14):
        x, y = r.randrange(32), r.randrange(8, 16)
        if (x, y) in cv.p:
            cv.p[(x, y)] = DRT_D if r.random() < 0.6 else DRT_L
    for x in range(32):              # 위쪽 굴러가는 흙덩이 점
        if x % 8 == 3:
            cv.set(x, 1, DRT)
            cv.set(x + 1, 2, DRT_D)
    return fx_out('fx_landslide', [cv], 32, 16, tiled=True)


def fx_roller():                     # 다져진 자국 타일 (16×16 · 어두운 줄 + 밝은 줄이 번갈아)
    cv = Cv()
    for y in range(16):
        for x in range(16):
            band = (y // 2) % 2
            cv.p[(x, y)] = DRT if band == 0 else DRT_D
            if y % 2 == 0 and band == 0 and (x + y) % 5 == 0:
                cv.p[(x, y)] = DRT_L
    for x in range(0, 16, 4):
        cv.p[(x, 7)] = DRT_D
        cv.p[(x + 1, 15)] = DRT_L
    return fx_out('fx_roller', [cv], 16, 16, tiled=True)


def fx_wreckball():                  # 쇠공 굴림 4프레임 + 발밑 그림자
    fr = []
    for f in range(4):
        cv = Cv()
        cv.ell(16, 25, 9, 3, (0, 0, 0, 115))
        cx, cy, R = 16, 15, 8.2
        cv.disc(cx, cy, R, (40, 44, 64))
        cv.disc(cx - 0.6, cy - 0.8, R - 1.2, (74, 80, 108))
        cv.disc(cx - 2.6, cy - 3.2, 2.4, (150, 158, 184))
        cv.set(cx - 3, cy - 4, WHITE)
        ph = f * TAU / 8                   # 굴러 돌아가는 노랑 띠 · 볼트
        for t in range(14):
            a = ph + t * 0.14 - 1.0
            x, y = cx + math.cos(a) * (R - 1.6), cy + math.sin(a) * (R - 1.6) * 0.95
            if math.cos(a) > -0.1:
                cv.set(x - 0.5, y - 0.5, YEL if t % 4 < 2 else BLK)
        a = ph + 2.2
        cv.disc(cx + math.cos(a) * 4.2, cy + math.sin(a) * 4.2, 1.2, STL_M)
        cv.ring(cx, cy, R - 0.4, R + 0.4, K1) if False else None
        cv.outline(lambda x, y: (0, 0, 0, 115) if cv.p.get((x, y)) == (0, 0, 0, 115) else K1) if False else None
        fr.append(cv)
    # 몸통에만 외곽 (그림자 제외)
    for cv in fr:
        keep = {p: c for p, c in cv.p.items() if len(c) == 3}
        add = {}
        for (x, y) in keep:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                q = (x + dx, y + dy)
                if q not in keep:
                    add[q] = K1
        cv.p.update(add)
    return fx_out('fx_wreckball', fr, 32, 32)


def barricade_parts(cv, dx=0, tilt=0, down=False):
    if not down:
        for x0 in (9, 41):             # A자 다리
            cv.line(x0 + dx, 22, x0 + 2 + dx, 5 + tilt, (210, 214, 224), 3)
            cv.line(x0 + 1 + dx, 22, x0 + 3 + dx, 5 + tilt, STL_D, 1)
        for (y0, c) in ((5, 0), (12, 1)):
            for y in range(y0 + tilt, y0 + 5 + tilt):
                for x in range(3, 49):
                    k = ((x + y) // 3) % 2
                    cv.p[(x + dx, y)] = YEL if (k == c) else BLK
            for x in range(3, 49):
                cv.p[(x + dx, y0 + tilt)] = YEL_L if cv.p[(x + dx, y0 + tilt)] == YEL else cv.p[(x + dx, y0 + tilt)]
        cv.rect(22 + dx, 19, 8, 3, STL_M)
        cv.rect(23 + dx, 19, 6, 1, STL)
    else:                              # 무너짐: 널빤지가 바닥에 흩어짐
        for (x0, y0, ang) in ((4, 15, 0.12), (24, 19, -0.08)):
            for i in range(24):
                for j in range(5):
                    x, y = x0 + i, y0 + j + i * ang
                    k = ((i + j) // 3) % 2
                    cv.set(x, y, YEL if k == 0 else BLK)
        cv.line(9, 18, 14, 22, (210, 214, 224), 3)
        cv.line(38, 13, 42, 20, (210, 214, 224), 3)
        cv.rect(33, 21, 8, 2, STL_M)


def fx_barricade():                  # 「공사 중」 바리케이드 3프레임: 서 있음 · 흔들림 · 무너짐 (52×24)
    fr = []
    for f in range(3):
        cv = Cv()
        if f == 0:
            barricade_parts(cv)
        elif f == 1:
            barricade_parts(cv, dx=1, tilt=1)
        else:
            barricade_parts(cv, down=True)
            dirt_puff(cv, 26, 19, 14, 7, 2.2, 410)
            fade(cv, 0.0)
        cv.outline(K1)
        fr.append(cv)
    return fx_out('fx_barricade', fr, 52, 24)


def fx_rebar():                      # 철근 투사체 2프레임 (24×8 · 오른쪽으로 날아감)
    fr = []
    for f in range(2):
        cv = Cv()
        for x in range(1, 22):
            rib = ((x + f * 2) // 2) % 2 == 0
            cv.p[(x, 3)] = STL
            cv.p[(x, 4)] = STL_M if not rib else STL_D
            if rib:
                cv.p[(x, 2)] = STL_M
                cv.p[(x, 5)] = STL_D
        cv.poly([(22, 2), (24, 3.5), (22, 6)], STL)
        for x in range(1, 6):
            cv.set(x, 3, (176, 100, 56))
        cv.outline(K1)
        fr.append(cv)
    return fx_out('fx_rebar', fr, 24, 8)


def fx_tape():                       # 통행 금지 테이프 (한 장 · Tiled · 16×8)
    cv = Cv()
    for y in range(8):
        for x in range(16):
            cv.p[(x, y)] = YEL if ((x - y) % 8) < 4 else BLK
    for x in range(16):
        cv.p[(x, 0)] = YEL_L if cv.p[(x, 0)] == YEL else cv.p[(x, 0)]
    return fx_out('fx_tape', [cv], 16, 8, tiled=True)


def fx_bucket():                     # 굴착기 버킷 내려찍기 4프레임 (48×48): 높이 · 내려옴 · 쾅 · 먼지
    fr = []

    def bucket(cv, cx, cy, k=1.0):
        cv.line(cx + 6, cy - 18, cx + 2, cy - 4, YEL, 5)
        cv.line(cx + 7, cy - 18, cx + 3, cy - 4, YEL_D, 1)
        pts = [(cx - 13 * k, cy - 6), (cx + 11 * k, cy - 6), (cx + 13 * k, cy + 3), (cx - 12 * k, cy + 5)]
        cv.poly(pts, YEL)
        cv.poly([(cx - 13 * k, cy - 6), (cx + 11 * k, cy - 6), (cx + 11 * k, cy - 3), (cx - 13 * k, cy - 3)], YEL_L)
        cv.poly([(cx - 12 * k, cy + 2), (cx + 13 * k, cy + 1), (cx + 13 * k, cy + 3), (cx - 12 * k, cy + 5)], YEL_D)
        for i in range(5):
            x = cx - 12 * k + i * 6 * k
            cv.poly([(x, cy + 4), (x + 4, cy + 4), (x + 2, cy + 9)], STL)
        cv.line(cx - 9, cy - 1, cx + 7, cy - 1, BLK, 1)
        cv.outline(K1)
    for f in range(4):
        cv = Cv()
        sh = [4, 7, 11, 13][f]
        cv.ell(24, 40, sh, sh * 0.3, (0, 0, 0, 100))
        if f == 0:
            bucket(cv, 22, 14)
        elif f == 1:
            bucket(cv, 24, 26)
        elif f == 2:
            bucket(cv, 24, 33)
            dirt_puff(cv, 24, 40, 10, 8, 3.2, 500)
        else:
            dirt_puff(cv, 24, 38, 16, 9, 3.4, 510)
            for a in (0.4, 1.2, 1.9, 2.7):
                shard(cv, 24, 38, -a, 8, 1, STL_M, STL_D)
            fade(cv, 0.3)
        fr.append(cv)
    return fx_out('fx_bucket', fr, 48, 48)


def fxs():
    return [fx_dust(), fx_crunch(), fx_rubble(), fx_cement(), fx_landslide(), fx_roller(), fx_wreckball(), fx_barricade(), fx_rebar(), fx_tape(),
            fx_bucket()]


# ================================================================== 4. 아이콘 (범주 틀 + 그림문자)
ORG, ORGD, ORGL = (248, 140, 44), (192, 90, 30), (255, 184, 92)
SK, SKS = (242, 188, 150), (202, 126, 112)
RED, REDD, REDL = (222, 60, 70), (150, 30, 50), (255, 130, 120)
GRN, GRND = (140, 220, 140), (62, 140, 92)
GOLD, GOLD_D = (254, 222, 108), (200, 140, 40)
BRN, BRND = (170, 112, 66), (104, 66, 44)
EYE = (32, 22, 50)
WHT = (250, 250, 244)
BRICK, BRICKD, BRICKL = (196, 96, 72), (130, 58, 50), (232, 140, 106)


def foe(cv, cx, cy, r, tilt=0):
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


def star(cv, cx, cy, n, long_, short_, w, c1, c2):
    for i in range(n):
        a = TAU * i / n
        ln = long_ if i % 2 == 0 else short_
        for t in range(int(ln * 2)):
            d = t / 2.0
            x, y = cx + math.cos(a) * d, cy + math.sin(a) * d
            ww = max(1, int(round(w * (1 - d / (ln + 1)))))
            for j in range(ww):
                cv.set(x + math.cos(a + 1.57) * (j - (ww - 1) / 2) - 0.5, y + math.sin(a + 1.57) * (j - (ww - 1) / 2) - 0.5, c1 if d < ln * 0.55 else c2)


def hazard(cv, x, y, w, h, period=4):                   # 노랑 · 검정 사선 줄무늬 판
    for j in range(h):
        for i in range(w):
            cv.p[(x + i, y + j)] = YEL if ((i - j) // 2) % 2 == 0 else BLK
    for i in range(w):
        cv.p[(x + i, y)] = YEL_L if cv.p[(x + i, y)] == YEL else cv.p[(x + i, y)]


def helmet(cv, cx, cy, k=1.0, col=YEL, hi=YEL_L, dk=YEL_D):    # 안전모 (앞에서)
    cv.ell(cx, cy, 8 * k, 7 * k, dk)
    cv.ell(cx - 0.4, cy - 0.6, 7.2 * k, 6.2 * k, col)
    cv.rect(int(cx - 9 * k), int(cy + 2 * k), int(18 * k), int(3 * k), dk)
    cv.rect(int(cx - 9 * k), int(cy + 2 * k), int(18 * k), 1, col)
    cv.rect(int(cx - 1), int(cy - 7 * k), 2, int(8 * k), dk)
    cv.ell(cx - 3 * k, cy - 3 * k, 2.4 * k, 1.6 * k, hi)


def pile(cv, cx, cy):                                  # 적 더미 (작은 초록 덩어리 셋)
    foe(cv, cx - 5, cy + 1, 4)
    foe(cv, cx + 5, cy + 1, 4)
    foe(cv, cx, cy - 4, 4)


def blade(cv, x, y, w, h, flare=True, col=YEL, dk=YEL_D, hi=YEL_L):   # 사다리꼴 삽날 (오른쪽이 넓음 = 날 방향)
    cv.poly([(x, y + h * 0.3), (x + w, y), (x + w, y + h), (x, y + h * 0.7)], col)
    cv.poly([(x, y + h * 0.3), (x + w, y), (x + w, y + h * 0.18), (x, y + h * 0.42)], hi)
    cv.poly([(x, y + h * 0.58), (x + w, y + h * 0.82), (x + w, y + h), (x, y + h * 0.7)], dk)
    for j in range(int(h)):
        cv.set(x + w - 1, y + j, STL if j % 2 == 0 else STL_M)


# ---- 특수 능력 547~553
def g547(cv):     # 굴착기 버킷 (노랑 팔 + 이빨 통 + 흙)
    cv.line(25, 4, 15, 13, YEL, 4)
    cv.line(26, 5, 16, 14, YEL_D, 1)
    cv.poly([(5, 13), (19, 11), (22, 20), (20, 23), (7, 23)], YEL)
    cv.poly([(5, 13), (19, 11), (19, 13.5), (5, 15.5)], YEL_L)
    cv.poly([(7, 20), (22, 19), (20, 23), (7, 23)], YEL_D)
    cv.poly([(6, 15), (18, 14), (18, 17), (7, 18)], DRT)
    for k in range(5):
        cv.poly([(6 + k * 3.6, 23), (9 + k * 3.6, 23), (7.5 + k * 3.6, 27)], STL)
    cv.line(8, 20, 18, 20, BLK)
    for (x, y) in ((8, 9), (13, 8), (19, 7), (11, 5)):
        cv.set(x, y, DRT); cv.set(x + 1, y + 1, DRT_D)


def g548(cv):     # 착암기 (손잡이 둘 · 몸통 · 끌 · 떨림 선)
    cv.rect(8, 4, 16, 3, BLK); cv.rect(7, 3, 3, 5, BLK); cv.rect(22, 3, 3, 5, BLK)
    cv.rect(13, 7, 6, 10, YEL); cv.rect(13, 7, 2, 10, YEL_L); cv.rect(17, 7, 2, 10, YEL_D)
    cv.rect(12, 17, 8, 3, YEL_D); cv.rect(14, 20, 4, 2, STL_D)
    cv.poly([(14, 22), (18, 22), (16, 28)], STL)
    cv.line(16, 22, 16, 27, STL_M)
    for sgn in (-1, 1):
        for k in range(3):
            cv.line(16 + sgn * (7 + k), 10 + k, 16 + sgn * (7 + k), 13 + k, STL if k % 2 == 0 else STL_M)
    for x, y in ((9, 27), (22, 26), (12, 29), (20, 29)):
        cv.set(x, y, DRT); cv.set(x + 1, y, DRT_D)


def g549(cv):     # 철거 쇠공 (사슬 + 검은 쇠공)
    for i in range(5):
        x, y = 22 - i * 1.5, 3 + i * 3
        cv.rect(int(x) - 1, int(y), 3, 2, STL if i % 2 == 0 else STL_M)
    cv.disc(14, 19, 9, (36, 40, 60))
    cv.disc(13.4, 18.4, 8, (70, 76, 104))
    cv.disc(10, 14.5, 3, (150, 158, 184))
    cv.set(9, 13, WHITE)
    for k in range(7):
        cv.set(7 + k * 2, 22 - (k % 2) * 0 + (k // 4), YEL if k % 2 == 0 else BLK)
    cv.rect(21, 1, 5, 3, STL_D)
    for x, y in ((25, 22), (27, 18), (4, 27)):
        cv.set(x, y, DRT_L); cv.set(x + 1, y + 1, DRT)


def g550(cv):     # 안전화 (갈색 장화 + 은색 앞코 + 노랑 끈)
    cv.poly([(10, 4), (19, 4), (19, 19), (27, 21), (27, 27), (8, 27), (8, 14)], BRN)
    cv.poly([(10, 4), (13, 4), (13, 19), (8, 19), (8, 14)], (206, 150, 100))
    cv.rect(8, 24, 20, 4, BRND)
    cv.poly([(21, 19), (27, 21), (27, 24), (20, 24)], STL)
    cv.rect(10, 5, 9, 2, BRND)
    for y in (9, 12, 15):
        cv.rect(13, y, 5, 1, YEL)
    cv.rect(10, 21, 11, 1, BRND)


def g551(cv):     # 현장 소장 (흰 안전모 + 도면 판)
    cv.rect(17, 13, 10, 14, (206, 164, 108)); cv.rect(18, 14, 8, 12, WHT)
    cv.rect(20, 11, 4, 3, STL_D)
    for y in (17, 20, 23):
        cv.rect(19, y, 6, 1, STL_M)
    cv.rect(19, 24, 3, 1, RED)
    helmet(cv, 12, 14, 0.95, WHT, WHT, (190, 196, 214))
    cv.rect(8, 13, 9, 2, YEL)
    cv.rect(6, 21, 11, 5, (96, 106, 134)); cv.rect(10, 21, 3, 5, ORG)


def g552(cv):     # 무너진 담 (벽돌 담이 무너지며 조각이 떨어짐)
    for r in range(4):
        y = 6 + r * 4
        off = 0 if r % 2 == 0 else 3
        for x in range(4 - off, 26, 6):
            if r == 0 and x > 14:
                continue
            cv.rect(max(4, x), y, 5, 3, BRICK)
            cv.rect(max(4, x), y, 5, 1, BRICKL)
            cv.rect(max(4, x), y + 2, 5, 1, BRICKD)
    for (x, y, a) in ((20, 6, 0.6), (24, 12, 1.1)):
        cv.rect(x, y, 5, 3, BRICK); cv.rect(x, y, 5, 1, BRICKL); cv.rect(x, y + 2, 5, 1, BRICKD)
    cv.rect(4, 23, 24, 4, DRT)
    cv.rect(4, 26, 24, 1, DRT_D)
    for x in range(5, 27, 4):
        cv.set(x, 23, DRT_L)
    cv.ell(24, 25, 4, 2, CON)


def g553(cv):     # 새참 (김 나는 따끈한 빵 + 보온병)
    cv.rect(18, 8, 7, 17, RED); cv.rect(18, 8, 2, 17, REDL); cv.rect(23, 8, 2, 17, REDD)
    cv.rect(18, 6, 7, 3, STL); cv.rect(19, 4, 5, 2, STL_D)
    cv.rect(18, 15, 7, 2, WHT)
    cv.ell(11, 20, 7, 5, (214, 150, 80))
    cv.ell(10.4, 19, 6, 3.8, (240, 190, 112))
    cv.ell(9, 17.6, 2.6, 1.4, (255, 226, 160))
    for x, y in ((8, 20), (11, 21), (14, 20)):
        cv.set(x, y, (176, 112, 60))
    for x, y in ((8, 11), (11, 8), (9, 6), (12, 12)):
        cv.set(x, y, WHT); cv.set(x, y + 1, WHT)


# ---- 전용 카드 750~755
def g750(cv):     # 긴 삽날 (옆으로 늘어나는 삽날)
    blade(cv, 8, 10, 15, 12)
    arrow_head(cv, 4, 16, -1, 0, YEL_L, 3); arrow_head(cv, 27, 16, 1, 0, YEL_L, 3)
    cv.line(5, 16, 9, 16, YEL_L); cv.line(22, 16, 26, 16, YEL_L)
    cv.rect(3, 26, 24, 1, DRT_D)


def g751(cv):     # 잔해 (더미 속 돌 조각 + 철근)
    cv.line(8, 6, 12, 17, (190, 104, 56), 2); cv.line(21, 5, 18, 16, (190, 104, 56), 2)
    for (x, y, r, c) in ((11, 21, 6, CON), (20, 22, 5.5, CON_D), (16, 17, 4.4, CON_L), (7, 25, 3.4, CON_D), (25, 26, 3.2, CON)):
        cv.disc(x, y, r, CON_D if c != CON_D else (74, 78, 96))
        cv.disc(x - 0.6, y - 0.8, r - 1.2, c)
    cv.set(9, 19, CON_L); cv.set(18, 20, CON_L)
    for x in (4, 27):
        cv.set(x, 12, GOLD); cv.set(x - 1, 11, GOLD)


def g752(cv):     # 빠각! (더미가 벽에 처박힘)
    cv.rect(19, 3, 9, 26, BRICK)
    for y in range(4, 28, 4):
        cv.rect(19, y, 9, 1, BRICKD)
    for x, y in ((22, 7), (25, 11), (22, 15), (25, 19), (22, 23)):
        cv.rect(x, y, 1, 3, BRICKD)
    cv.rect(19, 3, 1, 26, BRICKL)
    pile(cv, 10, 16)
    star(cv, 18, 16, 8, 6, 3.4, 2, YEL_L, YEL)
    cv.disc(18, 16, 1.8, WHT)
    cv.line(2, 22, 6, 24, YEL_D)


def g753(cv):     # 공사 구간 (줄무늬 바리케이드 + 테이프)
    for x0 in (7, 22):
        cv.line(x0, 26, x0 + 1, 11, STL, 3)
        cv.line(x0 + 1, 26, x0 + 2, 11, STL_D, 1)
    hazard(cv, 3, 8, 26, 5)
    hazard(cv, 3, 15, 26, 5)
    cv.rect(3, 26, 26, 1, DRT_D)
    cv.set(5, 4, YEL_L); cv.set(6, 5, YEL_L); cv.set(26, 4, YEL_L)


def g754(cv):     # 안전 수칙 (노랑 방패가 탄을 막음)
    cv.poly([(8, 5), (24, 5), (24, 16), (16, 27), (8, 16)], YEL_D)
    cv.poly([(10, 7), (22, 7), (22, 15), (16, 23), (10, 15)], YEL)
    cv.poly([(10, 7), (16, 7), (16, 23), (10, 15)], YEL_L)
    cv.rect(14, 10, 4, 10, BLK); cv.rect(12, 12, 8, 3, BLK)
    for (x, y) in ((27, 9), (28, 14), (27, 19)):
        cv.disc(x, y, 1.4, RED)
    for (x, y) in ((4, 7),):
        cv.disc(x, y, 1.4, RED)


def g755(cv):     # 적재함 확장 (더 크게 쏟아지는 덤프)
    cv.poly([(4, 10), (18, 14), (16, 24), (4, 24)], YEL)
    cv.poly([(4, 10), (18, 14), (18, 16), (4, 12)], YEL_L)
    cv.poly([(4, 21), (16, 22), (16, 24), (4, 24)], YEL_D)
    cv.rect(4, 24, 14, 2, BLK)
    cv.disc(8, 27, 2.4, BLK); cv.disc(8, 27, 1, STL_M)
    cv.disc(15, 27, 2.4, BLK); cv.disc(15, 27, 1, STL_M)
    for (x, y, r) in ((21, 17, 3), (25, 21, 3.2), (22, 24, 2.6), (27, 25, 2)):
        cv.disc(x, y, r, DRT_D); cv.disc(x - 0.4, y - 0.4, r - 0.9, DRT)
    arrow_head(cv, 27, 9, 1, 0, YEL_L, 3); cv.line(19, 9, 26, 9, YEL_L)
    arrow_head(cv, 14, 5, 0, -1, YEL_L, 3); cv.line(14, 6, 14, 10, YEL_L)


# ---- 고유 스킬 850~859
def g850(cv):     # 야근 수당 (초승달 + 동전)
    cv.disc(12, 12, 8, (200, 206, 240)); cv.disc(16, 10, 7.2, (22, 17, 32))
    cv.disc(10.6, 11, 6.4, (236, 240, 255)) if False else None
    for (x, y) in ((24, 6), (27, 12), (22, 3)):
        cv.set(x, y, GOLD)
    for k in range(3):
        cv.ell(21, 25 - k * 3, 7, 2.6, GOLD_D)
        cv.ell(21, 24 - k * 3, 7, 2.4, GOLD)
    cv.rect(16, 17, 1, 1, WHT)
    cv.rect(19, 22, 4, 1, WHT)


def g851(cv):     # 철근 (앞으로 꽂히는 세 가닥)
    for k, y in enumerate((9, 16, 23)):
        for x in range(3, 24):
            rib = ((x + k) // 2) % 2
            cv.set(x, y, STL); cv.set(x, y + 1, STL_D if rib else STL_M)
        cv.poly([(23, y - 1), (28, y + 0.5), (23, y + 2)], STL)
        cv.rect(3, y, 4, 2, (186, 104, 58))
    star(cv, 29, 16, 6, 3, 2, 1, YEL_L, YEL)


def g852(cv):     # 롤러 자국 (다짐 롤러 + 다져진 줄)
    cv.rect(4, 22, 24, 2, DRT_D); cv.rect(4, 25, 24, 1, DRT); cv.rect(4, 27, 24, 1, DRT_D)
    cv.ell(16, 15, 12, 6, STL_D)
    cv.rect(4, 9, 24, 12, STL_M)
    cv.rect(4, 9, 24, 3, STL)
    cv.rect(4, 18, 24, 3, STL_D)
    cv.ell(5, 15, 2.2, 6, STL_D); cv.ell(27, 15, 2.2, 6, STL_M)
    for x in range(7, 27, 4):
        cv.rect(x, 12, 1, 6, STL_D)
    cv.rect(13, 4, 6, 5, YEL); cv.rect(13, 4, 6, 1, YEL_L); cv.rect(11, 8, 10, 2, BLK)


def g853(cv):     # 크레인 (고리가 적을 끌어당김)
    cv.rect(14, 2, 4, 3, STL_D)
    for i in range(4):
        cv.rect(15, 5 + i * 2, 2, 2, STL if i % 2 == 0 else STL_M)
    arc(cv, 16, 18, 7, -0.2, 3.5, STL, 3)
    cv.rect(17, 11, 4, 3, STL)
    cv.rect(8, 21, 4, 2, STL_M)
    foe(cv, 5, 25, 3.4); foe(cv, 27, 25, 3.4); foe(cv, 16, 27, 3)
    arrow_head(cv, 10, 24, 1, -1, YEL_L, 3); arrow_head(cv, 22, 24, -1, -1, YEL_L, 3)


def g854(cv):     # 안전 제일 (노랑 안전모 + 하얀 십자)
    helmet(cv, 16, 14, 1.15)
    cv.rect(14, 12, 4, 8, (240, 250, 244)) if False else None
    cv.rect(10, 22, 12, 5, WHT); cv.rect(14, 22, 4, 5, GRND)
    cv.rect(12, 24, 8, 1, GRND)
    cv.rect(10, 22, 12, 1, (200, 206, 214))


def g855(cv):     # 시멘트 (시멘트 포대 + 흙손)
    cv.poly([(6, 8), (22, 8), (24, 26), (4, 26)], (196, 192, 180))
    cv.poly([(6, 8), (12, 8), (11, 26), (4, 26)], (226, 222, 210))
    cv.poly([(18, 8), (22, 8), (24, 26), (17, 26)], (150, 146, 136))
    cv.rect(8, 14, 12, 6, BLK)
    cv.rect(10, 16, 8, 2, YEL)
    cv.rect(6, 6, 16, 3, (170, 166, 156))
    cv.poly([(22, 15), (28, 18), (22, 23)], STL); cv.line(22, 19, 28, 25, BRND, 2)
    cv.ell(24, 27, 5, 2, CON)


def g856(cv):     # 과적 (넘쳐 굴러떨어지는 더미)
    cv.rect(3, 22, 26, 5, BLK); cv.rect(3, 22, 26, 1, STL_M)
    foe(cv, 9, 17, 5); foe(cv, 16, 17, 5); foe(cv, 23, 17, 5)
    foe(cv, 12, 9, 4.4); foe(cv, 20, 9, 4.4)
    foe(cv, 25, 3, 3.2)
    arrow_head(cv, 28, 10, 0, 1, YEL_L, 3); cv.line(28, 4, 28, 9, YEL_L)
    cv.disc(6, 29, 2, STL_D); cv.disc(26, 29, 2, STL_D)


def g857(cv):     # 우회로 (노랑 마름모 표지판 + 휘는 화살표)
    cv.poly([(16, 3), (29, 16), (16, 29), (3, 16)], BLK)
    cv.poly([(16, 5), (27, 16), (16, 27), (5, 16)], YEL)
    cv.poly([(16, 5), (27, 16), (24, 16), (16, 8)], YEL_L)
    arc(cv, 17, 20, 5, -2.6, -0.3, BLK, 3)
    arrow_head(cv, 21.5, 14.8, 1, 0, BLK, 3)
    cv.rect(15, 20, 3, 4, BLK)


def g858(cv):     # 자재 회수 (잔해 위로 튀는 동전)
    for (x, y, r, c) in ((9, 24, 5, CON), (18, 25, 5, CON_D), (25, 26, 3.4, CON), (13, 20, 3.4, CON_L)):
        cv.disc(x, y, r, (74, 78, 96)); cv.disc(x - 0.6, y - 0.8, r - 1.2, c)
    cv.ell(16, 10, 7, 7, GOLD_D); cv.ell(15.6, 9.6, 6, 6, GOLD)
    cv.rect(15, 5, 2, 9, GOLD_D); cv.rect(13, 7, 6, 1, GOLD_D); cv.rect(13, 11, 6, 1, GOLD_D)
    cv.ell(13.6, 7, 2, 1.4, WHT)
    for (x, y) in ((6, 8), (26, 7), (27, 14)):
        cv.set(x, y, GOLD); cv.set(x, y + 1, GOLD)


def g859(cv):     # 추후 업데이트 예정 (공사 중 표지판 + 차오르는 공정률)
    cv.rect(15, 24, 3, 5, STL_D)
    hazard(cv, 4, 4, 24, 3); hazard(cv, 4, 19, 24, 3)
    cv.rect(4, 4, 2, 18, YEL); cv.rect(26, 4, 2, 18, YEL)
    cv.rect(6, 7, 20, 12, YEL_L)
    cv.rect(8, 10, 16, 5, BLK)
    cv.rect(9, 11, 9, 3, ORG)
    cv.rect(9, 11, 9, 1, ORGL)
    for x in (10, 13, 16):
        cv.set(x, 17, BLK)


# ---- 스킬 진화 950~956 (육각)
def g950(cv):     # 불도저 (노랑 몸 + 앞 삽날 + 캐터필러)
    cv.rect(10, 8, 11, 8, YEL); cv.rect(10, 8, 11, 2, YEL_L)
    cv.rect(12, 10, 5, 4, STL); cv.rect(12, 10, 5, 1, WHT)
    cv.rect(4, 15, 22, 3, YEL_D)
    cv.rect(5, 18, 19, 5, BLK)
    for x in range(6, 24, 3):
        cv.rect(x, 19, 1, 3, STL_D)
    for x in (7, 14, 21):
        cv.disc(x, 20.5, 2.2, STL_M)
    cv.poly([(25, 8), (29, 6), (29, 24), (25, 22)], STL)
    cv.poly([(25, 8), (29, 6), (29, 10), (25, 11)], WHT)
    cv.line(21, 12, 26, 13, STL_D, 2)
    cv.rect(24, 3, 2, 5, BLK)


def g951(cv):     # 덤프 트럭 (뒤가 들린 짐칸에서 흙이 왼쪽으로 쏟아짐 · 오른쪽에 운전석)
    cv.poly([(5, 5), (19, 11), (19, 20), (5, 20)], YEL)
    cv.poly([(5, 5), (19, 11), (19, 13), (5, 7)], YEL_L)
    cv.poly([(5, 17), (19, 18), (19, 20), (5, 20)], YEL_D)
    cv.poly([(7, 9), (17, 13), (17, 17), (7, 17)], DRT)
    cv.poly([(7, 9), (17, 13), (17, 14), (7, 10)], DRT_L)
    cv.rect(20, 12, 8, 8, ORG); cv.rect(20, 12, 8, 2, ORGL)
    cv.rect(24, 14, 4, 4, (150, 214, 250)); cv.set(24, 14, WHT)
    cv.rect(3, 20, 26, 3, BLK)
    for (x, y, r) in ((3, 17, 2.4), (2, 22, 2.2), (6, 25, 1.8)):
        cv.disc(x, y, r, DRT_D); cv.disc(x - 0.4, y - 0.4, r - 0.8, DRT)
    cv.disc(9, 25, 3, BLK); cv.disc(9, 25, 1.2, STL_M); cv.disc(24, 25, 3, BLK); cv.disc(24, 25, 1.2, STL_M)


def g952(cv):     # 콘크리트 (레미콘 믹서 통 + 흘러나오는 시멘트)
    cv.poly([(4, 8), (22, 5), (22, 18), (4, 19)], CON)
    cv.poly([(4, 8), (22, 5), (22, 8), (4, 11)], CON_L)
    cv.poly([(4, 16), (22, 15), (22, 18), (4, 19)], CON_D)
    for k in range(4):
        x = 6 + k * 4.5
        cv.line(x, 10 - k * 0.6, x + 3, 17 - k * 0.6, YEL, 2)
    cv.poly([(22, 8), (27, 13), (27, 17), (22, 16)], CON_D)
    cv.rect(26, 17, 3, 8, CON); cv.rect(26, 24, 3, 2, CON_D)
    cv.rect(8, 21, 11, 3, STL_D); cv.rect(6, 24, 15, 3, BLK)
    cv.disc(8, 27, 2.2, BLK); cv.disc(18, 27, 2.2, BLK)


def g953(cv):     # 통행 금지 (붉은 금지 표지 + 테이프)
    cv.disc(16, 15, 11, REDD); cv.disc(16, 15, 9.4, RED)
    cv.rect(8, 13, 16, 5, WHT); cv.rect(8, 17, 16, 1, (210, 214, 222))
    hazard(cv, 2, 24, 28, 4)
    cv.set(9, 8, REDL); cv.set(10, 7, REDL); cv.set(11, 6, REDL)


DIG = {'0': ['111', '101', '101', '101', '111'], '1': ['010', '110', '010', '010', '111']}


def digit(cv, x, y, ch, c):
    for j, row in enumerate(DIG[ch]):
        for i, v in enumerate(row):
            if v == '1':
                cv.rect(x + i * 2, y + j * 2, 2, 2, c)


def g954(cv):     # 무재해 100일 (초록 안전 표지판 + 100)
    cv.rect(4, 4, 24, 20, (36, 120, 84)); cv.rect(4, 4, 24, 2, GRN); cv.rect(4, 22, 24, 2, (22, 78, 56))
    cv.rect(3, 3, 26, 1, WHT); cv.rect(3, 24, 26, 1, WHT); cv.rect(3, 3, 1, 22, WHT); cv.rect(28, 3, 1, 22, WHT)
    digit(cv, 6, 8, '1', WHT); digit(cv, 13, 8, '0', WHT); digit(cv, 20, 8, '0', WHT)
    cv.rect(13, 24, 6, 5, STL_D)
    cv.rect(11, 20, 10, 1, GOLD)


def g955(cv):     # 항타기 (쇠 추가 말뚝을 내리찍음)
    cv.rect(13, 2, 6, 4, STL_D)
    cv.rect(15, 5, 2, 8, STL)
    cv.rect(10, 12, 12, 7, BLK); cv.rect(10, 12, 12, 2, STL_M); cv.rect(12, 14, 8, 3, YEL)
    cv.rect(14, 19, 4, 8, BRN); cv.rect(14, 19, 2, 8, (206, 150, 100))
    cv.rect(4, 27, 24, 2, DRT_D); cv.rect(4, 26, 24, 1, DRT)
    star(cv, 16, 27, 8, 9, 4, 2, YEL_L, YEL)
    for x in (4, 27):
        cv.set(x, 14, YEL); cv.set(x, 17, YEL)


def g956(cv):     # 정식 출시 (로켓 + 리본)
    cv.poly([(16, 2), (22, 10), (22, 20), (10, 20), (10, 10)], WHT)
    cv.poly([(16, 2), (10, 10), (10, 20), (14, 20)], (214, 220, 232))
    cv.poly([(16, 2), (19, 6), (13, 6)], YEL)
    cv.disc(16, 12, 3, STL_D); cv.disc(16, 12, 2.2, (110, 200, 240)); cv.set(15, 11, WHT)
    cv.poly([(10, 14), (5, 22), (10, 20)], YEL); cv.poly([(22, 14), (27, 22), (22, 20)], YEL)
    cv.rect(10, 17, 12, 2, BLK) if False else None
    cv.poly([(12, 20), (20, 20), (16, 29)], ORG); cv.poly([(14, 20), (18, 20), (16, 25)], YEL_L)
    for (x, y) in ((4, 8), (27, 9), (6, 16), (25, 4)):
        cv.set(x, y, GOLD); cv.set(x + 1, y + 1, GRN if x % 2 else RED)


# ---- 무기 강화 615~617
def g615(cv):     # 뒷날 (앞날 + 뒤에 달린 작은 날)
    blade(cv, 14, 6, 13, 12)
    cv.poly([(10, 22), (2, 20), (2, 28), (10, 26)], YEL_D) if False else None
    cv.poly([(11, 21), (3, 19), (3, 27), (11, 25)], YEL_D)
    cv.poly([(11, 21), (3, 19), (3, 21), (11, 22.5)], YEL)
    cv.rect(3, 19, 1, 8, STL_M)
    cv.line(11, 18, 15, 14, STL_D, 2)
    arrow_head(cv, 4, 14, -1, 0, YEL_L, 3); cv.line(5, 14, 9, 14, YEL_L)


def g616(cv):     # 유압 실린더 (통 + 피스톤 + 늘어나는 화살표)
    cv.rect(4, 12, 14, 8, YEL); cv.rect(4, 12, 14, 2, YEL_L); cv.rect(4, 18, 14, 2, YEL_D)
    cv.rect(2, 10, 3, 12, STL_D); cv.rect(17, 10, 3, 12, STL_D)
    cv.rect(19, 15, 9, 2, STL); cv.rect(19, 15, 9, 1, WHT)
    cv.rect(27, 12, 3, 8, STL_M)
    cv.rect(8, 14, 6, 4, BLK); cv.rect(9, 15, 4, 2, ORG)
    arrow_head(cv, 28, 6, 1, 0, YEL_L, 3); cv.line(16, 6, 27, 6, YEL_L)
    arrow_head(cv, 4, 26, -1, 0, YEL_L, 3); cv.line(5, 26, 16, 26, YEL_L)


def g617(cv):     # 안전 펜스 (주황 그물 펜스)
    for x0 in (4, 25):
        cv.rect(x0, 4, 3, 24, STL); cv.rect(x0 + 2, 4, 1, 24, STL_D)
    cv.rect(3, 4, 26, 2, STL); cv.rect(3, 26, 26, 2, STL_D)
    for y in range(6, 26):
        for x in range(7, 25):
            if ((x + y) % 4 == 0) or ((x - y) % 4 == 0):
                cv.set(x, y, ORG if (x + y) % 8 else ORGL)
    cv.rect(3, 4, 26, 1, WHT)
    for (x, y) in ((16, 3), (6, 2)):
        cv.set(x, y, YEL_L)


ICONS = [
    (547, 'square', 'weapon', g547), (548, 'square', 'weapon', g548), (549, 'square', 'weapon', g549),
    (550, 'diamond', 'survival', g550), (551, 'diamond', 'survival', g551), (552, 'diamond', 'survival', g552), (553, 'diamond', 'survival', g553),
    (750, 'square', 'weapon', g750), (751, 'square', 'weapon', g751), (752, 'square', 'weapon', g752), (753, 'square', 'weapon', g753),
    (754, 'circle', 'skill', g754), (755, 'circle', 'skill', g755),
    (850, 'circle', 'skill', g850), (851, 'circle', 'skill', g851), (852, 'circle', 'skill', g852), (853, 'circle', 'skill', g853),
    (854, 'circle', 'skill', g854), (855, 'circle', 'skill', g855), (856, 'circle', 'skill', g856), (857, 'circle', 'skill', g857),
    (858, 'circle', 'skill', g858), (859, 'circle', 'skill', g859),
    (950, 'hex', 'soul', g950), (951, 'hex', 'soul', g951), (952, 'hex', 'soul', g952), (953, 'hex', 'soul', g953),
    (954, 'hex', 'soul', g954), (955, 'hex', 'soul', g955), (956, 'hex', 'soul', g956),
    (615, 'square', 'weapon', g615), (616, 'square', 'weapon', g616), (617, 'square', 'weapon', g617),
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
FXNAMES = ['fx_dust', 'fx_crunch', 'fx_rubble', 'fx_cement', 'fx_landslide', 'fx_roller', 'fx_wreckball', 'fx_barricade', 'fx_rebar', 'fx_tape', 'fx_bucket']


def preview(folder, body, weps, icns):
    os.makedirs(folder, exist_ok=True)
    bg = (60, 58, 76, 255)
    sc = 6
    b = Image.new('RGBA', (192, 32), bg); b.alpha_composite(body)
    b.resize((192 * sc, 32 * sc), Image.NEAREST).save(os.path.join(folder, 'body.png'))
    cols = 11
    I = Image.new('RGBA', (cols * 100, ((len(icns) + cols - 1) // cols) * 100), (30, 24, 44, 255))
    for k, (n, im) in enumerate(icns):
        I.alpha_composite(im.resize((96, 96), Image.NEAREST), ((k % cols) * 100 + 2, (k // cols) * 100 + 2))
    I.save(os.path.join(folder, 'icons.png'))
    items = []
    for nm in ('weapon_plow', 'weapon_kit_147', 'weapon_kit_148', 'weapon_kit_149'):
        items.append((Image.open(os.path.join(WEAP, nm + '.png')).convert('RGBA'), 10))
    for nm in FXNAMES:
        items.append((Image.open(os.path.join(FXD, nm + '.png')).convert('RGBA'), 4))
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
