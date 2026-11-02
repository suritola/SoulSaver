# 새 영웅 셋(인형사 · 장의사 · 사진사) 스킨 12장의 메뉴 초상 128칸 공용 설정 (2.2.0~)
#   스킨마다 .py 한 줄 모듈(예: puppeteer__shadow.py) → 여기서 Variant2 를 만들어 돌려줌
#   기본 영웅 모듈(puppeteer · undertaker · photographer)의 부위 · 형태 그대로 + 재질 단계(ramps) 교체 + shape(c, mod) 덧그림
import numpy as np

from .portrait import poly_mask, ellipse_mask, line_cells, n_plane, n_ellipsoid, path
from .skinops import conv, box, cells, block_glyph
from .skinshape import Variant2 as Variant
from .skinshape import poly, rect, ell


def pid(c, name):
    return [i for i, p in enumerate(c.parts) if p['name'] == name]


def erase(c, names):
    m = np.isin(c.pid, [i for n in names for i in pid(c, n)]) & (c.mat >= 0)
    c.mat[m] = -1
    c.pid[m] = -1
    c.lvl[m] = 0
    c.fixed[m] = False
    for key in [k for k in c.color if m[k]]:
        del c.color[key]


def new_part(c, name, mask, mat, normal=None, bias=0.0, line=True):
    return c.part(name, mask, mat, normal or n_plane(0.2, -0.5, 0.8), bias=bias, line=line)


def grade(c, names, y0=96, by=-1, left_x=0, left_by=1):
    """아래쪽(y0~)을 한 단계 어둡게 · 왼쪽(x<left_x)을 한 단계 밝게 (위 · 왼 광원 맞춤)"""
    ids = [i for n in names for i in pid(c, n)]
    m = np.isin(c.pid, ids) & (c.mat >= 0)
    ys, xs = np.nonzero(m)
    low = [(int(x), int(y)) for y, x in zip(ys, xs) if y >= y0]
    c.nudge(low, by, ids)
    if left_x:
        c.nudge([(int(x), int(y)) for y, x in zip(ys, xs) if x < left_x and y < 118], left_by, ids)


def grade_all(c, lo=95, hi=55, skip=()):
    """몸 전체: lo 줄 아래는 한 단계 어둡게 · hi 줄 위는 한 단계 밝게 (위 광원 맞춤)"""
    sk = [i for n in skip for i in pid(c, n)]
    ys, xs = np.nonzero((c.mat >= 0) & ~np.isin(c.pid, sk))
    c.nudge([(int(x), int(y)) for y, x in zip(ys, xs) if y >= lo], -1)
    c.nudge([(int(x), int(y)) for y, x in zip(ys, xs) if y <= hi], 1)


# ───────────── 인형사
def pu_shadow(c, mod):
    grade(c, ['cloak', 'sleeve', 'sh_l', 'sh_r', 'arm_l'], 200, 0, 58, 1)
    return []


def pu_paper(c, mod):
    cl = pid(c, 'cloak')
    for pts in ([(46, 72), (50, 96), (44, 118)], [(66, 76), (62, 100), (70, 118)], [(38, 90), (40, 110)]):
        cc = line_cells(pts)
        c.nudge(cc, -2, cl)
        c.nudge([(x + 1, y) for x, y in cc], 1, cl)
    ell(c, 'bowl', 12.5, 16.2, 1.7, 1.1, 'gold', behind=False)
    ell(c, 'bowr', 15.5, 16.2, 1.7, 1.1, 'gold', behind=False)
    grade(c, ['cloak', 'sleeve', 'sh_l', 'sh_r'], 64, -1)
    return []


def pu_crypt(c, mod):
    ell(c, 'soul1', 8.8, 12.6, 0.9, 1.4, 'soul', behind=True)
    ell(c, 'soul2', 23.2, 13.8, 0.9, 1.4, 'soul', behind=True)
    return []


