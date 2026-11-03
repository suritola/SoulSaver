using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 2.0.0~ 역류 (docs/plans/2026-11-02-big-update/modes.md 2절): 15분째 완성된 몸으로 시작해 시간이 거꾸로 흐름
//   1분마다 「무엇을 잃을까」 3택으로 힘을 하나씩 내려놓고 적도 약해짐 · 개발자 → 거울의 군주 → 킹 슬라임 → 지옥의 군주 → 리치 왕 → 맨몸 「처음의 슬라임」
// 일일 도전 패턴: GameMode.Override = Endless · CharacterData.Override = <고른 영웅> → 사막 · 무한 규칙 · HUD · 제출 길(EndlessMode.Finish)이 따라옴
//   일정(보스 · 잃기 · 맨몸)은 RefluxDirector 가 대신 (EndlessMode 는 일정만 끔) · 적 세기 곡선은 GameMode.RampSeconds 가 넣는 시간만 뒤집음
// 기록은 오름차순 순위표(SteamLeaderboards.Reflux · 짧을수록 좋음) · 로컬 최고는 Prefs "reflux.best" (LeaderboardUI 역류 탭이 읽음)
public static class RefluxRun
{
    public static bool Active { get; private set; }
    public static CharacterId Hero { get; private set; }
    public static int Path { get; private set; }            // 고른 갈래 0 ~ 4
    public static int Form { get; private set; }            // 1차 무기 형태 번호 0 ~ 2

    // 열리는 조건 (D10): 개발자를 한 번이라도 쓰러뜨림 (무한 · 일일 도전 · 릴레이 어디서든) — 옛 저장은 이정표 · 클리어 기록으로 소급
    public const string DevKey = "dev.defeated";
    public static bool Unlocked => !Demo.On && (Prefs.GetInt(DevKey, 0) == 1 || Prefs.GetInt("endless.milestone.dev", 0) != 0 || EndlessMode.EverCleared);
    public static bool Implemented => true;

    // 개발자를 처음 쓰러뜨린 순간 (EndlessMode.OnBossDefeated)
    public static void MarkDeveloperDefeated()
    {
        if (GameInput.Auto || Demo.On || Prefs.GetInt(DevKey, 0) == 1) return;
        bool was = Unlocked;
        Prefs.SetInt(DevKey, 1);
        Prefs.Save();
        if (!was && !Active) Notice.Big(Loc.T("reflux.unlock.title"), Loc.T("reflux.unlock.sub"), NoticeIcon.Unlock, 4f, NoticeLevel.Important);
    }

    // ================================================================= 기록 (Prefs)
    public const string BestKey = "reflux.best";
    const string BestHeroPrefix = "reflux.best.", LastKey = "reflux.last", ClearedKey = "reflux.cleared";
    // 로컬 최고 기록 (초, 0 = 없음 · 짧을수록 좋음)
    public static float Best => Prefs.GetFloat(BestKey, 0f);
    public static float BestOf(CharacterId hero) => Prefs.GetFloat(BestHeroPrefix + (int)hero, 0f);
    public static bool EverClearedReflux => Prefs.GetInt(ClearedKey, 0) == 1;

    // 지난 역류에서 고른 영웅 · 갈래 · 1차 무기 (없으면 false)
    public static bool LoadLast(out CharacterId hero, out int path, out int form)
    {
        hero = CharacterData.Selected; path = 0; form = 0;
        string[] p = Prefs.GetString(LastKey, "").Split(',');
        if (p.Length != 3 || !int.TryParse(p[0], out int h) || !int.TryParse(p[1], out int a) || !int.TryParse(p[2], out int f)) return false;
        if (h < 0 || h >= CharacterData.All.Length || !CharacterData.IsUnlocked((CharacterId)h)) return false;
        hero = (CharacterId)h; path = Mathf.Clamp(a, 0, Paths.Count - 1); form = Mathf.Clamp(f, 0, 2);
        return true;
    }

    // ================================================================= 시작 · 끝
    // 같은 선택이면 누구나 같은 몸 (고정 시드 — 일일 도전 갈래 뽑기와 같은 방식)
    public static int SeedOf(CharacterId who, int path, int form) => unchecked((int)who * 7919 + path * 104729 + form);

    public static void Begin(CharacterId hero, int path, int form)
    {
        if (!Unlocked || !Setup(hero, path, form)) return;
        Prefs.SetString(LastKey, (int)hero + "," + path + "," + form);
        Prefs.Save();
        SceneFlow.Go("GameScene");
    }

    // 배치 테스트 · 봇 균형 측정용: 장면은 부르는 쪽이 로드 (업적 · 순위 · 포인트는 기존 Auto 조건이 막음)
    public static void BeginForTest(CharacterId hero, int path, int form) { if (Setup(hero, path, form)) fresh = false; }

    // 2.0.0~ 검토: 「다시 시작」(ESC · 결과 화면 [R] · 같은 영웅으로 다시)은 Begin 을 거치지 않고 GameScene 을 다시 불러서 앞 판의 클리어 · 맨몸 · 잃기가 남았음
    //   SceneFlow.Go("GameScene") 가 여기를 부름 — Begin 이 막 준비한 첫 판은 그대로, 그 뒤 같은 모드로 다시 불러오는 판은 판 상태를 새로
    static bool fresh;
    internal static void BeforeGameScene()
    {
        if (!Active) return;
        if (fresh) { fresh = false; return; }
        ResetRunState();
    }

    static bool Setup(CharacterId hero, int path, int form)
    {
        if ((int)hero < 0 || (int)hero >= CharacterData.All.Length) return false;
        ModeRun.EndAll();
        Hero = hero;
        Path = Mathf.Clamp(path, 0, Paths.Count - 1);
        Form = Mathf.Clamp(form, 0, 2);
        ResetRunState();
        fresh = true;
        Active = true;
        GameMode.Override = Difficulty.Endless;
        CharacterData.Override = hero;
        return true;
    }

    public static void End()
    {
        if (!Active) return;
        Active = false;
        fresh = false;
        GameMode.Override = null;
        CharacterData.Override = null;
        VirtualOverride = -1f;
        Director = null;
    }

