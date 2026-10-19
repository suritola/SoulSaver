using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 판 중간 저장 (2.1.9~): 메인 메뉴로 나가거나 게임을 끄면 지금 판을 저장하고, 메인 메뉴 「이어하기」로 이어서 함
// 판의 상태를 통째로 적지 않고, 판을 바꾼 "선택"을 순서대로 적어 두었다가 이어할 때 그대로 다시 적용함
//   c<번호> 레벨업 카드 · f<번호> 시작 카드(레벨이 오르지 않음) · e<번호> 무기 진화 · t<키> 영혼 트리 칸 · s<번호,…> 특수 능력 · b<번호> 코인 상점
//   1.0.4~: 성장 카드도 c<100 ~ 103> · r 영혼 정련 한 번
//   1.2.3~: bt<번호> 무한 모드 보스 전리품
// 그 밖에 바뀌는 값(체력 · 경험치 · 남은 레벨업 · 코인 · 영혼 조각 · 처치 수 · 게이지 · 판 기록)은 저장할 때의 값을 그대로
// 이야기 · 무한 모드 판을 저장 (1.2.3~ 무한 모드도 같은 칸 · 일일 도전 · 체험판 · 자동 촬영 · 튜토리얼은 저장하지 않음) · 죽거나 4장을 클리어하거나 새 판을 시작하면 지움
public static class RunSave
{
    const string Key = "run.save";
    const int Version = 1;

