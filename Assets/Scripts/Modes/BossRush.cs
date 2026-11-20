using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// 2.0.6~ 보스 러시 (docs/plans/2026-11-20-big-update/plan.md C): 지금 고른 영웅으로 보스 다섯을 차례로 — 리치 왕 → 지옥의 군주 → 킹 슬라임 → 거울의 군주 → 개발자
//   일일 도전 패턴 (GameMode.Override = Endless) · 일정만 BossRushDirector 가 대신 (EndlessMode 는 자기 보스 · 15:00 일정을 끔)
//   보스를 잡을 때마다 무한 모드처럼 무기 진화 · 전리품 + 체력 30% 회복 · 다음 보스는 처치 연출 · 진화 창이 끝나고 12초 뒤
//   기록 = 다섯을 모두 쓰러뜨린 시간 (짧을수록 좋음 · 순위표 BOSSRUSH_BEST 오름차순) · 이어하기 없음 · 해금 = 보통 클리어
public static class BossRushRun
{
    public static bool Active { get; private set; }
    public static bool Unlocked => !Demo.On && GameMode.HasCleared(Difficulty.Normal);
    public const int BossTotal = 5;

    public const string BestKey = "rush.best";
    const string ClearedKey = "rush.cleared";
    public static float Best => Prefs.GetFloat(BestKey, 0f);
    public static bool EverCleared => Prefs.GetInt(ClearedKey, 0) == 1;

    public static bool Cleared { get; internal set; }
    public static float ClearSeconds { get; internal set; }
    public static int BossCount { get; internal set; }
    public static BossRushDirector Director { get; internal set; }

    static bool fresh;

    public static void Begin()
    {
        if (!Unlocked) return;
        ModeRun.EndAll();
        ResetRunState();
        fresh = true;
        Active = true;
        GameMode.Override = Difficulty.Endless;
        CharacterData.Override = CharacterData.Selected;
        SceneFlow.Go("GameScene");
    }

    // 「다시 시작」은 Begin 을 거치지 않고 GameScene 을 다시 부름 (RefluxRun 과 같은 이유) → 판 상태를 새로
    internal static void BeforeGameScene()
    {
        if (!Active) return;
        if (fresh) { fresh = false; return; }
        ResetRunState();
    }

    public static void End()
    {
        if (!Active) return;
        Active = false;
        fresh = false;
        GameMode.Override = null;
        CharacterData.Override = null;
        Director = null;
    }

    static void ResetRunState()
    {
        Cleared = false;
        ClearSeconds = 0f;
        BossCount = 0;
        LastPrevBest = 0f;
        LastNewRecord = false;
        Director = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() => SceneManager.sceneLoaded += (s, m) => { if (s.name == "MainMenu") End(); };

    public static void OnBossDown() { if (Director != null) Director.BossDown(); }

    public static string HudLine(float seconds) =>
        Loc.T("rush.name") + "  " + EndlessMode.Clock(seconds) + "   " + Loc.T("rush.hud.boss").Replace("{0}", Mathf.Min(BossCount, BossTotal).ToString()).Replace("{1}", BossTotal.ToString())
        + (Best > 0f ? "   " + Loc.T("최고 ") + EndlessMode.Clock(Best) : "");

    // ================================================================= 판이 끝날 때 (EndlessMode.Finish)
    public static float LastPrevBest;
    public static bool LastNewRecord;

    public static void Submit()
    {
        LastPrevBest = Best;
        LastNewRecord = false;
        DailyChallenge.LastBoard = "";
        DailyChallenge.LastRank = DailyChallenge.LastTotal = 0;
        if (!Cleared || GameInput.Auto || Application.isBatchMode || ClearSeconds < 1f) return;
        float t = ClearSeconds;
        if (LastPrevBest <= 0f || t < LastPrevBest) { Prefs.SetFloat(BestKey, t); LastNewRecord = true; }
        Prefs.SetInt(ClearedKey, 1);
        Prefs.Save();
        // 자세한 값: 0 영웅 · 1 보스 수 · 2 레벨 · 3 처치 수 · 4 판 버전 (무한 순위표와 같은 칸)
        PlayerController p = Hostile.Player;
        int[] details = { (int)CharacterData.Selected, BossTotal, p != null ? p.level : -1, RunStats.Kills, SteamLeaderboards.VersionCode() };
        DailyChallenge.LastBoard = SteamLeaderboards.BossRush;
        SteamLeaderboards.Upload(SteamLeaderboards.BossRush, Mathf.RoundToInt(t), details, (rank, total) =>
        {
            if (DailyChallenge.LastBoard != SteamLeaderboards.BossRush) return;
            DailyChallenge.LastRank = rank;
            DailyChallenge.LastTotal = total;
        });
    }

    // 결과 화면 세 줄 (MenuExtras.InstallGameOver)
    public static string ResultText(float seconds)
    {
        string head = Loc.T("rush.name") + " · " + Loc.T(CharacterData.Def(CharacterData.Selected).name);
        string best = Loc.T("reflux.result.best") + " ";
        if (!Cleared)
            return head + "\n" + Loc.T("rush.result.fail").Replace("{0}", BossCount.ToString()).Replace("{1}", BossTotal.ToString()).Replace("{2}", EndlessMode.Clock(seconds))
                 + "\n" + best + (Best > 0f ? EndlessMode.Clock(Best) : "-");
        string line3 = LastNewRecord
            ? best + (LastPrevBest > 0f ? EndlessMode.Clock(LastPrevBest) + " → " : "") + EndlessMode.Clock(ClearSeconds) + " <color=#F5D478>(" + Loc.T("relay.result.newrecord") + ")</color>"
            : best + EndlessMode.Clock(Best);
        return head + "\n" + Loc.T("reflux.result.clear") + " " + EndlessMode.Clock(ClearSeconds) + "\n" + line3;
    }
}

// ===================================================================== 판 안 감독 (StageManager 오브젝트에 붙음 · EndlessMode.Init)
public class BossRushDirector : MonoBehaviour
{
    // 종류 -1 = 개발자 · 0 리치 왕 · 1 지옥의 군주 · 2 킹 슬라임 · 3 거울의 군주 (EndlessMode.SpawnBossKind)
    static readonly int[] Kinds = { 0, 1, 2, 3, -1 };
    static readonly float[] HpMul = { 1f, 0.5f, 1f, 1f, 1f };          // 지옥의 군주 0.5 (기본 체력이 리치 왕의 5배 · 역류와 같은 이유)
    const float FirstAt = 8f, Rest = 12f, BossHeal = 0.3f;

