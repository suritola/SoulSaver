# 새 모드 2개 — 영혼 릴레이 · 역류 (2.0.0 구체 기획)

콘셉트: `concept.md` 「모드」 · 큰 그림: `design.md` 7절 · 결정 D10. 구현자는 이 문서만 보고 옮길 수 있게 쓴다.
코드 위치는 2026-11-02 기준 줄 (구현 때 `git grep` 으로 다시 확인).

---

## 0. 공통 뼈대

### 0-1. 두 모드 모두 「일일 도전 패턴」을 그대로 쓴다
`DailyChallenge.Begin` 처럼 메뉴에서 `GameMode.Override = Difficulty.Endless` · `CharacterData.Override = <영웅>` 을 걸고 `SceneFlow.Go("GameScene")`, 메인 메뉴로 돌아오면(`sceneLoaded` "MainMenu") 풀린다.
→ `GameMode.IsEndless` 가 참이므로 **사막 맵 · 무한 스테이지 · 적 구성 · EndlessMode HUD · 순위표 제출 길(Finish)이 공짜로 따라온다.** 새로 만드는 것은 「모드가 다른 점」만.

### 0-2. 새 정적 클래스 `ModeRun` (새 파일 `Assets/Scripts/Modes/ModeRun.cs`, 약 60줄)
기존 코드가 `DailyChallenge.Active` 를 여기저기 따로 묻는 구조라, 모드가 둘 더 붙으면 조건이 세 배가 된다. 한 곳에서 묻게 한다.
```csharp
public enum ModeKind { None, Daily, Relay, Reflux }
public static class ModeRun
{
    public static ModeKind Kind => DailyChallenge.Active ? ModeKind.Daily : RelayRun.Active ? ModeKind.Relay : RefluxRun.Active ? ModeKind.Reflux : ModeKind.None;
    public static bool Special => Kind != ModeKind.None;          // 무한 모드 기록 · 이정표 · 무한 순위표에 섞지 않음
    public static bool Relay => RelayRun.Active;
    public static bool Reflux => RefluxRun.Active;
    public static void EndAll() { DailyChallenge.End(); RelayRun.End(); RefluxRun.End(); }   // 메인 메뉴 진입 (MenuExtras.InstallMainMenu)
}
```
- 셋은 **서로 배타**: 각 `Begin` 이 먼저 `ModeRun.EndAll()` 을 부른다.
- 기존 `DailyChallenge.Active` 확인은 지우지 않는다. 「무한 모드 기록과 섞지 않음」 뜻으로 쓰인 곳만 `ModeRun.Special` 로 바꾼다 (3절 표).

### 0-3. 「특별 모드」 메뉴 (새 파일 `Assets/Scripts/Modes/ModeHubUI.cs`, 약 320줄)
- 메인 메뉴 세로 목록(`MenuExtras` 183~196줄 `list = { codex, daily, settings, more, exit }`)에 **「특별 모드」 한 줄**을 일일 도전 바로 아래에 더한다 → `{ codex, daily, modes, settings, more, exit }`. (사용자가 「메인 화면 버튼이 많다」고 했으므로 두 모드를 한 줄로 묶음 — 결정 M1)
- 누르면 `ModeHubUI.Open(root)` — `UIKit.Modal` 1240×820, 왼쪽 카드 「영혼 릴레이」 · 오른쪽 카드 「역류」.
  - 카드: 그림(128칸, `Icons/mode_relay` · `Icons/mode_reflux`) · 이름 · 규칙 세 줄 · 내 최고 기록 · [고르기] (잠겨 있으면 회색 + 여는 조건 한 줄, `DailyChallenge.Open` 의 잠긴 버튼 처리 그대로).
  - 아래쪽 [순위 보기] → `LeaderboardUI.Open(root, Board.Relay / Board.Reflux, …)`.
- 새로 열린 모드가 있으면 「특별 모드」 줄에 `NewGlow` (열어 보면 꺼짐 · Prefs `mode.hub.seen.<relay|reflux>`).
- 체험판(`Demo.On`): 줄은 보이되 두 카드 모두 「정식판에서 만날 수 있습니다」.

### 0-4. 공통으로 고치는 작은 것
| 파일:함수 | 할 일 | 이유 |
|---|---|---|
| `SteamManager.cs` `SteamLeaderboards.Find` (516줄 `FindOrCreateLeaderboard`) | 정렬 인자 `bool ascending` 추가 (기본 false). 역류만 오름차순 | 지금은 내림차순 하드코딩 — 역류는 「짧을수록 좋음」 |
| `SteamLeaderboards` | `Relay(string combo)` · `RelayAll` · `Reflux` 이름 함수 | 순위표 이름 한 곳 |
| `LeaderboardUI.cs` `enum Board` | `Relay` · `Reflux` 탭 추가 (122~123줄 버튼 줄에 둘 더, Tab 순환) | 순위 창 재사용 |
| `MenuExtras.InstallMainMenu` (146줄) | `DailyChallenge.End()` → `ModeRun.EndAll()` | 메뉴 오면 모드 해제 |
| `MenuExtras.InstallGameOver` (108~132줄 「EndlessRecord」) | 모드별 기록 줄 (1-7 · 2-10) | 결과 화면 재사용 |
| `EndlessMode.Finish` (730줄) | `DailyChallenge.Submit` 앞에 분기: 릴레이 → `RelayRun.Submit`, 역류 → `RefluxRun.Submit`, 그 밖 → 지금 그대로 | 순위표 · 최고 기록을 무한과 섞지 않음 |

---

## 1. 영혼 릴레이

### 1-1. 한 줄
영웅 셋을 순서대로 골라 무한 사막에 들어간다. 쓰러지면 **그 자리 · 그 시각에 다음 영웅이 쓰러진 몸에서 일어난다.** 레벨 · 공용 카드 · 영혼 트리 공용 칸 · 코인 · 영혼 조각 · 무한 진행은 이어지고, 앞 영웅 전용 빌드는 사라지되 **「유품」 하나**가 남는다. 셋째가 쓰러지면 끝. 「앞 영웅의 빌드를 언제 현금으로 바꿀까(죽음을 언제 쓸까)」가 판 전체의 전략.

### 1-2. 열리는 조건 (D10)
`RelayRun.Unlocked => !Demo.On && GameMode.IsUnlocked(Difficulty.Endless) && OwnedHeroes >= 3`
- `OwnedHeroes` = `CharacterData.All` 중 `IsUnlocked(id)` 인 수 (처음부터 있는 거너 · 검사 · 도적 = 3이므로 사실상 「무한 모드 해금」 = 열림).
- 처음 열린 판단은 메인 메뉴에서: `Prefs relay.unlockNoticed` 가 0이고 Unlocked 면 `Notice.Big("새 모드 「영혼 릴레이」가 열렸다", …)` 한 번 + 「특별 모드」 NewGlow.

### 1-3. 메뉴 진입 UI — 「주자 고르기」 창 (`ModeHubUI.OpenRelay`)
```
┌ 영혼 릴레이 ───────────────────────────────── [닫기] ┐
│ [1번 주자: 거너] → [2번 주자: 유령] → [3번 주자: 씨름꾼]  │  ← 칸 3개 (초상 96칸 · 이름 · 「넘겨줄 유품: 마지막 탄창」)
│                                                      │
│ 영웅 격자 (5칸 × 3줄 · 영웅 고르기 화면과 같은 순서)        │  ← 가진 영웅만 밝게, 못 가진 영웅은 실루엣 · 누를 수 없음
│                                                      │
│ 이 조합 최고 기록 12:40 · 조합 순위 상위 5 (스팀)           │
│                    [출발]                             │
└──────────────────────────────────────────────────────┘
```
- 격자의 영웅을 누르면 비어 있는 가장 앞 칸에 들어감. 칸에 든 영웅을 다시 누르면 빠짐(뒤 칸이 당겨짐). 칸을 끌어서 순서 바꾸기는 하지 않음 (누르기만으로 충분 · 패드 조작 쉽게).
- 같은 영웅 두 번 불가 (격자에서 이미 든 영웅은 번호 배지 「1」 「2」 「3」).
- 칸 아래 작은 글: 그 영웅이 **넘겨줄** 유품 이름 · 한 줄 (3번 주자는 「마지막 주자」).
- 처음 열 때 기본값 = 지난 릴레이 조합 (`Prefs relay.last` = "0,8,9"), 없으면 지금 고른 영웅 + 가진 영웅 앞 순서 둘.
- [출발] 은 셋이 다 찼을 때만. 패드: 격자 이동 = 방향, A = 넣기/빼기, Y = 출발, B = 닫기.
- 조합 순위 상위 5: `SteamLeaderboards.Top(Relay(combo), 5, …)` — `DailyChallenge.BoardView` 를 일반화해 재사용(보드 이름만 인자로).
- [출발] → `RelayRun.Begin(order)`.

### 1-4. 규칙
**시작**
- `RelayRun.Begin(int[] order)`: `ModeRun.EndAll()` → `Active = true`, `Order = order`, `Index = 0`, `GameMode.Override = Endless`, `CharacterData.Override = order[0]`, `Prefs relay.last` 저장, `SceneFlow.Go("GameScene")`.
- 판은 무한 모드와 똑같이 시작 (레벨 10 시작 보너스 · 계약 · 사막 · 보스 순환 · 15분 최후의 시간 · 개발자 · 엄마 · 한계 돌파). 사막 비밀 해금도 됨 (desert-secrets 공통 규칙).
- HUD 맨 위 무한 글(EndlessMode.BuildHud)에 「영혼 릴레이 · 주자 1/3」, 화면 왼쪽 위 레벨 칸 옆에 **주자 띠** (초상 32칸 셋, 지금 주자 밝게 · 쓰러진 주자는 회색 + 묘비 표시 · 다음 주자 테두리 깜빡임 없음).

**넘겨주기 (주자 1 · 2 가 쓰러질 때)**
판정: 체력이 0 이하가 되어 `DeathSequence` 를 부를 차례 (= 최후의 보루 · 불사의 맹세 · 되살아난 영혼 · 명계의 문 같은 기존 부활이 모두 실패한 뒤). 기존 부활이 먼저 — 「죽음을 쓴다」는 정말 쓰러졌을 때만.

