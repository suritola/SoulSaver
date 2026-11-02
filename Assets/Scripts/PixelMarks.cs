using System.Collections.Generic;
using UnityEngine;

// 2.0.0~ 범위 표시 도트 장식 — 얇은 단색 선 · 단색 네모(「그림판 같은」 표시) 대신 코드로 만든 도트 그림
//   사진사 뷰파인더 · 파노라마 필름 띠 · 영사기 필름 · 장의사 부고 테 · 씨름꾼 던짐 길 · 공사 반장 굴림 길 · 인형사 안내 점선
//   그림은 HeroAimArt 와 같은 칸 크기(1칸 = 1/7 유닛) · 같은 어두운 외곽 · 점 필터 · 맨 위 그리기(PixelStrokeMin) — 한 번 만들어 계속 씀
//   그리는 쪽은 매 프레임 부르기만 함: 이번 프레임에 안 그린 조각은 이 컴포넌트의 LateUpdate(맨 뒤)가 숨김 → 숨기는 코드를 따로 안 둠
//   판정은 손대지 않음 (그리는 쪽이 판정 값으로 좌표 · 크기를 넘김) · 조각은 돌려 쓰는 풀 (매 프레임 할당 없음)
public static class PixelMarkArt
{
    const float PPU = HeroAimArt.PPU;
    static readonly Dictionary<string, Sprite> tiles = new Dictionary<string, Sprite>();

    // ---------------------------------------------------------------- 외곽을 둘러 주는 그림 (HeroAimArt.Icon · o = 흰색 · + = 밝은 회색)
    // 뷰파인더 꺾쇠 (왼쪽 위 모양 · 나머지는 뒤집어 씀): 바깥 흰 줄 + 안쪽 회색 줄 2칸 두께 · 끝은 한 칸 옅게
    static readonly string[] CornerRows =
    {
        "ooooo+",
        "o++++.",
        "o+....",
        "o+....",
        "o+....",
        "+.....",
    };
    static readonly string[] TickVRows = { "o", "+" };              // 가장자리 가운데 눈금 (위 · 아래)
    static readonly string[] TickHRows = { "o+" };                  // (왼쪽 · 오른쪽)
    static readonly string[] FocusRows = { ".o.", "o.o", ".o." };   // 가운데 초점 점
    static readonly string[] DotRows = { "oo", "oo" };              // 길 점 (큰)
    static readonly string[] DotSmallRows = { "o" };                // 길 점 (작은 · 옅은 안내)
    static readonly string[] CrossRows = { ".o.", "ooo", ".o.", ".o." };    // 장의사 모서리 십자

    public static Sprite Corner => HeroAimArt.Icon("pm_corner", CornerRows);
    public static Sprite TickV => HeroAimArt.Icon("pm_tickv", TickVRows);
    public static Sprite TickH => HeroAimArt.Icon("pm_tickh", TickHRows);
    public static Sprite Focus => HeroAimArt.Icon("pm_focus", FocusRows);
    public static Sprite Dot => HeroAimArt.Icon("pm_dot", DotRows);
    public static Sprite DotSmall => HeroAimArt.Icon("pm_dot1", DotSmallRows);
    public static Sprite Cross => HeroAimArt.Icon("pm_cross", CrossRows);
    public const float CornerInset = 2f / PPU;     // 꺾쇠 그림 가운데 ↔ 획 가운데(틀 가장자리) 거리 (외곽 1 + 획 1칸)

