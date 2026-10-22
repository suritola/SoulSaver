using UnityEngine;

// 도트를 더 잘 보이게 (1.8.7~): 캐릭터 · 적 · 보스 스프라이트에 1도트 윤곽선, 이펙트는 윤곽선 + 채도 · 밝기 (Resources/Shaders/SpriteOutline.shader)
// 적 · 보스는 짙은 윤곽선으로 배경과 분리, 플레이어는 밝은 윤곽선으로 적 무리 속에서도 바로 보이게
public static class SpriteOutline
{
    static readonly Color Dark = new Color(0.06f, 0.04f, 0.09f, 0.9f);
    static readonly Color Light = new Color(1f, 0.95f, 0.8f, 0.85f);
    static Material dark, light;
    static bool failed;

    public static void Enemy(GameObject go) => Apply(go, ref dark, Dark);

    // 이펙트용 (Fx): 얇고 짙은 윤곽선 + 채도 1.3배 · 밝기 1.08배 (1.8.7~)
    static Material effect;
    public static Material EffectMaterial
    {
        get
        {
            if (effect != null || failed) return effect;
            Shader s = Resources.Load<Shader>("Shaders/SpriteOutline");
            if (s == null || !s.isSupported) { failed = true; return null; }
            effect = new Material(s) { name = "EffectOutline" };
            effect.SetColor("_OutlineColor", new Color(0.05f, 0.03f, 0.08f, 0.7f));
            // 그래픽 개편 1단계-8 (2026-10-22~): 채도 1.3 · 밝기 1.08 과장을 뺌 (VisualRules.Legacy 면 예전 값)
            effect.SetFloat("_Saturation", VisualRules.Legacy ? 1.3f : 1f);
            effect.SetFloat("_Brightness", VisualRules.Legacy ? 1.08f : 1f);
            effect.SetFloat("_EmptyAlpha", 0.05f);
            return effect;
        }
    }

    // 1단계-8~ 내 이펙트용: 짙은 윤곽선 없음 (짙은 테 · 흰 심은 적 공격 문법이라 피아가 섞였음) · 채도 · 밝기 그대로
    static Material myEffect;
    public static Material MyEffectMaterial
    {
        get
        {
            if (myEffect != null || failed) return myEffect;
            Shader s = Resources.Load<Shader>("Shaders/SpriteOutline");
            if (s == null || !s.isSupported) { failed = true; return null; }
            myEffect = new Material(s) { name = "MyEffect" };
            myEffect.SetColor("_OutlineColor", new Color(0f, 0f, 0f, 0f));
            myEffect.SetFloat("_Saturation", 1f);
            myEffect.SetFloat("_Brightness", 1f);
            myEffect.SetFloat("_EmptyAlpha", 0.05f);
            return myEffect;
        }
    }
    public static void Player(GameObject go) => Apply(go, ref light, Light);

    static void Apply(GameObject go, ref Material mat, Color color)
    {
        if (go == null || failed) return;
        if (mat == null)
        {
            Shader s = Resources.Load<Shader>("Shaders/SpriteOutline");
            if (s == null || !s.isSupported) { failed = true; return; }
            mat = new Material(s) { name = "SpriteOutline" };
            mat.SetColor("_OutlineColor", color);
        }
        if (effectLayer == int.MinValue)
        {
            effectLayer = SortingLayer.NameToID("Effect");
            markLayer = SortingLayer.NameToID("TargetMark");
        }
        // 기본 스프라이트 재질을 쓰는 몸 그림에만 (이펙트 · 글자 · 체력바는 그대로)
        // 자식이 없으면 배열을 만들지 않고 자기 렌더러 하나만 (스폰마다 할당 줄임)
        if (go.transform.childCount == 0)
        {
            Tint(go.GetComponent<SpriteRenderer>(), mat);
            return;
        }
        go.GetComponentsInChildren(true, renderers);
        for (int i = 0; i < renderers.Count; i++) Tint(renderers[i], mat);
        renderers.Clear();
    }

    static readonly System.Collections.Generic.List<SpriteRenderer> renderers = new System.Collections.Generic.List<SpriteRenderer>(16);
    static int effectLayer = int.MinValue, markLayer;
    static Shader spriteDefault;

    static void Tint(SpriteRenderer sr, Material mat)
    {
        if (sr == null) return;
        Material m = sr.sharedMaterial;
        if (m == null) return;
        Shader sh = m.shader;
        if (sh == null) return;
        if (spriteDefault != null) { if (sh != spriteDefault) return; }
        else if (sh.name == "Sprites/Default") spriteDefault = sh;
        else return;
        int layer = sr.sortingLayerID;
        if (layer == effectLayer || layer == markLayer) return;
        sr.sharedMaterial = mat;
    }
}
