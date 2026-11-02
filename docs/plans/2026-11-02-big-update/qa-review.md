# 2.0.0 출시 전 독립 코드 리뷰 (기준 커밋 `caa03706`)

- 범위: `git diff 75b724be caa03706 -- Assets/Scripts` (새 영웅 5명 키트 · EnemyHold · 판 중간 저장 · 릴레이 · 역류 · Impact/Juice 시간 배율 · DesertSecrets · Boot · CharacterUI)
- 방법: 읽기만 함 (코드 수정 없음). 위험 패턴을 grep 하고 호출 흐름을 따라가 확인함. 「확실」은 코드 경로를 끝까지 확인한 것이고, 「의심」은 실행 환경이나 경합에 달린 것.
- 리뷰할 때 작업 트리와 `caa03706` 의 `Assets/Scripts` 는 같았음. 줄 번호는 `caa03706` 기준.

## 요약

| 심각도 | 개수 |
|---|---|
| 치명 (튕김 · 진행 불가 · 저장 깨짐) | 1 (의심 · 에디터에서 확인 필요) |
| 높음 | 4 |
| 중간 | 15 |
| 낮음 | 6 |

확실한 튕김(예외로 게임이 멈추는 것)이나 저장 파일 손상은 찾지 못했다. 1.6.0 옛 저장, 계약 보드 키, CharacterId 9 는 문제없음(아래 「확인한 곳」 참고). 가장 위험한 곳은 두 군데다.
- 판 중간에 영웅을 바꾸는 `RelaySwap` 이 물리 콜백 안에서 `DestroyImmediate` 를 부르는 것
- 특별 모드(릴레이 · 역류)의 정적 상태가 「다시 시작」 때 초기화되지 않는 것

---

## 치명

### C1. [치명 · 의심] 영혼 릴레이 교대가 물리 충돌 콜백 안에서 `DestroyImmediate(oldKit)` 를 부름

→ 고침 (Modes/RelayRun.cs): TryHandoff 는 체력 1 + 무적 3초 + HandoffPending 깃발만 걸고, 실제 교대(DoHandoff)는 코루틴 yield return null 뒤(물리 콜백 밖)에서. 예약 중 또 맞아도 교대 한 번 · 쓰러짐 시작 안 함
- 위치: `Modes/RelaySwap.cs:55`. 호출 경로는 `PlayerController.cs:1427` (OnTriggerStay2D) → `HandleContact` → `TryHit` (`:1547`) → `RelayRun.TryHandoff` → `RelaySwap.Run`. 잡몹 접촉(`EnermyController.TouchPlayer`, OnTriggerEnter/Stay2D)과 적 탄도 같은 경로다.
- 근거: 교대할 때 가장 흔한 죽음은 보스 · 잡몹 몸에 닿거나 탄에 맞는 것이고, 이 판정은 모두 트리거 콜백 안에서 돈다. 그런데 Unity 는 물리 트리거 · 충돌 콜백 안에서 `DestroyImmediate` 를 막는다. 이때 「Destroying object immediately is not permitted during physics trigger/contact…」 오류만 남기고 지우지 않는다. 2D 물리에서도 막히는지는 엔진 버전마다 확인이 필요하다.
- 막힐 경우:
  - 옛 키트가 남고 `CharacterKit.Attach` 가 새 키트를 하나 더 붙인다.
  - 곧바로 `p.GetComponent<CharacterKit>()` 가 옛 키트를 돌려주어 `ApplyDef` 가 엉뚱한 키트에 걸린다.
  - 결과는 키트 두 개가 동시에 돌거나, `CharacterKit.Instance` 가 엉킴 → 평타 · 필살기 이상, 배율 이중 적용 등이다. 판이 사실상 망가진다.
- 재현: 릴레이에서 첫 주자가 보스 몸통 접촉이나 잡몹 접촉으로 쓰러지게 둔다. 그다음 콘솔에서 위 오류가 나는지, 플레이어 오브젝트에 `CharacterKit` 컴포넌트가 두 개 붙었는지 본다.
- 고치는 법:
  - 쉬운 방법은 `TryHandoff` 에서 무적과 체력 1 만 주고, 실제 `RelaySwap.Run` 은 다음 프레임(`RelayRunner` 코루틴의 `yield return null` 뒤)에 돌리는 것이다.
  - `DestroyImmediate` 를 그대로 쓰려면, 콜백 밖(Update · 코루틴)에서만 부르도록 보장한다.

