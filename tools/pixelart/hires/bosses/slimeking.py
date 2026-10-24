"""킹 슬라임 (1장 보스 · 체험판) — 32칸 설계도 → 162칸 다시 그리기 (그래픽 개편 2단계 묶음 2 · 시안 1)

배율 27 (프리팹 그대로) · PPU 162 = 6 × 27 → 그림 1칸 = 월드 1/6칸 = 1080p 4.09 화소 (영웅과 같은 도트 크기).
쓰는 그림 8장: 살아 있는 동안 정지 그림 _0 + 죽음 클립 _22~28 (8fps). 시트는 162칸 4×2 (648×324).
  0 살아 있음  · 1 영혼 빠짐(옛 흰 번쩍 — 검사 13 몸 밝기 때문에 회보라 4단)  · 2 찌그러짐  · 3 옆으로 쓰러짐(다시 비춤)
  4 영혼 덩이  · 5 갈라짐  · 6 흩어짐  · 7 사라짐
그리는 법 (2-2): 설계도에서 부위 상자 · 기준점(눈 · 입 · 보석)을 칸 좌표로 → 젤리는 타원 돔 높이장, 왕관은 원기둥 띠 + 평면 가시 + 구슬,
영혼은 덩어리마다 타원체 → 광원(위 · 15° 왼쪽) 명암 단계 → 손 도트 지도(handmaps/slimeking.txt) 덧그림 → 외곽 1칸 → 1칸 섬 정리.
"""
import numpy as np
from scipy import ndimage

from .. import blueprint as bp
from .. import cleanup, detail, outline, shade

NAME = "slimeking"
CELL = 162
K = CELL / 32.0                 # 5.0625
SCALE = 27.0                    # SlimeKing.prefab localScale.x
PPU = CELL                      # 6 × 배율
EXTRUDE = int(round(K))         # 5
SPRITES = ["meadow_slimeking_0"] + [f"meadow_slimeking_{i}" for i in range(22, 29)]
ASSET = "Assets/Sprites/enemy/meadow/meadow_slimeking.png"
# 옛 시트(256×416 · 32칸)에서 쓰는 8칸의 자리 (열, 행 — 위에서부터)
DESIGN_CELLS = [(0, 0)] + [(c, 5) for c in range(7)]

# ---------------------------------------------------------------- 재질 · 팔레트
MAT = {"none": 0, "gel": 1, "refl": 2, "gold": 3, "gem": 4, "eye": 5, "mouth": 6, "soul": 7, "spark": 8}


def H(s):
    return bp.hexrgb(s)


# 재질마다 단계 색상은 한 30° 칸 안에서만 돎 (어두운 쪽 청록 쪽 · 밝은 쪽 노랑 쪽 — 2-1 규칙, 검사기 「색상 묶음」 정의와 맞춤)
#   젤리 147° → 121° · 금 31° → 53° · 반짝 흰빛은 모두 #FFFFFF 한 색 (무채 묶음)
#   몸 밝기 = 설계도(옛 그림) ± 15 (검사 13 — 옛 킹 슬라임 164) → 옛 그림처럼 밝은 초록
LIVE = {
    "gel": [H("#2E9056"), H("#3CAA62"), H("#52CE6B"), H("#7EE688"), H("#BAF9BC"), H("#FFFFFF")],
    "refl": [H("#3C9F8C"), H("#5AC4A4")],
    "gold": [H("#6E4618"), H("#B47624"), H("#EAAA3A"), H("#FEDE6C"), H("#FFFAD2")],
    "gem": [H("#6A1434"), H("#A8204A"), H("#E0466A"), H("#FFFFFF")],
    "eye": [H("#120B20"), H("#120B20"), H("#FFFFFF")],
    "mouth": [H("#120B20"), H("#C8466A")],
}
# 영혼 빠짐 = 옛 흰 번쩍 (판정 1회차: 보랏빛 회색 → 밝은 흰 번쩍 톤 · 설계도 몸 밝기 221)
GHOST = {
    "gel": [H("#C6C5F2"), H("#CBCAF4"), H("#CECEF6"), H("#D4D4FB"), H("#D8D8FF"), H("#FFFFFF")],      # 채도 ≥ 0.15 (한 색상 묶음) · 몸 밝기 설계도 221 ± 15
    "refl": [H("#C2C8EE"), H("#D0D6F6")],
    "gold": [H("#B0BEEA"), H("#BCC9F0"), H("#C6D2F4"), H("#CCD8F9"), H("#D2DCFC")],       # 왕관은 푸른빛 (몸과 다른 색상 묶음)
    "gem": [H("#8482B8"), H("#9C9ACC"), H("#BAB9E0"), H("#FFFFFF")],
    "eye": [H("#B494C4"), H("#B494C4"), H("#FFFFFF")],          # 눈 · 입은 다른 색상 묶음 (몸 광원 상관을 흐리지 않게)
    "mouth": [H("#B494C4"), H("#B49ACC")],
}
# 영혼 (덩이 · 조각 · 흩어짐) — 젤리 초록과 같은 색상 칸 · 7단 (프레임마다 bright 만큼 밝은 쪽으로 밀어 씀), 반짝이는 하늘빛 (180° 칸)
SOUL = {
    "soul": [H("#1E7445"), H("#2C9653"), H("#40BE5D"), H("#6EDE79"), H("#A6EEA8"), H("#D2FAD4"), H("#FFFFFF")],
    "spark": [H("#2E8A9A"), H("#4CB4C4"), H("#9CE4EE"), H("#FFFFFF")],
}
SOUL["gel"] = SOUL["soul"][:5] + [H("#FFFFFF")]     # 손 도트 지도의 거품 · 윤을 영혼 색으로
OL_COOL = (0x14, 0x26, 0x2A)    # 그림자 쪽 외곽 보조색 (Y < 41)


# ---------------------------------------------------------------- 설계도 → 부위 상자 (칸 좌표 · 그 칸의 절대 자리)
# 직접 읽은 값 (bp.bbox 로 아래 build() 가 상자를 다시 확인함)
UPRIGHT = {
    # 살아 있음 · 영혼 빠짐 · 쓰러짐(이것을 돌림)
    "stand": dict(box=(9, 13, 24, 27), body=(9, 16.4, 24, 27), crown=(12, 13, 21, 17.4),
                  eyes=[(14.5, 22.0), (18.5, 22.0)], eye="eye", mouth=(16.0, 24.6),
                  gems=[(15.5, 15.9), (17.5, 15.9)],
                  tonemap=("body_stand", 87)),          # 몸 명암 · 거품 · 볼 그림자 · 바닥 반사 = 손 도트 지도 (대안 F1)
    # 찌그러짐 (위 몸을 아래 기준으로 누름)
    "squash": dict(box=(8, 18, 25, 27), body=(8, 20.2, 25, 27), crown=(11, 18, 22, 21.3),
                   eyes=[(14.5, 23.4), (18.5, 23.4)], eye="squint", mouth=(16.0, 25.4),
                   gems=[(15.5, 20.2), (17.5, 20.2)],
                   tonemap=("body_squash", 102),          # 몸 명암 · 거품 · 윤 줄 · 바닥 반사 = 손 도트 지도 (대안 F1)
                   splash=[("drop", 8.25, 18.0, False), ("drop_d", 10.1, 19.2, False), ("drop_b", 23.85, 18.3, True), ("drop_s", 22.3, 19.0, True)]),
}


def px(v):
    return v * K


