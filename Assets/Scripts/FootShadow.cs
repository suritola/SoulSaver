using UnityEngine;

// 그래픽 개편 1단계-11 (2026-10-22~): 발밑 그림자 — 적이 바닥에 서 있는 자리 · 플레이어가 맞는 자리를 보여 줌
//   적: 몸 아래 납작한 어두운 타원 (폭 = 닿기 판정 원 지름) · 플레이어: 발밑 접촉 원(PlayerController.ContactCenter · ContactR)을 그대로 그린 옅은 고리
//   스크립트 없는 자식 SpriteRenderer 하나 · 이름으로 한 번만 붙임 (적을 복제해도 이미 있으면 다시 안 붙음 · 자기 복제 Instantiate 없음)
//   Character 층 −10 (모든 적 몸 뒤) · 판정은 건드리지 않음 · VisualRules.Legacy 면 안 붙임
public static class FootShadow
{
    const string Name = "FootShadow";
    static Sprite ellipse;

    public static void Enemy(GameObject e, SpriteRenderer body, EnemyHitbox hitbox)
    {
        if (VisualRules.Legacy || e == null || body == null || body.sprite == null) return;
        Transform t = e.transform.Find(Name);
        if (t != null) { t.gameObject.SetActive(true); return; }
        float r = hitbox != null ? hitbox.ContactLocal : 0f;
        Bounds b = body.sprite.bounds;          // 로컬 (부모 크기 · 뒤집기 전)
        float w = Mathf.Max(r * 2.2f, b.size.x * 0.55f);
        GameObject go = new GameObject(Name);
        go.transform.SetParent(e.transform, false);
        go.transform.SetAsLastSibling();        // MidBossMark · WeakSpot 은 첫 SpriteRenderer 를 몸으로 잡음
        go.transform.localPosition = new Vector3(b.center.x, b.min.y + w * 0.12f, 0f);
        go.transform.localScale = new Vector3(w, w * 0.38f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Ellipse;
        sr.color = new Color(0f, 0f, 0f, 0.42f);
        sr.sortingLayerID = body.sortingLayerID;
        sr.sortingOrder = body.sortingOrder - 10;
    }

    // 죽는 연출 동안 바로 숨김 (풀에서 다시 쓰면 Enemy 가 다시 켬)
    public static void Hide(GameObject e)
    {
        if (e == null) return;
        Transform t = e.transform.Find(Name);
        if (t != null) t.gameObject.SetActive(false);
    }

    // 플레이어: 발밑 접촉 원 (어두운 원판 + 영웅색 얇은 고리) — 몸과 같은 정렬 층 맨 뒤
    public static void Player(PlayerController p, Color hero)
    {
        if (VisualRules.Legacy || p == null || p.transform.Find(Name) != null) return;
        SpriteRenderer body = p.GetComponent<SpriteRenderer>();
        Vector3 ls = p.transform.lossyScale;
        float sx = Mathf.Max(0.0001f, Mathf.Abs(ls.x)), sy = Mathf.Max(0.0001f, Mathf.Abs(ls.y));
        Vector2 c = p.ContactCenter;
        float r = p.ContactR;
        GameObject go = new GameObject(Name);
        go.transform.SetParent(p.transform, false);
        go.transform.SetAsLastSibling();
        go.transform.position = new Vector3(c.x, c.y, p.transform.position.z);
        go.transform.localScale = new Vector3(r * 2f / sx, r * 2f / sy, 1f);
        SpriteRenderer disc = go.AddComponent<SpriteRenderer>();
        disc.sprite = Ellipse;
        // 1.6.0~ 원판은 적 아래 GroundFx 층(순서 100 · 내 바닥 이펙트 12 · 코인 70 위) · 알파 0.5 — Player 층(맨 위)이면 진하게 할수록 발밑으로 들어오는 적 탄을 가림
        //   고리는 1칸 선이라 Player 층 그대로 · 알파 0.75 (VisualRules.PlayerDiscAlpha · PlayerDiscOrder · PlayerRingAlpha)
        int bodyLayer = body != null ? body.sortingLayerID : PlayerLook.TopLayer;
        int bodyOrder = body != null ? body.sortingOrder : 0;
        disc.color = new Color(0f, 0f, 0f, VisualRules.PlayerDiscAlpha);
        //   1.6.0~ Legacy 면 예전 자리 (몸 층 · 몸 순서 −20) 그대로
        if (VisualRules.Legacy) { disc.sortingLayerID = bodyLayer; disc.sortingOrder = bodyOrder - 20; }
        else { disc.sortingLayerName = VisualRules.GroundFx; disc.sortingOrder = VisualRules.PlayerDiscOrder; }
        Sprite ring = PixelRingArt.Get(r, -1f, true, 0, true);      // 1.5.9~ 고정 (판 시작 때 한 번만 받아 끝까지 씀 — 캐시가 차면 먼저 지워져 고리가 사라졌음)
        if (ring == null) return;
        GameObject rg = new GameObject("ContactRing");
        rg.transform.SetParent(p.transform, false);
        rg.transform.position = go.transform.position;
        rg.transform.localScale = new Vector3(1f / sx, 1f / sy, 1f);
        SpriteRenderer rs = rg.AddComponent<SpriteRenderer>();
        rs.sprite = ring;
        rs.color = new Color(hero.r, hero.g, hero.b, VisualRules.PlayerRingAlpha);
        rs.sortingLayerID = bodyLayer;
        rs.sortingOrder = bodyOrder - 19;       // 예전 원판(−20) 바로 위 자리 그대로 · 몸 뒤
    }

    // 1유닛 지름 꽉 찬 원 (32칸 · 점 필터 · 가장자리 2칸만 옅게)
    static Sprite Ellipse
    {
        get
        {
            if (ellipse != null) return ellipse;
            const int n = 32;
            Texture2D tex = new Texture2D(n, n) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            Color[] px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                    px[y * n + x] = new Color(1f, 1f, 1f, d > 1f ? 0f : d > 0.88f ? 0.5f : 1f);
                }
            tex.SetPixels(px);
            tex.Apply();
            ellipse = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return ellipse;
        }
    }
}
