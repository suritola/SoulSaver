# 진화 무기(영웅 전용 무기) 필살기 · 평타 덧효과 전용 도트 이펙트
#   예전엔 원 웅덩이(fx_puddle) · 충격파 고리(fx_shock) · 구슬(fx_orb) · 키캡(fx_keycap) · 매(fx_falcon) · 번개(fx_bolt) 같은
#   범용 도형을 색만 바꿔 썼음 → 무기마다 무엇인지 모양으로 읽히는 그림 (docs/art/진화무기-이펙트-교체표.md)
#   규칙 (docs/art/그래픽-개편-기획서.md): 한 화소 크기 · 그림당 4~6색 · 반투명 · 그라데이션 없음(사라질 때는 디더로 화소를 뺌)
#     · 검정 테 대신 그 물체의 짙은 색 테 · 범위 그림은 속을 비우고 가장자리로 범위를 보여 줌 · 닫힌 고리 대신 끊긴 마디
# 실행: py -3.14 -X utf8 tools/pixelart/make_kit_ults.py
import math, os, random, sys
sys.path.insert(0, os.path.dirname(__file__))
from make_fx2 import save_sheet
from png import Img

TAU = math.tau
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


# ------------------------------------------------------------------ 기본 붓
def px(img, x, y, c):
    img.set(int(round(x)), int(round(y)), c)


def line(img, x0, y0, x1, y1, c):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for i in range(n + 1):
        t = i / n
        px(img, x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, c)


