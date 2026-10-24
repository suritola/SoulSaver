"""손 구간 → 손 도트 지도 격자 (그래픽 개편 2단계 · 대안 F1)

몸 명암처럼 넓은 면은 글자 격자를 한 칸씩 치기보다 「행마다 단계:끝열」 구간으로 손으로 적는 편이 고치기 쉽다.
handmaps/runs/<보스>_*.txt (손으로 적은 원본) → handmaps/<보스>.txt 의 @grid 칸을 다시 만든다. 결과 격자가 그림에 쓰이는 원본.

구간 파일 형식:
  #! grid <격자 이름> y0 <시트 첫 행> width <판 폭> body <몸 열쇠>     (첫 줄 · 몸 열쇠는 보스 모듈의 body_extents 가 앎)
  <시트 행> <시작열> | <단계>:<끝열> ...                                 (끝열까지 그 단계 · 행 끝이 몸 끝과 다르면 멈춤)
  @ <시트 행> <시작열> | <글자들>                                        (덧칠 · '.' 은 그대로)
사용: py -3.14 -X utf8 tools/pixelart/hires/runs2grid.py slimeking
"""
import glob
import importlib
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.dirname(HERE))


def build_grid(path, ext):
    head, rows, over = None, {}, []
    for line in open(path, encoding="utf-8"):
        line = line.rstrip("\n")
        if line.startswith("#!"):
            t = line[2:].split()
            head = dict(zip(t[0::2], t[1::2]))
            continue
        if not line.strip() or line.startswith("#"):
            continue
        if line.startswith("@"):
            h, txt = line[1:].split("|")
            y, x = map(int, h.split())
            over.append((y, x, txt.strip()))
            continue
        h, runs = line.split("|")
        y, start = map(int, h.split())
        a, b = ext[y]
        if start != a:
            raise SystemExit(f"{os.path.basename(path)} 행 {y}: 시작열 {start} ≠ 몸 {a}")
        width = int(head["width"])
        row, x = ["."] * width, start
        for r in runs.split():
            t, e = r.split(":")
            e = int(e)
            if e < x:
                raise SystemExit(f"{os.path.basename(path)} 행 {y}: 구간 {r} 이 거꾸로")
            row[x:e + 1] = [t] * (e + 1 - x)
            x = e + 1
        if x - 1 != b:
            raise SystemExit(f"{os.path.basename(path)} 행 {y}: 끝열 {x - 1} ≠ 몸 끝 {b}")
        rows[y] = row
    for y, x, txt in over:
        for i, ch in enumerate(txt):
            if ch != ".":
                rows[y][x + i] = ch
    ys = sorted(rows)
    if ys != list(range(ys[0], ys[-1] + 1)) or ys[0] != int(head["y0"]):
        raise SystemExit(f"{os.path.basename(path)}: 행이 비었거나 y0 와 다름")
    return head["grid"], [f"@grid {head['grid']}", f"# 시트 y {ys[0]} 부터 · 열 0 = 몸 판 왼쪽 (원본: handmaps/runs/{os.path.basename(path)})"] + ["".join(rows[y]) for y in ys], head["body"]


def main():
    boss = sys.argv[1] if len(sys.argv) > 1 else "slimeking"
    mod = importlib.import_module("hires.bosses." + boss)
    hm = os.path.join(HERE, "handmaps", boss + ".txt")
    s = open(hm, encoding="utf-8").read()
    for path in sorted(glob.glob(os.path.join(HERE, "handmaps", "runs", boss + "_*.txt"))):
        head = open(path, encoding="utf-8").readline()
        key = dict(zip(head[2:].split()[0::2], head[2:].split()[1::2]))["body"]
        name, lines, _ = build_grid(path, mod.body_extents(key))
        g = "\n".join(lines)
        m = re.search(r"@grid " + name + r"\n(?:[^@\n][^\n]*\n?)*", s)
        s = s[:m.start()] + g + "\n" + s[m.end():] if m else s.rstrip("\n") + "\n\n" + g + "\n"
        print(f"격자 {name}: {len(lines) - 2}행 ← {os.path.relpath(path, HERE)}")
    with open(hm, "w", encoding="utf-8", newline="\n") as f:
        f.write(s)


if __name__ == "__main__":
    main()
