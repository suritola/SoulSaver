"""글자 판·로고·세로 판 자막·srt(§B-5) → work/<판>_<언어>/text/<번호>/%04d.png(알파) + manifest.json.

글꼴은 게임 그대로: 한글 MaruBuri-Regular, 영문 SourceSerif4-Italic, 자막은 MaruBuri.
자간 넓게, 그림자 대신 뒤에 옅은 먹 번짐, 움직임은 0.6초 뜨고 0.5초 사라지기뿐(inOutCubic).
"""
import json
import os
import shutil
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

import audio
from common import ROOT, SPF

FONTS = os.path.join(ROOT, "Assets", "TextMesh Pro", "Fonts")
HANJI, INK = (250, 240, 214), (10, 8, 14)  # 게임 글자의 상아색 · 어두운 번짐
SIZE = {"center": 0.05, "top": 0.05, "bl": 0.03, "br": 0.03, "sub": 0.048}  # 글자 높이 / 화면 짧은 변
SUB_MAX = {"ko": 16, "en": 34}  # 자막 한 줄 최대 글자 수


def ease(t):
    t = max(0.0, min(1.0, t))
    return 4 * t ** 3 if t < 0.5 else 1 - (-2 * t + 2) ** 3 / 2


def safe_box(w, h):
    """세로 판: 틱톡·릴스·쇼츠 공통 안전 영역 가운데 900×1400(§A-3). 가로 판: 가장자리 5% 안쪽."""
    if h > w:
        return w * 90 / 1080, h * 260 / 1920, w * 990 / 1080, h * 1660 / 1920
    return w * 0.05, h * 0.05, w * 0.95, h * 0.95


def text_image(text, lang, px, sub=False, dark=False):
    """여러 줄 글자 → (RGBA 그림, 여백). dark=True는 밝은 종이 위 먹색 글자."""
    name = "Cafe24Danjunghae-v2.0 1.ttf"   # 게임 글꼴 그대로(영문도)
    try:
        fnt = ImageFont.truetype(os.path.join(FONTS, name), px)
    except OSError:
        sys.exit(f"글꼴 {name}을 읽지 못했다")
    track = px * (0.02 if sub else 0.03 if lang == "en" else 0.05)
    lines = text.split("\n")
    # 글자를 하나씩 놓되 커닝은 살린다: 앞으로 가는 폭 = (이 글자+다음 글자 폭) - 다음 글자 폭 + 자간
    advs = [[fnt.getlength(s[i:i + 2]) - fnt.getlength(s[i + 1:i + 2]) + track for i in range(len(s))] for s in lines]
    widths = [sum(a) - track for a in advs]
    pad, lh = int(px * 0.9), int(px * 1.5)
    size = (int(max(widths)) + pad * 2, lh * len(lines) + pad * 2)
    mask = Image.new("L", size, 0)
    d = ImageDraw.Draw(mask)
    for row, (s, adv, wd) in enumerate(zip(lines, advs, widths)):
        x = (size[0] - wd) / 2
        for ch, a in zip(s, adv):
            d.text((x, pad + row * lh), ch, font=fnt, fill=255)
            x += a
    fg, bg = (INK, HANJI) if dark else (HANJI, INK)
    smear = Image.new("RGBA", size, bg + (0,))
    k = 6.0 if sub else 2.6   # 자막은 밝은 종이(편지) 위에도 얹히므로 번짐을 짙게
    smear.putalpha(mask.filter(ImageFilter.GaussianBlur(px * (0.42 if sub else 0.3))).point(lambda v: min(255, int(v * k))))
    top = Image.new("RGBA", size, fg + (0,))
    top.putalpha(mask)
    return Image.alpha_composite(smear, top), pad, max(widths)


def chunk(text, limit):
    """자막을 한 줄 limit자 이하 토막으로(낱말 경계). 한 낱말이 limit보다 길면 그대로 둔다."""
    out = []
    for word in text.split():
        if out and len(out[-1]) + 1 + len(word) <= limit:
            out[-1] += " " + word
        else:
            out.append(word)
    return out


def vo_text(tl, v):
    """vo 줄의 글자: 줄에 적힌 text → 타임라인 옆 narration.json[키]."""
    if "text" in v:
        return v["text"].get(tl.lang)
    p = os.path.join(tl.dir, "narration.json")
    if os.path.isfile(p):
        with open(p, encoding="utf-8") as f:
            return json.load(f).get(v["key"], {}).get(tl.lang)
    return None


