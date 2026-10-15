# 초월 카드 「새 규칙」 그림 (1.2~, LevelShop.Transcend · TranscendRules): 32x32
#   금테 메달(위 왼쪽이 밝은 3단 금빛 테 + 짙은 영혼빛 안쪽) 안에 규칙마다 상징, 남보라 외곽선
#   → Assets/Resources/Icons/choice_tr_<이름>.png (ChoiceUI 는 choice_ 를 붙여 읽음)
# 실행: python tools/pixelart/make_transcend_icons.py
import math, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line
from make_menu_icons import OUT, retry

K = (26, 17, 44, 255)                   # 남보라 외곽선 (새 그림체와 같은 색)
GL = (255, 236, 160, 255); G = (255, 200, 86, 255); GM = (234, 170, 58, 255); GD = (160, 98, 34, 255)
IN1 = (62, 50, 104, 255); IN2 = (44, 34, 80, 255); IN3 = (30, 22, 56, 255)
W = (255, 255, 255, 255)
BL = (196, 240, 255, 255); B = (104, 214, 255, 255); BD = (52, 120, 200, 255)
PL = (220, 190, 255, 255); P = (170, 110, 250, 255); PD = (100, 56, 170, 255)
GRL = (210, 255, 190, 255); GR = (110, 230, 120, 255); GRD = (40, 140, 80, 255)
OL = (255, 220, 140, 255); O = (255, 150, 60, 255); OD = (200, 70, 40, 255)
BR = (150, 96, 56, 255); BRD = (96, 58, 34, 255)
CX = CY = 16.0


def medallion(img):
    # 테: 반지름 12.4~15, 위 왼쪽이 밝게 (빛 방향 -135도)
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - CX, y + 0.5 - CY
            d = math.hypot(dx, dy)
            if d > 15.0:
                continue
            if d >= 12.4:
                lit = (-dx - dy) / (d * 1.414)          # 1 = 위 왼쪽
                c = GL if lit > 0.75 else G if lit > 0.15 else GM if lit > -0.6 else GD
                if 13.1 < d < 13.9 and lit < 0.6:       # 테 가운데 홈
                    c = GM if c in (G, GL) else GD
                img.set(x, y, c)
            else:
                k = (dy + d * 0.3) / 12.4               # 아래 · 바깥으로 갈수록 어둡게
                img.set(x, y, IN1 if k < -0.25 else IN2 if k < 0.45 else IN3)
    # 테 위 반짝
    img.set(8, 6, W); img.set(7, 7, W); img.set(9, 5, GL)


def glint(img, x, y):
    img.set(x, y, W)


def echo(img):
    # 가운데 영혼에서 퍼지는 메아리 고리 셋 (안쪽일수록 밝게)
    ring(img, 16, 16, 10.0, 1.4, GD)
    ring(img, 16, 16, 7.2, 1.4, GM)
    ring(img, 16, 16, 4.6, 1.4, G)
    ring(img, 16, 16, 4.6, 0.5, GL, gaps=1, rot=3.6)
    disc(img, 16, 16, 2.3, GL)
    disc(img, 15.4, 15.4, 0.9, W)


def reap(img):
    # 낫 (크게 휜 날 + 자루)
    line(img, 9, 26, 18, 8, BR, 2.0)
    line(img, 9.5, 25, 17.5, 9, (190, 130, 80, 255), 0.8)
    for y in range(32):
        for x in range(32):
            d1 = math.hypot(x + 0.5 - 17, y + 0.5 - 13)
            d2 = math.hypot(x + 0.5 - 20, y + 0.5 - 17.5)
            if d1 <= 9 and d2 > 7.4 and y < 16 and x > 15:
                img.set(x, y, PL if d1 > 7.9 else P if d1 > 6.3 else PD)
    line(img, 17, 7, 18, 9, PD, 1.2)
    disc(img, 23, 7, 0.9, W)


def rift(img):
    # 모래시계 + 갈라진 틈
    for y in range(8, 25):
        t = abs(y - 16) / 8.0
        half = 1 + 6 * t
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, BL if x < 16 - half * 0.3 else B if x < 16 + half * 0.4 else BD)
    for x in range(8, 25):
        img.set(x, 7, G); img.set(x, 25, G)
        img.set(x, 6, GD); img.set(x, 26, GD)
    for y in range(9, 12):                          # 위 모래
        pass
    for y in range(19, 24):
        half = (y - 18) * 1.1
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, G)
    line(img, 18, 9, 15, 14, W, 0.9)                # 틈
    line(img, 15, 14, 17.5, 17, W, 0.9)
    line(img, 17.5, 17, 15, 22, W, 0.9)


