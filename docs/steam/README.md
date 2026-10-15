# 스팀 출시 준비

Soul Saver를 스팀에 올리기 위해 **이미 해 둔 것**과 **직접 해야 하는 것**을 정리했습니다.

## ✅ 해 둔 것

| 항목 | 위치 |
|---|---|
| Steamworks.NET 연동 (초기화 · 콜백 · 스팀 밖 실행 시 스팀으로 재실행) | `Assets/Scripts/SteamManager.cs`, `Packages/manifest.json` |
| 업적 32개 (코드 연결 + 4개 언어 이름 · 설명 + 아이콘) | [achievements.md](achievements.md), `art/achievements/` |
| 순위표: 무한 모드 최고 생존 `ENDLESS_BEST` · 일일 도전 `DAILY_<UTC 날짜 yyyyMMdd>` (게임이 처음 올릴 때 `FindOrCreateLeaderboard` 로 만듦, 내림차순 · 시간(초) 표시, KeepBest). 점수 = 버틴 초, 자세한 값 5칸 = 영웅 번호 · 보스 처치 · 레벨 · 처치 수 · 판 버전(1.1.5 → 10105). 1.1.5~ 메인 메뉴 「순위표」 창(전체 상위 100 · 내 주변 ±5 · 친구) | `SteamLeaderboards` (`Assets/Scripts/SteamManager.cs`), `Assets/Scripts/DailyChallenge.cs`, `Assets/Scripts/LeaderboardUI.cs` |
| 스팀용 빌드와 GitHub용 빌드 분리 (`-steam`일 때만 연동, 아니면 `DISABLESTEAMWORKS`) | `Assets/Editor/BuildScript.cs` |
| 게임 아이콘 (실행 파일 · 작업 표시줄) | `Assets/Art/Icon/GunSaverIcon.png` |
| SteamPipe 업로드 스크립트 | `tools/steam-upload.ps1`, `tools/steam/steam-config.json` |
| 상점 페이지 글 (한 · 영 · 일 · 중), 태그, 시스템 요구 사항 | [store-page.md](store-page.md) |
| 상점 · 라이브러리 이미지 · 아이콘 (정식판 + 체험판, 2026-10-14 새 도트 그래픽으로 전면 교체) · 스크린샷 12장 × 4개 언어 + 체험판 7장 × 4개 언어 | `art/capsules/`, `art/demo/`, `art/screenshots/`, `art/demo/screenshots/` |

> 캡슐 · 라이브러리 이미지는 게임 안 실제 도트 그림(영웅 · 적 · 보스 · 이펙트 · 타일)을 정수 배율로 키워 합성하고, 로고는 손으로 찍은 도트 글자입니다 (`tools/steam_art/make_store_art.py`). 자세한 목록은 아래 「이미지 규격」.

## 📝 직접 해야 하는 것

### 1. 값 (채워 둠)
- [x] App ID `5328770` · Depot ID `5328771` · 빌드 계정 `phdstudy` — `tools/steam/steam-config.json`, `SteamManager.cs`
- [x] 개발사 `박민근` — `ProjectSettings` Company Name (이름이 바뀌어 저장된 설정이 한 번 초기화됨)
- [ ] steamcmd를 다른 곳에 설치했다면 `steam-config.json`의 `steamcmd` 경로 수정

### 2. steamcmd 설치
- [ ] https://developer.valvesoftware.com/wiki/SteamCMD 에서 받아 `C:\steamcmd`에 풀기

### 3. Steamworks 설정 (partner.steamgames.com)
- [ ] **SteamPipe → 디포**: 디포 1개 (Windows, 모든 언어)
- [ ] **설치 → 일반**: 실행 옵션 — 실행 파일 `Soul Saver.exe` (1.7.3 전에는 `Gun Saver.exe`), OS Windows, 64비트
- [ ] **스탯 및 업적**: [achievements.md](achievements.md) 대로 32개 만들고 **게시**
- [ ] **순위표** (선택): 게임이 알아서 만들지만, 커뮤니티 허브에 보이게 하려면 **스탯 및 업적 → 순위표**에서 `ENDLESS_BEST` 의 표시 이름 · "커뮤니티에 표시"를 켜고 게시
  - 미리 만들 때는 이름 `ENDLESS_BEST` · 정렬 **내림차순** · 표시 **시간(초)** · "쓰기 권한" 은 **신뢰됨 아님(클라이언트에서 쓰기)** 그대로 (게임이 올림) — 정렬 · 표시를 다르게 만들면 게임이 그 설정을 따르므로 꼭 같게
  - 표시 이름 예: 무한 모드 최고 생존 / Endless — Longest Survival, 바꾼 뒤 **게시**해야 적용
- [ ] **상점 페이지**: [store-page.md](store-page.md) 글, `art/` 이미지 올리기
- [ ] **가격** 정하기 · **출시일** 정하기
- [ ] **콘텐츠 설문 / 연령 등급** (폭력성: 판타지 몬스터와의 전투, 피 표현 약함)
- [ ] **지원 언어**: 한국어, 영어, 일본어, 중국어 간체 (인터페이스 · 자막 ✔, 음성 ✘)

