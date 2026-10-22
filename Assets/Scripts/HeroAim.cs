using System.Collections.Generic;
using UnityEngine;

// 1.5.0~ 영웅별 범위표시 (docs/review/2026-10-22-범위표시-영웅별-기획.md)
//   우클릭 필살기 범위표시를 그리는 코드가 매 프레임 HeroAim.Mark 로 「어느 영웅 것인지」를 직접 알려 줌 (이름 추측 없음)
//   지정된 선만: 맨 위 그리기 · 영웅 문양 고리(PixelRingArt style) · 움직이는 장식 · 가운데 큰 상징 (PixelStrokeMin 이 그림)
//   지정이 끊기거나 선이 꺼지면 같은 프레임(늦어도 다음 프레임)에 장식이 숨고 맨 위 그리기도 원래대로
//   판정은 손대지 않음 — 그림은 선이 가진 좌표(판정 값으로 그린 것)를 읽어 그리기만
public enum HeroAimKind { None = 0, Gunner, Swordsman, Rogue, Archer, Alchemist, Cartographer, Clockmaker, Postman, Ghost }

// 장식: None = 문양 고리 · 맨 위만 (덧붙는 궁극기 · 보조 선), Full = 움직이는 장식 + 가운데 상징, Skull = 머리 위 해골만 (유령 숙주 고리)
public enum HeroAimFx { None, Full, Skull }

public static class HeroAim
{
    public static HeroAimKind Of(CharacterId id)
    {
        switch (id)
        {
            case CharacterId.Gunner: return HeroAimKind.Gunner;
            case CharacterId.Swordsman: return HeroAimKind.Swordsman;
            case CharacterId.Rogue: return HeroAimKind.Rogue;
            case CharacterId.Archer: return HeroAimKind.Archer;
            case CharacterId.Alchemist: return HeroAimKind.Alchemist;
            case CharacterId.Cartographer: return HeroAimKind.Cartographer;
            case CharacterId.Clockmaker: return HeroAimKind.Clockmaker;
            case CharacterId.Postman: return HeroAimKind.Postman;
            case CharacterId.Ghost: return HeroAimKind.Ghost;
        }
        return HeroAimKind.None;
    }

    // 영웅 범위표시 대표색 (예전엔 여러 영웅이 하늘 · 초록 계열로 겹쳐 비슷해 보였음) · 판정 · 효과 색은 그대로
    public static Color ColorOf(HeroAimKind k)
    {
        switch (k)
        {
            case HeroAimKind.Gunner: return new Color(1f, 0.62f, 0.25f);
            case HeroAimKind.Swordsman: return new Color(0.55f, 0.75f, 1f);
            case HeroAimKind.Rogue: return new Color(0.75f, 0.45f, 1f);
            case HeroAimKind.Archer: return new Color(0.5f, 0.95f, 0.35f);
            case HeroAimKind.Alchemist: return new Color(0.95f, 0.4f, 0.85f);
            case HeroAimKind.Cartographer: return new Color(0.9f, 0.7f, 0.42f);
            case HeroAimKind.Clockmaker: return new Color(1f, 0.8f, 0.35f);
            case HeroAimKind.Postman: return new Color(0.98f, 0.38f, 0.32f);
            case HeroAimKind.Ghost: return new Color(0.6f, 0.92f, 1f);
        }
        return Color.white;
    }

    public static Color ColorOf(HeroAimKind k, float alpha)
    {
        Color c = ColorOf(k);
        c.a = alpha;
        return c;
    }

    static PixelStrokeMin Stroke(LineRenderer lr)
    {
        if (lr == null) return null;
        PixelStrokeMin m = lr.GetComponent<PixelStrokeMin>();
        if (m == null) m = lr.gameObject.AddComponent<PixelStrokeMin>();
        return m;
    }

    // 이번 프레임 이 선은 kind 영웅의 범위표시 (매 프레임 불러야 유지 · 안 부르면 다음 프레임에 풀림)
    public static void Mark(LineRenderer lr, HeroAimKind kind, HeroAimFx fx = HeroAimFx.Full)
    {
        PixelStrokeMin m = Stroke(lr);
        if (m == null || kind == HeroAimKind.None) return;
        m.markKind = kind;
        m.markFx = fx;
        m.markFrame = Time.frameCount;
    }

    // 우체부: 표식이 붙은 적 (편지가 그 적에게서 날아옴) · 목록은 참조만 (매 프레임 복사하지 않음)
    public static void SetTargets(LineRenderer lr, List<Transform> targets)
    {
        PixelStrokeMin m = Stroke(lr);
        if (m != null) m.markTargets = targets;
    }

