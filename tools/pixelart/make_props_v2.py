# 그래픽 가독성 개편 2단계 묶음 6 — 작은 물건을 「그림 1칸 = 월드 1/6칸」 도트로 새로 찍음
#   코인 4단계 (캔버스 9 · 11 · 13 · 15 — CoinTag 의 단계 배율 1 · 1.2 · 1.45 · 1.7 을 곱해도 어느 단계든 같은 도트 크기)
#     0단계 = Sprites/coin_sprites_fixed/coin_01.png (guid 그대로 · PPU 만 2.8125), 1~3단계 = Resources/Coins/coin_t1~3.png (새)
#     로컬 크기는 모두 3.2 (예전 320칸 ÷ PPU 100) → 월드 크기 · 줍기 판정 그대로
#   회복 물약 Sprites/Items/hp_potion.png — 캔버스 13 · 몸 10×11 · PPU 13 (로컬 1.0 그대로)
#   거너 필살기 조준 표식 Sprites/UI/Theme/fx_target.png — 46칸 · PPU 3.59375 (로컬 12.8 그대로, TargetMark 배율 0.6 에서 1/6칸)
#   리치 왕 의식의 부술 것 Resources/Bosses/ritual_{orb,tomb,lantern}.png (새) — PPU 6 (배율 1 에서 1/6칸)
#     BossPart 가 보스 색(영혼 하늘 · 저주 보라)을 곱하므로 밝은 무채 · 옅은 색으로 찍음 (맞으면 흰색으로 번쩍 = 곱한 색이 빠져 밝아짐)
#   그림 규칙 (기획서 2-1): 바깥 외곽 1칸 #1A112C (금 · 나무 옆은 #2D192A), 위 · 살짝 왼쪽 광원, 재질마다 4단, 알파 0/255 만 · 디더 없음
#   .meta: 이미 있으면 guid · spriteID 그대로 PPU 만 바꿈, 없으면 비슷한 그림 .meta 를 복사해 guid · spriteID 만 새로
# 실행: py -3.14 -X utf8 tools/pixelart/make_props_v2.py [비교 시트 경로 (예전 그림 폴더는 같은 폴더의 old_*.png)]
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
import png
from png import Img

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
A = os.path.join(ROOT, 'Assets')
COIN0 = os.path.join(A, 'Sprites', 'coin_sprites_fixed', 'coin_01.png')
COINS = os.path.join(A, 'Resources', 'Coins')
POTION = os.path.join(A, 'Sprites', 'Items', 'hp_potion.png')
TARGET = os.path.join(A, 'Sprites', 'UI', 'Theme', 'fx_target.png')
BOSSES = os.path.join(A, 'Resources', 'Bosses')


def hx(s):
    return (int(s[1:3], 16), int(s[3:5], 16), int(s[5:7], 16), 255)


OUT = hx('#1a112c')      # 기본 외곽 (차가운 재질)
WARM = hx('#2d192a')     # 따뜻한 재질(금 · 나무) 옆 외곽
GOLD = {'d': hx('#7c3a2a'), 'g': hx('#b46826'), 'G': hx('#eaaa3a'), 'y': hx('#fede6c'), 'Y': hx('#fffad2')}


class Pic:
    # 칸(x, y) → 색. 채움 칸만 찍고 outline() 이 4방향 빈칸에 외곽을 두름
    def __init__(self, w, h):
        self.w, self.h, self.px = w, h, {}

    def rows(self, rows, pal, ox=0, oy=0):
        n = len(rows[0])
        for y, row in enumerate(rows):
            assert len(row) == n, (row, len(row), n)
            for x, ch in enumerate(row):
                if ch != '.':
                    self.px[(ox + x, oy + y)] = pal[ch]
        return self

    def outline(self, color_at):
        # color_at(이웃 채움 색들) → 외곽 색 (따뜻한 재질 옆이면 WARM)
        add = {}
        for (x, y), c in self.px.items():
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                p = (x + dx, y + dy)
                if p not in self.px:
                    add.setdefault(p, []).append(c)
        for p, cs in add.items():
            self.px[p] = color_at(cs)
        return self

    def image(self):
        im = Img(self.w, self.h)
        for (x, y), c in self.px.items():
            assert 0 <= x < self.w and 0 <= y < self.h, ('캔버스 밖', x, y)
            im.set(x, y, c)
        return im


