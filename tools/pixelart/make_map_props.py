# 맵 물체 · 석관 · 떨어지는 것 그림 (1.2~, MapProps · ContentRegistry.Props · Drops): 배경 없이 윤곽선 있는 도트
#   → Assets/Resources/Icons/prop_<이름>.png · drop_<이름>.png
# 1.1.2~ 적 떼 · 화려한 이펙트 속에서도 「부술 수 있는 것」으로 읽히게:
#   몸통(32칸) 둘레에 어두운 윤곽 1칸 + 밝은 테두리 1칸 (부서지는 물체는 금빛 · 석관은 영혼빛 보라 · 떨어지는 것은 종류별 색)
#   물체 그림은 40x40 (몸통 32칸 + 둘레 4칸 여백) · 떨어지는 것은 36x36 (둘레 2칸)
#   prop_<이름>_glow.png: 테두리 바깥 2칸까지 넓힌 흰 실루엣 (MapProp 이 몸통 뒤에 금빛으로 맥동 · 가까이 가면 밝아짐 · 부서질 때 흰 번쩍임)
#   몸통 색도 장 바닥과 겹치지 않게 다시 칠함 (화로: 어두운 쇠 → 놋쇠 · 나무통: 초원 흙길과 같은 갈색 → 밝은 꿀색 나무 · 수정: 심연 파랑 → 밝은 청록)
#   python tools/pixelart/make_map_props.py --preview <png> : 장별 바닥 타일 위에 얹은 비교 그림 (커밋하지 않는 확인용)
#   장마다 부서지는 물체: urn 낡은 항아리(지하 묘역) · brazier 꺼진 화로(불타는 지옥) · barrel 나무통(초원) · crystal 영혼 수정(영혼의 심연)
#   sarcophagus 봉인된 석관 (2장부터, 부수면 유물 2택)
#   떨어지는 것: meat 고기(회복) · magnet 자석 · hourglass 시간 정지 · purify 정화 (코인 · 금화는 게임 안 코인 그림 그대로)
# 실행: python tools/pixelart/make_map_props.py
import os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import disc, ring, line
from make_fx import ROOT
from make_menu_icons import outline, retry, OUT, K, W, G, GD, R, B, P, S

BR = (206, 140, 72, 255)        # 나무 (밝은 꿀색 · 초원 흙길 (128, 96, 48) 보다 확실히 밝게)
BRL = (240, 184, 108, 255)
BRD = (140, 84, 40, 255)
CLAY = (176, 120, 86, 255)      # 흙 항아리
CLAYL = (212, 160, 120, 255)
CLAYD = (122, 78, 58, 255)
STONE = (128, 124, 140, 255)    # 돌
STONEL = (170, 166, 182, 255)
STONED = (84, 80, 98, 255)
IRON = (150, 146, 162, 255)     # 쇠 띠 (밝게)
IRONL = (205, 202, 216, 255)
BRASS = (196, 140, 58, 255)     # 화로 놋쇠 (지옥 바닥의 어두운 빨강과 겹치지 않게)
BRASSL = (246, 200, 106, 255)
BRASSD = (128, 84, 36, 255)
EMBER = (255, 120, 40, 255)
EMBERL = (255, 210, 110, 255)
SOUL = (110, 236, 255, 255)     # 수정 (심연 바닥의 파랑 · 보라보다 밝은 청록)
SOULL = (226, 255, 255, 255)
SOULD = (40, 168, 214, 255)
VIO = (175, 110, 245, 255)
VIOL = (225, 190, 255, 255)
MEAT = (196, 86, 60, 255)
MEATL = (236, 136, 96, 255)
MEATD = (140, 54, 40, 255)
BONE = (238, 230, 214, 255)
RIM_GOLD = (255, 222, 110, 255)  # 부서지는 물체 테두리 (보물 상자와 같은 금빛 = 「건드리면 뭔가 나옴」)
RIM_SOUL = (214, 170, 255, 255)  # 석관 테두리
RIM_DROP = {'drop_meat': (130, 255, 140, 255), 'drop_magnet': (255, 110, 120, 255),
            'drop_hourglass': (130, 210, 255, 255), 'drop_purify': (255, 250, 200, 255)}


