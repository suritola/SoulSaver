# 영혼 트리 칸 아이콘 (1.0.6~): 칸마다 다른 그림, 32x32, 배경 없이 남보라 외곽선 + 5단 명암
#   (그래픽 개편 2단계 · 2026-10-25) 가지 색 반투명 빛 테두리(halo)를 없애고 가지 표시는 영혼 트리 칸 테(SoulTreeUI)로 옮김,
#   반짝임은 본체에 붙은 불투명 1개만, 마무리는 restyle_icons.finish (알파 0/255 · 떠 있는 섬 · 외곽 · 색 ≤ 16) — 아이콘 화소 규칙 통일
#   → Assets/Resources/Icons/tree_<칸 키>.png   (칸 키의 '.' 은 '_' 로: w.dmg1 → tree_w_dmg1)
#   여러 단계 칸(무기 화력 I~V 등)은 같은 그림에 아래쪽 단계 구슬(1~5개)로 칸마다 다르게
#   tree_mark_<이름>.png: 무기 숙련 · 패시브 진화 칸에 무기 · 능력 그림 위로 겹쳐 그리는 작은 표식
#   가지 색: 무기 주황 · 필살기 하늘 · 운명 파랑 · 생존 초록 · 영혼 보라 · 재물 금빛 (SoulTreeUI.BranchColor)
# 실행: py tools/pixelart/make_tree_icons.py   (미리 보기: --preview <출력.png>)
import math, os, re, sys, uuid, time
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from restyle_icons import finish

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Icons')
N = 32
K = (24, 17, 38, 255)               # 남보라 외곽선

# ------------------------------------------------------------------ 색
def mix(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3)) + (255,)


def ramp(base):
    # 5단 명암: 그림자는 보랏빛으로, 밝은 쪽은 따뜻한 흰빛으로 색을 틀어 줌
    return [mix(base, (34, 20, 62), 0.62), mix(base, (52, 30, 84), 0.33), mix(base, base, 0),
            mix(base, (255, 248, 226), 0.38), mix(base, (255, 255, 246), 0.74)]


STEEL = (172, 182, 204); BLADE = (214, 222, 238); BRASS = (226, 168, 66); GOLD = (246, 198, 72)
COPPER = (206, 116, 66); FIRE = (250, 112, 40); FLAME = (255, 196, 72); EMBER = (255, 236, 150)
RED = (218, 56, 72); BLOOD = (176, 30, 52); WOOD = (138, 86, 52); LEATHER = (124, 78, 50)
GREEN = (86, 196, 98); LEAF = (110, 214, 92); CYAN = (104, 214, 255); ICE = (196, 240, 255)
BLUE = (84, 132, 236); VIOLET = (160, 104, 238); LILAC = (206, 172, 255); BONE = (232, 226, 238)
PAPER = (238, 224, 190); DARK = (58, 46, 84); VOID = (30, 18, 50); WHITE = (250, 248, 255)
SHIELD = (120, 150, 196); TEAL = (70, 190, 170)

BRANCH = {      # 가지 색 (반짝임 · 단계 구슬)
    'w': (255, 158, 76), 'u': (128, 230, 255), 'f': (115, 190, 255),
    's': (140, 242, 128), 'o': (184, 148, 255), 'g': (255, 216, 90), 'm': (255, 214, 120),
}


# ------------------------------------------------------------------ 모양 (칸 좌표 집합)
def m_disc(cx, cy, r):
    return {(x, y) for y in range(N) for x in range(N) if (x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2 <= r * r}


def m_ellipse(cx, cy, rx, ry):
    return {(x, y) for y in range(N) for x in range(N) if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1}


def m_rect(x0, y0, x1, y1):
    return {(x, y) for y in range(max(0, y0), min(N, y1 + 1)) for x in range(max(0, x0), min(N, x1 + 1))}


def m_poly(pts):
    s = set()
    for y in range(N):
        for x in range(N):
            px, py = x + 0.5, y + 0.5
            inside = False
            j = len(pts) - 1
            for i in range(len(pts)):
                xi, yi = pts[i]; xj, yj = pts[j]
                if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi) + xi:
                    inside = not inside
                j = i
            if inside: s.add((x, y))
    return s


def m_line(x0, y0, x1, y1, w):
    s = set()
    dx, dy = x1 - x0, y1 - y0
    L2 = dx * dx + dy * dy or 1e-9
    for y in range(N):
        for x in range(N):
            px, py = x + 0.5, y + 0.5
            t = max(0.0, min(1.0, ((px - x0) * dx + (py - y0) * dy) / L2))
            if (px - x0 - t * dx) ** 2 + (py - y0 - t * dy) ** 2 <= (w / 2) ** 2: s.add((x, y))
    return s


def m_arc(cx, cy, r, w, a0, a1):
    # 각도는 도(°), 0 = 오른쪽, 90 = 아래 (화면 좌표)
    s = set()
    for y in range(N):
        for x in range(N):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            if abs(math.hypot(dx, dy) - r) > w / 2: continue
            a = math.degrees(math.atan2(dy, dx))
            if a0 <= a1:
                ok = any(a0 <= a + k * 360 <= a1 for k in (-1, 0, 1))
            else:
                ok = False
            if ok: s.add((x, y))
    return s


def m_path(pts, w):
    s = set()
    for (a, b), (c, d) in zip(pts, pts[1:]): s |= m_line(a, b, c, d, w)
    return s


def m_heart(cx, cy, r):
    # r = 반쪽 둥근 부분 반지름
    s = m_disc(cx - r * 0.95, cy, r) | m_disc(cx + r * 0.95, cy, r)
    s |= m_poly([(cx - r * 1.95, cy + 0.4), (cx + r * 1.95, cy + 0.4), (cx, cy + r * 2.5)])
    return s


def m_flame(cx, by, h, w):
    # 위로 타오르는 불꽃 (아래 둥글고 끝이 살짝 휨)
    s = m_ellipse(cx, by - w * 0.85, w, w * 0.9)
    s |= m_poly([(cx - w, by - w * 0.8), (cx - w * 0.55, by - h * 0.55), (cx + w * 0.15, by - h),
                 (cx + w * 0.35, by - h * 0.6), (cx + w * 0.75, by - h * 0.7), (cx + w, by - w * 0.8)])
    return s


def m_star(cx, cy, r_out, r_in, n, rot=-90):
    pts = []
    for k in range(n * 2):
        r = r_out if k % 2 == 0 else r_in
        a = math.radians(rot + k * 180 / n)
        pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    return m_poly(pts)


def m_arrowhead(x, y, ang, size):
    # (x, y) 가 화살 끝, ang = 가리키는 방향 (도)
    a = math.radians(ang)
    bx, by = x - math.cos(a) * size, y - math.sin(a) * size
    px, py = -math.sin(a) * size * 0.75, math.cos(a) * size * 0.75
    return m_poly([(x + math.cos(a) * 0.6, y + math.sin(a) * 0.6), (bx + px, by + py), (bx - px, by - py)])


def dilate(s, diag=False):
    out = set(s)
    nb = ((1, 0), (-1, 0), (0, 1), (0, -1)) + (((1, 1), (-1, -1), (1, -1), (-1, 1)) if diag else ())
    for x, y in s:
        for dx, dy in nb:
            if 0 <= x + dx < N and 0 <= y + dy < N: out.add((x + dx, y + dy))
    return out


