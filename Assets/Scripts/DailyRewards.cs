using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 1.2~ 일일 도전 보상 · 문턱 (설계 3-4 ①)
//   하루 한 번 (그날 일일 도전 날짜 기준 · Prefs "daily.reward.<yyyyMMdd>" 비트): 5분 500P · 10분 1,000P · 15분 「완주」 2,000P (배율 없음)
//   15:00 은 이정표이고 판은 계속됨 — 그 순간 멈추고 「완주!」 창(「계속 버티기」 기본 · 「결과 보기」 = 포기하고 결과 보기와 같은 흐름)
//     순위표 점수는 버틴 초 그대로라 상한을 두지 않음 (완주자가 모두 900초 동점이 되지 않게)
//   완주 누적(하루 한 번 셈) 1 · 5 · 10 · 30회: 각인 「고요」 · 유물 「화염 장판」 · 칭호(10회는 커서 대신 칭호 「달력을 넘기는 자」) · 칭호 「매일 오는 자」
//   「오늘의 영웅」이 아직 못 산 영웅이면 완주할 때 그 영웅의 계약 보드 1칸을 미리 채움
//   연속 출석 3 · 7 · 30일: 1,000 · 3,000 · 10,000P — 출석 = 그날 일일 도전을 1분 이상 (UTC 날짜 · 하루 건너면 처음부터 · 시계를 되돌린 날은 셈하지 않음)
//   스팀이 없어도 보상은 받음 (순위만 없음) · 체험판에는 일일 도전이 없음
public static class DailyRewards
{
    public const float Attend = 60f, R5 = 300f, R10 = 600f, R15 = 900f;
    static readonly int[] RewardPoints = { 500, 1000, 2000 };
    static readonly float[] RewardAt = { R5, R10, R15 };
    static readonly string[] RewardKeys = { "daily.reward.5", "daily.reward.10", "daily.reward.15" };

    const string StreakKey = "daily.streak", LastKey = "daily.attend.last", FinishKey = "daily.finishes", BestStreakKey = "life.daily.streak.best";
    public static int Finishes => Prefs.GetInt(FinishKey, 0);
    public static bool CalmUnlocked => Finishes >= 1;
    public static bool RelicUnlocked => Finishes >= 5;

    static string Day => DailyChallenge.RunDayKey;
    static int Bits(string day) => Prefs.GetInt("daily.reward." + day, 0);
    public static bool GotToday(int i) => (Bits(DailyChallenge.Key(DailyChallenge.Today)) & (1 << i)) != 0;
    public static int[] Points => RewardPoints;

    // 지금 이어지는 연속 출석 (마지막 출석이 오늘 · 어제면 그 수, 아니면 0)
    public static int Streak
    {
        get
        {
            string last = Prefs.GetString(LastKey, "");
            DateTime today = DailyChallenge.Today;
            if (last == DailyChallenge.Key(today) || last == DailyChallenge.Key(today.AddDays(-1))) return Prefs.GetInt(StreakKey, 0);
            return 0;
        }
    }

    // 메인 메뉴: 하루 넘게 건넜으면 한 번 알림 (연속이 끊김)
    public static void MenuCheck()
    {
        if (Demo.On) return;
        string last = Prefs.GetString(LastKey, "");
        if (string.IsNullOrEmpty(last) || Prefs.GetInt(StreakKey, 0) <= 0 || Streak > 0) return;
        if (string.Compare(last, DailyChallenge.Key(DailyChallenge.Today), StringComparison.Ordinal) >= 0) return;     // 미래 날짜 (시계를 되돌림): 그대로
        MenuToast.RunLater(() => StreakBrokenLater(0));
    }

