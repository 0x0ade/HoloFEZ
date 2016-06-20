Shader "FezUnity/Water"
{
	Properties
	{
		_LiquidBody("Liquid Body", Color) = (1,1,1,1)
		_SolidOverlay("Solid Overlay", Color) = (1,1,1,1)
		_NoiseTex("Noise", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "Queue" = "Transparent+1" }
		Cull Off
		//ZWrite Off
		//ZTest Always
		Offset -2, -2

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
				float4 screenpos : TEXCOORD1;
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4 _LiquidBody;
			float4 _SolidOverlay;

			sampler2D _GrabTexture;
			sampler2D _NoiseTex;
			float4 _NoiseTex_ST;
			sampler2D _CameraDepthTexture;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);
				o.screenpos = ComputeGrabScreenPos(o.vertex);
				
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 n = sin(4.0 * tex2D(_NoiseTex, sin(6.0 * float2(
					_Time.x * 0.1 + sin(_Time.x * 0.2 + i.uv.x * 0.02 + i.uv.y * 0.02) * 0.1 + sin(i.uv.y * 4.0) * 0.01 + cos(i.uv.x * 3.0) * 0.01,
					sin(i.uv.y * 3.0) * 0.02 + cos(i.uv.x * 4.0) * 0.02
				))));
				float4 offs = float4(n.r - 0.25, n.g - 0.25, 0, 0) * 0.05;

				float4 depthpos = i.screenpos + offs;
				#ifdef UNITY_UV_STARTS_AT_TOP
				depthpos.y = depthpos.w - depthpos.y;
				#endif
				float d = Linear01Depth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(depthpos)).r);
				float4 c = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(i.screenpos + offs));
				
				//return _LiquidBody * d + _SolidOverlay * (1.0 - d);
				return _LiquidBody * d + (_SolidOverlay * 0.85 + c * 0.15) * (1.0 - d);
			}
			ENDCG
		}
	}
}
