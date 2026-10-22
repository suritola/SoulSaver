using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 1.2~ 영혼 계약 (설계 3-1 ①): 판마다 목표 3개 — 쉬움(300P) · 보통(800P) · 영웅 고유(600P)
//   판 시작 직후 화면 아래에 세 장을 잠깐 보여 주고 (멈추지 않음), 이루는 순간 토스트 + 포인트를 그 자리에서 (배율 없음, 「클리어 보너스」와 따로)
//   쓰러져도 이룬 것은 남음 · 못 이룬 계약은 다음 판에 한 번 이어지고 (「이어진 계약」), 또 못 이루면 접고 새로 뽑음 · 벌칙 없음
//   무한 모드: 전용 풀(쉬움 · 보통) + 영웅 · 일일 도전: 전용 1개 · 튜토리얼 · 자동 플레이 · 촬영: 없음
//   체험판: 1장에서 되는 쉬움 · 거너 · 검사 계약만 (보드는 정식판에서)
//   저장: 다음 판의 계약은 Prefs "contract.slot.<칸>" (뒤에 * = 이어진 계약) — 이루는 순간 그 칸은 새로 뽑아 둠
//         (판 중간에 나갔다가 새 판을 시작해도 같은 계약을 두 번 받지 않게) · 판 중간 저장은 RunSave.Data.contracts (옛 저장은 계약 없는 판)
//   글: LocTable.Contract 의 contract.<id>.name · .cond (4개 언어)
public enum ContractKind { Easy, Normal, Hero, EndlessEasy, EndlessNormal, Daily }

public sealed class ContractDef
{
    public string id;                   // 저장에도 쓰는 이름 (바꾸지 말 것) — 글 열쇠 contract.<id>.name
    public string since = "1.2";
    public ContractKind kind;
    public CharacterId hero;            // 영웅 계약만
    public bool demo;                   // 체험판에서도 뽑힘
    public int target = 1;
    public System.Func<int> value;      // 이번 판 진행 (목표 이상이면 이룸)
    public System.Func<bool> available; // 지금 뽑아도 되는지 (없으면 늘)
    public int Reward => kind == ContractKind.Normal || kind == ContractKind.EndlessNormal ? 800 : kind == ContractKind.Hero ? 600 : 300;
    public string Name => Loc.T("contract." + id + ".name");
    public string Cond => Loc.T("contract." + id + ".cond");
    public int Now => value != null ? Mathf.Max(0, value()) : 0;
}

public static class Contracts
{
    // ================================================================= 영웅 이름 (글 열쇠 · 보드 열쇠의 가운데)
    public static readonly string[] HeroKeys = { "gunner", "sword", "rogue", "archer", "alch", "carto", "clock", "post", "ghost" };
    public static string HeroKey(CharacterId id) => (int)id >= 0 && (int)id < HeroKeys.Length ? HeroKeys[(int)id] : "gunner";

    // ================================================================= 판 안 값 (RunCounters · 기존 기록)
    static int C(string key) => RunCounters.Get(key);
    static int Stage => RunStats.Stage;
    static LevelShop shopCache;
    static LevelShop Shop => shopCache != null ? shopCache : (shopCache = Object.FindFirstObjectByType<LevelShop>(FindObjectsInactive.Include));
    static SpecialAbilities Sa => SpecialAbilities.SharedInstance;
    static int EvoTier => Sa != null ? Sa.EvolutionTier : 0;
    static int TreeNodes { get { int n = 0; foreach (int b in RunStats.Branches) n += b; return n; } }
    static int CardPicks
    {
        get
        {
            LevelShop s = Shop;
            if (s == null || s.ability_level == null) return 0;
            int n = 0;
            foreach (int lv in s.ability_level) n += Mathf.Max(0, lv);
            for (int k = 0; k < LevelShop.GrowthCount; k++) n += s.GrowthLevel(k);
            return n + s.TranscendPicks;
        }
    }
    static float EndlessT => GameMode.EndlessSeconds;
    static EndlessMode Em => EndlessMode.Instance;