def write_seq(img, d, frames, fin, fout):
    """프레임마다 한 장. 다 뜬 구간은 같은 그림이라 하드 링크로 채운다."""
    os.makedirs(d)
    alpha, hold = img.getchannel("A"), None
    for i in range(frames):
        a = min(ease(i / fin) if fin else 1.0, ease((frames - 1 - i) / fout) if fout else 1.0)
        p = os.path.join(d, f"{i:04d}.png")
        if a >= 1.0 and hold:
            try:
                os.link(hold, p)
            except OSError:
                shutil.copyfile(hold, p)
            continue
        f = img.copy()
        f.putalpha(alpha.point(lambda v: int(v * a + 0.5)))
        f.save(p, compress_level=1)
        if a >= 1.0:
            hold = p


def build(tl):
    out = os.path.join(tl.work, "text")
    shutil.rmtree(out, ignore_errors=True)
    os.makedirs(out)
    w, h = tl.w, tl.h
    x0, y0, x1, y1 = safe_box(w, h)
    rows = [dict(r, at=tl.pick(r["at"]), frames=tl.pick(r["frames"])) for r in tl.rows("text")]

    subs, srt = tl.cut.get("subs"), []
    for v in tl.rows("vo"):
        text = vo_text(tl, v)
        if not text:
            continue
        at, n = tl.pick(v["at"]), max(1, round(len(audio.vo_clip(tl, v)) / SPF))
        srt.append((at, at + n, text))
        if subs == "burn":
            # shortcut: 토막 시각을 글자 수 비례로 나눈다. 어긋나 보이면 align.json의 글자 시각으로 바꾼다.
            parts, f = chunk(text, SUB_MAX.get(tl.lang, 34)), at
            for k, part in enumerate(parts):
                end = at + round(n * sum(len(p) for p in parts[:k + 1]) / sum(len(p) for p in parts))
                rows.append({"at": f, "frames": max(end - f, 1), "pos": "sub", tl.lang: part, "fadeIn": 6, "fadeOut": 6})
                f = end
    if subs == "srt":
        os.makedirs(os.path.join(tl.out, "srt"), exist_ok=True)
        stamp = lambda f: "%02d:%02d:%02d,%03d" % (f // 216000, f // 3600 % 60, f // 60 % 60, f % 60 * 1000 // 60)
        with open(os.path.join(tl.out, "srt", f"{tl.tag}_{tl.lang}.srt"), "w", encoding="utf-8") as f:
            f.write("".join(f"{i + 1}\n{stamp(a)} --> {stamp(b)}\n{t}\n\n" for i, (a, b, t) in enumerate(srt)))

    manifest = []
    for i, r in enumerate(rows):
        pos = r.get("pos", "center")
        if "image" not in r and not r.get(tl.lang):
            continue   # 이 언어에는 없는 줄(한글 로고에는 '천천히 와'가 이미 있다)
        if "image" in r:  # 로고: {"image": {"ko": 경로, "en": 경로}, "h": 화면 높이 비율}
            src = r["image"][tl.lang] if isinstance(r["image"], dict) else r["image"]
            img = Image.open(os.path.join(ROOT, src)).convert("RGBA")
            ih = round(h * r.get("h", 0.14))
            img, pad = img.resize((round(img.width * ih / img.height), ih), Image.NEAREST if r.get("pixel") else Image.LANCZOS), 0
        else:
            px = round(min(w, h) * r.get("size", SIZE[pos]))
            while True:  # 안전 영역 폭을 넘으면 줄인다
                img, pad, tw = text_image(r[tl.lang], tl.lang, px, sub=pos == "sub", dark=r.get("dark", False))
                if tw <= x1 - x0 or px <= 12:
                    break
                px = int(px * (x1 - x0) / tw)
        x = {"bl": x0 - pad, "br": x1 + pad - img.width}.get(pos, (w - img.width) / 2)
        y = {"top": y0 - pad, "bl": y1 + pad - img.height, "br": y1 + pad - img.height,
             "sub": (h * 1120 / 1920 if h > w else h * 0.88) - img.height / 2}.get(pos, (h - img.height) / 2)
        if "y" in r:  # 가운데 높이를 화면 높이 비율로 직접(제목 아래 작은 줄)
            y = h * tl.pick(r["y"]) - img.height / 2
        d = os.path.join(out, f"{i:03d}")
        write_seq(img, d, r["frames"], r.get("fadeIn", 36), r.get("fadeOut", 30))
        manifest.append({"dir": d, "at": r["at"], "frames": r["frames"], "x": round(x), "y": round(y)})
    with open(os.path.join(out, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=1)
    print(f"[글자] {out} · 판 {len(manifest)}장")
    return manifest


assert chunk("가나 다라 마바사", 5) == ["가나 다라", "마바사"]
