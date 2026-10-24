"""그림 품질 검사기 (그래픽 가독성 개편 2단계 2-5 · 묶음 T)

새 고해상도 그림(보스 · 메뉴 초상 · 아이콘)이 지금 손 그림체와 같은 규칙을 지키는지 숫자로 판정한다.
외곽 두께 · 색 수 · 반투명 · 덩어리(디테일) · 1칸 섬 · 넓은 평면 · 위/왼 광원 · 베개 명암 · 몸 밝기 · 상자 · 여백.
문턱은 대조군(옛 그림을 그냥 키운 것 · 부드럽게 키워 색을 줄인 것 · 베개 명암)으로 보정했다 → `--calibrate`.

사용법:
  py -3.14 -X utf8 tools/art/lint.py <그림.png> [--design <설계도.png>] [--k K] [--kind boss|portrait|icon|medal|sprite]
        [--cell 162x162 --frame 0 | --frames all] [--design-cell 32x32] [--glow 칸.json] [--dots 칸.json]
        [--scale 배율 --ppu PPU --ortho 22] [--ui 칸] [--out metrics.txt] [--json]
  py -3.14 -X utf8 tools/art/lint.py --calibrate          (Unity 없이 · 손 그림 통과율 + 대조군 A · B · C (+D · E) 탈락 확인)

  --design   같은 프레임의 32칸 설계도(옛 그림 · 새 설계도). 7 · 9 · 12 · 14 항목의 기준. 없으면 그 항목은 건너뜀
  --k        설계도 1칸 = 새 그림 몇 칸 (없으면 칸 너비 비로 추정)
  --cell/--frame/--frames   시트에서 칸 고르기 (가로로 먼저 · 0 부터). 설계도도 같은 번호 칸 (--design-cell)
  --glow     발광 칸 목록 JSON [[x, y], ...] (칸 안 좌표 · 위가 y=0) — 2 · 3 항목에서 뺌
  --dots     의도한 1칸 점(눈 · 반짝) 목록 JSON — 8 항목에서 뺌
  --scale/--ppu/--ortho   1 항목(1080p 화소/칸) = 배율 ÷ PPU × 1080 ÷ (2 × ortho). PPU 를 안 주면 그림 옆 .meta 에서 읽음
  --ui       UI 칸 크기(초상) — 칸 ÷ 그림 화소 × --ui-scale(캔버스 배율, 기본 1) = 화면 배율이 정수인지
  --bg-mul   배경 도트 1칸의 화면 화소 (메인 메뉴 배경 = 1080p 에서 4 · 초상은 기본 4) — 화면 배율이 이것과 같아야 통과
  --design 에 여러 칸 시트를 주면 (가로 > 세로 · --design-cell 없음) 맨 왼쪽 정사각 칸을 설계도로 자름
             (--kind portrait 이고 시트가 32의 배수면 칸 = 32 — 세로로 긴 시트도 맨 위 왼쪽 32칸)
끝 코드: 0 = 모두 통과 · 1 = 하나라도 실패 · 2 = 사용 오류
필요: numpy · Pillow · scipy (py -3.14 에 있음)
"""
import argparse
import collections
import glob
import json
import os
import re
import sys

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
EIGHT = np.ones((3, 3), dtype=int)
DARK = 41                  # 외곽으로 치는 어두움 (Rec.601 Y < 41)
ORTHO = 22.0               # 게임 카메라 직교 크기 (normalZoom 20 × VisualRules.CameraMul) — 1080 ÷ 44 = 화면 24.5 화소/월드

