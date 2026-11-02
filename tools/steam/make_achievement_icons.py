# 새 업적 아이콘 (1.8.6~): 게임 도트를 StoreArtTests와 같은 구도로 합성 — 금테 · 색 배경 · 가운데 도트
# 달성 <API>.png, 미달성 <API>_locked.png (회색 · 밝기 55%), 256×256
# Steamworks에는 JPG로 올림: PowerShell System.Drawing 으로 .jpg 변환 후 PNG는 지움
# 실행 (저장소 루트에서): python tools/steam/make_achievement_icons.py
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'pixelart'))
from png import Img, read, write

RES = 'Assets/Resources/'
OUT = 'docs/steam/art/achievements/'
SIZE, INSET, BOX = 256, 10, int(256 * 0.66)
FRAME = (217, 184, 102, 255)          # Color(0.85, 0.72, 0.4)


def frame(path, index=None):
    """가로로 이어진 시트에서 index번째 정사각 칸 (index 없으면 통째로)"""
    src = read(RES + path)
    if index is None:
        return src
    h = src.h
    out = Img(h, h)
    for y in range(h):
        for x in range(h):
            out.set(x, y, src.get(index * h + x, y))
    return out


def soul_tree(steps):
    """영혼 트리 모양: 가운데 칸에서 여섯 가지가 뻗고 가지마다 steps칸 (배운 칸 = 금색)"""
    import math
    n = 12 + steps * 10
    img = Img(n, n)
    gold, line = (245, 212, 120, 255), (190, 160, 90, 255)
    c = n // 2

    def node(x, y, r):
        for yy in range(-r, r + 1):
            for xx in range(-r, r + 1):
                if xx * xx + yy * yy <= r * r:
                    img.set(x + xx, y + yy, gold)

    for k in range(6):
        a = math.pi / 2 + k * math.pi / 3
        px, py = c, c
        for s in range(1, steps + 1):
            x, y = c + round(math.cos(a) * s * 5), c - round(math.sin(a) * s * 5)
            for t in range(11):
                img.set(round(px + (x - px) * t / 10), round(py + (y - py) * t / 10), line)
            node(x, y, 1)
            px, py = x, y
    node(c, c, 2)
    return img


