# 메뉴 초상 128칸 (그래픽 가독성 개편 2단계 묶음 3) — 영웅 · 스킨마다 모듈 하나
#   모듈 규칙: SIZE(128) · DESIGN(설계도 32칸 시트 경로 — 첫 칸이 설계도) · build() → (RGBA 배열, 의도한 점 [(x, y)])
#   이름 = Resources/Characters 의 시트 이름 (기본 영웅 9 + 스킨 20 = 29장). 아직 안 그린 것은 None → 게임은 예전 32칸 초상
#   (CharacterUI.MenuPortrait 가 그 시트의 메뉴 초상이 없으면 32칸으로 — 다른 영웅 · 스킨의 초상을 대신 쓰지 않음)
import importlib

HEROES = [
    'gunner_nogun', 'swordsman', 'archer', 'alchemist', 'rogue', 'ghost', 'postman', 'clockmaker', 'cartographer',
]
SKINS = [
    'alchemist__herb', 'alchemist__mercury', 'alchemist__plague', 'alchemist__stone',
    'archer__autumn', 'archer__elf', 'archer__snow', 'archer__sun',
    'gunner_nogun__bounty', 'gunner_nogun__desert', 'gunner_nogun__ghost', 'gunner_nogun__gold',
    'rogue__ninja', 'rogue__raven', 'rogue__sand', 'rogue__void',
    'swordsman__black', 'swordsman__bronze', 'swordsman__dragon', 'swordsman__paladin',
]
ALL = HEROES + SKINS

# 시트 이름 → 모듈 이름 (그린 것만). 시안 2 = 도적 1장 — 독립 검토 판정을 넘은 뒤 나머지 28장을 더함
#   영웅 8 — 도적 규칙(README.md) 그대로 더함
MODULES = {
    'rogue': 'rogue',
    'gunner_nogun': 'gunner_nogun',
    'swordsman': 'swordsman',
    'archer': 'archer',
    'alchemist': 'alchemist',
    'ghost': 'ghost',
    'postman': 'postman',
    'clockmaker': 'clockmaker',
    'cartographer': 'cartographer',
}
# 스킨 20 — 색 대응 15벌은 기본 영웅 모듈 + 스킨 32칸 색 단계 (skin.py Variant), 모양이 다른 5벌은 + 덧그림 (뿔 · 모자 · 가면 · 날개 자락 · 머리띠)
MODULES.update({n: n for n in SKINS})


def load(name):
    """그 시트의 그리기 모듈 (없으면 None)"""
    mod = MODULES.get(name)
    return importlib.import_module(__name__ + '.' + mod) if mod else None
