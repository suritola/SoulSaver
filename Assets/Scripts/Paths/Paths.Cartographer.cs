using System.Collections.Generic;
using UnityEngine;

// 지도 제작자 갈래 5개 (기획서 3-6)
// 지도 제작자 담당이 고치는 곳: 이 파일 · Paths/LocTable.Paths.Cartographer.cs · 기존 파일(CharacterKit.Cartographer.cs · SignatureSkills.Cartographer.cs)
//   — 공용 파일(LevelShop*.cs · StageManager · RunSave · HUD · Paths.cs)은 고치지 않음
// 안내: docs/review/2026-10-19-빌드-시너지-영웅구현안내.md
//
// 효과가 어디서 나나
//   평타 모양 (도형 각 수 · 주기 · 넓이 · 획 길이 · 굵기 · 피해 배율)  → 아래 「평타가 묻는 값」을 CharacterKit.Cartographer 가 읽음
//   획을 그은 뒤 (먹물 웅덩이 · 먹 튀김 · 왕복 획)                         → Stroked (CharacterKit.MapAttack 이 부름)
//   도형을 닫은 뒤 (접지술 게이지 +5%)                                    → ShapeClosed (CharacterKit.CloseShape · 행군 고리가 부름)
//   지도 접기 뒤 (남는 접힌 선 · 북극성)                                   → Folded (SignatureSkills.OnFolded 가 부름)
//   걸어간 길 (행군 측량) · 게이지 (접지술) · 웅덩이 피해                     → Tick
// 밸런스 (1.3.7 실측 26분 → 약 30분으로 늦춘 수준 유지 · 보스 화력 ±8%):
//   기준 = 획 1 + 삼각형 2.2 ÷ 3 ≈ 획당 1.73. 갈래마다 획 · 도형 배율과 대가로 보스 화력을 비슷하게 맞추고,
//   차이는 무리(웅덩이 · 고리 · 넓은 도형) · 생존(고리 뒤 받는 피해 −30%) · 기동(걸어야 하는 선)에서 냄
// 안전: 상태는 모두 「끝나는 시각」(until) 값 · 무적 없음 · 보스 invulnerable 안 건드림 · 웅덩이 40 상한(Paths.Trim) · 겹친 웅덩이는 피해 한 번
public sealed class CartographerPaths : HeroPaths
{
    public const int Survey = 0, Stroke = 1, Ink = 2, Fold = 3, March = 4;
    // 새 교차 번호 (= Crosses 배열 순서)
    public const int XInkMarch = 2, XBlueprint = 3;

    public override CharacterId Who => CharacterId.Cartographer;
    public override string IconKey => "cartographer";

