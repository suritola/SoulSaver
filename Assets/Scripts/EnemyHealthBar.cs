using UnityEngine;

// 적 머리 위에 표시되는 작은 체력바
// EnermyController가 Start에서 Attach로 붙인다
public class EnemyHealthBar : MonoBehaviour
{
    static Sprite pixel;

    const float Width = 2.2f;
    const float Height = 0.32f;     // 0.26 → 0.32 (줌 15 에서 9.4 → 11.5px · 흰 게이지 · 흔들림이 픽셀에 묻히지 않게)
    const float Border = 0.06f;
    const float HeadMargin = 0.25f;

    static readonly Color FullColor = new Color(0.45f, 0.85f, 0.35f);
    static readonly Color HalfColor = new Color(0.98f, 0.72f, 0.22f);
    static readonly Color LowColor = new Color(0.86f, 0.17f, 0.24f);
    static readonly Color BackColor = new Color(0.05f, 0.04f, 0.06f, 0.9f);
    static readonly Color BackHit = new Color(1f, 0.96f, 0.92f);    // Big 이상: 어두운 테두리가 하얗게

    EnermyController enemy;
    Transform fill;
    SpriteRenderer fillRenderer;
    SpriteRenderer backRenderer;
    float shownRatio = -1f;

    // 타격감 (HealthBarFeel): 잃은 양 흰색 지연 게이지 · 큰 피해에 흔들림 · 두께 펀치 · 흰 테두리 · 번쩍임
    readonly HealthBarFeel feel = new HealthBarFeel(HealthBarFeel.Kind.Enemy);
    SpriteRenderer ghostRenderer;    // 처음 흰 게이지가 필요할 때 만들고(보이는 적만) 없을 땐 꺼 둠
    Transform ghost;
    Vector3 rest;
    Vector3 baseScale = Vector3.one; // 부모 스케일 보정값 (Attach) — 펀치는 여기에 곱함
    Color shownBack = BackColor;
    float shownGhost = -1f;
    Color fillColor = FullColor;
    Color shownFill = FullColor;     // 지금 칠해 둔 색 (바뀔 때만 다시 씀)
    float shownIgnite = -1f;

    public HealthBarFeel.Tier OnDamaged(float amount, float before)
    {
        // 1.2.0: 1.2.0 에서 「이야기 모드 중간 보스만 애니메이션」으로 막았던 것을 되돌림 (모든 적 · 무한 모드도 1.1.8 처럼)
        //   — 렉의 원인은 체력바 애니메이션이 아니라 PlayerHP 의 자기 복제 연쇄였음 (HealthBarFeel.MakeGhost 참고)
        bool visible = fillRenderer != null && fillRenderer.enabled && fillRenderer.isVisible;
        HealthBarFeel.Tier tier = feel.OnDamaged(amount, before, enemy != null ? enemy.setEnemyHP : 0f, visible);
        // 화면 밖: 흰 게이지를 지금 체력에 바로 붙임 (Active 가 켜지지 않게 → 예전처럼 체력이 바뀔 때만 다시 그림)
        if (!visible) feel.Reset(before - amount);
        return tier;
    }
    // 풀에서 다시 꺼냄 등: 완전 초기화 (색 · 위치 · 스케일 · 흰 게이지)
    public void ResetFeel(float hp)
    {
        feel.Reset(hp);
        shownRatio = shownGhost = shownIgnite = -1f;
        shownFill = fillColor;
        if (fillRenderer != null) fillRenderer.color = fillColor;
        shownBack = BackColor;
        if (backRenderer != null) backRenderer.color = BackColor;
        if (ghostRenderer != null) ghostRenderer.enabled = false;
        transform.localPosition = rest;
        transform.localScale = baseScale;
    }
    void OnDisable() => feel.Cancel();

    public static EnemyHealthBar Attach(EnermyController enemy, SpriteRenderer body)
    {
        GameObject root = new GameObject("HealthBar");
        root.transform.SetParent(enemy.transform, false);

        // 부모 스케일과 상관없이 같은 크기로 보이게 보정
        Vector3 s = enemy.transform.lossyScale;
        root.transform.localScale = new Vector3(1f / Mathf.Abs(s.x), 1f / Mathf.Abs(s.y), 1f);
        root.transform.localPosition = new Vector3(0f, HeadTop(body) + HeadMargin / Mathf.Abs(s.y), 0f);

        EnemyHealthBar bar = root.AddComponent<EnemyHealthBar>();
        bar.enemy = enemy;
        bar.Build();
        return bar;
    }

    // 스프라이트에서 실제로 그려지는 부분의 가장 윗점 (로컬 좌표)
    static float HeadTop(SpriteRenderer body)
    {
        if (body == null || body.sprite == null) return 1f;

        // 1.1.9: sprite.vertices 는 부를 때마다 새 배열을 만듦 (적이 스폰될 때마다) → 스프라이트마다 한 번만 계산해 둠
        Sprite sp = body.sprite;
        if (headTops.TryGetValue(sp, out float cached)) return cached;
        float top = float.MinValue;
        foreach (Vector2 v in sp.vertices) top = Mathf.Max(top, v.y);
        headTops[sp] = top;
        return top;
    }
    static readonly System.Collections.Generic.Dictionary<Sprite, float> headTops = new System.Collections.Generic.Dictionary<Sprite, float>();

