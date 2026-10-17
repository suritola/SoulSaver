using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 1.2~ 「한 판 더」 카운터: 영혼 계약(Contracts) · 계약 보드(ContractBoard) · 망자의 전령(Herald) · 기억 조각(MemoryShards)이 같이 씀
//   판 안 (Get · Add · Max): 판을 시작할 때 0 — 상자 · 상점 · 물체 · 무리 · 처치 출처 · 영웅 고유 동작 같은, RunStats 에 없던 수
//   누적 (Life · AddLife): 판을 넘어 쌓임 · Prefs "life.<열쇠>" (영웅마다 따로 세는 것은 Hero(열쇠) = 열쇠@영웅 번호)
//     처치 · 배달처럼 자주 오르는 수는 메모리에 모았다가 씬이 바뀔 때 · 판이 끝날 때 한꺼번에 저장 (Prefs.Save 를 매번 부르지 않게)
//   세지 않는 판: 튜토리얼 · 자동 플레이 · 촬영 · 배치 실행 · 체험판의 누적(보드는 정식판에서만 — 판 안 수는 셈)
//   이어하기: 계약 목록은 RunSave.Data.contracts(Contracts), 여기 판 안 수는 RunSave.Data.counters(Serialize · Restore)가 남김
public static class RunCounters
{
    static readonly Dictionary<string, int> run = new Dictionary<string, int>();
    static readonly Dictionary<string, int> lifePending = new Dictionary<string, int>();
    static readonly HashSet<string> runSet = new HashSet<string>();
    public const string LifePrefix = "life.";

    // 지금 판의 수를 세는지 (게임 씬 · 진짜 플레이)
    //   씬 이름(.name)은 읽을 때마다 새 문자열이라 씬 핸들이 바뀔 때만 다시 읽음 (처치마다 GC 없게)
    public static bool Counting => InGameScene && !Off;
    static int sceneHandle = -1;
    static bool inGameScene;
    static bool InGameScene
    {
        get
        {
            Scene sc = SceneManager.GetActiveScene();
            if (sc.handle != sceneHandle) { sceneHandle = sc.handle; inGameScene = sc.name == "GameScene"; }
            return inGameScene;
        }
    }

    // 처치 출처 열쇠 「kill.<출처>」를 한 번만 만들어 다시 씀
    static readonly Dictionary<string, string> killKeys = new Dictionary<string, string>();
    public static string KillKey(string src)
    {
        if (!killKeys.TryGetValue(src, out string k)) { k = "kill." + src; killKeys[src] = k; }
        return k;
    }
    public static bool Off => TutorialRun.Active || GameInput.Auto || GameInput.TrailerRunning || Application.isBatchMode;
    // 누적(보드 · 기억)은 정식판에서만
    public static bool LifeCounting => Counting && !Demo.On;

    // 수가 바뀌었음 (계약이 바로 다시 확인 · 토스트)
    public static event System.Action Changed;

    // ================================================================= 판 안
    public static int Get(string key) => run.TryGetValue(key, out int v) ? v : 0;

    public static void Add(string key, int n = 1)
    {
        if (n == 0 || !Counting) return;
        run.TryGetValue(key, out int v);
        run[key] = v + n;
        Changed?.Invoke();
    }

    // 한 번에 이룬 가장 큰 수 (한 번 휘둘러 몇 마리 · 한 번의 필살기로 몇 마리)
    public static void Max(string key, int value)
    {
        if (!Counting) return;
        run.TryGetValue(key, out int v);
        if (value <= v) return;
        run[key] = value;
        Changed?.Invoke();
    }

    // 이번 판에 본 것 (이벤트 6종 · 각인 종류처럼 「모두 한 번씩」을 셀 때)
    public static void Saw(string key) { if (Counting && runSet.Add(key)) Changed?.Invoke(); }
    public static bool Seen(string key) => runSet.Contains(key);
    public static int SeenCount(string prefix)
    {
        int n = 0;
        foreach (string k in runSet) if (k.StartsWith(prefix)) n++;
        return n;
    }

