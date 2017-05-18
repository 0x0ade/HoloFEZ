// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Based on Unlit/Transparent
Shader "HoloFez/FloatingButton" {
Properties {
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	_MaskTex ("Mask (A)", 2D) = "white" {}
	_Color("Color", Color) = (1,1,1,1)
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
	Cull Off

	Pass {
		ZWrite On
		//ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 uv_MainTex : TEXCOORD0;
				float2 uv_MaskTex : TEXCOORD1;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 uv_MainTex : TEXCOORD0;
				half2 uv_MaskTex : TEXCOORD1;
				UNITY_FOG_COORDS(2)
			};

			sampler2D _MainTex;
			sampler2D _MaskTex;
			float4 _MainTex_ST;
			float4 _MaskTex_ST;

			float4 _Color;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv_MainTex = TRANSFORM_TEX(v.uv_MainTex, _MainTex);
				o.uv_MaskTex = TRANSFORM_TEX(v.uv_MaskTex, _MaskTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv_MainTex);
				col.a *= tex2D(_MaskTex, i.uv_MaskTex).a;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col * _Color;
			}
		ENDCG
	}


	Pass {
		Tags { "LightMode" = "ShadowCaster" }
		ZWrite On
		ZTest LEqual

		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 uv_MainTex : TEXCOORD0;
				float2 uv_MaskTex : TEXCOORD1;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 uv_MainTex : TEXCOORD0;
				half2 uv_MaskTex : TEXCOORD1;
				UNITY_FOG_COORDS(2)
			};

			sampler2D _MainTex;
			sampler2D _MaskTex;
			float4 _MainTex_ST;
			float4 _MaskTex_ST;

			float4 _Color;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv_MainTex = TRANSFORM_TEX(v.uv_MainTex, _MainTex);
				o.uv_MaskTex = TRANSFORM_TEX(v.uv_MaskTex, _MaskTex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv_MainTex);
				col.a *= tex2D(_MaskTex, i.uv_MaskTex).a;
				clip(col.a - 0.5);
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col * _Color;
			}
		ENDCG
	}

}

}
