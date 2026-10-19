using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// 자동 플레이 밸런스 측정 (tools/autoplay/run.ps1 이 프로젝트 복사본의 Assets/Scripts/Autoplay 에만 넣음 · 원본 저장소 게임 코드에는 없음)
// 한 번의 Unity 실행에서 무한 모드를 여러 판 연달아: 판마다 GameScene 을 새로 불러 AutoplayBot 이 끝까지 플레이
// 설정은 환경 변수 (run.ps1 이 채움):
//   AP_OUT 결과 폴더 · AP_HERO 영웅 · AP_PATH1 · AP_PATH2 (0~4 · 갈래 키 · random · single) · AP_RUNS · AP_SKILL (0~1)
//   AP_STEP 게임 한 프레임 길이(초, captureDeltaTime) · AP_MAXMIN 판 상한(게임 분) · AP_SEED 첫 시드 · AP_NODEATH 1 이면 쓰러져도 체력을 채우고 계속 (사망 횟수만 셈)
//   AP_BEYOND 한계 돌파 뒤 더 버틸 게임 초 (0 = 엄마 처치 즉시 끝)
// 결과: AP_OUT/results.csv (판마다 한 줄, 이어 붙임) · AP_OUT/errors_<hero>.txt (예외 · 오류 전체) · AP_OUT/progress.log
public static class AutoplayRunner
{
    public static string Out;
    public static string Hero = "Gunner";
    public static string Path1 = "random", Path2 = "random";
    public static int Runs = 1, Seed = 1000;
    public static float Skill = 0.5f, Step = 1f / 30f, MaxMinutes = 45f, BeyondSeconds = 0f;
    public static bool NoDeath;

    // 판 하나의 기록
    public class RunRecord
    {
        public string hero, path1 = "", path2 = "", pathTiers = "", cross = "";
        public float skill;
        public int seed;
        public string result = "";
        public float endSec, devSummonSec = -1f, devKillSec = -1f, momSummonSec = -1f, momKillSec = -1f, finalStartSec = -1f, fourBossSec = -1f;
        public int bosses, level, deaths, kills, lastStandUsed;
        public float taken, dealt;
        public string topTaken = "", topDealt = "";
        public int exceptions, errors;
        public readonly List<string> firstErrors = new List<string>();
        public float realSec;
        public string stuck = "";
        public int unknownWindows;
    }

    static RunRecord cur;
    static StreamWriter progress, errLog;
    static readonly Dictionary<string, int> errSeen = new Dictionary<string, int>();
    static int errTotal;

