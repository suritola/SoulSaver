#!/usr/bin/env python
"""테이크의 한 구간을 직접 보기: 프레임 8장 + 그 구간의 조준선 자취(마지막 칸)를 한 장으로.

  py -3.14 tools/trailer/post/peek.py <테이크 폴더> <테이크> <시작 프레임> <길이> [<테이크> <시작> <길이> …]
결과: <테이크 폴더>/peek/<테이크>_<시작>.jpg
"""
import io
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw  # noqa: E402

import takes  # noqa: E402
from common import ffmpeg  # noqa: E402


def frame(mkv, n):
    raw = subprocess.run([ffmpeg(), "-hide_banner", "-loglevel", "error", "-i", mkv, "-vf", r"select=eq(n\,%d)" % n, "-frames:v", "1",
                          "-f", "image2pipe", "-c:v", "png", "-"], stdout=subprocess.PIPE, check=True).stdout
    return Image.open(io.BytesIO(raw)).convert("RGB")


def peek(d, take, start, n, cols=8):
    base = os.path.join(d, take)
    ims = [frame(base + ".mkv", start + round(i * (n - 1) / (cols - 1))) for i in range(cols)]
    w, h = ims[0].size
    crop = (w - h * 9 // 16) // 2 if w > h else 0      # 가로로 찍은 UI 컷은 가운데 세로 창만
    ims = [im.crop((crop, 0, w - crop, h)) for im in ims]
    tw = 270
    th = tw * ims[0].height // ims[0].width
    out = Image.new("RGB", (tw * (cols + 1), th))
    for i, im in enumerate(ims):
        out.paste(im.resize((tw, th), Image.NEAREST), (i * tw, 0))
    t = takes.load(base + ".trace.csv")
    if t is not None and w < h:
        k = tw / w
        dr = ImageDraw.Draw(out)
        ox = cols * tw
        pts = [(ox + x * k, y * k) for x, y in zip(t["cx"][start:start + n], t["cy"][start:start + n])]
        held = t["held"][start:start + n]
        dr.line(pts, fill=(200, 200, 200), width=1)
        for (x, y), hd in zip(pts, held):
            dr.ellipse([x - 1.5, y - 1.5, x + 1.5, y + 1.5], fill=(255, 70, 70) if hd else (90, 220, 255))
        dr.line([(ox + x * k, y * k) for x, y in zip(t["hx"][start:start + n], t["hy"][start:start + n])], fill=(120, 255, 120), width=2)
        hit = int(t["hit"][start:start + n].sum())
        dr.text((ox + 4, 4), f"{take} {start}+{n} hit {hit}", fill=(255, 255, 0))
    os.makedirs(os.path.join(d, "peek"), exist_ok=True)
    p = os.path.join(d, "peek", f"{take}_{start}.jpg")
    out.save(p, quality=85)
    return p


if __name__ == "__main__":
    a = sys.argv[2:]
    for i in range(0, len(a), 3):
        print(peek(sys.argv[1], a[i], int(a[i + 1]), int(a[i + 2])))