# ---------------------------------------------------------------- 그리기 판 (재질 · 단계 · 법선 · 명암 여부)
class Layer:
    def __init__(self, h, w):
        self.mat = np.zeros((h, w), int)
        self.tone = np.zeros((h, w), int)
        self.toff = np.zeros((h, w), int)          # 명암 단계 더하기 (왕관 그림자 등)
        self.n = np.zeros((h, w, 3))
        self.shaded = np.zeros((h, w), bool)       # 광원으로 단계를 정할 칸 (덧그림 칸은 False)
        self.zone = np.zeros((h, w), int)          # 0 젤리 몸 · 1 왕관 · 3 튀는 방울 (외곽색 고르기)
        self.hand = (0.40, HAND_SHARE)              # 손 명암: 빛 중심 가로 비율 · 띠 면적 몫

    def rot_cw(self):
        L = Layer(1, 1)
        L.mat = np.rot90(self.mat, -1).copy()
        L.tone = np.rot90(self.tone, -1).copy()
        L.toff = np.rot90(self.toff, -1).copy()
        L.n = shade.rotate_normals_cw(np.rot90(self.n, -1, axes=(0, 1)).copy())
        L.shaded = np.rot90(self.shaded, -1).copy()
        L.zone = np.rot90(self.zone, -1).copy()
        L.hand = self.hand
        return L


def stamp(L, rows, legend, x, y, rot=0, flip=False, clip=None):
    lg = {ch: (MAT[m], t) for ch, (m, t) in legend.items()}
    hit = detail.stamp(L.mat, L.tone, rows, lg, int(round(x)), int(round(y)), rot, flip, clip)
    L.shaded[hit] = False
    return hit


# ---------------------------------------------------------------- 젤리 몸 (돔 · 아래 둥근 모서리 · 좌우 조금 다른 어깨)
def body_shape(shape, bl, bt, br, bb):
    Hh, W = shape
    ys, xs = np.mgrid[0:Hh, 0:W]
    py, pxx = ys + 0.5, xs + 0.5
    cx, half, hgt = (bl + br) / 2, (br - bl) / 2, bb - bt
    yc = bt + 0.58 * hgt
    rc = 0.17 * hgt
    t = np.clip((yc - py) / max(yc - bt, 1e-6), 0, 1)
    left = pxx < cx
    p = np.where(left, 2.1, 2.6)                         # 왼 어깨는 둥글게 · 오른쪽은 조금 각지게 (기계적 대칭 피함)
    hw_top = half * np.clip(1 - t ** p, 0, 1) ** (1 / p)
    dyb = np.clip(py - (bb - rc), 0, rc)
    hw_bot = half - rc + np.sqrt(np.clip(rc * rc - dyb * dyb, 0, None))
    hw = np.where(py < yc, hw_top, np.where(py > bb - rc, hw_bot, half))
    inside = (np.abs(pxx - cx) <= hw) & (py >= bt) & (py <= bb)
    u = np.clip((pxx - cx) / np.maximum(hw, 1e-6), -1, 1)
    q = (py - bt) / hgt
    prof = np.where(q < 0.6, np.sqrt(np.clip(1 - ((q - 0.6) / 0.6) ** 2, 0, 1)), np.sqrt(np.clip(1 - ((q - 0.6) / 0.52) ** 2, 0, 1)))
    hgtf = np.sqrt(np.clip(1 - u * u, 0, 1)) ** 0.85 * prof
    # 젤리 표면의 느린 물결 (출렁임 — 명암 띠 경계가 곧은 사선이 되지 않게 · 결정적)
    hgtf = hgtf * (1 + 0.07 * np.sin((pxx - bl) / (br - bl) * 2 * np.pi * 1.4 + q * 2 * np.pi * 0.8 + 0.6))
    hgtf[~inside] = 0
    return inside, shade.from_height(hgtf, inside, depth=0.55 * half)


# ---------------------------------------------------------------- 왕관 (원기둥 띠 + 평면 가시 5 + 구슬 3)
def crown_shape(shape, x0, y0, x1, y1):
    Hh, W = shape
    ys, xs = np.mgrid[0:Hh, 0:W]
    py, pxx = ys + 0.5, xs + 0.5
    cw, chh = x1 - x0, y1 - y0
    ccx = (x0 + x1) / 2
    u = (pxx - ccx) / (cw / 2)
    band_top = y0 + 0.50 * chh
    yt = band_top + 1.3 * (1 - u * u)                    # 띠 위끝: 가운데가 조금 내려옴 (앞에서 본 고리)
    yb = y1 - 1.6 * u * u                                # 띠 아래끝: 양끝이 조금 올라감
    band = (np.abs(u) <= 0.98) & (py >= yt) & (py <= yb)
    n = np.zeros((Hh, W, 3))
    nz = np.sqrt(np.clip(1 - (u * 0.92) ** 2, 0.05, 1))
    n[..., 0], n[..., 1], n[..., 2] = u * 0.92, 0.12, nz
    lip = band & (py < yt + 2.0)
    n[lip, 1] = 0.75                                      # 위 테: 위를 봄 (밝음)
    rim = band & (py > yb - 1.6)
    n[rim, 1] = -0.55                                     # 아래 테: 아래를 봄 (어두움)
    mask = band.copy()
    spikes = [(-0.80, 0.45, 0.15, False), (-0.40, 0.17, 0.17, True), (0.0, 0.0, 0.19, True), (0.40, 0.17, 0.17, True), (0.80, 0.45, 0.15, False)]
    ball_r = max(3.0, 0.15 * chh)                     # 구슬 지름 ≥ 6칸 (외곽 빼고 속이 보이게)
    for uc, hfrac, wfrac, ball in spikes:
        sx = ccx + uc * cw / 2
        top = y0 + hfrac * (band_top - y0)
        tip = top + (ball_r if ball else 0)              # 구슬은 가시 끝에 바로 붙음 (목 없음)
        w = wfrac * cw / 2
        tri = detail.poly_mask(shape, [(sx - w, band_top + 2.2), (sx + w, band_top + 2.2), (sx, tip)])
        tri &= ~band | (py < yt + 0.5)
        n[tri & (pxx < sx)] = (-0.45, 0.38, 0.81)
        n[tri & (pxx >= sx)] = (0.50, 0.30, 0.81)
        mask |= tri
        if ball:
            bcx, bcy = sx, top + ball_r
            d = detail.disc_mask(shape, bcx, bcy, ball_r)
            e = shade.ellipsoid(shape, bcx, bcy, ball_r, ball_r)
            nb = shade.from_height(e, d, depth=ball_r)
            n[d] = nb[d]
            mask |= d
    n = shade.normalize(n)
    return mask, n, band, (yt, yb)


