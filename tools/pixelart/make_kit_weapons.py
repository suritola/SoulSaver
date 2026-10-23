# 영웅 전용 무기(진화 무기) 손에 드는 도트 — PlayerLook 가 Resources/Weapons/weapon_kit_<번호> 를 씀
#   (번호 = SpecialAbilities.Kit* 상수, KitDefs 의 kind == Weapon 인 24종)
#   그림 규칙: 기본 무기(weapon_<held>)와 같은 팔레트 · 1픽셀 외곽선(#1a112c) · 오른쪽을 가리키고 손잡이가 왼쪽
#   채움 글자로만 그리고 외곽선은 자동으로 두름 (기본 무기 그림을 그대로 가져온 칸은 이미 외곽선이 있어 건너뜀)
#   .meta 는 그 영웅 기본 무기 .meta 를 복사해 guid · spriteID 만 새로 (다시 돌리면 기존 guid 유지) · spritePivot 은 손잡이 위치로
# 실행: py -3.14 -X utf8 tools/pixelart/make_kit_weapons.py [비교 시트 경로]
import os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
import png
from png import Img

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
WEAP = os.path.join(ROOT, 'Assets', 'Resources', 'Weapons')


def hx(s):
    return (int(s[1:3], 16), int(s[3:5], 16), int(s[5:7], 16), 255)


OUT = hx('#1a112c')
PAL = {
    'o': OUT, 'O': hx('#2d192a'),
    # 쇠 (어두움 → 밝음)
    '1': hx('#373455'), '2': hx('#616a7a'), '3': hx('#7895b9'), '4': hx('#9ea6c6'), '5': hx('#d6def0'), '6': hx('#fafcff'),
    # 나무 · 가죽
    'w': hx('#5b3629'), 'W': hx('#8c5634'), 'L': hx('#b47c4a'), 'l': hx('#d0b278'), 'a': hx('#56322e'), 'A': hx('#72483c'),
    # 금
    'g': hx('#b46826'), 'G': hx('#eaaa3a'), 'y': hx('#fede6c'), 'Y': hx('#fffad2'),
    # 종이
    'c': hx('#fffaea'), 'f': hx('#faeec6'), 'e': hx('#e8d09c'), 'd': hx('#ba8e64'),
    # 빨강
    'R': hx('#e23654'), 'k': hx('#ff7c84'), 'K': hx('#9a1f40'),
    # 초록
    'v': hx('#30b676'), 'V': hx('#82ecaa'), 'u': hx('#2a7a6a'), 'U': hx('#deffe8'),
    # 파랑 · 얼음
    'b': hx('#2486b8'), 'B': hx('#50d6ea'), 'i': hx('#b0f8ff'), 'I': hx('#f2ffff'), 'h': hx('#1e4267'), 'H': hx('#205179'),
    # 보라 · 그림자
    'x': hx('#403f62'), 'X': hx('#59516e'), 'z': hx('#5a3492'), 'Z': hx('#9a6ae0'), 'j': hx('#241c3c'), 'J': hx('#d8c0ff'),
    # 잉크 · 주황 · 분홍
    'N': hx('#1e2661'), 'n': hx('#3a52a8'), 'q': hx('#e8642a'), 'Q': hx('#ffb050'), 'm': hx('#ff7cd0'),
}


class Pic:
    # 칸(x, y) → 색. new = 자동 외곽선을 둘러야 하는 칸 (기본 무기에서 가져온 칸은 제외)
    def __init__(self):
        self.px, self.new = {}, set()

    def rows(self, rows, ox=0, oy=0):
        n = len(rows[0])
        for y, row in enumerate(rows):
            assert len(row) == n, (row, len(row), n)
            for x, ch in enumerate(row):
                if ch != '.':
                    self.dot(ox + x, oy + y, ch)
        return self

    def dot(self, x, y, ch):
        self.px[(x, y)] = PAL[ch] if isinstance(ch, str) else ch
        if ch != 'o':
            self.new.add((x, y))

    def base(self, name, ox=0, oy=0):
        im = png.read(os.path.join(WEAP, 'weapon_%s.png' % name))
        for y in range(im.h):
            for x in range(im.w):
                if im.px[y][x][3] > 0:
                    self.px[(ox + x, oy + y)] = im.px[y][x]
                    self.new.discard((ox + x, oy + y))
        return self

    def outline(self):
        add = []
        for (x, y) in self.new:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                p = (x + dx, y + dy)
                if p not in self.px:
                    add.append(p)
        for p in add:
            self.px[p] = OUT
        return self

    def image(self):
        xs = [p[0] for p in self.px]; ys = [p[1] for p in self.px]
        x0, y0 = min(xs), min(ys)
        im = Img(max(xs) - x0 + 1, max(ys) - y0 + 1)
        for (x, y), c in self.px.items():
            im.set(x - x0, y - y0, c)
        return im, x0, y0


