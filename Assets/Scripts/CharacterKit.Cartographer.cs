using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 지도 제작자 (1.0.2): 깃펜으로 잉크 획을 그어 벰
//   평타 · 잉크 획: 나에게서 마우스 쪽으로 획을 그음 (사거리 안)
//                   1.0.2: 세 번째 획마다 마우스 자리를 감싸는 삼각형이 저절로 닫혀 안쪽의 적이 크게 다침
//                   (예전: 앞 획 끝에서 이어 그려 직접 도형을 닫아야 했음 — 너무 어렵다는 의견)
//   우클릭 · 지도 접기: 누르고 있는 동안 접히는 선(마우스 자리, 나와 마우스를 잇는 선에 수직)이 보이고,
//                   떼면 선 양쪽 띠 안의 적을 선 위로 끌어당겨 짓이김 (오래 누를수록 넓고 길게)
// 레벨업 카드 (card): 0 번지는 잉크 · 1 굵은 펜촉 · 2 나침반 바늘 · 3 접힌 자국 · 5 측량 표식 · 6 끊기지 않는 선
// 형태 (무기 진화 1차): 측량 사슬 · 잉크병 · 지구본 / 강화 (2차): 정밀 측량 · 이중 인쇄 · 판화
// 2026-10-19~ 빌드 갈래 (Paths/Paths.Cartographer.cs): 도형 각 수 · 주기 · 넓이 · 획 길이 · 굵기 · 피해 배율을 CartographerPaths 에 물음
//   정밀 측량 사각형(극의 오각형) · 일필 도형 없음 · 먹물 웅덩이 · 접지술 게이지 · 행군 고리 — 갈래 규칙이 아니면 모두 기본값(삼각형 · 세 획)
public partial class CharacterKit
{
    public bool IsCartographer => Id == CharacterId.Cartographer;
    public bool Moving => stillTime <= 0.05f;       // 움직이는 중 (현지 안내)
    static readonly Color Ink = new Color(0.22f, 0.36f, 0.92f, 0.95f);
    static readonly Color InkGold = new Color(1f, 0.82f, 0.35f, 0.95f);

    float mapChainUntil;
    int strokeCount;
    float shapeAngle;                  // 다음 도형(삼각형)의 방향 — 미리 보기와 실제가 같게 미리 정해 둠
    LineRenderer mapCloseRing;
    // 정밀 측량 공명 II: 직전 도형 (겹친 곳 피해 2배)
    List<Vector3> mapPrevShape;
    float mapPrevShapeAt = -99f;
    // 측량 표식: 획에 맞은 적 (도형 안에서 피해 +)
    readonly Dictionary<GameObject, float> surveyMarks = new Dictionary<GameObject, float>();

    // 그리는 중: 마지막 획 뒤 잠깐 (지름길 · 직행로)
    public bool MapChainActive => IsCartographer && Time.time < mapChainUntil;
    const float MapChainTime = 1.4f;
    int ShapeEvery => CartographerPaths.ShapeEvery;     // 이 획마다 도형이 저절로 닫힘 (기본 3 · 정밀 측량 4)
    // 도형 반지름: 지구본 1.5배, 끊기지 않는 선 단계마다 +15%, 정밀 측량 사각형 · 오각형은 넓이에 맞춰
    float ShapeRadius => 2.4f * (form == SpecialAbilities.KitGlobe ? 1.5f : 1f) * (1f + 0.15f * card[6]) * CartographerPaths.ShapeRadiusMul;
    bool ShapeNext => !CartographerPaths.NoShapes && strokeCount % ShapeEvery == ShapeEvery - 1;

    float StrokeRange => def.range * SignatureSkills.StrokeRangeMul() * CartographerPaths.StrokeLenMul;

    float StrokeWidth => 0.75f * (1f + 0.3f * card[1]) * SignatureSkills.StrokeWidthMul() * CartographerPaths.StrokeWidthMul;

