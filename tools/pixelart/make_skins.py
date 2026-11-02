# 스킨 도트 (1.8.3~): 캐릭터 스킨 20 · 무기 스킨 10 · 이펙트 스킨 미리보기 4
#   캐릭터: Assets/Resources/Characters/<몸>__<스킨>.png   (거너는 gunner_nogun__<스킨>.png, 프레임 이름 그대로)
#   무기:   Assets/Resources/Weapons/weapon_<무기>__<스킨>.png  (원래 무기 메타를 복사 → 잡는 자리 그대로)
#   이펙트: Assets/Resources/Icons/skin_<스킨>.png
# 몸 크기 · 실루엣은 그대로 두고 색 · 장식만 바꿈 (게임 판정에 영향 없음)
# 실행: python tools/pixelart/make_skins.py
import colorsys, os, re, sys, uuid
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, read, write
from make_fx import disc, ring, line, ROOT
import make_characters as mc

CHARS = mc.CHARS
WEAP = os.path.join(ROOT, 'Assets', 'Resources', 'Weapons')
ICONS = os.path.join(ROOT, 'Assets', 'Resources', 'Icons')


def retry(fn):
    # 편집기가 열려 있으면 새 파일을 잠깐 잡고 있을 수 있어 몇 번 다시 시도
    import time
    for i in range(20):
        try:
            return fn()
        except PermissionError:
            time.sleep(0.5)
    return fn()


def copy_meta(src_meta, dst_meta):
    s = open(src_meta, encoding='utf-8').read()
    s = re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + uuid.uuid4().hex, s, count=1)
    retry(lambda: open(dst_meta, 'w', encoding='utf-8', newline='\n').write(s))