# 종류별 문턱 (2-5 · 4-1). None = 그 종류엔 검사 안 함
KINDS = {
    "boss":     dict(colors=40, density=(3.9, 4.3), left=(-0.15, 0.30), body_y=(90, 130), margin=2, bbox=None),
    "sprite":   dict(colors=40, density=None, left=(-0.15, 0.30), body_y=(90, 130), margin=2, bbox=None),
    "portrait": dict(colors=32, density=None, left=(-0.30, 0.30), body_y=(90, 130), margin=2, bbox=None),
    "icon":     dict(colors=16, density=None, left=(-0.15, 0.30), body_y=None, margin=None, bbox=(22, 30)),
    "medal":    dict(colors=20, density=None, left=(-0.15, 0.30), body_y=None, margin=None, bbox=(22, 30)),
}
EDGE_DARK_MIN = 0.97       # 2 가장자리 Y < 41 몫
EDGE_ONE_COLOR = (0.75, 0.95)   # 3 가장자리 한 색 몫
THICK_MAX = 0.25           # 4 외곽 두께 (가장자리 바로 안쪽도 어두운 몫)
SINGLE_MAX = 1.0           # 8 1칸 섬 (불투명 칸의 %)
BIGGEST_MUL = 0.8          # 9 가장 큰 한 색 덩어리 몫 ≤ 설계도 × 0.8
UP = (0.15, 0.65)          # 10 위 광원
PILLOW_FLOOR = 0.40        # 12 베개 ≤ max(0.40, 설계도 + 0.05)
BODY_Y_TOL = 15            # 13 설계도가 있으면 몸 평균 Y = 설계도 ± 15

NAMES = {1: "1080p 화소/칸", 2: "가장자리 Y<41", 3: "가장자리 한 색 몫", 4: "외곽 두께", 5: "색 수", 6: "반투명 칸",
         7: "덩어리/100칸", 8: "1칸 섬(8-연결)", 9: "가장 큰 한 색 덩어리", 10: "위 광원", 11: "왼 광원", 12: "베개 지수",
         13: "몸 평균 Y", 14: "불투명 상자 · 여백"}


# ================================================================= 지표 (정의는 기획서 2-5 한 곳과 같음)
def luma(rgb):
    return 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]


def mask_of(cells, shape):
    m = np.zeros(shape, dtype=bool)
    for x, y in cells or []:
        if 0 <= y < shape[0] and 0 <= x < shape[1]:
            m[y, x] = True
    return m


def measure(im, glow=None, dots=None):
    """im = H×W×4 uint8. 「가장자리 칸」 = 불투명이고 4방향 중 빈칸(그림 밖 포함)이 있는 칸"""
    im = np.asarray(im)
    alpha = im[..., 3].astype(int)
    a = alpha > 127
    op = int(a.sum())
    if op < 10:
        return None
    rgb = im[..., :3].astype(np.int64)
    Y = luma(rgb.astype(float))
    H, W = a.shape
    P = np.pad(a, 1)
    edge = a & (~P[:-2, 1:-1] | ~P[2:, 1:-1] | ~P[1:-1, :-2] | ~P[1:-1, 2:])
    inner1 = a & ~edge & ndimage.binary_dilation(edge)
    glow_m = mask_of(glow, a.shape)
    dots_m = mask_of(dots, a.shape)
    e2 = edge & ~glow_m
    edge_dark = float((Y[e2] < DARK).mean()) if e2.any() else 1.0
    cols = collections.Counter(map(tuple, rgb[e2]))
    edge_one = cols.most_common(1)[0][1] / max(1, int(e2.sum())) if cols else 1.0
    thick = float((Y[inner1] < DARK).mean()) if inner1.any() else 0.0
    semi = int(((alpha > 0) & (alpha < 255)).sum())

    # 색상 묶음: 채도 < 0.15 무채 · 나머지 30° 칸 (그림 외곽 칸 = 가장자리이면서 어두운 칸 제외)
    dist = ndimage.distance_transform_edt(np.pad(a, 1))[1:-1, 1:-1]
    inner = a & ~(edge & (Y < DARK))
    r, g, b = rgb[..., 0] / 255.0, rgb[..., 1] / 255.0, rgb[..., 2] / 255.0
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-9), 0)
    d = np.maximum(mx - mn, 1e-9)
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) / 6.0
    grp = np.where(inner, np.where(s < 0.15, 12, (h * 12).astype(int) % 12), -1)
    yy, xx = np.mgrid[0:H, 0:W]
    up = (H - 1 - yy).astype(float)
    left = (W - 1 - xx).astype(float)
    pil = lu = ll = 0.0
    wsum = 0
    for gi in range(13):
        m = grp == gi
        n = int(m.sum())
        if n < 20 or Y[m].std() < 1e-6:
            continue

        def corr(v):
            v = v[m]
            return 0.0 if v.std() < 1e-6 else float(np.corrcoef(Y[m], v)[0, 1])
        pil += abs(corr(dist)) * n
        lu += corr(up) * n
        ll += corr(left) * n
        wsum += n

    # 덩어리: 같은 RGB 8-연결 영역
    key = np.where(a, (rgb[..., 0] << 16) | (rgb[..., 1] << 8) | rgb[..., 2], -1)
    sizes = []
    singles = 0
    for k in np.unique(key[a]):
        lab, nl = ndimage.label(key == k, structure=EIGHT)
        if not nl:
            continue
        cnt = np.bincount(lab.ravel())[1:]
        sizes += list(cnt)
        for li in np.nonzero(cnt == 1)[0]:
            yx = np.argwhere(lab == li + 1)[0]
            if not dots_m[yx[0], yx[1]]:
                singles += 1
    sizes = np.array(sizes)
    ys, xs = np.nonzero(a)
    body = inner & ~edge
    return dict(
        opaque=op, edge_dark=edge_dark, edge_one=edge_one, thick=thick, colors=int(len(np.unique(key[a]))), semi=semi,
        per100=len(sizes) / op * 100, per100_3=int((sizes >= 3).sum()) / op * 100, single=singles / op * 100, biggest=float(sizes.max()) / op,
        up=lu / wsum if wsum else 0.0, left=ll / wsum if wsum else 0.0, pillow=pil / wsum if wsum else 0.0,
        body_y=float(Y[body].mean()) if body.any() else float(Y[a].mean()),
        bbox=(int(xs.min()), int(ys.min()), int(xs.max()) + 1, int(ys.max()) + 1), size=(W, H))


