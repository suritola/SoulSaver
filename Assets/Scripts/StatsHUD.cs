using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 화면 오른쪽 아래에 플레이어의 현재 능력치를 보여주는 패널
public class StatsHUD : MonoBehaviour
{
    [Header("아이콘 (공격력, 방어력, 공격 속도, 재장전, 이동 속도)")]
    public Sprite[] icons = new Sprite[5];

    [Header("모양")]
    public TMP_FontAsset font;
    public Material fontMaterial;
    public float rowHeight = 34f;
    public float padding = 26f;
    public float refreshInterval = 0.2f;

    [Header("열고 닫기 탭")]
    public Sprite tabSprite;
    public Sprite arrowSprite;          // 위쪽을 가리키는 화살표
    public Vector2 tabSize = new Vector2(64f, 38f);
    public bool startOpen = false;      // 처음엔 접힌 상태
    public float animSpeed = 8f;

    const string PrefKey = "StatsPanelOpen";

    bool open;
    float openAmount;                   // 0 = 접힘, 1 = 펼침
    RectTransform panelRect;
    RectTransform arrowRect;

    // 한국어 원문 (static 초기화 중엔 설정을 읽을 수 없으므로 쓸 때 번역)
    internal static readonly string[] Labels = { "공격력", "방어력", "공격 속도", "재장전", "이동 속도",
                                        "체력", "치명타 확률", "치명타 피해", "피해 배율", "관통", "체력 재생", "행운", "성장" };     // 1.0.4~ 성장 (성장 카드 · 영혼 정련 횟수)
    // 6번째 줄부터는 레벨업 카드 아이콘을 빌려 씀 (강철같은 심장 · 노려보는 눈빛 · 관통하는 총알 · 생명의 샘)
    // 1.0.4~: 마지막 줄 「행운」도 아이콘 (영혼 트리 행운 칸과 같은 4번 카드 아이콘, 예전엔 빈칸)
    // 1.1~: 마지막 줄 「성장」도 아이콘 (레벨업 · 경험치 4번 카드 아이콘, 예전엔 배열이 모자라 빈칸)
    static readonly int[] CardIcons = { 8, 3, 3, -1, 0, 10, 4, 4 };

    // 1.1.9~ 줄마다 마우스를 올리면 뜻 (일시정지 중에는 PauseInspect 가 바로 보여 줌) — Labels 와 같은 순서
    internal static readonly string[] Descs =
    {
        "평타 한 발의 피해입니다. 멀티 샷이면 발당 피해 × 발사 수로 보입니다.",
        "받는 피해를 이 비율만큼 줄입니다. 상한이 있습니다.",
        "초당 평타 횟수입니다.",
        "탄창을 다 쓴 뒤 다시 채우는 데 걸리는 시간입니다. 탄창이 없는 영웅은 「-」로 보입니다.",
        "시작 속도에 비한 지금 이동 속도입니다.",
        "지금 체력과 최대 체력입니다. 0이 되면 쓰러집니다.",
        "공격이 치명타가 될 확률입니다. 영혼 트리에서 올립니다.",
        "치명타가 났을 때의 피해 배율입니다.",
        "버프 · 특수 능력으로 모든 피해에 곱해지는 값입니다.",
        "총알 하나가 뚫고 지나가는 적의 수입니다.",
        "초당 회복하는 체력입니다. 적을 쓰러뜨릴 때 회복하는 양은 작게 붙습니다.",
        "지금 키우는 카드가 레벨업 카드에 나올 확률을 올립니다. 1마다 40%씩 더 자주 나옵니다.",
        "최대 레벨 뒤에 고른 성장 카드 수와 영혼 정련 횟수입니다.",
    };

    // 1.1.9~ 빌드 자세히 보기(BuildView) 가 같은 값을 쓰려고
    public static StatsHUD Instance { get; private set; }
    void Awake() => Instance = this;
    void OnDestroy() { if (Instance == this) Instance = null; }

    PlayerController player;
    TextMeshProUGUI[] values;
    TextMeshProUGUI[] labels;
    float[] labelRight;
    float baseSpeed;
    float timer;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (player != null) baseSpeed = player.speed;

