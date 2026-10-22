using UnityEngine;

// 평타 투사체를 잘 보이게 (1.1.2~ AttackLook): 밝은 흰 심 + 영웅색 테두리 + 어두운 외곽선 · 가산 혼합 빛 무리 · 짧은 잔상
//   예광탄 모드: 거너 총알 프리팹(가로 3.3 · 세로 -0.02 로 납작한 흰 실선)은 실선을 숨기고 금빛 예광탄 그림을 자식으로 (판정은 총알 그대로)
//   몸 모드:     평타 그림(atk_* · 편지 · 플라스크)을 입힌 투사체는 그 몸에 영웅색 · 흰 심 층 · 빛 무리 · 잔상
//   예전 모드:   그 밖의 스킬 투사체(SignatureSkills 등 그림만 바꾼 것)는 예전 모습 그대로 (작은 빛 무리 · 흰 점 · 점 꼬리)
// 모두 처음 LateUpdate 에 꾸밈 — 쏜 쪽이 색 · 크기 · 그림을 다 정한 뒤 (스킨 색 SkinTint 도 Start 에서 먼저 입혀짐)
// 파티클은 쓰지 않음: 자식 스프라이트 2~3개 + 잔상은 FadeSprite 를 다시 씀
public class BulletGlow : MonoBehaviour
{
    // ---- 쏜 쪽이 정함
    [System.NonSerialized] public Sprite tracerSprite;      // 이 그림이면 예광탄 모드 (총알 프리팹의 실선)
    [System.NonSerialized] public Vector3 tracerBaseScale;  // 프리팹 크기 (특수 총기가 키운 배율을 알려고)
    [System.NonSerialized] public Sprite[] frames;          // 몸 모드: 몸 프레임 (심 프레임과 같은 배치)
    [System.NonSerialized] public Sprite[] coreFrames;      // 몸 모드: 흰 심 층 (없으면 심 없음)
    [System.NonSerialized] public bool styled;              // 몸 모드로 꾸밈 (평타 그림)
    [System.NonSerialized] public bool keepColor;           // 색까지 그린 그림(편지): 몸 색은 그대로, 빛 · 잔상만 영웅색
    [System.NonSerialized] public bool trail = true;
    [System.NonSerialized] public float haloMul = 1.5f;     // 빛 무리 지름 = 몸의 긴 쪽 × 이 값

    SpriteRenderer body, coreSr, tracerSr;
    SpriteRenderer lookSr, outlineSr;       // 1.1.9: 몸 모드의 보이는 그림 자식 (LookMul 배) · 어두운 실루엣 테두리
    Transform halo;
    Sprite[] tracerFrames, tracerCore;
    int tier = 1, lastFrame = -1;
    bool ready, legacy;
    Color tint;
    float alpha = 1f, trailT, haloW, haloH, age;

    void Awake() => tier = AttackLook.Tier;       // 쏜 순간의 단계 (평타 · 스킬)

