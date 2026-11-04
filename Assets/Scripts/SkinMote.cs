using System.Collections.Generic;
using UnityEngine;

// 스킨 연출 입자 (스킨 시청각 개편 1단계 · 2026-11-04): 풀에서 다시 쓰는 그림 한 장
//   속도 · 중력 · 흔들림 · 위로 솟는 나선 · 몸으로 흡수 · 실제 시간(사망 연출) — 스킨 FX 는 모두 이것 하나로 (SkinFx.Spawn 통로)
//   새로 만들고 지우지 않음 (1.1.8~1.2.0 자기 복제 렉 사고) · 동시 상한 Cap 을 넘으면 그 입자는 생략
public sealed class SkinMote : FxTicked
{
    static readonly Stack<SkinMote> pool = new Stack<SkinMote>();
    static readonly Dictionary<string, int> layerIds = new Dictionary<string, int>();
    public static int Live { get; private set; }
    public const int Cap = 360;

    static Material defaultMat, silhouetteMat;
    public static Material Silhouette
    {
        get
        {
            if (silhouetteMat == null)
            {
                Shader s = Resources.Load<Shader>("Shaders/SkinSilhouette");
                if (s != null) silhouetteMat = new Material(s) { name = "SkinSilhouette", hideFlags = HideFlags.DontUnloadUnusedAsset };
            }
            return silhouetteMat;
        }
    }

    SpriteRenderer sr;
    Vector3 pos, vel, origin;
    float grav, drag, life, age, s0, s1, spin, swayA, swayF, phase, helixR, helixW, helixV, hold = 0.5f, arc, stretchY = 1f;
    float baseScale, widthMul = 1f, fps;
    Sprite[] frames;
    Color col;
    bool unscaled, helix, alive, flip;
    Transform target;
    Vector3 targetOff;
    int spawnFrame = -1;

    public static SkinMote Make(Sprite sp, Vector3 at, float size, Color c, string layer, int order, float life, bool unscaled = false, bool silhouette = false)
    {
        if (sp == null || Live >= Cap || life <= 0f) return null;
        SkinMote m = null;
        while (pool.Count > 0 && m == null) m = pool.Pop();     // 장면이 바뀌어 지워진 것은 건너뜀
        if (m == null)
        {
            GameObject go = new GameObject("SkinMote");
            m = go.AddComponent<SkinMote>();
            m.sr = go.AddComponent<SpriteRenderer>();
            if (defaultMat == null) defaultMat = m.sr.sharedMaterial;
        }
        if (!layerIds.TryGetValue(layer, out int id)) layerIds[layer] = id = SortingLayer.NameToID(layer);
        Material want = silhouette && Silhouette != null ? Silhouette : defaultMat;
        if (want != null && m.sr.sharedMaterial != want) m.sr.sharedMaterial = want;
        m.sr.sprite = sp;
        m.sr.sortingLayerID = id;
        m.sr.sortingOrder = order;
        m.sr.flipX = m.sr.flipY = false;
        float h = sp.bounds.size.y;
        m.baseScale = h > 0f ? size / h : 1f;
        m.pos = m.origin = new Vector3(at.x, at.y, 0f);
        m.vel = Vector3.zero;
        m.grav = m.drag = m.spin = m.swayA = m.swayF = m.phase = m.helixR = m.helixW = m.helixV = m.arc = 0f;
        m.s0 = m.s1 = 1f;
        m.stretchY = 1f;
        m.widthMul = 1f;
        m.frames = null;
        m.hold = 0.5f;
        m.life = life;
        m.age = 0f;
        m.col = c;
        m.unscaled = unscaled;
        m.helix = false;
        m.flip = false;
        m.target = null;
        Transform tr = m.transform;
        tr.position = m.pos;
        tr.rotation = Quaternion.identity;
        tr.localScale = Vector3.one * m.baseScale;
        m.sr.color = new Color(c.r, c.g, c.b, 0f);
        m.spawnFrame = Time.frameCount;
        m.alive = true;
        Live++;
        m.gameObject.SetActive(true);
        return m;
    }