    protected override PathDef[] BuildPaths() => new[]
    {
        new PathDef {
            key = "c.survey", name = "정밀 측량", tagline = "네 획 사각형 · 무리 일괄",
            seed = "정밀 측량", seedDesc = "도형이 네 획마다 사각형으로 닫힙니다. (넓이 ×1.8 · 피해 ×1.4)",
            cost = "도형이 닫히는 주기가 느려집니다.", strong = "무리 일괄 · 보스 가두기", weak = "주기",
            cards = new[] { A, S(0), S(8) }, evo = "ke.world",
            res1 = "도형 안의 적에게 측량 표식이 저절로 붙습니다.",
            res2 = "직전 도형과 겹친 곳의 적은 도형 피해가 2배입니다. (보스 제외)",
            mastery = "도형이 오각형이 됩니다. (넓이 ×2.5 · 피해 ×1.6)",
            color = new Color(0.45f, 0.65f, 1f), form = "측량 사슬" },
        new PathDef {
            key = "c.stroke", name = "일필", tagline = "도형 대신 굵은 한 획",
            seed = "일필휘지", seedDesc = "도형이 닫히지 않는 대신 모든 획의 길이 ×1.4 · 굵기 ×1.5 · 피해 ×1.7.",
            cost = "도형 효과가 없습니다.", strong = "보스 · 일렬", weak = "포위",
            cards = new[] { M, S(2), S(4) }, evo = "ke.bold",
            res1 = "여백 대기 시간이 3초에서 1.5초로 줄어듭니다.",
            res2 = "획 끝에 먹이 튀며 작게 터집니다. (공격력 30%)",
            mastery = "획이 되돌아오며 한 번 더 벱니다. (피해 50%)",
            color = new Color(0.85f, 0.85f, 0.95f) },
        new PathDef {
            key = "c.ink", name = "먹물", tagline = "획이 잉크 웅덩이 · 지역",
            seed = "먹물병 엎기", seedDesc = "모든 획이 4초 동안 잉크 웅덩이를 남깁니다. (초당 공격력 40% · 겹쳐도 한 번만)",
            cost = "획 피해가 15% 줄어듭니다.", strong = "지역 무리", weak = "이동 보스",
            cards = new[] { P, S(9), K }, evo = "ke.blot",
            res1 = "웅덩이 위의 적이 40% 느려집니다.",
            res2 = "웅덩이 위에서 쓰러진 적에게서 잉크 폭풍이 일어납니다. (공격력 50%)",
            mastery = "웅덩이가 8초 남습니다.",
            color = new Color(0.35f, 0.4f, 0.8f), form = "잉크병" },
        new PathDef {
            key = "c.fold", name = "접지술", tagline = "지도 접기 중심 · 필살기 폭딜",
            seed = "접지술", seedDesc = "지도 접기 게이지가 60% 빨리 차고, 도형을 닫을 때마다 게이지가 5% 찹니다.",
            cost = "획 · 도형 피해가 25% 줄어듭니다.", strong = "필살기 폭딜 · 보스", weak = "주기 사이",
            cards = new[] { G, S(3), S(1) }, evo = "ke.constellation",
            res1 = "접힌 선이 3초 동안 남아 0.5초마다 벱니다. (공격력 20%)",
            res2 = "8초 안에 지도 접기를 두 번 쓰면 두 선의 교차점에 북극성이 뜹니다.",
            mastery = "지도 접기를 2번 모아 둡니다.",
            color = new Color(1f, 0.85f, 0.45f) },
        new PathDef {
            key = "c.march", name = "행군 측량", tagline = "걸어간 길이 선 · 기동 생존",
            seed = "행군 측량", seedDesc = "걸어간 길이 잉크 선이 되어 닿는 적을 0.3초마다 벱니다. (공격력 25%) 지나온 길이 고리로 닫히면 안쪽을 도형처럼 짓이깁니다. (공격력 200% · 2초에 한 번)",
            cost = "평타 획 피해가 30% 줄어듭니다.", strong = "기동 · 생존 · 무리", weak = "보스 (계속 걸어야 함)",
            cards = new[] { B, S(5), S(6), S(7) }, evo = "ke.express",
            res1 = "닫힌 고리 안의 적 투사체를 지웁니다.",
            res2 = "고리를 닫으면 1.5초 동안 이동 속도 +30% · 받는 피해 −30%.",
            mastery = "고리 피해가 2배가 됩니다.",
            color = new Color(0.5f, 0.95f, 0.6f), form = "지구본" },
    };

    protected override CrossDef[] BuildCrosses() => new[]
    {
        Cross(Ink, Fold, "ke.relief"),                      // 입체 지도 (K · S1)
        Cross(March, Survey, "ke.treasure"),                // 황금 항로 (S7 · S8)
        Cross(March, Ink, "c.x.inkmarch", "잉크 행군", "걸어간 길이 잉크 웅덩이가 됩니다."),
        Cross(Survey, Stroke, "c.x.blueprint", "대형 도면", "여백 획 하나가 도형을 바로 닫습니다."),
    };

    // ================================================================= 수치 (데이터 문장 · LocTable 과 같은 값)
    const float SquareArea = 1.8f, SquareDmg = 1.4f, PentaArea = 2.5f, PentaDmg = 1.6f;
    const float StrokeLen = 1.4f, StrokeWide = 1.5f, StrokeDmg = 1.7f;
    const float InkCost = 0.85f, FoldCost = 0.75f, MarchCost = 0.7f;
    const float SplashRatio = 0.3f, ReturnRatio = 0.5f;
    const float PuddleLife = 4f, PuddleLifeMastery = 8f, PuddlePerSec = 0.4f, PuddleTick = 0.5f, PuddleSlow = 0.6f, StormRatio = 0.5f;
    const int PuddleMax = 40;
    const float FoldGaugeBonus = 0.6f, FoldShapeGauge = 0.05f, CreaseLife = 3f, CreaseTick = 0.5f, CreaseRatio = 0.2f, FoldPairWindow = 8f;
    const float StarLife = 4f, StarTick = 0.6f, StarRatio = 0.4f, StarReach = 9f;
    const float TrailLife = 3f, TrailStep = 0.5f, TrailTick = 0.3f, TrailRatio = 0.25f, TrailHalf = 0.45f, LoopRatio = 2f;
    // 고리: 최소 12점(둘레 약 6칸) · 넓이 6 이상(반지름 약 1.4칸 원) · 2초에 한 번 — 작은 원을 빙빙 돌아 도형 효과(고유 스킬 · 게이지 · 공명 II)를 연사하지 않게
    const int TrailMax = 48, LoopMinPoints = 12;
    const float LoopMinArea = 6f, LoopCooldown = 2f;
    const float MarchBuff = 1.5f, MarchMove = 1.3f, MarchTaken = 0.7f, InkWalkStep = 1.5f;

