# 스팀 상점 캡슐 · 라이브러리 · 아이콘 그림 (정식판 + 체험판) — 게임 안 도트 그림으로 합성
#
# 사용법: py -3 tools/steam_art/make_store_art.py              (전부)
#         py -3 tools/steam_art/make_store_art.py header demo    (이름에 header 가 들어간 것 · 체험판만 — 걸러 보기)
# 결과:   docs/steam/art/capsules/*  (정식판), docs/steam/art/demo/*  (체험판)
# 필요:   pip install pillow numpy
#
# 도트 그림(영웅 · 적 · 보스 · 이펙트 · 타일 · 소품)은 모두 Assets 의 실제 게임 그림을 정수 배율 nearest 로 키움.
# 빛 · 그림자 · 안개 · 비네트만 매끈하게 더함. 로고는 storeart_logo.py 의 손으로 찍은 도트 글자.
import os
import random
import sys

import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, os.path.dirname(__file__))
import storeart_lib as L  # noqa: E402
import storeart_logo as LG  # noqa: E402

OUT_FULL = os.path.join(L.ROOT, "docs", "steam", "art", "capsules")
OUT_DEMO = os.path.join(L.ROOT, "docs", "steam", "art", "demo")

CAVE = os.path.join(L.SPR, "tileset_cave_32.png")
OBJ_CAVE = os.path.join(L.SPR, "objects_cave.png")

SOUL_C = (90, 200, 255)
GOLD_C = (255, 190, 80)
FIRE_C = (255, 110, 40)
VIOLET = (150, 80, 255)

# 괴물 떼 (같은 이름을 여러 번 넣어 비율 조절) — 체험판은 1장 지하 묘역 적만
HORDE_DEMO = ["skeleton", "skeleton", "zombie", "zombie", "wraith"]
HORDE_FULL = ["skeleton", "skeleton", "zombie", "zombie", "wraith", "imp", "imp", "hound", "knight", "flameskull",
              "slime", "mushroom", "wolf", "reaper", "wisp"]


# ================================================================= 장면 재료
def floor(cv, scale, dark, seed, sheet=CAVE):
    tiles = [(L.tile(sheet, 0, 0), 3), (L.tile(sheet, 1, 0), 3), (L.tile(sheet, 2, 0), 2), (L.tile(sheet, 0, 1), 2), (L.tile(sheet, 1, 1), 2)]
    cv.tile_floor(tiles, scale, seed=seed, dark=dark)


def brazier(cv, scale, x, y, color=SOUL_C):
    img = L.obj(OBJ_CAVE, 7)                        # 푸른 영혼 화로
    w, h = cv.sprite(img, scale, x, y, mul=0.95)
    cv.light(x, y - h * 0.8, 70 * scale / 3, color, 0.55)
    cv.light(x, y - h * 0.8, 18 * scale / 3, (220, 250, 255), 0.5)


def unit(cv, img, scale, x, y, face_left=False, dark=1.0, rim_c=None, rim_side="right", shadow=True):
    if face_left:
        img = L.flip(img)
    if rim_c:
        img = L.rim(img, rim_c, rim_side, 0.8)
    if shadow:
        cv.shadow_ellipse(x, y - scale, img.width * scale * 0.48, 3.2 * scale, 0.6)
    cv.sprite(img, scale, x, y, mul=dark)


def horde(cv, rnd, kinds, n, box, scales, face_left=True, dark=(0.38, 1.0), rim_c=(255, 140, 70), avoid=None):
    # box 안에 n마리, 위(먼 곳)일수록 작고 어둡게 · 아래일수록 크고 밝게. y 순서로 그려 겹침이 자연스럽게
    x0, y0, x1, y1 = box
    pts = []
    tries = 0
    while len(pts) < n and tries < n * 60:
        tries += 1
        x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
        if avoid and any(ax0 < x < ax1 and ay0 < y < ay1 for ax0, ay0, ax1, ay1 in avoid):
            continue
        t = (y - y0) / max(1, y1 - y0)
        sc = scales[min(len(scales) - 1, int(t * len(scales)))]
        if any(abs(x - px) < 9 * sc and abs(y - py) < 5 * sc for px, py, _ in pts):
            continue
        pts.append((x, y, sc))
    pts.sort(key=lambda p: p[1])
    for x, y, sc in pts:
        t = (y - y0) / max(1, y1 - y0)
        img = L.enemy(rnd.choice(kinds), rnd.randint(0, 1))
        unit(cv, img, sc, x, y, face_left=face_left, dark=dark[0] + (dark[1] - dark[0]) * t, rim_c=rim_c, rim_side="left" if face_left else "right")


