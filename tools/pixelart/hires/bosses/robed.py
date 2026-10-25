"""옷 입은 장 보스 (리치 왕 · 지옥의 군주) 공용 — 손 도트 지도 한 장을 그대로 칸에 찍고 외곽 · 섬 정리만 한다.

킹 슬라임 3회 판정에서 찾은 규칙 (그래픽 개편 2단계 묶음 4):
  · 몸 명암 경계는 스크립트 수열이 아니라 손 도트 지도 격자로 직접 (이 모듈은 명암을 계산하지 않음)
  · 같은 모양 도장 · 같은 줄 복사 · 고르게 뿌린 점 금지 → 알갱이 프레임도 칸마다 손 지도
  · 죽음 프레임도 칸마다 손으로 (회전 복사 금지) · 작은 알갱이는 몸 계열 짙은 색 외곽
손 도트 지도 글자 = 재질:단계 (legend). 재질 이름 → 팔레트 단계 색은 보스 모듈이 정함.
특수 재질: ol = 기본 외곽색 · olw = 따뜻한 외곽색 · olc = 차가운 외곽색 (안쪽 선 · 겹침 선을 손으로 찍을 때)
"""
import numpy as np

from .. import cleanup, detail, outline


def load(name):
    return detail.load_handmaps(name)


def paint(cell, placements, legend, grids, base=None, center=False):
    """placements = [(지도 이름, x, y[, 단계 밀기])] 차례로 덮어 찍음 → (재질 이름 판, 단계 판).
    center: x, y 를 지도 가운데 자리로 (알갱이 자리를 설계도 칸 가운데 × k 로 적을 때). 단계 밀기는 1 아래로 내려가지 않음"""
    mat = np.full((cell, cell), "", dtype=object) if base is None else base[0].copy()
    tone = np.zeros((cell, cell), int) if base is None else base[1].copy()
    for item in placements:
        name, x, y = item[:3]
        shift = item[3] if len(item) > 3 else 0
        g = detail.grid_array(grids[name])
        if center:
            x, y = int(round(x - g.shape[1] / 2)), int(round(y - g.shape[0] / 2))
        for (gy, gx), ch in np.ndenumerate(g):
            if ch in ". ":
                continue
            if ch not in legend:
                raise SystemExit(f"지도 {name} ({gx},{gy}) 글자 '{ch}' 가 legend 에 없음")
            yy, xx = y + gy, x + gx
            if 0 <= yy < cell and 0 <= xx < cell:
                m, t = legend[ch]
                mat[yy, xx], tone[yy, xx] = m, (max(1, t + shift) if shift else t)
    return mat, tone


def motes_rgba(mat, tone, pal, edge_rgb, side_rgb, top_frac=0.4, x_frac=0.5):
    """작은 알갱이 칸: 가장자리 = 몸 계열 짙은 색 (기본 외곽 #1A112C 대신), 오른쪽(가로 반 · 세로 위 top_frac 아래) 알갱이의 오른쪽 · 아래 가운데 칸만 그늘 색 (ㄴ자).
    섬 정리 안 함 (알갱이 속은 손 지도에서 2칸 이상으로 찍음)"""
    H, W = mat.shape
    op = mat != ""
    rgb = np.zeros((H, W, 3), np.uint8)
    for m in set(mat[op].tolist()):
        sel = mat == m
        ramp = np.array(pal[m], np.uint8)
        rgb[sel] = ramp[np.clip(tone[sel], 0, len(ramp) - 1)]
    edge = outline.edge_cells(op)
    dx, dy = outline.outward(op)
    rgb[edge] = edge_rgb
    ys_, xs_ = np.nonzero(op)
    quad = (np.arange(W)[None, :] >= xs_.min() + x_frac * (xs_.max() - xs_.min())) & (np.arange(H)[:, None] >= ys_.min() + top_frac * (ys_.max() - ys_.min()))
    rgb[outline.no_lonely(edge & quad & (((dx > 0) & (dy == 0)) | ((dy > 0) & (dx == 0))))] = side_rgb
    cleanup.kill_singles(rgb, op, protect=~edge)          # 그늘 칸에 끊겨 외톨이가 된 외곽 칸 → 이웃 외곽색
    return cleanup.to_rgba(rgb, op)


