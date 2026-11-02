# 2.0.0 구조 검토 — 「버그는 아니지만 구조상 불가능한 요소」 (2026-11-02)

읽기 전용 검토 (코드 수정 없음). 기준은 기획 문서가 아니라 실제 코드.
제외: 영혼 릴레이 모드 (`Modes/RelayRun.cs`) · 사진사 갈래 (`Paths/*Photographer*`) — 다른 작업자가 작업 중.

요약: **치명 0 · 높음 0 · 중간 4 · 낮음 10** (확실 9 · 의심 5).
영영 못 얻는 영웅 · 스킨 · 계약 · 진화는 찾지 못함. 문제는 「거의 안 되는 것」, 「겉보기와 다른 것」, 「설정에 따라 사라지는 것」 쪽.

---

## 1. 확실한 문제

### [중간] 특수효과 「꺼짐」이면 사진사 비밀의 「찰칵!」 소리가 영영 안 남
- 위치: `Content/DesertSecrets.cs:890` (`Hostile.Play("imp_shutter")`), `BossSkills.Photographer.cs:38 · 56 · 82`, `Impact.cs:63 · 86`
- 원인: `imp_*` 합성음은 `Impact.Sounds.Register` 로만 만들어짐. 이 함수는 `Impact.Play` 안에서만 불리고, `Off`(특수효과 꺼짐) · 쉬는 시간이면 그 전에 빠져나감. `SpecialFeedback.PlayRaw` 는 없는 클립이면 조용히 무시함.
- 결과:
  - 「꺼짐」이면 사진사 비밀 달성 신호(찰칵), 사진사 보스 흉내의 셔터 · 커튼 소리가 늘 무음.
  - 다른 설정이어도 그 장면에서 Impact 가 한 번도 안 터졌으면 무음.
  - 비밀 달성 신호는 화면 가장자리 0.1초 섬광과 화면 밖 NPC 뿐이라 놓치기 쉬움. 30초 안에 못 가면 처음부터 다시.
- 고치는 법: `SpecialFeedback` 이 만들어질 때(또는 `DesertSecrets.Begin`) `Impact.Sounds.Register(fx)` 를 무조건 한 번 부름.
- → 고침 (`SpecialAbilities.cs` Start 에서 `Impact.Sounds.Register(fx)` 를 장면마다 한 번 — 꺼짐이어도 imp_* 소리가 남음)

### [중간] 도감에서 새 영웅 특수 능력 아이콘이 다른 영웅 그림으로 나옴
- 위치: `CodexUI.cs:685` (1차 진화 칸), `CodexUI.cs:1033` (`KitSpecials`)
- 원인: `"Icons/ability_" + id` 를 그대로 씀. 실제 규칙은 `SpecialAbilities.KitIconPath(id)` (60번 이상은 400 + id · `SpecialAbilities.Kits.cs:32`).
- 결과:
  - 140~174 번이 `ability_140~174.png` 를 읽음 (도적 · 궁수 · 연금술사 고유 스킬, 거너 스킬 진화 그림). 파일이 있어서 빈칸이 아니라 엉뚱한 그림이 보임.
  - 예: 씨름꾼 「들돌」 → 도적 「연막」 그림.
  - 1.0.2 영웅(60~87)도 같은 버그.
  - 게임 안 HUD 는 정상.
- 고치는 법: 두 줄을 `Resources.Load<Sprite>(SpecialAbilities.KitIconPath(id))` 로.
- → 고침 (`CodexUI.cs` 두 곳을 `SpecialAbilities.KitIconPath(id)` 로 — 1.0.2 영웅 60~87 도 함께)

### [중간] 영웅 해금 스팀 업적 5개는 그 순간을 놓치면 다시 받을 길이 없음
- 위치: `Content/DesertSecrets.cs:172-174`, `SteamManager.cs:348-359` (`SteamAchievements.Unlock`)
- 원인:
  - `UNLOCK_*` 는 「처음 해금하는 그 순간」에만 부름. 다음 판부터는 `IsUnlocked` 라서 그 분기에 다시 들어가지 않음.
  - 스팀 미연결(오프라인 · 초기화 실패) 상태면 `Unlock` 이 `done` 에 넣고 아무것도 안 함.
  - 스킨은 `CheckOwnedSkins` 로 장면마다 다시 맞추지만 영웅 해금은 그런 동기화가 없음.
