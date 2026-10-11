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

    // 게이지 바로 뒤에 같은 모양의 후광 (테두리 밖으로 조금 넘침)
    void MakeGlow()
    {
        if (gaugeImage == null || gaugeImage.transform.parent == null) return;
        RectTransform src = gaugeImage.rectTransform;
        GameObject go = new GameObject("ReadyGlow", typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(src.parent, false);
        rt.SetSiblingIndex(src.GetSiblingIndex());
        rt.anchorMin = src.anchorMin; rt.anchorMax = src.anchorMax; rt.pivot = src.pivot;
        rt.anchoredPosition = src.anchoredPosition;
        rt.sizeDelta = src.sizeDelta + new Vector2(30f, 30f);
        glow = go.GetComponent<Image>();
        glow.sprite = gaugeImage.sprite;
        glow.raycastTarget = false;
        glow.color = new Color(1f, 0.85f, 0.35f, 0f);
        frameScale = src.parent.localScale;
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
        if (gaugeImage != null)
            gaugeImage.color = full ? Color.Lerp(ReadyGold, ReadyWhite, 0.5f + 0.5f * Mathf.Sin(t * 10f)) : baseColor;

        // 가득 찬 순간의 큰 번쩍임 (0.6초) + 1.2초마다 작은 번쩍임
        float since = t - fullSince;
        float burst = full ? Mathf.Clamp01(1f - since / 0.6f) : 0f;
        float tick = full ? Mathf.Clamp01(1f - Mathf.Repeat(since, 1.2f) / 0.35f) * 0.5f : 0f;
        if (glow != null)
        {
            float breathe = 0.45f + 0.3f * Mathf.Sin(t * 7f);
            float a = full ? Mathf.Max(breathe, burst, tick + 0.4f) : 0f;
            glow.color = new Color(1f, 0.85f, 0.35f, a);
            glow.transform.localScale = Vector3.one * (full ? 1f + 0.08f * Mathf.Sin(t * 7f) + 0.5f * burst + 0.15f * tick : 1f);
        }
        // 게이지 틀이 튀어 오름 (가득 찬 순간 크게, 그 뒤 작게)
        if (gaugeImage != null && gaugeImage.transform.parent != null && gaugeImage.transform.parent.GetComponent<Canvas>() == null)
            gaugeImage.transform.parent.localScale = frameScale * (1f + 0.22f * Mathf.Sin(Mathf.Clamp01(since / 0.35f) * Mathf.PI) * (full ? 1f : 0f) + 0.05f * tick);
    }

    void Update()
    {
        ReadyFeedback();
        PlayerController p = Hostile.Player;
        // 멈췄을 때나 스킬을 쓰는 중에는 차지 않음
        if (Time.timeScale == 0f || (p != null && p.IsSkillUsing) || IsFull()) return;
        // 캐릭터 스킬 게이지 %가 높을수록 게이지가 길어서 늦게 참
        AddSkillPoint(pointsPerSecond * GameMode.GaugeMul / Mathf.Max(0.1f, CharacterData.Current.gauge) * (Boosted ? killBoost : 1f) * TreeMul * Time.deltaTime);
        if (IsFull()) Hints.Show("ult", "필살기 게이지가 가득 찼습니다! 우클릭으로 필살기를 씁니다.");
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

        gaugeImage.fillAmount = value;


        // 충전 중에는 청록색, 가득 차면 금색
        if (value >= 1) gaugeImage.color = new Color(1f, 0.82f, 0.3f);
        else gaugeImage.color = Boosted ? new Color(0.6f, 1f, 0.95f) : new Color(0.3f, 0.86f, 0.9f);

    }
}