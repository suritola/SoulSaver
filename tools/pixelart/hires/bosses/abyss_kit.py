"""심연 · 무한 보스 (개발자 · 거울의 군주 · 개발자 엄마) 공용 그리기 도구 — 그래픽 개편 2단계 묶음 5

킹 슬라임 · 리치 판정에서 걸린 것을 처음부터 피하는 틀:
  · 큰 몸(후드 · 옷 · 파마 · 가디건)의 명암 경계 = 손 구간(handmaps/runs/<보스>_*.txt — 행마다 「단계:끝열」)을 그대로 칠함.
    스크립트 수열 · 직선 그라데이션 · 등차 계단을 만들지 않음 (명암을 계산하지 않음)
  · 얼굴 · 손 · 장식 = 손 도트 지도 격자(handmaps/<보스>.txt 의 @grid) 를 그대로 찍음
  · 단단한 물건의 윤곽(후광 고리 · 책상 · 거울 테)만 손으로 놓은 기준점을 지나는 곡선으로 — 실루엣에 긴 곧은 변을 만들지 않음
  · 외곽 1칸 (#1A112C · 따뜻한 재질 옆 #2D192A · 오른쪽 아래 그림자 쪽 보조색) → 1칸 섬 정리
대상별 그림은 bosses/dev.py · mirror.py · mom.py. 이 파일은 hires 공용 모듈을 고치지 않고 불러 쓰기만 한다.
"""
import os
import re

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

from .. import blueprint as bp
from .. import cleanup, detail, outline

HERE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RUNS = os.path.join(HERE, "handmaps", "runs")
H = bp.hexrgb


# ---------------------------------------------------------------- 그림판 (재질 이름 · 단계)
class Pic:
    def __init__(self, cell):
        self.C = cell
        self.mat = np.full((cell, cell), "", dtype=object)
        self.tone = np.zeros((cell, cell), int)

    def put(self, mask, mat, tone=2):
        self.mat[mask] = mat
        self.tone[mask] = tone

    def has(self, *mats):
        return np.isin(self.mat, list(mats))

    def grid(self, rows, legend, x, y, flip=False, clip=None, shift=0):
        """손 도트 지도 찍기 ('.' · ' ' 는 그대로) → 찍은 칸 지도"""
        g = detail.grid_array(rows, 0, flip)
        hit = np.zeros((self.C, self.C), bool)
        for (gy, gx), ch in np.ndenumerate(g):
            if ch in ". ":
                continue
            if ch not in legend:
                raise SystemExit(f"글자 '{ch}' 가 legend 에 없음 (격자 ({gx},{gy}))")
            yy, xx = y + gy, x + gx
            if 0 <= yy < self.C and 0 <= xx < self.C and (clip is None or clip[yy, xx]):
                m, t = legend[ch]
                self.mat[yy, xx] = m
                self.tone[yy, xx] = max(0, t + shift) if shift and m not in ("ol", "olw", "olc") else t
                hit[yy, xx] = True
        return hit

    def runs(self, lines, mats, dx=0, dy=0):
        """손 구간: 'y | 단계:끝열 ... 단계' — 그 행에서 mats 재질 칸만, 왼쪽부터 끝열(포함)까지 그 단계, 마지막 단계는 행 끝까지.
        글자 단계 대신 '재질/단계' 도 됨 (예: 'hair/1:40'). 열 · 행은 칸 좌표 (dx · dy 만큼 옮겨 찍음)"""
        sel = self.has(*mats)
        for raw in lines:
            line = raw.split("#")[0].strip()
            if not line:
                continue
            h, _, body = line.partition("|")
            y = int(h) + dy
            if not 0 <= y < self.C:
                continue
            xs = np.nonzero(sel[y])[0]
            if not len(xs):
                continue
            x = int(xs.min())
            for tok in body.split():
                t, _, e = tok.partition(":")
                end = int(e) + dx if e else self.C - 1
                m = None
                if "/" in t:
                    m, t = t.split("/")
                cols = [c for c in range(x, end + 1) if 0 <= c < self.C and sel[y, c]]
                for c in cols:
                    self.tone[y, c] = int(t)
                    if m:
                        self.mat[y, c] = m
                x = max(x, end + 1)


