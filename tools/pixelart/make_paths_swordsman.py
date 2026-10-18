# 검사 갈래 아이콘 (2026-10-19 · 빌드 갈래, docs/review/2026-10-19-빌드-시너지-영웅구현안내.md 8절): 32x32
#   갈래 색 3단 테(위 왼쪽이 밝게) 메달 + 짙은 안쪽 + 갈래 상징, 남보라 외곽선 (초월 카드 메달과 같은 틀 · 색만 갈래 색)
#   swordsman_<갈래 0~4>          입문 카드 · HUD 갈래 칸   (0 검기 · 1 받아치기 · 2 피의 맹세 · 3 검무 · 4 회오리)
#   swordsman_<갈래>_r1 · _r2     공명 I · II 알림 (테 아래 금빛 점 1 · 2개)
#   swordsman_<갈래>_m            극의 알림 (금테 + 위 별)
#   swordsman_x2 · _x3            새 교차 (피의 검기 · 칼날 폭풍, 두 갈래 색을 반씩 나눈 테)
#   → Assets/Resources/Icons/Paths/ (+ .meta: ability_900.png.meta 를 복사해 guid 만 새로)
# 실행: python tools/pixelart/make_paths_swordsman.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line, ROOT

ICONS = os.path.join(ROOT, 'Assets', 'Resources', 'Icons')
OUT = os.path.join(ICONS, 'Paths')
K = (26, 17, 44, 255)                   # 남보라 외곽선
W = (255, 255, 255, 255)
GL = (255, 236, 160, 255); G = (255, 200, 86, 255); GM = (234, 170, 58, 255); GD = (160, 98, 34, 255)
CX = CY = 16.0
RIN = 12.4                               # 안쪽 반지름


def rgb(c, a=255):
    return (max(0, min(255, int(c[0]))), max(0, min(255, int(c[1]))), max(0, min(255, int(c[2]))), a)


def tones(c):
    # 갈래 색(0~1) → 밝은 · 가운데 · 짙은 · 아주 짙은 네 단
    r, g, b = c[0] * 255, c[1] * 255, c[2] * 255
    light = rgb((r + (255 - r) * 0.55, g + (255 - g) * 0.55, b + (255 - b) * 0.55))
    mid = rgb((r, g, b))
    dark = rgb((r * 0.62, g * 0.62, b * 0.62))
    deep = rgb((r * 0.38, g * 0.38, b * 0.38))
    return light, mid, dark, deep


# 갈래 색 (Paths.Swordsman.cs 의 PathDef.color 와 같음)
COLORS = [(0.6, 0.8, 1.0), (1.0, 0.9, 0.5), (0.95, 0.25, 0.3), (0.55, 1.0, 0.75), (0.75, 0.6, 1.0)]


def medallion(img, colA, colB=None, gold=False):
    # 테: 반지름 12.4~15, 위 왼쪽이 밝게 · colB 가 있으면 오른쪽 아래 반은 colB (교차)
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - CX, y + 0.5 - CY
            d = math.hypot(dx, dy)
            if d > 15.0:
                continue
            if d >= RIN:
                lit = (-dx - dy) / (d * 1.414)
                if gold:
                    t = (GL, G, GM, GD)
                else:
                    t = tones(colB if colB is not None and dx + dy > 0.5 else colA)
                c = t[0] if lit > 0.75 else t[1] if lit > 0.15 else t[2] if lit > -0.6 else t[3]
                if 13.1 < d < 13.9 and lit < 0.6:
                    c = t[2] if c in (t[0], t[1]) else t[3]
                img.set(x, y, c)
            else:
                # 안쪽: 갈래 색을 아주 짙게 (아래 · 바깥이 더 어둡게)
                base = tones(colA)[3]
                k = (dy + d * 0.3) / RIN
                f = 1.0 if k < -0.25 else 0.78 if k < 0.45 else 0.58
                img.set(x, y, rgb((base[0] * f * 0.9 + 14, base[1] * f * 0.9 + 10, base[2] * f * 0.9 + 24)))
    img.set(8, 6, W); img.set(7, 7, W)