---

## 높음

### H1. [높음 · 확실] 영혼 릴레이 「다시 시작」하면 마지막 주자 한 명으로 시작하고 교대가 없음

→ 고침 (Modes/RelayRun.cs · Modes/ModeRun.cs · SceneFlow.cs): SceneFlow.Go("GameScene") → ModeRun.BeforeGameScene 한 곳에서, Begin/Resume 이 막 준비한 첫 판이 아니면 Index · Override · 구간 · 유품 · 빚을 새로
- 위치: `Modes/RelayRun.cs:75-90` (Setup 만 `Index = 0` · `ResetRunState`), `:119-127` (씬 로드 때는 MainMenu 에서만 End). 다시 시작 경로는 `ESCmenu.cs:295` Restart, `ResultScreen.cs:481` 「같은 영웅으로 다시」, `RunStats.cs:839` QuickRestart [R].
- 근거: 이 세 경로는 `SceneFlow.Go("GameScene")` 만 하고 `RelayRun.Begin/Setup` 을 다시 부르지 않는다. 그래서 다음 값이 그대로 남는다.
  - `Index` (예: 2)
  - `CharacterData.Override = Current` (셋째 주자)
  - `SegmentStart`, `FreeLeft`, `EvoOwed`
  - `RelayHeirlooms.owned` (static 목록)
  - `bossMask`
- 결과:
  - 셋째 주자로 시작하고 `TryHandoff` 의 `Index >= Runners - 1` 때문에 한 번 죽으면 바로 끝난다.
  - 시작부터 앞 판 유품을 들고 있다.
  - 결과 화면 구간 시간이 엉망이 되고, 이 기록이 조합 순위표에 올라간다.
- 재현: 릴레이에서 셋째 주자까지 죽은 뒤 결과 화면에서 [R] 을 누르거나 「같은 영웅으로 다시」를 고른다. ESC 다시 시작도 같다(둘째 주자일 때는 둘째부터 시작).
- 고치는 법: `RelayRun` 의 GameScene sceneLoaded 에서 이어하기(`RunSave.Pending == null`)가 아니면 `Setup(Order)` 처럼 `Index = 0` · `Override = Order[0]` · `ResetRunState` · `SegmentStart` 를 초기화한다. 또는 세 경로에서 `RelayRun.Begin(Order)` 를 부른다.

### H2. [높음 · 확실] 역류 「다시 시작」하면 앞 판의 클리어 · 맨몸 · 잃기 상태가 남아 기록이 오염됨

→ 고침 (Modes/RefluxRun.cs · 같은 시작 지점): BeforeGameScene 에서 다시 시작이면 ResetRunState (Cleared · Naked · LossCount …)
- 위치: `Modes/RefluxRun.cs:66-77` (Setup 만 `ResetRunState`), `:110-112` (MainMenu 에서만 End). `Submit` `:160-178`, `TierMul` `:126-134`.
- 근거: H1 과 같은 이유로 다음 static 값이 다음 판까지 남는다.
  - `Cleared`, `ClearSeconds`, `LossCount`, `BossCount`, `Naked`, `NakedHit`, `ShownLevel`, `Kept`
- 결과:
  - 앞 판을 클리어했으면 `Cleared = true` 인 채로 시작한다. 새 판에서 죽기만 해도 `Submit` 이 **앞 판 클리어 시간을 다시 순위표 · 로컬 최고에 올린다**. 결과 화면에도 「클리어」가 뜬다.
  - `Naked = true` 라서 처음부터 1장 몹만 나온다.
  - `LossCount = 9` 라서 「무엇을 잃을까」가 한 번도 안 뜬다.
  - HUD 가 「잃기 9/9 · 보스 6/6」으로 나온다.
- 재현: 역류를 클리어하거나 맨몸까지 간 뒤 결과 화면에서 [R] 을 누르거나 「같은 영웅으로 다시」를 고른다.
- 고치는 법: `RefluxDirector.Begin` (판마다 새로 붙음) 첫 줄에서 `RefluxRun.ResetRunState()` 를 부른다. `internal` 로 열어 두면 된다.

