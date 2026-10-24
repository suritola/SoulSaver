using UnityEngine;

// 1.5.0~ 도트 획 보정 (PixelLine.Apply 가 붙임)
//   ① 두께: LineRenderer 는 startWidth 를 넣을 때 widthMultiplier 로 나눠 저장하므로 배율로는 두께가 안 바뀜 → 두께 값을 직접 고침
//      · 도트 획은 바깥 1줄씩이 어두운 외곽 → 두께 × Widen (색 부분 = 코드가 정한 두께)
//      · 가는 선(외곽선 등)은 화면 3~4 화소라 획이 뭉개짐 → 색 부분 최소 MinStroke
//      코드가 두께를 다시 넣으면(모으기 · 깜빡임) 그 값을 새 기준으로 삼고, 내가 넣은 값이면 기준을 그대로 둠 (매 프레임 커지지 않게)
//   ② 고리: 한 바퀴 닫힌 원(범위 · 조준 · 예고 고리)은 매끈한 선 대신 도트 문양 그림(PixelRingArt)으로 그림
//      선은 그대로 두고 그리기만 끔(forceRenderingOff) → 선을 만드는 코드 · 판정은 손대지 않음
//   선을 LateUpdate 에서 그리는 코드(CharacterKit 등)보다 뒤에 돌도록 실행 순서를 맨 뒤로
[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)]
public class PixelStrokeMin : MonoBehaviour
{
    LineRenderer lr;
    float baseStart, baseEnd, wroteStart = -1f, wroteEnd = -1f;
    SpriteRenderer ring;
    float charge = -1f;
    int chargeFrame = -1;

    static readonly Vector3[] buf = new Vector3[512];
    public const float MaxRingRadius = 30f;     // 이보다 큰 원은 도트 고리 그림으로 바꾸지 않음 (그림 크기 상한)
    // 단정한 테로 그릴 고리 (여러 개가 늘 떠 있음 · 퍼져 나감 · 바닥 영역 테두리)
    static readonly System.Collections.Generic.HashSet<string> Plain = new System.Collections.Generic.HashSet<string>
        { "PickupRing", "Ring", "SarcoZone", "PossessMistEdge", "StallNearRing" };
    // 1.5.0~ 영웅 범위표시(우클릭 필살기): 이름 목록 대신 그리는 코드가 매 프레임 HeroAim.Mark 로 직접 알려 줌
    //   지정된 선만 맨 위 · 영웅 문양 고리 · 움직이는 장식 (상시 · 평타 표시는 맨 위에서 뺌) · 지정이 끊기면 원래 정렬 · 재질로
    internal HeroAimKind markKind;
    internal HeroAimFx markFx;
    internal int markFrame = -10;
    internal System.Collections.Generic.List<Transform> markTargets;
    internal int markCount, markMax;
    internal float markHalf, markGold;
    bool onTop;
    Material origMat;
    int origLayer, origOrder;
    HeroAimDecor decor;
    bool plain, under;

    bool Marked => markFrame >= Time.frameCount - 1 && markKind != HeroAimKind.None;

    // ---------------------------------------------------------------- 맨 위 그리기 (모든 반투명 그림 뒤 · 가장 위 정렬 층 · 카메라 바로 앞)
    const int TopQueue = 4000;
    public const int TopOrder = 32000;         // 양피지 -1 · 선 · 고리 0 · 장식 · 상징 +1
    static Material topSprite, topStroke, topDash;
    static int topLayer = int.MinValue;

    static Material TopSprite
    {
        get
        {
            if (topSprite == null)
                topSprite = new Material(Shader.Find("Sprites/Default")) { renderQueue = TopQueue, hideFlags = HideFlags.DontUnloadUnusedAsset };
            return topSprite;
        }
    }

    internal static Material TopSpriteMaterial => TopSprite;

    static Material TopDash
    {
        get
        {
            if (topDash == null)
                topDash = new Material(PixelLine.Dash) { renderQueue = TopQueue, hideFlags = HideFlags.DontUnloadUnusedAsset };
            return topDash;
        }
    }

    static Material TopStroke
    {
        get
        {
            if (topStroke == null)
                topStroke = new Material(PixelLine.Stroke) { renderQueue = TopQueue, hideFlags = HideFlags.DontUnloadUnusedAsset };
            return topStroke;
        }
    }

    static int TopLayer
    {
        get
        {
            if (topLayer == int.MinValue)
            {
                SortingLayer[] layers = SortingLayer.layers;
                topLayer = layers.Length > 0 ? layers[layers.Length - 1].id : 0;
            }
            return topLayer;
        }
    }