    static Sprite Pixel()
    {
        if (pixel == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            // 피벗을 왼쪽 가운데에 두어 가로 스케일로 줄어들게 함
            pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);
        }
        return pixel;
    }

    SpriteRenderer MakePart(string name, Color color, int order, float x, float width, float height)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(x, 0f, 0f);
        go.transform.localScale = new Vector3(width, height, 1f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Pixel();
        sr.color = color;
        sr.sortingLayerName = "Effect";
        sr.sortingOrder = order;
        return sr;
    }

    void Build()
    {
        backRenderer = MakePart("Back", BackColor, 0,
            -Width / 2f - Border, Width + Border * 2f, Height + Border * 2f);
        fillRenderer = MakePart("Fill", FullColor, 2, -Width / 2f, Width, Height);     // Back 0 · Ghost 1 · Fill 2
        fill = fillRenderer.transform;
        rest = transform.localPosition;
        baseScale = transform.localScale;
    }

    void LateUpdate()
    {
        if (enemy == null) return;

        float hp = enemy.EnemyHealth, max = enemy.setEnemyHP;
        float ratio = max > 0 ? Mathf.Clamp01(hp / max) : 0f;
        // 체력이 그대로고 효과도 없으면 다시 그리지 않음 (수백 마리 대부분이 여기서 끝)
        if (ratio == shownRatio && !feel.Active) return;
        feel.Tick(hp, max, HealthBarFeel.Dt, out HealthBarFeel.Frame f);

        bool alive = ratio > 0f;
        if (ratio != shownRatio)
        {
            shownRatio = ratio;
            // 죽으면 숨김
            backRenderer.enabled = alive;
            fillRenderer.enabled = alive;
            if (alive)
            {
                fill.localScale = new Vector3(Width * ratio, Height, 1f);
                fillColor = ratio > 0.5f
                    ? Color.Lerp(HalfColor, FullColor, (ratio - 0.5f) * 2f)
                    : Color.Lerp(LowColor, HalfColor, ratio * 2f);
            }
        }
        if (!alive)
        {
            // 죽으면 전부 원위치 · 흰 게이지 · 효과도 끔 (다음 프레임부터 첫 줄에서 반환)
            if (ghostRenderer != null) ghostRenderer.enabled = false;
            shownGhost = shownIgnite = -1f;
            feel.Reset(0f);
            if (transform.localPosition != rest) transform.localPosition = rest;
            if (transform.localScale != baseScale) transform.localScale = baseScale;
            if (shownBack != BackColor) { shownBack = BackColor; backRenderer.color = BackColor; }
            return;
        }

        if (f.ghost != shownGhost || (ghostRenderer == null ? f.ghostShown : ghostRenderer.enabled != f.ghostShown))     // 맞는 순간엔 흰 게이지 값(맞기 전 체력)이 그대로라 켜짐 여부도 같이 봄
        {
            // 화면 밖 적은 흰 게이지 렌더러를 만들지 않음 (보일 때 다시 시도)
            if (f.ghostShown && ghostRenderer == null && fillRenderer.isVisible)
            {
                ghostRenderer = MakePart("Ghost", HealthBarFeel.GhostColor(0f), 1, -Width / 2f, Width, Height);
                ghost = ghostRenderer.transform;
            }
            if (ghostRenderer != null)
            {
                shownGhost = f.ghost;
                if (ghostRenderer.enabled != f.ghostShown) { ghostRenderer.enabled = f.ghostShown; shownIgnite = -1f; }
                if (f.ghostShown) ghost.localScale = new Vector3(Width * f.ghost, Height, 1f);
            }
        }
        // 흔들림: Big 0.144 · Deadly 0.256 (월드 단위 ≈ 5 / 9px @줌 15)
        // localPosition 은 부모(적) 스케일만큼 커지므로 바 자신의 보정 스케일(1/lossyScale)을 곱함 (중간 보스 1.8배여도 같은 폭)
        Vector3 pos = rest + new Vector3(f.shakeX * baseScale.x, f.shakeY * baseScale.y, 0f) * Height;
        if (transform.localPosition != pos) transform.localPosition = pos;
        // 두께 펀치 (세로만)
        Vector3 sc = new Vector3(baseScale.x, baseScale.y * (1f + f.punch), baseScale.z);
        if (transform.localScale != sc) transform.localScale = sc;
        // 색은 바뀔 때만 씀
        Color fc = f.flash > 0f ? Color.Lerp(fillColor, Color.white, f.flash) : fillColor;
        if (fc != shownFill) { shownFill = fc; fillRenderer.color = fc; }
        Color bc = f.backFlash > 0f ? Color.Lerp(BackColor, BackHit, f.backFlash) : BackColor;
        if (bc != shownBack) { shownBack = bc; backRenderer.color = bc; }
        if (ghostRenderer != null && ghostRenderer.enabled)
        {
            if (f.ignite != shownIgnite) { shownIgnite = f.ignite; ghostRenderer.color = HealthBarFeel.GhostColor(f.ignite); }
        }
        else shownIgnite = -1f;
    }
}
