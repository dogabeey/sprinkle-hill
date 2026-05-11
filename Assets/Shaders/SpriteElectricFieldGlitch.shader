Shader "Universal Render Pipeline/Sprites/ElectricFieldGlitch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _EmissionMask ("Emission Mask", 2D) = "white" {}
        _EmissionMaskStrength ("Emission Mask Strength", Range(0, 1)) = 1
        [HDR] _EmissionColor ("Emission Color", Color) = (0.35, 0.8, 1.5, 1)
        _EmissionBase ("Emission Base", Range(0, 10)) = 0.2
        _EmissionAmplitude ("Emission Amplitude", Range(0, 10)) = 1.6

        _PulseSpeed ("Pulse Speed", Range(-20, 20)) = 4
        _PulsePhase ("Pulse Phase", Range(0, 6.2831853)) = 0
        _PulseCurve ("Pulse Curve", Range(0.1, 8)) = 1.25
        _PulseMin ("Pulse Min", Range(0, 1)) = 0.05
        _PulseMax ("Pulse Max", Range(0, 1)) = 1

        _BrightnessInfluence ("Brightness Influence", Range(0, 1)) = 0.9
        _BrightnessPower ("Brightness Power", Range(0.1, 4)) = 1
        _BrightnessMultiplier ("Brightness Multiplier", Range(0, 4)) = 1
        _LuminanceWeights ("Luminance Weights (RGB)", Vector) = (0.2126, 0.7152, 0.0722, 0)

        _GlitchTex ("Glitch Texture", 2D) = "gray" {}
        _GlitchTiling ("Glitch Tiling XY", Vector) = (4, 6, 0, 0)
        _GlitchDirection ("Flow Direction XY", Vector) = (0.9, 0.2, 0, 0)
        _GlitchSpeed ("Glitch Flow Speed", Range(-30, 30)) = 7
        _GlitchStrength ("UV Distortion Strength", Range(0, 0.2)) = 0.03
        _GlitchThreshold ("Glitch Threshold", Range(0, 1)) = 0.45
        _GlitchContrast ("Glitch Contrast", Range(0.1, 8)) = 2.5
        _SecondaryDistortionStrength ("Secondary Distortion", Range(0, 0.2)) = 0.015

        [HDR] _FlowColor ("Flow Glow Color", Color) = (0.4, 0.95, 2, 1)
        _FlowIntensity ("Flow Glow Intensity", Range(0, 8)) = 1.8
        _FlowSharpness ("Flow Sharpness", Range(0.1, 8)) = 2.2

        [HDR] _StaticColor ("Static Color", Color) = (0.8, 0.95, 1.25, 1)
        _StaticIntensity ("Static Intensity", Range(0, 4)) = 0.5
        _StaticScanDensity ("Static Scan Density", Range(1, 128)) = 38
        _StaticScanSpeed ("Static Scan Speed", Range(-50, 50)) = 17
        _StaticNoiseTiling ("Static Noise Tiling", Range(1, 128)) = 26

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
            TEXTURE2D(_EmissionMask);
            SAMPLER(sampler_EmissionMask);
            TEXTURE2D(_GlitchTex);
            SAMPLER(sampler_GlitchTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _EmissionMask_ST;
                float4 _GlitchTex_ST;

                half4 _Color;
                half4 _RendererColor;

                half _EmissionMaskStrength;
                half4 _EmissionColor;
                half _EmissionBase;
                half _EmissionAmplitude;

                half _PulseSpeed;
                half _PulsePhase;
                half _PulseCurve;
                half _PulseMin;
                half _PulseMax;

                half _BrightnessInfluence;
                half _BrightnessPower;
                half _BrightnessMultiplier;
                float4 _LuminanceWeights;

                float4 _GlitchTiling;
                float4 _GlitchDirection;
                half _GlitchSpeed;
                half _GlitchStrength;
                half _GlitchThreshold;
                half _GlitchContrast;
                half _SecondaryDistortionStrength;

                half4 _FlowColor;
                half _FlowIntensity;
                half _FlowSharpness;

                half4 _StaticColor;
                half _StaticIntensity;
                half _StaticScanDensity;
                half _StaticScanSpeed;
                half _StaticNoiseTiling;
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

            float ComputePulse01()
            {
                float pulse01 = sin(_Time.y * _PulseSpeed + _PulsePhase) * 0.5 + 0.5;
                float shapedPulse = pow(saturate(pulse01), max(_PulseCurve, 0.0001));
                return lerp(_PulseMin, _PulseMax, shapedPulse);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 flowDir = normalize(_GlitchDirection.xy + float2(0.0001, 0.0001));
                float2 glitchBaseUV = TRANSFORM_TEX(IN.uv, _GlitchTex) * max(_GlitchTiling.xy, float2(0.0001, 0.0001));
                float timePhase = _Time.y * _GlitchSpeed;

                float2 flowUV0 = glitchBaseUV + flowDir * timePhase;
                float2 flowUV1 = glitchBaseUV * 1.73 + float2(13.17, 5.31) - flowDir.yx * (timePhase * 0.77);

                float noiseA = SAMPLE_TEXTURE2D(_GlitchTex, sampler_GlitchTex, flowUV0).r;
                float noiseB = SAMPLE_TEXTURE2D(_GlitchTex, sampler_GlitchTex, flowUV1).g;
                float combinedNoise = saturate(noiseA * 0.7 + noiseB * 0.3);

                float thresholdDenom = max(1e-5, 1.0 - _GlitchThreshold);
                float thresholded = saturate((combinedNoise - _GlitchThreshold) / thresholdDenom);
                float glitchMask = pow(thresholded, max(_GlitchContrast, 0.0001));

                float signedNoiseA = noiseA * 2.0 - 1.0;
                float signedNoiseB = noiseB * 2.0 - 1.0;
                float2 uvOffset = flowDir * (signedNoiseA * _GlitchStrength * glitchMask);
                uvOffset += flowDir.yx * (signedNoiseB * _SecondaryDistortionStrength * glitchMask);

                float2 sampleUV = IN.uv + uvOffset;
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sampleUV);
                half4 baseColor = texColor * IN.color;

                float2 emissionUV = TRANSFORM_TEX(sampleUV, _EmissionMask);
                half emissionMask = SAMPLE_TEXTURE2D(_EmissionMask, sampler_EmissionMask, emissionUV).r;
                emissionMask = lerp(1.0h, saturate(emissionMask), saturate(_EmissionMaskStrength));

                float pulse = ComputePulse01();
                float luminance = saturate(dot(baseColor.rgb, _LuminanceWeights.rgb));
                float brightnessResponse = pow(max(luminance, 0.0001), max(_BrightnessPower, 0.0001));
                float brightnessFactor = lerp(1.0, brightnessResponse * _BrightnessMultiplier, saturate(_BrightnessInfluence));

                float emissionStrength = _EmissionBase + (_EmissionAmplitude * pulse * brightnessFactor);
                float flowGlow = pow(saturate(combinedNoise), max(_FlowSharpness, 0.0001)) * _FlowIntensity * glitchMask;
                half3 emission = _EmissionColor.rgb * emissionStrength * baseColor.rgb * emissionMask;
                emission += _FlowColor.rgb * flowGlow * emissionMask;

                float staticNoise = SAMPLE_TEXTURE2D(_GlitchTex, sampler_GlitchTex, glitchBaseUV * _StaticNoiseTiling + float2(0.0, _Time.y * _StaticScanSpeed)).b;
                float scanline = sin((IN.uv.y + _Time.y * _StaticScanSpeed * 0.02) * _StaticScanDensity * 6.2831853) * 0.5 + 0.5;
                float staticMask = saturate(scanline * staticNoise * glitchMask);
                half3 staticColor = _StaticColor.rgb * (_StaticIntensity * staticMask);

                baseColor.rgb += emission + staticColor;
                return baseColor;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