def alchemy(img):
    # 코인이 녹색 플라스크로
    disc(img, 11, 12, 4.2, G)
    ring(img, 11, 12, 4.2, 1.0, GM)
    disc(img, 10, 11, 1.4, GL)
    for y in range(13, 26):                         # 플라스크 몸
        half = 2 if y < 17 else min(7, 2 + (y - 17) * 1.4)
        for x in range(int(20 - half), int(20 + half) + 1):
            img.set(x, y, (200, 220, 235, 255) if y < 19 else GR if x < 20 + half * 0.3 else GRD)
    for x in range(14, 27):
        if img.get(x, 19)[3]: img.set(x, 19, GRL)
    img.rect(18, 11, 5, 2, BR)
    disc(img, 18, 22, 1.0, W)
    for x, y in ((24, 10), (26, 14), (22, 8)): img.set(x, y, GRL)


def shadow(img):
    def figure(ox, oy, light, mid, dark):
        disc(img, ox, oy, 3.3, mid)                 # 머리
        for y in range(int(oy + 3), int(oy + 12)):
            half = 2.5 + (y - oy - 3) * 0.45
            for x in range(int(ox - half), int(ox + half) + 1):
                img.set(x, y, light if x < ox - half * 0.4 else mid if x < ox + half * 0.4 else dark)
        disc(img, ox - 1, oy - 1, 1.1, light)
    figure(20.5, 9.5, PD, (70, 40, 120, 255), (46, 26, 84, 255))
    figure(12.5, 11, (255, 250, 230, 255), (220, 214, 236, 255), (160, 150, 190, 255))
    disc(img, 21.5, 9, 0.8, P); disc(img, 19.3, 9, 0.8, P)        # 그림자 눈빛


def gravity(img):
    # 소용돌이
    for i in range(160):
        t = i / 160.0
        a = t * math.pi * 4.2
        r = 1.5 + t * 9.5
        x, y = 16 + math.cos(a) * r, 16 + math.sin(a) * r
        c = PL if t < 0.3 else P if t < 0.7 else PD
        disc(img, x, y, 0.9 + t * 0.5, c)
    disc(img, 16, 16, 2.0, W)
    for x, y in ((24, 22), (8, 11)):
        disc(img, x, y, 1.3, G)


def reversal(img):
    # 위로 솟는 폭발 별
    pts = []
    for i in range(16):
        a = -math.pi / 2 + i * math.pi / 8
        r = 10.5 if i % 2 == 0 else 5
        pts.append((16 + math.cos(a) * r, 17 + math.sin(a) * r))
    for y in range(32):
        for x in range(32):
            px, py = x + 0.5, y + 0.5
            inside = False
            j = len(pts) - 1
            for i in range(len(pts)):
                xi, yi = pts[i]; xj, yj = pts[j]
                if (yi > py) != (yj > py) and px < (xj - xi) * (py - yi) / (yj - yi + 1e-9) + xi:
                    inside = not inside
                j = i
            if inside:
                d = math.hypot(px - 16, py - 17)
                img.set(x, y, OL if d < 3.5 else O if d < 6.5 else OD)
    disc(img, 16, 17, 2.2, W)
    line(img, 16, 14, 16, 7, W, 1.2)               # 위 화살 (역전)
    line(img, 13.5, 9.5, 16, 7, W, 1.2)
    line(img, 18.5, 9.5, 16, 7, W, 1.2)


def dominion(img):
    # 나무 상자 + 둘레를 도는 금빛 화살 (다시 생김)
    img.rect(11, 13, 10, 9, BR)
    img.rect(11, 13, 10, 1, (190, 130, 80, 255))
    img.rect(11, 21, 10, 1, BRD)
    line(img, 11.5, 13.5, 20.5, 21.5, BRD, 0.9)
    img.rect(11, 13, 1, 9, BRD); img.rect(20, 13, 1, 9, BRD)
    for i in range(70):
        a = math.pi * 1.15 + i / 70.0 * math.pi * 1.55
        disc(img, 16 + math.cos(a) * 8.6, 17 + math.sin(a) * 8.6, 0.8, G)
    a = math.pi * 1.15 + math.pi * 1.55
    hx, hy = 16 + math.cos(a) * 8.6, 17 + math.sin(a) * 8.6
    line(img, hx, hy, hx - 3.2, hy + 0.4, GL, 1.4)
    line(img, hx, hy, hx + 0.2, hy - 3.2, GL, 1.4)
    disc(img, 13, 15, 0.8, (230, 180, 120, 255))


