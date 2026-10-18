# 우체부 갈래 그림 (2026-10-19 · 빌드 갈래): 32x32 도트 메달 · Assets/Resources/Icons/Paths/
#   postman_<갈래 0~4>          입문 카드 · HUD 입문 칸 (= 갈래 문장)
#   postman_<갈래>_r1 · _r2 · _m 공명 I · II · 극의 알림 (아래 금색 점 1 · 2 / 금테 + 별)
#   postman_x2 · postman_x3      새 교차 (달리는 소포 · 전국 배송) — 두 갈래 색 반반 테
#   postman_parcel               무거운 소포 · 달리는 소포가 날아가는 그림 (16x16, 배경 없음)
# 갈래: 0 순회 배달 · 1 무거운 소포 · 2 속달 · 3 자전거 배달 · 4 장거리 배송 (색 = Paths.Postman.cs 의 PathDef.color)
# .meta 는 Icons/ability_900.png.meta 를 복사해 guid 만 새로 (Unity 를 열지 않아도 픽셀 아트 설정 그대로)
# 실행: py tools/pixelart/make_paths_postman.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'Paths')
META_SRC = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'ability_900.png.meta')

K = (26, 17, 44, 255)                # 외곽선 (기존 아이콘과 같은 색)
PAPER = (246, 238, 214, 255); PAPER_D = (204, 188, 158, 255)
SEAL = (214, 64, 56, 255); SEAL_L = (250, 120, 100, 255)
GOLD = (245, 205, 95, 255); GOLD_D = (185, 135, 50, 255); GOLD_L = (255, 240, 170, 255)
BOX = (196, 142, 82, 255); BOX_D = (140, 92, 50, 255); BOX_L = (228, 184, 120, 255); TAPE = (232, 214, 150, 255)
STEEL = (170, 180, 200, 255); STEEL_D = (100, 110, 135, 255)

COLORS = [(0.95, 0.55, 0.45), (0.8, 0.6, 0.35), (1.0, 0.85, 0.4), (0.5, 0.9, 0.6), (0.55, 0.75, 1.0)]


def rgb(c, k=1.0, a=255):
    return (min(255, int(c[0] * 255 * k)), min(255, int(c[1] * 255 * k)), min(255, int(c[2] * 255 * k)), a)


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3)) + (255,)


def disc(img, cx, cy, r, c):
    for y in range(int(cy - r - 1), int(cy + r + 2)):
        for x in range(int(cx - r - 1), int(cx + r + 2)):
            if (x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2 <= r * r:
                img.set(x, y, c)


def ring(img, cx, cy, r, w, c):
    for y in range(img.h):
        for x in range(img.w):
            d = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
            if abs(d - r) <= w / 2:
                img.set(x, y, c)


def line(img, x0, y0, x1, y1, c):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for i in range(n + 1):
        t = i / n
        img.set(int(round(x0 + (x1 - x0) * t)), int(round(y0 + (y1 - y0) * t)), c)


def outline(img, c=K):
    src = [[img.px[y][x] for x in range(img.w)] for y in range(img.h)]
    for y in range(img.h):
        for x in range(img.w):
            if src[y][x][3] > 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < img.w and 0 <= yy < img.h and src[yy][xx][3] > 0:
                    img.set(x, y, c)
                    break


# ------------------------------------------------------------------ 메달 (테 · 안쪽)
def medal(col_a, col_b=None, gold=False):
    img = Img(32, 32)
    cx = cy = 16
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            d = math.hypot(dx, dy)
            if d > 15.6:
                continue
            if d > 14.6:
                img.set(x, y, K)
                continue
            if d > 12.4:                         # 테: 위는 밝게 · 아래는 어둡게 (교차는 왼쪽 · 오른쪽 색)
                base = col_a if (col_b is None or dx < 0) else col_b
                if gold:
                    base = (0.96, 0.8, 0.37)
                shade = 1.15 - 0.35 * ((dy / 14.0) + 1) / 2
                img.set(x, y, rgb(base, shade))
                continue
            if d > 11.6:
                img.set(x, y, K)
                continue
            # 안쪽: 갈래 색을 어둡게 깐 바탕 + 위쪽에 은은한 빛
            base = col_a if (col_b is None or dx < 0) else col_b
            k = 0.22 + 0.12 * max(0.0, 1 - math.hypot(dx + 3, dy + 4) / 11)
            img.set(x, y, mix((24, 18, 40), rgb(base), k))
    # 테에 박힌 작은 반짝임
    img.set(9, 4, rgb(col_a, 1.6))
    img.set(8, 5, rgb(col_a, 1.4))
    return img


def pips(img, n):
    # 공명 단계: 테 아래쪽 금색 점
    xs = [16] if n == 1 else [12, 20]
    for x in xs:
        disc(img, x, 28.5, 1.9, K)
        disc(img, x, 28.5, 1.2, GOLD)
        img.set(x - 1, 27, GOLD_L)


def star(img, cx, cy):
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        r = 3.6 if i % 2 == 0 else 1.6
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    for y in range(int(cy - 5), int(cy + 5)):
        for x in range(int(cx - 5), int(cx + 5)):
            if inside(pts, x + 0.5, y + 0.5):
                img.set(x, y, GOLD)
    img.set(int(cx), int(cy) - 2, GOLD_L)


def inside(pts, x, y):
    c = False
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi + 1e-9) + xi:
            c = not c
        j = i
    return c


