using UnityEngine;

// 1.0.2: 떨어진 코인 · 회복 물약이 화려한 스킬 이펙트에 묻히지 않게
// 밝은 윤곽선 + 아래에 두근거리는 빛 무리, 그림은 스킬 이펙트(Fx 의 +40)보다 위 순서에 그림 (판정 · 위치는 그대로)
public class PickupGlow : MonoBehaviour
{
    public const int Order = 70;       // 스킬 이펙트(12 + 40 안팎)보다 위

    public Color color = new Color(1f, 0.85f, 0.35f);
    public float size = 1.1f;          // 빛 무리 지름 (칸)

    GameObject halo;
    Transform haloTr;
    SpriteRenderer haloSr;
    float phase;
    Vector3 shownScale;

    public static void Add(GameObject go, Color color, float size)
    {
        if (go == null || go.GetComponent<PickupGlow>() != null) return;
        PickupGlow g = go.AddComponent<PickupGlow>();
        g.color = color;
        g.size = size;
    }

    // 1.0.4~: 값이 큰 코인은 빛 무리 색 · 크기를 바꿈 (CoinTag)
    public void SetLook(Color c, float s)
    {
        color = c;
        size = s;
        if (haloSr != null) haloSr.color = new Color(c.r, c.g, c.b, 0.6f);
    }

    void Start()
    {
        SpriteOutline.Player(gameObject);       // 층을 옮기기 전에 (Effect 층 그림에는 윤곽선을 안 붙임)
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.sortingLayerName = "Effect";
            sr.sortingOrder = Order + 1;
        }
        phase = Random.value * 6f;
        if (SpecialAbilities.GlowSprite != null)
        {
            halo = SpecialAbilities.MakeSprite("PickupHalo", SpecialAbilities.GlowSprite, transform.position, size / 8f,
                                               new Color(color.r, color.g, color.b, 0.6f), "Effect", Order);
            // 최적화: 빛 무리를 코인의 자식으로 — 매 프레임 위치를 따라 쓰지 않음 (그리는 층 · 순서는 그대로 지정돼 있음)
            //   크기는 부모(코인 단계별 크기) 를 나눠 월드 크기가 예전과 같게
            haloTr = halo.transform;
            haloSr = halo.GetComponent<SpriteRenderer>();
            haloTr.SetParent(transform, true);
            haloTr.localPosition = Vector3.zero;
            haloTr.localRotation = Quaternion.identity;
            shownScale = Vector3.zero;
            ApplyScale(1f);
        }
    }

    void ApplyScale(float pulse)
    {
        Vector3 ls = transform.lossyScale;
        float w = size / 8f * pulse;
        Vector3 want = new Vector3(Div(w, ls.x), Div(w, ls.y), Div(w, ls.z));
        if (want != shownScale) { shownScale = want; haloTr.localScale = want; }
    }

    static float Div(float w, float s) => Mathf.Abs(s) > 1e-5f ? w / Mathf.Abs(s) : w;

    void LateUpdate()
    {
        if (halo == null) return;
        // 그래픽 품질 낮음(L7): 맥동 없이 고정 크기 (바뀔 때만 씀)
        if (GraphicsQuality.Low) { ApplyScale(1f); return; }
        if (haloSr != null && !haloSr.isVisible) return;       // 화면 밖이면 맥동을 계산하지 않음 (보이지 않음)
        ApplyScale(1f + 0.18f * Mathf.Sin(Time.time * 6f + phase));
    }

    void OnDisable() { if (halo != null) halo.SetActive(false); }
    void OnEnable() { if (halo != null) halo.SetActive(true); }
    void OnDestroy() { if (halo != null) Destroy(halo); }
}
