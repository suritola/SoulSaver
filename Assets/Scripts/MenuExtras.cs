using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 메인 메뉴에 튜토리얼 · 설정 버튼, 게임 중 ESC 메뉴에 설정 버튼을 붙임
// 기존 버튼을 복제해서 같은 모양으로 만듦 (씬을 따로 고치지 않음)
public static class MenuExtras
{
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        UIKit.RestyleScene();           // 1.4.8~ 장면에 놓인 버튼 · 창도 새 평평한 칸으로 (먼저 · 아래 설치 코드가 복제하는 버튼까지)
        if (scene.name == "MainMenu") { InstallMainMenu(); StyleTitle(); InstallAmbience(); }
        else if (scene.name == "GameScene") InstallEsc();
        else if (scene.name == "GameOver") InstallGameOver();
    }

    // 무한 모드에서 쓰러지면 생존 시간 · 보스 처치 수 · 최고 기록
    static void InstallGameOver()
    {
        Canvas canvas = UIKit.HudCanvas();
        // 1.0.4~: 끝까지 깨고 엔딩을 본 뒤에도 이 결과 화면을 거침 — 「사망하셨습니다...」 대신 「<난이도> 클리어!」, 해골은 숨김
        // 1.1.9~ 무한 모드 · 일일 도전을 「포기하고 결과 보기」로 끝냈으면 「여기까지!」 (쓰러진 게 아니므로 해골도 숨김)
        if (RunStats.Cleared || RunStats.GaveUp)
        {
            GameObject msg = GameObject.Find("GameOverMessage");
            TMP_Text title = msg != null ? msg.GetComponentInChildren<TMP_Text>(true) : null;
            if (title != null)
            {
                if (RunStats.Cleared) title.text = GameMode.Name(GameMode.Current) + " " + Loc.T("클리어!");
                else { title.text = Loc.T("여기까지!"); UIKit.Remember(title, "여기까지!"); }
                title.color = new Color(0.96f, 0.83f, 0.47f);
            }
            GameObject skull = GameObject.Find("Skull");
            if (skull != null) skull.SetActive(false);
        }
        // 이번 판 결과 (왼쪽) · [R] 다시 시작
        if (canvas != null)
        {
            UIKit.EnsureStyle();
            if (!UiRules.Legacy) InstallGameOverV2(canvas.transform);
            else
            {
            // 1.4.8~ 왼쪽 반투명 패널 하나에: 다음 목표(위) → 구분선 → 이번 판 결과 → 빌드 자세히 보기(아래) (ResultScreen.BuildLeftPanel)
            RectTransform left = ResultScreen.BuildLeftPanel(canvas.transform);
            float goalsH = ResultScreen.BuildGoals(left);       // 1.1~ 다음 목표 막대 3줄 (예전 「다음 해금까지」 글은 이 위로 옮김)
            float resTop = ResultScreen.LeftPad;
            if (goalsH > 0f)
            {
                ResultScreen.Divider(left, -(ResultScreen.LeftPad + goalsH + 12f));
                resTop += goalsH + 24f;
            }
            const float BvH = 56f;
            bool buildView = BuildView.HasSnapshot;
            // 1.4.8~ 24(하한 14) → 22(하한 20) · 항목 사이 빈 줄을 반 줄로 (넘치면 끝을 … 로 — 전부는 「빌드 자세히 보기」에)
            TMP_Text res = UIKit.Text(left, "", 22f, new Color(0.93f, 0.9f, 0.84f), Vector2.zero, Vector2.zero, TextAlignmentOptions.TopLeft);
            res.enableAutoSizing = true;
            res.fontSizeMin = 20f;
            res.fontSizeMax = 22f;
            res.overflowMode = TextOverflowModes.Ellipsis;
            RunRecords.CommitBest();         // 1.1.9~ 내 최고 기록 갱신 (판 기록 쓰기와 순서가 보장되지 않아 양쪽에서 부르되 한 번만)
            res.text = "<size=127%><color=" + (UiRules.Legacy ? "#C9A0FF" : "#EAE3D2") + ">" + Loc.T("이번 판 결과") + "</color></size>" + HalfGap + CompactGaps(RunRecords.WithBest(RunStats.Summary(false)));
            if (!UiRules.Legacy) res.color = UiTokens.Text2;          // UI 개편 P0-4c: 본문은 Text2 · 머리는 뼈색
            RunRecords.PlayNewRecordSound(canvas.transform);
            RectTransform rr = res.rectTransform;
            rr.anchorMin = Vector2.zero;
            rr.anchorMax = Vector2.one;
            rr.offsetMin = new Vector2(ResultScreen.LeftPad, ResultScreen.LeftPad + (buildView ? BvH + 16f : 0f));
            rr.offsetMax = new Vector2(-ResultScreen.LeftPad, -resTop);
            RunStats.BuildDamagePanel(canvas.transform);      // 오른쪽: 준 피해 · 받은 피해 · 쓰러진 곳
            // 2026-10-22~ 결과 화면을 간결하게 (사용자 「난잡하다」): 아래 [R] 안내 줄 · 결과 복사 버튼 · 3연속 사망 추천 줄을 뺌 (R 키 바로 다시 하기는 그대로)
            canvas.gameObject.AddComponent<QuickRestart>();
            ResultScreen.BuildRetry();       // 1.1~ 「같은 설정으로 다시」 · 「캐릭터 바꿔 다시」 · 「메인 메뉴로」 (1.1.2~ 가운데 세로 한 줄)
            ResultScreen.FitTitle();
            ResultScreen.BuildLeftBehind(canvas.transform);     // 1.2~ 「이번 판이 남긴 것」
            // 1.1.9~ 결과 글 아래 「빌드 자세히 보기」 (떠나기 전에 떠 둔 스냅샷) · 1.4.8~ 왼쪽 패널 아래끝 안쪽, 결과 글과 같은 폭
            if (buildView)
            {
                Transform croot = canvas.transform;
                Button bv = UIKit.MakeButton(left, "빌드 자세히 보기", Vector2.zero, new Vector2(ResultScreen.ContentW, BvH), () => BuildView.Open(croot), 24f);
                RectTransform bvr = (RectTransform)bv.transform;
                bvr.anchorMin = bvr.anchorMax = bvr.pivot = new Vector2(0.5f, 0f);
                bvr.anchoredPosition = new Vector2(0f, ResultScreen.LeftPad);
                bv.name = "BuildViewButton";
                bv.navigation = new Navigation { mode = Navigation.Mode.None };
                if (!UiRules.Legacy) { UiV2.Restyle(bv, UiV2.Kind.Secondary, UiTokens.Body); UiV2.Label(bv, "ui.btn.build"); }
                TMP_Text bl = bv.GetComponentInChildren<TMP_Text>(true);
                if (bl != null) bl.fontSizeMin = 20f;
            }
            }
            // 1.1.9~ 「결과 복사」 (편의성 3차 10) · 1.4.8~ 자리는 가운데 버튼 열 아래 한 줄 (ResultScreen.StackCenter)
        }
        // 이번 판에 얻은 캐릭터 포인트 (1.4.8~ 자리는 StackCenter · 기록 줄과 같은 30)
        if (canvas != null && CharacterData.RunPoints > 0)
        {
            UIKit.EnsureStyle();
            TMP_Text pt = UIKit.Text(canvas.transform, "", UiRules.Legacy ? 30f : UiTokens.Body, UiRules.Legacy ? new Color(0.96f, 0.83f, 0.47f) : UiTokens.Text1, Vector2.zero, new Vector2(1200f, 36f));
            pt.fontSizeMin = 20f;
            pt.name = "RunPointsLine";
            pt.text = Loc.T("획득 포인트 ") + "+" + CharacterData.RunPoints.ToString("N0")
                    + (Point.RunBonus > 0 ? " <size=70%>(" + Loc.T("result.bonus").Replace("{0}", Point.RunBonus.ToString("N0")) + ")</size>" : "")     // 1.1~ 클리어 보너스
                    + "   " + Loc.T("보유 ") + CharacterData.Points.ToString("N0") + " P";
        }
        if (EndlessMode.LastSeconds >= 0f && canvas != null)
        {
            UIKit.EnsureStyle();
            bool daily = DailyChallenge.Active;
            string text = (daily ? Loc.T("일일 도전") + " · " + Loc.T(DailyChallenge.RunRuleName) + "\n" : "")
                        + Loc.T("생존 시간 ") + EndlessMode.Clock(EndlessMode.LastSeconds) + "   " + Loc.T("보스 처치 ") + EndlessMode.LastBosses
                        + "\n" + (daily ? Loc.T("오늘 내 최고 기록") + " " + EndlessMode.Clock(DailyChallenge.RunBest) : Loc.T("최고 기록 ") + EndlessMode.Clock(EndlessMode.BestSeconds));
            // 1.4.8~ 34 → 30 (획득 포인트 줄과 같은 크기 · 줄 높이 38) · 자리는 StackCenter (버튼 열 → 순위 보기 · 결과 복사 줄 아래)
            TMP_Text t = UIKit.Text(canvas.transform, "", UiRules.Legacy ? 30f : UiTokens.Body, UiRules.Legacy ? new Color(1f, 0.8f, 0.45f) : UiTokens.Text1, Vector2.zero, new Vector2(1200f, daily ? 114f : 76f));
            t.fontSizeMin = 20f;
            t.name = "EndlessRecord";
            t.text = text;
            // 스팀 순위: 올린 결과가 오면 마지막 줄 끝에 붙임
            DailyChallenge.RankLine line = t.gameObject.AddComponent<DailyChallenge.RankLine>();
            line.text = t;
            line.baseText = text;
            line.daily = daily;
            // 1.1.5~ 「순위 보기」 → 순위 창 (내 주변 탭, 이번 판 기록을 다 올린 뒤에 받음) · 체험판은 무한 모드가 없음
            // 1.4.8~ 기록 글 오른쪽(x 600)에 떠 있던 것을 「결과 복사」와 한 줄로 (StackCenter)
            if (!Demo.On)
            {
                Transform root = canvas.transform;
                Button view = UIKit.MakeButton(root, "lb.view", Vector2.zero, new Vector2(ResultScreen.RowBtnW, ResultScreen.RowBtnH),
                    () => LeaderboardUI.Open(root, daily ? LeaderboardUI.Board.Daily : LeaderboardUI.Board.Endless, SteamLeaderboards.Scope.AroundMe), 24f);
                view.name = "LeaderboardViewButton";
                if (!UiRules.Legacy) { UiV2.Restyle(view, UiV2.Kind.Secondary, UiTokens.Body); UiV2.Label(view, "ui.btn.ranks"); }
            }
        }
        EndlessMode.LastSeconds = -1f;
        if (canvas != null) ResultScreen.StackCenter(canvas.transform);
    }

    // 1.4.8~ 결과 · ESC 이번 판 글: 항목 사이 빈 줄(\n\n)을 반 줄 높이로 (줄이 많아 글씨가 20 아래로 줄던 것)
    public const string HalfGap = "\n<size=45%>\n</size>";
    public static string CompactGaps(string s) => string.IsNullOrEmpty(s) ? s : s.Replace("\n\n", HalfGap);

    static void InstallMainMenu()
    {
        DailyChallenge.End();
        EndlessMode.LastSeconds = -1f;  // 1.2.3~ 무한 판을 저장하고 나오면 남은 기록이 다음 이야기 판 결과 화면에 붙지 않게
        RunSave.EndContinue();          // 1.0.4~: 이어한 판의 캐릭터 · 난이도를 메뉴 선택에서 거둠 (난이도 버튼을 그리기 전에)
        mainLaidOut = false;
        GameObject start = GameObject.Find("GameStartButton");
        GameObject exit = GameObject.Find("ExitButton");
        if (start == null || GameObject.Find("TutorialButton") != null) return;

        RectTransform sr = (RectTransform)start.transform;
        GameObject character = UIKit.CloneButton(start, "CharacterButton", "캐릭터", () => CharacterUI.Open(sr.root));
        GameObject skins = UIKit.CloneButton(start, "SkinButton", "스킨 상점", () => SkinShopUI.Open(sr.root));
        // 2.1.9: 글 튜토리얼 대신 직접 해 보는 플레이 튜토리얼 (TutorialRun), 한 번도 안 해 봤으면 반짝임
        GameObject tutorial = UIKit.CloneButton(start, "TutorialButton", "튜토리얼", () => TutorialRun.Begin());
        if (!TutorialRun.Done) tutorial.AddComponent<NewGlow>();       // 1.0.4~: 시작만 해서는 꺼지지 않고 끝까지 마쳐야 꺼짐 (마치면 아래에서 완료 체크)
        GameObject codex = UIKit.CloneButton(start, "CodexButton", "도감", () => CodexUI.Open(sr.root));
        if (CodexSeen.HasNew || MemoryShards.HasNew) codex.AddComponent<NewGlow>();      // 1.2~ 읽지 않은 기억 조각도          // 1.2~ 발견형 도감: NEW 탭 · 받을 수집 보상
        GameObject evo = UIKit.CloneButton(start, "SkillEvoButton", "스킬 진화", () => SkillEvoCodexUI.Open(sr.root));
        GameObject settings = UIKit.CloneButton(start, "SettingsButton", "설정", () => SettingsUI.Open(sr.root));
        GameObject daily = UIKit.CloneButton(start, "DailyButton", "일일 도전", () => DailyChallenge.Open(sr.root));
        // 1.0.4~: 잠긴 일일 도전은 흐리게 · 「잠김」 표시 · 여는 방법 툴팁 (창 안의 오늘 규칙 · 순위표는 그대로 볼 수 있음)
        bool dailyLocked = !DailyChallenge.Unlocked;

        // 1.8.5~: 큰 게임 시작 버튼 하나 + 그 아래 아이콘 칸 (왼쪽 캐릭터 그림 x -830 ~ -370 과 안 겹치게 폭 740 안)
        // 2.1~: 스킬 진화 도감이 더해져 4 × 2, 2.1.1~: 일일 도전으로 8칸이 다 참
        // 1.4.8~ 가운데 열을 한 격자로: 난이도 줄 · 게임 시작 · 아이콘 칸이 같은 폭(ColW) · 같은 간격(Gap) — 위에서부터
        //   난이도 줄(DiffY) → 난이도 설명 한 줄(HintY) → 게임 시작(StartY) → 잠금 안내 · 영혼의 무게 줄(LockY) → 아이콘 칸 2줄
        sr.anchoredPosition = new Vector2(0f, StartY);
        sr.sizeDelta = new Vector2(ColW, StartH);
        AddIcon(start, "menu_play", new Vector2(-205f, 0f), 64f);
        TMP_Text st = start.GetComponentInChildren<TMP_Text>(true);
        if (st != null) { RectTransform tr = st.rectTransform; tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f); tr.sizeDelta = new Vector2(380f, 90f); tr.anchoredPosition = new Vector2(30f, 0f); }
        InstallContinue(start, st);

        // 1.4.8~ 사용자 「메인화면에 버튼이나 설명창 같은게 너무 많다」: 칸은 한 줄 넷(도감 · 일일 도전 · 설정 · 더 보기)만
        //   캐릭터는 왼쪽 영웅 패널(누르면 영웅 고르기)이 맡고, 스킨 상점 · 스킬 진화 · 튜토리얼 · 영혼 각인 · 계약 보드 · 순위표 · 기록 · 패치노트는 「더 보기」 창으로
        //   게임 종료는 왼쪽 아래 작은 버튼 · 숨긴 칸 버튼은 지우지 않고 꺼 둠 (다른 코드가 이름으로 찾음)
        Transform moreRoot = sr.root;
        GameObject more = UIKit.CloneButton(start, "MoreButton", "더 보기", () => OpenMore(moreRoot));
        if (!TutorialRun.Done) more.AddComponent<NewGlow>();
        Transform clonedIcon = more.transform.Find("Icon");          // 게임 시작에서 복제된 ▶ 그림은 뺌 (칸 밖 왼쪽으로 삐져나옴)
        if (clonedIcon != null) Object.Destroy(clonedIcon.gameObject);
        foreach (GameObject hide in new[] { character, skins, evo, tutorial })
            if (hide != null) hide.SetActive(false);
        // 1.4.8~ 사용자 「게임이 덜 완성된 것 같다」: 칸 격자 대신 게임 시작 아래 같은 폭 버튼을 세로로 (도감 · 일일 도전 · 설정 · 더 보기 · 게임 종료)
        //   글자만 · 같은 폭 · 같은 간격 — 흔한 게임 메인 메뉴 모양 (게임 시작 바로 아래 한 줄은 영혼의 무게 버튼 자리로 비워 둠)
        GameObject[] list = { codex, daily, settings, more, exit };
        // (1.4.8~ 메뉴 뒤 반투명 판은 사용자 「별로」로 뺌 → 글자 자체에 굵기 · 어두운 외곽선 · 그림자 (MenuItemFx))
        float listY = StartY - StartH * 0.5f - (LockRowH + Gap) - ListH * 0.5f;
        for (int i = 0; i < list.Length; i++)
        {
            GameObject go = list[i];
            if (go == null) continue;
            RectTransform r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(ListW, ListH);
            r.anchoredPosition = new Vector2(0f, listY - (ListH + ListGap) * i);
            Image img = go.GetComponent<Image>();
            bool locked = dailyLocked && go == daily;
            if (img != null) img.color = go == exit ? new Color(0.72f, 0.58f, 0.62f) : locked ? new Color(0.42f, 0.4f, 0.47f) : new Color(0.8f, 0.78f, 0.86f);
            if (locked) MarkLocked(go);
            TMP_Text t = go.GetComponentInChildren<TMP_Text>(true);
            if (t == null) continue;
            RectTransform lr = t.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
            lr.sizeDelta = new Vector2(ListW - 40f, ListH - 10f);
            lr.anchoredPosition = Vector2.zero;
            t.enableAutoSizing = true;
            t.fontSizeMin = 20f;
            t.fontSizeMax = 30f;
            t.enableWordWrapping = false;
            if (locked) t.color = new Color(0.62f, 0.6f, 0.6f);
            if (!UiRules.Legacy)
            {
                // UI 개편 P0-4b: 글자만 있던 메뉴 → 불투명 석판 버튼 (사용자 「투명하면 누를 수 있는지 모름」) · 잠긴 일일 도전은 비활성 그림 + 흐린 글자 (툴팁은 그대로)
                UiV2.Restyle(go.GetComponent<Button>(), UiV2.Kind.Secondary, UiTokens.Body);
                if (go == exit) UiV2.Label(go.GetComponent<Button>(), "ui.btn.quit");          // P0-6 「게임 종료」 → 「종료」
                if (locked) { Sprite ds = UiRules.Sprite("ui2_btn_secondary_d"); if (img != null && ds != null) img.sprite = ds; t.color = UiTokens.Text3; }
                continue;
            }
            MenuItemFx.Apply(go.GetComponent<Button>(), 32f, false, locked);
        }
        // 1.4.8~ 게임 시작 · 이어하기도 글자 메뉴 (게임 시작이 가장 크게 · 금빛) · ▶ 그림은 뺌
        foreach (GameObject top in new[] { start, sr.parent.Find("ContinueButton") != null ? sr.parent.Find("ContinueButton").gameObject : null })
        {
            if (top == null) continue;
            Transform ic = top.transform.Find("Icon");
            if (ic != null) Object.Destroy(ic.gameObject);
            TMP_Text tl = top.GetComponentInChildren<TMP_Text>(true);
            if (tl != null) { tl.rectTransform.anchoredPosition = Vector2.zero; tl.rectTransform.sizeDelta = new Vector2(((RectTransform)top.transform).sizeDelta.x - 20f, StartH - 10f); }
            if (!UiRules.Legacy)
            {
                // 주 버튼은 하나: 이어하기가 있으면 이어하기, 없으면 게임 시작 (금빛 54 글자 대신)
                bool hasContinue = sr.parent.Find("ContinueButton") != null;
                bool primary = top.name == "ContinueButton" || !hasContinue;
                UiV2.Restyle(top.GetComponent<Button>(), primary ? UiV2.Kind.Primary : UiV2.Kind.Secondary, UiTokens.Heading);
                if (top == start) UiV2.Label(top.GetComponent<Button>(), "ui.btn.start");      // P0-6 「게임 시작」 → 「시작」
                continue;
            }
            MenuItemFx.Apply(top.GetComponent<Button>(), 54f, true, false);
        }

        InstallDifficulty(sr);
        InstallPoints(sr.parent);
        MenuHeroPanel.Install(sr.parent, sr.root);         // 1.1.9~ 왼쪽 「지금 영웅」 패널 (씬의 고정 그림 자리)
        // 오른쪽 위 보유 포인트 아래: 지난 버전까지 패치노트 (2.1.3~) · 1.4.8~ 자리는 아래 LayoutRightColumn 이 한꺼번에
        Button notes = UIKit.MakeButton(sr.parent, "패치노트", Vector2.zero, new Vector2(RightW, RightBtnH), () => PatchNotesUI.OpenHistory(sr.root), 24f);
        notes.name = "PatchNotesButton";
        SoulBrand.InstallMenuButton(sr.parent, sr.root);       // 1.2~ 패치노트 아래: 영혼 각인 (열린 각인 · 해금 조건)
        LeaderboardUI.InstallMenuButton(sr.parent, sr.root);   // 1.1.5~ 영혼 각인 아래: 스팀 순위표 (무한 모드 · 일일 도전, 체험판은 숨김)
        // 1.4.8~ 이번 판 계약 패널은 메인 화면에서 뺌 → 「더 보기」의 계약 보드 (판을 시작하면 그 판 계약 띠가 뜸)
        DailyRewards.MenuCheck();                              // 1.2~ 연속 출석이 끊겼으면 한 번 알림
        if (daily != null && !dailyLocked && DailyRewards.Streak > 0)
        {
            // 1.2~ 일일 도전 칸 아래: 「연속 n일」 (폰트에 없는 그림 글자는 쓰지 않음)
            TMP_Text streak = UIKit.Text(daily.transform, "", 20f, new Color(0.79f, 0.63f, 1f), new Vector2(0f, -TileH * 0.5f - 16f), new Vector2(TileW + Gap, 26f));     // 1.4.8~ 16 → 20
            streak.fontSizeMin = 20f;
            streak.text = Loc.T("daily.streak.label").Replace("{0}", DailyRewards.Streak.ToString());
            UIKit.Forget(streak);
        }
        // 1.1.9~ 순위표 아래 「기록」 (지난 판 · 영웅별 통계 — 체험판은 숨김: 체험판 기록은 정식판으로 이어지지 않음)
        if (!Demo.On)
        {
            Transform hroot = sr.root;
            Button hist = UIKit.MakeButton(sr.parent, "기록", Vector2.zero, new Vector2(RightW, RightBtnH), () => RunHistoryUI.Open(hroot), 24f);
            hist.name = "RunHistoryButton";
        }
        LayoutRightColumn(sr.parent);
        if (!UiRules.Legacy) LayoutMainV2(sr.parent);
        // 1.4.8~ 오른쪽 위는 포인트만: 패치노트 · 영혼 각인 · 순위표 · 기록 버튼은 만들어 두고 끈 채 「더 보기」에서 누름 (각자 여는 방식 · NEW 표시를 그대로 씀)
        foreach (string n in new[] { "PatchNotesButton", "SoulBrandButton", "LeaderboardButton", "RunHistoryButton" })
        {
            Transform t = sr.parent.Find(n);
            if (t == null) continue;
            if (t.GetComponent<NewGlow>() != null && more.GetComponent<NewGlow>() == null) more.AddComponent<NewGlow>();      // 안에 새 것이 있으면 「더 보기」가 반짝임
            t.gameObject.SetActive(false);
        }
        // 1.0.4~: 저장 복구 알림 → 처음 켰을 때 언어 고르기 → 새 버전이면 패치노트, 하나씩 차례로
        Transform root = sr.root;
        // 1.1.9~ 스팀 덱 첫 실행 자동 설정 알림: 언어 고르기 창이 뜨면 그 안에 한 줄, 아니면 메뉴 아래에 8초
        if (GameSettings.DeckApplied && GameSettings.LanguageChosen)
        {
            Canvas mc = sr.GetComponentInParent<Canvas>();
            MenuToast.Show(mc != null ? mc.rootCanvas.transform : sr.parent, "스팀 덱에 맞춰 설정했습니다 (UI 크기 110% · 게임패드). 설정에서 바꿀 수 있습니다.", 8f);
            GameSettings.DeckNoticed();
        }
        SaveNotice(root, () => LanguagePicker.Open(root, () => PatchNotesUI.OpenOnLaunch(root)));
        ClaimRewards.InstallMenu(root);         // 1.2~ 소급 해금 알림 · 이미 이룬 기록의 보상 「받기」 (위 창들이 닫힌 뒤)
        TelemetryConsentUI.InstallMenu(root);   // 1.4.4~ 플레이 데이터 보내기 동의 (미정일 때만 · 위 창들이 다 닫힌 뒤)
        // 1.0.4~: 튜토리얼 완료 화면 「바로 첫 판 시작」 → 게임 시작 버튼을 대신 누름 (저장된 판이 있으면 원래처럼 한 번 물음)
        if (TutorialRun.ConsumeQuickStart())
        {
            Button sb = start.GetComponent<Button>();
            if (sb != null) sb.onClick.Invoke();
        }
        // 1.1~ 결과 화면 「캐릭터 바꿔 다시」로 왔으면 캐릭터 창을 바로 엶
        if (ResultScreen.ConsumeHeroPick()) CharacterUI.Open(sr.root);
    }

    // UI 개편 (Legacy 끔): 결과 화면 왼쪽 창(다음 목표 + 이번 판 결과 긴 목록)을 없앰 (사용자 「왼쪽 잡스러운 창 치워」)
    //   다음 목표는 메인 메뉴 오른쪽 카드로 · 판 요약은 제목 아래 한 줄 · 긴 빌드 목록은 「빌드」 버튼(가운데 버튼 줄)으로
    //   계약 마무리(보상 확정)는 예전 왼쪽 창이 부르던 그대로 꼭 부름 · 최고 기록 · 새 기록 소리도 그대로
    static void InstallGameOverV2(Transform canvas)
    {
        Contracts.EnsureEnded();
        RunRecords.CommitBest();
        RunRecords.PlayNewRecordSound(canvas);
        // 판 요약 한 줄: 「이번 판 결과」 글의 앞 두 줄(영웅 · 난이도 / 시간 · 레벨 · 처치)에서 서식 태그를 빼고 이어 붙임
        string[] lines = RunStats.Summary(false).Replace("\r", "").Split('\n');
        List<string> parts = new List<string>();
        foreach (string l in lines)
        {
            string plain = System.Text.RegularExpressions.Regex.Replace(l, "<[^>]+>", "").Trim();
            if (plain.Length == 0) continue;
            parts.Add(System.Text.RegularExpressions.Regex.Replace(plain, "\\s{2,}", "  ·  "));
            if (parts.Count >= 2) break;
        }
        string summary = System.Text.RegularExpressions.Regex.Replace(string.Join("  ·  ", parts), @"(\s*·\s*)+", "  ·  ");      // 원래 「 · 」와 겹친 점을 하나로 (캡처: 「거너 · · · 쉬움」)
        if (Contracts.LastRunOn && Contracts.LastRun.Count > 0)
        {
            int done = 0;
            foreach (Contracts.Active a in Contracts.LastRun) if (a.done) done++;
            summary += "\n" + Loc.T("contract.run.title") + "  " + done + " / " + Contracts.LastRun.Count;
        }
        GameObject msg = GameObject.Find("GameOverMessage");
        float titleY = msg != null ? ((RectTransform)msg.transform).anchoredPosition.y : 170f;
        TMP_Text st = UIKit.Text(canvas, "", UiTokens.Body, UiTokens.Text2, new Vector2(0f, titleY - 70f), new Vector2(1200f, 70f));
        st.name = "RunSummaryLine";
        st.fontSizeMin = UiTokens.Small;
        st.text = summary;
        RunStats.BuildDamagePanel(canvas);
        canvas.gameObject.AddComponent<QuickRestart>();
        ResultScreen.BuildRetry();
        ResultScreen.FitTitle();
        ResultScreen.BuildLeftBehind(canvas);
        if (BuildView.HasSnapshot)
        {
            Button bv = UiV2.Btn(canvas, "ui.btn.build", Vector2.zero, new Vector2(ResultScreen.RowBtnW, ResultScreen.RowBtnH), () => BuildView.Open(canvas), UiV2.Kind.Secondary, UiTokens.Body);
            bv.name = "BuildViewButton";          // 가운데 버튼 줄(StackCenter)에 「순위」와 나란히
        }
    }

    // 1.1~ 초록 체크 표시 (짧은 막대 · 긴 막대가 아래 꼭짓점에서 만남, 어두운 외곽선)
    static void CheckMark(Transform parent, Vector2 pos)
    {
        RectTransform box = UIKit.Rect("DoneCheck", parent, pos, new Vector2(40f, 40f));
        Bar(box, 14f, 135f);
        Bar(box, 30f, 50f);
    }

    static void Bar(RectTransform box, float length, float angle)
    {
        Image bar = new GameObject("Stroke", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        RectTransform r = bar.rectTransform;
        r.SetParent(box, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0f, 0.5f);
        r.anchoredPosition = new Vector2(-5f, -10f);
        r.sizeDelta = new Vector2(length, 6f);
        r.localRotation = Quaternion.Euler(0f, 0f, angle);
        bar.color = new Color(0.55f, 1f, 0.55f);
        bar.raycastTarget = false;
        Outline o = bar.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0.05f, 0.15f, 0.05f, 0.9f);
        o.effectDistance = new Vector2(1.5f, -1.5f);
    }

    // 1.0.4~: 잠긴 칸 — 아이콘을 흐리게, 오른쪽 위 「잠김」, 마우스를 올리면 여는 방법
    static void MarkLocked(GameObject tile)
    {
        Transform icon = tile.transform.Find("Icon");
        if (icon != null) icon.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.55f, 0.75f);
        UIKit.EnsureStyle();
        RectTransform pill = UIKit.Rect("LockedPill", tile.transform, Vector2.zero, new Vector2(78f, 28f));
        pill.anchorMin = pill.anchorMax = new Vector2(1f, 1f);
        pill.anchoredPosition = new Vector2(-36f, -14f);
        Image pi = pill.gameObject.AddComponent<Image>();
        pi.sprite = UIKit.ButtonSprite;
        pi.type = Image.Type.Sliced;
        pi.color = new Color(0.2f, 0.19f, 0.23f, 0.95f);
        pi.raycastTarget = false;
        UIKit.Text(pill, "잠김", 18f, new Color(0.85f, 0.82f, 0.78f), Vector2.zero, new Vector2(72f, 26f));
        TooltipTrigger tip = tile.AddComponent<TooltipTrigger>();
        tip.titleProvider = () => Loc.T("일일 도전");
        tip.bodyProvider = () => Loc.T(Demo.On ? "정식판에서 만날 수 있습니다" : "ladder.lock.daily");      // 1.2~ 쉬움 클리어로 열림
    }

    // 1.0.4~: 저장 파일이 깨져서 백업에서 되살렸거나 새 저장으로 시작했으면 한 번 알림 (예전엔 말없이 빈 저장)
    static void SaveNotice(Transform root, System.Action then)
    {
        string problem = Prefs.LoadProblem;
        if (string.IsNullOrEmpty(problem) || GameInput.Auto || Application.isBatchMode) { then?.Invoke(); return; }
        Prefs.ClearLoadProblem();
        RectTransform win = UIKit.Modal(root, "SaveNotice", new Vector2(1000f, 460f), out GameObject blocker);
        UIKit.Text(win, Prefs.Recovered ? "저장 파일을 되살렸습니다" : "저장 파일을 읽지 못했습니다", 40f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 150f), new Vector2(920f, 60f));
        UIKit.Text(win, problem, 26f, new Color(0.92f, 0.88f, 0.8f), new Vector2(0f, 55f), new Vector2(900f, 100f));
        TMP_Text path = UIKit.Text(win, "", 18f, new Color(0.66f, 0.61f, 0.53f), new Vector2(0f, -30f), new Vector2(900f, 50f));
        path.text = Prefs.BrokenPath ?? Prefs.FilePath;          // 경로는 번역하지 않음
        bool closed = false;
        System.Action close = () =>
        {
            if (closed) return;
            closed = true;
            if (blocker != null) Object.Destroy(blocker);
            then?.Invoke();
        };
        Button folder = UIKit.MakeButton(win, "저장 폴더 열기", new Vector2(-160f, -150f), new Vector2(290f, 76f), () => Application.OpenURL("file:///" + Prefs.FolderPath.Replace('\\', '/')), 26f);
        Button okButton = UIKit.MakeButton(win, "확인", new Vector2(160f, -150f), new Vector2(260f, 76f), () => close(), 28f);
        UIKit.ConfirmKeys(win, okButton, folder, -205f);         // 1.1.9~ Enter 로 확인
        UIKit.EscCancel(blocker, () => close());
    }

    // 2.1.9: 저장된 판이 있으면 게임 시작 오른쪽에 「이어하기」 (둘이 합쳐 폭 740 안, 왼쪽 캐릭터 그림과 안 겹치게)
    static void InstallContinue(GameObject start, TMP_Text startLabel)
    {
        RunSave.Data saved = RunSave.Peek();
        if (saved == null) return;
        GameObject cont = UIKit.CloneButton(start, "ContinueButton", "이어하기", () => RunSave.Continue());
        RectTransform sr = (RectTransform)start.transform, cr = (RectTransform)cont.transform;
        // 1.4.8~ 두 버튼이 가운데 열 폭(ColW)을 반씩 · 사이 Gap (아이콘 칸 두 칸씩과 끝이 맞음)
        float half = (ColW - Gap) * 0.5f;
        sr.sizeDelta = cr.sizeDelta = new Vector2(half, StartH);
        sr.anchoredPosition = new Vector2(-(half + Gap) * 0.5f, StartY);
        cr.anchoredPosition = new Vector2((half + Gap) * 0.5f, StartY);
        foreach (GameObject b in new[] { start, cont })
        {
            Transform icon = b.transform.Find("Icon");
            if (icon != null) ((RectTransform)icon).anchoredPosition = new Vector2(-130f, 0f);
            TMP_Text t = b == start ? startLabel : b.GetComponentInChildren<TMP_Text>(true);
            if (t == null) continue;
            t.rectTransform.sizeDelta = new Vector2(250f, 96f);
            t.rectTransform.anchoredPosition = new Vector2(38f, 0f);
            t.enableAutoSizing = true;
            t.fontSizeMin = 16f;
            t.fontSizeMax = 40f;
        }
        TMP_Text ct = cont.GetComponentInChildren<TMP_Text>(true);
        if (ct != null)
        {
            UIKit.Forget(ct);           // 언어를 바꿔도 아래 줄(캐릭터 · 장 · 레벨)이 지워지지 않게
            // 1.0.4~: 난이도도 (이어하면 메뉴에서 고른 난이도가 아니라 저장한 판의 난이도로 함) · 언어를 바꾸면 다시 씀
            System.Action label = () => ct.text = Loc.T("이어하기") + "\n<size=55%>" + Loc.T(CharacterData.Def((CharacterId)saved.character).name)
                      + " · " + GameMode.Name((Difficulty)Mathf.Clamp(saved.difficulty, 0, 3))
                      + (saved.weight > 0 ? " " + Loc.T("weight.short").Replace("{0}", saved.weight.ToString()) : "")      // 1.2~ 영혼의 무게
                      + " · " + (saved.difficulty == (int)Difficulty.Endless      // 1.2.3~ 무한 판은 장이 없어 생존 시간 · 보스 처치 수로
                                 ? Loc.T("생존 시간 ") + EndlessMode.Clock(saved.endlessSeconds) + " · " + Loc.T("보스 처치 ") + saved.endlessBosses
                                 : Chapters.Title(saved.stage))
                      + " · Lv " + saved.level + "</size>";
            label();
            ct.gameObject.AddComponent<LocRefresh>().refresh = label;
        }
        Image img = cont.GetComponent<Image>();
        if (img != null) img.color = new Color(1f, 0.88f, 0.55f);
    }

    // 버튼 안에 도트 아이콘 (Resources/Icons/menu_*.png)
    static void AddIcon(GameObject button, string name, Vector2 pos, float size)
    {
        Sprite s = Resources.Load<Sprite>("Icons/" + name);
        if (s == null) return;
        RectTransform r = UIKit.Rect("Icon", button.transform, pos, new Vector2(size, size));
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = s;
        img.preserveAspect = true;
        img.raycastTarget = false;
    }

    // 1.4.8~ 메인 메뉴 가운데 열 격자 (1080p 기준 · 캔버스 가운데 기준 y)
    //   난이도 줄 89 ~ 147 · 설명 한 줄 51 ~ 81 · 게임 시작 -75 ~ 35 · 잠금 안내 줄 -122 ~ -90 · 아이콘 칸 -137 ~ -395
    //   양옆 패널(영웅 · 이번 판 계약)은 게임 시작 위끝 35 ~ 아이콘 칸 아래끝 -395 에 맞춤 (MenuHeroPanel · MenuContracts)
    public const float TileW = 176f, TileH = 122f, Gap = 14f;
    public const float ColW = TileW * 4f + Gap * 3f;          // 746
    const float DiffY = 118f, DiffH = 58f, HintY = 66f, StartY = -20f, StartH = 110f, LockY = -106f, TileTopY = -137f;
    // 1.4.8~ 세로 메뉴: 게임 시작(-75 아래끝) → 영혼의 무게 줄(LockRowH) → 버튼 다섯 (-138 ~ -480)
    const float ListW = 460f, ListH = 60f, ListGap = 10f, LockRowH = 40f;
    public const float SideTop = StartY + StartH * 0.5f;                  // 35
    public const float SideBottom = -480f;                                // 1.4.8~ 세로 메뉴 아래끝
    public const float ColRight = ColW * 0.5f;                            // 373
    static float ColumnX(int i) => -ColW * 0.5f + TileW * 0.5f + (TileW + Gap) * i;

    // 1.4.8~ 메인 메뉴 분위기 (MenuAmbience): 비네트 · 불티는 메뉴 버튼들 뒤 · 배경 앞
    static void InstallAmbience()
    {
        GameObject start = GameObject.Find("GameStartButton");
        if (start == null) return;
        Transform parent = start.transform.parent;
        TMP_Text title = null;
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (t.text == "Soul Saver") { title = t; break; }
        List<RectTransform> items = new List<RectTransform>();
        foreach (string n in new[] { "GameStartButton", "ContinueButton", "CodexButton", "DailyButton", "SettingsButton", "MoreButton", "ExitButton" })
        {
            Transform t = parent.Find(n);
            if (t != null && t.gameObject.activeSelf) items.Add((RectTransform)t);
        }
        // 맨 앞 형제(가장 뒤에 그려짐)의 바로 다음 = 배경 그림 앞 · 버튼 뒤
        Canvas canvas = start.GetComponentInParent<Canvas>();
        Transform root = canvas != null ? canvas.rootCanvas.transform : parent;
        Transform behind = root.childCount > 1 ? root.GetChild(1) : null;
        MenuAmbience.Install(root, behind, title, items.ToArray());
    }

    // 1.4.8~ 사용자 「메인화면에 있는 게임 제목도 스타일 바꿔」: 금빛 그라데이션(위 밝은 금 → 아래 호박) · 어두운 외곽선 · 넓은 글자 간격,
    //   부제는 작은 크림색 · 넓은 간격 · 양옆에 금줄 (메뉴 글자와 같은 결)
    static void StyleTitle()
    {
        if (!UiRules.Legacy) { StyleTitleV2(); return; }
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t.text == "Soul Saver")
            {
                t.enableVertexGradient = true;
                t.colorGradient = new VertexGradient(new Color(1f, 0.95f, 0.72f), new Color(1f, 0.95f, 0.72f), new Color(0.86f, 0.55f, 0.2f), new Color(0.86f, 0.55f, 0.2f));
                t.color = Color.white;
                t.characterSpacing = 8f;
                Shadow sh = t.GetComponent<Shadow>();
                if (sh == null) sh = t.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.55f);
                sh.effectDistance = new Vector2(0f, -6f);
            }
            else if (t.name == "Subtitle")
            {
                t.color = new Color(0.9f, 0.85f, 0.76f);
                t.fontSize *= 0.8f;
                t.characterSpacing = 10f;
                t.fontStyle |= FontStyles.Bold;
                float w = Mathf.Max(220f, t.preferredWidth) * 0.5f + 34f;
                for (int side = -1; side <= 1; side += 2)
                {
                    RectTransform line = UIKit.Rect("SubtitleLine", t.transform, new Vector2(side * (w + 60f), 0f), new Vector2(110f, 2f));
                    Image li = line.gameObject.AddComponent<Image>();
                    li.color = new Color(0.96f, 0.83f, 0.47f, 0.75f);
                    li.raycastTarget = false;
                }
            }
        }
    }

    // 1.4.8~ 「더 보기」: 메인 화면에서 뺀 메뉴를 한 창에 세로로 (누르면 창을 닫고 그 메뉴를 엶)
    static GameObject moreOpen;
    static void OpenMore(Transform root)
    {
        if (moreOpen != null) return;
        Transform parent = GameObject.Find("GameStartButton") != null ? GameObject.Find("GameStartButton").transform.parent : null;
        System.Collections.Generic.List<(string ko, UnityAction act)> items = new System.Collections.Generic.List<(string, UnityAction)>
        {
            ("스킨 상점", () => SkinShopUI.Open(root)),
            ("스킬 진화", () => SkillEvoCodexUI.Open(root)),
            ("brand.title", () => Press(parent, "SoulBrandButton")),
            ("board.open", () => ContractBoard.Open(root)),
        };
        if (parent != null && parent.Find("LeaderboardButton") != null) items.Add(("lb.button", () => Press(parent, "LeaderboardButton")));
        if (parent != null && parent.Find("RunHistoryButton") != null) items.Add(("기록", () => Press(parent, "RunHistoryButton")));
        items.Add(("패치노트", () => PatchNotesUI.OpenHistory(root)));
        items.Add(("튜토리얼", () => TutorialRun.Begin()));
        const float BtnW = 420f, BtnH = 60f, BtnGap = 12f;
        float h = 150f + items.Count * (BtnH + BtnGap);
        RectTransform win = UIKit.Modal(root, "MoreMenu", new Vector2(520f, h), out moreOpen);
        UIKit.Text(win, "더 보기", 40f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, h * 0.5f - 50f), new Vector2(460f, 56f));
        float y = h * 0.5f - 110f - BtnH * 0.5f;
        foreach (var it in items)
        {
            UnityAction act = it.act;
            UIKit.MakeButton(win, it.ko, new Vector2(0f, y), new Vector2(BtnW, BtnH), () => { CloseMore(); act(); }, 26f);
            y -= BtnH + BtnGap;
        }
        UIKit.EscCancel(moreOpen, CloseMore);
    }

    static void CloseMore()
    {
        if (moreOpen != null) Object.Destroy(moreOpen);
        moreOpen = null;
    }

    // 꺼 둔 메뉴 버튼을 대신 누름 (영혼 각인 · 순위표 · 기록은 각자 여는 방식 · 인자가 달라 버튼에 맡김)
    static void Press(Transform parent, string name)
    {
        Transform t = parent != null ? parent.Find(name) : null;
        Button b = t != null ? t.GetComponent<Button>() : null;
        if (b != null) b.onClick.Invoke();
    }

    // 1.4.8~ 오른쪽 위 열: 포인트 · 패치노트 · 영혼 각인 · 순위표 · 기록을 같은 폭 · 같은 간격으로 위에서부터 (체험판에서 빠진 버튼은 자리도 당겨짐)
    const float RightW = 280f, RightBadgeH = 64f, RightBtnH = 56f, RightGap = 10f, RightMargin = 30f, RightTop = 26f;
    static void LayoutRightColumn(Transform parent)
    {
        float y = -RightTop;
        foreach (string name in new[] { "PointsBadge", "PatchNotesButton", "SoulBrandButton", "LeaderboardButton", "RunHistoryButton" })
        {
            RectTransform r = parent.Find(name) as RectTransform;
            if (r == null) continue;
            float h = name == "PointsBadge" ? RightBadgeH : RightBtnH;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
            r.sizeDelta = new Vector2(RightW, h);
            r.anchoredPosition = new Vector2(-RightMargin, y);
            TMP_Text t = name == "PointsBadge" ? null : r.GetComponentInChildren<TMP_Text>(true);
            if (t != null)
            {
                t.rectTransform.sizeDelta = new Vector2(RightW - 24f, h - 12f);
                t.fontSizeMax = 24f;
                t.fontSizeMin = 20f;
            }
            y -= h + RightGap;
        }
    }

    // 오른쪽 위: 보유 포인트 (스킨 상점에서 쓰면 바로 줄어듦)
    static void InstallPoints(Transform parent)
    {
        // 1.4.8~ 아래 버튼 열과 같은 폭(RightW) · 안쪽 여백 16 (자리는 LayoutRightColumn)
        RectTransform box = UIKit.Rect("PointsBadge", parent, Vector2.zero, new Vector2(RightW, RightBadgeH));
        box.anchorMin = box.anchorMax = box.pivot = new Vector2(1f, 1f);
        Image bg = box.gameObject.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.11f, 0.72f);
        bg.raycastTarget = true;        // 툴팁을 받으려고
        // 1.0.4~: 판 안의 코인과 같은 동전 그림이라 무엇이 다음 판에도 남는지 헷갈렸음 → 그림 대신 「포인트」 글자 + 마우스를 올리면 설명
        float inner = RightW * 0.5f - 16f;
        UIKit.Text(box, "포인트", 22f, new Color(0.8f, 0.76f, 0.7f), new Vector2(-inner + 52f, 0f), new Vector2(104f, 40f), TextAlignmentOptions.Left).fontSizeMin = 20f;
        TMP_Text t = UIKit.Text(box, "", 30f, new Color(0.96f, 0.83f, 0.47f), new Vector2(inner - 80f, 0f), new Vector2(160f, 50f), TextAlignmentOptions.Right);
        t.fontSizeMin = 20f;
        box.gameObject.AddComponent<MenuPoints>().text = t;
        TooltipTrigger tip = box.gameObject.AddComponent<TooltipTrigger>();
        tip.titleProvider = () => Loc.T("포인트");
        tip.bodyProvider = () => Loc.T("적을 처치하면 쌓이고 판이 끝나도 남습니다. 메인 메뉴에서 새 영웅과 스킨을 사는 데 씁니다. (판 안에서 떠돌이 상점에 쓰는 코인과는 다릅니다)");
    }

    // 게임 시작 버튼 위: 쉬움 · 보통 · 어려움 · 무한 (잠긴 난이도는 회색, 누를 수 없음)
    // UI 개편 P1 (Legacy 끔): 메인 메뉴 배치 (docs/art/ui-v2-preview.png 왼쪽) — 왼쪽 여백 120 에 한 열(폭 420): 제목 · 부제 → 이어하기 · 시작
    //   → 난이도 탭 한 줄 · 영혼의 무게 → 도감 · 일일 도전 · 설정 · 더 보기 · 종료, 영웅은 오른쪽 (카펫 가운데를 비움)
    //   왼쪽 · 오른쪽 끝에 붙여(앵커) UI 크기 110 · 120% 로 논리 폭이 줄어도 화면 안 · 이름은 그대로 (다른 코드 · QA 가 이름으로 찾음)
    // 2026-10-22~ 사용자 「가운데가 텅 비고 UI 가 작다」: 열 420 → 480 · 목록 글자 26 → 34 · 줄 54 → 64, 영웅은 오른쪽 끝 → 카펫 가운데(무대)
    // 2026-10-22~ 사용자 「메뉴 버튼들과 캐릭터 위치를 바꾸자」: 메뉴 열은 가운데(카펫 위 · 가운데 앵커), 영웅은 왼쪽(예전 메뉴 자리) · 제목은 왼쪽 위 그대로
    const float TitleX = 120f, MainW = 480f, MainRowH = 64f, MainRowGap = 12f;
    const float MainX = -MainW * 0.5f;          // 가운데 앵커 기준 열 왼쪽 끝
    static RectTransform LeftPlace(Transform parent, string name, float y, float h, float w = MainW, float font = 0f)
    {
        RectTransform r = parent.Find(name) as RectTransform;
        if (r == null || !r.gameObject.activeSelf) return null;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0f, 0.5f);
        r.anchoredPosition = new Vector2(MainX, y);
        r.sizeDelta = new Vector2(w, h);
        TMP_Text t = r.GetComponentInChildren<TMP_Text>(true);
        if (t != null && t.transform.parent == r)
        {
            RectTransform tr = t.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
            tr.anchoredPosition = Vector2.zero;
            tr.sizeDelta = new Vector2(w - UiTokens.S4 * 2f, h - UiTokens.S1);
            if (font > 0f) { t.enableAutoSizing = true; t.fontSize = t.fontSizeMax = font; t.fontSizeMin = UiTokens.MinSize(font); }
        }
        return r;
    }

    static bool mainLaidOut;            // 메인 메뉴 배치를 마쳤는지 (난이도를 바꿀 때 다시 배치 · InstallMainMenu 가 처음에 지움)

    static void LayoutMainV2(Transform parent)
    {
        mainLaidOut = true;
        // 제목 · 부제 (장면 글자 · 가운데 앵커 → 왼쪽)
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            bool title = t.text == "Soul Saver", sub = t.name == "Subtitle";
            if (!title && !sub) continue;
            RectTransform r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(TitleX, title ? 390f : 312f);
            r.sizeDelta = new Vector2(title ? 1000f : 700f, title ? 140f : 34f);
            t.alignment = TextAlignmentOptions.Left;
            if (title) { t.enableAutoSizing = true; t.fontSizeMax = Mathf.Min(t.fontSizeMax, 120f); t.fontSizeMin = 60f; }
        }
        // 2026-10-22~ 사용자 「시작은 왜 저기 있냐 · 배치가 좀 그래」: 고르는 순서대로 한 덩어리 — 난이도 → 시작(이어하기가 있으면 반씩 나란히) → (영혼의 무게) → 목록
        //   덩어리 위끝은 배경 문(포털) 아래끝 밑 (예전엔 「시작」이 문을 가리고 혼자 떨어져 있었음 — 캡처 확인) · 「시작」과 목록 사이만 넓게 띄워 두 묶음으로
        float y = 150f;
        Transform plate = parent.Find("DiffPlate");
        if (plate != null) plate.gameObject.SetActive(false);
        float tabW = (MainW - UiTokens.S1 * 3f) / 4f;
        const float TabH = 54f, StartH2 = 76f;
        y -= TabH * 0.5f;
        for (int i = 0; i < 4; i++)
        {
            RectTransform r = parent.Find("Difficulty_" + (Difficulty)i) as RectTransform;
            if (r == null) continue;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.anchoredPosition = new Vector2(MainX + i * (tabW + UiTokens.S1), y);
            r.sizeDelta = new Vector2(tabW, TabH);
            TMP_Text t = r.GetComponentInChildren<TMP_Text>(true);
            if (t != null) { t.rectTransform.sizeDelta = new Vector2(tabW - 12f, 46f); t.enableAutoSizing = true; t.fontSizeMin = 18f; t.fontSizeMax = UiTokens.Body; }
            Transform ul = r.Find("Underline");
            if (ul is RectTransform ur) { ur.anchoredPosition = new Vector2(0f, -18f); ur.sizeDelta = new Vector2(tabW * 0.5f, UiRules.Dot); }
        }
        y -= TabH * 0.5f + UiTokens.S1 + StartH2 * 0.5f;
        bool cont = parent.Find("ContinueButton") != null && parent.Find("ContinueButton").gameObject.activeSelf;
        if (cont)
        {
            float half = (MainW - UiTokens.S2) * 0.5f;
            RectTransform cr = LeftPlace(parent, "ContinueButton", y, StartH2, half, UiTokens.Body);
            RectTransform sr2 = LeftPlace(parent, "GameStartButton", y, StartH2, half, UiTokens.Heading);
            if (sr2 != null) sr2.anchoredPosition = new Vector2(MainX + half + UiTokens.S2, y);
            if (cr != null) cr.SetAsLastSibling();
        }
        else LeftPlace(parent, "GameStartButton", y, StartH2, MainW, UiTokens.Heading);
        y -= StartH2 * 0.5f;
        // 영혼의 무게 (해금했으면 시작 바로 아래 자리를 늘 잡음 · 어려움을 고르면 켜짐)
        RectTransform wr = parent.Find("SoulWeightButton") as RectTransform;
        bool weightOn = wr != null && SoulWeight.Unlocked && GameMode.Current == Difficulty.Hard;      // 보일 때(어려움)만 자리 · 난이도를 바꾸면 다시 배치 (늘 비워 두면 시작과 목록 사이가 80 벌어졌음 — 캡처 확인)
        if (wr != null)
        {
            wr.anchorMin = wr.anchorMax = new Vector2(0.5f, 0.5f);
            wr.pivot = new Vector2(0f, 0.5f);
            wr.anchoredPosition = new Vector2(MainX, y - UiTokens.S1 - 16f);
            wr.sizeDelta = new Vector2(MainW, 32f);
        }
        if (weightOn) y -= UiTokens.S1 + 32f;
        y -= UiTokens.S6 + MainRowH * 0.5f;        // 「시작」 묶음과 목록 사이만 넓게
        foreach (string n in new[] { "CodexButton", "DailyButton", "SettingsButton", "MoreButton", "ExitButton" })
        {
            if (LeftPlace(parent, n, y, MainRowH, MainW, UiTokens.Heading) == null) continue;
            y -= MainRowH + MainRowGap + (n == "DailyButton" && DailyRewards.Streak > 0 ? 18f : 0f);       // 일일 도전 아래 「연속 n일」 자리
        }
        // 영웅은 카펫 정가운데 · 문(포털) 바로 아래 — 배경의 가운데 축(문 · 카펫)에 맞춰 섬 (+150 으로 비켜 서면 어중간했음: 캡처 확인)
        GameObject hpo = GameObject.Find("HeroPanel");          // 씬 고정 그림의 부모에 붙어 있을 수 있어 장면 전체에서 (못 찾으면 메뉴 열과 겹침)
        Transform hp = hpo != null ? hpo.transform : null;
        if (hp is RectTransform hr)
        {
            hr.anchorMin = hr.anchorMax = new Vector2(0f, 0.5f);
            hr.pivot = new Vector2(0.5f, 0.5f);
            hr.anchoredPosition = new Vector2(TitleX + MainW * 0.5f, -70f);     // 왼쪽 끝 120 + 열 폭 절반 (예전 메뉴 열 가운데 · 제목과 같은 왼쪽 줄)
            hr.sizeDelta = new Vector2(Mathf.Max(hr.sizeDelta.x, 540f), 700f);          // 512 그림 + 이름 · 영웅 변경
            MenuHeroPanel.Refresh();             // 새 크기로 안쪽 그림 · 이름 자리를 다시
        }
        InstallNewsCard(parent);
        InstallGoalsCard(parent);
    }

    // 오른쪽 열 「다음 목표」 카드 (사용자 「아직 허전」) — 새 소식 아래 · 아래끝은 왼쪽 「종료」와 맞춤, 결과 화면과 같은 목표 셋 (이름 · 막대 · 수치)
    //   목표가 없으면(체험판 · 다 이룸) 만들지 않음 · 언어를 바꾸면 다시 씀
    static void InstallGoalsCard(Transform parent)
    {
        List<(string label, string detail, float progress)> goals = ResultScreen.MenuGoals(3);
        if (goals.Count == 0 || parent.Find("GoalsCard") != null) return;
        const float W = 480f, Pad = 30f, HeadH = 44f, RowH = 96f;
        float h = Pad + HeadH + RowH * goals.Count + Pad - 12f;
        RectTransform card = UIKit.Rect("GoalsCard", parent, Vector2.zero, new Vector2(W, h));
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(1f, 1f);
        card.anchoredPosition = new Vector2(-UiTokens.Edge, -282f - 176f - UiTokens.S2);
        UiV2.Slab(card, "ui2_panel", UiTokens.Panel).raycastTarget = false;
        TMP_Text head = UIKit.Text(card, "result.goal.title", UiTokens.Small, UiTokens.Text3, new Vector2(0f, h * 0.5f - Pad - 12f), new Vector2(W - Pad * 2f, 28f), TextAlignmentOptions.Left);
        head.fontSizeMin = UiTokens.Small;
        List<(TMP_Text label, TMP_Text detail)> rows = new List<(TMP_Text, TMP_Text)>();
        Sprite inset = UiRules.Sprite("ui2_inset"), fill = UiRules.Sprite("ui2_fill");
        for (int i = 0; i < goals.Count; i++)
        {
            float y = h * 0.5f - Pad - HeadH - RowH * i;            // 이 줄 위끝
            TMP_Text label = UIKit.Text(card, "", UiTokens.Body, UiTokens.Text1, new Vector2(0f, y - 18f), new Vector2(W - Pad * 2f, 34f), TextAlignmentOptions.Left);
            label.enableWordWrapping = false; label.overflowMode = TextOverflowModes.Ellipsis; label.fontSizeMin = UiTokens.Small;
            RectTransform bar = UIKit.Rect("GoalBar", card, new Vector2(0f, y - 50f), new Vector2(W - Pad * 2f, 18f));
            Image bi = bar.gameObject.AddComponent<Image>();
            if (inset != null) { bi.sprite = inset; bi.type = Image.Type.Tiled; bi.pixelsPerUnitMultiplier = 1f; } else bi.color = UiTokens.Sunken;
            bi.raycastTarget = false;
            RectTransform fr = UIKit.Rect("Fill", bar, Vector2.zero, Vector2.zero);
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = new Vector2(Mathf.Clamp(goals[i].progress, 0.02f, 1f), 1f);
            fr.offsetMin = new Vector2(3f, 3f); fr.offsetMax = new Vector2(-3f, -3f);
            Image fi = fr.gameObject.AddComponent<Image>();
            if (fill != null) { fi.sprite = fill; fi.type = Image.Type.Sliced; fi.pixelsPerUnitMultiplier = 1f; }
            fi.color = goals[i].progress >= 1f ? UiTokens.Accent : UiTokens.GaugeSoul;
            fi.raycastTarget = false;
            TMP_Text detail = UIKit.Text(card, "", UiTokens.Small, UiTokens.Text3, new Vector2(0f, y - 78f), new Vector2(W - Pad * 2f, 28f), TextAlignmentOptions.Right);
            detail.enableWordWrapping = false; detail.overflowMode = TextOverflowModes.Ellipsis; detail.fontSizeMin = 18f;
            rows.Add((label, detail));
        }
        System.Action write = () =>
        {
            List<(string label, string detail, float progress)> now = ResultScreen.MenuGoals(3);
            for (int i = 0; i < rows.Count && i < now.Count; i++)
            {
                rows[i].label.text = now[i].label;
                rows[i].detail.text = now[i].detail;
                UIKit.Forget(rows[i].label); UIKit.Forget(rows[i].detail);
            }
        };
        write();
        card.gameObject.AddComponent<LocRefresh>().refresh = write;
    }

    // 오른쪽 아래 「새 소식」 카드 하나 (사용자 「오른쪽이 텅 비지 않을까」) — 최신 공지 제목 + 패치노트 버튼, 판 하나 · 버튼 하나만 (설명창을 늘리지 않음)
    static void InstallNewsCard(Transform parent)
    {
        string title = PatchNotesUI.LatestTitle();
        if (string.IsNullOrEmpty(title) || parent.Find("NewsCard") != null) return;
        // 오른쪽 열 위쪽 (포인트 바로 아래 · 위끝을 메뉴 열 「시작」 위끝에 맞춤) — 왼쪽 메뉴 열과 좌우 균형 (오른쪽 아래 구석에 따로 떠 있었음: 캡처 확인)
        const float W = 480f, H = 176f;
        RectTransform card = UIKit.Rect("NewsCard", parent, Vector2.zero, new Vector2(W, H));
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(1f, 1f);
        card.anchoredPosition = new Vector2(-UiTokens.Edge, -282f);
        UiV2.Slab(card, "ui2_panel", UiTokens.Panel).raycastTarget = false;
        TMP_Text head = UIKit.Text(card, "새 소식", UiTokens.Small, UiTokens.Text3, new Vector2(0f, H * 0.5f - 34f), new Vector2(W - 60f, 28f), TextAlignmentOptions.Left);
        head.fontSizeMin = UiTokens.Small;
        TMP_Text t = UIKit.Text(card, "", UiTokens.Body, UiTokens.Text1, new Vector2(0f, 6f), new Vector2(W - 60f, 64f), TextAlignmentOptions.TopLeft);
        t.fontSizeMin = UiTokens.Small;
        t.text = title;
        System.Action refresh = () => t.text = PatchNotesUI.LatestTitle();
        t.gameObject.AddComponent<LocRefresh>().refresh = refresh;
        Transform root = parent.root;
        Button b = UiV2.Btn(card, "패치노트", new Vector2(W * 0.5f - 30f - 100f, -H * 0.5f + 24f + 24f), new Vector2(200f, 48f), () => PatchNotesUI.OpenHistory(root), UiV2.Kind.Secondary, UiTokens.Body);
        b.name = "NewsPatchNotesButton";
    }


    // UI 개편 P0-4b: 제목은 뼈색 그대로 (그라데이션 · 그림자 · 넓은 자간 · 숨쉬기 없음 — 도트 로고는 P3 전까지 글자), 부제는 작은 흐린 글자 · 양옆 금줄 없음
    static void StyleTitleV2()
    {
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t.text == "Soul Saver")
            {
                t.enableVertexGradient = false;
                t.color = UiTokens.Text1;
                t.characterSpacing = 2f;
                foreach (Shadow s in t.GetComponents<Shadow>()) Object.Destroy(s);
            }
            else if (t.name == "Subtitle")
            {
                t.color = UiTokens.Text3;
                t.fontStyle &= ~FontStyles.Bold;
                t.fontSize = t.fontSizeMax = UiTokens.Small;
                t.characterSpacing = 4f;
            }
        }
    }

    static void InstallDifficulty(RectTransform start)
    {
        Transform parent = start.parent;
        // 2.1.9: 이어하기가 있으면 게임 시작 버튼이 왼쪽으로 가므로, 난이도 줄은 가운데(x 0)에 고정
        List<Button> buttons = new List<Button>();
        // 1.0.4~: 이 줄은 고른 난이도 설명 (예전 잠금 안내 자리), 잠금 안내는 게임 시작 버튼 아래 빈 줄로
        // 1.4.8~ 설명이 게임 시작 버튼 위끝과 겹쳤음 → 난이도 줄(아래끝 DiffY - 29)과 게임 시작(위끝 StartY + 55) 사이 빈 줄 한가운데 한 줄
        //   폭 1280: 양옆 패널(위끝 StartY + 55)보다 위라 넓게 써도 됨 — en 이 가장 길어 22 → 20 까지 줄고 줄바꿈 없음
        TMP_Text hint = UIKit.Text(parent, "", 22f, new Color(0.8f, 0.76f, 0.7f), new Vector2(0f, HintY), new Vector2(1280f, 30f));
        hint.fontSizeMin = 20f;
        hint.enableWordWrapping = false;
        hint.overflowMode = TextOverflowModes.Ellipsis;
        TMP_Text lockHint = UIKit.Text(parent, "", 20f, new Color(0.66f, 0.61f, 0.53f), new Vector2(0f, LockY), new Vector2(1000f, 30f));
        lockHint.fontSizeMin = 20f;
        lockHint.enableWordWrapping = false;
        lockHint.overflowMode = TextOverflowModes.Ellipsis;
        for (int i = 0; i < 4; i++)
        {
            Difficulty d = (Difficulty)i;
            Button b = UIKit.MakeButton(parent, GameMode.Names[i], new Vector2(ColumnX(i), DiffY), new Vector2(TileW, DiffH), () =>
            {
                GameMode.Current = d;
                HighlightDifficulty(buttons, hint, lockHint);
            }, 26f);
            b.name = "Difficulty_" + d;
            buttons.Add(b);
            // 1.2~ 난이도 사다리: 마우스를 올리면 클리어하면 무엇이 열리는지 (잠긴 난이도도 미리 보임)
            TooltipTrigger tip = b.gameObject.AddComponent<TooltipTrigger>();
            tip.titleProvider = () => GameMode.Name(d);
            tip.bodyProvider = () => LadderTip(d);
        }
        // 1.2~ 영혼의 무게: 어려움을 클리어하면 잠금 안내 줄 자리(이때는 늘 비어 있음)에 버튼 · 어려움을 고를 때만 보임
        SoulWeight.InstallMenu(parent, new Vector2(0f, LockY));
        HighlightDifficulty(buttons, hint, lockHint);
        hint.gameObject.AddComponent<LocRefresh>().refresh = () => HighlightDifficulty(buttons, hint, lockHint);
        // 1.4.8~ 사용자 「설명창 같은게 너무 많다」: 난이도 설명 · 잠금 안내 두 줄은 화면에서 빼고 난이도 버튼 툴팁(LadderTip)으로
        hint.gameObject.SetActive(false);
        lockHint.gameObject.SetActive(false);
    }

    // 1.5.0~ 난이도 탭 뒤 어두운 도트 판 (빨간 카펫 위에서도 탭이 또렷하게) · 한 번만 만듦
    static void EnsureDiffPlate(List<Button> buttons)
    {
        Transform parent = buttons[0].transform.parent;
        if (parent == null || parent.Find("DiffPlate") != null) return;
        RectTransform pr = UIKit.Rect("DiffPlate", parent, new Vector2(0f, DiffY), new Vector2(ColW + 36f, DiffH + 26f));
        Image pi = pr.gameObject.AddComponent<Image>();
        if (UIKit.PanelSprite != null) { pi.sprite = UIKit.PanelSprite; pi.type = Image.Type.Sliced; }
        pi.color = new Color(0.1f, 0.08f, 0.14f, 0.88f);
        if (!UiRules.Legacy && UiRules.Sprite("ui2_panel") != null) { pi.sprite = UiRules.Sprite("ui2_panel"); pi.type = Image.Type.Tiled; pi.pixelsPerUnitMultiplier = 1f; pi.color = Color.white; }
        pi.raycastTarget = false;
        pr.SetSiblingIndex(buttons[0].transform.GetSiblingIndex());     // 탭들 뒤
    }

    // 1.2~ 난이도 버튼 툴팁: 클리어 보상 (체험판은 쉬움만이라 정식판 안내) · 이미 깼으면 한 줄 더
    static string LadderTip(Difficulty d)
    {
        if (Demo.On && d != Difficulty.Easy) return Loc.T("정식판에서 만날 수 있습니다");
        string[] keys = { "ladder.tip.easy", "ladder.tip.normal", "ladder.tip.hard", "ladder.tip.endless" };
        // 1.4.8~ 화면에서 뺀 난이도 설명 · 포인트 배율 · 잠금 안내를 툴팁 맨 위에
        string s = Loc.T(DifficultyDescs[Mathf.Clamp((int)d, 0, DifficultyDescs.Length - 1)]);
        float pointRate = GameMode.PointRateOf(d);
        if (pointRate > 1f) s += " · " + Loc.T("result.diffpoints").Replace("{0}", pointRate.ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture));
        if (!GameMode.IsUnlocked(d)) s += "\n<color=#C9B98A>" + Loc.T(d == Difficulty.Endless ? "ladder.lock.endless" : "쉬움을 클리어하면 보통 · 어려움이 열립니다") + "</color>";
        s += "\n\n" + Loc.T(keys[Mathf.Clamp((int)d, 0, keys.Length - 1)]);
        if (d == Difficulty.Endless) s += "\n" + Loc.T("ladder.tip.endless.points");
        else if (!Demo.On && GameMode.HasCleared(d)) s += "\n<color=#7CFF9A>" + Loc.T("ladder.tip.cleared") + "</color>";
        return s;
    }

    // 1.5.0~ 난이도 색 (고른 탭의 틀 · 글자 · 표시): 쉬움 초록 · 보통 금 · 어려움 빨강 · 무한 보라
    static readonly Color[] DiffColors =
    {
        new Color(0.55f, 0.95f, 0.55f), new Color(0.98f, 0.82f, 0.42f), new Color(1f, 0.45f, 0.4f), new Color(0.78f, 0.58f, 1f),
    };

    static void HighlightDifficulty(List<Button> buttons, TMP_Text hint, TMP_Text lockHint)
    {
        // 1.5.0~ 사용자 「난이도 선택 버튼 더 잘 보이게」: 글자만 있던 탭(빨간 카펫 위에서 묻힘) → 어두운 판 위 도트 버튼 틀
        //   고른 난이도는 난이도 색으로 밝은 틀 · 글자 · 위아래 마름모 · 조금 크게, 나머지는 어두운 틀 · 밝은 글자, 잠긴 것은 더 어둡게
        UIKit.EnsureStyle();                // 도트 버튼 · 판 그림을 먼저 불러 둠
        if (buttons.Count > 0) EnsureDiffPlate(buttons);
        if (!UiRules.Legacy) HighlightDifficultyV2(buttons);
        else
        for (int i = 0; i < buttons.Count; i++)
        {
            Difficulty d = (Difficulty)i;
            bool open = GameMode.IsUnlocked(d), on = open && GameMode.Current == d;
            Color dc = DiffColors[Mathf.Clamp(i, 0, DiffColors.Length - 1)];
            buttons[i].interactable = open;
            Image img = buttons[i].GetComponent<Image>();
            if (UIKit.ButtonSprite != null)
            {
                img.sprite = UIKit.ButtonSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = on ? Color.Lerp(Color.white, dc, 0.55f) : open ? new Color(0.36f, 0.32f, 0.42f, 0.95f) : new Color(0.2f, 0.19f, 0.23f, 0.85f);
            ColorBlock cb = buttons[i].colors;
            cb.disabledColor = Color.white;     // 잠긴 탭 색은 위에서 정함 (버튼 기본 회색이 덮지 않게)
            buttons[i].colors = cb;
            buttons[i].transform.localScale = on ? new Vector3(1.06f, 1.06f, 1f) : Vector3.one;
            TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>();
            label.color = on ? Color.Lerp(dc, Color.white, 0.35f) : open ? new Color(0.93f, 0.9f, 0.86f) : new Color(0.5f, 0.48f, 0.52f);
            label.fontSize = 30f;
            label.enableAutoSizing = false;
            label.fontStyle |= FontStyles.Bold;
            Transform ul = buttons[i].transform.Find("Underline");
            if (ul == null)
            {
                RectTransform ur = UIKit.Rect("Underline", buttons[i].transform, new Vector2(0f, -DiffH * 0.5f + 6f), new Vector2(TileW * 0.62f, 3f));
                Image ui = ur.gameObject.AddComponent<Image>();
                ui.raycastTarget = false;
                ul = ur;
            }
            ul.GetComponent<Image>().color = dc;
            ul.gameObject.SetActive(on);
            // 고른 탭 위아래 마름모 (난이도 색)
            for (int k = 0; k < 2; k++)
            {
                string gem = k == 0 ? "GemTop" : "GemBottom";
                Transform g = buttons[i].transform.Find(gem);
                if (g == null)
                {
                    RectTransform gr = UIKit.Rect(gem, buttons[i].transform, new Vector2(0f, (k == 0 ? 1f : -1f) * (DiffH * 0.5f + 2f)), new Vector2(12f, 12f));
                    gr.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    Image gi = gr.gameObject.AddComponent<Image>();
                    gi.raycastTarget = false;
                    g = gr;
                }
                g.GetComponent<Image>().color = dc;
                g.gameObject.SetActive(on);
            }
        }
        // 고른 난이도가 무엇이 다른지 (1.0.4~, 예전엔 잠금 안내만 · 다 열리면 비었음) + 잠긴 난이도가 있으면 여는 방법
        // 체험판은 쉬움만이라 「쉬움을 클리어하면」 대신 정식판 안내 (1.0.4~)
        string lockText = Demo.On ? "보통 · 어려움 · 무한 모드는 정식판에서 만날 수 있습니다"
                        : !GameMode.IsUnlocked(Difficulty.Normal) ? "쉬움을 클리어하면 보통 · 어려움이 열립니다"
                        : !GameMode.IsUnlocked(Difficulty.Endless) ? (GameMode.Current == Difficulty.Hard ? "ladder.lock.endless.hard" : "ladder.lock.endless")     // 1.2~ 보통 클리어로 무한 모드
                        : SoulWeight.Unlocked && GameMode.Current != Difficulty.Hard ? "weight.menu.hardonly"     // 1.2~ (어려움이면 이 자리에 무게 버튼)
                        : "";
        hint.text = Loc.T(DifficultyDescs[Mathf.Clamp((int)GameMode.Current, 0, DifficultyDescs.Length - 1)]);
        float pointRate = GameMode.PointRateOf(GameMode.Current);      // 1.1~ 포인트 배율 (보통 ×1.3 · 어려움 ×1.5)
        if (pointRate > 1f) hint.text += " · " + Loc.T("result.diffpoints").Replace("{0}", pointRate.ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture));
        lockHint.text = Loc.T(lockText);
    }

    // UI 개편 P0-4b: 난이도 탭 = 석판 버튼 · 고른 탭은 올림 그림 + 뼈색 글자 + 아래 1도트 밑줄 (청록, 어려움만 위험색 · 무한은 영혼색)
    //   4색 칠 · 1.06배 확대 · 위아래 마름모 · 굵게 없음 · 잠긴 탭은 비활성 그림 (툴팁은 그대로)
    static void HighlightDifficultyV2(List<Button> buttons)
    {
        Sprite on2 = UiRules.Sprite("ui2_btn_secondary_h"), off2 = UiRules.Sprite("ui2_btn_secondary_n");
        for (int i = 0; i < buttons.Count; i++)
        {
            Difficulty d = (Difficulty)i;
            bool open = GameMode.IsUnlocked(d), on = open && GameMode.Current == d;
            Button b = buttons[i];
            if (b.GetComponent<UiFocusMark>() == null) UiV2.Restyle(b, UiV2.Kind.Secondary, UiTokens.Body);
            b.interactable = open;
            b.transform.localScale = Vector3.one;
            Image img = b.GetComponent<Image>();
            if (img != null && on2 != null && off2 != null) img.sprite = on ? on2 : off2;
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            if (label != null) label.color = on ? UiTokens.Text1 : open ? UiTokens.Text2 : UiTokens.Text3;
            Transform ul = b.transform.Find("Underline");
            if (ul == null) ul = UiV2.Line(b.transform, "Underline", new Vector2(0f, -DiffH * 0.5f + 9f), TileW * 0.5f, true).transform;
            Image ui = ul.GetComponent<Image>();
            if (ui != null) { ui.sprite = null; ui.color = d == Difficulty.Hard ? UiTokens.Danger : d == Difficulty.Endless ? UiTokens.GaugeSoul : UiTokens.Accent; }     // 단색 (그림에 곱하면 탁해짐)
            ul.gameObject.SetActive(on);
        }
        // 어려움을 고르면 영혼의 무게 줄이 생기고 · 다른 난이도면 없어지므로 메뉴 열을 다시 배치
        if (mainLaidOut && buttons.Count > 0 && buttons[0] != null && buttons[0].transform.parent != null) LayoutMainV2(buttons[0].transform.parent);
    }

    // 난이도 설명 (1.0.4~ · 수치는 GameMode 배율 그대로 — 보통: 적 체력 1.15 · 적 피해 1.25 · 예고 0.85 · 보상 1.05,
    //   어려움: 1.3 · 1.5 · 0.7 · 1.1, 무한: 보상 1.3 · 시간이 지날수록 강해짐 · 보스가 주기마다)
    static readonly string[] DifficultyDescs =
    {
        "기본 난이도 · 처음이라면 여기서 시작하세요",
        "적 체력 +15% · 적 공격력 +25% · 공격 예고 -15% · 더 많이 몰려옴 · 보상 +5%",
        "적 체력 +30% · 적 공격력 +50% · 공격 예고 -30% · 훨씬 많이 몰려옴 · 보상 +10%",
        "끝없이 버티는 모드 · 시간이 지날수록 적이 강해지고 보스가 계속 나옴 · 보상 +30%",
    };

    static void InstallEsc()
    {
        ESCmenu esc = Object.FindFirstObjectByType<ESCmenu>(FindObjectsInactive.Include);
        if (esc == null || esc.escMenu == null) return;
        if (esc.escMenu.transform.Find("SettingsButton") != null) return;
        Button[] buttons = esc.escMenu.GetComponentsInChildren<Button>(true);
        if (buttons.Length == 0) return;

        // 가장 아래 버튼 밑에 설정 버튼
        Button lowest = buttons[0];
        foreach (Button b in buttons)
            if (((RectTransform)b.transform).anchoredPosition.y < ((RectTransform)lowest.transform).anchoredPosition.y) lowest = b;
        RectTransform lr = (RectTransform)lowest.transform;
        GameObject settings = UIKit.CloneButton(lowest.gameObject, "SettingsButton", "설정", () => SettingsUI.Open(esc.escMenu.transform.root));
        RectTransform r = (RectTransform)settings.transform;
        r.anchoredPosition = lr.anchoredPosition - new Vector2(0f, lr.sizeDelta.y + 24f);

        // 창(560 높이) 안에 버튼 셋을 고른 간격으로 다시 놓고, 재개 안내는 맨 아래 버튼 밑에 (설정 버튼과 겹치던 문제)
        List<RectTransform> column = new List<RectTransform>();
        foreach (Button b in esc.escMenu.GetComponentsInChildren<Button>(true))
            if (b.transform.parent == esc.escMenu.transform) column.Add((RectTransform)b.transform);      // 방금 만든 설정 버튼 포함
        column.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
        // 1.0.4~: 맨 아래 줄에 「다시 시작」 · 「게임 종료」 를 반씩 나란히 (확인 창을 거침) — 줄이 하나 늘어 간격을 좁힘 (창 560 · 제목 아래 198 ~ 안내 -272)
        const float Top = 150f, Step = 106f, H = 96f;
        for (int i = 0; i < column.Count; i++)
        {
            column[i].sizeDelta = new Vector2(column[i].sizeDelta.x, H);
            column[i].anchoredPosition = new Vector2(0f, Top - Step * i);
        }
        float rowY = Top - Step * column.Count;
        float w = lr.sizeDelta.x, half = (w - 16f) * 0.5f;
        // 1.3.3~ 「게임 종료」 버튼을 없앰 (사용자 지시) — 「다시 시작」이 맨 아래 줄을 다 씀 (창을 닫아 끄면 무한 모드는 EndlessQuit 이 순위표에 올림)
        GameObject restart = UIKit.CloneButton(lowest.gameObject, "RestartButton", "다시 시작", esc.onPressRestart);
        RectTransform rr = (RectTransform)restart.transform;
        rr.sizeDelta = new Vector2(w, H);
        rr.anchoredPosition = new Vector2(0f, rowY);
        Transform hint = esc.escMenu.transform.Find("ResumeHint");
        if (hint != null) ((RectTransform)hint).anchoredPosition = new Vector2(0f, rowY - H * 0.5f - 34f);

        // 1.1.9~ 설정 줄을 반으로 나눠 「설정 | 도감」 → 1.4.8~ 「설정 | 빌드 보기」 (양옆 패널을 없애고 지금 빌드는 여기서 · 도감은 빌드 보기 · 메인 메뉴에서)
        Transform escRoot = esc.escMenu.transform.root;
        GameObject codex = UIKit.CloneButton(lowest.gameObject, "BuildViewButton", "빌드 보기", () => BuildView.Open(escRoot));
        RectTransform cr = (RectTransform)codex.transform;
        r.sizeDelta = cr.sizeDelta = new Vector2(half, H);
        cr.anchoredPosition = new Vector2((half + 16f) * 0.5f, r.anchoredPosition.y);
        r.anchoredPosition = new Vector2(-(half + 16f) * 0.5f, r.anchoredPosition.y);
        StyleEsc(esc);
    }

    // 1.4.8~ 사용자 「큰 돈 주고 산 게임처럼 · ESC 도」: 창 틀 · 빨간 띠를 걷고 화면 전체를 어둡게 덮은 뒤
    //   제목 「일시정지」 아래 글자 메뉴 한 줄씩 (계속하기 · 빌드 보기 · 설정 · 다시 시작 · 메인 메뉴로) — 메인 메뉴와 같은 MenuItemFx
    static void StyleEsc(ESCmenu esc)
    {
        Transform m = esc.escMenu.transform;
        RectTransform mr = (RectTransform)m;
        foreach (string n in new[] { "Window", "Banner" })
        {
            Transform t = m.Find(n);
            if (t != null) t.gameObject.SetActive(false);
        }
        // 화면 전체 막 (맨 뒤 · 누름을 받아 뒤 HUD 를 막음)
        RectTransform dim = UIKit.Rect("Dim", m, Vector2.zero, Vector2.zero);
        dim.SetAsFirstSibling();
        dim.anchorMin = Vector2.zero; dim.anchorMax = Vector2.one;
        dim.offsetMin = new Vector2(-4000f, -4000f); dim.offsetMax = new Vector2(4000f, 4000f);
        Image di = dim.gameObject.AddComponent<Image>();
        di.color = new Color(0.02f, 0.01f, 0.04f, 0.55f);
        // 1.4.8~ 흐린 배경: 열 때 화면을 찍어 작게 줄였다 늘려 깜 (막보다 뒤 · 막이 어둡게 덮음)
        RectTransform blur = UIKit.Rect("Blur", m, Vector2.zero, Vector2.zero);
        blur.SetAsFirstSibling();
        blur.anchorMin = Vector2.zero; blur.anchorMax = Vector2.one;
        blur.offsetMin = Vector2.zero; blur.offsetMax = Vector2.zero;
        RawImage ri = blur.gameObject.AddComponent<RawImage>();
        ri.raycastTarget = false;
        ri.color = new Color(0.62f, 0.58f, 0.68f);
        ri.enabled = false;
        m.gameObject.AddComponent<EscBackdrop>().image = ri;
        Transform title = m.Find("TItle");
        if (title != null) title.gameObject.SetActive(false);
        if (!UiRules.Legacy) { di.color = UiTokens.Scrim(false); StyleEscV2(m); return; }
        TMP_Text head = UIKit.Text(m, "일시정지", 64f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 250f), new Vector2(800f, 90f));
        head.name = "PauseTitle";
        RectTransform hl = UIKit.Rect("PauseTitleLine", m, new Vector2(0f, 200f), UIKit.DividerSprite != null ? new Vector2(512f, 32f) : new Vector2(360f, 2f));
        Image hli = hl.gameObject.AddComponent<Image>();
        hli.sprite = UIKit.DividerSprite;
        hli.color = UIKit.DividerSprite != null ? Color.white : new Color(0.96f, 0.83f, 0.47f, 0.6f);
        hli.raycastTarget = false;
        string[] order = { "resume", "BuildViewButton", "SettingsButton", "RestartButton", "Menu" };
        float y = 140f;
        foreach (string n in order)
        {
            Transform t = m.Find(n);
            if (t == null) foreach (Button b in m.GetComponentsInChildren<Button>(true)) if (b.name == n) { t = b.transform; break; }
            if (t == null) continue;
            if (t.parent != m) t.SetParent(m, false);
            RectTransform br = (RectTransform)t;
            br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 0.5f);
            br.sizeDelta = new Vector2(460f, 64f);
            br.anchoredPosition = new Vector2(0f, y);
            TMP_Text bt = t.GetComponentInChildren<TMP_Text>(true);
            if (bt != null) { bt.rectTransform.anchorMin = bt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); bt.rectTransform.anchoredPosition = Vector2.zero; bt.rectTransform.sizeDelta = new Vector2(440f, 60f); }
            MenuItemFx.Apply(t.GetComponent<Button>(), n == "resume" ? 40f : 34f, n == "resume", false);
            y -= n == "RestartButton" ? 96f : 74f;        // 「메인 메뉴로」는 한 칸 띄움
        }
        Transform hint = m.Find("ResumeHint");
        if (hint != null) ((RectTransform)hint).anchoredPosition = new Vector2(0f, y - 30f);
    }

    // UI 개편 P0-4a (UiRules.Legacy = false): 가운데 열 그대로 (왼쪽엔 계약 판 · 각인 목록) · 뼈색 제목 48 + 1도트 선 · 불투명 석판 버튼
    //   「계속」만 주 버튼 · 판을 끝내는 「다시 시작 · 메인 메뉴」는 짧은 선 아래 묶음 · 이름 · 순서 · 누름 동작은 그대로 (QA · 패드가 이름으로 찾음)
    static void StyleEscV2(Transform m)
    {
        const float W = 420f, H = 54f, Gap = 12f;
        TMP_Text head = UIKit.Text(m, "일시정지", UiTokens.Title, UiTokens.Text1, new Vector2(0f, 250f), new Vector2(800f, 64f));
        head.name = "PauseTitle";
        head.fontSizeMin = UiTokens.MinSize(UiTokens.Title);
        UiV2.Line(m, "PauseTitleLine", new Vector2(0f, 204f), W);
        string[] order = { "resume", "BuildViewButton", "SettingsButton", "RestartButton", "Menu" };
        float y = 150f;
        foreach (string n in order)
        {
            Transform t = m.Find(n);
            if (t == null) foreach (Button b in m.GetComponentsInChildren<Button>(true)) if (b.name == n) { t = b.transform; break; }
            if (t == null) continue;
            if (t.parent != m) t.SetParent(m, false);
            if (n == "RestartButton")
            {
                y -= UiTokens.S2;
                UiV2.Line(m, "EscEndLine", new Vector2(0f, y + H * 0.5f), 120f);
                y -= UiTokens.S4;
            }
            RectTransform br = (RectTransform)t;
            br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 0.5f);
            br.sizeDelta = new Vector2(W, H);
            br.anchoredPosition = new Vector2(0f, y);
            TMP_Text bt = t.GetComponentInChildren<TMP_Text>(true);
            if (bt != null) { bt.rectTransform.anchorMin = bt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); bt.rectTransform.anchoredPosition = Vector2.zero; bt.rectTransform.sizeDelta = new Vector2(W - UiTokens.S4 * 2f, H - UiTokens.S1); }
            UiV2.Restyle(t.GetComponent<Button>(), n == "resume" ? UiV2.Kind.Primary : UiV2.Kind.Secondary, n == "resume" ? UiTokens.Heading : UiTokens.Body);
            string shortName = n == "resume" ? "ui.btn.resume" : n == "BuildViewButton" ? "ui.btn.build" : n == "Menu" ? "result.menu" : null;     // P0-6 짧은 이름
            if (shortName != null) UiV2.Label(t.GetComponent<Button>(), shortName);
            y -= H + Gap;
        }
        Transform hint = m.Find("ResumeHint");
        if (hint != null)
        {
            ((RectTransform)hint).anchoredPosition = new Vector2(0f, y - 12f);
            TMP_Text ht = hint.GetComponentInChildren<TMP_Text>(true);
            if (ht != null) { ht.color = UiTokens.Text3; ht.fontStyle &= ~FontStyles.Bold; ht.fontSize = ht.fontSizeMax = UiTokens.Small; ht.fontSizeMin = UiTokens.Small; }
        }
    }
}

