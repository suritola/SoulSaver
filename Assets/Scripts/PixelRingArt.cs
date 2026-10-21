using System.Collections.Generic;
using UnityEngine;

// 1.5.0~ 범위 · 조준 고리를 도트 문양으로 (시안 A+B: 이중 테 · 룬 점 · 네 방향 마름모 · 안쪽 디더 · 모은 만큼 차오르는 바깥 테)
//   캐릭터 도트와 같은 크기(1칸 = 1/ArtPPU 유닛, 화면 약 4화소)로 반지름마다 그림을 만들어 다시 씀
//   그림은 흰색 기반 명암이라 SpriteRenderer 색을 곱해 고리 색을 냄 (어두운 외곽은 곱해도 어두움)
//   판정 원(반지름)은 바깥 테의 가운데 → 범위 크기는 예전 선과 같음
public static class PixelRingArt
{
    public const float ArtPPU = 7f;         // 도트 1칸 = 1/7 유닛 (캐릭터 PPU 8 × 몸 배율 ≈ 화면에서 같은 칸 크기)
    public const int ChargeSteps = 24;      // 차오름 단계 (바깥 테를 24 칸으로 나눠 밝힘)
    const int Pad = 5;                      // 바깥 테 · 마름모가 그림 밖으로 안 잘리게
    const int CacheMax = 256;               // 그림 수 상한 (넘으면 가장 오래 안 쓴 것부터 버림 · 퍼지는 충격파는 반지름마다 새 그림이라 넉넉히 · 1.5.0~ 영웅 문양까지 192 → 256)
    const int StylePad = 8;                 // 1.5.0~ 영웅 문양은 바깥으로 뻗는 바늘 · 가시 · 털이 있어 여백을 더

    static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
    static readonly Color Outline = new Color(0.08f, 0.055f, 0.12f, 1f);
    static readonly Color Band = new Color(0.82f, 0.82f, 0.86f, 1f);         // 바깥 테 (색을 곱하면 고리 색)
    static readonly Color BandDim = new Color(0.32f, 0.32f, 0.36f, 0.9f);    // 아직 안 찬 바깥 테
    static readonly Color Light = Color.white;                               // 룬 점 · 마름모 심
    static readonly Color Inner = new Color(0.82f, 0.82f, 0.86f, 0.8f);      // 안쪽 가는 테
    static readonly Color Dither = new Color(1f, 1f, 1f, 0.16f);             // 안쪽 가장자리 디더

    class Entry { public Sprite sprite; public int used; }
    static readonly Dictionary<long, Entry> cache = new Dictionary<long, Entry>();
    static int tick;

    // 반지름(유닛) · 차오름(0~1, 음수 = 다 찬 모습)으로 고리 그림
    //   plain: 여러 개가 늘 떠 있거나 퍼져 나가는 고리(줍기 범위 · 충격파 등)는 외곽 + 테만 (룬 · 마름모 · 디더 없이 단정하게)
    //   style: 1.5.0~ 영웅 문양 (HeroAimKind 값, 0 = 기본 — 적 · 공용 고리는 지금 그대로)
    public static Sprite Get(float radius, float charge, bool plain = false, int style = 0)
    {
        int r = Mathf.Max(2, Mathf.RoundToInt(radius * ArtPPU));
        if (r > 12) r = (r + 1) & ~1;               // 큰 고리는 반지름 2칸 단위 (퍼지거나 좁혀지는 고리가 프레임마다 새 그림을 덜 만들게 · 어긋남 최대 1칸)
        int step = charge < 0f ? ChargeSteps : Mathf.Clamp(Mathf.FloorToInt(charge * ChargeSteps + 0.0001f), 0, ChargeSteps);
        long key = ((long)style << 40) | ((long)r << 9) | ((long)(plain ? 1 : 0) << 8) | (uint)step;
        tick++;
        if (cache.TryGetValue(key, out Entry e) && e.sprite != null) { e.used = tick; return e.sprite; }
        if (cache.Count >= CacheMax) Evict();
        Sprite s = Make(r, step, plain, style);
        cache[key] = new Entry { sprite = s, used = tick };
        return s;
    }

    static void Evict()
    {
        long oldest = 0; int min = int.MaxValue;
        foreach (KeyValuePair<long, Entry> kv in cache)
            if (kv.Value.used < min) { min = kv.Value.used; oldest = kv.Key; }
        if (cache.TryGetValue(oldest, out Entry e) && e.sprite != null)
        {
            Object.Destroy(e.sprite.texture);
            Object.Destroy(e.sprite);
        }
        cache.Remove(oldest);
    }

