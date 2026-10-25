# 스킨 메뉴 초상 128칸 — alchemist__stone (색 대응: 기본 alchemist 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
from .skin import Variant

V = Variant('alchemist', 'alchemist__stone',
            # 머리: 32칸 흰 머리 → 레몬 금발 (FFE26D) — 최고 단계는 기본 흰 머리(Y 218)와 같은 자리 · 얼굴 밝은 살(Y 222) 아래
            ramps={'hair': ['5C351E', 'A86E2C', 'DCAA3C', 'F0C850', 'FADC6A'],
                   'lens': ['7A1030', 'E23654', 'FF7C84']})
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