    static string Env(string k, string d) { string v = Environment.GetEnvironmentVariable(k); return string.IsNullOrEmpty(v) ? d : v; }
    static float EnvF(string k, float d) => float.TryParse(Env(k, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : d;
    static int EnvI(string k, int d) => int.TryParse(Env(k, ""), out int v) ? v : d;
    static string Inv(float v, string f = "0.0") => v.ToString(f, CultureInfo.InvariantCulture);

    public static void Log(string s)
    {
        string line = DateTime.Now.ToString("HH:mm:ss") + " " + s;
        Debug.Log("[AP] " + line);
        try { progress?.WriteLine(line); progress?.Flush(); } catch { }
    }

    // 게임에서 나온 예외 · 오류 (봇 자신이 낸 [AP] 줄은 제외) — 모든 줄을 errors 파일에 · 같은 첫 줄은 처음 3번만 스택까지
    static void OnLog(string msg, string stack, LogType type)
    {
        if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
        if (msg != null && msg.StartsWith("[AP]")) return;
        string first = (msg ?? "").Split('\n')[0];
        // 배치 모드에서만 나는 그래픽 · 오디오 장치 경고성 오류는 따로 셈하지 않음
        if (first.Contains("FMOD") || first.Contains("audio") && first.Contains("device")) return;
        errTotal++;
        if (cur != null)
        {
            if (type == LogType.Exception) cur.exceptions++; else cur.errors++;
            if (cur.firstErrors.Count < 5 && !cur.firstErrors.Contains(first)) cur.firstErrors.Add(first);
        }
        errSeen.TryGetValue(first, out int n);
        errSeen[first] = n + 1;
        try
        {
            if (n < 3)
            {
                string at = cur != null ? cur.hero + " seed " + cur.seed + " t=" + Inv(GameMode.EndlessSeconds, "0") : "-";
                errLog?.WriteLine("[" + at + "] " + type + ": " + msg);
                if (!string.IsNullOrEmpty(stack)) errLog?.WriteLine(stack.TrimEnd());
                errLog?.WriteLine();
                errLog?.Flush();
            }
        }
        catch { }
    }

    public static IEnumerator Run()
    {
        Out = Env("AP_OUT", Path.Combine(Application.temporaryCachePath, "Autoplay"));
        Hero = Env("AP_HERO", "Gunner");
        Path1 = Env("AP_PATH1", "random");
        Path2 = Env("AP_PATH2", "random");
        Runs = Mathf.Max(1, EnvI("AP_RUNS", 1));
        Seed = EnvI("AP_SEED", 1000);
        Skill = Mathf.Clamp01(EnvF("AP_SKILL", 0.5f));
        Step = Mathf.Clamp(EnvF("AP_STEP", 1f / 30f), 0.005f, 0.2f);
        MaxMinutes = EnvF("AP_MAXMIN", 45f);
        BeyondSeconds = EnvF("AP_BEYOND", 0f);
        NoDeath = Env("AP_NODEATH", "0") == "1";
        Directory.CreateDirectory(Out);
        progress = new StreamWriter(Path.Combine(Out, "progress.log"), true, new UTF8Encoding(false));
        errLog = new StreamWriter(Path.Combine(Out, "errors.txt"), true, new UTF8Encoding(false));
        Application.logMessageReceived += OnLog;
        Log("시작 " + Hero + " 갈래 " + Path1 + "/" + Path2 + " · " + Runs + "판 · 실력 " + Inv(Skill, "0.00") + " · 한 프레임 " + Inv(Step, "0.000") + "초 · 상한 " + MaxMinutes + "분" + (NoDeath ? " · 불사(사망 횟수만 셈)" : "") + " · 게임 " + Application.version);

        GameInput.Auto = true;
        AudioListener.volume = 0f;
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        AutoplayMeta.SetupVeteranAccount();

        GameObject host = new GameObject("AutoplayHost");
        UnityEngine.Object.DontDestroyOnLoad(host);

        // 계획: AP_PLAN "영웅|갈래1|갈래2|시드;..." (없으면 AP_HERO 로 Runs 판) · AP_SKIP 앞에서 이미 끝낸 판 수 (Unity 를 다시 켰을 때)
        var plan = new List<string[]>();
        foreach (string item in Env("AP_PLAN", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] c = item.Split('|');
            if (c.Length >= 4) plan.Add(c);
        }
        if (plan.Count == 0) for (int i = 0; i < Runs; i++) plan.Add(new[] { Hero, Path1, Path2, (Seed + i).ToString() });
        int skip = EnvI("AP_SKIP", 0);
        Log("계획 " + plan.Count + "판 (앞 " + skip + "판은 이미 끝남)");
        for (int run = skip; run < plan.Count; run++)
        {
            Hero = plan[run][0]; Path1 = plan[run][1]; Path2 = plan[run][2];
            int seed = int.TryParse(plan[run][3], out int sd) ? sd : Seed + run;
            cur = new RunRecord { hero = Hero, skill = Skill, seed = seed };
            Log("==== 판 " + (run + 1) + "/" + plan.Count + " · " + Hero + " 갈래 " + Path1 + "/" + Path2 + " · 시드 " + seed);
            float realStart = Time.realtimeSinceStartup;
            IEnumerator one = null;
            try { one = OneRun(host, seed); } catch (Exception e) { Log("판 시작 실패 " + e); }
            // 판 하나를 돌리며 봇 쪽 예외는 삼키고 기록 (다음 판은 계속)
            while (one != null)
            {
                bool moved;
                object y = null;
                try { moved = one.MoveNext(); if (moved) y = one.Current; }
                catch (Exception e) { Log("봇 예외 " + e); cur.result = cur.result.Length > 0 ? cur.result : "봇오류"; cur.firstErrors.Add("BOT: " + e.Message); break; }
                if (!moved) break;
                yield return y;
            }
            cur.realSec = Time.realtimeSinceStartup - realStart;
            WriteRow(cur);
            Log("판 끝 " + (run + 1) + "/" + plan.Count + " · " + cur.result + " · " + EndlessMode.Clock(cur.endSec) + " · 개발자 " + Clock(cur.devKillSec) + " · 엄마 " + Clock(cur.momKillSec) + " · 사망 " + cur.deaths + " · 예외 " + cur.exceptions + " · 실제 " + Inv(cur.realSec / 60f, "0.0") + "분");
            cur = null;
            // 다음 판 전에 정리
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            GameInput.AutoMove = Vector2.zero; GameInput.AutoFire = false; GameInput.AutoUlt = false;
        }

        Log("모두 끝 · 오류 · 예외 합계 " + errTotal + " (종류 " + errSeen.Count + ")");
        try
        {
            errLog?.WriteLine("==== 이번 실행 종류별 횟수");
            foreach (var kv in errSeen.OrderByDescending(k => k.Value)) errLog?.WriteLine(kv.Value + "\t" + kv.Key);
            errLog?.WriteLine();
        }
        catch { }
        Application.logMessageReceived -= OnLog;
        GameInput.Auto = false;
        Time.captureDeltaTime = 0f;
        progress?.Close(); progress = null;
        errLog?.Close(); errLog = null;
        // 테스트가 바로 끝나면 결과 저장이 늦을 수 있어 한 프레임
        yield return null;
    }

    static string Clock(float s) => s < 0f ? "-" : EndlessMode.Clock(s);

    // ================================================================= 판 하나
    static IEnumerator OneRun(GameObject host, int seed)
    {
        UnityEngine.Random.InitState(seed);
        Time.captureDeltaTime = 0f;
        Time.timeScale = 1f;
        if (!Enum.TryParse(Hero, true, out CharacterId id)) throw new Exception("모르는 영웅 " + Hero);
        CharacterData.Override = id;
        GameMode.Override = Difficulty.Endless;
        AutoplayMeta.BeforeRun(id);
        SceneManager.LoadScene("GameScene");
        // 씬이 열리고 플레이어 · 스테이지가 생길 때까지
        for (int i = 0; i < 600 && (SceneManager.GetActiveScene().name != "GameScene" || Hostile.Player == null || StageManager.Instance == null); i++) yield return null;
        UnityEngine.Random.InitState(seed);     // 씬 시작에서 쓴 무작위와 상관없이 판마다 같은 시작
        AutoplayBot bot = host.GetComponent<AutoplayBot>();
        if (bot != null) UnityEngine.Object.DestroyImmediate(bot);
        bot = host.AddComponent<AutoplayBot>();
        bot.Begin(cur, id, Path1, Path2, Skill, seed);
        AutoplayHooks.Begin(cur);
        Time.captureDeltaTime = Step;

        float lastGame = -1f, lastMoveReal = Time.realtimeSinceStartup, nextBeat = Time.realtimeSinceStartup + 20f;
        float beyondAt = -1f;
        while (true)
        {
            yield return null;
            string scene = SceneManager.GetActiveScene().name;
            EndlessMode em = EndlessMode.Instance;
            float s = GameMode.EndlessSeconds;
            if (em != null) { cur.endSec = Mathf.Max(cur.endSec, s); bot.Track(em, s); }
            if (scene != "GameScene" || (Hostile.Player == null && em == null))
            {
                if (cur.result.Length == 0) cur.result = cur.momKillSec >= 0f ? "클리어" : "사망";
                break;
            }
            if (em != null && em.Beyond)
            {
                if (beyondAt < 0f) beyondAt = s;
                if (s - beyondAt >= BeyondSeconds) { cur.result = "클리어"; break; }
            }
            if (s >= MaxMinutes * 60f) { cur.result = cur.devKillSec >= 0f ? "시간초과(개발자처치)" : "시간초과"; break; }
            // 멈춤 감시: 게임 시계가 흐르지 않으면 (창이 안 닫힘 · 컷신 등) 상태를 남기고, 3분이면 이 판을 버림
            if (s > lastGame + 0.01f || em == null) { lastGame = s; lastMoveReal = Time.realtimeSinceStartup; }
            else if (Time.realtimeSinceStartup - lastMoveReal > 60f && cur.stuck.Length == 0)
            {
                cur.stuck = bot.StateLine();
                Log("?? 게임 시계가 1분째 멈춤: " + cur.stuck);
            }
            if (Time.realtimeSinceStartup - lastMoveReal > 180f) { cur.result = "멈춤"; Log("XX 3분 멈춤 → 이 판을 버림: " + bot.StateLine()); break; }
            if (Time.realtimeSinceStartup >= nextBeat)
            {
                nextBeat = Time.realtimeSinceStartup + 20f;
                Log("… " + Hero + " " + EndlessMode.Clock(s) + " · " + bot.StateLine());
            }
        }
        // 판 정보 (씬이 바뀌기 전 · 또는 바뀐 뒤에도 남는 정적 값)
        Finalize(cur);
        AutoplayHooks.End();
        if (bot != null) UnityEngine.Object.Destroy(bot);
        Time.captureDeltaTime = 0f;
        // 아직 게임 씬이면 (클리어 · 시간초과 · 멈춤) 메인 메뉴로 나가 판을 정리 (RunStats 가 판을 닫음)
        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
            for (int i = 0; i < 30; i++) yield return null;
        }
        else for (int i = 0; i < 10; i++) yield return null;
    }

    static void Finalize(RunRecord r)
    {
        try
        {
            r.level = RunStats.Level;
            PlayerController p = Hostile.Player;
            if (p != null) r.level = Mathf.Max(r.level, p.level);
            r.kills = RunStats.Kills;
            r.taken = RunStats.DamageTaken.Values.Sum();
            r.dealt = RunStats.DamageDealt.Values.Sum();
            r.topTaken = Top(RunStats.DamageTaken, 4);
            r.topDealt = Top(RunStats.DamageDealt, 4);
            if (EndlessMode.Instance != null) r.bosses = EndlessMode.Instance.BossesDefeated;
            r.lastStandUsed = LastStand.Used;
            AutoplayBot.FillPaths(r);
        }
        catch (Exception e) { Log("기록 정리 실패 " + e.Message); }
    }

    static string Top(Dictionary<string, float> d, int n)
    {
        float total = Mathf.Max(1f, d.Values.Sum());
        return string.Join(" | ", d.OrderByDescending(k => k.Value).Take(n).Select(k => k.Key.Replace(",", " ") + " " + Mathf.RoundToInt(k.Value / total * 100f) + "%"));
    }

    // ================================================================= CSV
    const string Header = "time,hero,path1,path2,skill,seed,result,end_time,four_bosses,dev_summon,dev_kill,mom_summon,mom_kill,bosses,level,kills,deaths,laststand,taken,dealt,top_taken,top_dealt,path_tiers,cross,exceptions,errors,first_errors,stuck,unknown_windows,real_minutes,step,version";

    static void WriteRow(RunRecord r)
    {
        r.path1 = r.path1.Length > 0 ? r.path1 : Path1; r.path2 = r.path2.Length > 0 ? r.path2 : Path2;
        string path = Path.Combine(Out, "results.csv");
        bool fresh = !File.Exists(path);
        string Q(string s) => "\"" + (s ?? "").Replace("\"", "'").Replace("\n", " ").Replace("\r", " ") + "\"";
        string T(float s) => s < 0f ? "" : Inv(s, "0");
        string line = string.Join(",", new[]
        {
            DateTime.Now.ToString("yyyy-MM-dd HH:mm"), r.hero, r.path1, r.path2, Inv(r.skill, "0.00"), r.seed.ToString(), r.result,
            T(r.endSec), T(r.fourBossSec), T(r.devSummonSec), T(r.devKillSec), T(r.momSummonSec), T(r.momKillSec),
            r.bosses.ToString(), r.level.ToString(), r.kills.ToString(), r.deaths.ToString(), r.lastStandUsed.ToString(), Inv(r.taken, "0"), Inv(r.dealt, "0"),
            Q(r.topTaken), Q(r.topDealt), Q(r.pathTiers), Q(r.cross), r.exceptions.ToString(), r.errors.ToString(), Q(string.Join(" || ", r.firstErrors)),
            Q(r.stuck), r.unknownWindows.ToString(), Inv(r.realSec / 60f, "0.0"), Inv(Step, "0.000"), Application.version,
        });
        try { File.AppendAllText(path, (fresh ? Header + "\n" : "") + line + "\n", new UTF8Encoding(fresh)); }
        catch (Exception e) { Log("CSV 쓰기 실패 " + e.Message); }
    }
}
