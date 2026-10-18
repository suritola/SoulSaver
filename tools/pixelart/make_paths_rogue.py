# 도적 갈래 그림 (2026-10-19 · 빌드 갈래, Paths.Rogue): 32x32
#   갈래 색 메달(위 왼쪽이 밝은 3단 테 + 짙은 안쪽) 안에 갈래 상징, 남보라 외곽선 — 새 그림체(초월 카드 메달)와 같은 틀
#   rogue_<갈래>        입문 카드 · HUD 갈래 칸        (0 출혈 · 1 그림자 · 2 회귀 칼날 · 3 질주 암살 · 4 광란)
#   rogue_<갈래>_r1/_r2  공명 I · II 알림 (테 위 보석 하나 · 둘)
#   rogue_<갈래>_m       극의 알림 (금테 + 보석 셋)
#   rogue_x2 · rogue_x3 새 교차 (두 갈래 색을 반씩 나눈 테)
#   → Assets/Resources/Icons/Paths/ (+ .meta: ability_900.png.meta 를 복사해 guid 만 새로)
# 실행: python tools/pixelart/make_paths_rogue.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line, ROOT
from make_menu_icons import retry

OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'Paths')
META = os.path.join(ROOT, 'Assets', 'Resources', 'Icons', 'ability_900.png.meta')

K = (26, 17, 44, 255)                   # 남보라 외곽선
W = (255, 255, 255, 255)
IN1 = (58, 46, 92, 255); IN2 = (40, 31, 70, 255); IN3 = (27, 20, 50, 255)
SL = (236, 240, 252, 255); S = (178, 188, 212, 255); SD = (108, 116, 146, 255)      # 표창 은빛
BLL = (255, 120, 130, 255); BL = (214, 36, 58, 255); BLD = (128, 16, 36, 255)       # 피
GL = (255, 236, 160, 255); G = (255, 200, 86, 255); GM = (234, 170, 58, 255); GD = (160, 98, 34, 255)
CX = CY = 16.0

# 갈래 색 (PathDef.color 와 같은 계열) — 밝음 · 기본 · 중간 · 어두움
PATH = [
    ((255, 140, 150, 255), (230, 52, 76, 255), (176, 32, 56, 255), (110, 18, 40, 255)),        # 0 출혈
    ((206, 182, 255, 255), (140, 102, 216, 255), (100, 70, 170, 255), (62, 42, 112, 255)),     # 1 그림자
    ((214, 248, 255, 255), (128, 216, 242, 255), (72, 162, 204, 255), (38, 102, 150, 255)),    # 2 회귀 칼날
    ((255, 236, 170, 255), (242, 192, 76, 255), (200, 146, 46, 255), (138, 92, 26, 255)),      # 3 질주 암살
    ((255, 196, 140, 255), (255, 116, 52, 255), (210, 72, 30, 255), (140, 40, 20, 255)),       # 4 광란
]
GOLD = (GL, G, GM, GD)


def medallion(img, rim, rim2=None):
    # 테: 반지름 12.4~15, 위 왼쪽이 밝게 · rim2 가 있으면 오른쪽 반은 그 색 (교차)
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - CX, y + 0.5 - CY
            d = math.hypot(dx, dy)
            if d > 15.0:
                continue
            if d >= 12.4:
                cols = rim2 if rim2 is not None and dx > 0 else rim
                lit = (-dx - dy) / (d * 1.414)
                c = cols[0] if lit > 0.75 else cols[1] if lit > 0.15 else cols[2] if lit > -0.6 else cols[3]
                if 13.1 < d < 13.9 and lit < 0.6:
                    c = cols[2] if c in (cols[0], cols[1]) else cols[3]
                img.set(x, y, c)
            else:
                k = (dy + d * 0.3) / 12.4
                img.set(x, y, IN1 if k < -0.25 else IN2 if k < 0.45 else IN3)
    img.set(8, 6, W); img.set(7, 7, W)


def poly(img, pts, c):
    # 다각형 칠하기 (점 안 판정)
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    for y in range(int(min(ys)) - 1, int(max(ys)) + 2):
        for x in range(int(min(xs)) - 1, int(max(xs)) + 2):
            px, py = x + 0.5, y + 0.5
            inside = False
            j = len(pts) - 1
            for i in range(len(pts)):
                xi, yi = pts[i]; xj, yj = pts[j]
                if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi) + xi:
                    inside = not inside
                j = i
            if inside:
                img.blend(x, y, c)