def pu_devhand(c, mod):
    S = c.S
    erase(c, ['bar', 'cap63', 'cap94', 'thr0', 'thr1', 'thr2', 'thr3'])
    # 마우스 커서: 검은 테 + 흰 화살 (손이 꼬리를 쥠)
    outer = poly_mask([(66, 3), (66, 33), (73, 27), (78, 38), (85, 35), (80, 25), (89, 25)], S)
    inner = poly_mask([(69, 9), (69, 28), (74, 23), (80, 34), (83, 33), (78, 22), (84, 22)], S)
    new_part(c, 'cur_o', outer, 'cursor_o', line=False)
    new_part(c, 'cur', inner, 'cursor', line=False)
    c.set_lvl(cells(70, 10, 72, 26), 0, mat='cursor')
    new_part(c, 'cable', poly_mask([(88, 10), (94, 10), (94, 14), (91, 14), (91, 30), (88, 30)], S) & (c.mat < 0), 'thread', line=False)
    conv(c, box(c, 0, 80, S, 88), 'titlebar', parts=['cloak', 'sh_l', 'sh_r'], lmap=[0, 0, 1, 1, 1])
    conv(c, box(c, 0, 110, S, 122), 'taskbar', parts=['cloak', 'slit'], lmap=[0, 0, 1, 1, 1])
    c.set_lvl(cells(40, 113, 46, 119), 0, mat='ic1')
    c.set_lvl(cells(50, 113, 56, 119), 0, mat='titlebar')
    c.set_lvl(cells(60, 113, 66, 119), 1, mat='titlebar')
    grade(c, ['cloak', 'sh_l', 'sh_r', 'sleeve'], 64, -1, 62, 1)
    folds(c, ['sleeve', 'sh_r'], [([(78, 44), (80, 62)], -2), ([(84, 44), (86, 62)], -1), ([(70, 70), (74, 66)], -1)])
    folds(c, ['cloak'], [([(40, 70), (38, 100)], 1), ([(48, 90), (46, 108)], 1), ([(60, 90), (58, 108)], -1)])
    grade_all(c, 100, 45)
    return []


# ───────────── 장의사
def strokes(c, specs):
    """[(부위 이름들, 점들, 단계)] — 2칸 굵기 선을 그 단계로 (덧그림 · 새 덩어리)"""
    for names, pts, lv in specs:
        ids = [i for n in names for i in pid(c, n)]
        cc = line_cells(pts)
        c.set_lvl(cc + [(x + 1, y) for x, y in cc], lv, ids)


def folds(c, names, lines):
    ids = [i for n in names for i in pid(c, n)]
    for pts, by in lines:
        cc = line_cells(pts)
        c.nudge(cc + [(x + 1, y) for x, y in cc], by, ids)


def ut_keeper(c, mod):
    S = c.S
    erase(c, ['crown', 'top', 'band', 'brim'])
    hood = poly_mask([(64, 6), (74, 20), (82, 44), (89, 62), (90, 76), (38, 76), (39, 62), (46, 44), (54, 20)], S)
    hood &= ~ellipse_mask(64, 49, 13, 12, S)
    hood &= ~poly_mask([(55, 61), (73, 61), (70, 82), (64, 88), (58, 82)], S)
    hood &= ~ellipse_mask(64, 66, 9, 8, S)
    new_part(c, 'hood', hood, 'hood', n_ellipsoid(64, 44, 28, 36), bias=0.0)
    c.nudge([(x, y) for x in range(64, 90) for y in range(20, 70) if c.pid[y, x] == pid(c, 'hood')[0]], -1)
    rect(c, 'lampf', 9.0, 27.6, 12.0, 31.0, 'lampf', behind=False)
    rect(c, 'lamp', 9.4, 28.4, 11.6, 30.4, 'lamp', behind=False)
    rect(c, 'lamph', 10.2, 25.8, 10.8, 27.8, 'lampf', behind=False)
    hd = pid(c, 'hood')
    for pts, by in (([(56, 24), (50, 48), (44, 68)], -1), ([(72, 24), (78, 48), (84, 68)], 1), ([(64, 12), (64, 40)], 1), ([(48, 52), (46, 70)], -1)):
        cc = line_cells(pts)
        c.nudge(cc + [(x + 1, y) for x, y in cc], by, hd)
    folds(c, ['coat'], [([(44, 80), (42, 108)], 2), ([(56, 92), (54, 110)], 2), ([(74, 92), (76, 110)], -1), ([(84, 80), (86, 108)], 2), ([(46, 70), (50, 74)], 1)])
    c.set_lvl(cells(46, 100, 56, 104), 3, pid(c, 'coat'))
    c.set_lvl(cells(72, 100, 82, 104), 3, pid(c, 'coat'))
    grade(c, ['coat', 'hood'], 92, -1)
    return []