def urn(img):
    # 둥근 몸통 + 좁은 목 + 넓은 입 · 금 간 자국
    for y in range(9, 29):
        t = (y - 18.5) / 10.0
        half = 10.5 * (1 - t * t) ** 0.5 if abs(t) < 1 else 0
        half = max(half, 4)
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, CLAYL if x < 13 else CLAY if x < 20 else CLAYD)
    img.rect(12, 5, 8, 5, CLAY)                     # 목
    img.rect(12, 5, 2, 5, CLAYL)
    img.rect(10, 3, 12, 3, CLAYD)                   # 입
    img.rect(11, 3, 10, 1, CLAYL)
    for x in range(7, 26): img.set(x, 15, CLAYD)    # 띠
    for x in range(8, 25, 3): img.set(x, 16, CLAYD)
    line(img, 20, 11, 22, 17, (90, 58, 42, 255), 1)  # 금
    line(img, 22, 17, 20, 21, (90, 58, 42, 255), 1)
    disc(img, 10.5, 12, 1.5, (240, 200, 165, 255))  # 빛


def brazier(img):
    # 세 발 쇠 화로 + 남은 불씨
    line(img, 9, 20, 6, 29, BRASSD, 2)
    line(img, 23, 20, 26, 29, BRASSD, 2)
    line(img, 16, 21, 16, 29, BRASS, 2)
    for y in range(13, 22):                         # 그릇
        half = 12 - (y - 13) * 0.6
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, BRASSL if y < 15 else BRASS if x < 20 else BRASSD)
    img.rect(3, 12, 26, 2, BRASSL)
    for x in range(5, 28, 4): img.set(x, 12, (255, 236, 170, 255))
    for x in range(6, 27, 5): img.set(x, 17, BRASSD)        # 그릇 무늬
    for cx, cy, r in ((11, 11, 2.6), (16, 10, 3.2), (21, 11, 2.6)):   # 불씨 더미
        disc(img, cx, cy, r, (90, 46, 36, 255))
    for cx, cy in ((10, 10), (15, 9), (18, 10), (22, 10), (13, 11), (20, 11), (12, 10)):
        img.set(cx, cy, EMBER)
    disc(img, 16, 8.5, 1.6, EMBERL)


def barrel(img):
    # 나무통: 세로 널빤지 + 쇠 띠 두 줄
    for y in range(4, 29):
        t = (y - 16.5) / 12.5
        half = 9 + 2.2 * (1 - t * t)
        for x in range(int(16 - half), int(16 + half) + 1):
            k = (x - 5) % 5
            c = BRL if x < 11 else BR if x < 21 else BRD
            if k == 0: c = BRD
            img.set(x, y, c)
    for y in (8, 23):
        for x in range(4, 29):
            if img.get(x, y)[3] > 0:
                img.set(x, y, IRONL); img.set(x, y + 1, IRON)
    img.rect(8, 3, 16, 2, BRD)                      # 뚜껑 테
    img.rect(9, 3, 14, 1, BRL)
    disc(img, 9, 13, 1.2, (210, 150, 95, 255))


def crystal(img):
    # 바위 위 영혼 수정 셋 (가운데가 크게)
    def shard(cx, top, h, w):
        mid = top + h * 0.35
        for y in range(int(top), int(top + h) + 1):
            half = (y - top) / max(1, mid - top) * w if y <= mid else w * (1 - (y - mid) / max(1, top + h - mid) * 0.25)
            for x in range(int(round(cx - half)), int(round(cx + half)) + 1):
                img.set(x, y, SOULL if x < cx - 0.5 else SOUL if x < cx + half * 0.5 else SOULD)
    for y in range(23, 30):                         # 바위
        half = 13 - (29 - y) * 0.4 if y > 26 else 11
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, STONEL if y < 25 else STONE if y < 28 else STONED)
    shard(9, 11, 13, 3.5)
    shard(23, 13, 11, 3)
    shard(16, 2, 22, 4.5)
    line(img, 15, 6, 15, 19, (255, 255, 255, 255), 1)


def sarcophagus(img):
    # 서 있는 돌 석관 + 보랏빛 봉인 문양 + 쇠사슬
    for y in range(1, 31):
        if y < 6: half = 6 + (y - 1) * 0.9          # 어깨까지 넓어짐
        elif y < 12: half = 10.5
        else: half = 10.5 - (y - 12) * 0.22         # 발 쪽은 좁게
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, STONEL if x < 11 else STONE if x < 21 else STONED)
    for y in range(3, 29):                          # 뚜껑 테두리 홈
        img.set(8 if y > 6 else 9, y, STONED)
    for x in range(9, 24): img.set(x, 3, STONED)
    # 봉인 문양: 가운데 눈 + 둘레 고리
    ring(img, 16, 13, 4.6, 1.2, VIO)
    disc(img, 16, 13, 2.2, VIOL)
    disc(img, 16, 13, 1.0, (255, 255, 255, 255))
    for x, y in ((16, 6), (16, 20), (10, 13), (22, 13)): disc(img, x, y, 0.9, VIO)
    line(img, 16, 21, 16, 27, VIO, 1)
    line(img, 13, 24, 19, 24, VIO, 1)
    # 사슬 (대각선 두 줄)
    for i in range(7):
        x, y = 6 + i * 3.3, 8 + i * 2.6
        ring(img, x, y, 1.5, 0.9, IRONL)
    for i in range(7):
        x, y = 26 - i * 3.3, 8 + i * 2.6
        ring(img, x, y, 1.5, 0.9, IRONL)