# ------------------------------------------------------------------ 다른 캐릭터: 팔레트 + 장식 줄
# (몸, 팔레트 덮어쓰기, 몸통 줄 덮어쓰기 {줄 번호: 새 줄})
KIT_SKINS = {
    # 검사: G 갑옷 밝음 · g 갑옷 어둠 · r 깃털 · b 겉옷 · y 금장 · c 망토 · m 허리띠 · L 다리 · B 장화
    'swordsman__bronze': ('swordsman', {'G': (205, 150, 90), 'g': (140, 95, 55), 'b': (120, 80, 50), 'm': (170, 120, 70), 'c': (110, 60, 35), 'r': (230, 170, 60)}, {}),
    'swordsman__black': ('swordsman', {'G': (80, 78, 92), 'g': (45, 42, 55), 'b': (30, 28, 38), 'y': (200, 40, 45), 'c': (150, 20, 30), 'r': (220, 30, 40), 'm': (60, 58, 70), 'L': (40, 38, 48)}, {}),
    'swordsman__paladin': ('swordsman', {'G': (240, 240, 250), 'g': (190, 190, 210), 'b': (245, 245, 250), 'y': (240, 200, 80), 'c': (70, 110, 200), 'r': (240, 200, 80), 'm': (220, 180, 90), 'L': (200, 200, 215), 'B': (150, 120, 70)},
                           {0: 'yyy', 1: 'yyyy'}),
    'swordsman__dragon': ('swordsman', {'G': (60, 30, 35), 'g': (35, 18, 22), 'b': (170, 30, 25), 'y': (250, 150, 40), 'c': (220, 70, 20), 'r': (250, 120, 30), 'm': (120, 30, 25), 'L': (50, 25, 28), 'B': (35, 18, 20)},
                          {0: 'r....r', 1: 'rr..rr'}),
    # 도적: h 두건 어둠 · H 두건 밝음 · m 가면 · e 눈 · r 목도리 · l 가죽 · d 칼 · L 다리 · B 장화
    'rogue__sand': ('rogue', {'h': (150, 115, 70), 'H': (195, 160, 105), 'm': (90, 70, 45), 'r': (200, 120, 60), 'l': (130, 95, 60), 'L': (110, 85, 55)}, {}),
    'rogue__raven': ('rogue', {'h': (28, 26, 32), 'H': (55, 52, 62), 'm': (15, 14, 18), 'e': (245, 245, 255), 'r': (190, 20, 35), 'l': (40, 36, 44), 'L': (30, 28, 34), 'B': (20, 18, 22)}, {}),
    'rogue__ninja': ('rogue', {'h': (30, 38, 70), 'H': (50, 62, 105), 'm': (20, 25, 45), 'e': (255, 250, 230), 'r': (220, 40, 45), 'l': (35, 44, 80), 'd': (200, 205, 220), 'L': (28, 34, 60), 'B': (18, 22, 40)},
                    {7: 'krrrrrrrrrrk'}),
    'rogue__void': ('rogue', {'h': (45, 15, 70), 'H': (85, 35, 130), 'm': (20, 5, 35), 'e': (255, 80, 230), 'r': (140, 40, 220), 'l': (50, 20, 80), 'd': (220, 150, 255), 'L': (35, 12, 55), 'B': (25, 8, 40)}, {}),
    # 궁수: n 두건 어둠 · N 두건 밝음 · t 옷 · T 옷 밝음 · q 화살통 · f 깃 · y 허리띠 · L 다리 · B 장화
    'archer__autumn': ('archer', {'n': (170, 80, 30), 'N': (220, 130, 50), 't': (130, 70, 35), 'T': (170, 100, 50), 'f': (240, 200, 90), 'L': (100, 60, 35)}, {}),
    'archer__snow': ('archer', {'n': (200, 220, 240), 'N': (240, 248, 255), 't': (150, 180, 215), 'T': (190, 215, 240), 'q': (110, 130, 160), 'f': (140, 200, 255), 'y': (180, 220, 255), 'L': (120, 140, 170), 'B': (90, 105, 130)}, {}),
    'archer__elf': ('archer', {'n': (25, 120, 80), 'N': (60, 190, 120), 't': (30, 95, 70), 'T': (50, 140, 95), 'q': (140, 100, 40), 'f': (240, 210, 90), 'y': (245, 205, 80), 'L': (30, 75, 55), 'B': (95, 70, 35)},
                    {1: 'yyyyyf'}),
    'archer__sun': ('archer', {'n': (230, 150, 30), 'N': (255, 215, 90), 't': (245, 235, 210), 'T': (255, 250, 235), 'q': (200, 120, 30), 'f': (255, 240, 150), 'y': (255, 180, 40), 'L': (200, 140, 50), 'B': (150, 95, 35)}, {}),
    # 연금술사: p 옷 · P 옷 밝음 · o 고글 알 · O 고글 테 · a 머리 · v · V 물약 · L 다리 · B 장화
    'alchemist__herb': ('alchemist', {'p': (45, 110, 60), 'P': (80, 160, 90), 'v': (240, 220, 90), 'V': (120, 200, 255), 'L': (40, 80, 50)}, {}),
    'alchemist__plague': ('alchemist', {'p': (28, 26, 32), 'P': (55, 52, 62), 'a': (25, 22, 28), 'o': (200, 30, 40), 'O': (235, 230, 215), 'v': (130, 200, 90), 'V': (200, 40, 40), 'L': (30, 28, 34), 'B': (20, 18, 22)},
                          {5: 'kOOOOOOk', 6: 'kkOOOOkk'}),
    'alchemist__mercury': ('alchemist', {'p': (140, 150, 165), 'P': (200, 210, 225), 'a': (230, 235, 245), 'o': (80, 240, 255), 'O': (60, 65, 80), 'v': (190, 255, 255), 'V': (150, 160, 255), 'L': (90, 95, 110), 'B': (55, 58, 70)}, {}),
    'alchemist__stone': ('alchemist', {'p': (140, 20, 35), 'P': (200, 45, 55), 'a': (250, 210, 90), 'o': (255, 80, 60), 'O': (240, 190, 70), 'v': (255, 60, 50), 'V': (255, 200, 60), 'L': (90, 15, 25), 'B': (60, 12, 18)}, {}),
}


def kit_skins():
    for sid, (body, pal_over, rows_over) in KIT_SKINS.items():
        pal, upper = mc.BODIES[body]
        pal = dict(pal)
        pal.update(pal_over)
        upper = list(upper)
        for i, r in rows_over.items():
            upper[i] = r
        frames = [mc.body_frame(pal, upper, 'idle'), mc.body_frame(pal, upper, 'idle', 1),
                  mc.body_frame(pal, upper, 'run1'), mc.body_frame(pal, upper, 'run2', -1),
                  mc.body_frame(pal, upper, 'run3'), mc.body_frame(pal, upper, 'run2', -1)]
        sheet = Img(32 * len(frames), 32)
        for i, f in enumerate(frames):
            sheet.paste(f, i * 32, 0)
        out = os.path.join(CHARS, sid + '.png')
        retry(lambda: mc.meta_frames(sid, len(frames), out + '.meta'))
        retry(lambda: write(sheet, out))
        print('wrote', sid)