    static readonly Color InkPool = new Color(0.18f, 0.24f, 0.75f, 0.5f);
    static readonly Color Gold = new Color(1f, 0.82f, 0.35f, 0.95f);
    static readonly Color TrailInk = new Color(0.35f, 0.9f, 0.55f, 0.85f);

    // ================================================================= 평타가 묻는 값 (CharacterKit.Cartographer 가 읽음 · 갈래 규칙이 아니면 모두 기본값)
    static CartographerPaths Me => Paths.Active ? Paths.Hero as CartographerPaths : null;

    // 일필: 도형이 닫히지 않음
    public static bool NoShapes => Paths.Has(Stroke);
    // 도형 각 수 · 닫히는 주기 (정밀 측량: 사각형 · 극의 오각형, 네 획마다)
    public static int ShapeSides => Paths.Has(Survey) ? (Paths.Mastered(Survey) ? 5 : 4) : 3;
    public static int ShapeEvery => Paths.Has(Survey) ? 4 : 3;
    // 같은 외접 반지름 기준 넓이 비 → 반지름 배율 (정삼각형 1.299 R² · 정사각형 2 R² · 정오각형 2.378 R²)
    public static float ShapeRadiusMul => !Paths.Has(Survey) ? 1f
        : Paths.Mastered(Survey) ? Mathf.Sqrt(PentaArea * 1.299f / 2.378f) : Mathf.Sqrt(SquareArea * 1.299f / 2f);
    public static float ShapeDmgMul => (Paths.Has(Survey) ? (Paths.Mastered(Survey) ? PentaDmg : SquareDmg) : 1f) * (Paths.Has(Fold) ? FoldCost : 1f);
    public static float StrokeLenMul => Paths.Has(Stroke) ? StrokeLen : 1f;
    public static float StrokeWidthMul => Paths.Has(Stroke) ? StrokeWide : 1f;
    public static float StrokeDmgMul => (Paths.Has(Stroke) ? StrokeDmg : 1f) * (Paths.Has(Ink) ? InkCost : 1f) * (Paths.Has(Fold) ? FoldCost : 1f) * (Paths.Has(March) ? MarchCost : 1f);
    // 정밀 측량 공명 I: 도형 안 적에게 표식 · 공명 II: 직전 도형과 겹친 곳 2배 (보스 제외)
    public static bool AutoMark => Paths.Has(Survey) && Paths.Res(Survey) >= 1;
    public static bool Overlap => Paths.Has(Survey) && Paths.Res(Survey) >= 2;
    // 일필 공명 I: 여백 대기 3초 → 1.5초 (SignatureSkills.Cartographer 의 여백)
    public static float MarginWait => Paths.Has(Stroke) && Paths.Res(Stroke) >= 1 ? 1.5f : 3f;
    // 대형 도면 (정밀 측량 × 일필): 여백 획이 도형을 바로 닫음
    public static bool BlueprintOn => Paths.CrossOn(XBlueprint);

    // ================================================================= 상태 (판마다 새 인스턴스 · static 없음)
    readonly List<GameObject> puddles = new List<GameObject>();      // 먹물 웅덩이 (SigZone · 상한 40)
    readonly HashSet<GameObject> wet = new HashSet<GameObject>();     // 이번 틱에 웅덩이 위에 있는 적 (겹쳐도 한 번)
    float puddleAt;
    bool storming;                                                    // 잉크 폭풍이 또 폭풍을 부르지 않게 (try/finally)
    float stormWindow = -9f;
    int stormCount;

    float foldBank;                                                   // 접지술 극의: 두 번째 충전 (0 ~ 1)
    float prevFoldAt = -99f;
    Vector3 prevFoldA, prevFoldB, prevFoldCenter;

    readonly List<Vector3> trail = new List<Vector3>(TrailMax + 1);  // 행군 측량: 걸어간 길 점
    readonly List<float> trailAt = new List<float>(TrailMax + 1);
    readonly List<GameObject> trailHit = new List<GameObject>(32);
    LineRenderer trailLine;
    Vector3 lastSample;
    bool sampled;
    float trailHitAt, inkWalk;
    float marchUntil = -99f;                                          // 고리를 닫은 뒤 빨라짐 · 단단해짐이 끝나는 시각
    float loopReadyAt;                                                // 다음 고리를 닫을 수 있는 시각 (재사용 대기)