def star(img, cx, cy, r, rot=0.0, cols=(SL, S, SD)):
    # 네 날 표창: 날마다 밝은 쪽 · 어두운 쪽 반씩 (빛은 위 왼쪽) + 가운데 구멍
    for i in range(4):
        a = rot + i * math.pi / 2
        tip = (cx + math.cos(a) * r, cy + math.sin(a) * r)
        l = (cx + math.cos(a + math.pi / 4) * r * 0.42, cy + math.sin(a + math.pi / 4) * r * 0.42)
        rr = (cx + math.cos(a - math.pi / 4) * r * 0.42, cy + math.sin(a - math.pi / 4) * r * 0.42)
        lit = math.cos(a + math.pi * 0.75) > -0.2           # 위 왼쪽을 향한 날이 밝음
        poly(img, [(cx, cy), l, tip], cols[0] if lit else cols[1])
        poly(img, [(cx, cy), tip, rr], cols[1] if lit else cols[2])
    disc(img, cx, cy, max(0.9, r * 0.17), IN3)


def drop(img, cx, cy, s=1.0):
    # 핏방울 (위가 뾰족)
    poly(img, [(cx, cy - 3.2 * s), (cx + 1.9 * s, cy + 0.4 * s), (cx - 1.9 * s, cy + 0.4 * s)], BL)
    disc(img, cx, cy + 0.6 * s, 1.95 * s, BL)
    disc(img, cx + 0.5 * s, cy + 1.1 * s, 1.0 * s, BLD)
    img.set(int(cx - 0.6 * s), int(cy), BLL)


def hood(img, cx, cy, cols, alpha=255):
    # 두건 쓴 도적 실루엣: 뾰족한 두건 + 어깨 망토 + 어두운 얼굴 틈에 빛나는 눈
    c0, c1, c2 = [(c[0], c[1], c[2], alpha) for c in cols]
    poly(img, [(cx - 7, cy + 9.5), (cx - 4.5, cy + 1.5), (cx + 4.5, cy + 1.5), (cx + 7, cy + 9.5)], c2)
    poly(img, [(cx - 6, cy + 9.5), (cx - 4, cy + 2), (cx - 1.5, cy + 2), (cx - 2.5, cy + 9.5)], c1)
    poly(img, [(cx - 1.5, cy - 10), (cx + 4.8, cy - 2.5), (cx + 5.2, cy + 3), (cx - 5.2, cy + 3), (cx - 4.8, cy - 2.5)], c1)
    line(img, cx - 1.6, cy - 9, cx - 4.6, cy - 2.4, c0, 1.0)
    disc(img, cx + 0.6, cy - 0.6, 2.7, (16, 10, 30, alpha))
    if alpha == 255:
        img.set(int(cx - 0.5), int(cy - 1), (255, 236, 250, 255))
        img.set(int(cx + 1.8), int(cy - 1), (255, 236, 250, 255))


def flame(img, cx, cy, h, cols):
    # 위로 솟는 불길 (눈물 모양 세 겹 + 양옆 불 혀)
    for w, c in ((1.0, cols[3]), (0.7, cols[1]), (0.4, cols[0])):
        r = 6.0 * w
        disc(img, cx, cy - r * 0.6, r, c)
        poly(img, [(cx - r, cy - r * 0.6), (cx + r, cy - r * 0.6), (cx + r * 0.25, cy - h * w), (cx - r * 0.35, cy - h * 0.8 * w)], c)
        poly(img, [(cx - r * 0.95, cy - r * 0.4), (cx - r * 0.2, cy - r), (cx - r * 1.35, cy - h * 0.55 * w)], c)
        poly(img, [(cx + r * 0.95, cy - r * 0.4), (cx + r * 0.2, cy - r), (cx + r * 1.3, cy - h * 0.62 * w)], c)


def speed_lines(img, cols):
    for i, (y, x0, x1) in enumerate(((10, 6, 15), (14, 4, 13), (18, 5, 12), (22, 7, 14))):
        line(img, x0, y, x1, y, cols[2] if i % 2 else cols[1], 1.1)
        img.set(x1, y, cols[0])


def dagger(img, x0, y0, x1, y1):
    # 손잡이(어두운 금) → 날(은빛), 오른쪽 위로 찌름
    line(img, x0, y0, x0 + (x1 - x0) * 0.28, y0 + (y1 - y0) * 0.28, GD, 2.2)
    mx, my = x0 + (x1 - x0) * 0.3, y0 + (y1 - y0) * 0.3
    line(img, mx - 2.6, my - 2.6 + 5.2, mx + 2.6, my + 2.6 - 5.2, G, 1.6)       # 코등이
    line(img, mx, my, x1, y1, S, 2.6)
    line(img, mx + 0.4, my - 0.6, x1, y1, SL, 1.0)
    disc(img, x0, y0, 1.4, GM)


