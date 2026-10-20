using UnityEngine;

// 1.2~ 「한 판 더」 측정 (설계서 3-10): 판 하나가 아니라 「판과 판 사이」를 보는 숫자
//   balance_log.csv 끝에 여섯 칸 (RunStats.WriteBalanceLog 가 Row 를 붙임)
//     retry_kind     이 판을 어떻게 시작했나 — menu(메인 메뉴) · same(결과 화면 「같은 설정으로 다시」) · quick(결과 화면 [R])
//                    · hero(「캐릭터 바꿔 다시」로 메뉴를 거침) · quit(ESC 「다시 시작」으로 하던 판을 버림) · continue(이어하기) · tutorial
//     menu_gap_secs  지난 판이 끝난 순간 → 이 판 시작까지 (이번 실행의 첫 판은 빈 칸)
//     contracts_done 이번 판에 이룬 영혼 계약 수 (3-1, Contracts.Fulfil 이 ContractsDone 을 올림 · 없으면 0)
//     herald_shown   쓰러진 뒤 망자의 전령이 한 줄을 건넸는지 (3-2, Herald 가 한 줄을 띄울 때 HeraldShown 을 켬 · 없으면 0)
//     milestones     이번 판에 처음 넘긴 무한 이정표 "m5|m10|boss5" (EndlessMilestones)
//     points_run     이번 판에 얻은 포인트 (보너스 포함)
//   sessions.csv: 게임을 켠 시각 · 끈 시각 · 분 · 판 수 · 60초 안에 끝난 판 수 · 마지막 판 결과 · 버전 (끌 때 한 줄)
// 테스트 · 자동 플레이 · 트레일러 촬영 · 배치 모드는 기록하지 않음 (RunStats 와 같은 기준)
public static class RunLog
{
    //   2026-10-18~ 끝에 넷: weak_hits(빈틈 명중 수) · weak_share(빈틈으로 더한 피해 ÷ 빈틈이 붙은 보스 · 중간 보스에게 준 피해, WeakSpot)
    //                      · memory_trig · memory_break(기억하는 보스 발동 · 깨기 — 그 갈래가 RunCounters "bossmem.trig" · "bossmem.break" 로 셈)
    //   2026-10-19~ 끝에 다섯: path1 · path2(외길이면 single) · path_tiers · cross · cards_seq (빌드 갈래, Paths.LogRow)
    public const string Columns = "retry_kind,menu_gap_secs,contracts_done,herald_shown,milestones,points_run,weak_hits,weak_share,memory_trig,memory_break," + Paths.LogColumns;

    // 다른 갈래가 채우는 칸 (판이 시작될 때 0 으로)
    public static int ContractsDone;
    public static bool HeraldShown;

    // 다음 판을 시작하는 방식 힌트: 결과 화면 [R] 은 "quick", 「캐릭터 바꿔 다시」는 "hero" (다음 판이 시작되면 지움)
    public static string NextKind;

    static string retryKind = "", menuGap = "";
    public static bool TutorialStarted => retryKind == "tutorial";      // 1.4.7~ 이번 판이 튜토리얼로 시작됨 (Telemetry 가 보내지 않음 · 끝날 땐 TutorialRun.Active 가 이미 꺼졌을 수 있음)
    static int points;
    static float lastRunEnd = -1f;
    static string prevScene;

    // 세션
    static System.DateTime sessionStart = System.DateTime.Now;
    static int sessionRuns, sessionShort;
    static string lastResult = "";
    static bool quitHooked;

    static bool Recording => !GameInput.Auto && !GameInput.TrailerRunning && !Application.isBatchMode;

