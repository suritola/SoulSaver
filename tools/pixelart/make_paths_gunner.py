# 빌드 갈래 · 거너 그림 (2026-10-19, Paths.Gunner · 안내서 8절): 32x32 도트, 남보라 외곽선
#   갈래 문장 = 갈래 색 3단 테 메달(위 왼쪽이 밝음) + 짙은 안쪽 + 갈래 상징
#   gunner_<0~4>            입문 카드 · HUD 갈래 칸
#   gunner_<갈래>_r1 · _r2  공명 I · II 알림 (아래에 마름모 하나 · 둘)
#   gunner_<갈래>_m         극의 알림 (금테 + 별)
#   gunner_x2 · gunner_x3   새 교차 알림 (두 갈래 문장을 반씩 · 테도 두 색)
#   single                  외길 카드 (공용 · 거너 담당이 하나만): 금테 + 한 줄기 길 끝의 별
#   gunner_turret · gunner_turret_fire   포탑 그림 (오른쪽을 봄 · 메달 없음) — GunTurret
#   → Assets/Resources/Icons/Paths/ (.meta 는 ability_900.png.meta 를 복사해 guid · spriteID 만 새로)
# 실행: python tools/pixelart/make_paths_gunner.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line
from make_menu_icons import OUT, retry

DST = os.path.join(OUT, 'Paths')
K = (26, 17, 44, 255)                   # 남보라 외곽선 (새 그림체와 같은 색)
W = (255, 255, 255, 255)
IN1 = (52, 46, 84, 255); IN2 = (38, 32, 64, 255); IN3 = (26, 21, 46, 255)
GL = (255, 236, 160, 255); G = (255, 200, 86, 255); GM = (234, 170, 58, 255); GD = (160, 98, 34, 255)
STEEL = (190, 198, 214, 255); STEEL_L = (232, 236, 246, 255); STEEL_D = (110, 116, 140, 255); STEEL_K = (70, 74, 98, 255)
BRASS = (255, 206, 102, 255); BRASS_D = (196, 132, 50, 255)
CX = CY = 16.0

# 갈래 색 (PathDef.color 와 같음)
COLORS = [(140, 217, 255), (255, 217, 89), (255, 128, 51), (255, 102, 89), (128, 242, 128)]


def shades(c):
    r, g, b = c
    light = (min(255, int(r + (255 - r) * 0.55)), min(255, int(g + (255 - g) * 0.55)), min(255, int(b + (255 - b) * 0.55)), 255)
    mid = (r, g, b, 255)
    midd = (int(r * 0.78), int(g * 0.78), int(b * 0.78), 255)
    dark = (int(r * 0.5), int(g * 0.5), int(b * 0.5), 255)
    return light, mid, midd, dark


def medallion(img, rim_l, rim_r=None, gold=False):
    # 테: 반지름 12.4~15 (위 왼쪽이 밝게) · rim_r 이 있으면 오른쪽 반은 그 색 (교차)
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - CX, y + 0.5 - CY
            d = math.hypot(dx, dy)
            if d > 15.0:
                continue
            if d >= 12.4:
                if gold:
                    pal = (GL, G, GM, GD)
                else:
                    pal = shades(rim_r if (rim_r is not None and dx > 0) else rim_l)
                lit = (-dx - dy) / (d * 1.414)
                c = pal[0] if lit > 0.75 else pal[1] if lit > 0.15 else pal[2] if lit > -0.6 else pal[3]
                if 13.1 < d < 13.9 and lit < 0.6:
                    c = pal[2] if c in (pal[0], pal[1]) else pal[3]
                img.set(x, y, c)
            else:
                k = (dy + d * 0.3) / 12.4
                img.set(x, y, IN1 if k < -0.25 else IN2 if k < 0.45 else IN3)
    img.set(8, 6, W); img.set(7, 7, W)


