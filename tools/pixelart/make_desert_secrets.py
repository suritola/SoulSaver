# 사막 비밀 소품 그림 (docs/plans/2026-11-02-big-update/desert-secrets.md · 영웅 문서 11절)
#   Props/secret_bull_graze0/1 · brace · moo   씨름꾼 황소 (32칸 · 풀 뜯기 2 · 버팀 · 음머. 밀려남은 버팀 그림을 씀)
#   Props/secret_hoof                          발굽 자국 (두 점 한 쌍 · 모래보다 어두운 8칸)
#   Props/secret_stonering                     돌 원 조각 (둥근 돌 8칸 · 16개로 원을 그림)
#   Props/secret_cone                          주황 안전 고깔 (흰 띠 두 줄)
#   Props/secret_sign_0/1/2                    공사 표지판 (서 있음 · 금 감 · 넘어짐) — 글자는 코드의 월드 글자
#   Props/secret_tpose_0/1                     T 자세 더미 (흰 바탕 마젠타 격자 · 얼굴 없음 · 서 있음 · 한 발 내딛음)
#   Props/secret_string                        해금 연출용 줄 한 가닥
#   Props/secret_shadow                        더미의 긴 그림자 (코드가 길게 늘림 · 왼쪽 끝이 발밑)
#   Props/secret_grave                         장의사 묘비 (사막 유일의 회색 세로 소품)
#   Props/secret_flower                        시든 꽃 (흔적)
#   Props/secret_tripod_0/1                    사진사 빈 삼각대 (다리 셋 · 꼭대기 렌즈 반사광 없음 / 반짝)
#   .meta 는 이미 있으면 그대로 둠 (다시 돌려도 guid 유지) · 새로 만들 때 guid 는 이름에서 정해짐
# 실행: py -3.14 -X utf8 tools/pixelart/make_desert_secrets.py [미리보기 파일.png]
import math, os, re, sys, uuid
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Props')
TEMPLATE = os.path.join(ROOT, 'Assets', 'Resources', 'Desert', 'prop_cactus.png.meta')
K1 = (26, 17, 44)

# ------------------------------------------------------------------ 붓
class Cv:
    def __init__(s, w, h):
        s.w, s.h, s.p = w, h, {}

    def set(s, x, y, c):
        s.p[(int(x), int(y))] = c

    def rect(s, x, y, w, h, c):
        for j in range(y, y + h):
            for i in range(x, x + w):
                s.p[(i, j)] = c

    def ell(s, cx, cy, rx, ry, c):
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1:
                    s.p[(x, y)] = c

    def line(s, x0, y0, x1, y1, c):
        n = int(max(abs(x1 - x0), abs(y1 - y0))) * 2 + 1
        for i in range(n + 1):
            t = i / n
            s.set(round(x0 + (x1 - x0) * t), round(y0 + (y1 - y0) * t), c)

    def outline(s, c=K1):
        add = {}
        for (x, y) in s.p:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                q = (x + dx, y + dy)
                if q not in s.p:
                    add[q] = c
        s.p.update(add)

    def image(s):
        im = Image.new('RGBA', (s.w, s.h), (0, 0, 0, 0))
        for (x, y), c in s.p.items():
            if 0 <= x < s.w and 0 <= y < s.h:
                im.putpixel((x, y), tuple(c[:3]) + ((c[3] if len(c) == 4 else 255),))
        return im


def guid_of(key):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'soulsaver/desertsecrets/' + key).hex


# pivot: 'bottom' | 'center' | 'left'
def write(name, im, pivot='bottom', ppu=16):
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + '.png')
    im.save(path)
    m = path + '.meta'
    if os.path.exists(m):
        return
    t = open(TEMPLATE, encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + guid_of(name), t, count=1, flags=re.M)
    t = re.sub(r'spriteID: [0-9a-f]{32}', 'spriteID: ' + guid_of(name + '/sprite'), t)
    a, p = {'bottom': (7, '0.5, y: 0'), 'center': (0, '0.5, y: 0.5'), 'left': (9, '0, y: 0.5')}[pivot]
    t = re.sub(r'alignment: \d+\n  spritePivot: \{x: [-0-9.]+, y: [-0-9.]+\}', 'alignment: %d\n  spritePivot: {x: %s}' % (a, p), t, count=1)
    t = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, t, count=1)
    open(m, 'w', encoding='utf-8', newline='\n').write(t)