# ------------------------------------------------------------------ 갈래 상징
def sym_bleed(img):
    star(img, 14.5, 14.0, 9.2, rot=0.35)
    drop(img, 21.5, 21.0, 1.35)
    drop(img, 11.5, 23.5, 0.95)
    img.set(19, 11, BL); img.set(20, 12, BLD); img.set(12, 17, BL)


def sym_shadow(img):
    c = PATH[1]
    hood(img, 11.5, 16.0, (c[0], c[1], c[2]), 120)         # 남은 미끼 분신 (옅게)
    hood(img, 18.0, 15.5, ((230, 214, 255, 255), c[1], c[2]))
    for i, (x, y) in enumerate(((7, 22), (9, 24), (6, 19))):
        img.blend(x, y, (c[0][0], c[0][1], c[0][2], 170))


def sym_return(img):
    c = PATH[2]
    # 휘어 돌아오는 궤적 (위 왼쪽에서 아래로 돌아 화살촉)
    for t in range(0, 34):
        a = math.radians(200 + t * 7.5)
        x = 16 + math.cos(a) * 8.6; y = 16.5 + math.sin(a) * 7.2
        disc(img, x, y, 1.0, c[1] if t % 6 < 4 else c[2])
    poly(img, [(7.0, 10.5), (11.5, 8.0), (10.6, 13.6)], c[0])
    star(img, 20.5, 19.5, 5.6, rot=0.3)
    img.set(9, 11, W)


def sym_dash(img):
    speed_lines(img, PATH[3])
    dagger(img, 11, 23, 25, 8)
    img.set(25, 8, W); img.set(24, 9, SL)


def sym_frenzy(img):
    flame(img, 16, 23.5, 19, PATH[4])
    star(img, 16, 17.0, 7.8, rot=0.35)
    star(img, 8.0, 9.5, 3.0, rot=0.1)
    star(img, 24.0, 9.5, 3.0, rot=0.6)


def sym_shadowbleed(img):
    c = PATH[1]
    hood(img, 13.5, 16.0, ((230, 214, 255, 255), c[1], c[2]))
    star(img, 22.5, 10.5, 4.4, rot=0.3)
    drop(img, 22.5, 20.5, 1.35)


def sym_stormdash(img):
    flame(img, 19, 22, 13, PATH[4])
    speed_lines(img, PATH[3])
    dagger(img, 12, 22, 25, 9)


SYMS = [sym_bleed, sym_shadow, sym_return, sym_dash, sym_frenzy]


def gems(img, n, col):
    # 테 위쪽 보석 (공명 I = 1 · II = 2 · 극의 = 3)
    spots = {1: [(16, 2)], 2: [(12, 2.6), (20, 2.6)], 3: [(10, 3.4), (16, 2), (22, 3.4)]}[n]
    for x, y in spots:
        disc(img, x, y + 0.5, 2.0, K)
        disc(img, x, y + 0.5, 1.4, col)
        img.set(int(x - 1), int(y), W)


def outline(img):
    src = [[img.px[y][x] for x in range(img.w)] for y in range(img.h)]
    for y in range(img.h):
        for x in range(img.w):
            if src[y][x][3] > 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < img.w and 0 <= yy < img.h and src[yy][xx][3] > 0:
                    img.px[y][x] = K
                    break


def save(img, name, tpl):
    path = os.path.join(OUT, name + '.png')
    meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
    # 메타를 먼저 (편집기가 그림을 먼저 보면 기본 설정 메타를 만들어 버림) · 이미 있으면 guid 를 지킴
    if not os.path.exists(path + '.meta'):
        retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta))
    retry(lambda: write(img, path))
    print('wrote', name)


def main():
    os.makedirs(OUT, exist_ok=True)
    tpl = open(META, encoding='utf-8').read()
    for p, sym in enumerate(SYMS):
        for suffix, rim, n in (('', PATH[p], 0), ('_r1', PATH[p], 1), ('_r2', PATH[p], 2), ('_m', GOLD, 3)):
            img = Img(32, 32)
            medallion(img, rim)
            sym(img)
            outline(img)
            if n:
                gems(img, n, GL if n == 3 else PATH[p][0])
            save(img, 'rogue_%d%s' % (p, suffix), tpl)
    for k, (a, b, sym) in ((2, (1, 0, sym_shadowbleed)), (3, (3, 4, sym_stormdash))):
        img = Img(32, 32)
        medallion(img, PATH[a], PATH[b])
        sym(img)
        outline(img)
        save(img, 'rogue_x%d' % k, tpl)


if __name__ == '__main__':
    main()
