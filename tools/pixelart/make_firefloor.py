# 불 장판 (화염 지대 · 유탄 불 · 지옥의 군주 불 웅덩이) 고품질 도트 — FireGround.cs 가 씀
#   fx_firefloor    64 x 64 · 2프레임: 그을린 땅 + 빛나는 금 (2프레임째 금이 더 밝음 · 실행 중 번갈아 깜빡임)
#   fx_flametongue  16 x 24 · 6프레임: 땅에서 넘실대는 불꽃 혀 (흰 노랑 속 → 노랑 → 주황 → 붉은 끝 · 어두운 외곽선 없음, 빛나는 불)
# 실행: python tools/pixelart/make_firefloor.py
import math, os, random, sys
sys.path.insert(0, os.path.dirname(__file__))
from make_fx2 import save_sheet
from png import Img

CHAR0 = (34, 18, 16, 255)      # 숯 (가장 어두움)
CHAR1 = (58, 30, 22, 255)
CHAR2 = (86, 44, 28, 255)      # 그을린 가장자리
EMBER = (196, 64, 24, 255)     # 꺼져 가는 불씨
CRACK = (255, 128, 36, 255)    # 빛나는 금
CRACKHOT = (255, 214, 110, 255)
ASH = (120, 96, 84, 255)


def edge_radius(theta, seed):
    r = 27.5
    r += 2.4 * math.sin(5 * theta + seed) + 1.6 * math.sin(9 * theta + seed * 2.1) + 1.0 * math.sin(14 * theta + seed * 0.7)
    return r


def firefloor():
    rnd = random.Random(7)
    seed = 1.3
    base = Img(64, 64)
    inside = [[False] * 64 for _ in range(64)]
    for y in range(64):
        for x in range(64):
            dx, dy = x + 0.5 - 32, y + 0.5 - 32
            d = math.hypot(dx, dy)
            r = edge_radius(math.atan2(dy, dx), seed)
            if d > r:
                continue
            inside[y][x] = True
            t = d / r
            # 가운데일수록 짙은 숯 · 가장자리는 그을린 갈색 + 바깥 몇 칸은 성긴 디더
            c = CHAR0 if t < 0.45 else CHAR1 if t < 0.78 else CHAR2
            if t > 0.9 and (x + y) % 2 == 0:
                c = CHAR1
            if rnd.random() < 0.035:
                c = ASH if t > 0.5 else CHAR2
            base.set(x, y, c)
    # 꺼져 가는 불씨 점
    for _ in range(70):
        a = rnd.random() * math.tau; d = rnd.random() ** 0.6 * 24
        x, y = int(32 + math.cos(a) * d), int(32 + math.sin(a) * d)
        if 0 <= x < 64 and 0 <= y < 64 and inside[y][x]:
            base.set(x, y, EMBER)
    # 빛나는 금: 가운데 근처에서 바깥으로 꺾이며 뻗는 줄 9개 (가지 몇 개)
    cracks = []
    for i in range(9):
        a = i / 9 * math.tau + rnd.uniform(-0.25, 0.25)
        x, y = 32 + math.cos(a) * rnd.uniform(2, 6), 32 + math.sin(a) * rnd.uniform(2, 6)
        path = []
        length = rnd.uniform(14, 24)
        for s in range(int(length)):
            a += rnd.uniform(-0.45, 0.45)
            x += math.cos(a); y += math.sin(a)
            path.append((int(x), int(y), s / length))
            if s == int(length * 0.5) and rnd.random() < 0.6:
                b = a + rnd.choice((-1, 1)) * rnd.uniform(0.6, 1.1)
                bx, by = x, y
                for k in range(int(length * 0.35)):
                    b += rnd.uniform(-0.4, 0.4)
                    bx += math.cos(b); by += math.sin(b)
                    path.append((int(bx), int(by), 0.6 + k / length))
        cracks.append(path)
    frames = []
    for f in range(2):
        img = Img(64, 64)
        for y in range(64):
            for x in range(64):
                img.set(x, y, base.get(x, y))
        for path in cracks:
            for (x, y, t) in path:
                if not (0 <= x < 64 and 0 <= y < 64) or not inside[y][x]:
                    continue
                hot = t < (0.55 if f == 1 else 0.35)
                img.set(x, y, CRACKHOT if hot else CRACK)
                # 금 둘레 붉은 열기 (한 칸)
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < 64 and 0 <= ny < 64 and inside[ny][nx]:
                        cur = img.get(nx, ny)
                        if cur not in (CRACK, CRACKHOT):
                            img.set(nx, ny, EMBER)
        frames.append(img)
    save_sheet("fx_firefloor", frames)


def flame_frame(f):
    W, H = 16, 24
    img = Img(W, H)
    ph = f / 6 * math.tau
    height = 19 + 3 * math.sin(ph)              # 넘실대는 키
    sway = 1.6 * math.sin(ph + 0.8)              # 끝이 좌우로 흔들림
    for y in range(H):
        up = (H - 1 - y)                         # 바닥에서 위로
        if up > height:
            continue
        t = up / height                          # 0 바닥 → 1 끝
        half = 6.2 * (1 - t) ** 0.75 * (0.85 + 0.15 * math.sin(ph * 2 + t * 6))
        if t < 0.12:
            half *= 0.75 + t * 2                 # 바닥은 살짝 좁게 (땅에서 피어오름)
        cx = 7.5 + sway * t * t + 0.6 * math.sin(ph * 3 + t * 9) * t
        for x in range(W):
            dx = abs(x + 0.5 - cx)
            if dx > half:
                continue
            k = dx / max(0.5, half)              # 0 가운데 → 1 가장자리
            core = (1 - t) * (1 - k)
            if core > 0.55:
                c = (255, 248, 200, 255)
            elif core > 0.34:
                c = (255, 214, 90, 255)
            elif core > 0.16 or t < 0.5:
                c = (250, 140, 40, 255)
            else:
                c = (210, 64, 28, 255)
            # 끝 · 가장자리는 성긴 디더로 녹아 사라짐
            if t > 0.82 and (x + y + f) % 2 == 0:
                continue
            img.set(x, y, c)
    # 떨어져 나간 불똥 한두 개
    sx = int(7.5 + sway + 2 * math.sin(ph * 1.7)); sy = int(H - 1 - height - 2 - (f % 3))
    if 0 <= sx < W and 0 <= sy < H:
        img.set(sx, sy, (255, 200, 80, 255))
    return img


def flametongue():
    save_sheet("fx_flametongue", [flame_frame(f) for f in range(6)])


if __name__ == "__main__":
    firefloor()
    flametongue()
