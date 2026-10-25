# 스팀 상점 그림 합성 도구 (make_store_art.py 가 씀)
# 게임 안 실제 도트 그림(Assets 의 캐릭터 · 적 · 보스 · 이펙트 · 타일)을 정수 배율 nearest 로 키워서 붙이고,
# 빛 · 안개 · 비네트처럼 부드러운 효과만 매끈하게 더한다. 도트 로고는 glyphs 를 손으로 찍은 글자로 만든다.
import math
import os
import random

import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
A = os.path.join(ROOT, "Assets")
SPR = os.path.join(A, "Sprites")
RES = os.path.join(A, "Resources")


# ================================================================= 그림 불러오기
_cache = {}


def load(path):
    if path not in _cache:
        _cache[path] = Image.open(path).convert("RGBA")
    return _cache[path]


def trim(img):
    box = img.getbbox()
    return img.crop(box) if box else img


def cell(path, row=0, col=0, size=32):
    return trim(load(path).crop((col * size, row * size, (col + 1) * size, (row + 1) * size)))


# 영웅 (Resources/Characters/<이름>.png 는 32칸 6장: 0 대기 · 1~5 걷기/동작)
HERO_FILES = {
    "gunner": (os.path.join(SPR, "Players", "players blue x1.png"), 4, 0),       # 사격 자세 (총 포함)
    "gunner_idle": (os.path.join(SPR, "Players", "players blue x1.png"), 0, 0),
    "swordsman": (os.path.join(RES, "Characters", "swordsman.png"), 0, 3),
    "rogue": (os.path.join(RES, "Characters", "rogue.png"), 0, 3),
    "archer": (os.path.join(RES, "Characters", "archer.png"), 0, 3),
    "alchemist": (os.path.join(RES, "Characters", "alchemist.png"), 0, 3),
    "cartographer": (os.path.join(RES, "Characters", "cartographer.png"), 0, 3),
    "clockmaker": (os.path.join(RES, "Characters", "clockmaker.png"), 0, 3),
    "postman": (os.path.join(RES, "Characters", "postman.png"), 0, 3),
    "ghost": (os.path.join(RES, "Characters", "ghost.png"), 0, 3),
}


def hero(name, frame=None):
    path, row, col = HERO_FILES[name]
    return cell(path, row, col if frame is None else frame)


ENEMY_FILES = {
    "skeleton": os.path.join(SPR, "Players", "players blue x1 1.png"),
    "zombie": os.path.join(SPR, "Players", "eneymy2.png"),
    "wraith": os.path.join(SPR, "enemy", "eneymy3.png"),
    "lich": os.path.join(SPR, "enemy", "boss.png"),
    "imp": os.path.join(SPR, "enemy", "hell", "hell_imp.png"),
    "hound": os.path.join(SPR, "enemy", "hell", "hell_hound.png"),
    "golem": os.path.join(SPR, "enemy", "hell", "hell_golem.png"),
    "knight": os.path.join(SPR, "enemy", "hell", "hell_knight.png"),
    "flameskull": os.path.join(SPR, "enemy", "hell", "hell_flameskull.png"),
    "hell_lord": os.path.join(SPR, "enemy", "hell", "hell_lord.png"),
    "slime": os.path.join(SPR, "enemy", "meadow", "meadow_slime.png"),
    "slime_king": os.path.join(SPR, "enemy", "meadow", "meadow_slimeking.png"),
    "bee": os.path.join(SPR, "enemy", "meadow", "meadow_bee.png"),
    "mushroom": os.path.join(SPR, "enemy", "meadow", "meadow_mushroom.png"),
    "treant": os.path.join(SPR, "enemy", "meadow", "meadow_treant.png"),
    "wolf": os.path.join(SPR, "enemy", "meadow", "meadow_wolf.png"),
}
ABYSS = ["chainwraith", "colossus", "developer", "mirrorlord", "reaper", "voideye", "wisp"]