### H3. [높음 · 확실] 씨름꾼 「한 아름」(card[1])으로 함께 든 적이 영영 붙잡힌 채 남음

→ 고침 (CharacterKit.Wrestler.cs): WrReleaseExtras 추가 — 주된 적이 죽거나 빼앗기면 · 다음 잡기 전 · 던질 적이 이미 죽었을 때 동행을 풀고 바닥에 내려놓음
- 위치: `CharacterKit.Wrestler.cs:401` (WrLate), `:251` (WrGatherExtra), `:245`.
- 근거: 주된 적이 들린 채 죽으면 `wrCarried = null; wrExtra.RemoveAll(...)` 만 한다. 살아 있는 동행 적은 풀어 주지 않는다. 다음 잡기의 `wrExtra.Clear()` 가 목록에서 지워 버려 `holder == this` 가 끝까지 남는다.
- 결과: 동행 적이 공중에 얼어붙는다(이동 · 접촉 · 밀어내기 없음). 그림자는 꺼지고, 정렬은 플레이어 위로 고정되고, 다시 잡을 수 없다. 씨름꾼 평타 · 핀 · 충격파가 `holder != null` 인 적을 건너뛰어 범위 피해로만 죽는다. 교대하거나 죽을 때까지 풀리지 않는다.
- 재현: 한 아름 카드를 가진 채 둘을 든다. 그다음 투사체 막기(`WrBlock`), 워낭, 배지기 등으로 주된 적이 들린 채 죽게 하고, 다시 평타로 잡는다.
- 고치는 법:
  - 401행에서 `WrReleaseCarried(true)` 로 동행 적을 바닥(`FreeSpot`)에 내려놓는다.
  - `WrGatherExtra` 의 `Clear()` 전에 남은 `wrExtra` 를 `EnemyHold.Release` 한다.

### H4. [높음 · 확실(동작) / 의심(스택 오버플로)] 인형사 「끊어진 실」이 실로 죽인 적에서 또 튀며 같은 프레임에 끝없이 재귀함

→ 고침 (CharacterKit.Puppeteer.cs): pupThreading 깃발로 끊어진 실이 도는 동안 PupKilled 재진입 차단 (try/finally)
- 위치: `CharacterKit.Puppeteer.cs:478-480` (PupApplyCut 이 `pupCutting = true` 인 채 `Specials.Damage`), `:577-579` (PupKilled), `:585-607` (PupBrokenThread 가 다시 `Specials.Damage`).
- 근거: 끊어진 실이 다음 적을 죽이는 순간에도 `pupCutting` 이 true 다. 그래서 `OnKill → PupKilled → PupBrokenThread` 가 다시 불린다. 6칸 안에 한 방에 죽는 적이 이어져 있으면, 그 무리가 다 죽을 때까지 콜 스택이 깊어진다(한 단계마다 Damage · Die · Killed 이벤트 · OnKill 이 쌓임).
- 결과:
  - 설계보다 훨씬 센 연쇄가 난다. 「줄로 벤 적만」 튀어야 한다.
  - 끝판 밀집에서는 `StackOverflowException` 가능성이 있다. 이 예외는 잡을 수 없어 프로세스가 강제 종료된다.
- 재현: 인형사로 끊어진 실을 찍고 공격력을 높게 올린 뒤, 15분 이후 빽빽한 무리를 줄로 가른다.
- 고치는 법: `PupKilled` 에서 `PupBrokenThread` 를 부르기 전후로 `pupCutting` 을 끈다(`bool was = pupCutting; pupCutting = false; … pupCutting = was;`). 또는 재진입 막는 깃발을 둔다.

---

## 중간

### M1. [중간 · 확실] 릴레이 교대 `TimeDip` 이 창 · ESC 에 끊기면 게임이 느린 채 남고, 환급 무료 카드가 멈춤