        values = new TextMeshProUGUI[Labels.Length];
        labels = new TextMeshProUGUI[Labels.Length];
        labelRight = new float[Labels.Length];
        for (int i = 0; i < Labels.Length; i++) BuildRow(i);

        panelRect = GetComponent<RectTransform>();
        // 줄이 늘어난 만큼 패널을 키움 (아래쪽 기준이라 위로 자람)
        panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, padding * 2f + Labels.Length * rowHeight);
        BuildTab();

        open = Prefs.GetInt(PrefKey, startOpen ? 1 : 0) == 1;
        openAmount = open ? 1f : 0f;
        ApplyOpenAmount();

        Refresh();
    }

    // 패널 바로 아래(탄약 패널 위)에 붙는 화살표 버튼
    void BuildTab()
    {
        GameObject tab = new GameObject("StatsToggle", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform tabRect = tab.GetComponent<RectTransform>();
        tabRect.SetParent(panelRect.parent, false);
        tabRect.SetSiblingIndex(panelRect.GetSiblingIndex() + 1);
        tabRect.anchorMin = panelRect.anchorMin;
        tabRect.anchorMax = panelRect.anchorMax;
        tabRect.pivot = new Vector2(1f, 0f);
        tabRect.sizeDelta = tabSize;
        tabRect.anchoredPosition = panelRect.anchoredPosition - new Vector2(0f, tabSize.y + 4f);

        Image bg = tab.GetComponent<Image>();
        bg.sprite = tabSprite;
        bg.type = Image.Type.Sliced;

        Button button = tab.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.9f, 0.62f);
        colors.pressedColor = new Color(0.72f, 0.62f, 0.48f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        button.onClick.AddListener(Toggle);

        TooltipTrigger tip = tab.AddComponent<TooltipTrigger>();
        tip.title = Loc.T("능력치");
        tip.body = Loc.T("눌러서 능력치 창을 열고 닫습니다.");

        GameObject arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.SetParent(tabRect, false);
        arrowRect.sizeDelta = new Vector2(tabSize.y - 6f, tabSize.y - 6f);
        Image arrowImage = arrow.GetComponent<Image>();
        arrowImage.sprite = arrowSprite;
        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;
    }

    public void Toggle()
    {
        open = !open;
        Prefs.SetInt(PrefKey, open ? 1 : 0);
        // 버튼이 선택된 채로 남아 색이 고정되지 않도록
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    // 패널은 아래쪽 기준으로 세로로 접힘
    void ApplyOpenAmount()
    {
        float eased = openAmount * openAmount * (3f - 2f * openAmount);
        panelRect.localScale = new Vector3(1f, eased, 1f);
        // 열려 있으면 ▼(접기), 닫혀 있으면 ▲(펼치기)
        if (arrowRect != null) arrowRect.localEulerAngles = new Vector3(0f, 0f, open ? 180f : 0f);
    }

    void BuildRow(int i)
    {
        float y = -padding - i * rowHeight - rowHeight * 0.5f;

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.SetParent(transform, false);
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.sizeDelta = new Vector2(rowHeight - 4f, rowHeight - 4f);
        iconRect.anchoredPosition = new Vector2(padding + rowHeight * 0.5f, y);
        Image icon = iconGo.GetComponent<Image>();
        icon.sprite = i < icons.Length ? icons[i] : RowIcon(i);
        if (icon.sprite == null) icon.enabled = false;
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        labels[i] = MakeText("Label", Loc.T(Labels[i]), new Color(0.92f, 0.88f, 0.80f), TextAlignmentOptions.Left,
                 padding + rowHeight + 8f, y);
        // 1.1~: 긴 이름(「クリティカルダメージ」 · 「Damage Multiplier」)이 값 · 창 밖까지 넘쳤음 → 값 왼쪽까지만, 넘치면 글자를 줄임
        labels[i].enableAutoSizing = true;
        labels[i].fontSizeMax = 24f;
        labels[i].fontSizeMin = 13f;
        labels[i].overflowMode = TextOverflowModes.Ellipsis;
        values[i] = MakeText("Value", "", new Color(0.96f, 0.83f, 0.47f), TextAlignmentOptions.Right,
                             padding, y);

        // 1.1.9~ 줄 전체에 투명 판 + 툴팁 (전투 중 조준은 막지 않음 · hud)
        GameObject hit = new GameObject("RowTip", typeof(RectTransform), typeof(Image));
        RectTransform hr = hit.GetComponent<RectTransform>();
        hr.SetParent(transform, false);
        hr.anchorMin = new Vector2(0f, 1f);
        hr.anchorMax = new Vector2(1f, 1f);
        hr.pivot = new Vector2(0.5f, 0.5f);
        hr.offsetMin = new Vector2(padding * 0.5f, 0f);
        hr.offsetMax = new Vector2(-padding * 0.5f, 0f);
        hr.sizeDelta = new Vector2(hr.sizeDelta.x, rowHeight);
        hr.anchoredPosition = new Vector2(hr.anchoredPosition.x, y);
        Image hi = hit.GetComponent<Image>();
        hi.color = new Color(0f, 0f, 0f, 0f);
        hi.raycastTarget = true;
        TooltipTrigger tip = hit.AddComponent<TooltipTrigger>();
        int row = i;
        tip.titleProvider = () => Loc.T(Labels[row]);
        tip.bodyProvider = () => Loc.T(Descs[row]) + (values != null && values[row] != null ? "\n\n<color=#F5D478>" + values[row].GetParsedText() + "</color>" : "");
        tip.hud = true;
    }

    // 1.1.9~ 빌드 자세히 보기: 줄 아이콘 (HUD 와 같은 그림)
    internal Sprite RowSprite(int i) => i < icons.Length ? icons[i] : RowIcon(i);

    Sprite RowIcon(int i)
    {
        int k = i - icons.Length;
        if (k < 0 || k >= CardIcons.Length) return null;
        if (CardIcons[k] < 0) return icons.Length > 0 ? icons[0] : null;      // 피해 배율: 공격력 아이콘
        AbilityHUD hud = FindFirstObjectByType<AbilityHUD>(FindObjectsInactive.Include);
        return hud != null ? hud.GetIcon(CardIcons[k]) : null;
    }

    TextMeshProUGUI MakeText(string name, string text, Color color, TextAlignmentOptions align, float inset, float y)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        // 왼쪽 글자는 아이콘 오른쪽부터, 오른쪽 값은 패널 오른쪽 여백까지
        bool left = align == TextAlignmentOptions.Left;
        rect.offsetMin = new Vector2(left ? inset : padding, 0f);
        rect.offsetMax = new Vector2(-padding, 0f);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, rowHeight);
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);

        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        if (fontMaterial != null) t.fontSharedMaterial = fontMaterial;
        t.fontSize = 24f;
        t.color = color;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.raycastTarget = false;
        t.text = text;
        return t;
    }

    void Update()
    {
        float target = open ? 1f : 0f;
        if (!Mathf.Approximately(openAmount, target))
        {
            openAmount = Mathf.MoveTowards(openAmount, target, Time.unscaledDeltaTime * animSpeed);
            ApplyOpenAmount();
        }

        if (!open) return;

        timer += Time.unscaledDeltaTime;
        if (timer < refreshInterval) return;
        timer = 0f;
        Refresh();
    }

    void Refresh()
    {
        if (player == null || values == null) return;
        string[] v = ValueTexts();
        for (int i = 0; i < values.Length && i < v.Length; i++) values[i].text = v[i];
        FitLabels();
    }

    // 1.1.9~ 13줄 값 (HUD 와 빌드 자세히 보기가 같이 씀)
    internal string[] ValueTexts()
    {
        string[] values = new string[Labels.Length];
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (player == null) { for (int i = 0; i < values.Length; i++) values[i] = "-"; return values; }

        // 공격력: 멀티샷이면 발당 피해 x 발사 수
        int shots = Mathf.Max(1, player.multiShot);
        values[0] = shots > 1
            ? (player.damage * player.MultiShotDamageRate(shots)).ToString("0.##") + " x " + shots
            : player.damage.ToString("0.##");

        // 방어력: 받는 피해 감소율
        values[1] = (Mathf.Min(player.def, PlayerController.MaxDef) * 100f).ToString("0") + "%";

        // 공격 속도: 초당 발사 수
        values[2] = (1f / Mathf.Max(0.01f, player.ShootSpeed)).ToString("0.0") + Loc.T("/초");

        // 1.0.4~: 탄을 쓰지 않는 영웅(근접 · 궁수 등)은 재장전이 없으니 「-」
        CharacterKit kit = CharacterKit.Instance;
        values[3] = kit != null && !kit.UsesAmmo ? "-" : player.reloadTime.ToString("0.0") + Loc.T("초");

        // 이동 속도: 시작 속도 대비
        values[4] = baseSpeed > 0f ? (player.speed / baseSpeed * 100f).ToString("0") + "%" : player.speed.ToString("0.0");

        values[5] = Mathf.CeilToInt(player.PlayerHealth) + " / " + Mathf.RoundToInt(player.PlayerMaxHealth);

        // 치명타: 영혼 트리에서 얻음 (없으면 0%)
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        values[6] = ((sp != null ? sp.TreeCrit : 0f) * 100f).ToString("0") + "%";
        // 1.0.4~: 치명타 확률이 0% 면 치명타 피해는 쓰이지 않으니 「-」 (예전엔 0% 인데도 200% 가 보였음)
        float critChance = sp != null ? sp.TreeCrit : 0f;
        values[7] = critChance <= 0f ? "-" : ((sp != null ? sp.TreeCritDamage : 2f) * 100f).ToString("0") + "%";

        // 피해 배율: 버프 · 특수 능력으로 곱해지는 값
        values[8] = (player.damageMultiplier * 100f).ToString("0") + "%";

        // 관통: 뚫고 지나가는 적 수
        int pierce = Mathf.Max(0, player.pene - 1);
        values[9] = pierce + Loc.T(pierce == 1 ? "마리 (한 마리)" : "마리");      // 1.1~ 영어 「1 enemy」 (예전엔 「1 enemies」)

        // 체력 재생: 초당 · 처치 시
        // 행운 (1.0.2): 지금 키우는 카드가 나올 확률 (행운 1마다 +40%, LevelShop.DrawWeight)
        int luck = sp != null ? sp.Luck : 0;
        values[11] = luck > 0 ? luck + "  <size=80%><color=#9FE89F>(+" + (luck * 40) + "%)</color></size>" : "0";
        // 1.1~: 처치 회복이 붙으면 값이 길어져 이름 위로 겹쳤음 → 처치 회복은 작게
        values[10] = player.regenPerSecond.ToString("0.#") + Loc.T("/초") + (player.healOnKill > 0f ? " <size=65%>+" + player.healOnKill.ToString("0.#") + Loc.T("/처치") + "</size>" : "");
        // 성장 (1.0.4~): 만렙 뒤 고른 성장 카드 수 · 영혼 정련 횟수
        LevelShop shop = Cache<LevelShop>.Get;
        int grown = shop != null ? shop.GrowthPicks : 0, refines = sp != null ? sp.Refines : 0;
        values[12] = grown + (refines > 0 ? "  <size=80%><color=#C9A0FF>" + Loc.T("영혼 정련") + " " + refines + "</color></size>" : "");
        return values;
    }

    // 1.1~: 줄마다 이름 칸 오른쪽 끝을 값 글자 왼쪽(+8)까지로 (값이 길면 이름이 자동으로 작아짐)
    void FitLabels()
    {
        if (labels == null) return;
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == null || values[i] == null) continue;
            float w = values[i].GetPreferredValues(values[i].text).x;
            float right = -padding - Mathf.Ceil(w) - 8f;
            if (Mathf.Approximately(labelRight[i], right)) continue;
            labelRight[i] = right;
            RectTransform r = labels[i].rectTransform;
            r.offsetMax = new Vector2(right, r.offsetMax.y);
        }
    }
}