    // ---------------------------------------------------------------- 이어 붙이는 타일 (외곽을 따로 두르지 않음 · 글자 그대로)
    //   O = 어두운 외곽 · o = 흰색 · + = 밝은 회색 · D = 진한 필름 · d = 옅은 그늘 · w = 옅은 빛 · . = 투명
    //   가장자리 띠는 「위쪽 가장자리」 모양으로 그림 (첫 줄 = 바깥) → 옆 가장자리는 전치(transpose)해서 씀
    static readonly string[] FilmRows =         // 파노라마 필름 띠 (7칸 = 1 유닛 · 첫 줄 바깥 · 끝 줄 = 사진 쪽 경계)
    {
        "OOOOOO",
        "DDDDDD",
        "DDooDD",
        "DDooDD",
        "DDDDDD",
        "oooooo",
        "OOOOOO",
    };
    static readonly string[] MourningRows =     // 장의사 부고 테 (바깥 은빛 줄 · 그늘 띠에 구슬 · 안쪽 가는 줄)
    {
        "OOOO",
        "oooo",
        "dddd",
        "dodd",
        "dddd",
        "++++",
        "OOOO",
    };
    static readonly string[] VignetteRows =     // 뷰파인더 안쪽 가장자리 그늘 (가장자리에서 안으로 옅어지는 디더)
    {
        "dddd",
        "d.d.",
        ".d..",
    };
    static readonly string[] ThirdRows = { "o.." };                 // 삼분할 점선
    static readonly string[] SheenRows = { "w.", ".w" };            // 파노라마 띠 안 옅은 체크 빛
    static readonly string[] PixelRows = { "o" };                   // 외곽 없는 1칸 (필름 구멍)

    public static Sprite Film(bool side) => Tile(side ? "pm_film_s" : "pm_film", FilmRows, side);
    public static Sprite Mourning(bool side) => Tile(side ? "pm_mourn_s" : "pm_mourn", MourningRows, side);
    public static Sprite Vignette(bool side) => Tile(side ? "pm_vig_s" : "pm_vig", VignetteRows, side);
    public static Sprite Third(bool vertical) => Tile(vertical ? "pm_third_v" : "pm_third", ThirdRows, vertical);
    public static Sprite Sheen => Tile("pm_sheen", SheenRows, false);
    public static Sprite Pixel => Tile("pm_px", PixelRows, false);
    public const float FilmThick = 7f / PPU, MourningThick = 7f / PPU, VignetteThick = 3f / PPU;

    static Color32 Of(char ch)
    {
        switch (ch)
        {
            case 'O': return new Color32(20, 14, 30, 255);
            case 'o': return new Color32(255, 255, 255, 255);
            case '+': return new Color32(190, 190, 200, 255);
            case 'D': return new Color32(22, 18, 30, 215);
            case 'd': return new Color32(22, 18, 30, 120);
            case 'w': return new Color32(255, 255, 255, 70);
        }
        return new Color32(0, 0, 0, 0);
    }

    // transpose: 첫 줄(바깥)이 왼쪽 열이 됨 → 왼쪽 가장자리 모양 (오른쪽은 좌우 뒤집어 씀)
    static Sprite Tile(string key, string[] rows, bool transpose)
    {
        if (tiles.TryGetValue(key, out Sprite s) && s != null) return s;
        int rw = rows[0].Length, rh = rows.Length;
        int w = transpose ? rh : rw, h = transpose ? rw : rh;
        Color32[] px = new Color32[w * h];
        for (int ry = 0; ry < rh; ry++)
            for (int rx = 0; rx < rw; rx++)
            {
                Color32 c = rx < rows[ry].Length ? Of(rows[ry][rx]) : new Color32(0, 0, 0, 0);
                if (!transpose) px[(h - 1 - ry) * w + rx] = c;
                else px[(h - 1 - rx) * w + ry] = c;
            }
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
            name = "aim_" + key,
            hideFlags = HideFlags.DontSave
        };
        tex.SetPixels32(px);
        tex.Apply(false, true);
        s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect);
        s.hideFlags = HideFlags.DontSave;
        tiles[key] = s;
        return s;
    }
}

[DefaultExecutionOrder(10001)]
public class PixelMarks : MonoBehaviour
{
    Object owner;
    readonly List<SpriteRenderer> pool = new List<SpriteRenderer>(48);
    int used, frame = -1, shown;
    float z;

