// 1.6.1~ 로딩 화면 오른쪽 아래 영혼불 스피너 (SceneFlow) — 움직임을 CPU Update 대신 셰이더 _Time 으로 돌림
//   정점에서 UV 를 _Time.y × _Speed 만큼 돌리고, 조각에서 도트 격자(_Cells 칸)로 끊은 고리 위에 꼬리가 사라지는 청록 불꽃 · 가운데 불씨가 맥동
//   색은 정점 색(Image.color) · 그림 없음 (코드로 그림) · UI 기본 셰이더처럼 스텐실 · ColorMask · 마스크 사각형 자르기 지원
Shader "UI/SoulSpinner"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Speed ("Turns Per Second", Float) = 0.9
        _Cells ("Pixel Cells", Float) = 16

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;     // 돌리기 전 (도트 격자 · 거리)
                float2 spin     : TEXCOORD1;     // 돌린 뒤 (각도)
                float4 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float _Speed;
            float _Cells;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.uv = v.texcoord;
                // UV 를 가운데 기준으로 시간만큼 돌림 (GPU 가 매 프레임 그릴 때마다 — 스크립트가 값을 바꿀 필요 없음)
                float a = _Time.y * _Speed * 6.2831853;
                float s = sin(a), c = cos(a);
                float2 p = v.texcoord - 0.5;
                OUT.spin = float2(p.x * c - p.y * s, p.x * s + p.y * c);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 도트 격자: 칸 가운데 값으로 계산 (게임 그림체처럼 각진 점)
                float2 cell = (floor(IN.uv * _Cells) + 0.5) / _Cells - 0.5;
                float r = length(cell) * 2.0;                       // 0 가운데 ~ 1 가장자리
                // 돌린 좌표계에서 같은 칸의 각도 (칸 가운데를 돌림 — 점이 격자에 붙은 채 꼬리만 돎)
                float a = _Time.y * _Speed * 6.2831853;
                float s = sin(a), c = cos(a);
                float2 q = float2(cell.x * c - cell.y * s, cell.x * s + cell.y * c);
                float ang = atan2(q.y, q.x) / 6.2831853 + 0.5;      // 0 ~ 1
                float tail = pow(saturate(ang), 2.2);                // 머리(1)에서 꼬리(0)로 흐려짐
                float ring = step(0.56, r) * step(r, 0.94);
                float pulse = 0.5 + 0.5 * sin(_Time.y * 4.0);
                float core = step(r, 0.22 + 0.08 * pulse) * (0.55 + 0.45 * pulse);   // 가운데 불씨 (맥동)
                float alpha = max(ring * (0.12 + 0.88 * tail), core);
                // 머리 쪽은 거의 흰빛 (영혼불의 밝은 속)
                float3 rgb = lerp(IN.color.rgb, float3(1, 1, 1), ring * smoothstep(0.85, 1.0, ang) * 0.6);
                fixed4 col = fixed4(rgb, IN.color.a * alpha);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif
                return col;
            }
        ENDCG
        }
    }
}
