Shader "Hidden/JCDeferredShading/CompositeResultBuffer"
{
	Properties
	{
	}

	CGINCLUDE
	
		#include "UnityCG.cginc"

		struct appdata
		{
			float4 vertex : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct v2f
		{
			float2 uv : TEXCOORD0;
			float4 pos : TEXCOORD1;
			float4 vertex : SV_POSITION;
		};

		uniform sampler2D _DiffuseBuffer;
		uniform sampler2D _NormalBuffer;
		uniform sampler2D _PositionBuffer;
		uniform sampler2D _ResultBuffer;

		uniform float4 _DirLightDir;
		uniform fixed4 _DirLightColor;

		uniform float3 _PointLightPos;
		uniform fixed4 _PointLightColor;
		uniform float4 _PointLightRange;

		v2f vert(appdata v)
		{
			v2f o;
			o.vertex = v.vertex;
			o.uv = v.uv;
			return o;
		}

		v2f vert_point_lighting(appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.pos = o.vertex;
			o.uv = v.uv;
			return o;
		}

		float4 frag_dir_lighting(v2f i) : SV_Target
		{
			float4 diffuse = tex2D(_DiffuseBuffer, i.uv);
			float3 wNormal = tex2D(_NormalBuffer, i.uv);
			float3 wLightDir = _DirLightDir.xyz;
			float4 c = diffuse * _DirLightColor * _DirLightDir.w * dot(wNormal, wLightDir);

			return c;
		}

		float4 frag_point_lighting(v2f i) : SV_Target
		{
			float2 screenUV = i.pos.xy / i.pos.w * 0.5 + 0.5;

			#if UNITY_UV_STARTS_AT_TOP
				screenUV.y = 1.0 - screenUV.y;
			#endif

			float4 diffuse = tex2D(_DiffuseBuffer, screenUV);
			float4 wFragPos = tex2D(_PositionBuffer, screenUV);
			float3 wLightDir = _PointLightPos.xyz - wFragPos.xyz;
			float3 wLightDir_norm = normalize(wLightDir);
			float3 wNormal = tex2D(_NormalBuffer, screenUV);
			float3 wEyePos = _WorldSpaceCameraPos.xyz;
			float3 wEyeDir = normalize(wEyePos - wFragPos);
			float3 wHalf = normalize(wEyeDir + wLightDir_norm);
			float wNdotH = max(0, dot(wHalf, wNormal));
			float specular = pow(wNdotH, diffuse.a);
			float wNdotD = max(0, dot(wNormal, wLightDir_norm));
			float attenuation = (1 - saturate(length(wLightDir) * _PointLightRange.x));

			float4 c = float4(0,0,0,1);
			c += diffuse *_PointLightColor * _PointLightRange.y * wNdotD * attenuation;
			c += specular * attenuation * wNdotD * _PointLightRange.y;
			c.a = 1;

			return c;
		}

		float4 frag_result(v2f i) : SV_Target
		{
			return tex2D(_ResultBuffer, i.uv);
		}

	ENDCG

	SubShader
	{
		// Directional Lighting
		Pass
		{
			Blend One One
			ZTest Always
			ZWrite Off
			Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_dir_lighting
			ENDCG
		}

		// Point lighting
		Pass
		{
			Blend One One
			ZTest Greater
			ZWrite Off
			Cull Front

			CGPROGRAM
			#pragma vertex vert_point_lighting
			#pragma fragment frag_point_lighting
			ENDCG
		}

		// Result
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_result
			ENDCG
		}
	}
}
