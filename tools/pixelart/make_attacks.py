# 영웅 평타 그림 (1.1.2): 「평타가 안 보인다」 — 작고 흐린 흰 그림이라 밝은 초원 · 어두운 묘역 어디서나 묻혔음
# 모든 평타를 3단으로: 밝은 심(거의 흰색) + 영웅 고유색 테두리 + 어두운 외곽선 1칸
#
#   색을 코드에서 입히는 투사체(총알 · 표창 · 화살 · 톱니 · 플라스크)는 두 장씩:
#     atk_<이름>.png       테두리 층 — 회색 명암 + 어두운 외곽선 (SpriteRenderer 색 = 영웅색 · 형태색을 곱해 입힘)
#     atk_<이름>_core.png  심 층     — 흰 심만 (색을 입히지 않고 위에 겹침, 같은 프레임 배치)
#   색이 정해진 그림(편지)과 검사 베기(fx_swordswing · _t2 · _t3)는 색까지 그려 넣음
#
# 프레임은 가로로 이어 붙인 시트, 16칸 = 1 월드 단위 (.meta 는 make_fx.sheet_meta, 이미 있으면 그대로 = guid 유지)
# 실행: python tools/pixelart/make_attacks.py
import math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from png import Img
from make_fx2 import save_sheet

OUTLINE = (20, 14, 34, 255)          # 어두운 외곽선 (남보라, 곱해도 어두움)


# ------------------------------------------------------------------ 공통: 모양(마스크) → 층
def depth_map(w, h, inside):
    # 칠한 칸마다 가장자리에서 몇 칸 안쪽인지 (1 = 가장자리)
    m = [[inside(x + 0.5, y + 0.5) for x in range(w)] for y in range(h)]
    INF = 99
    d = [[(INF if m[y][x] else 0) for x in range(w)] for y in range(h)]
    changed = True
    while changed:
        changed = False
        for y in range(h):
            for x in range(w):
                if not m[y][x]:
                    continue
                best = INF
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    v = d[ny][nx] if 0 <= nx < w and 0 <= ny < h else 0
                    best = min(best, v + 1)
                if best < d[y][x]:
                    d[y][x] = best
                    changed = True
    return m, d


