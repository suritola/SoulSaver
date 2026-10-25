# 스킨 메뉴 초상 128칸 — swordsman__black (색 대응: 기본 swordsman 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
#   단계 색은 손으로 정함 (32칸 대응 색을 닻으로 · 단계 수 · 명도 순서 유지 · 옷 최고 단계는 얼굴 밝은 살 근처 아래 — 흑백 1위는 얼굴 · 눈)
from .skin import Variant

V = Variant('swordsman', 'swordsman__black', auto=False, ramps={
    # 흑기사: 파란 갑옷 → 검정 (32칸 1B1429 · 282438 · 3E364F) · 투구 · 어깨받이 → 짙은 쇠 (403A56 · 5D5373)
    'armor': ['100C1A', '261E38', '342E48', '443C58', '544C68'],
    'steel': ['1E1A2C', '403C5A', '4C4664', '5A5270', '665E7E'],     # 그늘 단계를 올려 투구 좌우 대비를 줄임 (오른쪽만 밝으면 왼 광원 항목이 기울어짐)
    # 금 장식 → 핏빛 붉은 테 (32칸 770E24 · AA2C36) · 깃털 · 망토: 더 짙은 붉은 천 (280A22 · C4283E · EE5C56)
    'gold':  ['480A1E', '770E24', 'AA2C36'],
    'red':   ['280A22', '5A1A2C', '8E1C30', 'C4283E', 'EE5C56'],
    'cloth': ['140E20', '1F192D', '2E2A40', '51576D'],
})
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
