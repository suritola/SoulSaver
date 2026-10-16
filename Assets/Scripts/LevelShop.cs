using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class LevelShop : MonoBehaviour
{
    public GameObject LvshopPanel;

    [Header("첫번째 버튼 텍스트")]
    public TextMeshProUGUI FirstTitle;
    public TextMeshProUGUI FirstAbility;

    [Header("두번째 버튼 텍스트")]
    public TextMeshProUGUI SecondTitle;
    public TextMeshProUGUI SecondAbility;

    [Header("세번째 버튼 텍스트")]
    public TextMeshProUGUI ThirdTitle;
    public TextMeshProUGUI ThirdAbility;

    public int remaining_abilitys;
    public int Total_abilitys;

    public string[] ability_name = new string[10];
    public string[] ability_content = new string[10];
    public bool[] ability_selected = new bool[10];
    // 능력별로 몇 번 선택했는지 (HUD의 Lv 표시)
    public int[] ability_level = new int[10];

    [Header("능력 아이콘 표시")]
    public AbilityHUD abilityHUD;
    public Image FirstIcon;
    public Image SecondIcon;
    public Image ThirdIcon;

    PlayerController bul;
    Level lv;

    // Start is called before the first frame update
    void Start()
    {

        Total_abilitys = AbilityCount;
        remaining_abilitys = AbilityCount;

        // 씬에 저장된 배열은 10칸이라 새 능력 수에 맞게 늘림
        System.Array.Resize(ref ability_name, AbilityCount);
        System.Array.Resize(ref ability_content, AbilityCount);
        System.Array.Resize(ref ability_selected, AbilityCount);
        System.Array.Resize(ref ability_level, AbilityCount);

        bul = FindFirstObjectByType<PlayerController>();
        lv = FindFirstObjectByType<Level>();
        for (int i = 0; i< Total_abilitys; i++)
        {
            // 11번(처치 시 회복)은 더 이상 나오지 않음 (도적은 사냥의 기세), 12 · 13번은 캐릭터 카드, 14번은 비상 보급 전용
            // 2번(재활용 에너지)은 스킬 강화라 빠짐: 필살기는 영혼 트리 · 특수 능력에서만 강화
            ability_selected[i] = ((i == 11 || i == 12 || i == 13) && KitIcon(i) == null) || i == SupplyId || i == RecycleId;
        }
        CompactLevelUp();
        CompactTitle();
        freePick = true;            // 시작할 때 주는 카드는 레벨을 올리지 않음
        SignatureSkills.Ensure()?.Refresh(this);         // 고유 스킬 효과 (플레이어에 붙음)
        //레벨업 능력들
        setAbilitys();
        

        openLevelShop();
    }

    // Update is called once per frame

    void setAbilitys()
    {
        // 공용 카드 (0 ~ 11) · 비상 보급: 모두 같은 모양 (한 문장 + 수치 지금 → 다음 + Lv)
        string[] names = { "관통하는 총알", "코인충", "재활용 에너지", "노려보는 눈빛", "더 많은 경험치", "코인 자석",
                           "멀티 샷", "밀어내기", "강철같은 심장", "단단한 신체", "생명의 샘", "피의 굶주림" };
        for (int i = 0; i < names.Length && i < ability_name.Length; i++)
        {
            ability_name[i] = Loc.T(names[i]);
            ability_content[i] = SharedCardText(i);
        }
        ability_name[SupplyId] = Loc.T("비상 보급");
        ability_content[SupplyId] = SharedCardText(SupplyId);
        KitSetAbilitys();
        SigSetAbilitys();
    }
    void Update()
    {
        // 카드 글(15장)은 창이 열려 있을 때만 새로 만듦 (닫혀 있을 때 매 프레임 만들던 문자열 쓰레기 제거)
        if (IsOpen) setAbilitys();
        UpdateSelectLock();

        UpdateReroll();
        UpdateSkipButton();
        // 1.0.6~: 최대 레벨에 닿으면 남은 레벨업은 버림 (상한 뒤로는 카드를 더 고를 수 없음, 성장 카드는 상한 전에 카드를 다 배웠을 때만)
        if (PendingLevels > 0 && !IsOpen && AtLevelCap) PendingLevels = 0;
        UpdateTranscend();          // 1.1~ 최대 레벨 뒤 막대가 차면 초월 카드 (LevelShop.Transcend)
        // 숫자 1 · 2 · 3: 그 카드를 고름, 이미 고른 카드의 숫자를 한 번 더 누르면 확정
        // 1.0.4~: 예전엔 한 번에 확정돼 살펴보려다 실수로 골랐음 → 보스 보상 · 무기 진화 창과 같은 방식으로 통일
        // 1.0.5~ 패드 십자키 좌우: 카드 고르기 (커서도 그 카드로 옮겨, A 를 누르면 확정)
        if (selectReady && LvshopPanel != null && LvshopPanel.activeInHierarchy)
        {
            int pad = GamePad.TakeDpadX();
            if (pad != 0)
            {
                pendingSlot = pendingSlot < 0 ? (pad > 0 ? 0 : 2) : Mathf.Clamp(pendingSlot + pad, 0, 2);
                lastPickAt = -9f;
                RefreshCards();
                GamePad.WarpTo(CardOf(pendingSlot) as RectTransform);
            }
        }
        int quick = QuickPickKey();
        if (selectReady && quick >= 0 && LvshopPanel != null && LvshopPanel.activeInHierarchy)
        {
            if (quick == pendingSlot || OneClick) ConfirmSlot(quick);     // 1.1.9~ 설정 「카드 한 번 클릭으로 확정」이면 숫자 한 번에
            else
            {
                pendingSlot = quick;
                lastPickAt = -9f;           // 숫자로 고른 뒤 클릭 한 번이 더블클릭으로 잡히지 않게
                RefreshCards();
            }
        }
        // 클릭으로 고른 카드를 스페이스바로 확정
        else if (selectReady && pendingSlot >= 0 && LvshopPanel != null && LvshopPanel.activeInHierarchy && KeyBindings.Down(GameAction.Interact))
            ConfirmSlot(pendingSlot);
        // 쌓인 레벨업: [Space]로 하나씩 고름 (상점 가판대 옆이면 상점이 먼저) · 설정 「레벨업 바로 열기」면 바로 엶 (2.1.8)
        // 1.1~: 판 3번 미만이면 저절로 엶 · 이어 고르는 중이면 다음 창을 바로 (LevelShop.Chain)
        else if (CanOpenPending && ((!ShopStall.PlayerNear && (KeyBindings.Down(GameAction.Interact) || AutoOpenLevelUp)) || ChainNext))
            OpenPending();
        UpdateChain();
        UpdatePendingBadge();
    }

    // ================================================================= 운명의 실: [다시 뽑기 키] 다시 뽑기
    TMP_Text rerollText;
    int shownRerolls = -1;
    bool shownRerollPad, shownPendingPad;      // 1.0.5~ 안내를 만들 때 패드였는지 (바뀌면 [R] ↔ [X])

    // 카드 아래 안내 두 줄 (카드 아래끝 -360, 고른 카드는 1.08배라 -384까지): 확정 안내 → 다시 뽑기 순서로 겹치지 않게
    // 그 아래 가운데는 건너뛰기 버튼 (LevelShop.EvoUI), 다시 뽑기 안내는 그 왼쪽
    const float SelectHintY = -426f,     // 1.1~ -410 → -426: 고른 카드의 「★ 추천」 알약 · 시너지 배지(박동까지 -408)와 안 겹치게
    RerollHintY = SkipY, RerollHintX = -480f;

    void UpdateReroll()
    {
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        int n = sp != null ? sp.Rerolls : 0;
        bool open = IsOpen && n > 0 && ShowRerollIntro;
        // 1.0.4~: 열린 직후 0.6초 잠금(selectReady) 동안은 받지 않음 — 재장전하던 [R]에 다시 뽑기가 써지던 문제
        if (open && selectReady && KeyBindings.Down(GameAction.Reroll) && TryReroll())     // 키 설정에서 바꿀 수 있음 (2.1.8)
        {
            n = sp.Rerolls;
            open = n > 0;
        }
        if (rerollText == null)
        {
            if (!open || LvshopPanel == null) return;
            // 1.0.1: 작은 보라 글씨라 잘 안 보였음 → 테두리 있는 배지 + 노란 키 + 두근거림
            // 1.1.9~ 배지를 눌러도 다시 뽑기 (마우스로 카드를 고르는 창에서 다시 뽑기만 키였음)
            rerollText = HintBadge.Make(LvshopPanel.transform, "RerollHint", new Vector2(RerollHintX, RerollHintY), new Vector2(400f, 58f),
                new Color(0.2f, 0.1f, 0.34f, 0.96f), new Color(0.82f, 0.62f, 1f), 30f, clickable: true);
            Button rb = rerollText.transform.parent.GetComponent<Button>();
            if (rb != null) rb.onClick.AddListener(() => TryReroll());
        }
        GameObject badge = rerollText.transform.parent.gameObject;
        if (badge.activeSelf != open) badge.SetActive(open);
        if (open && (shownRerolls != n || shownRerollPad != GamePad.Active))
        {
            shownRerolls = n;
            shownRerollPad = GamePad.Active;
            rerollText.text = "<color=#FFE066>[" + KeyBindings.Name(GameAction.Reroll) + "]</color> " + Loc.T("카드 다시 뽑기") + "  <color=#FFE066>(" + n + ")</color>";
        }
    }

    // 키 · 배지 클릭이 같이 씀 (0.6초 잠금 동안은 받지 않음)
    bool TryReroll()
    {
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        if (sp == null || !IsOpen || !selectReady || sp.Rerolls <= 0 || !ShowRerollIntro || !sp.TryReroll()) return false;
        UpdateLvShopContent(true);      // 1.0.4~: 방금 본 세 장은 빼고 뽑음
        pendingSlot = -1;
        RefreshCards();
        PlayRerollFx();             // 1.0.1: 반짝이며 터지고 뒤집히듯 바뀌는 연출 (LevelShop.RerollFx)
        return true;
    }

    // ================================================================= 쌓인 레벨업
    public int PendingLevels { get; private set; }

    // 영웅 숙련도 (Mastery, 1.0.1): 경험치 요구량을 올리지 않고 고를 카드만 더 줌
    public void GrantFreePicks(int n) { if (n > 0) AddPending(n); }

    public void AddPending(int n = 1)
    {
        PendingLevels += n;
        SpecialAbilities.SharedInstance?.TreeOnLevelUp(n);       // 영혼 트리 '깨달음'
        Hints.Show("levelup", "레벨이 오르면 레벨업 카드를 고를 수 있습니다. [{INTERACT}]를 눌러 원할 때 카드를 고르세요.");     // 1.0.4~: 용어를 「레벨업 카드」로 통일
    }

    GameObject pendingBadge;
    BadgeClick pendingClick;
    public GameObject PendingBadge => pendingBadge;        // 1.0.4~: 튜토리얼이 레벨업 배지를 짚어 줌
    TextMeshProUGUI pendingText;
    int shownPending = -1;
    bool shownTree;

    bool CanOpenPending => PendingLevels > 0 && !IsOpen && Time.timeScale == 1f && !ShopOpen;

    void OpenPending()
    {
        PendingLevels--;
        chainIndex++;
        openLevelShop();
    }

    // 1.1.9~ 「레벨업!」 배지를 눌러도 카드 창이 열림 (예전엔 키로만 열려 마우스로 눌러 보고 고장인 줄 알았음)
    void TryOpenPending()
    {
        if (!CanOpenPending || ESCmenu.IsOpen || StoryDirector.Playing) return;
        OpenPending();
    }

    void UpdatePendingBadge()
    {
        bool show = PendingLevels > 0 && !IsOpen && !ESCmenu.IsOpen && !StoryDirector.Playing;     // 1.1~ 보스 등장 연출 중엔 숨김 (대사 뒤에서 번쩍였음)
        if (!show)
        {
            if (pendingBadge != null && pendingBadge.activeSelf) pendingBadge.SetActive(false);
            return;
        }
        if (pendingBadge == null)
        {
            Canvas canvas = LvshopPanel != null ? LvshopPanel.GetComponentInParent<Canvas>(true) : null;
            if (canvas == null) canvas = UIKit.HudCanvas();
            if (canvas == null) return;
            canvas = canvas.rootCanvas;
            pendingBadge = new GameObject("LevelUpPending", typeof(RectTransform), typeof(Image));
            RectTransform r = pendingBadge.GetComponent<RectTransform>();
            r.SetParent(canvas.transform, false);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(900f, 62f);       // 1.0.1: 780 × 48 → 900 × 62 (눈에 잘 띄게)
            Image bg = pendingBadge.GetComponent<Image>();
            bg.color = new Color(0.35f, 0.22f, 0.02f, 0.85f);
            bg.raycastTarget = true;
            // Button 이 아니라 클릭만 받음: 전투 중 조준이 지나갈 때마다 버튼 소리(UISoundHook)가 나지 않게 · Space 가 다시 누르지 않게
            pendingClick = pendingBadge.AddComponent<BadgeClick>();
            pendingClick.click = TryOpenPending;
            TooltipTrigger tip = pendingBadge.AddComponent<TooltipTrigger>();
            tip.hud = true;                                 // HUD 칸처럼: 이 위를 조준해도 평타를 막지 않음 (PlayerController.PointerOverUI)
            tip.titleProvider = () => Loc.T("레벨업 카드");
            tip.bodyProvider = () => Loc.T("눌러서 카드를 고릅니다. 모아 두고 나중에 골라도 됩니다.");
            GameObject tg = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform tr = tg.GetComponent<RectTransform>();
            tr.SetParent(r, false);
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(12f, 2f);
            tr.offsetMax = new Vector2(-12f, -2f);
            pendingText = tg.GetComponent<TextMeshProUGUI>();
            UIKit.EnsureStyle();
            if (UIKit.Font != null) pendingText.font = UIKit.Font;
            if (UIKit.FontMaterial != null) pendingText.fontSharedMaterial = UIKit.FontMaterial;
            pendingText.enableAutoSizing = true;
            pendingText.fontSizeMin = 14f;
            pendingText.fontSizeMax = 30f;
            pendingText.alignment = TextAlignmentOptions.Center;
            pendingText.raycastTarget = false;
        }
        if (!pendingBadge.activeSelf) pendingBadge.SetActive(true);
        // 경험치 바 위 「특수 강화」 버튼(96 ~ 160)이 떠 있으면 그 위로 올려 겹치지 않게
        bool upgradeShown = StageManager.Instance != null && StageManager.Instance.UpgradeButtonAtBottom;
        ((RectTransform)pendingBadge.transform).anchoredPosition = new Vector2(0f, upgradeShown ? 172f : 110f);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
        pendingText.color = Color.Lerp(new Color(1f, 0.85f, 0.4f), Color.white, pulse);
        bool hover = pendingClick != null && pendingClick.Hover;       // 마우스를 올리면 밝고 크게 (누를 수 있음을 보여 줌)
        pendingBadge.GetComponent<Image>().color = hover ? new Color(0.85f, 0.6f, 0.12f, 1f)
                                                         : Color.Lerp(new Color(0.35f, 0.22f, 0.02f, 0.85f), new Color(0.75f, 0.5f, 0.08f, 0.95f), pulse);     // 배경도 금빛으로 숨 쉼
        pendingBadge.transform.localScale = Vector3.one * (hover ? 1.06f : 1f + 0.04f * pulse);
        bool tree = StageManager.Instance != null && StageManager.Instance.TreeAffordable;
        if (shownPending != PendingLevels || shownTree != tree || shownPendingPad != GamePad.Active)
        {
            shownPending = PendingLevels;
            shownTree = tree;
            shownPendingPad = GamePad.Active;
            pendingText.text = Loc.T("레벨업!") + "  [" + KeyBindings.Name(GameAction.Interact) + "] " + Loc.T("카드 고르기") + (PendingLevels > 1 ? "  x" + PendingLevels : "")
                             + (tree ? "   \u00B7   [" + KeyBindings.Name(GameAction.Upgrade) + "] " + Loc.T("배울 칸 있음") : "");
        }
    }

    // 클릭한 카드 (0~2, -1 = 아직 없음)
    int pendingSlot = -1;
    TextMeshProUGUI selectHint;

    Transform CardOf(int slot)
    {
        TextMeshProUGUI title = slot == 0 ? FirstTitle : slot == 1 ? SecondTitle : ThirdTitle;
        if (title == null) return null;
        Button b = title.GetComponentInParent<Button>();
        return b != null ? b.transform : title.transform.parent;
    }

    // 1.1.9~ 설정 「카드 한 번 클릭으로 확정」 (튜토리얼은 「클릭 → [Space]」를 가르치므로 두 단계 그대로)
    static bool OneClick => GameSettings.OneClickPick && !TutorialRun.Active;

    void PickCard(int slot)
    {
        if (!selectReady) return;
        if (OneClick) { pendingSlot = slot; RefreshCards(); ConfirmSlot(slot); return; }
        // 같은 카드를 0.4초 안에 두 번 누르면 바로 확정 (더블클릭, 2.1.8)
        // 1.0.5~ 패드: 고른 카드를 A 로 한 번 더 누르면 확정
        if (slot == pendingSlot && (Time.unscaledTime - lastPickAt < 0.4f || GamePad.PadClickNow)) { ConfirmSlot(slot); return; }
        lastPickAt = Time.unscaledTime;
        pendingSlot = slot;
        RefreshCards();
    }

    float lastPickAt = -9f;

    void ConfirmSlot(int slot)
    {
        int what = slot == 0 ? first : slot == 1 ? second : third;
        pendingSlot = -1;
        onSelect(what);
    }

    // 숫자 키 1 · 2 · 3 (키패드 포함) → 카드 칸 0 · 1 · 2, 안 눌렀으면 -1
    static int QuickPickKey()
    {
        for (int i = 0; i < 3; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) return i;
        return -1;
    }

    // 고른 카드는 커지고 금빛, 아래 안내 글자도 바뀜
    void RefreshCards()
    {
        for (int i = 0; i < 3; i++)
        {
            Transform card = CardOf(i);
            if (card == null) continue;
            bool on = i == pendingSlot;
            card.localScale = Vector3.one * (on ? 1.08f : 1f);
            if (card.TryGetComponent(out Image img)) img.color = on ? new Color(1f, 0.88f, 0.55f) : Color.white;
        }

        if (selectHint == null && LvshopPanel != null)
        {
            GameObject go = new GameObject("SelectHint", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(LvshopPanel.transform, false);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = new Vector2(1000f, 44f);
            r.anchoredPosition = new Vector2(0f, SelectHintY);
            // 씬에 있던 옛 안내(「카드를 눌러 선택」)는 이 줄과 같은 말이라 숨김 (같은 자리에 겹쳐 보이던 문제)
            Transform old = LvshopPanel.transform.Find("PickHint");
            if (old != null) old.gameObject.SetActive(false);
            selectHint = go.GetComponent<TextMeshProUGUI>();
            if (FirstTitle != null)
            {
                selectHint.font = FirstTitle.font;
                selectHint.fontSharedMaterial = FirstTitle.fontSharedMaterial;
            }
            selectHint.fontSize = 32f;
            selectHint.enableAutoSizing = true;         // 안내가 길어져도 한 줄 (2.1.8 더블클릭 · 숫자 키 안내)
            selectHint.fontSizeMin = 20f;
            selectHint.fontSizeMax = 32f;
            selectHint.fontStyle = FontStyles.Bold;     // 1.0.1: 더 잘 보이게 (굵게 + 검은 테두리)
            selectHint.outlineWidth = 0.22f;
            selectHint.outlineColor = new Color32(0, 0, 0, 255);
            selectHint.enableWordWrapping = false;
            selectHint.alignment = TextAlignmentOptions.Center;
            selectHint.raycastTarget = false;
        }
        if (selectHint != null)
        {
            string picked = pendingSlot == 0 ? FirstTitle.text : pendingSlot == 1 ? SecondTitle.text : pendingSlot == 2 ? ThirdTitle.text : null;
            selectHint.color = picked != null ? new Color(1f, 0.86f, 0.4f) : Color.white;
            selectHint.text = picked != null ? Loc.T("[{INTERACT}] 확정 : ") + picked
                            : OneClick ? Loc.T("카드를 클릭하면 바로 확정됩니다")
                                             : Loc.T("카드를 클릭해 고른 뒤 [{INTERACT}]로 확정") + "   <color=#C9BCA4>" + Loc.T("더블클릭 · 같은 숫자 두 번으로 바로") + "</color>";     // 1.0.4~ 숫자 키 방식 통일
        }
    }

    // 사격하던 클릭이 열리자마자 카드를 누르지 않도록:
    // 창이 열리고 잠깐 지난 뒤 마우스 버튼을 한 번 떼야 고를 수 있음
    const float SelectDelay = 0.6f;
    float openedAt;
    bool selectReady;
    CanvasGroup cardGroup;

    void LockSelection()
    {
        openedAt = Time.unscaledTime;
        selectReady = false;
        if (cardGroup == null && LvshopPanel != null)
        {
            cardGroup = LvshopPanel.GetComponent<CanvasGroup>();
            if (cardGroup == null) cardGroup = LvshopPanel.AddComponent<CanvasGroup>();
        }
        if (cardGroup != null)
        {
            cardGroup.interactable = false;
            cardGroup.alpha = 0.6f;
        }
    }

    void UpdateSelectLock()
    {
        if (selectReady || LvshopPanel == null || !LvshopPanel.activeInHierarchy) return;

        float t = Mathf.Clamp01((Time.unscaledTime - openedAt) / SelectDelay);
        if (cardGroup != null) cardGroup.alpha = Mathf.Lerp(0.6f, 1f, t);

        bool mouseHeld = Input.GetMouseButton(0) || Input.GetMouseButton(1) || GameInput.FireHeld || GameInput.UltHeld;     // 1.0.4~ 공격 · 필살기를 다른 키로 바꿨어도
        if (t >= 1f && !mouseHeld)
        {
            selectReady = true;
            if (cardGroup != null)
            {
                cardGroup.interactable = true;
                cardGroup.alpha = 1f;
            }
        }
    }
    private bool showLv;
    public GameObject LvUpPanel;
    // 「레벨업!」 표시를 작게 줄여 알림판 아래에 (시야를 가리지 않게)
    void CompactLevelUp()
    {
        if (LvUpPanel == null) return;
        RectTransform r = LvUpPanel.GetComponent<RectTransform>();
        if (r == null) return;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = new Vector2(0f, -222f);       // 알림판 · 포털 카운트다운 아래
        r.localScale = Vector3.one * 0.4f;
        foreach (Graphic g in LvUpPanel.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
    }

    // 「능력을 하나 선택하세요」 띠: 원래 크기(폭 980)면 왼쪽 위 체력 패널(오른쪽 끝 x 620)을 덮어서 줄임
    void CompactTitle()
    {
        Transform t = LvshopPanel != null ? LvshopPanel.transform.Find("Title") : null;
        if (t == null) return;
        RectTransform r = (RectTransform)t;
        r.localScale = Vector3.one * 0.66f;                // 폭 약 650 → 화면 가운데 637 ~ 1283
        r.anchoredPosition = new Vector2(0f, 380f);
    }

    // 「레벨업!」 표시를 띄우고 있는 연출 수 (겹쳐도 마지막 연출이 끝날 때 꺼짐)
    int levelUpShowing;
    // 아직 연출하지 않은 레벨업 (한 번에 여러 레벨이 오르면 차례로)
    int levelUpQueued;

    // 경험치를 더하고 넘친 만큼 레벨업. 처치한 적 · 보스가 곧 사라져도 연출과 포인트가 끊기지 않게 이 창이 맡음
    public void GainExp(PlayerController p, float amount)
    {
        if (p == null) return;
        // 최대 레벨 50 (무한 모드 100): 모든 스킬을 다 올리지 못하게 해서 어떤 스킬 · 진화를 노릴지 고르게
        // 2.1~: 경험치가 차면 레벨업 포인트만 쌓이고, 레벨은 카드를 골라야 오름 (건너뛰면 레벨이 그대로라 상한 안에서 더 고를 수 있음)
        // 1.0.4~: 카드를 다 배웠거나 최대 레벨이어도 경험치가 멈추지 않음 → 그 뒤 레벨업은 끝없이 쌓이는 「성장」 카드 (LevelShop.Growth)
        p.nowEXP += amount;
        // 1.0.6~: 최대 레벨(50 · 무한 모드 80)에 닿으면 더 쌓지 않음 — 지금 레벨 + 쌓인 레벨업이 상한을 넘지 않게, 넘친 경험치는 막대를 가득 채운 채 멈춤
        int room = LevelCap - (p.level + PendingLevels);
        if (room <= 0)
        {
            p.nowEXP = Mathf.Min(p.nowEXP, p.needEXP);
            return;
        }
        int gained = 0;
        while (p.nowEXP >= p.needEXP && p.needEXP > 0f && gained < room)
        {
            p.nowEXP -= p.needEXP;
            earned++;
            p.needEXP = PlayerController.NeedExp(1 + earned);
            gained++;
        }
        if (gained >= room) p.nowEXP = Mathf.Min(p.nowEXP, p.needEXP);
        if (gained == 0) return;
        // 창이 꺼져 있으면 연출 없이 포인트만
        if (!isActiveAndEnabled) { AddPending(gained); return; }
        for (int i = 0; i < gained; i++) StartCoroutine(LevelUpSequence(p, levelUpQueued++ * 0.35f));
    }

    bool freePick;
    // 경험치로 얻은 레벨업 횟수 (경험치 요구량은 이 횟수로 오름 · 레벨 자체는 카드를 고를 때 오름)
    int earned;

    // 한꺼번에 레벨업 포인트를 줌 (무한 모드는 레벨 10 만큼 미리)
    public void GrantLevels(PlayerController p, int n)
    {
        if (p == null || n <= 0) return;
        earned += n;
        p.needEXP = PlayerController.NeedExp(1 + earned);
        AddPending(n);
    }

    IEnumerator LevelUpSequence(PlayerController p, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (p != null) Juice.LevelUp(p.transform.position);
        levelUpShowing++;
        showLv = true;
        yield return new WaitForSeconds(1f);
        levelUpShowing--;
        levelUpQueued = Mathf.Max(0, levelUpQueued - 1);
        showLv = levelUpShowing > 0;
        AddPending();                       // 바로 창을 띄우지 않고 쌓아 둠 ([Space]로 고름)
    }

    Shop shopRef;
    bool ShopOpen
    {
        get
        {
            if (shopRef == null) shopRef = FindFirstObjectByType<Shop>();
            return shopRef != null && shopRef.isShopOpen;
        }
    }

    // 상점이 열려 있는 동안은 레벨업 표시가 화면을 가리지 않게
    void LateUpdate()
    {
        if (LvUpPanel != null && LvUpPanel.activeSelf != (showLv && !ShopOpen)) LvUpPanel.SetActive(showLv && !ShopOpen);
    }
    public bool IsOpen => LvshopPanel != null && LvshopPanel.activeSelf;

    // 지금 고를 수 있는 레벨업 카드가 하나도 없음 = 만렙 (무기가 바뀌어 맞는 카드가 생기면 다시 오름)
    public bool NothingToLearn
    {
        get
        {
            if (Total_abilitys <= 0 || ability_selected == null) return false;
            for (int i = 0; i < Total_abilitys && i < ability_selected.Length; i++)
                if (!ability_selected[i] && KitFits(i) && !SigLocked(i)) return false;
            return true;
        }
    }

    // 처음 레벨업 창 몇 번은 안내를 하나씩 늘림 (1.0.2 — 첫 창에 도움말 · 추천 · 시너지 · 다시 뽑기가 한꺼번에 떠 복잡했음)
    // 1번째 창: 추천만 · 2번째: + 시너지 · 3번째부터: + 다시 뽑기 (게임 전체에서 한 번만, 저장)
    const string IntroKey = "intro.lvopens";
    static int IntroOpens => Prefs.GetInt(IntroKey, 0);
    static bool ShowSynergyIntro => IntroOpens >= 2;
    static bool ShowRerollIntro => IntroOpens >= 3;

    public void openLevelShop()
    {
        if (IntroOpens < 3 && !GameInput.Auto) Prefs.SetInt(IntroKey, IntroOpens + 1);
        setAbilitys();                      // 창을 열 때 최신 수치로
        
        LvshopPanel.SetActive(true);
        EnsureSkipButton();
        DisarmSkip();                       // 1.0.4~: 건너뛰기 두 번 누르기를 처음 상태로
        evoIntroShowing = false;            // 1.0.4~: 스킬 진화 첫 안내는 처음 본 창에서만
        LockSelection();
        UpdateLvShopContent();
        pendingSlot = -1;
        RefreshCards();
        

    }
    void closeLevelShop()
    {
        if (LvshopPanel != null) LvshopPanel.SetActive(false);
        Time.timeScale = 1f;
    }
    private int first = 0, second = 1, third = 2;
    void UpdateLvShopContent(bool reroll = false)
    {
        // 아직 고를 수 있는 카드 중에서 세 장 (모자라면 성장 카드, 1.0.4~ — 예전엔 비상 보급)
        System.Collections.Generic.List<int> open = new System.Collections.Generic.List<int>();
        // 최대 레벨이면 성장 카드만 (1.0.4~, LevelShop.Growth)
        bool growthOnly = GrowthOnly;
        ResetGrowthDeck();
        // 1.0.4~: 다시 뽑기면 방금 보여 준 세 장은 뒤로 미룸 (다른 카드가 모자랄 때만 다시 나옴 — 같은 카드가 또 나오던 문제)
        System.Collections.Generic.List<int> spare = new System.Collections.Generic.List<int>();
        // 지금 무기에 안 맞는 카드는 빼고 (예: 영혼 저격총에 유도 탄두) — 무기가 바뀌면 다시 나옴
        if (!growthOnly)
            for (int i = 0; i < Total_abilitys; i++)
            {
                if (ability_selected[i] || !KitFits(i) || SigLocked(i)) continue;
                if (reroll && (i == first || i == second || i == third)) spare.Add(i);
                else open.Add(i);
            }
        if (open.Count + spare.Count < 3) GrowthHint();
        // 스킬 진화에 가까운 카드일수록 조금 더 잘 나옴 (가중치 1 ~ 1.8, LevelShop.Signature)
        int Draw()
        {
            if (open.Count == 0 && spare.Count > 0) { open.AddRange(spare); spare.Clear(); }
            if (open.Count == 0) return DrawGrowth();
            float total = 0f;
            foreach (int c in open) total += DrawWeight(c);
            float roll = Random.Range(0f, total);
            int k = open.Count - 1;
            for (int i = 0; i < open.Count; i++)
            {
                roll -= DrawWeight(open[i]);
                if (roll < 0f) { k = i; break; }
            }
            int id = open[k];
            open.RemoveAt(k);
            return id;
        }

        // 2026-10-16: 카드 설명을 완결 문장으로 다듬어 영어 · 일본어가 길어짐 — 씬의 최소 18pt 로는 칸 밖으로 넘칠 수 있어 15pt 까지 줄어들게
        foreach (TextMeshProUGUI ab in new[] { FirstAbility, SecondAbility, ThirdAbility })
            if (ab != null && ab.fontSizeMin > 15f) ab.fontSizeMin = 15f;

        //첫번째 능력 선택
        first = Draw();

        FirstTitle.text = CardTitleOf(first);
        FirstAbility.text = CardBodyOf(first);

        //두번째 능력 선택
        second = Draw();

        SecondTitle.text = CardTitleOf(second);
        SecondAbility.text = CardBodyOf(second);

        //세번째 능력 선택
        third = Draw();

        ThirdTitle.text = CardTitleOf(third);
        ThirdAbility.text = CardBodyOf(third);

        SetCardIcon(FirstIcon, first);
        SetCardIcon(SecondIcon, second);
        SetCardIcon(ThirdIcon, third);
        ShowEvoHints();

        Time.timeScale = 0f;
    }

    SkillGauge skill;

    const int MinSkillPoint = 10;
    const int RecycleId = 2;

    static string Percent(float rate) => Mathf.RoundToInt(rate * 100f) + "%";
    const float MagnetStep = 4f;            // 코인 자석 1회당 끌어오는 범위
    const int MagnetMaxLevel = 4;
    const float DefStep = 0.12f;            // 단단한 신체 1회당 받는 피해 감소
    const int DefMaxLevel = 3;

    // 밀어내기 표시용 기본 넉백 값 (PlayerController.knockBack 초기값)
    const float BaseKnockBack = 0.3f;
    const int AbilityCount = SigFirstId + SigCount;      // 공용 · 전용 15장 + 고유 스킬 10장
    // 끝없이 고를 수 있던 카드의 상한
    const int CoinMaxLevel = 4, ExpMaxLevel = 4, HeartMaxLevel = 4, GlareMaxLevel = 3;
    const float RegenStep = 0.5f;       // 생명의 샘 1회당 초당 회복량
    const int RegenMaxLevel = 4;
    const float HealOnKillStep = 1f;    // 피의 굶주림 1회당 처치 회복량
    const int HealOnKillMaxLevel = 3;
    const float KnockBackGrowth = 1.25f;
    const int KnockBackMaxLevel = 3;

    // 툴팁용: 카드와 같은 글 (한 문장 + 수치 지금 → 다음 + Lv)
    public string GetAbilityTooltip(int id)
    {
        if (bul == null) bul = FindFirstObjectByType<PlayerController>();
        if (lv == null) lv = FindFirstObjectByType<Level>();
        if (skill == null) skill = FindFirstObjectByType<SkillGauge>();
        if (bul == null) return "";
        // 1.1.2~ 카드 · 진화 재료 칸은 단계를 칸(LevelPips)으로만 보여 주므로 정확한 숫자는 마우스를 올린 설명에만
        string lvLine = HudView ? "" : StepTip(id);
        if (KitTooltip(id, out string kitTip)) return kitTip + lvLine;
        return SharedCardText(id) + lvLine;
    }

    // 「Lv 2 · 최대 3」 (최대 단계가 없는 카드는 빈 줄)
    string StepTip(int id)
    {
        int max = MaxLevelOf(CharacterData.Selected, id);
        return max > 0 ? "\n<color=#A89C86>Lv " + LevelOf(id) + "  ·  " + Loc.T("최대") + " " + max + "</color>" : "";
    }

    // 공용 카드 설명: 한 문장 + 금색 "수치  지금 → 다음" + Lv
    string SharedCardText(int id)
    {
        if (bul == null) return "";
        int n = id < ability_level.Length ? ability_level[id] : 0;
        string S(string ko) => Loc.T(ko);
        int Rem(int max) => Mathf.Max(0, max - n);        // 최대까지 남은 단계 (설명의 최대 수치)
        switch (id)
        {
            case 0:
                return CardText(S("총알이 적을 뚫고 지나갑니다."), S("관통"), (bul.pene - 1) + S("마리"), bul.pene + S("마리"), n, 0);
            case 1:
                return CardText(S("코인을 주울 때 더 많이 얻습니다."), S("코인 1개당 획득량"), (1 + bul.bonusCoin).ToString(), (2 + bul.bonusCoin).ToString(), n, CoinMaxLevel, (1 + bul.bonusCoin + Rem(CoinMaxLevel)).ToString());
            case 2:
                {
                    float pps = skill != null ? skill.pointsPerSecond : 1f;
                    int max = skill != null ? skill.MaxSkillPoint : 10;
                    int nextMax = Mathf.Max(MinSkillPoint, Mathf.RoundToInt(max * 0.8f));
                    return CardText(S("필살기 게이지가 더 빨리 가득 찹니다. (적을 처치하면 잠깐 더 빨라짐)"), S("게이지가 가득 차는 시간"),
                                    (max / pps).ToString("0") + S("초"), (nextMax / pps).ToString("0") + S("초"), n, 0);
                }
            case 3:
                return CardText(S("스킬을 쓰는 동안 적이 더 느려집니다."), S("스킬 중 시간 속도"),
                                (bul.Skill_setTime * 100f).ToString("0.#") + "%", (bul.Skill_setTime * 80f).ToString("0.#") + "%", n, GlareMaxLevel,
                                (bul.Skill_setTime * Mathf.Pow(0.8f, Rem(GlareMaxLevel)) * 100f).ToString("0.#") + "%");
            case 4:
                {
                    float bonus = lv != null ? lv.bonusEXP : 1f;
                    return CardText(S("적을 처치할 때 얻는 경험치가 늘어납니다."), S("경험치 배율"),
                                    (bonus * 100f).ToString("0") + "%", ((bonus + 0.1f) * 100f).ToString("0") + "%", n, ExpMaxLevel,
                                    ((bonus + 0.1f * Rem(ExpMaxLevel)) * 100f).ToString("0") + "%");
                }
            case 5:
                return CardText(S("주변의 코인을 끌어옵니다."), S("끌어오는 범위"),
                                bul.coinMagnetRange.ToString("0") + S("칸"), (bul.coinMagnetRange + MagnetStep).ToString("0") + S("칸"), n, MagnetMaxLevel,
                                (bul.coinMagnetRange + MagnetStep * Rem(MagnetMaxLevel)).ToString("0") + S("칸"));
            case 6:
                return CardText(S("한 번에 여러 발을 부채꼴로 발사합니다. 발사 수가 늘수록 한 발당 피해는 줄어듭니다."), S("발사 수 · 한 발당 피해"),
                                bul.multiShot + S("발") + " " + Percent(bul.MultiShotDamageRate(bul.multiShot)),
                                (bul.multiShot + 1) + S("발") + " " + Percent(bul.MultiShotDamageRate(bul.multiShot + 1)), n, 4,
                                (bul.multiShot + Rem(4)) + S("발") + " " + Percent(bul.MultiShotDamageRate(bul.multiShot + Rem(4))));
            case 7:
                return CardText(S("총알이 적을 더 멀리 밀어냅니다."), S("넉백"),
                                (bul.knockBack / BaseKnockBack * 100f).ToString("0") + "%", (bul.knockBack * KnockBackGrowth / BaseKnockBack * 100f).ToString("0") + "%", n, KnockBackMaxLevel,
                                (bul.knockBack * Mathf.Pow(KnockBackGrowth, Rem(KnockBackMaxLevel)) / BaseKnockBack * 100f).ToString("0") + "%");
            case 8:
                return CardText(S("최대 체력이 늘어나고, 선택하는 순간 체력을 모두 회복합니다."), S("최대 체력"),
                                Mathf.RoundToInt(bul.PlayerMaxHealth).ToString(), Mathf.RoundToInt(bul.PlayerMaxHealth * 1.12f).ToString(), n, HeartMaxLevel,
                                Mathf.RoundToInt(bul.PlayerMaxHealth * Mathf.Pow(1.12f, Rem(HeartMaxLevel))).ToString());
            case 9:
                return CardText(S("적에게 받는 피해가 줄어듭니다."), S("받는 피해 감소"),
                                (Mathf.Min(bul.def, PlayerController.MaxDef) * 100f).ToString("0") + "%", (Mathf.Min(bul.def + DefStep, PlayerController.MaxDef) * 100f).ToString("0") + "%", n, DefMaxLevel,
                                (Mathf.Min(bul.def + DefStep * Rem(DefMaxLevel), PlayerController.MaxDef) * 100f).ToString("0") + "%");
            case 10:
                return CardText(S("시간이 지나면 체력이 조금씩 회복됩니다."), S("초당 회복"),
                                bul.regenPerSecond.ToString("0.#"), (bul.regenPerSecond + RegenStep).ToString("0.#"), n, RegenMaxLevel,
                                (bul.regenPerSecond + RegenStep * Rem(RegenMaxLevel)).ToString("0.#"));
            case 11:
                return CardText(S("적을 처치할 때마다 체력을 회복합니다. (처치 회복은 초당 최대 6)"), S("처치당 회복"),
                                bul.healOnKill.ToString("0.#"), (bul.healOnKill + HealOnKillStep).ToString("0.#"), n, HealOnKillMaxLevel,
                                (bul.healOnKill + HealOnKillStep * Rem(HealOnKillMaxLevel)).ToString("0.#"));
            case SupplyId:
                return S("고를 능력을 모두 배웠습니다.") + "\n<color=#F5D478>" + S("보급") + "  " + S("체력 +30% · 코인 +10") + "</color>";
        }
        return "";
    }

    // 카드를 누르면 고르기만 하고, 확정은 스페이스바
    public void onFirstButton() => PickCard(0);
    public void onSecondButton() => PickCard(1);
    public void onThirdButton() => PickCard(2);
    void SetCardIcon(Image target, int id)
    {
        if (target == null || abilityHUD == null) return;

        Sprite icon = IsGrowth(id) ? GrowthIcon(id - GrowthFirstId) : KitIcon(id) ?? abilityHUD.GetIcon(id);
        if (icon != null) target.sprite = icon;
    }

    void onSelect(int what)
    {
        if (!selectReady) return;
        selectReady = false;
        pendingSlot = -1;
        RefreshCards();
        skill = FindFirstObjectByType<SkillGauge>();
        bul = FindFirstObjectByType<PlayerController>();
        closeLevelShop();
        RunSave.Record((freePick ? "f" : "c") + what);         // 판 중간 저장 (2.1.9): 고른 카드를 순서대로
        // 레벨은 카드를 고를 때 오름 (건너뛰면 그대로 · 시작할 때 주는 카드는 제외) · 최대 레벨에서 멈춤 (그 뒤는 성장 카드, 1.0.4~)
        if (freePick) freePick = false;
        else if (bul != null && bul.level < LevelCap) bul.level++;
        // 성장 카드 (1.0.4~): 능력 배열 밖이라 HUD · 진화 확인 없이 효과만
        if (IsGrowth(what))
        {
            ApplyGrowth(what - GrowthFirstId);
            return;
        }

        // 비상 보급은 능력이 아니라 HUD에 남기지 않음
        if (what != SupplyId)
        {
            ability_level[what]++;
            if (abilityHUD != null) abilityHUD.SetAbility(what, ability_level[what]);
        }
        if (what == SupplyId)
        {
            bul.PlayerHealth = Mathf.Min(bul.PlayerMaxHealth, bul.PlayerHealth + bul.PlayerMaxHealth * 0.3f);
            Coin c = FindFirstObjectByType<Coin>();
            if (c != null) c.AddCoin(10);
            return;
        }
        ApplyPick(what);
        CheckEvolutions();          // 재료가 모두 최대면 스킬 진화
    }

    // ================================================================= 판 중간 저장 (RunSave, 2.1.9~)
    // 기록한 카드를 연출 없이 다시 고름 (free: 판을 시작할 때 준 카드라 레벨이 오르지 않음)
    public void ReplayPick(int what, bool free)
    {
        if (IsGrowth(what))
        {
            // 성장 카드 (1.0.4~): 고를 때와 같이 최대 레벨에서는 레벨이 오르지 않음
            bul = FindFirstObjectByType<PlayerController>();
            if (bul == null) return;
            if (!free && bul.level < LevelCap) bul.level++;
            ApplyGrowth(what - GrowthFirstId);
            return;
        }
        if (what < 0 || what >= ability_level.Length) return;
        skill = FindFirstObjectByType<SkillGauge>();
        bul = FindFirstObjectByType<PlayerController>();
        if (lv == null) lv = FindFirstObjectByType<Level>();
        if (bul == null) return;
        if (!free && bul.level < LevelCap) bul.level++;
        ability_level[what]++;
        if (abilityHUD != null) abilityHUD.SetAbility(what, ability_level[what]);
        if (what == SupplyId) return;
        ApplyPick(what);
        CheckEvolutions();
    }

    // 판을 시작할 때 뜨는 첫 카드 창을 닫음 (이어하기는 기록한 첫 카드로 대신)
    public void CancelOpening()
    {
        if (IsOpen) closeLevelShop();
        freePick = false;
        pendingSlot = -1;
        Time.timeScale = 1f;
    }

    // 저장: 경험치로 얻은 레벨업 횟수 · 아직 고르지 않은 레벨업 (창이 열려 있거나 연출 중인 것도 포함)
    public void SaveLevels(out int earnedLevels, out int pending)
    {
        earnedLevels = earned;
        pending = PendingLevels + levelUpQueued + (IsOpen && !freePick ? 1 : 0);
    }

    public void RestoreLevels(int earnedLevels, int pending, float nowExp, float needExp)
    {
        if (bul == null) bul = FindFirstObjectByType<PlayerController>();
        earned = Mathf.Max(0, earnedLevels);
        PendingLevels = Mathf.Max(0, pending);      // AddPending 은 영혼 트리 '깨달음'을 한 번 더 발동시키므로 바로 넣음
        if (bul != null)
        {
            bul.needEXP = needExp > 0f ? needExp : PlayerController.NeedExp(1 + earned);
            bul.nowEXP = Mathf.Clamp(nowExp, 0f, bul.needEXP);
        }
    }

    void ApplyPick(int what)
    {
        if (SigApply(what)) return;
        if (KitApply(what)) return;
        if (what == 0) bul.pene++;
        if (what == 1)
        {
            bul.bonusCoin++;
            if (ability_level[1] >= CoinMaxLevel) ability_selected[1] = true;
        }
        if (what == 2)
        {
            // 너무 쉬워지지 않도록 최소 5회까지만 줄어듦
            skill.SetMax(Mathf.Max(MinSkillPoint, Mathf.RoundToInt(skill.MaxSkillPoint * 0.8f)));
            if (skill.MaxSkillPoint <= MinSkillPoint) ability_selected[2] = true;
        }
        if (what == 3)
        {
            bul.Skill_setTime *= 0.8f;
            if (ability_level[3] >= GlareMaxLevel) ability_selected[3] = true;
        }
        if (what == 4)
        {
            lv.bonusEXP += 0.1f;
            if (ability_level[4] >= ExpMaxLevel) ability_selected[4] = true;
        }
        if (what == 5)
        {
            bul.coinMagnetRange += MagnetStep;
            if (ability_level[5] >= MagnetMaxLevel) ability_selected[5] = true;
        }
        if (what == 6)
        {
            bul.multiShot += 1;
            if (bul.multiShot >= 5) ability_selected[6] = true;
        }
        if (what == 7)
        {
            bul.knockBack *= KnockBackGrowth;
            if (ability_level[7] >= KnockBackMaxLevel) ability_selected[7] = true;
        }
        if (what == 8)
        {
            bul.PlayerMaxHealth = Mathf.Round(bul.PlayerMaxHealth * 1.12f);
            bul.PlayerHealth = bul.PlayerMaxHealth;
            // 최대 체력이 끝없이 불어나지 않게
            if (ability_level[8] >= HeartMaxLevel) ability_selected[8] = true;
        }
        if (what == 9)
        {
            bul.def += DefStep;
            if (ability_level[9] >= DefMaxLevel) ability_selected[9] = true;
        }
        if (what == 10)
        {
            bul.regenPerSecond += RegenStep;
            if (ability_level[10] >= RegenMaxLevel) ability_selected[10] = true;
        }
        if (what == 11)
        {
            bul.healOnKill += HealOnKillStep;
            if (ability_level[11] >= HealOnKillMaxLevel) ability_selected[11] = true;
        }
    }
    


}

// 1.1.9~ HUD 배지(「레벨업!」)를 마우스로 누를 수 있게: 왼쪽 클릭만 받고, 올려 둔 동안 Hover
public class BadgeClick : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
{
    public System.Action click;
    public bool Hover { get; private set; }

    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData e)
    {
        if (e.button != UnityEngine.EventSystems.PointerEventData.InputButton.Left || GameInput.Auto) return;
        UISound.Click();
        click?.Invoke();
    }

    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) => Hover = true;
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) => Hover = false;
    void OnDisable() => Hover = false;
}
