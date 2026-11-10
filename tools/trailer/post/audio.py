"""소리 띠 조립(§B-6): 컷의 cues.jsonl + 타임라인의 music·vo → work/<판>_<언어>/mix.wav.

프레임 N의 소리는 정확히 샘플 N×800에서 시작하고, 길이는 정확히 총 프레임×800 샘플이다.
섞기·덕킹·리미터를 numpy로 하는 까닭: ffmpeg alimiter는 기본값에서 attack만큼 소리를 늦춘다(latency=false).
"""
import json
import math
import os
import struct
import subprocess
import sys

import numpy as np

from common import ROOT, SR, SPF, run

# 기준 음량(dB, §B-6 3번)과 덕킹. 타임라인의 "mix"로 덮어쓴다.
MIX = {"music": -10, "amb": -18, "sfx": -8, "vo": 0,
       "duckDb": -7, "duckThreshold": 0.03, "attackMs": 20, "releaseMs": 600}
SFX_FADE = SR * 20 // 1000    # 컷 경계에서 잘리는 효과음 20ms 페이드
AMB_XFADE = SR * 400 // 1000  # 환경음 컷 사이 400ms 교차 페이드
TARGET_I, TARGET_TP, CEIL = -14.0, -1.0, 0.89

_cache = {}


def db(x):
    return 10 ** (x / 20)


def decode(path, af=None):
    """아무 소리 파일 → 48kHz 스테레오 float32 (n,2)."""
    if (path, af) not in _cache:
        raw = run(["-i", path] + (["-af", af] if af else []) + ["-vn", "-f", "f32le", "-ac", "2", "-ar", SR, "-"],
                  stdout=subprocess.PIPE).stdout
        _cache[(path, af)] = np.frombuffer(raw, np.float32).reshape(-1, 2)
    return _cache[(path, af)]


def find(tl, name):
    """소리 id → 파일. 영어 판이 한국어 음성으로 떨어지지 않게 음성 폴더는 언어별로만 본다."""
    dirs = tl.t.get("sounds", []) + [os.path.join(tl.shots, "sounds"), "Assets/Resources/Music", "Assets/Resources/Sounds", "Assets/Sounds"]
    for d in dirs:
        for ext in ("", ".wav", ".ogg", ".mp3"):
            p = os.path.join(ROOT, d, name + ext)
            if os.path.isfile(p):
                return p
    sys.exit(f"소리 '{name}'({tl.lang})를 찾지 못했다: {dirs}")


def ramp(k):
    return np.sin(np.linspace(0, math.pi / 2, k, dtype=np.float32))[:, None] ** 2


def place(buf, snd, pos, lo, hi, fade, gain=1.0):
    """snd를 샘플 pos에 더하되 [lo, hi) 밖은 자르고, 잘린 쪽에 fade 샘플 페이드를 건다."""
    a, b = max(pos, lo), min(pos + len(snd), hi)
    if b <= a:
        return
    chunk = snd[a - pos:b - pos] * np.float32(gain)
    k = min(fade, len(chunk))
    if k and a > pos:
        chunk[:k] *= ramp(k)
    if k and b < pos + len(snd):
        chunk[-k:] *= ramp(k)[::-1]
    buf[a:b] += chunk