    // 거너: 지정한 표적 수 / 최대 수 (고리 바깥 탄환 점)
    public static void SetCount(LineRenderer lr, int count, int max)
    {
        PixelStrokeMin m = Stroke(lr);
        if (m == null) return;
        m.markCount = count;
        m.markMax = max;
    }

    // 지도 제작자: 접힐 영역 반폭(접힘선에서 바깥 가장자리까지) · 다 찼을 때 금빛(0~1)
    public static void SetArea(LineRenderer lr, float half, float gold)
    {
        PixelStrokeMin m = Stroke(lr);
        if (m == null) return;
        m.markHalf = half;
        m.markGold = gold;
    }
}

// ================================================================= 장식 그림 (코드로 만든 도트 · 무기 그림을 방향별로 미리 돌려 구움)
//   모든 그림은 1칸 = 1/7 유닛 (캐릭터 · 고리와 같은 칸 크기) · 점 필터 · 한 번 만들어 계속 씀
public static class HeroAimArt
{
    public const float PPU = PixelRingArt.ArtPPU;
    public const int Dirs = 16;

    static readonly Color32 OutlineC = new Color32(20, 14, 30, 255);
    static readonly Dictionary<string, Sprite[]> rotated = new Dictionary<string, Sprite[]>();
    static readonly Dictionary<string, Sprite> flat = new Dictionary<string, Sprite>();

    // ---------------------------------------------------------------- 코드 도트 (o = 흰색 · + = 밝은 회색 · 나머지 투명, 1칸 어두운 외곽을 둘러 줌)
    public static readonly string[] IconSwords =
    {
        "oo.......oo",
        "ooo.....ooo",
        ".ooo...ooo.",
        "..ooo.ooo..",
        "...ooooo...",
        "....ooo....",
        "...ooooo...",
        ".+.oo.oo.+.",
        "..+o...o+..",
        ".oo+...+oo.",
        "oo.......oo",
    };
    public static readonly string[] IconTarget =
    {
        "...ooooo...",
        "..o.....o..",
        ".o..ooo..o.",
        "o..o...o..o",
        "o.o..o..o.o",
        "o.o.ooo.o.o",
        "o.o..o..o.o",
        "o..o...o..o",
        ".o..ooo..o.",
        "..o.....o..",
        "...ooooo...",
    };
    public static readonly string[] IconScope =
    {
        "...ooooo...",
        "..o..o..o..",
        ".o...o...o.",
        "o....o....o",
        "o.........o",
        "oooo.+.oooo",
        "o.........o",
        "o....o....o",
        ".o...o...o.",
        "..o..o..o..",
        "...ooooo...",
    };
    public static readonly string[] IconFlask =
    {
        "...ooooo...",
        "....o.o....",
        "....o.o....",
        "...o...o...",
        "..o.....o..",
        ".o.......o.",
        "o.+++++++.o",
        "o.ooooooo.o",
        "o.ooooooo.o",
        ".o.ooooo.o.",
        "..ooooooo..",
    };
    public static readonly string[] IconCompass =
    {
        ".....o.....",
        ".....o.....",
        "....ooo....",
        "..+.ooo.+..",
        "...+ooo+...",
        "ooooo.ooooo",
        "...+ooo+...",
        "..+.+++.+..",
        "....+++....",
        ".....+.....",
        ".....+.....",
    };
    public static readonly string[] IconClock =
    {
        "...ooooo...",
        "..o..o..o..",
        ".o.......o.",
        "o.........o",
        "o.........o",
        "oo.......oo",
        "o.........o",
        "o.........o",
        ".o.......o.",
        "..o..o..o..",
        "...ooooo...",
    };
    public static readonly string[] IconLetter =
    {
        "ooooooooooo",
        "oo.......oo",
        "o.o.....o.o",
        "o..o...o..o",
        "o...o.o...o",
        "o....o....o",
        "o.........o",
        "o.........o",
        "ooooooooooo",
    };
    public static readonly string[] IconSkull =
    {
        "..ooooooo..",
        ".ooooooooo.",
        "ooooooooooo",
        "oo...o...oo",
        "oo...o...oo",
        "ooooo.ooooo",
        ".ooooooooo.",
        "..o.o.o.o..",
        "..ooooooo..",
    };
    public static readonly string[] IconShuriken =
    {
        "....o......",
        "....oo.....",
        "....ooo....",
        ".....ooo.oo",
        "..oo.+.ooo.",
        "oooo+++oooo",
        ".ooo.+.oo..",
        "oo.ooo.....",
        "....ooo....",
        ".....oo....",
        "......o....",
    };
    static readonly string[] Casing = { "oo", "oo", "++" };
    static readonly string[] Bullet = { "o", "o" };
    static readonly string[] Bubble = { ".o.", "o.o", ".o." };
    static readonly string[] Wisp = { "..o..", "..o..", ".ooo.", "ooooo", "o+++o", ".ooo." };
    static readonly string[] Arrow = { "..o..", "...o.", "ooooo", "...o.", "..o.." };
    // 시곗바늘: 가운데(그림 중심)에서 오른쪽으로 뻗음 → 돌려도 축이 가운데
    static readonly string[] HandLong = { ".....oooooo" };
    static readonly string[] HandShort = { "...oooo" };