| 시각 | 일어나는 일 | 수치 |
|---|---|---|
| 0 (같은 프레임) | **논리 교대 전부** (1-8 의 `RelaySwap.Run`) — 다음 영웅 스탯 · 키트 · 카드 정리 · 환급 · 유품 · 기록 `hz` · 즉시 저장 | 이 프레임 안에서 끝남 (게임이 멈춰도 상태가 반쯤 바뀐 채 남지 않게) |
| 0 | 무적 시작 · 시간 0.35배 (실제 시간으로 0.6초 동안 0.35 → 1) | 무적 3.0초 |
| 0 | 쓰러진 자리에 앞 영웅 **유해** 소품(앞 영웅 몸 첫 프레임을 회색 · 눕힘, 8초 뒤 흐려져 사라짐) | — |
| 0.35초 | 「영혼 이어받기」 파동: 둘레 반경 7칸 적을 밀쳐냄 + 피해 | 새 영웅 공격력 × 300% (보스 · 중간 보스는 피해만 ×50%, 밀리지 않음) · 넉백 = LastStand 밀쳐 내기와 같은 세기 |
| 0.35초 | `Impact.Hit(ImpactTier.Ult, 자리)` 한 번 + 새 영웅 첫 대사 말풍선 1.5초 (그 영웅 등장 대사 · hero 문서 12절 / 기존 9명은 StoryDirector 등장 한 줄) | 손맛 원칙: 조작은 막지 않음, 화면 전체 백색 금지 |
| 0.35초 | 체력 = 새 영웅 최대 체력 가득 · **필살기 게이지 가득** | 「죽음을 쓰는」 보상 — 보스 앞에서 쓰러지면 바로 필살기 |
| 0.6초 | 진화 빚이 있으면 진화 창 (1-5) → 환급 카드 창 (첫 장 = 새 영웅 갈래 입문) | 창은 기존 `StageManager.Evolution(true)` · `LevelShop` 무료 고르기 |
| — | 토스트 「주자 교대 — ○○ 의 유품 「△△」 를 이어받았다」 4초 | `Notice.Toast` |

- 결계(`BossUltimate.Active`) · 개발자 등장 연출(`PlayerController.Cinematic`) 중에 쓰러져도 논리 교대는 즉시, 창(진화 · 환급)은 그 연출이 끝난 뒤 (`while (Cinematic || BossUltimate.Active || Time.timeScale == 0) yield`).
- **셋째 주자**가 쓰러지면 지금 그대로 `DeathSequence` → 결과 화면. 판 기록 = `GameMode.EndlessSeconds` (셋이 버틴 시간 합 = 판 시간).

### 1-5. 이어지는 것 · 버리는 것 · 환급 (모든 영웅 공통)
| 무엇 | 처리 | 코드 근거 |
|---|---|---|
| 레벨 · 경험치 · 대기 레벨업 | **이어짐** | `PlayerController.level` · `LevelShop.PendingLevels` 는 영웅과 무관 |
| 공용 레벨업 카드 1 · 4 · 5 · 8 · 9 · 10 · 성장 카드 100~103 · 초월 카드 | **이어짐** (효과가 PlayerController · Level · Coin 에 들어가 있음) | `LevelShop.SharedCardText` 의 공용 번호 |
| 영웅 카드 칸 0 · 3 · 6 · 7 · 11 · 12 · 13 (`LevelShop.KitIds`), 거너 총탄 카드 `gunCard[]` | **버림** → 단계 수를 셈 | `LevelShop.Kits` · `SpecialAbilities.GunCards` |
| 고유 스킬 15~24 (`SigFirstId` ~) · 스킬 진화(`LevelShop.evolved`) | **버림** → 단계 수를 셈 | `LevelShop.Signature` (`SignatureSkills.Refresh` 가 단계를 실시간으로 읽음) |
| 갈래 25~31 · 갈래 상태(`Paths` 정적 상태) | **버림** → 단계 수를 셈, 새 영웅으로 `Paths.BeginRun` 다시 | `Paths.BeginRun(shop, who)` 가 정적 상태를 통째로 비움 |
| **환급** | 버린 단계 수 × 0.5 (올림) 만큼 **무료 카드** (레벨이 오르지 않는 `f` 고르기). 첫 장은 새 영웅 갈래 입문 창. 최대 12장 | 새 영웅이 맨손이 되지 않게 · 「버린 빌드를 반값으로 되사는」 느낌 |
| 무기 진화 1차 · 2차 | **버림** → 같은 단계 수만큼 새 영웅 진화 창을 곧바로 다시 엶 (진화 빚) | 1차 = 영웅 전용 무기라 넘겨줄 수 없음 |
| 무기 진화 각성(3번째) | **이어짐** (`ApplyAwaken` 이 SpecialAbilities 공통 공격력 · 간격 · 치명타에 들어가 있음). 새 영웅 2차를 고르면 곧바로 3단계로 | `SpecialAbilities.Evolution` 41줄 |
| 보스 전리품(`bt`) | **이어짐** (공통 능력치) | `BossTrophy` |
| 영혼 트리 공용 칸 (생존 `s.` · 재물 `g.` · 운명 `f.` · 영혼 `o.root`·`o.harvest` · 무기 공용 `w.dmg`·`w.rate`·`w.crit` · 필살기 공용 `u.power` 등) | **이어짐** | 효과가 SpecialAbilities · PlayerController 에 있음 |
| 영혼 트리 영웅 칸 (`w.reach`·`w.arc`·`w.blast` · `u.shadow` · 형태 숙련 `m<무기>` · 영웅 패시브 `o.<번호>`) | **버림** → 쓴 영혼 조각 70% 돌려받음 (`SoulShards` 에 배율 없이) | Kit 필드에 들어간 효과는 키트와 함께 사라짐 · 패시브는 `Unequip` |
| 코인 · 영혼 조각 · 다시 뽑기 · 처치 수 · 계약 · 무한 진행(보스 · 지형 · 국면) | **이어짐** | 영웅과 무관 |
| 필살기 게이지 | 넘겨주기 때 **가득** | 1-4 |
| 손에 든 무기 · 몸 그림 · 스킨 | 새 영웅 것 (그 영웅에 장착한 스킨) | `CharacterKit.Start` · `PlayerLook` |
| 유품 | 앞 영웅 유품 하나가 붙음. 셋째 주자는 **둘** (1번 · 2번 주자 유품) | 1-6 |

### 1-6. 영웅 14명 — 넘겨줄 때 정리 · 유품
유품은 「앞 영웅 동사의 메아리」: 지금 영웅의 조작을 막지 않는 **자동 발동** 작은 기술. 앞 영웅의 키트가 없어도 돌아야 하므로 새 파일 `RelayHeirlooms.cs` 에 따로 만든다(1-8). 피해는 **지금 공격력 × 배율**, `DamageSource` 이름 「유품」(결과 화면 피해 출처 한 줄). 수치는 첫 구현값 — 봇 측정으로 판 화력 +6~10% 띠에 맞춤.

| 영웅 | 넘겨줄 때 정리(키트 OnDestroy 에 맡김 + 따로 할 것) | 유품 이름 | 유품 효과 (첫 값) |
|---|---|---|---|
| 거너 | 일제 사격 조준 중이면 취소(`CancelSkill`) · 탄창 · 장전 시간은 거너 기본값으로 되돌림 | 마지막 탄창 | 필살기를 쓸 때마다 화면 안 적 최대 6마리에 리볼버 탄 한 발씩 (공격력 120%) — 일제 사격 메아리 |
| 검사 | 회전 베기 충전 링 끔 | 녹슨 장검 | 4초마다 둘레 2.5칸에 적이 셋 이상이면 회전 베기 한 바퀴 (공격력 80%) |
| 도적 | 돌진 중이면 끝냄 · 그림자 분신 제거 | 그림자 발자국 | 맞을 때마다(쿨 6초) 가장 가까운 적 쪽으로 출혈 선 4칸 (지나간 적 출혈 3초, 초당 공격력 40%) |
| 궁수 | 시위 줄 · 가시 덤불 제거 | 꺾인 활 | 10초마다 커서 쪽으로 가득 당긴 화살 한 발 자동 (모두 관통, 공격력 250%) |
| 연금술사 | 호문쿨루스 · 시약 표시 제거 | 깨진 플라스크 | 적 12마리를 처치할 때마다 마지막 처치 자리에서 플라스크 폭발 (반경 2.5, 공격력 150%) |
| 지도 제작자 | 그리던 획 · 닫힌 도형 즉시 사라짐 | 접힌 지도 | 걸은 자리에 잉크 꼬리 3초 (닿은 적 초당 공격력 30%) |
| 시계공 | 톱니 · 되감기 표시 제거 (되감기 중이었으면 그 자리에서 멈춤) | 멈춘 회중시계 | 15초마다 「째깍」 둘레 4칸 적 0.6초 정지 (보스 제외 · 시간 자체는 건드리지 않음 — 손맛 원칙) |
| 우체부 | 날아가는 우편은 그 자리에서 터져 정상 피해 · 우체통 제거 | 마지막 우편 | 10번째 처치마다 화면 안 가장 먼 적에게 소포 (착탄 반경 2, 공격력 200%) |
| 유령 | 빙의 중이면 놓아줌(적은 기절 1초) · 냉기 겹은 그대로 녹게 | 차가운 손 | 지금 영웅 평타에 맞은 적에게 냉기 1겹 (유령 냉기 다섯 겹 얼림 규칙 그대로 · 0.5초에 한 번) — 냉기 상태 판정은 적 쪽(`EnermyController`)에 있으면 그대로, 키트에 있으면 유품 파일로 옮김 |
| 씨름꾼 | 들고 있던 적은 `EnemyHold.ReleaseAll` → 그 자리에 「메친」 것으로 떨어뜨림(피해 · 기절) | 샅바 | 8초마다 몸에 닿은 일반 적 하나를 들어 반대편 4칸으로 메침 (공격력 200% · 떨어진 자리 반경 1.5 같은 피해) — `EnemyHold` 재사용 |
| 공사 반장 | 쌓인 더미는 흩어짐(압착 피해 한 번) | 공사 중 표지판 | 필살기를 쓸 때마다 영웅 뒤쪽 2칸에 「공사 중」 바리케이드 2개 6초 (적 길막 · 부딪히면 공격력 50%) |
| 인형사 | 더미는 그 자리에 5초 남아 적 시선을 끈 뒤(`EnermyController.Decoy`) 사라짐 | 남은 줄 | 작은 더미 하나가 영웅 2칸 뒤를 따라다니며 3초마다 둘레 1.5칸 휘두름 (공격력 70%) |
| 장의사 | 열린 구멍은 바로 덮임 → 빠진 적은 묻힘 처리(장의사 매장 규칙대로) · **묘비 벽은 남음** (지형이 다음 주자에게 넘어감) | 삽 한 자루 | 보스 아닌 적 처치 때 20% 확률로 그 자리 구멍 2초 (지나던 적이 빠져 1초 못 나옴) |
| 사진사 | 종이가 된 적은 원래대로 · 인화 미끼 제거 | 마지막 필름 | 20초마다 커서 자리 3×3칸 셔터 한 번 — 틀 안 적이 종이 (2초 · 받는 피해 +30%) |