# ------------------------------------------------------------------ 그림 조각 (따로 그린 뒤 윤곽선을 둘러 붙임)
def layer():
    return Img(32, 32)


def stamp(img, part):
    outline(part)
    img.paste(part, 0, 0)


def envelope(p, x0, y0, w, h, seal=True):
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            p.set(x, y, PAPER if y < y0 + h - 2 else PAPER_D)
    # 덮개 V
    cx = x0 + (w - 1) / 2
    for i in range(w // 2 + 1):
        yy = y0 + int(i * (h * 0.55) / (w / 2))
        p.set(x0 + i, yy, PAPER_D)
        p.set(x0 + w - 1 - i, yy, PAPER_D)
    if seal:
        sy = y0 + int(h * 0.55)
        disc(p, cx + 0.5, sy + 0.5, 1.6, SEAL)
        p.set(int(cx), sy - 1, SEAL_L)


def parcel_box(p, x0, y0, w, h):
    top = max(2, h // 3)
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            if y < y0 + top:
                p.set(x, y, BOX_L)
            else:
                p.set(x, y, BOX if x < x0 + w - 3 else BOX_D)
    for x in range(x0, x0 + w):
        p.set(x, y0 + top, BOX_D)
    mx = x0 + w // 2
    for y in range(y0, y0 + h):
        p.set(mx, y, TAPE)
        p.set(mx - 1, y, TAPE)
    # 리본 매듭
    p.set(mx - 3, y0 - 1, SEAL); p.set(mx - 2, y0 - 1, SEAL); p.set(mx + 1, y0 - 1, SEAL); p.set(mx + 2, y0 - 1, SEAL)
    p.set(mx - 1, y0 - 1, SEAL_L); p.set(mx, y0 - 1, SEAL_L)
    p.set(mx - 3, y0 - 2, SEAL); p.set(mx + 2, y0 - 2, SEAL)


def bolt(p, ox, oy, c, cl):
    rows = ["...xxx",
            "..xxx.",
            ".xxx..",
            "xxxxxx",
            "..xxx.",
            ".xxx..",
            ".xx...",
            "xx...."]
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch == 'x':
                p.set(ox + x, oy + y, cl if y < 3 else c)


def glyph(path):
    img = layer()
    if path == 0:            # 순회 배달: 편지 + 둘레를 도는 화살 고리
        a = layer()
        for i in range(0, 300, 2):
            t = math.radians(i - 60)
            a.set(int(round(16 + math.cos(t) * 9.5)), int(round(16 + math.sin(t) * 9.5)), (255, 200, 170, 255))
        # 화살촉 (고리 끝)
        t = math.radians(240)
        hx, hy = int(round(16 + math.cos(t) * 9.5)), int(round(16 + math.sin(t) * 9.5))
        for dx, dy in ((0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (0, 2), (-1, 0), (0, -1)):
            a.set(hx + dx, hy + dy, (255, 225, 200, 255))
        stamp(img, a)
        e = layer(); envelope(e, 10, 12, 12, 8); stamp(img, e)
    elif path == 1:          # 무거운 소포: 끈 묶은 상자 + 무게 선
        b = layer(); parcel_box(b, 9, 12, 14, 10); stamp(img, b)
        w = layer()
        for x in (7, 24):
            for y in range(23, 26):
                w.set(x, y, rgb(COLORS[1], 1.3))
        stamp(img, w)
    elif path == 2:          # 속달: 편지 + 번개 + 표식
        e = layer(); envelope(e, 7, 13, 13, 9); stamp(img, e)
        b = layer(); bolt(b, 18, 8, GOLD_D, GOLD); stamp(img, b)
        m = layer()
        for i in range(4):
            m.set(23 + i, 19 + i, SEAL); m.set(26 - i, 19 + i, SEAL)
        stamp(img, m)
    elif path == 3:          # 자전거 배달: 자전거 + 짐받이 편지
        bk = layer()
        ring(bk, 10.5, 20.5, 3.6, 1.2, STEEL)
        ring(bk, 21.5, 20.5, 3.6, 1.2, STEEL)
        bk.set(10, 20, STEEL_D); bk.set(21, 20, STEEL_D)
        fc = rgb(COLORS[3], 1.15)
        line(bk, 10, 20, 15, 15, fc); line(bk, 15, 15, 21, 20, fc); line(bk, 15, 15, 20, 15, fc)
        line(bk, 20, 15, 21, 20, fc); line(bk, 10, 20, 15, 20, fc); line(bk, 15, 20, 15, 15, fc)
        line(bk, 19, 13, 20, 15, STEEL); bk.set(18, 12, K); bk.set(19, 12, STEEL_D)
        line(bk, 14, 14, 16, 14, STEEL_D)
        stamp(img, bk)
        e = layer(); envelope(e, 8, 9, 8, 5, seal=False); stamp(img, e)
        s = layer()
        for y in (10, 12):
            for x in range(3, 6):
                s.set(x, y, rgb(COLORS[3], 1.3))
        stamp(img, s)
    elif path == 4:          # 장거리 배송: 멀리 날아가는 편지 + 긴 꼬리 + 날개
        t = layer()
        for i in range(10):
            c = rgb(COLORS[4], 0.7 + 0.06 * i)
            t.set(5 + i, 22 - i // 2, c)
            t.set(5 + i, 23 - i // 2, c)
        stamp(img, t)
        e = layer(); envelope(e, 13, 10, 11, 8); stamp(img, e)
        w = layer()
        for i, (x, y) in enumerate(((12, 9), (11, 8), (10, 8), (9, 7), (11, 9), (10, 9))):
            w.set(x, y, (235, 245, 255, 255))
        stamp(img, w)
    return img


def cross_glyph(k):
    img = layer()
    if k == 2:               # 달리는 소포: 바퀴 + 떨어지는 소포
        bk = layer()
        ring(bk, 11.5, 20.5, 4.2, 1.2, STEEL)
        line(bk, 11, 20, 16, 15, rgb(COLORS[3], 1.15))
        stamp(img, bk)
        b = layer(); parcel_box(b, 16, 15, 9, 8); stamp(img, b)
        s = layer()
        for x in range(4, 8):
            s.set(x, 13, rgb(COLORS[3], 1.3)); s.set(x + 1, 16, rgb(COLORS[3], 1.3))
        stamp(img, s)
    elif k == 3:             # 전국 배송: 둥근 땅 + 둘레를 도는 편지
        g = layer()
        disc(g, 16, 17, 7, rgb(COLORS[4], 0.75))
        for (x, y) in ((13, 13), (14, 13), (12, 14), (13, 14), (14, 15), (18, 18), (19, 18), (19, 19), (17, 20), (18, 21)):
            g.set(x, y, (120, 200, 120, 255))
        stamp(img, g)
        a = layer()
        for i in range(0, 220, 8):
            t = math.radians(i + 160)
            a.set(int(round(16 + math.cos(t) * 10.5)), int(round(17 + math.sin(t) * 10.5)), rgb(COLORS[0], 1.25))
        stamp(img, a)
        e = layer(); envelope(e, 19, 6, 8, 5, seal=False); stamp(img, e)
    return img


def compose(bg, g):
    bg.paste(g, 0, 0)
    return bg


def parcel_sprite():
    p = Img(16, 16)
    parcel_box(p, 2, 4, 12, 10)
    outline(p)
    return p


# ------------------------------------------------------------------ 저장 (+ .meta)
def save(img, name):
    path = os.path.join(OUT, name + '.png')
    write(img, path)
    meta = path + '.meta'
    if not os.path.exists(meta):
        src = open(META_SRC, encoding='utf-8').read()
        src = re.sub(r'^guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, src, count=1, flags=re.M)
        open(meta, 'w', encoding='utf-8', newline='\n').write(src)


def main():
    os.makedirs(OUT, exist_ok=True)
    for p in range(5):
        col = COLORS[p]
        g = glyph(p)
        save(compose(medal(col), g), 'postman_%d' % p)
        r1 = compose(medal(col), glyph(p)); pips(r1, 1); save(r1, 'postman_%d_r1' % p)
        r2 = compose(medal(col), glyph(p)); pips(r2, 2); save(r2, 'postman_%d_r2' % p)
        m = compose(medal(col, gold=True), glyph(p)); star(m, 16, 4.5); save(m, 'postman_%d_m' % p)
    save(compose(medal(COLORS[3], COLORS[1]), cross_glyph(2)), 'postman_x2')
    save(compose(medal(COLORS[0], COLORS[4]), cross_glyph(3)), 'postman_x3')
    save(parcel_sprite(), 'postman_parcel')
    print('우체부 갈래 그림 저장:', OUT)


if __name__ == '__main__':
    main()
