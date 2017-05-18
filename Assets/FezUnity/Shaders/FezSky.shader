// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// With help from https://github.com/keijiro/UnitySkyboxShaders
Shader "FezUnity/Sky"
{
    Properties
    {
		_SkyGradientTex("Sky Gradient Map", 2D) = "white" {}
		_SkyGradientX("Sky X", Range(0.0, 1.0)) = 0.5
		_SkyGradientYOffset("Sky Y Offset", Range(0.0, 1.0)) = 0.0
    }

    CGINCLUDE

    #include "UnityCG.cginc"

    struct appdata
    {
        float4 position : POSITION;
        float3 uv : TEXCOORD0;
    };
    
    struct v2f
    {
        float4 position : SV_POSITION;
        float3 uv : TEXCOORD0;
    };
    
	sampler2D _SkyGradientTex;
	float4 _SkyGradientTex_ST;
	float _SkyGradientX;
	float _SkyGradientYOffset;

    v2f vert(appdata v)
    {
        v2f o;
        o.position = UnityObjectToClipPos(v.position);
        o.uv = v.uv;
        return o;
    }
    
    float4 frag(v2f i) : COLOR
    {
        return tex2D(_SkyGradientTex, float2(_SkyGradientX, (i.uv.y * (1.0 - _SkyGradientYOffset * 2.0) + _SkyGradientYOffset) * 0.5 + 0.5));
    }

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" }
        Pass
        {
            ZWrite Off
            Cull Off
            Fog { Mode Off }
            CGPROGRAM
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma vertex vert
            #pragma fragment frag
            ENDCG
        }
    } 
}