    static void ResetRunState()
    {
        Cleared = false;
        ClearSeconds = 0f;
        LossCount = BossCount = 0;
        Naked = false;
        ShownLevel = 40;
        NakedHit = false;
        SafetyStart = 720f;
        VirtualOverride = -1f;
        LastPrevBest = LastPrevBestHero = 0f;
        LastNewRecord = false;
        Kept = "";
        Director = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) => { if (s.name == "MainMenu") End(); };
    }

    // ================================================================= 판 상태 (RefluxDirector 가 적음)
    public static RefluxDirector Director { get; internal set; }
    public static bool Cleared { get; internal set; }
    public static float ClearSeconds { get; internal set; }
    public static int LossCount { get; internal set; }              // 지금까지 고른 「잃기」 수 (0 ~ 9)
    public static int BossCount { get; internal set; }              // 쓰러뜨린 보스 수 (0 ~ 6 — 6번째가 처음의 슬라임)
    public static bool Naked { get; internal set; }                 // 「역류의 끝」 뒤 맨몸
    public static bool NakedHit { get; internal set; }              // 처음의 슬라임이 나온 뒤 맞았음
    public static int ShownLevel { get; internal set; } = 40;       // HUD 레벨 숫자 (잃을 때마다 같이 줄어듦)
    public static string Kept { get; internal set; } = "";          // 「끝까지 쥔 것」 (맨몸 직전 남은 항목 상위 2개)
    public const int MaxLoss = 9, BossTotal = 6;
    public static int SpiritLeft => Mathf.Max(0, MaxLoss - LossCount);       // 「남은 영혼」: 쓰지 않은 잃기

    // ================================================================= 거꾸로 가는 적 세기 곡선 (modes.md 2-7)
    //   곡선 식은 그대로 두고 GameMode.RampSeconds 가 넣는 시간만 뒤집음 — 899 는 Surge(FinalAt=900 이상)에 걸리지 않게
    public const float VirtualStart = 899f, VirtualRate = 1.5f;
    public static float VirtualOverride = -1f;                       // 0 이상이면 이 가상 시간으로 고정 (처음의 슬라임 체력 계산)
    public static float VirtualSeconds => VirtualOverride >= 0f ? VirtualOverride : Mathf.Max(0f, VirtualStart - VirtualRate * GameMode.EndlessSeconds);
    // 화면 시계 (15:00 → 00:00)
    public static float ShownClock => Mathf.Max(0f, VirtualSeconds + 1f);

    // 몹 종류 거름 (가상 9분↑ 전체 · 3~9분 1 · 2장 · 3분↓ 1장 · 맨몸 이후는 1장만) — EnemySpawner.Pick 이 적 목록 번호의 장(tier)으로 물음
    public static float TierMul(float tier)
    {
        int chapter = Mathf.FloorToInt(tier);
        if (Naked) return chapter <= 0 ? 1f : 0f;
        float v = VirtualSeconds;
        if (v >= 540f) return 1f;
        if (v >= 180f) return chapter <= 1 ? 1f : 0f;
        return chapter <= 0 ? 1f : 0f;
    }

    // 풍화 안전장치: 실제 12:00 이 지나도 보스가 남으면 1분마다 +50% (맨몸 끝판이 영원히 안 끝나는 일 방지)
    static float SafetyStart = 720f;
    public static float SafetyErosion
    {
        get
        {
            float over = GameMode.EndlessSeconds - SafetyStart;
            return over <= 0f ? 1f : 1f + over / 60f * 0.5f;
        }
    }

    // 2.0.1~ 오래 사는 보스(개발자 포함) · 처음의 슬라임 풍화: 나온 지 2분 뒤부터 1분마다 +50% (상한 10배) — 잃기로 약해지는 몸으로도 보스전이 끝나게
    //   자동 플레이 4판 모두 보스 둘이 13분 넘게 살아 판이 멈췄음 (보통 풍화는 개발자 제외 · 최대 2.5배라 모자람) · 실제 12:00 안전장치와 큰 쪽
    //   2.0.1~ 재측정: 10배 상한으로도 씨름꾼 봇이 20분 동안 보스를 하나도 못 잡음 (보스 받은 피해 2.7만 · 개발자 체력 3.6만) → 나온 지 6분 뒤부터는 1분마다 두 배 (상한 200배) — 언젠가는 반드시 끝남
    const float ErodeAfter = 120f, ErodePerMin = 0.5f, ErodeCap = 200f, DoubleAfter = 360f;
    public static float ErosionFor(float aliveSeconds)
    {
        float mul = 1f + Mathf.Max(0f, aliveSeconds - ErodeAfter) / 60f * ErodePerMin;
        if (aliveSeconds > DoubleAfter) mul *= Mathf.Pow(2f, (aliveSeconds - DoubleAfter) / 60f);
        return Mathf.Max(SafetyErosion, Mathf.Min(ErodeCap, mul));
    }

    // ================================================================= HUD · 보스 처치 알림
    public static string HudLine(float realSeconds)
    {
        string clock = EndlessMode.Clock(ShownClock);
        string rec = Loc.T("reflux.hud.time") + " " + EndlessMode.Clock(realSeconds);
        string best = Best > 0f ? "   " + Loc.T("최고 ") + EndlessMode.Clock(Best) : "";
        return Loc.T("역류") + "  " + clock + "   " + Loc.T("reflux.hud.lost").Replace("{0}", LossCount.ToString()).Replace("{1}", MaxLoss.ToString())
             + " <size=75%><color=#C9B8A0>(" + Loc.T("reflux.hud.spirit").Replace("{0}", SpiritLeft.ToString()) + ")</color></size>"      // 2.0.1~ 결과 · 계약의 「남은 영혼」이 무엇인지 판 안에서도 보이게
             + " · " + Loc.T("reflux.hud.boss").Replace("{0}", BossCount.ToString()).Replace("{1}", BossTotal.ToString())
             + "   <size=75%><color=#C9B8A0>" + rec + best + "</color></size>";
    }

    // EndlessMode.OnBossDefeated (역류 판): 진화 창 · 전리품 없음 → 체력 회복 + 다음 일정
    public static void OnBossDown()
    {
        if (Director != null) Director.BossDown();
    }

    // ================================================================= 판이 끝날 때 (EndlessMode.Finish)
    public static float LastPrevBest, LastPrevBestHero;
    public static bool LastNewRecord;
    public static string LastBoard = "";

    public static void Submit(float seconds, int bosses)
    {
        LastPrevBest = Best;
        LastPrevBestHero = BestOf(Hero);
        LastNewRecord = false;
        DailyChallenge.LastBoard = "";
        DailyChallenge.LastRank = DailyChallenge.LastTotal = 0;
        if (!Cleared || GameInput.Auto || Application.isBatchMode || ClearSeconds < 1f) return;
        float t = ClearSeconds;
        if (LastPrevBest <= 0f || t < LastPrevBest) { Prefs.SetFloat(BestKey, t); LastNewRecord = true; }
        if (LastPrevBestHero <= 0f || t < LastPrevBestHero) Prefs.SetFloat(BestHeroPrefix + (int)Hero, t);
        Prefs.SetInt(ClearedKey, 1);
        Prefs.Save();
        // 자세한 값: 0 영웅 · 1 갈래 번호 · 2 남은 영혼 · 3 처치 수 · 4 판 버전
        int[] details = { (int)Hero, Path, SpiritLeft, RunStats.Kills, SteamLeaderboards.VersionCode() };
        DailyChallenge.LastBoard = SteamLeaderboards.Reflux;
        SteamLeaderboards.Upload(SteamLeaderboards.Reflux, Mathf.RoundToInt(t), details, (rank, total) =>
        {
            if (DailyChallenge.LastBoard != SteamLeaderboards.Reflux) return;
            DailyChallenge.LastRank = rank;
            DailyChallenge.LastTotal = total;
        });
    }

    // 결과 화면 세 줄 (MenuExtras.InstallGameOver)
    public static string ResultText(float seconds)
    {
        string head = Loc.T("역류") + " · " + Loc.T(CharacterData.Def(Hero).name) + " · " + PathName();
        string line2, line3;
        if (Cleared)
        {
            line2 = Loc.T("reflux.result.clear") + " " + EndlessMode.Clock(ClearSeconds) + "   " + Loc.T("reflux.result.spirit") + " " + SpiritLeft
                  + (Kept.Length > 0 ? "   <size=80%><color=#A89C86>" + Loc.T("reflux.result.kept") + " " + Kept + "</color></size>" : "");
            line3 = LastNewRecord
                ? Loc.T("reflux.result.best") + " " + (LastPrevBest > 0f ? EndlessMode.Clock(LastPrevBest) + " → " : "") + EndlessMode.Clock(ClearSeconds) + " <color=#F5D478>(" + Loc.T("relay.result.newrecord") + ")</color>"
                : Loc.T("reflux.result.best") + " " + EndlessMode.Clock(Best);
        }
        else
        {
            line2 = Loc.T("reflux.result.fail").Replace("{0}", Mathf.Min(BossCount + 1, BossTotal).ToString()).Replace("{1}", BossTotal.ToString()).Replace("{2}", EndlessMode.Clock(seconds));
            line3 = Loc.T("reflux.result.best") + " " + (Best > 0f ? EndlessMode.Clock(Best) : "-");
        }
        return head + "\n" + line2 + "\n" + line3;
    }

    static string PathName()
    {
        PathDef d = Paths.DefOf(Hero, Path);
        return d != null ? "「" + Loc.T(d.name) + "」" : "";
    }
}