// ===================================================================== runtime UI helpers
public static class UIKit
{
    // 화면 UI를 붙일 캔버스: 탄약 패널이 있는 HUD 캔버스 > 화면용 최상위 캔버스.
    // UIKit.HudCanvas()는 적 이름표 같은 월드 캔버스를 고를 수 있어서
    // (그 적이 죽으면 붙어 있던 창까지 같이 사라짐) 쓰지 않음
    // 1.2.9~ 지금 장면의 캔버스만: 장면이 바뀌어도 남는 캔버스(FPS 표시 · 장면 전환 막, 정렬 32000)를 고르면
    //   결과 화면처럼 탄약 패널이 없는 장면의 UI 가 거기 붙어 메인 메뉴 · 다음 판까지 맨 위에 남고 판마다 쌓였음
    public static Canvas HudCanvas()
    {
        Scene active = SceneManager.GetActiveScene();
        Canvas best = null, any = null;
        foreach (Canvas cv in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (cv.gameObject.scene != active) continue;
            if (any == null) any = cv;
            if (!cv.isRootCanvas || cv.renderMode == RenderMode.WorldSpace) continue;
            if (cv.transform.Find("AmmoPanel") != null) return cv;
            if (best == null) best = cv;
        }
        return best != null ? best : any;
    }