    // 지금 영웅을 붙인 열쇠 (누적 · 판 안 모두)
    // 1.2.5~ 열쇠 × 영웅마다 한 번만 만들어 다시 씀 (궁수 「완벽!」 화살마다 · 0.5초마다 부르므로 GC 없게 · 열쇠는 코드에 적힌 것뿐이라 판마다 쌓이지 않음)
    static readonly Dictionary<string, string[]> heroKeys = new Dictionary<string, string[]>();
    public static string Hero(string key) => Hero(key, CharacterData.Selected);
    public static string Hero(string key, CharacterId id)
    {
        int i = (int)id;
        if (i < 0) return key + "@" + i;
        if (!heroKeys.TryGetValue(key, out string[] arr) || arr.Length <= i)
        {
            string[] n = new string[Mathf.Max(i + 1, 16)];
            if (arr != null) arr.CopyTo(n, 0);
            heroKeys[key] = arr = n;
        }
        return arr[i] ??= key + "@" + i;
    }

    // 1.2.5~ 누적 저장 열쇠 「life.<열쇠>」도 한 번만 만들어 다시 씀
    static readonly Dictionary<string, string> lifeKeys = new Dictionary<string, string>();
    static string LifeKey(string key)
    {
        if (!lifeKeys.TryGetValue(key, out string k)) { k = LifePrefix + key; lifeKeys[key] = k; }
        return k;
    }

    // 이어하기 저장: "열쇠=값|…" + 본 것은 "~열쇠" (열쇠는 영문 · 숫자 · . · @ 뿐)
    public static string Serialize()
    {
        if (run.Count == 0 && runSet.Count == 0) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (KeyValuePair<string, int> kv in run)
        {
            if (sb.Length > 0) sb.Append('|');
            sb.Append(kv.Key).Append('=').Append(kv.Value);
        }
        foreach (string k in runSet)
        {
            if (sb.Length > 0) sb.Append('|');
            sb.Append('~').Append(k);
        }
        return sb.ToString();
    }

    // 이어하기 복원 (기록을 다시 적용한 뒤 · 저장된 값으로 덮어씀)
    public static void Restore(string saved)
    {
        if (string.IsNullOrEmpty(saved)) return;
        run.Clear();
        runSet.Clear();
        foreach (string part in saved.Split('|'))
        {
            if (part.Length == 0) continue;
            if (part[0] == '~') { runSet.Add(part.Substring(1)); continue; }
            int eq = part.LastIndexOf('=');
            if (eq > 0 && int.TryParse(part.Substring(eq + 1), out int v)) run[part.Substring(0, eq)] = v;
        }
        Changed?.Invoke();
    }

    // ================================================================= 누적
    public static int Life(string key) => Prefs.GetInt(LifeKey(key), 0) + (lifePending.TryGetValue(key, out int p) ? p : 0);

    public static void AddLife(string key, int n = 1)
    {
        if (n == 0 || !LifeCounting) return;
        lifePending.TryGetValue(key, out int v);
        lifePending[key] = v + n;
        Changed?.Invoke();
    }

    // 판 안 수와 지금 영웅 누적을 함께 (가장 흔한 쓰임: 상자 · 상점 · 고유 동작)
    public static void Both(string key, int n = 1)
    {
        Add(key, n);
        AddLife(key, n);
        AddLife(Hero(key), n);
    }

    // 누적 비트 (보스 4종 · 각인 12종처럼 「모두」를 셀 때) — 바로 저장
    public static void LifeBit(string key, int bit)
    {
        if (!LifeCounting || bit < 0 || bit > 30) return;
        int v = Prefs.GetInt(LifeKey(key), 0);
        if ((v & (1 << bit)) != 0) return;
        Prefs.SetInt(LifeKey(key), v | (1 << bit));
        Changed?.Invoke();
    }

    public static int Bits(string key) => Prefs.GetInt(LifeKey(key), 0);
    public static int BitCount(string key)
    {
        int v = Bits(key), n = 0;
        while (v != 0) { n += v & 1; v >>= 1; }
        return n;
    }

    // 누적 최댓값 (한 판 최고 기록)
    public static void LifeMax(string key, int value)
    {
        if (!LifeCounting) return;
        if (value <= Life(key)) return;
        lifePending.Remove(key);
        Prefs.SetInt(LifeKey(key), value);
        Changed?.Invoke();
    }

    public static void Flush()
    {
        if (lifePending.Count == 0) return;
        foreach (KeyValuePair<string, int> kv in lifePending)
            Prefs.SetInt(LifeKey(kv.Key), Prefs.GetInt(LifeKey(kv.Key), 0) + kv.Value);
        lifePending.Clear();
        Prefs.Save();
    }

    // ================================================================= 판 시작 · 끝
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            Flush();
            if (s.name != "GameScene") return;
            run.Clear();
            runSet.Clear();
            new GameObject("RunCounters").AddComponent<RunCountersHost>();
        };
        Application.quitting += Flush;
    }
}