### 4. 빌드 올리기
```powershell
pwsh tools/steam-upload.ps1 -Version v2.4 -Preview   # 먼저 미리보기 (실제로 안 올림)
pwsh tools/steam-upload.ps1 -Version v2.4            # 업로드 (처음엔 비밀번호 · Steam Guard 코드 입력)
```
- [ ] Steamworks → SteamPipe → 빌드에서 올린 빌드를 **default** 브랜치로 설정
- [ ] 스팀 클라이언트에서 설치 → 실행 → 오버레이(Shift+Tab) · 업적이 뜨는지 확인

### 5. 검토와 출시
- [ ] **"곧 출시" 페이지 공개**: 출시 최소 2주 전에 공개해야 합니다
- [ ] **상점 페이지 검토 요청** · **빌드 검토 요청** (보통 3~5 영업일)
- [ ] 검토 통과 후 출시 버튼

## ⚠️ 라이선스 확인 필요

판매용 게임은 모든 그림 · 소리 · 폰트를 상업적으로 써도 되는지 확인해야 합니다.

| 에셋 | 상태 |
|---|---|
| 캐릭터 스프라이트 (Trevor Pupkin 팩) | ✅ 상업 사용 가능 (`Assets/Sprites/Read Me.txt`) · 팩 자체 재배포만 금지 |
| Noto Sans JP / SC | ✅ SIL OFL (`Assets/Resources/Fonts/OFL-NotoSansCJK.txt`) |
| LiberationSans (TextMesh Pro 기본) | ✅ SIL OFL |
| 배경음악 5곡 · 코드로 만든 효과음 | ✅ 이 프로젝트에서 직접 만듦 |
| **카페24 단정해 폰트** | ❓ 받은 곳 · 라이선스 파일 확인 (카페24 폰트는 보통 상업 무료) |
| **효과음 mp3 5개** (`Assets/Sounds/`) | ❓ 출처 확인 필요 — 모르면 바꾸는 게 안전 |
| 지옥 · 초원 적, 타일, UI, 이펙트 스프라이트 | ❓ 직접 그린 것인지 받은 것인지 확인 |

## 이미지 규격 · 올릴 곳

### 다시 만들기
```powershell
py -3 tools/steam_art/make_store_art.py                      # 캡슐 · 라이브러리 · 아이콘 (정식판 + 체험판), steam_assets/ 도 갱신
pwsh tools/steam/store-art/shoot.ps1 -Publish                 # 스크린샷 4개 언어 촬영 → art/screenshots, art/demo/screenshots
pwsh tools/steam/store-art/shoot.ps1 -Lang en -Only 01,06     # 일부 장면만 빨리 확인 (옮기지 않음)
```
- 캡슐: 로고(게임 이름) 말고는 글자 없음 (수상 · 리뷰 · 할인 문구 금지 규칙). 체험판은 로고 아래 붉은 「DEMO」 리본.
- 라이브러리 히어로: 글자 없음. 가운데 띠에 주인공 · 괴물 떼 · 보스, 왼쪽은 로고가 얹힐 자리라 비워 둠.
- 스크린샷 촬영 도구는 메인 프로젝트 · 빌드 복사본이 아닌 `%LOCALAPPDATA%\SoulSaverStoreShots` 복사본(제품 이름 `Soul Saver StoreShots`)에서 돌아서 실제 저장과 섞이지 않음.

### 정식판 앱 (App ID 5328770) — `art/capsules/`

| 파일 | 크기 | Steamworks 칸 | 그림 |
|---|---|---|---|
| `header_capsule.png` | 920×430 | 상점 페이지 관리 → 그래픽 에셋 → **헤더 캡슐** | 로고 왼쪽 위, 거너 · 검사 · 궁수 · 유령 vs 지하 묘역 괴물 떼 · 지옥의 군주 · 리치 왕 |
| `small_capsule.png` | 462×174 | 그래픽 에셋 → **소형 캡슐** | 로고를 가장 크게, 아래에 사격하는 거너 vs 괴물 떼 |
| `main_capsule.png` | 1232×706 | 그래픽 에셋 → **메인 캡슐** | 헤더와 같은 구도를 크게 |
| `vertical_capsule.png` | 748×896 | 그래픽 에셋 → **세로 캡슐** | 두 줄 로고, 리치 왕 · 킹 슬라임 · 지옥의 군주, 영웅 다섯 |
| `page_background.png` | 1438×810 | 그래픽 에셋 → **페이지 배경** (선택) | 영혼 화로가 놓인 어두운 묘역 바닥 (글자 · 인물 없음) |
| `library_capsule.png` | 600×900 | 라이브러리 에셋 → **라이브러리 캡슐** | 세로 캡슐과 같은 구도 |
| `library_header.png` | 920×430 | 라이브러리 에셋 → **라이브러리 헤더** | 헤더 캡슐과 같음 |
| `library_hero.png` | 3840×1240 | 라이브러리 에셋 → **라이브러리 히어로** | 영웅 아홉 명 vs 괴물 떼 · 네 보스 (글자 없음) |
| `library_logo.png` | 1280×720 (투명) | 라이브러리 에셋 → **라이브러리 로고** | 두 줄 도트 로고 (가로 1278) |
| `community_icon.jpg` | 184×184 | 앱 관리 → 일반 → **커뮤니티 아이콘** | 거너 전신 |
| `client_icon.ico` / `.png` | 16·32·48·256 / 32×32 | 앱 관리 → 일반 → **클라이언트 아이콘** (.ico) · 바탕 화면 바로 가기는 `shortcut_icon_512.png` | 거너 상반신 (투명) |
| `shortcut_icon_512.png` | 512×512 | 앱 관리 → 일반 → **클라이언트 바로 가기 아이콘** (필요할 때) | 거너 전신 |
| `art/screenshots/*.png` | 1920×1080 | 상점 페이지 관리 → **스크린샷** (영어 파일이 기본, `_koreana` · `_japanese` · `_schinese` 는 언어별 스크린샷 칸) | 아래 표 |