# ------------------------------------------------------------------ 칠하기
class Canvas:
    def __init__(self):
        self.img = Img(N, N)
        self.sparks = []

    def paint(self, m, base, outline=True, light=0.0):
        # 왼쪽 위에서 빛: 위 · 왼쪽 가장자리는 한 단 밝게, 아래 · 오른쪽 가장자리는 한 단 어둡게, 안쪽은 위치에 따라
        if not m: return
        if outline:
            for p in dilate(m) - m: self.img.set(p[0], p[1], K)
        rp = ramp(base) if isinstance(base, tuple) else base
        xs = [p[0] for p in m]; ys = [p[1] for p in m]
        cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
        R = max(2.0, (max(xs) - min(xs) + max(ys) - min(ys)) / 4)
        for x, y in m:
            t = (-(x - cx) * 0.55 - (y - cy) * 0.85) / R + light
            lv = 2 + (1 if t > 0.55 else -1 if t < -0.55 else 0)
            if (x, y + 1) not in m or (x + 1, y) not in m: lv -= 1
            if (x, y - 1) not in m or (x - 1, y) not in m: lv += 1
            if (x, y + 1) not in m and (x + 1, y + 1) not in m and (x + 1, y) not in m: lv -= 1
            self.img.set(x, y, rp[max(0, min(4, lv))])

    def flat(self, m, c, outline=False):
        if outline:
            for p in dilate(m) - m: self.img.set(p[0], p[1], K)
        c = c if len(c) == 4 else c + (255,)
        for x, y in m: self.img.set(x, y, c)

    def px(self, x, y, c):
        self.img.set(x, y, c if len(c) == 4 else c + (255,))

    def sparkle(self, x, y, c, big=False):
        # 반짝임: 흰 심 + 가지 색 십자 — 바로 칠하지 않고 모아 뒀다가 finish_sparkles 에서 본체 위에 있는 것 하나만 (떠 있는 반짝이 금지)
        self.sparks.append((x, y, c))

    def finish_sparkles(self):
        for x, y, c in self.sparks:
            if self.img.get(x, y)[3] == 0: continue
            self.px(x, y, WHITE)
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if self.img.get(x + dx, y + dy)[3] > 0: self.px(x + dx, y + dy, c)
            break

    def pips(self, n, c):
        # 단계 구슬: 아래 가운데 마름모 n 개
        if n <= 0: return
        gap = 4
        x0 = 16 - (n - 1) * gap / 2
        for i in range(n):
            x = int(round(x0 + i * gap)) - 1
            m = {(x, 28), (x - 1, 29), (x, 29), (x + 1, 29), (x, 30)}
            for p in dilate(m) - m: self.img.set(p[0], p[1], K)
            for p in m: self.px(p[0], p[1], mix(c, (255, 255, 255), 0.25))
            self.px(x, 29, WHITE)
            self.px(x, 30, mix(c, (40, 20, 70), 0.35))


# ------------------------------------------------------------------ 자주 쓰는 부품
def sword(cv, x0, y0, x1, y1, blade=BLADE, w=3.2, guard=GOLD):
    # (x0, y0) 손잡이 쪽 → (x1, y1) 칼끝
    ang = math.atan2(y1 - y0, x1 - x0)
    ux, uy = math.cos(ang), math.sin(ang)
    tip = m_poly([(x0 + ux * 2 - uy * w / 2, y0 + uy * 2 + ux * w / 2), (x1 - ux * 2 - uy * w / 2, y1 - uy * 2 + ux * w / 2),
                  (x1 + ux * 0.8, y1 + uy * 0.8), (x1 - ux * 2 + uy * w / 2, y1 - uy * 2 - ux * w / 2), (x0 + ux * 2 + uy * w / 2, y0 + uy * 2 - ux * w / 2)])
    hx, hy = x0 - ux * 4.5, y0 - uy * 4.5
    cv.paint(m_line(x0 - ux * 0.5, y0 - uy * 0.5, hx, hy, 2.2), LEATHER)
    cv.paint(tip, blade)
    # 날 가운데 홈 (밝은 줄)
    for p in m_line(x0 + ux * 3, y0 + uy * 3, x1 - ux * 3.5, y1 - uy * 3.5, 0.9): cv.px(p[0], p[1], WHITE)
    cv.paint(m_line(x0 + uy * 3.6, y0 - ux * 3.6, x0 - uy * 3.6, y0 + ux * 3.6, 2.2), guard)
    cv.paint(m_disc(hx - ux * 0.8, hy - uy * 0.8, 1.7), guard)


def bullet_h(cv, x, y, length=10, h=5, tip=COPPER, case=BRASS):
    # 오른쪽을 가리키는 총알 (x = 꽁무니, y = 가운데)
    body = m_rect(x, int(y - h / 2 + 0.5), x + length - 5, int(y + h / 2 - 0.5))
    nose = m_ellipse(x + length - 5, y, 5, h / 2) - m_rect(0, 0, x + length - 6, N)
    cv.paint(body | nose, case)
    cv.paint(nose, tip, outline=False)
    for yy in range(int(y - h / 2 + 0.5), int(y + h / 2 + 0.5)): cv.px(x + 1, yy, mix(case, (60, 30, 80), 0.4))


def bullet_v(cv, x, top, h=18, w=4, tip=COPPER, case=BRASS):
    nose = m_ellipse(x + w / 2, top + 3, w / 2, 3) - m_rect(0, top + 3, N, N)
    body = m_rect(x, top + 3, x + w - 1, top + h)
    cv.paint(body | nose, case)
    cv.paint(nose, tip, outline=False)
    for xx in range(x, x + w): cv.px(xx, top + h - 1, mix(case, (60, 30, 80), 0.45))


def orb(cv, cx, cy, r, glass=(150, 170, 230), fill=None, level=0.5, liquid=CYAN):
    # 유리 구슬 (안에 차오르는 빛)
    cv.paint(m_disc(cx, cy, r), mix(glass, (30, 20, 60), 0.55)[:3])
    if fill is None:
        top = cy + r - 2 * r * level
        liq = {p for p in m_disc(cx, cy, r - 1) if p[1] + 0.5 >= top + math.sin(p[0] * 0.9) * 0.8}
        cv.paint(liq, liquid, outline=False, light=0.3)
    for p in m_arc(cx, cy, r - 1.2, 1, 190, 250): cv.px(p[0], p[1], WHITE)
    cv.px(int(cx - r * 0.45), int(cy - r * 0.55), WHITE)


def arrow_up(cv, x, y, c, size=4):
    cv.paint(m_line(x, y + size + 3, x, y + 1.5, 2.2) | m_arrowhead(x, y - 1, -90, size), c)


def coin(cv, cx, cy, r, side=False):
    if side:
        cv.paint(m_ellipse(cx, cy, r, r * 0.42), GOLD)
        for p in m_ellipse(cx, cy + 1, r, r * 0.42) - m_ellipse(cx, cy, r, r * 0.42): cv.px(p[0], p[1], mix(GOLD, (90, 40, 30), 0.5))
        return
    cv.paint(m_disc(cx, cy, r), GOLD)
    for p in m_arc(cx, cy, r - 1.6, 0.9, -180, 180): cv.px(p[0], p[1], mix(GOLD, (120, 60, 30), 0.35))
    cv.px(int(cx - r * 0.4), int(cy - r * 0.45), WHITE)


def skull(cv, cx, cy, s=1.0, bone=BONE, eye=VOID):
    cv.paint(m_disc(cx, cy, 6 * s) | m_rect(int(cx - 3.5 * s), int(cy + 2 * s), int(cx + 3.5 * s - 0.5), int(cy + 6 * s)), bone)
    for ex in (-2.5, 2.5): cv.flat(m_disc(cx + ex * s, cy + 0.5 * s, 1.7 * s), eye)
    cv.px(int(cx), int(cy + 3 * s), K)
    for tx in (-2, 0, 2): cv.px(int(cx + tx * s), int(cy + 5.5 * s), K)


