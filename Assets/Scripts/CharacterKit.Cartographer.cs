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
    // 측량 표식: 획에 맞은 적 (도형 안에서 피해 +)
    readonly Dictionary<GameObject, float> surveyMarks = new Dictionary<GameObject, float>();

    // 그리는 중: 마지막 획 뒤 잠깐 (지름길 · 직행로)
    public bool MapChainActive => IsCartographer && Time.time < mapChainUntil;
    const float MapChainTime = 1.4f;
    const int ShapeEvery = 3;          // 이 획마다 도형이 저절로 닫힘
    // 도형(삼각형) 반지름: 지구본 1.5배, 끊기지 않는 선 단계마다 +15%
    float ShapeRadius => 2.4f * (form == SpecialAbilities.KitGlobe ? 1.5f : 1f) * (1f + 0.15f * card[6]);
    bool ShapeNext => strokeCount % ShapeEvery == ShapeEvery - 1;

    float StrokeRange => def.range * SignatureSkills.StrokeRangeMul();

    float StrokeWidth => 0.75f * (1f + 0.3f * card[1]) * SignatureSkills.StrokeWidthMul();

    void MapAttack(Vector3 start, Vector2 dir, float dmg, int shots)
    {
        Vector3 from = start;
        Vector3 to = StrokeEnd(from, dir);

        float width = StrokeWidth;
        float lenMul = SignatureSkills.StrokeMul((to - from).magnitude, StrokeRange);
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

        strokeCount++;
        if (card[2] > 0 && strokeCount % 4 == 0) CompassNeedles(to, card[2]);
        if (augment == AugReprint) StartCoroutine(Reprint(from, to, width, dmg * lenMul * 0.5f));

        // 자동 도형: 세 번째 획마다 획 끝(마우스 자리)을 감싸는 삼각형이 저절로 닫힘
        if (strokeCount % ShapeEvery == 0)
        {
            CloseShape(Triangle(to, ShapeRadius, shapeAngle), dmg);
            shapeAngle = Random.Range(0f, 120f);
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
            Specials.Damage(c.gameObject, dmg * m, dir, 1f);
            if (card[5] > 0) surveyMarks[c.gameObject] = Time.time + 5f;
            Fx.Spawn("fx_spark", c.transform.position, 0.8f, Ink, 26f);
            n++;
        }
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
            float tick = Damage * (0.05f + 0.1f * card[0]) * SignatureSkills.InkSpreadMul();
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

        float mul = 2.6f * formPower * SignatureSkills.CloseMul();
        if (form == SpecialAbilities.KitGlobe) mul *= 1.3f;
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
            if (card[5] > 0 && surveyMarks.TryGetValue(c.gameObject, out float until) && until > Time.time) m *= 1f + 0.3f * card[5];
            Specials.Damage(c.gameObject, dmg * m, (c.transform.position - center).normalized, 1.5f);
            Fx.Spawn("fx_sparkle", c.transform.position, 1.2f, InkGold, 24f);
        }
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
        // 측량 표식: 쓰러진 적 · 지난 표식은 지움 (계속 쌓이지 않게)
        if (surveyMarks.Count > 64)
        {
            List<GameObject> old = new List<GameObject>();
            foreach (var kv in surveyMarks) if (kv.Key == null || kv.Value < Time.time) old.Add(kv.Key);
            foreach (GameObject g in old) surveyMarks.Remove(g);
        }
        if (card[6] > 0 && player.skillGauge != null) player.skillGauge.AddSkillPoint(player.skillGauge.MaxSkillPoint * (0.01f + 0.03f * card[6]));
        Play("chime", 0.6f, 1.25f);
        Play("paperfold", 0.6f, 1.2f);
        Play("stamp", 0.5f, 0.9f);
        if (inside.Count >= 4) Hostile.Shake(0.1f);
        SpecialAbilities.SharedFx?.FloatText(center + Vector3.up, Loc.T("측량 완료!"), InkGold, 4f, 0.4f);
        SignatureSkills.Closed(poly, center, reach, inside);
    }

    static List<Vector3> Triangle(Vector3 c, float r, float a0)
    {
        List<Vector3> poly = new List<Vector3>(3);
        for (int i = 0; i < 3; i++)
        {
            float a = (a0 + 120f * i) * Mathf.Deg2Rad;
            poly.Add(c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r);
        }
        return poly;
    }

    // 적 몸 반지름 (작은 쪽 기준, 판정이 너무 후하지 않게)
    static float MapBody(Collider2D c) => Mathf.Min(c.bounds.extents.x, c.bounds.extents.y) * 0.7f;

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
        mapCloseRing.enabled = IsCartographer && ShapeNext && !PlayerController.Cinematic && Time.timeScale > 0f;
        if (!mapCloseRing.enabled) return;
        float pulse = 0.55f + 0.35f * Mathf.Sin(Time.time * 10f);
        List<Vector3> tri = Triangle(StrokeEnd(player.MuzzlePosition, body.flipX ? Vector2.left : Vector2.right), ShapeRadius, shapeAngle);
        mapCloseRing.positionCount = tri.Count;
        for (int i = 0; i < tri.Count; i++) mapCloseRing.SetPosition(i, tri[i]);
        mapCloseRing.startColor = mapCloseRing.endColor = new Color(InkGold.r, InkGold.g, InkGold.b, pulse);
        mapCloseRing.startWidth = mapCloseRing.endWidth = 0.1f + 0.05f * pulse;
    }

    // ================================================================= 우클릭: 지도 접기
    bool folding;
    float foldCharge;
    LineRenderer foldLine, foldSideA, foldSideB;

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
        }
        float pulse = 0.55f + 0.35f * Mathf.Sin(Time.time * 14f);
        SetSeg(foldLine, a, b, new Color(0.6f, 0.75f, 1f, pulse), 0.12f + 0.12f * foldCharge);
        Vector3 off = (Vector3)(n * half);
        Color side = Color.Lerp(new Color(0.45f, 0.6f, 1f), InkGold, foldCharge >= 1f ? 0.5f + 0.5f * Mathf.Sin(Time.time * 14f) : 0f);
        side.a = 0.5f + 0.35f * foldCharge;
        SetSeg(foldSideA, a + off, b + off, side, 0.1f);        // 다 모이면(최대 넓이) 금빛으로 깜빡
        SetSeg(foldSideB, a - off, b - off, side, 0.1f);
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
        Play("whoosh", 0.9f, 0.7f);
        Fx.Beam(a + (Vector3)(n * half), b + (Vector3)(n * half), 0.8f, new Color(0.5f, 0.65f, 1f, 0.7f), 0.22f);
        Fx.Beam(a - (Vector3)(n * half), b - (Vector3)(n * half), 0.8f, new Color(0.5f, 0.65f, 1f, 0.7f), 0.22f);
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
        Fx.Beam(a, b, 1.8f, new Color(0.75f, 0.85f, 1f, 0.95f), 0.4f, 18);
        Fx.Beam(a, b, 0.7f, InkGold, 0.5f, 19);
        Fx.Spawn("fx_shock", center, len * 0.8f, new Color(0.45f, 0.6f, 1f, 0.85f), 18f);
        for (int i = 0; i <= 4; i++) Fx.Spawn("fx_slash", Vector3.Lerp(a, b, i / 4f), 3f, new Color(0.8f, 0.9f, 1f), 26f, Mathf.Atan2(crease.y, crease.x) * Mathf.Rad2Deg);
        Hostile.Shake(0.22f);
        Play("boom", 0.7f, 1.2f);
        Play("paperfold", 1f, 0.8f);
        Play("crack", 0.6f, 0.8f);
        SignatureSkills.Folded(center, a, b, caught, dmg);
        if (card[3] > 0) StartCoroutine(Crease(a, b, 1f + card[3]));
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
    }
}