### 체험판 앱 (App ID 5369030) — `art/demo/`
같은 이름 · 같은 칸에 올립니다 (체험판 앱의 Steamworks 페이지). 그림체는 정식판과 같고, 체험판에서 실제로 보이는 것만: 거너 · 검사 · 1장 지하 묘역 적(해골 · 좀비 · 망령) · 리치 왕, 로고 아래 「DEMO」.

| 파일 | 크기 | Steamworks 칸 |
|---|---|---|
| `header_capsule.png` · `small_capsule.png` · `main_capsule.png` · `vertical_capsule.png` · `page_background.png` | 정식판과 같음 | 그래픽 에셋 (같은 이름의 칸) |
| `library_capsule.png` · `library_header.png` · `library_hero.png` · `library_logo.png` | 정식판과 같음 | 라이브러리 에셋 (로고에도 DEMO 리본) |
| `community_icon.jpg` · `client_icon.ico` · `shortcut_icon_512.png` | 정식판과 같음 | 앱 관리 → 일반 (커뮤니티 아이콘 · 바로 가기에 DEMO 리본) |
| `screenshots/*.png` | 1920×1080 | 스크린샷 (아래 표의 체험판 열) |

### 스크린샷 (1920×1080, 영어 기본 + `_koreana` · `_japanese` · `_schinese`)

| 파일 | 장면 | 정식판 | 체험판 |
|---|---|---|---|
| `01_gunner_catacombs_horde` | 거너 · 지하 묘역, Lv 21 · 2차 진화 무기 · 카드 여럿, 몰려오는 괴물 떼에 필살기 | ✔ | |
| `02_level_up_cards` | 레벨업 카드 3택 (진화 재료 표시) | ✔ | |
| `03_weapon_evolution` | 무기 진화 3택 화면 | ✔ | ✔ (같은 파일) |
| `04_skill_evolution` | 스킬 진화 「천둥 장전」 연출 | ✔ | ✔ (같은 파일) |
| `05_soul_tree` | 영혼 트리 | ✔ | ✔ (같은 파일) |
| `06_swordsman_lich_king` | 검사 vs 리치 왕 | ✔ | |
| `07_archer_demon_lord_barrier` | 궁수 · 지옥의 군주 필살기 결계 | ✔ | |
| `08_cartographer_king_slime` | 지도 제작자 vs 킹 슬라임 (초원) | ✔ | |
| `09_ghost_mirror_sovereign` | 유령 vs 거울의 군주 (영혼의 심연) | ✔ | |
| `10_endless_developer` | 무한 모드 25분 · 최종 보스 「개발자」 (우체부) | ✔ | |
| `11_nine_heroes` | 캐릭터 선택: 영웅 아홉 명 | ✔ | |
| `12_skin_shop` | 스킨 상점 | ✔ | |
| `demo/screenshots/01_gunner_catacombs` | 체험판: 진화 전 거너 · 지하 묘역 | | ✔ |
| `demo/screenshots/02_level_up_cards` | 체험판: 레벨업 카드 | | ✔ |
| `demo/screenshots/06_swordsman_lich_king` | 체험판: 검사 vs 리치 왕 | | ✔ |
| `demo/screenshots/07_gunner_lich_barrier` | 체험판: 거너 · 리치 왕 결계 | | ✔ |

### 그 밖
| 파일 | 크기 | 칸 |
|---|---|---|
| `art/achievements/*.jpg` | 256×256 | 업적 아이콘 (달성 / 미달성) |
| `steam_assets/*` | 예전 파일 이름 | 보도 자료(`docs/marketing/press-kit.md`)가 가리키는 사본 — `make_store_art.py` 가 새 그림으로 덮어씀 |