    void MapAttack(Vector3 start, Vector2 dir, float dmg, int shots)
    {
        bool margin = SignatureSkills.MarginReady;      // 이번 획이 여백 획인가 (긋기 전에 봄 · 대형 도면)
        Vector3 from = start;
        Vector3 to = StrokeEnd(from, dir);

        float width = StrokeWidth;
        float lenMul = SignatureSkills.StrokeMul((to - from).magnitude, StrokeRange) * CartographerPaths.StrokeDmgMul;     // 갈래 획 배율 (일필 ×1.7 · 대가)
        int hits = Stroke(from, to, width, dmg * lenMul);
        // 여러 발: 옆으로 나란한 획을 더 그음
        Vector2 sd = ((Vector2)(to - from)).normalized;
        Vector3 side = new Vector3(-sd.y, sd.x, 0f);
        for (int i = 1; i < shots; i++)
        {
            float off = ((i + 1) / 2) * (i % 2 == 1 ? 1f : -1f) * (width * 1.6f);
            hits += Stroke(from + side * off, to + side * off, width * 0.8f, dmg * lenMul * 0.8f);
        }
        SignatureSkills.Stroke(from, to, hits);
        StrokeExtras(from, to, dmg);
        CartographerPaths.Stroked(this, from, to, width, dmg * lenMul);      // 먹물 웅덩이 · 먹 튀김 · 왕복 획

        strokeCount++;
        if (card[2] > 0 && strokeCount % 4 == 0) CompassNeedles(to, card[2]);
        if (augment == AugReprint) StartCoroutine(Reprint(from, to, width, dmg * lenMul * 0.5f));

        // 자동 도형: 세 번째 획마다 획 끝(마우스 자리)을 감싸는 삼각형이 저절로 닫힘
        //   갈래: 정밀 측량 네 번째 획 · 사각형(극의 오각형) / 일필 닫히지 않음 / 대형 도면(교차) 여백 획이면 바로 닫힘
        bool blueprint = margin && CartographerPaths.BlueprintOn;
        if ((!CartographerPaths.NoShapes && strokeCount % ShapeEvery == 0) || blueprint)
        {
            CloseShape(MapPolygon(to, ShapeRadius, shapeAngle, CartographerPaths.ShapeSides), dmg * CartographerPaths.ShapeDmgMul);
            shapeAngle = Random.Range(0f, 360f / CartographerPaths.ShapeSides);
        }
        mapChainUntil = Time.time + MapChainTime;
        PlayerMotion.Of(this).Recoil(-sd, 0.4f);
        Play("ink", 0.6f, 1f + Random.Range(-0.1f, 0.1f));
        Play("whoosh", 0.25f, 1.6f);
        DrawMapChain();
    }

    // 획 끝 (= 도형이 닫히는 자리): 마우스 자리 (사거리 안), 너무 가까우면 1칸 앞
    Vector3 StrokeEnd(Vector3 from, Vector2 dir)
    {
        Vector3 to = ClampRange(transform.position, Mouse, StrokeRange);
        // 일필: 획 길이 ×1.4 (마우스 너머로 · 늘어난 사거리 안)
        if (CartographerPaths.StrokeLenMul > 1f)
            to = from + Vector3.ClampMagnitude((to - from) * CartographerPaths.StrokeLenMul, StrokeRange);
        if ((to - from).magnitude < 1f) to = from + (Vector3)(((Vector2)(to - from)).sqrMagnitude > 0.01f ? ((Vector2)(to - from)).normalized : dir);
        return to;
    }

