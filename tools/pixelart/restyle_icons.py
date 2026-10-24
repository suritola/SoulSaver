# 아이콘 화소 규칙 통일 (그래픽 개편 2단계 묶음 7 · 2026-10-25) — Assets/Resources/Icons/** PNG 만 고침 (.meta 그대로)
#   규칙 (기획 4-1): 알파 0/255 만 · 바깥 외곽 1칸(가장자리 Y < 41 ≥ 97% · 두께 ≤ 25%) · 떠 있는 섬(≤ 16칸 — 반짝이 · 불티) 없음
#                    · 색 수 물건형 ≤ 16 / 메달형 ≤ 20
#   물건형 (틀 없음): choice_* · choice_brand_* · 그 밖 (menu · prop · cursor · potion · drop · skin · shop)
#       → 반투명 칸 정리 → 떨어진 섬 지움 → 빠진 외곽 채움(빈칸에 1칸, 캔버스 끝이면 그 칸을 외곽색으로) → 색 줄이기
#   메달형: Paths/* · choice_tr_* 는 이미 평면 메달이라 공통 규칙만
#           ability_* (유리 구슬) 은 --medal 경로: 그림문자 분리 → 갈래 아이콘과 같은 테(4단) · 어두운 원판(2단) · 반사 2칸으로 다시 그림
#       그림문자 = 원판 바탕(테 안쪽 고리에서 색 거리로 채워 찾음)을 뺀 나머지 → tools/pixelart/out/glyph/ 에 중간 산출물로 남김 (Assets 밖)
#   tree_* 는 생성기(make_tree_icons.py)가 같은 finish() 로 마무리하므로 여기서는 건드리지 않음
#   prop_*_glow 는 그림이 아니라 지도 소품의 흰 테 가면(MapProps.rimArt)이라 제외
# 실행: python tools/pixelart/restyle_icons.py [--from <원본 폴더>] [--only <접두어>] [--dry] [--sheet <출력 폴더>] [--report]
#   규칙을 이미 지키는 그림은 그대로 둠 (여러 번 돌려도 같음). 원본은 git 에 있음
import colorsys, math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
from png import Img, read, write

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
ICONS = os.path.join(ROOT, 'Assets', 'Resources', 'Icons')
GLYPH_OUT = os.path.join(os.path.dirname(__file__), 'out', 'glyph')

K1 = (26, 17, 44, 255)       # 외곽 남보라 #1A112C
K2 = (45, 25, 42, 255)       # 외곽 따뜻한 쪽 #2D192A
CLEAR = (0, 0, 0, 0)
N4 = ((1, 0), (-1, 0), (0, 1), (0, -1))
N8 = N4 + ((1, 1), (-1, -1), (1, -1), (-1, 1))


def Y(c):
    return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]


def opaque(img, x, y):
    return 0 <= x < img.w and 0 <= y < img.h and img.px[y][x][3] > 0


def copy(img):
    o = Img(img.w, img.h)
    o.px = [row[:] for row in img.px]
    return o


# ------------------------------------------------------------------ 잼 (lint 와 같은 정의 — 기획 2-5)
def edge_cells(img):
    # 가장자리 칸 = 불투명이고 4방향 중 빈칸(캔버스 밖 포함)이 있는 칸
    return [(x, y) for y in range(img.h) for x in range(img.w)
            if img.px[y][x][3] > 0 and any(not opaque(img, x + dx, y + dy) for dx, dy in N4)]


def metrics(img, keep_outlined=False):
    edge = edge_cells(img)
    es = set(edge)
    inner1 = {(x + dx, y + dy) for x, y in edge for dx, dy in N4
              if opaque(img, x + dx, y + dy) and (x + dx, y + dy) not in es}
    cols = {img.px[y][x][:3] for y in range(img.h) for x in range(img.w) if img.px[y][x][3] > 0}
    semi = sum(1 for y in range(img.h) for x in range(img.w) if 0 < img.px[y][x][3] < 255)
    xs = [x for y in range(img.h) for x in range(img.w) if img.px[y][x][3] > 0]
    ys = [y for y in range(img.h) for x in range(img.w) if img.px[y][x][3] > 0]
    comps = components(img)
    return dict(
        outline=sum(1 for x, y in edge if Y(img.px[y][x]) < 41) / max(1, len(edge)),
        thick=sum(1 for x, y in inner1 if Y(img.px[y][x]) < 41) / max(1, len(inner1)),
        colors=len(cols), semi=semi,
        box=(max(xs) - min(xs) + 1, max(ys) - min(ys) + 1) if xs else (0, 0),
        islands=sum(1 for c in sorted(comps, key=len)[:-1] if len(c) <= 16
                    and not (keep_outlined and len(c) > 6 and any(Y(img.px[y][x]) < 41 for x, y in c))))


