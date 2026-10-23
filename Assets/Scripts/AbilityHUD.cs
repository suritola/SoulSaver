using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 레벨업으로 얻은 능력을 아이콘 + Lv 로 보여주는 HUD
public class AbilityHUD : MonoBehaviour
{
    [Header("능력 아이콘 (LevelShop 능력 순서와 같게)")]
    public Sprite[] icons = new Sprite[10];

    [Header("칸 모양")]
    public Sprite slotSprite;
    public TMP_FontAsset font;
    public Material fontMaterial;
    public Vector2 slotSize = new Vector2(76f, 76f);
    public float spacing = 8f;
    public int columns = 6;

    class Slot
    {
        public RectTransform rect;
        public TextMeshProUGUI label;
        public int level;
        public int id;
        public LevelPips pips;             // 1.1.2~ 최대 단계가 있는 스킬은 「Lv.2」 대신 단계 칸 (칸 아래끝)
        public TextMeshProUGUI badge;      // 쌓이는 스킬의 지금 상태 (검무 x3 · 총열 과열 +30% …)
        public string shown;
    }

    LevelShop levelShop;

    readonly Dictionary<int, Slot> slots = new Dictionary<int, Slot>();
    // 1.1~: 화면에 놓인 순서 (Dictionary 는 지운 자리를 다시 써서 열거 순서가 바뀜 → 진화를 두 번 하면 칸들이 다른 자리로 튀었음)
    readonly List<int> order = new List<int>();

    public Sprite GetIcon(int id)
    {
        if (id >= 1000)
        {
            LevelShop.SkillEvo e = Shop != null ? Shop.EvoById(id) : null;
            return e != null ? Resources.Load<Sprite>("Icons/ability_" + e.icon) : null;
        }
        Sprite kit = LevelShop.KitIcon(id);
        if (kit != null) return kit;
        if (id == 5) return Resources.Load<Sprite>("Icons/ability_104");     // 코인 자석 (예전 그림이 핏방울처럼 보여서)
        if (icons == null || id < 0 || id >= icons.Length) return null;
        return icons[id];
    }

    // 접기 · 펼치기 (아이콘이 화면을 많이 가려서): 왼쪽 위 작은 단추, 상태는 저장
    const string OpenKey = "AbilityHUDOpen";
    const float ToggleWidth = 36f;
    bool open = true;
    TextMeshProUGUI toggleText;
    RectTransform toggleRect;
    const float ClosedWidth = 230f;     // 1.0.1: 접혀 있으면 숫자만 보여 뭔지 몰랐음 → 「▶ 레벨업 스킬 12」

    void Start()
    {
        open = Prefs.GetInt(OpenKey, 1) == 1;
        UiScaler.Isolate(gameObject);                   // 1.1.9: 칸 크기 · 스택 배지 변경이 HUD 전체를 다시 묶지 않게
        BuildToggle();
    }

    void BuildToggle()
    {
        GameObject go = new GameObject("AbilityToggle", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform r = go.GetComponent<RectTransform>();
        r.SetParent(transform, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
        r.sizeDelta = new Vector2(ToggleWidth, slotSize.y);
        toggleRect = r;
        r.anchoredPosition = Vector2.zero;
        Image bg = go.GetComponent<Image>();
        bg.sprite = slotSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 1f, 1f, 0.85f);
        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(Toggle);
        TooltipTrigger tip = go.AddComponent<TooltipTrigger>();
        tip.titleProvider = () => Loc.T("레벨업 스킬");          // 1.5.6~ 언어를 바꾸면 다시 (보여 줄 때 번역)
        tip.bodyProvider = () => Loc.T("눌러서 레벨업 스킬 아이콘을 접고 펼칩니다.");

        GameObject tg = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform tr = tg.GetComponent<RectTransform>();
        tr.SetParent(r, false);
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        toggleText = tg.GetComponent<TextMeshProUGUI>();
        if (font != null) toggleText.font = font;
        if (fontMaterial != null) toggleText.fontSharedMaterial = fontMaterial;
        toggleText.fontSize = 28f;
        toggleText.color = new Color(0.96f, 0.83f, 0.47f);
        toggleText.alignment = TextAlignmentOptions.Center;
        toggleText.enableAutoSizing = true;
        toggleText.fontSizeMin = 14f;
        toggleText.fontSizeMax = 28f;
        toggleText.enableWordWrapping = false;
        toggleText.raycastTarget = false;
        tg.AddComponent<LocRefresh>().refresh = ApplyOpen;     // 1.5.6~ 언어를 바꾸면 다시 (접힌 단추 「▶ 레벨업 스킬  n」)
        ApplyOpen();
    }

    void Toggle()
    {
        open = !open;
        Prefs.SetInt(OpenKey, open ? 1 : 0);
        if (UnityEngine.EventSystems.EventSystem.current != null) UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        ApplyOpen();
    }

    void ApplyOpen()
    {
        foreach (Slot s in slots.Values) s.rect.gameObject.SetActive(open);
        // 접혀 있으면 단추를 넓혀 이름과 가진 스킬 수를 함께 보여 줌
        if (toggleRect != null) toggleRect.sizeDelta = new Vector2(open ? ToggleWidth : ClosedWidth, slotSize.y);
        if (toggleText == null) return;
        toggleText.fontSizeMax = open ? 28f : 22f;
        toggleText.text = open ? "<" : "▶ " + Loc.T("레벨업 스킬") + (slots.Count > 0 ? "  " + slots.Count : "");
    }

    public void SetAbility(int id, int level)
    {
        // 2026-10-19~ 외길로 진화 재료를 한 단계 더 올려도 진화 칸에 합쳐진 재료 칸을 다시 만들지 않음
        if (merged.Contains(id)) return;
        if (!slots.TryGetValue(id, out Slot slot))
        {
            slot = CreateSlot(id, slots.Count);
            slots.Add(id, slot);
            order.Add(id);
            ApplyOpen();
        }

        slot.level = level;
        slot.label.text = "Lv." + level;
        // 1.1.2~ 최대 단계가 있으면 단계 칸 (몇 단계 중 몇인지 한눈에, 최대면 금빛 + MAX) · 없으면 예전처럼 「Lv.n」
        int max = Shop != null ? Shop.CapOf(id) : LevelShop.MaxLevelOf(CharacterData.Selected, id);      // 외길이면 늘어난 단계까지
        if (max > 0)
        {
            if (slot.pips == null)
            {
                slot.pips = LevelPips.Create(slot.rect, Vector2.zero, new Vector2(slotSize.x - 14f, 9f), "Pips");
                RectTransform pr = (RectTransform)slot.pips.transform;
                pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0f);
                pr.anchoredPosition = new Vector2(0f, 8f);
            }
            slot.pips.Set(level, max);
        }
        slot.label.gameObject.SetActive(max <= 0);

        // 레벨이 오른 칸을 잠깐 키웠다가 되돌림
        slot.rect.localScale = Vector3.one * 1.25f;
    }

