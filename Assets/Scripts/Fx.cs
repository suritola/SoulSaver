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

    // 1.0.2: 강해질수록 화려하게 — 플레이어가 낸 이펙트는 무기 진화 단계(기본 → 1차 강화 → 2차 진화)를 따라
    // 같은 크기의 _t2 · _t3 시트(광채 · 고리 · 파편 · 2차 폭발이 더해진 그림)로 바꿔 그림. 진화한 특수 능력은 바로 _t3. 시트가 없으면 기본 그대로
    public static int TierOf(string source)
    {
        if (source == null || source == DamageSource.Other) return 1;      // 적 · 처치 연출은 기본
        if (forcedTier > 1) return forcedTier;                             // 1.1.2~ 진화한 고유 스킬 (SignatureSkills.EvoFx)
        SpecialAbilities sp = SpecialAbilities.SharedInstance;
        if (sp == null) return 1;
        return sp.IsEvolvedSource(source) ? 3 : 1 + Mathf.Clamp(sp.EvolutionTier, 0, 2);
    }

    // 1.1.2~: 진화한 고유 스킬의 이펙트는 무기 단계와 상관없이 가장 화려한 단계(_t3)로
    //   using (Fx.Tier(3)) { ... } 안에서 만든 플레이어 이펙트에만 (0 · 1 이면 그대로)
    static int forcedTier;
    public readonly struct TierScope : System.IDisposable
    {
        readonly int prev;
        public TierScope(int tier) { prev = forcedTier; if (tier > forcedTier) forcedTier = tier; }
        public void Dispose() => forcedTier = prev;
    }
    public static TierScope Tier(int tier) => new TierScope(tier);

    // 최적화: 이름 · 단계마다 고른 시트를 기억 (예전엔 부를 때마다 name + "_t" + t 문자열 2개를 만들었음 · 초당 수백 번)
    //   단계마다 처음 필요할 때만 찾음 (쓰지 않는 _t2 · _t3 시트를 미리 불러오지 않음). 고르는 규칙은 같음: _t3 → _t2 → 기본
    static readonly Dictionary<string, Sprite[][]> tiered = new Dictionary<string, Sprite[][]>();
    static Sprite[] TieredFrames(string name)
    {
        int tier = Mathf.Clamp(TierOf(DamageSource.Current), 1, 3);
        if (!tiered.TryGetValue(name, out Sprite[][] byTier)) tiered[name] = byTier = new Sprite[4][];
        return TierSheet(name, byTier, tier);
    }

    static Sprite[] TierSheet(string name, Sprite[][] byTier, int tier)
    {
        if (byTier[tier] != null) return byTier[tier];
        Sprite[] f;
        if (tier <= 1) f = Frames(name);
        else
        {
            f = Frames(name + "_t" + tier);
            if (f == null || f.Length == 0) f = TierSheet(name, byTier, tier - 1);
        }
        byTier[tier] = f;
        return f;
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
    static float DecorScale => VisualRules.DecorScale;     // 1.0.2: 1.3 → 1.45 (장식만, 판정 범위인 이펙트는 그대로) · 2026-10-22~ VisualRules 로
    static readonly HashSet<string> AreaFx = new HashSet<string>
    {
        "fx_shock", "fx_explosion", "fx_rune", "fx_reticle", "fx_warn", "fx_target_rune", "fx_puddle", "fx_vortex",
        "fx_alchemyblast", "fx_fissure", "fx_geyser", "fx_firepillar", "fx_meteor", "fx_net", "fx_arrowrain", "fx_tornado",
        "fx_beam", "fx_bolt", "fx_orb", "fx_slash", "fx_swordswing", "fx_spinslash", "fx_bigsword", "fx_swordwave",
        "fx_slipper", "fx_scratcher", "fx_broom", "fx_codeblock",       // 개발자 · 엄마 고유 공격 (그림 크기 = 판정 크기)
        "fx_ind_disc", "fx_ind_floor", "fx_ind_band", "fx_ind_cone",    // 1.5.1~ 범위 · 예고 도트 그림 (가장자리 = 판정 · PixelLine)
    };
    static float Visible(string name, float size) => AreaFx.Contains(name) ? size : size * DecorScale;

    // 2.1.3: 플레이어 스킬(필살기 · 특수 능력 · 고유 스킬 · 영혼 트리 · 진화) 이펙트를 확 눈에 띄게
    // 지금 스킬이 쓰는 중인지는 DamageSource 로 앎 (평타 · 적 · 맞은 불꽃 · 처치 연출은 그대로)
    //   더 크게(장식만, 범위가 곧 판정인 이펙트는 그대로) · 더 밝고 진하게 · 적 이펙트 위에 · 밑에 빛 번짐
    static float SkillDecor => VisualRules.SkillDecor;     // 1.0.2: 1.25 → 1.4 · 2026-10-22~ VisualRules 로
    const int SkillOrder = 40;
    static readonly HashSet<string> NoBoost = new HashSet<string>
    {
        "fx_spark", "fx_smoke", "fx_bleed", "fx_deathburst", "fx_warn", "fx_reticle", "fx_target_rune",
    };
    // 이 이름 · 지금 출처의 장식 배율이 바뀌어 보이는 크기로 잰 판정이 달라지는 만큼 되돌리는 값 (판정 범위 이펙트는 1)
    public static float JudgeKeep(string name) => AreaFx.Contains(name) ? 1f : VisualRules.DecorKeep(SkillFx(name));

    static bool SkillFx(string name)
    {
        string s = DamageSource.Current;
        return s != null && s != DamageSource.Basic && s != DamageSource.Other && !NoBoost.Contains(name);
    }

    // 1.0.2: 플레이어가 낸 이펙트(평타 포함)는 설정의 「내 이펙트 투명도」를 따름 — 적 공격 · 처치 연출은 그대로
    static bool Mine => DamageSource.Current != null && DamageSource.Current != DamageSource.Other;
    static readonly HashSet<string> Markers = new HashSet<string> { "fx_warn", "fx_reticle", "fx_target_rune", "fx_ind_disc", "fx_ind_floor", "fx_ind_band", "fx_ind_cone" };     // 조준 · 경고 표시는 흐리게 하지 않음
    static Color MyAlpha(Color c, string name = null) { if (Mine && (name == null || !Markers.Contains(name))) c.a *= GameSettings.MyFxAlpha; return c; }

    static Color Brighten(Color c) => new Color(Mathf.Lerp(c.r, 1f, 0.35f), Mathf.Lerp(c.g, 1f, 0.35f), Mathf.Lerp(c.b, 1f, 0.35f), Mathf.Max(c.a, 0.97f));     // 1.0.2: 0.25 → 0.35

    // 이펙트 밑에 잠깐 번지는 빛 (Glow 스프라이트는 지름 약 8칸 = 크기 1)
    static void SkillGlow(Vector3 pos, float size, Color c, int order, string layer)
    {
        if (Hostile.Glow == null || size < 1.2f) return;
        if (GraphicsQuality.Low) return;   // 그래픽 품질 낮음(L2): 빛 번짐 생략
        bool evo = forcedTier >= 3;        // 1.1.2~ 진화한 고유 스킬: 빛 번짐도 더 크고 길게
        FadeSprite.Spawn("SkillGlow", Hostile.Glow, pos, size * (evo ? 2.2f : 1.7f) / 8f, MyAlpha(new Color(Mathf.Lerp(c.r, 1f, 0.4f), Mathf.Lerp(c.g, 1f, 0.4f), Mathf.Lerp(c.b, 1f, 0.4f), evo ? 0.85f : 0.7f)), layer, order - 1, evo ? 0.55f : 0.38f);     // 1.0.2: 더 크고 진하게
    }

    static void Style(SpriteRenderer sr)
    {
        Material m = SpriteOutline.EffectMaterial;
        if (GraphicsQuality.Low)
        {
            // 그래픽 품질 낮음(L3): 이펙트는 기본 스프라이트 재질 (윤곽선 셰이더의 픽셀당 여러 번 읽기를 줄임 · 적 · 플레이어 윤곽선은 그대로)
            //   새 SpriteRenderer 는 이미 기본 재질 · 높음에서 쓰던 것을 다시 쓰면 기본으로 되돌림
            if (m != null && sr.sharedMaterial == m) sr.sharedMaterial = SpriteDefault;
            return;
        }
        if (m != null) sr.sharedMaterial = m;
    }

    static Material spriteDefault;
    static Material SpriteDefault
    {
        get
        {
            if (spriteDefault == null)
            {
                GameObject tmp = new GameObject("FxDefaultMat");
                spriteDefault = tmp.AddComponent<SpriteRenderer>().sharedMaterial;
                Object.Destroy(tmp);
            }
            return spriteDefault;
        }
    }

    // 그래픽 품질 낮음(L4): 꾸밈 이펙트(불꽃 튐 · 반짝임 · 핏방울 · 연기)는 0.05초에 12개까지 (넘치면 생략, 처치 연출 상한과 같은 방식)
    static readonly HashSet<string> DecorFx = new HashSet<string> { "fx_spark", "fx_sparkle", "fx_bleed", "fx_smoke" };
    static float decorWindow = -1f;
    static int decorCount;
    static bool DecorBudgetFull(string name)
    {
        if (!GraphicsQuality.Low || !DecorFx.Contains(name)) return false;
        if (Time.time - decorWindow >= 0.05f || Time.time < decorWindow) { decorWindow = Time.time; decorCount = 0; }
        if (decorCount >= 12) return true;
        decorCount++;
        return false;
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
        Sprite[] frames = TieredFrames(name);
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
        sr.color = MyAlpha(skill ? Brighten(color ?? Color.white) : color ?? Color.white, name);
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
        => SpawnGet(name, pos, size, color, fps, rotation, order, loop, life, layer);

    // 1.2.5~ 결계 예고 느낌표: 튀어나왔다가 두근거림 (예전 WarnPop 컴포넌트를 새 오브젝트마다 붙이던 것 → 풀 이펙트의 pop 표시)
    // 반복 재생 · life 동안 (Play 와 같은 모습 · 다시 쓸 때 Setup 이 pop 을 끔)
    public static void SpawnPop(string name, Vector3 pos, float size, Color color, float fps, int order, float life)
    {
        FxAnim a = SpawnGet(name, pos, size, color, fps, 0f, order, true, life, "Effect");
        if (a == null) return;
        a.pop = true;
        a.popBase = a.transform.localScale;
    }

    static FxAnim SpawnGet(string name, Vector3 pos, float size, Color? color, float fps,
                           float rotation, int order, bool loop, float life, string layer)
    {
        if (DecorBudgetFull(name)) return null;
        Sprite[] frames = TieredFrames(name);
        if (frames == null || frames.Length == 0) return null;

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
        return a;
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
        sr.color = MyAlpha(color ?? Color.white, name);
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
        a.pop = false;          // 1.2.5: 풀로 돌아간 느낌표가 다른 이펙트에서 튀지 않게
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

// 최적화: 수천 개가 동시에 뜨는 이펙트(FxAnim · FadeSprite · FlameParticle)가 저마다 Update 를 받던 것을
//   FxRunner 하나의 Update 가 차례로 Tick 을 부르게 바꿈 (계산 · 순서 규칙은 그대로 · 켜진 동안만 목록에 있음)
public abstract class FxTicked : MonoBehaviour
{
    internal int tickIndex = -1;
    internal int tickedFrame = -1;
    internal abstract void Tick();

    protected virtual void OnEnable() => FxRunner.Add(this);
    protected virtual void OnDisable() => FxRunner.Remove(this);
}

public sealed class FxRunner : MonoBehaviour
{
    static FxRunner instance;
    static readonly List<FxTicked> live = new List<FxTicked>(512);

    internal static void Add(FxTicked x)
    {
        if (x.tickIndex >= 0) return;
        if (instance == null)
        {
            GameObject go = new GameObject("FxRunner");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<FxRunner>();
        }
        x.tickIndex = live.Count;
        x.tickedFrame = Time.frameCount;        // 새로 만든 오브젝트처럼, 켜진 그 프레임에는 Tick 하지 않음
        live.Add(x);
    }

    internal static void Remove(FxTicked x)
    {
        int i = x.tickIndex;
        if (i < 0) return;
        x.tickIndex = -1;
        if (i >= live.Count || !ReferenceEquals(live[i], x)) return;
        RemoveAt(i);
    }

    static void RemoveAt(int i)
    {
        int last = live.Count - 1;
        if (i != last)
        {
            FxTicked moved = live[last];
            live[i] = moved;
            if (!ReferenceEquals(moved, null)) moved.tickIndex = i;
        }
        live.RemoveAt(last);
    }

    void Update()
    {
        int frame = Time.frameCount;
        // 뒤에서부터: Tick 안에서 꺼지면(풀로 돌아감) 맨 끝 것이 그 자리로 옴 — 이미 돈 것이라 tickedFrame 으로 두 번 돌지 않음
        for (int i = live.Count - 1; i >= 0; i--)
        {
            if (i >= live.Count) continue;
            FxTicked x = live[i];
            if (x == null) { RemoveAt(i); continue; }       // 꺼짐 알림 없이 사라진 것 (안전장치)
            if (x.tickedFrame == frame) continue;
            x.tickedFrame = frame;
            x.Tick();
        }
    }
}

// 프레임 애니메이션: 한 번 재생 후 사라지거나(loop = false), life 동안 반복
public class FxAnim : FxTicked
{
    public Sprite[] frames;
    public float fps = 16f;
    public bool loop;
    public float life = -1f;       // 반복일 때 수명 (-1이면 직접 지울 때까지)
    public float spin;             // 초당 회전 (룬 등)
    public Transform follow;       // 따라다닐 대상
    public SpriteRenderer sr;
    public bool pooled;             // Fx.Spawn 으로 만든 것: 끝나면 지우지 않고 다시 씀
    public bool pop;                // 1.2.5~ 결계 예고 느낌표 (0.15초 동안 1.8 → 1배, 그 뒤 두근거림 · Fx.SpawnPop)
    public Vector3 popBase;
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

    internal override void Tick()
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
        Sprite want = frames[i];
        if (!ReferenceEquals(sr.sprite, want)) sr.sprite = want;      // 최적화: 같은 그림이면 다시 쓰지 않음
        if (spin != 0f) transform.Rotate(0f, 0f, spin * Time.deltaTime);
        if (follow != null) transform.position = follow.position;
        if (pop) transform.localScale = popBase * (age < 0.15f ? Mathf.Lerp(1.8f, 1f, age / 0.15f) : 1f + 0.12f * Mathf.Sin(age * 18f));

        if (life > 0f)
        {
            // 끝나기 직전 0.15초 동안 흐려짐
            float left = life - age;
            if (left < 0.15f) sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * Mathf.Clamp01(left / 0.15f));
            if (left <= 0f) Finish();
        }
    }
}