    public static Sprite Icon(string key, string[] rows) => FlatOf(key, rows);
    public static Sprite CasingAt(int dir) => RotOf("casing", Casing, dir);
    public static Sprite BulletDot => FlatOf("bullet", Bullet);
    public static Sprite BubbleDot => FlatOf("bubble", Bubble);
    public static Sprite WispDot => FlatOf("wisp", Wisp);
    public static Sprite ArrowAt(int dir) => RotOf("arrow", Arrow, dir);
    public static Sprite HandLongAt(int dir) => RotOf("handL", HandLong, dir);
    public static Sprite HandShortAt(int dir) => RotOf("handS", HandShort, dir);

    // 무기 그림(오른쪽을 향한 그림)을 dir 방향으로 (0 = 오른쪽, 반시계 22.5° 씩)
    public static Sprite WeaponAt(string weapon, int dir) => SpriteAt("Weapons/" + weapon, null, 0, dir);
    public static Sprite FxAt(string fx, int frame, int dir) => SpriteAt(null, fx, frame, dir);

    public static int Dir(Vector2 d) => Mathf.RoundToInt(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg / (360f / Dirs)) & (Dirs - 1);
    public static int DirDeg(float deg) => Mathf.RoundToInt(deg / (360f / Dirs)) & (Dirs - 1);

    static Sprite FlatOf(string key, string[] rows)
    {
        if (flat.TryGetValue(key, out Sprite s) && s != null) return s;
        Color32[] px = FromRows(rows, out int w, out int h);
        s = Bake(px, w, h, 0f, key);
        flat[key] = s;
        return s;
    }

    static Sprite RotOf(string key, string[] rows, int dir)
    {
        if (!rotated.TryGetValue(key, out Sprite[] arr)) rotated[key] = arr = new Sprite[Dirs];
        dir &= Dirs - 1;
        if (arr[dir] != null) return arr[dir];
        Color32[] px = FromRows(rows, out int w, out int h);
        arr[dir] = Bake(px, w, h, dir * 360f / Dirs, key + "_" + dir);
        return arr[dir];
    }

    // 원본 그림 화소 (읽기 불가 텍스처도 렌더 텍스처로 한 번 복사해 읽음) · 실패하면 null → 장식을 건너뜀
    static readonly Dictionary<string, (Color32[] px, int w, int h)> sources = new Dictionary<string, (Color32[], int, int)>();

    static Sprite SpriteAt(string path, string fx, int frame, int dir)
    {
        string key = path ?? (fx + "#" + frame);
        if (!rotated.TryGetValue(key, out Sprite[] arr)) rotated[key] = arr = new Sprite[Dirs];
        dir &= Dirs - 1;
        if (arr[dir] != null) return arr[dir];
        if (!sources.TryGetValue(key, out var src))
        {
            Sprite s = null;
            if (path != null) s = Resources.Load<Sprite>(path);
            else
            {
                Sprite[] f = Fx.Frames(fx);
                if (f != null && f.Length > 0) s = f[Mathf.Clamp(frame, 0, f.Length - 1)];
            }
            src = (Read(s, out int sw, out int sh), sw, sh);
            sources[key] = src;
        }
        if (src.px == null) return null;
        arr[dir] = Bake(src.px, src.w, src.h, dir * 360f / Dirs, key + "_" + dir);
        return arr[dir];
    }

    public static int FxFrameCount(string fx)
    {
        Sprite[] f = Fx.Frames(fx);
        return f != null ? f.Length : 0;
    }

    static Color32[] Read(Sprite s, out int w, out int h)
    {
        w = h = 0;
        if (s == null) return null;
        RenderTexture prev = RenderTexture.active;
        RenderTexture rt = null;
        Texture2D tmp = null;
        try
        {
            Rect r = s.rect;
            w = Mathf.RoundToInt(r.width);
            h = Mathf.RoundToInt(r.height);
            Texture2D src = s.texture;
            rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Point;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            tmp = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tmp.ReadPixels(new Rect(r.x, r.y, w, h), 0, 0, false);
            tmp.Apply(false);
            return tmp.GetPixels32();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
            return null;
        }
        finally
        {
            RenderTexture.active = prev;
            if (rt != null) RenderTexture.ReleaseTemporary(rt);
            if (tmp != null) Object.Destroy(tmp);
        }
    }