# ------------------------------------------------------------------ 상징 (안쪽 반지름 12.4 안)
def crescent(img, cx, cy, r, thick, rot, light, mid, dark):
    # 초승달 검기: 바깥 원 r · 안쪽 원을 rot 반대쪽으로 thick 만큼 밀어 깎음
    ox, oy = math.cos(rot) * thick, math.sin(rot) * thick
    for y in range(32):
        for x in range(32):
            px, py = x + 0.5, y + 0.5
            d1 = math.hypot(px - cx, py - cy)
            d2 = math.hypot(px - (cx - ox), py - (cy - oy))
            if d1 <= r and d2 > r * 0.92:
                e = (d1 - (r - 2.2))
                img.set(x, y, light if e > 1.0 else mid if e > -0.4 else dark)


def wave(img, light, mid, dark):
    crescent(img, 15, 16, 8.6, 4.2, 0.0, light, mid, dark)
    for i, yy in enumerate((11, 16, 21)):           # 날아가는 결
        line(img, 6 + i % 2 * 1.5, yy, 11 + i % 2, yy, (light[0], light[1], light[2], 210), 1.3)
    disc(img, 22.5, 12, 0.9, W)


def sword(img, x0, y0, x1, y1, blade=(232, 240, 255, 255), edge=(150, 170, 210, 255)):
    line(img, x0, y0, x1, y1, blade, 2.2)
    line(img, x0 + 0.6, y0 + 0.6, x1 + 0.6, y1 + 0.6, edge, 0.8)
    disc(img, x1, y1, 0.9, W)


def parry(img, light, mid, dark):
    # 비스듬한 장검 + 막는 순간의 별빛
    sword(img, 11, 22, 22, 9)
    line(img, 8.5, 19.5, 13.5, 24.5, mid, 1.6)       # 코등이
    line(img, 9.5, 24.5, 7.5, 26.5, (150, 96, 56, 255), 1.6)   # 손잡이
    cx, cy = 15.5, 15.0
    for a in range(4):
        ang = a * math.pi / 2 + math.pi / 4
        line(img, cx, cy, cx + math.cos(ang) * 6.2, cy + math.sin(ang) * 6.2, light, 1.2)
    for a in range(4):
        ang = a * math.pi / 2
        line(img, cx, cy, cx + math.cos(ang) * 3.6, cy + math.sin(ang) * 3.6, W, 1.0)
    disc(img, cx, cy, 1.8, W)


def blood(img, light, mid, dark):
    # 핏방울 + 가로지르는 칼날
    for y in range(6, 26):
        for x in range(32):
            px, py = x + 0.5, y + 0.5
            inside = math.hypot(px - 16, py - 19.5) <= 5.6
            if py < 19.5:
                half = (py - 6.0) / 13.5 * 5.6
                inside = inside or abs(px - 16) <= half
            if inside:
                k = (px - 16) / 6 + (py - 19) / 10
                img.set(x, y, light if k < -0.55 else mid if k < 0.25 else dark)
    disc(img, 14, 19, 1.3, (255, 210, 215, 255))
    sword(img, 7, 24, 25, 8, blade=(236, 236, 246, 230), edge=(170, 150, 170, 230))


def dance(img, light, mid, dark):
    # 기울어진 검 + 뒤로 남는 잔상 둘 + 발자국
    for i, a in enumerate((140, 200)):
        c = (light[0], light[1], light[2], a)
        line(img, 8 + i * 2.5, 22, 17 + i * 2.5, 8, c, 1.8)
    sword(img, 14, 23, 24, 9)
    line(img, 11.5, 21, 16.5, 25.5, mid, 1.5)
    for x, y in ((7, 26), (11, 27.5)):
        disc(img, x, y, 1.1, mid)


def whirl(img, light, mid, dark):
    # 소용돌이 + 가운데 칼끝
    for i in range(170):
        t = i / 170.0
        a = t * math.pi * 3.6
        r = 1.6 + t * 9.0
        x, y = 16 + math.cos(a) * r, 16 + math.sin(a) * r
        c = light if t < 0.3 else mid if t < 0.7 else dark
        disc(img, x, y, 0.8 + t * 0.6, c)
    for k in range(3):
        a = k * math.pi * 2 / 3 + 0.4
        line(img, 16 + math.cos(a) * 4, 16 + math.sin(a) * 4, 16 + math.cos(a + 0.9) * 10, 16 + math.sin(a + 0.9) * 10, W, 0.9)
    disc(img, 16, 16, 1.9, W)


SYMBOLS = [wave, parry, blood, dance, whirl]


