using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 1.2.5~ ESC 일시정지: 따로 버튼을 누르지 않아도 메뉴 왼쪽(오른쪽은 「이번 판」 요약)에 지금 가진 것을 아이콘으로
//   묶음: 무기 · 레벨업 스킬 · 특수 스킬 · 유물 · 영혼 각인 — 아이콘 + 단계 칸(LevelPips) + 진화 · 최대(MAX, 단계 칸이 그림) 표시
//   마우스를 올리면 이름 · 단계 · 효과 (설명 글은 HUD 툴팁 · 빌드 자세히 보기와 같은 함수: GetAbilityTooltip(HudView) · EvoTooltip · BuildTiles · CardTextOf)
//   게임패드: 칸마다 Selectable 이라 십자키가 가상 커서를 칸에 붙임 → 툴팁 · 오른쪽 스틱으로 스크롤
//   많으면 칸을 줄이고(64 → 44), 그래도 넘치면 스크롤 · 설정 · 확인 · 빌드 보기 · 도감 창이 열리면 숨김
//   메뉴를 열 때 한 번 만들고 닫을 때 지움 (매 프레임 만들지 않음 · Update 는 bool 비교만)
public class PauseBuild : MonoBehaviour
{
    static PauseBuild open;

    struct Cell
    {
        public Sprite icon;
        public string title, body, mark;
        public int level, max;
        public Color markColor;
    }

    sealed class Group
    {
        public string key;
        public readonly List<Cell> cells = new List<Cell>();
    }

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Faint = new Color(0.66f, 0.61f, 0.53f);
    static readonly Color EvoColor = new Color(0.79f, 0.63f, 1f);
    static readonly Color RelicColor = new Color(0.79f, 0.64f, 1f);

    CanvasGroup group;
    TMP_Text hint;
    bool hidden, hintPad;

    // ESCmenu.ToggleEsc 가 부름 (열기 true · 닫기 false)
    public static void Show(bool show)
    {
        Close();
        if (!show) return;
        Canvas canvas = UIKit.HudCanvas();
        if (canvas == null) return;
        try { Build(canvas); }
        catch (System.Exception e)
        {
            Debug.LogWarning("[PauseBuild] 만들기 실패: " + e.Message);
            Close();
        }
    }

    static void Close()
    {
        if (open != null) Destroy(open.gameObject);
        open = null;
    }

    void OnDestroy() { if (open == this) open = null; }

    void Update()
    {
        // 다른 창이 위에 열리면 숨김 (알파 · 레이캐스트만 — 다시 만들지 않음)
        bool hide = ESCmenu.ConfirmOpen || SettingsUI.IsOpen || BuildView.IsOpen || CodexUI.IsOpen || SkillEvoCodexUI.IsOpen
                    || MemoryShards.ReaderOpen || HeroGuide.IsOpen || !ESCmenu.IsOpen;
        if (hide != hidden)
        {
            hidden = hide;
            group.alpha = hide ? 0f : 1f;
            group.blocksRaycasts = !hide;
            group.interactable = !hide;         // 숨은 칸에 십자키가 붙지 않게
        }
        if (hint != null && hintPad != GamePad.Active) WriteHint();
    }

    void WriteHint()
    {
        hintPad = GamePad.Active;
        hint.text = Loc.T(hintPad ? "pausebuild.hint.pad" : "pausebuild.hint.mouse");
    }