    static Color32[] FromRows(string[] rows, out int w, out int h)
    {
        int iw = 0;
        foreach (string row in rows) iw = Mathf.Max(iw, row.Length);
        w = iw + 2; h = rows.Length + 2;
        Color32[] px = new Color32[w * h];
        for (int ry = 0; ry < rows.Length; ry++)
            for (int rx = 0; rx < rows[ry].Length; rx++)
            {
                char ch = rows[ry][rx];
                if (ch != 'o' && ch != '+') continue;
                int x = rx + 1, y = h - 2 - ry;         // 글자 첫 줄 = 그림 위
                for (int oy = -1; oy <= 1; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int i = (y + oy) * w + x + ox;
                        if (px[i].a == 0) px[i] = OutlineC;
                    }
            }
        for (int ry = 0; ry < rows.Length; ry++)
            for (int rx = 0; rx < rows[ry].Length; rx++)
            {
                char ch = rows[ry][rx];
                if (ch == 'o') px[(h - 2 - ry) * w + rx + 1] = new Color32(255, 255, 255, 255);
                else if (ch == '+') px[(h - 2 - ry) * w + rx + 1] = new Color32(190, 190, 200, 255);
            }
        return px;
    }

    // 최근접 샘플링으로 한 번 돌려 구움 (화면에서 돌리지 않음 → 도트 격자가 비틀리지 않음)
    static Sprite Bake(Color32[] src, int w, int h, float deg, string name)
    {
        int n = deg == 0f ? 0 : Mathf.CeilToInt(Mathf.Sqrt(w * w + h * h)) + 1;
        int ow = deg == 0f ? w : n, oh = deg == 0f ? h : n;
        Color32[] px = deg == 0f ? src : new Color32[ow * oh];
        if (deg != 0f)
        {
            float a = -deg * Mathf.Deg2Rad, cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            for (int y = 0; y < oh; y++)
                for (int x = 0; x < ow; x++)
                {
                    float dx = x + 0.5f - ow * 0.5f, dy = y + 0.5f - oh * 0.5f;
                    int u = Mathf.FloorToInt(dx * cs - dy * sn + w * 0.5f);
                    int v = Mathf.FloorToInt(dx * sn + dy * cs + h * 0.5f);
                    if (u >= 0 && v >= 0 && u < w && v < h) px[y * ow + x] = src[v * w + u];
                }
        }
        Texture2D tex = new Texture2D(ow, oh, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "aim_" + name,
            hideFlags = HideFlags.DontSave
        };
        tex.SetPixels32(px);
        tex.Apply(false, true);
        Sprite s = Sprite.Create(tex, new Rect(0, 0, ow, oh), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect);
        s.hideFlags = HideFlags.DontSave;
        return s;
    }

    // ---------------------------------------------------------------- 지도 제작자 양피지 (회전한 직사각형을 화면 격자 그대로 찍음)
    //   len · width 는 유닛, 도트 2칸 단위 · 각도 5° 단위로 캐시 (모으는 동안 크기가 자라도 새 그림이 적게)
    class PEntry { public Sprite s; public int used; }
    static readonly Dictionary<long, PEntry> parchments = new Dictionary<long, PEntry>();
    static int ptick;
    const int ParchMax = 48;

    static readonly Color32 PInk = new Color32(70, 42, 20, 235);
    static readonly Color32 PBorder = new Color32(238, 214, 160, 235);
    static readonly Color32 PFill = new Color32(236, 205, 140, 58);         // 투명도 약 0.23 (적 탄 · 보스 예고가 비쳐 보이게)
    static readonly Color32 PGrid = new Color32(90, 60, 30, 110);

