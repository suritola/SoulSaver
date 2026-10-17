using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.2~ 난이도 사다리 재배치 (설계서 3-3 · 4-2): 무한 이정표 · 영웅 해금 대체 루트 · 「받기」 창
//   사다리 자체(무한 = 보통 클리어 · 일일 = 쉬움 클리어)는 GameMode.OnCleared/Load · DailyChallenge.Unlocked
//   기존 플레이어 보호: 조건을 낮추면 소급 해금하고 한 번 알림 · 새 보상은 이미 이룬 기록에도 주되 「받기」로 · 저장 키는 더하기만

// ===================================================================== 무한 이정표 (첫 달성 한 번 · 합 18,000P)
// 5분 1,000 · 10분 2,000 · 15분(최종 국면) 3,000 · 20분 5,000 · 한 판 보스 5마리 2,000 · 개발자 처치 5,000
// 달성 순간 Point.Bonus(배율 없음) + 큰 알림. 일일 도전 · 체험판 · 자동 플레이에는 없음
// 저장: endless.milestone.<code> — 1 = 받음 · 2 = 이 패치 전 기록으로 이미 넘겨 「받기」를 기다림 (처음 켤 때 한 번 정함, Snapshot)
public static class EndlessMilestones
{
    public const int Min5 = 0, Min10 = 1, Min15 = 2, Min20 = 3, Bosses5 = 4, Developer = 5;

    public struct Def
    {
        public string code;
        public float seconds;       // 시간 이정표 (아니면 0)
        public int points;
    }

    public static readonly Def[] All =
    {
        new Def { code = "m5",    seconds = 300f,  points = 1000 },
        new Def { code = "m10",   seconds = 600f,  points = 2000 },
        new Def { code = "m15",   seconds = 900f,  points = 3000 },
        new Def { code = "m20",   seconds = 1200f, points = 5000 },
        new Def { code = "boss5", seconds = 0f,    points = 2000 },
        new Def { code = "dev",   seconds = 0f,    points = 5000 },
    };

    const string Key = "endless.milestone.";
    const string SnapshotKey = "endless.milestone.snapshot";
    const int Got = 1, Waiting = 2;

    static int State(int i) => Prefs.GetInt(Key + All[i].code, 0);
    public static bool Received(int i) => State(i) == Got;

    static bool Allowed => !Demo.On && !GameInput.Auto && !GameInput.TrailerRunning && !Application.isBatchMode && !TutorialRun.Active;

    // 이번 판에 처음 넘긴 이정표 (판 기록 milestones 칸 · RunLog 가 판마다 비움)
    static readonly List<string> run = new List<string>();
    public static string RunCodes => string.Join("|", run);
    public static void ResetRun() { run.Clear(); System.Array.Clear(checkedRun, 0, checkedRun.Length); }
    // 이번 판에 이미 확인한 시간 이정표 (EndlessMode.Update 가 프레임마다 부르므로 넘긴 뒤로는 저장을 다시 읽지 않음)
    static readonly bool[] checkedRun = new bool[All.Length];

    public static string Label(int i)
    {
        Def d = All[i];
        if (d.seconds > 0f) return Loc.T("milestone.min").Replace("{0}", Mathf.RoundToInt(d.seconds / 60f).ToString());
        return Loc.T(i == Bosses5 ? "milestone.boss5" : "milestone.dev");
    }

    // EndlessMode.Update (일일 도전이 아닐 때)
    public static void CheckTime(float s)
    {
        for (int i = Min5; i <= Min20; i++)
            if (!checkedRun[i] && s >= All[i].seconds) { checkedRun[i] = true; Reach(i); }
    }

    public static void Reach(int i)
    {
        if (i < 0 || i >= All.Length || !Allowed || Received(i)) return;
        Prefs.SetInt(Key + All[i].code, Got);          // 「받기」를 기다리던 것이어도 지금 받음 (두 번 주지 않음)
        Prefs.Save();
        run.Add(All[i].code);
        Point.Bonus(All[i].points);
        Notice.Big(Loc.T("milestone.reached"), Label(i) + "  <color=#F5D478>+" + All[i].points.ToString("N0") + "P</color>", NoticeIcon.Star, 3.5f, NoticeLevel.Important);
        Hostile.Play("chime", 0.8f, 1.2f);
    }

