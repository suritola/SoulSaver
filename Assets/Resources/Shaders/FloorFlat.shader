// 바닥 타일맵 전용 재질 (1.6.0~ 가독성 개편 C1): 밝은 바닥을 누르고 무늬 대비를 줄여 적 · 탄이 바닥에 묻히지 않게
// Sprites/Default 와 같은 정점 처리(정점 색 · 반전 · 사전 곱 알파 · 픽셀 스냅)라 타일맵 색(FloorTone · 초원 감광 · 칸 색)은 그대로 곱해짐
// 값은 VisualRules (FloorKnee · FloorKneeRatio · FloorContrast · FloorMeanMip) — StageManager.ApplyFloorFlat 이 넣음
Shader "SoulSaver/FloorFlat"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Knee ("Knee luma", Float) = 0.18
        _KneeRatio ("Above-knee ratio", Float) = 0.2
        _Contrast ("Local contrast", Float) = 0.65
        _MeanMip ("Tile mean mip", Float) = 5
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _Knee, _KneeRatio, _Contrast, _MeanMip;

            // 밉 단계를 직접 정해 읽기 (SampleSpriteTexture 는 자동 밉이라 조준 줌으로 멀어지면 흐려짐)
            fixed4 SampleLod (float2 uv, float lod)
            {
                fixed4 c = tex2Dlod(_MainTex, float4(uv, 0, lod));
            #if ETC1_EXTERNAL_ALPHA
                fixed4 a = tex2Dlod(_AlphaTex, float4(uv, 0, lod));
                c.a = lerp(c.a, a.r, _EnableExternalAlpha);
            #endif
                return c;
            }

            // sRGB 정확한 변환 (UnityCG 의 GammaToLinearSpace 는 근사식이라 왕복하면 벽 · 밉 0 재질 색이 1~2% 틀어짐)
            float3 ToGamma (float3 x)
            {
                x = max(x, 0);
                return x <= 0.0031308 ? x * 12.92 : 1.055 * pow(x, 1.0 / 2.4) - 0.055;
            }
            float3 ToLinear (float3 x)
            {
                return x <= 0.04045 ? x / 12.92 : pow((x + 0.055) / 1.055, 2.4);
            }

            fixed4 Frag (v2f IN) : SV_Target
            {
                // 주 색 = 밉 0 고정 (밉맵을 켜도 도트가 흐려지지 않음)
                fixed4 c = SampleLod(IN.texcoord, 0) * IN.color;
                // 타일 1장 평균 = 밉 5 (160×64 타일셋의 32칸 타일 하나가 5×2 칸 중 정확히 1칸) · 밉맵이 없으면 밉 0 으로 잘려 평균 = 원본 → 대비는 그대로, 무릎만
                fixed4 m = SampleLod(IN.texcoord, _MeanMip) * IN.color;
            #if !defined(UNITY_COLORSPACE_GAMMA)
                // 1.6.0~ 이 프로젝트는 Linear 색 공간 — 값(무릎 0.18 = 화면 Y 46 · 대비)은 화면(감마) 기준이라 감마로 바꿔 계산하고 되돌림
                //   (선형 그대로면 0.18 이 화면 Y 약 118 이 되어 사막 92 에 무릎이 아예 안 걸렸음 · QA read_desert 91.4)
                c.rgb = ToGamma(c.rgb);
                m.rgb = ToGamma(m.rgb);
            #endif
                // 국소 대비: 평균 쪽으로 당김. 평균에 투명 칸이 섞인 타일(가장자리 조각)은 그만큼 덜 당김 (투명 칸 색이 평균을 검게 끌지 않게)
                c.rgb = lerp(c.rgb, lerp(m.rgb, c.rgb, _Contrast), saturate(m.a));
                // 밝은 쪽 무릎: 흑백 Y 가 _Knee 를 넘는 몫만 _KneeRatio 배 (비율로 줄여 색상 · 채도 유지)
                float l = dot(c.rgb, float3(0.299, 0.587, 0.114));
                if (l > _Knee) c.rgb *= (_Knee + (l - _Knee) * _KneeRatio) / l;
            #if !defined(UNITY_COLORSPACE_GAMMA)
                c.rgb = ToLinear(c.rgb);
            #endif
                c.rgb *= c.a;                  // 사전 곱 알파 (Sprites/Default 와 같음)
                return c;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
