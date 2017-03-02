Shader "SgtTerrainColor"
{
	Properties
	{
		//_MainTex("Main Tex", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "IgnoreProjector" = "True" }
		
		CGPROGRAM
			#pragma surface Surf Standard
			
			struct Input
			{
				//float2 uv_MainTex;
				float4 color : COLOR;
			};
			
			void Surf(Input IN, inout SurfaceOutputStandard o)
			{
				o.Albedo = IN.color.rgb;
			}
		ENDCG
	} // SubShader
} // Shader