- 새 영웅 다섯(씨름꾼~사진사)의 정리 항목은 각 `hero-<키>.md` 의 키트 `OnDestroy` 구현에 들어가야 한다 (이 문서 표를 hero 문서 15절 옆에 한 줄로 알릴 것). 유품 쪽 수치는 hero 문서 평타 수치가 정해지면 「평타 한 번의 40~80%」 띠로 맞춤.
- 유품 둘이 같은 동사 계열이면(예: 마지막 탄창 + 공사 중 표지판 — 둘 다 필살기 때) 둘 다 발동 (한 판에 최대 둘이라 겹쳐도 됨).
- 유품은 HUD 특수 능력 칸 줄(`SpecialAbilities.Hud`)에 「유품」 칸으로 보임 — 그림 = 앞 영웅 메뉴 초상 축소 + 유품 테두리, 툴팁 = 이름 · 한 줄 · 쿨 남은 시간.

### 1-7. 결과 화면 · 순위표 · 보상 · 계약 · 업적
**결과 화면** (`MenuExtras.InstallGameOver` 의 EndlessRecord 자리, 3줄)
```
영혼 릴레이 · 거너 → 유령 → 씨름꾼
생존 시간 24:10   보스 처치 7   (거너 08:02 · 유령 11:30 · 씨름꾼 04:38)
이 조합 최고 기록 21:55 → 24:10 (새 기록!)   조합 순위 3위 / 41
```
- 구간 시간 = `RelayRun.SegmentStart[k]` 차. 「빌드 자세히 보기」(`RunStats.CaptureForResult`)는 셋째 주자 빌드 + 「이어받은 유품」 줄.
- `RankLine` 재사용: 이름 글자만 「조합 순위」.

**순위표 (스팀)**
- `RELAY_ALL` (모든 조합 한 줄 · 내림차순 · 초) + **조합별** `RELAY_<a>_<b>_<c>` — 세 영웅 번호를 **작은 순으로 정렬한** 두 자리 수 (`RELAY_00_08_09`). 순서는 다른 판으로 보지 않음(같은 셋이면 같은 보드) → 14명 기준 364개.
  - 순서까지 따로 하면 2,184개 — 보드가 너무 잘게 쪼개져 대부분 비어 있음 (결정 R3).
- 자세한 값 5칸 (`DetailCount` 그대로): 0 = 순서 부호 `a*10000 + b*100 + c` · 1 보스 · 2 레벨 · 3 처치 · 4 판 버전. 순위 창은 칸 0 을 풀어 초상 셋.
- 로컬 최고: `Prefs relay.best.<a>_<b>_<c>` · `relay.best`.
- **무한 모드 최고 기록 · 무한 순위표 · 무한 이정표(EndlessMilestones)에는 넣지 않음** (목숨이 셋이라 규칙이 다름).

**보상**
- 포인트: 무한 모드와 같음 (`Point` 처치 포인트 · 최종 클리어 보너스 `EndlessFinalBonus` 도 받음 — 개발자 · 엄마는 셋이서라도 어려움).
- 영웅 해제 조건(`CharUnlock.Endless10` 등) · 사막 비밀 해금: **됨** (결정 R5 — 셋이 이어 달려도 10분 · 보스 5는 노력의 결과).
- 영웅 숙련도 · 영혼의 무게: 무한 모드라 원래 없음.

**계약** (`Contracts`)
- `KindsFor(endless, daily)` 에 릴레이 분기: 무한 쉬움 1 + 무한 보통 1 + **릴레이 전용 1** (새 `ContractKind.Relay`, 600P). 영웅 계약(H)은 뽑지 않음 (영웅이 바뀌어 셀 수 없음).
- 릴레이 전용 계약 (`RunCounters` 키):
  - `r.first5` 「첫 주자로 5분 버티기」 — `relay.segment0` ≥ 300
  - `r.handoff30` 「이어받고 10초 안에 30마리 처치」 — `relay.handoffBurst` (Max)
  - `r.allboss` 「세 주자 모두 보스를 하나씩 쓰러뜨리기」 — `relay.bossBy` 비트 = 7
  - `r.burstboss` 「이어받기 파동으로 보스에게 피해 주기」 — `relay.burstBoss` ≥ 1
- 글: `LocTable.Contract` 에 `contract.r.*.name / .cond` 4개 언어.

**업적** (`SteamAchievements` 에 더함 · 스팀 쪽 등록은 사용자 몫)
- `ACH_RELAY_FIRST` 영혼 릴레이를 10분 넘게 버팀
- `ACH_RELAY_DEV` 영혼 릴레이에서 개발자 처치
- `ACH_RELAY_ALLBOSS` 세 주자 모두 보스 처치 (계약과 같은 조건)
- `ACH_RELAY_FIRSTHERO` 첫 주자 혼자 15분 (넘겨주지 않고 최후의 시간 진입)

### 1-8. 구현 설계 — 판 중간에 영웅 바꾸기 (가장 어려운 부분)

#### 상태가 사는 곳
| 상태 | 사는 곳 | 영웅 따라 바뀜? |
|---|---|---|
| 모드 · 순서 · 지금 주자 · 구간 시작 시각 · 유품 목록 · 진화 빚 · 남은 환급 | **새** `RelayRun` (정적, `Modes/RelayRun.cs`) | — |
| 지금 영웅 번호 | `CharacterData.Override` (일일 도전처럼) | 교대 때 바꿈 |
| 기본 스탯 (damage · PlayerMaxHealth · ShootSpeed · speed · MaxBullet · reloadTime) | `PlayerController` — **영웅 배율이 곱해진 값** | 비율로 바꿈 |
| 평타 · 우클릭 · 영웅 카드 단계(`card[]`) · 형태 · 강화 · 영웅 소품 | `CharacterKit` 컴포넌트 (거너는 없음) | 컴포넌트를 지우고 새로 붙임 |
| 고유 스킬 효과 | `SignatureSkills` (`who` 를 Awake 에서 캐시) | `who` 다시 · Refresh |
| 갈래 | `Paths` 정적 (`Hero` · rank · tier …) | `Paths.BeginRun` 다시 |
| 카드 단계 · 고른 표시 · 스킬 진화 | `LevelShop.ability_level` · `ability_selected` · `evolved` · `myEvos` 캐시 | 영웅 칸만 0 |
| 무기 진화 · 트리 · 장착 패시브 · 거너 총탄 카드 | `SpecialAbilities` (씬 공용 · 파괴되지 않음) | 진화 · 영웅 칸만 되돌림 |
| 손에 든 것 · 발 그림자 색 · 반동 상수 | `PlayerLook` (Start 에 한 번 캐시, 178~188줄) | 다시 캐시 |
| 게이지 | `SkillGauge.SkillPoint` (차는 속도는 `Current.gauge` 를 매 프레임 읽음) | 값만 가득 |

#### `RelaySwap.Run(PlayerController p, CharacterId next, bool replaying)` — 순서 그대로 (새 파일 `Modes/RelaySwap.cs`, 약 220줄)
1. **멈출 것 멈추기**: `p.CancelSkill()` · `CharacterKit.Instance?.CancelUltForPause()` · `GameInput.ResetAttackLock()` · `EnemyHold.ReleaseAll(kit)`.
2. **앞 영웅 배율 빼기** (`CharacterDef old = CharacterData.Current`):
   `p.damage /= old.damage` · `p.PlayerMaxHealth /= old.hp` · `p.ShootSpeed *= old.attackSpeed` · `p.speed /= old.move` · `p.BaseMoveSpeed /= old.move` (지금 private `baseMoveSpeed` → 읽기 · 쓰기 가능한 속성으로) · `p.MaxBullet = RelayRun.BaseMag` · `p.reloadTime = RelayRun.BaseReload`.
   `SpecialAbilities.RescaleBase(1f / old.damage)` (기준 공격력 `baseAttack` 도 같은 비율 — 트리 · 전리품의 「기준 공격력 n%」가 이 값에 더해지므로 비율이 맞음. `AddAttack` 이 `damage += BaseAttack * pct` 덧셈이라 둘을 같은 비율로 바꾸면 그대로 성립).
   - `RelayRun.BaseMag · BaseReload` = **`PlayerController.Start` 에서 `CharacterKit.Attach` 전에** 한 번 기록한 인스펙터 값 (`RelayRun.CaptureBase(this)` 한 줄 추가).
3. **카드 정리** `LevelShop.RelayDropHero(out int dropped)` (새 partial `LevelShop.Modes.cs`):
   - `KitIds` · `SigFirstId..+9` · `Paths.SeedFirstId..IdEnd-1` 칸의 `ability_level` 합 = dropped, 모두 0. 거너였으면 `sp.gunCard[]` 합도 더하고 0.
   - `evolved`(스킬 진화) 비우고 `myEvos = null` (`MyEvos` 가 새 영웅으로 다시 만듦) · `abilityHUD.SetAbility(id, 0)` (0 이면 칸을 지우게 `AbilityHUD.SetAbility` 확인 · 필요하면 `Remove(id)` 추가) · 스킬 진화 칸(`id >= 1000`) 지움.
   - `ability_selected` 를 `Start` 59~63줄 식으로 다시 계산 — 그 줄을 `RecomputeClosed()` 로 빼서 Start 와 같이 씀 (`KitIcon(i)` 가 새 영웅 기준).