- 결과: 오프라인에서 비밀을 푼 사람은 그 업적을 영영 못 받음. 업적 ID(`UNLOCK_*`, 다른 업적은 `ACH_*`)가 파트너 사이트 등록과 다르면 모두 못 받음 (등록은 사용자 몫).
- 고치는 법: `SteamManager.OnSceneLoaded` 에서 `DesertSecrets.Defs` 를 돌며 `IsUnlocked(d.hero)` 면 `Unlock(d.achievement)` (CheckOwnedSkins 와 같은 방식).
- → 고침 (`SteamManager.cs` `CheckUnlockedHeroes` — 장면마다 해금한 비밀 영웅의 `UNLOCK_*` 를 다시 맞춤 · 이미 받은 것은 `Unlock` 이 그대로 둠)

### [낮음] 계약 보드 「아홉 개의 쉬움」 · 「아홉 손」: 글은 「모든 영웅」인데 목표는 9명 그대로
- 위치: `ContractBoard.cs:64 · 80` (target 9, 세는 범위 `Cols` 14), `LocTable.Contract.cs:433-435 · 478-480`
- 불가능하지는 않음 (14명 중 9명이면 됨). 다만 조건 글 「모든 영웅으로 쉬움 클리어」 · 「모든 영웅 숙련 3」과 실제 판정이 다름. 14로 올리면 비밀 영웅 5명도 해금 → 클리어해야 해서 어렵지만 가능.
- 고치는 법: 목표를 9로 둘지 14로 할지 정하고, 글이나 target 을 맞춤 (이름 「아홉」도 함께).
- → 고침 (`LocTable.Contract.cs` 조건 글을 「영웅 아홉 명」으로 4개 언어 · 목표 9 그대로)

### [낮음] 번역 누락 1건: 공사 반장 카드 「자재 회수」의 수치 이름 「확률」
- 위치: `LevelShop.NewHeroes.cs:202` (`Card("자재 회수", …, "확률", "|10%|", …)`)
- 영어 · 일본어 · 중국어 화면에 「확률」이 한국어로 보임. 「회수 확률」 키는 있음.
- 고치는 법: 「확률」 키를 4개 언어로 추가하거나 「회수 확률」로 바꿈.
- → 고침 (`LocTable.Foreman.cs` 「확률」 4개 언어 추가)
- 참고: 전수 조사 결과 (리터럴 1,114곳 · 래퍼 501곳 · 번역표 5,801키 · 동적 키 범위)에서 2.0.0 새 내용 누락은 이것 하나. 일본어 · 중국어 빈 칸 0. 영어 빈 칸 6개는 모두 일부러 비운 단위.

### [낮음] 번역 중복 등록: 새 파일에 쓴 번역 일부가 무시됨
- 원인: 번역표 등록이 `if (!ContainsKey)` 라서 먼저 넣은 쪽이 이김.
- 사례:
  - 「게이지」: 인형사 파일의 중국어 「必杀技槽」 → 「充能」으로 나옴
  - 「묶는 시간」: 장의사 파일 「Bind time」 → 「Root time」
  - 「최대 체력」 (씨름꾼), 「도깨비불」 (Skins2) 등
- `BeforeSceneLoad` 메서드끼리는 실행 순서가 정해져 있지 않아서, 새 파일끼리 겹친 키는 어느 쪽이 이길지 알 수 없음.
- 목록: scratchpad `dups.txt` (아래 부록).
- 고치는 법: 겹치는 키는 한 곳에만 두거나, 영웅별로 다른 뜻이면 키를 나눔.
- → 문제 아님 (겹친 키를 모두 대조: 한 곳이 이겨도 뜻이 같은 동의어 — Bind/Root time, Gauge/充能, Crack/Fracture 등. 뜻이 달라지는 키는 없어 그대로. 사진사 갈래 파일 몫은 제외)

### [낮음] Impact: 건너뛴 연출도 쉬는 시간을 써 버림
- 위치: `Impact.cs:63-67 · 107`
- 원인: `lastAt[ti] = now` 를 먼저 적음. 그 뒤 터지는 순간 `timeScale == 0`(레벨업 창)이면 소리 · 흔들림 없이 끝남.
- 결과: 평타 3초 · 진화 2.5초 동안 다음 연출도 안 나옴. 레벨업 창이 자주 뜨는 구간에서 손맛이 「가끔 안 터지는」 느낌.
- `CanSlow` 가 `timeScale == 1` 일 때만 참이라, 필살기 조준처럼 시간이 느린 동안 터진 연출은 슬로우 · 줌이 늘 빠짐. 의도된 것으로 보임.
- 고치는 법: 실제로 터졌을 때만 `lastAt` 을 적거나, 건너뛰었으면 `lastAt` 을 되돌림.
- → 고침 (`Impact.cs` 터지기 전에 건너뛰면 `lastAt` 되돌림)

