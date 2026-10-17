using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SkillGauge : MonoBehaviour
{
    [Header("실제로 줄어들고 차오르는 Image")]
    public Image gaugeImage;

    [Header("스킬 포인트")]
    public float SkillPoint = 0f;
    public int MaxSkillPoint = 10;

    [Header("자동 충전")]
    // 적을 맞혀서가 아니라 시간이 지나면서 참 (스킬 난사 방지)
    public float pointsPerSecond = 1.4f;
    // 적을 처치하면 잠깐 더 빨리 참
    public float killBoost = 3f;
    public float killBoostTime = 1.5f;
    float boostUntil;

    void OnEnable() => EnermyController.Killed += OnKill;
    void OnDisable() => EnermyController.Killed -= OnKill;
    void OnKill(Vector3 pos) => boostUntil = Time.time + killBoostTime;

    public bool Boosted => Time.time < boostUntil;

    // 영혼 트리 '빠른 충전'
    static float TreeMul => SpecialAbilities.SharedInstance != null ? SpecialAbilities.SharedInstance.TreeGaugeMul : 1f;

    // 2.1.8: 가득 차는 순간 소리 + 가득 찬 동안 게이지가 금빛으로 숨 쉬듯 반짝임 (처음 한 번 뜨는 도움말 말고는 알림이 없었음)
    // 2.2.0: 훨씬 눈에 띄게 — 차임 상승 아르페지오 소리, 게이지 뒤 금빛 후광(가득 차는 순간 크게 번쩍 · 그 뒤 숨 쉬듯 + 1.2초마다 작은 번쩍),
    //        게이지 틀이 튀어 오르고, 게이지는 금색 ↔ 흰색으로 빠르게 반짝
    bool wasFull;
    Color baseColor = Color.white;
    static readonly Color ReadyGold = new Color(1f, 0.85f, 0.35f);
    static readonly Color ReadyWhite = new Color(1f, 0.98f, 0.85f);
    Image glow;
    float fullSince = -99f;
    Vector3 frameScale = Vector3.one;

    // 게이지 틀 뒤에 부드러운 후광 (테두리 밖으로 조금 넘침)
    // 1.1~: 틀(SkillFrame)의 자식이라 틀 그림 위에 금색 판으로 덮였고, 크게 번쩍일 때 왼쪽 스킬 아이콘까지 덮었음
    //   → 틀과 같은 부모에서 틀 앞 순서(뒤에 그려짐) · 부드러운 빛 그림 · 번쩍일 때는 세로로만 퍼짐
    void MakeGlow()
    {
        if (gaugeImage == null || gaugeImage.transform.parent == null) return;
        RectTransform frame = gaugeImage.transform.parent as RectTransform;
        if (frame == null) return;
        GameObject go = new GameObject("ReadyGlow", typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        bool sibling = frame.GetComponent<Canvas>() == null && frame.parent != null && frame.anchorMin == frame.anchorMax;
        if (sibling)
        {
            rt.SetParent(frame.parent, false);
            rt.SetSiblingIndex(frame.GetSiblingIndex());
            rt.anchorMin = rt.anchorMax = frame.anchorMin;
            rt.pivot = new Vector2(0.5f, 0.5f);
            // 틀 가운데 (틀의 기준점이 가운데가 아니어도)
            rt.anchoredPosition = frame.anchoredPosition + Vector2.Scale(new Vector2(0.5f, 0.5f) - frame.pivot, frame.sizeDelta);
            rt.sizeDelta = frame.sizeDelta + new Vector2(24f, 40f);
        }
        else
        {
            RectTransform src = gaugeImage.rectTransform;
            rt.SetParent(src.parent, false);
            rt.SetSiblingIndex(src.GetSiblingIndex());
            rt.anchorMin = src.anchorMin; rt.anchorMax = src.anchorMax; rt.pivot = src.pivot;
            rt.anchoredPosition = src.anchoredPosition;
            rt.sizeDelta = src.sizeDelta + new Vector2(30f, 30f);
        }
        glow = go.GetComponent<Image>();
        glow.sprite = SpecialAbilities.GlowSprite != null ? SpecialAbilities.GlowSprite : gaugeImage.sprite;
        glow.raycastTarget = false;
        glow.color = new Color(1f, 0.85f, 0.35f, 0f);
        frameScale = gaugeImage.transform.parent.localScale;     // 준비됐을 때 키웠다 줄이는 기준 크기 (Update 가 같은 부모를 키움)
    }

    IEnumerator ReadyJingle()
    {
        Hostile.Play("pulse", 0.45f, 1.2f);
        Hostile.Play("shimmer", 0.5f, 1.3f);
        float[] notes = { 1.0f, 1.26f, 1.5f, 2.0f };    // 도 · 미 · 솔 · 도
        for (int i = 0; i < notes.Length; i++)
        {
            Hostile.Play("chime", 0.45f + 0.08f * i, notes[i]);
            yield return new WaitForSecondsRealtime(0.07f);
        }
        Hostile.Play("sparkle", 0.6f, 1.2f);
        Hostile.Play("chime", 0.5f, 2.5f);
    }

    void ReadyFeedback()
    {
        bool full = IsFull();
        if (full && !wasFull && Time.timeScale > 0f && !GameInput.Auto)
        {
            fullSince = Time.unscaledTime;
            StartCoroutine(ReadyJingle());
        }
        wasFull = full;
        float t = Time.unscaledTime;
        // 1.1~: 「번쩍임」 설정을 끄면 금↔흰 깜빡임 · 큰 번쩍 · 1.2초 번쩍 · 틀 튀어오름 없이 고정 금색 후광만
        bool flashes = GameSettings.Flashes;
        if (gaugeImage != null)
            gaugeImage.color = full ? (flashes ? Color.Lerp(ReadyGold, ReadyWhite, 0.5f + 0.5f * Mathf.Sin(t * 10f)) : ReadyGold) : baseColor;

        // 가득 찬 순간의 큰 번쩍임 (0.6초) + 1.2초마다 작은 번쩍임
        float since = t - fullSince;
        float burst = full && flashes ? Mathf.Clamp01(1f - since / 0.6f) : 0f;
        float tick = full && flashes ? Mathf.Clamp01(1f - Mathf.Repeat(since, 1.2f) / 0.35f) * 0.5f : 0f;
        if (glow != null)
        {
            if (SpecialAbilities.GlowSprite != null && glow.sprite != SpecialAbilities.GlowSprite) glow.sprite = SpecialAbilities.GlowSprite;
            float breathe = flashes ? 0.45f + 0.3f * Mathf.Sin(t * 7f) : 0.6f;
            float a = full ? Mathf.Max(breathe, burst, tick + 0.4f) : 0f;
            glow.color = new Color(1f, 0.85f, 0.35f, a);
            // 가로는 거의 그대로 (왼쪽 스킬 아이콘을 덮지 않게), 세로로 퍼짐
            float wave = flashes ? 0.08f * Mathf.Sin(t * 7f) : 0f;
            glow.transform.localScale = full ? new Vector3(1f + 0.02f * burst, 1f + wave + 0.6f * burst + 0.15f * tick, 1f) : Vector3.one;
        }
        // 게이지 틀이 튀어 오름 (가득 찬 순간 크게, 그 뒤 작게)
        float pop = full && flashes ? 0.22f * Mathf.Sin(Mathf.Clamp01(since / 0.35f) * Mathf.PI) : 0f;
        if (gaugeImage != null && gaugeImage.transform.parent != null && !ParentIsCanvas(gaugeImage.transform.parent))
            gaugeImage.transform.parent.localScale = frameScale * (1f + pop + 0.05f * tick);
    }

    // 1.1.9: 매 프레임 GetComponent<Canvas>() 대신 부모가 바뀔 때만 찾음
    Transform canvasCheckParent;
    bool canvasCheckResult;
    bool ParentIsCanvas(Transform parent)
    {
        if (parent != canvasCheckParent) { canvasCheckParent = parent; canvasCheckResult = parent.GetComponent<Canvas>() != null; }
        return canvasCheckResult;
    }

    void Update()
    {
        ReadyFeedback();
        PlayerController p = Hostile.Player;
        // 멈췄을 때나 스킬을 쓰는 중에는 차지 않음
        if (Time.timeScale == 0f || (p != null && p.IsSkillUsing) || IsFull()) return;
        // 캐릭터 스킬 게이지 %가 높을수록 게이지가 길어서 늦게 참
        AddSkillPoint(pointsPerSecond * GameMode.GaugeMul / Mathf.Max(0.1f, CharacterData.Current.gauge) * (Boosted ? killBoost : 1f) * TreeMul * Time.deltaTime);
        if (IsFull()) Hints.Show("ult", "필살기 게이지가 가득 찼습니다! 필살기: [{ULT}]");      // 1.0.4~ 바꾼 필살기 키를 따름
    }

    void Start()
    {
        // Inspector에 연결 안 했으면
        // 현재 오브젝트의 Image를 자동으로 가져옴
        if (gaugeImage == null)
        {
            gaugeImage = GetComponent<Image>();
        }
        if (gaugeImage != null) baseColor = gaugeImage.color;
        MakeGlow();
        // 1.1.9: 차오르는 게이지 · 후광 색이 매 프레임 바뀌어도 HUD 전체(루트 캔버스)를 다시 묶지 않게 하위 캔버스로
        if (gaugeImage != null) UiScaler.Isolate(gaugeImage.gameObject);
        if (glow != null) UiScaler.Isolate(glow.gameObject);

        UpdateGauge();
    }

    public void AddSkillPoint(float amount)
    {
        SkillPoint += amount;

        SkillPoint = Mathf.Clamp(SkillPoint, 0, MaxSkillPoint);

        UpdateGauge();
    }

    // 최대치를 바꿀 때 (재활용 에너지): 넘친 양을 맞추고 게이지를 바로 다시 그림
    public void SetMax(int max)
    {
        MaxSkillPoint = Mathf.Max(1, max);
        SkillPoint = Mathf.Clamp(SkillPoint, 0, MaxSkillPoint);
        UpdateGauge();
    }

    public bool IsFull()
    {
        return SkillPoint >= MaxSkillPoint;
    }

    public void ResetSkillPoint()
    {
        // 영혼 트리 '잔불': 필살기를 쓴 뒤 게이지가 조금 남음
        SkillPoint = SpecialAbilities.SharedInstance != null ? MaxSkillPoint * SpecialAbilities.SharedInstance.TreeUltRefund : 0f;

        UpdateGauge();
    }
    int order;

    void UpdateGauge()
    {
        if (gaugeImage == null)
        {
            Debug.LogWarning("Gauge Image가 연결되지 않았습니다!");
            return;
        }

        float value = (float)SkillPoint / MaxSkillPoint;

        // 1.1.9: 차는 중엔 1/300 단위로만 고침 (매 프레임 fillAmount 가 바뀌면 HUD 캔버스 전체를 매 프레임 다시 빌드함 · 눈에는 같음)
        gaugeImage.fillAmount = value >= 1f ? 1f : Mathf.Floor(value * 300f) / 300f;


        // 충전 중에는 청록색, 가득 차면 금색
        if (value >= 1) gaugeImage.color = new Color(1f, 0.82f, 0.3f);
        else gaugeImage.color = Boosted ? new Color(0.6f, 1f, 0.95f) : new Color(0.3f, 0.86f, 0.9f);

    }
}