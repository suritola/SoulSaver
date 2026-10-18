# 유령 갈래 그림 (빌드 갈래 2026-10-19 · Paths/Paths.Ghost.cs): 32x32
#   갈래 색 메달(위 왼쪽이 밝은 3단 테 + 짙은 남색 안쪽) 안에 갈래마다 상징, 남보라 외곽선 (초월 카드 그림과 같은 틀)
#   공명 I · II = 메달 아래 보석 하나 · 둘, 극의 = 금테 + 별, 새 교차 = 두 갈래 색을 반씩 섞은 테
#   → Assets/Resources/Icons/Paths/ghost_<갈래 0~4>.png · ghost_<갈래>_r1 · _r2 · _m · ghost_x<교차 번호>
#     (갈래 번호 = Paths.Ghost.cs BuildPaths 순서: 0 혹한 · 1 산산조각 · 2 영혼 군단 · 3 사령술 · 4 유체 이탈,
#      교차 번호 = BuildCrosses 순서: 2 서리 군단 · 3 안에서 얼리기 — 0 · 1 은 기존 진화라 그림이 필요 없음)
# 실행: python tools/pixelart/make_paths_ghost.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line, ROOT
from make_menu_icons import retry

OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'Paths')
META_SRC = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'ability_900.png.meta')

K = (26, 17, 44, 255)                   # 남보라 외곽선 (새 그림체와 같은 색)
W = (255, 255, 255, 255)
IN1 = (46, 54, 96, 255); IN2 = (32, 38, 74, 255); IN3 = (22, 26, 54, 255)       # 안쪽 (차가운 남색)
GOLD = [(255, 236, 160, 255), (255, 200, 86, 255), (234, 170, 58, 255), (160, 98, 34, 255)]
ICE = [(236, 250, 255, 255), (170, 228, 255, 255), (104, 190, 240, 255), (52, 112, 190, 255)]
SOUL = [(236, 240, 255, 255), (188, 196, 255, 255), (130, 140, 240, 255), (76, 80, 180, 255)]
VIO = [(232, 210, 255, 255), (184, 140, 240, 255), (130, 84, 200, 255), (78, 46, 140, 255)]
CX = CY = 16.0

# 갈래 색 (PathDef.color × 255) → 테 4단 (밝음 · 기본 · 중간 · 어둠)
PATH_COLORS = [(0.6, 0.9, 1.0), (0.75, 0.95, 1.0), (0.7, 0.75, 1.0), (0.6, 0.45, 0.85), (0.85, 0.9, 1.0)]


def shades(rgb):
    r, g, b = rgb
    def c(k, add=0):
        return (min(255, int(r * 255 * k + add)), min(255, int(g * 255 * k + add)), min(255, int(b * 255 * k + add)), 255)
    return [c(1.0, 50), c(1.0), c(0.78), c(0.5)]


def mix(a, b, t=0.5):
    return tuple(a[i] + (b[i] - a[i]) * t for i in range(3))


def medallion(img, rim, rim2=None):
    # 테: 반지름 12.4~15, 위 왼쪽이 밝게 · rim2 가 있으면 오른쪽 반은 그 색 (교차)
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - CX, y + 0.5 - CY
            d = math.hypot(dx, dy)
            if d > 15.0:
                continue
            if d >= 12.4:
                s = rim2 if rim2 is not None and dx > 0 else rim
                lit = (-dx - dy) / (d * 1.414)
                c = s[0] if lit > 0.75 else s[1] if lit > 0.15 else s[2] if lit > -0.6 else s[3]
                if 13.1 < d < 13.9 and lit < 0.6:
                    c = s[2] if c in (s[0], s[1]) else s[3]
                img.set(x, y, c)
            else:
                k = (dy + d * 0.3) / 12.4
                img.set(x, y, IN1 if k < -0.25 else IN2 if k < 0.45 else IN3)
    img.set(8, 6, W); img.set(7, 7, W); img.set(9, 5, rim[0])


