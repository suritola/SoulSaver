"""개발자 엄마 (무한 모드 30분 최종 보스) — 새 32칸 설계도 → 158칸 다시 그리기 (그래픽 개편 2단계 묶음 5)

배율 15 × 1.75 = 26.25 (거울의 군주 프리팹 15 × EndlessMode 1.75) · PPU 158 = round(6 × 26.25) → 그림 1칸 = 1080p 4.08 화소.
쓰는 그림 4장: Resources/Abyss/mom_0~3 (새 · AbyssBody 8fps). 없으면 예전처럼 개발자 그림 + 분홍 물들이기 (EndlessMode).
디자인 (기획서 2-4 · 「개발자 엄마」 — 슬리퍼 · 효자손 · 타임 세일 · 빗자루 · 「불 끄고 자!」 잔소리):
  위 = 둥근 뽀글 파마 구름 + 분홍 헤어롤 셋 · 머리 위 금 후광(개발자 후광과 짝 · 더 굵게)
  얼굴 · 손 = 개발자와 같은 살색 단계 (가족) · 돋보기 안경 + 금줄 · 반달 눈웃음 + 입꼬리 · 볼 홍조
  아래 = 넓은 A자 분홍 가디건 + 흰 앞치마 + 하트 주머니 · 오른손에 치켜든 슬리퍼(공격 fx_slipper 와 같은 모양 · 색) · 왼손 허리
  옆 = 「!」 잔소리 말풍선 둘 (개발자 「< >」 자리 · 경계 폭) · 발에 분홍 슬리퍼
설계도: hires/bosses/blueprints/mom_32.png (손 격자 handmaps/mom_bp.txt 프레임 0 + 부위 옮김 4프레임). 도감 아이콘 = 설계도 첫 칸.
그리는 법: 가디건 · 앞치마 · 얼굴 · 후광 명암 = 손 구간(handmaps/runs/mom.txt), 파마 = 손으로 놓은 곱슬 덩이(가운데 · 반지름)마다 공 모양 명암,
눈 · 안경 · 손 · 슬리퍼 · 하트 · 말풍선 = 손 도트 지도(handmaps/mom.txt). 칸 좌표 = 설계도 칸 × 4.9375 + (2, −2.9).
프레임 (설계도와 같음, 5칸 = 설계도 1칸): 머리 0/+5/+5/0 (잔소리 끄덕) · 말풍선 위아래 엇갈림 · 슬리퍼 0/0/−5/−5 까딱 · 안경 반짝 2.
"""
import numpy as np
from scipy import ndimage

from .. import blueprint as bp
from .. import detail, shade
from . import abyss_kit as kit

NAME = "mom"
CELL = 158
K = CELL / 32.0                 # 4.9375
SCALE = 26.25                   # 15 × 1.75
PPU = CELL
EXTRUDE = int(round(K))         # 5
FILES = [f"Assets/Resources/Abyss/mom_{i}.png" for i in range(4)]
ASSET = FILES[0]
OLD = None
H = bp.hexrgb

PAL = {
    "perm": [H("#3A1E3A"), H("#5A2E52"), H("#7E4470"), H("#A4628E")],
    "pink": [H("#8A3A64"), H("#AA5078"), H("#D0709A"), H("#F08CB4"), H("#FFB8D4")],
    "skin": [H("#A65E50"), H("#D88E72"), H("#F2BC96"), H("#FFDDBC")],
    "gold": [H("#6E4618"), H("#B46826"), H("#EAAA3A"), H("#FEDE6C"), H("#FFF6C8")],
    "apron": [H("#8686A6"), H("#ACACC6"), H("#D4D4E4"), H("#EEEEF6")],
    "heart": [H("#B02848"), H("#E04868")],
    "glass": [H("#1E3E6A"), H("#50D6EA"), H("#B0F8FF"), H("#FFFFFF")],     # 렌즈 = 개발자 안경과 같은 하늘색 (가족)
    "hood": [H("#3C3C80"), H("#5E62B4"), H("#7E88D4")],                    # 머리핀 = 개발자 후드 남색
    "wood": [H("#5B3629"), H("#8A5236"), H("#B87A4C")],                    # 빗자루 자루 · 효자손
}
OL_COOL = H("#2A1230")
WARM = ("gold", "skin")
# 프레임 옮김 (칸): 머리 · 말풍선1 · 말풍선2 · 슬리퍼 — 설계도 OFF × 4.9375 반올림
OFF = [(0, 0, 0, 0), (5, 5, -5, 0), (5, 0, 0, -5), (0, 5, -5, -5)]


