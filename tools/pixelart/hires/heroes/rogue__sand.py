# 스킨 메뉴 초상 128칸 — rogue__sand (색 대응: 기본 rogue 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
#   단계 색은 손으로 정함 (32칸 대응 색을 닻으로 · 단계 수 · 명도 순서 유지 · 옷 최고 단계는 얼굴 밝은 살 근처 아래 — 흑백 1위는 얼굴 · 눈)
from .skin import Variant

V = Variant('rogue', 'rogue__sand', auto=False, ramps={
    # 사막: 보라 두건 → 모래 (32칸 5F371F · 90683F · CEAC70 · FFE69B) — 바탕 Y 약 150, 눈(Y 약 235)보다 어둡게
    'hood':  ['5F371F', '8E663E', 'B8945E', 'CCAA6E', 'E2C68A'],
    # 망토 → 낙타 갈색 (452B28 · 967046) · 림 = 바랜 회녹 (777974 · 999C8C)
    'cloak': ['3A2420', '5E3E2A', '8E6A44', '8A8672', 'ACAE9C'],
    # 목도리 → 가죽 갈색 (3B212D · 68382E · 965C3A · C08650)
    'scarf': ['3B212D', '68382E', '965C3A', 'C08650'],
    # 얼굴 그늘 → 짙은 갈색 (32칸 5F371F 쪽)
    'void':  ['2E1A16', '4A2C1C'],
}, glob={'EYE': 'FFECBE', 'EYE_HOT': 'FFFAEA'})
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