# ------------------------------------------------------------------ 상징
def snowflake(img, cx=16, cy=16, r=8.5, pal=ICE):
    for k in range(6):
        a = math.radians(90 + 60 * k)
        ex, ey = cx + math.cos(a) * r, cy - math.sin(a) * r
        line(img, cx, cy, ex, ey, pal[2], 1.8)
        line(img, cx, cy, ex, ey, pal[1], 0.8)
        for f in (0.55, 0.8):
            bx, by = cx + math.cos(a) * r * f, cy - math.sin(a) * r * f
            for s in (-1, 1):
                b2 = a + s * math.radians(45)
                line(img, bx, by, bx + math.cos(b2) * r * 0.28, by - math.sin(b2) * r * 0.28, pal[1], 1.0)
        disc(img, ex, ey, 1.0, pal[0])
    disc(img, cx, cy, 2.2, pal[0])
    disc(img, cx - 0.5, cy - 0.5, 0.9, W)


def shatter(img):
    # 가운데 얼음 결정이 갈라지고 조각이 튐
    pts = [(16, 6), (22, 14), (18, 25), (11, 18)]
    for y in range(32):
        for x in range(32):
            px, py = x + 0.5, y + 0.5
            inside = True
            for i in range(4):
                ax, ay = pts[i]; bx, by = pts[(i + 1) % 4]
                if (bx - ax) * (py - ay) - (by - ay) * (px - ax) < 0:
                    inside = False; break
            if inside:
                c = ICE[0] if px + py < 30 else ICE[1] if px - py < 2 else ICE[2]
                img.set(x, y, c)
    # 금 (짙은 선) · 벌어진 틈
    line(img, 16, 7, 15, 13, ICE[3], 1.0)
    line(img, 15, 13, 19, 17, ICE[3], 1.0)
    line(img, 19, 17, 16, 23, ICE[3], 1.0)
    line(img, 15, 13, 12, 17, ICE[3], 1.0)
    # 튀는 조각
    for (x, y, s) in ((7, 9, 1.4), (24, 8, 1.2), (25, 21, 1.5), (8, 23, 1.1), (21, 26, 0.9)):
        disc(img, x, y, s, ICE[1])
        img.set(int(x - 0.5), int(y - 0.5), W)
    img.set(17, 9, W); img.set(18, 10, W)


def wisp(img, x, y, r, pal, ang):
    # 꼬리 달린 영혼 (ang = 꼬리 방향)
    for i in range(5):
        t = (i + 1) / 5.0
        disc(img, x + math.cos(ang) * r * 1.6 * t, y + math.sin(ang) * r * 1.6 * t, r * (1 - t * 0.75), pal[2] if i > 1 else pal[1])
    disc(img, x, y, r, pal[1])
    disc(img, x - r * 0.3, y - r * 0.3, r * 0.55, pal[0])
    img.set(int(x - r * 0.4), int(y - r * 0.4), W)


def legion(img):
    # 가운데 큰 영혼을 세 영혼이 돌며 돌진
    ring(img, 16, 16, 8.0, 0.8, SOUL[3], gaps=3, rot=0.6)
    wisp(img, 16, 16, 3.0, SOUL, math.radians(90))
    wisp(img, 9, 10, 2.0, SOUL, math.radians(200))
    wisp(img, 24, 12, 2.0, SOUL, math.radians(-30))
    wisp(img, 14, 25, 2.0, SOUL, math.radians(120))


def ghost_body(img, cx, cy, pal, alpha=255, eyes=True):
    # 동그란 머리 + 물결 치는 아랫자락
    for y in range(32):
        for x in range(32):
            px, py = x + 0.5 - cx, y + 0.5 - cy
            head = px * px + (py + 2) ** 2 <= 36
            skirt = abs(px) <= 6 and -2 <= py <= 5 + math.sin((px + 6) * 1.4) * 1.2
            if head or skirt:
                c = pal[0] if px + py < -6 else pal[1] if px < 2 else pal[2]
                img.set(x, y, (c[0], c[1], c[2], alpha))
    if eyes:
        for ex in (-2.4, 2.0):
            disc(img, cx + ex, cy - 2.6, 1.1, K)
        disc(img, cx - 0.2, cy + 1.2, 0.9, K)


def necro(img):
    # 보라 망령이 적의 몸 안에서 터지려는 모습: 망령 + 둘레 터짐 줄기
    for k in range(8):
        a = math.radians(22.5 + 45 * k)
        line(img, 16 + math.cos(a) * 9, 16 + math.sin(a) * 9, 16 + math.cos(a) * 12, 16 + math.sin(a) * 12, VIO[2], 1.2)
    ring(img, 16, 16, 9.0, 0.9, VIO[3])
    ghost_body(img, 16, 16, VIO)


