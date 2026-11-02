# 특별 모드 카드 그림 (2.0.0~, ModeHubUI): 128x128 (32칸 도트를 4배 확대 · 배경 없이 윤곽선 있는 도트)
#   → Assets/Resources/Icons/mode_relay.png (영혼 릴레이) · mode_reflux.png (역류)
#   relay  쓰러진 회색 영혼 → 밝은 영혼 → 금빛 영혼으로 이어지는 화살 (바통을 넘기는 모습)
#   reflux 거꾸로 도는 모래시계 + 반시계 화살
# 실행: py tools/pixelart/make_mode_cards.py
import os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line
from make_menu_icons import outline, retry, OUT, K, W, G, GD, R, B, P, S

BL = (170, 225, 255, 255)       # 밝은 영혼 파랑
BD = (60, 110, 200, 255)        # 어두운 영혼 파랑
GY = (120, 124, 138, 255)       # 쓰러진 회색
GYD = (70, 72, 86, 255)
OR = (255, 150, 60, 255)
SAND = (235, 200, 120, 255)
SANDD = (190, 140, 70, 255)


def soul(img, cx, cy, r, body, light, dark):
    # 영혼 구슬: 몸 + 하이라이트 + 꼬리
    for i in range(1, 4):
        disc(img, cx - i * 1.6, cy + i * 1.2, r * (1 - i * 0.2), dark)
    disc(img, cx, cy, r, body)
    disc(img, cx - r * 0.3, cy - r * 0.3, r * 0.45, light)
    img.set(int(cx - r * 0.35), int(cy - r * 0.25), K)           # 눈 둘
    img.set(int(cx + r * 0.3), int(cy - r * 0.25), K)


def arrow(img, x0, y0, x1, y1, c):
    line(img, x0, y0, x1, y1, c, 1.4)
    dx, dy = x1 - x0, y1 - y0
    n = max(1e-6, (dx * dx + dy * dy) ** 0.5)
    ux, uy = dx / n, dy / n
    for s in (0.6, -0.6):
        line(img, x1, y1, x1 - ux * 3 + -uy * 3 * s, y1 - uy * 3 + ux * 3 * s, c, 1.2)


def relay(img):
    # 둥근 어둠 바탕 위에 영혼 셋 (왼쪽 아래 회색 → 가운데 위 밝은 파랑 → 오른쪽 아래 금빛)
    disc(img, 16, 16, 14.5, (36, 28, 56, 255))
    ring(img, 16, 16, 14.5, 1.2, BD, gaps=0)
    soul(img, 7, 22, 3.6, GY, (170, 174, 188, 255), GYD)
    line(img, 4, 27, 10, 27, GYD, 1.2)                                   # 쓰러진 자리의 가로줄
    line(img, 7, 17, 7, 19, K, 1)
    soul(img, 16, 11, 4.2, BL, W, BD)
    soul(img, 25, 22, 3.6, G, (255, 240, 180, 255), GD)
    arrow(img, 9, 19, 13, 14, S)
    arrow(img, 19, 14, 23, 19, G)
    for x, y in ((12, 6), (21, 6), (27, 14), (4, 12)): img.set(x, y, W)    # 별가루


def reflux(img):
    # 거꾸로 선 모래시계: 위쪽에 모래가 다 쌓이고 아래가 비어 감 + 반시계 화살
    disc(img, 16, 16, 14.5, (36, 28, 56, 255))
    ring(img, 16, 16, 14.5, 1.2, P, gaps=0)
    img.rect(9, 5, 14, 2, SANDD)                                          # 위 받침
    img.rect(9, 25, 14, 2, SANDD)                                         # 아래 받침
    for y in range(7, 25):                                                 # 유리 두 칸 (잘록한 허리)
        half = abs(y - 15.5) * 0.62 + 1
        for x in range(int(round(16 - half)), int(round(16 + half)) + 1):
            img.set(x, y, (200, 225, 245, 150))
    for y in range(8, 15):                                                 # 위쪽 모래 (쌓임)
        half = abs(y - 15.5) * 0.62 + 0.2
        for x in range(int(round(16 - half)), int(round(16 + half)) + 1):
            img.set(x, y, SAND)
    for y in range(20, 24): img.set(16, y, SAND)                           # 거꾸로 올라가는 모래줄
    line(img, 16, 15, 16, 22, SANDD, 1)
    ring(img, 16, 16, 12, 1.2, P, gaps=6, rot=0.4)
    arrow(img, 6, 11, 5, 17, P)
    for x, y in ((4, 22), (27, 9), (25, 25)): img.set(x, y, W)


ICONS = {'relay': relay, 'reflux': reflux}


def scale(img, k):
    out = Img(img.w * k, img.h * k)
    for y in range(img.h):
        for x in range(img.w):
            c = img.px[y][x]
            if c[3] == 0: continue
            for dy in range(k):
                for dx in range(k):
                    out.px[y * k + dy][x * k + dx] = c
    return out


def main():
    tpl = open(os.path.join(OUT, 'ability_85.png.meta'), encoding='utf-8').read()
    for name, draw in ICONS.items():
        img = Img(32, 32)
        draw(img)
        outline(img)
        big = scale(img, 4)
        path = os.path.join(OUT, 'mode_%s.png' % name)
        meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
        # 메타를 먼저 (편집기가 그림을 먼저 보면 기본 설정 메타를 만들어 버림)
        if not os.path.exists(path + '.meta'):
            retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta))
        retry(lambda: write(big, path))
        print('그림 저장:', name)


if __name__ == '__main__':
    main()
