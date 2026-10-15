# 영혼 각인 그림 (1.2~, SoulBrand · ChoiceUI · 메인 메뉴 각인 창): 32x32, 배경 없이 윤곽선 있는 도트
#   → Assets/Resources/Icons/choice_brand_<이름>.png (고르기 창 그림과 같은 방식 · make_choice_icons.py)
#   glass 유리 대포 · tide 영혼의 밀물 · giant 거인 사냥꾼 · iron 무쇠 맹세 · greed 탐욕 · frenzy 광란
#   blood 피의 계약 · calm 고요 · tyrant 폭군의 왕관 · surge 마력 폭주 · bulwark 철벽 · dance 죽음의 춤
# 실행: py tools/pixelart/make_brand_icons.py
import os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line
from make_menu_icons import outline, retry, OUT, K, W, G, GD, R, B, P, S

BL = (170, 225, 255, 255)       # 밝은 영혼 파랑
BD = (60, 110, 200, 255)        # 어두운 영혼 파랑
RD = (150, 35, 45, 255)         # 어두운 빨강
PD = (110, 60, 160, 255)        # 어두운 보라
PL = (225, 185, 255, 255)       # 밝은 보라
SD = (105, 110, 125, 255)       # 어두운 쇠
SL = (215, 220, 230, 255)       # 밝은 쇠
BONE = (232, 225, 240, 255)
OR = (255, 150, 60, 255)


def glass(img):
    # 금이 간 유리 포신의 대포 + 포구의 불꽃
    for i in range(0, 15):                              # 비스듬한 포신 (왼쪽 아래 → 오른쪽 위)
        line(img, 7 + i, 22 - i * 0.8, 9 + i, 25 - i * 0.8, BL if i % 3 else (220, 245, 255, 255), 2.6)
    line(img, 9, 23, 22, 12.6, BD, 1)                   # 포신 아래 그늘
    line(img, 14, 17, 16, 20, W, 1)                     # 금
    line(img, 16, 20, 18, 17, W, 1)
    disc(img, 8, 25, 5, SD)                             # 바퀴
    disc(img, 9, 25, 2.2, S)
    disc(img, 26, 8, 3.2, OR)                           # 포구 불꽃
    disc(img, 26, 8, 1.6, (255, 235, 160, 255))
    for x, y in ((29, 4), (23, 4), (30, 10)): img.set(x, y, OR)


def tide(img):
    # 밀려오는 영혼의 물결 세 겹
    for k, (top, c) in enumerate(((7, BL), (14, B), (21, BD))):
        for x in range(3, 30):
            y = top + int(2.5 * (1 if (x + k * 3) % 10 < 5 else -1) * (((x + k * 3) % 5) / 5.0))
            for yy in range(y, y + 4): img.set(x, yy, c)
        disc(img, 6 + k * 9, top - 1, 1.6, W)             # 물마루의 영혼 불빛


def giant(img):
    # 커다란 붉은 눈 + 겨누는 조준 고리
    disc(img, 16, 16, 11, (60, 25, 35, 255))
    disc(img, 16, 16, 8, RD)
    disc(img, 16, 16, 5, R)
    disc(img, 16, 16, 2.2, K)
    disc(img, 13, 13, 1.4, (255, 190, 190, 255))
    ring(img, 16, 16, 13.5, 1.6, G, gaps=4, rot=0.4)
    for a, b, c, d in ((16, 0, 16, 5), (16, 27, 16, 31), (0, 16, 5, 16), (27, 16, 31, 16)):
        line(img, a, b, c, d, G, 1.4)


def iron(img):
    # 둥근 무쇠 방패 + 리벳 + 맹세의 띠
    disc(img, 16, 16, 12, SD)
    disc(img, 16, 16, 10.5, S)
    disc(img, 13, 12, 5, SL)
    for x, y in ((16, 6), (16, 26), (6, 16), (26, 16)): disc(img, x, y, 1.3, SD)
    img.rect(8, 14, 17, 4, RD)                          # 띠
    img.rect(8, 14, 17, 1, R)
    disc(img, 16, 16, 3, G)
    disc(img, 16, 16, 1.4, GD)


def greed(img):
    # 불룩한 금화 주머니 + 흘러내리는 보랏빛 저주
    disc(img, 16, 20, 9, (175, 120, 60, 255))
    disc(img, 13, 17, 4, (210, 160, 90, 255))
    img.rect(12, 8, 8, 4, (150, 100, 50, 255))          # 묶은 목
    line(img, 11, 11, 21, 11, G, 1.4)                   # 금 끈
    disc(img, 16, 6, 2.6, G)                            # 위로 삐져나온 금화
    disc(img, 21, 5, 2.2, G)
    disc(img, 16, 21, 3.4, G)                           # 주머니의 금 표시
    img.rect(15, 19, 2, 5, GD)
    for x, y0, n in ((9, 23, 6), (22, 24, 5), (16, 27, 4)):
        line(img, x, y0, x, y0 + n, P, 1.4)              # 저주 방울
        disc(img, x, y0 + n, 1.3, PD)


def frenzy(img):
    # 붉은 할퀸 자국 세 줄 + 불티
    for i, c in enumerate((R, (255, 110, 90, 255), R)):
        x = 8 + i * 6
        line(img, x, 4, x + 9, 27, RD, 3.2)
        line(img, x, 4, x + 9, 27, c, 1.6)
    for x, y in ((5, 9), (27, 6), (28, 24), (4, 25)): disc(img, x, y, 1.1, OR)


