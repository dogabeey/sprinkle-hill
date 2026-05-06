Shader "Universal Render Pipeline/Sprites/PulsingEmission"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [HDR] _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionBase ("Emission Base", Range(0, 10)) = 0
        _EmissionAmplitude ("Emission Amplitude", Range(0, 10)) = 1

        _PulseSpeed ("Pulse Speed", Range(-20, 20)) = 2
        _PulsePhase ("Pulse Phase", Range(0, 6.2831853)) = 0
        _PulseCurve ("Pulse Curve", Range(0.1, 8)) = 1
        _PulseMin ("Pulse Min", Range(0, 1)) = 0
        _PulseMax ("Pulse Max", Range(0, 1)) = 1

        _BrightnessInfluence ("Brightness Influence", Range(0, 1)) = 1
        _BrightnessPower ("Brightness Power", Range(0.1, 4)) = 1
        _BrightnessMultiplier ("Brightness Multiplier", Range(0, 4)) = 1
        _LuminanceWeights ("Luminance Weights (RGB)", Vector) = (0.2126, 0.7152, 0.0722, 0)

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

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _RendererColor;

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

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 baseColor = texColor * IN.color;

                float pulse01 = sin(_Time.y * _PulseSpeed + _PulsePhase) * 0.5 + 0.5;
                float shapedPulse = pow(saturate(pulse01), max(_PulseCurve, 0.0001));
                float pulse = lerp(_PulseMin, _PulseMax, shapedPulse);

                float luminance = saturate(dot(baseColor.rgb, _LuminanceWeights.rgb));
                float brightnessResponse = pow(max(luminance, 0.0001), max(_BrightnessPower, 0.0001));
                float brightnessFactor = lerp(1.0, brightnessResponse * _BrightnessMultiplier, saturate(_BrightnessInfluence));

                float emissionStrength = _EmissionBase + (_EmissionAmplitude * pulse * brightnessFactor);
                half3 emission = _EmissionColor.rgb * emissionStrength * baseColor.rgb;

                baseColor.rgb += emission;
                return baseColor;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
