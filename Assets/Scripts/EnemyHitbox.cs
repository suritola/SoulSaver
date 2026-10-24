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

    // 1.6.0~ 보스 판정 고정 표 (그래픽 개편 2단계 묶음 1): 보스 그림(PPU · 캔버스 · 꼭짓점)이 바뀌어도 판정 · 약점 자리가 그대로이게
    //   fixedBox   = 판정이 쓰는 그림 로컬 상자 (예전 그림의 타이트 메시 꼭짓점 경계 · 1/32 칸 격자) → 중심 · 맞는 · 닿기를 예전과 같은 식 · 클램프로 계산
    //   fixedFrame = 렌더러 경계에 해당하는 그림 로컬 틀 (예전 그림 32×32 · 피벗 가운데 = ±0.5) → 약점 자리(WeakSpot) · 머리 위 글자 · 후광이 씀
    //   보스 종류(bossKind + developer · developerMom)로 고름 · 표에 없는 적(잡몹 · 중간 보스)은 지금 그림으로 계산 · VisualRules.Legacy 와 무관하게 늘 같음
    //   결과 (로컬 · 2026-10-25 boss_gallery 기준 촬영과 같음): 리치 중심 (0.0313, −0.1563) 맞는 0.4125 닿기 0.2697(프리팹 반지름 상한) /
    //   지옥 (0.0313, −0.1719) 0.4031 · 0.2688 / 슬라임 (0.0156, −0.125) 0.3469 · 0.2313 / 거울 (0, −0.0156) 0.48 · 0.30 (상한) / 개발자 · 엄마 (0, 0) 0.48 · 0.30 (상한)
    const float P = 1f / 32f;
    static readonly (Vector2 min, Vector2 max) OldFrame = (new Vector2(-0.5f, -0.5f), new Vector2(0.5f, 0.5f));
    static bool FixedFor(bosss b, out (Vector2 min, Vector2 max) box)
    {
        box = default;
        if (b == null) return false;
        if (b.developer) { box = OldFrame; return true; }                                                   // developer_0 (개발자 · 엄마 같은 그림)
        switch (b.bossKind)
        {
            case 0: box = (new Vector2(-10f, -16f) * P, new Vector2(12f, 6f) * P); return true;            // 리치 왕 boss_0
            case 1: box = (new Vector2(-10f, -16f) * P, new Vector2(12f, 5f) * P); return true;            // 지옥의 군주 hell_lord_0
            case 2: box = (new Vector2(-9f, -13f) * P, new Vector2(10f, 5f) * P); return true;             // 킹 슬라임 meadow_slimeking_0
            case 3: box = (new Vector2(-16f, -16f) * P, new Vector2(16f, 15f) * P); return true;           // 거울의 군주 mirrorlord_0
        }
        return false;
    }
    (Vector2 min, Vector2 max) fixedBox, fixedFrame;
    bool hasFixed;
    public bool HasFixed => hasFixed;

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
        hasFixed = FixedFor(GetComponent<bosss>(), out fixedBox);
        fixedFrame = hasFixed ? OldFrame : default;
        if (hasFixed) { Apply(fixedBox.min, fixedBox.max); return; }       // 보스: 그림 꼭짓점을 보지 않음
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
        Apply(e.min, e.max);
    }

    // 그림 로컬 상자 → 중심 · 맞는 판정 · 닿기 (고정 표 · 그림 계산 공통)
    void Apply(Vector2 min, Vector2 max)
    {
        Vector2 c = (min + max) * 0.5f;
        centerX = c.x;
        float body = 0.25f * ((max.x - min.x) + (max.y - min.y));
        flipped = sr != null && sr.flipX;
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
    // 닿기 판정 원 반지름 (로컬) — 1단계-11 발밑 그림자 폭
    public float ContactLocal => contactLocal;

    // 1.6.0~ 렌더러 경계 대신 쓰는 몸 틀 (월드): 고정 표가 있으면 예전 그림 틀(로컬 → 월드 · 좌우 뒤집기 반영) · 없으면 렌더러 경계 그대로
    //   약점 자리 · 머리 위 글자 · 발밑 후광이 그림 크기에 끌려가지 않게 (그림을 바꿔도 같은 자리)
    public Bounds Frame(SpriteRenderer body)
    {
        if (!hasFixed || body == null || body.gameObject != gameObject) return body != null ? body.bounds : new Bounds(transform.position, Vector3.zero);
        Vector2 mn = fixedFrame.min, mx = fixedFrame.max;
        if (body.flipX) { float t = mn.x; mn.x = -mx.x; mx.x = -t; }
        Transform tr = body.transform;
        Vector3 a = tr.TransformPoint(mn), b = tr.TransformPoint(new Vector3(mx.x, mn.y)), c = tr.TransformPoint(new Vector3(mn.x, mx.y)), d = tr.TransformPoint(mx);
        Vector3 lo = Vector3.Min(Vector3.Min(a, b), Vector3.Min(c, d)), hi = Vector3.Max(Vector3.Max(a, b), Vector3.Max(c, d));
        Bounds r = new Bounds();
        r.SetMinMax(new Vector3(lo.x, lo.y, tr.position.z), new Vector3(hi.x, hi.y, tr.position.z));
        return r;
    }

    // 몸 틀 (월드) — 히트박스가 없거나 표에 없는 적은 렌더러 경계
    public static Bounds FrameOf(SpriteRenderer body)
    {
        if (body == null) return default;
        return body.TryGetComponent(out EnemyHitbox h) ? h.Frame(body) : body.bounds;
    }

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
        if (sr == null || col == null || sr.flipX == flipped) return;     // 고정 상자도 같은 중심 x 를 뒤집음 (상자 자체는 로컬 · 뒤집기 전 값)
        flipped = sr != null && sr.flipX;
        col.offset = new Vector2(flipped ? -centerX : centerX, col.offset.y);
    }
}
