# 무기 스킨 형태 시트 (스킨 시청각 개편 2단계 · 기획 3-2 W2 · 2-3 L1~L3)
#   원래 평타 시트(atk_* · fx_swordswing · fx_alchemyblast · fx_beam · …)를 읽어 「<원래 이름>__<스킨 id>」 시트를 만듦
#   알파는 칸 하나도 바꾸지 않음 → 몸통 상자(L1) · 바깥 테(L3) · 프레임 크기가 원래와 같음 = 판정 그대로
#   바뀌는 것: 안쪽 색 램프(스킨 색) + 스킨마다 무늬(새김선 · 불티 · 결정면 · 금 · 반짝 · 나뭇결 · 먹 번짐 · 분필 가루 …)
#   진화 단계 시트(_t2 · _t3)가 있는 검 · 플라스크는 단계 시트도 같은 방식으로 (R2 — 진화 광채가 사라지지 않게)
#   .meta 는 원래 시트 .meta 를 복사해 guid · 스프라이트 이름만 바꿈 (자르기 · 피벗 · 칸 크기 그대로)
# 실행: python tools/pixelart/make_skin_weapons.py  (끝에 알파 대조 검사 — 다르면 실패)
import os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, read, write

FX = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'FX')

# 스킨 → (원래 시트들, 주색, 보조색, 무늬)
SKINS = {
    'pistol__silver':      (['atk_bullet'], (205, 230, 255), (120, 140, 170), 'lines'),
    'pistol__dragon':      (['atk_bullet'], (255, 140, 50), (200, 40, 20), 'flecks'),
    'sword__frost':        (['fx_swordswing', 'fx_swordswing_t2', 'fx_swordswing_t3'], (150, 230, 255), (230, 250, 255), 'facets'),
    'sword__obsidian':     (['fx_swordswing', 'fx_swordswing_t2', 'fx_swordswing_t3'], (165, 90, 255), (40, 20, 70), 'cracks'),
    'shuriken__gold':      (['atk_shuriken'], (255, 216, 90), (255, 250, 200), 'sparkle'),
    'shuriken__bloodmoon': (['atk_shuriken'], (255, 64, 76), (90, 10, 20), 'grad'),
    'bow__silver':         (['atk_arrow'], (216, 235, 255), (140, 160, 190), 'lines'),
    'bow__worldtree':      (['atk_arrow'], (128, 255, 140), (60, 120, 40), 'wood'),
    'flask__crystal':      (['atk_flask', 'fx_alchemyblast', 'fx_alchemyblast_t2', 'fx_alchemyblast_t3'], (180, 230, 255), (255, 255, 255), 'facets'),
    'flask__lava':         (['atk_flask', 'fx_alchemyblast', 'fx_alchemyblast_t2', 'fx_alchemyblast_t3'], (255, 128, 38), (120, 20, 10), 'flecks'),
    'quill__brush':        (['fx_beam'], (76, 90, 166), (20, 22, 40), 'ink'),
    'quill__chalk':        (['fx_beam'], (242, 242, 255), (200, 200, 215), 'chalk'),
    'gear__coin':          (['atk_gear'], (255, 210, 76), (255, 248, 200), 'sparkle'),
    'gear__abacus':        (['atk_gear'], (190, 115, 64), (100, 55, 25), 'wood'),
    'letter__plane':       (['atk_letter'], (216, 230, 255), (255, 255, 255), 'lines'),
    'letter__bill':        (['atk_letter'], (230, 50, 76), (255, 230, 230), 'grad'),
    'lantern__candle':     (['fx_beam'], (255, 204, 102), (255, 245, 210), 'flecks'),
    'lantern__chain':      (['fx_beam'], (255, 153, 50), (90, 80, 80), 'links'),
    'satba__rope':         (['fx_throwtrail'], (216, 178, 102), (130, 95, 45), 'wood'),
    'satba__silk':         (['fx_throwtrail'], (242, 115, 166), (255, 220, 120), 'sparkle'),
    # 공사 반장: 평타 그림 시트가 따로 없어 삽날 밀기 틱마다 보이는 흙먼지(fx_dust)를 같은 원칙으로 (2026-11-04 마무리)
    'plow__broom':         (['fx_dust'], (230, 191, 102), (160, 128, 58), 'wood'),
    'plow__pitchfork':     (['fx_dust'], (204, 217, 242), (255, 154, 64), 'flecks'),
    'dummy__bear':         (['fx_puppet_swing'], (178, 128, 76), (240, 225, 200), 'chalk'),
    'dummy__wood':         (['fx_puppet_swing'], (216, 166, 102), (120, 80, 40), 'wood'),
    'shovel__pickaxe':     (['fx_dirt'], (178, 190, 216), (90, 95, 110), 'cracks'),
    'shovel__spoon':       (['fx_dirt'], (230, 242, 255), (255, 255, 255), 'sparkle'),
    'camera__disposable':  (['fx_flashframe'], (242, 200, 115), (255, 250, 230), 'grad'),
    'camera__film':        (['fx_flashframe'], (242, 204, 102), (255, 255, 240), 'flecks'),
}