def passes(m, maxc):
    return m['semi'] == 0 and m['outline'] >= 0.97 and m['thick'] <= 0.25 and m['colors'] <= maxc and m['islands'] == 0


# ------------------------------------------------------------------ 공통 손질
def components(img, cells=None):
    # 불투명 칸의 8-연결 덩어리
    todo = set(cells) if cells is not None else {(x, y) for y in range(img.h) for x in range(img.w) if img.px[y][x][3] > 0}
    out = []
    while todo:
        s = todo.pop(); comp = [s]; st = [s]
        while st:
            x, y = st.pop()
            for dx, dy in N8:
                p = (x + dx, y + dy)
                if p in todo:
                    todo.remove(p); comp.append(p); st.append(p)
        out.append(comp)
    return out


def solid_alpha(img, cut=128):
    # 반투명 번짐 금지: 반 넘게 보이는 칸은 불투명, 나머지는 지움
    for y in range(img.h):
        for x in range(img.w):
            a = img.px[y][x][3]
            if 0 < a < 255:
                img.px[y][x] = img.px[y][x][:3] + (255,) if a >= cut else CLEAR


def drop_islands(img, size=6, keep_outlined=False):
    # 본체와 떨어진 작은 섬(떠 있는 반짝이 · 불티)을 지움 — 가장 큰 덩어리는 늘 남김
    #   16칸 이하 덩어리도 떠 있는 반짝이로 봄 — keep_outlined 면 외곽(어두운 칸)을 두른 작은 부품(트리 단계 구슬)은 남김
    comps = sorted(components(img), key=len, reverse=True)
    for c in comps[1:]:
        outlined = any(Y(img.px[y][x]) < 41 for x, y in c)
        if len(c) <= size or (len(c) <= 16 and not (keep_outlined and outlined)):
            for x, y in c: img.px[y][x] = CLEAR


def near_k(c):
    d1 = sum((c[i] - K1[i]) ** 2 for i in range(3)); d2 = sum((c[i] - K2[i]) ** 2 for i in range(3))
    return K1 if d1 <= d2 else K2


def fill_outline(img):
    # 가장자리가 밝으면 바깥 빈칸에 외곽 1칸 (캔버스 끝이라 빈칸이 없으면 그 칸을 외곽색으로), 이미 어두운 가장자리는 외곽 두 색으로 맞춤
    edge = edge_cells(img)
    add = set()
    for x, y in edge:
        c = img.px[y][x]
        if Y(c) < 41:
            img.px[y][x] = near_k(c)
            continue
        room = False
        for dx, dy in N4:
            xx, yy = x + dx, y + dy
            if 0 <= xx < img.w and 0 <= yy < img.h and img.px[yy][xx][3] == 0:
                add.add((xx, yy)); room = True
        out_of_canvas = any(not (0 <= x + dx < img.w and 0 <= y + dy < img.h) for dx, dy in N4)
        if out_of_canvas or not room:
            img.px[y][x] = K1
    for x, y in add:
        img.px[y][x] = K1


def thin_outline(img, limit=0.25):
    # 외곽 두께: 가장자리 바로 안쪽도 어두운 칸이 많으면 그 안쪽 칸을 이웃 밝은 색으로 (두 겹 외곽 → 한 겹)
    for _ in range(4):
        edge = set(edge_cells(img))
        inner1 = {(x + dx, y + dy) for x, y in edge for dx, dy in N4
                  if opaque(img, x + dx, y + dy) and (x + dx, y + dy) not in edge}
        dark = [p for p in inner1 if Y(img.px[p[1]][p[0]]) < 41]
        if len(dark) <= limit * max(1, len(inner1)):
            return
        new = {}
        for x, y in dark:
            best = None
            for dx, dy in N8:
                xx, yy = x + dx, y + dy
                if opaque(img, xx, yy) and (xx, yy) not in edge and Y(img.px[yy][xx]) >= 41:
                    c = img.px[yy][xx]
                    if best is None or Y(c) < Y(best): best = c
            if best is not None: new[(x, y)] = best
        for (x, y), c in new.items(): img.px[y][x] = c


