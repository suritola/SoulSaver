using UnityEngine;

// 그래픽 개편 1단계-4 (2026-10-22~): 내 스킬의 착지 · 범위 예고 원 — 적 경고(Hostile.Circle: 채움 · 줄무늬 · 닫히는 고리 · 검정 테)와 다른 문법
//   영웅(스킬)색 점선 원 · 채움 없음 · 닫히는 고리 없음 · 검정 테 없음 · 정렬 층 GroundFx (적 몸 아래)
//   예전엔 화살비 · 궁수 메아리 · 심판 · 택배 폭격이 적 경고 원을 그대로 써서 「나를 때릴 것」과 구분이 안 됐음
//   새 GameObject 하나 + 스크립트 없는 점 그림 자식만 (자기 복제 Instantiate 없음) · 시간이 끝나면 스스로 지움 · 남는 상태 없음
public class Friendly : MonoBehaviour
{
    static Sprite dot;
    float life, age;
    Color color;
    SpriteRenderer[] dots;

    // 예전 Hostile.Circle(pos, radius, duration, color) 와 같은 인자 (VisualRules.Legacy 면 예전 경고 원 그대로)
    public static void Circle(Vector3 pos, float radius, float duration, Color color)
    {
        if (VisualRules.Legacy) { Hostile.Circle(pos, radius, duration, color); return; }
        GameObject go = new GameObject("FriendlyCircle");
        go.transform.position = pos;
        go.AddComponent<Friendly>().Build(radius, duration, color);
    }

    void Build(float radius, float duration, Color c)
    {
        life = Mathf.Max(0.1f, duration);
        color = c;
        int n = Mathf.Clamp(Mathf.RoundToInt(2f * Mathf.PI * radius / 0.55f), 10, 64);     // 점 사이 약 0.55칸
        float size = Mathf.Clamp(radius * 0.06f, 0.16f, 0.3f);
        int layer = SortingLayer.NameToID(VisualRules.GroundFx);
        dots = new SpriteRenderer[n];
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            GameObject d = new GameObject("Dot");
            d.transform.SetParent(transform, false);
            d.transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            d.transform.localScale = Vector3.one * size;
            SpriteRenderer sr = d.AddComponent<SpriteRenderer>();
            sr.sprite = Dot;
            sr.sortingLayerID = layer;
            sr.sortingOrder = 20;
            dots[i] = sr;
        }
        Apply();
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= life) { Destroy(gameObject); return; }
        transform.Rotate(0f, 0f, 30f * Time.deltaTime);     // 천천히 돎 (멈춘 적 경고와 다르게)
        Apply();
    }

    void Apply()
    {
        // 0.08초에 나타나 끝 0.15초 동안 사라짐 · 최대 알파 0.85
        float t = age / life;
        float a = Mathf.Min(1f, age / 0.08f) * Mathf.Clamp01((life - age) / 0.15f) * 0.85f * Mathf.Clamp01(color.a + 0.25f);
        Color c = new Color(Mathf.Lerp(color.r, 1f, 0.15f), Mathf.Lerp(color.g, 1f, 0.15f), Mathf.Lerp(color.b, 1f, 0.15f), a);
        for (int i = 0; i < dots.Length; i++) if (dots[i] != null) dots[i].color = c;
    }

    // 1유닛 크기 흰 사각 점 (점 필터 · 한 번만 만듦)
    static Sprite Dot
    {
        get
        {
            if (dot != null) return dot;
            Texture2D tex = new Texture2D(2, 2) { filterMode = FilterMode.Point };
            tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            tex.Apply();
            dot = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
            return dot;
        }
    }
}
