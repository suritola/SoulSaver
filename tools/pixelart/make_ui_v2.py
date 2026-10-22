# UI 전면 개편 v2 그림 (docs/art/UI-개편-기획서.md 4절 + 8절 「새긴 석판」 수정) — 2026-10-22~
#   → Assets/Resources/UI/v2/*.png  (1텍셀 = 1도트 · PPU 33.333 → 1080p · UI 100% 에서 1도트 3px · Point · 무압축 · FullRect)
#   판 · 버튼 · 카드는 9칸 나눔 + Tiled 로 씀 (가운데 디더 돌결이 늘어나지 않게 가운데 무늬는 나눔 칸 크기로 반복)
#   색은 Assets/Scripts/UiTokens.cs 의 Hex("#RRGGBB") 를 읽어 씀 (두 곳이 어긋나지 않게) · 상태(기본/올림/누름/비활성)는 그림을 바꿔 보여 줌
#   meta 가 이미 있으면 GUID 를 그대로 둠 (다시 그려도 참조가 깨지지 않게)
# 실행: py tools/pixelart/make_ui_v2.py            → 그림 + docs/art/ui-v2-preview.png (승인용 미리보기 1920x1080)
import os, re, uuid
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'UI', 'v2')
PPU = 100.0 / 3.0
DOT = 3

# ---------------------------------------------------------------- 색 (UiTokens.cs)
TOK = {}
for name, hx in re.findall(r'(\w+) = Hex\("#([0-9A-Fa-f]{6})"\)', open(os.path.join(ROOT, 'Assets', 'Scripts', 'UiTokens.cs'), encoding='utf-8').read()):
    TOK[name] = tuple(int(hx[i:i + 2], 16) for i in (0, 2, 4)) + (255,)
need = ['Void', 'Panel', 'Raised', 'Sunken', 'Outline', 'LineDim', 'Line', 'LineHi', 'Text1', 'Text2', 'Text3',
        'Accent', 'AccentDim', 'AccentDeep', 'Danger', 'DangerDim', 'DangerDeep', 'Bronze', 'GaugeHp', 'GaugeSoul']
miss = [n for n in need if n not in TOK]
if miss:
    raise SystemExit('UiTokens.cs 에 없는 색: ' + ', '.join(miss))
T = TOK
CLEAR = (0, 0, 0, 0)
WHITE = (255, 255, 255, 255)


def mix(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3)) + (255,)


def new(w, h):
    return Image.new('RGBA', (w, h), CLEAR)


# ---------------------------------------------------------------- 그림
def stone(img, x0, y0, x1, y1, base, fleck, tile):
    """새긴 돌 바탕: base 에 fleck 점을 tile 주기로 흩뿌림 (Tiled 로 반복해도 이음매 없음)"""
    pts = {(1, 1), (4, 3), (2, 5)} if tile == 6 else {(1, 2), (5, 1), (3, 5), (6, 6)}
    for y in range(y0, y1):
        for x in range(x0, x1):
            img.putpixel((x, y), fleck if ((x % tile), (y % tile)) in pts else base)


def slab(W, H, B, rim, body, hi, face, outline=T['Outline'], fleck=None, pressed=False):
    """석판 하나: 바깥 윤곽 1 · 테 1 · 위 하이라이트 1 · 몸 (돌결) · 아래 두께(face) 2 · 모서리 1도트 깎음
       pressed 면 두께가 없어지고 위에 그림자 1줄 (글자는 코드에서 1도트 내림)"""
    img = new(W, H)
    fl = fleck or mix(body, rim, 0.16)
    stone(img, 2, 2, W - 2, H - 2, body, fl, 6 if B <= 6 else 8)
    for x in range(W):
        for y in range(H):
            d = min(x, W - 1 - x, y, H - 1 - y)
            if d == 0:
                img.putpixel((x, y), outline)
            elif d == 1:
                img.putpixel((x, y), rim)
    if pressed:
        for x in range(2, W - 2):
            img.putpixel((x, 2), mix(body, outline, 0.55))
    else:
        for x in range(2, W - 2):
            img.putpixel((x, 2), hi)
            img.putpixel((x, H - 3), face)
            img.putpixel((x, H - 4), mix(face, body, 0.5))
    for cx, cy in ((0, 0), (W - 1, 0), (0, H - 1), (W - 1, H - 1)):
        img.putpixel((cx, cy), CLEAR)
    for cx, cy in ((1, 1), (W - 2, 1), (1, H - 2), (W - 2, H - 2)):
        img.putpixel((cx, cy), outline)
    return img