→ 고침 (Modes/RelayRun.cs · Juice.cs 외): TimeDip 은 timeScale 0 이면 기다림 + 창들이 시간을 되돌릴 때 Juice.Settle 로 중간값이면 1
- 위치: `Modes/RelayRun.cs:261-275`. 함께 걸리는 곳은 `ESCmenu.cs:350/358` (`timeScaleBeforeOpen` 복원), `ChoiceUI.cs:112/312`, `StageManager.cs:533/543`, `BossUltimate.cs:241/326`, `LevelShop.Modes.cs:67`.
- 근거: TimeDip 은 시간을 0.35→1 로 0.6초 동안 올린다. 그사이 ESC 등이 0.35~0.99 값을 `before` 로 저장하고 0 으로 멈추면, TimeDip 은 `yield break` 로 끝난다. 창이 닫히며 그 중간값을 되돌리면 1 로 돌려놓을 주체가 없다.
- 결과:
  - 다음 레벨업 창이 닫힐 때까지 0.4~0.9배로 느리게 진행된다.
  - 그동안 `Juice.CanStop` 이 `timeScale == 1` 을 요구해 손맛 연출도 꺼진다.
  - `RelayOpenFree` 가 `timeScale != 1` 동안 기다려 **환급 무료 카드 창이 안 열린다**.
- 재현: 교대 직후 0.6초 안에 ESC 를 눌렀다가 닫는다.
- 고치는 법: TimeDip 이 `timeScale == 0` 이면 기다렸다가 이어 가게 하고, 끝에서 무조건 1 로 둔다. 또는 `Juice.Shared.Hold` 같은 소유권 있는 시간 관리로 옮긴다.

### M2. [중간 · 의심] JuiceRunner 가 손을 뗀 뒤 바깥 창이 옛 슬로값(0.3)을 되돌리면 0.3배로 고정됨

→ 고침 (Juice.cs: Settle · ESCmenu · ChoiceUI · StageManager · BossUltimate · SpaceErosion · ChestRewards): before 가 0~1 사이 중간값이고 조준 중 · 쓰러지는 중 · Juice 소유가 아니면 1 로
- 위치: `Juice.cs:337-344` (LateUpdate 의 Release), `Impact.cs:99` (`Hold(0.3, slow)`).
- 근거: 경로는 이렇다.
  1. Impact 슬로 0.3 중에 창 A 가 0.3 을 `before` 로 저장하고 멈춘다.
  2. 그 안에서 창 B(예: `LevelShop.closeLevelShop` → 1)가 직접 1 로 둔다.
  3. LateUpdate 가 소유권을 놓는다.
  4. 창 A 가 닫히며 0.3 을 되돌린다 → 주인 없는 0.3배.

  예전 HitStop 도 같은 구조였지만, 2.0.0 Impact 가 평타 특수 조건마다 0.3배를 걸어 빈도가 크게 늘었다.
- 고치는 법: 창들이 `before` 를 되돌릴 때 `before < 1 && !Juice.Shared.Owns` 이면 1 로 둔다. 또는 JuiceRunner 가 마지막으로 건 값과 같은 값이 주인 없이 남으면 1 로 돌리는 안전망을 둔다.

### M3. [중간 · 의심] 릴레이 교대 중 Safe 밖 단계에서 예외가 나면 반쯤 바뀐 상태로 남음

→ 고침 (Modes/RelaySwap.cs): 2단계(배율)와 6단계(키트 갈아 끼우기)도 Safe 로 감쌈 — Record · 저장은 항상 실행
- 위치: `Modes/RelaySwap.cs:30-41` (배율 나누기), `:57-72` (`CharacterKit.Attach` · `ApplyDef` · `ApplyGunnerDef` 가 `Safe` 밖), `RelayRun.cs:146-150` (`Index++` 를 Run 보다 먼저 함).
- 근거: 이 구간에서 예외가 나면 `finally` 가 `Swapping` 만 끈다. `Record("hz")` · 저장은 건너뛰고 예외는 `TryHit` 까지 올라간다.
- 결과: `Index` 는 올랐는데 키트가 없거나 반쯤 붙고, 체력 0 인데 `DeathSequence` 도 시작되지 않는다. 다음 피격에서 또 교대하거나 판이 이상하게 이어진다. 이어하기 기록에도 `hz` 가 빠진다.
- 고치는 법: 6~7단계도 `Safe` 로 감싼다. `TryHandoff` 는 `Run` 이 성공한 뒤에만 `Index` 를 확정하거나, catch 에서 `DeathSequence` 로 떨어지게 한다.