    // ================================================================= 자료 (판 안)
    static List<Group> Collect()
    {
        List<Group> list = new List<Group>();
        Group weapons = new Group { key = "pausebuild.weapon" };
        Group cards = new Group { key = "pausebuild.cards" };
        Group specials = new Group { key = "pausebuild.special" };
        Group relics = new Group { key = "pausebuild.relic" };
        Group transcends = new Group { key = "pausebuild.transcend" };      // 1.4.1~ 초월을 레벨업 스킬과 따로 (섞여 있어 찾기 어려웠음)
        Group brands = new Group { key = "brand.title" };
        string evoMark = Loc.T("pausebuild.evolved");

        LevelShop shop = Object.FindFirstObjectByType<LevelShop>(FindObjectsInactive.Include);
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        CharacterId who = CharacterData.Selected;

        // 무기 · 특수 스킬 · 유물 (HUD 칸 툴팁과 같은 글)
        if (sp != null)
            foreach (SpecialAbilities.TileInfo ti in sp.BuildTiles())
            {
                Cell c = new Cell { icon = ti.icon, title = ti.title, body = ti.body };
                if (ti.evolved) { c.mark = evoMark; c.markColor = EvoColor; }
                if (ti.relic) relics.cells.Add(c);
                else if (ti.kind == SpecialKind.Weapon) weapons.cells.Add(c);
                else specials.cells.Add(c);
            }

        // 레벨업 스킬 (+ 성장 · 초월 · 스킬 진화)
        if (shop != null && shop.ability_level != null && shop.ability_name != null)
        {
            for (int i = 0; i < shop.ability_level.Length && i < shop.ability_name.Length; i++)
            {
                int lv = shop.ability_level[i];
                if (lv <= 0 || string.IsNullOrEmpty(shop.ability_name[i])) continue;
                int max = shop.CapOf(i);           // 1.4.1~ 외길이면 갈래 A 카드 +1 (HUD 칸과 같게 · 예전엔 원래 최대라 「Lv 4 / 3」)
                cards.cells.Add(new Cell
                {
                    icon = LevelShop.CardIcon(who, i),
                    title = shop.ability_name[i] + "  <size=80%>" + LevelLine(lv, max) + "</size>",
                    body = HudTooltip(shop, i),
                    level = lv, max = max,
                });
            }
            for (int k = 0; k < LevelShop.GrowthCount; k++)
            {
                int lv = shop.GrowthLevel(k);
                if (lv <= 0) continue;
                cards.cells.Add(new Cell
                {
                    icon = shop.GrowthIconOf(k),
                    title = LevelShop.GrowthName(k) + "  <size=80%>Lv " + lv + "</size>",
                    body = HudTooltip(shop, LevelShop.GrowthFirstId + k),
                    mark = "Lv" + lv, markColor = Parch,          // 끝없는 단계 → 칸 대신 숫자
                });
            }
            for (int k = 0; k < LevelShop.TranscendCount; k++)
            {
                int lv = shop.TranscendLevel(k);
                if (lv <= 0) continue;
                int max = LevelShop.TranscendMax[k];
                transcends.cells.Add(new Cell
                {
                    icon = Resources.Load<Sprite>("Icons/choice_" + LevelShop.TranscendIcon(k)),
                    title = LevelShop.TranscendName(k) + "  <size=80%>" + LevelLine(lv, max) + "</size>",
                    body = LevelShop.TranscendBody(k, lv),
                    level = lv, max = max,
                });
            }
            foreach (KeyValuePair<LevelShop.SkillEvo, float> kv in shop.EvoLog)
                cards.cells.Add(new Cell
                {
                    icon = Resources.Load<Sprite>("Icons/ability_" + kv.Key.icon),
                    title = Loc.T(kv.Key.name) + "  <size=80%><color=#C9A2FF>" + Loc.T("진화") + "</color></size>",
                    body = LevelShop.EvoTooltip(kv.Key, who),
                    mark = evoMark, markColor = EvoColor,
                });
        }

        foreach (SoulBrandDef b in SoulBrand.Run)
            brands.cells.Add(new Cell { icon = Resources.Load<Sprite>(ChoiceUI.IconPath(b.Icon)), title = b.Name, body = SoulBrand.CardTextOf(b) });

        foreach (Group g in new[] { weapons, cards, transcends, specials, relics, brands })
            if (g.cells.Count > 0) list.Add(g);
        return list;
    }

    static string LevelLine(int lv, int max) =>
        max > 0 ? "Lv " + lv + " / " + max + (lv >= max ? "  <color=#FFD966>MAX</color>" : "") : "Lv " + lv;

    static string HudTooltip(LevelShop shop, int id)
    {
        LevelShop.HudView = true;           // 지금 수치 + 최대 (HUD 툴팁과 같게)
        try { return shop.GetAbilityTooltip(id); }
        finally { LevelShop.HudView = false; }
    }

