using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 지옥의 문 특수 능력 선택: 뿌리에서 무기 / 패시브 / 스킬 세 갈래로 뻗는 스킬 트리
public class SpecialTreeUI : MonoBehaviour
{
    [Header("모양")]
    public Sprite nodeSprite;       // 능력 노드 틀
    public Sprite headerSprite;     // 갈래 이름판
    bool newStyle;                  // 1.4.8~ 이름판 그림이 새 도트 판인지 (작은 배지 · 번호는 테두리 반 두께 · 배지 바탕은 흰색 그대로)
    public Sprite panelSprite;      // 아래 설명창
    public Sprite rootIcon;
    public TMP_FontAsset font;
    public Material fontMaterial;

    [Header("포인트")]
    public int points = 3;              // 고를 수 있는 능력 수

    static readonly Color Gold = new Color(0.96f, 0.83f, 0.47f);
    static readonly Color Parch = new Color(0.92f, 0.88f, 0.80f);
    static readonly Color LineColor = new Color(0.78f, 0.55f, 0.25f, 0.8f);
    static readonly Color Selected = new Color(1f, 0.85f, 0.4f);
    static readonly Color Owned = new Color(0.6f, 0.85f, 1f);
    static readonly Color Maxed = new Color(0.55f, 0.5f, 0.58f);

    SpecialAbilities specials;
    System.Action<int[]> onConfirm;
    bool built;
    readonly List<int> picked = new List<int>();
    int viewing = -1;                   // 설명창에 보이는 노드

    readonly List<Image> nodeFrames = new List<Image>();
    readonly List<TextMeshProUGUI> nodeTags = new List<TextMeshProUGUI>();
    readonly List<GameObject> nodeBadges = new List<GameObject>();
    readonly List<TooltipTrigger> nodeTips = new List<TooltipTrigger>();
    // 고른 노드 표시: 두꺼운 금빛 테두리(맥박) + 고른 순서 번호
    readonly List<Image> nodePicks = new List<Image>();
    readonly List<GameObject> nodeNumbers = new List<GameObject>();
    readonly List<TextMeshProUGUI> nodeNumberTexts = new List<TextMeshProUGUI>();
    TextMeshProUGUI detailName, detailKind, detailText;
    Button confirm;
    TextMeshProUGUI confirmText;
    GameObject closeButton;

    // 게임 중 강화 모드: 특수 능력 포인트로 새 능력을 배우거나 가진 능력을 진화
    bool upgradeMode;
    int pointsNow;
    System.Action onCancel;

    // 지옥 입장 때: 포인트를 모두 써야 확정
    public void Open(SpecialAbilities specials, System.Action<int[]> onConfirm)
    {
        upgradeMode = false;
        pointsNow = points;
        onCancel = null;
        Show(specials, onConfirm);
    }

    // 중간 보스 보상: 포인트 안에서 원하는 만큼 쓰고, 닫아서 나중에 써도 됨
    public void OpenUpgrade(SpecialAbilities specials, int points, System.Action<int[]> onConfirm, System.Action onCancel)
    {
        upgradeMode = true;
        pointsNow = points;
        this.onCancel = onCancel;
        Show(specials, onConfirm);
    }

    void Show(SpecialAbilities specials, System.Action<int[]> onConfirm)
    {
        this.specials = specials;
        this.onConfirm = onConfirm;
        if (!built) Build();
        picked.Clear();
        closeButton.SetActive(upgradeMode);
        View(-1, null);
        gameObject.SetActive(true);
        introEnd = CardIntro.Play(introNodes, 0.045f);      // 1.4.1~ 노드 등장 애니메이션 — 다 나오기 전엔 고를 수 없음
    }

    readonly List<Transform> introNodes = new List<Transform>();
    float introEnd;

    bool CanConfirm => upgradeMode ? picked.Count > 0 : picked.Count >= pointsNow;

