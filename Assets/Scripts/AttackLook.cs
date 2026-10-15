using UnityEngine;

// 1.1.2: 평타를 한눈에 — 「여전히 평타가 안 보인다」
//   예전: 거너 총알은 납작한 흰 실선(3.27 × 0.02) + 작은 흰 빛, 표창 · 화살 · 톱니는 1칸 남짓한 옅은 흰 그림이라
//         밝은 초원(흰색 명도 대비 3.3)에서 묻히고, 흰 · 하늘색 적 탄과도 색이 겹쳤음
//   지금: 보이는 크기 3~4배(판정과 따로) · 밝은 흰 심 + 영웅 고유색 테두리 + 어두운 외곽선 3단 · 가산 혼합 빛 무리 · 짧은 잔상
//         강해질수록(Fx.TierOf: 기본 → 1차 → 2차 진화) 더 크고 밝게
// 그림: tools/pixelart/make_attacks.py (atk_* · atk_*_core · fx_swordswing*)
public static class AttackLook
{
    // ---------------------------------------------------------------- 정렬 (Effect 층)
    // 투사체(총알 · 표창 · 화살 · 톱니 · 플라스크 · 편지 · 검기): 맵 물체(5~8) · 적 이펙트(12~22) · 피해 숫자(40~45) · 내 스킬 이펙트(52~62) 위
    //   (예전 총알 61 과 같은 자리, 빛 무리는 가산 혼합이라 밑을 가리지 않음)
    // 판 위 범위 공격(검사 베기 · 지도 획 · 유령 손길 · 플라스크 폭발): 크게 덮으니 적 이펙트 위 · 피해 숫자 · 스킬 이펙트 아래 (AreaOrder)
    // 적 탄(구슬 · 심 · 그림자)은 그보다 위 층(HitScan)이라, 평타가 아무리 커져도 적 탄을 가리지 않음
    public const int HaloOrder = 60, TrailOrder = 61, Order = 63, CoreOrder = 64;
    public const int AreaOrder = 32;
    static int layer = -1;
    public static int Layer => layer >= 0 ? layer : (layer = SortingLayer.NameToID("Effect"));

    // ---------------------------------------------------------------- 단계 · 크기
    // 지금 낸 공격의 단계 1 · 2 · 3 (평타는 무기 진화 단계를 따름)
    public static int Tier
    {
        get
        {
            string s = DamageSource.Current;
            return Mathf.Clamp(Fx.TierOf(string.IsNullOrEmpty(s) ? DamageSource.Basic : s), 1, 3);
        }
    }

    // 보이는 크기 배율 (예전 보이는 크기 대비, 판정과 따로)
    //   작은 투사체(총알 · 표창 · 화살 · 톱니 · 뼈 손 등): 3 → 3.5 → 4배
    //   원래 큰 것(정각 톱니 · 편지 · 플라스크): 2.4 → 2.7 → 3배
    //   검기(이미 3.4칸): 2 → 2.3 → 2.6배 (화면을 덮지 않게)
    // 1.1.2 빌드 뒤: 「평타 크기가 너무 크다, 지금의 30% 정도로」 → 모든 배율 × 0.3 (작은 것 0.9 → 1.2배, 큰 것 0.72 → 0.9배, 검기 0.6 → 0.78배)
    //   색 대비(흰 심 · 영웅색 테두리 · 외곽선) · 빛 무리 · 잔상 · 정렬은 그대로라 예전보다 작아도 또렷함
    public const float SizeScale = 0.3f;
    public static float SmallMul(int tier) => (tier >= 3 ? 4f : tier == 2 ? 3.5f : 3f) * SizeScale;
    public static float BigMul(int tier) => (tier >= 3 ? 3f : tier == 2 ? 2.7f : 2.4f) * SizeScale;
    public static float WaveMul(int tier) => (tier >= 3 ? 2.6f : tier == 2 ? 2.3f : 2f) * SizeScale;
    // 판 위 범위 공격(베기 · 획 · 손길)은 판정 범위 = 그림이라 바깥 크기는 그대로, 띠 굵기만 단계마다
    public static float BandMul(int tier) => tier >= 3 ? 1.2f : tier == 2 ? 1.1f : 1f;

    // 판정: 그림이 3~4배 커져 「맞은 것 같은데 안 맞는」 어긋남이 커서 투사체 판정 상자만 1.3배 (피해는 그대로)
    public const float HitMul = 1.3f;
    // 거너 총알 판정 두께: 0.022 → 0.3 (길이 3.27 은 그대로)
    public const float GunnerHitThick = 0.3f;

