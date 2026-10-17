using UnityEngine;

// 적 머리 위에 표시되는 작은 체력바
// EnermyController가 Start에서 Attach로 붙인다
public class EnemyHealthBar : MonoBehaviour
{
    static Sprite pixel;

    const float Width = 2.2f;
    const float Height = 0.26f;
    const float Border = 0.07f;
    const float HeadMargin = 0.25f;

    static readonly Color FullColor = new Color(0.45f, 0.85f, 0.35f);
    static readonly Color HalfColor = new Color(0.98f, 0.72f, 0.22f);
    static readonly Color LowColor = new Color(0.86f, 0.17f, 0.24f);

    EnermyController enemy;
    Transform fill;
    SpriteRenderer fillRenderer;
    SpriteRenderer backRenderer;
    float shownRatio = -1f;

    // 타격감 (HealthBarFeel): 잃은 양 조각 · 큰 피해에 흔들림 · 번쩍임
    readonly HealthBarFeel feel = new HealthBarFeel(HealthBarFeel.Kind.Enemy);
    SpriteRenderer ghostRenderer;    // 처음 조각이 필요할 때 만들고(보이는 적만) 조각이 없을 땐 꺼 둠
    Transform ghost;
    Vector3 rest;
    float shownGhost = -1f;
    Color fillColor = FullColor;
    Color shownFill = FullColor;     // 지금 칠해 둔 색 (바뀔 때만 다시 씀)
    float shownGhostFlash = -1f;

    public HealthBarFeel.Tier OnDamaged(float amount, float before)
    {
        // 1.2.0: 1.2.0 에서 「이야기 모드 중간 보스만 애니메이션」으로 막았던 것을 되돌림 (모든 적 · 무한 모드도 1.1.8 처럼)
        //   — 렉의 원인은 체력바 애니메이션이 아니라 PlayerHP 의 자기 복제 연쇄였음 (HealthBarFeel.MakeGhost 참고)
        bool visible = fillRenderer != null && fillRenderer.enabled && fillRenderer.isVisible;
        HealthBarFeel.Tier tier = feel.OnDamaged(amount, before, enemy != null ? enemy.setEnemyHP : 0f, visible);
        // 화면 밖: 조각을 지금 체력에 바로 붙임 (Active 가 켜지지 않게 → 예전처럼 체력이 바뀔 때만 다시 그림)
        if (!visible) feel.Reset(before - amount);
        return tier;
    }
    public void ResetFeel(float hp) { feel.Reset(hp); shownRatio = -1f; shownGhost = -1f; }
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
        backRenderer = MakePart("Back", new Color(0.05f, 0.04f, 0.06f, 0.9f), 0,
            -Width / 2f - Border, Width + Border * 2f, Height + Border * 2f);
        fillRenderer = MakePart("Fill", FullColor, 2, -Width / 2f, Width, Height);     // Back 0 · Ghost 1 · Fill 2
        fill = fillRenderer.transform;
        rest = transform.localPosition;
    }

    void LateUpdate()
    {
        if (enemy == null) return;

        float hp = enemy.EnemyHealth, max = enemy.setEnemyHP;
        float ratio = max > 0 ? Mathf.Clamp01(hp / max) : 0f;
        // 체력이 그대로고 효과도 없으면 다시 그리지 않음 (수백 마리 대부분이 여기서 끝)
        if (ratio == shownRatio && !feel.Active) return;
        feel.Tick(hp, max, Time.unscaledDeltaTime, out HealthBarFeel.Frame f);

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
            if (ghostRenderer != null) ghostRenderer.enabled = false;
            shownGhost = -1f;
            if (transform.localPosition != rest) transform.localPosition = rest;
            return;
        }

        if (f.ghost != shownGhost)
        {
            // 화면 밖 적은 조각 렌더러를 만들지 않음 (보일 때 다시 시도)
            if (f.ghostShown && ghostRenderer == null && fillRenderer.isVisible)
            {
                ghostRenderer = MakePart("Ghost", HealthBarFeel.GhostColor(0f), 1, -Width / 2f, Width, Height);
                ghost = ghostRenderer.transform;
            }
            if (ghostRenderer != null)
            {
                shownGhost = f.ghost;
                ghostRenderer.enabled = f.ghostShown;
                if (f.ghostShown) ghost.localScale = new Vector3(Width * f.ghost, Height, 1f);
            }
        }
        // 흔들림: Big 0.065 · Deadly 0.13 (월드 단위 · 머리 위에서 살짝)
        // localPosition 은 부모(적) 스케일만큼 커지므로 바 자신의 보정 스케일(1/lossyScale)을 곱함 (중간 보스 1.8배여도 같은 폭)
        Vector3 sc = transform.localScale;
        Vector3 pos = rest + new Vector3(f.shakeX * sc.x, f.shakeY * sc.y, 0f) * Height;
        if (transform.localPosition != pos) transform.localPosition = pos;
        // 색은 바뀔 때만 씀
        Color fc = f.flash > 0f ? Color.Lerp(fillColor, Color.white, f.flash) : fillColor;
        if (fc != shownFill) { shownFill = fc; fillRenderer.color = fc; }
        if (ghostRenderer != null && ghostRenderer.enabled)
        {
            if (f.flash != shownGhostFlash) { shownGhostFlash = f.flash; ghostRenderer.color = HealthBarFeel.GhostColor(f.flash); }
        }
        else shownGhostFlash = -1f;
    }
}
