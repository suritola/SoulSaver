"""개발자 (무한 모드 15분 최종 보스) — 32칸 설계도 → 122칸 다시 그리기 (그래픽 개편 2단계 묶음 5)

배율 15 × 1.35 = 20.25 (거울의 군주 프리팹 15 × EndlessMode 1.35) · PPU 122 = round(6 × 20.25) → 그림 1칸 = 1080p 4.08 화소.
쓰는 그림 4장: Resources/Abyss/developer_0~3 (AbyssBody 6fps · 엄마 그림이 없을 때는 엄마도 이 그림). 낱장 4장 · .meta guid 그대로.
정체성(설계도): 금 후광 · 남색 후드 · 빛나는 하늘빛 안경 · 초록 코드가 흐르는 노트북 · 금 책상 · 옆에 뜬 보라 「< >」.
그리는 법: 큰 몸(후드 · 몸통 · 책상 앞판 · 후광)의 명암 = 손 구간(handmaps/runs/dev.txt), 얼굴 · 머그 · 괄호 · 장식 = 손 도트 지도(handmaps/dev.txt),
노트북 코드 줄 = 손으로 적은 줄 목록(CODE). 칸 좌표 = 설계도 칸 × 3.8125 + (2, −1.8) (설계도 상자 31칸 → 118칸 · 여백 2칸).
프레임: 후광 0/+3/0/+3 (옛 그림처럼 1·3 에서 한 칸 내려옴) · 몸 0/+1/0/+1 숨쉬기 · 괄호 자리 = 설계도 프레임마다 · 코드 한 줄씩 흐름 · 안경 반짝 2.
"""
import numpy as np

from .. import blueprint as bp
from .. import detail
from . import abyss_kit as kit

NAME = "developer"
CELL = 122
K = CELL / 32.0                 # 3.8125
SCALE = 20.25                   # 15 × 1.35
PPU = CELL
EXTRUDE = int(round(K))         # 4
FILES = [f"Assets/Resources/Abyss/developer_{i}.png" for i in range(4)]
ASSET = FILES[0]
OLD = "Assets/Resources/Abyss/developer_{}.png"
H = bp.hexrgb

PAL = {
    "hood": [H("#1E1A3E"), H("#3C3C80"), H("#4C4E9A"), H("#5E62B4"), H("#7E88D4")],
    "rim": [H("#2A3260"), H("#323C70"), H("#3C4880")],                 # 후드 오른쪽 가장자리 · 노트북 빛 반사 (푸른 쪽)
    "skin": [H("#A65E50"), H("#D88E72"), H("#F2BC96"), H("#FFDDBC")],
    "glass": [H("#1E3E6A"), H("#50D6EA"), H("#B0F8FF"), H("#FFFFFF")],
    "lap": [H("#34344C"), H("#545A6E"), H("#828AA0"), H("#B4BCD2")],
    "scr": [H("#11301F"), H("#126E42"), H("#28BE6E"), H("#78F0A0"), H("#D8FFE6")],
    "gold": [H("#6E4618"), H("#B46826"), H("#EAAA3A"), H("#FEDE6C"), H("#FFF6C8")],
    "brk": [H("#4A2A8A"), H("#7C4CC8"), H("#A880EC"), H("#D4BCFF")],
    "book": [H("#5B3629"), H("#8A5236")],
}
OL_COOL = H("#161234")
WARM = ("gold", "skin")


def px(x):
    return x * K + 2.0


def py(y):
    return (y - 1) * K + 2.0


# ---------------------------------------------------------------- 윤곽 (손으로 놓은 기준점 · 프레임 0)
HOOD = [(46, 19.5), (54, 16.8), (64, 16.4), (72.5, 17.6), (79, 21), (84, 28), (86.4, 36), (87.2, 44), (86.4, 52), (84.6, 57.5), (81.5, 61),
        (85.5, 63.8), (89.2, 66.8), (91.6, 71), (92.8, 78), (93, 87), (93.2, 95), (91, 101.5), (32, 101.5), (30, 95), (30.2, 87), (30.6, 78),
        (32.6, 71.2), (35.4, 67), (39.4, 64), (43, 61.2), (39.8, 56.5), (37.8, 51), (37.2, 43), (38.4, 35), (40.6, 28.5), (44.8, 21.6)]
