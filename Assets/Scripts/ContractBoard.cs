using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.2~ 영혼 계약 보드 (설계 3-1 ②): 영웅 9열 × 1.2 범위 5줄 = 45칸 (영웅 줄 1 손맛 · 2 보스 · 3 탐색 + 공용 두 줄) — 4 ~ 6줄(27칸)은 1.3
//   칸마다 진행 막대 · 수치. 영웅 줄은 위 · 왼쪽 칸을 채우면 다음 칸이 드러나고, 공용 줄은 처음부터 보임 · 채운 칸은 늘 보임
//   보상: 포인트는 「받기」로 (미리 채운 칸 · 판 안에서 채운 칸 모두 — 조용히 더하지 않음, 4-2 원칙 ②)
//         새 유물 · 칭호 · 영웅 해제 조건(지도 제작자 · 시계공 · 유령의 이야기 모드 길)은 칸을 채우는 순간
//   기존 기록(숙련 · 도감 · 영혼의 무게 · 무한 모드 · 각인 · 유물)으로 셀 수 있는 칸은 처음 열 때부터 채워져 있음 (받기 창이 먼저 뜸)
//   체험판: 보드 없음 (「정식판에서 열립니다」) · 글: LocTable.Contract 의 board.<열쇠>.name · .desc · .cond
public sealed class BoardCell
{
    public string key;                  // 저장 · 글 열쇠 (board.<key>.name)
    public int col, row;                // 열 0 ~ 8 · 줄 0 ~ 2 영웅, 3 ~ 4 공용
    public bool heroRow;
    public int target = 1;
    public System.Func<int> value;
    public int points;
    public string relic;                // 새 유물 saveCode (ContentRegistry.Relics)
    public bool title;                  // 칸 이름이 칭호가 됨
    public CharUnlock route = CharUnlock.None;      // 이 영웅 해제 조건을 채움
    public CharacterId routeHero;
    public bool onerun;                 // 「한 판 안에」
    public string Name => Loc.T("board." + key + ".name");
    public string Desc => Loc.T("board." + key + ".desc");
    public string Cond => Loc.T("board." + key + ".cond");
    public CharacterId Hero => (CharacterId)col;
}

public static class ContractBoard
{
    public const int Cols = 9, Rows = 5, HeroRows = 3;
    static int L(string k) => RunCounters.Life(k);
    static int Lh(string k, int c) => RunCounters.Life(RunCounters.Hero(k, (CharacterId)c));

    static readonly string[] Row1 = { "ult", "ult", "ult", "perfect", "unstable", "shape", "oclock", "letter", "freeze" };
    static readonly int[] Row1Target = { 100, 100, 100, 300, 100, 1000, 200, 1000, 500 };
    static readonly string[] Row3 = { "chest", "sarco", "event.goblin", "event.done", "chest.legend", "sarco", "midboss", "shop", "event.altar" };
    static readonly int[] Row3Target = { 20, 10, 10, 15, 5, 10, 30, 20, 10 };
    public static readonly string[] Row3Relic = { "casing", "scabbard", "seal", "arrow", "notebook", "mapscrap", "clock", "letter", null };
    static readonly string[] OriginalRelics = { "wc", "jd", "dm", "ct", "fa", "ga", "gi", "sk" };

    static List<BoardCell> cells;
    public static List<BoardCell> Cells => cells ??= Build();