# ------------------------------------------------------------------ 갈래 상징 (메달 안 · 반지름 12 안쪽)
def sym_snipe(img, col):
    l, m, md, dk = shades(col)
    # 조준경 십자선 + 둥근 렌즈, 가운데를 꿰뚫는 저격탄 줄기
    ring(img, 16, 16, 7.0, 1.4, m)
    ring(img, 16, 16, 5.6, 0.6, dk)
    for a, b in ((16, 6.5), (16, 25.5)):
        line(img, 16, a, 16, a + (4 if a < 16 else -4), l, 1.1)
    for a in (6.5, 25.5):
        line(img, a, 16, a + (4 if a < 16 else -4), 16, l, 1.1)
    line(img, 5, 23, 27, 9, W, 1.3)              # 관통 궤적
    line(img, 6, 23.5, 26, 10.5, l, 0.7)
    disc(img, 16, 16, 1.6, (255, 90, 90, 255))   # 붉은 점
    disc(img, 15.6, 15.6, 0.6, W)
    for x, y in ((9, 18.5), (21, 12.5)):        # 꿰뚫린 자리 불티
        disc(img, x, y, 1.0, G)


def sym_ricochet(img, col):
    l, m, md, dk = shades(col)
    # 지그재그로 튕기는 탄 궤적 + 튕긴 자리 별빛 + 끝의 탄두
    pts = [(5, 20), (11, 9), (17, 21), (23, 10)]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        line(img, x0, y0, x1, y1, md, 1.8)
        line(img, x0, y0, x1, y1, l, 0.7)
    for x, y in pts[1:3]:
        for a in range(0, 360, 90):
            r = math.radians(a + 45)
            line(img, x, y, x + math.cos(r) * 2.6, y + math.sin(r) * 2.6, W, 0.8)
        disc(img, x, y, 1.3, GL)
    # 탄두 (오른쪽 위로)
    disc(img, 24.5, 8.5, 2.4, BRASS)
    disc(img, 25.4, 7.6, 1.4, GL)
    disc(img, 23.2, 9.8, 1.2, BRASS_D)
    for x, y in ((7, 25), (26, 22), (14, 26)):
        img.set(x, y, l)


def sym_overheat(img, col):
    l, m, md, dk = shades(col)
    # 붉게 달아오른 총열 + 위로 솟는 불꽃 + 열 눈금
    img.rect(7, 18, 16, 4, STEEL_D)
    img.rect(7, 18, 16, 1, STEEL)
    for x in range(14, 23):                        # 끝으로 갈수록 붉게
        t = (x - 14) / 8.0
        img.set(x, 19, (255, int(170 - 90 * t), int(60 - 40 * t), 255))
        img.set(x, 20, (230, int(110 - 60 * t), 40, 255))
    img.rect(5, 17, 3, 7, STEEL_K)
    def flame(x, y, h, c1, c2, c3):
        for yy in range(int(y - h), int(y) + 1):
            t = (y - yy) / h
            half = (1 - t) ** 0.8 * h * 0.42 if t > 0.35 else h * 0.42 * (0.75 + t * 0.7)
            for xx in range(int(round(x - half)), int(round(x + half)) + 1):
                e = abs(xx - x) / max(0.5, half)
                img.set(xx, yy, c1 if e < 0.35 and t < 0.6 else c2 if e < 0.75 else c3)
    flame(19.5, 17, 10, GL, m, dk)
    flame(13.5, 17, 6, GL, m, dk)
    disc(img, 19.5, 13, 1.0, W)
    for i in range(4):                             # 열 눈금 (아래)
        img.rect(8 + i * 4, 24, 3, 2, l if i < 3 else (255, 70, 60, 255))


def sym_shotgun(img, col):
    l, m, md, dk = shades(col)
    # 왼쪽 아래 총구에서 부채꼴로 퍼지는 산탄 6발 + 붉은 탄피
    ox, oy = 7, 22
    disc(img, ox, oy, 2.2, (255, 220, 140, 255))   # 총구 섬광
    disc(img, ox, oy, 1.1, W)
    for i in range(6):
        a = math.radians(-70 + i * 12)
        ex, ey = ox + math.cos(a) * 17, oy + math.sin(a) * 17
        line(img, ox + math.cos(a) * 3, oy + math.sin(a) * 3, ox + math.cos(a) * 12, oy + math.sin(a) * 12, m, 1.0)
        disc(img, ex, ey, 1.4, l)
        disc(img, ex - 0.3, ey - 0.3, 0.6, W)
    # 탄피 (오른쪽 아래)
    img.rect(20, 21, 6, 4, m)
    img.rect(20, 21, 6, 1, l)
    img.rect(18, 21, 2, 4, BRASS)
    img.rect(18, 21, 1, 4, BRASS_D)
    img.rect(25, 22, 1, 3, dk)