# ================================================================= 판정
def judge(m, kind="boss", design=None, k=None, density=None, ui=None, ui_scale=1.0, bg_mul=None):
    """[(번호, 이름, 통과 True/False/None, 값 글, 기준 글)]"""
    K = KINDS[kind]
    hi_res = k is not None and k >= 2
    out = []

    def add(no, ok, val, rule):
        out.append((no, NAMES[no], ok, val, rule))
    # 1
    if density is not None and K["density"]:
        lo, hi = K["density"]
        add(1, lo <= density <= hi, f"{density:.2f}", f"{lo}~{hi}")
    elif ui is not None:
        # 화면 배율 = 칸 ÷ 그림 화소 × 캔버스 배율 — 정수여야 하고, 배경 도트 배율이 있으면 그것과 같아야 (밀도 섞임 방지)
        r = ui / m["size"][0] * ui_scale
        ok = abs(r - round(r)) < 1e-6 and (bg_mul is None or abs(r - bg_mul) < 1e-6)
        add(1, ok, f"{ui}÷{m['size'][0]}" + (f"×{ui_scale:g}" if ui_scale != 1.0 else "") + f" = 화면 ×{r:.3f}",
            "정수 배" + (f" · 배경 도트 ×{bg_mul:g} 와 같음" if bg_mul is not None else ""))
    else:
        add(1, None, "—", "배율 · PPU (또는 --ui) 를 주면 잼")
    add(2, m["edge_dark"] >= EDGE_DARK_MIN, f"{m['edge_dark'] * 100:.1f}%", f"≥ {EDGE_DARK_MIN * 100:.0f}% (발광 칸 제외)")
    lo, hi = EDGE_ONE_COLOR
    add(3, lo <= m["edge_one"] <= hi, f"{m['edge_one'] * 100:.1f}%", f"{lo * 100:.0f}~{hi * 100:.0f}%")
    add(4, m["thick"] <= THICK_MAX, f"{m['thick'] * 100:.1f}%", f"≤ {THICK_MAX * 100:.0f}%")
    add(5, m["colors"] <= K["colors"], f"{m['colors']}", f"≤ {K['colors']}")
    add(6, m["semi"] == 0, f"{m['semi']}", "0")
    if design is not None and k:
        # 하한은 3칸 이상 덩어리만 셈 (1~2칸 잔점으로 디테일 수를 채우는 것 방지) · 상한(잡음)은 모든 덩어리
        lo7, hi7 = design["per100_3"] / k, design["per100"]
        add(7, m["per100_3"] >= lo7 - 1e-9 and m["per100"] <= hi7 + 1e-9, f"3칸↑ {m['per100_3']:.1f} · 전체 {m['per100']:.1f}",
            f"3칸↑ ≥ {lo7:.1f} (설계도 3칸↑ ÷ k) · 전체 ≤ {hi7:.1f} (설계도)")
    else:
        add(7, None, f"{m['per100']:.1f}", "설계도가 있어야 잼")
    if hi_res:
        add(8, m["single"] <= SINGLE_MAX, f"{m['single']:.2f}%", f"≤ {SINGLE_MAX:.1f}% (의도한 점 제외)")
    else:
        add(8, None, f"{m['single']:.2f}%", "고해상도(k ≥ 2)만 — 32칸은 1칸 덩어리가 자연스러움")
    if design is not None:
        lim = design["biggest"] * BIGGEST_MUL if hi_res else design["biggest"]
        add(9, m["biggest"] <= lim + 1e-9, f"{m['biggest']:.3f}", f"≤ {lim:.3f} (설계도 {design['biggest']:.3f}" + (" × 0.8)" if hi_res else ")"))
    else:
        add(9, None, f"{m['biggest']:.3f}", "설계도가 있어야 잼")
    add(10, UP[0] <= m["up"] <= UP[1], f"{m['up']:+.2f}", f"{UP[0]:+.2f}~{UP[1]:+.2f}")
    lo, hi = K["left"]
    add(11, lo <= m["left"] <= hi, f"{m['left']:+.2f}", f"{lo:+.2f}~{hi:+.2f}")
    lim = max(PILLOW_FLOOR, design["pillow"] + 0.05) if design is not None else PILLOW_FLOOR
    add(12, m["pillow"] <= lim + 1e-9, f"{m['pillow']:.2f}", f"≤ {lim:.2f}")
    if K["body_y"] and design is not None and hi_res:
        # 설계도가 있으면 설계도 몸 밝기 ± 15 (어두운 옷 캐릭터를 밝게 만들라고 강요하지 않음 — 도적 시안 1회차 판정)
        lo, hi = design["body_y"] - BODY_Y_TOL, design["body_y"] + BODY_Y_TOL
        add(13, lo <= m["body_y"] <= hi, f"{m['body_y']:.1f}", f"{lo:.0f}~{hi:.0f} (설계도 {design['body_y']:.0f} ± {BODY_Y_TOL})")
    elif K["body_y"]:
        lo, hi = K["body_y"]
        add(13, lo <= m["body_y"] <= hi, f"{m['body_y']:.1f}", f"{lo}~{hi}")
    else:
        add(13, None, f"{m['body_y']:.1f}", "이 종류는 안 봄")
    x0, y0, x1, y1 = m["bbox"]
    W, H = m["size"]
    bw, bh = x1 - x0, y1 - y0
    if K["bbox"]:
        lo, hi = K["bbox"]
        add(14, lo <= bw <= hi and lo <= bh <= hi, f"{bw}×{bh}", f"{lo}~{hi}칸")
    else:
        parts, ok = [], True
        if design is not None and k:
            dx0, dy0, dx1, dy1 = design["bbox"]
            ew, eh = (dx1 - dx0) * k, (dy1 - dy0) * k
            ok &= abs(bw - ew) <= 1.5 and abs(bh - eh) <= 1.5          # ± 1칸 (+ k 가 정수가 아닐 때 반올림 0.5)
            parts.append(f"상자 {bw}×{bh} (설계도 × k = {ew:.0f}×{eh:.0f})")
        if hi_res and K["margin"]:
            mg = min(x0, y0, W - x1, H - y1)
            ok &= mg >= K["margin"]
            parts.append(f"여백 {mg}칸 (≥ {K['margin']})")
        add(14, ok if parts else None, " · ".join(parts) or f"{bw}×{bh}", "설계도 × k ± 1칸 · 가장자리 → 몸 ≥ 2칸 (고해상도)")
    return out


