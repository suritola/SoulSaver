# 공사 반장 갈래 그림 (2026-11-02 · 빌드 갈래): 32x32 도트 메달 · Assets/Resources/Icons/Paths/
#   foreman_<갈래 0~4>          입문 카드 · HUD 입문 칸 (= 갈래 문장)
#   foreman_<갈래>_r1 · _r2 · _m 공명 I · II · 극의 알림 (아래 금색 점 1 · 2 / 금테 + 별)
#   foreman_x2 · foreman_x3     새 교차 (콘크리트 벽 · 낙석 주의) — 두 갈래 색 반반 테
#   foreman_card31              31번 새 카드 「작업 지시」
# 갈래: 0 불도저 · 1 산사태 · 2 콘크리트 · 3 철거 · 4 안전 제일 (색 = Paths.Foreman.cs 의 PathDef.color)
# 메달 · 별 · 금색 점 · 저장(+ .meta, 있으면 guid 유지)은 make_paths_postman.py 의 것을 그대로 씀 — 틀을 통일
# 실행: py tools/pixelart/make_paths_foreman.py
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from png import Img
from make_paths_postman import (K, GOLD, GOLD_D, GOLD_L, STEEL, STEEL_D, OUT, rgb, mix, disc, ring, line,
                                 outline, medal, pips, star, inside, layer, stamp, compose, save)
from make_paths_wrestler import poly, ellipse, arrow

# 갈래 색 (Paths.Foreman.cs 의 PathDef.color 와 같게)
COLORS = [(0.95, 0.8, 0.2), (0.78, 0.6, 0.36), (0.68, 0.7, 0.74), (0.92, 0.42, 0.24), (0.42, 0.82, 0.48)]

YEL = (246, 214, 70, 255); YEL_D = (196, 156, 30, 255); YEL_L = (255, 240, 150, 255)
BLK = (40, 40, 48, 255)
FOE = (120, 190, 100, 255); FOE_D = (74, 130, 66, 255)
DIRT = (176, 132, 82, 255); DIRT_D = (120, 86, 52, 255); DIRT_L = (214, 174, 120, 255)
CONC = (176, 180, 188, 255); CONC_D = (118, 122, 132, 255); CONC_L = (222, 226, 232, 255)
BALL = (54, 56, 66, 255); BALL_L = (112, 116, 132, 255)
ORANGE = (240, 120, 50, 255); WHITE = (250, 246, 234, 255)
GREEN = (92, 190, 110, 255); GREEN_D = (52, 130, 70, 255)


def foe(p, cx, cy, r=2.2):
    # 눌린 잡몹 한 마리
    disc(p, cx, cy, r, FOE)
    p.set(int(cx) - 1, int(cy) - 1, K); p.set(int(cx) + 1, int(cy) - 1, K)
    p.set(int(cx), int(cy) + 1, FOE_D)


