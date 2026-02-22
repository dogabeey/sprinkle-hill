Shader "Unlit/USB_simple_color"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcBlend ("SrcFactor", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]
        _DstBlend ("DstFactor", Float) = 1
        _MainColor ("Main Color", Color) = (1,1,1,1)
        [Toggle]
        _Cutoff ("Alpha To Mask", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)]
        _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
        }
        Cull [_Cull]

        LOD 100

        Blend [_SrcBlend] [_DstBlend]
        AlphaToMask [_Cutoff]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };
            
            fixed4 _MainColor;
            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);

                // Add a random value to vertex position
    o.vertex.x += sin(_Time.y * 10.0 + v.vertex.x) * 0.1;

                return o;
            }

            fixed4 frag (v2f i, bool face : SV_ISFRONTFACE) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);

                // apply main COLOR
                col = _MainColor;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
