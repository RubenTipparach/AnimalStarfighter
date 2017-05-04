// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/SgtAtmosphereInner"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_DepthTex("Depth Tex", 2D) = "white" {}
		_HorizonLengthRecip("Horizon Length Recip", Float) = 0

		_LightingTex("Lighting Tex", 2D) = "white" {}
		_ScatteringTex("Scattering Tex", 2D) = "white" {}

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
			Cull Back
			Lighting Off
			Offset -0.1, 0
			ZWrite Off

			CGPROGRAM
			#include "SgtLight.cginc"
			#include "SgtShadow.cginc"
			#pragma vertex Vert
			#pragma fragment Frag
			// Outside
			#pragma multi_compile __ SGT_A
			// Scattering
			#pragma multi_compile __ SGT_B
			// Lights
			#pragma multi_compile __ LIGHT_0 LIGHT_1 LIGHT_2
			// Shadows
			#pragma multi_compile __ SHADOW_1 SHADOW_2

			float4    _Color;
			sampler2D _DepthTex;
			sampler2D _LightingTex;
			sampler2D _ScatteringTex;
			float     _HorizonLengthRecip;
			float4x4  _WorldToLocal;
			float4x4  _LocalToWorld;

			struct a2v
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex    : SV_POSITION;
				float2 texcoord0 : TEXCOORD0; // 0..1 depth
#if LIGHT_1 || LIGHT_2
				float4 texcoord1 : TEXCOORD1; // xyz = light 1 to vertex/pixel, w = light 1 theta
	#if LIGHT_2
				float4 texcoord2 : TEXCOORD2; // xyz = light 2 to vertex/pixel, w = light 2 theta
	#endif
	#if SGT_B
				float3 texcoord4 : TEXCOORD4; // camera to vertex/pixel
	#endif
#endif
#if SHADOW_1 || SHADOW_2
				float4 texcoord6 : TEXCOORD6; // near world position
#endif
			};

			struct f2g
			{
				float4 color : COLOR;
			};

			float3 IntersectUnitSphere(float3 ray, float3 rayD)
			{
				float B = dot(ray, rayD);
				float C = dot(ray, ray) - 1.0f;
				float D = B * B - C;
				return ray + (-B - sqrt(D)) * rayD;
			}

			void Vert(a2v i, out v2f o)
			{
				float4 wPos = mul(unity_ObjectToWorld, i.vertex);
				float4 far  = mul(_WorldToLocal, wPos);
				float4 near = mul(_WorldToLocal, float4(_WorldSpaceCameraPos, 1.0f));
#if SGT_A // Outside
				near.xyz = IntersectUnitSphere(far.xyz, normalize(far.xyz - near.xyz));
#endif
				float depth   = length(near.xyz - far.xyz);
				float horizon = depth * _HorizonLengthRecip;

				o.vertex    = UnityObjectToClipPos(i.vertex);
				o.texcoord0 = horizon;
#if LIGHT_1 || LIGHT_2
				float3 nearD = normalize(near.xyz);

				o.texcoord1 = dot(nearD, _Light1Direction) * 0.5f + 0.5f;
	#if LIGHT_2
				o.texcoord2 = dot(nearD, _Light2Direction) * 0.5f + 0.5f;
	#endif
	#if SGT_B
				o.texcoord4 = _WorldSpaceCameraPos - wPos.xyz;
				o.texcoord1.xyz = _Light1Position.xyz - _WorldSpaceCameraPos;
		#if LIGHT_2
				o.texcoord2.xyz = _Light2Position.xyz - _WorldSpaceCameraPos;
		#endif
	#endif
#endif
#if SHADOW_1 || SHADOW_2
				o.texcoord6 = mul(_LocalToWorld, wPos);
#endif
			}

			void Frag(v2f i, out f2g o)
			{
				float4 depth = tex2D(_DepthTex, i.texcoord0);
				float4 main = depth * _Color;

				o.color = main;
#if LIGHT_0 || LIGHT_1 || LIGHT_2
				o.color.rgb *= UNITY_LIGHTMODEL_AMBIENT;
	#if LIGHT_1 || LIGHT_2
				i.texcoord0.x = i.texcoord1.w;

				float4 lighting = tex2D(_LightingTex, i.texcoord0) * main * _Light1Color;
		#if SGT_B // Scattering
				i.texcoord4 = normalize(-i.texcoord4);

				i.texcoord0.y = dot(i.texcoord4, normalize(i.texcoord1.xyz)) * 0.5f + 0.5f;

				float4 scattering = tex2D(_ScatteringTex, i.texcoord0) * _Light1Scatter;
			#if LIGHT_2
				i.texcoord0.y = dot(i.texcoord4, normalize(i.texcoord2.xyz));

				scattering += tex2D(_ScatteringTex, i.texcoord0) * _Light2Scatter;
			#endif
				scattering *= o.color.a; // Fade scattering out according to optical depth
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