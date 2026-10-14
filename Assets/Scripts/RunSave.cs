using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 판 중간 저장 (2.1.9~): 메인 메뉴로 나가거나 게임을 끄면 지금 판을 저장하고, 메인 메뉴 「이어하기」로 이어서 함
// 판의 상태를 통째로 적지 않고, 판을 바꾼 "선택"을 순서대로 적어 두었다가 이어할 때 그대로 다시 적용함
//   c<번호> 레벨업 카드 · f<번호> 시작 카드(레벨이 오르지 않음) · e<번호> 무기 진화 · t<키> 영혼 트리 칸 · s<번호,…> 특수 능력 · b<번호> 코인 상점
//   1.0.4~: 성장 카드도 c<100 ~ 103> · r 영혼 정련 한 번
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
            NeedEvolution = false;
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
            return Savable && sm != null && sm.Started;
        }
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

        // 장 · 처치 수: 보스를 쓰러뜨린 뒤면 다음 장 처음부터, 보스와 싸우는 중이면 보스가 바로 다시 나오게
        int stage = sm.CurrentStage;
        if (sp.bossCleared)
        {
            int next = Chapters.Next(stage);
            if (next < 0) return;                                   // 마지막 보스를 쓰러뜨림 → 엔딩 (저장 안 함)
            d.stage = next;
            d.kills = 0;
        }
        else
        {
            d.stage = stage;
            d.kills = Mathf.Min(sp.killedEnemy, sp.BossKillTarget);
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
                    case 'e': if (int.TryParse(arg, out int w) && specials != null) specials.EvolveWeapon(w); break;
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
        EnemySpawner sp = Object.FindFirstObjectByType<EnemySpawner>();
        if (sp != null) sp.RestoreKills(d.kills);
        NeedEvolution = d.needEvolution;
        yield return null;
        // 보스를 쓰러뜨리고 무기 진화를 고르기 전에 나갔으면 지금 고름
        if (d.needEvolution) yield return sm.Evolution(false);
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
    }
}
