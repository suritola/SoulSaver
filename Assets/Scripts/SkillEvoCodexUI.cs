using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 메인 메뉴 「스킬 진화」 도감: 캐릭터별로 어떤 레벨업 스킬이 합쳐져 무엇이 되는지
// 카드 한 장 = 재료 아이콘들 (+) → 진화 아이콘 · 진화 이름 · 새 능력 · 재료 이름
public static class SkillEvoCodexUI
{
    static GameObject open;
    static RectTransform grid;
    static ScrollRect scroll;
    static readonly List<Button> tabs = new List<Button>();
    static int tab;

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color Dim = new Color(0.72f, 0.68f, 0.76f);
    static readonly Color Purple = new Color(0.79f, 0.63f, 1f);

    // 탭: 거너 · 검사 · 도적 · 궁수 · 연금술사 · 공통
    // 탭: 만들어진 캐릭터마다 하나 + 마지막에 「모든 캐릭터」 (1.0.2: 새 캐릭터가 늘어 5명 고정에서 바꿈)
    static readonly List<CharacterId> heroes = new List<CharacterId>();
    static int CommonTab => heroes.Count;

    public static void Open(Transform root)
    {
        if (open != null) return;
        UIKit.EnsureStyle();
        RectTransform win = UIKit.Modal(root, "SkillEvoCodex", new Vector2(1640f, 960f), out open);
        TMP_Text title = UIKit.Text(win, "", 50f, Purple, new Vector2(0f, 425f), new Vector2(700f, 70f));
        title.text = Loc.T("스킬 진화 도감");
        UIKit.MakeButton(win, "닫기", new Vector2(720f, 425f), new Vector2(150f, 58f), Close, 22f);

        tabs.Clear();
        heroes.Clear();
        for (int i = 0; i < CharacterData.All.Length; i++)
            if (CharacterData.IsDeveloped((CharacterId)i)) heroes.Add((CharacterId)i);
        string[] names = new string[heroes.Count + 1];
        for (int i = 0; i < heroes.Count; i++) names[i] = Loc.T(CharacterData.Def(heroes[i]).name);
        names[CommonTab] = Loc.T("모든 캐릭터");
        float w = Mathf.Min(220f, (1560f - 12f * (names.Length - 1)) / names.Length);
        for (int i = 0; i < names.Length; i++)
        {
            int t = i;
            Button b = UIKit.MakeButton(win, "", new Vector2((i - (names.Length - 1) * 0.5f) * (w + 12f), 345f), new Vector2(w, 64f), () => Show(t), 26f);
            TMP_Text bt = b.GetComponentInChildren<TMP_Text>();
            bt.text = names[i];
            bt.enableAutoSizing = true;         // 캐릭터가 많으면 탭이 좁아짐
            bt.fontSizeMin = 12f;
            bt.fontSizeMax = 26f;
            tabs.Add(b);
        }
        TMP_Text hint = UIKit.Text(win, "", 21f, Dim, new Vector2(0f, 282f), new Vector2(1500f, 56f));
        hint.text = Loc.T("레벨업 카드 두세 장을 모두 최대 단계로 올리면 하나로 합쳐져 새 능력이 생깁니다. 레벨은 50 (무한 모드 100) 까지라 모두 올릴 수는 없습니다.");
        hint.enableWordWrapping = true;

        BuildScroll(win);
        Show(Mathf.Max(0, heroes.IndexOf(CharacterData.Selected)));
    }