// ===================================================================== 판 안 감독 (StageManager 오브젝트에 붙음)
//   시간표(보스) · 잃기 타이머 · 「무엇을 잃을까」 3택 만들기 · 맨몸 · 처음의 슬라임 · 클리어
public class RefluxDirector : MonoBehaviour
{
    // 보스 순서: 종류 -1 = 개발자 · 0 리치 왕 · 1 지옥의 군주 · 2 킹 슬라임 · 3 거울의 군주 (EndlessMode.SpawnBossKind)
    static readonly int[] Kinds = { -1, 3, 2, 1, 0 };
    static readonly float[] At = { 3f, 150f, 270f, 360f, 450f };             // 예정 시각 (실제 초)
    static readonly float[] HpMul = { 0.6f, 1f, 1f, 0.5f, 1f };               // 개발자 0.6 · 지옥의 군주 0.5 (기본 체력이 리치 왕의 5배)
    const float AfterDownGap = 6f;              // 앞 보스 처치 + 6초
    const float LossEvery = 60f;
    // 2.0.1~ 잃기가 결계 · 필살기 연출에 막혀 16분에 2 ~ 4번뿐이었음: 결계 · 연출만 피하되 예정보다 LossMaxWait 초 늦으면 그냥 열고,
    //   밀린 잃기는 LossCatchUp 초 간격으로 한 번만 따라잡음 (그 이상 밀린 시간은 버림) · 창을 닫은 뒤 LossGrace 초 무적 (갑자기 약해진 몸 숨 고르기)
    const float LossMaxWait = 25f, LossCatchUp = 20f, LossGrace = 2.5f;
    const float NakedMaxWait = 20f;             // 2.0.1~ 맨몸 전 연출 대기 상한 (1.2.7 교훈: 대기에는 상한)
    const float LossHeal = 0.25f, BossHeal = 0.4f;
    const int SlimeHpMul = 40;

    EndlessMode em;
    StageManager sm;
    EnemySpawner spawner;
    LevelShop shop;
    SpecialAbilities sp;
    PlayerController player;

