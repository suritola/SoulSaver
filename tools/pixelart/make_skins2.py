# 스킨 도트 2 (2.0.0~): 새 영웅 넷(지도 제작자 · 시계공 · 우체부 · 유령)의 캐릭터 스킨 16 · 무기 스킨 8
#   캐릭터: Assets/Resources/Characters/<영웅>__<스킨>.png — 기본 시트(192x32 = 6프레임)를 읽어 색 바꾸기 + 덧그림
#   무기:   Assets/Resources/Weapons/weapon_<무기>__<스킨>.png — 기본 무기와 같은 크기 · 같은 잡는 자리(메타 복사)
#   덧그림 좌표: x = 칸 안 가로 자리 · y = 「그 프레임 머리 꼭대기 줄」 기준 (rel) → 몸이 위아래로 흔들려도 같이 따라감
# 실행: make_skins.py 가 부름 (python tools/pixelart/make_skins.py) · 이 파일만 돌려도 됨
import colorsys, math, os, re, sys, uuid
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(__file__))
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
CHARS = os.path.join(ROOT, 'Assets', 'Resources', 'Characters')
WEAP = os.path.join(ROOT, 'Assets', 'Resources', 'Weapons')
OUT = (0x1A, 0x11, 0x2C, 255)           # 모든 캐릭터 공통 윤곽선 색


def hx(s):
    s = s.lstrip('#')
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16), 255)


def luma(c):
    return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]


def retry(fn):
    import time
    for _ in range(20):
        try:
            return fn()
        except PermissionError:
            time.sleep(0.5)
    return fn()


def write_png(arr, path):
    retry(lambda: Image.fromarray(arr.astype(np.uint8), 'RGBA').save(path))


def old_guid(path):
    if os.path.exists(path):
        m = re.search(r'guid: ([0-9a-f]{32})', open(path, encoding='utf-8').read())
        if m:
            return m.group(1)
    return uuid.uuid4().hex


def sheet_meta(base_meta, base_name, new_name, dst):
    """기본 시트 .meta 를 복사 — 스프라이트 이름만 새 이름으로 · 아이디는 새로 (다시 돌려도 guid 유지)"""
    s = open(base_meta, encoding='utf-8').read()
    s = s.replace(base_name + '_', new_name + '_')
    s = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + old_guid(dst), s, count=1)
    # 스프라이트 아이디도 이름으로 정한 값 (다시 돌려도 같음 · 다른 시트와 겹치지 않음)
    n = [0]

    def rep(m):
        n[0] += 1
        return 'spriteID: ' + uuid.uuid5(uuid.NAMESPACE_DNS, new_name + str(n[0])).hex
    s = re.sub(r'spriteID: [0-9a-f]{32}', rep, s)
    retry(lambda: open(dst, 'w', encoding='utf-8', newline='\n').write(s))


# ------------------------------------------------------------------ 프레임 편집기
class F:
    """한 프레임 (32x32 RGBA) 편집 — y 는 머리 꼭대기 줄 기준 (rel)"""

    def __init__(self, arr, fi, top0):
        self.a = arr.copy()
        self.fi = fi
        alpha = arr[:, :, 3] > 0
        self.top = int(np.argmax(alpha.any(axis=1)))
        self.dy = self.top - top0           # 기본(0번) 프레임보다 몸이 내려간 만큼
        self.added = np.zeros(arr.shape[:2], bool)
        self.orig = arr.copy()

    def Y(self, rel):
        return self.top + rel

    def px(self, x, rel):
        y = self.top + rel
        return tuple(int(v) for v in self.a[y, x]) if 0 <= x < 32 and 0 <= y < 32 else (0, 0, 0, 0)

    def put(self, x, rel, c, behind=False, mark=True):
        y = self.top + rel
        if not (0 <= x < 32 and 0 <= y < 32):
            return
        if isinstance(c, str):
            c = hx(c)
        if behind and self.a[y, x, 3] > 0:
            return
        self.a[y, x] = c
        if mark:
            self.added[y, x] = True

    def art(self, x0, rel0, rows, legend, behind=False, mark=True):
        for j, row in enumerate(rows):
            for i, ch in enumerate(row):
                if ch in '. ':
                    continue
                self.put(x0 + i, rel0 + j, legend[ch], behind, mark)

    def clear(self, x, rel):
        y = self.top + rel
        if 0 <= x < 32 and 0 <= y < 32:
            self.a[y, x] = (0, 0, 0, 0)

    def recolor(self, cmap, r0=-99, r1=99):
        """r0~r1 줄 (rel, 포함) 안의 색을 바꿈 — cmap = {원래 hex: 새 hex}"""
        m = {hx(k)[:3]: hx(v) for k, v in cmap.items()}
        for y in range(32):
            rel = y - self.top
            if rel < r0 or rel > r1:
                continue
            for x in range(32):
                p = self.a[y, x]
                if p[3] and tuple(int(v) for v in p[:3]) in m:
                    self.a[y, x] = m[tuple(int(v) for v in p[:3])]

    def map_all(self, fn):
        """모든 불투명 화소에 fn(x, rel, (r,g,b)) → 새 색 또는 None"""
        for y in range(32):
            for x in range(32):
                p = self.a[y, x]
                if p[3]:
                    c = fn(x, y - self.top, tuple(int(v) for v in p[:3]))
                    if c is not None:
                        self.a[y, x] = hx(c) if isinstance(c, str) else (c[0], c[1], c[2], 255)

    def outline(self, color=OUT):
        """덧그린 화소 둘레(투명한 곳)에 윤곽선"""
        if isinstance(color, str):
            color = hx(color)
        add = self.added.copy()
        for y in range(32):
            for x in range(32):
                if not add[y, x]:
                    continue
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < 32 and 0 <= ny < 32 and self.a[ny, nx, 3] == 0:
                        self.a[ny, nx] = color

    def alpha_mask(self):
        return self.a[:, :, 3] > 0


