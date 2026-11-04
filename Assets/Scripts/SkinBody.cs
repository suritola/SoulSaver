using UnityEngine;

// 스킨 몸 덧칠 (스킨 시청각 개편 1단계): 플레이어 자식 하나 — 몸 그림을 같은 자리 · 같은 프레임으로 복제해 실루엣 안쪽만 한 색으로 칠함
//   피격 번쩍(tint) · 위기 맥동(crisis) · 굳음 — 몸이 이미 덮는 칸 밖으로 안 나감 (2-2절 Player 층 몸 + 1)
//   PlayerLook.LateUpdate(거너 맨손 그림을 바꿈) 뒤에 그림을 맞춤 · 판에 하나 · 다시 만들지 않음
[DefaultExecutionOrder(1000)]
public sealed class SkinBody : MonoBehaviour
{
    SpriteRenderer src, sr;
    float tintUntil, tintDur, tintAlpha;
    Color tintColor;
    bool crisis;
    Color crisisColor;

    public static SkinBody For(SpriteRenderer body)
    {
        if (body == null) return null;
        Transform t = body.transform.Find("SkinBody");
        if (t != null && t.TryGetComponent(out SkinBody got)) return got;
        GameObject go = new GameObject("SkinBody");
        go.transform.SetParent(body.transform, false);
        SkinBody b = go.AddComponent<SkinBody>();
        b.src = body;
        b.sr = go.AddComponent<SpriteRenderer>();
        if (SkinMote.Silhouette != null) b.sr.sharedMaterial = SkinMote.Silhouette;
        b.sr.enabled = false;
        return b;
    }

    // 잠깐 덧칠 (0 → 알파 → 0, life 초)
    public void Tint(Color c, float alpha, float life)
    {
        tintColor = c;
        tintAlpha = Mathf.Clamp01(alpha);
        tintDur = Mathf.Max(0.05f, life);
        tintUntil = Time.time + tintDur;
    }

    public void Crisis(bool on, Color c) { crisis = on; crisisColor = c; }

    // 남는 상태 없게 (릴레이 교대 · 사망)
    public void Clear() { tintUntil = 0f; crisis = false; if (sr != null) sr.enabled = false; }

    void LateUpdate()
    {
        if (src == null || sr == null) return;
        float a = 0f;
        Color c = tintColor;
        float left = tintUntil - Time.time;
        if (left > 0f)
        {
            float k = 1f - left / tintDur;
            a = tintAlpha * (k < 0.2f ? k / 0.2f : 1f - (k - 0.2f) / 0.8f);
        }
        if (crisis)
        {
            float p = 0.2f + 0.3f * (0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.PI * 2f / 0.8f));
            if (p > a) { a = p; c = crisisColor; }
        }
        a *= src.color.a * GameSettings.MyFxAlpha;
        bool on = a > 0.01f && src.enabled && src.sprite != null;
        if (sr.enabled != on) sr.enabled = on;
        if (!on) return;
        if (!ReferenceEquals(sr.sprite, src.sprite)) sr.sprite = src.sprite;
        sr.flipX = src.flipX;
        sr.flipY = src.flipY;
        sr.sortingLayerID = src.sortingLayerID;
        sr.sortingOrder = src.sortingOrder + 1;
        sr.color = new Color(c.r, c.g, c.b, a);
    }
}