# ------------------------------------------------------------------ 황소 (오른쪽을 봄 · 32×32 · 발바닥 y 31)
HIDE = (118, 82, 58); HIDE_D = (82, 54, 42); HIDE_L = (156, 114, 82)
HORN = (232, 192, 84); HORN_D = (178, 136, 52)
SNOUT = (206, 164, 142); EYE = (20, 12, 24)
RIB_R = (192, 52, 56); RIB_W = (242, 238, 222)
FLW = (232, 132, 172); GRASS = (110, 160, 70)


def bull(head_dy=0, head_dx=0, lower=0, mouth=False, brace=False, tail=0, grass=False):
    c = Cv(32, 32)
    top = 3 + lower * 0          # 몸통 위치는 고정
    # 다리 (뒤 둘 · 앞 둘)
    fx = 2 if brace else 0
    for x in (8, 11):
        c.rect(x, 23, 3, 8, HIDE_D)
    for x in (20 + fx, 23 + fx):
        c.rect(x, 23, 3, 8, HIDE)
    for x in (8, 11, 20 + fx, 23 + fx):
        c.rect(x, 29, 3, 2, (60, 44, 40))                                # 발굽
    # 몸통
    c.ell(15.5, 19, 11, 6.5, HIDE)
    c.ell(14, 21, 8, 3.5, HIDE_L if False else HIDE)
    for x in range(8, 24):
        c.set(x, 24, HIDE_D)                                              # 배 그림자
    c.ell(21, 15, 4, 3, HIDE_L)                                           # 어깨 혹
    # 상품 띠 (몸통을 가로지르는 붉은 띠 + 흰 줄)
    for y in range(13, 25):
        if (14, y) in c.p:
            c.set(14, y, RIB_W); c.set(15, y, RIB_R); c.set(16, y, RIB_R); c.set(17, y, RIB_W)
    # 꼬리
    tx = 4 + tail
    c.line(5, 15, tx, 20, HIDE_D); c.line(tx, 20, tx, 24, HIDE_D); c.rect(tx - 1, 24, 2, 3, (50, 34, 34))
    # 머리
    hx, hy = 25 + head_dx, 16 + head_dy
    c.ell(hx, hy, 4.2, 3.6, HIDE)
    c.ell(hx + 2, hy + 1.5, 2.6, 2, SNOUT)                                # 주둥이
    c.set(hx + 3, hy + 1, HIDE_D); c.set(hx + 4, hy + 2, HIDE_D)          # 콧구멍
    c.set(hx - 0, hy - 1, EYE); c.set(hx + 1, hy - 1, EYE)
    if mouth:
        c.rect(hx + 1, hy + 3, 4, 2, (120, 40, 50)); c.set(hx + 2, hy + 3, (240, 230, 220))
    # 뿔 (금빛 · 위로 휘어짐)
    c.line(hx - 2, hy - 3, hx - 3, hy - 6, HORN_D); c.line(hx - 3, hy - 6, hx - 1, hy - 8, HORN)
    c.line(hx + 1, hy - 3, hx + 1, hy - 6, HORN_D); c.line(hx + 1, hy - 6, hx + 3, hy - 8, HORN)
    # 목에 꽃 목걸이
    for i, (dx, dy) in enumerate(((-4, 1), (-4, 3), (-3, 4), (-2, 5))):
        c.set(hx + dx, hy + dy, FLW if i % 2 == 0 else (250, 210, 120))
    if grass:
        for dx in (3, 4, 5, 6):
            c.set(hx + dx, hy + 4, GRASS); c.set(hx + dx, hy + 5, GRASS) if dx % 2 else None
    c.outline()
    return c.image()


# ------------------------------------------------------------------ 작은 소품
def hoof():
    c = Cv(8, 8)
    D = (150, 112, 72, 230)
    for ox in (1, 5):
        c.rect(ox, 2, 2, 4, D)
        c.set(ox, 2, (0, 0, 0, 0)) if False else None
    c.rect(3, 3, 2, 1, (150, 112, 72, 150))
    return c.image()