ICONS = [
    ('ACH_WEAPON_EVOLVE', frame('Weapons/weapon_pistol.png'), (0.40, 0.22, 0.10)),
    ('ACH_TREE_20', soul_tree(2), (0.12, 0.30, 0.16)),
    ('ACH_TREE_40', soul_tree(4), (0.08, 0.26, 0.30)),
    ('ACH_REROLL', frame('FX/fx_card.png', 0), (0.22, 0.14, 0.32)),
    ('ACH_SKIN', frame('Icons/menu_skin.png'), (0.34, 0.16, 0.30)),
    ('ACH_SKIN_LEGEND', frame('Icons/skin_starlight.png'), (0.50, 0.38, 0.10)),
    # 2.0.8: 스킬 진화 · 보스 결계
    ('ACH_SKILL_EVOLVE', frame('Icons/ability_170.png'), (0.30, 0.16, 0.42)),
    ('ACH_SKILL_EVOLVE_3', frame('Icons/menu_evolution.png'), (0.42, 0.20, 0.50)),
    ('ACH_SKILL_EVOLVE_15', frame('Icons/ability_205.png'), (0.48, 0.36, 0.10)),
    ('ACH_BARRIER_SURVIVE', frame('FX/fx_rune.png', 0), (0.10, 0.08, 0.20)),
    ('ACH_BARRIER_NOHIT', frame('FX/fx_warn.png', 0), (0.12, 0.26, 0.30)),
    ('ACH_BARRIER_BREAK', frame('FX/fx_markskull.png', 0), (0.40, 0.08, 0.10)),
    # 2.1.1 4장 영혼의 심연
    ('ACH_ENTER_ABYSS', frame('Abyss/wisp_0.png'), (0.10, 0.08, 0.26)),
    ('ACH_MIRROR_QUICK', frame('Abyss/mirrorlord_0.png'), (0.16, 0.22, 0.36)),
    # 2.0.0 사막 비밀 영웅 해금 · 영혼 릴레이 · 역류
    ('UNLOCK_WRESTLER', frame('Characters/wrestler.png', 0), (0.46, 0.24, 0.10)),
    ('UNLOCK_FOREMAN', frame('Characters/foreman.png', 0), (0.44, 0.34, 0.08)),
    ('UNLOCK_PUPPETEER', frame('Characters/puppeteer.png', 0), (0.34, 0.12, 0.34)),
    ('UNLOCK_UNDERTAKER', frame('Characters/undertaker.png', 0), (0.12, 0.14, 0.18)),
    ('UNLOCK_PHOTOGRAPHER', frame('Characters/photographer.png', 0), (0.12, 0.30, 0.34)),
    ('ACH_RELAY_FIRST', frame('Icons/mode_relay.png'), (0.10, 0.28, 0.34)),
    ('ACH_RELAY_DEV', frame('Icons/mode_relay.png'), (0.12, 0.30, 0.14)),
    ('ACH_RELAY_ALLBOSS', frame('Icons/mode_relay.png'), (0.44, 0.10, 0.12)),
    ('ACH_RELAY_FIRSTHERO', frame('Icons/mode_relay.png'), (0.42, 0.30, 0.08)),
    ('ACH_REFLUX_CLEAR', frame('Icons/mode_reflux.png'), (0.26, 0.14, 0.40)),
    ('ACH_REFLUX_FAST', frame('Icons/mode_reflux.png'), (0.08, 0.22, 0.42)),
    ('ACH_REFLUX_NAKED', frame('Icons/mode_reflux.png'), (0.30, 0.30, 0.32)),
]
# ONLY=ACH_A,ACH_B 로 일부만 다시 만듦 (나머지 JPG 를 건드리지 않게)
ONLY = [a for a in os.environ.get('ONLY', '').split(',') if a]


def icon(sprite, bg):
    img = Img(SIZE, SIZE, FRAME)
    img.rect(INSET, INSET, SIZE - 2 * INSET, SIZE - 2 * INSET, tuple(int(c * 255) for c in bg) + (255,))
    # 도트가 뭉개지지 않게 정수 배로 키움
    s = max(1, min(BOX // sprite.w, BOX // sprite.h))
    ox, oy = (SIZE - sprite.w * s) // 2, (SIZE - sprite.h * s) // 2
    for y in range(sprite.h):
        for x in range(sprite.w):
            c = sprite.get(x, y)
            if c[3] == 0:
                continue
            for dy in range(s):
                for dx in range(s):
                    img.blend(ox + x * s + dx, oy + y * s + dy, c)
    return img


def gray(img):
    out = Img(img.w, img.h)
    for y in range(img.h):
        for x in range(img.w):
            r, g, b, a = img.get(x, y)
            l = int((0.299 * r + 0.587 * g + 0.114 * b) * 0.55)
            out.set(x, y, (l, l, l, 255))
    return out


for api, sprite, bg in ICONS:
    if ONLY and api not in ONLY:
        continue
    done = icon(sprite, bg)
    write(done, OUT + api + '.png')
    write(gray(done), OUT + api + '_locked.png')
    print(api, sprite.w, sprite.h)


# PNG -> JPG (PIL), PNG 는 지움
try:
    from PIL import Image
    for api, _, _ in ICONS:
        if ONLY and api not in ONLY:
            continue
        for suf in ('', '_locked'):
            f = OUT + api + suf
            if os.path.exists(f + '.png'):
                Image.open(f + '.png').convert('RGB').save(f + '.jpg', quality=95)
                os.remove(f + '.png')
except ImportError:
    print('PIL 없음: PNG 를 JPG 로 직접 바꿀 것')
