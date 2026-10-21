# UI 틀 그림 (1.4.8~): 사용자 「도형에 색만 채운 대신 게임 스타일에 맞게 이미지를 만들어 써」
#   → Assets/Resources/UI/ui_button.png (48x24 · 9칸 나눔 테두리 8) · ui_panel.png (64x64 · 테두리 14) · ui_divider.png (128x8)
#   늘어나는 가장자리 · 가운데는 늘어나는 방향으로 색이 바뀌지 않게 (줄무늬 방지) · 질감 · 장식은 모서리에만
# 실행: python tools/pixelart/make_ui_frames.py
import os, re, sys, uuid, random
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import ROOT

OUT = os.path.join(ROOT, 'Assets', 'Resources', 'UI')

OUTL = (14, 9, 20, 255)
DARK = (30, 22, 42, 255)
G_D = (150, 100, 40, 255)
G = (214, 164, 74, 255)
G_L = (250, 218, 140, 255)
GEM = (196, 52, 66, 255)
GEM_L = (255, 150, 150, 255)


def lerp(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3)) + (255,)


def button():
    W, H = 48, 24
    img = Img(W, H)
    top_fill, bot_fill = (84, 70, 116), (46, 37, 66)
    for y in range(H):
        for x in range(W):
            d = min(x, W - 1 - x, y, H - 1 - y)
            corner = (x in (0, W - 1)) and (y in (0, H - 1))
            if corner:
                continue
            if d == 0:
                c = OUTL
            elif d == 1:
                # 금장 테두리: 위 밝게 · 아래 어둡게 · 옆은 가운데 금
                c = G_L if y == 1 else G_D if y == H - 2 else G
            elif d == 2:
                c = DARK
            else:
                t = (y - 3) / max(1, H - 7)
                c = lerp(top_fill, bot_fill, max(0.0, min(1.0, t)))
                if y == 3:
                    c = lerp(c, (140, 122, 178), 0.55)        # 위 하이라이트 (경사)
                elif y >= H - 5:
                    c = lerp(c, (20, 14, 30), 0.35)           # 아래 그림자
            img.set(x, y, c)
    # 모서리 금 리벳 (2x2 + 어두운 점)
    for cx, cy in ((4, 4), (W - 6, 4), (4, H - 6), (W - 6, H - 6)):
        img.set(cx, cy, G_L); img.set(cx + 1, cy, G); img.set(cx, cy + 1, G); img.set(cx + 1, cy + 1, G_D)
    return img


def panel():
    S = 64
    B = 14
    img = Img(S, S)
    rnd = random.Random(7)
    center = (22, 16, 32)
    stone_a, stone_b = (40, 31, 56), (32, 24, 46)
    for y in range(S):
        for x in range(S):
            d = min(x, S - 1 - x, y, S - 1 - y)
            corner_zone = (x < B or x >= S - B) and (y < B or y >= S - B)
            if (x in (0, S - 1)) and (y in (0, S - 1)):
                continue
            if d == 0:
                c = OUTL
            elif d == 1:
                c = G_L if (y == 1 or x == 1) else G
            elif d == 2:
                c = G_D
            elif d == 3:
                c = DARK
            elif d < B - 3:
                # 돌 테두리: 늘어나는 가장자리는 d 에만 따라 (줄무늬 방지) · 모서리만 질감
                base = lerp(stone_a, stone_b, (d - 4) / max(1, B - 8))
                if corner_zone and rnd.random() < 0.28:
                    base = lerp(base, (60, 48, 82) if rnd.random() < 0.5 else (24, 18, 34), 0.6)
                c = base
            elif d == B - 3:
                c = G_D                                       # 안쪽 금 실선
            elif d == B - 2:
                c = DARK
            else:
                c = center + (255,)
            img.set(x, y, c)
    # 모서리 장식: L 자 금 브래킷 + 붉은 보석 (네 모서리 대칭)
    def ornament(fx, fy):
        def P(x, y, c):
            img.set(x if fx > 0 else S - 1 - x, y if fy > 0 else S - 1 - y, c)
        for i in range(5, 11):
            P(i, 5, G); P(5, i, G)
        P(5, 5, G_L); P(10, 5, G_D); P(5, 10, G_D)
        for i in range(6, 9):
            P(i, 6, G_D); P(6, i, G_D)
        # 보석 (다이아몬드)
        P(8, 7, GEM); P(7, 8, GEM); P(8, 8, GEM_L); P(9, 8, GEM); P(8, 9, GEM)
        P(8, 6, OUTL); P(6, 8, OUTL); P(10, 8, OUTL); P(8, 10, OUTL)
    for fx in (1, -1):
        for fy in (1, -1):
            ornament(fx, fy)
    return img, B


def divider():
    W, H = 128, 8
    img = Img(W, H)
    cx = W // 2
    for x in range(W):
        t = 1 - abs(x - cx) / cx
        a = int(255 * min(1.0, t * 1.6))
        if a <= 0:
            continue
        img.set(x, 4, G[:3] + (a,))
        img.set(x, 3, G_L[:3] + (int(a * 0.55),))
        img.set(x, 5, G_D[:3] + (int(a * 0.7),))
    for dy in range(-3, 4):
        w = 3 - abs(dy)
        for dx in range(-w, w + 1):
            img.set(cx + dx, 4 + dy, G_L if (dx == 0 and dy <= 0) else G)
    img.set(cx, 4, GEM)
    return img


