Shader "Unlit/Stencil Value"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_StencilRef ("Stencil Reference", float) = 2

    }
    SubShader
    {
        Tags { "Queue"="Geometry-1" }
        ZWrite Off
        ColorMask 0
        Stencil
        {
            Ref [_StencilRef] // StencilRef
            Comp Always
            Pass Replace
        }

        Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float2 texcoord : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.texcoord);
				return col;
			}
			ENDCG
		}
    }
}