BTN = {
    #            rim(기본)        rim(올림)     몸             올림 몸
    'primary':   (T['AccentDim'], T['Accent'], T['AccentDeep'], mix(T['AccentDeep'], T['AccentDim'], 0.3)),
    'secondary': (T['Line'],      T['LineHi'], T['Raised'],     mix(T['Raised'], T['Line'], 0.3)),
    'danger':    (T['DangerDim'], T['Danger'], T['DangerDeep'], mix(T['DangerDeep'], T['DangerDim'], 0.35)),
}


def button(kind, state):
    rim_n, rim_h, body_n, body_h = BTN[kind]
    if state == 'd':
        return slab(18, 18, 6, T['LineDim'], T['Sunken'], T['Sunken'], mix(T['Sunken'], T['Outline'], 0.5))
    rim = rim_h if state in ('h', 'p') else rim_n
    body = body_h if state == 'h' else body_n
    return slab(18, 18, 6, rim, body, mix(body, rim, 0.45), mix(body, T['Outline'], 0.6), pressed=state == 'p')


def panel(hi=False):
    img = slab(24, 24, 8, T['LineHi'] if hi else T['Line'], T['Panel'], T['Raised'], T['Sunken'], fleck=mix(T['Panel'], T['LineDim'], 0.45))
    return img


def card(state):
    rim = {'n': T['LineDim'], 'h': T['AccentDim'], 's': T['Accent']}[state]
    return slab(24, 24, 8, rim, T['Raised'], mix(T['Raised'], rim, 0.4), T['Sunken'], fleck=mix(T['Raised'], T['Line'], 0.3))


def inset():
    W = H = 12
    img = new(W, H)
    for y in range(H):
        for x in range(W):
            img.putpixel((x, y), T['Outline'] if (x == 0 or y == 0) else T['LineDim'] if (x == W - 1 or y == H - 1) else T['Sunken'])
    return img


def line(c):
    img = new(4, 1)
    for x in range(4):
        img.putpixel((x, 0), c)
    return img


def fill():
    img = new(4, 4)
    for y in range(4):
        for x in range(4):
            img.putpixel((x, y), WHITE if y == 0 else (204, 204, 204, 255))
    return img


def seg(on):
    img = new(4, 3)
    for y in range(3):
        for x in range(4):
            img.putpixel((x, y), (WHITE if y == 0 else (204, 204, 204, 255)) if on else (T['LineDim'] if y == 0 else T['Sunken']))
    return img


def focus():
    S, L = 16, 4
    img = new(S, S)
    for (ox, oy, sx, sy) in ((0, 0, 1, 1), (S - 1, 0, -1, 1), (0, S - 1, 1, -1), (S - 1, S - 1, -1, -1)):
        for i in range(L + 1):
            for (x, y) in ((ox + sx * i, oy), (ox, oy + sy * i)):
                img.putpixel((x, y), T['Outline'])
        for i in range(1, L):
            for (x, y) in ((ox + sx * i, oy + sy), (ox + sx, oy + sy * i)):
                img.putpixel((x, y), T['Accent'])
    return img


def marker():
    rows = ['..A..', '.AA..', '.AAA.', 'AATAA', 'ATTTA', 'ATTTA', '.AAA.']
    img = new(5, 7)
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch != '.':
                img.putpixel((x, y), T['Accent'] if ch == 'A' else T['Text1'])
    return img


def rune():
    """창 모서리 각인 하나 (새긴 선: 어두운 홈 + 아래 밝은 턱) — 영혼불 마름모"""
    rows = ['....d....', '...d.d...', '..d.l.d..', '.d.l.l.d.', 'd.l.o.l.d', '.d.l.l.d.', '..d.l.d..', '...d.d...', '....d....']
    img = new(9, 9)
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            c = {'d': T['Outline'], 'l': T['LineHi'], 'o': T['AccentDim']}.get(ch)
            if c:
                img.putpixel((x, y), c)
    return img