def ut_mason(c, mod):
    S = c.S
    conv(c, box(c, 44, 84, 84, 111), 'apron', parts=['coat'], lmap=[1, 1, 1, 2, 2])
    conv(c, box(c, 54, 80, 74, 84), 'apron', parts=['coat'], lmap=[0, 0, 0, 0, 0])
    ap = pid(c, 'coat')
    for pts, by in (([(52, 90), (50, 108)], -1), ([(60, 90), (60, 108)], 1), ([(70, 90), (72, 108)], -1), ([(78, 90), (79, 108)], 1)):
        cc = line_cells(pts)
        c.nudge(cc + [(x + 1, y) for x, y in cc], by, ap)
    c.set_lvl(cells(54, 98, 74, 102), 0, ap, mat='apron')
    strokes(c, [(['coat'], [(52, 88), (52, 108)], 2), (['coat'], [(60, 90), (60, 108)], 0), (['coat'], [(68, 88), (68, 108)], 2), (['coat'], [(76, 90), (76, 108)], 0), (['coat'], [(46, 92), (82, 92)], 0)])
    rect(c, 'chisel', 19.6, 22.8, 20.6, 26.4, 'tool', behind=False)
    folds(c, ['coat'], [([(44, 80), (42, 108)], 2), ([(84, 80), (86, 108)], 2)])
    c.set_lvl(cells(46, 94, 54, 98), 0, pid(c, 'coat'))
    c.set_lvl(cells(76, 94, 84, 98), 4, pid(c, 'coat'))
    c.set_lvl(cells(56, 106, 72, 110), 0, ap, mat='apron')
    c.set_lvl(cells(60, 86, 68, 92), 2, ap, mat='apron')
    ht = pid(c, 'crown')
    c.set_lvl(cells(52, 10, 60, 18), 3, ht)
    c.set_lvl(cells(68, 12, 76, 20), 1, ht)
    c.set_lvl(cells(50, 22, 56, 27), 1, pid(c, 'band'))
    c.set_lvl(cells(66, 66, 74, 72), 4, pid(c, 'sh_r'))
    c.set_lvl(cells(54, 66, 62, 72), 0, pid(c, 'sh_l'))
    grade(c, ['coat', 'hat'], 110, -1)
    folds(c, ['crown'], [([(52, 10), (52, 28)], 1), ([(60, 8), (60, 28)], -1), ([(70, 8), (70, 28)], 1)])
    folds(c, ['coat', 'arm_l', 'arm_r'], [([(48, 66), (46, 84)], -2), ([(80, 66), (82, 84)], 1), ([(38, 84), (38, 100)], 1), ([(90, 84), (88, 100)], -1)])
    c.set_lvl(cells(44, 106, 52, 110), 4, pid(c, 'coat'))
    c.set_lvl(cells(54, 64, 60, 70), 0, pid(c, 'lap_l'))
    c.set_lvl(cells(68, 64, 74, 70), 4, pid(c, 'lap_r'))
    c.set_lvl(cells(56, 36, 62, 40), 0, pid(c, 'brim'))
    c.set_lvl(cells(66, 36, 72, 40), 3, pid(c, 'brim'))
    c.set_lvl(cells(78, 106, 86, 110), 0, pid(c, 'coat'))
    grade_all(c, 108, 40)
    return []


def ut_hell(c, mod):
    S = c.S
    poly(c, 'flame', [(15.5, 2.6), (16.6, 4.0), (17.2, 5.4), (16.6, 6.8), (14.4, 6.8), (13.8, 5.4), (14.5, 4.2)], 'flame', behind=False)
    conv(c, box(c, 0, 106, S, 112), 'lava', parts=['coat'], lmap=[0, 1, 1, 2, 2])
    c.set_lvl(cells(60, 14, 64, 18), 0, mat='flame')
    return []


def ut_todo(c, mod):
    rect(c, 'note', 13.4, 2.6, 18.6, 6.6, 'note', behind=False)
    c.set_lvl(cells(54, 11, 70, 14), 0, mat='ink')
    c.set_lvl(cells(54, 17, 66, 20), 0, mat='ink')
    poly(c, 'hgt', [(14.2, 17.4), (16.8, 17.4), (15.5, 19.6)], 'sand', behind=False)
    poly(c, 'hgb', [(15.5, 19.6), (16.8, 21.8), (14.2, 21.8)], 'sand', behind=False)
    return []