    public static TMP_FontAsset Font;
    public static Material FontMaterial;
    public static Sprite ButtonSprite;

    public static GameObject CloneButton(GameObject template, string name, string label, UnityAction onClick)
    {
        GameObject go = Object.Instantiate(template, template.transform.parent);
        go.name = name;
        Button b = go.GetComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();       // 복제된 원래 기능은 지움
        b.onClick.AddListener(onClick);
        TMP_Text t = go.GetComponentInChildren<TMP_Text>(true);
        if (t != null)
        {
            t.text = Loc.T(label);
            Remember(t, label);
            if (Font == null && HasKorean(t.font)) { Font = t.font; FontMaterial = t.font.material; }
        }
        // (1.4.8~ 복제한 장면 버튼의 옛 그림을 공용 그림으로 잡지 않음 — EnsureStyle 이 새 도트 그림을 불러옴)
        return go;
    }

    // 언어가 바뀌면 다시 번역할 글자 (한국어 원문)
    static readonly Dictionary<TMP_Text, string> labels = new Dictionary<TMP_Text, string>();
    static bool hooked;
    public static void Forget(TMP_Text t) => labels.Remove(t);
    static int rememberCount;
    public static void Remember(TMP_Text t, string ko)
    {
        labels[t] = ko;
        // 최적화: 창을 닫아 지워진 글자가 세션 내내 쌓이지 않게 256번마다 정리 (씬 로드 때도 GameSettings 가 부름)
        if (++rememberCount >= 256) { rememberCount = 0; Prune(); }
        if (hooked) return;
        hooked = true;
        Loc.Changed += () =>
        {
            Prune();
            foreach (KeyValuePair<TMP_Text, string> kv in labels) kv.Key.text = Loc.T(kv.Value);
        };
    }