    public static void PutOnTop(Renderer r, int offset = 0)
    {
        if (r.sortingLayerID != TopLayer) r.sortingLayerID = TopLayer;
        if (r.sortingOrder != TopOrder + offset) r.sortingOrder = TopOrder + offset;
    }

    // 맨 위 그림의 높이(z): 카메라 바로 앞 → 바닥 · 벽 등 어떤 그림의 깊이에도 가리지 않음
    public static float TopZ(float fallback)
    {
        Camera cam = Camera.main;
        return cam != null ? cam.transform.position.z + cam.nearClipPlane + 0.5f : fallback;
    }

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        plain = Plain.Contains(gameObject.name);
        under = gameObject.name == "Under";     // 예고 원 밑 검은 바탕 고리: 문양 고리에 어두운 외곽이 있어 원이면 그리지 않음
        heroTop = HeroTop.Contains(gameObject.name);
        closingLine = gameObject.name == "Closing";
    }

    // 1.6.0~ 가독성 개편 C6: 적 예고의 닫히는 흰 고리(Telegraph 「Closing」)는 도트 고리 그림으로 바꾸지 않고 획 그대로 —
    //   고리 그림의 테는 0.82 회색(흑백 Y 209)이라 흰색을 곱해도 플레이어 흰 띠(Y 250)보다 어두웠음. 획은 가운데 4줄이 흰 심(255)
    //   (「적 위험 흰 요소가 화면에서 가장 밝다」 · QA read_* 흰고리 210 고정) · Legacy 면 예전처럼 고리 그림
    bool closingLine;

    // 우클릭이 아닌 영웅 표시(궁수 시위 · 유령 냉기 표식 · 시계공 발밑 문자판 · 지도 제작자 도형 미리보기 · 행군 경로)도 맨 위 (ff03c4d 때처럼 바닥 · 적에 가리지 않게)
    //   장식 · 영웅 문양은 붙이지 않음 (HeroAim.Mark 로 지정된 우클릭 범위표시만)
    static readonly System.Collections.Generic.HashSet<string> HeroTop = new System.Collections.Generic.HashSet<string>
        { "BowDraw", "ChillMark", "ClockDial", "ClockRing", "MapClose", "MarchTrail" };
    bool heroTop;

    // 모으는 스킬(검사 회전 베기 · 연금술사 대폭발)이 매 프레임 알려 줌 → 바깥 테가 모은 만큼 차오름. 안 알려 주면 다 찬 모습
    public static void SetCharge(LineRenderer lr, float charge)
    {
        if (lr == null) return;
        PixelStrokeMin m = lr.GetComponent<PixelStrokeMin>();
        if (m == null) return;
        m.charge = Mathf.Clamp01(charge);
        m.chargeFrame = Time.frameCount;
    }

    void LateUpdate()
    {
        if (lr == null) return;
        if (!lr.enabled) { HideRing(); if (decor != null) decor.Hide(); return; }
        float s = lr.startWidth, e = lr.endWidth;
        if (!Mathf.Approximately(s, wroteStart) || !Mathf.Approximately(e, wroteEnd)) { baseStart = s; baseEnd = e; }
        float w = Mathf.Max(baseStart, baseEnd);
        float k = PixelLine.Widen * (w > 0.0001f && w < PixelLine.MinStroke ? PixelLine.MinStroke / w : 1f);
        wroteStart = baseStart * k;
        wroteEnd = baseEnd * k;
        if (!Mathf.Approximately(s, wroteStart)) lr.startWidth = wroteStart;
        if (!Mathf.Approximately(e, wroteEnd)) lr.endWidth = wroteEnd;

        // 영웅 범위표시는 맨 위 (선을 만든 코드가 정렬을 다시 넣어도 매 프레임 덮어씀) · 지정이 끊기면 원래대로
        bool marked = Marked;
        if (marked || heroTop) GoTop();
        else if (onTop) LeaveTop();

        Vector3 center = Vector3.zero; float radius = 0f;
        bool circle = !(closingLine && !VisualRules.Legacy) && TryCircle(out center, out radius);
        if (circle)
        {
            if (under) { if (ring != null && ring.enabled) ring.enabled = false; lr.forceRenderingOff = true; }
            else ShowRing(center, radius, marked ? (int)markKind : 0);
        }
        else HideRing();

        // 움직이는 장식 · 가운데 상징 (지정된 선만 · 꺼지거나 지정이 끊기면 같은 프레임에 숨김)
        if (marked && markFx != HeroAimFx.None && !under)
        {
            if (decor == null) decor = new HeroAimDecor(transform);
            decor.Begin();
            try
            {
                if (circle) decor.Ring(markKind, markFx, center, radius, chargeFrame >= Time.frameCount - 1 ? charge : -1f, this);
                else if (lr.positionCount >= 2)
                {
                    Vector3 a = lr.GetPosition(0), b = lr.GetPosition(lr.positionCount - 1);
                    if (!lr.useWorldSpace) { a = transform.TransformPoint(a); b = transform.TransformPoint(b); }
                    decor.Line(markKind, markFx, a, b, this);
                }
            }
            finally { decor.End(); }
        }
        else if (decor != null) decor.Hide();
    }

    void GoTop()
    {
        if (!onTop)
        {
            onTop = true;
            origMat = lr.sharedMaterial;
            origLayer = lr.sortingLayerID;
            origOrder = lr.sortingOrder;
        }
        Material m = lr.sharedMaterial;
        if (m == PixelLine.Stroke) lr.sharedMaterial = TopStroke;
        else if (m == PixelLine.Dash) lr.sharedMaterial = TopDash;
        PutOnTop(lr);
    }

    void LeaveTop()
    {
        onTop = false;
        Material m = lr.sharedMaterial;
        if (origMat != null && (m == topStroke || m == topDash)) lr.sharedMaterial = origMat;
        lr.sortingLayerID = origLayer;
        lr.sortingOrder = origOrder;
    }

    // 실선 획이고, 점들이 한 바퀴 원 위에 있으면 중심 · 반지름
    bool TryCircle(out Vector3 center, out float radius)
    {
        center = Vector3.zero; radius = 0f;
        int count = lr.positionCount;
        if (count < 16 || count > buf.Length) return false;
        Material m = lr.sharedMaterial;
        if (m != PixelLine.Stroke && m != topStroke) return false;
        if (!lr.loop && (lr.GetPosition(0) - lr.GetPosition(count - 1)).sqrMagnitude >= 0.0004f) return false;     // 닫히지 않은 선은 점 복사 전에 거름
        lr.GetPositions(buf);
        if (!lr.useWorldSpace)
            for (int i = 0; i < count; i++) buf[i] = lr.transform.TransformPoint(buf[i]);
        bool closed = lr.loop || (buf[0] - buf[count - 1]).sqrMagnitude < 0.0004f;
        if (!closed) return false;
        int pts = lr.loop ? count : count - 1;
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < pts; i++) sum += buf[i];
        center = sum / pts;
        for (int i = 0; i < pts; i++) radius += Vector2.Distance(buf[i], center);
        radius /= pts;
        if (radius < 0.3f || radius > MaxRingRadius) return false;      // 너무 큰 원은 선 그대로 (1.5.0 거너 사거리 500 → 7016² 그림 · 1GB 로 멈추고 튕긴 사고)
        float tol = 0.06f * radius + 0.02f;
        for (int i = 0; i < pts; i++)
            if (Mathf.Abs(Vector2.Distance(buf[i], center) - radius) > tol) return false;
        return true;
    }

    void ShowRing(Vector3 center, float radius, int style)
    {
        if (ring == null)
        {
            GameObject go = new GameObject("PixelRing");
            go.transform.SetParent(transform, false);
            ring = go.AddComponent<SpriteRenderer>();
            ring.sharedMaterial = TopSprite;        // 재질을 직접 지정 (기본 재질에 기대지 않음) · 맨 위 그리기 큐
        }
        bool charged = chargeFrame >= Time.frameCount - 1;
        ring.sprite = PixelRingArt.Get(radius, charged ? charge : -1f, plain, style);
        ring.color = lr.startColor;
        PutOnTop(ring);                             // 범위 고리는 무조건 맨 위
        Transform t = ring.transform;
        // 높이(z)는 카메라 바로 앞 → 바닥 · 벽 등 어떤 그림의 깊이에도 가리지 않음
        t.position = new Vector3(center.x, center.y, TopZ(center.z));
        t.rotation = Quaternion.identity;
        Vector3 ls = transform.lossyScale;
        t.localScale = new Vector3(ls.x != 0f ? 1f / ls.x : 1f, ls.y != 0f ? 1f / ls.y : 1f, 1f);
        if (!ring.enabled) ring.enabled = true;
        lr.forceRenderingOff = true;
    }

    void HideRing()
    {
        if (ring != null && ring.enabled) ring.enabled = false;
        if (lr != null && lr.forceRenderingOff) lr.forceRenderingOff = false;
    }

    void OnDisable()
    {
        HideRing();
        if (decor != null) decor.Hide();
        markFrame = -10;
        if (onTop && lr != null) LeaveTop();
    }
}
