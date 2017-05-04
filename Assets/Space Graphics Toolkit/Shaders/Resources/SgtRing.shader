// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/SgtRing"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_MainTex("Main Tex", 2D) = "white" {}

		_FadeTex("Fade Tex", 2D) = "white" {}
		_FadeDistanceRecip("Fade Distance Recip", Float) = 1

		_DetailTex("Detail Tex", 2D) = "white" {}
		_DetailScale("Detail Scale", Vector) = (1,1,1)
		_DetailOffset("Detail Offset", Vector) = (0,0,0)
		_DetailTwist("Detail Twist", Float) = 0
		_DetailTwistBias("Detail Twist Bias", Float) = 0
			
		_LightingTex("Lighting Tex", 2D) = "white" {}
		_ScatteringMie("ScatteringMie", Float) = 10
		
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
			"Queue"           = "Transparent"
			"RenderType"      = "Transparent"
			"IgnoreProjector" = "True"
		}
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha, One One
			Cull Off
			Lighting Off
			ZWrite Off

			CGPROGRAM
			#include "SgtLight.cginc"
			#include "SgtShadow.cginc"
			#pragma vertex Vert
			#pragma fragment Frag
			float4    _Color;
			sampler2D _MainTex;

			sampler2D _LightingTex;
			float     _ScatteringMie;

			sampler2D _DetailTex;
			float2    _DetailScale;
			float2    _DetailOffset;
			float     _DetailTwist;
			float     _DetailTwistBias;

			sampler2D _FadeTex;
			float     _FadeDistanceRecip;
			// Scattering
			#pragma multi_compile __ SGT_A
			// Detail
			#pragma multi_compile __ SGT_B
			// Fade
			#pragma multi_compile __ SGT_C
			// Lights
			#pragma multi_compile __ LIGHT_0 LIGHT_1 LIGHT_2
			// Shadows
			#pragma multi_compile __ SHADOW_1 SHADOW_2

			struct a2v
			{
				float4 vertex    : POSITION;
				float4 color     : COLOR;
				float2 texcoord0 : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
				float3 normal    : NORMAL;
			};

			struct v2f
			{
				float4 vertex    : SV_POSITION;
				float2 texcoord0 : TEXCOORD0; // x = 0..1 distance, y = inner/outer edge position
				float2 texcoord1 : TEXCOORD1;
				float3 texcoord2 : TEXCOORD2; // world camera to vert/frag
				float3 texcoord3 : TEXCOORD3; // local vert/frag
#if LIGHT_1 || LIGHT_2
				float3 texcoord4 : TEXCOORD4; // world vert/frag to light 1
	#if LIGHT_2
				float3 texcoord5 : TEXCOORD5; // world vert/frag to light 2
	#endif
#endif
#if SHADOW_1 || SHADOW_2
				float4 texcoord6 : TEXCOORD6; // world vert/frag
#endif
			};

			struct f2g
			{
				float4 color : COLOR;
			};

			void Vert(a2v i, out v2f o)
			{
				float4 wVertex = mul(unity_ObjectToWorld, i.vertex);

				o.vertex    = UnityObjectToClipPos(i.vertex);
				o.texcoord0 = i.texcoord0;
				o.texcoord1 = i.texcoord1;
				o.texcoord2 = wVertex.xyz - _WorldSpaceCameraPos;
				o.texcoord3 = i.vertex.xyz;
#if LIGHT_1 || LIGHT_2
				o.texcoord4 = _Light1Position.xyz - wVertex.xyz;
	#if LIGHT_2
				o.texcoord5 = _Light2Position.xyz - wVertex.xyz;
	#endif
#endif
#if SHADOW_1 || SHADOW_2
				o.texcoord6 = wVertex;
#endif
			}

			void Frag(v2f i, out f2g o)
			{
				i.texcoord0.y = i.texcoord1.y / i.texcoord1.x;
				float4 main = _Color * tex2D(_MainTex, i.texcoord0);
#if SGT_B // Detail
				i.texcoord0.y += pow(i.texcoord0.x, _DetailTwistBias) * _DetailTwist;
				float4 detail = tex2D(_DetailTex, i.texcoord0 * _DetailScale + _DetailOffset);
				main *= detail;
#endif
#if SGT_C // Fade
				float2 fade01 = length(i.texcoord2) * _FadeDistanceRecip;
				float  fade   = tex2D(_FadeTex, fade01).a;
				main.a *= fade;
#endif
				o.color = main;
#if LIGHT_0 || LIGHT_1 || LIGHT_2
				o.color.rgb *= UNITY_LIGHTMODEL_AMBIENT.rgb;
	#if LIGHT_1 || LIGHT_2
				i.texcoord2 = normalize(i.texcoord2);

				float2 theta    = dot(i.texcoord2, normalize(i.texcoord4));
				float4 lighting = tex2D(_LightingTex, theta * 0.5f + 0.5f) * main * _Light1Color;
		#if SGT_A // Scattering
				float4 scattering = MiePhase2(theta.x, _ScatteringMie) * _Light1Scatter;
			#if LIGHT_2
				theta       = dot(i.texcoord2, normalize(i.texcoord5));
				scattering += MiePhase2(theta.x, _ScatteringMie) * _Light2Scatter;
			#endif
				scattering *= o.color.a * (1.0f - o.color.a); // Fade scattering out according to optical depth
				scattering *= main;
				scattering *= saturate(1.0f - (o.color + lighting)); // Only scatter into remaining rgba

				lighting += scattering;
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