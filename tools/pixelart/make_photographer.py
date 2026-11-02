# 새 영웅 사진사 그림 일체 (docs/plans/2026-11-02-big-update/hero-photographer.md 13절)
#   Characters/photographer.png         32칸 몸 시트 (192×32 · idle 2 + run 4 · 스프라이트 photographer_0~5)
#   Weapons/weapon_camera.png           손에 든 작은 검정 카메라 (위에 플래시 전구)
#   Weapons/weapon_kit_168~170.png      전용 무기 플래시 전구 · 삼각대 카메라 · 영사기 (기획서 이름 weapon_camera__bulb · __tripodcam · __projector 도 같은 그림으로)
#   FX/fx_*.png                         뷰파인더 틀 · 틀 안 흰빛 · 종이가 되는 테 · 종잇조각 · 인화지 · 사본 찢김 · 셔터 커튼 · 종이비행기 · 전구 불꽃 · 삼각대 카메라
#   Icons/ability_<번호>.png            특수 568~574 · 전용 카드 780~785 · 고유 스킬 880~889 · 스킬 진화 980~986 · 강화 624~626
#   범주 틀은 frame_icons.py 의 틀을 그대로 가져옴 (사각 무기 · 원 스킬 · 마름모 패시브 · 육각 진화)
#   .meta 는 이미 있으면 그대로 둠 (다시 돌려도 guid 유지) · 새로 만들 때 guid 는 이름에서 정해짐
# 실행: py -3.14 -X utf8 tools/pixelart/make_photographer.py [미리보기 폴더]
import math, os, random, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from PIL import Image
import frame_icons as FI
import make_kit_weapons as KW
from make_fx import sheet_meta
from make_wrestler import Cv, save_png, fade, BAYER, star_burst, arrow_head, arc, foe  # 붓 · 공용 도우미만 가져옴

ROOT = FI.ROOT
RES = os.path.join(ROOT, 'Assets', 'Resources')
CHARS, WEAP, FXD, ICON = [os.path.join(RES, d) for d in ('Characters', 'Weapons', 'FX', 'Icons')]
K1 = (26, 17, 44)
K2 = (44, 28, 70)                 # Y < 41
TAU = math.tau


def guid_of(key):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'soulsaver/photographer/' + key).hex


def meta_copy(template, out, key, sprite_names=None):
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
        t = t.replace(sprite_names[0], sprite_names[1])
    with open(m, 'w', encoding='utf-8', newline='\n') as f:
        f.write(t)


# ================================================================== 1. 몸 시트
BP = {
    'h': (76, 60, 40), 'H': (126, 96, 60), 's': (232, 180, 142), 'S': (190, 130, 106), 'k': (26, 17, 44),
    'W': (238, 240, 248), 'w': (186, 192, 214), 'd': (124, 130, 162), 'B': (46, 42, 66),
    'P': (52, 54, 80), 'p': (36, 36, 58), 'T': (96, 78, 82),
    'c': (40, 38, 60), 't': (84, 90, 118), 'e': (30, 26, 48), 'r': (108, 116, 142), 'g': (24, 30, 74),
    'F': (200, 212, 255), 'f': (252, 253, 255),
    'a': (138, 150, 190), 'A': (190, 204, 240), 'D': (88, 98, 140),
}
HEAD = [(0, 6, 'hhhhhh'), (1, 5, 'hHHHhhhh'), (2, 5, 'hssssssh'), (3, 5, 'sskssksS'), (4, 5, 'ssssssss'), (5, 6, 'ssssSS')]
LEGS = {
    'stand': [(14, 5, 'PPPPpppP'), (15, 5, 'PPP..pPP'), (16, 5, 'PPP..pPP'), (17, 5, 'PPP..pPP'), (18, 4, 'TTTT..TTTT')],
    'a': [(14, 5, 'PPPPpppP'), (15, 5, 'PPP..pPP'), (16, 5, 'PPP'), (16, 11, 'PPp'), (17, 4, 'TTTT'), (17, 11, 'PPp'), (18, 11, 'TTTT')],
    'b': [(14, 5, 'PPPPpppP'), (15, 6, 'PPPpPP'), (16, 6, 'PPPpPP'), (17, 6, 'PPPpPP'), (18, 5, 'TTTTTTTT')],
    'c': [(14, 5, 'PPPPpppP'), (15, 5, 'PPp..PPP'), (16, 5, 'PPp'), (16, 10, 'PPP'), (17, 5, 'PPp'), (17, 10, 'PPP'), (18, 5, 'TTTT'), (18, 10, 'TTTT')],
}


def body_cells(cam_y, dy, legs, tip):
    c = {}

    def put(rows, ddy=0):
        for y, x0, text in rows:
            for i, ch in enumerate(text):
                if ch != '.':
                    c[(x0 + i, y + ddy)] = BP[ch]
    # 어깨 뒤로 삐죽한 삼각대 (뒤에 깔고 몸이 덮음)
    t = Cv()
    t.line(12, 13, 15 + tip, -1, BP['a'])
    t.line(13, 12, 17 + tip, 2, BP['A'])
    t.line(11, 12, 13 + tip, -2, BP['D'])
    for (x, y), col in t.p.items():
        c[(x, y)] = col
    c[(15 + tip, -2)] = BP['k']; c[(16 + tip, -2)] = BP['k']
    put(LEGS[legs])
    # 위 몸 (dy 만큼 내려감)
    torso = []
    for y in range(6, 14):
        row = ''
        for i in range(10):
            ch = 'W' if (i < 6 and (y <= 9 or i < 2)) else 'w'
            if i == 5:
                ch = 'k' if y in (8, 10) else 'd'
            if i == 9:
                ch = 'd'
            if y == 6 and i in (4, 5):
                ch = 'B'
            if y == 7 and i in (4, 5):
                ch = 'B' if i == 4 else 'd'
            if y == 13:
                ch = 'd' if i % 2 else 'w'
            if y in (11, 12) and i in (1, 2):
                ch = 'w'                                    # 주머니 선
            if y in (9, 10) and i == 2:
                ch = 'w'                                    # 주름
            row += ch
        torso.append((y, 4, row))
    put(torso, dy)
    # 소매 (왼쪽 x2~3 · 오른쪽 x14~15)
    for y in range(6, 10):
        c[(2, y + dy)] = BP['d']; c[(3, y + dy)] = BP['W']
        c[(14, y + dy)] = BP['w']; c[(15, y + dy)] = BP['d']
    put(HEAD, dy)
    c[(4, 3 + dy)] = BP['S']; c[(13, 3 + dy)] = BP['S']       # 귀
    # 카메라 (얼굴 앞 네모)
    cy = cam_y + dy
    put([(cy, 5, 'cttttttc'), (cy + 1, 5, 'ccrrrrcc'), (cy + 2, 5, 'crgFggrc'), (cy + 3, 5, 'crggggrc'), (cy + 4, 5, 'ceeeeeec')])
    put([(cy - 1, 11, 'Ff')])                               # 플래시 전구 (청백)
    for y in (cy + 3, cy + 4):                              # 카메라 양옆 손
        for x in (3, 4, 13, 14):
            c[(x, y)] = BP['s'] if x in (3, 13) else BP['S']
    return c


