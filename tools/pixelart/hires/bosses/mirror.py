"""거울의 군주 (4장 심연 보스) — 32칸 설계도 → 90칸 다시 그리기 (그래픽 개편 2단계 묶음 5)

배율 15 (AbyssStage.MakeBoss) · PPU 90 = 6 × 15 → 그림 1칸 = 1080p 4.09 화소.
쓰는 그림 4장: Resources/Abyss/mirrorlord_0~3 (AbyssBody 5fps · 거울 분신이 같은 그림 · 배율을 복사). 낱장 4장 · .meta guid 그대로.
정체성(설계도): 떠 있는 거울 머리(눈 하나) · 손잡이 목 · 보라 원뿔 옷 + 가운데 흰 띠 · 짙은 받침대 · 둘레에 반짝이 · 옆에 거울 조각.
약점(고정 틀 비율 · WeakSpot.cs): (0.35, 0.72) 거울 얼굴 왼쪽 = 금 간 조각 · (0.62, 0.5) 목 오른쪽 = 떠 있는 거울 조각 · (0.42, 0.28) 옷자락 = 꿰맨 거울 조각.
그리는 법: 거울 유리 · 테 · 옷 · 받침대 명암 = 손 구간(handmaps/runs/mirror.txt), 눈 · 손잡이 · 조각 · 반짝이 = 손 도트 지도(handmaps/mirror.txt).
칸 좌표 = 설계도 칸 × 2.8125 + (0, −3) (옛 그림은 칸 맨 아래에 닿음 → 3칸 올려 여백 2칸 이상).
프레임: 거울 머리 0/+3/0/−3 (설계도처럼 들썩임) · 반짝이 · 조각 자리 = 설계도 프레임마다 (모양은 프레임마다 다른 손 지도).
"""
import numpy as np

from .. import blueprint as bp
from .. import detail
from . import abyss_kit as kit

NAME = "mirror"
CELL = 90
K = CELL / 32.0                 # 2.8125
SCALE = 15.0
PPU = CELL
EXTRUDE = int(round(K))         # 3
FILES = [f"Assets/Resources/Abyss/mirrorlord_{i}.png" for i in range(4)]
ASSET = FILES[0]
OLD = "Assets/Resources/Abyss/mirrorlord_{}.png"
H = bp.hexrgb

PAL = {
    "glass": [H("#9AA2D2"), H("#C4CAE8"), H("#DCE2F6"), H("#EEF2FC"), H("#FCFEFF")],
    "frame": [H("#4A4870"), H("#7876A6"), H("#A6A8D0"), H("#D2D6F0")],
    "eye": [H("#120B20"), H("#4C4C8A"), H("#7C80AA"), H("#A8B0D8"), H("#E1FCFF")],
    "robe": [H("#2E1A5A"), H("#4A2C8C"), H("#6038A8"), H("#6E46BA"), H("#8C64D6")],
    "sash": [H("#9670DC"), H("#CCB4FA"), H("#F0E8FF")],
    "rim": [H("#666EB0"), H("#8088D0")],
    "base": [H("#20183A"), H("#362E64"), H("#4E4686"), H("#5E78A0")],
    "shard": [H("#5E78A0"), H("#89A6CA"), H("#C4D8F0"), H("#FFFFFF")],
}
OL_COOL = H("#16123A")

HEAD = [(43.6, 13.6), (48.4, 16.6), (53.2, 21), (57.4, 26), (59.2, 31.4), (58.6, 37), (56, 41.6), (51.6, 45.4), (49, 47),
        (43.6, 48.2), (38.4, 47), (35.6, 45.4), (31.2, 41.6), (28.6, 37), (28, 31.4), (29.8, 26), (34, 21), (38.8, 16.6)]
ROBE = [(35, 50.4), (52.4, 50.4), (56.4, 52.4), (59.8, 55.8), (62.8, 60.2), (65.6, 65.4), (68.2, 71), (70.4, 76.5), (71.4, 81),
        (44, 82), (15.8, 81), (16.8, 76.5), (18.8, 71), (21.4, 65.4), (24.6, 60), (28, 55.6), (31.4, 52.4)]
BASE = [(16, 79.4), (30, 78.4), (44, 78.1), (58, 78.4), (71.8, 79.4), (74.2, 82.2), (72.6, 85), (58, 86.2), (44, 86.5),
        (30, 86.2), (15.2, 85), (13.4, 82.2)]
HEAD_DY = (0, 3, 0, -3)