    // 켠 순간 (첫 씬 뒤): 끌 때 세션 한 줄을 쓰게 · 첫 씬 이름 (메인 메뉴에서 바로 끄는 세션도 남김)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        sessionStart = System.DateTime.Now;
        if (prevScene == null) prevScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        HookQuit();
    }

    // RunStats 가 씬이 열릴 때마다 (지난 판 줄을 쓴 바로 뒤) 부름 · endedRun: 방금 게임 씬을 떠났는지
    public static void OnScene(string scene, bool endedRun)
    {
        HookQuit();
        if (prevScene == null) prevScene = "MainMenu";          // 첫 씬은 이 갈래가 붙기 전에 열림
        if (endedRun) lastRunEnd = Time.realtimeSinceStartup;
        if (scene == "GameScene") BeginRun();
        prevScene = scene;
    }

    static void BeginRun()
    {
        if (RunSave.Pending != null) retryKind = "continue";
        else if (TutorialRun.Active) retryKind = "tutorial";
        else if (prevScene == "GameOver") retryKind = NextKind == "quick" ? "quick" : "same";
        else if (prevScene == "GameScene") retryKind = "quit";
        else retryKind = NextKind == "hero" ? "hero" : "menu";
        NextKind = null;
        menuGap = lastRunEnd >= 0f ? Mathf.Max(0f, Time.realtimeSinceStartup - lastRunEnd).ToString("0") : "";
        ContractsDone = 0;
        HeraldShown = false;
        points = 0;
        EndlessMilestones.ResetRun();
        WeakSpot.ResetRun();
    }

    public static void NotePoints(int runPoints) => points = Mathf.Max(points, runPoints);

    // balance_log 한 줄 끝 열 칸
    public static string Row()
    {
        int pts = Mathf.Max(points, CharacterData.RunPoints);
        WeakSpot.FlushAll();        // 빈틈 칸은 판 저장으로 되살아나는 카운터 (weak.hit · weak.bonus ÷ weak.dealt) — 이어한 판도 판 전체 · 기억 칸 · 계약 weak30 과 같은 기준
        return retryKind + "," + menuGap + "," + ContractsDone + "," + (HeraldShown ? 1 : 0) + "," + EndlessMilestones.RunCodes + "," + pts
             + "," + RunCounters.Get("weak.hit") + "," + WeakSpot.Share.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)
             + "," + RunCounters.Get("bossmem.trig") + "," + RunCounters.Get("bossmem.break")
             + "," + Paths.LogRow();
    }

    public static void OnRunLogged(string result, float seconds)
    {
        sessionRuns++;
        if (seconds < 60f) sessionShort++;
        lastResult = result;
    }

    // 끝에 칸만 더해진 머리줄이면 머리줄만 새것으로 바꿔 쓰고 true (옛 줄은 그대로 · 뒤 칸이 빔)
    public static bool UpgradeHeader(string path, string oldHeader, string newHeader)
    {
        if (string.IsNullOrEmpty(oldHeader) || !newHeader.StartsWith(oldHeader + ",")) return false;
        try
        {
            string[] lines = System.IO.File.ReadAllLines(path);
            if (lines.Length == 0) return false;
            lines[0] = newHeader;
            System.IO.File.WriteAllLines(path, lines, new System.Text.UTF8Encoding(true));
            return true;
        }
        catch (System.Exception ex) { Debug.LogWarning("balance log header: " + ex.Message); return false; }
    }

    // ================================================================= 세션 (sessions.csv, 끌 때 한 줄)
    public static string SessionsPath => System.IO.Path.Combine(Application.persistentDataPath, "sessions.csv");
    const string SessionHeader = "start,end,minutes,runs,runs_under_60s,last_result,version";

    static void HookQuit()
    {
        if (quitHooked) return;
        quitHooked = true;
        Application.quitting += WriteSession;
    }

    static void WriteSession()
    {
        if (!Recording) return;
        try
        {
            string path = SessionsPath;
            bool fresh = !System.IO.File.Exists(path);
            System.DateTime now = System.DateTime.Now;
            string line = sessionStart.ToString("yyyy-MM-dd HH:mm") + "," + now.ToString("yyyy-MM-dd HH:mm") + ","
                        + (now - sessionStart).TotalMinutes.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ","
                        + sessionRuns + "," + sessionShort + "," + lastResult + "," + Application.version;
            System.IO.File.AppendAllText(path, (fresh ? SessionHeader + "\n" : "") + line + "\n", new System.Text.UTF8Encoding(true));
        }
        catch (System.Exception ex) { Debug.LogWarning("sessions log: " + ex.Message); }
    }
}