4. **진화 정리** `SpecialAbilities.RelayResetEvolution(out int owed)` (새 partial `SpecialAbilities.Modes.cs`):
   - `owed = Mathf.Min(EvolutionTier, 2)` · `keepAwaken = awakenId >= 0` · `EvolutionTier = 0` · `gunEvo1 = gunEvo2 = -1` · `weaponHistory = {-1}` · `historyTier = {-1:0}` · 거너 진화 필살기(`UltId`) 기본으로 · `RebuildHudRows()`.
   - `NextEvolutionOptions` 에 한 줄: `if (EvolutionTier == 2 && awakenId >= 0) { EvolutionTier = 3; return null; }` (각성은 다시 고르지 않음).
5. **트리 영웅 칸 정리** `SpecialAbilities.RelayDropHeroNodes(CharacterId old)`: `ownedNodes` 중 영웅 칸(1-5 표의 앞글자 · `o.<번호>` 는 `old.pool` 의 패시브)만 지우고 값 합의 70% 를 `SoulShards.Refund`. 패시브는 새 `Unequip(int id)` — `equipped` · `evolved` 에서 빼고 개별 뒷정리(`KitPlate` 는 `KitOnEquip` 반대: def −0.12 · speed /0.95, `OrbsId` 면 구슬 제거). Kit 필드(`reachMul` 등)에 들어간 효과는 키트와 함께 사라지므로 따로 할 것 없음.
6. **키트 갈아 끼우기**:
   - `Destroy(oldKit)` 대신 **`DestroyImmediate`** — `CharacterKit.Instance` 가 같은 프레임에 새 키트를 가리켜야 하고, OnDestroy 정리(호문쿨루스 · 도형 · 톱니 …)가 지금 끝나야 함.
   - `CharacterData.Override = next` (`Selected` 프레임 캐시는 Override 가 먼저라 바로 반영).
   - 거너가 아니면 `CharacterKit.Attach(p)` 후 **곧바로 `kit.ApplyDef()`** — 지금 `CharacterKit.Start` 의 스탯 곱 · 몸 그림 불러오기를 `ApplyDef()` 로 빼고 `defApplied` 표시로 Start 에서 두 번 하지 않게. (Start 를 기다리면 다음 프레임에 `PlayerHealth = Max` 가 저장 복원값을 덮어씀 — 이어하기 재생에서 체력 버그)
   - 거너면 `p.damage *= def.damage` · `p.reloadTime = Min(reloadTime, 1.5f)` (PlayerController.Start 267~269줄과 같은 식 — 그 두 줄도 `ApplyGunnerDef()` 로 빼서 같이 씀) · `Animator.enabled = true` (키트가 껐음) · 스프라이트를 애니메이터 첫 그림으로.
   - `p.speed *= def.move` · `p.BaseMoveSpeed *= def.move` · `SpecialAbilities.RescaleBase(def.damage)`.
7. **다시 캐시**: `PlayerLook.Rebind()` (178~226줄 캐시 부분을 함수로 · `FootShadow` 색 · 손에 든 무기 그림 · 반동), `SignatureSkills.Rebind(next)` (`who = next` · 고유 스킬 소품 정리 · `Refresh(shop)`), `Paths.BeginRun(shop, next)` (정적 상태 비움 · 새 영웅 갈래), `HeroAim` · `AttackLook` · 범위 표시가 영웅을 캐시하면 같은 식으로 (구현 때 `git grep "CharacterData.Selected" ` 56개 파일 중 **Start/Awake 에서 읽는 곳만** 골라 Rebind — 매번 읽는 곳은 손대지 않음).
8. **체력 · 게이지 · 유품 · 환급**:
   `p.PlayerHealth = p.PlayerMaxHealth` (replaying 이면 건너뜀 — 복원이 저장값으로 덮음) · `SkillGauge` 가득 · `RelayHeirlooms.Add(old)` · `RelayRun.FreeLeft += Mathf.Min(12, Mathf.CeilToInt(dropped * 0.5f))` · `RelayRun.EvoOwed += owed`.
9. **기록 · 저장**: `RunSave.Record("hz" + RelayRun.Index)` → `RunSave.StageForSave(); Prefs.Save();` (기록 직후 바로 씀 — 1-9 의 「쓰러진 순간 끄기」 꼼수를 막음). replaying 이면 Record 는 저절로 안 쌓임.
10. **연출 · 창** (replaying 아니면): 1-4 표의 코루틴 `RelayRun.HandoffFx()` — 파동 · 말풍선 · 토스트, 그 뒤 `EvoOwed` 만큼 `stages.Evolution(true)`, 그 뒤 `FreeLeft` 를 `shop.GrantFreePicks(FreeLeft)` (첫 장은 `ResumeFreePick` = 갈래 입문 창).

#### 넘겨주기를 거는 자리
`PlayerController.TakeDamage` 1522줄 `if (PlayerHealth <= 0) StartCoroutine(DeathSequence());` →
```csharp
if (PlayerHealth <= 0) { if (RelayRun.TryHandoff(this)) return true; StartCoroutine(DeathSequence()); }
```
`TryHandoff`: `Active && Index < 2 && !IsDying && !Swapping` → `Index++` · `Herald.RecordDeath()` (그 영웅 죽음 수 · 장의사 묘비 N에 들어감, 전령 대사는 마지막에만) · `RunStats` 구간 표시 · `RelaySwap.Run(...)` · 연출 코루틴 시작 · `true`. 체력이 0 이하로 들어가는 다른 길(`PlayerHealth -=` 를 직접 하는 곳)이 있으면 같은 줄로 (구현 때 `git grep "PlayerHealth -="` 확인).

#### 바꿔야 할 파일 · 함수 (릴레이)
| 파일 : 함수 | 할 일 | 대략 줄 |
|---|---|---|
| 새 `Modes/RelayRun.cs` | Active · Order · Index · Begin · End · Unlocked · TryHandoff · HandoffFx · Segment · Submit · Save/Restore 칸 · 주자 띠 HUD | 300 |
| 새 `Modes/RelaySwap.cs` | 1-8 순서 1~9 | 220 |
| 새 `Modes/RelayHeirlooms.cs` | MonoBehaviour 하나(플레이어에 붙음) + 유품 14개 (`abstract class Heirloom { Tick · OnKill · OnUlt · OnHurt · OnPlayerHit }`) | 450 |
| 새 `Modes/ModeHubUI.cs` | 0-3 · 1-3 · 2-3 창 | 320 |
| `CharacterKit.cs` : `Start` | `ApplyDef()` 로 뺌 · `defApplied` | +15 |
| `PlayerController.cs` : `Start` · `TakeDamage` | `CaptureBase` · `ApplyGunnerDef()` · `BaseMoveSpeed` 속성 · 넘겨주기 줄 | +20 |
| `PlayerLook.cs` : 캐시부 | `Rebind()` | +10 |
| `SignatureSkills.cs` : `Awake` | `Rebind(CharacterId)` (소품 정리 포함) | +20 |
| `LevelShop.cs` : `Start` | `RecomputeClosed()` 로 뺌 | +5 |
| 새 `LevelShop.Modes.cs` | `RelayDropHero` · (역류용) `UndoCard` · `AutoFill` | 280 (역류 몫 포함) |
| 새 `SpecialAbilities.Modes.cs` | `RelayResetEvolution` · `RelayDropHeroNodes` · `Unequip` · `RescaleBase` · (역류용) `UndoEvolution` · 축복 | 200 (역류 몫 포함) |
| `SpecialAbilities.Evolution.cs` : `NextEvolutionOptions` | 각성 유지 한 줄 | +2 |
| `RunSave.cs` : `Data` · `Stage` · `Continue` · `Restore` · `Peek` | 1-9 | +45 |
| `EndlessMode.cs` : `Finish` · `Update`(이정표) · `BuildHud` | 순위 분기 · `!ModeRun.Special` · 「주자 n/3」 | +15 |
| `Contracts.cs` : `KindsFor` · 새 정의 | 릴레이 계약 4종 | +30 |
| `SteamManager.cs` | 업적 4 · 보드 이름 | +15 |
| `LeaderboardUI.cs` | 탭 · 순서 부호 풀기 | +50 |
| `MenuExtras.cs` | 「특별 모드」 줄 · 결과 줄 | +40 |
| `LocTable.Modes.cs` (새) | 글 약 90줄 × 4개 언어 | 400 |

### 1-9. 판 중간 저장 · 이어하기 (릴레이)
저장 방식은 「선택 기록 재생」(`RunSave.log`)이라, **교대도 기록 한 줄(`hz<번호>`)로 넣고 재생 때 같은 `RelaySwap.Run(replaying: true)` 를 그 자리에서 부른다.** 교대 전 앞 영웅 카드 기록(c · f · e · t)은 앞 영웅에게 적용됐다가 교대에서 버려지므로, 재생 결과가 실제 판과 같다.
- `Data` 에 더함: `string relay = ""` (순서 "0,8,9" · 빈 글 = 릴레이 아님) · `int relayIndex` · `int relayEvoOwed` · `int relayFreeLeft` · `string relaySegments` ("0,482,1172" 구간 시작 초).
- `Stage()`: `character` 에 **`Order[0]`** 을 적음 (재생은 첫 주자부터 시작해야 함) + 위 칸.
- `Continue()`: `relay` 가 있으면 `RelayRun.Resume(order, …)` (Active 켬 · Index 0 으로 시작 — 재생의 `hz` 가 올림) · `CharacterData.Override = order[0]`.
- `Restore()` 의 `switch` 에 `case 'h'` 이 이미 「보물 상자 생명(h<수>)」이므로 **두 글자 기록**으로: `if (twoDigit && e.StartsWith("hz"))` 를 aw · ap · tr · bt 옆에 더함 (다음 글자가 숫자) → `RelaySwap.Run(p, order[k], true)`.
- 재생이 끝난 뒤 `relayEvoOwed` 만큼 진화 창 · `relayFreeLeft` 만큼 무료 카드 (기존 `NeedEvolution` · `freeDue` 처리 바로 뒤).
- `Peek()`: 릴레이 저장이면 셋 모두 `IsUnlocked` 일 때만 유효.
- **교대 연출 중 저장**: 논리는 이미 끝났고 즉시 저장했으므로 연출 0.6초 사이에 꺼도 상태가 맞음. `Swapping` 동안(같은 프레임 안)은 `Stage()` 가 false.
- 「쓰러지는 순간 끄면 앞 영웅이 살아난다」 꼼수: 교대는 체력 0 프레임에 저장까지 끝내므로 없음. 셋째 주자 죽음은 기존 `RunSave.Abandon` 그대로.