### M4. [중간 · 의심] Boot 씬에서 SceneFlow 가 실패하면 입력 없는 빈 화면에 갇힘

→ 고침 (BootLoader.cs): 8초 뒤에도 활성 씬이 Boot 이면 SceneManager.LoadScene("MainMenu") 감시
- 위치: `BootLoader.cs:8-11`, `SceneFlow.cs:43` (`Guard(..., null)`), `SceneFlow.cs:298-303`.
- 근거:
  - `Ensure()`/`Build()` 예외, `Run` 안의 예외(`onFail` 이 null), `op == null` 이면 모두 Boot 씬(카메라만 있음)에 남는다.
  - 예전에는 첫 씬이 MainMenu 라 같은 실패가 나도 메뉴는 쓸 수 있었다.
  - 빌드 설정 0번 = Boot, 씬 이름, 스크립트 guid 는 모두 맞아서 실제로 터질 가능성은 낮다.
- 고치는 법: `try { SceneFlow.Go("MainMenu"); } catch { SceneManager.LoadScene("MainMenu"); }` 로 감싼다. 몇 초 뒤에도 활성 씬이 「Boot」 면 `SceneManager.LoadScene("MainMenu")` 를 부르는 감시도 둔다.

### M5. [중간 · 의심] `DesertSecrets.Build` 중 예외가 나면 비밀이 0.5초마다 중복 생성되고 static 이벤트도 중복 구독됨

→ 고침 (Content/DesertSecrets.cs): built = true 를 Build 앞으로
- 위치: `Content/DesertSecrets.cs:94-102` (`built = true` 가 Build 뒤), `:113-129`, `:550` (`PlayerController.AttackFired += OnAttack`).
- 결과: 판 내내 비밀 오브젝트가 쌓이고, 렉과 매 프레임 예외 로그가 생긴다. 지금 코드에서 예외가 날 지점은 뚜렷하지 않다.
- 고치는 법: `built = true` 를 Build 호출 앞으로 옮기거나, Build 를 try/catch 로 감싼다.

### M6. [중간 · 확실] 씨름꾼 천하장사 대상 보스가 도중에 파괴되면 MissingReferenceException

→ 고침 (CharacterKit.Wrestler.Ult.cs): boss && tg != null
- 위치: `CharacterKit.Wrestler.Ult.cs:178`. `boss ? tg.gameObject : null` 에 null 확인이 없다(바로 위 177행은 확인함).
- 결과: 코루틴이 예외로 끝난다(`finally` 로 깃발은 복구됨). 게이지만 쓰고 메침 · 모래판이 없다. 튕기지는 않는다.
- 고치는 법: `boss && tg != null ? tg.gameObject : null`.

### M7. [중간 · 확실] 씨름꾼 진화 「황소 장사」(wre.giantbull)가 동작하지 않고 적 그림자 · 정렬이 망가진 채 남음

→ 고침 (CharacterKit.Wrestler.Ult.cs): 황소 장사 분기에서 WrSlamAt 이 Release 한 뒤 TryTake 로 다시 잡고 Raise
- 위치: `CharacterKit.Wrestler.Ult.cs:180-191`, `:206`.
- 근거: `WrSlamAt` 이 먼저 `EnemyHold.Release` 한 뒤, holder 없이 `wrCarried = e; EnemyHold.Raise(e, …)` 를 한다. 그러면 다음 프레임에 `wrCarried = null` 이 된다. `Raise` 가 끈 그림자 · 정렬은 `holder == null` 이라 `Restore` 가 영영 불리지 않는다. 그동안 static `saved` 딕셔너리에 항목이 쌓인다.
- 고치는 법: 진화 분기에서는 `WrSlamAt` 이 Release 하지 않게 한다. 또는 `TryTake(e, this, true)` 로 다시 잡은 뒤 `Raise` 한다.

### M8. [중간 · 의심] 장의사 장례식 극의의 두 번째 장례와 새 장례가 static `utFall` 을 함께 써서 붙잡힌 적이 안 풀림

