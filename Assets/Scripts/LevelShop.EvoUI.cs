using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 레벨업 창: 카드 위 진화 조합 미리보기 · 크게 보이는 건너뛰기 버튼
public partial class LevelShop
{
    static readonly Color EvoPurple = new Color(0.79f, 0.63f, 1f);
    static readonly Color EvoDone = new Color(0.55f, 1f, 0.55f);

    // ================================================================= 진화 미리보기 (카드 바로 위 띠)
    // [재료 아이콘 + Lv] [재료] (→) [진화 아이콘] 진화 이름 — 최대 단계인 재료는 초록, 이 카드는 금빛 테두리
    // 아이콘에 마우스를 올리면 설명 (재료: 그 스킬 카드 글 · 진화: 새 능력과 재료)
    class EvoRow
    {
        public RectTransform root;
        public Image[] parts = new Image[3];
        public Image[] frames = new Image[3];
        public int[] ids = new int[3];
        public TextMeshProUGUI[] levels = new TextMeshProUGUI[3];
        public Image evoIcon;
        public SkillEvo evo;
        public TextMeshProUGUI name;
    }
    readonly EvoRow[] evoRows = new EvoRow[3];

    void ShowEvoHints()
    {
        int[] ids = { first, second, third };
        for (int slot = 0; slot < 3; slot++)
        {
            Transform card = CardOf(slot);
            if (card == null) continue;
            EvoRow row = evoRows[slot] ??= BuildEvoRow(card);
            SkillEvo e = ids[slot] == SupplyId ? null : EvoUsing(ids[slot]);
            row.root.gameObject.SetActive(e != null);
            if (e == null) continue;
            for (int i = 0; i < 3; i++)
            {
                bool has = i < e.parts.Length;
                row.frames[i].gameObject.SetActive(has);
                if (!has) continue;
                int p = e.parts[i];
                row.ids[i] = p;
                row.parts[i].sprite = CardIcon(CharacterData.Selected, p);
                int max = MaxLevelOf(CharacterData.Selected, p);
                int lv = LevelOf(p);
                bool done = max > 0 && lv >= max;
                bool me = p == ids[slot];
                row.frames[i].color = me ? new Color(0.96f, 0.83f, 0.47f) : done ? EvoDone : new Color(0.35f, 0.3f, 0.45f);
                row.levels[i].text = (me ? lv + 1 : lv) + "/" + max;
                row.levels[i].color = done ? EvoDone : Color.white;
            }
            row.evo = e;
            row.evoIcon.sprite = Resources.Load<Sprite>("Icons/ability_" + e.icon);
            // 2.1.8: 이 카드를 고른 뒤 진화까지 남은 단계 (0이면 바로 진화)
            int left = 0;
            foreach (int p in e.parts)
            {
                int max = MaxLevelOf(CharacterData.Selected, p);
                left += Mathf.Max(0, max - LevelOf(p) - (p == ids[slot] ? 1 : 0));
            }
            string tag = left <= 0 ? "<color=#FFD966>" + Loc.T("바로 진화!") + "</color>" : Loc.T("{0}단계 남음").Replace("{0}", left.ToString());
            row.name.text = "<size=70%>" + Loc.T("진화") + " · " + tag + "</size>\n" + Loc.T(e.name);
        }
        ShowRecommend(ids);
        ShowSynergyTags(ids);         // 1.0.5: 가진 카드와 어울리는 카드에 「시너지」
    }

    // ================================================================= 추천 카드 (2.1.6~)
    // 세 장 중 진화에 가장 가까워지는 카드 (고르면 바로 진화하는 카드 먼저) 하나가 반짝반짝 빛남 (RecommendGlow)
    RecommendGlow recommend;
    int recommendedSlot = -1;

    void ShowRecommend(int[] ids)
    {
        int best = -1;
        float bestScore = 0f;
        for (int slot = 0; slot < 3; slot++)
        {
            if (ids[slot] == SupplyId) continue;
            float s = EvoProgressAfter(ids[slot]);
            if (s > bestScore) { bestScore = s; best = slot; }
        }
        recommendedSlot = best;
        if (recommend == null) recommend = gameObject.AddComponent<RecommendGlow>();
        recommend.Show(best >= 0 ? CardOf(best) as RectTransform : null, bestScore >= 1f);
    }