    readonly bosss[] tracked = new bosss[At.Length];
    readonly bool[] spawnedAt = new bool[At.Length];
    readonly bool[] counted = new bool[At.Length];
    bool evoLost;                   // 무기 진화를 하나라도 잃었음 (계약 「진화를 끝까지 지키기」)
    int nextBoss;                   // 다음에 나올 보스 번호
    float lastDownT = -99f;         // 마지막 보스 처치 시각 (실제 초)
    bool filled, offering, nakedStarted, finished;
    float nextLossAt;
    float initialWeight = 1f;
    readonly Dictionary<string, float> rejected = new Dictionary<string, float>();      // 지난 3택에서 고르지 않은 항목 → 다음 무게 ×0.8
    readonly List<string> keptNames = new List<string>();

    // 처음의 슬라임
    EnermyController slime;
    bool slimeSpawned;
    GameObject slimePrefab;
    Image barFill;
    GameObject barRoot;
    float lastHp;
    float slimeAt;                  // 처음의 슬라임이 나온 시각 (풍화 기준)

    public bool AutoDone => filled;

    public void Begin(EndlessMode endless, StageManager stages)
    {
        em = endless;
        sm = stages;
        RefluxRun.Director = this;
        StartCoroutine(Run());
    }

    void OnDestroy()
    {
        if (RefluxRun.Director == this) RefluxRun.Director = null;
        if (barRoot != null) Destroy(barRoot);
    }

    // ================================================================= 진행
    IEnumerator Run()
    {
        // 필요한 것이 모두 생길 때까지 (키트의 Start 는 다음 프레임이라 늦을 수 있음 · 상한 5초)
        float until = Time.unscaledTime + 5f;
        while (Time.unscaledTime < until)
        {
            shop = FindFirstObjectByType<LevelShop>();
            sp = SpecialAbilities.SharedInstance;
            player = Hostile.Player;
            spawner = sm != null ? sm.spawner : FindFirstObjectByType<EnemySpawner>();
            if (shop != null && sp != null && player != null && spawner != null && (CharacterData.IsGunner || CharacterKit.Instance != null)) break;
            yield return null;
        }
        // 2.0.1~ 준비 실패면 보스도 잃기도 없는 판이 끝없이 이어졌음 (sp 가 null 이면 맨몸에서 예외) → 알리고 메뉴로
        if (shop == null || sp == null || player == null || spawner == null)
        {
            Debug.LogError("[역류] 감독 준비 실패: shop=" + (shop != null) + " sp=" + (sp != null) + " player=" + (player != null) + " spawner=" + (spawner != null));
            filled = true;
            finished = true;
            Notice.Big(Loc.T("reflux.fail.title"), Loc.T("reflux.fail.sub"), NoticeIcon.Warn, 3f, NoticeLevel.Urgent);
            yield return new WaitForSecondsRealtime(3f);
            SceneFlow.Go("MainMenu");
            yield break;
        }
        yield return null;
        shop.AutoFill(RefluxRun.Hero, RefluxRun.Path, RefluxRun.Form, RefluxRun.SeedOf(RefluxRun.Hero, RefluxRun.Path, RefluxRun.Form));
        initialWeight = Mathf.Max(1f, TotalWeight());
        RefluxRun.ShownLevel = 40;
        filled = true;
        player = Hostile.Player;
        if (player != null) player.GrantInvincibility(1.5f);
        Notice.Big(Loc.T("reflux.banner"), Loc.T("reflux.banner.sub"), NoticeIcon.Boss, 2.5f, NoticeLevel.Important);
        nextLossAt = Time.time + LossEvery;
        while (!finished)
        {
            Tick();
            yield return null;
        }
    }

    void Tick()
    {
        if (player == null) player = Hostile.Player;
        float t = GameMode.EndlessSeconds;
        // free: 창 · 일시정지 · 쓰러짐이 아님 (이 동안은 시간이 멈춰 대기가 쌓이지 않음) · calm: 거기에 결계 · 공간 침식 · 연출도 아님
        bool free = Time.timeScale > 0f && !ChoiceUI.Open && sm != null && !sm.IsMenuOpen && player != null && !player.IsDying;
        bool calm = free && !BossUltimate.Active && !BossUltimate.ErosionPending && !PlayerController.Cinematic;

        // 처음의 슬라임이 나온 뒤 맞았는지 (업적 · 계약)
        if (slime != null && player != null)
        {
            if (player.PlayerHealth < lastHp - 0.01f) RefluxRun.NakedHit = true;
            lastHp = player.PlayerHealth;
        }

        if (calm && nextBoss < At.Length && bosss.Dying == 0 && bosss.LiveCount < 2) TrySpawnBoss(t);
        // 2.0.1~ 잃기: 결계 · 연출 · 처치 연출이 끝난 틈에, 예정보다 LossMaxWait 초 늦었으면 그냥 (보스 둘이면 결계가 이어져 거의 안 열렸음)
        if (free && !offering && !nakedStarted && RefluxRun.LossCount < RefluxRun.MaxLoss && Time.time >= nextLossAt
            && ((calm && bosss.Dying == 0) || (Time.time - nextLossAt >= LossMaxWait && !PlayerController.Cinematic && !player.IsAiming)))      // 늦었어도 연출 · 필살기 조준(느린 시간) 중엔 열지 않음 — 창이 느린 시간을 기억했다 되돌림
            StartCoroutine(OfferLoss());
        // 리치 왕까지 모두 나왔고 모두 쓰러졌고 처치 연출이 끝났으면 「역류의 끝」
        if (calm && !nakedStarted && !offering && nextBoss >= At.Length && AllBossesDown() && bosss.Dying == 0)
            StartCoroutine(Naked());
        if (slime != null)
        {
            slime.takenMul = RefluxRun.ErosionFor(Time.time - slimeAt);       // 2.0.1~ 처음의 슬라임도 풍화 (맨몸으로 영영 못 잡는 판 방지)
            UpdateBar();
        }
        // 2.0.0~ 검토: 슬라임이 onKilled 없이 사라지면(Purge · 장외 · Destroy) 클리어 조건이 영영 안 켜짐 → 사라졌으면 클리어로 침
        if (slimeSpawned && !finished && (slime == null || slime.IsDead)) Clear();
    }

    bool AllBossesDown()
    {
        for (int i = 0; i < tracked.Length; i++)
        {
            if (!spawnedAt[i]) return false;
            if (tracked[i] != null && !tracked[i].IsDead) return false;
        }
        return true;
    }