# ───────────── 사진사
def ph_press(c, mod):
    rect(c, 'brim', 10.2, 14.4, 19.8, 15.4, 'hair', behind=False)
    rect(c, 'card', 13.6, 12.6, 16.8, 14.4, 'card', behind=False)
    c.set_lvl(cells(56, 52, 66, 54), 0, mat='cam')
    return []


def ph_instant(c, mod):
    S = c.S
    names = ['jacket', 'sl_l', 'sl_r', 'pocket_l', 'pocket_r']
    for k, y0 in enumerate(range(78, 114, 2)):
        conv(c, box(c, 0, y0, S, y0 + 2), 'rb%d' % (k % 6), parts=names, lmap=[0, 0, 1, 1, 1])
    rect(c, 'photo', 13.0, 25.4, 17.8, 29.4, 'photo', behind=False)
    c.set_lvl(cells(54, 103, 70, 112), 0, mat='rb0')
    grade(c, ['hair', 'pants', 'jacket', 'sl_l', 'sl_r'], 64, -1)
    folds(c, ['jacket', 'sl_l', 'sl_r'], [([(40, 84), (36, 98)], 1), ([(86, 84), (90, 98)], -1), ([(50, 106), (50, 112)], 1), ([(74, 106), (74, 112)], -1)])
    c.set_lvl(cells(40, 90, 46, 96), 2, pid(c, 'sl_l'))
    c.set_lvl(cells(78, 90, 84, 96), 1, pid(c, 'sl_r'))
    c.set_lvl(cells(44, 48, 52, 54), 2, pid(c, 'hair'))
    c.set_lvl(cells(76, 112, 84, 118), 1, pid(c, 'pants'))
    c.set_lvl(cells(60, 40, 66, 46), 2, pid(c, 'hair'))
    c.set_lvl(cells(66, 84, 72, 90), 0, pid(c, 'cam'))
    c.set_lvl(cells(50, 84, 56, 90), 0, pid(c, 'cam'))
    strokes(c, [(['cam'], [(48, 84), (48, 103)], 2), (['cam'], [(76, 84), (76, 103)], 0), (['pod_a'], [(90, 40), (80, 80)], 0), (['pod_b'], [(98, 44), (88, 84)], 2), (['sl_l'], [(36, 90), (34, 100)], 2), (['sl_r'], [(88, 90), (90, 100)], 0), (['pants'], [(48, 112), (48, 118)], 0), (['hair'], [(66, 42), (72, 50)], 0)])
    strokes(c, [(['pocket_l'], [(44, 108), (56, 108)], 0), (['pocket_r'], [(68, 108), (80, 108)], 3), (['film_l'], [(48, 102), (48, 108)], 0), (['hair'], [(52, 44), (50, 52)], 0), (['hair'], [(76, 46), (78, 52)], 2), (['face'], [(50, 64), (54, 70)], 1), (['neck'], [(58, 74), (66, 74)], 0)])
    for k, (x, y) in enumerate([(40, 82), (84, 96), (38, 100), (80, 82), (46, 108), (74, 108), (34, 88), (90, 88), (42, 96), (82, 102)]):
        c.set_lvl(cells(x, y, x + 4, y + 3), 0, mat='rb%d' % ((k + 2) % 6))
    grade_all(c, 84, 56)
    return []