def load_runs(name):
    """handmaps/runs/<name>.txt → {구간 이름: [줄]} ('@runs 이름' 으로 나눔)"""
    out, cur = {}, None
    with open(os.path.join(RUNS, name + ".txt"), encoding="utf-8") as f:
        for raw in f:
            line = raw.rstrip("\n")
            if line.startswith("@runs"):
                cur = line.split(None, 1)[1].strip()
                out[cur] = []
            elif cur and line.strip() and not line.lstrip().startswith("#"):
                out[cur].append(line)
    return out


# ---------------------------------------------------------------- 곡선 윤곽 (손으로 놓은 기준점 → 캣멀롬 곡선 → 채우기)
def spline(pts, n=10, closed=True):
    P = [np.array(p, float) for p in pts]
    if closed:
        P = [P[-1]] + P + [P[0], P[1]]
    else:
        P = [P[0]] + P + [P[-1]]
    out = []
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = P[i - 1], P[i], P[i + 1], P[i + 2]
        for t in np.linspace(0, 1, n, endpoint=False):
            t2, t3 = t * t, t * t * t
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    if not closed:
        out.append(P[-2])
    return out


def shape(C, pts, n=10):
    im = Image.new("1", (C, C), 0)
    ImageDraw.Draw(im).polygon([(float(x), float(y)) for x, y in spline(pts, n)], fill=1, outline=1)
    return np.array(im, bool)


def poly(C, pts):
    im = Image.new("1", (C, C), 0)
    ImageDraw.Draw(im).polygon([(float(x), float(y)) for x, y in pts], fill=1, outline=1)
    return np.array(im, bool)


def ellipse(C, cx, cy, rx, ry):
    ys, xs = np.mgrid[0:C, 0:C]
    return ((xs + 0.5 - cx) / rx) ** 2 + ((ys + 0.5 - cy) / ry) ** 2 <= 1.0


def shifted(mask, dx, dy):
    out = np.zeros_like(mask)
    H_, W_ = mask.shape
    ys, xs = np.nonzero(mask)
    ys, xs = ys + dy, xs + dx
    ok = (ys >= 0) & (ys < H_) & (xs >= 0) & (xs < W_)
    out[ys[ok], xs[ok]] = True
    return out


def extents(mask, y0=None, y1=None):
    """행마다 (첫 열, 끝 열) — 손 구간을 적을 때 보는 표"""
    rows = {}
    for y in range(mask.shape[0]):
        if y0 is not None and not y0 <= y <= y1:
            continue
        xs = np.nonzero(mask[y])[0]
        if len(xs):
            rows[y] = (int(xs.min()), int(xs.max()))
    return rows


