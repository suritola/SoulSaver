# V2.0 대규모 업데이트 공지 표시 이미지 800x450 — 이전 공지(어두운 성 · 금 띠)와 다르게 노을 사막 + 새 영웅 5명
# 실행 (저장소 루트에서): py tools/steam_art/make_v2_banner.py
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import storeart_lib as L
import storeart_logo as G

W, H, P = 800, 450, 2          # 배경은 2화소 = 1칸으로 그려 키움 (도트 느낌)
w, h = W // P, H // P
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16

# ── 하늘: 위 보라 → 주황 → 지평선 옅은 노랑, 4×4 디더로 단계 섞기
SKY = [(38, 18, 62), (74, 28, 88), (132, 40, 96), (196, 70, 84), (236, 120, 70), (252, 178, 92), (255, 222, 140)]
HOR = 132                      # 지평선 (배경 칸 기준)
yy, xx = np.mgrid[0:h, 0:w]
t = np.clip(yy / HOR, 0, 1) * (len(SKY) - 1)
t = np.floor(t + BAYER[yy % 4, xx % 4] - 0.5).clip(0, len(SKY) - 1).astype(int)
bg = np.array(SKY)[t].astype(np.uint8)

# ── 해: 지평선 위 큰 원, 가로 줄무늬로 아래가 끊기는 레트로 해
cx, cy, R = w // 2, HOR - 6, 58
d = np.hypot(xx - cx, (yy - cy) * 1.0)
sun = d <= R
cut = (yy > cy - 20) & (((yy - (cy - 20)) % 9) < ((yy - (cy - 20)) // 9 + 1))
SUNC = [(255, 244, 196), (255, 216, 120), (252, 170, 84)]
st = np.clip((yy - (cy - R)) / (2 * R) * 3, 0, 2.99).astype(int)
m = sun & ~cut
bg[m] = np.array(SUNC)[st[m]]
glow = (d > R) & (d < R + 10) & (yy < HOR)
bg[glow & ((xx + yy) % 2 == 0)] = (255, 200, 120)

# ── 모래 언덕 세 겹 (뒤 → 앞으로 진하게)
DUNES = [((222, 150, 92), (196, 120, 76), HOR - 4, 7, 0.031, 1.0),
         ((200, 118, 70), (170, 92, 58), HOR + 14, 9, 0.022, 2.3),
         ((150, 76, 54), (118, 54, 46), HOR + 38, 12, 0.017, 4.1)]
for lit, shade, base, amp, fr, ph in DUNES:
    top = base - amp * np.sin(np.arange(w) * fr + ph) - 3 * np.sin(np.arange(w) * fr * 2.7 + ph * 2)
    for x in range(w):
        t0 = int(top[x]); slope = top[(x + 1) % w] - top[x]
        bg[t0:, x] = shade if slope > 0 else lit
        bg[t0, x] = tuple(min(255, c + 30) for c in lit)
img = Image.fromarray(bg, "RGB").resize((W, H), Image.NEAREST).convert("RGBA")


def paste(im, s, x, y):         # 정수 배율로 키워 (x, y) = 왼쪽 위
    im = L.up(im, s)
    img.alpha_composite(im, (int(x), int(y)))
    return im.size


# ── 사막 소품 (선인장 · 해골 · 돌)
DES = os.path.join(L.RES, "Desert")
prop = lambda n: L.trim(L.load(os.path.join(DES, n)))
for n, s, x, yb in [("prop_cactus.png", 4, 40, 300), ("prop_cactus_small.png", 4, 712, 296),
                    ("prop_skull.png", 3, 118, 330), ("prop_rocks.png", 3, 640, 334)]:
    im = prop(n); paste(im, s, x, yb - im.size[1] * s)

# ── 새 영웅 5명: 32칸 첫 장(대기), 4배, 언덕 위에 줄지어
NEW = ["wrestler", "foreman", "puppeteer", "undertaker", "photographer"]
heroes = [L.outline(L.cell(os.path.join(L.RES, "Characters", n + ".png"), 0, 0)) for n in NEW]
S, GAP, FEET = 4, 26, 318
tw = sum(hh.size[0] * S for hh in heroes) + GAP * (len(heroes) - 1)
x = (W - tw) // 2
for i, hh in enumerate(heroes):
    lift = 10 if i == 2 else (4 if i in (1, 3) else 0)      # 가운데가 살짝 높게 (아치)
    sh = Image.new("RGBA", (hh.size[0] * S, 8), (60, 24, 34, 120))
    img.alpha_composite(sh, (x, FEET - 4))
    paste(hh, S, x, FEET - hh.size[1] * S - lift)
    x += hh.size[0] * S + GAP

# ── 로고 (위 가운데)
lg = G.logo(); ls = 4
paste(lg, ls, (W - lg.size[0] * ls) // 2, 14)

# ── 아래 띠: 짙은 자주 바탕 + 청록 제목 (이전 공지의 금 띠와 다르게)
dr = ImageDraw.Draw(img)
dr.rectangle((0, 340, W, H), fill=(44, 16, 40, 255))
dr.rectangle((0, 336, W, 339), fill=(64, 214, 200, 255))
F = os.path.join(os.path.dirname(os.path.abspath(__file__)), "fonts", "Silkscreen-Bold.ttf")


def put(text, size, col, ytop, shadow=(16, 6, 20)):
    f = ImageFont.truetype(F, size); b = f.getbbox(text); tw = b[2] - b[0]
    x0 = (W - tw) // 2 - b[0]; y0 = ytop - b[1]
    dr.text((x0 + 4, y0 + 4), text, font=f, fill=shadow); dr.text((x0, y0), text, font=f, fill=col)


put("V2.0  MAJOR UPDATE", 50, (110, 240, 222), 352)
put("5 NEW HEROES  +  54 SKINS  +  2 MODES", 22, (255, 214, 150), 414)

out = os.path.join(L.ROOT, "docs", "steam", "art", "events", "v2.0_800x450.png")
img.convert("RGB").save(out)
print(out)