    void Update()
    {
        foreach (Slot slot in slots.Values)
        {
            // 1.1.9: 이미 1이면 쓰지 않음 (Transform 쓰기만 해도 캔버스를 다시 묶음)
            if (slot.rect.localScale != Vector3.one)
                slot.rect.localScale = Vector3.MoveTowards(slot.rect.localScale, Vector3.one, Time.unscaledDeltaTime * 2f);
            UpdateBadge(slot);
        }
    }

    // 쌓이는 고유 스킬은 칸 오른쪽 위에 지금 스택 (진화한 칸은 그 진화의 고유 스킬 재료를 따름)
    void UpdateBadge(Slot slot)
    {
        string key = null;
        // 2026-10-19~ 입문 칸 = 갈래 칸: 갈래 효과의 숫자(열 게이지 · 중첩 …)가 없으면 공명 단계 I · II · 극의
        if (Paths.IsSeed(slot.id))
        {
            SetBadge(slot, Paths.HudBadge(slot.id - Paths.SeedFirstId));
            return;
        }
        if (LevelShop.IsSig(slot.id)) key = LevelShop.SigKey(slot.id);
        else if (slot.id >= 1000 && Shop != null)
        {
            LevelShop.SkillEvo e = Shop.EvoById(slot.id);
            if (e != null)
                foreach (int p in e.parts)
                    if (LevelShop.IsSig(p) && SignatureSkills.StackText(LevelShop.SigKey(p)) != null) { key = LevelShop.SigKey(p); break; }
        }
        SetBadge(slot, key != null ? SignatureSkills.StackText(key) : null);
    }

    void SetBadge(Slot slot, string text)
    {
        if (text == slot.shown) return;
        slot.shown = text;
        if (slot.badge == null)
        {
            if (text == null) return;
            GameObject go = new GameObject("Stack", typeof(RectTransform), typeof(Image));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(slot.rect, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
            r.anchoredPosition = new Vector2(6f, 6f);
            r.sizeDelta = new Vector2(46f, 24f);
            Image bg = go.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.06f, 0.18f, 0.92f);
            bg.raycastTarget = false;
            GameObject tg = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform tr = tg.GetComponent<RectTransform>();
            tr.SetParent(r, false);
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = tr.offsetMax = Vector2.zero;
            slot.badge = tg.GetComponent<TextMeshProUGUI>();
            if (font != null) slot.badge.font = font;
            if (fontMaterial != null) slot.badge.fontSharedMaterial = fontMaterial;
            slot.badge.enableAutoSizing = true;
            slot.badge.fontSizeMin = 10f;
            slot.badge.fontSizeMax = 20f;
            slot.badge.alignment = TextAlignmentOptions.Center;
            slot.badge.color = new Color(1f, 0.85f, 0.45f);
            slot.badge.raycastTarget = false;
        }
        slot.badge.transform.parent.gameObject.SetActive(text != null);
        if (text != null) slot.badge.text = text;
    }

    LevelShop Shop => levelShop != null ? levelShop : (levelShop = FindFirstObjectByType<LevelShop>());

    string AbilityName(int id)
    {
        if (Shop == null) return "";
        if (id >= 1000) { LevelShop.SkillEvo e = Shop.EvoById(id); return e != null ? Loc.T(e.name) : ""; }
        if (id >= Shop.ability_name.Length) return "";
        return Shop.ability_name[id];
    }