    // ================================================================= layout
    void Build()
    {
        built = true;
        RectTransform root = (RectTransform)transform;
        // 1.4.8~ 새 도트 스타일: 갈래 이름판 · 배지 · 버튼을 옛 Theme/ui_button 대신 금장 테두리 돌판(ui_button) · 못 찾으면 씬에 지정한 그림
        UIKit.EnsureStyle();
        if (RecommendGlow.PillArt) { headerSprite = UIKit.ButtonSprite; newStyle = true; }

        // 뿌리
        Vector2 rootPos = new Vector2(0f, 255f);
        Node(root, rootPos, 104f, rootIcon, Loc.T("지옥의 문"), -1);

        // 갈래 (x = 줄기 위치, 두 열의 x)
        var branches = new[]
        {
            (kind: SpecialKind.Weapon, name: Loc.T("무기  [Q] 교체"), x: -630f, cols: new[] { -760f, -500f }),
            (kind: SpecialKind.Passive, name: Loc.T("패시브  항상 적용"), x: 0f, cols: new[] { -130f, 130f }),
            (kind: SpecialKind.Skill, name: Loc.T("스킬  [{SKILL1}] 사용"), x: 630f, cols: new[] { 500f, 760f }),
        };
        float[] rows = { 60f, -55f, -170f, -285f };
        const float headerY = 160f;

        foreach (var br in branches)
        {
            // 뿌리 → 갈래 이름판
            Line(root, rootPos + new Vector2(0f, -52f), new Vector2(0f, 205f));
            Line(root, new Vector2(0f, 205f), new Vector2(br.x, 205f));
            Line(root, new Vector2(br.x, 205f), new Vector2(br.x, headerY + 28f));
            Header(root, new Vector2(br.x, headerY), br.name);

            List<int> ids = new List<int>();
            // 고른 캐릭터의 능력만 (CharacterData.pool)
            for (int i = 0; i < specials.abilities.Length; i++)
                if (specials.abilities[i] != null && specials.abilities[i].kind == br.kind && CharacterData.InPool(i)) ids.Add(i);

            int rowCount = (ids.Count + 1) / 2;
            // 줄기
            Line(root, new Vector2(br.x, headerY - 28f), new Vector2(br.x, rows[rowCount - 1]));

            for (int n = 0; n < ids.Count; n++)
            {
                float y = rows[n / 2];
                float x = br.cols[n % 2];
                Line(root, new Vector2(br.x, y), new Vector2(x, y));
                SpecialDef def = specials.abilities[ids[n]];
                Node(root, new Vector2(x, y), 88f, def.icon, Loc.T(def.name), ids[n]);
            }
        }

        BuildDetail(root);
    }

