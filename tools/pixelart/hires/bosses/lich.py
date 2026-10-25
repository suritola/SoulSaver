"""리치 왕 (1장 끝 보스) — 32칸 설계도 → 120칸 다시 그리기 (그래픽 개편 2단계 묶음 4)

배율 20 × 20.638 (BOSS.prefab 그대로) · PPU 120 = 6 × 20 → 그림 1칸 = 1080p 4.09 × 4.22 화소 (y 배율이 3% 큼 — 옛 그림과 같은 비율).
쓰는 그림 8장: 살아 있는 동안 정지 그림 boss_0 + 죽음 클립 boss_22~28 (BossDeath.anim · 8fps). 시트 120칸 4×2 (480×240).
  0 살아 있음 · 1 영혼 빠짐(흰 번쩍) · 2 무너짐(주저앉음) · 3 쓰러짐(머리 오른쪽) · 4 영혼 덩이 · 5 터짐 · 6 흩어짐 · 7 사라짐
그리는 법: 칸마다 손 도트 지도(handmaps/lich.txt)를 그대로 찍음 — 명암 계산 · 회전 복사 없음. 이 파일은 팔레트 · 자리 · 외곽만.
설계도 상자 × 3.75 를 그대로 두되 1화소 위로 올리고 지팡이 아래 끝을 1화소 줄여 칸 가장자리 여백 2칸 (옛 그림은 칸 맨 아래에 닿음).
"""
import numpy as np

from .. import blueprint as bp
from . import robed

NAME = "lich"
CELL = 120
K = CELL / 32.0                 # 3.75
SCALE = 20.0                    # BOSS.prefab localScale.x (y 20.638)
PPU = CELL
EXTRUDE = int(round(K))         # 4
SPRITES = ["boss_0"] + [f"boss_{i}" for i in range(22, 29)]
ASSET = "Assets/Sprites/enemy/boss.png"
DESIGN_CELLS = [(0, 0)] + [(c, 5) for c in range(7)]


def H(s):
    return bp.hexrgb(s)


# 재질마다 단계 색 — 어두운 쪽 차갑게 · 밝은 쪽 따뜻하게, 한 재질은 한 30° 색상 칸 안 (검사기 색상 묶음)
LIVE = {
    "robe": [H("#44267A"), H("#6038A4"), H("#764CC6"), H("#8C5CDA"), H("#AE8CF2")],
    "gold": [H("#6E4618"), H("#BA7228"), H("#EAAA3A"), H("#FEDE6C"), H("#FFF6C8")],
    "bone": [H("#6E5E6A"), H("#A08C88"), H("#CDBBA4"), H("#EADFC6"), H("#FFF8E8")],
    "eye": [H("#0A1A22"), H("#50D6EA"), H("#D8FCFF")],      # 눈구멍 = 하늘빛 눈 계열 짙은 청록 (옷 보라 묶음에 안 섞이게)
    "staff": [H("#3F2A36"), H("#5E4A52"), H("#8C8078"), H("#BDB4A2")],
    "orb": [H("#1E4267"), H("#2486B8"), H("#50D6EA"), H("#B0F8FF"), H("#FFFFFF")],
}
# 영혼 빠짐 = 옛 흰 번쩍 (설계도 몸 밝기 202) — 옷 · 뼈 · 금을 서로 다른 옅은 색상으로 (모양이 읽히게)
GHOST = {
    "robe": [H("#9C94CE"), H("#ACA6DA"), H("#BEB8E6"), H("#CCC8F0"), H("#E2DEFA")],
    "gold": [H("#A4A4B8"), H("#BCBCCC"), H("#D2D2DE"), H("#E4E4EE"), H("#F6F6FA")],      # 금은 빛 바랜 은빛 (무채 — 옷과 다른 묶음)
    "bone": [H("#B0AECA"), H("#C4C2DA"), H("#D6D4E8"), H("#E6E4F2"), H("#FAFAFF")],
    "eye": [H("#A6B4DC"), H("#E6F2FF"), H("#FFFFFF")],      # 눈구멍은 옅은 푸른 쪽 (옷 묶음과 갈라 둠)
    "staff": [H("#A29EC6"), H("#B4B0D4"), H("#C8C6E2"), H("#DCDAF0")],
    "orb": [H("#A8B4E0"), H("#C0CCEE"), H("#D4E0F8"), H("#EAF2FF"), H("#FFFFFF")],
}
# 영혼 (칸 4~7): 하늘빛 7단 — 0 은 눈구멍 · 알갱이 외곽에만 (몸 계열 짙은 청록, Y < 41)
SOUL = {"soul": [H("#0C2A3A"), H("#2A8CB8"), H("#5ACEEA"), H("#98ECF8"), H("#B8F8FF"), H("#E1FCFF"), H("#FFFFFF")]}
SOUL_EDGE = H("#0C2A3A")         # 알갱이 · 조각 외곽 = 몸 계열 짙은 색 (기본 외곽 대신)
SOUL_SIDE = H("#101C3C")         # 오른쪽을 보는 가운데 칸 (그늘)
OL_COOL = (0x16, 0x14, 0x38)    # 오른쪽 아래 그림자 쪽 외곽 (Y < 41)
GHOST_INNER = H("#20243F")       # 영혼 빠짐: 안쪽 선 — 짙은 남색 (Y < 41 · 옷 보라 묶음 밖)
WARM = ("gold",)