def body_frame(cells):
    ox, oy = 7, 12
    cv = Cv()
    for (x, y), col in cells.items():
        cv.p[(x + ox, y + oy)] = col
    shade = {BP['d'], BP['S']}
    cv.outline(lambda x, y: K2 if cv.p.get((x, y)) in shade else K1)          # 그늘 쪽 외곽만 살짝 보랏빛 (한 색 단조로움 줄임)
    return cv.image(32, 32)


def body_sheet():
    frames = [
        body_cells(2, 0, 'stand', 0),          # idle: 카메라를 눈에 댐
        body_cells(4, 0, 'stand', 0),          # idle: 살짝 내림 (눈이 보임)
        body_cells(3, 1, 'a', 1),              # run: 삼각대가 등에서 덜렁
        body_cells(3, 0, 'b', 0),
        body_cells(3, 1, 'c', -1),
        body_cells(3, 0, 'b', 0),
    ]
    sheet = Image.new('RGBA', (192, 32), (0, 0, 0, 0))
    for i, c in enumerate(frames):
        sheet.paste(body_frame(c), (i * 32, 0))
    out = os.path.join(CHARS, 'photographer.png')
    save_png(sheet, out)
    meta_copy(os.path.join(CHARS, 'cartographer.png.meta'), out, 'Characters/photographer', sprite_names=('cartographer', 'photographer'))
    return sheet


# ================================================================== 2. 손에 든 것 · 전용 무기
for _k, _v in {'#': '#c8d4ff', '@': '#fcfdff', '%': '#ffb046', '&': '#9aa4be', '$': '#d63a46', '!': '#6d7a9a'}.items():
    KW.PAL[_k] = KW.hx(_v)


def wmeta(name, pivot):
    path = os.path.join(WEAP, name + '.png')
    meta = path + '.meta'
    if not os.path.exists(meta):                 # guid · 스프라이트 번호를 이름에서 (KW.write_meta 는 있는 meta 를 이어받음)
        t = open(os.path.join(WEAP, 'weapon_sword.png.meta'), encoding='utf-8').read()
        t = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + guid_of('Weapons/' + name), t, count=1, flags=re.M)
        t = re.sub(r'spriteID: [0-9a-f]+', 'spriteID: ' + guid_of('Weapons/sprite/' + name), t, count=1)
        open(meta, 'w', encoding='utf-8', newline='\n').write(t)
    KW.write_meta(path, 'sword', pivot)


def weapon_out(name, pic, pivot_cell, also=()):
    pic.outline()
    im, x0, y0 = pic.image()
    px = (pivot_cell[0] - x0 + 0.5) / im.w
    py = 1.0 - (pivot_cell[1] - y0 + 0.5) / im.h
    for nm in (name,) + tuple(also):
        KW.png.write(im, os.path.join(WEAP, nm + '.png'))
        wmeta(nm, (round(px, 3), round(py, 3)))
    return im.w, im.h


def w_camera():         # 손에 든 카메라 — 검정 몸통 + 오른쪽 렌즈 + 위의 플래시 전구 한 점
    return KW.Pic().rows([
        '...#@.......',
        '.2222222....',
        '.1111111344.',
        '.11111113#43',
        '.1111111344.',
        '.1111111....',
    ]), (1, 3)


def w_bulb():           # 플래시 전구 — 왼쪽 은빛 소켓 + 오른쪽 유리 전구(청백 심 · 필라멘트)
    p = KW.Pic()
    for y in range(11):
        for x in range(11):
            d = math.hypot(x + 0.5 - 5.5, y + 0.5 - 5.5)
            if d <= 5.3:
                hl = math.hypot(x + 0.5 - 3.8, y + 0.5 - 3.6)
                ch = '@' if d < 3.5 else '#'
                p.dot(x + 5, y, ch)
    for (x, y) in ((8, 6), (9, 5), (10, 6), (11, 5)):               # 필라멘트
        p.dot(x, y, '%')
    for x in range(0, 5):
        ch = '2' if x % 2 else '4'
        for y in (3, 4, 5, 6, 7):
            p.dot(x, y, ch)
    p.dot(0, 5, '1')
    return p, (0, 5)


