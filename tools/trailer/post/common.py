"""릴스 후반 작업 공용: 경로, ffmpeg 찾기, 타임라인 읽기. Deepfold Tools/trailer 에서 가져와 Soul Saver 에 맞춤(2026-11-11).

시계는 프레임 번호 하나다. 60fps × 48,000Hz → 한 프레임 = 정확히 800샘플.
"""
import glob
import json
import os
import shutil
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
FPS, SR, SPF = 60, 48000, 800
THREADS = os.environ.get("SS_FFMPEG_THREADS", "4")


def ffmpeg(tool="ffmpeg"):
    """DF_FFMPEG 환경 변수 → PATH → winget(Gyan.FFmpeg) 설치 자리 순서로 찾는다."""
    exe = os.environ.get("SS_FFMPEG") or shutil.which("ffmpeg")
    if not exe:
        hits = sorted(glob.glob(os.path.join(os.environ.get("LOCALAPPDATA", ""),
                                             "Microsoft/WinGet/Packages/Gyan.FFmpeg*/*/bin/ffmpeg.exe")))
        if not hits:
            sys.exit("ffmpeg를 찾지 못했다. winget install Gyan.FFmpeg 뒤 다시 하거나 SS_FFMPEG에 ffmpeg.exe 경로를 넣는다.")
        exe = hits[-1]
    return exe if tool == "ffmpeg" else os.path.join(os.path.dirname(exe), tool + os.path.splitext(exe)[1])


def run(args, loglevel="error", **kw):
    cmd = [ffmpeg(), "-hide_banner", "-nostdin", "-loglevel", loglevel, "-y"] + [str(a) for a in args]
    p = subprocess.run(cmd, **kw)
    if p.returncode:
        sys.exit(f"ffmpeg 실패({p.returncode}): {' '.join(cmd)}")
    return p


def rel(p):
    return p if os.path.isabs(p) else os.path.join(ROOT, p)


class Timeline:
    """타임라인 JSON에서 꼬리표(tag) 하나·언어 하나의 판을 뽑는다. 형식은 README.md."""

    def __init__(self, path, tag, lang):
        with open(path, encoding="utf-8") as f:
            self.t = json.load(f)
        if tag not in self.t["cuts"]:
            sys.exit(f"타임라인에 판 '{tag}'이 없다: {sorted(self.t['cuts'])}")
        self.dir = os.path.dirname(os.path.abspath(path))
        self.tag, self.lang = tag, lang
        self.cut = self.t["cuts"][tag]
        self.w, self.h = self.cut["w"], self.cut["h"]
        self.out = rel(self.t.get("out", "docs/marketing/trailer"))
        self.work = os.path.join(self.out, "work", f"{tag}_{lang}")
        self.shots = rel(self.t.get("shots", os.path.join(self.out, "work", "shots")))
        self.ink = self.t.get("ink", "0B0A10")  # 페이드 색
        self.clips, start = [], 0
        for c in self.rows("clips"):
            c = dict(c, start=start, frames=self.pick(c["frames"]), **{"in": self.pick(c.get("in", 0))})
            start += c["frames"]
            self.clips.append(c)
        self.total = start
        os.makedirs(self.work, exist_ok=True)

    def rows(self, name):
        return [r for r in self.t.get(name, []) if self.tag in r["tags"]]

    def pick(self, v):
        """판마다 다른 값은 {"reels": 480, "hook": 360}, 언어마다 다른 값은 {"ko": 81, "en": 60}처럼 적는다(겹쳐 써도 된다)."""
        while isinstance(v, dict) and (self.tag in v or self.lang in v):
            v = v[self.tag] if self.tag in v else v[self.lang]
        return v

    def source(self, clip):
        """(종류, 경로, 소리 기록 경로). 종류: video(<src>.mkv) · png(<src>/000000.png…) · still(<src>.png) · color."""
        if "color" in clip:
            return "color", clip["color"], None
        base = os.path.join(self.shots, clip["src"].format(lang=self.lang, tag=self.tag))
        cues = base + ".cues.jsonl"
        if os.path.isdir(base):
            return "png", base, cues
        for ext, kind in ((".mkv", "video"), (".png", "still")):
            if os.path.isfile(base + ext):
                return kind, base + ext, cues
        sys.exit(f"컷 원본이 없다: {base}(.mkv | 폴더 | .png)")

    def final(self, fps=FPS):
        d = os.path.join(self.out, "final")
        os.makedirs(d, exist_ok=True)
        return os.path.join(d, f"soulsaver_{self.tag}_{self.lang}_{min(self.w, self.h)}p{fps}.mp4")
