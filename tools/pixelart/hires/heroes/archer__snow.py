# 스킨 메뉴 초상 128칸 — archer__snow (색 대응: 기본 archer 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
#   단계 색은 손으로 정함 (32칸 대응 색을 닻으로 · 단계 수 · 명도 순서 유지 · 옷 최고 단계는 얼굴 밝은 살 Y 199 근처 아래)
from .skin import Variant

V = Variant('archer', 'archer__snow', auto=False, ramps={
    # 두건: 초록 → 눈 흰 (32칸 606172 · 9C9FAE · ECF0F8) — 흰 두건이 얼굴보다 밝지 않게 바탕을 은회색으로 누름
    'hood':  ['363A54', '6E7694', 'AAB2C8', 'B6BED0', 'BEC4D6'],     # 판정 2회차: 최고 단계 Y 196 (살갗 최고 200 아래)
    # 깃털 · 화살통: 살짝 푸른 흰 (32칸 E6F8FF)
    'pale':  ['363A4E', '5E6678', '8A96A8', 'B4C6D6', 'E2F2FC'],
    # 조끼: 하늘 파랑 (32칸 43609E · 78A8E2 · 98D2FF) · 바지: 옅은 하늘 (78AAE4 · BEE8FF — 얼굴 아래로 누름)
    'vest':  ['34406E', '5274B0', '82B0E2', '9CC6EE'],
    'pants': ['364874', '668EC2', '94BAE0', 'A8CCEA'],
    # 장화: 짙은 남청 (32칸 415179) · 쇠붙이 버클: 은 (32칸 707096)
    'boot':  ['222644', '303C64', '465882', '64789E'],
    'brass': ['707096', 'B4B4D2'],
})
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
