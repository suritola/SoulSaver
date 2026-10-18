# 지도 제작자 갈래 문장 (2026-10-19 빌드 갈래 · Paths/Paths.Cartographer.cs): 32x32
#   갈래 색(PathDef.color) 메달 테 + 짙은 남색 종이 안쪽 + 갈래 상징 · 남보라 외곽선 (초월 카드 그림과 같은 틀)
#   cartographer_<갈래>.png        입문 카드 · HUD 갈래 칸 (0 정밀 측량 · 1 일필 · 2 먹물 · 3 접지술 · 4 행군 측량)
#   cartographer_<갈래>_r1 · _r2   공명 I · II 알림 (아래에 갈래 색 보석 하나 · 둘)
#   cartographer_<갈래>_m          극의 알림 (금테 + 별)
#   cartographer_x2 · _x3          새 교차 (잉크 행군 · 대형 도면) — 두 갈래 상징을 반씩
#   → Assets/Resources/Icons/Paths/ (.meta 는 ability_900.png.meta 를 복사해 guid 만 새로)
# 실행: python tools/pixelart/make_paths_cartographer.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line
from make_menu_icons import OUT as ICONS, retry

OUT = os.path.join(ICONS, 'Paths')
K = (26, 17, 44, 255)                   # 남보라 외곽선
W = (255, 255, 255, 255)
GL = (255, 236, 160, 255); G = (255, 200, 86, 255); GM = (234, 170, 58, 255); GD = (160, 98, 34, 255)
PAPER1 = (52, 58, 104, 255); PAPER2 = (38, 42, 82, 255); PAPER3 = (26, 28, 60, 255)
INKL = (150, 180, 255, 255); INK = (70, 100, 230, 255); INKD = (34, 50, 150, 255)
CX = CY = 16.0

# 갈래 색 (PathDef.color 와 같은 색 · 밝음 / 보통 / 어두움)
PATH = [
    ((175, 205, 255), (115, 166, 255), (60, 96, 190)),      # 정밀 측량
    ((240, 240, 252), (217, 217, 242), (140, 140, 176)),    # 일필
    ((140, 150, 235), (90, 102, 204), (48, 56, 140)),       # 먹물
    ((255, 238, 170), (255, 217, 115), (190, 140, 50)),     # 접지술
    ((185, 255, 200), (128, 242, 153), (52, 160, 90)),      # 행군 측량
]


def rgba(c): return (c[0], c[1], c[2], 255)


def medallion(img, tone, gold=False):
    lt, md, dk = (GL, G, GD) if gold else (rgba(tone[0]), rgba(tone[1]), rgba(tone[2]))
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - CX, y + 0.5 - CY
            d = math.hypot(dx, dy)
            if d > 15.0: continue
            if d >= 12.4:
                lit = (-dx - dy) / (d * 1.414)
                c = lt if lit > 0.6 else md if lit > -0.4 else dk
                if 13.2 < d < 13.8 and lit < 0.5: c = dk
                img.set(x, y, c)
            else:
                k = (dy + d * 0.3) / 12.4
                img.set(x, y, PAPER1 if k < -0.25 else PAPER2 if k < 0.45 else PAPER3)
    # 종이 격자 (지도 느낌 · 아주 옅게)
    for i in (8, 16, 24):
        for t in range(32):
            for (x, y) in ((i, t), (t, i)):
                if math.hypot(x + 0.5 - CX, y + 0.5 - CY) < 12.0 and img.px[y][x] in (PAPER1, PAPER2, PAPER3):
                    r, g, b, a = img.px[y][x]
                    img.set(x, y, (r + 10, g + 12, b + 18, 255))
    img.set(8, 6, W); img.set(7, 7, W)


# ------------------------------------------------------------------ 갈래 상징
def survey(img, tone):
    # 사각형 측량선 + 네 꼭짓점 측량 핀 + 가운데 십자 조준
    c = rgba(tone[1]); l = rgba(tone[0])
    pts = [(9, 9), (23, 9), (23, 23), (9, 23)]
    for i in range(4):
        a, b = pts[i], pts[(i + 1) % 4]
        line(img, a[0], a[1], b[0], b[1], c, 1.6)
    for (x, y) in pts:
        disc(img, x, y, 1.9, G); disc(img, x - 0.5, y - 0.5, 0.8, GL)
    line(img, 16, 12, 16, 20, l, 1.0); line(img, 12, 16, 20, 16, l, 1.0)
    disc(img, 16, 16, 1.0, W)


