using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.1.9~ 「빌드 자세히 보기」 (편의성 2차 4 · 3차 9): 일시정지 · 결과 화면에서 지금 가진 카드 · 능력 · 유물 · 각인 · 트리 · 능력치의 설명을 한 창에
// 설명 글은 새로 만들지 않고 HUD 툴팁이 쓰는 함수를 그대로 씀 (같은 글이 두 자리에서 다르게 보이지 않게)
//   레벨업 카드 = LevelShop.GetAbilityTooltip(HudView) · 스킬 진화 = LevelShop.EvoTooltip · 무기 · 특수 능력 · 유물 = SpecialAbilities.TileTitle/TileBody
// 결과 씬에는 LevelShop · SpecialAbilities 가 없으므로 게임 씬을 떠나기 전에 글 그대로 스냅샷 (RunStats.CaptureForResult)
// 섹션은 목록 API(AddSection) — 다른 파일이 섹션을 덧붙일 수 있음
public struct BuildRow
{
    public Sprite icon;
    public string title, body;      // 번역된 글 (리치 텍스트 그대로)
    public int level, max;          // 단계 칸 (max <= 0 이면 없음)
    public Color? tag;              // 이름 색 (유물 보라 · 가지 색 · 다 만든 진화 …)
    public bool dim;                // 흐리게 (아직 못 만든 진화 · 지금 무기로는 안 나옴)
}

public class BuildSection
{
    public string titleKo;          // 섹션 제목 (번역 키)
    public readonly List<BuildRow> rows = new List<BuildRow>();
    public bool grid;               // 두 열 표 (능력치) — title = 이름 · body = 값
    public bool alwaysShow = true;  // 비어도 제목 + 「아직 없음」
    public System.Action<RectTransform, float> custom;    // 직접 그리는 섹션 (피해 막대) — 넘겨받은 폭에 그리고 높이는 BuildView.CustomHeight 로
    public float customHeight;
}

