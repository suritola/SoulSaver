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
    // 단정한 테로 그릴 고리 (여러 개가 늘 떠 있음 · 퍼져 나감 · 바닥 영역 테두리)
    static readonly System.Collections.Generic.HashSet<string> Plain = new System.Collections.Generic.HashSet<string>
        { "PickupRing", "Ring", "SarcoZone", "PossessMistEdge", "StallNearRing" };
    // 영웅 스킬(우클릭 · 필살기) 범위 · 조준 선: 무조건 맨 위에 그림 (바닥 · 적 · 효과에 가려 안 보였음)
    static readonly System.Collections.Generic.HashSet<string> Hero = new System.Collections.Generic.HashSet<string>
    {
        "UltRing", "UltAimRing", "UltAimLine", "RainAim", "BowDraw", "ChillMark", "ClockDial", "ClockRing", "ExpressRing",
        "FoldLine", "FoldSideA", "FoldSideB", "MapClose", "MarchTrail", "PossessRange", "RewindPath", "RewindPathCore", "RewindRing"
    };
    bool plain, under, hero;

    // ---------------------------------------------------------------- 맨 위 그리기 (모든 반투명 그림 뒤 · 가장 위 정렬 층 · 카메라 바로 앞)
    const int TopQueue = 4000;
    const int TopOrder = 32000;
    static Material topSprite, topStroke;
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

    static void PutOnTop(Renderer r)
    {
        if (r.sortingLayerID != TopLayer) r.sortingLayerID = TopLayer;
        if (r.sortingOrder != TopOrder) r.sortingOrder = TopOrder;
    }

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        plain = Plain.Contains(gameObject.name);
        hero = Hero.Contains(gameObject.name);
        under = gameObject.name == "Under";     // 예고 원 밑 검은 바탕 고리: 문양 고리에 어두운 외곽이 있어 원이면 그리지 않음
    }

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
        if (!lr.enabled) { HideRing(); return; }
        float s = lr.startWidth, e = lr.endWidth;
        if (!Mathf.Approximately(s, wroteStart) || !Mathf.Approximately(e, wroteEnd)) { baseStart = s; baseEnd = e; }
        float w = Mathf.Max(baseStart, baseEnd);
        float k = PixelLine.Widen * (w > 0.0001f && w < PixelLine.MinStroke ? PixelLine.MinStroke / w : 1f);
        wroteStart = baseStart * k;
        wroteEnd = baseEnd * k;
        if (!Mathf.Approximately(s, wroteStart)) lr.startWidth = wroteStart;
        if (!Mathf.Approximately(e, wroteEnd)) lr.endWidth = wroteEnd;

        // 영웅 스킬 선은 맨 위 (선을 만든 코드가 정렬을 다시 넣어도 매 프레임 덮어씀)
        if (hero)
        {
            if (lr.sharedMaterial == PixelLine.Stroke) lr.sharedMaterial = TopStroke;
            PutOnTop(lr);
        }

        if (TryCircle(out Vector3 center, out float radius))
        {
            if (under) { if (ring != null && ring.enabled) ring.enabled = false; lr.forceRenderingOff = true; }
            else ShowRing(center, radius);
        }
        else HideRing();
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
        if (radius < 0.3f) return false;
        float tol = 0.06f * radius + 0.02f;
        for (int i = 0; i < pts; i++)
            if (Mathf.Abs(Vector2.Distance(buf[i], center) - radius) > tol) return false;
        return true;
    }

    void ShowRing(Vector3 center, float radius)
    {
        if (ring == null)
        {
            GameObject go = new GameObject("PixelRing");
            go.transform.SetParent(transform, false);
            ring = go.AddComponent<SpriteRenderer>();
            ring.sharedMaterial = TopSprite;        // 재질을 직접 지정 (기본 재질에 기대지 않음) · 맨 위 그리기 큐
        }
        bool charged = chargeFrame >= Time.frameCount - 1;
        ring.sprite = PixelRingArt.Get(radius, charged ? charge : -1f, plain);
        ring.color = lr.startColor;
        PutOnTop(ring);                             // 범위 고리는 무조건 맨 위
        Transform t = ring.transform;
        // 높이(z)는 카메라 바로 앞 → 바닥 · 벽 등 어떤 그림의 깊이에도 가리지 않음
        Camera cam = Camera.main;
        float z = cam != null ? cam.transform.position.z + cam.nearClipPlane + 0.5f : center.z;
        t.position = new Vector3(center.x, center.y, z);
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

    void OnDisable() { HideRing(); }
}