    // ================================================================= 그리기
    // 1.4.8~ 안쪽 여백 18 → 24 · 아래 안내 17 → 20 (두 줄 자리) · 묶음 머리 21 → 22
    const float Pad = 24f, TitleH = 64f, HintH = 58f, HeadH = 32f, Gap = 8f, GroupGap = 10f, PipH = 8f, PipGap = 4f;
    static readonly float[] Sizes = { 64f, 56f, 50f, 44f };

    static void Build(Canvas canvas)
    {
        List<Group> groups = Collect();
        RectTransform cr = (RectTransform)canvas.transform;
        Vector2 cs = cr.rect.size;
        // 왼쪽 빈자리 (가운데 ESC 창 폭 720 · 양옆 30 · 틈 12) — 좁은 화면 · 큰 UI 크기에서도 메뉴와 겹치지 않게
        float w = Mathf.Clamp((cs.x - 720f) * 0.5f - 42f, 240f, RunStats.PausePanelW);
        // 1.4.8~ 예전엔 늘 560 높이라 스킬이 몇 개 없으면 거의 빈 판이었음 → 위끝은 오른쪽 「이번 판」과 같게 두고 높이는 내용만큼 (최대 RunStats.PausePanelMaxH, 넘치면 스크롤)
        float maxH = RunStats.PausePanelMaxH(cr);

        GameObject go = new GameObject("PauseBuild", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(canvas.transform, false);
        r.SetAsLastSibling();                       // 일시정지 판 위 (「이번 판」 요약과 같은 자리 규칙)
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = new Vector2(30f, -RunStats.PausePanelTop);    // 오른쪽 요약과 같은 위끝
        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.06f, 0.05f, 0.08f, 0.88f);
        bg.raycastTarget = true;                    // 이 판 아래 HUD 툴팁은 띄우지 않음 (PauseInspect)
        PauseBuild pb = go.AddComponent<PauseBuild>();
        pb.group = go.GetComponent<CanvasGroup>();
        open = pb;

        TMP_Text title = UIKit.Text(r, "pausebuild.title", 28f, Gold, Vector2.zero, new Vector2(w - Pad * 2f, 40f), TextAlignmentOptions.Left);
        title.fontSizeMin = 20f;
        TopLeft(title.rectTransform, new Vector2(Pad, -Pad + 6f));
        pb.hint = UIKit.Text(r, "", 20f, Faint, Vector2.zero, new Vector2(w - Pad * 2f, HintH - 8f), TextAlignmentOptions.BottomLeft);
        pb.hint.fontSizeMin = 20f;
        RectTransform hr = pb.hint.rectTransform;
        hr.anchorMin = hr.anchorMax = hr.pivot = new Vector2(0f, 0f);
        hr.anchoredPosition = new Vector2(Pad, 14f);
        pb.WriteHint();

        float innerW = w - Pad * 2f;
        float maxViewH = maxH - TitleH - HintH - 10f;
        float total;
        float size = Sizes[Sizes.Length - 1];
        if (groups.Count == 0) total = 40f;
        else
        {
            // 다 들어가는 가장 큰 칸 (안 되면 가장 작은 칸 + 스크롤)
            foreach (float s in Sizes)
                if (Height(groups, s, innerW) <= maxViewH) { size = s; break; }
            total = Height(groups, size, innerW) - GroupGap;         // 마지막 묶음 아래 틈은 빼고
        }
        float viewH = Mathf.Min(total, maxViewH);
        float h = TitleH + viewH + 10f + HintH;
        r.sizeDelta = new Vector2(w, h);

        RectTransform view = UIKit.Rect("Viewport", r, Vector2.zero, new Vector2(innerW, viewH));
        TopLeft(view, new Vector2(Pad, -TitleH));
        view.gameObject.AddComponent<RectMask2D>();
        Image vi = view.gameObject.AddComponent<Image>();
        vi.color = new Color(0f, 0f, 0f, 0f);       // 스크롤 휠 · 끌기를 받는 바탕
        RectTransform content = UIKit.Rect("Content", view, Vector2.zero, new Vector2(innerW, 10f));
        TopLeft(content, Vector2.zero);

        if (groups.Count == 0)
        {
            TMP_Text e = UIKit.Text(content, "pausebuild.empty", 22f, Faint, Vector2.zero, new Vector2(innerW, 34f), TextAlignmentOptions.Left);
            e.fontSizeMin = 20f;
            TopLeft(e.rectTransform, new Vector2(0f, -4f));
            content.sizeDelta = new Vector2(innerW, 40f);
        }
        else
        {
            float drawn = Draw(content, groups, size, innerW);
            content.sizeDelta = new Vector2(innerW, drawn);
            if (drawn - GroupGap > viewH + 0.5f)
            {
                ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
                scroll.content = content;
                scroll.viewport = view;
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 40f;
            }
        }

        // 일시정지 중 언어를 바꾸면 다시 만듦 (한 번)
        go.AddComponent<LocRefresh>().refresh = () => { if (open == pb) Show(true); };
    }

