# 스킨 메뉴 초상 128칸 — 색 대응 스킨의 공용 틀 (그래픽 가독성 개편 2단계 묶음 3 · 스킨 20장)
#   README 「색 대응 15벌」: 기본 영웅 모듈의 부위 · 형태 · 덧그림은 그대로, 재질 표(MATERIALS)만 스킨 32칸 색에서 다시 뽑아 같은 길로 그림
#   32칸 「칸별 색 대응표」를 128칸에 그대로 쓰지 않음 (128칸의 명암 단계 색은 32칸에 없음) →
#     재질마다 「기본 32칸 색 → 스킨 32칸 색」 짝(닻)을 모아 OKLCH 에서 명도 이동 · 채도 비 · 색상 회전을 명도 따라 보간해 단계 색을 옮김
#     (단계 수 · 명도 순서는 그대로 — 흑백 계층이 깨지지 않게. 밝은 스킨은 Variant 의 cap 으로 재질 최고 명도를 눌러 얼굴 · 눈 아래로)
#   기본 모듈은 새 사본으로 읽어 고침 (기본 영웅 초상 · 다른 스킨에 영향 없음)
#   예외 칸(모양 · 무늬가 조금 다른 스킨)은 extra(c) 덧그림 — 기본 draw() 가 끝난 뒤(외곽까지) 손 도트 지도로
import importlib.util
import math

import numpy as np
from PIL import Image

from .portrait import hx, luma, despeckle

ROOT_CHARS = 'Assets/Resources/Characters/'


# ───────────── 색 공간 (OKLab)
def _lin(c):
    c = c / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def _gam(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055) * 255.0


def to_lch(rgb):
    r, g, b = _lin(np.array(rgb, float))
    l = (0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b) ** (1 / 3)
    m = (0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b) ** (1 / 3)
    s = (0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b) ** (1 / 3)
    L = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s
    A = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s
    B = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s
    return L, math.hypot(A, B), math.atan2(B, A)


def from_lch(L, C, h):
    A, B = C * math.cos(h), C * math.sin(h)
    l = (L + 0.3963377774 * A + 0.2158037573 * B) ** 3
    m = (L - 0.1055613458 * A - 0.0638541728 * B) ** 3
    s = (L - 0.0894841775 * A - 1.2914855480 * B) ** 3
    r = 4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s
    g = -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s
    b = -0.0041960863 * l - 0.7034186147 * m + 1.7076424809 * s
    return tuple(int(round(v)) for v in _gam(np.array([r, g, b])))


def _dist(p, q):
    a, b = to_lch(p), to_lch(q)
    return math.sqrt((a[0] - b[0]) ** 2 + (a[1] * math.cos(a[2]) - b[1] * math.cos(b[2])) ** 2
                     + (a[1] * math.sin(a[2]) - b[1] * math.sin(b[2])) ** 2)


def remap(ramp, pairs):
    """단계 색 목록을 닻 짝 [(기본 색, 스킨 색)] 대로 옮김 — 명도 이동 · 채도 비 · 색상 회전을 기본 명도 따라 선형 보간
    (기본 닻이 무채색이면 색상은 스킨 닻의 색상을 그대로)"""
    if not pairs:
        return list(ramp)
    an = []
    for p, q in pairs:
        L0, C0, h0 = to_lch(p)
        L1, C1, h1 = to_lch(q)
        an.append((L0, L1 - L0, C0, C1, h0, h1))
    an.sort()
    out = []
    for col in ramp:
        L, C, h = to_lch(col)
        Ls = [a[0] for a in an]
        if L <= Ls[0] or len(an) == 1:
            i0 = i1 = 0 if L <= Ls[0] else len(an) - 1
            t = 0.0
        elif L >= Ls[-1]:
            i0 = i1 = len(an) - 1
            t = 0.0
        else:
            i1 = next(i for i, v in enumerate(Ls) if v >= L)
            i0 = i1 - 1
            t = (L - Ls[i0]) / max(Ls[i1] - Ls[i0], 1e-6)

        def one(a):
            L0, dL, C0, C1, h0, h1 = a
            ratio = C1 / C0 if C0 > 0.025 else None
            nC = C * ratio if ratio is not None else C1 + (C - C0) * 0.5
            dh = (h1 - h0) if (C0 > 0.025 and C1 > 0.02) else None
            return dL, nC, dh, h1, C1
        a0, a1 = one(an[i0]), one(an[i1])
        dL = a0[0] * (1 - t) + a1[0] * t
        nC = max(0.0, a0[1] * (1 - t) + a1[1] * t)
        # 목표 색상 = 각 닻의 (색상 + 회전) 또는 (무채색 닻이면 스킨 닻 색상) 을 원 위에서 보간
        g0 = h + a0[2] if a0[2] is not None else a0[3]
        g1 = h + a1[2] if a1[2] is not None else a1[3]
        g1 = g0 + math.remainder(g1 - g0, 2 * math.pi)
        nh = g0 * (1 - t) + g1 * t
        out.append(from_lch(min(max(L + dL, 0.0), 1.0), min(nC, 0.32), nh))
    return out