def frames_of(name):
    im = np.asarray(Image.open(os.path.join(CHARS, name + '.png')).convert('RGBA'))
    cells = [im[:, i * 32:(i + 1) * 32] for i in range(im.shape[1] // 32)]
    top0 = int(np.argmax((cells[0][:, :, 3] > 0).any(axis=1)))
    return [F(c, i, top0) for i, c in enumerate(cells)]


# ------------------------------------------------------------------ 지도 제작자 (머리 꼭대기 y13)
#   모자 rel 0~5 · 얼굴 rel 6~7 · 겉옷 rel 8~13 · 다리 rel 14~18
#   모자 b 865436 k 563432 r 5C342A D 6F4129 I B07C4C L 7B4D51 · 띠 q D03C3A v 941E34 J F46E56
#   겉옷 c 304CA8 d 1C1F54 i 1E2A74 A 5278D6 N 5A86C9 x 709ED8 · 금 f EAAA3A C B46826 P FEDE6C · 두루마리 o FAEEC6 t E8D09C F CCD8C2 M AFAF9F
HAT = ('865436', '563432', '5C342A', '6F4129', 'B07C4C', '7B4D51')


def hatmap(b, k, r, d, i, l, band=None):
    m = dict(zip(HAT, (b, k, r, d, i, l)))
    if band:
        m.update({'D03C3A': band[0], '941E34': band[1], 'F46E56': band[2]})
    return m


def carto_drafter(f):          # 제도사 (일반): 카키 조끼 · 올리브 모자 · 등에 멘 도면통
    f.recolor(hatmap('7C7C48', '56562F', '44441F', '56562F', 'B4B478', '56562F', ('E8A33A', 'B3731F', 'F5C876')), 0, 5)
    f.recolor({'304CA8': 'C9B27A', '1C1F54': '8A7442', '1E2A74': '6E5C33', '5278D6': 'E4D3A0', '5A86C9': 'D6C28A',
               'EAAA3A': '6F8F52', 'B46826': '4B6A38', 'FEDE6C': 'A9C58A'}, 8, 14)
    f.art(8, 6, ['cC', 'cC', 'cC', 'cC', 'cC', 'cC', 'cC'], {'c': hx('E8DCB0'), 'C': hx('9A8650')}, behind=True)
    f.outline()


def carto_hydro(f):            # 해도사 (희귀): 남색 외투 · 흰 띠 선장 모자(양끝이 올라감) · 육분의 · 바다색 해도
    f.recolor(hatmap('2F4A7A', '1E3258', '1B2B4C', '1E3258', '6C8CC0', '1E3258', ('F2F2F2', 'B8C4D4', 'FFFFFF')), 0, 5)
    f.recolor({'304CA8': '1D4A6E', '1C1F54': '0F2A44', '1E2A74': '163A58', '5278D6': '2F7BA0', '5A86C9': '3B8FB5',
               'FAEEC6': 'BFE8E0', 'E8D09C': '8CC8C8', 'CCD8C2': '6AA8B0', 'AFAF9F': '5E8A98'}, 8, 14)
    f.put(8, 3, '2F4A7A'); f.put(22, 3, '2F4A7A'); f.put(8, 2, '1E3258'); f.put(22, 2, '1E3258')
    f.art(5, 10, ['..EE.', '.EwwE', 'EwwwB', 'EBBBB'], {'E': hx('E0B04A'), 'B': hx('8A6420'), 'w': hx('BFF0FF')})
    f.outline()


def carto_starmap(f):          # 별지도꾼 (영웅): 밤하늘 망토 · 별을 꽂은 뾰족 모자 · 별자리 두루마리
    f.recolor(hatmap('3A2A6E', '2A1C52', '231646', '2A1C52', '8A78D0', '2A1C52', ('F2D860', 'B8962C', 'FFF0A0')), 0, 5)
    f.recolor({'304CA8': '2B2F7A', '1C1F54': '171A4A', '1E2A74': '1E2260', '5278D6': '5A64C8', '5A86C9': '6874D8',
               'EAAA3A': 'F2D860', 'B46826': 'C9A22E', 'FEDE6C': 'FFF3A8',
               'FAEEC6': '4A5AB0', 'E8D09C': '3A4A90', 'CCD8C2': '2E3A78', 'AFAF9F': '2A3470'}, 8, 14)
    L = {'c': hx('3A2A6E'), 'C': hx('241848'), 'Y': hx('F2D860'), 'W': hx('FFF8C8'), 's': hx('1E2260'), 'S': hx('141845')}
    f.art(13, -2, ['.cc.', 'cccc'], L)
    f.art(14, -5, ['W', 'Y'], L)
    f.art(13, -4, ['.Y.', 'YWY', '.Y.'][1:2], L)
    # 어깨 뒤로 흘러내린 밤하늘 망토 (양옆)
    for rel in range(8, 17):
        for x in (8, 9, 22, 23):
            f.put(x, rel, 'S' if False else ('1E2260' if x in (9, 22) else '141845'), behind=True)
    for (x, r) in ((9, 10), (23, 12), (8, 14), (22, 15), (9, 8)):
        f.put(x, r, 'F2D860')
    f.outline()


def paper_body(f, burn_phase):
    # 흰 종이 몸: 칸마다 명도 따라 종이색 · 안쪽 경계는 잉크 선 · 윤곽선은 그대로
    L = f.orig
    H = {}

    def lum_at(x, y):
        p = L[y, x] if 0 <= x < 32 and 0 <= y < 32 else (0, 0, 0, 0)
        return luma(p) if p[3] else None

    def fn(x, rel, c):
        y = f.top + rel
        l = luma(c)
        if c in ((0x1A, 0x11, 0x2C), (0x12, 0x0B, 0x20)):
            return None                                        # 윤곽선 · 눈
        # 안쪽 경계 (이웃과 명도 차 큼) → 잉크 선
        for dx, dy in ((1, 0), (0, 1)):
            q = lum_at(x + dx, y + dy)
            if q is not None and abs(q - l) > 70 and l < 120:
                return '4A5A8A'
        if c == (0xD0, 0x3C, 0x3A) or c == (0x94, 0x1E, 0x34) or c == (0xF4, 0x6E, 0x56):
            return c                                           # 빨간 모자 띠 (교정 펜)
        if l > 170:
            base = 'FAF6EA'
        elif l > 120:
            base = 'EAE2CE'
        elif l > 80:
            base = 'D6CCB2'
        else:
            base = 'BFB59A'
        # 모눈 (설계 용지)
        if 8 <= rel <= 14 and (x % 3 == 0 or rel % 3 == 0):
            return 'C4D6E2' if base != 'BFB59A' else 'AFC2D0'
        return base
    f.map_all(fn)


def carto_blank(f):            # 백지도의 저자 (전설): 종이로 된 몸 · 모눈 · 빨간 교정 펜 · 모서리부터 타들어 감
    paper_body(f, 0)
    # 교정 펜 (오른손): 빨간 몸통 + 흰 촉
    for i, (x, r) in enumerate(((22, 9), (23, 8), (24, 7), (25, 6))):
        f.put(x, r, 'D03C3A' if i else 'F4F4FA')
    f.put(26, 5, '8A1C28'); f.put(22, 10, 'F4F4FA')
    # 빨간 X 교정 표시 (가슴)
    for (x, r) in ((13, 9), (14, 10), (15, 11), (16, 12), (17, 9), (16, 10), (14, 12), (13, 13)):
        f.put(x, r, 'D03C3A')
    # 모자 오른쪽 모서리가 타오름 (프레임마다 모양이 바뀜)
    fl = [['.o.', 'oYo', 'oRo'], ['o..', 'YoY', 'RRo'], ['.oo', 'oYo', 'RoR']][f.fi % 3]
    f.art(19, -3, fl, {'o': hx('F08A28'), 'Y': hx('FFE066'), 'R': hx('C8301C')}, mark=False)
    for (x, r) in ((19, 0), (20, 1), (21, 2)):
        f.put(x, r, '5A3A28')
    # 발끝이 그을림
    for x in range(8, 24):
        for rel in (17, 18):
            p = f.px(x, rel)
            if p[3] and (x + rel + f.fi) % 3 == 0:
                f.put(x, rel, '4A3A30', mark=False)
    f.outline()


# ------------------------------------------------------------------ 시계공 (머리 꼭대기 y13)
#   머리카락 rel 0~4 · 얼굴 rel 4~8 · 조끼 rel 8~12 · 허리띠 rel 13 · 다리 rel 14~18
#   머리 c FFE389 j E2B868 s C8C9A1 e 513831 · 조끼 d 7C4E2C n 572714 f 3C1D1F k 56322E · 셔츠 h FFFAE9 · 톱니 w DA9C3C
def clock_apprentice(f):       # 견습 (일반): 회색 가죽 앞치마 · 쇠 톱니 · 이마에 그을음
    f.recolor({'7C4E2C': '5E6272', '572714': '3F4252', '3C1D1F': '2C2E3C', '56322E': '3A3C4A', 'DA9C3C': '9AA4B4',
               'A06428': '7A8294'}, 8, 14)
    f.art(12, 9, ['SSSSSSSS', 'aaaaaaaa', 'aPPPPPPa', 'aPaaaaPa', 'aPPPPPPa', 'aaaaaaaa'], {'S': hx('3A3C4A'), 'a': hx('A4AABC'), 'P': hx('6E7488')}, mark=False)
    f.art(20, 11, ['.gg.', 'gGGg', 'gGGg', '.gg.'], {'g': hx('7A8294'), 'G': hx('C0C8D8')})
    f.put(21, 12, '3A3C4A'); f.put(22, 13, '3A3C4A')
    f.outline()


def clock_hourglass(f):        # 모래시계 수도사 (희귀): 모래빛 후드 · 로브 · 밧줄 허리띠 · 모래시계 목걸이
    f.recolor({'FFE389': 'C8A870', 'E2B868': '9E8250', 'C8C9A1': '9E8250', '513831': '5E4A2A'}, 0, 5)
    f.recolor({'7C4E2C': 'B89A62', '572714': '8E7444', '3C1D1F': '6A5028', '56322E': '6A5028', 'FFFAE9': 'E8DCC0',
               'DA9C3C': 'B89A62', 'A06428': 'D8C89A', '5A728F': '6A5028', '484264': '8E7444', '302C4A': '6A5028',
               '26203C': '4A381C', '341A22': '4A381C', '563432': '4A381C', '572714': '8E7444'}, 8, 18)
    for rel in range(3, 9):                                    # 후드 안쪽 그늘이 얼굴 양옆을 감쌈
        f.put(11, rel, '8E7444'); f.put(20, rel, '8E7444')
    for rel in (8, 9):
        for x in (10, 11, 20, 21):
            f.put(x, rel, 'B89A62');
    f.art(14, 10, ['hhh', 'gSg', '.S.', 'hhh'], {'h': hx('6A4A28'), 'g': hx('D8F0FF'), 'S': hx('E8C060')})
    f.outline()


def clock_cuckoo(f):           # 뻐꾸기 시계 (영웅): 등에 멘 뻐꾸기 시계 집 · 초록 알프스 모자 · 숲빛 조끼
    f.recolor({'7C4E2C': '2F5A38', '572714': '20402A', '3C1D1F': '16281C', '56322E': '16281C'}, 8, 13)
    G = {'g': hx('2A5030'), 'G': hx('3E7A44'), 'l': hx('5FA05A'), 'r': hx('C0392B'), 'w': hx('F4F0E0'), 'W': hx('D8C4A0')}
    f.art(13, -2, ['gGGg'], G)
    f.art(12, -1, ['gGlGGg'], G)
    f.art(11, 0, ['gGGlGGGg'], G)
    f.art(11, 1, ['rrrrrrrr'], G)
    f.art(10, 2, ['gGGGGGGGGg'], G)
    f.art(20, -3, ['.w', 'ww', 'rw'][0:3], G)
    H = {'R': hx('6A3A24'), 'r': hx('A8683C'), 'W': hx('8A5A2E'), 'w': hx('5A3A1C'), 'c': hx('F2E6C0'), 'h': hx('A06428'),
         'D': hx('3A2410'), 'Y': hx('FFD84A'), 'b': hx('FF7A3A')}
    f.art(5, 5, ['..rr..', '.rRRr.', 'rRRRRr', '.WWWW.', '.WcchW', '.WccWW', '.WDDWW', '.WDYWW', 'wwwwww'], H, behind=True)
    f.put(7, 14, 'EAAA3A'); f.put(7, 15, 'EAAA3A'); f.put(8, 14, '8A8A7A'); f.put(8, 15, '8A8A7A')
    for (x, r) in ((11, 8), (11, 9), (12, 10), (12, 11), (13, 12), (13, 13), (19, 8), (19, 9), (18, 10), (18, 11), (17, 12), (17, 13)):
        f.put(x, r, '5A3A1C', mark=False)                       # 어깨 멜빵
    f.outline()


def clock_stopped(f):          # 멈춘 시계의 주인 (전설): 흑백 몸 · 머리 뒤 깨진 시계판(11:59) · 깨진 유리 톱니
    def gray(x, rel, c):
        l = luma(c)
        if c in ((0x1A, 0x11, 0x2C), (0x12, 0x0B, 0x20)):
            return None
        if c == (0xDA, 0x9C, 0x3C):
            return '9FD8E0'                                    # 깨진 유리 톱니
        v = int(l * 0.9 + 10)
        return '%02X%02X%02X' % (min(255, v), min(255, int(v * 1.0)), min(255, v + 6))
    f.map_all(gray)
    cx, cy, R = 15.5, -1.5, 7.2
    ring, face, tick = hx('2A2A34'), hx('F0E6C8'), hx('2A2A34')
    dial = {}
    for y in range(-10, 8):
        for x in range(7, 25):
            d = math.hypot(x - cx, y - cy)
            if d <= R:
                dial[(x, y)] = ring if d > R - 1.1 else face
    for k in range(12):
        a_ = k * math.pi / 6
        x, y = int(round(cx + math.sin(a_) * (R - 1.9))), int(round(cy - math.cos(a_) * (R - 1.9)))
        dial[(x, y)] = tick

    def hand(ang, ln, col):
        for t in range(0, ln + 1):
            dial[(int(round(cx + math.sin(ang) * t)), int(round(cy - math.cos(ang) * t)))] = hx(col)
    hand(math.radians(-30), 3, '2A2A34')           # 시침 11
    hand(math.radians(-6), 4, 'B01828')            # 분침 59
    for (x, r) in ((20, -8), (19, -7), (19, -6), (18, -5), (18, -4), (17, -3)):       # 금
        dial[(x, r)] = hx('8A6A5A')
    for (x, r) in ((21, -7), (22, -6), (22, -5), (21, -6), (21, -5), (20, -9), (21, -8)):   # 깨져 나간 조각
        dial.pop((x, r), None)
    for (x, r), c in dial.items():
        f.put(x, r, c, behind=True)
    for (x, r) in ((24, -6), (25, -4), (24, -2), (6, -7), (5, -5)):
        f.put(x, r, 'BFF0F8')
    f.outline()


# ------------------------------------------------------------------ 우체부 (머리 꼭대기 y14)
#   모자 rel 0~3 · 얼굴 rel 4~7 · 제복 rel 8~12 · 가방 rel 12~14 · 다리 rel 15~
#   모자 c BE2836 B F2504E u 470E29 f 261C3C(챙) o EAAA3A(배지) · 제복 d 2C428C k 4A6EB5 g 16164A m 121A62 z 5780B8 x 171543
#   가방 e 865632 r 5E2C17 i 3F2020 s 76726F G B46826
def post_paperboy(f):          # 신문 배달 (일반): 파란 야구 모자 · 겨자색 점퍼 · 둘둘 만 신문
    f.recolor({'BE2836': '2B5BA8', 'F2504E': '5C8AD8', '470E29': '1C3A74', '261C3C': '14264C', 'EAAA3A': 'F2F2F2',
               'FEDE6C': 'DCDCE8'}, 0, 3)
    f.recolor({'2C428C': 'C8902A', '4A6EB5': 'E8B858', '16164A': '8A5E18', '121A62': '6A4510', '5780B8': 'F4D080',
               '171543': '5A3C10'}, 8, 13)
    P = {'p': hx('F0ECD8'), 'g': hx('B8B4A0'), 'd': hx('7A7660')}
    f.art(19, 10, ['.ppppdd.', 'pgggpdPd', 'pppgpdPd', '.ppppdd.'], dict(P, P=hx('FFFBEA')))
    f.outline()


def post_pigeon(f):            # 비둘기 조련사 (희귀): 올리브 베레 · 잿빛 외투 · 모자 위의 비둘기
    f.recolor({'BE2836': '6A7A4A', 'F2504E': '8A9A66', '470E29': '3E4A2A', '261C3C': '2E3820'}, 0, 3)
    f.recolor({'2C428C': '5A6478', '4A6EB5': '8A94A8', '16164A': '343C4E', '121A62': '2A3244', '5780B8': 'A8B2C4',
               '171543': '20283A'}, 8, 13)
    B = {'b': hx('8A94A8'), 'B': hx('B4BED0'), 'g': hx('3E8C7A'), 'o': hx('E8A030'), 'e': hx('C02030'), 'd': hx('5A6478')}
    f.art(13, -4, ['..gge.', '.BBBbo', 'BBBBb.', '.dbbd.', '..oo..'][0:4], B)
    f.put(14, 0, 'E8A030'); f.put(16, 0, 'E8A030')
    f.outline()


def post_courier(f):           # 택배 기사 (영웅): 형광 주황 조끼 · 갈색 모자 · 종이 상자(바코드) · 단말기
    f.recolor({'BE2836': '7A5230', 'F2504E': 'A8744A', '470E29': '4E321C', '261C3C': '34210F'}, 0, 3)
    f.recolor({'2C428C': 'F08A20', '4A6EB5': 'FFB858', '16164A': 'B85A10', '121A62': '8A3E08', '5780B8': 'FFD08A',
               '171543': '6E2E04'}, 8, 13)
    for x in range(11, 21):
        f.put(x, 10, 'D0D8E0', mark=False)                     # 반사띠
    X = {'k': hx('B88848'), 'K': hx('8E6630'), 't': hx('E8D4A0'), 'w': hx('F4F4F4'), 'b': hx('201830'),
         'T': hx('2A3050'), 'c': hx('40E0F0')}
    f.art(9, 9, ['kkkkttkkkkK', 'kwwwttkkkkK', 'kwbwttkkkkK', 'kkkkttkkkkK', 'kkkkttkkkkK', 'KKKKKKKKKKK'], X)
    f.art(20, 12, ['TT', 'cT', 'TT', 'TT'], X)
    f.outline()


def post_return(f):            # 반송 우체부 (전설): 검은 제복 · 모자 밀랍 봉인 · 가슴의 빨간 RETURN 도장
    f.recolor({'BE2836': '24202E', 'F2504E': '4A4458', '470E29': '121018', '261C3C': '0A0810', 'EAAA3A': 'D82030',
               'FEDE6C': 'F04A52'}, 0, 3)
    f.recolor({'2C428C': '1E1C28', '4A6EB5': '3A3648', '16164A': '100E18', '121A62': '0A0810', '5780B8': '4A4658',
               '171543': '0A0810', 'EAAA3A': 'B8BCCC', 'B46826': '6A6E80'}, 8, 14)
    f.recolor({'865632': '2E2430', '5E2C17': '1C1420', '3F2020': '14101A', '76726F': '5A5868', 'B46826': 'D82030'}, 11, 15)
    S = {'p': hx('F04A52'), 'R': hx('B01828'), 'w': hx('FFC0C0')}
    f.art(11, 9, ['ppppppppp', 'pRRRRRRRp', 'pRwwRwwRp', 'pRwRwwRRp', 'pRRRRRRRp', 'ppppppppp'][0:6], S)
    f.put(17, 2, 'D82030'); f.put(18, 2, 'D82030'); f.put(17, 3, 'B01828')      # 봉인 드립
    f.outline()


# ------------------------------------------------------------------ 유령 (머리 꼭대기 y12)
#   몸 a E2E4F8 · 빛 d FAFAFF · 그늘 e BEBEE2 · 윤곽 b 4A4468 / c 1A112C · 눈 f 120B20 · 눈물 g 50D6EA · 입 h 59547A
def ghost_sheet(f):            # 이불 유령 (일반): 파란 체크 이불 · 헝겊 잠옷 모자 · 가장자리 바느질
    def fn(x, rel, c):
        if c in ((0xE2, 0xE4, 0xF8), (0xBE, 0xBE, 0xE2)) and ((x // 5) % 2 == 0 and (rel // 5) % 2 == 0):
            return '7CA8E4' if c[0] == 0xE2 else '5A88C8'
        if c == (0xFA, 0xFA, 0xFF):
            return 'FFFDF0'
        return None
    f.map_all(fn)
    N = {'b': hx('5A78C8'), 'B': hx('86A0E8'), 'w': hx('FFFFFF')}
    f.art(14, -1, ['.bb', 'bbB'], N)
    f.art(16, -2, ['b', 'b'], N, mark=True)
    f.art(17, -3, ['bb'], N)
    f.art(18, -2, ['ww', 'ww'], N)
    f.outline()


def ghost_wisp(f):             # 도깨비불 (희귀): 청록 불꽃 몸 · 머리 위 불꽃 · 곁을 도는 작은 불
    f.recolor({'E2E4F8': '6FE8C8', 'BEBEE2': '2FB098', 'FAFAFF': 'C8FFF0', '4A4468': '1E7868', '1A112C': '0B2E30',
               '120B20': 'FFF2A0', '59547A': '1E7868'})
    fr = f.fi % 3
    shapes = [['.W.', 'WoW', 'ooo', 'ooo'], ['..W', '.Wo', 'WoW', 'ooo'], ['W..', 'oW.', 'WoW', 'ooo']]
    f.art(14, -4, shapes[fr], {'W': hx('E8FFF8'), 'o': hx('6FE8C8')})
    f.art(14, -4, [], {})
    dots = [((4, 6), (25, 14)), ((4, 8), (25, 12)), ((4, 10), (25, 10))][fr]
    for (x, r) in dots:
        f.art(x, r, ['.W.', 'WoW', 'ooo', '.o.'], {'W': hx('E8FFF8'), 'o': hx('6FE8C8')})
    f.outline('0B2E30')


def ghost_deepsea(f):          # 심해 유령 (영웅): 짙은 청남 몸 · 청록 발광 반점 · 낚싯대 빛 · 지느러미
    f.recolor({'E2E4F8': '1E5A78', 'BEBEE2': '143C54', 'FAFAFF': '3E8CA8', '4A4468': '0A2438', '1A112C': '06141E',
               '120B20': 'F0FFA0', '59547A': '0A2438', '50D6EA': '40E8C0'})
    for (x, r) in ((12, 6), (18, 8), (13, 10), (17, 12), (15, 9), (19, 14), (11, 13)):
        f.put(x, r, '40E8C0')
    S = {'s': hx('143C54'), 'Y': hx('F8F0A0'), 'G': hx('40E8C0')}
    f.art(15, -2, ['s', 's'], S)
    f.art(16, -4, ['ss', 's.'], S)
    f.art(18, -6, ['sG', 'GY'][0:2], S)
    f.art(17, -7, ['.G.', 'GYG', '.G.'][0:3], S, mark=False)
    f.art(8, 7, ['s.', 'ss', 's.'], S)
    f.art(23, 8, ['.s', 'ss', '.s'], S)
    f.outline('06141E')


def ghost_deleted(f):          # 삭제된 장면 (전설): 왼쪽은 그려지고 오른쪽은 마젠타 와이어프레임 · 취소선 · 빠진 텍스처
    sil = f.alpha_mask().copy()
    MAG, MAG2 = hx('FF2BD6'), hx('A81A94')
    for y in range(32):
        for x in range(16, 32):
            if not sil[y, x]:
                continue
            edge = any(not (0 <= x + dx < 32 and 0 <= y + dy < 32 and sil[y + dy, x + dx]) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            eye = tuple(f.a[y, x, :3]) in ((0x12, 0x0B, 0x20), (0x50, 0xD6, 0xEA))
            if edge:
                f.a[y, x] = MAG
            elif eye:
                f.a[y, x] = hx('201030')
            elif (x - y) % 5 == 0 or (x + y) % 6 == 0:
                f.a[y, x] = MAG2
            else:
                f.a[y, x] = (0, 0, 0, 0)
    for rel in range(5, 14):                                   # 반으로 갈리는 세로 접합선
        if f.px(15, rel)[3]:
            f.put(15, rel, 'FF9AEE')
    for x in range(6, 26):                                     # 취소선
        f.put(x, 10, 'FFD0F8')
    f.put(5, 10, 'FF2BD6'); f.put(26, 10, 'FF2BD6')
    for j in range(4):                                         # 빠진 텍스처 격자
        for i in range(4):
            f.put(19 + i, 12 + j, 'FF2BD6' if (i // 2 + j // 2) % 2 == 0 else '1A0A20', behind=False, mark=False)
    f.outline('FF2BD6')


SKINS = {
    # (기본 시트, 스킨 이름): 함수
    ('cartographer', 'drafter'): carto_drafter, ('cartographer', 'hydro'): carto_hydro,
    ('cartographer', 'starmap'): carto_starmap, ('cartographer', 'blank'): carto_blank,
    ('clockmaker', 'apprentice'): clock_apprentice, ('clockmaker', 'hourglass'): clock_hourglass,
    ('clockmaker', 'cuckoo'): clock_cuckoo, ('clockmaker', 'stopped'): clock_stopped,
    ('postman', 'paperboy'): post_paperboy, ('postman', 'pigeon'): post_pigeon,
    ('postman', 'courier'): post_courier, ('postman', 'return'): post_return,
    ('ghost', 'sheet'): ghost_sheet, ('ghost', 'wisp'): ghost_wisp,
    ('ghost', 'deepsea'): ghost_deepsea, ('ghost', 'deleted'): ghost_deleted,
}


def char_skins():
    for (base, name), fn in SKINS.items():
        sid = base + '__' + name
        frames = frames_of(base)
        for f in frames:
            fn(f)
        sheet = np.concatenate([f.a for f in frames], axis=1)
        out = os.path.join(CHARS, sid + '.png')
        sheet_meta(os.path.join(CHARS, base + '.png.meta'), base, sid, out + '.meta')
        write_png(sheet, out)
        print('wrote', sid)


# ------------------------------------------------------------------ 무기 (기본과 같은 크기 · 같은 메타)
def ascii_img(rows, legend):
    h, w = len(rows), len(rows[0])
    a = np.zeros((h, w, 4), np.uint8)
    for y, r in enumerate(rows):
        assert len(r) == w, (rows, y)
        for x, ch in enumerate(r):
            if ch != '.':
                a[y, x] = hx(legend[ch])
    return a


def recolor_img(base, cmap):
    a = np.asarray(Image.open(os.path.join(WEAP, 'weapon_%s.png' % base)).convert('RGBA')).copy()
    m = {hx(k)[:3]: hx(v) for k, v in cmap.items()}
    for y in range(a.shape[0]):
        for x in range(a.shape[1]):
            if a[y, x, 3] and tuple(int(v) for v in a[y, x, :3]) in m:
                a[y, x] = m[tuple(int(v) for v in a[y, x, :3])]
    return a


def weapon_art():
    O = '1A112C'
    W = {}
    # 붓: 대나무 자루 · 은 물미 · 먹 묻은 털
    W['quill__brush'] = ('quill', recolor_img('quill', {
        '59516E': '6B3F22', 'F4F4FA': 'C8924E', 'C6DFF4': 'E0B070', 'DEDCEE': 'D8A860', 'A9C0DE': 'A8743A',
        '5B3629': 'C8C8D0', '1E2661': '1C1C2C', 'FEDE6C': '3A3A50', '8CC4FF': '24305A'}))
    # 분필: 흰 막대 · 분홍 종이 감싼 끝
    W['quill__chalk'] = ('quill', recolor_img('quill', {
        '59516E': 'D0D0DA', 'F4F4FA': 'FFFFFF', 'C6DFF4': 'F4F4F8', 'DEDCEE': 'E4E4EC', 'A9C0DE': 'C0C0CE',
        '5B3629': 'E86A8A', '1E2661': 'C84870', 'FEDE6C': 'FFB0C8', '8CC4FF': 'F0F0F8'}))
    # 금화
    W['gear__coin'] = ('gear', ascii_img([
        '..ccccc..', '.cdbbbfc.', 'cdbbbbbfc', 'cbbgggbfc', 'cbbgdgbff', 'cbbgggffc', 'cbbbbbffc', '.cfiiifc.', '..ccccc..'],
        {'c': O, 'd': 'FFFAD2', 'b': 'FFE06A', 'f': 'E0A030', 'i': 'B46826', 'g': '9A6A1C'}))
    # 주판알: 윤나는 갈색 알 + 꿰는 막대
    W['gear__abacus'] = ('gear', ascii_img([
        '.........', '....c....', '...cdc...', '..cdbbc..', 'ccdbffocc', '..cbfoc..', '...cfc...', '....c....', '.........'],
        {'c': O, 'd': 'F0B888', 'b': 'C07A48', 'f': '8A4E2A', 'o': '5E3018'}))
    # 종이비행기
    W['letter__plane'] = ('letter', ascii_img([
        '...c.....', '..cdcc...', 'ccdddbcc.', '.ccdbbbcc', '..ccbbcc.', '....cc...'],
        {'c': O, 'd': 'FFFFFF', 'b': 'C8D4EC'}))
    # 청구서: 빨간 머리띠 · 글줄 · 「연체」 도장
    W['letter__bill'] = ('letter', ascii_img([
        '.ccccccc.', 'cRRRRRRRc', 'cwgggwwwc', 'cwwwwgRRc', 'cwgggwRRc', '.ccccccc.'],
        {'c': O, 'R': 'E23654', 'w': 'FFFAEA', 'g': '8A8A9A'}))
    # 촛대: 초 + 불꽃 + 금 받침
    W['lantern__candle'] = ('lantern', ascii_img([
        '...y...', '..yoy..', '..cwc..', '..cwc..', '..cwc..', '.ccGcc.', '..cGc..', '.cGGGc.'],
        {'c': O, 'y': 'FFE066', 'o': 'F08A28', 'w': 'FFFAEA', 'G': 'E0A030'}))
    # 쇠사슬 등불: 쇠 틀 · 따뜻한 불빛 · 둘러 감은 사슬
    W['lantern__chain'] = ('lantern', recolor_img('lantern', {
        '205179': '3A3E48', '1E4267': '2A2E38', '9EA6C6': '7A8090', 'B0F8FF': 'FFD98A', 'F2FFFF': 'FFF4C8',
        '9EBEDC': '8A90A0', '7895B9': '5A6070', '50D6EA': 'F09A30', '2486B8': 'C86A1C'}))
    a = W['lantern__chain'][1]
    for x, col in zip(range(0, 7), ['1A112C', 'B0B6C8', '1A112C', 'B0B6C8', '1A112C', 'B0B6C8', '1A112C']):
        a[4, x] = hx(col)
    a[0, 3] = hx('B0B6C8'); a[1, 3] = hx('1A112C')
    for sid, (base, arr) in W.items():
        out = os.path.join(WEAP, 'weapon_%s.png' % sid)
        src = os.path.join(WEAP, 'weapon_%s.png.meta' % base)
        s = open(src, encoding='utf-8').read()
        s = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + old_guid(out + '.meta'), s, count=1)
        retry(lambda: open(out + '.meta', 'w', encoding='utf-8', newline='\n').write(s))
        write_png(arr, out)
        print('wrote weapon', sid)


def run():
    char_skins()
    weapon_art()


if __name__ == '__main__':
    run()
