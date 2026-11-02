# 새 영웅 인형사 「줄 끝의 연출가」 그림 일체 (docs/plans/2026-11-02-big-update/hero-puppeteer.md 13절)
#   Characters/puppeteer.png           32칸 몸 시트 (192×32 · idle 2 + run 4 · 스프라이트 puppeteer_0~5)
#   Weapons/weapon_dummy.png           손에 든 것 = 더미 (128×32 · 4프레임: 떠 있음 · 회전 ¼ · ½ · ¾ · 스프라이트 weapon_dummy_0~3)
#   Weapons/weapon_dummy_<형태>.png    전용 무기 1차 형태의 더미 (scissor · spool · windup · 같은 4프레임 시트)
#   Weapons/weapon_kit_154~156.png     전용 무기 가위 손 · 실감개 · 태엽 병정 (32칸 한 장 · 더미 첫 프레임과 같은 그림)
#   FX/fx_puppet_swing · fx_string_cut · fx_string_twang · fx_swap_poof · fx_thread_mark · fx_curtain_string
#      fx_spotlight · fx_curtain_drop · fx_scissor_snip
#   UI/ui_curtain_edge.png             가장자리 붉은 벨벳 막 (9-slice 한 장 · 왼쪽 가장자리 모양, 오른쪽은 코드에서 뒤집음)
#   Icons/ability_<번호>.png           특수 554~560 · 전용 카드 760~765 · 고유 스킬 860~869 · 스킬 진화 960~966 · 강화 618~620
#   범주 틀은 frame_icons.py 의 틀을 그대로 가져옴 (사각 무기 · 원 스킬/필살기 · 마름모 패시브 · 육각 진화)
#   색 규칙: 아군색 #9FB8FF 는 더미 외곽 · 줄 · 휘두름 FX. 마젠타는 더미 격자 무늬(그림색)에만 쓴다.
#   .meta 는 이미 있으면 그대로 둠 (다시 돌려도 guid 유지) · 새로 만들 때 guid 는 이름에서 정해짐
# 실행: py -3.14 -X utf8 tools/pixelart/make_puppeteer.py [미리보기 폴더]
import math, os, random, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from PIL import Image
import frame_icons as FI
import make_kit_weapons as KW
from make_fx import sheet_meta
from make_wrestler import Cv, save_png, fade, BAYER, star_burst, arc, arrow_head, foe

ROOT = FI.ROOT
RES = os.path.join(ROOT, 'Assets', 'Resources')
CHARS, WEAP, FXD, ICON, UID = [os.path.join(RES, d) for d in ('Characters', 'Weapons', 'FX', 'Icons', 'UI')]
K1 = (26, 17, 44)
TAU = math.tau


def guid_of(key):
    return uuid.uuid5(uuid.NAMESPACE_URL, 'soulsaver/puppeteer/' + key).hex


# ------------------------------------------------------------------ .meta 만들기 (있으면 그대로)
def meta_copy(template, out, key, pivot=None, ppu=None, sprite_names=None, border=None):
    m = out + '.meta'
    if os.path.exists(m):
        return
    t = open(template, encoding='utf-8').read()
    t = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + guid_of(key), t, count=1, flags=re.M)
    r = random.Random(key)
    ids = {}
    for old in set(re.findall(r'internalID: (-?\d+)', t)):
        ids[old] = str(r.randint(-2 ** 31, 2 ** 31 - 1)) if old != '0' else '0'
    t = re.sub(r'(?<![\w-])-?\d{3,}(?![\w])', lambda mo: ids.get(mo.group(0), mo.group(0)), t)
    t = re.sub(r'spriteID: [0-9a-f]{32}', lambda mo: 'spriteID: ' + uuid.UUID(int=r.getrandbits(128)).hex, t)
    if sprite_names:
        a, b = sprite_names
        t = t.replace(a, b)
    if pivot is not None:
        t = re.sub(r'spritePivot: \{x: [-0-9.]+, y: [-0-9.]+\}', 'spritePivot: {x: %.3f, y: %.3f}' % pivot, t, count=1)
    if ppu:
        t = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, t, count=1)
    if border:
        t = re.sub(r'spriteBorder: \{[^}]*\}', 'spriteBorder: {x: %d, y: %d, z: %d, w: %d}' % border, t, count=1)
    with open(m, 'w', encoding='utf-8', newline='\n') as f:
        f.write(t)


def sheet_out(folder, name, frames, w, h, ppu=16):
    # 가로로 이어 붙인 시트 + 스프라이트 <이름>_0.. 가 든 .meta (이미 있으면 그대로)
    sheet = Image.new('RGBA', (w * len(frames), h), (0, 0, 0, 0))
    for i, cv in enumerate(frames):
        sheet.paste(cv.image(w, h) if isinstance(cv, Cv) else cv, (i * w, 0))
    out = os.path.join(folder, name + '.png')
    save_png(sheet, out)
    if not os.path.exists(out + '.meta'):
        sheet_meta(name, w, h, len(frames), out + '.meta')
        t = open(out + '.meta', encoding='utf-8').read()
        t = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + guid_of(folder.split(os.sep)[-1] + '/' + name), t, count=1, flags=re.M)
        t = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, t, count=1)
        open(out + '.meta', 'w', encoding='utf-8', newline='\n').write(t)
    return sheet


def fx_out(name, frames, w, h, ppu=16):
    return sheet_out(FXD, name, frames, w, h, ppu)


# ------------------------------------------------------------------ 색
ALLY, ALLY_L, ALLY_D = (159, 184, 255), (214, 226, 255), (96, 120, 206)       # 아군색 #9FB8FF
WHITE = (255, 250, 242)
MAG, MAG_D = (232, 48, 222), (150, 24, 150)                                    # 더미 격자 (그림색 전용)
PAPER, PAPER_D = (250, 250, 252), (214, 214, 226)
GOLD, GOLD_D, GOLD_L = (250, 214, 100), (190, 130, 40), (255, 244, 190)
RED, RED_D, RED_L = (190, 40, 58), (120, 24, 44), (240, 96, 100)
STEEL, STEEL_D, STEEL_L = (176, 188, 210), (100, 112, 140), (240, 246, 255)
WOOD, WOOD_D, WOOD_L = (176, 118, 72), (110, 70, 46), (226, 172, 112)
PUR, PUR_L, PUR_D = (112, 72, 172), (158, 110, 224), (72, 44, 118)
SKIN, SKIN_D = (236, 218, 216), (184, 156, 176)
EYE = (32, 22, 50)
GRN, GRND = (140, 220, 140), (62, 140, 92)
LIGHT = (255, 238, 160)                                                         # 조명 노랑 (옅게)


# ================================================================== 1. 몸 시트
BP = {
    'h': (56, 44, 88), 'H': (112, 96, 164), 's': SKIN, 'S': SKIN_D, 'k': EYE, 'm': (150, 98, 120),
    'L': (172, 124, 240), 'P': (122, 80, 186), 'D': (86, 54, 140), 'F': (60, 36, 102), 'N': (46, 28, 80), 'C': (212, 178, 255),
    'G': (240, 190, 72), 'b': (50, 38, 76), 'B': (84, 68, 118), 'l': (62, 46, 94),
    'W': (176, 118, 72), 'w': (112, 72, 48), 'V': (226, 172, 112), 'x': (255, 255, 255),
}
OUT_SKIN = (62, 28, 48)           # 살색에 붙은 외곽 (K1 보다 살짝 붉은 어둠 · Y < 41)

HEAD = ['.hhhh.', 'hhHhhh', 'hsssSh', 'sksskS', 'sSmmSs', '.sSSs.']


