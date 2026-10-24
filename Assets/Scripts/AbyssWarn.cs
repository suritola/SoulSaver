using UnityEngine;

// 1.5.8~ 4장 잡몹 스킬 예고: 넷 다 같은 직선 띠 · 원판(룬 · 좁혀 드는 고리)이라 단조롭고, 여럿이 겹치면 어지러웠음
//   (사용자 「4장 잡몹들의 스킬 범위 표시가 너무 단조롭고 많이 겹쳐서 어지러워」)
//   → 스킬마다 다른 모양 · 색: 사슬 = 점선 테두리 + 뻗어 나가는 사슬, 시선 = 훑는 방향으로 차오르는 부채꼴,
//     베기 = 안쪽 원 → 바깥 고리 (처음부터 둘 다 보여 줌), 균열 = 갈라져 나가는 금
//   판정 크기는 그대로, 바닥 채움은 옅게, 룬 · 좁혀 드는 고리는 없음. 시전한 적이 죽으면 공격도 취소되므로 바로 거둠
public class AbyssWarn : MonoBehaviour
{
    enum Kind { Chain, Fan, Reap, Crack }

    Kind kind;
    EnermyController owner;
    float duration, duration2, t, blinkPhase;
    Color color;

    // 사슬
    Vector3 a, b;
    LineRenderer edgeL, edgeR, chain, hook;
    // 부채꼴 (transform = 중심)
    float radius, fromDeg, sweepDeg;
    Mesh bgMesh, fillMesh;
    MeshRenderer bgRenderer, fillRenderer;
    LineRenderer fanEdge, startEdge, fanBand;
    // 베기
    float r1, r2;
    LineRenderer innerRing, outerRing, ringBand;
    SpriteRenderer innerFill;
    float innerFillScale;
    // 균열
    Vector3[][] cracks;
    LineRenderer[] crackLines, crackBands;

    // ---------------------------------------------------------------- 만들기
    static AbyssWarn Make(string name, Vector3 pos, Kind kind, float duration, Color color, EnermyController owner)
    {
        GameObject go = new GameObject(name);
        go.transform.position = pos;
        AbyssWarn w = go.AddComponent<AbyssWarn>();
        w.kind = kind;
        w.duration = Mathf.Max(0.05f, duration);
        w.color = Hostile.Accessible(KindColor(kind, color));
        w.owner = owner;
        return w;
    }

    // 1.6.0~ 가독성 개편 C6: 적 공격 색을 적대 대역(빨강 · 보라 쪽)으로 — 단 1.5.8 「스킬마다 다른 모양 · 색」이라 종류마다 다른 색
    //   (VisualRules.HostileColor 를 그대로 걸면 영혼 194° · 균열 157° 가 보라 285° 로 모여 공허와 같은 색이 됨)
    //   사슬 강철 흰(채도 0.15 = 닫히는 고리 흰색) · 시선 공허 보라 272°(이미 적대 대역)는 그대로,
    //   베기 영혼 하늘 194° → 자홍 323° · 균열 민트 157° → 주홍 14°. 알파는 받은 색 그대로 · Legacy 면 예전 색
    //   색각 모드는 Make 가 Hostile.Accessible 을 덧씌움 → 보라 · 자홍은 둘 다 자홍(모양으로 구분) · 주홍은 주황 · 강철 그대로
    static Color KindColor(Kind kind, Color c)
    {
        if (VisualRules.Legacy) return c;
        switch (kind)
        {
            case Kind.Reap: return new Color(1f, 0.42f, 0.8f, c.a);
            case Kind.Crack: return new Color(1f, 0.45f, 0.25f, c.a);
            default: return c;
        }
    }

    LineRenderer Line(string name, Color c, float width, int order, bool dashed = false)
    {
        LineRenderer lr = Hostile.NewLine(name, c, width, order, VisualRules.WarnLayer);
        if (dashed) PixelLine.Apply(lr, true);
        lr.transform.SetParent(transform, false);
        return lr;
    }

