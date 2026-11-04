using UnityEngine;

// 화면 가장자리 테마 액자 (스킨 시청각 개편 — 캐릭터 영웅 이상 필살기 C10 · 이펙트 영웅 이상 연쇄 E8)
//   오버레이 캔버스는 적 탄 위에 그려지므로 금지 → 카메라 자식 월드 그림 (Effect 38 — 적 탄 · 경고 아래, 2-2절)
//   네 변의 그라데이션 띠(안쪽이 투명) + 테마 글리프 · 나타남 0.1 · 머묾 0.35 · 사라짐 0.25 · 게임 시간 (일시정지면 멈춤)
//   판에 하나 · 다시 켜기만 · 필살기 액자가 떠 있으면 연쇄 액자는 생략 (6-1절)
public sealed class SkinEdge : MonoBehaviour
{
    const float In = 0.1f, Stay = 0.35f, Out = 0.25f, Thick = 0.06f;
    static SkinEdge instance;
    static Sprite band;

    readonly SpriteRenderer[] sides = new SpriteRenderer[4];
    Camera cam;
    float t = 99f;
    bool ult;
    Color color;

    // recipe: 「글리프 개수」 (예: "flake 14") — 변을 따라 떠 있는 테마 그림
    public static void Show(string recipe, Color c, bool fromUlt)
    {
        if (GraphicsQuality.Low || SkinFx.Off) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        if (instance == null || instance.cam != cam)
        {
            GameObject go = new GameObject("SkinEdge");
            go.transform.SetParent(cam.transform, false);
            instance = go.AddComponent<SkinEdge>();
            instance.cam = cam;
            instance.Build();
        }
        SkinEdge e = instance;
        float total = In + Stay + Out;
        if (!fromUlt && e.ult && e.t < total) return;       // 필살기 액자가 우선
        bool fresh = e.t >= total * 0.6f;
        e.t = 0f;
        e.ult = fromUlt;
        e.color = c;
        e.gameObject.SetActive(true);
        if (fresh) e.Scatter(recipe, c);
    }

    void Build()
    {
        if (band == null)
        {
            // 세로 그라데이션 띠: 아래(화면 가장자리)가 진하고 위(안쪽)로 투명
            Texture2D tex = new Texture2D(4, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Color32[] px = new Color32[4 * 32];
            for (int y = 0; y < 32; y++)
            {
                float a = Mathf.Pow(1f - y / 31f, 1.6f);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            band = Sprite.Create(tex, new Rect(0, 0, 4, 32), new Vector2(0.5f, 0f), 32f);
        }
        for (int i = 0; i < 4; i++)
        {
            GameObject g = new GameObject("Side" + i);
            g.transform.SetParent(transform, false);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = band;
            sr.sortingLayerName = "Effect";
            sr.sortingOrder = 38;
            sides[i] = sr;
        }
    }

    // 변을 따라 테마 글리프 (월드 입자 — 짧게 떠 있다 사라짐)
    void Scatter(string recipe, Color c)
    {
        if (string.IsNullOrEmpty(recipe)) return;
        string[] p = recipe.Split(' ');
        Sprite sp = SkinArt.Get(p[0]);
        int n = p.Length > 1 && int.TryParse(p[1], out int k) ? Mathf.Clamp(k, 1, 24) : 12;
        if (sp == null) return;
        float h = cam.orthographicSize, w = h * cam.aspect;
        Vector3 o = cam.transform.position;
        bool still = GameSettings.ReduceMotion;
        for (int i = 0; i < n; i++)
        {
            float u = Random.value;
            int side = i % 4;
            Vector3 at = side switch
            {
                0 => new Vector3(Mathf.Lerp(-w, w, u), h * 0.92f, 0f),
                1 => new Vector3(Mathf.Lerp(-w, w, u), -h * 0.92f, 0f),
                2 => new Vector3(-w * 0.95f, Mathf.Lerp(-h, h, u), 0f),
                _ => new Vector3(w * 0.95f, Mathf.Lerp(-h, h, u), 0f),
            };
            Color col = new Color(c.r, c.g, c.b, 0.85f * GameSettings.MyFxAlpha);
            SkinMote m = SkinMote.Make(sp, o + at, h * Random.Range(0.06f, 0.1f), col, "Effect", 38, In + Stay + Out);
            if (m != null && !still) m.Move((Vector3)(Random.insideUnitCircle * h * 0.05f)).Spin(Random.Range(-90f, 90f), Random.Range(0f, 360f));
        }
    }

    void LateUpdate()
    {
        if (cam == null) { Destroy(gameObject); return; }
        float total = In + Stay + Out;
        t += Time.deltaTime;
        if (t >= total) { gameObject.SetActive(false); return; }
        float a = t < In ? t / In : t < In + Stay ? 1f : 1f - (t - In - Stay) / Out;
        float h = cam.orthographicSize, w = h * cam.aspect;
        float thick = h * 2f * Thick;
        Color col = new Color(color.r, color.g, color.b, a * 0.85f * GameSettings.MyFxAlpha);
        // 0 위 · 1 아래 · 2 왼쪽 · 3 오른쪽 — 띠의 진한 쪽(피벗)이 화면 끝
        Place(sides[0], new Vector3(0f, h, 1f), 180f, w * 2f, thick, col);
        Place(sides[1], new Vector3(0f, -h, 1f), 0f, w * 2f, thick, col);
        Place(sides[2], new Vector3(-w, 0f, 1f), -90f, h * 2f, thick, col);
        Place(sides[3], new Vector3(w, 0f, 1f), 90f, h * 2f, thick, col);
    }

    static void Place(SpriteRenderer sr, Vector3 local, float rot, float length, float thick, Color c)
    {
        if (sr == null) return;
        Transform tr = sr.transform;
        tr.localPosition = local;
        tr.localRotation = Quaternion.Euler(0f, 0f, rot);
        Vector2 size = sr.sprite.bounds.size;
        tr.localScale = new Vector3(length / size.x, thick / size.y, 1f);
        sr.color = c;
    }
}
