Shader "Hidden/JCDeferredShading/FrontFaceDepth"
{
	Properties
	{
	}

	CGINCLUDE
	
		#include "UnityCG.cginc"
		#include "JCDSInclude.cginc"

	ENDCG

	SubShader
	{
		Pass
		{
			ColorMask R
			ZTest LEqual
			ZWrite On
			Cull Back

			CGPROGRAM
			#pragma vertex vert_double_face_depth
			#pragma fragment frag_front_face_depth
			ENDCG
		}
	}
}