def median_cut(counts, n):
    # counts: {rgb: 칸 수} → 대표색 n 개 (상자마다 가장 많이 쓴 실제 색 = 지금 팔레트에 붙임)
    boxes = [list(counts.items())]
    while len(boxes) < n:
        best, bi, ch = -1, -1, 0
        for i, b in enumerate(boxes):
            if len(b) < 2: continue
            for k in range(3):
                r = max(c[0][k] for c in b) - min(c[0][k] for c in b)
                w = r * (1.0 if k != 1 else 1.3)
                if w > best: best, bi, ch = w, i, k
        if bi < 0: break
        b = sorted(boxes.pop(bi), key=lambda c: c[0][ch])
        tot = sum(c[1] for c in b); acc = 0; cut = 1
        for j, c in enumerate(b):
            acc += c[1]
            if acc >= tot / 2: cut = max(1, min(len(b) - 1, j + 1)); break
        boxes += [b[:cut], b[cut:]]
    return [max(b, key=lambda c: c[1])[0] for b in boxes if b]


def cdist(a, b):
    dr, dg, db = a[0] - b[0], a[1] - b[1], a[2] - b[2]
    return 0.3 * dr * dr + 0.59 * dg * dg + 0.11 * db * db


def reduce_colors(img, maxc, cells=None, keep=(K1[:3], K2[:3])):
    # 외곽 두 색은 지키고 나머지를 maxc - (쓴 외곽 색 수) 개로
    cells = cells if cells is not None else [(x, y) for y in range(img.h) for x in range(img.w) if img.px[y][x][3] > 0]
    counts = {}
    used_k = set()
    for x, y in cells:
        c = img.px[y][x][:3]
        if c in keep: used_k.add(c); continue
        counts[c] = counts.get(c, 0) + 1
    room = maxc - len(used_k)
    if len(counts) <= room: return
    pal = median_cut(counts, room)
    memo = {}
    for x, y in cells:
        c = img.px[y][x][:3]
        if c in keep: continue
        if c not in memo: memo[c] = min(pal, key=lambda p: cdist(c, p))
        img.px[y][x] = memo[c] + (255,)


def finish(img, maxc, keep_outlined=False):
    # 물건형 · 생성기 공용 마무리 (make_tree_icons.py 도 씀 — 단계 구슬을 지키려고 keep_outlined)
    solid_alpha(img)
    drop_islands(img, keep_outlined=keep_outlined)
    fill_outline(img)
    thin_outline(img)
    drop_islands(img, keep_outlined=keep_outlined)
    reduce_colors(img, maxc)
    thin_outline(img)              # 색 줄이기로 안쪽 칸이 어두운 색에 붙었을 때 (이웃 색만 쓰므로 색 수는 안 늘어남)
    return img


# ------------------------------------------------------------------ 메달형 (ability_* 유리 구슬 → 평면 메달)
CX = CY = 16.0
IN = [(52, 46, 84), (38, 32, 64)]           # 원판 2단 (갈래 메달 안쪽과 같은 남보라)
GOLD = [(255, 236, 160), (255, 200, 86), (234, 170, 58), (160, 98, 34)]
WHITE = (255, 255, 255, 255)


def hsv(c):
    return colorsys.rgb_to_hsv(c[0] / 255, c[1] / 255, c[2] / 255)


def rgb(h, s, v):
    r, g, b = colorsys.hsv_to_rgb(h % 1.0, max(0, min(1, s)), max(0, min(1, v)))
    return (round(r * 255), round(g * 255), round(b * 255))


def shades(c):
    # 갈래 아이콘 테와 같은 4단 (make_paths_*.shades)
    r, g, b = c
    return [(min(255, int(r + (255 - r) * 0.55)), min(255, int(g + (255 - g) * 0.55)), min(255, int(b + (255 - b) * 0.55))),
            (r, g, b), (int(r * 0.78), int(g * 0.78), int(b * 0.78)), (int(r * 0.5), int(g * 0.5), int(b * 0.5))]


def hue_dist(a, b):
    d = abs(a - b) % 1.0
    return min(d, 1 - d)


def mean_hue(cs):
    sx = sy = 0.0
    for c in cs:
        h, s, v = hsv(c)
        w = s * v
        sx += math.cos(h * 2 * math.pi) * w; sy += math.sin(h * 2 * math.pi) * w
    return (math.atan2(sy, sx) / (2 * math.pi)) % 1.0, math.hypot(sx, sy) / max(1, len(cs))


