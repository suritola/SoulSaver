using UnityEngine;

// 1.5.1~ 범위 · 예고 표시를 도트 그림체로 — 코드로 그리는 선(LineRenderer) · 원판 · 띠가 같이 쓰는 도트 재질 · 그림
//   그림은 Resources/FX/fx_ind_* (tools/pixelart/make_indicators.py), 흰색 기반이라 선 · 그림 색을 곱해 씀
//   재질 · 그림은 한 번만 만들어 모든 선이 같이 씀 (선마다 복사하지 않음 · 묶어 그리기 그대로)
public static class PixelLine
{
    // 도트 획 텍스처는 두께 방향 8줄 중 바깥 1줄씩이 어두운 외곽 → 두께를 8/6 배로 키워 색 부분 두께는 예전 그대로
    //   (원 · 고리는 판정 반지름 위에 가운데를 맞춰 그리므로 판정 원은 그대로, 바깥 외곽만 더해짐)
    public const float Widen = 8f / 6f;

    static Material stroke, dash, cone;

    // 선 · 고리용 도트 획 재질 (실선)
    public static Material Stroke
    {
        get
        {
            if (stroke == null) stroke = Make("fx_ind_stroke");
            return stroke;
        }
    }

    // 점선 획 재질 (선 마디마다 획 한 칸 · RepeatPerSegment)
    public static Material Dash
    {
        get
        {
            if (dash == null) dash = Make("fx_ind_dash");
            return dash;
        }
    }

    // 부채꼴 채움 메시 재질 (가로 = 반지름 방향 동심 띠)
    public static Material Cone
    {
        get
        {
            if (cone == null) cone = Make("fx_ind_cone");
            return cone;
        }
    }

    static Material Make(string tex)
    {
        // 장면 전환의 사용 안 하는 에셋 정리가 정적 캐시를 지우지 않게 (지워지면 선이 분홍색이 됨)
        Material m = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.DontUnloadUnusedAsset };
        Texture2D t = Resources.Load<Texture2D>("FX/" + tex);
        if (t != null) m.mainTexture = t;       // 그림이 없으면 예전 단색 선 그대로
        return m;
    }

    // 선 하나를 도트 획으로 (재질 · 늘이기 방식 · 두께 보정). 두께는 startWidth · endWidth 그대로 쓰고 배율만 곱함
    public static void Apply(LineRenderer lr, bool dashed = false)
    {
        if (lr == null) return;
        lr.sharedMaterial = dashed ? Dash : Stroke;
        lr.textureMode = dashed ? LineTextureMode.RepeatPerSegment : LineTextureMode.Stretch;
        lr.widthMultiplier = 1f;            // 두께 × Widen · 최소 두께는 PixelStrokeMin 이 두께 값으로 직접 (배율은 startWidth 를 넣을 때 나눠져 효과 없음)
        if (lr.GetComponent<PixelStrokeMin>() == null) lr.gameObject.AddComponent<PixelStrokeMin>();
    }

    // 1.5.0~ 가는 선(고리 · 외곽선 두께 0.06~0.2)은 화면에서 3~4 화소라 도트 획 8줄이 뭉개져 그냥 가는 선으로 보였음
    //   → 색 부분 두께를 최소 MinStroke 로 (PixelStrokeMin). 넓은 선(돌진 경로 1.4 등 범위 폭)은 색 부분 그대로 · 외곽만 더해짐
    public const float MinStroke = 0.24f;

    // ---------------------------------------------------------------- 원판 · 띠 그림
    static Sprite disc, floor, band;
    static bool loaded;

    static void Load()
    {
        if (loaded && disc != null) return;
        loaded = true;
        disc = First("fx_ind_disc");
        floor = First("fx_ind_floor");
        band = First("fx_ind_band");
    }

    static Sprite First(string name)
    {
        Sprite[] f = Fx.Frames(name);
        return f != null && f.Length > 0 ? f[0] : null;
    }

    // 예고 원 채움 원판 (가장자리 = 판정 원)
    public static Sprite Disc { get { Load(); return disc; } }
    // 바닥 범위 원판 (체크 디더 + 테두리, 가장자리 = 판정 원)
    public static Sprite Floor { get { Load(); return floor; } }
    // 직선 예고 띠 타일 (SpriteRenderer Tiled)
    public static Sprite Band { get { Load(); return band; } }

    // 원판 그림을 지름 diameter 로 그릴 때의 크기 배율 (그림 너비 = 지름)
    public static float DiscScale(Sprite s, float diameter)
    {
        float w = s != null ? s.bounds.size.x : 0f;
        return w > 0f ? diameter / w : 0f;
    }
}
