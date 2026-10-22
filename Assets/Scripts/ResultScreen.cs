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
    static List<Goal> Goals(int max = 2)      // 2026-10-22~ 3 → 2 (간결하게)
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

        // 1.2~ 계약 보드: 지금 영웅 열 · 공용 줄에서 보이는 칸 중 가장 가까운 것 하나 (받을 보상이 있으면 그것을 먼저)
        Goal? cell = BoardGoal();
        if (cell != null) all.Add(cell.Value);

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
        if (all.Count > max) all.RemoveRange(max, all.Count - max);
        return all;
    }

    static Goal? BoardGoal()
    {
        if (!ContractBoard.Available) return null;
        ContractBoard.Refresh(false);
        HashSet<string> done = new HashSet<string>();
        foreach (BoardCell c in ContractBoard.Cells) if (ContractBoard.IsDone(c.key)) done.Add(c.key);
        Goal? best = null;
        foreach (BoardCell c in ContractBoard.Cells)
        {
            if (c.heroRow && c.col != (int)CharacterData.Selected) continue;
            if (!ContractBoard.Visible(c, done) || c.key == "c.last") continue;
            bool isDone = done.Contains(c.key);
            if (isDone && (ContractBoard.IsClaimed(c.key) || c.points <= 0)) continue;
            float p = isDone ? 1f : (float)ContractBoard.Progress(c) / Mathf.Max(1, c.target);
            string detail = isDone ? "<color=#7CFF9A>" + Loc.T("claim.badge").Replace("{0}", "1") + "</color>" : ContractBoard.Progress(c).ToString("N0") + " / " + c.target.ToString("N0");
            Goal g = new Goal(Loc.T("board.open") + " · " + c.Name, p, detail, ContractBoard.RewardLine(c));
            if (best == null || g.progress > best.Value.progress) best = g;
        }
        return best;
    }

    static string PointsLeft(int left) =>
        left > 0 ? Loc.T("해금까지") + " " + left.ToString("N0") + " P" : "<color=#7CFF9A>" + Loc.T("result.goal.ready") + "</color>";

    // ================================================================= 1.4.8~ 왼쪽 반투명 패널
    // 예전엔 다음 목표 · 이번 판 결과 · 빌드 자세히 보기가 배경 위에 14 ~ 24 글씨로 따로 떠 있었음 → 한 패널(폭 528 · 안쪽 여백 24)에 위에서부터
    //   다음 목표(또는 이번 판 계약 + 영구 목표) → 구분선 → 이번 판 결과 → 빌드 자세히 보기, 글씨 22 (작은 줄 20)
    //   위아래는 캔버스 높이에 붙임(위아래 16) — 16:10 · 4:3 처럼 캔버스가 길어져도 · UI 크기 110 · 120% 로 짧아져도 화면 안
    //   오른쪽 끝 x -396 (1080p) — 가운데 제목(폭 800) · 버튼 열(폭 560)과 떨어져 있음
    public const float LeftW = 528f, LeftPad = 24f, LeftX = 36f;
    public const float ContentW = LeftW - LeftPad * 2f;        // 480

    public static RectTransform BuildLeftPanel(Transform canvas)
    {
        UIKit.EnsureStyle();
        RectTransform panel = UIKit.Rect("ResultLeftPanel", canvas, Vector2.zero, Vector2.zero);
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 0.5f);
        panel.offsetMin = new Vector2(LeftX, 16f);
        panel.offsetMax = new Vector2(LeftX + LeftW, -16f);
        Image bg = panel.gameObject.AddComponent<Image>();
        bg.sprite = UIKit.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.08f, 0.06f, 0.11f, 0.72f);
        if (!UiRules.Legacy && UiRules.Sprite("ui2_panel") != null) { bg.sprite = UiRules.Sprite("ui2_panel"); bg.type = Image.Type.Tiled; bg.pixelsPerUnitMultiplier = 1f; bg.color = Color.white; }     // UI 개편 P0-4c: 불투명 석판
        bg.raycastTarget = false;
        return panel;
    }

    // 패널 안 가로 구분선 (위끝에서 y 만큼 아래)
    public static void Divider(RectTransform panel, float y)
    {
        RectTransform r = UIKit.Rect("Divider", panel, Vector2.zero, new Vector2(ContentW, 2f));
        FixTop(r, y);
        Image img = r.gameObject.AddComponent<Image>();
        img.color = new Color(0.79f, 0.63f, 1f, 0.25f);
        img.raycastTarget = false;
        if (!UiRules.Legacy) { r.sizeDelta = new Vector2(ContentW, UiRules.Dot); img.color = UiTokens.LineDim; }        // 1도트 선
    }

    // 결과 화면 왼쪽 위 (이번 판 결과 글 위, 480 폭)
    // 1.2~ 영혼 계약이 있던 판: 「이번 판 계약」 3줄 → 「영구 목표」 2줄 (BuildContracts) · 없던 판(튜토리얼 · 옛 저장)은 예전처럼 다음 목표 3줄
    // 1.4.8~ 왼쪽 패널(BuildLeftPanel) 안 위쪽에 · 쓴 높이를 돌려줌 (없으면 0) — 그 아래에 이번 판 결과 글
    public static float BuildGoals(RectTransform left)
    {
        Contracts.EnsureEnded();
        if (Contracts.LastRunOn && Contracts.LastRun.Count > 0) return BuildContracts(left);
        List<Goal> goals = Goals();
        if (goals.Count == 0) return 0f;
        UIKit.EnsureStyle();
        // 1.1~ 한 줄: 위 이름(전체 폭) → 막대 → 아래 왼쪽 설명 · 「거의 다!」 + 오른쪽 수치 (예전엔 이름 옆 192 폭 칸에 수치 · 숙련 설명이 다 들어가 12pt 두 줄로 넘쳤음)
        // 1.4.8~ 글씨 20 · 16 → 22 · 20, 줄 높이 72 → 82 (줄 사이 9) · 머리 24 → 28
        const float RowH = 82f, HeadH = 38f, W = ContentW;
        RectTransform panel = UIKit.Rect("NextGoals", left, Vector2.zero, new Vector2(W, HeadH + RowH * goals.Count));
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -LeftPad);
        TMP_Text head = UIKit.Text(panel, "result.goal.title", 28f, new Color(0.79f, 0.63f, 1f), Vector2.zero, new Vector2(W, HeadH), TextAlignmentOptions.Left);
        head.fontSizeMin = 20f;
        FixTop(head.rectTransform, -HeadH * 0.5f);

        List<(TMP_Text label, TMP_Text detail, TMP_Text hint)> rows = new List<(TMP_Text, TMP_Text, TMP_Text)>();
        for (int i = 0; i < goals.Count; i++)
        {
            Goal g = goals[i];
            float y = -HeadH - RowH * i - RowH * 0.5f;
            RectTransform row = UIKit.Rect("Goal", panel, Vector2.zero, new Vector2(W, RowH));
            FixTop(row, y);
            TMP_Text label = UIKit.Text(row, "", 22f, Parch, new Vector2(0f, 24f), new Vector2(W, 28f), TextAlignmentOptions.Left);
            TMP_Text hint = UIKit.Text(row, "", 20f, Dim, new Vector2(-W * 0.2f, -22f), new Vector2(W * 0.6f, 26f), TextAlignmentOptions.Left);
            TMP_Text detail = UIKit.Text(row, "", 20f, Dim, new Vector2(W * 0.3f, -22f), new Vector2(W * 0.4f, 26f), TextAlignmentOptions.Right);
            label.enableWordWrapping = hint.enableWordWrapping = detail.enableWordWrapping = false;      // 줄을 접어 칸 높이를 넘지 않게 (자동 크기로 한 줄에)
            foreach (TMP_Text t in new[] { label, hint, detail }) { t.fontSizeMin = 20f; t.overflowMode = TextOverflowModes.Ellipsis; }
            // 막대: 어두운 바탕 + 진행률만큼 채움
            RectTransform bar = UIKit.Rect("Bar", row, Vector2.zero, new Vector2(W, 12f));
            Image bg = bar.gameObject.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.12f, 0.17f, 0.92f);
            bg.raycastTarget = false;
            RectTransform fill = UIKit.Rect("Fill", BarGroove(bar, bg), Vector2.zero, Vector2.zero);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Max(0.01f, g.progress), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            Image fi = fill.gameObject.AddComponent<Image>();
            if (BarArt) fi.sprite = UIKit.BarFillSprite;
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
        return panel.sizeDelta.y;
    }

    // ================================================================= 1.2~ 이번 판 계약 + 영구 목표
    //   계약 한 줄 = 이름(달성 금빛 · 진행) + 아래 작은 글(달성 +P · 다음 판에 이어짐 · 접고 새로 뽑음) 44 높이
    //   위 끝 y 515 · 아래 끝은 이번 판 결과 글 위끝(260) 위: 머리 28 + 계약 40×3 + 머리 28 + 목표 42×2 = 260 → 255 (목표 줄은 이름 · 수치 한 줄 + 얇은 막대 + 작은 설명)
    //   1.4.8~ 글씨 18 · 14 · 13 → 22 · 20 (머리 28) · 줄 높이 40 · 42 → 60 · 70 · 머리 38 — 합 38 + 60×3 + 38 + 70×2 = 396
    static float BuildContracts(RectTransform left)
    {
        UIKit.EnsureStyle();
        const float W = ContentW, HeadH = 38f, RowH = 60f, GoalH = 70f;
        List<Goal> goals = Goals(1);      // 2026-10-22~ 계약이 있는 판은 영구 목표 한 줄만
        float h = HeadH + RowH * Contracts.LastRun.Count + (goals.Count > 0 ? HeadH + GoalH * goals.Count : 0f);
        RectTransform panel = UIKit.Rect("NextGoals", left, Vector2.zero, new Vector2(W, h));
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -LeftPad);
        float y = 0f;
        TMP_Text head = UIKit.Text(panel, "contract.result.title", 28f, new Color(0.79f, 0.63f, 1f), Vector2.zero, new Vector2(W, HeadH), TextAlignmentOptions.Left);
        head.fontSizeMin = 20f;
        FixTop(head.rectTransform, -HeadH * 0.5f);
        y -= HeadH;
        int i = 0;
        foreach (Contracts.Active a in Contracts.LastRun)
        {
            RectTransform row = UIKit.Rect("Contract", panel, Vector2.zero, new Vector2(W, RowH));
            FixTop(row, y - RowH * 0.5f);
            TMP_Text name = UIKit.Text(row, "", 22f, a.done ? Gold : Parch, new Vector2(0f, 13f), new Vector2(W, 28f), TextAlignmentOptions.Left);
            TMP_Text sub = UIKit.Text(row, "", 20f, Dim, new Vector2(0f, -15f), new Vector2(W, 24f), TextAlignmentOptions.Left);
            name.enableWordWrapping = sub.enableWordWrapping = false;
            foreach (TMP_Text t in new[] { name, sub }) { t.fontSizeMin = 20f; t.overflowMode = TextOverflowModes.Ellipsis; }
            UIKit.Forget(name);
            UIKit.Forget(sub);
            System.Action write = null;
            Contracts.Active ca = a;
            write = () =>
            {
                name.text = "<size=80%><color=#A89C86>" + Contracts.KindTag(ca.def) + "</color></size>  " + ca.def.Name
                          + (ca.done ? "" : "  <size=80%><color=#A89C86>" + Contracts.Progress(ca.def, ca.best) + "</color></size>");
                sub.text = ca.done ? "<color=#7CFF9A>" + Loc.T("contract.result.done").Replace("{0}", ca.def.Reward.ToString("N0")) + "</color>"
                         : Contracts.WillCarry(ca) ? Loc.T("contract.carry").Replace("{0}", Mathf.Min(ca.best, ca.def.target).ToString("N0")).Replace("{1}", ca.def.target.ToString("N0"))
                         : Loc.T("contract.dropped");
            };
            write();
            row.gameObject.AddComponent<LocRefresh>().refresh = write;
            if (a.done) row.gameObject.AddComponent<ResultPop>().delay = 0.35f + 0.3f * i;
            y -= RowH;
            i++;
        }
        if (goals.Count == 0) return h;
        TMP_Text gh = UIKit.Text(panel, "contract.result.goals", 28f, new Color(0.79f, 0.63f, 1f), Vector2.zero, new Vector2(W, HeadH), TextAlignmentOptions.Left);
        gh.fontSizeMin = 20f;
        FixTop(gh.rectTransform, y - HeadH * 0.5f);
        y -= HeadH;
        foreach (Goal g in goals)
        {
            RectTransform row = UIKit.Rect("Goal", panel, Vector2.zero, new Vector2(W, GoalH));
            FixTop(row, y - GoalH * 0.5f);
            TMP_Text label = UIKit.Text(row, "", 22f, Parch, new Vector2(-W * 0.18f, 18f), new Vector2(W * 0.64f, 28f), TextAlignmentOptions.Left);
            TMP_Text detail = UIKit.Text(row, "", 20f, Dim, new Vector2(W * 0.32f, 18f), new Vector2(W * 0.36f, 28f), TextAlignmentOptions.Right);
            TMP_Text hint = UIKit.Text(row, "", 20f, Dim, new Vector2(0f, -20f), new Vector2(W, 24f), TextAlignmentOptions.Left);
            label.enableWordWrapping = detail.enableWordWrapping = hint.enableWordWrapping = false;
            foreach (TMP_Text t in new[] { label, detail, hint }) { UIKit.Forget(t); t.fontSizeMin = 20f; t.overflowMode = TextOverflowModes.Ellipsis; }
            label.text = g.label;
            detail.text = g.detail;
            bool almost = g.progress >= AlmostAt && g.progress < 1f;
            hint.text = (almost ? "<color=#F5D478><b>" + Loc.T("result.goal.almost") + "</b></color>  " : "") + (g.hint ?? "");
            // 1.4.8~ 막대 그림이 있으면 틀이 보이게 4 → 12 (위 글 아래끝 4 · 아래 글 위끝 -8 사이)
            RectTransform bar = UIKit.Rect("Bar", row, new Vector2(0f, -2f), new Vector2(W, BarArt ? 12f : 4f));
            Image bg = bar.gameObject.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.12f, 0.17f, 0.92f);
            bg.raycastTarget = false;
            RectTransform fill = UIKit.Rect("Fill", BarGroove(bar, bg), Vector2.zero, Vector2.zero);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Max(0.01f, g.progress), 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            Image fi = fill.gameObject.AddComponent<Image>();
            if (BarArt) fi.sprite = UIKit.BarFillSprite;
            fi.color = g.progress >= 1f ? Ready : g.progress >= AlmostAt ? Gold : Fill;
            fi.raycastTarget = false;
            y -= GoalH;
        }
        return h;
    }

    // 1.2~ 결과 화면 아래 한 줄: 「이번 판이 남긴 것: +4,720P · 계약 2 · 도감 +1 · 기억 +1」 (계약이 있던 판만 · 획득 포인트 줄 위)
    public static void BuildLeftBehind(Transform canvas)
    {
        if (!Contracts.LastRunOn || DailyChallenge.Active) return;        // 일일 도전은 아래 기록 글(오늘 순위 3줄)이 이 자리까지 내려옴
        UIKit.EnsureStyle();
        TMP_Text t = UIKit.Text(canvas, "", 22f, new Color(0.79f, 0.63f, 1f), Vector2.zero, new Vector2(1200f, 34f));
        t.fontSizeMin = 20f;
        t.name = "LeftBehindLine";      // 1.4.8~ 자리는 StackCenter (예전 아래 기준 y 132)
        RectTransform r = t.rectTransform;
        System.Action write = () => t.text = Loc.T("contract.result.left").Replace("{0}", "+" + CharacterData.RunPoints.ToString("N0"))
                                    .Replace("{1}", Contracts.DoneThisRun.ToString()).Replace("{2}", Contracts.CodexGained.ToString()).Replace("{3}", Contracts.MemoryGained.ToString())
                                    + (Contracts.MemoryGained > 0 ? "\n<size=80%><color=#F5D478>" + Loc.T("memory.new") + "</color></size>" : "");
        write();
        UIKit.Forget(t);
        t.gameObject.AddComponent<LocRefresh>().refresh = write;
        if (Contracts.MemoryGained > 0) r.sizeDelta = new Vector2(1200f, 64f);
    }

    // 1.2~ 같은 영웅 · 난이도로 세 번 연달아 쓰러짐: 버튼 열 아래 한 줄 추천 (설계 3-5 ① e · 일일 도전 · 튜토리얼 · 체험판 · 무한 모드 기록 글 자리에는 없음)
    public static void BuildRecommend(Transform canvas)
    {
        if (Herald.Streak < 3 || RunStats.Cleared || RunStats.GaveUp || DailyChallenge.Active || TutorialRun.Active || Demo.On || GameMode.IsEndless) return;
        UIKit.EnsureStyle();
        // 산 영웅 중 아직 숙련이 없는 다른 영웅 (얼굴 그림을 옆에 — 영웅 이름을 문장에 끼우면 조사가 틀려서) · 없으면 한 단계 낮은 난이도
        CharacterId? pick = null;
        for (int i = 0; i < CharacterData.All.Length; i++)
        {
            CharacterId id = (CharacterId)i;
            if (id == CharacterData.Selected || !CharacterData.IsDeveloped(id) || !CharacterData.IsUnlocked(id) || Mastery.Level(id) > 0) continue;
            pick = id;
            break;
        }
        string line = pick != null ? "recommend.hero.noname" : GameMode.Current != Difficulty.Easy ? "recommend.diff" : null;
        if (line == null && Herald.SameCause3) line = "recommend.hint";
        if (line == null) return;
        // 1.4.8~ 19 → 22 (하한 14 → 20) · 자리는 StackCenter (얼굴은 글 왼쪽 끝 옆에 같이 옮겨짐)
        TMP_Text t = UIKit.Text(canvas, line, 22f, new Color(0.75f, 0.85f, 1f), new Vector2(0f, -318f), new Vector2(760f, 58f));
        t.fontSizeMin = 20f;
        t.name = "RecommendLine";
        if (pick != null)
        {
            Sprite face = CharacterUI.Portrait(pick.Value);
            if (face != null)
            {
                Image fi = UIKit.Rect("RecommendHero", t.rectTransform, new Vector2(-410f, 0f), new Vector2(48f, 48f)).gameObject.AddComponent<Image>();
                fi.sprite = face;
                fi.preserveAspect = true;
                fi.raycastTarget = false;
            }
        }
    }

    // ================================================================= 1.4.8~ 가운데 버튼 열 아래를 위에서부터 차례로 (MenuExtras.InstallGameOver 끝에서)
    // 예전엔 「순위 보기」(기록 글 오른쪽 x 600) · 「결과 복사」(오른쪽 아래 구석)가 따로 떠 있었고 기록 · 남긴 것 · 포인트 줄이 저마다 다른 기준(가운데 · 아래)이었음
    //   → 버튼 열 아래끝부터 간격 8 로: 「순위 보기 · 결과 복사」 한 줄 → 무한 · 일일 기록(또는 추천 한 줄) → 이번 판이 남긴 것 → 획득 포인트
    //   [R] 다시 시작 안내만 화면 아래 가장자리 (1080p 에서 가장 많이 쌓여도(무한 + 기억 조각) 아래끝 -492 · [R] 위끝 -494)
    public const float RowBtnW = 260f, RowBtnH = 56f;
    public static void StackCenter(Transform canvas)
    {
        if (canvas == null) return;
        const float Step = 8f;
        float y = columnBottom - Step;
        // 버튼 한 줄 (있는 것만, 가운데 맞춤)
        List<RectTransform> row = new List<RectTransform>();
        foreach (string n in new[] { "LeaderboardViewButton", "CopyResultButton" })
            if (canvas.Find(n) is RectTransform b && b.gameObject.activeSelf) row.Add(b);
        if (row.Count > 0)
        {
            float total = RowBtnW * row.Count + 16f * (row.Count - 1);
            for (int i = 0; i < row.Count; i++)
            {
                RectTransform b = row[i];
                b.anchorMin = b.anchorMax = b.pivot = new Vector2(0.5f, 0.5f);
                b.sizeDelta = new Vector2(RowBtnW, RowBtnH);
                b.anchoredPosition = new Vector2(-total * 0.5f + RowBtnW * 0.5f + (RowBtnW + 16f) * i, y - RowBtnH * 0.5f);
                TMP_Text l = b.GetComponentInChildren<TMP_Text>(true);
                if (l != null) { l.rectTransform.sizeDelta = new Vector2(RowBtnW - 20f, RowBtnH - 12f); l.fontSizeMax = 24f; l.fontSizeMin = 20f; }
            }
            y -= RowBtnH + Step;
        }
        foreach (string n in new[] { "EndlessRecord", "RecommendLine", "LeftBehindLine", "RunPointsLine" })
        {
            if (!(canvas.Find(n) is RectTransform r)) continue;
            float h = r.sizeDelta.y;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0f, y - h * 0.5f);
            y -= h + Step;
        }
    }

    // 1.4.8~ 새 도트 스타일 목표 막대: 막대 틀(ui_bar_frame · 테두리 반 두께) + 채움(ui_bar_fill · 상태 색을 곱함)
    static bool BarArt
    {
        get { UIKit.EnsureStyle(); return UIKit.BarFrameSprite != null && UIKit.BarFillSprite != null; }
    }

    // 막대 바탕을 틀 그림으로 바꾸고 채움을 넣을 안쪽 홈(테두리 3 안쪽)을 돌려줌 — 채움 길이(anchorMax.x = 진행률)는 홈 폭 기준으로 그대로
    //   그림이 없으면 바탕 그대로 · 막대 자신
    static RectTransform BarGroove(RectTransform bar, Image bg)
    {
        if (!BarArt) return bar;
        bg.sprite = UIKit.BarFrameSprite;
        bg.type = Image.Type.Sliced;
        bg.pixelsPerUnitMultiplier = 2f;
        bg.color = Color.white;
        RectTransform g = UIKit.Rect("Groove", bar, Vector2.zero, Vector2.zero);
        g.anchorMin = Vector2.zero;
        g.anchorMax = Vector2.one;
        g.offsetMin = new Vector2(3f, 3f);
        g.offsetMax = new Vector2(-3f, -3f);
        return g;
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
    // 1.4.8~ 높이 120 · 88 · 간격 16 → 104 · 76 · 12: 아래에 「순위 보기 · 결과 복사」 한 줄과 기록 · 포인트 줄이 차례로 들어갈 자리 (StackCenter)
    const float ColW = 480f, MainH = 88f, SubH = 64f, Gap = 10f, MainY = 0f;     // 2026-10-22~ 간결하게: 560 · 104 · 76 → 480 · 88 · 64
    const float MainFont = 40f, SubFont = 32f;
    // 버튼 열 아래끝 (BuildRetry 가 씀 · 세 개면 -228, 두 개면 -140) — StackCenter 가 이 밑으로 쌓음
    static float columnBottom = MainY - MainH * 0.5f;

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
            SceneFlow.Go("GameScene");
        });
        col.Add(same);
        // 일일 도전(캐릭터 고정 · 메인 메뉴로 가면 도전이 끝남) · 튜토리얼 판은 「캐릭터 바꿔 다시」 없이
        if (!DailyChallenge.Active && !TutorialRun.Active)
            col.Add(UIKit.CloneButton(restart, "RetryHeroButton", "result.retry.hero", () =>
            {
                if (Time.unscaledTime < readyAt) return;
                openHeroPick = true;
                RunLog.NextKind = "hero";           // 1.2~ 판 기록 retry_kind (메인 메뉴를 거쳐 다음 판까지 남음)
                SceneFlow.Go("MainMenu");
            }));
        if (menu != null)
        {
            col.Add(UIKit.CloneButton(menu, "ResultMenuButton", "result.menu", () =>
            {
                if (Time.unscaledTime < readyAt) return;
                SceneFlow.Go("MainMenu");
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
        columnBottom = top + Gap;
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
        if (!UiRules.Legacy)
        {
            // UI 개편 P0-4c: 배치(세로 한 줄)는 그대로 · 「다시 하기」만 주 버튼, 나머지는 보조 석판 (금빛 글자 · 굵게 · 자간 출렁임 없음)
            for (int i = 0; i < col.Count; i++) UiV2.Restyle(col[i].GetComponent<Button>(), i == 0 ? UiV2.Kind.Primary : UiV2.Kind.Secondary, i == 0 ? UiTokens.Heading : UiTokens.Body);
            return;
        }
        // 1.4.8~ 메인 메뉴와 같은 글자 메뉴 (첫 버튼이 가장 크게 · 금빛)
        for (int i = 0; i < col.Count; i++) MenuItemFx.Apply(col[i].GetComponent<Button>(), i == 0 ? 38f : 30f, i == 0, false);      // 2026-10-22~ 46 · 34 → 38 · 30
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
        if (!UiRules.Legacy)
        {
            // UI 개편 P0-4c: 120pt 제목 → 제목 단계 48 (쓰러짐 뼈색 · 클리어 청동) · 해골 그림 숨김
            t.fontSize = t.fontSizeMax = UiTokens.Title;
            t.fontSizeMin = UiTokens.MinSize(UiTokens.Title);
            t.fontStyle &= ~FontStyles.Bold;
            t.enableVertexGradient = false;
            t.color = RunStats.Cleared ? UiTokens.Bronze : UiTokens.Text1;
            GameObject skull = GameObject.Find("Skull");
            if (skull != null) skull.SetActive(false);
        }
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

// 1.2~ 결과 화면 계약 달성 줄: 차례로 커지며 나타나고 「달성」 소리 (1.2초 안)
public class ResultPop : MonoBehaviour
{
    public float delay;
    CanvasGroup g;
    float t;

    void Start()
    {
        g = gameObject.AddComponent<CanvasGroup>();
        g.alpha = 0f;
    }

    void Update()
    {
        t += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01((t - delay) / 0.35f);
        if (g != null) g.alpha = k;
        if (UiRules.Legacy) transform.localScale = Vector3.one * Mathf.Lerp(1f + 0.25f * GameSettings.Motion, 1f, Mathf.SmoothStep(0f, 1f, k));      // 움직임 줄이기면 알파만 · UI 개편(Legacy 끔)도 알파만
        if (k > 0f && t - Time.unscaledDeltaTime - delay <= 0f) Hostile.Play("sparkle", 0.6f, 1.3f);
        if (k >= 1f) { transform.localScale = Vector3.one; enabled = false; }
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
