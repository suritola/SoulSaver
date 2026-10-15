using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 메인 메뉴에 튜토리얼 · 설정 버튼, 게임 중 ESC 메뉴에 설정 버튼을 붙임
// 기존 버튼을 복제해서 같은 모양으로 만듦 (씬을 따로 고치지 않음)
public static class MenuExtras
{
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name == "MainMenu") InstallMainMenu();
        else if (scene.name == "GameScene") InstallEsc();
        else if (scene.name == "GameOver") InstallGameOver();
    }

    // 무한 모드에서 쓰러지면 생존 시간 · 보스 처치 수 · 최고 기록
    static void InstallGameOver()
    {
        Canvas canvas = UIKit.HudCanvas();
        // 1.0.4~: 끝까지 깨고 엔딩을 본 뒤에도 이 결과 화면을 거침 — 「사망하셨습니다...」 대신 「<난이도> 클리어!」, 해골은 숨김
        if (RunStats.Cleared)
        {
            GameObject msg = GameObject.Find("GameOverMessage");
            TMP_Text title = msg != null ? msg.GetComponentInChildren<TMP_Text>(true) : null;
            if (title != null)
            {
                title.text = GameMode.Name(GameMode.Current) + " " + Loc.T("클리어!");
                title.color = new Color(0.96f, 0.83f, 0.47f);
            }
            GameObject skull = GameObject.Find("Skull");
            if (skull != null) skull.SetActive(false);
        }
        // 이번 판 결과 (왼쪽) · [R] 다시 시작
        if (canvas != null)
        {
            UIKit.EnsureStyle();
            TMP_Text res = UIKit.Text(canvas.transform, "", 24f, new Color(0.93f, 0.9f, 0.84f), Vector2.zero, new Vector2(480f, 520f), TextAlignmentOptions.TopLeft);
            res.enableAutoSizing = true;
            res.fontSizeMin = 14f;
            res.fontSizeMax = 24f;
            res.text = "<size=120%>" + Loc.T("이번 판 결과") + "</size>\n\n" + RunStats.Summary(false);
            ResultScreen.BuildGoals(canvas.transform);       // 1.1~ 다음 목표 막대 3줄 (예전 「다음 해금까지」 글은 이 위로 옮김)
            RectTransform rr = res.rectTransform;
            rr.anchorMin = rr.anchorMax = rr.pivot = new Vector2(0f, 0.5f);
            rr.anchoredPosition = new Vector2(60f, 0f);
            RunStats.BuildDamagePanel(canvas.transform);      // 오른쪽: 준 피해 · 받은 피해 · 쓰러진 곳
            TMP_Text again = UIKit.Text(canvas.transform, Loc.T("[{RELOAD}] 같은 캐릭터 · 난이도로 바로 다시 시작"), 24f, new Color(0.6f, 0.9f, 1f), Vector2.zero, new Vector2(900f, 40f));
            RectTransform ar = again.rectTransform;
            ar.anchorMin = ar.anchorMax = new Vector2(0.5f, 0f);
            ar.anchoredPosition = new Vector2(0f, 40f);
            canvas.gameObject.AddComponent<QuickRestart>();
            ResultScreen.BuildRetry();       // 1.1~ 「같은 설정으로 다시」 · 「캐릭터 바꿔 다시」 · 「메인 메뉴로」 (1.1.2~ 가운데 세로 한 줄)
            ResultScreen.FitTitle();
        }
        // 이번 판에 얻은 캐릭터 포인트
        if (canvas != null && CharacterData.RunPoints > 0)
        {
            UIKit.EnsureStyle();
            TMP_Text pt = UIKit.Text(canvas.transform, "", 30f, new Color(0.96f, 0.83f, 0.47f), Vector2.zero, new Vector2(1200f, 50f));
            pt.text = Loc.T("획득 포인트 ") + "+" + CharacterData.RunPoints.ToString("N0")
                    + (Point.RunBonus > 0 ? " <size=70%>(" + Loc.T("result.bonus").Replace("{0}", Point.RunBonus.ToString("N0")) + ")</size>" : "")     // 1.1~ 클리어 보너스
                    + "   " + Loc.T("보유 ") + CharacterData.Points.ToString("N0") + " P";
            RectTransform pr = pt.rectTransform;
            pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0f);
            pr.anchoredPosition = new Vector2(0f, 90f);
        }
        if (EndlessMode.LastSeconds < 0f) return;
        if (canvas != null)
        {
            UIKit.EnsureStyle();
            bool daily = DailyChallenge.Active;
            string text = (daily ? Loc.T("일일 도전") + " · " + Loc.T(DailyChallenge.RunRuleName) + "\n" : "")
                        + Loc.T("생존 시간 ") + EndlessMode.Clock(EndlessMode.LastSeconds) + "   " + Loc.T("보스 처치 ") + EndlessMode.LastBosses
                        + "\n" + (daily ? Loc.T("오늘 내 최고 기록") + " " + EndlessMode.Clock(DailyChallenge.RunBest) : Loc.T("최고 기록 ") + EndlessMode.Clock(EndlessMode.BestSeconds));
            TMP_Text t = UIKit.Text(canvas.transform, "", 34f, new Color(1f, 0.8f, 0.45f), Vector2.zero, new Vector2(1200f, daily ? 140f : 110f));
            t.text = text;
            RectTransform r = t.rectTransform;
            // 위 가운데의 해골(255 ~ 405)과 겹치지 않게 결과 버튼 열(아래끝 -268, ResultScreen.BuildRetry) 아래로, 아래 획득 포인트(-475 ~ -425)와도 안 겹치게
            r.anchoredPosition = new Vector2(0f, daily ? -345f : -330f);
            // 스팀 순위: 올린 결과가 오면 마지막 줄 끝에 붙임
            DailyChallenge.RankLine line = t.gameObject.AddComponent<DailyChallenge.RankLine>();
            line.text = t;
            line.baseText = text;
            line.daily = daily;
            // 1.1.5~ 기록 글 오른쪽: 「순위 보기」 → 순위 창 (내 주변 탭, 이번 판 기록을 다 올린 뒤에 받음) · 체험판은 무한 모드가 없음
            if (!Demo.On)
            {
                t.rectTransform.sizeDelta = new Vector2(880f, t.rectTransform.sizeDelta.y);       // 버튼 자리 (오른쪽 결과 판 아래끝 -260 보다 아래)
                Transform root = canvas.transform;
                Button view = UIKit.MakeButton(root, "lb.view", new Vector2(600f, r.anchoredPosition.y), new Vector2(240f, 72f),
                    () => LeaderboardUI.Open(root, daily ? LeaderboardUI.Board.Daily : LeaderboardUI.Board.Endless, SteamLeaderboards.Scope.AroundMe), 26f);
                view.name = "LeaderboardViewButton";
            }
        }
        EndlessMode.LastSeconds = -1f;
    }

    static void InstallMainMenu()
    {
        DailyChallenge.End();
        RunSave.EndContinue();          // 1.0.4~: 이어한 판의 캐릭터 · 난이도를 메뉴 선택에서 거둠 (난이도 버튼을 그리기 전에)
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
        if (CodexSeen.HasNew) codex.AddComponent<NewGlow>();          // 1.2~ 발견형 도감: NEW 탭 · 받을 수집 보상
        GameObject evo = UIKit.CloneButton(start, "SkillEvoButton", "스킬 진화", () => SkillEvoCodexUI.Open(sr.root));
        GameObject settings = UIKit.CloneButton(start, "SettingsButton", "설정", () => SettingsUI.Open(sr.root));
        GameObject daily = UIKit.CloneButton(start, "DailyButton", "일일 도전", () => DailyChallenge.Open(sr.root));
        // 1.0.4~: 잠긴 일일 도전은 흐리게 · 「잠김」 표시 · 여는 방법 툴팁 (창 안의 오늘 규칙 · 순위표는 그대로 볼 수 있음)
        bool dailyLocked = !DailyChallenge.Unlocked;

        // 1.8.5~: 큰 게임 시작 버튼 하나 + 그 아래 아이콘 칸 (왼쪽 캐릭터 그림 x -830 ~ -370 과 안 겹치게 폭 740 안)
        // 2.1~: 스킬 진화 도감이 더해져 4 × 2, 2.1.1~: 일일 도전으로 8칸이 다 참
        sr.anchoredPosition = new Vector2(0f, -10f);
        sr.sizeDelta = new Vector2(560f, 110f);
        AddIcon(start, "menu_play", new Vector2(-205f, 0f), 64f);
        TMP_Text st = start.GetComponentInChildren<TMP_Text>(true);
        if (st != null) { RectTransform tr = st.rectTransform; tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f); tr.sizeDelta = new Vector2(380f, 90f); tr.anchoredPosition = new Vector2(30f, 0f); }
        InstallContinue(start, st);

        (GameObject go, string icon)[] tiles =
        {
            (character, "menu_character"), (skins, "menu_skin"), (codex, "menu_codex"), (evo, "menu_evolution"),
            (daily, "menu_daily"), (tutorial, "menu_tutorial"), (settings, "menu_settings"), (exit, "menu_exit"),
        };
        for (int i = 0; i < tiles.Length; i++)
        {
            if (tiles[i].go == null) continue;
            RectTransform r = (RectTransform)tiles[i].go.transform;
            r.sizeDelta = new Vector2(176f, 122f);
            r.anchoredPosition = new Vector2(-282f + 188f * (i % 4), -160f - 138f * (i / 4));
            Image img = tiles[i].go.GetComponent<Image>();
            bool locked = dailyLocked && tiles[i].go == daily;
            if (img != null) img.color = tiles[i].go == exit ? new Color(0.78f, 0.62f, 0.64f) : locked ? new Color(0.42f, 0.4f, 0.47f) : new Color(0.8f, 0.78f, 0.86f);
            AddIcon(tiles[i].go, tiles[i].icon, new Vector2(0f, 20f), 56f);
            if (locked) MarkLocked(tiles[i].go);
            TMP_Text t = tiles[i].go.GetComponentInChildren<TMP_Text>(true);
            if (t == null) continue;
            RectTransform lr = t.rectTransform;
            lr.anchorMin = lr.anchorMax = new Vector2(0.5f, 0.5f);
            lr.sizeDelta = new Vector2(164f, 40f);
            lr.anchoredPosition = new Vector2(0f, -36f);
            t.enableAutoSizing = true;
            t.fontSizeMin = 14f;
            t.fontSizeMax = 28f;
            if (locked) t.color = new Color(0.62f, 0.6f, 0.6f);
        }

        // 1.0.4~: 튜토리얼을 끝까지 마쳤으면 칸 오른쪽 위에 초록 체크
        // 1.1~ 폰트에 ✔ 글자가 없어 빈 네모(□)로 보였으므로 막대 두 개로 그림
        if (tutorial != null && TutorialRun.Done) CheckMark(tutorial.transform, new Vector2(66f, 40f));

        InstallDifficulty(sr);
        InstallPoints(sr.parent);
        // 오른쪽 위 보유 포인트 아래: 지난 버전까지 패치노트 (2.1.3~)
        Button notes = UIKit.MakeButton(sr.parent, "패치노트", Vector2.zero, new Vector2(220f, 56f), () => PatchNotesUI.OpenHistory(sr.root), 24f);
        RectTransform nr = (RectTransform)notes.transform;
        nr.anchorMin = nr.anchorMax = nr.pivot = new Vector2(1f, 1f);
        nr.anchoredPosition = new Vector2(-30f, -102f);
        notes.name = "PatchNotesButton";
        SoulBrand.InstallMenuButton(sr.parent, sr.root);       // 1.2~ 패치노트 아래: 영혼 각인 (열린 각인 · 해금 조건)
        LeaderboardUI.InstallMenuButton(sr.parent, sr.root);   // 1.1.5~ 영혼 각인 아래: 스팀 순위표 (무한 모드 · 일일 도전, 체험판은 숨김)
        // 1.0.4~: 저장 복구 알림 → 처음 켰을 때 언어 고르기 → 새 버전이면 패치노트, 하나씩 차례로
        Transform root = sr.root;
        SaveNotice(root, () => LanguagePicker.Open(root, () => PatchNotesUI.OpenOnLaunch(root)));
        // 1.0.4~: 튜토리얼 완료 화면 「바로 첫 판 시작」 → 게임 시작 버튼을 대신 누름 (저장된 판이 있으면 원래처럼 한 번 물음)
        if (TutorialRun.ConsumeQuickStart())
        {
            Button sb = start.GetComponent<Button>();
            if (sb != null) sb.onClick.Invoke();
        }
        // 1.1~ 결과 화면 「캐릭터 바꿔 다시」로 왔으면 캐릭터 창을 바로 엶
        if (ResultScreen.ConsumeHeroPick()) CharacterUI.Open(sr.root);
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
        tip.bodyProvider = () => Loc.T(Demo.On ? "정식판에서 만날 수 있습니다" : "어려움을 클리어하면 일일 도전이 열립니다");
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
        UIKit.MakeButton(win, "저장 폴더 열기", new Vector2(-160f, -150f), new Vector2(290f, 76f), () => Application.OpenURL("file:///" + Prefs.FolderPath.Replace('\\', '/')), 26f);
        UIKit.MakeButton(win, "확인", new Vector2(160f, -150f), new Vector2(260f, 76f), () => close(), 28f);
        UIKit.EscCancel(blocker, () => close());
    }

    // 2.1.9: 저장된 판이 있으면 게임 시작 오른쪽에 「이어하기」 (둘이 합쳐 폭 740 안, 왼쪽 캐릭터 그림과 안 겹치게)
    static void InstallContinue(GameObject start, TMP_Text startLabel)
    {
        RunSave.Data saved = RunSave.Peek();
        if (saved == null) return;
        GameObject cont = UIKit.CloneButton(start, "ContinueButton", "이어하기", () => RunSave.Continue());
        RectTransform sr = (RectTransform)start.transform, cr = (RectTransform)cont.transform;
        sr.sizeDelta = cr.sizeDelta = new Vector2(360f, 110f);
        sr.anchoredPosition = new Vector2(-190f, -10f);
        cr.anchoredPosition = new Vector2(190f, -10f);
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
                      + " · " + Chapters.Title(saved.stage) + " · Lv " + saved.level + "</size>";
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

    // 오른쪽 위: 보유 포인트 (스킨 상점에서 쓰면 바로 줄어듦)
    static void InstallPoints(Transform parent)
    {
        RectTransform box = UIKit.Rect("PointsBadge", parent, Vector2.zero, new Vector2(300f, 64f));
        box.anchorMin = box.anchorMax = box.pivot = new Vector2(1f, 1f);
        box.anchoredPosition = new Vector2(-30f, -26f);
        Image bg = box.gameObject.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.11f, 0.72f);
        bg.raycastTarget = true;        // 툴팁을 받으려고
        // 1.0.4~: 판 안의 코인과 같은 동전 그림이라 무엇이 다음 판에도 남는지 헷갈렸음 → 그림 대신 「포인트」 글자 + 마우스를 올리면 설명
        UIKit.Text(box, "포인트", 20f, new Color(0.8f, 0.76f, 0.7f), new Vector2(-88f, 0f), new Vector2(104f, 40f), TextAlignmentOptions.Left);
        TMP_Text t = UIKit.Text(box, "", 30f, new Color(0.96f, 0.83f, 0.47f), new Vector2(26f, 0f), new Vector2(220f, 50f), TextAlignmentOptions.Right);
        box.gameObject.AddComponent<MenuPoints>().text = t;
        TooltipTrigger tip = box.gameObject.AddComponent<TooltipTrigger>();
        tip.titleProvider = () => Loc.T("포인트");
        tip.bodyProvider = () => Loc.T("적을 처치하면 쌓이고 판이 끝나도 남습니다. 메인 메뉴에서 새 영웅과 스킨을 사는 데 씁니다. (판 안에서 떠돌이 상점에 쓰는 코인과는 다릅니다)");
    }

    // 게임 시작 버튼 위: 쉬움 · 보통 · 어려움 · 무한 (잠긴 난이도는 회색, 누를 수 없음)
    static void InstallDifficulty(RectTransform start)
    {
        Transform parent = start.parent;
        // 2.1.9: 이어하기가 있으면 게임 시작 버튼이 왼쪽으로 가므로, 난이도 줄은 가운데(x 0)에 고정
        Vector2 at = new Vector2(0f, start.anchoredPosition.y);
        List<Button> buttons = new List<Button>();
        // 1.0.4~: 이 줄은 고른 난이도 설명 (예전 잠금 안내 자리), 잠금 안내는 게임 시작 버튼 아래 빈 줄(버튼 아래 끝 -65 ~ 아이콘 칸 위 끝 -99)로
        TMP_Text hint = UIKit.Text(parent, "", 20f, new Color(0.8f, 0.76f, 0.7f), at + new Vector2(0f, 62f), new Vector2(940f, 26f));
        TMP_Text lockHint = UIKit.Text(parent, "", 18f, new Color(0.66f, 0.61f, 0.53f), at + new Vector2(0f, -72f), new Vector2(900f, 26f));
        for (int i = 0; i < 4; i++)
        {
            Difficulty d = (Difficulty)i;
            Button b = UIKit.MakeButton(parent, GameMode.Names[i], at + new Vector2(-300f + 200f * i, 118f), new Vector2(184f, 58f), () =>
            {
                GameMode.Current = d;
                HighlightDifficulty(buttons, hint, lockHint);
            }, 26f);
            b.name = "Difficulty_" + d;
            buttons.Add(b);
        }
        // 1.2~ 영혼의 무게: 어려움을 클리어하면 잠금 안내 줄 자리(이때는 늘 비어 있음)에 버튼 · 어려움을 고를 때만 보임
        SoulWeight.InstallMenu(parent, at + new Vector2(0f, -72f));
        HighlightDifficulty(buttons, hint, lockHint);
        hint.gameObject.AddComponent<LocRefresh>().refresh = () => HighlightDifficulty(buttons, hint, lockHint);
    }

    static void HighlightDifficulty(List<Button> buttons, TMP_Text hint, TMP_Text lockHint)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            Difficulty d = (Difficulty)i;
            bool open = GameMode.IsUnlocked(d), on = open && GameMode.Current == d;
            buttons[i].interactable = open;
            Image img = buttons[i].GetComponent<Image>();
            img.color = on ? new Color(0.96f, 0.83f, 0.47f) : open ? new Color(0.6f, 0.58f, 0.65f) : new Color(0.25f, 0.24f, 0.28f, 0.8f);
            buttons[i].transform.localScale = Vector3.one * (on ? 1.08f : 1f);
            TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>();
            label.color = open ? new Color(0.96f, 0.9f, 0.8f) : new Color(0.55f, 0.52f, 0.5f);
        }
        // 고른 난이도가 무엇이 다른지 (1.0.4~, 예전엔 잠금 안내만 · 다 열리면 비었음) + 잠긴 난이도가 있으면 여는 방법
        // 체험판은 쉬움만이라 「쉬움을 클리어하면」 대신 정식판 안내 (1.0.4~)
        string lockText = Demo.On ? "보통 · 어려움 · 무한 모드는 정식판에서 만날 수 있습니다"
                        : !GameMode.IsUnlocked(Difficulty.Normal) ? "쉬움을 클리어하면 보통 · 어려움이 열립니다"
                        : !GameMode.IsUnlocked(Difficulty.Endless) ? "어려움을 클리어하면 무한 모드가 열립니다"
                        : SoulWeight.Unlocked && GameMode.Current != Difficulty.Hard ? "weight.menu.hardonly"     // 1.2~ (어려움이면 이 자리에 무게 버튼)
                        : "";
        hint.text = Loc.T(DifficultyDescs[Mathf.Clamp((int)GameMode.Current, 0, DifficultyDescs.Length - 1)]);
        float pointRate = GameMode.PointRateOf(GameMode.Current);      // 1.1~ 포인트 배율 (보통 ×1.3 · 어려움 ×1.5)
        if (pointRate > 1f) hint.text += " · " + Loc.T("result.diffpoints").Replace("{0}", pointRate.ToString("0.0#", System.Globalization.CultureInfo.InvariantCulture));
        lockHint.text = Loc.T(lockText);
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
        GameObject restart = UIKit.CloneButton(lowest.gameObject, "RestartButton", "다시 시작", esc.onPressRestart);
        GameObject quit = UIKit.CloneButton(lowest.gameObject, "QuitButton", "게임 종료", esc.onPressQuit);
        RectTransform rr = (RectTransform)restart.transform, qr = (RectTransform)quit.transform;
        rr.sizeDelta = qr.sizeDelta = new Vector2(half, H);
        rr.anchoredPosition = new Vector2(-(half + 16f) * 0.5f, rowY);
        qr.anchoredPosition = new Vector2((half + 16f) * 0.5f, rowY);
        Transform hint = esc.escMenu.transform.Find("ResumeHint");
        if (hint != null) ((RectTransform)hint).anchoredPosition = new Vector2(0f, rowY - H * 0.5f - 34f);
    }
}