# ---------------------------------------------------------------- 마무리 (재질 · 단계 → RGBA · 외곽 · 섬 정리)
def finish(pic, pal, warm=(), cool_rgb=None, glow=(), no_ol=(), keep_dots=None, cool_down=False):
    """warm: 밝은 단계(≥3) 옆 위/옆을 보는 가장자리 = 따뜻한 외곽. cool_rgb: 오른쪽 아래 4분면의 오른쪽 · 아래를 보는 가장자리.
    glow: 가장자리여도 외곽을 씌우지 않는 발광 재질 (lint --glow 목록으로 돌려줌). no_ol: 외곽 대신 그 재질의 가장 어두운 단계.
    손으로 찍은 ol · olw · olc 칸은 그대로 (안쪽 선)"""
    mat, tone = pic.mat, pic.tone
    C = pic.C
    op = mat != ""
    rgb = np.zeros((C, C, 3), np.uint8)
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
    typed = np.isin(mat, ["ol", "olw", "olc"])
    glow_m = np.isin(mat, list(glow)) if glow else np.zeros_like(op)
    base = edge & ~glow_m & ~typed
    inner_mat = outline.inner_neighbor(mat, op, edge)
    inner_tone = outline.inner_neighbor(tone, op, edge)
    dx, dy = outline.outward(op)
    rgb[base] = outline.OL
    for m in no_ol:
        s = base & (mat == m)
        rgb[s] = pal[m][0]
    if warm:
        w = outline.no_lonely(base & np.isin(inner_mat, list(warm)) & (inner_tone >= 3) & (dy <= 0) & (dx <= 0))
        rgb[w] = outline.OL_WARM
    else:
        w = np.zeros_like(op)
    if cool_rgb is not None:
        ys_, xs_ = np.nonzero(op)
        cx, cy = (xs_.min() + xs_.max()) / 2, (ys_.min() + ys_.max()) / 2
        quad = (np.arange(C)[None, :] >= cx) & (np.arange(C)[:, None] >= cy)
        c = base & ~w & quad & (((dx > 0) & (dy >= 0)) | ((dy > 0) & (dx > 0)))
        if cool_down:                       # 아래 절반의 아래를 보는 가장자리도 (외곽 한 색 몫 75~95%)
            c |= base & ~w & (np.arange(C)[:, None] >= cy) & (dy > 0)
        c = outline.no_lonely(c)
        rgb[c] = cool_rgb
    keep = keep_dots if keep_dots is not None else np.zeros_like(op)
    cleanup.kill_singles(rgb, op, keep=keep, protect=edge | typed)
    rgba = cleanup.to_rgba(rgb, op)
    gl = [[int(x), int(y)] for y, x in np.argwhere(glow_m & edge)]
    return rgba, gl


def place(img, C):
    """그림판 그대로 (칸 = 그림판)"""
    return img


def islands4(rgba):
    """판정관 isl.py 와 같은 정의: Y ≥ 41 이고 4-이웃에 같은 색이 없는 칸 수"""
    a = rgba[..., 3] > 0
    rgb = rgba[..., :3].astype(int)
    Y = 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]
    key = np.where(a, (rgb[..., 0] << 16) | (rgb[..., 1] << 8) | rgb[..., 2], -1)
    P = np.pad(key, 1, constant_values=-2)
    same = np.zeros(a.shape, bool)
    for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
        same |= P[1 + dy:1 + dy + a.shape[0], 1 + dx:1 + dx + a.shape[1]] == key
    return int((a & (Y >= 41) & ~same).sum()), int(a.sum())


def fix_islands4(rgba, protect=None, passes=2):
    """4-이웃 외톨이(대각선으로만 이어진 칸)를 4-이웃 중 가장 많은 색으로 — 판정관 isl.py 기준 1칸 섬 줄이기.
    외곽(어두운 칸 Y < 41)과 protect 칸은 바꾸지 않음"""
    a = rgba[..., 3] > 0
    out = rgba.copy()
    H_, W_ = a.shape
    for _ in range(passes):
        rgb = out[..., :3].astype(int)
        Y = 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]
        key = np.where(a, (rgb[..., 0] << 16) | (rgb[..., 1] << 8) | rgb[..., 2], -1)
        changed = 0
        for y, x in np.argwhere(a & (Y >= 41)):
            if protect is not None and protect[y, x]:
                continue
            nb = []
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                yy, xx = y + dy, x + dx
                if 0 <= yy < H_ and 0 <= xx < W_ and a[yy, xx]:
                    nb.append(int(key[yy, xx]))
            if key[y, x] in nb or not nb:
                continue
            cand = [k for k in nb if 0.299 * (k >> 16) + 0.587 * ((k >> 8) & 255) + 0.114 * (k & 255) >= 41]
            if not cand:
                continue
            v = max(set(cand), key=cand.count)
            out[y, x, :3] = ((v >> 16) & 255, (v >> 8) & 255, v & 255)
            key[y, x] = v
            changed += 1
        if not changed:
            break
    return out


