# 새 영웅 넷 스킨(2.0.0) 메뉴 초상 — 설계도(32칸) 좌표로 덧그리는 도우미 (k = 4 : (x, y) → (4x, 4y − 3))
import numpy as np

from .portrait import poly_mask, ellipse_mask, n_plane, n_ellipsoid


def P(pts):
    return [(4.0 * x, 4.0 * y - 3.0) for x, y in pts]


def poly(c, name, pts, mat, normal=None, behind=True, bias=0.0, line=True):
    """설계도 좌표 다각형 (꼭짓점 = 칸 모서리). behind = 빈칸에만 (몸 뒤)"""
    m = poly_mask(P(pts), c.S)
    if behind:
        m &= c.mat < 0
    return c.part(name, m, mat, normal or n_plane(0.25, -0.5, 0.8), bias=bias, line=line)


def rect(c, name, x0, y0, x1, y1, mat, **kw):
    return poly(c, name, [(x0, y0), (x1, y0), (x1, y1), (x0, y1)], mat, **kw)


def ell(c, name, cx, cy, rx, ry, mat, behind=True, normal=None, bias=0.0):
    m = ellipse_mask(4.0 * cx, 4.0 * cy - 3.0, 4.0 * rx, 4.0 * ry, c.S)
    if behind:
        m &= c.mat < 0
    return c.part(name, m, mat, normal or n_ellipsoid(4.0 * cx, 4.0 * cy - 3.0, 4.0 * rx, 4.0 * ry), bias=bias)


def tune(V, **factors):
    """Variant 의 재질 단계 색 밝기를 배율로 조정 (factor > 1 이면 흰 쪽으로 · < 1 이면 검은 쪽으로 — 형태 · 단계 수 그대로).
    재질 이름 '_all' = 전부 (개별 지정이 우선)"""
    orig = V.materials

    def mats(mod):
        out = orig(mod)
        for n, md in out.items():
            f = factors.get(n, factors.get('_all'))
            if f is None or f == 1.0:
                continue
            md = dict(md)
            ramp = []
            for col in md['ramp']:
                col = tuple(col[:3])
                if f > 1:
                    t = f - 1
                    ramp.append(tuple(int(round(v + (255 - v) * t)) for v in col))
                else:
                    ramp.append(tuple(int(round(v * f)) for v in col))
            md['ramp'] = ramp
            out[n] = md
        return out
    V.materials = mats
    return V


from .skin import Variant


class Variant2(Variant):
    """Variant + 덧그림 부위의 바깥 외곽 색 고르기 (lint 3 「가장자리 한 색 몫」: 기본 모듈은 따뜻한 부위 목록을 손으로 정함 →
    덧그린 부위는 목록에 없어 차가운 외곽이 되어 한 색 몫이 깨짐). warm = 따뜻한 외곽을 줄 덧그림 부위 이름들 (None = 전부)"""
    def __init__(self, *a, warm=None, **kw):
        self._warm = warm
        hook = kw.pop('shape', None)
        super().__init__(*a, **kw)
        self._hook2 = hook

    def module(self):
        first = self._mod is None
        mod = super().module()
        if first and self._hook2 is not None:
            hook, owner = self._hook2, self
            base = mod.Canvas

            class SC(base):
                def outline(self, warm_parts=None):
                    n0 = len(self.parts)
                    owner._dots = hook(self, mod) or []
                    if warm_parts is not None:
                        ex = [i for i in range(n0, len(self.parts))
                              if owner._warm is None or self.parts[i]['name'] in owner._warm]
                        warm_parts = tuple(warm_parts) + tuple(ex)
                    return base.outline(self, warm_parts)
            mod.Canvas = SC
        return mod