def cascade(img):
    def burst(x, y, r, c1, c2):
        disc(img, x, y, r, c2)
        disc(img, x - r * 0.25, y - r * 0.25, r * 0.6, c1)
        disc(img, x - r * 0.35, y - r * 0.35, r * 0.25, W)
    line(img, 10, 21, 16, 14, PL, 1.0)
    line(img, 16, 14, 22, 20, PL, 1.0)
    burst(10, 21, 3.6, PL, P)
    burst(22, 20, 3.0, PL, P)
    burst(16, 13, 4.6, PL, PD)
    for x, y in ((8, 12), (24, 11), (13, 25), (20, 26)): img.set(x, y, PL)


def ward(img):
    # 푸른 방울 막 속 하트
    disc(img, 16, 16, 9.5, (104, 214, 255, 90))
    ring(img, 16, 16, 9.3, 1.3, B)
    ring(img, 16, 16, 9.3, 0.5, BL, gaps=1, rot=3.4)
    disc(img, 13.5, 15, 2.6, (226, 54, 84, 255))
    disc(img, 18.5, 15, 2.6, (226, 54, 84, 255))
    for y in range(15, 22):
        half = max(0, 5 - (y - 15) * 0.8)
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, (226, 54, 84, 255) if x < 17 else (150, 22, 64, 255))
    disc(img, 12.8, 14, 0.9, (255, 216, 216, 255))
    disc(img, 11, 10, 1.1, W)


def embers(img):
    # 지나간 자리(점 발자국)에 피어오르는 영혼불 둘
    def flame(x, y, h):
        for yy in range(int(y - h), int(y) + 1):
            t = (y - yy) / h                       # 0 = 밑, 1 = 끝
            half = (1 - t) ** 0.8 * h * 0.42 if t > 0.35 else h * 0.42 * (0.75 + t * 0.7)
            for xx in range(int(round(x - half)), int(round(x + half)) + 1):
                e = abs(xx - x) / max(0.5, half)
                img.set(xx, yy, OL if e < 0.35 and t < 0.6 else O if e < 0.75 else OD)
        disc(img, x, y - h * 0.25, h * 0.12, W)
    for i, (fx, fy) in enumerate(((8, 25), (11, 23), (14, 25), (17, 23), (20, 25))):
        disc(img, fx, fy, 1.0, OD if i < 3 else O)
    flame(11.5, 21, 8)
    flame(20, 19, 10)


ICONS = {'echo': echo, 'reap': reap, 'rift': rift, 'alchemy': alchemy, 'shadow': shadow, 'gravity': gravity,
         'reversal': reversal, 'dominion': dominion, 'cascade': cascade, 'ward': ward, 'embers': embers}


def outline(img):
    src = [[img.px[y][x] for x in range(img.w)] for y in range(img.h)]
    for y in range(img.h):
        for x in range(img.w):
            if src[y][x][3] > 0: continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < img.w and 0 <= yy < img.h and src[yy][xx][3] > 0:
                    img.px[y][x] = K
                    break


def symbol_outline(img, sym):
    # 상징 둘레(안쪽 메달 위)에 1칸 짙은 테 → 메달 안에서도 또렷하게
    for y in range(32):
        for x in range(32):
            if sym.px[y][x][3] > 0: continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                c = sym.get(x + dx, y + dy)
                if c[3] > 160 and math.hypot(x + 0.5 - CX, y + 0.5 - CY) < 12.4:
                    img.set(x, y, K)
                    break


def main():
    tpl = open(os.path.join(OUT, 'ability_85.png.meta'), encoding='utf-8').read()
    for name, draw in ICONS.items():
        img = Img(32, 32)
        medallion(img)
        sym = Img(32, 32)
        draw(sym)
        # 메달 밖으로 나간 상징은 자름
        for y in range(32):
            for x in range(32):
                if math.hypot(x + 0.5 - CX, y + 0.5 - CY) >= 12.4: sym.px[y][x] = (0, 0, 0, 0)
        symbol_outline(img, sym)
        img.paste(sym, 0, 0)
        outline(img)
        path = os.path.join(OUT, 'choice_tr_%s.png' % name)
        meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
        if not os.path.exists(path + '.meta'):           # 메타를 먼저 (편집기가 기본 설정 메타를 만들지 않게)
            retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta))
        retry(lambda: write(img, path))
        print('그림 저장:', name)


if __name__ == '__main__':
    main()