LID = [(33, 77), (86, 77), (85, 101), (34, 101)]                 # 단단한 물건: 곧은 변 (몸 안쪽 · 실루엣 아님)
SCREEN = [(38, 81), (81, 81), (80, 97.6), (39, 97.6)]
DECK = [(29, 101), (90, 101), (91.6, 104), (90, 107), (28, 107), (26.6, 104)]
DESK = [(23, 105), (40, 103.6), (60, 103.2), (80, 103.6), (97, 105), (100.5, 108.5), (100.5, 113), (97, 116.6),
        (80, 118.2), (62, 119.4), (44, 118.6), (25, 116.8), (20.5, 113), (20.5, 108.5)]
HALO_O = (60.0, 9.2, 23.0, 6.9)       # 가운데 x, y · 반지름 x, y
HALO_I = (60.6, 10.0, 13.4, 2.1)


def body_masks(C, dy=0):
    hood = kit.shape(C, [(x, y + dy) for x, y in HOOD])
    return hood


def halo_mask(C, dy=0):
    cx, cy, rx, ry = HALO_O
    o = kit.ellipse(C, cx, cy + dy, rx, ry)
    cx, cy, rx, ry = HALO_I
    i = kit.ellipse(C, cx, cy + dy, rx, ry)
    return o & ~i


# 노트북 코드 줄 (손으로 적음): 단계:길이 · _n = n칸 띄움. 한 줄 1칸 높이 · 줄 사이 1칸 · 프레임마다 한 줄씩 위로 흐름
CODE = [
    "3:5 _1 2:2 _2 2:17", "_2 2:2 _1 3:11", "_2 1:14", "_4 3:3 _1 2:2 _1 2:7", "_4 2:2 4:2 _1 2:12", "_2 3:6 _1 4:2",
    "2:4 _1 3:18 _1 2:2", "_2 1:16", "_2 2:2 _1 3:19 _1 4:2", "_4 2:13", "_2 3:2 _1 2:15", "4:2 _1 2:2 _1 3:2 _1 2:2",
]                                   # 3칸 넘는 낱말 길이는 한 번씩만 · 책 막대(8~10)와도 안 겹침 (같은 막대 모양 반복 방지)
# 괄호 자리 (왼쪽 위 칸): 큰 「<」 · 큰 「>」 · 작은 「<」 · 작은 「>」 — 설계도 프레임마다 옮긴 자리 (여백 2칸 안)
BRACKETS = [
    ((9, 32), (102, 32), (2, 56), (111, 56)),
    ((13, 33), (98, 33), (2, 58), (111, 54)),
    ((9, 31), (102, 31), (3, 55), (111, 57)),
    ((6, 32), (105, 32), (5, 56), (110, 56)),
]


def lid_tones(p, lid, screen):
    """테두리: 위 3 · 왼 2 · 오른 1 · 아래 1 (각 변 한 단계 — 테두리 안 곧은 단계 경계를 만들지 않음) · 노트북 외곽 = 안쪽 선"""
    ys, xs = np.nonzero(lid)
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    sx, sy = np.nonzero(screen)[1], np.nonzero(screen)[0]
    for y, x in zip(ys, xs):
        d = {3: y - y0, 2: x - x0, 1: x1 - x, 0: y1 - y}
        side = min(d, key=d.get)
        p.mat[y, x] = "lap"
        p.tone[y, x] = {3: 3, 2: 2, 1: 1, 0: 1}[side]
    edge = lid & ~(kit.shifted(lid, 1, 0) & kit.shifted(lid, -1, 0) & kit.shifted(lid, 0, 1) & kit.shifted(lid, 0, -1))
    p.put(edge, "ol", 0)