def cold(cs):
    return OUT


def warm(cs):
    return WARM


# ───────────────────────────── 코인 ─────────────────────────────
# 정면에서 조금 내려다본 금화 (손 도트): 테두리는 볼록(위 · 왼쪽 밝음 · 아래 · 오른쪽 어두움),
#   가운데 네모 구멍 문양은 새김(위 · 왼쪽 변 그늘 · 아래 · 오른쪽 변 밝음), 큰 단계는 아래 두께 띠 1칸
#   채움 칸만 (외곽 #2D192A 는 자동) — 몸(외곽 포함) 8×7 · 10×9 · 12×10 · 14×12 = 0단계 몸 × 단계 배율
COIN_ROWS = [
    [".yYYG.",
     "yYGGGg",
     "yGgyGg",
     "GGGGgd",
     ".Ggdd."],
    ["..yYYG..",
     ".yYGGGg.",
     "yYggggGg",
     "yGgYYyGg",
     "GGgyyyGd",
     ".GGGGGd.",
     "..ggdd.."],
    ["...yYYG...",
     ".yYYGGGGg.",
     "yYGggggGGg",
     "yGGgYYyGGg",
     "yGGgYyyGgd",
     "GGGgyyyGgd",
     ".GGGGGGgd.",
     "..gggddd.."],
    ["....yYYG....",
     "..yYYGGGGg..",
     ".yYGGGGGGGg.",
     "yYGGggggGGGg",
     "yGGGgYYyGGGg",
     "yGGGgYyyGGgd",
     "GGGGgyyyGGgd",
     ".GGGGGGGGgd.",
     "..gGGGGGgd..",
     "...gggddd..."],
]
COIN_TIERS = [9, 11, 13, 15]   # 캔버스 = 9 × 단계 배율 (CoinTag.TierScale 1 · 1.2 · 1.45 · 1.7)