    static ContractDef E(string id, bool demo, int target, System.Func<int> v, System.Func<bool> ok = null) =>
        new ContractDef { id = "e." + id, kind = ContractKind.Easy, demo = demo, target = target, value = v, available = ok };
    static ContractDef N(string id, int target, System.Func<int> v, System.Func<bool> ok = null) =>
        new ContractDef { id = "n." + id, kind = ContractKind.Normal, target = target, value = v, available = ok };
    static ContractDef H(CharacterId who, int n, bool demo, int target, System.Func<int> v) =>
        new ContractDef { id = "h." + HeroKey(who) + "." + n, kind = ContractKind.Hero, hero = who, demo = demo, target = target, value = v };
    static ContractDef X(string id, bool normal, int target, System.Func<int> v) =>
        new ContractDef { id = "x." + id, kind = normal ? ContractKind.EndlessNormal : ContractKind.EndlessEasy, target = target, value = v };

    static bool Story => !GameMode.IsEndless && !DailyChallenge.Active;
    static bool FullGame => !Demo.On;

    // 글 문서 4-1 ~ 4-4 (조건을 코드로 잴 수 있는 것만 · 영웅 계약은 영웅마다 3~5종)
    public static readonly List<ContractDef> All = new List<ContractDef>
    {
        // ---------------- 쉬움 300P
        E("chest2", true, 2, () => C("chest")),
        E("shop2", true, 2, () => C("shop")),
        E("props10", true, 10, () => C("prop")),
        E("swarm3", true, 3, () => C("swarm")),
        E("lv25", false, 25, () => RunStats.Level),
        E("ch2", false, 2, () => Stage),
        E("kills250", true, 250, () => RunStats.Kills),
        E("curse", false, 1, () => C("curse"), () => FullGame),
        E("meat5", true, 5, () => C("meat")),
        E("coins300", true, 300, () => C("coins")),
        E("shards500", true, 500, () => SoulShards.Total),
        E("tree10", true, 10, () => TreeNodes),
        E("cards15", true, 15, () => CardPicks),
        E("magnet", true, 1, () => C("magnet")),
        E("ult5", true, 5, () => C("ult")),
        E("midboss2", true, 2, () => C("midboss")),
        E("boss1", true, 1, () => C("boss")),
        E("events2", false, 2, () => C("event.join"), () => FullGame),
        E("goblin", false, 1, () => C("event.goblin"), () => FullGame),
        E("altar", false, 1, () => C("event.altar"), () => FullGame),
        E("raid", false, 1, () => C("event.raid"), () => FullGame),
        E("potion", true, 1, () => C("potion")),
        E("reroll", true, 1, () => C("reroll"), () => !SoulWeight.NoReroll),
        E("evo1", false, 1, () => EvoTier >= 1 ? 1 : 0),
        E("weak30", true, 30, () => C("weak.hit")),          // 빈틈 30번 (WeakHit · 체험판에서도)
        // ---------------- 보통 800P
        N("bossquick", 1, () => C("boss.quick")),
        N("midboss20", 1, () => C("midboss.fast")),
        N("skillevo", 1, () => Shop != null ? Shop.EvoLog.Count : 0),
        N("relic", 1, () => Sa != null ? Sa.Relics.Count : 0),
        N("legend", 1, () => C("chest.legend")),
        N("brands2", 2, () => SoulBrand.Current.Count, () => SoulBrand.FeatureUnlocked),
        N("ch3", 3, () => Stage),
        N("tree30", 30, () => TreeNodes),
        N("ult20", 20, () => C("ult")),
        N("lv40", 40, () => RunStats.Level),
        N("kills600", 600, () => RunStats.Kills),
        N("chest5", 5, () => C("chest")),
        N("elitechest", 1, () => C("chest.elite")),
        N("cursedchest", 1, () => C("event.cursed")),
        N("pack", 1, () => C("event.pack")),
        N("refine", 1, () => RunStats.Refines),
        N("awaken", 1, () => EvoTier >= 3 ? 1 : 0),
        N("evo2", 1, () => EvoTier >= 2 ? 1 : 0),
        N("shop4", 4, () => C("shop")),
        N("coins1000", 1000, () => C("coins")),
        N("sarco2", 2, () => C("sarco")),
        N("ch4", 4, () => Stage),
        N("transcend", 1, () => Shop != null ? Shop.TranscendPicks : 0),
        N("midboss5", 5, () => C("midboss")),
        // ---------------- 영웅 600P (영웅의 손맛을 「더 쓰게」 하는 것만)
        H(CharacterId.Gunner, 1, true, 10, () => C("volley.boss")),
        H(CharacterId.Gunner, 2, true, 10, () => C("ult")),
        H(CharacterId.Gunner, 4, true, 6, () => C("volley.marks")),
        H(CharacterId.Gunner, 5, false, 200, () => C("kill." + DamageSource.Special)),
        H(CharacterId.Gunner, 6, true, 400, () => C("kill." + DamageSource.Basic)),
        H(CharacterId.Swordsman, 1, true, 60, () => C("kill." + DamageSource.Ult)),
        H(CharacterId.Swordsman, 5, false, 3, () => C("midboss.basic")),
        H(CharacterId.Swordsman, 6, true, 15, () => C("ult")),
        H(CharacterId.Rogue, 2, false, 15, () => C("ult")),
        H(CharacterId.Rogue, 4, false, 8, () => C("dash.hits")),
        H(CharacterId.Rogue, 6, false, 500, () => C("kill." + DamageSource.Basic)),
        H(CharacterId.Archer, 1, false, 30, () => C("perfect")),
        H(CharacterId.Archer, 2, false, 10, () => C("ult")),
        H(CharacterId.Archer, 3, false, 25, () => C("ult.wave")),
        H(CharacterId.Archer, 6, false, 5, () => C("perfect.streak")),
        H(CharacterId.Alchemist, 1, false, 10, () => C("unstable")),
        H(CharacterId.Alchemist, 2, false, 10, () => C("ult")),
        H(CharacterId.Alchemist, 4, false, 5, () => C("alch.fullpour")),
        H(CharacterId.Cartographer, 2, false, 60, () => C("shape")),
        H(CharacterId.Cartographer, 3, false, 10, () => C("ult")),
        H(CharacterId.Cartographer, 4, false, 10, () => C("fold.pull")),
        H(CharacterId.Clockmaker, 1, false, 5, () => C("ult")),
        H(CharacterId.Clockmaker, 2, false, 20, () => C("oclock")),
        H(CharacterId.Clockmaker, 6, false, 600, () => C("gear")),       // 1.3.5: 800 → 600 (판이 짧아짐)
        H(CharacterId.Postman, 1, false, 100, () => C("letter")),
        H(CharacterId.Postman, 2, false, 10, () => C("ult")),
        H(CharacterId.Postman, 3, false, 8, () => C("express.marks")),
        H(CharacterId.Postman, 4, false, 8, () => C("post.bonus")),
        H(CharacterId.Postman, 6, false, 300, () => C("letter")),
        H(CharacterId.Ghost, 1, true, 4, () => C("ult")),          // 체험판 영웅 (2026-10-18~)
        H(CharacterId.Ghost, 2, false, 50, () => C("freeze")),
        H(CharacterId.Ghost, 4, false, 10, () => C("ult.wave")),
        // ---------------- 무한 모드 전용
        X("t5", false, 300, () => Mathf.FloorToInt(EndlessT)),
        X("boss2", false, 2, () => Em != null ? Em.BossesDefeated : 0),
        X("t10", true, 600, () => Mathf.FloorToInt(EndlessT)),
        X("boss5", true, 5, () => Em != null ? Em.BossesDefeated : 0),
        X("final", true, 1, () => Em != null && Em.FinalPhase ? 1 : 0),
        X("beyond", true, 1, () => Em != null && Em.Beyond ? 1 : 0),
        // ---------------- 일일 도전 전용 (일일 보상과 겹치지 않게 하나만)
        new ContractDef { id = "d.midboss", kind = ContractKind.Daily, target = 1, value = () => C("midboss") },
    };

