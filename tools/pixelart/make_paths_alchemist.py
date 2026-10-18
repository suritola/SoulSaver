# 연금술사 갈래 아이콘 (2026-10-19 · 빌드 갈래): 32x32 도트
#   갈래 색 메달(위 왼쪽이 밝은 3단 테 + 짙은 안쪽) 안에 갈래 상징, 남보라 외곽선 — make_transcend_icons.py 와 같은 그림체
#   → Assets/Resources/Icons/Paths/alchemist_<갈래 0~4>.png          입문 카드 · HUD 갈래 칸
#                                   alchemist_<갈래>_r1 · _r2 · _m   공명 I · II (아래 보석 하나 · 둘) · 극의 (금테 + 별)
#                                   alchemist_x2 · _x3               새 교차 (얼음 늪 · 폭탄 조수: 두 갈래 색을 반씩 나눈 테)
#                                   alchemist_card31                 31번 새 카드 「서리 장막」
#   갈래 색 = Paths/Paths.Alchemist.cs 의 PathDef.color
# 실행: python tools/pixelart/make_paths_alchemist.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line, ROOT
from make_menu_icons import retry

OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'Paths')
META = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'ability_900.png.meta')

K = (26, 17, 44, 255)                   # 남보라 외곽선
W = (255, 255, 255, 255)
GL = (255, 236, 160, 255); G = (255, 200, 86, 255); GM = (234, 170, 58, 255); GD = (160, 98, 34, 255)
CX = CY = 16.0

# 갈래 색 (PathDef.color × 255)
PATH_COLORS = [(153, 242, 77), (140, 217, 255), (255, 140, 64), (230, 153, 255), (255, 217, 89)]
FIRE = ((255, 220, 140, 255), (255, 150, 60, 255), (200, 70, 40, 255))
ICE = ((220, 246, 255, 255), (140, 217, 255, 255), (60, 130, 210, 255))
ACID = ((215, 255, 180, 255), (120, 230, 90, 255), (50, 140, 60, 255))
PURP = ((235, 205, 255, 255), (190, 120, 250, 255), (110, 60, 180, 255))
GLASS = ((235, 245, 255, 255), (180, 200, 235, 255), (110, 120, 170, 255))