    public static Sprite Parchment(float len, float width, float deg)
    {
        int L = Mathf.Clamp(Mathf.RoundToInt(len * PPU * 0.5f) * 2, 8, 400);       // 상한: 거대한 값이 와도 그림이 터지지 않게 (1.5.0 거너 사고 이후)
        int W = Mathf.Clamp(Mathf.RoundToInt(width * PPU * 0.5f) * 2, 8, 400);
        int step = Mathf.RoundToInt(Mathf.Repeat(deg, 180f) / 5f) % 36;       // 직사각형은 180° 돌려도 같음
        long key = ((long)L << 24) | ((long)W << 8) | (long)step;
        ptick++;
        if (parchments.TryGetValue(key, out PEntry e) && e.s != null) { e.used = ptick; return e.s; }
        if (parchments.Count >= ParchMax)
        {
            long old = 0; int min = int.MaxValue;
            foreach (KeyValuePair<long, PEntry> kv in parchments) if (kv.Value.used < min) { min = kv.Value.used; old = kv.Key; }
            if (parchments.TryGetValue(old, out PEntry o) && o.s != null) { Object.Destroy(o.s.texture); Object.Destroy(o.s); }
            parchments.Remove(old);
        }
        float a = step * 5f * Mathf.Deg2Rad, cs = Mathf.Cos(a), sn = Mathf.Sin(a);
        int bw = Mathf.CeilToInt(Mathf.Abs(L * cs) + Mathf.Abs(W * sn)) + 2;
        int bh = Mathf.CeilToInt(Mathf.Abs(L * sn) + Mathf.Abs(W * cs)) + 2;
        Color32[] px = new Color32[bw * bh];
        float hl = L * 0.5f, hw = W * 0.5f;
        for (int y = 0; y < bh; y++)
            for (int x = 0; x < bw; x++)
            {
                float dx = x + 0.5f - bw * 0.5f, dy = y + 0.5f - bh * 0.5f;
                float u = dx * cs + dy * sn, v = -dx * sn + dy * cs;
                float eu = hl - Mathf.Abs(u), ev = hw - Mathf.Abs(v);
                if (eu < 0f || ev < 0f) continue;
                float edge = Mathf.Min(eu, ev);
                Color32 c;
                if (edge < 1f) c = PInk;                                        // 바깥 잉크 외곽
                else if (edge < 3f) c = PBorder;                                // 양피지 테두리 2칸
                else if (eu < 9f && ev < 9f && (Mathf.Abs(eu - ev) < 0.8f || (edge < 4f && Mathf.Max(eu, ev) < 7f))) c = PInk;     // 모서리 장식 (사선 · 꺾쇠)
                else if (Mathf.Abs(Mathf.Repeat(u + 3.5f, 7f) - 3.5f) < 0.5f && Mathf.Abs(Mathf.Repeat(v + 3.5f, 7f) - 3.5f) < 0.5f) c = PGrid;     // 안쪽 옅은 격자 점 (1 유닛마다)
                else c = PFill;
                px[y * bw + x] = c;
            }
        Texture2D tex = new Texture2D(bw, bh, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "aim_parchment",
            hideFlags = HideFlags.DontSave
        };
        tex.SetPixels32(px);
        tex.Apply(false, true);
        Sprite s = Sprite.Create(tex, new Rect(0, 0, bw, bh), new Vector2(0.5f, 0.5f), PPU, 0, SpriteMeshType.FullRect);
        s.hideFlags = HideFlags.DontSave;
        parchments[key] = new PEntry { s = s, used = ptick };
        return s;
    }
}

// ================================================================= 움직이는 장식 (PixelStrokeMin 이 매 프레임 그림 · 선 오브젝트의 자식 → 선이 파괴되면 같이)
//   new GameObject + AddComponent 로만 만듦 (복제 없음) · 영웅당 ~12 개를 돌려 씀 · 일시정지면 멈춤 (Time.deltaTime)
public class HeroAimDecor
{
    readonly Transform root;
    readonly List<SpriteRenderer> pool = new List<SpriteRenderer>(16);
    SpriteRenderer parchment;
    int used;
    float t, phase, z;
    bool any;

    public HeroAimDecor(Transform root) { this.root = root; }

    public void Begin()
    {
        used = 0;
        t += Time.deltaTime;
        z = PixelStrokeMin.TopZ(0f) - 0.02f;         // 고리보다 카메라 쪽
    }

    public void End()
    {
        for (int i = used; i < pool.Count; i++)
            if (pool[i] != null && pool[i].enabled) pool[i].enabled = false;
        any = used > 0;
    }

    public void Hide()
    {
        if (!any && (parchment == null || !parchment.enabled)) return;
        used = 0;
        End();
        HideParchment();
    }

    void HideParchment()
    {
        if (parchment != null && parchment.enabled) parchment.enabled = false;
    }

    static float Snap(float v) => Mathf.Round(v * HeroAimArt.PPU) / HeroAimArt.PPU;
    static float H(int k) { float s = Mathf.Sin(k * 12.9898f + 1.7f) * 43758.5453f; return s - Mathf.Floor(s); }
    static Vector3 Polar(Vector3 c, float deg, float r) => c + new Vector3(Mathf.Cos(deg * Mathf.Deg2Rad), Mathf.Sin(deg * Mathf.Deg2Rad)) * r;

    SpriteRenderer Make(string name, int offset)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(root, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = PixelStrokeMin.TopSpriteMaterial;
        PixelStrokeMin.PutOnTop(sr, offset);
        return sr;
    }

    void Place(SpriteRenderer sr, Vector3 p, float zz, int scale)
    {
        Transform tr = sr.transform;
        tr.position = new Vector3(Snap(p.x), Snap(p.y), zz);
        tr.rotation = Quaternion.identity;
        Vector3 ls = root.lossyScale;
        tr.localScale = new Vector3(ls.x != 0f ? scale / ls.x : scale, ls.y != 0f ? scale / ls.y : scale, 1f);
    }