    static int Cols(float size, float innerW) => Mathf.Max(1, Mathf.FloorToInt((innerW + Gap) / (size + Gap)));

    static float RowH(Group g, float size)
    {
        bool pips = false;
        foreach (Cell c in g.cells) if (c.max > 0) { pips = true; break; }
        return size + (pips ? PipGap + PipH : 0f) + Gap;
    }

    static float Height(List<Group> groups, float size, float innerW)
    {
        int cols = Cols(size, innerW);
        float y = 0f;
        foreach (Group g in groups) y += HeadH + Mathf.CeilToInt(g.cells.Count / (float)cols) * RowH(g, size) + GroupGap;
        return y;
    }

    static float Draw(RectTransform content, List<Group> groups, float size, float innerW)
    {
        int cols = Cols(size, innerW);
        float y = 0f;
        foreach (Group g in groups)
        {
            TMP_Text head = UIKit.Text(content, "", 22f, Parch, Vector2.zero, new Vector2(innerW, HeadH - 4f), TextAlignmentOptions.Left);
            head.fontSizeMin = 20f;
            head.text = Loc.T(g.key) + "  <size=80%><color=#9E968A>" + g.cells.Count + "</color></size>";
            TopLeft(head.rectTransform, new Vector2(0f, -y));
            y += HeadH;
            float rowH = RowH(g, size);
            for (int i = 0; i < g.cells.Count; i++)
            {
                float x = (i % cols) * (size + Gap);
                float cy = y + (i / cols) * rowH;
                MakeCell(content, g.cells[i], new Vector2(x, -cy), size);
            }
            y += Mathf.CeilToInt(g.cells.Count / (float)cols) * rowH + GroupGap;
        }
        return y;
    }

