# 새 영웅 셋(인형사 · 장의사 · 사진사) 스킨 12장의 메뉴 초상 128칸 공용 틀 (2.2.0~)
#   스킨마다 .py 한 줄 모듈(예: puppeteer__shadow.py) → make() 가 Variant2 를 만들어 돌려줌 · 설정은 skins4_cfg.py · 덧그림은 skins4_shapes.py
from .skinshape import Variant2 as Variant, tune
from .skins4_cfg import CFG


def make(name):
    c = dict(CFG[name])
    base = c.pop('base')
    warm = c.pop('warm', ())
    tn = c.pop('tune', None)
    v = Variant(base, name, warm=warm, auto=False, **c)
    return tune(v, **tn) if tn else v
