# 씨름꾼 갈래 그림 (2026-11-02 · 빌드 갈래): 32x32 도트 메달 · Assets/Resources/Icons/Paths/
#   wrestler_<갈래 0~4>          입문 카드 · HUD 입문 칸 (= 갈래 문장)
#   wrestler_<갈래>_r1 · _r2 · _m 공명 I · II · 극의 알림 (아래 금색 점 1 · 2 / 금테 + 별)
#   wrestler_x2 · wrestler_x3    새 교차 (도효 다지기 · 거구 볼링) — 두 갈래 색 반반 테
#   wrestler_card31              31번 새 카드 「모래판 끌기」
# 갈래: 0 볼링 · 1 모래판 · 2 인간 방패 · 3 엎어치기 · 4 장사 (색 = Paths.Wrestler.cs 의 PathDef.color)
# 메달 · 별 · 금색 점 · 저장(+ .meta, 있으면 guid 유지)은 make_paths_postman.py 의 것을 그대로 씀 — 틀을 통일
# 실행: py tools/pixelart/make_paths_wrestler.py
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from png import Img
from make_paths_postman import (K, GOLD, GOLD_D, GOLD_L, STEEL, STEEL_D, OUT, rgb, mix, disc, ring, line,
                                 outline, medal, pips, star, inside, layer, stamp, compose, save)

# 갈래 색 (Paths.Wrestler.cs 의 PathDef.color 와 같게)
COLORS = [(0.949, 0.757, 0.306), (0.91, 0.761, 0.478), (0.498, 0.659, 1.0), (0.961, 0.722, 0.353), (0.851, 0.643, 0.255)]

BALL = (58, 66, 108, 255); BALL_L = (118, 132, 190, 255)
PIN = (250, 246, 234, 255); PIN_D = (200, 192, 176, 255); STRIPE = (214, 64, 56, 255)
SAND = (232, 194, 122, 255); SAND_D = (190, 150, 86, 255); SAND_L = (250, 226, 170, 255)
ROPE = (206, 150, 72, 255); ROPE_D = (140, 96, 44, 255)
SHIELD = (98, 134, 220, 255); SHIELD_L = (160, 192, 255, 255); SHIELD_D = (58, 84, 158, 255)
GREEN = (120, 190, 100, 255); GREEN_D = (74, 130, 66, 255)
HEAD = (150, 100, 62, 255); HEAD_D = (104, 68, 40, 255); HEAD_L = (190, 140, 92, 255)
HORN = (246, 236, 206, 255); HORN_D = (196, 178, 140, 255)
ORANGE = (245, 184, 90, 255); ORANGE_L = (255, 222, 150, 255)


def poly(img, pts, c):
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    for y in range(int(min(ys)) - 1, int(max(ys)) + 2):
        for x in range(int(min(xs)) - 1, int(max(xs)) + 2):
            if inside(pts, x + 0.5, y + 0.5):
                img.set(x, y, c)


def ellipse(img, cx, cy, rx, ry, c):
    for y in range(int(cy - ry - 1), int(cy + ry + 2)):
        for x in range(int(cx - rx - 1), int(cx + rx + 2)):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1:
                img.set(x, y, c)


def pin(p, x0, y0):
    # 볼링 핀 (3 x 8): 머리 · 목 · 배(붉은 띠) · 밑
    rows = [".x.", "xxx", ".x.", ".x.", "xxx", "xxx", "xxx", ".x."]
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch == 'x':
                p.set(x0 + x, y0 + y, STRIPE if y == 3 or y == 4 else (PIN if x < 2 else PIN_D))


def arrow(p, x0, y0, x1, y1, c, cl):
    line(p, x0, y0, x1, y1, c)
    ang = math.atan2(y1 - y0, x1 - x0)
    for da in (2.5, -2.5):
        a = ang + math.pi + da * 0.35
        for r in (1, 2, 3):
            p.set(int(round(x1 + math.cos(a) * r)), int(round(y1 + math.sin(a) * r)), cl)
    p.set(x1, y1, cl)


