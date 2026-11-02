# 능력 아이콘 범주 틀 (그래픽 개편 I-3 · 2026-11-02) — Assets/Resources/Icons/ability_<번호>.png 만 고침 (.meta 그대로)
#   예전: 능력 아이콘 311장이 모두 같은 둥근 메달(4단 테 · 어두운 원판 2단 · 반사 2칸)이라 HUD 에서 무슨 종류인지 구분이 안 됐음 (그래픽 기획 A9)
#   이제: 메달에서 그림문자만 걷어 내고 범주 틀에 다시 얹음 (UI 기획 3-6 「무기 사각 · 스킬 원 · 패시브 마름모 · 유물 육각」 · 범주색 6 은 틀에만)
#     무기 사각  CatWeapon   — 무기 특수 능력 · 무기 강화(2차 진화) · 공격을 바꾸는 영웅 전용 카드(관통 · 다중 · 밀어내기 · 눈빛 자리)
#     스킬 원    CatSkill    — 스킬 특수 능력 · 영웅 고유 레벨업 스킬 · 영웅 전용 카드(추가 자리)
#                CatUltimate — 설명에 「필살기」가 들어간 영웅 전용 카드
#     패시브 마름모 CatSurvival — 패시브 특수 능력 · 생존 카드(피의 굶주림 자리) · CatWealth 코인 자석 · 비상 보급 · CatSoul 운명 가지
#     유물 육각  CatSoul     — 스킬 진화
#   범주는 C# 정의를 읽어 정함 (SpecialAbilities.Kits KitDefs 의 SpecialKind · LevelShop.Kits / NewHeroes 카드 자리 · 진화 Evo 번호)
#   틀: 바깥 외곽 1칸(K1) · 범주색 선 2칸(위왼 빛 4단) · 안쪽은 범주색이 조금 섞인 어두운 2단 · 그림문자는 틀 선 위에 겹쳐도 그대로 그림
# 실행: py tools/pixelart/frame_icons.py [--rev <원본 커밋>] [--only <번호,...>] [--dry] [--sheet <폴더>]
#   원본은 git 의 메달 그림 (기본 75b724be — 틀을 씌우기 전) → 여러 번 돌려도 같음
import io, math, os, re, subprocess, sys
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
ICONS = 'Assets/Resources/Icons'
SRC_REV = '75b724be'
K1 = (26, 17, 44)
CX = CY = 16.0

CAT = {
    'weapon': (0xC4, 0x8A, 0x5A), 'ultimate': (0x6F, 0xAF, 0xC4), 'skill': (0x7E, 0x8E, 0xD0),
    'survival': (0x84, 0xAE, 0x78), 'soul': (0x9C, 0x86, 0xCF), 'wealth': (0xC6, 0xAF, 0x6A),
}


# ------------------------------------------------------------------ 범주 (C# 정의에서)
def cs(rel):
    return open(os.path.join(ROOT, rel), encoding='utf-8').read()


def categories():
    out = {}
    # 특수 능력 (번호 20 ~): KitDefs 순서 = 번호 - 20 · 아이콘 = 번호 < 60 ? 번호 : 400 + 번호
    #   2.0.0~ 두 번째 대역 KitDefs2 (번호 140 ~ · 아이콘 540 ~)
    kits = cs('Assets/Scripts/SpecialAbilities.Kits.cs')
    first = kits.split('KitDefs =', 1)[1].split('};', 1)[0]
    second = kits.split('KitDefs2 =', 1)[1].split('};', 1)[0] if 'KitDefs2 =' in kits else ''
    shapes = {'Weapon': ('square', 'weapon'), 'Skill': ('circle', 'skill'), 'Passive': ('diamond', 'survival')}
    for base, block in ((20, first), (140, second)):
        for i, k in enumerate(re.findall(r'SpecialKind\.(\w+)', block)):
            sid = base + i
            out[sid if sid < 60 else 400 + sid] = shapes[k]

    def kit(slot, desc):
        if desc is not None and '필살기' in desc: return ('circle', 'ultimate')
        if slot in ('PierceId', 'MultiId', 'KnockId', 'GlareId'): return ('square', 'weapon')
        if slot == 'HungerId': return ('diamond', 'survival')
        return ('circle', 'skill')

    # 첫 영웅들의 전용 카드 (60 ~ 91): 카드에 아이콘 번호가 적혀 있음
    for m in re.finditer(r'(\w+Id) => (?:Zero\("[^"]*", )?Card\("[^"]*", "([^"]*)", (\d+)', cs('Assets/Scripts/LevelShop.Kits.cs')):
        out[int(m.group(3))] = kit(m.group(1), m.group(2))
    # 새 영웅 전용 카드: 700 + 영웅 칸 × 10 + 자리 (LevelShop.NewHeroes)
    nh = cs('Assets/Scripts/LevelShop.NewHeroes.cs')
    body = nh.split('NewHeroSigCard', 1)[0]
    slots = ['PierceId', 'MultiId', 'KnockId', 'GlareId', 'ExtraAId']
    for h, block in enumerate(re.split(r'CharacterId\.\w+ => id switch', body)[1:]):
        for m in re.finditer(r'(\w+Id) => [^\n]*?Card\("[^"]*", "([^"]*)"', block):
            slot = slots.index(m.group(1)) if m.group(1) in slots else 5
            out[700 + h * 10 + slot] = kit(m.group(1), m.group(2))
    # 무기 강화 (2차 진화): 92 ~ 103 · 600 ~ 611
    for n in list(range(92, 104)) + list(range(600, 627)): out[n] = ('square', 'weapon')
    # 코인 자석 · 비상 보급 / 운명 가지
    out[104] = ('diamond', 'wealth'); out[85] = out.get(85, ('diamond', 'wealth'))
    for n in range(105, 112): out[n] = ('diamond', 'soul')
    # 영웅 고유 레벨업 스킬: 120 ~ 169 · 800 ~ 839
    for n in list(range(120, 170)) + list(range(800, 900)): out[n] = ('circle', 'skill')
    # 스킬 진화
    for n in re.findall(r'Evo\("[^"]+", "[^"]+", "[^"]*", (\d+)', cs('Assets/Scripts/LevelShop.Signature.cs')):
        out[int(n)] = ('hex', 'soul')
    return out