# ---------------------------------------------------------------- 낱장 .meta (심연식 Resources/Abyss/<이름>_0~3)
def rewrite_single_meta(text, ppu, extrude):
    text = re.sub(r"(?m)^  spritePixelsToUnits: [\d.]+$", f"  spritePixelsToUnits: {ppu}", text)
    text = re.sub(r"(?m)^  spriteExtrude: \d+$", f"  spriteExtrude: {extrude}", text)
    for key, val in (("filterMode", 0), ("enableMipMap", 0), ("spriteMeshType", 1), ("alignment", 0)):
        text = re.sub(rf"(?m)^(\s+){key}: \d+$", lambda m: f"{m.group(1)}{key}: {val}", text)
    text = re.sub(r"(?m)^(\s+)textureCompression: \d+$", lambda m: f"{m.group(1)}textureCompression: 0", text)
    return text


def curve_cols(keys, ys):
    """손으로 놓은 기준점 {행: 열} 을 지나는 캣멀롬 곡선 → 행마다 열 (펜으로 그은 곡선 — 일정한 계단을 만들지 않음)"""
    ks = sorted(keys.items())
    out = {}
    for y in ys:
        if y <= ks[0][0]:
            out[y] = int(round(ks[0][1])); continue
        if y >= ks[-1][0]:
            out[y] = int(round(ks[-1][1])); continue
        i = max(j for j in range(len(ks) - 1) if ks[j][0] <= y)
        p0 = ks[max(i - 1, 0)]; p1 = ks[i]; p2 = ks[i + 1]; p3 = ks[min(i + 2, len(ks) - 1)]
        t = (y - p1[0]) / (p2[0] - p1[0])
        m1 = (p2[1] - p0[1]) / max(p2[0] - p0[0], 1) * (p2[0] - p1[0])
        m2 = (p3[1] - p1[1]) / max(p3[0] - p1[0], 1) * (p2[0] - p1[0])
        h00, h10, h01, h11 = 2*t**3 - 3*t**2 + 1, t**3 - 2*t**2 + t, -2*t**3 + 3*t**2, t**3 - t**2
        out[y] = int(round(h00 * p1[1] + h10 * m1 + h01 * p2[1] + h11 * m2))
    return out


def fix_small(rgba, max_size=2, region=None):
    """같은 색 4-연결 조각 중 max_size 칸 이하(밝기 Y ≥ 41)를 둘레 4-이웃 중 가장 많은 밝은 색으로 — 구간 끝 부스러기 정리.
    region: 이 칸 안 조각만 (없으면 전체)"""
    out = rgba.copy()
    a = out[..., 3] > 0
    rgb = out[..., :3].astype(int)
    Y = 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]
    key = np.where(a, (rgb[..., 0] << 16) | (rgb[..., 1] << 8) | rgb[..., 2], -1)
    H_, W_ = a.shape
    for k in np.unique(key[a & (Y >= 41)]):
        lab, n = ndimage.label(key == k)
        sizes = np.bincount(lab.ravel())
        for i in range(1, n + 1):
            if sizes[i] > max_size:
                continue
            cells = np.argwhere(lab == i)
            if region is not None and not region[cells[:, 0], cells[:, 1]].any():
                continue
            cnt = {}
            for y, x in cells:
                for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    yy, xx = y + dy, x + dx
                    if 0 <= yy < H_ and 0 <= xx < W_ and a[yy, xx] and key[yy, xx] != k and Y[yy, xx] >= 41:
                        cnt[key[yy, xx]] = cnt.get(key[yy, xx], 0) + 1
            if not cnt:
                continue
            v = max(cnt, key=cnt.get)
            for y, x in cells:
                out[y, x, :3] = ((v >> 16) & 255, (v >> 8) & 255, v & 255)
                key[y, x] = v
    return out