---

## 2. 역류

### 2-1. 한 줄
15분째 **완성된 몸**으로 시작해 시간이 **거꾸로** 흐른다(화면 시계 15:00 → 00:00). 1분마다 「무엇을 잃을까」 3택으로 힘을 하나씩 내려놓고, 적도 함께 약해진다. 첫 상대는 개발자, 그 뒤 왕들이 거꾸로 오고, 마지막엔 **맨몸으로 「처음의 슬라임」**. 10분 안쪽.
재미의 핵: 서바이버의 「쌓는 손맛」을 뒤집어 「버리는 선택」이 매분 판의 맛을 바꿈 — 무엇을 끝까지 쥐느냐가 그 판의 개성.

### 2-2. 열리는 조건 (D10)
`RefluxRun.Unlocked => !Demo.On && (Prefs dev.defeated == 1 || EndlessMilestones 「dev」 달성 || EndlessMode.EverCleared)`
- 새 Prefs `dev.defeated` 는 `EndlessMode.OnBossDefeated` 에서 개발자가 처음 쓰러질 때 **모든 무한 계열(무한 · 일일 도전 · 릴레이)** 에서 1 (이정표는 일일 도전을 빼므로 따로 둠). 옛 저장은 이정표 · 클리어 기록으로 소급.
- 처음 열린 판의 결과 화면에 「새 모드 「역류」가 열렸다」 한 줄 (`ResultScreen` 해금 줄 자리) + 「특별 모드」 NewGlow.

### 2-3. 메뉴 진입 UI — 「역류 준비」 창 (`ModeHubUI.OpenReflux`)
```
┌ 역류 ─────────────────────────────────── [닫기] ┐
│ 영웅 격자 (가진 영웅만)                              │
│ 갈래: [ A ][ B ][ C ][ D ][ E ]   ← 그 영웅 갈래 5개 이름 · 마우스 올리면 한 줄 │
│ 1차 무기: [형태1][형태2][형태3]    ← 진화 1차 형태 그림 · 이름              │
│ 「이 빌드로 완성된 몸: 레벨 40 · 진화 각성 · 카드 39장」 미리 보기 (아이콘 줄)   │
│ 내 최고 기록 08:41 (이 영웅 09:20)       [역류 시작]                          │
└────────────────────────────────────────────────┘
```
- 기본값 = 지금 고른 영웅 · 지난 역류에서 고른 갈래 · 무기 (`Prefs reflux.last`).
- 미리 보기 줄은 2-5 의 자동 채우기를 메뉴에서 **같은 시드로 미리 돌린 결과**의 아이콘(카드 · 진화)만 — 「무엇을 들고 들어가는지」 알고 시작해야 잃는 선택이 의미 있음.
- [역류 시작] → `RefluxRun.Begin(hero, path, form)`.

### 2-4. 판 흐름 (실제 시간 기준)
| 실제 시각 | 화면 시계(거꾸로) | 일 |
|---|---|---|
| 0:00 | 15:00 | 자동 채우기(2-5) 끝난 몸으로 시작 · 게이지 가득 · 배너 「역류 — 시간이 거꾸로 흐른다」 2.5초 |
| 0:03 | 14:55 | **개발자** 강림 (`SummonDeveloper` 재사용 · 잡몹 비우기 없이) |
| 1:00 ~ 9:00 | 13:30 ~ 01:30 | **매분 「무엇을 잃을까」 3택** (최대 9번 · 2-6) |
| 앞 보스 처치 + 6초 또는 예정 시각 중 빠른 쪽 | | 다음 보스 (2-8) |
| 리치 왕 처치 직후 | | **「역류의 끝」**: 남은 것을 모두 잃음(맨몸) → **처음의 슬라임** (2-9) |
| 처음의 슬라임 처치 | | 클리어 — 기록 = 그 실제 시각 |

- 레벨업 · 경험치 · 코인 상점 · 보물 상자 · 영혼 조각 · 영혼 트리 · 중간 보스 · 습격 · 보스 진화 창은 **모두 끔** (쌓는 길이 하나라도 있으면 「잃기」가 흐려짐). 처치 경험치는 0, 코인 · 조각은 떨어뜨리지 않음.
- 체력: 잃기를 고를 때마다 최대 체력의 25% 회복 (「힘이 빠져나가며 숨을 돌림」), 보스 처치마다 40% 회복.
- 15분 최후의 시간 · 모래 폭풍 · 한계 돌파 · 엄마: 없음 (EndlessMode 의 일정은 끄고 RefluxRun 이 대신 — 2-11).
- 사막 비밀: 나옴 · 해금됨 (desert-secrets 공통 규칙). 지형 변화(EndlessTerrain): 끔 (보스 처치마다 영토가 번지는 연출은 「쌓는」 쪽 상징이라 맞지 않음 · 결정 X6).

### 2-5. 완성 상태로 시작 — 어떻게 채우나
| 안 | 장점 | 단점 |
|---|---|---|
| A. 완전 자동 (영웅만 고름) | 가장 쉬움 | 「내 빌드」 느낌이 없어 잃을 때 아깝지 않음 |
| B. 지난 판 빌드 | 내가 만든 것을 잃는 맛 | 죽으면 `RunSave` 기록이 지워져 남지 않음 · 이야기 판 빌드는 레벨이 들쭉날쭉 · 순위가 불공평 |
| **C. 반자동 (추천)** — 영웅 · **갈래** · **1차 무기**만 고르고 나머지는 고정 시드로 자동 | 빌드 정체성(갈래 · 무기)은 내 선택 · 같은 선택이면 누구나 같은 몸 → 순위가 공정 · 메뉴 미리 보기 가능 | 자동 채우기 코드가 필요 |

**C 의 자동 채우기 `LevelShop.AutoFill(CharacterId who, int path, int form, int seed)`** (`LevelShop.Modes.cs`)
- 시드 = `who * 7919 + path * 104729 + form` (일일 도전 갈래 뽑기와 같은 방식 · `UnityEngine.Random` 은 건드리지 않고 `System.Random`).
- `RunSave.Replaying` 과 같은 「연출 없음」 표시(`AutoFilling`)를 켜고:
  1. 갈래 입문 `ReplayPick(Paths.SeedFirstId + path, free: true)`.
  2. 레벨 40 까지 **39번**: 지금 레벨업과 같은 뽑기(`Draw` · `DrawWeight`)로 3장을 뽑고 우선순위로 하나 — ① 고른 갈래 카드 · 그 갈래 31번 새 카드 ② 스킬 진화 재료(`MyEvos` 재료 중 덜 찬 것) ③ 이미 가진 카드 ④ 공용 생존(8 · 9 · 10) ⑤ 나머지. `ReplayPick(id, false)` 로 적용 (`CheckEvolutions` · `PathsAfterPick` 이 그대로 돌아 스킬 진화 · 갈래 단계가 저절로 참).
  3. 진화: `EvolveWeapon(form)` → 2차 = `CharacterKit.AugmentsFor(who)` 중 갈래 추천(갈래 정의에 추천 강화가 없으면 첫째) → 각성 = 갈래가 화력형이면 파괴 · 속사형이면 질풍 · 그 밖 영혼 (갈래 정의에 한 칸 `refluxAwaken` 추가, 없으면 파괴).
  4. 영혼 트리 대신 **「역류의 축복」** (아래) — 트리 칸은 되돌리는 길이 없어서(칸마다 `apply` 람다만 있고 반대가 없음) 트리를 쓰지 않는다.
- 「역류의 축복」 = 15분째 트리 몫을 되돌릴 수 있는 덩어리로: 공격력 +15% × 4조각 · 공격 간격 −6% × 3 · 치명타 +5% × 3 · 최대 체력 +10% × 3 (`AddAttack` · `AddRate` · `TreeCrit` · `PlayerMaxHealth` — 모두 정확한 반대 연산이 있음). 수치는 「15분 무한 판 트리 평균」에 맞춰 봇으로 조정.
- 레벨 40 · 각성까지 = 무한 15분 판의 평균 몸(15분에 보스 3~4 처치 · 레벨 35~42, `NeedExp` 주석 기준). HUD 레벨 숫자는 잃을 때마다 같이 줄여 보여 줌(「힘이 빠진다」를 숫자로).
- B(지난 판 빌드)는 2차 기능으로 남김: 죽을 때 `RunSave.log` 를 `Prefs reflux.lastlog` 에 복사해 두면 가능 (결정 X2).

### 2-6. 「무엇을 잃을까」 3택
**창**: 기존 `ChoiceUI.Run(title, sub, options, onPick)` 그대로 (보물 상자 · 보스 보상 · 저주 제단이 쓰는 창 — 게임이 멈추고 숫자 키 · 더블클릭 · Space 확정 · 열린 직후 0.6초 잠금이 이미 있음). 레벨업 창(LevelShop 카드 UI)이 아니라 ChoiceUI 를 쓰는 이유: 「한 장 = 여러 항목 묶음」을 이름 · 설명 · 그림으로 보여 주는 칸이 ChoiceUI 에 있고, 레벨업 창은 카드 번호 하나에 묶여 있음.
- 제목 「무엇을 잃을까」 · 부제 「역류 n/9 · 남은 시간 12:00 → 적도 약해집니다」.
- 카드 하나 = 잃을 묶음: 그림 = 묶음 첫 항목 아이콘(`ChoiceUI.Option.sprite` 에 카드 · 진화 아이콘) · 이름 = 「관통 · 자석 을 잃는다」 · 설명 = 항목별 「Lv 4 → 0」 + 회색 「이 힘으로 지금까지 n 피해」(`RunStats.DamageDealt` 출처로, 있으면) · 단계 칸(`WithPips(now, max, -n)` 로 빠질 칸을 흐리게 — `LevelPips` 가 음수 add 를 받게 한 줄).
- 고르지 않고 넘기기 없음. 패드 · 자동 조작은 ChoiceUI 기본 동작 그대로(자동 조작이면 첫 장).
- 열 때 조건: `!BossUltimate.Active && !PlayerController.Cinematic && !ChoiceUI.Open && 플레이어가 살아 있음` — 아니면 그 상태가 끝난 프레임에 (다음 잃기는 실제로 고른 때부터 60초).

