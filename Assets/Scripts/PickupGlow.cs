using UnityEngine;

// 1.0.2: 떨어진 코인 · 회복 물약이 화려한 스킬 이펙트에 묻히지 않게
// 밝은 윤곽선 + 아래에 두근거리는 빛 무리, 그림은 스킬 이펙트(Fx 의 +40)보다 위 순서에 그림 (판정 · 위치는 그대로)
public class PickupGlow : MonoBehaviour
{
    public const int Order = 70;       // 스킬 이펙트(12 + 40 안팎)보다 위

    public Color color = new Color(1f, 0.85f, 0.35f);
    public float size = 1.1f;          // 빛 무리 지름 (칸)

    GameObject halo;
    float phase;

    public static void Add(GameObject go, Color color, float size)
    {
        if (go == null || go.GetComponent<PickupGlow>() != null) return;
        PickupGlow g = go.AddComponent<PickupGlow>();
        g.color = color;
        g.size = size;
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
            halo = SpecialAbilities.MakeSprite("PickupHalo", SpecialAbilities.GlowSprite, transform.position, size / 8f,
                                               new Color(color.r, color.g, color.b, 0.6f), "Effect", Order);
    }

    void LateUpdate()
    {
        if (halo == null) return;
        float pulse = 1f + 0.18f * Mathf.Sin(Time.time * 6f + phase);
        halo.transform.position = transform.position;
        halo.transform.localScale = Vector3.one * (size / 8f) * pulse;
    }

    void OnDisable() { if (halo != null) halo.SetActive(false); }
    void OnEnable() { if (halo != null) halo.SetActive(true); }
    void OnDestroy() { if (halo != null) Destroy(halo); }
}
