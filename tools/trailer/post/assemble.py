"""그림 조립 + 소리 합치기(§B-2·B-5·B-6 5번): 컷 원본 → 자르기·맞춤 → 이어 붙이기 → 보정 → 글자 얹기 → 고정 60fps mp4.

컷 원본의 약속: 프레임 f는 mkv의 f번째 프레임 또는 <폴더>/00000f.png, YUV 원본은 BT.709.
"""
import json
import os
import sys

from common import FPS, THREADS, run

GRADE = ""  # 도트 그림은 보정 없이 그대로. 판의 "grade"로 덮어쓴다


def build(tl, fps=FPS, preset="slow"):
    W, H = tl.w, tl.h
    inputs, graph = [], []

    def add(*args):
        inputs.append(args)
        return len(inputs) - 1

    for i, c in enumerate(tl.clips):
        kind, path, _ = tl.source(c)
        a, n = c["in"], c["frames"]
        trim = f"trim=end_frame={n}"
        if kind == "video":
            k, trim = add("-i", path), f"trim=start_frame={a}:end_frame={a + n}"
        elif kind == "png":
            last = os.path.join(path, f"{a + n - 1:06d}.png")
            if not os.path.isfile(last):
                sys.exit(f"컷 프레임이 모자란다: {last}")
            k = add("-framerate", FPS, "-start_number", a, "-i", os.path.join(path, "%06d.png"))
        elif kind == "still":
            k = add("-loop", "1", "-framerate", FPS, "-i", path)
        else:
            k = add("-f", "lavfi", "-i", f"color=c=0x{path}:s={W}x{H}:r={FPS},format=gbrp")
        f = [trim, "setpts=PTS-STARTPTS"]
        if "crop" in c:  # [w, h, x, y] 원본 픽셀(세로 판: 가로 4K에서 1215×2160)
            cw, ch, cx, cy = tl.pick(c["crop"])
            if abs(cw / ch - W / H) > 0.01:
                sys.exit(f"{c.get('src')}: crop {cw}×{ch}의 비율이 판 {W}×{H}와 다르다")
            f.append(f"crop={cw}:{ch}:{cx}:{cy}")
        # 도트 그림: 크기가 다를 때만 최근접 보간으로 맞춘다(같은 크기면 scale 은 아무것도 하지 않는다)
        f += [f"scale={W}:{H}:flags=neighbor:in_color_matrix=bt709:out_color_matrix=bt709:out_range=tv", "setsar=1", "format=yuv444p"]
        if c.get("fadeIn"):
            f.append(f"fade=t=in:s=0:n={c['fadeIn']}:c=0x{tl.ink}")
        if c.get("fadeOut"):
            f.append(f"fade=t=out:s={n - c['fadeOut']}:n={c['fadeOut']}:c=0x{tl.ink}")
        graph.append(f"[{k}:v]{','.join(f)}[c{i}]")

    graph.append("".join(f"[c{i}]" for i in range(len(tl.clips))) +
                 f"concat=n={len(tl.clips)}:v=1:a=0,{tl.cut.get('grade', GRADE) or 'null'}[v0]")
    manifest = os.path.join(tl.work, "text", "manifest.json")
    cards = json.load(open(manifest, encoding="utf-8")) if os.path.isfile(manifest) else []
    for j, t in enumerate(cards):
        k = add("-framerate", FPS, "-i", os.path.join(t["dir"], "%04d.png"))
        graph.append(f"[{k}:v]setpts=PTS+{t['at']}/{FPS}/TB[t{j}]")
        graph.append(f"[v{j}][t{j}]overlay={t['x']}:{t['y']}:eof_action=pass:format=yuv444[v{j + 1}]")
    graph.append(f"[v{len(cards)}]format=yuv420p{',fps=%d' % fps if fps != FPS else ''}[vout]")

    script = os.path.join(tl.work, "graph.txt")
    with open(script, "w", encoding="utf-8") as f:
        f.write(";\n".join(graph))
    mix, out = add("-i", os.path.join(tl.work, "mix.wav")), tl.final(fps)
    run([x for args in inputs for x in args] + [
        "-/filter_complex", script, "-map", "[vout]", "-map", f"{mix}:a", "-frames:v", tl.total * fps // FPS,
        "-c:v", "libx264", "-preset", preset, "-crf", tl.cut.get("crf", 15), "-profile:v", "high", "-pix_fmt", "yuv420p",
        "-r", fps, "-fps_mode", "cfr", "-g", fps // 2, "-bf", 2, "-flags", "+cgop",
        "-color_primaries", "bt709", "-color_trc", "bt709", "-colorspace", "bt709", "-color_range", "tv",
        "-c:a", "aac", "-b:a", "384k", "-ar", 48000, "-movflags", "+faststart",
        "-threads", THREADS, "-filter_complex_threads", THREADS, out])
    print(f"[그림] {out} · {tl.total}프레임 · 컷 {len(tl.clips)} · 글자 판 {len(cards)}")
    return out