# ------------------------------------------------------------------ 무기 가지
def w_dmg(cv):                          # 무기 화력: 불꽃에 휩싸인 칼
    cv.paint(m_flame(20, 24, 22, 6.5), FIRE)
    cv.paint(m_flame(20.5, 23, 15, 4), FLAME, outline=False)
    cv.paint(m_flame(11, 22, 13, 4), FIRE)
    cv.paint(m_flame(11.3, 21, 8, 2.4), FLAME, outline=False)
    sword(cv, 9, 22, 25, 5)
    cv.sparkle(27, 12, BRANCH['w']); cv.px(6, 9, EMBER); cv.px(15, 4, EMBER)


def w_rate(cv):                         # 연사 강화: 줄지어 날아가는 총알 셋
    for x, y in ((15, 7), (19, 15), (13, 23)):
        for ln, dy, c in ((5, -1, FIRE), (8, 0, FLAME), (4, 1, FIRE)):
            cv.flat(m_line(x - 2 - ln, y + dy + 0.5, x - 2, y + dy + 0.5, 1), c)
        bullet_h(cv, x, y, 11, 5)
    cv.sparkle(29, 8, BRANCH['w'])


def w_mag(cv):                          # 큰 탄창: 가죽 띠로 묶은 탄환 다발
    for i, x in enumerate((6, 11, 16, 21)):
        bullet_v(cv, x, 5 + (i % 2) * 2, 20 - (i % 2) * 2, 4)
    cv.paint(m_rect(5, 16, 25, 18), LEATHER)
    cv.paint(m_rect(14, 15, 17, 19), GOLD)
    cv.px(15, 17, K)
    cv.sparkle(28, 6, BRANCH['w'])


def w_reload(cv):                       # 재빠른 장전: 탄창 둘레를 도는 화살표
    cv.paint(m_arc(16, 16, 12, 2.4, -200, 40), FLAME)
    cv.paint(m_arrowhead(23.5, 26, 120, 4.5), FLAME)
    cv.paint(m_poly([(11, 9), (19, 9), (21, 24), (13, 24)]), (88, 92, 118))
    for x in (13, 15, 17): bullet_v(cv, x, 5, 4, 2)
    for y in (13, 17, 21): cv.flat(m_line(13.5, y, 19.5, y, 1), (60, 60, 90))
    cv.sparkle(5, 8, BRANCH['w'])


def w_crit(cv):                         # 급소 사격: 붉은 과녁 조준선
    cv.paint(m_arc(16, 15, 10.5, 2.2, -180, 180) - m_rect(14, 0, 17, N) - m_rect(0, 13, N, 16), BLADE)
    for a, b, c, d in ((16, 1.5, 16, 8), (16, 22, 16, 28.5), (2, 15, 9, 15), (23, 15, 30, 15)):
        cv.paint(m_line(a, b, c, d, 1.6), BRANCH['w'])
    cv.paint(m_disc(16, 15, 5), RED)
    cv.flat(m_arc(16, 15, 3, 1, -180, 180), (255, 200, 200))
    cv.flat(m_disc(16, 15, 1.2), WHITE)


def w_critdmg(cv):                      # 처참한 일격: 갈라지는 붉은 충격
    cv.paint(m_star(16, 16, 15, 7, 9, -80), RED)
    cv.paint(m_star(16, 16, 10, 5, 9, -60), FIRE, outline=False)
    cv.paint(m_star(16, 16, 6, 3, 7, -90), FLAME, outline=False)
    cv.flat(m_disc(16, 16, 2), EMBER)
    for path in (((16, 16), (11, 11), (9, 6)), ((16, 16), (21, 20), (25, 21)), ((16, 16), (19, 10)), ((16, 16), (12, 22))):
        cv.flat(m_path(path, 1), K)


def w_pene(cv):                         # 철갑탄: 두 장의 판을 꿰뚫는 총알
    hole = m_rect(0, 14, N, 18)
    for x in (10, 18):
        cv.paint(m_poly([(x, 5), (x + 3, 4), (x + 3, 27), (x, 28)]) - hole, STEEL)
    bullet_h(cv, 3, 16, 26, 5, tip=BLADE, case=BRASS)
    for x, y in ((14, 11), (15, 21), (22, 12), (23, 20), (25, 10)): cv.px(x, y, (220, 225, 240))
    cv.sparkle(28, 23, BRANCH['w'])


def w_speed(cv):                        # 고속탄: 불꼬리를 끄는 총알 (오른쪽 위로)
    for w, c, a, b in ((9, FIRE, 1, 31), (5.5, FLAME, 4, 28), (2.4, EMBER, 7, 25)):
        cv.flat(m_poly([(a, b), (14 - w * 0.5, 13 + w * 0.2), (19 + w * 0.2, 18 + w * 0.5)]), c)
    body = m_line(14, 18, 21, 11, 7)
    cv.paint(body, BRASS)
    nose = m_line(21, 11, 25.5, 6.5, 6.2) | m_disc(25.5, 6.5, 3.1)
    cv.paint(nose - body, COPPER)
    cv.flat(m_line(13, 15, 19, 9, 1), (255, 230, 160))
    cv.flat(m_line(15, 21.5, 21.5, 15, 1), mix(BRASS, (60, 30, 80), 0.5))
    for a, b in ((5, 10), (20, 27)): cv.flat(m_line(a, b, a + 6, b - 6, 1.2), BRANCH['w'])
    cv.sparkle(29, 14, BRANCH['w'])


def w_exec(cv):                         # 처형: 피 묻은 처형 도끼
    cv.paint(m_line(6, 29, 18, 4, 2.8), WOOD)
    head = m_poly([(15, 6), (20, 6), (24, 2), (28, 4), (30, 10), (29, 16), (26, 21), (22, 22), (21, 15), (15, 14)])
    cv.paint(head, STEEL)
    cv.flat(m_path([(24, 3), (28, 5), (29.5, 10), (28.5, 16), (26, 20.5)], 1.2), WHITE)
    cv.paint(m_rect(14, 6, 19, 14), (96, 96, 122))
    cv.px(16, 9, (180, 180, 200)); cv.px(16, 12, (180, 180, 200))
    cv.flat(m_line(27, 17, 25, 22, 1.3), BLOOD)
    cv.paint(m_disc(25, 26.5, 2.1) | m_poly([(23, 26), (27, 26), (25, 22.5)]), RED)
    cv.px(24, 26, (255, 170, 170))
    cv.sparkle(7, 6, BRANCH['w'])


def w_reach(cv):                        # 긴 칼날: 길게 뻗은 칼 + 길이 화살표
    sword(cv, 6, 25, 29, 2, w=2.8)
    cv.paint(m_line(14, 29, 29, 14, 1.4), FLAME)
    cv.paint(m_arrowhead(30, 13, -45, 3.4) | m_arrowhead(13, 30, 135, 3.4), FLAME)
    cv.sparkle(4, 5, BRANCH['w'], True)