    // 지워진 글자 항목을 뺌 (살아 있는 글자는 그대로)
    static readonly List<TMP_Text> deadLabels = new List<TMP_Text>();
    public static void Prune()
    {
        foreach (KeyValuePair<TMP_Text, string> kv in labels)
            if (kv.Key == null) deadLabels.Add(kv.Key);
        for (int i = 0; i < deadLabels.Count; i++) labels.Remove(deadLabels[i]);
        deadLabels.Clear();
    }

    public static void EnsureStyle()
    {
        if (Font != null && ButtonSprite != null && PanelSprite != null && RibbonSprite != null && BarFrameSprite != null) return;      // 1.4.8~ 새 그림까지 다 불러왔을 때만 건너뜀
        // 1.4.9 긴급: 장면에서 처음 찾은 글자의 폰트를 쓰다 보니, 장면 설치를 앞당긴 뒤 영문 전용 제목 폰트(「Soul Saver」)가 잡혀
        //   코드로 만드는 모든 한글이 깨졌음 → 한글이 있는 폰트만 · 재질은 그 폰트 원본 재질 (글자별 복사본 재질이 아니라)
        if (Font == null)
            foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (HasKorean(t.font)) { Font = t.font; FontMaterial = t.font.material; break; }
        // 1.4.8~ 사용자 「메인 메뉴 느낌으로 설정창 · 도감 등 UI 가 있는 곳은 모두」: 장면 버튼의 장식 보라 그림 대신
        //   평평한 칸 + 얇은 금빛 테두리를 직접 그려 씀 (코드로 만드는 창 · 버튼 · 칸 전부가 이 그림) — 색을 곱해 상태를 칠하는 기존 코드는 그대로 통함
        // 1.4.8~ 사용자 「도형에 색만 채우면 싸구려 같다 · 게임 스타일에 맞게 이미지를」: tools/pixelart/make_ui_frames.py 가 그린 도트 틀
        //   (금장 테두리 · 돌판 · 리벳 / 창은 금 이중 테두리 · 모서리 보석) · 못 찾으면 예전처럼 코드로 그린 평평한 칸
        if (ButtonSprite == null) ButtonSprite = Resources.Load<Sprite>("UI/ui_button");
        if (ButtonSprite == null) ButtonSprite = FlatSprite();
        if (PanelSprite == null) PanelSprite = Resources.Load<Sprite>("UI/ui_panel");
        if (DividerSprite == null) DividerSprite = Resources.Load<Sprite>("UI/ui_divider");
        if (RibbonSprite == null) RibbonSprite = Resources.Load<Sprite>("UI/ui_ribbon");
        if (ShadowSprite == null) ShadowSprite = Resources.Load<Sprite>("UI/ui_shadow");
        if (VignetteSprite == null) VignetteSprite = Resources.Load<Sprite>("UI/ui_vignette");
        if (EmberSprite == null) EmberSprite = Resources.Load<Sprite>("UI/ui_ember");
        if (BarFrameSprite == null) BarFrameSprite = Resources.Load<Sprite>("UI/ui_bar_frame");
        if (BarFillSprite == null) BarFillSprite = Resources.Load<Sprite>("UI/ui_bar_fill");
    }
    public static Sprite PanelSprite, DividerSprite, RibbonSprite, ShadowSprite, VignetteSprite, EmberSprite, BarFrameSprite, BarFillSprite;

    // 한글(「가」)을 그릴 수 있는 폰트인지 (대체 폰트 포함)
    static bool HasKorean(TMP_FontAsset f) => f != null && f.HasCharacter('가', true, true);

