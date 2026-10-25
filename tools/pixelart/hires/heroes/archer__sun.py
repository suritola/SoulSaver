# 스킨 메뉴 초상 128칸 — archer__sun (색 대응: 기본 archer 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
#   단계 색은 손으로 정함 (32칸 대응 색을 닻으로 · 단계 수 · 명도 순서 유지 · 옷 최고 단계는 얼굴 밝은 살 Y 199 근처 아래)
from .skin import Variant

V = Variant('archer', 'archer__sun', auto=False, ramps={
    # 두건: 초록 → 햇빛 금 (32칸 733000 · AF6811 · FABE3C · FFDC58) — 금이 얼굴보다 밝지 않게 바탕 Y 160 근처
    'hood':  ['5A2410', '9E5C14', 'DA9C30', 'EAB244', 'EEC250'],
    # 조끼 · 바지: 상아 흰 (32칸 A79C8F · EEE8DA · FFFBED) — 얼굴(Y 199)보다 한 단계 어둡게
    'vest':  ['625246', '9A8C74', 'C8BC9E', 'D0C4A4'],     # 판정 2회차 권장: 크림 흰 쪽 · 최고 Y 196
    'pants': ['54464A', '887A6A', 'B4A88E', 'C8BC9E'],
    # 장화: 그을린 갈색 (32칸 714A2B)
    'boot':  ['3A2420', '583822', '78502E', '946C44'],
})
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