def blood_wave(img, light, mid, dark):
    # 피의 검기: 붉은 초승달 + 핏방울 하나
    rl, rm, rd, _ = tones(COLORS[2])
    crescent(img, 15, 16, 8.6, 4.2, 0.0, rl, rm, rd)
    for i, yy in enumerate((11, 16, 21)):
        line(img, 6 + i % 2 * 1.5, yy, 11 + i % 2, yy, (light[0], light[1], light[2], 210), 1.3)
    disc(img, 22, 20.5, 2.0, rm)
    disc(img, 21.5, 20, 0.8, (255, 210, 215, 255))


def blade_storm(img, light, mid, dark):
    # 칼날 폭풍: 소용돌이 + 바깥으로 튀는 칼날 셋
    vl, vm, vd, _ = tones(COLORS[4])
    for i in range(120):
        t = i / 120.0
        a = t * math.pi * 3.0
        r = 1.4 + t * 6.5
        disc(img, 16 + math.cos(a) * r, 16 + math.sin(a) * r, 0.7 + t * 0.5, vl if t < 0.4 else vm)
    dl, dm, dd, _ = tones(COLORS[3])
    for k in range(3):
        a = k * math.pi * 2 / 3 - 0.3
        x0, y0 = 16 + math.cos(a) * 6.5, 16 + math.sin(a) * 6.5
        x1, y1 = 16 + math.cos(a + 0.35) * 11, 16 + math.sin(a + 0.35) * 11
        line(img, x0, y0, x1, y1, dl, 1.6)
        disc(img, x1, y1, 0.8, W)
    disc(img, 16, 16, 1.7, W)


def pips(img, n):
    # 공명 단계: 테 아래 금빛 점
    xs = [16] if n == 1 else [12.5, 19.5]
    for x in xs:
        disc(img, x, 27.6, 1.9, GD)
        disc(img, x, 27.4, 1.4, G)
        img.set(int(x) - 1, 26, GL)


def star(img):
    # 극의: 테 위 금별
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        r = 4.2 if i % 2 == 0 else 1.9
        pts.append((16 + math.cos(a) * r, 4.6 + math.sin(a) * r))
    for y in range(0, 10):
        for x in range(10, 23):
            px, py = x + 0.5, y + 0.5
            inside = False
            j = len(pts) - 1
            for i in range(len(pts)):
                xi, yi = pts[i]; xj, yj = pts[j]
                if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi + 1e-9) + xi:
                    inside = not inside
                j = i
            if inside:
                img.set(x, y, GL if py < 4.5 else G)
    img.set(16, 3, W)


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
                if c[3] > 160 and math.hypot(x + 0.5 - CX, y + 0.5 - CY) < RIN:
                    img.set(x, y, K)
                    break


def build(draw, colA, colB=None, gold=False, tier=0, mastery=False):
    img = Img(32, 32)
    medallion(img, colA, colB, gold)
    sym = Img(32, 32)
    light, mid, dark, _ = tones(colA)
    draw(sym, light, mid, dark)
    for y in range(32):
        for x in range(32):
            if math.hypot(x + 0.5 - CX, y + 0.5 - CY) >= RIN: sym.px[y][x] = (0, 0, 0, 0)
    symbol_outline(img, sym)
    img.paste(sym, 0, 0)
    if tier: pips(img, tier)
    if mastery: star(img)
    outline(img)
    return img


def save(img, name, tpl):
    path = os.path.join(OUT, name + '.png')
    if not os.path.exists(path + '.meta'):           # 메타를 먼저 (편집기가 기본 설정 메타를 만들지 않게) · 이미 있으면 guid 유지
        meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
        with open(path + '.meta', 'w', encoding='utf-8', newline='\n') as f:
            f.write(meta)
    write(img, path)
    print('그림 저장:', name)


def main():
    os.makedirs(OUT, exist_ok=True)
    tpl = open(os.path.join(ICONS, 'ability_900.png.meta'), encoding='utf-8').read()
    for p, draw in enumerate(SYMBOLS):
        col = COLORS[p]
        save(build(draw, col), 'swordsman_%d' % p, tpl)
        save(build(draw, col, tier=1), 'swordsman_%d_r1' % p, tpl)
        save(build(draw, col, tier=2), 'swordsman_%d_r2' % p, tpl)
        save(build(draw, col, gold=True, mastery=True), 'swordsman_%d_m' % p, tpl)
    # 새 교차 (Crosses 배열 번호 2 · 3)
    save(build(blood_wave, COLORS[0], COLORS[2]), 'swordsman_x2', tpl)
    save(build(blade_storm, COLORS[3], COLORS[4]), 'swordsman_x3', tpl)


if __name__ == '__main__':
    main()
