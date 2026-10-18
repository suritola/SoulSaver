# 궁수 갈래 아이콘 (2026-10-19 · 빌드 갈래) → Assets/Resources/Icons/Paths/archer_*.png  (32x32)
#   archer_<갈래 0~4>        입문 카드 · HUD 입문 칸 (= 갈래 문장)
#   archer_<갈래>_r1 · _r2 · _m  공명 I · II · 극의 알림
#   archer_x2 · archer_x3     새 교차 (유성 관통 · 돌풍 덫, Crosses 배열 순서)
# 갈래 색(PathDef.color) 둥근 판 + 문양, 공명은 별 점 · 극의는 금테 (make_ability_icons.py 와 같은 틀)
# 실행: python tools/pixelart/make_paths_archer.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line, ROOT

OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'Paths')
W = (240, 240, 248, 255); K = (25, 20, 30, 255); Y = (240, 200, 80, 255); B = (120, 80, 50, 255)
G = (175, 180, 195, 255); GOLD = (255, 214, 90, 255)

# 갈래 색 (Paths.Archer.cs 의 color 와 같게)
COL = {0: (0.45, 0.8, 1.0), 1: (1.0, 0.85, 0.4), 2: (0.55, 0.9, 0.4), 3: (0.7, 1.0, 0.9), 4: (0.75, 0.6, 1.0)}


def rgb(c, k, a=255):
    return (int(min(255, c[0] * 255 * k)), int(min(255, c[1] * 255 * k)), int(min(255, c[2] * 255 * k)), a)


def arrow(img, x0, y0, x1, y1, col=W, shaft=(205, 155, 95, 255), w=1.3):
    line(img, x0, y0, x1, y1, K, w + 1.2)
    line(img, x0, y0, x1, y1, shaft, w)
    ang = math.atan2(y1 - y0, x1 - x0)
    bx = [(x1 - math.cos(ang + s) * 4.5, y1 - math.sin(ang + s) * 4.5) for s in (-0.5, 0.5)]
    for t in range(6):
        k = t / 5
        line(img, x1, y1, bx[0][0] + (bx[1][0] - bx[0][0]) * k, bx[0][1] + (bx[1][1] - bx[0][1]) * k, col, 1.1)
    for s in (-0.8, 0.8):
        line(img, x0, y0, x0 - math.cos(ang + s) * 3, y0 - math.sin(ang + s) * 3, (230, 90, 80, 255), 1.1)


def bow(img, cx, cy, r, a0, a1, col=B):
    n = 24
    pts = [(cx + math.cos(a0 + (a1 - a0) * i / n) * r, cy + math.sin(a0 + (a1 - a0) * i / n) * r) for i in range(n + 1)]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        line(img, x0, y0, x1, y1, col, 1.8)
    line(img, pts[0][0], pts[0][1], pts[-1][0], pts[-1][1], (230, 230, 230, 200), 0.8)


def jaws(img, cx, cy, col=G, r=8.0):
    # 덫: 벌어진 톱니 턱 (위 · 아래 반달 + 안쪽으로 향한 이빨)
    for half in (-1, 1):
        n = 18
        pts = [(cx + math.cos(math.pi * i / n) * r, cy + half * math.sin(math.pi * i / n) * r * 0.55) for i in range(n + 1)]
        for (a, b), (c, d) in zip(pts, pts[1:]):
            line(img, a, b, c, d, K, 2.6)
            line(img, a, b, c, d, col, 1.4)
        for i in (3, 6, 9, 12, 15):
            x, y = pts[i]
            line(img, x, y, x + (cx - x) * 0.25, y - half * 2.6, W, 1.1)
    line(img, cx - r - 1, cy, cx + r + 1, cy, K, 1.6)
    disc(img, cx - r - 1, cy, 1.6, Y); disc(img, cx + r + 1, cy, 1.6, Y)


def swirl(img, cx, cy, r, col, turns=1.3, w=1.4):
    n = 40
    prev = None
    for i in range(n + 1):
        t = i / n
        a = t * turns * 2 * math.pi
        rr = r * (0.25 + 0.75 * t)
        p = (cx + math.cos(a) * rr, cy + math.sin(a) * rr)
        if prev: line(img, prev[0], prev[1], p[0], p[1], col, w)
        prev = p


