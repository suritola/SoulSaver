# 장의사 갈래 그림 (2026-11-02 · 빌드 갈래): 32x32 도트 메달 · Assets/Resources/Icons/Paths/
#   undertaker_<갈래 0~4>          입문 카드 · HUD 입문 칸 (= 갈래 문장)
#   undertaker_<갈래>_r1 · _r2 · _m 공명 I · II · 극의 알림 (아래 금색 점 1 · 2 / 금테 + 별)
#   undertaker_x2 · undertaker_x3   새 교차 (합동 장례 · 저승길) — 두 갈래 색 반반 테
#   (장의사는 31번 새 카드가 없어 card31 그림 없음)
# 갈래: 0 생매장 · 1 묘역 설계 · 2 땅굴 · 3 장례식 · 4 도굴꾼 (색 = Paths.Undertaker.cs 의 PathDef.color)
# 메달 · 별 · 금색 점 · 저장(+ .meta, 있으면 guid 유지)은 make_paths_postman.py 의 것을 그대로 씀 — 틀을 통일
# 실행: py tools/pixelart/make_paths_undertaker.py
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from png import Img
from make_paths_postman import (K, GOLD, GOLD_D, GOLD_L, STEEL, STEEL_D, OUT, rgb, mix, disc, ring, line,
                                 outline, medal, pips, star, inside, layer, stamp, compose, save)
from make_paths_wrestler import poly, ellipse, arrow

# 갈래 색 (Paths.Undertaker.cs 의 PathDef.color 와 같게)
COLORS = [(0.851, 0.702, 0.549), (0.604, 0.69, 0.784), (0.549, 0.478, 0.4), (0.784, 0.784, 0.847), (0.878, 0.753, 0.439)]

SOIL = (150, 110, 70, 255); SOIL_D = (96, 68, 42, 255); SOIL_L = (200, 160, 112, 255)       # 흙
HOLE = (40, 28, 22, 255); HOLE_L = (70, 50, 38, 255)                                          # 구멍
STONE = (170, 182, 200, 255); STONE_D = (110, 122, 144, 255); STONE_L = (222, 230, 242, 255)  # 묘비 (영웅색 #B9CBE4 계열)
SILV = (185, 203, 228, 255)
FOE = (120, 190, 100, 255); FOE_D = (74, 130, 66, 255)
WHITE = (250, 246, 234, 255)


def foe(p, cx, cy, r=2.2):
    disc(p, cx, cy, r, FOE)
    p.set(int(cx) - 1, int(cy) - 1, K); p.set(int(cx) + 1, int(cy) - 1, K)
    p.set(int(cx), int(cy) + 1, FOE_D)


def hole(p, cx, cy, rx, ry):
    ellipse(p, cx, cy, rx + 1, ry + 1, SOIL_D)
    ellipse(p, cx, cy, rx, ry, HOLE)
    ellipse(p, cx, cy + 1, rx - 2, max(1, ry - 2), HOLE_L)


def tomb(p, cx, by, w=4, h=9):
    # 묘비 하나: 윗부분이 둥근 돌 (cx = 가운데, by = 밑)
    ellipse(p, cx, by - h + w, w, w, STONE_D)
    ellipse(p, cx, by - h + w, w - 1, w - 1, STONE)
    for y in range(by - h + w, by):
        for x in range(cx - w, cx + w):
            p.set(x, y, STONE_D if x == cx - w or x == cx + w - 1 else STONE)
    line(p, cx - 1, by - h + w - 1, cx - 1, by - h + w + 2, STONE_L)       # 십자 새김
    line(p, cx - 2, by - h + w, cx, by - h + w, STONE_L)
    for x in range(cx - w - 1, cx + w + 1):
        p.set(x, by, SOIL_D)


def shovel(p, x0, y0, x1, y1):
    line(p, x0, y0, x1, y1, SOIL_D)
    ang = math.atan2(y1 - y0, x1 - x0)
    bx, by = x1 + math.cos(ang) * 2, y1 + math.sin(ang) * 2
    disc(p, bx, by, 2.2, STEEL_D); disc(p, bx, by, 1.5, STEEL)


def bell(p, cx, cy, s=1.0):
    # 종: 위 고리 + 사다리꼴 몸 + 추
    poly(p, [(cx - 2 * s, cy - 5 * s), (cx + 2 * s, cy - 5 * s), (cx + 6 * s, cy + 4 * s), (cx - 6 * s, cy + 4 * s)], SILV)
    line(p, cx - 6 * s, cy + 4 * s, cx + 6 * s, cy + 4 * s, STONE_D)
    line(p, cx - 1 * s, cy - 4 * s, cx - 4 * s, cy + 3 * s, STONE_L)
    disc(p, cx, cy - 6 * s, 1.5 * s, GOLD)
    disc(p, cx, cy + 6 * s, 1.4 * s, GOLD_D)