    // 그리는 쪽이 필드에 들고 있음 (주인이 사라지면 스스로 지움)
    public static PixelMarks Ensure(ref PixelMarks m, Object owner)
    {
        if (m != null) return m;
        GameObject go = new GameObject("PixelMarks");
        m = go.AddComponent<PixelMarks>();
        m.owner = owner;
        return m;
    }

    static float Snap(float v) => Mathf.Round(v * HeroAimArt.PPU) / HeroAimArt.PPU;
    static bool Still => GameSettings.ReduceMotion;
    public static float Flow(float speed) => Still ? 0f : Time.time * speed;

    SpriteRenderer Next(int offset)
    {
        if (frame != Time.frameCount) { frame = Time.frameCount; used = 0; z = PixelStrokeMin.TopZ(0f) - 0.02f; }
        SpriteRenderer sr;
        if (used < pool.Count && pool[used] != null) sr = pool[used];
        else
        {
            GameObject go = new GameObject("Mark");
            go.transform.SetParent(transform, false);
            sr = go.AddComponent<SpriteRenderer>();
            sr.sharedMaterial = PixelStrokeMin.TopSpriteMaterial;
            if (used < pool.Count) pool[used] = sr; else pool.Add(sr);
        }
        used++;
        PixelStrokeMin.PutOnTop(sr, offset);
        if (!sr.enabled) sr.enabled = true;
        return sr;
    }

    // 그림 한 장 (돌리지 않음 · 칸에 맞춤)
    public void Put(Sprite s, Vector2 p, Color c, bool flipX = false, bool flipY = false, int offset = 1)
    {
        if (s == null || c.a <= 0.01f) return;
        SpriteRenderer sr = Next(offset);
        if (sr.drawMode != SpriteDrawMode.Simple) sr.drawMode = SpriteDrawMode.Simple;
        if (sr.sprite != s) sr.sprite = s;
        sr.color = c;
        sr.flipX = flipX;
        sr.flipY = flipY;
        sr.transform.position = new Vector3(Snap(p.x), Snap(p.y), z);
    }

    // 이어 붙인 타일 (가장자리 띠 · 점선 · 채움) · 크기는 유닛
    public void Tiled(Sprite s, Vector2 center, Vector2 size, Color c, bool flipX = false, bool flipY = false, int offset = 0)
    {
        if (s == null || c.a <= 0.01f || size.x < 0.05f || size.y < 0.05f) return;
        SpriteRenderer sr = Next(offset);
        if (sr.sprite != s) sr.sprite = s;
        if (sr.drawMode != SpriteDrawMode.Tiled) { sr.drawMode = SpriteDrawMode.Tiled; sr.tileMode = SpriteTileMode.Continuous; }
        sr.size = new Vector2(Snap(size.x), Snap(size.y));
        sr.color = c;
        sr.flipX = flipX;
        sr.flipY = flipY;
        sr.transform.position = new Vector3(Snap(center.x), Snap(center.y), z);
    }

    // 사각 틀 네 모서리 꺾쇠 (획 가운데 = 틀 가장자리)
    public void Corners(Vector2 c, float hw, float hh, Color col, int offset = 1)
    {
        Sprite s = PixelMarkArt.Corner;
        float k = PixelMarkArt.CornerInset;
        for (int i = 0; i < 4; i++)
        {
            float sx = (i & 1) == 0 ? -1f : 1f, sy = i < 2 ? 1f : -1f;
            Put(s, new Vector2(c.x + sx * (hw - k), c.y + sy * (hh - k)), col, sx > 0f, sy < 0f, offset);
        }
    }

