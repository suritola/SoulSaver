using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 판 중간 저장 (2.1.9~): 메인 메뉴로 나가거나 게임을 끄면 지금 판을 저장하고, 메인 메뉴 「이어하기」로 이어서 함
// 판의 상태를 통째로 적지 않고, 판을 바꾼 "선택"을 순서대로 적어 두었다가 이어할 때 그대로 다시 적용함
//   c<번호> 레벨업 카드 · f<번호> 시작 카드(레벨이 오르지 않음) · e<번호> 무기 진화 · t<키> 영혼 트리 칸 · s<번호,…> 특수 능력 · b<번호> 코인 상점
// 그 밖에 바뀌는 값(체력 · 경험치 · 남은 레벨업 · 코인 · 영혼 조각 · 처치 수 · 게이지 · 판 기록)은 저장할 때의 값을 그대로
// 보통 이야기 판만 저장 (무한 모드 · 일일 도전 · 체험판 · 자동 촬영 · 튜토리얼은 저장하지 않음) · 죽거나 4장을 클리어하거나 새 판을 시작하면 지움
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
        public bool hasPos;                             // 나갈 때 서 있던 곳 (1.0.4~, 같은 장에서 보스가 나오기 전에만)
        public float px, py;
        // 1.0.4~: 끝나지 않은 일 — 보스를 잡고 문에 들어가기 전에 나갔음 · 보스 보상을 아직 안 고름 · 이 장 저주 제단을 아직 안 고름
        public bool gatePending, needBossReward, altarPending;
        // 1.0.4~: 진행 중이던 습격 · 살아 있던 중간 보스(엘리트) 는 다시, 안 연 보물 상자는 옆에 다시 놓음 · 이번 장 최후의 보루 쓴 횟수
        public bool midBossAlive;
        public int ambushLeft, chests, lastStandUsed;
        public List<string> takenKeys = new List<string>();
        public List<float> takenValues = new List<float>();
        public int runKills;
        public bool needEvolution;                      // 보스는 쓰러뜨렸지만 무기 진화를 아직 고르지 않음
        public List<string> log = new List<string>();
        public List<string> dealtKeys = new List<string>();
        public List<float> dealtValues = new List<float>();
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
    // 튜토리얼처럼 저장하지 않는 판 (TutorialRun 이 켬)
    public static bool Disabled;

    public static bool Has => Prefs.HasKey(Key) && Peek() != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name != "GameScene") return;
            log.Clear();
            NeedEvolution = NeedBossReward = false;
            Curse.PendingStage = -1;                        // 지난 판에서 남은 값이 섞이지 않게
            ChapterEvents.Resume(0, false, 0);
            new GameObject("RunSaveWatcher").AddComponent<Watcher>();
        };
    }

    // 저장해도 되는 판인지
    static bool Savable => !Disabled && !GameMode.IsEndless && !DailyChallenge.Active && !Demo.On
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
            EnemySpawner sp = Object.FindFirstObjectByType<EnemySpawner>();
            if (sm == null || sp == null || GameMode.IsEndless || Chapters.Next(sm.CurrentStage) >= 0) return false;
            if (sp.bossCleared) return true;
            if (!sp.bossSpawned || bosss.LiveCount > 0) return false;
            foreach (bosss b in Object.FindObjectsByType<bosss>(FindObjectsSortMode.None))
                if (b != null && b.IsDead) return true;
            return false;
        }
    }

    // ESC 메뉴 「다시 시작」 (1.0.4~): 저장하는 판을 버리고 새로 시작하면 이어할 판도 지움 (무한 모드 · 일일 도전 등은 이야기 판 저장을 건드리지 않음)
    public static void Abandon()
    {
        if (Savable) Delete();
    }

    public static void Record(string entry)
    {
        if (Replaying || !Savable) return;
        log.Add(entry);
        if (entry.Length > 0 && entry[0] == 'e') NeedEvolution = false;
    }

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
    public static void Save()
    {
        if (!Savable) return;
        StageManager sm = StageManager.Instance;
        PlayerController p = Hostile.Player;
        LevelShop shop = Object.FindFirstObjectByType<LevelShop>();
        EnemySpawner sp = Object.FindFirstObjectByType<EnemySpawner>();
        if (sm == null || p == null || shop == null || sp == null || p.IsDying || p.PlayerHealth <= 0f) return;
        if (!sm.Started) return;                                    // 시작 이야기 · 첫 카드를 고르기 전은 저장할 게 없음
        if (FinalBossDown) return;                                  // 마지막 보스가 쓰러지는 연출 중에도 저장하지 않음 (1.0.4~, 예전엔 보스전이 다시 저장됐음)

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
            runKills = RunStats.Kills,
            shards = SoulShards.Amount,
            shardsTotal = SoulShards.Total,
            shardsSpent = SoulShards.Spent,
            log = new List<string>(log),
            needEvolution = NeedEvolution,
        };
        shop.SaveLevels(out d.earned, out d.pending);
        Coin coin = Object.FindFirstObjectByType<Coin>();
        d.coins = coin != null ? coin.coins : 0;
        SkillGauge g = Object.FindFirstObjectByType<SkillGauge>();
        d.gauge = g != null ? g.SkillPoint : 0f;
        SpecialAbilities specials = SpecialAbilities.SharedInstance;
        d.rerolls = specials != null ? specials.Rerolls : 0;
        foreach (KeyValuePair<string, float> kv in RunStats.DamageDealt) { d.dealtKeys.Add(kv.Key); d.dealtValues.Add(kv.Value); }
        foreach (KeyValuePair<string, float> kv in RunStats.DamageTaken) { d.takenKeys.Add(kv.Key); d.takenValues.Add(kv.Value); }

        // 장 · 처치 수: 보스를 쓰러뜨린 뒤면 다음 장 처음부터, 보스와 싸우는 중이면 보스가 바로 다시 나오게
        // 마지막 보스가 쓰러지는 연출(1초) 중이면 이미 쓰러뜨린 것으로 (1.0.4~, 예전엔 보스가 다시 나왔음)
        int stage = sm.CurrentStage;
        bool bossDown = sp.bossCleared;
        if (!bossDown && Chapters.Next(stage) >= 0 && sp.bossSpawned && bosss.LiveCount == 0)
            foreach (bosss b in Object.FindObjectsByType<bosss>(FindObjectsSortMode.None))
                if (b != null && b.IsDead) { bossDown = true; break; }
        if (bossDown)
        {
            int next = Chapters.Next(stage);
            if (next < 0) return;                                   // 마지막 보스를 쓰러뜨림 → 엔딩 (저장 안 함)
            d.stage = next;
            d.kills = 0;
            // 1.0.4~: 안 연 보물 상자는 다음 장으로 가져감 (ChapterEvents 가 장을 바꿀 때와 같게)
            ChapterEvents evNext = Object.FindFirstObjectByType<ChapterEvents>();
            if (evNext != null) d.chests = evNext.UnopenedChests;
            // 문에 들어갈 때 받는 것(첫 문의 특수 능력 · 포인트 +2 · 저주 제단) 과 보스 보상은 이어할 때 받음
            d.gatePending = true;
            d.needBossReward = sp.bossCleared ? NeedBossReward : true;
            if (!sp.bossCleared) d.needEvolution = sm.EvolutionAvailable;
        }
        else
        {
            d.stage = stage;
            d.kills = Mathf.Min(sp.killedEnemy, sp.BossKillTarget);
            d.altarPending = Curse.PendingStage == stage;
            d.lastStandUsed = LastStand.Used;
            ChapterEvents ev = Object.FindFirstObjectByType<ChapterEvents>();
            if (ev != null) d.chests = ev.UnopenedChests;
            // 습격 · 중간 보스는 보스가 나오기 전에만 (보스전이면 이어할 때 보스가 바로 다시 나옴)
            if (!sp.bossSpawned)
            {
                if (ev != null) d.ambushLeft = ev.AmbushLeft;
                foreach (EnermyController e in Object.FindObjectsByType<EnermyController>(FindObjectsSortMode.None))
                    if (e != null && !e.IsDead && e.name.StartsWith("MidBoss_")) { d.midBossAlive = true; break; }
            }
            // 장 시작점이 아니라 나갈 때 서 있던 곳에서 이어함 (보스가 나와 있으면 보스가 문에서 다시 나오므로 시작점에서)
            if (!sp.bossSpawned)
            {
                d.hasPos = true;
                d.px = p.transform.position.x;
                d.py = p.transform.position.y;
            }
        }

        Prefs.SetString(Key, JsonUtility.ToJson(d));
        Prefs.Save();
    }

    // 메인 메뉴 「이어하기」: 캐릭터 · 난이도를 맞추고 게임 씬으로
    public static bool Continue()
    {
        Data d = Peek();
        if (d == null) return false;
        Pending = d;
        CharacterData.Selected = (CharacterId)d.character;
        GameMode.Current = (Difficulty)d.difficulty;
        SceneManager.LoadScene("GameScene");
        return true;
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
        sm.JumpToStage(d.stage);
        RunStats.Seconds = d.seconds;                               // 다시 적용하는 진화의 시각이 00:00 으로 찍히지 않게 먼저 (1.0.4~)

        Replaying = true;
        try
        {
            foreach (string e in d.log)
            {
                if (string.IsNullOrEmpty(e)) continue;
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
                }
            }
        }
        finally { Replaying = false; }
        log.Clear();
        log.AddRange(d.log);

        // 저장할 때의 값 (선택을 다시 적용해서 바뀐 최대 체력 · 탄창 등은 그대로 두고 지금 값만)
        p.level = d.level;
        p.PlayerHealth = Mathf.Clamp(d.hp, 1f, p.PlayerMaxHealth);
        shop.RestoreLevels(d.earned, d.pending, d.nowExp, d.needExp);
        Coin coin = Object.FindFirstObjectByType<Coin>();
        if (coin != null) { coin.coins = 0; coin.AddCoin(d.coins); }
        SoulShards.Restore(d.shards, d.shardsTotal, d.shardsSpent);
        SkillGauge g = Object.FindFirstObjectByType<SkillGauge>();
        if (g != null) { g.SkillPoint = 0f; g.AddSkillPoint(d.gauge); }
        if (specials != null) specials.SetRerolls(d.rerolls);
        sm.specialPoints = d.specialPoints;
        RunStats.Seconds = d.seconds;
        RunStats.Kills = d.runKills;
        RunStats.Level = d.level;
        RunStats.DamageDealt.Clear();
        for (int i = 0; i < d.dealtKeys.Count && i < d.dealtValues.Count; i++) RunStats.DamageDealt[d.dealtKeys[i]] = d.dealtValues[i];
        RunStats.DamageTaken.Clear();
        for (int i = 0; i < d.takenKeys.Count && i < d.takenValues.Count; i++) RunStats.DamageTaken[d.takenKeys[i]] = d.takenValues[i];
        EnemySpawner sp = Object.FindFirstObjectByType<EnemySpawner>();
        // 2장부터 페이즈마다 나오는 중간 보스가 살아 있었으면 다시 나오게 (1장 엘리트는 ChapterEvents 가)
        bool firstChapter = Chapters.SlotOf(d.stage) == 0;
        if (sp != null) sp.RestoreKills(d.kills, d.midBossAlive && !firstChapter);
        ChapterEvents.Resume(d.ambushLeft, d.midBossAlive && firstChapter, d.chests);
        if (!d.gatePending) LastStand.Used = d.lastStandUsed;
        if (d.hasPos) p.transform.position = new Vector3(d.px, d.py, p.transform.position.z);
        NeedEvolution = d.needEvolution;
        NeedBossReward = d.needBossReward;
        sm.RestoredClock = d.clock > 0f ? d.clock : d.seconds;
        yield return null;
        // 보스를 쓰러뜨리고 무기 진화 · 보스 보상을 고르기 전에 나갔으면 지금 고름
        if (d.needEvolution) yield return sm.Evolution(false);
        if (d.needBossReward) yield return sm.ResumeBossReward();
        // 문에 들어가기 전에 나갔으면 문에 들어갈 때 받는 것을 지금 (첫 문 특수 능력 · 포인트 +2 · 저주 제단)
        if (d.gatePending) yield return sm.ResumeGate(d.stage);
        else if (d.altarPending) sm.StartCoroutine(Curse.Altar(d.stage));
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
        float nextAutoSave;

        void Start() => nextAutoSave = Time.unscaledTime + AutoSaveEvery;

        void Update()
        {
            if (Time.unscaledTime < nextAutoSave || Time.timeScale == 0f || StoryDirector.Playing) return;
            StageManager sm = StageManager.Instance;
            if (sm == null || sm.IsMenuOpen || !CanSaveNow) { nextAutoSave = Time.unscaledTime + 5f; return; }     // 저장하지 않는 판이면 매 프레임 찾지 않게 잠시 뒤에
            nextAutoSave = Time.unscaledTime + AutoSaveEvery;
            Save();
        }
    }
}