    // 1.4.8~ 장면에 놓인 UI 를 새 스타일로: 버튼 그림 · 그 그림을 같이 쓰는 창 · 패널(9칸 나눔)을 평평한 칸으로
    //   장면마다 한 번 (MenuExtras.Install) · HUD 의 다른 그림(체력 막대 틀 등)은 건드리지 않음
    static readonly HashSet<Sprite> oldSprites = new HashSet<Sprite>();
    public static void RestyleScene()
    {
        EnsureStyle();
        Scene active = SceneManager.GetActiveScene();
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.gameObject.scene != active) continue;
            Image img = b.GetComponent<Image>();
            if (img != null && img.sprite != null && img.sprite != ButtonSprite && img.type == Image.Type.Sliced) oldSprites.Add(img.sprite);
        }
        foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // 1.4.8~ 옛 빨간 띠 그림(ui_banner: 「레벨업!」 · 「능력을 하나 선택하세요」)은 새 리본으로 · 띠에 맞게 낮게 · 글자는 가운데 금빛
            if (RibbonSprite != null && img.gameObject.scene == active && img.sprite != null && img.sprite.name == "ui_banner")
            {
                img.sprite = RibbonSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                RectTransform ir = img.rectTransform;
                ir.sizeDelta = new Vector2(Mathf.Min(ir.sizeDelta.x, 820f), Mathf.Min(ir.sizeDelta.y, 92f));
                foreach (TMP_Text bt in img.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (bt.transform.parent != img.transform) continue;
                    RectTransform tr = bt.rectTransform;
                    tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 0.5f);
                    tr.anchoredPosition = Vector2.zero;
                    tr.sizeDelta = new Vector2(ir.sizeDelta.x - 120f, ir.sizeDelta.y - 24f);
                    bt.enableAutoSizing = true;
                    bt.fontSizeMin = 20f;
                    bt.fontSizeMax = Mathf.Min(bt.fontSizeMax, 46f);
                    bt.color = new Color(0.98f, 0.86f, 0.52f);
                    bt.fontStyle |= FontStyles.Bold;
                }
            }
        }
        // 1.4.8~ 옛 Theme 그림 → 새 도트 그림 (이름으로): 판(ui_panel) · 막대 틀(ui_bar_frame) · 막대 채움(ui_bar_fill)
        //   채움은 Filled 방식을 그대로 두고 그림만 (체력이 줄어드는 길이 계산은 그대로)
        foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (img.gameObject.scene != active || img.sprite == null) continue;
            string n = img.sprite.name;
            if (n == "ui_panel" && PanelSprite != null && img.sprite != PanelSprite) { img.sprite = PanelSprite; if (img.type != Image.Type.Filled) img.type = Image.Type.Sliced; }
            else if (n == "ui_bar_frame" && BarFrameSprite != null && img.sprite != BarFrameSprite) { img.sprite = BarFrameSprite; if (img.type != Image.Type.Filled) img.type = Image.Type.Sliced; }
            else if (n == "ui_bar_fill" && BarFillSprite != null && img.sprite != BarFillSprite) img.sprite = BarFillSprite;
        }
        if (!UiRules.Legacy) { RestyleSceneV2(active); return; }
        if (oldSprites.Count == 0) return;
        foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (img.gameObject.scene != active || img.sprite == null || !oldSprites.Contains(img.sprite)) continue;
            img.sprite = ButtonSprite;
            img.type = Image.Type.Sliced;
            TMP_Text t = img.GetComponent<Button>() != null ? img.GetComponentInChildren<TMP_Text>(true) : null;
            if (t != null) t.fontStyle |= FontStyles.Bold;
        }
    }

    // UI 개편 P0-5 (Legacy 끔): 장면의 판 · 막대 틀 · 채움 · 리본 · 버튼 그림을 v2 로 (위치 · 크기 · Filled 길이 계산은 그대로)
    //   금 보석 창 판(HUD 네 귀퉁이 판 5개 등) → 새긴 석판 · 금 막대 틀 → 홈 · 그라데이션 채움 → 두 톤 · 리본 → 석판 + 뼈색 글자
    //   버튼은 그림 상태 바꾸기만 (글자는 각 화면 코드가 맡음) · 높이 200 넘는 것(레벨업 카드 등)은 화면 코드가 맡으므로 건드리지 않음
    static void RestyleSceneV2(Scene active)
    {
        Sprite panel = UiRules.Sprite("ui2_panel"), inset = UiRules.Sprite("ui2_inset"), fill = UiRules.Sprite("ui2_fill");
        if (panel == null) return;
        foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (img.gameObject.scene != active || img.sprite == null) continue;
            Sprite s = img.sprite;
            if (s == PanelSprite || s.name == "ui_panel" || s == RibbonSprite)
            {
                img.sprite = panel;
                if (img.type != Image.Type.Filled) { img.type = Image.Type.Tiled; img.pixelsPerUnitMultiplier = 1f; }
                img.color = new Color(1f, 1f, 1f, Mathf.Max(img.color.a, 0.9f));
                if (s == RibbonSprite)
                    foreach (TMP_Text bt in img.GetComponentsInChildren<TMP_Text>(true))
                        if (bt.transform.parent == img.transform) { bt.color = UiTokens.Text1; bt.fontStyle &= ~FontStyles.Bold; }
            }
            else if ((s == BarFrameSprite || s.name == "ui_bar_frame") && inset != null)
            {
                img.sprite = inset;
                if (img.type != Image.Type.Filled) { img.type = Image.Type.Tiled; img.pixelsPerUnitMultiplier = 1f; }
            }
            else if ((s == BarFillSprite || s.name == "ui_bar_fill") && fill != null) img.sprite = fill;
        }
        // 버튼이 아닌 옛 테마 판(시간 칸 · 특수 능력 칸 등 금 점선 틀)도 석판으로 — 옛 모습은 이것들을 금장 버튼 그림으로 바꿨음 (HUD 캡처 확인)
        foreach (Image img in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (img.gameObject.scene != active || img.sprite == null || img.GetComponent<Button>() != null) continue;
            if (img.type != Image.Type.Sliced || (img.sprite != ButtonSprite && !oldSprites.Contains(img.sprite))) continue;      // lint-ok: 옛 그림을 v2 로 바꿔 끼우는 비교
            img.sprite = panel; img.type = Image.Type.Tiled; img.pixelsPerUnitMultiplier = 1f;
            img.color = new Color(1f, 1f, 1f, Mathf.Max(img.color.a, 0.9f));
        }
        // 장면에 박힌 금색 글자(Lv · 탄 · 코인 · 포인트 등) → 뼈색
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.gameObject.scene != active) continue;
            Color c = t.color;
            if (c.r > 0.85f && c.g > 0.62f && c.g < 0.95f && c.b < 0.6f) t.color = new Color(UiTokens.Text1.r, UiTokens.Text1.g, UiTokens.Text1.b, c.a);
        }
        Sprite bn = UiRules.Sprite("ui2_btn_secondary_n");
        if (bn == null) return;
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b.gameObject.scene != active) continue;
            Image img = b.GetComponent<Image>();
            if (img == null || img.sprite == null || img.type != Image.Type.Sliced) continue;
            if (img.sprite != ButtonSprite && !oldSprites.Contains(img.sprite)) continue;       // lint-ok: 옛 버튼 그림을 v2 로 바꿔 끼우는 비교
            if (((RectTransform)b.transform).rect.height > 200f) continue;
            img.sprite = bn; img.type = Image.Type.Tiled; img.pixelsPerUnitMultiplier = 1f; img.color = Color.white;
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState
            {
                highlightedSprite = UiRules.Sprite("ui2_btn_secondary_h"), selectedSprite = UiRules.Sprite("ui2_btn_secondary_h"),
                pressedSprite = UiRules.Sprite("ui2_btn_secondary_p"), disabledSprite = UiRules.Sprite("ui2_btn_secondary_d"),
            };
        }
    }

    // 32×32 · 9칸 나눔 테두리 6 · 점 단위(도트 게임) — 바깥 2px 금빛 테두리 · 안쪽 1px 어두운 선 · 위 1줄 밝은 선 · 네 모서리 1px 비움
    static Sprite FlatSprite()
    {
        const int N = 32;
        Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "FlatButton" };
        Color edge = new Color(0.80f, 0.67f, 0.44f), inner = new Color(0.12f, 0.09f, 0.16f), fill = new Color(0.30f, 0.25f, 0.42f), top = new Color(0.38f, 0.32f, 0.52f);
        Color[] px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, N - 1 - x), Mathf.Min(y, N - 1 - y));
                bool corner = (x == 0 || x == N - 1) && (y == 0 || y == N - 1);
                px[y * N + x] = corner ? Color.clear : d < 2 ? edge : d == 2 ? inner : y == N - 4 ? top : fill;
            }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(6f, 6f, 6f, 6f));
    }

    public static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        return r;
    }

    // 화면 구석이나 가장자리에 붙임 (16:10 · 4:3 처럼 화면 폭이 좁아도 잘리지 않게).
    // edge (0,1) = 왼쪽 위, (1,1) = 오른쪽 위, (0.5,1) = 위 가운데 · offset 은 그 지점에서 요소 가운데까지
    public static void Pin(RectTransform r, Vector2 edge, Vector2 offset)
    {
        r.anchorMin = r.anchorMax = edge;
        r.anchoredPosition = offset;
    }

    // 1.0.4~: 확인 창에 ESC = 취소 (같은 프레임에 다른 창도 ESC 로 닫히지 않게 EscHandledFrame 을 씀)
    public static void EscCancel(GameObject blocker, UnityAction cancel)
    {
        if (blocker == null) return;
        blocker.AddComponent<ModalEsc>().cancel = cancel;
    }

    // 화면 전체를 어둡게 덮는 판 + 가운데 창
    public static RectTransform Modal(Transform root, string name, Vector2 size, out GameObject blocker)
    {
        // UI 개편 P0-4e: 모든 창이 새긴 석판 창 (막 Scrim · 그림자 · 0.94 확대 없음) — 돌려주는 내용 칸 크기는 예전과 같게 (size - 6)
        if (!UiRules.Legacy)
        {
            RectTransform w = UiV2.Modal(root, name, size, out blocker);
            RectTransform innerV2 = Rect("Inner", w, Vector2.zero, size - new Vector2(6f, 6f));
            innerV2.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            return innerV2;
        }
        EnsureStyle();
        Canvas canvas = root.GetComponentInChildren<Canvas>();
        Transform parent = canvas != null ? canvas.transform : root;
        blocker = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform br = blocker.GetComponent<RectTransform>();
        br.SetParent(parent, false);
        br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
        br.offsetMin = br.offsetMax = Vector2.zero;
        br.SetAsLastSibling();
        blocker.GetComponent<Image>().color = new Color(0.03f, 0.02f, 0.05f, 0.82f);

        // 1.4.8~ 창 뒤 부드러운 그림자 (아래로 조금 · 떠 보이게) · 막 · 창이 서서히 나타남 (WindowIntro)
        if (ShadowSprite != null)
        {
            RectTransform sh = Rect("Shadow", br, new Vector2(0f, -14f), size + new Vector2(70f, 70f));
            Image si = sh.gameObject.AddComponent<Image>();
            si.sprite = ShadowSprite;
            si.type = Image.Type.Sliced;
            si.raycastTarget = false;
        }
        RectTransform win = Rect("Window", br, Vector2.zero, size);
        blocker.AddComponent<WindowIntro>().window = win;
        Image wi = win.gameObject.AddComponent<Image>();
        // 1.4.8~ 창 틀 그림(ui_panel: 금 이중 테두리 · 모서리 보석 · 가운데 어두운 돌)이 있으면 그것 · 안쪽 판은 투명 (내용 자리는 그대로)
        wi.sprite = PanelSprite != null ? PanelSprite : ButtonSprite;
        wi.type = Image.Type.Sliced;
        wi.color = Color.white;
        RectTransform inner = Rect("Inner", win, Vector2.zero, size - new Vector2(6f, 6f));
        inner.gameObject.AddComponent<Image>().color = PanelSprite != null ? new Color(0f, 0f, 0f, 0f) : new Color(0.07f, 0.05f, 0.10f, 0.98f);
        return inner;
    }

    public static TMP_Text Text(Transform parent, string ko, float size, Color color, Vector2 pos, Vector2 box, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        RectTransform r = Rect("Text", parent, pos, box);
        TextMeshProUGUI t = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (Font != null) t.font = Font;
        if (FontMaterial != null) t.fontSharedMaterial = FontMaterial;
        t.fontSize = size;
        t.enableAutoSizing = true;
        t.fontSizeMin = 12f;
        t.fontSizeMax = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.text = Loc.T(ko);
        if (!string.IsNullOrEmpty(ko)) Remember(t, ko);
        return t;
    }

    // 1.1.9~ 확인 창 키보드 조작: 기본 버튼을 선택해 두면 Unity 의 Submit(Enter · Space)이 그 버튼을 누르고 ←→ 로 옮겨 감
    // 게임패드가 활성이면 선택하지 않음 (A 가 가상 커서 클릭 + Submit 으로 두 번 눌릴 수 있음)
    public static void SetDefault(Button ok)
    {
        if (ok == null || GamePad.Active) return;
        UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
        if (es != null) es.SetSelectedGameObject(ok.gameObject);
    }

    // 확인 · 취소 두 버튼: 서로만 오가게 (뒤에 깔린 창 버튼으로 새지 않게) · 기본 선택 · 아래 「[Enter] 확인   [ESC] 취소」 (패드면 숨김)
    // selectOk = false (게임 종료 확인): 기본 선택도 안내도 없이 ←→ 이동만
    public static void ConfirmKeys(RectTransform win, Button ok, Button cancel, float footerY, bool selectOk = true)
    {
        if (ok != null && cancel != null)
        {
            ok.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = cancel, selectOnLeft = cancel };
            cancel.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = ok, selectOnRight = ok };
        }
        if (!selectOk) return;
        SetDefault(ok);
        if (GamePad.Active || win == null) return;
        Text(win, "[Enter] 확인   [ESC] 취소", 18f, new Color(0.66f, 0.61f, 0.53f), new Vector2(0f, footerY), new Vector2(720f, 28f));
    }

    // 1.1.9~ 글 입력 칸 (도감 검색): 어두운 뒤판 + 자리 글(Dim) · 일본어 · 중국어 입력은 글꼴 보강
    public static TMP_InputField MakeInput(Transform parent, string placeholderKo, Vector2 pos, Vector2 size, float fontSize = 22f)
    {
        EnsureStyle();
        RectTransform r = Rect("Input", parent, pos, size);
        Image bg = r.gameObject.AddComponent<Image>();
        bg.sprite = ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.2f, 0.18f, 0.25f, 1f);
        RectTransform area = Rect("Text Area", r, Vector2.zero, Vector2.zero);
        area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
        area.offsetMin = new Vector2(14f, 4f); area.offsetMax = new Vector2(-14f, -4f);
        area.gameObject.AddComponent<RectMask2D>();
        TMP_Text ph = Text(area, placeholderKo, fontSize, new Color(0.62f, 0.58f, 0.52f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
        Stretch(ph.rectTransform);
        ph.enableAutoSizing = true;
        ph.fontSizeMin = 12f;
        ph.fontStyle = FontStyles.Italic;
        TMP_Text txt = Text(area, "", fontSize, new Color(0.96f, 0.9f, 0.8f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
        Stretch(txt.rectTransform);
        txt.enableAutoSizing = false;
        txt.fontSize = fontSize;
        txt.raycastTarget = false;
        TMP_InputField f = r.gameObject.AddComponent<TMP_InputField>();
        f.textViewport = area;
        f.textComponent = txt;
        f.placeholder = ph;
        f.targetGraphic = bg;
        f.lineType = TMP_InputField.LineType.SingleLine;
        f.characterLimit = 40;
        f.richText = false;
        f.restoreOriginalTextOnEscape = false;          // ESC 는 창이 처리 (검색 비우기)
        if (Font != null) f.fontAsset = Font;
        f.pointSize = fontSize;
        Loc.EnsureCjkFallback(txt);
        return f;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    public static Button MakeButton(Transform parent, string ko, Vector2 pos, Vector2 size, UnityAction onClick, float fontSize = 26f)
    {
        // UI 개편 P0-4e: 코드로 만드는 모든 버튼이 석판 보조 버튼 (글자 크기는 4단계로 맞춤 · 굵게 없음) — 색을 덧칠해 상태를 보이던 곳은 P1 에서 화면별로
        if (!UiRules.Legacy)
            return UiV2.Btn(parent, ko, pos, size, onClick, UiV2.Kind.Secondary, fontSize >= 30f ? UiTokens.Heading : fontSize >= 24f ? UiTokens.Body : UiTokens.Small);
        RectTransform r = Rect("Button", parent, pos, size);
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = ButtonSprite;
        img.type = Image.Type.Sliced;
        Button b = r.gameObject.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1f, 0.9f, 0.62f);
        cb.pressedColor = new Color(0.72f, 0.62f, 0.48f);
        cb.selectedColor = Color.white;
        b.colors = cb;
        b.onClick.AddListener(onClick);
        TMP_Text bt = Text(r, ko, fontSize, new Color(0.96f, 0.9f, 0.8f), Vector2.zero, size - new Vector2(20f, 12f));
        bt.fontStyle |= FontStyles.Bold;            // 1.4.8~ 평평한 칸 위에서 잘 보이게
        return b;
    }

    // 1.2.4~ 드롭다운 (설정 「화면」 탭 해상도 · 그래픽 품질 · 최대 프레임) — 프리팹 없이 템플릿 · 뷰포트 · 스크롤 · 항목 토글까지 코드로 만듦
    // 펼친 목록은 TMP_Dropdown 이 템플릿을 복제해 자기 캔버스(정렬 30000, 그 아래 30000-1 막)에 그려 다른 줄 위에 뜨고, 캔버스 밖으로 나가면 위로 뒤집힘
    // 템플릿에는 우리 스크립트를 넣지 않음 (DropdownAssist 는 바깥 루트에만 · 복제되지 않음)
    // names: 한국어 원문(translate = true 면 Loc.T 로 번역 · 언어가 바뀌면 다시) 또는 숫자 같은 그대로 쓰는 글
    // maxVisible: 펼친 목록에 한 번에 보이는 항목 수 (넘으면 스크롤 · 휠 · 오른쪽 스틱)
    public static DropdownAssist MakeDropdown(Transform parent, Vector2 pos, Vector2 size, string[] names, bool translate,
                                              System.Func<int> get, System.Action<int> set, float fontSize = 26f, int maxVisible = 7)
    {
        EnsureStyle();
        Color cream = new Color(0.96f, 0.9f, 0.8f);
        RectTransform r = Rect("Dropdown", parent, pos, size);
        Image img = r.gameObject.AddComponent<Image>();
        img.sprite = ButtonSprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0.96f, 0.83f, 0.47f);
        TMP_Text caption = Text(r, "", fontSize, cream, new Vector2(-18f, 0f), size - new Vector2(64f, 12f));
        Image[] arrow = Chevron(r, new Vector2(size.x * 0.5f - 30f, 0f), cream);

        float itemH = Mathf.Max(36f, size.y - 4f);

        // 템플릿: 버튼 바로 아래, 버튼 폭 그대로 (항목이 적으면 TMP_Dropdown 이 높이를 줄임)
        RectTransform template = Rect("Template", r, Vector2.zero, Vector2.zero);
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = new Vector2(0f, -4f);
        template.sizeDelta = new Vector2(0f, itemH * Mathf.Max(1, maxVisible));     // 위아래 여백 없음 (있으면 항목이 적을 때 줄인 높이가 여백만큼 모자라 조금 스크롤됨)
        Image frame = template.gameObject.AddComponent<Image>();
        frame.sprite = ButtonSprite;
        frame.type = Image.Type.Sliced;
        frame.color = new Color(0.55f, 0.5f, 0.6f, 1f);
        ScrollRect scroll = template.gameObject.AddComponent<ScrollRect>();

        RectTransform viewport = Rect("Viewport", template, Vector2.zero, Vector2.zero);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.pivot = new Vector2(0f, 1f);
        viewport.offsetMin = viewport.offsetMax = Vector2.zero;
        viewport.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.07f, 0.12f, 0.98f);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = Rect("Content", viewport, Vector2.zero, Vector2.zero);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0f, itemH);

        // 항목 하나 (TMP_Dropdown 이 옵션 수만큼 복제): 배경 = 토글 색 · 왼쪽 금색 막대 = 지금 값 표시
        RectTransform item = Rect("Item", content, Vector2.zero, Vector2.zero);
        item.anchorMin = Vector2.zero;
        item.anchorMax = Vector2.one;
        item.offsetMin = item.offsetMax = Vector2.zero;
        Toggle toggle = item.gameObject.AddComponent<Toggle>();
        RectTransform bg = Rect("Bg", item, Vector2.zero, Vector2.zero);
        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = new Vector2(3f, 2f);
        bg.offsetMax = new Vector2(-3f, -2f);
        Image bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = Color.white;
        RectTransform mark = Rect("Mark", item, new Vector2(12f, 0f), new Vector2(6f, itemH - 16f));
        mark.anchorMin = mark.anchorMax = new Vector2(0f, 0.5f);
        Image markImg = mark.gameObject.AddComponent<Image>();
        markImg.color = new Color(0.96f, 0.83f, 0.47f);
        markImg.raycastTarget = false;
        TMP_Text label = Text(item, "", fontSize - 2f, new Color(0.92f, 0.88f, 0.80f), Vector2.zero, Vector2.zero, TextAlignmentOptions.Left);
        RectTransform lr = label.rectTransform;
        lr.anchorMin = Vector2.zero;
        lr.anchorMax = Vector2.one;
        lr.offsetMin = new Vector2(28f, 2f);
        lr.offsetMax = new Vector2(-8f, -2f);
        toggle.targetGraphic = bgImg;
        toggle.graphic = markImg;
        toggle.isOn = false;
        ColorBlock tc = toggle.colors;
        tc.normalColor = new Color(0.16f, 0.13f, 0.2f);
        tc.highlightedColor = new Color(0.44f, 0.35f, 0.21f);
        tc.selectedColor = new Color(0.44f, 0.35f, 0.21f);       // 마우스를 올리거나 패드 · 키보드로 고른 항목
        tc.pressedColor = new Color(0.32f, 0.25f, 0.16f);
        tc.fadeDuration = 0.05f;
        toggle.colors = tc;

        // 스크롤 막대: 보이기만 (누를 수 없게 해 패드 커서가 막대로 붙지 않음 · 끌기 대신 휠 · 오른쪽 스틱)
        RectTransform bar = Rect("Scrollbar", template, Vector2.zero, Vector2.zero);
        bar.anchorMin = new Vector2(1f, 0f);
        bar.anchorMax = new Vector2(1f, 1f);
        bar.pivot = new Vector2(1f, 1f);
        bar.sizeDelta = new Vector2(10f, 0f);
        bar.anchoredPosition = Vector2.zero;
        Image barImg = bar.gameObject.AddComponent<Image>();
        barImg.color = new Color(0.2f, 0.17f, 0.25f, 1f);
        barImg.raycastTarget = false;
        RectTransform slide = Rect("Sliding Area", bar, Vector2.zero, Vector2.zero);
        slide.anchorMin = Vector2.zero;
        slide.anchorMax = Vector2.one;
        slide.offsetMin = slide.offsetMax = Vector2.zero;
        RectTransform handle = Rect("Handle", slide, Vector2.zero, Vector2.zero);
        handle.offsetMin = handle.offsetMax = Vector2.zero;
        Image handleImg = handle.gameObject.AddComponent<Image>();
        handleImg.color = new Color(0.96f, 0.75f, 0.3f);
        handleImg.raycastTarget = false;
        Scrollbar sb = bar.gameObject.AddComponent<Scrollbar>();
        sb.handleRect = handle;
        sb.targetGraphic = handleImg;
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.interactable = false;
        ColorBlock sc = sb.colors;
        sc.disabledColor = Color.white;
        sb.colors = sc;
        Navigation none = new Navigation { mode = Navigation.Mode.None };
        sb.navigation = none;

        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.scrollSensitivity = itemH;       // 휠 한 칸 = 항목 하나
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.verticalScrollbar = sb;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scroll.verticalScrollbarSpacing = 2f;

        template.gameObject.SetActive(false);

        TMP_Dropdown dd = r.gameObject.AddComponent<TMP_Dropdown>();
        dd.targetGraphic = img;
        ColorBlock cb = dd.colors;
        cb.highlightedColor = new Color(1f, 0.9f, 0.62f);
        cb.pressedColor = new Color(0.72f, 0.62f, 0.48f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.55f, 0.55f, 0.58f, 0.7f);
        dd.colors = cb;
        dd.template = template;
        dd.captionText = caption;
        dd.itemText = label;
        dd.alphaFadeSpeed = 0.08f;

        DropdownAssist assist = r.gameObject.AddComponent<DropdownAssist>();
        assist.Init(dd, caption, arrow, itemH, names, translate, get);
        dd.onValueChanged.AddListener(v => set?.Invoke(v));
        return assist;
    }

    // 드롭다운 오른쪽 「V」 화살표 (글꼴에 ▼ 가 없을 수 있어 막대 두 개로)
    static Image[] Chevron(Transform parent, Vector2 pos, Color color)
    {
        RectTransform root = Rect("Arrow", parent, pos, new Vector2(20f, 14f));
        Image[] bars = new Image[2];
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -1f : 1f;
            RectTransform b = Rect("Bar", root, new Vector2(4.6f * side, 0f), new Vector2(13f, 4f));
            b.localRotation = Quaternion.Euler(0f, 0f, 45f * side);
            bars[i] = b.gameObject.AddComponent<Image>();
            bars[i].color = color;
            bars[i].raycastTarget = false;
        }
        return bars;
    }
}

// ===================================================================== settings
// 탭: 일반(언어 · 볼륨) / 화면(모드 · 해상도 · 최대 프레임 · 수직 동기화 · 프레임 표시 · 흔들림 · 번쩍임) / 조작(키 바꾸기) / 게임
public static class SettingsUI
{
    static GameObject open;
    public static bool IsOpen => open != null;

    static Color Gold => UiRules.Legacy ? LegacyGold : UiTokens.Text1;      // UI 개편 (Legacy 끔): 금색 글자 대신 뼈색 (기획서 C6 「금색 25벌」)
    static readonly Color LegacyGold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Off = new Color(0.6f, 0.58f, 0.65f);

    // 1.1.9~ 탭 6개 (편의성 2 · 3차): 접근성 · 표시(전장 · HUD 에 그려지는 것)를 더함
    static readonly string[] TabNames = { "일반", "화면", "조작", "게임", "접근성", "표시" };
    static int tab;
    static RectTransform page;
    static readonly List<Button> tabButtons = new List<Button>();

    // 키 바꾸기: 입력을 기다리는 동작 (-1 = 없음)
    static int capturing = -1;
    static readonly List<TMP_Text> keyLabels = new List<TMP_Text>();
    // 1.1~ 다른 동작이 쓰는 키라 못 바꾼 칸: 잠깐 「사용 중」 (동작 번호, 끝나는 시각)
    static int blocked = -1;
    static float blockedUntil;

    // ESC를 이 창이 처리한 프레임 (ESCmenu가 같은 ESC로 메뉴를 닫지 않게)
    public static int EscHandledFrame = -1;

    public static void Open(Transform root)
    {
        if (open != null) return;
        RectTransform win = UIKit.Modal(root, "SettingsPanel", new Vector2(1180f, 860f), out open);
        open.AddComponent<SettingsInput>();

        UIKit.Text(win, "설정", UiRules.Legacy ? 46f : UiTokens.Title, UiRules.Legacy ? Gold : UiTokens.Text1, new Vector2(0f, 360f), new Vector2(600f, 70f));      // 1.4.8~ 52 → 제목 46 · UI 개편: 뼈색 48

        tabButtons.Clear();
        for (int i = 0; i < TabNames.Length; i++)
        {
            int t = i;
            Button tb = UIKit.MakeButton(win, TabNames[i], new Vector2(-475f + 190f * i, 272f), new Vector2(180f, 64f), () => ShowTab(t), 24f);
            TMP_Text tl = tb.GetComponentInChildren<TMP_Text>();
            if (tl != null) { tl.enableAutoSizing = true; tl.fontSizeMin = 20f; tl.fontSizeMax = 24f; }      // 「Accessibility」 · 「アクセシビリティ」
            tabButtons.Add(tb);
        }
        page = UIKit.Rect("Page", win, new Vector2(0f, -30f), new Vector2(1100f, 560f));

        // 1.4.8~ 닫기 76 → 64 높이 · 30 → 28 (옆 「이 탭 기본값으로」와 같은 높이 · 아래끝 맞춤)
        UIKit.MakeButton(win, "닫기", new Vector2(0f, -364f), new Vector2(260f, 64f), Close, 28f);
        // 1.1.9~ 탭마다 「이 탭 기본값으로」 (예전엔 조작 탭에만 「기본값으로」) · 실수 방지로 두 번 누르기
        resetButton = UIKit.MakeButton(win, ResetKey, new Vector2(330f, -364f), new Vector2(280f, 64f), ResetTab, 24f);
        TMP_Text resetLabel = resetButton.GetComponentInChildren<TMP_Text>();
        if (resetLabel != null) { resetLabel.enableAutoSizing = true; resetLabel.fontSizeMin = 20f; resetLabel.fontSizeMax = 24f; resetLabelColor = resetLabel.color; }
        resetArmedAt = resetDoneAt = -99f;
        RowTip(resetLabel, "이 탭 기본값으로", "이 탭의 설정만 처음 값으로 되돌립니다(조작 탭은 모든 키 배치). 다른 탭과 언어 · 화면 모드 · 해상도는 그대로 둡니다.");
        ShowTab(tab);
    }

