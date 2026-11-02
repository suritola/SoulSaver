# 인형사 갈래 그림 (2026-11-02 · 빌드 갈래): 32x32 도트 메달 · Assets/Resources/Icons/Paths/
#   puppeteer_<갈래 0~4>          입문 카드 · HUD 입문 칸 (= 갈래 문장)
#   puppeteer_<갈래>_r1 · _r2 · _m 공명 I · II · 극의 알림 (아래 금색 점 1 · 2 / 금테 + 별)
#   puppeteer_x2 · puppeteer_x3   새 교차 (이중주 · 대역) — 두 갈래 색 반반 테
#   (인형사는 31번 새 카드가 없어 card31 그림 없음)
# 갈래: 0 팽팽한 줄 · 1 호위 인형 · 2 바꿔치기 · 3 거울 인형 · 4 꼭두각시 (색 = Paths.Puppeteer.cs 의 PathDef.color)
# 메달 · 별 · 금색 점 · 저장(+ .meta, 있으면 guid 유지)은 make_paths_postman.py 의 것을 그대로 씀 — 틀을 통일
# 실행: py tools/pixelart/make_paths_puppeteer.py
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from png import Img
from make_paths_postman import (K, GOLD, GOLD_D, GOLD_L, STEEL, STEEL_D, OUT, rgb, mix, disc, ring, line,
                                 outline, medal, pips, star, inside, layer, stamp, compose, save)
from make_paths_wrestler import poly, ellipse, arrow

# 갈래 색 (Paths.Puppeteer.cs 의 PathDef.color 와 같게)
COLORS = [(0.75, 0.65, 1.0), (0.6, 0.85, 1.0), (1.0, 0.85, 0.5), (0.85, 0.95, 1.0), (0.95, 0.7, 0.9)]

SKY = (159, 184, 255, 255); SKY_L = (214, 226, 255, 255); SKY_D = (96, 118, 200, 255)       # 줄 (아군색 #9FB8FF)
WOOD = (214, 168, 108, 255); WOOD_D = (150, 108, 64, 255); WOOD_L = (244, 214, 160, 255)    # 더미
RED = (200, 60, 70, 255); RED_D = (140, 36, 48, 255)                                         # 막 · 무늬
FOE = (120, 190, 100, 255); FOE_D = (74, 130, 66, 255)
PINK = (240, 150, 210, 255); PINK_D = (170, 90, 150, 255)
WHITE = (250, 246, 234, 255); BLK = (40, 40, 48, 255)


def foe(p, cx, cy, r=2.2):
    disc(p, cx, cy, r, FOE)
    p.set(int(cx) - 1, int(cy) - 1, K); p.set(int(cx) + 1, int(cy) - 1, K)
    p.set(int(cx), int(cy) + 1, FOE_D)


def dummy(p, cx, cy, s=1.0, flip=False):
    # 작은 나무 더미: 머리 + 몸 + 두 팔 (cx, cy = 머리 중심)
    disc(p, cx, cy, 2.6 * s, WOOD_D)
    disc(p, cx, cy, 2.0 * s, WOOD)
    p.set(int(cx) - 1, int(cy) - 1, K); p.set(int(cx) + 1, int(cy) - 1, K)
    p.set(int(cx), int(cy) + 1, RED)
    by = int(cy + 3 * s)
    for y in range(by, by + int(6 * s)):
        p.set(int(cx), y, WOOD_D); p.set(int(cx) + 1, y, WOOD)
    line(p, int(cx - 4 * s), by + 1, int(cx + 4 * s), by + 1, WOOD_D)
    line(p, int(cx - 4 * s), by + 2, int(cx + 4 * s), by + 2, WOOD)