    // 1.5.9~ 끊김 알림이 게임을 켠 직후 시작 연출(TitleIntro)에 덮여 끝난 뒤 1초쯤만 보였음 → 연출이 걷힌 뒤 띄움
    //   0.25초마다 확인 · 30번(7.5초, 연출 상한 6초보다 김)이면 그냥 띄움 · 그 사이 판에 들어갔으면 저장도 하지 않음 (다음 메뉴에서 다시 알림)
    static void StreakBrokenLater(int tries)
    {
        if (SceneManager.GetActiveScene().name != "MainMenu") return;
        if (TitleIntro.Showing && tries < 30) { MenuToast.RunLater(() => StreakBrokenLater(tries + 1), 0.25f); return; }
        if (Prefs.GetInt(StreakKey, 0) <= 0 || Streak > 0) return;      // 이미 알렸음 (메뉴를 두 번 열어 둘이 기다린 경우)
        Prefs.SetInt(StreakKey, 0);
        Prefs.Save();
        Notice.Toast(NoticeIcon.Info, Loc.T("daily.streak.broken"), 4f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded += (s, m) =>
        {
            if (s.name == "GameScene" && DailyChallenge.Active && !Demo.On) new GameObject("DailyRewards").AddComponent<Runner>();
        };
    }

    class Runner : MonoBehaviour
    {
        bool attended, finishShown, ruleApplied;
        float tick;

        void Update()
        {
            if (!DailyChallenge.Active || RunCounters.Off) return;
            if (!ruleApplied) ruleApplied = DailyChallenge.ApplyPlayerRule();
            tick -= Time.unscaledDeltaTime;
            if (tick > 0f) return;
            tick = 0.25f;
            PlayerController p = Hostile.Player;
            if (p == null || p.IsDying || p.PlayerHealth <= 0f) return;
            float t = GameMode.EndlessSeconds;
            if (!attended && t >= Attend) { attended = true; MarkAttend(); }
            for (int i = 0; i < RewardAt.Length; i++)
            {
                if (t < RewardAt[i]) continue;
                int bits = Bits(Day);
                bool first = (bits & (1 << i)) == 0;
                if (first)
                {
                    Prefs.SetInt("daily.reward." + Day, bits | (1 << i));
                    Point.Bonus(RewardPoints[i], false);
                    if (i < 2) Notice.Big(Loc.T(RewardKeys[i]), NoticeIcon.Coin, 3f, NoticeLevel.Important);
                    if (i == 2) OnFinishFirst();
                    Prefs.Save();
                }
                if (i == 2 && !finishShown && Time.timeScale > 0f && !ChoiceUI.Open && !ESCmenu.IsOpen && !StoryDirector.Playing)
                {
                    finishShown = true;
                    FinishWindow(!first);
                }
            }
        }

        void MarkAttend()
        {
            string today = DailyChallenge.Key(DailyChallenge.Today);
            string last = Prefs.GetString(LastKey, "");
            if (string.Compare(last, today, StringComparison.Ordinal) >= 0) return;        // 오늘 이미 · 미래 날짜는 셈하지 않음
            int streak = last == DailyChallenge.Key(DailyChallenge.Today.AddDays(-1)) ? Prefs.GetInt(StreakKey, 0) + 1 : 1;
            Prefs.SetString(LastKey, today);
            Prefs.SetInt(StreakKey, streak);
            if (streak > Prefs.GetInt(BestStreakKey, 0)) Prefs.SetInt(BestStreakKey, streak);
            int pay = streak == 3 ? 1000 : streak == 7 ? 3000 : streak == 30 ? 10000 : 0;
            if (pay > 0)
            {
                Point.Bonus(pay, false);
                Notice.Big(Loc.T("daily.streak." + streak), NoticeIcon.Star, 3.5f, NoticeLevel.Important);
            }
            Prefs.Save();
        }

        // 그날 처음 15분: 완주 누적 · 누적 보상 · 오늘의 영웅 보드 칸
        void OnFinishFirst()
        {
            int n = Finishes + 1;
            Prefs.SetInt(FinishKey, n);
            Notice.Big(Loc.T("daily.reward.15"), NoticeIcon.Star, 3.5f, NoticeLevel.Important);
            if (n == 1) Notice.Big(Loc.T("daily.done.1"), NoticeIcon.Unlock, 4f, NoticeLevel.Important);
            else if (n == 5) Notice.Big(Loc.T("daily.done.5"), NoticeIcon.Relic, 4f, NoticeLevel.Important);
            else if (n == 10) Titles.Grant("daily.10", "title.daily10");
            else if (n == 30) Titles.Grant("daily.30", "title.daily30");
            CharacterId hero = CharacterData.Selected;
            if (!CharacterData.IsUnlocked(hero) && ContractBoard.GrantOne(hero))
                Notice.Big(Loc.T("daily.herocell"), NoticeIcon.Unlock, 4f, NoticeLevel.Important);
        }

        // 「완주!」 창: 멈춤 · 계속 버티기(기본) / 결과 보기
        void FinishWindow(bool already)
        {
            Canvas canvas = UIKit.HudCanvas();
            if (canvas == null) return;
            UIKit.EnsureStyle();
            float before = Time.timeScale;
            Time.timeScale = 0f;
            ChoiceUI.Open = true;               // 다른 고르기 창 · ESC 메뉴가 끼어들지 않게
            RectTransform win = UIKit.Modal(canvas.rootCanvas.transform, "DailyFinish", new Vector2(900f, 420f), out GameObject blocker);
            SoulTreeUI.OnTop(blocker, 640);
            UIKit.Text(win, "daily.reward.15", 44f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 140f), new Vector2(820f, 60f));
            TMP_Text body = UIKit.Text(win, already ? "daily.finish.again" : "daily.finish.body", 24f, new Color(0.93f, 0.9f, 0.84f), new Vector2(0f, 40f), new Vector2(800f, 110f));
            body.fontSizeMin = 16f;
            float readyAt = Time.unscaledTime + 0.6f;
            System.Action close = () =>
            {
                if (blocker != null) Destroy(blocker);
                ChoiceUI.Open = false;
                Time.timeScale = before > 0f ? before : 1f;
            };
            Button keep = UIKit.MakeButton(win, "daily.finish.keep", new Vector2(-180f, -120f), new Vector2(320f, 80f), () => { if (Time.unscaledTime >= readyAt) close(); }, 28f);
            keep.GetComponent<Image>().color = new Color(1f, 0.88f, 0.55f);
            UIKit.MakeButton(win, "daily.finish.result", new Vector2(180f, -120f), new Vector2(320f, 80f), () =>
            {
                if (Time.unscaledTime < readyAt) return;
                close();
                RunStats.GaveUp = true;
                RunStats.CaptureForResult();        // 결과 화면 「빌드 자세히 보기」 스냅샷 (ESC 포기와 같게)
                RunSave.Abandon();
                if (Herald.Enabled) { Herald.RunThen(true, () => SceneFlow.Go("GameOver")); return; }      // 망자의 전령 한 줄 뒤 결과 (ESC 포기와 같게)
                SceneFlow.Go("GameOver");           // 1.1.9~ 로딩 막 · 공격 고정 풀기 (다른 씬 전환과 같게)
            }, 28f);
            keep.gameObject.AddComponent<ResultPadFocus>();
            UIKit.EscCancel(blocker, () => close());
        }
    }

