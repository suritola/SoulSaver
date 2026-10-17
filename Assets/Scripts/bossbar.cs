using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class bossbar : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject bar;
    public GameObject backBar;
    public int NowHealth;
    public int MaxHealth;

    public float MaxBarX = 1333f;

    public float BarX;
    public float BarY = 119.2869f;

    public bool bossSpawn = false;
    public const int DeveloperKind = 9;       // 무한 모드 최종 보스 「개발자」 (이름만 · 1.0.2)
    public const int DeveloperMomKind = 10;   // 1.0.4~ 30분의 「개발자 엄마」 (이름만)
    public int bossKind = 0;            // 0 = 리치 왕, 1 = 지옥의 군주, 2 = 킹 슬라임, 3 = 거울의 군주 (보스가 매 프레임 알려줌)
    public float erosion = 1f;          // 1.2.3~ 무한 모드 모래 폭풍 풍화 배율 (보스가 매 프레임 알려줌 · 이름 뒤 「풍화 +n%」)

    TMP_Text nameText;
    RectTransform barRect;
    float nextNameSearch;
    int shownKind = -1;
    Loc.Lang shownLang;

    public static string BossName(int kind) => kind switch
    {
        1 => Loc.T("지옥의 군주"),
        2 => Loc.T("킹 슬라임"),
        3 => Loc.T("거울의 군주"),
        DeveloperKind => Loc.T("개발자"),
        DeveloperMomKind => Loc.T("개발자 엄마"),
        _ => Loc.T("리치 왕"),
    };
    void Start()
    {
        // 씬에 배치된 보스바 크기를 최대치로 사용
        RectTransform barRect = bar.GetComponent<RectTransform>();
        MaxBarX = barRect.sizeDelta.x;
        BarY = barRect.sizeDelta.y;

        HealthBarFeel.ResetStatics();
        BossUltimate.ResetStatics();       // 1.2.5~ 통합 결계 게이지 · 공간 침식 대기 · 쉬는 시간 · 땅 타격 대기열 (장면마다)
        // 조각: 채움 막대와 같은 자리 · 크기의 Image 를 새로 만들어 바로 뒤(형제 순서 한 칸 앞)에 — 아래로 쌓는 줄 복제(backBar 복제)에도 함께 따라감
        // 1.2.0: Instantiate(bar) 복제 대신 (PlayerHP 의 자기 복제 연쇄와 같은 일이 생기지 않게 · HealthBarFeel.MakeGhost 는 스크립트 · 자식을 복제하지 않음)
        Image gi = HealthBarFeel.MakeGhost(bar.GetComponent<Image>(), GhostName);
        main = MakeRow(backBar, barRect, gi != null ? gi.rectTransform : null);
        instance = this;
        AddTip(backBar, main);          // 1.1.9~ 뒤판 툴팁 (뒤판이 raycastTarget 이 됨)
        // 최적화: 보스 체력바 묶음을 하위 캔버스로 — 타격마다 막대 · 흔들림 · 이름이 바뀌어도 HUD 전체를 다시 묶지 않게
        //   (Canvas 컴포넌트만 붙임 · MonoBehaviour 아님 → 아래 줄 복제(ShowExtraBars)에도 그대로 따라가 각자 하위 캔버스)
        //   툴팁(마우스)을 받아야 하므로 레이캐스터도 둠
        UiScaler.Isolate(backBar, true);
    }

    // ================================================================= 체력바 타격감 (HealthBarFeel): 줄마다 흰색 지연 게이지 · 흔들림 · 두께 펀치 · 번쩍임 · 프레임 붉은 틴트
    const string GhostName = "BossGhost";

    sealed class Row
    {
        public RectTransform fill, ghost, root;
        public Image fillImg, ghostImg, backImg;
        public Vector2 rest;
        public Vector3 baseScale = Vector3.one;     // 맨 위 줄 1 · 아래 줄 0.8
        public Color fillColor, backColor;
        public float shownFront = -1f, shownGhost = -1f, shownIgnite = -1f;
        public bosss drawnFor;                      // 지난번에 그린 보스 (바뀌면 지난 보스 값이 남지 않게 다시 그림)
        // 1.2.3~ 이름 글자 캐시 (SetBar): 값이 바뀔 때만 문자열을 새로 (예전엔 여러 줄일 때 매 프레임 줄마다 문자열을 만들어 비교했음)
        public bosss nameFor;
        public int nameKind = -1, nameNow = -1, nameMax = -1, nameErode = -1;
        public Loc.Lang nameLang;
        public float nameAt;                        // 체력 숫자만 바뀔 때 다음 갱신 시각 (0.1초)
        // 1.1.9~ 체력바 눈금 (편의성 2차 12): 실제 단계 자리 — 지옥의 군주 66% · 33%, 나머지 50% · 지나면 빨강 + 뒤판이 붉어짐
        public readonly List<Image> ticks = new List<Image>();
        public int tickKind = -99;
        public Color baseBack;
        public bool baseSet;
        public float enrage;
        public int kind;            // 이 줄의 보스 (툴팁)
        // 기억하는 보스 (BossMemory): 기억 눈금(청록 마름모 · 지나면 회색) · 바 아래 「기억: … n초」 한 줄
        public readonly List<Image> memTicks = new List<Image>();
        public int memTickKind = -99;
        public TMP_Text memLine;
        public string memShown;
        public bool memSearched;
    }
    static readonly Color FrameHit = new Color(1f, 0.72f, 0.72f);
    static readonly Color FrameHitCB = new Color(1f, 0.80f, 0.50f);   // 색각 모드: 빨강 대신 주황

    static readonly float[] HalfTick = { 0.5f }, HellTicks = { 0.66f, 0.33f };
    static readonly float[] NoTicks = new float[0];
    static float[] TicksOf(int kind) => kind >= DeveloperKind ? NoTicks : kind == 1 ? HellTicks : HalfTick;      // 프레임마다 불려서 빈 배열도 하나만
    static readonly Color TickColor = new Color(1f, 0.9f, 0.7f, 0.75f), TickPassed = new Color(1f, 0.4f, 0.35f, 0.9f);
    static readonly Color EnragedBack = new Color(0.35f, 0.08f, 0.08f, 1f);

    void UpdateTicks(Row r, bosss s, int kind)
    {
        if (r == null || r.fill == null || s == null) return;
        Transform parent = r.fill.parent;
        if (r.tickKind != kind)
        {
            r.tickKind = kind;
            // 줄을 복제하면 눈금도 같이 복제되므로 이름으로 다시 찾아 씀
            r.ticks.Clear();
            List<Image> found = new List<Image>();
            foreach (Transform c in parent) if (c.name == "BossTick") { Image im = c.GetComponent<Image>(); if (im != null) found.Add(im); }
            float[] fr = TicksOf(kind);
            for (int i = 0; i < found.Count; i++)
            {
                if (i < fr.Length) r.ticks.Add(found[i]);
                else Destroy(found[i].gameObject);
            }
            while (r.ticks.Count < fr.Length)
            {
                GameObject g = new GameObject("BossTick", typeof(RectTransform), typeof(Image));
                g.transform.SetParent(parent, false);
                g.transform.SetSiblingIndex(r.fill.GetSiblingIndex() + 1);
                Image im = g.GetComponent<Image>();
                im.raycastTarget = false;
                r.ticks.Add(im);
            }
            for (int i = 0; i < r.ticks.Count; i++)
            {
                RectTransform t = r.ticks[i].rectTransform;
                t.anchorMin = r.fill.anchorMin;
                t.anchorMax = r.fill.anchorMax;
                t.pivot = new Vector2(0.5f, 0.5f);
                t.sizeDelta = new Vector2(3f, BarY + 8f);
                float left = r.fill.anchoredPosition.x - r.fill.pivot.x * r.fill.sizeDelta.x;
                float midY = r.fill.anchoredPosition.y + (0.5f - r.fill.pivot.y) * BarY;
                t.anchoredPosition = new Vector2(left + MaxBarX * fr[i], midY);
            }
        }
        float frac = s.setEnemyHP > 0 ? s.EnemyHealth / s.setEnemyHP : 1f;
        float[] at = TicksOf(kind);
        int passed = 0;
        for (int i = 0; i < r.ticks.Count && i < at.Length; i++)
        {
            bool p = frac <= at[i];
            if (p) passed++;
            r.ticks[i].color = p ? TickPassed : TickColor;
        }
        if (!r.baseSet) { r.baseSet = true; r.baseBack = r.backColor; }
        float want = at.Length > 0 ? passed / (float)at.Length : 0f;
        r.enrage = Mathf.MoveTowards(r.enrage, want, Time.unscaledDeltaTime / 0.3f);
        r.backColor = Color.Lerp(r.baseBack, EnragedBack, r.enrage * 0.7f);
    }

    // ================================================================= 기억하는 보스: 기억 눈금 · 「기억: … n초」
    static readonly Color MemTickColor = new Color(0.35f, 1f, 0.9f, 0.95f), MemTickPassed = new Color(0.55f, 0.58f, 0.6f, 0.8f);
    static readonly Color MemLineColor = new Color(0.45f, 1f, 0.92f);
    const string MemTickName = "BossMemTick", MemLineName = "BossMemLine";

    void UpdateMemTicks(Row r, bosss s, int kind)
    {
        if (r == null || r.fill == null || s == null) return;
        Transform parent = r.fill.parent;
        int want = s.memory != null ? kind : -1;      // 기억이 없는 보스(튜토리얼)는 눈금 없음
        if (r.memTickKind != want)
        {
            r.memTickKind = want;
            // 줄을 복제하면 눈금도 같이 복제되므로 이름으로 다시 찾아 씀 (BossTick 과 따로)
            r.memTicks.Clear();
            List<Image> found = new List<Image>();
            foreach (Transform c in parent) if (c.name == MemTickName) { Image im = c.GetComponent<Image>(); if (im != null) found.Add(im); }
            float[] fr = want < 0 ? NoTicks : BossMemory.Thresholds(want);
            for (int i = 0; i < found.Count; i++)
            {
                if (i < fr.Length) r.memTicks.Add(found[i]);
                else Destroy(found[i].gameObject);
            }
            while (r.memTicks.Count < fr.Length)
            {
                GameObject g = new GameObject(MemTickName, typeof(RectTransform), typeof(Image));
                g.transform.SetParent(parent, false);
                g.transform.SetSiblingIndex(r.fill.GetSiblingIndex() + 1);
                Image im = g.GetComponent<Image>();
                im.raycastTarget = false;
                r.memTicks.Add(im);
            }
            float size = Mathf.Clamp(BarY * 0.55f, 8f, 14f);
            for (int i = 0; i < r.memTicks.Count; i++)
            {
                RectTransform t = r.memTicks[i].rectTransform;
                t.anchorMin = r.fill.anchorMin;
                t.anchorMax = r.fill.anchorMax;
                t.pivot = new Vector2(0.5f, 0.5f);
                t.sizeDelta = new Vector2(size, size);
                t.localRotation = Quaternion.Euler(0f, 0f, 45f);       // 마름모
                float left = r.fill.anchoredPosition.x - r.fill.pivot.x * r.fill.sizeDelta.x;
                float topY = r.fill.anchoredPosition.y + (1f - r.fill.pivot.y) * BarY;
                t.anchoredPosition = new Vector2(left + MaxBarX * fr[i], topY);
            }
        }
        if (r.memTicks.Count == 0) return;
        float frac = s.setEnemyHP > 0 ? s.EnemyHealth / s.setEnemyHP : 1f;
        float[] at = BossMemory.Thresholds(want);
        for (int i = 0; i < r.memTicks.Count && i < at.Length; i++)
        {
            Color c = frac <= at[i] ? MemTickPassed : MemTickColor;
            if (r.memTicks[i].color != c) r.memTicks[i].color = c;
        }
    }

    // 바 아래 오른쪽 「기억: 탄환 8초」 (글자는 BossMemory 가 미리 만든 것 · 바뀔 때만 다시 씀)
    void UpdateMemLine(Row r, bosss s)
    {
        if (r == null || r.root == null) return;
        if (r.memLine == null)
        {
            if (!r.memSearched)
            {
                r.memSearched = true;
                Transform found = r.root.Find(MemLineName);       // 줄 복제에 같이 따라온 것
                if (found != null) r.memLine = found.GetComponent<TMP_Text>();
            }
            if (r.memLine == null)
            {
                if (nameText == null) return;           // 보스 이름과 같은 글꼴을 씀
                GameObject go = new GameObject(MemLineName, typeof(RectTransform), typeof(TextMeshProUGUI));
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.SetParent(r.root, false);
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 1f);
                rt.sizeDelta = new Vector2(640f, 24f);
                rt.anchoredPosition = new Vector2(-12f, -2f);
                TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
                t.font = nameText.font;
                t.fontSharedMaterial = nameText.fontSharedMaterial;
                t.fontSize = 19f;
                t.color = MemLineColor;
                t.alignment = TextAlignmentOptions.MidlineRight;
                t.enableWordWrapping = false;
                t.raycastTarget = false;
                t.text = "";
                t.enabled = false;
                r.memLine = t;
            }
            r.memShown = null;
            r.memLine.text = "";
            r.memLine.enabled = false;
        }
        string text = s != null && s.memory != null ? s.memory.BarText : null;
        if (ReferenceEquals(text, r.memShown)) return;
        r.memShown = text;
        r.memLine.enabled = text != null;
        if (text != null) r.memLine.text = text;
    }

    static int KindOf(bosss s) =>s.developer ? (s.developerMom ? DeveloperMomKind : DeveloperKind) : s.bossKind;

    // 1.1.9~ 보스바 뒤판에 마우스 → 이름 · 도감 설명 · 단계 규칙 (전투 중엔 잠깐 머물러야 · 일시정지 중에는 바로)
    static void AddTip(GameObject back, Row r)
    {
        if (back == null) return;
        Image img = back.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;      // hud 툴팁이라 평타를 막지 않음 (PlayerController.PointerOverUI)
        TooltipTrigger old = back.GetComponent<TooltipTrigger>();
        if (old != null) Destroy(old);
        TooltipTrigger t = back.AddComponent<TooltipTrigger>();
        t.hud = true;
        t.titleProvider = () => BossName(r.kind);
        t.bodyProvider = () =>
        {
            if (r.kind >= DeveloperKind) return "";
            string desc = CodexUI.BossDescOf(r.kind);
            string rule = Loc.T(r.kind == 1 ? "체력 66% · 33%에서 단계가 오르고, 결계 게이지가 차면 결계를 펼칩니다." : "체력이 절반 아래로 내려가면 특수 스킬을 쓰고, 결계 게이지가 차면 결계를 펼칩니다.");
            return (desc.Length > 0 ? desc + "\n\n" : "") + "<color=#F5D478>" + rule + "</color>";
        };
    }

    // 1.1.9~ 「분노!」 순간 보스 이름표를 0.4초 붉게
    static bossbar instance;
    float nameFlashUntil = -1f;
    Color nameBase;
    bool nameBaseSet;
    public static void FlashName() { if (instance != null) instance.nameFlashUntil = Time.unscaledTime + 0.4f; }

    void FlashTick()
    {
        if (nameText == null) return;
        if (!nameBaseSet) { nameBaseSet = true; nameBase = nameText.color; }
        float left = nameFlashUntil - Time.unscaledTime;
        nameText.color = left > 0f ? Color.Lerp(nameBase, new Color(1f, 0.35f, 0.3f), Mathf.Clamp01(left / 0.4f) * 1.5f) : nameBase;
    }

    void OnDestroy() { if (instance == this) instance = null; }
    Row main;
    readonly List<Row> extraRows = new List<Row>();

    static Row MakeRow(GameObject back, RectTransform fill, RectTransform ghost)
    {
        Row r = new Row();
        r.root = back.GetComponent<RectTransform>();
        r.rest = r.root.anchoredPosition;
        r.baseScale = r.root.localScale;
        r.backImg = back.GetComponent<Image>();
        if (r.backImg != null) r.backColor = r.backImg.color;
        r.fill = fill;
        r.fillImg = fill != null ? fill.GetComponent<Image>() : null;
        if (r.fillImg != null) r.fillColor = r.fillImg.color;
        r.ghost = ghost;
        r.ghostImg = ghost != null ? ghost.GetComponent<Image>() : null;
        return r;
    }

    // 줄 하나 그리기: 앞 막대 · 조각 · 흔들림 · 번쩍임 (값이 같으면 크기는 다시 쓰지 않음)
    void Draw(Row r, bosss s)
    {
        if (r == null || s == null) return;
        if (r.drawnFor != s) { r.drawnFor = s; r.shownFront = r.shownGhost = -1f; }
        r.kind = KindOf(s);
        UpdateTicks(r, s, r.kind);
        UpdateMemTicks(r, s, r.kind);
        UpdateMemLine(r, s);
        s.BarFeel.Tick(s.EnemyHealth, s.setEnemyHP, HealthBarFeel.Dt, out HealthBarFeel.Frame f);
        if (r.fill != null && f.front != r.shownFront) { r.shownFront = f.front; r.fill.sizeDelta = new Vector2(f.front * MaxBarX, BarY); }
        if (r.ghostImg != null && (f.ghost != r.shownGhost || r.ghostImg.enabled != f.ghostShown))     // 맞는 순간엔 흰 게이지 값이 그대로라 켜짐 여부도 같이 봄
        {
            r.shownGhost = f.ghost;
            if (r.ghostImg.enabled != f.ghostShown) { r.ghostImg.enabled = f.ghostShown; r.shownIgnite = -1f; }
            if (f.ghostShown) r.ghost.sizeDelta = new Vector2(f.ghost * MaxBarX, BarY);
        }
        // 흔들림: 실제 BarY 는 씬 크기 20 (인스펙터 기본값 119 아님) → 높이 비율 그대로 (Big 9px · Deadly 16px · 예전 ×0.25 는 1~2px 로 안 보였음)
        if (r.root != null)
        {
            Vector2 pos = r.rest + new Vector2(f.shakeX, f.shakeY) * BarY;
            if (pos != r.root.anchoredPosition) r.root.anchoredPosition = pos;
            // 두께 펀치 (피벗이 위쪽이라 아래로 자람 · 가로는 그대로)
            Vector3 sc = new Vector3(r.baseScale.x, r.baseScale.y * (1f + f.punch), r.baseScale.z);
            if (sc != r.root.localScale) r.root.localScale = sc;
        }
        if (r.fillImg != null)
        {
            Color fc = f.flash > 0f ? Color.Lerp(r.fillColor, Color.white, f.flash) : r.fillColor;
            if (fc != r.fillImg.color) r.fillImg.color = fc;
        }
        if (r.ghostImg != null && r.ghostImg.enabled && f.ignite != r.shownIgnite) { r.shownIgnite = f.ignite; r.ghostImg.color = HealthBarFeel.GhostColor(f.ignite); }
        // Big 이상: 프레임이 붉게 물듦 (흰 틴트 프레임은 더 밝힐 수 없음)
        if (r.backImg != null)
        {
            Color bc = f.backFlash > 0f ? Color.Lerp(r.backColor, GameSettings.ColorBlind ? FrameHitCB : FrameHit, f.backFlash) : r.backColor;
            if (bc != r.backImg.color) r.backImg.color = bc;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!bossSpawn)
        {
            backBar.SetActive(false);
            ShowExtraBars(0);
            return;
        }
        backBar.SetActive(true);
        UpdateName();
        FlashTick();
        UpdateUltGauge();
        if (barRect == null) barRect = bar.GetComponent<RectTransform>();

        // 킹 슬라임이 갈라진 뒤에는 슬라임마다 따로 보스 체력바 (첫 줄 + 아래로 쌓음)
        if (bossKind == 2)
        {
            bosss.CollectLiveSlimes(liveSlimes);
            bool split = liveSlimes.Exists(s => s.slimeGen >= 2);
            if (split && liveSlimes.Count > 0)
            {
                // 슬라임마다 자기 체력 (각자 따로 깎이고, 이름 번호는 그 개체에 고정)
                SetBar(main, nameText, liveSlimes[0]);
                Draw(main, liveSlimes[0]);
                ShowExtraBars(liveSlimes.Count - 1);
                for (int i = 1; i < liveSlimes.Count; i++) { SetBar(extraRows[i - 1], extraNames[i - 1], liveSlimes[i]); Draw(extraRows[i - 1], liveSlimes[i]); }
                wasMulti = true;
                return;
            }
        }
        // 무한 모드 후반 여러 보스가 함께 있을 때 (1.0.2): 보스마다 한 줄씩
        if (bosss.LiveCount >= 2)
        {
            liveBosses.Clear();
            liveBosses.AddRange(bosss.Live);
            SetBar(main, nameText, liveBosses[0]);
            Draw(main, liveBosses[0]);
            ShowExtraBars(liveBosses.Count - 1);
            for (int i = 1; i < liveBosses.Count; i++) { SetBar(extraRows[i - 1], extraNames[i - 1], liveBosses[i]); Draw(extraRows[i - 1], liveBosses[i]); }
            wasMulti = true;
            return;
        }
        // 슬라임마다 그리던 것에서 돌아오면 첫 줄 이름을 다시 씀
        if (wasMulti) { wasMulti = false; shownKind = -1; }
        ShowExtraBars(0);
        WriteSingleName();
        if (MaxHealth <= 0) return;
        BarX = Mathf.Clamp01((float)NowHealth / (float)MaxHealth) * MaxBarX;
        if (bosss.Primary != null) Draw(main, bosss.Primary);
        else
        {
            // 보스 본체 없이 바만 켠 경우 (예전 방식 · 조각 · 흔들림 없음)
            barRect.sizeDelta = new Vector2(BarX, BarY);
            if (main != null)
            {
                main.shownFront = main.shownGhost = -1f;
                main.drawnFor = null;
                if (main.ghostImg != null) main.ghostImg.enabled = false;
                if (main.root != null) { main.root.anchoredPosition = main.rest; main.root.localScale = main.baseScale; }
                if (main.backImg != null) main.backImg.color = main.backColor;
                if (main.fillImg != null) main.fillImg.color = main.fillColor;
            }
        }
    }

    // ================================================================= 분열한 킹 슬라임: 슬라임마다 보스 체력바
    const float ExtraGap = 8f;
    const float ExtraScale = 0.8f;      // 아래로 쌓는 체력바는 조금 작게 (화면을 덜 가리게)
    bool wasMulti;
    readonly List<bosss> liveSlimes = new List<bosss>();
    readonly List<bosss> liveBosses = new List<bosss>();
    readonly List<GameObject> extraBars = new List<GameObject>();
    readonly List<TMP_Text> extraNames = new List<TMP_Text>();

    // 줄 이름 글자 (막대 길이는 Draw 가 그림) · 1.2.3~ 보스 · 체력 숫자 · 풍화 · 언어가 바뀔 때만 글자를 새로
    void SetBar(Row r, TMP_Text label, bosss s)
    {
        if (r == null || label == null || s == null) return;
        int kind = s.developer ? (s.developerMom ? DeveloperMomKind : DeveloperKind) : s.bossKind == 2 && s.slimeGen >= 2 ? 100 + s.barSlot : s.bossKind;
        int now = Mathf.CeilToInt(Mathf.Max(0f, s.EnemyHealth));
        int erode = s.developer ? 0 : ErodePct(s.erosionMul);
        if (r.nameFor == s && r.nameKind == kind && r.nameMax == s.setEnemyHP && r.nameErode == erode && r.nameLang == Loc.Current)
        {
            // 최적화: 체력 숫자만 바뀐 것은 0.1초에 한 번만 글자를 새로 (초당 수십 타에서도 눈에 같음 · 막대는 매 타격 즉시)
            if (r.nameNow == now || Time.unscaledTime < r.nameAt) return;
        }
        r.nameAt = Time.unscaledTime + 0.1f;
        r.nameFor = s; r.nameKind = kind; r.nameNow = now; r.nameMax = s.setEnemyHP; r.nameErode = erode; r.nameLang = Loc.Current;
        label.text = (kind >= 100 ? BossName(2) + " " + (s.barSlot + 1) : BossName(kind)) + "  " + now + " / " + s.setEnemyHP + ErodeTag(erode);
    }

    // 1.2.3~ 모래 폭풍 풍화: 받는 피해가 몇 % 늘었는지 (10% 단위 내림 · 글자가 덜 바뀌게)
    static int ErodePct(float mul) => mul <= 1f ? 0 : Mathf.FloorToInt((mul - 1f) * 10f + 0.001f) * 10;
    static string ErodeTag(int pct) => pct > 0 ? "  <color=#F0C060>" + Loc.T("풍화") + " +" + pct + "%</color>" : "";

    // 위쪽 보스 체력바(배경 · 채움 · 이름)를 복제해 아래로 쌓음
    void ShowExtraBars(int count)
    {
        while (extraBars.Count < count)
        {
            // backBar 아래에는 스스로 복제하는 스크립트가 없어야 함 (Image · TMP · 흰 게이지 Image · 눈금 Image 뿐) — 그런 스크립트가 붙으면 복제마다 따라붙음 (1.2.0 렉 원인과 같은 경로)
            //   뒤판의 TooltipTrigger(1.1.9~)는 복제되지만 아무것도 만들지 않고, 아래 AddTip 이 지우고 새로 붙임
            GameObject clone = Instantiate(backBar, backBar.transform.parent);
            clone.name = backBar.name + "_" + (extraBars.Count + 1);
            // 1.1~: 복제는 캔버스 맨 뒤에 붙어 레벨업 · 일시정지 창 위에 그려졌음 → 원래 체력바 바로 뒤 순서로
            clone.transform.SetSiblingIndex(backBar.transform.GetSiblingIndex() + 1 + extraBars.Count);
            Transform cg = clone.transform.Find("UltGauge");        // 필살기 게이지는 맨 위 줄에만
            if (cg != null) Destroy(cg.gameObject);
            RectTransform r = clone.GetComponent<RectTransform>();
            // 바 위의 이름 글자 · 왼쪽 해골 아이콘까지 포함한 높이로 쌓음
            // (예전엔 바 높이만큼만 내려서, 아래 체력바의 이름이 윗 체력바를 덮었음)
            Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(backBar.transform);
            // 흔들리는 중이어도 제자리(rest) 기준으로 쌓음
            Vector2 mainPos = main != null ? main.rest : backBar.GetComponent<RectTransform>().anchoredPosition;
            float prevBottom = extraRows.Count == 0
                ? mainPos.y + b.min.y
                : extraRows[extraRows.Count - 1].rest.y + b.min.y * ExtraScale;
            r.localScale = Vector3.one * ExtraScale;
            r.anchoredPosition = new Vector2(mainPos.x, prevBottom - ExtraGap - b.max.y * ExtraScale);
            Transform fill = clone.transform.Find(bar.name);
            Transform ghost = clone.transform.Find(GhostName);          // 위쪽 줄의 흰 게이지도 함께 복제됨
            TMP_Text label = null;
            foreach (TMP_Text t in clone.GetComponentsInChildren<TMP_Text>(true))
                if (t.name == "BossName") { label = t; t.name = "BossNameExtra"; FitName(t); }    // 원래 이름 글자 찾기와 헷갈리지 않게
            extraBars.Add(clone);
            extraNames.Add(label);
            Row row = MakeRow(clone, fill as RectTransform, ghost as RectTransform);
            // 복제 순간 원래 줄이 번쩍이는 중이었을 수 있으니 색은 원래 줄의 기본색으로
            if (main != null) { row.fillColor = main.fillColor; row.backColor = main.baseSet ? main.baseBack : main.backColor; }
            AddTip(clone, row);             // 1.1.9~ 줄마다 자기 보스 설명 (복제는 코드로 채운 툴팁 내용을 잃음)
            extraRows.Add(row);
        }
        for (int i = 0; i < extraBars.Count; i++)
            if (extraBars[i] != null && extraBars[i].activeSelf != (i < count)) extraBars[i].SetActive(i < count);
    }

    // 가장 아래에 보이는 보스 체력바 (알림판을 그 아래로 내릴 때)
    public RectTransform LowestBar
    {
        get
        {
            for (int i = extraBars.Count - 1; i >= 0; i--)
                if (extraBars[i] != null && extraBars[i].activeInHierarchy) return extraBars[i].GetComponent<RectTransform>();
            return backBar != null ? backBar.GetComponent<RectTransform>() : null;
        }
    }

    // ================================================================= 보스 필살기 게이지 (맨 위 체력바 바로 아래, 가득 차면 결계)
    RectTransform ultFill;
    Image ultFillImg;
    float shownUlt = -1f;
    const float UltWidth = 596f;

    void UpdateUltGauge()
    {
        if (ultFill == null)
        {
            GameObject bg = new GameObject("UltGauge", typeof(RectTransform), typeof(Image));
            RectTransform r = bg.GetComponent<RectTransform>();
            r.SetParent(backBar.transform, false);
            r.anchorMin = r.anchorMax = new Vector2(0f, 0f);
            r.pivot = new Vector2(0f, 1f);
            r.sizeDelta = new Vector2(UltWidth, 10f);
            r.anchoredPosition = new Vector2(12f, -4f);
            Image bi = bg.GetComponent<Image>();
            bi.color = new Color(0.08f, 0.04f, 0.12f, 0.9f);
            bi.raycastTarget = false;
            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            ultFill = fill.GetComponent<RectTransform>();
            ultFill.SetParent(r, false);
            ultFill.anchorMin = ultFill.anchorMax = ultFill.pivot = new Vector2(0f, 0.5f);
            ultFill.anchoredPosition = Vector2.zero;
            ultFillImg = fill.GetComponent<Image>();
            ultFillImg.raycastTarget = false;
        }
        // 1.2.5~ 개발자 · 개발자 엄마도 결계(디버그 모드 · 엄마의 대청소)를 씀 → 게이지가 보임 (칸 4 · 5, BossUltimate.Gauge 가 9 · 10 을 바꿔 읽음)
        GameObject gaugeObj = ultFill.parent.gameObject;
        if (!gaugeObj.activeSelf) gaugeObj.SetActive(true);
        // 1.2.5~ 보스가 둘 이상이면 통합 게이지 하나 (맨 위 줄에만 · 「공간 침식 ×n」) · 결계 · 침식 중에는 남은 시간 (1 → 0)
        bool shared = BossUltimate.Shared || BossUltimate.ErosionActive;
        float g = BossUltimate.Active ? BossUltimate.RemainingFrac : shared ? BossUltimate.SharedGauge : BossUltimate.Gauge(bossKind);
        // 최적화: 1/300 단위로 맞춰 바뀔 때만 크기를 씀 (예전엔 매 프레임 같은 값을 썼음)
        float gq = Mathf.Round(g * 300f) / 300f;
        if (gq != shownUlt) { shownUlt = gq; ultFill.sizeDelta = new Vector2(UltWidth * gq, 10f); }
        UltLabel(BossUltimate.Active ? 0f : g, shared);
        Color uc;
        if (BossUltimate.ErosionPending || (shared && !BossUltimate.Active && g >= 1f))
            uc = Mathf.Repeat(Time.unscaledTime * 6f, 1f) < 0.5f ? new Color(1f, 0.25f, 0.3f) : Color.white;       // 가득 참 · 대기: 빨강 · 흰색 빠른 깜빡임
        else if (shared)
        {
            // 진보라 → 핏빛 (값에 따라) · 85%부터 깜빡임
            Color baseC = Color.Lerp(new Color(0.54f, 0.17f, 0.89f), new Color(0.88f, 0.19f, 0.29f), g);
            float pulse = !BossUltimate.Active && g > 0.85f ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f) : 0f;
            uc = Color.Lerp(baseC, Color.white, 0.35f * pulse);
        }
        else
        {
            float pulse = !BossUltimate.Active && g > 0.85f ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f) : 0f;
            uc = Color.Lerp(new Color(0.75f, 0.3f, 1f), new Color(1f, 0.35f, 0.35f), pulse);
        }
        if (ultFillImg.color != uc) ultFillImg.color = uc;
    }

    // 1.0.4~: 이름표 없는 보라색 막대라 뜻을 몰랐음 → 게이지 오른쪽 끝에 「결계」 글자 · 처음 찰 때 도움말 한 번
    TMP_Text ultLabel;
    Loc.Lang ultLabelLang;
    bool ultHintTried;

    int ultLabelCount = -1;     // 1.2.5~ 「공간 침식 ×n」의 n (n · 언어가 바뀔 때만 글자를 새로 씀)

    void UltLabel(float g, bool shared)
    {
        if (ultLabel == null)
        {
            if (nameText == null || ultFill == null) return;      // 보스 이름과 같은 글꼴을 씀
            GameObject go = new GameObject("UltLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(ultFill.parent, false);                  // 게이지 배경 안 (아래로 쌓는 체력바 복제에는 들어가지 않음)
            r.anchorMin = r.anchorMax = new Vector2(1f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.sizeDelta = new Vector2(260f, 22f);         // 1.2.5: 「공간 침식 ×n」 (120 → 260 · 자동 크기)
            r.anchoredPosition = new Vector2(6f, 0f);
            TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
            t.font = nameText.font;
            t.fontSharedMaterial = nameText.fontSharedMaterial;
            t.fontSize = 18f;
            t.enableAutoSizing = true;
            t.fontSizeMin = 14f;
            t.fontSizeMax = 18f;
            t.color = new Color(0.82f, 0.6f, 1f);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            ultLabel = t;
            ultLabelLang = (Loc.Lang)(-1);
        }
        int n = shared ? Mathf.Max(2, BossUltimate.SharedCount) : 0;
        if (ultLabelLang != Loc.Current || ultLabelCount != n)
        {
            ultLabelLang = Loc.Current;
            ultLabelCount = n;
            ultLabel.text = n > 0 ? Loc.T("공간 침식 ×{0}").Replace("{0}", n.ToString()) : Loc.T("결계");
            ultLabel.color = n > 0 ? new Color(1f, 0.55f, 0.65f) : new Color(0.82f, 0.6f, 1f);
        }
        if (!ultLabel.enabled) ultLabel.enabled = true;            // 1.2.5~ 개발자 · 엄마도 결계를 씀
        if (!ultHintTried && g >= 0.15f)
        {
            ultHintTried = true;
            Hints.Show("bossult.gauge", "보스 체력바 아래 보라색 게이지는 「결계」입니다. 보스를 때릴수록 차고, 가득 차면 보스가 결계를 펼칩니다. 결계 전에 보스 체력이 40% 아래로 내려가면 곧바로 가득 찹니다.");
        }
    }

    // 체력바 위 이름을 지금 보스의 이름으로
    void UpdateName()
    {
        if (nameText == null)
        {
            // 이름 글자를 못 찾았을 때 매 프레임 씬 전체를 뒤지지 않게 1초에 한 번만
            if (Time.unscaledTime < nextNameSearch) return;
            nextNameSearch = Time.unscaledTime + 1f;
            foreach (TMP_Text t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "BossName") { nameText = t; break; }
            if (nameText == null) return;
            FitName(nameText);
        }
    }

    // 1.1~: 긴 이름 + 체력 숫자(영어 「Developer's Mom  300000 / 300000」 등)가 두 줄로 꺾여 체력바를 덮었음 → 한 줄 고정, 넘치면 글자를 줄임
    static void FitName(TMP_Text t)
    {
        if (t == null) return;
        float max = t.enableAutoSizing ? t.fontSizeMax : t.fontSize;
        t.enableWordWrapping = false;
        t.fontSizeMax = max;
        t.fontSizeMin = Mathf.Min(26f, max);
        t.enableAutoSizing = true;
    }

    // 보스가 한 마리일 때 이름 + 「현재 / 최대」 (1.0.4~: 예전엔 여러 마리일 때만 숫자가 보였음)
    // 숫자 · 보스 · 언어가 바뀔 때만 글자를 새로
    int shownNow = -1, shownMax = -1, shownErode = -1;
    float singleNameAt;
    void WriteSingleName()
    {
        if (nameText == null) return;
        int now = Mathf.Max(0, NowHealth);
        int erode = bossKind >= DeveloperKind ? 0 : ErodePct(erosion);
        if (shownKind == bossKind && shownLang == Loc.Current && shownMax == MaxHealth && shownErode == erode)
        {
            // 최적화: 체력 숫자만 바뀐 것은 0.1초에 한 번만 (막대는 매 타격 즉시)
            if (shownNow == now || Time.unscaledTime < singleNameAt) return;
        }
        singleNameAt = Time.unscaledTime + 0.1f;
        shownKind = bossKind;
        shownLang = Loc.Current;
        shownNow = now;
        shownMax = MaxHealth;
        shownErode = erode;
        nameText.text = BossName(bossKind) + (MaxHealth > 0 ? "  " + now + " / " + MaxHealth : "") + ErodeTag(erode);
        if (main != null) main.nameFor = null;      // 여러 줄로 돌아가면 첫 줄 이름을 다시 씀
    }
}