# 칸 0 조립: 몸판(옷 · 지팡이) → 머리 → 구슬 → 손 (손 지도 이름, 칸 x, 칸 y)
STAND = [("lich_stand", 30, 44), ("head", 41, 48), ("orb", 81, 44), ("hand", 80, 75)]
# 칸 2 무너짐: 몸판 → 가라앉은 머리 → 구슬 (지팡이 한 칸 오른쪽) → 손
SQUASH = [("lich_squash", 26, 74), ("head_sunk", 41, 74), ("orb", 84, 74), ("hand", 83, 92)]
# 칸 3 쓰러짐 (머리 오른쪽): 몸판 → 누운 머리 → 옆 구슬 → 손 — 회전 복사 없이 칸마다 손 지도
FALLEN = [("lich_fallen", 22, 51), ("head_fallen", 63, 62), ("orb_side", 79, 102), ("hand_fallen", 56, 101)]


# 칸 4 영혼 덩이
SOULBALL = [("lich_soul", 41, 66)]
# 칸 5 터짐: 조각 26개 (칸 x 30 · y 59 기준 자리 — 설계도 터짐 고리 두 줄을 따라 손으로 놓고, 오른쪽은 비워 끝 조각만 외톨이로)
F5 = [("f5_A1", 6, 0), ("f5_A2", 0, 4), ("f5_A3", 50, 0), ("f5_A4", 21, 0), ("f5_A5", 22, 6), ("f5_A6", 36, 0), ("f5_A7", 34, 5),
      ("f5_A10", 37, 11), ("f5_A11", 9, 12), ("f5_A12", 0, 17), ("f5_A14", 60, 14),
      ("f5_B1", 11, 22), ("f5_B2", 54, 32), ("f5_B3", 0, 31), ("f5_B4", 21, 25), ("f5_B5", 17, 30), ("f5_B6", 32, 26), ("f5_B7", 40, 26),
      ("f5_B9", 41, 31), ("f5_B10", 6, 35), ("f5_B11", 13, 36), ("f5_B12", 29, 35), ("f5_B14", 34, 39),
      ("f5_B16", 27, 44), ("f5_B17", 54, 45), ("f5_B18", 21, 45)]