    // 일일 도전 창 아래 줄: 오늘 받은 보상 · 출석 · 바뀌는 시각
    public static string WindowLines()
    {
        if (Demo.On) return "";
        System.Text.StringBuilder b = new System.Text.StringBuilder();
        b.Append("<color=#F5D478>").Append(Loc.T("daily.today.title")).Append("</color>\n");
        string[] mins = { "5", "10", "15" };
        for (int i = 0; i < 3; i++)
        {
            string label = Loc.T("daily.today.row").Replace("{0}", mins[i]).Replace("{1}", RewardPoints[i].ToString("N0"));
            b.Append(GotToday(i) ? "<color=#7CFF9A>" + label + "  " + Loc.T("claim.done") + "</color>" : label).Append('\n');
        }
        b.Append("<color=#C9A2FF>").Append(Loc.T("daily.streak.label").Replace("{0}", Streak.ToString())).Append("</color>  ")
         .Append("<size=85%><color=#A89C86>").Append(Loc.T("daily.attend.how")).Append("</color></size>");
        return b.ToString();
    }

    // 「다음 도전까지」 옆: 날이 바뀌는 현지 시각 (UTC 자정)
    public static string ResetLocal() => Loc.T("daily.reset.local").Replace("{0}", DateTime.UtcNow.Date.AddDays(1).ToLocalTime().ToString("HH:mm"));
}