    [System.Serializable]
    public class Data
    {
        public int version = Version;
        public string gameVersion;
        public int character, difficulty, stage, kills, specialPoints, rerolls;
        public int level, earned, pending, coins, shards, shardsTotal, shardsSpent;
        public float hp, nowExp, needExp, gauge, seconds;
        public float clock;                             // 화면 위 시계 (1.0.4~, 없으면 seconds 로)
        public string chapterLog = "";                  // 1.1~ 장에 들어온 시각 (RunStats.ChapterLog · balance_log 장별 소요 시간)
        public bool hasPos;                             // 나갈 때 서 있던 곳 (1.0.4~, 같은 장에서 보스가 나오기 전에만)
        public float px, py;
        // 1.0.4~: 끝나지 않은 일 — 보스를 잡고 문에 들어가기 전에 나갔음 · 보스 보상을 아직 안 고름 · 이 장 저주 제단을 아직 안 고름
        public bool gatePending, needBossReward, altarPending;
        public string bossSlot = "";                    // 1.2.6~ 보스 보상 가운데 칸으로 뽑힌 것 (보상 id · "*legend" · 비면 이어할 때 새로 뽑음)
        // 1.0.4~: 진행 중이던 습격 · 살아 있던 중간 보스(엘리트) 는 다시, 안 연 보물 상자는 옆에 다시 놓음 · 이번 장 최후의 보루 쓴 횟수
        public bool midBossAlive;
        public int ambushLeft, chests, lastStandUsed;
        public List<string> takenKeys = new List<string>();
        public List<float> takenValues = new List<float>();
        public int runKills;
        public bool needEvolution;                      // 보스는 쓰러뜨렸지만 무기 진화를 아직 고르지 않음
        public int chapterBonus;                        // 1.1~ 보스가 쓰러지는 연출 중에 나가서 아직 못 받은 장 클리어 보너스 (이어할 때 줌)
        // 1.2~ 영혼 각인 (SoulBrand): 각인을 쓰는 판인지 · 고른 각인 saveCode 를 쉼표로 (옛 저장은 false · "" → 각인 없는 판 그대로)
        public bool soulBrandOn;
        public string soulBrands = "";
        public List<string> log = new List<string>();
        public List<string> dealtKeys = new List<string>();
        public List<float> dealtValues = new List<float>();
        // 1.1~ 콘텐츠 항목 표 (RunContent): 이벤트 시드(0 = 옛 저장 → 30% · 55% 고정) · 이번 장 계획(saveCode@처치 비율) · 이번 판에 쓴 이벤트 · 다시 할 이벤트(번호+1) · 상자 전설 · 진화 촉매 횟수
        public int eventSeed, eventStage, eventActive, chestLegends, chestCatalysts;
        public List<string> eventPlan = new List<string>();
        public List<string> eventUsed = new List<string>();
        // 1.2~ 맵 물체 · 유물 (MapProps): 장착한 유물(RelicDef.saveCode, 장착 순서) · 맵 물체 시드(0 = 옛 저장 → 새로 뽑음)
        //   이번 장에 부순 물체 수(propStage = 그 장 스테이지 번호 + 1, 0 = 없음) · 석관을 이미 연 장(Chapters 자리 번호 글자, 예 "13")
        public List<string> relics = new List<string>();
        public int propSeed, propStage, propsBroken;
        public string sarcoOpened = "";
        // 1.2~ 영혼의 무게 (SoulWeight, 0 = 무게 없음 · 옛 저장에는 없어 0)
        public int weight;
        // 영혼 트리 · 성장 · 「전설의 힘」(ap) 이 더하는 기준 공격력 · 기준 탄창 (SpecialAbilities.baseAttack · basePistolMag)
        // 옛 저장에는 없어 0 → 기록을 다시 적용하기 전(시작 공격력일 때) 기준을 정함
        public float baseAttack;
        public int baseMag;
        // 1.2~ 영혼 계약 (Contracts): 이번 판 계약 열쇠 · 뒤에 * 이어진 계약 · ! 이룸 (옛 저장은 빈 글 = 계약 없는 판)
        public string contracts = "";
        // 1.2~ 판 안 수 (RunCounters: 상자 · 상점 · 처치 출처 · 영웅 고유 동작 · 본 것) — 이어해도 계약 진행이 남게 (옛 저장은 빈 글 = 0부터)
        public string counters = "";
        // 1.2.3~ 무한 모드 (EndlessMode.SaveRun · RestoreRun 이 적고 되살림 · 이야기 판 저장에는 모두 기본값)
        public float endlessSeconds;                    // GameMode.EndlessSeconds — 경과 시간
        public int endlessBosses;                       // 쓰러뜨린 보스 수
        public int endlessTurn;                         // 다음에 나올 보스 차례 (보스가 살아 있었는데 endlessLiveBosses 가 비었으면 1 뺀 값: 같은 보스가 다시 나오게)
        public bool endlessBossAlive;                   // 평소 · 한계 돌파의 보스가 살아 있었음 → 이어할 때 바로 다시 (체력 가득)
        public float endlessNextBoss = -1f;             // 다음 보스까지 남은 초 (-1 = 예약 없음 · 1.2.3~ 보스가 살아 있으면 겹쳐 나올 때까지)
        public float endlessNextMid = -1f;              // 다음 중간 보스까지 남은 초
        public int endlessFinal;                        // finalStage 0 평소 · 1 모든 보스 · 2 개발자(· 엄마) · 4 한계 돌파 (3 클리어 연출 중은 4 로)
        public int endlessFinalAlive;                   // finalStage 1: 살아 있던 보스 종류 (bossPrefabs 번호 비트)
        public bool endlessDevSummoned, endlessMomSummoned, endlessDevDown, endlessMomDown;
        public int endlessMomKillBase = -1;             // 개발자를 쓰러뜨린 순간의 판 처치 수 (-1 = 아직)
        public int endlessTrophies;                     // 진화를 다 마친 뒤 받은 전리품 수 (효과는 기록 bt<번호> 로 다시 적용)
        public bool endlessStartGiven;                  // 레벨 10 시작 보너스를 받았음 (false 면 이어할 때 줌)
        public int terrainSeed;                         // 지형 변화 시드 (0 = 옛 저장 → 사막으로 새로 시작 · EndlessTerrain.Save · Restore)
        public string terrain = "";                     // 지형 변화 기록 (처치마다 한 항목, ';' 로 구분 · 형식은 EndlessTerrain.Save)
        // 1.2.3~ 보스 타이밍 「보스는 기다리지 않는다」 (EndlessMode.SaveRun · RestoreRun)
        public bool endlessStorm;                       // 15:00 을 보스 처치 0 으로 넘김 (모래 폭풍 국면 · 이 판에선 최후의 시간이 열리지 않음)
        public string endlessLiveBosses = "";           // 살아 있던 보스 (개발자 · 엄마 제외 · 최대 4): "종류,최대 체력,체력,살아 있은 초;…" — 비어 있으면 endlessBossAlive 로 예전처럼 (체력 가득)
        // 1.4.2~ 최후의 시간 왕 두 무리 · 최종 보스 단계 보상 (EndlessMode.SaveRun · RestoreRun · 옛 저장은 기본값 = 넷이 다 나온 것으로 · 받은 보상 없음)
        public int endlessFinalWave;                    // finalStage 1: 1 앞 무리(리치 왕 · 지옥의 군주) 중 · 2 뒤 무리까지 나옴 · 0 옛 저장 · 그 밖의 국면
        public float endlessWave2In = -1f;              // 앞 무리 중: 뒤 무리가 기다리지 않고 깨어나기까지 남은 초 (-1 = 해당 없음)
        public int endlessPhaseRewards;                 // 받은 단계 보상 비트 (개발자 66 · 33% → 0 · 1, 엄마 75 · 50 · 25% → 2 · 3 · 4)
        // 1.2.5~ 이번 판 포인트 (결과 화면 「획득 포인트」 · 「클리어 보너스」 · balance_log points_run 이 이어한 판에서도 판 전체로)
        //   보유 포인트는 그때그때 저장되므로 여기 값은 표시 · 기록용으로만 되살림 (옛 저장은 0 → 예전처럼 이어한 뒤 몫만)
        public int runPoints, runBonus, contractPoints, contractsDone, pointsShown;
        // 2026-10-19~ 빌드 갈래 규칙 판 (1) · 옛 저장 · 구판 규칙 판 (0 → 이어해도 끝까지 예전 뽑기 · 진화 재료, LevelShop.SetPathRules)
        //   입문 · 외길 · 새 카드는 다른 카드처럼 c<25 ~ 31> · f<25 ~ 29> 기록으로 다시 적용 (Version 은 그대로 — 올리면 진행 중인 판이 모두 버려짐)
        public int paths;
        public float pathsState;                        // 1.4.1~ 영웅 갈래가 남기는 값 하나 (HeroPaths.SaveState · 예 지도 제작자 접지술 극의의 두 번째 충전, 옛 저장은 0)
        public bool freeDue;                            // 1.4.1~ 판 시작 무료 카드(= 갈래 입문)를 아직 고르지 않음 → 이어할 때 그 창을 다시 엶
    }