### [낮음] 장의사 묘비: 기획은 「장의사가 아닌 영웅」인데 코드는 아무 영웅이나 해금
- 위치: `Content/DesertSecrets.cs:1025-1032`
- 영향: 장의사는 해금 전에 고를 수 없어서 실제 차이는 없음. 영혼 릴레이(제외 대상)가 붙으면 다시 볼 것.
- 쓰러짐을 가로채는 경로:
  - `LastStand` · 불사의 맹세는 `IsDying` 이전에 막을 뿐이라, 다음 진짜 쓰러짐에서 판정됨.
  - `IsDying` 이 된 뒤 되살아나는 경로는 없음 (`PlayerController.cs:1549-1574`).
  - `dyingDone` 이 판당 한 번이라도 문제 없음.
  - 포기(ESC) · 일일 보상 종료는 `IsDying` 이 아니라 판정되지 않음 (기획대로).
- 고치는 법: 지금은 그대로. 릴레이 붙일 때 기획 문구와 맞춤.
- → 고침 (`DesertSecrets.cs` 묘비는 `CharacterData.Selected == hero` 이면 판정 안 함)

### [낮음] `Herald.Forced` 가 전령을 끈 사람에게는 다음 판까지 남음
- 위치: `Content/DesertSecrets.cs:947 · 1036`, `Herald.cs:234-236`
- 원인: 전령이 꺼져 있으면(`!Enabled`) `Forced` 가 지워지지 않음.
- 결과: 사진사를 만난 판의 줄이 나중에 전령을 다시 켰을 때 엉뚱한 판에 나옴.
- 고치는 법: `GameScene` 로드 때(`DesertSecrets.Begin`) `Herald.Forced = null`.
- → 고침 (`Herald.cs` `Play` 에서 꺼짐 · 이미 보임이면 `Forced` 비움)

### [낮음] 스킨 상점 미리듣기 · 평타 반동: 새 영웅 일부가 기본값으로 떨어짐
- 위치: `SkinShopUI.cs:370` (`BaseAttack`: 인형사 · 장의사 · 사진사가 `_ => "gunshot"`), `PlayerLook.cs:121 · 125` (`BaseRecoil` / `BaseLift`, 1.0.2 영웅도 같음)
- 기능 영향은 없음. 무기 스킨 미리듣기가 총소리로 날 수 있음.
- 고치는 법: case 추가 (`SkinFx.cs:459` 의 소리 이름 whoosh / dig · bury / shutter 와 맞춤).
- → 고침 (`SkinShopUI.cs` `BaseAttack` 에 새 영웅 소리 이름 추가 · `PlayerLook.cs` 반동 · 들림에 새 영웅 5명 값)

---

## 2. 의심 (확인 필요)

### [중간] 사진사 「북적이는 장면」: 3분 ~ 15분 사이는 「한 화면 상한 거의 전부를 화면 안에 살려 둔 채 12초」라서 매우 빡빡함
- 위치: `Content/DesertSecrets.cs:783 · 801-802`, `EnemySpawn.cs:79-85 · 558-580`, `EndlessMode.cs:802-806`, `GameMode.cs:223`
- 계산 (무한 모드는 `Chapters.IsClassic` 이라 장 배율 1, `ExtraAlive` 2, `MobRamp` 는 10분 전까지 더하는 수 0~2):

  | 시점 | MaxAlive (국면 alive + 더하는 수) | 필요 수 = clamp(상한 − 4, 12, 18) | 비율 |
  |---|---|---|---|
  | 3:00 (4국면) | 14 + 2 = 16 | 12 | 75% |
  | 5분대 | 18 ~ 20 | 14 ~ 16 | 78~80% |
  | 8분 뒤 | 22 | 18 | 82% |

  - 15:00 (최종 국면) 뒤에는 `Surge` 로 상한이 크게 늘어 쉬움.
- 왜 어려운가:
  - 적은 화면 테두리 바깥 2.5~14칸에서 생겨 걸어 들어옴. 경기장 배율 1(Legacy)이면 맵 아무 데서나 생겨 더 어려움.
  - 판정은 화면에서 1칸 안쪽만 셈.
  - 원거리 적은 거리를 둠.
  - 그동안 거의 죽이지 않고 맞으면서 버텨야 함.
  - 기준 아래로 떨어지면 노출이 0.5배속으로 줄어듦.
