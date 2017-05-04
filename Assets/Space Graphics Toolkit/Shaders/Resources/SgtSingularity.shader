// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Hidden/SgtSingularity"
{
	Properties
	{
		_Center("Center", Vector) = (0, 0, 0)
		_EdgeFadePower("Edge Fade Power", Float) = 10

		_PinchPower("Pinch Power", Float) = 10
		_PinchScale("Pinch Scale", Float) = 1
		_PinchOffset("Pinch Offset", Float) = 1

		_HolePower("Hole Power", Float) = 1
		_HoleColor("Hole Color", Color) = (0, 0, 0, 1)

		_TintPower("Tint Power", Float) = 1
		_TintColor("Tint Color", Color) = (0, 0, 0, 1)
	}
	SubShader
	{
		Tags
		{
			"Queue"           = "Transparent"
			"RenderType"      = "Opaque"
			"IgnoreProjector" = "True"
		}
		GrabPass
		{
			Name "BASE"
			Tags { "LightMode" = "Always" }
 		}
		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha, Zero One
			Cull Front
			Lighting Off
			ZWrite Off
			Name "BASE"
			Tags { "LightMode" = "Always" }
			
			CGPROGRAM
				#pragma vertex Vert
				#pragma fragment Frag
				// Tint
				#pragma multi_compile __ SGT_A
				// Fade Center, Fade Fragment
				#pragma multi_compile __ SGT_B SGT_C
				
				sampler2D _GrabTexture;
				float4    _Center;
				float     _EdgeFadePower;
				float     _PinchPower;
				float     _PinchScale;
				float     _PinchOffset;
				float     _HolePower;
				float4    _HoleColor;
				float     _TintPower;
				float4    _TintColor;
				
				struct a2v
				{
					float4 vertex    : POSITION;
					float3 normal    : NORMAL; // corners
					float2 texcoord0 : TEXCOORD0; // uv
				};
				
				struct v2f
				{
					float4 vertex    : SV_POSITION;
					float2 texcoord0 : TEXCOORD0;
					float2 texcoord1 : TEXCOORD1;
					float  texcoord2 : TEXCOORD2;
				};
				
				struct f2g
				{
					fixed4 color : COLOR;
				};
				
				void Vert(a2v i, out v2f o)
				{
					float4 vertexMVP = UnityObjectToClipPos(i.vertex);
					float4 centerMVP = mul(UNITY_MATRIX_VP, _Center);
					float4 vertM     = mul(unity_ObjectToWorld, i.vertex);
					float3 cam2vertM = _WorldSpaceCameraPos - vertM.xyz;
					float3 normalM   = mul((float3x3)unity_ObjectToWorld, i.normal);
					float  rim       = dot(normalize(cam2vertM), normalize(normalM));
#if UNITY_UV_STARTS_AT_TOP
					float scale = -1.0;
#else
					float scale = 1.0;
#endif
					o.vertex    = vertexMVP;
					o.texcoord0 = (float2(vertexMVP.x, vertexMVP.y*scale) + vertexMVP.w) * 0.5 / vertexMVP.w; // Screen UV
					o.texcoord1 = (float2(centerMVP.x, centerMVP.y*scale) + centerMVP.w) * 0.5 / centerMVP.w; // Center UV
					o.texcoord2 = rim;
				}
				
				void Frag(v2f i, out f2g o)
				{
					float  rim      = abs(i.texcoord2);
					float  rimScale = saturate(rim * _PinchScale + _PinchOffset);
					float  rimPinch = pow(rimScale, _PinchPower);
					float2 coord    = lerp(i.texcoord0, i.texcoord1, rimPinch);
#if SGT_B // Fade Center
					float2 shift = abs(i.texcoord1 - 0.5f) * 2.0f;
					float  edge  = saturate(max(shift.x, shift.y));

					coord = lerp(coord, i.texcoord0, pow(edge, _EdgeFadePower));
#endif
#if SGT_C // Fade Fragment
					float2 shift = abs(i.texcoord0 - 0.5f) * 2.0f;
					float  edge  = saturate(max(shift.x, shift.y));

					coord = lerp(coord, i.texcoord0, pow(edge, _EdgeFadePower));
#endif
					// Base color from grab
					o.color.rgb = tex2D(_GrabTexture, coord).rgb;

					// Fade edges of the screen out
					o.color.a = 1.0f;
#if SGT_A // Tint
					// Tint color
					o.color.rgb = lerp(o.color.rgb, o.color.rgb * _TintColor, pow(rim, _TintPower));
#endif
					// Hole color
					o.color = lerp(o.color, _HoleColor, pow(rimScale, _HolePower));
				}
			ENDCG
		} // Pass
	} // SubShader
	SubShader
	{
		Blend DstColor Zero
		Pass
		{
			Name "BASE"
			SetTexture [_MainTex] {	combine texture }
		}
	}
} // Shader