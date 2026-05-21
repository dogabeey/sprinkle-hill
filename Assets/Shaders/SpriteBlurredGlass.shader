Shader "Universal Render Pipeline/Sprites/BlurredGlass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _BlurMask ("Blur Mask", 2D) = "white" {}
        _BlurMaskTiling ("Blur Mask Tiling", Vector) = (1,1,0,0)
        _BlurMaskOffset ("Blur Mask Offset", Vector) = (0,0,0,0)
        _BlurStrength ("Blur Strength", Range(0, 8)) = 1
        _BlurSamples ("Blur Samples", Range(0, 1)) = 1

        _FrostMask ("Frost Mask", 2D) = "white" {}
        _FrostMaskTiling ("Frost Mask Tiling", Vector) = (1,1,0,0)
        _FrostMaskOffset ("Frost Mask Offset", Vector) = (0,0,0,0)
        _FrostStrength ("Frost Strength", Range(0, 1)) = 0.5
        _FrostColor ("Frost Color", Color) = (1,1,1,1)

        _GlassOpacity ("Glass Opacity", Range(0, 1)) = 0.35
        _BackgroundBlend ("Background Blend", Range(0, 1)) = 1
        [Toggle] _UseOpaqueTextureFallback ("Use Opaque Texture Fallback", Float) = 0

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
                float4 positionCS     : SV_POSITION;
                float2 uv             : TEXCOORD0;
                float4 color          : COLOR;
                float4 screenPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BlurMask);
            SAMPLER(sampler_BlurMask);
            TEXTURE2D(_FrostMask);
            SAMPLER(sampler_FrostMask);

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            TEXTURE2D_X(_CameraSortingLayerTexture);
            SAMPLER(sampler_CameraSortingLayerTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _RendererColor;

                float4 _BlurMaskTiling;
                float4 _BlurMaskOffset;
                float _BlurStrength;
                float _BlurSamples;

                float4 _FrostMaskTiling;
                float4 _FrostMaskOffset;
                half _FrostStrength;
                half4 _FrostColor;

                half _GlassOpacity;
                half _BackgroundBlend;
                half _UseOpaqueTextureFallback;
            CBUFFER_END

            float2 GetScreenUV(float4 screenPosition)
            {
                float2 uv = screenPosition.xy / max(screenPosition.w, 0.0001);
                #if UNITY_UV_STARTS_AT_TOP
                uv.y = 1.0 - uv.y;
                #endif
                return uv;
            }

            float2 GetScreenTiledUV(float2 screenUV, float4 tiling, float4 offset)
            {
                return screenUV * tiling.xy + offset.xy;
            }

            half SampleBlurMask(float2 screenUV)
            {
                float2 blurMaskUV = GetScreenTiledUV(screenUV, _BlurMaskTiling, _BlurMaskOffset);
                return SAMPLE_TEXTURE2D(_BlurMask, sampler_BlurMask, blurMaskUV).r;
            }

            half SampleFrostMask(float2 screenUV)
            {
                float2 frostMaskUV = GetScreenTiledUV(screenUV, _FrostMaskTiling, _FrostMaskOffset);
                return SAMPLE_TEXTURE2D(_FrostMask, sampler_FrostMask, frostMaskUV).r;
            }

            half3 SampleSceneColor(float2 screenUV)
            {
                if (_UseOpaqueTextureFallback > 0.5h)
                    return SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV).rgb;

                return SAMPLE_TEXTURE2D_X(_CameraSortingLayerTexture, sampler_CameraSortingLayerTexture, screenUV).rgb;
            }

            half3 SampleSceneBlur(float2 screenUV, float2 texelSize, float blurRadius, half blurMask, half quality)
            {
                float2 blurOffset = texelSize * blurRadius * blurMask;
                half3 center = SampleSceneColor(screenUV);

                if (blurMask <= 0.001h || blurRadius <= 0.001)
                    return center;

                half3 blurred = center;
                half weight = 1.0h;

                half3 crossBlur =
                    SampleSceneColor(screenUV + float2( blurOffset.x, 0.0)) +
                    SampleSceneColor(screenUV + float2(-blurOffset.x, 0.0)) +
                    SampleSceneColor(screenUV + float2(0.0,  blurOffset.y)) +
                    SampleSceneColor(screenUV + float2(0.0, -blurOffset.y));

                blurred += crossBlur;
                weight += 4.0h;

                if (quality > 0.5)
                {
                    half2 diagonalOffset = blurOffset * 0.70710678;
                    blurred +=
                        SampleSceneColor(screenUV + float2( diagonalOffset.x,  diagonalOffset.y)) +
                        SampleSceneColor(screenUV + float2(-diagonalOffset.x,  diagonalOffset.y)) +
                        SampleSceneColor(screenUV + float2( diagonalOffset.x, -diagonalOffset.y)) +
                        SampleSceneColor(screenUV + float2(-diagonalOffset.x, -diagonalOffset.y));
                    weight += 4.0h;
                }

                return blurred / max(weight, 1.0h);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = positionInputs.positionCS;
                OUT.screenPosition = ComputeScreenPos(positionInputs.positionCS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color * _RendererColor;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 spriteSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 spriteColor = spriteSample * IN.color;

                float2 screenUV = GetScreenUV(IN.screenPosition);
                float2 opaqueTexelSize = _ScaledScreenParams.zw;

                half blurMask = SampleBlurMask(screenUV);
                half frostMask = SampleFrostMask(screenUV);

                half3 blurredScene = SampleSceneBlur(screenUV, opaqueTexelSize, _BlurStrength, blurMask, _BlurSamples);
                half3 sceneThroughGlass = lerp(spriteColor.rgb, blurredScene, saturate(_BackgroundBlend));

                half frostAmount = saturate(frostMask * _FrostStrength);
                half3 frostedSprite = lerp(spriteColor.rgb, _FrostColor.rgb * spriteColor.rgb, frostAmount);

                half spriteAlpha = spriteColor.a;
                half glassAlpha = saturate(spriteAlpha * _GlassOpacity);
                half visibleBackgroundFactor = saturate((1.0h - spriteAlpha) * blurMask);

                half3 finalRgb = lerp(frostedSprite, sceneThroughGlass, visibleBackgroundFactor);
                half finalAlpha = saturate(glassAlpha + visibleBackgroundFactor * _BackgroundBlend);

                return half4(finalRgb, finalAlpha);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