def dot():
    img = new(4, 4)
    for y in range(4):
        for x in range(4):
            img.putpixel((x, y), T['Outline'] if x in (0, 3) or y in (0, 3) else T['Accent'])
    for c in ((0, 0), (3, 0), (0, 3), (3, 3)):
        img.putpixel(c, CLEAR)
    return img


def tag(rim, body):
    return slab(9, 9, 3, rim, body, mix(body, rim, 0.4), mix(body, T['Outline'], 0.5))


def keycap():
    W, H = 9, 10
    img = new(W, H)
    for y in range(H):
        for x in range(W):
            d = min(x, W - 1 - x, y, H - 1 - y)
            c = T['Outline'] if d == 0 else T['LineHi'] if y == 1 else T['Sunken'] if y >= H - 3 else T['Raised']
            img.putpixel((x, y), c)
    for c in ((0, 0), (W - 1, 0), (0, H - 1), (W - 1, H - 1)):
        img.putpixel(c, CLEAR)
    return img


def track(on):
    W, H = 14, 7
    img = new(W, H)
    for y in range(H):
        for x in range(W):
            d = min(x, W - 1 - x, y, H - 1 - y)
            img.putpixel((x, y), T['Outline'] if d == 0 else (T['AccentDim'] if on else T['LineDim']) if d == 1 else (T['AccentDeep'] if on else T['Sunken']))
    for c in ((0, 0), (W - 1, 0), (0, H - 1), (W - 1, H - 1)):
        img.putpixel(c, CLEAR)
    return img


def knob(c, w=5, h=5):
    img = new(w, h)
    for y in range(h):
        for x in range(w):
            img.putpixel((x, y), T['Outline'] if x in (0, w - 1) or y in (0, h - 1) else c)
    return img


def glyph(rows):
    img = new(len(rows[0]), len(rows))
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch == '#':
                img.putpixel((x, y), WHITE)
    return img


def frame(shape):
    S = 36
    img = new(S, S)
    c = (S - 1) / 2.0
    for y in range(S):
        for x in range(S):
            dx, dy = abs(x - c), abs(y - c)
            if shape == 'weapon':
                d = max(dx, dy); r = 15
            elif shape == 'skill':
                d = (dx * dx + dy * dy) ** 0.5; r = 15.5
            elif shape == 'passive':
                d = dx + dy; r = 16.5
            else:                       # relic: 육각 (위아래 평평)
                d = max(dy, dx * 0.866 + dy * 0.5); r = 15
            if r - 1 <= d < r:
                img.putpixel((x, y), WHITE)
            elif r <= d < r + 1:
                img.putpixel((x, y), T['Outline'])
    return img


SPRITES = {}                 # 이름 → (그림, 9칸 테두리 (왼 · 아래 · 오른 · 위))
for kind in BTN:
    for st in 'nhpd':
        SPRITES['ui2_btn_%s_%s' % (kind, st)] = (button(kind, st), (6, 6, 6, 6))
SPRITES['ui2_panel'] = (panel(), (8, 8, 8, 8))
SPRITES['ui2_panel_hi'] = (panel(True), (8, 8, 8, 8))
for st in 'nhs':
    SPRITES['ui2_card_' + st] = (card(st), (8, 8, 8, 8))
