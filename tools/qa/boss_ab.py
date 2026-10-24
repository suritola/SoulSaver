"""보스 판정 · 그림 전후 비교 (그래픽 가독성 개편 2단계 5-4 · 묶음 T)

QaCapture 의 boss_gallery 장면이 qa_log.txt 에 남긴 「판정 · 밀도 · 죽음 · 물건 · 도감카드」 줄을 기준(전) · 새(후) 두 번 읽어
  ① 판정 값(col.offset · col.radius · 닿기 · 몸 · 고정 상자 · 약점 중심 · 약점 반지름 · 종류) 차이 0
  ② 렌더러 경계(가운데 · 크기) · 머리 위 글자 y 차이 ≤ 0.2칸 (겉보기)
  ③ 그림이 바뀐(PPU 가 달라진) 보스의 1080p 화소/칸 4.0~4.3
  ④ 영웅의 1.5배 넘는 점 필터 렌더러가 1-8 표 목록(짧은 이펙트 등)뿐
을 표로 보여 주고, 보스마다 전 · 후 촬영을 잘라 붙인 그림 한 장(boss_ab.png)을 만든다.

사용법:
  py -3.14 -X utf8 tools/qa/boss_ab.py <기준 Qa 폴더 | qa_log.txt> <새 Qa 폴더 | qa_log.txt> [--out 그림.png] [--allow 정규식]... [--tol 0.001] [--bounds 0.2]
  (Qa 폴더는 qa.ps1 이 다음 실행 때 지우므로 qa_log.txt · bg_*.png 를 scratchpad 로 복사해 둔 것을 줌)
끝 코드: 0 = 모두 통과 · 1 = 실패 있음 · 2 = 줄 없음
표준 라이브러리 + Pillow(그림에만 · 없으면 건너뜀).
"""
import argparse
import glob
import os
import re
import sys

LINE = re.compile(r"^\[boss_gallery\] (판정|밀도|죽음|물건|도감카드) ?(\S*)\s*(.*)$")
BOSSES = ["lich", "hell", "slimeking", "mirror", "developer", "mom"]
EXACT = ["kind", "col.offset", "col.radius", "닿기", "몸", "상자", "약점", "약점반지름", "약점궤도", "약점종류"]
LOOSE = ["경계", "글자y"]
# 1-8 표: 2단계 뒤에도 남기로 한 어긋남 (보스 장면에 나올 수 있는 것) — 짧은 반짝 · 터짐 이펙트
ALLOW = [r"^fx_(sparkle|soulburst|spark|burst|explosion|hit|pop|flash|shock)", r"^Hit", r"^DamagePopup"]


def folder_log(path):
    if os.path.isdir(path):
        return os.path.join(path, "qa_log.txt"), path
    return path, os.path.dirname(os.path.abspath(path))


def toks(rest):
    d = {}
    for t in rest.split():
        k, _, v = t.partition("=")
        if k and _:
            d[k] = v
    return d


def nums(v):
    try:
        return [float(x) for x in v.split(",")]
    except (AttributeError, ValueError):
        return None


def read(path):
    log, folder = folder_log(path)
    if not os.path.isfile(log):
        sys.exit(f"qa_log.txt 없음: {log}  (먼저 pwsh tools/qa/qa.ps1 -Only boss_gallery)")
    g = dict(boss={}, dens={}, death={}, props={}, cards=[], folder=folder, log=log)
    with open(log, encoding="utf-8", errors="replace") as f:
        for raw in f:
            m = LINE.match(raw.rstrip("\n"))
            if not m:
                continue
            kind, name, rest = m.groups()
            if kind == "판정":
                g["boss"][name] = toks(rest)
            elif kind == "밀도":
                ent, meta = {}, {}
                for t in rest.split():
                    k, _, v = t.rpartition("=")
                    if not k:
                        continue
                    if k in ("영웅", "범위", "개수", "밖"):
                        meta[k] = v
                    else:
                        try:
                            ent[k] = float(v)
                        except ValueError:
                            pass
                g["dens"][name] = (meta, ent)
            elif kind == "죽음":
                g["death"][name] = toks(rest)
            elif kind == "물건":
                g["props"][name] = toks(rest)
            elif kind == "도감카드":
                c = toks(name + " " + rest)
                if "정수" in (name + " " + rest).split():         # 「정수」는 = 없는 낱말이라 toks 가 버림 → 표시로 남김
                    c["정수"] = "1"
                g["cards"].append(c)
    return g


