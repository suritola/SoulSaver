#!/usr/bin/env python
"""테이크 고르기: trace.csv 에서 「사람 같은가」 지표를 재고, 가장 센 2초 구간과 조준선 궤적 그림을 만든다.

  py -3.14 tools/trailer/post/takes.py <테이크 폴더> [--trail] [--only aim2]

지표(통과 기준):
  step    조준선이 한 프레임에 가장 멀리 간 거리 / 화면 높이 < 0.12 (순간 이동 없음)
  bell    도약(속도 봉우리)마다 봉우리 앞뒤가 완만한가 — 봉우리 속도의 절반 이상인 프레임이 3개 이상인 비율 ≥ 0.8
  center  표적 정중앙(12px 안)에 있는 프레임 비율 < 0.30
  fireCV  발사 간격의 변동 계수 ≥ 0.2
  turns   이동 방향 전환 < 초당 4번
  react   새 표적 → 첫 발 중앙값 150~600ms
  off     표적에서 0.75칸 넘게 벗어난 채 쏜 탄 비율 (참고: 5~15%가 자연스러움)
  hits    맞은 횟수 ≤ 1
"""
import argparse
import csv
import glob
import json
import os
import subprocess
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.stdout.reconfigure(encoding="utf-8")
from common import ffmpeg  # noqa: E402


def load(path):
    with open(path, encoding="utf-8") as f:
        rows = list(csv.DictReader(f))
    return {k: np.array([float(r[k]) for r in rows]) for k in rows[0]} if rows else None


def metrics(t, h):
    n = len(t["f"])
    d = np.hypot(np.diff(t["cx"]), np.diff(t["cy"]))
    scale = h / 1920
    has = t["gx"] >= 0
    off = np.hypot(t["cx"] - t["gx"], t["cy"] - t["gy"])
    # 도약 봉우리: 속도가 양옆보다 크고 20px/프레임 이상
    peaks = [i for i in range(2, len(d) - 2) if d[i] > 20 * scale and d[i] >= d[i - 1] and d[i] > d[i + 1]]
    bell = np.mean([np.sum(d[max(0, i - 6):i + 7] >= d[i] / 2) >= 3 for i in peaks]) if peaks else 1.0
    shots = np.flatnonzero(np.diff(t["bullets"]) < 0) + 1
    if len(shots) < 4:  # 탄을 안 쓰는 영웅: 누른 순간
        shots = np.flatnonzero(np.diff(t["held"]) > 0) + 1
    iv = np.diff(shots)
    cv = float(iv.std() / iv.mean()) if len(iv) > 2 else 0.0
    mv = t["mv"]
    turns = np.sum(np.diff(mv) != 0) / (n / 60)
    press = np.flatnonzero(np.diff(t["held"]) > 0) + 1
    newt = np.flatnonzero((np.diff(t["tgt"]) != 0) & (t["tgt"][1:] != 0)) + 1
    lat = [int(press[press >= i][0] - i) for i in newt if np.any(press >= i) and press[press >= i][0] - i < 90]
    react = float(np.median(lat)) / 60 * 1000 if lat else 0.0
    miss = float(np.mean(off[shots][has[shots]] > 60 * scale)) if np.any(has[shots]) else 0.0
    alive = t["alive"]
    kills = np.clip(-np.diff(alive), 0, None)
    act = np.zeros(n)
    act[1:] += kills * 3
    act[shots] += 1
    win = 120
    best = int(np.argmax(np.convolve(act, np.ones(win), "valid"))) if n > win else 0
    m = {"frames": n, "step": float(d.max() / h), "bell": float(bell), "center": float(np.mean(off[has] < 12 * scale)) if has.any() else 0.0,
         "fireCV": cv, "turns": float(turns), "react": react, "off": miss, "hits": int(t["hit"].sum() > 0) + int(np.sum(np.diff(t["hit"]) > 0) > 1),
         "shots": int(len(shots)), "kills": int(kills.sum()), "best": best}
    m["hitFrames"] = int(t["hit"].sum())
    ok = {"step": m["step"] < 0.12, "bell": m["bell"] >= 0.8, "center": m["center"] < 0.30, "fireCV": m["fireCV"] >= 0.2,
          "turns": m["turns"] < 4, "react": 150 <= m["react"] <= 600 or not lat, "hits": np.sum(np.diff(np.r_[0, t["hit"]]) > 0) <= 1}
    m["fail"] = [k for k, v in ok.items() if not v]
    return m


def trail(base, t, start, n, out):
    """start 부터 n 프레임의 조준선 자취를 가운데 프레임 위에 그린다(점 간격 = 속도, 빨강 = 쏘는 중)."""
    from PIL import Image, ImageDraw
    mid = start + n // 2
    raw = subprocess.run([ffmpeg(), "-hide_banner", "-loglevel", "error", "-i", base + ".mkv", "-vf", r"select=eq(n\,%d)" % mid, "-frames:v", "1",
                          "-f", "image2pipe", "-c:v", "png", "-"], stdout=subprocess.PIPE, check=True).stdout
    import io
    im = Image.open(io.BytesIO(raw)).convert("RGB")
    d = ImageDraw.Draw(im, "RGBA")
    pts = list(zip(t["cx"][start:start + n], t["cy"][start:start + n]))
    held = t["held"][start:start + n]
    d.line(pts, fill=(255, 255, 255, 150), width=2)
    for i, (x, y) in enumerate(pts):
        c = (255, 60, 60, 255) if held[i] else (80, 220, 255, 255)
        d.ellipse([x - 4, y - 4, x + 4, y + 4], fill=c)
    gx, gy = t["gx"][start:start + n], t["gy"][start:start + n]
    for i in range(0, n, 6):
        if gx[i] >= 0:
            d.rectangle([gx[i] - 3, gy[i] - 3, gx[i] + 3, gy[i] + 3], outline=(255, 220, 0, 200))
    hx, hy = t["hx"][start:start + n], t["hy"][start:start + n]
    d.line(list(zip(hx, hy)), fill=(120, 255, 120, 180), width=3)
    im.save(out, quality=88)


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("dir")
    ap.add_argument("--trail", action="store_true", help="가장 센 2초의 궤적 그림(<테이크>.trail.jpg)")
    ap.add_argument("--only", default="")
    a = ap.parse_args()
    res, passed = {}, 0
    for p in sorted(glob.glob(os.path.join(a.dir, "*.trace.csv"))):
        base = p[:-len(".trace.csv")]
        name = os.path.basename(base)
        if a.only and not name.startswith(a.only):
            continue
        t = load(p)
        meta = json.load(open(base + ".meta.json")) if os.path.isfile(base + ".meta.json") else {"h": 1920}
        if t is None or len(t["f"]) < 130:
            continue
        m = metrics(t, meta["h"])
        res[name] = m
        passed += not m["fail"]
        print(f"{name:26s} {'통과' if not m['fail'] else '탈락 ' + ','.join(m['fail']):18s} step {m['step']:.3f} bell {m['bell']:.2f} center {m['center']:.2f} "
              f"fireCV {m['fireCV']:.2f} turns {m['turns']:.1f}/s react {m['react']:.0f}ms off {m['off']:.2f} hit {m['hitFrames']} shots {m['shots']} kills {m['kills']} best {m['best']}")
        if a.trail:
            trail(base, t, m["best"], 120, base + ".trail.jpg")
    print(f"지표 통과 {passed}/{len(res)}")
    json.dump(res, open(os.path.join(a.dir, "takes.json"), "w"), indent=1)
