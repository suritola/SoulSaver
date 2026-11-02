# 사진사 갈래 그림 (2026-11-02 · 빌드 갈래): 32x32 도트 메달 · Assets/Resources/Icons/Paths/
#   photographer_<갈래 0~4>          입문 카드 · HUD 입문 칸 (= 갈래 문장)
#   photographer_<갈래>_r1 · _r2 · _m 공명 I · II · 극의 알림 (아래 금색 점 1 · 2 / 금테 + 별)
#   photographer_x2 · photographer_x3  새 교차 (종이 인형극 · 스트로보 연사) — 두 갈래 색 반반 테
#   (사진사는 31번 새 카드가 없어 card31 그림 없음)
# 갈래: 0 연사 · 1 망원 · 2 암실 · 3 종이 공작 · 4 플래시 (색 = Paths.Photographer.cs 의 PathDef.color)
# 메달 · 별 · 금색 점 · 저장(+ .meta, 있으면 guid 유지)은 make_paths_postman.py 의 것을 그대로 씀 — 틀을 통일
# 실행: py tools/pixelart/make_paths_photographer.py
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from png import Img
from make_paths_postman import (K, GOLD, GOLD_D, GOLD_L, STEEL, STEEL_D, OUT, rgb, mix, disc, ring, line,
                                 outline, medal, pips, star, inside, layer, stamp, compose, save)
from make_paths_wrestler import poly, ellipse, arrow

# 갈래 색 (Paths.Photographer.cs 의 PathDef.color 와 같게)
COLORS = [(1.0, 0.69, 0.38), (0.561, 0.706, 1.0), (0.753, 0.549, 1.0), (0.949, 0.949, 0.949), (0.91, 0.941, 1.0)]

PAPER = (244, 242, 232, 255); PAPER_D = (176, 172, 160, 255); PAPER_L = (255, 255, 250, 255)     # 흑백 종이
FILM = (52, 54, 66, 255); FILM_L = (92, 96, 114, 255)                                            # 필름 띠
LENS = (86, 96, 128, 255); LENS_D = (52, 60, 86, 255); LENS_L = (150, 168, 214, 255)             # 렌즈 통
BULB = (255, 244, 190, 255); BULB_D = (240, 200, 90, 255)                                        # 전구 불빛
LAMP = (210, 60, 70, 255); LAMP_L = (255, 130, 120, 255)                                         # 암실 붉은 등
VIOLET = (150, 110, 214, 255)