def glyph(path):
    img = layer()
    if path == 0:            # 볼링: 볼 + 핀 셋
        b = layer()
        disc(b, 12.5, 20.5, 5.4, BALL)
        disc(b, 11.2, 18.6, 1.7, BALL_L)
        for (x, y) in ((13, 18), (15, 19), (14, 21)):
            b.set(x, y, K)
        stamp(img, b)
        for (x, y) in ((18, 8), (22, 11), (19, 16)):
            q = layer(); pin(q, x, y); stamp(img, q)
        m = layer()
        for (x, y) in ((17, 22), (16, 23), (15, 24)):        # 굴러온 자국
            m.set(x + 2, y, rgb(COLORS[0], 1.2))
        stamp(img, m)
    elif path == 1:          # 모래판: 새끼줄 테를 두른 모래 원 + 밖으로 나가는 화살
        r = layer()
        ellipse(r, 15, 19, 10.5, 6.6, ROPE)
        ellipse(r, 15, 19, 8.8, 5.2, SAND)
        for i in range(0, 360, 30):
            t = math.radians(i)
            r.set(int(15 + math.cos(t) * 9.7), int(19 + math.sin(t) * 5.9), ROPE_D)
        for (x, y) in ((11, 19), (15, 21), (18, 18), (13, 17), (20, 21)):
            r.set(x, y, SAND_D)
        r.set(12, 18, SAND_L); r.set(17, 20, SAND_L)
        stamp(img, r)
        a = layer(); arrow(a, 15, 16, 24, 8, GOLD_D, GOLD); stamp(img, a)
        g = layer()
        disc(g, 25.5, 6.5, 2.2, GREEN); g.set(25, 6, GREEN_D)
        stamp(img, g)
    elif path == 2:          # 인간 방패: 방패 + 머리 위에 든 적
        pts = [(8, 11), (24, 11), (24, 18), (21, 23), (16, 27), (11, 23), (8, 18)]
        s = layer()
        poly(s, pts, SHIELD_D)
        poly(s, [(9.5, 12.2), (22.5, 12.2), (22.5, 17.6), (20, 21.8), (16, 24.6), (12, 21.8), (9.5, 17.6)], SHIELD)
        for y in range(13, 17):
            for x in range(10, 16):
                s.set(x, y, SHIELD_L)
        disc(s, 16, 17, 2.6, GOLD_D); disc(s, 16, 17, 1.7, GOLD); s.set(15, 16, GOLD_L)
        stamp(img, s)
        e = layer()
        disc(e, 16, 7.5, 3.4, GREEN)
        e.set(14, 7, K); e.set(18, 7, K); e.set(15, 9, GREEN_D); e.set(16, 9, GREEN_D); e.set(17, 9, GREEN_D)
        e.set(13, 11, GREEN_D); e.set(19, 11, GREEN_D)          # 버둥치는 손
        stamp(img, e)
    elif path == 3:          # 엎어치기: 갈라진 바닥 + 내리꽂는 화살
        g = layer()
        ellipse(g, 16, 23, 10.5, 3.6, SAND_D)
        ellipse(g, 16, 22, 9.2, 2.8, SAND)
        stamp(img, g)
        c = layer()
        for (x0, y0, x1, y1) in ((16, 22, 8, 20), (16, 22, 24, 20), (16, 22, 12, 26), (16, 22, 21, 26)):
            line(c, x0, y0, x1, y1, ROPE_D)
        stamp(img, c)
        a = layer()
        for y in range(5, 15):
            a.set(15, y, ORANGE); a.set(16, y, ORANGE); a.set(14, y, ORANGE_L if y < 9 else ORANGE)
        for i in range(5):
            for x in range(15 - 4 + i, 17 + 4 - i):
                a.set(x, 15 + i, ORANGE)
        a.set(15, 19, ORANGE_L)
        stamp(img, a)
        d = layer()
        for (x, y) in ((6, 17), (5, 19), (26, 17), (27, 19), (9, 14), (23, 14)):
            d.set(x, y, rgb(COLORS[3], 1.3))
        stamp(img, d)
    elif path == 4:          # 장사: 황소 머리 + 큰 뿔
        h = layer()
        poly(h, [(10, 14), (22, 14), (23, 19), (20, 25), (12, 25), (9, 19)], HEAD)
        for y in range(15, 18):
            for x in range(11, 15):
                h.set(x, y, HEAD_L)
        poly(h, [(12, 20), (20, 20), (19, 24), (13, 24)], HEAD_D)
        h.set(14, 22, K); h.set(18, 22, K)                         # 콧구멍
        h.set(12, 17, K); h.set(20, 17, K); h.set(12, 16, PIN); h.set(20, 16, PIN)       # 눈
        stamp(img, h)
        hn = layer()
        for i in range(8):                                          # 왼 뿔
            hn.set(10 - i // 2, 14 - i, HORN); hn.set(11 - i // 2, 14 - i, HORN_D)
        for i in range(8):                                          # 오른 뿔
            hn.set(21 + i // 2, 14 - i, HORN); hn.set(20 + i // 2, 14 - i, HORN_D)
        hn.set(6, 6, HORN); hn.set(25, 6, HORN); hn.set(7, 5, HORN); hn.set(24, 5, HORN)
        stamp(img, hn)
        b = layer()
        for x in range(13, 20):
            b.set(x, 12, rgb(COLORS[4], 1.3))
        stamp(img, b)
    return img


def cross_glyph(k):
    img = layer()
    if k == 2:               # 도효 다지기: 모래 원 + 경계에서 밖으로 밀리는 화살
        r = layer()
        ring(r, 13, 18, 8.5, 2.2, ROPE)
        disc(r, 13, 18, 6.6, SAND)
        for (x, y) in ((10, 17), (14, 20), (12, 15), (16, 18)):
            r.set(x, y, SAND_D)
        stamp(img, r)
        a = layer(); arrow(a, 16, 16, 27, 11, ORANGE, ORANGE_L); stamp(img, a)
        s = layer()
        for (x, y) in ((22, 6), (25, 6), (23, 4)):
            s.set(x, y, rgb(COLORS[3], 1.3))
        stamp(img, s)
    elif k == 3:             # 거구 볼링: 큰 볼 + 뿔
        b = layer()
        disc(b, 15, 19, 7.2, BALL)
        disc(b, 12.6, 16.2, 2.2, BALL_L)
        for (x, y) in ((16, 15), (19, 17), (17, 19)):
            b.set(x, y, K)
        stamp(img, b)
        hn = layer()
        for i in range(6):
            hn.set(9 - i // 2, 14 - i, HORN); hn.set(10 - i // 2, 14 - i, HORN_D)
            hn.set(21 + i // 2, 14 - i, HORN); hn.set(20 + i // 2, 14 - i, HORN_D)
        stamp(img, hn)
        q = layer(); pin(q, 24, 14); stamp(img, q)
    return img


def card31_glyph():
    img = layer()
    r = layer()
    ring(r, 12, 18, 8, 2.0, ROPE)
    disc(r, 12, 18, 6.2, SAND)
    for (x, y) in ((9, 17), (13, 20), (11, 14), (15, 17)):
        r.set(x, y, SAND_D)
    stamp(img, r)
    a = layer(); arrow(a, 18, 16, 26, 13, ORANGE, ORANGE_L); stamp(img, a)
    p = layer()
    disc(p, 25.5, 17.5, 2.4, rgb(COLORS[2], 1.0))               # 영웅 (청샅바)
    p.set(25, 17, PIN)
    stamp(img, p)
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    for p in range(5):
        col = COLORS[p]
        save(compose(medal(col), glyph(p)), 'wrestler_%d' % p)
        r1 = compose(medal(col), glyph(p)); pips(r1, 1); save(r1, 'wrestler_%d_r1' % p)
        r2 = compose(medal(col), glyph(p)); pips(r2, 2); save(r2, 'wrestler_%d_r2' % p)
        m = compose(medal(col, gold=True), glyph(p)); star(m, 16, 4.5); save(m, 'wrestler_%d_m' % p)
    save(compose(medal(COLORS[3], COLORS[1]), cross_glyph(2)), 'wrestler_x2')       # 엎어치기 × 모래판
    save(compose(medal(COLORS[0], COLORS[4]), cross_glyph(3)), 'wrestler_x3')       # 볼링 × 장사
    save(compose(medal(COLORS[1]), card31_glyph()), 'wrestler_card31')
    print('씨름꾼 갈래 그림 저장:', OUT)


if __name__ == '__main__':
    main()
