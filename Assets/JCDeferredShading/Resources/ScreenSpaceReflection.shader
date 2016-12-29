Shader "Hidden/JCDeferredShading/ScreenSpaceReflection"
{
	Properties
	{
	}

	CGINCLUDE

		#include "UnityCG.cginc"
		#include "JCDSInclude.cginc"

		fixed4 frag(v2f i) : SV_Target
		{
			fixed4 col = tex2D(_NormalBuffer, i.uv);
			return 0;
		}
	ENDCG

	SubShader
	{
		Pass
		{
			Blend One One
			ZTest Always
			ZWrite Off
			Cull Back

			CGPROGRAM
			#pragma vertex vert_screen_quad
			#pragma fragment frag
			ENDCG
		}

		Pass
		{
			Blend One One
			ZTest Always
			ZWrite Off
			Cull Front

			CGPROGRAM
			#pragma vertex vert_screen_quad
			#pragma fragment frag
			ENDCG
		}
	}
}
