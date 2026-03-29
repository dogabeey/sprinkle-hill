Shader "UI/TutorialHighlight"
{
    Properties
    {
        _OverlayColor  ("Overlay Color",  Color)   = (0, 0, 0, 0.75)
        _Rect0         ("Spotlight 0 (xMin yMin xMax yMax)", Vector) = (0, 0, 0, 0)
        _Rect1         ("Spotlight 1 (xMin yMin xMax yMax)", Vector) = (0, 0, 0, 0)
        _Rect2         ("Spotlight 2 (xMin yMin xMax yMax)", Vector) = (0, 0, 0, 0)
        _Rect3         ("Spotlight 3 (xMin yMin xMax yMax)", Vector) = (0, 0, 0, 0)
        _RectCount     ("Active Rect Count",  Float) = 0
        _CornerRadius  ("Corner Radius (px)", Float) = 16
        _EdgeSoftness  ("Edge Softness (px)", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Overlay+10"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType"     = "Plane"
            "RenderPipeline"  = "UniversalPipeline"
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    Always
        Blend    SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 screenUV   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4  _OverlayColor;
                float4 _Rect0;
                float4 _Rect1;
                float4 _Rect2;
                float4 _Rect3;
                float  _RectCount;
                float  _CornerRadius;
                float  _EdgeSoftness;
            CBUFFER_END

            // Signed distance to a rounded rectangle.
            // rect: (xMin, yMin, xMax, yMax) in screen pixels.
            // p:    fragment position in screen pixels.
            float RoundedRectSDF(float4 rect, float2 p, float r)
            {
                float2 center   = (rect.xy + rect.zw) * 0.5;
                float2 halfSize = (rect.zw - rect.xy) * 0.5;
                float2 q        = abs(p - center) - halfSize + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);

                // Normalized device coordinates ? 0..1 UV
                float4 ndc     = OUT.positionCS * 0.5;
                OUT.screenUV   = float2(ndc.x, ndc.y * _ProjectionParams.x) / OUT.positionCS.w + 0.5;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Convert 0..1 UV to pixel position (matches Unity's Screen.width / Screen.height)
                float2 px = IN.screenUV * _ScreenParams.xy;

                float4 rects[4];
                rects[0] = _Rect0;
                rects[1] = _Rect1;
                rects[2] = _Rect2;
                rects[3] = _Rect3;

                float soft   = max(_EdgeSoftness, 0.5);
                float radius = max(_CornerRadius, 0.0);
                float cutout = 0.0;

                int count = (int)clamp(_RectCount, 0, 4);
                for (int k = 0; k < count; k++)
                {
                    float sdf = RoundedRectSDF(rects[k], px, radius);
                    // smoothstep: negative sdf = inside rect = transparent hole
                    cutout = max(cutout, 1.0 - smoothstep(-soft, soft, sdf));
                }

                half4 col = _OverlayColor;
                col.a    *= (1.0 - cutout);
                return col;
            }
            ENDHLSL
        }
    }

    Fallback "UI/Default"
}