→ 고침 (CharacterKit.Undertaker.Ult.cs): 두 번째 장례(echo)도 utFuneralBusy 를 잡아 새 장례가 utFall 을 지우지 못하게
- 위치: `CharacterKit.Undertaker.Ult.cs:19, 117, 155-168`, `CharacterKit.Undertaker.Paths.cs:168-174`.
- 근거: 두 번째 장례(echo)는 `utFuneralBusy` 를 켜지 않는다. echo 가 기다리는 1~2초 사이에 새 장례가 시작되면 `utFall.Clear()` 가 echo 의 목록을 지운다.
- 결과: echo 가 잡은 적은 holder 가 남아 멈춘 채로 있다(피해는 받음). `spots[i]` 가 범위를 벗어나면 코루틴이 예외로 끝난다.
- 고치는 법: 장례마다 지역 목록을 쓰거나, echo 에도 busy 깃발을 켠다.

### M9~M14. [중간 · 확실] 릴레이 교대(`DestroyImmediate(oldKit)`)로 코루틴이 끊기면 남는 오브젝트 · 그림

→ 고침: M9 사진사 납작 사본 child 삭제 · M10 삼각대 삭제 (CharacterKit.Photographer.cs) · M11 인형사 pupEdgeCanvas · pupAimRing 삭제 (CharacterKit.Puppeteer.cs) · M12 장의사 매장 중 구멍 utBurying 목록 정리 (CharacterKit.Undertaker.cs) · M13 공사 반장 덤프 중 잔해 fmDumping 목록 정리 (CharacterKit.Foreman.cs) · M14 씨름꾼 들돌 삭제 (CharacterKit.Wrestler.cs)
교대 때 키트의 `OnDestroy*` 가 정리하지 않는 것들이다. 붙잡힌 적 자체는 `RelaySwap.cs:26` 의 `ReleaseAll(oldKit)` 이 풀어 주므로 진행 불가는 아니다. 교대 때 「키트가 만든 모든 오브젝트를 키트 필드 목록으로 들고 `OnDestroy*` 에서 지운다」는 규칙 하나로 함께 고친다.

| # | 영웅 | 위치 | 남는 것 | 고칠 곳 |
|---|---|---|---|---|
| M9 | 사진사 | `CharacterKit.Photographer.cs:907-911`, `:407` | 종이 상태였던 적에 붙은 납작 사본 렌더러가 그 판 내내 두 겹으로 보임 (사진사는 늘 종이를 만들어 **거의 매번** 생김) | `OnDestroyPhotographer` 에서 `PhPaperEnd` 와 같이 `p.child` 를 끄거나 지움 |
| M10 | 사진사 | `CharacterKit.Photographer.cs:741-742, 779-788` | 삼각대 카메라 오브젝트 | `OnDestroyPhotographer` 에서 `phTripods` 를 Destroy |
| M11 | 인형사 | `CharacterKit.Puppeteer.Ult.cs:298-311, 347-371`, `CharacterKit.Puppeteer.cs:803-811` | 커튼콜 중 교대하면 화면 좌우 붉은 막이 판 끝까지 남음 (일반 사망 연출 중에도 남음) | `pupEdgeCanvas` · `pupAimRing` 을 Destroy |
| M12 | 장의사 | `CharacterKit.Undertaker.cs:567, 586, 608, 1000` | 매장 0.15초 사이에 교대하면 구멍 그림 | 매장 중인 구멍 목록을 따로 들고 정리 |
| M13 | 공사 반장 | `CharacterKit.Foreman.cs:426-489` | 덤프 중 날아가던 잔해 (`FmRubble`) | 목록으로 관리하고 정리 |
| M14 | 씨름꾼 | `CharacterKit.Wrestler.cs:860-874` | 날아가던 들돌 | 목록으로 관리하고 정리 |

### M15. [중간 · 의심] 역류 「처음의 슬라임」이 `onKilled` 없이 사라지면 판이 영영 끝나지 않음