def cell32(sheet):
    return np.asarray(Image.open(ROOT_CHARS + sheet + '.png').convert('RGBA').crop((0, 0, 32, 32)))


def pairs_of(base, skin):
    """기본 32칸 칸 → 스킨 32칸 칸 색 짝 (가장 많은 대응 하나씩) · 바뀐 색 · 안 바뀐 색 모두"""
    a, b = cell32(base), cell32(skin)
    from collections import Counter, defaultdict
    m = defaultdict(Counter)
    for y in range(32):
        for x in range(32):
            if a[y, x, 3] and b[y, x, 3]:
                m[tuple(int(v) for v in a[y, x, :3])][tuple(int(v) for v in b[y, x, :3])] += 1
    return {p: c.most_common(1)[0][0] for p, c in m.items()}


def fresh(base):
    """기본 영웅 모듈의 새 사본 (전역을 바꿔도 기본 · 다른 스킨에 영향 없음)"""
    spec = importlib.util.find_spec(__package__ + '.' + base)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


class Variant:
    """색 대응 스킨 하나.
    mats   = {재질: 닻 색 목록 [기본 32칸 색 hex …]} — 그 재질을 옮길 닻 (없으면 그 재질 단계와 가까운 기본 색을 자동으로)
    ramps  = {재질: [hex …]} — 손으로 정한 단계 (닻 대응보다 우선)
    cap    = {재질: 최고 Y} — 그 재질 단계의 명도 상한 (밝은 스킨 옷이 얼굴 · 눈보다 밝아지지 않게)
    glob   = {전역 이름: 값} (EYE 같은 낱색) · extra(c, mod) = 기본 draw() 뒤 덧그림
    parts  = {부위 이름: [hex …]} — 같은 재질을 쓰던 부위 하나를 다른 단계 색으로 (성기사: 붉은 천 중 깃털만 금 · 망토는 파랑)
    add    = {새 재질: dict(ramp=[hex …], cuts=[…], warm=…)} — 모양이 다른 스킨(뿔 · 가면 …)이 쓰는 재질
    shape(c, mod) = 기본 draw() 가 바깥 외곽을 정하기 바로 전에 부르는 덧그림 (새 부위 · 손 도트 지도 — 실루엣이 바뀌어도 외곽이 따라감)
                    돌려준 [(x, y)] 는 의도한 점(눈빛)"""
    def __init__(self, base, skin, mats=None, ramps=None, cap=None, glob=None, extra=None, auto=True, skip=(), parts=None,
                 add=None, shape=None):
        self.base, self.skin = base, skin
        self.mats, self.ramps, self.cap = mats or {}, ramps or {}, cap or {}
        self.glob, self.extra, self.auto, self.skip = glob or {}, extra, auto, set(skip)
        self.part_ramps = parts or {}
        self.add, self.shape = add or {}, shape
        self.SIZE = 128
        self.DESIGN = ROOT_CHARS + skin + '.png'
        self._mod = None

    def materials(self, mod):
        pr = pairs_of(self.base, self.skin)
        base_mats = mod.MATERIALS
        # 자동 닻: 기본 32칸 색마다 단계 색이 가장 가까운 재질 하나 (거리 0.09 안 · 그 재질에서 쓰는 색으로 봄)
        auto = {n: [] for n in base_mats}
        if self.auto:
            for p in pr:
                best, bd = None, 0.09
                for n, md in base_mats.items():
                    d = min(_dist(p, r) for r in md['ramp'])
                    if d < bd:
                        best, bd = n, d
                if best is not None:
                    auto[best].append(p)
        out = {}
        for n, md in base_mats.items():
            md = dict(md)
            if n in self.ramps:
                md['ramp'] = [hx(s) if isinstance(s, str) else s for s in self.ramps[n]]
            elif n not in self.skip:
                anchors = [hx(s) for s in self.mats[n]] if n in self.mats else auto[n]
                pairs = [(p, pr[p]) for p in anchors if p in pr]
                if any(p != q for p, q in pairs):
                    md['ramp'] = remap(md['ramp'], pairs)
            if n in self.cap:
                md['ramp'] = cap_ramp(md['ramp'], self.cap[n])
            out[n] = md
        return out

    def module(self):
        if self._mod is None:
            mod = fresh(self.base)
            mats = self.materials(mod)
            for n, md in self.add.items():
                md = dict(md)
                md['ramp'] = [hx(v) if isinstance(v, str) else v for v in md['ramp']]
                mats[n] = md
            mod.MATERIALS = mats
            if self.shape is not None:
                hook, owner = self.shape, self

                class SkinCanvas(mod.Canvas):
                    def outline(self, warm_parts=None):
                        owner._dots = hook(self, mod) or []
                        return super().outline(warm_parts)
                mod.Canvas = SkinCanvas
            for k, v in self.glob.items():
                setattr(mod, k, hx(v) if isinstance(v, str) else v)
            mod.DESIGN = self.DESIGN
            self._mod = mod
        return self._mod

    def draw(self):
        mod = self.module()
        self._dots = []
        c, dots = mod.draw()
        dots = list(dots) + list(self._dots)
        for pn, ramp in self.part_ramps.items():
            repaint(c, pn, [hx(v) if isinstance(v, str) else v for v in ramp])
        if self.extra:
            r = self.extra(c, mod)
            if r:
                dots = list(dots) + list(r)
        return c, dots

    def build(self):
        c, dots = self.draw()
        return despeckle(c.render(), protect=dots), dots


