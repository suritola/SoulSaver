# 유령 메뉴 초상 128칸 (그래픽 가독성 개편 2단계 묶음 3 · 영웅 8 — 도적 규칙 README.md 그대로)
#   설계도 = Resources/Characters/ghost.png 첫 그림(32칸 · 몸 14×17 · 발 없이 떠 있음) — 자리 · 비율 · 색 · 광원만 따옴 (화소를 키우지 않음)
#   k = 4 : 설계도 칸 (x, y) → 128칸 (4x, 4y − 3)
#   설계도 읽기 (줄 = 설계도 y): 12~15 둥근 머리 (꼭대기 6칸 → 아래로 넓어짐 · 왼 위가 가장 밝은 흰색 b) · 16~17 큰 검은 눈 둘 (x12~13 · x18~19)
#     19 눈 아래 청록 눈물 점 (x13 · x18) · 20 작은 입 (x16~17) · 오른쪽 세로 줄 f = 연보라 그늘
#     21~26 아래로 조금 좁아지는 시트 · 27 아랫단 · 28 짧고 뭉툭한 발 셋 (x12 · x16 · x20 — 뾰족한 끝 아님)
#   광원 = 설계도를 이어받아 위 · 왼쪽 (밝은 b 가 왼 위 · 그늘 f 가 오른쪽)
#   흑백 계층: 가장 어두운 눈 대비가 시선 1순위 > 눈물 점 > 입 > 머리 빛 > 몸 > 오른쪽 아래 라벤더 그늘 > 발 사이 틈
#   판정 1회차 불합격 → ⑧ 어금니 실루엣 → 매끈한 둥근 윗면 + 짧고 뭉툭한 발 셋 · U자 턱받이 띠 지움 · 다른 영웅과 같은 외곽 · 단계 문법
#   판정 2회차 불합격 (④ 거의 흰 세 색이 몸의 45% — 플라스틱 · 벡터) → 몸 명암을 다시 설계:
#     머리 = 왼 위 빛을 받는 구 (가장 밝은 단계는 왼 위 핵 46~60 × 46~56 만) · 아래 시트 = 발 셋으로 흘러내리는 천 갈래 셋
#     (갈래마다 왼쪽이 빛 · 오른쪽이 그늘 → 같은 단계가 갈래마다 끊겨 한 색 덩어리 ≤ 300칸, 아랫단 그늘 경계가 발 쪽으로 물결)
#     명암 경계 = 손으로 정한 곡면(구 + 볼 둘 · 이마 볼록 + 갈래 원기둥 · 갈래 중심선이 발 쪽으로 휨 + 오른쪽 반사광)을 고정 문턱 CUTS 로 나눔
#     (문턱은 「모든 한 색 덩어리 ≤ 300칸 · lint 통과」로 미리 골라 숫자로 박음 — 그릴 때 자동으로 나누지 않음) · 시트 7단 (2회차 「재질 단계 늘리기」)
#     큰 면을 끊는 손 주름: 왼눈 아래 → 첫 골 · 오른 어깨 → 둘째 골 처짐 · 입 아래 짧은 주름 · 정수리 얕은 오목 (등성이 빛 2칸 폭)
#     곧은 세로 반사 띠 · 곧은 가로 아랫단 띠 지움 (남는 반사광은 갈래 곡면을 따라 휨)
#   그늘 단계 색상은 무채(채도 < 0.15)로 — 눈(보라 260°)과 같은 색상 묶음이면 「위가 어둡다」로 읽혀 lint 10 이 뒤집힘
import numpy as np
from scipy import ndimage

from .portrait import Canvas, Map, hx, path, poly_mask, ellipse_mask, line_cells, n_plane, despeckle

SIZE = 128
K = 4
M = Map(K, 0, -3)
DESIGN = 'Assets/Resources/Characters/ghost.png'     # 시트 첫 칸 (32×32) 이 설계도