def layers(w, h, inside, core_from=3, alpha=None, rim=(150, 210, 255), core_soft=True):
    """테두리 층(회색 명암 + 외곽선)과 심 층(흰색)을 같이 그림
    rim: 가장자리 1칸 · 2칸 · 그 안쪽 회색 값, alpha(x, y): 꼬리처럼 옅어지는 곳"""
    m, d = depth_map(w, h, inside)
    body, core = Img(w, h), Img(w, h)
    for y in range(h):
        for x in range(w):
            a = 1.0 if alpha is None else max(0.0, min(1.0, alpha(x + 0.5, y + 0.5)))
            if m[y][x]:
                k = d[y][x]
                v = rim[0] if k == 1 else rim[1] if k == 2 else rim[2]
                body.set(x, y, (v, v, v, int(255 * a)))
                if k >= core_from:
                    core.set(x, y, (255, 255, 255, int(255 * a)))
                elif core_soft and k == core_from - 1:
                    core.set(x, y, (255, 255, 255, int(110 * a)))
            else:
                # 외곽선: 옆(상하좌우) 칸이 칠해진 빈칸
                near = any(0 <= x + dx < w and 0 <= y + dy < h and m[y + dy][x + dx]
                           for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
                if near and a > 0.15:
                    body.set(x, y, OUTLINE[:3] + (int(OUTLINE[3] * min(1.0, a * 1.3)),))
    return body, core


def hub(core, cx, cy, r0, r1):
    # 심 층에 흰 고리 (날이 얇아 안쪽 심이 안 생기는 표창 · 톱니)
    for y in range(core.h):
        for x in range(core.w):
            if r0 <= math.hypot(x + 0.5 - cx, y + 0.5 - cy) <= r1:
                core.set(x, y, (255, 255, 255, 255))


# ------------------------------------------------------------------ 거너 총알: 앞이 둥근 예광탄 + 뒤로 가늘어지는 꼬리 (오른쪽으로 날아감)
def bullet():
    W, H = 24, 10
    bodies, cores = [], []
    for f in range(2):
        tail0 = 1.0 if f == 0 else 2.5          # 꼬리 길이가 살짝 떨림
        def half(x):
            if x >= 15:                          # 탄두: 타원
                t = (x - 15) / 7.6
                return 3.9 * math.sqrt(max(0.0, 1 - t * t))
            if x >= tail0:                       # 꼬리: 점점 가늘게
                return 1.0 + 2.9 * ((x - tail0) / (15 - tail0)) ** 0.8
            return -1
        inside = lambda x, y: abs(y - H / 2) <= half(x)
        alpha = lambda x, y: 0.45 + 0.55 * min(1.0, x / 12)
        b, c = layers(W, H, inside, core_from=3, alpha=alpha, rim=(200, 238, 255))
        bodies.append(b); cores.append(c)
    return bodies, cores


# ------------------------------------------------------------------ 도적 표창: 휘어진 날 네 개 · 가운데 구멍 (코드에서 빙글 돎)
def shuriken():
    S = 16
    bodies, cores = [], []
    for f in range(4):
        rot = f * math.pi / 8
        def inside(x, y):
            dx, dy = x - S / 2, y - S / 2
            r = math.hypot(dx, dy)
            if r < 1.3 or r > 7.9:
                return False
            th = math.atan2(dy, dx) + rot - 0.16 * r        # 날이 휘어짐
            blade = max(0.0, math.cos(4 * th)) ** 1.6
            return r <= 3.1 + 4.8 * blade
        b, c = layers(S, S, inside, core_from=3, rim=(205, 242, 255), core_soft=False)
        hub(c, S / 2, S / 2, 1.3, 2.7)             # 흰 심: 가운데 고리
        bodies.append(b); cores.append(c)
    return bodies, cores


# ------------------------------------------------------------------ 궁수 화살: 굵은 촉 · 화살대 · 깃 (오른쪽으로)
def arrow():
    W, H = 20, 8
    bodies, cores = [], []
    for f in range(2):
        cy = H / 2
        def inside(x, y):
            dy = abs(y - cy)
            if 13 <= x <= 19.6:                     # 촉
                return dy <= 3.7 * (19.6 - x) / 6.6 + 0.2
            if 4 <= x < 13:                          # 화살대
                return dy <= 1.55
            if 0.5 <= x < 7:                          # 깃 (V 모양, 프레임마다 살짝 펄럭)
                k = (x - 0.5) / 6.5
                wing = 3.6 - 1.8 * k + (0.4 if f else 0)
                return 1.0 + 0.0 <= dy <= wing and (x - 0.5) > (dy - 1) * 0.9 - 0.5
            return False
        alpha = lambda x, y: 0.7 + 0.3 * min(1.0, x / 8)
        b, c = layers(W, H, inside, core_from=2, alpha=alpha, rim=(200, 240, 255))
        bodies.append(b); cores.append(c)
    return bodies, cores


# ------------------------------------------------------------------ 시계공 톱니: 이 여덟 개 · 굵은 테 · 가운데 축
def gear():
    S = 16
    bodies, cores = [], []
    for f in range(2):
        rot = f * math.pi / 8
        def inside(x, y):
            dx, dy = x - S / 2, y - S / 2
            r = math.hypot(dx, dy)
            th = math.atan2(dy, dx) + rot
            tooth = 1.0 if math.cos(8 * th) > 0.1 else 0.0
            if r > 5.4 + 2.4 * tooth:
                return False
            return r > 1.6                                            # 가운데 축 구멍
        b, c = layers(S, S, inside, core_from=4, rim=(205, 242, 255))
        hub(c, S / 2, S / 2, 1.6, 3.2)             # 흰 심: 축 둘레
        bodies.append(b); cores.append(c)
    return bodies, cores


# ------------------------------------------------------------------ 연금술사 플라스크: 둥근 병 · 목 · 마개 (코드에서 돌며 날아감)
def flask():
    S = 14
    bodies, cores = [], []
    for f in range(4):
        def inside(x, y):
            if math.hypot(x - 7, y - 8.6) <= 5.0:
                return True
            return 5.3 <= x <= 8.7 and 1.2 <= y <= 5.0
        b, c = layers(S, S, inside, core_from=99, rim=(195, 235, 255), core_soft=False)
        # 유리 반짝임: 프레임마다 위치가 도는 흰 점 (심 층)
        gx, gy = [(5, 6), (8, 6), (9, 9), (5, 9)][f]
        for dx, dy in ((0, 0), (1, 0), (0, 1)):
            c.set(gx + dx, gy + dy, (255, 255, 255, 255))
        bodies.append(b); cores.append(c)
    return bodies, cores


# ------------------------------------------------------------------ 우체부 편지 (색까지): 흰 종이 · 금빛 테 · 붉은 봉인 · 어두운 외곽선
def letter():
    W, H = 18, 13
    frames = []
    for f in range(2):
        img = Img(W, H)
        inside = lambda x, y: 1 <= x <= 17 and 1.5 <= y <= 11.5
        m, d = depth_map(W, H, inside)
        for y in range(H):
            for x in range(W):
                if m[y][x]:
                    k = d[y][x]
                    c = (255, 196, 70) if k == 1 else (255, 236, 170) if k == 2 else (255, 252, 240)
                    img.set(x, y, c)
                elif any(0 <= x + dx < W and 0 <= y + dy < H and m[y + dy][x + dx] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    img.set(x, y, OUTLINE)
        # 봉투 덮개 V 줄
        for x in range(3, 16):
            yy = 3 + round(abs(x - 9) * -0.62 + 4.4) if f == 0 else 3 + round(abs(x - 9) * -0.55 + 4.0)
            img.set(x, yy, (215, 170, 95))
        # 봉인 (붉은 밀랍 + 흰 반짝)
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, 1), (1, -1), (-1, -1)):
            img.set(9 + dx, 7 + dy, (230, 50, 60))
        img.set(8, 6, (255, 220, 220))
        # 반짝임 (프레임마다 다른 모서리)
        sx, sy = (14, 3) if f == 0 else (4, 9)
        img.set(sx, sy, (255, 255, 255)); img.set(sx + 1, sy, (255, 255, 255)); img.set(sx, sy + 1, (255, 255, 255))
        frames.append(img)
    return frames