    // 한 획: 선분 굵기 안의 적을 벰
    int Stroke(Vector3 a, Vector3 b, float width, float dmg)
    {
        int n = 0;
        Vector3 mid = (a + b) * 0.5f;
        float r = (b - a).magnitude * 0.5f + width;
        Vector2 dir = ((Vector2)(b - a)).normalized;
        // 1.1.2 뒤: 판정 반폭 = 보이는 획 반폭 (바깥 잉크 띠 2 × width × 단계 굵기 BandMul 의 절반) + 적 몸 — 예전엔 단계와 상관없이 width
        float band = AttackLook.BandMul(AttackLook.Tier);
        float half = width * band;
        r += half - width;
        List<Collider2D> found = new List<Collider2D>(Specials.Overlap(mid, r, mapHits));
        int stacks = 0;
        foreach (Collider2D c in found)
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            // 1.0.2: 큰 적(보스)은 몸 가장자리에 닿아도 맞게 (예전엔 몸 가운데만 봐서 보이는 획이 지나가도 안 맞았음)
            if (Hostile.DistanceToSegment(c.bounds.center, a, b) > half + MapBody(c)) continue;
            float m = formPower;
            // 정밀 제도 (패시브): 획 끝 가까이의 적은 더 아프게
            if (Special != null && Special.Has(SpecialAbilities.KitDraft) && (c.transform.position - b).magnitude <= 1.4f)
                m *= Special.IsEvolved(SpecialAbilities.KitDraft) ? 1.5f : 1.3f;
            // 정밀 측량 (2차): 적 하나를 지날 때마다 +10% (최대 +50%)
            if (augment == AugSurvey) m *= 1f + 0.1f * Mathf.Min(5, stacks);
            stacks++;
            WeakHit.Damage(c.gameObject, WeakShape.Segment(a, b, half, 1.75f), dmg * m, dir, 1f);     // 빈틈: 평타 획만 (이중 인쇄는 나중 프레임이라 조준 범위 밖)
            if (card[5] > 0) surveyMarks[c.gameObject] = Time.time + 5f;
            Fx.Spawn("fx_spark", c.transform.position, 0.8f, Ink, 26f);
            n++;
        }
        PruneSurveyMarks();             // 1.4.1~ 도형이 안 닫히는 판(일필)에서도 획 표식이 쌓이지 않게 (예전엔 도형을 닫을 때만 지웠음)
        // 1.1.2: 획을 한눈에 — 판정 폭(반폭 width = 폭 2 × width + 적 몸)까지 굵게 (예전 1.2 × width), 밝은 잉크 테 + 흰 심 줄
        //   짙은 남색 잉크는 어두운 묘역 · 심연 바닥에 묻혀서 테두리 색을 밝게, 적 · 맵 물체 위 순서
        Fx.Beam(a, b, width * 2f * band, SkinFx.Tint(new Color(0.35f, 0.55f, 1f, 0.95f)), 0.45f, AttackLook.AreaOrder - 1);
        Fx.Beam(a, b, width * 0.8f * band, new Color(0.92f, 0.96f, 1f, 1f), 0.25f, AttackLook.AreaOrder);
        Fx.Spawn("fx_trail_dot", b, 1.8f * band, new Color(0.55f, 0.75f, 1f), 20f, 0f, AttackLook.AreaOrder + 1);
        return n;
    }
    static readonly List<Collider2D> mapHits = new List<Collider2D>(64);

    // 획 끝에 붙는 것: 번지는 잉크(카드) · 형태 능력
    void StrokeExtras(Vector3 from, Vector3 to, float dmg)
    {
        if (card[0] > 0 && Special != null)
        {
            // 번지는 잉크: 획을 따라 잉크 웅덩이 셋
            float tick = Damage * (0.025f + 0.05f * card[0]) * SignatureSkills.InkSpreadMul();      // 1.3.7: 0.5초 틱이라 실제 초당 30/50/70% 였음 → 카드 설명대로 15/25/35%
            float rr = 0.9f * SignatureSkills.InkSpreadMul();
            for (int i = 0; i < 3; i++)
                Special.SpawnZone(Vector3.Lerp(from, to, (i + 0.5f) / 3f), rr, 2f, tick, new Color(0.2f, 0.3f, 0.85f, 0.55f));
        }
        if (form == SpecialAbilities.KitSurveyChain)
        {
            // 측량 사슬: 획 끝에서 가장 가까운 적에게 사슬이 튐
            Transform t = Specials.NearestEnemy(to, 6f);
            if (t != null)
            {
                Fx.Bolt(to, t.position, 0.6f, new Color(0.7f, 0.8f, 1f));
                Specials.Damage(t.gameObject, dmg * 0.5f * formPower, ((Vector2)(t.position - to)).normalized, 0.5f);
            }
        }
        else if (form == SpecialAbilities.KitInkpot)
        {
            // 잉크병: 획 끝에서 잉크병이 깨져 작게 터짐
            DamageCircle(to, 1.5f, dmg * 0.6f * formPower, 1f);
            Fx.Spawn("fx_puddle", to, 3f, new Color(0.2f, 0.25f, 0.7f, 0.85f), 18f);
            Play("glassclink", 0.35f, 1.4f);
        }
    }

    // 도형이 닫힘: 안쪽의 적을 통째로
    void CloseShape(List<Vector3> poly, float dmg)
    {
        Vector3 center = Vector3.zero;
        foreach (Vector3 p in poly) center += p;
        center /= poly.Count;
        float reach = 0f;
        foreach (Vector3 p in poly) reach = Mathf.Max(reach, (p - center).magnitude);

        float mul = 2.2f * formPower * SignatureSkills.CloseMul();
        if (form == SpecialAbilities.KitGlobe) mul *= 1.3f;
        // 정밀 측량 공명 I: 표식 단계가 없어도 1단계처럼 · 공명 II: 직전 도형(6초 안)과 겹친 곳 2배 (보스 제외)
        bool autoMark = CartographerPaths.AutoMark;
        int markLv = Mathf.Max(card[5], autoMark ? 1 : 0);
        List<Vector3> prev = CartographerPaths.Overlap && mapPrevShape != null && Time.time - mapPrevShapeAt <= 6f ? mapPrevShape : null;
        List<Collider2D> inside = new List<Collider2D>();
        foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(center, reach + 0.5f, mapHits)))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            if (!InShape(c, poly)) continue;
            inside.Add(c);
        }
        foreach (Collider2D c in inside)
        {
            if (c == null) continue;
            // 지구본: 안쪽 적이 가운데로 모임 (보스 제외)
            if (form == SpecialAbilities.KitGlobe && c.CompareTag("enermy"))
                c.transform.position = Vector3.Lerp(c.transform.position, center, 0.6f);
            float m = mul;
            if (markLv > 0 && surveyMarks.TryGetValue(c.gameObject, out float until) && until > Time.time) m *= 1f + 0.3f * markLv;
            bool twice = prev != null && !Paths.IsBoss(c.gameObject) && InShape(c, prev);
            if (twice) m *= 2f;
            Specials.Damage(c.gameObject, dmg * m, (c.transform.position - center).normalized, 1.5f);
            if (c != null) Fx.Spawn("fx_sparkle", c.transform.position, twice ? 1.7f : 1.2f, twice ? Color.white : InkGold, 24f);
        }
        if (autoMark)
            foreach (Collider2D c in inside)
                if (c != null) surveyMarks[c.gameObject] = Time.time + 5f;
        mapPrevShape = poly;
        mapPrevShapeAt = Time.time;
        // 그림: 닫힌 테두리를 금색으로 한 번 더 · 가운데 섬광
        for (int i = 0; i < poly.Count; i++)
            Fx.Beam(poly[i], poly[(i + 1) % poly.Count], 0.5f, InkGold, 0.6f, 16);
        Fx.Spawn("fx_shock", center, Mathf.Max(3f, reach * 2.2f), new Color(0.45f, 0.6f, 1f, 0.8f), 18f);
        ShockRing.Spawn(center, 0.3f, Mathf.Max(1.5f, reach), 0.35f, InkGold, 0.22f);
        if (augment == AugEngrave)
        {
            // 판화: 안쪽에 잉크 폭발
            DamageCircle(center, Mathf.Max(1.5f, reach * 0.8f), Damage * 1.5f * formPower, 2f);
            Fx.Spawn("fx_explosion", center, Mathf.Max(3f, reach * 1.8f), new Color(0.3f, 0.4f, 1f), 18f);
        }
        PruneSurveyMarks();
        if (card[6] > 0 && player.skillGauge != null) player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * (0.01f + 0.03f * card[6]));
        Play("chime", 0.6f, 1.25f);
        Play("paperfold", 0.6f, 1.2f);
        Play("stamp", 0.5f, 0.9f);
        if (inside.Count >= 4) Hostile.Shake(0.1f);
        SpecialAbilities.SharedFx?.FloatText(center + Vector3.up, Loc.T("측량 완료!"), InkGold, 4f, 0.4f);
        SignatureSkills.Closed(poly, center, reach, inside);
        CartographerPaths.ShapeClosed();        // 접지술: 도형마다 게이지
    }

    // ---------------- 1.4.1~ 행군 고리 (갈래 행군 측량 · Paths.Cartographer.CloseLoop)도 「도형이 닫힘」처럼
    //   예전엔 끊기지 않는 선(card[6]) · 측량 표식(card[5]) · 지구본 · 판화가 CloseShape 안에만 있어, 일필(도형 없음) + 행군 판에서 효과 0
    // 고리 넓이 배율 (끊기지 않는 선 단계마다 +15%, 도형 반지름과 같게)
    public float MapLoopScale => IsCartographer ? 1f + 0.15f * card[6] : 1f;

    // 고리 피해 전: 지구본이면 안쪽 적이 가운데로 모임 (보스 제외)
    public void MapLoopPull(Vector3 center, List<Collider2D> inside)
    {
        if (!IsCartographer || form != SpecialAbilities.KitGlobe) return;
        foreach (Collider2D c in inside)
            if (c != null && c.CompareTag("enermy")) c.transform.position = Vector3.Lerp(c.transform.position, center, 0.6f);
    }

    // 고리 피해 배율: 지구본 +30% · 측량 표식 (정밀 측량 공명 I 이면 1단계처럼)
    public float MapLoopMul(GameObject g)
    {
        if (!IsCartographer || g == null) return 1f;
        float m = form == SpecialAbilities.KitGlobe ? 1.3f : 1f;
        int markLv = Mathf.Max(card[5], CartographerPaths.AutoMark ? 1 : 0);
        if (markLv > 0 && surveyMarks.TryGetValue(g, out float until) && until > Time.time) m *= 1f + 0.3f * markLv;
        return m;
    }

    // 고리 피해 뒤: 정밀 측량 공명 I 표식 · 판화 잉크 폭발 · 끊기지 않는 선 게이지
    public void MapLoopClosed(Vector3 center, float reach, List<Collider2D> inside)
    {
        if (!IsCartographer) return;
        if (CartographerPaths.AutoMark)
            foreach (Collider2D c in inside)
                if (c != null) surveyMarks[c.gameObject] = Time.time + 5f;
        PruneSurveyMarks();             // 도형이 안 닫히는 판(일필 + 행군)에서도 표식이 쌓이지 않게
        if (augment == AugEngrave)
        {
            DamageCircle(center, Mathf.Max(1.5f, reach * 0.8f), Damage * 1.5f * formPower, 2f);
            Fx.Spawn("fx_explosion", center, Mathf.Max(3f, reach * 1.8f), new Color(0.3f, 0.4f, 1f), 18f);
        }
        if (card[6] > 0 && player != null && player.skillGauge != null) player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * (0.01f + 0.03f * card[6]));
    }

    // 측량 표식: 쓰러진 적 · 지난 표식은 지움 (계속 쌓이지 않게)
    void PruneSurveyMarks()
    {
        if (surveyMarks.Count <= 64) return;
        List<GameObject> old = new List<GameObject>();
        foreach (var kv in surveyMarks) if (kv.Key == null || kv.Value < Time.time) old.Add(kv.Key);
        foreach (GameObject g in old) surveyMarks.Remove(g);
    }

    // 도형이 하나도 닫히지 않는 판인지 (일필 · 대형 도면 교차 없음 · 행군 고리 없음) — 지구본 · 판화 진화를 고를 수 없게 (SpecialAbilities.NextEvolutionOptions)
    public static bool MapNoClosures => CartographerPaths.NoShapes && !CartographerPaths.BlueprintOn && !Paths.Has(CartographerPaths.March);

    // 정다각형 (기본 삼각형 · 정밀 측량 사각형 · 극의 오각형)
    static List<Vector3> MapPolygon(Vector3 c, float r, float a0, int sides)
    {
        sides = Mathf.Max(3, sides);
        List<Vector3> poly = new List<Vector3>(sides);
        for (int i = 0; i < sides; i++)
        {
            float a = (a0 + 360f / sides * i) * Mathf.Deg2Rad;
            poly.Add(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r);
        }
        return poly;
    }

    // 적 몸 반지름 (작은 쪽 기준, 판정이 너무 후하지 않게)
    static float MapBody(Collider2D c) => Mathf.Min(c.bounds.extents.x, c.bounds.extents.y) * 0.7f;

    // ---------------- 갈래 효과(CartographerPaths)가 쓰는 공개 래퍼
    public static float MapBodyOf(Collider2D c) => c != null ? MapBody(c) : 0f;
    public static bool MapInShape(Collider2D c, List<Vector3> poly) => c != null && poly != null && poly.Count >= 3 && InShape(c, poly);
    public static bool MapInPolygon(Vector3 p, List<Vector3> poly) => poly != null && poly.Count >= 3 && InPolygon(p, poly);

    // 일필 극의: 0.15초 뒤 획이 끝에서 처음으로 되돌아오며 한 번 더 벰 (쓰러진 뒤에는 안 함)
    public void PathReturnStroke(Vector3 from, Vector3 to, float width, float dmg)
    {
        if (!isActiveAndEnabled) return;
        StartCoroutine(MapReturnStroke(from, to, width, dmg));
    }

    IEnumerator MapReturnStroke(Vector3 from, Vector3 to, float width, float dmg)
    {
        yield return new WaitForSeconds(0.15f);
        if (player == null || player.IsDying) yield break;
        Stroke(to, from, width, dmg);
    }

    // 도형 안: 몸 가운데가 안쪽이거나 몸이 테두리에 걸침
    static bool InShape(Collider2D c, List<Vector3> poly)
    {
        Vector3 p = c.bounds.center;
        if (InPolygon(p, poly)) return true;
        float r = MapBody(c);
        for (int i = 0; i < poly.Count; i++)
            if (Hostile.DistanceToSegment(p, poly[i], poly[(i + 1) % poly.Count]) <= r) return true;
        return false;
    }

    static bool InPolygon(Vector3 p, List<Vector3> poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            Vector3 a = poly[i], b = poly[j];
            if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y + 1e-6f) + a.x) inside = !inside;
        }
        return inside;
    }

    // 나침반 바늘: 가까운 적에게 바늘이 날아감
    void CompassNeedles(Vector3 from, int count)
    {
        using var noAim = WeakHit.AimScope(0f);       // 빈틈: 스스로 겨누는 자동 표적 탄이라 후보가 아님 (평타 조준 범위 안에서 불려도)
        List<Transform> targets = SignatureSkills.Nearest(from, 12f, count);
        foreach (Transform t in targets)
        {
            Vector2 d = ((Vector2)(t.position - from)).normalized;
            Projectile(from, d, Damage * 1.2f, 1, 30f, 14f, "fx_markbolt", 0.6f, new Color(1f, 0.4f, 0.35f), false);
        }
        if (targets.Count > 0) Play("pew", 0.4f, 1.5f);
    }

    // 이중 인쇄 (2차): 0.3초 뒤 같은 자리에 한 번 더
    IEnumerator Reprint(Vector3 a, Vector3 b, float width, float dmg)
    {
        yield return new WaitForSeconds(0.3f);
        Stroke(a, b, width, dmg);
    }

    // 다음 획에 도형이 닫힐 때: 마우스 자리에 실제로 닫힐 삼각형을 금색으로 미리 보여 줌 (매 프레임)
    //   1.0.2: 예전엔 삼각형을 감싸는 원을 그려 실제 범위(삼각형)보다 훨씬 커 보였고, 스킬 이펙트에 묻혔음
    void DrawMapChain()
    {
        if (mapCloseRing == null)
        {
            mapCloseRing = Hostile.NewLine("MapClose", InkGold, 0.14f, 57);
            mapCloseRing.loop = true;
        }
        // 대형 도면(교차): 여백 획이 준비되면 다음 획에 닫힐 도형을 미리 보여 줌
        bool next = ShapeNext || (CartographerPaths.BlueprintOn && SignatureSkills.MarginReady);
        mapCloseRing.enabled = IsCartographer && next && !PlayerController.Cinematic && Time.timeScale > 0f;
        if (!mapCloseRing.enabled) return;
        float pulse = 0.55f + 0.35f * Mathf.Sin(Time.time * 10f);
        List<Vector3> tri = MapPolygon(StrokeEnd(player.MuzzlePosition, body.flipX ? Vector2.left : Vector2.right), ShapeRadius, shapeAngle, CartographerPaths.ShapeSides);
        mapCloseRing.positionCount = tri.Count;
        for (int i = 0; i < tri.Count; i++) mapCloseRing.SetPosition(i, tri[i]);
        mapCloseRing.startColor = mapCloseRing.endColor = new Color(InkGold.r, InkGold.g, InkGold.b, pulse);
        mapCloseRing.startWidth = mapCloseRing.endWidth = 0.1f + 0.05f * pulse;
    }

    // ================================================================= 우클릭: 지도 접기
    bool folding;
    float foldCharge;
    LineRenderer foldLine, foldSideA, foldSideB, foldCompass;

    void FoldGeometry(out Vector3 center, out Vector3 a, out Vector3 b, out Vector2 normal, out float half, out float len)
    {
        Vector3 me = transform.position;
        center = ClampRange(me, Mouse, 12f);
        normal = ((Vector2)(center - me)).normalized;
        if (normal.sqrMagnitude < 0.01f) normal = body.flipX ? Vector2.left : Vector2.right;
        Vector2 crease = new Vector2(-normal.y, normal.x);
        len = (10f + 6f * foldCharge) * Mathf.Sqrt(UltMul);
        half = (3f + 3f * foldCharge) * UltMul;
        a = center - (Vector3)(crease * len * 0.5f);
        b = center + (Vector3)(crease * len * 0.5f);
    }

    void UpdateFold(SkillGauge gauge, bool full)
    {
        DrawMapChain();
        // 멈춤 · 연출 중: 접던 것은 게이지를 쓰지 않고 취소 (예전엔 멈춘 채로 떼면 게이지를 쓰고 접혔음)
        if (Time.timeScale == 0f || PlayerController.Cinematic)
        {
            if (folding) CancelFold();
            return;
        }
        if (!folding && GameInput.UltDown && full)
        {
            folding = true;
            foldCharge = 0f;
            Play("hum", 0.5f, 1.2f);
        }
        if (!folding) return;
        foldCharge = Mathf.Min(1f, foldCharge + Time.deltaTime / 1.2f);
        FoldGeometry(out Vector3 center, out Vector3 a, out Vector3 b, out Vector2 n, out float half, out float len);
        if (foldLine == null)
        {
            // 1.0.2: 스킬 이펙트(+40)에 묻히지 않게 그 위 순서로
            foldLine = Hostile.NewLine("FoldLine", Ink, 0.16f, 57);
            foldSideA = Hostile.NewLine("FoldSideA", Ink, 0.1f, 56);
            foldSideB = Hostile.NewLine("FoldSideB", Ink, 0.1f, 56);
            PixelLine.Apply(foldLine, true);                    // 1.5.0~ 접힘선은 점선 잉크
        }
        if (foldCompass == null) { foldCompass = Hostile.NewLine("FoldCompass", Ink, 0.1f, 57); foldCompass.loop = true; }
        float pulse = 0.55f + 0.35f * Mathf.Sin(Time.time * 14f);
        // 1.5.0~ 범위표시 색: 양피지 · 잉크 갈색 (예전 파랑은 시계공 · 유령 하늘색과 겹쳤음) · 다 모이면 금빛 깜빡은 그대로
        Color map = HeroAim.ColorOf(HeroAimKind.Cartographer);
        float gold = foldCharge >= 1f ? 0.5f + 0.5f * Mathf.Sin(Time.time * 14f) : 0f;
        SetSeg(foldLine, a, b, new Color(map.r * 0.75f, map.g * 0.7f, map.b * 0.6f, pulse), 0.12f + 0.12f * foldCharge);
        Vector3 off = (Vector3)(n * half);
        Color side = Color.Lerp(map, InkGold, gold);
        side.a = 0.5f + 0.35f * foldCharge;
        SetSeg(foldSideA, a + off, b + off, side, 0.1f);        // 다 모이면(최대 넓이) 금빛으로 깜빡
        SetSeg(foldSideB, a - off, b - off, side, 0.1f);
        // 접힐 영역 = 양피지 지도 (판정과 같은 len × 2·half) · 양쪽에서 접힘선으로 흘러드는 화살표 (HeroAimDecor) · 가운데 나침반 고리
        HeroAim.Mark(foldLine, HeroAimKind.Cartographer);
        HeroAim.SetArea(foldLine, half, gold);
        HeroAim.Mark(foldSideA, HeroAimKind.Cartographer, HeroAimFx.None);
        HeroAim.Mark(foldSideB, HeroAimKind.Cartographer, HeroAimFx.None);
        foldCompass.enabled = true;
        Hostile.SetArc(foldCompass, center, 0.9f, 0f, 360f);
        foldCompass.startColor = foldCompass.endColor = new Color(map.r, map.g, map.b, 0.9f);
        foldCompass.startWidth = foldCompass.endWidth = 0.1f;
        HeroAim.Mark(foldCompass, HeroAimKind.Cartographer, HeroAimFx.None);
        if (AddedUltShown) DrawUltAim(false, true);

        if (!GameInput.UltUp && GameInput.UltHeld) return;
        CancelFold();
        float dmg = Damage * (3f + 4f * foldCharge) * UltMul;
        StartCoroutine(FoldRoutine(center, a, b, n, half, len, dmg));
        PlayerMotion.Of(this).Cast(0.35f);
        KitUltAdd(0.3f, center);
        Spend(gauge);
    }

    void CancelFold()
    {
        folding = false;
        if (foldLine != null) foldLine.enabled = foldSideA.enabled = foldSideB.enabled = false;
        if (foldCompass != null) foldCompass.enabled = false;
        CancelUltAim();
    }

    static void SetSeg(LineRenderer lr, Vector3 a, Vector3 b, Color c, float w)
    {
        lr.enabled = true;
        lr.loop = false;
        lr.positionCount = 2;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.startColor = lr.endColor = c;
        lr.startWidth = lr.endWidth = w;
    }

    IEnumerator FoldRoutine(Vector3 center, Vector3 a, Vector3 b, Vector2 n, float half, float len, float dmg)
    {
        Vector2 crease = ((Vector2)(b - a)).normalized;
        List<Collider2D> caught = new List<Collider2D>();
        foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(center, Mathf.Sqrt(half * half + len * len * 0.25f) + 1f, mapHits)))
        {
            if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
            Vector2 d = c.bounds.center - center;
            float br = MapBody(c);
            if (Mathf.Abs(Vector2.Dot(d, n)) > half + br || Mathf.Abs(Vector2.Dot(d, crease)) > len * 0.5f + br) continue;
            caught.Add(c);
        }
        // 접히는 종이: 양쪽 띠가 선 쪽으로 닫힘
        // 1.5.0~ 사용자 「놓는 순간 예전 선 세 개 대신 지금 보이는 지도 그림이 닫히는 표현으로」: 빛줄기 선 → 양피지 두 쪽이 접힘선으로 접혀 닫힘 · 접힌 자국 금빛 번쩍
        Play("whoosh", 0.9f, 0.7f);
        StartCoroutine(FoldCloseArt(center, crease, n, half, (b - a).magnitude));
        var from = new Dictionary<Collider2D, Vector3>();
        foreach (Collider2D c in caught) if (c != null && c.CompareTag("enermy")) from[c] = c.transform.position;
        for (float t = 0f; t < 0.22f; t += Time.deltaTime)
        {
            float k = t / 0.22f;
            foreach (var kv in from)
            {
                if (kv.Key == null) continue;
                Vector2 d = kv.Value - center;
                Vector3 onLine = center + (Vector3)(crease * Vector2.Dot(d, crease));
                kv.Key.transform.position = Vector3.Lerp(kv.Value, onLine, k * k);
            }
            yield return null;
        }
        foreach (Collider2D c in caught)
        {
            if (c == null) continue;
            Specials.Damage(c.gameObject, dmg, n, 0f);
            if (c != null && c.TryGetComponent(out EnermyController e)) e.Slow(0f, 0.8f);
        }
        Fx.Spawn("fx_shock", center, len * 0.8f, new Color(0.45f, 0.6f, 1f, 0.85f), 18f);
        for (int i = 0; i <= 4; i++) Fx.Spawn("fx_slash", Vector3.Lerp(a, b, i / 4f), 3f, new Color(0.8f, 0.9f, 1f), 26f, Mathf.Atan2(crease.y, crease.x) * Mathf.Rad2Deg);
        Hostile.Shake(0.22f);
        Play("boom", 0.7f, 1.2f);
        Play("paperfold", 1f, 0.8f);
        Play("crack", 0.6f, 0.8f);
        SignatureSkills.Folded(center, a, b, caught, dmg);
        if (card[3] > 0) StartCoroutine(Crease(a, b, 1f + card[3]));
    }

    // 1.5.0~ 지도 접기 발동 그림: 양피지 두 쪽(접힘선 양옆 · 폭 half)이 0.22초 동안 접힘선으로 접혀 닫힌 뒤, 접힌 자국(좁은 양피지 띠)이 금빛으로 번쩍이며 0.3초에 사라짐
    //   그림만 (판정 · 끌어오기는 FoldRoutine 그대로) · 맨 위 · 어떤 경로로 끝나도 finally 에서 파괴
    IEnumerator FoldCloseArt(Vector3 mid, Vector2 crease, Vector2 n, float half, float len)
    {
        float deg = Mathf.Atan2(crease.y, crease.x) * Mathf.Rad2Deg;
        SpriteRenderer[] sides = new SpriteRenderer[2];
        try
        {
            for (int i = 0; i < 2; i++)
            {
                GameObject go = new GameObject("FoldCloseSide");
                sides[i] = go.AddComponent<SpriteRenderer>();
                sides[i].sharedMaterial = PixelStrokeMin.TopSpriteMaterial;
                PixelStrokeMin.PutOnTop(sides[i], -1);
            }
            float z = PixelStrokeMin.TopZ(mid.z) + 0.03f;
            for (float t = 0f; t < 0.22f; t += Time.deltaTime)
            {
                float k = t / 0.22f;
                float w = Mathf.Max(0.15f, half * (1f - k * k));          // 끌려오는 적과 같은 빠르기(k²)로 좁아짐
                for (int i = 0; i < 2; i++)
                {
                    float s = i == 0 ? 1f : -1f;
                    sides[i].sprite = HeroAimArt.Parchment(len, w, deg);
                    sides[i].color = new Color(1f, 1f, 1f, 1f);
                    Vector3 p = mid + (Vector3)(n * (s * w * 0.5f));
                    sides[i].transform.position = new Vector3(p.x, p.y, z);
                }
                yield return null;
            }
            // 접힌 자국: 두 쪽이 겹친 좁은 띠가 금빛으로 번쩍였다 사라짐
            Destroy(sides[1].gameObject);
            sides[1] = null;
            sides[0].sprite = HeroAimArt.Parchment(len, 0.6f, deg);
            sides[0].transform.position = new Vector3(mid.x, mid.y, z);
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                float k = t / 0.3f;
                sides[0].color = Color.Lerp(new Color(1f, 0.85f, 0.4f, 1f), new Color(1f, 0.9f, 0.6f, 0f), k);
                yield return null;
            }
        }
        finally
        {
            for (int i = 0; i < 2; i++) if (sides[i] != null) Destroy(sides[i].gameObject);
        }
    }

    // 접힌 자국 (카드): 접힌 선에 자국이 남아 0.3초마다 벰
    IEnumerator Crease(Vector3 a, Vector3 b, float seconds)
    {
        FxAnim mark = Fx.Beam(a, b, 0.6f, new Color(0.35f, 0.5f, 1f, 0.6f), seconds);
        for (float t = 0f; t < seconds; t += 0.3f)
        {
            yield return new WaitForSeconds(0.3f);
            Vector3 mid = (a + b) * 0.5f;
            foreach (Collider2D c in new List<Collider2D>(Specials.Overlap(mid, (b - a).magnitude * 0.5f + 0.8f, mapHits)))
            {
                if (c == null || (!c.CompareTag("enermy") && !c.CompareTag("boss"))) continue;
                if (Hostile.DistanceToSegment(c.bounds.center, a, b) > 0.8f + MapBody(c)) continue;
                Specials.Damage(c.gameObject, Damage * 0.4f, Vector3.zero, 0f);
            }
        }
        if (mark != null) Destroy(mark.gameObject);
    }

    void OnDestroyCartographer()
    {
        if (mapCloseRing != null) Destroy(mapCloseRing.gameObject);
        if (foldLine != null) Destroy(foldLine.gameObject);
        if (foldSideA != null) Destroy(foldSideA.gameObject);
        if (foldSideB != null) Destroy(foldSideB.gameObject);
        if (foldCompass != null) Destroy(foldCompass.gameObject);
    }
}