def w_tripodcam():      # 삼각대 카메라 — 세 다리 위에 검정 카메라 상자 (윗부분이 오른쪽을 봄)
    p = KW.Pic()
    p.rows([
        '.2222222.',
        '.1111111#',
        '.11111344',
        '.1111113#',
        '.1111111#',
        '.........'[:9],
    ])
    p.rows(['....1....'], 0, 5)
    for i in range(6):
        p.dot(4 - i // 2 - (1 if i > 4 else 0), 6 + i, '&')
        p.dot(4 + i // 2 + (1 if i > 4 else 0), 6 + i, '!')
        p.dot(4, 6 + i // 2, '4')
    return p, (4, 5)


def w_projector():      # 영사기 — 필름 릴 두 개 + 상자 몸통 + 오른쪽 렌즈 통
    p = KW.Pic()
    for cx in (3.5, 9.5):
        for y in range(8):
            for x in range(8):
                d = math.hypot(x + 0.5 - 4, y + 0.5 - 4)
                if d <= 3.9:
                    ch = '2' if d > 2.8 else '4' if d > 1.2 else '1'
                    p.dot(int(cx - 3.5) + x, y, ch)
    p.rows([
        '2222222222.....',
        '1111111111.....',
        '11111111113...',
        '1111111111344.'[:14],
        '11111111113...',
        '1111111111.....',
    ][0:1] + ['1111111111111'] * 0, 0, 8) if False else None
    for y in range(8, 14):
        for x in range(0, 11):
            p.dot(x, y, '1' if y > 8 else '2')
    for y in (9, 10, 11, 12):
        for x in (11, 12, 13):
            p.dot(x, y, '3' if x == 11 else '4')
    p.dot(13, 10, '#'); p.dot(13, 11, '#')
    for x in (2, 5, 8):
        p.dot(x, 11, '#')
    return p, (1, 11)


def weapons():
    out = [('weapon_camera',) + weapon_out('weapon_camera', *w_camera())]
    for kid, nm, fn in ((168, 'bulb', w_bulb), (169, 'tripodcam', w_tripodcam), (170, 'projector', w_projector)):
        out.append(('weapon_kit_%d' % kid,) + weapon_out('weapon_kit_%d' % kid, *fn(), also=('weapon_camera__' + nm,)))
    return out


# ================================================================== 3. 이펙트 (FX)
FL, FLL, FLD, WHT = (200, 212, 255), (232, 238, 255), (112, 130, 212), (255, 255, 255)
PAPER, PAPG, PAPD = (246, 246, 250), (170, 174, 194), (112, 116, 140)
CAMB, CAML = (52, 50, 72), (116, 122, 150)


def fx_out(name, frames, w, h, ppu=16):
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, cv in enumerate(frames):
        sheet.paste(cv.image(w, h), (i * w, 0))
    out = os.path.join(FXD, name + '.png')
    save_png(sheet, out)
    if not os.path.exists(out + '.meta'):
        sheet_meta(name, w, h, len(frames), out + '.meta')
        t = open(out + '.meta', encoding='utf-8').read()
        t = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + guid_of('FX/' + name), t, count=1, flags=re.M)
        t = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, t, count=1)
        open(out + '.meta', 'w', encoding='utf-8', newline='\n').write(t)
    return sheet


def brackets(cv, x0, y0, x1, y1, c, c2=None, L=8, t=2):
    for (cx, cy, sx, sy) in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        for i in range(L):
            for j in range(t):
                cv.set(cx + sx * i + (0 if sx > 0 else -1), cy + sy * j + (0 if sy > 0 else -1), c if j == 0 or c2 is None else c2)
                cv.set(cx + sx * j + (0 if sx > 0 else -1), cy + sy * i + (0 if sy > 0 else -1), c if j == 0 or c2 is None else c2)


def fx_viewfinder():             # 뷰파인더 틀 64×48 (4:3) — 꺾쇠 네 모서리 + 가운데 초점 원 (코드가 도트 선으로도 그림 · 이 그림은 대체/도안)
    cv = Cv()
    brackets(cv, 1, 1, 63, 47, FL, FLD, L=10, t=2)
    for k in range(16):                                   # 초점 원 (끊긴 점선)
        a = TAU * k / 16
        if k % 2 == 0:
            for rr in (7.0, 6.0):
                cv.set(32 + math.cos(a) * rr - 0.5, 24 + math.sin(a) * rr - 0.5, FL if rr == 7.0 else FLD)
    for dx, dy in ((-11, 0), (10, 0), (0, -11), (0, 10)):   # 십자 눈금
        cv.set(32 + dx, 24 + dy, FL); cv.set(32 + dx + (1 if dx else 0), 24 + dy + (1 if dy else 0), FL)
    cv.set(31, 23, WHT); cv.set(32, 24, WHT)
    return fx_out('fx_viewfinder', [cv], 64, 48)


def fx_flashframe():             # 셔터 순간 틀 안 흰빛 3프레임 (틀 안쪽만 · 점점 디더로 걷힘)
    fr = []
    for f in range(3):
        cv = Cv()
        cv.rect(0, 0, 64, 48, FLL)
        cv.rect(4, 4, 56, 40, WHT)
        cv.rect(0, 0, 64, 2, FL); cv.rect(0, 46, 64, 2, FL); cv.rect(0, 0, 2, 48, FL); cv.rect(62, 0, 2, 48, FL)
        if f == 0:
            star_burst(cv, 32, 24, 8, 17, 9, 3, WHT, FLL)
        fade(cv, [0.0, 0.42, 0.78][f])
        fr.append(cv)
    return fx_out('fx_flashframe', fr, 64, 48)


def fx_paperize():               # 적이 종이가 되는 순간 흰 테가 튀어 나옴 2프레임 (네모 테 + 모서리 반짝)
    fr = []
    for f in range(2):
        cv = Cv()
        h = 8 + f * 4
        for i in range(-h, h):
            if (i + f) % 5 != 4:
                for x, y in ((16 + i, 16 - h), (16 + i, 16 + h - 1), (16 - h, 16 + i), (16 + h - 1, 16 + i)):
                    cv.set(x, y, WHT)
                    cv.set(x + (1 if y in (16 - h, 16 + h - 1) and False else 0), y, WHT)
        for sx in (-1, 1):
            for sy in (-1, 1):
                x, y = 16 + sx * (h + 1) - (1 if sx > 0 else 0), 16 + sy * (h + 1) - (1 if sy > 0 else 0)
                cv.disc(x, y, 1.6 - f * 0.3, FLL)
                cv.set(x, y, WHT)
        fr.append(cv)
    return fx_out('fx_paperize', fr, 32, 32)


def fx_papertear():              # 종잇조각 5프레임 (16칸) — 접힌 종이 조각이 뒤집히며 구름
    fr = []
    for f in range(5):
        cv = Cv()
        k = [1.0, 0.72, 0.38, 0.72, 1.0][f]
        ang = [0.3, 0.9, 1.5, 2.1, 2.7][f]
        pts = [(-5, -4), (5, -3), (4, 4), (-2, 5), (-6, 1)]
        ca, sa = math.cos(ang), math.sin(ang)
        pp = []
        for (x, y) in pts:
            x2 = x * k
            pp.append((8 + x2 * ca - y * sa * 0.8, 8 + x2 * sa + y * ca * 0.8))
        cv.poly(pp, PAPER if f != 2 else PAPG)
        cv.poly([pp[1], pp[2], pp[3], ((pp[3][0] + pp[1][0]) / 2, (pp[3][1] + pp[1][1]) / 2)], PAPG)
        cv.set(pp[0][0], pp[0][1], PAPD); cv.set(pp[4][0], pp[4][1], PAPD)
        fr.append(cv)
    return fx_out('fx_papertear', fr, 16, 16)


def fx_print():                  # 인화지 팔랑 4프레임 — 위에서 흔들리며 떨어져 바닥에 납작
    fr = []
    spec = [(5, 6, 0.35, 10), (9, 8, -0.3, 11), (12, 9, 0.2, 12), (14, 10, 0.0, 12)]
    for f, (cy_, h, tilt, w) in enumerate(spec):
        cv = Cv()
        cy = [8, 12, 16, 19][f]
        if f == 3:
            cv.ell(16, 29, 9, 2.2, (0, 0, 0, 100))
        cx = 16
        for yy in range(-h, h):
            for xx in range(-w // 2 - 1, w // 2 + 1):
                X, Y = cx + xx + tilt * yy * 0.6, cy + yy
                cv.set(X, Y, PAPER)
        # 사진 면 (가운데 어두운 칸 · 작은 적 그림자)
        for yy in range(-h + 2, h - 3):
            for xx in range(-w // 2 + 1, w // 2 - 1):
                cv.set(cx + xx + tilt * yy * 0.6, cy + yy, (86, 96, 140) if yy < 1 else (62, 70, 112))
        cv.disc(cx + tilt * 1.2, cy - 1.0, 2.0 if f > 0 else 1.4, (180, 208, 150))
        for xx in range(-w // 2, w // 2):
            cv.set(cx + xx + tilt * (h - 1) * 0.6, cy + h - 1, PAPG)
        cv.outline(K1) if False else None
        fr.append(cv)
    return fx_out('fx_print', fr, 32, 32)


def fx_papercopy_tear():         # 종이 사본 찢김 4프레임 — 금이 가고 조각이 흩어짐
    fr = []
    for f in range(4):
        cv = Cv()
        d = f * 2.2
        left = [(8 - d, 4 - d * 0.4), (16 - d * 0.5, 4), (14 - d * 0.5, 12), (17 - d * 0.5, 18), (8 - d, 28 + d * 0.3)]
        right = [(16 + d * 0.5, 4), (24 + d, 4 - d * 0.3), (24 + d, 28 + d * 0.4), (17 + d * 0.5, 28), (14 + d * 0.5, 18), (17 + d * 0.5, 12)]
        if f == 0:
            cv.poly([(8, 4), (24, 4), (24, 28), (8, 28)], PAPER)
            for y in range(4, 29):
                cv.set(15 + int(math.sin(y * 0.9) * 1.5 + 1), y, PAPG) if y % 5 < 3 else None
            cv.rect(10, 8, 12, 12, (72, 82, 126))
        else:
            cv.poly(left, PAPER); cv.poly(right, PAPG if f > 1 else PAPER)
            cv.rect(int(10 - d), 9, 5, 9, (72, 82, 126)); cv.rect(int(18 + d * 0.8), 10, 5, 8, (62, 70, 112))
            r = random.Random(7 + f)
            for _ in range(2 + f * 2):
                a = r.random() * TAU
                dd = 6 + f * 3 + r.random() * 3
                x, y = 16 + math.cos(a) * dd, 16 + math.sin(a) * dd * 0.8
                cv.rect(int(x), int(y), 2, 2 if r.random() < 0.5 else 1, PAPER if r.random() < 0.6 else PAPG)
        fade(cv, [0.0, 0.0, 0.2, 0.55][f])
        fr.append(cv)
    return fx_out('fx_papercopy_tear', fr, 32, 32)


def fx_curtain():                # 셔터 커튼 띠 (가로 이음 타일 32×16 × 6프레임) — 위아래 필름 구멍 + 날 이음
    fr = []
    for f in range(6):
        cv = Cv()
        for x in range(32):
            for y in range(16):
                blade = ((x + f * 4) // 4) % 2
                if y in (0, 15):
                    cv.p[(x, y)] = FL
                elif y in (1, 2, 13, 14):
                    hole = ((x + f * 4) % 8) in (2, 3, 4)
                    cv.p[(x, y)] = FLL if hole and y in (1, 14) else (40, 44, 78)
                else:
                    cv.p[(x, y)] = (46, 52, 92) if blade == 0 else (60, 68, 116)
                    if ((x + f * 4) % 4) == 0:
                        cv.p[(x, y)] = (112, 130, 212)
        fr.append(cv)
    return fx_out('fx_curtain', fr, 32, 16)


def fx_paperplane():             # 종이비행기 2프레임 (오른쪽 향함 · 날개 모양이 바뀜)
    fr = []
    for f in range(2):
        cv = Cv()
        w = 4 + f * 2
        cv.poly([(22, 8), (2, 2 + f), (8, 8)], PAPER)
        cv.poly([(22, 8), (8, 8), (4, 13 + f - 1 + w * 0.2)], PAPG)
        cv.line(8, 8, 22, 8, PAPD, 1)
        cv.set(2, 2 + f, PAPD)
        cv.poly([(22, 8), (11, 8), (8, 11 + f)], (210, 214, 230))
        fr.append(cv)
    return fx_out('fx_paperplane', fr, 24, 16)


def fx_bulb():                   # 전구 불꽃 3프레임 — 청백 별이 터짐
    fr = []
    for f in range(3):
        cv = Cv()
        L = [6, 11, 8][f]
        star_burst(cv, 16, 16, 8, L, L * 0.55, 3, WHT, FL)
        cv.disc(16, 16, [3, 4, 2.5][f], WHT)
        for i in range(8):
            a = TAU * i / 8 + 0.4 + f * 0.3
            cv.set(16 + math.cos(a) * (L + 3) - 0.5, 16 + math.sin(a) * (L + 3) - 0.5, FLL)
        fade(cv, [0.0, 0.0, 0.35][f])
        fr.append(cv)
    return fx_out('fx_bulb', fr, 32, 32)


def tripod_cam(cv, lit):
    for (x0, x1) in ((16, 9), (16, 23), (16, 16)):
        cv.line(x0, 17, x1, 29, (150, 156, 172), 2 if x0 != x1 else 1)
    cv.line(17, 17, 24, 29, (98, 102, 124), 1)
    cv.rect(7, 6, 18, 11, CAMB)
    cv.rect(7, 6, 18, 2, CAML)
    cv.rect(11, 3, 5, 3, (36, 32, 54)); cv.rect(20, 3, 3, 2, FL)
    cv.disc(16, 12, 4.4, (140, 150, 176))
    cv.disc(16, 12, 3.2, (24, 30, 74))
    cv.set(15, 11, FL if not lit else WHT); cv.set(14, 10, WHT)
    cv.outline(K1)


def fx_tripodcam():              # 삼각대 카메라 2프레임 (두 번째는 렌즈 불이 켜져 반짝)
    fr = []
    for f in range(2):
        cv = Cv()
        tripod_cam(cv, f == 1)
        if f == 1:
            star_burst(cv, 16, 12, 4, 6, 3, 2, WHT, FL)
            cv.disc(16, 12, 1.6, WHT)
        fr.append(cv)
    return fx_out('fx_tripodcam', fr, 32, 32)


def fxs():
    return [fx_viewfinder(), fx_flashframe(), fx_paperize(), fx_papertear(), fx_print(), fx_papercopy_tear(), fx_curtain(),
            fx_paperplane(), fx_bulb(), fx_tripodcam()]


# ================================================================== 4. 아이콘 (범주 틀 + 그림문자)
SK, SKS = (242, 188, 150), (202, 126, 112)
RED, REDD, REDL = (222, 60, 70), (150, 30, 50), (255, 130, 120)
GOLD, GOLD_D, WHITE = (254, 222, 108), (200, 140, 40), (255, 250, 240)
CAM, CAMD, CAMH = (60, 58, 84), (34, 30, 54), (122, 128, 156)
STL, STLD, STLL = (150, 156, 174), (98, 102, 126), (214, 218, 232)
FILM = (206, 214, 255)
GRN, GRND = (140, 220, 140), (62, 140, 92)
ORG, ORGD = (255, 176, 70), (200, 110, 40)
EYE = (32, 22, 50)
GLS = (28, 36, 84)
AMB, AMBD = (230, 150, 70), (160, 90, 44)
MAG = (230, 120, 200)


def camera(cv, x, y, w=14, h=9, lit=False):        # 정면 카메라 (왼쪽 위 x,y · 렌즈 가운데 · 플래시 전구)
    cv.rect(x, y, w, h, CAM)
    cv.rect(x, y, w, 2, CAMH)
    cv.rect(x, y + h - 1, w, 1, CAMD)
    cv.rect(x + 2, y - 2, 3, 2, CAMD)
    cv.rect(x + w - 4, y - 2, 3, 2, FL)
    r = min(w, h) / 2 - 1.2
    cx, cy = x + w / 2, y + h / 2 + 0.5
    cv.disc(cx, cy, r + 1, STL)
    cv.disc(cx, cy, r, GLS)
    cv.set(cx - 1.5, cy - 1.5, WHT if lit else FL)


def brk(cv, x0, y0, x1, y1, c, L=4, c2=None):    # 꺾쇠 네 모서리 (x1 · y1 은 포함)
    for (cx, cy, sx, sy) in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        for i in range(L):
            cv.set(cx + sx * i, cy, c); cv.set(cx, cy + sy * i, c)
            if c2:
                cv.set(cx + sx * i, cy + sy, c2); cv.set(cx + sx, cy + sy * i, c2)


def print_card(cv, x, y, w, h, shade=False):      # 인화지 (흰 테 + 어두운 사진 면)
    cv.rect(x, y, w, h, PAPER)
    cv.rect(x, y + h - 1, w, 1, PAPG)
    cv.rect(x + 1, y + 1, w - 2, h - 4, (74, 84, 132) if not shade else (96, 104, 150))
    cv.rect(x + 1, y + 1, w - 2, 1, (110, 124, 186))


def paper_foe(cv, cx, cy, r):                      # 종이가 된 적 (흰 테 · 회색 몸 · 눈)
    cv.disc(cx, cy, r + 1, PAPER)
    cv.disc(cx, cy, r, PAPG)
    cv.disc(cx - 0.3, cy - 0.4, r - 1, (200, 204, 218))
    if r >= 3:
        cv.set(cx - r * 0.45, cy - r * 0.15, EYE); cv.set(cx + r * 0.45 - 1, cy - r * 0.15, EYE)


def plane(cv, x, y, s=1.0, c=PAPER):               # 종이비행기 (오른쪽 향함)
    cv.poly([(x + 12 * s, y), (x - 8 * s, y - 6 * s), (x - 3 * s, y)], c)
    cv.poly([(x + 12 * s, y), (x - 3 * s, y), (x - 6 * s, y + 4 * s)], PAPG)
    cv.line(x - 3 * s, y, x + 12 * s, y, PAPD, 1)


def bulb(cv, cx, cy, r, lit=True):
    cv.disc(cx, cy, r, FL if lit else STL)
    cv.disc(cx - 0.3, cy - 0.3, r - 1.2, FLL if lit else STLL)
    cv.disc(cx - r * 0.35, cy - r * 0.4, max(1.0, r * 0.28), WHT)
    cv.line(cx - r * 0.45, cy + r * 0.4, cx, cy - r * 0.1, ORG, 1)
    cv.line(cx, cy - r * 0.1, cx + r * 0.45, cy + r * 0.4, ORG, 1)
    cv.rect(int(cx - r * 0.55), int(cy + r * 0.9), int(r * 1.1), 2, STLD)
    cv.rect(int(cx - r * 0.45), int(cy + r * 0.9) + 2, int(r * 0.9), 2, STL)


def reel(cv, cx, cy, r):
    cv.disc(cx, cy, r, STLD)
    cv.disc(cx - 0.3, cy - 0.3, r - 1, STL)
    cv.disc(cx, cy, r * 0.35, CAMD)
    for a in (0.5, 2.6, 4.7):
        cv.set(cx + math.cos(a) * r * 0.65 - 0.5, cy + math.sin(a) * r * 0.65 - 0.5, CAMD)


def sparkle(cv, x, y, n=2, c=WHITE):
    for i in range(-n, n + 1):
        cv.set(x + i, y, c); cv.set(x, y + i, c)


def lamp_cone(cv, x0, y0, x1, ya, yb, c):
    cv.poly([(x0, y0), (x1, ya), (x1, yb)], c)


# ---- 특수 능력 568~574
def g568(cv):     # 플래시 전구
    for a in range(8):
        d = [(-1, -1), (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0)][a]
        if a in (1, 3, 5, 7):
            cv.line(16 + d[0] * 10, 12 + d[1] * 10, 16 + d[0] * 13, 12 + d[1] * 13, WHITE, 1)
        else:
            cv.line(16 + d[0] * 9, 12 + d[1] * 9, 16 + d[0] * 11, 12 + d[1] * 11, FL, 1)
    bulb(cv, 16, 12, 7.0)


def g569(cv):     # 삼각대 카메라
    for x1 in (8, 24, 16):
        cv.line(16, 16, x1, 27, STL if x1 != 24 else STLD, 2 if x1 != 16 else 1)
    camera(cv, 8, 6, 16, 10)
    cv.rect(14, 15, 4, 2, STLD)


def g570(cv):     # 영사기
    reel(cv, 10, 8, 4.4); reel(cv, 19.5, 8, 4.4)
    cv.rect(5, 13, 17, 9, CAM); cv.rect(5, 13, 17, 2, CAMH); cv.rect(5, 21, 17, 1, CAMD)
    cv.rect(22, 15, 3, 5, STLD); cv.rect(25, 16, 2, 3, STL)
    lamp_cone(cv, 27, 17.5, 31, 12, 24, FLL)
    cv.rect(8, 17, 2, 2, FL); cv.rect(12, 17, 2, 2, FL); cv.rect(16, 17, 2, 2, FL)
    cv.rect(7, 22, 3, 3, STLD); cv.rect(17, 22, 3, 3, STLD)


def g571(cv):     # 필름 보관함 (통 + 삐죽 나온 필름 + 하나 더)
    cv.rect(8, 9, 13, 15, STL)
    cv.rect(8, 9, 13, 3, STLL); cv.rect(8, 22, 13, 2, STLD); cv.rect(19, 12, 2, 10, STLD)
    cv.rect(10, 14, 8, 6, PAPER); cv.rect(11, 15, 6, 1, RED); cv.rect(11, 17, 4, 1, PAPD); cv.rect(11, 19, 5, 1, PAPD)
    cv.rect(11, 6, 7, 3, STLD); cv.rect(12, 6, 5, 1, STL)
    cv.rect(21, 20, 7, 4, CAMD)
    for x in (22, 24, 26):
        cv.set(x, 21, FILM); cv.set(x, 23, FILM)
    cv.rect(21, 22, 7, 1, (120, 98, 70))
    cv.rect(23, 3, 6, 2, GOLD); cv.rect(25, 1, 2, 6, GOLD)


def g572(cv):     # 모델 섭외 (사진 속 웃는 사람 + 하트)
    print_card(cv, 7, 5, 16, 20)
    cv.rect(8, 6, 14, 14, (116, 134, 200))
    cv.disc(15, 12, 3.6, SK); cv.disc(15.5, 12.6, 1.8, SKS)
    cv.set(13, 11, EYE); cv.set(17, 11, EYE); cv.rect(14, 14, 3, 1, REDD)
    cv.ell(15, 22 - 3, 6.5, 3.4, (76, 96, 164))
    cv.disc(15, 8.4, 3.9, (110, 66, 56)); cv.disc(15, 10, 3.4, SK)
    cv.set(13, 11, EYE); cv.set(17, 11, EYE)
    cv.poly([(23, 20), (29, 20), (29, 23), (26, 27), (23, 23)], RED) if False else None
    for (x, y) in ((22, 17), (28, 17)):
        cv.disc(x, y, 2.4, RED)
    cv.poly([(19.5, 17.5), (30.5, 17.5), (25, 25)], RED)
    cv.set(22, 16, REDL)


def g573(cv):     # 셔터 소리 (조리개 + 퍼지는 고리)
    cv.disc(16, 16, 8.6, CAM)
    cv.disc(16, 16, 7.4, STL)
    for k in range(6):
        a = TAU * k / 6
        cv.line(16 + math.cos(a) * 2.6, 16 + math.sin(a) * 2.6, 16 + math.cos(a + 0.9) * 7.4, 16 + math.sin(a + 0.9) * 7.4, CAMD, 1)
    cv.disc(16, 16, 2.8, GLS); cv.set(15, 15, FL)
    for sg in (-1, 1):
        for r_ in (11.5, 14.0):
            arc(cv, 16, 16, r_, -0.5 if sg > 0 else math.pi - 0.5 + 0.0, 0.5 if sg > 0 else math.pi + 0.5, FL, 1)


def g574(cv):     # 빠른 현상 (감기는 필름 릴 + 앞으로 달리는 화살표)
    cv.disc(11, 15, 8.2, STLD); cv.disc(10.6, 14.6, 7.2, STL)
    cv.disc(11, 15, 5, CAMD)
    for a in (0.4, 2.5, 4.6):
        cv.disc(11 + math.cos(a) * 3.3, 15 + math.sin(a) * 3.3, 1.2, STL)
    cv.disc(11, 15, 1.4, STLL)
    cv.rect(17, 21, 11, 5, CAMD); cv.rect(17, 22, 11, 3, (120, 98, 70))
    for x in (18, 21, 24, 27):
        cv.set(x, 21, FILM); cv.set(x, 25, FILM)
    for k in range(2):
        for i in range(4):
            cv.set(18 + k * 5 + i, 6 + i, GOLD if k == 0 else ORG); cv.set(18 + k * 5 + i, 13 - i, GOLD if k == 0 else ORG)
            cv.set(19 + k * 5 + i, 6 + i, GOLD_D); cv.set(19 + k * 5 + i, 13 - i, GOLD_D)


# ---- 전용 카드 780~785
def g780(cv):     # 장노출 (남는 틀)
    brk(cv, 5, 12, 18, 25, (96, 108, 176), 4)
    brk(cv, 8, 9, 21, 22, FL, 4)
    brk(cv, 11, 6, 24, 19, WHITE, 4, FLD)
    for i in range(5):
        cv.line(14 + i, 20 - i, 22 + i, 20 - i, FL if i % 2 == 0 else FLD, 1)
    for x, y in ((24, 25), (27, 22), (6, 8)):
        sparkle(cv, x, y, 1, FLL)
    cv.disc(17.5, 13, 2.2, WHITE)


def g781(cv):     # 광각 렌즈 (넓어지는 화살표)
    brk(cv, 4, 7, 27, 24, FL, 5)
    cv.disc(16, 15.5, 6.2, STL); cv.disc(16, 15.5, 4.8, GLS); cv.disc(16, 15.5, 2.2, (60, 74, 150))
    cv.set(14, 13, WHITE)
    arrow_head(cv, 28, 15, 1, 0, GOLD, 3); arrow_head(cv, 3, 15, -1, 0, GOLD, 3)
    cv.line(23, 15, 27, 15, GOLD, 1); cv.line(4, 15, 9, 15, GOLD, 1)


def g782(cv):     # 현상액 (병 · 떨어지는 방울 · 흩날리는 종잇조각)
    cv.rect(7, 12, 11, 14, AMB); cv.rect(7, 12, 3, 14, (250, 190, 110)); cv.rect(15, 12, 3, 14, AMBD)
    cv.rect(10, 7, 5, 5, STLL); cv.rect(9, 5, 7, 2, STLD)
    cv.rect(9, 17, 7, 6, PAPER); cv.rect(10, 18, 5, 1, PAPD); cv.rect(10, 20, 4, 1, PAPD)
    cv.line(21, 5, 21, 9, AMB, 2); cv.disc(21, 12, 1.8, AMB)
    for (x, y, a) in ((23, 18, 0.4), (27, 14, 1.2), (26, 23, 2.2)):
        cv.poly([(x - 2, y - 2), (x + 2, y - 1), (x + 1, y + 2), (x - 2, y + 1)], PAPER)
        cv.set(x + 1, y + 1, PAPG)


def g783(cv):     # 바람에 날린 사진 (바람 + 날아가는 인화지 + 부딪힌 적)
    for k, (y0, ph) in enumerate(((8, 0.0), (15, 1.6), (22, 3.1))):
        for x in range(2, 14):
            cv.set(x, y0 + math.sin(x * 0.55 + ph) * 1.6, FL if k != 1 else FLL)
    cv.poly([(7, 8), (17, 10), (16, 21), (6, 19)], PAPER)
    cv.poly([(8.5, 10), (15.5, 11.5), (14.6, 18), (8, 17)], (74, 84, 132))
    cv.poly([(6, 19), (16, 21), (16.4, 19), (6.4, 17.2)], PAPG)
    foe(cv, 25, 17, 4.2)
    star_burst(cv, 20, 14, 4, 4, 2, 2, GOLD, GOLD_D)
    cv.set(20, 14, WHITE)


def g784(cv):     # 자동 초점 (가까운 큰 적에게 끌려가는 틀)
    foe(cv, 20, 17, 6.2)
    brk(cv, 12, 9, 28, 25, FL, 4, FLD)
    cv.disc(20, 17, 2.0, WHITE) if False else None
    for i in range(5):
        cv.set(4 + i * 2, 26 - i * 2 + (i % 2), WHITE if i % 2 == 0 else FLD)
    arrow_head(cv, 11, 17, 1, 0, GOLD, 3)
    cv.line(4, 17, 9, 17, GOLD, 1)
    sparkle(cv, 20, 17, 1, WHITE)


def g785(cv):     # 인화지 추가 (겹친 인화지 + 더하기)
    print_card(cv, 12, 13, 14, 15, True)
    print_card(cv, 9, 9, 14, 15, True)
    print_card(cv, 6, 5, 14, 15)
    paper_foe(cv, 13, 11, 3)
    cv.rect(23, 4, 6, 2, GOLD); cv.rect(25, 2, 2, 6, GOLD)


# ---- 고유 스킬 880~889
def g880(cv):     # 플래시 반사 (번쩍 + 종이가 되어 떨어지는 탄)
    star_burst(cv, 13, 12, 8, 10, 5.5, 3, WHITE, FL)
    cv.disc(13, 12, 3.4, WHITE)
    cv.disc(24, 7, 3.4, RED); cv.set(23, 6, REDL)
    cv.line(22, 10, 21, 15, PAPG, 1)
    cv.poly([(19, 17), (27, 18), (26, 25), (18, 23)], PAPER)
    cv.poly([(18, 23), (26, 25), (27, 22), (19, 21)], PAPG)
    cv.set(23, 20, RED)


def g881(cv):     # 셀카 (틀 안에 내 얼굴 · 회복)
    brk(cv, 4, 4, 27, 27, FL, 4, FLD)
    cv.disc(16, 16, 7.4, SKS); cv.disc(15.6, 15.6, 6.6, SK)
    cv.disc(16, 11, 7.0, (110, 66, 56)) if False else None
    cv.poly([(9, 14), (11, 8), (17, 7), (23, 9), (23, 13), (17, 11)], (96, 60, 52))
    cv.set(13, 16, EYE); cv.set(19, 16, EYE); cv.rect(14, 20, 5, 1, REDD); cv.set(13, 19, REDD); cv.set(19, 19, REDD)
    cv.rect(25, 3, 4, 2, GREEN) if False else None
    cv.rect(23, 4, 6, 2, GRN); cv.rect(25, 2, 2, 6, GRN)
    sparkle(cv, 6, 24, 1, WHITE)


def g882(cv):     # 셀프 타이머 (카메라 + 시계)
    for x1 in (5, 17):
        cv.line(11, 21, x1, 28, STL, 1)
    camera(cv, 3, 12, 16, 9)
    cv.disc(23, 10, 6.4, STLD); cv.disc(23, 10, 5.4, PAPER)
    cv.line(23, 10, 23, 6, EYE, 1); cv.line(23, 10, 26, 11, REDD, 1)
    cv.set(23, 4, RED); cv.set(23, 15, PAPD); cv.set(18, 10, PAPD); cv.set(28, 10, PAPD)
    for i in range(3):
        cv.set(21 + i * 2, 25, GOLD) if False else None
    cv.rect(21, 22, 2, 5, GOLD); cv.rect(25, 22, 2, 5, GOLD); cv.rect(21, 22, 6, 1, GOLD); cv.rect(21, 26, 6, 1, GOLD); cv.rect(25, 24, 2, 1, GOLD)


def g883(cv):     # 오려 붙이기 (가위 + 종이비행기)
    plane(cv, 21, 11, 1.1)
    for i in range(8):
        cv.set(4 + i * 2, 23, (255, 255, 255) if i % 2 == 0 else PAPD)
    cv.line(6, 27, 20, 19, STLL, 2) if False else None
    cv.line(6, 18, 22, 25, STLL, 2); cv.line(6, 25, 22, 18, STL, 2)
    cv.ring(5, 17, 1.4, 3.2, RED); cv.ring(5, 26, 1.4, 3.2, REDD)


def g884(cv):     # 노출 과다 (하얗게 타는 틀)
    cv.rect(5, 5, 22, 22, (255, 190, 100))
    cv.rect(7, 7, 18, 18, (255, 236, 190))
    cv.rect(9, 9, 14, 14, WHITE)
    brk(cv, 4, 4, 27, 27, ORG, 5, ORGD)
    star_burst(cv, 16, 16, 8, 12, 7, 3, WHITE, FLL)
    cv.disc(16, 16, 4.5, WHITE)
    for (x, y) in ((6, 6), (25, 6), (6, 25), (25, 25), (16, 6), (6, 16)):
        cv.set(x, y, (255, 150, 70))


def g885(cv):     # 흔들린 사진 (이동 방향으로 늘어난 틀 + 잔상)
    for k, c in enumerate(((84, 96, 170), (126, 142, 220), FL)):
        brk(cv, 3 + k * 4, 8, 18 + k * 4, 23, c, 4)
    brk(cv, 15, 8, 30, 23, WHITE, 5, FLD) if False else None
    foe(cv, 23, 16, 4.4)
    for y in (11, 16, 21):
        cv.line(2, y, 9 + (y % 3) * 2, y, FLL, 1)
    arrow_head(cv, 28, 16, 1, 0, GOLD, 3)


def g886(cv):     # 앨범 (펼친 책 · 사진 두 장)
    cv.poly([(3, 9), (16, 12), (16, 27), (3, 24)], (150, 100, 70))
    cv.poly([(29, 9), (16, 12), (16, 27), (29, 24)], (120, 76, 56))
    cv.poly([(4.5, 10), (15.5, 13), (15.5, 25), (4.5, 22.5)], PAPER)
    cv.poly([(27.5, 10), (16.5, 13), (16.5, 25), (27.5, 22.5)], (236, 236, 244))
    cv.poly([(6.5, 12.5), (13.5, 14.5), (13.5, 20), (6.5, 18.5)], (84, 98, 156))
    cv.poly([(25.5, 12.5), (18.5, 14.5), (18.5, 20), (25.5, 18.5)], (96, 112, 176))
    paper_foe(cv, 10, 17, 2); cv.disc(22, 17.5, 2, GRND)
    cv.rect(15, 11, 2, 16, CAMD)
    cv.set(9, 22, PAPD); cv.set(10, 22, PAPD); cv.set(22, 22, PAPD); cv.set(23, 22, PAPD)
    cv.rect(13, 3, 6, 5, GOLD); cv.set(16, 5, WHITE)


def g887(cv):     # 셔터 찬스 (스킬을 준비하던 적을 찰칵)
    cv.disc(16, 18, 6.8, GRND); cv.disc(15.6, 17.6, 5.8, GRN)
    cv.set(13, 17, EYE); cv.set(19, 17, EYE); cv.rect(14, 21, 5, 1, EYE)
    brk(cv, 6, 8, 25, 27, FL, 4, FLD)
    cv.rect(15, 1, 3, 5, RED); cv.rect(15, 7, 3, 2, RED)
    for (x, y) in ((26, 6), (5, 6), (28, 18)):
        sparkle(cv, x, y, 1, WHITE)
    cv.line(10, 10, 14, 14, WHITE, 1)


def g888(cv):     # 현상소 (붉은 전구 · 널린 사본에 끌려오는 적)
    cv.line(2, 6, 30, 6, STLD, 1)
    cv.disc(16, 6, 5, REDD); cv.disc(16, 6, 3.8, RED); cv.disc(15, 5, 1.6, REDL)
    cv.poly([(11, 8), (21, 8), (28, 28), (4, 28)], (160, 40, 60)) if False else None
    cv.rect(13, 17, 7, 9, PAPER); cv.rect(14, 18, 5, 5, (110, 60, 90)); cv.rect(13, 25, 7, 1, PAPG)
    cv.rect(16, 11, 1, 6, STLD)
    foe(cv, 5, 22, 3.4); foe(cv, 27, 22, 3.4)
    arrow_head(cv, 11, 22, 1, 0, REDL, 2); arrow_head(cv, 21, 22, -1, 0, REDL, 2)


def g889(cv):     # 기념 사진 (보스와 잡몹을 한 틀에)
    brk(cv, 3, 4, 28, 27, FL, 5, FLD)
    foe(cv, 18, 13, 7.0)
    cv.rect(13, 3, 10, 2, GOLD) if False else None
    cv.poly([(13, 5), (15, 8), (18, 4), (21, 8), (23, 5), (23, 9), (13, 9)], GOLD)
    for (x, y) in ((8, 23), (15, 24), (22, 23), (27, 22)):
        foe(cv, x, y, 2.6)
    cv.set(18, 5, WHITE)


# ---- 스킬 진화 980~986
def g980(cv):     # 야경 (빛 궤적)
    cv.rect(4, 5, 24, 22, (20, 26, 64))
    brk(cv, 4, 5, 27, 26, FL, 4, FLD)
    cv.disc(10, 12, 4, (250, 244, 210)); cv.disc(12, 11, 3.4, (20, 26, 64))
    for i in range(4):
        cv.line(8 + i * 5, 24 - i * 2, 24, 17 - i, [GOLD, ORG, FL, WHITE][i], 1)
    cv.line(6, 23, 25, 17, WHITE, 1)
    for (x, y) in ((22, 9), (17, 8), (26, 11), (7, 8)):
        cv.set(x, y, WHITE)


def g981(cv):     # 추적 초점 (잠금 표식 + 기절)
    cv.ring(16, 17, 8.2, 9.6, FL)
    for (x, y) in ((16, 5), (16, 28), (3, 17), (28, 17)):
        cv.rect(x - 1, y - 2, 2, 4, FL) if x in (16,) else cv.rect(x - 2, y - 1, 4, 2, FL)
    foe(cv, 16, 17, 5)
    cv.line(2, 17, 8, 17, WHITE, 1) if False else None
    cv.poly([(20, 2), (24, 2), (22, 6), (26, 6), (19, 12), (21, 7), (18, 7)], (255, 230, 90))
    cv.set(16, 17, WHITE)
    for x in (4, 26):
        cv.set(x, 27, GOLD)


def g982(cv):     # 스튜디오 (조명 · 삼각대 · 빛 아래 적)
    cv.rect(3, 6, 7, 11, STLL); cv.rect(4, 7, 5, 9, FLL); cv.rect(3, 6, 7, 1, STL); cv.rect(3, 16, 7, 1, STLD)
    cv.rect(6, 17, 1, 8, STLD); cv.line(6, 25, 3, 28, STLD, 1); cv.line(6, 25, 9, 28, STLD, 1)
    lamp_cone(cv, 10, 8, 26, 14, 26, (214, 224, 255))
    cv.poly([(10, 10), (22, 16), (22, 24), (10, 15)], WHITE) if False else None
    foe(cv, 22, 20, 4.4)
    cv.line(11, 27, 28, 27, STLD, 1)
    sparkle(cv, 17, 14, 1, WHITE); sparkle(cv, 27, 10, 1, WHITE)


def g983(cv):     # 색종이 폭풍 (돌며 퍼지는 색종이)
    cols = [RED, GOLD, FL, GRN, MAG, ORG]
    r = random.Random(5)
    for i in range(26):
        a = i * 0.62
        d = 3 + i * 0.5
        x, y = 16 + math.cos(a) * d, 16 + math.sin(a) * d * 0.9
        c = cols[i % 6]
        if i % 3 == 0:
            cv.rect(int(x), int(y), 3, 2, c)
        elif i % 3 == 1:
            cv.rect(int(x), int(y), 2, 3, c)
        else:
            cv.rect(int(x), int(y), 2, 2, c)
    cv.disc(16, 16, 3.2, PAPER); cv.set(15, 15, WHITE)


def g984(cv):     # 태풍 파노라마 (띠 + 소용돌이 + 날아가는 종이 적)
    cv.rect(2, 12, 28, 8, (46, 52, 92))
    cv.rect(2, 11, 28, 1, FL); cv.rect(2, 20, 28, 1, FL)
    for x in range(3, 29, 4):
        cv.rect(x, 13, 2, 1, FLL); cv.rect(x, 18, 2, 1, FLL)
    for k, rr in enumerate((11, 8, 5)):
        arc(cv, 16, 16, rr, 0.4 + k * 0.7, 0.4 + k * 0.7 + 3.6, FLL if k != 1 else FL, 1)
    paper_foe(cv, 25, 6, 3)
    paper_foe(cv, 7, 26, 2)
    arrow_head(cv, 29, 6, 1, 0, GOLD, 2)


def g985(cv):     # 플립북 (부채꼴로 넘어가는 장 + 맨 앞 장의 사진)
    for k in range(4):
        x = 4 + k * 3
        cv.poly([(x, 24), (x + 2, 8 + k * 2), (x + 16, 9 + k * 2), (x + 15, 25)], (190 + k * 14, 196 + k * 14, 226))
        cv.line(x, 24, x + 15, 25, PAPG, 1)
    cv.poly([(13, 24), (15, 6), (29, 7), (28, 25)], PAPER)
    cv.poly([(15, 8), (27, 9), (26.6, 18), (15.4, 17)], (84, 98, 156))
    paper_foe(cv, 21, 13, 3)
    cv.line(13, 24, 28, 25, PAPG, 1)
    cv.rect(4, 6, 2, 2, GOLD); sparkle(cv, 7, 5, 2, GOLD)


def g986(cv):     # 기념 앨범 (금박 표지 · 별 · 리본)
    cv.rect(6, 5, 20, 23, (150, 60, 80)); cv.rect(6, 5, 3, 23, (104, 38, 58)); cv.rect(23, 5, 3, 23, (176, 86, 106))
    cv.rect(10, 8, 13, 17, GOLD_D); cv.rect(11, 9, 11, 15, (176, 60, 82))
    cv.poly([(16.5, 11), (18, 15), (22, 15), (19, 18), (20, 22), (16.5, 19.5), (13, 22), (14, 18), (11, 15), (15, 15)], GOLD)
    cv.set(16, 15, WHITE)
    cv.rect(15, 2, 3, 6, RED); cv.rect(13, 1, 7, 2, REDL) if False else None
    cv.rect(4, 26, 4, 3, GOLD); cv.rect(24, 26, 4, 3, GOLD)
    brk(cv, 4, 3, 27, 27, GOLD, 3) if False else None
    cv.rect(26, 12, 3, 5, STLD); cv.rect(26, 11, 3, 2, FL)


# ---- 무기 강화 624~626
def g624(cv):     # 이중 노출 (겹친 두 장)
    cv.rect(4, 7, 17, 15, (96, 112, 180)); cv.rect(11, 12, 17, 15, (230, 170, 80))
    cv.rect(11, 12, 10, 10, (240, 238, 220))
    brk(cv, 3, 6, 21, 22, FL, 3)
    brk(cv, 10, 11, 28, 27, GOLD, 3)
    cv.disc(10, 14, 3, GRND); cv.disc(21, 20, 3, GRND)
    cv.set(15, 17, WHITE)


def g625(cv):     # 흑백 사진 (반은 색 · 반은 회색)
    cv.rect(5, 5, 22, 22, PAPER)
    cv.rect(7, 7, 18, 16, (110, 110, 118))
    cv.rect(7, 7, 9, 16, (232, 110, 90))
    cv.rect(7, 7, 9, 6, (255, 190, 110))
    cv.rect(16, 7, 9, 7, (180, 182, 190))
    cv.disc(11, 17, 3.2, GRN) if False else None
    cv.disc(11, 17, 3.2, GRND); cv.disc(21, 17, 3.2, (78, 80, 90))
    cv.rect(5, 24, 22, 3, PAPG)
    cv.rect(15, 7, 2, 16, WHITE)


def g626(cv):     # 보정 (더 커진 초점 원 + 반짝)
    cv.ring(16, 16, 9.0, 11.0, FL)
    cv.ring(16, 16, 9.0, 9.8, FLD)
    for (x, y, dx, dy) in ((16, 4, 0, 1), (16, 27, 0, -1), (4, 16, 1, 0), (27, 16, -1, 0)):
        cv.rect(x - 1 if dy else x, y - 1 if dx else y, 2 if dy else 3, 3 if dy else 2, WHITE)
    foe(cv, 16, 16, 5.2)
    cv.set(14, 14, WHITE)
    sparkle(cv, 25, 6, 2, GOLD); sparkle(cv, 6, 25, 1, FLL)
    cv.set(16, 16, WHITE) if False else None


GREEN = GRN
ICONS = [
    (568, 'square', 'weapon', g568), (569, 'square', 'weapon', g569), (570, 'square', 'weapon', g570),
    (571, 'diamond', 'survival', g571), (572, 'diamond', 'survival', g572), (573, 'diamond', 'survival', g573), (574, 'diamond', 'survival', g574),
    (780, 'square', 'weapon', g780), (781, 'square', 'weapon', g781), (782, 'square', 'weapon', g782), (783, 'square', 'weapon', g783),
    (784, 'circle', 'skill', g784), (785, 'circle', 'skill', g785),
    (880, 'circle', 'skill', g880), (881, 'circle', 'skill', g881), (882, 'circle', 'skill', g882), (883, 'circle', 'skill', g883),
    (884, 'circle', 'skill', g884), (885, 'circle', 'skill', g885), (886, 'circle', 'skill', g886), (887, 'circle', 'skill', g887),
    (888, 'circle', 'skill', g888), (889, 'circle', 'skill', g889),
    (980, 'hex', 'soul', g980), (981, 'hex', 'soul', g981), (982, 'hex', 'soul', g982), (983, 'hex', 'soul', g983),
    (984, 'hex', 'soul', g984), (985, 'hex', 'soul', g985), (986, 'hex', 'soul', g986),
    (624, 'square', 'weapon', g624), (625, 'square', 'weapon', g625), (626, 'square', 'weapon', g626),
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
FXNAMES = ['fx_viewfinder', 'fx_flashframe', 'fx_paperize', 'fx_papertear', 'fx_print', 'fx_papercopy_tear', 'fx_curtain',
           'fx_paperplane', 'fx_bulb', 'fx_tripodcam']


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
    for nm in ('weapon_camera', 'weapon_kit_168', 'weapon_kit_169', 'weapon_kit_170'):
        items.append((Image.open(os.path.join(WEAP, nm + '.png')).convert('RGBA'), 10))
    for nm in FXNAMES:
        items.append((Image.open(os.path.join(FXD, nm + '.png')).convert('RGBA'), 3 if nm in ('fx_flashframe', 'fx_curtain') else 4))
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