    string AbilityDescription(int id)
    {
        if (Shop == null) return "";
        if (id >= 1000) { LevelShop.SkillEvo e = Shop.EvoById(id); return e != null ? LevelShop.EvoTooltip(e, CharacterData.Selected) : ""; }
        LevelShop.HudView = true;          // 지금 수치 + 최대 수치 (레벨업 카드의 「지금 → 다음」 대신)
        try { return Shop.GetAbilityTooltip(id); }
        finally { LevelShop.HudView = false; }
    }

    // 스킬 진화: 재료 칸들을 지우고 그 자리에 진화 칸 하나 (빛나는 테두리)
    readonly HashSet<int> merged = new HashSet<int>();      // 진화 칸에 합쳐진 재료 (다시 칸을 만들지 않음)

    public void Merge(int[] parts, int evoId)
    {
        foreach (int p in parts) merged.Add(p);
        // 진화 칸은 첫 재료가 있던 자리에 (없으면 맨 끝)
        int at = -1;
        foreach (int p in parts)
        {
            int i = order.IndexOf(p);
            if (i >= 0 && (at < 0 || i < at)) at = i;
        }
        foreach (int p in parts)
        {
            if (!slots.TryGetValue(p, out Slot s)) continue;
            Destroy(s.rect.gameObject);
            slots.Remove(p);
        }
        order.RemoveAll(p => !slots.ContainsKey(p));
        if (slots.TryGetValue(evoId, out Slot old)) { Destroy(old.rect.gameObject); slots.Remove(evoId); order.Remove(evoId); }
        Slot slot = CreateSlot(evoId, slots.Count);
        slots.Add(evoId, slot);
        order.Insert(at >= 0 && at <= order.Count ? at : order.Count, evoId);
        ApplyOpen();       // 접혀 있으면 새 진화 칸도 숨기고 수 갱신
        slot.level = 0;
        // 1.2.3~ 칸 폭 안에서 자동으로 줄임 (영어 「Evolution」 22pt 는 약 102px 라 76px 칸 밖 옆 칸까지 넘쳤음)
        slot.label.enableAutoSizing = true;
        slot.label.fontSizeMin = 12f;
        slot.label.fontSizeMax = 22f;
        slot.label.margin = new Vector4(3f, 0f, 3f, 0f);
        slot.label.text = Loc.T("진화");
        slot.label.color = new Color(0.79f, 0.63f, 1f);
        Image bg = slot.rect.GetComponent<Image>();
        if (bg != null) bg.color = new Color(0.85f, 0.7f, 1f);
        slot.rect.localScale = Vector3.one * 1.5f;
        Relayout();
        ApplyOpen();
    }

    void Relayout()
    {
        int i = 0;
        foreach (int id in order)
        {
            if (!slots.TryGetValue(id, out Slot s)) continue;
            int col = i % columns, row = i / columns;
            s.rect.anchoredPosition = new Vector2(
                ToggleWidth + spacing + slotSize.x * 0.5f + col * (slotSize.x + spacing),
                -slotSize.y * 0.5f - row * (slotSize.y + spacing));
            i++;
        }
    }

    Slot CreateSlot(int id, int index)
    {
        int col = index % columns;
        int row = index / columns;

        GameObject slotGo = new GameObject("Ability_" + id, typeof(RectTransform), typeof(Image));
        RectTransform rect = slotGo.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = slotSize;
        rect.anchoredPosition = new Vector2(
            ToggleWidth + spacing + slotSize.x * 0.5f + col * (slotSize.x + spacing),
            -slotSize.y * 0.5f - row * (slotSize.y + spacing));

        Image bg = slotGo.GetComponent<Image>();
        bg.sprite = slotSprite;
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = true;   // 마우스를 올리면 설명 표시

        TooltipTrigger tip = slotGo.AddComponent<TooltipTrigger>();
        tip.titleProvider = () => AbilityName(id) + (id >= 1000 ? "  ◆ " + Loc.T("진화") : "  Lv." + (slots.TryGetValue(id, out Slot s) ? s.level : 1));
        tip.bodyProvider = () => AbilityDescription(id);
        tip.hud = true;            // 1.0.4~: 이 칸 위를 겨눠도 평타가 멈추지 않음 · 전투 중엔 잠깐 머물러야 설명

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.SetParent(rect, false);
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(10f, 14f);
        iconRect.offsetMax = new Vector2(-10f, -6f);
        Image icon = iconGo.GetComponent<Image>();
        icon.sprite = GetIcon(id);
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        GameObject labelGo = new GameObject("Level", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = new Vector2(1f, 0f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.sizeDelta = new Vector2(0f, 30f);
        labelRect.anchoredPosition = new Vector2(0f, -8f);
        TextMeshProUGUI label = labelGo.GetComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        if (fontMaterial != null) label.fontSharedMaterial = fontMaterial;
        label.fontSize = 22f;
        label.color = new Color(0.96f, 0.83f, 0.47f);
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.raycastTarget = false;

        return new Slot { rect = rect, label = label, id = id };
    }
}