def glyph(path):
    img = layer()
    if path == 0:            # 생매장: 구멍에 빠진 잡몹 + 쏟아지는 흙 + 삽
        h = layer(); hole(h, 16, 21, 10, 4); stamp(img, h)
        f = layer(); foe(f, 16, 17, 3.2); stamp(img, f)
        h2 = layer()                                       # 구멍 앞 가장자리 (하반신을 가림)
        ellipse(h2, 16, 22, 10, 3, HOLE); stamp(img, h2)
        d = layer()
        for (x, y) in ((11, 6), (16, 4), (21, 7), (14, 9), (19, 10)):
            d.set(x, y, SOIL_L); d.set(x, y + 1, SOIL)
        stamp(img, d)
        s = layer(); shovel(s, 27, 6, 22, 14); stamp(img, s)
    elif path == 1:          # 묘역 설계: 묘비 셋 + 이어진 흙담
        w = layer()
        line(w, 7, 22, 25, 22, SOIL_L); line(w, 7, 23, 25, 23, SOIL); line(w, 7, 24, 25, 24, SOIL_D)
        stamp(img, w)
        for cx in (8, 16, 24):
            t = layer(); tomb(t, cx, 25, 3, 10); stamp(img, t)
        s = layer()
        for (x, y) in ((16, 6), (15, 7), (17, 7), (16, 8)):
            s.set(x, y, GOLD_L)
        stamp(img, s)
    elif path == 2:          # 땅굴: 두 구멍 + 땅속 점선 화살
        a = layer(); hole(a, 7, 24, 5, 2.5); hole(a, 25, 24, 5, 2.5); stamp(img, a)
        t = layer()
        for x in range(8, 24, 2):
            y = 24 + int(round(5 * math.sin((x - 8) / 16.0 * math.pi)))
            t.set(x, y, SOIL_L); t.set(x, y + 1, SOIL)
        arrow(t, 22, 28, 25, 24, GOLD, GOLD_L)
        stamp(img, t)
        f = layer(); foe(f, 25, 18, 2.4); stamp(img, f)
        d = layer()
        for (x, y) in ((22, 14), (28, 13), (24, 11), (27, 17)):
            d.set(x, y, SOIL_L)
        stamp(img, d)
    elif path == 3:          # 장례식: 종 + 퍼져 나가는 파동
        r = layer()
        for rr, c in ((11, STONE_D), (13, STONE)):
            for a in range(200, 341, 6):
                ra = math.radians(a)
                r.set(int(round(16 + rr * math.cos(ra))), int(round(14 + rr * math.sin(ra) * 0.7)), c)
        stamp(img, r)
        b = layer(); bell(b, 16, 15, 1.3); stamp(img, b)
        g = layer(); hole(g, 16, 28, 8, 2); stamp(img, g)
    elif path == 4:          # 도굴꾼: 금이 간 묘비 + 삽 + 튀는 파편 · 금화
        t = layer(); tomb(t, 13, 27, 5, 13); stamp(img, t)
        c = layer()
        line(c, 13, 14, 12, 18, K); line(c, 12, 18, 14, 21, K); line(c, 14, 21, 13, 25, K)
        stamp(img, c)
        s = layer(); shovel(s, 27, 8, 20, 21); stamp(img, s)
        p = layer()
        for (x, y) in ((22, 25), (25, 22), (19, 27)):
            p.set(x, y, STONE_L); p.set(x + 1, y, STONE)
        disc(p, 24, 9, 2.0, GOLD); disc(p, 24, 9, 1.0, GOLD_L)
        stamp(img, p)
    return img


def cross_glyph(k):
    img = layer()
    if k == 2:               # 합동 장례: 한 구멍에 여럿이 빠지고 종이 울림
        h = layer(); hole(h, 16, 24, 11, 4); stamp(img, h)
        for (x, y) in ((11, 20), (16, 19), (21, 20)):
            f = layer(); foe(f, x, y, 2.4); stamp(img, f)
        h2 = layer(); ellipse(h2, 16, 25, 11, 3, HOLE); stamp(img, h2)
        b = layer(); bell(b, 16, 8, 0.8); stamp(img, b)
    elif k == 3:             # 저승길: 종 + 땅속으로 이어지는 화살
        b = layer(); bell(b, 9, 10, 0.8); stamp(img, b)
        a = layer(); hole(a, 8, 26, 4.5, 2); hole(a, 25, 26, 4.5, 2); stamp(img, a)
        t = layer()
        for x in range(9, 24, 2):
            y = 26 + int(round(4 * math.sin((x - 9) / 15.0 * math.pi)))
            t.set(x, y, SOIL_L); t.set(x, y + 1, SOIL)
        arrow(t, 21, 29, 25, 25, GOLD, GOLD_L)
        stamp(img, t)
        s = layer()
        for (x, y) in ((24, 14), (25, 15), (23, 15), (24, 16)):
            s.set(x, y, GOLD_L)
        stamp(img, s)
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    for p in range(5):
        col = COLORS[p]
        save(compose(medal(col), glyph(p)), 'undertaker_%d' % p)
        r1 = compose(medal(col), glyph(p)); pips(r1, 1); save(r1, 'undertaker_%d_r1' % p)
        r2 = compose(medal(col), glyph(p)); pips(r2, 2); save(r2, 'undertaker_%d_r2' % p)
        m = compose(medal(col, gold=True), glyph(p)); star(m, 16, 4.5); save(m, 'undertaker_%d_m' % p)
    save(compose(medal(COLORS[0], COLORS[3]), cross_glyph(2)), 'undertaker_x2')       # 생매장 × 장례식
    save(compose(medal(COLORS[2], COLORS[3]), cross_glyph(3)), 'undertaker_x3')       # 땅굴 × 장례식
    print('장의사 갈래 그림 저장:', OUT)


if __name__ == '__main__':
    main()