# ------------------------------------------------------------------ 거너: 원래 그림의 색 묶음을 밝기 순서대로 새 색으로
G_TEAL = [(31, 143, 153), (63, 214, 223), (154, 204, 209), (168, 236, 240), (184, 251, 255)]
G_COAT = [(107, 74, 47), (140, 100, 64)]
G_SHIRT = [(177, 62, 83)]
G_GOLD = [(199, 154, 69), (229, 209, 171)]
G_PANTS = [(43, 36, 51), (44, 38, 50), (64, 55, 74)]

GUNNER_SKINS = {
    'desert': {'teal': [(160, 130, 80), (215, 190, 120), (230, 210, 150), (240, 225, 170), (248, 238, 200)],
               'coat': [(150, 110, 70), (185, 145, 95)], 'shirt': [(190, 110, 60)]},
    'bounty': {'teal': [(150, 30, 40), (230, 70, 80), (240, 150, 150), (250, 175, 175), (255, 200, 200)],
               'coat': [(50, 40, 45), (75, 62, 68)], 'shirt': [(200, 40, 50)]},
    'ghost': {'teal': [(150, 220, 230), (230, 255, 255), (220, 245, 250), (235, 252, 255), (250, 255, 255)],
              'coat': [(150, 170, 190), (185, 200, 215)], 'shirt': [(70, 200, 200)], 'pants': [(70, 80, 100), (72, 82, 102), (95, 108, 130)]},
    'gold': {'teal': [(200, 150, 40), (255, 230, 120), (250, 230, 160), (255, 240, 190), (255, 250, 215)],
             'coat': [(210, 165, 60), (240, 200, 90)], 'shirt': [(240, 240, 250)], 'gold': [(255, 250, 230), (255, 255, 245)],
             'pants': [(40, 40, 70), (42, 42, 72), (60, 60, 100)]},
}


def gunner_skins():
    src = os.path.join(CHARS, 'gunner_nogun.png')
    im = read(src)
    groups = {'teal': G_TEAL, 'coat': G_COAT, 'shirt': G_SHIRT, 'gold': G_GOLD, 'pants': G_PANTS}
    for sid, spec in GUNNER_SKINS.items():
        m = {}
        for g, targets in spec.items():
            for a, b in zip(groups[g], targets):
                m[a] = b
        out = Img(im.w, im.h)
        for y in range(im.h):
            for x in range(im.w):
                p = im.px[y][x]
                c = m.get(tuple(p[:3]))
                out.px[y][x] = (c[0], c[1], c[2], p[3]) if c and p[3] > 0 else p
        path = os.path.join(CHARS, 'gunner_nogun__%s.png' % sid)
        copy_meta(src + '.meta', path + '.meta')
        retry(lambda: write(out, path))
        print('wrote gunner', sid)


# ------------------------------------------------------------------ 무기: 윤곽선은 두고 색상 · 채도 · 밝기를 바꿈
# (hue 또는 None, 채도 배율, 밝기 배율, 금속(채도 낮은 부분)도 칠할지)
WEAPON_SKINS = {
    'pistol__silver': ('pistol', 0.58, 0.35, 1.25, True),
    'pistol__dragon': ('pistol', 0.03, 1.4, 1.1, True),
    'sword__frost': ('sword', 0.53, 0.9, 1.2, True),
    'sword__obsidian': ('sword', 0.78, 0.8, 0.6, True),
    'shuriken__gold': ('shuriken', 0.12, 1.1, 1.25, True),
    'shuriken__bloodmoon': ('shuriken', 0.98, 1.2, 0.9, True),
    'bow__silver': ('bow', 0.6, 0.25, 1.3, True),
    'bow__worldtree': ('bow', 0.33, 1.0, 1.05, False),
    'flask__crystal': ('flask', 0.55, 0.6, 1.25, True),
    'flask__lava': ('flask', 0.06, 1.3, 1.1, True),
}