# ================================================================= 그림 읽기
def load(path, cell=None, frame=0):
    im = np.asarray(Image.open(path).convert("RGBA"))
    if cell is None:
        return im
    cw, ch = cell
    cols = max(1, im.shape[1] // cw)
    ty, tx = divmod(frame, cols)
    return im[ty * ch:(ty + 1) * ch, tx * cw:(tx + 1) * cw]


def frames_in(path, cell):
    im = Image.open(path)
    return (im.width // cell[0]) * (im.height // cell[1])


def parse_cell(s):
    if not s:
        return None
    m = re.match(r"^(\d+)[xX×](\d+)$", s)
    if not m:
        sys.exit(f"칸 크기 형식이 틀림: {s} (예: 162x162)")
    return int(m.group(1)), int(m.group(2))


def meta_ppu(path):
    meta = path + ".meta"
    if not os.path.isfile(meta):
        return None
    with open(meta, encoding="utf-8", errors="replace") as f:
        mm = re.search(r"spritePixelsToUnits:\s*([0-9.]+)", f.read())
    return float(mm.group(1)) if mm else None


def report(title, rows):
    lines = [f"## {title}"]
    for no, name, ok, val, rule in rows:
        mark = "통과" if ok is True else "실패" if ok is False else "  — "
        lines.append(f"[{mark}] {no:>2}. {name:<14} {val:<28} 기준 {rule}")
    nf = sum(1 for r in rows if r[2] is False)
    lines.append(f"→ 실패 {nf} · 통과 {sum(1 for r in rows if r[2] is True)} · 건너뜀 {sum(1 for r in rows if r[2] is None)}")
    return "\n".join(lines)


# ================================================================= 대조군 (gfx2/lint_calib.py · lint_calib2.py 이식)
def ctrl_a(im, n):
    return np.asarray(Image.fromarray(im).resize((n, n), Image.NEAREST))


def ctrl_b(im, n):
    pil = Image.fromarray(im)
    rgb = pil.convert("RGB").resize((n, n), Image.BILINEAR).quantize(40, method=Image.Quantize.MEDIANCUT).convert("RGB")
    alpha = np.asarray(pil.split()[3].resize((n, n), Image.BILINEAR)) > 127
    return np.dstack([np.asarray(rgb), np.where(alpha, 255, 0).astype(np.uint8)])


def ctrl_c(im, n):
    A = ctrl_a(im, n)
    a = A[..., 3] > 127
    dd = ndimage.distance_transform_edt(np.pad(a, 1))[1:-1, 1:-1]
    f = np.clip(0.55 + 0.45 * np.minimum(dd / (n * 0.12), 1.0), 0, 1)
    C = A.astype(float)
    C[..., :3] = C[..., :3] * f[..., None]
    C = np.clip(np.round(C / 16) * 16, 0, 255).astype(np.uint8)
    C[..., 3] = A[..., 3]
    return C


def thin_outline(X):
    """「가장자리만 다시 칠하기」 속임수: 가장자리 1칸만 어둡게 두고 그 안쪽 어두운 칸은 가장 가까운 밝은 안쪽 색으로"""
    X = X.copy()
    a = X[..., 3] > 127
    Y = luma(X[..., :3].astype(float))
    P = np.pad(a, 1)
    edge = a & (~P[:-2, 1:-1] | ~P[2:, 1:-1] | ~P[1:-1, :-2] | ~P[1:-1, 2:])
    dark_in = a & ~edge & (Y < DARK)
    src = a & ~edge & (Y >= DARK)
    if not src.any():
        return X
    _, (iy, ix) = ndimage.distance_transform_edt(~src, return_indices=True)
    X[dark_in, :3] = X[iy[dark_in], ix[dark_in], :3]
    return X


# 고해상도로 바꾼 보스는 32칸 원본이 설계도 파일에만 남음 (tools/pixelart/hires/bosses/blueprints/<보스>_32.png 첫 칸 = 살아 있는 그림)
CAL_SRC = [("리치 왕", "Assets/Sprites/enemy/boss.png", 3.75, "boss"), ("지옥의 군주", "Assets/Sprites/enemy/hell/hell_lord.png", 4.5, "boss"),
           ("킹 슬라임", "tools/pixelart/hires/bosses/blueprints/slimeking_32.png", 5.06, "boss"), ("개발자", "Assets/Resources/Abyss/developer_0.png", 3.8, "boss"),
           ("거너", "Assets/Resources/Characters/gunner_nogun.png", 4.0, "portrait")]


def hand_set():
    """style_metrics.py 와 같은 손 그림 묶음 (영웅 9 · 잡몹 · 심연 잡몹 5 · 보스 5 — 첫 칸 32칸)"""
    A = os.path.join(ROOT, "Assets")
    items = []
    for n in ["gunner_nogun", "swordsman", "rogue", "archer", "alchemist", "cartographer", "clockmaker", "postman", "ghost"]:
        items.append(("영웅", n, os.path.join(A, "Resources/Characters/%s.png" % n), "sprite"))
    for f in sorted(glob.glob(os.path.join(A, "Sprites/enemy/**/*.png"), recursive=True)):
        b = os.path.basename(f)[:-4]
        if b not in ("boss", "hell_lord", "meadow_slimeking"):
            items.append(("잡몹", b, f, "sprite"))
    for f in [os.path.join(A, "Sprites/Players/eneymy2.png"), os.path.join(A, "Sprites/Players/players blue x1 1.png")]:
        if os.path.exists(f):
            items.append(("잡몹", os.path.basename(f)[:-4], f, "sprite"))
    for n in ["wisp_0", "chainwraith_0", "voideye_0", "reaper_0", "colossus_0"]:
        items.append(("심연잡몹", n, os.path.join(A, "Resources/Abyss/%s.png" % n), "sprite"))
    for n, p in [("boss", "Sprites/enemy/boss.png"), ("hell_lord", "Sprites/enemy/hell/hell_lord.png"), ("meadow_slimeking", "../tools/pixelart/hires/bosses/blueprints/slimeking_32.png"),
                 ("mirrorlord_0", "Resources/Abyss/mirrorlord_0.png"), ("developer_0", "Resources/Abyss/developer_0.png")]:
        items.append(("보스", n, os.path.join(A, p), "boss"))
    return [(g, n, p, k) for g, n, p, k in items if os.path.exists(p)]


# 손 그림이 지켜야 하는 「그림체」 항목 (해상도와 무관 · 2-5 표에서 지금 손 그림이 넘는 값으로 정한 것)
STYLE_ITEMS = (3, 4, 5, 6, 12)
# 새 그림에만 더 엄하게 건 항목 (2-5 표의 「지금 32칸 손 그림」 칸이 이미 기준 밖 — 지금 그림이 못 넘는 것이 의도)
STRICT_ITEMS = (2, 10, 11, 13)
# 대조군 탈락을 셀 때 빼는 항목: 1 (런타임) · 14 (캔버스 여백 — 옛 그림이 칸 끝에 닿아 있어 키우면 늘 떨어짐 · 그림 품질과 무관)
CAL_SKIP = (1, 14)


def calibrate():
    ok_all = True
    print("# lint.py --calibrate — 대조군 보정 시험 (기획서 2-5)\n")
    # 1) 손 그림 — 보정 원본 5장 (반드시 그림체 항목 전부 통과) + 넓은 묶음 32장 (참고 통과율)
    print("## 1) 지금 손 그림 (32칸 첫 칸 · 설계도 = 자기 자신 · k = 1)")
    src_res = {}
    print("  ### 보정 원본 5장 — 그림체 항목(" + ", ".join(map(str, STYLE_ITEMS)) + ") 을 모두 넘어야 함")
    for name, rel, k, kind in CAL_SRC:
        p = os.path.join(ROOT, rel)
        if not os.path.exists(p):
            continue
        m = measure(load(p, (32, 32), 0))
        # 손 그림 자체는 게임 속 32칸 종류로 (초상 원본 = 영웅 시트 → sprite) · 대조군의 「원본 물려받음」은 목표 종류(kind)로
        hand = {no: ok for no, _, ok, _, _ in judge(m, "sprite" if kind == "portrait" else kind, design=m, k=1)}
        src_res[name] = (m, {no: ok for no, _, ok, _, _ in judge(m, kind, design=m, k=1)})
        bad = [no for no in STYLE_ITEMS if hand[no] is False]
        strict = [f"{no} {fmt_val(no, m)}" for no in STRICT_ITEMS if hand[no] is False]
        strict += [f"{no} {fmt_val(no, m)} ({kind} 문턱)" for no, ok in src_res[name][1].items() if ok is False and hand.get(no) is not False and no not in CAL_SKIP]
        if bad:
            ok_all = False
        print(f"  {name:<6} " + ("통과" if not bad else "실패 " + ", ".join(f"{no} {NAMES[no]} {fmt_val(no, m)}" for no in bad))
              + "  (" + " · ".join(f"{no}={fmt_val(no, m)}" for no in STYLE_ITEMS) + ")" + ("  · 새 그림 전용 항목 밖: " + ", ".join(strict) if strict else ""))
    rows = []
    for g, n, p, kind in hand_set():
        m = measure(load(p, (32, 32), 0))
        if m is None:
            continue
        res = {no: ok for no, _, ok, _, _ in judge(m, kind, design=m, k=1)}
        rows.append((g, n, m, res))
    print(f"\n  ### 넓은 묶음 {len(rows)}장 (영웅 · 잡몹 · 심연 잡몹 · 보스) — 참고 통과율")
    for no in STYLE_ITEMS + STRICT_ITEMS:
        vals = [r[3][no] for r in rows if r[3][no] is not None]
        fails = [f"{r[1]}({fmt_val(no, r[2])})" for r in rows if r[3][no] is False]
        tag = "그림체" if no in STYLE_ITEMS else "새 그림만 엄하게"
        print(f"  {no:>2}. {NAMES[no]:<14} {sum(1 for v in vals if v)}/{len(vals)} [{tag}]" + ("  · 못 넘음: " + ", ".join(fails[:10]) + (" …" if len(fails) > 10 else "") if fails else ""))
    style_pass = sum(1 for r in rows if all(r[3][no] is not False for no in STYLE_ITEMS))
    print(f"  → 그림체 항목 모두 통과: {style_pass}/{len(rows)}장")

    # 2) 대조군
    print("\n## 2) 대조군 — 원본이 넘는 항목 중 두 개 이상에서 떨어져야 함 (「고유 탈락」 · 1 · 14 제외)")
    print("     A 최근접 ×k · B 쌍선형 + 색 40 · C 최근접 + 베개 명암 · (참고) D = A + 외곽만 1칸으로 다시 칠함 · E = B + 외곽만 다시 칠함")
    summary = collections.defaultdict(list)
    for name, rel, k, kind in CAL_SRC:
        p = os.path.join(ROOT, rel)
        if not os.path.exists(p) or name not in src_res:
            continue
        im = load(p, (32, 32), 0)
        dm, dres = src_res[name]
        nn = int(round(32 * k))
        ctrls = {"A": ctrl_a(im, nn), "B": ctrl_b(im, nn), "C": ctrl_c(im, nn)}
        ctrls["D"] = thin_outline(ctrls["A"])
        ctrls["E"] = thin_outline(ctrls["B"])
        for tag, X in ctrls.items():
            m = measure(X)
            res = judge(m, kind, design=dm, k=nn / 32.0)
            failed = [no for no, _, ok, _, _ in res if ok is False]
            own = [no for no in failed if no not in CAL_SKIP and dres.get(no) is not False]
            inherited = [no for no in failed if no not in own]
            summary[tag].append(len(own))
            print(f"  {name:<6} {tag} ×{nn / 32:.2f}: 고유 탈락 {len(own)} — " + (", ".join(f"{no} {NAMES[no]} {fmt_val(no, m)}" for no in own) or "없음 (!)")
                  + (f"  (원본 물려받음 · 여백: {', '.join(map(str, inherited))})" if inherited else ""))
    print()
    for tag in "ABCDE":
        if not summary[tag]:
            continue
        need = 2 if tag in "ABC" else 1
        good = min(summary[tag]) >= need
        if tag in "ABC" and not good:
            ok_all = False
        print(f"  대조 {tag}: 가장 적게 떨어진 그림도 고유 {min(summary[tag])}항목 → " + ("떨어짐 (보정 통과)" if good else "보정 실패 — 그 항목 문턱을 고칠 것")
              + ("" if tag in "ABC" else " [참고 · 1항목 이상이면 됨]"))
    print("\n결과: " + ("보정 통과 — 원본 손 그림 5장 그림체 항목 전부 통과 · 대조 A · B · C 모두 고유 2항목 이상 탈락" if ok_all else "보정 실패 — 위 표 확인"))
    return 0 if ok_all else 1


def fmt_val(no, m):
    v = {2: f"{m['edge_dark'] * 100:.0f}%", 3: f"{m['edge_one'] * 100:.0f}%", 4: f"{m['thick'] * 100:.0f}%", 5: f"{m['colors']}색", 6: f"{m['semi']}",
         7: f"{m['per100_3']:.1f}", 8: f"{m['single']:.1f}%", 9: f"{m['biggest']:.2f}", 10: f"{m['up']:+.2f}", 11: f"{m['left']:+.2f}",
         12: f"{m['pillow']:.2f}", 13: f"{m['body_y']:.0f}", 14: "상자"}
    return v.get(no, "")


# ================================================================= 한 장 검사
def main():
    ap = argparse.ArgumentParser(description="그림 품질 검사기 (2-5)", formatter_class=argparse.RawDescriptionHelpFormatter, epilog=__doc__)
    ap.add_argument("image", nargs="?")
    ap.add_argument("--calibrate", action="store_true")
    ap.add_argument("--design")
    ap.add_argument("--k", type=float)
    ap.add_argument("--kind", default="boss", choices=sorted(KINDS))
    ap.add_argument("--cell")
    ap.add_argument("--frame", type=int, default=0)
    ap.add_argument("--frames")
    ap.add_argument("--design-cell")
    ap.add_argument("--glow")
    ap.add_argument("--dots")
    ap.add_argument("--scale", type=float)
    ap.add_argument("--ppu", type=float)
    ap.add_argument("--ortho", type=float, default=ORTHO)
    ap.add_argument("--ui", type=float)
    ap.add_argument("--ui-scale", type=float, default=1.0)
    ap.add_argument("--bg-mul", type=float)
    ap.add_argument("--out")
    ap.add_argument("--json", action="store_true")
    a = ap.parse_args()
    if a.calibrate:
        return calibrate()
    if not a.image:
        ap.print_help()
        return 2
    cell = parse_cell(a.cell)
    dcell = parse_cell(a.design_cell) or ((32, 32) if a.design and cell else None)
    glow = json.load(open(a.glow, encoding="utf-8")) if a.glow else None
    dots = json.load(open(a.dots, encoding="utf-8")) if a.dots else None
    frames = list(range(frames_in(a.image, cell))) if (a.frames == "all" and cell) else [a.frame]
    ppu = a.ppu or meta_ppu(a.image)
    density = a.scale / ppu * 1080.0 / (2.0 * a.ortho) if (a.scale and ppu) else None
    texts, dump, nfail = [], [], 0
    for fi in frames:
        im = load(a.image, cell, fi)
        m = measure(im, glow, dots)
        if m is None:
            texts.append(f"## 칸 {fi}: 불투명 칸이 거의 없음 (건너뜀)")
            continue
        dm = None
        k = a.k
        if a.design:
            dim = load(a.design, dcell, fi if dcell else 0)
            if dcell is None and a.kind == "portrait" and dim.shape[0] != dim.shape[1] \
                    and dim.shape[0] % 32 == 0 and dim.shape[1] % 32 == 0:
                dim = dim[:32, :32]                  # 초상: 영웅 시트의 칸 = 32칸 → 맨 위 왼쪽 칸 (세로로 긴 거너 시트 256×416 에서 k 0.50 으로 빗나갔음)
            elif dcell is None and dim.shape[1] > dim.shape[0]:
                dim = dim[:, :dim.shape[0]]          # 시트를 주면 맨 왼쪽 정사각 칸 (k 를 시트 폭으로 잘못 잡던 것)
            dm = measure(dim)
            if k is None:
                k = im.shape[1] / dim.shape[1]
        bg_mul = a.bg_mul if a.bg_mul is not None else (4.0 if (a.kind == "portrait" and a.ui) else None)
        rows = judge(m, a.kind, dm, k, density, a.ui, a.ui_scale, bg_mul)
        nfail += sum(1 for r in rows if r[2] is False)
        title = f"{os.path.basename(a.image)}" + (f" 칸 {fi}" if cell else "") + f" ({a.kind}" + (f" · k {k:.2f}" if k else "") + ")"
        texts.append(report(title, rows))
        dump.append(dict(frame=fi, metrics={kk: (list(v) if isinstance(v, tuple) else v) for kk, v in m.items()},
                         items=[dict(no=no, name=nm, ok=ok, value=val, rule=rule) for no, nm, ok, val, rule in rows]))
    text = "\n\n".join(texts) + f"\n\n전체: {'통과' if nfail == 0 else f'실패 {nfail}항목'}"
    if a.json:
        print(json.dumps(dump, ensure_ascii=False, indent=1))
    else:
        print(text)
    if a.out:
        with open(a.out, "w", encoding="utf-8") as f:
            f.write(text + "\n")
    return 0 if nfail == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