def orb_shape(img):
    # 불투명 칸이 원판인지: 넓이로 반지름, 원 안 채움 · 원 밖 넘침
    cells = [(x, y) for y in range(img.h) for x in range(img.w) if img.px[y][x][3] > 0]
    if not cells: return None
    cx = sum(x + 0.5 for x, _ in cells) / len(cells); cy = sum(y + 0.5 for _, y in cells) / len(cells)
    R = math.sqrt(len(cells) / math.pi)
    inside = sum(1 for x, y in cells if math.hypot(x + 0.5 - cx, y + 0.5 - cy) <= R + 0.8)
    disc = [(x, y) for y in range(img.h) for x in range(img.w) if math.hypot(x + 0.5 - cx, y + 0.5 - cy) <= R - 0.8]
    cover = sum(1 for x, y in disc if img.px[y][x][3] > 0) / max(1, len(disc))
    return cx, cy, R, inside / len(cells), cover


BG_TOL = 30


def fit_bg(img, cells, cx, cy):
    # 칸 색 = 자리 (x, y) 의 2차식 (채널마다 최소제곱) → 예측 함수
    def feat(x, y):
        u, v = (x + 0.5 - cx) / 16.0, (y + 0.5 - cy) / 16.0
        return (1.0, u, v, u * u, u * v, v * v)
    n = 6
    coef = []
    for ch in range(3):
        A = [[0.0] * n for _ in range(n)]; b = [0.0] * n
        for x, y in cells:
            f = feat(x, y); c = img.px[y][x][ch]
            for i in range(n):
                b[i] += f[i] * c
                for j in range(n): A[i][j] += f[i] * f[j]
        for i in range(n): A[i][i] += 1e-3 + (0.5 if i >= 3 else 0.0)      # 칸이 적을 때 2차 항이 튀지 않게
        for i in range(n):                                               # 가우스 소거
            pv = max(range(i, n), key=lambda r: abs(A[r][i]))
            A[i], A[pv] = A[pv], A[i]; b[i], b[pv] = b[pv], b[i]
            for r in range(n):
                if r != i and A[i][i]:
                    k = A[r][i] / A[i][i]
                    for j in range(i, n): A[r][j] -= k * A[i][j]
                    b[r] -= k * b[i]
        coef.append([b[i] / A[i][i] if A[i][i] else 0.0 for i in range(n)])
    def model(x, y):
        f = feat(x, y)
        return tuple(sum(coef[ch][i] * f[i] for i in range(n)) for ch in range(3))
    return model


# 손질 목록: 그림문자가 테 안쪽 띠를 거의 덮어 바탕 색상을 그림문자 색으로 잘못 고르는 그림 → 바탕 색상을 직접 (보라 0.75)
HUE_FIX = {n: 0.75 for n in ('ability_101', 'ability_900', 'ability_197', 'ability_921', 'ability_923', 'ability_183', 'ability_187', 'ability_193', 'ability_194', 'ability_198', 'ability_199',
                             'ability_201', 'ability_702', 'ability_814', 'ability_930', 'ability_931')}