    void Line(RectTransform parent, Vector2 a, Vector2 b)
    {
        GameObject go = new GameObject("Line", typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.SetSiblingIndex(3);                       // 노드보다 뒤, 배경보다 앞
        Vector2 d = b - a;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(d.magnitude + 4f, 4f);
        r.anchoredPosition = (a + b) * 0.5f;
        r.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        Image img = go.GetComponent<Image>();
        img.color = LineColor;
        img.raycastTarget = false;
    }

    void Header(RectTransform parent, Vector2 pos, string text)
    {
        GameObject go = new GameObject("Branch", typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(250f, 56f);
        r.anchoredPosition = pos;
        Image img = go.GetComponent<Image>();
        img.sprite = headerSprite;
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        Text(r, text, 24f, Gold, Vector2.zero, new Vector2(240f, 50f), TextAlignmentOptions.Center);
    }

    void Node(RectTransform parent, Vector2 pos, float size, Sprite icon, string label, int id)
    {
        GameObject go = new GameObject(id < 0 ? "Root" : "Node_" + id, typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        introNodes.Add(r);              // 1.4.1~ 창이 열릴 때 등장 애니메이션
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(size, size);
        r.anchoredPosition = pos;
        Image frame = go.GetComponent<Image>();
        frame.sprite = nodeSprite;
        frame.type = Image.Type.Sliced;

        GameObject ic = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform ir = ic.GetComponent<RectTransform>();
        ir.SetParent(r, false);
        ir.sizeDelta = new Vector2(size - 28f, size - 28f);
        // 고른 노드의 금빛 테두리 (틀 위, 아이콘 아래)
        Image pickGlow = null;
        if (id >= 0)
        {
            GameObject pg = new GameObject("Picked", typeof(RectTransform), typeof(Image));
            RectTransform pr = pg.GetComponent<RectTransform>();
            pr.SetParent(r, false);
            pr.SetAsFirstSibling();
            pr.sizeDelta = new Vector2(size + 18f, size + 18f);
            pickGlow = pg.GetComponent<Image>();
            pickGlow.sprite = nodeSprite;
            pickGlow.type = Image.Type.Sliced;
            pickGlow.color = new Color(1f, 0.82f, 0.25f);
            pickGlow.raycastTarget = false;
            pg.SetActive(false);
        }
        Image iconImg = ic.GetComponent<Image>();
        iconImg.sprite = icon;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // 뿌리는 제목 배너와 이름이 같아서 글자를 따로 달지 않음
        if (id < 0)
        {
            frame.raycastTarget = false;
            return;
        }

        Text(r, label, 20f, Parch, new Vector2(0f, -size * 0.5f - 14f), new Vector2(230f, 28f), TextAlignmentOptions.Center);

        SpecialDef def = specials.abilities[id];
        Button b = go.AddComponent<Button>();
        ColorBlock cb = b.colors;
        cb.highlightedColor = new Color(1f, 0.9f, 0.62f);
        cb.pressedColor = new Color(0.72f, 0.62f, 0.48f);
        cb.selectedColor = Color.white;
        b.colors = cb;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        b.onClick.AddListener(() => Toggle(id));

        TooltipTrigger tip = go.AddComponent<TooltipTrigger>();
        tip.title = Loc.T(def.name) + "  · " + KindName(def.kind);
        tip.body = SpecialAbilities.AbilityText(id, def.description);

        // 노드 안쪽 아래의 작은 배지 (진화 가능 / 진화 완료) - 위아래 노드의 이름과 겹치지 않게 틀 안에 둠
        GameObject badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
        RectTransform br = badge.GetComponent<RectTransform>();
        br.SetParent(r, false);
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
        br.sizeDelta = new Vector2(84f, 22f);
        br.anchoredPosition = new Vector2(0f, -size * 0.5f + 14f);
        Image bimg = badge.GetComponent<Image>();
        bimg.sprite = headerSprite;
        bimg.type = Image.Type.Sliced;
        bimg.color = newStyle ? new Color(1f, 1f, 1f, 0.95f) : new Color(0.25f, 0.2f, 0.3f, 0.95f);
        if (newStyle) bimg.pixelsPerUnitMultiplier = 2f;
        bimg.raycastTarget = false;
        TextMeshProUGUI tag = Text(br, "", 14f, Owned, Vector2.zero, new Vector2(80f, 20f), TextAlignmentOptions.Center);
        badge.SetActive(false);

        // 오른쪽 위 고른 순서 번호
        GameObject num = new GameObject("PickNumber", typeof(RectTransform), typeof(Image));
        RectTransform nr = num.GetComponent<RectTransform>();
        nr.SetParent(r, false);
        nr.anchorMin = nr.anchorMax = new Vector2(1f, 1f);
        nr.sizeDelta = new Vector2(34f, 34f);
        nr.anchoredPosition = new Vector2(-2f, -2f);
        Image nimg = num.GetComponent<Image>();
        nimg.sprite = headerSprite;
        nimg.type = Image.Type.Sliced;
        nimg.color = new Color(0.85f, 0.45f, 0.05f);
        if (newStyle) nimg.pixelsPerUnitMultiplier = 2f;
        nimg.raycastTarget = false;
        TextMeshProUGUI ntext = Text(nr, "", 22f, Color.white, Vector2.zero, new Vector2(34f, 34f), TextAlignmentOptions.Center);
        num.SetActive(false);

        while (nodeFrames.Count <= id) { nodeFrames.Add(null); nodeTags.Add(null); nodeTips.Add(null); nodeBadges.Add(null); nodePicks.Add(null); nodeNumbers.Add(null); nodeNumberTexts.Add(null); }
        nodePicks[id] = pickGlow;
        nodeNumbers[id] = num;
        nodeNumberTexts[id] = ntext;
        nodeFrames[id] = frame;
        nodeTags[id] = tag;
        nodeTips[id] = tip;
        nodeBadges[id] = badge;
    }

    TextMeshProUGUI Text(RectTransform parent, string text, float size, Color color, Vector2 pos, Vector2 box, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = box;
        r.anchoredPosition = pos;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        if (fontMaterial != null) t.fontSharedMaterial = fontMaterial;
        t.fontSize = size;
        t.enableAutoSizing = true;
        t.fontSizeMin = 12f;
        t.fontSizeMax = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.text = text;
        return t;
    }

    void BuildDetail(RectTransform root)
    {
        GameObject go = new GameObject("Detail", typeof(RectTransform), typeof(Image));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(root, false);
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(1300f, 130f);
        r.anchoredPosition = new Vector2(0f, -420f);
        Image img = go.GetComponent<Image>();
        img.sprite = panelSprite;
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;

        detailName = Text(r, "", 34f, Gold, new Vector2(-470f, 20f), new Vector2(300f, 44f), TextAlignmentOptions.Left);
        detailKind = Text(r, "", 22f, new Color(1f, 0.72f, 0.55f), new Vector2(-470f, -24f), new Vector2(300f, 30f), TextAlignmentOptions.Left);
        detailText = Text(r, "", 24f, Parch, new Vector2(20f, 0f), new Vector2(660f, 100f), TextAlignmentOptions.Left);
        TermLinks.Apply(detailText);        // 1.4.8~ 용어에 마우스 → 정의 (글이 바뀔 때마다 다시 감쌈)

        GameObject btn = new GameObject("Confirm", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform brt = btn.GetComponent<RectTransform>();
        brt.SetParent(r, false);
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(230f, 76f);
        brt.anchoredPosition = new Vector2(510f, 0f);
        Image bimg = btn.GetComponent<Image>();
        bimg.sprite = headerSprite;
        bimg.type = Image.Type.Sliced;
        confirm = btn.GetComponent<Button>();
        ColorBlock cb = confirm.colors;
        cb.highlightedColor = new Color(1f, 0.9f, 0.62f);
        cb.disabledColor = new Color(0.45f, 0.42f, 0.48f, 0.8f);
        confirm.colors = cb;
        confirm.navigation = new Navigation { mode = Navigation.Mode.None };
        confirm.onClick.AddListener(() => { if (CanConfirm) onConfirm?.Invoke(picked.ToArray()); });
        confirmText = Text(brt, "", 26f, Gold, Vector2.zero, new Vector2(210f, 60f), TextAlignmentOptions.Center);

        // 강화 모드에서만: 포인트를 아껴 두고 닫기
        closeButton = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform crt = closeButton.GetComponent<RectTransform>();
        crt.SetParent(root, false);
        // 오른쪽 위 구석에 붙임 (화면이 16:9보다 좁으면 가운데 기준 좌표로는 잘림)
        crt.anchorMin = crt.anchorMax = Vector2.one;
        crt.sizeDelta = new Vector2(170f, 62f);
        crt.anchoredPosition = new Vector2(-120f, -70f);
        Image cimg = closeButton.GetComponent<Image>();
        cimg.sprite = headerSprite;
        cimg.type = Image.Type.Sliced;
        Button close = closeButton.GetComponent<Button>();
        ColorBlock ccb = close.colors;
        ccb.highlightedColor = new Color(1f, 0.9f, 0.62f);
        close.colors = ccb;
        close.navigation = new Navigation { mode = Navigation.Mode.None };
        close.onClick.AddListener(() => onCancel?.Invoke());
        Text(crt, Loc.T("닫기"), 24f, Parch, Vector2.zero, new Vector2(150f, 50f), TextAlignmentOptions.Center);
        closeButton.SetActive(false);
    }

    bool IsOwned(int id) => specials.Has(id);
    bool IsMaxed(int id) => IsOwned(id) && specials.IsEvolved(id);

    // 새로 고른 스킬까지 합쳐 스킬 칸(E·F·Space)이 가득 찼는지
    bool SkillSlotsFull()
    {
        int count = specials.SkillCount;
        foreach (int p in picked)
            if (!specials.Has(p) && specials.abilities[p].kind == SpecialKind.Skill) count++;
        return count >= SpecialAbilities.MaxSkills;
    }

    // 노드를 누르면 선택/해제 (포인트 안에서)
    void Toggle(int id)
    {
        if (Time.unscaledTime < introEnd) return;      // 1.4.1~ 등장 애니메이션 중엔 고르지 않음
        // 누른 노드에 포커스가 남으면 Space · Enter 로 다시 눌리므로 바로 풂
        if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        string note = null;
        if (picked.Contains(id))
        {
            picked.Remove(id);
            note = Loc.T("선택을 취소했습니다.");
        }
        else if (IsMaxed(id))
        {
            note = Loc.T("이미 진화한 능력입니다.");
        }
        else if (upgradeMode && !specials.Has(id) && specials.abilities[id].kind == SpecialKind.Skill && SkillSlotsFull())
        {
            note = Loc.T("스킬 칸({SKILL1} · {SKILL2} · {SKILL3})이 가득 찼습니다. 가진 스킬을 진화시켜 보세요.");
        }
        else if (picked.Count < pointsNow)
        {
            picked.Add(id);
        }
        else
        {
            note = Loc.T("포인트를 모두 썼습니다. 다른 능력을 먼저 취소하세요.");
        }
        View(id, note);
    }

    void View(int id, string note)
    {
        viewing = id;
        for (int i = 0; i < nodeFrames.Count; i++)
        {
            if (nodeFrames[i] == null) continue;
            bool on = picked.Contains(i);
            // 하나라도 골랐으면 안 고른 노드는 더 어둡게
            Color idle = picked.Count > 0 ? new Color(0.55f, 0.52f, 0.58f) : new Color(0.8f, 0.78f, 0.82f);
            nodeFrames[i].color = on ? Selected : IsMaxed(i) ? Maxed : IsOwned(i) ? Owned : idle;
            nodeFrames[i].transform.localScale = Vector3.one * (on ? 1.18f : 1f);
            if (nodePicks[i] != null) nodePicks[i].gameObject.SetActive(on);
            nodeNumbers[i].SetActive(on);
            nodeNumberTexts[i].text = on ? (picked.IndexOf(i) + 1).ToString() : "";
            nodeTags[i].text = IsMaxed(i) ? Loc.T("진화 완료") : IsOwned(i) ? (on ? Loc.T("진화!") : Loc.T("진화 가능")) : on ? Loc.T("선택됨") : "";
            nodeBadges[i].SetActive(nodeTags[i].text.Length > 0);
            // 1.4.8~ 새 도트 판이면 평소 흰색 그대로 · 고른 것만 금빛 (예전엔 판을 어둡게 칠함)
            nodeBadges[i].GetComponent<Image>().color = newStyle
                ? (on ? new Color(1f, 0.78f, 0.38f, 0.98f) : new Color(1f, 1f, 1f, 0.95f))
                : (on ? new Color(0.55f, 0.35f, 0.05f, 0.98f) : new Color(0.25f, 0.2f, 0.3f, 0.95f));
            nodeTags[i].color = on ? new Color(1f, 0.95f, 0.7f) : IsMaxed(i) ? new Color(0.75f, 0.72f, 0.78f) : Owned;
            nodeTips[i].body = SpecialAbilities.AbilityText(i, specials.abilities[i].description)
                + (IsOwned(i) && !IsMaxed(i) ? "\n\n<color=#F5D478>" + Loc.T("한 번 더 고르면 진화합니다.") + "</color>" : IsMaxed(i) ? "\n\n<color=#A89C86>" + Loc.T("이미 진화한 능력입니다.") + "</color>" : "");
        }

        int left = pointsNow - picked.Count;
        bool ready = CanConfirm;
        confirm.interactable = ready;
        confirmText.color = ready ? Gold : new Color(0.6f, 0.56f, 0.62f);
        confirmText.text = upgradeMode
            ? (ready ? Loc.T("강화 완료") : Loc.T("포인트 ") + left)
            : (ready ? Loc.T("선택 완료") : Loc.T("남은 포인트 ") + left);

        if (id < 0)
        {
            if (upgradeMode)
            {
                detailName.text = Loc.T("특수 능력 포인트 ") + pointsNow;
                detailKind.text = Loc.T("새 능력을 배우거나 가진 능력을 진화");
                detailText.text = Loc.T("파란 테두리는 이미 가진 능력입니다. 한 번 더 고르면 진화해서 더 강해집니다. 남은 포인트는 아껴 두었다가 나중에 써도 됩니다.");
            }
            else
            {
                detailName.text = Loc.T("능력 ") + pointsNow + Loc.T("개를 고르세요");
                detailKind.text = Loc.T("포인트 ") + pointsNow + Loc.T("개 · 노드를 눌러 선택");
                detailText.text = Loc.T("무기는 {SWAP}로 기본 권총과 번갈아 쓰고, 스킬은 고른 순서대로 {SKILL1} · {SKILL2} · {SKILL3}에 배정됩니다. 다시 누르면 선택이 취소됩니다.");
            }
            return;
        }

        SpecialDef def = specials.abilities[id];
        bool evolving = IsOwned(id) && !IsMaxed(id);
        detailName.text = Loc.T(def.name) + (evolving ? Loc.T(" → 진화") : "");
        detailKind.text = KindName(def.kind) + (picked.Contains(id) ? Loc.T("  (선택됨)") : IsMaxed(id) ? Loc.T("  (진화 완료)") : "");
        string body = SpecialAbilities.AbilityText(id, def.description);
        detailText.text = note != null ? body + "\n<color=#ff9d8a>" + note + "</color>" : body;
    }

    // 고른 노드의 금빛 테두리가 맥박침 (창이 열려 있는 동안은 시간이 멈춰 있어서 unscaledTime)
    void Update()
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
        foreach (Image p in nodePicks)
        {
            if (p == null || !p.gameObject.activeSelf) continue;
            p.color = Color.Lerp(new Color(1f, 0.7f, 0.15f), new Color(1f, 0.95f, 0.6f), pulse);
            p.transform.localScale = Vector3.one * (1f + 0.05f * pulse);
        }
    }

    static string KindName(SpecialKind k) => k == SpecialKind.Weapon ? Loc.T("무기 · {SWAP}로 교체") : k == SpecialKind.Skill ? Loc.T("스킬 · {SKILL1}/{SKILL2}/{SKILL3}") : Loc.T("패시브 · 항상 적용");
}