def build_upright(g, ghost=False, with_shine=True):
    """세운 슬라임 한 장 (칸 상자 g['box'] 크기의 판) → Layer"""
    bx0, by0, bx1, by1 = g["box"]
    W, Hh = int(round(px(bx1))) - int(round(px(bx0))), int(round(px(by1))) - int(round(px(by0)))
    ox, oy = int(round(px(bx0))), int(round(px(by0)))
    loc = lambda x, y: (px(x) - ox, px(y) - oy)
    L = Layer(Hh, W)
    L.hand = (g.get("hx", 0.39), g.get("share", HAND_SHARE), 0.11, 1.02)
    leg, grids = detail.load_handmaps("slimeking")

    bl, bt, br, bb = g["body"]
    (blx, bty), (brx, bby) = loc(bl, bt), loc(br, bb)
    body, nb = body_shape((Hh, W), blx, bty, brx, bby)
    L.mat[body] = MAT["gel"]
    L.n[body] = nb[body]
    L.shaded[body] = True
    if "tonemap" in g:                                  # 손 도트 지도로 몸 단계를 직접 (명암 계산 안 함)
        apply_tonemap(L, grids[g["tonemap"][0]], g["tonemap"][1] - oy, body)

    # 젤리 속 거품 (판정 1회차: 고르게 뿌린 기포 = 절차적 인상 → 아래 · 옆 가장자리에만, 큰 것 1 + 작은 것 몇 개)
    bw, bh = brx - blx, bby - bty
    inner = ndimage.binary_erosion(body, iterations=3)
    for name, fx, fy in g.get("bubbles", ()):
        rows = grids[name]
        stamp(L, rows, leg, blx + fx * bw - len(rows[0]) / 2, bty + fy * bh - len(rows) / 2, clip=inner)
    # 볼 그림자: 눈 아래 · 바깥 볼에 한 단계 어두운 덩어리 (이마 · 얼굴 사이 큰 한 색 면을 나눔)
    for i, (ex, ey) in enumerate(g["eyes"]):
        cx, cy = loc(ex, ey)
        for sh_name, ddx, ddy in g.get("cheeks", ()):
            rows = grids[sh_name]
            gy0, gx0 = int(round(cy + ddy)), int(round(cx + (ddx if i else -ddx) - len(rows[0]) / 2))
            ga = detail.grid_array(rows, flip=(i == 0))
            for (yy, xx), ch in np.ndenumerate(ga):
                Y_, X_ = gy0 + yy, gx0 + xx
                if ch == "z" and 0 <= Y_ < Hh and 0 <= X_ < W and body[Y_, X_]:
                    L.toff[Y_, X_] -= 1

    # 얼굴
    for i, (ex, ey) in enumerate(g["eyes"]):
        cx, cy = loc(ex, ey)
        if g["eye"] == "eye":
            rows = grids["eye"]
            stamp(L, rows, leg, cx - 3, cy - 5, clip=body)
        else:
            rows = grids["eye_squint"]
            stamp(L, rows, leg, cx - 3.5, cy - 2.5, flip=(i == 1), clip=body)
    mx, my = loc(*g["mouth"])
    stamp(L, grids["mouth"], leg, mx - 4.5, my - 2, clip=body)

    # 왕관
    cx0, cy0 = loc(g["crown"][0], g["crown"][1])
    cx1, cy1 = loc(g["crown"][2], g["crown"][3])
    crown, nc, band, (yt, yb) = crown_shape((Hh, W), cx0, cy0, cx1, cy1)
    # 왕관 그림자: 띠 바로 아래 젤리 3칸 한 단계 어둡게
    below = ndimage.binary_dilation(crown, structure=np.array([[0, 0, 0], [0, 1, 0], [0, 1, 0]], bool), iterations=3) & ~crown & body
    L.toff[below] -= 1
    L.mat[crown] = MAT["gold"]
    L.n[crown] = nc[crown]
    L.shaded[crown] = True
    L.zone[crown] = 1
    # 띠 장식: 보석 2 (금 테 두름) · 징 3
    band_mid = lambda x: (yt[0, int(min(W - 1, max(0, x)))] + yb[0, int(min(W - 1, max(0, x)))]) / 2
    for gx, gy in g["gems"]:
        gcx, _ = loc(gx, gy)
        rows = grids["gem"]
        gy0 = band_mid(gcx) - len(rows) / 2 + 0.5
        bez = np.zeros((Hh, W), bool)
        bez[int(round(gy0)) - 1:int(round(gy0)) + len(rows) + 1, int(round(gcx - 3)) - 1:int(round(gcx - 3)) + 7] = True
        bez &= crown
        L.mat[bez], L.tone[bez], L.shaded[bez] = MAT["gold"], 1, False
        stamp(L, rows, leg, gcx - 3, gy0, clip=crown)
    # 아래 테 구슬 줄: 띠 아래 2줄을 2칸 구슬로 (구슬마다 왼쪽 밝음 · 오른쪽 그림자)
    ys_, xs_ = np.mgrid[0:Hh, 0:W]
    beads = band & (ys_ + 0.5 > yb - 3.2) & (ys_ + 0.5 <= yb - 1.0)      # 맨 아래 줄은 젤리와의 이음선
    phase = (xs_ - int(cx0)) % 3
    L.tone[beads] = np.where(phase == 0, 3, np.where(phase == 1, 2, 0))[beads]
    L.shaded[beads] = False
    ccx = (cx0 + cx1) / 2
    for su in (-0.80, -0.50, 0.0, 0.50, 0.80):
        sx = ccx + su * (cx1 - cx0) / 2
        stamp(L, grids["stud"], leg, sx - 2, band_mid(sx) - 1.0, clip=band)

    # 찌그러질 때 튀는 방울 (몸 위 빈 모서리 — 상자 안)
    for name, sx, sy, flip in g.get("splash", ()):
        x, y = loc(sx, sy)
        hit = stamp(L, grids[name], leg, x, y, flip=flip, clip=(L.mat == 0))
        L.mat[hit & (L.mat == 0)] = MAT["gel"]
        L.zone[hit] = 3
    if with_shine:
        add_shine(L, grids, leg, body, blx, bty, bw, bh)
    if ghost:
        L.ghost = True
    return L, (ox, oy), body


def apply_tonemap(L, rows, y0, body):
    """몸 손 지도: 0~4 = 젤리 단계 · r/R = 바닥 반사 어두움/밝음 · . = 그대로"""
    for r, row in enumerate(rows):
        y = y0 + r
        if not 0 <= y < body.shape[0]:
            continue
        for x, ch in enumerate(row[:body.shape[1]]):
            if ch == "." or not body[y, x]:
                continue
            if ch in "rR":
                L.mat[y, x], L.tone[y, x] = MAT["refl"], 1 if ch == "R" else 0
            else:
                L.tone[y, x] = int(ch)
            L.shaded[y, x] = False


def add_shine(L, grids, leg, body, blx, bty, bw, bh):
    free = body & (L.mat == MAT["gel"])
    stamp(L, grids["shine"], leg, blx + 0.15 * bw, bty + 0.14 * bh, clip=free)
    stamp(L, grids["shine2"], leg, blx + 0.40 * bw, bty + 0.12 * bh, clip=free)


# ---------------------------------------------------------------- 명암 (화면 공간 — 돌린 판도 같은 광원)
CUTS = {"gel": [0.30, 0.50, 0.74, 0.92], "gold": [0.32, 0.52, 0.70, 0.86], "soul": [0.38, 0.58, 0.80, 0.94]}


# 손 계단: 행 · 열마다 띠 경계를 0~2칸 밀어 「2-1-2-3」처럼 길이가 섞인 계단 (고정된 짧은 수열 — 결정적)
JIT_ROW = [0, 0, 1, 1, 0, 1, 0, 0, 0, 1, 1, 1, 0, 1, 0, 0, 1]
JIT_COL = [0, 1, 1, 0, 0, 0, 1, 0, 1, 1, 0, 0, 0]
HAND_SHARE = [0.22, 0.44, 0.66, 0.84]       # 몸 면적 몫으로 띠 문턱 (밝음 22% · 22 · 22 · 18 · 짙음 16% — 한 색 큰 면 ≤ 0.15 목표)


