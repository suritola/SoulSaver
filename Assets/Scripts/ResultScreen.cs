using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 1.1~ 결과 화면(GameOver 씬) 「다음 목표」 막대 3줄 + 「같은 설정으로 다시」 · 「캐릭터 바꿔 다시」 (MenuExtras.InstallGameOver 가 부름)
// 1.0.3 의 다음 해금 글(캐릭터 하나 · 스킨 하나)을 후보 정렬 방식으로: 영웅 · 스킨까지 남은 P, 숙련, 업적 중 진행률이 높은 셋
// 90% 이상이면 막대가 금빛 + 「거의 다!」, 다 찼으면(살 수 있음) 초록
public static class ResultScreen
{
    // 「캐릭터 바꿔 다시」 → 메인 메뉴가 뜨면 캐릭터 창을 바로 엶 (MenuExtras.InstallMainMenu)
    static bool openHeroPick;
    public static bool ConsumeHeroPick()
    {
        bool on = openHeroPick;
        openHeroPick = false;
        return on;
    }

    // 누적 처치 · 무한 모드 20분 업적은 SteamManager 의 값을 그대로 (복사해 두면 한쪽만 바뀌어 어긋남)
    const string TotalKillsKey = SteamManager.TotalKillsKey;
    const int TotalKillsGoal = SteamManager.TotalKillsGoal;
    const float EndlessGoal = SteamManager.EndlessGoal;
    const float AlmostAt = 0.9f;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.93f, 0.9f, 0.84f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);
    static readonly Color Ready = new Color(0.49f, 1f, 0.6f);
    static readonly Color Fill = new Color(0.62f, 0.47f, 0.92f);

    struct Goal
    {
        public string label, detail;
        public float progress;      // 0 ~ 1
        public Goal(string label, float progress, string detail) { this.label = label; this.progress = Mathf.Clamp01(progress); this.detail = detail; }
    }

    // ================================================================= 다음 목표
    // 갈래마다(영웅 · 스킨 · 숙련 · 업적) 가장 가까운 것 하나씩 → 진행률 순으로 셋
    static List<Goal> Goals()
    {
        List<Goal> all = new List<Goal>();
        if (Demo.On || TutorialRun.Active) return all;
        int pts = CharacterData.Points;

        // 영웅: 조건을 채웠으면 남은 P, 아니면 잴 수 있는 조건만 (무한 모드 10분)
        Goal? hero = null;
        for (int i = 0; i < CharacterData.All.Length; i++)
        {
            CharacterId id = (CharacterId)i;
            CharacterDef d = CharacterData.Def(id);
            if (d.price <= 0 || CharacterData.IsUnlocked(id)) continue;
            string label = Loc.T("result.goal.hero").Replace("{0}", Loc.T(d.name));
            Goal g;
            if (!CharacterData.ConditionMet(id))
            {
                if (d.condition != CharUnlock.Endless10 || !GameMode.IsUnlocked(Difficulty.Endless)) continue;
                g = new Goal(label, EndlessMode.BestSeconds / 600f, Loc.T("최고 기록 ") + EndlessMode.Clock(EndlessMode.BestSeconds) + " / 10:00");
            }
            else g = new Goal(label, (float)pts / d.price, PointsLeft(d.price - pts));
            if (hero == null || g.progress > hero.Value.progress) hero = g;
        }
        if (hero != null) all.Add(hero.Value);

        // 스킨: 지금 영웅 · 모든 영웅 공용 중 가장 싼 것
        SkinDef cheap = null;
        foreach (SkinDef sk in SkinData.All)
        {
            if (SkinData.Owns(sk.id) || (sk.who >= 0 && sk.who != (int)CharacterData.Selected)) continue;
            if (cheap == null || sk.Price < cheap.Price) cheap = sk;
        }
        if (cheap != null) all.Add(new Goal(Loc.T("스킨") + " · " + Loc.T(cheap.name), (float)pts / cheap.Price, PointsLeft(cheap.Price - pts)));

        // 숙련: 지금 영웅 (이야기 모드를 클리어할 때마다 +1) · 무한 모드 · 일일 도전 판에서는 오르지 않으므로 빼기
        CharacterId me = CharacterData.Selected;
        int lv = Mastery.Level(me);
        if (lv < Mastery.Max && !GameMode.IsEndless && !DailyChallenge.Active)
            all.Add(new Goal(Loc.T("result.goal.mastery").Replace("{0}", Loc.T(CharacterData.Def(me).name)), (float)lv / Mastery.Max,
                             Mastery.Label(me) + " · " + Loc.T("result.goal.masteryhint")));

        // 업적: 누적 처치 · 무한 모드 20분 중 가까운 것
        Goal? ach = null;
        int kills = Prefs.GetInt(TotalKillsKey, 0);
        if (kills < TotalKillsGoal)
            ach = new Goal(Loc.T("result.goal.kills"), (float)kills / TotalKillsGoal, kills.ToString("N0") + " / " + TotalKillsGoal.ToString("N0"));
        float best = EndlessMode.BestSeconds;
        if (GameMode.IsUnlocked(Difficulty.Endless) && best < EndlessGoal)
        {
            Goal g = new Goal(Loc.T("result.goal.endless"), best / EndlessGoal, Loc.T("최고 기록 ") + EndlessMode.Clock(best) + " / 20:00");
            if (ach == null || g.progress > ach.Value.progress) ach = g;
        }
        if (ach != null) all.Add(ach.Value);

        all.Sort((a, b) => b.progress.CompareTo(a.progress));
        if (all.Count > 3) all.RemoveRange(3, all.Count - 3);
        return all;
    }

    static string PointsLeft(int left) =>
        left > 0 ? Loc.T("해금까지") + " " + left.ToString("N0") + " P" : "<color=#7CFF9A>" + Loc.T("result.goal.ready") + "</color>";

    // 결과 화면 왼쪽 위 (이번 판 결과 글 위, 480 폭)
    public static void BuildGoals(Transform canvas)
    {
        List<Goal> goals = Goals();
        if (goals.Count == 0) return;
        UIKit.EnsureStyle();
        const float RowH = 64f, HeadH = 34f, W = 480f;
        RectTransform panel = UIKit.Rect("NextGoals", canvas, Vector2.zero, new Vector2(W, HeadH + RowH * goals.Count));
        panel.anchorMin = panel.anchorMax = new Vector2(0f, 0.5f);
        panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = new Vector2(60f, 515f);
        TMP_Text head = UIKit.Text(panel, "result.goal.title", 24f, new Color(0.79f, 0.63f, 1f), Vector2.zero, new Vector2(W, HeadH), TextAlignmentOptions.Left);
        FixTop(head.rectTransform, -HeadH * 0.5f);

        List<(TMP_Text label, TMP_Text detail)> rows = new List<(TMP_Text, TMP_Text)>();
        for (int i = 0; i < goals.Count; i++)
        {
            Goal g = goals[i];
            float y = -HeadH - RowH * i - RowH * 0.5f;
            RectTransform row = UIKit.Rect("Goal", panel, Vector2.zero, new Vector2(W, RowH));
            FixTop(row, y);
            TMP_Text label = UIKit.Text(row, "", 20f, Parch, new Vector2(-W * 0.2f, 12f), new Vector2(W * 0.6f, 28f), TextAlignmentOptions.Left);
            TMP_Text detail = UIKit.Text(row, "", 17f, Dim, new Vector2(W * 0.3f, 12f), new Vector2(W * 0.4f, 28f), TextAlignmentOptions.Right);
            // 막대: 어두운 바탕 + 진행률만큼 채움
            RectTransform bar = UIKit.Rect("Bar", row, new Vector2(0f, -14f), new Vector2(W, 14f));
            Image bg = bar.gameObject.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.12f, 0.17f, 0.92f);
            bg.raycastTarget = false;
            RectTransform fill = UIKit.Rect("Fill", bar, Vector2.zero, Vector2.zero);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Max(0.01f, g.progress), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            Image fi = fill.gameObject.AddComponent<Image>();
            fi.color = g.progress >= 1f ? Ready : g.progress >= AlmostAt ? Gold : Fill;
            fi.raycastTarget = false;
            rows.Add((label, detail));
        }

        // 글자는 언어를 바꾸면 다시 씀 (막대는 그대로)
        System.Action write = () =>
        {
            List<Goal> now = Goals();
            for (int i = 0; i < rows.Count && i < now.Count; i++)
            {
                Goal g = now[i];
                bool almost = g.progress >= AlmostAt && g.progress < 1f;
                rows[i].label.text = g.label + (almost ? "  <color=#F5D478><b>" + Loc.T("result.goal.almost") + "</b></color>" : "");
                rows[i].detail.text = g.detail;
            }
        };
        write();
        panel.gameObject.AddComponent<LocRefresh>().refresh = write;
    }

    // 위쪽 가운데에 붙여 y 만큼 아래로
    static void FixTop(RectTransform r, float y)
    {
        if (r == null) return;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, y);
    }

    // ================================================================= 다시 하기 버튼
    // 씬의 「다시 하기」(ReStart) 자리에 기본 버튼 「같은 설정으로 다시」(금빛, 넓게) + 옆에 「캐릭터 바꿔 다시」
    // 원래 버튼은 숨김 (씬 글자 번역 SceneLocalizer 가 원래 글자로 되돌리지 않게 복제해서 씀) · 패드는 가상 커서가 기본 버튼 위로
    public static void BuildRetry()
    {
        GameObject restart = GameObject.Find("ReStart");
        if (restart == null) return;
        RectTransform rr = (RectTransform)restart.transform;
        Vector2 at = rr.anchoredPosition;
        float h = rr.sizeDelta.y;
        GameObject same = UIKit.CloneButton(restart, "RetrySameButton", "result.retry.same", () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("GameScene");
        });
        restart.SetActive(false);
        // 일일 도전(캐릭터 고정 · 메인 메뉴로 가면 도전이 끝남) · 튜토리얼 판은 「캐릭터 바꿔 다시」 없이 원래 자리 · 폭으로
        if (DailyChallenge.Active || TutorialRun.Active)
            Place(same, at, new Vector2(540f, h));
        else
        {
            GameObject hero = UIKit.CloneButton(restart, "RetryHeroButton", "result.retry.hero", () =>
            {
                openHeroPick = true;
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            });
            // 둘이 합쳐 폭 740 (오른쪽 피해 패널 x 420 ~ 과 안 겹치게)
            Place(same, new Vector2(at.x - 150f, at.y), new Vector2(424f, h));
            Place(hero, new Vector2(at.x + 226f, at.y), new Vector2(300f, h));
        }
        Image img = same.GetComponent<Image>();
        if (img != null) img.color = new Color(1f, 0.88f, 0.55f);
        same.AddComponent<ResultPadFocus>();
    }

    static void Place(GameObject button, Vector2 pos, Vector2 size)
    {
        RectTransform r = (RectTransform)button.transform;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        TMP_Text t = button.GetComponentInChildren<TMP_Text>(true);
        if (t == null) return;
        float max = t.fontSize;
        t.enableAutoSizing = true;
        t.fontSizeMin = 16f;
        t.fontSizeMax = max;
    }
}

// 1.1~ 패드로 결과 화면에 오면 가상 커서를 기본 버튼 위에 (배치가 끝난 다음 프레임, ResultScreen.BuildRetry)
public class ResultPadFocus : MonoBehaviour
{
    IEnumerator Start()
    {
        yield return null;
        GamePad.WarpTo(transform as RectTransform);
    }
}