    static List<BoardCell> Build()
    {
        List<BoardCell> l = new List<BoardCell>();
        for (int c = 0; c < Cols; c++)
        {
            int col = c;
            string h = Contracts.HeroKeys[c];
            l.Add(new BoardCell { key = h + ".1", col = c, row = 0, heroRow = true, target = Row1Target[c], value = () => Lh(Row1[col], col), points = 500 });
            l.Add(new BoardCell { key = h + ".2", col = c, row = 1, heroRow = true, target = 4, value = () => RunCounters.BitCount(RunCounters.Hero("bosskinds", (CharacterId)col)), points = 1000 });
            l.Add(new BoardCell { key = h + ".3", col = c, row = 2, heroRow = true, target = Row3Target[c], value = () => Lh(Row3[col], col),
                                  points = Row3Relic[c] == null ? 1500 : 0, relic = Row3Relic[c] });
        }
        void S(string key, int col, int row, int target, System.Func<int> v, int points, bool title = false, bool onerun = false, CharUnlock route = CharUnlock.None, CharacterId routeHero = CharacterId.Gunner) =>
            l.Add(new BoardCell { key = "c." + key, col = col, row = row, target = target, value = v, points = points, title = title, onerun = onerun, route = route, routeHero = routeHero });
        // ---------------- 공용 첫 줄
        S("alleasy", 0, 3, 9, HeroesCleared, 3000);
        S("brands12", 1, 3, 12, () => CountFound("brand.", ContentRegistry.SoulBrands.ConvertAll(b => b.saveCode)), 2000);
        S("events6", 2, 3, 6, () => L("events.onerun"), 2000, onerun: true);
        S("lanterns", 3, 3, 1, () => L("lanterns15"), 1500, onerun: true);
        S("roll10", 4, 3, 10, () => L("roll.dodge"), 1000);
        S("mirrorquick", 5, 3, 1, () => (RunCounters.Bits("quickkinds") & (1 << 3)) != 0 ? 1 : 0, 2000);
        S("chest15", 6, 3, 15, () => L("chest.story"), 1000, route: CharacterData.Def(CharacterId.Cartographer).condition, routeHero: CharacterId.Cartographer);
        S("quick4", 7, 3, 4, () => RunCounters.BitCount("quickkinds"), 2000, route: CharacterData.Def(CharacterId.Clockmaker).condition, routeHero: CharacterId.Clockmaker);
        S("weight2", 8, 3, 1, () => GameMode.HasCleared(Difficulty.Hard) && BestWeight() >= 2 ? 1 : 0, 2000, route: CharacterData.Def(CharacterId.Ghost).condition, routeHero: CharacterId.Ghost);
        // ---------------- 공용 둘째 줄
        S("daily7", 0, 4, 7, () => Mathf.Max(L("daily.streak.best"), DailyRewards.Streak), 2000);
        S("relics8", 1, 4, 8, () => CountFound("relic.", new List<string>(OriginalRelics)), 3000);
        S("endless5", 2, 4, 1, () => CharacterData.ConditionMet(CharUnlock.EndlessBosses5) ? 1 : 0, 2000);
        S("codex50", 3, 4, 50, () => CodexSeen.Percent(), 2000);
        S("codex100", 4, 4, 100, () => CodexSeen.Percent(), 5000);
        S("endlessclear", 5, 4, 1, () => EndlessMode.EverCleared ? 1 : 0, 5000, title: true);
        S("mastery9", 6, 4, 9, () => { int n = 0; for (int i = 0; i < Cols; i++) if (Mastery.Level((CharacterId)i) >= Mastery.Max) n++; return n; }, 5000, title: true);
        S("weight6", 7, 4, 1, () => BestWeight() >= SoulWeight.Max ? 1 : 0, 5000);
        S("last", 8, 4, 1, () => AllOthersDone() ? 1 : 0, 0);
        return l;
    }

    static int HeroesCleared()
    {
        int n = 0;
        for (int i = 0; i < Cols; i++) if (Mastery.Level((CharacterId)i) >= 1 || Lh("clear", i) > 0) n++;
        return n;
    }

    static int BestWeight()
    {
        int b = 0;
        for (int i = 0; i < Cols; i++) b = Mathf.Max(b, SoulWeight.Best((CharacterId)i));
        return b;
    }

    static int CountFound(string prefix, List<string> codes)
    {
        int n = 0;
        foreach (string c in codes) if (CodexSeen.Found(prefix + c)) n++;
        return n;
    }

    static bool AllOthersDone()
    {
        foreach (BoardCell c in Cells) if (c.key != "c.last" && !IsDoneNow(c)) return false;
        return true;
    }