def stripes(p, x0, y0, x1, y1, a, b):
    # 줄무늬 띠 (사선)
    for y in range(y0, y1):
        for x in range(x0, x1):
            p.set(x, y, a if ((x + y) // 3) % 2 == 0 else b)


def glyph(path):
    img = layer()
    if path == 0:            # 불도저: 노랑 삽날 + 앞에 쌓인 잡몹 더미
        pile = layer()
        for (x, y) in ((19, 13), (22, 17), (19, 20), (23, 11), (24, 22)):
            foe(pile, x, y)
        stamp(img, pile)
        b = layer()
        poly(b, [(8, 9), (14, 11), (14, 24), (8, 26)], YEL_D)
        poly(b, [(9, 10.5), (13, 12), (13, 23), (9, 24.5)], YEL)
        for y in range(12, 22, 2):
            b.set(10, y, BLK); b.set(11, y + 1, BLK)
        b.set(10, 11, YEL_L); b.set(11, 12, YEL_L)
        line(b, 14, 17, 5, 17, BLK); line(b, 14, 18, 5, 18, BLK)           # 자루
        stamp(img, b)
    elif path == 1:          # 산사태: 흙 파도 띠 + 앞으로 가는 화살
        w = layer()
        for x in range(4, 28):
            top = 14 + int(3 * math.sin(x * 0.55))
            for y in range(top, 24):
                w.set(x, y, DIRT_L if y < top + 2 else DIRT if y < top + 5 else DIRT_D)
        for (x, y) in ((9, 20), (14, 22), (19, 19), (23, 21)):
            w.set(x, y, BLK)
        stamp(img, w)
        a = layer(); arrow(a, 5, 9, 26, 9, ORANGE, YEL_L); stamp(img, a)
        d = layer()
        for (x, y) in ((8, 6), (14, 5), (20, 6)):
            d.set(x, y, DIRT_L)
        stamp(img, d)
    elif path == 2:          # 콘크리트: 굳은 회색 덩어리 + 흘러내리는 시멘트
        c = layer()
        poly(c, [(7, 12), (25, 12), (26, 25), (6, 25)], CONC_D)
        poly(c, [(8, 13), (24, 13), (25, 24), (7, 24)], CONC)
        for y in range(14, 17):
            for x in range(9, 18):
                c.set(x, y, CONC_L)
        for (x, y) in ((13, 19), (19, 21), (10, 22), (21, 17)):
            c.set(x, y, CONC_D)
        stamp(img, c)
        d = layer()
        for y in range(8, 13):
            d.set(14, y, CONC); d.set(15, y, CONC_L)
        disc(d, 14.5, 7.5, 1.8, CONC)
        stamp(img, d)
        e = layer(); foe(e, 16, 18, 2.6); stamp(img, e)                  # 굳어 가는 적
        g = layer()
        for (x, y) in ((11, 18), (21, 19), (16, 22)):
            g.set(x, y, CONC_L)
        stamp(img, g)
    elif path == 3:          # 철거: 쇠공 + 사슬 + 바리케이드
        ch = layer()
        for i in range(5):
            ch.set(9 + i, 4 + i * 2, STEEL); ch.set(10 + i, 4 + i * 2, STEEL_D)
        stamp(img, ch)
        b = layer()
        disc(b, 16, 17, 6.2, BALL)
        disc(b, 14, 14.5, 2.0, BALL_L)
        b.set(18, 19, K); b.set(20, 17, K)
        stamp(img, b)
        f = layer()
        stripes(f, 5, 25, 27, 28, YEL, BLK)
        line(f, 7, 24, 7, 28, BLK); line(f, 25, 24, 25, 28, BLK)
        stamp(img, f)
    elif path == 4:          # 안전 제일: 노랑 안전모 + 초록 십자
        h = layer()
        ellipse(h, 16, 17, 10.5, 8.2, YEL_D)
        ellipse(h, 16, 16.4, 9.2, 7.2, YEL)
        for y in range(11, 14):
            for x in range(10, 16):
                h.set(x, y, YEL_L)
        for x in range(14, 19):
            for y in range(9, 18):
                h.set(x, y, YEL_D if (x == 14 or x == 18) else YEL)
        for x in range(4, 29):
            h.set(x, 23, YEL_D); h.set(x, 24, YEL_D)
        stamp(img, h)
        c = layer()
        for i in range(-2, 3):
            c.set(16 + i, 28, GREEN); c.set(16, 28 + i, GREEN)
        stamp(img, c)
    return img


def cross_glyph(k):
    img = layer()
    if k == 2:               # 콘크리트 벽: 회색 벽 + 앞에 막힌 잡몹
        w = layer()
        poly(w, [(14, 6), (21, 6), (21, 27), (14, 27)], CONC_D)
        poly(w, [(15, 7), (20, 7), (20, 26), (15, 26)], CONC)
        for y in range(8, 25, 6):
            for x in range(15, 20):
                w.set(x, y, CONC_D)
        w.set(16, 8, CONC_L); w.set(16, 14, CONC_L)
        stamp(img, w)
        p = layer()
        for (x, y) in ((8, 12), (10, 17), (8, 22), (11, 9)):
            foe(p, x, y, 2.0)
        stamp(img, p)
        a = layer(); arrow(a, 3, 27, 12, 27, YEL, YEL_L); stamp(img, a)
    elif k == 3:             # 낙석 주의: 떨어지는 돌 + 줄무늬 바리케이드 + 흙 파도
        r = layer()
        for (cx, cy, rr) in ((11, 7, 3.0), (19, 10, 2.4), (15, 14, 2.0)):
            disc(r, cx, cy, rr, DIRT_D)
            disc(r, cx - 0.8, cy - 0.8, rr * 0.45, DIRT_L)
        stamp(img, r)
        f = layer()
        stripes(f, 6, 19, 26, 22, YEL, BLK)
        line(f, 8, 19, 8, 25, BLK); line(f, 24, 19, 24, 25, BLK)
        stamp(img, f)
        w = layer()
        for x in range(5, 28):
            top = 25 + int(1.4 * math.sin(x * 0.6))
            for y in range(top, 28):
                w.set(x, y, DIRT_L if y == top else DIRT)
        stamp(img, w)
    return img


def card31_glyph():
    # 작업 지시: 되돌아오는 화살 + 더미
    img = layer()
    p = layer()
    for (x, y) in ((22, 12), (25, 16), (21, 18), (24, 22)):
        foe(p, x, y, 2.0)
    stamp(img, p)
    a = layer()
    arrow(a, 18, 8, 6, 8, ORANGE, YEL_L)
    line(a, 6, 8, 6, 17, ORANGE)
    arrow(a, 6, 17, 16, 17, ORANGE, YEL_L)
    stamp(img, a)
    b = layer()
    poly(b, [(4, 22), (9, 23), (9, 29), (4, 30)], YEL_D)
    poly(b, [(5, 23), (8, 24), (8, 28), (5, 29)], YEL)
    stamp(img, b)
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    for p in range(5):
        col = COLORS[p]
        save(compose(medal(col), glyph(p)), 'foreman_%d' % p)
        r1 = compose(medal(col), glyph(p)); pips(r1, 1); save(r1, 'foreman_%d_r1' % p)
        r2 = compose(medal(col), glyph(p)); pips(r2, 2); save(r2, 'foreman_%d_r2' % p)
        m = compose(medal(col, gold=True), glyph(p)); star(m, 16, 4.5); save(m, 'foreman_%d_m' % p)
    save(compose(medal(COLORS[0], COLORS[2]), cross_glyph(2)), 'foreman_x2')       # 불도저 × 콘크리트
    save(compose(medal(COLORS[1], COLORS[3]), cross_glyph(3)), 'foreman_x3')       # 산사태 × 철거
    save(compose(medal(COLORS[1]), card31_glyph()), 'foreman_card31')
    print('공사 반장 갈래 그림 저장:', OUT)


if __name__ == '__main__':
    main()