# ------------------------------------------------------------------ 틀 모양 (칸 가운데 기준 · 안쪽으로 몇 칸인지)
def depth(shape, x, y):
    dx, dy = abs(x + 0.5 - CX), abs(y + 0.5 - CY)
    if shape == 'circle':
        return 15.2 - math.hypot(dx, dy)
    if shape == 'square':
        if dx > 14.9 and dy > 14.9: return -1          # 모서리 1칸 깎음
        return 16.0 - max(dx, dy)
    if shape == 'diamond':
        return (16.6 - dx - dy) / 1.25
    if shape == 'hex':                                  # 위아래 평평 · 좌우 뾰족
        return min(15.6 - dy, (16.4 - dx - dy * 0.5) / 1.12)
    raise ValueError(shape)


def mix(a, b, t):
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


def lum(c):
    return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]


def frame(shape, cat):
    c = CAT[cat]
    line = [mix(c, (255, 255, 255), 0.38), c, mix(c, (0, 0, 0), 0.28), mix(c, (0, 0, 0), 0.5)]
    if shape == 'diamond':          # 마름모는 빗변이라 같은 두께여도 가늘고 어두워 보임 → 한 단 밝게 · 선 두껍게
        line = [mix(c, (255, 255, 255), 0.5), mix(c, (255, 255, 255), 0.2), c, mix(c, (0, 0, 0), 0.3)]
    band = 3.6 if shape == 'diamond' else 3.0
    inn = [mix(c, (22, 17, 32), 0.8), mix(c, (14, 11, 22), 0.86)]
    px = {}
    for y in range(32):
        for x in range(32):
            d = depth(shape, x, y)
            if d <= 0: continue
            dx, dy = x + 0.5 - CX, y + 0.5 - CY
            if d <= 1.0:
                px[(x, y)] = K1
            elif d <= band:
                n = math.hypot(dx, dy) or 1
                lit = (-dx - dy) / (n * 1.414)
                px[(x, y)] = line[0] if lit > 0.75 else line[1] if lit > 0.15 else line[2] if lit > -0.6 else line[3]
            else:
                k = (dy + math.hypot(dx, dy) * 0.3) / 12.4
                px[(x, y)] = inn[0] if k < -0.1 else inn[1]
    return px


# ------------------------------------------------------------------ 메달 → 그림문자
def git_png(rev, rel):
    data = subprocess.run(['git', 'show', '%s:%s' % (rev, rel)], cwd=ROOT, capture_output=True, check=True).stdout
    return Image.open(io.BytesIO(data)).convert('RGBA')