    static readonly Dictionary<string, ContractDef> byId = new Dictionary<string, ContractDef>();
    public static ContractDef ById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (byId.Count == 0) foreach (ContractDef d in All) byId[d.id] = d;
        return byId.TryGetValue(id, out ContractDef c) ? c : null;
    }

    // ================================================================= 이번 판
    public sealed class Active
    {
        public ContractDef def;
        public bool done, carried;
        public int best;                // 지금까지의 진행 (값이 줄어드는 일은 없지만 결과 화면에 그대로 보여 주려고)
    }

    static readonly List<Active> run = new List<Active>();
    public static IReadOnlyList<Active> Run => run;
    public static bool RunOn { get; private set; }
    public static int DoneThisRun { get; private set; }
    public static int PointsThisRun { get; private set; }
    static int codexAtStart, memoryAtStart;
    static bool runStory;               // 이어하기로 저장하는 판이었는지 (이야기 · 1.2.3~ 무한 모드 — 메인 메뉴로 나갈 때 이어하기 판을 남겼는지 볼 때 · 일일 도전은 메뉴에서 먼저 풀림)
    public static int CodexGained { get; private set; }
    public static int MemoryGained { get; private set; }

    // 결과 화면 (판이 끝난 뒤에도 남음 · 새 판에서 지움)
    public static readonly List<Active> LastRun = new List<Active>();
    public static bool LastRunOn { get; private set; }

    static bool Off => TutorialRun.Active || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode;

    // 다음 판 칸 이름 (영웅 칸은 영웅마다)
    static string SlotOf(ContractKind k, CharacterId who) => k switch
    {
        ContractKind.Easy => "e",
        ContractKind.Normal => "n",
        ContractKind.Hero => "h" + (int)who,
        ContractKind.EndlessEasy => "xe",
        ContractKind.EndlessNormal => "xn",
        _ => "d",
    };
    const string SlotKey = "contract.slot.";

    // 지금 메뉴 · 판의 종류에 맞는 칸 셋 (일일 도전은 하나)
    static List<ContractKind> KindsFor(bool endless, bool daily)
    {
        if (daily) return new List<ContractKind> { ContractKind.Daily };
        if (endless) return new List<ContractKind> { ContractKind.EndlessEasy, ContractKind.EndlessNormal, ContractKind.Hero };
        if (Demo.On) return new List<ContractKind> { ContractKind.Easy, ContractKind.Hero };
        return new List<ContractKind> { ContractKind.Easy, ContractKind.Normal, ContractKind.Hero };
    }

    static bool Eligible(ContractDef d, ContractKind k, CharacterId who, ICollection<string> exclude)
    {
        if (d.kind != k || exclude.Contains(d.id)) return false;
        if (k == ContractKind.Hero && d.hero != who) return false;
        if (Demo.On && !d.demo) return false;
        return d.available == null || d.available();
    }

    static ContractDef Draw(ContractKind k, CharacterId who, ICollection<string> exclude)
    {
        List<ContractDef> pool = new List<ContractDef>();
        foreach (ContractDef d in All) if (Eligible(d, k, who, exclude)) pool.Add(d);
        if (pool.Count == 0) foreach (ContractDef d in All) if (d.kind == k && (k != ContractKind.Hero || d.hero == who) && (!Demo.On || d.demo)) pool.Add(d);
        return pool.Count == 0 ? null : pool[Random.Range(0, pool.Count)];
    }

    // 칸의 계약 (없으면 새로 뽑아 둠) · carried = 이어진 계약
    static ContractDef Slot(ContractKind k, CharacterId who, out bool carried, ICollection<string> exclude)
    {
        string raw = Prefs.GetString(SlotKey + SlotOf(k, who), "");
        carried = raw.EndsWith("*");
        ContractDef d = ById(carried ? raw.Substring(0, raw.Length - 1) : raw);
        if (d != null && d.kind == k && (k != ContractKind.Hero || d.hero == who) && (!Demo.On || d.demo)) return d;
        d = Draw(k, who, exclude);
        carried = false;
        Prefs.SetString(SlotKey + SlotOf(k, who), d != null ? d.id : "");
        return d;
    }

    static void SetSlot(ContractKind k, CharacterId who, ContractDef d, bool carried) =>
        Prefs.SetString(SlotKey + SlotOf(k, who), d == null ? "" : d.id + (carried ? "*" : ""));

    // 메인 메뉴 · 판 시작: 지금 칸들의 계약 (뽑기만 하고 저장)
    public static List<Active> Preview(bool endless, bool daily, CharacterId who)
    {
        List<Active> list = new List<Active>();
        HashSet<string> taken = new HashSet<string>();
        foreach (ContractKind k in KindsFor(endless, daily))
        {
            ContractDef d = Slot(k, who, out bool carried, taken);
            if (d == null) continue;
            taken.Add(d.id);
            list.Add(new Active { def = d, carried = carried });
        }
        return list;
    }

    // StageManager.Opening: 시작 이야기 뒤, 영혼 각인 창 앞 (이어하기는 RestoreRun)
    public static void BeginRun()
    {
        run.Clear();
        LastRun.Clear();
        LastRunOn = false;
        DoneThisRun = PointsThisRun = 0;
        RunOn = !Off;
        runStory = !DailyChallenge.Active;     // 1.2.3~ 무한 모드도 이어하기 저장 (RunSave) → 나갔다 이어하면 계약이 그대로 이어지게
        if (!RunOn) return;
        runStartedAt = Time.realtimeSinceStartup;
        run.AddRange(Preview(GameMode.IsEndless, DailyChallenge.Active, CharacterData.Selected));
        Prefs.Save();
        Snapshot();
        Strip.Show(run);
    }

    static void Snapshot()
    {
        CodexSeen.Count(out codexAtStart, out int _);
        memoryAtStart = MemoryShards.Count;
    }

    // RunCountersHost 가 0.5초마다
    public static void Tick()
    {
        if (!RunOn) return;
        for (int i = 0; i < run.Count; i++)
        {
            Active a = run[i];
            if (a.def == null || a.done) continue;
            int now = a.def.Now;
            if (now > a.best) a.best = now;
            if (now < a.def.target) continue;
            Fulfil(a);
        }
    }

    static void Fulfil(Active a)
    {
        a.done = true;
        a.best = Mathf.Max(a.best, a.def.target);
        DoneThisRun++;
        RunLog.ContractsDone++;             // 1.2~ 판 기록 contracts_done (RunLog)
        PointsThisRun += a.def.Reward;
        Point.Bonus(a.def.Reward, false);
        RunCounters.AddLife("contracts");
        RunCounters.AddLife(RunCounters.Hero("contracts"));
        // 이 칸은 다음 판을 위해 바로 새로 뽑아 둠 (지금 판의 다른 계약과 겹치지 않게)
        HashSet<string> taken = new HashSet<string>();
        foreach (Active o in run) if (o.def != null) taken.Add(o.def.id);
        SetSlot(a.def.kind, CharacterData.Selected, Draw(a.def.kind, CharacterData.Selected, taken), false);
        RunSave.StageForSave();             // 1.2.5~ 판 저장의 「!」(이룸)를 보상 포인트와 같은 파일 쓰기에 (튕긴 뒤 이어해서 두 번 받지 않게)
        Prefs.Save();
        Notice.Toast(NoticeIcon.Star, Loc.T("contract.done.toast").Replace("{0}", a.def.Name).Replace("{1}", a.def.Reward.ToString("N0")), 4f, new Color(0.96f, 0.83f, 0.47f));
        Hostile.Play("chime", 0.7f, 1.35f);
        Hud.Dirty = true;
    }

    // 판이 끝남 (쓰러짐 · 클리어 · 포기 · 다시 시작 · 메인 메뉴): 못 이룬 계약을 다음 판으로 이음 / 접음
    //   saved = 이어하기로 남긴 판을 두고 나감 → 칸을 바꾸지 않음 (이어하면 RunSave 가 되살림)
    //   짧은 판(ShortRunSec 미만 · 「다시 시작」 연타)도 칸을 바꾸지 않음 → 다시 시작 두 번으로 계약을 접고 새로 뽑는 길을 막음 (다시 뽑기는 하루 3번)
    const float ShortRunSec = 60f;
    static float runStartedAt;
    static bool lastShort;
    static void EndRun(bool saved)
    {
        if (!RunOn) return;
        RunOn = false;
        Tick();
        lastShort = Time.realtimeSinceStartup - runStartedAt < ShortRunSec;
        if (lastShort) saved = true;
        CodexSeen.Count(out int codexNow, out int _);
        CodexGained = Mathf.Max(0, codexNow - codexAtStart);
        MemoryGained = Mathf.Max(0, MemoryShards.Count - memoryAtStart);
        LastRun.Clear();
        LastRunOn = true;
        foreach (Active a in run)
        {
            LastRun.Add(new Active { def = a.def, done = a.done, carried = a.carried, best = a.best });
            if (saved || a.done || a.def == null) continue;
            if (a.carried) SetSlot(a.def.kind, CharacterData.Selected, Draw(a.def.kind, CharacterData.Selected, new[] { a.def.id }), false);     // 두 번째도 못 이룸 → 접고 새로
            else SetSlot(a.def.kind, CharacterData.Selected, a.def, true);
        }
        run.Clear();
        Prefs.Save();
    }

    // 이어하기 판을 남기고 메인 메뉴로 (ESC 메뉴 · 창 닫기) → 칸 그대로 · 결과 화면 · 메인 메뉴가 먼저 그려져도 같은 결과가 되게 (씬 불러오기 순서와 무관)
    static void EndFor(string scene) => EndRun(scene == "MainMenu" && runStory && RunSave.Has);
    public static void EnsureEnded() { if (RunOn && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GameScene") EndFor(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); }

    // 결과 화면 줄: 이 계약은 다음 판에 이어지는지 (이미 이어진 것이었으면 접힘)
    public static bool WillCarry(Active a) => !a.done && (!a.carried || lastShort);       // 짧은 판은 이어진 계약도 접지 않음

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            shopCache = null;
            if (s.name == "GameScene")
            {
                EndRun(false);          // ESC 「다시 시작」처럼 게임 씬에서 바로 새 판이면 지난 판을 여기서 끝냄
                run.Clear(); RunOn = false; LastRun.Clear(); LastRunOn = false;
                new GameObject("ContractHud").AddComponent<Hud>();
                return;
            }
            // 이어하기 판을 남기고 메인 메뉴로 (ESC 메뉴 · 창 닫기) → 칸 그대로
            EndFor(s.name);
        };
    }

    // ================================================================= 이어하기 (RunContent)
    public static void SaveRun(RunSave.Data d)
    {
        d.counters = RunCounters.Serialize();        // 판 안 수 (이어해도 상자 · 상점 · 영웅 계약 진행이 남게)
        d.contractPoints = PointsThisRun;            // 1.2.5~ 결과 화면 계약 포인트 · 이룬 수 (이어해도 판 전체로)
        d.contractsDone = DoneThisRun;
        if (!RunOn) { d.contracts = ""; return; }
        List<string> parts = new List<string>();
        foreach (Active a in run) if (a.def != null) parts.Add(a.def.id + (a.carried ? "*" : "") + (a.done ? "!" : ""));
        d.contracts = string.Join(",", parts);
    }

    public static void RestoreRun(RunSave.Data d)
    {
        run.Clear();
        RunCounters.Restore(d.counters);        // 옛 저장(빈 글)은 0부터
        RunOn = !Off && !string.IsNullOrEmpty(d.contracts);
        if (!RunOn) return;
        runStory = true;                // 이어하기는 저장하는 판뿐 (이야기 · 무한 모드)
        runStartedAt = Time.realtimeSinceStartup - ShortRunSec;     // 이어한 판은 짧은 판으로 치지 않음
        DoneThisRun = Mathf.Max(0, d.contractsDone);         // 1.2.5~ 옛 저장은 0
        PointsThisRun = Mathf.Max(0, d.contractPoints);
        RunLog.ContractsDone = Mathf.Max(RunLog.ContractsDone, DoneThisRun);
        foreach (string raw in d.contracts.Split(','))
        {
            string s = raw.Trim();
            bool done = s.EndsWith("!");
            if (done) s = s.Substring(0, s.Length - 1);
            bool carried = s.EndsWith("*");
            if (carried) s = s.Substring(0, s.Length - 1);
            ContractDef def = ById(s);
            if (def != null) run.Add(new Active { def = def, done = done, carried = carried, best = done ? def.target : def.Now });
        }
        Snapshot();
        Hud.Dirty = true;
    }

    // ================================================================= 다시 뽑기 (메인 메뉴 · 하루 3번, UTC 날짜)
    public const int RerollsPerDay = 3;
    const string RerollDayKey = "contract.reroll.day", RerollUsedKey = "contract.reroll.used";
    public static int RerollsLeft => Prefs.GetString(RerollDayKey, "") == DailyChallenge.Key(DailyChallenge.Today) ? Mathf.Max(0, RerollsPerDay - Prefs.GetInt(RerollUsedKey, 0)) : RerollsPerDay;

    public static bool Reroll(bool endless, CharacterId who)
    {
        if (RerollsLeft <= 0) return false;
        string today = DailyChallenge.Key(DailyChallenge.Today);
        if (Prefs.GetString(RerollDayKey, "") != today) { Prefs.SetString(RerollDayKey, today); Prefs.SetInt(RerollUsedKey, 0); }
        Prefs.SetInt(RerollUsedKey, Prefs.GetInt(RerollUsedKey, 0) + 1);
        List<Active> now = Preview(endless, false, who);
        HashSet<string> exclude = new HashSet<string>();
        foreach (Active a in now) exclude.Add(a.def.id);
        HashSet<string> taken = new HashSet<string>(exclude);
        foreach (ContractKind k in KindsFor(endless, false))
        {
            ContractDef d = Draw(k, who, taken);
            if (d == null) continue;
            taken.Add(d.id);
            SetSlot(k, who, d, false);
        }
        Prefs.Save();
        return true;
    }

    // 결과 화면 · 메뉴 한 줄 꼬리표 (쉬움 · 300 P)
    public static string KindTag(ContractDef d) => Loc.T(d.kind == ContractKind.Normal || d.kind == ContractKind.EndlessNormal ? "contract.kind.normal"
                                                       : d.kind == ContractKind.Hero ? "contract.kind.hero" : "contract.kind.easy");

    public static string Progress(ContractDef d, int now) => Mathf.Min(now, d.target).ToString("N0") + " / " + d.target.ToString("N0");

    // ================================================================= 판 시작: 화면 아래에 세 장 (멈추지 않음 · 5초 뒤 사라짐)
    class Strip : MonoBehaviour
    {
        float age;
        CanvasGroup group;
        const float Life = 5.5f;

        public static void Show(List<Active> list)
        {
            if (list.Count == 0) return;
            GameObject go = new GameObject("ContractStrip", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Canvas cv = go.GetComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 30500;                // 영혼 각인 창 위 · 이야기 연출(31000) 아래
            CanvasScaler cs = go.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            UiScaler.Fit(cs);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            UIKit.EnsureStyle();
            Strip s = go.AddComponent<Strip>();
            s.group = go.AddComponent<CanvasGroup>();
            s.group.blocksRaycasts = false;
            s.group.interactable = false;
            s.group.alpha = 0f;                     // 고르기 창이 닫힐 때까지 숨김 (Update)

            // 2026-10-22~ 화면 아래 가운데 띠(다른 UI 와 겹쳐 시야를 가림 — 사용자) → 오른쪽 가운데에 작게 세로로 (고르기 창 카드 · HUD 와 겹치지 않게)
            const float W = 380f, H = 96f, Gap = 10f, X = 960f - 30f - W * 0.5f;
            float top = (list.Count - 1) * (H + Gap) * 0.5f + 40f;
            TMP_Text head = UIKit.Text(go.transform, "contract.run.title", 22f, new Color(0.79f, 0.63f, 1f), new Vector2(X, top + H * 0.5f + 20f), new Vector2(W, 30f));
            head.raycastTarget = false;
            for (int i = 0; i < list.Count; i++)
            {
                Active a = list[i];
                RectTransform card = UIKit.Rect("Contract", go.transform, new Vector2(X, top - i * (H + Gap)), new Vector2(W, H));
                Image bg = card.gameObject.AddComponent<Image>();
                bg.sprite = UIKit.ButtonSprite;
                bg.type = Image.Type.Sliced;
                // 1.4.8~ 새 도트 판(금장 돌판)이면 흰색 그대로 (예전엔 판을 거의 검게 칠해 금 테두리까지 묻혔음)
                bg.color = RecommendGlow.PillArt ? new Color(1f, 1f, 1f, 0.94f) : new Color(0.1f, 0.08f, 0.14f, 0.94f);
                bg.raycastTarget = false;
                TMP_Text tag = UIKit.Text(card, "", 20f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 32f), new Vector2(W - 24f, 24f), TextAlignmentOptions.Left);
                tag.text = KindTag(a.def) + (a.carried ? "  ·  <color=#C9A2FF>" + Loc.T("contract.carried.tag") + "</color>" : "");
                TMP_Text name = UIKit.Text(card, "", 22f, new Color(0.95f, 0.92f, 0.86f), new Vector2(0f, 4f), new Vector2(W - 24f, 30f), TextAlignmentOptions.Left);
                name.text = a.def.Name;
                TMP_Text cond = UIKit.Text(card, "", 20f, new Color(0.72f, 0.68f, 0.62f), new Vector2(0f, -30f), new Vector2(W - 24f, 30f), TextAlignmentOptions.Left);
                cond.text = a.def.Cond;
                foreach (TMP_Text t in new[] { tag, name, cond }) { t.enableWordWrapping = false; t.raycastTarget = false; UIKit.Forget(t); }
            }
        }

        // 2026-10-22~ 판 시작 순서상 바로 뒤에 영혼 각인 3택 1 · 레벨업 입문 창이 떠서 어디에 두든 겹쳤음 (사용자 「아직도 다른 UI 랑 겹친다」)
        //   → 고르기 창 · 일시정지 · 연출이 모두 닫히고 0.6초 뒤부터 보임 · 보이는 중에 창이 열리면 잠깐 숨고 기다림 · 실제 시간 90초를 넘기면 그냥 사라짐
        float clearFor, waited;
        LevelShop levelShop;
        bool Busy
        {
            get
            {
                if (levelShop == null) levelShop = FindFirstObjectByType<LevelShop>();
                return Time.timeScale == 0f || ChoiceUI.Open || SoulTreeUI.IsOpen || ESCmenu.IsOpen || StoryDirector.Playing || Herald.Playing
                    || (levelShop != null && levelShop.IsOpen);
            }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            waited += dt;
            if (Busy) { clearFor = 0f; group.alpha = Mathf.MoveTowards(group.alpha, 0f, dt * 6f); if (age <= 0f && waited > 90f) Destroy(gameObject); return; }
            clearFor += dt;
            if (clearFor < 0.6f) return;
            age += dt;
            group.alpha = age < 0.3f ? age / 0.3f : 1f - Mathf.Clamp01((age - (Life - 0.6f)) / 0.6f);
            if (age >= Life) Destroy(gameObject);
        }
    }

    // ================================================================= ESC 메뉴: 왼쪽 아래에 이번 판 계약 (영혼 각인 목록 아래)
    class Hud : MonoBehaviour
    {
        public static bool Dirty;
        GameObject panel;

        void Update()
        {
            bool want = ESCmenu.IsOpen && run.Count > 0;
            if (want && (panel == null || Dirty)) Build();
            else if (!want && panel != null) { Destroy(panel); panel = null; }
        }

        void OnDestroy() { if (panel != null) Destroy(panel); }

        void Build()
        {
            Dirty = false;
            if (panel != null) Destroy(panel);
            Canvas canvas = UIKit.HudCanvas();
            if (canvas == null) return;
            UIKit.EnsureStyle();
            Tick();
            const float RowH = 66f, W = 440f;
            float h = 56f + RowH * run.Count;
            panel = new GameObject("ContractPanel", typeof(RectTransform), typeof(Image));
            RectTransform r = panel.GetComponent<RectTransform>();
            r.SetParent(canvas.transform, false);
            r.SetAsLastSibling();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 0f);
            r.anchoredPosition = new Vector2(30f, 24f);         // 각인 목록(가운데 -20 · 최대 높이 464 → 아래끝 -252) 밑
            r.sizeDelta = new Vector2(W, h);
            Image bg = panel.GetComponent<Image>();
            // 1.4.8~ 새 도트 스타일: 단색 판 대신 창 판(ui_panel: 금 이중 테두리 · 모서리 보석) · 못 찾으면 예전 단색
            bool framed = UIKit.PanelSprite != null;
            if (framed) { bg.sprite = UIKit.PanelSprite; bg.type = Image.Type.Sliced; }
            bg.color = framed ? new Color(1f, 1f, 1f, 0.94f) : new Color(0.06f, 0.05f, 0.08f, 0.88f);
            bg.raycastTarget = true;        // 일시정지 HUD 툴팁(PauseInspect)이 패널 밑 HUD 칸 · 적을 보지 않게 (RunStats 요약 판과 같게)
            UIKit.Text(r, "contract.run.title", 24f, new Color(0.79f, 0.63f, 1f), new Vector2(0f, h / 2f - 26f), new Vector2(W - 30f, 34f));
            for (int i = 0; i < run.Count; i++)
            {
                Active a = run[i];
                float y = h / 2f - 56f - RowH * (i + 0.5f);
                TMP_Text name = UIKit.Text(r, "", 20f, a.done ? new Color(0.96f, 0.83f, 0.47f) : new Color(0.93f, 0.9f, 0.84f), new Vector2(-40f, y + 13f), new Vector2(W - 120f, 28f), TextAlignmentOptions.Left);
                name.text = a.def.Name;
                TMP_Text cond = UIKit.Text(r, "", 15f, new Color(0.7f, 0.66f, 0.6f), new Vector2(-40f, y - 15f), new Vector2(W - 120f, 24f), TextAlignmentOptions.Left);
                cond.text = a.def.Cond;
                TMP_Text prog = UIKit.Text(r, "", 17f, a.done ? new Color(0.49f, 1f, 0.6f) : new Color(0.8f, 0.76f, 0.7f), new Vector2(W / 2f - (framed ? 70f : 64f), y), new Vector2(110f, 28f), TextAlignmentOptions.Right);      // 새 판이면 오른쪽 금 테두리 안쪽으로 6
                prog.text = a.done ? Loc.T("board.cell.done") : Progress(a.def, Mathf.Max(a.best, a.def.Now));
                foreach (TMP_Text t in new[] { name, cond, prog }) { t.enableWordWrapping = false; UIKit.Forget(t); }
            }
        }
    }
}
