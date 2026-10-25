# 스킨 메뉴 초상 128칸 — alchemist__mercury (색 대응: 기본 alchemist 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
from .skin import Variant

V = Variant('alchemist', 'alchemist__mercury', skip={'hair'},
            ramps={'robe': ['3C3A5A', '646A8C', '949EBC', 'ACB8D6', 'C0CCEA'],
                    'lens': ['4A7CC8', '7CB4F8', 'B8DCFF']},
            # 깃: 목 둘레 은빛 (32칸 깃 칸 A9C9DF) — 몸통보다 위가 밝게 (위 광원)
            parts={'collar': ['4A4A6E', '7A82A4', 'A0ACCC', 'B4C0E0', 'C8D2EE']})     # 고글 알: 수은 파랑 (32칸 4280E8 · 8CC4FF) — 기본 청록 알처럼 밝게     # 은빛 겉옷(32칸 4A4962 · B0B8CC · E1EDFF) — 바탕 Y 약 150, 최고 단계도 흰 머리(Y 218) · 밝은 살 아래
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