def weapon_skins():
    for sid, (base, hue, smul, vmul, metal) in WEAPON_SKINS.items():
        src = os.path.join(WEAP, 'weapon_%s.png' % base)
        im = read(src)
        out = Img(im.w, im.h)
        for y in range(im.h):
            for x in range(im.w):
                p = im.px[y][x]
                if p[3] == 0:
                    out.px[y][x] = p
                    continue
                h, s, v = colorsys.rgb_to_hsv(p[0] / 255, p[1] / 255, p[2] / 255)
                if v < 0.18:                        # 윤곽선
                    out.px[y][x] = p
                    continue
                if s < 0.15 and not metal:
                    out.px[y][x] = p
                    continue
                nh = hue if hue is not None else h
                ns = min(1.0, max(s, 0.25 if s < 0.15 else s) * smul)
                nv = min(1.0, v * vmul)
                r, g, b = colorsys.hsv_to_rgb(nh, ns, nv)
                out.px[y][x] = (int(r * 255), int(g * 255), int(b * 255), p[3])
        path = os.path.join(WEAP, 'weapon_%s.png' % sid)
        copy_meta(src + '.meta', path + '.meta')
        retry(lambda: write(out, path))
        print('wrote weapon', sid)


# ------------------------------------------------------------------ 이펙트 스킨 미리보기 아이콘 (64x64)
def effect_icons():
    specs = {
        'skin_bluefire': [(80, 170, 255), (180, 230, 255)],
        'skin_sakura': [(255, 150, 190), (255, 220, 235)],
        'skin_thunder': [(255, 230, 80), (255, 255, 200)],
        'skin_starlight': [(255, 120, 120), (255, 220, 120), (120, 255, 160), (120, 190, 255), (220, 140, 255)],
    }
    for name, cols in specs.items():
        img = Img(64, 64)
        disc(img, 32, 32, 30, (30, 24, 40, 255))
        ring(img, 32, 32, 30, 2, (15, 10, 20, 255))
        if name == 'skin_sakura':
            import math
            for i in range(5):
                a = i * 2 * math.pi / 5
                disc(img, 32 + math.cos(a) * 12, 32 + math.sin(a) * 12, 8, cols[0] + (255,))
            disc(img, 32, 32, 6, cols[1] + (255,))
        elif name == 'skin_thunder':
            line(img, 38, 8, 26, 32, cols[0] + (255,), 4); line(img, 26, 32, 40, 32, cols[0] + (255,), 4); line(img, 40, 32, 28, 56, cols[0] + (255,), 4)
            line(img, 38, 8, 26, 32, cols[1] + (255,), 1.5); line(img, 40, 32, 28, 56, cols[1] + (255,), 1.5)
        elif name == 'skin_starlight':
            import math
            for i, c in enumerate(cols):
                a = i * 2 * math.pi / len(cols) - math.pi / 2
                x, y = 32 + math.cos(a) * 16, 32 + math.sin(a) * 16
                line(img, x - 5, y, x + 5, y, c + (255,), 2); line(img, x, y - 5, x, y + 5, c + (255,), 2)
            line(img, 20, 32, 44, 32, (255, 255, 240, 255), 3); line(img, 32, 20, 32, 44, (255, 255, 240, 255), 3)
        else:
            for i in range(10):
                import math
                a = i * 2 * math.pi / 10
                line(img, 32, 32, 32 + math.cos(a) * 24, 32 + math.sin(a) * 24, cols[0] + (255,), 3)
            disc(img, 32, 32, 8, cols[1] + (255,))
        path = os.path.join(ICONS, name + '.png')
        copy_meta(os.path.join(ICONS, 'ability_85.png.meta'), path + '.meta')
        retry(lambda: write(img, path))
        print('wrote icon', name)


if __name__ == '__main__':
    kit_skins()
    gunner_skins()
    weapon_skins()
    effect_icons()
    import make_skins2          # 2.0.0~ 새 영웅 넷 스킨 (캐릭터 16 · 무기 8)
    make_skins2.run()
    import make_skins3          # 2.1.0~ 새 영웅 둘(씨름꾼 · 공사 반장) 스킨 (캐릭터 8 · 무기 4)
    make_skins3.run()