// ===================================================================== runtime UI helpers
public static class UIKit
{
    // 화면 UI를 붙일 캔버스: 탄약 패널이 있는 HUD 캔버스 > 화면용 최상위 캔버스.
    // UIKit.HudCanvas()는 적 이름표 같은 월드 캔버스를 고를 수 있어서
    // (그 적이 죽으면 붙어 있던 창까지 같이 사라짐) 쓰지 않음
    public static Canvas HudCanvas()
    {
        Canvas best = null;
        foreach (Canvas cv in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!cv.isRootCanvas || cv.renderMode == RenderMode.WorldSpace) continue;
            if (cv.transform.Find("AmmoPanel") != null) return cv;
            if (best == null) best = cv;
        }
        return best != null ? best : Object.FindFirstObjectByType<Canvas>();
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
            if (Font == null) { Font = t.font; FontMaterial = t.fontSharedMaterial; }
        }
        Image img = go.GetComponent<Image>();
        if (img != null && ButtonSprite == null) ButtonSprite = img.sprite;
        return go;
    }

    // 언어가 바뀌면 다시 번역할 글자 (한국어 원문)
    static readonly Dictionary<TMP_Text, string> labels = new Dictionary<TMP_Text, string>();
    static bool hooked;
    public static void Forget(TMP_Text t) => labels.Remove(t);
    public static void Remember(TMP_Text t, string ko)
    {
        labels[t] = ko;
        if (hooked) return;
        hooked = true;
        Loc.Changed += () =>
        {
            List<TMP_Text> dead = new List<TMP_Text>();
            foreach (KeyValuePair<TMP_Text, string> kv in labels)
            {
                if (kv.Key == null) dead.Add(kv.Key);
                else kv.Key.text = Loc.T(kv.Value);
            }
            foreach (TMP_Text d in dead) labels.Remove(d);
        };
    }

    public static void EnsureStyle()
    {
        if (Font != null && ButtonSprite != null) return;
        foreach (TMP_Text t in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (Font == null && t.font != null) { Font = t.font; FontMaterial = t.fontSharedMaterial; }
        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Image img = b.GetComponent<Image>();
            if (ButtonSprite == null && img != null && img.sprite != null) ButtonSprite = img.sprite;
        }
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

        RectTransform win = Rect("Window", br, Vector2.zero, size);
        Image wi = win.gameObject.AddComponent<Image>();
        wi.sprite = ButtonSprite;
        wi.type = Image.Type.Sliced;
        wi.color = new Color(0.55f, 0.5f, 0.6f, 1f);
        RectTransform inner = Rect("Inner", win, Vector2.zero, size - new Vector2(24f, 24f));
        inner.gameObject.AddComponent<Image>().color = new Color(0.09f, 0.07f, 0.12f, 0.97f);
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

    public static Button MakeButton(Transform parent, string ko, Vector2 pos, Vector2 size, UnityAction onClick, float fontSize = 26f)
    {
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
        Text(r, ko, fontSize, new Color(0.96f, 0.9f, 0.8f), Vector2.zero, size - new Vector2(20f, 12f));
        return b;
    }
}

