-- Soul Saver 플레이 데이터 (판 기록 한 줄 = 한 행)
-- IP · User-Agent · 기기 정보는 저장하지 않음
CREATE TABLE IF NOT EXISTS runs (
  id          INTEGER PRIMARY KEY AUTOINCREMENT,
  received_at TEXT NOT NULL,          -- 서버가 받은 시각 (ISO 8601 UTC)
  anon_id     TEXT NOT NULL,          -- 게임이 만든 무작위 UUID
  schema      INTEGER NOT NULL,
  version     TEXT NOT NULL,
  build       TEXT NOT NULL,          -- full / demo
  data        TEXT NOT NULL           -- 판 기록 전체 JSON
);
CREATE INDEX IF NOT EXISTS idx_runs_anon     ON runs (anon_id, received_at);
CREATE INDEX IF NOT EXISTS idx_runs_version  ON runs (version);
CREATE INDEX IF NOT EXISTS idx_runs_received ON runs (received_at);