    EvoRow BuildEvoRow(Transform card)
    {
        UIKit.EnsureStyle();
        EvoRow row = new EvoRow();
        GameObject go = new GameObject("EvoHint", typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(card, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 0f);
        r.sizeDelta = new Vector2(420f, 70f);
        r.anchoredPosition = new Vector2(0f, 8f);
        Image bg = go.GetComponent<Image>();
        bg.sprite = UIKit.ButtonSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.32f, 0.2f, 0.48f, 0.95f);
        bg.raycastTarget = false;
        row.root = r;

        float x = -175f;
        for (int i = 0; i < 3; i++)
        {
            Image frame = Box(r, new Vector2(x + i * 54f, 4f), new Vector2(48f, 48f), Color.white);
            frame.sprite = UIKit.ButtonSprite;
            frame.type = Image.Type.Sliced;
            row.frames[i] = frame;
            // 마우스를 올리면 이 재료 스킬의 설명 (카드와 같은 글)
            frame.raycastTarget = true;
            int idx = i;
            TooltipTrigger tip = frame.gameObject.AddComponent<TooltipTrigger>();
            tip.titleProvider = () => CardName(CharacterData.Selected, row.ids[idx]);
            tip.bodyProvider = () => GetAbilityTooltip(row.ids[idx]);
            row.parts[i] = Box(frame.rectTransform, Vector2.zero, new Vector2(40f, 40f), Color.white);
            row.parts[i].preserveAspect = true;
            row.levels[i] = Label(frame.rectTransform, new Vector2(0f, -30f), new Vector2(60f, 22f), 17f);
        }
        TextMeshProUGUI arrow = Label(r, new Vector2(-4f, 4f), new Vector2(30f, 40f), 30f);
        arrow.text = "▶";
        arrow.color = EvoPurple;
        // 진화 결과 (마우스를 올리면 새 능력 · 재료)
        row.evoIcon = Box(r, new Vector2(38f, 4f), new Vector2(52f, 52f), Color.white);
        row.evoIcon.preserveAspect = true;
        row.evoIcon.raycastTarget = true;
        TooltipTrigger evoTip = row.evoIcon.gameObject.AddComponent<TooltipTrigger>();
        evoTip.titleProvider = () => row.evo != null ? Loc.T(row.evo.name) + "  ◆ " + Loc.T("진화") : "";
        evoTip.bodyProvider = () => row.evo != null ? EvoTooltip(row.evo, CharacterData.Selected) : "";
        row.name = Label(r, new Vector2(140f, 2f), new Vector2(150f, 64f), 21f);
        row.name.alignment = TextAlignmentOptions.Left;
        row.name.color = EvoPurple;
        row.name.enableAutoSizing = true;
        row.name.fontSizeMin = 13f;
        row.name.fontSizeMax = 21f;
        return row;
    }

    static Image Box(RectTransform parent, Vector2 pos, Vector2 size, Color c)
    {
        GameObject go = new GameObject("Img", typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        Image img = go.GetComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI Label(RectTransform parent, Vector2 pos, Vector2 size, float fontSize)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = size;
        r.anchoredPosition = pos;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (UIKit.Font != null) t.font = UIKit.Font;
        if (UIKit.FontMaterial != null) t.fontSharedMaterial = UIKit.FontMaterial;
        t.fontSize = fontSize;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.outlineWidth = 0.25f;
        t.outlineColor = new Color32(0, 0, 0, 255);
        t.raycastTarget = false;
        return t;
    }

    // ================================================================= 건너뛰기
    // 원하는 빌드를 지키려고 이번 레벨업을 고르지 않고 넘김 (크게 · 눈에 띄게, 카드 아래 가운데)
    const float SkipY = -478f;
    Button skipButton;
    TextMeshProUGUI skipText;

    void EnsureSkipButton()
    {
        if (skipButton != null || LvshopPanel == null) return;
        UIKit.EnsureStyle();
        skipButton = UIKit.MakeButton(LvshopPanel.transform, "", new Vector2(0f, SkipY), new Vector2(440f, 78f), SkipLevel, 30f);
        skipButton.name = "SkipButton";
        skipText = skipButton.GetComponentInChildren<TextMeshProUGUI>();
        skipText.text = Loc.T("건너뛰기") + "  ▶▶";
        skipText.fontStyle = FontStyles.Bold;
        Image img = skipButton.GetComponent<Image>();
        if (img != null) img.color = new Color(1f, 0.55f, 0.35f);
        TooltipTrigger tip = skipButton.gameObject.AddComponent<TooltipTrigger>();
        tip.title = Loc.T("건너뛰기");
        tip.body = Loc.T("이번 레벨업에서는 아무것도 배우지 않습니다. 원하는 스킬 · 진화를 노릴 때 쓰세요.");
    }

    void UpdateSkipButton()
    {
        if (skipButton == null) return;
        bool show = IsOpen;
        if (skipButton.gameObject.activeSelf != show) skipButton.gameObject.SetActive(show);
        if (!show) return;
        skipButton.interactable = selectReady;
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
        skipButton.transform.localScale = Vector3.one * (1f + 0.03f * pulse);
        if (skipText != null) skipText.color = Color.Lerp(Color.white, new Color(1f, 0.9f, 0.6f), pulse);
    }

    void SkipLevel()
    {
        if (!selectReady || !IsOpen) return;
        selectReady = false;
        pendingSlot = -1;
        freePick = false;
        RefreshCards();
        closeLevelShop();
        SpecialAbilities.SharedFx?.Play("whoosh", 0.5f, 1.3f);
    }
}