def split_glyph(img, cx, cy, R, name=None):
    # 원판 바탕 찾기: 테 안쪽 고리(R-7 ~ R-3.5)의 흔한 색상에서 출발, 이웃끼리 색 거리가 작고 바탕 색상과 가까우면 바탕으로 번져 감
    #   나머지(원판 안) = 그림문자. 테(바깥 고리) · 보석 · 바깥 반짝이는 버림
    ring = [(x, y) for y in range(img.h) for x in range(img.w)
            if img.px[y][x][3] > 0 and R - 7.5 <= math.hypot(x + 0.5 - cx, y + 0.5 - cy) <= R - 3.5]
    # 바탕 색상 = 고리 칸 색상의 최빈 묶음 (금테 그림은 테 · 보석의 금색을 빼고)
    gold = rim_kind(img, cx, cy, R, 0)
    # 바탕 색상 = 테 바로 안쪽 띠에서 「둘레를 가장 넓게 두른」 색상 (그림문자는 한쪽에 몰려 있고 바탕은 빙 둘러 있음)
    #   테 · 보석의 금색은 바탕이 거의 금색일 때만 셈
    band = [(x, y) for y in range(img.h) for x in range(img.w)
            if img.px[y][x][3] > 0 and (R - 7.5 if gold else R - 5.0) <= math.hypot(x + 0.5 - cx, y + 0.5 - cy) <= (R - 3.5 if gold else R - 1.5)]
    def is_gold(h, s, v): return 0.07 <= h <= 0.18 and s > 0.4 and v > 0.5
    cand = [(x, y) + hsv(img.px[y][x]) for x, y in band]
    cand = [c for c in cand if c[3] >= 0.25 and c[4] >= 0.2]
    nog = [c for c in cand if not is_gold(*c[2:])]
    if len(nog) >= 0.2 * max(1, len(cand)): cand = nog
    best, hue = -1, None
    for b0 in range(24):
        sel = [c for c in cand if min((int(c[2] * 24) - b0) % 24, (b0 - int(c[2] * 24)) % 24) <= 1]
        if not sel: continue
        sectors = {int((math.atan2(y + 0.5 - cy, x + 0.5 - cx) + math.pi) / (2 * math.pi) * 12) % 12 for x, y, *_ in sel}
        score = len(sectors) * 1000 + sum(c[3] for c in sel)
        if score > best:
            best = score
            hue, _ = mean_hue([img.px[c[1]][c[0]] for c in sel if (int(c[2] * 24) - b0) % 24 in (0, 1, 23)])
    if hue is None:
        hue, _ = mean_hue([img.px[y][x] for x, y in ring])
    if name in HUE_FIX: hue = HUE_FIX[name]
    def bg_like(c):
        h, s, v = hsv(c)
        if v < 0.16: return False                       # 외곽 · 그림문자 테
        if s < 0.18: return v < 0.5                     # 무채: 어두운 쪽만 바탕 (흰 광택 · 흰 그림문자는 다음 단계에서)
        if s < 0.3 and v > 0.8: return False            # 흰 물건의 푸른 그늘 (유령 · 눈 · 종이) — 광택이면 다음 단계에서 바탕으로
        return hue_dist(h, hue) < 0.075
    inner_r = R - 2.6
    # 유리 구슬 바탕은 자리마다 매끈하게 바뀌는 그라데이션 → 자리의 2차식으로 바탕 색을 맞춰 보고,
    #   그 예측과 가까운 칸만 고리에서부터 번져 감 (색상이 같아도 밝기 · 외곽이 다른 그림문자는 남음)
    seeds = [p for p in set(ring) | set(band) if bg_like(img.px[p[1]][p[0]])
             and math.hypot(p[0] + 0.5 - cx, p[1] + 0.5 - cy) <= inner_r + 0.5]
    model = fit_bg(img, seeds, cx, cy)
    for _ in range(2):
        seeds = [p for p in seeds if cdist(img.px[p[1]][p[0]], model(*p)) < BG_TOL ** 2] or seeds
        model = fit_bg(img, seeds, cx, cy)
    # 바탕 결(잡티 · 무늬)이 거친 구슬은 문턱을 그만큼 넓힘
    res = sorted(math.sqrt(cdist(img.px[y][x], model(x, y))) for x, y in seeds)
    tol = max(BG_TOL, min(55, 2.2 * res[int(len(res) * 0.8)] if res else BG_TOL))
    bg = set()
    for _ in range(2):
        bg = set(seeds); st = list(seeds)
        while st:
            x, y = st.pop()
            for dx, dy in N4:
                xx, yy = x + dx, y + dy
                if (xx, yy) in bg or not opaque(img, xx, yy): continue
                if math.hypot(xx + 0.5 - cx, yy + 0.5 - cy) > inner_r + 0.5: continue
                c = img.px[yy][xx]
                if Y(c) >= 30 and bg_like(c) and cdist(c, model(xx, yy)) < tol ** 2:
                    bg.add((xx, yy)); st.append((xx, yy))
        model = fit_bg(img, list(bg), cx, cy)
    disc_n = sum(1 for y in range(img.h) for x in range(img.w) if math.hypot(x + 0.5 - cx, y + 0.5 - cy) <= inner_r)
    if len(bg) < 0.15 * disc_n:
        # 그림문자가 커서 바탕 칸이 적으면 2차식이 흔들림 → 이웃끼리 색 거리로만 번지는 예전 방식
        bg = set(seeds); st = list(seeds)
        while st:
            x, y = st.pop()
            c0 = img.px[y][x]
            for dx, dy in N4:
                xx, yy = x + dx, y + dy
                if (xx, yy) in bg or not opaque(img, xx, yy): continue
                if math.hypot(xx + 0.5 - cx, yy + 0.5 - cy) > inner_r + 0.5: continue
                c = img.px[yy][xx]
                if bg_like(c) and cdist(c, c0) < 34 ** 2:
                    bg.add((xx, yy)); st.append((xx, yy))
        model = fit_bg(img, list(bg) or seeds, cx, cy)
    rest = [(x, y) for y in range(img.h) for x in range(img.w)
            if opaque(img, x, y) and (x, y) not in bg and math.hypot(x + 0.5 - cx, y + 0.5 - cy) <= inner_r]
    if len(rest) > 0.8 * disc_n:
        # 그래도 거의 다 그림문자면 안쪽 장식 고리에 갇힌 바탕 → 바탕 색상인 큰 덩어리(4-연결 · 20칸 이상)도 바탕
        cand = {p for p in rest if Y(img.px[p[1]][p[0]]) >= 30 and bg_like(img.px[p[1]][p[0]])}
        while cand:
            q = cand.pop(); comp = [q]; st = [q]
            while st:
                x, y = st.pop()
                for dx, dy in N4:
                    n = (x + dx, y + dy)
                    if n in cand: cand.remove(n); comp.append(n); st.append(n)
            if len(comp) >= 20: bg.update(comp)
    # 광택 덩어리: 바탕 색상의 밝은 무채 · 옅은 칸이 바탕에 둘러싸여 있으면 바탕 (왼쪽 위 큰 반점)
    glyph = {(x, y) for y in range(img.h) for x in range(img.w)
             if opaque(img, x, y) and (x, y) not in bg and math.hypot(x + 0.5 - cx, y + 0.5 - cy) <= inner_r}
    for comp in components(img, glyph):
        cs = [img.px[y][x] for x, y in comp]
        h, s = mean_hue(cs)
        vmean = sum(hsv(c)[2] for c in cs) / len(cs)
        dark = sum(1 for c in cs if Y(c) < 50)
        same = sum(1 for c in cs if hsv(c)[1] < 0.25 or hue_dist(hsv(c)[0], hue) < 0.09) / len(cs)
        my = sum(y + 0.5 for _, y in comp) / len(comp)
        # 외곽 없는 얼룩이 바탕 색상(또는 무채)이고 구슬 위쪽에 있으면 광택 → 바탕
        if dark == 0 and len(comp) <= 150 and same >= 0.8 and my < cy + 3:
            for p in comp: glyph.discard(p); bg.add(p)
        elif len(comp) <= 3:
            for p in comp: glyph.discard(p); bg.add(p)
    # 그림문자에 붙어 남은 광택: 바탕 색상(또는 무채)이면서 예측 바탕보다 밝고, 둘레에 외곽(어두운 칸)이 거의 없는 덩어리
    lit = set()
    for x, y in glyph:
        c = img.px[y][x]; h, sv, v = hsv(c)
        if (sv < 0.3 or hue_dist(h, hue) < 0.09) and Y(c) > Y(model(x, y)) + 6: lit.add((x, y))
    for comp in components(img, lit):
        if len(comp) < 4 or len(comp) > 45 or sum(y + 0.5 for _, y in comp) / len(comp) > cy + 1: continue
        if sum(1 for x, y in comp if hsv(img.px[y][x])[1] >= 0.15 and hue_dist(hsv(img.px[y][x])[0], hue) < 0.09) < 0.5 * len(comp): continue
        cs = set(comp)
        ring_n = {(x + dx, y + dy) for x, y in comp for dx, dy in N4} - cs
        dark = sum(1 for x, y in ring_n if opaque(img, x, y) and Y(img.px[y][x]) < 50)
        if dark <= 0.15 * max(1, len(ring_n)):
            for p in comp: glyph.discard(p); bg.add(p)
    return glyph, bg, hue