→ 고침 (Modes/RefluxRun.cs): RefluxDirector.Tick 안전망 — 슬라임이 나온 뒤 사라지거나 죽었는데 끝나지 않았으면 Clear()
- 위치: `Modes/RefluxRun.cs:672` (`slime.onKilled += OnSlimeKilled`), `:686-699`, `EnermyController.cs:503-511` (`Purge` → `Die(0)` 은 onKilled 를 부르지 않음).
- 근거: 클리어 조건은 `onKilled` 하나뿐이다. 슬라임이 `Purge()`(보스 처치 화면 정리 · `spawner.ClearMobs`)나 다른 영웅 「장외」처럼 `Die(0)`/`Destroy` 로 사라지는 경로를 타면, `finished` 가 영영 안 켜진다. 그러면 적만 계속 나오는 끝없는 판이 된다.
- 지금 확인한 바로는 맨몸 단계에는 보스가 없어 `Purge` 는 잘 안 불린다. 새 영웅의 「장외 · 매장」 처치가 `Die(1)` 을 거치는지는 함께 확인할 것.
- 고치는 법: `RefluxDirector.Tick` 에서 `nakedStarted && slimeSpawned && (slime == null || slime.IsDead) && !finished` 이면 `Clear()` 를 부르는 안전망을 둔다.

---

## 낮음

| # | 위치 | 문제 | 고치는 법 |
|---|---|---|---|
| L1 | `CharacterKit.Puppeteer.cs:698, 784-791`, `SpecialAbilities.cs:129/159` | `EnermyController.DecoyBossToo` 가 판 · 씬 사이에 false 로 남을 수 있음 → 다음 판 도적 미끼가 보스를 못 끎 | `SpecialAbilities.Awake` 에서 `DecoyBossToo = true` 도 초기화 | → 고침 (SpecialAbilities.Awake)
| L2 | `PlayerController.cs:653`, `CharacterKit.Photographer.cs:579-589` | `PhReloadStart` 가 영웅을 확인하지 않음 → 도적 등 탄창 영웅이 재장전할 때 「clank」 소리 | 맨 앞에 `if (Id != CharacterId.Photographer) return;` | → 고침 (PhReloadStart 맨 앞 영웅 확인)
| L3 | `EnemyHold.cs:45-49, 85` | static `saved` 딕셔너리에 파괴된 적 키가 쌓임 (메모리만) | 씬 로드 때 `saved.Clear()` · `held.Clear()` | → 고침 (EnemyHold.ClearStatics 를 SpecialAbilities.Awake 에서)
| L4 | `Modes/RefluxRun.cs:648` (StripAll) | `while (sp.blessing[b] > 0) sp.RefluxBless(b, -1)` — `RefluxBless` 는 `player == null` 이면 값을 줄이지 않고 돌아가므로 그때는 무한 루프로 멈춤 (지금은 player 가 늘 있어 발생 가능성 낮음) | 루프에 횟수 상한을 두거나 `player` 를 먼저 확인 | → 고침 (RefluxRun 맨몸 단계 반복 상한 99)
| L5 | `CharacterKit.Puppeteer.Ult.cs:192` | 커튼콜 더미 그림 번호를 자르지 않음 (지금 시트는 모두 4프레임이라 안전, 2~3프레임 스킨이 생기면 매 프레임 예외) | `:235` 처럼 Clamp | → 고침 (CharacterKit.Puppeteer.Ult.cs 프레임 번호 Min)
| L6 | `Impact.cs` · `SpecialAbilities.cs:145` | 설정에서 「특수효과」를 끄면 `imp_*` 합성음이 등록되지 않아 사진사 비밀 셔터음 등이 안 남 · 첫 Impact 에 합성 클립 25개를 한꺼번에 만들어 짧은 끊김 | 소리 등록을 효과 설정과 떼어 판 시작 때 미리 | → 안 고침 (합성음 등록 시점 변경은 오디오 초기화 순서에 닿아 출시 직전에 위험)

---

## 확인한 곳 (문제 없음)

- **1.6.0 옛 저장 · CharacterId 9**
  - `Slot10` 은 `price = -1` 이라 `IsUnlocked` 가 늘 false 였다. 그래서 9번 해금 키 · 선택 · 판 저장이 있을 수 없다.
  - `RunSave.Version` 은 그대로이고 새 칸(`relay*`)은 기본값이면 「릴레이 아님」으로 읽힌다.
  - 특수 능력 번호는 기존 대역(20~87) 그대로다. 새 영웅은 140~174 다.
  - `IsAug` 가 범위(100~138) 판정으로 바뀌었지만 옛 진화 기록(100~123)과 맞는다.
