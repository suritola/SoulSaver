# 스킨 메뉴 초상 128칸 — rogue__raven (색 대응: 기본 rogue 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
#   단계 색은 손으로 정함 (32칸 대응 색을 닻으로 · 단계 수 · 명도 순서 유지 · 옷 최고 단계는 얼굴 밝은 살 근처 아래 — 흑백 1위는 얼굴 · 눈)
from .skin import Variant

V = Variant('rogue', 'rogue__raven', auto=False, ramps={
    # 까마귀: 보라 두건 → 검은 깃 (32칸 18121F · 282434 · 3D364A) · 망토 → 더 짙은 검정 (221E2C · 림 475E70 · 51697C)
    'hood':  ['141020', '2C2838', '363246', '4A4460', '68627E'],     # 실루엣 가까이 쓰는 단계는 Y ≥ 41 (lint 4 외곽 두께)
    'cloak': ['120E1A', '2C2838', '34303F', '46566A', '667A8E'],
}, glob={'EYE': 'E8E8F0', 'EYE_HOT': 'FFFFFF'})     # 노란 눈 → 흰 눈 (32칸 FFFFFF)
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
