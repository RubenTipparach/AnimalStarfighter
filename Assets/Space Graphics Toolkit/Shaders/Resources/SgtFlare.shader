// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "SgtFlare"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_ZTest("ZTest", Float) = 0
	}
	SubShader
	{
		Tags
		{
			"Queue"             = "Transparent"
			"RenderType"        = "Transparent"
			"PreviewType"       = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Pass
		{
			Blend One One
			Cull Off
			ZWrite Off
			ZTest [_ZTest]

			CGPROGRAM
				#pragma vertex   Vert
				#pragma fragment Frag

				sampler2D _MainTex;

				struct a2v
				{
					float4 vertex   : POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex   : SV_POSITION;
					float2 texcoord : TEXCOORD0;
				};

				struct f2g
				{
					float4 color : COLOR;
				};

				void Vert(a2v i, out v2f o)
				{
					o.vertex   = UnityObjectToClipPos(i.vertex);
					o.texcoord = i.texcoord;
				}

				void Frag(v2f i, out f2g o)
				{
					o.color = tex2D(_MainTex, i.texcoord);
				}
		ENDCG
		} // Pass
	} // SubShader
} // Shader