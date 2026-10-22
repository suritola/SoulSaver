using UnityEngine;

// 1.3.3~ 불 장판 고품질 연출 (화염 지대 · 유탄 불 · 지옥의 군주 불 웅덩이 공용)
//   예전: 둥근 빛 덩어리 / 주황으로 물들인 산성 웅덩이 그림 → 「그을린 땅 + 빛나는 금(2프레임 깜빡임) + 넘실대는 불꽃 혀(6프레임) + 열기 빛 + 불티」
//   그림: tools/pixelart/make_firefloor.py (fx_firefloor · fx_flametongue)
//   붙는 곳의 자식 SpriteRenderer 만 씀 (스크립트를 복제하지 않음 · Instantiate 없음) · 장판이 사라지면 같이 사라짐 · 판정은 붙는 쪽(DamageZone · HazardZone) 그대로
public class FireGround : MonoBehaviour
{
    float radius, life, age;
    SpriteRenderer ground, heat;
    SpriteRenderer[] flames;
    float[] phase, baseScale;
    Sprite[] floorFrames, flameFrames;
    float emberAt;
    // 그래픽 개편 1단계-4 (2026-10-22~): 적 불 장판과 내 불 장판을 가름 (예전엔 같은 그림이라 밟아도 되는 불인지 알 수 없었음)
    //   적(hostile): 가장자리에 경고색 테 (Warning 층 · 맥동) / 내 것: 옅게 (알파 ×0.6) · VisualRules.Legacy 면 예전 그대로
    float alphaMul = 1f;
    SpriteRenderer rim;
    static readonly Color RimColor = new Color(1f, 0.25f, 0.2f);

    public static FireGround Attach(GameObject host, float radius, float life, int sortingBase = 6, bool hostile = false)
    {
        if (host == null) return null;
        FireGround g = host.AddComponent<FireGround>();
        if (!VisualRules.Legacy && !hostile) g.alphaMul = 0.6f;
        g.Build(radius, life, sortingBase);
        if (!VisualRules.Legacy && hostile) g.BuildRim(radius);
        return g;
    }

    // 적 장판 가장자리 경고 테 (판정 반지름 = 테 가운데 · PixelRingArt 단정한 고리)
    void BuildRim(float r)
    {
        Sprite ring = PixelRingArt.Get(r, -1f, true);
        if (ring == null) return;
        rim = Part("FireRim", ring, SortingLayer.NameToID(VisualRules.WarnLayer), 0);
        if (rim == null) return;
        float ps = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        rim.transform.localScale = Vector3.one / ps;
    }

