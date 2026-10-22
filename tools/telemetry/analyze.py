"""Soul Saver 플레이 데이터 분석 — pull.py 가 쌓은 SQLite 를 읽어 마크다운 표로 출력.

사용:
    python tools/telemetry/analyze.py                       # 전체
    python tools/telemetry/analyze.py --version 1.4.3 --build full
    python tools/telemetry/analyze.py --version 1.4 --since 2026-10-01 > docs/balance/telemetry/report.md
--version 은 앞부분 일치 (1.4 → 1.4.x 전부). 승리 = result "clear". 표준 라이브러리만 씀.
"""
import argparse
import collections
import json
import os
import sqlite3
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
DEFAULT_DB = os.path.join(ROOT, "docs", "balance", "telemetry", "telemetry.sqlite")
BRANCHES = ["weapon", "ultimate", "skill", "survival", "soul", "wealth"]     # 영혼 트리 갈래 레벨 칸


def num(v, default=0.0):
    try:
        return float(v)
    except (TypeError, ValueError):
        return default


def s(v):
    return "" if v is None else str(v)


def split_list(v):
    """'a|b|c' 칸 → 목록 (빈 칸 빼고)"""
    return [x for x in s(v).split("|") if x.strip()]


def dev_ids():
    """이 PC(개발 기기)의 익명 ID — %LOCALAPPDATA%\Soul Saver*\save.json 의 telemetry.anon_id (2026-10-22~ 전체 판의 70% 가 개발 기기 판이라 기본으로 뺌)"""
    import glob, re
    ids = set()
    base = os.environ.get("LOCALAPPDATA", "")
    for f in glob.glob(os.path.join(base, "Soul Saver*", "save.json")):
        try:
            txt = open(f, encoding="utf-8", errors="ignore").read()
        except OSError:
            continue
        ids.update(re.findall(r"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", txt))
    return ids


def load(db_path, version, build, since, skip=frozenset()):
    if not os.path.exists(db_path):
        sys.exit("로컬 DB 없음: 먼저 pull.py 실행 (%s)" % db_path)
    db = sqlite3.connect(db_path)
    q = "SELECT version, build, data FROM runs WHERE 1=1"
    args = []
    if version:
        q += " AND (version = ? OR version LIKE ?)"
        args += [version, version + ".%"]
    if build:
        q += " AND build = ?"
        args.append(build)
    rows, tut = [], []
    for ver, bld, data in db.execute(q, args):
        try:
            d = json.loads(data)
        except ValueError:
            continue
        if since and s(d.get("date")) < since:
            continue
        if s(d.get("anon_id")) in skip:
            continue
        d["version"], d["build"] = ver, bld
        # 튜토리얼 판은 밸런싱에서 빼고 따로 셈 (1.4.7~ 게임도 보내지 않음)
        (tut if s(d.get("retry_kind")) == "tutorial" else rows).append(d)
    return rows, tut


def is_win(d):
    return s(d.get("result")) == "clear"


def table(title, head, body, note=""):
    out = ["", "## " + title, ""]
    if note:
        out += [note, ""]
    if not body:
        return "\n".join(out + ["(기록 없음)"])
    out.append("| " + " | ".join(head) + " |")
    out.append("|" + "|".join("---" if i == 0 else "---:" for i in range(len(head))) + "|")
    for r in body:
        out.append("| " + " | ".join(str(x) for x in r) + " |")
    return "\n".join(out)


def pct(a, b):
    return "%.1f%%" % (100.0 * a / b) if b else "-"


