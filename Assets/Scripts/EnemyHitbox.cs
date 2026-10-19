using UnityEngine;

// 적 · 보스 히트박스를 그림(불투명한 부분)의 가운데에 맞추고 넉넉하게 키움
// 그림을 바꾼 뒤에도 프리팹 콜라이더가 옛 그림 기준(아래로 치우침)으로 남아 있던 것을 코드로 바로잡음
// 맞는 판정(플레이어 공격)은 넉넉하게, 몸통 피해(닿기)는 예전보다 넓어지지 않게 — 공격은 잘 맞고, 닿지도 않았는데 맞지는 않게
//   맞는 판정: 몸 크기 × 1.2, 단 예전 반지름의 1.25 ~ 1.6배 (작은 적도 반드시 커지고, 장식이 큰 보스 그림도 지나치지 않게)
//   닿기: 몸 크기 × 0.8, 단 예전 반지름의 0.75 ~ 1배 · 밀어내기 · 벽 판정: 예전 반지름 그대로
public class EnemyHitbox : MonoBehaviour
{
    // 몸 크기(그림 가로 · 세로 평균의 절반) 에 곱하는 배율과 예전 반지름 기준 범위
    public const float HurtScale = 1.2f, HurtMin = 1.25f, HurtMax = 1.6f;
    public const float ContactScale = 0.8f, ContactMin = 0.75f, ContactMax = 1f;

    CircleCollider2D col;
    SpriteRenderer sr;
    float centerX, oldLocal = -1f, contactLocal;
    bool flipped;
    static readonly System.Collections.Generic.Dictionary<Sprite, (Vector2 min, Vector2 max)> extents = new System.Collections.Generic.Dictionary<Sprite, (Vector2 min, Vector2 max)>();

    public static EnemyHitbox Fit(GameObject go)
    {
        EnemyHitbox h = go.GetComponent<EnemyHitbox>();
        if (h == null) h = go.AddComponent<EnemyHitbox>();
        h.Refit();
        return h;
    }

    public void Refit()
    {
        col = GetComponent<CircleCollider2D>();
        sr = GetComponent<SpriteRenderer>();
        if (col == null) return;
        if (oldLocal < 0f) oldLocal = col.radius;       // 프리팹(· 심연) 이 정한 원래 반지름 (두 번 맞춰도 한 번만)
        contactLocal = oldLocal;
        if (sr == null || sr.sprite == null) { col.radius = oldLocal * HurtMin; return; }

        // Tight 메시면 꼭짓점이 불투명한 부분을 감쌈 (Full Rect 면 그림 틀 전체)
        // 그림마다 한 번만 계산해 둠 (sprite.vertices 는 부를 때마다 새 배열 — 스폰마다 할당이던 것)
        Sprite sp = sr.sprite;
        if (!extents.TryGetValue(sp, out var e))
        {
            Vector2 mn, mx;
            Vector2[] v = sp.vertices;
            if (v != null && v.Length > 0)
            {
                mn = mx = v[0];
                for (int i = 1; i < v.Length; i++) { mn = Vector2.Min(mn, v[i]); mx = Vector2.Max(mx, v[i]); }
            }
            else
            {
                Bounds b = sp.bounds;
                mn = b.min; mx = b.max;
            }
            e = (mn, mx);
            extents[sp] = e;
        }
        Vector2 min = e.min, max = e.max;

        Vector2 c = (min + max) * 0.5f;
        centerX = c.x;
        float body = 0.25f * ((max.x - min.x) + (max.y - min.y));
        flipped = sr.flipX;
        col.offset = new Vector2(flipped ? -centerX : centerX, c.y);
        col.radius = Mathf.Clamp(body * HurtScale, oldLocal * HurtMin, oldLocal * HurtMax);
        contactLocal = Mathf.Clamp(body * ContactScale, oldLocal * ContactMin, oldLocal * ContactMax);
    }

    float Scale
    {
        get
        {
            Vector3 s = transform.lossyScale;
            return Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y));
        }
    }

    // 몸 반지름 (월드) — 적끼리 밀어내기 · 벽 판정은 키운 히트박스가 아니라 예전 크기 그대로
    public float BodyWorldRadius => Mathf.Max(0f, oldLocal) * Scale;

    // 몸통 피해를 줄 만큼 실제로 닿았는지 (상대 콜라이더가 몸 안쪽 원에 들어옴)
    public bool Touching(Collider2D other)
    {
        if (col == null || other == null || contactLocal <= 0f) return true;
        Vector2 c = col.bounds.center;
        float r = contactLocal * Scale;
        return ((Vector2)other.ClosestPoint(c) - c).sqrMagnitude <= r * r;
    }

    // 1.4.4~ 원(플레이어 몸통 접촉 원 PlayerController.ContactCenter · ContactRadius)과 몸 안쪽 원이 겹치는지
    public bool Touching(Vector2 center, float radius)
    {
        if (col == null || contactLocal <= 0f) return true;
        Vector2 c = col.bounds.center;
        float r = contactLocal * Scale + radius;
        return (center - c).sqrMagnitude <= r * r;
    }

    // 좌우로 뒤집으면 (flipX 는 피벗 기준) 가운데도 따라 뒤집음
    void LateUpdate()
    {
        if (sr == null || col == null || sr.flipX == flipped) return;
        flipped = sr.flipX;
        col.offset = new Vector2(flipped ? -centerX : centerX, col.offset.y);
    }
}