def ph_ghost(c, mod):
    S = c.S
    yy, xx = c.yy, c.xx
    gone = (yy >= 112) & ((xx.astype(int) // 3) % 3 == 1) & (c.mat >= 0)
    for nm in ('pants', 'shoe_l', 'shoe_r'):
        m = gone & np.isin(c.pid, pid(c, nm))
        c.mat[m] = -1; c.pid[m] = -1
    ell(c, 'orb1', 9.4, 13.4, 0.9, 1.1, 'orb', behind=True)
    ell(c, 'orb2', 23.4, 21.0, 0.9, 1.1, 'orb', behind=True)
    return []


def ph_f12(c, mod):
    S = c.S
    conv(c, box(c, 43, 82, 82, 106), 'key', parts=['cam', 'camtop', 'ring', 'silver', 'lens', 'finder'], lmap=[0, 1, 2, 2, 2])
    Fr = ['###', '#..', '##.', '#..', '#..']
    One = ['.#', '##', '.#', '.#', '.#']
    Two = ['###', '..#', '###', '#..', '###']
    rows = [Fr[i] + '.' + One[i] + '.' + Two[i] for i in range(5)]
    c.set_lvl(block_glyph(rows, 47, 88, 3), 0, mat='keyink')
    rect(c, 'b1', 8.0, 12.2, 10.0, 13.0, 'cyan', behind=False)
    rect(c, 'b2', 8.0, 12.2, 8.8, 14.2, 'cyan', behind=False)
    rect(c, 'b3', 23.2, 12.2, 25.0, 13.0, 'cyan', behind=False)
    rect(c, 'b4', 24.2, 12.2, 25.0, 14.2, 'cyan', behind=False)
    rect(c, 'b5', 8.0, 29.4, 10.0, 30.2, 'cyan', behind=False)
    rect(c, 'b6', 8.0, 27.8, 8.8, 30.2, 'cyan', behind=False)
    rect(c, 'b7', 23.2, 29.4, 25.0, 30.2, 'cyan', behind=False)
    rect(c, 'b8', 24.2, 27.8, 25.0, 30.2, 'cyan', behind=False)
    c.set_lvl(cells(56, 86, 62, 90), 0, pid(c, 'key'))
    for (x, y) in ((38, 86), (86, 90), (40, 104), (82, 106), (58, 108), (90, 100)):
        c.set_lvl(cells(x, y, x + 3, y + 3), 0, mat='cyan')
    for (x, y, nm, lv) in ((40, 84, 'jacket', 0), (84, 88, 'jacket', 3), (46, 104, 'jacket', 0), (76, 104, 'jacket', 3), (36, 96, 'sl_l', 0), (88, 96, 'sl_r', 0), (54, 114, 'pants', 1), (68, 114, 'pants', 1)):
        c.set_lvl(cells(x, y, x + 4, y + 4), lv, pid(c, nm))
    c.set_lvl(cells(54, 60, 60, 66), 1, pid(c, 'face'))
    c.set_lvl(cells(66, 108, 72, 114), 0, pid(c, 'pants'))
    c.set_lvl(cells(86, 100, 90, 103), 3, pid(c, 'sl_r'))
    c.set_lvl(cells(50, 70, 56, 74), 0, pid(c, 'neck'))
    c.set_lvl(cells(84, 60, 88, 64), 2, pid(c, 'pod_a'))
    c.set_lvl(cells(52, 112, 58, 116), 0, pid(c, 'pants'))
    c.set_lvl(cells(36, 100, 40, 103), 0, pid(c, 'sl_l'))
    c.set_lvl(cells(60, 40, 66, 44), 0, pid(c, 'hair'))
    c.set_lvl(cells(70, 66, 76, 70), 1, pid(c, 'jacket'))
    c.set_lvl(cells(44, 66, 50, 70), 1, pid(c, 'jacket'))
    c.set_lvl(cells(90, 50, 94, 54), 0, pid(c, 'pod_b'))
    strokes(c, [(['pod_a'], [(90, 40), (80, 80)], 2), (['pod_b'], [(98, 44), (88, 84)], 0), (['sl_l'], [(36, 90), (34, 100)], 0), (['sl_r'], [(88, 90), (90, 100)], 3), (['pants'], [(48, 112), (48, 118)], 1), (['hair'], [(66, 42), (72, 50)], 0), (['jacket'], [(46, 80), (44, 100)], 3)])
    strokes(c, [(['pocket_l'], [(44, 108), (56, 108)], 0), (['pocket_r'], [(68, 108), (80, 108)], 3), (['hair'], [(52, 44), (50, 52)], 0), (['hair'], [(76, 46), (78, 52)], 2), (['face'], [(50, 64), (54, 70)], 1), (['neck'], [(58, 74), (66, 74)], 0)])
    c.set_lvl(cells(70, 90, 74, 93), 1, pid(c, 'jacket'))
    c.set_lvl(cells(36, 100, 40, 103), 0, pid(c, 'sl_l'))
    c.set_lvl(cells(86, 100, 90, 103), 3, pid(c, 'sl_r'))
    c.set_lvl(cells(50, 70, 56, 74), 0, pid(c, 'neck'))
    c.set_lvl(cells(84, 60, 88, 64), 2, pid(c, 'pod_a'))
    c.set_lvl(cells(52, 112, 58, 116), 0, pid(c, 'pants'))
    grade_all(c, 105, 70)
    return []