def px(x):
    return x * K + 2.0


def py(y):
    return (y - 1) * K + 2.0


HALO_O = (80.0, 10.0, 28.0, 7.6)
HALO_I = (80.6, 11.0, 13.6, 2.3)
FACE = (77.0, 54.5, 23.5, 18.5)
# 파마 곱슬 덩이 (가운데 x, y, 반지름) — 손으로 놓음 · 크기 · 간격 섞음 (같은 공 도장 금지)
PERM = [(76, 40, 17), (50, 30, 8), (58, 23.5, 10), (70, 19, 8), (81, 21.5, 11), (93, 19, 8), (102.5, 25, 9.5), (110, 32.5, 7),
        (42, 38, 9), (53, 35, 6.4), (62, 30.5, 7), (88, 30, 6), (98, 33, 7), (114.5, 40, 9.1), (34, 48.5, 10.5), (27.5, 58, 7),
        (36.5, 60.5, 6), (30, 68, 5), (120, 50, 9), (124.5, 61, 6), (115.5, 63, 7.5), (122, 70, 5)]
BANGS = [(58, 39, 5.5), (67, 36.5, 6.5), (78, 35.5, 5.6), (88, 36.5, 6.4), (97, 39.5, 5)]
CURL_HL = [("cA", 52, 16), ("cB", 74, 12), ("cC", 96, 13), ("cD", 27, 42), ("cE", 114, 43), ("cF", 39, 30),
           ("cG", 64, 25), ("cH", 88, 24), ("cI", 31, 54), ("cJ", 118, 56)]
BUTTONS = [("btnA", 49, 102), ("btnB", 47, 116), ("btnC", 45, 130), ("btnD", 104, 104), ("btnE", 106, 118), ("btnF", 108, 132)]
CARD = [(64, 70), (90, 70), (102, 74), (113, 79), (121, 85), (131, 89), (140, 94), (142.4, 99.5), (137, 104), (129, 104.5), (129.5, 111),
        (133, 118), (136.5, 126), (141, 134), (146, 141), (140, 145.2), (132, 143.4), (124, 147.4), (114, 144.2), (104, 148.2), (92, 145),
        (80, 149), (68, 145.2), (56, 148.2), (44, 144.4), (32, 147.4), (22, 143.4), (12, 146), (7, 141), (11, 133), (16, 124), (19.4, 116),
        (18.6, 108), (22, 100), (27, 93), (32, 86), (41, 78), (52, 72)]
ARM_L = [(26, 59), (32, 62), (40, 71), (47, 82), (40, 90), (31, 84), (22, 74), (16, 67)]
APRON = [(58, 74), (95, 74), (99, 84), (98, 96), (100, 106), (103, 120), (106, 132), (108, 138), (104, 142.4), (98, 139.2), (91, 143.2),
         (84, 140), (77, 144.2), (70, 140.2), (63, 143.2), (56, 139.4), (49, 141.4), (46, 136), (49, 124), (52, 110), (55, 98), (55, 84)]


def ell(C, e, dx=0, dy=0):
    cx, cy, rx, ry = e
    return kit.ellipse(C, cx + dx, cy + dy, rx, ry)


PERM_GRAIN = (0.035, 0.9, 1.3)       # 곱슬 결 (세기 · 가로 · 세로 진동수)