    // 거너 예광탄 보이는 높이 (예전 흰 심 지름 ≈ 0.3 → 0.85 · 1.0 · 1.15, 길이는 그 2.4배)
    public static float GunnerHeight(int tier) => (tier >= 3 ? 1.15f : tier == 2 ? 1f : 0.85f) * SizeScale;

    // 빛 무리 진하기 · 잔상 (단계마다 더 밝고 길게)
    public static float HaloAlpha(int tier) => tier >= 3 ? 0.6f : tier == 2 ? 0.5f : 0.42f;
    public static float TrailAlpha(int tier) => tier >= 3 ? 0.55f : tier == 2 ? 0.48f : 0.4f;
    public static float TrailLife(int tier) => tier >= 3 ? 0.16f : tier == 2 ? 0.13f : 0.1f;

    // ---------------------------------------------------------------- 색
    // 영웅 고유색 (테두리 · 빛 무리 · 잔상): 붉은 적 위험색(1, 0.25, 0.2)과 겹치지 않게, 명도는 모두 높게
    public static readonly Color Gold = new Color(1f, 0.8f, 0.22f);         // 거너
    public static readonly Color Azure = new Color(0.4f, 0.78f, 1f);        // 검사
    public static readonly Color Magenta = new Color(1f, 0.36f, 0.86f);     // 도적
    public static readonly Color Mint = new Color(0.5f, 1f, 0.66f);         // 궁수
    public static readonly Color Brass = new Color(1f, 0.72f, 0.28f);       // 시계공
    public static readonly Color InkBlue = new Color(0.4f, 0.6f, 1f);       // 지도 제작자
    public static readonly Color PostGold = new Color(1f, 0.78f, 0.35f);    // 우체부
    public static readonly Color Frost = new Color(0.55f, 0.92f, 1f);       // 유령

    public static Color HeroColor()
    {
        CharacterKit k = CharacterKit.Instance;
        if (k == null) return Gold;
        switch (k.Id)
        {
            case CharacterId.Swordsman: return Azure;
            case CharacterId.Rogue: return Magenta;
            case CharacterId.Archer: return Mint;
            case CharacterId.Alchemist: return new Color(1f, 0.55f, 0.25f);
            case CharacterId.Cartographer: return InkBlue;
            case CharacterId.Clockmaker: return Brass;
            case CharacterId.Postman: return PostGold;
            case CharacterId.Ghost: return Frost;
            default: return Gold;
        }
    }

    // 코드가 입힌 색(형태 · 시약 · 스킨 · 완벽 사격 등)을 또렷하게: 색상은 그대로 채도 · 밝기만 올림
    // 거의 흰색(입힌 색이 없음)이면 영웅색으로
    public static Color Vivid(Color c, Color fallback)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        Color o = s < 0.12f ? fallback : Color.HSVToRGB(h, Mathf.Max(s, 0.6f), 1f);
        o.a = c.a;
        return o;
    }

    // ---------------------------------------------------------------- 모양
    // 투사체 보이는 높이 · 판정 상자(월드 크기)를 따로: 몸을 보이는 크기로 늘리고 상자는 그만큼 줄여 판정을 맞춤
    public static void Shape(Bullet b, Sprite s, float visibleHeight, Vector2 hitWorld)
    {
        if (b == null || s == null) return;
        float h = s.bounds.size.y;
        if (h <= 0f || visibleHeight <= 0f) return;
        float k = visibleHeight / h;
        b.transform.localScale = new Vector3(k, k, 1f);
        if (b.TryGetComponent(out BoxCollider2D box)) { box.size = hitWorld / k; box.offset = Vector2.zero; }
        else if (b.TryGetComponent(out CircleCollider2D circle)) circle.radius = Mathf.Max(hitWorld.x, hitWorld.y) * 0.5f / k;
    }

    // 평타 그림 이름 (예전 그림 이름 → 새 그림), 없으면 null (예전 그림을 키우기만)
    public static string LookFor(string fx, bool clockmaker) => fx switch
    {
        "fx_shuriken" => clockmaker ? "atk_gear" : "atk_shuriken",
        "fx_arrow" => "atk_arrow",
        _ => null,
    };

    // ---------------------------------------------------------------- 가산 혼합 (빛 무리)
    static Material additive;
    static bool additiveFailed;
    public static Material Additive
    {
        get
        {
            if (additive != null || additiveFailed) return additive;
            Shader s = Resources.Load<Shader>("Shaders/SpriteAdditive");
            if (s == null || !s.isSupported) { additiveFailed = true; return null; }
            additive = new Material(s) { name = "AttackGlow" };
            return additive;
        }
    }
}
