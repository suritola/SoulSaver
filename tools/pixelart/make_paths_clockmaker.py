# 시계공 갈래 그림 (2026-10-19 · 빌드 갈래, Paths/Paths.Clockmaker.cs): 32x32
#   갈래 색 메달(위 왼쪽이 밝은 3단 테 + 짙은 안쪽) 안에 갈래 상징, 남보라 외곽선 — 초월 그림(make_transcend_icons)과 같은 틀
#   → Assets/Resources/Icons/Paths/clockmaker_<갈래 0~4>.png          입문 카드 · HUD 갈래 칸
#     clockmaker_<갈래>_r1 · _r2 (아래 눈금 점 1 · 2개) · _m (금테 + 별)   공명 I · II · 극의 알림
#     clockmaker_x2 · clockmaker_x3                                      새 교차 (톱니 메아리 · 정각 되감기)
# 메타: ability_900.png.meta 를 본떠 guid 만 새로 (filterMode 0 · textureType 8 · spriteMode 1 그대로)
# 실행: python tools/pixelart/make_paths_clockmaker.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line, ROOT

OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'Paths')
TPL = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'ability_900.png.meta')

K = (26, 17, 44, 255)                   # 남보라 외곽선
W = (255, 255, 255, 255)
GL = (255, 236, 160, 255); G = (255, 200, 86, 255); GM = (234, 170, 58, 255); GD = (160, 98, 34, 255)
BR = (205, 160, 95, 255); BRL = (240, 205, 140, 255); BRD = (130, 88, 46, 255)       # 놋쇠 톱니
BL = (196, 240, 255, 255); B = (104, 200, 255, 255); BD = (52, 120, 200, 255)
PL = (225, 200, 255, 255); P = (175, 130, 250, 255); PD = (105, 65, 175, 255)
RL = (255, 175, 150, 255); R = (240, 95, 80, 255); RD = (160, 45, 45, 255)
CX = CY = 16.0

# 갈래 색 (PathDef.color 와 같게) → 테 4단
PATH_COLORS = [(0.85, 0.75, 0.55), (1.0, 0.8, 0.35), (0.6, 0.8, 1.0), (0.8, 0.65, 1.0), (1.0, 0.5, 0.4)]


def shade(c, k):
    r, g, b = c
    if k >= 0:
        return (round(255 * (r + (1 - r) * k)), round(255 * (g + (1 - g) * k)), round(255 * (b + (1 - b) * k)), 255)
    k = -k
    return (round(255 * r * (1 - k)), round(255 * g * (1 - k)), round(255 * b * (1 - k)), 255)


def medallion(img, col, gold=False):
    if gold:
        rim = [GL, G, GM, GD]
    else:
        rim = [shade(col, 0.55), shade(col, 0.0), shade(col, -0.25), shade(col, -0.5)]
    in1, in2, in3 = shade(col, -0.62), shade(col, -0.72), shade(col, -0.8)
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - CX, y + 0.5 - CY
            d = math.hypot(dx, dy)
            if d > 15.0:
                continue
            if d >= 12.4:
                lit = (-dx - dy) / (d * 1.414)
                c = rim[0] if lit > 0.75 else rim[1] if lit > 0.15 else rim[2] if lit > -0.6 else rim[3]
                if 13.1 < d < 13.9 and lit < 0.6:
                    c = rim[2] if c in (rim[0], rim[1]) else rim[3]
                img.set(x, y, c)
            else:
                k = (dy + d * 0.3) / 12.4
                img.set(x, y, in1 if k < -0.25 else in2 if k < 0.45 else in3)
    img.set(8, 6, W); img.set(7, 7, W); img.set(9, 5, rim[0])


def gear(img, cx, cy, r, teeth, lit, mid, dark, rot=0.0, hole=True):
    # 톱니바퀴: 몸통 원 + 네모난 이빨 (위 왼쪽이 밝게) + 가운데 구멍
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            d = math.hypot(dx, dy)
            a = math.atan2(dy, dx) + rot
            tooth = ((a * teeth / (2 * math.pi)) % 1.0) < 0.5
            if d <= r * 0.7 or (tooth and d <= r):
                l = (-dx - dy) / (max(d, 0.01) * 1.414)
                img.set(x, y, lit if l > 0.45 else mid if l > -0.35 else dark)
    if hole:
        disc(img, cx, cy, max(1.0, r * 0.3), dark)
        img.set(int(cx - r * 0.3), int(cy - r * 0.3), lit)