- 다만 습격(`EnemySpawn.cs:348`)은 상한을 넘겨 나오고 중간 보스 · 보스 부하도 세므로, 그때는 됨. 실측 필요.
- 고치는 법: 필요 수를 상한 비율로 (예 `clamp(round(MaxAlive × 0.6), 10, 18)`), 또는 판정 범위를 화면 + 3칸으로.
- → 고침 (`DesertSecrets.cs` 기준 수 `clamp(MaxAliveNow − 6, 10, 16)` 상수로 · 자동 조작 실측으로 다시 볼 것)

### [낮음] 비밀 자리가 벽 태그 구조물 · 다른 소품과 겹치는지 확인 안 함
- 위치: `Content/DesertSecrets.cs:118-129` (`ClampArena` 만 함)
- 지형 조각은 `EndlessTerrain.cs:816` 에서 비켜 감. 장식 소품은 충돌이 없어 문제 없음.
- 그러나 다음은 비켜 가지 않음:
  - 복제한 지옥 맵에 `Wall` 태그 구조물이 남아 있을 경우
  - 상점 노점 · 체력 팩 · 제단 · 습격 이벤트가 생기는 자리
- 황소를 밀 원(반지름 4.5) 안에 충돌체가 생기면 황소는 통과하지만 플레이어가 막힐 수 있음.
- 고치는 법: `Build` 에서 `Hostile.WallNear(at, ClearRadius)` 면 경고 로그. 노점 · 팩 생성도 `DesertSecrets.NearAny` 를 보게.
- → 고침 (`DesertSecrets.cs` `ClearSpot` 으로 벽 · 용암에 겹치면 가까운 빈 자리 · `Shop.cs` 노점 · `MapProps.cs` 소품 자리가 `NearAny` 를 봄. 체력 팩은 따로 생성 지점을 못 찾아 손대지 않음)

### [낮음] 사진사 사진사 만남 자리가 지형 조각 · 용암 위일 수 있음
- 위치: `Content/DesertSecrets.cs:886-889`
- 화면 밖 3칸 자리를 `ClampArena` 만 하고 만듦. 막히는 일은 드묾 (만남 반경 3.5칸).
- 고치는 법: `WallNear` 면 화면 가장자리 쪽으로 몇 칸씩 당겨 다시 찾음.
- → 고침 (`DesertSecrets.cs` 막히면 화면 가운데 쪽으로 최대 6번 당겨 다시 찾음)

### [낮음] 공사 반장 갈래: 더미 최대치에 갈래 보정이 두 번 걸림
- 위치: `CharacterKit.Foreman.Paths.cs:295` (`FmPathPileMax(FmPileMax)` — `FmPileMax` 가 이미 `FmPathPileMax` 를 거침 · `CharacterKit.Foreman.cs:78`)
- 갈래 불도저에서 그 경로의 상한이 의도보다 큼. 불가능 쪽은 아님.
- 고치는 법: 295줄은 `FmPileMax` 만.
- → 고침 (`CharacterKit.Foreman.Paths.cs:295` 를 `FmPileMax` 만)

### [낮음] 처음부터 열린 각인 4종의 `.hint` 키가 번역표에 없음
- 키: `brand.glass/tide/giant/iron.hint`
- `Hint` 는 잠긴 각인에서만 보여 화면에 나올 일이 없음. 기록만.
- → 문제 아님 (화면에 나올 일이 없음 · 그대로)

---

## 3. 확인함 (문제 없음)

**영웅 해금**
- 비밀 영웅 5명 모두 `UnlockSecret` 경로 있음 (`DesertSecrets.cs:113-117 · 172`).
- `Buy` 는 `price <= 0` 이라 비밀 영웅을 살 수 없음 (의도). 고르기 화면은 해금 전 ???.
- `GameInput.Auto` 는 트레일러 전용 (`TrailerDirector.cs:78`) → 일반 플레이어는 비밀이 꺼지지 않음.
- 무한 모드 선행 조건(보통 클리어)만 있으면 사막 비밀 모두 도달 가능.

**업적형 전설 스킨 5개**
- `CA(...)` 는 비밀 영웅 5명에게만 있음.
- `UnlockLegend` (`DesertSecrets.cs:189 · 194-205`) 가 「이미 해금 + 그 영웅으로 다시 풂」에서 열어 줌 → 5개 모두 도달 가능.
- 전설 업적 `ACH_SKIN_LEGEND` 는 `CheckOwnedSkins` 로 받음.