def head_masks(C, dy=0):
    head = kit.shape(C, [(x, y + dy) for x, y in HEAD])
    glass = kit.shape(C, [(43.6 + (x - 43.6) * 0.80, 31.2 + (y - 31.2) * 0.80 + dy) for x, y in HEAD])
    return head, glass


# 프레임마다 반짝이 · 조각 자리 (반짝이 = 가운데 자리 · 조각 = 왼쪽 위 칸) — 설계도 프레임의 자리를 옮김, 상자 = 설계도 × k
STARS = [
    [("starL", 71, 11), ("starM", 8, 29), ("starN", 82, 40)],
    [("starM", 8, 22), ("starS", 76, 25), ("starN", 82, 36)],
    [("starL", 18, 14), ("starN", 80, 24), ("starS", 83, 37), ("starM", 8, 54)],
    [("starL", 74, 11), ("starM", 10, 22), ("starN", 82, 30)],
]
SHARDS = [
    [("shardC", 69, 58), ("shardA", 13, 57), ("shardB", 17, 68)],
    [("shardC", 69, 61), ("shardA", 13, 60), ("shardB", 16, 70)],
    [("shardC", 69, 58), ("shardA", 13, 59), ("shardB", 17, 69)],
    [("shardC", 69, 55), ("shardA", 11, 55), ("shardB", 16, 66)],
]
FLOAT_BOB = (0, 0, 0, -1)
HEAD_ZONE = np.zeros((CELL, CELL), bool)
HEAD_ZONE[:49] = True                # 부스러기 정리는 머리 · 목 위쪽만 (옷 흰 띠 · 주름은 손 구간 그대로)


def build_frame(frame):
    C = CELL
    p = kit.Pic(C)
    leg, g = detail.load_handmaps("mirror")
    runs = kit.load_runs("mirror")
    dy = HEAD_DY[frame]
    base = kit.shape(C, BASE)
    p.put(base, "base", 1)
    p.runs(runs["base"], ["base"])
    robe = kit.shape(C, ROBE)
    p.put(robe, "robe", 2)
    p.runs(runs["robe"], ["robe"])
    p.grid(g["shardS"], leg, 34, 61)
    p.grid(g["sq1"], leg, 24, 72)
    p.grid(g["sq4"], leg, 22, 64)
    p.grid(g["sq5"], leg, 64, 70)
    p.grid(g["sq2"], leg, 62, 74)
    p.grid(g["sq3"], leg, 61, 64)
    p.grid(g["glintA"], leg, 20, 82)
    p.grid(g["glintB"], leg, 62, 83)
    head, glass = head_masks(C, dy)
    p.put(head, "frame", 2)
    p.runs(runs["frame"], ["frame"], dy=dy)
    p.put(glass, "glass", 2)
    p.runs(runs["glass"], ["glass"], dy=dy)
    p.grid(g["eye"], leg, 39, 23 + dy)
    p.grid(g["refl"], leg, 32, 21 + dy, clip=glass)
    p.grid(g["studT"], leg, 42, 14 + dy, clip=head)
    p.grid(g["studL"], leg, 28, 30 + dy, clip=head)
    p.grid(g["studR"], leg, 56, 33 + dy, clip=head)
    p.grid(g["refl2"], leg, 37, 31 + dy, clip=glass)
    p.grid(g["crack"], leg, 28, 21 + dy)
    p.grid(g["neck"], leg, 38, 42)                      # 목: 머리 들썩임과 무관하게 y 42~51 (머리 · 옷이 늘 이어짐)
    p.grid(g["shardF"], leg, 56, 43 + FLOAT_BOB[frame])
    for name, x, y in SHARDS[frame]:
        p.grid(g[name], leg, x, y)
    for name, cx, cy in STARS[frame]:
        rows = g[name]
        p.grid(rows, leg, cx - len(rows[0]) // 2, cy - len(rows) // 2)
    return p


def frame_rgba(frame):
    p = build_frame(frame)
    rgba, glow = kit.finish(p, PAL, cool_rgb=OL_COOL, cool_down=True)
    return kit.fix_small(kit.fix_islands4(rgba), 2, region=HEAD_ZONE), glow          # 판정관 기준(4-이웃) 외톨이 · 2칸 부스러기 정리


def design_frames(sheet):
    """설계도 시트(128×32 · 4×1)의 4칸"""
    return bp.cells(sheet, 32)


def build(design_sheet):
    frames = [frame_rgba(i)[0] for i in range(4)]
    return dict(frames=frames, design=design_frames(design_sheet), cols=4, cell=CELL, ppu=PPU, extrude=EXTRUDE,
                sprites=None, asset=ASSET, files=FILES, scale=SCALE)
