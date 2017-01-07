Shader "Hidden/JCDeferredShading/BackFaceDepth"
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
			ColorMask G
			ZTest LEqual
			ZWrite On
			Cull Front

			CGPROGRAM
			#pragma vertex vert_double_face_depth
			#pragma fragment frag_back_face_depth
			ENDCG
		}
	}
}
