// Based on standard shader
Shader "FezUnity/BackgroundPlane_Onesided"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.0

		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.0
		[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
		_MetallicGlossMap("Metallic", 2D) = "white" {}

		_BumpScale("Scale", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}

		_Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
		_ParallaxMap ("Height Map", 2D) = "black" {}

		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}

		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		
		_DetailMask("Detail Mask", 2D) = "white" {}

		_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapScale("Scale", Float) = 1.0
		_DetailNormalMap("Normal Map", 2D) = "bump" {}

		[Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0


		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0

		_PlaneScale("Plane Scale", Vector) = (1,1,0,0)
	}

	CGINCLUDE
		#define UNITY_SETUP_BRDF_INPUT MetallicSetup
	ENDCG

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300
	

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			Offset -1, -1

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------
					
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP 
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex vertBase
			#pragma fragment fragHook
			#include "UnityStandardCoreForward.cginc"

			float4 _PlaneScale;
			#pragma multi_compile _ _PlaneClamp
			half4 fragHook(VertexOutputForwardBase i) : SV_Target {
				i.tex.xy =
				#ifndef _PlaneClamp
				frac(
				#else
				saturate(
				#endif
				(i.tex.xy - _MainTex_ST.zw) * _PlaneScale.xy / _MainTex_ST.xy) * _MainTex_ST.xy + _MainTex_ST.zw;
				return fragBase(i);
			}

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			// GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles

			// -------------------------------------

			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog

			#pragma vertex vertAdd
			#pragma fragment fragHook
			#include "UnityStandardCoreForward.cginc"

			float4 _PlaneScale;
			#pragma multi_compile _ _PlaneClamp
			half4 fragHook(VertexOutputForwardAdd i) : SV_Target{
				i.tex.xy = 
				#ifndef _PlaneClamp
				frac(
				#else
				saturate(
				#endif
				(i.tex.xy - _MainTex_ST.zw) * _PlaneScale.xy / _MainTex_ST.xy) * _MainTex_ST.xy + _MainTex_ST.zw;
				return fragAdd(i);
			}

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers gles
			
			// -------------------------------------


			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragHook

			#include "UnityStandardShadow.cginc"

			float4 _PlaneScale;
			#pragma multi_compile _ _PlaneClamp
			half4 fragHook(
				#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
				VertexOutputShadowCaster i
				#endif
				#ifdef UNITY_STANDARD_USE_DITHER_MASK
				, UNITY_VPOS_TYPE vpos : VPOS
				#endif
			) : SV_Target {
				#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
				//FIXME
				/*i.tex.xy = 
				#ifndef _PlaneClamp
				frac(
				#else
				saturate(
				#endif
				(i.tex.xy - _MainTex_ST.zw) * _PlaneScale.xy / _MainTex_ST.xy) * _MainTex_ST.xy + _MainTex_ST.zw;*/
				#endif
				return fragShadowCaster(
					#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
					i
					#endif
					#ifdef UNITY_STANDARD_USE_DITHER_MASK
					, vpos
					#endif
				);
			}

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Deferred pass
		Pass
		{
			Name "DEFERRED"
			Tags { "LightMode" = "Deferred" }

			CGPROGRAM
			#pragma target 3.0
			// TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
			#pragma exclude_renderers nomrt gles
			

			// -------------------------------------

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			#pragma shader_feature _PARALLAXMAP

			#pragma multi_compile ___ UNITY_HDR_ON
			#pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
			#pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
			#pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
			
			#pragma vertex vertDeferred
			#pragma fragment fragHook

			#include "UnityStandardCore.cginc"

			float4 _PlaneScale;
			#pragma multi_compile _ _PlaneClamp
			void fragHook(VertexOutputDeferred i,
				out half4 outDiffuse : SV_Target0,
				out half4 outSpecSmoothness : SV_Target1,
				out half4 outNormal : SV_Target2,
				out half4 outEmission : SV_Target3) {
				i.tex.xy = 
				#ifndef _PlaneClamp
				frac(
				#else
				saturate(
				#endif
				(i.tex.xy - _MainTex_ST.zw) * _PlaneScale.xy / _MainTex_ST.xy) * _MainTex_ST.xy + _MainTex_ST.zw;
				fragDeferred(i, outDiffuse, outSpecSmoothness, outNormal, outEmission);
			}

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_hook

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"

			float4 _PlaneScale;
			#pragma multi_compile _ _PlaneClamp
			float4 frag_hook(v2f_meta i) : SV_Target {
				i.uv.xy = 
				#ifndef _PlaneClamp
				frac(
				#else
				saturate(
				#endif
				(i.uv.xy - _MainTex_ST.zw) * _PlaneScale.xy / _MainTex_ST.xy) * _MainTex_ST.xy + _MainTex_ST.zw;
				return frag_meta(i);
			}

			ENDCG
		}
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 150

		// ------------------------------------------------------------------
		//  Base forward pass (directional light, emission, lightmaps, ...)
		Pass
		{
			Name "FORWARD" 
			Tags { "LightMode" = "ForwardBase" }

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			Offset -1, -1

			CGPROGRAM
			#pragma target 2.0
			
			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION 
			#pragma shader_feature _METALLICGLOSSMAP 
			#pragma shader_feature ___ _DETAIL_MULX2
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP

			#pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#pragma vertex vertBase
			#pragma fragment fragHook
			#include "UnityStandardCoreForward.cginc"

			float4 _PlaneScale;
			#pragma multi_compile _ _PlaneClamp
			half4 fragHook(VertexOutputForwardBase i) : SV_Target {
				i.tex.xy = 
				#ifndef _PlaneClamp
				frac(
				#else
				saturate(
				#endif
				(i.tex.xy - _MainTex_ST.zw) * _PlaneScale.xy / _MainTex_ST.xy) * _MainTex_ST.xy + _MainTex_ST.zw;
				return fragBase(i);
			}

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Additive forward pass (one light per pass)
		Pass
		{
			Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend [_SrcBlend] One
			Fog { Color (0,0,0,0) } // in additive pass fog should be black
			ZWrite Off
			ZTest LEqual
			
			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _NORMALMAP
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2
			// SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP
			#pragma skip_variants SHADOWS_SOFT
			
			#pragma multi_compile_fwdadd_fullshadows
			#pragma multi_compile_fog
			
			#pragma vertex vertAdd
			#pragma fragment fragHook
			#include "UnityStandardCoreForward.cginc"

			float4 _PlaneScale;
			#pragma multi_compile _ _PlaneClamp
			half4 fragHook(VertexOutputForwardAdd i) : SV_Target {
				i.tex.xy = 
				#ifndef _PlaneClamp
				frac(
				#else
				saturate(
				#endif
				(i.tex.xy - _MainTex_ST.zw) * _PlaneScale.xy / _MainTex_ST.xy) * _MainTex_ST.xy + _MainTex_ST.zw;
				return fragAdd(i);
			}

			ENDCG
		}
		// ------------------------------------------------------------------
		//  Shadow rendering pass
		Pass {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }
			
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma target 2.0

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma skip_variants SHADOWS_SOFT
			#pragma multi_compile_shadowcaster

			#pragma vertex vertShadowCaster
			#pragma fragment fragHook

			#include "UnityStandardShadow.cginc"

			float4 _PlaneScale;
			#pragma multi_compile _ _PlaneClamp
			half4 fragHook(
				#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
				VertexOutputShadowCaster i
				#endif
				#ifdef UNITY_STANDARD_USE_DITHER_MASK
				, UNITY_VPOS_TYPE vpos : VPOS
				#endif
			) : SV_Target {
				#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
				//FIXME
				/*i.tex.xy = 
				#ifndef _PlaneClamp
				frac(
				#else
				saturate(
				#endif
				(i.tex.xy - _MainTex_ST.zw) * _PlaneScale.xy / _MainTex_ST.xy) * _MainTex_ST.xy + _MainTex_ST.zw;*/
				#endif
				return fragShadowCaster(
					#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
					i
					#endif
					#ifdef UNITY_STANDARD_USE_DITHER_MASK
					, vpos
					#endif
				);
			}

			ENDCG
		}

		// ------------------------------------------------------------------
		// Extracts information for lightmapping, GI (emission, albedo, ...)
		// This pass it not used during regular rendering.
		Pass
		{
			Name "META" 
			Tags { "LightMode"="Meta" }

			CGPROGRAM
			#pragma vertex vert_meta
			#pragma fragment frag_hook

			#pragma shader_feature _EMISSION
			#pragma shader_feature _METALLICGLOSSMAP
			#pragma shader_feature ___ _DETAIL_MULX2

			#include "UnityStandardMeta.cginc"

			float4 _PlaneScale;
			#pragma multi_compile _ _PlaneClamp
			float4 frag_hook(v2f_meta i) : SV_Target {
				i.uv.xy = 
				#ifndef _PlaneClamp
				frac(
				#else
				saturate(
				#endif
				(i.uv.xy - _MainTex_ST.zw) * _PlaneScale.xy / _MainTex_ST.xy) * _MainTex_ST.xy + _MainTex_ST.zw;
				return frag_meta(i);
			}

			ENDCG
		}
	}


	FallBack "VertexLit"
	CustomEditor "StandardShaderGUI"
}