def mmss(sec):
    sec = int(round(sec))
    return "%d:%02d" % (sec // 60, sec % 60)


def group_stats(rows, keyf, min_n=1):
    g = collections.defaultdict(list)
    for d in rows:
        k = keyf(d)
        if k is None or k == "":
            continue
        g[k].append(d)
    body = []
    for k, rs in sorted(g.items(), key=lambda kv: -len(kv[1])):
        if len(rs) < min_n:
            continue
        wins = sum(1 for d in rs if is_win(d))
        body.append([k, len(rs), pct(wins, len(rs)),
                     mmss(sum(num(d.get("seconds")) for d in rs) / len(rs)),
                     "%.1f" % (sum(num(d.get("level")) for d in rs) / len(rs)),
                     "%.0f" % (sum(num(d.get("kills")) for d in rs) / len(rs))])
    return body


def main():
    ap = argparse.ArgumentParser(description="Soul Saver 플레이 데이터 분석 (마크다운)")
    ap.add_argument("--db", default=DEFAULT_DB)
    ap.add_argument("--version", default="", help="게임 버전 (앞부분 일치)")
    ap.add_argument("--build", choices=["full", "demo"], default=None, help="정식 full / 체험판 demo")
    ap.add_argument("--since", default="", help="이 날짜(yyyy-MM-dd)부터")
    ap.add_argument("--min", type=int, default=5, help="카드 · 원인 표에 넣을 최소 판 수")
    ap.add_argument("--top", type=int, default=30, help="목록 표 최대 줄 수")
    ap.add_argument("--include-dev", action="store_true", help="개발 기기(이 PC 저장 파일의 익명 ID) 판도 넣기 (기본은 뺌)")
    a = ap.parse_args()

    skip = frozenset() if a.include_dev else frozenset(dev_ids())
    rows, tut = load(a.db, a.version, a.build, a.since, skip)
    if sys.stdout.encoding and sys.stdout.encoding.lower() not in ("utf-8", "utf8"):
        try:
            sys.stdout.reconfigure(encoding="utf-8")
        except Exception:
            pass

    filt = []
    if a.version: filt.append("버전 " + a.version)
    if a.build: filt.append({"full": "정식판", "demo": "체험판"}[a.build])
    if a.since: filt.append(a.since + " 부터")
    if skip: filt.append("개발 기기 %d개 뺌" % len(skip))
    players = len(set(s(d.get("anon_id")) for d in rows))
    wins = sum(1 for d in rows if is_win(d))
    out = ["# Soul Saver 플레이 데이터 분석",
           "",
           "필터: %s · 판 %d · 익명 플레이어 %d · 승률 %s" % (" · ".join(filt) or "전체", len(rows), players, pct(wins, len(rows)))]

    # 튜토리얼 (위 통계에서 빠진 판) · 본게임 이어감 = 같은 익명 ID 가 본게임 판도 남김
    real_ids = set(s(d.get("anon_id")) for d in rows)
    body = []
    for ver, ts in sorted(collections.Counter(d["version"] for d in tut).items()):
        rs = [d for d in tut if d["version"] == ver]
        ids = set(s(d.get("anon_id")) for d in rs)
        body.append([ver, len(rs), len(ids), mmss(sum(num(d.get("seconds")) for d in rs) / len(rs)),
                     "%d / %d" % (len(ids & real_ids), len(ids))])
    out.append(table("튜토리얼 (위 통계에서 뺀 판)", ["버전", "판", "플레이어", "평균 시간", "본게임 이어감"], body,
                     "1.4.7~ 게임은 튜토리얼 판을 보내지 않음"))

    head = ["", "판", "승률", "평균 생존", "평균 레벨", "평균 처치"]
    out.append(table("버전 · 판본별", head, group_stats(rows, lambda d: "%s (%s)" % (d["version"], d["build"]))))
    out.append(table("영웅별", head, group_stats(rows, lambda d: s(d.get("character")))))
    out.append(table("난이도별", head, group_stats(rows, lambda d: s(d.get("difficulty")) + (" · 무한" if s(d.get("endless")) in ("1", "1.0", "True", "true") else ""))))
    out.append(table("영웅 × 난이도", head, group_stats(rows, lambda d: "%s · %s" % (s(d.get("character")), s(d.get("difficulty"))), min_n=a.min)))

    # 결과 분포
    res = collections.Counter(s(d.get("result")) or "?" for d in rows)
    out.append(table("판 결과", ["결과", "판", "비율"], [[k, n, pct(n, len(rows))] for k, n in res.most_common()]))

    # 갈래 (판 안의 갈래 path1 · path2)
    out.append(table("갈래별 (첫 갈래)", head, group_stats(rows, lambda d: "%s · %s" % (s(d.get("character")), s(d.get("path1"))) if s(d.get("path1")) else None)))
    out.append(table("갈래 조합", head, group_stats(rows, lambda d: "%s · %s + %s" % (s(d.get("character")), s(d.get("path1")), s(d.get("path2"))) if s(d.get("path1")) else None, min_n=a.min)))

    # 영혼 트리 갈래 레벨 평균 (판 시작 시 투자량)
    body = []
    by_hero = collections.defaultdict(list)
    for d in rows:
        by_hero[s(d.get("character"))].append(d)
    for hero, rs in sorted(by_hero.items(), key=lambda kv: -len(kv[1])):
        body.append([hero or "?", len(rs)] + ["%.1f" % (sum(num(d.get(b)) for d in rs) / len(rs)) for b in BRANCHES])
    out.append(table("영혼 트리 갈래 평균 레벨", ["영웅", "판"] + BRANCHES, body))

    # 카드 픽률 · 승률 (cards_seq: 고른 카드 번호 순서). 픽률 = 그 카드를 한 번이라도 고른 판 / 그 영웅 판
    def pick_table(field, title, note):
        cnt, win = collections.Counter(), collections.Counter()
        hero_n = collections.Counter(s(d.get("character")) for d in rows)
        for d in rows:
            hero = s(d.get("character"))
            for c in set(split_list(d.get(field))):
                k = (hero, c.split("@")[0])
                cnt[k] += 1
                if is_win(d):
                    win[k] += 1
        body = []
        for (hero, c), n in cnt.most_common():
            if n < a.min:
                continue
            body.append([hero, c, n, pct(n, hero_n[hero]), pct(win[(hero, c)], n)])
            if len(body) >= a.top * 3:
                break
        return table(title, ["영웅", "항목", "판", "픽률", "승률"], body, note)

    out.append(pick_table("cards_seq", "카드 픽률 · 승률", "카드 번호(cards_seq). 픽률 = 고른 판 / 그 영웅의 판, 승률 = 고른 판 중 클리어 (판 %d 이상)" % a.min))
    out.append(pick_table("relics", "유물 픽률 · 승률", ""))
    out.append(pick_table("brands", "각인 픽률 · 승률", ""))
    out.append(pick_table("skill_evos", "스킬 진화 픽률 · 승률", ""))

    # 진화 (evo1 · evo2)
    evo = collections.Counter()
    evo_sec = collections.defaultdict(list)
    for d in rows:
        for k, t in (("evo1", "evo1_sec"), ("evo2", "evo2_sec")):
            e = s(d.get(k))
            if e:
                evo[e] += 1
                evo_sec[e].append(num(d.get(t)))
    out.append(table("진화", ["진화", "판", "평균 시점"],
                     [[e, n, mmss(sum(evo_sec[e]) / len(evo_sec[e]))] for e, n in evo.most_common(a.top)]))

    # 쓰러진 원인 상위
    deaths = [d for d in rows if s(d.get("result")) == "death"]
    lh = collections.Counter(s(d.get("last_hit")) or "?" for d in deaths)
    out.append(table("쓰러진 원인 상위", ["원인", "판", "죽음 중 비율"], [[k, n, pct(n, len(deaths))] for k, n in lh.most_common(a.top)]))

    # 받은 피해 출처 (taken_by "이름:양|…")
    taken = collections.Counter()
    for d in rows:
        for part in split_list(d.get("taken_by")):
            name, _, amt = part.rpartition(":")
            taken[name or part] += num(amt)
    tot = sum(taken.values())
    out.append(table("받은 피해 출처", ["출처", "피해 합", "비율"], [[k, "%.0f" % v, pct(v, tot)] for k, v in taken.most_common(a.top)]))

    # 장별 시간 (chapter_secs "장:초|…")
    ch = collections.defaultdict(list)
    for d in rows:
        for part in split_list(d.get("chapter_secs")):
            name, _, sec = part.rpartition(":")
            if name:
                ch[name].append(num(sec))
    body = []
    for name in sorted(ch, key=lambda n: (len(n), n)):
        v = sorted(ch[name])
        body.append([name, len(v), mmss(sum(v) / len(v)), mmss(v[len(v) // 2])])
    out.append(table("장별 시간", ["장", "판", "평균", "중앙값"], body))

    print("\n".join(out))


if __name__ == "__main__":
    main()