    static Button resetButton;
    static float resetArmedAt = -99f;
    static Color resetLabelColor = Color.white;
    static string ResetKey => UiRules.Legacy ? "이 탭 기본값으로" : "ui.btn.reset";      // UI 개편 P0-6: 짧은 이름 「초기화」 (두 번 누르기 안내 뒤 되돌리는 글자도 같은 키)
    static float rowH = 64f;                            // 설정 줄 버튼 높이 (게임 탭 7줄 64 · 접근성 탭 9줄 54 · 화면 탭은 BuildDisplay 에서 10줄 48 · 11줄 44)

    static float resetDoneAt = -99f;
    static bool ResetArmed => resetArmedAt > -99f;

    // 2초 안에 다시 누르지 않으면 · 「되돌렸습니다」를 1.5초 보인 뒤 원래 글자로
    static void DisarmReset()
    {
        if (!ResetArmed && resetDoneAt <= -99f) return;
        resetArmedAt = resetDoneAt = -99f;
        if (resetButton != null) SetResetLabel(resetButton.GetComponentInChildren<TMP_Text>(), ResetKey, resetLabelColor);
    }

    static void ResetTab()
    {
        TMP_Text t = resetButton != null ? resetButton.GetComponentInChildren<TMP_Text>() : null;
        Deselect();
        if (!ResetArmed || Time.unscaledTime - resetArmedAt >= 2f)
        {
            resetArmedAt = Time.unscaledTime;
            resetDoneAt = -99f;
            SetResetLabel(t, "한 번 더 누르면 되돌립니다", new Color(1f, 0.55f, 0.45f));
            return;
        }
        if (tab == 0) GameSettings.ResetGeneral();
        else if (tab == 1) GameSettings.ResetDisplay();
        else if (tab == 2) { capturing = -1; KeyBindings.ResetAll(); }
        else if (tab == 3) GameSettings.ResetGameplay();
        else if (tab == 4) GameSettings.ResetAccess();
        else GameSettings.ResetHud();
        ShowTab(tab);                                   // 줄을 다시 그려 새 값이 보이게 (무장 해제)
        // 되돌렸다는 표시는 버튼 글자로 잠깐 (토스트는 설정 창 뒤에 가려 보이지 않음)
        SetResetLabel(t, "되돌렸습니다", new Color(0.6f, 0.95f, 0.6f));
        resetDoneAt = Time.unscaledTime;
    }

    static void SetResetLabel(TMP_Text t, string ko, Color color)
    {
        if (t == null) return;
        t.text = Loc.T(ko);
        t.color = color;
        UIKit.Remember(t, ko);
    }

    static void ShowTab(int t)
    {
        tab = t;
        DisarmReset();                                  // 다른 탭으로 옮기면 무장 해제
        rowH = tab == 1 || tab == 4 ? 54f : 64f;          // 화면 · 접근성 탭은 54 (접근성 9줄 · 화면 탭은 BuildDisplay 가 10줄 48 · 11줄 44 로 다시 정함) · 나머지(게임 탭 7줄 등)는 75 간격 · 64 높이
        capturing = -1;
        blocked = -1;
        keyLabels.Clear();
        for (int i = page.childCount - 1; i >= 0; i--) Object.Destroy(page.GetChild(i).gameObject);
        for (int i = 0; i < tabButtons.Count; i++)
        {
            Mark(tabButtons[i], i == tab);
        }
        if (tab == 0) BuildGeneral();
        else if (tab == 1) BuildDisplay();
        else if (tab == 2) BuildControls();
        else if (tab == 3) BuildGameplay();
        else if (tab == 4) BuildAccess();
        else BuildHud();
    }

    // 1.4.8~ 공통 글자 단계: 30 → 본문 26 (하한 12 → 20) · 왼끝 -540 에 맞추고 폭은 줄마다 오른쪽 조작 칸 앞까지
    //   (슬라이더 · 언어 줄 300, 화면 모드 360, 버튼 · 드롭다운 칸이 x -30 에서 시작하는 줄 490 — en 이 20 아래로 줄지 않게)
    const float LabelLeft = -540f, LabelW = 300f, LabelWide = 490f;
    static TMP_Text RowLabel(string ko, float y, float w = LabelW)
    {
        TMP_Text t = UIKit.Text(page, ko, 26f, Parch, new Vector2(LabelLeft + w * 0.5f, y), new Vector2(w, 50f), TextAlignmentOptions.Left);
        t.fontSizeMin = 20f;
        return t;
    }

    // ================================================================= 일반
    // 1.4.4~ 플레이 데이터 보내기 두 줄을 더하면 8줄: 65 간격 · 높이 56 (서버 주소가 없으면 예전 6줄 그대로)
    static readonly float[] GeneralY6 = { 200f, 110f, 20f, -70f, -160f, -250f };
    static readonly float[] GeneralY8 = { 215f, 150f, 85f, 20f, -45f, -110f, -175f, -240f };