    // 예정 시각이 되었거나(앞 보스가 아직 살아 있어도 함께) 앞 보스를 쓰러뜨리고 6초가 지났으면 다음 보스
    void TrySpawnBoss(float t)
    {
        int i = nextBoss;
        float due = At[i];
        if (i > 0 && (tracked[i - 1] == null || tracked[i - 1].IsDead) && spawnedAt[i - 1]) due = Mathf.Min(due, lastDownT + AfterDownGap);
        if (t < due) return;
        bosss b = i == 0 ? em.SpawnDeveloperReflux(HpMul[0]) : em.SpawnBossKind(Kinds[i], HpMul[i]);
        if (i == 0 && b != null)
        {
            Notice.Big(Loc.T("최종 보스 「개발자」가 강림한다!"), NoticeIcon.Boss, 3f, NoticeLevel.Urgent);       // 개발자는 SpawnBossKind 를 거치지 않아 도착 알림을 여기서
            Hostile.Play("roar", 1f, 0.45f);
        }
        spawnedAt[i] = true;
        tracked[i] = b;
        nextBoss++;
        if (b == null) RefluxRun.BossCount++;       // 견본이 없으면 건너뜀 (멈추지 않게)
    }

    // 보스 하나를 쓰러뜨림 (EndlessMode.OnBossDefeated → RefluxRun.OnBossDown)
    public void BossDown()
    {
        float t = GameMode.EndlessSeconds;
        int newly = 0;
        for (int i = 0; i < tracked.Length; i++)
        {
            if (counted[i] || tracked[i] == null || !tracked[i].IsDead) continue;
            counted[i] = true;
            newly++;
            if (i == 0 && t - At[0] <= 120f) RunCounters.Max("reflux.dev2", 1);          // 계약: 개발자를 2분 안에
            if (i == tracked.Length - 1 && !evoLost) RunCounters.Max("reflux.keepevo", 1);   // 계약: 무기 진화를 리치 왕까지 하나도 잃지 않기
        }
        RefluxRun.BossCount = Mathf.Min(RefluxRun.BossCount + Mathf.Max(1, newly), 5);
        lastDownT = t;
        Heal(BossHeal);
        Notice.Big(Loc.T("보스 처치!"), NoticeIcon.Star, 2.5f, NoticeLevel.Normal);
        if (spawner != null) spawner.bossCleared = false;
    }

