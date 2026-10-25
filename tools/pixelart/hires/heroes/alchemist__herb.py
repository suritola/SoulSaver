# 스킨 메뉴 초상 128칸 — alchemist__herb (색 대응: 기본 alchemist 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
#   단계 색은 손으로 정함 (32칸 대응 색을 닻으로 · 단계 수 · 명도 순서 유지 · 옷 최고 단계는 얼굴 밝은 살 근처 아래 — 흑백 1위는 얼굴 · 눈)
from .skin import Variant

V = Variant('alchemist', 'alchemist__herb', auto=False, ramps={
    # 약초: 보라 겉옷 → 풀빛 (32칸 1F332F · 213E30 · 266233 · 468C4C · 69B567)
    'robe':   ['182926', '235E35', '478648', '65A65B', '8ECA84'],
    # 물약 → 주황 꿀빛 (F8A030 · FFD66E)
    'potion': ['986828', 'F8A030', 'FDE4A0'],
})
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