def w_arc(cv):                          # 넓은 베기: 크게 휘두른 반달 칼바람
    cres = m_disc(15, 17, 14) - m_disc(19, 21, 12.5)
    cv.paint(cres, ICE)
    cv.paint((m_disc(15, 17, 12.2) - m_disc(18.5, 20.5, 11.5)) & cres, WHITE, outline=False)
    for a, b, c, d in ((7, 24, 3, 28), (10, 27, 7, 30)): cv.flat(m_line(a, b, c, d, 1), BRANCH['w'])
    sword(cv, 22, 24, 28, 18, w=2.6)
    cv.sparkle(26, 5, BRANCH['w'])


def w_blast(cv):                        # 넓은 폭발: 터지는 플라스크 + 사방으로 넓어지는 화살표
    cv.paint(m_star(16, 17, 12.5, 7, 8, -90), FIRE)
    cv.paint(m_star(16, 17, 8.5, 5, 8, -68), FLAME, outline=False)
    cv.paint(m_rect(14, 7, 17, 12), (196, 220, 236))
    cv.paint(m_rect(13, 5, 18, 7), WOOD)
    body = m_disc(15.5, 18, 6)
    cv.paint(body, (196, 220, 236))
    cv.paint({p for p in body if p[1] >= 18}, (120, 230, 110), outline=False)
    cv.px(13, 15, WHITE); cv.px(12, 16, WHITE)
    for a in (-135, -45, 45, 135):
        r = math.radians(a)
        cv.paint(m_arrowhead(16 + math.cos(r) * 15.5, 17 + math.sin(r) * 15.5, a, 3.4), EMBER)


# ------------------------------------------------------------------ 필살기 가지
def u_power(cv):                        # 필살 위력: 터지는 빛의 별
    cv.flat(m_arc(16, 16, 13, 1.4, -180, 180), BRANCH['u'] + (190,))
    cv.paint(m_star(16, 16, 15, 4.5, 4, -90), CYAN)
    cv.paint(m_star(16, 16, 9, 3.5, 4, -45), ICE, outline=False)
    cv.flat(m_disc(16, 16, 3), WHITE)
    for x, y in ((6, 7), (26, 25)): cv.sparkle(x, y, BRANCH['u'])


def u_kit(cv):                          # 캐릭터 필살기 강화: 빛을 두른 주먹 + 위 화살표
    cv.paint(m_flame(13, 28, 26, 9), (60, 150, 230))
    cv.paint(m_flame(13, 27, 18, 6), CYAN, outline=False)
    cv.paint(m_rect(7, 21, 18, 26), LEATHER)
    fist = m_rect(6, 10, 19, 21) - {(6, 10), (19, 10), (6, 21)}
    cv.paint(fist, (236, 190, 150))
    for x in (9, 12, 15): cv.flat(m_line(x + 0.5, 10.5, x + 0.5, 14.5, 1), (170, 110, 90))
    cv.paint(m_rect(5, 15, 12, 18), (236, 190, 150))
    cv.flat(m_rect(7, 21, 18, 21), GOLD)
    arrow_up(cv, 25, 5, BRANCH['u'], 4.5)


def u_trait(cv):                        # 필살기 특성: 빛나는 마름모 룬
    cv.paint(m_poly([(16, 2), (28, 16), (16, 30), (4, 16)]), (70, 110, 200))
    cv.paint(m_poly([(16, 6), (24, 16), (16, 26), (8, 16)]), CYAN, outline=False)
    cv.flat(m_path([(16, 10), (16, 22)], 1.6) | m_path([(12, 14), (16, 10), (20, 14)], 1.4), WHITE)
    cv.flat(m_path([(12, 20), (16, 22), (20, 20)], 1.2), ICE)
    cv.sparkle(27, 5, BRANCH['u'], True)


def u_gauge(cv):                        # 빠른 충전: 빛이 차오르는 구슬 + 겹화살표
    orb(cv, 13, 17, 10, level=0.72, liquid=CYAN)
    for y in (4, 10):
        cv.paint(m_path([(23, y + 5), (27, y + 1), (31, y + 5)], 2.2), ICE)
    cv.sparkle(25, 22, BRANCH['u'])


def u_refund(cv):                       # 잔불: 쓰고 남은 불씨가 이는 구슬
    orb(cv, 16, 17, 11, level=0.28, liquid=(90, 150, 220))
    cv.paint(m_flame(16, 25, 12, 3.6), FIRE, outline=False)
    cv.paint(m_flame(16.2, 24.5, 7, 2), FLAME, outline=False)
    for x, y in ((11, 13), (21, 11), (18, 8)): cv.px(x, y, EMBER)
    cv.sparkle(27, 5, BRANCH['u'])


def u_slow(cv):                         # 노려보는 눈빛: 세로 동공의 눈 + 느려지는 물결
    eye = m_poly([(2, 16), (9, 9), (16, 7.5), (23, 9), (30, 16), (23, 23), (16, 24.5), (9, 23)])
    cv.paint(eye, BONE)
    cv.paint(m_disc(16, 16, 6.5) & eye, (70, 200, 230), outline=False)
    cv.flat(m_ellipse(16, 16, 1.6, 5), VOID)
    cv.px(13, 13, WHITE); cv.px(14, 13, WHITE)
    for x0 in (3, 22):
        cv.flat({p for p in m_rect(x0, 26, x0 + 7, 30) if abs(p[1] - (28 + math.sin(p[0] * 1.3) * 1.2)) < 0.7}, BRANCH['u'])
    for a, b, c, d in ((16, 1, 16, 4), (7, 3, 9, 6), (25, 3, 23, 6)): cv.flat(m_line(a, b, c, d, 1.4), BRANCH['u'])


def u_storm(cv):                        # 폭풍 사거리: 소용돌이치는 총알 고리 + 밖으로 화살표
    cv.flat(m_arc(16, 16, 8.5, 1, -180, 180), BRANCH['u'] + (150,))
    for k in range(6):
        a = math.radians(k * 60 - 90)
        x, y = 16 + math.cos(a) * 8.5, 16 + math.sin(a) * 8.5
        t = a + math.pi / 2
        cv.paint(m_line(x - math.cos(t) * 2.2, y - math.sin(t) * 2.2, x + math.cos(t) * 2.2, y + math.sin(t) * 2.2, 3), BRASS)
    for k in range(4):
        a = k * 90 + 45
        r = math.radians(a)
        cv.paint(m_arrowhead(16 + math.cos(r) * 15, 16 + math.sin(r) * 15, a, 3.2), ICE)
    cv.flat(m_disc(16, 16, 2.4), WHITE)


def boot(cv, x, y, c):
    cv.paint(m_rect(x, y, x + 6, y + 9) | m_rect(x, y + 6, x + 11, y + 11) - {(x + 11, y + 6)}, c)


def u_shadow1(cv):                      # 그림자 숙련 I · 이동 속도: 잔상을 남기며 달리는 그림자 장화
    shape = [(12, 4), (20, 4), (20, 16), (27, 18), (29, 21), (29, 25), (12, 25)]
    for i, a in enumerate((60, 120)):
        dx = -9 + i * 4.5
        cv.flat(m_poly([(x + dx, y) for x, y in shape]), (120, 90, 210, a))
    cv.paint(m_poly(shape), (96, 72, 160))
    cv.flat(m_rect(12, 23, 29, 25), (60, 44, 100))
    cv.flat(m_rect(12, 8, 20, 9), CYAN)
    cv.flat(m_line(14, 5.5, 14, 20, 1), LILAC)
    for y in (12, 18): cv.flat(m_line(1, y, 6, y, 1.2), BRANCH['u'])
    cv.sparkle(28, 8, BRANCH['u'])