**잃는 목록(LossPool)** — 항목마다 「무게」(그 판 화력에서 차지하는 몫의 어림):
| 종류 | 항목 | 무게 | 되돌리는 법 |
|---|---|---|---|
| 카드 | 레벨업 카드 한 장 전부 (공용 1 · 4 · 5 · 8 · 9 · 10 · 성장 · 영웅 칸 · 고유 스킬) | 단계 수 | `LevelShop.UndoCard(id)` — 공용은 카드마다 반대 연산(관통 −1 · 최대 체력 −n · 받는 피해 감소 −0.12×n …, `ApplyPick` 의 반대를 한 함수에 표로), 영웅 칸 `kit.card[slot] -= n` + `kit.CardLost(slot)`(카드가 소환한 것 제거 — 호문쿨루스 · 분신 등, 영웅마다 `CardPicked` 를 보고 반대를 둠), 거너 `gunCard[g] -= n`, 고유 스킬 `ability_level = 0` + `SignatureSkills.Refresh` |
| 스킬 진화 | 그 카드를 재료로 쓴 스킬 진화는 **재료와 함께** 사라짐 (따로 뽑지 않음) | 재료에 +2 | `evolved.Remove(key)` · HUD 합침 풀기(`AbilityHUD.Merge` 반대) |
| 진화 | 무기 진화 한 단계 — **위에서부터만** (각성 → 2차 → 1차) | 각성 4 · 2차 4 · 1차 6 | 각성: `ApplyAwaken` 반대 · 2차: `kit.SetAugment(-1)` / 거너 `gunEvo2 = -1` · 1차: `kit.SetForm(-1)` / `gunEvo1 = -1` · `EvolutionTier--` · `RebuildHudRows` |
| 축복 | 역류의 축복 한 조각 | 2 | 반대 연산 |
| 갈래 | **갈래 전체** (5분째부터만 나옴 · 한 번) | 8 | 새 `Paths.Disable()` — `Hero = null` 로 전투 훅이 멈춤(`Live => Active && Hero != null`) · 갈래 카드 25~31 단계 0 |

**묶음 짓기** (`RefluxRun.BuildOffers(int k)`, k = 몇 번째 잃기 1~9)
- 목표 무게 `target = 남은 무게 합 / (10 − k)` (9번째엔 남은 것의 절반 · 나머지는 끝에 맨몸으로).
- 카드 셋은 **서로 다른 종류**에서: ① 「공격」 묶음(영웅 칸 · 고유 스킬 · 진화 · 공격 축복) ② 「생존 · 살림」 묶음(공용 카드 · 체력 축복) ③ 「영웅다움」 묶음(갈래 · 스킬 진화가 걸린 재료 · 1차 무기). 종류가 비면 남은 종류에서.
- 각 묶음 = 그 종류 항목을 무게가 target 에 가장 가깝게 1~3개 (욕심쟁이로 큰 것부터). 셋의 무게 차이는 ±30% 안 — 넘으면 큰 쪽 항목 하나를 뺌.
- 같은 묶음이 연달아 나오지 않게 지난 3택에서 고르지 않은 항목은 다음 번에 무게 ×0.8 (「아까 안 버린 그것」이 또 나와도 덜 매력적이게 하지 않으려면 결정 X4).
- 남은 항목이 없으면 창을 띄우지 않음 · 하나뿐이면 한 장 창.
- 고른 묶음은 `RunCounters` `reflux.lost.<종류>` 로 셈 (계약 · 결과 화면).

### 2-7. 거꾸로 가는 적 세기 곡선
`GameMode` 의 무한 곡선은 모두 `EndlessSeconds` 한 값에서 나옴 (`Ramp` · `MobRamp` · `Surge` · `EarlyT`). **곡선 식은 그대로 두고 넣는 시간만 뒤집는다.**
```csharp
// GameMode.cs
public static float RampSeconds => RefluxRun.Active ? RefluxRun.VirtualSeconds : EndlessSeconds;
// Ramp · MobRamp · EarlyT · Surge 안의 EndlessSeconds → RampSeconds (4곳)
// RefluxRun
public const float VirtualStart = 899f, VirtualRate = 1.5f;      // 899: Surge(FinalAt=900 이상)에 걸리지 않게
public static float VirtualSeconds => Mathf.Max(0f, VirtualStart - VirtualRate * GameMode.EndlessSeconds);
```
- 실제 10:00 에 가상 0:00. 화면 시계는 가상 시계(15:00 → 00:00) · 기록은 실제 시계.
| 실제 | 가상 | 잡몹 세기 MobRamp | 보스 세기 Ramp | 초반 완화(EarlyT) |
|---|---|---|---|---|
| 0:00 | 14:59 | 2.8 | 1.97 | 없음 |
| 2:00 | 11:59 | 2.0 | 1.6 | 없음 |
| 4:00 | 8:59 | 1.5 | 1.3 | 시작 |
| 6:00 | 5:59 | 1.1 | 1.03 | 반쯤 (체력 ×0.75) |
| 8:00 | 2:59 | 0.8 | 0.86 | 최대 (잡몹 체력 ×0.5 · 받는 피해 ×0.6) |
| 10:00 | 0:00 | 0.6 | 0.7 | 최대 |
- `PlayerDamageMul` 은 무한과 같은 고정 0.816 (판 안 시간 배율 없음) — 그대로.
- 생성 간격 · 한 화면 수(`SpawnIntervalMul` · `ExtraAliveCount`)도 같은 MobRamp 를 쓰므로 저절로 줄어듦 → 「적도 함께 약해진다」가 화면 밀도로 보임.
- 나오는 적 종류: 무한 스테이지(1 · 2 · 3장 섞음) 그대로. 가상 3분 아래(실제 8:00~)는 1장 적(슬라임 · 박쥐)만 — `EnemySpawner` 가 스테이지 적 목록에서 뽑을 때 역류면 `RefluxRun.MobTier` (가상 9분↑ 전체 · 3~9분 1 · 2장 · 3분↓ 1장)로 거름 (새 조건 한 줄).

### 2-8. 보스 순서 · 체력
무한 모드의 순서(리치 왕 → 지옥의 군주 → 킹 슬라임 → 15분 거울의 군주 → 개발자)를 뒤집음.
| 순서 | 보스 | 예정 시각(실제) | 체력 |
|---|---|---|---|
| 1 | 개발자 | 0:03 | `DeveloperHp × GlobalEnemyHpMul × 0.6` = 36,000 (엄마 없음 · 단계 보상 없음) |
| 2 | 거울의 군주 | 2:30 | 프리팹 체력 × `BossHpMul` (가상 시간 Ramp 가 그 순간 곱해짐 — `bosss.Start`) |
| 3 | 킹 슬라임 | 4:30 | 같음 |
| 4 | 지옥의 군주 | 6:00 | 같음 × 0.5 (`SecondBossHpMul` 과 같은 이유 — 기본 체력이 리치 왕의 5배) |
| 5 | 리치 왕 | 7:30 | 같음 |
| 6 | 처음의 슬라임 | 리치 왕 처치 직후 | 2-9 |
- 「앞 보스 처치 + 6초 또는 예정 시각 중 빠른 쪽」 · 예정 시각에 앞 보스가 살아 있으면 **함께** (무한의 「보스는 기다리지 않는다」 · 동시 최대 2 그대로).
- 실제 12:00 이 지나도 보스가 남으면 풍화(`bosss.erosionMul`)를 1분마다 +50% (맨몸 끝판이 영원히 안 끝나는 일 방지). 개발자 제외 규칙은 역류에선 풂.
- 보스 처치: 진화 창 · 전리품 없음 → 체력 40% 회복 + 「보스 처치!」 알림. 포인트 · 처치 수는 셈.

### 2-9. 「역류의 끝」 · 처음의 슬라임
- 리치 왕이 쓰러지고 처치 연출이 끝나면(`bosss.Dying == 0`): 화면 가장자리 흑백 한 박자(테두리만 · 손맛 원칙) · 소리 「스르르—」 · 남은 모든 것을 `RefluxRun.StripAll()` (LossPool 전부 되돌림 + 축복 0) → **맨몸** (레벨 1 표시 · 기본 무기 · 기본 우클릭 · 필살기 게이지는 그대로).
- 「쓰지 않은 잃기」 수(9 − 이미 고른 수) = 「남은 영혼」 — 결과 화면에 보여 주고 순위 자세한 값에 적음 (빨리 온 사람의 자랑).
- **처음의 슬라임**: 1장 첫 적 슬라임 프리팹을 `localScale × 3` · 체력 = 그 슬라임 기본 체력 × 40 × `EnemyHpMul`(가상 0분) · 접촉 피해 ×2 · 10초마다 작은 슬라임 6마리 낳음 · 체력바는 보스바(`bossbar`)에 「처음의 슬라임」. 맨몸 기준 40~70초 (봇으로 맞춤). 잡몹은 이때부터 1장 슬라임만 적게.
- 처치 → 클리어 연출 (`Notice.Big("역류 끝 — 처음으로 돌아왔다")`, 5초) → 결과 화면 (`SceneFlow.Go("GameOver")` 를 클리어 표시로 · `RunStats.Cleared`).
- 쓰러지면 그대로 결과 화면 (실패 · 「보스 n/6」).

### 2-10. 결과 화면 · 순위표 · 보상 · 계약 · 업적
**결과 화면** (EndlessRecord 자리)
```
역류 · 우체부 · 「속달」 갈래 · 비둘기
클리어 08:41   남은 영혼 2   끝까지 쥔 것: 관통 · 각성(파괴)
최고 기록 09:20 → 08:41 (새 기록!)   역류 순위 12위 / 230
```
- 「끝까지 쥔 것」 = 맨몸 직전까지 남아 있던 항목 상위 2개 (그 판의 개성 한 줄).
- 실패면 「보스 4/6 · 07:12 에 쓰러짐」.

