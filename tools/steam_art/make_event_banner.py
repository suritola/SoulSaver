# 스팀 공지 표시 이미지 800x450: 기존 공지 그림 둘(v1.3 · v1.2.1)에서 글자 없는 배경을 복원하고 글자 세 줄을 얹음
# 사용법: py tools/steam_art/make_event_banner.py "V1.3.2 UPDATE" "POLISH PATCH" "QUALITY OF LIFE  +  BUG FIXES" docs/steam/art/events/v1.3.2_800x450.png
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
R = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
F = os.path.join(R, "tools", "steam_art", "fonts", "Silkscreen-Bold.ttf")
E = os.path.join(R, "docs", "steam", "art", "events")
A = np.array(Image.open(os.path.join(E, "v1.3_800x450.png")).convert("RGB")).astype(int)
B = np.array(Image.open(os.path.join(E, "v1.2.1_800x450.png")).convert("RGB")).astype(int)
lum = lambda X: X @ np.array([.3, .59, .11])
bg = np.where((lum(A) <= lum(B))[..., None], A, B)
txt = np.zeros(bg.shape[:2], bool)
for c in [(255, 196, 64), (150, 230, 255), (255, 255, 255), (40, 20, 70)]:
    txt |= (bg == c).all(2)
txt[:250] = False; txt[236:243] = False; txt[378:387] = False
for y, x in zip(*np.where(txt)):
    for dx in range(1, 60):
        hit = False
        for xx in (x - dx, x + dx):
            if 0 <= xx < 800 and not txt[y, xx]:
                bg[y, x] = bg[y, xx]; hit = True; break
        if hit: break
img = Image.fromarray(bg.astype(np.uint8)); d = ImageDraw.Draw(img)
def put(t, size, col, ytop, cx=396):
    f = ImageFont.truetype(F, size); b = f.getbbox(t); w = b[2] - b[0]
    x = cx - (w + 4) // 2 - b[0]; y = ytop - b[1]
    d.text((x + 4, y + 4), t, font=f, fill=(40, 20, 70)); d.text((x, y), t, font=f, fill=col)
put(sys.argv[1], 62, (255, 196, 64), 272)
put(sys.argv[2], 29, (150, 230, 255), 334)
put(sys.argv[3], 22, (255, 255, 255), 409)
img.save(os.path.join(R, sys.argv[4]))