def u_shadow2(cv):                      # II · 착지 둔화: 내려꽂혀 퍼지는 차가운 물결
    cv.flat(m_ellipse(16, 25, 14, 4.5) - m_ellipse(16, 25, 11.5, 3), ICE + (220,))
    cv.flat(m_ellipse(16, 25, 8, 2.4) - m_ellipse(16, 25, 5.5, 1.3), CYAN)
    cv.paint(m_line(16, 2, 16, 16, 4.5) | m_arrowhead(16, 22, 90, 6), (92, 70, 150))
    cv.flat(m_line(15, 4, 15, 15, 1), LILAC)
    for x, y in ((5, 18), (27, 17), (9, 14)): cv.sparkle(x, y, BRANCH['u'])


def u_shadow3(cv):                      # III · 거리 · 무적: 잔상을 남기는 긴 돌진
    for i, a in enumerate((50, 90, 140)):
        cv.flat(m_disc(5 + i * 5, 16, 3.8), (130, 90, 220, a))
    cv.paint(m_line(10, 16, 24, 16, 4.6) | m_arrowhead(30, 16, 0, 6.5), (92, 70, 150))
    cv.flat(m_line(12, 15, 24, 15, 1), LILAC)
    cv.paint(m_arc(16, 16, 12, 1.8, -160, -20), ICE)
    cv.sparkle(4, 6, BRANCH['u'])


def u_shadow4(cv):                      # IV · 연속 돌진: 겹쳐 달리는 두 겹 꺾쇠
    for x, c in ((5, (80, 60, 140)), (14, (120, 92, 200))):
        cv.paint(m_poly([(x, 5), (x + 5, 5), (x + 13, 16), (x + 5, 27), (x, 27), (x + 8, 16)]), c)
    cv.flat(m_path([(15, 7), (22, 16)], 1), LILAC)
    cv.sparkle(29, 6, BRANCH['u'], True)


def u_shadow5(cv):                      # V · 게이지 반환: 단검과 돌아오는 빛
    cv.paint(m_arc(16, 16, 12, 2.2, -150, 150), CYAN)
    cv.paint(m_arrowhead(5.6, 9.4, -120, 4.2), CYAN)
    sword(cv, 10, 22, 22, 10, w=2.8)
    for x, y in ((24, 25), (27, 21), (21, 28)): cv.flat(m_disc(x, y, 1.3), ICE)


# ------------------------------------------------------------------ 운명 가지
def f_thread(cv):                       # 운명의 실: 금실 실패 + 바늘
    cv.paint(m_rect(6, 6, 18, 8) | m_rect(6, 24, 18, 26), WOOD)
    cv.paint(m_rect(8, 9, 16, 23), GOLD)
    for y in range(10, 23, 2): cv.flat(m_line(8.5, y + 0.5, 16, y - 0.5, 1), mix(GOLD, (140, 70, 30), 0.35))
    cv.flat(m_path([(16, 14), (21, 12), (25, 16), (22, 21), (19, 18), (24, 9)], 1), (255, 226, 120))
    cv.paint(m_line(20, 30, 29, 3, 1.8), BLADE)
    cv.px(28, 6, VOID)
    cv.sparkle(4, 15, BRANCH['f'])


def f_rift(cv):                         # 시간의 틈: 갈라진 모래시계
    cv.paint(m_rect(7, 3, 24, 5) | m_rect(7, 26, 24, 28), WOOD)
    glass = m_poly([(9, 6), (23, 6), (17, 16), (23, 25), (9, 25), (15, 16)])
    cv.paint(glass, (180, 210, 240))
    cv.flat(m_poly([(11, 8), (21, 8), (16, 15)]) & glass, (240, 210, 140))
    cv.flat(m_poly([(10, 25), (22, 25), (16, 19)]) & glass, (240, 210, 140))
    cv.paint(m_path([(26, 4), (19, 12), (23, 16), (14, 24), (17, 27), (8, 31)], 2), CYAN)
    cv.flat(m_path([(26, 4), (19, 12), (23, 16), (14, 24)], 0.8), WHITE)
    cv.sparkle(4, 10, BRANCH['f'])


def f_nova(cv):                         # 각성의 파동: 몸에서 터지는 두 겹 충격파
    cv.flat(m_arc(16, 16, 14, 1.6, -180, 180), BRANCH['f'] + (210,))
    cv.paint(m_arc(16, 16, 10, 2.2, -180, 180), (150, 200, 255))
    cv.paint(m_star(16, 16, 7, 3, 8, -90), ICE)
    cv.flat(m_disc(16, 16, 2.2), WHITE)
    for k in range(8):
        a = math.radians(k * 45 + 22)
        cv.px(int(16 + math.cos(a) * 12.5), int(16 + math.sin(a) * 12.5), WHITE)


def f_storm(cv):                        # 연쇄 광풍: 회오리바람
    for i, (y, w) in enumerate(((5, 13), (10, 11), (15, 8.5), (20, 6), (25, 3.5))):
        cx = 16 + math.sin(i * 1.1) * 2
        cv.paint(m_ellipse(cx, y, w, 2.4) - m_ellipse(cx + 1, y + 0.5, w - 2.5, 1.2), (150, 230, 220) if i % 2 == 0 else (110, 200, 220))
    cv.flat(m_path([(17, 27), (15, 29), (17, 31)], 1.2), (150, 230, 220))
    for x, y in ((3, 13), (28, 18), (6, 25)): cv.sparkle(x, y, BRANCH['f'])


def f_well(cv):                         # 중력 우물: 빨아들이는 검은 소용돌이
    cv.paint(m_ellipse(16, 16, 14, 9), (70, 50, 140))
    for k in range(3):
        cv.flat(m_arc(16, 16, 4 + k * 3.2, 1.2, -170 + k * 50, 40 + k * 50) & m_ellipse(16, 16, 13, 8.5), (160, 130, 255))
    cv.flat(m_disc(16, 16, 3.6), VOID)
    cv.flat(m_disc(16, 16, 1.6), (0, 0, 0))
    for x, y in ((3, 4), (28, 27), (27, 5)): cv.px(x, y, LILAC)
    cv.sparkle(5, 27, BRANCH['f'])


def f_curse(cv):                        # 저주 전이: 저주받은 해골에서 다음 해골로 옮겨 가는 불씨
    cv.paint(m_flame(12, 22, 22, 9), (80, 170, 80))
    cv.paint(m_flame(12, 21, 15, 6), (140, 230, 110), outline=False)
    skull(cv, 12, 12, 1.15, eye=(150, 255, 120))
    for k in range(5):
        a = math.radians(-20 + k * 22)
        cv.paint(m_disc(14 + math.cos(a) * 11, 13 + math.sin(a) * 11, 1.2 + k * 0.15), (170, 130, 240))
    skull(cv, 25, 25, 0.68, eye=(255, 110, 90))
    cv.sparkle(27, 5, BRANCH['f'])


def f_echo(cv):                         # 영혼 메아리: 처치 자리에 남은 유령
    cv.flat(m_arc(16, 16, 14, 1.2, -180, 180), BRANCH['f'] + (130,))
    ghost = m_disc(16, 12, 8) | m_rect(8, 12, 23, 22) | m_disc(10, 23, 2.2) | m_disc(16, 23, 2.2) | m_disc(22, 23, 2.2)
    cv.paint(ghost, (206, 226, 255))
    cv.flat(m_ellipse(13, 13, 1.4, 2), VOID); cv.flat(m_ellipse(19, 13, 1.4, 2), VOID)
    cv.flat(m_ellipse(16, 18, 1.4, 1), VOID)
    cv.sparkle(27, 27, BRANCH['f']); cv.sparkle(4, 5, BRANCH['f'])