def ribbon():
    # 띠 · 리본 (96x32 · 9칸 나눔 좌우 18 · 위아래 9): 어두운 돌 띠 · 위아래 금 테두리 · 양끝 화살 깃 모양 · 끝에 금 보석
    W, H = 96, 32
    img = Img(W, H)
    top_fill, bot_fill = (58, 44, 80), (30, 22, 44)
    for y in range(H):
        for x in range(W):
            e = min(x, W - 1 - x)               # 끝에서의 거리
            notch = abs(y - (H - 1) / 2.0)      # 가운데 줄에서의 거리
            if e < 10 and notch > e + 6:        # 양끝 깃 (가운데가 튀어나온 화살촉 모양)
                continue
            edge_near = e < 10 and notch > e + 4
            if y in (2, H - 3) or edge_near:
                c = OUTL
            elif y in (3, H - 4):
                c = G_L if y == 3 else G_D
            elif y in (4, H - 5):
                c = G if y == 4 else G_D
            elif 4 < y < H - 5:
                t = (y - 5) / max(1, H - 11)
                c = lerp(top_fill, bot_fill, t)
                if y == 5:
                    c = lerp(c, (110, 92, 150), 0.4)
            else:
                continue
            img.set(x, y, c)
    for cx in (12, W - 13):          # 양끝 금 보석
        for dx, dy, c in ((0, -1, G_L), (-1, 0, G), (0, 0, GEM), (1, 0, G), (0, 1, G_D)):
            img.set(cx + dx, H // 2 + dy, c)
    return img


def shadow():
    # 창 그림자 (64x64 · 테두리 24): 가장자리로 갈수록 옅어지는 검정
    S = 64
    img = Img(S, S)
    for y in range(S):
        for x in range(S):
            d = min(x, S - 1 - x, y, S - 1 - y)
            a = int(170 * min(1.0, d / 24.0) ** 1.6)
            img.set(x, y, (0, 0, 0, a))
    return img


def vignette():
    # 화면 가장자리 어둡게 (128x128 · 늘려 씀)
    S = 128
    img = Img(S, S)
    for y in range(S):
        for x in range(S):
            dx, dy = (x - 63.5) / 63.5, (y - 63.5) / 63.5
            r = (dx * dx * 0.8 + dy * dy) ** 0.5
            a = int(255 * max(0.0, min(1.0, (r - 0.55) / 0.6)) ** 1.5 * 0.85)
            img.set(x, y, (6, 3, 12, a))
    return img


def ember():
    # 떠오르는 영혼 불티 (8x8): 금빛 점 + 옅은 빛무리
    img = Img(8, 8)
    for y in range(8):
        for x in range(8):
            d = ((x - 3.5) ** 2 + (y - 3.5) ** 2) ** 0.5
            if d < 1.2:
                img.set(x, y, (255, 236, 170, 255))
            elif d < 2.3:
                img.set(x, y, (250, 200, 110, 200))
            elif d < 3.6:
                img.set(x, y, (230, 160, 70, 70))
    return img


def meta(path, border, ppu):
    tpl = open(os.path.join(ROOT, 'Assets', 'Sprites', 'Items', 'hp_potion.png.meta'), encoding='utf-8').read()
    tpl = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
    tpl = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, tpl)
    b = border if isinstance(border, tuple) else (border, border, border, border)      # (왼 · 아래 · 오른 · 위)
    tpl = re.sub(r'spriteBorder: \{x: 0, y: 0, z: 0, w: 0\}', 'spriteBorder: {x: %d, y: %d, z: %d, w: %d}' % b, tpl)
    tpl = re.sub(r'spriteMeshType: \d', 'spriteMeshType: 0', tpl)          # 9칸 나눔은 FullRect
    open(path, 'w', encoding='utf-8', newline='\n').write(tpl)


def folder_meta(path):
    if os.path.exists(path):
        return
    open(path, 'w', encoding='utf-8', newline='\n').write(
        'fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex)


def main():
    os.makedirs(OUT, exist_ok=True)
    folder_meta(OUT + '.meta')
    p, pb = panel()
    for name, img, border, ppu in (('ui_button', button(), 8, 50), ('ui_panel', p, pb, 50), ('ui_divider', divider(), 0, 50),
                                   ('ui_ribbon', ribbon(), (18, 9, 18, 9), 50), ('ui_shadow', shadow(), 24, 50),
                                   ('ui_vignette', vignette(), 0, 100), ('ui_ember', ember(), 0, 50)):
        out = os.path.join(OUT, name + '.png')
        write(img, out)
        meta(out + '.meta', border, ppu)
    print('wrote ui_button · ui_panel · ui_divider · ui_ribbon · ui_shadow · ui_vignette · ui_ember')


if __name__ == '__main__':
    main()