    void LateUpdate()
    {
        if (!ready) { Init(); if (!enabled) return; }
        age += Time.deltaTime;
        if (legacy) { LegacyUpdate(); return; }

        // 프레임: 예광탄은 직접 넘기고, 몸 모드는 FrameLoop · FxAnim 이 바꾼 몸 그림에 심 층을 맞춤
        if (tracerSr != null && tracerFrames.Length > 1)
        {
            int i = (int)(age * 18f) % tracerFrames.Length;
            if (i != lastFrame)
            {
                lastFrame = i;
                tracerSr.sprite = tracerFrames[i];
                if (outlineSr != null) outlineSr.sprite = tracerFrames[i];
                if (coreSr != null && i < tracerCore.Length) coreSr.sprite = tracerCore[i];
            }
        }
        else
        {
            if (lookSr != null) SyncLook();
            if (coreSr != null && frames != null)
            {
                int i = FrameOf(body.sprite);
                if (i >= 0 && i != lastFrame && i < coreFrames.Length) { lastFrame = i; coreSr.sprite = coreFrames[i]; }
            }
        }

        // 빛 무리: 두근거림 (번쩍임 효과를 끄면 그대로) — 값이 같으면 쓰지 않음 (최적화 9)
        if (halo != null)
        {
            float pulse = GameSettings.Flashes ? 1f + 0.1f * Mathf.Sin(age * 26f) : 1f;
            Vector3 ps = transform.lossyScale;
            Vector3 want = new Vector3(haloW * pulse / 8f / Safe(ps.x), haloH * pulse / 8f / Safe(ps.y), 1f);      // 빛 그림은 8칸 크기
            if (want != haloShown) { haloShown = want; halo.localScale = want; }
        }

        // 잔상: 몸 그림 그대로 옅어지며 (0.035초마다, 다시 쓰는 FadeSprite)
        if (!trail) return;
        trailT += Time.deltaTime;
        if (trailT < 0.035f) return;
        trailT = 0f;
        SpriteRenderer src = tracerSr != null ? tracerSr : lookSr != null ? lookSr : body;
        if (src == null || src.sprite == null) return;
        if (Time.frameCount != initFrame && !src.isVisible) return;      // 최적화 9: 화면 밖 탄은 잔상을 만들지 않음 (보이지 않으니 겉모습 같음, 막 꾸민 프레임은 아직 그려지기 전이라 제외)
        FadeSprite f = FadeSprite.Spawn("AtkTrail", src.sprite, src.transform.position, 1f,
                                        new Color(tint.r, tint.g, tint.b, AttackLook.TrailAlpha(tier) * alpha), "Effect", AttackLook.TrailOrder, AttackLook.TrailLife(tier));
        Vector3 s = src.transform.lossyScale;
        f.Shape(src.transform.rotation, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), 1f));
    }

    static float Safe(float v) => Mathf.Abs(v) < 0.0001f ? 1f : v;

    // 몸 그림이 frames 의 몇 번째인지: 지난 칸부터 차례로 (FrameLoop 이 순서대로 넘기므로 한두 번에 찾음, Array.IndexOf 대신)
    int FrameOf(Sprite s)
    {
        int n = frames.Length;
        if (n == 0) return -1;
        int start = lastFrame >= 0 && lastFrame < n ? lastFrame : 0;
        for (int k = 0; k < n; k++)
        {
            int i = (start + k) % n;
            if (frames[i] == s) return i;
        }
        return -1;
    }

    Vector3 haloShown = new Vector3(float.NaN, 0f, 0f);

    // ================================================================= 다시 쓰기 (최적화 8: Bullet 풀링)
    // 탄이 풀로 돌아갈 때 (Recycle): 꾸밈 자식은 지우지 않고 꺼 둠 · 예전 모드 빛도 꺼 둠
    // 다시 꺼낼 때 (Rearm): 쏜 쪽이 정하는 값 · 상태를 새로 붙인 것처럼 되돌림 — 다음 LateUpdate 의 Init 이 꺼 둔 자식을 다시 켜서 씀
    static Material defaultMat;
    readonly System.Collections.Generic.List<SpriteRenderer> made = new System.Collections.Generic.List<SpriteRenderer>(6);
    readonly System.Collections.Generic.List<string> madeName = new System.Collections.Generic.List<string>(6);
    readonly System.Collections.Generic.List<bool> madeBusy = new System.Collections.Generic.List<bool>(6);

    public bool Owns(Transform t)
    {
        for (int i = 0; i < made.Count; i++) if (made[i] != null && made[i].transform == t) return true;
        return false;
    }

    public void Recycle()
    {
        for (int i = 0; i < made.Count; i++)
        {
            madeBusy[i] = false;
            SpriteRenderer sr = made[i];
            if (sr == null) continue;
            Transform t = sr.transform;
            if (t.parent != transform) t.SetParent(transform, false);
            if (sr.gameObject.activeSelf) sr.gameObject.SetActive(false);
        }
        if (lHalo != null) lHalo.SetActive(false);
        if (lCore != null) lCore.SetActive(false);
    }

    public void Rearm()
    {
        tracerSprite = null;
        tracerBaseScale = Vector3.zero;
        frames = null;
        coreFrames = null;
        styled = false;
        keepColor = false;
        trail = true;
        haloMul = 1.5f;
        body = coreSr = tracerSr = lookSr = outlineSr = null;
        halo = null;
        tracerFrames = tracerCore = null;
        tier = AttackLook.Tier;          // 쏜 순간의 단계 (Awake 와 같게)
        lastFrame = -1;
        ready = legacy = fitted = false;
        tint = default;
        alpha = 1f;
        trailT = haloW = haloH = age = 0f;
        lColor = default;
        haloShown = new Vector3(float.NaN, 0f, 0f);
        enabled = true;
    }

    int initFrame = -1;

    void Init()
    {
        ready = true;
        initFrame = Time.frameCount;
        if (TryGetComponent(out SkinTint skin)) skin.ApplyNow();       // 무기 스킨 색을 먼저 (다시 쓰는 탄은 SkinTint.Start 가 늦을 수 있음)
        body = GetComponent<SpriteRenderer>();
        // 그림을 꺼 둔 총알(화염 판정탄, 낫)은 꾸미지 않음
        if (body == null || !body.enabled || SpecialAbilities.GlowSprite == null) { enabled = false; return; }
        Color c = body.color;
        alpha = c.a * GameSettings.MyFxAlpha;          // 설정의 「내 이펙트 투명도」
        tint = AttackLook.Vivid(c, AttackLook.HeroColor());

        bool tracer = tracerSprite != null && body.sprite == tracerSprite;
        if (!tracer && !styled) { LegacyInit(); return; }

        if (tracer) InitTracer();
        else InitBody();
        MakeHalo();
    }

    // 예광탄 보이는 높이: 단계 높이 × 특수 총기가 키운 배율 (저격 1.4 등)
    float TracerHeight(Vector3 ps)
    {
        float rel = tracerBaseScale.x != 0f ? Mathf.Abs(ps.x / tracerBaseScale.x) : 1f;
        return AttackLook.GunnerHeight(tier) * Mathf.Max(0.5f, rel);
    }

    // 예광탄으로 그릴 총알의 판정을 보이는 예광탄 몸통에 맞춤 (프리팹 실선 3.27 × 0.022 → 길이 0.92 × 2.4 × 높이 · 두께 0.8 × 높이)
    //   Bullet 이 첫 스윕 검사 전에 부름 (꾸미기는 LateUpdate 라 그보다 늦음) · 한 번만
    //   그림을 바꾼 탄 · 그림을 꺼 둔 탄 · 예광탄 그림이 없으면 그대로 (보이는 것이 실선 · 그 그림이라)
    bool fitted;
    public void FitTracerHit()
    {
        if (fitted) return;
        fitted = true;
        if (tracerSprite == null || SpecialAbilities.GlowSprite == null) return;
        if (!TryGetComponent(out SpriteRenderer sr) || sr.sprite != tracerSprite || (!sr.enabled && !ready)) return;
        Sprite[] f = Fx.Frames("atk_bullet");
        if (f == null || f.Length == 0) return;
        if (!TryGetComponent(out BoxCollider2D box)) return;
        Vector3 ps = transform.lossyScale;
        if (Mathf.Abs(ps.x) < 0.0001f || Mathf.Abs(ps.y) < 0.0001f) return;
        Vector2 hit = AttackLook.BodySize(f[0], "atk_bullet", TracerHeight(ps));
        box.size = new Vector2(hit.x / Mathf.Abs(ps.x), hit.y / Mathf.Abs(ps.y));
        box.offset = Vector2.zero;
        if (TryGetComponent(out Bullet b)) b.RefreshSweepShape();
    }

    // 예광탄: 실선은 숨기고 금빛 예광탄 + 흰 심을 자식으로 (프리팹이 납작해서 축마다 되돌림)
    void InitTracer()
    {
        tracerFrames = Fx.Frames("atk_bullet");
        tracerCore = Fx.Frames("atk_bullet_core");
        if (tracerFrames == null || tracerFrames.Length == 0) { LegacyInit(); return; }
        body.enabled = false;
        FitTracerHit();
        Vector3 ps = transform.lossyScale;
        float h = TracerHeight(ps);
        // 1.1.9: 그림만 LookMul 배 (판정은 FitTracerHit 가 맞춘 높이 h 그대로 — 이 예광탄은 자식이라 총알 상자에 영향 없음)
        float k = h / tracerFrames[0].bounds.size.y * AttackLook.LookMul;
        tracerSr = Child("AtkTracer", tracerFrames[0], new Color(tint.r, tint.g, tint.b, alpha * 0.97f), AttackLook.Order, new Vector3(k / Safe(ps.x), k / Safe(Mathf.Abs(ps.y)), 1f));
        float len = tracerFrames[0].bounds.size.x * k;
        outlineSr = Outline(tracerSr, len, tracerFrames[0].bounds.size.y * k, alpha);
        if (tracerCore != null && tracerCore.Length > 0)
        {
            coreSr = Child("AtkCore", tracerCore[0], new Color(1f, 1f, 1f, alpha), AttackLook.CoreOrder, Vector3.one);
            coreSr.transform.SetParent(tracerSr.transform, false);
        }
        haloW = len * 1.25f;
        haloH = h * AttackLook.LookMul * 2.4f;
    }

    // 1.1.9: 같은 그림을 어두운 색으로 조금 크게 뒤에 깔아 실루엣 테두리 (밝은 모래 · 초원에서도 윤곽이 또렷하게)
    //   기본 스프라이트 재질 그대로 (탄마다 새 재질 없음 · 배칭 유지), 두께는 월드 기준으로 가로 · 세로 같게
    //   그래픽 개편 1단계-9 (2026-10-22~): 어두운 테두리 + 흰 심은 적 탄 문법이라 내 투사체에선 뺌 (바닥 톤다운 1단계-7 로 사막 모래가 어두워져 대비는 충분)
    SpriteRenderer Outline(SpriteRenderer of, float w, float h, float a)
    {
        if (!VisualRules.Legacy) return null;
        if (of == null || of.sprite == null || w <= 0.0001f || h <= 0.0001f) return null;
        float t = AttackLook.OutlineWidth(w, h);
        SpriteRenderer o = Child("AtkOutline", of.sprite, Ink(a), AttackLook.OutlineOrder, Vector3.one);
        o.transform.SetParent(of.transform, false);
        o.transform.localScale = new Vector3(1f + 2f * t / w, 1f + 2f * t / h, 1f);
        o.flipX = of.flipX;
        o.flipY = of.flipY;
        return o;
    }

    static Color Ink(float a)
    {
        Color c = AttackLook.OutlineInk;
        c.a *= a;
        return c;
    }

    // 몸 모드: 꺼 둔 몸 그림을 보이는 자식이 따라감 (FrameLoop · FxAnim 이 바꾸는 그림, 끝날 때 흐려지는 색, 뒤집기)
    void SyncLook()
    {
        Sprite s = body.sprite;
        if (lookSr.sprite != s)
        {
            lookSr.sprite = s;
            if (outlineSr != null) outlineSr.sprite = s;
        }
        Color c = body.color;
        if (lookSr.color != c)
        {
            lookSr.color = c;
            if (outlineSr != null) outlineSr.color = Ink(c.a);
        }
        if (lookSr.flipX != body.flipX || lookSr.flipY != body.flipY)
        {
            lookSr.flipX = body.flipX;
            lookSr.flipY = body.flipY;
            if (outlineSr != null) { outlineSr.flipX = body.flipX; outlineSr.flipY = body.flipY; }
            if (coreSr != null) { coreSr.flipX = body.flipX; coreSr.flipY = body.flipY; }
        }
    }

    // 몸 모드: 몸에 영웅색, 흰 심 층을 같은 크기로 겹침
    void InitBody()
    {
        body.sortingLayerID = AttackLook.Layer;
        body.sortingOrder = AttackLook.Order;
        if (!keepColor) body.color = new Color(tint.r, tint.g, tint.b, alpha * 0.95f);
        else tint = AttackLook.HeroColor();        // 몸 색 · 투명도는 쏜 쪽(Fx.Play 가 이미 「내 이펙트 투명도」를 곱함) 그대로
        // 1.1.9: 보이는 그림은 LookMul 배 자식으로 (판정 상자 · 원은 이 오브젝트 크기 그대로라 그림만 커짐), 몸 그림은 끄고 SyncLook 이 따라감
        Vector3 ps = transform.lossyScale;
        Vector2 size = body.sprite != null ? (Vector2)body.sprite.bounds.size : Vector2.one;
        float mul = body.sprite != null ? AttackLook.LookMul : 1f;
        float w = size.x * Mathf.Abs(ps.x) * mul, h = size.y * Mathf.Abs(ps.y) * mul;
        Transform coreParent = transform;
        if (body.sprite != null)
        {
            lookSr = Child("AtkBody", body.sprite, body.color, AttackLook.Order, new Vector3(mul, mul, 1f));
            lookSr.sharedMaterial = body.sharedMaterial;
            lookSr.flipX = body.flipX;
            lookSr.flipY = body.flipY;
            body.enabled = false;
            outlineSr = Outline(lookSr, w, h, body.color.a);
            coreParent = lookSr.transform;
        }
        if (coreFrames != null && coreFrames.Length > 0)
        {
            int i = frames != null ? Mathf.Max(0, System.Array.IndexOf(frames, body.sprite)) : 0;
            coreSr = Child("AtkCore", coreFrames[Mathf.Min(i, coreFrames.Length - 1)], new Color(1f, 1f, 1f, alpha), AttackLook.CoreOrder, Vector3.one);
            coreSr.transform.SetParent(coreParent, false);
            coreSr.flipX = body.flipX;
            coreSr.flipY = body.flipY;
        }
        haloW = haloH = Mathf.Max(w, h) * haloMul;
    }

    // 가산 혼합 빛 무리 (셰이더가 없으면 반투명으로)
    void MakeHalo()
    {
        Material add = AttackLook.Additive;
        float a = AttackLook.HaloAlpha(tier) * alpha * (add != null ? 1f : 0.6f) * (GameSettings.Flashes ? 1f : 0.75f);
        SpriteRenderer h = Child("AtkHalo", SpecialAbilities.GlowSprite, new Color(tint.r, tint.g, tint.b, a), AttackLook.HaloOrder, Vector3.one);
        if (add != null) h.sharedMaterial = add;
        halo = h.transform;
        halo.localRotation = Quaternion.identity;
    }

    SpriteRenderer Child(string name, Sprite s, Color c, int order, Vector3 localScale)
    {
        // 꺼 둔 같은 이름의 자식이 있으면 다시 씀 (새로 만든 것과 같은 상태로)
        SpriteRenderer sr = null;
        for (int i = 0; i < made.Count; i++)
        {
            if (madeBusy[i] || made[i] == null || madeName[i] != name) continue;
            sr = made[i];
            madeBusy[i] = true;
            break;
        }
        GameObject g;
        if (sr != null)
        {
            g = sr.gameObject;
            g.transform.SetParent(transform, false);
            if (defaultMat != null) sr.sharedMaterial = defaultMat;
            sr.flipX = sr.flipY = false;
            g.SetActive(true);
        }
        else
        {
            g = new GameObject(name);
            g.transform.SetParent(transform, false);
            sr = g.AddComponent<SpriteRenderer>();
            if (defaultMat == null) defaultMat = sr.sharedMaterial;
            made.Add(sr);
            madeName.Add(name);
            madeBusy.Add(true);
        }
        g.transform.localPosition = Vector3.zero;
        g.transform.localRotation = Quaternion.identity;
        g.transform.localScale = localScale;
        sr.sprite = s;
        sr.color = c;
        sr.sortingLayerID = AttackLook.Layer;
        sr.sortingOrder = order;
        return sr;
    }

    // ================================================================= 예전 모습 (스킬 투사체)
    const int LegacyOrder = 60;
    const float LegacyHalo = 0.22f;
    GameObject lHalo, lCore;
    Color lColor;

    void LegacyInit()
    {
        legacy = true;
        Sprite glow = SpecialAbilities.GlowSprite;
        lColor = body.color;
        body.sortingLayerName = "Effect";
        body.sortingOrder = LegacyOrder + 1;
        lHalo = LegacySprite(lHalo, "BulletHalo", glow, LegacyHalo, new Color(lColor.r, lColor.g, lColor.b, 0.7f), LegacyOrder);
        lCore = LegacySprite(lCore, "BulletCore", glow, 0.085f, new Color(1f, 1f, 1f, 1f), LegacyOrder + 2);
    }

    // 다시 쓰는 탄은 꺼 둔 빛을 다시 켬 (MakeSprite 와 같은 상태로)
    GameObject LegacySprite(GameObject go, string name, Sprite glow, float scale, Color c, int order)
    {
        if (go == null || !go.TryGetComponent(out SpriteRenderer sr))
            return SpecialAbilities.MakeSprite(name, glow, transform.position, scale, c, "Effect", order);
        Transform t = go.transform;
        t.SetPositionAndRotation(transform.position, Quaternion.identity);
        t.localScale = Vector3.one * scale;
        sr.sprite = glow;
        sr.color = c;
        sr.sortingLayerName = "Effect";
        sr.sortingOrder = order;
        go.SetActive(true);
        return go;
    }

    void LegacyUpdate()
    {
        if (lHalo == null) return;
        float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 30f);
        lHalo.transform.position = transform.position;
        lHalo.transform.localScale = Vector3.one * LegacyHalo * pulse;
        lCore.transform.position = transform.position;
        trailT += Time.deltaTime;
        if (trailT >= 0.025f)
        {
            trailT = 0f;
            if (Time.frameCount != initFrame && !body.isVisible) return;     // 화면 밖은 꼬리 생략 (최적화 9)
            FadeSprite.Spawn("BulletTrail", SpecialAbilities.GlowSprite, transform.position, 0.09f,
                             new Color(lColor.r, lColor.g, lColor.b, 0.55f), "Effect", LegacyOrder - 1, 0.18f);
        }
    }

    void OnDestroy()
    {
        if (lHalo != null) Destroy(lHalo);
        if (lCore != null) Destroy(lCore);
    }
}
