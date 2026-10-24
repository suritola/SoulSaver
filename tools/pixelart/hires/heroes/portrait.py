# 메뉴 초상 공용 붓 (그래픽 가독성 개편 2단계 묶음 3 · 기획서 2-1 · 2-2)
#   128칸 캔버스에 부위(모양 · 재질 · 형태 모델)를 뒤에서 앞으로 칠하고 → 형태 모델 명암을 재질 단계로 나누고
#   → 부위 경계선 · 바깥 외곽 1칸 → 덧그림(손 도트 지도 · 선 · 점) → 정리(작은 단계 조각 합치기) 순서로 그림.
#   32칸 영웅 그림은 「설계도」(자리 · 비율 · 색 · 광원)로만 쓰고 그 화소를 키워 쓰지 않음 (그냥 확대 금지 — lint 7 · 9 항목).
#   좌표는 128칸 그대로 (x 오른쪽 · y 아래). 설계도 칸 → 128칸 = Map(k, ox, oy).
#   알파 0/255 만 · 그라데이션 · 디더 없음 · 난수 없음 (같은 입력이면 같은 그림)
# 이 모듈은 영웅 초상 전용 — 보스 쪽 hires 공용 모듈(blueprint · upscale · shade …)은 건드리지 않음
import math

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

OUT1 = (0x1A, 0x11, 0x2C)       # 바깥 외곽 기본 (지금 그림체 #1A112C)
OUT2 = (0x2D, 0x19, 0x2A)       # 따뜻한 재질(살 · 가죽 · 금 · 붉은 천) 옆 외곽 (make_kit_weapons 의 'O')


def hx(s):
    s = s.lstrip('#')
    return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16))


def luma(c):
    return 0.299 * c[0] + 0.587 * c[1] + 0.114 * c[2]


class Map:
    """설계도 칸 → 128칸 좌표 (칸 왼쪽 위 모서리 기준)"""
    def __init__(self, k=4, ox=0, oy=0):
        self.k, self.ox, self.oy = k, ox, oy

    def __call__(self, x, y):
        return (x * self.k + self.ox, y * self.k + self.oy)


def bezier(pts, n=10):
    """조절점 목록(2 · 3 · 4개)을 따라가는 점들 — 곡선 외곽을 계단 없이 찍으려고"""
    out = []
    if len(pts) == 2:                       # 조절점 둘 = 곧은 선
        (x0, y0), (x1, y1) = pts
        return [(x0 + (x1 - x0) * i / n, y0 + (y1 - y0) * i / n) for i in range(n + 1)]
    if len(pts) == 3:
        (x0, y0), (x1, y1), (x2, y2) = pts
        for i in range(n + 1):
            t = i / n
            out.append(((1 - t) ** 2 * x0 + 2 * (1 - t) * t * x1 + t * t * x2,
                        (1 - t) ** 2 * y0 + 2 * (1 - t) * t * y1 + t * t * y2))
    else:
        (x0, y0), (x1, y1), (x2, y2), (x3, y3) = pts
        for i in range(n + 1):
            t = i / n
            a, b, c, d = (1 - t) ** 3, 3 * (1 - t) ** 2 * t, 3 * (1 - t) * t * t, t ** 3
            out.append((a * x0 + b * x1 + c * x2 + d * x3, a * y0 + b * y1 + c * y2 + d * y3))
    return out


def path(*segs):
    """점 · 베지어 조각을 이어 다각형 점 목록으로. 조각 = (x, y) 또는 [조절점 …]"""
    out = []
    for s in segs:
        if isinstance(s, list):
            pts = bezier(s, 12)
            if out and pts and abs(out[-1][0] - pts[0][0]) < 1e-6 and abs(out[-1][1] - pts[0][1]) < 1e-6:
                pts = pts[1:]
            out += pts
        else:
            out.append(s)
    return out


def poly_mask(pts, size):
    """칸 가운데가 다각형 안이면 칠함 (PIL 의 다각형은 칸 모서리 기준이라 −0.5 옮김)"""
    im = Image.new('L', (size, size), 0)
    ImageDraw.Draw(im).polygon([(x - 0.5, y - 0.5) for x, y in pts], fill=255)
    return np.asarray(im) > 0


def ellipse_mask(cx, cy, rx, ry, size):
    yy, xx = np.mgrid[0:size, 0:size]
    return ((xx + 0.5 - cx) / rx) ** 2 + ((yy + 0.5 - cy) / ry) ** 2 <= 1.0


