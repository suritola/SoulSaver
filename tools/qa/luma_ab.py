"""가독성 QA 판정 (그래픽 가독성 개편 1-K · 기획서 6-3)

QaCapture 의 readability 장면이 qa_log.txt 에 남긴 「명도 · 내이펙트 · 밀도」 줄을 읽어
장면별 흑백(Rec.601 Y, 0~255) 표 + 6-3 기준 통과 여부를 보여 준다. 폴더를 둘 주면 전 · 후 차이도.
read_desert 를 흑백 한 장(read_desert_gray.png)으로 저장한다 (PNG 는 이 한 장만 보면 됨).

사용법:
  py -3.14 -X utf8 tools/qa/luma_ab.py "%LOCALAPPDATA%\\SoulSaverBuild\\Qa"
  py -3.14 -X utf8 tools/qa/luma_ab.py <후 Qa 폴더> <전 Qa 폴더>
표준 라이브러리 + Pillow(흑백 저장에만 · 없으면 건너뜀).
"""
import glob
import os
import re
import sys

PLACES = ["desert", "meadow", "cave", "abyss"]
NAMES = {"desert": "무한 사막", "meadow": "초원 영토", "cave": "이야기 묘역", "abyss": "심연"}
LINE = re.compile(r"^\[readability\] (명도|내이펙트|밀도) (\S+)\s*(.*)$")


def num(v):
    try:
        return float(v)
    except (TypeError, ValueError):
        return None


def nums(v):
    if v is None or v == "-":
        return []
    return [x for x in (num(p) for p in v.split(",")) if x is not None]


def read_log(folder):
    path = os.path.join(folder, "qa_log.txt")
    if not os.path.isfile(path):
        sys.exit(f"qa_log.txt 없음: {path}  (먼저 pwsh tools/qa/qa.ps1 -Only readability)")
    shots = {}
    with open(path, encoding="utf-8", errors="replace") as f:
        for raw in f:
            m = LINE.match(raw.rstrip("\n"))
            if not m:
                continue
            kind, shot, rest = m.groups()
            d = shots.setdefault(shot, {})
            if kind == "밀도":
                toks = rest.split()
                dense = {}
                for t in toks:
                    k, _, v = t.rpartition("=")
                    if k == "개수":
                        d["밀도개수"] = num(v)
                    elif k:
                        dense[k] = num(v)
                d["밀도"] = dense
                continue
            for t in rest.split():
                k, _, v = t.partition("=")
                if k:
                    d[k] = v
    return shots


def hostile_hue(h):
    # 적대 대역 = 빨강 · 보라 (색각 모드 끔 = QA 기본값). 색각 모드면 주황 · 자홍 대역으로 따로 볼 것
    return h <= 30 or h >= 260


def judge(shots):
    """(기준 번호, 곳, 통과 True/False/None, 설명) 목록"""
    out = []
    for p in PLACES:
        c = shots.get(f"read_{p}_clean")
        e = shots.get(f"read_{p}")
        if not c and not e:
            out.append(("-", p, None, "촬영 없음 (장면 실패 · qa_log 의 XX 줄 확인)"))
            continue
        c = c or {}
        e = e or {}
        floor = num(c.get("바닥"))
        dev = num(c.get("바닥편차"))
        # 1 바닥
        if floor is not None and floor >= 0:
            out.append(("1", p, 28 <= floor <= 55 and (dev is not None and dev <= 8),
                        f"바닥 Y {floor:.1f} (28~55) · 8×8 편차 {dev:.1f} (≤8) · 점 {c.get('바닥점')}"))
        else:
            out.append(("1", p, None, "바닥 점을 못 찾음"))
        # 2 적 몸 (깨끗한 판 · 맞는 무리)
        body = num(c.get("적"))
        hit = num(e.get("맞는적"))
        if floor is not None and floor >= 0 and body is not None and body >= 0:
            out.append(("2", p, body >= floor + 35, f"적 몸 Y {body:.1f} ≥ 바닥+35 = {floor + 35:.1f} (적 {c.get('적수')}마리)"))
        if floor is not None and floor >= 0 and hit is not None:
            out.append(("2", p, hit >= floor + 35, f"맞는 무리 Y {hit:.1f} ≥ {floor + 35:.1f} (화염 지대 안 {e.get('맞는수')}마리)"))
        # 3 플레이어 띠
        band = num(c.get("띠"))
        p98 = num(c.get("화면p98"))
        if band is not None and band >= 0:
            out.append(("3", p, band >= 235 and (p98 is None or band >= p98),
                        f"플레이어 띠 Y {band:.1f} ≥ 235 · 화면 상위 2% {p98:.1f} 이상"))
        # 4 적 위험
        hues = nums(e.get("테색상"))
        rings = nums(e.get("흰고리Y"))
        if hues:
            bad = [f"{h:.0f}°" for h in hues if not hostile_hue(h)]
            out.append(("4", p, not bad, "경고 테 색상 " + ", ".join(f"{h:.0f}°" for h in hues) + (" · 대역 밖 " + ", ".join(bad) if bad else " (빨강 · 보라 대역)")))
        if rings:
            out.append(("4", p, min(rings) >= 200, "닫히는 흰 고리 Y " + ", ".join(f"{r:.0f}" for r in rings) + " (모두 ≥200)"))
        runes = num(e.get("룬", c.get("룬")))
        if runes is not None:
            out.append(("4", p, runes == 0, f"룬 렌더러 {runes:.0f}개 (0)"))
        # 5 내 이펙트
        diff = num(e.get("적차이"))
        if diff is not None:
            out.append(("5", p, diff <= 10, f"내 이펙트 있을 때 적 몸 Y 차이 {diff:.1f}% (≤10%)"))
        alphas = []
        for t in (e.get("효과층알파") or "-").split(","):
            n, _, a = t.rpartition(":")
            if n and num(a) is not None:
                alphas.append((n, num(a)))
        if alphas:
            top = max(alphas, key=lambda x: x[1])
            out.append(("5", p, top[1] <= 0.8 + 1e-3, f"적 위(Effect 층) fx 최대 알파 {top[1]:.2f} ({top[0]}) ≤ 0.8 — 적 것이 섞일 수 있어 이름 확인"))
        zl, el = e.get("화염층"), e.get("적층")
        if zl and el and zl != "-" and el != "-":
            def key(s):
                m = re.match(r"(.*)/(-?\d+)#(-?\d+)", s)
                return (int(m.group(3)), int(m.group(2))) if m else None
            kz, ke = key(zl), key(el)
            if kz and ke:
                out.append(("5", p, kz < ke, f"내 화염 지대 층 {zl} < 적 층 {el}"))
        booms = e.get("폭발층")
        if booms and booms != "-":
            out.append(("5", p, None, f"내 폭발 층 {booms} (참고: 1-B 뒤 GroundFx 면 적 아래)"))
        # 6 순서
        if floor is not None and body is not None and band is not None and floor >= 0 and body >= 0 and band >= 0:
            top_white = max(rings) if rings else None
            ok = floor < body < band and (top_white is None or band <= top_white + 1e-6)
            out.append(("6", p, ok, f"바닥 {floor:.0f} < 적 {body:.0f} < 띠 {band:.0f} ≤ 적 위험 흰 요소 {top_white if top_white is None else round(top_white)}"))
        # 7 화소 밀도 · 큰 충격파
        dense = {}
        for s in (c, e):
            dense.update(s.get("밀도") or {})
        out.append(("7", p, len(dense) == 0, "화면 12화소/칸 넘는 렌더러 " + (", ".join(f"{k} {v:.1f}" for k, v in sorted(dense.items(), key=lambda x: -(x[1] or 0))[:8]) if dense else "없음") + " (이 장면엔 보스 없음 → 0)"))
        shock = num(e.get("큰충격파"))
        if shock is not None:
            out.append(("7", p, shock == 0, f"화면 1/3 넘는 충격파 {shock:.0f}개 (0)"))
    return out


