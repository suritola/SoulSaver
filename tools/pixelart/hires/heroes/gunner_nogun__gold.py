# 스킨 메뉴 초상 128칸 — gunner_nogun__gold (색 대응: 기본 gunner_nogun 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
#   단계 색은 손으로 정함 (32칸 대응 색을 닻으로 · 단계 수 · 명도 순서 유지 · 옷 최고 단계는 얼굴 밝은 살 근처 아래 — 흑백 1위는 얼굴 · 눈)
from .skin import Variant

V = Variant('gunner_nogun', 'gunner_nogun__gold', auto=False, ramps={
    # 황금: 모자 → 왕청 (32칸 1A2260 · 2A3E8C · 4C6CC4) · 띠 → 흰 금 (F2F2E8 — 얼굴 아래로 Y 약 200)
    'hat':   ['141A48', '1E2A6A', '2A3E8C', '3C58B0', '6A88D0'],
    'gold':  ['7C7C7A', 'B0B0AC', 'C2C2BE'],
    # 목도리 → 옅은 금 (B2955A · FFEA9A — 바탕 Y 약 180) · 외투 → 금빛 갈색 (6C2E2C · 98662A · B46826 · EAAA3A · FEDE6C)
    #   금 외투가 얼굴(Y 200)보다 밝지 않게 바탕 Y 약 113 · 최고 단계 Y 약 180
    'scarf': ['5E4A1C', '9E8248', 'CCB05C', 'DCC274'],
    'coat':  ['2D192A', '7A4A1C', 'A8742C', 'D09A38', 'E6BC50'],
    'pants': ['1A1842', '2E2A58', '4C6290'],
    'boot':  ['3A1A1E', '5A2826', '6C3A2C', '8A5A3A'],
})
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
