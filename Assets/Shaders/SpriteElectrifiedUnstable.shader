Shader "Universal Render Pipeline/Sprites/ElectrifiedUnstable"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _NoiseTex ("Noise / Flow Texture", 2D) = "gray" {}
        _NoiseTiling ("Noise Tiling XY", Vector) = (6, 6, 0, 0)
        _FlowDirection ("Flow Direction XY", Vector) = (1, 0.2, 0, 0)
        _FlowSpeed ("Flow Speed", Range(-40, 40)) = 12

        [HDR] _EmissionColor ("Emission Color", Color) = (0.45, 0.95, 2.0, 1)
        _EmissionBase ("Emission Base", Range(0, 8)) = 0.4
        _EmissionAmplitude ("Emission Amplitude", Range(0, 12)) = 2.4
        _EmissionUnstableSpeed ("Emission Unstable Speed", Range(0, 60)) = 17
        _EmissionUnstableStrength ("Emission Unstable Strength", Range(0, 4)) = 1.4

        [HDR] _RimSparkColor ("Rim Spark Color", Color) = (0.6, 1.0, 2.5, 1)
        _RimWidth ("Rim Width", Range(0.0005, 0.08)) = 0.015
        _RimIntensity ("Rim Intensity", Range(0, 8)) = 1.7
        _RimSparkSharpness ("Rim Spark Sharpness", Range(0.1, 16)) = 5.5

        [HDR] _LightningColor ("Lightning Color", Color) = (0.9, 1.0, 3.0, 1)
        _LightningChance ("Lightning Chance", Range(0, 1)) = 0.2
        _LightningFrequency ("Lightning Frequency", Range(0.5, 60)) = 10
        _LightningIntensity ("Lightning Intensity", Range(0, 12)) = 4

        _SparkThreshold ("Spark Threshold", Range(0, 1)) = 0.58
        _SparkContrast ("Spark Contrast", Range(0.1, 12)) = 4
        _DistortionStrength ("UV Distortion Strength", Range(0, 0.2)) = 0.02

        _AlphaClip ("Alpha Clip", Range(0, 1)) = 0.01

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
                float4 color      : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                half4 _Color;
                half4 _RendererColor;

                float4 _NoiseTiling;
                float4 _FlowDirection;
                half _FlowSpeed;

                half4 _EmissionColor;
                half _EmissionBase;
                half _EmissionAmplitude;
                half _EmissionUnstableSpeed;
                half _EmissionUnstableStrength;

                half4 _RimSparkColor;
                half _RimWidth;
                half _RimIntensity;
                half _RimSparkSharpness;

                half4 _LightningColor;
                half _LightningChance;
                half _LightningFrequency;
                half _LightningIntensity;

                half _SparkThreshold;
                half _SparkContrast;
                half _DistortionStrength;
                half _AlphaClip;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color * _RendererColor;
                return OUT;
            }

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 flowDir = normalize(_FlowDirection.xy + float2(0.0001, 0.0001));
                float2 noiseUV = TRANSFORM_TEX(IN.uv, _NoiseTex) * max(_NoiseTiling.xy, float2(0.0001, 0.0001));
                float t = _Time.y * _FlowSpeed;

                float2 flowUV0 = noiseUV + flowDir * t;
                float n0 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, flowUV0).r;
                float combined = saturate(n0);

                float thresholdDenom = max(1e-5, 1.0 - _SparkThreshold);
                float sparkMaskBase = saturate((combined - _SparkThreshold) / thresholdDenom);
                float sparkMask = pow(sparkMaskBase, max(_SparkContrast, 0.0001));

                float signed0 = n0 * 2.0 - 1.0;
                float2 uvOffset = flowDir * (signed0 * _DistortionStrength * sparkMask);

                float2 uv = IN.uv + uvOffset;
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 baseColor = texColor * IN.color;

                float alphaMask = step(_AlphaClip, baseColor.a);
                if (alphaMask <= 0.0)
                    return half4(0, 0, 0, 0);

                float edgeMask = saturate(1.0 - smoothstep(0.0, max(_RimWidth, 0.0001), baseColor.a));
                float rimSpark = pow(saturate(sparkMask * edgeMask), max(_RimSparkSharpness, 0.0001)) * _RimIntensity;

                float unstableWave = sin(_Time.y * _EmissionUnstableSpeed + combined * 18.0) * 0.5 + 0.5;
                float unstableNoise = saturate(combined * unstableWave * _EmissionUnstableStrength);
                float emissionStrength = _EmissionBase + _EmissionAmplitude * unstableNoise;

                float flashTime = floor(_Time.y * _LightningFrequency);
                float flashRandom = Hash11(flashTime);
                float flashGate = step(1.0 - _LightningChance, flashRandom);
                float lightningMask = flashGate * sparkMask;
                float lightning = lightningMask * _LightningIntensity * (0.6 + edgeMask * 0.8);

                half3 emission = _EmissionColor.rgb * emissionStrength * baseColor.rgb;
                half3 rimGlow = _RimSparkColor.rgb * rimSpark;
                half3 lightningGlow = _LightningColor.rgb * lightning;

                baseColor.rgb += emission + rimGlow + lightningGlow;
                return baseColor;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