def meat(img):
    # 뼈 달린 고기 (만화식)
    disc(img, 13, 14, 9, MEATD)
    disc(img, 12.5, 13.5, 8, MEAT)
    disc(img, 10, 11, 3.5, MEATL)
    disc(img, 9, 10, 1.4, (255, 200, 170, 255))
    line(img, 19, 20, 26, 27, BONE, 3)             # 뼈
    disc(img, 26.5, 25.5, 2.2, BONE)
    disc(img, 24.5, 28, 2.2, BONE)
    line(img, 19, 21, 25, 27, (200, 190, 175, 255), 1)


def magnet(img):
    # 빨간 말굽 자석 + 은색 끝 + 끌림 선
    ring(img, 16, 14, 8.5, 5, R)
    ring(img, 16, 14, 8.5, 1, (255, 140, 140, 255))
    for y in range(14, 27):
        for x in list(range(5, 10)) + list(range(23, 28)):
            img.set(x, y, R)
    # 아래 반원은 지움 (말굽 모양)
    for y in range(15, 24):
        for x in range(10, 23):
            img.px[y][x] = (0, 0, 0, 0)
    for y in range(22, 28):
        for x in list(range(5, 10)) + list(range(23, 28)):
            img.set(x, y, W if y < 24 else S)


def hourglass(img):
    # 금 테두리 모래시계 (모래가 멈춘 채 빛남)
    img.rect(7, 3, 18, 3, GD); img.rect(8, 3, 16, 1, G)
    img.rect(7, 26, 18, 3, GD); img.rect(8, 26, 16, 1, G)
    line(img, 9, 6, 9, 25, BR, 1.6)
    line(img, 23, 6, 23, 25, BR, 1.6)
    for y in range(6, 26):
        half = max(1, 6.5 * abs(y - 15.5) / 9.5)
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, (200, 230, 250, 255))
    for y in range(19, 26):                         # 아래 모래
        half = max(1, 6.5 * abs(y - 15.5) / 9.5) - (1 if y < 21 else 0)
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, (240, 200, 110, 255))
    for y in range(8, 12):
        half = max(1, 6.5 * abs(y - 15.5) / 9.5) - 1
        for x in range(int(16 - half), int(16 + half) + 1):
            img.set(x, y, (240, 200, 110, 255))
    img.set(16, 15, (240, 200, 110, 255)); img.set(16, 16, (255, 240, 180, 255))
    disc(img, 12, 8, 0.9, (255, 255, 255, 255))


def purify(img):
    # 성수 병 + 하얀 빛살
    for k in range(8):
        import math
        a = k * math.pi / 4
        line(img, 16 + math.cos(a) * 9, 17 + math.sin(a) * 9, 16 + math.cos(a) * 14, 17 + math.sin(a) * 14, (255, 250, 210, 255), 1.4)
    disc(img, 16, 19, 7.5, (90, 150, 220, 255))
    disc(img, 16, 19, 6.5, (150, 210, 255, 255))
    disc(img, 13.5, 16.5, 2, (235, 250, 255, 255))
    img.rect(13, 7, 6, 6, (215, 230, 245, 255))     # 병목
    img.rect(12, 4, 8, 3, G)                        # 금 마개
    line(img, 16, 16, 16, 23, W, 1.4)               # 십자
    line(img, 13, 19, 19, 19, W, 1.4)


ICONS = {'prop_urn': urn, 'prop_brazier': brazier, 'prop_barrel': barrel, 'prop_crystal': crystal,
         'prop_sarcophagus': sarcophagus,
         'drop_meat': meat, 'drop_magnet': magnet, 'drop_hourglass': hourglass, 'drop_purify': purify}

