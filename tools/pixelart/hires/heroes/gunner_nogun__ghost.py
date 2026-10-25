# 스킨 메뉴 초상 128칸 — gunner_nogun__ghost (색 대응: 기본 gunner_nogun 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
#   단계 색은 손으로 정함 (32칸 대응 색을 닻으로 · 단계 수 · 명도 순서 유지 · 옷 최고 단계는 얼굴 밝은 살 근처 아래 — 흑백 1위는 얼굴 · 눈)
from .skin import Variant

V = Variant('gunner_nogun', 'gunner_nogun__ghost', auto=False, ramps={
    # 유령: 온몸 푸른 회백 (32칸 2C3663 · 4E628E · 5C7098 · D6F4FF) — 살도 창백한 푸른 빛 (868AA9 · D0D8F2)
    'hat':   ['222446', '34406A', '4E628E', '6C84AE', '90A8CC'],
    'gold':  ['5C6C8A', '98AAC4', 'C4D6EA'],
    'skin':  ['5C6088', '868AA9', 'CCD4EE'],
    'hand':  ['5C6088', '868AA9', 'B4BCDA'],
    # 목도리: 흰 빛 (D6F4FF) — 바탕을 얼굴 아래로 누름
    'scarf': ['34486A', '6E8CB0', 'A2C2DE', 'B6D2E6'],
    'coat':  ['1E2244', '2C3660', '4C6692', '627AA2', '7CA2C8'],
    'pants': ['24264A', '3E4C74', '55759A'],
    'boot':  ['1E2244', '2C3663', '3E486A', '5A6890'],
})
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