    void Draw(Sprite s, Vector3 p, Color c, int scale = 1)
    {
        if (s == null || c.a <= 0.01f) return;
        SpriteRenderer sr;
        if (used < pool.Count && pool[used] != null) sr = pool[used];
        else
        {
            sr = Make("AimDecor", 1);
            if (used < pool.Count) pool[used] = sr; else pool.Add(sr);
        }
        used++;
        if (sr.sprite != s) sr.sprite = s;
        sr.color = c;
        PixelStrokeMin.PutOnTop(sr, 1);
        Place(sr, p, z, scale);
        if (!sr.enabled) sr.enabled = true;
    }

    // 가운데 큰 상징: 지름의 약 35% · 그림을 정수 배로 (칸 크기 유지) · 반투명
    void Icon(HeroAimKind k, Vector3 c, float r, Color col)
    {
        string[] rows = IconRows(k);
        if (rows == null) return;
        float want = 2f * r * (VisualRules.Legacy ? 0.35f : 0.25f) * HeroAimArt.PPU / 13f;         // 그림 13칸(외곽 포함) 기준 배율 · 1단계-12~ 지름 25%
        if (want < 0.75f) return;                                    // 너무 작은 고리는 생략 (상징이 고리를 덮음)
        int scale = Mathf.Clamp(Mathf.RoundToInt(want), 1, 3);
        Vector3 at = c;
        // 내 몸 위에 그려지는 고리(검사 · 우체부 · 거너)는 몸을 가리지 않게 고리 위쪽 안에
        if (k == HeroAimKind.Swordsman || k == HeroAimKind.Postman || k == HeroAimKind.Gunner) at = c + Vector3.up * (r * 0.55f);
        Draw(HeroAimArt.Icon(k.ToString(), rows), at, new Color(col.r, col.g, col.b, VisualRules.Legacy ? 0.42f : 0.2f), scale);     // 1단계-12~ 옅게 (고리 안 적 · 적 탄을 가렸음)
    }

    static string[] IconRows(HeroAimKind k)
    {
        switch (k)
        {
            case HeroAimKind.Gunner: return HeroAimArt.IconScope;
            case HeroAimKind.Swordsman: return HeroAimArt.IconSwords;
            case HeroAimKind.Rogue: return HeroAimArt.IconShuriken;
            case HeroAimKind.Archer: return HeroAimArt.IconTarget;
            case HeroAimKind.Alchemist: return HeroAimArt.IconFlask;
            case HeroAimKind.Cartographer: return HeroAimArt.IconCompass;
            case HeroAimKind.Clockmaker: return HeroAimArt.IconClock;
            case HeroAimKind.Postman: return HeroAimArt.IconLetter;
            case HeroAimKind.Ghost: return HeroAimArt.IconSkull;
        }
        return null;
    }

