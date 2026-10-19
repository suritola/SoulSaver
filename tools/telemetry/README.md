# 플레이 데이터 내려받기 · 분석 (개발자용)

서버(`server/telemetry`)에 모인 판 기록을 로컬 SQLite 로 받아 밸런싱 표를 뽑는다. 파이썬 표준 라이브러리만 씀.

## 준비

- 서버 주소: `--url https://soulsaver-telemetry.<계정>.workers.dev` 또는 환경변수 `SOULSAVER_TELEMETRY_URL`
- 토큰(서버 `ADMIN_TOKEN` 과 같은 값): 환경변수 `SOULSAVER_TELEMETRY_TOKEN` 또는 `tools/telemetry/.token` 파일 한 줄 (git 에 안 올라감)

## 내려받기

```powershell
python tools/telemetry/pull.py --url https://soulsaver-telemetry.<계정>.workers.dev
python tools/telemetry/pull.py --full      # 로컬을 비우고 처음부터 (한 달에 한 번쯤 — 서버에서 삭제 요청된 판을 로컬에서도 지움)
```

- 저장: `docs/balance/telemetry/telemetry.sqlite` (git 에 안 올라감). 마지막 id 다음부터 이어받음
- 표 `runs` (원본 JSON 은 `data`) · 뷰 `runs_flat` (판 기록 칸을 펼친 것 — `SELECT character, result, seconds FROM runs_flat`)
- 받은 지 2년 지난 행은 로컬에서도 지움 (서버 보유 기간과 같음)

## 분석

```powershell
python tools/telemetry/analyze.py                                   # 전체
python tools/telemetry/analyze.py --version 1.4.3 --build full      # 버전(앞부분 일치) · 정식 full / 체험판 demo
python tools/telemetry/analyze.py --version 1.4 --since 2026-10-20 --min 10 > docs/balance/telemetry/report.md
```

표: 버전 · 판본별, 영웅별, 난이도별, 영웅 × 난이도, 판 결과, 갈래별 · 갈래 조합, 영혼 트리 갈래 평균 레벨, 카드(`cards_seq`) · 유물 · 각인 · 스킬 진화 픽률 · 승률, 진화, 쓰러진 원인 상위, 받은 피해 출처, 장별 시간.
승리 = `result` 가 `clear`. `--min` 보다 판 수가 적은 항목은 카드 · 조합 표에서 뺌.

## 주의

- 내려받은 데이터는 개인정보 처리방침(`docs/privacy/`)의 목적(밸런스 · 버그 · 성능)에만 쓰고, 파일을 남에게 넘기지 않는다
- `anon_id` 로 개인을 알아내려 하지 않는다