def perm_paint(p, lobes, dy=0, cloud=(76, 40, 50, 32), cuts=(0.34, 0.56, 0.76)):
    """파마 = 구름 전체 둥근 형태(큰 타원체) 빛 0.55 + 곱슬 덩이마다 공 빛 0.45 → 4단.
    덩이는 나중 것이 앞 (앞 덩이 가장자리 아래 · 오른쪽 한 칸 = 그늘 → 곱슬 경계)"""
    C = p.C
    ys, xs = np.mgrid[0:C, 0:C]
    gx, gy, grx, gry = cloud
    hg = shade.ellipsoid((C, C), gx, gy + dy, grx, gry)
    owner = np.full((C, C), -1)
    lamL = np.zeros((C, C))
    for i, (cx, cy, r) in enumerate(lobes):
        cy += dy
        d = ((xs + 0.5 - cx) / r) ** 2 + ((ys + 0.5 - cy) / r) ** 2
        m = d <= 1.0
        h = shade.ellipsoid((C, C), cx, cy, r + 0.6, r + 0.6)
        n = shade.from_height(h, m, depth=r)
        lamL[m] = shade.lambert(n, shade.LIGHT, wrap=0.3)[m]
        owner[m] = i
    allm = owner >= 0
    ng = shade.from_height(hg, allm, depth=30)
    lamG = shade.lambert(ng, shade.LIGHT, wrap=0.3)
    v = 0.62 * lamG + 0.38 * lamL
    ga, gx_, gy_ = PERM_GRAIN
    t = np.digitize(v + ga * np.sin(xs * gx_ + ys * gy_), cuts)      # 곱슬 결: 띠 경계가 곧게 이어지지 않게 (공 덩이 명암 줄임)
    # 앞 덩이 경계: 이웃(왼 · 위)이 다른 덩이인 칸 = 한 단계 어둡게 (덩이 사이 곱슬 홈)
    seam = allm & (((np.roll(owner, 1, 1) != owner) & np.roll(allm, 1, 1)) | ((np.roll(owner, 1, 0) != owner) & np.roll(allm, 1, 0)))
    t = np.where(seam, np.maximum(t - 1, 0), t)
    p.mat[allm] = "perm"
    p.tone[allm] = t[allm]
    return allm


BUB1 = [(130, 3.6), (143, 3.0), (154, 4), (155.4, 13), (154, 23), (143, 25), (131, 24.4), (127.6, 14)]
BUB2 = [(135, 47.5), (145, 46.6), (154.2, 48.4), (155.4, 57), (153.8, 66), (145, 67.6), (135.6, 66.6), (132.4, 57)]
LENS = [(67.5, 53), (86.5, 53)]           # 눈 사이 19칸 = 개발자 안경 간격 × 158/122
CHAIN_L = {57: 57, 63: 55, 69: 53, 75: 52}
CHAIN_R = {57: 97, 63: 99, 69: 101, 75: 103}
# 후광 단계: 고리 가운데에서 본 각도(도 · 0 = 오른쪽, 반시계) 구간 — 손으로 정함 (왼 위 밝음 · 오른 아래 짙음)
HALO_TONES = [(-25, 1), (8, 2), (58, 3), (121, 4), (176, 3), (214, 2), (262, 1), (300, 0), (335, 1)]


def edge_of(m):
    return m & ~(kit.shifted(m, 1, 0) & kit.shifted(m, -1, 0) & kit.shifted(m, 0, 1) & kit.shifted(m, 0, -1))


def tilted(C, e, dy, deg=-5.0):
    cx, cy, rx, ry = e
    cy += dy
    ys, xs = np.mgrid[0:C, 0:C]
    t = np.radians(deg)
    u = (xs + 0.5 - cx) * np.cos(t) + (ys + 0.5 - cy) * np.sin(t)
    v = -(xs + 0.5 - cx) * np.sin(t) + (ys + 0.5 - cy) * np.cos(t)
    return (u / rx) ** 2 + (v / ry) ** 2 <= 1.0