    static Sprite Make(int r, int step, bool plain, int style)
    {
        int n = 2 * (r + (style != 0 ? StylePad : Pad));
        Color[] px = new Color[n * n];          // 그림을 새로 만들 때만 (보관된 그림을 쓰면 할당 없음)
        float c = n * 0.5f;
        bool rich = r >= 10 && !plain;      // 작은 고리는 바깥 테 · 마름모만 (룬 · 안쪽 테가 뭉개지지 않게)
        float charge = step / (float)ChargeSteps;
        int runes = Mathf.Clamp(Mathf.RoundToInt(2f * Mathf.PI * r / 10f), 8, 40);

        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x + 0.5f - c, dy = y + 0.5f - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                Color col = Clear;
                // 안쪽 가장자리 디더 (바닥에 깔린 범위 느낌)
                if (rich && d > r - 11f && d < r - 6f && ((x + y) & 1) == 0) col = Dither;
                // 안쪽 가는 테 (외곽 + 1칸)
                if (rich)
                {
                    float di = Mathf.Abs(d - (r - 4.5f));
                    if (di < 1.1f) col = Outline;
                    if (di < 0.5f) col = Inner;
                }
                // 바깥 테 (외곽 + 2칸, 판정 원 = 가운데) · 모은 만큼 밝게 (위에서 시계 방향)
                float db = Mathf.Abs(d - r);
                if (db < 1.6f) col = Outline;
                if (db < 1.0f)
                {
                    float ang = Mathf.Repeat(Mathf.Atan2(dx, dy) / (2f * Mathf.PI), 1f);    // 위 = 0, 시계 방향 증가
                    col = ang <= charge ? Band : BandDim;
                }
                px[y * n + x] = col;
            }

        if (style != 0)
        {
            HeroMarks(px, n, c, r, rich, (HeroAimKind)style, runes);
            return Finish(px, n, r, step, plain, style);
        }

        // 룬 점 (두 테 사이, 짝수 번째는 2칸)
        if (rich)
            for (int k = 0; k < runes; k++)
            {
                float a = (k + 0.5f) / runes * 2f * Mathf.PI;
                int x = Mathf.FloorToInt(c + Mathf.Sin(a) * (r - 2.3f));
                int y = Mathf.FloorToInt(c + Mathf.Cos(a) * (r - 2.3f));
                Put(px, n, x, y, Light);
                if ((k & 1) == 0) Put(px, n, x + 1, y, Band);
            }

        // 네 방향 마름모 (바깥 테 위)
        int size = rich ? 3 : 2;
        for (int k = 0; k < (plain ? 0 : 4); k++)
        {
            float a = k * Mathf.PI * 0.5f;
            int mx = Mathf.FloorToInt(c + Mathf.Sin(a) * r);
            int my = Mathf.FloorToInt(c + Mathf.Cos(a) * r);
            for (int oy = -size; oy <= size; oy++)
                for (int ox = -size; ox <= size; ox++)
                {
                    int m = Mathf.Abs(ox) + Mathf.Abs(oy);
                    if (m <= size) Put(px, n, mx + ox, my + oy, Outline);
                    if (m <= size - 1) Put(px, n, mx + ox, my + oy, Band);
                    if (m == 0) Put(px, n, mx + ox, my + oy, Light);
                }
        }

