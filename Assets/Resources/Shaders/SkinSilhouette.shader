// 스킨 몸 덧칠 (스킨 시청각 개편 1단계): 몸 그림의 알파 모양 그대로 정점 색 한 가지로 칠함 — 실루엣 안쪽만 (SkinBody · 사망 굳음)
// Sprites/Default 와 같은 정점 처리(반전 · 색 · 사전 곱 알파)
Shader "SoulSaver/SkinSilhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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

            fixed4 Frag (v2f IN) : SV_Target
            {
                fixed a = SampleSpriteTexture(IN.texcoord).a;
                fixed4 c = IN.color;
                c.a *= a;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