# 2단계 그래픽 개편(2026-10-25~): 보스 시트 · 심연 보스 그림이 고해상도(90~162칸)로 바뀌어 32칸으로 잘라 쓰면 깨짐
#   → 32칸 설계도(tools/pixelart/hires/bosses/blueprints/<이름>_32.png · 32칸 4열 × 2줄, 첫 칸 = 서 있는 모습)를 씀
BLUEPRINTS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "pixelart", "hires", "bosses", "blueprints")
BOSS_BLUEPRINTS = {"lich": "lich_32.png", "hell_lord": "helllord_32.png", "slime_king": "slimeking_32.png",
                   "developer": "developer_32.png", "mirrorlord": "mirror_32.png", "mom": "mom_32.png"}


def enemy(name, frame=0):
    if name in BOSS_BLUEPRINTS:
        return cell(os.path.join(BLUEPRINTS, BOSS_BLUEPRINTS[name]), (frame // 4) % 2, frame % 4)
    if name in ABYSS:
        return trim(load(os.path.join(RES, "Abyss", "%s_%d.png" % (name, frame % 4))))
    return cell(ENEMY_FILES[name], 1 if frame else 0, frame)


def fx(name, frame):
    sheet = load(os.path.join(RES, "FX", name + ".png"))
    h = sheet.height
    n = max(1, sheet.width // h)
    w = sheet.width // n
    return trim(sheet.crop((frame * w, 0, (frame + 1) * w, h)))


def obj(sheet, idx):
    # objects_<맵>.png 의 Unity 슬라이스 (meta 의 rect, y 는 아래에서부터)
    path = sheet
    meta = open(path + ".meta", encoding="utf-8").read().split("\n")
    rects = []
    cur = {}
    for line in meta:
        s = line.strip()
        for k in ("x:", "y:", "width:", "height:"):
            if s.startswith(k) and "name" in cur and k[:-1] not in cur:
                cur[k[:-1]] = int(float(s.split(":")[1]))
        if s.startswith("name:") and "_" in s:
            if "width" in cur:
                rects.append(cur)
            cur = {"name": s.split(":")[1].strip()}
    if "width" in cur:
        rects.append(cur)
    r = rects[idx]
    img = load(path)
    return img.crop((r["x"], img.height - r["y"] - r["height"], r["x"] + r["width"], img.height - r["y"]))


def tile(sheet, col, row, size=32):
    return load(sheet).crop((col * size, row * size, (col + 1) * size, (row + 1) * size))


# ================================================================= 도트 변형 (원본 해상도에서)
def up(img, s):
    return img.resize((img.width * s, img.height * s), Image.NEAREST)


def flip(img):
    return img.transpose(Image.FLIP_LEFT_RIGHT)


def tint(img, mul=(1, 1, 1), add=(0, 0, 0), alpha=1.0):
    a = np.asarray(img).astype(np.float32)
    for i in range(3):
        a[..., i] = a[..., i] * mul[i] + add[i]
    a[..., 3] *= alpha
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA")


def silhouette(img, color, alpha=1.0):
    a = np.asarray(img).copy()
    a[..., :3] = color
    a[..., 3] = (a[..., 3].astype(np.float32) * alpha).astype(np.uint8)
    return Image.fromarray(a, "RGBA")


def rim(img, color, side="left", strength=0.85, width=1):
    # 가장자리 한 칸에 빛 (게임의 영혼빛 림 라이트처럼). side: left/right/top/bottom 또는 둘 이상 "left,top"
    a = np.asarray(img).astype(np.float32)
    m = a[..., 3] > 40
    edge = np.zeros_like(m)
    for sd in side.split(","):
        for k in range(1, width + 1):
            sh = np.zeros_like(m)
            if sd == "left":
                sh[:, k:] = m[:, :-k]
            elif sd == "right":
                sh[:, :-k] = m[:, k:]
            elif sd == "top":
                sh[k:, :] = m[:-k, :]
            elif sd == "bottom":
                sh[:-k, :] = m[k:, :]
            edge |= m & ~sh
    c = np.array(color, np.float32)
    a[edge, :3] = a[edge, :3] * (1 - strength) + c * strength
    return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), "RGBA")


def outline(img, color=(16, 9, 30, 255), n=1):
    # 원본 해상도에서 바깥 윤곽선 n칸
    a = np.asarray(img)
    m = a[..., 3] > 40
    H, W = m.shape
    out = np.zeros((H + 2 * n, W + 2 * n, 4), np.uint8)
    big = np.zeros((H + 2 * n, W + 2 * n), bool)
    big[n:n + H, n:n + W] = m
    d = big.copy()
    for _ in range(n):
        e = d.copy()
        e[1:] |= d[:-1]; e[:-1] |= d[1:]; e[:, 1:] |= d[:, :-1]; e[:, :-1] |= d[:, 1:]
        d = e
    out[d] = color
    inner = out[n:n + H, n:n + W]
    inner[m] = a[m]
    return Image.fromarray(out, "RGBA")


# ================================================================= 캔버스 (float 배열로 합성)
class Canvas:
    def __init__(self, w, h, color=(0, 0, 0)):
        self.w, self.h = w, h
        self.a = np.zeros((h, w, 3), np.float32)
        self.a[...] = color

    # ---- 배경
    def gradient(self, top, bottom, power=1.0):
        t = (np.linspace(0, 1, self.h) ** power)[:, None, None]
        self.a[...] = np.array(top, np.float32) * (1 - t) + np.array(bottom, np.float32) * t

    def tile_floor(self, tiles, scale, seed=1, dark=1.0, ox=0, oy=0):
        # tiles: [(img, weight)] 무작위로 깐 바닥 (정수 배율)
        rnd = random.Random(seed)
        ts = tiles[0][0].width * scale
        total = sum(w for _, w in tiles)
        for ty in range(-1, self.h // ts + 2):
            for tx in range(-1, self.w // ts + 2):
                r = rnd.uniform(0, total)
                for img, wgt in tiles:
                    r -= wgt
                    if r <= 0:
                        break
                self.paste(up(img, scale), tx * ts + ox, ty * ts + oy, mul=dark)

    # ---- 붙이기
    def paste(self, img, x, y, alpha=1.0, mul=1.0, add=False):
        x, y = int(round(x)), int(round(y))
        src = np.asarray(img).astype(np.float32)
        x0, y0 = max(0, x), max(0, y)
        x1, y1 = min(self.w, x + img.width), min(self.h, y + img.height)
        if x0 >= x1 or y0 >= y1:
            return
        s = src[y0 - y:y1 - y, x0 - x:x1 - x]
        k = s[..., 3:4] / 255.0 * alpha
        dst = self.a[y0:y1, x0:x1]
        if add:
            dst += s[..., :3] * mul * k
        else:
            dst[...] = dst * (1 - k) + s[..., :3] * mul * k

    def sprite(self, img, scale, cx, by, alpha=1.0, add=False, mul=1.0):
        # 아래 가운데(cx, by)에 맞춰 붙임 · 정수 배율
        s = up(img, scale)
        self.paste(s, cx - s.width / 2, by - s.height, alpha=alpha, add=add, mul=mul)
        return s.width, s.height

    def center(self, img, scale, cx, cy, alpha=1.0, add=False, mul=1.0):
        s = up(img, scale) if scale != 1 else img
        self.paste(s, cx - s.width / 2, cy - s.height / 2, alpha=alpha, add=add, mul=mul)

    # ---- 빛
    def light(self, cx, cy, r, color, strength=1.0, power=2.0, ry=None):
        ry = ry or r
        x0, x1 = max(0, int(cx - r)), min(self.w, int(cx + r) + 1)
        y0, y1 = max(0, int(cy - ry)), min(self.h, int(cy + ry) + 1)
        if x0 >= x1 or y0 >= y1:
            return
        yy, xx = np.mgrid[y0:y1, x0:x1]
        d = np.sqrt(((xx - cx) / r) ** 2 + ((yy - cy) / ry) ** 2)
        k = np.clip(1 - d, 0, 1) ** power * strength
        self.a[y0:y1, x0:x1] += k[..., None] * np.array(color, np.float32)

    def shade(self, cx, cy, r, amount, ry=None, power=1.5):
        # 원형으로 어둡게 (곱하기)
        ry = ry or r
        x0, x1 = max(0, int(cx - r)), min(self.w, int(cx + r) + 1)
        y0, y1 = max(0, int(cy - ry)), min(self.h, int(cy + ry) + 1)
        if x0 >= x1 or y0 >= y1:
            return
        yy, xx = np.mgrid[y0:y1, x0:x1]
        d = np.sqrt(((xx - cx) / r) ** 2 + ((yy - cy) / ry) ** 2)
        k = np.clip(1 - d, 0, 1) ** power * amount
        self.a[y0:y1, x0:x1] *= (1 - k)[..., None]

    def glow_layer(self, img, x, y, radius, color, strength):
        # 그림 모양을 흐려서 더하기 빛으로 (로고 · 이펙트 뒤 번짐)
        pad = int(radius * 3)
        base = Image.new("L", (img.width + pad * 2, img.height + pad * 2), 0)
        base.paste(img.split()[3], (pad, pad))
        bl = np.asarray(base.filter(ImageFilter.GaussianBlur(radius))).astype(np.float32) / 255.0
        x0, y0 = int(x) - pad, int(y) - pad
        H, W = bl.shape
        cx0, cy0 = max(0, x0), max(0, y0)
        cx1, cy1 = min(self.w, x0 + W), min(self.h, y0 + H)
        if cx0 >= cx1 or cy0 >= cy1:
            return
        k = bl[cy0 - y0:cy1 - y0, cx0 - x0:cx1 - x0] * strength
        self.a[cy0:cy1, cx0:cx1] += k[..., None] * np.array(color, np.float32)

    def shadow_ellipse(self, cx, cy, rx, ry, amount=0.55):
        self.shade(cx, cy, rx, amount, ry=ry, power=0.8)

    def beam(self, x0, y0, x1, y1, color, width=3, core=(255, 250, 225), glow=10):
        # 총알 궤적: 매끈한 빛줄기 + 흰 심지
        n = int(math.hypot(x1 - x0, y1 - y0) / 2) + 1
        for i in range(n):
            t = i / max(1, n - 1)
            px, py = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            f = 0.25 + 0.75 * t
            self.light(px, py, glow, color, 0.18 * f, power=2)
            self.light(px, py, width, core, 0.9 * f, power=1)

    def vignette(self, amount=0.6, inner=0.45, cx=0.5, cy=0.5):
        yy, xx = np.mgrid[0:self.h, 0:self.w]
        d = np.sqrt(((xx / self.w - cx) / 0.62) ** 2 + ((yy / self.h - cy) / 0.62) ** 2)
        k = np.clip((d - inner) / (1.0 - inner), 0, 1) ** 1.4 * amount
        self.a *= (1 - k)[..., None]

    def fog(self, color, y0, y1, strength, seed=3):
        # 아래에서 피어오르는 안개 (가로로 일렁임)
        rnd = np.random.RandomState(seed)
        noise = rnd.rand(8, 24).astype(np.float32)
        n = np.asarray(Image.fromarray((noise * 255).astype(np.uint8)).resize((self.w, self.h), Image.BICUBIC)).astype(np.float32) / 255
        yy = np.arange(self.h)[:, None]
        band = np.clip((yy - y0) / max(1, (y1 - y0)), 0, 1)
        k = band * (0.55 + 0.45 * n) * strength
        self.a = self.a * (1 - k[..., None] * 0.6) + k[..., None] * np.array(color, np.float32)

    def grade(self, contrast=1.0, sat=1.0, lift=(0, 0, 0)):
        a = self.a
        lum = (a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114)[..., None]
        a = lum + (a - lum) * sat
        a = (a - 128) * contrast + 128 + np.array(lift, np.float32)
        self.a = a

    def image(self):
        return Image.fromarray(np.clip(self.a, 0, 255).astype(np.uint8), "RGB")


def sparks(cv, rnd, n, box, colors, rmin=1.2, rmax=3.0, strength=1.0):
    x0, y0, x1, y1 = box
    for _ in range(n):
        x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
        c = rnd.choice(colors)
        r = rnd.uniform(rmin, rmax)
        cv.light(x, y, r * 3, c, 0.35 * strength)
        cv.light(x, y, r, (255, 255, 240), 0.8 * strength, power=1)


def save_png(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path, optimize=True)
    print("저장:", os.path.relpath(path, ROOT), img.size)