    static void BuildGeneral()
    {
        bool tel = Telemetry.Configured;
        float[] ys = tel ? GeneralY8 : GeneralY6;
        if (tel) rowH = 56f;
        RowLabel("언어", ys[0]);
        List<Button> langButtons = new List<Button>();
        for (int i = 0; i < 4; i++)
        {
            int lang = i;
            Button b = UIKit.MakeButton(page, "", new Vector2(-140f + 185f * i, ys[0]), new Vector2(170f, rowH), () =>
            {
                GameSettings.Language = (Loc.Lang)lang;
                HighlightLang(langButtons);
            }, 26f);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            label.text = Loc.LangNames[i];     // 언어 이름은 각 언어로 그대로
            TMP_FontAsset native = Loc.NativeFont((Loc.Lang)i);
            if (native != null) label.font = native;   // 기본 폰트엔 일본어 · 중국어 글자가 없음
            langButtons.Add(b);
        }
        HighlightLang(langButtons);

        // 2.1.9: 「창이 비활성일 때 소리 끄기」 한 줄을 더 넣으려고 줄 간격 90
        SliderRow("전체 볼륨", ys[1], GameSettings.Volume, v => GameSettings.Volume = v);
        SliderRow("음악", ys[2], GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
        SliderRow("효과음", ys[3], GameSettings.SfxVolume, v => GameSettings.SfxVolume = v, true);
        SensitivityRow(ys[4]);
        ToggleRow("창이 비활성일 때 소리 끄기", ys[5], () => GameSettings.MuteUnfocused, v => GameSettings.MuteUnfocused = v);
        if (tel) TelemetryRows(ys[6], ys[7]);
    }

    // 1.4.4~ 플레이 데이터 보내기: 켜기 = 동의 · 끄기 = 거부 · 「전문 보기」(동의창을 읽기 전용으로)
    //   아랫줄: 익명 ID 앞 8자 · 「내 데이터 삭제 요청」(두 번 눌러 확인 · 3초 안에 다시 안 누르면 풀림)
    static Button deleteButton;
    static TMP_Text telemetryIdLabel;
    static float deleteArmedAt = -99f, deleteDoneAt = -99f;
    static bool deleteSending;

    static void TelemetryRows(float y1, float y2)
    {
        ToggleRow("telemetry.setting", y1, () => Telemetry.Consent == 1, v => Telemetry.SetConsent(v), "telemetry.setting.tip");
        Transform root = open != null ? open.transform.parent : null;
        Button view = UIKit.MakeButton(page, "telemetry.view", new Vector2(355f, y1), new Vector2(230f, rowH), () =>
        {
            Deselect();
            if (root != null) TelemetryConsentUI.Open(root, true);
        }, 24f);
        TMP_Text vl = view.GetComponentInChildren<TMP_Text>();
        if (vl != null) { vl.fontSizeMin = 20f; vl.fontSizeMax = 24f; }

        telemetryIdLabel = RowLabel("", y2);
        telemetryIdLabel.rectTransform.sizeDelta = new Vector2(420f, 50f);
        telemetryIdLabel.rectTransform.anchoredPosition = new Vector2(-330f, y2);
        telemetryIdLabel.fontSizeMax = 26f;
        RefreshTelemetryId();
        telemetryIdLabel.gameObject.AddComponent<LocRefresh>().refresh = RefreshTelemetryId;

        deleteArmedAt = deleteDoneAt = -99f;
        deleteSending = false;
        deleteButton = UIKit.MakeButton(page, "telemetry.delete", new Vector2(220f, y2), new Vector2(500f, rowH), DeleteClicked, 24f);
        TMP_Text dl = deleteButton.GetComponentInChildren<TMP_Text>();
        if (dl != null) { dl.fontSizeMin = 20f; dl.fontSizeMax = 24f; }
        SetDeleteLabel("telemetry.delete", new Color(0.96f, 0.9f, 0.8f));
    }

    static void RefreshTelemetryId()
    {
        if (telemetryIdLabel == null) return;
        telemetryIdLabel.text = Loc.T("telemetry.id") + "  " + Telemetry.ShortId;
    }

    static void SetDeleteLabel(string ko, Color color)
    {
        TMP_Text t = deleteButton != null ? deleteButton.GetComponentInChildren<TMP_Text>() : null;
        if (t == null) return;
        t.text = Loc.T(ko);
        t.color = color;
        UIKit.Remember(t, ko);
    }

    static void DeleteClicked()
    {
        Deselect();
        if (deleteSending) return;
        if (deleteArmedAt <= -99f || Time.unscaledTime - deleteArmedAt >= 3f)
        {
            deleteArmedAt = Time.unscaledTime;
            deleteDoneAt = -99f;
            SetDeleteLabel("telemetry.delete.confirm", new Color(1f, 0.55f, 0.45f));
            return;
        }
        deleteArmedAt = -99f;
        deleteSending = true;
        SetDeleteLabel("telemetry.delete.sending", new Color(0.96f, 0.9f, 0.8f));
        Telemetry.RequestDelete(ok =>
        {
            deleteSending = false;
            if (deleteButton == null) return;          // 그사이 설정 창 · 탭이 닫힘
            SetDeleteLabel(ok ? "telemetry.delete.done" : "telemetry.delete.failed", ok ? new Color(0.6f, 0.95f, 0.6f) : new Color(1f, 0.55f, 0.45f));
            deleteDoneAt = Time.unscaledTime;
            RefreshTelemetryId();
        });
    }

    // 무장 3초 · 결과 글자 3초 뒤 원래 글자로 (Tick)
    static void TickDelete()
    {
        if (deleteButton == null || deleteSending) return;
        if ((deleteArmedAt > -99f && Time.unscaledTime - deleteArmedAt >= 3f) || (deleteDoneAt > -99f && Time.unscaledTime - deleteDoneAt >= 3f))
        {
            deleteArmedAt = deleteDoneAt = -99f;
            SetDeleteLabel("telemetry.delete", new Color(0.96f, 0.9f, 0.8f));
        }
    }

    // 마우스 감도 25% ~ 300% (5% 단위, 기본 100%) · 전투 중 조준에만 적용
    static void SensitivityRow(float y)
    {
        RowLabel("마우스 감도", y);
        TMP_Text percent = UIKit.Text(page, "", 26f, UiRules.Legacy ? Gold : UiTokens.Text1, new Vector2(450f, y), new Vector2(120f, 50f));
        Slider slider = MakeSlider(page, new Vector2(80f, y), new Vector2(560f, 36f));
        slider.minValue = GameSettings.MouseSensMin;
        slider.maxValue = GameSettings.MouseSensMax;
        slider.value = GameSettings.MouseSensitivity;
        percent.text = Mathf.RoundToInt(slider.value * 100f) + "%";
        slider.onValueChanged.AddListener(v =>
        {
            float snapped = Mathf.Round(v * 20f) / 20f;
            GameSettings.MouseSensitivity = snapped;
            percent.text = Mathf.RoundToInt(snapped * 100f) + "%";
        });
    }

    static void HighlightLang(List<Button> buttons)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool on = i == (int)GameSettings.Language;
            Mark(buttons[i], on);
        }
    }

    // 고른 것 표시: 옛 모습은 금색 칠 + 1.06배 · 새 모습은 그림 (UiV2.Choose — 금색을 새 석판에 곱하면 탁한 갈색이 됐음)
    static void Mark(Button b, bool on)
    {
        if (b == null) return;
        if (!UiRules.Legacy) { b.transform.localScale = Vector3.one; UiV2.Choose(b, on); return; }
        b.GetComponent<Image>().color = on ? Gold : Off;
        b.transform.localScale = Vector3.one * (on ? 1.06f : 1f);
    }

    // 0 ~ 100%
    static float sfxPreviewAt;

    // preview: 효과음 슬라이더는 움직일 때 새 크기로 짧은 소리를 들려줌 (1.1~, 0.15초 간격)
    static void SliderRow(string ko, float y, float value, System.Action<float> set, bool preview = false)
    {
        RowLabel(ko, y);
        TMP_Text percent = UIKit.Text(page, "", 26f, UiRules.Legacy ? Gold : UiTokens.Text1, new Vector2(450f, y), new Vector2(120f, 50f));
        Slider slider = MakeSlider(page, new Vector2(80f, y), new Vector2(560f, 36f));
        slider.value = value;
        percent.text = Mathf.RoundToInt(value * 100f) + "%";
        slider.onValueChanged.AddListener(v =>
        {
            set(v);
            percent.text = Mathf.RoundToInt(v * 100f) + "%";
            if (preview && Time.unscaledTime >= sfxPreviewAt)
            {
                sfxPreviewAt = Time.unscaledTime + 0.15f;
                UISound.Click();
            }
        });
    }

    // ================================================================= 화면
    // 1.1.9~ 화면 모드 · 모니터 · 해상도 · 최대 프레임 · 그래픽 품질 · UI 크기 · 안내 메시지 크기 · 수직 동기화 · 프레임 표시 · 화면 흔들림 · 번쩍임 효과
    //   1.2.4~ 해상도 · 최대 프레임 · 그래픽 품질 · 모니터는 드롭다운 (DropdownAssist · 「이 탭 기본값으로」는 탭을 다시 그려 드롭다운 표시도 새 값으로)
    //   모니터가 둘 이상이면 11줄: 50 간격 · 높이 44 (RowsY11) · 모니터가 하나면 10줄: 53 간격 · 높이 48 (RowsY10) — 줄 목록에 y 를 차례로 배정
    static void BuildDisplay()
    {
        List<DisplayInfo> monitors = GameSettings.Displays();
        List<System.Action<float>> rows = new List<System.Action<float>>();
        rows.Add(DisplayModeRow);
        if (monitors.Count > 1) rows.Add(y => MonitorRow(y, monitors));
        rows.Add(ResolutionRow);
        // 1.2.4~ 최대 프레임 · 그래픽 품질은 드롭다운 · 수직 동기화가 켜져 있으면 최대 프레임은 흐리게 (모니터 주사율을 따름)
        rows.Add(y =>
        {
            maxFpsDropdown = DropdownRow("최대 프레임", y, GameSettings.MaxFpsNames, () => GameSettings.MaxFpsIndex, v => GameSettings.MaxFpsIndex = v,
                     "1초에 그리는 화면 수의 상한입니다. 낮추면 컴퓨터 부담과 발열이 줄어듭니다. 수직 동기화를 켜면 모니터 주사율을 따릅니다.");
            maxFpsDropdown.SetUsable(!GameSettings.VSync);
        });
        // 그래픽 품질: 낮음이면 꾸밈 효과만 줄임 (GraphicsQuality · 다음 이펙트부터 바로 적용)
        rows.Add(y => graphicsDropdown = DropdownRow("그래픽 품질", y, GraphicsQuality.Names, () => GraphicsQuality.Index, v => GraphicsQuality.Index = v,
                 "낮음으로 두면 잔상 · 불꽃 · 빛 번짐 같은 꾸밈 효과만 줄여 내장 그래픽에서도 부드럽게 움직입니다. 적 · 공격 · 경고 표시 · 피해 숫자는 그대로입니다."));
        rows.Add(y => CycleRow("UI 크기", y, GameSettings.UiScaleNames, () => GameSettings.UiScaleIndex, v => GameSettings.UiScaleIndex = v));
        rows.Add(y => CycleRow("안내 메시지 크기", y, Notice.SizeNames, () => GameSettings.NoticeSize, v => GameSettings.NoticeSize = v,
                 "게임 중 뜨는 큰 알림 · 오른쪽 위 소식 · 도움말 글자의 크기입니다."));
        rows.Add(y => ToggleRow("수직 동기화", y, () => GameSettings.VSync, v =>
        {
            GameSettings.VSync = v;
            if (maxFpsDropdown != null) maxFpsDropdown.SetUsable(!v);
        }, "켜면 모니터 주사율에 맞춰 그려 화면 찢김이 사라집니다. 대신 입력이 조금 늦어질 수 있습니다."));
        rows.Add(y => ToggleRow("프레임 표시", y, () => GameSettings.ShowFps, v => GameSettings.ShowFps = v,
                  "화면 오른쪽 위 구석에 초당 프레임 수(FPS)를 작게 표시합니다."));
        rows.Add(y => ToggleRow("화면 흔들림", y, () => GameSettings.ScreenShake, v => GameSettings.ScreenShake = v));
        rows.Add(y => ToggleRow("번쩍임 효과", y, () => GameSettings.Flashes, v => GameSettings.Flashes = v));
        float[] ys = rows.Count > RowsY10.Length ? RowsY11 : RowsY10;
        rowH = rows.Count > RowsY10.Length ? 44f : 48f;
        for (int i = 0; i < rows.Count; i++) rows[i](ys[i]);
    }

    static readonly float[] RowsY9 = { 235f, 175f, 115f, 55f, -5f, -65f, -125f, -185f, -245f };
    static readonly float[] RowsY10 = { 238f, 185f, 132f, 79f, 26f, -27f, -80f, -133f, -186f, -239f };
    // 화면 탭 11줄 (모니터가 둘 이상): 50 간격 · 높이 44 — 윗변 266 (탭 버튼 아랫변 270 아래) · 아랫변 -278 (「닫기」 윗변 -296 위, 페이지 기준)
    static readonly float[] RowsY11 = { 244f, 194f, 144f, 94f, 44f, -6f, -56f, -106f, -156f, -206f, -256f };
    static readonly float[] RowsY6 = { 200f, 125f, 50f, -25f, -100f, -175f, -250f };

    // 창이 있는 모니터 크기 (예전엔 Display.main = 주 모니터라 보조 모니터에서 전체 화면 해상도가 맞지 않았음)
    static Vector2Int Monitor => GameSettings.MonitorSize;

    // 모니터: 드롭다운 (모니터 1 (1920×1080) …) — 고르면 창을 옮기고, 해상도 목록이 새 모니터 것으로 바뀌므로 창이 옮겨진 뒤 탭을 다시 그림
    static void MonitorRow(float y, List<DisplayInfo> monitors)
    {
        RowTip(RowLabel("모니터", y, LabelWide), "모니터", "게임 창을 띄울 모니터입니다. 전체 화면이면 그 모니터의 해상도로 맞춥니다. 「이 탭 기본값으로」는 이 값을 바꾸지 않습니다.");
        string[] names = new string[monitors.Count];
        for (int i = 0; i < monitors.Count; i++)
            names[i] = Loc.T("모니터 {0}").Replace("{0}", (i + 1).ToString()) + " (" + monitors[i].width + "×" + monitors[i].height + ")";
        // 항목 글자는 만들 때 번역해 둠 (언어는 일반 탭에서 바꾸므로 이 탭으로 돌아오면 다시 그려짐)
        UIKit.MakeDropdown(page, new Vector2(95f, y), new Vector2(250f, rowH), names, false, () =>
        {
            int at = GameSettings.CurrentDisplayIndex(monitors);
            return at >= 0 ? at : Mathf.Clamp(GameSettings.DisplayIndex, 0, monitors.Count - 1);
        }, i =>
        {
            if (i < 0 || i >= monitors.Count) return;
            // 1.2.5~ 정해진 0.4초 뒤가 아니라 옮기기가 끝나 전체 화면이 돌아온 뒤에 다시 그림 (지나가는 창 모드를 chosenMode 로 담지 않게)
            GameSettings.DisplaySettled -= RedrawAfterMove;
            GameSettings.DisplaySettled += RedrawAfterMove;
            GameSettings.DisplayIndex = i;
        }, 22f, 7);
    }

    static void RedrawAfterMove()
    {
        GameSettings.DisplaySettled -= RedrawAfterMove;         // 한 번만 (구독이 쌓이지 않게)
        if (open != null && tab == 1) ShowTab(1);
    }

    static void DisplayModeRow(float y)
    {
        RowLabel("화면 모드", y, 360f);
        Button full = null, windowed = null;
        // SetResolution 은 다음 프레임에 적용되므로 Screen 값이 아니라 고른 모드 · 해상도로 표시 (바로 읽으면 반대로 켜졌음)
        chosenRes = new Vector2Int(Screen.width, Screen.height);
        chosenMode = Screen.fullScreenMode;
        System.Action<bool> highlightMode = isFull =>
        {
            Mark(full, isFull);
            Mark(windowed, !isFull);
        };
        full = UIKit.MakeButton(page, "전체 화면", new Vector2(-40f, y), new Vector2(250f, rowH), () =>
        {
            Vector2Int m = Monitor;
            chosenRes = m;
            chosenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(m.x, m.y, chosenMode);
            RefreshResolutions();
            Deselect();
            highlightMode(true);
        }, 26f);
        windowed = UIKit.MakeButton(page, "창 모드", new Vector2(230f, y), new Vector2(250f, rowH), () =>
        {
            // 화면을 꽉 채우는 크기면 창 테두리가 화면 밖으로 나가므로 한 단계 작은 해상도로
            Vector2Int r = chosenRes;
            Vector2Int m = Monitor;
            if (r.x >= m.x || r.y >= m.y)
            {
                List<Vector2Int> list = Resolutions();
                for (int i = list.Count - 1; i >= 0; i--)
                    if (list[i].x < m.x && list[i].y < m.y) { r = list[i]; break; }
            }
            chosenRes = r;
            chosenMode = FullScreenMode.Windowed;
            Screen.SetResolution(r.x, r.y, chosenMode);
            RefreshResolutions();
            Deselect();
            highlightMode(false);
        }, 26f);
        highlightMode(chosenMode != FullScreenMode.Windowed);
    }

    // 해상도: 1.2.4~ < > 로 넘기던 것을 드롭다운으로 (창 모드는 모니터보다 작은 것만 · 지금 화면 모드 그대로 적용)
    //   화면 모드 줄이 먼저 그려져 chosenRes · chosenMode 가 정해진 뒤에 부름
    static void ResolutionRow(float y)
    {
        RowLabel("해상도", y, LabelWide);
        resOptions = ResolutionOptions();
        resDropdown = UIKit.MakeDropdown(page, new Vector2(95f, y), new Vector2(250f, rowH), ResolutionNames(resOptions), false,
            () => resOptions.IndexOf(chosenRes), i =>
            {
                if (i < 0 || i >= resOptions.Count) return;
                chosenRes = resOptions[i];
                Screen.SetResolution(chosenRes.x, chosenRes.y, chosenMode);       // 실제 적용은 다음 프레임
            }, 26f, 7);
    }

    // 1.2.4~ 해상도 드롭다운: 고른 해상도 · 화면 모드 (SetResolution 이 다음 프레임에 적용돼 Screen 값 대신 이것으로 표시)
    static Vector2Int chosenRes;
    static FullScreenMode chosenMode;
    static List<Vector2Int> resOptions = new List<Vector2Int>();
    static DropdownAssist resDropdown, maxFpsDropdown, graphicsDropdown;

    // 드롭다운에 넣을 해상도: 창 모드면 모니터보다 작은 것만 (창 테두리 · 제목 표시줄이 화면 밖으로 나가지 않게, 「창 모드」 버튼과 같은 조건) + 지금 고른 것
    static List<Vector2Int> ResolutionOptions()
    {
        List<Vector2Int> list = Resolutions();
        if (chosenMode == FullScreenMode.Windowed)
        {
            Vector2Int m = Monitor;
            List<Vector2Int> fit = list.FindAll(v => v.x < m.x && v.y < m.y);
            if (fit.Count > 0) list = fit;
        }
        if (!list.Contains(chosenRes))
        {
            list.Add(chosenRes);
            list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        }
        return list;
    }

    static string[] ResolutionNames(List<Vector2Int> list)
    {
        string[] names = new string[list.Count];
        for (int i = 0; i < list.Count; i++) names[i] = list[i].x + " × " + list[i].y;
        return names;
    }

    // 화면 모드를 바꾸면 고를 수 있는 해상도도 바뀜
    static void RefreshResolutions()
    {
        resOptions = ResolutionOptions();
        if (resDropdown != null) resDropdown.SetOptions(ResolutionNames(resOptions), resOptions.IndexOf(chosenRes));
    }

    // 모니터가 지원하는 해상도 (가로 · 세로가 같은 것은 하나로, 작은 것부터)
    static List<Vector2Int> Resolutions()
    {
        List<Vector2Int> list = new List<Vector2Int>();
        foreach (Resolution r in Screen.resolutions)
        {
            Vector2Int v = new Vector2Int(r.width, r.height);
            if (v.x >= 800 && !list.Contains(v)) list.Add(v);
        }
        Vector2Int now = new Vector2Int(Screen.width, Screen.height);
        if (!list.Contains(now)) list.Add(now);
        list.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return list;
    }

    // 게임: 도움말 · 피해 숫자 · 색각 이상 모드 · 키 아이콘 · 게임 커서
    static void BuildGameplay()
    {
        // 2.1.8: 「레벨업 바로 열기」 한 줄을 더 넣으려고 줄 간격 110 → 90, 1.0.2: 「내 이펙트 투명도」 넣으려고 90 → 75
        // 1.0.4~ 줄마다 마우스를 올리면 한 줄 설명, 거의 효과가 없던 「키 아이콘」은 뺌 (줄 간격 75 → 80), 「투명도」 → 「불투명도」
        // 1.1.9~ 「커서를 창 안에 가두기」 · 「카드 한 번 클릭으로 확정」 두 줄을 더 넣어 9줄: 줄 간격 75 → 60, 버튼 높이 64 → 54 (rowH)
        //        위 끝 = 탭 버튼 아래(창 240) 밑, 아래 끝 = 「닫기」 위(창 -326) 위 (page 가운데가 창 -30)
        // 1.1.9~ 색각 이상 모드는 접근성, 피해 숫자 · 내 이펙트 불투명도는 표시 탭으로 옮겨 75 간격 · 높이 64 로 되돌림
        // 1.2~ 「망자의 전령」을 더해 7줄 (RowsY6 의 7칸을 다 씀)
        float[] y = RowsY6;
        ToggleRow("도움말 안내", y[0], () => GameSettings.Hints, v => GameSettings.Hints = v,
                  "처음 보는 기능마다 짧은 도움말을 한 번씩 띄웁니다.");
        // 1.0.4~: 본 도움말을 처음부터 다시 볼 수 있게 (켜기 · 끄기 옆)
        Button again = null;
        again = UIKit.MakeButton(page, "도움말 다시 보기", new Vector2(370f, y[0]), new Vector2(230f, rowH), () =>
        {
            Hints.ResetAll();
            Deselect();
            TMP_Text t = again.GetComponentInChildren<TMP_Text>();
            if (t != null) { t.text = Loc.T("처음부터 다시 보입니다"); UIKit.Remember(t, "처음부터 다시 보입니다"); }
        }, 22f);
        TMP_Text againLabel = again.GetComponentInChildren<TMP_Text>();
        if (againLabel != null) { againLabel.enableAutoSizing = true; againLabel.fontSizeMin = 20f; againLabel.fontSizeMax = 22f; }
        TMP_Text cursorLabel = ToggleRow("게임 커서", y[1], () => GameSettings.GameCursor, v => GameSettings.GameCursor = v,
                  "게임 전용 화살표와 조준점을 씁니다. 끄면 윈도우 기본 커서라 반응이 더 빠릅니다.");
        if (GamePad.Active && cursorLabel != null) cursorLabel.color = Off;       // 1.1.9~ 패드 중엔 뜻이 없어 흐리게 (누를 수는 있음 · 스팀 덱)
        ToggleRow("커서를 창 안에 가두기", y[2], () => GameSettings.ConfineCursor, v => GameSettings.ConfineCursor = v,
                  "전투 중 마우스가 창 밖으로 나가지 않게 합니다. 멈춘 화면 · 메뉴 · 패드를 쓸 때는 풀립니다. 창 모드나 모니터가 두 대일 때 유용합니다.");
        ToggleRow("레벨업 바로 열기", y[3], () => GameSettings.AutoLevelUp, v => GameSettings.AutoLevelUp = v,
                  "켜면 레벨이 오르자마자 카드 창이 열립니다. 끄면 포인트가 쌓이고 [{INTERACT}]로 원할 때 고릅니다.");
        ToggleRow("카드 한 번 클릭으로 확정", y[4], () => GameSettings.OneClickPick, v => GameSettings.OneClickPick = v,
                  "켜면 레벨업 · 보물 상자 · 보스 보상 카드를 한 번만 눌러도 바로 확정됩니다. 끄면 누른 뒤 [{INTERACT}] · 더블클릭으로 확정합니다. 영혼 트리 칸은 늘 한 번에 배우고, 무기 진화는 늘 두 번 확인합니다.");
        ToggleRow("herald.setting", y[5], () => GameSettings.Herald, v => GameSettings.Herald = v, "herald.setting.desc");
        // 1.2~ 발견형 도감의 스포일러 허용 (줄 간격 80 → 75 로 한 줄 더)
        ToggleRow("도감 모두 보기", y[6], () => CodexSeen.ShowAll, v => CodexSeen.ShowAll = v,
                  "켜면 도감의 진화 · 유물 · 각인 · 비밀 탭에서 아직 못 얻은 것도 모두 보입니다 (스포일러). 수집률과 보상에는 들어가지 않습니다.");
    }

    // ================================================================= 1.1.9~ 접근성 (9줄 · 60 간격 · 높이 54, ⑧ ⑨ 는 비워 둠)
    static void BuildAccess()
    {
        float[] y = RowsY9;
        ToggleRow("색각 이상 모드", y[0], () => GameSettings.ColorBlind, v => GameSettings.ColorBlind = v,
                  "경고 표시를 빨강 · 초록 대신 주황 · 파랑 · 자홍으로 그립니다.");
        ToggleRow("공격 고정", y[1], () => GameSettings.AttackLock, v => GameSettings.AttackLock = v,
                  "켜면 공격 키를 한 번 누르면 손을 떼도 계속 쏘고, 다시 누르면 멈춥니다. 조준은 마우스 그대로입니다. 궁수는 가득 당겨지면 자동으로 놓되 「완벽!」 보너스는 받지 못하고, 숨이 차면 숨을 고릅니다.");
        ToggleRow("필살기 누르면 토글", y[2], () => GameSettings.UltToggle, v => GameSettings.UltToggle = v,
                  "켜면 필살기 키를 누르고 있는 대신 한 번 누르면 조준이 시작되고, 다시 누르면 발동합니다. 오래 누르기가 힘들 때 유용합니다.");
        CycleRow("대사 속도", y[3], GameSettings.DialogueSpeedNames, () => GameSettings.DialogueSpeed, v => GameSettings.DialogueSpeed = v,
                 "이야기 · 보스 대사 글자가 나타나는 속도입니다. 「즉시」는 한 줄이 한 번에 다 나옵니다.");
        CycleRow("보스 등장 대사", y[4], GameSettings.BossIntroNames, () => GameSettings.BossIntroAlways ? 1 : 0, v => GameSettings.BossIntroAlways = v == 1,
                 "보스가 처음 나올 때는 늘 대사를 봅니다. 「처음만」이면 다음 판부터 이름과 다시 만난 한마디만 짧게 보여 주고 바로 싸웁니다.");
        ToggleRow("움직임 줄이기", y[5], () => GameSettings.ReduceMotion, v => GameSettings.ReduceMotion = v,
                  "버튼 · 배지의 맥동, 카드 뒤집기, 체력바 흔들림, 알림이 들어오는 움직임을 없애고 제자리에서 바로 보여 줍니다. 켜면 화면 흔들림도 함께 꺼집니다. 조준 줌과 진화 연출은 그대로입니다.");
        ToggleRow("빠른 연출", y[6], () => GameSettings.FastFx, v => GameSettings.FastFx = v,
                  "보스 처치, 진화, 상자 · 보상, 장 전환, 엔딩 같은 조작하지 않는 연출을 절반 시간으로 보여 줍니다. 상점 · 포털 카운트다운, 보스 결계 · 등장 연출, 카드 실수 방지 시간은 그대로입니다.");
    }

    // ================================================================= 1.1.9~ 표시 (전장 · HUD 에 그려지는 것 — 75 간격 · 높이 64, 남은 두 줄은 비워 둠)
    static void BuildHud()
    {
        float[] y = RowsY6;
        CycleRow("피해 숫자", y[0], GameSettings.DamageNumberNames, () => GameSettings.DamageNumbers, v => GameSettings.DamageNumbers = v,
                 "적에게 준 피해를 숫자로 띄웁니다. 끄면 화면이 깔끔해집니다.");
        CycleRow("내 이펙트 불투명도", y[1], GameSettings.MyFxAlphaNames, () => GameSettings.MyFxAlphaIndex, v => GameSettings.MyFxAlphaIndex = v,
                 "내 공격 · 스킬 · 필살기 이펙트의 진하기입니다. 낮추면 캐릭터와 적의 공격이 잘 보입니다. (적 공격은 그대로)");
        CycleRow("전투 통계", y[2], GameSettings.CombatStatsNames, () => GameSettings.CombatStats, v => GameSettings.CombatStats = v,
                 "화면 위에 최근 10초의 초당 피해와 최근 1분의 분당 처치 수를 보여 줍니다. 멈춘 동안은 흐리게 보입니다.");
        ToggleRow("획득 범위 표시", y[3], () => GameSettings.ShowPickupRange, v => GameSettings.ShowPickupRange = v,
                  "코인을 끌어오는 범위를 캐릭터 둘레에 연한 고리로 늘 보여 줍니다. 꺼도 코인 자석 카드를 고른 직후에는 잠깐 보입니다.");
        ToggleRow("화면 밖 표시", y[4], () => GameSettings.EdgeMarkers, v => GameSettings.EdgeMarkers = v,
                  "보물 상자 · 떠돌이 상점 · 황금 고블린 · 중간 보스가 화면 밖에 있으면 화면 가장자리에 방향과 남은 시간을 보여 줍니다. 석관 · 영혼 제단 화살표와 포털 안내선은 늘 보입니다.");
    }

    // 1.0.4~ 설정 줄 이름에 마우스를 올리면 한 줄 설명 (글자는 TooltipUI.Show 가 Loc.T 로 번역)
    static void RowTip(TMP_Text label, string ko, string tip)
    {
        if (label == null || string.IsNullOrEmpty(tip)) return;
        label.raycastTarget = true;
        TooltipTrigger t = label.gameObject.AddComponent<TooltipTrigger>();
        t.title = ko;
        t.body = tip;
    }

    // 1.2.4~ 펼쳐서 고르는 줄 (CycleRow 와 같은 자리 · 크기, 항목 글자는 Loc.T 로 번역)
    static DropdownAssist DropdownRow(string ko, float y, string[] names, System.Func<int> get, System.Action<int> set, string tip = null)
    {
        RowTip(RowLabel(ko, y, LabelWide), ko, tip);
        return UIKit.MakeDropdown(page, new Vector2(95f, y), new Vector2(250f, rowH), names, true, get, set, 26f, 7);
    }

    // 누를 때마다 다음 값으로
    static void CycleRow(string ko, float y, string[] names, System.Func<int> get, System.Action<int> set, string tip = null)
    {
        RowTip(RowLabel(ko, y, LabelWide), ko, tip);
        TMP_Text label = null;
        Button b = null;
        System.Action refresh = () =>
        {
            string text = names[Mathf.Clamp(get(), 0, names.Length - 1)];
            label.text = Loc.T(text);
            UIKit.Remember(label, text);
            if (UiRules.Legacy) b.GetComponent<Image>().color = Gold;
            else UiV2.Choose(b, false);         // 순환 값: 늘 같은 모습 (금색 칠 없이)
        };
        b = UIKit.MakeButton(page, "", new Vector2(95f, y), new Vector2(250f, rowH), () =>
        {
            set((get() + 1) % names.Length);
            Deselect();
            refresh();
        }, 26f);
        label = b.GetComponentInChildren<TMP_Text>();
        refresh();
    }

    static TMP_Text ToggleRow(string ko, float y, System.Func<bool> get, System.Action<bool> set, string tip = null)
    {
        TMP_Text rowLabel = RowLabel(ko, y, LabelWide);
        RowTip(rowLabel, ko, tip);
        Button b = null;
        TMP_Text label = null;
        System.Action refresh = () =>
        {
            bool on = get();
            string text = on ? "켜짐" : "꺼짐";
            label.text = Loc.T(text);
            UIKit.Remember(label, text);
            if (UiRules.Legacy) b.GetComponent<Image>().color = on ? Gold : Off;
            else UiV2.Toggle(b, on);
        };
        b = UIKit.MakeButton(page, "", new Vector2(95f, y), new Vector2(250f, rowH), () =>
        {
            set(!get());
            Deselect();
            refresh();
        }, 26f);
        label = b.GetComponentInChildren<TMP_Text>();
        refresh();
        return rowLabel;
    }

    // ================================================================= 조작
    static readonly string[] ActionNames =
    {
        "위로 이동", "아래로 이동", "왼쪽 이동", "오른쪽 이동", "재장전", "무기 교체",
        "스킬 1", "스킬 2", "스킬 3", "상호작용 (상점 · 확정)", "영혼 트리", "카드 다시 뽑기",
        "공격", "필살기",           // 1.0.4~ 좌 · 우클릭도 바꿀 수 있음
        "유물",                     // 1.2~ [Q] 유물 칸 (봉인된 석관)
    };
    // 설정에 보이는 키 (무기 교체 · 스킬 1~3은 이제 쓰지 않음), 1.0.4~ 공격 · 필살기를 넣어 두 줄 × 5칸
    // 1.2~ 유물(14)을 넣어 두 줄 × 6칸 (줄 간격 78 → 63 · 버튼 높이 60 → 56, 아래 게임패드 안내와 겹치지 않게)
    static readonly int[] ShownActions = { 0, 1, 2, 3, 4, 14, 12, 13, 9, 10, 11 };
    const int RowsPerColumn = 6;
    const string PadLayoutKo = "왼쪽 스틱 이동 · 오른쪽 스틱 조준(기울이면 자동 공격) · RT 공격 · LT 필살기 · A 상호작용 · 확정 · B 취소 · X 재장전 · Y 유물\nRB · LB · L3 스킬 1 · 2 · 3 · View 영혼 트리 · Menu 일시정지 · 메뉴에서는 왼쪽 스틱 · 십자키로 커서, A 클릭, 오른쪽 스틱 스크롤";

    static void BuildControls()
    {
        for (int j = 0; j < ShownActions.Length; j++)
        {
            int i = ShownActions[j];
            int index = i;
            int col = j < RowsPerColumn ? 0 : 1;
            float y = 222f - 63f * (j % RowsPerColumn);
            float x = col == 0 ? -540f : 20f;
            UIKit.Text(page, ActionNames[i], 26f, Parch, new Vector2(x + 170f, y), new Vector2(340f, 50f), TextAlignmentOptions.Left);
            Button b = UIKit.MakeButton(page, "", new Vector2(x + 430f, y), new Vector2(170f, 56f), () =>
            {
                capturing = index;
                Deselect();                 // Space · Enter가 버튼을 다시 누르지 않게
                RefreshKeys();
            }, 26f);
            keyLabels.Add(b.GetComponentInChildren<TMP_Text>());
        }
        // 1.0.5~ 게임패드 버튼 배치 (바꿀 수 없음, 엑스박스 배열 · 스팀 덱 · 플레이스테이션은 스팀 입력으로 같은 배열)
        // 1.1~ 두 줄이 폭을 넘어 4줄로 접히며 약 14pt 까지 줄었음 → 위 키 칸(아래 끝 -122) · 아래 안내(위 끝 -233) 사이를 꽉 채움
        UIKit.Text(page, "게임패드", 24f, UiRules.Legacy ? Gold : UiTokens.Text2, new Vector2(-470f, -172f), new Vector2(160f, 40f), TextAlignmentOptions.Left);
        UIKit.Text(page, PadLayoutKo, 19f, Parch, new Vector2(80f, -173f), new Vector2(940f, 98f), TextAlignmentOptions.Left);
        // 1.1.9~ 「기본값으로」 버튼은 창 아래 「이 탭 기본값으로」가 대신함
        UIKit.Text(page, "버튼을 누른 뒤 바꿀 키나 마우스 버튼을 누르세요 (ESC: 취소)", 22f, Off, new Vector2(-160f, -255f), new Vector2(760f, 44f), TextAlignmentOptions.Left);
        RefreshKeys();
    }

    static void RefreshKeys()
    {
        for (int j = 0; j < keyLabels.Count && j < ShownActions.Length; j++)
        {
            if (keyLabels[j] == null) continue;
            int i = ShownActions[j];
            bool waiting = i == capturing;
            bool denied = !waiting && i == blocked;
            keyLabels[j].text = waiting ? Loc.T("키를 누르세요") : denied ? Loc.T("사용 중") : KeyBindings.KeyName(KeyBindings.Get(KeyBindings.All[i]));     // 1.0.5~ 패드를 써도 키보드 키 이름
            keyLabels[j].color = waiting ? (UiRules.Legacy ? Gold : UiTokens.Accent) : denied ? new Color(1f, 0.45f, 0.4f) : new Color(0.96f, 0.9f, 0.8f);
        }
    }

    static readonly KeyCode[] Bindable = BuildBindable();

    static KeyCode[] BuildBindable()
    {
        List<KeyCode> list = new List<KeyCode>();
        foreach (KeyCode k in (KeyCode[])System.Enum.GetValues(typeof(KeyCode)))
            if (k != KeyCode.None && k != KeyCode.Escape && k < KeyCode.Mouse0 && !list.Contains(k)) list.Add(k);
        for (KeyCode k = KeyCode.Mouse0; k <= KeyCode.Mouse6; k++) list.Add(k);       // 1.0.4~ 마우스 버튼 (좌클릭은 공격 · 필살기에만)
        return list.ToArray();
    }

    // 창이 열려 있는 동안 매 프레임 (SettingsInput): 키 입력 받기 · ESC
    internal static void Tick()
    {
        if ((ResetArmed && Time.unscaledTime - resetArmedAt >= 2f) || (resetDoneAt > -99f && Time.unscaledTime - resetDoneAt >= 1.5f)) DisarmReset();
        TickDelete();
        if (TelemetryConsentUI.IsOpen) return;          // 1.4.4~ 「전문 보기」 창이 위에 떠 있으면 ESC · 키 입력은 그 창이 받음
        if (GameInput.EscDown && EscHandledFrame != Time.frameCount)     // 같은 프레임에 「전문 보기」 창이 ESC 로 닫혔으면 설정은 그대로
        {
            EscHandledFrame = Time.frameCount;
            if (DropdownAssist.HideOpen()) return;         // 1.2.4~ 펼친 드롭다운 목록만 닫음 (설정 창은 그대로)
            if (capturing >= 0) { capturing = -1; RefreshKeys(); }
            else Close();
            return;
        }
        if (blocked >= 0 && Time.unscaledTime >= blockedUntil) { blocked = -1; RefreshKeys(); }
        if (capturing < 0) return;
        // 1.1~ 기다리는 칸 밖을 좌클릭하면 취소 (닫기 · 빈 곳을 눌러 취소하려다 좌클릭이 들어가지 않게)
        if (Input.GetKeyDown(KeyCode.Mouse0) && !PointerOnCapturing())
        {
            capturing = -1;
            RefreshKeys();
            return;
        }
        foreach (KeyCode k in Bindable)
        {
            if (!Input.GetKeyDown(k) || !KeyBindings.CanBind(KeyBindings.All[capturing], k)) continue;      // 1.0.4~ 받을 수 없는 키(좌클릭)는 무시하고 계속 기다림
            int at = capturing;
            capturing = -1;
            if (!KeyBindings.Set(KeyBindings.All[at], k)) { blocked = at; blockedUntil = Time.unscaledTime + 1.2f; }     // 1.1~ 맞바꿀 수 없는 키: 잠깐 「사용 중」
            else blocked = -1;
            RefreshKeys();
            return;
        }
    }

    // 1.1~ 마우스가 지금 키 입력을 기다리는 칸 위에 있는지
    static bool PointerOnCapturing()
    {
        int j = System.Array.IndexOf(ShownActions, capturing);
        if (j < 0 || j >= keyLabels.Count || keyLabels[j] == null) return false;
        RectTransform box = keyLabels[j].transform.parent as RectTransform;
        if (box == null) return false;
        Canvas c = box.GetComponentInParent<Canvas>();
        Camera cam = c != null && c.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? c.rootCanvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(box, Input.mousePosition, cam);
    }

    static void Deselect()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    static Slider MakeSlider(Transform parent, Vector2 pos, Vector2 size)
    {
        RectTransform root = UIKit.Rect("VolumeSlider", parent, pos, size);
        Slider s = root.gameObject.AddComponent<Slider>();

        // 1.4.3~ 클릭 영역: 얇은 막대(높이 40%)만 눌리던 것을 줄 전체 + 위아래 · 양 끝 14px 까지 (게이지 아무 곳이나 누르면 그 위치로 바로 이동 · 끝 바깥은 0% · 100%)
        RectTransform hit = UIKit.Rect("Hit Area", root, Vector2.zero, Vector2.zero);
        hit.anchorMin = Vector2.zero; hit.anchorMax = Vector2.one;
        hit.offsetMin = new Vector2(-14f, -14f); hit.offsetMax = new Vector2(14f, 14f);
        hit.gameObject.AddComponent<Image>().color = Color.clear;

        RectTransform bg = UIKit.Rect("Background", root, Vector2.zero, Vector2.zero);
        bg.anchorMin = new Vector2(0f, 0.3f); bg.anchorMax = new Vector2(1f, 0.7f);
        bg.offsetMin = bg.offsetMax = Vector2.zero;
        bg.gameObject.AddComponent<Image>().color = UiRules.Legacy ? new Color(0.25f, 0.22f, 0.3f) : UiTokens.LineDim;

        RectTransform area = UIKit.Rect("Fill Area", root, Vector2.zero, Vector2.zero);
        area.anchorMin = new Vector2(0f, 0.3f); area.anchorMax = new Vector2(1f, 0.7f);
        area.offsetMin = area.offsetMax = Vector2.zero;
        RectTransform fill = UIKit.Rect("Fill", area, Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = UiRules.Legacy ? new Color(0.96f, 0.75f, 0.3f) : UiTokens.Accent;      // UI 개편: 금 → 청록

        RectTransform handleArea = UIKit.Rect("Handle Slide Area", root, Vector2.zero, Vector2.zero);
        handleArea.anchorMin = Vector2.zero; handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(12f, 0f); handleArea.offsetMax = new Vector2(-12f, 0f);
        RectTransform handle = UIKit.Rect("Handle", handleArea, Vector2.zero, new Vector2(28f, 0f));
        handle.anchorMin = new Vector2(0f, 0f); handle.anchorMax = new Vector2(0f, 1f);
        handle.sizeDelta = new Vector2(28f, 12f);
        Image hi = handle.gameObject.AddComponent<Image>();
        hi.sprite = UIKit.ButtonSprite;
        hi.type = Image.Type.Sliced;
        hi.color = Color.white;
        if (!UiRules.Legacy && UiRules.Sprite("ui2_slider_knob_n") != null)
        {
            // 찌그러진 리벳 버튼 대신 도트 손잡이 (4x8 도트 × 2 = 24x48) · 마우스 올림은 청록
            hi.sprite = UiRules.Sprite("ui2_slider_knob_n");
            hi.type = Image.Type.Simple;
            handle.sizeDelta = new Vector2(24f, 24f);
            Slider sl = root.GetComponent<Slider>();
            if (sl != null) { sl.transition = Selectable.Transition.SpriteSwap; sl.spriteState = new SpriteState { highlightedSprite = UiRules.Sprite("ui2_slider_knob_h"), selectedSprite = UiRules.Sprite("ui2_slider_knob_h"), pressedSprite = UiRules.Sprite("ui2_slider_knob_h") }; }
        }

        s.fillRect = fill;
        s.handleRect = handle;
        s.targetGraphic = hi;
        s.minValue = 0f;
        s.maxValue = 1f;
        return s;
    }

    public static void Close()
    {
        capturing = -1;
        resetArmedAt = resetDoneAt = -99f;
        resetButton = null;
        deleteButton = null;
        telemetryIdLabel = null;
        deleteArmedAt = deleteDoneAt = -99f;
        TelemetryConsentUI.Close();             // 1.4.4~ 「전문 보기」 창이 남지 않게
        GameSettings.Save();
        if (open != null) Object.Destroy(open);
        open = null;
    }
}

// 1.0.4~: 확인 창의 ESC = 취소 (UIKit.EscCancel)
public class ModalEsc : MonoBehaviour
{
    public UnityAction cancel;

    void Update()
    {
        if (!GameInput.EscDown || SettingsUI.EscHandledFrame == Time.frameCount) return;
        SettingsUI.EscHandledFrame = Time.frameCount;
        cancel?.Invoke();
    }
}