    // 이 패치 전 기록으로 이미 넘긴 이정표 (최고 생존 시간 · 보스 5마리 조건 · 무한 모드 클리어)
    static bool History(int i)
    {
        if (All[i].seconds > 0f) return EndlessMode.BestSeconds >= All[i].seconds;
        if (i == Bosses5) return UnlockRoutes.MainRouteMet(CharUnlock.EndlessBosses5);
        return EndlessMode.EverCleared;                 // 클리어 = 개발자 · 엄마를 모두 쓰러뜨림
    }

    // 처음 켤 때 한 번: 이미 넘긴 이정표를 「받기」 대기로 (그 뒤 판에서 넘긴 것은 그 자리에서 받으므로 섞이지 않음)
    public static void Snapshot()
    {
        if (Demo.On || Prefs.GetInt(SnapshotKey, 0) == 1) return;
        for (int i = 0; i < All.Length; i++)
            if (State(i) == 0 && History(i)) Prefs.SetInt(Key + All[i].code, Waiting);
        Prefs.SetInt(SnapshotKey, 1);
        Prefs.Save();
    }

    // 받기 창 (ClaimRewards)
    public static IEnumerable<ClaimRewards.Claim> Claims()
    {
        for (int i = 0; i < All.Length; i++)
        {
            if (State(i) != Waiting) continue;
            int k = i;
            yield return new ClaimRewards.Claim
            {
                id = "milestone." + All[k].code,
                label = Label(k),
                points = All[k].points,
                give = () => { Prefs.SetInt(Key + All[k].code, Got); CharacterData.AddPoints(All[k].points); },
            };
        }
    }

    // 결과 화면 (무한 모드): 아직 받지 않은 다음 시간 이정표 (없으면 -1)
    public static int NextTime()
    {
        for (int i = Min5; i <= Min20; i++) if (State(i) == 0) return i;
        return -1;
    }
}

// ===================================================================== 영웅 해금 대체 루트
// 무한 모드 조건 셋에 이야기 모드 길 하나씩 (같은 플래그 char.cond.<n> 을 켬 · 어느 길로 이뤘는지는 char.alt.<n>)
//   지도 제작자 (무한 10분)     또는 이야기 모드 보물 상자 누적 15개 열기          stats.chestsOpened
//   시계공 (무한 보스 5)        또는 장 보스 4종을 각각 결계 전에 쓰러뜨리기(누적)  stats.quickKill.<보스 종류 0~3>
//   유령 (무한 클리어)          또는 어려움에서 영혼의 무게 2 클리어              weight.best.* (기록이 있어 소급)
// 상자 · 결계 전 처치는 지금까지의 기록이 없어 앞으로만 셈 (기존 조건으로는 그대로 열림)
public static class UnlockRoutes
{
    const string ChestsKey = "stats.chestsOpened";
    const string QuickKey = "stats.quickKill.";
    const string AltKey = "char.alt.";
    const string AltNoticeKey = "char.altNotice.";      // + 조건 번호 → 1 = 소급으로 열려 메인 메뉴에서 알릴 차례
    public const int ChestGoal = 15, QuickGoal = 4, WeightGoal = 2;

    public static int ChestsOpened => Prefs.GetInt(ChestsKey, 0);
    public static int QuickKills
    {
        get
        {
            int n = 0;
            for (int k = 0; k < QuickGoal; k++) if (Prefs.GetInt(QuickKey + k, 0) == 1) n++;
            return n;
        }
    }
    public static int BestWeight
    {
        get
        {
            int best = 0;
            for (int i = 0; i < CharacterData.All.Length; i++) best = Mathf.Max(best, SoulWeight.Best((CharacterId)i));
            return best;
        }
    }

