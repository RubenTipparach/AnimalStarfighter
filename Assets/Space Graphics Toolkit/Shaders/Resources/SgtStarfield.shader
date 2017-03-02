Shader "Hidden/SgtStarfield"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_MainTex("Main Tex", 2D) = "white" {}
		_Scale("Scale", Float) = 1
		_CameraRollAngle("Camera Roll Angle", Float) = 0

		_WrapSize("Wrap Size", Vector) = (0,0,0)

		_StretchDirection("Stretch Direction", Vector) = (0,0,0)
		_StretchLength("Stretch Length", Float) = 0
		_StretchVector("Stretch Vector", Float) = 0

		_FadeNearTex("Fade Near Tex", 2D) = "white" {}
		_FadeNearRadius("Fade Near Radius", Float) = 0
		_FadeNearScale("Fade Near Scale", Float) = 0

		_FadeFarTex("Fade Far Tex", 2D) = "white" {}
		_FadeFarRadius("Fade Far Radius", Float) = 0
		_FadeFarScale("Fade Far Scale", Float) = 0

		_PulseOffset("Pulse Offset", Float) = 1

		_InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0
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
			Blend One One
			Cull Off
			Lighting Off
			ZWrite Off

			CGPROGRAM
				#pragma vertex Vert
				#pragma fragment Frag
				// Wrap
				#pragma multi_compile __ SGT_B
				// Stretch
				#pragma multi_compile __ SGT_C
				// Fade near
				#pragma multi_compile __ SGT_D
				// Fade far
				#pragma multi_compile __ SGT_E
				// Pulse (avoid using SGT_F)
				#pragma multi_compile __ LIGHT_1
				// Softness (avoid using SGT_G)
				#pragma multi_compile __ LIGHT_2

#if LIGHT_2 // Soft particles
				#include "UnityCG.cginc"
#endif
				float4    _Color;
				sampler2D _MainTex;
				float     _Scale;
				float     _CameraRollAngle;

				float3 _WrapSize;

				float3 _StretchDirection;
				float  _StretchLength;
				float3 _StretchVector;

				sampler2D _FadeNearTex;
				float     _FadeNearRadius;
				float     _FadeNearScale;

				sampler2D _FadeFarTex;
				float     _FadeFarRadius;
				float     _FadeFarScale;

				float _PulseOffset;

				sampler2D_float _CameraDepthTexture;
				float           _InvFade;

				struct a2v
				{
					float4 vertex    : POSITION;
					float4 color     : COLOR;
					float3 normal    : NORMAL; // xy = corner offset, z = angle
					float3 tangent   : TANGENT; // x = pulse offset, y = pulse speed, z = pulse scale
					float2 texcoord0 : TEXCOORD0; // uv
					float2 texcoord1 : TEXCOORD1; // x = radius, y = back or front [-0.5 .. 0.5]
				};

				struct v2f
				{
					float4 vertex    : SV_POSITION;
					float4 color     : COLOR;
					float2 texcoord0 : TEXCOORD0;
					float3 texcoord1 : TEXCOORD1; // mvpos
#if LIGHT_2 // Soft particles
					float4 projPos : TEXCOORD2;
#endif
				};

				struct f2g
				{
					float4 color : COLOR;
				};

				float2 Rotate(float2 v, float a)
				{
					float s = sin(a);
					float c = cos(a);
					return float2(c * v.x - s * v.y, s * v.x + c * v.y);
				}

				void Vert(a2v i, out v2f o)
				{
#if SGT_B // Wrap
					float4 cameraO   = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0f)) / _Scale;
					float3 relativeO = i.vertex.xyz - cameraO.xyz;
					i.vertex.xyz = cameraO.xyz + (frac(relativeO / _WrapSize + 0.5f) - 0.5f) * _WrapSize;
#endif
					float radius = i.texcoord1.x * _Scale;
#if SGT_C // Stretch
					float4 vertexM  = mul(unity_ObjectToWorld, i.vertex);
					float4 vertexMV = mul(UNITY_MATRIX_MV, i.vertex);
					float3 up       = cross(_StretchDirection, normalize(vertexM.xyz - _WorldSpaceCameraPos));

					// Uncomment below if you want the stars to be stretched based on their size too
					vertexM.xyz += _StretchVector * i.texcoord1.y; // * radius;
					vertexM.xyz += up * i.normal.y * radius;

					o.vertex    = mul(UNITY_MATRIX_VP, vertexM);
					o.texcoord1 = mul(UNITY_MATRIX_V, vertexM);
#else
	#if LIGHT_1 // Pulse
					radius *= 1.0f + sin(i.tangent.x * 3.141592654f + _PulseOffset * i.tangent.y) * i.tangent.z;
	#endif
					float4 vertexMV = mul(UNITY_MATRIX_MV, i.vertex);
					float4 cornerMV = vertexMV;
					float  angle    = _CameraRollAngle + i.normal.z * 3.141592654f;

					i.normal.xy = Rotate(i.normal.xy, angle);

					cornerMV.xy += i.normal.xy * radius;

					o.vertex    = mul(UNITY_MATRIX_P, cornerMV);
					o.texcoord1 = cornerMV.xyz;
#endif
					o.color     = i.color * _Color;
					o.texcoord0 = i.texcoord0;
#if LIGHT_2 // Softness
					o.projPos = ComputeScreenPos(o.vertex);
					o.projPos.z = -mul(UNITY_MATRIX_MV, i.vertex).z;
#endif
				}

				void Frag(v2f i, out f2g o)
				{
					float dist = length(i.texcoord1);
					o.color = tex2D(_MainTex, i.texcoord0) * i.color;
#if SGT_D // Fade near
					float2 fadeNear = (dist - _FadeNearRadius) * _FadeNearScale;
					o.color *= tex2D(_FadeNearTex, fadeNear);
#endif
#if SGT_E // Fade near
					float2 fadeFar = (_FadeFarRadius - dist) * _FadeFarScale;
					o.color *= tex2D(_FadeFarTex, fadeFar);
#endif
#if LIGHT_2 // Softness
					float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
					float partZ  = i.projPos.z;

					o.color *= saturate(_InvFade * (sceneZ - partZ));
#endif
					o.color.a = saturate(o.color.a);
				}
			ENDCG
		} // Pass
	} // SubShader
} // Shader