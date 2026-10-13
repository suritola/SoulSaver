using UnityEngine;

// 총알을 잘 보이게: 따라다니는 빛 무리 + 밝은 심 + 색 꼬리
// 총알 프리팹이 아주 납작하게(가로 3.3, 세로 -0.02) 늘어나 있어서 자식으로 붙이지 않고 따로 따라다님
public class BulletGlow : MonoBehaviour
{
    const int Order = 60;
    const float HaloSize = 0.22f;      // 1.0.2: 0.16 → 0.22
    SpriteRenderer body;
    GameObject halo, core;
    float trail;
    Color color;

    void Start()
    {
        body = GetComponent<SpriteRenderer>();
        Sprite glow = SpecialAbilities.GlowSprite;
        // 그림을 꺼 둔 총알(화염 판정탄, 낫)은 빛을 붙이지 않음
        if (glow == null || body == null || !body.enabled) { enabled = false; return; }
        color = body.color;

        // 1.0.2: 화려해진 스킬 이펙트(Fx 의 +40)에 덮이지 않게 그 위 순서로, 빛 무리 · 심도 조금 크게
        body.sortingLayerName = "Effect";
        body.sortingOrder = Order + 1;
        halo = SpecialAbilities.MakeSprite("BulletHalo", glow, transform.position, HaloSize, new Color(color.r, color.g, color.b, 0.7f), "Effect", Order);
        core = SpecialAbilities.MakeSprite("BulletCore", glow, transform.position, 0.085f, new Color(1f, 1f, 1f, 1f), "Effect", Order + 2);
    }

    void LateUpdate()
    {
        if (halo == null) return;
        float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 30f);
        halo.transform.position = transform.position;
        halo.transform.localScale = Vector3.one * HaloSize * pulse;
        core.transform.position = transform.position;

        // 꼬리: 옅어지며 사라지는 빛 조각
        trail += Time.deltaTime;
        if (trail >= 0.025f)
        {
            trail = 0f;
            FadeSprite.Spawn("BulletTrail", SpecialAbilities.GlowSprite, transform.position, 0.09f,
                             new Color(color.r, color.g, color.b, 0.55f), "Effect", Order - 1, 0.18f);
        }
    }

    void OnDestroy()
    {
        if (halo != null) Destroy(halo);
        if (core != null) Destroy(core);
    }
}