def stone():
    c = Cv(8, 8)
    c.ell(4, 4, 3.4, 3, (118, 108, 98))
    c.ell(3.4, 3.2, 2, 1.5, (156, 146, 130))
    for x in range(2, 7):
        c.set(x, 6, (84, 76, 70))
    c.outline((58, 50, 52))
    return c.image()


def cone():
    c = Cv(12, 16)
    O, W = (228, 112, 40), (240, 236, 224)
    for y in range(1, 13):
        half = 1 + (y - 1) * 0.4
        for x in range(int(6 - half), int(6 + half + 0.5)):
            c.set(x, y, O)
    for y in (4, 8):
        for x in range(0, 12):
            if (x, y) in c.p and (x, y + 1) in c.p:
                c.set(x, y, W); c.set(x, y + 1, W)
    c.rect(1, 13, 10, 2, (70, 54, 46))
    c.set(6, 1, (250, 160, 80))
    c.outline()
    return c.image()


def sign(variant):
    c = Cv(32, 32)
    Y, YD, BK = (238, 196, 50), (186, 146, 30), (30, 26, 28)
    LEG = (112, 100, 92)
    c.rect(9, 17, 3, 14, LEG); c.rect(20, 17, 3, 14, LEG)
    c.rect(2, 3, 28, 15, BK)
    for y in range(3, 18):
        for x in range(2, 30):
            edge = x < 4 or x > 27 or y < 5 or y > 15
            if edge and ((x + y) // 3) % 2 == 0:
                c.set(x, y, Y)
    c.rect(4, 5, 24, 11, (246, 220, 110))                               # 글자 자리 (코드가 월드 글자를 얹음)
    if variant >= 1:                                                    # 금
        c.line(14, 5, 17, 9, BK); c.line(17, 9, 14, 12, BK); c.line(14, 12, 18, 15, BK)
    c.outline()
    im = c.image()
    if variant == 1:
        im = im.rotate(-5, resample=Image.NEAREST, center=(16, 30))
    if variant == 2:                                                    # 앞으로 넘어짐: 판이 누워 다리가 위로
        big = im.rotate(-84, resample=Image.NEAREST, expand=True)
        out = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
        big = big.crop(big.getbbox())
        out.paste(big, (min(2, 32 - big.size[0]), 32 - big.size[1]), big)
        im = out
    return im


def tpose(step):
    c = Cv(32, 32)
    W, M = (244, 244, 248), (214, 40, 200)
    body = Cv(32, 32)
    body.ell(16, 6, 4.2, 4.2, W)                                        # 얼굴 없는 머리
    body.rect(13, 10, 6, 11, W)                                         # 몸통
    body.rect(1, 10, 30, 3, W)                                          # 팔 (T 자세)
    lx = 12 + (0 if step == 0 else -1); rx = 16 + (0 if step == 0 else 2)
    body.rect(lx, 21, 4, 10, W)
    body.rect(rx, 21 if step == 0 else 20, 4, 10 if step == 0 else 9, W)
    for (x, y), col in body.p.items():
        c.p[(x, y)] = M if (x % 4 == 0 or y % 4 == 0) else col         # 마젠타 격자 (「텍스처 없음」 농담)
    c.outline()
    return c.image()


def string():
    c = Cv(2, 32)
    c.rect(0, 0, 1, 32, (214, 214, 224))
    c.rect(1, 0, 1, 32, (140, 140, 156))
    return c.image()


def shadow():
    c = Cv(64, 10)
    S = (10, 8, 14, 90)
    c.rect(0, 4, 54, 2, S)
    for x in range(0, 18):
        c.rect(x, 4 - (1 if x < 10 else 0), 1, 2 + (2 if x < 10 else 0), S) if x < 10 else None
    c.rect(54, 0, 4, 10, S)                                             # 팔
    c.ell(60.5, 5, 3.2, 3.2, S)                                         # 머리
    return c.image()


def grave():
    c = Cv(16, 28)
    G, GD, GL = (134, 136, 146), (96, 98, 112), (168, 170, 180)
    c.rect(2, 7, 12, 15, G)
    c.ell(8, 7.5, 6, 6.2, G)
    for y in range(2, 22):
        c.set(13, y, GD) if (13, y) in c.p else None
        c.set(12, y, GD) if (12, y) in c.p and y > 9 else None
    for y in range(3, 16):
        c.set(3, y, GL) if (3, y) in c.p else None
    c.line(8, 3, 9, 7, K1); c.line(9, 7, 7, 10, K1); c.line(7, 10, 9, 13, K1)         # 금 간 자국
    c.outline()
    # 발치 모래 무덤
    S, SD = (194, 160, 104), (150, 118, 76)
    m = Cv(16, 28)
    m.ell(8, 24, 8, 3.2, S)
    for x in range(1, 15):
        m.set(x, 26, SD)
    for k, v in m.p.items():
        if k not in c.p or k[1] >= 22:
            c.p[k] = v
    # 시든 꽃
    c.line(3, 24, 3, 20, (110, 116, 100)); c.rect(2, 19, 3, 2, (192, 140, 152)); c.set(3, 20, (150, 100, 112))
    return c.image()


def flower():
    c = Cv(8, 12)
    c.line(4, 11, 4, 6, (112, 118, 104)); c.line(4, 6, 6, 4, (112, 118, 104))
    c.set(3, 10, (112, 118, 104)); c.set(2, 9, (112, 118, 104))
    c.rect(5, 2, 3, 3, (196, 144, 156)); c.set(6, 3, (150, 100, 112))
    return c.image()


def tripod(lit):
    c = Cv(16, 30)
    L, LD, HEAD = (96, 100, 112), (60, 64, 76), (44, 46, 56)
    # 다리 셋 (가운데 곧게 · 양옆으로 벌어짐)
    c.line(8, 10, 8, 29, L)
    c.line(8, 10, 2, 29, L)
    c.line(8, 10, 14, 29, L)
    c.line(9, 12, 9, 28, LD)
    # 머리 (카메라 없음) + 나사
    c.rect(5, 5, 6, 5, HEAD)
    c.rect(7, 2, 2, 3, LD)
    c.set(6, 6, (150, 154, 168))
    c.outline()
    if lit:
        # 렌즈 반사광: 꼭대기 십자 반짝
        W = (255, 255, 240)
        c.set(8, 1, W); c.set(8, 0, W); c.set(7, 1, W); c.set(9, 1, W); c.set(8, 2, W)
    return c.image()


def build():
    arts = {
        'secret_bull_graze0': (bull(head_dy=7, head_dx=0, grass=True, tail=0), 'bottom'),
        'secret_bull_graze1': (bull(head_dy=6, head_dx=1, grass=True, tail=1), 'bottom'),
        'secret_bull_brace': (bull(head_dy=4, head_dx=1, brace=True, tail=-1), 'bottom'),
        'secret_bull_moo': (bull(head_dy=-3, head_dx=1, mouth=True, tail=1), 'bottom'),
        'secret_hoof': (hoof(), 'center'),
        'secret_stonering': (stone(), 'center'),
        'secret_cone': (cone(), 'bottom'),
        'secret_sign_0': (sign(0), 'bottom'),
        'secret_sign_1': (sign(1), 'bottom'),
        'secret_sign_2': (sign(2), 'bottom'),
        'secret_tpose_0': (tpose(0), 'bottom'),
        'secret_tpose_1': (tpose(1), 'bottom'),
        'secret_string': (string(), 'bottom'),
        'secret_shadow': (shadow(), 'left'),
        'secret_grave': (grave(), 'bottom'),
        'secret_flower': (flower(), 'bottom'),
        'secret_tripod_0': (tripod(False), 'bottom'),
        'secret_tripod_1': (tripod(True), 'bottom'),
    }
    for n, (im, pv) in arts.items():
        write(n, im, pv)
    return arts


def preview(arts, path):
    names = list(arts)
    S = 8
    cell = 40
    cols = 6
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * cell * S // 2, rows * cell * S // 2), (212, 176, 112, 255))
    for i, n in enumerate(names):
        im = arts[n][0]
        sc = max(1, min(8, (cell * 4 - 8) // max(im.size)))
        big = im.resize((im.size[0] * sc, im.size[1] * sc), Image.NEAREST)
        x = (i % cols) * cell * S // 2 + 6
        y = (i // cols) * cell * S // 2 + 6
        sheet.paste(big, (x, y), big)
    sheet.save(path)


if __name__ == '__main__':
    a = build()
    if len(sys.argv) > 1:
        preview(a, sys.argv[1])
    print('완료:', len(a), '장')
