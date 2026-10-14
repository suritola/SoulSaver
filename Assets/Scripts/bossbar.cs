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
                SetBar(barRect, nameText, liveSlimes[0]);
                ShowExtraBars(liveSlimes.Count - 1);
                for (int i = 1; i < liveSlimes.Count; i++) SetBar(extraFills[i - 1], extraNames[i - 1], liveSlimes[i]);
                wasMulti = true;
                return;
            }
        }
        // 무한 모드 후반 여러 보스가 함께 있을 때 (1.0.2): 보스마다 한 줄씩
        if (bosss.LiveCount >= 2)
        {
            liveBosses.Clear();
            liveBosses.AddRange(bosss.Live);
            SetBar(barRect, nameText, liveBosses[0]);
            ShowExtraBars(liveBosses.Count - 1);
            for (int i = 1; i < liveBosses.Count; i++) SetBar(extraFills[i - 1], extraNames[i - 1], liveBosses[i]);
            wasMulti = true;
            return;
        }
        // 슬라임마다 그리던 것에서 돌아오면 첫 줄 이름을 다시 씀
        if (wasMulti) { wasMulti = false; shownKind = -1; }
        ShowExtraBars(0);
        WriteSingleName();
        if (MaxHealth <= 0) return;
        BarX = Mathf.Clamp01((float)NowHealth / (float)MaxHealth) * MaxBarX;
        barRect.sizeDelta = new Vector2(BarX, BarY);
    }

    // ================================================================= 분열한 킹 슬라임: 슬라임마다 보스 체력바
    const float ExtraGap = 8f;
    const float ExtraScale = 0.8f;      // 아래로 쌓는 체력바는 조금 작게 (화면을 덜 가리게)
    bool wasMulti;
    readonly List<bosss> liveSlimes = new List<bosss>();
    readonly List<bosss> liveBosses = new List<bosss>();
    readonly List<GameObject> extraBars = new List<GameObject>();
    readonly List<RectTransform> extraFills = new List<RectTransform>();
    readonly List<TMP_Text> extraNames = new List<TMP_Text>();

    void SetBar(RectTransform fill, TMP_Text label, bosss s)
    {
        float ratio = s.setEnemyHP > 0 ? Mathf.Clamp01(s.EnemyHealth / s.setEnemyHP) : 0f;
        if (fill != null) fill.sizeDelta = new Vector2(ratio * MaxBarX, BarY);
        string n = (s.bossKind == 2 && s.slimeGen >= 2 ? BossName(2) + " " + (s.barSlot + 1) : (s.developer ? BossName(s.developerMom ? DeveloperMomKind : DeveloperKind) : BossName(s.bossKind))) + "  " + Mathf.CeilToInt(Mathf.Max(0f, s.EnemyHealth)) + " / " + s.setEnemyHP;
        if (label != null && label.text != n) label.text = n;
    }

    // 위쪽 보스 체력바(배경 · 채움 · 이름)를 복제해 아래로 쌓음
    void ShowExtraBars(int count)
    {
        while (extraBars.Count < count)
        {
            GameObject clone = Instantiate(backBar, backBar.transform.parent);
            clone.name = backBar.name + "_" + (extraBars.Count + 1);
            Transform cg = clone.transform.Find("UltGauge");        // 필살기 게이지는 맨 위 줄에만
            if (cg != null) Destroy(cg.gameObject);
            RectTransform main = backBar.GetComponent<RectTransform>();
            RectTransform r = clone.GetComponent<RectTransform>();
            // 바 위의 이름 글자 · 왼쪽 해골 아이콘까지 포함한 높이로 쌓음
            // (예전엔 바 높이만큼만 내려서, 아래 체력바의 이름이 윗 체력바를 덮었음)
            Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(backBar.transform);
            float prevBottom = extraBars.Count == 0
                ? main.anchoredPosition.y + b.min.y
                : ((RectTransform)extraBars[extraBars.Count - 1].transform).anchoredPosition.y + b.min.y * ExtraScale;
            r.localScale = Vector3.one * ExtraScale;
            r.anchoredPosition = new Vector2(main.anchoredPosition.x, prevBottom - ExtraGap - b.max.y * ExtraScale);
            Transform fill = clone.transform.Find(bar.name);
            TMP_Text label = null;
            foreach (TMP_Text t in clone.GetComponentsInChildren<TMP_Text>(true))
                if (t.name == "BossName") { label = t; t.name = "BossNameExtra"; }    // 원래 이름 글자 찾기와 헷갈리지 않게
            extraBars.Add(clone);
            extraFills.Add(fill as RectTransform);
            extraNames.Add(label);
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
        float g = BossUltimate.Active ? 0f : BossUltimate.Gauge(bossKind);
        ultFill.sizeDelta = new Vector2(UltWidth * g, 10f);
        UltLabel(g);
        float pulse = g > 0.85f ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f) : 0f;
        ultFillImg.color = Color.Lerp(new Color(0.75f, 0.3f, 1f), new Color(1f, 0.35f, 0.35f), pulse);
    }

    // 1.0.4~: 이름표 없는 보라색 막대라 뜻을 몰랐음 → 게이지 오른쪽 끝에 「결계」 글자 · 처음 찰 때 도움말 한 번
    TMP_Text ultLabel;
    Loc.Lang ultLabelLang;
    bool ultHintTried;

    void UltLabel(float g)
    {
        if (ultLabel == null)
        {
            if (nameText == null || ultFill == null) return;      // 보스 이름과 같은 글꼴을 씀
            GameObject go = new GameObject("UltLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform r = go.GetComponent<RectTransform>();
            r.SetParent(ultFill.parent, false);                  // 게이지 배경 안 (아래로 쌓는 체력바 복제에는 들어가지 않음)
            r.anchorMin = r.anchorMax = new Vector2(1f, 0.5f);
            r.pivot = new Vector2(0f, 0.5f);
            r.sizeDelta = new Vector2(120f, 22f);
            r.anchoredPosition = new Vector2(6f, 0f);
            TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
            t.font = nameText.font;
            t.fontSharedMaterial = nameText.fontSharedMaterial;
            t.fontSize = 18f;
            t.color = new Color(0.82f, 0.6f, 1f);
            t.alignment = TextAlignmentOptions.MidlineLeft;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            ultLabel = t;
            ultLabelLang = (Loc.Lang)(-1);
        }
        if (ultLabelLang != Loc.Current) { ultLabelLang = Loc.Current; ultLabel.text = Loc.T("결계"); }
        bool hasUlt = bossKind != DeveloperKind;                  // 개발자는 결계 대신 필살기만
        if (ultLabel.enabled != hasUlt) ultLabel.enabled = hasUlt;
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
        }
    }

    // 보스가 한 마리일 때 이름 + 「현재 / 최대」 (1.0.4~: 예전엔 여러 마리일 때만 숫자가 보였음)
    // 숫자 · 보스 · 언어가 바뀔 때만 글자를 새로
    int shownNow = -1, shownMax = -1;
    void WriteSingleName()
    {
        if (nameText == null) return;
        int now = Mathf.Max(0, NowHealth);
        if (shownKind == bossKind && shownLang == Loc.Current && shownNow == now && shownMax == MaxHealth) return;
        shownKind = bossKind;
        shownLang = Loc.Current;
        shownNow = now;
        shownMax = MaxHealth;
        nameText.text = BossName(bossKind) + (MaxHealth > 0 ? "  " + now + " / " + MaxHealth : "");
    }
}