# ------------------------------------------------------------------ 검사 베기 (색까지): 굵은 초승달 칼날 · 하얀 날 · 하늘색 테 · 어두운 외곽선
# 64x64, 가운데 = 플레이어, 오른쪽을 향함, +75° → -75° 로 쓸어내림 (예전과 같은 범위 = 판정 반지름 그대로)
# tier 2: 날이 더 두껍고 반짝이 · tier 3: 금빛 안쪽 날 + 영혼빛 바람 + 큰 반짝
def sword_swing(tier):
    frames = []
    N = 6
    inner = {1: 17.0, 2: 15.0, 3: 13.0}[tier]       # 칼날 띠 안쪽 반지름 (클수록 얇음)
    for f in range(N):
        img = Img(64, 64)
        head = 75 - 150 * min(1.0, (f + 1) / 4)
        fade = 1.0 if f < 4 else (1.0 - (f - 3) / 3)

        def band(x, y):
            dx, dy = x - 32, 32 - y
            r = math.hypot(dx, dy)
            ang = math.degrees(math.atan2(dy, dx))
            if ang > 75 or ang < head:
                return False
            behind = (ang - head) / 150
            # 칼끝 쪽은 두껍고 뒤로 갈수록 바깥 쪽으로 가늘어짐
            r_in = inner + (31 - inner) * min(1.0, behind * 1.1) ** 1.6
            return r_in <= r <= 31.2
        m, d = depth_map(64, 64, band)
        for y in range(64):
            for x in range(64):
                dx, dy = x + 0.5 - 32, 32 - (y + 0.5)
                r = math.hypot(dx, dy)
                ang = math.degrees(math.atan2(dy, dx))
                behind = max(0.0, (ang - head) / 150)
                a = max(0.0, 1 - behind * 0.85) * fade
                if m[y][x]:
                    k = d[y][x]
                    if r > 29.6:
                        c = (255, 255, 255)                       # 바깥 날
                    elif k == 1:
                        c = (40, 120, 235)                        # 짙은 테
                    elif tier >= 3 and k >= 2 and r < inner + 4.5 + 6 * behind:
                        c = (255, 214, 110)                       # 진화: 금빛 안쪽 날
                    elif tier >= 2 and abs(r - 26.5) < 0.6:
                        c = (255, 255, 255)                       # 강화: 안쪽 두 번째 흰 날
                    elif k == 2:
                        c = (95, 190, 255)
                    else:
                        c = (195, 238, 255)
                    if a > 0.04:
                        img.set(x, y, c + (int(255 * min(1.0, a * 1.15)),))
                elif any(0 <= x + ox < 64 and 0 <= y + oy < 64 and m[y + oy][x + ox] for ox, oy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                    if a > 0.1:
                        img.set(x, y, OUTLINE[:3] + (int(230 * min(1.0, a * 1.2)),))
                elif 9 <= r < 31 and head <= ang <= 75 and a > 0.05:
                    # 안쪽 바람 (옅게, 적을 가리지 않게)
                    wind = (150, 225, 255) if tier < 3 else (170, 240, 255)
                    img.blend(x, y, wind + (int(70 * a * (r - 9) / 22),))
        if f < 4:
            ha = math.radians(head)
            # 칼끝 섬광: 흰 십자 (단계가 오를수록 크게)
            L = 2 + tier
            cx, cy = 32 + math.cos(ha) * 29.5, 32 - math.sin(ha) * 29.5
            for i in range(-L, L + 1):
                img.blend(int(cx + i), int(cy), (255, 255, 255, 240 - 30 * abs(i)))
                img.blend(int(cx), int(cy + i), (255, 255, 255, 240 - 30 * abs(i)))
            if tier >= 2:
                for j, rr in enumerate((24, 27)):
                    aa = math.radians(head + 18 + 22 * j + 8 * f)
                    sx, sy = int(32 + math.cos(aa) * rr), int(32 - math.sin(aa) * rr)
                    for ox, oy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
                        img.blend(sx + ox, sy + oy, (255, 255, 255, 220 if (ox, oy) == (0, 0) else 150))
        frames.append(img)
    return frames


def save_pair(name, pair):
    bodies, cores = pair
    save_sheet(name, bodies)
    save_sheet(name + '_core', cores)


if __name__ == '__main__':
    save_pair('atk_bullet', bullet())
    save_pair('atk_shuriken', shuriken())
    save_pair('atk_arrow', arrow())
    save_pair('atk_gear', gear())
    save_pair('atk_flask', flask())
    save_sheet('atk_letter', letter())
    save_sheet('fx_swordswing', sword_swing(1))
    save_sheet('fx_swordswing_t2', sword_swing(2))
    save_sheet('fx_swordswing_t3', sword_swing(3))
