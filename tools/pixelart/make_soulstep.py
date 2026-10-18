# 초월 「영혼불 발자국」 전용 도트 — TranscendRules.TickEmbers 가 씀 (예전: 산성 웅덩이 fx_puddle 을 주황으로 물들여 씀)
#   fx_soulstep  32 x 32 · 6프레임: 보랏빛으로 그을린 자국 + 가운데서 넘실대는 푸른 영혼불 (흰 하늘 속 → 하늘 → 청록 → 보라 끝)
#   한 판에 최대 26개가 깔리므로 그림 하나 · 루프 애니만 (불티 · 자식 오브젝트 없음)
# 실행: python tools/pixelart/make_soulstep.py
import math, os, random, sys
sys.path.insert(0, os.path.dirname(__file__))
from make_fx2 import save_sheet
from png import Img

W = H = 32
SCORCH0 = (28, 20, 44, 200)     # 그을린 자국 (짙은 보라 · 반투명)
SCORCH1 = (46, 32, 72, 170)
RIM = (86, 70, 150, 150)        # 가장자리 희미한 빛
CORE = (240, 252, 255, 255)     # 불 속 (흰 하늘)
HOT = (150, 230, 255, 255)
MID = (70, 190, 235, 255)
TIP = (120, 90, 220, 235)       # 불 끝 (보라)
MOTE = (190, 245, 255, 255)


def edge(theta, seed):
    return 12.5 + 1.3 * math.sin(4 * theta + seed) + 0.9 * math.sin(7 * theta + seed * 1.7)


def scorch(rnd):
    base = Img(W, H)
    for y in range(H):
        for x in range(W):
            dx, dy = x + 0.5 - 16, (y + 0.5 - 20) * 1.6        # 땅에 눕힌 타원 (아래쪽 · 납작)
            d = math.hypot(dx, dy)
            r = edge(math.atan2(dy, dx), 0.9)
            if d > r:
                continue
            t = d / r
            c = SCORCH0 if t < 0.55 else SCORCH1 if t < 0.85 else RIM
            if t > 0.85 and (x + y) % 2 == 0:
                continue                                       # 가장자리 디더로 녹아 사라짐
            if rnd.random() < 0.04 and t < 0.85:
                c = RIM
            base.set(x, y, c)
    return base


def flame(img, cx, base_y, height, half0, ph, f):
    for y in range(H):
        up = base_y - y
        if up < 0 or up > height:
            continue
        t = up / height
        half = half0 * (1 - t) ** 0.7 * (0.85 + 0.15 * math.sin(ph * 2 + t * 6))
        sx = cx + 1.2 * math.sin(ph + t * 4) * t * t
        for x in range(W):
            k = abs(x + 0.5 - sx) / max(0.5, half)
            if k > 1:
                continue
            core = (1 - t) * (1 - k)
            c = CORE if core > 0.55 else HOT if core > 0.34 else MID if core > 0.14 or t < 0.45 else TIP
            if t > 0.8 and (x + y + f) % 2 == 0:
                continue
            img.set(x, y, c)


def frames():
    rnd = random.Random(11)
    base = scorch(rnd)
    out = []
    for f in range(6):
        ph = f / 6 * math.tau
        img = Img(W, H)
        for y in range(H):
            for x in range(W):
                img.set(x, y, base.get(x, y))
        # 큰 불꽃 하나 + 양옆 작은 불꽃 둘 (서로 다른 박자로 넘실댐)
        flame(img, 16.0, 21, 15 + 3 * math.sin(ph), 5.0, ph, f)
        flame(img, 10.5, 22, 8 + 2.5 * math.sin(ph + 2.1), 3.0, ph + 2.1, f)
        flame(img, 21.5, 22, 9 + 2.5 * math.sin(ph + 4.2), 3.2, ph + 4.2, f)
        # 위로 떠오르는 영혼 티끌 (프레임마다 한 칸씩 올라감)
        for i, (mx, my0) in enumerate(((13, 12), (19, 9), (16, 5))):
            my = (my0 - f - i * 2) % 14 + 1
            img.set(mx + (1 if (f + i) % 3 == 0 else 0), my, MOTE)
        out.append(img)
    return out


if __name__ == "__main__":
    save_sheet("fx_soulstep", frames())