public static class BuildView
{
    static GameObject open;
    public static bool IsOpen => open != null;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.62f, 0.58f, 0.52f);
    static readonly Color RelicTag = new Color(0.79f, 0.64f, 1f);

    // 결과 화면용 스냅샷 (게임 씬을 떠나는 순간)
    static List<BuildSection> snapshot;
    static string snapshotHeader;
    public static bool HasSnapshot => snapshot != null;

    // 다른 파일이 덧붙이는 섹션 (판 안 · 스냅샷 둘 다 이 함수로 만듦)
    static readonly List<System.Func<BuildSection>> extra = new List<System.Func<BuildSection>>();
    public static void AddSection(System.Func<BuildSection> make) { if (make != null && !extra.Contains(make)) extra.Add(make); }
    public static BuildSection Section(string titleKo, IEnumerable<BuildRow> rows)
    {
        BuildSection s = new BuildSection { titleKo = titleKo };
        if (rows != null) s.rows.AddRange(rows);
        return s;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        // 새 판이 시작되면 지난 판 스냅샷은 버림
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => { if (s.name == "GameScene") { snapshot = null; snapshotHeader = null; } };
    }

    public static void TakeSnapshot()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GameScene") return;
        try
        {
            snapshot = Collect(false);
            snapshotHeader = Header();
        }
        catch (System.Exception e) { Debug.LogWarning("[BuildView] 스냅샷 실패: " + e.Message); }
    }

    // ================================================================= 창
    public static void Open(Transform root)
    {
        if (open != null || root == null) return;
        bool live = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameScene";
        if (!live && snapshot == null) return;
        RectTransform win = UIKit.Modal(root, "BuildView", new Vector2(1500f, 900f), out open);
        SoulTreeUI.OnTop(open, 620);                      // 일시정지 패널 · 요약 · 각인 패널보다 위
        GameObject mine = open;
        UIKit.Text(win, "이번 판 빌드", 44f, Gold, new Vector2(0f, 400f), new Vector2(700f, 60f));
        UIKit.MakeButton(win, "닫기", new Vector2(650f, 400f), new Vector2(150f, 58f), Close, 22f);
        TMP_Text head = UIKit.Text(win, "", 22f, Parch, new Vector2(-330f, 400f), new Vector2(720f, 70f), TextAlignmentOptions.Left);
        head.fontSizeMin = 14f;

        RectTransform view = UIKit.Rect("Viewport", win, new Vector2(0f, -40f), new Vector2(1420f, 700f));
        view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        view.gameObject.AddComponent<RectMask2D>();
        RectTransform content = UIKit.Rect("Content", view, Vector2.zero, new Vector2(1400f, 0f));
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup vg = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vg.childControlHeight = true;
        vg.childControlWidth = false;
        vg.childForceExpandHeight = false;
        vg.childForceExpandWidth = false;
        vg.spacing = 6f;
        vg.padding = new RectOffset(10, 10, 12, 16);
        ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = content;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;

        System.Action draw = () =>
        {
            for (int i = content.childCount - 1; i >= 0; i--) Object.Destroy(content.GetChild(i).gameObject);
            List<BuildSection> sections = live ? Collect(true) : snapshot;
            head.text = live ? Header() : snapshotHeader ?? "";
            foreach (BuildSection s in sections) Draw(content, s);
        };
        draw();
        // 언어를 바꾸면 다시 그림 (일시정지 중 설정에서 바꿀 수 있음 · 결과 화면 스냅샷은 그때 언어 그대로)
        if (live) head.gameObject.AddComponent<LocRefresh>().refresh = draw;
        UIKit.EscCancel(open, Close);
        WindowFit.Attach(win, new Vector2(1520f, 920f));
        TooltipUI.Hide();
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }

    // 맨 위 왼쪽: 영웅 · 난이도 / 시간 · 레벨 · 처치 (RunStats.Summary 첫 두 줄과 같은 글)
    static string Header()
    {
        string s = RunStats.Summary(false);
        string[] lines = s.Split('\n');
        return lines.Length >= 2 ? lines[0] + "\n" + lines[1] : s;
    }

    // ================================================================= 그리기
    const float RowW = 1380f, TextX = 84f, BodyW = 1100f;

    static void Draw(RectTransform content, BuildSection s)
    {
        if (s == null) return;
        bool empty = s.rows.Count == 0 && s.custom == null;
        if (empty && !s.alwaysShow) return;
        // 섹션 제목 (직접 그리는 섹션은 제목 · 합계를 스스로 씀 · 제목이 없는 섹션은 줄만)
        if (!string.IsNullOrEmpty(s.titleKo) && (empty || s.custom == null))
        {
            RectTransform head = Block(content, 44f);
            TMP_Text t = UIKit.Text(head, "", 28f, Gold, Vector2.zero, new Vector2(RowW, 40f), TextAlignmentOptions.Left);
            TopLeft(t.rectTransform, new Vector2(0f, -2f));
            t.text = Loc.T(s.titleKo) + (empty ? "   <size=75%><color=#9E968A>" + Loc.T("아직 없음") + "</color></size>" : "");
        }
        if (empty) return;
        if (s.custom != null)
        {
            RectTransform box = Block(content, Mathf.Max(10f, s.customHeight));
            s.custom(box, RowW - 40f);
            return;
        }
        if (s.grid) { DrawGrid(content, s); return; }
        foreach (BuildRow r in s.rows) DrawRow(content, r);
    }

    static void DrawRow(RectTransform content, BuildRow r)
    {
        // 설명 줄 수에 맞춘 높이
        TMP_Text measure = UIKit.Text(content, "", 20f, Parch, Vector2.zero, new Vector2(BodyW, 10f), TextAlignmentOptions.TopLeft);
        measure.enableAutoSizing = false;
        measure.enableWordWrapping = true;
        string body = r.body ?? "";
        float bodyH = body.Length > 0 ? measure.GetPreferredValues(body, BodyW, 0f).y : 0f;
        Object.Destroy(measure.gameObject);
        float h = Mathf.Max(72f, 36f + bodyH + 12f);
        RectTransform row = Block(content, h);
        Image bg = row.gameObject.AddComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.035f);
        bg.raycastTarget = false;
        if (r.icon != null)
        {
            RectTransform ir = UIKit.Rect("Icon", row, Vector2.zero, new Vector2(64f, 64f));
            TopLeft(ir, new Vector2(8f, -4f));
            Image img = ir.gameObject.AddComponent<Image>();
            img.sprite = r.icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (r.dim) img.color = new Color(0.55f, 0.55f, 0.55f, 0.7f);
        }
        TMP_Text title = UIKit.Text(row, "", 24f, r.dim ? Dim : r.tag ?? Gold, Vector2.zero, new Vector2(760f, 32f), TextAlignmentOptions.Left);
        title.fontSizeMin = 14f;
        title.text = r.title ?? "";
        TopLeft(title.rectTransform, new Vector2(TextX, -4f));
        if (r.max > 0)
        {
            float tw = Mathf.Min(740f, title.GetPreferredValues(title.text).x);
            float pw = Mathf.Min(220f, 18f * r.max + 3f * (r.max - 1));
            LevelPips pips = LevelPips.Create(row, Vector2.zero, new Vector2(pw, 14f));
            RectTransform pr = (RectTransform)pips.transform;
            pr.anchorMin = pr.anchorMax = new Vector2(0f, 1f);
            pr.pivot = new Vector2(0f, 0.5f);
            pr.anchoredPosition = new Vector2(TextX + tw + 16f, -20f);
            pips.Set(r.level, r.max);
        }
        if (body.Length > 0)
        {
            TMP_Text b = UIKit.Text(row, "", 20f, r.dim ? Dim : Parch, Vector2.zero, new Vector2(BodyW, bodyH + 4f), TextAlignmentOptions.TopLeft);
            b.enableAutoSizing = false;
            b.enableWordWrapping = true;
            b.text = body;
            TopLeft(b.rectTransform, new Vector2(TextX, -38f));
        }
    }

    static void DrawGrid(RectTransform content, BuildSection s)
    {
        const float lineH = 34f, colW = 660f;
        int perCol = Mathf.CeilToInt(s.rows.Count / 2f);
        RectTransform box = Block(content, perCol * lineH + 8f);
        for (int i = 0; i < s.rows.Count; i++)
        {
            int col = i / Mathf.Max(1, perCol), line = i % Mathf.Max(1, perCol);
            float x = 10f + col * (colW + 40f), y = -4f - line * lineH;
            BuildRow r = s.rows[i];
            if (r.icon != null)
            {
                RectTransform ir = UIKit.Rect("Icon", box, Vector2.zero, new Vector2(28f, 28f));
                TopLeft(ir, new Vector2(x, y - 2f));
                Image img = ir.gameObject.AddComponent<Image>();
                img.sprite = r.icon;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
            TMP_Text name = UIKit.Text(box, "", 22f, Parch, Vector2.zero, new Vector2(360f, lineH), TextAlignmentOptions.Left);
            name.text = r.title;
            TopLeft(name.rectTransform, new Vector2(x + 38f, y));
            TMP_Text val = UIKit.Text(box, "", 22f, Gold, Vector2.zero, new Vector2(260f, lineH), TextAlignmentOptions.Right);
            val.text = r.body;
            TopLeft(val.rectTransform, new Vector2(x + colW - 260f, y));
        }
    }

    static RectTransform Block(RectTransform content, float h)
    {
        RectTransform r = UIKit.Rect("Row", content, Vector2.zero, new Vector2(RowW, h));
        LayoutElement le = r.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = le.minHeight = h;
        le.preferredWidth = RowW;
        return r;
    }

    static void TopLeft(RectTransform r, Vector2 pos)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = pos;
    }

    // ================================================================= 자료 모으기 (판 안)
    static List<BuildSection> Collect(bool live)
    {
        List<BuildSection> list = new List<BuildSection>();
        LevelShop shop = Object.FindFirstObjectByType<LevelShop>(FindObjectsInactive.Include);
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        CharacterId who = CharacterData.Selected;

        // 레벨업 카드 (+ 성장 · 초월)
        BuildSection cards = new BuildSection { titleKo = "레벨업 카드" };
        if (shop != null && shop.ability_level != null && shop.ability_name != null)
        {
            for (int i = 0; i < shop.ability_level.Length && i < shop.ability_name.Length; i++)
            {
                int lv = shop.ability_level[i];
                if (lv <= 0 || string.IsNullOrEmpty(shop.ability_name[i])) continue;
                int max = LevelShop.MaxLevelOf(who, i);
                string body;
                LevelShop.HudView = true;          // 지금 수치 + 최대 (HUD 툴팁과 같게)
                try { body = shop.GetAbilityTooltip(i); }
                finally { LevelShop.HudView = false; }
                cards.rows.Add(new BuildRow { icon = LevelShop.CardIcon(who, i), title = shop.ability_name[i] + "  <size=80%>Lv " + lv + "</size>", body = body, level = lv, max = max });
            }
            for (int k = 0; k < LevelShop.GrowthCount; k++)
            {
                int lv = shop.GrowthLevel(k);
                if (lv <= 0) continue;
                string body;
                LevelShop.HudView = true;
                try { body = shop.GetAbilityTooltip(LevelShop.GrowthFirstId + k); }
                finally { LevelShop.HudView = false; }
                cards.rows.Add(new BuildRow { icon = shop.GrowthIconOf(k), title = LevelShop.GrowthName(k) + "  <size=80%>Lv " + lv + "</size>", body = body });
            }
            for (int k = 0; k < LevelShop.TranscendCount; k++)
            {
                int lv = shop.TranscendLevel(k);
                if (lv <= 0) continue;
                cards.rows.Add(new BuildRow
                {
                    icon = Resources.Load<Sprite>("Icons/choice_" + LevelShop.TranscendIcon(k)),
                    title = LevelShop.TranscendName(k),
                    body = LevelShop.TranscendBody(k, lv),
                    level = lv, max = LevelShop.TranscendMax[k],
                    tag = new Color(1f, 0.84f, 0.32f),
                });
            }
        }
        list.Add(cards);

        // 스킬 진화
        BuildSection evos = new BuildSection { titleKo = "스킬 진화" };
        if (shop != null)
            foreach (KeyValuePair<LevelShop.SkillEvo, float> kv in shop.EvoLog)
                evos.rows.Add(new BuildRow
                {
                    icon = Resources.Load<Sprite>("Icons/ability_" + kv.Key.icon),
                    title = Loc.T(kv.Key.name) + "  <size=75%><color=#A89C86>(" + EndlessMode.Clock(kv.Value) + ")</color></size>",
                    body = LevelShop.EvoTooltip(kv.Key, who),
                    tag = new Color(0.79f, 0.63f, 1f),
                });
        list.Add(evos);

        // 무기 진화 · 특수 능력 · 유물 · 패시브 (HUD 칸 툴팁과 같은 글)
        BuildSection weaponEvo = new BuildSection { titleKo = "무기 진화", alwaysShow = false };
        BuildSection specials = new BuildSection { titleKo = "특수 능력" };
        if (sp != null)
        {
            foreach (SpecialAbilities.TileInfo ti in sp.BuildTiles())
            {
                if (ti.evoRow)
                {
                    string when = "";
                    foreach (KeyValuePair<string, float> e in sp.EvolutionLog) when += (when.Length > 0 ? " · " : "") + EndlessMode.Clock(e.Value);
                    weaponEvo.rows.Add(new BuildRow { icon = ti.icon, title = ti.title + (when.Length > 0 ? "  <size=75%><color=#A89C86>(" + when + ")</color></size>" : ""), body = ti.body });
                    continue;
                }
                specials.rows.Add(new BuildRow
                {
                    icon = ti.icon,
                    title = ti.title + (ti.relic ? "  <color=#C9A2FF>(" + Loc.T("유물") + ")</color>" : ""),
                    body = ti.body,
                    tag = ti.relic ? RelicTag : (Color?)null,
                });
            }
        }
        list.Add(weaponEvo);
        list.Add(specials);

        // 영혼 각인
        BuildSection brands = new BuildSection { titleKo = "brand.title", alwaysShow = false };
        foreach (SoulBrandDef b in SoulBrand.Run)
            brands.rows.Add(new BuildRow { icon = Resources.Load<Sprite>(ChoiceUI.IconPath(b.Icon)), title = b.Name, body = SoulBrand.CardTextOf(b), tag = b.color });
        list.Add(brands);

        // 영혼 트리: 가지별 배운 칸
        BuildSection tree = new BuildSection { titleKo = "영혼 트리" };
        if (sp != null)
        {
            List<string>[] names = new List<string>[SpecialAbilities.BranchCount];
            for (int b = 0; b < names.Length; b++) names[b] = new List<string>();
            foreach (SpecialAbilities.SoulNode n in sp.BuildSoulTree())
                if (sp.OwnsNode(n.key) && n.branch >= 0 && n.branch < names.Length) names[n.branch].Add(n.name);
            for (int b = 0; b < names.Length; b++)
            {
                if (names[b].Count == 0) continue;
                tree.rows.Add(new BuildRow
                {
                    title = Loc.T(SpecialAbilities.BranchNames[b]) + "  <size=80%>" + names[b].Count + "</size>",
                    body = string.Join(" · ", names[b]),
                    tag = SoulTreeUI.BranchColorOf(b),
                });
            }
            if (sp.Refines > 0) tree.rows.Add(new BuildRow { title = Loc.T("영혼 정련") + "  <size=80%>x" + sp.Refines + "</size>", tag = new Color(0.79f, 0.63f, 1f) });
        }
        list.Add(tree);

        // 능력치 (오른쪽 아래 HUD 와 같은 값 · 두 열)
        BuildSection stats = new BuildSection { titleKo = "능력치", grid = true };
        StatsHUD hud = StatsHUD.Instance;
        if (hud != null)
        {
            string[] v = hud.ValueTexts();
            for (int i = 0; i < StatsHUD.Labels.Length && i < v.Length; i++)
                stats.rows.Add(new BuildRow { icon = hud.RowSprite(i), title = Loc.T(StatsHUD.Labels[i]), body = v[i] });
        }
        list.Add(stats);

        // 3차 9: 준 피해 · 받은 피해 · 처치 · 진화 진행
        list.Add(DamageSection("준 피해", RunStats.DamageDealt, 8, new Color(0.96f, 0.72f, 0.3f)));
        list.Add(DamageSection("받은 피해", RunStats.DamageTaken, 5, new Color(0.92f, 0.32f, 0.3f)));
        list.Add(KillsSection(live));
        list.Add(EvoProgress(shop, who));

        foreach (System.Func<BuildSection> f in extra)
        {
            BuildSection s = null;
            try { s = f(); } catch (System.Exception e) { Debug.LogWarning("[BuildView] 섹션 실패: " + e.Message); }
            if (s != null) list.Add(s);
        }
        return list;
    }

    // 준 피해 · 받은 피해 막대 (결과 화면과 같은 그리기 · 폭만 넓게) — 사전 복사본으로 (결과 화면이 다른 판 값을 보지 않게)
    static BuildSection DamageSection(string titleKo, Dictionary<string, float> from, int top, Color bar)
    {
        List<KeyValuePair<string, float>> rows = RunStats.Top(new Dictionary<string, float>(from), top);
        BuildSection s = new BuildSection { titleKo = titleKo };
        if (rows.Count == 0) return s;
        s.customHeight = 40f + rows.Count * 36f + 6f;
        s.custom = (box, width) => RunStats.Section(box, 0f, Loc.T(titleKo), rows, bar, width);
        return s;
    }

    static BuildSection KillsSection(bool live)
    {
        // 「처치 n · 분당 m」 한 줄 (제목 없음) — 분당 처치는 최근 1분 (결과 화면은 판 전체 평균)
        BuildSection s = new BuildSection { titleKo = null };
        float perMin = live ? RunStats.KillsPerMin : RunStats.Kills / Mathf.Max(1f, RunStats.Seconds / 60f);
        string line = Loc.T("처치 ") + RunStats.Kills.ToString("N0") + "   ·   " + Loc.T("분당 처치") + " " + Mathf.RoundToInt(perMin).ToString("N0");
        s.rows.Add(new BuildRow { title = "<color=#EDE6D6>" + line + "</color>" });
        return s;
    }

    // 지금 영웅의 모든 스킬 진화: 재료 단계 → 결과 (아직 본 적 없는 진화는 결과를 「???」, 재료는 보임)
    static BuildSection EvoProgress(LevelShop shop, CharacterId who)
    {
        BuildSection s = new BuildSection { titleKo = "진화 진행" };
        if (shop == null) return s;
        HashSet<string> done = new HashSet<string>();
        foreach (KeyValuePair<LevelShop.SkillEvo, float> kv in shop.EvoLog) done.Add(kv.Key.key);
        foreach (LevelShop.SkillEvo e in shop.MyEvos)
        {
            bool made = done.Contains(e.key);
            bool known = made || CodexSeen.Shown(CodexSeen.SkillEvoEntry(who, e.key));
            int left = 0;
            bool blocked = false;
            List<string> parts = new List<string>();
            foreach (int p in e.parts)
            {
                int max = LevelShop.MaxLevelOf(who, p);
                int lv = shop.LevelOf(p);
                left += Mathf.Max(0, max - lv);
                bool full = max > 0 && lv >= max;
                if (!full && !LevelShop.KitFits(p)) blocked = true;
                string name = p >= 0 && p < shop.ability_name.Length && !string.IsNullOrEmpty(shop.ability_name[p]) ? shop.ability_name[p] : "?";
                parts.Add((full ? "<color=#9CFF9C>" : "") + name + " " + lv + "/" + max + (full ? "</color>" : ""));
            }
            string state = made ? "<color=#9CFF9C>" + Loc.T("진화") + "</color>"
                         : blocked ? "<color=#A8A8A8>" + Loc.T("지금 무기로는 안 나옴") + "</color>"
                         : left <= 0 ? "<color=#FFD966>" + Loc.T("바로 진화!") + "</color>"
                         : left == 1 ? "<color=#FFE08A>" + Loc.T("진화까지 1단계!") + "</color>"
                         : Loc.T("{0}단계 남음").Replace("{0}", left.ToString());
            s.rows.Add(new BuildRow
            {
                icon = known ? Resources.Load<Sprite>("Icons/ability_" + e.icon) : null,
                title = (known ? Loc.T(e.name) : "???") + "   <size=80%>" + state + "</size>",
                body = string.Join("  +  ", parts) + "  →  " + (known ? Loc.T(e.name) : "???"),
                tag = made ? new Color(0.6f, 1f, 0.6f) : (Color?)null,
                dim = !made && blocked,
            });
        }
        return s;
    }
}