def fxs(cv, name, frame, scale, cx, cy, strength=1.0, glow=None, add=False, flip=False):
    img = L.fx(name, frame)
    if flip:
        img = L.flip(img)
    if glow:
        s = L.up(img, scale)
        cv.glow_layer(s, cx - s.width / 2, cy - s.height / 2, max(4, s.width / 6), glow, 0.55 * strength)
    cv.center(img, scale, cx, cy, alpha=min(1.0, strength), add=add)


def logo_on(cv, layout, scale, cx, top, demo=False, glow=True, badge_scale=None):
    img = L.up(LG.logo(layout), scale)
    x = int(cx - img.width / 2)
    # 글자 뒤 어두운 받침 (어떤 배경에서도 읽히게) + 영혼빛 번짐
    cv.shade(cx, top + img.height * 0.5, img.width * 0.62, 0.62, ry=img.height * 0.85, power=1.2)
    if glow:
        cv.glow_layer(img, x, top, scale * 4, (70, 170, 255), 0.55)
    cv.paste(img, x, top)
    bottom = top + img.height
    if demo:
        bs = badge_scale or max(1, scale // 2 + (1 if scale >= 4 else 0))
        b = L.up(LG.demo_badge(), bs)
        by = bottom - bs * 3
        cv.glow_layer(b, cx - b.width / 2, by, bs * 4, (255, 80, 60), 0.5)
        cv.paste(b, cx - b.width / 2, by)
        bottom = by + b.height
    return x, top, img.width, bottom - top


# ================================================================= 공통 장면: 영웅들 vs 몰려오는 괴물 떼
def battle(cv, rnd, P):
    W, H = cv.w, cv.h
    s = P["s"]                                     # 기준 배율 (영웅)
    floor(cv, P["floor"], 0.42, seed=P.get("seed", 3))
    # 위쪽은 어둠, 아래 바닥은 은은하게 (먼 곳일수록 짙게)
    yy = np.linspace(0, 1, H)[:, None, None]
    cv.a *= (0.35 + 0.65 * np.clip(yy * 1.4, 0, 1))
    cv.a += np.array((14, 8, 30), np.float32) * (1 - yy)

    for bx, by, bs in P.get("braziers", []):
        brazier(cv, bs, bx, by)

    # 보스 (뒤에 크게, 기운)
    for name, sc, bx, by, aura in P["bosses"]:
        img = L.enemy(name, 0)
        w = img.width * sc
        cv.light(bx, by - img.height * sc * 0.5, w * 1.1, aura, 0.5)
        unit(cv, L.outline(img, (12, 6, 24, 255)), sc, bx, by, face_left=True, dark=0.95, rim_c=(255, 190, 140), rim_side="left")

    # 괴물 떼 + 발밑 붉은 기운
    hx0, hy0, hx1, hy1 = P["horde"]
    cv.light((hx0 + hx1) / 2, hy1, (hx1 - hx0) * 0.7, (120, 30, 40), 0.35, ry=(hy1 - hy0) * 0.8)
    horde(cv, rnd, P["kinds"], P["n"], P["horde"], P["hscales"], rim_c=(255, 150, 90), avoid=P.get("avoid"))

    # 주인공 발밑 마법진 · 영웅 뒤 영혼빛
    if P.get("rune"):
        rx, ry, rs = P["rune"]
        r = L.up(L.tint(L.fx("fx_rune", 0), (0.45, 0.85, 1.0)), rs)
        r = r.resize((r.width, r.height // 2), Image.NEAREST)
        cv.glow_layer(r, rx - r.width / 2, ry - r.height / 2, rs * 3, SOUL_C, 0.4)
        cv.paste(r, rx - r.width / 2, ry - r.height / 2, alpha=0.45)
    for name, sc, hx, hy in P["heroes"]:
        cv.light(hx, hy - 12 * sc, 22 * sc, SOUL_C, 0.28)

    # 이펙트 (영웅보다 뒤 · 괴물 앞) · 총알 궤적
    for e in P.get("fx", []):
        fxs(cv, *e)
    for x0, y0, x1, y1 in P.get("bullets", []):
        cv.beam(x0, y0, x1, y1, GOLD_C, width=max(2.2, s / 3.2), glow=max(9, s * 1.3))

    # 영웅 (위 · 왼쪽 영혼빛 테두리, 오른쪽 불빛 테두리)
    for name, sc, hx, hy in sorted(P["heroes"], key=lambda e: e[3]):
        img = L.rim(L.hero(name), (150, 230, 255), "left,top", 0.55)
        unit(cv, img, sc, hx, hy, rim_c=(255, 200, 120), rim_side="right")

    for e in P.get("fx_front", []):
        fxs(cv, *e)
    for mx, my, ms in P.get("muzzle", []):        # 총구 불꽃
        cv.light(mx, my, 16 * ms, (255, 200, 90), 0.9)
        fxs(cv, "fx_muzzle", 1, ms, mx + 4 * ms, my)

    L.sparks(cv, rnd, P.get("sparks", 40), P.get("spark_box", (W * 0.3, H * 0.15, W, H)),
             [(255, 120, 40), (255, 180, 70), (120, 200, 255)], 1.0, 2.4 * s / 6)


# ================================================================= 가로 캡슐
def wide(w, h, demo=False, seed=11):
    # 헤더 920×430 기준 배치를 가로 kx · 세로 ky 로 늘림: 로고 왼쪽 위, 영웅 왼쪽 아래, 괴물 떼 · 보스 오른쪽
    cv = L.Canvas(w, h)
    rnd = random.Random(seed)
    kx, ky = w / 920, h / 430
    k = min(kx, ky)
    s = max(2, round(7 * k))
    f = max(1, s - 5)                               # 이펙트 배율

    def X(v):
        return v * kx

    def Y(v):
        return v * ky
    heroes = [("gunner", s, X(160), Y(410)), ("swordsman", s - 1, X(330), Y(392))]
    if not demo:
        heroes = [("archer", s - 2, X(70), Y(318)), ("ghost", s - 2, X(250), Y(286))] + heroes
    bosses = [("lich", s + 3, X(806), Y(300), VIOLET)]
    if not demo:
        bosses = [("hell_lord", s, X(630), Y(268), FIRE_C)] + bosses
    gx, gy = X(160) + 9 * s, Y(410) - 10 * s
    P = dict(s=s, floor=max(1, round(2 * k)), kinds=HORDE_DEMO if demo else HORDE_FULL, n=int(40 * kx * ky) + 8,
             horde=(X(510), Y(300), X(935), Y(452)), hscales=[max(2, s - 4), max(2, s - 3), max(2, s - 2), max(2, s - 2)],
             bosses=bosses, heroes=heroes,
             braziers=[(X(36), Y(214), max(2, s - 4))] + ([] if demo else [(X(470), Y(214), max(2, s - 5))]),
             bullets=[(gx, gy, X(556), Y(356)), (gx, gy, X(622), Y(414)), (gx, gy, X(610), Y(300))],
             muzzle=[(gx, gy, max(2, s - 4))],
             fx=[("fx_explosion_t3", 3, f + 1, X(560), Y(352), 1.0, FIRE_C),
                 ("fx_soulburst_t3", 2, f + 1, X(624), Y(412), 1.0, SOUL_C),
                 ("fx_explosion_t3", 2, f, X(612), Y(296), 1.0, FIRE_C)],
             fx_front=[("fx_slash_t3", 2, f, X(404), Y(340), 1.0, (200, 220, 255), False, True)],
             rune=(X(160), Y(404), f + 1),
             avoid=[(0, 0, X(530), h)])
    battle(cv, rnd, P)
    cv.vignette(0.55, 0.5)
    sc = max(1, round(5 * k))
    lw = LG.logo("line").width * sc
    logo_on(cv, "line", sc, X(26) + lw / 2, Y(20), demo=demo)
    return cv.image()


def small(demo=False):
    # 소형 캡슐 462×174: 로고를 최대한 크게, 아래에 영웅 vs 괴물 떼를 작게
    w, h = 462, 174
    cv = L.Canvas(w, h)
    rnd = random.Random(5)
    gx, gy = 64 + 27, 170 - 30
    P = dict(s=3, floor=1, kinds=HORDE_DEMO if demo else HORDE_FULL, n=26,
             horde=(270, 96, 470, 182), hscales=[2, 2, 3],
             bosses=[("lich", 5, 420, 120, VIOLET)] if demo else [("hell_lord", 4, 330, 104, FIRE_C), ("lich", 5, 424, 120, VIOLET)],
             heroes=[("gunner", 3, 64, 170), ("swordsman", 3, 134, 166)] if demo else [("ghost", 2, 22, 150), ("gunner", 3, 64, 170), ("swordsman", 3, 134, 166)],
             bullets=[(gx, gy, 286, 140), (gx, gy, 300, 160)],
             muzzle=[(gx, gy, 2)],
             fx=[("fx_explosion_t3", 3, 1, 290, 140, 1.0, FIRE_C), ("fx_soulburst_t3", 2, 1, 302, 160, 1.0, SOUL_C)],
             fx_front=[("fx_slash_t3", 2, 1, 160, 150, 1.0, (200, 220, 255), False, True)],
             avoid=[(0, 0, 280, 174)], sparks=18)
    battle(cv, rnd, P)
    cv.vignette(0.5, 0.55)
    logo_on(cv, "line", 3, w / 2, 6, demo=demo, badge_scale=2)
    return cv.image()


# ================================================================= 세로 캡슐
def tall(w, h, demo=False, seed=21):
    # 세로 748×896 · 라이브러리 600×900: 두 줄 로고 위, 보스가 가운데 뒤, 영웅은 왼쪽 아래에서 오른쪽 위로 쏨
    cv = L.Canvas(w, h)
    rnd = random.Random(seed)
    kx, ky = w / 748, h / 896
    k = min(kx, ky)
    s = max(2, round(9 * k))
    f = max(1, s - 7)

    def X(v):
        return v * kx

    def Y(v):
        return v * ky
    heroes = [("swordsman", s - 1, X(390), Y(812)), ("gunner", s, X(215), Y(876))]
    if not demo:
        heroes = [("archer", s - 3, X(70), Y(690)), ("ghost", s - 3, X(230), Y(650)), ("alchemist", s - 2, X(64), Y(812))] + heroes
    bosses = [("lich", s + 4, X(610) if demo else X(500), Y(590), VIOLET)]
    if not demo:
        bosses = [("hell_lord", s + 1, X(690), Y(548), FIRE_C), ("slime_king", s, X(300), Y(520), (120, 255, 140))] + bosses
    gx, gy = X(215) + 9 * s, Y(876) - 10 * s
    P = dict(s=s, floor=max(1, round(2 * k)), kinds=HORDE_DEMO if demo else HORDE_FULL, n=int(36 * kx * ky) + 6,
             horde=(X(330), Y(560), X(770), Y(800)), hscales=[max(2, s - 5), max(2, s - 4), max(2, s - 3)],
             bosses=bosses, heroes=heroes,
             braziers=[(X(44), Y(470), max(2, s - 6)), (X(704), Y(400), max(2, s - 6))],
             bullets=[(gx, gy, X(540), Y(680)), (gx, gy, X(470), Y(712)), (gx, gy, X(620), Y(740))],
             muzzle=[(gx, gy, max(2, s - 6))],
             fx=[("fx_explosion_t3", 3, f + 1, X(540), Y(680), 1.0, FIRE_C),
                 ("fx_soulburst_t3", 2, f + 1, X(466), Y(714), 1.0, SOUL_C),
                 ("fx_explosion_t3", 2, f, X(624), Y(742), 1.0, FIRE_C)],
             fx_front=[("fx_slash_t3", 2, f + 1, X(500), Y(770), 1.0, (200, 220, 255), False, True)],
             rune=(X(215), Y(870), f + 2),
             avoid=[(X(300), Y(700), X(560), h), (0, 0, X(400), Y(600))], sparks=50,
             spark_box=(X(300), Y(330), w, h))
    battle(cv, rnd, P)
    cv.vignette(0.55, 0.5)
    sc = max(1, round(8 * k))
    logo_on(cv, "stack", sc, w / 2, Y(34), demo=demo)
    return cv.image()


# ================================================================= 라이브러리 히어로 · 페이지 배경 · 로고
def hero_art(demo=False):
    # 3840×1240, 글자 없음. 가운데 띠(창이 좁아져도 남는 곳)에 영웅들 vs 괴물 떼 · 보스. 왼쪽은 로고 자리라 차분하게
    w, h = 3840, 1240
    cv = L.Canvas(w, h)
    rnd = random.Random(31)
    s, f = 13, 5
    if demo:
        heroes = [("swordsman", 12, 1760, 930), ("gunner", 13, 1460, 1010)]
        bosses = [("lich", 22, 2860, 790, VIOLET)]
    else:
        heroes = [("cartographer", 9, 900, 690), ("clockmaker", 9, 1060, 800), ("postman", 9, 1210, 660), ("alchemist", 10, 1010, 1000),
                  ("ghost", 9, 1420, 600), ("rogue", 10, 1250, 880), ("archer", 10, 1560, 740), ("swordsman", 12, 1760, 930), ("gunner", 13, 1460, 1010)]
        bosses = [("mirrorlord", 13, 3140, 560, SOUL_C), ("slime_king", 14, 2380, 720, (120, 255, 140)), ("hell_lord", 16, 3500, 760, FIRE_C), ("lich", 20, 2860, 790, VIOLET)]
    gx, gy = 1460 + 9 * s, 1010 - 10 * s
    P = dict(s=s, floor=3, kinds=HORDE_DEMO if demo else HORDE_FULL, n=120,
             horde=(2050, 740, 3900, 1210), hscales=[6, 7, 8, 9],
             bosses=bosses, heroes=heroes,
             braziers=[(260, 520, 6), (760, 450, 6), (3720, 450, 6)],
             bullets=[(gx, gy, 2110, 800), (gx, gy, 2210, 960), (gx, gy, 2170, 1080), (gx, gy, 2400, 880)],
             muzzle=[(gx, gy, 5)],
             fx=[("fx_explosion_t3", 3, f + 1, 2120, 800, 1.0, FIRE_C), ("fx_soulburst_t3", 2, f + 1, 2220, 960, 1.0, SOUL_C),
                 ("fx_explosion_t3", 2, f, 2180, 1080, 1.0, FIRE_C), ("fx_shock_t3", 2, f, 2410, 880, 1.0, (200, 230, 255)),
                 ("fx_vortex_t3", 2, f, 2600, 1050, 0.9, SOUL_C)],
             fx_front=[("fx_slash_t3", 2, f + 1, 1930, 850, 1.0, (200, 220, 255), False, True)],
             rune=(1460, 1004, 6),
             avoid=[(0, 0, 2080, h)], sparks=160)
    battle(cv, rnd, P)
    cv.vignette(0.5, 0.55)
    return cv.image()


def page_bg(demo=False):
    # 상점 페이지 배경 1438×810: 글자 없이 은은하게 (스팀이 위에 글 · 스크린샷을 얹음)
    w, h = 1438, 810
    cv = L.Canvas(w, h)
    floor(cv, 2, 0.30, seed=8)
    for bx, by in ((120, 300), (1318, 300), (120, 700), (1318, 700)):
        brazier(cv, 4, bx, by)
    cv.light(w / 2, 300, 620, (70, 40, 140), 0.32, ry=420)
    cv.fog((40, 26, 70), h * 0.55, h, 0.5)
    yy = np.linspace(0, 1, h)[:, None, None]
    cv.a *= 0.55 + 0.45 * (1 - yy)
    cv.grade(contrast=0.85, sat=0.7)
    cv.vignette(0.8, 0.25)
    return cv.image()


def library_logo(demo=False):
    # 1280×720 투명: 두 줄 로고 ×18 (가로 1278 로 거의 꽉, 캔버스 1280×720), 뒤에 은은한 그림자 · 체험판은 DEMO 리본
    sc = 16 if demo else 18
    img = L.up(LG.logo("stack"), sc)
    cw, ch = 1280, 720
    base = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    total = img.height + (LG.demo_badge().height * 7 - 7 * 3 if demo else 0)
    x, y = (cw - img.width) // 2, (ch - total) // 2
    layer = Image.new("RGBA", (cw, ch), (0, 0, 0, 0))
    layer.alpha_composite(img, (x, y))
    if demo:
        b = L.up(LG.demo_badge(), 7)
        layer.alpha_composite(b, ((cw - b.width) // 2, y + img.height - 21))
    sh = Image.new("L", (cw, ch), 0)
    sh.paste(layer.split()[3], (0, 10))
    sh = sh.filter(ImageFilter.GaussianBlur(14)).point(lambda v: int(v * 0.55))
    shadow = Image.new("RGBA", (cw, ch), (8, 4, 20, 0))
    shadow.putalpha(sh)
    base.alpha_composite(shadow)
    base.alpha_composite(layer)
    return base


# ================================================================= 아이콘
def community_icon(demo=False, size=184):
    # 커뮤니티 아이콘 184×184 JPG: 거너 전신 ×8, 영혼빛 배경
    g = L.rim(L.hero("gunner_idle"), (150, 230, 255), "left,top", 0.5)
    cv = L.Canvas(size, size)
    cv.gradient((24, 14, 46), (10, 6, 22))
    cv.light(size / 2, size * 0.55, size * 0.62, SOUL_C, 0.55)
    sc = int(size * 0.86) // g.height
    cv.shadow_ellipse(size / 2, size - size * 0.05, g.width * sc * 0.45, 3 * sc, 0.6)
    cv.sprite(g, sc, size / 2, size - size * 0.04)
    cv.vignette(0.5, 0.45)
    img = cv.image()
    if demo:
        k = max(1, size // 184)
        b = L.up(LG.demo_badge(), k)
        im = img.convert("RGBA")
        im.alpha_composite(b, ((size - b.width) // 2, size - b.height - 3 * k))
        img = im.convert("RGB")
    return img


def client_icons():
    # 투명 아이콘: 크기마다 정수 배율 (16 · 32 · 48 은 상반신, 256 은 전신)
    g = L.hero("gunner_idle")
    bust = g.crop((0, 0, 16, 16))
    out = {}
    for size, src, sc in ((16, bust, 1), (32, bust, 2), (48, bust, 3), (256, g, 11)):
        im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
        s = L.up(src, sc)
        im.alpha_composite(s, ((size - s.width) // 2, (size - s.height) // 2))
        out[size] = im
    return out


JOBS = {
    "header_capsule": lambda d: wide(920, 430, d),
    "library_header": lambda d: wide(920, 430, d),
    "main_capsule": lambda d: wide(1232, 706, d, seed=13),
    "small_capsule": lambda d: small(d),
    "vertical_capsule": lambda d: tall(748, 896, d),
    "library_capsule": lambda d: tall(600, 900, d, seed=22),
    "library_hero": lambda d: hero_art(d),
    "page_background": lambda d: page_bg(d),
    "library_logo": lambda d: library_logo(d),
}


def main():
    only = [a for a in sys.argv[1:] if a not in ("demo", "full")]
    which = [x for x in ("full", "demo") if x in sys.argv[1:]] or ["full", "demo"]
    for kind in which:
        demo = kind == "demo"
        out = OUT_DEMO if demo else OUT_FULL
        for name, fn in JOBS.items():
            if only and not any(o in name for o in only):
                continue
            L.save_png(fn(demo), os.path.join(out, name + ".png"))
        if not only or any(o in "icon" for o in only):
            img = community_icon(demo)
            p = os.path.join(out, "community_icon.jpg")
            img.save(p, quality=95)
            print("저장:", os.path.relpath(p, L.ROOT), img.size)
            icons = client_icons()
            icons[32].save(os.path.join(out, "client_icon.png"), optimize=True)
            icons[256].save(os.path.join(out, "client_icon.ico"), sizes=[(16, 16), (32, 32), (48, 48), (256, 256)],
                            append_images=[icons[16], icons[32], icons[48]])
            print("저장:", os.path.relpath(os.path.join(out, "client_icon.png / .ico"), L.ROOT))
            big = community_icon(demo, 512)
            L.save_png(big, os.path.join(out, "shortcut_icon_512.png"))


def sync_steam_assets():
    # 예전 이름(steam_assets/*_<크기>.png)을 쓰는 문서(docs/marketing/press-kit.md)를 위해 새 그림으로 덮어씀
    sa = os.path.join(L.ROOT, "steam_assets")
    pairs = {"header_capsule.png": "header_capsule_920x430.png", "library_capsule.png": "library_capsule_600x900.png",
             "library_header.png": "library_header_920x430.png", "library_hero.png": "library_hero_3840x1240.png",
             "library_logo.png": "library_logo.png", "main_capsule.png": "main_capsule_1232x706.png",
             "page_background.png": "page_background_1438x810.png", "small_capsule.png": "small_capsule_462x174.png",
             "vertical_capsule.png": "vertical_capsule_748x896.png", "community_icon.jpg": "community_icon_184x184.jpg",
             "client_icon.ico": "client_icon.ico"}
    import shutil
    for a, b in pairs.items():
        src = os.path.join(OUT_FULL, a)
        if os.path.exists(src):
            shutil.copyfile(src, os.path.join(sa, b))
    community_icon(False, 736).save(os.path.join(sa, "app_icon_736x736.png"), optimize=True)
    shutil.copyfile(os.path.join(OUT_DEMO, "shortcut_icon_512.png"), os.path.join(sa, "shortcut_icon_512x512.png")) if os.path.exists(os.path.join(OUT_DEMO, "shortcut_icon_512.png")) else None
    print("저장: steam_assets/ (예전 파일 이름으로 복사)")


if __name__ == "__main__":
    main()
    if len(sys.argv) == 1:
        sync_steam_assets()
