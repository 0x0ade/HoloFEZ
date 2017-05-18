// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "HoloFez/CanvasBG"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_NoiseTex("Noise", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" }
		Cull Off
		//ZWrite Off
		//ZTest Always

		GrabPass { }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 screenuv : TEXCOORD1;
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4 _Color;

			sampler2D _GrabTexture;
			sampler2D _NoiseTex;
			float4 _NoiseTex_ST;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);
				half4 screenpos = ComputeGrabScreenPos(o.vertex);
				o.screenuv = screenpos.xy / screenpos.w;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.screenuv;
				float4 n = tex2D(_NoiseTex, float2(frac(_Time.w + i.uv.y * i.uv.x * 512.0), i.uv.y));
				//uv.x += n.r * 0.5;
				//return tex2D(_GrabTexture, frac(uv)) * _Color;
				return float4(
					tex2D(_GrabTexture, frac(uv + n.r * 0.0125)).r,
					tex2D(_GrabTexture, frac(uv + n.g * 0.025)).g,
					tex2D(_GrabTexture, frac(uv + n.b * 0.0375)).b,
					1.0
				) * _Color;
			}
			ENDCG
		}
	}
}
