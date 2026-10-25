"""지옥의 군주 (2장 끝 보스) — 32칸 설계도 → 144칸 다시 그리기 (그래픽 개편 2단계 묶음 4)

배율 24 × 24.766 (Prefabs/Hell/DemonLord.prefab 그대로) · PPU 144 = 6 × 24 → 그림 1칸 = 1080p 4.09 × 4.22 화소.
쓰는 그림 8장: 정지 그림 hell_lord_0 + 죽음 클립 hell_lord_22~28 (DemonLordDeath.anim · 8fps). 시트 144칸 4×2 (576×288).
  0 살아 있음 · 1 영혼 빠짐(흰 번쩍) · 2 무너짐 · 3 쓰러짐(머리 오른쪽) · 4 불 넋 덩이 · 5 터짐 · 6 흩어짐 · 7 사라짐
그리는 법: 칸마다 손 도트 지도(handmaps/helllord.txt)를 그대로 찍음 — 명암 계산 · 회전 복사 없음.
리치와 같은 자세 계열이지만 각진 계단 실루엣 · 가운데 금 줄 · 불꽃 밑단 · 불꽃 왕관과 뿔로 갈라 놓음.
설계도 상자 × 4.5 를 그대로 두되 2화소 위로 올리고 지팡이 끝을 줄여 칸 가장자리 여백 2칸.
"""
import numpy as np

from .. import blueprint as bp
from . import robed

NAME = "helllord"
CELL = 144
K = CELL / 32.0                 # 4.5
SCALE = 24.0                    # DemonLord.prefab localScale.x (y 24.766)
PPU = CELL
EXTRUDE = int(round(K))         # 4 (round(4.5) = 4)
SPRITES = ["hell_lord_0"] + [f"hell_lord_{i}" for i in range(22, 29)]
ASSET = "Assets/Sprites/enemy/hell/hell_lord.png"
DESIGN_CELLS = [(0, 0)] + [(c, 5) for c in range(7)]


def H(s):
    return bp.hexrgb(s)


LIVE = {
    "robe": [H("#2E163E"), H("#44205C"), H("#5A2C78"), H("#763892"), H("#9450B0")],
    "gold": [H("#6A3A14"), H("#B4621E"), H("#EAAA3A"), H("#FFC850"), H("#FFF0B0")],
    "skin": [H("#5E1C16"), H("#96281E"), H("#D43C2A"), H("#F4704E"), H("#FFB08C")],
    "horn": [H("#3E2A22"), H("#6E503C"), H("#AA825A"), H("#E2C496")],
    "fire": [H("#6E1A10"), H("#D24618"), H("#FA8C28"), H("#FFC850"), H("#FFF8C8")],
    "eye": [H("#2A0A12"), H("#FFE678"), H("#FFFFFF")],
    "staff": [H("#3A3436"), H("#5A5254"), H("#857C7A"), H("#B0A89E")],      # 쇠 (무채 — 옷 · 살 묶음과 갈라 둠)
}
GHOST = {
    "robe": [H("#948CC4"), H("#A49ED2"), H("#B6B0E0"), H("#C6C2EC"), H("#DCD8F8")],
    "gold": [H("#A4A4B8"), H("#BCBCCC"), H("#D2D2DE"), H("#E4E4EE"), H("#F6F6FA")],
    "skin": [H("#B4ACC8"), H("#C4BED6"), H("#D6D2E4"), H("#E6E4F0"), H("#FAFAFF")],
    "horn": [H("#ACA8C4"), H("#C0BCD4"), H("#D4D2E4"), H("#ECEAF4")],
    "fire": [H("#B8B6CE"), H("#CCCADE"), H("#DEDCEC"), H("#EEEEF8"), H("#FFFFFF")],
    "eye": [H("#A6B4DC"), H("#E6F2FF"), H("#FFFFFF")],
    "staff": [H("#A29EC6"), H("#B4B0D4"), H("#C8C6E2"), H("#DCDAF0")],
}
# 불 넋 (칸 4~7): 잉걸 7단 — 0 은 눈 · 알갱이 외곽에만 (몸 계열 짙은 잉걸색, Y < 41)
SOUL = {"soul": [H("#3A1008"), H("#B03814"), H("#F06A28"), H("#FFA640"), H("#FFC850"), H("#FFF0A0"), H("#FFFFFF")]}
SOUL_EDGE = H("#3A1008")
SOUL_SIDE = H("#2A0C1C")
OL_COOL = (0x1E, 0x10, 0x2C)       # 차가운 외곽 — 지옥은 자동 규칙을 끄고 손 지도의 & 칸만 (긴 외곽 한 줄을 몇 군데 끊음)
GHOST_INNER = H("#20243F")       # 영혼 빠짐 안쪽 선 — 짙은 남색 (Y < 41)
WARM = ("gold",)

STAND = [("hell_stand", 36, 56), ("heart", 83, 91), ("hhand", 96, 89)]
# 칸 2 무너짐: 몸판 → 불씨 심장 → 손 (지팡이는 몸판에)
SQUASH = [("hell_squash", 31, 88), ("heart", 79, 112), ("hhand", 101, 106)]
# 칸 3 쓰러짐 (머리 오른쪽 · 회전 복사 없음): 몸판(얼굴 · 불씨 심장 · 지팡이 포함) → 손
FALLEN = [("hell_fallen", 31, 61), ("hhand_fallen", 71, 120)]