# 재질: 어두움 → 밝음 (빛 쪽 따뜻한 흰 → 그늘 쪽 차가운 라벤더) · 재질당 ≤ 5단 · 모든 시트 단계 채도 < 0.15 (같은 무채 묶음 — lint 12 베개)
MATERIALS = {
    #   라벤더 그늘 · 반그늘 (설계도 f #BEBEE2 쪽) · 바탕 (설계도 c #E2E4F8 쪽) · 빛 · 핵 (설계도 b #FAFAFF 를 따뜻하게)
    #   판정 2회차 「재질 단계 늘리기」 → 7단 (5단으로는 3,000칸 몸의 단계마다 600칸 — 한 색 넓은 면). 단계 간격 Y 약 11~18 (등차 아님)
    'sheet': dict(ramp=[hx('A8A7C4'), hx('B9B8D4'), hx('C9C9E1'), hx('D9D9EC'), hx('E6E5F1'), hx('F1EFF1'), hx('FCF9EF')], cuts=[9.0] * 6),
    # 발 사이 틈 · 아랫단 안쪽 (가장 어두운 천 — 차갑게 · 눈과 같은 색상 묶음)
    'gap':   dict(ramp=[hx('6C649C'), hx('8680B4')], cuts=[9.0]),
    'eye':   dict(ramp=[hx('120B20'), hx('2C2450'), hx('4A4478')], cuts=[9.0, 9.1]),
    'tear':  dict(ramp=[hx('2A8CB4'), hx('50D6EA'), hx('B8F4F8')], cuts=[9.0, 9.1]),
    'mouth': dict(ramp=[hx('3A3458'), hx('59547A'), hx('8A86B0')], cuts=[9.0, 9.1]),
}
GLINT = hx('FFFFFF')
LIGHT = np.array([-0.12, -0.66, 0.74])           # 위 · 왼쪽 (lint 10 · 11 범위 안에서)
LIGHT = LIGHT / np.linalg.norm(LIGHT)
FEET = (49.5, 65.5, 81.5)                          # 발 셋 가운데 x (설계도 x12 · x16 · x20)
# 몸 단계 문턱 (밝기 → 0 라벤더 … 6 핵) — 손으로 정함 (간격이 같지 않게)
BUMPS = ((50.5, 77.0, 7.5, 0.55), (77.5, 77.5, 7.0, 0.55), (63.0, 53.0, 11.0, 0.30))   # (x, y, 반지름, 세기) 볼 둘 · 이마
REFL = 0.45                                        # 오른쪽 반사광 세기 (밝기 곡면에 더함)
CUTS = [0.374, 0.461, 0.609, 0.709, 0.855, 0.99]