# ------------------------------------------------------------------ 생존 가지
def s_hp(cv):                           # 강인함: 반쯤 남은 심장을 막는 방패
    h = m_heart(13, 11, 5.5)
    cv.paint(h, RED)
    cv.flat({p for p in h if p[0] >= 14} - (dilate(h) - h), (80, 40, 70))
    sh = m_poly([(18, 13), (29, 13), (29, 21), (23.5, 29), (18, 21)])
    cv.paint(sh, SHIELD)
    cv.paint(m_poly([(20, 15), (27, 15), (27, 20.5), (23.5, 26), (20, 20.5)]), GREEN, outline=False)
    cv.flat(m_line(23.5, 16, 23.5, 25, 1), WHITE)
    cv.sparkle(4, 5, BRANCH['s'])


def s_regen(cv):                        # 전투 치유: 초록 빛이 오르는 심장
    cv.paint(m_heart(15, 13, 6.5), RED)
    cv.paint(m_rect(13, 13, 17, 25) | m_rect(9, 17, 21, 21), GREEN)
    cv.flat(m_rect(14, 14, 16, 24) | m_rect(10, 18, 20, 20), (190, 255, 180))
    for x, y in ((27, 9), (25, 4), (4, 6)): cv.flat(m_rect(x - 1, y, x + 1, y) | m_rect(x, y - 1, x, y + 1), BRANCH['s'])
    cv.px(10, 9, WHITE); cv.px(9, 10, WHITE)


def s_def(cv):                          # 완충: 떨어지는 피해를 받아 내는 푹신한 방석
    cush = m_ellipse(16, 22, 13.5, 6)
    cv.paint(cush, TEAL)
    for x, y in ((8, 21), (24, 21), (16, 23)): cv.flat({(x, y), (x + 1, y)}, mix(TEAL, (20, 40, 60), 0.5))
    for x, y in ((3, 18), (29, 18), (3, 26), (29, 26)): cv.paint(m_disc(x, y, 1.4), GOLD)
    cv.paint(m_poly([(13, 3), (19, 3), (19, 9), (22, 9), (16, 15), (10, 9), (13, 9)]), RED)
    cv.flat(m_rect(9, 17, 23, 17), (190, 255, 230))
    cv.sparkle(27, 6, BRANCH['s'])


def s_guard(cv):                        # 재정비: 방패와 짧은 모래시계 (맞은 뒤 무적)
    sh = m_poly([(4, 4), (22, 4), (22, 16), (13, 28), (4, 16)])
    cv.paint(sh, SHIELD)
    cv.paint(m_poly([(7, 7), (19, 7), (19, 15), (13, 23.5), (7, 15)]), (70, 170, 110), outline=False)
    cv.flat(m_line(13, 8, 13, 22, 1.2) | m_line(8, 12, 18, 12, 1.2), (200, 255, 210))
    cv.paint(m_rect(20, 18, 29, 19) | m_rect(20, 29, 29, 30), WOOD)
    g = m_poly([(21, 20), (28, 20), (25, 24.5), (28, 29), (21, 29), (24, 24.5)])
    cv.paint(g, ICE)
    cv.flat(m_poly([(21.5, 29), (27.5, 29), (24.5, 26)]) & g, (240, 210, 140))
    cv.sparkle(27, 6, BRANCH['s'])


def s_barrier(cv):                      # 영혼 보호막: 심장을 감싼 빛 방울
    cv.flat(m_disc(16, 16, 13.5), (120, 240, 210, 70))
    cv.paint(m_arc(16, 16, 13, 2, -180, 180), (150, 245, 220))
    cv.paint(m_heart(16, 14, 3.6), RED)
    cv.flat(m_arc(16, 16, 11, 1, 195, 255), WHITE)
    cv.px(9, 7, WHITE); cv.px(8, 8, WHITE)
    cv.sparkle(27, 26, BRANCH['s'])


def s_leech(cv):                        # 피의 굶주림: 송곳니에서 떨어지는 핏방울
    cv.paint(m_ellipse(16, 8, 13, 5) - m_rect(0, 9, N, N), (150, 40, 70))
    for x in (8, 24): cv.paint(m_poly([(x - 2.5, 8), (x + 2.5, 8), (x, 18)]), BONE)
    cv.paint(m_poly([(10, 8), (22, 8), (16, 11)]), BONE)
    drop = m_disc(16, 24, 4.2) | m_poly([(12.2, 23), (19.8, 23), (16, 14)])
    cv.paint(drop, RED)
    cv.px(14, 22, WHITE); cv.px(14, 23, WHITE)
    cv.sparkle(28, 25, BRANCH['s'])


def s_laststand(cv):                    # 꺾이지 않는 의지: 꽂힌 채 펄럭이는 깃발
    cv.paint(m_line(8, 3, 8, 29, 2.2), WOOD)
    cv.paint(m_disc(8, 3, 1.8), GOLD)
    flag = m_poly([(9, 5), (28, 7), (24, 11.5), (28, 16), (9, 17)])
    cv.paint(flag, GREEN)
    cv.paint(m_star(16, 11, 3.6, 1.6, 5, -90), GOLD, outline=False)
    cv.paint(m_ellipse(9, 29, 7, 2), (100, 80, 70))
    cv.sparkle(26, 24, BRANCH['s'], True)


def s_revive(cv):                       # 되살아난 영혼: 날개 단 빛나는 심장
    for sgn in (-1, 1):
        wing = m_poly([(16 + sgn * 4, 13), (16 + sgn * 14, 4), (16 + sgn * 15, 9), (16 + sgn * 13, 13),
                       (16 + sgn * 14, 16), (16 + sgn * 11, 19), (16 + sgn * 5, 19)])
        cv.paint(wing, (236, 240, 255))
        for i in range(3): cv.px(16 + sgn * (8 + i * 2), 15 - i * 2, (160, 180, 220))
    cv.paint(m_heart(16, 15, 4.4), RED)
    cv.flat(m_line(16, 15.5, 16, 22, 1.4) | m_line(13, 18, 19, 18, 1.4), EMBER)
    cv.paint(m_arc(16, 26, 5, 1.4, -180, 0), GOLD)
    cv.sparkle(16, 3, BRANCH['s'], True)


# ------------------------------------------------------------------ 영혼 가지
def o_root(cv):                         # 영혼 각성: 빛을 뿜는 영혼 결정
    for k in range(8):
        a = math.radians(k * 45 + 22.5)
        cv.flat(m_line(16 + math.cos(a) * 9, 15 + math.sin(a) * 9, 16 + math.cos(a) * 13, 15 + math.sin(a) * 13, 1), BRANCH['o'] + (200,))
    cr = m_poly([(16, 2), (23, 12), (16, 28), (9, 12)])
    cv.paint(cr, VIOLET)
    cv.flat(m_poly([(16, 3), (16, 27), (9.5, 12)]), LILAC)
    cv.flat(m_poly([(16, 4), (12, 12), (16, 12)]), (240, 226, 255))
    cv.flat(m_line(9.5, 12, 22.5, 12, 1), mix(VIOLET, (40, 20, 80), 0.3))
    cv.sparkle(26, 5, BRANCH['o'], True)


