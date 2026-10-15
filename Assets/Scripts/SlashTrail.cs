using UnityEngine;

// 날아가는 검기의 잔상 (1.0.1): 총알처럼 둥근 빛 · 꼬리가 붙어 총알 같던 것을, 초승달 칼날 모양 그대로 옅어지는 잔상으로
// CharacterKit.Projectile · SignatureSkills.Shot 에서 fx_swordwave 일 때 붙임
public class SlashTrail : MonoBehaviour
{
    SpriteRenderer body;
    float tick;

    public static void Attach(Bullet b)
    {
        if (b == null) return;
        BulletGlow glow = b.GetComponent<BulletGlow>();
        if (glow != null) Destroy(glow);                 // 둥근 빛 · 점 꼬리는 빼고
        b.gameObject.AddComponent<SlashTrail>();
    }

    // 1.1.2: 평타와 같은 순서(적 · 맵 물체 위, AttackLook.Order) · 잔상은 새로 만들고 지우지 않고 FadeSprite 를 다시 씀
    void Start()
    {
        body = GetComponent<SpriteRenderer>();
        if (body == null) return;
        body.sortingLayerID = AttackLook.Layer;
        body.sortingOrder = AttackLook.Order;
    }

    void LateUpdate()
    {
        if (body == null || body.sprite == null || !body.enabled) return;
        tick += Time.deltaTime;
        if (tick < 0.03f) return;
        tick = 0f;
        Color c = body.color;
        FadeSprite f = FadeSprite.Spawn("SlashGhost", body.sprite, transform.position, 1f, new Color(c.r, c.g, c.b, c.a * 0.5f),
                                        "Effect", AttackLook.TrailOrder, 0.18f);
        Vector3 s = transform.lossyScale;
        f.Shape(transform.rotation, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), 1f));
    }
}