def rim_kind(img, cx, cy, R, hue):
    # 바깥 고리 색: 금테(노랑 · 채도 높음)면 금, 아니면 원판 색상
    band = [img.px[y][x] for y in range(img.h) for x in range(img.w)
            if img.px[y][x][3] > 0 and R - 3.2 <= math.hypot(x + 0.5 - cx, y + 0.5 - cy) <= R - 0.8]
    gold = sum(1 for c in band if 0.08 <= hsv(c)[0] <= 0.17 and hsv(c)[1] > 0.45 and hsv(c)[2] > 0.55)
    return gold > 0.35 * max(1, len(band))


def make_medal(src, name=None):
    sh = orb_shape(src)
    if sh is None: return None, '빈 그림'
    cx, cy, R, inside, cover = sh
    if inside < 0.93 or cover < 0.93 or R < 11:
        return None, '원판 아님 (안 %.2f · 채움 %.2f · 반지름 %.1f)' % (inside, cover, R)
    glyph, bg, hue = split_glyph(src, cx, cy, R, name)
    disc_n = sum(1 for y in range(src.h) for x in range(src.w) if math.hypot(x + 0.5 - cx, y + 0.5 - cy) <= R - 2.6)
    ratio = len(glyph) / max(1, disc_n)
    bgc = [src.px[y][x] for x, y in bg]
    _, sat = mean_hue(bgc)
    vmed = sorted(hsv(c)[2] for c in bgc)[len(bgc) // 2] if bgc else 0.6
    if rim_kind(src, cx, cy, R, hue):
        rim = GOLD
    else:
        # 범주 색 = 지금 원판 색상 (채도 · 밝기만 갈래 테 범위로)
        s = max(0.42, min(0.78, sat * 1.6 + 0.25)); v = max(0.72, min(0.95, vmed + 0.3))
        rim = shades(rgb(hue, s, v))
        while Y(rim[3]) < 45 and s > 0.2:          # 테 가장 어두운 단이 외곽색(Y < 41)에 붙으면 두 겹 외곽처럼 보임 → 채도를 낮춰 밝힘
            s -= 0.05; v = min(1.0, v + 0.03)
            rim = shades(rgb(hue, s, v))
    out = Img(32, 32)
    tint = rgb(hue, 0.55, 0.55)
    inn = [tuple(round(IN[i][k] * 0.8 + tint[k] * 0.2 * (0.55 if i else 0.75)) for k in range(3)) for i in range(2)]
    for y in range(32):
        for x in range(32):
            dx, dy = x + 0.5 - CX, y + 0.5 - CY
            d = math.hypot(dx, dy)
            if d > 15.0: continue
            if d >= 12.4:
                lit = (-dx - dy) / (d * 1.414)
                c = rim[0] if lit > 0.75 else rim[1] if lit > 0.15 else rim[2] if lit > -0.6 else rim[3]
                if 13.1 < d < 13.9 and lit < 0.6:
                    c = rim[2] if c in (rim[0], rim[1]) else rim[3]
                out.px[y][x] = c + (255,)
            else:
                k = (dy + d * 0.3) / 12.4
                out.px[y][x] = (inn[0] if k < -0.1 else inn[1]) + (255,)
    # 그림문자: 원래 자리를 원판 가운데로 옮겨 붙임 (구슬 중심이 칸 가운데가 아니어도)
    ox, oy = round(CX - cx), round(CY - cy)
    gimg = Img(32, 32)
    for x, y in glyph:
        xx, yy = x + ox, y + oy
        if 0 <= xx < 32 and 0 <= yy < 32 and math.hypot(xx + 0.5 - CX, yy + 0.5 - CY) < 12.4:
            gimg.px[yy][xx] = src.px[y][x][:3] + (255,)
    gcells = [(x, y) for y in range(32) for x in range(32) if gimg.px[y][x][3] > 0]
    # 그림문자 안의 어두운 칸은 외곽색으로, 원판 쪽 빈 가장자리에는 외곽 1칸
    for x, y in gcells:
        if Y(gimg.px[y][x]) < 41: gimg.px[y][x] = near_k(gimg.px[y][x])
    reduce_colors(gimg, 12, gcells)
    if name:
        os.makedirs(GLYPH_OUT, exist_ok=True)
        write(gimg, os.path.join(GLYPH_OUT, name + '.png'))
    for x, y in gcells:
        if Y(gimg.px[y][x]) >= 41 and any(not opaque(gimg, x + dx, y + dy) for dx, dy in N4):
            for dx, dy in N4:
                xx, yy = x + dx, y + dy
                if 0 <= xx < 32 and 0 <= yy < 32 and gimg.px[yy][xx][3] == 0 and math.hypot(xx + 0.5 - CX, yy + 0.5 - CY) < 12.4:
                    out.px[yy][xx] = K1
    for x, y in gcells:
        out.px[y][x] = gimg.px[y][x]
    # 반사: 왼쪽 위 테에 흰 2칸 (갈래 메달과 같은 자리)
    for x, y in ((8, 6), (7, 7)):
        out.px[y][x] = WHITE
    # 바깥 외곽 1칸 (반지름 15 → 32칸 꽉)
    fill_outline(out)
    return out, 'ok %.2f' % ratio if 0.2 <= ratio <= 0.85 else '분리 의심 (그림문자 %.0f%%)' % (ratio * 100)


# ------------------------------------------------------------------ 목록 · 실행
def kind(rel):
    n = os.path.basename(rel)
    if rel.startswith('tree_'): return None                 # 생성기 몫
    if n.startswith('prop_') and n.endswith('_glow.png'): return None
    if n.startswith('ability_'): return 'ability'
    if rel.startswith('Paths') or n.startswith('choice_tr_'): return 'medal'
    return 'object'


def all_files():
    out = []
    for d, _, fs in os.walk(ICONS):
        for f in sorted(fs):
            if f.endswith('.png'):
                out.append(os.path.relpath(os.path.join(d, f), ICONS).replace('\\', '/'))
    return sorted(out)


def process(rel, src):
    k = kind(rel)
    if k is None: return None, '제외'
    maxc = 16 if k == 'object' else 20
    if passes(metrics(src), maxc): return None, '이미 규칙'
    if k == 'ability':
        img, note = make_medal(src, os.path.basename(rel)[:-4])
        if img is not None: return img, note
        # 원판이 아닌 그림은 물건형 손질
        return finish(copy(src), 20), note + ' → 물건형 손질'
    return finish(copy(src), maxc), 'ok'


def sheet(path, pairs, scale=4, cols=8):
    # 전 / 후 나란히 (위 = 어두운 칸 바탕, 아래 = 밝은 바탕 — 외곽 · 반투명이 잘 보이게)
    cw = max(max(a.w, b.w) for _, a, b in pairs) * scale
    ch = max(max(a.h, b.h) for _, a, b in pairs) * scale
    pw, ph = cw * 2 + 14, ch * 2 + 12
    rows = (len(pairs) + cols - 1) // cols
    S = Img(cols * pw, rows * ph, (64, 64, 74, 255))
    for k, (_, a, b) in enumerate(pairs):
        ox, oy = (k % cols) * pw + 4, (k // cols) * ph + 4
        for j, im in enumerate((a, b)):
            for band, bgc in enumerate(((20, 15, 31, 255), (128, 124, 138, 255))):
                bx, by = ox + j * (cw + 4), oy + band * (ch + 2)
                for y in range(ch):
                    for x in range(cw): S.px[by + y][bx + x] = bgc
                for y in range(im.h):
                    for x in range(im.w):
                        c = im.px[y][x]
                        if c[3] == 0: continue
                        for dy in range(scale):
                            for dx in range(scale): S.blend(bx + x * scale + dx, by + y * scale + dy, c)
    write(S, path)


def main():
    a = sys.argv
    src_dir = a[a.index('--from') + 1] if '--from' in a else ICONS
    only = a[a.index('--only') + 1] if '--only' in a else ''
    sheet_dir = a[a.index('--sheet') + 1] if '--sheet' in a else None
    dry = '--dry' in a
    done, notes, pairs = {}, [], {}
    for rel in all_files():
        if only and not os.path.basename(rel).startswith(only) and not rel.startswith(only): continue
        sp = os.path.join(src_dir, rel)
        if not os.path.exists(sp): continue
        src = read(sp)
        img, note = process(rel, src)
        notes.append((rel, note))
        if img is None: continue
        if (img.w, img.h) != (src.w, src.h): raise SystemExit('크기가 바뀜: ' + rel)
        k = kind(rel)
        done[k] = done.get(k, 0) + 1
        pairs.setdefault(k, []).append((rel, src, img))
        if not dry: write(img, os.path.join(ICONS, rel))
    for rel, note in notes:
        if not note.startswith('ok') and note not in ('제외', '이미 규칙'): print(rel, note)
    print('바꾼 그림:', done, '· 이미 규칙:', sum(1 for _, n in notes if n == '이미 규칙'), '· 제외:', sum(1 for _, n in notes if n == '제외'))
    if '--report' in a:
        for rel, src, img in sum(pairs.values(), []):
            m0, m1 = metrics(src), metrics(img)
            print('%-40s 외곽 %.2f→%.2f 두께 %.2f→%.2f 색 %d→%d 반투명 %d→%d 섬 %d→%d' % (
                rel, m0['outline'], m1['outline'], m0['thick'], m1['thick'], m0['colors'], m1['colors'],
                m0['semi'], m1['semi'], m0['islands'], m1['islands']))
    if sheet_dir:
        os.makedirs(sheet_dir, exist_ok=True)
        for k, ps in pairs.items():
            for i in range(0, len(ps), 48):
                sheet(os.path.join(sheet_dir, 'icons_%s_%02d.png' % (k, i // 48 + 1)), ps[i:i + 48])


if __name__ == '__main__':
    main()