def shades(rgb):
    r, g, b = rgb
    lite = (min(255, r + (255 - r) // 2), min(255, g + (255 - g) // 2), min(255, b + (255 - b) // 2), 255)
    mid = (r, g, b, 255)
    dark = (r * 3 // 5, g * 3 // 5, b * 3 // 5, 255)
    deep = (r * 2 // 5, g * 2 // 5, b * 2 // 5, 255)
    return lite, mid, dark, deep


def medallion(img, rgb, rgb2=None, gold=False):
    # 테: 반지름 12.4~15 (rgb2 가 있으면 오른쪽 절반은 그 색 — 교차), 위 왼쪽이 밝게
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - CX, y + 0.5 - CY
            d = math.hypot(dx, dy)
            if d > 15.0:
                continue
            if d >= 12.4:
                if gold:
                    L, M, D, DD = GL, G, GM, GD
                else:
                    L, M, D, DD = shades(rgb2 if rgb2 and dx > 0 else rgb)
                lit = (-dx - dy) / (d * 1.414)
                c = L if lit > 0.75 else M if lit > 0.15 else D if lit > -0.6 else DD
                if 13.1 < d < 13.9 and lit < 0.6:
                    c = D if c in (M, L) else DD
                img.set(x, y, c)
            else:
                k = (dy + d * 0.3) / 12.4
                img.set(x, y, (52, 40, 82, 255) if k < -0.25 else (38, 28, 66, 255) if k < 0.45 else (26, 18, 46, 255))
    img.set(8, 6, W); img.set(7, 7, W)


def flask(img, cx, cy, s, cols, liquid=True):
    # 둥근 플라스크 (몸통 + 목 + 마개), s = 몸통 반지름
    lite, mid, dark = cols
    disc(img, cx, cy, s, GLASS[1])
    if liquid:
        for y in range(int(cy - s), int(cy + s + 1)):
            for x in range(int(cx - s - 1), int(cx + s + 2)):
                if (x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2 <= (s - 0.6) ** 2 and y + 0.5 > cy - s * 0.25:
                    img.set(x, y, mid if y + 0.5 < cy + s * 0.45 else dark)
    neck_w = max(1, int(s * 0.55))
    img.rect(int(cx - neck_w / 2 + 0.5), int(cy - s - 3), neck_w, 3, GLASS[1])
    img.rect(int(cx - neck_w / 2 + 0.5) - 1, int(cy - s - 4), neck_w + 2, 1, (150, 96, 56, 255))
    img.set(int(cx - s * 0.45), int(cy - s * 0.35), W)


def sym_swamp(img):
    # 바닥의 넓은 산성 웅덩이 + 거품 + 위에서 떨어지는 방울
    for y in range(32):
        for x in range(32):
            dx, dy = (x + 0.5 - 16) / 9.5, (y + 0.5 - 21) / 4.2
            d = dx * dx + dy * dy
            if d <= 1:
                img.set(x, y, ACID[0] if d < 0.25 and dy < 0 else ACID[1] if d < 0.7 else ACID[2])
    disc(img, 12, 19.5, 1.4, ACID[0]); img.set(12, 19, W)
    disc(img, 19.5, 21, 1.1, ACID[0])
    ring(img, 21.5, 15.5, 1.6, 0.9, ACID[0])
    # 떨어지는 방울
    disc(img, 15.5, 11, 2.2, ACID[1]); line(img, 15.5, 7, 15.5, 9.5, ACID[1], 1.4); img.set(14, 10, W)


def sym_frost(img):
    # 눈 결정 (여섯 갈래 + 가지) + 가운데 빛
    for k in range(6):
        a = k * math.pi / 3 + math.pi / 2
        x1, y1 = 16 + math.cos(a) * 9.5, 16 + math.sin(a) * 9.5
        line(img, 16, 16, x1, y1, ICE[1], 1.6)
        mx, my = 16 + math.cos(a) * 6, 16 + math.sin(a) * 6
        for s in (-1, 1):
            b = a + s * 0.75
            line(img, mx, my, mx + math.cos(b) * 3, my + math.sin(b) * 3, ICE[0], 1.0)
    disc(img, 16, 16, 2.6, ICE[0])
    disc(img, 15.6, 15.6, 1.1, W)


def sym_chain(img):
    # 보라 불안정 플라스크 + 뒤로 터지는 주황 폭발 살
    for k in range(8):
        a = k * math.pi / 4 + 0.39
        line(img, 16 + math.cos(a) * 5, 17 + math.sin(a) * 5, 16 + math.cos(a) * 11, 17 + math.sin(a) * 11, FIRE[1] if k % 2 else FIRE[0], 1.6)
    flask(img, 16, 18, 5.2, PURP)
    disc(img, 13.5, 19.5, 0.9, PURP[0]); disc(img, 18, 20.5, 0.7, PURP[0])


def homunculus(img, cx, cy, s, cols):
    lite, mid, dark = cols
    disc(img, cx, cy, s, mid)
    disc(img, cx - s * 0.3, cy - s * 0.3, s * 0.45, lite)
    for x in range(int(cx - s), int(cx + s + 1)):
        if (x + 0.5 - cx) ** 2 <= s * s:
            img.set(x, int(cy + s * 0.7), dark)
    img.set(int(cx - s * 0.4), int(cy - 0.2), K); img.set(int(cx + s * 0.35), int(cy - 0.2), K)
    line(img, cx, cy - s, cx + 1.5, cy - s - 2.5, dark, 1.0)
    disc(img, cx + 1.8, cy - s - 2.8, 0.9, lite)


def sym_twins(img):
    # 호문쿨루스 둘 (초록 · 보라) + 사이에 작은 플라스크
    homunculus(img, 11, 18, 4.2, ACID)
    homunculus(img, 21, 17, 4.2, PURP)
    flask(img, 16, 25, 1.8, FIRE, True)


def sym_catalyst(img):
    # 화염 · 빙결 · 산성 세 방울을 순서 화살표로 이은 삼각형 + 가운데 붕괴 별
    pts = [(16, 8.5), (22.3, 19.8), (9.7, 19.8)]
    cols = [FIRE, ICE, ACID]
    for i in range(3):
        a, b = pts[i], pts[(i + 1) % 3]
        line(img, a[0], a[1], b[0], b[1], GM, 1.2)
    for (x, y), c in zip(pts, cols):
        disc(img, x, y, 2.7, c[1]); disc(img, x - 0.7, y - 0.7, 1.1, c[0])
    for k in range(4):
        a = k * math.pi / 2
        line(img, 16, 16.5, 16 + math.cos(a) * 3.2, 16.5 + math.sin(a) * 3.2, GL, 1.2)
    disc(img, 16, 16.5, 1.2, W)


def sym_icebog(img):
    sym_swamp(img)
    # 웅덩이 위 얼음 결정
    for k in range(6):
        a = k * math.pi / 3 + math.pi / 2
        line(img, 16, 13, 16 + math.cos(a) * 5.5, 13 + math.sin(a) * 5.5, ICE[0], 1.3)
    disc(img, 16, 13, 1.6, W)


def sym_bombhelper(img):
    # 보라 호문쿨루스 + 들고 있는 터지는 플라스크
    for k in range(8):
        a = k * math.pi / 4
        line(img, 21 + math.cos(a) * 2.5, 11 + math.sin(a) * 2.5, 21 + math.cos(a) * 6, 11 + math.sin(a) * 6, FIRE[0] if k % 2 else FIRE[1], 1.3)
    flask(img, 21, 12, 2.6, PURP)
    homunculus(img, 13, 19, 5.0, ACID)


def sym_card31(img):
    # 서리 장막: 아래로 늘어진 얼음 휘장 + 가운데 작은 눈 결정
    for x in range(7, 26):
        top = 8 + int(1.5 * math.sin((x - 7) / 18 * math.pi))
        bottom = 22 + int(2.5 * math.sin(x * 1.3))
        for y in range(top, bottom):
            k = (x - 7) % 4
            img.set(x, y, ICE[0] if k == 0 else ICE[1] if k < 3 else ICE[2])
    for x in range(7, 26):
        img.set(x, 8 + int(1.5 * math.sin((x - 7) / 18 * math.pi)), W)
    for k in range(6):
        a = k * math.pi / 3 + math.pi / 2
        line(img, 16, 15.5, 16 + math.cos(a) * 4, 15.5 + math.sin(a) * 4, W, 1.1)
    disc(img, 16, 15.5, 1.3, (30, 70, 140, 255))


def pips(img, n):
    # 공명 단계: 메달 아래 테 위에 보석 n 개
    xs = [16] if n == 1 else [12.5, 19.5]
    for x in xs:
        disc(img, x, 27.5, 2.1, K)
        disc(img, x, 27.5, 1.5, (255, 120, 200, 255))
        img.set(int(x) - 1, 26, W)


def star(img):
    # 극의: 메달 위쪽 가운데 금별 (짙은 테를 먼저 한 칸 크게 깔아 금테 위에서도 또렷하게)
    cx, cy = 16, 5.5
    for lim_mul, cols in ((1.35, (K, K)), (1.0, (GL, G))):
        for y in range(32):
            for x in range(32):
                dx, dy = x + 0.5 - cx, y + 0.5 - cy
                a = math.atan2(dy, dx)
                r = math.hypot(dx, dy)
                lim = 4.2 * lim_mul * (0.5 + 0.5 * abs(math.cos(2.5 * (a + math.pi / 2))))
                if r <= max(lim, 1.6 * lim_mul):
                    img.set(x, y, cols[0] if dy < 0 else cols[1])
    img.set(16, 4, W)


def outline(img):
    src = [[img.px[y][x] for x in range(img.w)] for y in range(img.h)]
    for y in range(img.h):
        for x in range(img.w):
            if src[y][x][3] > 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < img.w and 0 <= yy < img.h and src[yy][xx][3] > 0 and src[yy][xx][:3] != K[:3]:
                    img.px[y][x] = K
                    break


def symbol_outline(img, sym):
    for y in range(32):
        for x in range(32):
            if sym.px[y][x][3] > 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                c = sym.get(x + dx, y + dy)
                if c[3] > 160 and math.hypot(x + 0.5 - CX, y + 0.5 - CY) < 12.4:
                    img.set(x, y, K)
                    break


def build(draw, rgb, rgb2=None, gold=False, tier=0, mastery=False):
    img = Img(32, 32)
    medallion(img, rgb, rgb2, gold)
    sym = Img(32, 32)
    draw(sym)
    for y in range(32):
        for x in range(32):
            if math.hypot(x + 0.5 - CX, y + 0.5 - CY) >= 12.4:
                sym.px[y][x] = (0, 0, 0, 0)
    symbol_outline(img, sym)
    img.paste(sym, 0, 0)
    if tier:
        pips(img, tier)
    if mastery:
        star(img)
    outline(img)
    return img


def save(img, name, tpl):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + '.png')
    meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
    meta = re.sub(r'spriteID: [0-9a-f]{32}', 'spriteID: ' + uuid.uuid4().hex, meta, count=1)
    if not os.path.exists(path + '.meta'):          # 메타를 먼저 (편집기가 기본 설정 메타를 만들지 않게) · 있으면 guid 를 그대로 둠
        retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta))
    retry(lambda: write(img, path))
    print('그림 저장:', name)


def main():
    tpl = open(META, encoding='utf-8').read()
    syms = [sym_swamp, sym_frost, sym_chain, sym_twins, sym_catalyst]
    for i, draw in enumerate(syms):
        rgb = PATH_COLORS[i]
        save(build(draw, rgb), 'alchemist_%d' % i, tpl)
        save(build(draw, rgb, tier=1), 'alchemist_%d_r1' % i, tpl)
        save(build(draw, rgb, tier=2), 'alchemist_%d_r2' % i, tpl)
        save(build(draw, rgb, gold=True, mastery=True), 'alchemist_%d_m' % i, tpl)
    # 새 교차: Crosses 배열 순서 2 얼음 늪 (빙결 × 늪) · 3 폭탄 조수 (조수 × 연쇄 폭발)
    save(build(sym_icebog, PATH_COLORS[1], PATH_COLORS[0]), 'alchemist_x2', tpl)
    save(build(sym_bombhelper, PATH_COLORS[3], PATH_COLORS[2]), 'alchemist_x3', tpl)
    save(build(sym_card31, PATH_COLORS[1]), 'alchemist_card31', tpl)


if __name__ == '__main__':
    main()