def o_harvest(cv):                      # 영혼 수확: 낫으로 거두는 영혼 결정
    cv.paint(m_line(5, 29, 22, 5, 2.4), WOOD)
    blade = (m_disc(14, 13, 12) - m_disc(10, 17, 11)) & m_rect(0, 0, N, 15)
    cv.paint(blade, (180, 170, 220))
    for p in m_arc(14, 13, 11, 1, -150, -40): cv.px(p[0], p[1], WHITE)
    for x, y, h in ((22, 18, 8), (27, 22, 6), (19, 25, 5)):
        cv.paint(m_poly([(x, y - h / 2), (x + h / 3, y), (x, y + h / 2), (x - h / 3, y)]), VIOLET)
        cv.px(int(x - 1), int(y - 1), LILAC)
    cv.sparkle(28, 12, BRANCH['o'])


# ------------------------------------------------------------------ 재물 가지
def g_coin(cv):                         # 황금 손길: 금빛 손에 떨어지는 코인
    hand = m_poly([(3, 15), (7, 14), (9, 21), (22, 21), (25, 15), (29, 14), (28, 19), (24, 26), (10, 27), (5, 23)])
    cv.paint(hand, GOLD)
    for x in (13, 17, 21): cv.flat(m_line(x, 23, x + 1, 26, 1), mix(GOLD, (120, 60, 30), 0.4))
    cv.paint(m_rect(10, 27, 23, 29), (200, 120, 60))
    coin(cv, 15, 18, 3.4)
    coin(cv, 20, 9, 3.4); coin(cv, 12, 5, 2.8); coin(cv, 27, 5, 2.4)
    cv.sparkle(5, 7, BRANCH['g'])


def g_magnet(cv):                       # 흥정: 깎인 가격표
    tag = m_poly([(4, 13), (13, 4), (28, 4), (28, 19), (19, 28)])
    cv.paint(tag, PAPER)
    cv.flat(m_disc(12, 12, 1.8), VOID)
    cv.flat(m_path([(12, 12), (6, 4), (3, 6)], 1), (200, 60, 60))
    cv.flat(m_disc(18, 11, 1.6) | m_disc(24, 18, 1.6) | m_line(24, 10, 17.5, 19.5, 1.4), RED)
    cv.paint(m_line(26, 21, 26, 26, 2.4) | m_arrowhead(26, 31, 90, 4), (90, 210, 110))
    cv.sparkle(5, 25, BRANCH['g'])


