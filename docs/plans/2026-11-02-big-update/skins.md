# 스킨 — 지도 제작자 · 시계공 · 우체부 · 유령 (2.0.0)

콘셉트: concept.md 「스킨」. 기존 다섯 영웅 스킨과 **같은 규격 · 같은 경로 · 같은 급**으로 만든다. 따라 할 예시는 도적(rogue) 스킨 전부.

## 목록 (id = 시트 이름 = 저장 키)
| 영웅 (who) | 일반 (tier 0) | 희귀 (1) | 영웅 (2) | 전설 (3) | 무기 희귀 · 영웅 (held) |
|---|---|---|---|---|---|
| 지도 제작자 5 | `cartographer__drafter` 제도사 | `cartographer__hydro` 해도사 | `cartographer__starmap` 별지도꾼 | `cartographer__blank` 백지도의 저자 | `quill__brush` 붓 · `quill__chalk` 분필 |
| 시계공 6 | `clockmaker__apprentice` 견습 | `clockmaker__hourglass` 모래시계 수도사 | `clockmaker__cuckoo` 뻐꾸기 시계 | `clockmaker__stopped` 멈춘 시계의 주인 | `gear__coin` 금화 · `gear__abacus` 주판알 |
| 우체부 7 | `postman__paperboy` 신문 배달 | `postman__pigeon` 비둘기 조련사 | `postman__courier` 택배 기사 | `postman__return` 반송 우체부 | `letter__plane` 종이비행기 · `letter__bill` 청구서 |
| 유령 8 | `ghost__sheet` 이불 유령 | `ghost__wisp` 도깨비불 | `ghost__deepsea` 심해 유령 | `ghost__deleted` 삭제된 장면 | `lantern__candle` 촛대 · `lantern__chain` 쇠사슬 등불 |

이름 · 한 줄 설명은 concept.md 의 묘사를 30자 안팎 한국어 한 문장으로 (기존 SkinData 설명 말투). 4개 언어 번역 필수.

## 할 일
1. **32칸 몸 시트** `Assets/Resources/Characters/<id>.png` (+ `.meta`): 각 영웅 기본 시트(`cartographer.png` 등, 192×32 = 6프레임)를 원본으로 `tools/pixelart/make_skins.py` 방식(팔레트 바꾸기 + 장식 줄 덮어쓰기)으로. 새 영웅 넷은 `make_characters.BODIES` 에 없으므로 **기본 시트 PNG 를 읽어 색 묶음을 바꾸는 함수**(거너 `gunner_nogun` 처리와 같은 방식)를 make_skins.py 에 더함. `.meta` 는 기본 시트 `.meta` 를 복사하고 guid 만 새로(스프라이트 이름 · 잘림은 그대로 → 애니메이션 이름 맞음). 다시 돌리면 guid 유지.
   - 등급이 오를수록 실루엣 차이도 커지게 (전설은 모자 · 장식 · 재질이 확 달라 보이게). 영웅의 정체(실루엣 핵심)는 지킴.
2. **무기 그림** `Assets/Resources/Weapons/weapon_<held>__<스킨>.png`: make_skins.py `WEAPON_SKINS` 방식 (기본 무기에서 색 · 모양 손질).
3. **메뉴 초상 128칸** `tools/pixelart/hires/heroes/<id>.py` 16개 + `__init__.py` `MODULES` 등록 → `py -3.14 -X utf8 tools/pixelart/make_menu_portraits.py <id…> --lint` 로 `Assets/Resources/Characters/Menu/<id>.png`. `hires/heroes/README.md` 규칙 전부, 기존 `rogue__*.py` 처럼 기본 영웅 모듈(`cartographer.py` 등)을 바탕으로 `skin.py` 붓으로 바꿈. lint 전 항목 통과.
4. **SkinData.All** (`Assets/Scripts/SkinData.cs`) 에 24줄 (`C(...)` · `W(...)` 팩토리, who = 5~8). 색 = 그 스킨의 테마 색.
5. **상점 차단 풀기**: `SkinShopUI.cs` 의 `IsNewHero` 차단(44 · 78 · 85 근처)을 「그 영웅의 스킨이 하나라도 있으면 보임」으로. 스킨이 없는 영웅(앞으로 올 새 영웅 중 아직 없는 경우)만 「준비 중」.
6. **전설 연출** (`SkinFx.cs`): 전설 4개 각각 — 오라(BuildAura 의 색 · 모양 갈래), 처치 연출(OnKill 갈래: 종이 조각 · 톱니 · 소인 봉투 · 픽셀 흩어짐), 필살기 소리(`SkinAudio.UltSound` 갈래 + `Register` 에 합성 소리 4개: 「끼이익—쫘아악」 · 「뎅——」 · 「쿵쿵쿵—딩」 · 「삐——걱」). 기존 전설 처리(검사 dragon 등)를 그대로 본뜸.
7. **무기 스킨 소리**: `SkinAudio.IsAttack` (SkinFx.cs:276) 이 처음 다섯 영웅 공격 소리만 앎 → 새 영웅 넷의 평타 소리 이름도 알게 해서 무기 스킨 음높이 · 덧소리가 먹게.
8. **번역**: 새 파일 `Assets/Scripts/LocTable.Skins2.cs` (`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` + `S(ko,en,ja,zh)` 패턴, LocTable.Perf.cs 처럼) — 이름 24 · 설명 24.

## 확인
- `py -3.14 -X utf8 tools/pixelart/make_menu_portraits.py <16개> --lint` 통과
- `pwsh -NoProfile -File tools/compile-check.ps1` 통과
- AGENT.md 규칙 2 (게임 스크립트에 UnityEditor 금지)