def pup_frame(T=0, bd=0, sway=0, legs='stand', lean=0, phase=0, hem=28, hemshift=0, fist=0):
    cv = Cv()
    P = BP

    def put(x, y, ch):
        cv.p[(x, y)] = P[ch]
    # ---- 다리 (망토 아래로 보이는 부분)
    LEGS = {'stand': [((12, 26), (11, 29), 30), ((15, 26), (16, 29), 30)],
            'A': [((12, 25), (9, 29), 30), ((15, 25), (19, 28), 29)],
            'B': [((12, 25), (12, 29), 30), ((15, 25), (16, 27), 28)],
            'C': [((12, 25), (8, 28), 29), ((15, 25), (18, 29), 30)],
            'D': [((12, 25), (11, 27), 28), ((15, 25), (15, 29), 30)]}[legs]
    for (hx_, hy_), (ax, ay), by in LEGS:
        cv.line(hx_, hy_, ax, ay, P['l'], 2)
        for dx in (-1, 0, 1):
            put(ax + dx, by, 'b')
        put(ax + (1 if ax >= 14 else -1) * 0, by - 1 if by - 1 > ay else by, 'l') if by - 1 > ay else None
        put(ax - 1, by, 'B') if ax < 14 else put(ax + 1, by, 'B')
    # ---- 망토
    top = 16 + T
    for y in range(top, hem + 1):
        L, R = 11, 16
        if y <= top + 1:
            L, R = 10, 17
        elif y >= 24:
            L, R = 10, 17
        sh = int(round(sway * max(0, y - 20) / 8.0)) if y > 20 else 0
        if y == hem:
            sh += hemshift
        for x in range(L + sh, R + sh + 1):
            u = (x - (L + sh)) / float(R - L)
            if y == top:
                ch = 'C' if u < 0.9 else 'L'
            elif y == hem:
                ch = 'N'
            else:
                if u < 0.16:
                    i = 1
                elif u > 0.84:
                    i = 3
                elif y >= 19 and (abs(u - 0.42) < 0.07 or abs(u - 0.72) < 0.07):
                    i = 3
                else:
                    i = 2 if u < 0.62 else 3
                i = min(i + (0 if y < 19 else 1 if y < 23 else 2), 4)
                ch = 'CLPDF'[i] if i else 'L'
            if y == hem and (x + phase) % 5 == 0:
                continue                                      # 헝겊 끝 톱니
            put(x, y, ch)
    put(13, top, 'G'); put(14, top, 'G')                      # 망토 걸쇠
    # ---- 머리 · 목
    for j, row in enumerate(HEAD):
        for i, ch in enumerate(row):
            if ch != '.':
                put(11 + i, 9 + T + j, ch)
    put(13, 15 + T, 'S'); put(14, 15 + T, 'S')
    # ---- 왼팔 (아래로 드리운 소매 + 손)
    for k in range(6):
        sx = int(round(lean * k / 5.0))
        put(9 - sx, 17 + T + k, 'L' if k < 5 else 'C'); put(10 - sx, 17 + T + k, 'P')
    sx = int(round(lean))
    put(9 - sx, 23 + T, 's'); put(10 - sx, 23 + T, 'S'); put(9 - sx, 24 + T, 's'); put(10 - sx, 24 + T, 'S')
    # ---- 오른팔 (조종대를 높이 듦)
    for y in range(9, top + 1):
        put(18, y, 'L' if y < 13 else 'P'); put(19, y, 'P' if y < 13 else 'D')
    put(18, 8, 'C'); put(19, 8, 'C')
    for y in (6 + fist, 7 + fist):
        put(18, y, 's'); put(19, y, 'S')
    # ---- 조종대 (나무 십자: 가로 7 · 세로 5) — 손목만 까딱이면 bd 가 달라짐
    by = 2 + bd
    for x in range(16, 23):
        put(x, by, 'V' if x < 19 else 'W' if x == 19 else 'w')
    for y in range(by - 1, 6):
        put(19, y, 'W' if y != by - 1 else 'V')
    for x in (16, 22):
        put(x, by, 'w')
    # ---- 실 네 가닥 (조종대 네 끝 · 흰 점) — 외곽선 안에 들어가게 먼저 놓고 같이 두름
    for (x, y0, n) in ((16, by + 1, 2), (22, by + 1, 2), (18, by + 1, 1), (20, by + 1, 1)):
        for j in range(n):
            cv.p[(x, y0 + j)] = P['x']
    cv.outline(lambda x, y: OUT_SKIN if cv.p.get((x, y)) in (P['s'], P['S']) else K1)
    return cv


def body_sheet():
    frames = [
        pup_frame(T=0, bd=0, phase=0),
        pup_frame(T=0, bd=-1, phase=2, hemshift=1),                                  # 조종대 위로 까딱 · 망토 끝 펄럭
        pup_frame(T=1, bd=0, sway=-2, legs='A', lean=1, phase=1, hem=27, fist=0),
        pup_frame(T=0, bd=0, sway=-1, legs='B', lean=-1, phase=3, hem=27),
        pup_frame(T=1, bd=0, sway=-2, legs='C', lean=1, phase=4, hem=27),
        pup_frame(T=0, bd=0, sway=-1, legs='D', lean=-1, phase=2, hem=27),
    ]
    sheet = Image.new('RGBA', (192, 32), (0, 0, 0, 0))
    for i, cv in enumerate(frames):
        sheet.paste(cv.image(32, 32), (i * 32, 0))
    out = os.path.join(CHARS, 'puppeteer.png')
    save_png(sheet, out)
    meta_copy(os.path.join(CHARS, 'cartographer.png.meta'), out, 'Characters/puppeteer', sprite_names=('cartographer', 'puppeteer'))
    return sheet


