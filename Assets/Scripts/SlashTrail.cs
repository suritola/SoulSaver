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

    void Start() => body = GetComponent<SpriteRenderer>();

    void LateUpdate()
    {
        if (body == null || body.sprite == null) return;
        tick += Time.deltaTime;
        if (tick < 0.025f) return;
        tick = 0f;
        GameObject g = new GameObject("SlashGhost", typeof(SpriteRenderer));
        g.transform.SetPositionAndRotation(transform.position, transform.rotation);
        g.transform.localScale = transform.localScale;
        SpriteRenderer sr = g.GetComponent<SpriteRenderer>();
        sr.sprite = body.sprite;
        Color c = body.color;
        sr.color = new Color(c.r, c.g, c.b, c.a * 0.5f);
        sr.sortingLayerID = body.sortingLayerID;
        sr.sortingOrder = body.sortingOrder - 1;
        g.AddComponent<Ghost>();
    }

    // 0.18초 동안 옅어지며 조금 작아짐
    class Ghost : MonoBehaviour
    {
        SpriteRenderer sr;
        float t, a0;
        Vector3 s0;
        void Start() { sr = GetComponent<SpriteRenderer>(); a0 = sr.color.a; s0 = transform.localScale; }
        void Update()
        {
            t += Time.deltaTime;
            float k = t / 0.18f;
            if (k >= 1f) { Destroy(gameObject); return; }
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, a0 * (1f - k));
            transform.localScale = s0 * (1f - 0.25f * k);
        }
    }
}
