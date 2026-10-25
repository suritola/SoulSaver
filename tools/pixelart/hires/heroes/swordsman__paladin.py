# 스킨 메뉴 초상 128칸 — swordsman__paladin (색 대응: 기본 swordsman 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
#   단계 색은 손으로 정함 (32칸 대응 색을 닻으로 · 단계 수 · 명도 순서 유지 · 옷 최고 단계는 얼굴 밝은 살 근처 아래 — 흑백 1위는 얼굴 · 눈)
from .skin import Variant

V = Variant('swordsman', 'swordsman__paladin', auto=False, ramps={
    # 성기사: 파란 갑옷 → 흰 판금 (32칸 9E968D · E2DED6 · FFFCF3) — 흰 갑옷이 얼굴(Y 199)보다 밝지 않게 바탕 Y 약 165 · 최고 단계 Y 약 200
    'armor': ['5E587A', '908AA0', 'BEB8AC', 'C2BCB0', 'C6C0B4'],     # 판정 2회차: 최고 단계 Y 192
    'cloth': ['2A2E7A', '34449E', '6C8CC0', '8EA8D0'],
    # 투구 · 어깨받이 · 투구 테: 흰 은 (32칸 B0B6CE · E2E6F4) — 최고 단계 Y 약 214 는 투구 위 테 1줄만
    'steel': ['4C5074', '7C84A4', 'AAB0C8', 'B6BCD0', 'BCC2D6'],     # 판정 2회차: 최고 단계 Y 195
    # 망토: 붉은 천 → 파란 천 (32칸 1C1A5C · 222565 · 283694 · 3E5ECA · 6A96F2)
    'red':   ['2A2E7A', '34449E', '3E5ECA', '5A80DC', '86AEF4'],
}, parts={
    # 깃털 장식만 금 (32칸 6E3503 · F0C448 · FFE065 — 최고 단계 Y 약 200)
    'plume': ['7A4018', 'B46826', 'D49A34', 'E2A83C', 'E8BE52'],     # 판정 2회차: 최고 단계 Y 189
})
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