def coin(tier):
    rows = COIN_ROWS[tier]
    cv = COIN_TIERS[tier]
    p = Pic(cv, cv)
    bw, bh = len(rows[0]) + 2, len(rows) + 2
    p.rows(rows, GOLD, ox=(cv - bw) // 2 + 1, oy=(cv - bh) // 2 + 1)
    return p.outline(warm)


# ───────────────────────────── 회복 물약 ─────────────────────────────
POTION_PAL = {
    # 코르크 (나무 4단)
    'w': hx('#56322e'), 'W': hx('#8c5634'), 'L': hx('#b47c4a'), 'l': hx('#d0b278'),
    # 유리 목
    'c': hx('#d6def0'), 'C': hx('#9ea6c6'), 'q': hx('#616a7a'),
    # 붉은 물약 (깊은 그림자 → 반사) — 어두운 쪽은 보라로, 밝은 쪽은 주황 쪽으로
    'D': hx('#5e1440'), 'K': hx('#9a1f40'), 'R': hx('#e23654'), 'k': hx('#ff7c84'), 'S': hx('#ffe6dc'),
}
POTION_ROWS = [  # 채움 칸만 (외곽은 자동) — 몸 10×11 은 외곽 포함
    "...lL...",
    "...LW...",
    "...cq...",
    "..kRRK..",
    ".kSkRRK.",
    "RkkRRRKK",
    "RkRRRRKD",
    "RRRRRKKD",
    ".KRRKKD.",
]


def potion():
    p = Pic(13, 13)
    p.rows(POTION_ROWS, POTION_PAL, ox=2, oy=2)
    # 목 · 코르크 옆은 따뜻한 외곽, 유리 · 물약 옆은 기본 외곽
    woods = {POTION_PAL[k] for k in 'wWLl'}
    return p.outline(lambda cs: WARM if all(c in woods for c in cs) else OUT)


# ───────────────────────────── 조준 표식 ─────────────────────────────
CY = {'b': hx('#2486b8'), 'B': hx('#50d6ea'), 'i': hx('#b0f8ff'), 'I': hx('#f2ffff')}


def target():
    n = 46
    p = Pic(n, n)
    c = n / 2.0
    lx, ly = -0.26, -0.97                     # 위 · 살짝 왼쪽에서 오는 빛
    # 네 방향 괄호 고리 (반지름 19~21 · 각 ±34°)
    for y in range(n):
        for x in range(n):
            dx, dy = x + 0.5 - c, y + 0.5 - c
            r = math.hypot(dx, dy)
            if not (18.6 <= r <= 21.1):
                continue
            ang = math.degrees(math.atan2(-dy, dx)) % 90.0
            if 34.0 < ang < 56.0:
                continue
            nx, ny = dx / r, dy / r
            lit = nx * lx + ny * ly
            if r >= 19.85:                     # 바깥 반: 위를 보는 쪽이 밝음
                col = 'i' if lit > 0.5 else 'B' if lit > -0.75 else 'b'
            else:                              # 안쪽 반: 안쪽(가운데)을 보므로 빛 방향이 반대 — 아래 고리는 안쪽이 밝음
                col = 'B' if -lit > -0.3 else 'b'
            p.px[(x, y)] = CY[col]
    # 안쪽을 가리키는 금 삼각형 넷 (밑변 7 · 높이 5)
    def tri(tip_x, tip_y, ux, uy):
        # (ux, uy) = 꼭짓점 → 밑변 방향
        for k in range(5):
            half = min(3, k)
            for s in range(-half, half + 1):
                x = tip_x + ux * k + (-uy) * s
                y = tip_y + uy * k + ux * s
                # 빛: 위 · 왼쪽 가장자리는 밝게, 아래 · 오른쪽은 어둡게
                side = (-uy) * s * lx + ux * s * ly          # 옆으로 간 방향의 빛 성분
                back = ux * k * lx + uy * k * ly
                v = side * 0.9 + back * 0.25
                col = 'y' if v > 0.9 else 'G' if v > -0.9 else 'g'
                if k == 4 and abs(s) <= 3:
                    col = 'g' if (ux * lx + uy * ly) < 0 else 'y'
                p.px[(int(x), int(y))] = GOLD[col]
    m = int(c)
    tri(m - 1, 13, 0, -1); tri(m, 13, 0, -1)          # 위 (아래를 가리킴) — 가운데 2칸 폭
    tri(m - 1, n - 14, 0, 1); tri(m, n - 14, 0, 1)    # 아래
    tri(13, m - 1, -1, 0); tri(13, m, -1, 0)          # 왼쪽
    tri(n - 14, m - 1, 1, 0); tri(n - 14, m, 1, 0)    # 오른쪽
    # 가운데 마름모 보석 (반지름 2)
    for y in range(n):
        for x in range(n):
            dx, dy = x + 0.5 - c, y + 0.5 - c
            if abs(dx) + abs(dy) <= 2.6:
                p.px[(x, y)] = CY['I' if dx + dy < -1.0 else 'i' if dx + dy < 0.5 else 'B']
    golds = set(GOLD.values())
    return p.outline(lambda cs: WARM if all(c in golds for c in cs) else OUT)


# ───────────────────────────── 리치 왕 의식 물건 (BossPart 가 색을 곱함) ─────────────────────────────
PALE = {
    # 밝은 무채 4단 (차가운 그림자) — 곱하면 보스 색의 명암이 됨
    '1': hx('#ffffff'), '2': hx('#d4d8ea'), '3': hx('#9a9cc0'), '4': hx('#62628c'),
    'x': hx('#2a2448'),   # 눈 · 구멍 (곱해도 어두움)
}
STONE = {
    '1': hx('#d2d0e2'), '2': hx('#a6a4c0'), '3': hx('#7a7698'), '4': hx('#524e72'),
    'e': hx('#ffffff'), 'E': hx('#e8e4ff'),   # 새긴 저주 문양 (빛남)
    'p': hx('#8c8298'), 'm': hx('#665c74'), 'n': hx('#463e56'), 'k': hx('#2e2840'),   # 흙 무덤
}
IRON = {
    'a': hx('#c4c6d6'), 'b': hx('#9294ae'), 'c': hx('#64668a'), 'd': hx('#40405e'),
    'q': hx('#5a5a82'), 'g': hx('#8a8ab6'),   # 유리 (어두움 · 불빛 받은 쪽)
    'w': hx('#ffffff'), 'e': hx('#eef4ff'), 'f': hx('#c0cce8'),   # 불꽃
}


def ritual_orb():
    # 영혼구 12×12: 떠 있는 둥근 영혼 + 위로 꼬리 불꽃 + 텅 빈 두 눈
    p = Pic(12, 12)
    cx, cy, r = 6.0, 7.0, 4.0
    L = (-0.35, -0.75, 0.56)
    for y in range(12):
        for x in range(12):
            u, v = (x + 0.5 - cx) / r, (y + 0.5 - cy) / r
            d2 = u * u + v * v
            if d2 > 1.0:
                continue
            w = math.sqrt(1.0 - d2)
            lam = u * L[0] + v * L[1] + w * L[2]
            p.px[(x, y)] = PALE['1' if lam > 0.78 else '2' if lam > 0.42 else '3' if lam > 0.05 else '4']
    # 꼬리 불꽃 (왼쪽 위로 휨)
    for (x, y, c) in ((5, 3, '2'), (6, 3, '3'), (5, 2, '2'), (4, 2, '1'), (4, 1, '2')):
        p.px[(x, y)] = PALE[c]
    # 눈 (세로 2칸) · 입
    for (x, y) in ((4, 7), (4, 8), (7, 7), (7, 8)):
        p.px[(x, y)] = PALE['x']
    return p.outline(cold)


TOMB_ROWS = [  # 12×16 채움 (외곽 자동) — 둥근 머리 묘비 + 오른쪽 두께 + 저주 십자 + 흙 무덤
    "....1112....",
    "..11122223..",
    ".1122E22233.",
    ".1222e22233.",
    ".12Eeeee233.",
    ".1222e22233.",
    ".1222e22234.",
    ".1222e22234.",
    ".1222222434.",
    ".1222224334.",
    ".1222222334.",
    ".1222223334.",
    ".ppmmmmmmnn.",
    "ppmmmmmmnnnn",
    "pmmmmmmnnnnk",
    ".mmmmnnnnkk.",
]

LANTERN_ROWS = [  # 12×16 채움 — 손잡이 고리 · 뚜껑 · 유리 안 불꽃 · 받침
    ".....ab.....",
    "....a..c....",
    ".....bc.....",
    "...aabbcc...",
    "..aaabbbcc..",
    ".bddddddddc.",
    ".agqqfqqqqc.",
    ".agqfeqqqqc.",
    ".agfewfqqqc.",
    ".afewwfgqqc.",
    ".afewwfgqqc.",
    ".agfwefgqqc.",
    ".agqffgqqqc.",
    ".bddddddddc.",
    "..aabbbccc..",
    "....bbcc....",
]


def ritual_rows(rows, pal):
    p = Pic(14, 18)
    p.rows(rows, pal, ox=1, oy=1)
    return p.outline(cold)


# ───────────────────────────── 저장 · .meta ─────────────────────────────
def write_meta(path, template, ppu):
    meta = path + '.meta'
    if os.path.exists(meta):
        text = open(meta, encoding='utf-8').read()          # guid · spriteID 그대로
    else:
        text = open(template, encoding='utf-8').read()
        text = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + uuid.uuid4().hex, text, count=1, flags=re.M)
        text = re.sub(r'spriteID: [0-9a-f]+', 'spriteID: ' + uuid.uuid4().hex, text, count=1)
    text = re.sub(r'spritePixelsToUnits: [0-9.]+', 'spritePixelsToUnits: %s' % ('%g' % ppu), text, count=1)
    # 도트 그림: 점 필터 · 밉맵 끔 · 무압축 · Tight
    text = re.sub(r'filterMode: -?\d+', 'filterMode: 0', text, count=1)
    text = re.sub(r'enableMipMap: \d', 'enableMipMap: 0', text, count=1)
    text = re.sub(r'textureCompression: \d', 'textureCompression: 0', text)
    with open(meta, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)