def hand_tones(L, body):
    """몸 명암 (판정 1회차 고침): 법선 램버트의 곧은 대각 띠 대신, 왼쪽 위 빛 중심을 둘러싼 타원 호 —
    위 돔이 밝고 아래 · 오른쪽으로 감싸며 어두워짐. 띠 경계는 손 계단(JIT_*)으로 길이를 섞음."""
    x0, y0, x1, y1 = bp.bbox(body)
    w, h = x1 - x0, y1 - y0
    hx, hy = x0 + L.hand[0] * w, y0 + (L.hand[2] if len(L.hand) > 2 else 0.16) * h
    ys, xs = np.mgrid[0:body.shape[0], 0:body.shape[1]]
    jr = np.array(JIT_ROW, float)[ys % len(JIT_ROW)]
    jc = np.array(JIT_COL, float)[xs % len(JIT_COL)]
    dx = (xs + 0.5 + jr - hx) / (0.80 * w)
    dy = (ys + 0.5 + jc - hy) / ((L.hand[3] if len(L.hand) > 3 else 1.02) * h)
    d = np.sqrt(dx * dx + dy * dy * 1.15)
    cuts = np.quantile(d[body], L.hand[1])
    return 4 - np.digitize(d, cuts)


def light(L, pal, vec=shade.LIGHT, quant=None, hand=True):
    """quant: 젤리 단계 문턱을 이 판의 밝기 분위수로 (넓은 한 단계 면이 생기지 않게 — 누운 몸처럼 한쪽 면이 넓을 때)"""
    lam = shade.lambert(L.n, vec, wrap=0.15)
    op = L.mat > 0
    # 땅에서 튀는 반사광: 아래를 보는 젤리 칸 중 아래쪽 빈칸에서 3칸 안
    gel = (L.mat == MAT["gel"]) & L.shaded
    below_empty = np.zeros_like(op)
    for d in range(1, 4):
        sh = np.zeros_like(op)
        sh[:-d] = ~op[d:]
        sh[-d:] = True
        below_empty |= sh
    refl = gel & below_empty & (L.n[..., 1] < -0.25)
    for m, cuts in CUTS.items():
        sel = (L.mat == MAT.get(m, -1)) & L.shaded
        if not sel.any():
            continue
        if quant and m == "gel":
            cuts = list(np.quantile(lam[sel], quant))
        t = shade.bands(lam, cuts) + L.toff
        if m == "gel" and hand:
            body = (L.zone == 0) & op
            t = np.where(body, hand_tones(L, body) + L.toff, t)
        L.tone[sel] = np.clip(t, 0, 4)[sel]
    L.mat[refl] = MAT["refl"]
    # 바닥 반사 띠: 손 길이 마디(왼쪽은 밝은 마디가 길고 오른쪽으로 갈수록 짧아짐)로 번갈아 — 젖은 바닥 반짝임
    if refl.any():
        rx0, _, rx1, _ = bp.bbox(refl)
        seg = np.zeros(L.mat.shape[1], int)
        x, i, lens = rx0, 0, [9, 3, 7, 2, 5, 4, 3, 6, 2, 8, 3, 5]
        while x < rx1:
            n = lens[i % len(lens)]
            seg[x:x + n] = 1 if (i % 2 == 0 and x < rx0 + 0.75 * (rx1 - rx0)) else 0
            x, i = x + n, i + 1
        L.tone[refl] = np.broadcast_to(seg, L.mat.shape)[refl]
    L.shaded[refl] = False
    # 단계 띠 정리: 3칸 미만 조각은 이웃 단계로 (명암 칸만)
    for m in ("gel", "gold"):
        sel = (L.mat == MAT[m]) & L.shaded
        tone = L.tone
        for t in range(5):
            lab, nl = ndimage.label(sel & (tone == t), structure=bp.EIGHT)
            if not nl:
                continue
            sizes = np.bincount(lab.ravel())
            for i in np.nonzero(sizes[1:] < 3)[0]:
                cells = lab == i + 1
                ring = ndimage.binary_dilation(cells) & ~cells & sel
                if ring.any():
                    tone[cells] = np.bincount(tone[ring]).argmax()


def to_rgb(L, pal):
    Hh, W = L.mat.shape
    rgb = np.zeros((Hh, W, 3), np.uint8)
    inv = {v: k for k, v in MAT.items()}
    for mi in np.unique(L.mat):
        if mi == 0:
            continue
        ramp = pal[inv[mi]]
        sel = L.mat == mi
        idx = np.clip(L.tone[sel], 0, len(ramp) - 1)
        rgb[sel] = np.array(ramp, np.uint8)[idx]
    return rgb


def finish(L, pal, cool_quarter=True):
    """외곽 1칸 (금 옆 따뜻한 색 · 오른쪽 아래 그림자 쪽 보조색) → 1칸 섬 정리 → RGBA"""
    op = L.mat > 0
    rgb = to_rgb(L, pal)
    edge = outline.edge_cells(op)
    zone_in = outline.inner_neighbor(L.zone, op, edge)
    mat_in = outline.inner_neighbor(L.mat, op, edge)
    dx, dy = outline.outward(op)
    rgb[edge] = outline.OL
    tone_in = outline.inner_neighbor(L.tone, op, edge)
    warm = edge & ((zone_in == 1) | (mat_in == MAT["gold"])) & (tone_in >= 3) & (dy <= 0)     # 밝은 금 옆만 (한 색 몫 75~95%)
    rgb[warm] = outline.OL_WARM
    if cool_quarter:
        cool = outline.no_lonely(edge & ~warm & (dx > 0) & (dy > 0))
        rgb[cool] = OL_COOL
    # 왕관과 젤리 사이 안쪽 선 (따뜻한 외곽색 1칸): 젤리와 4방향으로 맞닿은 왕관 칸
    cross = np.array([[0, 1, 0], [1, 1, 1], [0, 1, 0]], bool)
    seam = (L.zone == 1) & ~edge & ndimage.binary_dilation((L.zone == 0) & op, structure=cross)
    rgb[seam] = outline.OL_WARM
    cleanup.kill_singles(rgb, op, protect=edge | seam)
    return cleanup.to_rgba(rgb, op)


def frame_upright(name, ghost=False):
    g = UPRIGHT[name]
    L, (ox, oy), body = build_upright(g, ghost)
    pal = GHOST if ghost else LIVE
    light(L, pal)
    img = finish(L, pal)
    out = np.zeros((CELL, CELL, 4), np.uint8)
    h, w = img.shape[:2]
    out[oy:oy + h, ox:ox + w] = img
    return out


FALLEN_LIGHT = [-0.12, 0.36, 0.93]          # 누운 몸 왕관 빛: 앞쪽 비중 큼 (몸은 손 지도 · 위 광원 상관 ≤ 0.65)
FALLEN_QUANT = [0.18, 0.42, 0.68, 0.90]
FALLEN_HAND = (0.46, HAND_SHARE, 0.20, 1.22)   # 누운 몸 손 명암 (빛 중심 가로 · 몫 · 세로 · 세로 폭)


