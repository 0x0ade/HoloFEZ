// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "HoloFez/Reticle"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		_RadiusOuter("Outer Radius", Range(0.0, 0.5)) = 0.5
		_RadiusInner("Inner Radius", Range(0.0, 0.5)) = 0.0
		_Fill("Fill", Range(0.0, 1.0)) = 1.0
	}
	SubShader
	{
		Tags { "Queue"="Overlay" "IgnoreProjector"="True" "RenderType"="Transparent" }
		LOD 100

		Pass
		{
			Offset -100,-100 // Always up-front
			Cull Off
			Lighting Off
			ZWrite Off
			ZTest Off
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0

			#define TAU 6.28318530718
			#define PI 3.14159265359
			#define PIover2 1.57079632679

			#pragma multi_compile _ loading_rot loading_fill loading_jump 
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4 _Color;

			float _RadiusOuter;
			float _RadiusInner;
			
			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			#define PSIN(a) (0.5 + 0.5 * sin(a))
			#define SSr(s, r, v) smoothstep(s - r, s + r, v)
			#define SS(s, v) smoothstep(s - 0.01, s + 0.01, v)

			float circle(float2 xy, float or, float ir) {
				float r = length(xy - 0.5);
				return (1.0 - SS(or, r)) * SS(or - ir, r);
			}

			#ifdef loading_rot
			float loadrot(float2 xy, float f) {
				f = frac(f * 0.5) * -TAU;
				xy -= 0.5;
				xy /= length(xy);
				return SS(0.5, xy.x * cos(f) + xy.y * sin(f));
			}
			#endif

			#ifdef loading_fill
			float _Fill;
			float loadfill(float2 xy, float f) {
				f = frac(f) * 2.0;
				xy -= 0.5;
				xy /= length(xy);

				float rf = 0.0;
				float ss = 0.5;

				rf += SS(ss,  xy.y *  cos(PI * min(0.0, f)));
				rf += SS(ss,  xy.x *  sin(PI * min(0.5, f)));
				rf += SS(ss, -xy.y * -cos(PI * min(1.0, f)));
				rf += SS(ss, -xy.x * -sin(PI * min(1.5, f)));

				return clamp(0.0, 1.0, rf);
			}
			#endif

			#ifdef loading_jump
			float loadjump(float2 xy, float f) {
				f = frac(f) * 2.0 - 2.0;
				xy -= 0.5;
				xy /= length(xy);

				float rf = 0.0;
				float ss = 0.5;

				rf += SS(ss,  xy.y *  cos(PI * min(0.0, f)));
				rf += SS(ss,  xy.x *  sin(PI * min(0.5, f)));
				rf += SS(ss, -xy.y * -cos(PI * min(1.0, f)));
				rf += SS(ss, -xy.x * -sin(PI * min(1.5, f)));

				return clamp(0.0, 1.0, rf);
			}
			#endif
			
			fixed4 frag (v2f i) : SV_Target {
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				
				col.a *= circle(i.uv, _RadiusOuter, _RadiusInner);
				
				#ifdef loading_rot
				col.a *= loadrot(i.uv, _Time.y);
				#endif
				#ifdef loading_fill
				col.a *= loadfill(i.uv, _Fill);
				#endif
				#ifdef loading_jump
				col.a *= loadjump(i.uv, _Time.y);
				#endif

				return col * _Color;
			}
			ENDCG
		}
	}
}