def to_rgba(mat, tone, pal, warm=(), cool_rgb=None, cool=True, dark_edge=None, glow=(), inner_rgb=None, side_rgb=None, side_quad=False, warm_any=()):
    """재질 · 단계 → RGBA. 바깥 1칸 = 외곽색 (따뜻한 재질 밝은 칸 옆 · 위/옆을 보는 곳 = 따뜻한 외곽,
    오른쪽 아래 = 차가운 외곽). dark_edge = 재질 → 그 재질만 쓰는 외곽색 (작은 알갱이: 몸 계열 짙은 색).
    glow 재질은 가장자리여도 외곽을 씌우지 않음 (발광 — lint --glow 로 뺌).
    inner_rgb: 손으로 찍은 안쪽 선 색 (영혼 빠짐처럼 몸 전체가 옅을 때 — 가장자리 외곽은 그대로)"""
    H, W = mat.shape
    op = mat != ""
    rgb = np.zeros((H, W, 3), np.uint8)
    for m in set(mat[op].tolist()):
        sel = mat == m
        if m == "ol":
            rgb[sel] = outline.OL
        elif m == "olw":
            rgb[sel] = outline.OL_WARM
        elif m == "olc":
            rgb[sel] = cool_rgb or outline.OL
        else:
            ramp = np.array(pal[m], np.uint8)
            rgb[sel] = ramp[np.clip(tone[sel], 0, len(ramp) - 1)]
    edge = outline.edge_cells(op)
    glow_m = np.isin(mat, list(glow)) if glow else np.zeros_like(op)
    edge_ol = edge & ~glow_m
    typed_ol = np.isin(mat, ["ol", "olw", "olc"])
    # 손으로 찍은 안쪽 선은 보호 · 가장자리에 찍은 기본 외곽(*)은 아래 규칙으로 다시 칠함 · 가장자리에 찍은 따뜻한(%) · 차가운(&) 외곽은 그대로 (외곽 끊기)
    hand_ol = (typed_ol & ~edge) | np.isin(mat, ["olw", "olc"])
    inner_mat = np.full((H, W), "", dtype=object)
    inner_tone = np.zeros((H, W), int)
    inside = op & ~edge & ~typed_ol
    for y, x in np.argwhere(edge_ol):
        found = False
        for r in (1, 2):                  # 바로 안쪽이 손 외곽이면 한 칸 더 안쪽 재질
            for dy, dx in ((r, 0), (-r, 0), (0, r), (0, -r), (r, r), (r, -r), (-r, r), (-r, -r)):
                yy, xx = y + dy, x + dx
                if 0 <= yy < H and 0 <= xx < W and inside[yy, xx]:
                    inner_mat[y, x], inner_tone[y, x] = mat[yy, xx], tone[yy, xx]
                    found = True
                    break
            if found:
                break
    if inner_rgb is not None:
        rgb[hand_ol] = inner_rgb
    dx, dy = outline.outward(op)
    base = edge_ol & ~hand_ol
    rgb[base] = outline.OL
    if dark_edge:
        for m, c in dark_edge.items():
            rgb[base & (mat == m)] = c
    w = base & np.isin(inner_mat, list(warm)) & (inner_tone >= 3) & (dy < 0) if warm else np.zeros_like(op)
    if warm_any:                          # 이 재질(불) 아래를 보는 가장자리도 따뜻한 외곽 (밑단 — 긴 외곽 한 줄을 끊음)
        w = w | (base & np.isin(inner_mat, list(warm_any)) & (dy > 0))
    w = outline.no_lonely(w)
    rgb[w] = outline.OL_WARM
    if cool and cool_rgb is not None:
        # 그림자 쪽: 오른쪽 아래 4분면의 오른쪽을 보는 가장자리
        ys_, xs_ = np.nonzero(op)
        cx, cy = (xs_.min() + xs_.max()) / 2, (ys_.min() + ys_.max()) / 2
        quad = (np.arange(W)[None, :] >= cx) & (np.arange(H)[:, None] >= cy)
        c = outline.no_lonely(base & ~w & (dx > 0) & (dy >= 0) & quad)
        rgb[c] = cool_rgb
    if side_rgb is not None:              # 오른쪽 · 아래를 보는 가운데 칸 = 그늘 색 (ㄴ자 · 외톨이 칸은 뺌)
        sq = np.ones_like(op)
        if side_quad:                     # 오른쪽 아래 4분면만
            ys_, xs_ = np.nonzero(op)
            sq = (np.arange(W)[None, :] >= (xs_.min() + xs_.max()) / 2) & (np.arange(H)[:, None] >= (ys_.min() + ys_.max()) / 2)
        rgb[outline.no_lonely(base & ~w & sq & (((dx > 0) & (dy == 0)) | ((dy > 0) & (dx == 0))))] = side_rgb
        cleanup.kill_singles(rgb, op, protect=~edge | hand_ol)     # 그늘 칸에 끊겨 외톨이가 된 외곽 칸 → 이웃 외곽색
    cleanup.kill_singles(rgb, op, protect=edge | hand_ol)
    return cleanup.to_rgba(rgb, op)


def recolor(mat, mapping):
    """재질 이름 바꾸기 (영혼 빠짐 = 같은 손 지도 · 다른 색 벌)"""
    out = mat.copy()
    for a, b in mapping.items():
        out[mat == a] = b
    return out


def dump(mat, letters, box=None):
    """견본 (윤곽 잡기용): 재질 → 글자, 빈칸 '.'"""
    H, W = mat.shape
    x0, y0, x1, y1 = box or (0, 0, W, H)
    rows = []
    for y in range(y0, y1):
        rows.append("".join(letters.get(mat[y, x], "?") if mat[y, x] else "." for x in range(x0, x1)))
    return rows