def frame_fallen():
    """옆으로 쓰러짐: 세운 그림을 시계 방향 90° (왕관이 오른쪽) → 화면 광원으로 다시 비추고 윤은 화면 왼쪽 위에"""
    g = UPRIGHT["stand"]
    L, _, body = build_upright(g, with_shine=False)
    R = L.rot_cw()
    R.hand = FALLEN_HAND
    # 몸 명암은 화면 빛(왼쪽 위) 기준 손 도트 지도로 다시 (돌린 칸0 지도는 빛이 오른쪽 위가 되므로 덮어씀)
    leg0, grids0 = detail.load_handmaps("slimeking")
    rbody = np.rot90(body, -1) & (R.zone == 0) & ((R.mat == MAT["gel"]) | (R.mat == MAT["refl"]))
    keep = (R.mat == MAT["gel"]) | (R.mat == MAT["refl"])
    R.mat[keep & np.rot90(body, -1)] = MAT["gel"]
    # 입: 돌린 웃는 입 대신 옛 설계도처럼 짧은 입 (판정 3회차)
    mouth = R.mat == MAT["mouth"]
    my_, mx_ = np.argwhere(mouth).mean(axis=0) if mouth.any() else (0, 0)
    R.mat[mouth] = MAT["gel"]
    rbody = rbody | mouth
    apply_tonemap(R, grids0["body_fallen"], 61 - int(round(px(12))), rbody)
    stamp(R, grids0["mouth_side"], leg0, mx_ - 1, my_ - 2)
    leg, grids = detail.load_handmaps("slimeking")
    rb = np.rot90(body, -1)
    x0, y0, x1, y1 = bp.bbox(rb)
    add_shine(R, grids, leg, rb & (R.mat == MAT["gel"]), x0 + 1, y0 + 2, x1 - x0 - 12, y1 - y0)
    # 누운 몸은 위를 보는 면이 넓어 위 광원 상관이 너무 커짐 → 앞쪽 비중을 늘린 같은 방향 빛
    light(R, LIVE, shade.normalize(np.array(FALLEN_LIGHT)), quant=FALLEN_QUANT)
    img = finish(R, LIVE, cool_quarter=False)
    out = np.zeros((CELL, CELL, 4), np.uint8)
    ox, oy = int(round(px(9))), int(round(px(12)))
    h, w = img.shape[:2]
    out[oy:oy + h, ox:ox + w] = img
    return out


# ---------------------------------------------------------------- 영혼 (덩이 · 조각 · 흩어짐)
SOUL_COLORS = {"#AAEE96": 1, "#68D466": 0, "#30A04E": -1, "#7DDAA0": 0, "#5ABA91": 0}
SPARK_COLORS = ["#BEF5FF", "#E1FCFF", "#FFFFFF", "#E4FED4"]


LOBE_STEP = 15          # 혹 간격 (화소)