def smooth01(a, b, v):
    t = np.clip((v - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def field(xx, yy):
    """몸 밝기 곡면 — 머리 구 (위) 와 갈래 셋 원기둥 (아래) 을 y 76~92 에서 이어 붙임"""
    X, Y = xx + 0.5, yy + 0.5
    # 머리 구: 가운데 (64, 70) · 반지름 29 × 28
    u = np.clip((X - 64.0) / 29.0, -0.97, 0.97)
    v = np.clip((Y - 70.0) / 28.0, -0.97, 0.97)
    v = np.where(v > 0, 0.35 * v, v)                    # 머리 아래 반은 시트로 곧게 늘어짐 (구의 아랫면 그늘 띠가 얼굴 밑을 가로지르지 않게)
    hn = np.stack([u, v, np.sqrt(np.clip(1 - u * u - v * v, 0.03, 1))])
    # 갈래 셋: 중심선이 발 쪽으로 휨 (위 → 아래로 x 가 조금씩 옮겨감) · 폭이 아래로 좁아짐 — 갈래마다 폭 · 휨 다르게
    lob = np.full(X.shape, 9.0)
    lu = np.zeros_like(X)
    for (fx, w0, w1, drift) in ((FEET[0], 9.5, 6.0, -2.0), (FEET[1], 8.5, 5.5, 0.5), (FEET[2], 8.0, 6.5, 1.5)):
        t = smooth01(84, 110, Y)
        cx = fx - drift * (1 - t)                      # 위쪽은 가운데 쪽으로 · 아래는 발 위
        w = w0 + (w1 - w0) * t
        d = (X - cx) / w
        pick = np.abs(d) < np.abs(lob)
        lob = np.where(pick, d, lob)
    lu = np.clip(lob, -0.95, 0.95)
    # 갈래 원기둥 + 몸 전체의 얕은 원기둥 (오른쪽이 돌아가 그늘) + 아래로 갈수록 아래를 봄
    bu = np.clip((X - 62.0) / 34.0, -0.95, 0.95)
    gx = 0.5 * lu + 0.45 * bu
    gy = 0.04 + 0.12 * smooth01(76, 96, Y) + 0.12 * smooth01(100, 112, Y)
    ln = np.stack([gx, gy, np.sqrt(np.clip(1 - gx * gx - gy * gy, 0.05, 1))])
    t = smooth01(80, 100, Y)
    n = hn * (1 - t) + ln * t
    #   얼굴 형태: 눈 아래 볼록한 두 볼 (눈물 점 자리 · 둘 크기 다르게) · 이마의 얕은 볼록 — 정면을 보는 넓은 얼굴 면을 볼마다 나눔
    for bx, by_, r, amp in BUMPS:
        dx, dy = (X - bx) / r, (Y - by_) / r
        g = np.exp(-(dx * dx + dy * dy))
        n = n + np.stack([amp * dx * g, amp * dy * g, np.zeros_like(g)])
    n = n / np.linalg.norm(n, axis=0)
    d = np.clip(n[0] * LIGHT[0] + n[1] * LIGHT[1] + n[2] * LIGHT[2], 0, 1)
    b = 0.12 + 0.98 * d
    # 왼 위 핵 (구의 반사 — 46~60 × 46~56 둘레만 한 단계 더)
    core = np.exp(-(((X - 50.5) / 6.5) ** 2 + ((Y - 51.0) / 5.0) ** 2))
    return b + 0.24 * core


def draw():
    c = Canvas(SIZE, MATERIALS)
    S = SIZE

    # ── 1 몸 실루엣 (머리 + 시트 + 발 셋 · 설계도 x9~22 · y12~28 → 상자 36~91 × 45~113)
    dome = ellipse_mask(64.2, 71.0, 27.9, 26.6, S) & (c.yy < 72)
    low_pts = path((36.2, 70), (92.2, 70),
                   [(92.2, 78), (91.0, 85), (89.4, 89)], [(88.6, 94), (88.6, 100), (88.4, 104)],
                   [(88.0, 106.5), (85.5, 107.6), (81, 107.6)], (47, 107.6),
                   [(44.6, 107.4), (43.6, 105.5), (43.6, 102)], [(43.4, 97), (41.5, 91.5), (40, 87)],
                   [(37.6, 81), (36.4, 76), (36.2, 70)])
    body = dome | poly_mask(low_pts, S)
    #   발: 폭 · 길이를 조금씩 다르게 (같은 도장 금지) — 몸과 같은 천 (경계선 없음)
    for fx, w, bot in zip(FEET, (9.4, 8.8, 9.0), (112.2, 112.8, 111.8)):
        body |= ellipse_mask(fx, bot - 5.6, w / 2, 6.0, S) & (c.yy >= 100)

    b = ndimage.gaussian_filter(field(c.xx, c.yy), 1.0)
    #   오른쪽 반사광 (둥근 몸이 바닥 · 카펫 빛을 되받음): 그늘 쪽 실루엣에서 안으로 2~3칸 · 바깥 방향이 오른쪽인 곳만 — 곡면을 따라 휜 띠
    dist = ndimage.distance_transform_edt(np.pad(body, 1))[1:-1, 1:-1]
    sm = ndimage.gaussian_filter(body.astype(float), 2.0)
    gy_, gx_ = np.gradient(sm)
    nxo = -gx_ / (np.sqrt(gx_ * gx_ + gy_ * gy_) + 1e-9)
    band = smooth01(58, 68, c.yy) * (1 - smooth01(102, 109, c.yy))
    b = b + REFL * np.exp(-np.maximum(dist - 1, 0) / 2.4) * np.clip(nxo, 0, 1) ** 1.5 * band
    c.cuts[c.mi('sheet')] = CUTS
    p_body = c.part('body', body, 'sheet', n_plane(0, 0, 1), smooth=0)
    c.lvl[body] = np.digitize(b, CUTS)[body]

    # ── 2 눈 (설계도 x12~13 · x18~19 · y16~17 → 세로로 조금 긴 타원)
    p_eyel = c.part('eye_l', ellipse_mask(52.0, 64.5, 4.5, 5.3, S), 'eye', n_plane(0, 0, 1), line=False, smooth=0)
    p_eyer = c.part('eye_r', ellipse_mask(76.0, 64.5, 4.5, 5.3, S), 'eye', n_plane(0, 0, 1), line=False, smooth=0)
    c.tidy(4)

    deep = ndimage.distance_transform_cdt(np.pad(c.mat >= 0, 1), metric='taxicab')[1:-1, 1:-1] >= 4

    def cells_of(m):
        return [(int(x), int(y)) for y, x in zip(*np.nonzero(m))]

    # ── 3 발 사이 틈 (갈래 사이 골이 아랫단에서 벌어짐 — 위가 좁은 쐐기 · 둘의 높이 · 폭 다르게)
    for gx, top, spread in ((57.5, 100.5, 0.55), (73.5, 102.0, 0.45)):
        gm = (c.pid == p_body) & (np.abs(c.xx + 0.5 - gx) <= 0.6 + spread * np.clip(c.yy - top, 0, 9)) & (c.yy >= top)
        c.set_lvl(cells_of(gm), 0, p_body, mat='gap')
        rim_g = ndimage.binary_dilation(gm, iterations=2) & ~gm & (c.pid == p_body) & (c.yy >= top - 6) & (c.mat == c.mi('sheet'))
        c.nudge(cells_of(rim_g & (c.xx + 0.5 > gx)), -1, p_body)        # 골 오른쪽 = 다음 갈래의 그늘 진 왼 옆 (빛 쪽이 아님 → 한 단계)
    # 발 아래 끝: 바닥을 보는 면 한 단계 (아래 가장자리 2칸 · 발 셋 아랫면이 둥글게)
    under = (c.pid == p_body) & (c.yy >= 108) & (c.mat == c.mi('sheet'))
    c.nudge(cells_of(under & ~ndimage.binary_erosion(c.mat >= 0, iterations=2)), -1, p_body)

    # ── 4 손으로 그은 시트 주름 (얼굴 아래 → 갈래 골로 흘러내림 · 길이 · 휨 다르게 — 대칭 · 같은 간격 금지)
    near = ndimage.distance_transform_cdt(np.pad(c.mat >= 0, 1), metric='taxicab')[1:-1, 1:-1] >= 2

    def fold(pts, side=1, dark=-1, ridge=1, w=2, edge_ok=False):
        ok = near if edge_ok else deep                 # edge_ok = 실루엣 2칸 안까지 (가장자리 띠를 끊는 처짐)
        cl = [(x, y) for x, y in line_cells(path(*pts)) if 0 <= y < S and 0 <= x < S and ok[y, x]]
        c.nudge(cl + ([(x + side, y) for x, y in cl] if w >= 2 else []), dark, p_body)
        if ridge:
            c.nudge([(x - side, y) for x, y in cl], ridge, p_body)
    def ridge2(pts, side=1):
        """주름 등성이 빛 2칸 폭 (빛 쪽 = 골의 왼쪽) — 1칸 굵기 긁힌 줄 금지"""
        cl = [(x, y) for x, y in line_cells(path(*pts)) if deep[y, x]]
        c.nudge([(x - side, y) for x, y in cl] + [(x - 2 * side, y) for x, y in cl], 1, p_body)
    # 왼눈 아래 → 첫 골: 가장 긴 주름 (골 2칸 + 왼쪽 등성이 2칸)
    f1 = [[(46.5, 72), (48.5, 80), (52, 87), (54.5, 91)], [(54.5, 91), (56.5, 96), (57.5, 101)]]
    fold(f1, ridge=0)
    ridge2(f1)
    # 오른 어깨에서 둘째 골로 비스듬히 처지는 천 (그늘 쪽 면을 가로지름 — 골 2칸 · 위 왼쪽 등성이 2칸)
    drape = [[(89, 73), (84, 80), (77.5, 87), (74.5, 93)], [(74.5, 93), (73.5, 97), (73.5, 102)]]
    fold(drape, side=1, ridge=0, w=2, edge_ok=True)
    ridge2(drape)
    # 정수리에서 오른눈 위로 내려오는 얕은 오목 (천이 머리에 당겨진 자리 · 골 2칸 — 핵 둘레 빛 고리를 둘로)
    fold([[(70, 46), (72, 50), (73.5, 54), (74, 57.5)]], ridge=0, w=2, edge_ok=True)
    # 입 아래 → 가운데 발로 짧게 처지는 주름 (얼굴 밑 빛 면을 둘로 · 골 1칸 + 왼쪽 등성이 2칸)
    f3 = [[(66, 82), (65.5, 86), (63.5, 90), (62, 93)]]
    fold(f3, ridge=0, w=1)
    ridge2(f3)
    # 눈: 아래쪽 반사 (초승달 2칸 폭)
    for ex, p in ((52.0, p_eyel), (76.0, p_eyer)):
        low = (c.pid == p) & (c.yy >= 67) & (c.yy <= 68) & (np.abs(c.xx + 0.5 - ex - 0.5) <= 2.6)
        c.set_lvl(cells_of(low), 1, p)
    # 눈 위 눈두덩 그늘 (위쪽 호 — 오른눈이 그늘 쪽이라 한 칸 더 넓게)
    for ex, grow in ((52.0, 0.0), (76.0, 0.8)):
        brow = ellipse_mask(ex + 0.4, 64.0, 6.4 + grow, 7.2 + grow, S) & ~ellipse_mask(ex, 64.5, 4.5, 5.3, S) & (c.pid == p_body) & (c.yy < 63)
        c.nudge(cells_of(brow), -1, p_body)
    # 눈물 점 (설계도 g x13 · x18 y19) — 둘 모양을 조금 다르게
    c.stamp(['.c..', 'cbb.', 'bbbb', 'bbaa', '.aa.'], 51, 72, {'a': ('tear', 0), 'b': ('tear', 1), 'c': ('tear', 2)}, part=p_body)
    c.stamp(['..c.', '.cbb', 'bbbb', 'baa.'], 74, 72, {'a': ('tear', 0), 'b': ('tear', 1), 'c': ('tear', 2)}, part=p_body)
    # 입 (설계도 h x16~17 · y20 — 작은 오므린 입)
    c.stamp(['.bbbb.', 'bbaaab', '.bbbb.'], 62, 77, {'a': ('mouth', 0), 'b': ('mouth', 1)}, part=p_body)

    # ── 5 림 라이트: 위 · 왼쪽 실루엣 안쪽 1칸 — 한 단계 올리되 5단째까지만 (가장 밝은 핵 단계는 왼 위 핵에만)
    rim = c.rim((p_body,), (-0.35, -1.0), width=1, by=0, thresh=0.45)
    for x, y in rim:
        if c.mat[y, x] == c.mi('sheet') and c.lvl[y, x] < 5:
            c.lvl[y, x] += 1

    # 1~2칸 조각 흡수 (4-연결)
    c.absorb(c.pid == p_body, min_size=3)

    # 눈 반짝 (왼 위 2×2 · 의도한 점)
    for ex in (49, 73):
        for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
            c.color[(61 + dy, ex + dx)] = GLINT
    dots = []
    c.outline()
    # 외곽 한 색 몫 75~95%: 발 아랫면(빛 반대 · 아래) 외곽만 따뜻한 외곽 #2D192A
    c.outline_warm = c.edge & (c.yy >= 109)
    return c, dots


def build():
    c, dots = draw()
    return despeckle(c.render(), protect=dots), dots
