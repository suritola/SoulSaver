# 스킨 메뉴 초상 128칸 — archer__autumn (색 대응: 기본 archer 초상의 부위 · 형태 그대로, 재질 단계만 스킨 32칸 색으로 · skin.py)
#   단계 색은 손으로 정함 (32칸 대응 색을 닻으로 · 단계 수 · 명도 순서 유지 · 옷 최고 단계는 얼굴 밝은 살 Y 199 근처 아래)
from .skin import Variant

V = Variant('archer', 'archer__autumn', auto=False, ramps={
    # 두건: 초록 → 단풍 주황 (32칸 660D00 · 9B2D05 · DE7028 · FFA64B)
    'hood':  ['4A0E14', '8E2A0C', 'CC6228', 'EE9644', 'F8B262'],
    # 조끼 · 바지: 한 단계씩 짙은 밤색 (32칸 조끼 966038 → 70462C · 바지 70462C → 8C5634)
    'vest':  ['301820', '4E2315', '70462C', '936C46'],
    'pants': ['2E1A22', '5E3228', '8C5634', 'A87048'],
})
SIZE, DESIGN = V.SIZE, V.DESIGN
draw, build = V.draw, V.build
