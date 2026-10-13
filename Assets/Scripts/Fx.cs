using System.Collections.Generic;
using UnityEngine;

// 도트 이펙트 재생기: Resources/FX 의 스프라이트 시트(이름_0, 이름_1 ...)를 프레임 애니메이션으로 재생
// 예) Fx.Play("fx_explosion", pos, 4f)  → 지름 4칸 크기의 폭발
public static class Fx
{
    static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();

    public static Sprite[] Frames(string name)
    {
        if (cache.TryGetValue(name, out Sprite[] frames)) return frames;
        frames = Resources.LoadAll<Sprite>("FX/" + name);
        System.Array.Sort(frames, (a, b) => Index(a.name).CompareTo(Index(b.name)));
        cache[name] = frames;
        return frames;
    }

    // 정렬 층 이름 → 번호 (이펙트마다 이름으로 찾지 않게 한 번만)
    static readonly Dictionary<string, int> layerIds = new Dictionary<string, int>();
    static int LayerId(string layer)
    {
        if (!layerIds.TryGetValue(layer, out int id)) layerIds[layer] = id = SortingLayer.NameToID(layer);
        return id;
    }

    // 1.8.7: 이펙트를 더 잘 보이게 — 장식 이펙트는 1.3배로 키우고, 크기가 곧 판정 범위인 이펙트는 그대로 (범위를 속이지 않게)
    // 모든 이펙트에 짙은 윤곽선 · 채도 · 밝기 (SpriteOutline.EffectMaterial)
    const float DecorScale = 1.45f;        // 1.1.1: 1.3 → 1.45 (장식만, 판정 범위인 이펙트는 그대로)
    static readonly HashSet<string> AreaFx = new HashSet<string>
    {
        "fx_shock", "fx_explosion", "fx_rune", "fx_reticle", "fx_warn", "fx_target_rune", "fx_puddle", "fx_vortex",
        "fx_alchemyblast", "fx_fissure", "fx_geyser", "fx_firepillar", "fx_meteor", "fx_net", "fx_arrowrain", "fx_tornado",
        "fx_beam", "fx_bolt", "fx_orb", "fx_slash", "fx_swordswing", "fx_spinslash", "fx_bigsword", "fx_swordwave",
    };
    static float Visible(string name, float size) => AreaFx.Contains(name) ? size : size * DecorScale;

    // 2.1.3: 플레이어 스킬(필살기 · 특수 능력 · 고유 스킬 · 영혼 트리 · 진화) 이펙트를 확 눈에 띄게
    // 지금 스킬이 쓰는 중인지는 DamageSource 로 앎 (평타 · 적 · 맞은 불꽃 · 처치 연출은 그대로)
    //   더 크게(장식만, 범위가 곧 판정인 이펙트는 그대로) · 더 밝고 진하게 · 적 이펙트 위에 · 밑에 빛 번짐
    const float SkillDecor = 1.4f;         // 1.1.1: 1.25 → 1.4
    const int SkillOrder = 40;
    static readonly HashSet<string> NoBoost = new HashSet<string>
    {
        "fx_spark", "fx_smoke", "fx_bleed", "fx_deathburst", "fx_warn", "fx_reticle", "fx_target_rune",
    };
    static bool SkillFx(string name)
    {
        string s = DamageSource.Current;
        return s != null && s != DamageSource.Basic && s != DamageSource.Other && !NoBoost.Contains(name);
    }

    static Color Brighten(Color c) => new Color(Mathf.Lerp(c.r, 1f, 0.35f), Mathf.Lerp(c.g, 1f, 0.35f), Mathf.Lerp(c.b, 1f, 0.35f), Mathf.Max(c.a, 0.97f));     // 1.1.1: 0.25 → 0.35

    // 이펙트 밑에 잠깐 번지는 빛 (Glow 스프라이트는 지름 약 8칸 = 크기 1)
    static void SkillGlow(Vector3 pos, float size, Color c, int order, string layer)
    {
        if (Hostile.Glow == null || size < 1.2f) return;
        FadeSprite.Spawn("SkillGlow", Hostile.Glow, pos, size * 1.7f / 8f, new Color(Mathf.Lerp(c.r, 1f, 0.4f), Mathf.Lerp(c.g, 1f, 0.4f), Mathf.Lerp(c.b, 1f, 0.4f), 0.7f), layer, order - 1, 0.38f);     // 1.1.1: 더 크고 진하게
    }

