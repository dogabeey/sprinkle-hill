Shader "Custom/URP/SpriteIceEffect"
{
    Properties
    {
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color ("Tint", Color) = (1,1,1,1)

        _IceColor ("Ice Color", Color) = (0.6, 0.9, 1.0, 1.0)
        _FrostColor ("Frost Color", Color) = (1,1,1,1)

        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _CrackTex ("Crack Texture", 2D) = "black" {}

        _FreezeAmount ("Freeze Amount", Range(0,1)) = 1
        _EdgeFreeze ("Edge Freeze", Range(0,5)) = 2

        _NoiseScale ("Noise Scale", Float) = 4
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.3
        _NoiseSpeed ("Noise Speed", Float) = 0.15

        _ShimmerStrength ("Shimmer Strength", Range(0,2)) = 0.5
        _ShimmerSpeed ("Shimmer Speed", Float) = 2

        _CrackStrength ("Crack Strength", Range(0,1)) = 0.5
        _EmissionStrength ("Emission Strength", Range(0,5)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SpriteIce"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            TEXTURE2D(_CrackTex);
            SAMPLER(sampler_CrackTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _IceColor;
                float4 _FrostColor;

                float _FreezeAmount;
                float _EdgeFreeze;

                float _NoiseScale;
                float _NoiseStrength;
                float _NoiseSpeed;

                float _ShimmerStrength;
                float _ShimmerSpeed;

                float _CrackStrength;
                float _EmissionStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                half4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                baseCol *= _Color;
                baseCol *= IN.color;

                float alpha = baseCol.a;

                // Animated noise
                float2 noiseUV = uv * _NoiseScale;
                noiseUV += float2(_Time.y * _NoiseSpeed,
                                  _Time.y * _NoiseSpeed * 0.5);

                float noise =
                    SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;

                // Crack texture
                float crack =
                    SAMPLE_TEXTURE2D(_CrackTex, sampler_CrackTex, uv * 2.0).r;

                // Edge freezing
                float2 centeredUV = abs(uv - 0.5) * 2.0;

                float edge =
                    saturate(pow(max(centeredUV.x, centeredUV.y),
                                   _EdgeFreeze));

                // Freeze mask
                float freezeMask =
                    saturate(_FreezeAmount +
                             edge * 0.5 +
                             noise * 0.2);

                // Frost overlay
                float2 screenUV = IN.screenPos.xy / max(IN.screenPos.w, 0.0001);
                float2 shimmerNoiseUV = screenUV * _NoiseScale;
                shimmerNoiseUV += float2(_Time.y * _NoiseSpeed,
                                         _Time.y * _NoiseSpeed * 0.5);
                float shimmerNoise =
                    SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, shimmerNoiseUV).r;

                float frost = noise * freezeMask;

                // Shimmer
                float shimmer =
                    sin((screenUV.x + screenUV.y + _Time.y * _ShimmerSpeed) * 15.0);

                shimmer = saturate(shimmer);
                shimmer *= shimmerNoise;
                shimmer *= _ShimmerStrength;

                // Ice tint
                float3 iceTint =
                    lerp(baseCol.rgb,
                         _IceColor.rgb,
                         freezeMask);

                // Frost blend
                iceTint =
                    lerp(iceTint,
                         _FrostColor.rgb,
                         frost * _NoiseStrength);

                // Cracks
                iceTint *=
                    lerp(1.0,
                         crack,
                         _CrackStrength * freezeMask);

                // Emission
                float3 emission =
                    _IceColor.rgb *
                    shimmer *
                    _EmissionStrength;

                float3 finalColor = iceTint + emission;

                return half4(finalColor, alpha);
            }

            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}