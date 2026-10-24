"""명암 (2-2 단계 4): 형태 모델의 법선 × 광원 → 단계. 외곽과 평행하게 겹겹이 어두워지는 「베개 명암」은 쓰지 않는다.

좌표: x 오른쪽 + · y 위쪽 + (화면 아래로 가는 행 번호와 반대) · z 보는 사람 쪽 +.
광원 = 위 · 15° 안쪽 왼쪽 (2-1) + 앞쪽.
"""
import numpy as np

LIGHT = np.array([-0.21, 0.80, 0.52])      # 왼쪽 성분 = tan 15° × 위
LIGHT = LIGHT / np.linalg.norm(LIGHT)


def normalize(n):
    l = np.linalg.norm(n, axis=-1, keepdims=True)
    return n / np.maximum(l, 1e-9)


def from_height(h, mask, depth=1.0):
    """높이장 h(행 · 열) → 화면 법선. 위쪽 성분은 행이 줄어드는 쪽"""
    gy, gx = np.gradient(h * depth)
    n = np.dstack([-gx, gy, np.ones_like(h)])
    n = normalize(n)
    n[~mask] = 0
    return n


def ellipsoid(shape, cx, cy, rx, ry, power=1.0):
    """타원체 높이장 (cx, cy = 화소 중심 · r = 반지름) — 슬라임 · 구슬 · 영혼 덩이"""
    H, W = shape
    ys, xs = np.mgrid[0:H, 0:W]
    u = (xs + 0.5 - cx) / max(rx, 1e-6)
    v = (ys + 0.5 - cy) / max(ry, 1e-6)
    return np.sqrt(np.clip(1 - u * u - v * v, 0, 1)) ** power


def lambert(n, light=LIGHT, wrap=0.0):
    d = (n @ light + wrap) / (1 + wrap)
    return np.clip(d, 0, 1)


def bands(v, cuts):
    """밝기 → 단계 번호 (cuts 오름차순 · 0 = 가장 어두움)"""
    return np.digitize(v, cuts)


def rotate_normals_cw(n):
    """그림을 시계 방향 90° 돌릴 때 법선도 같이: 위 → 오른쪽 · 왼쪽 → 위"""
    out = np.zeros_like(n)
    out[..., 0] = n[..., 1]
    out[..., 1] = -n[..., 0]
    out[..., 2] = n[..., 2]
    return out