    // ---------------------------------------------------------------- 고리 장식
    public void Ring(HeroAimKind k, HeroAimFx fx, Vector3 c, float r, float charge, PixelStrokeMin src)
    {
        HideParchment();
        Color col = HeroAim.ColorOf(k, 0.95f);
        if (fx == HeroAimFx.Skull || k == HeroAimKind.Ghost)
        {
            // 유령: 머리 위 해골 표식 (숙주 고리 · 표적 고리)
            Draw(HeroAimArt.FxAt("fx_markskull", 0, 0), c + Vector3.up * (r + 0.9f + 0.12f * Mathf.Sin(t * 5f)), new Color(1f, 1f, 1f, 0.95f));
            if (fx == HeroAimFx.Skull) return;
            // 표적 고리 둘레 혼불 셋: 제자리에서 일렁이며 깜빡 (돌지 않음 · 다른 영웅과 움직임이 겹치지 않게)
            for (int i = 0; i < 3; i++)
            {
                float a = 90f + 120f * i + 10f * Mathf.Sin(t * 2f + i);
                Vector3 p = Polar(c, a, r + 0.25f * Mathf.Sin(t * 6f + i * 2f)) + Vector3.up * (0.25f * Mathf.Sin(t * 4f + i * 2f));
                Draw(HeroAimArt.WispDot, p, new Color(col.r, col.g, col.b, 0.6f + 0.35f * Mathf.Sin(t * 9f + i * 1.7f)));
            }
            return;
        }
        if (fx != HeroAimFx.Full) return;
        float full01 = charge < 0f ? 1f : charge;
        switch (k)
        {
            case HeroAimKind.Swordsman:
            {
                // 2026-10-22~ 간결하게 (사용자 「검이 빙글빙글 도는 거 짜치다」): 도는 검 여섯 자루를 없애고,
                //   다 모였을 때만 고리 네 방향에 칼끝을 바깥으로 향한 검이 멈춰 섬 (깜빡임 없음 · 모으는 동안은 고리 차오름만)
                if (full01 < 1f) break;
                for (int i = 0; i < 4; i++)
                {
                    float a = 90f * i;
                    Vector2 dir = new Vector2(Mathf.Cos(a * Mathf.Deg2Rad), Mathf.Sin(a * Mathf.Deg2Rad));
                    Draw(HeroAimArt.WeaponAt("weapon_sword", HeroAimArt.Dir(dir)), Polar(c, a, r + 0.4f), col);
                }
                break;
            }
            case HeroAimKind.Rogue:
            {
                // 돌진 끝 고리 가운데 표창이 돎
                int f = HeroAimArt.FxFrameCount("atk_shuriken");
                if (f > 0) Draw(HeroAimArt.FxAt("atk_shuriken", (int)(t * 16f) % f, 0), c, col);
                break;
            }
            case HeroAimKind.Archer:
            {
                // 화살이 위에서 떨어져 꽂힘 (넷이 차례로)
                for (int i = 0; i < 4; i++)
                {
                    float local = t * 1.1f + i * 0.25f;
                    int cyc = Mathf.FloorToInt(local);
                    float f = local - cyc;
                    float ang = H(cyc * 7 + i) * 360f, rr = Mathf.Sqrt(H(cyc * 13 + i + 3)) * r * 0.8f;
                    Vector3 hit = Polar(c, ang, rr);
                    float drop = f < 0.22f ? (1f - f / 0.22f) * 3f : 0f;
                    float alpha = f > 0.8f ? (1f - f) / 0.2f : 1f;
                    Draw(HeroAimArt.FxAt("atk_arrow", 0, 12), hit + Vector3.up * (drop + 0.9f), new Color(col.r, col.g, col.b, alpha));
                }
                break;
            }
            case HeroAimKind.Alchemist:
            {
                // 고리 안에서 거품이 위로 떠오름 + 고리 위 플라스크 셋이 제자리에서 흔들림
                for (int i = 0; i < 5; i++)
                {
                    float local = t * 0.75f + i * 0.2f;
                    int cyc = Mathf.FloorToInt(local);
                    float f = local - cyc;
                    float x = (H(cyc * 5 + i) * 2f - 1f) * r * 0.6f;
                    Vector3 p = c + new Vector3(x + 0.15f * Mathf.Sin(t * 5f + i), -r * 0.6f + f * r * 1.2f, 0f);
                    Draw(HeroAimArt.BubbleDot, p, new Color(col.r, col.g, col.b, 0.85f * Mathf.Sin(f * Mathf.PI)));
                }
                for (int i = 0; i < 3; i++)
                {
                    float a = 90f + 120f * i;
                    int tilt = Mathf.RoundToInt(Mathf.Sin(t * 4f + i * 2.1f) * 1.4f);
                    Vector3 p = Polar(c, a, r) + Vector3.up * (0.15f * Mathf.Sin(t * 3f + i));
                    Draw(HeroAimArt.WeaponAt("weapon_flask", tilt), p, col);
                }
                break;
            }
            case HeroAimKind.Clockmaker:
            {
                // 과거 자리 문자판: 가운데 시곗바늘 두 개가 거꾸로(반시계) 돎
                float want = 2f * r * 0.35f * HeroAimArt.PPU / 13f;
                int scale = Mathf.Clamp(Mathf.RoundToInt(want), 1, 3);
                Color hc = Color.Lerp(col, Color.white, 0.4f);
                Draw(HeroAimArt.HandLongAt(HeroAimArt.DirDeg(90f + t * 240f)), c, hc, scale);
                Draw(HeroAimArt.HandShortAt(HeroAimArt.DirDeg(90f + t * 20f)), c, hc, scale);
                break;
            }
            case HeroAimKind.Postman:
            {
                // 표식 붙은 적 → 내 쪽으로 편지가 날아옴 (살짝 포물선)
                List<Transform> ts = src.markTargets;
                int n = 0;
                if (ts != null)
                    for (int i = 0; i < ts.Count && n < 10; i++)
                    {
                        Transform tr = ts[i];
                        if (tr == null) continue;
                        float f = Mathf.Repeat(t * 1.3f + i * 0.37f, 1f);
                        Vector3 p = Vector3.Lerp(tr.position + Vector3.up * 1.2f, c, f) + Vector3.up * (Mathf.Sin(f * Mathf.PI) * 1.1f);
                        Draw(HeroAimArt.WeaponAt("weapon_letter", 0), p, new Color(col.r, col.g, col.b, Mathf.Min(1f, Mathf.Sin(f * Mathf.PI) * 2f)));
                        n++;
                    }
                break;
            }
            case HeroAimKind.Gunner:
            {
                // 탄피 넷이 반시계로 돌며 구름 + 지정한 표적 수 / 최대 수 = 고리 위쪽 바깥 탄환 점
                phase += Time.deltaTime * 40f;
                for (int i = 0; i < 4; i++)
                {
                    float a = phase + 90f * i;
                    Draw(HeroAimArt.CasingAt((int)(t * 10f) + i * 4), Polar(c, a, r), col);
                }
                int max = Mathf.Clamp(src.markMax, 0, 12), cnt = Mathf.Clamp(src.markCount, 0, max);
                float step = 5.5f;
                for (int i = 0; i < max; i++)
                {
                    float a = 90f + (i - (max - 1) * 0.5f) * step;
                    Color bc = i < cnt ? Color.Lerp(col, Color.white, 0.3f) : new Color(0.35f, 0.33f, 0.32f, 0.7f);
                    Draw(HeroAimArt.BulletDot, Polar(c, a, r + 1.0f), bc);
                }
                break;
            }
        }
        Icon(k, c, r, col);
    }