    static void Style(SpriteRenderer sr)
    {
        Material m = SpriteOutline.EffectMaterial;
        if (m != null) sr.sharedMaterial = m;
    }

    static int Index(string spriteName)
    {
        int i = spriteName.LastIndexOf('_');
        return i >= 0 && int.TryParse(spriteName.Substring(i + 1), out int n) ? n : 0;
    }

    // size: 스프라이트 높이를 이 월드 크기로 맞춤
    public static FxAnim Play(string name, Vector3 pos, float size, Color? color = null, float fps = 16f,
                              float rotation = 0f, int order = 12, bool loop = false, float life = -1f, string layer = "Effect")
    {
        Sprite[] frames = Frames(name);
        if (frames == null || frames.Length == 0) return null;

        size = Visible(name, size);
        bool skill = SkillFx(name);
        if (skill)
        {
            if (!AreaFx.Contains(name)) size *= SkillDecor;
            order += SkillOrder;
            if (!loop) SkillGlow(pos, size, color ?? Color.white, order, layer);
        }
        GameObject go = new GameObject(name);
        go.transform.position = new Vector3(pos.x, pos.y, 0f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        Style(sr);
        sr.sprite = frames[0];
        sr.color = skill ? Brighten(color ?? Color.white) : color ?? Color.white;
        sr.sortingLayerID = LayerId(layer);
        sr.sortingOrder = order;
        float h = frames[0].bounds.size.y;
        go.transform.localScale = Vector3.one * (h > 0f ? size / h : 1f);

        FxAnim a = go.AddComponent<FxAnim>();
        a.frames = frames;
        a.fps = fps;
        a.loop = loop;
        a.life = life;
        a.sr = sr;
        return a;
    }

    // ================================================================= 만들고 잊는 이펙트 (다시 쓰기)
    // 돌려받은 값을 쓰지 않는 호출 전용: 다 끝난 이펙트를 지우지 않고 꺼 두었다가 다음에 다시 씀
    // (보이는 모습 · 시간 · 크기는 Play 와 똑같음. 누가 붙잡고 있는 이펙트는 Play 를 그대로 씀)
    internal static readonly Stack<FxAnim> pool = new Stack<FxAnim>();

    public static void Spawn(string name, Vector3 pos, float size, Color? color = null, float fps = 16f,
                             float rotation = 0f, int order = 12, bool loop = false, float life = -1f, string layer = "Effect")
    {
        Sprite[] frames = Frames(name);
        if (frames == null || frames.Length == 0) return;

        FxAnim a = null;
        while (pool.Count > 0 && a == null) a = pool.Pop();     // 장면이 바뀌어 지워진 것은 건너뜀
        if (a == null)
        {
            GameObject fresh = new GameObject("Fx");
            SpriteRenderer fsr = fresh.AddComponent<SpriteRenderer>();
            a = fresh.AddComponent<FxAnim>();
            a.sr = fsr;
            a.pooled = true;
        }
        bool skill = SkillFx(name);
        if (skill)
        {
            if (!AreaFx.Contains(name)) size *= SkillDecor;
            order += SkillOrder;
            if (!loop) SkillGlow(pos, Visible(name, size), color ?? Color.white, order, layer);
            color = Brighten(color ?? Color.white);
        }
        Setup(a, name, frames, pos, size, color, fps, rotation, order, loop, life, layer);
        a.gameObject.SetActive(true);
    }

    // 이미 있는 이펙트를 다른 모습으로 처음부터 다시 재생 (매 프레임 다시 그리는 조준 표시 등)
    // 보이는 모습은 Play 와 같음. 시트가 없으면 false
    public static bool Reuse(FxAnim a, string name, Vector3 pos, float size, Color? color = null, float fps = 16f,
                             float rotation = 0f, int order = 12, bool loop = false, float life = -1f, string layer = "Effect")
    {
        Sprite[] frames = Frames(name);
        if (a == null || frames == null || frames.Length == 0) return false;
        Setup(a, name, frames, pos, size, color, fps, rotation, order, loop, life, layer);
        if (!a.gameObject.activeSelf) a.gameObject.SetActive(true);
        return true;
    }

    static void Setup(FxAnim a, string name, Sprite[] frames, Vector3 pos, float size, Color? color, float fps,
                      float rotation, int order, bool loop, float life, string layer)
    {
        size = Visible(name, size);
        Transform tr = a.transform;
        tr.position = new Vector3(pos.x, pos.y, 0f);
        tr.rotation = Quaternion.Euler(0f, 0f, rotation);
        SpriteRenderer sr = a.sr;
        Style(sr);
        sr.sprite = frames[0];
        sr.color = color ?? Color.white;
        sr.sortingLayerID = LayerId(layer);
        sr.sortingOrder = order;
        float h = frames[0].bounds.size.y;
        tr.localScale = Vector3.one * (h > 0f ? size / h : 1f);
        a.frames = frames;
        a.fps = fps;
        a.loop = loop;
        a.life = life;
        a.spin = 0f;
        a.follow = null;
        a.Restart();
    }

    // 두 점 사이를 잇는 도트 빔 (fx_beam을 길이 방향으로 늘림)
    public static FxAnim Beam(Vector3 from, Vector3 to, float width, Color color, float life, int order = 14)
    {
        Vector3 d = to - from;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        FxAnim a = Play("fx_beam", (from + to) * 0.5f, width, color, 20f, angle, order, true, life);
        if (a != null)
        {
            float unit = a.sr.sprite.bounds.size.x;
            Vector3 s = a.transform.localScale;
            a.transform.localScale = new Vector3(d.magnitude / unit, s.y, 1f);
        }
        return a;
    }

    // 두 점 사이에 번개 (fx_bolt를 세로로 늘려 회전)
    public static FxAnim Bolt(Vector3 from, Vector3 to, float width, Color color, float life = 0.18f)
    {
        Vector3 d = to - from;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f;
        FxAnim a = Play("fx_bolt", (from + to) * 0.5f, d.magnitude, color, 24f, angle, 22, true, life);
        if (a != null)
        {
            float w = a.sr.sprite.bounds.size.x;
            Vector3 s = a.transform.localScale;
            a.transform.localScale = new Vector3(width / w, s.y, 1f);
        }
        return a;
    }
}

// 프레임 애니메이션: 한 번 재생 후 사라지거나(loop = false), life 동안 반복
public class FxAnim : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 16f;
    public bool loop;
    public float life = -1f;       // 반복일 때 수명 (-1이면 직접 지울 때까지)
    public float spin;             // 초당 회전 (룬 등)
    public Transform follow;       // 따라다닐 대상
    public SpriteRenderer sr;
    public bool pooled;             // Fx.Spawn 으로 만든 것: 끝나면 지우지 않고 다시 씀
    float t;
    float age;
    Color baseColor;
    bool started;

