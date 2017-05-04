// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/SgtCloudsphere"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_MainTex("Main Tex", CUBE) = "white" {}
		_DepthTex("Depth Tex", 2D) = "white" {}

		_FadeTex("Fade Tex", 2D) = "white" {}
		_FadeDistanceRecip("Fade Distance Recip", Float) = 1

		_LightingTex("Lighting Tex", 2D) = "white" {}

		_Light1Color("Light 1 Color", Color) = (0,0,0)
		_Light1Scatter("Light 1 Scatter", Color) = (0,0,0)
		_Light1Position("Light 1 Position", Vector) = (0,0,0)
		_Light1Direction("Light 1 Direction", Vector) = (0,0,0)

		_Light2Color("Light 2 Color", Color) = (0,0,0)
		_Light2Scatter("Light 2 Scatter", Color) = (0,0,0)
		_Light2Position("Light 2 Position", Vector) = (0,0,0)
		_Light2Direction("Light 2 Direction", Vector) = (0,0,0)

		_Shadow1Texture("Shadow 1 Texture", 2D) = "white" {}
		_Shadow1Ratio("Shadow 1 Ratio", Float) = 1

		_Shadow2Texture("Shadow 2 Texture", 2D) = "white" {}
		_Shadow2Ratio("Shadow 2 Ratio", Float) = 1
	}
	
	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
		}
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha, One One
			Cull Back
			Lighting Off
			ZWrite Off

			CGPROGRAM
			#include "SgtLight.cginc"
			#include "SgtShadow.cginc"
			#pragma vertex Vert
			#pragma fragment Frag
			// Fade
			#pragma multi_compile __ SGT_A
			// Lights
			#pragma multi_compile __ LIGHT_0 LIGHT_1 LIGHT_2
			// Shadows
			#pragma multi_compile __ SHADOW_1 SHADOW_2

			float4      _Color;
			samplerCUBE _MainTex;
			sampler2D   _DepthTex;
			sampler2D   _FadeTex;
			float       _FadeDistanceRecip;
			sampler2D   _LightingTex;

			struct a2v
			{
				float4 vertex   : POSITION;
				float3 normal   : NORMAL;
			};

			struct v2f
			{
				float4 vertex    : SV_POSITION;
				float3 normal    : NORMAL;
				float2 texcoord0 : TEXCOORD0; // 0..1 depth
				float3 texcoord1 : TEXCOORD1; // world camera to vert/frag
#if LIGHT_1 || LIGHT_2
				float2 texcoord4 : TEXCOORD4; // light 1 theta
	#if LIGHT_2
				float2 texcoord5 : TEXCOORD5; // light 2 theta
	#endif
#endif
#if SHADOW_1 || SHADOW_2
				float4 texcoord6 : TEXCOORD6; // world pos
#endif
			};

			struct f2g
			{
				float4 color : COLOR;
			};

			void Vert(a2v i, out v2f o)
			{
				float4 wVertex = mul(unity_ObjectToWorld, i.vertex);
				float3 wNormal = normalize(mul((float3x3)unity_ObjectToWorld, i.normal));

				o.vertex = UnityObjectToClipPos(i.vertex);
				o.normal = i.normal;
				o.texcoord1 = wVertex.xyz - _WorldSpaceCameraPos;
				o.texcoord0 = dot(wNormal, normalize(-o.texcoord1));
#if LIGHT_1 || LIGHT_2
				o.texcoord4 = dot(wNormal, _Light1Direction) * 0.5f + 0.5f;
	#if LIGHT_2
				o.texcoord5 = dot(wNormal, _Light2Direction) * 0.5f + 0.5f;
	#endif
#endif
#if SHADOW_1 || SHADOW_2
				o.texcoord6 = wVertex;
#endif
			}

			void Frag(v2f i, out f2g o)
			{
				float4 depth = tex2D(_DepthTex, i.texcoord0);
				float4 main = _Color * texCUBE(_MainTex, i.normal) * depth;
#if SGT_A // Fade
				float2 fade01 = length(i.texcoord1) * _FadeDistanceRecip;
				main *= tex2D(_FadeTex, fade01);
#endif
				o.color = main;
#if LIGHT_0 || LIGHT_1 || LIGHT_2
				o.color.rgb *= UNITY_LIGHTMODEL_AMBIENT;
	#if LIGHT_1 || LIGHT_2
				float4 lighting = main * tex2D(_LightingTex, i.texcoord4) * _Light1Color;
		#if LIGHT_2
				lighting += main * tex2D(_LightingTex, i.texcoord5) * _Light2Color;
		#endif
		#if SHADOW_1 || SHADOW_2
				lighting *= ShadowColor(i.texcoord6);
		#endif
				o.color += lighting;
	#endif
#endif
			}
			ENDCG
		} // Pass
	} // SubShader
} // Shader