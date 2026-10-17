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
        public string label, detail, hint;     // 1.1~ hint: 막대 아래 왼쪽 작은 설명 (숙련 +1 방법)
        public float progress;      // 0 ~ 1
        public Goal(string label, float progress, string detail, string hint = null) { this.label = label; this.progress = Mathf.Clamp01(progress); this.detail = detail; this.hint = hint; }
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
                // 1.2~ 잴 수 있는 길 중 가까운 쪽: 무한 모드 10분 · 이야기 모드 대체 루트(상자 15개 · 결계 전 보스 4종 · 무게 2)
                float alt = UnlockRoutes.HasAlt(d.condition) ? UnlockRoutes.AltProgress(d.condition) : 0f;
                float endless = d.condition == CharUnlock.Endless10 && GameMode.IsUnlocked(Difficulty.Endless) ? EndlessMode.BestSeconds / 600f : -1f;
                if (endless < 0f && alt <= 0f) continue;
                if (endless >= alt) g = new Goal(label, endless, Loc.T("최고 기록 ") + EndlessMode.Clock(EndlessMode.BestSeconds) + " / 10:00");
                else g = new Goal(label, alt, UnlockRoutes.AltShort(d.condition));
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
                             Mastery.Label(me), Loc.T("result.goal.masteryhint")));

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
        // 1.2~ 무한 모드 판(일일 도전 제외)이면 다음 이정표를 맨 위에 (「다음 이정표까지 n:nn」)
        int next = GameMode.IsEndless && !DailyChallenge.Active ? EndlessMilestones.NextTime() : -1;
        if (next >= 0)
        {
            EndlessMilestones.Def m = EndlessMilestones.All[next];
            float reach = Mathf.Min(EndlessMode.BestSeconds, m.seconds);
            all.Insert(0, new Goal(Loc.T("result.goal.milestone").Replace("{0}", Mathf.RoundToInt(m.seconds / 60f).ToString()), reach / m.seconds,
                                   Loc.T("result.goal.milestone.left").Replace("{0}", EndlessMode.Clock(m.seconds - reach)), "+" + m.points.ToString("N0") + " P"));
        }
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
        // 1.1~ 한 줄: 위 이름(전체 폭) → 막대 → 아래 왼쪽 설명 · 「거의 다!」 + 오른쪽 수치 (예전엔 이름 옆 192 폭 칸에 수치 · 숙련 설명이 다 들어가 12pt 두 줄로 넘쳤음)
        //   세 줄이면 34 + 72×3 = 250 → 아래 끝 y 265 (이번 판 결과 글 위끝 260 과 안 겹침)
        const float RowH = 72f, HeadH = 34f, W = 480f;
        RectTransform panel = UIKit.Rect("NextGoals", canvas, Vector2.zero, new Vector2(W, HeadH + RowH * goals.Count));
        panel.anchorMin = panel.anchorMax = new Vector2(0f, 0.5f);
        panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = new Vector2(60f, 515f);
        TMP_Text head = UIKit.Text(panel, "result.goal.title", 24f, new Color(0.79f, 0.63f, 1f), Vector2.zero, new Vector2(W, HeadH), TextAlignmentOptions.Left);
        FixTop(head.rectTransform, -HeadH * 0.5f);

        List<(TMP_Text label, TMP_Text detail, TMP_Text hint)> rows = new List<(TMP_Text, TMP_Text, TMP_Text)>();
        for (int i = 0; i < goals.Count; i++)
        {
            Goal g = goals[i];
            float y = -HeadH - RowH * i - RowH * 0.5f;
            RectTransform row = UIKit.Rect("Goal", panel, Vector2.zero, new Vector2(W, RowH));
            FixTop(row, y);
            TMP_Text label = UIKit.Text(row, "", 20f, Parch, new Vector2(0f, 21f), new Vector2(W, 28f), TextAlignmentOptions.Left);
            TMP_Text hint = UIKit.Text(row, "", 16f, Dim, new Vector2(-W * 0.2f, -21f), new Vector2(W * 0.6f, 24f), TextAlignmentOptions.Left);
            TMP_Text detail = UIKit.Text(row, "", 17f, Dim, new Vector2(W * 0.3f, -21f), new Vector2(W * 0.4f, 24f), TextAlignmentOptions.Right);
            label.enableWordWrapping = hint.enableWordWrapping = detail.enableWordWrapping = false;      // 줄을 접어 칸 높이를 넘지 않게 (자동 크기로 한 줄에)
            // 막대: 어두운 바탕 + 진행률만큼 채움
            RectTransform bar = UIKit.Rect("Bar", row, Vector2.zero, new Vector2(W, 14f));
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
            rows.Add((label, detail, hint));
        }

        // 글자는 언어를 바꾸면 다시 씀 (막대는 그대로)
        System.Action write = () =>
        {
            List<Goal> now = Goals();
            for (int i = 0; i < rows.Count && i < now.Count; i++)
            {
                Goal g = now[i];
                bool almost = g.progress >= AlmostAt && g.progress < 1f;
                rows[i].label.text = g.label;
                rows[i].detail.text = g.detail;
                rows[i].hint.text = (almost ? "<color=#F5D478><b>" + Loc.T("result.goal.almost") + "</b></color>" + (string.IsNullOrEmpty(g.hint) ? "" : "  ") : "") + (g.hint ?? "");
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
    // 1.1.2~ 가운데 한 줄로 세로 정렬: 「같은 설정으로 다시」(기본, 금빛, 가장 큼) → 「캐릭터 바꿔 다시」 → 「메인 메뉴로」
    //   예전엔 위 줄(같은 설정 424 + 캐릭터 300, 가운데 x +7 · 높이 128) 아래에 씬의 메인 메뉴(540 × 112, 가운데 x 0)가 그대로 남아
    //   양 끝이 -362 / 376 과 -270 / 270 으로 엇갈리고, 글자도 43 · 30 · 50pt 로 제각각(기본 버튼보다 메인 메뉴 글자가 컸음)
    // 씬의 원래 버튼(ReStart · MainMenu)은 숨기고 복제해서 씀 (SceneLocalizer 가 원래 글자로 되돌리지 않게 · 1초 입력 보호를 메인 메뉴에도)
    // 패드 십자키 · 키보드 방향키 위아래 순서 = 화면 위아래 순서, 패드는 가상 커서가 기본 버튼 위로
    // 캔버스는 1920×1080 Expand 라 16:10 · 4:3 에서도 폭이 1920 아래로 줄지 않음 → 가운데 열(왼쪽 결과 글 x -420 · 오른쪽 피해 패널 x 420 사이)이 늘 같음
    const float ColW = 560f, MainH = 120f, SubH = 88f, Gap = 16f, MainY = 0f;
    const float MainFont = 52f, SubFont = 40f;

    public static void BuildRetry()
    {
        GameObject restart = GameObject.Find("ReStart");
        if (restart == null) return;
        GameObject menu = GameObject.Find("MainMenu");
        // 1.1~ [R] 다시 시작(QuickRestart)처럼 결과 화면이 뜨고 1초 동안은 받지 않음 (죽을 때 패드 A 를 연타하던 중이면 결과를 보기도 전에 새 판이 시작됐음)
        float readyAt = Time.unscaledTime + 1f;
        List<GameObject> col = new List<GameObject>();
        GameObject same = UIKit.CloneButton(restart, "RetrySameButton", "result.retry.same", () =>
        {
            if (Time.unscaledTime < readyAt) return;
            Time.timeScale = 1f;
            SceneManager.LoadScene("GameScene");
        });
        col.Add(same);
        // 일일 도전(캐릭터 고정 · 메인 메뉴로 가면 도전이 끝남) · 튜토리얼 판은 「캐릭터 바꿔 다시」 없이
        if (!DailyChallenge.Active && !TutorialRun.Active)
            col.Add(UIKit.CloneButton(restart, "RetryHeroButton", "result.retry.hero", () =>
            {
                if (Time.unscaledTime < readyAt) return;
                openHeroPick = true;
                RunLog.NextKind = "hero";           // 1.2~ 판 기록 retry_kind (메인 메뉴를 거쳐 다음 판까지 남음)
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            }));
        if (menu != null)
        {
            col.Add(UIKit.CloneButton(menu, "ResultMenuButton", "메인 메뉴로", () =>
            {
                if (Time.unscaledTime < readyAt) return;
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            }));
            menu.SetActive(false);
        }
        restart.SetActive(false);

        // 위에서부터: 기본 버튼 가운데 y 0 (위끝 60 · 제목 글 아래끝 75 과 안 겹침), 그 아래 16 간격으로 88 높이
        //   세 개면 아래끝 -268 → 무한 모드 · 일일 도전 기록 글(위끝 -275) 위
        float top = MainY + MainH * 0.5f;
        for (int i = 0; i < col.Count; i++)
        {
            float h = i == 0 ? MainH : SubH;
            Place(col[i], new Vector2(0f, top - h * 0.5f), new Vector2(ColW, h), i == 0 ? MainFont : SubFont);
            top -= h + Gap;
        }
        // 마우스로 누른 뒤 방향키를 써도 화면 순서대로 (자동 탐색은 숨긴 원래 버튼 · 다른 UI 로 샐 수 있음)
        for (int i = 0; i < col.Count; i++)
        {
            Button b = col[i].GetComponent<Button>();
            if (b == null) continue;
            Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
            if (i > 0) nav.selectOnUp = col[i - 1].GetComponent<Button>();
            if (i < col.Count - 1) nav.selectOnDown = col[i + 1].GetComponent<Button>();
            b.navigation = nav;
        }
        Image img = same.GetComponent<Image>();
        if (img != null) img.color = new Color(1f, 0.88f, 0.55f);
        same.AddComponent<ResultPadFocus>();
    }

    // 씬 제목(「사망하셨습니다...」 · 「<난이도> 클리어!」)을 가운데 열 폭으로 — 원래 1500 폭이라 긴 글(영어 · 일본어 클리어 문구)이
    // 왼쪽 결과 글(x -900 ~ -420) · 오른쪽 피해 패널(x 420 ~ 900) 위까지 번졌음
    public static void FitTitle()
    {
        GameObject msg = GameObject.Find("GameOverMessage");
        if (msg == null) return;
        RectTransform r = (RectTransform)msg.transform;
        r.sizeDelta = new Vector2(800f, r.sizeDelta.y);
        TMP_Text t = msg.GetComponentInChildren<TMP_Text>(true);
        if (t == null) return;
        t.enableAutoSizing = true;
        t.fontSizeMin = 40f;
    }

    static void Place(GameObject button, Vector2 pos, Vector2 size, float font)
    {
        RectTransform r = (RectTransform)button.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        TMP_Text t = button.GetComponentInChildren<TMP_Text>(true);
        if (t == null) return;
        // 한 줄로, 넘치면 줄여서 (가장 긴 「キャラを変えて再挑戦」 10자도 글자 칸 528 에 40pt 로 들어감)
        t.enableWordWrapping = false;
        t.enableAutoSizing = true;
        t.fontSizeMin = 18f;
        t.fontSizeMax = font;
        t.fontSize = font;
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