    // 점선은 마디마다 한 칸이라 일정 간격으로 점을 찍음
    static void SetDashed(LineRenderer lr, Vector3 from, Vector3 to, float step)
    {
        int n = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(from, to) / step), 1, 60) + 1;
        Vector3[] buf = Hostile.PointBuffer(n);
        for (int i = 0; i < n; i++) buf[i] = Vector3.Lerp(from, to, i / (float)(n - 1));
        if (lr.positionCount != n) lr.positionCount = n;
        lr.SetPositions(buf);
    }

    // 사슬 망령: 판정 폭 점선 테두리 + 끝의 갈고리 고리 + 경로를 따라 뻗어 나가는 사슬 (바닥 띠 없음)
    public static GameObject Chain(Vector3 from, Vector3 to, float width, float duration, Color color, EnermyController owner)
    {
        AbyssWarn w = Make("WarnChain", from, Kind.Chain, duration, color, owner);
        color = w.color;        // 1.6.0~ 종류별 적대 색 · 색각 모드 색으로 처음부터 그림 (예전엔 첫 프레임만 받은 색 그대로였음)
        w.a = from;
        w.b = to;
        Vector3 side = Vector3.Cross(to - from, Vector3.forward).normalized * width * 0.5f;
        w.edgeL = w.Line("Edge", color, 0.14f, 18, true);
        w.edgeR = w.Line("Edge", color, 0.14f, 18, true);
        SetDashed(w.edgeL, from + side, to + side, 0.55f);
        SetDashed(w.edgeR, from - side, to - side, 0.55f);
        w.chain = w.Line("Chain", Color.white, 0.32f, 19, true);
        w.chain.positionCount = 0;
        w.hook = w.Line("Hook", color, 0.16f, 18);
        w.hook.loop = true;
        Hostile.SetArc(w.hook, to, width * 0.5f, 0f, 330f);
        return w.gameObject;
    }

    // 공허 눈알: 부채꼴 전체는 옅게, 광선이 시작하는 변은 밝게, 채움은 시작 변 → 끝 변으로 (훑는 방향이 보이게)
    // 1.5.9~ hitWidth > 0 이면 테두리를 따라 판정 폭 옅은 띠를 깔아 변 · 호 바깥으로 판정만큼 덮음 (예고 = 판정)
    public static GameObject Fan(Vector3 center, float radius, float fromDeg, float sweepDeg, float duration, Color color, EnermyController owner, float hitWidth = 0f)
    {
        AbyssWarn w = Make("WarnFan", center, Kind.Fan, duration, color, owner);
        color = w.color;        // 1.6.0~ 종류별 적대 색 · 색각 모드 색으로 처음부터 그림 (예전엔 첫 프레임만 받은 색 그대로였음)
        w.radius = radius;
        w.fromDeg = fromDeg;
        w.sweepDeg = sweepDeg;
        w.bgMesh = new Mesh { name = "WarnFanBg" };
        w.fillMesh = new Mesh { name = "WarnFanFill" };
        w.bgRenderer = w.MeshChild("Bg", w.bgMesh, 0);
        w.fillRenderer = w.MeshChild("Fill", w.fillMesh, 1);
        BuildFan(w.bgMesh, radius, fromDeg, fromDeg + sweepDeg, new Color(color.r, color.g, color.b, 0.16f));
        BuildFan(w.fillMesh, radius, fromDeg, fromDeg, new Color(color.r, color.g, color.b, 0.4f));

        // 테두리: 중심 → 호 → 중심
        w.fanEdge = w.Line("Edge", color, 0.14f, 18);
        int seg = Mathf.Max(4, Mathf.CeilToInt(Mathf.Abs(sweepDeg) / 6f));
        Vector3[] buf = Hostile.PointBuffer(seg + 3);
        buf[0] = center;
        for (int i = 0; i <= seg; i++)
        {
            float ang = (fromDeg + sweepDeg * i / seg) * Mathf.Deg2Rad;
            buf[i + 1] = center + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
        }
        buf[seg + 2] = center;
        w.fanEdge.positionCount = seg + 3;
        w.fanEdge.SetPositions(buf);
        if (hitWidth > 0f)
        {
            w.fanBand = w.Line("Band", new Color(color.r, color.g, color.b, 0.1f), hitWidth, 0);
            w.fanBand.positionCount = seg + 3;
            w.fanBand.SetPositions(buf);
        }

        float s = fromDeg * Mathf.Deg2Rad;
        w.startEdge = w.Line("Start", Color.white, 0.3f, 19);
        w.startEdge.positionCount = 2;
        w.startEdge.SetPosition(0, center);
        w.startEdge.SetPosition(1, center + new Vector3(Mathf.Cos(s), Mathf.Sin(s)) * radius);
        return w.gameObject;
    }

    MeshRenderer MeshChild(string name, Mesh mesh, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = PixelLine.Cone;
        mr.sortingLayerName = VisualRules.WarnLayer;
        mr.sortingOrder = order;
        return mr;
    }

    const int FanSeg = 12;
    static readonly Vector3[] fanV = new Vector3[FanSeg + 2];
    static readonly Vector2[] fanUv = new Vector2[FanSeg + 2];
    static readonly Color[] fanC = new Color[FanSeg + 2];
    static int[] fanTri;

    // 중심(0,0)에서 반지름 r · from → to 도 부채꼴 (uv 가로 = 반지름 방향 · fx_ind_cone 동심 띠)
    static void BuildFan(Mesh m, float r, float from, float to, Color c)
    {
        if (fanTri == null)
        {
            fanTri = new int[FanSeg * 3];
            for (int i = 0; i < FanSeg; i++) { fanTri[i * 3] = 0; fanTri[i * 3 + 1] = i + 2; fanTri[i * 3 + 2] = i + 1; }
        }
        fanV[0] = Vector3.zero;
        fanUv[0] = new Vector2(0f, 0.5f);
        for (int i = 0; i <= FanSeg; i++)
        {
            float ang = Mathf.Lerp(from, to, i / (float)FanSeg) * Mathf.Deg2Rad;
            fanV[i + 1] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            fanUv[i + 1] = new Vector2(1f, i / (float)FanSeg);
        }
        for (int i = 0; i < fanC.Length; i++) fanC[i] = c;
        m.vertices = fanV;
        m.uv = fanUv;
        m.colors = fanC;
        if (m.GetIndexCount(0) == 0) m.triangles = fanTri;
        m.RecalculateBounds();
    }

    // 영혼 수확자: 첫 베기(안쪽 원)는 차오르는 원판, 둘째 베기(바깥 고리)는 처음부터 점선으로 옅게 → 차례가 오면 진해짐
    public static GameObject Reap(Vector3 center, float r1, float r2, float first, float second, Color color, EnermyController owner)
    {
        AbyssWarn w = Make("WarnReap", center, Kind.Reap, first, color, owner);
        color = w.color;        // 1.6.0~ 종류별 적대 색 · 색각 모드 색으로 처음부터 그림 (예전엔 첫 프레임만 받은 색 그대로였음)
        w.duration2 = Mathf.Max(0.05f, second);
        w.r1 = r1;
        w.r2 = r2;
        w.ringBand = w.Line("Band", new Color(color.r, color.g, color.b, 0.08f), r2 - r1, 0);
        w.ringBand.loop = true;
        Hostile.SetArc(w.ringBand, center, (r1 + r2) * 0.5f, 0f, 354f);
        w.outerRing = w.Line("Outer", new Color(color.r, color.g, color.b, 0.5f), 0.14f, 18, true);
        Hostile.SetArc(w.outerRing, center, r2, 0f, 360f);
        w.innerRing = w.Line("Inner", color, 0.2f, 18);
        w.innerRing.loop = true;
        Hostile.SetArc(w.innerRing, center, r1, 0f, 354f);
        Sprite disc = PixelLine.Disc != null ? PixelLine.Disc : Hostile.Glow;
        if (disc != null)
        {
            w.innerFillScale = disc == Hostile.Glow ? r1 * 2f / 8f : PixelLine.DiscScale(disc, r1 * 2f);
            GameObject d = SpecialAbilities.MakeSprite("Fill", disc, center, 0.01f, new Color(color.r, color.g, color.b, 0.45f), VisualRules.WarnLayer, 1);
            d.transform.SetParent(w.transform, true);
            w.innerFill = d.GetComponent<SpriteRenderer>();
        }
        return w.gameObject;
    }

    // 심연 거상: 갈래마다 지그재그 금이 거상 발밑에서 끝까지 갈라져 나감 + 판정 폭의 옅은 띠
    public static GameObject Crack(Vector3 center, Vector2[] dirs, float from, float to, float width, float duration, Color color, EnermyController owner)
    {
        AbyssWarn w = Make("WarnCrack", center, Kind.Crack, duration, color, owner);
        color = w.color;        // 1.6.0~ 종류별 적대 색 · 색각 모드 색으로 처음부터 그림 (예전엔 첫 프레임만 받은 색 그대로였음)
        int n = dirs.Length;
        w.cracks = new Vector3[n][];
        w.crackLines = new LineRenderer[n];
        w.crackBands = new LineRenderer[n];
        int pts = Mathf.Max(3, Mathf.CeilToInt((to - from) / 0.8f) + 1);
        for (int k = 0; k < n; k++)
        {
            Vector3 d = dirs[k];
            Vector3 side = new Vector3(-d.y, d.x);
            Vector3[] p = new Vector3[pts];
            for (int i = 0; i < pts; i++)
            {
                float jitter = i == 0 || i == pts - 1 ? 0f : (i % 2 == 0 ? 1f : -1f) * Random.Range(0.12f, 0.32f);
                p[i] = center + d * Mathf.Lerp(from, to, i / (float)(pts - 1)) + side * jitter;
            }
            w.cracks[k] = p;
            LineRenderer band = w.Line("Band", new Color(color.r, color.g, color.b, 0.1f), width, 0);
            band.positionCount = 2;
            band.SetPosition(0, center + d * from);
            band.SetPosition(1, center + d * to);
            w.crackBands[k] = band;
            LineRenderer line = w.Line("Crack", color, 0.22f, 18);
            line.positionCount = 0;
            w.crackLines[k] = line;
        }
        return w.gameObject;
    }

    // ---------------------------------------------------------------- 매 프레임
    void Update()
    {
        if (owner == null || owner.IsDead) { Destroy(gameObject); return; }
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / duration);
        blinkPhase += Time.deltaTime * (4f + k * 14f);
        float blink = 0.65f + 0.35f * Mathf.PingPong(blinkPhase, 1f);
        Color c = new Color(color.r, color.g, color.b, color.a * blink);

        switch (kind)
        {
            case Kind.Chain:
            {
                edgeL.startColor = edgeL.endColor = edgeR.startColor = edgeR.endColor = c;
                hook.startColor = hook.endColor = c;
                if (k > 0.02f) SetDashed(chain, a, Vector3.Lerp(a, b, k), 0.45f);
                chain.startColor = chain.endColor = new Color(0.9f, 0.92f, 1f, 0.5f + 0.4f * k);
                break;
            }
            case Kind.Fan:
            {
                BuildFan(fillMesh, radius, fromDeg, fromDeg + sweepDeg * k, new Color(color.r, color.g, color.b, 0.22f + 0.2f * k * blink));
                fanEdge.startColor = fanEdge.endColor = c;
                if (fanBand != null) fanBand.startColor = fanBand.endColor = new Color(color.r, color.g, color.b, 0.08f + 0.12f * k);
                startEdge.startColor = startEdge.endColor = new Color(1f, 1f, 1f, 0.45f + 0.5f * blink * k);
                break;
            }
            case Kind.Reap:
            {
                if (t < duration)
                {
                    innerRing.startColor = innerRing.endColor = c;
                    if (innerFill != null) innerFill.transform.localScale = Vector3.one * innerFillScale * k;
                }
                else
                {
                    // 둘째 베기 차례: 안쪽은 안전 → 원판을 끄고 고리를 진하게
                    float k2 = Mathf.Clamp01((t - duration) / duration2);
                    if (innerFill != null && innerFill.enabled) innerFill.enabled = false;
                    if (outerRing.sharedMaterial != PixelLine.Stroke) PixelLine.Apply(outerRing);       // 점선 → 실선
                    outerRing.startColor = outerRing.endColor = c;
                    innerRing.startColor = innerRing.endColor = c;
                    ringBand.startColor = ringBand.endColor = new Color(color.r, color.g, color.b, 0.12f + 0.3f * k2);
                    if (k2 >= 1f) { Destroy(gameObject); return; }
                }
                return;
            }
            case Kind.Crack:
            {
                for (int i = 0; i < cracks.Length; i++)
                {
                    Vector3[] p = cracks[i];
                    float reach = k * (p.Length - 1);
                    int whole = Mathf.Min(p.Length - 1, Mathf.FloorToInt(reach));
                    int count = whole + 2 > p.Length ? p.Length : whole + 2;
                    LineRenderer lr = crackLines[i];
                    if (lr.positionCount != count) lr.positionCount = count;
                    for (int j = 0; j <= whole; j++) lr.SetPosition(j, p[j]);
                    if (count > whole + 1) lr.SetPosition(whole + 1, Vector3.Lerp(p[whole], p[whole + 1], reach - whole));
                    lr.startColor = lr.endColor = c;
                    crackBands[i].startColor = crackBands[i].endColor = new Color(color.r, color.g, color.b, 0.08f + 0.12f * k);
                }
                break;
            }
        }
        if (t >= duration) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (bgMesh != null) Destroy(bgMesh);
        if (fillMesh != null) Destroy(fillMesh);
    }
}