    static void BuildScroll(RectTransform win)
    {
        RectTransform view = UIKit.Rect("Viewport", win, new Vector2(0f, -118f), new Vector2(1560f, 690f));
        view.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.15f);
        view.gameObject.AddComponent<RectMask2D>();
        grid = UIKit.Rect("Grid", view, Vector2.zero, new Vector2(1540f, 0f));
        grid.anchorMin = grid.anchorMax = grid.pivot = new Vector2(0.5f, 1f);
        grid.anchoredPosition = Vector2.zero;
        GridLayoutGroup g = grid.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(756f, 250f);
        g.spacing = new Vector2(16f, 14f);
        g.padding = new RectOffset(6, 6, 10, 10);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 2;
        grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.content = grid;
        scroll.viewport = view;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;
    }

    static void Show(int t)
    {
        tab = t;
        for (int i = grid.childCount - 1; i >= 0; i--) Object.Destroy(grid.GetChild(i).gameObject);
        for (int i = 0; i < tabs.Count; i++)
        {
            bool on = i == tab;
            tabs[i].GetComponent<Image>().color = on ? Purple : new Color(0.6f, 0.58f, 0.65f);
            tabs[i].transform.localScale = Vector3.one * (on ? 1.05f : 1f);
        }

        if (tab == CommonTab)
        {
            List<LevelShop.SkillEvo> all = LevelShop.EvosFor(CharacterId.Gunner);
            foreach (LevelShop.SkillEvo e in all) if (e.key.StartsWith("ce.")) Card(e, CharacterId.Gunner);
        }
        else
        {
            CharacterId who = heroes[Mathf.Clamp(tab, 0, heroes.Count - 1)];
            if (!CharacterData.IsUnlocked(who))
                LockedCard(who);
            else
                foreach (LevelShop.SkillEvo e in LevelShop.EvosFor(who))
                    if (!e.key.StartsWith("ce.")) Card(e, who);
        }
        scroll.verticalNormalizedPosition = 1f;
    }

    static void Card(LevelShop.SkillEvo e, CharacterId who)
    {
        RectTransform inner = Frame(out _);

        // 재료 (+) → 진화
        int n = e.parts.Length;
        float x0 = -320f;
        for (int i = 0; i < n; i++)
        {
            float x = x0 + i * 74f;
            int part = e.parts[i];
            Image f = Icon(inner, new Vector2(x, 54f), 64f, LevelShop.CardIcon(who, part), new Color(0.22f, 0.18f, 0.3f));
            Tip(f, LevelShop.CardName(who, part), LevelShop.CardInfo(who, part));
            if (i < n - 1)
            {
                TMP_Text plus = UIKit.Text(inner, "", 24f, Dim, new Vector2(x + 37f, 54f), new Vector2(20f, 30f));
                plus.text = "+";
            }
        }
        float arrowX = x0 + n * 74f - 30f;
        TMP_Text arrow = UIKit.Text(inner, "", 30f, Purple, new Vector2(arrowX, 54f), new Vector2(30f, 40f));
        arrow.text = "▶";
        Image evo = Icon(inner, new Vector2(arrowX + 50f, 54f), 76f, Resources.Load<Sprite>("Icons/ability_" + e.icon), new Color(0.45f, 0.32f, 0.62f));
        Tip(evo, Loc.T(e.name) + "  ◆ " + Loc.T("진화"), LevelShop.EvoTooltip(e, who));

        TMP_Text name = UIKit.Text(inner, "", 32f, Gold, new Vector2(arrowX + 100f + 160f, 70f), new Vector2(330f, 44f), TextAlignmentOptions.Left);
        name.text = Loc.T(e.name);
        name.enableAutoSizing = true;
        name.fontSizeMin = 18f;
        name.fontSizeMax = 32f;
        List<string> parts = new List<string>();
        foreach (int p in e.parts) parts.Add(LevelShop.CardName(who, p));
        TMP_Text recipe = UIKit.Text(inner, "", 19f, Purple, new Vector2(arrowX + 100f + 160f, 36f), new Vector2(330f, 30f), TextAlignmentOptions.Left);
        recipe.text = string.Join(" + ", parts);
        recipe.enableAutoSizing = true;
        recipe.fontSizeMin = 12f;
        recipe.fontSizeMax = 19f;

        TMP_Text body = UIKit.Text(inner, "", 22f, Parch, new Vector2(0f, -58f), new Vector2(700f, 118f), TextAlignmentOptions.TopLeft);
        body.text = Loc.T(e.desc);
        body.enableWordWrapping = true;
        body.enableAutoSizing = true;
        body.fontSizeMin = 14f;
        body.fontSizeMax = 22f;
    }

    static void LockedCard(CharacterId who)
    {
        RectTransform inner = Frame(out _);
        Icon(inner, new Vector2(-282f, 0f), 150f, CharacterUI.Portrait(who), Color.black);
        TMP_Text t = UIKit.Text(inner, "", 32f, Gold, new Vector2(90f, 60f), new Vector2(540f, 44f), TextAlignmentOptions.Left);
        t.text = "???  <size=70%>" + Loc.T("잠긴 캐릭터") + "</size>";
        TMP_Text b = UIKit.Text(inner, "", 22f, Parch, new Vector2(90f, -30f), new Vector2(540f, 120f), TextAlignmentOptions.TopLeft);
        b.text = Loc.T("포인트로 잠금을 풀면 이 캐릭터의 진화 조합을 볼 수 있습니다.");
        b.enableWordWrapping = true;
    }

    static RectTransform Frame(out Image bg)
    {
        RectTransform card = UIKit.Rect("Card", grid, Vector2.zero, new Vector2(756f, 250f));
        bg = card.gameObject.AddComponent<Image>();
        bg.sprite = UIKit.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.32f, 0.24f, 0.44f, 1f);
        RectTransform inner = UIKit.Rect("Inner", card, Vector2.zero, new Vector2(740f, 234f));
        inner.gameObject.AddComponent<Image>().color = new Color(0.11f, 0.09f, 0.16f, 0.97f);
        return inner;
    }

    // 마우스를 올리면 설명 (재료 · 진화 아이콘)
    static void Tip(Image target, string title, string body)
    {
        target.raycastTarget = true;
        TooltipTrigger t = target.gameObject.AddComponent<TooltipTrigger>();
        t.title = title;
        t.body = body;
    }

    static Image Icon(RectTransform parent, Vector2 pos, float size, Sprite sprite, Color frame)
    {
        RectTransform f = UIKit.Rect("IconFrame", parent, pos, new Vector2(size, size));
        Image fi = f.gameObject.AddComponent<Image>();
        fi.color = frame;
        if (sprite == null) return fi;
        RectTransform ir = UIKit.Rect("Icon", f, Vector2.zero, new Vector2(size - 8f, size - 8f));
        Image img = ir.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        if (frame == Color.black) img.color = Color.black;
        return fi;
    }

    public static void Close()
    {
        if (open != null) Object.Destroy(open);
        open = null;
    }
}
