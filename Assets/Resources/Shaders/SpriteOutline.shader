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
            #pragma vertex SpriteVert
            #pragma fragment Frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _Saturation, _Brightness, _EmptyAlpha;
            float _Flash;

            fixed4 Frag (v2f IN) : SV_Target
            {
                // 판정은 그림 자체의 알파로 (무적 깜빡임처럼 반투명해져도 몸이 윤곽선 색으로 바뀌지 않게)
                fixed4 tex = SampleSpriteTexture(IN.texcoord);
                fixed4 c = tex * IN.color;
                // 빈칸 판정: 캐릭터는 0.5, 이펙트는 0.05 (반투명한 빛 번짐이 검은 테두리로 바뀌지 않게)
                if (tex.a < _EmptyAlpha)
                {
                    float2 t = _MainTex_TexelSize.xy;
                    float a = max(max(SampleSpriteTexture(IN.texcoord + float2(t.x, 0)).a, SampleSpriteTexture(IN.texcoord - float2(t.x, 0)).a),
                                  max(SampleSpriteTexture(IN.texcoord + float2(0, t.y)).a, SampleSpriteTexture(IN.texcoord - float2(0, t.y)).a));
                    if (a > 0.5)
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