    public static bool HasAlt(CharUnlock c) => c == CharUnlock.Endless10 || c == CharUnlock.EndlessBosses5 || c == CharUnlock.EndlessClear;
    public static bool ByAlt(CharUnlock c) => Prefs.GetInt(AltKey + (int)c, 0) == 1;
    // 원래 길(무한 모드)로 이뤘는지 — 대체 루트로 켠 플래그는 빼고 (무한 이정표 소급 · 도감 비밀처럼 「무한 모드에서 한 일」만 보는 곳)
    public static bool MainRouteMet(CharUnlock c) => CharacterData.ConditionMet(c) && !ByAlt(c);

    static bool Counting => !GameMode.IsEndless && !DailyChallenge.Active && !TutorialRun.Active && !Demo.On
                            && !GameInput.Auto && !GameInput.TrailerRunning && !Application.isBatchMode;

    // TreasureChest.OpenChest (보상 상자만 · 저주받은 상자 빼고)
    public static void OnChestOpened()
    {
        if (!Counting) return;
        int n = ChestsOpened + 1;
        Prefs.SetInt(ChestsKey, n);
        if (n >= ChestGoal) Achieve(CharUnlock.Endless10);
        else Prefs.Save();
    }

    // bosss.Die (장 보스의 마지막 한 마리): 그 보스전에 결계가 펼쳐지기 전이었으면
    public static void OnBossDown(int kind)
    {
        if (!Counting || kind < 0 || kind >= QuickGoal || BossUltimate.UsedThisFight(kind)) return;
        if (Prefs.GetInt(QuickKey + kind, 0) == 1) return;
        Prefs.SetInt(QuickKey + kind, 1);
        if (QuickKills >= QuickGoal) Achieve(CharUnlock.EndlessBosses5);
        else
        {
            Prefs.Save();
            if (!CharacterData.ConditionMet(CharUnlock.EndlessBosses5))         // 이미 (무한 모드로) 연 조건이면 기록만 조용히
                Notice.Toast(NoticeIcon.Hourglass, Loc.T("route.quick.toast").Replace("{0}", QuickKills + "/" + QuickGoal).Replace("{1}", Loc.T(CharacterData.Def(CharacterId.Clockmaker).name)), 4f);
        }
    }

    // StageManager: 영혼의 무게 기록을 적은 바로 뒤
    public static void OnWeightCleared()
    {
        if (BestWeight >= WeightGoal) Achieve(CharUnlock.EndlessClear);
    }

    // ContractBoard.Refresh: 보드의 「열다섯 상자」 · 「네 왕」 · 「두 번째 무게」 칸을 채웠을 때 (같은 기록이라 보통은 여기서 이미 열려 있음)
    //   판 안이면 바로 알림 · 메인 메뉴(옛 기록으로 처음 채움)면 조용히 열고 받기 창에서 한 번 알림 (Retro 와 같음)
    public static void FromBoard(CharUnlock c, bool inRun)
    {
        if (!HasAlt(c) || CharacterData.ConditionMet(c)) return;
        if (inRun) { Achieve(c); return; }
        Prefs.SetInt(AltKey + (int)c, 1);
        Prefs.SetInt("char.cond." + (int)c, 1);
        Prefs.SetInt(AltNoticeKey + (int)c, 1);
        Prefs.Save();
    }

    static void Achieve(CharUnlock c)
    {
        if (CharacterData.ConditionMet(c)) { Prefs.Save(); return; }
        Prefs.SetInt(AltKey + (int)c, 1);
        CharacterData.Achieve(c, true);
    }

    // 처음 켤 때: 기록이 이미 있는 대체 루트(무게 2)는 소급으로 열고 메인 메뉴에서 한 번 알림
    public static void Retro()
    {
        if (Demo.On || CharacterData.ConditionMet(CharUnlock.EndlessClear) || BestWeight < WeightGoal) return;
        Prefs.SetInt(AltKey + (int)CharUnlock.EndlessClear, 1);
        Prefs.SetInt("char.cond." + (int)CharUnlock.EndlessClear, 1);
        Prefs.SetInt(AltNoticeKey + (int)CharUnlock.EndlessClear, 1);
        Prefs.Save();
    }

