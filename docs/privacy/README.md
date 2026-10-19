# 개인정보 처리방침 · 운영 메모

| 파일 | 언어 |
|---|---|
| `privacy-policy.ko.md` | 한국어 (원문) |
| `privacy-policy.en.md` | English |
| `privacy-policy.ja.md` | 日本語 |
| `privacy-policy.zh.md` | 简体中文 |

게임 안 동의문(`Assets/Scripts/LocTable.Telemetry.cs`)과 같은 내용. 둘 중 하나를 고치면 다른 쪽도 고치고, 내용이 바뀌면 `Telemetry.ConsentVersion` 을 올려 다시 동의를 받는다 (문서 버전 · 시행일도 같이).

## 배포 전 할 일

1. **문의 이메일 정하기** — `Telemetry.ContactEmail` 과 네 문서의 「문의」 칸 대체 문구(TODO 주석)를 같은 주소로
2. **처리방침을 웹에 게시** — 예: GitHub 저장소 페이지 · GitHub Pages · 노션 공개 페이지 등. 고정 주소가 필요함
3. **스팀 상점 페이지에 링크** — Steamworks 앱 관리 > 상점 페이지 편집 > 기본 정보의 「개인정보 처리방침」(Privacy Policy) URL 칸에 위 주소. 상점 설명 아래에도 「플레이 데이터 수집(선택 · 동의 시)」 한 줄
4. **Steamworks 의 데이터 수집 고지 확인** — Steamworks 설정에 데이터 수집 · 개인정보 관련 고지/설문 항목이 있으면 「선택 동의 · 익명 게임 플레이 통계 · 제3자 판매 없음」으로 맞게 표시 (현재 Steamworks 화면에 어떤 칸이 있는지는 직접 확인 필요)
5. **체험판 상점 페이지도 같은 링크** (체험판도 같은 동의창을 띄움)
6. 서버 배포 후 Cloudflare 대시보드에서 Workers Logs · Logpush 가 꺼져 있는지 확인 (IP 가 로그에 남지 않게)

## 운영 중

- 삭제 요청이 이메일로 오면: `wrangler d1 execute soulsaver-telemetry --remote --command "DELETE FROM runs WHERE anon_id='<ID 전체>'"` (설정 화면은 앞 8자만 보여 주므로 `LIKE '<앞8자>%'` 로 찾아 확인 후 삭제). 로컬 사본은 `pull.py --full` 로 다시 받기
- 열람 요청: 같은 ID 로 `SELECT data FROM runs WHERE anon_id=...` 결과를 보내 줌
- 처리 · 응답은 늦어도 10일(한국) · 1개월(GDPR) 안에
- 수집 항목을 늘리면(새 칸이 개인을 알아볼 수 있는 정보라면 특히) 처리방침 · 동의문을 고치고 ConsentVersion 을 올린다. 판 기록에 이미 있는 밸런스 칸이 늘어나는 정도는 「피해 통계 · 고른 카드 …」 범위 안이면 그대로