    static void MakeCell(RectTransform content, Cell c, Vector2 pos, float size)
    {
        GameObject go = new GameObject("Cell", typeof(RectTransform), typeof(Image), typeof(Selectable), typeof(TooltipTrigger));
        RectTransform r = (RectTransform)go.transform;
        r.SetParent(content, false);
        r.sizeDelta = new Vector2(size, size);
        TopLeft(r, pos);
        Image frame = go.GetComponent<Image>();
        frame.color = new Color(0.42f, 0.36f, 0.52f, 0.95f);
        frame.raycastTarget = true;
        // 마우스 · 패드 커서가 올라가면 밝게 (ColorTint 곱: 평소 어둡게)
        Selectable sel = go.GetComponent<Selectable>();
        sel.targetGraphic = frame;
        sel.navigation = new Navigation { mode = Navigation.Mode.None };      // 키보드 · 패드 Navigation 이 ESC 메뉴 버튼에서 넘어오지 않게 (십자키는 GamePad.Snap 이 커서로)
        ColorBlock cb = sel.colors;
        cb.normalColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        cb.highlightedColor = Color.white;
        cb.selectedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        cb.fadeDuration = 0.06f;
        sel.colors = cb;
        TooltipTrigger tip = go.GetComponent<TooltipTrigger>();
        tip.title = c.title ?? "";
        tip.body = c.body ?? "";

        RectTransform inner = UIKit.Rect("Back", r, Vector2.zero, new Vector2(size - 4f, size - 4f));
        Image back = inner.gameObject.AddComponent<Image>();
        back.color = new Color(0.08f, 0.07f, 0.11f, 1f);
        back.raycastTarget = false;
        if (c.icon != null)
        {
            RectTransform ir = UIKit.Rect("Icon", r, Vector2.zero, new Vector2(size - 10f, size - 10f));
            Image img = ir.gameObject.AddComponent<Image>();
            img.sprite = c.icon;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }
        if (!string.IsNullOrEmpty(c.mark))
        {
            // 오른쪽 위 작은 띠 (진화 · 끝없는 단계 숫자)
            RectTransform mr = UIKit.Rect("Mark", r, Vector2.zero, new Vector2(size - 6f, 17f));
            mr.anchorMin = mr.anchorMax = mr.pivot = new Vector2(1f, 1f);
            mr.anchoredPosition = new Vector2(-1f, -1f);
            Image mb = mr.gameObject.AddComponent<Image>();
            mb.color = new Color(0.05f, 0.03f, 0.08f, 0.82f);
            mb.raycastTarget = false;
            TMP_Text mt = UIKit.Text(mr, "", 14f, c.markColor, Vector2.zero, new Vector2(size - 8f, 17f), TextAlignmentOptions.Right);
            mt.fontSizeMin = 9f;
            mt.text = c.mark;
            mt.rectTransform.anchorMin = Vector2.zero;
            mt.rectTransform.anchorMax = Vector2.one;
            mt.rectTransform.sizeDelta = new Vector2(-4f, 0f);
            mt.rectTransform.anchoredPosition = Vector2.zero;
        }
        if (c.max > 0)
        {
            LevelPips pips = LevelPips.Create(content, Vector2.zero, new Vector2(size, PipH));
            RectTransform pr = (RectTransform)pips.transform;
            pr.anchorMin = pr.anchorMax = new Vector2(0f, 1f);
            pr.pivot = new Vector2(0f, 1f);
            pr.anchoredPosition = pos - new Vector2(0f, size + PipGap);
            pips.Set(c.level, c.max);
        }
    }

    static void TopLeft(RectTransform r, Vector2 pos)
    {
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.anchoredPosition = pos;
    }
}

// 번역 (1.2.5~ 일시정지 「지금 가진 스킬」) — 이미 있는 키는 건드리지 않음
public static partial class LocTable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AddPauseBuild()
    {
        void C(string key, string ko, string en, string ja, string zh) { if (!Entries.ContainsKey(key)) Entries[key] = E(ko, en, ja, zh); }
        C("pausebuild.title", "지금 가진 스킬", "Current Build", "現在のスキル", "当前技能");
        C("pausebuild.weapon", "무기", "Weapons", "武器", "武器");
        C("pausebuild.cards", "레벨업 스킬", "Level-Up Skills", "レベルアップスキル", "升级技能");
        C("pausebuild.special", "특수 스킬", "Special Skills", "特殊スキル", "特殊技能");
        C("pausebuild.relic", "유물", "Relics", "遺物", "遗物");
        C("pausebuild.transcend", "초월", "Transcendence", "超越", "超越");
        C("pausebuild.evolved", "진화", "EVO", "進化", "进化");
        C("pausebuild.empty", "아직 가진 스킬이 없습니다.", "You have no skills yet.", "まだスキルを持っていません。", "目前还没有技能。");
        C("pausebuild.hint.mouse", "아이콘에 마우스를 올리면 이름과 효과가 보입니다.", "Hover over an icon to see its name and effect.",
          "アイコンにマウスを合わせると名前と効果が表示されます。", "将鼠标悬停在图标上即可查看名称和效果。");
        C("pausebuild.hint.pad", "십자키로 아이콘을 옮겨 가며 이름과 효과를 볼 수 있습니다.", "Use the D-pad to move between icons and see their names and effects.",
          "十字キーでアイコンを移動しながら名前と効果を確認できます。", "使用十字键在图标之间移动，即可查看名称和效果。");
    }
}