def disc(img, cx, cy, r, c):
    for y in range(int(cy - r - 1), int(cy + r + 2)):
        for x in range(int(cx - r - 1), int(cx + r + 2)):
            if (x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2 <= r * r:
                img.set(x, y, c)


def ellipse(img, cx, cy, rx, ry, c):
    for y in range(int(cy - ry - 1), int(cy + ry + 2)):
        for x in range(int(cx - rx - 1), int(cx + rx + 2)):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1:
                img.set(x, y, c)


def poly(img, pts, c):
    ys = [p[1] for p in pts]
    for y in range(int(min(ys)), int(max(ys)) + 2):
        yc = y + 0.5
        xs = []
        for i in range(len(pts)):
            (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % len(pts)]
            if (y0 <= yc < y1) or (y1 <= yc < y0):
                xs.append(x0 + (yc - y0) * (x1 - x0) / (y1 - y0))
        xs.sort()
        for a, b in zip(xs[::2], xs[1::2]):
            for x in range(int(math.floor(a + 0.5)), int(math.floor(b + 0.5))):
                img.set(x, y, c)


def outline(img, c):
    # 칠한 화소 바깥 한 칸에 테 (4방향)
    add = []
    for y in range(img.h):
        for x in range(img.w):
            if img.px[y][x][3]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                if img.get(x + dx, y + dy)[3]:
                    add.append((x, y))
                    break
    for x, y in add:
        img.set(x, y, c)


def fade(img, keep):
    # 디더로 화소를 뺌 (keep 0~1 = 남길 몫) — 반투명 대신
    for y in range(img.h):
        for x in range(img.w):
            if img.px[y][x][3] and BAYER[y % 4][x % 4] >= keep * 16:
                img.px[y][x] = (0, 0, 0, 0)


def rot(x, y, a):
    ca, sa = math.cos(a), math.sin(a)
    return x * ca - y * sa, x * sa + y * ca


def blank(w, h):
    return Img(w, h)


# ------------------------------------------------------------------ 검사 · 대지 진동 (망치): 갈라지는 땅 + 솟구치는 바위 조각
def quake():
    S, C = 48, 24
    CRK, LAVA, HOT = (58, 36, 26), (240, 140, 50), (255, 214, 120)
    R0, R1, R2 = (104, 78, 54), (156, 120, 84), (214, 182, 132)
    DUST = (196, 176, 146)
    rnd = random.Random(3)
    rays = []
    for i in range(8):
        a = i / 8 * TAU + rnd.uniform(-0.25, 0.25)
        pts, x, y = [], 0.0, 0.0
        for k in range(24):
            a2 = a + rnd.uniform(-0.45, 0.45)
            x += math.cos(a2); y += math.sin(a2)
            pts.append((x, y))
        rays.append(pts)
    chunks = [(i / 11 * TAU + rnd.uniform(-0.2, 0.2), rnd.uniform(0.8, 1.2), rnd.choice((2, 3))) for i in range(11)]
    out = []
    for f in range(6):
        img = blank(S, S)
        L = [7, 14, 20, 21, 21, 21][f]
        for pts in rays:
            for k, (x, y) in enumerate(pts[:L]):
                px(img, C + x, C + y, CRK)
                if k < L - 4:
                    px(img, C + x + 1, C + y, CRK); px(img, C + x, C + y + 1, CRK)
            for x, y in pts[:max(0, L - 4)]:
                if f <= 2:
                    px(img, C + x, C + y, HOT if f == 0 else LAVA)
                elif f == 3:
                    if (int(x) + int(y)) % 3 == 0:
                        px(img, C + x, C + y, LAVA)
        # 들려 기울어진 땅 판 넷 (균열 사이)
        if f >= 1:
            for i in range(4):
                a = i / 4 * TAU + 0.4
                d = 6 + min(f, 3)
                x, y = C + math.cos(a) * d, C + math.sin(a) * d
                q = [(x - 3, y - 2), (x + 3, y - 3), (x + 4, y + 2), (x - 2, y + 3)]
                poly(img, q, R1)
                line(img, x - 3, y - 2, x + 3, y - 3, R2)
                line(img, x - 2, y + 3, x + 4, y + 2, R0)
        # 가운데 움푹 팬 자리
        disc(img, C, C, 3.2, R0)
        disc(img, C - 0.6, C - 0.6, 2.0, CRK)
        if f <= 1:
            disc(img, C, C, 1.4, HOT)
        # 솟구쳤다 떨어지는 바위 조각
        if 1 <= f <= 5:
            for a, sp, sz in chunks:
                d = (3 + f * 3.6) * sp
                lift = [0, 2, 3, 2, 1, 0][f]
                x, y = C + math.cos(a) * d, C + math.sin(a) * d - lift
                x, y = int(x), int(y)
                sz += 1
                img.rect(x - 1, y - 1, sz + 2, sz + 2, CRK)
                img.rect(x, y, sz, sz, R1)
                img.rect(x, y, sz, 1, R2)
                img.rect(x + sz - 1, y + 1, 1, sz - 1, R0)
        # 바닥 먼지
        if f >= 3:
            for i in range(10):
                a = i / 10 * TAU + 0.3
                d = 14 + f * 1.5
                x, y = C + math.cos(a) * d, C + math.sin(a) * d
                if (i + f) % 2 == 0:
                    img.rect(int(x), int(y), 2, 2, DUST)
                else:
                    px(img, x, y, DUST)
        if f == 4:
            fade(img, 0.75)
        if f == 5:
            fade(img, 0.4)
        out.append(img)
    return out


# ------------------------------------------------------------------ 검사 · 뱀의 춤 (채찍검): 칼날 마디 (fx_trail_dot 와 같은 8x8 칸)
def whip_blade():
    STEEL, LIGHT, DARK, EDGE = (196, 212, 236), (252, 253, 255), (104, 120, 160), (66, 76, 112)
    out = []
    for f in range(4):
        img = blank(8, 8)
        # 오른쪽을 향한 잎 모양 칼날 마디 + 왼쪽 이음쇠
        poly(img, [(1, 4), (4, 2), (7.5, 4), (4, 6)], STEEL)
        line(img, 2, 3.5, 6, 3.5, LIGHT)
        line(img, 2, 4.6, 6, 4.6, DARK)
        img.set(0, 3, EDGE); img.set(0, 4, EDGE)
        g = 2 + f
        img.set(g, 3, LIGHT); img.set(g, 2 + (f % 2), LIGHT)
        out.append(img)
    return out


# ------------------------------------------------------------------ 검사 · 기사 돌격 (기창): 창 잔상 + 창끝 충돌
def lance_ghost():
    SH, LT, GD, TP = (150, 176, 220), (232, 240, 255), (232, 186, 80), (255, 255, 255)
    out = []
    for f in range(4):
        img = blank(32, 8)
        line(img, 1, 4, 20, 4, SH)
        line(img, 1, 3, 18, 3, LT)
        poly(img, [(20, 1), (31.5, 3.5), (31.5, 4.5), (20, 7)], SH)       # 원뿔 창끝
        line(img, 21, 3, 30, 3.5, LT)
        line(img, 19, 1, 19, 6, GD)                                       # 손 보호대
        img.set(30, 3, TP); img.set(31, 4, TP)
        fade(img, [1.0, 0.75, 0.5, 0.25][f])
        out.append(img)
    return out


def lance_tip():
    SL, LT, DK, GD, W = (206, 218, 236), (250, 252, 255), (98, 110, 140), (232, 184, 72), (255, 255, 255)
    out = []
    for f in range(5):
        img = blank(24, 24)
        if f <= 2:
            poly(img, [(2, 9), (13, 11.5), (13, 12.5), (2, 15)], SL)
            line(img, 3, 11, 12, 11.6, LT)
            line(img, 3, 14, 12, 12.6, DK)
            line(img, 1, 8, 1, 15, GD)
        # 창끝에서 터지는 별빛
        n = [4, 8, 8, 8, 8][f]
        L = [3, 6, 9, 10, 10][f]
        for i in range(n):
            a = i / n * TAU + (0.4 if i % 2 else 0)
            l = L if i % 2 == 0 else L * 0.6
            for k in range(int(max(0, L - 6)) if f >= 3 else 0, int(l) + 1):
                px(img, 14 + math.cos(a) * k, 12 + math.sin(a) * k, W if k < l * 0.6 else GD)
        if f <= 1:
            disc(img, 14, 12, 1.6 + f, W)
        if f == 3:
            fade(img, 0.65)
        if f == 4:
            fade(img, 0.3)
        out.append(img)
    return out


# ------------------------------------------------------------------ 도적 · 독안개 (독침): 가장자리에 굴러가는 독 연기 띠 + 독 거품 · 해골
def poison_fog():
    S, C, R = 64, 32, 27
    G0, G1, G2, BUB = (62, 112, 34), (128, 188, 52), (196, 236, 110), (210, 250, 150)
    SK, SKD = (222, 255, 200), (38, 98, 46)
    out = []
    for f in range(6):
        img = blank(S, S)
        ph = f / 6 * TAU / 14
        for i in range(14):
            a = i / 14 * TAU + ph
            rr = 4.4 + 0.9 * math.sin(i * 1.7 + f * TAU / 6)
            x, y = C + math.cos(a) * R, C + math.sin(a) * R
            tx, ty = -math.sin(a), math.cos(a)
            for s2 in (-1, 0, 1):
                disc(img, x + tx * s2 * rr * 0.8, y + ty * s2 * rr * 0.8 + abs(s2), rr * (0.62 if s2 else 0.8), G1)
        for i in range(14):
            a = i / 14 * TAU + ph
            rr = 4.4 + 0.9 * math.sin(i * 1.7 + f * TAU / 6)
            x, y = C + math.cos(a) * R, C + math.sin(a) * R
            line(img, x - rr * 0.9, y - rr * 0.55, x + rr * 0.5, y - rr * 0.6, G2)
            line(img, x - rr * 0.5, y + rr * 0.55, x + rr * 0.9, y + rr * 0.5, G0)
        # 안쪽: 떠오르는 거품 (속은 비움)
        for i, (bx, by0, br) in enumerate(((22, 40, 2), (40, 36, 1.5), (30, 46, 1.5), (44, 26, 2), (18, 28, 1.5), (34, 22, 1.5))):
            by = (by0 - f * 3 - 6) % 30 + 17
            for k in range(12):
                a = k / 12 * TAU
                px(img, bx + math.cos(a) * br, by + math.sin(a) * br, BUB)
        # 해골 거품 하나 (독 표시)
        sx, sy = 29, 26 + [0, -1, -2, -2, -1, 0][f]
        for yy, row in enumerate(('..sss..', '.sssss.', 'sssssss', 'sKKsKKs', 'sKKsKKs', 'sssKsss', '.sssss.', '.s.s.s.')):
            for xx, ch in enumerate(row):
                if ch == 's':
                    img.set(sx + xx, sy + yy, SK)
                elif ch == 'K':
                    img.set(sx + xx, sy + yy, SKD)
        out.append(img)
    return out


def poison_bubble():
    G1, G2, SK = (72, 160, 62), (196, 250, 150), (222, 255, 200)
    out = []
    for f in range(5):
        img = blank(16, 16)
        if f <= 2:
            r = 2.2 + f * 1.4
            cy = 10 - f
            for k in range(20):
                a = k / 20 * TAU
                px(img, 8 + math.cos(a) * r, cy + math.sin(a) * r, G1)
            px(img, 8 - r * 0.45, cy - r * 0.45, G2)
            if f == 2:
                px(img, 7, cy - 1, SK); px(img, 9, cy - 1, SK); px(img, 8, cy + 1, SK)
        else:
            d = 4 + (f - 3) * 2.5
            for k in range(8):
                a = k / 8 * TAU
                px(img, 8 + math.cos(a) * d, 7 + math.sin(a) * d, G2 if k % 2 else G1)
            if f == 3:
                for xx, yy in ((7, 5), (9, 5), (8, 6), (7, 7), (9, 7)):
                    img.set(xx, yy, SK)
        out.append(img)
    return out


# ------------------------------------------------------------------ 도적 · 잭팟 (도박 카드): 트럼프 카드 (fx_card 와 같은 16x16 칸) + 조커 폭발
def play_card():
    W, EDGE, RED, BLK, BACK, BACK2 = (246, 244, 236), (150, 148, 164), (214, 40, 60), (40, 36, 52), (176, 36, 52), (236, 124, 124)
    out = []
    # 앞면
    img = blank(16, 16)
    img.rect(3, 1, 10, 14, W)
    for x in range(3, 13):
        img.set(x, 1, EDGE); img.set(x, 14, EDGE)
    for y in range(1, 15):
        img.set(3, y, EDGE); img.set(12, y, EDGE)
    poly(img, [(5.5, 6), (10.5, 6), (8, 10)], RED)
    disc(img, 6.8, 6.4, 1.3, RED); disc(img, 9.2, 6.4, 1.3, RED)
    img.set(4, 2, RED); img.set(4, 3, RED); img.set(11, 13, RED); img.set(11, 12, RED)
    out.append(img)
    # 옆(돌아가는 중)
    img = blank(16, 16)
    img.rect(7, 1, 2, 14, W); line(img, 9, 1, 9, 14, EDGE)
    out.append(img)
    # 뒷면
    img = blank(16, 16)
    img.rect(3, 1, 10, 14, W)
    img.rect(4, 2, 8, 12, BACK)
    for y in range(2, 14):
        for x in range(4, 12):
            if (x + y) % 3 == 0 or (x - y) % 3 == 0:
                img.set(x, y, BACK2)
    out.append(img)
    img = blank(16, 16)
    img.rect(7, 1, 2, 14, BACK); line(img, 7, 1, 7, 14, W)
    out.append(img)
    return out


def joker_burst():
    PUR, PUR2, GD, RED, GRN, W = (128, 60, 172), (186, 120, 230), (250, 200, 60), (230, 60, 70), (80, 200, 110), (255, 255, 255)
    CONF = (RED, GD, PUR2, GRN, W)
    rnd = random.Random(9)
    bits = [(rnd.uniform(0, TAU), rnd.uniform(0.6, 1.1), rnd.randrange(5)) for _ in range(22)]
    cards = [(i / 5 * TAU + 0.3, rnd.choice((0, 1))) for i in range(5)]
    out = []
    for f in range(6):
        img = blank(32, 32)
        if f == 0:
            # 조커 카드 + 씩 웃는 얼굴
            img.rect(10, 7, 12, 17, W)
            img.rect(11, 8, 10, 15, PUR)
            poly(img, [(12, 13), (16, 9), (20, 13)], GD)         # 광대 모자
            img.set(14, 15, W); img.set(18, 15, W)
            line(img, 13, 18, 19, 18, W); img.set(12, 17, W); img.set(20, 17, W)
        else:
            if f == 1:
                for i in range(8):
                    a = i / 8 * TAU
                    for k in range(2, 8):
                        px(img, 16 + math.cos(a) * k, 16 + math.sin(a) * k, W if k < 5 else GD)
                disc(img, 16, 16, 2.5, W)
            for a, sp, ci in bits:
                d = (3 + f * 3) * sp
                x, y = 16 + math.cos(a) * d, 16 + math.sin(a) * d + max(0, f - 3)
                c = CONF[ci]
                px(img, x, y, c)
                if (ci + f) % 2 == 0:
                    px(img, x + 1, y, c)
                else:
                    px(img, x, y + 1, c)
            for a, side in cards:
                d = 2 + f * 2.6
                x, y = 16 + math.cos(a) * d, 16 + math.sin(a) * d
                x, y = int(x), int(y)
                if (f + side) % 2 == 0:
                    img.rect(x - 1, y - 2, 3, 4, W); img.set(x, y - 1, RED if side else PUR)
                else:
                    img.rect(x - 2, y - 1, 4, 3, W); img.set(x, y, RED if side else PUR)
        if f == 4:
            fade(img, 0.7)
        if f == 5:
            fade(img, 0.35)
        out.append(img)
    return out


# ------------------------------------------------------------------ 도적 · 거미줄 감옥 (살상 와이어): 마디로 끊긴 거미줄 테 + 실
def web_ring():
    S, C, R = 64, 32, 29
    SILK, SH, KNOT, DEW = (230, 224, 255), (146, 130, 200), (255, 255, 255), (176, 238, 255)
    out = []
    for f in range(2):
        img = blank(S, S)
        n = 12
        anchors = [(C + math.cos(i / n * TAU) * R, C + math.sin(i / n * TAU) * R) for i in range(n)]
        for i in range(n):
            (x0, y0), (x1, y1) = anchors[i], anchors[(i + 1) % n]
            for k in range(2, 15):          # 매듭 곁 한두 칸은 비움 (닫힌 고리 아님)
                t = k / 16
                a = (i + t) / n * TAU
                sag = 1.6 * math.sin(t * math.pi)
                rr = R - sag
                px(img, C + math.cos(a) * rr, C + math.sin(a) * rr, SILK)
                if k % 3 == 0:
                    px(img, C + math.cos(a) * (rr + 1), C + math.sin(a) * (rr + 1), SH)
            # 매듭 + 안쪽으로 짧은 바퀴살
            ax, ay = anchors[i]
            a = i / n * TAU
            for k in range(1, 7):
                px(img, C + math.cos(a) * (R - k), C + math.sin(a) * (R - k), SILK if k < 4 else SH)
            img.rect(int(ax) - 1, int(ay) - 1, 2, 2, KNOT)
            # 이슬방울 반짝임 (프레임마다 자리 바뀜)
            if (i + f) % 3 == 0:
                a2 = (i + 0.5) / n * TAU
                px(img, C + math.cos(a2) * (R - 1.6), C + math.sin(a2) * (R - 1.6), DEW)
        out.append(img)
    return out


def silk():
    SILK, KNOT, SH = (232, 226, 255), (255, 255, 255), (150, 134, 204)
    out = []
    for f in range(2):
        img = blank(16, 4)
        line(img, 0, 1, 15, 1, SILK)
        line(img, 0, 2, 15, 2, SH)
        for x in range(f * 2, 16, 5):
            img.set(x, 1, KNOT); img.set(x, 2, KNOT)
        out.append(img)
    return out


# ------------------------------------------------------------------ 궁수 · 창의 벽 (투창): 투창 (fx_arrow 와 같은 높이 5)
def javelin():
    WD, WL, WK, ST, SD, GD, GR = (164, 112, 62), (210, 160, 98), (102, 66, 36), (214, 222, 236), (110, 120, 142), (234, 188, 76), (120, 58, 40)
    out = []
    for f in range(2):
        img = blank(28, 5)
        line(img, 0, 2, 20, 2, WD)
        line(img, 1, 1, 19, 1, WL)
        line(img, 0, 3, 20, 3, WK)
        img.rect(8, 1, 3, 3, GR)
        img.rect(19, 1, 2, 3, GD)
        poly(img, [(21, 0), (27.5, 2), (27.5, 3), (21, 5)], ST)
        line(img, 21, 3, 27, 3, SD)
        if f == 1:
            img.set(24, 1, (255, 255, 255)); img.set(25, 2, (255, 255, 255))
        out.append(img)
    return out


# ------------------------------------------------------------------ 연금술사 · 수은 폭풍: 출렁이는 수은 방울 (fx_orb 와 같은 16x16 칸)
def mercury():
    MID, SH, LT, W, RIM = (178, 188, 204), (100, 110, 132), (226, 232, 244), (255, 255, 255), (64, 70, 94)
    out = []
    for f, (rx, ry) in enumerate(((5.6, 5.0), (5.0, 5.6), (6.2, 4.6), (5.2, 5.4))):
        img = blank(16, 16)
        ellipse(img, 8, 8.5, rx, ry, MID)
        ellipse(img, 8.8, 9.6, rx * 0.75, ry * 0.65, SH)
        ellipse(img, 7.6, 8.2, rx * 0.6, ry * 0.55, MID)
        ellipse(img, 6.4, 6.6, rx * 0.35, ry * 0.25, LT)
        img.set(6, 6, W); img.set(5, 7, W)
        line(img, 9, 11, 11, 10, LT)
        # 튀는 작은 방울
        dx, dy = [(13, 3), (14, 4), (13, 13), (2, 13)][f]
        img.set(dx, dy, MID); img.set(dx + 1, dy, SH)
        outline(img, RIM)
        out.append(img)
    return out


# ------------------------------------------------------------------ 연금술사 · 블랙홀 플라스크: 검은 핵 + 빨려 드는 나선 팔 + 깨진 병 조각
def black_hole():
    S, C = 48, 24
    CORE, RIM, ARM, ARM2, GLASS, EDGE = (26, 10, 46), (206, 150, 255), (150, 90, 230), (224, 196, 255), (192, 240, 255), (110, 70, 170)
    out = []
    for f in range(6):
        img = blank(S, S)
        sp = f / 6 * TAU / 3
        # 범위 가장자리: 끊긴 점선 (돌며 안으로)
        for i in range(24):
            if i % 2:
                continue
            for k in range(3):
                a = (i + k / 3) / 24 * TAU - sp
                px(img, C + math.cos(a) * 22.5, C + math.sin(a) * 22.5, EDGE)
        # 나선 팔 셋
        for arm in range(3):
            for k in range(40):
                r = 21 - k * 0.38
                a = arm / 3 * TAU + k * 0.16 + sp * 2
                c = ARM2 if k % 5 == 0 else ARM
                px(img, C + math.cos(a) * r, C + math.sin(a) * r, c)
        # 빨려 드는 병 조각
        for i in range(4):
            a = i / 4 * TAU + sp * 3 + 0.5
            r = 15 - (f % 3) * 2
            x, y = C + math.cos(a) * r, C + math.sin(a) * r
            poly(img, [(x, y), (x + 2.5, y + 0.5), (x + 0.5, y + 2.5)], GLASS)
        disc(img, C, C, 6.2, RIM)
        disc(img, C, C, 5.2, CORE)
        px(img, C - 2, C - 2, ARM)
        out.append(img)
    return out


def mag_field():
    S, C = 48, 24
    RED, RED2, TIP, FL, FL2 = (210, 50, 50), (150, 26, 36), (224, 228, 238), (150, 166, 255), (232, 238, 255)
    out = []
    for f in range(5):
        img = blank(S, S)
        R = [22, 18, 14, 10, 7][f]
        for i in range(12):
            a = i / 12 * TAU + (0.13 if f % 2 else 0)
            x, y = C + math.cos(a) * R, C + math.sin(a) * R
            # 안쪽을 가리키는 꺾쇠 (자기력선 화살)
            for s in (-1, 1):
                ex, ey = rot(3.4, 3.0 * s, a)
                line(img, x, y, x + ex, y + ey, FL)
                ex2, ey2 = rot(3.4, 3.0 * s + 0.9 * s, a)
                line(img, x + 0.6 * math.cos(a), y + 0.6 * math.sin(a), x + ex2, y + ey2, FL)
            px(img, x, y, FL2); px(img, x - math.cos(a), y - math.sin(a), FL2)
            # 꼬리 쇳가루
            tx, ty = C + math.cos(a) * (R + 4), C + math.sin(a) * (R + 4)
            px(img, tx, ty, FL)
        # 가운데 말굽자석 (자석만 테)
        mag = blank(S, S)
        field = img
        img = mag
        for y in range(19, 30):
            for x in range(19, 30):
                d = math.hypot(x + 0.5 - C, y + 0.5 - 22.5)
                if 2.6 <= d <= 5.3 and y < 23 or (y >= 23 and (19 <= x <= 21 or 27 <= x <= 29)):
                    img.set(x, y, RED)
        for x in (19, 20, 21, 27, 28, 29):
            img.set(x, 28, TIP); img.set(x, 29, TIP)
        line(img, 20, 22, 22, 18, (255, 120, 110))
        outline(img, RED2)
        field.paste(img, 0, 0)
        img = field
        if f == 4:
            fade(img, 0.55)
        out.append(img)
    return out


# ------------------------------------------------------------------ 연금술사 · 불꽃 축제 (폭죽): 국화 모양 불꽃 (흰 바탕 → 코드에서 색)
def firework_star():
    W, L, G = (255, 255, 255), (222, 222, 228), (160, 160, 172)
    out = []
    for f in range(6):
        img = blank(32, 32)
        if f == 0:
            disc(img, 16, 16, 1.6, W)
            for a in range(4):
                px(img, 16 + math.cos(a * TAU / 4) * 3, 16 + math.sin(a * TAU / 4) * 3, L)
        else:
            head = [0, 5, 10, 13, 14, 14][f]
            tail = [0, 4, 6, 6, 5, 3][f]
            drop = [0, 0, 0, 1, 2, 3][f]
            for i in range(14):
                a = i / 14 * TAU
                for k in range(max(1, head - tail), head + 1):
                    yy = 16 + math.sin(a) * k + drop * (k / max(1, head))
                    c = W if k == head else (L if k > head - tail * 0.5 else G)
                    if f >= 4 and (k + i) % 2:
                        continue
                    px(img, 16 + math.cos(a) * k, yy, c)
                if f >= 3:
                    px(img, 16 + math.cos(a) * head, 16 + math.sin(a) * head + drop + 1, L)
            if f == 1:
                disc(img, 16, 16, 2.4, W)
        if f == 5:
            fade(img, 0.45)
        out.append(img)
    return out


# ------------------------------------------------------------------ 지도 제작자 · 삼각 측량 (측량 사슬): 놋쇠 사슬 고리 · 측량 말뚝
def chain_link(main, dark, light):
    img = blank(12, 6)
    # 눕힌 고리 하나 + 옆으로 선 고리(막대) 하나를 이어 붙인 한 마디 → 줄지어 놓으면 사슬
    for k in range(20):
        a = k / 20 * TAU
        px(img, 3.5 + math.cos(a) * 3.0, 2.6 + math.sin(a) * 2.2, main)
    px(img, 2, 1, light); px(img, 3, 0, light)
    line(img, 6, 2, 11, 2, main)
    line(img, 6, 3, 11, 3, dark)
    img.set(8, 2, light)
    return [img]


def survey_stake():
    WD, WK, RED, W, DK = (172, 120, 70), (110, 72, 40), (222, 52, 52), (250, 246, 236), (130, 30, 34)
    out = []
    for f in range(4):
        img = blank(12, 20)
        img.rect(5, 4, 2, 13, WD)
        line(img, 6, 4, 6, 16, WK)
        img.set(5, 17, WD); img.set(5, 18, WK)
        # 펄럭이는 깃발 (빨강 · 흰 줄)
        wave = [0, 1, 0, -1][f]
        for x in range(7, 12):
            t = (x - 7) / 4
            top = 1 + round(wave * t)
            bot = 6 - round(t * 2) + round(wave * t)
            for y in range(top, bot + 1):
                img.set(x, y, W if x == 9 else RED)
        line(img, 7, 1, 7, 6, DK)
        # 말뚝이 박힌 땅 자국
        img.set(3, 18, WK); img.set(8, 18, WK); img.set(4, 19, WK); img.set(7, 19, WK)
        out.append(img)
    return out


# ------------------------------------------------------------------ 지도 제작자 · 먹물 해일 (잉크병): 말려 오는 먹물 파도 마루 · 깨지는 잉크병 · 튀는 먹
def ink_wave():
    W_, H = 32, 48
    BACK, MID, FACE, FOAM, DROP = (34, 32, 84), (54, 70, 160), (88, 118, 214), (218, 228, 255), (44, 52, 128)
    rnd = random.Random(5)
    out = []
    for f in range(6):
        img = blank(W_, H)
        ph = f / 6 * TAU
        for y in range(H):
            t = abs(y - H / 2 + 0.5) / (H / 2)
            if t > 0.98:
                continue
            taper = math.sqrt(1 - t * t)
            lip = 21 + 3.2 * math.sin(y * 0.33 + ph) * taper + 2 * taper
            back = lip - 18 * taper - 1.5 * math.sin(y * 0.5 - ph)
            for x in range(int(back), int(lip) + 1):
                k = (x - back) / max(1, lip - back)
                c = BACK if k < 0.35 else MID if k < 0.7 else FACE
                img.set(x, y, c)
            # 말린 물마루 거품
            img.set(int(lip) + 1, y, FOAM)
            img.set(int(lip), y, FOAM)
            if (y + f) % 4 == 0 and taper > 0.3:
                img.set(int(lip) + 2, y, FOAM)
                img.set(int(lip) + 2, y - 1, FOAM)
            # 뒤쪽 물결 무늬
            if (y + f) % 6 == 0 and taper > 0.4:
                for xx in range(int(back) + 3, int(back) + 7):
                    img.set(xx, y, MID)
        # 앞으로 튀는 물보라
        for i in range(9):
            y = rnd.randrange(6, H - 6)
            x = 25 + (i * 3 + f * 2) % 7
            img.set(x, y, FOAM if i % 3 else DROP)
            img.set(x, y + 1, DROP)
        out.append(img)
    return out


def ink_spatter():
    INK, LT = (48, 54, 132), (120, 142, 238)
    out = []
    for f in range(5):
        img = blank(16, 16)
        if f == 0:
            disc(img, 8, 8, 1.8, INK)
        else:
            r = [0, 2.6, 3.2, 3.0, 2.4][f]
            disc(img, 8, 8, r, INK)
            for i in range(6):
                a = i / 6 * TAU + 0.4
                d = [0, 4, 5.5, 6.5, 7][f]
                px(img, 8 + math.cos(a) * d, 8 + math.sin(a) * d, INK)
                if f <= 2:
                    px(img, 8 + math.cos(a) * (d - 1), 8 + math.sin(a) * (d - 1), INK)
            px(img, 7, 7, LT)
        if f == 4:
            fade(img, 0.5)
        out.append(img)
    return out


def ink_shatter():
    GLASS, GL2, GDK, CORK, INK, INK2, W = (196, 214, 244), (120, 140, 200), (62, 72, 132), (164, 112, 64), (46, 52, 128), (112, 136, 236), (255, 255, 255)
    rnd = random.Random(7)
    shards = [(i / 7 * TAU + rnd.uniform(-0.2, 0.2), rnd.uniform(0.8, 1.2)) for i in range(7)]
    drops = [(rnd.uniform(0, TAU), rnd.uniform(0.6, 1.0)) for _ in range(12)]
    out = []
    for f in range(6):
        img = blank(32, 32)
        if f <= 1:
            # 잉크병: 납작한 유리병 + 목 + 코르크
            ellipse(img, 16, 18, 6.5, 5, GDK)
            ellipse(img, 16, 19, 5.5, 3.5, INK)
            ellipse(img, 14, 16.5, 2, 1.2, GL2)
            img.set(13, 16, W)
            img.rect(14, 10, 4, 4, GDK)
            img.rect(14, 8, 4, 2, CORK)
            if f == 1:
                line(img, 12, 15, 16, 19, W); line(img, 16, 19, 19, 16, W); line(img, 16, 19, 15, 22, W)
                disc(img, 16, 20, 1, INK2)
        else:
            k = f - 1
            # 쏟아진 먹 (별 모양 얼룩 + 방울)
            disc(img, 16, 17, 3 + k * 1.2, INK)
            for i in range(6):
                a = i / 6 * TAU + 0.3
                for d in range(2, int(5 + k * 1.8)):
                    px(img, 16 + math.cos(a) * d, 17 + math.sin(a) * d, INK)
            for a, sp in drops:
                d = (6 + k * 2.6) * sp
                px(img, 16 + math.cos(a) * d, 17 + math.sin(a) * d, INK2 if k < 3 else INK)
            for i in range(10):
                a = i / 10 * TAU
                rr = 3 + k * 1.2
                px(img, 16 + math.cos(a) * rr, 17 + math.sin(a) * rr, INK2)
            px(img, 14, 15, INK2); px(img, 15, 15, INK2)
            # 날아가는 유리 조각
            for a, sp in shards:
                d = (4 + k * 3.2) * sp
                x, y = 16 + math.cos(a) * d, 17 + math.sin(a) * d - (2 if k < 3 else 0)
                poly(img, [(x, y), (x + 2.6, y + 0.8), (x + 0.6, y + 2.4)], GLASS)
                px(img, x + 1, y + 1, W)
        if f == 4:
            fade(img, 0.75)
        if f == 5:
            fade(img, 0.4)
        out.append(img)
    return out


# ------------------------------------------------------------------ 지도 제작자 · 자전 (지구본): 도는 지구본 (fx_orb 와 같은 16x16 칸)
def globe():
    SEA, SEA2, LAND, LAND2, BR, BR2, LT = (64, 126, 220), (36, 78, 164), (96, 184, 92), (54, 124, 64), (232, 188, 80), (150, 108, 40), (196, 226, 255)

    def land(lon, lat):
        v = math.sin(lon * 2 + 0.5) * math.cos(lat * 2.2) + 0.6 * math.sin(lon * 3 - lat * 2 + 1.3)
        return v > 0.45

    out = []
    for f in range(8):
        img = blank(16, 16)
        off = f / 8 * TAU
        for y in range(16):
            for x in range(16):
                dx, dy = (x + 0.5 - 8) / 5.6, (y + 0.5 - 8) / 5.6
                if dx * dx + dy * dy > 1:
                    continue
                lat = math.asin(max(-1, min(1, dy)))
                lon = math.asin(max(-1, min(1, dx / max(0.05, math.cos(lat))))) + off
                shade = dx * 0.6 + dy * 0.6 > 0.45
                if land(lon, lat):
                    img.set(x, y, LAND2 if shade else LAND)
                else:
                    img.set(x, y, SEA2 if shade else SEA)
        img.set(5, 5, LT); img.set(6, 4, LT)
        # 놋쇠 자오선 테 (위 · 아래가 트인 반원 둘)
        for k in range(24):
            a = k / 24 * TAU
            if abs(math.sin(a)) > 0.92:
                continue
            px(img, 8 + math.cos(a) * 7.0 - 0.5, 8 + math.sin(a) * 7.0 - 0.5, BR if math.cos(a) < 0 else BR2)
        img.set(7, 0, BR); img.set(8, 15, BR2)
        out.append(img)
    return out


# ------------------------------------------------------------------ 시계공 · 뻐꾸기 떼 / 우체부 · 비둘기 (fx_falcon 과 같은 24x16 칸)
def cuckoo():
    BR, BR2, CH, BAR, BEAK, EYE, W = (150, 98, 58), (96, 62, 38), (232, 212, 170), (150, 120, 92), (244, 150, 40), (34, 24, 20), (255, 255, 255)
    out = []
    for f in range(4):
        img = blank(24, 16)
        ellipse(img, 11, 9, 6, 3.6, BR)              # 몸
        ellipse(img, 12, 10.5, 4.5, 2, CH)          # 줄무늬 가슴
        for x in range(8, 17, 2):
            img.set(x, 10, BAR); img.set(x + 1, 11, BAR)
        disc(img, 17.5, 7, 2.6, BR)                 # 머리
        img.set(18, 6, W); img.set(19, 6, EYE)
        poly(img, [(20, 6.5), (23.5, 7.5), (20, 8.5)], BEAK)
        poly(img, [(5, 8), (0.5, 6.5), (0.5, 10.5), (5, 10)], BR2)    # 꼬리
        img.set(1, 7, CH); img.set(1, 9, CH)
        # 날개 (위 · 가운데 · 아래 · 가운데)
        wy = [-5, -1, 3, -1][f]
        poly(img, [(8, 8), (14, 8), (11 - (1 if wy < 0 else 0), 8 + wy)], BR2)
        line(img, 9, 8, 13, 8, BR)
        outline(img, (70, 44, 28))
        out.append(img)
    return out


def pigeon():
    BODY, WING, BAR, NG, NP, EYE, FOOT, W, RIM = (178, 184, 200), (126, 132, 152), (70, 76, 98), (92, 176, 124), (152, 100, 176), (240, 140, 60), (224, 130, 144), (246, 248, 255), (66, 70, 92)
    out = []
    for f in range(2):
        img = blank(24, 16)
        ellipse(img, 11, 9, 6.2, 3.6, BODY)
        poly(img, [(5, 8), (0.5, 7), (0.5, 10.5), (5, 10.5)], WING)       # 꼬리
        img.set(1, 9, BAR); img.set(2, 9, BAR)
        disc(img, 17.6, 6.6, 2.6, BODY)
        img.set(15, 8, NG); img.set(16, 9, NG); img.set(15, 9, NP); img.set(16, 8, NP); img.set(17, 9, NG)
        img.set(18, 6, EYE)
        img.set(20, 7, W); img.set(21, 7, (200, 170, 150))
        img.set(10, 12, FOOT); img.set(12, 12, FOOT)
        if f == 0:
            poly(img, [(8, 8), (15, 8), (10, 1)], WING)
            line(img, 10, 3, 12, 6, BAR); line(img, 11, 3, 13, 6, BAR)
            img.set(10, 1, W)
        else:
            poly(img, [(8, 9), (15, 9), (10, 15)], WING)
            line(img, 10, 13, 12, 10, BAR); line(img, 11, 13, 13, 10, BAR)
        outline(img, RIM)
        out.append(img)
    return out


# ------------------------------------------------------------------ 시계공 · 모래 폭풍 (모래시계): 모래 알갱이 회오리 + 가운데 모래시계
def sand_storm():
    S, C = 64, 32
    S1, S2, S3, BR, GL = (244, 214, 140), (204, 156, 84), (150, 104, 52), (222, 172, 70), (236, 246, 255)
    out = []
    for f in range(6):
        img = blank(S, S)
        sp = f / 6 * TAU / 3
        for arm in range(4):
            for k in range(60):
                r = 6 + k * 0.42
                a = arm / 4 * TAU + r * 0.12 + sp
                if k % 3 == 2:
                    continue
                c = (S1, S2, S3)[(k + arm) % 3]
                px(img, C + math.cos(a) * r, C + math.sin(a) * r, c)
                if k % 4 == 0:
                    px(img, C + math.cos(a + 0.08) * (r + 1), C + math.sin(a + 0.08) * (r + 1), S2)
        # 가장자리: 마디로 끊긴 모래띠 (범위)
        for i in range(36):
            if i % 3 == 2:
                continue
            a = i / 36 * TAU + sp * 1.5
            for rr in (29, 30):
                px(img, C + math.cos(a) * rr, C + math.sin(a) * rr, S2 if rr == 30 else S1)
        # 가운데 모래시계
        img.rect(27, 24, 11, 2, BR); img.rect(27, 39, 11, 2, BR)
        for y in range(26, 39):
            t = abs(y - 32.5) / 6.5
            hw = 1 + 3.5 * t
            for x in range(int(32.5 - hw), int(32.5 + hw) + 1):
                if x in (int(32.5 - hw), int(32.5 + hw)):
                    img.set(x, y, GL)
        top = [5, 4, 3, 3, 2, 1][f]
        for y in range(32 - top, 32):
            t = abs(y - 32.5) / 6.5
            hw = 3.5 * t
            for x in range(int(32.5 - hw) + 1, int(32.5 + hw)):
                img.set(x, y, S1)
        bot = [1, 2, 3, 3, 4, 5][f]
        for y in range(39 - bot, 39):
            t = abs(y - 32.5) / 6.5
            hw = 3.5 * t
            for x in range(int(32.5 - hw) + 1, int(32.5 + hw)):
                img.set(x, y, S2)
        img.set(32, 32 + f % 3, S1)
        out.append(img)
    return out


# ------------------------------------------------------------------ 시계공 · 열두 번의 종 (괘종시계): 열두 눈금 시계판이 퍼짐 + 흔들리는 종
def chime_ring():
    S, C = 64, 32
    BR, BRD, LT = (240, 196, 82), (162, 112, 40), (255, 244, 200)
    out = []
    for f in range(6):
        img = blank(S, S)
        R = [7, 13, 19, 24.5, 29, 30][f]
        for h in range(12):
            a = h / 12 * TAU - TAU / 4
            major = h % 3 == 0
            L = 5 if major else 3
            for k in range(L):
                for w in ((0,) if not major else (-0.6, 0.6)):
                    ox, oy = rot(0, w, a)
                    px(img, C + math.cos(a) * (R - k) + ox, C + math.sin(a) * (R - k) + oy, LT if k == 0 else BR)
            # 눈금 사이 짧은 호 (사이사이 비움)
            for k in range(5):
                a2 = a + (k + 4) / 13 * (TAU / 12) * 0.9
                px(img, C + math.cos(a2) * R, C + math.sin(a2) * R, BR)
                px(img, C + math.cos(a2) * (R - 1), C + math.sin(a2) * (R - 1), BRD)
        if f <= 2:
            # 가운데 종 (좌우로 흔들림)
            sw = [-0.35, 0.35, 0][f]
            bell = [(-4, 3), (-3.5, 0), (-2.5, -2.5), (0, -3.5), (2.5, -2.5), (3.5, 0), (4, 3)]
            pts = [(C + rot(x, y, sw)[0], C + rot(x, y, sw)[1]) for x, y in bell]
            poly(img, pts, BR)
            bx, by = rot(-1.5, -1.5, sw)
            px(img, C + bx, C + by, LT)
            cx, cy = rot(0, 4.5, sw)
            disc(img, C + cx, C + cy, 1.1, BRD)
            tx, ty = rot(0, -4.5, sw)
            px(img, C + tx, C + ty, BRD)
        if f == 4:
            fade(img, 0.7)
        if f == 5:
            fade(img, 0.35)
        out.append(img)
    return out


# ------------------------------------------------------------------ 우체부 · 택배 폭격 (소포 상자): 떨어지는 상자 · 터지는 상자
def parcel_box():
    FR, TOP, SD, TAPE, LBL, RIM = (204, 152, 92), (232, 188, 122), (150, 104, 60), (244, 222, 160), (250, 248, 240), (96, 62, 34)
    out = []
    for f in range(2):
        img = blank(16, 16)
        o = f
        poly(img, [(2, 5 + o), (8, 2 + o), (14, 5 - o), (8, 8)], TOP)
        poly(img, [(2, 5 + o), (8, 8), (8, 15), (2, 12 + o)], FR)
        poly(img, [(8, 8), (14, 5 - o), (14, 12 - o), (8, 15)], SD)
        line(img, 5, 3.5 + o, 11, 6.5 - o * 0.5, TAPE)
        line(img, 5, 6.5 + o, 5, 13 + o * 0.5, TAPE)
        img.rect(10, 9, 2, 2, LBL)
        outline(img, RIM)
        out.append(img)
    return out


def parcel_burst():
    FR, SD, TAPE, PEA, PAPER, DUST, W = (204, 152, 92), (150, 104, 60), (244, 222, 160), (250, 244, 214), (232, 236, 250), (214, 192, 156), (255, 255, 255)
    rnd = random.Random(12)
    pieces = [(i / 6 * TAU + rnd.uniform(-0.2, 0.2), rnd.uniform(0.8, 1.15)) for i in range(6)]
    peas = [(rnd.uniform(0, TAU), rnd.uniform(0.5, 1.0)) for _ in range(14)]
    out = []
    for f in range(6):
        img = blank(32, 32)
        if f == 0:
            img.rect(10, 13, 12, 10, FR)
            img.rect(16, 13, 6, 10, SD)
            poly(img, [(10, 13), (16, 13), (13, 8), (7, 9)], FR)       # 젖혀진 덮개
            poly(img, [(16, 13), (22, 13), (25, 9), (19, 8)], SD)
            img.rect(13, 10, 6, 3, (250, 244, 214))                    # 안의 완충재
            line(img, 10, 17, 21, 17, TAPE)
        else:
            k = f
            if f == 1:
                for i in range(8):
                    a = i / 8 * TAU
                    for d in range(2, 9):
                        px(img, 16 + math.cos(a) * d, 17 + math.sin(a) * d, W if d < 6 else TAPE)
            if f >= 2:
                for i in range(8):
                    a = i / 8 * TAU + 0.2
                    d = 4 + k
                    img.rect(int(16 + math.cos(a) * d), int(18 + math.sin(a) * d * 0.6), 2, 2, DUST)
            for a, sp in pieces:
                d = (3 + k * 2.6) * sp
                x, y = int(16 + math.cos(a) * d), int(17 + math.sin(a) * d - (2 if k < 4 else 0))
                if (k + int(a * 3)) % 2:
                    img.rect(x, y, 4, 3, FR); img.rect(x, y + 1, 4, 1, TAPE); img.set(x + 3, y + 2, SD)
                else:
                    img.rect(x, y, 3, 4, SD); img.rect(x + 1, y, 1, 4, TAPE)
            for a, sp in peas:
                d = (2 + k * 2.8) * sp
                x, y = 16 + math.cos(a) * d, 17 + math.sin(a) * d
                px(img, x, y, PEA); px(img, x + 1, y + 1, PEA)
            if 2 <= f <= 4:
                for i in range(3):
                    a = i / 3 * TAU + 1
                    d = 5 + k * 2
                    x, y = 16 + math.cos(a) * d, 17 + math.sin(a) * d
                    line(img, x, y, x + 2, y - 2, PAPER)
        if f == 4:
            fade(img, 0.7)
        if f == 5:
            fade(img, 0.35)
        out.append(img)
    return out


# ------------------------------------------------------------------ 우체부 · 우체통 행렬: 빨간 우체통 (예전 fx_keycap 「SPACE」 키캡을 빨갛게 칠해 썼음)
def postbox():
    RED, RL, RD, SLOT, PLATE, BASE, FLAG, ARM, RIM = (212, 44, 44), (246, 116, 104), (142, 22, 32), (48, 18, 24), (250, 236, 206), (92, 22, 28), (250, 200, 60), (190, 196, 210), (82, 14, 22)
    out = []
    for f in range(4):
        img = blank(16, 24)
        b = [0, -1, 0, 0][f]
        ellipse(img, 8, 6 + b, 5, 3.5, RED)
        img.rect(3, 6 + b, 10, 15 - b, RED)
        for y in range(5 + b, 21):
            img.set(3, y, RL); img.set(4, y, RL)
            img.set(12, y, RD)
        img.rect(5, 9 + b, 6, 1, SLOT)
        img.rect(6, 13, 4, 3, PLATE)
        img.rect(2, 21, 12, 2, BASE)
        img.rect(3, 4 + b, 3, 1, RL)
        # 옆의 배달 깃발 (올라갔다 내려옴)
        up = [0, 2, 4, 2][f]
        line(img, 13, 14, 13, 14 - 4 - up // 2, ARM)
        img.rect(14, 10 - up // 2 - 1, 2, 3, FLAG)
        outline(img, RIM)
        out.append(img)
    return out


# ------------------------------------------------------------------ 유령 · 영혼 사슬: 영혼 사슬 고리 · 조여 드는 족쇄
def soul_shackle():
    M, D, L, LOCK = (170, 238, 255), (72, 140, 204), (240, 255, 255), (120, 200, 255)
    out = []
    for f in range(4):
        img = blank(16, 16)
        r = [7, 6.2, 5.4, 5.4][f]
        n = 10
        for i in range(n):
            a = i / n * TAU + f * 0.2
            x, y = 8 + math.cos(a) * r, 9 + math.sin(a) * r * 0.55
            if i % 2 == 0:
                px(img, x, y, M); px(img, x + 1, y, M)
            else:
                px(img, x, y, D); px(img, x, y + 1, D)
        img.rect(7, 12 if f < 3 else 11, 2, 2, LOCK)
        img.set(7, 12 if f < 3 else 11, L)
        if f == 3:
            fade(img, 0.6)
        out.append(img)
    return out


# ------------------------------------------------------------------ 유령 · 절대 영도: 땅에서 솟는 얼음 가시 테 + 가운데 눈 결정
def ice_nova():
    S, C = 64, 32
    W, ICE, DEEP = (240, 252, 255), (150, 214, 244), (70, 128, 200)
    rnd = random.Random(4)
    small = [(rnd.uniform(0, TAU), rnd.uniform(8, 24)) for _ in range(14)]
    out = []
    for f in range(6):
        img = blank(S, S)
        R = [10, 20, 27, 28, 28, 28][f]
        H = [3, 5, 6, 6, 5, 4][f]
        # 가시 열여섯 (바깥을 향한 얼음 결정)
        for i in range(16):
            a = i / 16 * TAU + (0.1 if i % 2 else 0)
            h = H + (2 if i % 2 == 0 else 0)
            base = R - h
            bx, by = C + math.cos(a) * base, C + math.sin(a) * base
            tx, ty = C + math.cos(a) * (R + 1), C + math.sin(a) * (R + 1)
            sx, sy = rot(0, 1.8, a)
            poly(img, [(bx + sx, by + sy), (tx, ty), (bx - sx, by - sy)], ICE)
            line(img, bx, by, tx, ty, W if i % 2 == 0 else ICE)
            px(img, bx - sx * 0.6, by - sy * 0.6, DEEP)
        # 안쪽 작은 결정
        if f >= 2:
            for a, d in small:
                if d > R - 4:
                    continue
                x, y = C + math.cos(a) * d, C + math.sin(a) * d
                px(img, x, y, W); px(img, x + 1, y, ICE); px(img, x, y + 1, ICE)
        # 가운데 눈 결정
        L = [4, 6, 6, 5, 4, 3][f]
        for i in range(6):
            a = i / 6 * TAU
            for k in range(L + 1):
                px(img, C + math.cos(a) * k, C + math.sin(a) * k, W if k < L else ICE)
            ex, ey = C + math.cos(a) * (L - 1.5), C + math.sin(a) * (L - 1.5)
            for s in (-1, 1):
                dx, dy = rot(1.4, 1.2 * s, a)
                px(img, ex + dx, ey + dy, ICE)
        if f == 4:
            fade(img, 0.75)
        if f == 5:
            fade(img, 0.4)
        out.append(img)
    return out


# ------------------------------------------------------------------ 유령 · 그림자 군단: 두건 쓴 그림자 · 뻗는 그림자 손길
def shade():
    BODY, MID, RIM, EYE = (44, 38, 88), (76, 66, 140), (142, 132, 222), (204, 240, 255)
    out = []
    for f in range(4):
        img = blank(24, 32)
        b = [0, -1, -1, 0][f]
        ellipse(img, 12, 9 + b, 5.5, 6, MID)
        for y in range(9 + b, 27 + b):
            t = (y - 9 - b) / 18
            hw = 5.5 + t * 4
            for x in range(int(12 - hw), int(12 + hw) + 1):
                img.set(x, y, MID if x < 12 else BODY)
        # 너덜너덜한 밑자락 (흔들림)
        for i in range(5):
            x0 = 4 + i * 4
            L = 3 + ((i + f) % 3)
            for k in range(L):
                img.set(x0 + (1 if (k + f + i) % 2 else 0), 27 + b + k, BODY if k < L - 1 else MID)
        ellipse(img, 12, 10 + b, 3.4, 3.6, (18, 14, 40))       # 두건 속 어둠
        img.rect(10, 10 + b, 1, 2, EYE); img.rect(13, 10 + b, 1, 2, EYE)
        for y in range(4 + b, 26 + b):
            img.set(int(12 - (5.5 if y < 15 + b else 5.5 + (y - 9 - b) / 18 * 4)), y, RIM)
        # 뻗은 팔
        line(img, 16, 16 + b, 21, 13 + b + (f % 2), BODY)
        img.set(22, 12 + b + (f % 2), RIM); img.set(22, 14 + b + (f % 2), RIM)
        out.append(img)
    return out


def tendril():
    D, L, CLAW = (54, 46, 108), (128, 118, 220), (176, 166, 244)
    out = []
    for f in range(4):
        img = blank(32, 8)
        ph = f / 4 * TAU
        for x in range(0, 27):
            y = 3.5 + 1.8 * math.sin(x * 0.45 + ph) * min(1, x / 6)
            px(img, x, y, D); px(img, x, y + 1, D)
            if x % 3 == 0:
                px(img, x, y - 1, L)
        y = 3.5 + 1.8 * math.sin(26 * 0.45 + ph)
        for s in (-2, 0, 2):
            line(img, 26, y, 31, y + s * 1.2, CLAW)
        out.append(img)
    return out


# ------------------------------------------------------------------ 거너 · 관통 레일건: 광선을 감는 전자기 고리 (옆에서 본 고리 · 흰 바탕)
def rail_ring():
    W, L, G = (255, 255, 255), (214, 214, 224), (150, 150, 166)
    out = []
    for f in range(4):
        img = blank(16, 32)
        ry = [11, 13, 14, 14][f]
        rx = [2.2, 3.0, 3.4, 3.4][f]
        for k in range(36):
            a = k / 36 * TAU
            if abs(math.sin(a)) > 0.97:           # 위 · 아래 끝을 비워 닫힌 고리가 아니게
                continue
            px(img, 8 + math.cos(a) * rx, 16 + math.sin(a) * ry, W if math.cos(a) > 0 else L)
        # 고리에서 튀는 전기 가시
        for i in range(5):
            y = 6 + i * 5 + f
            s = 1 if (i + f) % 2 else -1
            line(img, 8 + s * rx, y, 8 + s * (rx + 2), y - 1, G)
        if f == 3:
            fade(img, 0.5)
        out.append(img)
    return out


SHEETS = {
    'fx_quake': quake,
    'fx_whipblade': whip_blade,
    'fx_lanceghost': lance_ghost,
    'fx_lancetip': lance_tip,
    'fx_poisonfog': poison_fog,
    'fx_poisonbubble': poison_bubble,
    'fx_playcard': play_card,
    'fx_jokerburst': joker_burst,
    'fx_webring': web_ring,
    'fx_silk': silk,
    'fx_javelin': javelin,
    'fx_mercury': mercury,
    'fx_blackhole': black_hole,
    'fx_magfield': mag_field,
    'fx_fwstar': firework_star,
    'fx_surveylink': lambda: chain_link((228, 178, 72), (142, 96, 36), (255, 236, 150)),
    'fx_surveystake': survey_stake,
    'fx_inkwave': ink_wave,
    'fx_inkspatter': ink_spatter,
    'fx_inkshatter': ink_shatter,
    'fx_globe': globe,
    'fx_cuckoo': cuckoo,
    'fx_pigeon': pigeon,
    'fx_sandstorm': sand_storm,
    'fx_chimering': chime_ring,
    'fx_parcelbox': parcel_box,
    'fx_parcelburst': parcel_burst,
    'fx_postbox': postbox,
    'fx_soullink': lambda: chain_link((170, 238, 255), (72, 140, 204), (240, 255, 255)),
    'fx_soulshackle': soul_shackle,
    'fx_icenova': ice_nova,
    'fx_shade': shade,
    'fx_tendril': tendril,
    'fx_railring': rail_ring,
}

if __name__ == '__main__':
    only = sys.argv[1:]
    for name, fn in SHEETS.items():
        if only and name not in only:
            continue
        save_sheet(name, fn())
