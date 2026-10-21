# 범위 · 예고 표시용 도트 그림 (1.5.1~)
#   → Assets/Resources/FX/fx_ind_*.png  (흰색 기반 · 코드가 색을 곱함 · 32px = 1칸)
#   fx_ind_stroke : 선 · 고리 「도트 획」 (가로는 같고 세로 = 두께 방향 8줄: 어두운 외곽 · 색 · 밝은 심)
#                   LineRenderer(Stretch)에 입혀 모든 선을 도트 획처럼. 바깥 1줄씩이 어두운 외곽이라
#                   코드가 두께를 8/6 배로 키워 색 부분 두께는 예전 그대로 (PixelLine.Widen)
#   fx_ind_dash   : 점선 획 (한 칸 = 획 10px + 빈칸 6px, RepeatPerSegment 로 선 마디마다 한 칸)
#   fx_ind_disc   : 예고 원 채움 도트 원판 (지름 64px = 2칸, 가장자리 = 판정 원)
#   fx_ind_floor  : 바닥 범위 원판 (체크 무늬 디더 + 테두리, 가장자리 = 판정 원)
#   fx_ind_band   : 직선 예고 띠 타일 (16px 사선 줄무늬 · SpriteRenderer Tiled 로 판정 띠 크기 그대로 깔아 씀)
#   fx_ind_cone   : 부채꼴 채움 (가로 = 중심 → 끝 반지름 방향 · 동심 띠 + 끝 테두리)
# 실행: python tools/pixelart/make_indicators.py [미리보기.png]
import os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, write
from make_fx import ROOT

OUT = os.path.join(ROOT, 'Assets', 'Resources', 'FX')

WHITE = (255, 255, 255, 255)
MID = (206, 206, 214, 255)      # 색 띠 (색을 곱하면 살짝 짙은 색)
DARK = (26, 20, 32, 255)        # 어두운 외곽 (어떤 색을 곱해도 거의 검정)
DIM = (150, 150, 160, 255)      # 원판 안쪽 테두리


def stroke_rows():
    # 두께 방향 8줄 (위아래 대칭): 외곽 · 색 · 심 4줄 · 색 · 외곽
    return [DARK, MID, WHITE, WHITE, WHITE, WHITE, MID, DARK]


def make_stroke():
    img = Img(4, 8)
    for y, c in enumerate(stroke_rows()):
        for x in range(4):
            img.set(x, y, c)
    return img


def make_dash():
    # 한 칸 16px: 0~9 획 (양 끝 1px 어두운 마개) · 10~15 빈칸
    img = Img(16, 8)
    rows = stroke_rows()
    for x in range(10):
        for y, c in enumerate(rows):
            img.set(x, y, DARK if (x == 0 or x == 9) and 0 < y < 7 else c)
    return img


def in_disc(x, y, r):
    dx, dy = x + 0.5 - r, y + 0.5 - r
    return (dx * dx + dy * dy) ** 0.5


def make_disc():
    # 지름 64: 가장자리 2px 밝은 테두리 · 1px 안쪽 짙은 줄 · 안은 고르게
    n, r = 64, 32.0
    img = Img(n, n)
    for y in range(n):
        for x in range(n):
            d = in_disc(x, y, r)
            if d > r:
                continue
            if d > r - 2:
                img.set(x, y, WHITE)
            elif d > r - 3:
                img.set(x, y, DIM)
            else:
                img.set(x, y, (225, 225, 230, 255))
    return img