SPRITES['ui2_inset'] = (inset(), (4, 4, 4, 4))
SPRITES['ui2_line'] = (line(T['LineDim']), (0, 0, 0, 0))
SPRITES['ui2_line_accent'] = (line(T['Accent']), (0, 0, 0, 0))
SPRITES['ui2_line_bronze'] = (line(T['Bronze']), (0, 0, 0, 0))
SPRITES['ui2_fill'] = (fill(), (1, 1, 1, 1))
SPRITES['ui2_seg_on'] = (seg(True), (0, 0, 0, 0))
SPRITES['ui2_seg_off'] = (seg(False), (0, 0, 0, 0))
SPRITES['ui2_focus'] = (focus(), (6, 6, 6, 6))
SPRITES['ui2_marker'] = (marker(), (0, 0, 0, 0))
SPRITES['ui2_rune'] = (rune(), (0, 0, 0, 0))
SPRITES['ui2_dot'] = (dot(), (0, 0, 0, 0))
SPRITES['ui2_tag'] = (tag(T['AccentDim'], T['AccentDeep']), (3, 3, 3, 3))
SPRITES['ui2_tag_bronze'] = (tag(T['Bronze'], mix(T['Panel'], T['Bronze'], 0.18)), (3, 3, 3, 3))
SPRITES['ui2_keycap'] = (keycap(), (3, 4, 3, 3))
SPRITES['ui2_switch_off'] = (track(False), (3, 3, 3, 3))
SPRITES['ui2_switch_on'] = (track(True), (3, 3, 3, 3))
SPRITES['ui2_switch_knob'] = (knob(T['Text1']), (0, 0, 0, 0))
SPRITES['ui2_slider_knob_n'] = (knob(T['Text1'], 4, 8), (0, 0, 0, 0))
SPRITES['ui2_slider_knob_h'] = (knob(T['Accent'], 4, 8), (0, 0, 0, 0))
SPRITES['ui2_arrow_l'] = (glyph(['..#', '.##', '###', '.##', '..#']), (0, 0, 0, 0))
SPRITES['ui2_arrow_r'] = (glyph(['#..', '##.', '###', '##.', '#..']), (0, 0, 0, 0))
SPRITES['ui2_chevron'] = (glyph(['#...#', '.#.#.', '..#..']), (0, 0, 0, 0))
SPRITES['ui2_check'] = (glyph(['......#', '.....##', '#...##.', '##.##..', '.###...']), (0, 0, 0, 0))
SPRITES['ui2_lock'] = (glyph(['.###.', '#...#', '#...#', '#####', '##.##', '##.##', '#####']), (0, 0, 0, 0))
SPRITES['ui2_x'] = (glyph(['#...#', '.#.#.', '..#..', '.#.#.', '#...#']), (0, 0, 0, 0))
for sh in ('weapon', 'skill', 'passive', 'relic'):
    SPRITES['ui2_frame_' + sh] = (frame(sh), (0, 0, 0, 0))


# ---------------------------------------------------------------- 파일 + meta
def meta(path, border):
    old = open(path, encoding='utf-8').read() if os.path.exists(path) else None
    tpl = open(os.path.join(ROOT, 'Assets', 'Sprites', 'Items', 'hp_potion.png.meta'), encoding='utf-8').read()
    g = re.search(r'guid: ([0-9a-f]{32})', old).group(1) if old else uuid.uuid4().hex
    tpl = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + g, tpl, count=1)
    tpl = re.sub(r'spritePixelsToUnits: [\d.]+', 'spritePixelsToUnits: %.5f' % PPU, tpl)
    tpl = re.sub(r'spriteBorder: \{x: 0, y: 0, z: 0, w: 0\}', 'spriteBorder: {x: %d, y: %d, z: %d, w: %d}' % border, tpl)
    tpl = re.sub(r'spriteMeshType: \d', 'spriteMeshType: 0', tpl)
    open(path, 'w', encoding='utf-8', newline='\n').write(tpl)