    static bool Alive => Hostile.Player != null && !Hostile.Player.IsDying;

    // ================================================================= 판 흐름 훅
    public override void End()
    {
        if (trailLine != null) Object.Destroy(trailLine.gameObject);
        trailLine = null;
        trail.Clear();
        trailAt.Clear();
        foreach (GameObject g in puddles) if (g != null) Object.Destroy(g);
        puddles.Clear();
        wet.Clear();
    }

    // ================================================================= 전투 훅
    public override float Taken(float dmg) => Paths.Has(March) && Time.time < marchUntil ? dmg * MarchTaken : dmg;
    public override float MoveMul() => Paths.Has(March) && Time.time < marchUntil ? MarchMove : 1f;

    public override void Tick()
    {
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) return;
        if (Paths.Has(Fold)) FoldGauge(p);
        if (Paths.Has(March)) MarchTick(p.transform.position);
        else if (trailLine != null) trailLine.enabled = false;
        PuddleDamage();
    }

    public override void Ult()
    {
        // 접지술 극의: 모아 둔 두 번째 충전이 있으면 쓰자마자 게이지가 다시 가득
        if (!Paths.Mastered(Fold) || foldBank < 1f) return;
        PlayerController p = Hostile.Player;
        if (p == null || p.skillGauge == null) return;
        foldBank = 0f;
        p.skillGauge.AddSkillPoint(p.skillGauge.MaxSkillPoint);
    }

    public override void Killed(Vector3 pos)
    {
        // 먹물 공명 II: 웅덩이 위에서 쓰러진 적 → 잉크 폭풍 (폭풍이 또 폭풍을 부르지 않음 · 1초에 8번까지)
        if (storming || !Paths.Has(Ink) || Paths.Res(Ink) < 2 || !InPuddle(pos)) return;
        if (Time.time - stormWindow > 1f) { stormWindow = Time.time; stormCount = 0; }
        if (stormCount >= 8) return;
        stormCount++;
        storming = true;
        try
        {
            Paths.Circle(pos, 2.2f, Paths.Atk * StormRatio * Paths.Power(Ink), 1f, "잉크 폭풍");
            Fx.Spawn("fx_puddle", pos, 3.5f, new Color(0.2f, 0.28f, 0.8f, 0.9f), 18f);
        }
        finally { storming = false; }
    }

    // HUD 입문 칸: 접지술 극의의 두 번째 충전이 차 있으면 「x2」 (문자열 상수라 매 프레임 새로 만들지 않음)
    public override string Badge(int path) => path == Fold && Paths.Mastered(Fold) && foldBank >= 1f ? "x2" : null;

    // 1.4.1~ 이어하기: 접지술 극의의 두 번째 충전(foldBank)은 카드 기록으로 되살아나지 않아 판 저장에 따로 남김 (「x2」가 사라지고 필살기 뒤 게이지가 다시 안 차던 것)
    public override float SaveState() => Paths.Mastered(Fold) ? foldBank : 0f;
    public override void LoadState(float v) => foldBank = Paths.Mastered(Fold) && !float.IsNaN(v) ? Mathf.Clamp01(v) : 0f;

    // ================================================================= 획을 그은 뒤 (CharacterKit.MapAttack)
    public static void Stroked(CharacterKit kit, Vector3 from, Vector3 to, float width, float dmg) => Me?.OnStroke(kit, from, to, width, dmg);

    void OnStroke(CharacterKit kit, Vector3 from, Vector3 to, float width, float dmg)
    {
        if (!Alive) return;
        // 먹물: 획 자리에 잉크 웅덩이 (선분 장판)
        if (Paths.Has(Ink)) SpawnInkLine(from, to, 0.55f + width * 0.35f);
        // 일필 공명 II: 획 끝 먹 튀김
        if (Paths.Has(Stroke) && Paths.Res(Stroke) >= 2)
        {
            Paths.Circle(to, 1.6f, Paths.Atk * SplashRatio * Paths.Power(Stroke), 0.8f, "먹 튀김");
            Fx.Spawn("fx_puddle", to, 2.8f, new Color(0.2f, 0.25f, 0.6f, 0.85f), 18f);
            Fx.Spawn("fx_spark", to, 1.6f, new Color(0.85f, 0.9f, 1f), 24f);
        }
        // 일필 극의: 획이 되돌아옴
        if (Paths.Mastered(Stroke) && kit != null) kit.PathReturnStroke(from, to, width, dmg * ReturnRatio);
    }

    // ================================================================= 도형을 닫은 뒤 (CharacterKit.CloseShape · 행군 고리)
    public static void ShapeClosed() => Me?.OnShape();

    void OnShape()
    {
        // 접지술: 도형마다 게이지 5%
        if (!Paths.Has(Fold)) return;
        PlayerController p = Hostile.Player;
        if (p != null && p.skillGauge != null) p.skillGauge.AddSkillPoint(p.skillGauge.MaxSkillPoint * FoldShapeGauge);
    }

    // ================================================================= 지도 접기 뒤 (SignatureSkills.OnFolded)
    public static void Folded(Vector3 center, Vector3 a, Vector3 b) => Me?.OnFold(center, a, b);

    void OnFold(Vector3 center, Vector3 a, Vector3 b)
    {
        if (!Alive || !Paths.Has(Fold)) return;
        // 공명 I: 접힌 선이 3초 남아 벰
        if (Paths.Res(Fold) >= 1) Crease(a, b);
        // 공명 II: 8초 안에 두 번 → 두 선의 교차점에 북극성 (짝을 쓰면 처음부터)
        if (Paths.Res(Fold) >= 2 && Time.time - prevFoldAt <= FoldPairWindow)
        {
            Vector3 at = SegCross(prevFoldA, prevFoldB, a, b, out Vector3 x) ? x : (prevFoldCenter + center) * 0.5f;
            NorthStar(at);
            prevFoldAt = -99f;
        }
        else
        {
            prevFoldAt = Time.time;
            prevFoldA = a;
            prevFoldB = b;
            prevFoldCenter = center;
        }
    }

    void Crease(Vector3 a, Vector3 b)
    {
        SigZone z = Paths.Zone((a + b) * 0.5f, 1.2f, CreaseLife, CreaseTick, CreaseTickOn);     // 판정: 적 가운데와 선의 거리 1.2 (큰 보스도 닿게)
        if (z == null) return;
        z.segment = true;
        z.a = a;
        z.b = b;
        FxAnim mark = Fx.Beam(a, b, 0.7f, new Color(1f, 0.85f, 0.45f, 0.55f), CreaseLife, 15);
        if (mark != null) { mark.transform.SetParent(z.transform, true); z.visual = mark; }
    }

    static void CreaseTickOn(SigZone z)
    {
        if (!Alive || SignatureSkills.Instance == null) return;
        float dmg = Paths.Atk * CreaseRatio * Paths.Power(Fold);
        foreach (Collider2D c in SignatureSkills.Instance.Inside(z))
            if (c != null) Paths.Deal(c.gameObject, dmg, Vector3.zero, 0f, "접힌 선");
    }

    void NorthStar(Vector3 at)
    {
        Paths.Zone(at, StarReach, StarLife, StarTick, StarTickOn, "fx_sparkle", Gold, 2.6f);
        ShockRing.Spawn(at, 0.3f, 3f, 0.4f, Gold, 0.22f);
        Hostile.Play("chime", 0.6f, 1.5f);
    }

    static void StarTickOn(SigZone z)
    {
        if (!Alive) return;
        Vector3 p = z.transform.position;
        List<Transform> near = Paths.Nearest(p, StarReach, 1);
        if (near == null || near.Count == 0 || near[0] == null) return;
        Transform t = near[0];
        Fx.Beam(p, t.position, 0.35f, Gold, 0.15f);
        Paths.Deal(t.gameObject, Paths.Atk * StarRatio * Paths.Power(Fold), (t.position - p).normalized, 0.3f, "북극성");
    }

    // 두 선분의 교차점
    static bool SegCross(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2, out Vector3 x)
    {
        x = Vector3.zero;
        Vector2 r = p2 - p1, s = q2 - q1, qp = q1 - p1;
        float d = r.x * s.y - r.y * s.x;
        if (Mathf.Abs(d) < 1e-4f) return false;
        float t = (qp.x * s.y - qp.y * s.x) / d, u = (qp.x * r.y - qp.y * r.x) / d;
        if (t < 0f || t > 1f || u < 0f || u > 1f) return false;
        x = p1 + (Vector3)(r * t);
        return true;
    }

    // ---------------- 접지술 게이지: 원래 차는 양의 +60% (멈춤 · 스킬 쓰는 중에는 원래처럼 안 참) · 극의면 가득 찬 뒤 두 번째 충전
    void FoldGauge(PlayerController p)
    {
        SkillGauge g = p.skillGauge;
        if (g == null || p.IsSkillUsing || Time.timeScale == 0f) return;
        float tree = SpecialAbilities.SharedInstance != null ? SpecialAbilities.SharedInstance.TreeGaugeMul : 1f;
        float add = g.pointsPerSecond * GameMode.GaugeMul / Mathf.Max(0.1f, CharacterData.Current.gauge) * (g.Boosted ? g.killBoost : 1f) * tree * Time.deltaTime;
        if (!g.IsFull()) g.AddSkillPoint(add * FoldGaugeBonus);
        else if (Paths.Mastered(Fold) && foldBank < 1f) foldBank = Mathf.Min(1f, foldBank + add * (1f + FoldGaugeBonus) / Mathf.Max(1, g.MaxSkillPoint));
    }

    // ================================================================= 먹물 웅덩이 (먹물 · 잉크 행군)
    float PuddleSeconds => Paths.Mastered(Ink) ? PuddleLifeMastery : PuddleLife;

    void SpawnInkLine(Vector3 a, Vector3 b, float half)
    {
        float life = PuddleSeconds;
        // 거의 같은 자리의 웅덩이는 새로 만들지 않고 늘림 (보스를 겨누며 제자리에서 그으면 같은 획이 반복됨)
        foreach (GameObject g in puddles)
        {
            if (g == null || !g.TryGetComponent(out SigZone z) || !z.segment) continue;
            if ((z.a - a).sqrMagnitude < 0.8f * 0.8f && (z.b - b).sqrMagnitude < 0.8f * 0.8f) { Extend(z, life); return; }
        }
        Paths.Trim(puddles, PuddleMax);
        SigZone nz = Paths.Zone((a + b) * 0.5f, half, life, 0f, null);
        if (nz == null) return;
        nz.segment = true;
        nz.a = a;
        nz.b = b;
        FxAnim v = Fx.Beam(a, b, half * 2.4f, InkPool, life, 3);
        if (v != null) { v.transform.SetParent(nz.transform, true); nz.visual = v; }
        puddles.Add(nz.gameObject);
    }

    void SpawnInkPool(Vector3 at, float r)
    {
        float life = PuddleSeconds;
        foreach (GameObject g in puddles)
        {
            if (g == null || !g.TryGetComponent(out SigZone z) || z.segment) continue;
            if ((g.transform.position - at).sqrMagnitude < r * r) { Extend(z, life); return; }
        }
        Paths.Trim(puddles, PuddleMax);
        SigZone nz = Paths.Zone(at, r, life, 0f, null, "fx_puddle", InkPool, r * 2.4f, true);
        if (nz != null) puddles.Add(nz.gameObject);
    }

    // 웅덩이 수명을 「지금부터 life」로 늘림 (그림도 같이)
    static void Extend(SigZone z, float life)
    {
        float more = z.age + life - z.life;
        if (more <= 0f) return;
        z.life += more;
        if (z.visual != null) z.visual.life += more;
    }

    bool InPuddle(Vector3 pos)
    {
        foreach (GameObject g in puddles)
            if (g != null && g.TryGetComponent(out SigZone z) && z.Contains(pos)) return true;
        return false;
    }

    // 웅덩이 피해는 한곳에서 0.5초마다: 여러 웅덩이가 겹쳐도 적 하나에 한 번 (= 겹친 웅덩이 합치기)
    void PuddleDamage()
    {
        if (puddles.Count == 0 || Time.time < puddleAt) return;
        puddleAt = Time.time + PuddleTick;
        puddles.RemoveAll(g => g == null);
        SignatureSkills s = SignatureSkills.Instance;
        if (s == null || puddles.Count == 0) return;
        wet.Clear();
        foreach (GameObject g in puddles)
        {
            if (g == null || !g.TryGetComponent(out SigZone z)) continue;
            // SignatureSkills.Inside 와 같은 판정이지만 웅덩이마다 새 목록을 만들지 않음 (빌린 목록 PhysHits)
            Vector3 c0 = z.segment ? (z.a + z.b) * 0.5f : z.transform.position;
            float r = z.segment ? Vector3.Distance(z.a, z.b) * 0.5f + z.radius : z.radius;
            using (PhysHits hits = PhysHits.Circle(c0, r, true))
                foreach (Collider2D c in hits)
                {
                    if (c == null || !Paths.IsFoe(c)) continue;
                    if (z.segment && Hostile.DistanceToSegment(c.transform.position, z.a, z.b) > z.radius) continue;
                    wet.Add(c.gameObject);
                }
        }
        if (wet.Count == 0) return;
        float dmg = Paths.Atk * PuddlePerSec * PuddleTick * Paths.Power(Ink);
        bool slow = Paths.Has(Ink) && Paths.Res(Ink) >= 1;
        foreach (GameObject e in wet)
        {
            if (e == null) continue;
            Paths.Deal(e, dmg, Vector3.zero, 0f, "먹물 웅덩이");
            if (slow && e != null && e.TryGetComponent(out EnermyController ec)) ec.Slow(PuddleSlow, 0.6f);
        }
        wet.Clear();
    }

    // ================================================================= 행군 측량: 걸어간 길
    void MarchTick(Vector3 pos)
    {
        // 오래된 점은 지움
        int old = 0;
        while (old < trailAt.Count && Time.time - trailAt[old] > TrailLife) old++;
        if (old > 0) { trail.RemoveRange(0, old); trailAt.RemoveRange(0, old); }

        if (!sampled) { lastSample = pos; sampled = true; }
        float step = ((Vector2)(pos - lastSample)).magnitude;
        if (step > 6f)
        {
            // 순간이동 (되감기 · 돌진 등): 선이 끊김
            trail.Clear();
            trailAt.Clear();
            lastSample = pos;
        }
        else if (step >= TrailStep)
        {
            if (trail.Count >= TrailMax) { trail.RemoveAt(0); trailAt.RemoveAt(0); }
            trail.Add(pos);
            trailAt.Add(Time.time);
            lastSample = pos;
            // 잉크 행군 (먹물 교차): 걸어간 길에 잉크 웅덩이
            if (Paths.CrossOn(XInkMarch))
            {
                inkWalk += step;
                if (inkWalk >= InkWalkStep) { inkWalk = 0f; SpawnInkPool(pos, 1f); }
            }
            TryLoop();
        }
        if (Time.time >= trailHitAt)
        {
            trailHitAt = Time.time + TrailTick;
            TrailHit();
        }
        DrawTrail(pos);
    }

    // 잉크 선에 닿은 적을 벰 (선분 48개까지 · 0.3초마다)
    void TrailHit()
    {
        if (trail.Count < 2) return;
        Vector3 min = trail[0], max = trail[0];
        foreach (Vector3 v in trail) { min = Vector3.Min(min, v); max = Vector3.Max(max, v); }
        Vector3 c = (min + max) * 0.5f;
        float r = ((Vector2)(max - min)).magnitude * 0.5f + 1.5f;
        trailHit.Clear();
        using (PhysHits hits = PhysHits.Circle(c, r, true))
        {
            foreach (Collider2D col in hits)
            {
                if (col == null || !Paths.IsFoe(col)) continue;
                Vector2 p = col.bounds.center;
                float lim = TrailHalf + CharacterKit.MapBodyOf(col);
                for (int k = 1; k < trail.Count; k++)
                    if (Hostile.DistanceToSegment(p, trail[k - 1], trail[k]) <= lim) { trailHit.Add(col.gameObject); break; }
            }
        }
        float dmg = Paths.Atk * TrailRatio * Paths.Power(March);
        foreach (GameObject g in trailHit) if (g != null) Paths.Deal(g, dmg, Vector3.zero, 0f, "행군 잉크 선");
        trailHit.Clear();
    }

    // 새 점이 예전 점(12점 이상 앞) 가까이에 오면 고리가 닫힘 → 안쪽을 짓이기고 선을 새로 시작
    void TryLoop()
    {
        int n = trail.Count - 1;
        if (n < LoopMinPoints || Time.time < loopReadyAt) return;
        Vector3 last = trail[n];
        for (int i = 0; i <= n - LoopMinPoints; i++)
        {
            if (((Vector2)(trail[i] - last)).sqrMagnitude > 1.1f * 1.1f) continue;
            List<Vector3> poly = trail.GetRange(i, n - i + 1);
            if (Mathf.Abs(Area(poly)) < LoopMinArea) continue;     // 왔다 갔다 한 줄 · 아주 작은 원은 고리가 아님
            loopReadyAt = Time.time + LoopCooldown;
            CloseLoop(poly);
            trail.RemoveRange(0, n);
            trailAt.RemoveRange(0, n);
            return;
        }
    }

    static float Area(List<Vector3> poly)
    {
        float s = 0f;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++) s += (poly[j].x + poly[i].x) * (poly[j].y - poly[i].y);
        return s * 0.5f;
    }

    void CloseLoop(List<Vector3> poly)
    {
        Vector3 center = Vector3.zero;
        foreach (Vector3 p in poly) center += p;
        center /= poly.Count;
        // 1.4.1~ 도형처럼: 끊기지 않는 선이면 고리가 넓어지고, 지구본 끌어모음 · +30% · 측량 표식 · 판화 · 게이지도 붙음 (CharacterKit.MapLoop*)
        CharacterKit kit = CharacterKit.Instance;
        float grow = kit != null ? kit.MapLoopScale : 1f;
        if (grow > 1f)
        {
            poly = new List<Vector3>(poly);
            for (int i = 0; i < poly.Count; i++) poly[i] = center + (poly[i] - center) * grow;
        }
        float reach = 0f;
        foreach (Vector3 p in poly) reach = Mathf.Max(reach, (p - center).magnitude);

        List<Collider2D> inside = new List<Collider2D>();
        using (PhysHits hits = PhysHits.Circle(center, reach + 0.5f, true))
            foreach (Collider2D c in hits)
                if (c != null && Paths.IsFoe(c) && CharacterKit.MapInShape(c, poly)) inside.Add(c);
        float dmg = Paths.Atk * LoopRatio * Paths.Power(March) * (Paths.Mastered(March) ? 2f : 1f);
        if (kit != null) kit.MapLoopPull(center, inside);
        foreach (Collider2D c in inside)
            if (c != null) Paths.Deal(c.gameObject, dmg * (kit != null ? kit.MapLoopMul(c.gameObject) : 1f), (c.transform.position - center).normalized, 1.2f, "행군 고리");
        if (kit != null) kit.MapLoopClosed(center, reach, inside);

        // 공명 I: 고리 안의 적 투사체를 지움
        if (Paths.Res(March) >= 1)
            foreach (HostileProjectile hp in new List<HostileProjectile>(HostileProjectile.Live))
                if (hp != null && CharacterKit.MapInPolygon(hp.transform.position, poly)) Object.Destroy(hp.gameObject);
        // 공명 II: 1.5초 동안 빨라지고 단단해짐 (끝나는 시각 하나)
        if (Paths.Res(March) >= 2) marchUntil = Time.time + MarchBuff;

        for (int i = 0; i < poly.Count; i++) Fx.Beam(poly[i], poly[(i + 1) % poly.Count], 0.45f, Gold, 0.5f, 16);
        Fx.Spawn("fx_shock", center, Mathf.Max(3f, reach * 2.2f), new Color(0.45f, 0.95f, 0.6f, 0.8f), 18f);
        ShockRing.Spawn(center, 0.3f, Mathf.Max(1.5f, reach), 0.35f, Gold, 0.22f);
        Hostile.Play("chime", 0.6f, 1.3f);
        Hostile.Play("paperfold", 0.5f, 1.1f);
        if (inside.Count >= 4) Hostile.Shake(0.08f);

        // 도형처럼: 도형에 붙는 고유 스킬(범례 · 등고선 · 나침도 · 보물 지도 …)과 접지술 게이지도 같이
        SignatureSkills.Closed(poly, center, reach, inside);
        OnShape();
    }

    void DrawTrail(Vector3 pos)
    {
        if (trailLine == null)
        {
            trailLine = Hostile.NewLine("MarchTrail", TrailInk, 0.32f, AttackLook.AreaOrder - 2);
            trailLine.loop = false;
            trailLine.gameObject.AddComponent<MarchTrailGuard>();      // 쓰러지는 중엔 Tick 이 안 불리므로 선이 스스로 숨음
        }
        int n = trail.Count;
        trailLine.enabled = n >= 1 && !PlayerController.Cinematic;
        if (!trailLine.enabled) return;
        trailLine.positionCount = n + 1;
        for (int i = 0; i < n; i++) trailLine.SetPosition(i, trail[i]);
        trailLine.SetPosition(n, pos);
        bool buff = Time.time < marchUntil;
        trailLine.startColor = new Color(TrailInk.r, TrailInk.g, TrailInk.b, 0.15f);
        trailLine.endColor = buff ? Gold : TrailInk;
        trailLine.startWidth = 0.18f;
        trailLine.endWidth = buff ? 0.42f : 0.34f;
    }
}

// 행군 잉크 선 지킴이: 갈래 Tick(SignatureSkills.SignatureTick)은 쓰러지는 중 · 멈춤 동안 안 불려 선이 화면에 그대로 남던 것 —
//   선 오브젝트 스스로 쓰러지는 중이면 숨음 (되살아나면 DrawTrail 이 다시 켬 · 새 오브젝트를 만들지 않음)
public sealed class MarchTrailGuard : MonoBehaviour
{
    LineRenderer line;

    void Awake() => line = GetComponent<LineRenderer>();

    void LateUpdate()
    {
        if (line == null || !line.enabled) return;
        PlayerController p = Hostile.Player;
        if (p == null || p.IsDying) line.enabled = false;
    }
}