def diff(a, b):
    va, vb = nums(a), nums(b)
    if va is None or vb is None or len(va) != len(vb):
        return None if a == b else float("inf")
    return max(abs(x - y) for x, y in zip(va, vb)) if va else 0.0


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("base")
    ap.add_argument("new")
    ap.add_argument("--out")
    ap.add_argument("--allow", action="append", default=[])
    ap.add_argument("--tol", type=float, default=0.001, help="판정 값 차이 0 의 허용 (로그가 소수 4자리)")
    ap.add_argument("--bounds", type=float, default=0.2, help="렌더러 경계 · 글자 y 허용 (칸)")
    a = ap.parse_args()
    base, new = read(a.base), read(a.new)
    if not new["boss"]:
        print("새 로그에 boss_gallery 「판정」 줄이 없음")
        return 2
    allow = [re.compile(p) for p in ALLOW + a.allow]
    fails = 0
    rows = []

    def put(boss, item, ok, msg):
        nonlocal fails
        fails += ok is False
        rows.append((boss, item, ok, msg))

    for name in BOSSES:
        nb, bb = new["boss"].get(name), base["boss"].get(name)
        if nb is None:
            put(name, "촬영", False, "새 로그에 없음 (장면 실패 · XX 줄 확인)")
            continue
        if bb is None:
            put(name, "촬영", None, "기준 로그에 없음 → 비교 건너뜀")
        else:
            # ① 판정 값 차이 0
            bad, same = [], 0
            for k in EXACT:
                if k not in nb and k not in bb:
                    continue
                # 기준이 못 읽은 칸(「-」 — 예: 묶음 1 전 상자는 지금 프레임 그림이 extents 에 없으면 비었음) → 비교할 값이 없음, 참고로만
                if bb.get(k) == "-" and nb.get(k) not in (None, "-"):
                    put(name, k, None, f"기준에 값 없음(-) → 새 {nb.get(k)} · 비교 건너뜀")
                    continue
                d = diff(nb.get(k), bb.get(k))
                if d is None or d <= a.tol:
                    same += 1
                else:
                    bad.append(f"{k} {bb.get(k)} → {nb.get(k)}")
            if nb.get("상자출처") != bb.get("상자출처"):
                put(name, "상자 출처", None, f"{bb.get('상자출처')} → {nb.get('상자출처')} (묶음 1 고정 표로 바뀌었으면 정상)")
            put(name, "판정 차이 0", not bad, f"{same}칸 같음" + (" · 다름: " + " · ".join(bad) if bad else ""))
            # ② 겉보기 경계 ≤ 0.2칸
            for k in LOOSE:
                if k not in nb or k not in bb:
                    continue
                d = diff(nb[k], bb[k])
                put(name, k, d is not None and d <= a.bounds + 1e-9, f"차이 {d:.3f}칸 (≤ {a.bounds}) · {bb[k]} → {nb[k]}" if d is not None and d != float("inf") else f"{bb[k]} → {nb[k]}")
        # ③ 밀도
        dens = nums(nb.get("밀도", ""))
        if dens:
            changed = bb is not None and nb.get("PPU") != bb.get("PPU")
            msg = f"1080p 화소/칸 {dens[0]:.1f}" + (f" (전 {bb.get('밀도')} · PPU {bb.get('PPU')} → {nb.get('PPU')})" if bb else "")
            if changed or bb is None:
                put(name, "밀도", 4.0 <= dens[0] <= 4.3, msg + " · 기준 4.0~4.3")
            else:
                put(name, "밀도", None, msg + " · 그림(PPU) 그대로 → 참고")
        # ④ 1.5배 넘는 렌더러
        meta, ent = new["dens"].get("bg_" + name, ({}, {}))
        if meta:
            try:
                hero = float(meta.get("영웅", "nan"))
            except ValueError:
                hero = float("nan")
            hi = [(k, v) for k, v in ent.items() if hero == hero and v > hero * 1.5]
            tex_new = nb.get("텍스처", "")
            boss_self = [(k, v) for k, v in hi if tex_new and f"[{tex_new}]" in k]
            others = [(k, v) for k, v in hi if (k, v) not in boss_self]
            unknown = [(k, v) for k, v in others if not any(p.search(k) for p in allow)]
            # 기준에도 같은 값으로 있던 것 = 이번에 바뀐 것이 아님 → 참고 (묶음 1 처럼 그림을 안 바꾼 비교가 실패하지 않게)
            bent = base["dens"].get("bg_" + name, ({}, {}))[1]
            kept = [(k, v) for k, v in unknown if k in bent and abs(bent[k] - v) < 0.05]
            unknown = [x for x in unknown if x not in kept]
            if kept:
                put(name, "1.5배 넘는 렌더러(그대로)", None, ", ".join(f"{k} {v:.1f}" for k, v in kept) + " — 기준에도 같은 값 · 그 그림을 바꿀 때 확인")
            if boss_self:
                same_art = bb is not None and nb.get("PPU") == bb.get("PPU")
                put(name, "보스 몸 밀도", None if same_art else False, ", ".join(f"{k} {v:.1f}" for k, v in boss_self) + (" (그림 교체 전 · 참고)" if same_art else " (그림을 바꿨는데 영웅 1.5배 넘음)"))
            listed = [(k, v) for k, v in others if any(p.search(k) for p in allow)]
            put(name, "1.5배 넘는 렌더러", not unknown, (f"1-8 목록 밖 (새로 생기거나 바뀜): " + ", ".join(f"{k} {v:.1f}" for k, v in unknown)) if unknown else
                f"새로 생긴 것 없음 · 1-8 목록 {len(listed)}종" + (": " + ", ".join(f"{k} {v:.1f}" for k, v in listed[:6]) if listed else ""))
        # 죽음 프레임
        dn, db = new["death"].get(name), base["death"].get(name)
        if dn and "그림" in dn:
            msg = f"죽음 0.4초 그림 {dn.get('그림')}" + (f" (전 {db.get('그림')})" if db else "")
            if db and "경계" in dn and "경계" in db:
                d = diff(dn["경계"], db["경계"])
                put(name, "죽음 경계", d is not None and d <= a.bounds + 1e-9, msg + f" · 경계 차이 {d:.3f}칸" if d is not None and d != float("inf") else msg)
            else:
                put(name, "죽음", None, msg)

    # 물건 줄 · 도감 카드
    for tag, p in new["props"].items():
        try:
            mul = float(p.get("영웅배", "nan"))
        except ValueError:
            mul = float("nan")
        bp = base["props"].get(tag)
        same_art = bp is not None and bp.get("그림") == p.get("그림") and bp.get("PPU") == p.get("PPU")
        ok = None if (mul != mul or same_art) else (0.5 <= mul <= 1.5 if p.get("필터") == "Point" else None)
        put("물건", tag, ok, f"밀도 {p.get('밀도')} (영웅 ×{mul:.2f}) · {p.get('그림')} PPU {p.get('PPU')} · 필터 {p.get('필터')}" + (f" (전 {bp.get('밀도')})" if bp else "")
            + ("" if ok is not None else " · 그림 그대로 → 참고" if same_art else " · 참고"))
    base_cards = {c.get("그림"): c for c in base["cards"]}
    for c in new["cards"]:
        if "그림" in c:
            bc = base_cards.get(c.get("그림"))
            same = bc is not None and bc.get("배") == c.get("배")
            put("도감", c.get("그림"), None if same and "정수" not in c else "정수" in c,
                f"그림 {c.get('그림화소')} → 칸 {c.get('칸')} = ×{c.get('배')}" + (" (기준과 같음 · 묶음 2 전 → 참고)" if same and "정수" not in c else ""))

    # 출력
    print(f"# boss_ab — 기준 {base['log']}\n#          새   {new['log']}\n")
    w = max(len(r[1]) for r in rows) if rows else 8
    for boss, item, ok, msg in rows:
        mark = "통과" if ok is True else "실패" if ok is False else "참고"
        print(f"[{mark}] {boss:<10} {item:<{w}}  {msg}")
    npass = sum(1 for r in rows if r[2] is True)
    print(f"\n통과 {npass} · 실패 {fails} · 참고 {sum(1 for r in rows if r[2] is None)}")
    sheet(base, new, a.out or os.path.join(new["folder"], "boss_ab.png"))
    return 0 if fails == 0 else 1