def blood(img):
    # 핏방울 + 계약의 고리
    for y in range(4, 29):
        if y < 15: half = (y - 4) * 0.55
        else: half = max(0.0, 8.5 - ((y - 21) ** 2) * 0.12) if y > 21 else 8.5 - (21 - y) * 0.15
        for x in range(int(round(16 - half)), int(round(16 + half)) + 1):
            img.set(x, y, R if x < 18 else RD)
    disc(img, 13, 19, 2, (255, 170, 170, 255))
    ring(img, 16, 20, 11.5, 1.4, G, gaps=6, rot=0.2)


def calm(img):
    # 초승달 + 고요한 별
    disc(img, 15, 16, 11, BL)
    for y in range(32):
        for x in range(32):
            if (x + 0.5 - 20) ** 2 + (y + 0.5 - 13) ** 2 <= 100: img.px[y][x] = (0, 0, 0, 0)
    disc(img, 10, 20, 3, (215, 240, 255, 255))
    for x, y in ((24, 22), (27, 8), (22, 4)):
        img.set(x, y, W); img.set(x - 1, y, B); img.set(x + 1, y, B); img.set(x, y - 1, B); img.set(x, y + 1, B)


def tyrant(img):
    # 검은 왕관 + 붉은 보석
    img.rect(5, 18, 22, 8, (70, 55, 80, 255))
    img.rect(5, 18, 22, 2, (110, 90, 125, 255))
    for cx in (6, 16, 26):                              # 왕관 뾰족한 끝
        for y in range(6, 18):
            half = (y - 6) * 0.42
            for x in range(int(round(cx - half)), int(round(cx + half)) + 1):
                img.set(x, y, (85, 65, 95, 255))
        disc(img, cx, 6, 1.6, G)
    disc(img, 16, 22, 2.8, R)
    disc(img, 15, 21, 1, (255, 190, 190, 255))
    disc(img, 9, 22, 1.6, RD)
    disc(img, 23, 22, 1.6, RD)
    line(img, 5, 26, 26, 26, GD, 1)


def surge(img):
    # 보랏빛 번개 + 마력 고리
    ring(img, 16, 16, 12, 1.4, PD, gaps=5, rot=0.7)
    pts = [(19, 2), (9, 17), (15, 17), (11, 30), (24, 12), (17, 12), (21, 2)]
    for y in range(32):
        for x in range(32):
            # 다각형 안쪽 칸 칠하기 (짝수-홀수 규칙)
            inside = False
            j = len(pts) - 1
            for i in range(len(pts)):
                xi, yi = pts[i]; xj, yj = pts[j]
                if (yi > y + 0.5) != (yj > y + 0.5) and x + 0.5 < (xj - xi) * (y + 0.5 - yi) / (yj - yi) + xi:
                    inside = not inside
                j = i
            if inside: img.set(x, y, P if x < 16 else PL)
    line(img, 18, 4, 12, 15, W, 1)


def bulwark(img):
    # 벽돌 성벽 + 성가퀴
    for x0 in (4, 12, 20):                              # 성가퀴
        img.rect(x0, 5, 7, 5, SD)
        img.rect(x0 + 1, 6, 5, 3, S)
    img.rect(4, 10, 24, 18, SD)
    for row in range(4):
        y = 11 + row * 4
        off = 0 if row % 2 == 0 else 4
        for x0 in range(4 - off, 28, 8):
            xs, xe = max(5, x0 + 1), min(27, x0 + 7)
            if xe > xs: img.rect(xs, y, xe - xs, 3, S if row % 2 else SL)
    img.rect(13, 20, 6, 8, (60, 45, 35, 255))           # 성문
    disc(img, 16, 20, 3, (60, 45, 35, 255))


def dance(img):
    # 불타는 눈의 해골 + 엇갈린 두 낫자루
    line(img, 4, 28, 27, 5, (120, 85, 55, 255), 1.8)
    line(img, 28, 28, 5, 5, (120, 85, 55, 255), 1.8)
    for cx, sgn in ((5, 1), (27, -1)):                  # 낫날
        for i in range(8):
            img.set(cx + sgn * i, 4 + int(i * 0.3), SL)
            img.set(cx + sgn * i, 5 + int(i * 0.3), S)
    disc(img, 16, 14, 7.5, BONE)
    img.rect(12, 18, 8, 6, BONE)
    disc(img, 13, 14, 2.3, K)
    disc(img, 19, 14, 2.3, K)
    disc(img, 13, 14, 1.2, R)
    disc(img, 19, 14, 1.2, R)
    img.set(16, 18, K)
    for x in (13, 16, 19): line(img, x, 21, x, 23, (150, 140, 160, 255), 1)
    for x, y in ((11, 6), (16, 4), (21, 6)): disc(img, x, y, 1.3, OR)


ICONS = {'glass': glass, 'tide': tide, 'giant': giant, 'iron': iron, 'greed': greed, 'frenzy': frenzy,
         'blood': blood, 'calm': calm, 'tyrant': tyrant, 'surge': surge, 'bulwark': bulwark, 'dance': dance}


def main():
    tpl = open(os.path.join(OUT, 'ability_85.png.meta'), encoding='utf-8').read()
    for name, draw in ICONS.items():
        img = Img(32, 32)
        draw(img)
        outline(img)
        path = os.path.join(OUT, 'choice_brand_%s.png' % name)
        meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
        # 메타를 먼저 (편집기가 그림을 먼저 보면 기본 설정 메타를 만들어 버림)
        if not os.path.exists(path + '.meta'):
            retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta))
        retry(lambda: write(img, path))
        print('그림 저장:', name)


if __name__ == '__main__':
    main()