def gear_o(img, cx, cy, r, teeth, lit, mid, dark, rot=0.0):
    # 겹친 톱니끼리 구분되게 남보라 테를 먼저 깔고 그 위에 톱니
    gear(img, cx, cy, r + 1.0, teeth, K, K, K, rot, hole=False)
    gear(img, cx, cy, r, teeth, lit, mid, dark, rot)


def hourglass(img, cx, cy, h, sand, glass, frame):
    for y in range(int(cy - h), int(cy + h) + 1):
        t = abs(y + 0.5 - cy) / h
        half = 0.8 + 5.0 * t
        for x in range(int(cx - half), int(cx + half) + 1):
            low = y > cy
            fill = (low and t > 0.35) or (not low and 0.1 < t < 0.55)
            img.set(x, y, sand if fill else glass)
    line(img, cx - 6.5, cy - h - 0.5, cx + 6.5, cy - h - 0.5, frame, 1.6)
    line(img, cx - 6.5, cy + h + 0.5, cx + 6.5, cy + h + 0.5, frame, 1.6)


def arc_arrow(img, cx, cy, r, a0, a1, col, w=1.4, head=True, ccw=True):
    # a0 → a1 (도) 로 도는 화살표
    n = 40
    pts = []
    for i in range(n + 1):
        a = math.radians(a0 + (a1 - a0) * i / n)
        pts.append((cx + r * math.cos(a), cy - r * math.sin(a)))
    for i in range(n):
        line(img, pts[i][0], pts[i][1], pts[i + 1][0], pts[i + 1][1], col, w)
    if head:
        ex, ey = pts[-1]
        px, py = pts[-3]
        dx, dy = ex - px, ey - py
        L = math.hypot(dx, dy) or 1
        dx, dy = dx / L, dy / L
        nx, ny = -dy, dx
        line(img, ex, ey, ex - dx * 3 + nx * 2.5, ey - dy * 3 + ny * 2.5, col, w)
        line(img, ex, ey, ex - dx * 3 - nx * 2.5, ey - dy * 3 - ny * 2.5, col, w)


def burst(img, cx, cy, r, c1, c2):
    for i in range(8):
        a = math.radians(i * 45 + 22)
        line(img, cx + math.cos(a) * r * 0.4, cy + math.sin(a) * r * 0.4, cx + math.cos(a) * r, cy + math.sin(a) * r, c1 if i % 2 else c2, 1.4)
    disc(img, cx, cy, r * 0.38, c2)
    disc(img, cx, cy, r * 0.18, W)


# ---------------- 상징
def swarm(img):
    # 톱니 둘이 나란히 날아감 + 튕김 궤적
    line(img, 6, 22, 11, 17, (200, 180, 140, 160), 1.0)
    line(img, 11, 17, 15, 21, (200, 180, 140, 160), 1.0)
    gear_o(img, 11.5, 12, 6.0, 7, BRL, BR, BRD, 0.2)
    gear_o(img, 21, 20.5, 6.0, 7, BRL, BR, BRD, 0.6)
    img.set(10, 10, W)


def grand(img):
    # 커다란 금빛 정각 톱니 (바늘이 12시)
    gear(img, 16, 16.5, 10.2, 10, GL, G, GD, 0.15, hole=False)
    disc(img, 16, 16.5, 6.0, (255, 246, 214, 255))
    for i in range(12):
        a = math.radians(i * 30)
        img.set(int(16 + math.sin(a) * 5.0), int(16.5 - math.cos(a) * 5.0), GD)
    line(img, 16, 16.5, 16, 11.5, K, 1.3)
    line(img, 16, 16.5, 16, 13.0, K, 1.6)
    disc(img, 16, 16.5, 1.1, GD)


def travel(img):
    # 모래시계 + 거꾸로 도는 화살
    arc_arrow(img, 16, 16, 9.6, 40, 320, BL, 1.5)
    hourglass(img, 16, 16, 6.2, G, BD, BRL)
    img.set(13, 11, W)


def echo(img):
    # 톱니 하나와 옅어지는 잔상 둘
    gear_o(img, 8.5, 22, 3.8, 6, (110, 85, 160, 255), (88, 66, 130, 255), (66, 48, 104, 255), 0.3)
    gear_o(img, 13.5, 17.5, 4.6, 7, (150, 115, 215, 255), (120, 88, 185, 255), (86, 60, 145, 255), 0.5)
    gear_o(img, 20.5, 11.5, 6.0, 7, PL, P, PD, 0.7)
    img.set(17, 10, W)