    // ---------------------------------------------------------------- 직선 장식
    public void Line(HeroAimKind k, HeroAimFx fx, Vector3 a, Vector3 b, PixelStrokeMin src)
    {
        if (fx != HeroAimFx.Full) { HideParchment(); return; }
        Color col = HeroAim.ColorOf(k, 0.95f);
        switch (k)
        {
            case HeroAimKind.Rogue:
            {
                // 돌진 경로 위를 표창 셋이 시작 → 끝으로 흘러감 (돌면서)
                HideParchment();
                int f = HeroAimArt.FxFrameCount("atk_shuriken");
                if (f <= 0) break;
                for (int i = 0; i < 3; i++)
                {
                    float u = Mathf.Repeat(t * 1.2f + i / 3f, 1f);
                    Draw(HeroAimArt.FxAt("atk_shuriken", ((int)(t * 16f) + i) % f, 0), Vector3.Lerp(a, b, u), new Color(col.r, col.g, col.b, Mathf.Min(1f, Mathf.Sin(u * Mathf.PI) * 2.5f)));
                }
                break;
            }
            case HeroAimKind.Clockmaker:
            {
                // 지나온 길 위를 톱니 둘이 과거 쪽(끝)으로 굴러감
                HideParchment();
                int f = HeroAimArt.FxFrameCount("atk_gear");
                if (f <= 0) break;
                for (int i = 0; i < 2; i++)
                {
                    float u = Mathf.Repeat(t * 0.8f + i * 0.5f, 1f);
                    Draw(HeroAimArt.FxAt("atk_gear", (int)(t * 10f) % f, 0), Vector3.Lerp(a, b, u), new Color(col.r, col.g, col.b, Mathf.Min(1f, Mathf.Sin(u * Mathf.PI) * 2.5f)));
                }
                break;
            }
            case HeroAimKind.Cartographer:
                Fold(a, b, src.markHalf, src.markGold, col);
                break;
            default:
                HideParchment();
                break;
        }
    }

    // 지도 제작자: 접힐 영역 양피지 (판정과 같은 len × 2·half) + 양쪽에서 접힘선으로 흘러드는 화살표 3쌍
    void Fold(Vector3 a, Vector3 b, float half, float gold, Color col)
    {
        Vector2 d = b - a;
        float len = d.magnitude;
        if (len < 0.1f || half <= 0f) { HideParchment(); return; }
        Vector2 crease = d / len;
        Vector2 n = new Vector2(-crease.y, crease.x);
        Vector3 mid = (a + b) * 0.5f;
        if (parchment == null) parchment = Make("FoldParchment", -1);
        parchment.sprite = HeroAimArt.Parchment(len, half * 2f, Mathf.Atan2(crease.y, crease.x) * Mathf.Rad2Deg);
        parchment.color = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.4f), gold);     // 다 차면 테두리가 금빛으로 깜빡 (기존 신호)
        PixelStrokeMin.PutOnTop(parchment, -1);
        Place(parchment, mid, z + 0.03f, 1);                      // 고리 · 선보다 뒤
        if (!parchment.enabled) parchment.enabled = true;

        for (int k = 0; k < 3; k++)
        {
            Vector3 along = (Vector3)(crease * (len * (k - 1) / 3f));
            for (int s = -1; s <= 1; s += 2)
            {
                float f = Mathf.Repeat(t * 1.1f + k * 0.33f + (s > 0 ? 0f : 0.5f), 1f);
                Vector3 p = mid + along + (Vector3)(n * (s * half * (0.9f - 0.8f * f)));
                Draw(HeroAimArt.ArrowAt(HeroAimArt.Dir(-n * s)), p, new Color(0.45f, 0.28f, 0.12f, Mathf.Sin(f * Mathf.PI)));
            }
        }
    }
}