    EndlessMode em;
    StageManager sm;
    readonly bosss[] tracked = new bosss[Kinds.Length];
    int next;
    float readyAt;          // 다음 보스를 불러도 되는 시각 (실제 초 · GameMode.EndlessSeconds)
    bool waitingRest, finished;

    public void Begin(EndlessMode endless, StageManager stages)
    {
        em = endless;
        sm = stages;
        BossRushRun.Director = this;
        readyAt = FirstAt;
        Notice.Big(Loc.T("rush.banner"), Loc.T("rush.banner.sub"), NoticeIcon.Boss, 3f, NoticeLevel.Important);
    }

    void OnDestroy() { if (BossRushRun.Director == this) BossRushRun.Director = null; }

    bool Calm()
    {
        PlayerController p = Hostile.Player;
        return Time.timeScale > 0f && sm != null && !sm.IsMenuOpen && !sm.IsEvolving && p != null && !p.IsDying
            && !BossUltimate.Active && !BossUltimate.ErosionPending && !PlayerController.Cinematic && bosss.Dying == 0;
    }

    void Update()
    {
        if (finished || em == null) return;
        float t = GameMode.EndlessSeconds;
        bool calm = Calm();
        // 앞 보스가 쓰러지고 처치 연출 · 진화 창이 끝난 순간부터 쉼 시작
        if (waitingRest && calm) { waitingRest = false; readyAt = t + Rest; }
        if (next < Kinds.Length && !waitingRest && calm && bosss.LiveCount == 0 && t >= readyAt) Spawn();
        if (next >= Kinds.Length && BossRushRun.BossCount >= BossRushRun.BossTotal && calm) Clear();
    }

    void Spawn()
    {
        int i = next++;
        bosss b = Kinds[i] < 0 ? em.SpawnDeveloperReflux(HpMul[i]) : em.SpawnBossKind(Kinds[i], HpMul[i]);
        tracked[i] = b;
        if (Kinds[i] < 0 && b != null)
        {
            Notice.Big(Loc.T("최종 보스 「개발자」가 강림한다!"), NoticeIcon.Boss, 3f, NoticeLevel.Urgent);
            Hostile.Play("roar", 1f, 0.45f);
        }
        if (b == null) { BossRushRun.BossCount++; waitingRest = true; }      // 견본이 없으면 건너뜀 (멈추지 않게)
    }

    // EndlessMode.OnBossDefeated → BossRushRun.OnBossDown (진화 · 전리품은 무한 모드 길 그대로)
    public void BossDown()
    {
        BossRushRun.BossCount = Mathf.Min(BossRushRun.BossCount + 1, BossRushRun.BossTotal);
        waitingRest = true;
        PlayerController p = Hostile.Player;
        if (p != null && !p.IsDying) p.PlayerHealth = Mathf.Min(p.PlayerMaxHealth, p.PlayerHealth + p.PlayerMaxHealth * BossHeal);
    }

    void Clear()
    {
        finished = true;
        BossRushRun.Cleared = true;
        BossRushRun.ClearSeconds = GameMode.EndlessSeconds;
        if (!GameInput.Auto && !Application.isBatchMode) Point.Bonus(BossRushRun.EverCleared ? 1000 : 5000);       // 첫 클리어 5,000P · 그 뒤 1,000P
        RunStats.MarkCleared();
        Notice.Big(Loc.T("rush.clear.title"), EndlessMode.Clock(BossRushRun.ClearSeconds), NoticeIcon.Star, 5f, NoticeLevel.Important);
        Hostile.Play("chime", 1f, 1f);
        if (sm != null && sm.spawner != null) { sm.spawner.spawningEnabled = false; sm.spawner.ClearMobs(); }
        PlayerController p = Hostile.Player;
        if (p != null) p.GrantInvincibility(30f);
        RunStats.CaptureForResult();
        EndlessMode.EndRunNow();
        StartCoroutine(Leave());
    }

    IEnumerator Leave()
    {
        yield return new WaitForSecondsRealtime(5f);
        SceneFlow.Go("GameOver");
    }
}