    void Heal(float frac)
    {
        if (player == null) player = Hostile.Player;
        if (player == null || player.IsDying) return;
        player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, player.PlayerHealth + player.PlayerMaxHealth * frac);
    }

    // ================================================================= 「무엇을 잃을까」
    class LossItem
    {
        public string key, name, line;
        public int kind, id;            // kind: 0 카드 · 1 무기 진화 · 2 축복 한 조각 · 3 갈래
        public float w;
        public int cat;                 // 0 공격 · 1 생존 · 2 영웅다움 (X4)
        public Sprite icon;
    }

    class Bundle
    {
        public readonly List<LossItem> items = new List<LossItem>();
        public float w;
        public int cat;
        public void Add(LossItem i) { items.Add(i); w += i.w; }
        public void Remove(LossItem i) { items.Remove(i); w -= i.w; }
    }

    static readonly Color[] CatColor = { new Color(1f, 0.6f, 0.45f), new Color(0.55f, 0.9f, 0.6f), new Color(0.78f, 0.62f, 1f) };

    // 지금 몸에서 잃을 수 있는 모든 항목 (무게 = 그 판 화력에서 차지하는 몫의 어림)
    List<LossItem> BuildPool(int k)
    {
        List<LossItem> pool = new List<LossItem>();
        if (shop == null || sp == null) return pool;
        // 카드: 무게 = 단계 수 (스킬 진화 재료는 +2 · 영웅다움 묶음) / 공용 생존 8 · 9 · 10 은 생존 묶음
        foreach (LevelShop.RefluxCard c in shop.RefluxCards())
        {
            LossItem it = new LossItem
            {
                key = "c" + c.id, kind = 0, id = c.id, w = c.level + (c.evoPart ? 2f : 0f),
                cat = c.evoPart ? 2 : (c.id == 8 || c.id == 9 || c.id == 10) ? 1 : 0,
                name = c.name, icon = c.icon,
            };
            it.line = c.name + "  <color=#E6DCC4>Lv " + c.level + " → 0</color>" + (c.evoPart ? "  <color=#C9A0FF>◆ " + Loc.T("reflux.lose.evo") + "</color>" : "");
            pool.Add(it);
        }
        // 무기 진화: 위에서부터만 (각성 4 → 2차 4 → 1차 6) · 1차 무기는 갈래보다 뒤 (형태 전용 갈래 효과가 null 을 만나지 않게 — modes.md 4-16)
        int tier = sp.RefluxTopTier;
        if (tier >= 2 || (tier == 1 && !(Paths.Active && Paths.A >= 0)))
        {
            string en = sp.RefluxTopEvolutionName();
            string tierName = Loc.T(tier >= 3 ? "각성" : tier == 2 ? "reflux.tier2" : "reflux.tier1");
            pool.Add(new LossItem
            {
                key = "e", kind = 1, w = tier == 1 ? 6f : 4f, cat = tier == 1 ? 2 : 0,
                name = en, icon = sp.RefluxTopEvolutionIcon(),
                line = tierName + "  " + en + "  <color=#E6DCC4>→ " + Loc.T("reflux.lose.gone") + "</color>",
            });
        }
        // 역류의 축복 한 조각 (공격 · 간격 · 치명타는 공격 묶음 · 체력은 생존 묶음)
        for (int b = 0; b < sp.blessing.Length; b++)
        {
            if (sp.blessing[b] <= 0) continue;
            string bn = Loc.T("reflux.bless." + b);
            pool.Add(new LossItem
            {
                key = "b" + b, kind = 2, id = b, w = 2f, cat = b == SpecialAbilities.BlessHp ? 1 : 0,
                name = bn, icon = null,
                line = bn + "  <color=#E6DCC4>" + sp.blessing[b] + " → " + (sp.blessing[b] - 1) + "</color>",
            });
        }
        // 갈래 전체 (5번째 잃기부터 · 한 번)
        if (k >= 5 && shop.RefluxPathInfo(out string pn, out Sprite pi, out int steps))
        {
            pool.Add(new LossItem
            {
                key = "p", kind = 3, w = 8f, cat = 2, name = Loc.T("reflux.lose.path").Replace("{0}", pn), icon = pi,
                line = Loc.T("reflux.lose.path").Replace("{0}", pn) + "  <color=#E6DCC4>" + Loc.T("reflux.lose.all") + "</color>",
            });
        }
        return pool;
    }

    float TotalWeight()
    {
        float w = 0f;
        foreach (LossItem i in BuildPool(99)) w += i.w;
        return w;
    }

    // 종류가 다른 세 묶음 · 크기는 서로 ±30% (X4)
    List<Bundle> BuildOffers(int k)
    {
        List<LossItem> pool = BuildPool(k);
        if (pool.Count == 0) return null;
        foreach (LossItem it in pool) if (rejected.TryGetValue(it.key, out float m)) it.w *= m;        // 지난 3택에서 안 고른 것은 덜 매력적으로
        float total = 0f;
        foreach (LossItem it in pool) total += it.w;
        float target = Mathf.Max(1f, k >= RefluxRun.MaxLoss ? total * 0.5f : total / (10 - k));
        List<LossItem> left = new List<LossItem>(pool);
        List<Bundle> res = new List<Bundle>();
        for (int cat = 0; cat < 3; cat++)
        {
            List<LossItem> mine = left.FindAll(i => i.cat == cat);
            if (mine.Count == 0) continue;
            Bundle b = MakeBundle(mine, target, cat);
            foreach (LossItem i in b.items) left.Remove(i);
            res.Add(b);
        }
        while (res.Count < 3 && left.Count > 0)             // 종류가 비면 남은 항목에서
        {
            Bundle b = MakeBundle(left, target, left[0].cat);
            foreach (LossItem i in b.items) left.Remove(i);
            res.Add(b);
        }
        // 크기 차이 ±30%: 큰 쪽은 가장 작은 항목을 덜어 내고, 작은 쪽은 남은 항목으로 채움
        float mean = 0f;
        foreach (Bundle b in res) mean += b.w;
        mean /= Mathf.Max(1, res.Count);
        foreach (Bundle b in res)
            while (b.items.Count > 1 && b.w > mean * 1.3f)
            {
                LossItem small = b.items[0];
                foreach (LossItem i in b.items) if (i.w < small.w) small = i;
                b.Remove(small);
                left.Add(small);
            }
        foreach (Bundle b in res)
            while (b.items.Count < 3 && b.w < mean * 0.7f)
            {
                LossItem best = null;
                foreach (LossItem i in left) if (b.w + i.w <= mean * 1.3f && (best == null || i.w > best.w)) best = i;
                if (best == null) break;
                left.Remove(best);
                b.Add(best);
            }
        // 다음 번 무게 ×0.8 표시를 새로 (이번에 나온 항목만)
        rejected.Clear();
        return res;
    }

    // 욕심쟁이: target 이하 중 가장 큰 항목으로 시작해 큰 것부터 target 에 가깝게 (1 ~ 3개)
    static Bundle MakeBundle(List<LossItem> from, float target, int cat)
    {
        List<LossItem> sorted = new List<LossItem>(from);
        sorted.Sort((a, b) => b.w.CompareTo(a.w));
        Bundle bundle = new Bundle { cat = cat };
        LossItem first = sorted.Find(i => i.w <= target * 1.1f) ?? sorted[sorted.Count - 1];
        bundle.Add(first);
        foreach (LossItem i in sorted)
        {
            if (bundle.items.Count >= 3) break;
            if (bundle.items.Contains(i)) continue;
            if (bundle.w + i.w <= target * 1.15f) bundle.Add(i);
        }
        return bundle;
    }

    IEnumerator OfferLoss()
    {
        offering = true;
        float due = nextLossAt;
        int k = RefluxRun.LossCount + 1;
        List<Bundle> offers = BuildOffers(k);
        if (offers == null || offers.Count == 0)
        {
            nextLossAt = Time.time + LossEvery;
            offering = false;
            yield break;
        }
        ChoiceUI.Option[] opts = new ChoiceUI.Option[offers.Count];
        for (int i = 0; i < offers.Count; i++)
        {
            Bundle b = offers[i];
            List<string> names = new List<string>(), lines = new List<string>();
            foreach (LossItem it in b.items) { names.Add(it.name); lines.Add(it.line); }
            Sprite icon = null;
            foreach (LossItem it in b.items) if (it.icon != null) { icon = it.icon; break; }
            string title = Loc.T("reflux.lose.name").Replace("{0}", string.Join(" · ", names));
            string desc = string.Join("\n", lines) + "\n<color=#A89C86>" + Loc.T("reflux.cat." + b.cat) + "</color>";
            opts[i] = icon != null ? new ChoiceUI.Option(title, desc, CatColor[Mathf.Clamp(b.cat, 0, 2)], icon) : new ChoiceUI.Option(title, desc, CatColor[Mathf.Clamp(b.cat, 0, 2)], "curse");
        }
        string sub = Loc.T("reflux.lose.sub").Replace("{0}", k.ToString()).Replace("{1}", RefluxRun.MaxLoss.ToString()).Replace("{2}", EndlessMode.Clock(RefluxRun.ShownClock));
        int picked = 0;
        yield return ChoiceUI.Run(Loc.T("reflux.lose.title"), sub, opts, i => picked = i);
        picked = Mathf.Clamp(picked, 0, offers.Count - 1);
        Apply(offers[picked], offers);
        // 2.0.1~ 다음 잃기는 예정 시각 + 60초 (늦게 열렸어도 「1분마다」를 지킴) — 밀렸으면 LossCatchUp 초 뒤 한 번만, 더 밀린 시간은 버림
        nextLossAt = Mathf.Max(due + LossEvery, Time.time + LossCatchUp);
        if (player != null && !player.IsDying) player.GrantInvincibility(LossGrace);       // 시각으로 끝나는 무적 (남는 상태 없음)
        offering = false;
    }

    void Apply(Bundle b, List<Bundle> all)
    {
        for (int i = 0; i < all.Count; i++)
            foreach (LossItem it in all[i].items)
                if (all[i] != b) rejected[it.key] = 0.8f;
        List<string> gone = new List<string>();
        foreach (LossItem it in b.items)
        {
            switch (it.kind)
            {
                case 0: shop.UndoCard(it.id); break;
                case 1: sp.UndoEvolution(); evoLost = true; break;
                case 2: sp.RefluxBless(it.id, -1); break;
                case 3: shop.RefluxLosePath(); break;
            }
            gone.Add(it.name);
        }
        RefluxRun.LossCount++;
        RunCounters.Add("reflux.lost." + b.cat);
        RefreshLevel();
        Heal(LossHeal);
        if (player != null && SpecialAbilities.SharedFx != null)
            SpecialAbilities.SharedFx.FloatText(player.transform.position + Vector3.up * 2.2f, Loc.T("reflux.lose.name").Replace("{0}", string.Join(" · ", gone)), CatColor[Mathf.Clamp(b.cat, 0, 2)], 6f, 0f, 1.6f);
        Hostile.Play("whoosh", 0.6f, 0.6f);
        Notice.Toast(NoticeIcon.Warn, Loc.T("reflux.lose.name").Replace("{0}", string.Join(" · ", gone)), 3.5f);
    }

    // HUD 레벨 숫자: 남은 무게 비율대로 40 → 1
    void RefreshLevel()
    {
        float w = 0f;
        foreach (LossItem i in BuildPool(99)) w += i.w;
        RefluxRun.ShownLevel = Mathf.Clamp(1 + Mathf.RoundToInt(39f * Mathf.Clamp01(w / initialWeight)), 1, 40);
    }

    // ================================================================= 「역류의 끝」 · 처음의 슬라임
    IEnumerator Naked()
    {
        nakedStarted = true;
        // 끝까지 쥔 것: 맨몸 직전까지 남은 항목 상위 2개 (그 판의 개성 한 줄)
        List<LossItem> left = BuildPool(99);
        left.Sort((a, b) => b.w.CompareTo(a.w));
        keptNames.Clear();
        for (int i = 0; i < left.Count && i < 2; i++) keptNames.Add(left[i].name);
        RefluxRun.Kept = string.Join(" · ", keptNames);
        // 처치 연출(결계 · 소용돌이)이 끝난 뒤
        //   2.0.1~ 연출 · 결계 · 처치 연출 대기는 NakedMaxWait 초까지 (어느 하나가 남는 고장에도 판이 멈추지 않게) · 일시정지 · 창은 시간이 멈춰 그대로 기다림
        float waitFrom = Time.time;
        while (Time.timeScale == 0f || ChoiceUI.Open
               || ((PlayerController.Cinematic || BossUltimate.Active || bosss.Dying > 0) && Time.time - waitFrom < NakedMaxWait)) yield return null;
        Notice.Big(Loc.T("reflux.end.title"), Loc.T("reflux.end.sub"), NoticeIcon.Warn, 3.5f, NoticeLevel.Urgent);
        Hostile.Play("evolve", 0.7f, 0.5f);
        yield return new WaitForSeconds(1.2f);
        StripAll();
        yield return new WaitForSeconds(2.2f);
        waitFrom = Time.time;
        while (Time.timeScale == 0f || (PlayerController.Cinematic && Time.time - waitFrom < NakedMaxWait)) yield return null;
        SpawnSlime();
    }

    // 남은 모든 것을 잃음: 카드 · 갈래 · 무기 진화 · 축복 (필살기 게이지는 그대로)
    void StripAll()
    {
        shop.RefluxStripCards();
        while (sp.RefluxTopTier > 0) sp.UndoEvolution();
        for (int b = 0; b < sp.blessing.Length; b++) for (int guard = 0; guard < 99 && sp.blessing[b] > 0; guard++) sp.RefluxBless(b, -1);      // 2.0.0~ 검토: 횟수 상한 (RefluxBless 가 값을 못 줄이는 경우에도 멈추지 않게)
        RefluxRun.Naked = true;
        RefluxRun.ShownLevel = 1;
        if (player != null) player.PlayerHealth = Mathf.Min(player.PlayerMaxHealth, Mathf.Max(player.PlayerHealth, player.PlayerMaxHealth * 0.6f));
        Fx.Spawn("fx_shock", player != null ? player.transform.position : Vector3.zero, 12f, new Color(0.8f, 0.8f, 0.85f, 0.7f), 16f);
    }

    // 1장 첫 적 슬라임을 3배로 키운 「처음의 슬라임」: 체력 = 기본 × 40 × (가상 0분의 체력 배율) · 접촉 피해 ×2 · 10초마다 작은 슬라임 6마리
    void SpawnSlime()
    {
        GameObject[] list = spawner != null ? spawner.Stage.enemies : null;
        slimePrefab = list != null && list.Length > 0 ? list[0] : null;
        if (slimePrefab == null) { Clear(); return; }                 // 견본이 없으면 바로 클리어 (멈추지 않게)
        Vector3 pp = player != null ? player.transform.position : Vector3.zero;
        Vector3 at = Hostile.ClampArena(pp + (Vector3)(Random.insideUnitCircle.normalized * 8f));
        GameObject go = Instantiate(slimePrefab, at, Quaternion.identity);
        go.name = "FirstSlime";
        go.transform.localScale *= 3f;
        spawner.spawnedEnemys++;
        slime = go.GetComponent<EnermyController>();
        if (slime == null) { Destroy(go); Clear(); return; }
        int baseHp = slimePrefab.GetComponent<EnermyController>().setEnemyHP;
        RefluxRun.VirtualOverride = 0f;
        float hpMul = GameMode.EnemyHpMul;
        RefluxRun.VirtualOverride = -1f;
        slime.difficultyApplied = true;                    // 체력은 아래서 직접 정함
        slime.setEnemyHP = Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(1e9f, baseHp * SlimeHpMul * hpMul)));
        slime.speed *= GameMode.EnemySpeedMul;
        slime.contactDamage *= 2f;
        slime.expReward = 0;
        slime.coinDrop = 0;
        slime.survivesContact = true;
        slime.knockBackTaken *= 0.3f;
        slime.onKilled += OnSlimeKilled;
        slimeSpawned = true;
        slimeAt = Time.time;
        lastHp = player != null ? player.PlayerHealth : 0f;
        RefluxRun.NakedHit = false;
        BuildBar();
        Notice.Big(Loc.T("reflux.slime.arrive"), NoticeIcon.Boss, 2.5f, NoticeLevel.Urgent);
        Hostile.Play("roar", 1f, 1.2f);
        StartCoroutine(SlimeBirth());
    }

    IEnumerator SlimeBirth()
    {
        while (slime != null && !slime.IsDead && !finished)
        {
            yield return new WaitForSeconds(10f);
            if (slime == null || slime.IsDead || finished) yield break;
            if (Time.timeScale == 0f) continue;
            for (int i = 0; i < 6; i++)
            {
                if (spawner.spawnedEnemys >= 40) break;
                Vector3 at = Hostile.ClampArena(slime.transform.position + (Vector3)(Random.insideUnitCircle.normalized * Random.Range(2f, 4f)));
                Instantiate(slimePrefab, at, Quaternion.identity);
                spawner.spawnedEnemys++;
            }
            Fx.Spawn("fx_smoke", slime.transform.position, 4f, new Color(0.7f, 1f, 0.7f), 18f);
        }
    }

    void OnSlimeKilled()
    {
        if (finished) return;
        RefluxRun.BossCount = RefluxRun.BossTotal;
        Clear();
    }

    // ================================================================= 클리어
    void Clear()
    {
        if (finished) return;
        finished = true;
        RefluxRun.Cleared = true;
        RefluxRun.ClearSeconds = GameMode.EndlessSeconds;
        RefluxRun.BossCount = RefluxRun.BossTotal;
        if (barRoot != null) barRoot.SetActive(false);
        SteamAchievements.Unlock(SteamAchievements.RefluxClear);
        if (RefluxRun.ClearSeconds < 480f) SteamAchievements.Unlock(SteamAchievements.RefluxFast);
        // 2.0.1~ 맨몸 무피격은 처음의 슬라임이 실제로 나왔던 판만 (견본이 없어 바로 클리어한 판은 공짜로 풀렸음)
        //   판정 = 슬라임이 나온 뒤 어떤 피해도 받지 않음 (Steam 업적 문구 「한 대도 맞지 않고」와 같음 · 계약 문구도 맞춤)
        bool nakedClean = slimeSpawned && RefluxRun.Naked && !RefluxRun.NakedHit;
        if (nakedClean) SteamAchievements.Unlock(SteamAchievements.RefluxNaked);
        RunCounters.Max("reflux.cleared", 1);
        RunCounters.Max("reflux.spirit", RefluxRun.SpiritLeft);
        if (nakedClean) RunCounters.Max("reflux.nakedclean", 1);
        // 처치 포인트는 무한 규칙 그대로 · 첫 클리어 3,000P · 그 뒤 클리어마다 800P
        if (!GameInput.Auto && !Application.isBatchMode) Point.Bonus(RefluxRun.EverClearedReflux ? 800 : 3000);
        RunStats.MarkCleared();
        Notice.Big(Loc.T("reflux.clear.title"), NoticeIcon.Star, 5f, NoticeLevel.Important);
        Hostile.Play("chime", 1f, 1f);
        if (spawner != null) { spawner.spawningEnabled = false; spawner.ClearMobs(); }
        if (player != null) player.GrantInvincibility(30f);
        // 2.0.1~ 기록 · 순위 제출은 지금 바로 (5초 안에 「다시 시작」하면 BeforeGameScene 이 판 상태를 비워 제출이 빠졌음)
        RunStats.CaptureForResult();
        EndlessMode.EndRunNow();
        StartCoroutine(Leave());
    }

    IEnumerator Leave()
    {
        yield return new WaitForSecondsRealtime(5f);
        SceneFlow.Go("GameOver");
    }

    // ================================================================= 처음의 슬라임 체력바 (보스바 대신 — 보스가 아니라 적이라)
    void BuildBar()
    {
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        UIKit.EnsureStyle();
        RectTransform r = UIKit.Rect("FirstSlimeBar", canvas.transform, Vector2.zero, new Vector2(620f, 30f));
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -112f);
        barRoot = r.gameObject;
        Image bg = barRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);
        bg.raycastTarget = false;
        RectTransform f = UIKit.Rect("Fill", r, Vector2.zero, new Vector2(612f, 22f));
        barFill = f.gameObject.AddComponent<Image>();
        barFill.color = new Color(0.55f, 0.9f, 0.5f);
        barFill.raycastTarget = false;
        f.anchorMin = new Vector2(0f, 0.5f);
        f.anchorMax = new Vector2(0f, 0.5f);
        f.pivot = new Vector2(0f, 0.5f);
        f.anchoredPosition = new Vector2(4f, 0f);
        TMPro.TMP_Text label = UIKit.Text(r, Loc.T("reflux.slime.name"), 20f, new Color(0.95f, 0.98f, 0.9f), Vector2.zero, new Vector2(600f, 28f));
        label.raycastTarget = false;
        UiScaler.Isolate(barRoot);
    }

    void UpdateBar()
    {
        if (barFill == null || slime == null) return;
        float frac = slime.setEnemyHP > 0 ? Mathf.Clamp01(slime.EnemyHealth / slime.setEnemyHP) : 0f;
        RectTransform f = (RectTransform)barFill.transform;
        f.sizeDelta = new Vector2(612f * frac, 22f);
    }
}