def debt(img):
    # 시계 판 + 붉은 사슬 고리 (빚) + 아래로 새는 게이지
    disc(img, 15, 14, 7.4, (250, 230, 215, 255))
    ring(img, 15, 14, 7.4, 1.3, R)
    line(img, 15, 14, 15, 9, K, 1.2)
    line(img, 15, 14, 19, 14, K, 1.2)
    disc(img, 15, 14, 1.0, RD)
    ring(img, 21, 22, 2.6, 1.2, RL)
    ring(img, 24.5, 24.5, 2.6, 1.2, R)
    line(img, 9, 22, 9, 26, RD, 1.6)
    line(img, 9, 26, 7, 24, RD, 1.4); line(img, 9, 26, 11, 24, RD, 1.4)


def x_gearecho(img):
    # 튕기는 톱니 + 보라 잔상
    line(img, 7, 9, 13, 16, (200, 180, 140, 170), 1.0)
    gear_o(img, 11, 21, 5.0, 7, (150, 115, 215, 255), (120, 88, 185, 255), (86, 60, 145, 255), 0.4)
    gear_o(img, 20.5, 12, 6.0, 7, BRL, BR, BRD, 0.1)
    img.set(18, 9, W)


def x_noonrewind(img):
    # 되감기 화살 고리 안에서 터지는 정각
    arc_arrow(img, 16, 16, 10.0, 50, 330, BL, 1.5)
    burst(img, 16, 16, 7.0, GM, G)


SYMBOLS = [swarm, grand, travel, echo, debt]


def outline(img):
    src = [[img.px[y][x] for x in range(img.w)] for y in range(img.h)]
    for y in range(img.h):
        for x in range(img.w):
            if src[y][x][3] > 0: continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < img.w and 0 <= yy < img.h and src[yy][xx][3] > 0:
                    img.px[y][x] = K
                    break


def symbol_outline(img, sym):
    for y in range(32):
        for x in range(32):
            if sym.px[y][x][3] > 0: continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                c = sym.get(x + dx, y + dy)
                if c[3] > 160 and math.hypot(x + 0.5 - CX, y + 0.5 - CY) < 12.4:
                    img.set(x, y, K)
                    break


def pips(img, n, col):
    # 메달 아래 테 위에 눈금 점 (공명 I = 1 · II = 2)
    xs = [16] if n == 1 else [13, 19]
    for x in xs:
        disc(img, x, 25.5, 2.2, K)
        disc(img, x, 25.5, 1.5, col)
        img.set(x - 1, 24, W)


def star(img, cx, cy, r, col):
    for i in range(5):
        a = math.radians(-90 + i * 72)
        line(img, cx, cy, cx + math.cos(a) * r, cy + math.sin(a) * r, col, 1.4)
    disc(img, cx, cy, r * 0.35, W)


def make(draw, col, variant=None):
    img = Img(32, 32)
    medallion(img, col, gold=(variant == 'm'))
    sym = Img(32, 32)
    draw(sym)
    for y in range(32):
        for x in range(32):
            if math.hypot(x + 0.5 - CX, y + 0.5 - CY) >= 12.4: sym.px[y][x] = (0, 0, 0, 0)
    symbol_outline(img, sym)
    img.paste(sym, 0, 0)
    if variant == 'r1': pips(img, 1, GL)
    if variant == 'r2': pips(img, 2, GL)
    if variant == 'm': star(img, 25.5, 6.5, 3.4, GL)
    outline(img)
    return img


def save(img, name, tpl):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + '.png')
    if not os.path.exists(path + '.meta'):           # 메타를 먼저 (편집기가 기본 설정 메타를 만들지 않게)
        meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
        open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta)
    write(img, path)
    print('그림 저장:', name)


def main():
    tpl = open(TPL, encoding='utf-8').read()
    for i, draw in enumerate(SYMBOLS):
        col = PATH_COLORS[i]
        save(make(draw, col), 'clockmaker_%d' % i, tpl)
        for v in ('r1', 'r2', 'm'):
            save(make(draw, col, v), 'clockmaker_%d_%s' % (i, v), tpl)
    mix = lambda a, b: tuple((PATH_COLORS[a][k] + PATH_COLORS[b][k]) * 0.5 for k in range(3))
    save(make(x_gearecho, mix(0, 3)), 'clockmaker_x2', tpl)
    save(make(x_noonrewind, mix(1, 2)), 'clockmaker_x3', tpl)


if __name__ == '__main__':
    main()
