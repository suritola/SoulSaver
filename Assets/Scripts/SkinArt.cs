using System.Collections.Generic;
using UnityEngine;

// 스킨 연출 그림 (스킨 시청각 개편 1단계): 코드로 그린 작은 도트 글리프(SkinArt.Glyphs.cs) → 처음 쓸 때 한 번만 텍스처로
//   그림은 흰 · 회색 명암만, 색은 쓰는 곳(SkinMote)에서 입힘 · 16칸 = 1칸 크기 기준이 아니라 쓰는 곳이 높이를 정함
//   "glow" = 부드러운 빛 점 (게임의 GlowSprite, 메뉴에선 같은 크기 대용)
public static partial class SkinArt
{
    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static bool Has(string name) => name == "glow" || Patterns.ContainsKey(name);

    public static Sprite Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (name == "glow") return Glow;
        if (cache.TryGetValue(name, out Sprite s) && s != null) return s;
        if (!Patterns.TryGetValue(name, out string[] rows) || rows.Length == 0)
        {
            // 없는 이름은 점으로 (개발 빌드에서만 알림)
            if (Debug.isDebugBuild) Debug.LogWarning("[SkinArt] 없는 글리프: " + name);
            return name == "dot" ? null : cache[name] = Get("dot");
        }
        int h = rows.Length, w = 0;
        foreach (string r in rows) w = Mathf.Max(w, r.Length);
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "sk_" + name };
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            string row = rows[h - 1 - y];       // 텍스처는 아래 줄부터
            for (int x = 0; x < w; x++)
            {
                char ch = x < row.Length ? row[x] : '.';
                px[y * w + x] = Shade(ch);
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        s = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
        s.name = "sk_" + name;
        cache[name] = s;
        return s;
    }

    static Color32 Shade(char ch)
    {
        switch (ch)
        {
            case '#': return new Color32(255, 255, 255, 255);
            case '=': return new Color32(204, 204, 204, 255);
            case '+': return new Color32(158, 158, 158, 255);
            case '-': return new Color32(115, 115, 115, 255);
            case 'k': return new Color32(46, 46, 46, 255);
            case ':': return new Color32(255, 255, 255, 128);
            default: return new Color32(0, 0, 0, 0);
        }
    }

    // 부드러운 빛 점: 게임 장면이면 GlowSprite (지름 약 8칸 = 크기 1), 메뉴 · 상점이면 같은 크기 대용
    static Sprite softGlow;
    public static Sprite Glow
    {
        get
        {
            if (SpecialAbilities.GlowSprite != null) return SpecialAbilities.GlowSprite;
            if (softGlow != null) return softGlow;
            const int N = 64;
            Texture2D tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Color32[] px = new Color32[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return softGlow = Sprite.Create(tex, new Rect(0f, 0f, N, N), Vector2.one * 0.5f, 8f);     // 64px · 8 = 8칸 (게임 빛 그림과 같은 크기)
        }
    }
}
