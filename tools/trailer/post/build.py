#!/usr/bin/env python
"""릴스 후반 작업 실행기. 사용법은 README.md.

  py -3.14 tools/trailer/post/build.py all --tag reels --lang ko
  py -3.14 tools/trailer/post/build.py audio|text|video|verify --tag reels --lang en
"""
import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.stdout.reconfigure(encoding="utf-8")  # Windows 콘솔 기본(cp949)에서 한글이 깨지지 않게
import assemble  # noqa: E402
import audio  # noqa: E402
import cards  # noqa: E402
import verify  # noqa: E402
from common import FPS, Timeline  # noqa: E402

p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
p.add_argument("step", choices=["audio", "text", "video", "verify", "all"])
p.add_argument("--timeline", default=os.path.join(os.path.dirname(os.path.abspath(__file__)), "timeline.json"))
p.add_argument("--tag", default="reels", help="판(타임라인 cuts의 이름)")
p.add_argument("--lang", default="ko")
p.add_argument("--fps", type=int, default=FPS, choices=[60, 30], help="30 = 인스타용 사본(한 프레임씩 거른다)")
p.add_argument("--preset", default="medium", help="libx264 preset")
a = p.parse_args()

tl = Timeline(a.timeline, a.tag, a.lang)
if a.step in ("audio", "all"):
    audio.build(tl)
if a.step in ("text", "all"):
    cards.build(tl)
if a.step in ("video", "all"):
    assemble.build(tl, a.fps, a.preset)
if a.step in ("verify", "all"):
    sys.exit(0 if verify.build(tl, tl.final(a.fps), a.fps) else 1)