def rect(p, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            p.set(x, y, c)


def film_card(p, x0, y0, w, h, fill):
    # 필름 한 컷: 어두운 테 + 안쪽 밝은 칸 + 위아래 구멍
    rect(p, x0, y0, x0 + w, y0 + h, FILM)
    rect(p, x0 + 2, y0 + 2, x0 + w - 2, y0 + h - 2, fill)
    for x in range(x0 + 1, x0 + w, 3):
        p.set(x, y0, FILM_L); p.set(x, y0 + h, FILM_L)


def sparkle(p, cx, cy, r, c):
    line(p, cx - r, cy, cx + r, cy, c); line(p, cx, cy - r, cx, cy + r, c)


def plane(p, cx, cy, s=1.0):
    # 종이비행기: 접힌 삼각형 두 장
    poly(p, [(cx - 7 * s, cy), (cx + 7 * s, cy - 4 * s), (cx - 1 * s, cy + 1 * s)], PAPER)
    poly(p, [(cx - 7 * s, cy), (cx - 1 * s, cy + 1 * s), (cx - 2 * s, cy + 5 * s)], PAPER_D)
    line(p, cx - 7 * s, cy, cx + 7 * s, cy - 4 * s, PAPER_L)


def doll(p, cx, cy, s=1.0, c=PAPER):
    # 종이 인형(사본): 머리 + 몸통 + 팔
    disc(p, cx, cy - 5 * s, 2.2 * s, c)
    poly(p, [(cx - 3 * s, cy - 2 * s), (cx + 3 * s, cy - 2 * s), (cx + 4 * s, cy + 6 * s), (cx - 4 * s, cy + 6 * s)], c)
    line(p, cx - 6 * s, cy, cx - 3 * s, cy - 1 * s, c); line(p, cx + 6 * s, cy, cx + 3 * s, cy - 1 * s, c)
    p.set(int(cx - 1 * s), int(cy - 5 * s), K); p.set(int(cx + 1 * s), int(cy - 5 * s), K)


def glyph(path):
    img = layer()
    if path == 0:            # 연사: 겹쳐 펼쳐진 필름 세 컷 + 빠른 줄
        for (x, y, c) in ((5, 14, PAPER_D), (10, 10, (200, 196, 184, 255)), (15, 6, PAPER)):
            f = layer(); film_card(f, x, y, 12, 14, c); stamp(img, f)
        m = layer()
        for (x, y) in ((3, 22), (6, 25), (9, 28)):
            line(m, x, y, x + 6, y, GOLD_L)
        sparkle(m, 25, 7, 2, BULB)
        stamp(img, m)
    elif path == 1:          # 망원: 긴 렌즈 통 + 겨눈 십자 원
        t = layer()
        rect(t, 3, 17, 20, 25, LENS)
        rect(t, 3, 17, 20, 18, LENS_L); rect(t, 3, 25, 20, 25, LENS_D)
        rect(t, 19, 14, 24, 28, LENS_D); rect(t, 20, 15, 22, 27, LENS)
        line(t, 8, 17, 8, 25, LENS_D); line(t, 13, 17, 13, 25, LENS_D)
        stamp(img, t)
        r = layer()
        for a in range(0, 360, 10):
            ra = math.radians(a)
            r.set(int(round(24 + 5 * math.cos(ra))), int(round(9 + 5 * math.sin(ra))), GOLD)
        line(r, 24, 2, 24, 16, GOLD_L); line(r, 17, 9, 31, 9, GOLD_L)
        disc(r, 24, 9, 1.0, LAMP)
        stamp(img, r)
    elif path == 2:          # 암실: 붉은 등 + 빨랫줄에 걸린 인화지
        a = layer()
        line(a, 3, 8, 29, 8, PAPER_D)
        for x, c in ((8, PAPER), (16, PAPER_L), (24, PAPER)):
            rect(a, x - 3, 9, x + 3, 19, c)
            line(a, x - 3, 9, x + 3, 9, PAPER_D); line(a, x - 3, 19, x + 3, 19, PAPER_D)
            disc(a, x, 14, 2.0, FILM_L)
            a.set(x, 8, K)
        stamp(img, a)
        l = layer()
        disc(l, 16, 26, 4.2, LAMP); disc(l, 16, 26, 2.6, LAMP_L)
        line(l, 11, 22, 21, 22, VIOLET)
        stamp(img, l)
    elif path == 3:          # 종이 공작: 종이비행기 + 날리는 종잇조각
        p = layer(); plane(p, 15, 14, 1.4); stamp(img, p)
        s = layer()
        for (x, y) in ((5, 24), (11, 27), (20, 25), (26, 21), (8, 6)):
            rect(s, x, y, x + 2, y + 2, PAPER); s.set(x, y + 2, PAPER_D); s.set(x + 1, y + 2, PAPER_D)
        stamp(img, s)
    elif path == 4:          # 플래시: 전구 + 퍼져 나가는 빛살
        r = layer()
        for a in range(0, 360, 30):
            ra = math.radians(a)
            line(r, 16 + 8 * math.cos(ra), 16 + 8 * math.sin(ra), 16 + 13 * math.cos(ra), 16 + 13 * math.sin(ra), BULB if a % 60 == 0 else BULB_D)
        stamp(img, r)
        b = layer()
        disc(b, 16, 16, 6.5, BULB_D); disc(b, 16, 16, 5.0, BULB); disc(b, 14, 14, 1.8, PAPER_L)
        rect(b, 13, 22, 19, 24, STEEL_D)
        stamp(img, b)
    return img


def cross_glyph(k):
    img = layer()
    if k == 2:               # 종이 인형극: 인형 둘이 줄로 매달려 펄럭임 (암실 × 종이 공작)
        s = layer()
        line(s, 4, 5, 28, 5, VIOLET)
        line(s, 10, 5, 10, 11, PAPER_D); line(s, 22, 5, 22, 9, PAPER_D)
        stamp(img, s)
        d1 = layer(); doll(d1, 10, 18, 1.1); stamp(img, d1)
        d2 = layer(); doll(d2, 22, 16, 1.1, PAPER_D); stamp(img, d2)
        p = layer(); plane(p, 16, 28, 0.6); stamp(img, p)
    elif k == 3:             # 스트로보 연사: 필름 컷 둘 + 번쩍임 (연사 × 플래시)
        for (x, y, c) in ((4, 14, PAPER_D), (11, 10, PAPER)):
            f = layer(); film_card(f, x, y, 11, 13, c); stamp(img, f)
        r = layer()
        for a in range(0, 360, 45):
            ra = math.radians(a)
            line(r, 25 + 2 * math.cos(ra), 8 + 2 * math.sin(ra), 25 + 6 * math.cos(ra), 8 + 6 * math.sin(ra), BULB)
        disc(r, 25, 8, 2.2, PAPER_L)
        for (x, y) in ((4, 26), (8, 28)):
            line(r, x, y, x + 6, y, GOLD_L)
        stamp(img, r)
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    for p in range(5):
        col = COLORS[p]
        save(compose(medal(col), glyph(p)), 'photographer_%d' % p)
        r1 = compose(medal(col), glyph(p)); pips(r1, 1); save(r1, 'photographer_%d_r1' % p)
        r2 = compose(medal(col), glyph(p)); pips(r2, 2); save(r2, 'photographer_%d_r2' % p)
        m = compose(medal(col, gold=True), glyph(p)); star(m, 16, 4.5); save(m, 'photographer_%d_m' % p)
    save(compose(medal(COLORS[2], COLORS[3]), cross_glyph(2)), 'photographer_x2')       # 암실 × 종이 공작
    save(compose(medal(COLORS[0], COLORS[4]), cross_glyph(3)), 'photographer_x3')       # 연사 × 플래시
    print('사진사 갈래 그림 저장:', OUT)


if __name__ == '__main__':
    main()