# ================================================================== 2. 더미 (손에 든 것) · 전용 무기
def tile(x, y, ox, oy, t=2, flip=False):
    k = ((x - ox) // t + (y - oy) // t) % 2
    if flip:
        k = 1 - k
    return MAG if k == 0 else PAPER


def chk_rect(cv, x, y, w, h, flip=False, t=2):
    for j in range(h):
        for i in range(w):
            cv.p[(x + i, y + j)] = tile(x + i, y + j, x, y, t, flip)
    # 아래 · 오른쪽 가장자리 한 줄 살짝 어둡게 (면 구분)
    for i in range(w):
        c = cv.p[(x + i, y + h - 1)]
        cv.p[(x + i, y + h - 1)] = MAG_D if c == MAG else PAPER_D


def chk_disc(cv, cx, cy, r, flip=False, t=2):
    for y in range(int(cy - r - 1), int(cy + r + 2)):
        for x in range(int(cx - r - 1), int(cx + r + 2)):
            if (x + 0.5 - cx) ** 2 + (y + 0.5 - cy) ** 2 <= r * r:
                c = tile(x, y, int(cx - r), int(cy - r), t, flip)
                if (x + 0.5 - cx) + (y + 0.5 - cy) > r * 0.95:
                    c = MAG_D if c == MAG else PAPER_D
                cv.p[(x, y)] = c


JOINT, JOINT_L = (28, 22, 40), (150, 150, 176)


def joint(cv, x, y, r=1.4):
    cv.disc(x, y, r, JOINT)
    cv.set(x - 0.5, y - 0.5, JOINT_L)


def blade(cv, x0, y0, x1, y1, w):          # 가위날: 어깨에서 끝으로 가늘어지는 은빛 날 (왼쪽 / 오른쪽 어느 쪽이든)
    n = int(max(abs(x1 - x0), abs(y1 - y0))) * 2 + 1
    for i in range(n + 1):
        t = i / n
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
        hw = max(0.5, w * (1 - t * 0.85) / 2)
        for j in range(-int(hw + 1), int(hw + 1) + 1):
            if abs(j) <= hw:
                cv.set(x, y + j - 0.5, STEEL_L if j < -hw * 0.3 else STEEL if j < hw * 0.5 else STEEL_D)


def dummy_frame(view, variant=None):
    # view 0 = 정면 T 자세 · 1 = ¼ 돌아섬 · 2 = 뒷모습 · 3 = ¾ 돌아섬 (휘두름 0.25초 · 팔이 먼저 쓸고 나감)
    cv = Cv()
    flip = view == 2
    wide = view in (0, 2)
    tw = 8 if wide else 6                       # 몸통 폭
    tx = 16 - tw // 2
    la, ra = (8, 8) if wide else ((6, 3) if view == 1 else (3, 6))       # 왼 · 오른팔 길이
    # 다리
    for k, lx in enumerate((tx, tx + tw - 3) if wide else (tx, tx + 3)):
        chk_rect(cv, lx, 19, 3, 10, flip)
        joint(cv, lx + 1.5, 19, 1.2); joint(cv, lx + 1.5, 24, 1.2)
        cv.rect(lx - 1 + (0 if k == 0 else 1), 29, 4, 2, (60, 56, 84) if not flip else (60, 56, 84))
    # 몸통
    if variant == 'spool':
        cv.rect(tx - 1, 11, tw + 2, 2, WOOD); cv.rect(tx - 1, 11, tw + 2, 1, WOOD_L)
        cv.rect(tx - 1, 17, tw + 2, 2, WOOD_D); cv.rect(tx - 1, 17, tw + 2, 1, WOOD)
        for y in range(13, 17):
            for x in range(tx, tx + tw):
                cv.p[(x, y)] = (238, 232, 210) if (y + (x // 2)) % 2 == 0 else (150, 130, 206)
        cv.rect(tx, 13, tw, 1, (250, 246, 230))
    elif variant == 'windup':
        cv.rect(tx, 11, tw, 8, RED)
        cv.rect(tx, 11, 2, 8, RED_L)
        cv.rect(tx + tw - 2, 11, 2, 8, RED_D)
        cv.rect(tx, 16, tw, 1, (30, 22, 40))                       # 허리띠
        for yy in (12, 14):
            cv.set(tx + tw // 2 - 1, yy, GOLD); cv.set(tx + tw // 2, yy, GOLD)
        key = 16 + (0 if view in (0, 2) else (1 if view == 1 else -1))
        ww = 5 if wide else 3
        cv.line(key, 13, key, 17, GOLD_D, 1)                       # 등의 태엽 열쇠 (옆으로 삐져나옴)
        cv.rect(key - ww // 2 - 1, 13, 2, 3, GOLD); cv.rect(key + ww // 2, 13, 2, 3, GOLD)
        cv.rect(key - ww // 2 - 1, 13, 2, 1, GOLD_L); cv.rect(key + ww // 2, 13, 2, 1, GOLD_L)
    else:
        chk_rect(cv, tx, 11, tw, 8, flip)
    if flip:
        cv.rect(15, 11, 2, 8, (36, 30, 52)) if variant is None else None   # 뒷면 등 솔기
    # 팔 (T 자세 · 가로로 곧게)
    arms = ((tx - la, tx, -1, la), (tx + tw, tx + tw + ra, 1, ra))
    for (x0, x1, sgn, ln) in arms:
        if ln <= 0:
            continue
        if variant == 'scissor':
            sx = x1 if sgn < 0 else x0
            blade(cv, sx, 12.6, sx + sgn * ln * 1.0 * (1 if True else 0), 12.6, 3.6)
            joint(cv, sx, 12.5, 1.3)
        else:
            chk_rect(cv, x0, 12, ln, 3 if ln > 3 else 2, flip)
            joint(cv, x1 if sgn < 0 else x0, 13.5, 1.4)
            hx_ = x0 if sgn < 0 else x1
            cv.rect(hx_ - (1 if sgn < 0 else 0) , 12, 2, 3, PAPER_D if not flip else MAG_D) if ln > 3 else None
    # 머리 (얼굴 없음 · 격자) + 목 관절 + 매다는 고리
    if variant == 'windup':
        cv.rect(12, 5, 8, 2, (30, 22, 40)); cv.rect(11, 6, 10, 1, (30, 22, 40)); cv.rect(12, 3, 8, 3, RED_D)
        cv.rect(12, 5, 8, 1, GOLD)
    chk_disc(cv, 16, 8 if variant != 'windup' else 8.6, 3.4, flip)
    joint(cv, 16, 11, 1.2)
    cv.line(16, 1, 16, 4 if variant != 'windup' else 2, WHITE, 1)
    cv.set(16, 1, ALLY_L)
    cv.outline(ALLY)
    return cv


def dummy_sheet(name, variant=None):
    frames = [dummy_frame(v, variant) for v in range(4)]
    return sheet_out(WEAP, name, frames, 32, 32, ppu=8)


def weapons():
    out = []
    dummy_sheet('weapon_dummy')
    out.append(('weapon_dummy', 128, 32))
    for kid, nm in ((154, 'scissor'), (155, 'spool'), (156, 'windup')):
        dummy_sheet('weapon_dummy_' + nm, nm)
        out.append(('weapon_dummy_' + nm, 128, 32))
        im = dummy_frame(0, nm).image(32, 32)
        path = os.path.join(WEAP, 'weapon_kit_%d.png' % kid)
        save_png(im, path)
        meta_copy(os.path.join(WEAP, 'weapon_sword.png.meta'), path, 'Weapons/weapon_kit_%d' % kid, pivot=(0.5, 0.5), ppu=8)
        out.append(('weapon_kit_%d' % kid, 32, 32))
    return out


# ================================================================== 3. 이펙트 (FX)
SMOKE, SMOKE_L, SMOKE_D = (196, 176, 236), (236, 226, 255), (136, 112, 190)


def fx_puppet_swing():           # 휘두름 고리 · 반원 잔상 4프레임 (더미 둘레 반지름 1.7칸 = 27칸 · 아군색 · 초승달이 한 바퀴를 따라 돎)
    fr = []
    R = 26.5
    for f in range(4):
        cv = Cv()
        head = -2.3 + f * 1.45                         # 초승달 머리 각도 (끝에 가장 진함)
        span = 2.5
        for y in range(64):
            for x in range(64):
                dx, dy = x + 0.5 - 32, y + 0.5 - 32
                d = math.hypot(dx, dy)
                a = math.atan2(dy, dx)
                rel = (head - a) % TAU                   # 머리에서 몇 라디안 뒤인가
                if rel > span:
                    continue
                t = rel / span                           # 0 = 머리 · 1 = 꼬리
                th = 5.2 * (1 - t) + 0.8                 # 꼬리로 갈수록 가늘어짐
                if R - th <= d <= R:
                    c = ALLY_L if (d > R - 1.2 and t < 0.55) else ALLY if t < 0.7 else ALLY_D
                    if t < 0.06:
                        c = WHITE
                    cv.p[(x, y)] = c
        fade(cv, 0.0)
        fr.append(cv)
    return fx_out('fx_puppet_swing', fr, 64, 64)


def fx_string_cut():             # 줄에 베인 자리 실 반짝 3프레임
    fr = []
    for f in range(3):
        cv = Cv()
        L = [5, 7, 4][f]
        cv.line(16 - L, 16 + L * 0.6, 16 + L, 16 - L * 0.6, WHITE, 1)
        cv.line(16 - L + 1, 16 + L * 0.6 + 1, 16 + L - 1, 16 - L * 0.6 + 1, ALLY_D, 1)
        s = [2, 3, 2][f]
        for (dx, dy) in ((0, -s - 1), (0, s), (-s - 1, 0), (s, 0)):
            cv.set(16 + dx, 16 + dy, ALLY_L)
        cv.disc(16, 16, [1.5, 1.2, 0.8][f], WHITE)
        # 끊긴 실 조각이 튐
        r = random.Random(7 + f)
        for _ in range(3 + f * 2):
            a, d = r.random() * TAU, 4 + r.random() * (4 + f * 2)
            cv.set(16 + math.cos(a) * d, 16 + math.sin(a) * d * 0.8, ALLY if r.random() < 0.6 else WHITE)
        fade(cv, [0, 0, 0.45][f])
        fr.append(cv)
    return fx_out('fx_string_cut', fr, 32, 32)


def fx_string_twang():           # 현 울림 · 실 퉁기기 파동 (줄 길이만큼 가로로 늘여 쓰는 띠 · 4프레임)
    fr = []
    for f in range(4):
        cv = Cv()
        amp = [6.0, 4.0, 2.4, 1.0][f]
        for x in range(32):
            env = math.sin(x / 31.0 * math.pi)
            y = 8 + math.sin(x / 31.0 * TAU * (f + 1) * 0.5 + f) * amp * env
            cv.set(x, y, ALLY_L)
            cv.set(x, y + 1, ALLY)
            if amp > 2:
                cv.set(x, y + 2, ALLY_D)
        for x in (0, 31):
            cv.set(x, 8, WHITE)
        fade(cv, [0, 0, 0.1, 0.35][f])
        fr.append(cv)
    return fx_out('fx_string_twang', fr, 32, 16)


def fx_swap_poof():              # 자리 바꾸기 양 끝 연기 + 반짝 6프레임
    fr = []
    for f in range(6):
        cv = Cv()
        R = 3 + f * 2.4
        r = random.Random(30 + f)
        n = 8
        for i in range(n):
            a = TAU * i / n + r.random() * 0.4
            d = R * (0.55 + r.random() * 0.45)
            x, y = 16 + math.cos(a) * d, 17 + math.sin(a) * d * 0.8
            cv.disc(x, y, max(1.4, 4.0 - f * 0.45) * (0.8 + r.random() * 0.4), SMOKE_D)
        for i in range(n):
            a = TAU * i / n + 0.3 * math.sin(i)
            x, y = 16 + math.cos(a) * R * 0.5, 17 + math.sin(a) * R * 0.5 * 0.8
            cv.disc(x - 0.5, y - 0.7, max(1.2, 3.6 - f * 0.4), SMOKE)
            cv.disc(x - 1, y - 1.3, max(0.8, 1.6 - f * 0.2), SMOKE_L)
        if f < 4:                                      # 한가운데 반짝 (별 네 갈래)
            s = [3, 5, 4, 2][f]
            star_burst(cv, 16, 14, 4, s, s * 0.5, 2, WHITE, ALLY)
        fade(cv, max(0.0, (f - 2) * 0.24))
        fr.append(cv)
    return fx_out('fx_swap_poof', fr, 32, 32)


def fx_thread_mark():            # 꼭두각시 실 1~4 표시 — 프레임 n = 실 고리 n 개 (적 머리 위)
    fr = []
    for n in range(1, 5):
        cv = Cv()
        for i in range(n):
            cx = 3.5 + i * 3.0 if n > 1 else 8
            cx = 8 - (n - 1) * 1.6 + i * 3.2
            cy = 9 - (i % 2)
            cv.ring(cx, cy, 1.0, 2.1, WHITE)
            cv.set(cx - 0.5, cy - 2.5, ALLY)
        cv.line(2, 14, 14, 14, ALLY_D, 1)
        cv.set(8, 13, ALLY)
        # 가장 위에 매듭 한 점 (늘면 금색)
        cv.set(8, 12, GOLD if n >= 4 else ALLY_L)
        fr.append(cv)
    return fx_out('fx_thread_mark', fr, 16, 16)


def fx_curtain_string():         # 커튼콜 위에서 내려오는 세로 줄 (세로로 이어 붙여 씀 · 2프레임: 반짝이 한 칸 내려감)
    fr = []
    for f in range(2):
        cv = Cv()
        for y in range(32):
            cv.set(3, y, WHITE)
            cv.set(4, y, ALLY_L if y % 8 < 4 else ALLY)
        for y in range(2 + f * 4, 32, 8):
            cv.set(2, y, ALLY_L); cv.set(5, y, ALLY_L)
            cv.set(3, y + 1, WHITE); cv.set(4, y + 1, WHITE)
        fr.append(cv)
    return fx_out('fx_curtain_string', fr, 8, 32)


def fx_spotlight():              # 무대 조명 원 — 바닥에 깔리는 옅은 노란 원 (반지름 2.6칸 · 디더로 안쪽을 비춤)
    fr = []
    for f in range(2):
        cv = Cv()
        R = 30
        for y in range(64):
            for x in range(64):
                d = math.hypot(x + 0.5 - 32, y + 0.5 - 32)
                if d > R:
                    continue
                if d > R - 1.6:
                    cv.p[(x, y)] = GOLD_L if (x + y + f) % 2 == 0 else LIGHT
                else:
                    cv.p[(x, y)] = (255, 238, 160, 62 if d < R * 0.62 else 46)
        fr.append(cv)
    return fx_out('fx_spotlight', fr, 64, 64)


def fx_curtain_drop():           # 막 내림 — 붉은 벨벳 막이 내려와 주름이 잡힘 4프레임
    fr = []
    for f in range(4):
        cv = Cv()
        hgt = [8, 16, 22, 24][f]
        cv.rect(3, 4, 26, 2, GOLD_D); cv.rect(3, 4, 26, 1, GOLD)               # 가로대
        cv.rect(1, 3, 2, 4, GOLD); cv.rect(29, 3, 2, 4, GOLD)
        for x in range(4, 28):
            u = ((x - 4) % 6) / 5.0
            c = RED_L if u < 0.2 else RED if u < 0.7 else RED_D
            for y in range(6, 6 + hgt):
                cv.p[(x, y)] = c
            if f >= 2:                                                       # 아랫단 물결
                cv.p[(x, 5 + hgt)] = RED_D if (x // 3) % 2 == 0 else RED
        if f == 3:
            for x in range(4, 28, 6):
                cv.set(x + 1, 30, (120, 100, 140)); cv.set(x + 3, 29, (120, 100, 140))
        cv.outline(K1)
        for x in (5, 26):                                                    # 술
            cv.set(x, 6 + hgt, GOLD); cv.set(x, 7 + hgt, GOLD_D)
        fr.append(cv)
    return fx_out('fx_curtain_drop', fr, 32, 32)


def fx_scissor_snip():           # 거대한 가위가 닫힘 (싹둑) 6프레임 — 마지막 프레임은 닫힌 가위 + 별 번쩍
    fr = []
    cx, cy = 32, 34
    for f in range(6):
        cv = Cv()
        th = [0.78, 0.55, 0.34, 0.16, 0.04, 0.0][f]
        for sgn in (-1, 1):
            a = -math.pi / 2 + sgn * th
            dx, dy = math.cos(a), math.sin(a)
            px, py = -dy, dx
            L, W = 29, 6.2
            for i in range(int(L * 2)):
                t = i / (L * 2.0)
                for j in range(-4, 5):
                    wj = W * (1 - t * 0.9) / 2
                    if abs(j) <= wj:
                        x, y = cx + dx * t * L + px * j * (1 if True else 0), cy + dy * t * L + py * j
                        c = STEEL_L if j * sgn < -wj * 0.35 else STEEL if j * sgn < wj * 0.5 else STEEL_D
                        cv.set(x - 0.5, y - 0.5, c)
            # 손잡이: 날 반대쪽 고리
            hx_, hy_ = cx - dx * 6 + px * sgn * 4 * 0, cy - dy * 6
            ang = a + math.pi
            rx, ry = cx + math.cos(ang) * 8 + sgn * 3.5, cy + math.sin(ang) * 8
            cv.ring(rx, ry, 2.2, 4.0, RED)
            cv.ring(rx, ry, 3.4, 4.0, RED_D)
        cv.disc(cx, cy, 2.4, GOLD_D); cv.disc(cx, cy, 1.4, GOLD)
        if f >= 4:
            star_burst(cv, cx, cy - 26, 8, 8 if f == 4 else 11, 4.5 if f == 4 else 6, 2, WHITE, ALLY)
            cv.disc(cx, cy - 26, 2.0, WHITE)
        fr.append(cv)
    return fx_out('fx_scissor_snip', fr, 64, 64)


def ui_curtain_edge():           # 가장자리 붉은 벨벳 막 (왼쪽 가장자리용 · 9-slice 한 장 · 안쪽 끝에 금실 술과 그림자)
    w, h = 24, 48
    cv = Cv()
    for y in range(h):
        for x in range(w):
            k = (x + (2 if (y // 12) % 2 else 0)) % 8
            c = RED_L if k < 1 else RED if k < 5 else RED_D
            if x >= w - 4:
                c = RED_D if x < w - 2 else (60, 18, 36)
            if x == w - 5:
                c = GOLD_D
            cv.p[(x, y)] = c
    for y in range(0, h, 6):
        cv.p[(w - 5, y)] = GOLD
    out = os.path.join(UID, 'ui_curtain_edge.png')
    save_png(cv.image(w, h), out)
    meta_copy(os.path.join(UID, 'ui_button.png.meta'), out, 'UI/ui_curtain_edge', border=(2, 8, 6, 8), ppu=16)
    return cv.image(w, h)


def fxs():
    return [fx_puppet_swing(), fx_string_cut(), fx_string_twang(), fx_swap_poof(), fx_thread_mark(), fx_curtain_string(),
            fx_spotlight(), fx_curtain_drop(), fx_scissor_snip()]


# ================================================================== 4. 아이콘 (범주 틀 + 그림문자)
ROPE_L, ROPE_D = (222, 178, 104), (158, 110, 60)
SK, SKS = (242, 214, 208), (190, 160, 176)


def mini(cv, cx, ty, k=1, flip=False, arms=7, head=True):
    # 작은 T 자세 더미 (격자 2×2) — cx 가운데 · ty 머리 꼭대기 · k 1 = 키 17칸
    t = 2
    if head:
        chk_disc(cv, cx, ty + 2.5 * k, 2.6 * k, flip, 2)
    tw = int(4 * k) if k < 1 else 5
    th = int(7 * k)
    tx = int(cx - tw / 2.0)
    ty2 = ty + int(5 * k)
    chk_rect(cv, tx, ty2, tw, th, flip, 2)
    ay = ty2
    chk_rect(cv, tx - arms, ay, arms, 2, flip, 2)
    chk_rect(cv, tx + tw, ay, arms, 2, flip, 2)
    lh = int(6 * k)
    chk_rect(cv, tx, ty2 + th, 2, lh, flip, 2)
    chk_rect(cv, tx + tw - 2, ty2 + th, 2, lh, flip, 2)


def thread_curve(cv, x0, y0, x1, y1, sag, c, c2=None):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) * 2 + 2
    for i in range(n + 1):
        t = i / n
        x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t + math.sin(t * math.pi) * sag
        cv.set(x - 0.5, y - 0.5, c)
        if c2:
            cv.set(x - 0.5, y + 0.5, c2)


def curtain(cv, x, y, w, h, folds=4):
    for xx in range(x, x + w):
        u = ((xx - x) % max(2, w // folds)) / float(max(2, w // folds) - 1)
        c = RED_L if u < 0.2 else RED if u < 0.7 else RED_D
        for yy in range(y, y + h):
            cv.p[(xx, yy)] = c


# ---- 특수 능력 154~160 (아이콘 554~560)
def g554(cv):     # 가위 손 인형
    chk_rect(cv, 12, 14, 8, 9)
    chk_disc(cv, 16, 10.5, 3.2)
    joint(cv, 12, 15.5, 1.4); joint(cv, 20, 15.5, 1.4)
    blade(cv, 11, 15, 1, 7, 6.0); blade(cv, 21, 15, 31, 7, 6.0)
    cv.line(11, 15, 3, 9, STEEL_D, 1); cv.line(21, 15, 29, 9, STEEL_D, 1)
    cv.rect(12, 23, 3, 5, PAPER); cv.rect(17, 23, 3, 5, MAG); cv.rect(12, 23, 3, 1, PAPER_D)
    cv.set(5, 5, WHITE); cv.set(27, 5, WHITE); cv.set(4, 4, GOLD_L); cv.set(28, 4, GOLD_L)


def g555(cv):     # 실감개 인형
    cv.rect(8, 7, 16, 3, WOOD); cv.rect(8, 7, 16, 1, WOOD_L); cv.rect(8, 9, 16, 1, WOOD_D)
    cv.rect(8, 22, 16, 3, WOOD_D); cv.rect(8, 22, 16, 1, WOOD)
    for y in range(10, 22):
        for x in range(10, 22):
            cv.p[(x, y)] = (240, 234, 214) if (y + x // 3) % 3 else ALLY
    cv.rect(10, 10, 12, 1, (255, 250, 232)); cv.rect(10, 20, 12, 1, (160, 140, 200))
    chk_rect(cv, 3, 14, 6, 2); chk_rect(cv, 23, 14, 6, 2)
    chk_disc(cv, 16, 4.2, 2.4)
    thread_curve(cv, 21, 24, 28, 28, 2, ALLY_L)


def g556(cv):     # 태엽 병정
    cv.rect(12, 3, 8, 4, (36, 28, 52)); cv.rect(11, 7, 10, 1, (36, 28, 52)); cv.rect(12, 5, 8, 1, GOLD)
    chk_disc(cv, 16, 10.2, 2.8)
    cv.rect(10, 13, 12, 10, RED); cv.rect(10, 13, 3, 10, RED_L); cv.rect(19, 13, 3, 10, RED_D)
    cv.rect(10, 19, 12, 1, (30, 22, 40))
    for y in (14, 16):
        cv.set(15, y, GOLD); cv.set(16, y, GOLD)
    cv.rect(10, 23, 4, 5, (46, 36, 70)); cv.rect(18, 23, 4, 5, (46, 36, 70))
    cv.line(22, 16, 25, 16, GOLD_D, 1)
    cv.ring(26.5, 13, 1.2, 2.6, GOLD); cv.ring(26.5, 19, 1.2, 2.6, GOLD)
    cv.line(26, 14, 26, 17, GOLD, 1)


def g557(cv):     # 무대 체질 (조명 + 바닥 + 작은 게이지)
    cv.poly([(16, 4), (10, 22), (22, 22)], LIGHT)
    cv.poly([(16, 4), (13, 22), (19, 22)], GOLD_L)
    cv.rect(5, 22, 22, 3, WOOD); cv.rect(5, 22, 22, 1, WOOD_L); cv.rect(5, 24, 22, 1, WOOD_D)
    for x in range(7, 26, 5):
        cv.set(x, 23, WOOD_D)
    mini(cv, 16, 12, 0.5)
    cv.rect(24, 8, 3, 1, ALLY_L); cv.rect(25, 7, 1, 3, ALLY_L)


def g558(cv):     # 긴 팔 (한 칸 더 뻗은 팔 + 줄)
    cv.line(9, 22, 18, 13, PUR, 4)
    cv.line(9, 21, 18, 12, PUR_L, 1)
    cv.line(10, 23, 19, 14, PUR_D, 1)
    cv.disc(21, 11.5, 3.4, SK); cv.disc(21.6, 12, 2.0, SKS)
    cv.rect(19, 8, 2, 2, SK); cv.rect(22, 8, 2, 2, SK)
    for i in range(2):
        arrow_head(cv, 21 + i * 3, 20, 1, 0, GOLD, 2)
    cv.line(21, 5, 21, 8, WHITE, 1)


def g559(cv):     # 만원 관객 (머리가 빽빽한 객석 + 별)
    for row, (y, n) in enumerate(((19, 4), (14, 3))):
        for i in range(n):
            x = 8.5 + i * 5 + (2.5 if row else 0)
            cv.disc(x + 1.5, y, 3.0, (44, 36, 66))
            cv.disc(x + 1.2, y - 0.5, 2.4, (92, 80, 130))
            cv.set(x + 0.5, y - 0.5, SK); cv.set(x + 2.5, y - 0.5, SK)
    cv.rect(8, 22, 16, 2, (44, 36, 66)); cv.rect(8, 22, 16, 1, (92, 80, 130))
    star_burst(cv, 16, 9, 4, 3, 1.5, 1, GOLD, GOLD_D)
    cv.disc(16, 9, 1.0, WHITE)


def g560(cv):     # 텅 빈 몸 (속이 빈 망토 + 멀리 떠 있는 머리)
    cv.poly([(8, 28), (10, 12), (14, 9), (18, 9), (22, 12), (24, 28)], PUR)
    cv.poly([(11, 28), (12, 14), (15, 12), (17, 14), (19, 28)], (24, 18, 40))
    cv.line(8, 28, 24, 28, PUR_D, 1)
    cv.rect(9, 12, 2, 14, PUR_L)
    cv.set(14, 17, (80, 70, 120)); cv.set(17, 17, (80, 70, 120))
    chk_disc(cv, 25, 5.5, 2.8)
    thread_curve(cv, 25, 8, 20, 13, 1, WHITE)


# ---- 전용 카드 760~765
def g760(cv):     # 실 자국
    foe(cv, 20, 18, 4.4)
    thread_curve(cv, 3, 8, 27, 24, -4, WHITE, ALLY_D)
    for dy in (-5, 5):
        for i in range(0, 20, 3):
            cv.set(5 + i, 11 - dy * 0.0 + i * 0.6 + dy * 0.0 + (3 if dy > 0 else -3), ALLY if i % 6 == 0 else ALLY_D)
    cv.set(4, 7, ALLY_L); cv.set(3, 9, ALLY_L)


def g761(cv):     # 팔 벌린 인형
    arc(cv, 16, 16, 12.5, 3.5, 5.9, ALLY, 2)
    arc(cv, 16, 16, 12.5, 0.4, 2.8, ALLY, 2)
    mini(cv, 16, 7, 1.0, arms=9)
    cv.set(3, 14, WHITE); cv.set(28, 14, WHITE)


def g762(cv):     # 끊어진 실
    thread_curve(cv, 4, 4, 15, 15, 1, WHITE)
    for dx, dy in ((0, 0), (1, -1), (-1, 1), (2, 1)):
        cv.set(15 + dx, 15 + dy, ALLY_D)
    cv.line(17, 14, 20, 11, WHITE, 1); cv.line(18, 16, 22, 17, WHITE, 1)
    foe(cv, 24, 22, 4.2)
    arc(cv, 18, 20, 7, -1.3, 0.1, ALLY, 2)
    arrow_head(cv, 25, 20, 1, 1, ALLY_L, 2)
    cv.disc(6, 24, 2.2, GRND)
    cv.rect(3, 22, 2, 1, WHITE); cv.rect(8, 22, 2, 1, WHITE)


def g763(cv):     # 빈자리의 인형 (사라진 영웅의 점선 윤곽 + 남은 더미)
    for (x, y) in [(9, 6), (11, 6), (13, 6), (8, 8), (14, 8), (8, 10), (14, 10), (9, 12), (13, 12), (6, 14), (8, 14), (14, 14), (16, 14),
                   (6, 17), (16, 17), (6, 20), (16, 20), (6, 23), (8, 23), (10, 23), (12, 23), (14, 23), (16, 23)]:
        cv.set(x, y, ALLY)
    mini(cv, 23, 8, 0.85, arms=5)
    cv.line(23, 6, 23, 3, WHITE, 1)
    cv.set(11, 9, ALLY_L); cv.set(9, 8, ALLY_L)


def g764(cv):     # 짧은 막간 (막이 반쯤 걷힌 무대 + 작아진 게이지 눈금)
    cv.rect(3, 4, 26, 3, GOLD_D); cv.rect(3, 4, 26, 1, GOLD)
    curtain(cv, 4, 7, 7, 14, 2); curtain(cv, 21, 7, 7, 14, 2)
    cv.rect(11, 7, 10, 14, (46, 34, 70)); cv.rect(11, 7, 10, 2, (68, 54, 100))
    cv.rect(5, 24, 22, 4, (36, 30, 54)); cv.rect(5, 24, 22, 1, STEEL_D)
    cv.rect(6, 25, 9, 2, (112, 190, 210))
    for x in (11, 16, 21):
        cv.rect(x, 24, 1, 4, GOLD)


def g765(cv):     # 관객의 박수 (손뼉 + 번쩍임 + 게이지 +)
    cv.line(8, 26, 14, 12, SK, 5); cv.line(10, 26, 15, 13, SKS, 1)
    cv.line(24, 26, 18, 12, SK, 5); cv.line(22, 26, 17, 13, SKS, 1)
    for k in range(4):                                  # 손가락 줄
        cv.line(10 + k, 12 - k * 0 , 12 + k, 9, SK, 2) if False else None
    cv.line(12, 11, 14, 8, SK, 2); cv.line(20, 11, 18, 8, SK, 2)
    cv.line(14, 12, 15, 9, SKS, 1); cv.line(18, 12, 17, 9, SKS, 1)
    star_burst(cv, 16, 4, 8, 4, 2, 1, GOLD, GOLD_D)
    cv.disc(16, 4, 1.2, WHITE)
    for (x, y) in ((5, 8), (27, 8), (4, 14), (28, 14)):
        cv.set(x, y, GOLD); cv.set(x + (1 if x < 16 else -1), y - 1, GOLD)
    cv.rect(24, 22, 4, 1, (112, 190, 210)); cv.rect(25, 21, 2, 3, (112, 190, 210))


# ---- 고유 스킬 860~869
def g860(cv):     # 무대 조명
    cv.poly([(5, 3), (12, 3), (26, 25), (10, 25)], LIGHT)
    cv.poly([(7, 3), (11, 3), (22, 24), (13, 24)], GOLD_L)
    cv.ell(17, 26, 11, 3, GOLD_D); cv.ell(17, 25.4, 10, 2.4, LIGHT)
    foe(cv, 17, 21, 3.4)
    cv.rect(3, 2, 10, 3, STEEL_D); cv.rect(3, 2, 10, 1, STEEL_L); cv.disc(8, 4.5, 2, GOLD_L)
    cv.set(26, 10, GOLD); cv.set(27, 9, GOLD); cv.set(27, 11, GOLD)


def g861(cv):     # 현 울림
    for k, (a, y) in enumerate(((5.5, 0), (3.2, 0), (1.6, 0))):
        for x in range(3, 29):
            yy = 16 + math.sin((x - 3) / 25.0 * TAU * (k + 1.5)) * a * math.sin((x - 3) / 25.0 * math.pi)
            cv.set(x, yy, WHITE if k == 0 else ALLY if k == 1 else ALLY_D)
            if k == 0:
                cv.set(x, yy + 1, ALLY_L)
    cv.rect(2, 12, 2, 8, WOOD); cv.rect(28, 12, 2, 8, WOOD)
    cv.rect(2, 12, 1, 8, WOOD_L); cv.rect(29, 12, 1, 8, WOOD_D)
    arc(cv, 16, 16, 12, -0.5, 0.5, ALLY_L, 1)
    cv.set(24, 7, GOLD); cv.set(8, 25, GOLD)


def g862(cv):     # 복화술
    chk_disc(cv, 13, 15, 6.2)
    cv.rect(8, 17, 3, 1, JOINT); cv.rect(15, 17, 3, 1, JOINT) if False else None
    cv.rect(10, 18, 6, 2, (60, 20, 70)); cv.rect(11, 18, 4, 1, RED_L)
    cv.poly([(20, 4), (30, 4), (30, 11), (25, 11), (22, 14), (22, 11), (20, 11)], WHITE)
    cv.rect(24, 5, 2, 4, RED); cv.rect(24, 10, 2, 1, RED)
    for (fx_, fy_, dx, dy) in ((4, 4, 1, 1), (28, 25, -1, -1), (4, 26, 1, -1)):
        foe(cv, fx_, fy_, 2.6)
        arrow_head(cv, fx_ + dx * 5, fy_ + dy * 5, dx, dy, ALLY_L, 3)


def g863(cv):     # 솜 채우기
    for (x, y, r) in ((10, 17, 5.6), (17, 13, 6.4), (22, 19, 5.2), (15, 21, 5.0)):
        cv.disc(x, y, r, (214, 208, 224))
    for (x, y, r) in ((10, 16, 4.8), (17, 12, 5.6), (22, 18, 4.4), (15, 20, 4.2)):
        cv.disc(x - 0.5, y - 0.5, r, WHITE)
    for i in range(5):                                  # 꿰맨 자국
        cv.line(7 + i * 4, 25, 9 + i * 4, 27, (60, 36, 70), 1)
        cv.line(9 + i * 4, 25, 7 + i * 4, 27, (60, 36, 70), 1)
    cv.rect(23, 5, 6, 2, GRN); cv.rect(25, 3, 2, 6, GRN)
    cv.set(26, 4, WHITE); cv.set(24, 5, WHITE)


def g864(cv):     # 줄타기
    cv.line(2, 24, 29, 18, ROPE_D, 2)
    cv.line(2, 23, 29, 17, ROPE_L, 1)
    cv.rect(2, 22, 2, 8, WOOD); cv.rect(28, 12, 2, 12, WOOD)
    mini(cv, 15, 4, 0.8, arms=8)
    cv.line(7, 12, 23, 12, WOOD_D, 1)
    for y in (6, 9):
        cv.line(2, y, 6, y, ALLY_L, 1); cv.line(1, y + 1, 4, y + 1, ALLY, 1)


def g865(cv):     # 막 내림
    cv.rect(3, 3, 26, 3, GOLD_D); cv.rect(3, 3, 26, 1, GOLD)
    curtain(cv, 5, 6, 22, 11, 4)
    for x in range(5, 27):
        cv.set(x, 17, RED_D if (x // 3) % 2 else RED)
    for (x, y) in ((6, 22), (11, 25), (18, 23), (24, 26), (14, 20), (22, 20)):
        cv.disc(x, y, 1.6, (230, 80, 70))
        cv.set(x, y - 1, (255, 190, 170))
    arrow_head(cv, 16, 21, 0, 1, GOLD_L, 3)
    cv.line(16, 18, 16, 20, GOLD_L, 1)


def g866(cv):     # 앙코르 (남은 더미 하나와 되풀이 화살표)
    mini(cv, 16, 7, 0.9, arms=6)
    arc(cv, 16, 16, 12.5, 0.4, 4.9, ALLY, 2)
    arrow_head(cv, 16 + math.cos(4.9) * 12.5 - 1, 16 + math.sin(4.9) * 12.5, -1, 0, ALLY_L, 3)
    cv.set(26, 25, GOLD); cv.set(25, 26, GOLD)
    star_burst(cv, 5, 6, 4, 3, 1.5, 1, GOLD, GOLD_D)


def g867(cv):     # 얽힌 실
    foe(cv, 16, 16, 4.4)
    for k in range(5):
        a = k * 1.25
        rx, ry = 11 + (k % 2) * 2, 7 + (k % 3)
        for i in range(60):
            t = i / 60 * TAU
            x = 16 + math.cos(t) * rx * math.cos(a) - math.sin(t) * ry * math.sin(a)
            y = 16 + math.cos(t) * rx * math.sin(a) + math.sin(t) * ry * math.cos(a)
            cv.set(x - 0.5, y - 0.5, WHITE if k % 2 == 0 else ALLY)
    foe(cv, 16, 16, 3.2)
    cv.rect(24, 25, 4, 1, ALLY_L); cv.rect(22, 27, 6, 1, ALLY_L)


def g868(cv):     # T 자세
    mini(cv, 16, 6, 1.1, arms=9)
    arc(cv, 16, 16, 14, 0.3, 2.8, ALLY, 1)
    arc(cv, 16, 16, 14, 3.5, 6.0, ALLY, 1)
    cv.set(16, 2, GOLD); cv.set(15, 3, GOLD); cv.set(17, 3, GOLD)


def g869(cv):     # 실 퉁기기
    cv.rect(2, 10, 2, 10, WOOD); cv.rect(28, 10, 2, 10, WOOD)
    cv.rect(2, 10, 1, 10, WOOD_L)
    for x in range(4, 28):
        y = 21 - math.sin((x - 4) / 23.0 * math.pi) * 8
        cv.set(x, y, WHITE); cv.set(x, y + 1, ALLY)
    cv.disc(16, 12.5, 2.6, SK); cv.rect(15, 8, 2, 3, SK)
    cv.disc(16.4, 13, 1.2, SKS)
    for (x, y, dx, dy) in ((6, 12, -1, -1), (26, 12, 1, -1), (3, 22, -1, 1), (29, 22, 1, 1)):
        cv.line(x, y, x + dx * 2, y + dy * 2, GOLD, 1)
    cv.set(16, 25, GOLD)


# ---- 스킬 진화 960~966 (육각)
def g960(cv):     # 하프 현
    arc(cv, 16, 17, 11, 3.5, 6.0, WOOD, 3)
    cv.line(7, 17, 7, 26, WOOD, 3); cv.line(7, 25, 25, 25, WOOD_D, 3)
    cv.line(25, 24, 25, 9, WOOD_D, 2)
    for i in range(6):
        x = 10 + i * 2.8
        cv.line(x, 8 + i * 0.7, x, 25, WHITE if i % 2 == 0 else ALLY_L, 1)
    cv.set(12, 6, GOLD); cv.set(11, 5, GOLD); cv.set(13, 5, GOLD)
    cv.rect(6, 25, 20, 1, WOOD_L)


def g961(cv):     # 갑옷 인형
    mini(cv, 16, 4, 1.0, arms=8)
    cv.rect(11, 14, 10, 8, STEEL); cv.rect(11, 14, 3, 8, STEEL_L); cv.rect(18, 14, 3, 8, STEEL_D)
    cv.rect(15, 15, 2, 6, GOLD)
    cv.rect(9, 12, 4, 3, STEEL_L); cv.rect(19, 12, 4, 3, STEEL)
    cv.rect(12, 3, 8, 2, STEEL_D); cv.rect(13, 6, 6, 1, K1)
    cv.line(16, 0, 16, 3, STEEL_L, 1) if False else None


def g962(cv):     # 퀵 체인지 (영웅 · 더미가 자리를 맞바꿈)
    cv.poly([(3, 28), (5, 14), (8, 12), (11, 14), (13, 28)], PUR)
    cv.rect(3, 26, 10, 2, PUR_D)
    cv.disc(8, 8.5, 2.8, SK); cv.rect(6, 5, 5, 2, (56, 44, 88))
    mini(cv, 24, 6, 0.8, arms=3)
    cv.line(13, 13, 20, 13, ALLY_L, 2); arrow_head(cv, 21, 13, 1, 0, ALLY_L, 3)
    cv.line(19, 21, 13, 21, GOLD, 2); arrow_head(cv, 12, 21, -1, 0, GOLD, 3)
    star_burst(cv, 16, 17, 4, 2.5, 1.5, 1, WHITE, ALLY)


def g963(cv):     # 극단
    cv.rect(3, 3, 26, 4, RED); cv.rect(3, 3, 26, 1, RED_L); cv.rect(3, 6, 26, 1, RED_D)
    for x in range(4, 28, 4):
        cv.rect(x, 7, 2, 2, RED_D)
    cv.rect(3, 26, 26, 2, WOOD); cv.rect(3, 26, 26, 1, WOOD_L)
    mini(cv, 7, 10, 0.7, arms=3); mini(cv, 16, 9, 0.85, arms=5); mini(cv, 25, 10, 0.7, arms=3, flip=True)
    for x in (4, 27):
        cv.line(x, 9, x, 14, GOLD, 1)


def g964(cv):     # 천 가닥의 실
    cv.line(16, 3, 16, 12, WHITE, 1)
    for i in range(7):
        x1 = 3 + i * 4.3
        thread_curve(cv, 16, 12, x1, 22, 1.5, WHITE if i % 2 == 0 else ALLY_L)
    cv.disc(16, 12, 2.2, GOLD); cv.set(16, 11, WHITE)
    foe(cv, 8, 25, 2.8); foe(cv, 24, 25, 2.8); foe(cv, 16, 26, 2.2)
    cv.rect(13, 1, 6, 2, WOOD)


def g965(cv):     # 가르는 퇴장 (출구 문 + 가르는 번쩍)
    cv.rect(18, 7, 10, 20, WOOD_D); cv.rect(19, 9, 8, 18, (24, 18, 40)); cv.rect(18, 7, 10, 1, WOOD_L)
    cv.rect(20, 11, 6, 3, GRND); cv.rect(21, 12, 4, 1, GRN)
    cv.line(2, 26, 17, 6, GOLD_L, 3)
    cv.line(3, 26, 18, 6, GOLD, 1)
    star_burst(cv, 10, 16, 4, 6, 3, 2, WHITE, GOLD)
    mini(cv, 24, 14, 0.5, arms=2) if False else None
    cv.set(8, 24, ALLY); cv.set(5, 21, ALLY)


def g966(cv):     # 인형 호위대
    cv.poly([(6, 8), (26, 8), (26, 19), (16, 28), (6, 19)], STEEL_D)
    cv.poly([(8, 10), (24, 10), (24, 18), (16, 25), (8, 18)], BLUE_S)
    cv.rect(15, 10, 2, 15, STEEL_L); cv.rect(8, 15, 16, 2, STEEL_L)
    chk_disc(cv, 16, 5.2, 2.8)
    for x in (5, 27):
        cv.line(x, 2, x, 8, WHITE, 1)
    cv.rect(10, 12, 3, 3, GOLD); cv.rect(19, 12, 3, 3, GOLD)


BLUE_S = (110, 128, 255)


# ---- 무기 강화 618~620
def g618(cv):     # 대역 배우 (남은 더미가 터짐)
    star_burst(cv, 20, 15, 8, 11, 6, 3, GOLD, GOLD_D)
    mini(cv, 20, 6, 0.9, arms=5)
    for (x, y) in [(5, 8), (7, 8), (4, 10), (8, 10), (4, 13), (8, 13), (5, 15), (7, 15), (3, 18), (9, 18), (3, 21), (9, 21), (3, 24), (5, 24), (7, 24), (9, 24)]:
        cv.set(x, y, ALLY)
    cv.disc(20, 15, 2, WHITE)


def g619(cv):     # 두 번째 실 (조종대에서 두 가닥)
    cv.rect(8, 4, 16, 2, WOOD); cv.rect(8, 4, 16, 1, WOOD_L); cv.rect(15, 2, 2, 6, WOOD)
    thread_curve(cv, 9, 6, 12, 18, 1, WHITE)
    thread_curve(cv, 23, 6, 20, 18, 1, ALLY)
    thread_curve(cv, 16, 7, 16, 14, 0, ALLY_L)
    mini(cv, 16, 15, 0.6, arms=4)
    cv.set(12, 12, GOLD); cv.set(20, 12, GOLD)


def g620(cv):     # 무거운 머리
    chk_disc(cv, 16, 11, 8.2)
    cv.ell(16, 11, 8, 1.2, JOINT) if False else None
    joint(cv, 16, 19.5, 1.4)
    chk_rect(cv, 13, 20, 6, 4)
    chk_rect(cv, 8, 20, 5, 2); chk_rect(cv, 19, 20, 5, 2)
    cv.rect(13, 24, 2, 4, PAPER); cv.rect(17, 24, 2, 4, MAG)
    arrow_head(cv, 16, 31, 0, 1, GOLD, 3) if False else None
    for x in (4, 27):
        cv.line(x, 4, x, 9, GOLD, 1); arrow_head(cv, x, 10, 0, 1, GOLD, 2)


ICONS = [
    (554, 'square', 'weapon', g554), (555, 'square', 'weapon', g555), (556, 'square', 'weapon', g556),
    (557, 'diamond', 'survival', g557), (558, 'diamond', 'survival', g558), (559, 'diamond', 'survival', g559), (560, 'diamond', 'survival', g560),
    (760, 'square', 'weapon', g760), (761, 'square', 'weapon', g761), (762, 'square', 'weapon', g762), (763, 'square', 'weapon', g763),
    (764, 'circle', 'ultimate', g764), (765, 'circle', 'ultimate', g765),
    (860, 'circle', 'skill', g860), (861, 'circle', 'skill', g861), (862, 'circle', 'skill', g862), (863, 'circle', 'skill', g863),
    (864, 'circle', 'skill', g864), (865, 'circle', 'skill', g865), (866, 'circle', 'skill', g866), (867, 'circle', 'skill', g867),
    (868, 'circle', 'skill', g868), (869, 'circle', 'skill', g869),
    (960, 'hex', 'soul', g960), (961, 'hex', 'soul', g961), (962, 'hex', 'soul', g962), (963, 'hex', 'soul', g963),
    (964, 'hex', 'soul', g964), (965, 'hex', 'soul', g965), (966, 'hex', 'soul', g966),
    (618, 'square', 'weapon', g618), (619, 'square', 'weapon', g619), (620, 'square', 'weapon', g620),
]


def icon_image(shape, cat, glyph):
    px = FI.frame(shape, cat)
    cv = Cv()
    glyph(cv)
    g = {p: c[:3] for p, c in cv.p.items() if p in px}
    band = 3.6 if shape == 'diamond' else 3.0
    for (x, y) in list(g):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            q = (x + dx, y + dy)
            if q not in g and q in px and FI.depth(shape, *q) > band:
                px[q] = K1
    px.update(g)
    im = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    for (x, y), c in px.items():
        im.putpixel((x, y), tuple(c[:3]) + (255,))
    return im


def icons():
    out = []
    tmpl = os.path.join(ICON, 'ability_100.png.meta')
    for num, shape, cat, fn in ICONS:
        im = icon_image(shape, cat, fn)
        path = os.path.join(ICON, 'ability_%d.png' % num)
        save_png(im, path)
        meta_copy(tmpl, path, 'Icons/ability_%d' % num)
        out.append((num, im))
    return out


# ================================================================== 미리보기
def preview(folder, body, icns):
    os.makedirs(folder, exist_ok=True)
    bg = (60, 58, 76, 255)
    sc = 6
    S = Image.new('RGBA', (192 * sc, 32 * sc), bg)
    b = Image.new('RGBA', (192, 32), bg); b.alpha_composite(body)
    S.paste(b.resize((192 * sc, 32 * sc), Image.NEAREST), (0, 0))
    S.save(os.path.join(folder, 'body.png'))
    cols = 11
    I = Image.new('RGBA', (cols * 100, ((len(icns) + cols - 1) // cols) * 100), (30, 24, 44, 255))
    for k, (n, im) in enumerate(icns):
        I.alpha_composite(im.resize((96, 96), Image.NEAREST), ((k % cols) * 100 + 2, (k // cols) * 100 + 2))
    I.save(os.path.join(folder, 'icons.png'))
    items = []
    for nm in ('weapon_dummy', 'weapon_dummy_scissor', 'weapon_dummy_spool', 'weapon_dummy_windup'):
        items.append((Image.open(os.path.join(WEAP, nm + '.png')).convert('RGBA'), 4))
    for nm in ('fx_puppet_swing', 'fx_string_cut', 'fx_string_twang', 'fx_swap_poof', 'fx_thread_mark', 'fx_curtain_string',
               'fx_spotlight', 'fx_curtain_drop', 'fx_scissor_snip'):
        items.append((Image.open(os.path.join(FXD, nm + '.png')).convert('RGBA'), 3 if nm in ('fx_puppet_swing', 'fx_spotlight', 'fx_scissor_snip') else 4))
    items.append((Image.open(os.path.join(UID, 'ui_curtain_edge.png')).convert('RGBA'), 4))
    W = 1500
    x = y = 0; rowh = 0
    pos = []
    for im, s in items:
        w, h = im.width * s, im.height * s
        if x + w > W:
            x, y, rowh = 0, y + rowh + 8, 0
        pos.append((im, s, x, y)); x += w + 8; rowh = max(rowh, h)
    F = Image.new('RGBA', (W, y + rowh + 8), (70, 66, 86, 255))
    for im, s, x, y in pos:
        F.alpha_composite(im.resize((im.width * s, im.height * s), Image.NEAREST), (x, y))
    F.save(os.path.join(folder, 'fx.png'))


def main():
    sheet = body_sheet()
    for w in weapons():
        print('%-22s %3d×%-2d' % w)
    fxs()
    ui_curtain_edge()
    icns = icons()
    print('아이콘', len(icns), '장')
    if len(sys.argv) > 1:
        preview(sys.argv[1], sheet, icns)


if __name__ == '__main__':
    main()