**순위표**: `REFLUX_BEST` 하나 (오름차순 · 초 · `Find(ascending: true)`), 클리어한 판만 올림. 자세한 값: 0 영웅 · 1 갈래 번호 · 2 남은 영혼 · 3 처치 수 · 4 판 버전. 영웅별 보드는 두지 않음 (자동 채우기로 몸이 비슷하게 맞춰짐 · 결정 X5). 로컬 `Prefs reflux.best` · `reflux.best.<영웅>`.

**보상**: 처치 포인트 무한 규칙 · 첫 클리어 3,000P · 그 뒤 클리어마다 800P (`Point.Bonus`). 해제 조건(`CharUnlock`)은 **세지 않음** (10분 · 보스 5 조건이 「완성된 몸」으로 거저 되므로).

**계약**: 쉬움 · 보통 칸 대신 **역류 전용 2** (새 `ContractKind.Reflux`, 300 · 800P).
- `rf.dev2` 「개발자를 2분 안에」 · `rf.keepEvo` 「무기 진화를 리치 왕까지 하나도 잃지 않기」 · `rf.nakedNoHit` 「맨몸으로 처음의 슬라임을 맞지 않고」 · `rf.spirit3` 「남은 영혼 3 이상으로 끝까지」.

**업적**: `ACH_REFLUX_CLEAR` 역류 클리어 · `ACH_REFLUX_FAST` 8분 안에 클리어 · `ACH_REFLUX_NAKED` 맨몸 무피격 처음의 슬라임.

### 2-11. 구현 설계 (역류)
**상태가 사는 곳**: 새 `Modes/RefluxRun.cs` (정적 설정 + 판 안 MonoBehaviour `RefluxDirector` 를 StageManager 오브젝트에 붙임). EndlessMode 는 **그대로 붙인다** — `GameMode.EndlessSeconds` 가 `EndlessMode.Instance != null` 을 요구하고 HUD · Finish · 보스 소환 함수가 거기 있음. 대신 일정만 끈다.

| 파일 : 함수 | 할 일 | 대략 줄 |
|---|---|---|
| 새 `Modes/RefluxRun.cs` | Begin(hero, path, form) · End · Unlocked · VirtualSeconds · MobTier · Submit · `RefluxDirector`(시간표 · 잃기 타이머 · BuildOffers · StripAll · 처음의 슬라임 · 풍화 안전장치 · 클리어) | 450 |
| `GameMode.cs` | `RampSeconds` · 4곳 교체 | +6 |
| `EndlessMode.cs` : `Init` | 역류면 `StartAtLevel(10)` 건너뜀 · 지형 `Begin` 건너뜀 | +3 |
| `EndlessMode.cs` : `Update` | `public bool ScriptedTimeline` — 참이면 15:00 판정 · 보스 · 중간 보스 일정 건너뜀 (HUD · 풍화는 그대로) | +4 |
| `EndlessMode.cs` : `SpawnBoss` · `SpawnFinalBoss` · `SummonDeveloper` | `internal` 로 열고 종류를 고르는 `SpawnBossKind(int kind, float hpMul)` 추가 · 개발자 체력 배율 인자 | +30 |
| `EndlessMode.cs` : `OnBossDefeated` | 역류면 진화 · 전리품 대신 `RefluxDirector.OnBossDown` | +4 |
| `EndlessMode.cs` : HUD | 역류면 가상 시계 · 「잃기 n/9 · 보스 n/6」 | +6 |
| `LevelShop` (exp · 레벨업) | 역류면 경험치 0 · 레벨업 창 안 엶 (`GrantLevels` · 경험치 받는 곳 한 줄) · HUD 레벨 표시는 `RefluxRun.ShownLevel` | +8 |
| `LevelShop.Modes.cs` | `AutoFill` · `UndoCard` (공용 카드 반대 표) | (위 280에 포함) |
| `CharacterKit.cs` : `CardLost(slot)` | 영웅마다 `CardPicked` 반대 (소환물 제거) — 새 영웅 5명은 hero 문서 구현 때 같이 | +60 |
| `SpecialAbilities.Modes.cs` | `UndoEvolution` · 축복 적용 · 해제 | (위 200에 포함) |
| `Paths.cs` | `Disable()` | +8 |
| `Shop.cs` · 상자(`ChestRewards`) · `SoulShards` · `SoulTreeUI` 버튼 · 중간 보스 | 역류면 끔 (각 입구 한 줄 `if (ModeRun.Reflux) return`) | +10 |
| `EnemySpawner` 적 고르기 | `MobTier` 거름 | +6 |
| `LevelPips` | 음수 add (빠질 칸 흐리게) | +6 |
| `ChoiceUI` | 그대로 재사용 (고칠 것 없음) | 0 |

**저장**: 역류는 판 중간 저장 **하지 않음** (`RunSave.Savable` 에 `!ModeRun.Reflux` · 10분 모드 · 「되돌림」 기록 재생을 만들 가치가 없음). ESC 「포기」는 무한 모드처럼 (`ESCmenu.GiveUpMode` 가 이미 IsEndless 로 참). 메인 메뉴로 나가면 판은 사라진다고 ESC 창에 한 줄 (`RunSave.CanSaveNow` 가 false 라 기존 안내 「저장되지 않습니다」가 그대로 뜸).

---

## 3. 모드 플래그를 확인하는 기존 곳 — 새 모드가 끼는 방법
범례: **그대로** = IsEndless 참이라 무한처럼 동작해도 됨 · **Special** = 조건을 `ModeRun.Special` 로 바꿈 · **역류 끔** = `!ModeRun.Reflux` 추가.

| 파일:줄 | 지금 조건 | 릴레이 | 역류 |
|---|---|---|---|
| Contracts.cs:71 `Story` | `!IsEndless && !Daily` | 그대로 (무한) | 그대로 |
| Contracts.cs:280 `runStory` · :283 `Preview` · `KindsFor` | 일일 도전 분기 | 릴레이 분기 (1-7) | 역류 분기 (2-10) |
| DailyRewards.cs:71 · :82 | 일일 도전만 | 그대로 (안 붙음) | 그대로 |
| ESCmenu.cs:275 `GiveUpMode` | 무한 · 일일 | 그대로 | 그대로 |
| EndlessMode.cs:140 `daily` | 기록 섞지 않기 | `Finish` 분기로 대신 | 같음 |
| EndlessMode.cs:190 · :525 · :530 이정표 `!daily` | 일일 도전 제외 | **Special** | **Special** |
| EndlessMode.cs:189 · :485 · :524 `CharacterData.Achieve` | 무한이면 셈 | 그대로 (셈) | **역류 끔** |
| EndlessMode.cs:490 최종 보너스 `!Daily` | | 그대로 (받음) | 해당 없음 (엄마 없음) |
| EndlessMode.cs:736 최고 기록 `!daily` | | **Special** | **Special** |
| Herald.cs:133 연속 사망 | 일일 · 튜토리얼 0 | 그대로 (마지막 죽음만 셈) | 그대로 |
| Ladder.cs:159 · RunRecords.cs:36 · SoulBrand.cs:33 · SoulWeight.cs:53 | 무한 제외 | 그대로 | 그대로 |
| LeaderboardUI.cs:207 | 일일 순위 줄 | 릴레이 · 역류 탭 (0-4) | 같음 |
| LevelShop.Paths.cs:283 갈래 뽑기 시드 | 일일 = 고정 시드 | 그대로 (최근 가중) | 자동 채우기는 고정 시드 (2-5) · 판 안 뽑기 없음 |
| MenuExtras.cs:111 결과 기록 줄 | 일일 / 무한 | 릴레이 줄 | 역류 줄 |
| ResultScreen.cs:93 · :116 · :339 · :357 · :485 | 숙련 · 이정표 · 계약 · 연속 · 추천 | :116 이정표 다음 목표 **Special** 로 숨김 · 나머지 그대로 | 같음 + :485 「다음에 해 볼 것」 추천 숨김 |
| RunSave.cs:138 `Savable` | 일일 제외 | 그대로 (저장함 · 1-9) | **역류 끔** |
| SpecialAbilities.Evolution.cs:865 `RefineOldCurve` | 무한 · 일일 | 그대로 | 해당 없음 (트리 끔) |
| StageManager.cs:292 숙련 보너스 · :902 · StoryDirector.cs:384 | 이야기 판 | 해당 없음 | 해당 없음 |
| StageManager.cs:225~ `Opening` (무한 길) | | 그대로 + `HeroGuide` 는 첫 주자만 | `StoryDirector.Intro` · `HeroGuide` 건너뜀 → `RefluxDirector.Begin` (자동 채우기 · 배너) |
| GameMode.cs `Ramp` · `MobRamp` · `EarlyT` · `Surge` | `EndlessSeconds` | 그대로 | `RampSeconds` |
| Shop.cs:103 · :115 · :191 (코인 상점) | 무한 값 | 그대로 | **역류 끔** |
| Content/MapProps.cs:50 `Off` | 무한이면 꺼짐 | 그대로 | 그대로 |
| SteamManager.cs:178 · :258 업적 (무한 10 · 20분 · 보스 5) | 무한이면 셈 | 그대로 (셈) | **역류 끔** |
| RunCounters.cs `Off` · TutorialRun.Active 확인 40여 곳 | 튜토리얼 | 그대로 (튜토리얼과 배타) | 그대로 |
| MusicManager.cs:90 · :107 | 무한 곡 | 그대로 | 그대로 (개발자 곡은 SummonDeveloper 가 켬) |
| Point.cs:25 처치 포인트 줄어듦 | 무한 | 그대로 | 그대로 |
| CharacterData.Selected 를 Start/Awake 에서 캐시하는 곳 | — | **Rebind** (1-8 7번) | 해당 없음 (영웅 안 바뀜) |

---

