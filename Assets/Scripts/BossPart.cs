using UnityEngine;

// 보스가 만들어 내는 부술 수 있는 것 (2.2.1 보스 개편: 리치 왕의 영혼 등불 · 저주 묘비 · 추적 영혼구)
// "boss" 태그라 모든 공격이 맞힘 — 총알은 Bullet, 나머지 공격은 Specials.Damage 에서 Hit 를 부름
// 닿아도 몸통 피해는 없음 (PlayerController.HandleContact), 보스 체력바 · 필살기 게이지 · 피해 기록과는 상관없음
public class BossPart : MonoBehaviour
{
    public float hp = 50f;
    public float maxHp = 50f;       // 일시정지 설명의 남은 체력 줄 (엄친아 메달)
    public bool Broken { get; private set; }

    SpriteRenderer sr;
    Color baseColor;
    float baseScale, flashUntil;

    public static BossPart Make(string name, Sprite sprite, Vector3 pos, float scale, Color color, float hp, float worldRadius)
    {
        GameObject go = SpecialAbilities.MakeSprite(name, sprite, pos, scale, color, "Effect", 12);
        go.tag = "boss";
        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = worldRadius / Mathf.Max(0.01f, Mathf.Abs(scale));
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        BossPart part = go.AddComponent<BossPart>();
        part.hp = hp;
        part.maxHp = hp;
        part.sr = go.GetComponent<SpriteRenderer>();
        part.baseColor = color;
        part.baseScale = scale;
        return part;
    }

    public void Hit(float damage)
    {
        if (Broken || damage <= 0f) return;
        hp -= damage;
        flashUntil = Time.time + 0.08f;
        DamagePopup.Show(transform, damage, sr);
        Fx.Spawn("fx_spark", transform.position, 1.2f, Color.white, 26f);
        if (hp <= 0f) Break();
    }

    void Break()
    {
        Broken = true;
        Fx.Spawn("fx_soulburst", transform.position, 3.5f, baseColor, 18f);
        Hostile.Play("shatter", 0.6f, 1.3f);
        Destroy(gameObject);
    }

    // 맞으면 하얗게 번쩍, 평소엔 천천히 숨 쉬듯 (부술 수 있는 것임을 알 수 있게)
    void Update()
    {
        if (sr != null) sr.color = Time.time < flashUntil ? Color.white : baseColor;
        transform.localScale = Vector3.one * baseScale * (1f + 0.08f * Mathf.Sin(Time.time * 6f + GetInstanceID()));
    }
}