def code_lines(p, screen, frame):
    ys, xs = np.nonzero(screen)
    x0, y0, y1 = xs.min() + 2, ys.min() + 2, ys.max() - 1
    for i, y in enumerate(range(y0, y1, 2)):
        line = CODE[(i + frame) % len(CODE)]
        x = x0
        for tok in line.split():
            if tok.startswith("_"):
                x += int(tok[1:])
                continue
            t, n = map(int, tok.split(":"))
            for c in range(x, x + n):
                if screen[y, c] and screen[y, c + 1 if c + 1 < p.C else c]:
                    p.mat[y, c], p.tone[y, c] = "scr", t
            x += n
    if frame % 2 == 0:                      # 깜빡이는 커서 (프레임 0 · 2)
        y = y0 + 2 * ((y1 - y0) // 2)
        p.mat[y, x0 + 18:x0 + 21], p.tone[y, x0 + 18:x0 + 21] = "scr", 4


def build_frame(frame):
    C = CELL
    p = kit.Pic(C)
    leg, g = detail.load_handmaps("dev")
    runs = kit.load_runs("dev")
    hb = (0, 3, 0, 3)[frame]
    bb = (0, 1, 0, 1)[frame]
    # 몸 (후드 · 몸통) → 손 구간 명암 → 얼굴 → 끈
    hood = body_masks(C, bb)
    p.put(hood, "hood", 2)
    p.runs(runs["hood"], ["hood"], dy=bb)
    p.grid(g["face"], leg, 48, 22 + bb)
    p.grid(g["foldA"], leg, 38, 64 + bb, clip=hood)
    p.grid(g["foldB"], leg, 41, 40 + bb, clip=hood)
    p.grid(g["foldC"], leg, 44, 52 + bb, clip=hood)
    p.grid(g["cordL"], leg, 54, 63 + bb, clip=hood)
    p.grid(g["cordR"], leg, 66, 63 + bb, clip=hood)
    if frame == 2:
        p.grid(g["glint"], leg, 53, 42 + bb)
    # 노트북 뚜껑 · 화면 · 코드
    lid = kit.poly(C, LID)
    scr = kit.poly(C, SCREEN)
    lid_tones(p, lid, scr)
    p.put(scr, "scr", 0)
    code_lines(p, scr, frame)
    # 책상 → 받침 → 머그 · 책 · 보석
    desk = kit.shape(C, DESK)
    p.put(desk, "gold", 2)
    p.runs(runs["desk"], ["gold"])
    deck = kit.poly(C, DECK)
    p.put(deck, "lap", 2)
    p.runs(runs["deck"], ["lap"])
    dedge = deck & ~(kit.shifted(deck, 0, 1) & kit.shifted(deck, 0, -1) & kit.shifted(deck, 1, 0) & kit.shifted(deck, -1, 0))
    p.put(dedge, "ol", 0)
    p.grid(g["mug"], leg, 79, 84)
    p.grid(g["books"], leg, 16, 97)
    p.grid(g["gem"], leg, 55, 110)
    p.grid(g["circL"], leg, 24, 110)
    p.grid(g["circR"], leg, 66, 110)
    # 후광
    halo = halo_mask(C, hb)
    p.put(halo, "gold", 2)
    p.runs(runs["halo"], ["gold"], dy=hb)
    hedge = halo & ~(kit.shifted(halo, 1, 0) & kit.shifted(halo, -1, 0) & kit.shifted(halo, 0, 1) & kit.shifted(halo, 0, -1))
    p.put(hedge, "ol", 0)                   # 후광 테 외곽 (머리 위에 겹쳐도 떠 보이게)
    # 괄호
    (a, b, c, d) = BRACKETS[frame]
    p.grid(g["brk_big"], leg, *a)
    p.grid(g["brk_big"], leg, *b, flip=True)
    p.grid(g["brk_small"], leg, *c)
    p.grid(g["brk_small"], leg, *d, flip=True)
    return p


def frame_rgba(frame):
    p = build_frame(frame)
    rgba, glow = kit.finish(p, PAL, warm=WARM, cool_rgb=OL_COOL)
    return rgba, glow


def design_frames(sheet):
    """설계도 시트(128×32 · 4×1)의 4칸"""
    return bp.cells(sheet, 32)


def build(design_sheet):
    frames = [frame_rgba(i)[0] for i in range(4)]
    return dict(frames=frames, design=design_frames(design_sheet), cols=4, cell=CELL, ppu=PPU, extrude=EXTRUDE,
                sprites=None, asset=ASSET, files=FILES, scale=SCALE)
