// 도트 스프라이트 윤곽선 (1.8.7~): 투명한 칸 중 옆 칸이 칠해진 곳을 윤곽선 색으로 칠함
// Sprites/Default 와 같은 방식(정점 색 · 반전 · 사전 곱 알파)이라 피격 번쩍임 · 투명도 효과는 그대로
Shader "SoulSaver/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline", Color) = (0.06,0.04,0.09,0.9)
        _Saturation ("Saturation", Float) = 1
        _Brightness ("Brightness", Float) = 1
        _EmptyAlpha ("Empty below alpha", Float) = 0.5
        _SkipDark ("Skip outline beside dark", Float) = 0         // 1.6.0~ 붙은 그림 칸이 이미 이보다 어두우면 외곽 생략 (0 = 예전 그대로)
        [PerRendererData] _Flash ("Hit flash", Float) = 0       // 1.1~ 피격 흰 번쩍임 (Juice.SetFlash 가 렌더러마다)
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
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _Saturation, _Brightness, _EmptyAlpha, _SkipDark;
            float _Flash;

            // 1.2.4: 옆 칸 4개의 좌표를 정점에서 미리 계산 (픽셀마다 좌표 계산 뒤 읽던 종속 텍스처 읽기 → 바로 읽기 · 같은 칸을 읽으므로 결과 같음)
            struct v2fOutline
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 nearX    : TEXCOORD1;     // xy = 오른쪽 칸, zw = 왼쪽 칸
                float4 nearY    : TEXCOORD2;     // xy = 위 칸, zw = 아래 칸
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2fOutline Vert (appdata_t IN)
            {
                v2f b = SpriteVert(IN);         // Sprites/Default 와 같은 정점 처리 (반전 · 색 · 픽셀 스냅)
                v2fOutline o;
                o.vertex = b.vertex;
                o.color = b.color;
                o.texcoord = b.texcoord;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(b, o);
                float2 t = _MainTex_TexelSize.xy;
                o.nearX = float4(b.texcoord + float2(t.x, 0), b.texcoord - float2(t.x, 0));
                o.nearY = float4(b.texcoord + float2(0, t.y), b.texcoord - float2(0, t.y));
                return o;
            }

            fixed4 Frag (v2fOutline IN) : SV_Target
            {
                // 판정은 그림 자체의 알파로 (무적 깜빡임처럼 반투명해져도 몸이 윤곽선 색으로 바뀌지 않게)
                fixed4 tex = SampleSpriteTexture(IN.texcoord);
                fixed4 c = tex * IN.color;
                // 빈칸 판정: 캐릭터는 0.5, 이펙트는 0.05 (반투명한 빛 번짐이 검은 테두리로 바뀌지 않게)
                if (tex.a < _EmptyAlpha)
                {
                    fixed4 nr = SampleSpriteTexture(IN.nearX.xy), nl = SampleSpriteTexture(IN.nearX.zw);
                    fixed4 nu = SampleSpriteTexture(IN.nearY.xy), nd = SampleSpriteTexture(IN.nearY.zw);
                    float a = max(max(nr.a, nl.a), max(nu.a, nd.a));
                    // 1.6.0~ 붙은 불투명 칸(a > 0.5) 중 가장 밝은 흑백 값 — 그림에 이미 어두운 외곽이 있으면 그 자리는 셰이더 외곽을 생략(이중 외곽 → 1칸)
                    //   같은 4칸을 다시 쓰므로 텍스처 읽기 추가 없음 · 키워드 없이 재질 값으로만 (_SkipDark 0 이면 늘 그림 = 예전 그대로)
                    //   피격 흰 번쩍임(_Flash) 동안은 그림의 어두운 외곽도 하얘지므로 생략하지 않음 (흰 몸 둘레에 외곽이 남게)
                    const float3 lw = float3(0.299, 0.587, 0.114);
                    float lit = max(max(nr.a > 0.5 ? dot(nr.rgb, lw) : 0, nl.a > 0.5 ? dot(nl.rgb, lw) : 0),
                                    max(nu.a > 0.5 ? dot(nu.rgb, lw) : 0, nd.a > 0.5 ? dot(nd.rgb, lw) : 0));
                    if (a > 0.5 && lit >= _SkipDark * (1 - _Flash))
                    {
                        fixed4 o = _OutlineColor;
                        o.a *= IN.color.a;
                        o.rgb *= o.a;
                        return o;
                    }
                }
                // 채도 · 밝기 (이펙트를 더 선명하게, 캐릭터는 1 · 1 그대로)
                float l = dot(c.rgb, float3(0.299, 0.587, 0.114));
                c.rgb = saturate(lerp(l.xxx, c.rgb, _Saturation) * _Brightness);
                c.rgb = lerp(c.rgb, float3(1, 1, 1), _Flash);      // 1.1~ 피격 흰 번쩍임 (윤곽선은 그대로)
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