def folder_meta(path):
    if os.path.exists(path):
        return
    open(path, 'w', encoding='utf-8', newline='\n').write(
        'fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex)


os.makedirs(OUT, exist_ok=True)
folder_meta(OUT + '.meta')
for name, (img, border) in SPRITES.items():
    p = os.path.join(OUT, name + '.png')
    img.save(p)
    meta(p + '.meta', border)


# ---------------------------------------------------------------- 승인용 미리보기 (게임 해상도 1920x1080 · 그림은 3배 · 글자는 게임 글꼴)
def nine(name, w, h):
    """9칸 나눔 + 가운데 · 가장자리 반복(Tiled) — w · h 는 도트"""
    src, (l, b, r, t) = SPRITES[name]
    W, H = src.size
    out = new(w, h)
    cw, ch = W - l - r, H - t - b

    def blit_tiled(sx, sy, sw, sh, dx, dy, dw, dh):
        if sw <= 0 or sh <= 0 or dw <= 0 or dh <= 0:
            return
        piece = src.crop((sx, sy, sx + sw, sy + sh))
        for yy in range(0, dh, sh):
            for xx in range(0, dw, sw):
                cut = piece.crop((0, 0, min(sw, dw - xx), min(sh, dh - yy)))
                out.alpha_composite(cut, (dx + xx, dy + yy))
    blit_tiled(0, 0, l, t, 0, 0, l, t)
    blit_tiled(W - r, 0, r, t, w - r, 0, r, t)
    blit_tiled(0, H - b, l, b, 0, h - b, l, b)
    blit_tiled(W - r, H - b, r, b, w - r, h - b, r, b)
    blit_tiled(l, 0, cw, t, l, 0, w - l - r, t)
    blit_tiled(l, H - b, cw, b, l, h - b, w - l - r, b)
    blit_tiled(0, t, l, ch, 0, t, l, h - t - b)
    blit_tiled(W - r, t, r, ch, w - r, t, r, h - t - b)
    blit_tiled(l, t, cw, ch, l, t, w - l - r, h - t - b)
    return out


def up(img):
    return img.resize((img.width * DOT, img.height * DOT), Image.NEAREST)


def tint(img, c):
    px = img.copy()
    r, g, b_, a = px.split()
    r = r.point(lambda v: round(v * c[0] / 255)); g = g.point(lambda v: round(v * c[1] / 255)); b_ = b_.point(lambda v: round(v * c[2] / 255))
    return Image.merge('RGBA', (r, g, b_, a))


board = Image.open(os.path.join(ROOT, 'Assets', 'Sprites', 'UI', 'Theme', 'menu_bg.png')).convert('RGBA').resize((1920, 1080), Image.NEAREST)
board = Image.blend(Image.new('RGBA', board.size, (0, 0, 0, 255)), board, 0.42)
scrim = Image.new('RGBA', board.size, CLEAR)
sd = ImageDraw.Draw(scrim)
for x in range(0, 960):
    sd.line([(x, 0), (x, 1080)], fill=T['Void'][:3] + (round(160 * (1 - x / 960)),))
board.alpha_composite(scrim)
D = ImageDraw.Draw(board)
FONT = os.path.join(ROOT, 'Assets', 'TextMesh Pro', 'Fonts', 'Cafe24Danjunghae-v2.0 1.ttf')


def text(x, y, s, size, color, anchor='la'):
    D.text((x, y), s, font=ImageFont.truetype(FONT, int(size)), fill=color, anchor=anchor)


def place(name, x, y, wd, hd):
    board.alpha_composite(up(nine(name, wd, hd)), (x, y))


def sprite(name, x, y, color=None, scale=DOT):
    img = SPRITES[name][0]
    if color:
        img = tint(img, color)
    board.alpha_composite(img.resize((img.width * scale, img.height * scale), Image.NEAREST), (x, y))


# 메인 메뉴 (왼쪽 · 불투명 석판 줄 · 포커스 = 청록 테 + 괄호 + 영혼불 표식)
text(120, 96, 'SOUL SAVER', 72, T['Text1'][:3])
text(123, 186, '영혼을 거두는 자', 22, T['Text3'][:3])
rows = [('이어하기', 'h'), ('시작', 'n'), ('도감', 'n'), ('일일 도전', 'n'), ('설정', 'n'), ('더 보기', 'n'), ('종료', 'n')]
y = 270
for i, (label, st) in enumerate(rows):
    place('ui2_btn_secondary_' + st, 120, y, 140, 20)
    if st == 'h':
        place('ui2_focus', 120 - 6, y - 6, 144, 24)
        sprite('ui2_marker', 138, y + 9)
    text(180, y + 30, label, 34 if i == 0 else 26, T['Text1'][:3] if st == 'h' else T['Text2'][:3], 'lm')
    if label == '시작':
        sprite('ui2_arrow_l', 400, y + 22, T['Text2'])
        text(452, y + 30, '보통', 26, T['Text1'][:3], 'mm')
        sprite('ui2_arrow_r', 495, y + 22, T['Text2'])
    if label == '이어하기':
        text(180, y + 76, '거너 · 보통 · 3장 · Lv 24', 22, T['Text3'][:3], 'lm')
        y += 36
    if label == '도감':
        sprite('ui2_dot', 270, y + 18)
    y += 72
text(120, 1032, 'v1.5.1', 22, T['Text3'][:3], 'lm')

# 확인 창 (가운데 위)
px, py = 660, 96
place('ui2_panel', px, py, 240, 104)
sprite('ui2_rune', px + 12, py + 12)
text(px + 72, py + 66, '다시 시작할까요?', 34, T['Text1'][:3], 'lm')
text(px + 72, py + 120, '지금 판을 버리고 같은 영웅 · 난이도로', 26, T['Text2'][:3], 'lm')
text(px + 72, py + 156, '새로 시작합니다.', 26, T['Text2'][:3], 'lm')
text(px + 72, py + 198, '마지막 저장: 12초 전', 22, T['Text3'][:3], 'lm')
place('ui2_btn_secondary_h', px + 252, py + 228, 72, 20)
place('ui2_focus', px + 246, py + 222, 76, 24)
text(px + 252 + 108, py + 228 + 30, '취소', 26, T['Text1'][:3], 'mm')
place('ui2_btn_danger_n', px + 486, py + 228, 72, 20)
text(px + 486 + 108, py + 228 + 30, '다시 시작', 26, T['Danger'][:3], 'mm')

# 레벨업 카드 셋 (가운데 아래)
text(1020, 468, '레벨업', 48, T['Text1'][:3], 'mm')
text(1020, 510, '카드를 하나 고르세요 · 남은 레벨업 2', 22, T['Text2'][:3], 'mm')
icons = ['ab_multishot', 'ab_regen', 'ab_coin']
names = [('스킬 · Lv 2→3', '연발 사격', '공격이 2발 더 나갑니다.', T['CatSkill'] if 'CatSkill' in T else T['Line'], 's'),
         ('생존 · 새로', '재생', '초당 체력 1% 회복.', T['CatSurvival'] if 'CatSurvival' in T else T['Line'], 'n'),
         ('재물 · Lv 1→2', '탐욕', '코인 +15%.', T['CatWealth'] if 'CatWealth' in T else T['Line'], 'n')]
for i, (cat, nm, desc, cc, st) in enumerate(names):
    cx, cy = 660 + i * 246, 552 + (-6 if st == 's' else 0)
    place('ui2_card_' + st, cx, cy, 76, 110)
    if st == 's':
        place('ui2_focus', cx - 6, cy - 6, 80, 114)
    D.rectangle([cx + 6, cy + 6, cx + 228 - 7, cy + 11], fill=cc[:3])            # 범주 띠 2도트
    text(cx + 24, cy + 36, cat, 22, T['Text3'][:3], 'lm')
    place('ui2_inset', cx + 60, cy + 60, 36, 36)
    ic = Image.open(os.path.join(ROOT, 'Assets', 'Sprites', 'UI', 'Theme', icons[i] + '.png')).convert('RGBA')
    ic = ic.resize((ic.width // 2, ic.height // 2), Image.NEAREST).resize((96, 96), Image.NEAREST)
    board.alpha_composite(ic, (cx + 66, cy + 66))
    text(cx + 114, cy + 198, nm, 34, T['Text1'][:3], 'mm')
    text(cx + 114, cy + 246, desc, 22, T['Text2'][:3], 'mm')
    sprite('ui2_keycap', cx + 228 - 45, cy + 330 - 48)
    text(cx + 228 - 31, cy + 330 - 34, str(i + 1), 22, T['Text1'][:3], 'mm')
    if i == 0:
        place('ui2_tag', cx + 18, cy + 270, 30, 10)
        text(cx + 18 + 45, cy + 270 + 15, '추천', 22, T['Accent'][:3], 'mm')

# 오른쪽: 부품 모음
kx = 1440
text(kx, 96, '버튼 (기본 · 올림 · 누름 · 비활성)', 22, T['Text3'][:3], 'lm')
for r, kind in enumerate(('primary', 'secondary', 'danger')):
    for c, st in enumerate('nhpd'):
        bx, by = kx + c * 102, 120 + r * 78
        place('ui2_btn_%s_%s' % (kind, st), bx, by, 30, 20)
        lab = {'primary': '시작', 'secondary': '설정', 'danger': '포기'}[kind]
        col = T['Text3'] if st == 'd' else T['Danger'] if kind == 'danger' else T['Text1']
        text(bx + 45, by + 30 + (3 if st == 'p' else 0), lab, 22, col[:3], 'mm')
text(kx, 372, '게이지 · 눈금', 22, T['Text3'][:3], 'lm')
place('ui2_inset', kx, 396, 130, 10)
board.alpha_composite(up(tint(nine('ui2_fill', 86, 6), T['GaugeHp'])), (kx + 6, 402))
text(kx + 196, 411, '412 / 600', 22, T['Text1'][:3], 'mm')
place('ui2_inset', kx, 438, 130, 6)
board.alpha_composite(up(tint(nine('ui2_fill', 60, 2), T['GaugeSoul'])), (kx + 6, 444))
for s in range(12):
    sprite('ui2_seg_on' if s < 8 else 'ui2_seg_off', kx + s * 15, 474, T['Accent'] if s < 8 else None)
text(kx, 528, '스위치 · 슬라이더 · 키캡 · 이름표', 22, T['Text3'][:3], 'lm')
place('ui2_switch_on', kx, 552, 14, 7)
sprite('ui2_switch_knob', kx + 24, 558)
place('ui2_switch_off', kx + 66, 552, 14, 7)
sprite('ui2_switch_knob', kx + 72, 558)
place('ui2_inset', kx + 138, 559, 60, 3)
board.alpha_composite(up(tint(nine('ui2_fill', 38, 1), T['Accent'])), (kx + 141, 562))
sprite('ui2_slider_knob_h', kx + 252, 549)
sprite('ui2_keycap', kx, 600)
text(kx + 14, 614, 'E', 22, T['Text1'][:3], 'mm')
sprite('ui2_keycap', kx + 42, 600)
text(kx + 56, 614, 'R', 22, T['Text1'][:3], 'mm')
place('ui2_tag_bronze', kx + 96, 600, 36, 10)
text(kx + 96 + 54, 615, '새 기록', 22, T['Bronze'][:3], 'mm')
sprite('ui2_dot', kx + 234, 609)
text(kx + 252, 615, '새것', 22, T['Text2'][:3], 'lm')
text(kx, 678, '아이콘 틀 (무기 · 스킬 · 패시브 · 유물)', 22, T['Text3'][:3], 'lm')
for i, (sh, cname) in enumerate((('weapon', 'CatWeapon'), ('skill', 'CatSkill'), ('passive', 'CatSurvival'), ('relic', 'CatSoul'))):
    sprite('ui2_frame_' + sh, kx + i * 117, 702, T.get(cname, T['Line']))
text(kx, 840, '그림문자', 22, T['Text3'][:3], 'lm')
for i, g in enumerate(('ui2_check', 'ui2_lock', 'ui2_x', 'ui2_arrow_l', 'ui2_arrow_r', 'ui2_chevron')):
    sprite(g, kx + i * 48, 870, T['Text1'])
text(kx, 948, '색', 22, T['Text3'][:3], 'lm')
for i, n in enumerate(('Panel', 'Raised', 'Line', 'LineHi', 'Text1', 'Text2', 'Text3', 'Accent', 'Danger', 'Bronze')):
    D.rectangle([kx + i * 42, 972, kx + i * 42 + 35, 1007], fill=T[n][:3], outline=T['Outline'][:3])

# 아래 키 안내 줄
sprite('ui2_keycap', 1440, 1026)
text(1454, 1040, 'E', 22, T['Text1'][:3], 'mm')
text(1482, 1040, '고르기', 22, T['Text3'][:3], 'lm')
sprite('ui2_keycap', 1590, 1026)
text(1604, 1040, 'B', 22, T['Text1'][:3], 'mm')
text(1632, 1040, '뒤로', 22, T['Text3'][:3], 'lm')

prev = os.path.join(ROOT, 'docs', 'art', 'ui-v2-preview.png')
board.convert('RGB').save(prev)
print('그림 %d장 → %s · 미리보기 %s' % (len(SPRITES), os.path.relpath(OUT, ROOT), os.path.relpath(prev, ROOT)))
