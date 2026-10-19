"""Soul Saver 플레이 데이터 내려받기 — 서버 /v1/export 에서 새 행만 받아 로컬 SQLite 에 쌓음.

사용:
    python tools/telemetry/pull.py --url https://soulsaver-telemetry.<계정>.workers.dev
    (또는 환경변수 SOULSAVER_TELEMETRY_URL)
토큰: 환경변수 SOULSAVER_TELEMETRY_TOKEN 또는 tools/telemetry/.token 파일 (서버의 ADMIN_TOKEN)
저장: docs/balance/telemetry/telemetry.sqlite (git 에 안 올라감)
표준 라이브러리만 씀.
"""
import argparse
import datetime
import json
import os
import sqlite3
import sys
import urllib.error
import urllib.request

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(HERE))
DEFAULT_DB = os.path.join(ROOT, "docs", "balance", "telemetry", "telemetry.sqlite")

# 게임 balance_log.csv 머리줄과 같은 칸 (RunStats.WriteBalanceLog) + 텔레메트리 추가 칸
# 펼친 뷰(runs_flat)에 쓰임. 여기 없는 칸도 data JSON 안에는 그대로 있음
FLAT_COLUMNS = [
    "date", "version", "build", "platform", "lang", "character", "difficulty", "endless", "result",
    "seconds", "level", "kills", "shards_total", "shards_spent",
    "evo1", "evo1_sec", "evo2", "evo2_sec",
    "weapon", "ultimate", "skill", "survival", "soul", "wealth",
    "cards", "stage", "dealt_total", "dealt_by", "taken_total", "taken_by", "last_hit",
    "chapter_secs", "weight", "brands", "relics", "skill_evos",
    "retry_kind", "menu_gap_secs", "contracts_done", "herald_shown", "milestones", "points_run",
    "weak_hits", "weak_share", "memory_trig", "memory_break",
    "path1", "path2", "path_tiers", "cross", "cards_seq",
]


def read_token():
    t = os.environ.get("SOULSAVER_TELEMETRY_TOKEN", "").strip()
    if t:
        return t
    p = os.path.join(HERE, ".token")
    if os.path.exists(p):
        with open(p, encoding="utf-8") as f:
            return f.read().strip()
    return ""


def open_db(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    db = sqlite3.connect(path)
    db.execute("""CREATE TABLE IF NOT EXISTS runs (
        id INTEGER PRIMARY KEY, received_at TEXT, anon_id TEXT, schema INTEGER,
        version TEXT, build TEXT, data TEXT)""")
    db.execute("CREATE INDEX IF NOT EXISTS idx_runs_version ON runs (version)")
    db.execute("CREATE INDEX IF NOT EXISTS idx_runs_anon ON runs (anon_id)")
    # 펼친 뷰는 칸 목록이 바뀔 수 있으니 매번 다시 만듦
    db.execute("DROP VIEW IF EXISTS runs_flat")
    cols = ", ".join(
        "json_extract(data, '$.%s') AS \"%s\"" % (c, c)
        for c in FLAT_COLUMNS if c not in ("version", "build"))
    db.execute("CREATE VIEW runs_flat AS SELECT id, received_at, anon_id, schema, version, build, %s FROM runs" % cols)
    db.commit()
    return db


def fetch_page(base, token, since):
    req = urllib.request.Request(
        "%s/v1/export?since=%d" % (base.rstrip("/"), since),
        headers={"Authorization": "Bearer " + token, "Accept": "application/json",
                 "User-Agent": "soulsaver-telemetry-pull"})
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.loads(r.read().decode("utf-8"))


def main():
    ap = argparse.ArgumentParser(description="Soul Saver 플레이 데이터 내려받기")
    ap.add_argument("--url", default=os.environ.get("SOULSAVER_TELEMETRY_URL", ""), help="Worker 주소")
    ap.add_argument("--db", default=DEFAULT_DB, help="로컬 SQLite 경로")
    ap.add_argument("--full", action="store_true",
                    help="로컬 기록을 비우고 처음부터 다시 받기 (서버에서 삭제 요청된 판을 로컬에서도 지움 · 주기적으로)")
    a = ap.parse_args()
    if not a.url:
        sys.exit("주소가 없음: --url 또는 환경변수 SOULSAVER_TELEMETRY_URL")
    u = a.url.lower()
    if not (u.startswith("https://") or u.startswith("http://localhost") or u.startswith("http://127.0.0.1")):
        sys.exit("https:// 주소만 씀 (토큰이 평문으로 나가지 않게 · 로컬 wrangler dev 의 http://localhost 만 예외)")
    token = read_token()
    if not token:
        sys.exit("토큰이 없음: 환경변수 SOULSAVER_TELEMETRY_TOKEN 또는 tools/telemetry/.token")

    db = open_db(a.db)
    if a.full:
        db.execute("DELETE FROM runs")         # 커밋은 다 받은 뒤 (중간에 실패하면 되돌려 로컬 사본 유지)
    since = 0 if a.full else (db.execute("SELECT COALESCE(MAX(id), 0) FROM runs").fetchone()[0])
    total = 0
    while True:
        try:
            page = fetch_page(a.url, token, since)
        except urllib.error.HTTPError as e:
            db.rollback()
            sys.exit("서버 오류 %d (토큰 · 주소 확인)" % e.code)
        except urllib.error.URLError as e:
            db.rollback()
            sys.exit("연결 실패: %s" % e.reason)
        rows = page.get("rows") or []
        db.executemany(
            "INSERT OR REPLACE INTO runs (id, received_at, anon_id, schema, version, build, data) VALUES (?,?,?,?,?,?,?)",
            [(r["id"], r["received_at"], r["anon_id"], r["schema"], r["version"], r["build"], r["data"]) for r in rows])
        if not a.full:
            db.commit()                         # 이어받기는 쪽마다 저장 (--full 은 끝에서 한 번)
        total += len(rows)
        since = page.get("next", since)
        if not page.get("more") or not rows:
            break
    # 보유 기간 2년: 서버 크론과 맞춰 로컬 사본에서도 지움
    cutoff = (datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(days=730)).strftime("%Y-%m-%dT%H:%M:%S")
    db.execute("DELETE FROM runs WHERE received_at < ?", (cutoff,))
    db.commit()
    n = db.execute("SELECT COUNT(*) FROM runs").fetchone()[0]
    print("새로 받은 판 %d · 로컬 전체 %d · 마지막 id %d" % (total, n, since))
    print("저장:", a.db)


if __name__ == "__main__":
    main()
