Shader "Universal Render Pipeline/Sprites/SeamlessOverlayMask"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _OverlayTex ("Overlay Texture", 2D) = "white" {}
        _OverlayTex2 ("Overlay Texture 2", 2D) = "white" {}
        _OverlayMaskTex ("Overlay Mask (Local Sprite UV)", 2D) = "white" {}
        _OverlayColor ("Overlay Color", Color) = (1,1,1,1)
        _OverlayColor2 ("Overlay Color 2", Color) = (1,1,1,1)
        _OverlayStrength ("Overlay Strength", Range(0, 1)) = 1
        _OverlayStrength2 ("Overlay Strength 2", Range(0, 1)) = 1
        _OverlayWorldTiling ("Overlay World Tiling (XY)", Vector) = (1,1,0,0)
        _OverlayWorldOffset ("Overlay World Offset (XY)", Vector) = (0,0,0,0)

        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float2 overlayUV  : TEXCOORD1;
                float4 color      : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_OverlayTex);
            SAMPLER(sampler_OverlayTex);

            TEXTURE2D(_OverlayTex2);
            SAMPLER(sampler_OverlayTex2);

            TEXTURE2D(_OverlayMaskTex);
            SAMPLER(sampler_OverlayMaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _RendererColor;

                half4 _OverlayColor;
                half4 _OverlayColor2;
                half _OverlayStrength;
                half _OverlayStrength2;
                float4 _OverlayWorldTiling;
                float4 _OverlayWorldOffset;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                OUT.positionCS = positionInputs.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.overlayUV = positionInputs.positionWS.xy * _OverlayWorldTiling.xy + _OverlayWorldOffset.xy;
                OUT.color = IN.color * _Color * _RendererColor;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 baseColor = baseSample * IN.color;

                float2 overlayUV = IN.overlayUV;
                half4 overlaySample1 = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, overlayUV) * _OverlayColor;
                half4 overlaySample2 = SAMPLE_TEXTURE2D(_OverlayTex2, sampler_OverlayTex, overlayUV) * _OverlayColor2;
                half overlayMask = SAMPLE_TEXTURE2D(_OverlayMaskTex, sampler_OverlayMaskTex, IN.uv).r;

                half blendFactor1 = saturate(overlayMask * overlaySample1.a * _OverlayStrength) * baseColor.a;
                half3 firstOverlayRgb = lerp(baseColor.rgb, overlaySample1.rgb, blendFactor1);

                half blendFactor2 = saturate(overlayMask * overlaySample2.a * _OverlayStrength2) * baseColor.a;
                half3 blendedRgb = lerp(firstOverlayRgb, overlaySample2.rgb, blendFactor2);

                return half4(blendedRgb, baseColor.a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