def halo_paint(p, dy):
    C = p.C
    ring = tilted(C, HALO_O, dy) & ~tilted(C, HALO_I, dy)          # 5° 기울임 (아래끝 긴 가로 직선 방지)
    ys, xs = np.nonzero(ring)
    cx, cy = HALO_O[0], HALO_O[1] + dy
    ang = (np.degrees(np.arctan2(-(ys + 0.5 - cy) * 3.2, xs + 0.5 - cx)) + 360) % 360
    for (y, x, a) in zip(ys, xs, ang):
        t = 1
        for lim, tone in HALO_TONES:
            if (a if a < 335 else a - 360) >= lim:
                t = tone
        p.mat[y, x], p.tone[y, x] = "gold", t
    p.put(edge_of(ring), "ol", 0)


def glasses_paint(p, leg, g, dy, frame):
    """돋보기 안경: 금테 + 개발자 안경과 같은 하늘색 렌즈(빛나는 유리) + 반달 눈"""
    C = p.C
    ys, xs = np.mgrid[0:C, 0:C]
    for i, (cx, cy) in enumerate(LENS):
        cy += dy
        d = np.hypot(xs + 0.5 - cx, ys + 0.5 - cy)
        rim = (d <= 8.6) & (d > 6.4)
        glass = d <= 6.4
        ang = (np.degrees(np.arctan2(-(ys + 0.5 - cy), xs + 0.5 - cx)) + 360) % 360
        a0 = (70, 20, 250, 300) if i == 0 else (84, 32, 236, 290)
        tone = np.where((ang > a0[0]) & (ang < 200), 4, np.where((ang > a0[1]) & (ang < a0[2]), 3, np.where((ang > a0[3]) | (ang < a0[1] - 10), 1, 2)))
        p.mat[rim], p.tone[rim] = "gold", tone[rim]
        hi = glass & ((xs + 0.5 - cx) + (ys + 0.5 - cy) < (-1.5 if i == 0 else -3.5))
        p.mat[glass], p.tone[glass] = "glass", 1
        p.mat[hi], p.tone[hi] = "glass", 2
        p.put(edge_of(rim | glass), "ol", 0)
        p.grid(g["eyeL" if i == 0 else "eyeR"], leg, int(cx) - 4, int(cy) - 1)
    p.put((xs >= 76) & (xs <= 78) & (ys >= 50 + dy) & (ys <= 51 + dy), "gold", 3)     # 다리
    for keys in (CHAIN_L, CHAIN_R):
        cols = kit.curve_cols({y + dy: x for y, x in keys.items()}, range(57 + dy, 76 + dy))
        for j, (y, x) in enumerate(sorted(cols.items())):
            if p.mat[y, x] in ("skin", "perm", "pink"):
                p.mat[y, x], p.tone[y, x] = "gold", (3 if (j // 2) % 2 == 0 else 1)
    if frame == 2:
        p.grid(g["glint"], leg, 63, 48 + dy)


def bubble_paint(p, leg, g, pts, dy, excl, tail, tx, ty):
    C = p.C
    m = kit.shape(C, [(x, y + dy) for x, y in pts])
    p.put(m, "apron", 3)
    sh = m & ~kit.shifted(m, -3, -3)
    p.put(sh, "apron", 2)
    p.put(m & ~kit.shifted(m, -1, -2) & ~kit.shifted(m, -2, -1), "apron", 1)
    p.grid(g[tail], leg, tx, ty + dy)
    ys, xs = np.nonzero(m)
    rows = g[excl]
    p.grid(rows, leg, int((xs.min() + xs.max()) / 2 - len(rows[0]) / 2 + 0.5), int((ys.min() + ys.max()) / 2 - len(rows) / 2 + 0.5))


def hem_shift(pts, d):
    """밑단 · 프릴 출렁임 (프레임 1 · 2 에서 아래 끝 기준점 d 칸)"""
    return [(x, y + (d if y >= 139 else 0)) for x, y in pts]


def build_frame(frame):
    C = CELL
    p = kit.Pic(C)
    leg, g = detail.load_handmaps("mom")
    runs = kit.load_runs("mom")
    hd, b1, b2, sl = OFF[frame]
    hs = 1 if frame in (1, 2) else 0
    # 빗자루 (가디건 뒤 · 머리가 왼쪽 아래로 튀어나옴)
    stick = kit.poly(C, [(11, 136), (14, 137), (36, 93), (33, 92)])
    ys, xs = np.nonzero(stick)
    p.mat[ys, xs] = "wood"
    p.tone[ys, xs] = np.where(xs + ys * 0.5 < 13 + 0.5 * ys + (36 - 11) * (136 - ys) / 44, 2, 1)
    p.put(edge_of(stick), "ol", 0)
    card = kit.shape(C, hem_shift(CARD, hs))
    p.put(card, "pink", 2)
    p.runs(runs["card"], ["pink"])
    arm = kit.shape(C, ARM_L)
    p.put(arm, "pinkarm", 2)                 # 임시 재질: 팔 구간이 같은 행 가디건 칸을 칠하지 않게
    p.runs(runs["arm"], ["pinkarm"])
    p.mat[p.mat == "pinkarm"] = "pink"
    p.put(edge_of(arm) & card, "ol", 0)
    apron = kit.shape(C, hem_shift(APRON, hs))
    p.put(apron, "apron", 2)
    p.runs(runs["apron"], ["apron"])
    p.put(edge_of(apron), "ol", 0)
    p.grid(g["broom"], leg, 3, 134)
    p.grid(g["scratch"], leg, 60, 91)
    for name, x, y in BUTTONS:
        p.grid(g[name], leg, x, y + hs, clip=(p.mat == "pink"))
    p.grid(g["heart"], leg, 69, 104)
    p.grid(g["hand"], leg, 114, 96)
    p.grid(g["footL"], leg, 44, 148)
    p.grid(g["footR"], leg, 86, 148)
    perm_paint(p, PERM, hd)
    for name, x, y in CURL_HL:
        p.grid(g[name], leg, x, y + hd, clip=(p.mat == "perm"))
    p.grid(g["pin"], leg, 105, 44 + hd)
    p.grid(g["rollA"], leg, 56, 14 + hd)
    p.grid(g["rollB"], leg, 85, 12 + hd)
    p.grid(g["rollC"], leg, 117, 28 + hd)
    face = ell(C, FACE, 0, hd)
    p.put(face, "skin", 2)
    p.runs(runs["face"], ["skin"], dy=hd)
    p.put(edge_of(face), "ol", 0)
    perm_paint(p, BANGS, hd)
    glasses_paint(p, leg, g, hd, frame)
    p.grid(g["nose"], leg, 76, 59 + hd)
    p.grid(g["blushL"], leg, 58, 62 + hd)
    p.grid(g["blushR"], leg, 90, 62 + hd)
    p.grid(g["mouth"], leg, 72, 65 + hd)
    halo_paint(p, hd)
    p.grid(g["slipper"], leg, 2, 39 + sl)
    bubble_paint(p, leg, g, BUB1, b1, "exclL", "tailL", 124, 21)
    bubble_paint(p, leg, g, BUB2, b2, "exclS", "tailS", 129, 63)
    return p


def frame_rgba(frame):
    p = build_frame(frame)
    rgba, glow = kit.finish(p, PAL, warm=WARM, cool_rgb=OL_COOL, cool_down=True)
    return kit.fix_islands4(rgba), glow


def design_frames(sheet):
    """설계도 시트(128×32 · 4×1)의 4칸"""
    return bp.cells(sheet, 32)


def build(design_sheet):
    frames = [frame_rgba(i)[0] for i in range(4)]
    return dict(frames=frames, design=design_frames(design_sheet), cols=4, cell=CELL, ppu=PPU, extrude=EXTRUDE,
                sprites=None, asset=ASSET, files=FILES, scale=SCALE)