    // ================================================================= 상태
    const string DoneKey = "board.done", ClaimKey = "board.claimed", GrantPrefix = "board.grant.", MigratedKey = "board.migrated";
    static HashSet<string> Set(string key) => new HashSet<string>(Prefs.GetString(key, "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));
    static void SaveSet(string key, HashSet<string> s) => Prefs.SetString(key, string.Join(",", s));

    static bool IsDoneNow(BoardCell c) => Prefs.GetInt(GrantPrefix + c.key, 0) == 1 || (c.value != null && c.value() >= c.target);
    public static bool IsDone(string key) => Set(DoneKey).Contains(key);
    public static bool IsClaimed(string key) => Set(ClaimKey).Contains(key);
    public static bool Available => !Demo.On;

    public static int Progress(BoardCell c) => Prefs.GetInt(GrantPrefix + c.key, 0) == 1 ? c.target : Mathf.Clamp(c.value != null ? c.value() : 0, 0, c.target);

    // 칸이 보이는지: 공용 줄 · 맨 위 줄 · 채운 칸은 늘, 영웅 줄은 위나 왼쪽 칸을 채웠을 때
    public static bool Visible(BoardCell c, HashSet<string> done)
    {
        if (!c.heroRow || c.row == 0 || done.Contains(c.key)) return true;
        BoardCell up = At(c.col, c.row - 1), left = c.col > 0 ? At(c.col - 1, c.row) : null;
        return (up != null && done.Contains(up.key)) || (left != null && done.Contains(left.key));
    }

    public static BoardCell At(int col, int row)
    {
        foreach (BoardCell c in Cells) if (c.col == col && c.row == row) return c;
        return null;
    }

    // 받을 보상 수 (메뉴 배지)
    public static int Claimable
    {
        get
        {
            if (!Available) return 0;
            HashSet<string> done = Set(DoneKey), claimed = Set(ClaimKey);
            int n = 0;
            foreach (BoardCell c in Cells) if (done.Contains(c.key) && !claimed.Contains(c.key) && c.points > 0) n++;
            return n;
        }
    }

    public static int ColumnDone(CharacterId id)
    {
        HashSet<string> done = Set(DoneKey);
        int n = 0;
        foreach (BoardCell c in Cells) if (c.heroRow && c.col == (int)id && done.Contains(c.key)) n++;
        return n;
    }

    // 새로 채운 칸을 적음 (판 안 0.5초마다 · 메뉴를 열 때) · inRun = 판 안이면 토스트
    //   새 유물 · 칭호 · 영웅 해제 조건은 이때 · 포인트는 받기로
    public static void Refresh(bool inRun)
    {
        if (!Available) return;
        HashSet<string> done = Set(DoneKey);
        bool changed = false;
        foreach (BoardCell c in Cells)
        {
            if (done.Contains(c.key) || !IsDoneNow(c)) continue;
            done.Add(c.key);
            changed = true;
            if (c.title) Titles.Grant("board." + c.key, "board." + c.key + ".name");
            if (c.route != CharUnlock.None && !CharacterData.ConditionMet(c.route)) CharacterData.Achieve(c.route);
            if (c.key == "c.last") Notice.Big(Loc.T("board.complete"), NoticeIcon.Star, 5f, NoticeLevel.Important);
            if (inRun)
            {
                string reward = RewardLine(c);
                Notice.Toast(NoticeIcon.Star, Loc.T("board.cell.toast").Replace("{0}", c.Name) + (reward.Length > 0 ? "\n" + reward : ""), 5f, new Color(0.79f, 0.63f, 1f));
            }
        }
        if (!changed) return;
        SaveSet(DoneKey, done);
        Prefs.Save();
        MemoryShards.Check(inRun);
    }

    public static int Claim(BoardCell c)
    {
        HashSet<string> done = Set(DoneKey), claimed = Set(ClaimKey);
        if (!done.Contains(c.key) || claimed.Contains(c.key)) return 0;
        claimed.Add(c.key);
        SaveSet(ClaimKey, claimed);
        CharacterData.AddPoints(c.points);
        Prefs.Save();
        return c.points;
    }

    public static int ClaimAll()
    {
        int sum = 0;
        foreach (BoardCell c in Cells) sum += Claim(c);
        return sum;
    }

    // 일일 도전 「오늘의 영웅」(아직 못 산 영웅)으로 완주: 그 영웅 열에서 아직 못 채운 맨 위 칸 하나를 미리 채움
    public static bool GrantOne(CharacterId id)
    {
        if (!Available) return false;
        for (int r = 0; r < HeroRows; r++)
        {
            BoardCell c = At((int)id, r);
            if (c == null || IsDoneNow(c)) continue;
            Prefs.SetInt(GrantPrefix + c.key, 1);
            Refresh(false);
            return true;
        }
        return false;
    }

    // 보상 한 줄 (칸 · 툴팁 · 토스트)
    public static string RewardLine(BoardCell c)
    {
        List<string> parts = new List<string>();
        if (c.route != CharUnlock.None) parts.Add(Loc.T("board.reward.hero").Replace("{0}", Loc.T(CharacterData.Def(c.routeHero).name)));
        if (c.relic != null) parts.Add(Loc.T("board.reward.relic").Replace("{0}", Loc.T("relic.new." + c.relic + ".name")));
        if (c.points > 0) parts.Add(Loc.T("board.reward.points").Replace("{0}", c.points.ToString("N0")));
        if (c.title) parts.Add(Loc.T("board.reward.title").Replace("{0}", c.Name));
        return string.Join(" + ", parts);
    }

    // ================================================================= 창
    static GameObject open;
    static Transform openRoot;
    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f), Parch = new Color(0.93f, 0.9f, 0.84f), Dim = new Color(0.66f, 0.61f, 0.53f), Violet = new Color(0.79f, 0.63f, 1f);
    public static System.Action Changed;

    public static void Open(Transform root)
    {
        if (open != null) return;
        if (!Available) { Notice.Toast(NoticeIcon.Info, Loc.T("contract.demo"), 3f); return; }
        openRoot = root;
        Refresh(false);
        bool first = Prefs.GetInt(MigratedKey, 0) == 0;
        if (first) { Prefs.SetInt(MigratedKey, 1); Prefs.Save(); }
        Draw();
        if (first && Claimable > 0) OpenClaim(true);
    }

    static void Draw()
    {
        if (open != null) Object.Destroy(open);
        RectTransform win = UIKit.Modal(openRoot, "ContractBoard", new Vector2(1660f, 980f), out open);
        UIKit.Text(win, "board.title", 46f, Gold, new Vector2(0f, 440f), new Vector2(800f, 64f));
        UIKit.MakeButton(win, "닫기", new Vector2(730f, 440f), new Vector2(150f, 58f), Close, 22f);
        TMP_Text how = UIKit.Text(win, "board.how", 19f, Dim, new Vector2(0f, 386f), new Vector2(1500f, 50f));
        how.fontSizeMin = 14f;

        HashSet<string> done = Set(DoneKey), claimed = Set(ClaimKey);
        const float CellW = 160f, CellH = 118f, Gap = 8f, LabelW = 96f;
        float x0 = -((Cols - 1) * (CellW + Gap)) * 0.5f + LabelW * 0.5f;
        float yHead = 318f, y0 = 238f;
        // 열 머리: 영웅 초상 · 이름 (못 산 영웅은 흐리게)
        for (int c = 0; c < Cols; c++)
        {
            CharacterId id = (CharacterId)c;
            bool owned = CharacterData.IsUnlocked(id);
            float x = x0 + c * (CellW + Gap);
            Sprite face = CharacterUI.Portrait(id);
            if (face != null)
            {
                Image fi = UIKit.Rect("Face", win, new Vector2(x - 52f, yHead), new Vector2(46f, 46f)).gameObject.AddComponent<Image>();
                fi.sprite = face;
                fi.preserveAspect = true;
                fi.color = owned ? Color.white : new Color(0.4f, 0.4f, 0.45f, 0.8f);
                fi.raycastTarget = false;
            }
            TMP_Text n = UIKit.Text(win, "", 17f, owned ? Parch : Dim, new Vector2(x + 18f, yHead), new Vector2(CellW - 60f, 40f), TextAlignmentOptions.Left);
            n.text = Loc.T(CharacterData.Def(id).name);
            n.fontSizeMin = 11f;
            UIKit.Forget(n);
        }
        // 줄 이름
        string[] rowKeys = { "board.row.1", "board.row.2", "board.row.3", "board.row.7", "board.row.7" };
        for (int r = 0; r < Rows; r++)
        {
            float y = y0 - r * (CellH + Gap) - (r >= HeroRows ? 14f : 0f);
            TMP_Text t = UIKit.Text(win, rowKeys[r], 19f, r >= HeroRows ? Violet : Gold, new Vector2(x0 - CellW * 0.5f - LabelW * 0.5f - 6f, y), new Vector2(LabelW, 40f));
            t.fontSizeMin = 12f;
        }
        foreach (BoardCell cell in Cells)
        {
            float y = y0 - cell.row * (CellH + Gap) - (cell.row >= HeroRows ? 14f : 0f);
            CellView(win, cell, new Vector2(x0 + cell.col * (CellW + Gap), y), new Vector2(CellW, CellH), done, claimed);
        }

        // 아래: 칭호 · 받을 보상
        TMP_Text titles = UIKit.Text(win, "", 18f, Violet, new Vector2(-180f, -440f), new Vector2(1180f, 34f), TextAlignmentOptions.Left);
        titles.text = Titles.Line();
        UIKit.Forget(titles);
        int claimable = Claimable;
        Button all = UIKit.MakeButton(win, "", new Vector2(640f, -440f), new Vector2(300f, 58f), () => OpenClaim(false), 22f);
        TMP_Text at = all.GetComponentInChildren<TMP_Text>();
        if (at != null) { UIKit.Forget(at); at.text = Loc.T("claim.badge").Replace("{0}", claimable.ToString()); }
        all.interactable = claimable > 0;
        if (claimable > 0) all.gameObject.AddComponent<NewGlow>();
        UIKit.EscCancel(open, Close);
        WindowFit.Attach(win, new Vector2(1680f, 1000f));
    }

    static void CellView(RectTransform win, BoardCell c, Vector2 pos, Vector2 size, HashSet<string> done, HashSet<string> claimed)
    {
        bool isDone = done.Contains(c.key), isClaimed = claimed.Contains(c.key) || c.points <= 0;
        bool locked = c.heroRow && !CharacterData.IsUnlocked(c.Hero) && !isDone;
        bool visible = Visible(c, done) && !(c.key == "c.last" && !isDone);
        RectTransform cell = UIKit.Rect("Cell_" + c.key, win, pos, size);
        Image bg = cell.gameObject.AddComponent<Image>();
        bg.sprite = UIKit.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = isDone ? (isClaimed ? new Color(0.36f, 0.3f, 0.16f, 1f) : new Color(0.62f, 0.5f, 0.2f, 1f))
                 : locked || !visible ? new Color(0.13f, 0.12f, 0.16f, 1f) : new Color(0.2f, 0.17f, 0.26f, 1f);
        TooltipTrigger tip = cell.gameObject.AddComponent<TooltipTrigger>();

        if (!visible)
        {
            UIKit.Text(cell, "board.cell.hidden", 30f, Dim, Vector2.zero, size);
            tip.titleProvider = () => "???";
            tip.bodyProvider = () => Loc.T(c.key == "c.last" ? "board.c.last.desc" : "board.cell.hidden.how");
            return;
        }
        TMP_Text name = UIKit.Text(cell, "", 16f, isDone ? Gold : locked ? Dim : Parch, new Vector2(0f, 34f), new Vector2(size.x - 14f, 42f));
        name.text = c.Name + (c.onerun ? "\n<size=75%><color=#C9A2FF>" + Loc.T("board.cell.onerun") + "</color></size>" : "");
        name.fontSizeMin = 11f;
        UIKit.Forget(name);
        int now = Progress(c);
        // 진행 막대
        RectTransform bar = UIKit.Rect("Bar", cell, new Vector2(0f, -4f), new Vector2(size.x - 22f, 9f));
        Image bb = bar.gameObject.AddComponent<Image>();
        bb.color = new Color(0.08f, 0.07f, 0.1f, 0.95f);
        bb.raycastTarget = false;
        RectTransform fill = UIKit.Rect("Fill", bar, Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(Mathf.Max(0.02f, (float)now / Mathf.Max(1, c.target)), 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        Image fi = fill.gameObject.AddComponent<Image>();
        fi.color = isDone ? new Color(0.49f, 1f, 0.6f) : new Color(0.62f, 0.47f, 0.92f);
        fi.raycastTarget = false;
        TMP_Text prog = UIKit.Text(cell, "", 14f, Dim, new Vector2(0f, -22f), new Vector2(size.x - 14f, 22f));
        prog.text = locked ? Loc.T("board.col.locked") : isDone ? Loc.T("board.cell.done") : Loc.T("board.cell.progress").Replace("{0}", now.ToString("N0")).Replace("{1}", c.target.ToString("N0"));
        prog.fontSizeMin = 10f;
        UIKit.Forget(prog);
        if (isDone && !isClaimed)
        {
            BoardCell cc = c;
            Button b = UIKit.MakeButton(cell, "claim.one", new Vector2(0f, -44f), new Vector2(size.x - 40f, 30f), () =>
            {
                int got = Claim(cc);
                if (got > 0) { Hostile.Play("coin", 0.8f, 1.2f); Notice.Toast(NoticeIcon.Coin, Loc.T("claim.done") + "  +" + got.ToString("N0") + " P", 2.5f); }
                Changed?.Invoke();
                Draw();
            }, 16f);
            b.name = "Claim";
        }
        else
        {
            TMP_Text rw = UIKit.Text(cell, "", 13f, isDone ? new Color(0.8f, 0.72f, 0.5f) : new Color(0.55f, 0.52f, 0.6f), new Vector2(0f, -44f), new Vector2(size.x - 12f, 26f));
            rw.text = RewardLine(c);
            rw.fontSizeMin = 9f;
            UIKit.Forget(rw);
        }
        tip.titleProvider = () => c.Name;
        tip.bodyProvider = () => c.Desc + "\n<color=#F5D478>" + c.Cond + "</color>  (" + Progress(c).ToString("N0") + " / " + c.target.ToString("N0") + ")"
                                + "\n" + RewardLine(c) + (locked ? "\n" + Loc.T("board.col.locked") : "");
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
        Changed?.Invoke();
    }

    // ================================================================= 받기 창 (첫 실행: 지금까지의 기록으로 채운 칸)
    static void OpenClaim(bool first)
    {
        if (openRoot == null) return;
        List<BoardCell> list = new List<BoardCell>();
        HashSet<string> done = Set(DoneKey), claimed = Set(ClaimKey);
        int sum = 0;
        foreach (BoardCell c in Cells) if (done.Contains(c.key) && !claimed.Contains(c.key) && c.points > 0) { list.Add(c); sum += c.points; }
        if (list.Count == 0) return;
        int rows = Mathf.Min(list.Count, 9);
        RectTransform win = UIKit.Modal(openRoot, "BoardClaim", new Vector2(900f, 300f + rows * 46f), out GameObject blocker);
        float top = (300f + rows * 46f) * 0.5f;
        UIKit.Text(win, "claim.title", 40f, Gold, new Vector2(0f, top - 50f), new Vector2(800f, 56f));
        TMP_Text desc = UIKit.Text(win, first ? "claim.first" : "claim.desc", 20f, Parch, new Vector2(0f, top - 104f), new Vector2(800f, 56f));
        desc.fontSizeMin = 14f;
        for (int i = 0; i < rows; i++)
        {
            BoardCell c = list[i];
            TMP_Text line = UIKit.Text(win, "", 21f, Parch, new Vector2(-60f, top - 160f - i * 46f), new Vector2(640f, 40f), TextAlignmentOptions.Left);
            line.text = c.Name;
            UIKit.Forget(line);
            TMP_Text p = UIKit.Text(win, "", 21f, Gold, new Vector2(330f, top - 160f - i * 46f), new Vector2(160f, 40f), TextAlignmentOptions.Right);
            p.text = "+" + c.points.ToString("N0") + " P";
            UIKit.Forget(p);
        }
        TMP_Text total = UIKit.Text(win, "", 26f, Gold, new Vector2(0f, -top + 104f), new Vector2(800f, 40f));
        total.text = Loc.T("claim.sum").Replace("{0}", sum.ToString("N0")) + (list.Count > rows ? "  <size=70%><color=#A89C86>(+" + (list.Count - rows) + ")</color></size>" : "");
        UIKit.Forget(total);
        System.Action close = () => { if (blocker != null) Object.Destroy(blocker); };
        UIKit.MakeButton(win, "claim.all", new Vector2(0f, -top + 46f), new Vector2(320f, 66f), () =>
        {
            int got = ClaimAll();
            close();
            if (got > 0) { Hostile.Play("chime", 0.8f, 1.2f); Notice.Toast(NoticeIcon.Coin, Loc.T("claim.done") + "  +" + got.ToString("N0") + " P", 3f); }
            Changed?.Invoke();
            if (open != null) Draw();
        }, 26f);
        UIKit.EscCancel(blocker, () => close());
    }
}

// 1.2~ 칭호 (계약 보드 · 기억 조각 · 일일 도전 완주): 얻은 것을 모아 보여 줌 (보드 창 아래 줄)
public static class Titles
{
    const string Key = "titles";
    public static List<string> Owned() => new List<string>(Prefs.GetString(Key, "").Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries));
    static string NameKeyOf(string id) => Prefs.GetString("title.name." + id, id);

    public static void Grant(string id, string nameKey)
    {
        if (string.IsNullOrEmpty(id) || Demo.On || RunCounters.Off) return;
        List<string> l = Owned();
        if (l.Contains(id)) return;
        l.Add(id);
        Prefs.SetString(Key, string.Join(",", l));
        Prefs.SetString("title.name." + id, nameKey);
        Prefs.Save();
        Notice.Big(Loc.T("title.got").Replace("{0}", Loc.T(nameKey)), NoticeIcon.Star, 3.5f, NoticeLevel.Important);
    }

    public static string Line()
    {
        List<string> l = Owned();
        if (l.Count == 0) return "<color=#A89C86>" + Loc.T("title.none") + "</color>";
        List<string> names = new List<string>();
        foreach (string id in l) names.Add(Loc.T(NameKeyOf(id)));
        return Loc.T("title.label") + "  " + string.Join(" · ", names);
    }
}