def glyph_of(im):
    w, h = im.size
    if (w, h) != (32, 32): return None, '크기 %dx%d' % (w, h)
    P = im.load()
    rim = [(x, y) for y in range(32) for x in range(32) if 12.8 < math.hypot(x + 0.5 - CX, y + 0.5 - CY) < 14.6]
    if sum(1 for p in rim if P[p][3] > 0) < 0.95 * len(rim): return None, '메달 아님'
    # 원판 두 색 = 테 바로 안쪽 고리에서 가장 많은 두 색 (외곽색 제외)
    cnt = {}
    for y in range(32):
        for x in range(32):
            if 9.0 < math.hypot(x + 0.5 - CX, y + 0.5 - CY) < 12.3 and P[x, y][3] > 0 and lum(P[x, y]) >= 30:
                cnt[P[x, y][:3]] = cnt.get(P[x, y][:3], 0) + 1
    tones = [c for c, _ in sorted(cnt.items(), key=lambda kv: -kv[1])[:2]]
    if not tones or sum(cnt[c] for c in tones) < 0.25 * sum(cnt.values()): return None, '원판 색 못 찾음'
    disc = {(x, y) for y in range(32) for x in range(32) if math.hypot(x + 0.5 - CX, y + 0.5 - CY) < 12.4 and P[x, y][3] > 0}
    # 원판 바탕 = 원판 두 색 중 원판 가장자리에서 4방향으로 이어진 칸 (그림문자 안에 같은 색이 있어도 지우지 않게)
    #   그림문자에 둘러싸인 원판색 칸은 바탕이 비쳐 보이던 틈 — 12칸 넘게 크면 바탕, 작으면 그림문자 쪽으로 둠
    tone = {p for p in disc if P[p][:3] in tones}
    bg, st = set(), [p for p in tone if math.hypot(p[0] + 0.5 - CX, p[1] + 0.5 - CY) >= 11.0]
    bg.update(st)
    while st:
        x, y = st.pop()
        for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if q in tone and q not in bg: bg.add(q); st.append(q)
    rest = tone - bg
    while rest:
        p = rest.pop(); comp, st = {p}, [p]
        while st:
            x, y = st.pop()
            for q in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if q in rest: rest.discard(q); comp.add(q); st.append(q)
        if len(comp) > 12: bg |= comp
    g = {p: P[p][:3] for p in disc if p not in bg}
    # 반사 2칸 (흰색 · 그림문자에 4방향으로 붙지 않은 것)
    for p in ((8, 6), (7, 7)):
        if g.get(p) == (255, 255, 255):
            if not any((p[0] + dx, p[1] + dy) in g and (p[0] + dx, p[1] + dy) not in ((8, 6), (7, 7))
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))):
                del g[p]
    # 외곽색만으로 된 조각 (원판 무늬 · 테 안쪽 금) 은 버림
    seen = set()
    for p in list(g):
        if p in seen: continue
        comp, st = [], [p]
        seen.add(p)
        while st:
            q = st.pop(); comp.append(q)
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    r = (q[0] + dx, q[1] + dy)
                    if r in g and r not in seen: seen.add(r); st.append(r)
        if all(lum(g[q]) < 41 for q in comp):
            for q in comp: del g[q]
    return g, 'ok'


def render(shape, cat, g):
    px = frame(shape, cat)
    # 그림문자 둘레의 빈칸(틀 안쪽)에 외곽 1칸 — 원래 메달에서 외곽이 원판색이었던 자리
    for (x, y), c in g.items():
        if lum(c) < 41: continue
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            q = (x + dx, y + dy)
            if q not in g and q in px and depth(shape, *q) > (3.6 if shape == 'diamond' else 3.0): px[q] = K1
    px.update(g)
    im = Image.new('RGBA', (32, 32), (0, 0, 0, 0))
    for (x, y), c in px.items(): im.putpixel((x, y), c + (255,))
    return im


def sheet(path, items, scale=4, cols=12):
    cw = 32 * scale + 8
    rows = (len(items) + cols - 1) // cols
    S = Image.new('RGBA', (cols * cw, rows * cw * 2), (64, 64, 74, 255))
    for k, (_, a, b) in enumerate(items):
        ox, oy = (k % cols) * cw + 4, (k // cols) * cw * 2 + 4
        for j, im in enumerate((a, b)):
            bg = Image.new('RGBA', (32 * scale, 32 * scale), (20, 15, 31, 255))
            bg.alpha_composite(im.resize((32 * scale, 32 * scale), Image.NEAREST))
            S.paste(bg, (ox, oy + j * cw))
    S.save(path)


def main():
    a = sys.argv
    rev = a[a.index('--rev') + 1] if '--rev' in a else SRC_REV
    only = set(int(n) for n in a[a.index('--only') + 1].split(',')) if '--only' in a else None
    sheet_dir = a[a.index('--sheet') + 1] if '--sheet' in a else None
    dry = '--dry' in a
    cats = categories()
    names = subprocess.run(['git', 'ls-tree', '--name-only', rev, ICONS + '/'], cwd=ROOT, capture_output=True, text=True, check=True).stdout.split()
    nums = sorted(int(m.group(1)) for n in names for m in [re.match(r'.*/ability_(\d+)\.png$', n)] if m)
    done, skipped, nocat, items, per = 0, [], [], [], {}
    for n in nums:
        if only and n not in only: continue
        rel = '%s/ability_%d.png' % (ICONS, n)
        if n not in cats: nocat.append(n); continue
        src = git_png(rev, rel)
        g, note = glyph_of(src)
        if g is None: skipped.append((n, note)); continue
        shape, cat = cats[n]
        out = render(shape, cat, g)
        per[(shape, cat)] = per.get((shape, cat), 0) + 1
        items.append((n, src, out))
        done += 1
        if not dry: out.save(os.path.join(ROOT, rel))
    print('틀 씌움:', done, ' 범주별:', ', '.join('%s/%s %d' % (s, c, k) for (s, c), k in sorted(per.items())))
    if skipped: print('건너뜀 (메달이 아님):', ', '.join('%d(%s)' % s for s in skipped))
    if nocat: print('범주 없음 (그대로):', ', '.join(map(str, nocat)))
    if sheet_dir:
        os.makedirs(sheet_dir, exist_ok=True)
        for i in range(0, len(items), 96):
            sheet(os.path.join(sheet_dir, 'frames_%02d.png' % (i // 96 + 1)), items[i:i + 96])


if __name__ == '__main__':
    main()
