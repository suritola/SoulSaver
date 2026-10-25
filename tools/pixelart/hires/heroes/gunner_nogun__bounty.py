# 스킨 메뉴 초상 128칸 — gunner_nogun__bounty (색 대응: 기본 gunner_nogun 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
#   단계 색은 손으로 정함 (32칸 대응 색을 닻으로 · 단계 수 · 명도 순서 유지 · 옷 최고 단계는 얼굴 밝은 살 근처 아래 — 흑백 1위는 얼굴 · 눈)
from .skin import Variant

V = Variant('gunner_nogun', 'gunner_nogun__bounty', auto=False, ramps={
    # 현상금 사냥꾼: 모자 → 검붉은 검정 (32칸 1C0C19 · 281622) · 띠 → 붉은 (961640 · E23654)
    'hat':   ['140A12', '201220', '2E1828', '46283A', '6A4458'],
    'gold':  ['6A1428', 'A82034', 'E23654'],
    # 목도리 → 붉은 (841434 · C4283E · EE5C56) · 외투 → 짙은 자주 검정 (180B16 · 24141F · 4C1E28 · 4A343E)
    'scarf': ['3A0A1E', '841434', 'C4283E', 'EE5C56'],
    'coat':  ['2D192A', '24141F', '3A1C28', '4C2A36', '6A4450'],
    'pants': ['201824', '362832', '4E5A6A'],
    'boot':  ['1C0C18', '2C1424', '40222E', '5A3A44'],
})
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
