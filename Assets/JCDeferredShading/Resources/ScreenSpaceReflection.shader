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
			fixed4 c = fixed4(0,0,0,0);
			float2 screenUV = i.uv;

			float4 wFragPos = tex2D(_PositionBuffer, screenUV);
			if (wFragPos.w == 0)
			{
				return c;
			}
			else
			{
				float3 wNormal = tex2D(_NormalBuffer, screenUV);
				float3 wEyePos = _WorldSpaceCameraPos.xyz;
				float3 wEyeDir = normalize(wEyePos - wFragPos.xyz);
				float3 wRefl = reflect(-wEyeDir, wNormal);
				
				float3 check_wpos;
				for (int i = 1; i < 21; ++i)
				{
					float3 check_wpos = wFragPos.xyz + wRefl * 0.3 * i;
					float4 check_vp_pos = mul(_SSR_VP_MATRIX, float4(check_wpos, 1));
					float2 check_screen_uv = check_vp_pos.xy / check_vp_pos.w * 0.5 + 0.5;
					float4 check_wFragPos = tex2D(_PositionBuffer, check_screen_uv);
					if (check_vp_pos.z > check_wFragPos.w)
					{
						c = tex2D(_ResultBuffer, check_screen_uv) * (1 - saturate(length(check_wFragPos.xyz - wFragPos.xyz) / 6.0));
						break;
					}
				}

				return c;
			}
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
