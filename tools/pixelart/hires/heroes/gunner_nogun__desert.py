# 스킨 메뉴 초상 128칸 — gunner_nogun__desert (색 대응: 기본 gunner_nogun 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
#   단계 색은 손으로 정함 (32칸 대응 색을 닻으로 · 단계 수 · 명도 순서 유지 · 옷 최고 단계는 얼굴 밝은 살 근처 아래 — 흑백 1위는 얼굴 · 눈)
from .skin import Variant

V = Variant('gunner_nogun', 'gunner_nogun__desert', auto=False, ramps={
    # 사막: 모자 → 카키 갈색 (32칸 4E301D · 705436 · 937A51) · 띠 → 바랜 금 (895B2B · C49C54)
    'hat':   ['3A261C', '5E3E28', '80583C', 'AA7E5C', 'CCA67E'],
    'gold':  ['6A4A22', '9C7A3E', 'C49C54'],
    # 목도리 → 모래 (9E7A53 · E2C48C — 바탕 Y 약 175, 얼굴 아래) · 외투 → 흙 갈색 (61361D · 785030 · D2AE6E · FFE597)
    'scarf': ['5E4A30', '9E7A53', 'CCB07C', 'D8BE84'],
    'coat':  ['2D192A', '61361D', '785030', 'A87C48', 'D0AC70'],
    'pants': ['2A2026', '4A3C34', '6C7070'],
})
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