    // 사각 틀 안쪽(inside) 또는 바깥쪽 네 가장자리 띠 · 가장자리 타일은 「위쪽」 모양 (첫 줄 = 바깥)
    //   sides: 옆 가장자리도 그림 (false 면 위 · 아래만 — 파노라마)
    public void EdgeBands(Rect r, Sprite top, Sprite side, float thick, Color col, bool inside, bool sides = true, int offset = 0)
    {
        float s = inside ? -1f : 1f;
        Tiled(top, new Vector2(r.center.x, r.yMax + s * thick * 0.5f), new Vector2(r.width, thick), col, false, false, offset);
        Tiled(top, new Vector2(r.center.x, r.yMin - s * thick * 0.5f), new Vector2(r.width, thick), col, false, true, offset);
        if (!sides) return;
        float h = inside ? r.height - 2f * thick : r.height;
        Tiled(side, new Vector2(r.xMin - s * thick * 0.5f, r.center.y), new Vector2(thick, h), col, false, false, offset);
        Tiled(side, new Vector2(r.xMax + s * thick * 0.5f, r.center.y), new Vector2(thick, h), col, true, false, offset);
    }

    // 점선 길: a → b · arc > 0 이면 위로 볼록한 포물선 (던짐 · 뜀) · 점이 끝 쪽으로 흐름 (움직임 줄이기 = 멈춤)
    //   arrow: 끝에 진행 방향 화살촉 · alt.a > 0 이면 점 색을 번갈아 (안전 테이프) · 점은 최대 48 개
    public void DotPath(Vector2 a, Vector2 b, float arc, Color c, float spacing, bool arrow, Color alt = default, bool small = false, float flow = 1.2f)
    {
        float len = Vector2.Distance(a, b);
        if (len < 0.05f || c.a <= 0.01f) return;
        spacing = Mathf.Max(0.2f, spacing);
        float end = arrow ? Mathf.Max(0f, len - 0.45f) : len;
        Sprite dot = small ? PixelMarkArt.DotSmall : PixelMarkArt.Dot;
        bool two = alt.a > 0.01f;
        float off = two ? 0f : Mathf.Repeat(Flow(flow), spacing);
        int n = 0;
        for (float d = off + spacing * 0.5f; d < end && n < 48; d += spacing, n++)
        {
            float u = d / len;
            Vector2 p = Vector2.Lerp(a, b, u) + Vector2.up * (arc * 4f * u * (1f - u));
            // 시작 쪽은 옅게 (내 몸 위 겹침이 덜 거슬리게)
            float fade = Mathf.Clamp01(d / 0.8f);
            Color k = two && (n & 1) == 1 ? alt : c;
            k.a *= fade;
            Put(dot, p, k);
        }
        if (arrow)
        {
            Vector2 tan = (b - a) + Vector2.up * (arc * (4f - 8f));      // u = 1 에서의 접선
            if (tan.sqrMagnitude < 0.0001f) tan = b - a;
            Vector2 dir = tan.normalized;
            Put(HeroAimArt.ArrowAt(HeroAimArt.Dir(dir)), b - dir * 0.2f, c, false, false, 2);
        }
    }

    // 필름 구멍: 선분 위에 1칸 빛 점을 일정 간격으로 (영사기 필름 줄 위)
    public void Perforate(Vector2 a, Vector2 b, Color c, float spacing = 4f / 7f)
    {
        float len = Vector2.Distance(a, b);
        if (len < 0.1f) return;
        float off = Mathf.Repeat(Flow(0.8f), spacing);
        int n = 0;
        for (float d = off; d < len && n < 24; d += spacing, n++) Put(PixelMarkArt.Pixel, Vector2.Lerp(a, b, d / len), c, false, false, 2);
    }

    void LateUpdate()
    {
        if (owner == null) { Destroy(gameObject); return; }
        int n = frame == Time.frameCount ? used : 0;
        if (n == 0 && shown == 0) return;
        for (int i = n; i < pool.Count; i++)
            if (pool[i] != null && pool[i].enabled) pool[i].enabled = false;
        shown = n;
    }
}