    void Build(float r, float l, int order)
    {
        radius = r;
        life = Mathf.Max(0.3f, l);
        floorFrames = Fx.Frames("fx_firefloor");
        flameFrames = Fx.Frames("fx_flametongue");
        int layer = SortingLayer.NameToID("Background");
        // 그을린 땅 (반지름보다 조금 넓게 — 가장자리가 들쭉날쭉해서)
        ground = Part("FireGroundBase", floorFrames != null && floorFrames.Length > 0 ? floorFrames[0] : null, layer, order);
        if (ground != null)
        {
            Fit(ground, r * 2.25f);
            ground.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        }
        // 열기 빛 (그을린 땅 위 · 불꽃 아래)
        heat = Part("FireHeat", SpecialAbilities.GlowSprite, layer, order + 1);
        if (heat != null) Fit(heat, r * 2.6f);
        // 불꽃 혀: 넓이에 맞춰 개수 (반지름 3 → 12개 · 상한 24) · 해바라기 배치로 고르게 · 키 · 속도 · 시작 프레임을 저마다
        int n = Mathf.Clamp(Mathf.RoundToInt(r * r * 1.35f), 4, 24);
        flames = new SpriteRenderer[n];
        phase = new float[n];
        baseScale = new float[n];
        for (int i = 0; i < n; i++)
        {
            float a = i * 2.39996f, d = Mathf.Sqrt((i + 0.5f) / n) * r * 0.82f;
            SpriteRenderer f = Part("Flame", flameFrames != null && flameFrames.Length > 0 ? flameFrames[i % flameFrames.Length] : null, layer, order + 2);
            if (f == null) continue;
            float ps = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));      // 화염 지대는 크기가 바뀐 객체에 붙으므로 월드 거리로 되돌림
            f.transform.localPosition = new Vector3(Mathf.Cos(a) * d, Mathf.Sin(a) * d * 0.85f, 0f) / ps;
            baseScale[i] = Random.Range(0.75f, 1.2f) * Mathf.Lerp(1.15f, 0.8f, d / Mathf.Max(0.01f, r));    // 가운데가 더 큼
            Fit(f, baseScale[i] * 1.25f);
            phase[i] = Random.value * 6f;
            flames[i] = f;
        }
        SetAlpha(0f);
    }

    SpriteRenderer Part(string name, Sprite s, int layer, int order)
    {
        if (s == null) return null;
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingLayerID = layer;
        sr.sortingOrder = order;
        return sr;
    }

    // 부모 크기를 되돌려 월드 가로 size 로
    void Fit(SpriteRenderer sr, float size)
    {
        float w = Mathf.Max(0.0001f, sr.sprite.bounds.size.x);
        float ps = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        sr.transform.localScale = Vector3.one * (size / w / ps);
    }

    void SetAlpha(float a)
    {
        a *= alphaMul;
        if (ground != null) ground.color = new Color(1f, 1f, 1f, a * 0.95f);
        if (flames != null) foreach (SpriteRenderer f in flames) if (f != null) f.color = new Color(1f, 1f, 1f, a);
        if (rim != null) rim.color = new Color(RimColor.r, RimColor.g, RimColor.b, a / Mathf.Max(0.01f, alphaMul));
    }

    void Update()
    {
        age += Time.deltaTime;
        // 0.2초에 피어오르고 마지막 0.5초에 사그라듦
        float fade = Mathf.Clamp01(age / 0.2f) * Mathf.Clamp01((life - age) / 0.5f);
        float a = fade * alphaMul;
        float now = Time.time;
        if (rim != null) rim.color = new Color(RimColor.r, RimColor.g, RimColor.b, fade * (0.75f + 0.25f * Mathf.Sin(now * 8f)));
        if (ground != null && floorFrames != null && floorFrames.Length > 1)
        {
            ground.sprite = floorFrames[(Mathf.FloorToInt(now * 3f + transform.position.x) & 1)];
            ground.color = new Color(1f, 1f, 1f, a * 0.95f);
        }
        if (heat != null) heat.color = new Color(1f, 0.45f, 0.12f, a * (0.28f + 0.1f * Mathf.Sin(now * 7f)));
        if (flames != null && flameFrames != null && flameFrames.Length > 0)
            for (int i = 0; i < flames.Length; i++)
            {
                SpriteRenderer f = flames[i];
                if (f == null) continue;
                float ph = now * (9f + i % 3) + phase[i];
                f.sprite = flameFrames[Mathf.FloorToInt(ph) % flameFrames.Length];
                // 사그라들 때 불꽃이 낮아짐
                float h = baseScale[i] * (0.55f + 0.45f * Mathf.Clamp01((life - age) / 0.8f));
                Fit(f, h * 1.25f);
                f.color = new Color(1f, 1f, 1f, a);
            }
        // 불티: 0.12초마다 하나 (FlameParticle 풀 · 끝날 즈음엔 멈춤)
        if (age < life - 0.4f && now >= emberAt && SpecialAbilities.GlowSprite != null)
        {
            emberAt = now + 0.12f;
            Vector3 at = transform.position + (Vector3)(Random.insideUnitCircle * radius * 0.8f);
            FlameParticle.SpawnEmber(SpecialAbilities.GlowSprite, at, new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(3f, 6f)));
        }
    }
}