**사막 비밀 판정**
- 황소: 플레이어 속도의 35% 로 밀림 → 약 2초에 원 밖. 되밀기는 플레이어만 밀고 황소는 그대로.
- 표지판:
  - 평타 신호 `AttackFired` 가 14명 모두에서 남 (공용 `CharacterKit.Attack` · 공사 반장 · 장의사 · 사진사 별도 발신).
  - 필살기는 `UltUsed`.
  - 0.4초에 한 대, 8대.
- 더미: 게임패드는 `GameInput.MousePosition` 이 조준 스틱 자리를 줌.
- 묘비: `IsDying` 직후 한 번 판정. 죽는 슬로 중에도 `Update` 가 돎.
- 위치: 경기장 197×125 와 109×69 두 배율 모두 안쪽 (k 0.5~1.2 반영). 성문에서 13칸 넘게 떨어짐.
- 보스전: 판정이 막히지 않음. 개발자 · 엄마전(`NoMobs`)만 사진사 판정 멈춤.
- 최후의 시간: 막지 않음.
- 일일 도전: 해금 없이 보너스 점수 (`DailyChallenge.cs:172`).

**계약**
- 새 영웅 25개 계약의 카운터 키가 모두 실제로 올라감 (`throw · ringout · strike · b.dump · b.pile.max · b.crash · b.crushkill · pup.cut · pup.swap.cut3 · pup.sweep5 · bury · bury.hole · grave · photo.masterpiece · paper · print.tear · photo.withboss · ult`).
- 목표가 상한보다 큰 것 없음:
  - 스트라이크 6핀: 무거운 적 기본 8핀
  - 무게 15 더미: 기본 12 + 긴 삽날 1단계 3
  - 한 구멍 6: 구멍 수용 2 + 3 × 반지름 2 = 8

**계약 보드**
- 영웅 14열 × 3줄 모두 키와 목표 있음. 2줄 보스 4종은 이야기 모드 처치로 비트가 켜짐.
- 일일 도전 영웅은 `IsNewHero` 로 0~4번만 → 보드 `GrantOne` 이 비밀 영웅을 미리 채우지 않음.

**레벨업 카드 · 진화**
- 전용 카드 6장 · 고유 스킬 10종 · 진화 7종 × 5명: 재료가 모두 그 영웅 카드. 「요구 단계 > 최대 단계」 없음.
- 갈래 뒤에도 다른 갈래 재료가 막히지 않아 교차 가능 (`LevelShop.Paths.cs:126`).

**아이콘**
- 쓰이는 번호(540~574 · 612~626 · 740~785 · 840~889 · 940~986)는 모두 파일이 있음. 빈 번호는 쓰지 않는 끝자리.

**특수 능력**
- `KitDefs2` 140~174 와 영웅 pool(무기 3 + 패시브 4) 일치. 형태 · 궁극기 · 2차 강화 124~138 모두 있음.

**영웅 수 배열 · switch**
- `(int)CharacterId` 인덱싱은 모두 14칸이거나 길이를 검사함.
- 영웅 switch 는 새 영웅이 다 들어 있음 (공사 반장 · 장의사 공격은 `FmLate` · `UtLate` 가 대신).
- `Clamp(..., Length - 1)` 로 다른 영웅 값을 조용히 쓰는 곳은 찾지 못함.

**Impact 표**
- `HeroImpacts.Get` 14명 모두 네 단계가 있음.
- 「꺼짐」은 `Impact.Off` 로 깔끔히 꺼짐 (소리 등록 문제는 위 1번 항목).

---

## 부록: 번역 전수 조사 산출물 (scratchpad, 저장소 밖)
`C:\Users\User\AppData\Local\Temp\claude\C--Users-User-Desktop-Unity-Soul-Saver\89392a5e-120e-444d-9c7e-bc7972d0781e\scratchpad\`

| 파일 | 내용 |
|---|---|
| `missing.txt` | 누락 리터럴 |
| `checks.txt` | 빈 칸 · 점 키 · 자리표시 검사 |
| `dups.txt` | 중복 등록 |
| `dynamic.txt` | 변수 인자 `Loc.T` |
| `orphans.tsv` | 표에 없는 한국어 · 점 리터럴 (대부분 화면 밖) |

다시 돌리기: `table.py` → `usage.py` → `analyze.py`

작업 중 제외분 (참고만): 영혼 릴레이 키 4개 누락 (`relay.toast.handoff` · `relay.result.best` · `relay.result.newrecord` · 「영혼 릴레이」), 사진사 갈래 「연사」 · 「화력」 중국어가 본표 값에 밀림.