def lobe_centers(m, step):
    """덩어리 안 혹 중심 — 깎인 안쪽에 엇갈린 격자 (결정적)"""
    er = ndimage.binary_erosion(m, iterations=3)
    if m.sum() < 2.2 * step * step or not er.any():
        return []
    pts = []
    y0, x0, y1, x1 = *np.argwhere(m).min(axis=0), *np.argwhere(m).max(axis=0)
    for i, y in enumerate(range(int(y0) + step // 2, int(y1) + 1, step)):
        for x in range(int(x0) + step // 2 + (step // 2 if i % 2 else 0), int(x1) + 1, step):
            if er[y, x]:
                pts.append((y, x))
    return pts if len(pts) >= 2 else []


def lobes(m, step):
    pts = lobe_centers(m, step)
    if not pts:
        return [m]
    ys, xs = np.nonzero(m)
    d = np.stack([(ys - py) ** 2 + (xs - px_) ** 2 for py, px_ in pts])
    owner = d.argmin(axis=0)
    out = []
    for i in range(len(pts)):
        lm = np.zeros_like(m)
        lm[ys[owner == i], xs[owner == i]] = True
        out.append(lm)
    return out


def lobe_seams(m, step):
    """혹 경계 중 아래쪽 혹 칸 (위 혹이 앞에 겹친 그림자)"""
    pts = lobe_centers(m, step)
    seam = np.zeros_like(m)
    if not pts:
        return seam
    ys, xs = np.nonzero(m)
    d = np.stack([(ys - py) ** 2 + (xs - px_) ** 2 for py, px_ in pts])
    own = np.full(m.shape, -1)
    own[ys, xs] = d.argmin(axis=0)
    cy = np.array([p[0] for p in pts])
    H_, W_ = m.shape
    for y, x in zip(ys, xs):
        for dy, dx in ((-1, 0), (0, -1), (0, 1)):
            yy, xx = y + dy, x + dx
            if 0 <= yy < H_ and 0 <= xx < W_ and own[yy, xx] >= 0 and own[yy, xx] != own[y, x] and cy[own[yy, xx]] < cy[own[y, x]]:
                seam[y, x] = True
    return seam


def frame_soul(design, lift=0.35, big_star=True, extra=(), pairs=None, vec=shade.LIGHT, quant=None, step=LOBE_STEP, bright=0, nudges=(), top_col=None):
    a = design[..., 3] > 127
    tb = bp.Grid(K).box(bp.bbox(a))
    target = np.zeros((CELL, CELL), bool)
    target[tb[1]:tb[3], tb[0]:tb[2]] = True             # 설계도 상자 × k 밖으로 안 나가게 (검사 14)
    spark = bp.color_mask(design, SPARK_COLORS)
    Hh = W = CELL
    L = Layer(Hh, W)
    leg, grids = detail.load_handmaps("slimeking")
    ys, xs = np.mgrid[0:Hh, 0:W]
    # 덩어리마다: 부드럽게 키운 모양 + 타원체 법선 + 설계도 색(밝음 · 어두움)으로 단계 치우침
    bias = np.zeros((Hh, W))
    comp_i = single_i = 0
    for comp, (cx0, cy0, cx1, cy1) in bp.components(a):
        m = ndimage.binary_fill_holes(bp.soft_upscale(comp, K, (Hh, W), sigma=0.3, thr=0.42)) & target
        for ci, cut, ddx, ddy in nudges:                 # 손으로 정한 파편 밀기 (가로줄 정렬 흐트러뜨림 · cut 열 오른쪽만 = 금 가르기)
            if ci == comp_i:
                part = m & (xs >= px(cut)) if cut is not None else m.copy()
                moved = np.zeros_like(m)
                ys_, xs_ = np.nonzero(part)
                ys2, xs2 = np.clip(ys_ + ddy, 0, Hh - 1), np.clip(xs_ + ddx, 0, W - 1)
                moved[ys2, xs2] = True
                m = ((m & ~part) | moved) & target
        comp_i += 1
        if 2 <= comp.sum() <= 3 and pairs and not (comp & spark).any():                     # 두 칸 알갱이: 칸마다 방울(split) · 가운데 방울 하나(one)
            cells = np.argwhere(comp).astype(float)
            pts = cells if pairs == "split" else [cells.mean(axis=0)]
            mid = cells.mean(axis=0)
            for cy, cx in pts:
                # 붙은 칸끼리는 가운데에서 1화소씩 벌려 방울마다 따로 (아령 모양 방지)
                oy, ox = np.sign(cy - mid[0]), np.sign(cx - mid[1])
                hit = stamp(L, grids["mote"], leg, int(round(px(cx) + ox)), int(round(px(cy) + oy)))
                L.tone[hit] = np.clip(L.tone[hit] + (1 if cy < 13 else -1 if cy > 20 else 0), 0, 4)
            continue
        if comp.sum() == 1:                               # 한 칸 알갱이 → 손 도트 지도 5×5 영혼 방울 (높이 따라 한 단계 밝게 · 어둡게)
            x0, y0 = int(round(px(cx0))), int(round(px(cy0)))
            single_i += 1
            hit = stamp(L, grids[("mote", "m4", "d3w", "d3s")[single_i % 4]], leg, x0, y0)   # 한 칸 알갱이마다 다른 모양
            c = "#%02X%02X%02X" % tuple(design[cy0, cx0, :3])
            shift = SOUL_COLORS.get(c, 0) + (1 if cy0 < 13 else -1 if cy0 > 20 else 0)
            L.tone[hit] = np.clip(L.tone[hit] + int(np.clip(shift, -1, 1)), 0, 4)
            continue
        L.mat[m] = MAT["soul"]
        L.shaded[m] = True
        # 큰 덩어리는 뭉게구름처럼 여러 혹(lobe)으로: 혹마다 타원체 명암 + 혹 사이 그림자 선 (한 덩어리 베개 명암 방지)
        for lm in lobes(m, step):
            X0, Y0, X1, Y1 = bp.bbox(lm)
            e = shade.ellipsoid((Hh, W), (X0 + X1) / 2, (Y0 + Y1) / 2, (X1 - X0) / 2 + 0.8, (Y1 - Y0) / 2 + 0.8)
            n = shade.from_height(e, lm, depth=max(2.0, (X1 - X0) / 2))
            L.n[lm] = n[lm]
        seam = lobe_seams(m, step)
        L.tone[seam], L.shaded[seam] = 0, False
        # 큰 조각마다 속 거품 · 알갱이 (조각 가운데)
        if m.sum() > 60:
            er = ndimage.binary_erosion(m, iterations=2)
            cy_, cx_ = np.argwhere(er).mean(axis=0) if er.any() else (0, 0)
            for name, ddx, ddy in (("bubble_s", -3, -2), ("speck", 3, 2)) if m.sum() > 400 else (("speck", -1, -1),):
                rows = grids[name]
                stamp(L, rows, leg, cx_ + ddx - len(rows[0]) / 2, cy_ + ddy - len(rows) / 2, clip=er)
        cols = {"#%02X%02X%02X" % tuple(design[y, x, :3]) for y, x in np.argwhere(comp)}
        b = np.mean([SOUL_COLORS.get(c, 0) for c in cols]) if cols else 0
        bias[m] = b * 0.06
    # 반짝이: 설계도의 하늘빛 흰 칸 덩어리마다 별 (3칸 넘으면 큰 별)
    for comp, (sx0, sy0, sx1, sy1) in bp.components(spark):
        cx, cy = px((sx0 + sx1) / 2), px((sy0 + sy1) / 2)
        rows = grids["star"] if comp.sum() >= 3 and big_star else grids["star_s"]
        hit = stamp(L, rows, leg, cx - len(rows[0]) / 2, cy - len(rows) / 2)
        L.zone[hit] = 2
    a_soul = (L.mat == MAT["soul"]) & L.shaded
    for name, fx, fy in extra:                        # 덩이 속 거품 (덩이 상자 비율 자리)
        X0, Y0, X1, Y1 = bp.bbox(a_soul)
        rows = grids[name]
        stamp(L, rows, leg, X0 + fx * (X1 - X0) - len(rows[0]) / 2, Y0 + fy * (Y1 - Y0) - len(rows) / 2, clip=ndimage.binary_erosion(a_soul, iterations=2))
    # 명암: 화면 높이에 따라 위로 갈수록 밝게 (솟아오르는 영혼) + 덩어리 광원
    lam = shade.lambert(L.n, vec, wrap=0.2)
    op = L.mat > 0
    top, bot = np.nonzero(op)[0].min(), np.nonzero(op)[0].max()
    rise = (bot - ys) / max(1, bot - top) - 0.5
    v = lam + bias + lift * rise
    sel = (L.mat == MAT["soul"]) & L.shaded
    cuts = list(np.quantile(v[sel], quant)) if quant else CUTS["soul"]
    L.tone[sel] = np.clip(shade.bands(v, cuts), 0, 4)[sel]
    # 프레임 밝기 (설계도 몸 밝기 ± 15 — 옛 영혼은 밝은 연두 · 흰빛): 영혼 칸 단계를 bright 만큼 밝은 쪽으로
    soul = L.mat == MAT["soul"]
    L.tone[soul] = np.clip(L.tone[soul] + bright, 0, len(SOUL["soul"]) - 1)
    pal = dict(SOUL)
    if top_col:                                       # 가장 밝은 단계 바꿔 쓰기 (판정 3회차: 흰 면 → 연한 민트, 얼음 조각 느낌 줄이기)
        pal["soul"] = SOUL["soul"][:-1] + [bp.hexrgb(top_col)]
    rgb = to_rgb(L, pal)
    edge = outline.edge_cells(op)
    dx, dy = outline.outward(op)
    rgb[edge] = outline.OL
    rgb[outline.no_lonely(edge & (dx > 0) & (dy > 0))] = OL_COOL
    cleanup.kill_singles(rgb, op, protect=edge)
    return cleanup.to_rgba(rgb, op)


# ---------------------------------------------------------------- 흩어짐 · 사라짐 (판정 1회차: 정확한 원호 고리 → 불규칙 산포)
def frame_scatter(design, bright=0, drop_outer=3, seed=7, big_ok=True):
    """설계도 알갱이 자리를 바탕으로: 바깥으로 갈수록 밀어내며 성기게(바깥 알갱이 drop_outer 개마다 하나 뺌),
    가운데 가까운 것은 큰 알갱이(설계도 두 칸) · 바깥은 작은 알갱이(한 칸) — 상자 끝 알갱이는 그 자리 그대로 (검사 14)"""
    a = design[..., 3] > 127
    spark = bp.color_mask(design, SPARK_COLORS)
    L = Layer(CELL, CELL)
    leg, grids = detail.load_handmaps("slimeking")
    bx0, by0, bx1, by1 = bp.bbox(a)
    comps = bp.components(a & ~spark)
    pts = [np.argwhere(c).mean(axis=0) for c, _ in comps]
    cy0, cx0 = np.mean(pts, axis=0) if pts else (16, 16)
    rmax = max(1e-6, max(np.hypot(py - cy0, px_ - cx0) for py, px_ in pts)) if pts else 1
    rng = np.random.default_rng(seed)
    outer_i = 0
    for (comp, (x0, y0, x1, y1)), (py, px_) in zip(comps, pts):
        extreme = x0 == bx0 or x1 == bx1 or y0 == by0 or y1 == by1
        r = np.hypot(py - cy0, px_ - cx0) / rmax
        if r > 0.62 and not extreme:
            outer_i += 1
            if outer_i % drop_outer == 0:
                continue                                    # 바깥은 성기게
        big = big_ok and (comp.sum() >= 2 or r < 0.38) and not extreme
        rows = grids["mote_l" if big else "mote"]
        n = len(rows)
        cxp, cyp = px(px_ + 0.5), px(py + 0.5)
        if not extreme:                                     # 바깥쪽으로 조금 밀고 (멀수록 많이) 손 흔들림 ±2
            ang = np.arctan2(py - cy0, px_ - cx0)
            push = 2.5 * r
            cxp += np.cos(ang) * push + rng.integers(-2, 3)
            cyp += np.sin(ang) * push + rng.integers(-2, 3)
            x, y = int(round(cxp - n / 2)), int(round(cyp - n / 2))
            tb = bp.Grid(K).box((bx0, by0, bx1, by1))
            x = int(np.clip(x, tb[0], tb[2] - n)); y = int(np.clip(y, tb[1], tb[3] - n))
        else:
            x, y = int(round(px(x0))), int(round(px(y0)))
        hit = stamp(L, rows, leg, x, y)
        c = "#%02X%02X%02X" % tuple(design[int(py), int(px_), :3])
        # 위로 솟은 알갱이일수록 밝게 (솟아오르는 영혼 · 광원 쪽)
        rise = int(np.clip(((by1 - py) / max(1, by1 - by0) * 0.65 + (bx1 - px_) / max(1, bx1 - bx0) * 0.35) * 3.0, 0, 2)) - 1   # 위 · 왼쪽일수록 밝게
        L.tone[hit] = np.clip(L.tone[hit] + SOUL_COLORS.get(c, 0) + rise + bright, 0, len(SOUL["soul"]) - 1)
    for comp, (sx0, sy0, sx1, sy1) in bp.components(spark):
        rows = grids["star_s"]
        hit = stamp(L, rows, leg, px((sx0 + sx1) / 2) - len(rows[0]) / 2, px((sy0 + sy1) / 2) - len(rows) / 2)
        L.zone[hit] = 2
    op = L.mat > 0
    rgb = to_rgb(L, dict(SOUL))
    edge = outline.edge_cells(op)
    dx, dy = outline.outward(op)
    rgb[edge] = outline.OL
    rgb[outline.no_lonely(edge & (dx > 0) & (dy > 0))] = OL_COOL
    cleanup.kill_singles(rgb, op, protect=edge)
    return cleanup.to_rgba(rgb, op)


# ---------------------------------------------------------------- 영혼 덩이 · 갈라짐 (판정 4회차: 설계도 덩어리를 키운 곧은 변 → 손으로 놓은 둥근 혹 · 물방울)
# 혹 = (가운데 x, y, 반지름 x, 반지름 y) 화소 · 상자 끝 혹은 설계도 상자 × k 에 닿게 (검사 14)
PUFF_F4 = [(78, 108, 16, 15), (66, 99, 10, 9), (92, 99, 11, 10), (67, 117, 11, 10), (90, 120, 10, 9), (75, 124, 9, 8),
           (82, 90, 4, 4), (103, 110, 5, 4), (107, 108, 4, 4)]
STARS_F4 = [("star", 70, 95), ("star_s", 54 + 6, 113), ("star_xs", 86, 115)]
# 갈라짐: 네 조각 크기비 ≈ 1 : 0.6 : 0.4 : 0.3 · 하나(셋째)는 위로 멀리 · 작은 방울 넷 (오른쪽 아래 뭉침 · 상자 끝)
PIECES_F5 = [
    [(64, 112, 19, 13), (49, 105, 6, 5), (44, 103, 4, 3)],                 # 1 큰 조각 (왼쪽으로 꼬리 · 상자 왼끝)
    [(104, 122, 14, 11), (117, 129, 5, 4)],                                # 0.6 오른쪽 아래
    [(98, 87, 11, 9), (106, 80, 4, 4)],                                    # 0.4 위로 날린 조각 (상자 위끝)
    [(55, 133, 10, 8), (61, 139, 4, 3)],                                   # 0.3 왼쪽 아래 (상자 아래끝)
    [(121, 105, 3, 3)], [(125, 111, 2, 2)], [(116, 99, 2, 3)], [(88, 136, 4, 2)],   # 작은 방울: 오른쪽 위 뭉침 셋 (상자 오른끝) · 아래 하나
]
STARS_F5 = [("star_s", 58, 104), ("star_xs", 99, 117), ("star_t", 94, 82)]


def blob_shape(lobes, shape=(CELL, CELL)):
    """혹 목록 → 모양 · 혹마다 타원체 법선 · 혹 사이 그림자 (작은 혹이 큰 혹 앞에 겹친 경계)"""
    Hh, W = shape
    ys, xs = np.mgrid[0:Hh, 0:W]
    m = np.zeros(shape, bool)
    owner = np.full(shape, -1)
    best = np.full(shape, 9.0)
    for i, (cx, cy, rx, ry) in enumerate(lobes):
        d = ((xs + 0.5 - cx) / rx) ** 2 + ((ys + 0.5 - cy) / ry) ** 2
        inside = d <= 1.0
        m |= inside
        better = inside & (d < best)
        owner[better], best[better] = i, d[better]
    n = np.zeros(shape + (3,))
    for i, (cx, cy, rx, ry) in enumerate(lobes):
        e = shade.ellipsoid(shape, cx, cy, rx + 0.6, ry + 0.6)
        lm = owner == i
        nn = shade.from_height(e, lm, depth=max(2.0, rx))
        n[lm] = nn[lm]
    seam = np.zeros(shape, bool)
    for y, x in np.argwhere(m):
        o = owner[y, x]
        for dy, dx in ((-1, 0), (0, -1), (0, 1), (1, 0)):
            yy, xx = y + dy, x + dx
            if 0 <= yy < Hh and 0 <= xx < W and owner[yy, xx] >= 0 and owner[yy, xx] != o and lobes[owner[yy, xx]][3] > lobes[o][3]:
                seam[y, x] = True                   # 큰 혹 쪽 경계 한 줄을 그림자로 (작은 혹이 앞)
    return m, n, seam


def frame_lobes(groups, stars, extras=(), vec=shade.LIGHT, quant=(0.12, 0.30, 0.52, 0.74), bright=2, top_col=None):
    """손으로 놓은 혹 묶음(조각마다 혹 목록) → 영혼 덩이 / 조각. 명암 = 혹 타원체 × 광원 → 분위수 5단 (+bright)"""
    L = Layer(CELL, CELL)
    leg, grids = detail.load_handmaps("slimeking")
    allm = np.zeros((CELL, CELL), bool)
    seams = np.zeros((CELL, CELL), bool)
    for lobes in groups:
        m, n, seam = blob_shape(lobes)
        L.mat[m] = MAT["soul"]
        L.n[m] = n[m]
        L.shaded[m] = True
        allm |= m
        seams |= seam
    for name, fx, fy in extras:                       # 덩이 속 거품 · 소용돌이 (손 자리)
        rows = grids[name]
        stamp(L, rows, leg, fx, fy, clip=ndimage.binary_erosion(allm, iterations=2))
    lam = shade.lambert(L.n, vec, wrap=0.2)
    sel = (L.mat == MAT["soul"]) & L.shaded
    cuts = list(np.quantile(lam[sel], quant))
    L.tone[sel] = (shade.bands(lam, cuts) + bright)[sel]
    L.tone[seams & sel] -= 1
    soul = L.mat == MAT["soul"]
    L.tone[soul] = np.clip(L.tone[soul], 0, len(SOUL["soul"]) - 1)
    for name, x, y in stars:
        rows = grids[name]
        hit = stamp(L, rows, leg, x - len(rows[0]) // 2, y - len(rows) // 2)
        L.zone[hit] = 2
    pal = dict(SOUL)
    if top_col:
        pal["soul"] = SOUL["soul"][:-1] + [bp.hexrgb(top_col)]
    op = L.mat > 0
    rgb = to_rgb(L, pal)
    edge = outline.edge_cells(op)
    dx, dy = outline.outward(op)
    rgb[edge] = outline.OL
    rgb[outline.no_lonely(edge & (dx > 0) & (dy > 0))] = OL_COOL
    cleanup.kill_singles(rgb, op, protect=edge)
    return cleanup.to_rgba(rgb, op)


# ---------------------------------------------------------------- 흩어짐 · 사라짐 (판정 3회차: 알갱이마다 다른 손 모양 · 크기 · 뭉침과 빈 곳)
# (도장 이름, 화소 x, 화소 y, 단계 밀기) — 도장 sA~sT 는 handmaps 의 손 모양 (한 칸 안에서 같은 모양 두 번 안 씀)
#   g1~g3 = 3칸 이하 낱알: 외곽 없는 초록 (어두운 외곽 조각이 남지 않게) · 상자 끝 알갱이는 설계도 상자 × k 에 닿게 (검사 14)
MOTES_F6 = [
    # 오른쪽 위 뭉침 (솟아오르는 쪽 · 크고 밝게)
    ("g1", 98, 56, 0), ("sA", 102, 58, 0), ("sD", 114, 70, 0), ("star_s", 124, 60, 0), ("sF", 133, 75, 0), ("sM", 138, 81, -1),
    # 왼쪽 가운데 뭉침
    ("sI", 51, 66, 0), ("sG", 61, 61, 1), ("sK", 57, 78, -1), ("star_xs", 80, 73, 0),
    # 바깥으로 갈수록 성긴 낱알 (아래일수록 어둡게)
    ("sJ", 25, 88, 0), ("g2", 40, 104, 0), ("sH", 88, 94, 0), ("sB", 95, 107, -1), ("sE", 66, 112, -1), ("sC", 108, 120, -1),
]
MOTES_F7 = [
    # 칸 6 보다 적게 (12개) · 대부분 작게. 큰 알갱이 넷(속 한 색 면 ≤ 8칸)은 바깥선 고리를 두 칸 짝 두 곳으로 끊고,
    # 작은 알갱이 일곱은 고리 8칸 이하라 그대로 (「가장 큰 덩어리 몫 ≤ 0.042」 한도 안) · 뭉침 셋과 큰 빈 곳
    ("sR", 93, 40, 1), ("m7", 98, 44, 0), ("sS", 107, 41, 1), ("d3w", 106, 51, 0),     # 위 오른쪽 뭉침 (상자 위끝)
    ("sC", 31, 84, 0), ("sF", 40, 93, -1), ("g3", 45, 100, 0), ("sJ", 20, 77, 0),      # 왼쪽 뭉침 (상자 왼끝)
    ("sE", 120, 97, 0), ("sG", 131, 104, -1),                                          # 오른쪽 아래 짝
    ("sO", 148, 68, 0), ("d3d", 58, 118, -1),                                         # 상자 오른끝 · 아래끝 외톨이
]


def frame_motes(placements, bright=0, cut_rings=False):
    """손 배치 알갱이. cut_rings: 알갱이마다 바깥선 고리를 짙은 두 칸(오른쪽 아래 그림자색 · 왼쪽 위 따뜻한 색)으로 끊어
    한 색 바깥선 덩어리를 반으로 (설계도가 한 칸 점뿐인 칸 7 의 「가장 큰 덩어리 몫」 한도)"""
    L = Layer(CELL, CELL)
    leg, grids = detail.load_handmaps("slimeking")
    motes, bare = [], np.zeros((CELL, CELL), bool)
    for name, x, y, shift in placements:
        hit = stamp(L, grids[name], leg, x, y)
        if name.startswith("star"):
            L.zone[hit] = 2
            continue
        if name.startswith("g"):                      # 외곽 없는 낱알
            bare |= hit
            continue
        L.tone[hit] = np.clip(L.tone[hit] + shift + bright, 0, len(SOUL["soul"]) - 1)
        inner = hit & ~outline.edge_cells(hit)
        if 0 < inner.sum() <= 4:                      # 작은 알갱이 속은 한 색 (속 1칸짜리 다른 색 = 1칸 섬)
            L.tone[inner] = L.tone[inner].max()
        motes.append(hit)
    op = L.mat > 0
    rgb = to_rgb(L, dict(SOUL))
    keep = rgb[bare].copy()
    edge = outline.edge_cells(op)
    dx, dy = outline.outward(op)
    rgb[edge] = outline.OL
    if not cut_rings:
        rgb[outline.no_lonely(edge & (dx > 0) & (dy > 0))] = OL_COOL
    else:
        limit = 0.042 * op.sum()                      # 검사 9 한도 (설계도 칸 7 = 0.053 × 0.8)
        for hit in motes:
            e = np.argwhere(edge & hit)
            if len(e) <= limit:                       # 한도 안 고리는 끊지 않음 (한 색 몫 유지)
                continue
            ssum = e[:, 0] + e[:, 1]
            for k0, col, sgn in ((ssum.argmax(), OL_COOL, 1), (ssum.argmin(), outline.OL_WARM, -1)):   # 두 칸 짝으로 끊음 (1칸 섬 방지)
                y0_, x0_ = e[k0]
                nb = [(yy, xx) for yy, xx in e if max(abs(yy - y0_), abs(xx - x0_)) == 1]
                y1_, x1_ = max(nb, key=lambda q: sgn * (q[0] + q[1])) if nb else (y0_, x0_)
                rgb[y0_, x0_] = col
                rgb[y1_, x1_] = col
    rgb[bare] = keep
    cleanup.kill_singles(rgb, op, protect=edge | bare)
    return cleanup.to_rgba(rgb, op)



def body_extents(key):
    """손 구간(runs2grid.py)용: 시트 행 → (몸 시작열, 끝열) · 열은 몸 판 왼쪽 기준"""
    if key == "fallen":
        L, _, body = build_upright(UPRIGHT["stand"], with_shine=False)
        body = np.rot90(body, -1)
        oy = int(round(px(12)))
    else:
        L, (_, oy), body = build_upright(UPRIGHT[key])
    out = {}
    for y in range(body.shape[0]):
        xs = np.nonzero(body[y])[0]
        if len(xs):
            out[y + oy] = (int(xs.min()), int(xs.max()))
    return out


# ---------------------------------------------------------------- 전체
def design_frames(sheet):
    """옛 시트(256×416) 또는 설계도 시트(128×64 · 4×2)에서 쓰는 8칸"""
    if sheet.shape[:2] == (64, 128):
        return bp.cells(sheet, 32)
    return [sheet[r * 32:(r + 1) * 32, c * 32:(c + 1) * 32] for c, r in DESIGN_CELLS]


def build(design_sheet):
    d = design_frames(design_sheet)
    # 설계도 상자 확인 (손으로 읽은 부위 상자가 설계도와 맞는지)
    for fi, key in ((0, "stand"), (2, "squash")):
        got = bp.bbox(d[fi][..., 3] > 127)
        if got != UPRIGHT[key]["box"]:
            raise SystemExit(f"설계도 칸 {fi} 상자 {got} ≠ 표 {UPRIGHT[key]['box']}")
    frames = [
        frame_upright("stand"),
        frame_upright("stand", ghost=True),
        frame_upright("squash"),
        frame_fallen(),
        frame_lobes([PUFF_F4], STARS_F4, extras=(("bubble_m", 70, 116), ("bubble_s", 95, 104), ("swirl", 76, 101), ("speck", 62, 106)),
                    vec=shade.normalize(np.array([-0.10, 0.38, 0.92])), quant=(0.05, 0.16, 0.36, 0.60), bright=2, top_col="#D6F9CE"),
        frame_lobes(PIECES_F5, STARS_F5, quant=(0.08, 0.22, 0.46, 0.72), bright=2, top_col="#D6F9CE"),
        frame_motes(MOTES_F6, bright=1),      # 밝은 초록(A6EEA8 · 6EDE79) 중심 — 죽음 흐름 4 → 5 → 6 → 7 밝기가 한 방향으로 줄게 (검사 13 예외 · 판정 4회차 지시)
        frame_motes(MOTES_F7, bright=0, cut_rings=True),
    ]
    return dict(frames=frames, design=d, cols=4, cell=CELL, ppu=PPU, extrude=EXTRUDE, sprites=SPRITES, asset=ASSET, scale=SCALE)
