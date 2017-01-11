Shader "Unlit/JCDeferredShading"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BumpMap("Bump Map", 2D) = "bump" {}
		_Shininess("Shininess", Range(0.1, 500)) = 50
		_BumpIntensity("Bump Intensity", Range(0.1, 1)) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				float4 tangent : TANGENT;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uv_bump : TEXCOORD1;
				float4 vertex : SV_POSITION;
				float3 wNormal : TEXCOORD2;
				float3 wTangent : TEXCOORD3;
				float3 wBinormal : TEXCOORD4;
				float3 wPos : TEXCOORD5;
			};

			struct ps_out
			{
				float4 diffuse : SV_TARGET0;
				float4 normal : SV_TARGET1;
				float4 position : SV_TARGET2;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			sampler2D _BumpMap;
			float4 _BumpMap_ST;
			
			float _Shininess;
			float _BumpIntensity;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv_bump = TRANSFORM_TEX(v.uv, _BumpMap);

				o.wNormal = UnityObjectToWorldNormal(v.normal);
				o.wTangent = UnityObjectToWorldDir(v.tangent.xyz);
				o.wBinormal = cross(o.wNormal, o.wTangent) * v.tangent.w;

				o.wPos = mul(unity_ObjectToWorld, v.vertex).xyz;

				return o;
			}
			
			ps_out frag (v2f i)
			{
				ps_out o;
				UNITY_INITIALIZE_OUTPUT(ps_out, o);

				fixed4 col = tex2D(_MainTex, i.uv);
				float3 normal = UnpackNormal(tex2D(_BumpMap, i.uv_bump));
				normal = lerp(float3(0,0,1), normal, _BumpIntensity);
				float3x3 worldToTangent = float3x3(i.wTangent, i.wBinormal, i.wNormal);
				float3 wNormal = normalize(mul(normal, worldToTangent))/*tangent to world*/;

				o.diffuse = float4(col.rgb, _Shininess);
				o.normal = float4(wNormal, 1);
				o.position = float4(i.wPos, 1);

				return o;
			}
			ENDCG
		}
	}
}