## 4. 위험 · 놓치기 쉬운 예외
**릴레이**
1. **키트 Start 지연**: 새 `CharacterKit` 의 `Start` 는 다음 프레임 — 재생(이어하기) 중엔 Restore 가 체력을 저장값으로 넣은 뒤 Start 가 `PlayerHealth = Max` 로 덮음 → `ApplyDef()` 를 교대 안에서 바로 (1-8 6번). 가장 터지기 쉬운 곳.
2. **같은 프레임 여러 피해**: 교대 직후 같은 프레임의 다음 피해가 새 영웅을 다시 죽임 → 교대 첫 줄에서 `invincibleUntil` 설정 · `TryHandoff` 는 `Swapping` 이면 거절.
3. **키트 OnDestroy 누락**: 영웅 소품(도형 · 톱니 · 우편 · 더미 · 묘비 · 들린 적)이 정리되지 않으면 다음 영웅 판에 떠다님. 기존 9명은 `OnDestroyCartographer` 등이 있음 — **판 끝(씬 전환)에만 불리던 정리라 판 중간 호출을 견디는지** 영웅마다 확인. 새 영웅 5명은 hero 문서에 「판 중간 정리」를 요구.
4. **정적 이벤트 구독**: `EnermyController.Killed` · `PlayerController.UltUsed` 를 키트 · SignatureSkills 가 구독 — Rebind 때 두 번 구독하지 않게 (`-=` 후 `+=`).
5. **`EnermyController.DamageHook` · `Decoy` · `GlobalSpeedMultiplier`** (SpecialAbilities 정리 156~158줄): 앞 영웅이 걸어 둔 전역 훅이 남으면 새 영웅에게 엉뚱한 효과 → 교대 때 키트가 건 것만 풂.
6. **거너 ↔ 거너 아님**: 애니메이터 켜기/끄기 · `PlayerLook` 의 권총 · 탄창 HUD(`AmmoPanel`) 표시 여부 · `UsesAmmo`.
7. **보스가 흉내 내는 영웅**: 개발자 · 거울의 군주가 `Selected` 로 패턴을 고름 — 싸우는 도중 영웅이 바뀌면 다음 패턴부터 새 영웅 흉내 (괜찮음 · 오히려 재미). 이미 시작한 패턴은 그대로.
8. **필살기 연출 중 쓰러짐**: `CancelSkill` · 줌 · 시간 배율 원래대로 (`Time.timeScale` 은 교대 코루틴이 1로 마무리).
9. **레벨업 · 진화 창이 열린 채 죽을 수는 없음**(창이 멈춤) — 하지만 시간 0.35배 연출 중에 대기 레벨업 창이 열리지 않게 `LevelShop` 열기 조건에 `RelayRun.Swapping` 추가.
10. **영웅 해금 취소**: 이어하기 저장의 셋 중 하나가 잠기면(체험판 전환 · 저장 꼬임) `Peek` 이 null → 이어하기 버튼이 안 보임 (판 사라짐 · 정상).
11. **일일 도전과 겹침**: `ModeRun.EndAll()` 로 배타. 일일 도전 창에서 시작하면 릴레이가 풀림.
12. **자동 조작 · 배치 테스트**: `GameInput.Auto` 면 `GameMode.Current` 가 Override 를 먼저 보므로 `RelayRun.BeginForTest(order)` 로 돌릴 수 있게 (봇 균형 측정용). 업적 · 순위 · 포인트는 기존 Auto 조건이 막음. `AutoplayBot` 이 `CharacterKit.Instance` 를 캐시하면 교대 뒤 다시 찾게.
13. **스팀 없음**: 순위 창은 「스팀에 연결되어 있지 않아…」 그대로, 로컬 최고 기록은 남음.
14. **빈 상태**: 처음 여는 릴레이 창(지난 조합 없음) · 조합 기록 없음 「아직 기록이 없습니다」 · 가진 영웅이 정확히 셋이면 순서만 고름.

**역류**
15. **되돌림 누락**: 공용 카드 반대 연산 표가 `ApplyPick` 과 어긋나면 잃어도 힘이 남거나 마이너스로 감. `UndoCard` 는 `ApplyPick` 바로 옆에 두고, 개발 빌드에서 「채우기 → 전부 되돌림 → 처음 값과 비교」 검사 한 번 (`#if UNITY_EDITOR` 가 아니라 `Debug.isDebugBuild` 로 · AGENT.md 규칙 2).
16. **진화를 잃었는데 갈래 · 스킬 진화가 그 형태를 전제**: 1차 무기를 잃은 뒤 형태 전용 갈래 효과가 null 참조 → 1차 무기는 갈래보다 **뒤에** 나오게 (갈래가 남아 있으면 1차 무기 항목은 목록에서 뺌).
17. **잃기 창이 보스 결계 · 개발자 등장과 겹침**: 2-6 의 열기 조건. 결계 15초 동안 미뤄지면 그만큼 늦게 열림 (다음 잃기는 고른 때부터).
18. **맨몸 끝인데 보스가 남음**: 순서상 리치 왕 처치 뒤에만 맨몸이라 없음. 다만 9번째 잃기까지 보스를 못 잡으면 거의 맨몸으로 보스전 → 12:00 풍화 안전장치 (2-8).
19. **레벨업 · 경험치 끔의 빈틈**: 상자 · 상점 · 트리 · 전리품 · 이벤트(`ChapterEvents` · `RunContent`) · 사막 비밀의 「다시 했을 때 작은 보상(상자)」 — 하나라도 힘을 주면 안 됨 → 사막 비밀 재보상은 역류에선 포인트로 바꿈.
20. **저장 없음**: 판 도중 게임이 꺼지면 판이 사라짐 → 준비 창에 회색 한 줄 「역류는 중간 저장되지 않습니다」.
21. **체험판**: 둘 다 잠김 (Unlocked 에 `!Demo.On`).
22. **튜토리얼**: 배타 (TutorialRun.Begin 이 `ModeRun.EndAll()` 을 부르게).

---

## 5. 구현 순서 · 규모
1. 공통 뼈대 (`ModeRun` · `ModeHubUI` 껍데기 · 순위 정렬 인자 · 결과 줄 자리)
2. 릴레이 교대 엔진 (`RelaySwap` + 키트 `ApplyDef` · Rebind 들) — 기존 9명으로 먼저, 봇으로 9×8 교대 쌍 한 번씩 돌려 예외 확인
3. 릴레이 저장 · 이어하기 (`hz` 재생)
4. 유품 9개 → 새 영웅 구현과 함께 5개
5. 역류 시간 뒤집기 · 일정 (`RampSeconds` · `RefluxDirector`)
6. 자동 채우기 · 되돌림 (`AutoFill` · `UndoCard` · `UndoEvolution` · 축복) — 되돌림 검사 포함
7. 잃기 3택 · 맨몸 · 처음의 슬라임
8. 계약 · 업적 · 순위 탭 · 번역 4개 언어

규모 어림: **새 파일 8개 약 2,700줄** (ModeRun 60 · ModeHubUI 320 · RelayRun 300 · RelaySwap 220 · RelayHeirlooms 450 · RefluxRun 450 · LevelShop.Modes 280 · SpecialAbilities.Modes 200 + LocTable.Modes 약 400) · **고치는 파일 약 24개 약 450줄**. 새 그림: 모드 카드 2 · 유품 테두리 1 · 처음의 슬라임은 기존 그림 확대(새 그림 없음).

---

## 6. 결정 (추천)
| # | 결정 | 추천 | 이유 |
|---|---|---|---|
| M1 | 메뉴 자리 | 메인 목록에 「특별 모드」 한 줄 → 두 모드 카드 창 | 메인 버튼을 늘리지 말라는 사용자 의견 · 일일 도전은 그대로 |
| M2 | 두 모드 기반 | `GameMode.Override = Endless` (일일 도전 패턴) | 사막 · 무한 규칙 · HUD · 제출 길 재사용 |
| R1 | 넘겨주기 보상 | 체력 가득 · 무적 3초 · 파동(공격력 300%) · 게이지 가득 | 「죽음을 언제 쓸까」가 실제 선택이 되게 |
| R2 | 버린 영웅 카드 | 단계 × 0.5 무료 카드로 환급 (최대 12) + 진화 1 · 2차 다시 고름 · 각성 유지 | 새 주자가 맨손이 되지 않으면서도 「빌드를 잃는」 값이 남음 |
| R3 | 순위표 | 전체 1 + 조합(순서 무시) 364 | 순서까지 나누면 2,184개로 거의 비어 있음 |
| R4 | 유품 | 영웅마다 새로 만든 자동 발동 「동사 메아리」 1개 · 셋째 주자는 둘 | 기존 패시브 재사용(싼 안)은 맛이 밋밋 — 판 맛을 바꾸는 쪽 |
| R5 | 릴레이에서 영웅 해제 조건 · 사막 비밀 | 셈 | 무한 규칙 그대로 · 목숨 셋이어도 노력의 결과 |
| R6 | 릴레이 저장 | 함 (`hz` 기록 재생) | 무한처럼 길어서 |
| X1 | 역류 시작 몸 | 반자동: 영웅 · 갈래 · 1차 무기만 고르고 고정 시드로 레벨 40 · 각성까지 자동 | 내 선택 + 공정한 순위 + 메뉴 미리 보기 |
| X2 | 「지난 판 빌드」 | 2차로 미룸 (죽을 때 기록을 남기는 일이 필요) | 지금 저장은 죽으면 지워짐 |
| X3 | 트리 대신 「역류의 축복」 | 씀 | 트리 칸은 되돌리는 길이 없음 |
| X4 | 잃기 3택 묶음 | 공격 · 생존 · 영웅다움 세 종류에서 하나씩, 무게 ±30% | 매번 성격이 다른 셋 · 크기는 비슷하게 |
| X5 | 역류 순위 | 클리어 시간 하나(오름차순), 영웅별 보드 없음 | 자동 채우기로 몸이 맞춰짐 · 보드 쪼개짐 방지 |
| X6 | 역류에서 끄는 것 | 레벨업 · 경험치 · 상점 · 상자 · 조각 · 트리 · 중간 보스 · 지형 변화 · 영웅 해제 조건 · 무한 업적 | 쌓는 길이 하나라도 있으면 「잃기」가 흐려짐 |
| X7 | 역류 저장 | 하지 않음 | 10분 모드 · 되돌림 재생을 만들 가치가 낮음 |
| X8 | 맨몸 시점 | 리치 왕 처치 직후 (남은 잃기는 「남은 영혼」 점수) | 빨리 온 사람도 끝은 모두 맨몸 · 빠름이 순위로 보상 |
| X9 | 시간 뒤집기 | 곡선 식은 그대로, 넣는 시간만 `899 − 1.5 × 실제` | 무한 밸런스를 그대로 거꾸로 씀 · 고칠 곳 4줄 |