// 1.0.4~: 언어가 바뀌면 refresh 를 다시 부름 (여러 조각을 이어 붙인 글처럼 UIKit.Remember 로 안 되는 글)
public class LocRefresh : MonoBehaviour
{
    public System.Action refresh;
    void OnEnable() => Loc.Changed += Run;
    void OnDisable() => Loc.Changed -= Run;
    void Run() => refresh?.Invoke();
}
// 1.2.4~ UIKit.MakeDropdown 이 드롭다운 루트에 붙임 (템플릿 밖이라 펼칠 때 복제되지 않음)
//   옵션 글자 번역 · 언어가 바뀌면 다시 · 값 다시 읽기(Sync) · 흐리게(SetUsable)
//   펼쳤을 때: 지금 값이 보이게 스크롤, 패드 · 키보드로 고른 항목이 가려지면 따라 스크롤 (패드는 한 칸 앞까지 미리 보여 십자키가 다음 항목에 붙게)
//   닫히면 선택을 풀어 Space · Enter 가 다시 펼치지 않게 (설정 창 버튼들과 같은 규칙) · ESC · B 는 SettingsUI.Tick 이 HideOpen 으로 목록만 닫음
public class DropdownAssist : MonoBehaviour
{
    static readonly List<DropdownAssist> live = new List<DropdownAssist>();
    static readonly List<string> scratch = new List<string>();

    public TMP_Dropdown Dropdown { get; private set; }
    TMP_Text caption;
    Image[] arrow;
    float itemH;
    string[] names;
    bool translate;
    System.Func<int> get;

    bool wasOpen;
    ScrollRect scroll;
    RectTransform content, viewport;
    Camera cam;
    GameObject lastSel;

    internal void Init(TMP_Dropdown dd, TMP_Text caption, Image[] arrow, float itemH, string[] names, bool translate, System.Func<int> get)
    {
        Dropdown = dd;
        this.caption = caption;
        this.arrow = arrow;
        this.itemH = itemH;
        this.translate = translate;
        this.get = get;
        SetOptions(names, get != null ? get() : 0);
    }

    void OnEnable() { live.Add(this); Loc.Changed += Relabel; }
    void OnDisable() { live.Remove(this); Loc.Changed -= Relabel; }

    // 펼친 목록이 있으면 닫고 true (ESC · B 가 설정 창까지 닫지 않게)
    public static bool HideOpen()
    {
        bool any = false;
        for (int i = 0; i < live.Count; i++)
        {
            TMP_Dropdown d = live[i] != null ? live[i].Dropdown : null;
            if (d == null || !d.IsExpanded) continue;
            d.Hide();
            any = true;
        }
        return any;
    }

    // 항목을 바꿈 (해상도 목록처럼 상황에 따라 달라질 때) · 알림 없이 value 만 맞춤
    public void SetOptions(string[] names, int value)
    {
        this.names = names;
        if (Dropdown == null || names == null) return;
        if (Dropdown.IsExpanded) Dropdown.Hide();
        scratch.Clear();
        for (int i = 0; i < names.Length; i++) scratch.Add(translate ? Loc.T(names[i]) : names[i]);
        Dropdown.ClearOptions();
        Dropdown.AddOptions(scratch);
        scratch.Clear();
        Dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, Mathf.Max(0, names.Length - 1)));
        Dropdown.RefreshShownValue();
    }

    // 설정 값이 밖에서 바뀌었을 때 (기본값 되돌리기 등) 표시를 다시 맞춤
    public void Sync()
    {
        if (Dropdown == null || get == null || names == null) return;
        Dropdown.SetValueWithoutNotify(Mathf.Clamp(get(), 0, Mathf.Max(0, names.Length - 1)));
        Dropdown.RefreshShownValue();
    }

    // 누를 수 없게 흐리게 (수직 동기화가 켜지면 최대 프레임)
    public void SetUsable(bool on)
    {
        if (Dropdown == null) return;
        if (!on && Dropdown.IsExpanded) Dropdown.Hide();
        Dropdown.interactable = on;
        float a = on ? 1f : 0.4f;
        if (caption != null) caption.alpha = a;
        if (arrow != null)
            for (int i = 0; i < arrow.Length; i++)
                if (arrow[i] != null) { Color c = arrow[i].color; c.a = a; arrow[i].color = c; }
    }

    void Relabel()
    {
        if (!translate || Dropdown == null) return;
        SetOptions(names, Dropdown.value);
    }

    static GameObject Selected()
    {
        UnityEngine.EventSystems.EventSystem es = UnityEngine.EventSystems.EventSystem.current;
        return es != null ? es.currentSelectedGameObject : null;
    }

    void Update()
    {
        if (Dropdown == null) return;
        if (!Dropdown.IsExpanded)
        {
            if (!wasOpen) return;
            wasOpen = false;
            scroll = null;
            content = viewport = null;
            lastSel = null;
            if (Selected() == gameObject) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            return;
        }
        if (!wasOpen || scroll == null)
        {
            wasOpen = true;
            Transform list = transform.Find("Dropdown List");      // TMP_Dropdown 이 템플릿을 복제해 이 이름으로 루트 밑에 둠
            scroll = list != null ? list.GetComponent<ScrollRect>() : null;
            if (scroll == null) return;
            content = scroll.content;
            viewport = scroll.viewport;
            Canvas c = GetComponentInParent<Canvas>();
            Canvas root = c != null ? c.rootCanvas : null;
            cam = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
            // 항목에 버튼 소리를 바로 붙임 (UISound 가 0.25초마다 찾아 붙이기 전에도 올림 · 누름 소리 · 템플릿이 아니라 복제된 항목에)
            for (int i = 0; i < content.childCount; i++)
            {
                Transform t = content.GetChild(i);
                if (t.gameObject.activeSelf && !t.TryGetComponent(out UISoundHook _)) t.gameObject.AddComponent<UISoundHook>();
            }
            // 펼치자마자 지금 값을 가운데쯤 (해상도처럼 항목이 많으면 지금 값이 아래에 가려져 있었음)
            Reveal(Dropdown.value, 0f, true);
            lastSel = Selected();
            if (GamePad.Active && lastSel != null && lastSel.transform.parent == content) GamePad.WarpTo((RectTransform)lastSel.transform);
            return;
        }
        GameObject sel = Selected();
        if (sel == lastSel) return;
        lastSel = sel;
        if (sel == null || sel.transform.parent != content) return;
        RectTransform item = (RectTransform)sel.transform;
        bool pad = GamePad.Active;
        // 마우스를 올려 고른 것은 그대로 (따라 스크롤하면 마우스 밑 항목이 바뀌며 계속 내려감)
        if (!pad && RectTransformUtility.RectangleContainsScreenPoint(item, Input.mousePosition, cam)) return;
        if (Reveal(item.GetSiblingIndex() - 1, pad ? itemH : 0f, false) && pad) GamePad.WarpTo(item);     // 커서도 그 항목을 따라감
    }

    // i 번째 항목(위에서부터)이 보이게 목록을 옮김 · margin 만큼 앞뒤 항목도 · 옮겼으면 true
    bool Reveal(int i, float margin, bool center)
    {
        if (content == null || viewport == null || i < 0) return false;
        float vh = viewport.rect.height;
        float max = Mathf.Max(0f, content.rect.height - vh);
        float top = i * itemH, bottom = top + itemH;
        Vector2 p = content.anchoredPosition;
        float want = p.y;
        if (center) want = top - (vh - itemH) * 0.5f;
        else if (top - margin < p.y) want = top - margin;
        else if (bottom + margin > p.y + vh) want = bottom + margin - vh;
        want = Mathf.Clamp(want, 0f, max);
        if (Mathf.Abs(want - p.y) < 0.5f) return false;
        content.anchoredPosition = new Vector2(p.x, want);
        scroll.velocity = Vector2.zero;
        return true;
    }
}

// 1.0.4~: 처음 켰을 때 한 번 언어 고르기 (스팀 → OS 언어로 미리 골라 둔 것을 강조, 언어 이름은 그 언어 글꼴로)
public static class LanguagePicker
{
    static GameObject open;

    public static void Open(Transform root, System.Action then)
    {
        if (open != null) return;
        if (GameSettings.LanguageChosen || GameInput.Auto || Application.isBatchMode) { then?.Invoke(); return; }
        RectTransform win = UIKit.Modal(root, "LanguagePicker", new Vector2(1000f, 440f), out open);
        GameObject mine = open;
        TMP_Text title = UIKit.Text(win, "", 44f, UiRules.Legacy ? new Color(0.96f, 0.83f, 0.47f) : UiTokens.Text1, new Vector2(0f, 145f), new Vector2(920f, 64f));
        title.text = Loc.T("언어를 고르세요") + (Loc.Current != Loc.Lang.English ? "  <size=60%><color=#A89C86>Language</color></size>" : "");
        UIKit.Text(win, "설정 → 일반에서 언제든 바꿀 수 있습니다.", 22f, new Color(0.66f, 0.61f, 0.53f), new Vector2(0f, -150f), new Vector2(900f, 36f));
        if (GameSettings.DeckApplied)
        {
            // 1.1.9~ 스팀 덱 첫 실행: 자동으로 맞춘 설정 안내 (창 아래 안내 줄 위)
            UIKit.Text(win, "스팀 덱에 맞춰 설정했습니다 (UI 크기 110% · 게임패드). 설정에서 바꿀 수 있습니다.", 20f, new Color(0.66f, 0.61f, 0.53f), new Vector2(0f, -118f), new Vector2(940f, 30f));
            GameSettings.DeckNoticed();
        }
        Button preset = null;
        bool done = false;
        System.Action<Loc.Lang> pick = lang =>
        {
            if (done) return;
            done = true;
            GameSettings.Language = lang;          // 같은 언어여도 「골랐음」으로 저장
            if (mine != null) Object.Destroy(mine);
            open = null;
            then?.Invoke();
        };
        for (int i = 0; i < 4; i++)
        {
            Loc.Lang lang = (Loc.Lang)i;
            Button b = UIKit.MakeButton(win, "", new Vector2(-345f + 230f * i, 10f), new Vector2(210f, 100f), () => pick(lang), 34f);
            TMP_Text label = b.GetComponentInChildren<TMP_Text>();
            UIKit.Forget(label);
            label.text = Loc.LangNames[i];
            TMP_FontAsset native = Loc.NativeFont(lang);
            if (native != null) label.font = native;
            bool on = lang == Loc.Current;
            if (!UiRules.Legacy) UiV2.Choose(b, on);
            else
            {
                b.GetComponent<Image>().color = on ? new Color(0.96f, 0.83f, 0.47f) : new Color(0.6f, 0.58f, 0.65f);
                b.transform.localScale = Vector3.one * (on ? 1.06f : 1f);
            }
            if (on) preset = b;
        }
        UIKit.SetDefault(preset);          // 1.1.9~ 미리 고른 언어를 선택해 둠 (Enter · Space 로 확정, ←→ 로 이동)
        // ESC 는 미리 골라 둔 언어 그대로
        UIKit.EscCancel(open, () => pick(Loc.Current));
    }
}

// 설정 창에 붙어서 SettingsUI.Tick을 불러 줌 (멈춘 화면에서도 Update는 돔)
public class SettingsInput : MonoBehaviour
{
    void Update() => SettingsUI.Tick();
}

// 1.4.8~ 글자 메뉴 버튼 (메인 메뉴 · ESC): 상자 대신 글자 · 마우스를 올리거나 패드로 고르면 금빛 글자 · 은은한 띠 · 아래 줄이 차오름
//   시간이 멈춘 ESC 에서도 움직이게 실제 시간 · 버튼 상자 그림은 투명으로 두고 누름만 받음
public class MenuItemFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.9f, 0.86f, 0.8f);
    static readonly Color Dim = new Color(0.5f, 0.48f, 0.5f);

    Button button;
    TMP_Text label;
    Image band, line;
    bool primary, locked, over, selected;
    float k;

    public static void Apply(Button b, float size, bool primary, bool locked)
    {
        if (b == null) return;
        MenuItemFx fx = b.GetComponent<MenuItemFx>();
        if (fx == null) fx = b.gameObject.AddComponent<MenuItemFx>();
        fx.button = b;
        fx.primary = primary;
        fx.locked = locked;
        b.transition = Selectable.Transition.None;
        Image img = b.GetComponent<Image>();
        if (img != null) img.color = new Color(1f, 1f, 1f, 0f);
        RectTransform r = (RectTransform)b.transform;
        fx.label = b.GetComponentInChildren<TMP_Text>(true);
        if (fx.label != null)
        {
            fx.label.enableAutoSizing = true;
            fx.label.fontSizeMax = size;
            fx.label.fontSizeMin = 20f;
            fx.label.alignment = TextAlignmentOptions.Center;
            // 1.4.8~ 굵게 + 어두운 외곽선 · 아래 그림자: 빨간 카펫 · 밝은 벽 위에서도 판 없이 읽히게
            //   외곽선은 TMP 가 켜진 뒤에만 넣을 수 있음 (꺼진 ESC 창에서 넣으면 NullReference) → Start 에서 (StyleLabel)
            fx.label.fontStyle |= FontStyles.Bold;
            if (fx.label.gameObject.activeInHierarchy) fx.StyleLabel();
            Shadow sh = fx.label.GetComponent<Shadow>();
            if (sh == null) sh = fx.label.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.6f);
            sh.effectDistance = new Vector2(0f, -3f);
        }
        if (fx.band == null)
        {
            // 뒤 띠: 가운데가 진하고 양끝이 옅은 금빛 (Sliced 버튼 그림을 아주 옅게)
            RectTransform br = UIKit.Rect("HoverBand", b.transform, Vector2.zero, new Vector2(r.sizeDelta.x, r.sizeDelta.y * 0.86f));
            br.SetAsFirstSibling();
            fx.band = br.gameObject.AddComponent<Image>();
            // 1.4.8~ 사용자 「버튼이 너무 투명해서 누를 수 있는 건지 모르겠다」: 평소에도 도트 판(금 테두리 돌판)을 옅게 · 고르면 진하게
            fx.band.sprite = UIKit.ButtonSprite;
            fx.band.type = Image.Type.Sliced;
            fx.band.raycastTarget = false;
            br.sizeDelta = r.sizeDelta;
            RectTransform lr = UIKit.Rect("HoverLine", b.transform, new Vector2(0f, -r.sizeDelta.y * 0.5f + 4f), new Vector2(r.sizeDelta.x * 0.5f, 2f));
            fx.line = lr.gameObject.AddComponent<Image>();
            fx.line.color = Gold;
            fx.line.raycastTarget = false;
        }
        fx.Paint(true);
    }

    bool styled;
    void Start() => StyleLabel();
    void StyleLabel()
    {
        if (styled || label == null) return;
        styled = true;
    }

    public void OnPointerEnter(PointerEventData e) => over = true;
    public void OnPointerExit(PointerEventData e) => over = false;
    public void OnSelect(BaseEventData e) => selected = true;
    public void OnDeselect(BaseEventData e) => selected = false;
    void OnDisable() { over = selected = false; k = 0f; Paint(true); }

    void Update()
    {
        float target = (over || selected) && button != null && button.interactable && !locked ? 1f : 0f;
        if (Mathf.Approximately(k, target)) return;
        k = Mathf.MoveTowards(k, target, Time.unscaledDeltaTime * 7f);
        Paint(false);
    }

    void Paint(bool force)
    {
        if (label != null)
        {
            Color rest = locked || (button != null && !button.interactable) ? Dim : primary ? Gold : Parch;
            label.color = Color.Lerp(rest, primary ? Color.white : Gold, k);
            label.characterSpacing = Mathf.Lerp(0f, 4f, k);
        }
        if (band != null) band.color = locked ? new Color(0.6f, 0.58f, 0.62f, 0.45f) : Color.Lerp(new Color(0.78f, 0.74f, 0.86f, 0.62f), new Color(1f, 0.94f, 0.78f, 1f), k);
        transform.localScale = Vector3.one * (1f + 0.035f * k);
        if (line != null)
        {
            line.color = new Color(Gold.r, Gold.g, Gold.b, primary ? 0.55f + 0.45f * k : k);
            RectTransform lr = line.rectTransform;
            float w = ((RectTransform)transform).sizeDelta.x;
            lr.sizeDelta = new Vector2(Mathf.Lerp(primary ? w * 0.4f : w * 0.2f, w * 0.7f, k), 2f);
        }
    }
}

// 1.4.8~ ESC 흐린 배경: 메뉴가 켜질 때 그 프레임 끝에 화면을 찍어(메뉴는 잠깐 투명) 1/8 로 줄였다가 늘려 흐리게 · 꺼지면 해제
//   진짜 블러 셰이더 없이 내려 뽑기(쌍선형)로 · 한 번만 찍으므로 멈춘 동안 비용 없음
public class EscBackdrop : MonoBehaviour
{
    public RawImage image;
    RenderTexture small;
    CanvasGroup group;

    void OnEnable()
    {
        if (image == null) return;
        image.enabled = false;
        StartCoroutine(Grab());
    }

    float hiddenUntil = -1f;
    System.Collections.IEnumerator Grab()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;                                   // 이 프레임 화면에 메뉴가 찍히지 않게
        hiddenUntil = Time.unscaledTime + 0.25f;            // 안전장치: 찍기가 끝나지 않으면(화면 없는 실행 · 오류) 0.25초 뒤 Update 가 메뉴를 다시 보이게
        yield return new WaitForEndOfFrame();
        Texture2D shot = null;
        try { shot = ScreenCapture.CaptureScreenshotAsTexture(); }
        finally { group.alpha = 1f; hiddenUntil = -1f; }
        if (shot == null) yield break;
        int w = Mathf.Max(16, shot.width / 8), h = Mathf.Max(9, shot.height / 8);
        if (small == null || small.width != w || small.height != h)
        {
            if (small != null) small.Release();
            small = new RenderTexture(w, h, 0) { filterMode = FilterMode.Bilinear };
        }
        RenderTexture mid = RenderTexture.GetTemporary(shot.width / 3, shot.height / 3, 0);
        mid.filterMode = FilterMode.Bilinear;
        Graphics.Blit(shot, mid);
        Graphics.Blit(mid, small);
        RenderTexture.ReleaseTemporary(mid);
        Destroy(shot);
        if (image != null) { image.texture = small; image.enabled = true; }
    }

    void Update()
    {
        if (hiddenUntil > 0f && Time.unscaledTime > hiddenUntil && group != null) { group.alpha = 1f; hiddenUntil = -1f; }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        hiddenUntil = -1f;
        if (group != null) group.alpha = 1f;
        if (image != null) image.enabled = false;
    }

    void OnDestroy()
    {
        if (small != null) { small.Release(); Destroy(small); }
    }
}

// 1.4.8~ 창 열림 연출: 막은 0.18초에 서서히 · 창은 0.94 → 1 로 커지며 나타남 (실제 시간 · 멈춘 중에도)
public class WindowIntro : MonoBehaviour
{
    public RectTransform window;
    CanvasGroup group;
    float t;

    void Start()
    {
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        Apply(0f);
    }

    void Update()
    {
        t += Time.unscaledDeltaTime / 0.18f;
        Apply(Mathf.Clamp01(t));
        if (t >= 1f) enabled = false;
    }

    void Apply(float k)
    {
        float e = 1f - (1f - k) * (1f - k);
        if (group != null) group.alpha = e;
        if (window != null) window.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, e);
    }
}

// 1.4.8~ 메인 메뉴 분위기: 가장자리를 어둡게(비네트) · 금빛 영혼 불티가 천천히 떠오름 · 제목이 은은하게 숨 쉼 · 메뉴가 차례로 나타남
public class MenuAmbience : MonoBehaviour
{
    public TMP_Text title;
    public RectTransform[] items;
    RectTransform embers;
    readonly System.Collections.Generic.List<(RectTransform r, Image img, float speed, float life, float age, float sway)> live
        = new System.Collections.Generic.List<(RectTransform, Image, float, float, float, float)>();
    float nextEmber, t;
    Vector2[] itemBase;
    CanvasGroup[] groups;

    public static void Install(Transform canvasRoot, Transform behind, TMP_Text title, RectTransform[] items)
    {
        UIKit.EnsureStyle();
        GameObject go = new GameObject("MenuAmbience", typeof(RectTransform));
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(canvasRoot, false);
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        if (behind != null) r.SetSiblingIndex(behind.GetSiblingIndex());
        if (!UiRules.Legacy)
        {
            // UI 개편 P0-4b: 채도 높은 배경(보라 벽 · 붉은 카펫)을 어둡게 눌러 메뉴 · 영웅이 앞에 서게
            RectTransform dk = UIKit.Rect("Darken", r, Vector2.zero, Vector2.zero);
            dk.anchorMin = Vector2.zero; dk.anchorMax = Vector2.one; dk.offsetMin = dk.offsetMax = Vector2.zero;
            Image di = dk.gameObject.AddComponent<Image>();
            di.color = new Color(UiTokens.Void.r, UiTokens.Void.g, UiTokens.Void.b, 0.5f);
            di.raycastTarget = false;
        }
        if (UIKit.VignetteSprite != null)
        {
            Image v = go.AddComponent<Image>();
            v.sprite = UIKit.VignetteSprite;
            v.raycastTarget = false;
        }
        MenuAmbience a = go.AddComponent<MenuAmbience>();
        a.title = title;
        a.items = items;
        GameObject e = new GameObject("Embers", typeof(RectTransform));
        a.embers = (RectTransform)e.transform;
        a.embers.SetParent(r, false);
        a.embers.anchorMin = Vector2.zero; a.embers.anchorMax = Vector2.one; a.embers.offsetMin = a.embers.offsetMax = Vector2.zero;
    }

    static bool enteredOnce;         // UI 개편: 차례로 올라오는 연출은 실행 뒤 첫 진입 때만

    void Start()
    {
        if (items == null) items = new RectTransform[0];
        if (!UiRules.Legacy)
        {
            if (enteredOnce) t = 10f;            // 바로 제자리
            enteredOnce = true;
            if (title != null) title.color = UiTokens.Text1;
        }
        itemBase = new Vector2[items.Length];
        groups = new CanvasGroup[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null) continue;
            itemBase[i] = items[i].anchoredPosition;
            groups[i] = items[i].GetComponent<CanvasGroup>();
            if (groups[i] == null) groups[i] = items[i].gameObject.AddComponent<CanvasGroup>();
            groups[i].alpha = 0f;
        }
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        t += dt;
        bool motion = GameSettings.Motion > 0.01f;
        // 메뉴가 0.25초부터 0.07초 간격으로 아래에서 살짝 올라오며 나타남 (끝나면 제자리 · 움직임 줄이기면 바로)
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null || groups[i] == null) continue;
            float k = motion ? Mathf.Clamp01((t - 0.25f - 0.07f * i) / 0.35f) : 1f;
            float e = 1f - (1f - k) * (1f - k) * (1f - k);
            groups[i].alpha = e;
            items[i].anchoredPosition = itemBase[i] + new Vector2(0f, -14f * (1f - e));
        }
        if (!UiRules.Legacy) return;           // 제목 숨쉬기 · 불티 없음
        // 제목: 밝기가 아주 천천히 오르내림
        if (title != null) title.color = Color.Lerp(new Color(0.9f, 0.88f, 0.84f), Color.white, 0.5f + 0.5f * Mathf.Sin(t * 1.3f));
        // 불티
        if (motion && UIKit.EmberSprite != null && embers != null && t >= nextEmber && live.Count < 28)
        {
            nextEmber = t + Random.Range(0.18f, 0.42f);
            RectTransform er = UIKit.Rect("Ember", embers, Vector2.zero, Vector2.one * Random.Range(10f, 22f));
            er.anchorMin = er.anchorMax = new Vector2(Random.Range(0.04f, 0.96f), 0f);
            er.anchoredPosition = new Vector2(0f, Random.Range(-20f, 80f));
            Image img = er.gameObject.AddComponent<Image>();
            img.sprite = UIKit.EmberSprite;
            img.raycastTarget = false;
            live.Add((er, img, Random.Range(40f, 90f), Random.Range(6f, 11f), 0f, Random.Range(0f, 6.28f)));
        }
        for (int i = live.Count - 1; i >= 0; i--)
        {
            var p = live[i];
            p.age += dt;
            if (p.r == null || p.age >= p.life) { if (p.r != null) Destroy(p.r.gameObject); live.RemoveAt(i); continue; }
            p.r.anchoredPosition += new Vector2(Mathf.Sin(p.age * 1.2f + p.sway) * 14f * dt, p.speed * dt);
            float life = p.age / p.life;
            p.img.color = new Color(1f, 1f, 1f, Mathf.Sin(life * Mathf.PI) * 0.75f);
            live[i] = p;
        }
    }
}