    // ---- 움직임 (Make 바로 뒤에 이어 부름)
    public SkinMote Move(Vector3 v, float gravity = 0f, float dragPerSec = 0f) { vel = v; grav = gravity; drag = dragPerSec; return this; }
    public SkinMote Grow(float from, float to) { s0 = from; s1 = to; return this; }
    public SkinMote Spin(float degPerSec, float startDeg = 0f) { spin = degPerSec; transform.rotation = Quaternion.Euler(0f, 0f, startDeg); return this; }
    public SkinMote Sway(float amp, float freq) { swayA = amp; swayF = freq; phase = Random.value * 6.283f; return this; }
    public SkinMote Hold(float k) { hold = Mathf.Clamp01(k); return this; }
    public SkinMote Stretch(float y) { stretchY = y; return this; }
    public SkinMote Flip(bool on) { flip = on; return this; }
    public SkinMote Width(float mul) { widthMul = mul; return this; }
    // 기존 Fx 시트처럼 여러 프레임을 한 번 재생 (마지막 프레임에서 멈춤)
    public SkinMote Anim(Sprite[] f, float framesPerSec) { if (f != null && f.Length > 1) { frames = f; fps = framesPerSec; } return this; }
    // 위로 솟는 나선 (반지름 · 초당 회전 · 오르는 속도) — 몸 둘레를 같은 높이로 도는 무리 금지(L12)라 늘 오름
    public SkinMote Helix(float r, float w, float up, float startAngle) { helix = true; helixR = r; helixW = w; helixV = Mathf.Max(0.3f, up); phase = startAngle; return this; }
    // A → 몸(target) 으로 날아듦 (호를 그리며)
    public SkinMote Absorb(Transform to, Vector3 offset, float arcHeight) { target = to; targetOff = offset; arc = arcHeight; return this; }

    void Finish()
    {
        if (alive) { alive = false; Live--; }
        gameObject.SetActive(false);
        target = null;
        frames = null;
        pool.Push(this);
    }

    void OnDestroy()
    {
        if (alive) { alive = false; Live--; }
    }

    internal override void Tick()
    {
        if (Time.frameCount == spawnFrame) return;
        float dt = unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
        age += dt;
        float k = Mathf.Clamp01(age / life);
        Vector3 p;
        if (target != null)
        {
            float e = k * k * (3f - 2f * k);
            Vector3 to = target.position + targetOff;
            p = Vector3.Lerp(origin, to, e) + Vector3.up * Mathf.Sin(Mathf.PI * k) * arc;
        }
        else if (helix)
        {
            float a = phase + helixW * age;
            float r = helixR * (1f + 0.35f * k);
            p = origin + new Vector3(Mathf.Cos(a) * r, helixV * age + Mathf.Sin(a) * r * 0.25f, 0f);
        }
        else
        {
            vel.y -= grav * dt;
            if (drag > 0f) vel *= Mathf.Max(0f, 1f - drag * dt);
            pos += vel * dt;
            p = pos;
            if (swayA != 0f) p.x += Mathf.Sin(age * swayF + phase) * swayA;
        }
        Transform tr = transform;
        tr.position = p;
        if (spin != 0f) tr.Rotate(0f, 0f, spin * dt);
        float s = baseScale * Mathf.Lerp(s0, s1, k);
        tr.localScale = new Vector3((flip ? -s : s) * widthMul, s * stretchY, 1f);
        if (frames != null)
        {
            Sprite want = frames[Mathf.Min(frames.Length - 1, (int)(age * fps))];
            if (!ReferenceEquals(sr.sprite, want)) sr.sprite = want;
        }
        float fadeIn = Mathf.Min(1f, age / Mathf.Max(0.001f, life * 0.08f));
        float fadeOut = k <= hold ? 1f : 1f - (k - hold) / Mathf.Max(0.001f, 1f - hold);
        sr.color = new Color(col.r, col.g, col.b, col.a * fadeIn * fadeOut);
        if (k >= 1f || (target == null && !helix && age > life)) Finish();
    }
}