def phase(img):
    # 반투명 유령이 옆으로 빠져나가는 잔상 + 움직임 줄
    ghost_body(img, 12, 17, SOUL, alpha=110, eyes=False)
    ghost_body(img, 18, 15, ICE)
    for y, l in ((11, 5), (15, 7), (19, 5)):
        line(img, 3, y, 3 + l, y, SOUL[1], 0.9)


SYMBOLS = [lambda i: snowflake(i), shatter, legion, necro, phase]


def cross_frostlegion(img):
    snowflake(img, 13, 14, 6.0)
    wisp(img, 21, 20, 2.6, SOUL, math.radians(200))
    wisp(img, 22, 10, 1.8, SOUL, math.radians(160))


def cross_innerfreeze(img):
    ghost_body(img, 16, 16, VIO)
    line(img, 10, 9, 13, 14, ICE[0], 1.0)
    line(img, 22, 9, 19, 14, ICE[0], 1.0)
    line(img, 16, 23, 16, 27, ICE[0], 1.0)
    for (x, y) in ((8, 8), (24, 8), (16, 28), (6, 18), (26, 18)):
        disc(img, x, y, 1.2, ICE[1])


# ------------------------------------------------------------------ 단계 표시 (보석 · 금테)
def pips(img, n):
    xs = [16] if n == 1 else [13, 19]
    for x in xs:
        disc(img, x, 27.5, 2.1, K)
        disc(img, x, 27.5, 1.5, GOLD[1])
        img.set(x - 1, 26, GOLD[0])


def star(img, cx, cy, r):
    pts = []
    for k in range(10):
        a = math.radians(-90 + 36 * k)
        rr = r if k % 2 == 0 else r * 0.45
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
    for y in range(32):
        for x in range(32):
            px, py = x + 0.5, y + 0.5
            inside = False
            j = len(pts) - 1
            for i in range(len(pts)):
                xi, yi = pts[i]; xj, yj = pts[j]
                if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi + 1e-9) + xi:
                    inside = not inside
                j = i
            if inside:
                img.set(x, y, GOLD[0] if py < cy else GOLD[1])


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


def compose(rim, draw, rim2=None, tier=None):
    img = Img(32, 32)
    medallion(img, GOLD if tier == 'm' else rim, None if tier == 'm' else rim2)
    sym = Img(32, 32)
    draw(sym)
    for y in range(32):
        for x in range(32):
            if math.hypot(x + 0.5 - CX, y + 0.5 - CY) >= 12.4: sym.px[y][x] = (0, 0, 0, 0)
    symbol_outline(img, sym)
    img.paste(sym, 0, 0)
    if tier == 'r1': pips(img, 1)
    elif tier == 'r2': pips(img, 2)
    elif tier == 'm':
        star(img, 25.5, 6.5, 4.2)
    outline(img)
    return img


def save(img, name, tpl):
    path = os.path.join(OUT, name + '.png')
    meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
    if not os.path.exists(path + '.meta'):           # 메타를 먼저 (편집기가 기본 설정 메타를 만들지 않게) · 이미 있으면 guid 유지
        retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta))
    retry(lambda: write(img, path))
    print('그림 저장:', name)


def main():
    os.makedirs(OUT, exist_ok=True)
    folder_meta = OUT + '.meta'
    if not os.path.exists(folder_meta):
        open(folder_meta, 'w', encoding='utf-8', newline='\n').write(
            'fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex)
    tpl = open(META_SRC, encoding='utf-8').read()
    for p in range(5):
        rim = shades(PATH_COLORS[p])
        save(compose(rim, SYMBOLS[p]), 'ghost_%d' % p, tpl)
        for tier in ('r1', 'r2', 'm'):
            save(compose(rim, SYMBOLS[p], tier=tier), 'ghost_%d_%s' % (p, tier), tpl)
    save(compose(shades(PATH_COLORS[0]), cross_frostlegion, shades(PATH_COLORS[2])), 'ghost_x2', tpl)
    save(compose(shades(PATH_COLORS[3]), cross_innerfreeze, shades(PATH_COLORS[1])), 'ghost_x3', tpl)


if __name__ == '__main__':
    main()
