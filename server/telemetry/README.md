# Soul Saver 플레이 데이터 서버 (Cloudflare Workers + D1)

동의한 유저의 판 기록을 받아 D1 에 모으는 Worker. 빌드 단계 없음 (`src/index.js` 그대로 배포).

## 처음 배포 (사용자가 칠 명령)

```powershell
npm i -g wrangler
wrangler login                                   # 브라우저에서 Cloudflare 로그인
cd server/telemetry
wrangler d1 create soulsaver-telemetry           # 나온 database_id 를 wrangler.toml 의 database_id 에 붙여넣기
wrangler d1 execute soulsaver-telemetry --remote --file schema.sql
wrangler secret put ADMIN_TOKEN                  # 긴 무작위 문자열 입력 (내려받기용 · 아래 CLI 에도 같은 값)
wrangler deploy                                  # 나온 주소(https://soulsaver-telemetry.<계정>.workers.dev)를
                                                 # Assets/Scripts/Telemetry.cs 의 Endpoint 에
```

- 무작위 토큰 만들기: `pwsh -c "[Convert]::ToBase64String((1..32|%{Get-Random -Max 256}))"`
- 같은 토큰을 `tools/telemetry/.token` 파일에 넣거나 환경변수 `SOULSAVER_TELEMETRY_TOKEN` 으로 (→ `tools/telemetry/README.md`)
- 게임 공개 키를 쓰려면 `wrangler.toml` 의 `INGEST_KEY` 와 `Telemetry.IngestKey` 를 같은 값으로 (게임 안에 들어가므로 비밀이 아님 · 아무나 보내는 것만 조금 막음)

## 엔드포인트

| 경로 | 설명 |
|---|---|
| `POST /v1/run` | 판 기록 JSON 한 줄. `Content-Type: application/json`, 16KB 이하, 칸 200개 이하, 칸 이름 `[a-z0-9_]` 64자, 값은 문자열(4096자)·숫자·불·null 만. 필수: `schema` `anon_id`(UUID) `version` `build`(full/demo) `consent_ver`. `date` 는 `yyyy-MM-dd` 만. `INGEST_KEY` 가 있으면 `X-Ingest-Key` 헤더 일치. anon_id 당 하루(UTC) 100건 |
| `POST /v1/delete` | `{"anon_id": "..."}` → 그 ID 의 모든 행 삭제 (`X-Ingest-Key` 검사 동일) |
| `GET /v1/export?since=<id>` | `Authorization: Bearer <ADMIN_TOKEN>` (상수 시간 비교). id 순 5000행 → `{rows, next, more}` |
| 크론 (매일 03:17 UTC) | 받은 지 2년(730일) 지난 행 삭제 |

응답 코드: 성공 200, 검사 실패 4xx (게임은 그 줄을 버림), 서버 오류 500 (게임이 다음 기회에 다시 보냄).

## 개인정보

- IP · User-Agent · `CF-*` 헤더는 읽지 않고 저장 · 로그도 안 함. `wrangler.toml` 의 `[observability] enabled = false` 로 Workers 요청 로그도 끔 (Cloudflare 대시보드에서 Logpush · Workers Logs 를 켜지 말 것)
- CORS 없음
- 삭제 확인: `wrangler d1 execute soulsaver-telemetry --remote --command "SELECT COUNT(*) FROM runs WHERE anon_id='...'"`

## 무료 한도 메모 (Workers Free · 2025 기준, 바뀔 수 있음)

- Workers: 하루 요청 10만 건
- D1: 저장 5GB, 하루 행 읽기 500만 · 행 쓰기 10만
- 판 하나 ≈ 1~3KB → 5GB 면 수백만 판. 한 판 = 쓰기 1 + 하루 제한 카운트 읽기(인덱스) 몇 행
- 하루 제한 카운트는 `(anon_id, received_at)` 인덱스로 그 유저 오늘 행만 읽음
- 크론 삭제는 인덱스(`received_at`)로 오래된 행만 훑음