# 칸 4 불 넋 덩이
SOULBALL = [("hell_soul", 49, 79)]
# 칸 5 터짐: 불씨 26개 — 리치와 다른 배치. 3~4개씩 뭉친 무리 여섯(위 왼쪽 · 위 오른쪽 · 아래 왼쪽 · 아래 오른쪽 · 가운데 · 오른쪽)
# + 멀리 떨어진 외톨이 일곱 · 좌우 균형 · 가운데 자리
F5 = [("E8", 44, 76, 0), ("E11", 49, 80, 0), ("E24", 55, 76, 0), ("E12", 49, 72, 0),
      ("E18", 85, 72, 0), ("E6", 89, 76, 0), ("E14", 93, 72, 0),
      ("E7", 44, 113, 0), ("E16", 48, 119, 0), ("E4", 52, 113, 0),
      ("E3", 88, 116, 0), ("E21", 92, 120, -1), ("E22", 96, 116, 0),
      ("E2", 68, 95, 0), ("E13", 72, 99, 0), ("E15", 78, 96, 0), ("E23", 72, 91, 0),
      ("ES", 105, 89, 0), ("E5", 109, 93, 0),
      ("E17", 38, 94, 0), ("E9", 64, 72, 0), ("E10", 70, 124, -1), ("E19", 58, 106, 0), ("E25", 99, 104, 0), ("E20", 80, 82, 0), ("E1", 88, 92, 0)]
# 칸 6 흩어짐: 30조각 — 무리 여섯(위 오른쪽 · 가운데 왼쪽 · 아래 가운데 · 왼쪽 아래 · 오른쪽 가운데 · 위 왼쪽) + 외톨이 열하나 · 모양 30종 한 번씩
F6 = [("E16", 100, 58, 1), ("E1", 106, 55, 1), ("E10", 104, 62, 1), ("E8", 110, 60, 1),
      ("E13", 48, 78, 0), ("E5", 54, 75, 0), ("E2", 52, 82, 0),
      ("E21", 69, 105, -1), ("E22", 77, 103, 0), ("E3", 72, 112, -1), ("E26", 80, 110, -1),
      ("E12", 30, 96, 0), ("E15", 26, 101, 0), ("E11", 33, 103, -1),
      ("E27", 95, 86, 0), ("E28", 100, 90, 0), ("E29", 94, 93, 0),
      ("E9", 39, 55, 1), ("E14", 46, 60, 1),
      ("ES", 24, 66, 1), ("E7", 124, 78, 0), ("E4", 66, 54, 1), ("E24", 62, 90, 0), ("E6", 84, 70, 1), ("E18", 118, 104, -1), ("E17", 44, 112, -1),
      ("E19", 86, 113, -1), ("E20", 58, 64, 1), ("E23", 90, 100, 0), ("E25", 114, 82, 0)]
# 칸 7 사라짐: 24조각 — 무리 넷 + 외톨이 열하나 (리치와 다른 자리) · 흐리게 (위 −3 · 아래 −4)
F7 = [("E1", 30, 46, -3), ("E8", 36, 44, -3), ("E17", 33, 51, -3),
      ("E4", 112, 74, -3), ("E22", 118, 70, -4), ("E12", 116, 78, -3), ("E13", 122, 76, -4),
      ("E21", 40, 100, -3), ("E2", 46, 104, -4), ("E5", 42, 108, -3),
      ("E9", 78, 66, -4), ("E18", 84, 64, -3), ("E11", 80, 71, -4),
      ("E3", 20, 70, -3), ("E6", 133, 62, -4), ("E14", 90, 41, -3), ("E16", 60, 48, -3), ("E10", 66, 86, -3), ("E15", 98, 96, -4),
      ("E20", 124, 104, -3), ("E24", 80, 108, -4), ("E23", 56, 72, -3), ("E7", 104, 52, -3), ("E19", 30, 84, -3)]


def soul_frame(placements, center=False, motes=False, x_frac=0.5, quad=False):
    legend, grids = robed.load(NAME)
    mat, tone = robed.paint(CELL, placements, legend, grids, center=center)
    if motes:
        return robed.motes_rgba(mat, tone, SOUL, SOUL_EDGE, SOUL_SIDE, top_frac=0.0, x_frac=x_frac)
    return robed.to_rgba(mat, tone, SOUL, cool_rgb=SOUL_SIDE, dark_edge={"soul": SOUL_EDGE}, side_rgb=SOUL_SIDE, side_quad=quad)


def frame(placements, pal):
    legend, grids = robed.load(NAME)
    mat, tone = robed.paint(CELL, placements, legend, grids)
    return robed.to_rgba(mat, tone, pal, warm=WARM, cool_rgb=OL_COOL, cool=False, inner_rgb=GHOST_INNER if pal is GHOST else None, warm_any=("fire",))


def design_frames(sheet):
    if sheet.shape[:2] == (64, 128):
        return bp.cells(sheet, 32)
    return [sheet[r * 32:(r + 1) * 32, c * 32:(c + 1) * 32] for c, r in DESIGN_CELLS]


def build(design_sheet):
    d = design_frames(design_sheet)
    frames = [frame(STAND, LIVE), frame(STAND, GHOST), frame(SQUASH, LIVE), frame(FALLEN, LIVE),
              soul_frame(SOULBALL, quad=True), soul_frame(F5, center=True), soul_frame(F6, center=True, motes=True, x_frac=0.2), soul_frame(F7, center=True, motes=True, x_frac=0.2)]
    return dict(frames=frames, design=d, cols=4, cell=CELL, ppu=PPU, extrude=EXTRUDE, sprites=SPRITES, asset=ASSET, scale=SCALE)
