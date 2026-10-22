using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 레벨업 창: 카드 위 진화 조합 미리보기 · 크게 보이는 건너뛰기 버튼
public partial class LevelShop
{
    static readonly Color EvoPurple = new Color(0.79f, 0.63f, 1f);
    static readonly Color EvoDone = new Color(0.55f, 1f, 0.55f);
    // 1.4.8~ 진화 띠 바탕: 새 도트 판(금장 돌판)이면 흰색 그대로 · 가까우면 금빛으로 숨 쉼 (예전 보라 ↔ 금 칠은 판 그림이 없을 때만)
    static Color EvoRowBase => RecommendGlow.PillArt ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.32f, 0.2f, 0.48f, 0.95f);
    static Color EvoRowNear => RecommendGlow.PillArt ? new Color(1f, 0.8f, 0.42f, 0.98f) : new Color(0.62f, 0.45f, 0.18f, 0.98f);

    // ================================================================= 진화 미리보기 (카드 바로 위 띠)
    // [재료 아이콘 + 단계 칸] [재료] (→) [진화 아이콘] 진화 이름 — 최대 단계인 재료는 초록, 이 카드는 금빛 테두리
    // 아이콘에 마우스를 올리면 설명 (재료: 그 스킬 카드 글 · 진화: 새 능력과 재료)
    class EvoRow
    {
        public RectTransform root;
        public Image[] parts = new Image[3];
        public Image[] frames = new Image[3];
        public int[] ids = new int[3];
        public LevelPips[] levels = new LevelPips[3];       // 1.1.2~ 「0→1/5」 글자 대신 재료 아이콘 아래 단계 칸
        public Image evoIcon;
        public SkillEvo evo;
        public TextMeshProUGUI name;
        public bool near;           // 1.1.2~ 이 카드를 고르면 진화하거나 한 단계만 남음 → 띠가 숨 쉬듯 빛남 (EvoRowPulse)
        public Image bg;
        public TextMeshProUGUI left;        // UI 개편 P1 (Legacy 끔): 진화 칸 오른쪽 위 「n단계 남음」 · 「바로 진화!」
    }
    readonly EvoRow[] evoRows = new EvoRow[3];

    void ShowEvoHints()
    {
        int[] ids = { first, second, third };
        bool anyEvo = false;
        for (int slot = 0; slot < 3; slot++)
        {
            Transform card = CardOf(slot);
            if (card == null) continue;
            EvoRow row = evoRows[slot] ??= BuildEvoRow(card);
            SkillEvo e = ids[slot] == SupplyId ? null : EvoUsing(ids[slot]);
            row.root.gameObject.SetActive(e != null);
            if (!UiRules.Legacy)
            {
                // 진화 칸이 있으면 설명 글 아래끝을 칸 위로 (없으면 원래 자리)
                TextMeshProUGUI body = slot == 0 ? FirstAbility : slot == 1 ? SecondAbility : ThirdAbility;
                if (body != null) body.rectTransform.offsetMin = new Vector2(CardBodySide, e != null ? EvoSectionPad + EvoSectionH + UiTokens.S2 : V2BodyBottom);
            }
            if (e == null) continue;
            anyEvo = true;
            bool blockedAny = false;
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
                // 1.0.4~ (거너): 지금 무기에 맞지 않아 뽑히지 않는 재료는 회색
                bool blocked = !done && !me && !KitFits(p);
                blockedAny |= blocked;
                row.frames[i].color = !UiRules.Legacy ? (me ? UiTokens.Accent : done ? UiTokens.GaugeSoul : blocked ? new Color(0.5f, 0.5f, 0.5f) : Color.white)
                                    : me ? new Color(0.96f, 0.83f, 0.47f) : done ? EvoDone : blocked ? new Color(0.22f, 0.22f, 0.24f) : new Color(0.35f, 0.3f, 0.45f);
                row.parts[i].color = blocked ? new Color(0.45f, 0.45f, 0.45f, 0.6f) : Color.white;
                // 1.1.2~: 「지금→고른 뒤/최대」 숫자(「0→1/5」) 대신 단계 칸 — 찬 칸 · 이 카드면 차오를 칸이 금빛으로 맥동 · 최대면 전부 금빛
                //         정확한 숫자는 아이콘에 마우스를 올린 설명 끝 줄(「Lv 2 · 최대 3」, GetAbilityTooltip)
                row.levels[i].Set(lv, max, me ? 1 : 0);
                row.levels[i].Dim(blocked);
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
            // 1.1.2~ 하나 남으면 「진화까지 1단계!」를 금빛으로 (기대감) · 바로 진화는 더 크게
            string tag = left <= 0 ? "<size=125%><color=#FFD966>" + Loc.T("바로 진화!") + "</color></size>"
                       : left == 1 ? "<color=#FFE08A>" + Loc.T("진화까지 1단계!") + "</color>"
                       : Loc.T("{0}단계 남음").Replace("{0}", left.ToString());
            if (blockedAny && left > 0) tag = "<color=#A8A8A8>" + Loc.T("지금 무기로는 안 나옴") + "</color>";       // 1.0.4~
            // 1.1~ 앞의 「진화 · 」를 뺌 (칸이 150 이라 영어 · 일본어에서 글자가 9~11px 까지 작아졌음, 보라 띠 · ▶ 가 이미 진화를 뜻함)
            if (!UiRules.Legacy && row.left != null)
            {
                // 새 모습: 이름 · 남은 단계를 따로 (색 태그 없이 · 바로 진화만 보상색 청동, 1단계면 청록)
                row.name.text = Loc.T(e.name);
                row.left.text = blockedAny && left > 0 ? Loc.T("지금 무기로는 안 나옴") : left <= 0 ? Loc.T("바로 진화!") : left == 1 ? Loc.T("진화까지 1단계!") : Loc.T("{0}단계 남음").Replace("{0}", left.ToString());
                row.left.color = blockedAny && left > 0 ? UiTokens.Text3 : left <= 0 ? UiTokens.Bronze : left == 1 ? UiTokens.Accent : UiTokens.Text3;
            }
            else row.name.text = "<size=70%>" + tag + "</size>\n" + Loc.T(e.name);
            row.near = !blockedAny && left <= 1;
        }
        ShowEvoIntro(anyEvo);
        ShowRecommend(ids);
        ShowSynergyTags(ids);         // 1.0.1: 가진 카드와 어울리는 카드에 「시너지」
        ShowCardPips(ids);            // 1.1.2~: 카드 맨 위 단계 칸 (LevelShop.Pips)
    }

    // ================================================================= 스킬 진화 첫 안내 (1.0.4~)
    // 진화 띠가 처음 나온 레벨업 창에서 한 번만, 창 위쪽에 한 줄 (게임 전체에서 한 번, 저장)
    const string EvoIntroKey = "intro.evoband";
    TextMeshProUGUI evoIntroText;
    bool evoIntroShowing;

    void ShowEvoIntro(bool anyEvo)
    {
        if (!evoIntroShowing && anyEvo && !TutorialRun.Active && !GameInput.Auto && Prefs.GetInt(EvoIntroKey, 0) == 0)
        {
            evoIntroShowing = true;
            Prefs.SetInt(EvoIntroKey, 1);
        }
        if (evoIntroText == null)
        {
            if (!evoIntroShowing || LvshopPanel == null) return;
            UIKit.EnsureStyle();
            // 「능력을 하나 선택하세요」 띠(380, 0.66배) 바로 위
            evoIntroText = Label((RectTransform)LvshopPanel.transform, new Vector2(0f, 448f), new Vector2(1500f, 44f), 28f);
            evoIntroText.name = "EvoIntro";
            evoIntroText.color = EvoPurple;
            evoIntroText.enableAutoSizing = true;
            evoIntroText.fontSizeMin = 16f;
            evoIntroText.fontSizeMax = 28f;
        }
        evoIntroText.gameObject.SetActive(evoIntroShowing);
        if (evoIntroShowing)
            evoIntroText.text = "<color=#FFE066>" + Loc.T("도움말") + "</color>  " + Loc.T("카드 위 보라 띠는 스킬 진화입니다. 띠의 스킬을 모두 최대 단계로 올리면 하나로 합쳐져 새 능력이 생깁니다.");
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
            float s = Mathf.Max(EvoProgressAfter(ids[slot]), PathProgressAfter(ids[slot]));        // 갈래 규칙: 공명 · 완성에 닿게 하는 카드도 (LevelShop.Paths)
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
        bg.color = EvoRowBase;
        bg.raycastTarget = false;
        row.root = r;
        row.bg = bg;

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
            // 아이콘 바로 아래 단계 칸 (띠 아래끝 안쪽, 폭 = 아이콘 칸)
            row.levels[i] = LevelPips.Create(frame.rectTransform, new Vector2(0f, -31f), new Vector2(48f, 9f), "Pips");
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
        if (!UiRules.Legacy) LayoutEvoRowV2(row, card);
        return row;
    }

    // UI 개편 P1 (Legacy 끔): 진화 띠를 카드 위에 뜬 판 대신 카드 안 아래 「진화」 칸으로 (사용자 「진화 띠가 이상」 · docs/art/ui-v2-levelup.png)
    //   위 1도트 선 · 「진화 · 이름」 + 오른쪽 남은 단계 · 아래 줄 재료 셋(홈 칸 + 단계 칸) ▶ 진화 결과 — 띠 판 · 숨쉬기 없음, 설명 글 아래끝은 ShowEvoHints 가 올림
    public const float EvoSectionH = 120f, EvoSectionPad = 24f;
    static void LayoutEvoRowV2(EvoRow row, Transform card)
    {
        RectTransform r = row.root;
        float cw = card is RectTransform crt ? crt.rect.width : 420f;
        if (cw < 200f) cw = 420f;
        float w = cw - EvoSectionPad * 2f;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
        r.pivot = new Vector2(0.5f, 0f);
        r.sizeDelta = new Vector2(w, EvoSectionH);
        r.anchoredPosition = new Vector2(0f, EvoSectionPad);
        row.bg.enabled = false;
        UiV2.Line(r, "EvoLine", new Vector2(0f, EvoSectionH * 0.5f - UiRules.Dot * 0.5f), w);
        Image mark = UiV2.Line(r, "EvoLineMark", new Vector2(-w * 0.5f + 24f, EvoSectionH * 0.5f - UiRules.Dot * 0.5f), 48f);
        mark.sprite = null;
        mark.color = UiTokens.GaugeSoul;
        TextMeshProUGUI head = Label(r, new Vector2(-w * 0.5f + 30f, 28f), new Vector2(60f, 30f), UiTokens.Small);
        head.text = Loc.T("진화");
        head.color = UiTokens.GaugeSoul;
        head.alignment = TextAlignmentOptions.Left;
        UIKit.Remember(head, "진화");
        RectTransform nr = row.name.rectTransform;
        nr.anchoredPosition = new Vector2(-w * 0.5f + 66f + (w * 0.55f) * 0.5f, 28f);
        nr.sizeDelta = new Vector2(w * 0.55f, 32f);
        row.name.alignment = TextAlignmentOptions.Left;
        row.name.color = UiTokens.Text1;
        row.name.fontSizeMax = UiTokens.Body;
        row.name.fontSizeMin = UiTokens.Small;
        row.name.enableWordWrapping = false;
        row.name.overflowMode = TextOverflowModes.Ellipsis;
        row.left = Label(r, new Vector2(w * 0.5f - (w * 0.3f) * 0.5f, 28f), new Vector2(w * 0.3f, 30f), UiTokens.Small);
        row.left.alignment = TextAlignmentOptions.Right;
        row.left.enableAutoSizing = true;
        row.left.fontSizeMin = 16f;
        row.left.fontSizeMax = UiTokens.Small;
        Sprite inset = UiRules.Sprite("ui2_inset");
        for (int i = 0; i < 3; i++)
        {
            RectTransform fr = row.frames[i].rectTransform;
            fr.anchoredPosition = new Vector2(-w * 0.5f + 30f + i * 66f, -18f);
            fr.sizeDelta = new Vector2(54f, 54f);
            if (inset != null) { row.frames[i].sprite = inset; row.frames[i].type = Image.Type.Tiled; row.frames[i].pixelsPerUnitMultiplier = 1f; }
            row.parts[i].rectTransform.sizeDelta = new Vector2(48f, 48f);           // 32 · 64 그림 → 48 (1.5 · 0.75 배 — 칸이 작아 정수 배는 P2 아이콘 정리 때)
            ((RectTransform)row.levels[i].transform).anchoredPosition = new Vector2(0f, -36f);
        }
        foreach (TextMeshProUGUI t in r.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (t.text == "▶") { t.color = UiTokens.Text3; t.rectTransform.anchoredPosition = new Vector2(-w * 0.5f + 30f + 3 * 66f - 12f, -18f); t.fontSize = t.fontSizeMax = UiTokens.Small; }
        RectTransform er = row.evoIcon.rectTransform;
        er.anchoredPosition = new Vector2(-w * 0.5f + 30f + 3 * 66f + 30f, -18f);
        er.sizeDelta = new Vector2(54f, 54f);
    }

    // 1.1.2~ 진화가 가까운 카드의 띠: 보라 ↔ 금빛으로 숨 쉬고 진화 아이콘이 살짝 커졌다 작아짐 (게임이 멈춰 있어 실제 시간)
    void EvoRowPulse()
    {
        // UI 개편(Legacy 끔): 숨쉬기 · 아이콘 들썩임 없이 가까우면 진한 색으로 고정 (가까움은 「1단계 남음!」 글로도 보임)
        float k = UiRules.Legacy ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f) : 1f;
        foreach (EvoRow row in evoRows)
        {
            if (row == null || row.root == null || !row.root.gameObject.activeSelf) continue;
            row.bg.color = row.near ? Color.Lerp(EvoRowBase, EvoRowNear, k) : EvoRowBase;
            row.evoIcon.rectTransform.localScale = Vector3.one * (row.near && UiRules.Legacy ? 1f + 0.12f * k : 1f);
        }
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
    const float SkipY = -486f;          // 1.1~ -478 → -486: 확정 안내 줄(-426)을 내린 만큼
    Button skipButton;
    TextMeshProUGUI skipText;

    void EnsureSkipButton()
    {
        if (skipButton != null || LvshopPanel == null) return;
        UIKit.EnsureStyle();
        skipButton = UIKit.MakeButton(LvshopPanel.transform, "", new Vector2(0f, SkipY), new Vector2(440f, 78f), SkipLevel, 30f);
        skipButton.name = "SkipButton";
        skipText = skipButton.GetComponentInChildren<TextMeshProUGUI>();
        skipText.fontStyle = FontStyles.Bold;
        skipText.enableAutoSizing = true;           // 1.0.4~: 번역이 길어도 버튼 안에
        skipText.fontSizeMin = 18f;
        skipText.fontSizeMax = 30f;
        Image img = skipButton.GetComponent<Image>();
        if (img != null) img.color = new Color(1f, 0.55f, 0.35f);
        TooltipTrigger tip = skipButton.gameObject.AddComponent<TooltipTrigger>();
        tip.title = Loc.T("이번 레벨업 포기");
        tip.body = Loc.T("이번 레벨업에서는 아무것도 배우지 않습니다. 원하는 스킬 · 진화를 노릴 때 쓰세요.") + "\n" + Loc.T("실수로 누르지 않게 두 번 눌러야 포기됩니다.");
        DisarmSkip();
    }

    // 1.0.4~: 「다음」 버튼인 줄 알고 눌러 레벨업이 사라지던 문제 → 두 번 눌러야 포기 (2초 안에), 문구도 「이번 레벨업 포기」
    const float SkipConfirmTime = 2f;
    float skipArmedAt = -99f;
    bool SkipArmed => Time.unscaledTime - skipArmedAt < SkipConfirmTime;

    void DisarmSkip()
    {
        skipArmedAt = -99f;
        if (skipText != null) skipText.text = Loc.T("이번 레벨업 포기") + "  ▶▶";
    }

    void UpdateSkipButton()
    {
        if (IsOpen) EvoRowPulse();
        if (skipButton == null) return;
        bool show = IsOpen && !freePick && !PathWindowShown;        // 1.0.4~: 판 시작 무료 카드 창에는 없음 (레벨이 오르지 않는 공짜 카드) · 입문 · 갈림길 창에도 없음 (건너뛰면 갈래가 비어 버림)
        if (skipButton.gameObject.activeSelf != show) skipButton.gameObject.SetActive(show);
        if (!show) return;
        skipButton.interactable = selectReady;
        if (skipArmedAt > 0f && !SkipArmed) DisarmSkip();
        float pulse = UiRules.Legacy ? (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f)) * GameSettings.Motion : 0f;      // 1.1.9~ 「움직임 줄이기」 · UI 개편: 두근거림 없음
        skipButton.transform.localScale = Vector3.one * (1f + 0.03f * pulse);
        if (skipText != null) skipText.color = SkipArmed ? Color.Lerp(new Color(1f, 0.45f, 0.4f), Color.white, pulse)
                                                         : Color.Lerp(Color.white, new Color(1f, 0.9f, 0.6f), pulse);
    }

    void SkipLevel()
    {
        if (!selectReady || !IsOpen || freePick || PathWindowShown) return;
        if (!SkipArmed)
        {
            skipArmedAt = Time.unscaledTime;
            if (skipText != null) skipText.text = Loc.T("한 번 더 누르면 포기");
            SpecialAbilities.SharedFx?.Play("ding", 0.4f, 0.8f);
            return;
        }
        DisarmSkip();
        selectReady = false;
        pendingSlot = -1;
        freePick = false;
        RefreshCards();
        closeLevelShop();
        SpecialAbilities.SharedFx?.Play("whoosh", 0.5f, 1.3f);
    }
}