def die(cv, x, y, s, pips, rot=0):
    c, sn = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    pts = [(x + (dx * c - dy * sn) * s, y + (dx * sn + dy * c) * s) for dx, dy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    cv.paint(m_poly(pts), WHITE)
    for px, py in pips:
        cv.flat(m_disc(x + (px * c - py * sn) * s * 0.55, y + (px * sn + py * c) * s * 0.55, 1.1), RED if len(pips) == 1 else VOID)


def g_fate(cv):                         # 운명 바꾸기: 굴러가는 주사위 둘 + 다시 뽑기 화살표
    cv.paint(m_arc(16, 16, 13, 2, -170, -60), GOLD)
    cv.paint(m_arrowhead(23, 5.5, -10, 3.6), GOLD)
    die(cv, 10, 19, 6, [(0, 0)], -12)
    die(cv, 22, 20, 5, [(-1, -1), (1, 1), (0, 0), (1, -1), (-1, 1)], 18)
    cv.sparkle(5, 6, BRANCH['g'])


def g_luck(cv):                         # 행운: 네잎클로버
    cv.paint(m_path([(16, 16), (19, 24), (24, 29)], 2.2), (70, 150, 70))
    for a in (-135, -45, 45, 135):
        r = math.radians(a)
        dx, dy = math.cos(r), math.sin(r)
        px, py = -dy, dx
        cx, cy = 16 + dx * 7, 15 + dy * 7
        leaf = m_disc(cx + px * 3 + dx, cy + py * 3 + dy, 3.6) | m_disc(cx - px * 3 + dx, cy - py * 3 + dy, 3.6)
        leaf |= m_poly([(16 + dx * 0.5, 15 + dy * 0.5), (cx + px * 6 + dx, cy + py * 6 + dy), (cx - px * 6 + dx, cy - py * 6 + dy)])
        cv.paint(leaf, LEAF)
        cv.flat(m_line(16 + dx * 2, 15 + dy * 2, cx + dx * 1.5, cy + dy * 1.5, 1), mix(LEAF, (20, 60, 30), 0.45))
    cv.flat(m_disc(16, 15, 1.6), GOLD)
    cv.sparkle(28, 5, BRANCH['g'], True)


def g_exp(cv):                          # 깨달음: 빛나는 책 위로 떠오른 영혼 결정
    for k in range(5):
        a = math.radians(-160 + k * 35)
        cv.flat(m_line(16 + math.cos(a) * 6, 13 + math.sin(a) * 6, 16 + math.cos(a) * 11, 13 + math.sin(a) * 11, 1), BRANCH['g'] + (200,))
    cv.paint(m_poly([(16, 4), (20, 10), (16, 16), (12, 10)]), VIOLET)
    cv.px(14, 9, LILAC); cv.px(15, 8, LILAC)
    left = m_poly([(2, 19), (15, 21), (15, 29), (2, 27)])
    right = m_poly([(17, 21), (30, 19), (30, 27), (17, 29)])
    cv.paint(left | right, PAPER)
    cv.paint(m_rect(15, 21, 16, 29), (180, 100, 60))
    for y in (22, 24): cv.flat(m_line(4, y, 12, y + 1, 1) | m_line(20, y + 1, 28, y, 1), (150, 130, 110))


def g_interest(cv):                     # 이자: 쌓인 코인 + 초록 오름 화살표
    for i in range(4): coin(cv, 12, 26 - i * 4, 8, True)
    coin(cv, 12, 9, 6)
    cv.paint(m_path([(20, 22), (24, 16), (27, 18)], 2.2), (90, 210, 110))
    cv.paint(m_arrowhead(30, 10, -65, 4.4), (90, 210, 110))
    cv.sparkle(27, 27, BRANCH['g'])


# ------------------------------------------------------------------ 겹쳐 그리는 표식 (무기 숙련 · 패시브 진화)
def medal(cv, rim=GOLD, inner=(46, 34, 72)):
    cv.paint(m_disc(16, 16, 14), rim)
    cv.flat(m_disc(16, 16, 11), inner)


def mark_rate(cv):                      # 빠른 손: 번개
    medal(cv)
    cv.paint(m_poly([(18, 5), (10, 18), (15, 18), (13, 27), (22, 13), (17, 13), (20, 5)]), FLAME)


def mark_crit(cv):                      # 날카로운 감각 · 명사수: 조준점
    medal(cv)
    cv.flat(m_arc(16, 16, 7, 2, -180, 180) - m_rect(15, 0, 16, N) - m_rect(0, 15, N, 16), BLADE)
    cv.flat(m_line(16, 6, 16, 26, 1.6) | m_line(6, 16, 26, 16, 1.6), BLADE)
    cv.flat(m_disc(16, 16, 2.6), RED)


def mark_reload(cv):                    # 총잡이의 감: 도는 화살표
    medal(cv)
    cv.flat(m_arc(16, 16, 7, 2.6, -170, 100), CYAN)
    cv.flat(m_arrowhead(14, 24, 175, 4), CYAN)


def mark_power(cv):                     # 기본기: 불꽃
    medal(cv)
    cv.paint(m_flame(16, 25, 18, 6), FIRE, outline=False)
    cv.paint(m_flame(16.3, 24, 11, 3.4), FLAME, outline=False)


def numeral(cv, n, c):
    # 로마 숫자 I · II · III
    w = 3; gap = 2
    x0 = 16 - (n * w + (n - 1) * gap) / 2
    for i in range(n):
        x = int(round(x0 + i * (w + gap)))
        cv.paint(m_rect(x, 9, x + w - 1, 22), c)
    cv.paint(m_rect(int(x0) - 1, 7, int(round(x0 + n * w + (n - 1) * gap)), 8) | m_rect(int(x0) - 1, 23, int(round(x0 + n * w + (n - 1) * gap)), 24), c)


def mark_1(cv): medal(cv); numeral(cv, 1, GOLD)
def mark_2(cv): medal(cv); numeral(cv, 2, GOLD)
def mark_3(cv): medal(cv); numeral(cv, 3, GOLD)


def mark_memory(cv):                    # 기억: 별빛 깃털
    medal(cv, rim=(180, 150, 255))
    cv.paint(m_star(16, 16, 9, 3.6, 4, -90), (225, 210, 255), outline=False)
    cv.flat(m_disc(16, 16, 1.6), WHITE)


def mark_evolve(cv):                    # 진화: 금빛 오름 화살표
    medal(cv, rim=(255, 220, 110), inner=(80, 50, 30))
    cv.paint(m_line(16, 25, 16, 13, 4) | m_arrowhead(16, 5, -90, 7.5), (255, 226, 120))
    cv.flat(m_line(15, 22, 15, 12, 1), WHITE)


# ------------------------------------------------------------------ 칸 목록: (그리는 함수, 가지, 칸 키들)
def keys(prefix, first, last):
    return [prefix + str(i) for i in range(first, last + 1)]


SPECS = [
    (w_dmg, 'w', keys('w.dmg', 1, 5)), (w_rate, 'w', keys('w.rate', 1, 4)), (w_mag, 'w', keys('w.mag', 1, 3)),
    (w_reload, 'w', keys('w.reload', 1, 3)), (w_crit, 'w', keys('w.crit', 1, 3)), (w_critdmg, 'w', ['w.critdmg']),
    (w_pene, 'w', keys('w.pene', 1, 2)), (w_speed, 'w', ['w.speed']), (w_exec, 'w', keys('w.exec', 1, 2)),
    (w_reach, 'w', keys('w.reach', 1, 3)), (w_arc, 'w', keys('w.arc', 1, 3)), (w_blast, 'w', keys('w.blast', 1, 4)),
    (u_power, 'u', keys('u.power', 1, 4)), (u_kit, 'u', keys('u.kit', 1, 3)), (u_trait, 'u', keys('u.trait', 1, 3)),
    (u_gauge, 'u', keys('u.gauge', 1, 3)), (u_refund, 'u', keys('u.refund', 1, 2)), (u_slow, 'u', keys('u.slow', 1, 2)),
    (u_storm, 'u', keys('u.storm', 1, 2)),
    (u_shadow1, 'u', ['u.shadow1']), (u_shadow2, 'u', ['u.shadow2']), (u_shadow3, 'u', ['u.shadow3']),
    (u_shadow4, 'u', ['u.shadow4']), (u_shadow5, 'u', ['u.shadow5']),
    (f_thread, 'f', ['f.thread', 'f.thread2', 'f.thread3']), (f_rift, 'f', keys('f.rift', 1, 2)), (f_nova, 'f', keys('f.nova', 1, 2)),
    (f_storm, 'f', keys('f.storm', 1, 2)), (f_well, 'f', keys('f.well', 1, 2)), (f_curse, 'f', keys('f.curse', 1, 2)),
    (f_echo, 'f', keys('f.echo', 1, 2)),
    (s_hp, 's', keys('s.hp', 1, 3)), (s_regen, 's', keys('s.regen', 1, 3)), (s_def, 's', keys('s.def', 1, 2)),
    (s_guard, 's', keys('s.guard', 1, 2)), (s_barrier, 's', keys('s.barrier', 1, 2)), (s_leech, 's', keys('s.leech', 1, 2)),
    (s_laststand, 's', ['s.laststand']), (s_revive, 's', ['s.revive']),
    (o_root, 'o', ['o.root', 'o.root2', 'o.root3']), (o_harvest, 'o', ['o.harvest']),
    (g_coin, 'g', keys('g.coin', 1, 3)), (g_magnet, 'g', keys('g.magnet', 1, 2)), (g_fate, 'g', ['g.fate']),
    (g_luck, 'g', keys('g.luck', 1, 4)), (g_exp, 'g', keys('g.exp', 1, 3)), (g_interest, 'g', keys('g.interest', 1, 2)),
]
MARKS = {'rate': mark_rate, 'crit': mark_crit, 'reload': mark_reload, 'power': mark_power,
         '1': mark_1, '2': mark_2, '3': mark_3, 'memory': mark_memory, 'evolve': mark_evolve}


def render(fn, branch, tier):
    cv = Canvas()
    fn(cv)
    cv.pips(tier, BRANCH[branch])
    cv.finish_sparkles()
    return finish(cv.img, 16, keep_outlined=True)


def all_icons():
    out = []
    for fn, br, ks in SPECS:
        for i, k in enumerate(ks):
            out.append(('tree_' + k.replace('.', '_'), render(fn, br, i + 1 if len(ks) > 1 else 0), fn.__name__))
    for name, fn in MARKS.items():
        cv = Canvas(); fn(cv); cv.finish_sparkles()
        out.append(('tree_mark_' + name, finish(cv.img, 16, keep_outlined=True), name))
    return out


def retry(fn):
    for i in range(20):
        try:
            return fn()
        except PermissionError:
            time.sleep(0.5)
    return fn()


def preview(path, icons, scale=4, cols=12):
    # 확대 미리 보기: 어두운 칸 바탕(트리 칸과 같은 색) 위에 아이콘 격자
    cell = N * scale + 10
    rows = (len(icons) + cols - 1) // cols
    sheet = Img(cols * cell, rows * cell, (14, 10, 22, 255))
    for k, (_, im, _t) in enumerate(icons):
        ox, oy = (k % cols) * cell + 5, (k // cols) * cell + 5
        for y in range(cell - 10):
            for x in range(cell - 10): sheet.set(ox + x, oy + y, (20, 15, 31, 255))
        for y in range(N):
            for x in range(N):
                c = im.px[y][x]
                if c[3] == 0: continue
                for dy in range(scale):
                    for dx in range(scale): sheet.blend(ox + x * scale + dx, oy + y * scale + dy, c)
    write(sheet, path)


def main():
    icons = all_icons()
    if '--preview' in sys.argv:
        # 그림마다 한 장씩 (단계 구슬은 첫 칸 그림으로 확인), --all 이면 모든 칸
        if '--all' not in sys.argv:
            seen, pick = set(), []
            for ic in icons:
                if ic[2] in seen: continue
                seen.add(ic[2]); pick.append(ic)
            icons = pick
        preview(sys.argv[sys.argv.index('--preview') + 1], icons, 5, 10)
        print('미리 보기', len(icons), '장')
        return
    # 임포트 설정은 같은 폴더 아이콘(ability_105)을 그대로 따르고, guid · spriteID 만 새로
    tpl = open(os.path.join(OUT, 'ability_105.png.meta'), encoding='utf-8').read()
    for name, img, _t in icons:
        path = os.path.join(OUT, name + '.png')
        if not os.path.exists(path + '.meta'):
            meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
            meta = re.sub(r'spriteID: [0-9a-f]{32}', 'spriteID: ' + uuid.uuid4().hex, meta, count=1)
            retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta))
        retry(lambda: write(img, path))
    print('그림 저장:', len(icons), '장')


if __name__ == '__main__':
    main()