def turret_shape(img, col, firing=False, scale=1.0, ox=0.0, oy=0.0):
    l, m, md, dk = shades(col)
    def S(v): return v * scale
    # 세 다리 받침
    line(img, ox + S(10), oy + S(26), ox + S(14), oy + S(20), STEEL_K, max(1.0, S(1.6)))
    line(img, ox + S(22), oy + S(26), ox + S(18), oy + S(20), STEEL_K, max(1.0, S(1.6)))
    line(img, ox + S(16), oy + S(27), ox + S(16), oy + S(20), STEEL_D, max(1.0, S(1.6)))
    # 몸통 (둥근 포탑)
    disc(img, ox + S(16), oy + S(17), S(5.2), md)
    disc(img, ox + S(15.2), oy + S(16.2), S(3.8), m)
    disc(img, ox + S(14.2), oy + S(15.2), S(1.6), l)
    img.rect(int(ox + S(11)), int(oy + S(20)), max(1, int(S(10))), max(1, int(S(2))), dk)
    # 총열 (오른쪽)
    img.rect(int(ox + S(19)), int(oy + S(14)), max(1, int(S(9))), max(1, int(S(3))), STEEL)
    img.rect(int(ox + S(19)), int(oy + S(14)), max(1, int(S(9))), 1, STEEL_L)
    img.rect(int(ox + S(27)), int(oy + S(13)), max(1, int(S(2))), max(1, int(S(5))), STEEL_D)
    # 조준 눈 (빛)
    disc(img, ox + S(16.5), oy + S(16.5), S(1.1), (255, 255, 200, 255))
    if firing:
        fx, fy = ox + S(30.5), oy + S(15.5)
        disc(img, fx, fy, S(2.6), (255, 230, 140, 255))
        disc(img, fx, fy, S(1.3), W)


def sym_turret(img, col):
    turret_shape(img, col, False, 0.82, 2.6, 1.8)
    l, m, md, dk = shades(col)
    for x, y in ((24, 9), (26, 11)):                # 탄 두 발
        disc(img, x, y, 0.9, l)


SYMS = [sym_snipe, sym_ricochet, sym_overheat, sym_shotgun, sym_turret]


def sym_single(img, col):
    # 한 줄기 길이 멀리 뻗고 끝에 별 (외길)
    for y in range(9, 27):
        t = (y - 9) / 17.0
        half = 1.2 + 4.5 * t
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, (210, 180, 120, 255) if x < 16 - half * 0.3 else (176, 140, 84, 255) if x < 16 + half * 0.4 else (130, 96, 56, 255))
    for y in range(12, 27, 4):                      # 가운데 점선
        img.rect(16, y, 1, 2, GL)
    star(img, 16, 8, 4.4, GL, G)


def star(img, cx, cy, r, c1, c2):
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        rr = r if i % 2 == 0 else r * 0.45
        pts.append((cx + math.cos(a) * rr, cy + math.sin(a) * rr))
    for y in range(32):
        for x in range(32):
            px, py = x + 0.5, y + 0.5
            inside = False
            j = len(pts) - 1
            for i in range(len(pts)):
                xi, yi = pts[i]; xj, yj = pts[j]
                if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi + 1e-9) + xi:
                    inside = not inside
                j = i
            if inside:
                d = math.hypot(px - cx, py - cy)
                img.set(x, y, c1 if d < r * 0.4 else c2)


# ------------------------------------------------------------------ 테두리 · 조립
def outline(img):
    src = [[img.px[y][x] for x in range(img.w)] for y in range(img.h)]
    for y in range(img.h):
        for x in range(img.w):
            if src[y][x][3] > 0: continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < img.w and 0 <= yy < img.h and src[yy][xx][3] > 0:
                    img.px[y][x] = K
                    break