    // 대체 루트 글 (progress: 진행 수치 덧붙임)
    public static string AltText(CharUnlock c, bool progress)
    {
        switch (c)
        {
            case CharUnlock.Endless10: return Loc.T("route.chests") + (progress ? " (" + Mathf.Min(ChestsOpened, ChestGoal) + "/" + ChestGoal + ")" : "");
            case CharUnlock.EndlessBosses5: return Loc.T("route.quick") + (progress ? " (" + QuickKills + "/" + QuickGoal + ")" : "");
            case CharUnlock.EndlessClear: return Loc.T("route.weight") + (progress ? " (" + Mathf.Min(BestWeight, WeightGoal) + "/" + WeightGoal + ")" : "");
        }
        return "";
    }

    // 결과 화면 「다음 목표」 영웅 줄 오른쪽의 짧은 진행 (예: 「보물 상자 3/15」)
    public static string AltShort(CharUnlock c) => c switch
    {
        CharUnlock.Endless10 => Loc.T("route.chests.short").Replace("{0}", Mathf.Min(ChestsOpened, ChestGoal).ToString()).Replace("{1}", ChestGoal.ToString()),
        CharUnlock.EndlessBosses5 => Loc.T("route.quick.short").Replace("{0}", QuickKills.ToString()).Replace("{1}", QuickGoal.ToString()),
        CharUnlock.EndlessClear => Loc.T("route.weight.short").Replace("{0}", Mathf.Min(BestWeight, WeightGoal).ToString()).Replace("{1}", WeightGoal.ToString()),
        _ => "",
    };

    // 결과 화면 「다음 목표」 영웅 줄 (0 ~ 1)
    public static float AltProgress(CharUnlock c) => c switch
    {
        CharUnlock.Endless10 => (float)ChestsOpened / ChestGoal,
        CharUnlock.EndlessBosses5 => (float)QuickKills / QuickGoal,
        CharUnlock.EndlessClear => (float)BestWeight / WeightGoal,
        _ => 0f,
    };

    // 받기 창의 알림 (소급으로 열린 영웅)
    public static IEnumerable<ClaimRewards.Note> Notes()
    {
        for (int i = 0; i < CharacterData.All.Length; i++)
        {
            CharacterDef d = CharacterData.Def((CharacterId)i);
            if (!HasAlt(d.condition) || Prefs.GetInt(AltNoticeKey + (int)d.condition, 0) != 1) continue;
            CharUnlock c = d.condition;
            yield return new ClaimRewards.Note
            {
                text = Loc.T("route.retro").Replace("{0}", Loc.T(d.name)).Replace("{1}", AltText(c, false)).Replace("{2}", d.price.ToString("N0")),
                ack = () => Prefs.DeleteKey(AltNoticeKey + (int)c),
            };
        }
    }
}

// ===================================================================== 「받기」 창 (메인 메뉴, 받을 것 · 알릴 것이 있을 때 한 번 뜸)
// 다른 갈래(영혼 계약 보드 등)도 Register 로 받을 것을 더할 수 있음 · 받지 않고 닫으면 다음에 메인 메뉴에 올 때 다시 뜸
// 알림(Note)은 한 번 보여 주면 지움
public static class ClaimRewards
{
    public class Claim
    {
        public string id, label;
        public int points;
        public System.Action give;      // 받음으로 적고 포인트를 줌 (창이 Prefs.Save 함)
    }

    public class Note
    {
        public string text;
        public System.Action ack;
    }

    static readonly List<System.Func<IEnumerable<Claim>>> sources = new List<System.Func<IEnumerable<Claim>>> { EndlessMilestones.Claims, ContractBoard.Claims };      // 1.2~ 계약 보드: 옛 기록으로 채운 칸 · 받지 않은 칸
    static readonly List<System.Func<IEnumerable<Note>>> notes = new List<System.Func<IEnumerable<Note>>> { LadderNotes, UnlockRoutes.Notes };

    public static void Register(System.Func<IEnumerable<Claim>> source) { if (source != null && !sources.Contains(source)) sources.Add(source); }
    public static void RegisterNotes(System.Func<IEnumerable<Note>> source) { if (source != null && !notes.Contains(source)) notes.Add(source); }