def vo_clip(tl, v):
    """타임라인 vo 한 줄의 소리. "chars":[a,b]면 align.json의 글자 a 시작 -40ms ~ 글자 b 끝 +80ms만, 25ms 페이드(§F-2)."""
    path = find(tl, v["key"])
    snd = decode(path, v.get("af"))
    if "chars" in v:
        with open(os.path.splitext(path)[0] + ".align.json", encoding="utf-8") as f:
            al = json.load(f)
        a, b = tl.pick(v["chars"])   # 언어마다 글자 번호가 다르면 {"ko": [a, b], "en": [c, d]}
        snd = snd[max(0, int((al["start"][a] - 0.04) * SR)):int((al["end"][b] + 0.08) * SR)].copy()
    else:
        snd = snd.copy()
    k = min(SR * 25 // 1000, len(snd) // 2)   # 통째로 쓰는 줄도 25ms 페이드(mp3 끝의 짧은 튐이 딱 소리로 남지 않게)
    snd[:k] *= ramp(k)
    snd[-k:] *= ramp(k)[::-1]
    return snd


def duck_gain(vo, cfg):
    """목소리가 있는 동안 음악을 duckDb만큼 누르는 이득 곡선(10ms 블록, attack·release로 부드럽게)."""
    B = SR // 100
    nb = len(vo) // B
    blocks = vo[:nb * B].reshape(nb, B, 2)
    rms = np.sqrt(np.einsum("ijk,ijk->i", blocks, blocks) / (B * 2))
    ka, kr = math.exp(-10 / cfg["attackMs"]), math.exp(-10 / cfg["releaseMs"])
    g, cur = np.zeros(nb + 1, np.float32), 0.0
    for i, loud in enumerate(rms > cfg["duckThreshold"]):
        t = cfg["duckDb"] if loud else 0.0
        cur = t + (cur - t) * (ka if t < cur else kr)
        g[i + 1] = cur
    return (10 ** (expand(g, B, len(vo)) / 20))[:, None]


def expand(nodes, B, n):
    """블록 경계 값(nodes) 사이를 직선으로 이은 샘플 곡선(float32, 길이 n). 큰 float64 배열을 만들지 않는다."""
    nodes = nodes.astype(np.float32)
    lin = np.linspace(0, 1, B, endpoint=False, dtype=np.float32)
    out = (nodes[:-1, None] + (nodes[1:] - nodes[:-1])[:, None] * lin).ravel()
    return out[:n] if len(out) >= n else np.pad(out, (0, n - len(out)), mode="edge")


def limit(x, ceil):
    """앞을 내다보는 피크 리미터. 고정 길이(샘플 수가 변하지 않는다). 넘는 곳이 없으면 그대로 돌려준다."""
    B, w = SR // 1000, 6
    nb = -(-len(x) // B)
    pk = np.zeros(nb * B, np.float32)
    pk[:len(x)] = np.abs(x).max(axis=1)
    need = np.minimum(1.0, ceil / np.maximum(pk.reshape(nb, B).max(axis=1), 1e-9))
    if need.min() >= 1.0:
        return x
    view = np.lib.stride_tricks.sliding_window_view
    low = view(np.pad(need, w + 1, constant_values=1.0), 2 * w + 3).min(axis=1)        # ±(w+1) 블록 최솟값
    smooth = view(np.pad(low, w - 1, mode="edge"), 2 * w - 1).mean(axis=1)             # 그 안쪽 평균 → 항상 need 이하
    return x * expand(np.append(smooth, smooth[-1]), B, len(x))[:, None]


def write_wav(path, x):
    """32비트 float WAV. 표준 wave 모듈은 float를 못 쓴다."""
    x = np.ascontiguousarray(x, np.float32)
    ch = x.shape[1]
    with open(path, "wb") as f:
        f.write(b"RIFF" + struct.pack("<I", 36 + x.nbytes) + b"WAVEfmt " +
                struct.pack("<IHHIIHH", 16, 3, ch, SR, SR * ch * 4, ch * 4, 32) + b"data" + struct.pack("<I", x.nbytes))
        x.tofile(f)


def measure(path):
    """(통합 음량 LUFS, 참피크 dBTP) — ffmpeg loudnorm의 측정값."""
    err = run(["-i", path, "-vn", "-af", f"loudnorm=I={TARGET_I}:TP={TARGET_TP}:LRA=11:print_format=json", "-f", "null", "-"],
              loglevel="info", stderr=subprocess.PIPE).stderr.decode("utf-8", "replace")
    m = json.loads(err[err.rindex("{"):err.rindex("}") + 1])
    return float(m["input_i"]), float(m["input_tp"])


def build(tl):
    n = tl.total * SPF
    cfg = dict(MIX, **tl.t.get("mix", {}))
    music, vo, rest = (np.zeros((n, 2), np.float32) for _ in range(3))

    for c in tl.clips:
        cues = tl.source(c)[2]
        if not cues or not os.path.isfile(cues) or c.get("mute"):   # "mute": 그 컷의 소리 기록을 쓰지 않는다(콜드 오픈의 정적)
            continue
        with open(cues, encoding="utf-8") as f:
            rows = [json.loads(line) for line in f if line.strip()]
        lo, hi = c["start"] * SPF, (c["start"] + c["frames"]) * SPF
        at = lambda f: (c["start"] + f - c["in"]) * SPF  # 샘플 = (타임라인 시작 프레임 + f - 컷 안 시작 프레임) × 800
        for i, q in enumerate(rows):
            bus = q["bus"]
            # 게임 BGM 기록은 버린다. 컷의 voice는 화면 대화와 맞물리는 컷(keepVoice)만 살린다.
            if q.get("stop") or bus == "bgm" or (bus == "voice" and not c.get("keepVoice")):
                continue
            snd = decode(find(tl, q["clip"]))
            if not len(snd):
                continue
            pitch = q.get("pitch", 1.0)
            if pitch != 1.0:
                src = np.arange(int(len(snd) / pitch)) * pitch
                snd = np.stack([np.interp(src, np.arange(len(snd)), snd[:, k]) for k in (0, 1)], axis=1).astype(np.float32)
            a, b, fade = (lo - AMB_XFADE // 2, hi + AMB_XFADE // 2, AMB_XFADE) if bus == "amb" else (lo, hi, SFX_FADE)
            stop = next((s["f"] for s in rows[i + 1:] if s.get("stop") and s["bus"] == bus and s["clip"] == q["clip"]), None)
            if stop is not None:
                b = min(b, at(stop) + fade)
            pos = at(q["f"])
            if q.get("loop") and b > pos:
                snd = np.tile(snd, (-(-(b - pos) // len(snd)), 1))
            key = "vo" if bus == "voice" else bus
            place(vo if bus == "voice" else rest, snd, pos, max(a, 0), min(b, n), fade, db(cfg[key]) * q.get("vol", 1.0))

    for m in tl.rows("music"):
        at = tl.pick(m["at"])
        i0 = m.get("in", 0) * SPF
        snd = decode(find(tl, m["file"]), m.get("af"))
        need = tl.pick(m["frames"]) * SPF if "frames" in m else None
        if need and m.get("loop") and i0 + need > len(snd):   # 곡보다 길면 되풀이(게임에서 되풀이하는 곡)
            snd = np.concatenate([snd] * (-(-(i0 + need) // len(snd))))
        snd = snd[i0:i0 + need if need else None].copy()
        for key, sl in (("fadeIn", slice(None, None)), ("fadeOut", slice(None, None, -1))):
            k = min(m.get(key, 0) * SPF, len(snd))
            if k:
                snd[sl][:k] *= ramp(k)
        g = tl.pick(m.get("gainDb", 0))
        if isinstance(g, list):  # [[타임라인 프레임, dB], …] 사이를 dB 직선으로 잇는다
            curve = np.interp(at + np.arange(len(snd)) / SPF, [k[0] for k in g], [k[1] for k in g])
            snd *= (10 ** (curve / 20)).astype(np.float32)[:, None]
            g = 0
        place(music, snd, at * SPF, 0, n, 0, db(cfg["music"] + g))

    for v in tl.rows("vo"):
        place(vo, vo_clip(tl, v), tl.pick(v["at"]) * SPF, 0, n, 0, db(cfg["vo"] + v.get("gainDb", 0)))

    if vo.any():
        music *= duck_gain(vo, cfg)
    rest += vo
    rest += music
    del music, vo
    assert rest.shape == (n, 2), "소리 길이가 총 프레임 × 800이 아니다"

    # 음량 맞추기(§B-6 4번): 재고 → 고정 이득 + 리미터 → 다시 잰다. 고정 이득이라 샘플 수가 변하지 않는다.
    out = os.path.join(tl.work, "mix.wav")
    gain, ceil, i, tp = 0.0, CEIL, float("-inf"), 0.0
    for _ in range(4):
        write_wav(out, limit(rest * np.float32(db(gain)), ceil))
        i, tp = measure(out)
        if not math.isfinite(i) or (abs(i - TARGET_I) <= 0.3 and tp <= TARGET_TP):
            break
        if tp > TARGET_TP:
            ceil *= db(TARGET_TP - tp - 0.05)
        gain += TARGET_I - i
    print(f"[소리] {out} · {n}샘플 = {tl.total}프레임 × {SPF} · {i:.2f} LUFS · 참피크 {tp:.2f} dBTP · 이득 {gain:+.2f} dB")
    if not math.isfinite(i) or abs(i - TARGET_I) > 0.5 or tp > TARGET_TP:
        print("[소리] 경고: -14±0.5 LUFS · -1 dBTP에 닿지 못했다(소리가 너무 성기거나 비었다)")
    return out
