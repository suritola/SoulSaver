#!/usr/bin/env python
"""완성 영상의 장면 요약 한 장(처음 4초 · 토막별 · 마지막 8초): py -3.14 tools/trailer/post/qc.py <mp4> [<mp4> …] → <mp4 옆>/qc_<이름>.jpg"""
import io
import os
import subprocess
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw  # noqa: E402

from common import ffmpeg  # noqa: E402

TIMES = [0.0, 0.3, 1.0, 2.5, 3.5, 5, 7.5, 9.5, 11.5, 13, 14.5, 16, 17.5, 19.5, 21, 22.5, 24, 25.5, 27, 28.5,
         30, 31.5, 33, 35, 37.5, 39.5, 41, 42.5, 44, 46, 47.5, 49, 50.5, 52.3, 52.9, 53.5, 54.7, 55.2, 56.5, 57.8]
for mp4 in sys.argv[1:]:
    out = Image.new("RGB", (270 * 10, 480 * 4))
    d = ImageDraw.Draw(out)
    for i, t in enumerate(TIMES):
        raw = subprocess.run([ffmpeg(), "-hide_banner", "-loglevel", "error", "-ss", str(t), "-i", mp4, "-frames:v", "1", "-vf", "scale=270:480",
                              "-f", "image2pipe", "-c:v", "png", "-"], stdout=subprocess.PIPE, check=True).stdout
        x, y = i % 10 * 270, i // 10 * 480
        out.paste(Image.open(io.BytesIO(raw)).convert("RGB"), (x, y))
        d.rectangle([x, y, x + 58, y + 16], fill=(0, 0, 0))
        d.text((x + 4, y + 2), f"{t}s", fill=(255, 255, 0))
    p = os.path.join(os.path.dirname(mp4), "qc_" + os.path.splitext(os.path.basename(mp4))[0] + ".jpg")
    out.save(p, quality=85)
    print(p)
