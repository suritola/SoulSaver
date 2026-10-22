using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.1.9~ 메인 메뉴 「기록」 창 (편의성 2차 11): 지난 판 30개 · 영웅별 통계
// 자료는 판마다 쌓이는 balance_log.csv 와, 머리줄이 바뀌어 밀려난 balance_log_*.csv 를 함께 — 파일마다 자기 머리줄로 열 번호를 찾아 이름으로 읽음
// (없는 열은 빈 값 · date · character · result 가 없는 파일은 건너뜀 · 파일마다 끝 2,000줄까지)
// 펼친 줄 오른쪽 끝 「복사」 (편의성 3차 10 — 그 판을 글로 클립보드에)
public static class RunHistoryUI
{
    static GameObject open;
    public static bool IsOpen => open != null;
    static int tab;
    static int expanded = -1;
    static RectTransform content;
    static TMP_Text emptyText;
    static readonly List<Button> tabButtons = new List<Button>();
    static List<Run> runs;

    static Color Gold => UiRules.Legacy ? LegacyGold : UiTokens.Text1;      // UI 개편 (Legacy 끔): 금색 글자 대신 뼈색 (기획서 C6 「금색 25벌」)
    static readonly Color LegacyGold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.66f, 0.61f, 0.53f);
    static readonly Color Off = new Color(0.6f, 0.58f, 0.65f);

    const float RowW = 1380f;
    const int MaxRecent = 30, MaxLinesPerFile = 2000;

    class Run
    {
        public System.DateTime date;
        public Dictionary<string, string> v;
        public string this[string k] => v.TryGetValue(k, out string s) ? s ?? "" : "";
        public int Int(string k) => int.TryParse(this[k], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0;
        public bool HasHero => System.Enum.TryParse(this["character"], out CharacterId _);
        public CharacterId Hero => System.Enum.TryParse(this["character"], out CharacterId id) ? id : CharacterId.Gunner;
        public bool Endless => this["endless"] == "1";
    }

    public static void Open(Transform root)
    {
        if (open != null || root == null) return;
        runs = ReadAll();
        expanded = -1;
        RectTransform win = UIKit.Modal(root, "RunHistory", new Vector2(1500f, 900f), out open);
        UIKit.Text(win, "기록", 48f, Gold, new Vector2(0f, 400f), new Vector2(600f, 64f));
        UIKit.MakeButton(win, "닫기", new Vector2(650f, 400f), new Vector2(150f, 58f), Close, 22f);
        tabButtons.Clear();
        string[] names = { "지난 판", "영웅별 통계" };
        for (int i = 0; i < names.Length; i++)
        {
            int t = i;
            tabButtons.Add(UIKit.MakeButton(win, names[i], new Vector2(-160f + 320f * i, 325f), new Vector2(300f, 58f), () => Show(t), 24f));
        }
        RectTransform view = UIKit.Rect("Viewport", win, new Vector2(0f, -55f), new Vector2(1420f, 700f));
        view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        view.gameObject.AddComponent<RectMask2D>();
        content = UIKit.Rect("Content", view, Vector2.zero, new Vector2(1400f, 0f));
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup vg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vg.childControlHeight = true;
        vg.childControlWidth = false;
        vg.childForceExpandHeight = false;
        vg.childForceExpandWidth = false;
        vg.spacing = 4f;
        vg.padding = new RectOffset(10, 10, 10, 14);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;
        emptyText = UIKit.Text(view, "아직 기록이 없습니다. 한 판을 끝내면 여기에 남습니다.", 28f, Dim, Vector2.zero, new Vector2(1200f, 80f));
        UIKit.EscCancel(open, Close);
        WindowFit.Attach(win, new Vector2(1520f, 920f));
        content.gameObject.AddComponent<LocRefresh>().refresh = () => Show(tab);
        Show(tab);
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
        content = null;
    }

    static void Show(int t)
    {
        tab = t;
        if (content == null) return;
        for (int i = 0; i < tabButtons.Count; i++)
        {
            if (!UiRules.Legacy) { UiV2.Choose(tabButtons[i], i == tab); continue; }      // UI 개편: 금색 칠 · 확대 대신 그림 + 밑줄
            tabButtons[i].GetComponent<Image>().color = i == tab ? Gold : Off;
            tabButtons[i].transform.localScale = Vector3.one * (i == tab ? 1.05f : 1f);
        }
        for (int i = content.childCount - 1; i >= 0; i--) Object.Destroy(content.GetChild(i).gameObject);
        emptyText.gameObject.SetActive(runs.Count == 0);
        if (runs.Count == 0) return;
        if (tab == 0) BuildRecent();
        else BuildStats();
    }

    // ================================================================= 지난 판
    static void BuildRecent()
    {
        int n = Mathf.Min(MaxRecent, runs.Count);
        for (int i = 0; i < n; i++) RunRow(runs[i], i);
    }

    static void RunRow(Run r, int index)
    {
        bool isOpen = expanded == index;
        string detail = isOpen ? Detail(r) : "";
        float detailH = 0f;
        if (isOpen)
        {
            TMP_Text measure = UIKit.Text(content, "", 20f, Parch, Vector2.zero, new Vector2(1180f, 10f), TextAlignmentOptions.TopLeft);
            measure.enableAutoSizing = false;
            measure.enableWordWrapping = true;
            detailH = measure.GetPreferredValues(detail, 1180f, 0f).y + 14f;
            Object.Destroy(measure.gameObject);
        }
        float h = 64f + (isOpen ? Mathf.Max(56f, detailH) : 0f);
        RectTransform row = Block(h);
        Image bg = row.gameObject.AddComponent<Image>();
        bg.color = isOpen ? new Color(1f, 1f, 1f, 0.07f) : new Color(1f, 1f, 1f, index % 2 == 0 ? 0.035f : 0.015f);
        // 윗줄 전체가 펼침 버튼
        RectTransform head = UIKit.Rect("Head", row, Vector2.zero, new Vector2(RowW, 64f));
        TopLeft(head, Vector2.zero);
        Image hi = head.gameObject.AddComponent<Image>();
        hi.color = new Color(0f, 0f, 0f, 0f);
        Button hb = head.gameObject.AddComponent<Button>();
        hb.transition = Selectable.Transition.None;
        int at = index;
        hb.onClick.AddListener(() => { expanded = expanded == at ? -1 : at; Show(0); });

        Cell(head, r.date.ToString("MM/dd HH:mm", CultureInfo.InvariantCulture), 10f, 130f, 20f, Dim);
        if (r.HasHero)
        {
            RectTransform pr = UIKit.Rect("Portrait", head, Vector2.zero, new Vector2(48f, 48f));
            TopLeft(pr, new Vector2(150f, -8f));
            Image img = pr.gameObject.AddComponent<Image>();
            img.sprite = CharacterUI.Portrait(r.Hero);
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        Cell(head, r.HasHero ? Loc.T(CharacterData.Def(r.Hero).name) : r["character"], 210f, 180f, 24f, Gold);
        Cell(head, DifficultyText(r), 400f, 200f, 20f, Parch);
        string res = r["result"];
        Cell(head, res == "clear" ? "<color=#9CFF9C>" + Loc.T("클리어") + "</color>" : res == "death" ? "<color=#B8B2A8>" + Loc.T("쓰러짐") + "</color>" : "<color=#FFD966>" + Loc.T("포기") + "</color>", 610f, 110f, 22f, Parch);
        Cell(head, EndlessMode.Clock(r.Int("seconds")), 730f, 110f, 22f, Parch);
        Cell(head, "Lv " + r.Int("level"), 850f, 90f, 22f, Parch);
        Cell(head, Loc.T("처치 ") + r.Int("kills").ToString("N0"), 950f, 170f, 22f, Parch);
        string where = res == "death" && r["last_hit"].Length > 0 ? RunStats.SourceName(r["last_hit"])
                     : r.Endless ? Loc.T("무한 모드") : r.Int("stage") > 0 ? Loc.T("{0}장").Replace("{0}", r.Int("stage").ToString()) : "";
        Cell(head, where, 1130f, 240f, 20f, Dim);

        if (!isOpen) return;
        TMP_Text body = UIKit.Text(row, "", 20f, Parch, Vector2.zero, new Vector2(1180f, detailH), TextAlignmentOptions.TopLeft);
        body.enableAutoSizing = false;
        body.enableWordWrapping = true;
        body.text = detail;
        TopLeft(body.rectTransform, new Vector2(30f, -66f));
        // 오른쪽 끝 「복사」 160×44 (3차 10)
        Run copy = r;
        Button cb = RunRecords.CopyButton(row, "복사", new Vector2(160f, 44f), () => ShareText(copy), 20f);
        TopLeft((RectTransform)cb.transform, new Vector2(RowW - 180f, -72f));
    }

    static string DifficultyText(Run r)
    {
        if (r.Endless) return GameMode.Name(Difficulty.Endless);
        string d = System.Enum.TryParse(r["difficulty"], out Difficulty dd) ? GameMode.Name(dd) : r["difficulty"];
        int w = r.Int("weight");
        return w > 0 ? d + " " + Loc.T("weight.short").Replace("{0}", w.ToString()) : d;
    }

    // 펼친 줄: 무기 진화 · 스킬 진화 · 각인 · 유물 · 준/받은 피해 상위 3
    static string Detail(Run r)
    {
        string gold = "<color=#F5D478>", end = "</color>";
        List<string> lines = new List<string>();
        string evo = Loc.T("기본 무기");
        if (r["evo1"].Length > 0) evo += "  →  " + r["evo1"] + Sec(r["evo1_sec"]);
        if (r["evo2"].Length > 0) evo += "  →  " + r["evo2"] + Sec(r["evo2_sec"]);
        lines.Add(gold + Loc.T("무기 진화") + end + "  " + evo);
        string skill = SkillEvoNames(r);
        if (skill.Length > 0) lines.Add(gold + Loc.T("스킬 진화") + end + "  " + skill);
        List<string> brands = new List<string>();
        foreach (string c in Split(r["brands"])) { SoulBrandDef b = ContentRegistry.SoulBrandByCode(c.Trim()); if (b != null) brands.Add(b.Name); }
        if (brands.Count > 0) lines.Add(gold + Loc.T("brand.title") + end + "  " + string.Join(", ", brands));
        List<string> relics = new List<string>();
        foreach (string c in Split(r["relics"])) { RelicDef d = ContentRegistry.RelicByCode(c.Trim()); if (d != null) relics.Add(CodexUI.RelicName(d)); }
        if (relics.Count > 0) lines.Add(gold + Loc.T("유물") + end + "  " + string.Join(", ", relics));
        string dealt = TopParts(r["dealt_by"], 3);
        if (dealt.Length > 0) lines.Add(gold + Loc.T("준 피해") + end + "  " + dealt);
        string taken = TopParts(r["taken_by"], 3);
        if (taken.Length > 0) lines.Add(gold + Loc.T("받은 피해") + end + "  " + taken);
        return string.Join("\n", lines);
    }

    static string Sec(string s) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? " <color=#A89C86>(" + EndlessMode.Clock(v) + ")</color>" : "";

    static IEnumerable<string> Split(string s) => (s ?? "").Split(new[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);

    // "키@초|키@초" → 이름 (시각)
    static string SkillEvoNames(Run r)
    {
        List<string> outs = new List<string>();
        List<LevelShop.SkillEvo> all = r.HasHero ? LevelShop.EvosFor(r.Hero) : new List<LevelShop.SkillEvo>();
        foreach (string part in Split(r["skill_evos"]))
        {
            string[] kv = part.Split('@');
            LevelShop.SkillEvo e = all.Find(x => x.key == kv[0]);
            string name = e != null ? Loc.T(e.name) : kv[0];
            outs.Add("<color=#C9A0FF>" + name + "</color>" + (kv.Length > 1 ? Sec(kv[1]) : ""));
        }
        return string.Join(", ", outs);
    }

    // "이름:양|…" (많은 순) → 상위 n 「이름 41%」
    static string TopParts(string s, int n)
    {
        List<KeyValuePair<string, float>> all = new List<KeyValuePair<string, float>>();
        float total = 0f;
        foreach (string part in Split(s))
        {
            int c = part.LastIndexOf(':');
            if (c <= 0 || !float.TryParse(part.Substring(c + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out float v)) continue;
            all.Add(new KeyValuePair<string, float>(part.Substring(0, c), v));
            total += v;
        }
        all.Sort((a, b) => b.Value.CompareTo(a.Value));
        List<string> outs = new List<string>();
        for (int i = 0; i < all.Count && i < n; i++) outs.Add(RunStats.SourceName(all[i].Key) + " " + (all[i].Value / Mathf.Max(1f, total) * 100f).ToString("0") + "%");
        return string.Join(",  ", outs);
    }

    // 그 판을 결과 복사와 같은 꼴의 글로
    static string ShareText(Run r)
    {
        string res = r["result"] == "clear" ? "클리어" : r["result"] == "death" ? "쓰러짐" : "포기";
        System.Text.StringBuilder b = new System.Text.StringBuilder();
        b.Append("Soul Saver v").Append(r["version"]).Append(" · ").Append(r.HasHero ? Loc.T(CharacterData.Def(r.Hero).name) : r["character"])
         .Append(" · ").Append(RunRecords.Plain(DifficultyText(r))).Append(" · ").Append(Loc.T(res))
         .Append(" · ").Append(EndlessMode.Clock(r.Int("seconds"))).Append(" · Lv ").Append(r.Int("level"))
         .Append(" · ").Append(Loc.T("처치 ")).Append(r.Int("kills").ToString("N0"));
        string detail = RunRecords.Plain(Detail(r));
        if (detail.Length > 0) b.Append('\n').Append(detail.Replace("  ", ": ").Replace(":  →", " →"));
        return b.ToString();
    }

    // ================================================================= 영웅별 통계
    static void BuildStats()
    {
        string[] heads = { "", "판 수", "클리어 수", "최고 레벨", "최장 시간", "최단 클리어", "가장 많이 쓰러진 원인" };
        float[] xs = { 70f, 330f, 470f, 610f, 750f, 900f, 1060f };
        float[] ws = { 250f, 130f, 130f, 130f, 140f, 150f, 310f };
        RectTransform hr = Block(48f);
        for (int i = 0; i < heads.Length; i++) if (heads[i].Length > 0) Cell(hr, Loc.T(heads[i]), xs[i], ws[i], 20f, Gold);

        List<Run> all = runs;
        StatRow(Loc.T("합계"), null, all, xs, ws, true);
        foreach (CharacterId id in System.Enum.GetValues(typeof(CharacterId)))
        {
            List<Run> mine = all.FindAll(x => x.HasHero && x.Hero == id);
            if (mine.Count == 0) continue;       // 판이 없는 영웅은 숨김
            StatRow(Loc.T(CharacterData.Def(id).name), CharacterUI.Portrait(id), mine, xs, ws, false);
        }
    }

    static void StatRow(string name, Sprite icon, List<Run> list, float[] xs, float[] ws, bool total)
    {
        RectTransform row = Block(60f);
        Image bg = row.gameObject.AddComponent<Image>();
        bg.color = total ? new Color(0.96f, 0.83f, 0.47f, 0.08f) : new Color(1f, 1f, 1f, 0.03f);
        bg.raycastTarget = false;
        if (icon != null)
        {
            RectTransform pr = UIKit.Rect("Portrait", row, Vector2.zero, new Vector2(48f, 48f));
            TopLeft(pr, new Vector2(12f, -6f));
            Image img = pr.gameObject.AddComponent<Image>();
            img.sprite = icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        int clears = 0, bestLevel = 0, longest = 0, fastest = 0;
        Dictionary<string, int> causes = new Dictionary<string, int>();
        foreach (Run r in list)
        {
            bool clear = r["result"] == "clear";
            if (clear) { clears++; int s = r.Int("seconds"); if (s > 0 && (fastest == 0 || s < fastest)) fastest = s; }
            bestLevel = Mathf.Max(bestLevel, r.Int("level"));
            longest = Mathf.Max(longest, r.Int("seconds"));
            string hit = r["last_hit"];
            if (r["result"] == "death" && hit.Length > 0) causes[hit] = causes.TryGetValue(hit, out int c) ? c + 1 : 1;
        }
        string cause = "-";
        int most = 0;
        foreach (KeyValuePair<string, int> kv in causes) if (kv.Value > most) { most = kv.Value; cause = RunStats.SourceName(kv.Key) + " <color=#A89C86>(" + kv.Value + ")</color>"; }
        Cell(row, name, xs[0], ws[0], 24f, total ? Gold : Parch);
        Cell(row, list.Count.ToString("N0"), xs[1], ws[1], 22f, Parch);
        Cell(row, clears.ToString("N0"), xs[2], ws[2], 22f, Parch);
        Cell(row, bestLevel > 0 ? bestLevel.ToString() : "-", xs[3], ws[3], 22f, Parch);
        Cell(row, longest > 0 ? EndlessMode.Clock(longest) : "-", xs[4], ws[4], 22f, Parch);
        Cell(row, fastest > 0 ? EndlessMode.Clock(fastest) : "-", xs[5], ws[5], 22f, Parch);
        Cell(row, cause, xs[6], ws[6], 20f, Dim);
    }

    // ================================================================= 공통
    static RectTransform Block(float h)
    {
        RectTransform r = UIKit.Rect("Row", content, Vector2.zero, new Vector2(RowW, h));
        LayoutElement le = r.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = le.minHeight = h;
        le.preferredWidth = RowW;
        return r;
    }

    static TMP_Text Cell(RectTransform parent, string text, float x, float w, float size, Color color)
    {
        TMP_Text t = UIKit.Text(parent, "", size, color, Vector2.zero, new Vector2(w, 48f), TextAlignmentOptions.Left);
        t.fontSizeMin = 12f;
        t.text = text;
        TopLeft(t.rectTransform, new Vector2(x, -8f));
        return t;
    }

    static void TopLeft(RectTransform r, Vector2 pos)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = pos;
    }

    // ================================================================= CSV 읽기
    static List<Run> ReadAll()
    {
        List<Run> list = new List<Run>();
        try
        {
            string dir = Application.persistentDataPath;
            if (!Directory.Exists(dir)) return list;
            foreach (string path in Directory.GetFiles(dir, "balance_log*.csv"))
            {
                try { ReadFile(path, list); }
                catch (System.Exception e) { Debug.LogWarning("[RunHistory] 읽지 못함 " + Path.GetFileName(path) + ": " + e.Message); }
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[RunHistory] " + e.Message); }
        list.Sort((a, b) => b.date.CompareTo(a.date));
        return list;
    }

    static void ReadFile(string path, List<Run> into)
    {
        string[] lines = File.ReadAllLines(path);
        if (lines.Length < 2) return;
        string[] head = lines[0].Trim('﻿').Split(',');
        int dateCol = System.Array.IndexOf(head, "date"), charCol = System.Array.IndexOf(head, "character"), resCol = System.Array.IndexOf(head, "result");
        if (dateCol < 0 || charCol < 0 || resCol < 0) return;
        int from = Mathf.Max(1, lines.Length - MaxLinesPerFile);
        for (int i = from; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] cells = lines[i].Split(',');
            if (cells.Length <= Mathf.Max(dateCol, Mathf.Max(charCol, resCol))) continue;
            if (!System.DateTime.TryParseExact(cells[dateCol].Trim(), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out System.DateTime date)) continue;
            Dictionary<string, string> v = new Dictionary<string, string>();
            for (int c = 0; c < head.Length && c < cells.Length; c++) v[head[c]] = cells[c];
            into.Add(new Run { date = date, v = v });
        }
    }
}
