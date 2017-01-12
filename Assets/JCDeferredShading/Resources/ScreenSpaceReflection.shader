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
				
				if (dot(wEyeDir, wNormal) <= 0)
				{
					return c;
				}

				float3 vRefl = mul(_SSR_V_MATRIX, float4(wRefl, 0)).xyz;
				float3 vPos0 = mul(_SSR_V_MATRIX, float4(wFragPos.xyz, 1)).xyz;
				float3 vPos1 = vPos0 + vRefl;
				float4 clipPos0 = mul(_SSR_P_MATRIX, float4(vPos0, 1));
				float4 clipPos1 = mul(_SSR_P_MATRIX, float4(vPos1, 1));
				float2 uv0 = clipPos0.xy / clipPos0.w * 0.5 + 0.5;
				float2 uv1 = clipPos1.xy / clipPos1.w * 0.5 + 0.5;
				float2 pixel0 = uv0 * _ScreenPixelSize;
				float2 pixel1 = uv1 * _ScreenPixelSize;

				float2 delta = pixel1 - pixel0;
				bool isSwapped = false;
				if (abs(delta.x) < abs(delta.y))
				{
					isSwapped = true;
					delta.xy = delta.yx;
					pixel0.xy = pixel0.yx;
					pixel1.xy = pixel1.yx;
				}
				float signDir = sign(delta.x);
				if (signDir == 0)
				{
					return c;
				}
				float invdx = signDir / delta.x;
				float2 dPixel = float2(signDir, delta.y * invdx);
				float dVPosZ = (vPos1.z - vPos0.z) * invdx;

				float depthTolerance = 0;
				float startOffsetFactor = 2;
				float vPosZ = vPos0.z + dVPosZ*startOffsetFactor;
				float2 pixel = pixel0 + dPixel*startOffsetFactor;
				float count = 0;
				float loop = 100;
				for (; /*pixel.x * __sign < pixel1.x * __sign*/count < loop; pixel += dPixel, vPosZ += dVPosZ, ++count)
				{
					float2 unswappedPixel = isSwapped ? pixel.yx : pixel.xy;
					float2 uv = unswappedPixel / _ScreenPixelSize;
					if (uv.x < 0 || uv.y < 0 || uv.x > 1 || uv.y > 1)
					{
						break;
					}
					float4 doubleFaceDepth = tex2D(_DoubleFaceDepthBuffer, uv);
					// vPosZ and doubleFaceDepth.xy are negative values
					if (vPosZ < doubleFaceDepth.x-depthTolerance && vPosZ > doubleFaceDepth.y+depthTolerance)
					{
						c = tex2D(_ResultBuffer, uv) * min((1 - (count / loop)) * 2, 1);
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