    static IEnumerable<Note> LadderNotes()
    {
        if (!GameMode.UnlockNoticePending) yield break;
        yield return new Note { text = Loc.T("ladder.retro.endless"), ack = () => Prefs.DeleteKey(GameMode.UnlockNoticeKey) };
    }

    static List<Claim> PendingClaims()
    {
        List<Claim> all = new List<Claim>();
        foreach (System.Func<IEnumerable<Claim>> s in sources) foreach (Claim c in s()) if (c != null) all.Add(c);
        return all;
    }

    static List<Note> PendingNotes()
    {
        List<Note> all = new List<Note>();
        foreach (System.Func<IEnumerable<Note>> s in notes) foreach (Note n in s()) if (n != null) all.Add(n);
        return all;
    }

    static bool firstMenu = true;

    // MenuExtras.InstallMainMenu: 처음 켤 때 소급 판정 → 저장 알림 · 언어 · 패치노트 · 캐릭터 창이 닫힌 뒤 받기 창
    public static void InstallMenu(Transform root)
    {
        if (Demo.On || GameInput.Auto || Application.isBatchMode) return;
        if (firstMenu)
        {
            firstMenu = false;
            EndlessMilestones.Snapshot();
            UnlockRoutes.Retro();
        }
        new GameObject("ClaimRewardsRunner").AddComponent<Runner>().root = root;
    }