def folder_meta(folder):
    meta = folder + '.meta'
    if os.path.exists(meta):
        return
    with open(meta, 'w', encoding='utf-8', newline='\n') as f:
        f.write('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                '  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex)


def build():
    made = []
    os.makedirs(COINS, exist_ok=True)
    os.makedirs(BOSSES, exist_ok=True)
    folder_meta(COINS)
    folder_meta(BOSSES)
    coin_meta = COIN0 + '.meta'
    for t, cv in enumerate(COIN_TIERS):
        im = coin(t).image()
        path = COIN0 if t == 0 else os.path.join(COINS, 'coin_t%d.png' % t)
        png.write(im, path)
        write_meta(path, coin_meta, cv / 3.2)
        made.append(('coin_t%d' % t, im, cv / 3.2))
    im = potion().image()
    png.write(im, POTION)
    write_meta(POTION, POTION + '.meta', 13)
    made.append(('hp_potion', im, 13))
    im = target().image()
    png.write(im, TARGET)
    write_meta(TARGET, TARGET + '.meta', 46 / 12.8)
    made.append(('fx_target', im, 46 / 12.8))
    for name, pic in (('ritual_orb', ritual_orb()), ('ritual_tomb', ritual_rows(TOMB_ROWS, STONE)),
                      ('ritual_lantern', ritual_rows(LANTERN_ROWS, IRON))):
        im = pic.image()
        path = os.path.join(BOSSES, name + '.png')
        png.write(im, path)
        write_meta(path, POTION + '.meta', 6)
        made.append((name, im, 6))
    for name, im, ppu in made:
        print('%-15s %2d×%-2d  PPU %g' % (name, im.w, im.h, ppu))
    return made


