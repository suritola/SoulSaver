"""완성 mp4 검사(§G '그림·소리 맞음'): 규격(ffprobe) · 음량 · 번쩍 프레임과 딸깍 소리의 어긋남."""
import json
import os
import subprocess

import numpy as np

import audio
from common import FPS, SR, SPF, ffmpeg, run


def probe(mp4):
    out = subprocess.run([ffmpeg("ffprobe"), "-v", "error", "-show_entries",
                          "stream=codec_type,r_frame_rate,avg_frame_rate,nb_frames,start_time,duration", "-of", "json", mp4],
                         stdout=subprocess.PIPE, check=True).stdout
    return {s["codec_type"]: s for s in json.loads(out)["streams"]}


def flashes(mp4):
    """밝기가 튀는 프레임 번호(완성 파일을 다시 풀어서 센다)."""
    raw = run(["-i", mp4, "-an", "-vf", "scale=32:18,format=gray", "-f", "rawvideo", "-"], stdout=subprocess.PIPE).stdout
    y = np.frombuffer(raw, np.uint8).reshape(-1, 32 * 18).mean(axis=1)
    return np.flatnonzero(y > (y.min() + y.max()) / 2)


def onsets(mp4):
    """딸깍 소리가 시작하는 샘플 번호(봉우리의 절반을 처음 넘는 곳, 0.25초 안의 되풀이는 한 번으로)."""
    raw = run(["-i", mp4, "-vn", "-f", "f32le", "-ac", "1", "-ar", SR, "-"], stdout=subprocess.PIPE).stdout
    a = np.abs(np.frombuffer(raw, np.float32))
    idx = np.flatnonzero(a > a.max() / 2)
    return idx[np.insert(np.diff(idx) > SR // 4, 0, True)]


def build(tl, mp4, fps=FPS, sync=None):
    """sync = 번쩍·딸깍이 있어야 할 타임라인 프레임 목록(맞춤 시험). 돌려주는 값: 전부 통과했는가."""
    s, results = probe(mp4), []

    def check(name, ok, detail):
        results.append(ok)
        print(f"[검사] {'통과' if ok else '실패'} · {name} · {detail}")

    v, a = s["video"], s["audio"]
    want = tl.total * fps // FPS
    check("고정 프레임률", v["r_frame_rate"] == v["avg_frame_rate"] == f"{fps}/1", f"r={v['r_frame_rate']} avg={v['avg_frame_rate']}")
    check("프레임 수", int(v["nb_frames"]) == want, f"{v['nb_frames']} / 타임라인 {want}")
    gap = abs(float(v["duration"]) - float(a["duration"])) * 1000
    check("그림·소리 길이 차이 < 25ms", gap < 25, f"{gap:.2f}ms (그림 {v['duration']}s, 소리 {a['duration']}s)")
    check("두 줄기 시작 시각 0", float(v["start_time"]) == 0 and float(a["start_time"]) == 0, f"그림 {v['start_time']} 소리 {a['start_time']}")
    i, tp = audio.measure(mp4)
    check("음량 -14±0.5 LUFS · 참피크 ≤ -1 dBTP", abs(i + 14) <= 0.5 and tp <= -1.0, f"{i:.2f} LUFS, {tp:.2f} dBTP")

    if sync is not None and fps == FPS:
        fl, on = flashes(mp4), onsets(mp4)
        check("번쩍 프레임 = 기록한 프레임", list(fl) == list(sync), f"{len(fl)}개 / 기대 {len(sync)}개")
        check("딸깍 수 = 번쩍 수", len(on) == len(fl), f"딸깍 {len(on)} / 번쩍 {len(fl)}")
        if len(on) == len(fl) and len(fl):
            off = on - fl * SPF  # 샘플. +면 소리가 늦다
            for name, k in (("처음", 0), ("가운데", len(off) // 2), ("끝", len(off) - 1)):
                print(f"[맞춤] {name}: 프레임 {fl[k]} ({fl[k] / FPS:.2f}s) · 번쩍 샘플 {fl[k] * SPF} · 딸깍 샘플 {on[k]}"
                      f" · 차이 {off[k]:+d}샘플 = {off[k] / SR * 1000:+.3f}ms = {off[k] / SPF:+.4f}프레임")
            worst = int(np.abs(off).max())
            print(f"[맞춤] 전체 {len(off)}곳: 최대 |차이| {worst}샘플 = {worst / SR * 1000:.3f}ms · 끝-처음(밀림) {int(off[-1] - off[0]):+d}샘플")
            check("어긋남 ≤ 1프레임(800샘플)", worst <= SPF, f"최대 {worst}샘플")
    ok = all(results)
    print(f"[검사] {'전부 통과' if ok else '실패 있음'} · {os.path.basename(mp4)}")
    return ok