// 판 안에서 이벤트를 받아 수를 셈 (처치 출처 · 필살기 · 상점 · 진화 · 보스) + 0.5초마다 계약 · 보드 확인
public class RunCountersHost : MonoBehaviour
{
    int ultWaveKills, boardTick;
    float tick;
    EnemySpawner spawner;

    void OnEnable()
    {
        EnermyController.Killed += OnKill;
        PlayerController.UltUsed += OnUlt;
        Shop.StallOpened += OnShop;
        SpecialAbilities.Evolved += OnEvolved;
        SpecialAbilities.Rerolled += OnReroll;
    }

    void OnReroll() => RunCounters.Add("reroll");

    void OnDisable()
    {
        EnermyController.Killed -= OnKill;
        PlayerController.UltUsed -= OnUlt;
        Shop.StallOpened -= OnShop;
        SpecialAbilities.Evolved -= OnEvolved;
        SpecialAbilities.Rerolled -= OnReroll;
        if (spawner != null) spawner.onBossDefeated -= OnBoss;
    }

    void Start()
    {
        spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null) spawner.onBossDefeated += OnBoss;
        ContractBoard.Refresh(false);       // 지금까지의 기록으로 이미 채운 칸은 조용히 (판 안 토스트는 이 판에서 새로 채운 칸만)
    }

    // 처치: 누가(출처) 쓰러뜨렸는지 · 한 번의 필살기로 몇 마리
    //   특수 능력은 출처가 능력 이름(「샷건」 등)이라 묶어서 「특수 능력」으로도 셈
    void OnKill(Vector3 pos)
    {
        if (!EnermyController.KillCounts) return;
        string src = DamageSource.Current ?? DamageSource.Other;
        RunCounters.Add(RunCounters.KillKey(src));
        if (src != DamageSource.Basic && src != DamageSource.Ult && src != DamageSource.Signature && src != DamageSource.Tree && src != DamageSource.Other && src != DamageSource.Special)
            RunCounters.Add(RunCounters.KillKey(DamageSource.Special));
        if (src == DamageSource.Ult)
        {
            ultWaveKills++;
            RunCounters.Max("ult.wave", ultWaveKills);
        }
    }

    void OnUlt()
    {
        ultWaveKills = 0;
        RunCounters.Both("ult");
    }

    void OnShop() => RunCounters.Both("shop");

    void OnEvolved(int id) => RunCounters.Add("evo");

    // 장 보스 (무한 모드 보스는 EndlessMode.BossesDefeated 로): 영웅마다 보스 4종 · 결계 전 처치 4종 · 체력 10% 아래로 잡음(전령)
    void OnBoss(int stage)
    {
        int kind = Mathf.Clamp(stage, 0, 3);        // 스테이지 번호 = 보스 종류 (0 리치 왕 · 1 지옥의 군주 · 2 킹 슬라임 · 3 거울의 군주)
        bool quick = !BossUltimate.UsedThisFight(kind);
        RunCounters.Add("boss");
        if (!GameMode.IsEndless)
        {
            RunCounters.LifeBit(RunCounters.Hero("bosskinds"), kind);
            if (quick) { RunCounters.Add("boss.quick"); RunCounters.LifeBit("quickkinds", kind); }
            Prefs.SetInt("boss.down." + kind, Prefs.GetInt("boss.down." + kind, 0) + (RunCounters.Off ? 0 : 1));      // 보스 재회 한 줄 (StoryDirector)
        }
        PlayerController p = Hostile.Player;
        Herald.LowHpBossAt = p != null && p.PlayerHealth > 0f && p.PlayerHealth <= p.PlayerMaxHealth * 0.1f ? Chapters.Number(Chapters.CurrentStage) : -1;
    }

    void Update()
    {
        tick -= Time.unscaledDeltaTime;
        if (tick > 0f) return;
        tick = 0.5f;
        Contracts.Tick();
        // 무한 모드: 이 영웅으로 버틴 가장 긴 시간 (기억 조각 ⑤ · 10초 단위로 적음)
        if (GameMode.IsEndless) RunCounters.LifeMax(RunCounters.Hero("endless.best"), Mathf.FloorToInt(GameMode.EndlessSeconds / 10f) * 10);
        if (++boardTick % 4 == 0) ContractBoard.Refresh(true);       // 2초마다 보드 칸 (판 안에서 채우면 토스트)
    }

    void OnDestroy() => RunCounters.Flush();
}