def sheet(made, out):
    # 왼쪽 = 예전 그림 (같은 폴더의 old_*.png), 오른쪽 = 새 그림 — 모두 칸당 8배 · 어두운 바닥 위
    old_dir = os.path.dirname(out)
    S = 8
    bg = (40, 36, 52, 255)
    cells = []
    olds = {'coin_t0': 'old_coin_01.png', 'hp_potion': 'old_hp_potion.png', 'fx_target': 'old_fx_target.png'}
    for name, im, ppu in made:
        o = None
        if name in olds and os.path.exists(os.path.join(old_dir, olds[name])):
            o = png.read(os.path.join(old_dir, olds[name]))
        cells.append((o, im))
    W = 0
    rows = []
    for o, im in cells:
        sz = im.w * S
        rows.append(sz)
        W = max(W, sz * 2 + 30)
    H = sum(rows) + 10 * (len(rows) + 1)
    sh = Img(W, H, bg)
    y = 10
    for (o, im), sz in zip(cells, rows):
        if o is not None:
            # 예전 그림은 새 그림과 같은 월드 크기로 (가장 가까운 칸)
            for yy in range(sz):
                for xx in range(sz):
                    c = o.get(xx * o.w // sz, yy * o.h // sz)
                    if c[3] > 0:
                        sh.blend(10 + xx, y + yy, c)
        for yy in range(sz):
            for xx in range(sz):
                c = im.get(xx // S, yy // S)
                if c[3] > 0:
                    sh.blend(20 + sz + xx, y + yy, c)
        y += sz + 10
    png.write(sh, out)
    print('비교 시트:', out)


if __name__ == '__main__':
    made = build()
    if len(sys.argv) > 1:
        sheet(made, sys.argv[1])