- **abilities 배열 빈칸(88~139 null)**: 배열을 도는 곳(`SpecialTreeUI:121`, `MapProps:309`, `RunStats:267`, `SpecialAbilities.Chest/Relics`, `BossSkills:731`)은 모두 null 을 거른다. null 을 거르지 않는 `Equip` · `Hud` 는 영웅 pool · 장착 번호만 받는다.
- **계약 보드 9→14열 · `board.*` 키**: 칸 저장 키가 「영웅 키.줄」 문자열(`gunner.1`, `c.alleasy` …)이라 열 수가 바뀌어도 기존 완료 · 받음이 다른 칸으로 옮겨지지 않는다. `HeroKeys` · `Row1/Row3` 배열 길이는 14 로 맞다.
- **릴레이 저장 · 이어하기**
  - `character` 에는 첫 주자를 적는다.
  - `Resume` → `hz` 재생 때마다 `RelaySwap.Run(…, true)` → `ReplayAdvance` 로 `Index` 가 오르고, `RestoreAfterReplay` 가 마지막에 저장값으로 맞춘다.
  - 재생 중에는 `Record` · `TryHandoff` · 환급 · 체력 덮기를 건너뛴다.
  - 셋 중 하나라도 잠기면 `Peek` 가 null 을 돌려 이어하기가 숨는다.
  - 기준 공격력 비율 되돌림(`RelayRebaseForReplay`)은 곱셈 순서가 맞다.
- **역류**: 저장하지 않는다(`Savable` 에서 제외). `AutoFill` 은 `SetAutoFill` 을 try/finally 로 감싼다. 잃기 · 되돌리기(`UndoCard` · `UndoEvolution` · `RefluxBless`)는 0 · 음수 단계와 이미 없는 카드에서 일찍 돌아간다.
- **모드 플래그**: 메인 메뉴 진입(`MenuExtras.InstallMainMenu` → `ModeRun.EndAll`), 각 모드의 MainMenu sceneLoaded, 일일 도전 · 튜토리얼 시작에서 모두 풀린다. 남는 경로는 H1 · H2 의 「다시 시작」뿐이다.
- **이벤트 구독**: `RelayRunner` · `RelayHeirlooms` · `RunSave.Watcher` · `DesertSecrets` · 새 키트의 `Killed`/`UltUsed`/`Damaged`/`AttackFired` 는 `+=`/`-=` 짝이 맞다(Awake/OnDestroy, `defApplied` 가드).
- **Impact · JuiceRunner**
  - 대기는 모두 실제 시간이라 timeScale 0 에서도 끝난다.
  - 필살기 조준 · 창 · 사망 슬로 중에는 걸지 않는다.
  - 씬 전환은 `SceneFlow:317` 이 1 로 시작한다.
- **`SpecialFeedback.Shake`**: 「센 쪽 우선」 조기 return 이 시간이 지나면 풀려 영구로 막히지 않는다.
- **Boot**
  - 빌드 설정 0번이 Boot, 씬 이름, guid 가 모두 맞다.
  - AfterSceneLoad 로 만드는 오브젝트는 DDOL 이다.
  - MainMenu 설치 · 음악은 sceneLoaded 로 다시 처리한다.
- **CharacterUI 스크롤**: 0 나누기 없음, 인덱스는 `i % 5` · `i / 5` 만 쓴다. 비밀 영웅은 `???` 로 처리한다.
- **EnemyHold**
  - 죽음(`Die`), 풀 반환(`ReviveForPool`/`PoolDead`) 때 `OnGone` 으로 holder 가 자동으로 풀린다.
  - 풀로 도는 적은 부스러기(swarm)뿐이고 잡기 후보에서 걸러진다.
  - 붙잡힌 적도 피해는 받는다.
  - 붙잡힌 적을 빼고 세는 진행 조건은 없다.
- **새 영웅 키트 공통**
  - 목록 순회는 역순 for 이거나 복사본이다.
  - while 루프는 모두 줄거나 상한이 있다.
  - 분모는 하한 처리되어 있다.
  - `card[0..6]` 과 갈래 Badge 표는 Clamp 한다.
  - `Time.timeScale` 에 직접 대입하는 곳이 없다.