def symbol_outline(img, sym):
    for y in range(32):
        for x in range(32):
            if sym.px[y][x][3] > 0: continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                c = sym.get(x + dx, y + dy)
                if c[3] > 160 and math.hypot(x + 0.5 - CX, y + 0.5 - CY) < 12.4:
                    img.set(x, y, K)
                    break


def clip(sym, keep=None):
    for y in range(32):
        for x in range(32):
            if math.hypot(x + 0.5 - CX, y + 0.5 - CY) >= 12.4 or (keep is not None and not keep(x, y)):
                sym.px[y][x] = (0, 0, 0, 0)


def pips(img, n, col):
    # 아래 테 위에 마름모 n 개 (공명 I · II)
    l, m, md, dk = shades(col)
    xs = [16] if n == 1 else [12.5, 19.5]
    for cx in xs:
        for y in range(25, 32):
            for x in range(32):
                if abs(x + 0.5 - cx) + abs(y + 0.5 - 28.5) <= 3.0:
                    img.set(x, y, l if (x + 0.5 - cx) + (y + 0.5 - 28.5) < -0.5 else m)
        img.set(int(cx) - 1, 27, W)


def badge(path, tier):
    col = COLORS[path]
    img = Img(32, 32)
    medallion(img, col, gold=(tier == 'm'))
    sym = Img(32, 32)
    SYMS[path](sym, col)
    clip(sym)
    symbol_outline(img, sym)
    img.paste(sym, 0, 0)
    if tier == 'r1': pips(img, 1, col)
    if tier == 'r2': pips(img, 2, col)
    if tier == 'm':
        star(img, 25.5, 25.5, 4.6, W, GL)
        star(img, 25.5, 25.5, 3.4, GL, G)
    outline(img)
    return img


def cross(a, b):
    img = Img(32, 32)
    medallion(img, COLORS[a], COLORS[b])
    sa, sb = Img(32, 32), Img(32, 32)
    SYMS[a](sa, COLORS[a])
    SYMS[b](sb, COLORS[b])
    clip(sa, lambda x, y: x + y < 31)               # 왼쪽 위 반: 앞 갈래
    clip(sb, lambda x, y: x + y > 31)               # 오른쪽 아래 반: 뒤 갈래
    symbol_outline(img, sa); symbol_outline(img, sb)
    img.paste(sa, 0, 0); img.paste(sb, 0, 0)
    line(img, 9, 23, 23, 9, W, 1.0)                  # 가운데 빛줄기 (교차)
    disc(img, 16, 16, 1.6, GL)
    outline(img)
    return img


def single():
    img = Img(32, 32)
    medallion(img, None, gold=True)
    sym = Img(32, 32)
    sym_single(sym, None)
    clip(sym)
    symbol_outline(img, sym)
    img.paste(sym, 0, 0)
    outline(img)
    return img


def turret(firing):
    img = Img(32, 32)
    turret_shape(img, COLORS[4], firing)
    outline(img)
    return img


def save(img, name, tpl):
    os.makedirs(DST, exist_ok=True)
    path = os.path.join(DST, name + '.png')
    meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
    meta = re.sub(r'spriteID: [0-9a-f]{32}', 'spriteID: ' + uuid.uuid4().hex, meta, count=1)
    if not os.path.exists(path + '.meta'):           # 메타를 먼저 (편집기가 기본 설정 메타를 만들지 않게) · 이미 있으면 guid 그대로
        retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta))
    retry(lambda: write(img, path))
    print('그림 저장:', name)


def main():
    tpl = open(os.path.join(OUT, 'ability_900.png.meta'), encoding='utf-8').read()
    for p in range(5):
        save(badge(p, None), 'gunner_%d' % p, tpl)
        for t in ('r1', 'r2', 'm'):
            save(badge(p, t), 'gunner_%d_%s' % (p, t), tpl)
    save(cross(0, 2), 'gunner_x2', tpl)              # 철갑 소이탄 (관통 저격 × 화염 과열)
    save(cross(2, 3), 'gunner_x3', tpl)              # 화염 돌진 (화염 과열 × 산탄 돌격)
    save(single(), 'single', tpl)
    save(turret(False), 'gunner_turret', tpl)
    save(turret(True), 'gunner_turret_fire', tpl)


if __name__ == '__main__':
    main()