# ───────────────────────── 검사 (기본: 장검 23×6, 손에서 2.6배) ─────────────────────────

def hammer():          # 20 전쟁 망치 — 긴 자루 + 금 띠를 두른 네모난 쇠머리
    return Pic().rows([
        '.............y655443',
        '.............G554432',
        'y............G544332',
        'GGAaAaAlLLLLLG443322',
        'GgaAaAaWWWWWWG433321',
        'g............G433221',
        '.............g332211',
        '.............g322111',
    ]), (3, 3.5)


def whip():            # 21 채찍검 — 장검 손잡이 + 사슬로 이은 칼날 마디가 물결침
    return Pic().rows([
        '....Y.................',
        '....Gy....655.........',
        'yAaAGG65524432655.....',
        'gaAaGG443.....4432655.',
        '....Gg............4435',
        '....g.................',
    ]), (2, 2.5)


def lance():           # 22 기창 — 손 가리개(원뿔) + 빨강 · 흰 줄무늬 창대 + 쇠 창끝
    p = Pic()
    for x in range(0, 4):              # 손잡이
        p.dot(x, 3, 'A' if x % 2 else 'a')
        p.dot(x, 4, 'a' if x % 2 else 'A')
    p.dot(0, 3, 'G'); p.dot(0, 4, 'g')
    for y in range(0, 8):              # 손 가리개 (쇠 원뿔 밑면)
        p.dot(4, y, '5' if y < 3 else '4' if y < 5 else '3')
    for y in range(1, 7):
        p.dot(5, y, '4' if y < 3 else '3' if y < 5 else '2')
    half = {6: 2, 7: 2, 8: 2, 9: 1, 10: 1, 11: 1, 12: 1, 13: 0, 14: 0, 15: 0, 16: 0, 17: 0}
    for x, hh in half.items():         # 창대 — 끝으로 갈수록 가늘어짐 (가운데 3.5)
        for y in range(4 - hh - 1, 4 + hh + 1):
            light = y < 4
            stripe = ((x + y) // 2) % 2 == 0
            p.dot(x, y, ('R' if light else 'K') if stripe else ('c' if light else 'e'))
    for x, cols in ((18, '54'), (19, '64'), (20, '6'), (21, '5')):   # 쇠 창끝
        if len(cols) == 2:
            p.dot(x, 3, cols[0]); p.dot(x, 4, cols[1])
        else:
            p.dot(x, 3 if x == 20 else 4, cols)
    p.dot(20, 4, '4'); p.dot(21, 3, '5'); del p.px[(21, 4)]; p.new.discard((21, 4))
    return p, (2, 3.5)


# ───────────────────────── 도적 (기본: 표창 8×7) ─────────────────────────

def blowgun():         # 28 독침 대롱 — 마디 있는 대나무 대롱 + 끝에 독침
    return Pic().rows([
        'LVVVuVVuVVV..',
        'WvvvuvvuvvvqV',
        'wuuuuuuuuuu..',
    ]), (3, 1)


def cards():           # 29 도박 카드 — 카드 세 장을 비스듬히 겹침 (뒷면 · 스페이드 · 하트)
    p = Pic()

    def card(ox, oy, face):
        for y in range(7):
            for x in range(5):
                edge = x in (0, 4) or y in (0, 6)
                p.dot(ox + x, oy + y, 'o' if edge else face[y - 1][x - 1])
    card(0, 2, ['RkR', 'kRk', 'RkR', 'kRk', 'RkR'])        # 뒷면 (빨간 무늬)
    card(3, 1, ['1cc', 'c1c', '111', 'c1c', 'ccc'])        # 스페이드 (왼쪽 두 줄만 보임)
    card(6, 0, ['ccc', 'RcR', 'RRR', 'cRc', 'ccc'])        # 하트 (맨 앞)
    return p, (1.5, 5.5)


def wire():            # 30 살상 와이어 — 와이어 감개 + 은빛 줄 + 끝의 고리 칼날
    return Pic().rows([
        '.454.......6.',
        '4xXx3.....565',
        '5XGX355555456',
        '4xXx2.....434',
        '.323.......3.',
    ]), (2, 2)


# ───────────────────────── 궁수 (기본: 활 9×11) ─────────────────────────

def netbow():          # 36 그물 활 — 기본 활 + 화살 끝에 그물 뭉치
    p = Pic().base('bow')
    for x in range(8, 10):
        p.dot(x, 5, 'l')
    p.rows([
        '.l.l.',
        'lWlWl',
        'WlWlW',
        'lWlWl',
        '.l.l.',
    ], 10, 3)
    p.dot(9, 4, 'd'); p.dot(9, 6, 'd')
    return p, (3, 5)


def javelin():         # 37 투창 — 긴 나무 창대 + 금 매듭 + 나뭇잎 모양 창날
    return Pic().rows([
        '..............55..',
        'lLAaALLLLLLGG65566',
        '..............43..',
    ]), (3, 1)


def burstbow():        # 38 속사 활 — 기본 활 + 화살 세 발을 겹쳐 메김
    p = Pic().base('bow')
    for y in (3, 5, 7):
        for x in range(2, 12):
            ch = 'R' if x < 4 else 'l' if x < 10 else ('5' if x == 10 else '6')
            p.dot(x, y, ch)
    return p, (3, 5)


# ───────────────────────── 연금술사 (기본: 플라스크 7×7) ─────────────────────────

def quicksilver():     # 44 수은 구슬 — 둥근 유리병 속 반짝이는 수은
    return Pic().rows([
        '...aa...',
        '...6I...',
        '..5II5..',
        '.5I44I4.',
        '5I4664I3',
        '4I3443I3',
        '.4I22I3.',
        '..4334..',
    ]), (3.5, 4.5)


def magnet():          # 45 자석 폭탄 — 빨간 말굽 자석 (오른쪽이 열림) + 쇠 끝 + 불꽃 심지
    return Pic().rows([
        '..kkkk56',
        '.kRRRR45',
        'kRK.....',
        'RRK.....',
        'RKK.....',
        '.KKKKK34',
        '..KKKK23',
    ]), (1, 3)


def firework():        # 46 폭죽 발사기 — 빨강 · 흰 나선 통 + 노란 원뿔 머리 + 꼬리 심지 불똥
    p = Pic()
    for x in range(2, 9):
        for y in range(4):
            stripe = (x + y) % 3 == 0
            p.dot(x, y, ('c' if y < 2 else 'f') if stripe else ('R' if y < 2 else 'K'))
    p.rows(['y.', 'Qw'], 0, 1)
    p.rows(['G..', 'yG.', 'yGQ', 'gq.'], 9, 0)
    return p, (3, 1.5)


# ───────────────────────── 지도 제작자 (기본: 깃펜 8×9) ─────────────────────────

def surveychain():     # 60 측량 사슬 — 놋쇠 손잡이 고리 + 쇠사슬 두 마디 + 놋쇠 표지
    return Pic().rows([
        '.yGG............',
        'y...G.454.454.yG',
        'G...GG4.444.44Gg',
        'G...g.343.343.gg',
        '.ggg............',
    ]), (2, 2)


def inkpot():          # 61 잉크병 — 코르크 + 짙은 남색 병 + 종이 상표
    return Pic().rows([
        '..LL..',
        '..nN..',
        '.nNNN.',
        'nbNNNN',
        'nfeeeN',
        'NNNNNN',
        '.NNNN.',
    ]), (2, 3.5)


def globe():           # 62 지구본 — 파란 바다 · 초록 땅 + 금 자오선 고리 + 받침
    return Pic().rows([
        '..bBBG...',
        '.bIVvBG..',
        'bBvVvbbG.',
        'bbBvbbbG.',
        'bvvbBbbG.',
        '.bvbbbG..',
        '..bbbG...',
        '...g.....',
        '..GGG....',
    ]), (3, 4)


# ───────────────────────── 시계공 (기본: 톱니 9×9) ─────────────────────────

def cuckoo():          # 67 뻐꾸기 시계 — 지붕 집 + 문에서 나온 노란 뻐꾸기 + 시계판 + 솔방울 추
    return Pic().rows([
        '...ww...',
        '..wWWw..',
        '.wWyGWw.',
        'wWLyqLWw',
        '.WccccW.',
        '.Wc1ccW.',
        '.Wcc1cW.',
        '.WccccW.',
        '.wwwwww.',
        '..g..g..',
    ]), (3, 5.5)


def hourglass():       # 68 모래시계 — 나무 뚜껑 · 받침 + 유리 속 모래
    return Pic().rows([
        'WLLLLLW',
        '.5yyy5.',
        '.5yGy5.',
        '..5G5..',
        '...G...',
        '..5G5..',
        '.5yGy5.',
        '.yGGGg.',
        'wWWWWWw',
    ]), (3, 4)


def grandfather():     # 69 괘종시계 — 긴 나무 몸통 + 시계판 + 금 추
    return Pic().rows([
        '..wWw..',
        '.wLLLw.',
        'wLcccLw',
        'wLc1cLw',
        'wLcc1Lw',
        '.wWWWw.',
        '.WjGjW.',
        '.WjGjW.',
        '.WjyjW.',
        '.WjjjW.',
        'wwwwwww',
    ]), (3, 5)


# ───────────────────────── 우체부 (기본: 편지 9×6) ─────────────────────────

def pigeon():          # 74 비둘기 우편 — 오른쪽을 보는 회색 비둘기가 편지를 묾
    return Pic().rows([
        '.......44...',
        '......4414e.',
        '..33335V4ccc',
        '4335533z4cRc',
        '44555444.ccc',
        '.444444.....',
        '...g..g.....',
    ]), (4, 3.5)


def parcel():          # 75 소포 상자 — 갈색 상자 + 끈 십자 + 빨간 리본 매듭
    return Pic().rows([
        '...k.k...',
        '....R....',
        'llllRllll',
        'LLLLcLLLW',
        'ccccccccW',
        'LLLLcLLLW',
        'LdfLcLLLW',
        'WWWWcWWWw',
    ]), (3, 4.5)


def postbox():         # 76 우체통 — 둥근 머리 빨간 기둥 + 투입구 + 흰 안내판 + 검은 받침
    return Pic().rows([
        '.KRRRK.',
        'RkkRRRK',
        'Rk111RK',
        'RkRRRRK',
        'RkcccRK',
        'RkcRcRK',
        'RkcccRK',
        'RkRRRRK',
        'xxxxxxx',
    ]), (3, 4)


# ───────────────────────── 유령 (기본: 등불 7×8) ─────────────────────────

def soulchain():       # 81 사슬 망령 — 영혼 족쇄 고리 + 푸른 영혼 사슬 + 끝의 영혼불
    return Pic().rows([
        '.Bii..........iB.',
        'B...i.BiB.BiB.BIi',
        'b...bbB.BBB.BBIII',
        'b...H.bHb.bHb.BIb',
        '.HHH..........bB.',
    ]), (2, 2)


def iceghost():        # 82 얼음 유령 — 얼어붙은 등불: 눈꽃 품은 얼음 유리 + 서리 가시 + 고드름
    return Pic().rows([
        '...I...',
        '..i.i..',
        '.bBIBb.',
        'iIbIbIi',
        'IbIIIbB',
        'iIbIbIB',
        '.BiBiB.',
        '.i.i.i.',
        '...i...',
    ]), (3, 4)


def shadowghost():     # 83 그림자 유령 — 검보라 등불 속 그림자 눈 + 오른쪽으로 흩날리는 그림자 자락
    return Pic().rows([
        '..xxx....',
        '..x.x....',
        '.xjXjx...',
        'XzzzzzX..',
        'XzJzJzXZ.',
        'XzzZzzXzZ',
        'XjzzzjXz.',
        '.xxxxx...',
    ]), (3, 4)


# 번호 · 이름 · 기본 무기 · 그림 함수
KITS = [
    (20, '전쟁 망치', 'sword', hammer), (21, '채찍검', 'sword', whip), (22, '기창', 'sword', lance),
    (28, '독침 대롱', 'shuriken', blowgun), (29, '도박 카드', 'shuriken', cards), (30, '살상 와이어', 'shuriken', wire),
    (36, '그물 활', 'bow', netbow), (37, '투창', 'bow', javelin), (38, '속사 활', 'bow', burstbow),
    (44, '수은 구슬', 'flask', quicksilver), (45, '자석 폭탄', 'flask', magnet), (46, '폭죽 발사기', 'flask', firework),
    (60, '측량 사슬', 'quill', surveychain), (61, '잉크병', 'quill', inkpot), (62, '지구본', 'quill', globe),
    (67, '뻐꾸기 시계', 'gear', cuckoo), (68, '모래시계', 'gear', hourglass), (69, '괘종시계', 'gear', grandfather),
    (74, '비둘기 우편', 'letter', pigeon), (75, '소포 상자', 'letter', parcel), (76, '우체통', 'letter', postbox),
    (81, '사슬 망령', 'lantern', soulchain), (82, '얼음 유령', 'lantern', iceghost), (83, '그림자 유령', 'lantern', shadowghost),
]


def write_meta(path, base, pivot):
    meta = path + '.meta'
    if os.path.exists(meta):
        text = open(meta, encoding='utf-8').read()
    else:
        text = open(os.path.join(WEAP, 'weapon_%s.png.meta' % base), encoding='utf-8').read()
        text = re.sub(r'^guid: [0-9a-f]+', 'guid: ' + uuid.uuid4().hex, text, count=1, flags=re.M)
        text = re.sub(r'spriteID: [0-9a-f]+', 'spriteID: ' + uuid.uuid4().hex, text, count=1)
    text = re.sub(r'spritePivot: \{x: [-0-9.]+, y: [-0-9.]+\}', 'spritePivot: {x: %.3f, y: %.3f}' % pivot, text, count=1)
    with open(meta, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)


def build():
    made = []
    for kid, name, base, fn in KITS:
        pic, (hx_, hy_) = fn()
        pic.outline()
        im, x0, y0 = pic.image()
        # 손잡이 칸 → pivot (Unity 는 아래가 0)
        px = (hx_ - x0 + 0.5) / im.w
        py = 1.0 - (hy_ - y0 + 0.5) / im.h
        path = os.path.join(WEAP, 'weapon_kit_%d.png' % kid)
        png.write(im, path)
        write_meta(path, base, (round(px, 3), round(py, 3)))
        made.append((kid, name, base, im, (px, py)))
        print('weapon_kit_%d  %-8s %2d×%-2d  pivot (%.3f, %.3f)' % (kid, name, im.w, im.h, px, py))
    return made


def sheet(made, out):
    # 영웅마다 한 줄: 기본 무기 | 진화 무기 셋 (8배) — 피벗 자리에 작은 점
    S, CELL = 8, 30 * 8
    rows = [made[i:i + 3] for i in range(0, len(made), 3)]
    sh = Img(CELL * 4, CELL * len(rows), (62, 58, 82, 255))
    for r, group in enumerate(rows):
        items = [(png.read(os.path.join(WEAP, 'weapon_%s.png' % group[0][2])), None)] + [(m[3], m[4]) for m in group]
        for c, (im, piv) in enumerate(items):
            ox, oy = c * CELL + (CELL - im.w * S) // 2, r * CELL + (CELL - im.h * S) // 2
            for y in range(im.h):
                for x in range(im.w):
                    col = im.px[y][x]
                    if col[3]:
                        sh.rect(ox + x * S, oy + y * S, S, S, col)
            if piv:
                cx, cy = ox + int(piv[0] * im.w * S), oy + int((1 - piv[1]) * im.h * S)
                sh.rect(cx - 1, cy - 1, 3, 3, (255, 60, 220, 255))
    png.write(sh, out)


if __name__ == '__main__':
    made = build()
    if len(sys.argv) > 1:
        sheet(made, sys.argv[1])
