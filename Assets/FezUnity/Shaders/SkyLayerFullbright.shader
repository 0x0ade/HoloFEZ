// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Based on Unlit/Transparent
Shader "FezUnity/SkyLayer_Fullbright" {
Properties {
	_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
	_Color("Color", Color) = (1,1,1,1)
	
	_PlaneScale("Plane Scale", Vector) = (1,1,0,0)
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 100
	
	Cull Off
	Offset -1, -1
	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha 
	
	Pass {  
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4 _Color;

			float4 _PlaneScale;
			
			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				i.uv = frac((i.uv - _MainTex_ST.zw) * _PlaneScale.xy / _MainTex_ST.xy) * _MainTex_ST.xy + _MainTex_ST.zw;
				return tex2D(_MainTex, i.uv) * _Color;
			}
		ENDCG
	}
}

}
