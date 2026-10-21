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
    const int CacheMax = 192;               // 그림 수 상한 (넘으면 가장 오래 안 쓴 것부터 버림 · 퍼지는 충격파는 반지름마다 새 그림이라 넉넉히)

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
    public static Sprite Get(float radius, float charge, bool plain = false)
    {
        int r = Mathf.Max(2, Mathf.RoundToInt(radius * ArtPPU));
        if (r > 12) r = (r + 1) & ~1;               // 큰 고리는 반지름 2칸 단위 (퍼지거나 좁혀지는 고리가 프레임마다 새 그림을 덜 만들게 · 어긋남 최대 1칸)
        int step = charge < 0f ? ChargeSteps : Mathf.Clamp(Mathf.FloorToInt(charge * ChargeSteps + 0.0001f), 0, ChargeSteps);
        long key = ((long)r << 9) | ((long)(plain ? 1 : 0) << 8) | (uint)step;
        tick++;
        if (cache.TryGetValue(key, out Entry e) && e.sprite != null) { e.used = tick; return e.sprite; }
        if (cache.Count >= CacheMax) Evict();
        Sprite s = Make(r, step, plain);
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

    static Sprite Make(int r, int step, bool plain)
    {
        int n = 2 * (r + Pad);
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

        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = "pixel_ring_" + r + "_" + step + (plain ? "_plain" : ""),
            hideFlags = HideFlags.DontSave
        };
        tex.SetPixels(px);
        tex.Apply(false, true);                 // CPU 사본은 버림 (다시 읽지 않음 · 메모리 절반)
        Sprite s = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), ArtPPU, 0, SpriteMeshType.FullRect);
        s.hideFlags = HideFlags.DontSave;
        return s;
    }

    static void Put(Color[] px, int n, int x, int y, Color col)
    {
        if (x < 0 || y < 0 || x >= n || y >= n) return;
        px[y * n + x] = col;
    }
}