def line_cells(pts):
    """꺾은선을 1칸 굵기 칸 목록으로 (브레즌햄 · 8-연결)"""
    cells = []
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        x0, y0, x1, y1 = int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
        err = dx + dy
        while True:
            if not cells or cells[-1] != (x0, y0):
                cells.append((x0, y0))
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy
    return cells


# ───────────────────────────── 형태 모델 (법선) ─────────────────────────────
# 법선 = (x 오른쪽, y 아래, z 보는 쪽). 밝기 = 바탕 + 확산 × max(0, n · L)
def n_ellipsoid(cx, cy, rx, ry, flat=0.0):
    """타원체 (머리 두건 · 손 · 매듭). flat > 0 이면 가운데를 납작하게 (천처럼)"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / rx, -1, 1)
        v = np.clip((yy + 0.5 - cy) / ry, -1, 1)
        r2 = np.clip(u * u + v * v, 0, 0.98)
        z = np.sqrt(1 - r2) + flat
        return u, v, z
    return f


def n_cyl_v(cx, r, tilt=0.0):
    """세로 원기둥 (몸통 · 팔 · 다리). tilt > 0 = 위가 뒤로 젖혀져 위쪽이 빛을 더 받음"""
    def f(xx, yy):
        u = np.clip((xx + 0.5 - cx) / r, -0.97, 0.97)
        return u, np.full_like(u, -tilt), np.sqrt(1 - u * u)
    return f


def n_cyl_h(cy, r, tilt=0.0):
    """가로 원기둥 (목도리 띠 · 허리띠)"""
    def f(xx, yy):
        v = np.clip((yy + 0.5 - cy) / r, -0.97, 0.97)
        return np.full_like(v, tilt), v, np.sqrt(1 - v * v)
    return f


def n_plane(nx, ny, nz):
    def f(xx, yy):
        return np.full(xx.shape, nx, float), np.full(xx.shape, ny, float), np.full(xx.shape, nz, float)
    return f


class Canvas:
    """부위 단위 그림판. mat = 재질 번호(−1 빈칸), lvl = 단계, pid = 부위 번호"""
    def __init__(self, size, materials, light=(0.3, -0.75, 0.6)):
        self.S = size
        self.names = list(materials)
        self.ramps = [list(materials[n]['ramp']) for n in self.names]
        self.cuts = [list(materials[n].get('cuts', [])) for n in self.names]
        self.warm = [bool(materials[n].get('warm')) for n in self.names]
        self.mat = np.full((size, size), -1, int)
        self.lvl = np.zeros((size, size), int)
        self.pid = np.full((size, size), -1, int)
        self.fixed = np.zeros((size, size), bool)       # 덧그림으로 정한 칸 (정리에서 안 건드림)
        self.color = {}                                   # 덧그림 낱색 칸 (눈빛처럼 재질 밖 색) — (y, x) → rgb
        self.parts = []
        L = np.array(light, float)
        self.L = L / np.linalg.norm(L)
        self.yy, self.xx = np.mgrid[0:size, 0:size].astype(float)

    def mi(self, name):
        return self.names.index(name)

    def part(self, name, mask, mat, normal, amb=0.18, dif=0.95, bias=0.0, line=True, smooth=0.8):
        """mask 칸을 그 재질로 덮어 칠함 (뒤 → 앞 순서로 부름). 밝기 → 단계는 재질의 cuts
        smooth = 밝기 장을 살짝 흐려 단계 경계가 칸 단위로 들쭉날쭉하지 않게 (경계 = 매끈한 곡선)"""
        m = self.mi(mat)
        nx, ny, nz = normal(self.xx, self.yy)
        ln = np.sqrt(nx * nx + ny * ny + nz * nz)
        d = (nx * self.L[0] + ny * self.L[1] + nz * self.L[2]) / np.maximum(ln, 1e-6)
        b = amb + dif * np.clip(d, 0, 1) + bias
        if smooth:
            b = ndimage.gaussian_filter(b, smooth)
        lv = np.digitize(b, self.cuts[m])
        lv = np.clip(lv, 0, len(self.ramps[m]) - 1)
        p = len(self.parts)
        self.parts.append(dict(name=name, mat=m, line=line))
        self.mat[mask] = m
        self.lvl[mask] = lv[mask]
        self.pid[mask] = p
        self.fixed[mask] = False
        for key in [k for k in self.color if mask[k]]:
            del self.color[key]
        return p

    def shift(self, mask, by, part=None, lo=0, hi=None):
        """칸의 단계를 by 만큼 (그림자 −, 빛 +). part 를 주면 그 부위 칸만"""
        m = mask & (self.mat >= 0)
        if part is not None:
            m &= self.pid == part
        for mi in np.unique(self.mat[m]):
            mm = m & (self.mat == mi)
            top = len(self.ramps[mi]) - 1 if hi is None else hi
            self.lvl[mm] = np.clip(self.lvl[mm] + by, lo, top)

    def set_lvl(self, cells, level, part=None, mat=None, fix=True):
        """칸 목록을 정한 단계로 (덧그림 — 선 · 주름 · 바느질). mat 를 주면 재질도 바꿈"""
        for x, y in cells:
            if not (0 <= x < self.S and 0 <= y < self.S) or self.mat[y, x] < 0:
                continue
            if part is not None and self.pid[y, x] not in (part if isinstance(part, (list, tuple, set)) else (part,)):
                continue
            if mat is not None:
                self.mat[y, x] = self.mi(mat)
            n = len(self.ramps[self.mat[y, x]])
            self.lvl[y, x] = level if level >= 0 else n + level
            self.fixed[y, x] = fix

    def nudge(self, cells, by, part=None, fix=True):
        """칸 목록의 단계를 by 만큼 옮김 (재질 단계 안에서)"""
        for x, y in cells:
            if not (0 <= x < self.S and 0 <= y < self.S) or self.mat[y, x] < 0:
                continue
            if part is not None and self.pid[y, x] not in (part if isinstance(part, (list, tuple, set)) else (part,)):
                continue
            n = len(self.ramps[self.mat[y, x]])
            self.lvl[y, x] = int(np.clip(self.lvl[y, x] + by, 0, n - 1))
            self.fixed[y, x] = fix

    def crease(self, pts, part=None, by=-1, hi=(1, 0), hi_by=1, hi_dash=None):
        """천 주름: 접힌 골은 by 만큼 어둡게, 빛 쪽(hi 방향 1칸)의 등성이는 hi_by 만큼 밝게.
        hi_dash = (칠함, 쉼) — 등성이 빛을 끊어 찍음 (도트 그림의 「끊긴 하이라이트」)"""
        cells = line_cells(pts)
        self.nudge(cells, by, part)
        if hi is not None and hi_by:
            cs = set(cells)
            ridge = [(x + hi[0], y + hi[1]) for x, y in cells if (x + hi[0], y + hi[1]) not in cs]
            if hi_dash:
                on, off = hi_dash
                ridge = [r for i, r in enumerate(ridge) if i % (on + off) < on]
            self.nudge(ridge, hi_by, part)
        return cells

    def dashes(self, pts, part=None, on=2, off=2, by=1, level=None):
        """바느질 · 끈 무늬: 선을 on 칸 칠하고 off 칸 쉼"""
        cells = line_cells(pts)
        pick = [cl for i, cl in enumerate(cells) if i % (on + off) < on]
        if level is None:
            self.nudge(pick, by, part)
        else:
            self.set_lvl(pick, level, part)
        return pick

    def stamp(self, rows, ox, oy, legend, part=None):
        """손 도트 지도 — 글자 격자를 (ox, oy) 에 찍음. legend: 글자 → (재질, 단계) 또는 rgb 튜플(낱색). '.' = 건드리지 않음"""
        for j, row in enumerate(rows):
            for i, ch in enumerate(row):
                if ch == '.' or ch == ' ':
                    continue
                x, y = ox + i, oy + j
                if not (0 <= x < self.S and 0 <= y < self.S):
                    continue
                v = legend[ch]
                if not isinstance(v[0], str):                     # 낱색 (r, g, b)
                    if self.mat[y, x] < 0:
                        continue
                    self.color[(y, x)] = v
                else:
                    mat, level = v
                    if self.mat[y, x] < 0 and part is None:
                        continue
                    self.mat[y, x] = self.mi(mat)
                    n = len(self.ramps[self.mat[y, x]])
                    self.lvl[y, x] = level if level >= 0 else n + level
                    if part is not None:
                        self.pid[y, x] = part
                    self.color.pop((y, x), None)
                self.fixed[y, x] = True

    # ───────────────────────── 정리 · 선 · 외곽 ─────────────────────────
    def tidy(self, min_size=3, passes=2):
        """같은 부위 안에서 min_size 칸보다 작은 단계 조각을 둘레에서 가장 많은 단계로 (덧그림 칸 제외)"""
        for _ in range(passes):
            key = np.where(self.mat >= 0, self.pid * 64 + self.lvl, -1)
            for k in np.unique(key[key >= 0]):
                lab, n = ndimage.label(key == k, structure=np.ones((3, 3), int))
                if n == 0:
                    continue
                sizes = np.bincount(lab.ravel())
                for li in np.nonzero(sizes[1:] < min_size)[0] + 1:
                    cells = lab == li
                    if self.fixed[cells].any():
                        continue
                    ring = ndimage.binary_dilation(cells) & ~cells & (self.pid == k // 64) & ~self.fixed
                    if not ring.any():
                        continue
                    vals, cnt = np.unique(self.lvl[ring], return_counts=True)
                    self.lvl[cells] = vals[np.argmax(cnt)]

    def contact_lines(self, skip=()):
        """부위 경계 = 뒤 부위의 가장 어두운 단계 1칸 (기획서 2-1 「안쪽 선」). skip = 선을 안 넣을 (뒤, 앞) 이름 쌍"""
        S = self.S
        P = np.pad(self.pid, 1, constant_values=-1)
        out = np.zeros((S, S), bool)
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            nb = P[1 + dy:S + 1 + dy, 1 + dx:S + 1 + dx]
            cand = (self.pid >= 0) & (nb >= 0) & (nb > self.pid)
            for y, x in zip(*np.nonzero(cand)):
                back, front = self.parts[self.pid[y, x]], self.parts[nb[y, x]]
                if not back['line'] or (back['name'], front['name']) in skip or self.fixed[y, x]:
                    continue
                out[y, x] = True
        self.lvl[out] = 0
        return out

    def outline(self, warm_parts=None):
        """바깥 외곽 1칸: 불투명 칸 중 4방향에 빈칸이 있는 칸을 외곽 색으로 (몸 크기 그대로 — 바깥으로 안 늘림).
        그 칸이 따뜻한 재질(살 · 가죽 …)이면 #2D192A, 아니면 #1A112C.
        warm_parts = 따뜻한 외곽을 줄 부위 번호 목록 (주면 재질 대신 이것 — 가장자리 한 색 몫 75~95% 를 맞출 때)"""
        a = self.mat >= 0
        P = np.pad(a, 1)
        edge = a & (~P[:-2, 1:-1] | ~P[2:, 1:-1] | ~P[1:-1, :-2] | ~P[1:-1, 2:])
        self.edge = edge
        warm = np.zeros_like(a)
        if warm_parts is None:
            for i, w in enumerate(self.warm):
                if w:
                    warm |= self.mat == i
        else:
            for p in warm_parts:
                warm |= self.pid == p
        # 칸 자신(외곽이 되기 전 재질)이 따뜻하면 따뜻한 외곽
        self.outline_warm = edge & warm
        return edge

    def absorb(self, mask, min_size=3, passes=3):
        """mask 안에서 4-연결 기준 min_size 칸보다 작은 단계 조각(같은 부위 · 같은 단계)을 4방향 이웃에서 가장 많은 단계로 흡수
        — 대각선 계단으로만 이어진 1칸 굵기 하이라이트 · 주름 끝이 눈에는 잔점 무리로 보임 (도적 시안 2회차 판정: 두건 림 52조각)
        덧그림 칸도 흡수함 (의도한 점은 render 뒤 color 로 찍는 눈 같은 낱색만 남음). 실루엣 가장자리 칸(외곽이 될 칸)은 셈에서 뺌"""
        a = self.mat >= 0
        P = np.pad(a, 1)
        edge = a & (~P[:-2, 1:-1] | ~P[2:, 1:-1] | ~P[1:-1, :-2] | ~P[1:-1, 2:])
        live = a & ~edge
        for _ in range(passes):
            key = np.where(live, self.pid * 64 + self.lvl, -1)
            changed = 0
            for k in np.unique(key[live & mask]):
                lab, n = ndimage.label(key == k)
                cnt = np.bincount(lab.ravel())
                cnt[0] = 0
                for li in np.nonzero((cnt > 0) & (cnt < min_size))[0]:
                    cells = lab == li
                    if not (cells & mask).any():
                        continue
                    ring = ndimage.binary_dilation(cells) & ~cells & live & (self.pid == k // 64)
                    if not ring.any():
                        continue
                    vals, cn = np.unique(self.lvl[ring], return_counts=True)
                    self.lvl[cells] = vals[np.argmax(cn)]
                    changed += 1
            if not changed:
                break

    def rim(self, parts, direction=(0.45, -0.9), width=1, by=None, level=None, thresh=0.35):
        """림 라이트: 실루엣 바깥 외곽(1칸) 바로 안쪽 width 칸 중, 바깥 방향이 direction(x 오른쪽 · y 아래)을 보는 칸만 밝게.
        배경과 몸을 가르는 가장자리 빛 — 몸 전체를 밝히지 않고 위 · 오른쪽 윤곽만 (일괄 림 아님: 빛 쪽 몇 부위만)
        by = 단계 이동 · level = 정한 단계 (−1 = 가장 밝은 단계)"""
        a = self.mat >= 0
        dist = ndimage.distance_transform_cdt(np.pad(a, 1), metric='taxicab')[1:-1, 1:-1]
        sm = ndimage.gaussian_filter(a.astype(float), 1.6)
        gy, gx = np.gradient(sm)
        ln = np.sqrt(gx * gx + gy * gy) + 1e-9
        nx, ny = -gx / ln, -gy / ln                  # 바깥 방향
        dx, dy = direction
        dl = math.hypot(dx, dy)
        score = (nx * dx + ny * dy) / dl
        ring = a & (dist >= 2) & (dist <= 1 + width) & (score > thresh)
        sel = np.zeros_like(a)
        for p in (parts if isinstance(parts, (list, tuple, set)) else (parts,)):
            sel |= self.pid == p
        cells = [(int(x), int(y)) for y, x in zip(*np.nonzero(ring & sel))]
        if level is not None:
            self.set_lvl(cells, level)
        else:
            self.nudge(cells, by if by is not None else 1)
        return cells

    def render(self):
        S = self.S
        out = np.zeros((S, S, 4), np.uint8)
        for i, ramp in enumerate(self.ramps):
            m = self.mat == i
            if not m.any():
                continue
            lv = np.clip(self.lvl[m], 0, len(ramp) - 1)
            cols = np.array(ramp, np.uint8)[lv]
            out[m, :3] = cols
            out[m, 3] = 255
        for (y, x), c in self.color.items():
            out[y, x, :3] = c
        if hasattr(self, 'edge'):
            e = self.edge & ~self.outline_warm
            out[e, :3] = OUT1
            out[self.outline_warm, :3] = OUT2
        return out


def save_png(arr, path):
    Image.fromarray(arr, 'RGBA').save(path)


def despeckle(arr, protect=(), passes=2, intended=None, budget=0.0):
    """정리 (기획서 2-2 단계 6): 같은 색 8-연결 1칸 섬을 둘레에서 가장 많은 색으로. protect = 의도한 점 [(x, y)] (눈 · 반짝)
    intended = 덧그림으로 일부러 찍은 칸 지도(바느질 · 윤기 점) — 그 칸의 1칸 섬은 불투명 칸의 budget 몫까지 남김
      (lint 8 「1칸 섬 ≤ 1%」 안 — 명암 띠 경계에서 생긴 우연한 점만 지움). 빈칸 · 반투명은 건드리지 않음. arr 를 고쳐 돌려줌"""
    keep = set(map(tuple, protect))
    if intended is not None and budget > 0:
        a0 = arr[..., 3] > 0
        rgb0 = arr[..., :3].astype(np.int64)
        key0 = np.where(a0, (rgb0[..., 0] << 16) | (rgb0[..., 1] << 8) | rgb0[..., 2], -1)
        cand = []
        for k in np.unique(key0[a0]):
            lab, n = ndimage.label(key0 == k, structure=np.ones((3, 3), int))
            cnt = np.bincount(lab.ravel())
            cnt[0] = 0
            for li in np.nonzero(cnt == 1)[0]:
                y, x = (int(v) for v in np.argwhere(lab == li)[0])
                if intended[y, x]:
                    cand.append((y, x))
        cap = int(budget * int(a0.sum())) - len(keep)
        for y, x in sorted(cand)[:max(0, cap)]:
            keep.add((x, y))
    H, W = arr.shape[:2]
    for _ in range(passes):
        a = arr[..., 3] > 0
        rgb = arr[..., :3].astype(np.int64)
        key = np.where(a, (rgb[..., 0] << 16) | (rgb[..., 1] << 8) | rgb[..., 2], -1)
        changed = 0
        for k in np.unique(key[a]):
            lab, n = ndimage.label(key == k, structure=np.ones((3, 3), int))
            cnt = np.bincount(lab.ravel())
            cnt[0] = 0
            for li in np.nonzero(cnt == 1)[0]:
                y, x = (int(v) for v in np.argwhere(lab == li)[0])
                if (x, y) in keep:
                    continue
                nb = {}
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        yy, xx = y + dy, x + dx
                        if (dy or dx) and 0 <= yy < H and 0 <= xx < W and a[yy, xx]:
                            kk = int(key[yy, xx])
                            nb[kk] = nb.get(kk, 0) + 1
                if not nb:
                    continue
                best = max(nb.items(), key=lambda t: t[1])[0]
                arr[y, x, :3] = ((best >> 16) & 255, (best >> 8) & 255, best & 255)
                changed += 1
        if not changed:
            break
    return arr