        return Finish(px, n, r, step, plain, style);
    }

    static Sprite Finish(Color[] px, int n, int r, int step, bool plain, int style)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "pixel_ring_" + r + "_" + step + (plain ? "_plain" : "") + (style != 0 ? "_s" + style : ""),
            hideFlags = HideFlags.DontSave
        };
        tex.SetPixels(px);
        tex.Apply(false, true);                 // CPU 사본은 버림 (다시 읽지 않음 · 메모리 절반)
        Sprite s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), ArtPPU, 0, SpriteMeshType.FullRect);
        s.hideFlags = HideFlags.DontSave;
        return s;
    }

    // ---------------------------------------------------------------- 1.5.0~ 영웅 문양 (룬 점 · 네 방향 마름모 자리에)
    //   각도 a: 위 = 0, 시계 방향 · rad: 중심에서 거리 · tan: 테를 따라 옆으로 (칸)
    //   외곽은 빈 칸에만 둘러 줌 (테 · 차오름 표시를 덮지 않음) · 작은 고리(r < 10)는 상징 1~2칸만
    static readonly List<(int x, int y, Color col)> marks = new List<(int, int, Color)>(512);

    static void M(float c, float a, float rad, float tan, Color col)
    {
        float sn = Mathf.Sin(a), cs = Mathf.Cos(a);
        marks.Add((Mathf.FloorToInt(c + sn * rad + cs * tan), Mathf.FloorToInt(c + cs * rad - sn * tan), col));
    }

    static float Hash(int k) { float s = Mathf.Sin(k * 78.233f + 0.5f) * 43758.5453f; return s - Mathf.Floor(s); }

    static void HeroMarks(Color[] px, int n, float c, int r, bool rich, HeroAimKind k, int runes)
    {
        marks.Clear();
        float mid = r - 2.3f;               // 두 테 사이
        const float Q = Mathf.PI * 0.5f;
        switch (k)
        {
            case HeroAimKind.Gunner:        // 조준경: 네 방향 긴 십자 눈금 + 테 사이 탄환 점(2칸)
                for (int q = 0; q < 4; q++)
                    for (int d = rich ? -7 : -3; d <= 3; d++) M(c, q * Q, r + d, 0f, d >= -1 && d <= 1 ? Light : Band);
                if (rich)
                    for (int i = 0; i < runes; i++)
                    {
                        float a = (i + 0.5f) / runes * 2f * Mathf.PI;
                        M(c, a, mid + 0.5f, 0f, Light); M(c, a, mid - 0.5f, 0f, Band);
                    }
                break;
            case HeroAimKind.Swordsman:     // 테 사이 칼날 빗금 · 네 방향 칼끝 삼각 (작은 고리: 칼끝만)
                if (rich)
                    for (int i = 0; i < runes; i++)
                    {
                        float a = (i + 0.5f) / runes * 2f * Mathf.PI;
                        for (int j = -1; j <= 1; j++) M(c, a, mid + j, j, Light);
                    }
                for (int q = 0; q < 4; q++)
                {
                    float a = q * Q;
                    int h = rich ? 4 : 3;
                    for (int d = 1; d <= h; d++)
                        for (int w = -(h - d); w <= h - d; w++) M(c, a, r + d, w, d == h || w == 0 ? Light : Band);
                }
                break;
            case HeroAimKind.Rogue:         // 바깥 가시(2칸 톱니 16개) · 단검 점
                {
                    int spikes = rich ? 16 : 8;
                    for (int i = 0; i < spikes; i++)
                    {
                        float a = (i + 0.5f) / spikes * 2f * Mathf.PI;
                        M(c, a, r + 1.5f, 0f, Band); M(c, a, r + 2.5f, 0f, Light);
                        if (rich) M(c, a, r + 1.5f, 1f, Band);
                    }
                    if (rich)
                        for (int i = 0; i < runes; i += 2) { float a = (float)i / runes * 2f * Mathf.PI; M(c, a, mid, 0f, Light); M(c, a, mid - 1f, 0f, Band); }
                }
                break;
            case HeroAimKind.Archer:        // 테 사이 깃(∧) · 네 방향 화살촉
                if (rich)
                    for (int i = 0; i < runes; i++)
                    {
                        float a = (i + 0.5f) / runes * 2f * Mathf.PI;
                        M(c, a, mid + 0.5f, 0f, Light); M(c, a, mid - 0.5f, -1f, Band); M(c, a, mid - 0.5f, 1f, Band);
                    }
                for (int q = 0; q < 4; q++)
                {
                    float a = q * Q;
                    if (rich)
                    {
                        M(c, a, r + 4, 0, Light);
                        for (int w = -1; w <= 1; w++) M(c, a, r + 3, w, Band);
                        M(c, a, r + 2, -2, Band); M(c, a, r + 2, 0, Band); M(c, a, r + 2, 2, Band);
                    }
                    else { M(c, a, r + 3, 0, Light); M(c, a, r + 2, -1, Band); M(c, a, r + 2, 0, Band); M(c, a, r + 2, 1, Band); }
                }
                break;
            case HeroAimKind.Alchemist:     // 테 사이 거품(속 빈 3칸 고리) · 마름모 대신 작은 플라스크
                if (rich)
                    for (int i = 0; i < runes; i += 2)
                    {
                        float a = (i + 0.5f) / runes * 2f * Mathf.PI;
                        M(c, a, mid + 1f, 0f, Light); M(c, a, mid - 1f, 0f, Light); M(c, a, mid, -1f, Light); M(c, a, mid, 1f, Light);
                    }
                for (int q = 0; q < 4; q++)
                {
                    float a = q * Q;
                    int b = rich ? 1 : 0;
                    M(c, a, r + 3 + b, 0, Band); M(c, a, r + 2 + b, 0, Band);                 // 목
                    for (int w = -1; w <= 1; w++) { M(c, a, r + 1 + b, w, Light); if (rich) M(c, a, r + b, w, Light); }     // 몸통
                    if (rich) { M(c, a, r + 1 + b, -2, Band); M(c, a, r + 1 + b, 2, Band); }
                }
                break;
            case HeroAimKind.Cartographer:  // 나침반: 북쪽 긴 바늘 · 동서남 짧은 바늘 · 테 사이 점선 위도선
                for (int q = 0; q < 4; q++)
                {
                    float a = q * Q;
                    int h = q == 0 ? (rich ? 6 : 4) : (rich ? 3 : 2);
                    for (int d = 1; d <= h; d++) M(c, a, r + d, 0f, q == 0 ? Light : Band);
                    if (q == 0) { M(c, a, r + 1, -1f, Light); M(c, a, r + 1, 1f, Light); if (rich) { M(c, a, r + 2, -1f, Band); M(c, a, r + 2, 1f, Band); } }
                }
                if (rich)
                {
                    int dots = Mathf.Max(12, Mathf.RoundToInt(2f * Mathf.PI * mid / 3f));
                    for (int i = 0; i < dots; i++) M(c, (float)i / dots * 2f * Mathf.PI, mid, 0f, Band);
                }
                break;
            case HeroAimKind.Clockmaker:    // 문자판: 12 눈금 (3 · 6 · 9 · 12 는 길게) · 작은 고리는 4 눈금
                for (int i = 0; i < (rich ? 12 : 4); i++)
                {
                    float a = i * Mathf.PI * 2f / (rich ? 12 : 4);
                    bool major = !rich || i % 3 == 0;
                    int from = major ? (rich ? -6 : -3) : -3, to = major ? 2 : -1;
                    for (int d = from; d <= to; d++) M(c, a, r + d, 0f, major ? Light : Band);
                    if (major && rich) { M(c, a, r - 6, -1f, Band); M(c, a, r - 6, 1f, Band); }
                }
                break;
            case HeroAimKind.Postman:       // 우표 톱니: 바깥 가장자리에 구멍 · 그 사이 톱니 · 테 사이 소인 점선
                {
                    int teeth = Mathf.Max(10, Mathf.RoundToInt(2f * Mathf.PI * r / 3f));
                    for (int i = 0; i < teeth; i++)
                    {
                        float a = (float)i / teeth * 2f * Mathf.PI, a2 = (i + 0.5f) / teeth * 2f * Mathf.PI;
                        M(c, a2, r + 1.6f, 0f, Band); M(c, a2, r + 2.6f, 0f, Light);
                        int hx = Mathf.FloorToInt(c + Mathf.Sin(a) * (r + 1.3f)), hy = Mathf.FloorToInt(c + Mathf.Cos(a) * (r + 1.3f));
                        Put(px, n, hx, hy, Clear);           // 톱니 구멍 (외곽을 비움)
                    }
                    if (rich)
                        for (int i = 0; i < runes * 2; i++)
                        {
                            if ((i & 3) == 3) continue;
                            float a = (float)i / (runes * 2) * 2f * Mathf.PI;
                            M(c, a, mid + ((i & 1) == 0 ? 0.5f : -0.5f), 0f, Light);
                        }
                }
                break;
            case HeroAimKind.Ghost:         // 일렁이는 혼불 꼬리 (바깥으로 1~3칸 · 높이 고르지 않게)
                {
                    int hairs = Mathf.Max(10, Mathf.RoundToInt(2f * Mathf.PI * r / 2.5f));
                    for (int i = 0; i < hairs; i++)
                    {
                        float a = (i + 0.3f * Hash(i)) / hairs * 2f * Mathf.PI;
                        int h = 1 + Mathf.FloorToInt(Hash(i * 3 + 1) * (rich ? 3f : 2f));
                        for (int d = 0; d < h; d++) M(c, a, r + 1.5f + d, (d == h - 1 && (i & 1) == 0) ? 1f : 0f, d == h - 1 ? Light : Band);
                    }
                    if (rich)
                        for (int i = 0; i < runes; i += 3) { float a = (i + 0.5f) / runes * 2f * Mathf.PI; M(c, a, mid, 0f, Light); }
                }
                break;
        }
        // 외곽 (빈 칸에만) → 무늬
        foreach (var m in marks)
            for (int oy = -1; oy <= 1; oy++)
                for (int ox = -1; ox <= 1; ox++)
                {
                    int x = m.x + ox, y = m.y + oy;
                    if (x < 0 || y < 0 || x >= n || y >= n) continue;
                    if (px[y * n + x].a <= 0.001f) px[y * n + x] = Outline;
                }
        foreach (var m in marks) Put(px, n, m.x, m.y, m.col);
        marks.Clear();
    }

    static void Put(Color[] px, int n, int x, int y, Color col)
    {
        if (x < 0 || y < 0 || x >= n || y >= n) return;
        px[y * n + x] = col;
    }
}