def cap_ramp(ramp, ymax):
    """단계 색의 명도를 ymax 아래로 — 위 단계를 비율로 눌러 순서 · 간격을 지킴 (색상 · 채도는 OKLCH 에서 유지)"""
    ys = [luma(c) for c in ramp]
    if max(ys) <= ymax:
        return list(ramp)
    lo = min(ys)
    out = []
    for c, y in zip(ramp, ys):
        ty = lo + (y - lo) * (ymax - lo) / (max(ys) - lo) if max(ys) > lo else ymax
        L, C, h = to_lch(c)
        # 명도만 낮춤 — Y 를 맞출 때까지 L 을 이분 탐색
        a, b = 0.0, L
        for _ in range(30):
            mid = (a + b) / 2
            if luma(from_lch(mid, C, h)) < ty:
                a = mid
            else:
                b = mid
        out.append(from_lch(a, C, h))
    return out


def repaint(c, part_name, ramp):
    """그 이름 부위의 칸을 새 재질(ramp)로 옮김 — 단계 번호 · 덧그림 · 외곽은 그대로 (단계 수가 같아야 함)"""
    pids = [i for i, p in enumerate(c.parts) if p['name'] == part_name]
    assert pids, part_name
    m = np.isin(c.pid, pids) & (c.mat >= 0)
    old = set(int(v) for v in np.unique(c.mat[m]))
    for i in old:
        assert len(c.ramps[i]) == len(ramp), (part_name, len(c.ramps[i]), len(ramp))
    c.names.append(part_name + '_skin')
    c.ramps.append(list(ramp))
    c.cuts.append([])
    c.warm.append(any(c.warm[i] for i in old))
    c.mat[m] = len(c.names) - 1