    // 다시 쓸 때 처음부터
    public void Restart()
    {
        t = 0f;
        age = 0f;
        started = false;
        spawnFrame = Time.frameCount;
    }

    int spawnFrame = -1;

    // 끝남: 다시 쓰는 이펙트는 꺼서 보관, 아니면 지움
    void Finish()
    {
        if (!pooled) { Destroy(gameObject); return; }
        gameObject.SetActive(false);
        Fx.pool.Push(this);
    }

    void Update()
    {
        // 새로 만든 오브젝트처럼, 켜진 그 프레임에는 움직이지 않음
        if (Time.frameCount == spawnFrame) return;
        if (!started)
        {
            started = true;
            baseColor = sr.color;
        }
        t += Time.deltaTime * fps;
        age += Time.deltaTime;
        int i = (int)t;
        if (i >= frames.Length)
        {
            if (!loop) { Finish(); return; }
            i %= frames.Length;
        }
        sr.sprite = frames[i];
        if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
        if (follow != null) transform.position = follow.position;

        if (life > 0f)
        {
            // 끝나기 직전 0.15초 동안 흐려짐
            float left = life - age;
            if (left < 0.15f) sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * Mathf.Clamp01(left / 0.15f));
            if (left <= 0f) Finish();
        }
    }
}