TABLE_KEYS = [("바닥", "바닥"), ("바닥편차", "편차"), ("적", "적"), ("맞는적", "맞는적"), ("띠", "띠"),
              ("화면p99", "p99"), ("테색상", "테°"), ("흰고리Y", "흰고리"), ("적차이", "차이%"), ("룬", "룬"), ("큰충격파", "충격파"), ("밀도개수", "밀도>12")]


def table(shots, title):
    print(f"\n## {title}")
    head = ["촬영"] + [k[1] for k in TABLE_KEYS]
    rows = []
    for p in PLACES:
        for s in (f"read_{p}_clean", f"read_{p}"):
            d = shots.get(s)
            if not d:
                continue
            rows.append([s] + [str(d.get(k, "-")) if k != "밀도개수" else ("-" if d.get(k) is None else f"{d.get(k):.0f}") for k, _ in TABLE_KEYS])
    if not rows:
        print("(readability 줄 없음)")
        return
    w = [max(len(str(r[i])) for r in rows + [head]) for i in range(len(head))]
    print("  ".join(h.ljust(w[i]) for i, h in enumerate(head)))
    for r in rows:
        print("  ".join(str(v).ljust(w[i]) for i, v in enumerate(r)))


def compare(after, before):
    print("\n## 전 → 후 (숫자 칸 · 첫 값)")
    for p in PLACES:
        for s in (f"read_{p}_clean", f"read_{p}"):
            a, b = after.get(s), before.get(s)
            if not a or not b:
                continue
            parts = []
            for k, lab in TABLE_KEYS:
                if k == "밀도개수":
                    va, vb = a.get(k), b.get(k)
                else:
                    va = (nums(a.get(k)) or [None])[0]
                    vb = (nums(b.get(k)) or [None])[0]
                if va is None or vb is None:
                    continue
                parts.append(f"{lab} {vb:.0f}→{va:.0f}")
            print(f"{s}: " + " · ".join(parts))


def gray(folder):
    hits = sorted(glob.glob(os.path.join(folder, "*_read_desert.png")))
    if not hits:
        print("\nread_desert PNG 없음 → 흑백 건너뜀")
        return
    try:
        from PIL import Image
    except ImportError:
        print("\nPillow 없음 → 흑백 건너뜀 (py -3.14 에는 있음)")
        return
    out = os.path.join(folder, "read_desert_gray.png")
    Image.open(hits[-1]).convert("RGB").convert("L").save(out)       # PIL 'L' = ITU-R 601-2 (Rec.601 Y)
    print(f"\n흑백 한 장: {out}")


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    after = read_log(sys.argv[1])
    before = read_log(sys.argv[2]) if len(sys.argv) > 2 else None
    table(after, "명도 표 — " + sys.argv[1])
    if before is not None:
        table(before, "명도 표 (전) — " + sys.argv[2])
        compare(after, before)
    res = judge(after)
    print("\n## 6-3 기준")
    npass = nfail = 0
    for no, p, ok, msg in res:
        mark = "통과" if ok is True else "실패" if ok is False else "참고"
        npass += ok is True
        nfail += ok is False
        print(f"[{mark}] {no}. {NAMES.get(p, p)} — {msg}")
    print("[눈으로] 8. UI (알림 띠 위치 · 글 넘침 · 카드 위 정렬 · 띠 위를 덮는 그림) — read_* · ui_menu · reward_slot 캡처")
    print(f"\n통과 {npass} · 실패 {nfail}")
    gray(sys.argv[1])


if __name__ == "__main__":
    main()