def stroke(img, tone):
    # 굵은 붓 한 획 (왼쪽 아래 → 오른쪽 위, 끝이 가늘어짐) + 깃펜 촉
    for i in range(23):
        t = i / 22
        x, y = 7 + 17 * t, 25 - 17 * t
        w = 6.8 * (1 - t) + 2.0 * t
        disc(img, x, y, w / 2, INKD)
        disc(img, x - 0.6, y - 0.6, w / 2 - 0.8, INK)
    line(img, 9, 22, 21, 10, INKL, 0.8)
    # 깃펜 (오른쪽 위)
    line(img, 22, 10, 26, 5, rgba(tone[0]), 2.2)
    line(img, 23, 8, 27, 5, rgba(tone[1]), 1.0)
    img.set(22, 10, W)


def ink(img, tone):
    # 잉크 방울 + 아래 웅덩이
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - 22, y + 0.5 - 23
            if (dx / 7.5) ** 2 + (dy / 3.2) ** 2 <= 1: img.set(x, y, INKD)
            if (dx / 6.0) ** 2 + ((dy + 0.6) / 2.2) ** 2 <= 1: img.set(x, y, INK)
    # 물방울: 위는 뾰족 · 아래는 둥글게 (가운데 12, 둥근 부분 중심 y 16.5 · 반지름 5)
    for y in range(5, 22):
        for x in range(32):
            px, py = x + 0.5, y + 0.5
            if py >= 16.5:
                inside = math.hypot(px - 12, py - 16.5) <= 5.0
            else:
                inside = abs(px - 12) <= 5.0 * (py - 5) / 11.5
            if inside: img.set(x, y, INK if px < 13.2 else INKD)
    disc(img, 10.2, 16.5, 1.5, INKL); img.set(9, 15, W)
    disc(img, 24, 13, 1.2, rgba(tone[1])); disc(img, 27, 18, 0.9, rgba(tone[1]))


def fold(img, tone):
    # 접힌 지도 세 면 (지그재그) + 금빛 접힌 선
    c0, c1, c2 = rgba(tone[0]), rgba(tone[1]), rgba(tone[2])
    panels = [((7, 10), (13, 8), (13, 24), (7, 26), c0), ((13, 8), (19, 10), (19, 26), (13, 24), c2), ((19, 10), (25, 8), (25, 24), (19, 26), c1)]
    for (a, b, cc, d, col) in panels:
        for y in range(32):
            for x in range(a[0], b[0] + 1):
                t = (x - a[0]) / max(1, b[0] - a[0])
                top = a[1] + (b[1] - a[1]) * t
                bot = d[1] + (cc[1] - d[1]) * t
                if top <= y <= bot: img.set(x, y, col)
    line(img, 13, 8, 13, 24, GL, 1.0); line(img, 19, 10, 19, 26, GD, 1.0)
    # 지도 위 점선 길
    for (x, y) in ((9, 20), (11, 17), (15, 15), (17, 13), (21, 14), (23, 12)): img.set(x, y, INKD)
    disc(img, 23, 12, 1.1, (220, 70, 70, 255))


def march(img, tone):
    # 발자국 고리 (점선 원) + 발자국 둘
    c = rgba(tone[1]); l = rgba(tone[0])
    ring(img, 16, 16, 8.5, 1.5, c, gaps=10, rot=0.3)
    for (x, y, a) in ((12, 20, -0.4), (19, 12, 0.6)):
        for (ox, oy, r) in ((0, 0, 1.9), (0, -3.2, 1.3)):
            px = x + ox * math.cos(a) - oy * math.sin(a)
            py = y + ox * math.sin(a) + oy * math.cos(a)
            disc(img, px, py, r, l)
    disc(img, 24.5, 16, 1.6, G); img.set(24, 15, GL)


SYM = [survey, stroke, ink, fold, march]