    class Runner : MonoBehaviour
    {
        public Transform root;
        static readonly string[] Blockers = { "SaveNotice", "LanguagePicker", "PatchNotes", "PatchNotesHistory", "CharacterPanel" };

        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            while (true)
            {
                bool blocked = false;
                foreach (string b in Blockers) if (GameObject.Find(b) != null) { blocked = true; break; }
                if (!blocked) break;
                yield return new WaitForSecondsRealtime(0.25f);
            }
            if (root != null) Open(root);
            Destroy(gameObject);
        }
    }

    static GameObject window;
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f), Parch = new Color(0.93f, 0.9f, 0.84f), Dim = new Color(0.66f, 0.61f, 0.53f);
    const int MaxRows = 7;

    // 받을 것 · 알릴 것이 없으면 열지 않음
    public static void Open(Transform root)
    {
        if (window != null || root == null) return;
        taken.Clear();
        List<Claim> claims = PendingClaims();
        List<Note> pendingNotes = PendingNotes();
        if (claims.Count == 0 && pendingNotes.Count == 0) return;

        const float W = 1040f, NoteH = 84f, RowH = 76f;
        int rows = Mathf.Min(MaxRows, claims.Count);
        bool more = claims.Count > rows;
        float h = 190f + NoteH * pendingNotes.Count + (claims.Count > 0 ? 60f + RowH * rows + (more ? 34f : 0f) : 0f) + 110f;
        RectTransform win = UIKit.Modal(root, "ClaimRewards", new Vector2(W, h), out window);
        WindowFit.Attach(win, new Vector2(W + 20f, h + 20f));      // 알림 · 받을 것이 몰리면 1000 을 넘음 → UI 크기 110 · 120% 에서 화면 밖으로 나가지 않게
        GameObject blocker = window;
        float y = h * 0.5f - 60f;

        UIKit.Text(win, claims.Count > 0 ? "claim.title" : "claim.title.notice", 44f, Gold, new Vector2(0f, y), new Vector2(W - 60f, 60f));
        y -= 66f;
        foreach (Note n in pendingNotes)
        {
            TMP_Text t = UIKit.Text(win, "", 23f, new Color(0.6f, 1f, 0.7f), new Vector2(0f, y - NoteH * 0.5f), new Vector2(W - 80f, NoteH - 8f));
            t.text = n.text;
            t.enableWordWrapping = true;
            t.fontSizeMin = 16f;
            y -= NoteH;
        }

        List<(Claim c, Button b, TMP_Text label)> made = new List<(Claim, Button, TMP_Text)>();
        TMP_Text status = null;
        if (claims.Count > 0)
        {
            TMP_Text sub = UIKit.Text(win, "claim.sub", 21f, Dim, new Vector2(0f, y - 10f), new Vector2(W - 80f, 34f));
            sub.enableWordWrapping = true;
            y -= 60f;
            for (int i = 0; i < rows; i++)
            {
                Claim c = claims[i];
                RectTransform row = UIKit.Rect("Row", win, new Vector2(0f, y - RowH * 0.5f), new Vector2(W - 80f, RowH - 10f));
                row.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.13f, 0.2f, 1f);
                TMP_Text label = UIKit.Text(row, "", 22f, Parch, new Vector2(-160f, 0f), new Vector2(600f, RowH - 16f), TextAlignmentOptions.Left);
                label.text = c.label;
                label.enableWordWrapping = true;
                label.fontSizeMin = 14f;
                TMP_Text pts = UIKit.Text(row, "", 26f, Gold, new Vector2(235f, 0f), new Vector2(170f, 40f), TextAlignmentOptions.Right);
                pts.text = "+" + c.points.ToString("N0") + " P";
                Button b = null;
                b = UIKit.MakeButton(row, "claim.take", new Vector2(400f, 0f), new Vector2(150f, 56f), () => Take(c, b, label, status), 22f);
                made.Add((c, b, label));
                y -= RowH;
            }
            if (more)
            {
                TMP_Text m = UIKit.Text(win, "", 19f, Dim, new Vector2(0f, y - 14f), new Vector2(W - 80f, 30f));
                m.text = Loc.T("claim.more").Replace("{0}", (claims.Count - rows).ToString());
            }
            status = UIKit.Text(win, "", 22f, new Color(0.6f, 1f, 0.6f), new Vector2(0f, -h * 0.5f + 118f), new Vector2(W - 80f, 30f));
        }

        bool closed = false;
        System.Action close = () =>
        {
            if (closed) return;
            closed = true;
            foreach (Note n in pendingNotes) n.ack?.Invoke();
            Prefs.Save();
            if (blocker != null) Object.Destroy(blocker);
            window = null;
        };

        Button primary;
        if (claims.Count > 0)
        {
            primary = UIKit.MakeButton(win, "claim.all", new Vector2(-150f, -h * 0.5f + 60f), new Vector2(280f, 70f), () =>
            {
                int total = 0;
                foreach (Claim c in claims) total += Give(c);
                foreach (var m in made) MarkTaken(m.b, m.label);
                if (total > 0)
                {
                    Hostile.Play("chime", 0.9f, 1.1f);
                    if (status != null) status.text = Loc.T("claim.got").Replace("{0}", total.ToString("N0"));
                }
            }, 26f);
            primary.GetComponent<Image>().color = new Color(1f, 0.88f, 0.55f);
            UIKit.MakeButton(win, "닫기", new Vector2(150f, -h * 0.5f + 60f), new Vector2(240f, 70f), () => close(), 26f);
        }
        else primary = UIKit.MakeButton(win, "확인", new Vector2(0f, -h * 0.5f + 60f), new Vector2(260f, 70f), () => close(), 28f);
        primary.gameObject.AddComponent<ResultPadFocus>();          // 패드 가상 커서를 기본 버튼 위로
        UIKit.EscCancel(blocker, () => close());
    }

    static readonly HashSet<string> taken = new HashSet<string>();

    // 받음으로 적고 준 포인트 (이미 받았으면 0)
    static int Give(Claim c)
    {
        if (c == null || taken.Contains(c.id)) return 0;
        taken.Add(c.id);
        c.give?.Invoke();
        Prefs.Save();
        return c.points;
    }

    static void Take(Claim c, Button b, TMP_Text label, TMP_Text status)
    {
        int got = Give(c);
        MarkTaken(b, label);
        if (got <= 0) return;
        Hostile.Play("chime", 0.9f, 1.1f);
        if (status != null) status.text = Loc.T("claim.got").Replace("{0}", got.ToString("N0"));
    }

    static void MarkTaken(Button b, TMP_Text label)
    {
        if (b == null) return;
        b.interactable = false;
        TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
        if (t != null) { UIKit.Forget(t); t.text = Loc.T("claim.taken"); t.color = Dim; }
        if (label != null) label.color = Dim;
    }
}
