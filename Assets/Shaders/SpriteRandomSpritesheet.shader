Shader "Universal Render Pipeline/Sprites/RandomSpriteSheet"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Tiles ("Tiles (X,Y)", Vector) = (4,4,0,0)
        _AnimationSpeed ("Animation Speed", Float) = 8

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
                float4 _Tiles;
                float _AnimationSpeed;
            CBUFFER_END

            float Hash01(float n)
            {
                return frac(sin(n * 12.9898 + 78.233) * 43758.5453);
            }

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
                float2 tiles = max(_Tiles.xy, float2(1.0, 1.0));
                float totalFrames = max(tiles.x * tiles.y, 1.0);

                float frameStep = floor(_Time.y * max(_AnimationSpeed, 0.0));
                float randomFrame = floor(Hash01(frameStep + 1.0) * totalFrames);

                float2 tileCoord;
                tileCoord.x = fmod(randomFrame, tiles.x);
                tileCoord.y = floor(randomFrame / tiles.x);

                float2 localUV = frac(IN.uv * tiles);
                float2 sheetUV = (tileCoord + localUV) / tiles;

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, sheetUV) * IN.color;
                return col;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