// ===================================================================== settings
// 탭 3개: 일반(언어 · 볼륨) / 화면(모드 · 해상도 · 수직 동기화 · 흔들림 · 번쩍임) / 조작(키 바꾸기)
public static class SettingsUI
{
    static GameObject open;
    public static bool IsOpen => open != null;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Off = new Color(0.6f, 0.58f, 0.65f);

    static readonly string[] TabNames = { "일반", "화면", "조작", "게임" };
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

        UIKit.Text(win, "설정", 52f, Gold, new Vector2(0f, 360f), new Vector2(600f, 70f));

        tabButtons.Clear();
        for (int i = 0; i < TabNames.Length; i++)
        {
            int t = i;
            tabButtons.Add(UIKit.MakeButton(win, TabNames[i], new Vector2(-405f + 270f * i, 272f), new Vector2(250f, 64f), () => ShowTab(t), 28f));
        }
        page = UIKit.Rect("Page", win, new Vector2(0f, -30f), new Vector2(1100f, 560f));

        UIKit.MakeButton(win, "닫기", new Vector2(0f, -364f), new Vector2(260f, 76f), Close, 30f);
        ShowTab(tab);
    }

    static void ShowTab(int t)
    {
        tab = t;
        capturing = -1;
        blocked = -1;
        keyLabels.Clear();
        for (int i = page.childCount - 1; i >= 0; i--) Object.Destroy(page.GetChild(i).gameObject);
        for (int i = 0; i < tabButtons.Count; i++)
        {
            tabButtons[i].GetComponent<Image>().color = i == tab ? Gold : Off;
            tabButtons[i].transform.localScale = Vector3.one * (i == tab ? 1.06f : 1f);
        }
        if (tab == 0) BuildGeneral();
        else if (tab == 1) BuildDisplay();
        else if (tab == 2) BuildControls();
        else BuildGameplay();
    }

    static TMP_Text RowLabel(string ko, float y) =>
        UIKit.Text(page, ko, 30f, Parch, new Vector2(-400f, y), new Vector2(280f, 50f), TextAlignmentOptions.Left);

    // ================================================================= 일반
    static void BuildGeneral()
    {
        RowLabel("언어", 200f);
        List<Button> langButtons = new List<Button>();
        for (int i = 0; i < 4; i++)
        {
            int lang = i;
            Button b = UIKit.MakeButton(page, "", new Vector2(-140f + 185f * i, 200f), new Vector2(170f, 64f), () =>
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
        SliderRow("전체 볼륨", 110f, GameSettings.Volume, v => GameSettings.Volume = v);
        SliderRow("음악", 20f, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
        SliderRow("효과음", -70f, GameSettings.SfxVolume, v => GameSettings.SfxVolume = v, true);
        SensitivityRow(-160f);
        ToggleRow("창이 비활성일 때 소리 끄기", -250f, () => GameSettings.MuteUnfocused, v => GameSettings.MuteUnfocused = v);
    }

    // 마우스 감도 25% ~ 300% (5% 단위, 기본 100%) · 전투 중 조준에만 적용
    static void SensitivityRow(float y)
    {
        RowLabel("마우스 감도", y);
        TMP_Text percent = UIKit.Text(page, "", 30f, Gold, new Vector2(450f, y), new Vector2(120f, 50f));
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
            buttons[i].GetComponent<Image>().color = on ? Gold : Off;
            buttons[i].transform.localScale = Vector3.one * (on ? 1.06f : 1f);
        }
    }

    // 0 ~ 100%
    static float sfxPreviewAt;

    // preview: 효과음 슬라이더는 움직일 때 새 크기로 짧은 소리를 들려줌 (1.1~, 0.15초 간격)
    static void SliderRow(string ko, float y, float value, System.Action<float> set, bool preview = false)
    {
        RowLabel(ko, y);
        TMP_Text percent = UIKit.Text(page, "", 30f, Gold, new Vector2(450f, y), new Vector2(120f, 50f));
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
    static void BuildDisplay()
    {
        RowLabel("화면 모드", 200f);
        Button full = null, windowed = null;
        TMP_Text res = null;
        // SetResolution 은 다음 프레임에 적용되므로 Screen 값이 아니라 고른 모드 · 해상도로 표시 (바로 읽으면 반대로 켜졌음)
        System.Action<bool> highlightMode = isFull =>
        {
            full.GetComponent<Image>().color = isFull ? Gold : Off;
            windowed.GetComponent<Image>().color = isFull ? Off : Gold;
            full.transform.localScale = Vector3.one * (isFull ? 1.06f : 1f);
            windowed.transform.localScale = Vector3.one * (isFull ? 1f : 1.06f);
        };
        full = UIKit.MakeButton(page, "전체 화면", new Vector2(-40f, 200f), new Vector2(250f, 64f), () =>
        {
            int w = Display.main.systemWidth, h = Display.main.systemHeight;
            Screen.SetResolution(w, h, FullScreenMode.FullScreenWindow);
            res.text = w + " × " + h;
            Deselect();
            highlightMode(true);
        }, 26f);
        windowed = UIKit.MakeButton(page, "창 모드", new Vector2(230f, 200f), new Vector2(250f, 64f), () =>
        {
            // 화면을 꽉 채우는 크기면 창 테두리가 화면 밖으로 나가므로 한 단계 작은 해상도로
            Vector2Int r = new Vector2Int(Screen.width, Screen.height);
            if (r.x >= Display.main.systemWidth || r.y >= Display.main.systemHeight)
            {
                List<Vector2Int> list = Resolutions();
                for (int i = list.Count - 1; i >= 0; i--)
                    if (list[i].x < Display.main.systemWidth && list[i].y < Display.main.systemHeight) { r = list[i]; break; }
            }
            Screen.SetResolution(r.x, r.y, FullScreenMode.Windowed);
            res.text = r.x + " × " + r.y;
            Deselect();
            highlightMode(false);
        }, 26f);
        highlightMode(Screen.fullScreenMode != FullScreenMode.Windowed);

        // 해상도: < 1920 × 1080 >
        // 1.1.2~ 「안내 메시지 크기」 한 줄을 더 넣으려고 줄 간격 90 → 75
        RowLabel("해상도", 125f);
        res = UIKit.Text(page, "", 30f, Gold, new Vector2(95f, 125f), new Vector2(320f, 50f));
        res.text = Screen.width + " × " + Screen.height;
        System.Action<int> step = d =>
        {
            List<Vector2Int> list = Resolutions();
            // 1.1~ 창 모드에서는 모니터보다 작은 해상도만 (창 테두리 · 제목 표시줄이 화면 밖으로 나가지 않게, 「창 모드」 버튼과 같은 조건)
            if (Screen.fullScreenMode == FullScreenMode.Windowed)
            {
                List<Vector2Int> fit = list.FindAll(v => v.x < Display.main.systemWidth && v.y < Display.main.systemHeight);
                if (fit.Count > 0) list = fit;
            }
            int at = list.FindIndex(v => v.x == Screen.width && v.y == Screen.height);
            if (at < 0) at = list.Count - 1;
            at = Mathf.Clamp(at + d, 0, list.Count - 1);
            Screen.SetResolution(list[at].x, list[at].y, Screen.fullScreenMode);
            res.text = list[at].x + " × " + list[at].y;     // 실제 적용은 다음 프레임
            Deselect();
        };
        UIKit.MakeButton(page, "<", new Vector2(-110f, 125f), new Vector2(80f, 64f), () => step(-1), 30f);
        UIKit.MakeButton(page, ">", new Vector2(300f, 125f), new Vector2(80f, 64f), () => step(1), 30f);

        // 2.1.9: UI 크기 (모든 화면 글자 · 버튼 · HUD), 한 줄 더 넣으려고 줄 간격 90
        CycleRow("UI 크기", 50f, GameSettings.UiScaleNames, () => GameSettings.UiScaleIndex, v => GameSettings.UiScaleIndex = v);
        CycleRow("안내 메시지 크기", -25f, Notice.SizeNames, () => GameSettings.NoticeSize, v => GameSettings.NoticeSize = v,
                 "게임 중 뜨는 큰 알림 · 오른쪽 위 소식 · 도움말 글자의 크기입니다.");
        ToggleRow("수직 동기화", -100f, () => GameSettings.VSync, v => GameSettings.VSync = v);
        ToggleRow("화면 흔들림", -175f, () => GameSettings.ScreenShake, v => GameSettings.ScreenShake = v);
        ToggleRow("번쩍임 효과", -250f, () => GameSettings.Flashes, v => GameSettings.Flashes = v);
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
        ToggleRow("도움말 안내", 200f, () => GameSettings.Hints, v => GameSettings.Hints = v,
                  "처음 보는 기능마다 짧은 도움말을 한 번씩 띄웁니다.");
        // 1.0.4~: 본 도움말을 처음부터 다시 볼 수 있게 (켜기 · 끄기 옆)
        Button again = null;
        again = UIKit.MakeButton(page, "도움말 다시 보기", new Vector2(370f, 200f), new Vector2(230f, 64f), () =>
        {
            Hints.ResetAll();
            Deselect();
            TMP_Text t = again.GetComponentInChildren<TMP_Text>();
            if (t != null) { t.text = Loc.T("처음부터 다시 보입니다"); UIKit.Remember(t, "처음부터 다시 보입니다"); }
        }, 22f);
        TMP_Text againLabel = again.GetComponentInChildren<TMP_Text>();
        if (againLabel != null) { againLabel.enableAutoSizing = true; againLabel.fontSizeMin = 14f; againLabel.fontSizeMax = 22f; }
        CycleRow("피해 숫자", 125f, GameSettings.DamageNumberNames, () => GameSettings.DamageNumbers, v => GameSettings.DamageNumbers = v,
                 "적에게 준 피해를 숫자로 띄웁니다. 끄면 화면이 깔끔해집니다.");
        CycleRow("내 이펙트 불투명도", 50f, GameSettings.MyFxAlphaNames, () => GameSettings.MyFxAlphaIndex, v => GameSettings.MyFxAlphaIndex = v,
                 "내 공격 · 스킬 · 필살기 이펙트의 진하기입니다. 낮추면 캐릭터와 적의 공격이 잘 보입니다. (적 공격은 그대로)");
        ToggleRow("색각 이상 모드", -25f, () => GameSettings.ColorBlind, v => GameSettings.ColorBlind = v,
                  "경고 표시를 빨강 · 초록 대신 주황 · 파랑 · 자홍으로 그립니다.");
        ToggleRow("게임 커서", -100f, () => GameSettings.GameCursor, v => GameSettings.GameCursor = v,
                  "게임 전용 화살표와 조준점을 씁니다. 끄면 윈도우 기본 커서라 반응이 더 빠릅니다.");
        ToggleRow("레벨업 바로 열기", -175f, () => GameSettings.AutoLevelUp, v => GameSettings.AutoLevelUp = v,
                  "켜면 레벨이 오르자마자 카드 창이 열립니다. 끄면 포인트가 쌓이고 [{INTERACT}]로 원할 때 고릅니다.");
        // 1.2~ 발견형 도감의 스포일러 허용 (줄 간격 80 → 75 로 한 줄 더)
        ToggleRow("도감 모두 보기", -250f, () => CodexSeen.ShowAll, v => CodexSeen.ShowAll = v,
                  "켜면 도감의 진화 · 유물 · 각인 · 비밀 탭에서 아직 못 얻은 것도 모두 보입니다 (스포일러). 수집률과 보상에는 들어가지 않습니다.");
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

    // 누를 때마다 다음 값으로
    static void CycleRow(string ko, float y, string[] names, System.Func<int> get, System.Action<int> set, string tip = null)
    {
        RowTip(RowLabel(ko, y), ko, tip);
        TMP_Text label = null;
        Button b = null;
        System.Action refresh = () =>
        {
            string text = names[Mathf.Clamp(get(), 0, names.Length - 1)];
            label.text = Loc.T(text);
            UIKit.Remember(label, text);
            b.GetComponent<Image>().color = Gold;
        };
        b = UIKit.MakeButton(page, "", new Vector2(95f, y), new Vector2(250f, 64f), () =>
        {
            set((get() + 1) % names.Length);
            Deselect();
            refresh();
        }, 26f);
        label = b.GetComponentInChildren<TMP_Text>();
        refresh();
    }

    static void ToggleRow(string ko, float y, System.Func<bool> get, System.Action<bool> set, string tip = null)
    {
        RowTip(RowLabel(ko, y), ko, tip);
        Button b = null;
        TMP_Text label = null;
        System.Action refresh = () =>
        {
            bool on = get();
            string text = on ? "켜짐" : "꺼짐";
            label.text = Loc.T(text);
            UIKit.Remember(label, text);
            b.GetComponent<Image>().color = on ? Gold : Off;
        };
        b = UIKit.MakeButton(page, "", new Vector2(95f, y), new Vector2(250f, 64f), () =>
        {
            set(!get());
            Deselect();
            refresh();
        }, 26f);
        label = b.GetComponentInChildren<TMP_Text>();
        refresh();
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
        UIKit.Text(page, "게임패드", 24f, Gold, new Vector2(-470f, -172f), new Vector2(160f, 40f), TextAlignmentOptions.Left);
        UIKit.Text(page, PadLayoutKo, 19f, Parch, new Vector2(80f, -173f), new Vector2(940f, 98f), TextAlignmentOptions.Left);
        UIKit.Text(page, "버튼을 누른 뒤 바꿀 키나 마우스 버튼을 누르세요 (ESC: 취소)", 22f, Off, new Vector2(-160f, -255f), new Vector2(760f, 44f), TextAlignmentOptions.Left);
        UIKit.MakeButton(page, "기본값으로", new Vector2(390f, -255f), new Vector2(260f, 60f), () =>
        {
            capturing = -1;
            KeyBindings.ResetAll();
            Deselect();
            RefreshKeys();
        }, 24f);
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
            keyLabels[j].color = waiting ? Gold : denied ? new Color(1f, 0.45f, 0.4f) : new Color(0.96f, 0.9f, 0.8f);
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
        if (GameInput.EscDown)
        {
            EscHandledFrame = Time.frameCount;
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

        RectTransform bg = UIKit.Rect("Background", root, Vector2.zero, Vector2.zero);
        bg.anchorMin = new Vector2(0f, 0.3f); bg.anchorMax = new Vector2(1f, 0.7f);
        bg.offsetMin = bg.offsetMax = Vector2.zero;
        bg.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.22f, 0.3f);

        RectTransform area = UIKit.Rect("Fill Area", root, Vector2.zero, Vector2.zero);
        area.anchorMin = new Vector2(0f, 0.3f); area.anchorMax = new Vector2(1f, 0.7f);
        area.offsetMin = area.offsetMax = Vector2.zero;
        RectTransform fill = UIKit.Rect("Fill", area, Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.75f, 0.3f);

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
        TMP_Text title = UIKit.Text(win, "", 44f, new Color(0.96f, 0.83f, 0.47f), new Vector2(0f, 145f), new Vector2(920f, 64f));
        title.text = Loc.T("언어를 고르세요") + (Loc.Current != Loc.Lang.English ? "  <size=60%><color=#A89C86>Language</color></size>" : "");
        UIKit.Text(win, "설정 → 일반에서 언제든 바꿀 수 있습니다.", 22f, new Color(0.66f, 0.61f, 0.53f), new Vector2(0f, -150f), new Vector2(900f, 36f));
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
            b.GetComponent<Image>().color = on ? new Color(0.96f, 0.83f, 0.47f) : new Color(0.6f, 0.58f, 0.65f);
            b.transform.localScale = Vector3.one * (on ? 1.06f : 1f);
        }
        // ESC 는 미리 골라 둔 언어 그대로
        UIKit.EscCancel(open, () => pick(Loc.Current));
    }
}

// 설정 창에 붙어서 SettingsUI.Tick을 불러 줌 (멈춘 화면에서도 Update는 돔)
public class SettingsInput : MonoBehaviour
{
    void Update() => SettingsUI.Tick();
}