def find_png(folder, shot):
    hits = sorted(glob.glob(os.path.join(folder, f"*_{shot}.png")))
    return hits[-1] if hits else None


def sheet(base, new, out):
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        print("Pillow 없음 → 합친 그림 건너뜀")
        return
    cells = []
    for name in BOSSES:
        for suffix, rec in (("", "boss"), ("_death", "death")):
            row = []
            for g in (base, new):
                d = g[rec].get(name)
                png = find_png(g["folder"], "bg_" + name + suffix)
                if not d or not png or "화면" not in d:
                    row.append(None)
                    continue
                x0, y0, x1, y1 = (int(v) for v in d["화면"].split(","))
                im = Image.open(png).convert("RGB")
                pad = 30
                row.append(im.crop((max(0, x0 - pad), max(0, y0 - pad), min(im.width, x1 + pad), min(im.height, y1 + pad))))
            if any(row):
                cells.append((name + suffix, row))
    for shot in ("bg_props", "bg_codex_bosses"):
        row = []
        for g in (base, new):
            png = find_png(g["folder"], shot)
            if png:
                im = Image.open(png).convert("RGB")
                row.append(im.resize((im.width // 2, im.height // 2)))
            else:
                row.append(None)
        if any(row):
            cells.append((shot, row))
    if not cells:
        print("촬영 PNG 없음 → 합친 그림 건너뜀 (qa_log 만 복사했으면 정상)")
        return
    colw = [max((r[1][i].width for r in cells if r[1][i] is not None), default=200) for i in range(2)]
    label_h = 22
    H = sum(label_h + max((c.height for c in r[1] if c is not None), default=40) for r in cells)
    W = colw[0] + colw[1] + 30
    canvas = Image.new("RGB", (W, H + label_h), (24, 20, 30))
    dr = ImageDraw.Draw(canvas)
    dr.text((10, 4), "BASE", fill=(230, 230, 230))
    dr.text((colw[0] + 20, 4), "NEW", fill=(230, 230, 230))
    y = label_h
    for label, row in cells:
        dr.text((10, y + 4), label, fill=(255, 220, 120))
        y += label_h
        hmax = 0
        x = 10
        for i, c in enumerate(row):
            if c is not None:
                canvas.paste(c, (x, y))
                hmax = max(hmax, c.height)
            else:
                dr.text((x + 8, y + 8), "(none)", fill=(150, 150, 150))
                hmax = max(hmax, 40)
            x += colw[i] + 10
        y += hmax
    canvas.save(out)
    print(f"합친 그림: {out} ({W}×{H + label_h})")


if __name__ == "__main__":
    sys.exit(main())