def pips(img, n, tone):
    # 공명 단계: 아래 테 위에 갈래 색 보석 n 개
    xs = [16] if n == 1 else [13, 19]
    for x in xs:
        disc(img, x, 27.5, 2.3, K)
        disc(img, x, 27.5, 1.6, rgba(tone[1]))
        img.set(int(x) - 1, 26, W)


def star(img):
    # 극의: 오른쪽 위 금별 (짙은 테를 둘러 금테 위에서도 또렷하게)
    cx, cy = 24.5, 7.5
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        r = 5.2 if i % 2 == 0 else 2.3
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    s = Img(32, 32)
    for y in range(32):
        for x in range(32):
            px, py = x + 0.5, y + 0.5
            inside = False
            for i in range(10):
                (ax, ay), (bx, by) = pts[i], pts[(i + 1) % 10]
                if (ay > py) != (by > py) and px < (bx - ax) * (py - ay) / (by - ay + 1e-6) + ax: inside = not inside
            if inside: s.set(x, y, GL if px + py < cx + cy - 1 else G)
    for y in range(32):
        for x in range(32):
            if s.px[y][x][3] > 0: continue
            if any(s.get(x + dx, y + dy)[3] > 0 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))): img.set(x, y, K)
    img.paste(s, 0, 0)
    img.set(int(cx) - 1, int(cy) - 1, W)


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


def symbol_layer(draw_list):
    sym = Img(32, 32)
    for d in draw_list: d(sym)
    for y in range(32):
        for x in range(32):
            if math.hypot(x + 0.5 - CX, y + 0.5 - CY) >= 12.4: sym.px[y][x] = (0, 0, 0, 0)
    return sym


def compose(tone, draws, gold=False, extra=None):
    img = Img(32, 32)
    medallion(img, tone, gold)
    sym = symbol_layer(draws)
    # 상징 둘레 1칸 짙은 테
    for y in range(32):
        for x in range(32):
            if sym.px[y][x][3] > 0: continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                c = sym.get(x + dx, y + dy)
                if c[3] > 160 and math.hypot(x + 0.5 - CX, y + 0.5 - CY) < 12.4:
                    img.set(x, y, K)
                    break
    img.paste(sym, 0, 0)
    if extra: extra(img)
    outline(img)
    return img


def half(draw, tone, left):
    # 교차: 상징을 대각선 반쪽만
    def d(img):
        tmp = Img(32, 32)
        draw(tmp, tone)
        for y in range(32):
            for x in range(32):
                if (x + y < 32) == left and tmp.px[y][x][3] > 0: img.set(x, y, tmp.px[y][x])
    return d


def save(img, name, tpl):
    path = os.path.join(OUT, name + '.png')
    meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
    if not os.path.exists(path + '.meta'):           # 메타를 먼저 (편집기가 기본 설정 메타를 만들지 않게) · 있으면 guid 유지
        retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta))
    retry(lambda: write(img, path))
    print('그림 저장:', name)


def main():
    os.makedirs(OUT, exist_ok=True)
    tpl = open(os.path.join(ICONS, 'ability_900.png.meta'), encoding='utf-8').read()
    for p, (draw, tone) in enumerate(zip(SYM, PATH)):
        dr = lambda img, draw=draw, tone=tone: draw(img, tone)
        save(compose(tone, [dr]), 'cartographer_%d' % p, tpl)
        save(compose(tone, [dr], extra=lambda img, tone=tone: pips(img, 1, tone)), 'cartographer_%d_r1' % p, tpl)
        save(compose(tone, [dr], extra=lambda img, tone=tone: pips(img, 2, tone)), 'cartographer_%d_r2' % p, tpl)
        save(compose(tone, [dr], gold=True, extra=star), 'cartographer_%d_m' % p, tpl)
    # 새 교차: 2 잉크 행군 (행군 측량 × 먹물) · 3 대형 도면 (정밀 측량 × 일필)
    mix = lambda a, b: tuple(tuple((a[i][k] + b[i][k]) // 2 for k in range(3)) for i in range(3))
    save(compose(mix(PATH[4], PATH[2]), [half(march, PATH[4], True), half(ink, PATH[2], False)]), 'cartographer_x2', tpl)
    save(compose(mix(PATH[0], PATH[1]), [half(survey, PATH[0], True), half(stroke, PATH[1], False)]), 'cartographer_x3', tpl)


if __name__ == '__main__':
    main()