def make_floor():
    # 지름 64: 2px 단위 체크 무늬 디더(진함 · 옅음) + 가장자리 2px 테두리 + 반지름 2/3 점선 고리
    n, r = 64, 32.0
    img = Img(n, n)
    for y in range(n):
        for x in range(n):
            d = in_disc(x, y, r)
            if d > r:
                continue
            if d > r - 2:
                img.set(x, y, WHITE)
            elif abs(d - r * 0.62) < 0.75 and ((x + y) // 2) % 2 == 0:
                img.set(x, y, (255, 255, 255, 200))
            else:
                a = 150 if ((x // 2) + (y // 2)) % 2 == 0 else 70
                img.set(x, y, (255, 255, 255, a))
    return img


def make_band():
    # 16x16 사선 줄무늬 (진한 줄 4px · 옅은 줄 4px), 위아래로 이어 붙여도 이음매가 맞음
    img = Img(16, 16)
    for y in range(16):
        for x in range(16):
            a = 255 if ((x + y) // 4) % 2 == 0 else 150
            img.set(x, y, (255, 255, 255, a))
    return img


def make_cone():
    # 가로 32px = 부채꼴 중심(0) → 끝(31): 3px 마다 진함 · 옅음 동심 띠, 끝 3px 밝은 테두리, 그 안쪽 1px 짙은 줄
    img = Img(32, 2)
    for x in range(32):
        if x >= 29:
            c = WHITE
        elif x == 28:
            c = (110, 110, 120, 255)
        elif x < 2:
            c = (255, 255, 255, 120)
        else:
            c = (255, 255, 255, 235 if (x // 3) % 2 == 0 else 170)
        for y in range(2):
            img.set(x, y, c)
    return img


ASSETS = {
    # 이름: (그리기, 반복 감기(wrapU 0 = Repeat), 메시 Full Rect(타일 그리기용))
    'fx_ind_stroke': (make_stroke, False, False),
    'fx_ind_dash': (make_dash, True, False),
    'fx_ind_disc': (make_disc, False, False),
    'fx_ind_floor': (make_floor, False, False),
    'fx_ind_band': (make_band, True, True),
    'fx_ind_cone': (make_cone, False, False),
}


def meta(path, repeat, full_rect):
    # hp_potion 메타를 틀로 (Point 필터 · 압축 없음 · 밉맵 없음), 이미 있으면 guid 는 그대로
    guid = uuid.uuid4().hex
    if os.path.exists(path):
        m = re.search(r'guid: ([0-9a-f]{32})', open(path, encoding='utf-8').read())
        if m:
            guid = m.group(1)
    tpl = open(os.path.join(ROOT, 'Assets', 'Sprites', 'Items', 'hp_potion.png.meta'), encoding='utf-8').read()
    tpl = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + guid, tpl, count=1)
    tpl = re.sub(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: 32', tpl)
    tpl = re.sub(r'filterMode: \d+', 'filterMode: 0', tpl)
    w = '0' if repeat else '1'
    tpl = re.sub(r'wrapU: \d+', 'wrapU: ' + w, tpl)
    tpl = re.sub(r'wrapV: \d+', 'wrapV: 1', tpl)
    tpl = re.sub(r'spriteMeshType: \d+', 'spriteMeshType: ' + ('0' if full_rect else '1'), tpl)
    open(path, 'w', encoding='utf-8', newline='\n').write(tpl)


def preview(imgs, path, scale=6):
    # 어두운 바닥 · 밝은 바닥 두 줄에 몇 가지 색을 곱해 늘어놓은 확대 그림
    tints = [(255, 80, 70), (120, 200, 255), (255, 215, 90)]
    pad = 8
    cells = []
    for name, img in imgs:
        for t in tints:
            cells.append((img, t))
    cw = max(i.w for i, _ in cells) * scale + pad
    ch = max(i.h for i, _ in cells) * scale + pad
    cols = len(tints) * 2
    rows = (len(cells) + cols - 1) // cols
    out = Img(cols * cw * 2 // 2, rows * ch * 2)
    for half, bg in enumerate([(40, 34, 48, 255), (150, 170, 120, 255)]):
        for y in range(rows * ch):
            for x in range(cols * cw):
                out.set(x, y + half * rows * ch, bg)
        for i, (img, t) in enumerate(cells):
            ox, oy = (i % cols) * cw + pad // 2, (i // cols) * ch + pad // 2 + half * rows * ch
            for y in range(img.h * scale):
                for x in range(img.w * scale):
                    r, g, b, a = img.px[y // scale][x // scale]
                    if a:
                        out.blend(ox + x, oy + y, (r * t[0] // 255, g * t[1] // 255, b * t[2] // 255, a))
    write(out, path)


def main():
    os.makedirs(OUT, exist_ok=True)
    made = []
    for name, (draw, repeat, full) in ASSETS.items():
        img = draw()
        out = os.path.join(OUT, name + '.png')
        write(img, out)
        meta(out + '.meta', repeat, full)
        made.append((name, img))
    print('그림', len(made), '장 저장:', ', '.join(n for n, _ in made))
    if len(sys.argv) > 1:
        preview(made, sys.argv[1])
        print('미리보기:', sys.argv[1])


if __name__ == '__main__':
    main()