# 위 고리 조각은 한 단계 밝게 · 아래 고리 뒤쪽은 한 단계 어둡게 (솟아오르는 넋 — 위가 밝음)
F5 = [(n, x + 30, y + 59, 1 if n.startswith("f5_A") else -1 if y >= 35 else 0) for n, x, y in F5]
# 칸 6 흩어짐: 알갱이 가운데 자리 (설계도 점 자리를 바탕으로 손으로 — 무리 다섯(별 둘레 · 오른쪽 위 별 · 왼쪽 위 · 왼쪽 아래 · 아래)에
# 촘촘히 모으고 그 사이는 비워 외톨이 여섯만 · 모양 22종 한 번씩 · 위일수록 밝게)
F6 = [("S1", 69, 66, 0), ("M13", 62, 60, 0), ("M11", 76, 60, 0), ("M18", 69, 74, 0),
      ("M6", 93, 56, 1), ("M15", 97, 52, 0), ("M22", 98, 57, 0),
      ("M9", 29, 52, 1), ("M21", 35, 56, 0), ("M2", 28, 59, 0),
      ("M23", 76, 88, 0), ("M24", 78, 94, -1), ("M7", 71, 93, 0),
      ("M8", 24, 82, 0), ("M12", 28, 87, 0), ("M17", 21, 91, -1),
      ("M3", 45, 45, 1), ("M1", 82, 44, 0), ("M16", 49, 72, 0), ("M4", 103, 74, 0), ("M14", 89, 82, 0), ("M10", 50, 92, -1)]
# 칸 7 사라짐: 더 성기고 흐리게 (위 −3 · 아래 −4) — 무리 다섯 + 외톨이 여덟 · 모양 24종 한 번씩
F7 = [("M22", 22, 44, -3), ("M15", 27, 48, -3), ("M1", 22, 51, -3),
      ("M2", 17, 61, -4), ("M20", 22, 64, -4), ("M21", 17, 68, -4),
      ("M9", 72, 49, -3), ("M12", 77, 53, -3), ("M5", 72, 56, -4),
      ("M8", 75, 85, -4), ("M18", 81, 88, -4), ("M24", 75, 91, -3), ("M13", 70, 88, -4),
      ("M11", 99, 60, -3), ("M17", 104, 64, -4), ("M6", 109, 61, -4),
      ("M3", 77, 35, -3), ("M7", 95, 40, -3), ("M10", 52, 46, -3), ("M16", 45, 72, -4), ("M19", 56, 62, -4), ("M4", 96, 79, -4),
      ("M23", 24, 84, -4), ("M14", 38, 59, -4)]


def soul_frame(placements, center=False, motes=False):
    legend, grids = robed.load(NAME)
    mat, tone = robed.paint(CELL, placements, legend, grids, center=center)
    if motes:
        return robed.motes_rgba(mat, tone, SOUL, SOUL_EDGE, SOUL_SIDE)
    return robed.to_rgba(mat, tone, SOUL, cool_rgb=SOUL_SIDE, dark_edge={"soul": SOUL_EDGE}, side_rgb=SOUL_SIDE, side_quad=True)


def frame(placements, pal):
    legend, grids = robed.load(NAME)
    mat, tone = robed.paint(CELL, placements, legend, grids)
    return robed.to_rgba(mat, tone, pal, warm=WARM, cool_rgb=OL_COOL, inner_rgb=GHOST_INNER if pal is GHOST else None)


def design_frames(sheet):
    if sheet.shape[:2] == (64, 128):
        return bp.cells(sheet, 32)
    return [sheet[r * 32:(r + 1) * 32, c * 32:(c + 1) * 32] for c, r in DESIGN_CELLS]


def build(design_sheet):
    d = design_frames(design_sheet)
    frames = [frame(STAND, LIVE), frame(STAND, GHOST), frame(SQUASH, LIVE), frame(FALLEN, LIVE),
              soul_frame(SOULBALL), soul_frame(F5), soul_frame(F6, center=True, motes=True), soul_frame(F7, center=True, motes=True)]
    return dict(frames=frames, design=d, cols=4, cell=CELL, ppu=PPU, extrude=EXTRUDE, sprites=SPRITES, asset=ASSET, scale=SCALE)