    // 이번 판에서 지금까지 한 선택
    static readonly List<string> log = new List<string>();
    // 이어하기로 불러올 판 (메인 메뉴 → 게임 씬)
    public static Data Pending { get; private set; }
    // 기록을 다시 적용하는 동안 (기록이 두 번 쌓이지 않게 · 연출을 띄우지 않게)
    public static bool Replaying { get; private set; }
    // 보스를 쓰러뜨렸고 무기 진화를 아직 고르지 않음
    public static bool NeedEvolution;
    // 보스를 쓰러뜨렸고 보스 보상 3택을 아직 고르지 않음 (1.0.4~)
    public static bool NeedBossReward;
    // 1.2.6~ 이번 보스 보상 가운데 칸 (ChestRewards.MakeBossSlot 이 정하고, 고르면 비움)
    public static string BossSlotPick;
    // 튜토리얼처럼 저장하지 않는 판 (TutorialRun 이 켬)
    public static bool Disabled;

    public static bool Has => Prefs.HasKey(Key) && Peek() != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name == "MainMenu") EndContinue();
            if (s.name != "GameScene") return;
            log.Clear();
            dirty = false;
            failNoticed = false;
            LastSavedAt = 0f;
            NeedEvolution = NeedBossReward = false;
            BossSlotPick = null;
            Curse.PendingStage = -1;                        // 지난 판에서 남은 값이 섞이지 않게
            ChapterEvents.Resume(0, false, 0);
            new GameObject("RunSaveWatcher").AddComponent<Watcher>();
        };
    }

    // 저장해도 되는 판인지
    // 1.2.7~ 무한 모드 판이 끝난 뒤(게임을 끄며 순위표에 올림 · EndlessQuit)는 끌 때 저장(OnApplicationQuit)이 이어하기를 다시 만들지 않게
    static bool Savable => !Disabled && !DailyChallenge.Active && !Demo.On && !EndlessMode.Ended
                           && !GameInput.Auto && !GameInput.TrailerRunning && !Application.isBatchMode
                           && SceneManager.GetActiveScene().name == "GameScene";

    // 지금 나가면 저장되는 판인지 (ESC 메뉴 안내)
    public static bool CanSaveNow
    {
        get
        {
            StageManager sm = StageManager.Instance;
            // 1.0.4~: 쓰러지는 중(이미 저장을 지움) · 마지막 보스를 쓰러뜨린 뒤(엔딩, 저장 안 함)는 저장되지 않으므로 「저장됨」 안내를 하지 않음
            PlayerController p = Hostile.Player;
            return Savable && sm != null && sm.Started && p != null && !p.IsDying && p.PlayerHealth > 0f && !FinalBossDown;
        }
    }

    // 마지막 장 보스를 쓰러뜨림 (쓰러지는 연출 중 포함) — 판이 끝났으므로 저장하지 않음 (1.0.4~)
    public static bool FinalBossDown
    {
        get
        {
            StageManager sm = StageManager.Instance;
            EnemySpawner sp = Cache<EnemySpawner>.Get;
            if (sm == null || sp == null || GameMode.IsEndless || Chapters.Next(sm.CurrentStage) >= 0) return false;
            if (sp.bossCleared) return true;
            if (!sp.bossSpawned || bosss.LiveCount > 0) return false;
            foreach (bosss b in Object.FindObjectsByType<bosss>(FindObjectsSortMode.None))
                if (b != null && b.IsDead) return true;
            return false;
        }
    }

    // ESC 메뉴 「다시 시작」 (1.0.4~): 저장하는 판을 버리고 새로 시작하면 이어할 판도 지움 (일일 도전 등은 남아 있던 저장을 건드리지 않음)
    public static void Abandon()
    {
        if (Savable) Delete();
    }

    public static void Record(string entry)
    {
        if (Replaying || !Savable) return;
        log.Add(entry);
        MarkDirty();                // 1.1.9~ 선택 직후 곧 저장 (창이 닫혀 시간이 다시 흐른 뒤 0.5초 · 이어 고르기를 한 번에 묶음)
        if (entry.Length > 0 && (entry[0] == 'e' || entry.StartsWith("aw"))) NeedEvolution = false;
    }

    // 1.1.2~ 평타 고점 하향: 보스 보상 「전설의 힘」 · 보물 상자 「힘」은 지금 공격력에 곱하지 않고 기본 공격력의 pct% 를 더함
    // (영혼 트리 「무기 화력」과 같은 기준 · 예전엔 곱이라 판 후반에 고를수록 다른 강화까지 불려 고점을 키웠음). 기록은 ap<pct>
    public static void AddBaseAttack(PlayerController p, float pct, bool record = true)
    {
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        if (sp != null) sp.GrowAttack(pct / 100f);
        else if (p != null) p.damage *= 1f + pct / 100f;
        if (record) Record("ap" + pct.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    // 1.1.9~ 저장에 남는 선택을 했음 (기록이 없는 선택 — 보물 상자 열기 끝 · 상점 닫기 · 유물 장착 — 은 그 자리에서 부름)
    static bool dirty;
    static float dirtySince;
    public static void MarkDirty()
    {
        if (Replaying || !Savable) return;
        if (!dirty) dirtySince = Time.unscaledTime;
        dirty = true;
    }

    // 1.1.9~ 마지막으로 저장한 때 (Time.unscaledTime, 0 = 이번 판에 아직 없음) · 저장할 때마다 (「저장됨」 표시)
    public static float LastSavedAt { get; private set; }
    public static event System.Action Saved;
    static bool failNoticed;

    // 메인 메뉴에서 저장된 판 정보 보기 (이어하기 버튼 글)
    public static Data Peek()
    {
        string json = Prefs.GetString(Key, "");
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            Data d = JsonUtility.FromJson<Data>(json);
            if (d == null || d.version != Version || !CharacterData.IsUnlocked((CharacterId)d.character)) return null;
            return d;
        }
        catch { return null; }
    }

    public static void Delete()
    {
        Prefs.DeleteKey(Key);
        Prefs.Save();
    }

    // 지금 판을 저장 (메인 메뉴로 나갈 때 · 게임을 끌 때 · 다음 장에 들어갈 때)
    public static void Save() => Save(false);

    // background: 1분 자동 저장 — 파일 쓰기만 백그라운드로 (저장 내용은 같음 · 끌 때 · 장 넘어갈 때 · 메뉴로 나갈 때는 바로 씀)
    static void Save(bool background)
    {
        if (!Stage()) return;
        if (background) Prefs.SaveInBackground();
        else Prefs.Save();
        dirty = false;
        // 1.1.9~ 디스크가 꽉 찼거나 권한이 없으면 (예전엔 로그만 남기고 삼킴) — 같은 판에서 한 번만
        //   백그라운드 저장은 쓰기가 끝나야 LastError 가 바뀌므로, 그 실패는 다음 저장 때 알림
        if (Prefs.LastError != null)
        {
            if (!failNoticed) { failNoticed = true; Notice.Toast(NoticeIcon.Warn, Loc.T("저장하지 못했습니다. 저장 폴더를 확인해 주세요."), 5f); }
            return;
        }
        LastSavedAt = Time.unscaledTime;
        Saved?.Invoke();
    }

    // 1.2.5~ 판 저장 JSON 만 지금 값으로 갈아 둠 — 바로 뒤에 부르는 사람이 Prefs.Save() 로 함께 씀
    //   영혼 계약 이룸: 보상 포인트와 「!」(이룸) 표시가 같은 파일 쓰기에 들어가게 (튕긴 뒤 이어해서 보상을 두 번 받지 않게)
    public static void StageForSave()
    {
        if (Replaying) return;
        if (Stage()) dirty = false;
    }

    // 판 상태를 Data 로 만들어 Prefs 에 넣음 (파일 쓰기는 하지 않음) · 저장하지 않는 때면 false
    static bool Stage()
    {
        if (!Savable) return false;
        StageManager sm = StageManager.Instance;
        PlayerController p = Hostile.Player;
        LevelShop shop = Cache<LevelShop>.Get;
        EnemySpawner sp = Cache<EnemySpawner>.Get;
        if (sm == null || p == null || shop == null || sp == null || p.IsDying || p.PlayerHealth <= 0f) return false;
        if (!sm.Started) return false;                              // 시작 이야기 · 첫 카드를 고르기 전은 저장할 게 없음
        if (FinalBossDown) return false;                            // 마지막 보스가 쓰러지는 연출 중에도 저장하지 않음 (1.0.4~, 예전엔 보스전이 다시 저장됐음)

        Data d = new Data
        {
            gameVersion = Application.version,
            character = (int)CharacterData.Selected,
            difficulty = (int)GameMode.Current,
            specialPoints = sm.specialPoints,
            level = p.level,
            hp = p.PlayerHealth,
            nowExp = p.nowEXP,
            needExp = p.needEXP,
            seconds = RunStats.Seconds,
            clock = RunClock.Elapsed,
            chapterLog = RunStats.ChapterLog,
            runKills = RunStats.Kills,
            shards = SoulShards.Amount,
            shardsTotal = SoulShards.Total,
            shardsSpent = SoulShards.Spent,
            log = new List<string>(log),
            needEvolution = NeedEvolution,
            weight = SoulWeight.Level,                  // 1.2~
            runPoints = CharacterData.RunPoints,        // 1.2.5~ 이번 판 포인트 (표시 · 기록용)
            runBonus = Point.RunBonus,
        };
        Point pt = Cache<Point>.Get;
        d.pointsShown = pt != null ? pt.points : 0;
        d.paths = Paths.Active ? 1 : 0;                             // 2026-10-19~ 갈래 규칙 판인지
        d.pathsState = Paths.Active && Paths.Hero != null ? Paths.Safe(() => Paths.Hero.SaveState(), 0f) : 0f;     // 1.4.1~
        shop.SaveLevels(out d.earned, out d.pending);
        d.freeDue = shop.FreePickDue;                               // 1.4.1~ 무료 입문 창이 떠 있거나 ESC 로 숨긴 채 저장 (pending 에는 들어가지 않음)
        Coin coin = Cache<Coin>.Get;
        d.coins = coin != null ? coin.coins : 0;
        SkillGauge g = Cache<SkillGauge>.Get;
        d.gauge = g != null ? g.SkillPoint : 0f;
        SpecialAbilities specials = SpecialAbilities.SharedInstance;
        d.rerolls = specials != null ? specials.Rerolls : 0;
        if (specials != null) specials.GetBase(out d.baseAttack, out d.baseMag);
        foreach (KeyValuePair<string, float> kv in RunStats.DamageDealt) { d.dealtKeys.Add(kv.Key); d.dealtValues.Add(kv.Value); }
        foreach (KeyValuePair<string, float> kv in RunStats.DamageTaken) { d.takenKeys.Add(kv.Key); d.takenValues.Add(kv.Value); }

        // 장 · 처치 수: 보스를 쓰러뜨린 뒤면 다음 장 처음부터, 보스와 싸우는 중이면 보스가 바로 다시 나오게
        // 마지막 보스가 쓰러지는 연출(1초) 중이면 이미 쓰러뜨린 것으로 (1.0.4~, 예전엔 보스가 다시 나왔음)
        int stage = sm.CurrentStage;
        bool bossDown = !GameMode.IsEndless && sp.bossCleared;      // 1.2.3~ 무한 모드는 장이 없으므로 늘 「지금 장 · 처치 수」로
        if (!bossDown && !GameMode.IsEndless && Chapters.Next(stage) >= 0 && sp.bossSpawned && bosss.LiveCount == 0)
            foreach (bosss b in Object.FindObjectsByType<bosss>(FindObjectsSortMode.None))
                if (b != null && b.IsDead) { bossDown = true; break; }
        if (bossDown)
        {
            int next = Chapters.Next(stage);
            if (next < 0) return false;                             // 마지막 보스를 쓰러뜨림 → 엔딩 (저장 안 함)
            d.stage = next;
            d.kills = 0;
            // 1.0.4~: 안 연 보물 상자는 다음 장으로 가져감 (ChapterEvents 가 장을 바꿀 때와 같게)
            ChapterEvents evNext = Cache<ChapterEvents>.Get;
            if (evNext != null) d.chests = evNext.UnopenedChests;
            // 문에 들어갈 때 받는 것(첫 문의 특수 능력 · 포인트 +2 · 저주 제단) 과 보스 보상은 이어할 때 받음
            d.gatePending = true;
            d.needBossReward = sp.bossCleared ? NeedBossReward : true;
            d.bossSlot = sp.bossCleared && NeedBossReward ? BossSlotPick ?? "" : "";
            if (!sp.bossCleared)
            {
                d.needEvolution = sm.EvolutionAvailable;
                // 장 클리어 보너스는 연출이 끝난 뒤(OnBossDefeated)에 주므로, 그 전에 나가면 이어할 때 받게 남김
                if (!GameMode.IsEndless) d.chapterBonus = Point.ChapterBonusPer * Chapters.Number(stage);
            }
        }
        else
        {
            d.stage = stage;
            d.kills = Mathf.Min(sp.killedEnemy, sp.BossKillTarget);
            d.altarPending = Curse.PendingStage == stage;
            d.lastStandUsed = LastStand.Used;
            ChapterEvents ev = Cache<ChapterEvents>.Get;
            if (ev != null) d.chests = ev.UnopenedChests;
            // 습격 · 중간 보스는 보스가 나오기 전에만 (보스전이면 이어할 때 보스가 바로 다시 나옴)
            if (!sp.bossSpawned)
            {
                if (ev != null) d.ambushLeft = ev.AmbushLeft;
                d.midBossAlive = sp.MidBossAlive;                   // 1.2.5~ 적 전부를 찾지 않고 스포너가 들고 있는 중간 보스로
            }
            // 장 시작점이 아니라 나갈 때 서 있던 곳에서 이어함 (보스가 나와 있으면 보스가 문에서 다시 나오므로 시작점에서)
            if (!sp.bossSpawned)
            {
                d.hasPos = true;
                d.px = p.transform.position.x;
                d.py = p.transform.position.y;
            }
        }

        RunContent.Save(d);                                         // 1.1~ 이벤트 계획 · 상자 전설 횟수
        if (GameMode.IsEndless && EndlessMode.Instance != null) EndlessMode.Instance.SaveRun(d);     // 1.2.3~ 무한 전용 상태
        Prefs.SetString(Key, JsonUtility.ToJson(d));
        return true;
    }

    // 메인 메뉴 「이어하기」: 캐릭터 · 난이도를 맞추고 게임 씬으로
    // 1.0.4~: 메뉴에서 고른 난이도 · 캐릭터(저장값)를 덮어쓰지 않게 Override 로 잠시만 (메인 메뉴로 돌아오면 EndContinue)
    //   결과 화면 [R] 다시 시작도 이 판과 같은 캐릭터 · 난이도로
    static bool continuing;

    public static bool Continue()
    {
        Data d = Peek();
        if (d == null) return false;
        Pending = d;
        continuing = true;
        CharacterData.Override = (CharacterId)d.character;
        GameMode.Override = (Difficulty)Mathf.Clamp(d.difficulty, 0, 3);
        SoulWeight.Override = Mathf.Clamp(d.weight, 0, SoulWeight.Max);      // 1.2~ 저장한 판의 무게로 (메뉴에서 고른 무게는 그대로 둠)
        SoulWeight.Invalidate();
        SceneFlow.Go("GameScene");
        return true;
    }

    public static void EndContinue()
    {
        if (!continuing) return;
        continuing = false;
        CharacterData.Override = null;
        GameMode.Override = null;
        SoulWeight.Override = null;
        SoulWeight.Invalidate();
    }

    // 게임 씬에서 StageManager.Opening 이 부름: 시작 이야기 · 첫 카드 대신 저장한 판을 되살림
    public static IEnumerator Restore(StageManager sm)
    {
        Data d = Pending;
        Pending = null;
        if (d == null) yield break;
        LevelShop shop = Object.FindFirstObjectByType<LevelShop>();
        PlayerController p = Hostile.Player;
        SpecialAbilities specials = SpecialAbilities.SharedInstance;
        Shop coinShop = Object.FindFirstObjectByType<Shop>();
        if (shop == null || p == null) yield break;

        shop.CancelOpening();                                       // 판을 시작할 때 뜨는 첫 카드 창은 닫음 (기록에 있는 첫 카드로 대신)
        shop.SetPathRules(d.paths == 1);                            // 2026-10-19~ 옛 저장 · 구판 규칙 판은 끝까지 예전 규칙 (기록을 다시 적용하기 전에)
        if (!GameMode.IsEndless) sm.JumpToStage(d.stage);           // 1.2.3~ 무한 모드 맵 · 스테이지는 StageManager.PrepareEndless 가 먼저 맡음
        RunStats.Seconds = d.seconds;                               // 다시 적용하는 진화의 시각이 00:00 으로 찍히지 않게 먼저 (1.0.4~)
        // 기준 공격력을 기록 재생 전에 고정: 실제 판은 시작 직후 첫 프레임(영혼 트리 버튼)에 시작 공격력으로 정해지는데,
        // 재생 중 처음 GrowAttack 에서 정하면 앞서 재생한 상점 공격력(b1) 만큼 부푼 값이 기준이 되어 ap · 트리 · 성장이 더 크게 들어갔음
        if (specials != null) specials.RestoreBase(d.baseAttack, d.baseMag);

        Replaying = true;
        try
        {
            foreach (string e in d.log)
            {
                if (string.IsNullOrEmpty(e)) continue;
                // 1.1~ 두 글자 기록: aw<번호>@<초> 3장 각성 · tr<번호> 초월 카드 (한 글자 'a' · 't' 보다 먼저)
                // 다음 글자가 숫자일 때만 (나중에 'w' · 'r' 로 시작하는 트리 칸 key 가 생겨도 "tr.xxx" 같은 트리 기록을 먹지 않게)
                bool twoDigit = e.Length > 2 && char.IsDigit(e[2]);
                if (twoDigit && e.StartsWith("aw"))
                {
                    string[] aw = e.Substring(2).Split('@');
                    if (!int.TryParse(aw[0], out int awId) || specials == null) continue;
                    if (aw.Length > 1 && float.TryParse(aw[1], out float awAt)) RunStats.Seconds = awAt;
                    specials.EvolveWeapon(awId);
                    RunStats.Seconds = d.seconds;
                    continue;
                }
                if (twoDigit && e.StartsWith("ap"))
                {
                    if (float.TryParse(e.Substring(2), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ap)) AddBaseAttack(p, ap, false);
                    continue;
                }
                if (twoDigit && e.StartsWith("tr"))
                {
                    if (int.TryParse(e.Substring(2), out int tr)) shop.ReplayTranscend(tr);
                    continue;
                }
                if (twoDigit && e.StartsWith("bt"))         // 1.2.3~ 무한 모드 보스 전리품 (알림 · 효과음 없이 능력치만, 체력은 아래에서 저장 값으로)
                {
                    if (int.TryParse(e.Substring(2), out int bt) && specials != null) specials.BossTrophy(bt, true);
                    continue;
                }
                string arg = e.Substring(1);
                switch (e[0])
                {
                    case 'c': if (int.TryParse(arg, out int c)) shop.ReplayPick(c, false); break;
                    case 'f': if (int.TryParse(arg, out int f)) shop.ReplayPick(f, true); break;
                    case 'e':      // e<번호>@<초> (1.0.4~ 진화한 시각, 예전 기록은 번호만)
                        {
                            string[] ew = arg.Split('@');
                            if (!int.TryParse(ew[0], out int w) || specials == null) break;
                            if (ew.Length > 1 && float.TryParse(ew[1], out float at)) RunStats.Seconds = at;
                            specials.EvolveWeapon(w);
                            RunStats.Seconds = d.seconds;
                            break;
                        }
                    case 't': if (specials != null) specials.ReplayNode(arg); break;
                    case 's': sm.ReplayPicks(ParseIds(arg)); break;
                    case 'b': if (int.TryParse(arg, out int b) && coinShop != null) coinShop.ReplayBuy(b); break;
                    case 'h': if (float.TryParse(arg, out float h)) p.PlayerMaxHealth += h; break;     // 보물 상자 「생명」 (1.0.1)
                    case 'a': if (float.TryParse(arg, out float a)) p.damage *= 1f + a / 100f; break;     // 보스 보상 「전설의 힘」 (1.0.1)
                    case 'k': if (int.TryParse(arg, out int ks)) Curse.Active = ks == d.stage; break;   // 저주 제단: 저장한 장에서 받았으면 다시 (1.0.1)
                    case 'r': if (specials != null) specials.ReplayRefine(); break;                       // 영혼 정련 (1.0.4~)
                }
            }
        }
        finally { Replaying = false; }
        log.Clear();
        log.AddRange(d.log);

        // 저장할 때의 값 (선택을 다시 적용해서 바뀐 최대 체력 · 탄창 등은 그대로 두고 지금 값만)
        p.level = d.level;
        p.PlayerHealth = Mathf.Clamp(d.hp, 1f, p.PlayerMaxHealth);
        PlayerHP hpBar = Object.FindFirstObjectByType<PlayerHP>();
        if (hpBar != null) hpBar.ResetFeel();      // 체력바 조각을 복원한 체력에 붙임
        shop.RestoreLevels(d.earned, d.pending, d.nowExp, d.needExp);
        Coin coin = Object.FindFirstObjectByType<Coin>();
        if (coin != null) { coin.coins = 0; coin.AddCoin(d.coins); }
        SoulShards.Restore(d.shards, d.shardsTotal, d.shardsSpent);
        SkillGauge g = Object.FindFirstObjectByType<SkillGauge>();
        if (g != null) { g.SkillPoint = 0f; g.AddSkillPoint(d.gauge); }
        if (Paths.Active && Paths.Hero != null) Paths.Safe(() => Paths.Hero.LoadState(d.pathsState));     // 1.4.1~ 기록을 다시 적용한 뒤 (극의 등이 켜진 다음)
        if (specials != null) specials.SetRerolls(d.rerolls);
        sm.specialPoints = d.specialPoints;
        RunStats.Seconds = d.seconds;
        RunStats.Kills = d.runKills;
        RunStats.Level = d.level;
        RunStats.RestoreChapterLog(d.chapterLog);                   // 1.1~
        RunStats.DamageDealt.Clear();
        for (int i = 0; i < d.dealtKeys.Count && i < d.dealtValues.Count; i++) RunStats.DamageDealt[d.dealtKeys[i]] = d.dealtValues[i];
        RunStats.DamageTaken.Clear();
        for (int i = 0; i < d.takenKeys.Count && i < d.takenValues.Count; i++) RunStats.DamageTaken[d.takenKeys[i]] = d.takenValues[i];
        EnemySpawner sp = Object.FindFirstObjectByType<EnemySpawner>();
        // 2장부터 페이즈마다 나오는 중간 보스가 살아 있었으면 다시 나오게 (1장 엘리트는 ChapterEvents 가)
        bool firstChapter = Chapters.SlotOf(d.stage) == 0;
        if (sp != null) sp.RestoreKills(d.kills, d.midBossAlive && !firstChapter, GameMode.IsEndless ? d.endlessSeconds : -1f);     // 1.2.5~ 무한 시계는 아직 안 돎 → 저장한 시간으로
        ChapterEvents.Resume(d.ambushLeft, d.midBossAlive && firstChapter, d.chests);
        RunContent.Restore(d);                                      // 1.1~ 이벤트 계획 · 상자 전설 횟수 (옛 저장은 30% · 55% 고정)
        // 1.2.5~ 이번 판 포인트 표시 · 기록 (보유 포인트는 이미 저장돼 있으므로 더하지 않음) — 못 받은 장 보너스는 그 위에 더함
        CharacterData.RestoreRunPoints(d.runPoints);
        Point.RestoreRun(d.runBonus, d.pointsShown);
        if (d.chapterBonus > 0) Point.Bonus(d.chapterBonus);       // 1.1~ 보스가 쓰러지는 연출 중에 나가서 못 받은 장 클리어 보너스
        if (!d.gatePending) LastStand.Used = d.lastStandUsed;
        if (d.hasPos) p.transform.position = new Vector3(d.px, d.py, p.transform.position.z);
        NeedEvolution = d.needEvolution;
        NeedBossReward = d.needBossReward;
        BossSlotPick = d.needBossReward && !string.IsNullOrEmpty(d.bossSlot) ? d.bossSlot : null;
        sm.RestoredClock = d.clock > 0f ? d.clock : d.seconds;
        yield return null;
        // 보스를 쓰러뜨리고 무기 진화 · 보스 보상을 고르기 전에 나갔으면 지금 고름
        if (d.needEvolution) yield return sm.Evolution(false);
        if (d.needBossReward) yield return sm.ResumeBossReward();
        // 문에 들어가기 전에 나갔으면 문에 들어갈 때 받는 것을 지금 (첫 문 특수 능력 · 포인트 +2 · 저주 제단)
        if (d.gatePending) yield return sm.ResumeGate(d.stage);
        else if (d.altarPending) sm.StartCoroutine(Curse.Altar(d.stage));
        if (d.freeDue) shop.ResumeFreePick();                      // 1.4.1~ 무료 입문을 고르기 전에 나간 판: 그 창을 다시 (CancelOpening 이 끈 것을 되돌림)
    }

    static int[] ParseIds(string s)
    {
        List<int> ids = new List<int>();
        foreach (string part in s.Split(','))
            if (int.TryParse(part, out int id)) ids.Add(id);
        return ids.ToArray();
    }

    // 게임을 끌 때 (창 닫기 · Alt+F4) 저장
    class Watcher : MonoBehaviour
    {
        void OnApplicationQuit() => Save();

        // 1.0.4~: 게임이 튕겨도 많이 잃지 않게 1분마다 조용히 저장 (멈춘 창 · 연출 · 장 넘어가는 중 · 쓰러지는 중은 건너뛰고 다음 기회에)
        const float AutoSaveEvery = 60f;
        const float MinDirtyGap = 12f;
        float nextAutoSave;

        void Start()
        {
            nextAutoSave = Time.unscaledTime + AutoSaveEvery;
            Saved += OnSaved;
        }

        void OnDestroy() => Saved -= OnSaved;

        // 1.1.9~ 「저장됨」: 오른쪽 위 포인트 배지(아래끝 -174)와 영혼 트리 버튼(위끝 -222) 사이 · 0.15초 나타남 → 1.2초 → 0.5초 사라짐
        TMPro.TMP_Text badge;
        float shownAt = -99f;

        void OnSaved()
        {
            if (badge == null)
            {
                Canvas canvas = UIKit.HudCanvas();
                if (canvas == null) return;
                badge = UIKit.Text(canvas.transform, "저장됨", 18f, new Color(0.66f, 0.61f, 0.53f), Vector2.zero, new Vector2(220f, 26f), TMPro.TextAlignmentOptions.Right);
                RectTransform r = badge.rectTransform;
                r.pivot = new Vector2(1f, 0.5f);
                UIKit.Pin(r, new Vector2(1f, 1f), new Vector2(-30f, -198f));
                UiScaler.Isolate(badge.gameObject);         // 저장마다 2초 동안 알파가 바뀌므로 하위 캔버스로 (HUD 루트를 다시 묶지 않게)
                badge.alpha = 0f;
            }
            shownAt = Time.unscaledTime;
        }

        void LateUpdate()
        {
            if (badge == null) return;
            float t = Time.unscaledTime - shownAt;
            float a = t < 0.15f ? t / 0.15f : t < 1.35f ? 1f : 1f - (t - 1.35f) / 0.5f;
            a = Mathf.Clamp01(a) * 0.9f;
            if (badge.alpha != a) badge.alpha = a;
        }

        void Update()
        {
            // 1.1.9~ 선택 직후 저장: 창이 닫혀 시간이 다시 흐른 뒤 0.5초 (1분 자동 저장은 그대로)
            // 1.2.5~ 지난 저장 뒤 최소 12초 — 레벨업이 잦은 후반에 저장(적 · 기록 복사 · JSON)이 몰려 끊기지 않게 여러 선택을 묶음
            if (dirty && Time.timeScale > 0f && !StoryDirector.Playing && Time.unscaledTime >= Mathf.Max(dirtySince + 0.5f, LastSavedAt + MinDirtyGap))
            {
                StageManager dsm = StageManager.Instance;
                if (dsm != null && !dsm.IsMenuOpen && CanSaveNow) { Save(true); nextAutoSave = Time.unscaledTime + AutoSaveEvery; return; }
            }
            if (Time.unscaledTime < nextAutoSave || Time.timeScale == 0f || StoryDirector.Playing) return;
            StageManager sm = StageManager.Instance;
            if (sm == null || sm.IsMenuOpen || !CanSaveNow) { nextAutoSave = Time.unscaledTime + 5f; return; }     // 저장하지 않는 판이면 매 프레임 찾지 않게 잠시 뒤에
            nextAutoSave = Time.unscaledTime + AutoSaveEvery;
            Save(true);             // 최적화: 디스크 쓰기는 백그라운드 (1분 경계의 끊김 방지)
        }
    }
}
