"""내보내기 (2-2 단계 8): 쓰는 칸만 시트로 묶고 .meta 를 다시 쓴다.

.meta 규칙 (5-2): guid · 쓰는 그림의 이름 · internalID · spriteID · nameFileIdTable 그대로, rect 만 새로.
안 쓰는 그림은 참조(guid + fileID)가 0 일 때만 지움 — 아니면 멈춤 (새 텍스처 밖 rect 는 가져오기 오류라 남길 수 없음).
PPU = 칸 · spriteExtrude = round(k) · Tight · Point · 무압축 · 밉맵 끔 · 가운데 피벗(alignment 0).
"""
import glob
import os
import re

import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
REF_EXT = ("*.anim", "*.prefab", "*.unity", "*.asset", "*.controller", "*.mat", "*.overrideController")


def pack(frames, cols):
    """칸 목록 → 시트 (가로로 먼저 · 위에서부터)"""
    ch, cw = frames[0].shape[:2]
    rows = (len(frames) + cols - 1) // cols
    sheet = np.zeros((rows * ch, cols * cw, 4), np.uint8)
    for i, f in enumerate(frames):
        r, c = divmod(i, cols)
        sheet[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw] = f
    return sheet


def save_png(path, rgba):
    Image.fromarray(rgba, "RGBA").save(path, optimize=True)


def references(guid):
    """이 guid 를 참조하는 fileID 집합 (에셋 텍스트 전부)"""
    ids = set()
    pat = re.compile(r"fileID: (-?\d+), guid: " + guid)
    for ext in REF_EXT:
        for f in glob.glob(os.path.join(ROOT, "Assets", "**", ext), recursive=True):
            try:
                with open(f, encoding="utf-8", errors="ignore") as fh:
                    t = fh.read()
            except OSError:
                continue
            if guid in t:
                ids.update(int(m) for m in pat.findall(t))
    return ids


def rewrite_meta(text, keep, cell, cols, sheet_h, ppu, extrude):
    """keep = 시트 칸 순서대로 남길 그림 이름. 나머지 그림 항목 · 이름표 줄은 지움"""
    head, sep, rest = text.partition("    sprites:\n")
    if not sep:
        raise SystemExit(".meta 에 spriteSheet.sprites 가 없음")
    body, sep2, tail = rest.partition("    outline: []\n    physicsShape: []\n    bones: []\n    spriteID:")
    blocks = re.split(r"(?m)^    - serializedVersion: 2\n(?=      name:)", body)
    blocks = [b for b in blocks if b.strip()]
    by_name = {}
    for b in blocks:
        m = re.match(r"      name: (\S+)\n", b)
        if m:
            by_name[m.group(1)] = b
    missing = [n for n in keep if n not in by_name]
    if missing:
        raise SystemExit(f".meta 에 없는 그림: {missing}")
    out = []
    for i, n in enumerate(keep):
        r, c = divmod(i, cols)
        b = by_name[n]
        b = re.sub(r"(      rect:\n        serializedVersion: 2\n)        x: [-\d.]+\n        y: [-\d.]+\n        width: [-\d.]+\n        height: [-\d.]+\n",
                   lambda m: m.group(1) + f"        x: {c * cell}\n        y: {sheet_h - (r + 1) * cell}\n        width: {cell}\n        height: {cell}\n", b)
        out.append("    - serializedVersion: 2\n" + b)
    tail = re.sub(r"(?m)^      (\S+): -?\d+\n", lambda m: m.group(0) if m.group(1) in keep else "", tail)
    head = re.sub(r"(?m)^  spritePixelsToUnits: [\d.]+$", f"  spritePixelsToUnits: {ppu}", head)
    head = re.sub(r"(?m)^  spriteExtrude: \d+$", f"  spriteExtrude: {extrude}", head)
    for key, val in (("filterMode", 0), ("enableMipMap", 0), ("spriteMeshType", 1), ("alignment", 0)):
        head = re.sub(rf"(?m)^(\s+){key}: \d+$", lambda m: f"{m.group(1)}{key}: {val}", head)
    head = re.sub(r"(?m)^(\s+)textureCompression: \d+$", lambda m: f"{m.group(1)}textureCompression: 0", head)
    return head + sep + "".join(out) + sep2 + tail


def check_unused(meta_text, guid, keep):
    """지울 그림 중 참조되는 것이 있으면 이름 목록 (있으면 지우면 안 됨)"""
    table = dict((n, int(v)) for n, v in re.findall(r"(?m)^      (\S+): (-?\d+)$", meta_text.split("nameFileIdTable:")[-1]))
    used = references(guid)
    return [n for n, fid in table.items() if n not in keep and fid in used], [n for n in keep if table.get(n) not in used]