def h(x, y, s=0):
    v = (x * 73856093) ^ (y * 19349663) ^ (s * 83492791)
    v = (v ^ (v >> 13)) * 1274126177
    return ((v ^ (v >> 16)) & 0xffff) / 65535.0


def pattern(kind, x, y, w, hgt):
    """-1 ~ +1 밝기 변조"""
    if kind == 'lines':
        return 0.35 if (x + y) % 4 == 0 else 0.0
    if kind == 'flecks':
        return 0.8 if h(x, y, 1) > 0.86 else -0.1
    if kind == 'facets':
        return 0.3 if ((x // 2) + (y // 2)) % 2 == 0 else -0.15
    if kind == 'cracks':
        return -0.6 if abs((x * 2 + y * 3) % 11 - 5) == 0 and h(x // 3, y // 3, 2) > 0.4 else 0.05
    if kind == 'sparkle':
        return 0.9 if h(x, y, 3) > 0.9 else 0.1
    if kind == 'grad':
        return 0.35 - 0.7 * (y / max(1, hgt - 1))
    if kind == 'wood':
        return 0.25 if (y + int(2 * h(x // 4, 0, 4))) % 3 == 0 else -0.1
    if kind == 'ink':
        return -0.35 if h(x // 2, y // 2, 5) > 0.55 else 0.15
    if kind == 'chalk':
        return 0.4 if h(x, y, 6) > 0.6 else -0.2
    if kind == 'links':
        return 0.35 if (x % 6) in (0, 1) else -0.1
    return 0.0


def lerp(a, b, t):
    return tuple(int(round(p + (q - p) * t)) for p, q in zip(a, b))


def recolor(src, main, acc, kind):
    out = Img(src.w, src.h)
    dark = tuple(int(c * 0.28) for c in acc)
    for y in range(src.h):
        for x in range(src.w):
            r, g, b, a = src.get(x, y)
            if a == 0:
                continue
            L = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0
            if L < 0.13:            # 어두운 외곽선은 그대로 (윤곽 · 몸통 대비 유지)
                out.set(x, y, (r, g, b, a))
                continue
            L2 = max(0.0, min(1.0, L * (1.0 + 0.45 * pattern(kind, x, y, src.w, src.h))))
            if L2 < 0.6:
                c = lerp(dark, main, L2 / 0.6)
            else:
                c = lerp(main, (255, 255, 255) if L2 > 0.9 else acc, (L2 - 0.6) / 0.4 * 0.7)
            out.set(x, y, (c[0], c[1], c[2], a))      # 알파 그대로 (L1 · L3)
    return out


def copy_meta(base, name):
    src = os.path.join(FX, base + '.png.meta')
    dst = os.path.join(FX, name + '.png.meta')
    if os.path.exists(dst):
        return              # 이미 있으면 guid 유지
    t = open(src, encoding='utf-8').read()
    t = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, t, count=1)
    t = re.sub(r'(\b)' + re.escape(base) + r'_(\d+)\b', lambda m: name + '_' + m.group(2), t)
    open(dst, 'w', encoding='utf-8', newline='\n').write(t)


def out_name(base, skin):
    m = re.match(r'^(.*?)(_t\d)$', base)
    return (m.group(1) + '__' + skin + m.group(2)) if m else (base + '__' + skin)


def main():
    bad = 0
    for skin, (bases, main_c, acc, kind) in SKINS.items():
        for base in bases:
            p = os.path.join(FX, base + '.png')
            if not os.path.exists(p):
                print('없음 (건너뜀):', base)
                continue
            src = read(p)
            img = recolor(src, main_c, acc, kind)
            name = out_name(base, skin)
            write(img, os.path.join(FX, name + '.png'))
            copy_meta(base, name)
            # 검사: 알파가 칸 하나라도 다르면 실패 (판정 몸통 · 바깥 테 · 물체 상자가 같아야 — L1 · L3 · L11)
            chk = read(os.path.join(FX, name + '.png'))
            if chk.w != src.w or chk.h != src.h or any(chk.get(x, y)[3] != src.get(x, y)[3] for y in range(src.h) for x in range(src.w)):
                print('알파 불일치:', name)
                bad += 1
            else:
                print('wrote', name)
    if bad:
        sys.exit(1)


if __name__ == '__main__':
    main()