def glyph(path):
    img = layer()
    if path == 0:            # 팽팽한 줄: 팽팽한 사선 줄 + 갈라진 잡몹 둘 + 끝에 더미
        for (x, y) in ((11, 10), (17, 21)):
            f = layer(); foe(f, x, y, 2.4); stamp(img, f)
        s = layer()
        line(s, 5, 26, 23, 8, SKY); line(s, 6, 26, 24, 8, SKY_D)
        line(s, 5, 25, 22, 8, SKY_L)
        for (x, y) in ((11, 20), (16, 15)):
            s.set(x, y, WHITE)
        stamp(img, s)
        d = layer(); dummy(d, 25, 6, 0.8); stamp(img, d)
        h = layer(); disc(h, 5, 26, 1.6, GOLD); stamp(img, h)
    elif path == 1:          # 호위 인형: 팔 벌린 더미 + 튕겨 나가는 탄
        d = layer()
        dummy(d, 15, 9, 1.4)
        stamp(img, d)
        sh = layer()
        for a in range(-50, 51, 10):                       # 앞을 막는 호
            r = math.radians(a)
            sh.set(int(round(24 + 3 * math.cos(r))), int(round(16 + 8 * math.sin(r))), SKY_L)
            sh.set(int(round(23 + 3 * math.cos(r))), int(round(16 + 8 * math.sin(r))), SKY)
        stamp(img, sh)
        b = layer()
        arrow(b, 29, 6, 26, 9, RED, PINK)
        disc(b, 28, 24, 1.6, RED); b.set(28, 24, PINK)
        stamp(img, b)
    elif path == 2:          # 바꿔치기: 두 더미 + 서로 바꾸는 화살
        d = layer(); dummy(d, 8, 9, 0.9); dummy(d, 24, 21, 0.9); stamp(img, d)
        a = layer()
        arrow(a, 11, 8, 21, 8, GOLD, GOLD_L)
        arrow(a, 21, 24, 11, 24, GOLD, GOLD_L)
        line(a, 21, 8, 21, 11, GOLD)
        line(a, 11, 24, 11, 21, GOLD)
        stamp(img, a)
        sp = layer()
        for (x, y) in ((16, 15), (15, 16), (17, 16), (16, 17)):
            sp.set(x, y, WHITE)
        stamp(img, sp)
    elif path == 3:          # 거울 인형: 가운데 점선 + 좌우 대칭 더미
        m = layer()
        for y in range(3, 29, 3):
            m.set(16, y, SKY_L); m.set(16, y + 1, SKY)
        stamp(img, m)
        d = layer(); dummy(d, 8, 10, 1.0); stamp(img, d)
        e = layer(); dummy(e, 24, 10, 1.0); stamp(img, e)
        s = layer()
        line(s, 8, 24, 16, 28, SKY); line(s, 24, 24, 16, 28, SKY)
        disc(s, 16, 28, 1.4, GOLD)
        stamp(img, s)
    elif path == 4:          # 꼭두각시: 십자 조종대 + 실 + 매달린 잡몹
        c = layer()
        line(c, 8, 6, 24, 6, WOOD_D); line(c, 8, 7, 24, 7, WOOD)
        line(c, 16, 4, 16, 10, WOOD_D); line(c, 17, 4, 17, 10, WOOD)
        stamp(img, c)
        s = layer()
        for x, tx in ((9, 12), (16, 16), (23, 20)):
            line(s, x, 8, tx, 17, PINK)
        stamp(img, s)
        f = layer()
        foe(f, 16, 21, 3.4)
        line(f, 13, 18, 12, 24, FOE_D); line(f, 19, 18, 20, 24, FOE_D)
        stamp(img, f)
    return img


def cross_glyph(k):
    img = layer()
    if k == 2:               # 이중주: 두 줄이 한 적에서 X 로 만남 + 두 배 표시
        f = layer(); foe(f, 16, 16, 3.2); stamp(img, f)
        s = layer()
        line(s, 4, 6, 28, 26, SKY); line(s, 5, 6, 29, 26, SKY_D)
        line(s, 28, 6, 4, 26, PINK); line(s, 27, 6, 3, 26, PINK_D)
        stamp(img, s)
        t = layer()
        for (x, y) in ((16, 4), (15, 5), (17, 5), (16, 6)):
            t.set(x, y, GOLD_L)
        stamp(img, t)
    elif k == 3:             # 대역: 꼭두각시가 자리에 서고 바꾸는 화살
        d = layer(); dummy(d, 22, 20, 0.8); stamp(img, d)
        f = layer(); foe(f, 9, 10, 3.0); line(f, 9, 13, 9, 17, FOE_D); stamp(img, f)
        s = layer()
        line(s, 5, 3, 9, 7, PINK); line(s, 13, 3, 9, 7, PINK)
        arrow(s, 14, 12, 19, 18, GOLD, GOLD_L)
        stamp(img, s)
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    for p in range(5):
        col = COLORS[p]
        save(compose(medal(col), glyph(p)), 'puppeteer_%d' % p)
        r1 = compose(medal(col), glyph(p)); pips(r1, 1); save(r1, 'puppeteer_%d_r1' % p)
        r2 = compose(medal(col), glyph(p)); pips(r2, 2); save(r2, 'puppeteer_%d_r2' % p)
        m = compose(medal(col, gold=True), glyph(p)); star(m, 16, 4.5); save(m, 'puppeteer_%d_m' % p)
    save(compose(medal(COLORS[3], COLORS[0]), cross_glyph(2)), 'puppeteer_x2')       # 거울 인형 × 팽팽한 줄
    save(compose(medal(COLORS[2], COLORS[4]), cross_glyph(3)), 'puppeteer_x3')       # 바꿔치기 × 꼭두각시
    print('인형사 갈래 그림 저장:', OUT)


if __name__ == '__main__':
    main()
