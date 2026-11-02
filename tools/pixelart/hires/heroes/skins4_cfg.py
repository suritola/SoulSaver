# 새 영웅 셋 스킨 12장의 재질 단계 · 덧그림 설정 (skins4.py 가 읽음)
from .skins4_shapes import *


def R(*cols):
    return list(cols)


CFG = {
    'puppeteer__shadow': dict(base='puppeteer', ramps={
        'cloak': R('332C52', '3E3862', '4C4574', '5E5688', '746CA0'), 'lining': R('2E2850', '3A3262'), 'hair': R('2E2A44', '3A3654', '4C486A'),
        'skin': R('3C3458', '463E66', '54487A', '645A8C'), 'boot': R('38305A', '463E6C'), 'gold': R('5E5688', '746CA0'),
        'dwhite': R('3E3860', '4C4574', '5E5688'), 'dmag': R('34305A', '3E3A66', '4C4676'), 'dblack': R('3A3458'),
    }, glob={'EYE': 'F4F0FF'}, tune={'_all': 0.72}, shape=pu_shadow),
    'puppeteer__paper': dict(base='puppeteer', ramps={
        'cloak': R('C8B27A', 'D8C48C', 'E8D6A2', 'F4E8C4', 'FFF8E0'), 'lining': R('A8905A', 'C0A870'), 'thread': R('B8946A', 'D8B880'),
        'gold': R('B02828', 'E04848'), 'boot': R('8A7648', 'A8946A'), 'dwhite': R('D8C8A0', 'F4E8C8', 'FFFFF0'),
        'dmag': R('C86A5A', 'E88A78', 'FFB0A0'), 'dblack': R('6A5A3A'),
    }, tune={'_all': 0.9}, shape=pu_paper),
    'puppeteer__crypt': dict(base='puppeteer', ramps={
        'cloak': R('2C4A48', '3E6862', '5C8C86', '88C0B0', 'B8E0D0'), 'lining': R('2C4A48'), 'hair': R('A8B8B4', 'D0D8D4', 'FFFFFF'),
        'skin': R('7A8C88', 'A8BCB4', 'D4E4DC', 'F0F8F4'), 'wood': R('9C9478', 'D4CCB0', 'F6F0DA'), 'thread': R('8A9894', 'C8D4D0'),
        'gold': R('58E8A8', 'A8FFD0'), 'boot': R('26343A', '3A4A50'), 'dwhite': R('A8A088', 'DCD4BC', 'FAF4E0'),
        'dmag': R('2C3E38', '3E564E', '56786A'),
    }, add={'soul': dict(ramp=['58E8A8', 'A8FFD0'], cuts=[0.6])}, glob={'EYE': '58E8A8'}, tune={'_all': 0.82}, shape=pu_crypt),
    'puppeteer__devhand': dict(base='puppeteer', ramps={
        'cloak': R('2C3850', '384868', '465A80', '5A709C', '7C94BC'), 'lining': R('28344C', '34445E'), 'hair': R('1E2A44', '2A3858', '3C4C70'),
        'wood': R('4A5470', '6A7692', '98A4C0'), 'thread': R('2A5ED8', '6AA0FF'), 'gold': R('20A8C0', '60F0FF'), 'boot': R('2C3850', '3C4A68'),
        'dwhite': R('D8E4FA', 'FFFFFF'), 'dmag': R('1C48A0', '3A80F0'), 'hair': R('1E2A44', '3C4C70'), 'boot': R('2C3850'), 'lining': R('28344C'), 'wood': R('4A5470', '98A4C0'),
    }, add={'cursor_o': dict(ramp=['1A112C'], cuts=[]), 'cursor': dict(ramp=['FFFFFF'], cuts=[]),
            'titlebar': dict(ramp=['2A5ED8', '4A80F0'], cuts=[0.7]), 'taskbar': dict(ramp=['4A5068'], cuts=[]),
            'ic1': dict(ramp=['58C060'], cuts=[])},
        glob={'EYE': '40F0FF'}, tune={'_all': 0.88}, shape=pu_devhand),
    'undertaker__keeper': dict(base='undertaker', ramps={
        'coat': R('2A3224', '384430', '4C5C40', '627654', '7C9268'), 'pants': R('222A1E', '30382A'), 'shoe': R('2A3224', '3C4630'),
        'glove': R('2A3224', '4C5C40'), 'stone': R('34422E', '46563C'),
    }, add={'hood': dict(ramp=['2C3424', '3E4A34', '566648', '6E8060'], cuts=[0.5, 0.75, 0.95]), 'lampf': dict(ramp=['2C2C3C'], cuts=[]),
            'lamp': dict(ramp=['C8A838', 'FFE070', 'FFF8C0'], cuts=[0.5, 0.9], warm=True)}, warm=('lamp', 'lampf', 'lamph'), shape=ut_keeper),
    'undertaker__mason': dict(base='undertaker', ramps={
        'hat': R('60606E', '8886A0', 'B8B6CC', 'F0EEFA'), 'silver': R('C8B890', 'F0E4C0'),
        'coat': R('3E322A', '54443A', '6E5A48', '8A7662', 'A8927A'), 'pants': R('3A3028', '4C4036'), 'stone': R('5C5A68', '6E6C7A'), 'shoe': R('3A3028', '4C4036'), 'glove': R('3A3028', '54443A'),
    }, add={'apron': dict(ramp=['B8A888', 'D8CCB0', 'F0E8D4'], cuts=[0.6, 1.0]), 'tool': dict(ramp=['D4DEF2'], cuts=[])}, shape=ut_mason),
    'undertaker__hell': dict(base='undertaker', ramps={
        'coat': R('3A1218', '52202A', '7A3030', 'A04040', 'C85850'), 'hat': R('2A1418', '3E1820', '5A2428', '7A3438'),
        'silver': R('FF7A28', 'FF9A38', 'FFD060'), 'skin': R('6A4A44', '8A6A5C', 'B89484', 'D8B8A0'), 'shirt': R('B08870', 'D8B8A0', 'F4E0C8'),
        'wood': R('4A2A20', '6A3C2A', '9A5A38'), 'glove': R('2A1418', '4A2428'), 'pants': R('2A1418', '3A1C22'), 'shoe': R('2A1418', '4A2428'),
        'stone': R('3A2028', '4A2C34'),
    }, add={'flame': dict(ramp=['FF7A28', 'FF9A38', 'FFD060'], cuts=[0.5, 0.9], warm=True), 'lava': dict(ramp=['B02808', 'FF7A28', 'FFC040'], cuts=[0.5, 0.9], warm=True)},
        glob={'EYE': 'FF9020'}, warm=('flame',), shape=ut_hell),
    'undertaker__todo': dict(base='undertaker', ramps={
        'coat': R('5C5C68', '70707C', '8A8A96', 'A8A8B4', 'C4C4CC'), 'hat': R('6A6A74', '8A8A94', 'A8A8B4', 'C4C4CC'), 'silver': R('9C9CA8', 'D0D0D8', 'F4F4F8'),
        'skin': R('8C8C98', 'B0B0BC', 'D0D0DA', 'E8E8F0'), 'wood': R('5A5A64', '7A7A86', 'A0A0AC'), 'pants': R('44444E', '565660'),
        'shoe': R('4A4A54'), 'glove': R('40404A', '5A5A66'), 'stone': R('50505C', '64646E'),
    }, add={'note': dict(ramp=['FFE060'], cuts=[], warm=True), 'ink': dict(ramp=['40404A'], cuts=[]), 'sand': dict(ramp=['F4D060'], cuts=[], warm=True)},
        warm=('note', 'hgt', 'hgb'), shape=ut_todo),
    'photographer__press': dict(base='photographer', ramps={
        'jacket': R('9C8A62', 'B8A47A', 'CDB888', 'E8D8AC'), 'hair': R('5A4228', '6A4A28', '9A6E3C'), 'pants': R('3E382E', '54483A'), 'shoe': R('4A3A2C'),
    }, add={'card': dict(ramp=['FFFFFF'], cuts=[])}, shape=ph_press),
    'photographer__instant': dict(base='photographer', ramps={
        'cam': R('A0386A', 'E86AA0', 'FFA0C8'), 'camtop': R('D8D0E8', 'FFFFFF'), 'jacket': R('B8BED4', 'D0D4E4', 'E4E8F4', 'F8FAFF'), 'glass': R('1C2870', '78A0F0'), 'film': R('34344E'),
    }, tune={'_all': 0.85}, add=dict([('rb%d' % k, dict(ramp=[hi], cuts=[])) for k, (lo, hi) in enumerate([
        ('B8302A', 'E8483E'), ('C06818', 'F08A30'), ('C8A828', 'F4D850'), ('389040', '58C060'), ('2C60B0', '4A88E0'), ('6038A0', '8A5AD0')])]
        + [('photo', dict(ramp=['F8FAFF'], cuts=[]))]), shape=ph_instant),
    'photographer__ghost': dict(base='photographer', ramps={
        'jacket': R('8E80C8', 'A89CE0', 'C4B8F0', 'E8E0FF'), 'hair': R('7A6CB8', 'A89CE0', 'C8BCF4'), 'skin': R('A898D8', 'C8BCF0', 'E4DCFF'),
        'cam': R('3A2E70', '5A4C98', '7A6CB8'), 'camtop': R('9A8CD8', 'C8BCF4', 'F0E8FF'), 'glass': R('2C1C80', '6A3CD0', 'A870FF', 'E8D0FF'),
        'pod': R('7A6CB8', 'A89CE0', 'C8BCF4'), 'pants': R('4A3C80', '5A4C98'), 'shoe': R('6A5CA8', '8A7CC8'), 'film': R('4A3C80', '6A5CA8'), 'label': R('C890FF'),
    }, add={'orb': dict(ramp=['D8C8FF', 'A890F0'], cuts=[0.7])}, shape=ph_ghost),
    'photographer__f12': dict(base='photographer', ramps={
        'jacket': R('7CB0B4', '98CCCC', 'B8E0E0', 'E4F8F8'), 'cam': R('0E5560', '1F9EB0', '5CE0F0'), 'glass': R('103840', '208898', '40C8D8', 'A0FFF0'),
        'pod': R('5C8C94', '88B8C0', 'B8E0E8'), 'pants': R('2A5058', '3C6870'), 'camtop': R('4AA8B8', '8CE0EC', 'D0FFFF'),
    }, tune={'_all': 0.8}, add={'key': dict(ramp=['B0BCCC', 'DCE4EE'], cuts=[0.6]), 'keyink': dict(ramp=['0E5560'], cuts=[]), 'cyan': dict(ramp=['40F0FF'], cuts=[])},
        warm=('b1', 'b2'), shape=ph_f12),
}