# 테두리 없이 맨 나중에 찍는 낱 픽셀 (튀는 불티 · 반짝임): 테두리를 두르면 네모 점처럼 보였음 · 몸통 32칸 기준 좌표
SPECKS = {'prop_brazier': [(12, 5, EMBER), (19, 4, EMBERL), (16, 2, EMBER)],
          'prop_crystal': [(6, 6, (255, 255, 255, 255)), (27, 8, VIOL), (20, 3, (255, 255, 255, 255))],
          'drop_magnet': [(3, 30, G), (16, 31, G), (29, 30, G)]}

N8 = ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1))


def grow(img, c, near=N8):
    # 칠한 칸 둘레 1칸을 c 로 (8방향이면 대각선까지 꽉 찬 굵은 테두리)
    src = [[img.px[y][x][3] > 0 for x in range(img.w)] for y in range(img.h)]
    for y in range(img.h):
        for x in range(img.w):
            if src[y][x]: continue
            for dx, dy in near:
                xx, yy = x + dx, y + dy
                if 0 <= xx < img.w and 0 <= yy < img.h and src[yy][xx]:
                    img.px[y][x] = c
                    break


def framed(name, draw, size, rim):
    # 몸통(32칸)을 가운데에 그리고 → 어두운 윤곽 1칸 → 밝은 테두리 1칸
    body = Img(32, 32)
    draw(body)
    img = Img(size, size)
    m = (size - 32) // 2
    img.paste(body, m, m)
    grow(img, K)                                    # 대각선까지 꽉 찬 어두운 윤곽 (밝은 테두리와 몸통 사이를 끊음)
    grow(img, rim)
    for x, y, c in SPECKS.get(name, ()):
        if img.get(x + m, y + m)[3] == 0: img.set(x + m, y + m, c)
    return img


def glow_of(img):
    # 테두리 바깥 2칸까지 넓힌 흰 실루엣 (안쪽은 진하게 · 바깥 칸으로 갈수록 옅게)
    out = Img(img.w, img.h)
    for y in range(img.h):
        for x in range(img.w):
            if img.px[y][x][3] > 0: out.px[y][x] = (255, 255, 255, 255)
    grow(out, (255, 255, 255, 190))
    grow(out, (255, 255, 255, 90))
    return out


def save(img, name, tpl):
    path = os.path.join(OUT, '%s.png' % name)
    meta = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, tpl, count=1)
    if not os.path.exists(path + '.meta'):          # 이미 있는 .meta 는 그대로 (guid 유지)
        retry(lambda: open(path + '.meta', 'w', encoding='utf-8', newline='\n').write(meta))
    retry(lambda: write(img, path))
    print('그림 저장:', name)


def build():
    arts = {}
    for name, draw in ICONS.items():
        if name.startswith('prop_'):
            img = framed(name, draw, 40, RIM_SOUL if name == 'prop_sarcophagus' else RIM_GOLD)
            arts[name] = img
            arts[name + '_glow'] = glow_of(img)
        else:
            arts[name] = framed(name, draw, 36, RIM_DROP[name])
    return arts


# ---------------------------------------------------------------- 확인용 비교 그림 (장별 바닥 위)
TILES = ['Assets/Sprites/tileset_cave_32.png', 'Assets/Sprites/tileset_hell_32.png',
         'Assets/Sprites/tileset_meadow_32.png', 'Assets/Resources/Abyss/tileset_abyss_32.png']


def preview(arts, path, scale=3):
    from png import read
    names = ['prop_urn', 'prop_brazier', 'prop_barrel', 'prop_crystal', 'prop_sarcophagus',
             'drop_meat', 'drop_magnet', 'drop_hourglass', 'drop_purify']
    cw = 44
    w, h = cw * len(names), 48 * len(TILES)
    sheet = Img(w, h)
    for row, tile in enumerate(TILES):
        t = read(os.path.join(ROOT, tile))
        for y in range(48):
            for x in range(w):
                sheet.px[row * 48 + y][x] = t.px[y % 32][x % 32][:3] + (255,)
        for col, n in enumerate(names):
            a = arts[n]
            sheet.paste(a, col * cw + (cw - a.w) // 2, row * 48 + (48 - a.h) // 2)
    big = Img(w * scale, h * scale)
    for y in range(big.h):
        for x in range(big.w):
            big.px[y][x] = sheet.px[y // scale][x // scale]
    write(big, path)
    print('비교 그림:', path)


def main():
    arts = build()
    if '--preview' in sys.argv:
        preview(arts, sys.argv[sys.argv.index('--preview') + 1])
        return
    tpl = open(os.path.join(OUT, 'ability_85.png.meta'), encoding='utf-8').read()
    for name, img in arts.items():
        save(img, name, tpl)


if __name__ == '__main__':
    main()