def star(img, cx, cy, r, col):
    for k in range(4):
        a = 0.3 + k * math.pi / 2
        line(img, cx, cy, cx + math.cos(a) * r, cy + math.sin(a) * r, col, 1.6)
    disc(img, cx, cy, 1.4, W)


def glyph(img, g):
    if g == 'long':
        bow(img, 13, 16, 11, -1.25, 1.25)
        arrow(img, 5, 16, 28, 16, W)
        disc(img, 19, 16, 2.2, (60, 40, 50, 255)); disc(img, 24, 16, 2.2, (60, 40, 50, 255))
        line(img, 5, 16, 28, 16, (255, 240, 170, 255), 0.9)
    elif g == 'rapid':
        for i, y in enumerate((9, 16, 23)):
            arrow(img, 6 + i * 2, y, 22 + i * 2, y, Y)
        for y in (9, 16, 23): line(img, 3, y, 6, y, (255, 255, 255, 140), 1)
    elif g == 'trap':
        jaws(img, 16, 18)
        arrow(img, 21, 4, 16, 13, W)
    elif g == 'wind':
        swirl(img, 14, 16, 10, (220, 255, 245, 255))
        arrow(img, 8, 24, 26, 8, W)
    elif g == 'sky':
        disc(img, 11, 8, 4, (235, 230, 255, 255)); disc(img, 17, 7, 5, (235, 230, 255, 255)); disc(img, 22, 9, 3.5, (235, 230, 255, 255))
        for x in (10, 16, 22):
            arrow(img, x, 12, x, 25, Y)
    elif g == 'x_meteor':
        disc(img, 20, 11, 5, (255, 180, 80, 255)); disc(img, 20, 11, 2.5, (255, 240, 180, 255))
        for k in range(3): line(img, 15 - k * 3, 16 + k * 3, 18 - k * 2, 13 + k * 2, (255, 160, 60, 200), 1.2)
        arrow(img, 4, 27, 28, 5, W)
    elif g == 'x_gust':
        swirl(img, 16, 12, 8, (220, 255, 245, 255), 1.1, 1.2)
        jaws(img, 16, 22)


def plate(img, c, tier):
    disc(img, 16, 16, 15, rgb(c, 0.32))
    disc(img, 14, 13, 11, rgb(c, 0.8, 90))
    ring(img, 16, 16, 15, 1.5, (20, 15, 25, 255))
    if tier == 'm':
        ring(img, 16, 16, 14.5, 2.2, GOLD)
        ring(img, 16, 16, 12.8, 0.8, (255, 245, 200, 200))
    elif tier == 'r2':
        ring(img, 16, 16, 14.3, 1.2, rgb(c, 1.15))


def pips(img, tier, c):
    if tier == 'r1':
        star(img, 26, 6, 3.2, rgb(c, 1.2))
    elif tier == 'r2':
        star(img, 24, 6, 3.2, rgb(c, 1.2)); star(img, 29, 10, 2.6, rgb(c, 1.2))
    elif tier == 'm':
        star(img, 26, 6, 4, GOLD)


def meta(path):
    tpl = open(os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'ability_900.png.meta'), encoding='utf-8').read()
    tpl = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
    open(path, 'w', encoding='utf-8', newline='\n').write(tpl)


def save(img, name):
    out = os.path.join(OUT, name + '.png')
    write(img, out)
    if not os.path.exists(out + '.meta'):
        meta(out + '.meta')


def main():
    os.makedirs(OUT, exist_ok=True)
    names = ['long', 'rapid', 'trap', 'wind', 'sky']
    n = 0
    for p, g in enumerate(names):
        for tier in (None, 'r1', 'r2', 'm'):
            img = Img(32, 32)
            plate(img, COL[p], tier)
            glyph(img, g)
            pips(img, tier, COL[p])
            save(img, 'archer_%d' % p + ('' if tier is None else '_' + tier))
            n += 1
    for k, (g, a, b) in {2: ('x_meteor', 0, 4), 3: ('x_gust', 3, 2)}.items():
        img = Img(32, 32)
        mix = tuple((COL[a][i] + COL[b][i]) / 2 for i in range(3))
        plate(img, mix, None)
        glyph(img, g)
        save(img, 'archer_x%d' % k)
        n += 1
    print('wrote', n, 'icons')


if __name__ == '__main__':
    main()
